using UnityEngine;
using UnityEngine.AI;
using cowsins;

[RequireComponent(typeof(AudioSource))]
public class TankBossAI : MonoBehaviour, IDamageable, ISpecialEnemy
{
    [Header("Player")]
    public Transform target;

    [Header("Health")]
    public int maxHealth = 500;
    public int currentHealth;

    [Header("Detection")]
    public float detectRange = 20f;

    [Header("Movement")]
    public float runSpeed = 3.5f;
    public float rotationSpeed = 10f;

    [Header("Attack")]
    public float meleeRange = 3f;
    public float jumpAttackRange = 10f;

    public float attackCooldown = 2f;
    public float jumpAttackCooldown = 10f;

    [Header("Damage")]
    public float punchDamage = 30f;
    public float swipeDamage = 40f;
    public float jumpDamage = 60f;
    public float jumpRadius = 5f;
    public bool shakeCameraOnLand = true;

    [Header("Scream")]
    public float screamDuration = 2.5f;

    [Header("Audio")]
    [Tooltip("Sound khi tank phát hiện player và bắt đầu scream.")]
    public AudioClip screamClip;
    [Tooltip("Sound khi tank thực hiện đòn đánh cận chiến (swipe/punch).")]
    public AudioClip meleeAttackClip;
    [Tooltip("Sound khi tank đáp xuống đất sau jump attack.")]
    public AudioClip jumpLandClip;
    [Tooltip("Sound khi tank chết.")]
    public AudioClip deathClip;

    [Header("Loot")]
    [Tooltip("Loot table: mỗi entry roll độc lập, có thể rơi 0..N loại cùng lúc.")]
    public LootDropEntry[] lootTable;
    [Tooltip("Fallback khi lootTable trống: loot đơn lẻ theo dropChance.")]
    public GameObject dropPrefab;
    [Range(0, 100)]
    public float dropChance = 100f;
    [Tooltip("Khoảng cách nâng loot lên so với vị trí boss khi rớt xuống.")]
    public float dropHeightOffset = 2f;
    [Tooltip("Bật hiệu ứng loot nhảy ra khỏi boss khi chết.")]
    public bool popLootOnDeath = true;
    [Tooltip("Vận tốc đứng (lên) khi loot bị bắn ra (m/s).")]
    public float lootPopUpwardSpeed = 6f;
    [Tooltip("Vận tốc ngang tối đa khi loot bị bắn ra (m/s).")]
    public float lootPopHorizontalSpeed = 4f;

    [Header("Loot Trail Effect")]
    [Tooltip("Cấu hình vệt trail + glow particle khi loot bay. Chỉnh trực tiếp trên tank boss.")]
    public LootTrailSettings lootTrailSettings = new LootTrailSettings();

    private Animator animator;
    private NavMeshAgent agent;
    private Rigidbody rb;
    private Collider col;
    private AudioSource audioSource;

    private float attackTimer;
    private float jumpTimer;

    private bool isDead;
    public bool IsDead { get { return isDead; } }
    private bool isHit;
    private bool isAttacking;
    private bool isJumpAttacking;

    private bool hasScreamed;
    private bool isScreaming;

    private int attackIndex;

    private void Start()
    {
        currentHealth = maxHealth;

        animator = GetComponentInChildren<Animator>();
        agent = GetComponent<NavMeshAgent>();
        rb = GetComponent<Rigidbody>();
        col = GetComponent<Collider>();
        audioSource = GetComponent<AudioSource>();

        FindPlayer();

        if (agent != null)
        {
            agent.speed = runSpeed;
            agent.acceleration = 20f;
            agent.angularSpeed = 300f;
            agent.stoppingDistance = meleeRange;
            agent.updateRotation = false;
            // Performance: HighQuality avoidance is ~quadratic with agent count;
            // use cheap avoidance so the Tank doesn't overload the avoidance
            // system when many regular zombies are also active (Ch5 boss wave).
            agent.obstacleAvoidanceType = UnityEngine.AI.ObstacleAvoidanceType.LowQualityObstacleAvoidance;
            agent.avoidancePriority = 10; // Boss gets high priority so regular zombies yield.
        }

        // Keep the Rigidbody KINEMATIC while alive so the NavMeshAgent fully
        // controls movement. A non-kinematic Rigidbody with gravity fights the
        // agent on stairs/slopes/bridges (Ch5 = ApartmentBridge): gravity pulls
        // the Tank down, the capsule collider catches on step edges, and the two
        // systems jitter — the Tank slides in one direction. Kinematic mode lets
        // the agent drive position smoothly while still sending collision events.
        // Die() sets kinematic again for the death animation, so this is consistent.
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.useGravity = true; // kept on so the body can fall if the agent is disabled later.
        }

        animator.speed =
            Random.Range(0.95f, 1.05f);
    }

    private void Update()
    {
        if (isDead)
            return;

        if (target == null)
        {
            findPlayerTimer += Time.deltaTime;
            if (findPlayerTimer >= 1f)
            {
                FindPlayer();
                findPlayerTimer = 0f;
            }
            return;
        }

        attackTimer += Time.deltaTime;
        jumpTimer += Time.deltaTime;

        float distance =
            Vector3.Distance(
                transform.position,
                target.position
            );

        HandleJumpState();

        if (isHit)
            return;

        //------------------------------------------------
        // FIRST SCREAM
        //------------------------------------------------

        if (!hasScreamed &&
            distance <= detectRange)
        {
            StartScream();
            return;
        }

        if (isScreaming)
            return;

        FaceTarget();

        //------------------------------------------------
        // CHASE
        //------------------------------------------------

        if (!isAttacking &&
            !isJumpAttacking)
        {
            agent.isStopped = false;

            agent.SetDestination(
                target.position
            );
        }

        //------------------------------------------------
        // JUMP ATTACK
        //------------------------------------------------

        if (!isJumpAttacking &&
            !isAttacking &&
            distance > meleeRange &&
            distance <= jumpAttackRange &&
            jumpTimer >= jumpAttackCooldown)
        {
            StartJumpAttack();
            return;
        }

        //------------------------------------------------
        // MELEE ATTACK
        //------------------------------------------------

        if (!isAttacking &&
            distance <= meleeRange &&
            attackTimer >= attackCooldown)
        {
            StartMeleeAttack();
        }
    }

    private float findPlayerTimer;
    private static Transform cachedPlayerTransform;
    private IDamageable cachedPlayerDamageable;
    private CameraEffects cachedCameraEffects;

    private void FindPlayer()
    {
        if (cachedPlayerTransform != null)
        {
            target = cachedPlayerTransform;
            cachedPlayerDamageable = target.GetComponent<IDamageable>();
            cachedCameraEffects = target.GetComponent<CameraEffects>();
            return;
        }

        GameObject player =
            GameObject.FindGameObjectWithTag(
                "Player"
            );

        if (player != null)
        {
            cachedPlayerTransform = player.transform;
            target = player.transform;
            cachedPlayerDamageable = target.GetComponent<IDamageable>();
            cachedCameraEffects = target.GetComponent<CameraEffects>();
        }
    }

    private void FaceTarget()
    {
        if (target == null)
            return;

        Vector3 dir =
            target.position -
            transform.position;

        dir.y = 0;

        if (dir.sqrMagnitude < 0.01f)
            return;

        Quaternion rot =
            Quaternion.LookRotation(dir);

        transform.rotation =
            Quaternion.Slerp(
                transform.rotation,
                rot,
                rotationSpeed *
                Time.deltaTime
            );
    }

    //==================================================
    // SCREAM
    //==================================================

    private void StartScream()
    {
        hasScreamed = true;
        isScreaming = true;

        agent.isStopped = true;

        animator.SetBool("isPlayerNear", true);
        animator.SetTrigger("Scream");

        PlaySound(screamClip);

        Invoke(
            nameof(EndScream),
            screamDuration
        );
    }

    private void EndScream()
    {
        isScreaming = false;
    }

    //==================================================
    // MELEE ATTACK
    //==================================================

    private void StartMeleeAttack()
    {
        isAttacking = true;

        attackTimer = 0;

        attackIndex =
            Random.Range(0, 2);

        animator.SetInteger(
            "AttackIndex",
            attackIndex
        );

        animator.SetTrigger("Attack");

        PlaySound(meleeAttackClip);

        Invoke(
            nameof(ResetAttack),
            attackCooldown
        );
    }

    private void ResetAttack()
    {
        isAttacking = false;
    }

    //==================================================
    // JUMP ATTACK
    //==================================================

    private void StartJumpAttack()
    {
        isJumpAttacking = true;
        isAttacking = true;

        jumpTimer = 0;

        agent.isStopped = true;

        // The Rigidbody is already kinematic from Start(), so there is no need
        // to toggle gravity/velocity here — the agent is stopped and the
        // animation drives the visual. Previously this disabled gravity on a
        // non-kinematic body, which caused the Tank to slide after the jump
        // ended (gravity was re-enabled but the agent was still stopped, so
        // physics pulled it down a slope/stairs).
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        animator.Play(
            "Mutant Jump Attack"
        );
    }

    private void HandleJumpState()
    {
        AnimatorStateInfo state =
            animator.GetCurrentAnimatorStateInfo(0);

        if (
            isJumpAttacking &&
            !state.IsName("Mutant Jump Attack")
        )
        {
            isJumpAttacking = false;
            isAttacking = false;

            // Nothing to restore — the Rigidbody stayed kinematic throughout
            // the jump. The agent will resume driving movement next Update.
        }
    }

    //==================================================
    // ANIMATION EVENTS
    //==================================================

    public void AttackHit()
    {
        DamagePlayer(punchDamage);
    }

    public void SwipeHit()
    {
        DamagePlayer(swipeDamage);
    }

    private void PlaySound(AudioClip clip)
    {
        if (clip == null || audioSource == null)
            return;

        audioSource.PlayOneShot(clip);
    }

    public void JumpLand()
    {
        PlaySound(jumpLandClip);

        Collider[] hits =
            Physics.OverlapSphere(
                transform.position,
                jumpRadius
            );

        foreach (Collider hit in hits)
        {
            if (hit.transform == transform)
                continue;

            if (target != null && hit.transform.root == target.root)
            {
                if (cachedPlayerDamageable != null)
                {
                    cachedPlayerDamageable.Damage(
                        jumpDamage,
                        false
                    );
                }

                if (cachedCameraEffects != null)
                {
                    cachedCameraEffects.ExplosionShake(
                        Vector3.Distance(
                            cachedCameraEffects.transform.position,
                            transform.position
                        ) * 0.5f
                    );
                }
            }
            else
            {
                // Damage other enemies or entities
                IDamageable damageable =
                    hit.GetComponent<IDamageable>();

                if (damageable != null)
                {
                    damageable.Damage(
                        jumpDamage,
                        false
                    );
                }
            }
        }
    }

    private void DamagePlayer(float damage)
    {
        if (target == null)
            return;

        float distance =
            Vector3.Distance(
                transform.position,
                target.position
            );

        if (distance > meleeRange + 1f)
            return;

        if (cachedPlayerDamageable != null)
        {
            cachedPlayerDamageable.Damage(
                damage,
                false
            );
        }
    }

    //==================================================
    // DAMAGE
    //==================================================

    public void Damage(
        float damage,
        bool isHeadshot
    )
    {
        TakeDamage(
            Mathf.RoundToInt(damage)
        );
    }

    public void TakeDamage(int damage)
    {

        if (isDead)
            return;

        currentHealth -= damage;

        if (PlayerStatsTracker.Instance != null)
            PlayerStatsTracker.Instance.RegisterDamageDealt(damage);

        animator.SetTrigger("Hit");

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    //==================================================
    // DEATH
    //==================================================

    public void Die()
    {
        if (isDead)
            return;

        isDead = true;

        CancelInvoke();

        if (CombatFeedbackHUD.Instance != null)
            CombatFeedbackHUD.Instance.ShowKill("Tank");

        if (AIDirector.Instance != null)
            AIDirector.Instance.RegisterKill();

        if (PlayerStatsTracker.Instance != null)
            PlayerStatsTracker.Instance.RegisterTankKill();

        if (AchievementManager.Instance != null)
            AchievementManager.Instance.NotifyTankKill();

        if (ScoreManager.Instance != null)
            ScoreManager.Instance.AddKill(500);

        agent.enabled = false;

        animator.SetBool(
            "isDeath",
            true
        );

        animator.SetTrigger(
            "Death"
        );

        PlaySound(deathClip);

        if (col != null)
        {
            col.enabled = false;
        }

        if (rb != null)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
        }

        LootDropHelper.TryDropLoot(
            lootTable,
            dropPrefab,
            dropChance,
            transform.position,
            dropHeightOffset,
            popLootOnDeath,
            lootPopUpwardSpeed,
            lootPopHorizontalSpeed,
            lootTrailSettings);

        Destroy(
            gameObject,
            6f
        );
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(
            transform.position,
            detectRange
        );

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(
            transform.position,
            meleeRange
        );

        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(
            transform.position,
            jumpAttackRange
        );
    }
}
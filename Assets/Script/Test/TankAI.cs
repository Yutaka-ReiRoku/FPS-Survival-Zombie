using UnityEngine;
using UnityEngine.AI;
using cowsins;

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

    private Animator animator;
    private NavMeshAgent agent;
    private Rigidbody rb;
    private Collider col;

    private float attackTimer;
    private float jumpTimer;

    private bool isDead;
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

        FindPlayer();

        if (agent != null)
        {
            agent.speed = runSpeed;
            agent.acceleration = 20f;
            agent.angularSpeed = 300f;
            agent.stoppingDistance = meleeRange;
            agent.updateRotation = false;
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

        animator.SetTrigger("Scream");

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

        if (rb != null)
        {
            rb.useGravity = false;

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

            if (rb != null)
            {
                rb.useGravity = true;
            }
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

    public void JumpLand()
    {
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

        agent.enabled = false;

        animator.SetBool(
            "isDeath",
            true
        );

        animator.SetTrigger(
            "Death"
        );

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
            lootPopHorizontalSpeed);

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
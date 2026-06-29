using UnityEngine;
using UnityEngine.AI;
using cowsins;

/// <summary>
/// Một entry trong loot table. Mỗi entry roll độc lập theo
/// <see cref="dropChance"/>; nếu trúng thì rơi <see cref="minQuantity"/>..
/// <see cref="maxQuantity"/> bản sao của <see cref="prefab"/> (mỗi bản sao
/// pop hướng riêng). Có thể có nhiều entry trùng prefab.
/// </summary>
[System.Serializable]
public struct LootDropEntry
{
    [Tooltip("Prefab loot sẽ rơi (Coin, Experience, Healthpack, ...).")]
    public GameObject prefab;

    [Range(0, 100)]
    [Tooltip("Xác suất rơi entry này (0-100). Mỗi entry roll độc lập.")]
    public float dropChance;

    [Tooltip("Số bản sao tối thiểu nếu entry trúng (mặc định 1).")]
    public int minQuantity;

    [Tooltip("Số bản sao tối đa nếu entry trúng (mặc định 1).")]
    public int maxQuantity;
}

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(AudioSource))]
public class ZombieAI : MonoBehaviour, IDamageable, ICrookEnemy, IEnemyHealthReadout
{
    public float GetMaxHealth()
    {
        return maxHealth;
    }

    // ---- Health observation (read-only; for UI such as EnemyHealthBar) ----
    // Combat logic is unchanged; these only expose state already tracked internally.

    /// <summary>Current hit points (never negative for display).</summary>
    public int CurrentHealth
    {
        get { return Mathf.Max(0, currentHealth); }
    }

    /// <summary>Normalized health in [0,1]. 1 = full, 0 = dead/empty.</summary>
    public float HealthFraction
    {
        get { return maxHealth > 0 ? Mathf.Clamp01((float)currentHealth / maxHealth) : 0f; }
    }

    /// <summary>True once this zombie has died (before it is pooled back to inactive).</summary>
    public bool IsDead
    {
        get { return isDead; }
    }

    /// <summary>
    /// Raised whenever health changes (damage or death). Argument is the new
    /// <see cref="HealthFraction"/> in [0,1]. Subscribers must null-check the
    /// zombie's <see cref="IsDead"/> state to decide show/hide.
    /// </summary>
    public event System.Action<float> OnHealthChanged;

    [Header("Target")]
    public Transform target;

    [Header("Movement")]
    public float walkSpeed = 1.5f;
    public float runSpeed = 3.5f;
    public float detectDistance = 20f;
    public float attackDistance = 2f;

    [Header("Wander")]
    public float wanderRadius = 15f;
    public float wanderInterval = 5f;

    [Header("Attack")]
    public float attackCooldown = 1.5f;
    public int attackDamage = 20;

    [Header("Health")]
    public int maxHealth = 100;

    [Header("Audio")]
    public AudioClip growlClip;
    public AudioClip attackClip;
    public AudioClip hitClip;
    public AudioClip deathClip;
    public AudioClip footstepClip;

    [Header("Loot")]
    [Tooltip("Loot table mới: mỗi entry roll độc lập, có thể rơi 0..N loại cùng lúc. Để trống nếu muốn dùng dropPrefab/dropChance cũ bên dưới.")]
    public LootDropEntry[] lootTable;

    [Tooltip("Fallback khi lootTable trống: loot đơn lẻ theo dropChance.")]
    public GameObject dropPrefab;
    [Range(0, 100)]
    public float dropChance = 50f;
    [Tooltip("Khoảng cách nâng loot lên so với vị trí zombie khi rớt xuống.")]
    public float dropHeightOffset = 1.5f;
    [Tooltip("Bật hiệu ứng loot nhảy ra khỏi zombie khi chết.")]
    public bool popLootOnDeath = true;
    [Tooltip("Vận tốc đứng (lên) khi loot bị bắn ra (m/s).")]
    public float lootPopUpwardSpeed = 4.5f;
    [Tooltip("Vận tốc ngang tối đa khi loot bị bắn ra (m/s).")]
    public float lootPopHorizontalSpeed = 2.5f;

    [Header("Loot Trail Effect")]
    [Tooltip("Cấu hình vệt trail + glow particle khi loot bay. Chỉnh trực tiếp trên zombie.")]
    public LootTrailSettings lootTrailSettings = new LootTrailSettings();

    [Header("Rewards")]
    public float experienceReward = 10f;
    public float headshotBonusExperience = 5f;
    public int coinReward = 5;
    public int headshotBonusCoins = 5;

    private Animator animator;
    private NavMeshAgent agent;
    private AudioSource audioSource;
    private Rigidbody rb;

    private int currentHealth;

    private float attackTimer;
    private float wanderTimer;

    private bool isDead;
    private bool isAttacking;
    private bool hasDetectedPlayer;
    private bool lastHitWasHeadshot;

    private static readonly int SpeedHash =
        Animator.StringToHash("Speed");

    private static readonly int AttackHash =
        Animator.StringToHash("Attack");

    private static readonly int HitHash =
        Animator.StringToHash("Hit");

    private static readonly int DeathHash =
        Animator.StringToHash("Death");

    private static readonly int AttackIndexHash =
        Animator.StringToHash("AttackIndex");

    void Awake()
    {
        animator = GetComponentInChildren<Animator>();
        agent = GetComponent<NavMeshAgent>();
        audioSource = GetComponent<AudioSource>();
        rb = GetComponent<Rigidbody>();

        // Performance: zombies do not cast/receive shadows (large GPU saving at high counts).
        var smrs = GetComponentsInChildren<SkinnedMeshRenderer>(true);
        for (int i = 0; i < smrs.Length; i++)
        {
            smrs[i].shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            smrs[i].receiveShadows = false;
        }

        // Performance: don't evaluate the animator for zombies no camera renders.
        // NOTE: off-screen zombies won't play attack animations until visible (acceptable for a horde shooter; revert to CullUpdateTransforms if undesired).
        if (animator != null)
            animator.cullingMode = AnimatorCullingMode.CullCompletely;
    }

    void OnEnable()
    {
        isDead = false;
        isAttacking = false;
        hasDetectedPlayer = false;
        lastHitWasHeadshot = false;
        currentHealth = maxHealth;
        attackTimer = 0f;
        wanderTimer = wanderInterval;

        if (target == null)
        {
            GameObject player =
                GameObject.FindGameObjectWithTag("Player");

            if (player != null)
                target = player.transform;
        }

        if (agent != null)
        {
            agent.enabled = true;
            agent.isStopped = false;
            agent.speed = walkSpeed;
            agent.stoppingDistance = attackDistance;
            // Let the agent control rotation during Wander; ChasePlayer overrides
            // via FaceTarget() which is fine since both point toward the player/path.
            agent.updateRotation = true;
            // Performance: HighQuality avoidance is ~quadratic with agent count; use cheap avoidance + varied priority.
            agent.obstacleAvoidanceType = UnityEngine.AI.ObstacleAvoidanceType.LowQualityObstacleAvoidance;
            agent.avoidancePriority = Random.Range(30, 70);
        }

        // Keep the Rigidbody KINEMATIC while alive so the NavMeshAgent fully
        // controls movement. A non-kinematic Rigidbody with gravity fights the
        // agent on stairs/slopes: gravity pulls the zombie down, the capsule
        // collider catches on step edges, and the two systems jitter — the
        // zombie gets stuck at the foot of stairs. Kinematic mode lets the
        // agent drive position smoothly while still sending collision events
        // (for hit detection, triggers, etc.). Die() already sets kinematic
        // for the death animation, so this just makes it consistent.
        if (rb != null)
        {
            rb.isKinematic = true;
            // Keep gravity on so that if the agent is disabled (e.g. on death)
            // and kinematic is later turned off, the body can fall naturally.
            rb.useGravity = true;
        }

        Collider col = GetComponent<Collider>();
        if (col != null)
            col.enabled = true;

        if (animator != null)
        {
            animator.Rebind();
            animator.Update(0f);
        }

        if (AIDirector.Instance != null)
            AIDirector.Instance.RegisterZombie(this);
    }

    void OnDisable()
    {
        if (AIDirector.Instance != null)
            AIDirector.Instance.UnregisterZombie(this);
    }


    private float distanceTimer = 0f;
    private float cachedDistance = 100f;

    void Update()
    {
        if (isDead || target == null)
            return;

        attackTimer += Time.deltaTime;
        wanderTimer += Time.deltaTime;
        distanceTimer += Time.deltaTime;

        if (distanceTimer >= 0.2f)
        {
            cachedDistance =
                Vector3.Distance(
                    transform.position,
                    target.position);
            distanceTimer = 0f;
        }

        if (cachedDistance <= detectDistance)
        {
            ChasePlayer(cachedDistance);
        }
        else
        {
            Wander();
            hasDetectedPlayer = false;
        }

        animator.SetFloat(
            SpeedHash,
            agent.velocity.magnitude / runSpeed,
            0.15f,
            Time.deltaTime);
    }

    private float pathTimer = 0f;

    void ChasePlayer(float distance)
    {
        if (!hasDetectedPlayer)
        {
            PlaySound(growlClip);
            hasDetectedPlayer = true;
        }

        FaceTarget();

        if (distance <= attackDistance)
        {
            agent.isStopped = true;

            if (!isAttacking &&
                attackTimer >= attackCooldown)
            {
                Attack();
            }
        }
        else
        {
            agent.isStopped = false;
            agent.speed = runSpeed;
            
            pathTimer += Time.deltaTime;
            if (pathTimer >= 0.25f)
            {
                agent.SetDestination(target.position);
                pathTimer = 0f;
            }
        }
    }

    void Wander()
    {
        agent.speed = walkSpeed;

        if (wanderTimer < wanderInterval)
            return;

        Vector3 destination =
            RandomNavSphere(
                transform.position,
                wanderRadius);

        agent.SetDestination(destination);

        wanderTimer = 0f;
    }

    void Attack()
    {
        isAttacking = true;
        attackTimer = 0;

        animator.SetInteger(
            AttackIndexHash,
            Random.Range(0, 2));

        animator.SetTrigger(AttackHash);

        PlaySound(attackClip);

        Invoke(nameof(ResetAttack),
            attackCooldown);
    }

    void ResetAttack()
    {
        isAttacking = false;
    }

    public void AttackHit()
    {
        if (target == null)
            return;

        float distance =
            Vector3.Distance(
                transform.position,
                target.position);

        if (distance > attackDistance + 0.5f)
            return;

        IDamageable damageable =
            target.GetComponent<IDamageable>();

        if (damageable != null)
        {
            damageable.Damage(
                attackDamage,
                false);

            // Feed the directional damage indicator (engine Damage carries no direction).
            if (DamageDirectionHUD.Instance != null)
                DamageDirectionHUD.Instance.ShowDamageFrom(transform.position);
        }
    }

    public void TakeDamage(int damage)
    {
        if (isDead)
            return;

        currentHealth -= damage;

        if (PlayerStatsTracker.Instance != null)
            PlayerStatsTracker.Instance.RegisterDamageDealt(damage);

        animator.SetTrigger(HitHash);

        PlaySound(hitClip);

        if (currentHealth <= 0)
        {
            Die();
        }

        // Notify observers (e.g. EnemyHealthBar). Die() above runs synchronously
        // and sets isDead, so a killing blow reports IsDead=true here.
        if (OnHealthChanged != null)
            OnHealthChanged(HealthFraction);
    }

    public void Damage(float damage,
        bool isHeadshot)
    {
        // Capture headshot/critical flag so Die() can award the bonus.
        // Damage -> TakeDamage -> Die run synchronously, so this reflects the killing blow.
        lastHitWasHeadshot = isHeadshot;

        if (CombatFeedbackHUD.Instance != null)
            CombatFeedbackHUD.Instance.ShowHit(transform.position, damage, isHeadshot);

        TakeDamage(
            Mathf.RoundToInt(damage));
    }

    void Die()
    {
        if (isDead)
            return;

        isDead = true;

        if (CombatFeedbackHUD.Instance != null)
            CombatFeedbackHUD.Instance.ShowKill("Zombie");

        if (AIDirector.Instance != null)
            AIDirector.Instance.RegisterKill();

        if (WaveManager.Instance != null)
            WaveManager.Instance.RegisterZombieKill();

        if (PlayerStatsTracker.Instance != null)
            PlayerStatsTracker.Instance.RegisterZombieKill();

        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.AddKill();
            if (lastHitWasHeadshot)
                ScoreManager.Instance.AddHeadshot();
        }

        // Player progression via the real Cowsins ExperienceManager (XP -> level -> skill points -> upgrades).
        if (ExperienceManager.Instance != null && ExperienceManager.Instance.useExperience)
        {
            float xp = experienceReward;
            if (lastHitWasHeadshot)
                xp += headshotBonusExperience;
            ExperienceManager.Instance.AddExperience(xp);
        }

        if (CoinManager.Instance != null && CoinManager.Instance.useCoins)
        {
            int coins = coinReward;
            if (lastHitWasHeadshot)
                coins += headshotBonusCoins;
            CoinManager.Instance.AddCoins(coins);
        }

        PlaySound(deathClip);

        // Rigidbody is already kinematic from OnEnable (so the NavMeshAgent
        // can drive movement on stairs without physics fighting it). Keep it
        // kinematic here so the death animation plays in place after the
        // agent is disabled.
        if (rb != null)
            rb.isKinematic = true;

        agent.isStopped = true;
        agent.enabled = false;

        animator.SetTrigger(DeathHash);

        Collider col =
            GetComponent<Collider>();

        if (col != null)
            col.enabled = false;

        TryDropLoot();

        Invoke(nameof(DeactivateZombie), 6f);
    }

    void DeactivateZombie()
    {
        gameObject.SetActive(false);
    }

    void TryDropLoot()
    {
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
    }

    void FaceTarget()
    {
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
                Time.deltaTime * 8f);
    }

    public void PlayFootstep()
    {
        PlaySound(footstepClip);
    }

    void PlaySound(AudioClip clip)
    {
        if (clip == null)
            return;

        audioSource.PlayOneShot(clip);
    }

    public static Vector3 RandomNavSphere(
        Vector3 origin,
        float distance)
    {
        Vector3 randomDirection =
            Random.insideUnitSphere * distance;

        randomDirection += origin;

        NavMeshHit hit;

        NavMesh.SamplePosition(
            randomDirection,
            out hit,
            distance,
            NavMesh.AllAreas);

        return hit.position;
    }
}
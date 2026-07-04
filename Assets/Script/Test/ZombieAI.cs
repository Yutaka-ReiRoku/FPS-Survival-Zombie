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
    /// True while the zombie is actively chasing the player (detected + within
    /// hysteresis/alert memory). External systems (e.g. Spawm roaming) should
    /// check this before overriding the NavMeshAgent destination.
    /// </summary>
    public bool IsChasing
    {
        get { return !isDead && (hasDetectedPlayer || alertMemoryTimer > 0f); }
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

    [Header("Erratic / Unpredictable Behavior")]
    [Tooltip("Khoảng cách mất dấu player. Lon hon detectDistance de chong flip-flop (hysteresis).")]
    public float loseSightDistance = 28f;
    [Tooltip("Sau khi mat dau, zombie van duoi theo alertMemoryDuration giay truoc khi quay ve Wander.")]
    public float alertMemoryDuration = 6f;
    [Tooltip("Kha nang trigger mot dot lunge (sprint dot ngot) moi giay khi dang duoi o mid-range (0-1).")]
    [Range(0f, 1f)]
    public float lungeChancePerSecond = 0.25f;
    [Tooltip("Toc do lunge = runSpeed * lungeSpeedMultiplier.")]
    public float lungeSpeedMultiplier = 1.8f;
    [Tooltip("Thoi luong moi dot lunge (giay).")]
    public float lungeDuration = 0.7f;
    [Tooltip("Kha nang trigger mot dot feint pause (dung dot ngot) moi giay khi dang duoi (0-1).")]
    [Range(0f, 1f)]
    public float feintPauseChancePerSecond = 0.12f;
    [Tooltip("Thoi luong moi dot feint pause (giay).")]
    public float feintPauseDuration = 0.4f;
    [Tooltip("Bien do jitter (zigzag) them vao path duoi player (met). 0 = tat.")]
    public float chaseJitterRadius = 2.5f;

    [Header("Line of Sight")]
    [Tooltip("If true, zombies require an unobstructed line of sight to the player before initial detection. Once detected, hysteresis and alert memory work as normal (no LOS needed).")]
    public bool requireLineOfSight = true;
    [Tooltip("Layer mask for objects that block the zombie's sight (walls, floors, furniture). Defaults to everything; the player and the zombie's own layer are automatically excluded from the check.")]
    public LayerMask sightObstructionMask = ~0;
    [Tooltip("Eye height offset from the zombie's pivot for the LOS raycast.")]
    public float sightEyeHeight = 1.5f;

    [Header("Stuck Recovery")]
    [Tooltip("How long (seconds) the zombie must be nearly stationary while chasing before it is considered stuck. Higher = more patient (less likely to re-path abruptly).")]
    public float stuckTimeThreshold = 3f;
    [Tooltip("If the zombie moves less than this distance (meters) over stuckTimeThreshold, it is considered stuck.")]
    public float stuckMoveThreshold = 1f;
    [Tooltip("How far to search for an intermediate re-path position when stuck (no teleport — just re-pathing).")]
    public float stuckRepathRadius = 5f;

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

    // --- Erratic behavior runtime state ---
    private float alertMemoryTimer;     // counts down after losing sight; >0 means keep chasing
    private float lungeTimer;           // >0 while a lunge burst is active
    private float feintPauseTimer;      // >0 while a feint pause is active
    private float erraticRollTimer;     // accumulates dt to roll lunge/feint once per second
    private Vector3 chaseJitterOffset;  // current jitter offset applied to chase destination
    private float chaseJitterTimer;     // when to pick a new jitter offset

    // --- Stuck recovery runtime state ---
    private Vector3 _lastStuckCheckPos;
    private float _stuckTimer;
    private bool _wasInAttackRange;     // tracks isStopped transition (attack→chase)
    private int _noPathRetryCount;      // counts consecutive SetDestinationRobust failures when hasPath=false

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

        // Reset erratic behavior state on (re)spawn.
        alertMemoryTimer = 0f;
        lungeTimer = 0f;
        feintPauseTimer = 0f;
        erraticRollTimer = 0f;
        chaseJitterOffset = Vector3.zero;
        chaseJitterTimer = 0f;

        // Reset stuck recovery state on (re)spawn.
        _stuckTimer = 0f;
        _lastStuckCheckPos = transform.position;
        _wasInAttackRange = false;
        _noPathRetryCount = 0;

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

        // First-time detection requires both distance AND line of sight (if
        // requireLineOfSight is enabled). Hysteresis and alert memory below do
        // NOT require LOS — once the zombie has detected the player, it keeps
        // chasing based on distance/memory even if the player briefly breaks
        // LOS (ducking behind cover, etc.).
        if (cachedDistance <= detectDistance &&
            (!requireLineOfSight || HasLineOfSight()))
        {
            // Player within detect range: chase and (re)arm alert memory.
            alertMemoryTimer = alertMemoryDuration;
            ChasePlayer(cachedDistance);
        }
        else if (hasDetectedPlayer && cachedDistance <= loseSightDistance)
        {
            // Hysteresis: once detected, keep chasing while player is still
            // within the larger loseSightDistance (prevents flip-flop at the
            // detectDistance edge).
            ChasePlayer(cachedDistance);
        }
        else if (alertMemoryTimer > 0f)
        {
            // Lost sight but still in alert memory: keep chasing for a while.
            alertMemoryTimer -= Time.deltaTime;
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

        // --- Roll lunge / feint once per second for unpredictability ---
        erraticRollTimer += Time.deltaTime;
        if (erraticRollTimer >= 1f)
        {
            erraticRollTimer = 0f;

            // Only roll lunge when at mid-range (not too close, not too far) and not already lunging.
            if (lungeTimer <= 0f &&
                distance > attackDistance + 1.5f &&
                distance < detectDistance * 0.8f &&
                Random.value < lungeChancePerSecond)
            {
                lungeTimer = lungeDuration;
            }

            // Only roll feint when actively moving toward player and not already pausing/lunging.
            if (feintPauseTimer <= 0f &&
                lungeTimer <= 0f &&
                distance > attackDistance + 2f &&
                Random.value < feintPauseChancePerSecond)
            {
                feintPauseTimer = feintPauseDuration;
            }
        }

        // Tick down active burst/pause timers.
        if (lungeTimer > 0f)
            lungeTimer -= Time.deltaTime;
        if (feintPauseTimer > 0f)
            feintPauseTimer -= Time.deltaTime;

        // --- Feint pause: stop abruptly, face the player, then resume ---
        if (feintPauseTimer > 0f)
        {
            agent.isStopped = true;
            FaceTarget();
            return;
        }

        FaceTarget();

        if (distance <= attackDistance)
        {
            agent.isStopped = true;
            _wasInAttackRange = true;

            if (!isAttacking &&
                attackTimer >= attackCooldown)
            {
                Attack();
            }
        }
        else
        {
            // Transitioning from attack range to chase: the agent's previous
            // path was completed while isStopped=true, so hasPath is false and
            // SetDestination alone may silently fail to create a new path
            // (a known Unity NavMeshAgent issue). Reset the path state and
            // force an immediate re-path on this frame.
            if (_wasInAttackRange)
            {
                _wasInAttackRange = false;
                agent.ResetPath();
                agent.isStopped = false;
                pathTimer = 0.25f; // force SetDestination this frame
            }
            else
            {
                agent.isStopped = false;
            }

            // Lunge burst: sudden sprint faster than normal run.
            float speed = runSpeed;
            if (lungeTimer > 0f)
                speed = runSpeed * lungeSpeedMultiplier;
            agent.speed = speed;

            // --- Jitter the chase destination so the zombie doesn't run in a straight line ---
            pathTimer += Time.deltaTime;
            chaseJitterTimer -= Time.deltaTime;
            if (chaseJitterTimer <= 0f)
            {
                // Pick a new lateral jitter offset around the player.
                Vector3 rand = Random.insideUnitSphere * chaseJitterRadius;
                rand.y = 0f;
                chaseJitterOffset = rand;
                chaseJitterTimer = Random.Range(0.4f, 0.9f);
            }

            if (pathTimer >= 0.25f)
            {
                Vector3 dest = target.position + chaseJitterOffset;
                SetDestinationRobust(dest);
                pathTimer = 0f;
            }

            // --- Stuck detection: if the zombie isn't making progress toward
            // the player while it should be chasing, try to recover by finding
            // a nearby valid NavMesh position and re-pathing. Handles cases
            // where the zombie is wedged against a wall, furniture, or door.
            HandleStuckDetection(distance);
        }
    }

    void Wander()
    {
        agent.speed = walkSpeed;

        // Not chasing — reset stuck detection state.
        _stuckTimer = 0f;
        _lastStuckCheckPos = transform.position;

        if (wanderTimer < wanderInterval)
            return;

        Vector3 destination =
            RandomNavSphere(
                transform.position,
                wanderRadius);

        SetDestinationRobust(destination);

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

        // Achievement tracking: check wall-run state before the kill is fully processed.
        if (AchievementManager.Instance != null)
        {
            AchievementManager.Instance.NotifyZombieKill();
            // Check if the player is currently wall-running.
            var pm = FindAnyObjectByType<PlayerMovement>();
            if (pm != null && pm.IsWallRunning)
                AchievementManager.Instance.NotifyZombieKillWhileWallRunning();
        }

        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.AddKill();
            if (lastHitWasHeadshot)
                ScoreManager.Instance.AddCrit();
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

    //==================================================
    // STUCK DETECTION & RECOVERY
    //==================================================

    /// <summary>
    /// Tracks whether the zombie is actually making progress toward the player.
    /// If it stays nearly stationary for too long while chasing (not in attack
    /// range), it tries to recover by snapping to a nearby valid NavMesh
    /// position and re-pathing. Handles cases where the zombie is wedged
    /// against a wall, furniture, or door.
    /// </summary>
    private void HandleStuckDetection(float distanceToPlayer)
    {
        // Only check for stuck while actively chasing (far enough to need
        // movement). If we're in attack range, being stationary is expected.
        if (distanceToPlayer <= attackDistance + 0.5f)
        {
            _stuckTimer = 0f;
            _lastStuckCheckPos = transform.position;
            _noPathRetryCount = 0;
            return;
        }

        // CRITICAL: If the agent has no path while it should be chasing, it
        // will stand forever without moving. This happens after the agent's
        // previous path completes (e.g. it was stopped in attack range, the
        // path finished, and then the player moved away). SetDestination
        // alone may silently fail to create a new path in this state.
        if (agent != null && agent.isOnNavMesh && !agent.hasPath && !agent.isStopped)
        {
            _noPathRetryCount++;
            SetDestinationRobust(target.position);

            // If SetDestinationRobust keeps failing (agent still has no path
            // after multiple attempts), the agent is in a broken state. Try
            // a full agent reset: disable/re-enable/warp to snap it out. This
            // is NOT a teleport to the player — it warps to the zombie's OWN
            // position to force the agent to re-initialize.
            if (_noPathRetryCount >= 10 && !agent.hasPath)
            {
                Vector3 currentPos = transform.position;
                agent.enabled = false;
                agent.enabled = true;
                if (agent.isOnNavMesh)
                {
                    agent.Warp(currentPos);
                    agent.isStopped = false;
                    SetDestinationRobust(target.position);
                }
                _noPathRetryCount = 0;
            }

            _stuckTimer = 0f;
            _lastStuckCheckPos = transform.position;
            return;
        }

        // Agent has a path but isn't moving — could be avoidance deadlock.
        _noPathRetryCount = 0;

        float moved = Vector3.Distance(transform.position, _lastStuckCheckPos);

        if (moved < stuckMoveThreshold * 0.33f)
        {
            _stuckTimer += Time.deltaTime;
        }
        else
        {
            _stuckTimer = 0f;
            _lastStuckCheckPos = transform.position;
        }

        if (_stuckTimer >= stuckTimeThreshold)
        {
            TryRecoverFromStuck();
            _stuckTimer = 0f;
            _lastStuckCheckPos = transform.position;
        }
    }

    /// <summary>
    /// Attempts to unstick the zombie by re-pathing toward the player. Does NOT
    /// warp/teleport the zombie — that looks jarring to the player. Instead it
    /// tries multiple re-path strategies (SetDestination, SetPath, intermediate
    /// waypoint) to get the agent moving again from its current position.
    /// </summary>
    private void TryRecoverFromStuck()
    {
        if (agent == null || target == null) return;

        // Strategy 1: robust re-path directly to the player.
        SetDestinationRobust(target.position);
        if (agent.hasPath) return;

        // Strategy 2: path to an intermediate point halfway to the player,
        // then re-path to the player once the zombie reaches it. This helps
        // when a direct path fails but a shorter leg succeeds.
        Vector3 toPlayer = target.position - transform.position;
        Vector3 midPoint = transform.position + toPlayer * 0.5f;

        UnityEngine.AI.NavMeshHit midHit;
        if (UnityEngine.AI.NavMesh.SamplePosition(midPoint, out midHit, 3f, UnityEngine.AI.NavMesh.AllAreas))
        {
            UnityEngine.AI.NavMeshPath path = new UnityEngine.AI.NavMeshPath();
            if (UnityEngine.AI.NavMesh.CalculatePath(transform.position, midHit.position, UnityEngine.AI.NavMesh.AllAreas, path)
                && path.status == UnityEngine.AI.NavMeshPathStatus.PathComplete)
            {
                agent.SetPath(path);
                return;
            }
        }

        // Strategy 3: path to a random nearby NavMesh position (small offset,
        // NOT a teleport — just a nudge in a different direction to break out
        // of avoidance deadlock).
        for (int attempt = 0; attempt < 3; attempt++)
        {
            Vector2 rand = Random.insideUnitCircle * 2f;
            Vector3 candidate = transform.position + new Vector3(rand.x, 0f, rand.y);

            UnityEngine.AI.NavMeshHit hit;
            if (!UnityEngine.AI.NavMesh.SamplePosition(candidate, out hit, 1.5f, UnityEngine.AI.NavMesh.AllAreas))
                continue;

            UnityEngine.AI.NavMeshPath path = new UnityEngine.AI.NavMeshPath();
            if (UnityEngine.AI.NavMesh.CalculatePath(transform.position, hit.position, UnityEngine.AI.NavMesh.AllAreas, path)
                && path.status == UnityEngine.AI.NavMeshPathStatus.PathComplete)
            {
                agent.SetPath(path);
                return;
            }
        }
    }

    /// <summary>
    /// Robust destination setter that works around a Unity NavMeshAgent issue
    /// where SetDestination returns true but doesn't actually create a path
    /// (happens after the agent's previous path completed while isStopped was
    /// true). Falls back to manually calculating the path and assigning it via
    /// SetPath, which reliably creates a new path.
    /// </summary>
    private void SetDestinationRobust(Vector3 destination)
    {
        if (agent == null || !agent.isOnNavMesh) return;

        agent.isStopped = false;
        agent.SetDestination(destination);

        // If SetDestination didn't create a path (known Unity issue after
        // isStopped toggle + path completion), calculate one manually.
        if (!agent.hasPath)
        {
            UnityEngine.AI.NavMeshPath path = new UnityEngine.AI.NavMeshPath();
            if (UnityEngine.AI.NavMesh.CalculatePath(transform.position, destination, UnityEngine.AI.NavMesh.AllAreas, path)
                && path.status == UnityEngine.AI.NavMeshPathStatus.PathComplete)
            {
                agent.SetPath(path);
            }
        }
    }

    //==================================================
    // LINE OF SIGHT
    //==================================================

    /// <summary>
    /// Checks whether there is an unobstructed line from the zombie's eyes to
    /// the player's eyes. Uses a raycast against sightObstructionMask; the
    /// player and the zombie's own layer are automatically excluded so they
    /// don't block their own LOS check.
    /// </summary>
    private bool HasLineOfSight()
    {
        if (target == null) return false;

        Vector3 start = transform.position + Vector3.up * sightEyeHeight;
        Vector3 end = target.position + Vector3.up * sightEyeHeight;
        Vector3 dir = end - start;
        float dist = dir.magnitude;
        if (dist <= 0.1f) return true;

        // Exclude the player and this zombie from the obstruction mask.
        int mask = sightObstructionMask
            & ~(1 << target.gameObject.layer)
            & ~(1 << gameObject.layer);

        return !Physics.Raycast(start, dir.normalized, dist, mask, QueryTriggerInteraction.Ignore);
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
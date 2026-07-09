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
    public float alertMemoryDuration = 3f;
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

    [Header("Chase Re-path (Responsiveness)")]
    [Tooltip("Khoảng cách tối thiểu (m) player phải di chuyển so với destination cuối cùng trước khi zombie re-path ngay lập tức. Nhỏ hơn = truy cập vị trí mới nhanh hơn nhưng tốn CPU pathfinding hơn.")]
    public float playerMovedRepathThreshold = 1f;
    [Tooltip("Interval tối đa (giây) giữa các lần re-path khi player đứng yên. Re-path ngay khi player di chuyển quá playerMovedRepathThreshold.")]
    public float maxRepathInterval = 0.1f;

    [Header("Direct Steering (Real-time Tracking)")]
    [Tooltip("Khi true, zombie di chuyển thẳng tới vị trí mới nhất của player mỗi frame khi có line-of-sight (không cần re-path). Khi mất LOS, quay lại NavMesh pathfinding.")]
    public bool useDirectSteeringWhenLOS = true;
    [Tooltip("Khoảng cách raycast check tường phía trước khi direct steering, tránh cắm đầu vào tường (m).")]
    public float directSteeringWallCheckDistance = 1.5f;
    [Tooltip("Interval cache LOS check khi direct steering (giây). Nhỏ hơn = responsive hơn nhưng tốn raycast hơn.")]
    public float directSteeringLOSCacheInterval = 0.15f;
    [Tooltip("Sau khi đụng tường khi direct steering, zombie tạm thời dùng NavMesh pathfinding trong bao lâu (giây) trước khi thử direct steering lại. Chống flip-flop cắm đầu vào tường.")]
    public float directSteeringWallCooldown = 1.5f;
    [Tooltip("Chiều cao raycast check tường phía trước ở mức thân (m). Raycast thêm ở mức thấp này để phát hiện chướng ngại vật thấp (xác xe, hàng rào, thùng) mà raycast ở eyeHeight bay qua trên.")]
    public float wallCheckBodyHeight = 0.5f;

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
    private Collider _collider;
    private IDamageable _targetDamageable;

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

    // --- Cached reusable objects (avoid per-frame allocation) ---
    private UnityEngine.AI.NavMeshPath _reusablePath;
    private int _cachedLOSMask = -1;
    private int _cachedLOSCacheKey = -1;

    // --- Direct steering runtime state ---
    private float _losCacheTimer;
    private bool _cachedLOSResult;
    private bool _isDirectSteering;
    private float _directSteeringCooldownTimer; // >0 = NavMesh only (after wall hit)

    // --- Last known position tracking ---
    private Vector3 _lastKnownPlayerPos;
    private bool _hasLastKnownPos;

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
        _collider = GetComponent<Collider>();
        _reusablePath = new UnityEngine.AI.NavMeshPath();

        // Zombies cast shadows (so they are visible on the ground during daytime)
        // but do not receive shadows (GPU saving at high counts).
        var smrs = GetComponentsInChildren<SkinnedMeshRenderer>(true);
        for (int i = 0; i < smrs.Length; i++)
        {
            smrs[i].shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
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
        _lastSetDestination = transform.position;
        pathTimer = maxRepathInterval; // force immediate first re-path
        _directSteeringCooldownTimer = 0f;
        _hasLastKnownPos = false;
        _losCacheTimer = directSteeringLOSCacheInterval; // force LOS recheck

        if (target == null)
        {
            GameObject player =
                GameObject.FindGameObjectWithTag("Player");

            if (player != null)
                target = player.transform;
        }

        // Cache the player's IDamageable once per spawn so AttackHit doesn't
        // call GetComponent every hit (50+ zombies * 1.5s cooldown = ~33 hits/s).
        if (target != null)
            _targetDamageable = target.GetComponent<IDamageable>();
        else
            _targetDamageable = null;

        if (agent != null)
        {
            agent.enabled = true;
            agent.isStopped = false;
            agent.speed = walkSpeed;
            agent.stoppingDistance = attackDistance * 0.5f;
            // Let the agent control rotation during Wander; ChasePlayer overrides
            // via FaceTarget() which is fine since both point toward the player/path.
            agent.updateRotation = true;
            // Performance: HighQuality avoidance is ~quadratic with agent count; use cheap avoidance + varied priority.
            agent.obstacleAvoidanceType = UnityEngine.AI.ObstacleAvoidanceType.LowQualityObstacleAvoidance;
            agent.avoidancePriority = Random.Range(30, 70);

            // UNDERGROUND NAVMESH FIX: 31% of the NavMesh triangles are
            // underground (y=-2 to -1385, from beach/rocks/sewers) and
            // disconnected from the main surface. If the zombie spawns on
            // an underground NavMesh island, it can never reach the player
            // and will slide around forever. Sample a ground-level NavMesh
            // position above the zombie and warp there if the current
            // position is too far below y=0.
            if (transform.position.y < -1f)
            {
                UnityEngine.AI.NavMeshHit groundHit;
                Vector3 searchFrom = new Vector3(transform.position.x, 0f, transform.position.z);
                if (UnityEngine.AI.NavMesh.SamplePosition(searchFrom, out groundHit, 10f, UnityEngine.AI.NavMesh.AllAreas)
                    && groundHit.position.y >= -1f)
                {
                    agent.Warp(groundHit.position);
                    transform.position = groundHit.position;
                }
            }
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

        Collider col = _collider;
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


    private float cachedDistance = 100f;

    void Update()
    {
        if (isDead || target == null)
            return;

        attackTimer += Time.deltaTime;
        wanderTimer += Time.deltaTime;

        // Compute distance every frame — Vector3.Distance is extremely cheap
        // (a few arithmetic ops) and using a stale 0.2s cache causes the zombie
        // to react late when the player moves in/out of detect/attack range.
        cachedDistance = Vector3.Distance(transform.position, target.position);

        // First-time detection requires both distance AND line of sight (if
        // requireLineOfSight is enabled). Hysteresis and alert memory below do
        // NOT require LOS — once the zombie has detected the player, it keeps
        // chasing based on distance/memory even if the player briefly breaks
        // LOS (ducking behind cover, etc.).
        bool hasLOSCurrently = cachedDistance <= detectDistance &&
            (!requireLineOfSight || HasLineOfSight());

        if (hasLOSCurrently)
        {
            // Player within detect range AND visible: chase and (re)arm alert memory.
            // Track last known position for when we lose sight.
            _lastKnownPlayerPos = target.position;
            _hasLastKnownPos = true;
            alertMemoryTimer = alertMemoryDuration;
            _isUsingLastKnownPos = false;
            ChasePlayer(cachedDistance);
        }
        else if (hasDetectedPlayer && cachedDistance <= loseSightDistance)
        {
            // Hysteresis: once detected, keep chasing while player is still
            // within the larger loseSightDistance (prevents flip-flop at the
            // detectDistance edge).
            // Still update last known pos if we can see the player (even if
            // beyond detectDistance, within loseSightDistance).
            if (!requireLineOfSight || HasLineOfSight())
            {
                _lastKnownPlayerPos = target.position;
                _hasLastKnownPos = true;
                _isUsingLastKnownPos = false;
            }
            else
            {
                // Within loseSightDistance but no LOS — use last known pos.
                _isUsingLastKnownPos = _hasLastKnownPos;
            }
            ChasePlayer(cachedDistance);
        }
        else if (alertMemoryTimer > 0f)
        {
            // Lost sight but still in alert memory: chase toward last known
            // position, NOT the player's real position. The zombie doesn't
            // know where the player is — it goes to where it last saw them.
            alertMemoryTimer -= Time.deltaTime;
            _isUsingLastKnownPos = _hasLastKnownPos;
            ChasePlayer(cachedDistance);
        }
        else
        {
            Wander();
            hasDetectedPlayer = false;
            _hasLastKnownPos = false;
            _isUsingLastKnownPos = false;
        }

        animator.SetFloat(
            SpeedHash,
            agent.velocity.magnitude / runSpeed,
            0.15f,
            Time.deltaTime);
    }

    private float pathTimer = 0f;
    private Vector3 _lastSetDestination = Vector3.zero;
    private bool _isUsingLastKnownPos; // true when chasing last known pos (no LOS)

    void ChasePlayer(float distance)
    {
        if (!hasDetectedPlayer)
        {
            PlaySound(growlClip);
            hasDetectedPlayer = true;
        }

        // --- Last known position mode: when we've lost sight of the player,
        // navigate to where we last saw them using NavMesh pathfinding (not
        // direct steering, since we can't see the player). This makes the
        // zombie go around obstacles to check the last known position rather
        // than walking into walls or cheating with the player's real position.
        if (_isUsingLastKnownPos && _hasLastKnownPos)
        {
            if (agent.updateRotation)
                agent.updateRotation = false;

            float distToLastKnown = Vector3.Distance(transform.position, _lastKnownPlayerPos);

            if (distToLastKnown <= attackDistance)
            {
                // Reached last known position but player isn't here.
                // Stop and look around — alert memory will expire and
                // the zombie will return to Wander.
                agent.isStopped = true;
                FaceTarget();
                return;
            }

            // Navigate to last known position via NavMesh (goes around walls).
            agent.isStopped = false;
            SyncAgentToTransform();
            agent.updatePosition = true;
            _isDirectSteering = false;
            agent.speed = runSpeed;

            pathTimer += Time.deltaTime;
            float distToLastDest = Vector3.Distance(_lastKnownPlayerPos, _lastSetDestination);
            if (pathTimer >= maxRepathInterval || distToLastDest > playerMovedRepathThreshold)
            {
                SetDestinationRobust(_lastKnownPlayerPos);
                _lastSetDestination = _lastKnownPlayerPos;
                pathTimer = 0f;
            }

            FaceTarget();
            return;
        }

        // Take over rotation from the agent so FaceTarget() and the agent
        // don't fight over transform.rotation (causes visual sliding when
        // the zombie faces the player while the path moves it sideways).
        if (agent.updateRotation)
            agent.updateRotation = false;

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
                _losCacheTimer = directSteeringLOSCacheInterval; // force LOS recheck
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

            // --- Direct steering: when the zombie has line-of-sight to the
            // player, bypass NavMesh pathfinding and move directly toward the
            // player's CURRENT position every frame. This eliminates the
            // "chasing stale positions" problem when the player moves
            // continuously. When LOS is lost (player behind a wall), fall
            // back to NavMesh pathfinding to navigate around obstacles.
            if (useDirectSteeringWhenLOS && TryDirectSteer(distance, speed))
            {
                // Direct steering handled movement this frame. Skip NavMesh
                // re-pathing and stuck detection (not needed when steering
                // directly). Reset stuck timer since we're moving under our
                // own control.
                _stuckTimer = 0f;
                _lastStuckCheckPos = transform.position;
                return;
            }

            // --- NavMesh pathfinding fallback (no LOS or direct steering disabled) ---
            _isDirectSteering = false;
            agent.isStopped = false;

            // --- Jitter the chase destination so the zombie doesn't run in a straight line ---
            // Only apply jitter when far enough from the player. Near attack range,
            // jitter causes the destination to shift by up to chaseJitterRadius,
            // which combined with stoppingDistance creates a flip-flop: the agent
            // stops at stoppingDistance from the jittered dest (which may be
            // further from the player than attackDistance), then the code sees
            // distance > attackDistance and chases again -> micro-sliding.
            chaseJitterTimer -= Time.deltaTime;
            if (chaseJitterTimer <= 0f)
            {
                // Pick a new lateral jitter offset around the player.
                Vector3 rand = Random.insideUnitSphere * chaseJitterRadius;
                rand.y = 0f;
                chaseJitterOffset = rand;
                chaseJitterTimer = Random.Range(0.4f, 0.9f);
            }

            pathTimer += Time.deltaTime;

            // Re-path when the throttle interval elapses OR the player has
            // moved far enough from the last destination that the current
            // path is stale. This keeps zombies responsive when the player
            // changes direction without flooding the async pathfinding queue.
            float distToLastDest = Vector3.Distance(target.position, _lastSetDestination);
            if (pathTimer >= maxRepathInterval || distToLastDest > playerMovedRepathThreshold)
            {
                // Disable jitter when close to attack range to prevent flip-flop.
                Vector3 dest = target.position;
                if (distance > attackDistance + chaseJitterRadius + 1f)
                    dest += chaseJitterOffset;
                SetDestinationRobust(dest);
                _lastSetDestination = dest;
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

        // Let the agent handle rotation during wander (ChasePlayer disabled it).
        if (!agent.updateRotation)
            agent.updateRotation = true;

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

        // Use the cached IDamageable from OnEnable. Re-resolve only as a fallback
        // (e.g. if the player was swapped at runtime).
        IDamageable damageable = _targetDamageable;
        if (damageable == null && target != null)
        {
            damageable = target.GetComponent<IDamageable>();
            _targetDamageable = damageable;
        }

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
            // Check if the player is currently wall-running. Use the cached
            // target's PlayerMovement instead of FindAnyObjectByType (which
            // scans the whole hierarchy on every zombie death).
            if (target != null)
            {
                var pm = target.GetComponent<PlayerMovement>();
                if (pm != null && pm.IsWallRunning)
                    AchievementManager.Instance.NotifyZombieKillWhileWallRunning();
            }
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

        Collider col = _collider;

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
        // Throttle re-path attempts to every 0.5s to avoid spamming
        // SetDestination every frame (which floods the async pathfinding queue).
        if (agent != null && agent.isOnNavMesh && !agent.hasPath && !agent.pathPending && !agent.isStopped)
        {
            _noPathRetryCount++;
            if (_noPathRetryCount % 30 == 0) // ~0.5s at 60fps
            {
                SetDestinationRobust(target.position);
            }

            // If the agent still has no path after ~5 seconds of retries,
            // it's in a broken state. Try a full agent reset: disable/re-enable/
            // warp to snap it out. This is NOT a teleport to the player — it
            // warps to the zombie's OWN position to force re-initialization.
            if (_noPathRetryCount >= 300 && !agent.hasPath) // ~5s at 60fps
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
            _reusablePath.ClearCorners();
            if (UnityEngine.AI.NavMesh.CalculatePath(transform.position, midHit.position, UnityEngine.AI.NavMesh.AllAreas, _reusablePath)
                && _reusablePath.status == UnityEngine.AI.NavMeshPathStatus.PathComplete)
            {
                agent.SetPath(_reusablePath);
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

            _reusablePath.ClearCorners();
            if (UnityEngine.AI.NavMesh.CalculatePath(transform.position, hit.position, UnityEngine.AI.NavMesh.AllAreas, _reusablePath)
                && _reusablePath.status == UnityEngine.AI.NavMeshPathStatus.PathComplete)
            {
                agent.SetPath(_reusablePath);
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
    ///
    /// IMPORTANT: The fallback (CalculatePath) is only used when the agent
    /// has NO path from a PREVIOUS frame — NOT immediately after
    /// SetDestination. SetDestination is async: it queues a path request but
    /// doesn't compute it synchronously, so hasPath is false on the same frame.
    /// Checking hasPath immediately would trigger CalculatePath every call
    /// (260+ synchronous path calculations per second on a 384k-triangle
    /// NavMesh = frame stalls = stale paths = sliding).
    /// Instead, we only use the fallback when the agent had no path BEFORE
    /// this call (i.e. it was stuck in the post-isStopped no-path state).
    /// </summary>
    private void SetDestinationRobust(Vector3 destination)
    {
        if (agent == null || !agent.isOnNavMesh) return;

        // Remember whether the agent had a path BEFORE this call. If it
        // didn't, SetDestination alone may silently fail (Unity issue after
        // isStopped + path completion), so we need the CalculatePath fallback.
        bool hadNoPath = !agent.hasPath && !agent.pathPending;

        agent.isStopped = false;
        agent.SetDestination(destination);

        // Only use the synchronous CalculatePath fallback when the agent was
        // in the broken no-path state before this call. This avoids running
        // expensive synchronous pathfinding on every SetDestination call.
        if (hadNoPath && !agent.pathPending)
        {
            _reusablePath.ClearCorners();
            if (UnityEngine.AI.NavMesh.CalculatePath(transform.position, destination, UnityEngine.AI.NavMesh.AllAreas, _reusablePath)
                && _reusablePath.status == UnityEngine.AI.NavMeshPathStatus.PathComplete)
            {
                agent.SetPath(_reusablePath);
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

        // Cache the LOS mask — it only changes when the target or this zombie's
        // layer changes (rare). Recompute only if the cached value is stale.
        int myLayer = gameObject.layer;
        int targetLayer = target.gameObject.layer;
        int cacheKey = (myLayer << 8) | targetLayer;
        if (_cachedLOSMask == -1 || _cachedLOSCacheKey != cacheKey)
        {
            _cachedLOSMask = sightObstructionMask
                & ~(1 << targetLayer)
                & ~(1 << myLayer);
            _cachedLOSCacheKey = cacheKey;
        }

        return !Physics.Raycast(start, dir.normalized, dist, _cachedLOSMask, QueryTriggerInteraction.Ignore);
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

        // Reject underground NavMesh positions (disconnected islands at
        // y<-1 that cover 31% of the NavMesh). Search at ground level.
        if (NavMesh.SamplePosition(
            randomDirection,
            out hit,
            distance,
            NavMesh.AllAreas))
        {
            // If the sampled position is underground, try again at ground level.
            if (hit.position.y < -1f)
            {
                Vector3 groundLevel = new Vector3(randomDirection.x, 0f, randomDirection.z);
                if (NavMesh.SamplePosition(groundLevel, out hit, distance, NavMesh.AllAreas)
                    && hit.position.y >= -1f)
                    return hit.position;
            }
            else
            {
                return hit.position;
            }
        }

        // Fallback: return origin if no valid ground-level position found.
        return origin;
    }

    //==================================================
    // DIRECT STEERING
    //==================================================

    /// <summary>
    /// Syncs the NavMeshAgent's internal position to the transform's current
    /// position. Call this BEFORE setting updatePosition = true when
    /// transitioning from direct steering back to NavMesh. Without this, the
    /// agent's position is stale (frozen at the spot where direct steering
    /// started), causing the zombie to "snap back" / retreat to that old
    /// position when the agent resumes position control.
    /// </summary>
    private void SyncAgentToTransform()
    {
        if (agent == null || !agent.isOnNavMesh)
            return;

        // Sample the NavMesh at the transform's current position to get a
        // valid NavMesh point (the transform may be slightly off the surface
        // due to y offset from rigidbody/agent).
        UnityEngine.AI.NavMeshHit hit;
        if (UnityEngine.AI.NavMesh.SamplePosition(transform.position, out hit, 2f, UnityEngine.AI.NavMesh.AllAreas))
        {
            agent.nextPosition = hit.position;
        }
        else
        {
            // Fallback: set directly even if not perfectly on NavMesh.
            agent.nextPosition = transform.position;
        }
    }

    /// <summary>
    /// When the zombie has line-of-sight to the player, moves directly toward
    /// the player's current position every frame (bypassing NavMesh
    /// pathfinding). Returns true if direct steering was used this frame.
    /// Falls back to NavMesh pathfinding when LOS is lost.
    /// </summary>
    private bool TryDirectSteer(float distance, float speed)
    {
        if (target == null || agent == null || !agent.isOnNavMesh)
            return false;

        // Wall cooldown: after hitting a wall while direct steering, stay in
        // NavMesh mode for a while to let the zombie navigate around the
        // obstacle instead of flip-flopping between direct steering and
        // NavMesh (which causes "te le" / sliding along walls).
        if (_directSteeringCooldownTimer > 0f)
        {
            _directSteeringCooldownTimer -= Time.deltaTime;
            if (_isDirectSteering)
            {
                _isDirectSteering = false;
                agent.isStopped = false;
                SyncAgentToTransform();
                agent.updatePosition = true;
                pathTimer = maxRepathInterval; // force re-path
            }
            return false;
        }

        // Cache LOS result to avoid raycasting every frame.
        _losCacheTimer -= Time.deltaTime;
        if (_losCacheTimer <= 0f)
        {
            _cachedLOSResult = HasLineOfSight();
            _losCacheTimer = directSteeringLOSCacheInterval;
        }

        if (!_cachedLOSResult)
        {
            // No LOS — let NavMesh pathfinding handle it.
            if (_isDirectSteering)
            {
                // Transitioning from direct steering back to NavMesh: force
                // an immediate re-path so the zombie doesn't stand still.
                _isDirectSteering = false;
                agent.isStopped = false;
                SyncAgentToTransform();
                agent.updatePosition = true; // restore agent position control
                pathTimer = maxRepathInterval; // force re-path this frame
            }
            return false;
        }

        // --- Direct steering: move toward player's current position ---
        _isDirectSteering = true;
        agent.isStopped = true; // pause NavMeshAgent while we steer manually
        agent.updatePosition = false; // prevent agent from overriding our transform

        Vector3 dir = target.position - transform.position;
        dir.y = 0f;
        float distMag = dir.magnitude;
        if (distMag < 0.01f)
            return true;

        Vector3 dirNorm = dir / distMag;

        // Wall check: if there's an obstacle directly ahead within
        // directSteeringWallCheckDistance, don't steer into it — let
        // NavMesh pathfinding navigate around it instead.
        // Check at BOTH eye height (for tall walls) and body height (for
        // low obstacles like car wrecks, fences, barricades that the eye-
        // level ray would pass over).
        if (Physics.Raycast(
                transform.position + Vector3.up * sightEyeHeight,
                dirNorm,
                out RaycastHit wallHit,
                directSteeringWallCheckDistance,
                _cachedLOSMask,
                QueryTriggerInteraction.Ignore) ||
            Physics.Raycast(
                transform.position + Vector3.up * wallCheckBodyHeight,
                dirNorm,
                out RaycastHit wallHit2,
                directSteeringWallCheckDistance,
                _cachedLOSMask,
                QueryTriggerInteraction.Ignore))
        {
            // Wall ahead — fall back to NavMesh pathfinding and enter
            // cooldown so the zombie navigates around the obstacle instead
            // of flip-flopping back to direct steering next frame.
            _isDirectSteering = false;
            agent.isStopped = false;
            SyncAgentToTransform();
            agent.updatePosition = true; // restore agent position control
            pathTimer = maxRepathInterval; // force re-path
            _directSteeringCooldownTimer = directSteeringWallCooldown;
            return false;
        }

        // Move directly toward the player. Use NavMesh.SamplePosition to
        // keep the zombie on the NavMesh surface (avoid sliding off edges).
        // Use a generous sample radius (2m) because the zombie's transform
        // may be above the NavMesh surface (y offset from agent/rigidbody).
        Vector3 newPos = transform.position + dirNorm * speed * Time.deltaTime;

        UnityEngine.AI.NavMeshHit navHit;
        if (UnityEngine.AI.NavMesh.SamplePosition(newPos, out navHit, 2f, UnityEngine.AI.NavMesh.AllAreas))
        {
            // Only move if the sampled position is at a reasonable height
            // (avoid snapping to underground NavMesh islands).
            if (navHit.position.y > -1f)
            {
                transform.position = navHit.position;
            }
        }

        // Update animator speed parameter so the run animation plays.
        if (animator != null)
            animator.SetFloat(SpeedHash, speed);

        return true;
    }
}
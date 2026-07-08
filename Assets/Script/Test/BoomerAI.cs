using UnityEngine;
using UnityEngine.AI;
using cowsins;

public class BoomerAI : MonoBehaviour, IDamageable, ISpecialEnemy, IEnemyHealthReadout
{
    [Header("Player")]
    public Transform target;

    [Header("Health")]
    public int maxHealth = 100;
    public int currentHealth;

    [Header("Detection")]
    public float detectRange = 20f;

    [Header("Line of Sight")]
    [Tooltip("If true, the boomer requires an unobstructed line of sight to the player before detection. Once chasing, it keeps chasing while within detectRange regardless of LOS.")]
    public bool requireLineOfSight = true;
    [Tooltip("Layer mask for objects that block the boomer's sight (walls, floors, furniture). Defaults to everything; the player and the boomer's own layer are automatically excluded.")]
    public LayerMask sightObstructionMask = ~0;
    [Tooltip("Eye height offset from the boomer's pivot for the LOS raycast.")]
    public float sightEyeHeight = 1.5f;

    [Header("Movement")]
    public float moveSpeed = 2f;
    public float rotationSpeed = 10f;

    [Header("Stuck Recovery")]
    [Tooltip("How long (seconds) the boomer must be nearly stationary while chasing before it is considered stuck. Higher = more patient.")]
    public float stuckTimeThreshold = 3f;
    [Tooltip("If the boomer moves less than this distance (meters) over stuckTimeThreshold, it is considered stuck.")]
    public float stuckMoveThreshold = 1f;
    [Tooltip("How far to search for an intermediate re-path position when stuck (no teleport — just re-pathing).")]
    public float stuckRepathRadius = 5f;

    [Header("Explosion")]
    public float explodeRange = 3f;
    [Tooltip("Thời gian cảnh báo (scream) trước khi nổ. Đặt rất nhỏ (vd 0.1s) để boomer gần như nổ ngay lập tức khi lại gần player.")]
    public float screamDuration = 0.1f;

    [Header("Explosion Damage")]
    public float explosionDamage = 50f;
    public float explosionRadius = 5f;

    [Header("Effects")]
    public GameObject explosionPrefab;

    [Header("Acid Pools")]
    public GameObject acidPoolSelfExplodePrefab;
    public GameObject acidPoolDeathPrefab;

    [Header("Acid Pool Lifetime")]
    public float acidPoolLifetime = 10f;

    [Header("Prefab Reference for Pooling")]
    public GameObject prefab;

    [Header("Rewards (granted only when the player shoots it down)")]
    public float experienceReward = 60f;
    public int coinReward = 25;

    [Header("Loot (dropped only when the player shoots it down)")]
    [Tooltip("Loot table: mỗi entry roll độc lập, có thể rơi 0..N loại cùng lúc.")]
    public LootDropEntry[] lootTable;
    [Tooltip("Fallback khi lootTable trống: loot đơn lẻ theo dropChance.")]
    public GameObject dropPrefab;
    [Range(0, 100)]
    public float dropChance = 100f;
    [Tooltip("Khoảng cách nâng loot lên so với vị trí boomer khi rớt xuống.")]
    public float dropHeightOffset = 1.5f;
    [Tooltip("Bật hiệu ứng loot nhảy ra khỏi boomer khi chết.")]
    public bool popLootOnDeath = true;
    [Tooltip("Vận tốc đứng (lên) khi loot bị bắn ra (m/s).")]
    public float lootPopUpwardSpeed = 5f;
    [Tooltip("Vận tốc ngang tối đa khi loot bị bắn ra (m/s).")]
    public float lootPopHorizontalSpeed = 3.5f;

    [Header("Loot Trail Effect")]
    [Tooltip("Cấu hình vệt trail + glow particle khi loot bay. Chỉnh trực tiếp trên boomer.")]
    public LootTrailSettings lootTrailSettings = new LootTrailSettings();

    [Header("Self-Contained Timing (independent of animation events)")]
    [Tooltip("Delay after the Explode trigger before the blast actually fires.")]
    public float explodeFxDelay = 0.25f;
    [Tooltip("Delay after the Explode trigger before the corpse is cleaned up.")]
    public float cleanupDelay = 2.5f;

    private static Collider[] overlapColliders = new Collider[500];

    // ---- IEnemyHealthReadout (read-only; for EnemyHealthBar) ----
    public float HealthFraction
    {
        get { return maxHealth > 0 ? Mathf.Clamp01((float)currentHealth / maxHealth) : 0f; }
    }
    public bool IsDead { get { return isDead; } }
    public event System.Action<float> OnHealthChanged;

    private bool hasExploded;
    private bool hasDestroyed;
    private bool rewardsGranted;
    private bool isWaitingForExplosion;
    private float explosionWaitTimer;

    private Animator animator;
    private NavMeshAgent agent;
    private Rigidbody rb;
    private Collider col;

    private bool isDead;
    private bool isHit;
    private bool isScreaming;
    private bool hasStartedExplosion;

    // --- Stuck recovery runtime state ---
    private Vector3 _lastStuckCheckPos;
    private float _stuckTimer;
    private int _noPathRetryCount;
    private float _noPathRetryTimer;

    // --- Cached reusable objects (avoid per-frame allocation) ---
    private UnityEngine.AI.NavMeshPath _reusablePath;
    private int _cachedLOSMask = -1;
    private int _cachedLOSCacheKey = -1;

    // --- Cached animator parameter hashes (avoid per-call string hashing) ---
    private static readonly int SpeedHash = Animator.StringToHash("Speed");
    private static readonly int HitHash = Animator.StringToHash("Hit");
    private static readonly int IsWarningHash = Animator.StringToHash("isWarning");
    private static readonly int ExplodeHash = Animator.StringToHash("Explode");

    private enum ExplosionType
    {
        SelfExplode,
        Killed
    }

    private ExplosionType explosionType;

    private void Start()
    {
        currentHealth = maxHealth;

        animator = GetComponentInChildren<Animator>();
        agent = GetComponent<NavMeshAgent>();
        rb = GetComponent<Rigidbody>();
        col = GetComponent<Collider>();
        _reusablePath = new UnityEngine.AI.NavMeshPath();

        FindPlayer();

        if (agent != null)
        {
            agent.speed = moveSpeed;
            agent.acceleration = 20f;
            agent.angularSpeed = 300f;
            agent.stoppingDistance = explodeRange;
            agent.updateRotation = false;
            // Performance: use cheap avoidance so Boomers don't overload the
            // avoidance system when many regular zombies are also active.
            agent.obstacleAvoidanceType = UnityEngine.AI.ObstacleAvoidanceType.LowQualityObstacleAvoidance;
            agent.avoidancePriority = 15; // Special enemy — slightly higher priority than regular zombies.

            // UNDERGROUND NAVMESH FIX: 31% of the NavMesh triangles are
            // underground (y=-2 to -1385) and disconnected from the main
            // surface. If the boomer spawns on an underground NavMesh island,
            // it can never reach the player. Sample a ground-level NavMesh
            // position above the boomer and warp there if the current
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
        // agent on stairs/slopes/bridges (Ch5 = ApartmentBridge): gravity pulls
        // the Boomer down, the capsule collider catches on step edges, and the
        // two systems jitter — the Boomer slides in one direction. Kinematic
        // mode lets the agent drive position smoothly while still sending
        // collision events (for hit detection, triggers, etc.).
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.useGravity = true; // kept on so the body can fall if the agent is disabled later.
        }

        if (animator != null)
        {
            animator.speed =
                Random.Range(0.95f, 1.05f);
        }

        _stuckTimer = 0f;
        _lastStuckCheckPos = transform.position;
        _noPathRetryCount = 0;
    }

    private void Update()
    {
        if (isDead)
            return;

        // Poll the death animation and fire the explosion when it is nearly
        // finished, so the blast syncs with the end of the animation instead
        // of firing too early (cutting the animation short).
        if (isWaitingForExplosion && !hasExploded)
        {
            TryFireExplosionFromAnimation();
        }

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

        if (isHit)
            return;

        float distance =
            Vector3.Distance(
                transform.position,
                target.position
            );

        FaceTarget();

        //--------------------------------
        // CHASE
        //--------------------------------

        if (!isScreaming &&
            distance <= detectRange &&
            (!requireLineOfSight || HasLineOfSight()))
        {
            agent.isStopped = false;

            SetDestinationRobust(
                target.position
            );

            // Stuck detection: if the boomer isn't making progress toward the
            // player while it should be chasing, try to recover.
            HandleStuckDetection(distance);
        }
        else
        {
            // Not chasing — reset stuck detection state.
            _stuckTimer = 0f;
            _lastStuckCheckPos = transform.position;
        }

        //--------------------------------
        // SELF EXPLODE
        //--------------------------------

        if (!hasStartedExplosion &&
            distance <= explodeRange)
        {
            explosionType =
                ExplosionType.SelfExplode;

            StartExplosion();
        }

        //--------------------------------
        // ANIMATION SPEED
        //--------------------------------

        float speed =
            agent.velocity.magnitude /
            moveSpeed;

        animator.SetFloat(
            SpeedHash,
            speed,
            0.2f,
            Time.deltaTime
        );
    }

    private float findPlayerTimer;
    private static Transform cachedPlayerTransform;

    private void FindPlayer()
    {
        if (cachedPlayerTransform != null)
        {
            target = cachedPlayerTransform;
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
        }
    }

    //==================================================
    // STUCK DETECTION & RECOVERY
    //==================================================

    /// <summary>
    /// Tracks whether the boomer is actually making progress toward the player.
    /// If it stays nearly stationary for too long while chasing, it tries to
    /// recover by snapping to a nearby valid NavMesh position and re-pathing.
    /// </summary>
    private void HandleStuckDetection(float distanceToPlayer)
    {
        // Only check for stuck while actively chasing (far enough to need
        // movement). If we're in explode range, being stationary is expected.
        if (distanceToPlayer <= explodeRange + 0.5f)
        {
            _stuckTimer = 0f;
            _lastStuckCheckPos = transform.position;
            _noPathRetryCount = 0;
            return;
        }

        // CRITICAL: If the agent has no path while it should be chasing, it
        // will stand forever without moving. Force a robust re-path, but
        // throttle to every 0.5s to avoid spamming SetDestination every frame
        // (which floods the async pathfinding queue and can cause frame stalls
        // on a large NavMesh).
        if (agent != null && agent.isOnNavMesh && !agent.hasPath && !agent.pathPending && !agent.isStopped)
        {
            _noPathRetryCount++;
            _noPathRetryTimer += Time.deltaTime;
            if (_noPathRetryTimer >= 0.5f)
            {
                SetDestinationRobust(target.position);
                _noPathRetryTimer = 0f;
            }

            // If the agent still has no path after ~5 seconds of retries,
            // it's in a broken state. Try a full agent reset: disable/re-enable/
            // warp to snap it out. This is NOT a teleport to the player — it
            // warps to the boomer's OWN position to force re-initialization.
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
                _noPathRetryTimer = 0f;
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
    /// Attempts to unstick the boomer by finding a valid NavMesh position nearby
    /// (with a complete path to the player) and warping there. If no valid
    /// position is found, the agent is simply re-pathed as a fallback.
    /// </summary>
    /// <summary>
    /// Attempts to unstick the boomer by re-pathing toward the player. Does NOT
    /// warp/teleport — only re-paths from the current position.
    /// </summary>
    private void TryRecoverFromStuck()
    {
        if (agent == null || target == null) return;

        // Strategy 1: robust re-path directly to the player.
        SetDestinationRobust(target.position);
        if (agent.hasPath) return;

        // Strategy 2: path to an intermediate point halfway to the player.
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

        // Strategy 3: path to a random nearby NavMesh position (small nudge,
        // NOT a teleport).
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
    /// (synchronous pathfinding on every frame = frame stalls = sliding).
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
    /// Checks whether there is an unobstructed line from the boomer's eyes to
    /// the player's eyes. The player and the boomer's own layer are
    /// automatically excluded from the obstruction mask.
    /// </summary>
    private bool HasLineOfSight()
    {
        if (target == null) return false;

        Vector3 start = transform.position + Vector3.up * sightEyeHeight;
        Vector3 end = target.position + Vector3.up * sightEyeHeight;
        Vector3 dir = end - start;
        float dist = dir.magnitude;
        if (dist <= 0.1f) return true;

        // Cache the LOS mask — it only changes when the target or this boomer's
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

    private void FaceTarget()
    {
        if (target == null)
            return;

        Vector3 dir =
            target.position -
            transform.position;

        dir.y = 0f;

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

    //==================================
    // EXPLOSION SEQUENCE
    //==================================

    private void StartExplosion()
    {
        if (hasStartedExplosion)
            return;

        hasStartedExplosion = true;

        isScreaming = true;

        if (agent != null)
        {
            agent.isStopped = true;
        }

        animator.SetBool(
            IsWarningHash,
            true
        );

        Invoke(
            nameof(PlayDeath),
            screamDuration
        );
    }

    private void PlayDeath()
    {
        animator.SetBool(
            IsWarningHash,
            false
        );

        animator.SetTrigger(
            ExplodeHash
        );

        // The explosion is fired from Update() by polling the death animation
        // state so the blast syncs with the end of the animation instead of
        // firing at a fixed delay that may cut the animation short.
        isWaitingForExplosion = true;
        explosionWaitTimer = 0f;
        Invoke(nameof(DestroyEvent), cleanupDelay);
    }

    /// <summary>
    /// Polls the animator for the Death/Death 0 state and fires the explosion
    /// when the death animation is nearly finished (>= 90% normalized time).
    /// Falls back to a timed delay if the death state is never entered
    /// (e.g. animator missing or transition blocked).
    /// </summary>
    private void TryFireExplosionFromAnimation()
    {
        explosionWaitTimer += Time.deltaTime;

        if (animator == null)
        {
            ExplosionEvent();
            isWaitingForExplosion = false;
            return;
        }

        AnimatorStateInfo state = animator.GetCurrentAnimatorStateInfo(0);

        if (state.IsName("Death") || state.IsName("Death 0"))
        {
            if (state.normalizedTime >= 0.9f)
            {
                ExplosionEvent();
                isWaitingForExplosion = false;
            }
        }
        else if (explosionWaitTimer >= explodeFxDelay + 2f)
        {
            // Fallback: if we never entered the Death state within a reasonable
            // window, fire the explosion anyway so the boomer always explodes.
            ExplosionEvent();
            isWaitingForExplosion = false;
        }
    }

    //==================================
    // DAMAGE
    //==================================

    public void Damage(
        float damage,
        bool isHeadshot
    )
    {
        if (CombatFeedbackHUD.Instance != null)
            CombatFeedbackHUD.Instance.ShowHit(transform.position, damage, isHeadshot);

        TakeDamage(
            Mathf.RoundToInt(damage)
        );
    }

    public void TakeDamage(int damage)
    {
        // Once it has begun screaming/exploding, further hits no longer matter.
        if (isDead || hasStartedExplosion)
            return;

        currentHealth -= damage;

        if (PlayerStatsTracker.Instance != null)
            PlayerStatsTracker.Instance.RegisterDamageDealt(damage);

        animator.SetTrigger(HitHash);

        if (OnHealthChanged != null)
            OnHealthChanged(HealthFraction);

        if (currentHealth <= 0)
        {
            explosionType =
                ExplosionType.Killed;

            GrantKillRewards();

            StartExplosion();
        }
    }

    // The player shot it down before it reached them: award bonus progression.
    // Self-detonation (reaching the player) grants nothing on purpose.
    private void GrantKillRewards()
    {
        if (rewardsGranted)
            return;

        rewardsGranted = true;

        if (CombatFeedbackHUD.Instance != null)
            CombatFeedbackHUD.Instance.ShowKill("Boomer");

        if (AIDirector.Instance != null)
            AIDirector.Instance.RegisterKill();

        if (PlayerStatsTracker.Instance != null)
            PlayerStatsTracker.Instance.RegisterBoomerKill();

        if (ScoreManager.Instance != null)
            ScoreManager.Instance.AddKill();

        if (ExperienceManager.Instance != null && ExperienceManager.Instance.useExperience)
            ExperienceManager.Instance.AddExperience(experienceReward);

        if (CoinManager.Instance != null && CoinManager.Instance.useCoins)
            CoinManager.Instance.AddCoins(coinReward);
    }

    //==================================
    // ANIMATION EVENTS
    //==================================

    // Gọi tại frame nổ
    public void ExplosionEvent()
    {
        if (hasExploded)
            return;

        hasExploded = true;

        // Achievement tracking: check if the player was close to the explosion.
        if (AchievementManager.Instance != null)
            AchievementManager.Instance.NotifyBoomerCloseExplosion(transform.position);

        //--------------------------------
        // VFX
        //--------------------------------

        if (explosionPrefab != null)
        {
            cowsins.PoolManager.Instance.GetFromPool(
                explosionPrefab,
                transform.position,
                Quaternion.identity
            );
        }

        //--------------------------------
        // ACID POOL
        //--------------------------------

        GameObject acidPool = null;

        if (explosionType ==
            ExplosionType.Killed)
        {
            if (acidPoolDeathPrefab != null)
            {
                acidPool = cowsins.PoolManager.Instance.GetFromPool(
                    acidPoolDeathPrefab,
                    transform.position,
                    Quaternion.identity,
                    acidPoolLifetime
                );
            }
        }
        else
        {
            if (acidPoolSelfExplodePrefab != null)
            {
                acidPool = cowsins.PoolManager.Instance.GetFromPool(
                    acidPoolSelfExplodePrefab,
                    transform.position,
                    Quaternion.identity,
                    acidPoolLifetime
                );
            }
        }

        //--------------------------------
        // DAMAGE
        //--------------------------------

        int numHits = Physics.OverlapSphereNonAlloc(
                transform.position,
                explosionRadius,
                overlapColliders
            );

        for (int i = 0; i < numHits; i++)
        {
            Collider hit = overlapColliders[i];
            if (hit.transform == transform)
                continue;

            IDamageable damageable =
                hit.GetComponentInParent<IDamageable>();

            if (damageable != null)
            {
                damageable.Damage(
                    explosionDamage,
                    false
                );

                // Feed the directional damage indicator when the blast hits the player.
                if (hit.CompareTag("Player") && DamageDirectionHUD.Instance != null)
                    DamageDirectionHUD.Instance.ShowDamageFrom(transform.position);
            }
        }
    }

    // Gọi ở frame cuối animation Death
    public void DestroyEvent()
    {
        if (hasDestroyed)
            return;

        hasDestroyed = true;

        isDead = true;

        if (agent != null)
        {
            agent.enabled = false;
        }

        // Chỉ rơi loot khi player bắn chết (Killed), không rơi khi tự nổ (SelfExplode).
        if (explosionType == ExplosionType.Killed)
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

        if (prefab != null)
        {
            cowsins.PoolManager.Instance.ReturnToPool(gameObject, prefab);
        }
        else
        {
            Destroy(gameObject);
        }
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
            explodeRange
        );

        Gizmos.color = Color.green;

        Gizmos.DrawWireSphere(
            transform.position,
            explosionRadius
        );
    }
}

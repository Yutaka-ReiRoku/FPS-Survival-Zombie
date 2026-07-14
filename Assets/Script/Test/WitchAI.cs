using UnityEngine;
using UnityEngine.AI;
using cowsins;

/// <summary>
/// The Witch — một boss đặc biệt đại diện cho người mẹ mất con.
/// Bình thường ngồi khóc nỉ non một mình (Cry state). Khi bị tác động
/// (player lại gần provokeRadius, hoặc bị bắn trúng), cô ta gào lên
/// rồi lao vào người chơi với tốc độ cao (Scream → Chase). Nếu mất dấu
/// player, quay về Cry state. Tầm gần tấn công gây damage lớn.
///
/// Stats mặc định theo ý tưởng: máu 35, tốc độ 5 m/s, dame 40.
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(AudioSource))]
public class WitchAI : MonoBehaviour, IDamageable, ISpecialEnemy, IEnemyHealthReadout
{
    // ---- IEnemyHealthReadout (read-only; for EnemyHealthBar) ----
    public float HealthFraction
    {
        get { return maxHealth > 0 ? Mathf.Clamp01((float)currentHealth / maxHealth) : 0f; }
    }
    public bool IsDead { get { return isDead; } }
    public event System.Action<float> OnHealthChanged;

    [Header("Target")]
    public Transform target;

    [Header("Health")]
    public int maxHealth = 35;
    public int currentHealth;

    [Header("Movement")]
    public float runSpeed = 5f;
    public float rotationSpeed = 10f;

    [Header("Detection")]
    [Tooltip("Khoảng cách player lại gần để provoke Witch (chuyển từ Cry sang Scream).")]
    public float provokeRadius = 12f;
    [Tooltip("Khoảng cách mất dấu player. Lớn hơn provokeRadius để chống flip-flop.")]
    public float loseSightDistance = 20f;

    [Header("Line of Sight")]
    [Tooltip("If true, Witch yêu cầu LOS rõ ràng đến player trước khi provoke bằng proximity.")]
    public bool requireLineOfSight = true;
    [Tooltip("Layer mask cho vật cản tầm nhìn (tường,家具). Mặc định tất cả.")]
    public LayerMask sightObstructionMask = ~0;
    [Tooltip("Chiều cao mắt từ pivot cho LOS raycast.")]
    public float sightEyeHeight = 1.5f;

    [Header("Attack")]
    public float attackRange = 1.5f;
    public float attackCooldown = 1.0f;
    public float attackDamage = 40f;
    [Tooltip("Delay sau trigger Attack trước khi áp dụng damage (sync với animation hit frame).")]
    public float damageApplyDelay = 0.35f;

    [Header("Scream")]
    [Tooltip("Thời gian gào trước khi bắt đầu lao vào player (giây).")]
    public float screamDuration = 1.2f;

    [Header("Stuck Recovery")]
    public float stuckTimeThreshold = 3f;
    public float stuckMoveThreshold = 1f;
    public float stuckRepathRadius = 5f;

    [Header("Chase Re-path (Responsiveness)")]
    [Tooltip("Khoảng cách tối thiểu (m) player phải di chuyển so với destination cuối cùng trước khi Witch re-path ngay lập tức.")]
    public float playerMovedRepathThreshold = 1f;
    [Tooltip("Interval tối đa (giây) giữa các lần re-path khi player đứng yên.")]
    public float maxRepathInterval = 0.1f;

    [Header("Audio")]
    [Tooltip("Âm thanh khóc lặp lại khi Cry (loop).")]
    public AudioClip cryClip;
    [Tooltip("Âm thanh gào khi provoke.")]
    public AudioClip screamClip;
    [Tooltip("Âm thanh khi tấn công.")]
    public AudioClip attackClip;
    [Tooltip("Âm thanh khi chết.")]
    public AudioClip deathClip;

    [Header("Loot")]
    [Tooltip("Loot table: mỗi entry roll độc lập, có thể rơi 0..N loại cùng lúc.")]
    public LootDropEntry[] lootTable;
    [Tooltip("Fallback khi lootTable trống: loot đơn lẻ theo dropChance.")]
    public GameObject dropPrefab;
    [Range(0, 100)]
    public float dropChance = 100f;
    [Tooltip("Khoảng cách nâng loot lên so với vị trí Witch khi rớt xuống.")]
    public float dropHeightOffset = 1.5f;
    [Tooltip("Bật hiệu ứng loot nhảy ra khỏi Witch khi chết.")]
    public bool popLootOnDeath = true;
    [Tooltip("Vận tốc đứng (lên) khi loot bị bắn ra (m/s).")]
    public float lootPopUpwardSpeed = 5f;
    [Tooltip("Vận tốc ngang tối đa khi loot bị bắn ra (m/s).")]
    public float lootPopHorizontalSpeed = 3f;

    [Header("Loot Trail Effect")]
    [Tooltip("Cấu hình vệt trail + glow particle khi loot bay.")]
    public LootTrailSettings lootTrailSettings = new LootTrailSettings();

    [Header("Quest")]
    [Tooltip("QuestTrigger (Manual mode) sẽ được complete khi Witch chết. Để null nếu không dùng quest.")]
    public QuestTrigger questTriggerOnDeath;

    [Header("Rewards")]
    public float experienceReward = 80f;
    public int coinReward = 30;

    [Header("Cleanup")]
    [Tooltip("Thời gian sau khi chết trước khi destroy GameObject (giây).")]
    public float cleanupDelay = 6f;

    [Header("Debug/Testing")]
    [SerializeField] private bool forceDetectPlayer = false;

    private Animator animator;
    private NavMeshAgent agent;
    private Rigidbody rb;
    private Collider col;
    private AudioSource audioSource;
    private EnemyLocomotion locomotion;
    private PlayerStats _targetStats;

    // --- State ---
    private enum WitchState { Crying, Screaming, Chasing, Attacking, Dead }
    private WitchState state = WitchState.Crying;
    private bool isDead;
    private bool hasScreamed;
    private bool isAttacking;
    private float attackTimer;
    private float _pathTimer;
    private Vector3 _lastSetDestination = Vector3.zero;

    // --- Timers (replacing Invoke for reliability) ---
    private float screamTimer;
    private bool screamTimerActive;
    private float attackDamageTimer;
    private bool attackDamageTimerActive;
    private float attackResetTimer;
    private bool attackResetTimerActive;

    // --- Cry audio loop ---
    private bool cryAudioPlaying;

    // --- Cached animator parameter hashes ---
    private static readonly int SpeedHash = Animator.StringToHash("Speed");
    private static readonly int ScreamHash = Animator.StringToHash("Scream");
    private static readonly int AttackHash = Animator.StringToHash("Attack");
    private static readonly int IsChasingHash = Animator.StringToHash("isChasing");
    private static readonly int IsCryingHash = Animator.StringToHash("isCrying");
    private static readonly int IsDeathHash = Animator.StringToHash("isDeath");
    private static readonly int DeathHash = Animator.StringToHash("Death");
    private static readonly int HitHash = Animator.StringToHash("Hit");
    private static readonly int AttackResetHash = Animator.StringToHash("attackReset");

    // --- Cached player references ---
    private float findPlayerTimer;
    private static Transform cachedPlayerTransform;
    private IDamageable cachedPlayerDamageable;

    private void Awake()
    {
        animator = GetComponentInChildren<Animator>();
        agent = GetComponent<NavMeshAgent>();
        rb = GetComponent<Rigidbody>();
        col = GetComponent<Collider>();
        audioSource = GetComponent<AudioSource>();
        locomotion = GetComponent<EnemyLocomotion>();
        if (locomotion == null)
            locomotion = gameObject.AddComponent<EnemyLocomotion>();
    }

    private void Start()
    {
        currentHealth = maxHealth;
        FindPlayer();

        if (agent != null)
        {
            agent.speed = runSpeed;
            agent.acceleration = 20f;
            agent.angularSpeed = 300f;
            agent.stoppingDistance = attackRange * 0.5f;
            agent.updateRotation = false;
            agent.obstacleAvoidanceType = ObstacleAvoidanceType.LowQualityObstacleAvoidance;
            agent.avoidancePriority = 8; // Boss — high priority, regular zombies yield.
        }

        if (rb != null)
        {
            rb.isKinematic = true;
            rb.useGravity = true;
        }

        if (animator != null)
        {
            animator.SetBool(IsCryingHash, true);
            animator.SetBool(IsChasingHash, false);
            animator.speed = Random.Range(0.95f, 1.05f);
        }

        if (locomotion != null)
        {
            locomotion.target = target;
            locomotion.requireLineOfSight = requireLineOfSight;
            locomotion.sightObstructionMask = sightObstructionMask;
            locomotion.sightEyeHeight = sightEyeHeight;
            locomotion.stuckTimeThreshold = stuckTimeThreshold;
            locomotion.stuckMoveThreshold = stuckMoveThreshold;
            locomotion.stuckRepathRadius = stuckRepathRadius;
            locomotion.playerMovedRepathThreshold = playerMovedRepathThreshold;
            locomotion.maxRepathInterval = maxRepathInterval;
            locomotion.Initialize();
        }

        state = WitchState.Crying;
        hasScreamed = false;
        _lastSetDestination = transform.position;
        _pathTimer = maxRepathInterval;
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

        if (locomotion != null)
            locomotion.target = target;

        attackTimer += Time.deltaTime;

        float distance = Vector3.Distance(transform.position, target.position);

        // Check if player is dead
        if (_targetStats != null && _targetStats.IsDead)
        {
            if (state == WitchState.Chasing || state == WitchState.Attacking)
            {
                ReturnToCrying();
            }
            return;
        }

        // Tick timers (replacing Invoke for reliability)
        TickTimers();

        switch (state)
        {
            case WitchState.Crying:
                UpdateCrying(distance);
                break;
            case WitchState.Screaming:
                // Screaming state is handled by screamTimer in TickTimers.
                FaceTarget();
                break;
            case WitchState.Chasing:
                UpdateChasing(distance);
                break;
            case WitchState.Attacking:
                // Attacking is handled by attackDamageTimer/attackResetTimer in TickTimers.
                FaceTarget();
                break;
        }

        // Update animator speed parameter
        if (animator != null)
        {
            float targetAnimSpeed = agent != null && agent.isOnNavMesh ? agent.velocity.magnitude / runSpeed : 0f;
            animator.SetFloat(SpeedHash, targetAnimSpeed, 0.1f, Time.deltaTime);
        }
    }

    //==================================================
    // TIMERS (replacing Invoke for reliability)
    //==================================================

    private void TickTimers()
    {
        if (screamTimerActive)
        {
            screamTimer += Time.deltaTime;
            if (screamTimer >= screamDuration)
            {
                screamTimerActive = false;
                EndScream();
            }
        }

        if (attackDamageTimerActive)
        {
            attackDamageTimer += Time.deltaTime;
            if (attackDamageTimer >= damageApplyDelay)
            {
                attackDamageTimerActive = false;
                ApplyAttackDamage();
            }
        }

        if (attackResetTimerActive)
        {
            attackResetTimer += Time.deltaTime;
            if (attackResetTimer >= attackCooldown)
            {
                attackResetTimerActive = false;
                ResetAttack();
            }
        }
    }

    //==================================================
    // CRYING STATE
    //==================================================

    private void UpdateCrying(float distance)
    {
        // Play cry audio as a looping clip so audioSource.Stop() can cut it
        if (!cryAudioPlaying && cryClip != null)
        {
            audioSource.clip = cryClip;
            audioSource.loop = true;
            audioSource.Play();
            cryAudioPlaying = true;
        }

        // Provoke by proximity
        if (distance <= provokeRadius)
        {
            bool hasLOS = !requireLineOfSight || HasLineOfSight();
            if (hasLOS)
            {
                StartScream();
                return;
            }
        }

        // Force detect for testing
        if (forceDetectPlayer)
        {
            StartScream();
            return;
        }
    }

    //==================================================
    // CHASING STATE
    //==================================================

    private void UpdateChasing(float distance)
    {
        // Lost sight — return to crying
        if (distance > loseSightDistance)
        {
            ReturnToCrying();
            return;
        }

        // In attack range and cooldown ready
        if (!isAttacking && distance <= attackRange && attackTimer >= attackCooldown)
        {
            StartAttack();
            return;
        }

        // Chase: re-path responsively
        if (!isAttacking)
        {
            agent.isStopped = false;

            _pathTimer += Time.deltaTime;
            float distToLastDest = Vector3.Distance(target.position, _lastSetDestination);
            bool canRepath = agent != null && !agent.pathPending &&
                (locomotion == null || !locomotion.IsRecoveringFromStuck || !agent.hasPath);

            // Dynamic re-path interval based on distance and LOS
            bool hasLOS = HasLineOfSight();
            float dynamicInterval = Mathf.Lerp(0.15f, 1.0f, Mathf.Clamp01((distance - 5f) / 15f));
            float dynamicThreshold = Mathf.Lerp(1.0f, 5.0f, Mathf.Clamp01((distance - 5f) / 15f));
            if (!hasLOS)
            {
                dynamicInterval *= 2.0f;
                dynamicThreshold *= 1.5f;
            }

            if (canRepath && (_pathTimer >= dynamicInterval || distToLastDest > dynamicThreshold))
            {
                SetDestinationRobust(target.position);
                _lastSetDestination = target.position;
                _pathTimer = 0f;
            }

            if (locomotion != null)
                locomotion.HandleStuckDetection(distance, attackRange * 0.5f);

            if (distance > attackRange)
                FaceMovementDirection();
            else
                FaceTarget();
        }
    }

    //==================================================
    // SCREAM
    //==================================================

    private void StartScream()
    {
        if (hasScreamed)
            return;

        hasScreamed = true;
        state = WitchState.Screaming;

        // Stop looping cry audio so it doesn't overlap with the scream
        if (cryAudioPlaying)
        {
            audioSource.Stop();
            audioSource.clip = null;
            audioSource.loop = false;
            cryAudioPlaying = false;
        }

        if (agent != null)
            agent.isStopped = true;

        if (animator != null)
        {
            animator.SetBool(IsCryingHash, false);
            animator.SetTrigger(ScreamHash);
        }

        if (screamClip != null)
            audioSource.PlayOneShot(screamClip);

        // Start scream timer (replacing Invoke for reliability)
        screamTimer = 0f;
        screamTimerActive = true;
    }

    private void EndScream()
    {
        state = WitchState.Chasing;

        if (animator != null)
            animator.SetBool(IsChasingHash, true);

        if (agent != null)
            agent.isStopped = false;

        _pathTimer = maxRepathInterval; // force immediate first re-path
    }

    //==================================================
    // ATTACK
    //==================================================

    private void StartAttack()
    {
        isAttacking = true;
        state = WitchState.Attacking;
        attackTimer = 0;

        if (agent != null)
            agent.isStopped = true;

        if (animator != null)
            animator.SetTrigger(AttackHash);

        if (attackClip != null)
            audioSource.PlayOneShot(attackClip);

        // Start attack timers (replacing Invoke for reliability)
        attackDamageTimer = 0f;
        attackDamageTimerActive = true;
        attackResetTimer = 0f;
        attackResetTimerActive = true;
    }

    private void ApplyAttackDamage()
    {
        if (isDead || target == null)
            return;

        float distance = Vector3.Distance(transform.position, target.position);
        if (distance > attackRange + 1f)
            return;

        if (cachedPlayerDamageable != null)
            cachedPlayerDamageable.Damage(attackDamage, false);
    }

    private void ResetAttack()
    {
        isAttacking = false;
        state = WitchState.Chasing;

        // Trigger animator transition Attack → Run immediately
        if (animator != null)
            animator.SetTrigger(AttackResetHash);

        if (agent != null)
            agent.isStopped = false;

        _pathTimer = maxRepathInterval;
    }

    //==================================================
    // RETURN TO CRYING
    //==================================================

    private void ReturnToCrying()
    {
        state = WitchState.Crying;
        hasScreamed = false;
        isAttacking = false;

        // Cancel any pending attack timers
        attackDamageTimerActive = false;
        attackResetTimerActive = false;

        if (agent != null)
            agent.isStopped = true;

        if (animator != null)
        {
            animator.SetBool(IsChasingHash, false);
            animator.SetBool(IsCryingHash, true);
        }

        cryAudioPlaying = false; // cry will restart on next UpdateCrying
    }

    //==================================================
    // DAMAGE
    //==================================================

    public void Damage(float damage, bool isHeadshot)
    {
        if (CombatFeedbackHUD.Instance != null)
            CombatFeedbackHUD.Instance.ShowHit(transform.position, damage, isHeadshot);

        TakeDamage(Mathf.RoundToInt(damage));
    }

    public void TakeDamage(int damage)
    {
        if (isDead)
            return;

        currentHealth -= damage;

        if (PlayerStatsTracker.Instance != null)
            PlayerStatsTracker.Instance.RegisterDamageDealt(damage);

        if (animator != null)
            animator.SetTrigger(HitHash);

        if (OnHealthChanged != null)
            OnHealthChanged(HealthFraction);

        // Provoke on hit: if still crying, scream immediately and chase
        if (state == WitchState.Crying && !hasScreamed)
        {
            StartScream();
        }

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
        state = WitchState.Dead;

        // Cancel all timers (replacing CancelInvoke)
        screamTimerActive = false;
        attackDamageTimerActive = false;
        attackResetTimerActive = false;

        if (CombatFeedbackHUD.Instance != null)
            CombatFeedbackHUD.Instance.ShowKill("Witch");

        if (AIDirector.Instance != null)
            AIDirector.Instance.RegisterKill();

        if (PlayerStatsTracker.Instance != null)
            PlayerStatsTracker.Instance.RegisterTankKill();

        if (AchievementManager.Instance != null)
            AchievementManager.Instance.NotifyTankKill();

        if (ScoreManager.Instance != null)
            ScoreManager.Instance.AddKill(500);

        if (ExperienceManager.Instance != null && ExperienceManager.Instance.useExperience)
            ExperienceManager.Instance.AddExperience(experienceReward);

        if (CoinManager.Instance != null && CoinManager.Instance.useCoins)
            CoinManager.Instance.AddCoins(coinReward);

        if (agent != null)
            agent.enabled = false;

        if (animator != null)
        {
            // Force-clear other state bools so no Cry/Run transition can
            // override the Death transition (the AnyState→Death trigger can
            // lose to an in-progress Scream→Run exit-time transition).
            animator.SetBool(IsCryingHash, false);
            animator.SetBool(IsChasingHash, false);
            animator.SetBool(IsDeathHash, true);
            animator.SetTrigger(DeathHash);
            // Force-play Death state immediately, bypassing any pending
            // transitions that might fire first.
            animator.Play("Death", 0, 0f);
        }

        // Stop any ongoing cry/scream before playing death sound
        if (cryAudioPlaying)
        {
            audioSource.Stop();
            audioSource.clip = null;
            audioSource.loop = false;
            cryAudioPlaying = false;
        }
        else
        {
            audioSource.Stop();
        }
        if (deathClip != null)
            audioSource.PlayOneShot(deathClip);

        if (col != null)
            col.enabled = false;

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

        // Complete the quest (if a QuestTrigger is assigned) — Manual mode.
        if (questTriggerOnDeath != null)
        {
            questTriggerOnDeath.Complete();
        }

        Destroy(gameObject, cleanupDelay);
    }

    //==================================================
    // PLAYER FINDING
    //==================================================

    private void FindPlayer()
    {
        if (cachedPlayerTransform != null)
        {
            target = cachedPlayerTransform;
            cachedPlayerDamageable = target.GetComponent<IDamageable>();
            _targetStats = target.GetComponent<PlayerStats>();
            return;
        }

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            cachedPlayerTransform = player.transform;
            target = player.transform;
            cachedPlayerDamageable = target.GetComponent<IDamageable>();
            _targetStats = target.GetComponent<PlayerStats>();
        }
    }

    //==================================================
    // HELPERS
    //==================================================

    private void FaceTarget()
    {
        if (locomotion != null)
            locomotion.FaceTarget(rotationSpeed);
    }

    private void FaceMovementDirection()
    {
        if (locomotion != null)
            locomotion.FaceMovementDirection(rotationSpeed);
    }

    private void SetDestinationRobust(Vector3 destination)
    {
        if (locomotion != null)
            locomotion.SetDestinationRobust(destination);
    }

    private bool HasLineOfSight()
    {
        if (locomotion != null)
            return locomotion.HasLineOfSight();
        return false;
    }

    //==================================================
    // GIZMOS
    //==================================================

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, provokeRadius);

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, loseSightDistance);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}

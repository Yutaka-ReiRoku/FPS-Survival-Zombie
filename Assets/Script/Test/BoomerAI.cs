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

    [Header("Movement")]
    public float moveSpeed = 2f;
    public float rotationSpeed = 10f;

    [Header("Explosion")]
    public float explodeRange = 3f;
    public float screamDuration = 1.5f;

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

    private Animator animator;
    private NavMeshAgent agent;
    private Rigidbody rb;
    private Collider col;

    private bool isDead;
    private bool isHit;
    private bool isScreaming;
    private bool hasStartedExplosion;

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

        FindPlayer();

        if (agent != null)
        {
            agent.speed = moveSpeed;
            agent.acceleration = 20f;
            agent.angularSpeed = 300f;
            agent.stoppingDistance = explodeRange;
            agent.updateRotation = false;
        }

        if (animator != null)
        {
            animator.speed =
                Random.Range(0.95f, 1.05f);
        }
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
            distance <= detectRange)
        {
            agent.isStopped = false;

            agent.SetDestination(
                target.position
            );
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
            "Speed",
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
            "isWarning",
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
            "isWarning",
            false
        );

        animator.SetTrigger(
            "Explode"
        );

        // The authored death clip has no animation events wired, so drive the
        // blast and cleanup from code. Guards make this safe even if events are
        // added later (they would simply no-op the second call).
        Invoke(nameof(ExplosionEvent), explodeFxDelay);
        Invoke(nameof(DestroyEvent), cleanupDelay);
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

        animator.SetTrigger("Hit");

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

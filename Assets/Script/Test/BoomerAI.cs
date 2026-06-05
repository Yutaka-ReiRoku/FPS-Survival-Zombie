using UnityEngine;
using UnityEngine.AI;
using cowsins;

public class BoomerAI : MonoBehaviour, IDamageable
{
    [Header("Player")]
    public Transform target;

    [Header("Health")]
    public int maxHealth = 100;

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

    private static Collider[] overlapColliders = new Collider[500];

    private Animator animator;
    private NavMeshAgent agent;

    private int currentHealth;

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
    }

    //==================================
    // DAMAGE
    //==================================

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
            explosionType =
                ExplosionType.Killed;

            StartExplosion();
        }
    }

    //==================================
    // ANIMATION EVENTS
    //==================================

    // Gọi tại frame nổ
    public void ExplosionEvent()
    {
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
            }
        }
    }

    // Gọi ở frame cuối animation Death
    public void DestroyEvent()
    {
        isDead = true;

        if (agent != null)
        {
            agent.enabled = false;
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

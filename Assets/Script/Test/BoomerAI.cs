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
    public float detectRange = 25f;

    [Header("Movement")]
    public float moveSpeed = 2f;
    public float rotationSpeed = 10f;

    [Header("Explosion")]
    public float explodeRange = 3f;
    public float screamDuration = 1.5f;

    [Header("Explosion Damage")]
    public float explosionDamage = 50f;
    public float explosionRadius = 5f;

    [Header("VFX")]
    public GameObject explosionPrefab;
    public GameObject acidPoolPrefab;

    private Animator animator;
    private NavMeshAgent agent;

    private int currentHealth;

    private bool isDead;
    private bool isHit;
    private bool isScreaming;
    private bool hasStartedExplosion;

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
            FindPlayer();
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

        //----------------------------------
        // CHASE
        //----------------------------------

        if (!isScreaming)
        {
            if (distance <= detectRange)
            {
                agent.isStopped = false;

                agent.SetDestination(
                    target.position
                );
            }
        }

        //----------------------------------
        // EXPLODE
        //----------------------------------

        if (!hasStartedExplosion &&
            distance <= explodeRange)
        {
            StartExplosion();
        }

        //----------------------------------
        // ANIMATION
        //----------------------------------

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

    private void FindPlayer()
    {
        GameObject player =
            GameObject.FindGameObjectWithTag(
                "Player"
            );

        if (player != null)
        {
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

    //==================================
    // EXPLOSION SEQUENCE
    //==================================

    private void StartExplosion()
    {
        if (hasStartedExplosion)
            return;

        hasStartedExplosion = true;

        isScreaming = true;

        agent.isStopped = true;

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
            StartExplosion();
        }
    }

    //==================================
    // ANIMATION EVENTS
    //==================================

    // Event ở frame bụng nổ
    public void ExplosionEvent()
    {
        // Spawn VFX

        if (explosionPrefab != null)
        {
            Instantiate(
                explosionPrefab,
                transform.position,
                Quaternion.identity
            );
        }

        if (acidPoolPrefab != null)
        {
            Instantiate(
                acidPoolPrefab,
                transform.position,
                Quaternion.identity
            );
        }

        // Damage

        Collider[] hits =
            Physics.OverlapSphere(
                transform.position,
                explosionRadius
            );

        foreach (Collider hit in hits)
        {
            if (hit.transform == transform)
                continue;

            IDamageable damageable =
                hit.GetComponent<IDamageable>();

            if (damageable != null)
            {
                damageable.Damage(
                    explosionDamage,
                    false
                );
            }
        }
    }

    // Event ở frame cuối animation
    public void DestroyEvent()
    {
        isDead = true;

        if (agent != null)
        {
            agent.enabled = false;
        }

        Destroy(gameObject);
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
    }
}
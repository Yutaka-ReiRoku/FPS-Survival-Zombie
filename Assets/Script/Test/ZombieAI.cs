using UnityEngine;
using UnityEngine.AI;
using cowsins;

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(AudioSource))]
public class ZombieAI : MonoBehaviour, IDamageable, ICrookEnemy
{
    public float GetMaxHealth()
    {
        return maxHealth;
    }

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
    public GameObject dropPrefab;
    [Range(0, 100)]
    public float dropChance = 50f;

    private Animator animator;
    private NavMeshAgent agent;
    private AudioSource audioSource;

    private int currentHealth;

    private float attackTimer;
    private float wanderTimer;

    private bool isDead;
    private bool isAttacking;
    private bool hasDetectedPlayer;

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
    }

    void OnEnable()
    {
        isDead = false;
        isAttacking = false;
        hasDetectedPlayer = false;
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
        }
    }

    public void TakeDamage(int damage)
    {
        if (isDead)
            return;

        currentHealth -= damage;

        animator.SetTrigger(HitHash);

        PlaySound(hitClip);

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    public void Damage(float damage,
        bool isHeadshot)
    {
        TakeDamage(
            Mathf.RoundToInt(damage));
    }

    void Die()
    {
        if (isDead)
            return;

        isDead = true;

        if (AIDirector.Instance != null)
            AIDirector.Instance.RegisterKill();

        PlaySound(deathClip);

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
        if (dropPrefab == null)
            return;

        float chance =
            Random.Range(0f, 100f);

        if (chance <= dropChance)
        {
            Instantiate(
                dropPrefab,
                transform.position,
                Quaternion.identity);
        }
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
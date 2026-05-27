using UnityEngine;
using UnityEngine.AI;

public class ZombieAI : MonoBehaviour
{
    [Header("Target")]
    public Transform target;

    [Header("Movement")]
    public float walkSpeed = 1.5f;
    public float runSpeed = 3.5f;
    public float chaseDistance = 10f;
    public float stopDistance = 1.8f;

    [Header("Attack")]
    public float attackCooldown = 2f;

    [Header("Smoothing")]
    public float acceleration = 6f;
    public float animationDamping = 0.2f;

    [Header("Hit Stun")]
    public float hitStunDuration = 2f;

    [Header("Health")]
    public int maxHealth = 100;

    private int currentHealth;

    private Animator animator;
    private NavMeshAgent agent;

    private float currentSpeed;
    private float targetSpeed;

    private float attackTimer;

    private bool isAttacking;
    private bool isDead;
    private bool isHit;

    // RANDOM ATTACK
    private int attackIndex;

    private void Start()
    {
        currentHealth = maxHealth;

        // Animator
        animator = GetComponentInChildren<Animator>();

        // NavMeshAgent
        agent = GetComponent<NavMeshAgent>();

        agent.speed = runSpeed;
        agent.acceleration = 20f;
        agent.angularSpeed = 120f;
        agent.stoppingDistance = stopDistance;

        // Auto find player
        if (target == null)
        {
            GameObject player =
                GameObject.FindGameObjectWithTag("Player");

            if (player != null)
            {
                target = player.transform;
            }
        }

        // Small random animation speed
        animator.speed = Random.Range(0.95f, 1.05f);
    }

    private void Update()
    {
        if (target == null || isDead)
            return;

        attackTimer += Time.deltaTime;

        // HIT STUN
        if (isHit)
        {
            targetSpeed = 0f;

            currentSpeed = Mathf.Lerp(
                currentSpeed,
                0f,
                acceleration * Time.deltaTime
            );

            agent.isStopped = true;

            animator.SetFloat(
                "Speed",
                0f,
                animationDamping,
                Time.deltaTime
            );

            return;
        }

        float distance =
            Vector3.Distance(
                transform.position,
                target.position
            );

        // ATTACK
        if (distance <= stopDistance)
        {
            targetSpeed = 0f;

            agent.isStopped = true;

            if (!isAttacking && attackTimer >= attackCooldown)
            {
                attackIndex = Random.Range(0, 2);

                animator.SetInteger(
                    "AttackIndex",
                    attackIndex
                );

                animator.SetTrigger("Attack");

                isAttacking = true;

                attackTimer = 0f;

                Invoke(nameof(ResetAttack), attackCooldown);
            }
        }
        else
        {
            // CHASE
            agent.isStopped = false;

            if (distance <= chaseDistance)
            {
                targetSpeed = runSpeed;
            }
            else
            {
                targetSpeed = walkSpeed;
            }

            agent.speed = targetSpeed;

            agent.SetDestination(target.position);
        }

        // Smooth animation speed
        currentSpeed = Mathf.Lerp(
            currentSpeed,
            targetSpeed,
            acceleration * Time.deltaTime
        );

        // Normalize speed for Blend Tree
        float normalizedSpeed =
            Mathf.Clamp01(currentSpeed / runSpeed);

        animator.SetFloat(
            "Speed",
            normalizedSpeed,
            animationDamping,
            Time.deltaTime
        );
    }

    private void ResetAttack()
    {
        isAttacking = false;

        animator.ResetTrigger("Attack");
    }

    private void ResetHit()
    {
        isHit = false;

        if (!isDead)
        {
            agent.isStopped = false;
        }
    }

    // TEST DAMAGE
    private void OnTriggerEnter(Collider other)
    {
        if (other.isTrigger && !isDead)
        {
            TakeDamage(25);
        }
    }

    public void TakeDamage(int damage)
    {
        if (isDead)
            return;

        currentHealth -= damage;

        animator.SetTrigger("Hit");

        isHit = true;

        agent.isStopped = true;

        CancelInvoke(nameof(ResetHit));

        float randomHitStun =
            Random.Range(0.3f, 0.8f);

        Invoke(nameof(ResetHit), randomHitStun);

        Debug.Log("Zombie HP: " + currentHealth);

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        isDead = true;

        currentSpeed = 0f;

        // Stop NavMesh
        agent.isStopped = true;
        agent.enabled = false;

        // Death animation
        animator.SetBool("isDeath", true);
        animator.SetTrigger("Death");

        // Disable collider interaction
        GetComponent<Collider>().isTrigger = true;

        // Freeze rigidbody
        Rigidbody rb = GetComponent<Rigidbody>();

        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;

            rb.useGravity = false;
            rb.isKinematic = true;
        }

        Destroy(gameObject, 5f);
    }
}
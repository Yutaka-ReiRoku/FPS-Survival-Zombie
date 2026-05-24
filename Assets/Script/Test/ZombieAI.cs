using UnityEngine;

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
    public float rotationSpeed = 7f;
    public float animationDamping = 0.2f;

    [Header("Hit Stun")]
    public float hitStunDuration = 2f;

    private bool isHit;

    [Header("Health")]
    public int maxHealth = 100;

    private int currentHealth;

    private Animator animator;

    private float currentSpeed;
    private float targetSpeed;

    private float attackTimer;

    private bool isAttacking;
    private bool isDead;

    private void Start()
    {
        currentHealth = maxHealth;

        // lấy animator ở child luôn cho chắc
        animator = GetComponentInChildren<Animator>();

        // Auto find player by tag
        if (target == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");

            if (player != null)
            {
                target = player.transform;
            }
        }

        // Small random speed variation
        animator.speed = Random.Range(0.95f, 1.05f);
    }

    private void Update()
    {
        if (target == null || isDead)
            return;

        attackTimer += Time.deltaTime;
        if (isHit)
        {
            targetSpeed = 0f;

            currentSpeed = Mathf.Lerp(
                currentSpeed,
                0f,
                acceleration * Time.deltaTime
            );

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

        // Direction tới player
        Vector3 direction =
            (target.position - transform.position).normalized;

        direction.y = 0;

        // Smooth rotation
        if (direction != Vector3.zero)
        {
            Quaternion targetRotation =
                Quaternion.LookRotation(direction);

            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRotation,
                rotationSpeed * Time.deltaTime
            );
        }

        // ATTACK
        if (distance <= stopDistance)
        {
            targetSpeed = 0f;

            if (!isAttacking && attackTimer >= attackCooldown)
            {
                animator.SetTrigger("Attack");

                isAttacking = true;

                attackTimer = 0f;

                Invoke(nameof(ResetAttack), attackCooldown);
            }
        }
        else
        {
            // CHASE
            if (distance <= chaseDistance)
            {
                targetSpeed = runSpeed;
            }
            else
            {
                targetSpeed = walkSpeed;
            }
        }

        // Smooth acceleration
        currentSpeed = Mathf.Lerp(
            currentSpeed,
            targetSpeed,
            acceleration * Time.deltaTime
        );

        // Move toward player
        if (currentSpeed > 0.05f)
        {
            transform.position +=
                transform.forward *
                currentSpeed *
                Time.deltaTime;
        }

        // Normalize speed for Blend Tree
        float normalizedSpeed =
            Mathf.Clamp01(currentSpeed / runSpeed);

        // Smooth animation blending
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
    }
    private void ResetHit()
    {
        isHit = false;
    }

    // TEST DAMAGE
    private void OnTriggerEnter(Collider other)
    {
        // bất kỳ trigger nào chạm zombie đều nhận damage
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

        // hit animation
        animator.SetTrigger("Hit");

        isHit = true;

        Invoke(nameof(ResetHit), hitStunDuration);

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

        // lock animator states
        animator.SetBool("isDeath", true);

        // play death animation
        animator.SetTrigger("Death");

        // collider không còn interact vật lý
        GetComponent<Collider>().isTrigger = true;

        // freeze rigidbody
        Rigidbody rb = GetComponent<Rigidbody>();

        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;

            rb.useGravity = false;
            rb.isKinematic = true;
        }

        // destroy sau vài giây
        Destroy(gameObject, 5f);
    }
}
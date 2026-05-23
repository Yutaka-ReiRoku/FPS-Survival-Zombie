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

    private Animator animator;

    private float currentSpeed;
    private float targetSpeed;

    private float attackTimer;
    private bool isAttacking;

    private void Start()
    {
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
        if (target == null)
            return;

        attackTimer += Time.deltaTime;

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
}
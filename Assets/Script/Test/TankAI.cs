using UnityEngine;

public class TankAI : MonoBehaviour
{
    [Header("Target")]
    public Transform target;

    [Header("Movement")]
    public float runSpeed = 5f;
    public float chaseDistance = 25f;

    [Header("Attack Range")]
    public float meleeRange = 3f;
    public float jumpAttackRange = 12f;

    [Header("Cooldown")]
    public float jumpAttackCooldown = 12f;

    [Header("Rotation")]
    public float rotationSpeed = 8f;

    [Header("Scream")]
    public float screamDuration = 2.5f;

    [Header("Lifetime")]
    public float lifeTime = 40f;

    private Animator animator;
    private Rigidbody rb;
    private Collider col;

    private float jumpTimer;

    private bool isDead;
    private bool isAttacking;
    private bool isJumpAttacking;
    private bool isScreaming;
    private bool hasScreamed;
    private bool introJumpUsed;

    private int attackIndex;

    private void Start()
    {
        animator = GetComponent<Animator>();
        rb = GetComponent<Rigidbody>();
        col = GetComponent<Collider>();

        if (target == null)
        {
            GameObject player =
                GameObject.FindGameObjectWithTag("Player");

            if (player != null)
            {
                target = player.transform;
            }
        }

        animator.speed = Random.Range(0.95f, 1.05f);

        Invoke(nameof(StartDeath), lifeTime);
    }

    private void Update()
    {
        if (target == null || isDead)
            return;

        jumpTimer += Time.deltaTime;

        // =====================
        // AUTO END JUMP ATTACK
        // =====================

        AnimatorStateInfo state =
            animator.GetCurrentAnimatorStateInfo(0);

        if (
            isJumpAttacking &&
            !state.IsName("Mutant Jump Attack")
        )
        {
            Debug.Log("Jump Finished");

            isJumpAttacking = false;
            isAttacking = false;

            if (rb != null)
            {
                rb.useGravity = true;
            }
        }

        // =====================
        // SCREAM LOCK
        // =====================

        if (isScreaming)
            return;

        float distance =
            Vector3.Distance(
                transform.position,
                target.position
            );

        // =====================
        // FIRST SCREAM
        // =====================

        if (
            !hasScreamed &&
            distance <= chaseDistance
        )
        {
            hasScreamed = true;
            isScreaming = true;

            animator.Play("Zombie Scream");

            Invoke(
                nameof(EndScream),
                screamDuration
            );

            return;
        }

        // =====================
        // LOCK DURING JUMP
        // =====================

        if (isJumpAttacking)
            return;

        // =====================
        // ROTATION
        // =====================

        Vector3 direction =
            (target.position - transform.position).normalized;

        direction.y = 0;

        if (direction != Vector3.zero)
        {
            Quaternion targetRotation =
                Quaternion.LookRotation(direction);

            transform.rotation =
                Quaternion.Slerp(
                    transform.rotation,
                    targetRotation,
                    rotationSpeed * Time.deltaTime
                );
        }

        // =====================
        // CHASE
        // =====================

        if (!isAttacking)
        {
            transform.position +=
                transform.forward *
                runSpeed *
                Time.deltaTime;
        }

        // =====================
        // INTRO JUMP ATTACK
        // =====================

        if (
            hasScreamed &&
            !introJumpUsed &&
            distance <= jumpAttackRange
        )
        {
            introJumpUsed = true;

            StartJumpAttack();

            return;
        }

        // =====================
        // NORMAL JUMP ATTACK
        // =====================

        if (
            distance > meleeRange &&
            distance <= jumpAttackRange &&
            jumpTimer >= jumpAttackCooldown
        )
        {
            StartJumpAttack();

            return;
        }

        // =====================
        // MELEE ATTACK
        // =====================

        if (
            distance <= meleeRange &&
            !isAttacking
        )
        {
            MeleeAttack();
        }

        // =====================
        // DEBUG
        // =====================

        if (Input.GetKeyDown(KeyCode.P))
        {
            Debug.Log(
                $"Jump:{isJumpAttacking} " +
                $"Attack:{isAttacking}"
            );
        }
    }

    // =====================
    // ROOT MOTION
    // =====================

    private void OnAnimatorMove()
    {
        if (!isJumpAttacking)
            return;

        transform.position +=
            animator.deltaPosition;

        transform.rotation *=
            animator.deltaRotation;
    }

    // =====================
    // SCREAM END
    // =====================

    private void EndScream()
    {
        isScreaming = false;
    }

    // =====================
    // MELEE ATTACK
    // =====================

    private void MeleeAttack()
    {
        isAttacking = true;

        attackIndex =
            Random.Range(0, 2);

        animator.SetInteger(
            "AttackIndex",
            attackIndex
        );

        animator.SetTrigger("Attack");

        Invoke(
            nameof(ResetAttack),
            1f
        );
    }

    private void ResetAttack()
    {
        isAttacking = false;
    }

    // =====================
    // JUMP ATTACK
    // =====================

    private void StartJumpAttack()
    {
        isJumpAttacking = true;
        isAttacking = true;

        jumpTimer = 0f;

        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;

            rb.useGravity = false;
        }

        animator.Play(
            "Mutant Jump Attack"
        );
    }

    // =====================
    // DEATH
    // =====================

    private void StartDeath()
    {
        if (!isDead)
        {
            Die();
        }
    }

    public void Die()
    {
        if (isDead)
            return;

        isDead = true;

        CancelInvoke();

        animator.SetBool(
            "isDeath",
            true
        );

        animator.SetTrigger(
            "Death"
        );

        if (col != null)
        {
            col.enabled = false;
        }

        if (rb != null)
        {
            rb.linearVelocity =
                Vector3.zero;

            rb.angularVelocity =
                Vector3.zero;

            rb.isKinematic = true;
            rb.useGravity = false;
        }

        Destroy(
            gameObject,
            6f
        );
    }
}
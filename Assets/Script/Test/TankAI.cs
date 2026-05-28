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

    // === SYNTY FIX ===
    private Transform meshRoot;
    private Vector3 rootMotionVelocity; // Giúp theo dõi root motion

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

        // === TÌM MESH ROOT (Synty) ===
        SkinnedMeshRenderer smr = GetComponentInChildren<SkinnedMeshRenderer>();
        if (smr != null)
        {
            meshRoot = smr.transform.parent;
        }
        if (meshRoot == null) meshRoot = transform;

        // AUTO FIND PLAYER
        if (target == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null) target = player.transform;
        }

        animator.speed = Random.Range(0.95f, 1.05f);
        Invoke(nameof(StartDeath), lifeTime);
    }

    private void Update()
    {
        if (target == null || isDead) return;

        jumpTimer += Time.deltaTime;

        if (isScreaming || isJumpAttacking) return;

        Vector3 direction = (target.position - transform.position).normalized;
        direction.y = 0;

        float distance = Vector3.Distance(transform.position, target.position);

        // Rotation
        if (direction != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }

        // First Scream
        if (!hasScreamed && distance <= chaseDistance)
        {
            hasScreamed = true;
            isScreaming = true;
            animator.CrossFade("Zombie Scream", 0.15f);
            Invoke(nameof(EndScream), screamDuration);
            return;
        }

        // Chase
        if (!isAttacking)
        {
            transform.position += transform.forward * runSpeed * Time.deltaTime;
        }

        // Jump Attacks
        if (hasScreamed && !introJumpUsed && distance <= jumpAttackRange)
        {
            introJumpUsed = true;
            JumpAttack();
            return;
        }

        if (distance > meleeRange && distance <= jumpAttackRange && jumpTimer >= jumpAttackCooldown)
        {
            JumpAttack();
            return;
        }

        // Melee
        if (distance <= meleeRange && !isAttacking)
        {
            MeleeAttack();
        }
    }

    // ====================== ROOT MOTION FIX CHO SYNTY ======================
    private void OnAnimatorMove()
    {
        if (!isJumpAttacking) return;

        // Lấy root motion từ animation
        Vector3 deltaPos = animator.deltaPosition;
        Quaternion deltaRot = animator.deltaRotation;

        // Áp dụng cho ROOT
        transform.position += deltaPos;
        transform.rotation *= deltaRot;

        // Fix mesh root (rất quan trọng)
        if (meshRoot != null && meshRoot != transform)
        {
            meshRoot.localPosition = Vector3.zero;
            meshRoot.localRotation = Quaternion.identity;
        }

        // Tắt physics ảnh hưởng trong lúc jump
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
    }

    private void EndScream()
    {
        isScreaming = false;
    }

    private void MeleeAttack()
    {
        isAttacking = true;
        attackIndex = Random.Range(0, 2);
        animator.SetInteger("AttackIndex", attackIndex);
        animator.SetTrigger("Attack");
        Invoke(nameof(ResetAttack), 0.8f);
    }

    private void JumpAttack()
    {
        isJumpAttacking = true;
        isAttacking = true;
        jumpTimer = 0f;

        // Tắt physics mạnh tay hơn
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.useGravity = false;           // Tắt tạm gravity
        }

        animator.CrossFade("Mutant Jump Attack", 0.1f);
    }

    public void EndJumpAttack()   // Animation Event
    {
        isJumpAttacking = false;
        isAttacking = false;

        // Bật lại gravity sau khi jump xong
        if (rb != null)
        {
            rb.useGravity = true;
        }
    }

    private void ResetAttack()
    {
        isAttacking = false;
        animator.ResetTrigger("Attack");
    }

    public void DoAOEDamage()
    {
        Debug.Log("TANK SLAM!");
    }

    private void StartDeath()
    {
        if (isDead) return;
        Die();
    }

    public void Die()
    {
        if (isDead) return;

        isDead = true;
        CancelInvoke();

        animator.SetBool("isDeath", true);
        animator.SetTrigger("Death");

        if (col != null) col.enabled = false;

        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.useGravity = false;
            rb.isKinematic = true;
            rb.constraints = RigidbodyConstraints.FreezeAll;
        }

        Destroy(gameObject, 6f);
    }
}
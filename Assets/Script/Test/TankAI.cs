using cowsins;
using UnityEngine;

public class TankAI : ZombieAI
{
    [Header("Tank Stats")]
    public int tankMaxHealth = 150;

    [Header("Jump Attack")]
    public float jumpAttackRange = 12f;
    public float jumpCooldown = 12f;
    public float shockwaveRadius = 4f;
    public float shockwaveDamage = 35f;

    [Header("Scream")]
    public float screamDuration = 2.5f;

    private float jumpTimer;

    private bool isJumpAttacking;
    private bool isScreaming;
    private bool hasScreamed;
    private bool introJumpUsed;
    private bool enraged;

    protected override void Start()
    {
        maxHealth = tankMaxHealth;

        walkSpeed *= 0.8f;
        runSpeed *= 0.8f;

        base.Start();

        jumpTimer = jumpCooldown;
    }

    protected override void Update()
    {
        if (target == null || isDead)
            return;

        jumpTimer += Time.deltaTime;

        float distance =
            Vector3.Distance(
                transform.position,
                target.position
            );

        // FIRST SCREAM
        if (
            !hasScreamed &&
            distance <= detectDistance
        )
        {
            StartScream();
            return;
        }

        // LOCK AI DURING SCREAM
        if (isScreaming)
            return;

        // ENRAGE
        CheckEnrage();

        // INTRO JUMP
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

        // NORMAL JUMP
        if (
            distance > attackDistance &&
            distance <= jumpAttackRange &&
            jumpTimer >= jumpCooldown &&
            !isJumpAttacking
        )
        {
            StartJumpAttack();

            return;
        }

        // LOCK DURING JUMP
        if (isJumpAttacking)
            return;

        base.Update();
    }

    private void StartScream()
    {
        hasScreamed = true;
        isScreaming = true;

        agent.isStopped = true;

        animator.SetTrigger("Scream");

        Invoke(
            nameof(EndScream),
            screamDuration
        );
    }

    private void EndScream()
    {
        isScreaming = false;

        if (!isDead)
        {
            agent.isStopped = false;
        }
    }

    private void StartJumpAttack()
    {
        if (isDead)
            return;

        isJumpAttacking = true;

        jumpTimer = 0f;

        agent.isStopped = true;

        FaceTarget();

        animator.SetTrigger(
            "JumpAttack"
        );
    }

    // Animation Event
    public void JumpLand()
    {
        Collider[] hits =
            Physics.OverlapSphere(
                transform.position,
                shockwaveRadius
            );

        foreach (Collider hit in hits)
        {
            if (!hit.CompareTag("Player"))
                continue;

            IDamageable player =
                hit.GetComponent<IDamageable>();

            if (player != null)
            {
                player.Damage(
                    shockwaveDamage,
                    false
                );
            }

            Rigidbody playerRb =
                hit.GetComponent<Rigidbody>();

            if (playerRb != null)
            {
                Vector3 dir =
                    (
                        hit.transform.position -
                        transform.position
                    ).normalized;

                playerRb.AddForce(
                    dir * 10f,
                    ForceMode.Impulse
                );
            }
        }
    }

    // Animation Event cuối clip
    public void EndJumpAttack()
    {
        isJumpAttacking = false;

        if (!isDead)
        {
            agent.isStopped = false;
        }
    }

    // Animation Event của attack clip
    public new void AttackHit()
    {
        if (target == null)
            return;

        float distance =
            Vector3.Distance(
                transform.position,
                target.position
            );

        if (distance <= attackDistance + 1f)
        {
            DamagePlayer(49);
        }
    }

    private void CheckEnrage()
    {
        if (enraged)
            return;

        float hpPercent =
            (float)currentHealth /
            maxHealth;

        if (hpPercent <= 0.3f)
        {
            enraged = true;

            runSpeed *= 1.25f;

            jumpCooldown *= 0.5f;

            animator.SetTrigger(
                "Enrage"
            );
        }
    }

    protected override void Die()
    {
        base.Die();
    }
}
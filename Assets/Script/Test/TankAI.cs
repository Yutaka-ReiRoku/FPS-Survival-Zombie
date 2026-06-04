using cowsins;
using UnityEngine;
using UnityEngine.AI;

public class TankAI : ZombieAI
{
    [Header("Tank Stats")]
    public int tankHealth = 300;
    public float speedMultiplier = 0.8f;

    [Header("Leap")]
    public float leapRangeMin = 8f;
    public float leapRangeMax = 20f;
    public float leapForce = 15f;
    public float leapHeight = 6f;
    public float leapCooldown = 15f;

    [Header("Shockwave")]
    public float shockwaveRadius = 4f;
    public float shockwaveDamage = 35f;

    private Rigidbody rb;

    private float leapTimer;

    private bool isLeaping;
    private bool enraged;

    protected override void Start()
    {
        maxHealth = tankHealth;

        walkSpeed *= speedMultiplier;
        runSpeed *= speedMultiplier;

        base.Start();

        rb = GetComponent<Rigidbody>();

        leapTimer = leapCooldown;
    }

    protected override void Update()
    {
        base.Update();

        if (target == null)
            return;

        leapTimer -= Time.deltaTime;

        float distance =
            Vector3.Distance(
                transform.position,
                target.position
            );

        if (!isLeaping &&
            leapTimer <= 0f &&
            distance >= leapRangeMin &&
            distance <= leapRangeMax)
        {
            StartLeap();
        }

        CheckEnrage();
    }

    private void StartLeap()
    {
        if (agent == null || !agent.enabled)
            return;

        isLeaping = true;

        leapTimer = leapCooldown;

        agent.enabled = false;

        animator.SetTrigger("Leap");

        Vector3 direction =
            (target.position -
            transform.position).normalized;

        rb.linearVelocity =
            direction * leapForce +
            Vector3.up * leapHeight;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (!isLeaping)
            return;

        if (collision.contacts.Length > 0)
        {
            LandImpact();
        }
    }

    private void LandImpact()
    {
        isLeaping = false;

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
                Vector3 pushDir =
                    (
                        hit.transform.position -
                        transform.position
                    ).normalized;

                playerRb.AddForce(
                    pushDir * 10f,
                    ForceMode.Impulse
                );
            }
        }

        if (!isDead)
        {
            agent.enabled = true;
        }
    }

    private void CheckEnrage()
    {
        if (enraged)
            return;

        float healthPercent =
            (float)GetCurrentHealth() /
            tankHealth;

        if (healthPercent <= 0.3f)
        {
            enraged = true;

            runSpeed *= 1.25f;

            leapCooldown = 8f;

            animator.SetTrigger("Enrage");
        }
    }

    public new void AttackHit()
    {
        if (target == null)
            return;

        float distance =
            Vector3.Distance(
                transform.position,
                target.position
            );

        if (distance <= attackDistance + 0.5f)
        {
            DamagePlayer(49);
        }
    }

    private int GetCurrentHealth()
    {
        return Mathf.Max(
            1,
            tankHealth
        );
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;

        Gizmos.DrawWireSphere(
            transform.position,
            shockwaveRadius
        );
    }
}
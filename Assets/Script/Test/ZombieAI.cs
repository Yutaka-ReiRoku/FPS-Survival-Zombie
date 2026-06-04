    using UnityEngine;
    using UnityEngine.AI;
    using cowsins;
    public class ZombieAI : MonoBehaviour, IDamageable
    {
        [Header("Target")]
        public Transform target;

        [Header("Movement")]
        public float walkSpeed = 1.5f;
        public float runSpeed = 3.5f;
        public float detectDistance = 20f;
        public float stopDistance = 1f;
        public float attackDistance = 1.8f;


        [Header("Wander")]
        public float wanderRadius = 15f;
        public float wanderTimer = 5f;

        [Header("Idle Wander")]
        public float idleTimeMin = 2f;
        public float idleTimeMax = 5f;

        [Header("Attack")]
        public float attackCooldown = 1.5f;

        [Header("Animation")]
        public float acceleration = 6f;
        public float animationDamping = 0.4f;

        [Header("Health")]
        public int maxHealth = 100;

        private int currentHealth;

        [Header("Loot Drop")]
        public GameObject dropPrefab;
        [Range(1, 100)]
        public float dropChance = 100f;
        public Vector3 dropOffset = new Vector3(0, 0.5f, 0);


        private Animator animator;
        private NavMeshAgent agent;

        private float currentSpeed;
        private float targetSpeed;

        private float attackTimer;
        private float wanderCounter;

        private bool isAttacking;
        private bool isDead;
        private bool isHit;

        // IDLE WANDER
        private bool isIdleWander;
        private float idleTimer;

        // RANDOM ATTACK
        private int attackIndex;



        private void Start()
        {
            currentHealth = maxHealth;

            animator = GetComponentInChildren<Animator>();

            agent = GetComponent<NavMeshAgent>();

            agent.speed = walkSpeed;
            agent.acceleration = 15f;
            agent.angularSpeed = 300f;
            agent.autoBraking = false;
            agent.stoppingDistance = attackDistance;

            agent.updateRotation = false;

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

            // Random animation speed
            animator.speed = Random.Range(0.95f, 1.05f);

            wanderCounter = wanderTimer;

            if (AIDirector.Instance != null)
            {
                AIDirector.Instance
                    .RegisterZombie(this);
            }

            NavMeshHit navHit;

            if (
                NavMesh.SamplePosition(
                    transform.position,
                    out navHit,
                    3f,
                    NavMesh.AllAreas
                )
            )
            {
                transform.position =
                    navHit.position;
            }
        }

        private void Update()
        {
            if (target == null || isDead)
                return;

            attackTimer += Time.deltaTime;
            wanderCounter += Time.deltaTime;

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

        // PLAYER DETECTED
        if (distance <= detectDistance)
        {
                isIdleWander = false;

            // ATTACK
                if (distance <= attackDistance)
                {
                    targetSpeed = 0f;
                agent.isStopped = true;

                    if (!isAttacking &&
                    attackTimer >= attackCooldown)
                    {
                        attackIndex = Random.Range(0, 2);

                        animator.SetInteger(
                            "AttackIndex",
                            attackIndex
                        );

                        animator.SetTrigger("Attack");

                        isAttacking = true;

                        attackTimer = 0f;
                        animator.SetTrigger("Attack");

                        isAttacking = true;
                        attackTimer = 0f;

                        Invoke(nameof(ResetAttack), attackCooldown);


                        Invoke(
                            nameof(ResetAttack),
                            attackCooldown
                        );
                    }
                }
                else
                {
                    // CHASE
                    targetSpeed = runSpeed;

                    agent.isStopped = false;

                    agent.speed = runSpeed;

                    agent.SetDestination(
                        target.position
                    );
                    FaceTarget();
                }
            }
            else
            {
                // IDLE WANDER
                if (isIdleWander)
                {
                    targetSpeed = 0f;

                    idleTimer -= Time.deltaTime;

                    agent.isStopped = true;

                    if (idleTimer <= 0f)
                    {
                        isIdleWander = false;

                        wanderCounter = wanderTimer;
                    }
                }
                else
                {
                    // NORMAL WANDER
                    targetSpeed = walkSpeed;

                    agent.isStopped = false;

                    agent.speed = walkSpeed;
                if (agent.velocity.sqrMagnitude > 0.1f)
                {
                    Vector3 lookDir =
                        agent.velocity.normalized;

                    lookDir.y = 0f;

                    transform.rotation =
                        Quaternion.Slerp(
                            transform.rotation,
                            Quaternion.LookRotation(lookDir),
                            Time.deltaTime * 5f
                        );
                }

                if (wanderCounter >= wanderTimer)
                    {
                        Vector3 newPos =
                            RandomNavSphere(
                                transform.position,
                                wanderRadius,
                                -1
                            );

                        agent.SetDestination(newPos);

                        wanderCounter = 0;

                        // RANDOM IDLE
                        if (Random.value > 0.5f)
                        {
                            StartIdle();
                        }
                    }
                }
            }
            
        // Smooth animation
            currentSpeed = Mathf.Lerp(
                currentSpeed,
                targetSpeed,
                acceleration * Time.deltaTime
            );

            float normalizedSpeed =
            Mathf.Clamp01(
                agent.velocity.magnitude /
                runSpeed
            );

        animator.SetFloat(
                "Speed",
                normalizedSpeed,
                animationDamping,
                Time.deltaTime
            );
    }
        
        // RANDOM POSITION
        public static Vector3 RandomNavSphere(
            Vector3 origin,
            float distance,
            int layermask
        )
        {
            Vector3 randomDirection =
                Random.insideUnitSphere * distance;

            randomDirection += origin;

            NavMeshHit navHit;

            NavMesh.SamplePosition(
                randomDirection,
                out navHit,
                distance,
                layermask
            );

            return navHit.position;
        }

        // START IDLE
        private void StartIdle()
        {
            isIdleWander = true;

            idleTimer = Random.Range(
                idleTimeMin,
                idleTimeMax
            );

            agent.isStopped = true;
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
            if (AIDirector.Instance != null)
            {
                AIDirector.Instance
                    .UnregisterZombie(this);
                ScoreManager.Instance?.AddKill();
            }
            WaveManager.Instance?.
            RegisterZombieKill();
        isDead = true;

            currentSpeed = 0f;

            agent.isStopped = true;
            agent.enabled = false;
            TestDrop();
            animator.SetBool("isDeath", true);
            animator.SetTrigger("Death");

            GetComponent<Collider>().isTrigger = true;

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
        public void Damage(float damage, bool isHeadshot)
        {
            if (isHeadshot)
            {
                ScoreManager.Instance?.
                    AddHeadshot();
            }

            TakeDamage(Mathf.RoundToInt(damage));
        }

    private void DamagePlayer(float damage)
        {
            if (target == null) return;

            IDamageable player =
                target.GetComponent<IDamageable>();

            if (player != null)
            {
                player.Damage(damage, false);
            }
        }

        public void DropLoot()
        {
            if (dropPrefab == null)
            {
                Debug.LogWarning("Drop Prefab NULL");
                return;
            }

            Debug.Log("DROP TEST");

            Instantiate(
                dropPrefab,
                transform.position + dropOffset,
                Quaternion.identity
            );
    }


    [ContextMenu("TEST DROP")]
    public void TestDrop()
    {
        DropLoot();
    }

    private void FaceTarget()
    {
        if (target == null)
            return;

        Vector3 lookDir =
            target.position -
            transform.position;

        lookDir.y = 0f;

        if (lookDir.sqrMagnitude < 0.01f)
            return;

        Quaternion targetRotation =
            Quaternion.LookRotation(
                lookDir
            );

        transform.rotation =
            Quaternion.Slerp(
                transform.rotation,
                targetRotation,
                Time.deltaTime * 12f
            );
    }

    public void AttackHit()
    {
        if (isDead || target == null)
            return;

        float distance = Vector3.Distance(
            transform.position,
            target.position
        );

        if (distance <= attackDistance + 0.5f)
        {
            DamagePlayer(20);
        }
    }

}
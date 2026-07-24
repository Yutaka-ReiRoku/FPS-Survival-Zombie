using UnityEngine;
using UnityEngine.AI;
using cowsins;
using System.Collections;

/// <summary>
/// Companion (ally) NPC that follows the player and fights zombies with a
/// shotgun (raycast spread). Implements IDamageable + IEnemyHealthReadout so
/// the existing EnemyHealthBar world-space UI can render its health bar.
///
/// The companion never truly dies: when HP reaches 1 it enters a Downed state
/// for <see cref="downedDuration"/> seconds, then revives at 50% HP.
///
/// State machine:
///   Waiting     — idle at spawn, waiting for player to interact (E) and decide.
///   Following   — follows the player via NavMeshAgent, keeps followDistance.
///   Shooting    — sub-state of Following: fires shotgun at nearest enemy in range.
///   Downed      — incapacitated, revives after downedDuration.
///   WalkingAway — paths to deadEndPoint, then self-destructs (player refused).
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(AudioSource))]
public class CompanionAI : MonoBehaviour, IDamageable, IEnemyHealthReadout
{
    public enum State { Waiting, Following, Downed, WalkingAway }

    [Header("Identity")]
    [Tooltip("Tag of hostile targets the companion will shoot.")]
    public string enemyTag = "Enemy";

    [Header("Health")]
    public int maxHealth = 150;
    public int currentHealth { get; private set; }
    public bool IsDead => false; // Companion never truly dies.

    [Header("Following")]
    public float followSpeed = 3.5f;
    public float followDistance = 4f;
    public float repathInterval = 0.25f;
    [Tooltip("How long the companion stops to shoot before resuming movement (L4D2 style).")]
    public float shootStopDuration = 0.35f;
    [Tooltip("Smooth time for Speed animator parameter.")]
    public float speedSmoothTime = 0.15f;
    [Tooltip("If player is farther than this, companion abandons combat to catch up (L4D2 style).")]
    public float abandonCombatDistance = 12f;

    [Header("Combat — Shotgun")]
    [Tooltip("Range at which the companion can detect and shoot enemies.")]
    public float detectRange = 20f;
    public float shootCooldown = 1.5f;
    public int shotgunPellets = 8;
    public float shotgunSpreadDeg = 8f;
    public float shotgunRange = 20f;
    public int shotgunDamagePerRay = 18;
    [Tooltip("Muzzle flash effect prefab (spawned at gun barrel, once per shot).")]
    public GameObject muzzleFlashPrefab;
    [Tooltip("Impact effect prefab (spawned at each hit point). Optional — runtime sparks used as fallback.")]
    public GameObject impactPrefab;
    [Tooltip("Audio clip played when firing.")]
    public AudioClip shootClip;

    [Header("Downed / Revive")]
    public float downedDuration = 30f;
    public float reviveHealthFraction = 0.5f;

    [Header("Rescue by Player (hold E)")]
    [Tooltip("How long the player must hold E near the downed companion to revive it.")]
    public float rescueHoldDuration = 3f;
    [Tooltip("Maximum distance from the downed companion within which the player can rescue.")]
    public float rescueMaxDistance = 3f;
    [Tooltip("Key the player must hold to rescue the downed companion.")]
    public KeyCode rescueKey = KeyCode.E;
    [Tooltip("Health fraction restored when the player rescues the companion (1 = full HP).")]
    public float rescueHealthFraction = 1f;
    [Tooltip("Dialogue line spoken by the companion after being rescued.")]
    [TextArea(1, 3)]
    public string rescuedThankLine = "Cảm ơn vì đã cứu tôi.";
    [Tooltip("How long (seconds) the thank-you dialogue stays visible before fading.")]
    public float rescuedThankHoldDuration = 2f;

    [Header("Walking Away (refused)")]
    [Tooltip("Destination the companion walks to when the player refuses.")]
    public Vector3 deadEndPoint = new Vector3(60.62f, 0f, -21.49f);
    public float destroyDistance = 1.5f;
    public float fadeOutDuration = 0.5f;

    [Header("Damage From Enemies")]
    [Tooltip("How often (seconds) nearby enemies deal damage to the companion.")]
    public float enemyDamageTickInterval = 1f;
    [Tooltip("Damage per tick per nearby enemy within enemyDamageRadius.")]
    public int enemyDamagePerTick = 10;
    public float enemyDamageRadius = 2f;

    [Header("Animator")]
    public Animator animator;

    [Header("Reload")]
    [Tooltip("Number of shots before the companion reloads.")]
    public int magazineSize = 8;
    [Tooltip("Reload duration in seconds (must match Reload animation length).")]
    public float reloadDuration = 2.5f;

    public EnemyType EnemyType => EnemyType.Special;
    public float HealthFraction => maxHealth > 0 ? Mathf.Clamp01((float)currentHealth / maxHealth) : 0f;
    public event System.Action<float> OnHealthChanged;
    public event System.Action<State> OnStateChanged;
    /// <summary>Raised with normalized 0..1 progress while the player holds E to rescue. 0 = stopped, 1 = complete.</summary>
    public event System.Action<float> OnRescueProgressChanged;

    public State CurrentState
    {
        get => _state;
        private set
        {
            if (_state == value) return;
            _state = value;
            OnStateChanged?.Invoke(_state);
        }
    }

    private State _state = State.Waiting;
    private NavMeshAgent _agent;
    private AudioSource _audio;
    private Transform _player;
    private float _shootTimer;
    private float _repathTimer;
    private float _enemyDamageTimer;
    private float _downedTimer;
    private float _shootStopTimer; // Stops movement while shooting (L4D2 style)
    private int _ammoRemaining; // Shots left before reload
    private float _reloadTimer; // Counts down during reload; companion can't shoot while > 0
    private float _rescueProgress; // 0..rescueHoldDuration — accumulated while player holds E near downed companion
    private DialogueBubble _bubble; // Cached for rescue thank-you dialogue
    private Collider _interactCollider; // Trigger collider on layer Interactable — disabled while Downed so InteractManager ignores the companion
    private cowsins.InputManager _playerInput; // Cached player InputManager (Input System) for reading the Interacting action
    private float _speedVelocity; // SmoothDamp velocity for Speed parameter
    private float _currentAnimSpeed; // Current smoothed Speed value
    private float _ikWeight; // Smoothed IK weight for left hand grip
    private Transform _rootBone; // Skeleton root bone (for fixing sink issue)
    private Transform _leftHandGrip; // Grip target on the shotgun forestock (for IK)
    private Collider[] _enemyBuffer = new Collider[32];
    private static readonly int SpeedHash = Animator.StringToHash("Speed");
    private static readonly int ShootHash = Animator.StringToHash("Shoot");
    private static readonly int DownedHash = Animator.StringToHash("Downed");
    private static readonly int HitHash = Animator.StringToHash("Hit");
    private static readonly int ReviveHash = Animator.StringToHash("Revive");
    private static readonly int ReloadHash = Animator.StringToHash("Reload");
    private static readonly int DeathHash = Animator.StringToHash("Death");
    private static readonly int DashBackHash = Animator.StringToHash("DashBack");

    private void Awake()
    {
        _agent = GetComponent<NavMeshAgent>();
        _audio = GetComponent<AudioSource>();
        _bubble = GetComponent<DialogueBubble>();
        if (animator == null) animator = GetComponentInChildren<Animator>();
        _ammoRemaining = magazineSize;

        // Cache the trigger collider on the Interactable layer. This is what
        // InteractManager raycasts against to show the "Nói chuyện" prompt.
        // We disable it while Downed so InteractManager completely ignores the
        // companion (no prompt, no E consumption) — leaving E free for rescue.
        _interactCollider = GetComponent<Collider>();
        // CRITICAL: Disable root motion — NavMeshAgent controls position.
        // Root motion from Mixamo animations causes the model to sink into the ground.
        if (animator != null) animator.applyRootMotion = false;
        // Cache the skeleton root bone so we can fix its Y in LateUpdate.
        // Mixamo animations drive the Root bone to y=-0.96, causing the model
        // to sink into the ground. We override this every frame after animation.
        _rootBone = animator != null ? animator.transform.Find("Root") : null;

        // Find the left-hand grip target on the shotgun forestock.
        // The shotgun is parented to the right hand; the forestock is at ~60%
        // of the barrel length (z ≈ 0.37 for a 0.617m-long shotgun).
        if (animator != null)
        {
            var rh = animator.GetBoneTransform(HumanBodyBones.RightHand);
            if (rh != null)
            {
                var gun = rh.Find("CompanionShotgun");
                if (gun != null)
                {
                    // Create a grip target child on the gun at the forestock.
                    var gripGO = new GameObject("LeftHandGrip");
                    gripGO.transform.SetParent(gun, false);
                    gripGO.transform.localPosition = new Vector3(0f, 0f, 0.37f);
                    _leftHandGrip = gripGO.transform;
                }
            }
        }
    }

    private void LateUpdate()
    {
        // Fix model sinking: Mixamo animations push the Root bone down to y=-0.96.
        // After the Animator applies pose, reset the Root bone Y so feet stay on ground.
        // When Downed (idle crouching), the animation root is at y=0.526 instead of
        // 0.955 — we need to offset so the character doesn't float or sink.
        if (_rootBone != null)
        {
            Vector3 pos = _rootBone.localPosition;
            // Use animator bool (more reliable than _state for LateUpdate timing).
            bool isDowned = animator != null && animator.GetBool(DownedHash);
            if (isDowned)
            {
                // idle crouching RootT.y=0.526 vs idle aiming RootT.y=0.955
                // Offset = -(0.955 - 0.526) = -0.429, plus extra -0.03 to plant feet
                pos.y = -0.46f;
            }
            else
            {
                pos.y = 0f;
            }
            _rootBone.localPosition = pos;
        }
    }

    private void OnEnable()
    {
        currentHealth = maxHealth;
        _agent.speed = followSpeed;
        _agent.acceleration = 8f; // Smooth acceleration (L4D2 style)
        _agent.angularSpeed = 360f; // Turn quickly to face enemies
        _agent.stoppingDistance = followDistance * 0.5f;
        // Disable agent auto-rotation — we handle facing manually (like ZombieAI)
        // so the companion faces its movement direction while chasing (not the
        // player), preventing spine twisting when the player runs in circles.
        _agent.updateRotation = false;
        _agent.obstacleAvoidanceType = ObstacleAvoidanceType.LowQualityObstacleAvoidance;
        _agent.avoidancePriority = 50;
    }

    private void Start()
    {
        FindPlayer();
    }

    private void FindPlayer()
    {
        if (_player != null) return;
        var p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) _player = p.transform;
        ResolvePlayerInput();
    }

    /// <summary>
    /// Resolves the player's InputManager (Input System). The InputManager is a
    /// sibling of the tagged "Player" object under the same parent (e.g.
    /// "Player/InputManager" vs "Player/Player"), so we search the root parent's
    /// children. Safe to call repeatedly — returns early once found.
    /// </summary>
    private void ResolvePlayerInput()
    {
        if (_playerInput != null) return;
        if (_player == null) return;
        var p = _player.gameObject;
        _playerInput = p.GetComponentInParent<cowsins.InputManager>();
        if (_playerInput == null && p.transform.parent != null)
            _playerInput = p.transform.parent.GetComponentInChildren<cowsins.InputManager>();
        if (_playerInput == null)
            _playerInput = p.GetComponentInChildren<cowsins.InputManager>();
    }

    private void Update()
    {
        FindPlayer();
        if (_player == null) return;

        switch (_state)
        {
            case State.Waiting:
                SetAgentStopped(true);
                SetAnimSpeed(0f);
                break;
            case State.Following:
                UpdateFollowing();
                break;
            case State.Downed:
                UpdateDowned();
                break;
            case State.WalkingAway:
                UpdateWalkingAway();
                break;
        }
    }

    // ---- Following + Combat ----

    private void UpdateFollowing()
    {
        // Tick enemy damage (nearby zombies hurt the companion).
        _enemyDamageTimer -= Time.deltaTime;
        if (_enemyDamageTimer <= 0f)
        {
            _enemyDamageTimer = enemyDamageTickInterval;
            TakeDamageFromNearbyEnemies();
        }

        // L4D2 style: if player is too far, abandon combat and catch up.
        float distToPlayer = Vector3.Distance(transform.position, _player.position);
        bool playerTooFar = distToPlayer > abandonCombatDistance;

        // Adjust agent speed based on distance: walk when close, run when far.
        // This creates smooth idle->walk->run blend transitions (like Zombie).
        if (distToPlayer > followDistance * 2f)
            _agent.speed = followSpeed;           // Run (Speed ≈ 1.0)
        else
            _agent.speed = followSpeed * 0.5f;    // Walk (Speed ≈ 0.5)

        if (playerTooFar)
        {
            // Player is too far — stop shooting, follow player immediately.
            _shootStopTimer = 0f;
            SetAgentStopped(false);
            _repathTimer -= Time.deltaTime;
            if (_repathTimer <= 0f)
            {
                _repathTimer = repathInterval;
                _agent.SetDestination(_player.position);
            }
        }
        else
        {
            // Tick down reload timer — companion can't shoot while reloading.
            if (_reloadTimer > 0f)
            {
                _reloadTimer -= Time.deltaTime;
                SetAgentStopped(true);
            }

            // Player is close enough — can afford to stop and shoot.
            _shootTimer -= Time.deltaTime;
            if (_shootTimer <= 0f && _reloadTimer <= 0f)
            {
                var target = FindNearestEnemy();
                if (target != null)
                {
                    if (_ammoRemaining <= 0)
                    {
                        // Out of ammo — reload.
                        _shootTimer = reloadDuration;
                        _reloadTimer = reloadDuration;
                        _shootStopTimer = reloadDuration;
                        PlayReload();
                    }
                    else
                    {
                        _shootTimer = shootCooldown;
                        _shootStopTimer = shootStopDuration; // Stop to shoot (L4D2 style)
                        ShootAt(target);
                    }
                }
            }

            // L4D2 style: stop moving while shooting, face the enemy.
            if (_shootStopTimer > 0f)
            {
                _shootStopTimer -= Time.deltaTime;
                SetAgentStopped(true);

                // Face the nearest enemy while shooting (like ZombieAI.FaceTarget).
                var target = FindNearestEnemy();
                if (target != null)
                {
                    FacePosition(target.position, 10f);
                }
            }
            else
            {
                // Resume following player.
                SetAgentStopped(false);
                _repathTimer -= Time.deltaTime;
                if (_repathTimer <= 0f)
                {
                    _repathTimer = repathInterval;
                    _agent.SetDestination(_player.position);
                }

                // Face movement direction (like ZombieAI.FaceMovementDirection)
                // so the companion looks where it's going, not at the player.
                // This prevents spine twisting when the player runs in circles.
                FaceMovementDirection(8f);
            }
        }

        // Set Speed parameter directly from agent velocity for snappy response.
        // SmoothDamp was too slow — agent reaches full speed in ~0.1s but the
        // smoothed Speed parameter lagged behind by several seconds.
        float targetSpeed = (_agent != null && _agent.isOnNavMesh && _agent.isStopped) ? 0f : Mathf.Clamp01(_agent.velocity.magnitude / followSpeed);
        SetAnimSpeed(targetSpeed);
    }

    private Transform FindNearestEnemy()
    {
        // Use Enemy layer mask (layer 7) so we only detect enemies, not environment.
        // Using ~0 (all layers) fills the buffer with environment colliders and
        // the zombie may not fit in the 32-slot buffer.
        int enemyLayerMask = 1 << LayerMask.NameToLayer("Enemy");
        int count = Physics.OverlapSphereNonAlloc(
            transform.position, detectRange, _enemyBuffer, enemyLayerMask, QueryTriggerInteraction.Ignore);
        Transform best = null;
        float bestSqr = float.MaxValue;
        for (int i = 0; i < count; i++)
        {
            var col = _enemyBuffer[i];
            if (col == null) continue;
            if (!col.CompareTag(enemyTag)) continue;
            // Skip dead enemies.
            var dmg = col.GetComponent<IDamageable>();
            if (dmg is IEnemyHealthReadout readout && readout.IsDead) continue;
            float sqr = (col.transform.position - transform.position).sqrMagnitude;
            if (sqr < bestSqr) { bestSqr = sqr; best = col.transform; }
        }
        return best;
    }

    private void ShootAt(Transform target)
    {
        if (animator != null) animator.CrossFade("Shoot", 0.1f, 0, 0f);
        if (shootClip != null) _audio.PlayOneShot(shootClip);
        _ammoRemaining--;

        Vector3 origin = transform.position + Vector3.up * 1.5f;

        var targetCollider = target.GetComponent<Collider>();
        Vector3 aimPoint;
        if (targetCollider != null)
            aimPoint = targetCollider.bounds.center;
        else
            aimPoint = target.position + Vector3.up * 1f;

        Vector3 baseDir = (aimPoint - origin).normalized;

        int shootMask = ~0;
        int playerLayer = LayerMask.NameToLayer("Player");
        if (playerLayer >= 0) shootMask &= ~(1 << playerLayer);
        int companionLayer = gameObject.layer;
        shootMask &= ~(1 << companionLayer);

        for (int i = 0; i < shotgunPellets; i++)
        {
            Vector3 dir = ApplySpread(baseDir, shotgunSpreadDeg);
            bool hasHit = false;
            Vector3 hitPoint = origin + dir * shotgunRange;

            if (targetCollider != null && targetCollider.Raycast(new Ray(origin, dir), out var targetHit, shotgunRange))
            {
                hasHit = true;
                hitPoint = targetHit.point;
                var dmg = targetHit.collider.GetComponent<IDamageable>();
                if (dmg != null)
                {
                    bool headshot = targetHit.collider.CompareTag("Critical");
                    dmg.Damage(shotgunDamagePerRay, headshot);
                }
            }
            else if (Physics.Raycast(origin, dir, out var hit, shotgunRange, shootMask, QueryTriggerInteraction.Ignore))
            {
                if (!hit.collider.transform.IsChildOf(transform))
                {
                    hasHit = true;
                    hitPoint = hit.point;
                    var dmg = hit.collider.GetComponent<IDamageable>();
                    if (dmg != null)
                    {
                        bool headshot = hit.collider.CompareTag("Critical");
                        dmg.Damage(shotgunDamagePerRay, headshot);
                    }
                }
            }

            SpawnTracer(origin, hitPoint);
            if (hasHit) SpawnImpact(hitPoint);
        }

        // Muzzle flash at the gun barrel (once per shot).
        if (muzzleFlashPrefab != null)
        {
            var fx = Instantiate(muzzleFlashPrefab, origin, Quaternion.LookRotation(baseDir));
            Destroy(fx, 1f);
        }
    }

    private void SpawnTracer(Vector3 from, Vector3 to)
    {
        var go = new GameObject("Tracer");
        var lr = go.AddComponent<LineRenderer>();
        lr.startWidth = 0.04f;
        lr.endWidth = 0.01f;
        lr.positionCount = 2;
        lr.SetPositions(new Vector3[] { from, to });
        lr.material = new Material(Shader.Find("Sprites/Default"));
        lr.startColor = new Color(1f, 0.9f, 0.4f, 0.8f);
        lr.endColor = new Color(1f, 0.6f, 0f, 0f);
        lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        lr.receiveShadows = false;
        Destroy(go, 0.15f);
    }

    private void SpawnImpact(Vector3 point)
    {
        if (impactPrefab != null)
        {
            var impact = Instantiate(impactPrefab, point, Quaternion.identity);
            Destroy(impact, 1.5f);
            return;
        }

        // Runtime spark effect (fallback).
        var spark = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        spark.transform.position = point;
        spark.transform.localScale = Vector3.one * 0.08f;
        Destroy(spark.GetComponent<SphereCollider>());
        var mr = spark.GetComponent<MeshRenderer>();
        mr.material = new Material(Shader.Find("Sprites/Default"));
        mr.material.color = new Color(1f, 0.8f, 0.2f, 1f);
        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        mr.receiveShadows = false;
        Destroy(spark, 0.3f);
    }

    /// <summary>Plays the Reload animation and refills the magazine when complete.</summary>
    private void PlayReload()
    {
        if (animator != null) animator.CrossFade("Reload", 0.15f, 0, 0f);
        // Refill ammo immediately (the reload timer gates the next shot).
        _ammoRemaining = magazineSize;
    }

    private static Vector3 ApplySpread(Vector3 dir, float spreadDeg)
    {
        float spreadRad = spreadDeg * Mathf.Deg2Rad;
        float yaw = Random.Range(-spreadRad, spreadRad);
        float pitch = Random.Range(-spreadRad, spreadRad);
        var rot = Quaternion.Euler(pitch * Mathf.Rad2Deg, yaw * Mathf.Rad2Deg, 0f);
        return rot * dir;
    }

    private void TakeDamageFromNearbyEnemies()
    {
        // Use Enemy layer mask (layer 7) to only detect enemies.
        int enemyLayerMask = 1 << LayerMask.NameToLayer("Enemy");
        int count = Physics.OverlapSphereNonAlloc(
            transform.position, enemyDamageRadius, _enemyBuffer, enemyLayerMask, QueryTriggerInteraction.Ignore);
        int attackers = 0;
        for (int i = 0; i < count; i++)
        {
            var col = _enemyBuffer[i];
            if (col == null) continue;
            if (!col.CompareTag(enemyTag)) continue;
            var readout = col.GetComponent<IEnemyHealthReadout>();
            if (readout != null && readout.IsDead) continue;
            attackers++;
        }
        if (attackers > 0)
        {
            int dmg = enemyDamagePerTick * attackers;
            Damage(dmg, false);
        }
    }

    // ---- Downed / Revive ----

    private void UpdateDowned()
    {
        SetAgentStopped(true);
        SetAnimSpeed(0f);

        // ---- Player rescue (hold E) ----
        // Runs in parallel with the auto-revive timer. If the player holds E
        // within rescueMaxDistance for rescueHoldDuration seconds, the companion
        // is revived at rescueHealthFraction (default full HP). If the timer
        // expires first, auto-revive kicks in at reviveHealthFraction (50%).
        bool rescuing = false;
        if (_player != null)
        {
            // Re-resolve the InputManager if it wasn't found yet (e.g. it was
            // not ready during Start).
            ResolvePlayerInput();
            float dist = Vector3.Distance(transform.position, _player.position);
            // Read the Interacting action from the player's InputManager (Input
            // System). Fallback to Input.GetKey for Input Manager mode.
            bool eHeld = _playerInput != null
                ? _playerInput.Interacting
                : Input.GetKey(rescueKey);
            if (dist <= rescueMaxDistance && eHeld)
            {
                rescuing = true;
                _rescueProgress += Time.deltaTime;
                float normalized = Mathf.Clamp01(_rescueProgress / rescueHoldDuration);
                OnRescueProgressChanged?.Invoke(normalized);
                if (_rescueProgress >= rescueHoldDuration)
                {
                    OnRescueProgressChanged?.Invoke(0f); // Reset UI before revive
                    Revive(rescueHealthFraction, byPlayer: true);
                    return;
                }
            }
        }
        if (!rescuing && _rescueProgress > 0f)
        {
            // Player released E or walked away — reset progress.
            _rescueProgress = 0f;
            OnRescueProgressChanged?.Invoke(0f);
        }

        // ---- Auto-revive timer ----
        _downedTimer -= Time.deltaTime;
        if (_downedTimer <= 0f)
        {
            OnRescueProgressChanged?.Invoke(0f);
            Revive(reviveHealthFraction, byPlayer: false);
        }
    }

    /// <summary>
    /// Revives the companion from the Downed state.
    /// </summary>
    /// <param name="healthFraction">Fraction of maxHealth to restore (0.5 = 50%, 1 = full).</param>
    /// <param name="byPlayer">If true, the companion thanks the player via DialogueBubble.</param>
    private void Revive(float healthFraction, bool byPlayer = false)
    {
        _rescueProgress = 0f;
        currentHealth = Mathf.RoundToInt(maxHealth * Mathf.Clamp01(healthFraction));
        OnHealthChanged?.Invoke(HealthFraction);
        CurrentState = State.Following;
        // Re-enable the interaction collider so the player can talk to the
        // companion again (dialogue prompt reappears).
        if (_interactCollider != null) _interactCollider.enabled = true;
        if (animator != null)
        {
            // Reset Downed bool FIRST so AnyState transition stops firing.
            animator.SetBool(DownedHash, false);
            // Then trigger Revive to play revive animation.
            animator.SetTrigger(ReviveHash);
        }
        if (byPlayer && _bubble != null && !string.IsNullOrEmpty(rescuedThankLine))
        {
            // Show thank-you dialogue, auto-hide after rescuedThankHoldDuration.
            _bubble.ShowSpeech(rescuedThankLine, rescuedThankHoldDuration);
            Debug.Log("[CompanionAI] Rescued by player. Revived at " + (healthFraction * 100f) + "% HP.");
        }
        else
        {
            Debug.Log("[CompanionAI] Auto-revived at " + (healthFraction * 100f) + "% HP.");
        }
    }

    // ---- Walking Away (refused) ----

    private void UpdateWalkingAway()
    {
        SetAgentStopped(false);
        _agent.SetDestination(deadEndPoint);
        SetAnimSpeed(Mathf.Clamp01(_agent.velocity.magnitude / followSpeed));

        float dist = Vector3.Distance(transform.position, deadEndPoint);
        if (dist <= destroyDistance)
        {
            StartCoroutine(FadeAndDestroy());
        }
    }

    private IEnumerator FadeAndDestroy()
    {
        // Fade out visual meshes.
        var smrs = GetComponentsInChildren<SkinnedMeshRenderer>();
        var mrs = GetComponentsInChildren<MeshRenderer>();
        float t = 0f;
        while (t < fadeOutDuration)
        {
            t += Time.deltaTime;
            float a = 1f - (t / fadeOutDuration);
            foreach (var smr in smrs)
            {
                var c = smr.material.color; c.a = a; smr.material.color = c;
            }
            foreach (var mr in mrs)
            {
                var c = mr.material.color; c.a = a; mr.material.color = c;
            }
            yield return null;
        }
        Destroy(gameObject);
    }

    // ---- IDamageable ----

    public void Damage(float damage, bool isHeadshot)
    {
        if (_state == State.Downed) return; // Can't be damaged while downed.
        currentHealth -= Mathf.RoundToInt(damage);
        OnHealthChanged?.Invoke(HealthFraction);

        if (currentHealth <= 1)
        {
            currentHealth = 1;
            EnterDowned();
        }
        else if (animator != null && _state == State.Following)
        {
            // Only trigger Hit reaction when actively following (not Waiting/WalkingAway).
            // Use CrossFade for reliable state transition.
            animator.CrossFade("Hit", 0.1f, 0, 0f);
        }
    }

    private void EnterDowned()
    {
        CurrentState = State.Downed;
        _downedTimer = downedDuration;
        _shootStopTimer = 0f;
        _rescueProgress = 0f;
        OnRescueProgressChanged?.Invoke(0f);
        // Disable the interaction collider so InteractManager stops detecting
        // the companion (no "Nói chuyện" prompt, no E consumption). This frees
        // the E key for the rescue hold in UpdateDowned.
        if (_interactCollider != null) _interactCollider.enabled = false;
        if (animator != null)
        {
            // Reset ALL triggers to prevent stale transitions.
            animator.ResetTrigger(HitHash);
            animator.ResetTrigger(ShootHash);
            animator.ResetTrigger(ReloadHash);
            animator.ResetTrigger(DeathHash);
            animator.ResetTrigger(DashBackHash);
            animator.ResetTrigger(ReviveHash);
            // Set Downed bool LAST so AnyState transition fires correctly.
            animator.SetBool(DownedHash, true);
        }
        Debug.Log("[CompanionAI] Downed! Will revive in " + downedDuration + "s.");
    }

    // ---- Public API (called by CompanionManager / DialogueTrigger) ----

    public void StartFollowing()
    {
        CurrentState = State.Following;
    }

    public void WalkAway(Vector3 destination)
    {
        deadEndPoint = destination;
        CurrentState = State.WalkingAway;
    }

    /// <summary>Teleport the companion near the player (used on chapter changes).</summary>
    public void TeleportNearPlayer(float offset = 2.5f)
    {
        if (_player == null) return;
        _agent.enabled = false;
        Vector3 behind = _player.position - _player.forward * offset;
        if (NavMesh.SamplePosition(behind, out var hit, 5f, NavMesh.AllAreas))
            transform.position = hit.position;
        else
            transform.position = behind;
        _agent.enabled = true;
        SetAgentStopped(true);
        _repathTimer = 0.5f; // Brief pause before re-pathing.
    }

    /// <summary>
    /// Safely sets _agent.isStopped — only when the agent is active and on a
    /// NavMesh. Setting isStopped on an agent not placed on a NavMesh throws
    /// "Stop can only be called on an active agent that has been placed on a
    /// NavMesh" (e.g. right after TeleportNearPlayer re-enables the agent but
    /// before it has sampled a position on the NavMesh).
    /// </summary>
    private void SetAgentStopped(bool stopped)
    {
        if (_agent == null || !_agent.enabled || !_agent.isOnNavMesh) return;
        _agent.isStopped = stopped;
    }

    private void SetAnimSpeed(float speed)
    {
        if (animator == null) return;
        float dt = Mathf.Max(Time.deltaTime, 0.0001f);
        _currentAnimSpeed = Mathf.SmoothDamp(_currentAnimSpeed, speed, ref _speedVelocity, speedSmoothTime, float.MaxValue, dt);
        animator.SetFloat(SpeedHash, _currentAnimSpeed);
    }

    /// <summary>
    /// Smoothly rotates the companion to face the given world position.
    /// Mirrors EnemyLocomotion.FaceTarget — used to face enemies while shooting.
    /// </summary>
    private void FacePosition(Vector3 worldPos, float rotSpeed)
    {
        Vector3 dir = worldPos - transform.position;
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.01f) return;
        Quaternion rot = Quaternion.LookRotation(dir);
        transform.rotation = Quaternion.Slerp(transform.rotation, rot, Time.deltaTime * rotSpeed);
    }

    /// <summary>
    /// Smoothly rotates the companion to face its movement direction (NavMeshAgent
    /// velocity or steering target). Mirrors EnemyLocomotion.FaceMovementDirection —
    /// used while chasing so the companion looks where it's going, not at the
    /// player. Prevents spine twisting when the player runs in circles.
    /// </summary>
    private void FaceMovementDirection(float rotSpeed)
    {
        Vector3 moveDir = Vector3.zero;
        if (_agent.velocity.sqrMagnitude > 0.5f)
            moveDir = _agent.velocity;
        else if (_agent.hasPath)
            moveDir = _agent.steeringTarget - transform.position;

        moveDir.y = 0f;
        if (moveDir.sqrMagnitude < 0.01f)
        {
            // Not moving — face the player instead (idle pose).
            if (_player != null) FacePosition(_player.position, rotSpeed);
            return;
        }

        Quaternion rot = Quaternion.LookRotation(moveDir.normalized);
        transform.rotation = Quaternion.Slerp(transform.rotation, rot, Time.deltaTime * rotSpeed);
    }

    /// <summary>
    /// Pins the left hand to the shotgun forestock via IK so the companion
    /// always grips the rifle with both hands, regardless of animation pose.
    /// </summary>
    private void OnAnimatorIK(int layerIndex)
    {
        if (animator == null || _leftHandGrip == null) return;

        float targetWeight = (_state == State.Downed) ? 0f : 1f;
        _ikWeight = Mathf.MoveTowards(_ikWeight, targetWeight, Time.deltaTime * 8f);
        animator.SetIKPositionWeight(AvatarIKGoal.LeftHand, _ikWeight);
        animator.SetIKRotationWeight(AvatarIKGoal.LeftHand, _ikWeight);
        if (_ikWeight > 0.01f)
        {
            animator.SetIKPosition(AvatarIKGoal.LeftHand, _leftHandGrip.position);
            animator.SetIKRotation(AvatarIKGoal.LeftHand, _leftHandGrip.rotation);
        }
    }
}

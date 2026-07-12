using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class EnemyLocomotion : MonoBehaviour
{
    [Header("Target")]
    public Transform target;

    [Header("Line of Sight & Detection")]
    public bool requireLineOfSight = true;
    public LayerMask sightObstructionMask = ~0;
    public float sightEyeHeight = 1.5f;

    [Header("Stuck Recovery")]
    public float stuckTimeThreshold = 3f;
    public float stuckMoveThreshold = 1f;
    public float stuckRepathRadius = 5f;

    [Header("Chase Re-path (Responsiveness)")]
    public float playerMovedRepathThreshold = 1f;
    public float maxRepathInterval = 0.1f;

    [Header("Direct Steering")]
    public bool useDirectSteeringWhenLOS = false;
    public float directSteeringWallCheckDistance = 1.5f;
    public float directSteeringLOSCacheInterval = 0.15f;
    public float directSteeringWallCooldown = 1.5f;
    public float wallCheckBodyHeight = 0.5f;
    public float maxDirectSteerHeightDiff = 2f;

    public NavMeshAgent Agent { get; private set; }
    
    public bool IsRecoveringFromStuck => _stuckRecoveryCooldownTimer > 0f;

    // Runtime state variables for pathfinding, stuck checks, and direct steering
    private NavMeshPath _reusablePath;
    private int _cachedLOSMask = -1;
    private int _cachedLOSCacheKey = -1;
    private bool _cachedLOSResult;
    private float _nextLOSCheckTime;

    private float _stuckTimer;
    private Vector3 _lastStuckCheckPos;
    private int _noPathRetryCount;
    private float _noPathRetryTimer;

    private bool _isDirectSteering;
    private float _directSteeringCooldownTimer;
    private float _losCacheTimer;
    private Vector3 _prevFramePos;
    private float _stuckRecoveryCooldownTimer;

    public bool IsDirectSteering => _isDirectSteering;

    private void Awake()
    {
        Agent = GetComponent<NavMeshAgent>();
        _reusablePath = new NavMeshPath();
    }

    public void Initialize()
    {
        _stuckTimer = 0f;
        _lastStuckCheckPos = transform.position;
        _noPathRetryCount = 0;
        _noPathRetryTimer = 0f;
        _directSteeringCooldownTimer = 0f;
        _losCacheTimer = directSteeringLOSCacheInterval;
        _prevFramePos = transform.position;
        _stuckRecoveryCooldownTimer = 0f;

        WarpIfUnderground();
    }

    public void WarpIfUnderground()
    {
        if (Agent != null && target != null)
        {
            float targetY = target.position.y;
            if (transform.position.y < targetY - 4f)
            {
                _reusablePath.ClearCorners();
                if (NavMesh.CalculatePath(transform.position, target.position, NavMesh.AllAreas, _reusablePath) 
                    && _reusablePath.status != NavMeshPathStatus.PathComplete)
                {
                    NavMeshHit groundHit;
                    Vector3 searchFrom = new Vector3(transform.position.x, targetY, transform.position.z);
                    if (NavMesh.SamplePosition(searchFrom, out groundHit, 15f, NavMesh.AllAreas))
                    {
                        Agent.Warp(groundHit.position);
                        transform.position = groundHit.position;
                    }
                }
            }
        }
    }

    public bool HasLineOfSight()
    {
        if (target == null) return false;

        if (Time.time < _nextLOSCheckTime)
        {
            return _cachedLOSResult;
        }

        Vector3 start = transform.position + Vector3.up * sightEyeHeight;
        Vector3 end = target.position + Vector3.up * sightEyeHeight;
        Vector3 dir = end - start;
        float dist = dir.magnitude;
        if (dist <= 0.1f)
        {
            _cachedLOSResult = true;
            _nextLOSCheckTime = Time.time + Random.Range(0.12f, 0.18f);
            return true;
        }

        int myLayer = gameObject.layer;
        int targetLayer = target.gameObject.layer;
        int cacheKey = (myLayer << 8) | targetLayer;
        if (_cachedLOSMask == -1 || _cachedLOSCacheKey != cacheKey)
        {
            _cachedLOSMask = sightObstructionMask & ~(1 << targetLayer) & ~(1 << myLayer);
            _cachedLOSCacheKey = cacheKey;
        }

        _cachedLOSResult = !Physics.Raycast(start, dir.normalized, dist, _cachedLOSMask, QueryTriggerInteraction.Ignore);
        _nextLOSCheckTime = Time.time + Random.Range(0.12f, 0.18f);

        return _cachedLOSResult;
    }

    public void ForceRecalculateLOS()
    {
        _nextLOSCheckTime = 0f;
    }

    public void SetDestinationRobust(Vector3 destination)
    {
        if (Agent == null || !Agent.isOnNavMesh) return;
        bool hadNoPath = !Agent.hasPath && !Agent.pathPending;
        Agent.isStopped = false;
        Agent.SetDestination(destination);

        if (hadNoPath && !Agent.pathPending)
        {
            _reusablePath.ClearCorners();
            if (NavMesh.CalculatePath(transform.position, destination, NavMesh.AllAreas, _reusablePath)
                && (_reusablePath.status == NavMeshPathStatus.PathComplete || _reusablePath.status == NavMeshPathStatus.PathPartial))
            {
                Agent.SetPath(_reusablePath);
            }
        }
    }

    public bool TryDirectSteer(float speed)
    {
        // Completely disable Direct Steering to rely 100% on NavMesh pathfinding.
        // This prevents railing stuck/staring loops and ensures robust collision physics.
        return false;

        if (target == null || Agent == null || !Agent.isOnNavMesh) return false;

        // If recovering from being stuck, bypass direct steering so agent can pathfind out
        if (IsRecoveringFromStuck)
        {
            ExitDirectSteering();
            return false;
        }

        float heightDiff = Mathf.Abs(target.position.y - transform.position.y);
        if (heightDiff > maxDirectSteerHeightDiff)
        {
            ExitDirectSteering();
            return false;
        }

        if (_directSteeringCooldownTimer > 0f)
        {
            _directSteeringCooldownTimer -= Time.deltaTime;
            ExitDirectSteering();
            return false;
        }

        _losCacheTimer -= Time.deltaTime;
        if (_losCacheTimer <= 0f)
        {
            _cachedLOSResult = HasLineOfSight();
            _losCacheTimer = directSteeringLOSCacheInterval;
        }

        if (!_cachedLOSResult)
        {
            ExitDirectSteering();
            return false;
        }

        // 1. Check NavMesh connectivity (Fix A)
        if (Agent.Raycast(target.position, out _))
        {
            ExitDirectSteering();
            return false;
        }

        _isDirectSteering = true;
        Agent.isStopped = true;
        Agent.updatePosition = false;

        Vector3 dir = target.position - transform.position;
        float distMag = dir.magnitude;
        if (distMag < 0.01f) return true;

        Vector3 dirNorm = dir / distMag;

        // 2. Compute slope-aligned direction vector to prevent terrain collisions (Fix C)
        Vector3 groundNormal = Vector3.up;
        if (Physics.Raycast(transform.position + Vector3.up * 0.5f, Vector3.down, out RaycastHit groundHit, 1.0f, _cachedLOSMask))
        {
            groundNormal = groundHit.normal;
        }
        Vector3 moveDir = Vector3.ProjectOnPlane(dirNorm, groundNormal).normalized;

        // 3. Slope-aligned physics raycasts
        if (Physics.Raycast(transform.position + Vector3.up * sightEyeHeight, moveDir, out _, directSteeringWallCheckDistance, _cachedLOSMask, QueryTriggerInteraction.Ignore) ||
            Physics.Raycast(transform.position + Vector3.up * wallCheckBodyHeight, moveDir, out _, directSteeringWallCheckDistance, _cachedLOSMask, QueryTriggerInteraction.Ignore))
        {
            ExitDirectSteering();
            _directSteeringCooldownTimer = directSteeringWallCooldown;
            return false;
        }

        // 4. Calculate movement step
        Vector3 newPos = transform.position + moveDir * speed * Time.deltaTime;
        
        // 5. Use frame-rate-independent search radius to prevent stutter (Fix C)
        float sampleRadius = Mathf.Max(0.5f, speed * Time.deltaTime * 1.5f);
        
        NavMeshHit navHit;
        if (NavMesh.SamplePosition(newPos, out navHit, sampleRadius, NavMesh.AllAreas))
        {
            if (Mathf.Abs(navHit.position.y - transform.position.y) <= 1.5f)
            {
                // Progress check to detect if we are blocked by a NavMesh boundary (e.g. gap/wall)
                float expectedDistance = speed * Time.deltaTime;
                float actualDistance = Vector3.Distance(transform.position, navHit.position);
                if (actualDistance < expectedDistance * 0.3f)
                {
                    ExitDirectSteering();
                    _directSteeringCooldownTimer = directSteeringWallCooldown;
                    return false;
                }

                transform.position = navHit.position;
                Agent.nextPosition = navHit.position;
                return true;
            }
        }

        ExitDirectSteering();
        return false;
    }

    public void ExitDirectSteering()
    {
        if (_isDirectSteering)
        {
            _isDirectSteering = false;
            Agent.isStopped = false;
            SyncAgentToTransform();
            Agent.updatePosition = true;
        }
    }

    public void SyncAgentToTransform()
    {
        if (Agent == null || !Agent.isOnNavMesh) return;
        NavMeshHit hit;
        if (NavMesh.SamplePosition(transform.position, out hit, 2f, NavMesh.AllAreas))
            Agent.nextPosition = hit.position;
        else
            Agent.nextPosition = transform.position;
    }

    public void FaceTarget(float rotSpeed)
    {
        if (target == null) return;
        Vector3 dir = target.position - transform.position;
        dir.y = 0;
        if (dir.sqrMagnitude < 0.01f) return;

        Quaternion rot = Quaternion.LookRotation(dir);
        transform.rotation = Quaternion.Slerp(transform.rotation, rot, Time.deltaTime * rotSpeed);
    }

    public void FaceMovementDirection(float rotSpeed)
    {
        Vector3 moveDir = Vector3.zero;
        if (_isDirectSteering)
            moveDir = transform.position - _prevFramePos;
        else if (Agent.velocity.sqrMagnitude > 0.5f)
            moveDir = Agent.velocity;
        else if (Agent.hasPath)
            moveDir = Agent.steeringTarget - transform.position;

        moveDir.y = 0f;
        if (moveDir.sqrMagnitude < 0.01f)
        {
            FaceTarget(rotSpeed);
            return;
        }

        Quaternion rot = Quaternion.LookRotation(moveDir.normalized);
        transform.rotation = Quaternion.Slerp(transform.rotation, rot, Time.deltaTime * rotSpeed);
    }

    public void HandleStuckDetection(float distanceToPlayer, float stoppingDistance)
    {
        if (distanceToPlayer <= stoppingDistance + 0.5f)
        {
            _stuckTimer = 0f;
            _lastStuckCheckPos = transform.position;
            _noPathRetryCount = 0;
            _noPathRetryTimer = 0f;
            return;
        }

        if (Agent != null && Agent.isOnNavMesh && !Agent.hasPath && !Agent.pathPending && !Agent.isStopped)
        {
            _noPathRetryCount++;
            _noPathRetryTimer += Time.deltaTime;
            if (_noPathRetryTimer >= 0.5f)
            {
                SetDestinationRobust(target.position);
                _noPathRetryTimer = 0f;
            }

            if (_noPathRetryCount >= 300 && !Agent.hasPath)
            {
                Vector3 currentPos = transform.position;
                Agent.enabled = false;
                Agent.enabled = true;
                if (Agent.isOnNavMesh)
                {
                    Agent.Warp(currentPos);
                    Agent.isStopped = false;
                    SetDestinationRobust(target.position);
                }
                _noPathRetryCount = 0;
                _noPathRetryTimer = 0f;
            }

            _stuckTimer = 0f;
            _lastStuckCheckPos = transform.position;
            return;
        }

        _noPathRetryCount = 0;
        _noPathRetryTimer = 0f;
        float moved = Vector3.Distance(transform.position, _lastStuckCheckPos);

        if (moved < stuckMoveThreshold * 0.33f)
        {
            _stuckTimer += Time.deltaTime;
        }
        else
        {
            _stuckTimer = 0f;
            _lastStuckCheckPos = transform.position;
        }

        if (_stuckTimer >= stuckTimeThreshold)
        {
            TryRecoverFromStuck();
            _stuckTimer = 0f;
            _lastStuckCheckPos = transform.position;
        }
    }

    private void TryRecoverFromStuck()
    {
        if (Agent == null || target == null) return;
        Debug.Log($"[EnemyLocomotion] {name} is stuck! Running stuck recovery.");
        _stuckRecoveryCooldownTimer = 1.5f; // Lock normal chasing re-pathing for 1.5s

        Vector3 toPlayer = target.position - transform.position;
        Vector3 midPoint = transform.position + toPlayer * 0.5f;

        NavMeshHit midHit;
        if (NavMesh.SamplePosition(midPoint, out midHit, 3f, NavMesh.AllAreas))
        {
            _reusablePath.ClearCorners();
            if (NavMesh.CalculatePath(transform.position, midHit.position, NavMesh.AllAreas, _reusablePath)
                && _reusablePath.status == NavMeshPathStatus.PathComplete)
            {
                Agent.SetPath(_reusablePath);
                return;
            }
        }

        for (int attempt = 0; attempt < 3; attempt++)
        {
            Vector2 rand = Random.insideUnitCircle * 2f;
            Vector3 candidate = transform.position + new Vector3(rand.x, 0f, rand.y);
            NavMeshHit hit;
            if (!NavMesh.SamplePosition(candidate, out hit, 1.5f, NavMesh.AllAreas)) continue;

            _reusablePath.ClearCorners();
            if (NavMesh.CalculatePath(transform.position, hit.position, NavMesh.AllAreas, _reusablePath)
                && _reusablePath.status == NavMeshPathStatus.PathComplete)
            {
                Agent.SetPath(_reusablePath);
                return;
            }
        }
    }

    public static Vector3 RandomNavSphere(Vector3 origin, float distance)
    {
        Vector3 randomDirection = Random.insideUnitSphere * distance;
        randomDirection += origin;
        NavMeshHit hit;

        if (NavMesh.SamplePosition(randomDirection, out hit, distance, NavMesh.AllAreas))
        {
            if (hit.position.y < origin.y - 3f)
            {
                Vector3 groundLevel = new Vector3(randomDirection.x, origin.y, randomDirection.z);
                if (NavMesh.SamplePosition(groundLevel, out hit, distance, NavMesh.AllAreas) && hit.position.y >= origin.y - 3f)
                    return hit.position;
            }
            else
            {
                return hit.position;
            }
        }
        return origin;
    }

    private void LateUpdate()
    {
        if (_stuckRecoveryCooldownTimer > 0f)
        {
            _stuckRecoveryCooldownTimer -= Time.deltaTime;
        }
        _prevFramePos = transform.position;
    }
}

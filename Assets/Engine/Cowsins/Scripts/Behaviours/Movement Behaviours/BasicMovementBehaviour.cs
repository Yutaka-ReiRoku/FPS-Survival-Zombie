using cowsins;
using UnityEngine;
using System; 

public class BasicMovementBehaviour
{
    private MovementContext context;
    private Rigidbody rb;
    private InputManager inputManager;
    private IPlayerMovementStateProvider playerMovement;
    private IPlayerMovementEventsProvider playerEvents;
    private IPlayerControlProvider playerControl;
    private IWeaponReferenceProvider weaponReference;
    private IWeaponBehaviourProvider weaponController;
    private IPlayerMultipliers playerMultipliers;

    private PlayerOrientation orientation => playerMovement?.Orientation;

    private Vector3 moveDirection;
    private PlayerMovementSettings playerSettings;

    private CapsuleCollider playerCapsuleCollider;

    private const float frictionThreshold = 0.1f;
    private const float slopeGravityMultiplier = 150;
    private const float extraGravityMultiplier = 10f;
    private bool wasMovingLastFrame;

    public BasicMovementBehaviour(MovementContext context)
    {
        this.context = context;
        this.rb = context.Rigidbody;
        this.inputManager = context.InputManager;

        this.playerMovement = context.Dependencies.PlayerMovementState;
        this.playerEvents = context.Dependencies.PlayerMovementEvents;
        this.playerControl = context.Dependencies.PlayerControl;
        this.weaponReference = context.Dependencies.WeaponReference;
        this.weaponController = context.Dependencies.WeaponBehaviour;
        this.playerMultipliers = context.Dependencies.PlayerMultipliers;

        this.playerSettings = context.Settings;

        this.playerCapsuleCollider = context.Capsule;
    }

    /// <summary>
    /// Handle all the basics related to the movement of the player.
    /// </summary>
    public void Movement()
    {
        if(!playerControl.IsMovementControllable) return;

        //Find actual velocity relative to where player is looking
        Vector2 relativeVelocity = FindVelRelativeToLook();

        // Counteract sliding and sloppy movement.
        FrictionForce(inputManager.X, inputManager.Y, relativeVelocity);
        //If speed is larger than maxspeed, clamp the velocity so you don't go over max speed
        LimitDiagonalVelocity();

        // Only zero horizontal velocity when grounded and not on a slope/stairs.
        // Zeroing Y velocity on stairs causes the player to lose momentum on each step.
        if (rb.linearVelocity.magnitude < .1f && playerMovement.Grounded && !context.IsPlayerOnSlope)
        {
            rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);
        }

        if (!playerControl.IsControllable)
        {
            if (playerMovement.Grounded) rb.linearVelocity = Vector3.zero;
            return;
        }

        Vector3 horizontalVel = new Vector3(rb.linearVelocity.x, 0, rb.linearVelocity.z);
        bool isCrouchSliding = playerMovement.IsCrouching && horizontalVel.magnitude >= playerMovement.CrouchSpeed;

        if (isCrouchSliding && !playerSettings.allowMoveWhileSliding) return;

        float airborneMultiplier = !playerMovement.Grounded ? playerSettings.controlAirborne : 1;
        float movementMultipliers = playerSettings.acceleration * Time.deltaTime * airborneMultiplier;

        // Reduce movement influence while sliding if sliding is active and movement while sliding isn't allowed
        if (isCrouchSliding && !playerSettings.allowMoveWhileSliding)
            movementMultipliers *= 0f;

        CalculateMoveDirection();
        CallEvents();

        // If crouch-sliding, respect steering multiplier and don't add full movement force
        movementMultipliers *= isCrouchSliding ? playerSettings.slideSteerMultiplier : 1; 

        rb.AddForce(moveDirection * movementMultipliers);

        // Proactively follow ground height ahead for smooth stair/terrain climbing
        StepAssist();
        SnapToGround();
    }

    private void CalculateMoveDirection()
    {
        if (context.IsPlayerOnSlope)
        {
            moveDirection = GetSlopeDirection();

            if (moveDirection.magnitude == 0 && !context.HasJumped)
            {
                float slopeAngle = context.IsPlayerOnSlope ? Vector3.Angle(Vector3.up, context.SlopeHit.normal) : 0f;
                if (context.IsPlayerOnSlope && slopeAngle <= 45f)
                {
                    // Zero out all velocity to stop sliding completely when standing still on stable slopes
                    rb.linearVelocity = Vector3.zero;
                }
                else
                {
                    // Fallback for steep slopes or non-slope surfaces
                    rb.linearVelocity = new Vector3(0f, Mathf.Min(rb.linearVelocity.y, 0f), 0f);
                }
            }
        }
        else
        {
            moveDirection = (orientation.Forward * inputManager.Y + orientation.Right * inputManager.X).normalized;
        }
    }

    private void CallEvents()
    {
        bool isMoving = moveDirection.magnitude > .1f;

        if (isMoving && !wasMovingLastFrame)
        {
            // Transition Idle -> Moving
            playerEvents.Events.OnIdleToMove?.Invoke();
        }
        else if (!isMoving && wasMovingLastFrame)
        {
            // Transition Moving -> Idle
            playerEvents.Events.OnMovingToIdle?.Invoke();
        }


        if (moveDirection.magnitude > .1f)
        {
            playerEvents.Events.OnMoving?.Invoke();
            playerSettings.events.OnMoving?.Invoke();
        }
        else
        {
            playerEvents.Events.OnIdle?.Invoke();
            playerSettings.events.OnIdle?.Invoke();
        }
    }


    /// <summary>
    /// Limits diagonal velocity
    /// </summary>
    private void LimitDiagonalVelocity()
    {
        float currentWeightedSpeed = playerMovement.CurrentSpeed * playerMultipliers.WeightMultiplier;
        
        if (context.IsPlayerOnSlope)
        {
            if (rb.linearVelocity.magnitude > currentWeightedSpeed)
            {
                rb.linearVelocity = rb.linearVelocity.normalized * currentWeightedSpeed;
            }
        }
        else
        {
            Vector3 horizontalVelocity = new Vector3(rb.linearVelocity.x, 0, rb.linearVelocity.z);
            if (horizontalVelocity.magnitude > currentWeightedSpeed)
            {
                horizontalVelocity = horizontalVelocity.normalized * currentWeightedSpeed;
                rb.linearVelocity = new Vector3(horizontalVelocity.x, rb.linearVelocity.y, horizontalVelocity.z);
            }
        }
    }

    /// <summary>
    /// Find the velocity relative to where the player is looking
    /// Useful for vectors calculations regarding movement and limiting movement
    /// </summary>
    /// <returns></returns>
    private Vector2 FindVelRelativeToLook()
    {
        // Convert velocity to local space relative to the player's look direction
        Vector3 localVel = Quaternion.Euler(0, -orientation.Yaw, 0) * rb.linearVelocity;
        return new Vector2(localVel.x, localVel.z);
    }

    /// <summary>
    /// Get the direction of movement in a slope
    /// </summary>
    /// <returns></returns>
    private Vector3 GetSlopeDirection()
    {
        // Use the ground raycast normal recorded in the shared context
        return Vector3.ProjectOnPlane(orientation.Forward * inputManager.Y + orientation.Right * inputManager.X, context.SlopeHit.normal).normalized;
    }

    /// <summary>
    /// Add friction force to the player when it�s not airborne
    /// Please note that it counters movement, since it goes in the opposite direction to velocity
    /// </summary>
    private void FrictionForce(float x, float y, Vector2 mag)
    {
        // Prevent from adding friction on an airborne body
        if (!playerMovement.Grounded || inputManager.Jumping || context.HasJumped) return;

        Vector3 horizontalVel = new Vector3(rb.linearVelocity.x, 0, rb.linearVelocity.z);
        bool isCrouchSliding = playerMovement.IsCrouching && horizontalVel.magnitude >= playerMovement.CrouchSpeed;

        // If crouch-sliding and sliding friction is disabled, skip friction here. Sliding behaviour handles deceleration.
        if (isCrouchSliding && !playerSettings.applyFrictionForceOnSliding) return;

        float friction = isCrouchSliding ? playerSettings.slideFrictionForceAmount : playerSettings.controlsResponsiveness;

        // Counter movement ( Friction while moving )
        // Prevent from sliding not on purpose
        if (Math.Abs(mag.x) > frictionThreshold && Math.Abs(x) < 0.5f || (mag.x < -frictionThreshold && x > 0) || (mag.x > frictionThreshold && x < 0))
        {
            rb.AddForce(playerSettings.acceleration * orientation.Right * Time.deltaTime * -mag.x * friction);
        }
        if (Math.Abs(mag.y) > frictionThreshold && Math.Abs(y) < 0.05f || (mag.y < -frictionThreshold && y > 0) || (mag.y > frictionThreshold && y < 0))
        {
            rb.AddForce(playerSettings.acceleration * orientation.Forward * Time.deltaTime * -mag.y * friction);
        }
    }

    /// <summary>
    /// Helps the player climb stairs and rough terrain by testing whether the capsule
    /// can physically fit at increasing heights. When the player is blocked at ground
    /// level but CAN fit when raised slightly, the player is lifted to that height.
    /// This directly tests collision geometry rather than guessing from raycasts,
    /// making it reliable with any mesh shape (solid stairs, rough terrain, etc.)
    /// </summary>
    private void StepAssist()
    {
        if (playerMovement.IsCrouching || playerMovement.IsSliding) return;
        if (context.HasJumped || inputManager.Jumping) return;
        if (!playerMovement.Grounded) return;

        // Need horizontal input
        Vector3 inputDir = (orientation.Forward * inputManager.Y + orientation.Right * inputManager.X);
        inputDir.y = 0;
        if (inputDir.magnitude < 0.1f) return;
        inputDir = inputDir.normalized;

        float stepH = playerSettings.stepHeight;
        if (stepH <= 0) return;
        if (rb.linearVelocity.y > 0.5f) return;

        // Proactive Raycast checking
        Vector3 rayOrigin = rb.position + Vector3.up * 0.05f;
        float checkDistance = playerCapsuleCollider.radius + 0.15f;
        
        if (Physics.Raycast(rayOrigin, inputDir, out RaycastHit hit, checkDistance, context.WhatIsGround, QueryTriggerInteraction.Ignore))
        {
            float obstacleAngle = Vector3.Angle(Vector3.up, hit.normal);
            if (obstacleAngle > 45f) // A steep vertical obstacle
            {
                // 1. Calculate actual step height landing point first
                Vector3 downRayOrigin = rb.position + inputDir * checkDistance + Vector3.up * (stepH + 0.05f);
                if (Physics.Raycast(downRayOrigin, Vector3.down, out RaycastHit downHit, stepH + 0.1f, context.WhatIsGround, QueryTriggerInteraction.Ignore))
                {
                    float stepHeightActual = downHit.point.y - rb.position.y;
                    if (stepHeightActual > 0.01f && stepHeightActual <= stepH)
                    {
                        // 2. Ceiling Clearance Sweep Check based on actual step height rather than max height
                        float radius = playerCapsuleCollider.radius;
                        Vector3 center = rb.position + playerCapsuleCollider.center;
                        float halfHeight = Mathf.Max(0, (playerCapsuleCollider.height * 0.5f) - radius);
                        Vector3 point0 = center + Vector3.up * halfHeight;
                        Vector3 point1 = center - Vector3.up * halfHeight;
                        
                        bool ceilingBlocked = Physics.CapsuleCast(
                            point1, point0, radius, Vector3.up, out RaycastHit ceilingHit,
                            stepHeightActual, context.WhatIsGround, QueryTriggerInteraction.Ignore
                        );
                        
                        if (ceilingBlocked) return; // Headroom blocked, abort step-up

                        // 3. Capsule overlap check at final destination to verify the player fits
                        Vector3 targetCenter = center + inputDir * checkDistance + Vector3.up * stepHeightActual;
                        bool targetBlocked = Physics.CheckCapsule(
                            targetCenter + Vector3.up * halfHeight,
                            targetCenter - Vector3.up * halfHeight,
                            radius * 0.95f,
                            context.WhatIsGround,
                            QueryTriggerInteraction.Ignore
                        );

                        if (!targetBlocked)
                        {
                            // Instantly step up to preserve Rigidbody velocity
                            rb.MovePosition(new Vector3(rb.position.x, downHit.point.y + 0.01f, rb.position.z));
                            rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
                            (playerMovement as PlayerMovement)?.OnStepClimb.Invoke(stepHeightActual);
                        }
                    }
                }
            }
        }
    }

    private void SnapToGround()
    {
        if (context.HasJumped || inputManager.Jumping) return;
        if (playerMovement.IsClimbing || playerMovement.IsWallRunning || playerMovement.IsDashing) return;
        if (!playerMovement.Grounded) return;
        if (rb.linearVelocity.y > 0.1f) return;

        float maxStepSnap = Mathf.Min(playerSettings.stepHeight, 0.4f);
        float checkDist = maxStepSnap + 0.1f;
        Vector3 castOrigin = rb.position + Vector3.up * 0.1f;
        if (Physics.Raycast(castOrigin, Vector3.down, out RaycastHit hit, checkDist + 0.1f, context.WhatIsGround, QueryTriggerInteraction.Ignore))
        {
            float distance = castOrigin.y - hit.point.y;
            if (distance > 0.1f && distance <= (maxStepSnap + 0.1f))
            {
                float angle = Vector3.Angle(Vector3.up, hit.normal);
                if (angle < 60f) // Matches max slope angle
                {
                    rb.MovePosition(new Vector3(rb.position.x, hit.point.y, rb.position.z));
                    Vector3 vel = rb.linearVelocity;
                    if (vel.y < 0)
                    {
                        vel.y = 0;
                        rb.linearVelocity = vel;
                    }
                }
            }
        }
    }
}

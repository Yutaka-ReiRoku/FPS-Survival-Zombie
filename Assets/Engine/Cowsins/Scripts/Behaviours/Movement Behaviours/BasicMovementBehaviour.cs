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

        //Extra gravity
        rb.AddForce(Vector3.down * Time.fixedDeltaTime * extraGravityMultiplier);

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

        movementMultipliers *= context.IsPlayerOnSlope ? 2f : 1;
        // If crouch-sliding, respect steering multiplier and don't add full movement force
        movementMultipliers *= isCrouchSliding ? playerSettings.slideSteerMultiplier : 1; 

        rb.AddForce(moveDirection * movementMultipliers);

        // Proactively follow ground height ahead for smooth stair/terrain climbing
        StepAssist();
    }

    private void CalculateMoveDirection()
    {
        if (context.IsPlayerOnSlope)
        {
            moveDirection = GetSlopeDirection();

            if (moveDirection.magnitude == 0 && !context.HasJumped)
            {
                // Only zero horizontal velocity to prevent sliding down slopes,
                // but preserve downward Y velocity so gravity can pull the player
                // onto the ground. Zeroing Y velocity caused the player to float
                // and lose the ability to jump when standing still on slopes.
                rb.linearVelocity = new Vector3(0f, Mathf.Min(rb.linearVelocity.y, 0f), 0f);
            }
            if (rb.linearVelocity.y != 0 && moveDirection.magnitude != 0) rb.AddForce(Vector3.down * slopeGravityMultiplier);
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
        Vector3 horizontalVelocity = new Vector3(rb.linearVelocity.x, 0, rb.linearVelocity.z);
        float currentWeightedSpeed = playerMovement.CurrentSpeed * playerMultipliers.WeightMultiplier;
        if (horizontalVelocity.magnitude > currentWeightedSpeed)
        {
            horizontalVelocity = horizontalVelocity.normalized * currentWeightedSpeed;
            rb.linearVelocity = new Vector3(horizontalVelocity.x, rb.linearVelocity.y, horizontalVelocity.z);
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

        // Only assist when the player is actually blocked (velocity significantly below target speed)
        Vector3 horizontalVel = new Vector3(rb.linearVelocity.x, 0, rb.linearVelocity.z);
        float currentTargetSpeed = playerMovement.CurrentSpeed;
        if (currentTargetSpeed > 0 && horizontalVel.magnitude > currentTargetSpeed * 0.7f) return;

        // Don't assist if already moving upward (e.g. mid-jump)
        if (rb.linearVelocity.y > 0.5f) return;

        // Capsule dimensions for CheckCapsule
        float radius = playerCapsuleCollider.radius;
        Vector3 center = rb.position + playerCapsuleCollider.center;
        float halfHeight = Mathf.Max(0, (playerCapsuleCollider.height * 0.5f) - radius);
        Vector3 point0 = center + Vector3.up * halfHeight;
        Vector3 point1 = center - Vector3.up * halfHeight;

        // Test at increasing heights to find the minimum lift needed to clear the obstacle.
        // At each height, check if the capsule can fit AND move forward (no collision ahead).
        float testIncrement = 0.1f;
        float forwardCheck = 0.1f;

        for (float testLift = testIncrement; testLift <= stepH; testLift += testIncrement)
        {
            // Check if the capsule can fit at this raised position + slightly forward
            Vector3 testCenter = center + Vector3.up * testLift + inputDir * forwardCheck;
            Vector3 testP0 = testCenter + Vector3.up * halfHeight;
            Vector3 testP1 = testCenter - Vector3.up * halfHeight;

            if (!Physics.CheckCapsule(testP0, testP1, radius, context.WhatIsGround, QueryTriggerInteraction.Ignore))
            {
                // The capsule fits at this height! Lift the player up gradually.
                float liftThisFrame = Mathf.Min(playerSettings.stepAssistForce * Time.fixedDeltaTime, testLift);
                rb.MovePosition(rb.position + Vector3.up * liftThisFrame);
                return;
            }
        }
    }
}

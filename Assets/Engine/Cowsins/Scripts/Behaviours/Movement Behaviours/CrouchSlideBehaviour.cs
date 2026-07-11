using cowsins;
using UnityEngine;

public class CrouchSlideBehaviour
{
    private MovementContext context;
    private Rigidbody rb;
    private InputManager inputManager;
    private IPlayerMovementStateProvider playerMovement;
    private IPlayerMovementEventsProvider playerEvents;

    private PlayerMovementSettings playerSettings;

    private PlayerOrientation orientation => playerMovement.Orientation;
    private float initialHeight;
    private Vector3 initialCenter;
    private Transform cameraHead;
    private float initialHeadLocalY;
    private bool canUnCrouch = false;
    private float slideTimer = 0f;
    private Vector3 slideDirection = Vector3.zero;
    private float slideBoostRemaining = 0f;
    private bool isBoosting = false;

    public CrouchSlideBehaviour(MovementContext context)
    {
        this.context = context;
        if (context == null || context.Transform == null) return;

        this.rb = context.Rigidbody;
        this.inputManager = context.InputManager;

        this.playerMovement = context.Dependencies.PlayerMovementState;
        this.playerEvents = context.Dependencies.PlayerMovementEvents;

        this.playerSettings = context.Settings;
        this.initialHeight = context.Capsule.height;
        this.initialCenter = context.Capsule.center;

        // Safe camera head detection
        cameraHead = context.Transform.Find("Head ( Camera Placement )");
        if (cameraHead == null)
        {
            var allTransforms = context.Transform.GetComponentsInChildren<Transform>(true);
            foreach (var t in allTransforms)
            {
                if (t != context.Transform && t.name.IndexOf("Head", System.StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    cameraHead = t;
                    break;
                }
            }
        }

        if (cameraHead != null)
        {
            initialHeadLocalY = cameraHead.localPosition.y;
        }
        else
        {
            initialHeadLocalY = 1.60f; // Fallback
        }

        playerEvents.Events.AllowSlide += AllowSliding;
    }
    public void Enter()
    {
        if (!playerSettings.allowCrouch) return;

        playerMovement.IsCrouching = true;

        playerSettings.events.OnCrouch.Invoke();
        playerEvents.Events.OnCrouchStart?.Invoke(); // Internal Event

        // Start sliding when conditions match.
        if (rb.linearVelocity.magnitude >= playerMovement.WalkSpeed && playerMovement.Grounded && playerSettings.allowSliding && !context.HasJumped)
        {
            Vector3 horizontalVel = new Vector3(rb.linearVelocity.x, 0, rb.linearVelocity.z);
            if (horizontalVel.magnitude > 0.1f)
                slideDirection = horizontalVel.normalized;
            else
                slideDirection = Vector3.ProjectOnPlane(orientation.Forward, Vector3.up).normalized;

            slideBoostRemaining = Mathf.Max(0.0001f, playerSettings.slideBoostDuration);
            isBoosting = true;

            // Begin local boost and timer.
            slideTimer = playerSettings.slideDuration;

            playerSettings.events.OnSlideStart.Invoke();
            playerEvents.Events.OnSlideStart?.Invoke();
        }
    }

    public void Tick() 
    {
        CapsuleCollider capsule = context.Capsule;
        if (!inputManager.Crouching)
        {
            playerMovement.IsCrouching = false;
            float targetH = initialHeight;
            float currentH = Mathf.MoveTowards(capsule.height, targetH, Time.deltaTime * playerSettings.crouchTransitionSpeed * 2.0f);
            capsule.height = currentH;
            
            float bottomOffset = initialCenter.y - initialHeight * 0.5f;
            capsule.center = new Vector3(initialCenter.x, bottomOffset + currentH * 0.5f, initialCenter.z);
            
            UpdateHeadPosition(capsule);
            return;
        }

        if (playerMovement.IsCrouching || isBoosting)
        {
            float targetH = playerSettings.crouchHeight;
            float currentH = Mathf.MoveTowards(capsule.height, targetH, Time.deltaTime * playerSettings.crouchTransitionSpeed * 1.5f * 2.0f);
            capsule.height = currentH;
            
            float bottomOffset = initialCenter.y - initialHeight * 0.5f;
            capsule.center = new Vector3(initialCenter.x, bottomOffset + currentH * 0.5f, initialCenter.z);
            
            UpdateHeadPosition(capsule);
        }
    }

    public void FixedTick()
    {
        if (!playerMovement.IsSliding) return;

        // Apply initial boost
        if (isBoosting && slideBoostRemaining > 0f)
        {
            float dt = Time.fixedDeltaTime;
            // Distribute the configured slideForce across the boost duration as acceleration
            float boostAmount = playerSettings.slideForce * dt / Mathf.Max(playerSettings.slideBoostDuration, dt);
            rb.AddForce(slideDirection * boostAmount, ForceMode.Acceleration);
            slideBoostRemaining -= dt;
            if (slideBoostRemaining <= 0f) isBoosting = false;
        }

        // Steering while sliding
        Vector3 inputDir = (orientation.Forward * inputManager.Y + orientation.Right * inputManager.X);
        inputDir.y = 0;
        if (inputDir.sqrMagnitude > 0.0001f)
        {
            Vector3 steer = inputDir.normalized * playerSettings.acceleration * playerSettings.slideSteerMultiplier * Time.fixedDeltaTime;
            rb.AddForce(steer, ForceMode.Acceleration);
        }

        // Decrease timer and check for stop condition
        slideTimer -= Time.fixedDeltaTime;
        Vector3 horizontalVel = new Vector3(rb.linearVelocity.x, 0, rb.linearVelocity.z);
        if (slideTimer <= 0 || horizontalVel.magnitude < playerSettings.slideStopSpeed || !playerMovement.Grounded)
        {
            EndSlide();
        }
    }

    public void Exit()
    {
        playerEvents.Events.OnCrouchStop?.Invoke();
        playerSettings.events.OnStopCrouch?.Invoke();
        EndSlide();
    }

    public bool CanExecute()
    {
        return inputManager.Crouching && !playerMovement.IsWallRunning && playerSettings.allowCrouch && (playerMovement.Grounded || !playerMovement.Grounded && playerSettings.allowCrouchWhileJumping);
    }


    public void HandleCrouch()
    {
        if (inputManager.Crouching)
        {
            CapsuleCollider capsule = context.Capsule;
            float targetH = playerSettings.crouchHeight;
            float currentH = Mathf.MoveTowards(capsule.height, targetH, Time.deltaTime * playerSettings.crouchTransitionSpeed * 1.5f * 2.0f);
            capsule.height = currentH;
            
            float bottomOffset = initialCenter.y - initialHeight * 0.5f;
            capsule.center = new Vector3(initialCenter.x, bottomOffset + currentH * 0.5f, initialCenter.z);
            
            UpdateHeadPosition(capsule);
        }

        playerMovement.IsCrouching = true;

        playerEvents.Events.OnCrouching?.Invoke();
    }

    public bool CheckUnCrouch()
    {
        if (!inputManager.Crouching) // Prevent from uncrouching when there’s a roof and we can get hit with it
        {
            RaycastHit hit;
            bool isObstacleAbove = Physics.Raycast(context.Transform.position, context.Transform.up, out hit, playerSettings.roofCheckDistance, context.WhatIsGround);

            canUnCrouch = !isObstacleAbove;

            if (canUnCrouch)
            {
                Tick();
                if (Mathf.Approximately(context.Capsule.height, initialHeight))
                {
                    playerMovement.IsCrouching = false;
                    return true;
                }
            }
        }

        return false;
    }

    private bool AllowSliding() => playerSettings.allowSliding;

    private void EndSlide()
    {
        if (!playerMovement.IsSliding) return;

        playerMovement.IsSliding = false;
        //playerEvents.Events.OnSlideEnd?.Invoke();
        playerSettings.events.OnStopCrouch?.Invoke();

        Vector3 vel = rb.linearVelocity;
        Vector3 horizontal = new Vector3(vel.x, 0, vel.z);

        rb.linearVelocity = new Vector3(horizontal.magnitude > 0 ? horizontal.normalized.x * Mathf.Max(0, horizontal.magnitude * 0.6f) : 0, vel.y, horizontal.magnitude > 0 ? horizontal.normalized.z * Mathf.Max(0, horizontal.magnitude * 0.6f) : 0);
    }

    private void UpdateHeadPosition(CapsuleCollider capsule)
    {
        if (cameraHead != null && initialHeight > 0 && capsule != null)
        {
            float targetHeadY = initialHeadLocalY * (capsule.height / initialHeight);
            cameraHead.localPosition = new Vector3(cameraHead.localPosition.x, targetHeadY, cameraHead.localPosition.z);
        }
    }
}

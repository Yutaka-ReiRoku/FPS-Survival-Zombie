namespace cowsins
{
    /// <summary>
    /// This script belongs to cowsins™ as a part of the cowsins´ FPS Engine. All rights reserved.
    /// </summary>
    using UnityEngine;

    /// <summary>
    /// Keep camera in place
    /// </summary>
    public class MoveCamera : MonoBehaviour
    {
        [Tooltip("Reference to our Camera Head Transform, that defines the placement of our Camera"), SerializeField] private Transform cameraHead;

        [Tooltip("Smoothing speed for vertical camera movement on stairs/rough terrain. Higher = more responsive, lower = smoother."), SerializeField, Min(0.1f)]
        private float verticalSmoothSpeed = 15f;

        private float smoothedY;
        private bool initialized;
        private bool wasSmoothingLastFrame;
        private PlayerMovement playerMovement;

        private void Awake()
        {
            if (cameraHead != null)
                playerMovement = cameraHead.GetComponentInParent<PlayerMovement>();
        }

        private void Update()
        {
            if (cameraHead == null) return;

            if (playerMovement == null)
                playerMovement = cameraHead.GetComponentInParent<PlayerMovement>();

            Vector3 targetPos = cameraHead.position;

            // Determine whether to smooth this frame.
            // Only smooth when grounded and not in special movement states.
            bool shouldSmooth = playerMovement != null && playerMovement.Grounded
                && !playerMovement.IsClimbing && !playerMovement.IsWallRunning;

            if (!initialized)
            {
                smoothedY = targetPos.y;
                initialized = true;
                wasSmoothingLastFrame = shouldSmooth;
            }
            else if (shouldSmooth != wasSmoothingLastFrame)
            {
                // State transition (grounded<->airborne): snap to current target
                // to avoid a visual jump from the smoothed position.
                smoothedY = targetPos.y;
                wasSmoothingLastFrame = shouldSmooth;
            }
            else if (shouldSmooth)
            {
                smoothedY = Mathf.Lerp(smoothedY, targetPos.y, verticalSmoothSpeed * Time.deltaTime);
            }
            else
            {
                smoothedY = targetPos.y;
            }

            transform.position = new Vector3(targetPos.x, smoothedY, targetPos.z);
        }
    }
}

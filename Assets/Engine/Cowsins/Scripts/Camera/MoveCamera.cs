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

        [Tooltip("Vertical velocity threshold above which smoothing is disabled (jumping/falling). Below this, the camera smooths Y for stairs."), SerializeField, Min(0.1f)]
        private float snapVelocityThreshold = 2f;

        private float smoothedY;
        private bool initialized;
        private PlayerMovement playerMovement;
        private Rigidbody playerRb;

        private void Awake()
        {
            if (cameraHead != null)
            {
                playerMovement = cameraHead.GetComponentInParent<PlayerMovement>();
                if (playerMovement != null)
                    playerRb = playerMovement.GetComponent<Rigidbody>();
            }
        }

        private void Update()
        {
            if (cameraHead == null) return;

            if (playerMovement == null)
            {
                playerMovement = cameraHead.GetComponentInParent<PlayerMovement>();
                if (playerMovement != null)
                    playerRb = playerMovement.GetComponent<Rigidbody>();
            }

            Vector3 targetPos = cameraHead.position;

            // Smooth Y only when the player is on the ground AND moving slowly vertically.
            // This covers walking up stairs/rough terrain (small Y changes per frame).
            // When jumping/falling (high vertical velocity), snap instantly for responsiveness.
            // Using velocity instead of Grounded avoids the 1-2 frame delay where Grounded
            // is still true but the player is already launching upward.
            bool shouldSmooth = playerMovement != null && playerMovement.Grounded
                && !playerMovement.IsClimbing && !playerMovement.IsWallRunning
                && playerRb != null && Mathf.Abs(playerRb.linearVelocity.y) < snapVelocityThreshold;

            if (!initialized)
            {
                smoothedY = targetPos.y;
                initialized = true;
            }
            else if (shouldSmooth)
            {
                smoothedY = Mathf.Lerp(smoothedY, targetPos.y, verticalSmoothSpeed * Time.deltaTime);
            }
            else
            {
                // Snap instantly — no smoothing when airborne or moving fast vertically
                smoothedY = targetPos.y;
            }

            transform.position = new Vector3(targetPos.x, smoothedY, targetPos.z);
        }
    }
}

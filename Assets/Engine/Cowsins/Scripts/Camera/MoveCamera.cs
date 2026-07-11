namespace cowsins
{
    using UnityEngine;

    /// <summary>
    /// Keep camera in place with premium AAA-grade step-climbing Y-offset compensation.
    /// Decouples visual camera transitions from sudden vertical physics teleports.
    /// </summary>
    public class MoveCamera : MonoBehaviour
    {
        [Tooltip("Reference to our Camera Head Transform, that defines the placement of our Camera"), SerializeField] private Transform cameraHead;

        [Tooltip("Smoothing speed for vertical camera movement on stairs/rough terrain. Higher = more responsive, lower = smoother."), SerializeField, Min(0.1f)]
        private float verticalSmoothSpeed = 15f;

        [Tooltip("Maximum Y-offset compensation value to prevent camera from clipping into player meshes or stairs."), SerializeField, Min(0.1f)]
        private float maxOffset = 0.4f;

        private float cameraYOffset = 0f;
        private PlayerMovement playerMovement;

        private void Awake()
        {
            if (cameraHead != null)
            {
                playerMovement = cameraHead.GetComponentInParent<PlayerMovement>();
            }
        }

        private void Start()
        {
            if (playerMovement == null && cameraHead != null)
            {
                playerMovement = cameraHead.GetComponentInParent<PlayerMovement>();
            }
            if (playerMovement != null)
            {
                // Subscribe to the step-climb event
                playerMovement.OnStepClimb.AddListener(OnStepClimb);
            }
        }

        private void OnDestroy()
        {
            if (playerMovement != null)
            {
                playerMovement.OnStepClimb.RemoveListener(OnStepClimb);
            }
        }

        private void OnStepClimb(float heightDelta)
        {
            // Subtract heightDelta to cancel out the physical step-up pop
            cameraYOffset -= heightDelta;
            
            // Clamp offset to prevent clipping into player body or step geometry
            cameraYOffset = Mathf.Clamp(cameraYOffset, -maxOffset, maxOffset);
        }

        private void Update()
        {
            if (cameraHead == null) return;

            if (playerMovement == null)
            {
                playerMovement = cameraHead.GetComponentInParent<PlayerMovement>();
                if (playerMovement != null)
                {
                    playerMovement.OnStepClimb.AddListener(OnStepClimb);
                }
            }

            Vector3 targetPos = cameraHead.position;

            // Instantly clear/snap the offset if the player is not grounded, climbing a ladder, or sliding
            if (playerMovement == null || !playerMovement.Grounded || playerMovement.IsClimbing || playerMovement.IsSliding)
            {
                cameraYOffset = 0f;
            }
            else
            {
                // Frame-rate independent exponential decay towards 0
                cameraYOffset = cameraYOffset * Mathf.Exp(-verticalSmoothSpeed * Time.deltaTime);
            }

            // Render camera at the target position + the smoothed offset
            transform.position = new Vector3(targetPos.x, targetPos.y + cameraYOffset, targetPos.z);
        }
    }
}

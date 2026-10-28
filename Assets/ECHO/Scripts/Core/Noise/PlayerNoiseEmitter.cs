using Echo.Shared;
using UnityEngine;

namespace Echo.Core
{
    /// <summary>
    /// Emits noise events at distance-based footstep intervals when the player moves.
    /// Priority order: Crouch (quietest), Run (loudest), Walk. Airborne and stationary = no noise.
    /// Attaches to the player GameObject alongside CharacterController.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public sealed class PlayerNoiseEmitter : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField]
        [Tooltip("Shared gameplay tuning asset for loudness and step spacing. If null, fallback values are used.")]
        private GameTuning gameTuning;

        [Header("Fallback Step Spacing (metres)")]
        [SerializeField] private float fallbackCrouchStepDistance = 1.2f;
        [SerializeField] private float fallbackWalkStepDistance = 1.8f;
        [SerializeField] private float fallbackRunStepDistance = 2.2f;

        [Header("Fallback Loudness (0..1 scale)")]
        [SerializeField] private float fallbackCrouchLoudness = 0.25f;
        [SerializeField] private float fallbackWalkLoudness = 0.5f;
        [SerializeField] private float fallbackRunLoudness = 1.0f;

        [Header("Thresholds")]
        [SerializeField]
        [Tooltip("CharacterController height below which player is considered crouching (normal height is 1.75m).")]
        private float crouchHeightThreshold = 1.4f;

        [SerializeField]
        [Tooltip("Horizontal speed in metres/second above which player is considered running (walk is ~4.25m/s, run is ~6.3m/s).")]
        private float runSpeedThreshold = 5.0f;

        private CharacterController controller;
        private Vector3 lastPosition;
        private float accumulatedDistance;

        // ── Unity Callbacks ────────────────────────────────────────────────────

        /// <summary>
        /// Awake: Cache CharacterController reference and record initial position.
        /// </summary>
        private void Awake()
        {
            controller = GetComponent<CharacterController>();
            lastPosition = transform.position;
        }

        /// <summary>
        /// OnEnable: Reset distance accumulator when activated.
        /// </summary>
        private void OnEnable()
        {
            lastPosition = transform.position;
            accumulatedDistance = 0f;
        }

        /// <summary>
        /// Update: Accumulate horizontal movement while grounded, and emit noise every N metres.
        /// </summary>
        private void Update()
        {
            if (controller == null)
                return;

            Vector3 currentPosition = transform.position;

            // Airborne = no footstep noise accumulated
            if (!controller.isGrounded)
            {
                lastPosition = currentPosition;
                return;
            }

            // Calculate horizontal ground displacement since last frame
            Vector3 horizontalDelta = new Vector3(currentPosition.x - lastPosition.x, 0f, currentPosition.z - lastPosition.z);
            float horizontalDistance = horizontalDelta.magnitude;
            lastPosition = currentPosition;

            // Stationary = no noise
            if (horizontalDistance < 0.0001f)
                return;

            accumulatedDistance += horizontalDistance;

            // Evaluate locomotion mode (Priority: Crouch -> Run -> Walk)
            float dt = Mathf.Max(Time.deltaTime, 0.0001f);
            float horizontalSpeed = horizontalDistance / dt;

            bool isCrouching = controller.height < crouchHeightThreshold;
            bool isRunning = !isCrouching && horizontalSpeed >= runSpeedThreshold;

            float stepDistance;
            float loudness;

            if (isCrouching)
            {
                stepDistance = gameTuning != null ? gameTuning.crouchStepDistance : fallbackCrouchStepDistance;
                loudness = gameTuning != null ? gameTuning.crouchLoudness : fallbackCrouchLoudness;
            }
            else if (isRunning)
            {
                stepDistance = gameTuning != null ? gameTuning.runStepDistance : fallbackRunStepDistance;
                loudness = gameTuning != null ? gameTuning.runLoudness : fallbackRunLoudness;
            }
            else
            {
                stepDistance = gameTuning != null ? gameTuning.walkStepDistance : fallbackWalkStepDistance;
                loudness = gameTuning != null ? gameTuning.walkLoudness : fallbackWalkLoudness;
            }

            // When accumulated distance exceeds step threshold, emit one noise event at player's feet
            if (accumulatedDistance >= stepDistance)
            {
                accumulatedDistance = 0f;
                Vector3 feetPosition = transform.position;
                EchoEvents.MakeNoise(feetPosition, loudness);
            }
        }
    }
}


using Echo.Shared;
using UnityEngine;

namespace Echo.Core
{
    /// <summary>
    /// Emits EchoEvents.Noise when the player moves on the ground.
    ///
    /// Locomotion tiers and their behaviour:
    ///   Crouch-walk  – one quiet noise every crouchStepDistance metres.
    ///                  Very low loudness; monster only hears it if very close.
    ///   Walk         – one medium noise every walkStepDistance metres.
    ///   Run          – one loud noise every runStepDistance metres AND one loud
    ///                  timed pulse every runContinuousInterval seconds while
    ///                  sprinting. The timed pulse keeps _lastHeardPosition fresh
    ///                  so the monster keeps chasing even on long straight runs.
    ///
    /// Attaches to the player GameObject alongside CharacterController.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public sealed class PlayerNoiseEmitter : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField]
        [Tooltip("Shared gameplay tuning asset. If null, fallback values below are used.")]
        private GameTuning gameTuning;

        [Header("Fallback Step Spacing (metres)")]
        [SerializeField] private float fallbackCrouchStepDistance = 1.5f;
        [SerializeField] private float fallbackWalkStepDistance   = 2.0f;
        [SerializeField] private float fallbackRunStepDistance    = 2.5f;

        [Header("Fallback Loudness (0..1 scale)")]
        [SerializeField] private float fallbackCrouchLoudness = 0.10f;
        [SerializeField] private float fallbackWalkLoudness   = 0.40f;
        [SerializeField] private float fallbackRunLoudness    = 1.00f;

        [Header("Fallback Continuous Run Noise")]
        [SerializeField]
        [Tooltip("While running, emit an additional noise every N seconds so the monster's " +
                 "target position stays fresh across long straight corridors.")]
        private float fallbackRunContinuousInterval = 0.35f;

        [Header("Detection Thresholds")]
        [SerializeField]
        [Tooltip("CharacterController height below which the player is considered crouching.")]
        private float crouchHeightThreshold = 1.4f;

        [SerializeField]
        [Tooltip("Horizontal speed (m/s) at or above which the player is running. " +
                 "Walk ≈ 4.25 m/s, Run ≈ 6.3 m/s.")]
        private float runSpeedThreshold = 5.0f;

        // ── Runtime ───────────────────────────────────────────────────────────
        private CharacterController _controller;
        private Vector3 _lastPosition;
        private float   _accumulatedDistance;
        private float   _continuousRunTimer;   // counts up; fires when >= interval

        // ── Unity lifecycle ───────────────────────────────────────────────────

        private void Awake()
        {
            _controller  = GetComponent<CharacterController>();
            _lastPosition = transform.position;
        }

        private void OnEnable()
        {
            _lastPosition        = transform.position;
            _accumulatedDistance = 0f;
            _continuousRunTimer  = 0f;
        }

        private void Update()
        {
            if (_controller == null) return;

            Vector3 current = transform.position;

            if (!_controller.isGrounded)
            {
                _lastPosition = current;
                _continuousRunTimer = 0f;
                return;
            }

            // Horizontal distance this frame
            var delta = new Vector3(current.x - _lastPosition.x, 0f, current.z - _lastPosition.z);
            float moved = delta.magnitude;
            _lastPosition = current;

            if (moved < 0.0001f)
            {
                _continuousRunTimer = 0f; // reset run timer when stationary
                return;
            }

            _accumulatedDistance += moved;

            // ── Locomotion mode ───────────────────────────────────────────────
            float dt    = Mathf.Max(Time.deltaTime, 0.0001f);
            float speed = moved / dt;

            bool isCrouching = _controller.height < crouchHeightThreshold;
            bool isRunning   = !isCrouching && speed >= runSpeedThreshold;

            float stepDistance;
            float loudness;
            float runInterval;

            if (isCrouching)
            {
                stepDistance = gameTuning != null ? gameTuning.crouchStepDistance : fallbackCrouchStepDistance;
                loudness     = gameTuning != null ? gameTuning.crouchLoudness     : fallbackCrouchLoudness;
                runInterval  = float.MaxValue; // no continuous noise while crouching
            }
            else if (isRunning)
            {
                stepDistance = gameTuning != null ? gameTuning.runStepDistance          : fallbackRunStepDistance;
                loudness     = gameTuning != null ? gameTuning.runLoudness              : fallbackRunLoudness;
                runInterval  = gameTuning != null ? gameTuning.runContinuousInterval    : fallbackRunContinuousInterval;
            }
            else
            {
                stepDistance = gameTuning != null ? gameTuning.walkStepDistance : fallbackWalkStepDistance;
                loudness     = gameTuning != null ? gameTuning.walkLoudness     : fallbackWalkLoudness;
                runInterval  = float.MaxValue; // no continuous noise while walking
            }

            // ── Distance-based footstep pulse ─────────────────────────────────
            if (_accumulatedDistance >= stepDistance)
            {
                _accumulatedDistance = 0f;
                EchoEvents.MakeNoise(transform.position, loudness);
            }

            // ── Time-based continuous run pulse ───────────────────────────────
            // Fires BETWEEN distance pulses so the monster always has a fresh
            // position even on long straight runs without a step crossing.
            if (isRunning)
            {
                _continuousRunTimer += Time.deltaTime;
                if (_continuousRunTimer >= runInterval)
                {
                    _continuousRunTimer = 0f;
                    EchoEvents.MakeNoise(transform.position, loudness);
                }
            }
            else
            {
                _continuousRunTimer = 0f;
            }
        }
    }
}


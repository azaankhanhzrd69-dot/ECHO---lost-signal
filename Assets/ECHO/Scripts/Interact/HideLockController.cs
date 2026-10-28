using Echo.Shared;
using UnityEngine;

namespace Echo.Interact
{
    /// <summary>
    /// Manages the player's input lock during the hide-minigame sequence.
    ///
    /// Flow:
    ///   1. EchoEvents.HideStarted  → lock all input (motor + camera + interactor).
    ///   2. EchoEvents.HideFinished → if survived, begin monitoring monster distance.
    ///   3. Once monster is beyond safeRadius → unlock, fire SafeToLeave, InteractPromptUI
    ///      shows the "safe" message for 3 s.
    ///
    /// Attach to a persistent GameObject in the scene (e.g. the player or a GameManager).
    /// Requires: EchoEvents.InputLocked (in Shared) and Progress.MonsterPosition (updated by Core).
    /// </summary>
    public sealed class HideLockController : MonoBehaviour
    {
        // ── Inspector ──────────────────────────────────────────────────────────
        [SerializeField]
        [Tooltip("Distance (metres) the monster must be from the player before input unlocks. " +
                 "TODO: move to GameTuning when a field is added there.")]
        private float safeRadius = 8f;

        // ── Public event ───────────────────────────────────────────────────────
        /// <summary>
        /// Raised once when the monster moves far enough away after a hide.
        /// InteractPromptUI subscribes to this to show the "safe" message.
        /// </summary>
        public static event System.Action SafeToLeave;

        // ── State ──────────────────────────────────────────────────────────────
        private bool waitingForSafe;    // true after a successful hide, before safe distance reached
        private Transform playerTransform;

        // ── Unity callbacks ────────────────────────────────────────────────────

        /// <summary>Awake: cache player transform.</summary>
        private void Awake()
        {
            playerTransform = transform;
        }

        /// <summary>OnEnable: subscribe to hide events.</summary>
        private void OnEnable()
        {
            EchoEvents.HideStarted  += OnHideStarted;
            EchoEvents.HideFinished += OnHideFinished;
        }

        /// <summary>OnDisable: always unsubscribe to prevent leaked delegates.</summary>
        private void OnDisable()
        {
            EchoEvents.HideStarted  -= OnHideStarted;
            EchoEvents.HideFinished -= OnHideFinished;

            // If disabled mid-hide, do not leave the game permanently locked.
            if (EchoEvents.InputLocked)
                EchoEvents.UnlockInput();

            waitingForSafe = false;
        }

        /// <summary>
        /// Update: poll monster distance while waiting for safe clearance.
        /// Only active between HideFinished(true) and the monster walking away.
        /// </summary>
        private void Update()
        {
            if (!waitingForSafe) return;

            float dist = Vector3.Distance(playerTransform.position, Progress.MonsterPosition);
            if (dist >= safeRadius)
            {
                waitingForSafe = false;
                EchoEvents.UnlockInput();
                SafeToLeave?.Invoke();
            }
        }

        // ── Handlers ───────────────────────────────────────────────────────────

        /// <summary>HideStarted: immediately lock input so the player cannot move or interact.</summary>
        private void OnHideStarted(Vector3 _)
        {
            waitingForSafe = false;
            EchoEvents.LockInput();
        }

        /// <summary>
        /// HideFinished: if the player survived, begin watching monster distance and keep input locked.
        /// If caught, leave input locked (game-over sequence owns control).
        /// </summary>
        private void OnHideFinished(bool survived)
        {
            if (survived)
            {
                // Keep lock on; Update will lift it once the monster is far enough.
                waitingForSafe = true;
                EchoEvents.LockInput();
            }
            else
            {
                // survived == false: caught by monster — ensure lock stays on for death sequence.
                waitingForSafe = false;
                EchoEvents.LockInput();
            }
        }

        // ── Static reset ───────────────────────────────────────────────────────
        /// <summary>
        /// Clears the static event on Play-mode start so stale delegates from a
        /// previous session do not fire when Domain Reload is disabled.
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            SafeToLeave = null;
        }
    }
}


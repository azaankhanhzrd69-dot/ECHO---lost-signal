using Echo.Shared;
using TMPro;
using UnityEngine;

namespace Echo.Interact
{
    /// <summary>
    /// Shows "[E] {Prompt}" when the player is looking at an IInteractable.
    /// Also shows the "safe to leave" message after a hide sequence ends.
    /// Attach to a Canvas/UI object with a TextMeshProUGUI child.
    /// </summary>
    public sealed class InteractPromptUI : MonoBehaviour
    {
        // ── Inspector ──────────────────────────────────────────────────────────
        [SerializeField]
        [Tooltip("The Interactor on the player that drives what is targeted.")]
        private Interactor interactor;

        [SerializeField]
        [Tooltip("TMP label for the interaction prompt (e.g. '[E] Open door').")]
        private TextMeshProUGUI promptLabel;

        [SerializeField]
        [Tooltip("TMP label for the 'Safe to leave' message (separate object so it can be styled differently).")]
        private TextMeshProUGUI safeLabel;

        // ── State ──────────────────────────────────────────────────────────────
        private float safeLabelTimer;       // counts down while safeLabel is showing
        private const float SafeLabelDuration = 3f;

        // ── Unity callbacks ────────────────────────────────────────────────────

        /// <summary>OnEnable: subscribe to interactor target changes.</summary>
        private void OnEnable()
        {
            if (interactor != null)
                interactor.TargetChanged += OnTargetChanged;

            // Listen for "safe" unlock so we can flash the message.
            HideLockController.SafeToLeave += OnSafeToLeave;
        }

        /// <summary>OnDisable: always unsubscribe to prevent leaked delegates.</summary>
        private void OnDisable()
        {
            if (interactor != null)
                interactor.TargetChanged -= OnTargetChanged;

            HideLockController.SafeToLeave -= OnSafeToLeave;
        }

        /// <summary>Awake: hide labels at startup.</summary>
        private void Awake()
        {
            SetPromptVisible(false);
            SetSafeVisible(false);
        }

        /// <summary>Update: count down safe-label timer.</summary>
        private void Update()
        {
            if (safeLabelTimer > 0f)
            {
                safeLabelTimer -= Time.deltaTime;
                if (safeLabelTimer <= 0f)
                    SetSafeVisible(false);
            }
        }

        // ── Handlers ───────────────────────────────────────────────────────────

        private void OnTargetChanged(IInteractable target)
        {
            if (target != null && !string.IsNullOrEmpty(target.Prompt) && !EchoEvents.InputLocked)
            {
                if (promptLabel != null)
                    promptLabel.text = "[E]  " + target.Prompt;
                SetPromptVisible(true);
            }
            else
            {
                SetPromptVisible(false);
            }
        }

        private void OnSafeToLeave()
        {
            SetSafeVisible(true);
            safeLabelTimer = SafeLabelDuration;
        }

        // ── Helpers ────────────────────────────────────────────────────────────

        private void SetPromptVisible(bool visible)
        {
            if (promptLabel != null)
                promptLabel.gameObject.SetActive(visible);
        }

        private void SetSafeVisible(bool visible)
        {
            if (safeLabel != null)
                safeLabel.gameObject.SetActive(visible);
        }
    }
}


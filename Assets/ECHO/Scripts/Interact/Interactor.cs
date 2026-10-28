using Echo.Shared;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Echo.Interact
{
    /// <summary>
    /// First-person interaction raycaster.
    /// Attach to the player root (or any object that holds the camera as a child).
    /// Runs a raycast every Update to find the nearest IInteractable within reach,
    /// then fires Interact() on the key-down frame.
    /// </summary>
    public sealed class Interactor : MonoBehaviour
    {
        // ── Inspector ──────────────────────────────────────────────────────────
        [SerializeField]
        [Tooltip("Camera used to aim the ray. Leave null to auto-find in Awake (warns if missing).")]
        private Camera interactCamera;

        [SerializeField]
        [Tooltip("Maximum distance (metres) at which the player can interact. " +
                 "TODO: move to GameTuning when a field is added there.")]
        private float reach = 3f;

        [SerializeField]
        [Tooltip("Layer mask for objects with an IInteractable component. " +
                 "Create an 'Interactable' layer and assign it here.")]
        private LayerMask interactableMask = ~0; // default: all layers

        // ── State ──────────────────────────────────────────────────────────────
        private IInteractable currentTarget;

        // ── Events ─────────────────────────────────────────────────────────────
        /// <summary>Raised whenever the targeted IInteractable changes (including to null).</summary>
        public event System.Action<IInteractable> TargetChanged;

        // ── Unity callbacks ────────────────────────────────────────────────────

        /// <summary>Awake: cache camera, warn if absent.</summary>
        private void Awake()
        {
            if (interactCamera == null)
            {
                interactCamera = FindAnyObjectByType<Camera>();
                if (interactCamera == null)
                    Debug.LogWarning("[Interactor] No camera found. Assign 'Interact Camera' in the Inspector.", this);
                else
                    Debug.LogWarning("[Interactor] No camera assigned; found one automatically. " +
                                     "Assign 'Interact Camera' in the Inspector for reliability.", this);
            }
        }

        /// <summary>Update: raycast and handle interaction input.</summary>
        private void Update()
        {
            // Motor and interactor both respect the global input lock.
            if (EchoEvents.InputLocked || interactCamera == null)
            {
                SetTarget(null);
                return;
            }

            // Raycast from the camera's forward direction.
            Ray ray = new Ray(interactCamera.transform.position, interactCamera.transform.forward);
            IInteractable found = null;

            if (Physics.Raycast(ray, out RaycastHit hit, reach, interactableMask))
                found = hit.collider.GetComponentInParent<IInteractable>();

            SetTarget(found);

            // One press = one action; ignore held key.
            if (currentTarget != null && Keyboard.current != null &&
                Keyboard.current.eKey.wasPressedThisFrame)
            {
                currentTarget.Interact();
            }
        }

        // ── Helpers ────────────────────────────────────────────────────────────

        /// <summary>Updates cached target and fires TargetChanged only when it actually changes.</summary>
        private void SetTarget(IInteractable next)
        {
            if (ReferenceEquals(currentTarget, next)) return;
            currentTarget = next;
            TargetChanged?.Invoke(currentTarget);
        }
    }
}


using UnityEngine;
using UnityEngine.Events;
using Echo.Shared;

namespace Echo.Core
{
    /// <summary>
    /// Placed on each hiding spot prefab (Locker, Table, Chair) in any scene.
    /// Implements IInteractable so Interact's Interactor can call Interact() on it.
    ///
    /// Registers itself with HideController.Instance in Awake and deregisters in OnDestroy.
    /// The human must assign kind, monsterPoint, and wire OnEnter/OnExit in the Inspector.
    /// </summary>
    public class HideSpot : MonoBehaviour, IInteractable
    {
        // ── Inspector fields ──────────────────────────────────────────────────

        [Tooltip("Locker = safe (up to limit). Table/Chair = instant kill.")]
        [SerializeField] public HideKind kind = HideKind.Locker;

        [Tooltip("The monster is moved to this Transform while the hide plays. " +
                 "Place it just outside or at the spot (e.g. in front of the locker door).")]
        [SerializeField] public Transform monsterPoint;

        [Header("UnityEvents — wire World's door/camera/player pose here")]
        [Tooltip("Fired when the player enters this spot. Wire to locker door close animation, " +
                 "camera pose, player movement disable (World).")]
        public UnityEvent OnEnter = new UnityEvent();

        [Tooltip("Fired when the player exits this spot after surviving. " +
                 "Wire to locker door open animation, camera restore (World).")]
        public UnityEvent OnExit = new UnityEvent();

        // ── IInteractable ─────────────────────────────────────────────────────

        /// <summary>Prompt shown by Interact's InteractPromptUI above this spot.</summary>
        public string Prompt => kind == HideKind.Locker ? "Hide" : "Hide (???)";

        /// <summary>
        /// Called by Interact's Interactor when the player presses the interact key.
        /// Delegates to HideController.Instance so Core owns the hide logic.
        /// </summary>
        public void Interact(GameObject interactor)
        {
            HideController ctrl = HideController.Instance;
            if (ctrl == null)
            {
                Debug.LogWarning("[HideSpot] No HideController instance in scene.", this);
                return;
            }
            ctrl.BeginHide(this);
        }

        // ── Unity callbacks ───────────────────────────────────────────────────

        /// <summary>Awake: register with the singleton so HideController can reference spots.</summary>
        private void Awake()
        {
            // Registration is one-way: HideSpot tells HideController it exists.
            // HideController.BeginHide receives the spot directly from Interact().
        }

        private void OnDestroy()
        {
            // If this spot is destroyed mid-hide (scene unload), abort via the controller.
            HideController ctrl = HideController.Instance;
            if (ctrl != null && ctrl.CurrentSpot == this)
                ctrl.AbortHide();
        }
    }
}


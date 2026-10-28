#if UNITY_EDITOR || DEBUG
using UnityEngine;
using UnityEngine.InputSystem;
using Echo.Shared;

namespace Echo.Core
{
    /// <summary>
    /// Editor / dev-build helper for testing the hide system before I-04 is merged.
    /// Press successKey to simulate a survived hide (EchoEvents.EndHide(true)).
    /// Press failKey to simulate a failed hide (EchoEvents.EndHide(false)).
    /// Only fires while a hide is in progress (HideController.Instance.IsHiding).
    /// </summary>
    public class DebugHideResult : MonoBehaviour
    {
        [SerializeField] private Key successKey = Key.F5;
        [SerializeField] private Key failKey    = Key.F6;

        // Update runs every frame only in Editor / dev builds (guarded by #if above).
        private void Update()
        {
            if (Keyboard.current == null) return;

            HideController ctrl = HideController.Instance;
            if (ctrl == null || !ctrl.IsHiding) return;

            if (Keyboard.current[successKey].wasPressedThisFrame)
            {
                Debug.Log("[DebugHideResult] Simulating hide SUCCESS.");
                EchoEvents.EndHide(true);
            }
            else if (Keyboard.current[failKey].wasPressedThisFrame)
            {
                Debug.Log("[DebugHideResult] Simulating hide FAILURE.");
                EchoEvents.EndHide(false);
            }
        }
    }
}
#endif


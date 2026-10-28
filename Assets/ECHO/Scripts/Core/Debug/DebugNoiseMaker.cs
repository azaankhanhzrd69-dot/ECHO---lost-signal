#if UNITY_EDITOR || DEBUG
using UnityEngine;
using Echo.Shared;

namespace Echo.Core
{
    /// <summary>
    /// Editor / dev-build helper: left-click on the floor to emit a noise at that point.
    /// Useful for testing MonsterAI without the player controller making noise.
    ///
    /// Add this to any scene GameObject. Assign the camera in the Inspector.
    /// Click = a configurable loudness noise at the ray hit point.
    /// </summary>
    public class DebugNoiseMaker : MonoBehaviour
    {
        [SerializeField] private Camera sourceCamera;
        [SerializeField] [Range(0f, 1f)] private float debugLoudness = 0.8f;
        [SerializeField] private LayerMask floorMask = ~0;

        // Update: runs every frame only in Editor / dev builds (guarded by #if above).
        private void Update()
        {
            if (!Input.GetMouseButtonDown(0)) return;

            Camera cam = sourceCamera != null ? sourceCamera : Camera.main;
            if (cam == null) return;

            Ray ray = cam.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit, 200f, floorMask))
            {
                EchoEvents.MakeNoise(hit.point, debugLoudness);
                Debug.Log($"[DebugNoiseMaker] Noise at {hit.point}, loudness {debugLoudness}");
            }
        }
    }
}
#endif


using System.Collections.Generic;
using Echo.Shared;
using UnityEngine;

namespace Echo.Core
{
    /// <summary>
    /// Debug visualizer for EchoEvents.Noise events.
    /// Displays an OnGUI list of the last 5 noises and draws Scene Gizmo spheres.
    /// </summary>
    public sealed class NoiseDebugOverlay : MonoBehaviour
    {
        private struct NoiseRecord
        {
            public float time;
            public Vector3 position;
            public float loudness;
        }

        [Header("Visualization Settings")]
        [SerializeField]
        [Tooltip("Toggle the OnGUI list and Scene gizmos.")]
        private bool showOverlay = true;

        [SerializeField]
        [Tooltip("Player transform used to calculate distance to noise events. Auto-found if null.")]
        private Transform playerTransform;

        [SerializeField]
        [Tooltip("Max radius in metres represented by loudness 1.0 in Scene Gizmos.")]
        private float gizmoMaxRadius = 8f;

        private static readonly List<NoiseRecord> recentNoises = new List<NoiseRecord>(5);
        private const int MaxRecords = 5;

        // ── Unity Callbacks ────────────────────────────────────────────────────

        /// <summary>
        /// Awake: Cache player transform reference if not assigned.
        /// </summary>
        private void Awake()
        {
            if (playerTransform == null)
            {
                CharacterController cc = FindAnyObjectByType<CharacterController>();
                if (cc != null)
                    playerTransform = cc.transform;
            }
        }

        /// <summary>
        /// OnEnable: Subscribe to global noise event.
        /// </summary>
        private void OnEnable()
        {
            EchoEvents.Noise += OnNoiseReceived;
        }

        /// <summary>
        /// OnDisable: Unsubscribe from global noise event to prevent memory leaks.
        /// </summary>
        private void OnDisable()
        {
            EchoEvents.Noise -= OnNoiseReceived;
        }

        // ── Event Handler ──────────────────────────────────────────────────────

        private void OnNoiseReceived(Vector3 pos, float loudness)
        {
            if (recentNoises.Count >= MaxRecords)
            {
                recentNoises.RemoveAt(0);
            }

            recentNoises.Add(new NoiseRecord
            {
                time = Time.time,
                position = pos,
                loudness = loudness
            });
        }

        // ── OnGUI Display ──────────────────────────────────────────────────────

        private void OnGUI()
        {
            if (!showOverlay)
                return;

            Vector3 refPos = playerTransform != null ? playerTransform.position : Vector3.zero;

            GUILayout.BeginArea(new Rect(15, 15, 340, 180), GUI.skin.box);
            GUILayout.Label("<b>NOISE DEBUG OVERLAY (EchoEvents.Noise)</b>");

            if (recentNoises.Count == 0)
            {
                GUILayout.Label("No noise events recorded yet.\nMove to generate footstep noise.");
            }
            else
            {
                for (int i = recentNoises.Count - 1; i >= 0; i--)
                {
                    NoiseRecord r = recentNoises[i];
                    float age = Time.time - r.time;
                    float dist = Vector3.Distance(refPos, r.position);

                    string modeHint = r.loudness <= 0.3f ? "Crouch" : r.loudness >= 0.8f ? "Run" : "Walk";
                    GUILayout.Label($"[{age:F1}s ago] {modeHint} | Loudness: {r.loudness:F2} | Dist: {dist:F1}m");
                }
            }

            GUILayout.EndArea();
        }

        // ── Scene Gizmos ───────────────────────────────────────────────────────

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private void OnDrawGizmos()
        {
            if (!showOverlay)
                return;

            for (int i = 0; i < recentNoises.Count; i++)
            {
                NoiseRecord r = recentNoises[i];
                float age = Application.isPlaying ? (Time.time - r.time) : 0f;
                float alpha = Mathf.Clamp01(1f - (age / 3.0f));

                if (alpha <= 0.05f)
                    continue;

                // Color by loudness: Green = quiet/crouch, Yellow = walk, Red = run/loud
                Color c = r.loudness <= 0.3f ? Color.green : r.loudness >= 0.8f ? Color.red : Color.yellow;
                c.a = alpha * 0.4f;
                Gizmos.color = c;

                float radius = Mathf.Max(0.5f, r.loudness * gizmoMaxRadius);
                Gizmos.DrawSphere(r.position, radius);

                c.a = alpha;
                Gizmos.color = c;
                Gizmos.DrawWireSphere(r.position, radius);
            }
        }
#endif

        // ── Static Reset ───────────────────────────────────────────────────────

        /// <summary>
        /// Clears static records when entering Play Mode if Domain Reload is disabled.
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            recentNoises.Clear();
        }
    }
}


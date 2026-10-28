using System.Collections;
using Echo.Shared;
using UnityEngine;
using UnityEngine.AI;

namespace Echo.Core
{
    /// <summary>
    /// Coordinates the climactic Chase beat:
    /// - Warps the monster to chaseStartPoint and boosts its speed and hearing radius.
    /// - Continuously emits synthetic noise beacons at the player's position so the monster hunts relentlessly.
    /// - Refuses hiding spots and ensures the monster maintains chase pressure to the escape pod.
    /// - Includes an Editor ContextMenu balance reporter to verify pathing margins.
    /// </summary>
    public sealed class ChaseDirector : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private MonsterAI monster;
        [SerializeField] private Transform playerTransform;
        [SerializeField] private GameTuning tuning;

        [Header("Positions")]
        [Tooltip("Where the monster spawns when the Chase beat begins.")]
        [SerializeField] private Transform chaseStartPoint;

        [Tooltip("Escape pod destination (used for balance check).")]
        [SerializeField] private Transform podPoint;

        [Tooltip("Player starting point in reactor (used for balance check).")]
        [SerializeField] private Transform playerStart;

        // ── State ─────────────────────────────────────────────────────────────
        private Coroutine _beaconRoutine;
        private Coroutine _beginChaseRoutine;

        // ── Unity Callbacks ───────────────────────────────────────────────────

        private void OnEnable()
        {
            EchoEvents.BeatChanged += OnBeatChanged;
            if (Progress.Current == Beat.Chase)
            {
                StartChase();
            }
        }

        private void OnDisable()
        {
            EchoEvents.BeatChanged -= OnBeatChanged;
            StopChase();
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Initiates the Chase countdown sequence.
        /// Wire to reactor power restore or core insertion interaction.
        /// </summary>
        public void BeginChase()
        {
            if (_beginChaseRoutine != null)
                StopCoroutine(_beginChaseRoutine);

            _beginChaseRoutine = StartCoroutine(BeginChaseRoutine());
        }

        private IEnumerator BeginChaseRoutine()
        {
            float delay = tuning != null ? tuning.powerOnToChaseDelay : 2.0f;
            if (delay > 0f)
                yield return new WaitForSeconds(delay);

            _beginChaseRoutine = null;
            EchoEvents.SetBeat(Beat.Chase);
        }

        // ── Beat Routing ──────────────────────────────────────────────────────

        private void OnBeatChanged(Beat newBeat)
        {
            if (newBeat == Beat.Chase)
            {
                StartChase();
            }
            else
            {
                StopChase();
            }
        }

        private void StartChase()
        {
            StopChase();

            if (monster != null)
            {
                if (monster.State == MonsterState.Scripted)
                {
                    monster.ExitScripted();
                }

                monster.ResetToDormant();

                if (chaseStartPoint != null && monster.Agent != null && monster.Agent.isOnNavMesh)
                {
                    monster.Agent.Warp(chaseStartPoint.position);
                }

                // Compute speed multiplier so monster speed equals (playerSprintSpeed * chaseSpeedRatio)
                float targetSpeed = (tuning != null ? tuning.playerSprintSpeed : 6.3f) * (tuning != null ? tuning.chaseSpeedRatio : 0.90f);
                float baseSpeed = (tuning != null && tuning.speedChase > 0f) ? tuning.speedChase : 5.0f;
                float speedMultiplier = targetSpeed / baseSpeed;
                float hearingMultiplier = tuning != null ? tuning.chaseHearingMultiplier : 2.5f;

                monster.SetChaseBoost(speedMultiplier, hearingMultiplier);
            }

            _beaconRoutine = StartCoroutine(BeaconRoutine());
        }

        private void StopChase()
        {
            if (_beaconRoutine != null)
            {
                StopCoroutine(_beaconRoutine);
                _beaconRoutine = null;
            }

            if (_beginChaseRoutine != null)
            {
                StopCoroutine(_beginChaseRoutine);
                _beginChaseRoutine = null;
            }

            if (monster != null)
            {
                monster.ClearChaseBoost();
            }
        }

        private IEnumerator BeaconRoutine()
        {
            while (true)
            {
                if (playerTransform != null)
                {
                    float loud = tuning != null ? tuning.beaconLoudness : 1.0f;
                    EchoEvents.MakeNoise(playerTransform.position, loud);
                }

                float interval = tuning != null ? tuning.beaconInterval : 0.8f;
                yield return new WaitForSeconds(interval);
            }
        }

        // ── Editor Balance Reporter ───────────────────────────────────────────

        [ContextMenu("Report chase balance")]
        public void ReportChaseBalance()
        {
            if (podPoint == null || playerStart == null || chaseStartPoint == null)
            {
                Debug.LogWarning("[ChaseDirector] Cannot report balance: podPoint, playerStart, and chaseStartPoint must be assigned.");
                return;
            }

            float pSpeed = tuning != null ? tuning.playerSprintSpeed : 6.3f;
            float mSpeed = pSpeed * (tuning != null ? tuning.chaseSpeedRatio : 0.90f);

            var pPath = new NavMeshPath();
            var mPath = new NavMeshPath();

            if (!NavMesh.CalculatePath(playerStart.position, podPoint.position, NavMesh.AllAreas, pPath))
            {
                Debug.LogError("[ChaseDirector] Failed to calculate NavMesh path for player.");
                return;
            }

            if (!NavMesh.CalculatePath(chaseStartPoint.position, podPoint.position, NavMesh.AllAreas, mPath))
            {
                Debug.LogError("[ChaseDirector] Failed to calculate NavMesh path for monster.");
                return;
            }

            float pLength = CalculatePathLength(pPath);
            float mLength = CalculatePathLength(mPath);

            float pTime = pLength / Mathf.Max(pSpeed, 0.1f);
            float mTime = mLength / Mathf.Max(mSpeed, 0.1f);
            float margin = mTime - pTime;

            float minMargin = tuning != null ? tuning.minMarginSeconds : 2.0f;

            Debug.Log($"=== CHASE BALANCE REPORT ===\n" +
                      $"Player Sprint Speed: {pSpeed:F2} m/s | Path Length: {pLength:F1} m | Time: {pTime:F2} s\n" +
                      $"Monster Chase Speed: {mSpeed:F2} m/s | Path Length: {mLength:F1} m | Time: {mTime:F2} s\n" +
                      $"Escape Margin: {margin:F2} seconds (Min Required: {minMargin:F2} s)");

            if (margin < minMargin)
            {
                Debug.LogWarning($"[ChaseDirector] WARNING: Escape margin ({margin:F2}s) is BELOW the safety threshold ({minMargin:F2}s)! Player will likely be caught.");
            }
            else
            {
                Debug.Log($"[ChaseDirector] SUCCESS: Chase path has an acceptable margin ({margin:F2}s).");
            }
        }

        private static float CalculatePathLength(NavMeshPath path)
        {
            if (path.corners == null || path.corners.Length < 2) return 0f;
            float length = 0f;
            for (int i = 0; i < path.corners.Length - 1; i++)
            {
                length += Vector3.Distance(path.corners[i], path.corners[i + 1]);
            }
            return length;
        }
    }
}


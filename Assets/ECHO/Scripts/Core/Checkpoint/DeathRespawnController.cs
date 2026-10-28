using System.Collections.Generic;
using Echo.Shared;
using UnityEngine;
using UnityEngine.Events;

namespace Echo.Core
{
    /// <summary>
    /// Master death and checkpoint respawn controller:
    /// - Captures Progress snapshots on beat changes and mid-beat checkpoints (pickups, hides).
    /// - Guarantees single-death execution (IsDead flag) and handles caught overlays.
    /// - Enforces safe player teleportation with CharacterController and monster state resets.
    /// - Re-raises the story beat upon respawn to cleanly restart systems.
    /// </summary>
    public sealed class DeathRespawnController : MonoBehaviour
    {
        public static DeathRespawnController Instance { get; private set; }

        [Header("Configuration")]
        [SerializeField] private GameTuning tuning;

        [Header("References")]
        [SerializeField] private Transform playerTransform;
        [SerializeField] private MonsterAI monster;

        [Header("Checkpoints")]
        [Tooltip("Inspector list of checkpoint points. Points also auto-register in Awake.")]
        [SerializeField] private List<CheckpointPoint> checkpointPoints = new List<CheckpointPoint>();

        [Header("Events")]
        [Tooltip("Invoked when a death occurs. Wire to I-09 ScreenManager.ShowCaught.")]
        public UnityEvent OnShowCaught = new UnityEvent();

        [Tooltip("Invoked after the player has respawned and the beat has re-started.")]
        public UnityEvent OnRespawned = new UnityEvent();

        // ── Static Registry ──────────────────────────────────────────────────
        private static readonly Dictionary<Beat, CheckpointPoint> PointsByBeat = new Dictionary<Beat, CheckpointPoint>();

        // ── State ─────────────────────────────────────────────────────────────
        private ProgressSnapshot _snapshot;
        private Vector3 _lastSavedPlayerPosition;
        private Quaternion _lastSavedPlayerRotation;
        private float _deathTime = -999f;
        private bool _isDead;

        public bool IsDead => _isDead;

        // ── Registration ──────────────────────────────────────────────────────

        public static void RegisterPoint(CheckpointPoint point)
        {
            if (point == null) return;
            PointsByBeat[point.beat] = point;
        }

        public static void UnregisterPoint(CheckpointPoint point)
        {
            if (point == null) return;
            if (PointsByBeat.TryGetValue(point.beat, out CheckpointPoint existing) && existing == point)
            {
                PointsByBeat.Remove(point.beat);
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            Instance = null;
            PointsByBeat.Clear();
        }

        // ── Unity Callbacks ───────────────────────────────────────────────────

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            for (int i = 0; i < checkpointPoints.Count; i++)
            {
                RegisterPoint(checkpointPoints[i]);
            }

            if (playerTransform != null)
            {
                _lastSavedPlayerPosition = playerTransform.position;
                _lastSavedPlayerRotation = playerTransform.rotation;
            }

            // Capture initial snapshot
            _snapshot = ProgressSnapshot.Capture();
        }

        private void OnEnable()
        {
            EchoEvents.BeatChanged += OnBeatChanged;
        }

        private void OnDisable()
        {
            EchoEvents.BeatChanged -= OnBeatChanged;
            if (Instance == this) Instance = null;
        }

        // ── Public Checkpoint & Death API ─────────────────────────────────────

        /// <summary>
        /// Saves current Progress fields and player position into the active snapshot.
        /// Call whenever progress is made inside a beat (core pickups, surviving a hide).
        /// </summary>
        public void SaveCheckpoint()
        {
            _snapshot = ProgressSnapshot.Capture();
            if (playerTransform != null)
            {
                _lastSavedPlayerPosition = playerTransform.position;
                _lastSavedPlayerRotation = playerTransform.rotation;
            }
        }

        /// <summary>
        /// Handles player death from any source (monster attack, fatal hide spot).
        /// Wire to MonsterAI.OnPlayerCaught and HideController.OnHideDeath.
        /// </summary>
        public void HandleCaught()
        {
            // Ignore if already dead or in non-lethal final beats
            if (_isDead || Progress.Current == Beat.Airlock || Progress.Current == Beat.Ending)
                return;

            _isDead = true;
            _deathTime = Time.time;

            // Lock player input
            EchoEvents.LockInput();

            // Notify UI overlay (I-09 ScreenManager)
            OnShowCaught?.Invoke();
        }

        /// <summary>
        /// Executes player respawn back to the last valid checkpoint point or position.
        /// Wire to I-09 ScreenManager.OnRespawnRequested.
        /// </summary>
        public void RequestRespawn()
        {
            if (!_isDead) return;

            float minWait = tuning != null ? tuning.minDeathScreenSeconds : 1.0f;
            if (Time.time - _deathTime < minWait)
                return;

            // 1. Restore Progress state
            if (_snapshot != null)
            {
                _snapshot.Restore();
            }

            // 2. Teleport player safely
            TeleportPlayer();

            // 3. Reset monster
            ResetMonster();

            // 4. Release locks and death state
            _isDead = false;
            EchoEvents.UnlockInput();

            // 5. Re-raise beat so systems cleanly re-initialize
            EchoEvents.SetBeat(Progress.Current);

            // 6. Notify listeners
            OnRespawned?.Invoke();
        }

        // ── Internal Helpers ──────────────────────────────────────────────────

        private void TeleportPlayer()
        {
            if (playerTransform == null) return;

            Vector3 targetPos = _lastSavedPlayerPosition;
            Quaternion targetRot = _lastSavedPlayerRotation;

            if (PointsByBeat.TryGetValue(Progress.Current, out CheckpointPoint point) && point.playerPoint != null)
            {
                targetPos = point.playerPoint.position;
                targetRot = point.playerPoint.rotation;
            }
            else
            {
                Debug.LogWarning($"[DeathRespawnController] No CheckpointPoint found for beat {Progress.Current}. Respawing at last saved position.");
            }

            // Safe CharacterController teleport
            var cc = playerTransform.GetComponent<CharacterController>();
            if (cc != null) cc.enabled = false;

            playerTransform.position = targetPos;
            playerTransform.rotation = targetRot;

            Physics.SyncTransforms();

            if (cc != null) cc.enabled = true;
        }

        private void ResetMonster()
        {
            if (monster == null) return;

            if (monster.State == MonsterState.Scripted)
            {
                monster.ExitScripted();
            }

            monster.ResetToDormant();

            if (PointsByBeat.TryGetValue(Progress.Current, out CheckpointPoint point) && point.monsterPoint != null)
            {
                if (monster.Agent != null && monster.Agent.isOnNavMesh)
                {
                    monster.Agent.Warp(point.monsterPoint.position);
                }
                else
                {
                    monster.transform.position = point.monsterPoint.position;
                }
            }
        }

        private void OnBeatChanged(Beat newBeat)
        {
            if (_isDead)
            {
                _isDead = false;
                EchoEvents.UnlockInput();
            }

            // Automatically snapshot on every beat transition (including F1 jumps)
            SaveCheckpoint();
        }
    }
}


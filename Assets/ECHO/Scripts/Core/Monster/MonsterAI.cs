using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;
using Echo.Shared;

namespace Echo.Core
{
    /// <summary>
    /// Sound-driven monster AI. The creature has no eyes; it hunts purely by noise.
    ///
    /// States: Dormant → Investigate → Chase / Search → Dormant (or Attack on kill-range hit).
    ///
    /// Add this component and a NavMeshAgent to the monster GameObject.
    /// Assign playerTransform and tuning in the Inspector.
    /// Wire the three UnityEvents to any responders (audio, lighting, UI, C-09 death handler).
    /// </summary>
    [RequireComponent(typeof(NavMeshAgent))]
    public class MonsterAI : MonoBehaviour
    {
        // ── Active beats: monster AI is only live during these. ───────────────
        private static readonly HashSet<Beat> ActiveBeats = new HashSet<Beat>
        {
            Beat.Sneak,
            Beat.CoreHunt,
            Beat.PowerOn,
            Beat.Chase,
        };

        // ── Inspector fields ──────────────────────────────────────────────────

        [Header("References")]
        [Tooltip("The player's Transform. Used ONLY for kill-range distance check. " +
                 "The monster does NOT chase this position — it chases the last heard noise.")]
        [SerializeField] private Transform playerTransform;

        [SerializeField] private GameTuning tuning;

        [Header("UnityEvents — wire to audio, lighting, C-09, etc.")]
        [Tooltip("Fired when entering the Chase state.")]
        public UnityEvent OnChaseStarted  = new UnityEvent();

        [Tooltip("Fired when leaving the Chase state (to Search or Dormant).")]
        public UnityEvent OnChaseEnded    = new UnityEvent();

        [Tooltip("Fired exactly once when the monster kills the player. " +
                 "Wire to C-09's death handler and any death audio / UI.")]
        public UnityEvent OnPlayerCaught  = new UnityEvent();

        // ── Public read-only state ────────────────────────────────────────────

        /// <summary>Current AI state. Read by C-06 tests, C-07 scripted mode, C-08 chase ending.</summary>
        public MonsterState State { get; private set; } = MonsterState.Dormant;

        /// <summary>Current alert meter value. Read by C-06 tests and debug overlay.</summary>
        public float AlertValue => _alert.Value;

        /// <summary>Hearing radius from GameTuning. Read by C-06 tests and Gizmos.</summary>
        public float HearingRadius => tuning != null ? tuning.hearingRadius : 0f;

        // ── NavMeshAgent — kept internal but accessible to C-07 scripted mode ─
        // C-07 will call EnterScripted() / ExitScripted() and will need the agent.
        // 'protected' would expose to subclasses but not across assemblies;
        // we use a property so C-07 can read it without a field rename later.
        internal NavMeshAgent Agent => _agent;

        // ── Private fields ────────────────────────────────────────────────────

        private NavMeshAgent _agent;
        private AlertMeter   _alert = new AlertMeter();

        private bool     _isActive;              // true when current beat is in ActiveBeats
        private bool     _isScripted;            // C-07 takeover flag
        private bool     _attackFired;           // ensures OnPlayerCaught fires only once

        private Vector3  _lastHeardPosition;     // monster chases THIS, never the player's real pos
        private float    _lastHeardTime = -999f; // Time.time when the position was last updated

        private Coroutine _searchRoutine;

        // ── Unity callbacks ───────────────────────────────────────────────────

        /// <summary>Awake: cache the NavMeshAgent (never use GetComponent in Update).</summary>
        private void Awake()
        {
            _agent = GetComponent<NavMeshAgent>();
            _agent.updateRotation = true;
            _agent.updateUpAxis   = false; // flat floor — don't tilt the monster

            // Resolve tuning-driven beat state immediately so an F1 jump works
            // even if BeatChanged fired before this component was enabled.
            _isActive = ActiveBeats.Contains(Progress.Current);

            if (tuning != null)
                _alert.Configure(tuning.hearingRadius, tuning.alertGainScale, tuning.alertDecayPerSecond);
        }

        /// <summary>OnEnable: subscribe to events. Called on every re-enable, not just first time.</summary>
        private void OnEnable()
        {
            EchoEvents.Noise       += OnNoise;
            EchoEvents.BeatChanged += OnBeatChanged;
        }

        /// <summary>OnDisable: always unsubscribe. Guards against domain-reload and object disable.</summary>
        private void OnDisable()
        {
            EchoEvents.Noise       -= OnNoise;
            EchoEvents.BeatChanged -= OnBeatChanged;
        }

        /// <summary>Update: tick the alert meter and run the state machine each frame.</summary>
        private void Update()
        {
            if (tuning == null || _isScripted) return;

            _alert.Tick(Time.deltaTime);

            // Write our position to Progress every frame for audio/lighting lanes.
            Progress.MonsterPosition = transform.position;

            if (!_isActive)
            {
                // Inactive beat: ensure Dormant, stop the agent.
                if (State != MonsterState.Dormant)
                    EnterDormant();
                return;
            }

            TickStateMachine();
        }

        // ── Gizmos ───────────────────────────────────────────────────────────

        /// <summary>OnDrawGizmosSelected: drawn only when this object is selected in the Editor.</summary>
        private void OnDrawGizmosSelected()
        {
            float radius = tuning != null ? tuning.hearingRadius : 18f;
            Gizmos.color = new Color(1f, 0.9f, 0f, 0.3f);
            Gizmos.DrawWireSphere(transform.position, radius);

            // Last heard position (only meaningful in Play Mode).
            if (Application.isPlaying && _lastHeardTime > 0f)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawSphere(_lastHeardPosition, 0.25f);
                Gizmos.DrawLine(transform.position, _lastHeardPosition);
            }
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Returns the monster to Dormant and resets the alert meter.
        /// Call after a death sequence finishes (C-09) or for a checkpoint reset.
        /// </summary>
        public void ResetToDormant()
        {
            StopSearchRoutine();
            _attackFired = false;
            _alert.Reset();
            EnterDormant();
        }

        // ── C-07 scripted-mode hooks (stubs — C-07 adds the body) ─────────────
        // These exist now so C-07 can fill them in without changing the signature.

        /// <summary>
        /// Hands control of the monster to a scripted sequence (C-07/C-08).
        /// The NavMeshAgent stays enabled so the caller can drive it directly via Agent.
        /// MonsterAI stops reacting to noise and stops updating the state machine.
        /// Beat changes to inactive beats are also suppressed while scripted.
        /// </summary>
        public void EnterScripted()
        {
            StopSearchRoutine();   // cancel any in-progress search coroutine
            _isScripted = true;
            ChangeState(MonsterState.Scripted);
        }

        /// <summary>
        /// Returns AI control to MonsterAI after a scripted sequence ends.
        /// Resets to Dormant so the AI restarts cleanly.
        /// </summary>
        public void ExitScripted()
        {
            _isScripted = false;
            ResetToDormant();
        }

        // ── Event handlers ────────────────────────────────────────────────────

        private void OnBeatChanged(Beat newBeat)
        {
            _isActive = ActiveBeats.Contains(newBeat);

            if (!_isActive && !_isScripted)
                ResetToDormant();
        }

        private void OnNoise(Vector3 position, float loudness)
        {
            // Ignore noise when not active, in Attack, or scripted.
            if (!_isActive || _isScripted || State == MonsterState.Attack) return;

            float added = _alert.Add(position, loudness, transform.position);
            if (added <= 0f) return; // outside hearing radius

            // Update the heard position.
            _lastHeardPosition = position;
            _lastHeardTime     = Time.time;

            // Escalate state from the current one based on new alert level.
            EscalateFromNoise();
        }

        // ── State machine ─────────────────────────────────────────────────────

        private void TickStateMachine()
        {
            // Kill check: ONLY fires while actively chasing. The monster hunts by sound —
            // it cannot "sense" you while dormant, investigating a noise, or wandering in
            // search. Removing the kill check from those states eliminates the perceived
            // wall-hack / line-of-sight behaviour where proximity alone caused death.
            if (State == MonsterState.Chase && playerTransform != null)
            {
                float dist = Vector3.Distance(transform.position, playerTransform.position);
                if (dist <= tuning.killRange)
                {
                    EnterAttack();
                    return;
                }
            }

            switch (State)
            {
                case MonsterState.Dormant:      TickDormant();      break;
                case MonsterState.Investigate:  TickInvestigate();  break;
                case MonsterState.Chase:        TickChase();        break;
                case MonsterState.Search:       /* coroutine-driven */ break;
                case MonsterState.Attack:       /* halted */        break;
            }
        }

        // ── Dormant ───────────────────────────────────────────────────────────

        private void EnterDormant()
        {
            _agent.isStopped = true;
            _agent.ResetPath();
            ChangeState(MonsterState.Dormant);
        }

        private void TickDormant()
        {
            // Noise events drive escalation; nothing to poll here.
        }

        // ── Investigate ───────────────────────────────────────────────────────

        private void EnterInvestigate()
        {
            _agent.isStopped = false;
            _agent.speed     = tuning.speedInvestigate;
            _agent.SetDestination(_lastHeardPosition);
            ChangeState(MonsterState.Investigate);
        }

        private void TickInvestigate()
        {
            // Keep destination fresh in case it was updated by a new noise.
            _agent.SetDestination(_lastHeardPosition);

            if (!HasArrivedAtDestination()) return;

            // Arrived: decide next state.
            if (_alert.IsAbove(tuning.chaseThreshold))
                EnterChase();
            else
                EnterSearch();
        }

        // ── Chase ─────────────────────────────────────────────────────────────

        private void EnterChase()
        {
            bool wasChasing = State == MonsterState.Chase;
            _agent.isStopped = false;
            _agent.speed     = tuning.speedChase;
            _agent.SetDestination(_lastHeardPosition);
            ChangeState(MonsterState.Chase);

            if (!wasChasing)
                OnChaseStarted?.Invoke();
        }

        private void TickChase()
        {
            // Always aim at the last heard position (updated by OnNoise).
            _agent.SetDestination(_lastHeardPosition);

            if (!HasArrivedAtDestination()) return;

            // Arrived at the noise position. Check memory window.
            bool heardRecently = (Time.time - _lastHeardTime) < tuning.noiseMemory;
            if (!heardRecently)
            {
                OnChaseEnded?.Invoke();
                EnterSearch();
            }
            // else: a new noise came in and updated _lastHeardPosition; stay chasing.
        }

        // ── Search ────────────────────────────────────────────────────────────

        private void EnterSearch()
        {
            StopSearchRoutine();
            _agent.isStopped = false;
            _agent.speed     = tuning.speedSearch;
            ChangeState(MonsterState.Search);
            _searchRoutine = StartCoroutine(SearchRoutine());
        }

        private IEnumerator SearchRoutine()
        {
            float elapsed = 0f;

            while (elapsed < tuning.searchTimeout)
            {
                // Pick a random point on the NavMesh near the last heard position.
                Vector3 randomDir    = Random.insideUnitSphere * tuning.searchRadius;
                randomDir.y          = 0f;
                Vector3 sampleOrigin = _lastHeardPosition + randomDir;

                if (NavMesh.SamplePosition(sampleOrigin, out NavMeshHit hit, tuning.searchRadius, NavMesh.AllAreas))
                    _agent.SetDestination(hit.position);

                // Walk to that waypoint (or until a noise interrupts).
                float waypointTimeout = 8f;
                float waited = 0f;
                while (!HasArrivedAtDestination() && waited < waypointTimeout)
                {
                    waited  += Time.deltaTime;
                    elapsed += Time.deltaTime;

                    // A new noise while searching? Let OnNoise/EscalateFromNoise handle the state change.
                    if (State != MonsterState.Search)
                        yield break;

                    yield return null;
                }

                elapsed += waited; // avoid double-counting if waypoint reached quickly
                yield return null;
            }

            // Timed out: go Dormant.
            _alert.Reset();
            EnterDormant();
        }

        private void StopSearchRoutine()
        {
            if (_searchRoutine != null)
            {
                StopCoroutine(_searchRoutine);
                _searchRoutine = null;
            }
        }

        // ── Attack ────────────────────────────────────────────────────────────

        private void EnterAttack()
        {
            StopSearchRoutine();
            _agent.isStopped = true;
            _agent.ResetPath();
            ChangeState(MonsterState.Attack);

            if (!_attackFired)
            {
                _attackFired = true;
                EchoEvents.Caught();         // notify cross-lane hub
                OnPlayerCaught?.Invoke();    // notify locally wired C-09 handler
            }
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        /// <summary>
        /// Decides whether to escalate from Dormant/Investigate to Investigate/Chase
        /// based on the current alert level after a new noise was heard.
        /// </summary>
        private void EscalateFromNoise()
        {
            if (_alert.IsAbove(tuning.chaseThreshold))
            {
                if (State == MonsterState.Dormant || State == MonsterState.Investigate)
                    EnterChase();
                else if (State == MonsterState.Chase)
                    EnterChase(); // refresh destination
                else if (State == MonsterState.Search)
                    EnterChase(); // noise during search: escalate
            }
            else if (_alert.IsAbove(tuning.investigateThreshold))
            {
                if (State == MonsterState.Dormant)
                    EnterInvestigate();
                else if (State == MonsterState.Search)
                    EnterInvestigate(); // small noise during search: downgrade to investigate
            }
            // Below investigate threshold: noise heard but alert too low to act yet.
        }

        private bool HasArrivedAtDestination()
        {
            if (_agent.pathPending)           return false;
            if (_agent.remainingDistance > _agent.stoppingDistance) return false;
            if (_agent.hasPath && _agent.velocity.sqrMagnitude > 0.01f) return false;
            return true;
        }

        private void ChangeState(MonsterState next)
        {
            if (State == next) return;
            State = next;
            EchoEvents.SetMonsterState((Echo.Shared.MonsterState)next);
        }
    }
}


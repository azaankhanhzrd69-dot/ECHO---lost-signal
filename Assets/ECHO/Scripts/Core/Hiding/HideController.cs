using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;
using Echo.Shared;

namespace Echo.Core
{
    /// <summary>
    /// Singleton-style MonoBehaviour that owns all hiding logic.
    /// Lives in the Main scene (or any persistent scene). Only one instance per session.
    ///
    /// Flow per hide:
    ///   BeginHide(spot) → evaluate rules → InstantKill or Normal/ForcedDeath branch.
    ///   Waits for EchoEvents.HideFinished(bool) from I-04's minigame.
    ///   On success: invoke OnHideSurvived, spot.OnExit, increment SafeHides if !intro,
    ///               ExitScripted on monster, advance beat if intro.
    ///   On failure: invoke OnHideDeath (C-09 handles death/respawn).
    ///
    /// Wire UnityEvents in Inspector. Do NOT call code from other lanes directly.
    /// </summary>
    public class HideController : MonoBehaviour
    {
        // ── Singleton registry (reset on Play start for domain-reload safety) ──

        /// <summary>The active HideController. Null if none is in the scene.</summary>
        public static HideController Instance { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => Instance = null;

        // ── Inspector fields ──────────────────────────────────────────────────

        [Header("References")]
        [Tooltip("The MonsterAI component on the creature.")]
        [SerializeField] private MonsterAI monster;

        [Tooltip("GameTuning ScriptableObject.")]
        [SerializeField] private GameTuning tuning;

        [Tooltip("Transform the monster is warped to during the intro hide (when C-07 did not place it there). " +
                 "This is the MedBay door point used by LungeSequence.")]
        [SerializeField] private Transform introMonsterPoint;

        [Header("UnityEvents — wire Interact (minigame), World (audio/lighting), C-09 (death)")]

        [Tooltip("Fired when a Normal or ForcedDeath locker hide starts. " +
                 "bool parameter = forceFail (true → I-04 should force the minigame to fail). " +
                 "Wire to HideMinigame (I-04) to start the banging minigame.")]
        public UnityEvent<bool> OnHideBegin = new UnityEvent<bool>();

        [Tooltip("Fired at each hideBangInterval while the hide is in progress. " +
                 "Wire to audio (bang sound).")]
        public UnityEvent OnBang = new UnityEvent();

        [Tooltip("Fired when the player dies during or immediately after a hide. " +
                 "Wire to C-09's death/respawn handler and death audio.")]
        public UnityEvent OnHideDeath = new UnityEvent();

        [Tooltip("Fired when the player successfully survives a hide. " +
                 "Wire to survival audio, progress UI, etc.")]
        public UnityEvent OnHideSurvived = new UnityEvent();

        // ── Public state ──────────────────────────────────────────────────────

        /// <summary>The spot currently being used. Null when no hide is in progress.</summary>
        public HideSpot CurrentSpot { get; private set; }

        /// <summary>True while a hide coroutine is running.</summary>
        public bool IsHiding { get; private set; }

        // ── Private fields ────────────────────────────────────────────────────

        private Coroutine _routine;
        private bool      _waitingForMinigame; // true while waiting for HideFinished event
        private bool?     _hideResult;          // set by the HideFinished event handler
        private float     _cooldownEnds;        // Time.time when cooldown expires

        // ── Unity callbacks ───────────────────────────────────────────────────

        /// <summary>Awake: register singleton. Warn if a duplicate exists.</summary>
        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning("[HideController] Duplicate instance destroyed.", this);
                Destroy(this);
                return;
            }
            Instance = this;
        }

        /// <summary>OnEnable: subscribe to events.</summary>
        private void OnEnable()
        {
            EchoEvents.HideFinished += OnHideFinished;
            EchoEvents.BeatChanged  += OnBeatChanged;
        }

        /// <summary>OnDisable: unsubscribe and abort any running hide.</summary>
        private void OnDisable()
        {
            EchoEvents.HideFinished -= OnHideFinished;
            EchoEvents.BeatChanged  -= OnBeatChanged;
            AbortHide();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Called by HideSpot.Interact(). Starts a hide if not already hiding and cooldown has elapsed.
        /// </summary>
        public void BeginHide(HideSpot spot)
        {
            if (IsHiding)
            {
                Debug.Log("[HideController] Already hiding — ignoring BeginHide.", this);
                return;
            }

            if (Time.time < _cooldownEnds)
            {
                Debug.Log("[HideController] Hide cooldown active — ignoring BeginHide.", this);
                return;
            }

            if (tuning == null)
            {
                Debug.LogError("[HideController] GameTuning not assigned.", this);
                return;
            }

            bool isIntro = Progress.Current == Beat.HideIntro;
            CurrentSpot = spot;
            _routine    = StartCoroutine(RunHide(spot, isIntro));
        }

        /// <summary>
        /// Aborts the current hide immediately — used by OnDisable, scene unload,
        /// HideSpot.OnDestroy, and F1 beat jump away from an active hide.
        /// Restores input lock and exits scripted mode.
        /// </summary>
        public void AbortHide()
        {
            if (_routine != null)
            {
                StopCoroutine(_routine);
                _routine = null;
            }

            IsHiding           = false;
            _waitingForMinigame = false;
            _hideResult        = null;

            if (CurrentSpot != null)
            {
                // Best-effort: invoke OnExit so World can restore camera/player pose.
                CurrentSpot.OnExit?.Invoke();
                CurrentSpot = null;
            }

            EchoEvents.UnlockInput();

            if (monster != null && monster.State == MonsterState.Scripted)
                monster.ExitScripted();
        }

        // ── Event handlers ────────────────────────────────────────────────────

        // HideFinished is raised by I-04's HideMinigame when it ends.
        private void OnHideFinished(bool survived)
        {
            if (!_waitingForMinigame) return;
            _waitingForMinigame = false;
            _hideResult = survived;
        }

        private void OnBeatChanged(Beat newBeat)
        {
            // F1 jump away from the active beat mid-hide: abort cleanly.
            if (IsHiding)
                AbortHide();
        }

        // ── Main hide coroutine ───────────────────────────────────────────────

        private IEnumerator RunHide(HideSpot spot, bool isIntro)
        {
            IsHiding = true;

            // Snapshot progress BEFORE the hide — C-09 can restore from this if the player dies.
            ProgressSnapshot snapshot = Progress.Snapshot();

            // ── Step 1: Lock input and play the entry animation. ──────────────
            EchoEvents.LockInput();
            spot.OnEnter?.Invoke();
            EchoEvents.StartHide(spot.transform.position);

            // ── Step 2: Evaluate the rules. ───────────────────────────────────
            HideOutcome outcome = HideRules.Evaluate(
                spot.kind, isIntro,
                Progress.SafeHides,
                tuning.safeHidesBeforeDeath);

            // ── Step 3: InstantKill branch (Table / Chair). ───────────────────
            if (outcome == HideOutcome.InstantKill)
            {
                yield return new WaitForSeconds(tuning.instantKillDelay);
                IsHiding    = false;
                CurrentSpot = null;
                EchoEvents.UnlockInput();
                OnHideDeath?.Invoke();
                _routine = null;
                yield break;
            }

            // ── Step 4: Normal or ForcedDeath — summon the monster. ──────────
            bool forceFail = outcome == HideOutcome.ForcedDeath;

            if (monster != null)
                yield return StartCoroutine(SummonMonster(spot, isIntro));

            // ── Step 5: Start the banging minigame (via UnityEvent). ─────────
            OnHideBegin?.Invoke(forceFail);

            // ── Step 6: Run bang loop while waiting for HideFinished. ─────────
            _waitingForMinigame = true;
            _hideResult         = null;
            float nextBang      = 0f;
            float bangElapsed   = 0f;

            while (_waitingForMinigame)
            {
                if (bangElapsed >= nextBang)
                {
                    OnBang?.Invoke();
                    nextBang += tuning.hideBangInterval;
                }
                bangElapsed += Time.deltaTime;
                yield return null;
            }

            bool survived = _hideResult ?? false;

            // ── Step 7: Process the result. ───────────────────────────────────
            if (survived)
            {
                OnHideSurvived?.Invoke();
                spot.OnExit?.Invoke();

                if (!isIntro)
                    Progress.SafeHides++;

                // Release the monster back to AI control.
                if (monster != null)
                    monster.ExitScripted(); // calls ResetToDormant internally

                EchoEvents.UnlockInput();
                _cooldownEnds = Time.time + tuning.hideCooldown;

                // Intro hide surviving → advance to Sneak.
                if (isIntro)
                    EchoEvents.SetBeat(Beat.Sneak);
            }
            else
            {
                // Death: unlock input but leave monster scripted — C-09 handles respawn.
                EchoEvents.UnlockInput();
                OnHideDeath?.Invoke();
                // C-09 should call monster.ExitScripted() after respawn and
                // restore Progress from the snapshot taken at the start of this hide.
            }

            IsHiding    = false;
            CurrentSpot = null;
            _routine    = null;
        }

        // ── Monster summoning ─────────────────────────────────────────────────

        private IEnumerator SummonMonster(HideSpot spot, bool isIntro)
        {
            // If already scripted (C-07 left it at the door), do not re-enter.
            bool alreadyScripted = monster.State == MonsterState.Scripted;

            if (!alreadyScripted)
                monster.EnterScripted();

            NavMeshAgent agent = monster.Agent;

            if (isIntro)
            {
                // Intro: if monster is not already at the door, warp it to introMonsterPoint.
                if (!alreadyScripted && introMonsterPoint != null)
                {
                    agent.Warp(introMonsterPoint.position);
                    monster.transform.rotation = introMonsterPoint.rotation;
                }
                // Already at the door (C-07): nothing to move. Just start banging.
                yield break;
            }

            // CoreHunt hides: navigate to the spot's monsterPoint.
            Transform dest = spot.monsterPoint;
            if (dest == null)
            {
                Debug.LogWarning("[HideController] HideSpot has no monsterPoint assigned — " +
                                 "monster will not move to the spot.", this);
                yield break;
            }

            if (agent.isOnNavMesh)
            {
                agent.isStopped = false;
                agent.speed     = tuning.speedChase; // use chase speed to arrive fast
                agent.SetDestination(dest.position);

                float waited = 0f;
                while (!HasArrived(agent) && waited < tuning.monsterArriveTimeout)
                {
                    waited += Time.deltaTime;
                    yield return null;
                }

                // Warp if timed out.
                if (!HasArrived(agent))
                {
                    Debug.LogWarning("[HideController] Monster did not arrive in time — warping.", this);
                    agent.Warp(dest.position);
                }
            }
            else
            {
                agent.Warp(dest.position);
            }

            monster.transform.rotation = dest.rotation;
            agent.isStopped = true;
            agent.ResetPath();
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private static bool HasArrived(NavMeshAgent agent)
        {
            if (agent.pathPending)                                    return false;
            if (agent.remainingDistance > agent.stoppingDistance)     return false;
            if (agent.hasPath && agent.velocity.sqrMagnitude > 0.01f) return false;
            return true;
        }
    }
}


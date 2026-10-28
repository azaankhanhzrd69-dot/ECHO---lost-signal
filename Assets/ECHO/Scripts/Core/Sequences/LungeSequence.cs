using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;
using Echo.Shared;

namespace Echo.Core
{
    /// <summary>
    /// Drives the Lunge beat scripted sequence:
    ///   1) Warp monster to eating spot + enter scripted mode.
    ///   2) Pause (notice time).
    ///   3) Dash to door point via NavMeshAgent (falls back to direct move if no NavMesh).
    ///   4) Fire OnDoorLocked (World wires the door).
    ///   5) Bang phase: fire OnBang at intervals.
    ///   6) Advance beat to HideIntro. Monster stays at door in Scripted state (C-08 releases it).
    ///
    /// Assign this MonoBehaviour to any scene object in the MedBay area.
    /// It subscribes to EchoEvents.BeatChanged on Enable and unsubscribes on Disable.
    /// </summary>
    public class LungeSequence : MonoBehaviour
    {
        // ── Inspector fields ──────────────────────────────────────────────────

        [Header("Scene References")]
        [Tooltip("Transform placed at the monster's eating position in the MedBay. " +
                 "The monster is warped here at the start of the sequence.")]
        [SerializeField] private Transform eatingSpot;

        [Tooltip("Transform placed at the MedBay exit door. " +
                 "The monster dashes to this point and stays here banging.")]
        [SerializeField] private Transform doorPoint;

        [Tooltip("The MonsterAI component on the creature GameObject.")]
        [SerializeField] private MonsterAI monster;

        [Tooltip("GameTuning ScriptableObject. Provides timing and speed values.")]
        [SerializeField] private GameTuning tuning;

        [Header("UnityEvents — wire in Inspector, never in code")]
        [Tooltip("Fired once when the monster reaches the door. " +
                 "Wire to World's DoorController to lock it.")]
        public UnityEvent OnDoorLocked = new UnityEvent();

        [Tooltip("Fired at each bang interval during the banging phase. " +
                 "Wire to Interact's audio / haptic system.")]
        public UnityEvent OnBang = new UnityEvent();

        // ── Private state ─────────────────────────────────────────────────────

        private Coroutine _routine;
        private bool      _hasRun; // prevent running twice per session

        // ── Unity callbacks ───────────────────────────────────────────────────

        /// <summary>OnEnable: subscribe to beat changes. Fires on every enable.</summary>
        private void OnEnable()
        {
            EchoEvents.BeatChanged += OnBeatChanged;

            // Handle the case where the beat was already Lunge before this component enabled
            // (e.g. scene was loaded with Lunge already set, or F1 jump happened before enable).
            if (Progress.Current == Beat.Lunge && !_hasRun)
                StartSequence();
        }

        /// <summary>OnDisable: always unsubscribe and abort any running sequence.</summary>
        private void OnDisable()
        {
            EchoEvents.BeatChanged -= OnBeatChanged;
            AbortSequence();
        }

        // ── Beat handler ──────────────────────────────────────────────────────

        private void OnBeatChanged(Beat newBeat)
        {
            if (newBeat == Beat.Lunge)
            {
                if (!_hasRun)
                    StartSequence();
                return;
            }

            // Beat changed away from Lunge mid-sequence (e.g. F1 jump) — abort cleanly.
            if (_routine != null)
                AbortSequence();
        }

        // ── Sequence control ──────────────────────────────────────────────────

        private void StartSequence()
        {
            if (_routine != null) return; // already running
            _hasRun = true;
            _routine = StartCoroutine(RunLunge());
        }

        private void AbortSequence()
        {
            if (_routine != null)
            {
                StopCoroutine(_routine);
                _routine = null;
            }

            // Restore all locks regardless of where we were in the sequence.
            EchoEvents.UnlockInput();

            if (monster != null && monster.State == MonsterState.Scripted)
                monster.ExitScripted();
        }

        // ── Main coroutine ────────────────────────────────────────────────────

        private IEnumerator RunLunge()
        {
            // Validate references before doing anything visible.
            if (monster == null || eatingSpot == null || doorPoint == null || tuning == null)
            {
                Debug.LogError("[LungeSequence] Missing reference. Assign monster, eatingSpot, " +
                               "doorPoint and tuning in the Inspector.", this);
                _routine = null;
                _hasRun  = false;
                yield break;
            }

            // ── Step 1: Lock input and warp monster to eating spot. ───────────
            EchoEvents.LockInput();

            monster.EnterScripted();

            NavMeshAgent agent = monster.Agent;
            agent.Warp(eatingSpot.position);
            monster.transform.rotation = eatingSpot.rotation;

            // ── Step 2: Notice pause (monster idle, reacting to John). ────────
            yield return new WaitForSeconds(tuning.lungeNoticePause);

            // ── Step 3: Dash to the door point. ──────────────────────────────
            agent.isStopped = false;
            agent.speed     = tuning.lungeDashSpeed;

            bool reachedDoor;

            if (agent.isOnNavMesh)
            {
                agent.SetDestination(doorPoint.position);

                // Wait until the agent arrives (or a safety timeout).
                float dashTimeout = 20f;
                float waited      = 0f;
                while (!HasArrived(agent) && waited < dashTimeout)
                {
                    waited += Time.deltaTime;
                    yield return null;
                }
                reachedDoor = waited < dashTimeout;
            }
            else
            {
                // Fallback: no NavMesh baked — move in a straight line.
                Debug.LogWarning("[LungeSequence] Monster is not on NavMesh. " +
                                 "Using direct linear movement as fallback. Bake the NavMesh!", this);
                reachedDoor = false;

                Vector3 start = monster.transform.position;
                Vector3 end   = doorPoint.position;
                float   dist  = Vector3.Distance(start, end);
                float   dur   = dist / Mathf.Max(tuning.lungeDashSpeed, 0.1f);
                float   t     = 0f;

                while (t < dur)
                {
                    t += Time.deltaTime;
                    monster.transform.position = Vector3.Lerp(start, end, t / dur);
                    yield return null;
                }

                monster.transform.position = end;
                reachedDoor = true;
            }

            // Snap to the door point exactly and face the door.
            monster.transform.position = doorPoint.position;
            monster.transform.rotation = doorPoint.rotation;
            if (agent.isOnNavMesh)
            {
                agent.isStopped = true;
                agent.ResetPath();
            }

            // ── Step 4: Fire OnDoorLocked (World wires DoorController here). ──
            OnDoorLocked?.Invoke();

            // ── Step 5: Banging phase. ────────────────────────────────────────
            float bangElapsed  = 0f;
            float nextBang     = 0f; // fire immediately on first frame

            while (bangElapsed < tuning.lungeBangDuration)
            {
                if (bangElapsed >= nextBang)
                {
                    OnBang?.Invoke();
                    nextBang += tuning.lungeBangInterval;
                }
                bangElapsed += Time.deltaTime;
                yield return null;
            }

            // ── Step 6: Advance beat. Monster stays Scripted (C-08 exits it). ─
            // Do NOT call ExitScripted here — the hide minigame needs the monster
            // at the door in scripted mode. C-08 calls ExitScripted when the
            // hiding sequence ends.
            EchoEvents.UnlockInput();
            EchoEvents.SetBeat(Beat.HideIntro);

            _routine = null;
            // _hasRun stays true — do not run again for this session.
        }

        // ── Helper ────────────────────────────────────────────────────────────

        private static bool HasArrived(NavMeshAgent agent)
        {
            if (agent.pathPending)                                    return false;
            if (agent.remainingDistance > agent.stoppingDistance)     return false;
            if (agent.hasPath && agent.velocity.sqrMagnitude > 0.01f) return false;
            return true;
        }

#if UNITY_EDITOR
        /// <summary>OnDrawGizmosSelected: shows the eating spot and door point in the Editor.</summary>
        private void OnDrawGizmosSelected()
        {
            if (eatingSpot != null)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(eatingSpot.position, 0.4f);
                Gizmos.DrawIcon(eatingSpot.position, "sv_label_1", true);
            }

            if (doorPoint != null)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(doorPoint.position, 0.4f);
                if (eatingSpot != null)
                    Gizmos.DrawLine(eatingSpot.position, doorPoint.position);
            }
        }
#endif
    }
}


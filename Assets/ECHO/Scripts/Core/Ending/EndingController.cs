using System.Collections;
using Echo.Shared;
using UnityEngine;
using UnityEngine.Events;

namespace Echo.Core
{
    /// <summary>
    /// Core director for the Airlock sequence and the Good/Secret Ending flow:
    /// - Beat.Airlock: Locks player input, puts monster in scripted mode, invokes OnAirlockBegin,
    ///   and transitions to Beat.Ending when AirlockFinished() is called or safety timeout expires.
    /// - Beat.Ending: Evaluates EndingRules, stages Good Ending followed by Secret Ending (if MorseSent),
    ///   and coordinates with World cutscenes and Interact UI via UnityEvents.
    /// </summary>
    public sealed class EndingController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private MonsterAI monster;
        [SerializeField] private GameTuning tuning;

        [Header("UnityEvents — wire World cutscenes and Interact screens")]
        [Tooltip("Invoked when Beat.Airlock begins. Wire World's hatch/doors and monster suck-out cutscene.")]
        public UnityEvent OnAirlockBegin = new UnityEvent();

        [Tooltip("Invoked when Beat.Ending begins (asteroid approach / station exterior).")]
        public UnityEvent OnEndingBegin = new UnityEvent();

        [Tooltip("Invoked when Good Ending narrative starts.")]
        public UnityEvent OnGoodEnding = new UnityEvent();

        [Tooltip("Invoked when Secret Ending narrative starts (only if Morse was sent).")]
        public UnityEvent OnSecretEnding = new UnityEvent();

        [Tooltip("Invoked when the ending sequence has completed.")]
        public UnityEvent OnEndingFinished = new UnityEvent();

        // ── State ─────────────────────────────────────────────────────────────
        private Coroutine _airlockRoutine;
        private Coroutine _endingRoutine;
        private bool _airlockFinishedSignal;
        private bool _goodEndingFinishedSignal;
        private bool _secretEndingFinishedSignal;
        private bool _enteredScripted;

        // ── Unity Callbacks ───────────────────────────────────────────────────

        private void OnEnable()
        {
            EchoEvents.BeatChanged += OnBeatChanged;
        }

        private void OnDisable()
        {
            EchoEvents.BeatChanged -= OnBeatChanged;
            StopAllSequences();
        }

        // ── Public Signal Methods (called by World cutscenes or UI) ───────────

        /// <summary>
        /// Call to signal that World's airlock cutscene has concluded.
        /// </summary>
        public void AirlockFinished()
        {
            _airlockFinishedSignal = true;
        }

        /// <summary>
        /// Call to advance past the Good Ending phase immediately.
        /// </summary>
        public void GoodEndingFinished()
        {
            _goodEndingFinishedSignal = true;
        }

        /// <summary>
        /// Call to advance past the Secret Ending phase immediately.
        /// </summary>
        public void SecretEndingFinished()
        {
            _secretEndingFinishedSignal = true;
        }

        // ── Beat Routing ──────────────────────────────────────────────────────

        private void OnBeatChanged(Beat newBeat)
        {
            if (newBeat == Beat.Airlock)
            {
                StopAllSequences();
                _airlockRoutine = StartCoroutine(AirlockSequenceRoutine());
            }
            else if (newBeat == Beat.Ending)
            {
                // Ensure airlock routine is halted if we jumped directly to Ending
                if (_airlockRoutine != null)
                {
                    StopCoroutine(_airlockRoutine);
                    _airlockRoutine = null;
                }
                if (_endingRoutine != null)
                {
                    StopCoroutine(_endingRoutine);
                }
                _endingRoutine = StartCoroutine(EndingSequenceRoutine());
            }
            else
            {
                // Jumped away from Airlock/Ending (e.g. F1 debug jump)
                StopAllSequences();
            }
        }

        // ── Sequence Coroutines ───────────────────────────────────────────────

        private IEnumerator AirlockSequenceRoutine()
        {
            _airlockFinishedSignal = false;

            // Lock input and put monster in scripted mode
            EchoEvents.LockInput();

            if (monster != null)
            {
                monster.EnterScripted();
                _enteredScripted = true;
            }

            OnAirlockBegin?.Invoke();

            float timeout = tuning != null ? tuning.airlockTimeoutSeconds : 8.0f;
            float elapsed = 0f;

            while (!_airlockFinishedSignal && elapsed < timeout)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }

            _airlockRoutine = null;

            // Advance to Ending beat
            EchoEvents.SetBeat(Beat.Ending);
        }

        private IEnumerator EndingSequenceRoutine()
        {
            _goodEndingFinishedSignal = false;
            _secretEndingFinishedSignal = false;

            // Ensure input remains locked during endings
            EchoEvents.LockInput();

            OnEndingBegin?.Invoke();

            EndingPlan plan = EndingRules.Evaluate(Progress.MorseSent);

            // 1. Good Ending phase
            OnGoodEnding?.Invoke();
            float goodDuration = tuning != null ? tuning.goodEndingSeconds : 8.0f;
            float elapsedGood = 0f;

            while (!_goodEndingFinishedSignal && elapsedGood < goodDuration)
            {
                elapsedGood += Time.deltaTime;
                yield return null;
            }

            // 2. Secret Ending phase (only if Morse was sent)
            if (plan.HasSecret)
            {
                OnSecretEnding?.Invoke();
                float secretDuration = tuning != null ? tuning.secretEndingSeconds : 8.0f;
                float elapsedSecret = 0f;

                while (!_secretEndingFinishedSignal && elapsedSecret < secretDuration)
                {
                    elapsedSecret += Time.deltaTime;
                    yield return null;
                }
            }

            OnEndingFinished?.Invoke();
            _endingRoutine = null;
        }

        private void StopAllSequences()
        {
            if (_airlockRoutine != null)
            {
                StopCoroutine(_airlockRoutine);
                _airlockRoutine = null;
            }

            if (_endingRoutine != null)
            {
                StopCoroutine(_endingRoutine);
                _endingRoutine = null;
            }

            if (_enteredScripted && monster != null)
            {
                monster.ExitScripted();
                _enteredScripted = false;
            }

            _airlockFinishedSignal = false;
            _goodEndingFinishedSignal = false;
            _secretEndingFinishedSignal = false;
        }
    }
}


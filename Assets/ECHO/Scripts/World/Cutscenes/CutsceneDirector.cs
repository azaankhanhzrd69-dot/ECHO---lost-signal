using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using Echo.Shared;

namespace Echo.World
{
    /// <summary>
    /// Plays a <see cref="CutsceneSequence"/> by stepping through its
    /// <see cref="CutsceneStep"/> list in a coroutine.
    ///
    /// Setup (Inspector):
    ///   1. Assign <see cref="cutsceneCamera"/>: a separate scene Camera, disabled by default.
    ///   2. Assign <see cref="fadeCanvasGroup"/>: CanvasGroup on a black full-screen Image.
    ///   3. Leave <see cref="playerCamera"/> empty; the director finds Camera.main at play-time.
    ///
    /// The cutscene camera must NOT have an AudioListener — the player camera's
    /// AudioListener remains enabled throughout, so there is never a clash.
    /// </summary>
    public class CutsceneDirector : MonoBehaviour
    {
        // ── Inspector fields ──────────────────────────────────────────────────

        [Header("Cameras")]
        [Tooltip("The dedicated cutscene camera. Must be disabled in the scene by default. " +
                 "Do NOT add an AudioListener to it.")]
        [SerializeField] private Camera cutsceneCamera;

        [Header("Fade")]
        [Tooltip("CanvasGroup on a full-screen black Image. Alpha 0 = transparent, 1 = black.")]
        [SerializeField] private CanvasGroup fadeCanvasGroup;

        [Header("Skip")]
        [Tooltip("Mirror of CutsceneSequence.allowSkip. Overridden at runtime by the sequence asset.")]
        [SerializeField] private bool allowSkipKey = false;

        [Tooltip("Key the player holds to skip the current cutscene (if allowSkip is true on the sequence).")]
        [SerializeField] private Key skipKey = Key.Space;

        [Tooltip("Seconds the skip key must be held before the cutscene is interrupted.")]
        [SerializeField] private float skipHoldDuration = 1f;

        // ── Public state ──────────────────────────────────────────────────────

        /// <summary>True while a cutscene coroutine is running.</summary>
        public bool IsPlaying { get; private set; }

        // ── Private ───────────────────────────────────────────────────────────

        private Camera       _playerCamera;   // resolved at Play() time via Camera.main
        private Coroutine    _routine;
        private Beat         _beatAtStart;    // detect external beat change mid-cutscene

        // ── Unity callbacks ───────────────────────────────────────────────────

        private void OnEnable()
        {
            // Stop if the beat changes externally (e.g. F1 debug jump).
            EchoEvents.BeatChanged += OnBeatChanged;
        }

        private void OnDisable()
        {
            EchoEvents.BeatChanged -= OnBeatChanged;
            Stop(); // restore all on scene unload or component toggle
        }

        private void OnDestroy()
        {
            Stop();
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Starts playing <paramref name="sequence"/>. Ignored (with one warning) if already playing.
        /// </summary>
        public void Play(CutsceneSequence sequence)
        {
            if (IsPlaying)
            {
                Debug.LogWarning("[CutsceneDirector] Play() called while already playing. Ignored.", this);
                return;
            }

            if (sequence == null || sequence.steps == null || sequence.steps.Count == 0)
            {
                Debug.LogWarning("[CutsceneDirector] Sequence is null or empty.", this);
                return;
            }

            _playerCamera = Camera.main; // resolved at runtime; player cam is tagged MainCamera
            _beatAtStart  = Progress.Current;
            allowSkipKey  = sequence.allowSkip;

            _routine = StartCoroutine(RunSequence(sequence));
        }

        /// <summary>
        /// Immediately stops the active cutscene and restores the player's camera and input.
        /// Safe to call when nothing is playing.
        /// </summary>
        public void Stop()
        {
            if (_routine != null)
            {
                StopCoroutine(_routine);
                _routine = null;
            }
            RestoreAll();
        }

        // ── Core coroutine ────────────────────────────────────────────────────

        private IEnumerator RunSequence(CutsceneSequence sequence)
        {
            IsPlaying = true;
            EnterCutscene();

            float skipHeld = 0f;

            foreach (CutsceneStep step in sequence.steps)
            {
                yield return StartCoroutine(RunStep(step));

                // Skip-key polling between steps (and inside timed steps via sub-coroutine).
                if (allowSkipKey && CheckSkipHeld(ref skipHeld))
                    break;
            }

            RestoreAll();
        }

        private IEnumerator RunStep(CutsceneStep step)
        {
            switch (step.type)
            {
                case StepType.Wait:
                    yield return WaitWithSkip(step.duration);
                    break;

                case StepType.MoveCamera:
                    yield return MoveCameraStep(step);
                    break;

                case StepType.LookAt:
                    yield return LookAtStep(step);
                    break;

                case StepType.PlayLine:
                    // Fire the wired UnityEvent (e.g. DialoguePlayer.PlayById).
                    step.onStepEvent?.Invoke();
                    yield break;

                case StepType.SetBeat:
                    EchoEvents.SetBeat(step.beatParam);
                    yield break;

                case StepType.FadeIn:
                    yield return FadeStep(0f, 1f, step.duration);
                    break;

                case StepType.FadeOut:
                    yield return FadeStep(1f, 0f, step.duration);
                    break;

                case StepType.InvokeEvent:
                    step.onStepEvent?.Invoke();
                    yield break;

                default:
                    Debug.LogWarning($"[CutsceneDirector] Unhandled step type: {step.type}", this);
                    yield break;
            }
        }

        // ── Step sub-coroutines ───────────────────────────────────────────────

        private IEnumerator MoveCameraStep(CutsceneStep step)
        {
            if (cutsceneCamera == null) yield break;

            Transform from = step.fromWaypoint;
            Transform to   = step.toWaypoint;

            if (from != null)
            {
                cutsceneCamera.transform.position = from.position;
                cutsceneCamera.transform.rotation = from.rotation;
            }

            if (to == null || step.duration <= 0f)
            {
                if (to != null)
                {
                    cutsceneCamera.transform.position = to.position;
                    cutsceneCamera.transform.rotation = to.rotation;
                }
                yield break;
            }

            Vector3    startPos = cutsceneCamera.transform.position;
            Quaternion startRot = cutsceneCamera.transform.rotation;
            float      elapsed  = 0f;

            while (elapsed < step.duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / step.duration);
                float e = step.easing != null ? step.easing.Evaluate(t) : t;

                cutsceneCamera.transform.position = Vector3.Lerp(startPos, to.position, e);
                cutsceneCamera.transform.rotation = Quaternion.Slerp(startRot, to.rotation, e);
                yield return null;
            }

            cutsceneCamera.transform.position = to.position;
            cutsceneCamera.transform.rotation = to.rotation;
        }

        private IEnumerator LookAtStep(CutsceneStep step)
        {
            if (cutsceneCamera == null || step.toWaypoint == null) yield break;

            Quaternion startRot = cutsceneCamera.transform.rotation;
            Quaternion targetRot = Quaternion.LookRotation(
                step.toWaypoint.position - cutsceneCamera.transform.position);

            if (step.duration <= 0f)
            {
                cutsceneCamera.transform.rotation = targetRot;
                yield break;
            }

            float elapsed = 0f;
            while (elapsed < step.duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / step.duration);
                float e = step.easing != null ? step.easing.Evaluate(t) : t;
                cutsceneCamera.transform.rotation = Quaternion.Slerp(startRot, targetRot, e);
                yield return null;
            }

            cutsceneCamera.transform.rotation = targetRot;
        }

        private IEnumerator FadeStep(float from, float to, float duration)
        {
            if (fadeCanvasGroup == null) yield break;

            fadeCanvasGroup.alpha = from;

            if (duration <= 0f)
            {
                fadeCanvasGroup.alpha = to;
                yield break;
            }

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                fadeCanvasGroup.alpha = Mathf.Lerp(from, to, Mathf.Clamp01(elapsed / duration));
                yield return null;
            }

            fadeCanvasGroup.alpha = to;
        }

        private IEnumerator WaitWithSkip(float duration)
        {
            float elapsed  = 0f;
            float skipHeld = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                if (allowSkipKey && CheckSkipHeld(ref skipHeld))
                    yield break;
                yield return null;
            }
        }

        // ── Camera & input management ─────────────────────────────────────────

        private void EnterCutscene()
        {
            EchoEvents.LockInput();

            if (_playerCamera != null)
                _playerCamera.enabled = false;

            if (cutsceneCamera != null)
                cutsceneCamera.enabled = true;
        }

        /// <summary>
        /// Restores camera and input on every exit path.
        /// Idempotent: safe to call multiple times.
        /// </summary>
        private void RestoreAll()
        {
            IsPlaying = false;

            // Restore fade to fully transparent.
            if (fadeCanvasGroup != null)
                fadeCanvasGroup.alpha = 0f;

            // Swap cameras back.
            if (cutsceneCamera != null)
                cutsceneCamera.enabled = false;

            if (_playerCamera != null)
                _playerCamera.enabled = true;

            EchoEvents.UnlockInput();
        }

        // ── Skip helper ───────────────────────────────────────────────────────

        /// <summary>
        /// Returns true once the skip key has been held for <see cref="skipHoldDuration"/> seconds.
        /// Accumulates into <paramref name="heldSeconds"/>; resets on key release.
        /// </summary>
        private bool CheckSkipHeld(ref float heldSeconds)
        {
            if (Keyboard.current == null) return false;

            if (Keyboard.current[skipKey].isPressed)
            {
                heldSeconds += Time.deltaTime;
                if (heldSeconds >= skipHoldDuration)
                    return true;
            }
            else
            {
                heldSeconds = 0f;
            }
            return false;
        }

        // ── External beat change ──────────────────────────────────────────────

        private void OnBeatChanged(Beat newBeat)
        {
            // If the beat changes externally while playing (e.g. F1 jump),
            // abort the cutscene so the game state stays consistent.
            if (IsPlaying && newBeat != _beatAtStart)
                Stop();
        }
    }
}


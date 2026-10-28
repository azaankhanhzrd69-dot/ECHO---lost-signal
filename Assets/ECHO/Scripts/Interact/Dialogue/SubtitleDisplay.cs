using System.Collections;
using TMPro;
using UnityEngine;

namespace Echo.Interact
{
    /// <summary>
    /// Subtitle display controller. Shows speaker and text on HUD with smooth alpha fading.
    /// Uses TextMeshProUGUI and CanvasGroup.
    /// </summary>
    public sealed class SubtitleDisplay : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField]
        [Tooltip("TextMeshProUGUI component that renders the subtitle text.")]
        private TextMeshProUGUI subtitleText;

        [SerializeField]
        [Tooltip("CanvasGroup on the subtitle panel used for smooth alpha fading.")]
        private CanvasGroup canvasGroup;

        [Header("Animation Tuning")]
        [SerializeField]
        [Tooltip("Duration in seconds for fade in / fade out.")]
        private float fadeDuration = 0.2f;

        private Coroutine activeDisplayRoutine;
        private Coroutine activeFadeRoutine;

        // ── Unity Callbacks ────────────────────────────────────────────────────

        /// <summary>
        /// Awake: Ensure subtitles are hidden on scene load.
        /// </summary>
        private void Awake()
        {
            if (canvasGroup == null)
                canvasGroup = GetComponent<CanvasGroup>();

            SetAlphaImmediate(0f);
            if (subtitleText != null)
                subtitleText.text = string.Empty;
        }

        /// <summary>
        /// OnDisable: Stop running coroutines and reset alpha to prevent orphan animations.
        /// </summary>
        private void OnDisable()
        {
            if (activeDisplayRoutine != null)
            {
                StopCoroutine(activeDisplayRoutine);
                activeDisplayRoutine = null;
            }

            if (activeFadeRoutine != null)
            {
                StopCoroutine(activeFadeRoutine);
                activeFadeRoutine = null;
            }

            SetAlphaImmediate(0f);
        }

        // ── Public API ─────────────────────────────────────────────────────────

        /// <summary>
        /// Displays a subtitle formatted as "Speaker: Text" for the specified duration.
        /// Replaces any currently displayed line immediately.
        /// </summary>
        public void Show(string speaker, string text, float durationSeconds)
        {
            if (subtitleText == null)
            {
                Debug.LogWarning("[SubtitleDisplay] SubtitleText reference is missing.", this);
                return;
            }

            if (activeDisplayRoutine != null)
            {
                StopCoroutine(activeDisplayRoutine);
                activeDisplayRoutine = null;
            }

            activeDisplayRoutine = StartCoroutine(DisplayRoutine(speaker, text, durationSeconds));
        }

        /// <summary>
        /// Immediately starts fading out the current subtitle.
        /// </summary>
        public void Hide()
        {
            if (activeDisplayRoutine != null)
            {
                StopCoroutine(activeDisplayRoutine);
                activeDisplayRoutine = null;
            }

            StartFade(0f);
        }

        // ── Coroutines & Helpers ───────────────────────────────────────────────

        private IEnumerator DisplayRoutine(string speaker, string text, float durationSeconds)
        {
            if (string.IsNullOrWhiteSpace(speaker))
            {
                subtitleText.text = text;
            }
            else
            {
                subtitleText.text = $"<b>{speaker}:</b> {text}";
            }

            // Fade in to full visibility
            yield return StartFade(1f);

            // Wait for display duration (using unscaled delta time to be pause-menu safe)
            float elapsed = 0f;
            while (elapsed < durationSeconds)
            {
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }

            // Fade out
            yield return StartFade(0f);

            if (subtitleText != null)
                subtitleText.text = string.Empty;

            activeDisplayRoutine = null;
        }

        private Coroutine StartFade(float targetAlpha)
        {
            if (activeFadeRoutine != null)
                StopCoroutine(activeFadeRoutine);

            activeFadeRoutine = StartCoroutine(FadeRoutine(targetAlpha));
            return activeFadeRoutine;
        }

        private IEnumerator FadeRoutine(float targetAlpha)
        {
            if (canvasGroup == null)
                yield break;

            float startAlpha = canvasGroup.alpha;
            float duration = Mathf.Max(0.01f, fadeDuration);
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, elapsed / duration);
                yield return null;
            }

            canvasGroup.alpha = targetAlpha;
            activeFadeRoutine = null;
        }

        private void SetAlphaImmediate(float alpha)
        {
            if (canvasGroup != null)
                canvasGroup.alpha = alpha;
        }
    }
}


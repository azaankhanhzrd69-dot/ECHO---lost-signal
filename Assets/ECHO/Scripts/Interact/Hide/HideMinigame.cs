using Echo.Shared;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Echo.Interact
{
    /// <summary>
    /// Minigame state machine.
    /// </summary>
    public enum HideMinigameState
    {
        Inactive,
        Playing,
        Succeeded,
        Failed
    }

    /// <summary>
    /// Locker hiding minigame controller.
    /// Subscribes to EchoEvents.HideStarted, presents reticle/meter UI, locks player controls,
    /// applies Perlin shake and mouse counter-steering, and fires EchoEvents.EndHide.
    /// </summary>
    public sealed class HideMinigame : MonoBehaviour
    {
        [Header("UI Elements")]
        [SerializeField] private GameObject minigameCanvas;
        [SerializeField] private RectTransform centerRingTransform;
        [SerializeField] private RectTransform reticleTransform;
        [SerializeField] private Image meterFillBar;

        [Header("Tuning (Inspector Fallbacks for GameTuning)")]
        [SerializeField] private GameTuning gameTuning;

        [Tooltip("Total duration player must survive in the locker (seconds).")]
        [SerializeField] private float minigameDuration = 8f;

        [Tooltip("Radius of the safe center ring (pixels).")]
        [SerializeField] private float ringRadius = 120f;

        [Tooltip("Mouse counter-steering sensitivity.")]
        [SerializeField] private float mouseSensitivity = 1.2f;

        [Tooltip("Max screen extents for the reticle displacement.")]
        [SerializeField] private float maxDisplacement = 350f;

        [Tooltip("Strength of the Perlin noise shake pulling the reticle.")]
        [SerializeField] private float shakeStrength = 220f;

        [Tooltip("Speed/frequency of the Perlin shake.")]
        [SerializeField] private float shakeFrequency = 3.5f;

        [Tooltip("Fallback rate at which the meter fills while outside the ring (per second).")]
        [SerializeField] private float fallbackFillRate = 0.35f;

        [Tooltip("Fallback rate at which the meter decays while inside the ring (per second).")]
        [SerializeField] private float fallbackDecayRate = 0.5f;

        [Header("Audio Feedback (Optional)")]
        [SerializeField] private AudioClip bangingClip;
        [SerializeField] private AudioSource audioSource;

        private HideMinigameState state = HideMinigameState.Inactive;
        private Vector2 reticleOffset;
        private float bangingMeter;
        private float timer;
        private float noiseSeedX;
        private float noiseSeedY;
        private bool endHideFired;
        private CursorLockMode previousCursorLockMode = CursorLockMode.Locked;
        private bool previousCursorVisible = false;

        public HideMinigameState State => state;

        private void Awake()
        {
            noiseSeedX = Random.Range(0f, 1000f);
            noiseSeedY = Random.Range(1000f, 2000f);

            if (minigameCanvas != null)
                minigameCanvas.SetActive(false);

            if (audioSource == null)
                audioSource = GetComponent<AudioSource>();
        }

        private void OnEnable()
        {
            EchoEvents.HideStarted += OnHideStarted;
        }

        private void OnDisable()
        {
            EchoEvents.HideStarted -= OnHideStarted;

            if (state == HideMinigameState.Playing)
            {
                StopMinigame();
            }
        }

        private void Update()
        {
            if (state != HideMinigameState.Playing)
                return;

            float dt = Time.deltaTime;
            timer += dt;

            // 1. Accumulate player mouse input
            Vector2 mouseDelta = Vector2.zero;
            if (Mouse.current != null)
            {
                mouseDelta = Mouse.current.delta.ReadValue() * mouseSensitivity;
            }
            reticleOffset += mouseDelta;

            // 2. Add time-varying continuous Perlin shake
            float timeParam = Time.time * shakeFrequency;
            float shakeX = (Mathf.PerlinNoise(noiseSeedX + timeParam, 0f) - 0.5f) * 2f * shakeStrength * dt;
            float shakeY = (Mathf.PerlinNoise(0f, noiseSeedY + timeParam) - 0.5f) * 2f * shakeStrength * dt;
            reticleOffset += new Vector2(shakeX, shakeY);

            // Clamp reticle to bounds
            reticleOffset.x = Mathf.Clamp(reticleOffset.x, -maxDisplacement, maxDisplacement);
            reticleOffset.y = Mathf.Clamp(reticleOffset.y, -maxDisplacement, maxDisplacement);

            if (reticleTransform != null)
                reticleTransform.anchoredPosition = reticleOffset;

            // 3. Evaluate ring containment and update banging meter
            bool insideRing = HideMinigameLogic.IsInsideRing(reticleOffset.x, reticleOffset.y, ringRadius);

            float fillRate = (gameTuning != null) ? gameTuning.bangFillPerSecond : fallbackFillRate;
            float decayRate = (gameTuning != null) ? gameTuning.bangDecayPerSecond : fallbackDecayRate;

            bangingMeter = HideMinigameLogic.UpdateMeter(bangingMeter, insideRing, fillRate, decayRate, dt);

            if (meterFillBar != null)
                meterFillBar.fillAmount = bangingMeter;

            // 4. Check win/loss conditions
            int result = HideMinigameLogic.Evaluate(bangingMeter, timer, minigameDuration);
            if (result == -1)
            {
                // Failed -> Monster breaks in
                state = HideMinigameState.Failed;
                FinishMinigame(false);
            }
            else if (result == 1)
            {
                // Succeeded -> Monster gives up
                state = HideMinigameState.Succeeded;
                FinishMinigame(true);
            }
        }

        public void StartMinigame()
        {
            if (state == HideMinigameState.Playing)
                return;

            state = HideMinigameState.Playing;
            endHideFired = false;
            reticleOffset = Vector2.zero;
            bangingMeter = 0f;
            timer = 0f;

            if (centerRingTransform != null)
                centerRingTransform.sizeDelta = new Vector2(ringRadius * 2f, ringRadius * 2f);

            if (reticleTransform != null)
                reticleTransform.anchoredPosition = Vector2.zero;

            if (meterFillBar != null)
                meterFillBar.fillAmount = 0f;

            if (minigameCanvas != null)
                minigameCanvas.SetActive(true);

            // Store cursor state and lock/hide cursor for physical mouse deflection
            previousCursorLockMode = Cursor.lockState;
            previousCursorVisible = Cursor.visible;

            EchoEvents.LockInput();
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            if (bangingClip != null && audioSource != null)
            {
                audioSource.clip = bangingClip;
                audioSource.loop = true;
                audioSource.Play();
            }
        }

        private void StopMinigame()
        {
            state = HideMinigameState.Inactive;

            if (minigameCanvas != null)
                minigameCanvas.SetActive(false);

            if (audioSource != null && audioSource.isPlaying)
                audioSource.Stop();

            // Always restore cursor to previous state and release minigame's input lock.
            // HideLockController will maintain/re-assert lock until monster is far enough.
            Cursor.lockState = previousCursorLockMode;
            Cursor.visible = previousCursorVisible;
            EchoEvents.UnlockInput();
        }

        private void FinishMinigame(bool success)
        {
            StopMinigame();

            if (!endHideFired)
            {
                endHideFired = true;
                EchoEvents.EndHide(success);
            }
        }

        private void OnHideStarted(Vector3 _)
        {
            StartMinigame();
        }
    }
}


using System.Collections;
using Echo.Shared;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Echo.Interact
{
    /// <summary>
    /// Coordinates all UI screens: Title, Pause, Caught (Death), and Good/Secret Ending.
    /// Manages Time.timeScale, AudioListener.pause, cursor lock, volume sliders, and graphics mode.
    /// </summary>
    public sealed class ScreenManager : MonoBehaviour
    {
        public static ScreenManager Instance { get; private set; }

        [Header("Configuration")]
        [SerializeField] private InteractTuning tuning;
        [SerializeField] private string startSceneName = "";

        [Header("Screens / Panels")]
        [SerializeField] private GameObject titlePanel;
        [SerializeField] private GameObject pausePanel;
        [SerializeField] private GameObject caughtPanel;
        [SerializeField] private GameObject endingPanel;

        [Header("Pause Menu Audio Sliders")]
        [SerializeField] private Slider masterVolumeSlider;
        [SerializeField] private Slider sfxVolumeSlider;
        [SerializeField] private Slider musicVolumeSlider;

        [Header("Ending Screen Display")]
        [SerializeField] private Text endingBodyText;
        [SerializeField] private TextMeshProUGUI tmpEndingBodyText;
        [SerializeField] private Text endingTitleText;
        [SerializeField] private TextMeshProUGUI tmpEndingTitleText;
        [SerializeField] private Button endingReturnButton;

        [Header("Events")]
        [Tooltip("Invoked when the Caught death overlay appears. Wire death audio or visual stingers here.")]
        public UnityEvent OnCaughtShown = new UnityEvent();

        [Tooltip("Invoked when the player requests respawn from the Caught screen. Wire to Core C-09 respawn.")]
        public UnityEvent OnRespawnRequested = new UnityEvent();

        [Tooltip("Invoked when the Good Ending screen displays. Wire ending audio cues here.")]
        public UnityEvent OnEndingGoodShown = new UnityEvent();

        [Tooltip("Invoked when the Secret Ending screen displays. Wire secret audio cues here.")]
        public UnityEvent OnEndingSecretShown = new UnityEvent();

        private ScreenState _state = ScreenState.Title;
        private Coroutine _endingRoutine;

        public ScreenState CurrentState => _state;

        // ── Unity Callbacks ───────────────────────────────────────────────────

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            InitializeSliders();

            if (endingReturnButton != null)
                endingReturnButton.onClick.AddListener(OnReturnToTitleClicked);
        }

        private void Start()
        {
            // If in Title beat or bootstrap, show Title; otherwise hide and start in Gameplay
            if (Progress.Current == Beat.Boarding && titlePanel != null && titlePanel.activeSelf)
            {
                ShowTitle();
            }
            else
            {
                ShowGameplay();
            }
        }

        private void OnEnable()
        {
            EchoEvents.BeatChanged += OnBeatChanged;
        }

        private void OnDisable()
        {
            EchoEvents.BeatChanged -= OnBeatChanged;
            EnsureUnpaused();
            if (Instance == this) Instance = null;
        }

        private void Update()
        {
            if (Keyboard.current == null) return;

            // Esc toggles Pause during Gameplay or Pause
            if (Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                if (_state == ScreenState.Gameplay)
                {
                    Pause();
                }
                else if (_state == ScreenState.Pause)
                {
                    Resume();
                }
            }

            // In Caught screen, pressing Space or Enter triggers retry
            if (_state == ScreenState.Caught)
            {
                if (Keyboard.current.spaceKey.wasPressedThisFrame ||
                    Keyboard.current.enterKey.wasPressedThisFrame ||
                    Keyboard.current.numpadEnterKey.wasPressedThisFrame)
                {
                    OnRetryClicked();
                }
            }
        }

        // ── Screen Navigation ─────────────────────────────────────────────────

        public void ShowTitle()
        {
            _state = ScreenState.Title;
            EnsureUnpaused();

            SetPanelActive(titlePanel, true);
            SetPanelActive(pausePanel, false);
            SetPanelActive(caughtPanel, false);
            SetPanelActive(endingPanel, false);

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        public void ShowGameplay()
        {
            _state = ScreenState.Gameplay;
            EnsureUnpaused();

            SetPanelActive(titlePanel, false);
            SetPanelActive(pausePanel, false);
            SetPanelActive(caughtPanel, false);
            SetPanelActive(endingPanel, false);

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        public void Pause()
        {
            if (_state != ScreenState.Gameplay) return;

            _state = ScreenState.Pause;
            Time.timeScale = 0f;
            AudioListener.pause = true;

            SetPanelActive(pausePanel, true);

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            EchoEvents.LockInput();
        }

        public void Resume()
        {
            if (_state != ScreenState.Pause) return;

            _state = ScreenState.Gameplay;
            EnsureUnpaused();

            SetPanelActive(pausePanel, false);

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            EchoEvents.UnlockInput();
        }

        public void ShowCaught()
        {
            EnsureUnpaused();
            _state = ScreenState.Caught;

            SetPanelActive(pausePanel, false);
            SetPanelActive(titlePanel, false);
            SetPanelActive(endingPanel, false);
            SetPanelActive(caughtPanel, true);

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            OnCaughtShown?.Invoke();
        }

        public void OnRetryClicked()
        {
            if (_state != ScreenState.Caught) return;

            SetPanelActive(caughtPanel, false);
            ShowGameplay();

            OnRespawnRequested?.Invoke();
        }

        // ── Button Callbacks ──────────────────────────────────────────────────

        public void OnPlayClicked()
        {
            if (!string.IsNullOrEmpty(startSceneName))
            {
                SceneManager.LoadSceneAsync(startSceneName);
            }
            else
            {
                ShowGameplay();
                EchoEvents.SetBeat(Beat.Boarding);
            }
        }

        public void OnQuitClicked()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        public void OnReturnToTitleClicked()
        {
            ShowTitle();
            Progress.Reset();
            EchoEvents.SetBeat(Beat.Boarding);
        }

        // ── Ending Sequence ───────────────────────────────────────────────────

        private void OnBeatChanged(Beat newBeat)
        {
            // If the beat jumped or changed while paused, unfreeze time cleanly
            if (_state == ScreenState.Pause)
            {
                Resume();
            }

            if (newBeat == Beat.Ending)
            {
                if (_endingRoutine != null) StopCoroutine(_endingRoutine);
                _endingRoutine = StartCoroutine(EndingSequenceRoutine());
            }
        }

        private IEnumerator EndingSequenceRoutine()
        {
            _state = ScreenState.EndingGood;
            EnsureUnpaused();

            SetPanelActive(titlePanel, false);
            SetPanelActive(pausePanel, false);
            SetPanelActive(caughtPanel, false);
            SetPanelActive(endingPanel, true);
            if (endingReturnButton != null) endingReturnButton.gameObject.SetActive(false);

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            // 1. GOOD ENDING (always shown)
            SetEndingText("GOOD ENDING",
                "As you make your run to the spaceship, the creature follows you, ready to take you out before you can leave.\n\n" +
                "But you sprint for it and shut the door tight. As you open the hatch of the space station, the creature gets sucked out into the vast space.\n\n" +
                "Feeling your sense of safety, you start to make your way back to the ISS.\n\n" +
                "And then, an asteroid comes crashing into the spaceship, causing it to explode into smithereens.\n\n" +
                "And both of you are out cold. Lying in space. One waiting for help, the other all alone.\n\n" +
                "GOOD ENDING");

            OnEndingGoodShown?.Invoke();

            float goodDuration = tuning != null ? tuning.endingGoodDuration : 8.0f;
            yield return new WaitForSeconds(goodDuration);

            // 2. SECRET ENDING (shown after good ending if MorseSent == true)
            if (Progress.MorseSent)
            {
                _state = ScreenState.EndingSecret;
                SetEndingText("SECRET ENDING",
                    "The signal was received by the ISS. They are ready to retrieve you from the extraction spaceship.\n\n" +
                    "As you float into space, the ISS comes to retrieve your body. But they see something far more important than you.\n\n" +
                    "You are expendable. You always have been.\n\n" +
                    "As they prepare to put the suit on the creature, it comes back to life. With a hissing screech, it takes out the entire crew in swift swipes.\n\n" +
                    "As you breathe your last, you watch it make its way to the ISS.\n\n" +
                    "Preparing to invade Earth.\n\n" +
                    "SECRET ENDING");

                OnEndingSecretShown?.Invoke();

                float secretDuration = tuning != null ? tuning.endingSecretDuration : 8.0f;
                yield return new WaitForSeconds(secretDuration);
            }

            if (endingReturnButton != null)
                endingReturnButton.gameObject.SetActive(true);

            _endingRoutine = null;
        }

        private void SetEndingText(string title, string body)
        {
            if (endingTitleText != null) endingTitleText.text = title;
            if (tmpEndingTitleText != null) tmpEndingTitleText.text = title;

            if (endingBodyText != null) endingBodyText.text = body;
            if (tmpEndingBodyText != null) tmpEndingBodyText.text = body;
        }

        // ── Audio Sliders ─────────────────────────────────────────────────────

        private void InitializeSliders()
        {
            if (masterVolumeSlider != null)
            {
                masterVolumeSlider.value = PlayerPrefs.GetFloat("ECHO_Audio_Master", 1.0f);
                masterVolumeSlider.onValueChanged.AddListener(val =>
                {
                    if (AudioManager.Instance != null) AudioManager.Instance.SetMasterVolume(val);
                });
            }

            if (sfxVolumeSlider != null)
            {
                sfxVolumeSlider.value = PlayerPrefs.GetFloat("ECHO_Audio_Sfx", 1.0f);
                sfxVolumeSlider.onValueChanged.AddListener(val =>
                {
                    if (AudioManager.Instance != null) AudioManager.Instance.SetSfxVolume(val);
                });
            }

            if (musicVolumeSlider != null)
            {
                musicVolumeSlider.value = PlayerPrefs.GetFloat("ECHO_Audio_Music", 0.8f);
                musicVolumeSlider.onValueChanged.AddListener(val =>
                {
                    if (AudioManager.Instance != null) AudioManager.Instance.SetMusicVolume(val);
                });
            }
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private void EnsureUnpaused()
        {
            Time.timeScale = 1f;
            AudioListener.pause = false;
        }

        private static void SetPanelActive(GameObject go, bool active)
        {
            if (go != null) go.SetActive(active);
        }

        // ── Static Reset ──────────────────────────────────────────────────────

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            Instance = null;
            Time.timeScale = 1f;
            AudioListener.pause = false;
        }
    }
}


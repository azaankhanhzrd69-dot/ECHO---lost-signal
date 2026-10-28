using System.Collections;
using Echo.Shared;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Echo.Interact
{
    /// <summary>
    /// UI Controller for the Morse Radio:
    /// - Dot, Dash, Gap, Clear, Send button actions.
    /// - Real-time symbol display and decoded text preview.
    /// - Esc closes, frees/locks cursor, manages OnOpened/OnClosed.
    /// - Success sets Progress.MorseSent = true and invokes OnSendSuccess.
    /// - Failure invokes OnSendFail and shows feedback for failFeedbackSeconds.
    /// </summary>
    public sealed class MorseRadioUI : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private InteractTuning tuning;

        [Header("Panel Root")]
        [SerializeField] private GameObject panelRoot;

        [Header("UI Text Displays (uGUI Text or TextMeshProUGUI)")]
        [SerializeField] private Text symbolsLabel;
        [SerializeField] private Text previewLabel;
        [SerializeField] private Text feedbackLabel;
        [SerializeField] private TextMeshProUGUI tmpSymbolsLabel;
        [SerializeField] private TextMeshProUGUI tmpPreviewLabel;
        [SerializeField] private TextMeshProUGUI tmpFeedbackLabel;

        [Header("UI Buttons")]
        [SerializeField] private Button dotButton;
        [SerializeField] private Button dashButton;
        [SerializeField] private Button gapButton;
        [SerializeField] private Button clearButton;
        [SerializeField] private Button sendButton;

        [Header("UnityEvents")]
        public UnityEvent OnOpened = new UnityEvent();
        public UnityEvent OnClosed = new UnityEvent();
        public UnityEvent OnBeepDot = new UnityEvent();
        public UnityEvent OnBeepDash = new UnityEvent();
        public UnityEvent OnSendSuccess = new UnityEvent();
        public UnityEvent OnSendFail = new UnityEvent();

        private string _currentSymbols = string.Empty;
        private bool _isOpen;
        private bool _isBusy;
        private Coroutine _feedbackRoutine;

        public bool IsOpen => _isOpen;

        private void Awake()
        {
            if (dotButton != null) dotButton.onClick.AddListener(OnDotClicked);
            if (dashButton != null) dashButton.onClick.AddListener(OnDashClicked);
            if (gapButton != null) gapButton.onClick.AddListener(OnGapClicked);
            if (clearButton != null) clearButton.onClick.AddListener(OnClearClicked);
            if (sendButton != null) sendButton.onClick.AddListener(OnSendClicked);

            if (panelRoot != null)
                panelRoot.SetActive(false);
        }

        private void OnEnable()
        {
            EchoEvents.BeatChanged += OnBeatChanged;
        }

        private void OnDisable()
        {
            EchoEvents.BeatChanged -= OnBeatChanged;
            if (_isOpen)
                Close();
        }

        private void Update()
        {
            if (!_isOpen || _isBusy || Keyboard.current == null)
                return;

            Keyboard kb = Keyboard.current;

            // Esc closes
            if (kb.escapeKey.wasPressedThisFrame)
            {
                Close();
                return;
            }

            // Enter sends
            if (kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame)
            {
                OnSendClicked();
                return;
            }

            // Backspace deletes last character
            if (kb.backspaceKey.wasPressedThisFrame)
            {
                OnBackspace();
                return;
            }

            // Keyboard symbol shortcuts
            if (kb.periodKey.wasPressedThisFrame || kb.numpadPeriodKey.wasPressedThisFrame)
            {
                OnDotClicked();
            }
            else if (kb.minusKey.wasPressedThisFrame || kb.numpadMinusKey.wasPressedThisFrame)
            {
                OnDashClicked();
            }
            else if (kb.spaceKey.wasPressedThisFrame)
            {
                OnGapClicked();
            }
        }

        public void Open()
        {
            if (_isOpen) return;

            _isOpen = true;
            _currentSymbols = string.Empty;
            UpdateDisplay();
            SetFeedback(string.Empty);

            if (panelRoot != null)
                panelRoot.SetActive(true);

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            OnOpened?.Invoke();
        }

        public void Close()
        {
            if (!_isOpen) return;

            if (_feedbackRoutine != null)
            {
                StopCoroutine(_feedbackRoutine);
                _feedbackRoutine = null;
            }

            _isOpen = false;
            _isBusy = false;

            if (panelRoot != null)
                panelRoot.SetActive(false);

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            OnClosed?.Invoke();
        }

        // ── Button Callbacks ──────────────────────────────────────────────────

        public void OnDotClicked()
        {
            if (!_isOpen || _isBusy) return;
            int max = tuning != null ? tuning.maxSymbols : 32;
            if (_currentSymbols.Length >= max) return;

            _currentSymbols += ".";
            OnBeepDot?.Invoke();
            UpdateDisplay();
        }

        public void OnDashClicked()
        {
            if (!_isOpen || _isBusy) return;
            int max = tuning != null ? tuning.maxSymbols : 32;
            if (_currentSymbols.Length >= max) return;

            _currentSymbols += "-";
            OnBeepDash?.Invoke();
            UpdateDisplay();
        }

        public void OnGapClicked()
        {
            if (!_isOpen || _isBusy) return;
            int max = tuning != null ? tuning.maxSymbols : 32;
            if (_currentSymbols.Length >= max) return;

            // Prevent multiple trailing spaces
            if (_currentSymbols.Length > 0 && !_currentSymbols.EndsWith(" "))
            {
                _currentSymbols += " ";
                UpdateDisplay();
            }
        }

        public void OnClearClicked()
        {
            if (!_isOpen || _isBusy) return;
            _currentSymbols = string.Empty;
            SetFeedback(string.Empty);
            UpdateDisplay();
        }

        private void OnBackspace()
        {
            if (_currentSymbols.Length > 0)
            {
                _currentSymbols = _currentSymbols.Substring(0, _currentSymbols.Length - 1);
                UpdateDisplay();
            }
        }

        public void OnSendClicked()
        {
            if (!_isOpen || _isBusy) return;

            string expected = tuning != null ? tuning.expectedMessage : "SOS";
            bool success = MorseValidator.Matches(_currentSymbols, expected);

            if (success)
            {
                Progress.MorseSent = true;
                SetFeedback("<color=#00FF88>TRANSMISSION CONFIRMED</color>");
                OnSendSuccess?.Invoke();
                _isBusy = true;
                StartCoroutine(CloseAfterSuccessRoutine());
            }
            else
            {
                OnSendFail?.Invoke();
                if (_feedbackRoutine != null) StopCoroutine(_feedbackRoutine);
                _feedbackRoutine = StartCoroutine(FailFeedbackRoutine());
            }
        }

        // ── Feedback Coroutines ───────────────────────────────────────────────

        private IEnumerator CloseAfterSuccessRoutine()
        {
            yield return new WaitForSeconds(1.0f);
            Close();
        }

        private IEnumerator FailFeedbackRoutine()
        {
            _isBusy = true;
            SetFeedback("<color=#FF4444>TRANSMISSION FAILED - INVALID CODE</color>");
            float waitSec = tuning != null ? tuning.failFeedbackSeconds : 1.5f;
            yield return new WaitForSeconds(waitSec);
            _currentSymbols = string.Empty;
            UpdateDisplay();
            SetFeedback(string.Empty);
            _isBusy = false;
            _feedbackRoutine = null;
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private void UpdateDisplay()
        {
            string symbols = string.IsNullOrEmpty(_currentSymbols) ? "..." : _currentSymbols;
            string preview = MorseValidator.Decode(_currentSymbols);
            if (string.IsNullOrEmpty(preview))
                preview = " ";

            if (symbolsLabel != null) symbolsLabel.text = symbols;
            if (tmpSymbolsLabel != null) tmpSymbolsLabel.text = symbols;

            if (previewLabel != null) previewLabel.text = "DECODED: " + preview;
            if (tmpPreviewLabel != null) tmpPreviewLabel.text = "DECODED: " + preview;
        }

        private void SetFeedback(string text)
        {
            if (feedbackLabel != null) feedbackLabel.text = text;
            if (tmpFeedbackLabel != null) tmpFeedbackLabel.text = text;
        }

        private void OnBeatChanged(Beat newBeat)
        {
            if (_isOpen && newBeat != Beat.PowerOn)
            {
                Close();
            }
        }
    }
}


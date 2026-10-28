using Echo.Shared;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

namespace Echo.Interact
{
    /// <summary>
    /// Keypad UI controller:
    /// - Manages button inputs (0-9, Clear, Enter) and keyboard input (Alpha numbers, Numpad, Backspace, Return, Escape).
    /// - Manages cursor lock/free state and player input lock through EchoEvents.
    /// - Validates code via KeypadLogic; on success triggers onDoorUnlocked UnityEvent and sets Beat.MedBay.
    /// </summary>
    public sealed class KeypadUI : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField]
        private KeypadConfig config;

        [Header("UI References")]
        [SerializeField]
        [Tooltip("Root panel of the keypad canvas.")]
        private GameObject keypadPanel;

        [SerializeField]
        [Tooltip("Text display showing entered digits or status.")]
        private TextMeshProUGUI codeDisplay;

        [Header("Audio Feedback")]
        [SerializeField] private AudioClip buttonClickClip;
        [SerializeField] private AudioClip accessGrantedClip;
        [SerializeField] private AudioClip accessDeniedClip;
        [SerializeField] private AudioSource audioSource;

        [Header("Events")]
        [SerializeField]
        [Tooltip("Triggered when correct code is entered. Wire the decompression door open animation/action here.")]
        private UnityEvent onDoorUnlocked;

        private string currentInput = string.Empty;
        private bool isOpen;
        private bool isProcessingSuccess;
        private bool ownsInputLock;

        public bool IsOpen => isOpen;

        private void Awake()
        {
            if (audioSource == null)
                audioSource = GetComponent<AudioSource>();

            if (keypadPanel != null)
                keypadPanel.SetActive(false);
        }

        private void OnDisable()
        {
            if (isOpen)
                Close();
        }

        private void Update()
        {
            if (!isOpen || isProcessingSuccess || Keyboard.current == null)
                return;

            // Close keypad on Escape
            if (Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                Close();
                return;
            }

            // Enter key
            if (Keyboard.current.enterKey.wasPressedThisFrame || Keyboard.current.numpadEnterKey.wasPressedThisFrame)
            {
                OnSubmitPressed();
                return;
            }

            // Backspace / Clear
            if (Keyboard.current.backspaceKey.wasPressedThisFrame)
            {
                OnBackspacePressed();
                return;
            }

            // Number keys 0-9
            CheckNumberKeys();
        }

        private void CheckNumberKeys()
        {
            Keyboard kb = Keyboard.current;
            if (kb.digit0Key.wasPressedThisFrame || kb.numpad0Key.wasPressedThisFrame) OnDigitPressed("0");
            else if (kb.digit1Key.wasPressedThisFrame || kb.numpad1Key.wasPressedThisFrame) OnDigitPressed("1");
            else if (kb.digit2Key.wasPressedThisFrame || kb.numpad2Key.wasPressedThisFrame) OnDigitPressed("2");
            else if (kb.digit3Key.wasPressedThisFrame || kb.numpad3Key.wasPressedThisFrame) OnDigitPressed("3");
            else if (kb.digit4Key.wasPressedThisFrame || kb.numpad4Key.wasPressedThisFrame) OnDigitPressed("4");
            else if (kb.digit5Key.wasPressedThisFrame || kb.numpad5Key.wasPressedThisFrame) OnDigitPressed("5");
            else if (kb.digit6Key.wasPressedThisFrame || kb.numpad6Key.wasPressedThisFrame) OnDigitPressed("6");
            else if (kb.digit7Key.wasPressedThisFrame || kb.numpad7Key.wasPressedThisFrame) OnDigitPressed("7");
            else if (kb.digit8Key.wasPressedThisFrame || kb.numpad8Key.wasPressedThisFrame) OnDigitPressed("8");
            else if (kb.digit9Key.wasPressedThisFrame || kb.numpad9Key.wasPressedThisFrame) OnDigitPressed("9");
        }

        public void Open()
        {
            if (isOpen) return;

            isOpen = true;
            isProcessingSuccess = false;
            currentInput = string.Empty;
            UpdateDisplay("----");

            if (keypadPanel != null)
                keypadPanel.SetActive(true);

            // Lock gameplay input & unlock cursor for UI interaction
            ownsInputLock = !EchoEvents.InputLocked;
            if (ownsInputLock)
                EchoEvents.LockInput();

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        public void Close()
        {
            if (!isOpen) return;

            CancelInvoke(nameof(Close));

            isOpen = false;
            isProcessingSuccess = false;
            currentInput = string.Empty;

            if (keypadPanel != null)
                keypadPanel.SetActive(false);

            // Restore gameplay input & relock cursor
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            if (ownsInputLock)
            {
                ownsInputLock = false;
                EchoEvents.UnlockInput();
            }
        }

        public void OnDigitPressed(string digit)
        {
            if (isProcessingSuccess) return;

            int maxLen = config != null ? config.MaxCodeLength : 4;
            if (currentInput.Length >= maxLen)
                return;

            currentInput += digit;
            UpdateDisplay(currentInput);
            PlaySound(buttonClickClip);
        }

        public void OnClearPressed()
        {
            if (isProcessingSuccess) return;

            currentInput = string.Empty;
            UpdateDisplay("----");
            PlaySound(buttonClickClip);
        }

        public void OnBackspacePressed()
        {
            if (isProcessingSuccess || currentInput.Length == 0) return;

            currentInput = currentInput.Substring(0, currentInput.Length - 1);
            UpdateDisplay(currentInput.Length > 0 ? currentInput : "----");
            PlaySound(buttonClickClip);
        }

        public void OnSubmitPressed()
        {
            if (isProcessingSuccess) return;

            string targetCode = config != null ? config.CorrectCode : "0000";
            if (KeypadLogic.Check(currentInput, targetCode))
            {
                // Correct code
                isProcessingSuccess = true;
                UpdateDisplay("GRANTED");
                PlaySound(accessGrantedClip);

                onDoorUnlocked?.Invoke();

                if (Progress.Current == Beat.Keypad)
                {
                    EchoEvents.SetBeat(Beat.MedBay);
                }

                // Close UI after short feedback delay or immediately
                Invoke(nameof(Close), 1.0f);
            }
            else
            {
                // Wrong code
                UpdateDisplay("ERROR");
                PlaySound(accessDeniedClip);
                currentInput = string.Empty;
            }
        }

        private void UpdateDisplay(string text)
        {
            if (codeDisplay != null)
                codeDisplay.text = text;
        }

        private void PlaySound(AudioClip clip)
        {
            if (clip != null && audioSource != null)
            {
                audioSource.PlayOneShot(clip);
            }
        }
    }
}


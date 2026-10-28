using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Echo.Interact
{
    /// <summary>
    /// Graphics mode selector for Pause menu:
    /// 0 = Performance, 1 = Balanced, 2 = Cinematic.
    /// Saves chosen setting to PlayerPrefs ("echo.graphics") and invokes OnGraphicsModeSelected
    /// (wired to World's W-10a switcher).
    /// </summary>
    public sealed class GraphicsModeSelector : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private InteractTuning tuning;

        [Header("UI Buttons")]
        [SerializeField] private Button performanceButton;
        [SerializeField] private Button balancedButton;
        [SerializeField] private Button cinematicButton;

        [Header("Button Colors")]
        [SerializeField] private Color selectedColor = new Color(0.2f, 0.7f, 0.9f);
        [SerializeField] private Color normalColor = new Color(0.15f, 0.22f, 0.30f);

        [Header("Events")]
        [Tooltip("Invoked when a graphics mode is selected or restored on boot. 0=Performance, 1=Balanced, 2=Cinematic.")]
        public UnityEvent<int> OnGraphicsModeSelected = new UnityEvent<int>();

        private const string PrefsKey = "echo.graphics";
        private int _currentMode = 1;

        public int CurrentMode => _currentMode;

        private void Awake()
        {
            if (performanceButton != null) performanceButton.onClick.AddListener(() => SetMode(0));
            if (balancedButton != null) balancedButton.onClick.AddListener(() => SetMode(1));
            if (cinematicButton != null) cinematicButton.onClick.AddListener(() => SetMode(2));

            int fallback = tuning != null ? tuning.defaultGraphicsMode : 1;
            _currentMode = PlayerPrefs.GetInt(PrefsKey, fallback);

            UpdateButtonVisuals();
        }

        private void Start()
        {
            // Re-broadcast saved graphics mode on boot so World settings apply
            OnGraphicsModeSelected?.Invoke(_currentMode);
        }

        public void SetMode(int mode)
        {
            _currentMode = Mathf.Clamp(mode, 0, 2);
            PlayerPrefs.SetInt(PrefsKey, _currentMode);
            PlayerPrefs.Save();

            UpdateButtonVisuals();
            OnGraphicsModeSelected?.Invoke(_currentMode);
        }

        private void UpdateButtonVisuals()
        {
            SetButtonColor(performanceButton, _currentMode == 0);
            SetButtonColor(balancedButton, _currentMode == 1);
            SetButtonColor(cinematicButton, _currentMode == 2);
        }

        private void SetButtonColor(Button btn, bool isSelected)
        {
            if (btn == null) return;
            var img = btn.GetComponent<Image>();
            if (img != null)
            {
                img.color = isSelected ? selectedColor : normalColor;
            }
        }
    }
}


// Lane: Core | Namespace: Echo.Core
// Modular Acoustic Noise Meter HUD — displays live player noise level with studio-grade
// smooth damping (attack/decay ballistics), peak-hold needle, threshold notches,
// and modular screen docking options.
//
// Scale & Tiers:
//   0% - 15% : CROUCH  [≤ 2m audible] (Soft Green / Cyan)
//   16% - 50%: WALK    [Medium radius] (Amber / Yellow)
//   51% - 100%: SPRINT [High alert]    (Crimson / Red)
//
// Automatically persists across scenes via [RuntimeInitializeOnLoadMethod].

using Echo.Shared;
using UnityEngine;

namespace Echo.Core
{
    /// <summary>
    /// Acoustic noise meter HUD displaying player loudness with damped VU ballistics,
    /// threshold marks, and customizable modular dock positions.
    /// Subscribes to <see cref="EchoEvents.Noise"/>.
    /// </summary>
    public sealed class PlayerNoiseUI : MonoBehaviour
    {
        public enum ScreenAnchor
        {
            BottomCenter,
            BottomLeft,
            BottomRight,
            TopLeft,
            TopCenter,
            TopRight,
            Custom
        }

        public enum NoiseTier
        {
            Silent,
            Crouch,
            Walk,
            Sprint
        }

        // ── Singleton & Static Events ─────────────────────────────────────────
        public static PlayerNoiseUI Instance { get; private set; }
        public static event System.Action<float, NoiseTier> OnNoiseUpdated;

        [Header("Modular Placement")]
        [Tooltip("Screen dock anchor for the noise meter.")]
        [SerializeField] private ScreenAnchor anchor = ScreenAnchor.BottomCenter;

        [Tooltip("Pixel offset relative to the chosen anchor position.")]
        [SerializeField] private Vector2 anchorOffset = new Vector2(0f, -24f);

        [Tooltip("Absolute screen pixel coordinates when anchor is set to Custom.")]
        [SerializeField] private Vector2 customPosition = new Vector2(50f, 50f);

        [Header("Dimensions & Typography")]
        [Tooltip("Width of the noise meter bar in pixels.")]
        [SerializeField] private float meterWidth = 220f;

        [Tooltip("Height of the noise meter bar in pixels.")]
        [SerializeField] private float meterHeight = 11f;

        [Tooltip("Padding around the inner meter content inside the background panel.")]
        [SerializeField] private float panelPadding = 7f;

        [Tooltip("Font size for the status label and percentage readout.")]
        [SerializeField] private int fontSize = 11;

        [Header("Modular Features")]
        [Tooltip("Master visibility toggle for the HUD.")]
        [SerializeField] private bool showHUD = true;

        [Tooltip("Show the live status label (SILENT, CROUCH, WALKING, RUNNING).")]
        [SerializeField] private bool showStatusLabel = true;

        [Tooltip("Show the numeric noise percentage readout.")]
        [SerializeField] private bool showPercentage = true;

        [Tooltip("Show subtle tick marks on the meter at the crouch (15%) and walk (50%) thresholds.")]
        [SerializeField] private bool showThresholdNotches = true;

        [Tooltip("Show the peak-hold indicator needle marking maximum recent noise level.")]
        [SerializeField] private bool showPeakNeedle = true;

        [Tooltip("When true, the meter completely hides when player is silent. When false, it dims to silentAlpha.")]
        [SerializeField] private bool autoHideWhenSilent = false;

        [Tooltip("Idle opacity of the panel when player is making zero noise (0..1).")]
        [Range(0f, 1f)] [SerializeField] private float silentAlpha = 0.28f;

        [Header("Ballistics & Damping (Anti-Choppy Smoothing)")]
        [Tooltip("Speed at which the meter rises toward a new louder sound. Higher = snappier, lower = smoother.")]
        [Range(2f, 30f)] [SerializeField] private float attackSpeed = 12f;

        [Tooltip("Speed at which the displayed meter glides back down toward silence. Lower = slower, smoother decay.")]
        [Range(0.5f, 10f)] [SerializeField] private float decaySpeed = 2.2f;

        [Tooltip("Rate at which the raw noise target decays per second.")]
        [Range(0.1f, 3f)] [SerializeField] private float targetDecayRate = 0.45f;

        [Tooltip("Seconds the peak indicator needle is held at maximum before beginning to drop.")]
        [Range(0.1f, 2f)] [SerializeField] private float peakHoldDuration = 0.55f;

        [Tooltip("Speed at which the peak indicator needle falls after its hold time expires.")]
        [Range(0.2f, 5f)] [SerializeField] private float peakDecaySpeed = 1.2f;

        // ── Public Accessors ──────────────────────────────────────────────────
        public float CurrentLoudness => _displayedLoudness;
        public float PeakLoudness    => _peakLoudness;
        public NoiseTier CurrentTier { get; private set; } = NoiseTier.Silent;

        // ── Runtime State ─────────────────────────────────────────────────────
        private float _targetLoudness;
        private float _displayedLoudness;
        private float _peakLoudness;
        private float _peakHoldTimer;

        // ── Texture Cache ─────────────────────────────────────────────────────
        private static Texture2D _whiteTexture;
        private static Texture2D WhiteTexture
        {
            get
            {
                if (_whiteTexture == null)
                {
                    _whiteTexture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
                    _whiteTexture.SetPixel(0, 0, Color.white);
                    _whiteTexture.Apply();
                }
                return _whiteTexture;
            }
        }

        // ── Lifecycle ─────────────────────────────────────────────────────────

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoInitialize()
        {
            if (FindAnyObjectByType<PlayerNoiseUI>() == null)
            {
                var go = new GameObject("[PlayerNoiseUI]");
                go.AddComponent<PlayerNoiseUI>();
                DontDestroyOnLoad(go);
            }
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void OnEnable()  => EchoEvents.Noise += OnNoise;
        private void OnDisable() => EchoEvents.Noise -= OnNoise;

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
            if (_whiteTexture != null)
            {
                Destroy(_whiteTexture);
                _whiteTexture = null;
            }
        }

        private void Update()
        {
            float dt = Time.deltaTime;

            // 1. Decay raw target
            if (_targetLoudness > 0f)
            {
                _targetLoudness = Mathf.MoveTowards(_targetLoudness, 0f, targetDecayRate * dt);
            }

            // 2. Smoothly damp displayed loudness toward target
            if (_displayedLoudness < _targetLoudness)
            {
                // Rising: smooth attack
                _displayedLoudness = Mathf.Lerp(_displayedLoudness, _targetLoudness, 1f - Mathf.Exp(-attackSpeed * dt));
            }
            else
            {
                // Falling: slowed-down graceful decay
                _displayedLoudness = Mathf.Lerp(_displayedLoudness, _targetLoudness, 1f - Mathf.Exp(-decaySpeed * dt));
                if (_displayedLoudness < 0.005f) _displayedLoudness = 0f;
            }

            // 3. Peak needle ballistics
            if (_peakHoldTimer > 0f)
            {
                _peakHoldTimer -= dt;
            }
            else if (_peakLoudness > _displayedLoudness)
            {
                _peakLoudness = Mathf.MoveTowards(_peakLoudness, _displayedLoudness, peakDecaySpeed * dt);
            }
            else
            {
                _peakLoudness = _displayedLoudness;
            }

            // 4. Update tier
            UpdateCurrentTier();

            // 5. Fire change notification
            OnNoiseUpdated?.Invoke(_displayedLoudness, CurrentTier);
        }

        // ── Noise Listener ────────────────────────────────────────────────────

        private void OnNoise(Vector3 _position, float loudness)
        {
            float clamped = Mathf.Clamp01(loudness);
            _targetLoudness = Mathf.Max(_targetLoudness, clamped);

            if (clamped >= _peakLoudness)
            {
                _peakLoudness = clamped;
                _peakHoldTimer = peakHoldDuration;
            }
        }

        // ── Tier Calculation ──────────────────────────────────────────────────

        private void UpdateCurrentTier()
        {
            if (_displayedLoudness <= 0.01f)
                CurrentTier = NoiseTier.Silent;
            else if (_displayedLoudness <= 0.15f)
                CurrentTier = NoiseTier.Crouch;
            else if (_displayedLoudness <= 0.50f)
                CurrentTier = NoiseTier.Walk;
            else
                CurrentTier = NoiseTier.Sprint;
        }

        private static string GetStatusString(NoiseTier tier)
        {
            switch (tier)
            {
                case NoiseTier.Crouch: return "CROUCH [<=2m]";
                case NoiseTier.Walk:   return "WALKING";
                case NoiseTier.Sprint: return "RUNNING";
                default:               return "SILENT";
            }
        }

        private static Color GetTierColor(NoiseTier tier)
        {
            switch (tier)
            {
                case NoiseTier.Crouch: return new Color(0.25f, 0.90f, 0.45f, 0.95f); // Soft Stealth Green
                case NoiseTier.Walk:   return new Color(0.95f, 0.78f, 0.20f, 0.95f); // Amber / Yellow
                case NoiseTier.Sprint: return new Color(0.95f, 0.22f, 0.22f, 1.00f); // Alert Red
                default:               return new Color(0.35f, 0.65f, 0.55f, 0.70f); // Subdued Idle
            }
        }

        // ── OnGUI Rendering ───────────────────────────────────────────────────

        private void OnGUI()
        {
            if (!showHUD) return;

            // Calculate content heights
            float headerHeight = (showStatusLabel || showPercentage) ? 16f : 0f;
            float gap = (headerHeight > 0f) ? 4f : 0f;
            float totalContentHeight = headerHeight + gap + meterHeight;
            float totalWidth = meterWidth + panelPadding * 2f;
            float totalHeight = totalContentHeight + panelPadding * 2f;

            Rect panelRect = CalculatePanelRect(totalWidth, totalHeight);

            // Opacity fade when quiet
            float masterAlpha;
            if (_displayedLoudness <= 0.005f)
            {
                if (autoHideWhenSilent) return;
                masterAlpha = silentAlpha;
            }
            else
            {
                masterAlpha = Mathf.Clamp01(silentAlpha + (1f - silentAlpha) * (_displayedLoudness * 2.5f));
            }

            // 1. Panel chassis background & border
            Color panelBg = new Color(0.04f, 0.06f, 0.08f, 0.88f * masterAlpha);
            Color panelBorder = new Color(0.24f, 0.30f, 0.40f, 0.65f * masterAlpha);
            DrawSolidRect(panelRect, panelBg);
            DrawBorder(panelRect, panelBorder, 1f);

            float innerX = panelRect.x + panelPadding;
            float innerY = panelRect.y + panelPadding;

            // 2. Header: Status label (left) & Percentage (right)
            if (headerHeight > 0f)
            {
                Color tierColor = GetTierColor(CurrentTier);
                tierColor.a *= masterAlpha;

                if (showStatusLabel)
                {
                    var statusStyle = new GUIStyle(GUI.skin.label)
                    {
                        fontSize = fontSize,
                        fontStyle = FontStyle.Bold,
                        alignment = TextAnchor.MiddleLeft,
                        normal = { textColor = tierColor }
                    };
                    GUI.Label(new Rect(innerX, innerY, meterWidth * 0.65f, headerHeight), GetStatusString(CurrentTier), statusStyle);
                }

                if (showPercentage)
                {
                    var pctStyle = new GUIStyle(GUI.skin.label)
                    {
                        fontSize = fontSize,
                        fontStyle = FontStyle.Normal,
                        alignment = TextAnchor.MiddleRight,
                        normal = { textColor = new Color(0.85f, 0.88f, 0.92f, 0.9f * masterAlpha) }
                    };
                    int pct = Mathf.RoundToInt(_displayedLoudness * 100f);
                    GUI.Label(new Rect(innerX + meterWidth * 0.55f, innerY, meterWidth * 0.45f, headerHeight), $"{pct}%", pctStyle);
                }

                innerY += headerHeight + gap;
            }

            // 3. Meter track slot
            Rect meterRect = new Rect(innerX, innerY, meterWidth, meterHeight);
            Color trackColor = new Color(0.12f, 0.15f, 0.20f, 0.9f * masterAlpha);
            DrawSolidRect(meterRect, trackColor);

            // 4. Meter fill bar
            if (_displayedLoudness > 0.001f)
            {
                float fillW = Mathf.Clamp(meterWidth * _displayedLoudness, 2f, meterWidth);
                Rect fillRect = new Rect(innerX, innerY, fillW, meterHeight);
                Color fillColor = GetTierColor(CurrentTier);
                fillColor.a = 0.95f * masterAlpha;
                DrawSolidRect(fillRect, fillColor);
            }

            // 5. Threshold guideline notches (15% crouch, 50% walk)
            if (showThresholdNotches)
            {
                Color notchColor = new Color(0.45f, 0.55f, 0.65f, 0.45f * masterAlpha);
                float x15 = innerX + meterWidth * 0.15f;
                float x50 = innerX + meterWidth * 0.50f;
                DrawSolidRect(new Rect(x15, innerY, 1f, meterHeight), notchColor);
                DrawSolidRect(new Rect(x50, innerY, 1f, meterHeight), notchColor);
            }

            // 6. Peak needle indicator
            if (showPeakNeedle && _peakLoudness > 0.02f)
            {
                float peakX = innerX + Mathf.Clamp(meterWidth * _peakLoudness - 1f, 0f, meterWidth - 2f);
                Rect peakRect = new Rect(peakX, innerY - 1f, 2f, meterHeight + 2f);
                Color peakColor = Color.white;
                peakColor.a = 0.95f * masterAlpha;
                DrawSolidRect(peakRect, peakColor);
            }

            // 7. Outer border on the meter bar
            Color meterBorder = new Color(0.30f, 0.38f, 0.48f, 0.45f * masterAlpha);
            DrawBorder(meterRect, meterBorder, 1f);
        }

        // ── Helper Drawing Functions ──────────────────────────────────────────

        private Rect CalculatePanelRect(float totalWidth, float totalHeight)
        {
            float sw = Screen.width;
            float sh = Screen.height;
            float x;
            float y;

            switch (anchor)
            {
                case ScreenAnchor.BottomCenter:
                    x = (sw - totalWidth) * 0.5f + anchorOffset.x;
                    y = sh - totalHeight + anchorOffset.y;
                    break;
                case ScreenAnchor.BottomLeft:
                    x = 16f + anchorOffset.x;
                    y = sh - totalHeight + anchorOffset.y;
                    break;
                case ScreenAnchor.BottomRight:
                    x = sw - totalWidth - 16f + anchorOffset.x;
                    y = sh - totalHeight + anchorOffset.y;
                    break;
                case ScreenAnchor.TopLeft:
                    x = 16f + anchorOffset.x;
                    y = 16f + anchorOffset.y;
                    break;
                case ScreenAnchor.TopCenter:
                    x = (sw - totalWidth) * 0.5f + anchorOffset.x;
                    y = 16f + anchorOffset.y;
                    break;
                case ScreenAnchor.TopRight:
                    x = sw - totalWidth - 16f + anchorOffset.x;
                    y = 16f + anchorOffset.y;
                    break;
                case ScreenAnchor.Custom:
                    x = customPosition.x;
                    y = customPosition.y;
                    break;
                default:
                    x = (sw - totalWidth) * 0.5f;
                    y = sh - totalHeight - 20f;
                    break;
            }

            return new Rect(x, y, totalWidth, totalHeight);
        }

        private static void DrawSolidRect(Rect rect, Color color)
        {
            Color prev = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(rect, WhiteTexture);
            GUI.color = prev;
        }

        private static void DrawBorder(Rect rect, Color color, float thickness)
        {
            DrawSolidRect(new Rect(rect.x, rect.y, rect.width, thickness), color);
            DrawSolidRect(new Rect(rect.x, rect.y + rect.height - thickness, rect.width, thickness), color);
            DrawSolidRect(new Rect(rect.x, rect.y, thickness, rect.height), color);
            DrawSolidRect(new Rect(rect.x + rect.width - thickness, rect.y, thickness, rect.height), color);
        }
    }
}

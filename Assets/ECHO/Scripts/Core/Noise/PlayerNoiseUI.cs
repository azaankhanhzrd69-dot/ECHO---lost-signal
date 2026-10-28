// Lane: Core | Namespace: Echo.Core
// Player-facing noise level HUD — shows a colour-coded OnGUI indicator:
//   GREEN  = crouch / quiet    (loudness < 0.2)
//   YELLOW = walking           (loudness 0.2–0.7)
//   RED    = running / loud    (loudness > 0.7)
//
// The indicator fades out over fadeTime seconds between noise events.
// No Slider, no Image component. Draws on screen using OnGUI so it works
// with any UI setup without wiring anything in the Inspector.
// Toggle with the showHUD bool in the Inspector (default ON).

using Echo.Shared;
using UnityEngine;

namespace Echo.Core
{
    /// <summary>
    /// Draws a small colour-coded noise indicator in the top-left corner of the
    /// screen so the player always knows how much sound they are making.
    ///
    /// Subscribes to <see cref="EchoEvents.Noise"/>; no other dependencies.
    /// Add this component to any persistent GameObject (e.g. the player or a HUD manager).
    /// </summary>
    public sealed class PlayerNoiseUI : MonoBehaviour
    {
        [Header("HUD")]
        [Tooltip("Show or hide the noise indicator at runtime.")]
        [SerializeField] private bool showHUD = true;

        [Tooltip("Seconds the indicator stays visible after the last noise event before fading to zero.")]
        [SerializeField] private float fadeTime = 1.2f;

        [Tooltip("Size of the noise icon on screen (pixels).")]
        [SerializeField] private float iconSize = 24f;

        // ── Runtime ───────────────────────────────────────────────────────────
        private float _currentLoudness; // 0..1, decays each frame
        private float _decayRate;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void OnEnable()  => EchoEvents.Noise += OnNoise;
        private void OnDisable() => EchoEvents.Noise -= OnNoise;

        private void Update()
        {
            if (_currentLoudness <= 0f) return;
            _currentLoudness = Mathf.Max(0f, _currentLoudness - _decayRate * Time.deltaTime);
        }

        // ── Noise listener ────────────────────────────────────────────────────

        private void OnNoise(Vector3 _position, float loudness)
        {
            // Spike to the louder of the current and incoming value — prevents a
            // quiet footstep from resetting a loud sprint burst downward.
            _currentLoudness = Mathf.Max(_currentLoudness, Mathf.Clamp01(loudness));
            _decayRate       = fadeTime > 0f ? 1f / fadeTime : float.MaxValue;
        }

        // ── OnGUI ─────────────────────────────────────────────────────────────

        private void OnGUI()
        {
            if (!showHUD || _currentLoudness <= 0.01f) return;

            // Colour: green = quiet, yellow = walk, red = run
            Color colour;
            string label;
            if (_currentLoudness < 0.2f)
            {
                colour = Color.green;
                label  = "◉ QUIET";
            }
            else if (_currentLoudness < 0.7f)
            {
                colour = Color.yellow;
                label  = "◉ WALKING";
            }
            else
            {
                colour = Color.red;
                label  = "◉ RUNNING";
            }

            // Fade alpha as the indicator decays
            colour.a = Mathf.Clamp01(_currentLoudness * 2f);

            float x    = Screen.width  * 0.5f - 50f;   // horizontally centred
            float y    = Screen.height - 80f;            // near bottom of screen

            GUI.color = colour;
            var style = new GUIStyle(GUI.skin.label)
            {
                fontSize  = Mathf.RoundToInt(iconSize),
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
            };
            GUI.Label(new Rect(x, y, 100f, iconSize + 8f), label, style);
            GUI.color = Color.white; // always restore
        }
    }
}


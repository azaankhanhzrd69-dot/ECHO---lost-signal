using UnityEngine;

namespace Echo.Interact
{
    /// <summary>
    /// Tunable gameplay settings for the Interact lane (Morse radio, keypads, pickups, etc.).
    /// Create via Assets > Create > ECHO > Interact Tuning.
    /// </summary>
    [CreateAssetMenu(fileName = "InteractTuning", menuName = "ECHO/Interact Tuning")]
    public class InteractTuning : ScriptableObject
    {
        [Header("Morse Radio (I-05)")]
        [Tooltip("The expected plain text message to be transmitted by the player (e.g. SOS).")]
        public string expectedMessage = "SOS";

        [Tooltip("Maximum Morse characters (. or - or space) allowed in the input buffer.")]
        [Min(1)]
        public int maxSymbols = 32;

        [Tooltip("Seconds to display the error feedback before resetting the Morse input field.")]
        [Min(0.1f)]
        public float failFeedbackSeconds = 1.5f;

        [Tooltip("HUD prompt when radio is operational in PowerOn with secret note.")]
        public string promptUsable = "Use radio [E]";

        [Tooltip("HUD prompt when radio lacks power or frequency note.")]
        public string promptNoSignal = "Radio (Offline / No frequency)";

        [Header("Audio & Ambience (I-07)")]
        [Tooltip("Base distance in metres the monster travels to trigger one footstep sound.")]
        [Min(0.5f)]
        public float monsterBaseStrideDistance = 1.4f;

        [Tooltip("Minimum pitch for monster footsteps (when moving slowly).")]
        [Range(0.5f, 2.0f)]
        public float monsterMinStepPitch = 0.85f;

        [Tooltip("Maximum pitch for monster footsteps (when sprinting/chasing).")]
        [Range(0.5f, 2.0f)]
        public float monsterMaxStepPitch = 1.15f;

        [Tooltip("Volume multiplier for monster 3D footsteps.")]
        [Range(0f, 1f)]
        public float monsterStepVolume = 1.0f;

        [Tooltip("Default seconds to crossfade between ambience tracks.")]
        [Min(0.1f)]
        public float ambienceCrossfadeSeconds = 2.0f;

        [Tooltip("Default SFX ID to play for generic object interactions / pickups.")]
        public string defaultInteractSfxId = "sfx_keypad_beep";

        [Header("Audio Occlusion (I-08)")]
        [Tooltip("Seconds between occlusion linecasts (not evaluated every frame).")]
        [Min(0.05f)]
        public float occlusionCheckInterval = 0.15f;

        [Tooltip("Maximum distance in metres between source and listener for occlusion linecasts.")]
        [Min(1f)]
        public float occlusionMaxDistance = 25.0f;

        [Tooltip("Low-pass cutoff frequency in Hz when sound has direct line of sight (unoccluded).")]
        [Range(10000f, 22000f)]
        public float occlusionOpenCutoff = 22000f;

        [Tooltip("Low-pass cutoff frequency in Hz when sound is occluded behind a wall or closed door.")]
        [Range(200f, 5000f)]
        public float occlusionCutoff = 1200f;

        [Tooltip("Volume multiplier applied when sound is occluded behind a wall or closed door.")]
        [Range(0f, 1f)]
        public float occlusionVolume = 0.6f;

        [Tooltip("Speed of interpolation (lerp factor) between occluded and unoccluded audio states.")]
        [Min(1f)]
        public float occlusionSmoothing = 8.0f;

        [Header("Screens & Display (I-09)")]
        [Tooltip("Fade transition duration in seconds between screens.")]
        [Min(0.1f)]
        public float screenFadeDuration = 0.5f;

        [Tooltip("Duration in seconds the Good Ending text card is shown before progressing.")]
        [Min(1.0f)]
        public float endingGoodDuration = 8.0f;

        [Tooltip("Duration in seconds the Secret Ending text card is shown before the return button.")]
        [Min(1.0f)]
        public float endingSecretDuration = 8.0f;

        [Tooltip("Default Graphics Mode index (0: Performance, 1: Balanced, 2: Cinematic).")]
        [Range(0, 2)]
        public int defaultGraphicsMode = 1;

        [Header("Objective Hints (I-10)")]
        [Tooltip("Seconds to fade the objective text in.")]
        [Min(0.1f)]
        public float objectiveFadeInSeconds = 1f;

        [Tooltip("Seconds the objective text stays visible before fading out.")]
        [Min(1f)]
        public float objectiveVisibleSeconds = 5f;

        [Tooltip("Minimum seconds between reminders for the same beat.")]
        [Min(0f)]
        public float objectiveMinReminderInterval = 3f;

        [Tooltip("Seconds between polls of Progress.CoresFound for the counter.")]
        [Min(0.05f)]
        public float coreCounterPollInterval = 0.25f;
    }
}


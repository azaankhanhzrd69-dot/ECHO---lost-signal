using UnityEngine;

namespace Echo.Shared
{
    /// <summary>
    /// Central gameplay tuning configuration for player noise, monster hearing, movement, and hide minigames.
    /// Do NOT load via Resources.Load or static singleton; consumers reference this via [SerializeField].
    /// </summary>
    [CreateAssetMenu(fileName = "GameTuning", menuName = "ECHO/Game Tuning")]
    public class GameTuning : ScriptableObject
    {
        // ── Noise — Loudness ─────────────────────────────────────────────────────
        // Scale: 0..1. At loudness 1.0 with gain 1.0 the monster hears it from
        // the full hearingRadius away. At 0.0 it is inaudible from any distance.
        //
        //   Crouch  0.10 — nearly silent; monster must be within ~10 % of its
        //                  hearing radius (~1.8 m at 18 m radius) to react.
        //   Walk    0.40 — half-alert range; gives the monster a chance to
        //                  investigate but won't instantly trigger Chase.
        //   Run     1.00 — maximum; heard across the entire hearing radius and
        //                  fills the alert meter fast enough to trigger Chase.
        [Header("Noise - Loudness (0..1 scale)")]
        [Tooltip("Crouch-walk loudness. Low enough that only nearby passes trigger Investigate.")]
        [Min(0f)] public float crouchLoudness = 0.10f;

        [Tooltip("Normal-walk loudness. Triggers Investigate at medium range; Chase if monster is close.")]
        [Min(0f)] public float walkLoudness   = 0.40f;

        [Tooltip("Run/sprint loudness. Heard from full hearingRadius; triggers Chase quickly.")]
        [Min(0f)] public float runLoudness    = 1.00f;

        [Header("Noise - Footstep Spacing (metres)")]
        [Tooltip("Horizontal distance in metres between footstep noise pulses while crouching.")]
        [Min(0.1f)] public float crouchStepDistance = 1.5f;

        [Tooltip("Horizontal distance in metres between footstep noise pulses while walking.")]
        [Min(0.1f)] public float walkStepDistance   = 2.0f;

        [Tooltip("Horizontal distance in metres between footstep noise pulses while running.")]
        [Min(0.1f)] public float runStepDistance    = 2.5f;

        [Header("Noise - Continuous Run Pulse")]
        [Tooltip("While sprinting, emit an additional noise every N seconds regardless of distance travelled. " +
                 "This keeps the monster's target position fresh on long straight corridors. " +
                 "Set to 0 to disable. Suggested: 0.35 s (roughly matches sprint footstep cadence).")]
        [Min(0.05f)] public float runContinuousInterval = 0.35f;

        [Header("Noise - Footstep Intervals (seconds, legacy audio only — not used by echolocation)")]
        [Min(0f)] public float footstepIntervalCrouch = 0.9f;
        [Min(0f)] public float footstepIntervalWalk   = 0.55f;
        [Min(0f)] public float footstepIntervalRun    = 0.35f;

        [Header("Hearing & Alert")]
        [Tooltip("How far (metres) the monster can hear any noise. " +
                 "Outside this sphere, all noises are inaudible regardless of loudness.")]
        [Min(0f)] public float hearingRadius = 18f;

        [Tooltip("Maximum distance in metres the monster can hear the player while crouching. " +
                 "Beyond this radius, crouch footsteps are 100% inaudible.")]
        [Min(0.1f)] public float crouchHearingRadius = 2.0f;

        [Tooltip("Scales how much each noise raises the alert meter. " +
                 "1.0 = raw effective value (loudness × falloff). " +
                 "Lower this to make the monster less sensitive overall.")]
        [Min(0f)] public float alertGainScale = 1.0f;

        [Tooltip("Alert decays by this many units per second when no noise is heard. " +
                 "Higher = monster forgets faster. 0.15 means full alert (1.0) clears in ~7 s.")]
        [Min(0f)] public float alertDecayPerSecond = 0.15f;

        [Tooltip("Alert level (0..∞) that triggers Investigate. " +
                 "One crouch step from 2 m away adds ≈ 0.09; one walk step from 5 m ≈ 0.31. " +
                 "Keep this above a single crouch step so the monster doesn't react to every whisper.")]
        [Min(0f)] public float investigateThreshold = 0.40f;

        [Tooltip("Alert level that triggers Chase. " +
                 "One run step anywhere in hearingRadius adds loudness × falloff × gain. " +
                 "At radius/2 distance that's 0.5 per step; a few steps reaches this. " +
                 "Suggested: 1.2 so two nearby run steps trigger Chase, not one.")]
        [Min(0f)] public float chaseThreshold = 1.20f;

        [Tooltip("Max seconds the monster searches near the last heard position before giving up.")]
        [Min(0f)] public float searchTimeout = 12f;

        [Header("Monster Movement & Combat")]
        [Min(0f)] public float speedInvestigate = 2.2f;
        [Min(0f)] public float speedChase = 5f;
        [Min(0f)] public float speedSearch = 1.8f;
        [Min(0f)] public float killRange = 1.45f;

        [Header("Monster Search")]
        [Tooltip("Radius in metres around the last heard position within which random search waypoints are sampled.")]
        [Min(1f)] public float searchRadius = 8f;

        [Header("Monster Memory")]
        [Tooltip("Seconds a heard position is kept as the active chase destination before the monster gives up " +
                 "and falls through to Search if it has not heard anything new.")]
        [Min(0f)] public float noiseMemory = 6f;

        [Header("Lunge Sequence (C-07)")]
        [Tooltip("Seconds the monster idles at the eating spot before noticing John and dashing.")]
        [Min(0f)] public float lungeNoticePause = 1.5f;

        [Tooltip("NavMeshAgent speed during the Lunge dash to the door. Should be faster than speedChase.")]
        [Min(0f)] public float lungeDashSpeed = 8f;

        [Tooltip("Total seconds the monster bangs on the door before the sequence ends and HideIntro begins.")]
        [Min(0f)] public float lungeBangDuration = 6f;

        [Tooltip("Seconds between each OnBang UnityEvent invocation during the banging phase.")]
        [Min(0.05f)] public float lungeBangInterval = 0.8f;

        [Header("Hide Mechanics")]
        [Min(0)] public int safeHidesBeforeDeath = 3;

        [Tooltip("Seconds after an instant-kill hide (Table/Chair) before OnHideDeath fires.")]
        [Min(0f)] public float instantKillDelay = 1.2f;

        [Tooltip("Seconds to wait for the monster to reach the hide spot's monsterPoint before warping it there.")]
        [Min(0f)] public float monsterArriveTimeout = 5f;

        [Tooltip("Seconds between each OnBang UnityEvent during a hide.")]
        [Min(0.05f)] public float hideBangInterval = 0.8f;

        [Tooltip("Seconds the player must wait after exiting a hide before they can begin another.")]
        [Min(0f)] public float hideCooldown = 2f;

        [Tooltip("Fill per second that drives the minigame difficulty bar while the monster bangs.")]
        [Min(0f)] public float bangFillPerSecond = 0.35f;

        [Tooltip("Decay per second of the minigame difficulty bar when the monster pauses.")]
        [Min(0f)] public float bangDecayPerSecond = 0.5f;

        [Header("Death & Respawn (C-09)")]
        [Tooltip("Minimum seconds the death overlay remains before a respawn request is accepted.")]
        [Min(0.1f)] public float minDeathScreenSeconds = 1.0f;

        [Tooltip("Seconds for screen fade transition on respawn.")]
        [Min(0.1f)] public float respawnFadeIn = 0.5f;

        [Header("Airlock & Ending (C-10)")]
        [Tooltip("Safety timeout in seconds before transitioning from Airlock to Ending if World cutscene does not call AirlockFinished.")]
        [Min(1.0f)] public float airlockTimeoutSeconds = 8.0f;

        [Tooltip("Seconds the Good Ending text/scene runs before moving to Secret Ending or completion.")]
        [Min(1.0f)] public float goodEndingSeconds = 8.0f;

        [Tooltip("Seconds the Secret Ending text/scene runs before sequence completes.")]
        [Min(1.0f)] public float secretEndingSeconds = 8.0f;

        [Header("Chase Beat (C-11)")]
        [Tooltip("Player's sprint speed in metres/second from EchoPlayerMotor.")]
        [Min(1.0f)] public float playerSprintSpeed = 6.3f;

        [Tooltip("Ratio of player's sprint speed at which the monster chases (e.g. 0.90 = 90% of player sprint speed).")]
        [Range(0.5f, 1.2f)] public float chaseSpeedRatio = 0.90f;

        [Tooltip("Multiplier applied to monster hearing radius during Beat.Chase.")]
        [Min(1.0f)] public float chaseHearingMultiplier = 2.5f;

        [Tooltip("Seconds between synthetic noise pulses emitted at player position during the chase.")]
        [Min(0.1f)] public float beaconInterval = 0.8f;

        [Tooltip("Loudness (0..1) of synthetic noise pulses emitted during the chase.")]
        [Range(0.1f, 1.0f)] public float beaconLoudness = 1.0f;

        [Tooltip("Seconds to wait after BeginChase is invoked before transitioning beat to Chase.")]
        [Min(0f)] public float powerOnToChaseDelay = 2.0f;

        [Tooltip("Minimum safety margin in seconds player must have over monster in chase balance checks.")]
        [Min(0.5f)] public float minMarginSeconds = 2.0f;
    }
}


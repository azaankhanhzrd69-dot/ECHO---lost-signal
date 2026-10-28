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
        [Header("Noise - Loudness (0..1 scale, where 1.0 represents full hearing radius)")]
        [Min(0f)] public float crouchLoudness = NoiseLevel.Crouch;
        [Min(0f)] public float walkLoudness = NoiseLevel.Walk;
        [Min(0f)] public float runLoudness = NoiseLevel.Run;

        [Header("Noise - Footstep Spacing (metres)")]
        [Tooltip("Horizontal distance in metres between footstep noises while crouching.")]
        [Min(0.1f)] public float crouchStepDistance = 1.2f;

        [Tooltip("Horizontal distance in metres between footstep noises while walking.")]
        [Min(0.1f)] public float walkStepDistance = 1.8f;

        [Tooltip("Horizontal distance in metres between footstep noises while running.")]
        [Min(0.1f)] public float runStepDistance = 2.2f;

        [Header("Noise - Footstep Intervals (seconds)")]
        [Min(0f)] public float footstepIntervalCrouch = 0.9f;
        [Min(0f)] public float footstepIntervalWalk = 0.55f;
        [Min(0f)] public float footstepIntervalRun = 0.35f;

        [Header("Hearing & Alert")]
        [Min(0f)] public float hearingRadius = 18f;
        [Min(0f)] public float alertGainScale = 1f;
        [Min(0f)] public float alertDecayPerSecond = 0.15f;
        [Min(0f)] public float investigateThreshold = 0.25f;
        [Min(0f)] public float chaseThreshold = 0.7f;
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
    }
}


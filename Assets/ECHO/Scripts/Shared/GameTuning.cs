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
        [Header("Noise - Loudness (0..1 scale)")]
        [Min(0f)] public float crouchLoudness = NoiseLevel.Crouch;
        [Min(0f)] public float walkLoudness = NoiseLevel.Walk;
        [Min(0f)] public float runLoudness = NoiseLevel.Run;

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

        [Header("Hide Mechanics")]
        [Min(0)] public int safeHidesBeforeDeath = 3;
        [Min(0f)] public float bangFillPerSecond = 0.35f;
        [Min(0f)] public float bangDecayPerSecond = 0.5f;
    }
}


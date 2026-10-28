using System;
using UnityEngine;

namespace Echo.Shared
{
    /// <summary>
    /// Global decoupled event hub across all lanes.
    /// Provides cross-lane communication without direct type dependencies.
    /// </summary>
    public static class EchoEvents
    {
        /// <summary>
        /// Raised when the active story beat advances.
        /// Raised by: Core/World sequence controllers via <see cref="SetBeat"/>.
        /// Listened by: All lanes (World environmental triggers, Interact door locks/puzzles, Core AI mode switches, UI objectives).
        /// </summary>
        public static event Action<Beat> BeatChanged;

        /// <summary>
        /// Raised when a noise occurs in the environment.
        /// Parameters: world position, normalized loudness (0..1).
        /// Raised by: Player footsteps (Core), interacting props (Interact), impact physics (World) via <see cref="MakeNoise"/>.
        /// Listened by: Core MonsterAI hearing/alert accumulator and World emergency light flickers.
        /// </summary>
        public static event Action<Vector3, float> Noise;

        /// <summary>
        /// Raised when the player enters a hiding spot (locker, desk, chair).
        /// Parameter: world position of the hiding spot.
        /// Raised by: Hiding spot interactions (Interact/Core) via <see cref="StartHide"/>.
        /// Listened by: Core hide minigame controller, camera director, and monster AI.
        /// </summary>
        public static event Action<Vector3> HideStarted;

        /// <summary>
        /// Raised when the hiding sequence completes.
        /// Parameter: true if player survived, false if caught/failed.
        /// Raised by: Core hide minigame controller via <see cref="EndHide"/>.
        /// Listened by: Interact hiding spots (release player), World atmosphere, Core player controller.
        /// </summary>
        public static event Action<bool> HideFinished;

        /// <summary>
        /// Raised when the monster transitions between operational states.
        /// Parameter: the new <see cref="MonsterState"/>.
        /// Raised by: Core MonsterAI via <see cref="SetMonsterState"/>.
        /// Listened by: Audio systems (music/tension cues), World lighting controllers, UI.
        /// </summary>
        public static event Action<MonsterState> MonsterStateChanged;

        /// <summary>
        /// Raised when the monster catches the player, initiating death.
        /// Raised once per death sequence.
        /// Raised by: Core monster attack/kill trigger via <see cref="Caught"/>.
        /// Listened by: Core death/respawn manager (C-09), UI game over screen, audio death stinger.
        /// </summary>
        public static event Action PlayerCaught;

        /// <summary>
        /// Updates <see cref="Progress.Current"/> and raises <see cref="BeatChanged"/>.
        /// </summary>
        /// <param name="b">The target story beat.</param>
        public static void SetBeat(Beat b)
        {
            Progress.Current = b;
            BeatChanged?.Invoke(b);
        }

        /// <summary>
        /// Dispatches a noise event at a specific world position with a given loudness.
        /// </summary>
        /// <param name="p">World position where noise originated.</param>
        /// <param name="loud">Normalized loudness (0..1 scale).</param>
        public static void MakeNoise(Vector3 p, float loud)
        {
            Noise?.Invoke(p, loud);
        }

        /// <summary>
        /// Signals that the player has initiated hiding at a specific spot.
        /// </summary>
        /// <param name="spot">World position of the hiding spot.</param>
        public static void StartHide(Vector3 spot)
        {
            HideStarted?.Invoke(spot);
        }

        /// <summary>
        /// Signals that the player hiding sequence has finished.
        /// </summary>
        /// <param name="ok">True if the player survived; false if caught.</param>
        public static void EndHide(bool ok)
        {
            HideFinished?.Invoke(ok);
        }

        /// <summary>
        /// Updates and announces the active monster AI state.
        /// </summary>
        /// <param name="s">The new state of the monster.</param>
        public static void SetMonsterState(MonsterState s)
        {
            MonsterStateChanged?.Invoke(s);
        }

        /// <summary>
        /// Signals that the player was caught and killed by the monster. Raised once per death.
        /// </summary>
        public static void Caught()
        {
            PlayerCaught?.Invoke();
        }

        // ── Input Lock ────────────────────────────────────────────────────────────
        // True while any system (hide minigame, cutscene, keypad) owns player input.
        // Set via LockInput / UnlockInput; read by motor, interactor, and UI.

        /// <summary>
        /// True while player input (movement, camera, interaction) is locked.
        /// Written by: Core hide controller and future cutscene/keypad systems.
        /// Read by: EchoPlayerMotor, Interactor, and any system that must pause on input lock.
        /// </summary>
        public static bool InputLocked { get; private set; }

        /// <summary>Locks all player input. Call when a hide/cutscene/minigame begins.</summary>
        public static void LockInput() { InputLocked = true; }

        /// <summary>Unlocks all player input. Call when control returns to the player.</summary>
        public static void UnlockInput() { InputLocked = false; }

        // ──────────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Clears all subscribed delegates from every event in this hub.
        /// </summary>
        public static void ClearAll()
        {
            BeatChanged = null;
            Noise = null;
            HideStarted = null;
            HideFinished = null;
            MonsterStateChanged = null;
            PlayerCaught = null;
            InputLocked = false;
        }

        /// <summary>
        /// Resets all static event subscribers and progress state on Play start.
        /// Ensures safety when Domain Reload is disabled in Project Settings > Editor > Enter Play Mode Options.
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            ClearAll();
            Progress.Reset();
        }
    }
}


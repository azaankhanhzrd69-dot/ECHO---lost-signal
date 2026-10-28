using UnityEngine;

namespace Echo.Shared
{
    /// <summary>
    /// Shared runtime progress state across all lanes.
    /// Acts as the single source of truth for narrative progression, collected items, and monster tracking.
    /// </summary>
    public static class Progress
    {
        /// <summary>
        /// Current active story beat.
        /// Written by: Core/World sequence directors via <see cref="EchoEvents.SetBeat"/> or <see cref="Restore"/>.
        /// Read by: All lanes to determine objectives, triggers, and active mechanics.
        /// </summary>
        public static Beat Current;

        /// <summary>
        /// Total number of reactor power cores collected by the player.
        /// Written by: Interact lane core pickup interactions.
        /// Read by: Core reactor logic and UI inventory / progress display.
        /// </summary>
        public static int CoresFound;

        /// <summary>
        /// Number of safe locker hiding minigames survived after HideIntro. The 4th safe hide kills.
        /// Written by: Core hide system upon surviving locker minigame.
        /// Read by: Core hide system to enforce 4th safe hide death penalty.
        /// </summary>
        public static int SafeHides;

        /// <summary>
        /// Whether the player has collected the station map.
        /// Written by: Interact lane (vent pickup).
        /// Read by: UI and Interact lane components.
        /// </summary>
        public static bool HasMap;

        /// <summary>
        /// Whether the player has collected the secret Morse frequency note.
        /// Written by: Interact lane (astronaut corpse inspection).
        /// Read by: Interact lane radio room Morse puzzle.
        /// </summary>
        public static bool HasNote;

        /// <summary>
        /// Whether the player successfully transmitted the secret Morse message over the radio.
        /// Written by: Interact lane radio transmission sequence.
        /// Read by: Core and World ending controllers to trigger Secret Ending.
        /// </summary>
        public static bool MorseSent;

        /// <summary>
        /// Current world-space position of the monster.
        /// Written by: MonsterAI in Core each frame.
        /// Read by: Audio spatialization, occlusion calculations, and emergency lighting.
        /// </summary>
        public static Vector3 MonsterPosition;

        /// <summary>
        /// Resets all progress fields to their default initial values (Beat.Boarding, zeroes, false, Vector3.zero).
        /// Called by: Session bootstrap, Play mode start, and <see cref="EchoEvents.ResetStatics"/>.
        /// </summary>
        public static void Reset()
        {
            Current = Beat.Boarding;
            CoresFound = 0;
            SafeHides = 0;
            HasMap = false;
            HasNote = false;
            MorseSent = false;
            MonsterPosition = Vector3.zero;
        }

        /// <summary>
        /// Captures a snapshot of current progress data for checkpointing (C-09).
        /// Called by: Core checkpoint manager before advancing beats.
        /// </summary>
        public static ProgressSnapshot Snapshot()
        {
            return new ProgressSnapshot(Current, CoresFound, SafeHides, HasMap, HasNote, MorseSent);
        }

        /// <summary>
        /// Restores progress fields from a saved snapshot. Sets fields only and raises NO events.
        /// Called by: Core checkpoint / respawn manager upon player death.
        /// </summary>
        public static void Restore(ProgressSnapshot s)
        {
            Current = s.Beat;
            CoresFound = s.CoresFound;
            SafeHides = s.SafeHides;
            HasMap = s.HasMap;
            HasNote = s.HasNote;
            MorseSent = s.MorseSent;
        }
    }

    /// <summary>
    /// Lightweight snapshot representation of <see cref="Progress"/> for checkpointing and respawns.
    /// </summary>
    public struct ProgressSnapshot
    {
        public Beat Beat;
        public int CoresFound;
        public int SafeHides;
        public bool HasMap;
        public bool HasNote;
        public bool MorseSent;

        public ProgressSnapshot(Beat beat, int coresFound, int safeHides, bool hasMap, bool hasNote, bool morseSent)
        {
            Beat = beat;
            CoresFound = coresFound;
            SafeHides = safeHides;
            HasMap = hasMap;
            HasNote = hasNote;
            MorseSent = morseSent;
        }
    }
}


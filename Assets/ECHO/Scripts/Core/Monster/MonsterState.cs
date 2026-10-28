namespace Echo.Core
{
    /// <summary>
    /// Operational states for the monster AI.
    /// Dormant  – idle, listening for noise.
    /// Investigate – walking to last heard position at low speed.
    /// Chase    – running to last heard position at high speed; updated on every new noise.
    /// Search   – wandering near last heard position after losing the trail.
    /// Attack   – kill range reached; fires death events and halts.
    /// Scripted – reserved for C-07/C-08 sequence takeover; MonsterAI ignores noise while in this state.
    /// </summary>
    public enum MonsterState
    {
        Dormant,
        Investigate,
        Chase,
        Search,
        Attack,
        Scripted,
    }
}


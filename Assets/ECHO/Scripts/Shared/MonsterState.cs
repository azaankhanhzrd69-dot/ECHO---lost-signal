namespace Echo.Shared
{
    /// <summary>
    /// Operational state of the eyeless monster AI state machine.
    /// Raised by: MonsterAI in Core via <see cref="EchoEvents.SetMonsterState"/>.
    /// Listened by: Audio systems (music and tension cues), World (lighting/ambience changes), and UI.
    /// </summary>
    public enum MonsterState
    {
        Dormant,
        Investigate,
        Chase,
        Search,
        Attack,
        Scripted
    }
}


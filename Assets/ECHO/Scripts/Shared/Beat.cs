namespace Echo.Shared
{
    /// <summary>
    /// Ordered progression of story beats across the game.
    /// Raised by: Core/World sequence directors and debug jump menu via <see cref="EchoEvents.SetBeat"/>.
    /// Listened by: All lanes (World environmental triggers, Interact door locks/puzzles, Core AI mode switches, UI objectives).
    /// </summary>
    public enum Beat
    {
        Boarding,
        Vent,
        Keypad,
        MedBay,
        Lunge,
        HideIntro,
        Sneak,
        ShuttleWreck,
        ReactorEmpty,
        CoreHunt,
        PowerOn,
        Chase,
        Airlock,
        Ending
    }
}


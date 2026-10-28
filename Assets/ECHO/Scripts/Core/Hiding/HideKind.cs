namespace Echo.Core
{
    /// <summary>
    /// Identifies what kind of hiding spot a HideSpot represents.
    /// Locker = safe (up to safeHidesBeforeDeath times after the intro).
    /// Table and Chair = instant kill, always.
    /// </summary>
    public enum HideKind
    {
        Locker,
        Table,
        Chair,
    }
}


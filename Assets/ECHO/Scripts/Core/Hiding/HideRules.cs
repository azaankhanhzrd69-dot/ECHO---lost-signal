namespace Echo.Core
{
    /// <summary>
    /// Outcome returned by HideRules.Evaluate. Used by HideController to choose the hide path.
    /// </summary>
    public enum HideOutcome
    {
        /// <summary>Table or Chair: monster kills the player immediately after instantKillDelay.</summary>
        InstantKill,

        /// <summary>Locker: the minigame runs normally; the player can survive.</summary>
        Normal,

        /// <summary>Locker on the 4th (or beyond) safe-hide attempt: starts like Normal
        /// but forceFail=true is passed to the minigame, guaranteeing death.</summary>
        ForcedDeath,
    }

    /// <summary>
    /// Pure C# rule evaluator — no MonoBehaviour, no UnityEngine dependency.
    /// C-06 can test this class in EditMode without a scene.
    ///
    /// Rules:
    ///   Table or Chair                                          → InstantKill
    ///   Locker, isIntro                                        → Normal (intro never counts)
    ///   Locker, !isIntro, safeHidesSoFar &lt; safeHidesBeforeDeath → Normal
    ///   Locker, !isIntro, safeHidesSoFar >= safeHidesBeforeDeath → ForcedDeath
    /// </summary>
    public static class HideRules
    {
        /// <summary>
        /// Evaluates the outcome for a hide attempt.
        /// </summary>
        /// <param name="kind">The kind of hiding spot.</param>
        /// <param name="isIntro">True if this is the mandatory first hide in HideIntro beat.</param>
        /// <param name="safeHidesSoFar">Progress.SafeHides at the moment the hide begins.</param>
        /// <param name="safeHidesBeforeDeath">GameTuning.safeHidesBeforeDeath (default 3).</param>
        public static HideOutcome Evaluate(
            HideKind kind,
            bool     isIntro,
            int      safeHidesSoFar,
            int      safeHidesBeforeDeath)
        {
            // Table and Chair are always instant kills regardless of context.
            if (kind == HideKind.Table || kind == HideKind.Chair)
                return HideOutcome.InstantKill;

            // Locker in the intro does not count toward the safe-hide limit.
            if (isIntro)
                return HideOutcome.Normal;

            // Locker after the intro: check how many safe hides have been used.
            return safeHidesSoFar < safeHidesBeforeDeath
                ? HideOutcome.Normal
                : HideOutcome.ForcedDeath;
        }
    }
}


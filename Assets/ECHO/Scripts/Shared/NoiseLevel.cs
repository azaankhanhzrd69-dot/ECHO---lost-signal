namespace Echo.Shared
{
    /// <summary>
    /// Named loudness constants (0..1 scale) so any lane can call EchoEvents.MakeNoise
    /// without depending on GameTuning or Echo.Core.
    ///
    /// Scale contract:
    ///   1.0 = heard from the full monster hearingRadius with no falloff loss.
    ///   0.0 = inaudible at any distance.
    ///
    /// These are the code-side defaults. GameTuning overrides crouchLoudness /
    /// walkLoudness / runLoudness for the player specifically; all other systems
    /// (props, doors, impacts) use these constants directly.
    /// </summary>
    public static class NoiseLevel
    {
        /// <summary>Crouch-walk footstep. Nearly silent — monster reacts only if very close.</summary>
        public const float Crouch   = 0.10f;

        /// <summary>Normal walk footstep. Triggers Investigate at medium range.</summary>
        public const float Walk     = 0.40f;

        /// <summary>Sprint footstep. Heard across the full hearing radius; triggers Chase quickly.</summary>
        public const float Run      = 1.00f;

        /// <summary>Light interact noise (picking up objects, pressing buttons).</summary>
        public const float Interact = 0.20f;

        /// <summary>Door open / close. Audible from moderate range.</summary>
        public const float Door     = 0.55f;

        /// <summary>Object impact / drop. Loud but brief.</summary>
        public const float Impact   = 0.80f;

        /// <summary>Reactor power-on burst. Maximum range — heard anywhere on the map.</summary>
        public const float PowerOn  = 1.00f;
    }
}


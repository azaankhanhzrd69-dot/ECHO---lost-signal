namespace Echo.Shared
{
    /// <summary>
    /// Named loudness values (0..1 scale) so any lane can emit noise without referencing Core.
    /// Starting default constants; Core tunes them later via GameTuning.
    /// Raised by / Used by: Core player movement, Interact props, and World physics when calling <see cref="EchoEvents.MakeNoise"/>.
    /// Listened by: Core hearing system / MonsterAI alert accumulator.
    /// </summary>
    public static class NoiseLevel
    {
        public const float Crouch = 0.25f;
        public const float Walk = 0.5f;
        public const float Run = 1.0f;
        public const float Interact = 0.3f;
        public const float Door = 0.6f;
        public const float Impact = 0.9f;
        public const float PowerOn = 1.0f;
    }
}


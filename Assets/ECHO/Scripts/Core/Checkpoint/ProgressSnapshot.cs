using Echo.Shared;

namespace Echo.Core
{
    /// <summary>
    /// Pure C# progress snapshot representation for the checkpoint system.
    /// Captures every field of Progress and restores them without external dependencies.
    /// </summary>
    public sealed class ProgressSnapshot
    {
        public Beat Current { get; set; }
        public int CoresFound { get; set; }
        public int SafeHides { get; set; }
        public bool HasMap { get; set; }
        public bool HasNote { get; set; }
        public bool MorseSent { get; set; }

        public static ProgressSnapshot Capture()
        {
            return new ProgressSnapshot
            {
                Current = Progress.Current,
                CoresFound = Progress.CoresFound,
                SafeHides = Progress.SafeHides,
                HasMap = Progress.HasMap,
                HasNote = Progress.HasNote,
                MorseSent = Progress.MorseSent
            };
        }

        public void Restore()
        {
            Progress.Current = Current;
            Progress.CoresFound = CoresFound;
            Progress.SafeHides = SafeHides;
            Progress.HasMap = HasMap;
            Progress.HasNote = HasNote;
            Progress.MorseSent = MorseSent;
        }
    }
}


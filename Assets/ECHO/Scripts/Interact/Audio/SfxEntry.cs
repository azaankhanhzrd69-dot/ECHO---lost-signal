using Echo.Shared;

namespace Echo.Interact
{
    /// <summary>
    /// Parsed SFX entry from sfx.csv.
    /// Columns: id, file, trigger_beat, notes.
    /// </summary>
    public sealed class SfxEntry
    {
        public string Id { get; }
        public string File { get; }
        public Beat? TriggerBeat { get; }
        public string Notes { get; }

        public SfxEntry(string id, string file, Beat? triggerBeat, string notes)
        {
            Id = id;
            File = file;
            TriggerBeat = triggerBeat;
            Notes = notes;
        }
    }
}


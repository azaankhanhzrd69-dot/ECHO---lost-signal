using System;
using System.Collections.Generic;
using Echo.Shared;
using UnityEngine;

namespace Echo.Interact
{
    /// <summary>
    /// Pure C# SFX table parser and indexer for sfx.csv.
    /// No MonoBehaviour, no statics. Indexes by sound ID and TriggerBeat.
    /// </summary>
    public sealed class SfxTable
    {
        private readonly Dictionary<string, SfxEntry> entriesById = new Dictionary<string, SfxEntry>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<Beat, List<SfxEntry>> entriesByBeat = new Dictionary<Beat, List<SfxEntry>>();

        /// <summary>
        /// Parses sfx.csv from a TextAsset into SfxEntry objects.
        /// </summary>
        public void LoadFromTextAsset(TextAsset csvAsset)
        {
            entriesById.Clear();
            entriesByBeat.Clear();

            if (csvAsset == null || string.IsNullOrWhiteSpace(csvAsset.text))
            {
                Debug.LogWarning("[SfxTable] Provided sfx.csv TextAsset is null or empty.");
                return;
            }

            string[] rawLines = csvAsset.text.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
            if (rawLines.Length == 0)
                return;

            // Row 0 is header: id,file,trigger_beat,notes
            for (int i = 1; i < rawLines.Length; i++)
            {
                string row = rawLines[i].Trim();
                if (string.IsNullOrEmpty(row))
                    continue;

                List<string> cols = ParseCsvRow(row);
                if (cols.Count < 4)
                {
                    Debug.LogWarning($"[SfxTable] Row {i + 1} has insufficient columns ({cols.Count}/4). Skipped: \"{row}\"");
                    continue;
                }

                string id = cols[0].Trim();
                string file = cols[1].Trim();
                string beatStr = cols[2].Trim();
                string notes = cols[3].Trim();

                if (string.IsNullOrEmpty(id))
                    continue;

                Beat? beat = null;
                if (!string.Equals(beatStr, "NONE", StringComparison.OrdinalIgnoreCase))
                {
                    if (Enum.TryParse(beatStr, true, out Beat parsedBeat))
                    {
                        beat = parsedBeat;
                    }
                }

                var entry = new SfxEntry(id, file, beat, notes);
                entriesById[id] = entry;

                if (beat.HasValue)
                {
                    if (!entriesByBeat.TryGetValue(beat.Value, out List<SfxEntry> list))
                    {
                        list = new List<SfxEntry>();
                        entriesByBeat[beat.Value] = list;
                    }
                    list.Add(entry);
                }
            }
        }

        /// <summary>
        /// Looks up an SFX entry by its unique id (e.g. "sfx_keypad_beep").
        /// </summary>
        public SfxEntry GetById(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            entriesById.TryGetValue(id, out SfxEntry entry);
            return entry;
        }

        /// <summary>
        /// Returns all SFX entries associated with a given story beat.
        /// </summary>
        public IReadOnlyList<SfxEntry> GetEntriesForBeat(Beat beat)
        {
            if (entriesByBeat.TryGetValue(beat, out List<SfxEntry> list))
                return list;

            return Array.Empty<SfxEntry>();
        }

        private static List<string> ParseCsvRow(string row)
        {
            var columns = new List<string>();
            var current = new System.Text.StringBuilder();
            bool inQuotes = false;

            for (int i = 0; i < row.Length; i++)
            {
                char c = row[i];
                if (c == '"')
                {
                    if (inQuotes && i + 1 < row.Length && row[i + 1] == '"')
                    {
                        current.Append('"');
                        i++;
                    }
                    else
                    {
                        inQuotes = !inQuotes;
                    }
                }
                else if (c == ',' && !inQuotes)
                {
                    columns.Add(current.ToString());
                    current.Clear();
                }
                else
                {
                    current.Append(c);
                }
            }

            columns.Add(current.ToString());
            return columns;
        }
    }
}


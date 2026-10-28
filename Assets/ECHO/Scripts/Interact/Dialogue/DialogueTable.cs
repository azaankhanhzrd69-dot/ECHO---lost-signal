using System;
using System.Collections.Generic;
using Echo.Shared;
using UnityEngine;

namespace Echo.Interact
{
    /// <summary>
    /// Parsed dialogue entry from voicelines.csv.
    /// </summary>
    public sealed class DialogueLine
    {
        public string Id { get; }
        public string Speaker { get; }
        public string Text { get; }
        public string File { get; }
        public Beat? TriggerBeat { get; }
        public float Delay { get; }

        public DialogueLine(string id, string speaker, string text, string file, Beat? triggerBeat, float delay)
        {
            Id = id;
            Speaker = speaker;
            Text = text;
            File = file;
            TriggerBeat = triggerBeat;
            Delay = delay;
        }
    }

    /// <summary>
    /// Pure C# dialogue table parser and query engine.
    /// No MonoBehaviour, no Find*, no statics. Parses voicelines.csv with custom quoted-comma support.
    /// </summary>
    public sealed class DialogueTable
    {
        private readonly Dictionary<string, DialogueLine> linesById = new Dictionary<string, DialogueLine>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<Beat, List<DialogueLine>> linesByBeat = new Dictionary<Beat, List<DialogueLine>>();

        /// <summary>
        /// Parses a CSV TextAsset into dialogue lines and indexes them by Id and Beat.
        /// </summary>
        public void LoadFromTextAsset(TextAsset csvAsset)
        {
            linesById.Clear();
            linesByBeat.Clear();

            if (csvAsset == null || string.IsNullOrWhiteSpace(csvAsset.text))
            {
                Debug.LogWarning("[DialogueTable] Provided TextAsset is null or empty.");
                return;
            }

            string[] rawLines = csvAsset.text.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
            if (rawLines.Length == 0)
                return;

            // Row 0 is the CSV header (id,speaker,text,file,trigger_beat,delay)
            for (int i = 1; i < rawLines.Length; i++)
            {
                string row = rawLines[i].Trim();
                if (string.IsNullOrEmpty(row))
                    continue;

                List<string> cols = ParseCsvRow(row);
                if (cols.Count < 6)
                {
                    Debug.LogWarning($"[DialogueTable] Row {i + 1} has insufficient columns ({cols.Count}/6). Row skipped: \"{row}\"");
                    continue;
                }

                string id = cols[0].Trim();
                string speaker = cols[1].Trim();
                string text = cols[2];
                string file = cols[3].Trim();
                string beatStr = cols[4].Trim();
                string delayStr = cols[5].Trim();

                if (string.IsNullOrEmpty(id))
                {
                    Debug.LogWarning($"[DialogueTable] Row {i + 1} has empty id. Row skipped.");
                    continue;
                }

                Beat? beat = null;
                if (!string.Equals(beatStr, "NONE", StringComparison.OrdinalIgnoreCase))
                {
                    if (Enum.TryParse(beatStr, true, out Beat parsedBeat))
                    {
                        beat = parsedBeat;
                    }
                    else
                    {
                        Debug.LogWarning($"[DialogueTable] Row {i + 1} has unknown trigger_beat '{beatStr}'. Interpreting as NONE.");
                    }
                }

                if (!float.TryParse(delayStr, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float delay))
                {
                    delay = 0f;
                }

                DialogueLine line = new DialogueLine(id, speaker, text, file, beat, delay);
                linesById[id] = line;

                if (beat.HasValue)
                {
                    if (!linesByBeat.TryGetValue(beat.Value, out List<DialogueLine> beatList))
                    {
                        beatList = new List<DialogueLine>();
                        linesByBeat[beat.Value] = beatList;
                    }
                    beatList.Add(line);
                }
            }

            // Sort each beat's dialogue list in ascending order of delay
            foreach (var kvp in linesByBeat)
            {
                kvp.Value.Sort((a, b) => a.Delay.CompareTo(b.Delay));
            }
        }

        /// <summary>
        /// Looks up a dialogue line by its unique id. Returns null if not found.
        /// </summary>
        public DialogueLine GetById(string id)
        {
            if (string.IsNullOrEmpty(id))
                return null;

            linesById.TryGetValue(id, out DialogueLine line);
            return line;
        }

        /// <summary>
        /// Returns all dialogue lines for a given story beat, sorted by delay.
        /// Returns an empty list if none exist.
        /// </summary>
        public IReadOnlyList<DialogueLine> GetLinesForBeat(Beat beat)
        {
            if (linesByBeat.TryGetValue(beat, out List<DialogueLine> list))
                return list;

            return Array.Empty<DialogueLine>();
        }

        /// <summary>
        /// Parses a single line of CSV taking quotes and quoted commas into account.
        /// </summary>
        private static List<string> ParseCsvRow(string row)
        {
            List<string> columns = new List<string>();
            var current = new System.Text.StringBuilder();
            bool inQuotes = false;

            for (int i = 0; i < row.Length; i++)
            {
                char c = row[i];

                if (c == '"')
                {
                    // Check for escaped quote ("")
                    if (inQuotes && i + 1 < row.Length && row[i + 1] == '"')
                    {
                        current.Append('"');
                        i++; // Skip the next quote
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


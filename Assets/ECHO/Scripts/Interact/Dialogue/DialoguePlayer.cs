using System;
using System.Collections;
using System.Collections.Generic;
using Echo.Shared;
using UnityEngine;

namespace Echo.Interact
{
    /// <summary>
    /// Audio clip mapping entry for Inspector assignment.
    /// </summary>
    [Serializable]
    public struct NamedAudioClip
    {
        [Tooltip("Identifier matching the 'file' or 'id' column in voicelines.csv.")]
        public string name;

        [Tooltip("Audio clip asset to play.")]
        public AudioClip clip;
    }

    /// <summary>
    /// Core dialogue and voiceline sequence player.
    /// Plays dialogue cues on beat changes in delay order, handles subtitles and fallback display durations.
    /// </summary>
    public sealed class DialoguePlayer : MonoBehaviour
    {
        [Header("Data Assets")]
        [SerializeField]
        [Tooltip("voicelines.csv TextAsset.")]
        private TextAsset voicelinesCsv;

        [SerializeField]
        [Tooltip("sfx.csv TextAsset (optional reference for audio indexing).")]
        private TextAsset sfxCsv;

        [Header("Outputs")]
        [SerializeField]
        [Tooltip("SubtitleDisplay UI component.")]
        private SubtitleDisplay subtitleDisplay;

        [SerializeField]
        [Tooltip("AudioSource used to play spoken lines.")]
        private AudioSource audioSource;

        [Header("Audio Clips")]
        [SerializeField]
        [Tooltip("List of audio clips assigned to their line ID or filename.")]
        private List<NamedAudioClip> audioClips = new List<NamedAudioClip>();

        [Header("Subtitle Settings")]
        [SerializeField]
        [Tooltip("Global toggle for subtitles (controllable via pause menu).")]
        private bool subtitlesEnabled = true;

        [SerializeField]
        [Tooltip("Reading speed in characters per second used to compute display time when audio clip is missing.")]
        private float charactersPerSecond = 18f;

        [SerializeField]
        [Tooltip("Minimum time in seconds a subtitle line remains visible.")]
        private float minSubtitleTime = 2.0f;

        private readonly DialogueTable table = new DialogueTable();
        private readonly Dictionary<string, AudioClip> clipMap = new Dictionary<string, AudioClip>(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> playedLinesInCurrentBeat = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        private Coroutine beatQueueRoutine;
        private Beat? activeBeat;

        public bool SubtitlesEnabled
        {
            get => subtitlesEnabled;
            set => subtitlesEnabled = value;
        }

        // ── Unity Callbacks ────────────────────────────────────────────────────

        /// <summary>
        /// Awake: Parse CSV assets and build clip lookup dictionary.
        /// </summary>
        private void Awake()
        {
            if (audioSource == null)
                audioSource = GetComponent<AudioSource>();

            InitializeClipMap();
            LoadTable();
        }

        /// <summary>
        /// OnEnable: Subscribe to global beat change events.
        /// </summary>
        private void OnEnable()
        {
            EchoEvents.BeatChanged += OnBeatChanged;
        }

        /// <summary>
        /// OnDisable: Unsubscribe from events and stop any active dialogue queue.
        /// </summary>
        private void OnDisable()
        {
            EchoEvents.BeatChanged -= OnBeatChanged;
            StopActiveQueue();
        }

        // ── Public API ─────────────────────────────────────────────────────────

        /// <summary>
        /// Plays a dialogue line by its unique ID.
        /// Can be called directly by cutscenes, triggers, or directors.
        /// </summary>
        public void PlayById(string id)
        {
            DialogueLine line = table.GetById(id);
            if (line == null)
            {
                Debug.LogWarning($"[DialoguePlayer] Line with id '{id}' not found in voicelines.csv.");
                return;
            }

            PlaySingleLine(line);
        }

        // ── Event Handlers ─────────────────────────────────────────────────────

        private void OnBeatChanged(Beat newBeat)
        {
            activeBeat = newBeat;
            playedLinesInCurrentBeat.Clear();

            StopActiveQueue();

            IReadOnlyList<DialogueLine> lines = table.GetLinesForBeat(newBeat);
            if (lines.Count > 0)
            {
                beatQueueRoutine = StartCoroutine(PlayBeatSequenceRoutine(lines));
            }
        }

        // ── Coroutines ─────────────────────────────────────────────────────────

        private IEnumerator PlayBeatSequenceRoutine(IReadOnlyList<DialogueLine> lines)
        {
            float beatTimer = 0f;

            for (int i = 0; i < lines.Count; i++)
            {
                DialogueLine line = lines[i];

                // Wait until the line's scheduled delay from beat start
                while (beatTimer < line.Delay)
                {
                    beatTimer += Time.deltaTime;
                    yield return null;
                }

                // Guard: each line plays at most once per beat
                if (playedLinesInCurrentBeat.Add(line.Id))
                {
                    PlaySingleLine(line);
                }
            }

            beatQueueRoutine = null;
        }

        private void PlaySingleLine(DialogueLine line)
        {
            AudioClip clip = FindClipForLine(line);
            float duration;

            if (clip != null)
            {
                if (audioSource != null)
                {
                    audioSource.PlayOneShot(clip);
                }
                duration = clip.length;
            }
            else
            {
                // No clip yet: compute subtitle display time from text length
                duration = ComputeDisplayDuration(line.Text);
            }

            if (subtitlesEnabled && subtitleDisplay != null && !string.IsNullOrEmpty(line.Text))
            {
                subtitleDisplay.Show(line.Speaker, line.Text, duration);
            }
        }

        // ── Helpers ────────────────────────────────────────────────────────────

        private void InitializeClipMap()
        {
            clipMap.Clear();
            for (int i = 0; i < audioClips.Count; i++)
            {
                NamedAudioClip item = audioClips[i];
                if (!string.IsNullOrEmpty(item.name) && item.clip != null)
                {
                    clipMap[item.name] = item.clip;
                }
            }
        }

        private void LoadTable()
        {
            if (voicelinesCsv != null)
            {
                table.LoadFromTextAsset(voicelinesCsv);
            }
            else
            {
                Debug.LogWarning("[DialoguePlayer] voicelinesCsv TextAsset is not assigned.", this);
            }
        }

        private AudioClip FindClipForLine(DialogueLine line)
        {
            if (!string.IsNullOrEmpty(line.File) && clipMap.TryGetValue(line.File, out AudioClip clipByFile))
                return clipByFile;

            if (!string.IsNullOrEmpty(line.Id) && clipMap.TryGetValue(line.Id, out AudioClip clipById))
                return clipById;

            return null;
        }

        private float ComputeDisplayDuration(string text)
        {
            if (string.IsNullOrEmpty(text))
                return minSubtitleTime;

            float calculated = text.Length / Mathf.Max(1f, charactersPerSecond);
            return Mathf.Max(minSubtitleTime, calculated);
        }

        private void StopActiveQueue()
        {
            if (beatQueueRoutine != null)
            {
                StopCoroutine(beatQueueRoutine);
                beatQueueRoutine = null;
            }
        }

        // ── Static Reset ───────────────────────────────────────────────────────

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            // Reset any domain-reload persistent state if added in the future
        }
    }
}


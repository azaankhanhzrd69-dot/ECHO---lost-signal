using System;
using System.Collections;
using System.Collections.Generic;
using Echo.Shared;
using UnityEngine;
using UnityEngine.Audio;

namespace Echo.Interact
{
    /// <summary>
    /// Central audio manager for the ECHO station:
    /// - Manages a fixed pool of 2D/3D AudioSources for one-shot SFX.
    /// - Dual-source crossfading for atmospheric ambience and beat-driven tension layers.
    /// - Optional AudioMixer integration with PlayerPrefs persistence for master, sfx, music, and voice.
    /// - Fallback volume control when no AudioMixer is assigned.
    /// </summary>
    public sealed class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        [Header("Data")]
        [SerializeField] private TextAsset sfxCsv;
        [SerializeField] private InteractTuning tuning;

        [Header("Audio Clips")]
        [Tooltip("Inspector clips mapped by sound ID (e.g. sfx_keypad_beep, sfx_ambience_corridor).")]
        [SerializeField] private List<NamedAudioClip> clips = new List<NamedAudioClip>();

        [Header("Audio Mixer (Optional)")]
        [SerializeField] private AudioMixer audioMixer;
        [SerializeField] private AudioMixerGroup masterGroup;
        [SerializeField] private AudioMixerGroup ambienceGroup;
        [SerializeField] private AudioMixerGroup sfxGroup;
        [SerializeField] private AudioMixerGroup musicGroup;
        [SerializeField] private AudioMixerGroup voiceGroup;

        [Header("SFX Pool Settings")]
        [SerializeField] private int sfxPoolSize = 10;

        // ── Internal State ────────────────────────────────────────────────────
        private readonly SfxTable _table = new SfxTable();
        private readonly Dictionary<string, AudioClip> _clipLookup = new Dictionary<string, AudioClip>(StringComparer.OrdinalIgnoreCase);

        private AudioSource[] _sfxPool;
        private int _poolIndex;

        private AudioSource _ambienceSourceA;
        private AudioSource _ambienceSourceB;
        private bool _isSourceAActive;
        private Coroutine _ambienceCrossfadeRoutine;

        private const string PrefsMasterVol = "ECHO_Audio_Master";
        private const string PrefsSfxVol = "ECHO_Audio_Sfx";
        private const string PrefsMusicVol = "ECHO_Audio_Music";

        // ── Unity Callbacks ───────────────────────────────────────────────────

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;

            BuildClipLookup();
            if (sfxCsv != null)
            {
                _table.LoadFromTextAsset(sfxCsv);
            }

            InitializeSources();
            LoadVolumeSettings();
        }

        private void OnEnable()
        {
            EchoEvents.BeatChanged += OnBeatChanged;
        }

        private void OnDisable()
        {
            EchoEvents.BeatChanged -= OnBeatChanged;
            if (Instance == this)
            {
                Instance = null;
            }
        }

        // ── Initialization ────────────────────────────────────────────────────

        private void BuildClipLookup()
        {
            _clipLookup.Clear();
            for (int i = 0; i < clips.Count; i++)
            {
                NamedAudioClip entry = clips[i];
                if (!string.IsNullOrEmpty(entry.name) && entry.clip != null)
                {
                    _clipLookup[entry.name] = entry.clip;
                }
            }
        }

        private void InitializeSources()
        {
            // Ambience Source A
            _ambienceSourceA = gameObject.AddComponent<AudioSource>();
            _ambienceSourceA.loop = true;
            _ambienceSourceA.playOnAwake = false;
            _ambienceSourceA.spatialBlend = 0f;
            if (ambienceGroup != null) _ambienceSourceA.outputAudioMixerGroup = ambienceGroup;

            // Ambience Source B
            _ambienceSourceB = gameObject.AddComponent<AudioSource>();
            _ambienceSourceB.loop = true;
            _ambienceSourceB.playOnAwake = false;
            _ambienceSourceB.spatialBlend = 0f;
            if (ambienceGroup != null) _ambienceSourceB.outputAudioMixerGroup = ambienceGroup;

            // One-shot SFX pool
            _sfxPool = new AudioSource[sfxPoolSize];
            for (int i = 0; i < sfxPoolSize; i++)
            {
                var src = gameObject.AddComponent<AudioSource>();
                src.playOnAwake = false;
                src.spatialBlend = 0f;
                if (sfxGroup != null) src.outputAudioMixerGroup = sfxGroup;
                _sfxPool[i] = src;
            }
        }

        // ── SFX Playback API ──────────────────────────────────────────────────

        /// <summary>
        /// Plays an SFX by its ID (e.g. "sfx_keypad_beep") as a 2D sound.
        /// </summary>
        public void PlaySfx(string sfxId, float volume = 1f, float pitch = 1f)
        {
            PlaySfxAt(sfxId, null, volume, pitch);
        }

        /// <summary>
        /// Plays an SFX by its ID at a specified 3D world position.
        /// </summary>
        public void PlaySfxAt(string sfxId, Vector3? position, float volume = 1f, float pitch = 1f)
        {
            if (string.IsNullOrEmpty(sfxId)) return;

            if (!_clipLookup.TryGetValue(sfxId, out AudioClip clip) || clip == null)
            {
                // Silent fallback if clip is missing or still being configured
                return;
            }

            PlayClip(clip, position, volume, pitch);
        }

        /// <summary>
        /// Directly plays an AudioClip using the internal pool.
        /// </summary>
        public void PlayClip(AudioClip clip, Vector3? position = null, float volume = 1f, float pitch = 1f)
        {
            if (clip == null || _sfxPool == null || _sfxPool.Length == 0) return;

            AudioSource src = _sfxPool[_poolIndex];
            _poolIndex = (_poolIndex + 1) % _sfxPool.Length;

            if (position.HasValue)
            {
                src.transform.position = position.Value;
                src.spatialBlend = 1f;
                src.minDistance = 1f;
                src.maxDistance = 20f;
                src.rolloffMode = AudioRolloffMode.Logarithmic;
            }
            else
            {
                src.spatialBlend = 0f;
            }

            src.pitch = pitch;
            src.volume = Mathf.Clamp01(volume);
            src.PlayOneShot(clip);
        }

        /// <summary>
        /// Retrieves an AudioClip mapped to an ID. Returns null if not found.
        /// </summary>
        public AudioClip GetClip(string sfxId)
        {
            if (string.IsNullOrEmpty(sfxId)) return null;
            _clipLookup.TryGetValue(sfxId, out AudioClip clip);
            return clip;
        }

        // ── Ambience & Beat Layering ──────────────────────────────────────────

        /// <summary>
        /// Crossfades current ambience to a new loop ID over the given duration.
        /// </summary>
        public void CrossfadeAmbience(string ambienceId, float duration = 2.0f)
        {
            if (!_clipLookup.TryGetValue(ambienceId, out AudioClip newClip) || newClip == null)
                return;

            AudioSource currentSource = _isSourceAActive ? _ambienceSourceA : _ambienceSourceB;
            AudioSource targetSource = _isSourceAActive ? _ambienceSourceB : _ambienceSourceA;

            if (currentSource.clip == newClip && currentSource.isPlaying)
                return;

            if (_ambienceCrossfadeRoutine != null)
                StopCoroutine(_ambienceCrossfadeRoutine);

            _ambienceCrossfadeRoutine = StartCoroutine(CrossfadeRoutine(currentSource, targetSource, newClip, duration));
            _isSourceAActive = !_isSourceAActive;
        }

        private IEnumerator CrossfadeRoutine(AudioSource fromSource, AudioSource toSource, AudioClip toClip, float duration)
        {
            toSource.clip = toClip;
            toSource.volume = 0f;
            toSource.Play();

            float elapsed = 0f;
            float startFromVol = fromSource.isPlaying ? fromSource.volume : 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);

                fromSource.volume = Mathf.Lerp(startFromVol, 0f, t);
                toSource.volume = Mathf.Lerp(0f, 1f, t);

                yield return null;
            }

            fromSource.Stop();
            fromSource.volume = 0f;
            toSource.volume = 1f;
            _ambienceCrossfadeRoutine = null;
        }

        private void OnBeatChanged(Beat newBeat)
        {
            float crossfade = tuning != null ? tuning.ambienceCrossfadeSeconds : 2.0f;

            switch (newBeat)
            {
                case Beat.Boarding:
                case Beat.Vent:
                case Beat.Keypad:
                    CrossfadeAmbience("sfx_ambience_corridor", crossfade);
                    break;

                case Beat.MedBay:
                case Beat.Lunge:
                case Beat.HideIntro:
                    CrossfadeAmbience("sfx_ambience_medbay", crossfade);
                    break;

                case Beat.Sneak:
                case Beat.ShuttleWreck:
                case Beat.ReactorEmpty:
                case Beat.CoreHunt:
                    CrossfadeAmbience("sfx_ambience_reactor", crossfade);
                    break;

                case Beat.PowerOn:
                case Beat.Chase:
                    // Heightened tension / alarm
                    CrossfadeAmbience("sfx_ambience_reactor", 0.5f);
                    break;

                case Beat.Airlock:
                case Beat.Ending:
                    CrossfadeAmbience("sfx_ambience_corridor", crossfade);
                    break;
            }
        }

        // ── Volume Settings (Mixer & PlayerPrefs) ─────────────────────────────

        public void SetMasterVolume(float linear)
        {
            linear = Mathf.Clamp(linear, 0.0001f, 1f);
            PlayerPrefs.SetFloat(PrefsMasterVol, linear);

            if (audioMixer != null)
            {
                audioMixer.SetFloat("MasterVolume", LinearToDecibels(linear));
            }
            else
            {
                AudioListener.volume = linear;
            }
        }

        public void SetSfxVolume(float linear)
        {
            linear = Mathf.Clamp(linear, 0.0001f, 1f);
            PlayerPrefs.SetFloat(PrefsSfxVol, linear);

            if (audioMixer != null)
            {
                audioMixer.SetFloat("SfxVolume", LinearToDecibels(linear));
            }
        }

        public void SetMusicVolume(float linear)
        {
            linear = Mathf.Clamp(linear, 0.0001f, 1f);
            PlayerPrefs.SetFloat(PrefsMusicVol, linear);

            if (audioMixer != null)
            {
                audioMixer.SetFloat("MusicVolume", LinearToDecibels(linear));
                audioMixer.SetFloat("AmbienceVolume", LinearToDecibels(linear));
            }
        }

        private void LoadVolumeSettings()
        {
            SetMasterVolume(PlayerPrefs.GetFloat(PrefsMasterVol, 1f));
            SetSfxVolume(PlayerPrefs.GetFloat(PrefsSfxVol, 1f));
            SetMusicVolume(PlayerPrefs.GetFloat(PrefsMusicVol, 0.8f));
        }

        private static float LinearToDecibels(float linear)
        {
            return Mathf.Log10(Mathf.Max(linear, 0.0001f)) * 20f;
        }

        // ── Static Domain Reload ──────────────────────────────────────────────

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            Instance = null;
        }
    }
}


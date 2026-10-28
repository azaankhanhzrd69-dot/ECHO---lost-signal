using UnityEngine;

namespace Echo.Interact
{
    /// <summary>
    /// Component wired to UnityEvents across World, Core, and Interact lanes.
    /// Provides instance methods accepting simple string IDs from sfx.csv.
    /// </summary>
    public sealed class SfxPlayer : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private InteractTuning tuning;

        [Header("Optional Local Source")]
        [Tooltip("If assigned, audio can be played locally; otherwise AudioManager pool is used.")]
        [SerializeField] private AudioSource localAudioSource;

        [Header("Settings")]
        [SerializeField] [Range(0f, 1f)] private float volume = 1.0f;
        [SerializeField] [Range(0.5f, 2f)] private float pitch = 1.0f;

        private float _lastPlayTime;
        private string _lastPlayId;

        /// <summary>
        /// Plays an SFX as a 2D sound through AudioManager.
        /// </summary>
        public void Play(string sfxId)
        {
            if (!CanPlay(sfxId)) return;

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySfx(sfxId, volume, pitch);
            }
            else if (localAudioSource != null)
            {
                localAudioSource.Play();
            }
        }

        /// <summary>
        /// Plays an SFX as a 3D sound at this GameObject's world position.
        /// </summary>
        public void PlayAt(string sfxId)
        {
            if (!CanPlay(sfxId)) return;

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySfxAt(sfxId, transform.position, volume, pitch);
            }
            else if (localAudioSource != null)
            {
                localAudioSource.transform.position = transform.position;
                localAudioSource.Play();
            }
        }

        /// <summary>
        /// Plays the default generic interaction sound configured in InteractTuning.
        /// Convenient for pickup events, button presses, etc.
        /// </summary>
        public void PlayDefaultInteract()
        {
            string sfxId = tuning != null ? tuning.defaultInteractSfxId : "sfx_keypad_beep";
            Play(sfxId);
        }

        private bool CanPlay(string sfxId)
        {
            if (string.IsNullOrEmpty(sfxId)) return false;

            // Debounce guard: Prevent the identical sound from firing multiple times in the exact same frame
            if (string.Equals(_lastPlayId, sfxId) && Mathf.Approximately(Time.time, _lastPlayTime))
                return false;

            _lastPlayId = sfxId;
            _lastPlayTime = Time.time;
            return true;
        }
    }
}


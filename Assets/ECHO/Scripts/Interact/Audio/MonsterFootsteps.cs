using UnityEngine;

namespace Echo.Interact
{
    /// <summary>
    /// Sound-driven 3D footstep emitter for the creature.
    /// Pure Interact lane component: DOES NOT reference Core or MonsterAI.
    /// Operates purely on Transform horizontal displacement and speed.
    /// Stops completely when stationary or disabled.
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public sealed class MonsterFootsteps : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private InteractTuning tuning;

        [Header("Audio Clips")]
        [Tooltip("Monster footstep audio clips. A random clip is picked per stride.")]
        [SerializeField] private AudioClip[] stepClips;

        [Header("3D Audio Falloff")]
        [SerializeField] private float minDistance = 1.0f;
        [SerializeField] private float maxDistance = 25.0f;

        private AudioSource _audioSource;
        private Vector3 _lastPosition;
        private float _accumulatedDistance;

        // ── Unity Callbacks ───────────────────────────────────────────────────

        private void Awake()
        {
            _audioSource = GetComponent<AudioSource>();
            _audioSource.spatialBlend = 1.0f; // Full 3D
            _audioSource.minDistance = minDistance;
            _audioSource.maxDistance = maxDistance;
            _audioSource.rolloffMode = AudioRolloffMode.Logarithmic;
            _audioSource.playOnAwake = false;
            _audioSource.loop = false;
        }

        private void OnEnable()
        {
            _lastPosition = transform.position;
            _accumulatedDistance = 0f;
        }

        private void OnDisable()
        {
            _accumulatedDistance = 0f;
            if (_audioSource != null && _audioSource.isPlaying)
            {
                _audioSource.Stop();
            }
        }

        private void Update()
        {
            Vector3 currentPos = transform.position;

            // Measure horizontal ground displacement
            Vector3 delta = new Vector3(currentPos.x - _lastPosition.x, 0f, currentPos.z - _lastPosition.z);
            float distanceThisFrame = delta.magnitude;
            _lastPosition = currentPos;

            // Stationary guard: if speed is negligible, do not step
            if (distanceThisFrame < 0.0001f)
                return;

            float dt = Mathf.Max(Time.deltaTime, 0.0001f);
            float speed = distanceThisFrame / dt;

            // Minimum movement threshold to consider moving (e.g. 0.05 m/s)
            if (speed < 0.05f)
                return;

            _accumulatedDistance += distanceThisFrame;

            float baseStride = tuning != null ? tuning.monsterBaseStrideDistance : 1.4f;

            // When sprinting/dashing, stride distance compresses slightly for rapid footfall
            float effectiveStride = speed > 4.0f ? baseStride * 0.85f : baseStride;

            if (_accumulatedDistance >= effectiveStride)
            {
                _accumulatedDistance = 0f;
                PlayStep(speed);
            }
        }

        // ── Private Helpers ───────────────────────────────────────────────────

        private void PlayStep(float speed)
        {
            if (stepClips == null || stepClips.Length == 0)
            {
                // Fallback to AudioManager if clips are not assigned in Inspector
                if (AudioManager.Instance != null)
                {
                    AudioManager.Instance.PlaySfxAt("sfx_footstep_walk", transform.position, 1.0f, 0.8f);
                }
                return;
            }

            AudioClip clip = stepClips[Random.Range(0, stepClips.Length)];
            if (clip == null) return;

            float minPitch = tuning != null ? tuning.monsterMinStepPitch : 0.85f;
            float maxPitch = tuning != null ? tuning.monsterMaxStepPitch : 1.15f;
            float baseVol = tuning != null ? tuning.monsterStepVolume : 1.0f;

            // Dynamic pitch and volume scaling with speed: faster = higher pitch and heavier volume
            float speedFactor = Mathf.Clamp01(speed / 6.0f);
            float pitch = Mathf.Lerp(minPitch, maxPitch, speedFactor);
            float volume = Mathf.Lerp(baseVol * 0.7f, baseVol, speedFactor);

            _audioSource.pitch = pitch;
            _audioSource.PlayOneShot(clip, volume);
        }
    }
}


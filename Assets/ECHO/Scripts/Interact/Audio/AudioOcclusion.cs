using UnityEngine;

namespace Echo.Interact
{
    /// <summary>
    /// Lightweight physical audio occlusion:
    /// - Performs an un-allocated Physics.Linecast to the active AudioListener at periodic intervals.
    /// - Dynamically lerps an AudioLowPassFilter and AudioSource volume multiplier to simulate sound muffled by walls/doors.
    /// - Disables the AudioLowPassFilter when unoccluded to eliminate DSP overhead.
    /// - Zero allocations per frame.
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public sealed class AudioOcclusion : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private InteractTuning tuning;

        [Header("Layer Mask")]
        [Tooltip("Layer mask representing solid barriers (walls, closed doors, structures).")]
        [SerializeField] private LayerMask occluderMask = ~0;

        [Header("Debugging")]
        [SerializeField] private bool showGizmos = true;

        // ── Components ────────────────────────────────────────────────────────
        private AudioSource _audioSource;
        private AudioLowPassFilter _lowPassFilter;
        private static AudioListener _cachedListener;

        // ── State ─────────────────────────────────────────────────────────────
        private float _baseVolume = 1.0f;
        private float _currentCutoff = 22000f;
        private float _currentVolumeMultiplier = 1.0f;
        private float _checkTimer;
        private bool _isOccluded;

        /// <summary>Current occlusion volume multiplier [occludedVolume..1.0].</summary>
        public float OcclusionVolumeMultiplier => _currentVolumeMultiplier;

        /// <summary>True when linecast indicates an obstruction between sound and listener.</summary>
        public bool IsOccluded => _isOccluded;

        // ── Unity Callbacks ───────────────────────────────────────────────────

        /// <summary>
        /// Awake: Cache AudioSource and ensure AudioLowPassFilter component exists.
        /// </summary>
        private void Awake()
        {
            _audioSource = GetComponent<AudioSource>();
            _baseVolume = _audioSource.volume;

            _lowPassFilter = GetComponent<AudioLowPassFilter>();
            if (_lowPassFilter == null)
            {
                _lowPassFilter = gameObject.AddComponent<AudioLowPassFilter>();
            }

            _currentCutoff = GetOpenCutoff();
            _lowPassFilter.cutoffFrequency = _currentCutoff;
            _lowPassFilter.enabled = false; // Disabled until occluded to save DSP
        }

        /// <summary>
        /// Start: Cache initial AudioListener and randomize check timer to stagger raycasts across sources.
        /// </summary>
        private void Start()
        {
            ResolveListener();
            float interval = GetCheckInterval();
            _checkTimer = Random.Range(0f, interval);
        }

        /// <summary>
        /// OnEnable: Reset occlusion state when enabled.
        /// </summary>
        private void OnEnable()
        {
            _isOccluded = false;
            _currentCutoff = GetOpenCutoff();
            _currentVolumeMultiplier = 1.0f;
            if (_lowPassFilter != null)
            {
                _lowPassFilter.cutoffFrequency = _currentCutoff;
                _lowPassFilter.enabled = false;
            }
        }

        /// <summary>
        /// OnDisable: Restore filter and volume to clear state cleanly.
        /// </summary>
        private void OnDisable()
        {
            if (_lowPassFilter != null)
            {
                _lowPassFilter.cutoffFrequency = GetOpenCutoff();
                _lowPassFilter.enabled = false;
            }

            if (_audioSource != null)
            {
                _audioSource.volume = _baseVolume;
            }
        }

        /// <summary>
        /// Update: Periodically runs Physics.Linecast, then smoothly lerps cutoff and volume every frame.
        /// </summary>
        private void Update()
        {
            // 1. Periodic linecast check
            _checkTimer -= Time.deltaTime;
            if (_checkTimer <= 0f)
            {
                _checkTimer = GetCheckInterval();
                CheckOcclusion();
            }

            // 2. Smooth per-frame lerp toward target values
            float targetCutoff = _isOccluded ? GetOccludedCutoff() : GetOpenCutoff();
            float targetVol = _isOccluded ? GetOccludedVolume() : 1.0f;
            float smoothing = GetSmoothing();

            _currentCutoff = Mathf.Lerp(_currentCutoff, targetCutoff, Time.deltaTime * smoothing);
            _currentVolumeMultiplier = Mathf.Lerp(_currentVolumeMultiplier, targetVol, Time.deltaTime * smoothing);

            // Apply to LowPassFilter
            if (_lowPassFilter != null)
            {
                _lowPassFilter.cutoffFrequency = _currentCutoff;

                // Disable DSP filter when fully open and clear
                bool needsFilter = _isOccluded || _currentCutoff < 21900f;
                if (_lowPassFilter.enabled != needsFilter)
                {
                    _lowPassFilter.enabled = needsFilter;
                }
            }

            // Apply to AudioSource volume
            if (_audioSource != null)
            {
                _audioSource.volume = _baseVolume * _currentVolumeMultiplier;
            }
        }

        // ── Occlusion Evaluation ──────────────────────────────────────────────

        private void CheckOcclusion()
        {
            if (_cachedListener == null)
            {
                ResolveListener();
                if (_cachedListener == null)
                {
                    _isOccluded = false;
                    return;
                }
            }

            Vector3 soundPos = transform.position;
            Vector3 listenerPos = _cachedListener.transform.position;

            float maxDist = GetMaxDistance();
            Vector3 delta = listenerPos - soundPos;
            float distSqr = delta.sqrMagnitude;

            // Sources further than maxDistance skip linecasts
            if (distSqr > maxDist * maxDist)
            {
                _isOccluded = false;
                return;
            }

            float dist = Mathf.Sqrt(distSqr);
            if (dist <= 0.2f)
            {
                _isOccluded = false;
                return;
            }

            // Offset start and end slightly to avoid intersecting source or player colliders
            Vector3 dir = delta / dist;
            Vector3 startOffset = soundPos + dir * 0.15f;
            Vector3 endOffset = listenerPos - dir * 0.15f;

            _isOccluded = Physics.Linecast(startOffset, endOffset, occluderMask.value);
        }

        private static void ResolveListener()
        {
            if (_cachedListener != null && _cachedListener.isActiveAndEnabled)
                return;

            _cachedListener = FindFirstObjectByType<AudioListener>();
        }

        // ── Tuning Accessors ──────────────────────────────────────────────────

        private float GetCheckInterval() => tuning != null ? tuning.occlusionCheckInterval : 0.15f;
        private float GetMaxDistance() => tuning != null ? tuning.occlusionMaxDistance : 25.0f;
        private float GetOpenCutoff() => tuning != null ? tuning.occlusionOpenCutoff : 22000f;
        private float GetOccludedCutoff() => tuning != null ? tuning.occlusionCutoff : 1200f;
        private float GetOccludedVolume() => tuning != null ? tuning.occlusionVolume : 0.6f;
        private float GetSmoothing() => tuning != null ? tuning.occlusionSmoothing : 8.0f;

        // ── Gizmos ────────────────────────────────────────────────────────────

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (!showGizmos || !Application.isPlaying || _cachedListener == null)
                return;

            Gizmos.color = _isOccluded ? Color.red : Color.green;
            Gizmos.DrawLine(transform.position, _cachedListener.transform.position);
        }
#endif
    }
}


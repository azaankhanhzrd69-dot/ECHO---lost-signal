using UnityEngine;

namespace Echo.World
{
    /// <summary>
    /// Emergency light that flickers more when noise happens nearby and as
    /// the story progresses. Subscribes to EchoEvents.Noise and BeatChanged
    /// only — no Core or MonsterAI references.
    /// </summary>
    public class EmergencyLightFlicker : MonoBehaviour
    {
        [SerializeField] private Light[] lights;
        [SerializeField] private Renderer optionalRenderer;
        [SerializeField] private Color emissionColor = Color.yellow;
        [SerializeField] private float baseIntensity;
        [SerializeField] private float flickerAmount = 0.2f;
        [SerializeField] private float flickerSpeed = 1f;
        [SerializeField] private float calmMultiplier = 1f;
        [SerializeField] private float tenseMultiplier = 2f;
        [SerializeField] private float noiseRadius = 5f;
        [SerializeField] private float decaySeconds = 3f;

        [SerializeField] private bool useBeatBaseline = true;
        [SerializeField] private BeatTensionFloor[] beatTensionFloors;

        private float tension;
        private Transform cachedTransform;

        private void Awake()
        {
            cachedTransform = transform;

            foreach (var light in lights)
            {
                if (light != null && baseIntensity <= 0f)
                {
                    baseIntensity = light.intensity;
                }
            }

            if (optionalRenderer != null)
            {
                optionalRenderer.materials ??= new Material[0];
            }
        }

        private void OnEnable()
        {
            EchoEvents.Noise += OnNoise;
            EchoEvents.BeatChanged += OnBeatChanged;
        }

        private void OnDisable()
        {
            EchoEvents.Noise -= OnNoise;
            EchoEvents.BeatChanged -= OnBeatChanged;
        }

        private void Update()
        {
            tension = Mathf.Max(tension - (Time.deltaTime / decaySeconds), 0f);

            float effectiveAmount = flickerAmount * Mathf.Lerp(calmMultiplier, tenseMultiplier, tension);
            float effectiveSpeed = flickerSpeed * Mathf.Lerp(calmMultiplier, tenseMultiplier, tension);

            float perlin = Mathf.PerlinNoise(Time.time * effectiveSpeed, 0f);

            foreach (var light in lights)
            {
                if (light == null) continue;
                float intensity = baseIntensity * (1f - effectiveAmount * perlin);
                light.intensity = Mathf.Max(intensity, 0f);
            }

            if (optionalRenderer != null)
            {
                // Use a MaterialPropertyBlock to avoid allocating a new material instance every frame.
                var block = new MaterialPropertyBlock();
                block.SetColor("_EmissionColor", emissionColor * tension);
                optionalRenderer.SetPropertyBlock(block);
            }
        }

        private void OnNoise(Vector3 position, float loudness)
        {
            float dist = Vector3.Distance(cachedTransform.position, position);
            if (dist > noiseRadius) return;

            float scaled = loudness * (1f - dist / noiseRadius);
            tension = Mathf.Min(tension + scaled, 1f);
        }

        private void OnBeatChanged(Beat newBeat)
        {
            if (!useBeatBaseline) return;

            foreach (var entry in beatTensionFloors)
            {
                if (entry.beat == newBeat)
                {
                    tension = Mathf.Max(tension, entry.tensionFloor);
                    break;
                }
            }
        }

        [System.Serializable]
        private struct BeatTensionFloor
        {
            public Beat beat;
            [Range(0f, 1f)] public float tensionFloor;
        }
    }
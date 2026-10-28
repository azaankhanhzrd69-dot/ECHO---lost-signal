using UnityEngine;

namespace Echo.Lighting
{
    /// <summary>
    /// Controls realistic horror/sci-fi flickering for Unity Lights (URP compatible).
    /// Supports smooth random noise flickering, sudden glitch bursts, and optional audio synchronization.
    /// </summary>
    [RequireComponent(typeof(Light))]
    public class FlickeringLight : MonoBehaviour
    {
        [Header("Flicker Intensity Settings")]
        [Tooltip("Minimum brightness during flicker.")]
        [SerializeField] private float minIntensity = 0.2f;

        [Tooltip("Maximum brightness during flicker.")]
        [SerializeField] private float maxIntensity = 1.5f;

        [Tooltip("Speed of Perlin noise fluctuation.")]
        [SerializeField] private float flickerSpeed = 15f;

        [Header("Random Glitches / Cutouts")]
        [Tooltip("Enables sudden pitch-black or near-black power drops.")]
        [SerializeField] private bool enableRandomBlackouts = true;

        [Range(0f, 1f)]
        [Tooltip("Probability of a momentary blackout per second.")]
        [SerializeField] private float blackoutChance = 0.08f;

        [Tooltip("Duration in seconds for momentary blackouts.")]
        [SerializeField] private Vector2 blackoutDurationRange = new Vector2(0.05f, 0.25f);

        [Header("Optional Audio / Emissive Linking")]
        [Tooltip("Optional audio source for buzzing, hum, or electrical pops.")]
        [SerializeField] private AudioSource buzzAudioSource;

        [Tooltip("Optional Renderer with emissive material (e.g. fluorescent bulb/panel mesh).")]
        [SerializeField] private Renderer targetRenderer;

        [Tooltip("Material slot index on targetRenderer to synchronize emission with.")]
        [SerializeField] private int materialIndex = 0;

        private Light lightComponent;
        private Material targetMaterial;
        private float noiseSeed;
        private bool isBlackedOut;
        private float blackoutTimer;
        private static readonly int EmissionColorProp = Shader.PropertyToID("_EmissionColor");
        private Color baseEmissionColor = Color.white;

        private void Awake()
        {
            lightComponent = GetComponent<Light>();
            noiseSeed = Random.Range(0f, 1000f);

            if (targetRenderer != null && targetRenderer.materials.Length > materialIndex)
            {
                targetMaterial = targetRenderer.materials[materialIndex];
                if (targetMaterial.HasProperty(EmissionColorProp))
                {
                    baseEmissionColor = targetMaterial.GetColor(EmissionColorProp);
                    targetMaterial.EnableKeyword("_EMISSION");
                }
            }
        }

        private void Update()
        {
            if (lightComponent == null) return;

            // Handle momentary blackout state
            if (isBlackedOut)
            {
                blackoutTimer -= Time.deltaTime;
                if (blackoutTimer <= 0f)
                {
                    isBlackedOut = false;
                }
                else
                {
                    SetLightState(0f);
                    return;
                }
            }
            else if (enableRandomBlackouts && Random.value < (blackoutChance * Time.deltaTime))
            {
                isBlackedOut = true;
                blackoutTimer = Random.Range(blackoutDurationRange.x, blackoutDurationRange.y);
                SetLightState(0f);
                return;
            }

            // Continuous Perlin noise flicker for organic fluorescent / failing bulb behavior
            float noise = Mathf.PerlinNoise(noiseSeed, Time.time * flickerSpeed);
            float targetIntensity = Mathf.Lerp(minIntensity, maxIntensity, noise);

            SetLightState(targetIntensity);
        }

        private void SetLightState(float intensity)
        {
            lightComponent.intensity = intensity;

            // Optional emissive material sync
            if (targetMaterial != null)
            {
                float factor = Mathf.InverseLerp(0f, maxIntensity, intensity);
                targetMaterial.SetColor(EmissionColorProp, baseEmissionColor * factor);
            }

            // Optional audio volume sync (electrical buzzing)
            if (buzzAudioSource != null)
            {
                buzzAudioSource.volume = Mathf.InverseLerp(0f, maxIntensity, intensity);
            }
        }

        private void OnDestroy()
        {
            if (targetMaterial != null)
            {
                Destroy(targetMaterial);
            }
        }
    }
}

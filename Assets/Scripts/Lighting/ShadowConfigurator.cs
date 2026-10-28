using UnityEngine;

namespace Echo.Lighting
{
    /// <summary>
    /// Utility helper to configure and optimize shadow settings on lights automatically,
    /// ensuring soft shadows, appropriate bias to prevent shadow acne / peter panning,
    /// and performance tuning for indoor spaces.
    /// </summary>
    [RequireComponent(typeof(Light))]
    [ExecuteAlways]
    public class ShadowConfigurator : MonoBehaviour
    {
        [Header("Shadow Setup")]
        [Tooltip("Shadow resolution and filtering mode.")]
        [SerializeField] private LightShadows shadowType = LightShadows.Soft;

        [Range(0f, 1f)]
        [Tooltip("Shadow darkness strength (0 = no shadow, 1 = full darkness).")]
        [SerializeField] private float shadowStrength = 0.9f;

        [Range(0f, 2f)]
        [Tooltip("Depth bias to prevent shadow acne (surface striations).")]
        [SerializeField] private float depthBias = 0.05f;

        [Range(0f, 3f)]
        [Tooltip("Normal bias to avoid peter-panning (shadows detaching from object bases).")]
        [SerializeField] private float normalBias = 0.4f;

        [Range(0.01f, 1f)]
        [Tooltip("Near plane distance for shadow calculations.")]
        [SerializeField] private float shadowNearPlane = 0.1f;

        private Light targetLight;

        private void OnEnable()
        {
            ApplyShadowSettings();
        }

        private void OnValidate()
        {
            ApplyShadowSettings();
        }

        public void ApplyShadowSettings()
        {
            if (targetLight == null)
            {
                targetLight = GetComponent<Light>();
            }

            if (targetLight != null)
            {
                targetLight.shadows = shadowType;
                targetLight.shadowStrength = shadowStrength;
                targetLight.shadowBias = depthBias;
                targetLight.shadowNormalBias = normalBias;
                targetLight.shadowNearPlane = shadowNearPlane;
            }
        }
    }
}

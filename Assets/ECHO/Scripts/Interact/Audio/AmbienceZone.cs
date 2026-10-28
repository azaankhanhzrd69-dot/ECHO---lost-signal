using UnityEngine;

namespace Echo.Interact
{
    /// <summary>
    /// Environmental trigger zone that transitions station ambience when entered by the player.
    /// Attach to a GameObject with a Trigger Collider (BoxCollider, SphereCollider, etc.).
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public sealed class AmbienceZone : MonoBehaviour
    {
        [Header("Ambience Settings")]
        [Tooltip("The SFX loop ID from sfx.csv to play in this zone (e.g. sfx_ambience_medbay).")]
        [SerializeField] private string ambienceSfxId = "sfx_ambience_corridor";

        [Tooltip("Duration in seconds for the crossfade transition.")]
        [SerializeField] [Min(0.1f)] private float crossfadeDuration = 2.0f;

        private void OnTriggerEnter(Collider other)
        {
            // Verify if the entering entity is the player
            if (IsPlayer(other))
            {
                if (AudioManager.Instance != null)
                {
                    AudioManager.Instance.CrossfadeAmbience(ambienceSfxId, crossfadeDuration);
                }
            }
        }

        private static bool IsPlayer(Collider col)
        {
            if (col == null) return false;
            if (col.CompareTag("Player") || col.CompareTag("MainCamera")) return true;
            if (col.GetComponent<CharacterController>() != null) return true;
            return false;
        }
    }
}


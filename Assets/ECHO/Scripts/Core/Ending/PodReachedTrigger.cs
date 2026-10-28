using Echo.Shared;
using UnityEngine;

namespace Echo.Core
{
    /// <summary>
    /// Trigger volume placed at the escape pod entrance.
    /// Advances the story beat from Chase to Airlock when reached by the player.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public sealed class PodReachedTrigger : MonoBehaviour
    {
        [Header("Filtering")]
        [Tooltip("Optional tag filter to verify the player (leave empty to check CharacterController).")]
        [SerializeField] private string playerTag = "Player";

        private void OnTriggerEnter(Collider other)
        {
            if (other == null) return;

            // Only active during the Chase beat
            if (Progress.Current != Beat.Chase)
                return;

            // Validate that entering entity is the player
            if (!IsPlayer(other))
                return;

            Debug.Log("[PodReachedTrigger] Player reached escape pod. Transitioning to Beat.Airlock.");
            EchoEvents.SetBeat(Beat.Airlock);
        }

        private bool IsPlayer(Collider col)
        {
            if (!string.IsNullOrEmpty(playerTag) && col.CompareTag(playerTag))
                return true;

            if (col.GetComponent<CharacterController>() != null)
                return true;

            return false;
        }
    }
}


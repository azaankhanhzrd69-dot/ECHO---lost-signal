using UnityEngine;
using Echo.Shared;

namespace Echo.World
{
    /// <summary>
    /// Trigger volume that advances the story beat when the player enters.
    /// Fires only once unless reset via OnEnable (e.g. on respawn).
    /// </summary>
    public class BeatTrigger : MonoBehaviour
    {
        [SerializeField] private Beat beatToSet;
        [SerializeField] private Beat requiredCurrentBeat;
        [SerializeField] private bool oneShot = true;
        [SerializeField] private string playerTag = "Player";

        private bool fired;

        private void OnEnable()
        {
            fired = false;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (fired) return;
            if (!other.CompareTag(playerTag)) return;
            if (Progress.Current != requiredCurrentBeat) return;

            EchoEvents.SetBeat(beatToSet);
            fired = true;
        }
    }
}
using Echo.Shared;
using UnityEngine;

namespace Echo.Interact
{
    /// <summary>
    /// Test component: attach to any GameObject with a Collider to verify the
    /// interaction system.  Logs a configurable message when the player presses E.
    /// </summary>
    public sealed class TestInteractable : MonoBehaviour, IInteractable
    {
        [SerializeField]
        [Tooltip("Text shown on the player's HUD when targeting this object.")]
        private string prompt = "Inspect";

        [SerializeField]
        [Tooltip("Message logged to the Console when the player interacts.")]
        private string testMessage = "TestInteractable: Interact() called!";

        // ── IInteractable ──────────────────────────────────────────────────────

        /// <summary>Returns the HUD prompt text.</summary>
        public string Prompt => prompt;

        /// <summary>Logs the test message to the Console.</summary>
        public void Interact()
        {
            Debug.Log(testMessage, this);
        }
    }
}


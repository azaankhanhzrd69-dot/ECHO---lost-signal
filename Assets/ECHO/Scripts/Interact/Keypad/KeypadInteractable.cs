using Echo.Shared;
using UnityEngine;

namespace Echo.Interact
{
    /// <summary>
    /// Keypad interactable placed in the station world (e.g. beside the decompression blast door).
    /// When interacted with [E], opens the associated KeypadUI.
    /// </summary>
    public sealed class KeypadInteractable : MonoBehaviour, IInteractable
    {
        [Header("References")]
        [SerializeField]
        [Tooltip("Reference to the KeypadUI canvas controller.")]
        private KeypadUI keypadUI;

        [SerializeField]
        [Tooltip("Prompt displayed on HUD when targeting the keypad.")]
        private string prompt = "Use Keypad";

        public string Prompt => keypadUI != null && keypadUI.IsOpen ? string.Empty : prompt;

        public void Interact()
        {
            if (keypadUI == null)
            {
                Debug.LogWarning("[KeypadInteractable] KeypadUI reference is missing!", this);
                return;
            }

            if (!keypadUI.IsOpen)
            {
                keypadUI.Open();
            }
        }
    }
}


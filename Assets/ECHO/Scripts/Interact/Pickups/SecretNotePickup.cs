using Echo.Shared;
using UnityEngine;

namespace Echo.Interact
{
    /// <summary>
    /// Secret note pickup on dead astronaut: Sets Progress.HasNote = true and stores Morse code data.
    /// </summary>
    public sealed class SecretNotePickup : PickupBase
    {
        [Header("Note Content")]
        [SerializeField]
        [Tooltip("Optional text asset containing secret transmission & frequency details.")]
        private TextAsset noteAsset;

        [SerializeField]
        [TextArea(3, 8)]
        [Tooltip("Fallback or direct string containing the Morse text and frequency.")]
        private string noteContent = "FREQ: 142.85 MHz\nTRANSMIT: SOS";

        public string NoteContent => noteAsset != null ? noteAsset.text : noteContent;

        protected override void OnCollected()
        {
            Progress.HasNote = true;
            Debug.Log($"[SecretNotePickup] Secret note found:\n{NoteContent}");
        }
    }
}


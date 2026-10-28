using Echo.Shared;
using UnityEngine;

namespace Echo.Interact
{
    /// <summary>
    /// Vent paper pickup: Grants station map and displays/provides keypad code.
    /// Advances story beat to Keypad if collected during Beat.Vent.
    /// </summary>
    public sealed class VentPaperPickup : PickupBase
    {
        [Header("Vent Paper Config")]
        [SerializeField]
        [Tooltip("Optional KeypadConfig asset created in I-03. If assigned, reads code directly from it.")]
        private KeypadConfig keypadConfig;

        [SerializeField]
        [Tooltip("Fallback keypad code if KeypadConfig is not assigned.")]
        private string keypadCode = "0451";

        public string KeypadCode => keypadConfig != null ? keypadConfig.CorrectCode : keypadCode;

        protected override void OnCollected()
        {
            Progress.HasMap = true;
            Debug.Log($"[VentPaperPickup] Collected! Map acquired. Keypad Code: {KeypadCode}");

            if (Progress.Current == Beat.Vent)
            {
                EchoEvents.SetBeat(Beat.Keypad);
            }
        }
    }
}


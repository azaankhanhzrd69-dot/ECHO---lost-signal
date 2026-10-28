using UnityEngine;

namespace Echo.Interact
{
    /// <summary>
    /// Configuration for the airlock/decompression keypad.
    /// ScriptableObject asset referenced by KeypadUI, KeypadInteractable, and VentPaperPickup.
    /// </summary>
    [CreateAssetMenu(fileName = "KeypadConfig", menuName = "ECHO/Keypad Config")]
    public sealed class KeypadConfig : ScriptableObject
    {
        [Header("Security Code")]
        [SerializeField]
        [Tooltip("The correct access code. Default placeholder is 0000; set in the Inspector according to the story/level design.")]
        private string correctCode = "0000";

        [SerializeField]
        [Tooltip("Maximum allowed digits in entered code.")]
        private int maxCodeLength = 4;

        public string CorrectCode => correctCode;
        public int MaxCodeLength => maxCodeLength;
    }
}


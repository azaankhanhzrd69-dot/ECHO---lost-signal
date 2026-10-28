using Echo.Shared;
using UnityEngine;

namespace Echo.Interact
{
    /// <summary>
    /// Interactive in-world Morse Radio console.
    /// Implements IInteractable:
    /// - Displays prompt to use the radio only during Beat.PowerOn when the player has the secret note.
    /// - Otherwise displays an offline / no signal message.
    /// - Interacting opens the MorseRadioUI.
    /// </summary>
    public sealed class MorseRadio : MonoBehaviour, IInteractable
    {
        [Header("Configuration")]
        [SerializeField] private InteractTuning tuning;

        [Header("UI Reference")]
        [SerializeField] private MorseRadioUI radioUI;

        /// <summary>
        /// True if the player is currently in Beat.PowerOn AND has retrieved the secret note.
        /// </summary>
        public bool IsUsable => Progress.Current == Beat.PowerOn && Progress.HasNote;

        /// <summary>
        /// Prompt read by the player Interactor HUD raycast.
        /// </summary>
        public string Prompt
        {
            get
            {
                if (IsUsable)
                {
                    return tuning != null ? tuning.promptUsable : "Use radio [E]";
                }
                return tuning != null ? tuning.promptNoSignal : "Radio (Offline / No frequency)";
            }
        }

        /// <summary>
        /// Called when the player presses the interact key [E] while looking at the radio.
        /// </summary>
        public void Interact()
        {
            if (!IsUsable)
            {
                Debug.Log("[MorseRadio] Cannot use radio: requires Beat.PowerOn and Progress.HasNote.");
                return;
            }

            if (radioUI != null)
            {
                radioUI.Open();
            }
            else
            {
                Debug.LogWarning("[MorseRadio] MorseRadioUI reference is missing!");
            }
        }
    }
}


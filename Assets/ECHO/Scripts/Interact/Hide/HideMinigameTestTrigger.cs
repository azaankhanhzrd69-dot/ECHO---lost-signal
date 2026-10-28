using Echo.Shared;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Echo.Interact
{
    /// <summary>
    /// TEMPORARY test trigger for I-04 locker hide minigame.
    /// Safe to delete once Core triggers EchoEvents.HideStarted naturally.
    /// Press F2 in Play Mode to simulate entering a locker.
    /// </summary>
    public sealed class HideMinigameTestTrigger : MonoBehaviour
    {
        [Header("Debug Controls")]
        [SerializeField]
        [Tooltip("Key to trigger minigame manually.")]
        private Key triggerKey = Key.F2;

        private void OnEnable()
        {
            EchoEvents.HideFinished += OnHideFinished;
        }

        private void OnDisable()
        {
            EchoEvents.HideFinished -= OnHideFinished;
        }

        private void Update()
        {
            if (Keyboard.current != null && Keyboard.current[triggerKey].wasPressedThisFrame)
            {
                Debug.Log("[HideMinigameTestTrigger] F2 pressed: Triggering EchoEvents.StartHide(transform.position)...");
                EchoEvents.StartHide(transform.position);
            }
        }

        private void OnHideFinished(bool survived)
        {
            Debug.Log($"[HideMinigameTestTrigger] Received EchoEvents.HideFinished! Survived = {survived}");
        }
    }
}


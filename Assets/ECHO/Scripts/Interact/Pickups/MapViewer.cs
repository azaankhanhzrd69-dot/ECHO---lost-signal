using Echo.Shared;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Echo.Interact
{
    /// <summary>
    /// Station Map viewer UI and trigger.
    /// Can be opened via IInteractable (or hotkey) when Progress.HasMap is true.
    /// Locks input via EchoEvents while open and closes with E or Escape.
    /// </summary>
    public sealed class MapViewer : MonoBehaviour, IInteractable
    {
        [Header("UI References")]
        [SerializeField]
        [Tooltip("Root GameObject of the full-screen map overlay.")]
        private GameObject mapPanel;

        [Header("Interaction")]
        [SerializeField]
        [Tooltip("Prompt shown when looking at the map terminal/station.")]
        private string prompt = "View Map";

        [SerializeField]
        [Tooltip("Optional: allow pressing M anytime to toggle map if player has it.")]
        private bool allowHotkey = true;

        private bool ownsInputLock;
        private bool isOpen;
        private int openFrame = -1;

        public string Prompt => Progress.HasMap ? prompt : string.Empty;

        private void Awake()
        {
            if (mapPanel != null)
                mapPanel.SetActive(false);
        }

        private void OnDisable()
        {
            if (isOpen)
                CloseMap();
        }

        private void Update()
        {
            if (Keyboard.current == null)
                return;

            if (isOpen)
            {
                // Don't close on the same frame it was opened (prevents frame race condition with E key)
                if (Time.frameCount == openFrame)
                    return;

                // Close when E or Escape was pressed this frame
                if (Keyboard.current.eKey.wasPressedThisFrame || Keyboard.current.escapeKey.wasPressedThisFrame)
                {
                    CloseMap();
                }
            }
            else if (allowHotkey && Progress.HasMap && !EchoEvents.InputLocked)
            {
                if (Keyboard.current.mKey.wasPressedThisFrame)
                {
                    OpenMap();
                }
            }
        }

        public void Interact()
        {
            if (!Progress.HasMap)
            {
                Debug.Log("[MapViewer] Cannot view map: Progress.HasMap is false.");
                return;
            }

            if (!isOpen)
                OpenMap();
            else
                CloseMap();
        }

        public void OpenMap()
        {
            if (mapPanel != null)
                mapPanel.SetActive(true);

            isOpen = true;
            openFrame = Time.frameCount;
            ownsInputLock = !EchoEvents.InputLocked;   // only lock if we're the first locker
            if (ownsInputLock)
                EchoEvents.LockInput();
        }

        public void CloseMap()
        {
            if (mapPanel != null)
                mapPanel.SetActive(false);

            isOpen = false;
            if (ownsInputLock)
            {
                ownsInputLock = false;
                EchoEvents.UnlockInput();
            }
        }
    }
}


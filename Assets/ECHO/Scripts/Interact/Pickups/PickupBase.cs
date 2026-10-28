using Echo.Shared;
using UnityEngine;

namespace Echo.Interact
{
    /// <summary>
    /// Base class for all pickup interactables in the station.
    /// Manages interaction prompt, one-time collection guard, and disabling visual/collider components.
    /// Subclasses implement OnCollected() for specific logic.
    /// </summary>
    public abstract class PickupBase : MonoBehaviour, IInteractable
    {
        [Header("Pickup Settings")]
        [SerializeField]
        [Tooltip("Prompt displayed on HUD when looking at this pickup.")]
        private string prompt = "Pick Up";

        [SerializeField]
        [Tooltip("Whether to disable the entire GameObject on collection. If false, disables collider/renderers instead.")]
        private bool disableGameObjectOnCollect = true;

        /// <summary>Guards against double collection.</summary>
        protected bool isCollected { get; private set; }

        public virtual string Prompt => prompt;

        public void Interact()
        {
            if (isCollected)
                return;

            isCollected = true;
            OnCollected();

            if (disableGameObjectOnCollect)
            {
                gameObject.SetActive(false);
            }
            else
            {
                Collider col = GetComponent<Collider>();
                if (col != null) col.enabled = false;

                Renderer[] renderers = GetComponentsInChildren<Renderer>();
                for (int i = 0; i < renderers.Length; i++)
                {
                    renderers[i].enabled = false;
                }
            }
        }

        /// <summary>
        /// Specific collection behavior executed exactly once when picked up.
        /// </summary>
        protected abstract void OnCollected();
    }
}


using Echo.Shared;
using UnityEngine;

namespace Echo.Interact
{
    /// <summary>
    /// Power core pickup. Increments Progress.CoresFound and disables itself.
    /// </summary>
    public sealed class CorePickup : PickupBase
    {
        protected override void OnCollected()
        {
            Progress.CoresFound++;
            Debug.Log($"[CorePickup] Core collected! Total cores found: {Progress.CoresFound}");
        }
    }
}


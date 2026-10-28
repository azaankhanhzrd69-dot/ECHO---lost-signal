using Echo.Shared;
using UnityEngine;

namespace Echo.Core
{
    /// <summary>
    /// Designated spawn location for a specific story beat.
    /// Defines where the player and optionally the monster are teleported upon respawn.
    /// </summary>
    public sealed class CheckpointPoint : MonoBehaviour
    {
        [Tooltip("The story beat this checkpoint serves.")]
        [SerializeField] public Beat beat = Beat.Boarding;

        [Tooltip("Transform defining the player's respawn position and rotation.")]
        [SerializeField] public Transform playerPoint;

        [Tooltip("Optional Transform defining the monster's position and rotation upon respawn.")]
        [SerializeField] public Transform monsterPoint;

        private void Awake()
        {
            DeathRespawnController.RegisterPoint(this);
        }

        private void OnDestroy()
        {
            DeathRespawnController.UnregisterPoint(this);
        }
    }
}


namespace Echo.Shared
{
    /// <summary>
    /// Shared contract for interactive objects across all lanes.
    /// Implemented by: Interact lane components (doors, keypads, items, hiding spots).
    /// Listened by / Read by: Core player interaction raycaster (reads Prompt for UI, calls Interact on activation).
    /// </summary>
    public interface IInteractable
    {
        /// <summary>
        /// Text prompt displayed on the player HUD when targeting this interactable (e.g. "Enter Vent [E]").
        /// </summary>
        string Prompt { get; }

        /// <summary>
        /// Executes the interaction logic when triggered by the player.
        /// </summary>
        void Interact();
    }
}


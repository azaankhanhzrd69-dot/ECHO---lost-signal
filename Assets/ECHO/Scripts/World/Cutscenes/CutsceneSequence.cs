using System.Collections.Generic;
using UnityEngine;

namespace Echo.World
{
    /// <summary>
    /// An ordered list of <see cref="CutsceneStep"/> objects that the
    /// <see cref="CutsceneDirector"/> plays back as a single cutscene.
    ///
    /// Create via: ECHO → Cutscene Sequence in the Project context menu.
    /// Assign to a <see cref="CutsceneTrigger"/> or call CutsceneDirector.Play() directly.
    /// </summary>
    [CreateAssetMenu(fileName = "NewCutsceneSequence", menuName = "ECHO/Cutscene Sequence")]
    public class CutsceneSequence : ScriptableObject
    {
        [Tooltip("Steps executed in order from index 0 upward.")]
        public List<CutsceneStep> steps = new List<CutsceneStep>();

        [Tooltip("Whether the player can skip this cutscene by holding the skip key. " +
                 "Leave false for story-critical cutscenes (e.g. Boarding) that must play in full.")]
        public bool allowSkip = false;
    }
}


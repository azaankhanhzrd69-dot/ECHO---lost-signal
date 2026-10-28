using UnityEngine;
using Echo.Shared;

namespace Echo.World
{
    /// <summary>
    /// Listens for a specific story beat and fires a <see cref="CutsceneSequence"/>
    /// on the assigned <see cref="CutsceneDirector"/> when that beat is reached.
    ///
    /// Attach to any scene GameObject. Configure <see cref="triggerBeat"/>,
    /// <see cref="sequence"/>, and <see cref="director"/> in the Inspector.
    /// </summary>
    public class CutsceneTrigger : MonoBehaviour
    {
        [Tooltip("The beat that triggers playback of this cutscene.")]
        [SerializeField] private Beat triggerBeat = Beat.Boarding;

        [Tooltip("The sequence asset to play when the beat matches.")]
        [SerializeField] private CutsceneSequence sequence;

        [Tooltip("The CutsceneDirector MonoBehaviour that will run the sequence.")]
        [SerializeField] private CutsceneDirector director;

        // ── Unity callbacks ───────────────────────────────────────────────────

        private void OnEnable()
        {
            EchoEvents.BeatChanged += OnBeatChanged;
        }

        private void OnDisable()
        {
            EchoEvents.BeatChanged -= OnBeatChanged;
        }

        // ── Handler ───────────────────────────────────────────────────────────

        private void OnBeatChanged(Beat newBeat)
        {
            if (newBeat != triggerBeat) return;

            if (sequence == null)
            {
                Debug.LogWarning($"[CutsceneTrigger] Beat {triggerBeat} reached but no sequence assigned.", this);
                return;
            }

            if (director == null)
            {
                Debug.LogWarning($"[CutsceneTrigger] Beat {triggerBeat} reached but no director assigned.", this);
                return;
            }

            director.Play(sequence);
        }
    }
}


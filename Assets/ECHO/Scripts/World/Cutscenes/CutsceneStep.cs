using System;
using UnityEngine;
using UnityEngine.Events;
using Echo.Shared;

namespace Echo.World
{
    /// <summary>
    /// Defines the operation a single step in a CutsceneSequence performs.
    /// </summary>
    public enum StepType
    {
        /// <summary>Pause for <see cref="CutsceneStep.duration"/> seconds.</summary>
        Wait,

        /// <summary>
        /// Interpolate the cutscene camera from <see cref="CutsceneStep.fromWaypoint"/>
        /// to <see cref="CutsceneStep.toWaypoint"/> over <see cref="CutsceneStep.duration"/> seconds.
        /// </summary>
        MoveCamera,

        /// <summary>
        /// Rotate the cutscene camera to face <see cref="CutsceneStep.toWaypoint"/>
        /// over <see cref="CutsceneStep.duration"/> seconds.
        /// </summary>
        LookAt,

        /// <summary>
        /// Fire <see cref="CutsceneStep.onStepEvent"/>.
        /// Wire it in the Inspector to DialoguePlayer.PlayById(id).
        /// The line id is stored in <see cref="CutsceneStep.stringParam"/>.
        /// </summary>
        PlayLine,

        /// <summary>
        /// Advance the story beat to <see cref="CutsceneStep.beatParam"/>
        /// via EchoEvents.SetBeat.
        /// </summary>
        SetBeat,

        /// <summary>Fade the full-screen overlay alpha from 0 → 1 over <see cref="CutsceneStep.duration"/> seconds.</summary>
        FadeIn,

        /// <summary>Fade the full-screen overlay alpha from 1 → 0 over <see cref="CutsceneStep.duration"/> seconds.</summary>
        FadeOut,

        /// <summary>
        /// Fire <see cref="CutsceneStep.onStepEvent"/> immediately.
        /// The event name is stored in <see cref="CutsceneStep.stringParam"/> for documentation only.
        /// </summary>
        InvokeEvent,
    }

    /// <summary>
    /// One step inside a <see cref="CutsceneSequence"/>.
    /// Serialised as a plain class so it can appear in a ScriptableObject list.
    /// </summary>
    [Serializable]
    public class CutsceneStep
    {
        // ── Type ──────────────────────────────────────────────────────────────
        [Tooltip("What this step does.")]
        public StepType type = StepType.Wait;

        // ── Timing ────────────────────────────────────────────────────────────
        [Tooltip("Duration in seconds for Wait, Move, LookAt, FadeIn, FadeOut steps.")]
        [Min(0f)] public float duration = 1f;

        // ── Camera waypoints (MoveCamera / LookAt) ────────────────────────────
        [Tooltip("Start Transform for MoveCamera; unused by other types.")]
        public Transform fromWaypoint;

        [Tooltip("End Transform for MoveCamera; target Transform for LookAt.")]
        public Transform toWaypoint;

        [Tooltip("Easing curve applied to MoveCamera and LookAt interpolation. " +
                 "Leave as linear (0→1) for a simple dolly. " +
                 "Use ease-in-out for cinematic moves.")]
        public AnimationCurve easing = AnimationCurve.Linear(0f, 0f, 1f, 1f);

        // ── String param (PlayLine id / InvokeEvent name) ─────────────────────
        [Tooltip("PlayLine: the voiceline id (must match voicelines.csv). " +
                 "InvokeEvent: human-readable event name (documentation only).")]
        public string stringParam = string.Empty;

        // ── Beat (SetBeat) ────────────────────────────────────────────────────
        [Tooltip("Target beat for SetBeat steps.")]
        public Beat beatParam = Beat.Boarding;

        // ── UnityEvent (PlayLine / InvokeEvent) ───────────────────────────────
        /// <summary>
        /// Fired when a <see cref="StepType.PlayLine"/> or <see cref="StepType.InvokeEvent"/> step executes.
        /// Wire this in the Inspector — e.g. DialoguePlayer.PlayById — since EchoEvents has no PlayLine event.
        /// </summary>
        public UnityEvent onStepEvent = new UnityEvent();
    }
}


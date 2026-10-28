using UnityEngine;

namespace Echo.Core
{
    /// <summary>
    /// Pure C# alert accumulator — no MonoBehaviour, no UnityEngine dependency except Mathf.
    /// Lives entirely in Echo.Core so C-06 can unit-test it in EditMode without a scene.
    ///
    /// Usage:
    ///   var meter = new AlertMeter();
    ///   meter.Configure(hearingRadius, gainScale, decayPerSecond);
    ///   // Each noise event:
    ///   meter.Add(position, noiseLoudness, monsterPosition);
    ///   // Each Update:
    ///   meter.Tick(Time.deltaTime);
    ///   float v = meter.Value;
    /// </summary>
    public class AlertMeter
    {
        // ── Configuration (set once from GameTuning) ──────────────────────────
        private float _hearingRadius;
        private float _gainScale;
        private float _decayPerSecond;

        /// <summary>Current alert level in the range [0, ∞). Thresholds are compared against this.</summary>
        public float Value { get; private set; }

        /// <summary>
        /// Configures the meter with values from GameTuning.
        /// Call whenever GameTuning changes (or once in Awake).
        /// </summary>
        public void Configure(float hearingRadius, float gainScale, float decayPerSecond)
        {
            _hearingRadius    = hearingRadius;
            _gainScale        = gainScale;
            _decayPerSecond   = decayPerSecond;
        }

        /// <summary>
        /// Adds alert from a single noise event.
        /// Formula: effective = loudness × (1 − distance / hearingRadius), clamped ≥ 0.
        /// Noises outside the hearing radius or with zero radius configured contribute zero.
        /// Returns the effective amount added (useful for debug logging).
        /// </summary>
        public float Add(Vector3 noiseWorldPos, float loudness, Vector3 monsterWorldPos)
        {
            if (_hearingRadius <= 0f) return 0f;

            float distance  = Vector3.Distance(noiseWorldPos, monsterWorldPos);
            float falloff   = 1f - distance / _hearingRadius;
            float effective = Mathf.Max(0f, loudness * falloff) * _gainScale;
            Value += effective;
            return effective;
        }

        /// <summary>Decays the alert value over time. Call once per Update with Time.deltaTime.</summary>
        public void Tick(float deltaTime)
        {
            Value = Mathf.Max(0f, Value - _decayPerSecond * deltaTime);
        }

        /// <summary>Immediately sets alert to zero.</summary>
        public void Reset() => Value = 0f;

        /// <summary>True when the alert has reached or exceeded <paramref name="threshold"/>.</summary>
        public bool IsAbove(float threshold) => Value >= threshold;
    }
}


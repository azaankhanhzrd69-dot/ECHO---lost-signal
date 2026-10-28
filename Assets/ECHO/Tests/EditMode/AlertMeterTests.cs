// C-06 | Lane: Core | EditMode unit tests for AlertMeter.
// Tests are written against the REAL AlertMeter signatures — no mocking.
// Formula under test:
//   effective = Max(0, loudness * (1 - distance / hearingRadius)) * gainScale
//   Value += effective
//   Tick: Value = Max(0, Value - decayPerSecond * deltaTime)
//   IsAbove: Value >= threshold

using NUnit.Framework;
using UnityEngine;
using Echo.Core;

namespace Echo.Tests
{
    [TestFixture]
    public class AlertMeterTests
    {
        private AlertMeter _meter;

        private const float Tolerance = 0.0001f;
        private const float Radius    = 10f;
        private const float Gain      = 2f;
        private const float Decay     = 1f;

        [SetUp]
        public void SetUp()
        {
            _meter = new AlertMeter();
            _meter.Configure(Radius, Gain, Decay);
        }

        // ── Add ───────────────────────────────────────────────────────────────────

        [Test]
        public void Add_NoiseAtExactRadius_AddsNothing()
        {
            // distance == hearingRadius → falloff = 0 → effective = 0
            float added = _meter.Add(new Vector3(Radius, 0f, 0f), loudness: 1f, Vector3.zero);
            Assert.AreEqual(0f, added, Tolerance);
            Assert.AreEqual(0f, _meter.Value, Tolerance);
        }

        [Test]
        public void Add_NoiseOutsideRadius_AddsNothing()
        {
            float added = _meter.Add(new Vector3(Radius + 1f, 0f, 0f), loudness: 1f, Vector3.zero);
            Assert.AreEqual(0f, added, Tolerance);
            Assert.AreEqual(0f, _meter.Value, Tolerance);
        }

        [Test]
        public void Add_NoiseAtDistanceZero_AddsFullLoudnessTimesGain()
        {
            // distance = 0, falloff = 1 → effective = loudness * 1 * gainScale
            float loudness = 0.8f;
            float expected = loudness * 1f * Gain;
            float added = _meter.Add(Vector3.zero, loudness, Vector3.zero);
            Assert.AreEqual(expected, added, Tolerance);
            Assert.AreEqual(expected, _meter.Value, Tolerance);
        }

        [Test]
        public void Add_NoiseAtHalfRadius_AddsHalfFalloffTimesGain()
        {
            // distance = R/2 → falloff = 0.5 → effective = loudness * 0.5 * gainScale
            float loudness = 1f;
            float expected = loudness * 0.5f * Gain;
            float added = _meter.Add(new Vector3(Radius / 2f, 0f, 0f), loudness, Vector3.zero);
            Assert.AreEqual(expected, added, Tolerance);
            Assert.AreEqual(expected, _meter.Value, Tolerance);
        }

        [Test]
        public void Add_ZeroLoudness_AddsNothing()
        {
            float added = _meter.Add(Vector3.zero, loudness: 0f, Vector3.zero);
            Assert.AreEqual(0f, added, Tolerance);
            Assert.AreEqual(0f, _meter.Value, Tolerance);
        }

        [Test]
        public void Add_NegativeLoudness_AddsNothing()
        {
            // Max(0, negative * falloff) = 0
            float added = _meter.Add(Vector3.zero, loudness: -1f, Vector3.zero);
            Assert.AreEqual(0f, added, Tolerance);
            Assert.AreEqual(0f, _meter.Value, Tolerance);
        }

        [Test]
        public void Add_RepeatedNoises_Accumulate()
        {
            // Two noises at distance 0, loudness 1 → Value = 2 * Gain
            _meter.Add(Vector3.zero, 1f, Vector3.zero);
            _meter.Add(Vector3.zero, 1f, Vector3.zero);
            Assert.AreEqual(2f * Gain, _meter.Value, Tolerance);
        }

        [Test]
        public void Configure_ZeroRadius_AddsNothing()
        {
            var zeroMeter = new AlertMeter();
            zeroMeter.Configure(hearingRadius: 0f, gainScale: Gain, decayPerSecond: Decay);
            float added = zeroMeter.Add(Vector3.zero, loudness: 1f, Vector3.zero);
            Assert.AreEqual(0f, added, Tolerance);
            Assert.AreEqual(0f, zeroMeter.Value, Tolerance);
        }

        // ── Tick ─────────────────────────────────────────────────────────────────

        [Test]
        public void Tick_ReducesValueByDecayTimeDelta()
        {
            _meter.Add(Vector3.zero, 1f, Vector3.zero); // adds Gain = 2
            float before = _meter.Value;                 // 2
            float dt     = 0.5f;
            _meter.Tick(dt);
            float expected = before - Decay * dt;        // 2 - 0.5 = 1.5
            Assert.AreEqual(expected, _meter.Value, Tolerance);
        }

        [Test]
        public void Tick_NeverGoesBelow0()
        {
            // Start at 0, tick a large delta → must stay 0
            _meter.Tick(1000f);
            Assert.AreEqual(0f, _meter.Value, Tolerance);
        }

        [Test]
        public void Tick_ZeroDeltaTime_ChangesNothing()
        {
            _meter.Add(Vector3.zero, 1f, Vector3.zero);
            float before = _meter.Value;
            _meter.Tick(0f);
            Assert.AreEqual(before, _meter.Value, Tolerance);
        }

        [Test]
        public void Tick_DecayExceedsValue_ClampsToZero()
        {
            _meter.Add(Vector3.zero, 0.1f, Vector3.zero); // small value
            _meter.Tick(100f);                              // huge decay
            Assert.AreEqual(0f, _meter.Value, Tolerance);
        }

        // ── IsAbove ───────────────────────────────────────────────────────────────

        [Test]
        public void IsAbove_ExactlyAtThreshold_ReturnsTrue()
        {
            // IsAbove uses >= so exact threshold must return true
            _meter.Add(Vector3.zero, 1f, Vector3.zero); // Value = Gain = 2
            Assert.IsTrue(_meter.IsAbove(Gain));
        }

        [Test]
        public void IsAbove_BelowThreshold_ReturnsFalse()
        {
            Assert.IsFalse(_meter.IsAbove(0.001f));
        }

        [Test]
        public void IsAbove_AboveThreshold_ReturnsTrue()
        {
            _meter.Add(Vector3.zero, 1f, Vector3.zero); // Value = 2
            Assert.IsTrue(_meter.IsAbove(1f));
        }

        // ── Reset ────────────────────────────────────────────────────────────────

        [Test]
        public void Reset_SetsValueToZero()
        {
            _meter.Add(Vector3.zero, 1f, Vector3.zero);
            Assert.Greater(_meter.Value, 0f); // sanity
            _meter.Reset();
            Assert.AreEqual(0f, _meter.Value, Tolerance);
        }
    }
}


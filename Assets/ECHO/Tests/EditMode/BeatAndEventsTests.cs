// C-06 | Lane: Core | EditMode unit tests for beat ordering and EchoEvents contract.
// Pure C#: no MonoBehaviours, no scenes, no NavMesh.
//
// NOTE: MonsterAI state-machine transitions are NOT tested here.
// MonsterAI is a MonoBehaviour and requires a scene; its logic cannot be
// instantiated in EditMode. AlertMeter and HideRules cover the testable logic.

using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using Echo.Shared;

namespace Echo.Tests
{
    [TestFixture]
    public class BeatAndEventsTests
    {
        [SetUp]
        public void SetUp()
        {
            EchoEvents.ClearAll();
            Progress.Reset();
        }

        [TearDown]
        public void TearDown()
        {
            EchoEvents.ClearAll();
            Progress.Reset();
        }

        // ── Beat enum order (story contract) ─────────────────────────────────────
        // Changing the int value of any Beat breaks F1 jump, saved progress, and
        // every beat-gated system. Hard-code the expected order to catch accidents.

        [Test]
        public void BeatEnum_OrderMatchesStory()
        {
            Assert.AreEqual(0,  (int)Beat.Boarding,      "Boarding");
            Assert.AreEqual(1,  (int)Beat.Vent,          "Vent");
            Assert.AreEqual(2,  (int)Beat.Keypad,        "Keypad");
            Assert.AreEqual(3,  (int)Beat.MedBay,        "MedBay");
            Assert.AreEqual(4,  (int)Beat.Lunge,         "Lunge");
            Assert.AreEqual(5,  (int)Beat.HideIntro,     "HideIntro");
            Assert.AreEqual(6,  (int)Beat.Sneak,         "Sneak");
            Assert.AreEqual(7,  (int)Beat.ShuttleWreck,  "ShuttleWreck");
            Assert.AreEqual(8,  (int)Beat.ReactorEmpty,  "ReactorEmpty");
            Assert.AreEqual(9,  (int)Beat.CoreHunt,      "CoreHunt");
            Assert.AreEqual(10, (int)Beat.PowerOn,       "PowerOn");
            Assert.AreEqual(11, (int)Beat.Chase,         "Chase");
            Assert.AreEqual(12, (int)Beat.Airlock,       "Airlock");
            Assert.AreEqual(13, (int)Beat.Ending,        "Ending");
        }

        [Test]
        public void BeatEnum_Has14Values()
        {
            Assert.AreEqual(14, System.Enum.GetValues(typeof(Beat)).Length);
        }

        // ── EchoEvents.SetBeat ────────────────────────────────────────────────────

        [Test]
        public void SetBeat_SetsProgressCurrent()
        {
            EchoEvents.SetBeat(Beat.Sneak);
            Assert.AreEqual(Beat.Sneak, Progress.Current);
        }

        [Test]
        public void SetBeat_FiresBeatChangedOnce()
        {
            int callCount = 0;
            EchoEvents.BeatChanged += _ => callCount++;
            EchoEvents.SetBeat(Beat.CoreHunt);
            Assert.AreEqual(1, callCount);
        }

        [Test]
        public void SetBeat_FiresBeatChangedWithCorrectValue()
        {
            Beat received = Beat.Boarding; // default
            EchoEvents.BeatChanged += b => received = b;
            EchoEvents.SetBeat(Beat.Chase);
            Assert.AreEqual(Beat.Chase, received);
        }

        [Test]
        public void SetBeat_MultipleCalls_FiresEachTime()
        {
            var beats = new List<Beat>();
            EchoEvents.BeatChanged += b => beats.Add(b);
            EchoEvents.SetBeat(Beat.Vent);
            EchoEvents.SetBeat(Beat.Keypad);
            EchoEvents.SetBeat(Beat.MedBay);
            CollectionAssert.AreEqual(
                new[] { Beat.Vent, Beat.Keypad, Beat.MedBay },
                beats);
        }

        // ── EchoEvents.MakeNoise ─────────────────────────────────────────────────

        [Test]
        public void MakeNoise_FiresNoiseWithCorrectPositionAndLoudness()
        {
            Vector3 receivedPos  = Vector3.zero;
            float   receivedLoud = -1f;
            EchoEvents.Noise += (p, l) => { receivedPos = p; receivedLoud = l; };

            var pos  = new Vector3(1f, 2f, 3f);
            float loud = 0.75f;
            EchoEvents.MakeNoise(pos, loud);

            Assert.AreEqual(pos,  receivedPos,  "position mismatch");
            Assert.AreEqual(loud, receivedLoud, 0.0001f, "loudness mismatch");
        }

        [Test]
        public void MakeNoise_WithNoListeners_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => EchoEvents.MakeNoise(Vector3.zero, 1f));
        }

        // ── EchoEvents.EndHide ────────────────────────────────────────────────────

        [Test]
        public void EndHide_True_FiresHideFinishedTrue()
        {
            bool? result = null;
            EchoEvents.HideFinished += ok => result = ok;
            EchoEvents.EndHide(true);
            Assert.IsTrue(result.HasValue && result.Value);
        }

        [Test]
        public void EndHide_False_FiresHideFinishedFalse()
        {
            bool? result = null;
            EchoEvents.HideFinished += ok => result = ok;
            EchoEvents.EndHide(false);
            Assert.IsTrue(result.HasValue && !result.Value);
        }

        // ── EchoEvents.ClearAll ───────────────────────────────────────────────────

        [Test]
        public void ClearAll_NullifiesSubscribers_NoFireAfterClear()
        {
            int count = 0;
            EchoEvents.BeatChanged += _ => count++;
            EchoEvents.ClearAll();
            EchoEvents.SetBeat(Beat.Vent); // should NOT fire cleared handler
            Assert.AreEqual(0, count);
        }

        [Test]
        public void ClearAll_ResetsInputLocked()
        {
            EchoEvents.LockInput();
            Assert.IsTrue(EchoEvents.InputLocked); // sanity
            EchoEvents.ClearAll();
            Assert.IsFalse(EchoEvents.InputLocked);
        }

        // ── Progress.Reset ────────────────────────────────────────────────────────

        [Test]
        public void Progress_Reset_SetsCurrentToBoarding()
        {
            Progress.Current = Beat.Chase;
            Progress.Reset();
            Assert.AreEqual(Beat.Boarding, Progress.Current);
        }

        [Test]
        public void Progress_Reset_ZerosCoresFound()
        {
            Progress.CoresFound = 4;
            Progress.Reset();
            Assert.AreEqual(0, Progress.CoresFound);
        }

        [Test]
        public void Progress_Reset_ZerosSafeHides()
        {
            Progress.SafeHides = 2;
            Progress.Reset();
            Assert.AreEqual(0, Progress.SafeHides);
        }

        [Test]
        public void Progress_Reset_FalsesBoolFields()
        {
            Progress.HasMap   = true;
            Progress.HasNote  = true;
            Progress.MorseSent = true;
            Progress.Reset();
            Assert.IsFalse(Progress.HasMap,    "HasMap");
            Assert.IsFalse(Progress.HasNote,   "HasNote");
            Assert.IsFalse(Progress.MorseSent, "MorseSent");
        }

        // ── InputLock ─────────────────────────────────────────────────────────────

        [Test]
        public void LockInput_SetsInputLockedTrue()
        {
            EchoEvents.LockInput();
            Assert.IsTrue(EchoEvents.InputLocked);
        }

        [Test]
        public void UnlockInput_SetsInputLockedFalse()
        {
            EchoEvents.LockInput();
            EchoEvents.UnlockInput();
            Assert.IsFalse(EchoEvents.InputLocked);
        }
    }
}


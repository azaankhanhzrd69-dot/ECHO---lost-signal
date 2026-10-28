using NUnit.Framework;
using Echo.Core;
using Echo.Shared;
using CoreProgressSnapshot = Echo.Core.ProgressSnapshot;

namespace Echo.Tests
{
    [TestFixture]
    public class ProgressSnapshotTests
    {
        [SetUp]
        public void SetUp()
        {
            Progress.Reset();
        }

        [TearDown]
        public void TearDown()
        {
            Progress.Reset();
        }

        [Test]
        public void Capture_CapturesEveryFieldAccurately()
        {
            Progress.Current = Beat.CoreHunt;
            Progress.CoresFound = 2;
            Progress.SafeHides = 1;
            Progress.HasMap = true;
            Progress.HasNote = true;
            Progress.MorseSent = false;

            var snapshot = CoreProgressSnapshot.Capture();

            Assert.AreEqual(Beat.CoreHunt, snapshot.Current);
            Assert.AreEqual(2, snapshot.CoresFound);
            Assert.AreEqual(1, snapshot.SafeHides);
            Assert.IsTrue(snapshot.HasMap);
            Assert.IsTrue(snapshot.HasNote);
            Assert.IsFalse(snapshot.MorseSent);
        }

        [Test]
        public void Restore_RestoresAllFieldsToCapturedState()
        {
            Progress.Current = Beat.Sneak;
            Progress.CoresFound = 1;
            Progress.SafeHides = 2;
            Progress.HasMap = true;
            Progress.HasNote = false;
            Progress.MorseSent = false;

            var snapshot = CoreProgressSnapshot.Capture();

            // Alter Progress state
            Progress.Current = Beat.Ending;
            Progress.CoresFound = 3;
            Progress.SafeHides = 3;
            Progress.HasMap = false;
            Progress.HasNote = true;
            Progress.MorseSent = true;

            // Restore snapshot
            snapshot.Restore();

            Assert.AreEqual(Beat.Sneak, Progress.Current);
            Assert.AreEqual(1, Progress.CoresFound);
            Assert.AreEqual(2, Progress.SafeHides);
            Assert.IsTrue(Progress.HasMap);
            Assert.IsFalse(Progress.HasNote);
            Assert.IsFalse(Progress.MorseSent);
        }

        [Test]
        public void IndependenceOfTwoSnapshots()
        {
            Progress.Current = Beat.Boarding;
            Progress.CoresFound = 0;
            var snap1 = CoreProgressSnapshot.Capture();

            Progress.Current = Beat.PowerOn;
            Progress.CoresFound = 3;
            var snap2 = CoreProgressSnapshot.Capture();

            snap1.Restore();
            Assert.AreEqual(Beat.Boarding, Progress.Current);
            Assert.AreEqual(0, Progress.CoresFound);

            snap2.Restore();
            Assert.AreEqual(Beat.PowerOn, Progress.Current);
            Assert.AreEqual(3, Progress.CoresFound);
        }
    }
}


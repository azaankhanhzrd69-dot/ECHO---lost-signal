using NUnit.Framework;
using Echo.Core;

namespace Echo.Tests
{
    [TestFixture]
    public class EndingRulesTests
    {
        [Test]
        public void Evaluate_MorseSentFalse_ReturnsGoodOnly()
        {
            EndingPlan plan = EndingRules.Evaluate(morseSent: false);

            Assert.IsTrue(plan.HasGood, "Good ending must always be true.");
            Assert.IsFalse(plan.HasSecret, "Secret ending must be false when Morse was not sent.");
            Assert.AreEqual(EndingOrder.GoodThenSecret, plan.Order);
        }

        [Test]
        public void Evaluate_MorseSentTrue_ReturnsGoodAndSecret()
        {
            EndingPlan plan = EndingRules.Evaluate(morseSent: true);

            Assert.IsTrue(plan.HasGood, "Good ending must always be true.");
            Assert.IsTrue(plan.HasSecret, "Secret ending must be true when Morse was sent.");
            Assert.AreEqual(EndingOrder.GoodThenSecret, plan.Order);
        }

        [TestCase(true)]
        [TestCase(false)]
        public void Evaluate_GoodEndingAlwaysPresent(bool morseSent)
        {
            EndingPlan plan = EndingRules.Evaluate(morseSent);
            Assert.IsTrue(plan.HasGood);
        }
    }
}


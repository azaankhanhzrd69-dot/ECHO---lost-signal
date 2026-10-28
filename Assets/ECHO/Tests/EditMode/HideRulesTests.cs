// C-06 | Lane: Core | EditMode unit tests for HideRules.
// Tests are written against the REAL HideRules.Evaluate signature.
//
// Rules under test:
//   Table or Chair (any context)                               → InstantKill
//   Locker + isIntro=true  (any safeHidesSoFar)               → Normal
//   Locker + isIntro=false + safeHidesSoFar <  limit          → Normal
//   Locker + isIntro=false + safeHidesSoFar >= limit          → ForcedDeath
//
// KEY STORY RULE: With default limit=3 (safeHidesBeforeDeath),
//   hides 1-3 → Normal, hide 4 (safeHidesSoFar=3) → ForcedDeath.

using NUnit.Framework;
using Echo.Core;

namespace Echo.Tests
{
    [TestFixture]
    public class HideRulesTests
    {
        // ── Table / Chair always InstantKill ─────────────────────────────────────

        [TestCase(HideKind.Table, false, 0, 3)]
        [TestCase(HideKind.Table, true,  0, 3)]
        [TestCase(HideKind.Table, false, 5, 3)]
        [TestCase(HideKind.Chair, false, 0, 3)]
        [TestCase(HideKind.Chair, true,  0, 3)]
        [TestCase(HideKind.Chair, false, 5, 3)]
        public void Evaluate_TableOrChair_AlwaysInstantKill(
            HideKind kind, bool isIntro, int safeHidesSoFar, int limit)
        {
            Assert.AreEqual(
                HideOutcome.InstantKill,
                HideRules.Evaluate(kind, isIntro, safeHidesSoFar, limit));
        }

        // ── Locker in the intro → always Normal ───────────────────────────────────

        [TestCase(0, 3)]
        [TestCase(3, 3)]
        [TestCase(99, 3)]
        [TestCase(0, 0)]
        public void Evaluate_Locker_Intro_IsNormal(int safeHidesSoFar, int limit)
        {
            Assert.AreEqual(
                HideOutcome.Normal,
                HideRules.Evaluate(HideKind.Locker, isIntro: true, safeHidesSoFar, limit));
        }

        // ── Locker after intro, below limit → Normal ──────────────────────────────

        [TestCase(0, 3)]
        [TestCase(1, 3)]
        [TestCase(2, 3)]
        [TestCase(0, 1)]
        public void Evaluate_Locker_NotIntro_BelowLimit_IsNormal(int safeHidesSoFar, int limit)
        {
            Assert.AreEqual(
                HideOutcome.Normal,
                HideRules.Evaluate(HideKind.Locker, isIntro: false, safeHidesSoFar, limit));
        }

        // ── Locker after intro, AT limit → ForcedDeath ────────────────────────────
        // Story: 4th hide (safeHidesSoFar == 3 with limit 3) → ForcedDeath.

        [Test]
        public void Evaluate_Locker_NotIntro_AtDefaultLimit_IsForcedDeath()
        {
            // Default story rule: safeHidesBeforeDeath = 3.
            // Player has survived 3 hides (hides 1-3 were Normal).
            // The 4th attempt → ForcedDeath.
            Assert.AreEqual(
                HideOutcome.ForcedDeath,
                HideRules.Evaluate(HideKind.Locker, isIntro: false, safeHidesSoFar: 3, safeHidesBeforeDeath: 3));
        }

        // ── Locker after intro, above limit → ForcedDeath ────────────────────────

        [TestCase(3, 3)]
        [TestCase(4, 3)]
        [TestCase(1, 1)]
        [TestCase(10, 3)]
        public void Evaluate_Locker_NotIntro_AtOrAboveLimit_IsForcedDeath(int safeHidesSoFar, int limit)
        {
            Assert.AreEqual(
                HideOutcome.ForcedDeath,
                HideRules.Evaluate(HideKind.Locker, isIntro: false, safeHidesSoFar, limit));
        }

        // ── Limit=0 edge case ────────────────────────────────────────────────────
        // If safeHidesBeforeDeath = 0, even the first post-intro hide is ForcedDeath.

        [Test]
        public void Evaluate_Locker_Limit0_FirstHide_IsForcedDeath()
        {
            Assert.AreEqual(
                HideOutcome.ForcedDeath,
                HideRules.Evaluate(HideKind.Locker, isIntro: false, safeHidesSoFar: 0, safeHidesBeforeDeath: 0));
        }

        // ── Limit=1 boundary ─────────────────────────────────────────────────────

        [Test]
        public void Evaluate_Locker_Limit1_ZeroHides_IsNormal()
        {
            Assert.AreEqual(
                HideOutcome.Normal,
                HideRules.Evaluate(HideKind.Locker, isIntro: false, safeHidesSoFar: 0, safeHidesBeforeDeath: 1));
        }

        [Test]
        public void Evaluate_Locker_Limit1_OneHide_IsForcedDeath()
        {
            Assert.AreEqual(
                HideOutcome.ForcedDeath,
                HideRules.Evaluate(HideKind.Locker, isIntro: false, safeHidesSoFar: 1, safeHidesBeforeDeath: 1));
        }
    }
}


using NUnit.Framework;
using Echo.Interact;

namespace Echo.Tests
{
    [TestFixture]
    public class MorseValidatorTests
    {
        // ── Encode ────────────────────────────────────────────────────────────

        [Test]
        public void Encode_SimpleSOS_ReturnsThreeDotsThreeDashesThreeDots()
        {
            string result = MorseValidator.Encode("SOS");
            Assert.AreEqual("... --- ...", result);
        }

        [Test]
        public void Encode_WithWords_SeparatesWithSlash()
        {
            string result = MorseValidator.Encode("SOS MAYDAY");
            Assert.AreEqual("... --- ... / -- .- -.-- -.. .- -.--", result);
        }

        [Test]
        public void Encode_EmptyOrNull_ReturnsEmptyString()
        {
            Assert.AreEqual(string.Empty, MorseValidator.Encode(null));
            Assert.AreEqual(string.Empty, MorseValidator.Encode(""));
            Assert.AreEqual(string.Empty, MorseValidator.Encode("   "));
        }

        // ── Decode ────────────────────────────────────────────────────────────

        [Test]
        public void Decode_SimpleSOS_ReturnsSOS()
        {
            string result = MorseValidator.Decode("... --- ...");
            Assert.AreEqual("SOS", result);
        }

        [Test]
        public void Decode_WordsWithSlash_RestoresSpaces()
        {
            string result = MorseValidator.Decode("... --- ... / -- .- -.-- -.. .- -.--");
            Assert.AreEqual("SOS MAYDAY", result);
        }

        [Test]
        public void Decode_EmptyOrNull_ReturnsEmptyString()
        {
            Assert.AreEqual(string.Empty, MorseValidator.Decode(null));
            Assert.AreEqual(string.Empty, MorseValidator.Decode(""));
            Assert.AreEqual(string.Empty, MorseValidator.Decode("   "));
        }

        [Test]
        public void Decode_InvalidSequences_DoesNotThrowAndSkipsGracefully()
        {
            Assert.DoesNotThrow(() =>
            {
                string res = MorseValidator.Decode("... --- ??? ... --- ... -----....");
                Assert.IsNotNull(res);
            });
        }

        // ── Matches ───────────────────────────────────────────────────────────

        [Test]
        public void Matches_ExactSOS_ReturnsTrue()
        {
            bool matches = MorseValidator.Matches("... --- ...", "SOS");
            Assert.IsTrue(matches);
        }

        [Test]
        public void Matches_SOS_ExtraWhitespace_ReturnsTrue()
        {
            // Extra spacing between letters or leading/trailing whitespace
            bool matches = MorseValidator.Matches("   ...    ---    ...   ", "SOS");
            Assert.IsTrue(matches);
        }

        [Test]
        public void Matches_CaseInsensitive_ReturnsTrue()
        {
            bool matchesUpper = MorseValidator.Matches("... --- ...", "SOS");
            bool matchesLower = MorseValidator.Matches("... --- ...", "sos");
            Assert.IsTrue(matchesUpper);
            Assert.IsTrue(matchesLower);
        }

        [Test]
        public void Matches_WrongMorse_ReturnsFalse()
        {
            bool matches = MorseValidator.Matches("... ... ...", "SOS"); // SSS != SOS
            Assert.IsFalse(matches);
        }

        [Test]
        public void Matches_EmptyOrWhitespaceInput_ReturnsFalse()
        {
            Assert.IsFalse(MorseValidator.Matches("", "SOS"));
            Assert.IsFalse(MorseValidator.Matches("   ", "SOS"));
            Assert.IsFalse(MorseValidator.Matches(null, "SOS"));
        }

        [Test]
        public void Matches_NullOrEmptyExpected_ReturnsFalse()
        {
            Assert.IsFalse(MorseValidator.Matches("... --- ...", ""));
            Assert.IsFalse(MorseValidator.Matches("... --- ...", null));
        }

        [Test]
        public void Matches_Numbers_DecodesAndMatchesCorrectly()
        {
            // 1 4 2 : .---- ....- ..---
            string morse142 = ".---- ....- ..---";
            Assert.IsTrue(MorseValidator.Matches(morse142, "142"));
        }
    }
}


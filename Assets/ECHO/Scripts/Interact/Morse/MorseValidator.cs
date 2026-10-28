using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace Echo.Interact
{
    /// <summary>
    /// Pure C# International Morse code encoder, decoder, and validator.
    /// Supports characters A-Z, 0-9. Letters are separated by spaces (' '), words by slashes ('/').
    /// Designed to never throw exceptions on invalid input.
    /// </summary>
    public static class MorseValidator
    {
        private static readonly Dictionary<char, string> CharToMorse = new Dictionary<char, string>
        {
            {'A', ".-"},    {'B', "-..."},  {'C', "-.-."},  {'D', "-.."},
            {'E', "."},     {'F', "..-."},  {'G', "--."},   {'H', "...."},
            {'I', ".."},    {'J', ".---"},  {'K', "-.-"},   {'L', ".-.."},
            {'M', "--"},    {'N', "-."},    {'O', "---"},   {'P', ".--."},
            {'Q', "--.-"},  {'R', ".-."},   {'S', "..."},   {'T', "-"},
            {'U', "..-"},   {'V', "...-"},  {'W', ".--"},   {'X', "-..-"},
            {'Y', "-.--"},  {'Z', "--.."},
            {'0', "-----"}, {'1', ".----"}, {'2', "..---"}, {'3', "...--"},
            {'4', "....-"}, {'5', "....."}, {'6', "-...."}, {'7', "--..."},
            {'8', "---.."}, {'9', "----."}
        };

        private static readonly Dictionary<string, char> MorseToChar = new Dictionary<string, char>();

        static MorseValidator()
        {
            foreach (var kvp in CharToMorse)
            {
                MorseToChar[kvp.Value] = kvp.Key;
            }
        }

        /// <summary>
        /// Encodes plain text into International Morse code.
        /// Letters are separated by a single space, words by " / ".
        /// Unknown characters are skipped.
        /// </summary>
        public static string Encode(string text)
        {
            if (string.IsNullOrEmpty(text))
                return string.Empty;

            text = text.Trim().ToUpperInvariant();
            var words = Regex.Split(text, @"\s+");
            var wordResults = new List<string>(words.Length);

            foreach (var word in words)
            {
                var letterResults = new List<string>(word.Length);
                foreach (char c in word)
                {
                    if (CharToMorse.TryGetValue(c, out string morse))
                    {
                        letterResults.Add(morse);
                    }
                }

                if (letterResults.Count > 0)
                {
                    wordResults.Add(string.Join(" ", letterResults));
                }
            }

            return string.Join(" / ", wordResults);
        }

        /// <summary>
        /// Decodes an International Morse code string into plain text.
        /// Supports '.' and '-', spaces between letters, and '/' (or multiple spaces) between words.
        /// Unrecognized sequences produce '?' or are gracefully handled without throwing.
        /// </summary>
        public static string Decode(string morse)
        {
            if (string.IsNullOrEmpty(morse))
                return string.Empty;

            // Normalize spaces and slashes
            string cleaned = morse.Trim();
            if (cleaned.Length == 0)
                return string.Empty;

            // Split by words first (separated by '/')
            string[] wordTokens = cleaned.Split('/');
            var sb = new StringBuilder();

            for (int w = 0; w < wordTokens.Length; w++)
            {
                string wordToken = wordTokens[w].Trim();
                if (wordToken.Length == 0)
                    continue;

                // Within each word, split letters by whitespace
                string[] letterTokens = Regex.Split(wordToken, @"\s+");
                foreach (string letter in letterTokens)
                {
                    if (string.IsNullOrEmpty(letter))
                        continue;

                    if (MorseToChar.TryGetValue(letter, out char c))
                    {
                        sb.Append(c);
                    }
                    else
                    {
                        // Unknown Morse letter pattern: return empty or skip
                        // To avoid throwing, simply ignore or append nothing
                    }
                }

                if (w < wordTokens.Length - 1 && sb.Length > 0 && sb[sb.Length - 1] != ' ')
                {
                    sb.Append(' ');
                }
            }

            return sb.ToString().Trim();
        }

        /// <summary>
        /// Checks if the input Morse code matches the expected plain text message.
        /// Handles whitespace variance, trailing spaces, and case-insensitivity.
        /// Returns false if input is empty or invalid.
        /// </summary>
        public static bool Matches(string input, string expectedText)
        {
            if (string.IsNullOrWhiteSpace(input) || string.IsNullOrWhiteSpace(expectedText))
                return false;

            string decoded = Decode(input);
            if (string.IsNullOrEmpty(decoded))
                return false;

            return string.Equals(decoded.Trim(), expectedText.Trim(), StringComparison.OrdinalIgnoreCase);
        }
    }
}


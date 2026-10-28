namespace Echo.Interact
{
    /// <summary>
    /// Pure logic validator for keypad inputs. Decoupled from Unity for testability.
    /// </summary>
    public static class KeypadLogic
    {
        /// <summary>
        /// Validates entered code against target code.
        /// Returns true if non-null and exact match.
        /// </summary>
        public static bool Check(string entered, string correct)
        {
            if (string.IsNullOrEmpty(entered) || string.IsNullOrEmpty(correct))
                return false;

            return entered.Trim() == correct.Trim();
        }
    }
}


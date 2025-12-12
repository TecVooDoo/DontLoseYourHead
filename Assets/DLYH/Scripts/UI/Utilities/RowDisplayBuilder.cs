using System;
using System.Text;
using UnityEngine;

namespace TecVooDoo.DontLoseYourHead.UI.Utilities
{
    /// <summary>
    /// Data required to build a word pattern row display string.
    /// </summary>
    public struct RowDisplayData
    {
        /// <summary>Row number (1-based) for display prefix</summary>
        public int RowNumber;

        /// <summary>Separator between row number and letters (e.g., ". ")</summary>
        public string NumberSeparator;

        /// <summary>Separator between individual letters (e.g., ' ')</summary>
        public char LetterSeparator;

        /// <summary>Character to show for unknown/hidden letters (e.g., '_')</summary>
        public char UnknownLetterChar;

        /// <summary>Current state of the row</summary>
        public RowState State;

        /// <summary>The complete word (for WordEntered/Placed/Gameplay states)</summary>
        public string CurrentWord;

        /// <summary>Text being entered (for Entering state)</summary>
        public string EnteredText;

        /// <summary>Required word length for this row</summary>
        public int RequiredLength;

        /// <summary>Array indicating which positions have revealed letters (Gameplay)</summary>
        public bool[] RevealedLetters;

        /// <summary>Whether word guess mode is currently active</summary>
        public bool InWordGuessMode;

        /// <summary>Callback to get guessed letter at position (for word guess mode)</summary>
        public Func<int, char> GetGuessedLetterAt;

        /// <summary>Color for player-typed guess letters (as hex string without #)</summary>
        public string GuessTypedLetterColorHex;

        /// <summary>
        /// Row state enum (matches WordPatternRow.RowState)
        /// </summary>
        public enum RowState
        {
            Empty,
            Entering,
            WordEntered,
            Placed,
            Gameplay
        }
    }

    /// <summary>
    /// Static utility class for building word pattern row display text.
    /// Pure functions with no side effects - easy to test and maintain.
    /// </summary>
    public static class RowDisplayBuilder
    {
        // Reusable StringBuilder to avoid allocations on repeated calls
        private static readonly StringBuilder SharedBuilder = new StringBuilder(64);

        /// <summary>
        /// Builds the complete display text for a word pattern row.
        /// </summary>
        /// <param name="data">Display data containing all required information</param>
        /// <returns>Formatted display string with rich text tags</returns>
        public static string Build(RowDisplayData data)
        {
            SharedBuilder.Clear();

            // Row number prefix
            SharedBuilder.Append(data.RowNumber);
            SharedBuilder.Append(data.NumberSeparator);

            // Build content based on state
            switch (data.State)
            {
                case RowDisplayData.RowState.Empty:
                    BuildEmptyState(data);
                    break;

                case RowDisplayData.RowState.Entering:
                    BuildEnteringState(data);
                    break;

                case RowDisplayData.RowState.WordEntered:
                case RowDisplayData.RowState.Placed:
                    BuildWordEnteredState(data);
                    break;

                case RowDisplayData.RowState.Gameplay:
                    BuildGameplayState(data);
                    break;
            }

            return SharedBuilder.ToString();
        }

        /// <summary>
        /// Builds display for Empty state - all underscores.
        /// </summary>
        private static void BuildEmptyState(RowDisplayData data)
        {
            for (int i = 0; i < data.RequiredLength; i++)
            {
                if (i > 0) SharedBuilder.Append(data.LetterSeparator);
                SharedBuilder.Append(data.UnknownLetterChar);
            }
        }

        /// <summary>
        /// Builds display for Entering state - typed letters underlined, rest as underscores.
        /// </summary>
        private static void BuildEnteringState(RowDisplayData data)
        {
            int enteredLength = string.IsNullOrEmpty(data.EnteredText) ? 0 : data.EnteredText.Length;

            for (int i = 0; i < data.RequiredLength; i++)
            {
                if (i > 0) SharedBuilder.Append(data.LetterSeparator);

                if (i < enteredLength)
                {
                    SharedBuilder.Append("<u>");
                    SharedBuilder.Append(data.EnteredText[i]);
                    SharedBuilder.Append("</u>");
                }
                else
                {
                    SharedBuilder.Append(data.UnknownLetterChar);
                }
            }
        }

        /// <summary>
        /// Builds display for WordEntered/Placed states - all letters underlined.
        /// </summary>
        private static void BuildWordEnteredState(RowDisplayData data)
        {
            if (string.IsNullOrEmpty(data.CurrentWord)) return;

            for (int i = 0; i < data.CurrentWord.Length; i++)
            {
                if (i > 0) SharedBuilder.Append(data.LetterSeparator);
                SharedBuilder.Append("<u>");
                SharedBuilder.Append(data.CurrentWord[i]);
                SharedBuilder.Append("</u>");
            }
        }

        /// <summary>
        /// Builds display for Gameplay state - revealed letters, guessed letters (colored), or underscores.
        /// </summary>
        private static void BuildGameplayState(RowDisplayData data)
        {
            if (string.IsNullOrEmpty(data.CurrentWord)) return;

            for (int i = 0; i < data.CurrentWord.Length; i++)
            {
                if (i > 0) SharedBuilder.Append(data.LetterSeparator);

                bool isRevealed = data.RevealedLetters != null &&
                                  i < data.RevealedLetters.Length &&
                                  data.RevealedLetters[i];

                if (isRevealed)
                {
                    // Revealed letter - show underlined
                    SharedBuilder.Append("<u>");
                    SharedBuilder.Append(data.CurrentWord[i]);
                    SharedBuilder.Append("</u>");
                }
                else if (data.InWordGuessMode && data.GetGuessedLetterAt != null)
                {
                    char guessedLetter = data.GetGuessedLetterAt(i);
                    if (guessedLetter != '\0')
                    {
                        // Guessed letter - show in colored text
                        SharedBuilder.Append("<color=#");
                        SharedBuilder.Append(data.GuessTypedLetterColorHex);
                        SharedBuilder.Append(">");
                        SharedBuilder.Append(guessedLetter);
                        SharedBuilder.Append("</color>");
                    }
                    else
                    {
                        // Not guessed yet - show underscore
                        SharedBuilder.Append(data.UnknownLetterChar);
                    }
                }
                else
                {
                    // Not in guess mode or no callback - show underscore
                    SharedBuilder.Append(data.UnknownLetterChar);
                }
            }
        }
    }
}
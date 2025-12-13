// LetterFrequency.cs
// Static reference data for English letter frequencies
// Created: December 13, 2025
// Developer: TecVooDoo LLC

using System.Collections.Generic;

namespace DLYH.AI.Data
{
    /// <summary>
    /// Static class containing English letter frequency data.
    /// Used by the AI to prioritize letter guesses based on how commonly
    /// letters appear in English words.
    /// 
    /// Frequencies based on analysis of English text corpora.
    /// Values represent percentage of letter occurrences (e.g., E = 12.7%).
    /// </summary>
    public static class LetterFrequency
    {
        // ============================================================
        // FREQUENCY DATA
        // ============================================================

        /// <summary>
        /// Letter frequencies as percentages (0-100 scale for readability).
        /// E appears in ~12.7% of letters in English text.
        /// </summary>
        private static readonly Dictionary<char, float> FrequencyData = new Dictionary<char, float>
        {
            { 'A', 8.2f },
            { 'B', 1.5f },
            { 'C', 2.8f },
            { 'D', 4.3f },
            { 'E', 12.7f },
            { 'F', 2.2f },
            { 'G', 2.0f },
            { 'H', 6.1f },
            { 'I', 7.0f },
            { 'J', 0.15f },
            { 'K', 0.77f },
            { 'L', 4.0f },
            { 'M', 2.4f },
            { 'N', 6.7f },
            { 'O', 7.5f },
            { 'P', 1.9f },
            { 'Q', 0.095f },
            { 'R', 6.0f },
            { 'S', 6.3f },
            { 'T', 9.1f },
            { 'U', 2.8f },
            { 'V', 0.98f },
            { 'W', 2.4f },
            { 'X', 0.15f },
            { 'Y', 2.0f },
            { 'Z', 0.074f }
        };

        /// <summary>
        /// Letters sorted by frequency (most common first).
        /// Useful for AI to iterate through in optimal order.
        /// </summary>
        public static readonly char[] LettersByFrequency = new char[]
        {
            'E', 'T', 'A', 'O', 'I', 'N', 'S', 'H', 'R', 'D',
            'L', 'C', 'U', 'M', 'W', 'F', 'G', 'Y', 'P', 'B',
            'V', 'K', 'J', 'X', 'Q', 'Z'
        };

        /// <summary>
        /// Common vowels in frequency order.
        /// </summary>
        public static readonly char[] Vowels = new char[] { 'E', 'A', 'O', 'I', 'U' };

        /// <summary>
        /// Common consonants in frequency order.
        /// </summary>
        public static readonly char[] ConsonantsByFrequency = new char[]
        {
            'T', 'N', 'S', 'H', 'R', 'D', 'L', 'C', 'M', 'W',
            'F', 'G', 'Y', 'P', 'B', 'V', 'K', 'J', 'X', 'Q', 'Z'
        };

        // ============================================================
        // PUBLIC METHODS
        // ============================================================

        /// <summary>
        /// Gets the frequency percentage for a given letter.
        /// </summary>
        /// <param name="letter">The letter to look up (case-insensitive)</param>
        /// <returns>Frequency as a percentage (0-12.7 range), or 0 if not a letter</returns>
        public static float GetFrequency(char letter)
        {
            char upperLetter = char.ToUpper(letter);

            if (FrequencyData.TryGetValue(upperLetter, out float frequency))
            {
                return frequency;
            }

            return 0f;
        }

        /// <summary>
        /// Gets the normalized frequency for a given letter (0-1 scale).
        /// Useful for weighting calculations.
        /// </summary>
        /// <param name="letter">The letter to look up (case-insensitive)</param>
        /// <returns>Normalized frequency (0-1), where 1 = most common (E)</returns>
        public static float GetNormalizedFrequency(char letter)
        {
            // E is the most common at 12.7%, so normalize to that
            const float MAX_FREQUENCY = 12.7f;
            return GetFrequency(letter) / MAX_FREQUENCY;
        }

        /// <summary>
        /// Gets the rank of a letter by frequency (1 = most common, 26 = least common).
        /// </summary>
        /// <param name="letter">The letter to look up (case-insensitive)</param>
        /// <returns>Rank from 1-26, or -1 if not a valid letter</returns>
        public static int GetFrequencyRank(char letter)
        {
            char upperLetter = char.ToUpper(letter);

            for (int i = 0; i < LettersByFrequency.Length; i++)
            {
                if (LettersByFrequency[i] == upperLetter)
                {
                    return i + 1;
                }
            }

            return -1;
        }

        /// <summary>
        /// Checks if a character is a vowel.
        /// </summary>
        /// <param name="letter">The character to check</param>
        /// <returns>True if the character is a vowel (A, E, I, O, U)</returns>
        public static bool IsVowel(char letter)
        {
            char upperLetter = char.ToUpper(letter);
            return upperLetter == 'A' || upperLetter == 'E' ||
                   upperLetter == 'I' || upperLetter == 'O' ||
                   upperLetter == 'U';
        }

        /// <summary>
        /// Checks if a character is a consonant.
        /// </summary>
        /// <param name="letter">The character to check</param>
        /// <returns>True if the character is a consonant</returns>
        public static bool IsConsonant(char letter)
        {
            char upperLetter = char.ToUpper(letter);
            return upperLetter >= 'A' && upperLetter <= 'Z' && !IsVowel(upperLetter);
        }

        /// <summary>
        /// Gets the total frequency of a set of letters.
        /// Useful for calculating coverage of remaining possibilities.
        /// </summary>
        /// <param name="letters">Collection of letters to sum</param>
        /// <returns>Combined frequency percentage</returns>
        public static float GetCombinedFrequency(IEnumerable<char> letters)
        {
            float total = 0f;

            foreach (char letter in letters)
            {
                total += GetFrequency(letter);
            }

            return total;
        }

        /// <summary>
        /// Gets letters that haven't been guessed yet, sorted by frequency.
        /// </summary>
        /// <param name="guessedLetters">Set of already guessed letters</param>
        /// <returns>Array of unguessed letters in frequency order (most common first)</returns>
        public static char[] GetUnguessedLettersByFrequency(HashSet<char> guessedLetters)
        {
            List<char> unguessed = new List<char>(26);

            foreach (char letter in LettersByFrequency)
            {
                if (!guessedLetters.Contains(letter) && !guessedLetters.Contains(char.ToLower(letter)))
                {
                    unguessed.Add(letter);
                }
            }

            return unguessed.ToArray();
        }

        /// <summary>
        /// Calculates a score for a letter based on frequency and pattern matching potential.
        /// </summary>
        /// <param name="letter">The letter to score</param>
        /// <param name="patternBonus">Additional bonus from pattern analysis (0+)</param>
        /// <returns>Combined score for letter selection</returns>
        public static float CalculateLetterScore(char letter, float patternBonus)
        {
            float baseScore = GetFrequency(letter);
            return baseScore + patternBonus;
        }
    }
}

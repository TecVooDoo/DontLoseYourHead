// LetterGuessStrategy.cs
// AI strategy for guessing letters based on frequency and pattern analysis
// Created: December 13, 2025
// Developer: TecVooDoo LLC

using System.Collections.Generic;
using UnityEngine;
using DLYH.AI.Config;
using DLYH.AI.Data;

namespace DLYH.AI.Strategies
{
    /// <summary>
    /// Strategy for selecting which letter to guess.
    /// 
    /// Uses English letter frequency as a base, then applies bonuses for:
    /// - Letters that could complete partially revealed word patterns
    /// 
    /// Skill level affects selection pool size:
    /// - High skill: Always picks optimal letter
    /// - Low skill: Picks randomly from top N candidates
    /// </summary>
    public class LetterGuessStrategy : IGuessStrategy
    {
        // ============================================================
        // CONSTANTS
        // ============================================================

        /// <summary>
        /// Weight multiplier for pattern completion bonus.
        /// </summary>
        private const float PATTERN_BONUS_WEIGHT = 2.0f;

        // ============================================================
        // CONFIGURATION
        // ============================================================

        private readonly ExecutionerConfigSO _config;

        // ============================================================
        // INTERFACE IMPLEMENTATION
        // ============================================================

        public GuessType StrategyType => GuessType.Letter;

        // ============================================================
        // CONSTRUCTOR
        // ============================================================

        /// <summary>
        /// Creates a new LetterGuessStrategy.
        /// </summary>
        /// <param name="config">Configuration ScriptableObject</param>
        public LetterGuessStrategy(ExecutionerConfigSO config)
        {
            _config = config;
        }

        // ============================================================
        // MAIN EVALUATION
        // ============================================================

        /// <summary>
        /// Evaluates the game state and recommends a letter to guess.
        /// </summary>
        /// <param name="state">Current game state</param>
        /// <returns>Letter guess recommendation</returns>
        public GuessRecommendation Evaluate(AIGameState state)
        {
            // Check if all letters in opponent's words are already found
            // If so, no point guessing more letters - focus on coordinates instead
            if (state.AreAllLettersFound())
            {
                Debug.Log("[LetterGuessStrategy] All letters already found - skipping letter guess");
                return GuessRecommendation.CreateInvalid();
            }

            // Get unguessed letters
            HashSet<char> unguessedLetters = state.GetUnguessedLetters();

            if (unguessedLetters.Count == 0)
            {
                Debug.LogWarning("[LetterGuessStrategy] No unguessed letters remaining");
                return GuessRecommendation.CreateInvalid();
            }

            // Score each unguessed letter
            List<(char letter, float score)> scoredLetters = new List<(char, float)>();

            foreach (char letter in unguessedLetters)
            {
                float score = CalculateLetterScore(letter, state);
                scoredLetters.Add((letter, score));
            }

            // Sort by score descending
            scoredLetters.Sort((a, b) => b.score.CompareTo(a.score));

            // Select based on skill level
            int poolSize = _config.GetLetterSelectionPoolSize(state.SkillLevel);
            poolSize = Mathf.Min(poolSize, scoredLetters.Count);

            // Pick randomly from the top N candidates
            int selectedIndex = Random.Range(0, poolSize);
            char selectedLetter = scoredLetters[selectedIndex].letter;
            float selectedScore = scoredLetters[selectedIndex].score;

            // Calculate confidence based on relative score
            float maxScore = scoredLetters[0].score;
            float confidence = maxScore > 0 ? selectedScore / maxScore : 0.5f;

            return GuessRecommendation.CreateLetterGuess(selectedLetter, confidence);
        }

        // ============================================================
        // SCORING
        // ============================================================

        /// <summary>
        /// Calculates a score for a letter based on frequency and pattern analysis.
        /// </summary>
        /// <param name="letter">The letter to score</param>
        /// <param name="state">Current game state</param>
        /// <returns>Combined score (higher = better candidate)</returns>
        private float CalculateLetterScore(char letter, AIGameState state)
        {
            // Base score from English letter frequency
            float baseScore = LetterFrequency.GetFrequency(letter);

            // Pattern completion bonus
            float patternBonus = CalculatePatternBonus(letter, state);

            return baseScore + (patternBonus * PATTERN_BONUS_WEIGHT);
        }

        /// <summary>
        /// Calculates bonus for a letter based on how likely it is to appear
        /// in unsolved word patterns.
        /// </summary>
        /// <param name="letter">The letter to check</param>
        /// <param name="state">Current game state</param>
        /// <returns>Bonus score based on pattern matching potential</returns>
        private float CalculatePatternBonus(char letter, AIGameState state)
        {
            if (state.WordPatterns == null || state.WordPatterns.Count == 0)
            {
                return 0f;
            }

            if (state.WordBank == null || state.WordBank.Count == 0)
            {
                return 0f;
            }

            float totalBonus = 0f;

            for (int i = 0; i < state.WordPatterns.Count; i++)
            {
                // Skip solved words
                if (i < state.WordsSolved.Count && state.WordsSolved[i])
                {
                    continue;
                }

                string pattern = state.WordPatterns[i];

                // Skip if pattern already contains this letter
                if (pattern.Contains(letter.ToString()))
                {
                    continue;
                }

                // Count how many matching words contain this letter
                int matchingWordsWithLetter = CountMatchingWordsWithLetter(pattern, letter, state.WordBank);
                int totalMatchingWords = CountMatchingWords(pattern, state.WordBank);

                if (totalMatchingWords > 0)
                {
                    // Bonus = proportion of matching words that contain this letter
                    float proportion = (float)matchingWordsWithLetter / totalMatchingWords;
                    totalBonus += proportion;
                }
            }

            return totalBonus;
        }

        // ============================================================
        // PATTERN MATCHING
        // ============================================================

        /// <summary>
        /// Counts how many words in the bank match the pattern and contain a specific letter.
        /// </summary>
        /// <param name="pattern">Word pattern with '_' for unknowns</param>
        /// <param name="letter">Letter to check for</param>
        /// <param name="wordBank">Set of valid words</param>
        /// <returns>Count of matching words containing the letter</returns>
        private int CountMatchingWordsWithLetter(string pattern, char letter, HashSet<string> wordBank)
        {
            int count = 0;
            char upperLetter = char.ToUpper(letter);

            foreach (string word in wordBank)
            {
                if (word.Length != pattern.Length)
                {
                    continue;
                }

                if (!word.ToUpper().Contains(upperLetter.ToString()))
                {
                    continue;
                }

                if (MatchesPattern(word, pattern))
                {
                    count++;
                }
            }

            return count;
        }

        /// <summary>
        /// Counts how many words in the bank match the pattern.
        /// </summary>
        /// <param name="pattern">Word pattern with '_' for unknowns</param>
        /// <param name="wordBank">Set of valid words</param>
        /// <returns>Count of matching words</returns>
        private int CountMatchingWords(string pattern, HashSet<string> wordBank)
        {
            int count = 0;

            foreach (string word in wordBank)
            {
                if (word.Length != pattern.Length)
                {
                    continue;
                }

                if (MatchesPattern(word, pattern))
                {
                    count++;
                }
            }

            return count;
        }

        /// <summary>
        /// Checks if a word matches a pattern.
        /// Pattern uses '_' for unknown positions and letters for known positions.
        /// </summary>
        /// <param name="word">Word to check</param>
        /// <param name="pattern">Pattern to match against</param>
        /// <returns>True if word matches pattern</returns>
        private bool MatchesPattern(string word, string pattern)
        {
            if (word.Length != pattern.Length)
            {
                return false;
            }

            string upperWord = word.ToUpper();
            string upperPattern = pattern.ToUpper();

            for (int i = 0; i < pattern.Length; i++)
            {
                char patternChar = upperPattern[i];

                // '_' matches any character
                if (patternChar == '_')
                {
                    continue;
                }

                // Known letter must match exactly
                if (upperWord[i] != patternChar)
                {
                    return false;
                }
            }

            return true;
        }

        // ============================================================
        // DEBUG
        // ============================================================

        /// <summary>
        /// Gets a debug breakdown of letter scores for the current state.
        /// </summary>
        /// <param name="state">Current game state</param>
        /// <param name="topN">Number of top letters to include</param>
        /// <returns>Debug string showing letter scores</returns>
        public string GetDebugScoreBreakdown(AIGameState state, int topN = 10)
        {
            HashSet<char> unguessedLetters = state.GetUnguessedLetters();
            List<(char letter, float score)> scoredLetters = new List<(char, float)>();

            foreach (char letter in unguessedLetters)
            {
                float score = CalculateLetterScore(letter, state);
                scoredLetters.Add((letter, score));
            }

            scoredLetters.Sort((a, b) => b.score.CompareTo(a.score));

            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            sb.AppendLine("Letter Scores (top candidates):");

            int count = Mathf.Min(topN, scoredLetters.Count);
            for (int i = 0; i < count; i++)
            {
                sb.AppendLine(string.Format("  {0}. {1}: {2:F2}", i + 1, scoredLetters[i].letter, scoredLetters[i].score));
            }

            int poolSize = _config.GetLetterSelectionPoolSize(state.SkillLevel);
            sb.AppendLine(string.Format("Selection pool size: {0} (skill: {1:F2})", poolSize, state.SkillLevel));

            return sb.ToString();
        }
    }
}

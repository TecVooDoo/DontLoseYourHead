// WordGuessStrategy.cs
// AI strategy for guessing complete words based on pattern matching and confidence
// Created: December 13, 2025
// Developer: TecVooDoo LLC

using System.Collections.Generic;
using UnityEngine;
using DLYH.AI.Config;

namespace DLYH.AI.Strategies
{
    /// <summary>
    /// Strategy for deciding IF and WHAT word to guess.
    /// 
    /// Risk: Wrong word guess costs 2 misses (double penalty).
    /// 
    /// Algorithm:
    /// 1. For each unsolved word pattern, find matching words in bank
    /// 2. Calculate confidence based on number of matches
    /// 3. Compare confidence to skill-based threshold
    /// 4. If above threshold, attempt guess
    /// 
    /// Confidence thresholds by skill (approximate):
    /// - Skill 0.2 (Easy): needs 86%+ confidence (rarely attempts)
    /// - Skill 0.5 (Normal): needs 65%+ confidence
    /// - Skill 0.8 (Hard): needs 44%+ confidence
    /// - Skill 0.95 (Expert): needs 34%+ confidence (risk-taker)
    /// </summary>
    public class WordGuessStrategy : IGuessStrategy
    {
        // ============================================================
        // CONSTANTS
        // ============================================================

        /// <summary>
        /// Confidence when exactly one word matches the pattern.
        /// Not 100% because the word might not be in the opponent's vocabulary.
        /// </summary>
        private const float SINGLE_MATCH_CONFIDENCE = 0.95f;

        /// <summary>
        /// Minimum confidence to ever consider a word guess.
        /// Below this, the strategy won't recommend guessing even at max skill.
        /// </summary>
        private const float MINIMUM_VIABLE_CONFIDENCE = 0.25f;

        // ============================================================
        // CONFIGURATION
        // ============================================================

        private readonly ExecutionerConfigSO _config;

        // ============================================================
        // INTERFACE IMPLEMENTATION
        // ============================================================

        public GuessType StrategyType => GuessType.Word;

        // ============================================================
        // CONSTRUCTOR
        // ============================================================

        /// <summary>
        /// Creates a new WordGuessStrategy.
        /// </summary>
        /// <param name="config">Configuration ScriptableObject</param>
        public WordGuessStrategy(ExecutionerConfigSO config)
        {
            _config = config;
        }

        // ============================================================
        // MAIN EVALUATION
        // ============================================================

        /// <summary>
        /// Evaluates the game state and recommends a word to guess (if any).
        /// Returns an invalid recommendation if no word guess meets the confidence threshold.
        /// </summary>
        /// <param name="state">Current game state</param>
        /// <returns>Word guess recommendation, or invalid if none recommended</returns>
        public GuessRecommendation Evaluate(AIGameState state)
        {
            if (state.WordBank == null || state.WordBank.Count == 0)
            {
                Debug.LogWarning("[WordGuessStrategy] No word bank available");
                return GuessRecommendation.CreateInvalid();
            }

            // Get the confidence threshold for current skill level
            float confidenceThreshold = _config.GetWordGuessThresholdForSkill(state.SkillLevel);

            // Track best candidate across all unsolved words
            string bestWord = null;
            int bestWordIndex = -1;
            float bestConfidence = 0f;

            // Evaluate each unsolved word pattern
            for (int i = 0; i < state.WordPatterns.Count; i++)
            {
                // Skip already solved words
                if (state.WordsSolved[i])
                {
                    continue;
                }

                string pattern = state.WordPatterns[i];

                // Skip patterns with no revealed letters (pure guessing is too risky)
                if (!HasRevealedLetters(pattern))
                {
                    continue;
                }

                // Find matching words
                List<string> matches = FindMatchingWords(pattern, state.WordBank);

                if (matches.Count == 0)
                {
                    continue;
                }

                // Calculate confidence
                float confidence = CalculateConfidence(matches.Count);

                // Track if this is the best candidate so far
                if (confidence > bestConfidence)
                {
                    bestConfidence = confidence;
                    bestWord = matches[0]; // Pick first match (they're all equally likely)
                    bestWordIndex = i;
                }
            }

            // Check if best candidate meets threshold
            if (bestWord != null && bestConfidence >= confidenceThreshold && bestConfidence >= MINIMUM_VIABLE_CONFIDENCE)
            {
                Debug.Log(string.Format(
                    "[WordGuessStrategy] Recommending word guess: {0} for pattern index {1} (confidence: {2:P0}, threshold: {3:P0})",
                    bestWord, bestWordIndex, bestConfidence, confidenceThreshold));

                return GuessRecommendation.CreateWordGuess(bestWord, bestWordIndex, bestConfidence);
            }

            // No word guess recommended
            return GuessRecommendation.CreateInvalid();
        }

        // ============================================================
        // PATTERN MATCHING
        // ============================================================

        /// <summary>
        /// Checks if a pattern has any revealed letters.
        /// </summary>
        /// <param name="pattern">Pattern like "_A_E" or "____"</param>
        /// <returns>True if at least one letter is revealed</returns>
        private bool HasRevealedLetters(string pattern)
        {
            foreach (char c in pattern)
            {
                if (c != '_')
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Finds all words in the bank that match the given pattern.
        /// </summary>
        /// <param name="pattern">Pattern like "_A_E" where _ is unknown</param>
        /// <param name="wordBank">Set of valid words</param>
        /// <returns>List of matching words</returns>
        private List<string> FindMatchingWords(string pattern, HashSet<string> wordBank)
        {
            List<string> matches = new List<string>();
            int patternLength = pattern.Length;

            foreach (string word in wordBank)
            {
                // Length must match
                if (word.Length != patternLength)
                {
                    continue;
                }

                // Check pattern match
                if (MatchesPattern(word, pattern))
                {
                    matches.Add(word);
                }
            }

            return matches;
        }

        /// <summary>
        /// Checks if a word matches a pattern.
        /// </summary>
        /// <param name="word">Word to check (e.g., "CAKE")</param>
        /// <param name="pattern">Pattern to match (e.g., "_A_E")</param>
        /// <returns>True if word matches pattern</returns>
        private bool MatchesPattern(string word, string pattern)
        {
            if (word.Length != pattern.Length)
            {
                return false;
            }

            for (int i = 0; i < pattern.Length; i++)
            {
                char patternChar = pattern[i];

                // Underscore matches any character
                if (patternChar == '_')
                {
                    continue;
                }

                // Known letters must match exactly (case-insensitive)
                if (char.ToUpperInvariant(word[i]) != char.ToUpperInvariant(patternChar))
                {
                    return false;
                }
            }

            return true;
        }

        // ============================================================
        // CONFIDENCE CALCULATION
        // ============================================================

        /// <summary>
        /// Calculates confidence based on number of matching words.
        /// Fewer matches = higher confidence that we've found the right word.
        /// </summary>
        /// <param name="matchCount">Number of words matching the pattern</param>
        /// <returns>Confidence value (0-1)</returns>
        private float CalculateConfidence(int matchCount)
        {
            if (matchCount <= 0)
            {
                return 0f;
            }

            if (matchCount == 1)
            {
                return SINGLE_MATCH_CONFIDENCE;
            }

            // Confidence decreases as match count increases
            // 2 matches = 50%, 3 matches = 33%, 4 matches = 25%, etc.
            return 1.0f / matchCount;
        }

        // ============================================================
        // DEBUG AND ANALYSIS
        // ============================================================

        /// <summary>
        /// Gets a debug breakdown of word guess opportunities for the current state.
        /// </summary>
        /// <param name="state">Current game state</param>
        /// <returns>Debug string showing pattern analysis</returns>
        public string GetDebugAnalysis(AIGameState state)
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();

            float confidenceThreshold = _config.GetWordGuessThresholdForSkill(state.SkillLevel);
            sb.AppendLine(string.Format("Word Guess Analysis (skill: {0:F2}, threshold: {1:P0}):",
                state.SkillLevel, confidenceThreshold));
            sb.AppendLine();

            for (int i = 0; i < state.WordPatterns.Count; i++)
            {
                string pattern = state.WordPatterns[i];
                bool solved = state.WordsSolved[i];

                sb.AppendLine(string.Format("Word {0}: {1} {2}",
                    i + 1, pattern, solved ? "(SOLVED)" : ""));

                if (solved)
                {
                    continue;
                }

                if (!HasRevealedLetters(pattern))
                {
                    sb.AppendLine("  -> No revealed letters, skipping");
                    continue;
                }

                List<string> matches = FindMatchingWords(pattern, state.WordBank);
                float confidence = CalculateConfidence(matches.Count);

                sb.AppendLine(string.Format("  -> {0} matches, confidence: {1:P0}",
                    matches.Count, confidence));

                if (matches.Count > 0 && matches.Count <= 5)
                {
                    sb.AppendLine(string.Format("  -> Candidates: {0}", string.Join(", ", matches)));
                }
                else if (matches.Count > 5)
                {
                    sb.AppendLine(string.Format("  -> First 5: {0}...",
                        string.Join(", ", matches.GetRange(0, 5))));
                }

                string decision = confidence >= confidenceThreshold ? "WOULD GUESS" : "Below threshold";
                sb.AppendLine(string.Format("  -> {0}", decision));
                sb.AppendLine();
            }

            return sb.ToString();
        }

        /// <summary>
        /// Gets the number of matching words for a pattern without returning the full list.
        /// Useful for quick analysis.
        /// </summary>
        /// <param name="pattern">Pattern to check</param>
        /// <param name="wordBank">Word bank to search</param>
        /// <returns>Count of matching words</returns>
        public int GetMatchCount(string pattern, HashSet<string> wordBank)
        {
            return FindMatchingWords(pattern, wordBank).Count;
        }
    }
}
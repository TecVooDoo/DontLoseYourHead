// DifficultyAdapter.cs
// Runtime difficulty management with rubber-banding and adaptive thresholds
// Created: December 13, 2025
// Developer: TecVooDoo LLC

using System.Collections.Generic;
using UnityEngine;
using TecVooDoo.DontLoseYourHead.Core;
using DLYH.AI.Config;

namespace DLYH.AI.Core
{
    /// <summary>
    /// Manages AI difficulty adaptation at runtime.
    /// 
    /// Features:
    /// - Rubber-banding: AI skill adjusts based on player performance
    /// - Adaptive thresholds: The rubber-banding itself adapts if player consistently struggles/dominates
    /// 
    /// This is a plain C# class (not MonoBehaviour) that tracks runtime state.
    /// </summary>
    public class DifficultyAdapter
    {
        // ============================================================
        // CONFIGURATION
        // ============================================================

        private readonly ExecutionerConfigSO _config;

        // ============================================================
        // RUNTIME STATE
        // ============================================================

        private float _currentSkill;
        private int _currentHitsToIncrease;
        private int _currentMissesToDecrease;

        // Tracking for rubber-banding - using ring buffer to avoid allocations
        private readonly bool[] _recentPlayerGuesses;
        private int _guessWriteIndex;
        private int _guessCount;

        // Tracking for adaptive thresholds
        private int _consecutiveIncreases;
        private int _consecutiveDecreases;

        // ============================================================
        // PROPERTIES
        // ============================================================

        /// <summary>
        /// Current AI skill level (0.15 to 0.95).
        /// Higher = smarter AI choices.
        /// </summary>
        public float CurrentSkill => _currentSkill;

        /// <summary>
        /// Current threshold: consecutive player hits needed before AI skill increases.
        /// </summary>
        public int CurrentHitsToIncrease => _currentHitsToIncrease;

        /// <summary>
        /// Current threshold: consecutive player misses needed before AI skill decreases.
        /// </summary>
        public int CurrentMissesToDecrease => _currentMissesToDecrease;

        /// <summary>
        /// Number of consecutive times AI skill has increased without decreasing.
        /// </summary>
        public int ConsecutiveIncreases => _consecutiveIncreases;

        /// <summary>
        /// Number of consecutive times AI skill has decreased without increasing.
        /// </summary>
        public int ConsecutiveDecreases => _consecutiveDecreases;

        // ============================================================
        // CONSTRUCTOR
        // ============================================================

        /// <summary>
        /// Creates a new DifficultyAdapter initialized for the given player difficulty.
        /// </summary>
        /// <param name="config">Configuration ScriptableObject</param>
        /// <param name="playerDifficulty">The difficulty setting chosen by the player</param>
        public DifficultyAdapter(ExecutionerConfigSO config, DifficultySetting playerDifficulty)
        {
            _config = config;

            // Initialize skill and thresholds based on player's chosen difficulty
            _currentSkill = config.GetStartSkillForDifficulty(playerDifficulty);
            _currentHitsToIncrease = config.GetHitsToIncreaseForDifficulty(playerDifficulty);
            _currentMissesToDecrease = config.GetMissesToDecreaseForDifficulty(playerDifficulty);

            // Initialize tracking - pre-allocate ring buffer to avoid per-guess allocations
            _recentPlayerGuesses = new bool[config.RecentGuessesToTrack];
            _guessWriteIndex = 0;
            _guessCount = 0;
            _consecutiveIncreases = 0;
            _consecutiveDecreases = 0;

            Debug.Log(string.Format(
                "[DifficultyAdapter] Initialized for {0} difficulty - Skill: {1:F2}, HitsToIncrease: {2}, MissesToDecrease: {3}",
                playerDifficulty, _currentSkill, _currentHitsToIncrease, _currentMissesToDecrease));
        }

        // ============================================================
        // PUBLIC METHODS
        // ============================================================

        /// <summary>
        /// Records a player guess result and potentially adjusts AI difficulty.
        /// Call this after every player guess.
        /// </summary>
        /// <param name="wasHit">True if the player's guess was a hit, false if miss</param>
        public void RecordPlayerGuess(bool wasHit)
        {
            // Add to ring buffer (no allocation)
            _recentPlayerGuesses[_guessWriteIndex] = wasHit;
            _guessWriteIndex = (_guessWriteIndex + 1) % _recentPlayerGuesses.Length;
            if (_guessCount < _recentPlayerGuesses.Length)
            {
                _guessCount++;
            }

            // Check for consecutive hits/misses and adjust accordingly
            int consecutiveHits = CountTrailingMatches(true);
            int consecutiveMisses = CountTrailingMatches(false);

            if (consecutiveHits >= _currentHitsToIncrease)
            {
                IncreaseSkill();
            }
            else if (consecutiveMisses >= _currentMissesToDecrease)
            {
                DecreaseSkill();
            }
        }

        /// <summary>
        /// Resets the adapter to initial state for a new game.
        /// </summary>
        /// <param name="playerDifficulty">The difficulty setting for the new game</param>
        public void Reset(DifficultySetting playerDifficulty)
        {
            _currentSkill = _config.GetStartSkillForDifficulty(playerDifficulty);
            _currentHitsToIncrease = _config.GetHitsToIncreaseForDifficulty(playerDifficulty);
            _currentMissesToDecrease = _config.GetMissesToDecreaseForDifficulty(playerDifficulty);

            _guessWriteIndex = 0;
            _guessCount = 0;
            _consecutiveIncreases = 0;
            _consecutiveDecreases = 0;

            Debug.Log(string.Format(
                "[DifficultyAdapter] Reset for {0} difficulty - Skill: {1:F2}",
                playerDifficulty, _currentSkill));
        }

        /// <summary>
        /// Gets a debug summary of the current adapter state.
        /// </summary>
        /// <returns>Multi-line string describing current state</returns>
        public string GetDebugSummary()
        {
            return string.Format(
                "Skill: {0:F2}\nHitsToIncrease: {1}\nMissesToDecrease: {2}\nConsec. Increases: {3}\nConsec. Decreases: {4}\nRecent Guesses: {5}",
                _currentSkill,
                _currentHitsToIncrease,
                _currentMissesToDecrease,
                _consecutiveIncreases,
                _consecutiveDecreases,
                GetRecentGuessesString());
        }

        // ============================================================
        // SKILL ADJUSTMENT
        // ============================================================

        /// <summary>
        /// Increases AI skill (player is doing well, make AI harder).
        /// </summary>
        private void IncreaseSkill()
        {
            float oldSkill = _currentSkill;
            _currentSkill = _config.ClampSkill(_currentSkill + _config.SkillAdjustmentStep);

            // Track consecutive adjustments
            _consecutiveIncreases++;
            _consecutiveDecreases = 0;

            Debug.Log(string.Format(
                "[DifficultyAdapter] AI skill INCREASED: {0:F2} -> {1:F2} (player doing well)",
                oldSkill, _currentSkill));

            // Check if thresholds should adapt
            if (_consecutiveIncreases >= _config.ConsecutiveAdjustmentsToAdapt)
            {
                AdaptThresholdsForDominatingPlayer();
            }

            // Clear recent guesses after adjustment
            ClearRecentGuesses();
        }

        /// <summary>
        /// Decreases AI skill (player is struggling, make AI easier).
        /// </summary>
        private void DecreaseSkill()
        {
            float oldSkill = _currentSkill;
            _currentSkill = _config.ClampSkill(_currentSkill - _config.SkillAdjustmentStep);

            // Track consecutive adjustments
            _consecutiveDecreases++;
            _consecutiveIncreases = 0;

            Debug.Log(string.Format(
                "[DifficultyAdapter] AI skill DECREASED: {0:F2} -> {1:F2} (player struggling)",
                oldSkill, _currentSkill));

            // Check if thresholds should adapt
            if (_consecutiveDecreases >= _config.ConsecutiveAdjustmentsToAdapt)
            {
                AdaptThresholdsForStrugglingPlayer();
            }

            // Clear recent guesses after adjustment
            ClearRecentGuesses();
        }

        // ============================================================
        // ADAPTIVE THRESHOLDS
        // ============================================================

        /// <summary>
        /// Adapts thresholds when player is consistently dominating.
        /// Makes it easier for AI to get harder (challenge the player more).
        /// </summary>
        private void AdaptThresholdsForDominatingPlayer()
        {
            int oldHits = _currentHitsToIncrease;
            int oldMisses = _currentMissesToDecrease;

            // Make it EASIER for AI to increase (fewer hits needed)
            _currentHitsToIncrease = _config.ClampHitsToIncrease(_currentHitsToIncrease - 1);

            // Make it HARDER for AI to decrease (more misses needed)
            _currentMissesToDecrease = _config.ClampMissesToDecrease(_currentMissesToDecrease + 1);

            Debug.Log(string.Format(
                "[DifficultyAdapter] Thresholds adapted (player dominating) - HitsToIncrease: {0} -> {1}, MissesToDecrease: {2} -> {3}",
                oldHits, _currentHitsToIncrease, oldMisses, _currentMissesToDecrease));

            // Reset consecutive counter after adaptation
            _consecutiveIncreases = 0;
        }

        /// <summary>
        /// Adapts thresholds when player is consistently struggling.
        /// Makes it easier for AI to get easier (protect the player).
        /// </summary>
        private void AdaptThresholdsForStrugglingPlayer()
        {
            int oldHits = _currentHitsToIncrease;
            int oldMisses = _currentMissesToDecrease;

            // Make it HARDER for AI to increase (more hits needed)
            _currentHitsToIncrease = _config.ClampHitsToIncrease(_currentHitsToIncrease + 1);

            // Make it EASIER for AI to decrease (fewer misses needed)
            _currentMissesToDecrease = _config.ClampMissesToDecrease(_currentMissesToDecrease - 1);

            Debug.Log(string.Format(
                "[DifficultyAdapter] Thresholds adapted (player struggling) - HitsToIncrease: {0} -> {1}, MissesToDecrease: {2} -> {3}",
                oldHits, _currentHitsToIncrease, oldMisses, _currentMissesToDecrease));

            // Reset consecutive counter after adaptation
            _consecutiveDecreases = 0;
        }

        // ============================================================
        // HELPER METHODS
        // ============================================================

        /// <summary>
        /// Counts consecutive matching results from the end of the ring buffer.
        /// No allocation - reads directly from pre-allocated array.
        /// </summary>
        /// <param name="targetValue">The value to count (true for hits, false for misses)</param>
        /// <returns>Number of consecutive matches from the most recent guess</returns>
        private int CountTrailingMatches(bool targetValue)
        {
            if (_guessCount == 0)
            {
                return 0;
            }

            int count = 0;

            // Read backwards from most recent entry (write index - 1, wrapping)
            for (int i = 0; i < _guessCount; i++)
            {
                // Calculate index going backwards from most recent
                int readIndex = (_guessWriteIndex - 1 - i + _recentPlayerGuesses.Length) % _recentPlayerGuesses.Length;

                if (_recentPlayerGuesses[readIndex] == targetValue)
                {
                    count++;
                }
                else
                {
                    break;
                }
            }

            return count;
        }

        /// <summary>
        /// Clears the recent guesses ring buffer after a skill adjustment.
        /// </summary>
        private void ClearRecentGuesses()
        {
            _guessWriteIndex = 0;
            _guessCount = 0;
        }

        /// <summary>
        /// Gets a string representation of recent guesses for debugging.
        /// Note: This allocates for debug output only - not called in hot paths.
        /// </summary>
        /// <returns>String like "HHMHM" where H=hit, M=miss</returns>
        private string GetRecentGuessesString()
        {
            if (_guessCount == 0)
            {
                return "(none)";
            }

            System.Text.StringBuilder sb = new System.Text.StringBuilder(_guessCount);

            // Read in chronological order (oldest to newest)
            int startIndex = _guessCount < _recentPlayerGuesses.Length
                ? 0
                : _guessWriteIndex;

            for (int i = 0; i < _guessCount; i++)
            {
                int readIndex = (startIndex + i) % _recentPlayerGuesses.Length;
                sb.Append(_recentPlayerGuesses[readIndex] ? 'H' : 'M');
            }

            return sb.ToString();
        }
    }
}

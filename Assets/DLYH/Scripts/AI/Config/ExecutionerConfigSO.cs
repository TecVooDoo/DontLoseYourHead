// ExecutionerConfigSO.cs
// ScriptableObject containing all tunable parameters for the AI opponent
// Created: December 13, 2025
// Developer: TecVooDoo LLC

using UnityEngine;
using TecVooDoo.DontLoseYourHead.Core;

namespace DLYH.AI.Config
{
    /// <summary>
    /// Configuration ScriptableObject for The Executioner AI opponent.
    /// All AI parameters are centralized here for easy tweaking in the Inspector.
    /// </summary>
    [CreateAssetMenu(fileName = "ExecutionerConfig", menuName = "DLYH/AI/Executioner Config")]
    public class ExecutionerConfigSO : ScriptableObject
    {
        // ============================================================
        // SKILL LEVEL BOUNDS
        // ============================================================

        [Header("Skill Level Bounds")]
        [Tooltip("Minimum skill level (AI never becomes completely random)")]
        [Range(0f, 1f)]
        [SerializeField] private float _minSkillLevel = 0.15f;

        [Tooltip("Maximum skill level (AI never becomes perfect)")]
        [Range(0f, 1f)]
        [SerializeField] private float _maxSkillLevel = 0.95f;

        [Tooltip("How much skill changes per adjustment")]
        [Range(0f, 0.3f)]
        [SerializeField] private float _skillAdjustmentStep = 0.15f;

        public float MinSkillLevel => _minSkillLevel;
        public float MaxSkillLevel => _maxSkillLevel;
        public float SkillAdjustmentStep => _skillAdjustmentStep;

        // ============================================================
        // INITIAL SKILL BY PLAYER DIFFICULTY
        // ============================================================

        [Header("Initial Skill by Player Difficulty")]
        [Tooltip("AI starting skill when player chooses Easy")]
        [Range(0f, 1f)]
        [SerializeField] private float _easyStartSkill = 0.25f;

        [Tooltip("AI starting skill when player chooses Normal")]
        [Range(0f, 1f)]
        [SerializeField] private float _normalStartSkill = 0.50f;

        [Tooltip("AI starting skill when player chooses Hard")]
        [Range(0f, 1f)]
        [SerializeField] private float _hardStartSkill = 0.75f;

        public float EasyStartSkill => _easyStartSkill;
        public float NormalStartSkill => _normalStartSkill;
        public float HardStartSkill => _hardStartSkill;

        // ============================================================
        // INITIAL THRESHOLDS BY PLAYER DIFFICULTY
        // ============================================================

        [Header("Initial Thresholds - Easy Difficulty")]
        [Tooltip("Consecutive player hits before AI skill increases (Easy)")]
        [Range(1, 7)]
        [SerializeField] private int _easyHitsToIncrease = 5;

        [Tooltip("Consecutive player misses before AI skill decreases (Easy)")]
        [Range(1, 7)]
        [SerializeField] private int _easyMissesToDecrease = 2;

        [Header("Initial Thresholds - Normal Difficulty")]
        [Tooltip("Consecutive player hits before AI skill increases (Normal)")]
        [Range(1, 7)]
        [SerializeField] private int _normalHitsToIncrease = 3;

        [Tooltip("Consecutive player misses before AI skill decreases (Normal)")]
        [Range(1, 7)]
        [SerializeField] private int _normalMissesToDecrease = 3;

        [Header("Initial Thresholds - Hard Difficulty")]
        [Tooltip("Consecutive player hits before AI skill increases (Hard)")]
        [Range(1, 7)]
        [SerializeField] private int _hardHitsToIncrease = 2;

        [Tooltip("Consecutive player misses before AI skill decreases (Hard)")]
        [Range(1, 7)]
        [SerializeField] private int _hardMissesToDecrease = 5;

        public int EasyHitsToIncrease => _easyHitsToIncrease;
        public int EasyMissesToDecrease => _easyMissesToDecrease;
        public int NormalHitsToIncrease => _normalHitsToIncrease;
        public int NormalMissesToDecrease => _normalMissesToDecrease;
        public int HardHitsToIncrease => _hardHitsToIncrease;
        public int HardMissesToDecrease => _hardMissesToDecrease;

        // ============================================================
        // ADAPTIVE THRESHOLD SETTINGS
        // ============================================================

        [Header("Adaptive Threshold Settings")]
        [Tooltip("Consecutive same-direction skill adjustments before thresholds adapt")]
        [Range(1, 5)]
        [SerializeField] private int _consecutiveAdjustmentsToAdapt = 2;

        [Tooltip("Minimum value for HitsToIncrease threshold")]
        [Range(1, 7)]
        [SerializeField] private int _minHitsToIncrease = 1;

        [Tooltip("Maximum value for HitsToIncrease threshold")]
        [Range(1, 10)]
        [SerializeField] private int _maxHitsToIncrease = 7;

        [Tooltip("Minimum value for MissesToDecrease threshold")]
        [Range(1, 7)]
        [SerializeField] private int _minMissesToDecrease = 1;

        [Tooltip("Maximum value for MissesToDecrease threshold")]
        [Range(1, 10)]
        [SerializeField] private int _maxMissesToDecrease = 7;

        public int ConsecutiveAdjustmentsToAdapt => _consecutiveAdjustmentsToAdapt;
        public int MinHitsToIncrease => _minHitsToIncrease;
        public int MaxHitsToIncrease => _maxHitsToIncrease;
        public int MinMissesToDecrease => _minMissesToDecrease;
        public int MaxMissesToDecrease => _maxMissesToDecrease;

        // ============================================================
        // TRACKING
        // ============================================================

        [Header("Tracking")]
        [Tooltip("How many recent player guesses to track for rubber-banding")]
        [Range(3, 10)]
        [SerializeField] private int _recentGuessesToTrack = 5;

        public int RecentGuessesToTrack => _recentGuessesToTrack;

        // ============================================================
        // STRATEGY - GRID DENSITY THRESHOLDS
        // ============================================================

        [Header("Strategy - Grid Density Thresholds")]
        [Tooltip("Fill ratio above this value favors coordinate guessing (dense grids)")]
        [Range(0f, 1f)]
        [SerializeField] private float _highDensityThreshold = 0.35f;

        [Tooltip("Fill ratio below this value strongly favors letter guessing (sparse grids)")]
        [Range(0f, 1f)]
        [SerializeField] private float _lowDensityThreshold = 0.12f;

        public float HighDensityThreshold => _highDensityThreshold;
        public float LowDensityThreshold => _lowDensityThreshold;

        // ============================================================
        // STRATEGY - WORD GUESSING
        // ============================================================

        [Header("Strategy - Word Guessing")]
        [Tooltip("Higher = more willing to risk word guesses at lower confidence. Risk threshold = 1.0 - (skill * factor)")]
        [Range(0f, 1f)]
        [SerializeField] private float _wordGuessRiskFactor = 0.7f;

        public float WordGuessRiskFactor => _wordGuessRiskFactor;

        // ============================================================
        // MEMORY
        // ============================================================

        [Header("Memory")]
        [Tooltip("Max chance to forget older information at lowest skill (0.3 = 30%)")]
        [Range(0f, 0.5f)]
        [SerializeField] private float _maxForgetChance = 0.3f;

        [Tooltip("Always remember this many most recent guesses regardless of skill")]
        [Range(1, 5)]
        [SerializeField] private int _alwaysRememberRecent = 3;

        public float MaxForgetChance => _maxForgetChance;
        public int AlwaysRememberRecent => _alwaysRememberRecent;

        // ============================================================
        // TIMING
        // ============================================================

        [Header("Timing")]
        [Tooltip("Minimum think time in seconds before AI makes a move")]
        [SerializeField] private float _minThinkTime = 1.0f;

        [Tooltip("Maximum think time in seconds before AI makes a move")]
        [SerializeField] private float _maxThinkTime = 3.0f;

        public float MinThinkTime => _minThinkTime;
        public float MaxThinkTime => _maxThinkTime;

        // ============================================================
        // HELPER METHODS
        // ============================================================

        /// <summary>
        /// Gets the initial AI skill level based on player's chosen difficulty.
        /// </summary>
        /// <param name="difficulty">The difficulty setting chosen by the player</param>
        /// <returns>Starting skill level for the AI</returns>
        public float GetStartSkillForDifficulty(DifficultySetting difficulty)
        {
            switch (difficulty)
            {
                case DifficultySetting.Easy:
                    return _easyStartSkill;
                case DifficultySetting.Normal:
                    return _normalStartSkill;
                case DifficultySetting.Hard:
                    return _hardStartSkill;
                default:
                    Debug.LogWarning(string.Format("[ExecutionerConfigSO] Unknown difficulty: {0}, defaulting to Normal", difficulty));
                    return _normalStartSkill;
            }
        }

        /// <summary>
        /// Gets the initial HitsToIncrease threshold based on player's chosen difficulty.
        /// </summary>
        /// <param name="difficulty">The difficulty setting chosen by the player</param>
        /// <returns>Number of consecutive player hits before AI skill increases</returns>
        public int GetHitsToIncreaseForDifficulty(DifficultySetting difficulty)
        {
            switch (difficulty)
            {
                case DifficultySetting.Easy:
                    return _easyHitsToIncrease;
                case DifficultySetting.Normal:
                    return _normalHitsToIncrease;
                case DifficultySetting.Hard:
                    return _hardHitsToIncrease;
                default:
                    Debug.LogWarning(string.Format("[ExecutionerConfigSO] Unknown difficulty: {0}, defaulting to Normal", difficulty));
                    return _normalHitsToIncrease;
            }
        }

        /// <summary>
        /// Gets the initial MissesToDecrease threshold based on player's chosen difficulty.
        /// </summary>
        /// <param name="difficulty">The difficulty setting chosen by the player</param>
        /// <returns>Number of consecutive player misses before AI skill decreases</returns>
        public int GetMissesToDecreaseForDifficulty(DifficultySetting difficulty)
        {
            switch (difficulty)
            {
                case DifficultySetting.Easy:
                    return _easyMissesToDecrease;
                case DifficultySetting.Normal:
                    return _normalMissesToDecrease;
                case DifficultySetting.Hard:
                    return _hardMissesToDecrease;
                default:
                    Debug.LogWarning(string.Format("[ExecutionerConfigSO] Unknown difficulty: {0}, defaulting to Normal", difficulty));
                    return _normalMissesToDecrease;
            }
        }

        /// <summary>
        /// Gets a random think time within the configured range.
        /// </summary>
        /// <returns>Random think time in seconds</returns>
        public float GetRandomThinkTime()
        {
            return Random.Range(_minThinkTime, _maxThinkTime);
        }

        /// <summary>
        /// Calculates the forget chance for a given skill level.
        /// Higher skill = lower chance to forget.
        /// </summary>
        /// <param name="skillLevel">Current AI skill level (0-1)</param>
        /// <returns>Probability of forgetting older information (0-1)</returns>
        public float GetForgetChanceForSkill(float skillLevel)
        {
            // forgetChance = (1.0 - skillLevel) * maxForgetChance
            // At skill 0.15: forgetChance = 0.85 * 0.3 = 0.255 (25.5%)
            // At skill 0.5: forgetChance = 0.5 * 0.3 = 0.15 (15%)
            // At skill 0.95: forgetChance = 0.05 * 0.3 = 0.015 (1.5%)
            return (1f - skillLevel) * _maxForgetChance;
        }

        /// <summary>
        /// Calculates the word guess confidence threshold for a given skill level.
        /// Higher skill = lower threshold = more willing to risk guesses.
        /// </summary>
        /// <param name="skillLevel">Current AI skill level (0-1)</param>
        /// <returns>Minimum confidence required to attempt a word guess (0-1)</returns>
        public float GetWordGuessThresholdForSkill(float skillLevel)
        {
            // threshold = 1.0 - (skillLevel * riskFactor)
            // At skill 0.2: threshold = 1.0 - (0.2 * 0.7) = 0.86 (needs 86%+ confidence)
            // At skill 0.5: threshold = 1.0 - (0.5 * 0.7) = 0.65 (needs 65%+ confidence)
            // At skill 0.8: threshold = 1.0 - (0.8 * 0.7) = 0.44 (needs 44%+ confidence)
            return 1f - (skillLevel * _wordGuessRiskFactor);
        }

        /// <summary>
        /// Gets the selection pool size for letter guessing based on skill level.
        /// Higher skill = smaller pool = more optimal choices.
        /// </summary>
        /// <param name="skillLevel">Current AI skill level (0-1)</param>
        /// <returns>Number of top candidates to select from</returns>
        public int GetLetterSelectionPoolSize(float skillLevel)
        {
            if (skillLevel >= 0.9f)
            {
                return 1;  // Expert: always pick optimal
            }
            else if (skillLevel >= 0.7f)
            {
                return 2;  // Hard: top 2
            }
            else if (skillLevel >= 0.4f)
            {
                return 5;  // Normal: top 5
            }
            else
            {
                return 10; // Easy: top 10 or random
            }
        }

        /// <summary>
        /// Determines strategy preference weights based on grid fill ratio.
        /// </summary>
        /// <param name="fillRatio">Current grid fill ratio (letters/totalCells)</param>
        /// <param name="letterWeight">Output: weight for letter guessing strategy</param>
        /// <param name="coordinateWeight">Output: weight for coordinate guessing strategy</param>
        public void GetStrategyWeightsForDensity(float fillRatio, out float letterWeight, out float coordinateWeight)
        {
            if (fillRatio >= _highDensityThreshold)
            {
                // High density: favor coordinates
                letterWeight = 0.4f;
                coordinateWeight = 0.6f;
            }
            else if (fillRatio >= 0.20f)
            {
                // Medium density: balanced
                letterWeight = 0.5f;
                coordinateWeight = 0.5f;
            }
            else if (fillRatio >= _lowDensityThreshold)
            {
                // Low density: favor letters
                letterWeight = 0.65f;
                coordinateWeight = 0.35f;
            }
            else
            {
                // Very low density: strongly favor letters
                letterWeight = 0.8f;
                coordinateWeight = 0.2f;
            }
        }

        /// <summary>
        /// Clamps a skill value to the configured bounds.
        /// </summary>
        /// <param name="skill">Skill value to clamp</param>
        /// <returns>Clamped skill value within min/max bounds</returns>
        public float ClampSkill(float skill)
        {
            return Mathf.Clamp(skill, _minSkillLevel, _maxSkillLevel);
        }

        /// <summary>
        /// Clamps a HitsToIncrease threshold to the configured bounds.
        /// </summary>
        /// <param name="threshold">Threshold value to clamp</param>
        /// <returns>Clamped threshold within min/max bounds</returns>
        public int ClampHitsToIncrease(int threshold)
        {
            return Mathf.Clamp(threshold, _minHitsToIncrease, _maxHitsToIncrease);
        }

        /// <summary>
        /// Clamps a MissesToDecrease threshold to the configured bounds.
        /// </summary>
        /// <param name="threshold">Threshold value to clamp</param>
        /// <returns>Clamped threshold within min/max bounds</returns>
        public int ClampMissesToDecrease(int threshold)
        {
            return Mathf.Clamp(threshold, _minMissesToDecrease, _maxMissesToDecrease);
        }

        // ============================================================
        // VALIDATION
        // ============================================================

        private void OnValidate()
        {
            // Ensure min think time is not greater than max
            if (_minThinkTime > _maxThinkTime)
            {
                _minThinkTime = _maxThinkTime;
            }

            // Ensure low density threshold is less than high density threshold
            if (_lowDensityThreshold > _highDensityThreshold)
            {
                _lowDensityThreshold = _highDensityThreshold;
            }
        }
    }
}
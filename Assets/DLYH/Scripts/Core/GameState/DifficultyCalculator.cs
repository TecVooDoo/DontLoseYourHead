using UnityEngine;

namespace TecVooDoo.DontLoseYourHead.Core
{
    /// <summary>
    /// Static utility class for calculating difficulty-related values.
    /// 
    /// IMPORTANT: Miss limit is calculated from OPPONENT's grid/word settings
    /// combined with the PLAYER's difficulty preference.
    /// 
    /// Formula: MissLimit = Base + OpponentGridBonus + OpponentWordModifier + PlayerDifficultyModifier
    /// 
    /// Example:
    ///   You: 6x6, 4 words, Easy difficulty
    ///   Opponent: 12x12, 3 words, Hard difficulty
    ///   Your miss limit = 15 + 13 + 0 + 4 = 32 (you're guessing opponent's large 12x12 grid)
    ///   Opponent's miss limit = 15 + 3 + (-2) + (-4) = 12 (they're guessing your small 6x6 grid)
    /// </summary>
    public static class DifficultyCalculator
    {
        // Base miss allowance before any modifiers
        private const int BASE_MISSES = 15;

        // Grid size bonuses (more cells = more chances to miss)
        private const int GRID_BONUS_6X6 = 3;
        private const int GRID_BONUS_7X7 = 4;
        private const int GRID_BONUS_8X8 = 6;
        private const int GRID_BONUS_9X9 = 8;
        private const int GRID_BONUS_10X10 = 10;
        private const int GRID_BONUS_11X11 = 12;
        private const int GRID_BONUS_12X12 = 13;

        // Word count modifiers (fewer words = more empty space = harder)
        private const int WORD_MODIFIER_3_WORDS = 0;    // Baseline (harder)
        private const int WORD_MODIFIER_4_WORDS = -2;   // More letters = easier

        // Difficulty modifiers
        private const int DIFFICULTY_HARD = -4;
        private const int DIFFICULTY_NORMAL = 0;
        private const int DIFFICULTY_EASY = 4;

        #region New Formula - Player vs Opponent

        /// <summary>
        /// Calculate the miss limit for a player based on their difficulty preference
        /// and the OPPONENT's grid configuration.
        /// 
        /// This is the correct formula: you get more/fewer misses based on how
        /// hard your opponent's grid is to solve, modified by your difficulty choice.
        /// </summary>
        /// <param name="playerDifficulty">The player's chosen difficulty (affects their forgiveness)</param>
        /// <param name="opponentGridSize">The opponent's grid size (what you're guessing on)</param>
        /// <param name="opponentWordCount">The opponent's word count (what you're trying to find)</param>
        /// <returns>Number of allowed misses for this player</returns>
        public static int CalculateMissLimitForPlayer(
            DifficultySetting playerDifficulty,
            GridSizeOption opponentGridSize,
            WordCountOption opponentWordCount)
        {
            int baseValue = BASE_MISSES;
            int gridBonus = GetGridBonus(opponentGridSize);
            int wordModifier = GetWordCountModifier(opponentWordCount);
            int difficultyModifier = GetDifficultyModifier(playerDifficulty);

            int total = baseValue + gridBonus + wordModifier + difficultyModifier;

            // Clamp to reasonable range
            return Mathf.Clamp(total, 10, 40);
        }

        /// <summary>
        /// Calculate miss limit using raw int values for opponent's settings.
        /// </summary>
        public static int CalculateMissLimitForPlayer(
            DifficultySetting playerDifficulty,
            int opponentGridSize,
            int opponentWordCount)
        {
            GridSizeOption gridOption = GridSizeFromInt(opponentGridSize);
            WordCountOption wordOption = WordCountFromInt(opponentWordCount);
            return CalculateMissLimitForPlayer(playerDifficulty, gridOption, wordOption);
        }

        #endregion

        #region Legacy Formula (Deprecated)

        /// <summary>
        /// [DEPRECATED] Calculate the miss limit based on configuration.
        /// This method uses the player's own settings which is incorrect.
        /// Use CalculateMissLimitForPlayer() instead which uses opponent's grid settings.
        /// </summary>
        [System.Obsolete("Use CalculateMissLimitForPlayer() which correctly uses opponent's grid settings")]
        public static int CalculateMissLimit(GridSizeOption gridSize, WordCountOption wordCount, DifficultySetting difficulty)
        {
            int baseValue = BASE_MISSES;
            int gridBonus = GetGridBonus(gridSize);
            int wordModifier = GetWordCountModifier(wordCount);
            int difficultyModifier = GetDifficultyModifier(difficulty);

            int total = baseValue + gridBonus + wordModifier + difficultyModifier;

            // Clamp to reasonable range
            return Mathf.Clamp(total, 10, 40);
        }

        /// <summary>
        /// [DEPRECATED] Calculate miss limit using raw grid size value.
        /// Use CalculateMissLimitForPlayer() instead.
        /// </summary>
        [System.Obsolete("Use CalculateMissLimitForPlayer() which correctly uses opponent's grid settings")]
        public static int CalculateMissLimit(int gridSize, int wordCount, DifficultySetting difficulty)
        {
            GridSizeOption gridOption = GridSizeFromInt(gridSize);
            WordCountOption wordOption = WordCountFromInt(wordCount);
#pragma warning disable CS0618 // Suppress obsolete warning for internal call
            return CalculateMissLimit(gridOption, wordOption, difficulty);
#pragma warning restore CS0618
        }

        #endregion

        #region Grid Bonus Methods

        /// <summary>
        /// Get the grid size bonus for miss calculation
        /// </summary>
        public static int GetGridBonus(GridSizeOption gridSize)
        {
            switch (gridSize)
            {
                case GridSizeOption.Size6x6:
                    return GRID_BONUS_6X6;
                case GridSizeOption.Size7x7:
                    return GRID_BONUS_7X7;
                case GridSizeOption.Size8x8:
                    return GRID_BONUS_8X8;
                case GridSizeOption.Size9x9:
                    return GRID_BONUS_9X9;
                case GridSizeOption.Size10x10:
                    return GRID_BONUS_10X10;
                case GridSizeOption.Size11x11:
                    return GRID_BONUS_11X11;
                case GridSizeOption.Size12x12:
                    return GRID_BONUS_12X12;
                default:
                    return GRID_BONUS_8X8;
            }
        }

        /// <summary>
        /// Get the grid size bonus using raw int value
        /// </summary>
        public static int GetGridBonus(int gridSize)
        {
            switch (gridSize)
            {
                case 6: return GRID_BONUS_6X6;
                case 7: return GRID_BONUS_7X7;
                case 8: return GRID_BONUS_8X8;
                case 9: return GRID_BONUS_9X9;
                case 10: return GRID_BONUS_10X10;
                case 11: return GRID_BONUS_11X11;
                case 12: return GRID_BONUS_12X12;
                default: return GRID_BONUS_8X8;
            }
        }

        #endregion

        #region Word Count Methods

        /// <summary>
        /// Get the word count modifier for miss calculation
        /// </summary>
        public static int GetWordCountModifier(WordCountOption wordCount)
        {
            switch (wordCount)
            {
                case WordCountOption.Three:
                    return WORD_MODIFIER_3_WORDS;
                case WordCountOption.Four:
                    return WORD_MODIFIER_4_WORDS;
                default:
                    return WORD_MODIFIER_3_WORDS;
            }
        }

        #endregion

        #region Difficulty Methods

        /// <summary>
        /// Get the difficulty modifier for miss calculation
        /// </summary>
        public static int GetDifficultyModifier(DifficultySetting difficulty)
        {
            switch (difficulty)
            {
                case DifficultySetting.Hard:
                    return DIFFICULTY_HARD;
                case DifficultySetting.Normal:
                    return DIFFICULTY_NORMAL;
                case DifficultySetting.Easy:
                    return DIFFICULTY_EASY;
                default:
                    return DIFFICULTY_NORMAL;
            }
        }

        #endregion

        #region Conversion Methods

        /// <summary>
        /// Convert int to GridSizeOption
        /// </summary>
        public static GridSizeOption GridSizeFromInt(int size)
        {
            switch (size)
            {
                case 6: return GridSizeOption.Size6x6;
                case 7: return GridSizeOption.Size7x7;
                case 8: return GridSizeOption.Size8x8;
                case 9: return GridSizeOption.Size9x9;
                case 10: return GridSizeOption.Size10x10;
                case 11: return GridSizeOption.Size11x11;
                case 12: return GridSizeOption.Size12x12;
                default:
                    Debug.LogWarning(string.Format("[DifficultyCalculator] Unknown grid size {0}, defaulting to 8x8", size));
                    return GridSizeOption.Size8x8;
            }
        }

        /// <summary>
        /// Convert int to WordCountOption
        /// </summary>
        public static WordCountOption WordCountFromInt(int count)
        {
            switch (count)
            {
                case 3:
                    return WordCountOption.Three;
                case 4:
                    return WordCountOption.Four;
                default:
                    Debug.LogWarning(string.Format("[DifficultyCalculator] Unknown word count {0}, defaulting to Three", count));
                    return WordCountOption.Three;
            }
        }

        #endregion

        #region Word Length Methods

        /// <summary>
        /// Get the word lengths required for a given word count
        /// </summary>
        public static int[] GetWordLengths(WordCountOption wordCount)
        {
            switch (wordCount)
            {
                case WordCountOption.Three:
                    return new int[] { 3, 4, 5 };
                case WordCountOption.Four:
                    return new int[] { 3, 4, 5, 6 };
                default:
                    return new int[] { 3, 4, 5 };
            }
        }

        #endregion

        #region Debug/Display Methods

        /// <summary>
        /// Get a human-readable description of the miss limit calculation for a player
        /// Uses the correct formula: opponent's grid + player's difficulty
        /// </summary>
        public static string GetCalculationBreakdown(
            DifficultySetting playerDifficulty,
            int opponentGridSize,
            int opponentWordCount)
        {
            int baseValue = BASE_MISSES;
            int gridBonus = GetGridBonus(opponentGridSize);
            int wordModifier = GetWordCountModifier(WordCountFromInt(opponentWordCount));
            int difficultyModifier = GetDifficultyModifier(playerDifficulty);
            int total = CalculateMissLimitForPlayer(playerDifficulty, opponentGridSize, opponentWordCount);

            string breakdown = string.Format(
                "Base: {0}\nOpponent Grid ({1}x{1}): +{2}\nOpponent Words ({3}): {4}\nYour Difficulty ({5}): {6}\n----------\nYour Miss Limit: {7}",
                baseValue,
                opponentGridSize,
                gridBonus,
                opponentWordCount,
                wordModifier >= 0 ? "+" + wordModifier.ToString() : wordModifier.ToString(),
                playerDifficulty.ToString(),
                difficultyModifier >= 0 ? "+" + difficultyModifier.ToString() : difficultyModifier.ToString(),
                total
            );

            return breakdown;
        }

        #endregion
    }
}

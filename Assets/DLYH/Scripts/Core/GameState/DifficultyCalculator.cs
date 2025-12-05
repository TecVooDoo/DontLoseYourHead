using UnityEngine;

namespace TecVooDoo.DontLoseYourHead.Core
{
    /// <summary>
    /// Static utility class for calculating difficulty-related values
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

        /// <summary>
        /// Calculate the miss limit based on configuration
        /// </summary>
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
        /// Calculate miss limit using raw grid size value
        /// </summary>
        public static int CalculateMissLimit(int gridSize, int wordCount, DifficultySetting difficulty)
        {
            GridSizeOption gridOption = GridSizeFromInt(gridSize);
            WordCountOption wordOption = WordCountFromInt(wordCount);
            return CalculateMissLimit(gridOption, wordOption, difficulty);
        }

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

        /// <summary>
        /// Get a human-readable description of the miss limit calculation
        /// </summary>
        public static string GetCalculationBreakdown(GridSizeOption gridSize, WordCountOption wordCount, DifficultySetting difficulty)
        {
            int baseValue = BASE_MISSES;
            int gridBonus = GetGridBonus(gridSize);
            int wordModifier = GetWordCountModifier(wordCount);
            int difficultyModifier = GetDifficultyModifier(difficulty);
            int total = CalculateMissLimit(gridSize, wordCount, difficulty);

            string breakdown = string.Format(
                "Base: {0}\nGrid Bonus ({1}x{1}): +{2}\nWord Modifier ({3} words): {4}\nDifficulty ({5}): {6}\n----------\nTotal: {7} misses",
                baseValue,
                (int)gridSize,
                gridBonus,
                (int)wordCount,
                wordModifier >= 0 ? "+" + wordModifier.ToString() : wordModifier.ToString(),
                difficulty.ToString(),
                difficultyModifier >= 0 ? "+" + difficultyModifier.ToString() : difficultyModifier.ToString(),
                total
            );

            return breakdown;
        }
    }
}
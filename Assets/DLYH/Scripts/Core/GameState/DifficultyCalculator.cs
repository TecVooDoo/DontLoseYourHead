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
        private const int GRID_BONUS_8X8 = 6;
        private const int GRID_BONUS_10X10 = 10;

        // Word count modifiers (fewer words = more empty space = harder)
        private const int WORD_MODIFIER_3_WORDS = 0;    // Baseline
        private const int WORD_MODIFIER_4_WORDS = -2;   // More letters = easier

        // Forgiveness modifiers
        private const int FORGIVENESS_STRICT = -4;
        private const int FORGIVENESS_NORMAL = 0;
        private const int FORGIVENESS_FORGIVING = 4;

        /// <summary>
        /// Calculate the miss limit based on configuration
        /// </summary>
        /// <param name="gridSize">Grid size option</param>
        /// <param name="wordCount">Number of words</param>
        /// <param name="forgiveness">Forgiveness setting</param>
        /// <returns>Calculated miss limit</returns>
        public static int CalculateMissLimit(GridSizeOption gridSize, WordCountOption wordCount, ForgivenessSetting forgiveness)
        {
            int baseValue = BASE_MISSES;
            int gridBonus = GetGridBonus(gridSize);
            int wordModifier = GetWordCountModifier(wordCount);
            int forgivenessModifier = GetForgivenessModifier(forgiveness);

            int total = baseValue + gridBonus + wordModifier + forgivenessModifier;

            // Clamp to reasonable range
            return Mathf.Clamp(total, 10, 35);
        }

        /// <summary>
        /// Calculate miss limit using raw grid size value
        /// </summary>
        public static int CalculateMissLimit(int gridSize, int wordCount, ForgivenessSetting forgiveness)
        {
            GridSizeOption gridOption = GridSizeFromInt(gridSize);
            WordCountOption wordOption = WordCountFromInt(wordCount);
            return CalculateMissLimit(gridOption, wordOption, forgiveness);
        }

        /// <summary>
        /// Get the grid size bonus for miss calculation
        /// </summary>
        public static int GetGridBonus(GridSizeOption gridSize)
        {
            switch (gridSize)
            {
                case GridSizeOption.Small:
                    return GRID_BONUS_6X6;
                case GridSizeOption.Medium:
                    return GRID_BONUS_8X8;
                case GridSizeOption.Large:
                    return GRID_BONUS_10X10;
                default:
                    return GRID_BONUS_6X6;
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
        /// Get the forgiveness modifier for miss calculation
        /// </summary>
        public static int GetForgivenessModifier(ForgivenessSetting forgiveness)
        {
            switch (forgiveness)
            {
                case ForgivenessSetting.Strict:
                    return FORGIVENESS_STRICT;
                case ForgivenessSetting.Normal:
                    return FORGIVENESS_NORMAL;
                case ForgivenessSetting.Forgiving:
                    return FORGIVENESS_FORGIVING;
                default:
                    return FORGIVENESS_NORMAL;
            }
        }

        /// <summary>
        /// Convert int to GridSizeOption
        /// </summary>
        public static GridSizeOption GridSizeFromInt(int size)
        {
            switch (size)
            {
                case 6:
                    return GridSizeOption.Small;
                case 8:
                    return GridSizeOption.Medium;
                case 10:
                    return GridSizeOption.Large;
                default:
                    Debug.LogWarning(string.Format("[DifficultyCalculator] Unknown grid size {0}, defaulting to Small", size));
                    return GridSizeOption.Small;
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
        /// <returns>Array of word lengths needed</returns>
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
        public static string GetCalculationBreakdown(GridSizeOption gridSize, WordCountOption wordCount, ForgivenessSetting forgiveness)
        {
            int baseValue = BASE_MISSES;
            int gridBonus = GetGridBonus(gridSize);
            int wordModifier = GetWordCountModifier(wordCount);
            int forgivenessModifier = GetForgivenessModifier(forgiveness);
            int total = CalculateMissLimit(gridSize, wordCount, forgiveness);

            string breakdown = string.Format(
                "Base: {0}\nGrid Bonus ({1}x{1}): +{2}\nWord Modifier ({3} words): {4}\nForgiveness ({5}): {6}\n----------\nTotal: {7} misses",
                baseValue,
                (int)gridSize,
                gridBonus,
                (int)wordCount,
                wordModifier >= 0 ? "+" + wordModifier.ToString() : wordModifier.ToString(),
                forgiveness.ToString(),
                forgivenessModifier >= 0 ? "+" + forgivenessModifier.ToString() : forgivenessModifier.ToString(),
                total
            );

            return breakdown;
        }
    }
}

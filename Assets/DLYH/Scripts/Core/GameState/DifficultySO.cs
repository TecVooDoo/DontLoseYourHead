using UnityEngine;
using Sirenix.OdinInspector;

namespace TecVooDoo.DontLoseYourHead.Core
{
    /// <summary>
    /// ScriptableObject that defines difficulty configuration for a player.
    /// 
    /// IMPORTANT: This stores a single player's settings. The actual miss limit
    /// is calculated at gameplay start using the opponent's grid/word settings
    /// combined with this player's difficulty preference.
    /// 
    /// See DifficultyCalculator.CalculateMissLimitForPlayer() for the formula.
    /// </summary>
    [CreateAssetMenu(fileName = "NewDifficulty", menuName = "DLYH/Difficulty")]
    public class DifficultySO : ScriptableObject
    {
        [Title("Display Settings")]
        [SerializeField]
        private string _difficultyName = "Custom";

        [Title("Configuration")]
        [SerializeField]
        [EnumToggleButtons]
        private GridSizeOption _gridSizeOption = GridSizeOption.Size8x8;

        [SerializeField]
        [EnumToggleButtons]
        private WordCountOption _wordCountOption = WordCountOption.Three;

        [SerializeField]
        [EnumToggleButtons]
        private DifficultySetting _difficulty = DifficultySetting.Normal;

        [Title("Grid Info (What Opponent Must Guess)")]
        [ShowInInspector]
        [ReadOnly]
        public int GridSize => (int)_gridSizeOption;

        [ShowInInspector]
        [ReadOnly]
        public int WordCount => (int)_wordCountOption;

        /// <summary>
        /// [DEPRECATED] Legacy property for backward compatibility.
        /// Uses the player's own settings to calculate miss limit.
        /// For correct gameplay, use CalculateMissLimitVsOpponent() instead.
        /// </summary>
        [ShowInInspector]
        [ReadOnly]
        public int MissLimit => DifficultyCalculator.CalculateMissLimitForPlayer(
            _difficulty,
            (int)_gridSizeOption,
            (int)_wordCountOption);


        [ShowInInspector]
        [ReadOnly]
        [InfoBox("Miss limit is calculated at gameplay start based on opponent's grid settings + your difficulty preference.")]
        public string DifficultyInfo => string.Format(
            "Your difficulty: {0}\nYour grid: {1}x{1} with {2} words\n\nMiss limit depends on opponent's settings.",
            _difficulty,
            (int)_gridSizeOption,
            (int)_wordCountOption);

        #region Properties

        public string DifficultyName => _difficultyName;
        public GridSizeOption GridSizeOption => _gridSizeOption;
        public WordCountOption WordCountOption => _wordCountOption;
        public DifficultySetting Difficulty => _difficulty;

        /// <summary>
        /// Get the word lengths required for this configuration
        /// </summary>
        public int[] RequiredWordLengths => DifficultyCalculator.GetWordLengths(_wordCountOption);

        #endregion

        #region Miss Limit Calculation

        /// <summary>
        /// Calculate this player's miss limit based on the opponent's settings.
        /// This is the correct way to get miss limit during gameplay.
        /// </summary>
        /// <param name="opponentGridSize">The opponent's grid size</param>
        /// <param name="opponentWordCount">The opponent's word count</param>
        /// <returns>Number of misses allowed for this player</returns>
        public int CalculateMissLimitVsOpponent(int opponentGridSize, int opponentWordCount)
        {
            return DifficultyCalculator.CalculateMissLimitForPlayer(
                _difficulty,
                opponentGridSize,
                opponentWordCount);
        }

        /// <summary>
        /// Calculate this player's miss limit based on opponent's DifficultySO.
        /// </summary>
        public int CalculateMissLimitVsOpponent(DifficultySO opponentSettings)
        {
            if (opponentSettings == null)
            {
                Debug.LogWarning("[DifficultySO] Opponent settings null, using default 8x8/3 words");
                return DifficultyCalculator.CalculateMissLimitForPlayer(_difficulty, 8, 3);
            }

            return DifficultyCalculator.CalculateMissLimitForPlayer(
                _difficulty,
                opponentSettings.GridSize,
                opponentSettings.WordCount);
        }

        /// <summary>
        /// Get a breakdown of the miss limit calculation vs a specific opponent.
        /// </summary>
        public string GetMissLimitBreakdownVsOpponent(int opponentGridSize, int opponentWordCount)
        {
            return DifficultyCalculator.GetCalculationBreakdown(
                _difficulty,
                opponentGridSize,
                opponentWordCount);
        }

        #endregion

        #region Runtime Configuration

        /// <summary>
        /// Set configuration at runtime (for dynamic difficulty)
        /// </summary>
        public void SetConfiguration(GridSizeOption gridSize, WordCountOption wordCount, DifficultySetting difficulty)
        {
            _gridSizeOption = gridSize;
            _wordCountOption = wordCount;
            _difficulty = difficulty;

            Debug.Log(string.Format("[DifficultySO] Configuration updated: {0}x{0} grid, {1} words, {2} difficulty",
                (int)gridSize, (int)wordCount, difficulty));
        }

        /// <summary>
        /// Set configuration using integer values (for UI convenience)
        /// </summary>
        public void SetConfiguration(int gridSize, int wordCount, DifficultySetting difficulty)
        {
            SetConfiguration(
                DifficultyCalculator.GridSizeFromInt(gridSize),
                DifficultyCalculator.WordCountFromInt(wordCount),
                difficulty
            );
        }

        #endregion

        #region Editor Helpers

        [Title("Quick Presets")]
        [Button("Set Easy (6x6, 3 words, Easy)")]
        [GUIColor(0.3f, 1f, 0.3f)]
        private void SetEasyPreset()
        {
            _difficultyName = "Easy";
            _gridSizeOption = GridSizeOption.Size6x6;
            _wordCountOption = WordCountOption.Three;
            _difficulty = DifficultySetting.Easy;
        }

        [Button("Set Medium (8x8, 3 words, Normal)")]
        [GUIColor(1f, 0.8f, 0.3f)]
        private void SetMediumPreset()
        {
            _difficultyName = "Medium";
            _gridSizeOption = GridSizeOption.Size8x8;
            _wordCountOption = WordCountOption.Three;
            _difficulty = DifficultySetting.Normal;
        }

        [Button("Set Hard (10x10, 3 words, Hard)")]
        [GUIColor(1f, 0.3f, 0.3f)]
        private void SetHardPreset()
        {
            _difficultyName = "Hard";
            _gridSizeOption = GridSizeOption.Size10x10;
            _wordCountOption = WordCountOption.Three;
            _difficulty = DifficultySetting.Hard;
        }

        [Button("Set Expert (12x12, 3 words, Hard)")]
        [GUIColor(0.6f, 0.2f, 0.2f)]
        private void SetExpertPreset()
        {
            _difficultyName = "Expert";
            _gridSizeOption = GridSizeOption.Size12x12;
            _wordCountOption = WordCountOption.Three;
            _difficulty = DifficultySetting.Hard;
        }

        #endregion
    }
} 

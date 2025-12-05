using UnityEngine;
using Sirenix.OdinInspector;

namespace TecVooDoo.DontLoseYourHead.Core
{
    /// <summary>
    /// ScriptableObject that defines difficulty configuration for a game
    /// Uses the enum-based system with calculated miss limits
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

        [Title("Calculated Values")]
        [ShowInInspector]
        [ReadOnly]
        public int MissLimit => DifficultyCalculator.CalculateMissLimit(_gridSizeOption, _wordCountOption, _difficulty);

        [ShowInInspector]
        [ReadOnly]
        public int GridSize => (int)_gridSizeOption;

        [ShowInInspector]
        [ReadOnly]
        public int WordCount => (int)_wordCountOption;

        [ShowInInspector]
        [ReadOnly]
        [MultiLineProperty(7)]
        public string CalculationBreakdown => DifficultyCalculator.GetCalculationBreakdown(_gridSizeOption, _wordCountOption, _difficulty);

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

        #region Runtime Configuration

        /// <summary>
        /// Set configuration at runtime (for dynamic difficulty)
        /// </summary>
        public void SetConfiguration(GridSizeOption gridSize, WordCountOption wordCount, DifficultySetting difficulty)
        {
            _gridSizeOption = gridSize;
            _wordCountOption = wordCount;
            _difficulty = difficulty;

            Debug.Log(string.Format("[DifficultySO] Configuration updated: {0}x{0} grid, {1} words, {2} difficulty = {3} misses",
                (int)gridSize, (int)wordCount, difficulty, MissLimit));
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
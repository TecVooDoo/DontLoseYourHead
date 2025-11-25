using UnityEngine;
using Sirenix.OdinInspector;

namespace TecVooDoo.DontLoseYourHead.Core
{
    /// <summary>
    /// ScriptableObject that defines difficulty configuration for a game
    /// Uses the new enum-based system with calculated miss limits
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
        private GridSizeOption _gridSizeOption = GridSizeOption.Small;

        [SerializeField]
        [EnumToggleButtons]
        private WordCountOption _wordCountOption = WordCountOption.Three;

        [SerializeField]
        [EnumToggleButtons]
        private ForgivenessSetting _forgiveness = ForgivenessSetting.Normal;

        [Title("Calculated Values")]
        [ShowInInspector]
        [ReadOnly]
        public int MissLimit => DifficultyCalculator.CalculateMissLimit(_gridSizeOption, _wordCountOption, _forgiveness);

        [ShowInInspector]
        [ReadOnly]
        public int GridSize => (int)_gridSizeOption;

        [ShowInInspector]
        [ReadOnly]
        public int WordCount => (int)_wordCountOption;

        [ShowInInspector]
        [ReadOnly]
        [MultiLineProperty(7)]
        public string CalculationBreakdown => DifficultyCalculator.GetCalculationBreakdown(_gridSizeOption, _wordCountOption, _forgiveness);

        #region Properties

        public string DifficultyName => _difficultyName;
        public GridSizeOption GridSizeOption => _gridSizeOption;
        public WordCountOption WordCountOption => _wordCountOption;
        public ForgivenessSetting Forgiveness => _forgiveness;

        /// <summary>
        /// Get the word lengths required for this configuration
        /// </summary>
        public int[] RequiredWordLengths => DifficultyCalculator.GetWordLengths(_wordCountOption);

        #endregion

        #region Runtime Configuration

        /// <summary>
        /// Set configuration at runtime (for dynamic difficulty)
        /// </summary>
        public void SetConfiguration(GridSizeOption gridSize, WordCountOption wordCount, ForgivenessSetting forgiveness)
        {
            _gridSizeOption = gridSize;
            _wordCountOption = wordCount;
            _forgiveness = forgiveness;
            
            Debug.Log(string.Format("[DifficultySO] Configuration updated: {0}x{0} grid, {1} words, {2} forgiveness = {3} misses",
                (int)gridSize, (int)wordCount, forgiveness, MissLimit));
        }

        /// <summary>
        /// Set configuration using integer values (for UI convenience)
        /// </summary>
        public void SetConfiguration(int gridSize, int wordCount, ForgivenessSetting forgiveness)
        {
            SetConfiguration(
                DifficultyCalculator.GridSizeFromInt(gridSize),
                DifficultyCalculator.WordCountFromInt(wordCount),
                forgiveness
            );
        }

        #endregion

        #region Editor Helpers

        [Title("Quick Presets")]
        [Button("Set Easy (6x6, 3 words, Forgiving)")]
        [GUIColor(0.3f, 1f, 0.3f)]
        private void SetEasyPreset()
        {
            _difficultyName = "Easy";
            _gridSizeOption = GridSizeOption.Small;
            _wordCountOption = WordCountOption.Three;
            _forgiveness = ForgivenessSetting.Forgiving;
        }

        [Button("Set Medium (8x8, 3 words, Normal)")]
        [GUIColor(1f, 0.8f, 0.3f)]
        private void SetMediumPreset()
        {
            _difficultyName = "Medium";
            _gridSizeOption = GridSizeOption.Medium;
            _wordCountOption = WordCountOption.Three;
            _forgiveness = ForgivenessSetting.Normal;
        }

        [Button("Set Hard (10x10, 3 words, Strict)")]
        [GUIColor(1f, 0.3f, 0.3f)]
        private void SetHardPreset()
        {
            _difficultyName = "Hard";
            _gridSizeOption = GridSizeOption.Large;
            _wordCountOption = WordCountOption.Three;
            _forgiveness = ForgivenessSetting.Strict;
        }

        #endregion
    }
}

using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Sirenix.OdinInspector;

namespace TecVooDoo.DontLoseYourHead.UI
{
    /// <summary>
    /// UI controller for the difficulty selection screen
    /// Allows players to choose grid size, word count, and forgiveness settings
    /// </summary>
    public class DifficultySelectionUI : MonoBehaviour
    {
        #region Serialized Fields
        [Title("References")]
        [Required]
        [SerializeField] private Core.GameStateMachine _gameStateMachine;

        [Required]
        [SerializeField] private Core.GameManager _gameManager;
        
        [Required]
        [SerializeField] private Core.DifficultySO _playerDifficulty;

        [Title("UI Elements")]
        [Required]
        [SerializeField] private TMP_Dropdown _gridSizeDropdown;
        
        [Required]
        [SerializeField] private TMP_Dropdown _wordCountDropdown;
        
        [Required]
        [SerializeField] private TMP_Dropdown _forgivenessDropdown;
        
        [Required]
        [SerializeField] private TextMeshProUGUI _missLimitPreviewText;
        
        [Required]
        [SerializeField] private Button _continueButton;

        [Title("Display Settings")]
        [SerializeField] private string _missLimitFormat = "Miss Limit: {0}";
        #endregion

        #region Unity Lifecycle
        private void Start()
        {
            SetupDropdowns();
            UpdateMissLimitPreview();
            
            // Add listeners
            _gridSizeDropdown.onValueChanged.AddListener(OnDropdownChanged);
            _wordCountDropdown.onValueChanged.AddListener(OnDropdownChanged);
            _forgivenessDropdown.onValueChanged.AddListener(OnDropdownChanged);
            _continueButton.onClick.AddListener(OnContinueClicked);
        }

        private void OnDestroy()
        {
            // Remove listeners
            _gridSizeDropdown.onValueChanged.RemoveListener(OnDropdownChanged);
            _wordCountDropdown.onValueChanged.RemoveListener(OnDropdownChanged);
            _forgivenessDropdown.onValueChanged.RemoveListener(OnDropdownChanged);
            _continueButton.onClick.RemoveListener(OnContinueClicked);
        }
        #endregion

        #region Setup
        private void SetupDropdowns()
        {
            // Grid Size dropdown
            _gridSizeDropdown.ClearOptions();
            _gridSizeDropdown.AddOptions(new System.Collections.Generic.List<string>
            {
                "6x6 (Small)",
                "8x8 (Medium)",
                "10x10 (Large)"
            });
            _gridSizeDropdown.value = 1; // Default to Medium (8x8)

            // Word Count dropdown
            _wordCountDropdown.ClearOptions();
            _wordCountDropdown.AddOptions(new System.Collections.Generic.List<string>
            {
                "3 Words",
                "4 Words"
            });
            _wordCountDropdown.value = 0; // Default to 3 words

            // Forgiveness dropdown
            _forgivenessDropdown.ClearOptions();
            _forgivenessDropdown.AddOptions(new System.Collections.Generic.List<string>
            {
                "Strict",
                "Normal",
                "Forgiving"
            });
            _forgivenessDropdown.value = 1; // Default to Normal
        }
        #endregion

        #region Event Handlers
        private void OnDropdownChanged(int value)
        {
            UpdateMissLimitPreview();
        }

        private void OnContinueClicked()
        {
            ApplyDifficultySettings();

            // Initialize game grids after difficulty selection
            if (_gameManager != null)
            {
                _gameManager.InitializeAfterDifficulty();
            }

            _gameStateMachine.OnDifficultySelected();
        }
        #endregion

        #region Private Methods
        private void UpdateMissLimitPreview()
        {
            Core.GridSizeOption gridSize = GetSelectedGridSize();
            Core.WordCountOption wordCount = GetSelectedWordCount();
            Core.ForgivenessSetting forgiveness = GetSelectedForgiveness();

            int missLimit = Core.DifficultyCalculator.CalculateMissLimit(gridSize, wordCount, forgiveness);
            
            _missLimitPreviewText.text = string.Format(_missLimitFormat, missLimit);
        }

        private void ApplyDifficultySettings()
        {
            Core.GridSizeOption gridSize = GetSelectedGridSize();
            Core.WordCountOption wordCount = GetSelectedWordCount();
            Core.ForgivenessSetting forgiveness = GetSelectedForgiveness();

            _playerDifficulty.SetConfiguration(gridSize, wordCount, forgiveness);
            
            Debug.Log($"[DifficultySelectionUI] Applied settings: {gridSize}, {wordCount} words, {forgiveness}");
        }

        private Core.GridSizeOption GetSelectedGridSize()
        {
            return _gridSizeDropdown.value switch
            {
                0 => Core.GridSizeOption.Small,
                1 => Core.GridSizeOption.Medium,
                2 => Core.GridSizeOption.Large,
                _ => Core.GridSizeOption.Medium
            };
        }

        private Core.WordCountOption GetSelectedWordCount()
        {
            return _wordCountDropdown.value switch
            {
                0 => Core.WordCountOption.Three,
                1 => Core.WordCountOption.Four,
                _ => Core.WordCountOption.Three
            };
        }

        private Core.ForgivenessSetting GetSelectedForgiveness()
        {
            return _forgivenessDropdown.value switch
            {
                0 => Core.ForgivenessSetting.Strict,
                1 => Core.ForgivenessSetting.Normal,
                2 => Core.ForgivenessSetting.Forgiving,
                _ => Core.ForgivenessSetting.Normal
            };
        }
        #endregion

        #region Editor Buttons
        [Title("Testing")]
        [Button("Test: Show Easy Settings")]
        private void TestEasySettings()
        {
            _gridSizeDropdown.value = 0;
            _wordCountDropdown.value = 0;
            _forgivenessDropdown.value = 2;
            UpdateMissLimitPreview();
        }

        [Button("Test: Show Hard Settings")]
        private void TestHardSettings()
        {
            _gridSizeDropdown.value = 2;
            _wordCountDropdown.value = 0;
            _forgivenessDropdown.value = 0;
            UpdateMissLimitPreview();
        }
        #endregion
    }
}

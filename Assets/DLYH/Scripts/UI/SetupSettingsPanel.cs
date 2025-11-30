using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Sirenix.OdinInspector;
using System;
using System.Collections.Generic;

namespace TecVooDoo.DontLoseYourHead.UI
{
    /// <summary>
    /// UI panel for player setup settings including name, color, difficulty options,
    /// and random word selection. Works alongside PlayerGridPanel during Setup phase.
    /// </summary>
    public class SetupSettingsPanel : MonoBehaviour
    {
        #region Serialized Fields - References
        [TitleGroup("References")]
        [SerializeField, Required]
        private PlayerGridPanel _playerGridPanel;

        [SerializeField]
        private Core.DifficultySO _difficultySO;

        [TitleGroup("Word Lists")]
        [SerializeField, Required]
        private Core.WordListSO _threeLetterWords;

        [SerializeField, Required]
        private Core.WordListSO _fourLetterWords;

        [SerializeField, Required]
        private Core.WordListSO _fiveLetterWords;

        [SerializeField]
        private Core.WordListSO _sixLetterWords;
        #endregion

        #region Serialized Fields - Player Configuration UI
        [TitleGroup("Player Configuration")]
        [SerializeField, Required]
        private TMP_InputField _playerNameInput;

        [SerializeField, Required]
        private Button _editNameButton;

        [SerializeField, Required]
        private Image _playerColorDisplay;

        [SerializeField, Required]
        private Button _colorPickerButton;

        [SerializeField, Required]
        private Button _randomWordsButton;

        [SerializeField]
        private GameObject _colorPickerPanel;

        [SerializeField]
        private List<Button> _colorOptionButtons = new List<Button>();
        #endregion

        #region Serialized Fields - Difficulty Settings UI
        [TitleGroup("Difficulty Settings")]
        [SerializeField, Required]
        private TMP_Dropdown _gridSizeDropdown;

        [SerializeField, Required]
        private TMP_Dropdown _wordCountDropdown;

        [SerializeField, Required]
        private TMP_Dropdown _forgivenessDropdown;

        [SerializeField]
        private TextMeshProUGUI _missLimitText;
        #endregion

        #region Serialized Fields - Top Bar
        [TitleGroup("Top Bar")]
        [SerializeField]
        private Button _mainMenuButton;

        [SerializeField]
        private TextMeshProUGUI _phaseLabel;

        [SerializeField]
        private Button _startButton;
        #endregion

        #region Serialized Fields - Color Options
        [TitleGroup("Available Colors")]
        [SerializeField]
        private List<Color> _availableColors = new List<Color>
        {
            new Color(0.2f, 0.6f, 1f, 1f),    // Blue
            new Color(0.4f, 0.8f, 0.4f, 1f),  // Green
            new Color(1f, 0.6f, 0.2f, 1f),    // Orange
            new Color(0.8f, 0.4f, 0.8f, 1f),  // Purple
            new Color(1f, 0.8f, 0.2f, 1f),    // Yellow
            new Color(0.2f, 0.8f, 0.8f, 1f),  // Cyan
            new Color(1f, 0.5f, 0.7f, 1f),    // Pink
            new Color(0.6f, 0.4f, 0.2f, 1f)   // Brown
        };
        #endregion

        #region Private Fields
        private string _playerName = "PLAYER1";
        private Color _playerColor;
        private int _gridSize = 8; // Now stores raw grid size (6-12)
        private Core.WordCountOption _wordCount = Core.WordCountOption.Three;
        private Core.ForgivenessSetting _forgiveness = Core.ForgivenessSetting.Normal;
        private int _currentPlayerIndex = 0;
        private bool _settingsLocked = false;
        #endregion

        #region Events
        /// <summary>
        /// Fired when player clicks Main Menu button
        /// </summary>
        public event Action OnMainMenuRequested;

        /// <summary>
        /// Fired when player clicks Start button (setup complete)
        /// </summary>
        public event Action OnStartRequested;

        /// <summary>
        /// Fired when grid size changes - PlayerGridPanel should respond
        /// </summary>
        public event Action<int> OnGridSizeChanged;

        /// <summary>
        /// Fired when word count changes - affects required word rows
        /// </summary>
        public event Action<Core.WordCountOption> OnWordCountChanged;

        /// <summary>
        /// Fired when player settings change
        /// </summary>
        public event Action<string, Color> OnPlayerSettingsChanged;
        #endregion

        #region Properties
        public string PlayerName => _playerName;
        public Color PlayerColor => _playerColor;
        public int GridSize => _gridSize;
        public Core.WordCountOption WordCount => _wordCount;
        public Core.ForgivenessSetting Forgiveness => _forgiveness;
        public int CurrentPlayerIndex => _currentPlayerIndex;
        public bool SettingsLocked => _settingsLocked;
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            // Set default color
            if (_availableColors.Count > 0)
            {
                _playerColor = _availableColors[0];
            }
        }

        private void Start()
        {
            SetupDropdowns();
            SetupButtons();
            UpdateUI();
            UpdateMissLimitDisplay();
        }

        private void OnDestroy()
        {
            RemoveListeners();
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Initialize the panel for a specific player
        /// </summary>
        public void Initialize(int playerIndex, string defaultName = null)
        {
            _currentPlayerIndex = playerIndex;
            _playerName = defaultName ?? $"PLAYER{playerIndex + 1}";
            _settingsLocked = false;

            // Assign a default color based on player index
            if (_availableColors.Count > playerIndex)
            {
                _playerColor = _availableColors[playerIndex];
            }

            UpdateUI();
            UpdateStartButtonState();
        }

        /// <summary>
        /// Lock settings (called when player starts placing words)
        /// </summary>
        public void LockGridAndWordSettings()
        {
            _settingsLocked = true;
            UpdateDropdownInteractivity();
        }

        /// <summary>
        /// Unlock settings (called if player wants to restart setup)
        /// </summary>
        public void UnlockSettings()
        {
            _settingsLocked = false;
            UpdateDropdownInteractivity();
        }

        /// <summary>
        /// Apply current settings to DifficultySO
        /// </summary>
        public void ApplySettingsToSO()
        {
            if (_difficultySO != null)
            {
                // Convert raw grid size to closest enum option for DifficultySO
                Core.GridSizeOption gridSizeOption = _gridSize switch
                {
                    <= 6 => Core.GridSizeOption.Small,
                    <= 8 => Core.GridSizeOption.Medium,
                    _ => Core.GridSizeOption.Large
                };

                _difficultySO.SetConfiguration(gridSizeOption, _wordCount, _forgiveness);
                Debug.Log($"[SetupSettingsPanel] Applied settings to DifficultySO: {_gridSize}x{_gridSize} -> {gridSizeOption}, {_wordCount}, {_forgiveness}");
            }
        }

        /// <summary>
        /// Check if Start button should be enabled
        /// </summary>
        public void UpdateStartButtonState()
        {
            if (_startButton == null) return;

            bool canStart = _playerGridPanel != null && _playerGridPanel.AreAllWordsPlaced();
            _startButton.interactable = canStart;
        }

        /// <summary>
        /// Fill word rows with random valid words from word bank
        /// </summary>
        [Button("Pick Random Words")]
        public void PickRandomWords()
        {
            if (_playerGridPanel == null)
            {
                Debug.LogError("[SetupSettingsPanel] PlayerGridPanel not assigned!");
                return;
            }

            int[] wordLengths = Core.DifficultyCalculator.GetWordLengths(_wordCount);

            for (int i = 0; i < wordLengths.Length && i < _playerGridPanel.WordRowCount; i++)
            {
                var row = _playerGridPanel.GetWordPatternRow(i);
                if (row == null) continue;

                // Skip rows that already have placed words
                if (row.IsPlaced) continue;

                string randomWord = GetRandomWordOfLength(wordLengths[i]);
                if (!string.IsNullOrEmpty(randomWord))
                {
                    row.SetEnteredText(randomWord);
                    Debug.Log($"[SetupSettingsPanel] Random word for row {i + 1}: {randomWord}");
                }
            }
        }
        #endregion

        #region Private Methods - Setup
        private void SetupDropdowns()
        {
            // Grid Size dropdown - supports 6x6 through 12x12
            if (_gridSizeDropdown != null)
            {
                _gridSizeDropdown.ClearOptions();
                _gridSizeDropdown.AddOptions(new List<string>
                {
                    "6x6",
                    "7x7",
                    "8x8",
                    "9x9",
                    "10x10",
                    "11x11",
                    "12x12"
                });
                _gridSizeDropdown.value = 2; // Default to 8x8 (index 2)
                _gridSizeDropdown.onValueChanged.AddListener(OnGridSizeDropdownChanged);
            }

            // Word Count dropdown
            if (_wordCountDropdown != null)
            {
                _wordCountDropdown.ClearOptions();
                _wordCountDropdown.AddOptions(new List<string>
                {
                    "3 Words",
                    "4 Words"
                });
                _wordCountDropdown.value = 0; // Default to 3 words
                _wordCountDropdown.onValueChanged.AddListener(OnWordCountDropdownChanged);
            }

            // Forgiveness dropdown
            if (_forgivenessDropdown != null)
            {
                _forgivenessDropdown.ClearOptions();
                _forgivenessDropdown.AddOptions(new List<string>
                {
                    "Strict",
                    "Normal",
                    "Forgiving"
                });
                _forgivenessDropdown.value = 1; // Default to Normal
                _forgivenessDropdown.onValueChanged.AddListener(OnForgivenessDropdownChanged);
            }
        }

        private void SetupButtons()
        {
            if (_editNameButton != null)
            {
                _editNameButton.onClick.AddListener(OnEditNameClicked);
            }

            if (_colorPickerButton != null)
            {
                _colorPickerButton.onClick.AddListener(OnColorPickerClicked);
            }

            if (_randomWordsButton != null)
            {
                _randomWordsButton.onClick.AddListener(PickRandomWords);
            }

            if (_mainMenuButton != null)
            {
                _mainMenuButton.onClick.AddListener(OnMainMenuClicked);
            }

            if (_startButton != null)
            {
                _startButton.onClick.AddListener(OnStartClicked);
            }

            if (_playerNameInput != null)
            {
                _playerNameInput.onEndEdit.AddListener(OnPlayerNameChanged);
            }

            // Setup color option buttons
            for (int i = 0; i < _colorOptionButtons.Count && i < _availableColors.Count; i++)
            {
                int colorIndex = i; // Capture for lambda
                var button = _colorOptionButtons[i];
                if (button != null)
                {
                    // Set button color
                    var buttonImage = button.GetComponent<Image>();
                    if (buttonImage != null)
                    {
                        buttonImage.color = _availableColors[colorIndex];
                    }

                    button.onClick.AddListener(() => OnColorSelected(colorIndex));
                }
            }
        }

        private void RemoveListeners()
        {
            if (_gridSizeDropdown != null)
                _gridSizeDropdown.onValueChanged.RemoveListener(OnGridSizeDropdownChanged);

            if (_wordCountDropdown != null)
                _wordCountDropdown.onValueChanged.RemoveListener(OnWordCountDropdownChanged);

            if (_forgivenessDropdown != null)
                _forgivenessDropdown.onValueChanged.RemoveListener(OnForgivenessDropdownChanged);

            if (_editNameButton != null)
                _editNameButton.onClick.RemoveListener(OnEditNameClicked);

            if (_colorPickerButton != null)
                _colorPickerButton.onClick.RemoveListener(OnColorPickerClicked);

            if (_randomWordsButton != null)
                _randomWordsButton.onClick.RemoveListener(PickRandomWords);

            if (_mainMenuButton != null)
                _mainMenuButton.onClick.RemoveListener(OnMainMenuClicked);

            if (_startButton != null)
                _startButton.onClick.RemoveListener(OnStartClicked);

            if (_playerNameInput != null)
                _playerNameInput.onEndEdit.RemoveListener(OnPlayerNameChanged);

            foreach (var button in _colorOptionButtons)
            {
                if (button != null)
                    button.onClick.RemoveAllListeners();
            }
        }
        #endregion

        #region Private Methods - Event Handlers
        private void OnGridSizeDropdownChanged(int value)
        {
            // Dropdown index 0-6 maps to grid sizes 6-12
            _gridSize = value + 6;

            UpdateMissLimitDisplay();
            OnGridSizeChanged?.Invoke(_gridSize);

            // Update PlayerGridPanel grid size
            if (_playerGridPanel != null)
            {
                _playerGridPanel.SetGridSize(_gridSize);
            }

            Debug.Log($"[SetupSettingsPanel] Grid size changed to: {_gridSize}x{_gridSize}");
        }

        private void OnWordCountDropdownChanged(int value)
        {
            _wordCount = value switch
            {
                0 => Core.WordCountOption.Three,
                1 => Core.WordCountOption.Four,
                _ => Core.WordCountOption.Three
            };

            UpdateMissLimitDisplay();
            OnWordCountChanged?.Invoke(_wordCount);

            // Update PlayerGridPanel word rows
            if (_playerGridPanel != null)
            {
                int[] wordLengths = Core.DifficultyCalculator.GetWordLengths(_wordCount);
                _playerGridPanel.SetWordLengths(wordLengths);
            }

            Debug.Log($"[SetupSettingsPanel] Word count changed to: {_wordCount}");
        }

        private void OnForgivenessDropdownChanged(int value)
        {
            _forgiveness = value switch
            {
                0 => Core.ForgivenessSetting.Strict,
                1 => Core.ForgivenessSetting.Normal,
                2 => Core.ForgivenessSetting.Forgiving,
                _ => Core.ForgivenessSetting.Normal
            };

            UpdateMissLimitDisplay();
            Debug.Log($"[SetupSettingsPanel] Forgiveness changed to: {_forgiveness}");
        }

        private void OnEditNameClicked()
        {
            if (_playerNameInput != null)
            {
                _playerNameInput.interactable = true;
                _playerNameInput.Select();
                _playerNameInput.ActivateInputField();
            }
        }

        private void OnPlayerNameChanged(string newName)
        {
            if (string.IsNullOrWhiteSpace(newName))
            {
                newName = $"PLAYER{_currentPlayerIndex + 1}";
            }

            _playerName = newName.ToUpper();

            if (_playerNameInput != null)
            {
                _playerNameInput.text = _playerName;
            }

            OnPlayerSettingsChanged?.Invoke(_playerName, _playerColor);
            Debug.Log($"[SetupSettingsPanel] Player name changed to: {_playerName}");
        }

        private void OnColorPickerClicked()
        {
            if (_colorPickerPanel != null)
            {
                _colorPickerPanel.SetActive(!_colorPickerPanel.activeSelf);
            }
        }

        private void OnColorSelected(int colorIndex)
        {
            if (colorIndex >= 0 && colorIndex < _availableColors.Count)
            {
                _playerColor = _availableColors[colorIndex];
                UpdateColorDisplay();

                // Hide color picker panel
                if (_colorPickerPanel != null)
                {
                    _colorPickerPanel.SetActive(false);
                }

                OnPlayerSettingsChanged?.Invoke(_playerName, _playerColor);
                Debug.Log($"[SetupSettingsPanel] Player color changed");
            }
        }

        private void OnMainMenuClicked()
        {
            OnMainMenuRequested?.Invoke();
        }

        private void OnStartClicked()
        {
            if (_playerGridPanel != null && _playerGridPanel.AreAllWordsPlaced())
            {
                ApplySettingsToSO();
                OnStartRequested?.Invoke();
            }
            else
            {
                Debug.LogWarning("[SetupSettingsPanel] Cannot start - not all words are placed!");
            }
        }
        #endregion

        #region Private Methods - UI Updates
        private void UpdateUI()
        {
            // Update player name input
            if (_playerNameInput != null)
            {
                _playerNameInput.text = _playerName;
            }

            // Update color display
            UpdateColorDisplay();

            // Update phase label
            if (_phaseLabel != null)
            {
                _phaseLabel.text = "SETUP";
            }

            UpdateDropdownInteractivity();
        }

        private void UpdateColorDisplay()
        {
            if (_playerColorDisplay != null)
            {
                _playerColorDisplay.color = _playerColor;
            }
        }

        private void UpdateMissLimitDisplay()
        {
            if (_missLimitText != null)
            {
                int missLimit = CalculateMissLimitForGridSize(_gridSize, _wordCount, _forgiveness);
                _missLimitText.text = $"Miss Limit: {missLimit}";
            }
        }

        /// <summary>
        /// Calculate miss limit for any grid size 6-12.
        /// Extends the existing formula to support intermediate sizes.
        /// </summary>
        private int CalculateMissLimitForGridSize(int gridSize, Core.WordCountOption wordCount, Core.ForgivenessSetting forgiveness)
        {
            // Base misses
            int baseMisses = 15;

            // Grid bonus scales linearly: 6x6=3, 7x7=4, 8x8=6, 9x9=7, 10x10=10, 11x11=11, 12x12=13
            // Formula: roughly (gridSize - 6) + some extra for larger grids
            int gridBonus;
            if (gridSize <= 6)
                gridBonus = 3;
            else if (gridSize <= 8)
                gridBonus = 3 + (gridSize - 6); // 7=4, 8=5... but we want 8=6
            else
                gridBonus = (gridSize - 6) + (gridSize - 8); // Accelerates for larger grids

            // Adjust to match original formula better: 6=3, 8=6, 10=10
            gridBonus = Mathf.RoundToInt((gridSize - 6) * 1.75f) + 3;
            gridBonus = Mathf.Clamp(gridBonus, 3, 15);

            // Word modifier
            int wordModifier = wordCount == Core.WordCountOption.Four ? -2 : 0;

            // Forgiveness modifier
            int forgivenessModifier = forgiveness switch
            {
                Core.ForgivenessSetting.Strict => -4,
                Core.ForgivenessSetting.Normal => 0,
                Core.ForgivenessSetting.Forgiving => 4,
                _ => 0
            };

            int total = baseMisses + gridBonus + wordModifier + forgivenessModifier;
            return Mathf.Clamp(total, 10, 40);
        }

        private void UpdateDropdownInteractivity()
        {
            // Grid size and word count locked once word placement starts
            if (_gridSizeDropdown != null)
            {
                _gridSizeDropdown.interactable = !_settingsLocked;
            }

            if (_wordCountDropdown != null)
            {
                _wordCountDropdown.interactable = !_settingsLocked;
            }

            // Forgiveness can always be changed
            if (_forgivenessDropdown != null)
            {
                _forgivenessDropdown.interactable = true;
            }
        }
        #endregion

        #region Private Methods - Word Selection
        private string GetRandomWordOfLength(int length)
        {
            Core.WordListSO wordList = length switch
            {
                3 => _threeLetterWords,
                4 => _fourLetterWords,
                5 => _fiveLetterWords,
                6 => _sixLetterWords,
                _ => null
            };

            if (wordList != null && wordList.Count > 0)
            {
                return wordList.GetRandomWord();
            }

            Debug.LogWarning($"[SetupSettingsPanel] No word list available for {length}-letter words");
            return string.Empty;
        }
        #endregion

        #region Editor Helpers
#if UNITY_EDITOR
        [TitleGroup("Debug")]
        [Button("Test Random Words")]
        private void TestRandomWords()
        {
            PickRandomWords();
        }

        [Button("Log Current Settings")]
        private void LogCurrentSettings()
        {
            Debug.Log($"[SetupSettingsPanel] Player: {_playerName}");
            Debug.Log($"[SetupSettingsPanel] Grid Size: {_gridSize}x{_gridSize}");
            Debug.Log($"[SetupSettingsPanel] Word Count: {_wordCount}");
            Debug.Log($"[SetupSettingsPanel] Forgiveness: {_forgiveness}");
            Debug.Log($"[SetupSettingsPanel] Miss Limit: {CalculateMissLimitForGridSize(_gridSize, _wordCount, _forgiveness)}");
        }

        [Button("Test Apply Settings")]
        private void TestApplySettings()
        {
            ApplySettingsToSO();
        }
#endif
        #endregion
    }
}
using Sirenix.OdinInspector;
using System;
using System.Collections.Generic;
using TecVooDoo.DontLoseYourHead.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;


namespace TecVooDoo.DontLoseYourHead.UI
{
    /// <summary>
    /// Manages the Setup Phase settings panel including:
    /// - Difficulty settings (Grid Size, Word Count, Difficulty)
    /// - Player settings (Name, Color)
    /// - Word validation setup
    /// - Miss limit display
    /// - Invalid word feedback (toast notifications)
    /// - Letter tracker routing to player name input
    /// </summary>
    public class SetupSettingsPanel : MonoBehaviour
    {
        #region Serialized Fields

        [TitleGroup("UI References")]
        [SerializeField, Required]
        private TMP_Dropdown _gridSizeDropdown;

        [SerializeField, Required]
        private TMP_Dropdown _wordCountDropdown;

        [SerializeField, Required]
        private TMP_Dropdown _difficultyDropdown;

        [SerializeField, Required]
        private TMP_InputField _playerNameInput;

        [SerializeField, Tooltip("Container holding individual color button choices")]
        private Transform _colorButtonsContainer;

        [SerializeField]
        private Button _pickRandomWordsButton;

        [SerializeField]
        private Button _placeRandomPositionsButton;

        [SerializeField]
        private TextMeshProUGUI _missLimitDisplay;

        [SerializeField]
        private Button _startGameButton;

        [TitleGroup("Player Grid Reference")]
        [SerializeField, Required]
        private PlayerGridPanel _playerGridPanel;

        [TitleGroup("Word Lists")]
        [SerializeField, Required]
        private WordListSO _threeLetterWords;

        [SerializeField, Required]
        private WordListSO _fourLetterWords;

        [SerializeField, Required]
        private WordListSO _fiveLetterWords;

        [SerializeField]
        private WordListSO _sixLetterWords;

        [TitleGroup("Player Colors")]
        [SerializeField]
        private Color[] _availableColors = new Color[]
        {
            new Color(0.4f, 0.6f, 0.9f, 1f),   // Blue
            new Color(0.9f, 0.5f, 0.7f, 1f),   // Pink
            new Color(0.5f, 0.8f, 0.5f, 1f),   // Green
            new Color(0.9f, 0.7f, 0.4f, 1f),   // Orange
            new Color(0.7f, 0.5f, 0.9f, 1f),   // Purple
            new Color(0.4f, 0.8f, 0.8f, 1f)    // Cyan
        };

        [TitleGroup("Invalid Word Toast Settings")]
        [SerializeField]
        private float _toastDuration = 2.0f;

        [SerializeField]
        private string _invalidWordMessage = "'{0}' is not a valid word!";

        [TitleGroup("Dynamic Sizing")]
        [SerializeField, Tooltip("The main content container with VerticalLayoutGroup")]
        private RectTransform _contentContainer;

        [SerializeField]
        private float _minFontSize = 14f;

        [SerializeField]
        private float _maxFontSize = 24f;

        [SerializeField]
        private float _minElementHeight = 35f;

        [SerializeField]
        private float _maxElementHeight = 60f;

        [SerializeField]
        private float _minSpacing = 8f;

        [SerializeField]
        private float _maxSpacing = 20f;

        #endregion

        #region Private Fields

        private int _gridSize = 8;
        private WordCountOption _wordCount = WordCountOption.Four;
        private DifficultySetting _difficulty = DifficultySetting.Normal;
        private string _playerName = "PLAYER1";
        private Color _playerColor;
        private int _currentColorIndex = 0;
        private int _currentPlayerIndex = 0;
        private bool _isNameInputFocused = false;

        // Controller for color button management
        private PlayerColorController _playerColorController;

        // Word validation service
        private WordValidationService _wordValidationService;

        #endregion

        #region Events

        /// <summary>Fired when grid size setting changes</summary>
        public event Action<int> OnGridSizeChanged;

        /// <summary>Fired when word count setting changes</summary>
        public event Action<WordCountOption> OnWordCountChanged;

        /// <summary>Fired when player name or color changes</summary>
        public event Action<string, Color> OnPlayerSettingsChanged;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            ValidateReferences();
            SetDefaultValues();
        }

        private void Start()
        {
            SetupDropdowns();
            SetupButtons();
            SetupWordValidationService();
            SetupWordValidation();
            SetupInvalidWordFeedback();
            InitializeWordRows();
            UpdateMissLimitDisplay();

            //ApplyDynamicSizing();


            // Subscribe to letter input events for routing to name field
            if (_playerGridPanel != null)
            {
                _playerGridPanel.OnLetterInput += OnLetterInputReceived;
            }

            // Re-subscribe after a frame to ensure PlayerGridPanel word rows are cached
            StartCoroutine(DelayedEventSubscription());
        }

        /// <summary>
        /// Delays event subscription by one frame to ensure PlayerGridPanel is fully initialized
        /// </summary>
        private System.Collections.IEnumerator DelayedEventSubscription()
        {
            yield return null; // Wait one frame

            UnsubscribeInvalidWordFeedback();
            SetupInvalidWordFeedback();
            UpdatePickRandomWordsButtonState();
            UpdatePlaceRandomPositionsButtonState();
            UpdateStartButtonState();

            Debug.Log("[SetupSettingsPanel] Re-subscribed to word row events after frame delay");
        }

        private void OnDestroy()
        {
            RemoveListeners();

            // Unsubscribe from letter input events
            if (_playerGridPanel != null)
            {
                _playerGridPanel.OnLetterInput -= OnLetterInputReceived;
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Sets up the panel for a specific player
        /// </summary>
        public void SetupForPlayer(int playerIndex, PlayerSO playerData = null)
        {
            _currentPlayerIndex = playerIndex;

            if (playerData != null)
            {
                _playerName = playerData.PlayerName;
                // Note: PlayerSO doesn't have PlayerColor - use default color logic
            }
            else
            {
                _playerName = $"PLAYER{playerIndex + 1}";
                _playerColor = _availableColors[playerIndex % _availableColors.Length];
                _currentColorIndex = playerIndex % _availableColors.Length;
            }

            UpdateUI();
        }

        /// <summary>
        /// Gets the current difficulty settings
        /// </summary>
        public (int gridSize, WordCountOption wordCount, DifficultySetting difficulty) GetDifficultySettings()
        {
            return (_gridSize, _wordCount, _difficulty);
        }

        /// <summary>
        /// Gets the current player settings
        /// </summary>
        public (string name, Color color) GetPlayerSettings()
        {
            return (_playerName, _playerColor);
        }

        #endregion

        #region Setup Methods

        private void ValidateReferences()
        {
            if (_gridSizeDropdown == null)
                Debug.LogError("[SetupSettingsPanel] Grid Size Dropdown is not assigned!");
            if (_wordCountDropdown == null)
                Debug.LogError("[SetupSettingsPanel] Word Count Dropdown is not assigned!");
            if (_difficultyDropdown == null)
                Debug.LogError("[SetupSettingsPanel] Difficulty Dropdown is not assigned!");
            if (_playerNameInput == null)
                Debug.LogError("[SetupSettingsPanel] Player Name Input is not assigned!");
            if (_playerGridPanel == null)
                Debug.LogError("[SetupSettingsPanel] Player Grid Panel is not assigned!");
            if (_missLimitDisplay == null)
                Debug.LogWarning("[SetupSettingsPanel] Miss Limit Display is not assigned - miss limit won't be shown!");
            if (_pickRandomWordsButton == null)
                Debug.LogWarning("[SetupSettingsPanel] Pick Random Words Button is not assigned!");
            if (_colorButtonsContainer == null)
                Debug.LogWarning("[SetupSettingsPanel] Color Buttons Container is not assigned - color selection won't work!");
        }

        private void SetDefaultValues()
        {
            _gridSize = 6;
            _wordCount = WordCountOption.Four;
            _difficulty = DifficultySetting.Normal;
            _playerName = "PLAYER1";
            _playerColor = _availableColors.Length > 0 ? _availableColors[0] : Color.cyan;
        }

        private void SetupDropdowns()
        {
            // Grid Size Dropdown (6x6 through 12x12)
            if (_gridSizeDropdown != null)
            {
                _gridSizeDropdown.ClearOptions();
                _gridSizeDropdown.AddOptions(new System.Collections.Generic.List<string>
                {
                    "6x6",
                    "7x7",
                    "8x8",
                    "9x9",
                    "10x10",
                    "11x11",
                    "12x12"
                });
                _gridSizeDropdown.value = 0; // Default to 6x6 (index 0)
                _gridSizeDropdown.onValueChanged.AddListener(OnGridSizeDropdownChanged);
                // Fire the handler to sync the grid
                OnGridSizeDropdownChanged(0);
            }

            // Word Count Dropdown
            if (_wordCountDropdown != null)
            {
                _wordCountDropdown.ClearOptions();
                _wordCountDropdown.AddOptions(new System.Collections.Generic.List<string>
                {
                    "3 Words",
                    "4 Words"
                });
                _wordCountDropdown.value = 1; // Default to 4 words
                _wordCountDropdown.onValueChanged.AddListener(OnWordCountDropdownChanged);
            }

            // Difficulty Dropdown
            if (_difficultyDropdown != null)
            {
                _difficultyDropdown.ClearOptions();
                _difficultyDropdown.AddOptions(new System.Collections.Generic.List<string>
                {
                    "Easy",
                    "Normal",
                    "Hard"
                });
                _difficultyDropdown.value = 1; // Default to Normal
                _difficultyDropdown.onValueChanged.AddListener(OnDifficultyDropdownChanged);
            }
        }

        private void SetupButtons()
        {
            // Player Name Input
            if (_playerNameInput != null)
            {
                _playerNameInput.onEndEdit.RemoveAllListeners();
                _playerNameInput.onEndEdit.AddListener(OnPlayerNameChanged);

                // Track focus state for letter routing (no longer disable buttons)
                _playerNameInput.onSelect.AddListener(OnNameInputSelected);
                _playerNameInput.onDeselect.AddListener(OnNameInputDeselected);
            }

            // Setup Color Buttons from container
            SetupColorButtons();

            // Pick Random Words Button
            if (_pickRandomWordsButton != null)
            {
                _pickRandomWordsButton.onClick.RemoveAllListeners();
                _pickRandomWordsButton.onClick.AddListener(OnPickRandomWordsClicked);
            }

            // Place Random Positions Button
            if (_placeRandomPositionsButton != null)
            {
                _placeRandomPositionsButton.onClick.RemoveAllListeners();
                _placeRandomPositionsButton.onClick.AddListener(OnPlaceRandomPositionsClicked);
                _placeRandomPositionsButton.interactable = false; // Start disabled
            }

            // Start Game Button - starts disabled until all words placed
            if (_startGameButton != null)
            {
                _startGameButton.interactable = false;
            }
        }

        /// <summary>
        /// Sets up color button click handlers using PlayerColorController
        /// </summary>
        private void SetupColorButtons()
        {
            // Create and initialize the color controller
            _playerColorController = new PlayerColorController(_colorButtonsContainer);
            _playerColorController.OnColorChanged += OnColorChanged;
            _playerColorController.Initialize();
        }

        /// <summary>
        /// Creates the word validation service instance
        /// </summary>
        private void SetupWordValidationService()
        {
            _wordValidationService = new WordValidationService(
                _threeLetterWords,
                _fourLetterWords,
                _fiveLetterWords,
                _sixLetterWords
            );
        }

        /// <summary>
        /// Sets up word validation by connecting the validator to PlayerGridPanel
        /// </summary>
        private void SetupWordValidation()
        {
            if (_playerGridPanel != null)
            {
                _playerGridPanel.SetWordValidator(ValidateWord);
                Debug.Log("[SetupSettingsPanel] Word validation connected to PlayerGridPanel");
            }
        }

        /// <summary>
        /// Validates a word against the appropriate word list based on length
        /// </summary>
        private bool ValidateWord(string word, int requiredLength)
        {
            return _wordValidationService?.ValidateWord(word, requiredLength) ?? false;
        }

        /// <summary>
        /// Gets a random word of the specified length from the word bank
        /// </summary>
        private string GetRandomWordOfLength(int length)
        {
            return _wordValidationService?.GetRandomWordOfLength(length);
        }

        /// <summary>
        /// Fill empty word rows with random valid words from word bank
        /// Skips rows that already have words entered
        /// </summary>
        [Button("Pick Random Words")]
        public void PickRandomWords()
        {
            if (_playerGridPanel == null)
            {
                Debug.LogWarning("[SetupSettingsPanel] Cannot pick random words - PlayerGridPanel not assigned!");
                return;
            }

            int[] wordLengths = DifficultyCalculator.GetWordLengths(_wordCount);
            int filledCount = 0;
            int emptyCount = 0;

            // First pass: log status of all rows
            Debug.Log("[SetupSettingsPanel] PickRandomWords - Checking row status:");
            for (int i = 0; i < wordLengths.Length; i++)
            {
                var row = _playerGridPanel.GetWordPatternRow(i);
                if (row != null && row.gameObject.activeSelf)
                {
                    bool hasWord = row.HasWord;
                    string currentWord = row.CurrentWord;
                    Debug.Log($"  Row {i + 1}: HasWord={hasWord}, CurrentWord='{currentWord}'");
                }
            }

            // Second pass: fill only empty rows
            for (int i = 0; i < wordLengths.Length; i++)
            {
                var row = _playerGridPanel.GetWordPatternRow(i);
                if (row == null || !row.gameObject.activeSelf)
                    continue;

                // Skip rows that already have a word entered
                if (row.HasWord)
                {
                    Debug.Log($"[SetupSettingsPanel] SKIPPING row {i + 1} - already has word: '{row.CurrentWord}'");
                    filledCount++;
                    continue;
                }

                // This row is empty - fill it with a random word
                emptyCount++;
                string randomWord = GetRandomWordOfLength(wordLengths[i]);
                if (!string.IsNullOrEmpty(randomWord))
                {
                    SetWordForRow(i, randomWord);
                    Debug.Log($"[SetupSettingsPanel] FILLED row {i + 1} with random word: {randomWord}");
                }
            }

            Debug.Log($"[SetupSettingsPanel] PickRandomWords complete - Skipped {filledCount} filled rows, filled {emptyCount} empty rows");
        }

        /// <summary>
        /// Sets a word for a specific row by selecting it and adding letters.
        /// </summary>
        private void SetWordForRow(int rowIndex, string word)
        {
            if (_playerGridPanel == null || string.IsNullOrEmpty(word))
                return;

            // Get the word pattern row
            var row = _playerGridPanel.GetWordPatternRow(rowIndex);
            if (row == null || !row.gameObject.activeSelf)
            {
                Debug.LogWarning($"[SetupSettingsPanel] Row {rowIndex} is null or inactive");
                return;
            }

            // Clear if already placed
            if (row.IsPlaced)
            {
                _playerGridPanel.ClearPlacedWord(rowIndex);
            }

            // Select this row
            _playerGridPanel.SelectWordRow(rowIndex);

            // Reset the row to empty state
            row.ResetToEmpty();

            // Add each letter
            string upperWord = word.ToUpper();
            foreach (char letter in upperWord)
            {
                _playerGridPanel.AddLetterToSelectedRow(letter);
            }

            Debug.Log($"[SetupSettingsPanel] Set word for row {rowIndex + 1}: {upperWord}");
        }

        /// <summary>
        /// Sets up invalid word feedback and button state tracking by subscribing to word row events
        /// </summary>
        private void SetupInvalidWordFeedback()
        {
            if (_playerGridPanel == null) return;

            // Subscribe to word placement event
            _playerGridPanel.OnWordPlaced -= OnWordPlacedHandler;
            _playerGridPanel.OnWordPlaced += OnWordPlacedHandler;

            // Subscribe to events from all word pattern rows
            var wordPatternRows = _playerGridPanel.GetWordPatternRows();
            if (wordPatternRows != null)
            {
                foreach (var row in wordPatternRows)
                {
                    if (row != null)
                    {
                        row.OnInvalidWordRejected -= OnInvalidWordRejected;
                        row.OnInvalidWordRejected += OnInvalidWordRejected;

                        row.OnWordAccepted -= OnWordAcceptedHandler;
                        row.OnWordAccepted += OnWordAcceptedHandler;

                        row.OnDeleteClicked -= OnWordDeletedHandler;
                        row.OnDeleteClicked += OnWordDeletedHandler;
                    }
                }
                Debug.Log($"[SetupSettingsPanel] Subscribed to {wordPatternRows.Length} WordPatternRow events");
            }
        }

        /// <summary>
        /// Unsubscribes from word row and grid events
        /// </summary>
        private void UnsubscribeInvalidWordFeedback()
        {
            if (_playerGridPanel == null) return;

            // Unsubscribe from word placement event
            _playerGridPanel.OnWordPlaced -= OnWordPlacedHandler;

            var wordPatternRows = _playerGridPanel.GetWordPatternRows();
            if (wordPatternRows != null)
            {
                foreach (var row in wordPatternRows)
                {
                    if (row != null)
                    {
                        row.OnInvalidWordRejected -= OnInvalidWordRejected;
                        row.OnWordAccepted -= OnWordAcceptedHandler;
                        row.OnDeleteClicked -= OnWordDeletedHandler;
                    }
                }
            }
        }

        /// <summary>
        /// Called when an invalid word is rejected - shows toast notification
        /// </summary>
        private void OnInvalidWordRejected(int rowNumber, string invalidWord)
        {
            string message = string.Format(_invalidWordMessage, invalidWord);
            ShowInvalidWordToast(message);
            Debug.Log($"[SetupSettingsPanel] Invalid word rejected in row {rowNumber}: '{invalidWord}'");
        }

        /// <summary>
        /// Called when a word is accepted in any row - updates button states
        /// </summary>
        private void OnWordAcceptedHandler(int rowNumber, string word)
        {
            Debug.Log($"[SetupSettingsPanel] Word accepted in row {rowNumber}: {word}");
            UpdatePickRandomWordsButtonState();
            UpdatePlaceRandomPositionsButtonState();
            UpdateStartButtonState();
        }

        /// <summary>
        /// Called when a word is deleted from any row - updates button states
        /// </summary>
        private void OnWordDeletedHandler(int rowNumber, bool wasPlaced)
        {
            Debug.Log($"[SetupSettingsPanel] Word deleted from row {rowNumber} (wasPlaced: {wasPlaced})");
            UpdatePickRandomWordsButtonState();
            UpdatePlaceRandomPositionsButtonState();
            UpdateStartButtonState();
        }

        /// <summary>
        /// Called when a word is placed on the grid - updates button states
        /// </summary>
        private void OnWordPlacedHandler(int rowIndex, string word, System.Collections.Generic.List<UnityEngine.Vector2Int> positions)
        {
            Debug.Log($"[SetupSettingsPanel] Word placed: {word} at {positions.Count} positions");
            UpdatePickRandomWordsButtonState();
            UpdatePlaceRandomPositionsButtonState();
            UpdateStartButtonState();
        }

        private void ShowInvalidWordToast(string message)
        {
            // TODO: Implement visual toast notification
            // Easy Popup System requires ScriptableObject - revisit later
            Debug.LogWarning($"[INVALID WORD] {message}");
        }

        /// <summary>
        /// Initializes word rows based on current word count setting
        /// </summary>
        private void InitializeWordRows()
        {
            if (_playerGridPanel != null)
            {
                int[] wordLengths = DifficultyCalculator.GetWordLengths(_wordCount);
                _playerGridPanel.SetWordLengths(wordLengths);
                Debug.Log($"[SetupSettingsPanel] Initialized word rows: {string.Join(", ", wordLengths)}");
            }
        }

        private void RemoveListeners()
        {
            if (_gridSizeDropdown != null)
                _gridSizeDropdown.onValueChanged.RemoveAllListeners();
            if (_wordCountDropdown != null)
                _wordCountDropdown.onValueChanged.RemoveAllListeners();
            if (_difficultyDropdown != null)
                _difficultyDropdown.onValueChanged.RemoveAllListeners();
            if (_playerNameInput != null)
            {
                _playerNameInput.onEndEdit.RemoveAllListeners();
                _playerNameInput.onSelect.RemoveAllListeners();
                _playerNameInput.onDeselect.RemoveAllListeners();
            }

            // Clean up color controller
            if (_playerColorController != null)
            {
                _playerColorController.OnColorChanged -= OnColorChanged;
                _playerColorController.Cleanup();
            }

            // Unsubscribe from invalid word events
            UnsubscribeInvalidWordFeedback();
        }

        #endregion

        #region Event Handlers

        private void OnGridSizeDropdownChanged(int value)
        {
            // Direct mapping: index 0 = 6, index 1 = 7, ... index 6 = 12
            _gridSize = value + 6;

            UpdateMissLimitDisplay();
            OnGridSizeChanged?.Invoke(_gridSize);

            // Update PlayerGridPanel
            if (_playerGridPanel != null)
            {
                _playerGridPanel.SetGridSize(_gridSize);
            }

            Debug.Log($"[SetupSettingsPanel] Grid size changed to: {_gridSize}x{_gridSize}");

            // Grid size change clears words from grid - update Start button
            UpdateStartButtonState();
        }

        private void OnWordCountDropdownChanged(int value)
        {
            _wordCount = value switch
            {
                0 => WordCountOption.Three,
                1 => WordCountOption.Four,
                _ => WordCountOption.Three
            };

            UpdateMissLimitDisplay();
            OnWordCountChanged?.Invoke(_wordCount);

            // Update PlayerGridPanel word rows
            if (_playerGridPanel != null)
            {
                int[] wordLengths = DifficultyCalculator.GetWordLengths(_wordCount);
                _playerGridPanel.SetWordLengths(wordLengths);

                // Re-subscribe to new word rows for invalid word feedback
                UnsubscribeInvalidWordFeedback();
                SetupInvalidWordFeedback();
            }

            // CRITICAL: Update button states after word count change
            // New rows may be empty and need Pick Random Words enabled
            UpdatePickRandomWordsButtonState();
            UpdatePlaceRandomPositionsButtonState();
            UpdateStartButtonState();

            Debug.Log($"[SetupSettingsPanel] Word count changed to: {_wordCount}");
        }

        private void OnDifficultyDropdownChanged(int value)
        {
            _difficulty = value switch
            {
                0 => DifficultySetting.Easy,
                1 => DifficultySetting.Normal,
                2 => DifficultySetting.Hard,
                _ => DifficultySetting.Normal
            };

            UpdateMissLimitDisplay();
            Debug.Log($"[SetupSettingsPanel] Difficulty changed to: {_difficulty}");
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

            // Update PlayerGridPanel player name label
            if (_playerGridPanel != null)
            {
                _playerGridPanel.SetPlayerName(_playerName);
            }

            OnPlayerSettingsChanged?.Invoke(_playerName, _playerColor);
            Debug.Log($"[SetupSettingsPanel] Player name changed to: {_playerName}");
        }

        /// <summary>
        /// Called when the name input field is selected/focused
        /// </summary>
        private void OnNameInputSelected(string text)
        {
            _isNameInputFocused = true;
            Debug.Log("[SetupSettingsPanel] Name input focused - letter input will route to name field");
        }

        /// <summary>
        /// Called when the name input field loses focus
        /// </summary>
        private void OnNameInputDeselected(string text)
        {
            _isNameInputFocused = false;
            Debug.Log("[SetupSettingsPanel] Name input unfocused - letter input routes to word entry");
        }

        /// <summary>
        /// Called when a letter is input from the letter tracker
        /// Routes to name input field if it's focused, otherwise normal word entry
        /// </summary>
        private void OnLetterInputReceived(char letter)
        {
            if (_isNameInputFocused && _playerNameInput != null)
            {
                // Route letter to name input field
                string currentText = _playerNameInput.text;
                _playerNameInput.text = currentText + letter;

                // Keep the input field focused
                _playerNameInput.ActivateInputField();
                _playerNameInput.MoveTextEnd(false);

                Debug.Log($"[SetupSettingsPanel] Routed letter '{letter}' to name input");
            }
            // If name input is not focused, letter goes to word entry (handled by PlayerGridPanel)
        }

        /// <summary>
        /// Called when color selection changes via PlayerColorController
        /// </summary>
        private void OnColorChanged(Color newColor)
        {
            _playerColor = newColor;
            _currentColorIndex = _playerColorController?.GetCurrentColorIndex() ?? 0;

            // Update PlayerGridPanel player color
            if (_playerGridPanel != null)
            {
                _playerGridPanel.SetPlayerColor(_playerColor);
            }

            OnPlayerSettingsChanged?.Invoke(_playerName, _playerColor);
            Debug.Log($"[SetupSettingsPanel] Player color changed to: {_playerColor}");
        }

        /// <summary>
        /// Called when Pick Random Words button is clicked
        /// </summary>
        private void OnPickRandomWordsClicked()
        {
            PickRandomWords();
            UpdatePickRandomWordsButtonState();
            UpdatePlaceRandomPositionsButtonState();
        }

        /// <summary>
        /// Called when Place Random Positions button is clicked
        /// </summary>
        private void OnPlaceRandomPositionsClicked()
        {
            if (_playerGridPanel != null)
            {
                _playerGridPanel.PlaceAllWordsRandomly();
                UpdatePlaceRandomPositionsButtonState(); // Disable after placing
            }
        }

        /// <summary>
        /// Updates the Place Random Positions button interactability based on word row state
        /// </summary>
        public void UpdatePlaceRandomPositionsButtonState()
        {
            if (_placeRandomPositionsButton == null || _playerGridPanel == null)
                return;

            // Enable if any row has a word entered (but not yet placed)
            bool hasAnyUnplacedWords = false;
            var rows = _playerGridPanel.GetWordPatternRows();
            if (rows != null)
            {
                foreach (var row in rows)
                {
                    if (row != null && row.gameObject.activeSelf && row.HasWord && !row.IsPlaced)
                    {
                        hasAnyUnplacedWords = true;
                        break;
                    }
                }
            }

            _placeRandomPositionsButton.interactable = hasAnyUnplacedWords;
            Debug.Log($"[SetupSettingsPanel] Place Random Positions button: {(hasAnyUnplacedWords ? "ENABLED" : "DISABLED")}");
        }

        /// <summary>
        /// Updates the Pick Random Words button interactability based on word row state
        /// Enabled when any row is empty, disabled when all rows are filled
        /// </summary>
        public void UpdatePickRandomWordsButtonState()
        {
            if (_pickRandomWordsButton == null || _playerGridPanel == null)
                return;

            // Enable if any active row is empty (doesn't have a word)
            bool hasAnyEmptyRows = false;
            int emptyCount = 0;
            int filledCount = 0;
            var rows = _playerGridPanel.GetWordPatternRows();
            if (rows != null)
            {
                foreach (var row in rows)
                {
                    if (row != null && row.gameObject.activeSelf)
                    {
                        if (!row.HasWord)
                        {
                            hasAnyEmptyRows = true;
                            emptyCount++;
                        }
                        else
                        {
                            filledCount++;
                        }
                    }
                }
            }

            _pickRandomWordsButton.interactable = hasAnyEmptyRows;
            Debug.Log($"[SetupSettingsPanel] Pick Random Words button: {(hasAnyEmptyRows ? "ENABLED" : "DISABLED")} (Empty: {emptyCount}, Filled: {filledCount})");
        }
        /// <summary>
        /// Updates the Start Game button interactability.
        /// Enabled only when ALL required word rows have words AND all are placed on the grid.
        /// </summary>
        public void UpdateStartButtonState()
        {
            if (_startGameButton == null || _playerGridPanel == null)
                return;

            bool allWordsPlaced = true;
            int activeRowCount = 0;
            int placedCount = 0;

            var rows = _playerGridPanel.GetWordPatternRows();
            if (rows != null)
            {
                foreach (var row in rows)
                {
                    if (row != null && row.gameObject.activeSelf)
                    {
                        activeRowCount++;

                        // Check if this row has a word AND it's placed on the grid
                        if (!row.HasWord || !row.IsPlaced)
                        {
                            allWordsPlaced = false;
                        }
                        else
                        {
                            placedCount++;
                        }
                    }
                }
            }

            // Must have at least one active row and all must be placed
            bool canStart = activeRowCount > 0 && allWordsPlaced;
            _startGameButton.interactable = canStart;

            Debug.Log($"[SetupSettingsPanel] Start button: {(canStart ? "ENABLED" : "DISABLED")} (Placed: {placedCount}/{activeRowCount})");
        }


        #endregion

        #region UI Updates

        private void UpdateUI()
        {
            // Update player name
            if (_playerNameInput != null)
            {
                _playerNameInput.text = _playerName;
            }

            // Update color button selection via controller
            _playerColorController?.RefreshSelectionVisual();

            // Update PlayerGridPanel
            if (_playerGridPanel != null)
            {
                _playerGridPanel.SetPlayerName(_playerName);
                _playerGridPanel.SetPlayerColor(_playerColor);
            }

            UpdateMissLimitDisplay();
        }

        /// <summary>
        /// Dynamically sizes all UI elements based on available panel height
        /// </summary>
        private void ApplyDynamicSizing()
        {
            if (_contentContainer == null)
            {
                Debug.LogWarning("[SetupSettingsPanel] Content container not assigned - skipping dynamic sizing");
                return;
            }

            // Force layout update to get accurate dimensions
            Canvas.ForceUpdateCanvases();

            float availableHeight = _contentContainer.rect.height;
            if (availableHeight <= 0)
            {
                Debug.LogWarning($"[SetupSettingsPanel] Invalid container height: {availableHeight}");
                return;
            }

            // Count elements that need sizing
            // Header row, Player Name, Color picker, Grid Size, Word Count, Difficulty, Miss Limit, Buttons = 8 rows
            int elementCount = 8;
            float totalPaddingAndSpacing = 40f; // Top/bottom padding estimate

            // Calculate sizing factor (0 = minimum space, 1 = plenty of space)
            float idealHeight = elementCount * _maxElementHeight + totalPaddingAndSpacing + (elementCount - 1) * _maxSpacing;
            float minHeight = elementCount * _minElementHeight + totalPaddingAndSpacing + (elementCount - 1) * _minSpacing;
            float sizeFactor = Mathf.Clamp01((availableHeight - minHeight) / (idealHeight - minHeight));

            // Calculate actual sizes based on factor
            float elementHeight = Mathf.Lerp(_minElementHeight, _maxElementHeight, sizeFactor);
            float fontSize = Mathf.Lerp(_minFontSize, _maxFontSize, sizeFactor);
            float spacing = Mathf.Lerp(_minSpacing, _maxSpacing, sizeFactor);

            Debug.Log($"[SetupSettingsPanel] Dynamic sizing: Available={availableHeight:F0}px, Factor={sizeFactor:F2}, ElementH={elementHeight:F0}, Font={fontSize:F0}, Spacing={spacing:F0}");

            // Apply spacing to VerticalLayoutGroup
            var layoutGroup = _contentContainer.GetComponent<VerticalLayoutGroup>();
            if (layoutGroup != null)
            {
                layoutGroup.spacing = spacing;
                layoutGroup.padding = new RectOffset(
                    Mathf.RoundToInt(spacing),
                    Mathf.RoundToInt(spacing),
                    Mathf.RoundToInt(spacing),
                    Mathf.RoundToInt(spacing)
                );
            }

            // Apply heights to child elements with LayoutElement
            foreach (Transform child in _contentContainer)
            {
                var layoutElement = child.GetComponent<LayoutElement>();
                if (layoutElement != null)
                {
                    layoutElement.preferredHeight = elementHeight;
                    layoutElement.minHeight = elementHeight;
                }

                // Apply font sizes to TextMeshPro elements
                ApplyFontSizeRecursive(child, fontSize);
            }

            // Apply specific sizes to known elements
            ApplyElementSizes(elementHeight, fontSize);

            // Force layout rebuild
            LayoutRebuilder.ForceRebuildLayoutImmediate(_contentContainer);
        }

        /// <summary>
        /// Recursively applies font size to all TMP text elements
        /// Respects Auto Size settings - sets min/max instead of fixed size
        /// </summary>
        private void ApplyFontSizeRecursive(Transform parent, float fontSize)
        {
            var tmpText = parent.GetComponent<TMP_Text>();
            if (tmpText != null)
            {
                ApplyTextSize(tmpText, fontSize);
            }

            foreach (Transform child in parent)
            {
                ApplyFontSizeRecursive(child, fontSize);
            }
        }

        /// <summary>
        /// Applies font size to a TMP text element, respecting Auto Size settings
        /// </summary>
        private void ApplyTextSize(TMP_Text tmpText, float fontSize)
        {
            if (tmpText == null) return;

            if (tmpText.enableAutoSizing)
            {
                // Respect Auto Size - adjust the min/max range
                tmpText.fontSizeMin = _minFontSize;
                tmpText.fontSizeMax = fontSize;
            }
            else
            {
                // Fixed size - set directly
                tmpText.fontSize = fontSize;
            }
        }

        /// <summary>
        /// Applies sizes to specific known UI elements
        /// </summary>
        private void ApplyElementSizes(float elementHeight, float fontSize)
        {
            // Player name input
            if (_playerNameInput != null)
            {
                var inputLayout = _playerNameInput.GetComponent<LayoutElement>();
                if (inputLayout == null)
                    inputLayout = _playerNameInput.gameObject.AddComponent<LayoutElement>();
                inputLayout.preferredHeight = elementHeight;
                inputLayout.minHeight = elementHeight;

                _playerNameInput.pointSize = fontSize;
            }

            // Dropdowns
            ApplyDropdownSizing(_gridSizeDropdown, elementHeight, fontSize);
            ApplyDropdownSizing(_wordCountDropdown, elementHeight, fontSize);
            ApplyDropdownSizing(_difficultyDropdown, elementHeight, fontSize);

            // Buttons
            ApplyButtonSizing(_pickRandomWordsButton, elementHeight, fontSize);
            ApplyButtonSizing(_placeRandomPositionsButton, elementHeight, fontSize);

            // Miss limit display - respect Auto Size
            if (_missLimitDisplay != null)
            {
                ApplyTextSize(_missLimitDisplay, fontSize);
            }

            // Color buttons container - slightly larger for touch targets
            if (_colorButtonsContainer != null)
            {
                var colorLayout = _colorButtonsContainer.GetComponent<LayoutElement>();
                if (colorLayout == null)
                    colorLayout = _colorButtonsContainer.gameObject.AddComponent<LayoutElement>();
                colorLayout.preferredHeight = elementHeight * 1.2f;
                colorLayout.minHeight = elementHeight * 1.2f;
            }
        }

        private void ApplyDropdownSizing(TMP_Dropdown dropdown, float height, float fontSize)
        {
            if (dropdown == null) return;

            var layout = dropdown.GetComponent<LayoutElement>();
            if (layout == null)
                layout = dropdown.gameObject.AddComponent<LayoutElement>();
            layout.preferredHeight = height;
            layout.minHeight = height;

            // Caption text - respect Auto Size
            var captionText = dropdown.captionText;
            if (captionText != null)
                ApplyTextSize(captionText, fontSize);

            // Item text template - respect Auto Size
            var itemText = dropdown.itemText;
            if (itemText != null)
                ApplyTextSize(itemText, fontSize);
        }

        private void ApplyButtonSizing(Button button, float height, float fontSize)
        {
            if (button == null) return;

            var layout = button.GetComponent<LayoutElement>();
            if (layout == null)
                layout = button.gameObject.AddComponent<LayoutElement>();
            layout.preferredHeight = height;
            layout.minHeight = height;

            // Respect Auto Size on button text
            var buttonText = button.GetComponentInChildren<TMP_Text>();
            if (buttonText != null)
                ApplyTextSize(buttonText, fontSize);
        }


private void UpdateMissLimitDisplay()
        {
            // Miss limit is now calculated at gameplay start using opponent's grid settings
            // This preview has been removed from Setup UI as it was displaying incorrect values
            // (was using player's own settings instead of opponent's settings)
            // See DifficultyCalculator.CalculateMissLimitForPlayer() for the correct formula
        }

        #endregion

        #region Debug/Testing

        [Button("Log Current Settings")]
        private void LogCurrentSettings()
        {
            Debug.Log($"[SetupSettingsPanel] Current Settings:");
            Debug.Log($"  Grid Size: {_gridSize}x{_gridSize}");
            Debug.Log($"  Word Count: {_wordCount}");
            Debug.Log($"  Difficulty: {_difficulty}");
            Debug.Log($"  Player Name: {_playerName}");
            Debug.Log($"  Player Color: {_playerColor}");
            Debug.Log($"  Name Input Focused: {_isNameInputFocused}");
        }

        [Button("Test Invalid Word Toast")]
        private void TestInvalidWordToast()
        {
            OnInvalidWordRejected(1, "TEST");
        }

        #endregion
    }
}
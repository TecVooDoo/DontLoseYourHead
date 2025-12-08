using Sirenix.OdinInspector;
using System;
using System.Collections.Generic;
using System.Linq;
using TecVooDoo.DontLoseYourHead.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TecVooDoo.DontLoseYourHead.UI
{
    /// <summary>
    /// Controls the Gameplay phase UI, managing two PlayerGridPanels
    /// and handling the transition from Setup to Gameplay.
    /// </summary>
    public class GameplayUIController : MonoBehaviour
    {
        #region Serialized Fields - Container References
        [TitleGroup("Container References")]
        [SerializeField, Required]
        private GameObject _setupContainer;

        [SerializeField, Required]
        private GameObject _gameplayContainer;

        [SerializeField, Required]
        private Transform _playerPanelParent;

        [SerializeField, Required]
        private Transform _opponentPanelParent;
        #endregion

        #region Serialized Fields - Prefab
        [TitleGroup("Prefab")]
        [SerializeField, Required]
        private PlayerGridPanel _playerGridPanelPrefab;
        #endregion

        #region Serialized Fields - UI References
        [TitleGroup("UI References")]
        [SerializeField, Required]
        private Button _startButton;

        [SerializeField]
        private TextMeshProUGUI _player1MissCounter;

        [SerializeField]
        private TextMeshProUGUI _player2MissCounter;
        #endregion

        #region Serialized Fields - Setup Reference
        [TitleGroup("Setup Reference")]
        [SerializeField, Tooltip("Reference to the setup panel's PlayerGridPanel for data transfer")]
        private PlayerGridPanel _setupPlayerGridPanel;

        [SerializeField]
        private SetupSettingsPanel _setupSettingsPanel;
        #endregion

        #region Serialized Fields - Word Lists (for AI opponent)
        [TitleGroup("Word Lists")]
        [SerializeField]
        private WordListSO _threeLetterWords;

        [SerializeField]
        private WordListSO _fourLetterWords;

        [SerializeField]
        private WordListSO _fiveLetterWords;

        [SerializeField]
        private WordListSO _sixLetterWords;
        #endregion

        #region Serialized Fields - Opponent Settings
        [TitleGroup("Opponent Settings")]
        [SerializeField]
        private string _opponentName = "OPPONENT";

        [SerializeField]
        private Color _opponentColor = new Color(0.9f, 0.4f, 0.4f, 1f);
        #endregion

        #region Private Fields
        private PlayerGridPanel _ownerPanel;
        private PlayerGridPanel _opponentPanel;
        private bool _isGameplayActive;

        // Cached setup data
        private string _playerName;
        private Color _playerColor;
        private int _playerGridSize;
        private int _playerWordCount;
        private int _playerMissLimit;
        private List<PlacedWordData> _playerPlacedWords = new List<PlacedWordData>();
        private DifficultySetting _playerDifficulty = DifficultySetting.Normal;


        // Opponent data
        private int _opponentGridSize;
        private int _opponentWordCount;
        private int _opponentMissLimit;
        private List<PlacedWordData> _opponentPlacedWords = new List<PlacedWordData>();
        #endregion

        #region Data Classes
        /// <summary>
        /// Stores data about a placed word for transfer between panels
        /// </summary>
        [Serializable]
        public class PlacedWordData
        {
            public string word;
            public int startCol;
            public int startRow;
            public int directionCol;
            public int directionRow;

public int rowIndex;

            public PlacedWordData(string word, int col, int row, int dCol, int dRow, int rowIdx = -1)
            {
                this.word = word;
                this.startCol = col;
                this.startRow = row;
                this.directionCol = dCol;
                this.directionRow = dRow;
                this.rowIndex = rowIdx;
            }
        }
        #endregion

        #region Events
        /// <summary>
        /// Fired when gameplay starts
        /// </summary>
        public event Action OnGameplayStarted;

        /// <summary>
        /// Fired when a letter is guessed on opponent's panel
        /// </summary>
        public event Action<char> OnLetterGuessed;

        /// <summary>
        /// Fired when a coordinate is guessed on opponent's panel
        /// </summary>
        public event Action<int, int> OnCoordinateGuessed;
        #endregion

        #region Properties
        public PlayerGridPanel OwnerPanel => _ownerPanel;
        public PlayerGridPanel OpponentPanel => _opponentPanel;
        public bool IsGameplayActive => _isGameplayActive;
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            ValidateReferences();
        }

private void Start()
        {
            // Ensure correct initial state
            if (_setupContainer != null)
                _setupContainer.SetActive(true);

            if (_gameplayContainer != null)
                _gameplayContainer.SetActive(false);

            // Subscribe to Start button
            if (_startButton != null)
            {
                _startButton.onClick.AddListener(OnStartButtonClicked);
                UpdateStartButtonState();
            }

            // Subscribe to word placement and deletion events to auto-update Start button
            if (_setupPlayerGridPanel != null)
            {
                _setupPlayerGridPanel.OnWordPlaced += OnSetupWordPlaced;
                SubscribeToWordRowEvents();
                Debug.Log("[GameplayUIController] Subscribed to setup panel word events");
            }

            // Subscribe to word count changes (need to re-subscribe to new rows)
            if (_setupSettingsPanel != null)
            {
                _setupSettingsPanel.OnWordCountChanged += OnWordCountChanged;
                Debug.Log("[GameplayUIController] Subscribed to word count changes");
            }

            _setupPlayerGridPanel.OnWordLengthsChanged += UpdateStartButtonState;
        }

private void OnDestroy()
        {
            if (_startButton != null)
            {
                _startButton.onClick.RemoveListener(OnStartButtonClicked);
            }

            // Unsubscribe from setup panel events
            if (_setupPlayerGridPanel != null)
            {
                _setupPlayerGridPanel.OnWordPlaced -= OnSetupWordPlaced;
                UnsubscribeFromWordRowEvents();
            }

            // Unsubscribe from settings panel events
            if (_setupSettingsPanel != null)
            {
                _setupSettingsPanel.OnWordCountChanged -= OnWordCountChanged;
            }

            if (_setupPlayerGridPanel != null)
            {
                _setupPlayerGridPanel.OnWordLengthsChanged -= UpdateStartButtonState;
            }

            CleanupPanels();
        }

        /// <summary>
        /// Handler for when a word is placed on the setup grid.
        /// Updates Start button state.
        /// </summary>
        private void OnSetupWordPlaced(int rowIndex, string word, System.Collections.Generic.List<UnityEngine.Vector2Int> positions)
        {
            Debug.Log($"[GameplayUIController] Word placed on setup: row {rowIndex}, word '{word}'");
            UpdateStartButtonState();
        }

/// <summary>
        /// Subscribes to word deletion events from all setup word rows.
        /// </summary>
        private void SubscribeToWordRowEvents()
        {
            if (_setupPlayerGridPanel == null) return;

            var wordRows = _setupPlayerGridPanel.GetWordPatternRows();
            if (wordRows != null)
            {
                foreach (var row in wordRows)
                {
                    if (row != null)
                    {
                        row.OnDeleteClicked -= OnSetupWordDeleted;
                        row.OnDeleteClicked += OnSetupWordDeleted;
                    }
                }
                Debug.Log($"[GameplayUIController] Subscribed to {wordRows.Length} word row deletion events");
            }
        }

        /// <summary>
        /// Unsubscribes from word deletion events from all setup word rows.
        /// </summary>
        private void UnsubscribeFromWordRowEvents()
        {
            if (_setupPlayerGridPanel == null) return;

            var wordRows = _setupPlayerGridPanel.GetWordPatternRows();
            if (wordRows != null)
            {
                foreach (var row in wordRows)
                {
                    if (row != null)
                    {
                        row.OnDeleteClicked -= OnSetupWordDeleted;
                    }
                }
            }
        }

        /// <summary>
        /// Handler for when a word is deleted from the setup grid.
        /// Updates Start button state.
        /// </summary>
        private void OnSetupWordDeleted(int rowNumber, bool wasPlaced)
        {
            Debug.Log($"[GameplayUIController] Word deleted from setup row {rowNumber} (wasPlaced: {wasPlaced})");
            UpdateStartButtonState();
        }

/// <summary>
        /// Handler for when word count changes in setup.
        /// Need to re-subscribe to new word rows.
        /// </summary>
        private void OnWordCountChanged(WordCountOption newWordCount)
        {
            Debug.Log($"[GameplayUIController] Word count changed to: {newWordCount}");
            
            // Re-subscribe to word row events (rows may have changed)
            UnsubscribeFromWordRowEvents();
            SubscribeToWordRowEvents();
            
            // Update Start button state since setup completion may have changed
            UpdateStartButtonState();
        }


        #endregion

        #region Public Methods
        /// <summary>
        /// Updates the Start button interactable state based on setup completion.
        /// Call this when words are placed or removed.
        /// </summary>
        public void UpdateStartButtonState()
        {
            if (_startButton == null) return;

            bool canStart = CanStartGame();
            _startButton.interactable = canStart;

            Debug.Log($"[GameplayUIController] Start button state: {(canStart ? "ENABLED" : "DISABLED")}");
        }

        /// <summary>
        /// Transitions from Setup to Gameplay phase.
        /// </summary>
        [Button("Start Gameplay")]
        public void StartGameplay()
        {
            if (!CanStartGame())
            {
                Debug.LogWarning("[GameplayUIController] Cannot start - setup not complete");
                return;
            }

            // Capture setup data before switching
            CaptureSetupData();

            // Generate opponent data
            GenerateOpponentData();

            // Create gameplay panels
            CreateGameplayPanels();

            // Switch containers
            if (_setupContainer != null)
                _setupContainer.SetActive(false);

            if (_gameplayContainer != null)
                _gameplayContainer.SetActive(true);

            _isGameplayActive = true;

            // Initialize miss counters
            UpdateMissCounter(0, 0, _playerMissLimit);
            UpdateMissCounter(1, 0, _opponentMissLimit);

            OnGameplayStarted?.Invoke();
            Debug.Log("[GameplayUIController] Gameplay started!");
        }

        /// <summary>
        /// Returns to Setup phase (for testing or restart).
        /// </summary>
        [Button("Return to Setup")]
        public void ReturnToSetup()
        {
            CleanupPanels();

            if (_gameplayContainer != null)
                _gameplayContainer.SetActive(false);

            if (_setupContainer != null)
                _setupContainer.SetActive(true);

            _isGameplayActive = false;

            // Clear cached data
            _playerPlacedWords.Clear();
            _opponentPlacedWords.Clear();

            Debug.Log("[GameplayUIController] Returned to setup");
        }

        /// <summary>
        /// Updates the miss counter display for a player.
        /// </summary>
        public void UpdateMissCounter(int playerIndex, int currentMisses, int maxMisses)
        {
            string text = $"{currentMisses} / {maxMisses}";

            if (playerIndex == 0 && _player1MissCounter != null)
            {
                _player1MissCounter.text = text;
            }
            else if (playerIndex == 1 && _player2MissCounter != null)
            {
                _player2MissCounter.text = text;
            }
        }
        #endregion

        #region Private Methods - Setup Data Capture
private void CaptureSetupData()
        {
            _playerPlacedWords.Clear();

            Debug.Log("[GameplayUIController] === CaptureSetupData START ===");
            Debug.Log($"[GameplayUIController] _setupSettingsPanel: {(_setupSettingsPanel != null ? "ASSIGNED" : "NULL")}");
            Debug.Log($"[GameplayUIController] _setupPlayerGridPanel: {(_setupPlayerGridPanel != null ? "ASSIGNED" : "NULL")}");

            // Get player settings from SetupSettingsPanel
            if (_setupSettingsPanel != null)
            {
                var (name, color) = _setupSettingsPanel.GetPlayerSettings();
                _playerName = name;
                _playerColor = color;

                var (gridSize, wordCount, difficulty) = _setupSettingsPanel.GetDifficultySettings();
                _playerGridSize = gridSize;
                _playerWordCount = (int)wordCount;
                
                // Store difficulty for later miss limit calculation
                // Miss limit will be calculated in GenerateOpponentData() once we know opponent settings
                _playerDifficulty = difficulty;

                Debug.Log($"[GameplayUIController] Captured: {_playerName}, Grid={_playerGridSize}, " +
                          $"Words={_playerWordCount}, Difficulty={_playerDifficulty}");
            }
            else
            {
                // Fallback defaults
                _playerName = "PLAYER1";
                _playerColor = Color.cyan;
                _playerGridSize = 6;
                _playerWordCount = 3;
                _playerDifficulty = DifficultySetting.Normal;
            }

            // Get placed words from setup grid
            if (_setupPlayerGridPanel != null)
            {
                CapturePlayerPlacedWords();
            }

            Debug.Log($"[GameplayUIController] Captured {_playerPlacedWords.Count} placed words");
        }

private void CapturePlayerPlacedWords()
        {
            Debug.Log("[GameplayUIController] === CapturePlayerPlacedWords START ===");

            // Get word placements from the setup panel
            var placements = _setupPlayerGridPanel.GetAllWordPlacements();

            Debug.Log($"[GameplayUIController] GetAllWordPlacements returned: {(placements != null ? placements.Count + " items" : "NULL")}");

            if (placements != null)
            {
                foreach (var placement in placements)
                {
                    _playerPlacedWords.Add(new PlacedWordData(
                        placement.word,
                        placement.startCol,
                        placement.startRow,
                        placement.dirCol,
                        placement.dirRow,
                        placement.rowIndex  // Preserve the original row index!
                    ));
                    Debug.Log($"[GameplayUIController] Captured: {placement.word} at " +
                              $"({placement.startCol},{placement.startRow}) rowIndex={placement.rowIndex}");
                }
            }
        }

        private void GenerateOpponentData()
        {
            _opponentPlacedWords.Clear();

            // Use same settings as player for balanced gameplay
            _opponentGridSize = _playerGridSize;
            _opponentWordCount = _playerWordCount;
            _opponentMissLimit = _playerMissLimit;

            // Generate random words for opponent
            int[] wordLengths = GetWordLengthsForCount(_opponentWordCount);

            foreach (int length in wordLengths)
            {
                string word = GetRandomWord(length);
                if (!string.IsNullOrEmpty(word))
                {
                    // Position will be set when placed on grid
                    _opponentPlacedWords.Add(new PlacedWordData(word, -1, -1, 0, 0));
                    Debug.Log($"[GameplayUIController] Generated opponent word: {word}");
                }
            }

            Debug.Log($"[GameplayUIController] Generated {_opponentPlacedWords.Count} opponent words");
        }

        private int[] GetWordLengthsForCount(int wordCount)
        {
            if (wordCount == 3)
            {
                return new int[] { 3, 4, 5 };
            }
            else
            {
                return new int[] { 3, 4, 5, 6 };
            }
        }

        private string GetRandomWord(int length)
        {
            WordListSO wordList = null;

            switch (length)
            {
                case 3: wordList = _threeLetterWords; break;
                case 4: wordList = _fourLetterWords; break;
                case 5: wordList = _fiveLetterWords; break;
                case 6: wordList = _sixLetterWords; break;
            }

            if (wordList != null && wordList.Words != null && wordList.Words.Count > 0)
            {
                int index = UnityEngine.Random.Range(0, wordList.Words.Count);
                return wordList.Words[index].ToUpper();
            }

            // Fallback test words if word lists not assigned
            switch (length)
            {
                case 3: return "CAT";
                case 4: return "WORD";
                case 5: return "HELLO";
                case 6: return "PLAYER";
                default: return "TEST";
            }
        }
        #endregion

        #region Private Methods - Setup
        private void ValidateReferences()
        {
            if (_setupContainer == null)
                Debug.LogError("[GameplayUIController] Setup container not assigned!");

            if (_gameplayContainer == null)
                Debug.LogError("[GameplayUIController] Gameplay container not assigned!");

            if (_playerPanelParent == null)
                Debug.LogError("[GameplayUIController] Player panel parent not assigned!");

            if (_opponentPanelParent == null)
                Debug.LogError("[GameplayUIController] Opponent panel parent not assigned!");

            if (_playerGridPanelPrefab == null)
                Debug.LogError("[GameplayUIController] PlayerGridPanel prefab not assigned!");
        }

        private bool CanStartGame()
        {
            if (_setupPlayerGridPanel != null)
            {
                return _setupPlayerGridPanel.AreAllWordsPlaced();
            }

            Debug.LogWarning("[GameplayUIController] No setup panel reference - allowing start for testing");
            return true;
        }
        #endregion

        #region Private Methods - Panel Creation
        private void CreateGameplayPanels()
        {
            Debug.Log("[GameplayUIController] === CreateGameplayPanels START ===");

            // Create owner panel (shows your words)
            _ownerPanel = CreatePanel(_playerPanelParent, "OwnerGridPanel");
            Debug.Log($"[GameplayUIController] _ownerPanel created: {(_ownerPanel != null ? "SUCCESS" : "FAILED")}");
            ConfigureOwnerPanel(_ownerPanel);

            // Create opponent panel (hidden words to guess)
            _opponentPanel = CreatePanel(_opponentPanelParent, "OpponentGridPanel");
            Debug.Log($"[GameplayUIController] _opponentPanel created: {(_opponentPanel != null ? "SUCCESS" : "FAILED")}");
            ConfigureOpponentPanel(_opponentPanel);

            Debug.Log("[GameplayUIController] === CreateGameplayPanels END ===");
        }

        private PlayerGridPanel CreatePanel(Transform parent, string name)
        {
            Debug.Log($"[GameplayUIController] CreatePanel: {name}, parent={parent?.name ?? "NULL"}, prefab={(_playerGridPanelPrefab != null ? "VALID" : "NULL")}");

            if (_playerGridPanelPrefab == null || parent == null)
            {
                Debug.LogError($"[GameplayUIController] Cannot create panel - missing prefab or parent");
                return null;
            }

            GameObject panelGO = Instantiate(_playerGridPanelPrefab.gameObject, parent);
            panelGO.name = name;

            PlayerGridPanel panel = panelGO.GetComponent<PlayerGridPanel>();
            Debug.Log($"[GameplayUIController] Created panel: {name}, component={panel != null}");
            return panel;
        }

        private void ConfigureOwnerPanel(PlayerGridPanel panel)
        {
            Debug.Log($"[GameplayUIController] === ConfigureOwnerPanel START === panel is {(panel != null ? "VALID" : "NULL")}");
            if (panel == null) return;

            // Set player identity
            panel.SetPlayerName(_playerName);
            panel.SetPlayerColor(_playerColor);

            // IMPORTANT: Force grid initialization BEFORE placing words
            // This must happen before Start() would normally run
            panel.InitializeGrid(_playerGridSize);
            Debug.Log($"[GameplayUIController] Forced grid initialization: {_playerGridSize}x{_playerGridSize}");

            // Set word lengths (creates/configures word pattern rows)
            int[] lengths = GetWordLengthsForCount(_playerWordCount);
            panel.SetWordLengths(lengths);
            Debug.Log($"[GameplayUIController] Set word lengths: {string.Join(", ", lengths)}");

            // Set to Gameplay mode
            panel.SetMode(PlayerGridPanel.PanelMode.Gameplay);

            // NOW place the captured words on the grid (cells should exist)
            Debug.Log($"[GameplayUIController] Placing {_playerPlacedWords.Count} words on owner panel...");
            foreach (var wordData in _playerPlacedWords)
            {
                PlaceWordOnPanel(panel, wordData, true);
            }

            // Set word rows to Gameplay state (hides setup buttons, shows words)
            ConfigurePlayerWordRows(panel);

            Debug.Log($"[GameplayUIController] Owner panel configured: {_playerName}, " +
                      $"{_playerGridSize}x{_playerGridSize}, {_playerPlacedWords.Count} words");
        }

        private void ConfigureOpponentPanel(PlayerGridPanel panel)
        {
            if (panel == null) return;

            Debug.Log($"[GameplayUIController] === ConfigureOpponentPanel START ===");

            // Set opponent identity
            panel.SetPlayerName(_opponentName);
            panel.SetPlayerColor(_opponentColor);

            // IMPORTANT: Force grid initialization BEFORE placing words
            panel.InitializeGrid(_opponentGridSize);
            Debug.Log($"[GameplayUIController] Forced opponent grid initialization: {_opponentGridSize}x{_opponentGridSize}");

            // Set word lengths
            int[] lengths = GetWordLengthsForCount(_opponentWordCount);
            panel.SetWordLengths(lengths);

            // Set to Gameplay mode
            panel.SetMode(PlayerGridPanel.PanelMode.Gameplay);

            // Place opponent words randomly
            PlaceOpponentWordsRandomly(panel);

            // Subscribe to click events for guessing
            panel.OnLetterClicked += HandleOpponentLetterClicked;
            panel.OnCellClicked += HandleOpponentCellClicked;

            // Hide opponent letters (words are secret)
            HideOpponentDisplay(panel);

            Debug.Log($"[GameplayUIController] Opponent panel configured: {_opponentName}, " +
                      $"{_opponentGridSize}x{_opponentGridSize}, {_opponentPlacedWords.Count} words");
        }

        private void PlaceWordOnPanel(PlayerGridPanel panel, PlacedWordData wordData, bool showLetters)
        {
            if (panel == null || wordData == null) return;

            int col = wordData.startCol;
            int row = wordData.startRow;

            Debug.Log($"[GameplayUIController] PlaceWordOnPanel: '{wordData.word}' at ({col},{row}) " +
                      $"dir({wordData.directionCol},{wordData.directionRow}) showLetters={showLetters}");

            for (int i = 0; i < wordData.word.Length; i++)
            {
                var cell = panel.GetCell(col, row);
                if (cell != null)
                {
                    if (showLetters)
                    {
                        cell.SetLetter(wordData.word[i]);
                        cell.SetState(CellState.Filled);  // ADD THIS LINE
                        Debug.Log($"[GameplayUIController] Set letter '{wordData.word[i]}' at ({col},{row}) - State set to Filled");
                    }
                    else
                    {
                        // Mark as having a letter but dont show it
                        cell.SetHiddenLetter(wordData.word[i]);
                    }
                }
                else
                {
                    Debug.LogWarning($"[GameplayUIController] Cell at ({col},{row}) is null!");
                }

                col += wordData.directionCol;
                row += wordData.directionRow;
            }
        }

        private void PlaceOpponentWordsRandomly(PlayerGridPanel panel)
        {
            if (panel == null) return;

            for (int i = 0; i < _opponentPlacedWords.Count; i++)
            {
                var wordData = _opponentPlacedWords[i];
                var placement = FindRandomPlacement(panel, wordData.word);

                if (placement.HasValue)
                {
                    wordData.startCol = placement.Value.col;
                    wordData.startRow = placement.Value.row;
                    wordData.directionCol = placement.Value.dCol;
                    wordData.directionRow = placement.Value.dRow;

                    PlaceWordOnPanel(panel, wordData, false);
                    Debug.Log($"[GameplayUIController] Placed opponent word '{wordData.word}' " +
                              $"at ({wordData.startCol},{wordData.startRow})");
                }
                else
                {
                    Debug.LogWarning($"[GameplayUIController] Could not place: {wordData.word}");
                }
            }
        }

        private (int col, int row, int dCol, int dRow)? FindRandomPlacement(
            PlayerGridPanel panel, string word)
        {
            if (panel == null || string.IsNullOrEmpty(word)) return null;

            int gridSize = panel.CurrentGridSize;
            int wordLength = word.Length;

            // 8 direction vectors
            int[,] directions = new int[,]
            {
                { 1, 0 },   // Right
                { -1, 0 },  // Left
                { 0, 1 },   // Down
                { 0, -1 },  // Up
                { 1, 1 },   // Diagonal down-right
                { -1, -1 }, // Diagonal up-left
                { 1, -1 },  // Diagonal up-right
                { -1, 1 }   // Diagonal down-left
            };

            var validPlacements = new List<(int col, int row, int dCol, int dRow)>();

            for (int col = 0; col < gridSize; col++)
            {
                for (int row = 0; row < gridSize; row++)
                {
                    for (int d = 0; d < 8; d++)
                    {
                        int dCol = directions[d, 0];
                        int dRow = directions[d, 1];

                        if (CanPlaceWord(panel, word, col, row, dCol, dRow))
                        {
                            validPlacements.Add((col, row, dCol, dRow));
                        }
                    }
                }
            }

            if (validPlacements.Count > 0)
            {
                int index = UnityEngine.Random.Range(0, validPlacements.Count);
                return validPlacements[index];
            }

            return null;
        }

        private bool CanPlaceWord(PlayerGridPanel panel, string word,
            int startCol, int startRow, int dCol, int dRow)
        {
            int gridSize = panel.CurrentGridSize;

            for (int i = 0; i < word.Length; i++)
            {
                int col = startCol + i * dCol;
                int row = startRow + i * dRow;

                // Check bounds
                if (col < 0 || col >= gridSize || row < 0 || row >= gridSize)
                    return false;

                // Check if cell is empty or has same letter (for crosswords)
                var cell = panel.GetCell(col, row);
                if (cell != null && cell.GetLetter() != '\0')
                {
                    if (cell.GetLetter() != word[i])
                        return false;
                }
            }

            return true;
        }

        private void ConfigurePlayerWordRows(PlayerGridPanel panel)
        {
            if (panel == null) return;

            Debug.Log($"[GameplayUIController] === ConfigurePlayerWordRows START ===");
            Debug.Log($"[GameplayUIController] _playerPlacedWords.Count = {_playerPlacedWords.Count}");

            var wordRows = panel.GetWordPatternRows();
            Debug.Log($"[GameplayUIController] panel.GetWordPatternRows() returned {(wordRows != null ? wordRows.Length + " rows" : "NULL")}");

            if (wordRows == null || wordRows.Length == 0)
            {
                Debug.LogError("[GameplayUIController] No word rows found on panel!");
                return;
            }

            // Sort placed words by word length to match row order (3, 4, 5, 6 letters)
            var sortedWords = _playerPlacedWords.OrderBy(w => w.word.Length).ToList();

            for (int i = 0; i < wordRows.Length && i < sortedWords.Count; i++)
            {
                var row = wordRows[i];
                var wordData = sortedWords[i];

                if (row != null && row.gameObject.activeSelf)
                {
                    Debug.Log($"[GameplayUIController] Setting row {i} with word: '{wordData.word}' (length {wordData.word.Length})");

                    // Set gameplay word (switches to Gameplay state, hides buttons)
                    row.SetGameplayWord(wordData.word);

                    // Reveal ALL letters since this is the player's own words
                    row.RevealAllLetters();

                    Debug.Log($"[GameplayUIController] Row {i} configured - calling RevealAllLetters");
                }
            }

            Debug.Log($"[GameplayUIController] Player word rows configured for gameplay");
        }

        private void HideOpponentDisplay(PlayerGridPanel panel)
        {
            if (panel == null) return;

            // Hide grid letters (already handled by SetHiddenLetter)
            // Reset letter tracker
            panel.ResetAllLetterButtons();

            // Show word patterns as underscores only
            var wordRows = panel.GetWordPatternRows();
            if (wordRows != null)
            {
                for (int i = 0; i < wordRows.Length && i < _opponentPlacedWords.Count; i++)
                {
                    var row = wordRows[i];
                    var wordData = _opponentPlacedWords[i];
                    if (row != null && row.gameObject.activeSelf)
                    {
                        row.SetGameplayWord(wordData.word);
                    }
                }
            }
        }

        private void CleanupPanels()
        {
            if (_ownerPanel != null)
            {
                Destroy(_ownerPanel.gameObject);
                _ownerPanel = null;
            }

            if (_opponentPanel != null)
            {
                _opponentPanel.OnLetterClicked -= HandleOpponentLetterClicked;
                _opponentPanel.OnCellClicked -= HandleOpponentCellClicked;
                Destroy(_opponentPanel.gameObject);
                _opponentPanel = null;
            }
        }
        #endregion

        #region Private Methods - Event Handlers
        private void OnStartButtonClicked()
        {
            StartGameplay();
        }

        private void HandleOpponentLetterClicked(char letter)
        {
            if (!_isGameplayActive) return;

            Debug.Log($"[GameplayUIController] Letter guessed: {letter}");
            OnLetterGuessed?.Invoke(letter);

            // TODO: Process guess through GameManager
        }

        private void HandleOpponentCellClicked(int column, int row)
        {
            if (!_isGameplayActive) return;

            Debug.Log($"[GameplayUIController] Coordinate guessed: {(char)('A' + column)}{row + 1}");
            OnCoordinateGuessed?.Invoke(column, row);

            // TODO: Process guess through GameManager
        }
        #endregion

        #region Editor Helpers
#if UNITY_EDITOR
        [TitleGroup("Debug")]
        [Button("Test Start Button State")]
        private void TestStartButtonState()
        {
            UpdateStartButtonState();
        }

        [Button("Force Start Gameplay")]
        private void ForceStartGameplay()
        {
            // Use test data
            _playerName = "TEST_PLAYER";
            _playerColor = Color.cyan;
            _playerGridSize = 6;
            _playerWordCount = 3;
            _playerMissLimit = 21;
            _playerPlacedWords.Clear();

            // Add test words
            _playerPlacedWords.Add(new PlacedWordData("CAT", 0, 0, 1, 0));
            _playerPlacedWords.Add(new PlacedWordData("WORD", 0, 1, 1, 0));
            _playerPlacedWords.Add(new PlacedWordData("HELLO", 0, 2, 1, 0));

            GenerateOpponentData();
            CreateGameplayPanels();

            if (_setupContainer != null)
                _setupContainer.SetActive(false);

            if (_gameplayContainer != null)
                _gameplayContainer.SetActive(true);

            _isGameplayActive = true;

            UpdateMissCounter(0, 0, _playerMissLimit);
            UpdateMissCounter(1, 0, _opponentMissLimit);

            Debug.Log("[GameplayUIController] Force started gameplay (debug)");
        }
#endif
        #endregion
    }
}
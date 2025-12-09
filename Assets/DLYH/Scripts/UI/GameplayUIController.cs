using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using TecVooDoo.DontLoseYourHead.Core;

namespace TecVooDoo.DontLoseYourHead.UI
{
    /// <summary>
    /// Controls the Gameplay UI phase, managing two PlayerGridPanel instances
    /// (owner and opponent) and handling the transition from Setup to Gameplay.
    /// </summary>
    public class GameplayUIController : MonoBehaviour
    {
        #region Serialized Fields

        [Header("Container References")]
        [SerializeField] private GameObject _setupContainer;
        [SerializeField] private GameObject _gameplayContainer;

        [Header("Pre-Placed Panel References")]
        [SerializeField] private PlayerGridPanel _ownerPanel;
        [SerializeField] private PlayerGridPanel _opponentPanel;

        [Header("Miss Counter References")]
        [SerializeField] private TextMeshProUGUI _player1MissCounter;
        [SerializeField] private TextMeshProUGUI _player2MissCounter;

        [Header("Start Button (for event subscription)")]
        [SerializeField] private Button _startGameButton;

        #endregion

        #region Private Fields

        // Captured setup data
        private SetupData _playerSetupData;
        private SetupData _opponentSetupData;

        // Reference to setup panel for data capture
        private SetupSettingsPanel _setupSettingsPanel;
        private PlayerGridPanel _setupGridPanel;

        #endregion

        #region Data Structures

        /// <summary>
        /// Data structure to hold captured setup information
        /// </summary>
        private class SetupData
        {
            public string PlayerName;
            public Color PlayerColor;
            public int GridSize;
            public int WordCount;
            public DifficultySetting DifficultyLevel;
            public int[] WordLengths;
            public List<WordPlacementData> PlacedWords = new List<WordPlacementData>();
        }

        /// <summary>
        /// Data structure for a placed word with position and direction
        /// </summary>
        private class WordPlacementData
        {
            public string Word;
            public int StartCol;
            public int StartRow;
            public int DirCol;
            public int DirRow;
            public int RowIndex;
        }

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            // Find setup references
            if (_setupContainer != null)
            {
                _setupSettingsPanel = _setupContainer.GetComponentInChildren<SetupSettingsPanel>(true);
                _setupGridPanel = _setupContainer.GetComponentInChildren<PlayerGridPanel>(true);
            }
        }

        private void Start()
        {
            // Subscribe to Start button if assigned
            if (_startGameButton != null)
            {
                _startGameButton.onClick.AddListener(StartGameplay);
            }

            // Ensure gameplay panels start inactive
            if (_ownerPanel != null)
                _ownerPanel.gameObject.SetActive(false);
            if (_opponentPanel != null)
                _opponentPanel.gameObject.SetActive(false);
        }

        private void OnDestroy()
        {
            // Unsubscribe from Start button
            if (_startGameButton != null)
            {
                _startGameButton.onClick.RemoveListener(StartGameplay);
            }

            // Clean up panel event subscriptions
            UnsubscribeFromPanelEvents();
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Called when the Start Game button is clicked in setup.
        /// Transitions from Setup to Gameplay phase.
        /// </summary>
        public void StartGameplay()
        {
            Debug.Log("[GameplayUI] Starting gameplay transition...");

            // Safety validation - ensure all words are placed
            if (_setupGridPanel != null)
            {
                var rows = _setupGridPanel.GetWordPatternRows();
                if (rows != null)
                {
                    foreach (var row in rows)
                    {
                        if (row != null && row.gameObject.activeSelf)
                        {
                            if (!row.HasWord || !row.IsPlaced)
                            {
                                Debug.LogWarning("[GameplayUI] Cannot start - not all words are placed!");
                                return;
                            }
                        }
                    }
                }
            }

            // Capture data from setup
            CaptureSetupData();

            // Generate opponent data (AI or second player)
            GenerateOpponentData();

            // Switch containers
            if (_setupContainer != null)
                _setupContainer.SetActive(false);

            if (_gameplayContainer != null)
                _gameplayContainer.SetActive(true);

            // Activate and configure the gameplay panels
            if (_ownerPanel != null)
            {
                _ownerPanel.gameObject.SetActive(true);
                ConfigureOwnerPanel();
            }

            if (_opponentPanel != null)
            {
                _opponentPanel.gameObject.SetActive(true);
                ConfigureOpponentPanel();
            }

            // Update miss counters
            UpdateMissCounters();

            Debug.Log("[GameplayUI] Gameplay transition complete");
        }

        /// <summary>
        /// Returns to setup mode (for future use)
        /// </summary>
        public void ReturnToSetup()
        {
            Debug.Log("[GameplayUI] Returning to setup...");

            UnsubscribeFromPanelEvents();

            // Hide gameplay panels
            if (_ownerPanel != null)
                _ownerPanel.gameObject.SetActive(false);
            if (_opponentPanel != null)
                _opponentPanel.gameObject.SetActive(false);

            // Switch containers
            if (_gameplayContainer != null)
                _gameplayContainer.SetActive(false);

            if (_setupContainer != null)
                _setupContainer.SetActive(true);
        }

        #endregion

        #region Data Capture

        /// <summary>
        /// Captures player setup data from the setup panel
        /// </summary>
        private void CaptureSetupData()
        {
            _playerSetupData = new SetupData();

            if (_setupSettingsPanel != null)
            {
                // Use the actual API methods from SetupSettingsPanel
                var playerSettings = _setupSettingsPanel.GetPlayerSettings();
                var difficultySettings = _setupSettingsPanel.GetDifficultySettings();

                _playerSetupData.PlayerName = playerSettings.name;
                _playerSetupData.PlayerColor = playerSettings.color;
                _playerSetupData.GridSize = difficultySettings.gridSize;
                _playerSetupData.WordCount = (int)difficultySettings.wordCount;
                _playerSetupData.DifficultyLevel = difficultySettings.difficulty;
                _playerSetupData.WordLengths = DifficultyCalculator.GetWordLengths(difficultySettings.wordCount);

                Debug.Log(string.Format("[GameplayUI] Captured setup: {0}, Grid: {1}x{1}, Words: {2}, Difficulty: {3}",
                    _playerSetupData.PlayerName,
                    _playerSetupData.GridSize,
                    _playerSetupData.WordCount,
                    _playerSetupData.DifficultyLevel));
            }

            // Capture placed words from setup grid using the actual API
            if (_setupGridPanel != null)
            {
                CaptureWordPlacements(_setupGridPanel, _playerSetupData);
            }
        }

        /// <summary>
        /// Captures the placed words using PlayerGridPanel.GetAllWordPlacements()
        /// </summary>
        private void CaptureWordPlacements(PlayerGridPanel panel, SetupData data)
        {
            // Use the actual API from PlayerGridPanel
            var placements = panel.GetAllWordPlacements();

            foreach (var placement in placements)
            {
                var wordData = new WordPlacementData
                {
                    Word = placement.word,
                    StartCol = placement.startCol,
                    StartRow = placement.startRow,
                    DirCol = placement.dirCol,
                    DirRow = placement.dirRow,
                    RowIndex = placement.rowIndex
                };

                data.PlacedWords.Add(wordData);

                Debug.Log(string.Format("[GameplayUI] Captured word: {0} at ({1},{2}) dir({3},{4})",
                    wordData.Word, wordData.StartCol, wordData.StartRow, wordData.DirCol, wordData.DirRow));
            }
        }

        #endregion

        #region Opponent Generation

        /// <summary>
        /// Generates opponent data (for now, creates AI opponent with random words)
        /// </summary>
        private void GenerateOpponentData()
        {
            _opponentSetupData = new SetupData
            {
                PlayerName = "OPPONENT",
                PlayerColor = new Color(0.8f, 0.2f, 0.2f, 1f),
                GridSize = _playerSetupData != null ? _playerSetupData.GridSize : 8,
                WordCount = _playerSetupData != null ? _playerSetupData.WordCount : 3,
                DifficultyLevel = _playerSetupData != null ? _playerSetupData.DifficultyLevel : DifficultySetting.Normal
            };

            // Get word lengths for opponent
            WordCountOption wordCountOption = _opponentSetupData.WordCount == 4 ? WordCountOption.Four : WordCountOption.Three;
            _opponentSetupData.WordLengths = DifficultyCalculator.GetWordLengths(wordCountOption);

            // Generate placeholder words for AI opponent
            GenerateRandomWordsForOpponent();

            Debug.Log(string.Format("[GameplayUI] Generated opponent: {0}, Grid: {1}x{1}, Words: {2}",
                _opponentSetupData.PlayerName, _opponentSetupData.GridSize, _opponentSetupData.WordCount));
        }

        /// <summary>
        /// Generates random words and placements for AI opponent
        /// </summary>
        private void GenerateRandomWordsForOpponent()
        {
            // Placeholder implementation - uses fixed words
            // TODO: Use WordListSO for random word selection

            string[] placeholderWords = _opponentSetupData.WordCount == 4
                ? new string[] { "CAT", "WORD", "GAMES", "PUZZLE" }
                : new string[] { "DOG", "PLAY", "BOARD" };

            int gridSize = _opponentSetupData.GridSize;

            for (int i = 0; i < placeholderWords.Length; i++)
            {
                string word = placeholderWords[i];
                int startCol = (i * 2) % Mathf.Max(1, gridSize - word.Length);
                int startRow = i;

                var wordData = new WordPlacementData
                {
                    Word = word,
                    StartCol = startCol,
                    StartRow = startRow,
                    DirCol = 1,
                    DirRow = 0,
                    RowIndex = i
                };

                _opponentSetupData.PlacedWords.Add(wordData);
            }
        }

        #endregion

        #region Panel Configuration

        /// <summary>
        /// Configures the owner panel with player's data (fully revealed)
        /// </summary>
        /// <summary>
        /// Configures the owner panel with player's data (fully revealed)
        /// </summary>
private void ConfigureOwnerPanel()
        {
            if (_ownerPanel == null || _playerSetupData == null) return;

            _ownerPanel.SetMode(PlayerGridPanel.PanelMode.Gameplay);
            _ownerPanel.InitializeGrid(_playerSetupData.GridSize);
            _ownerPanel.SetPlayerName(_playerSetupData.PlayerName);
            _ownerPanel.SetPlayerColor(_playerSetupData.PlayerColor);
            _ownerPanel.SetWordLengths(_playerSetupData.WordLengths);

            // CRITICAL: Ensure word pattern rows are cached before we try to use them
            // This is needed because Start() hasn't run yet on newly activated panels
            _ownerPanel.CacheWordPatternRows();

            // Place words on the grid AND set up word pattern rows
            foreach (var wordData in _playerSetupData.PlacedWords)
            {
                // Place letters on grid
                PlaceWordOnPanelRevealed(_ownerPanel, wordData);

                // Set up the word pattern row to show the full word
                var row = _ownerPanel.GetWordPatternRow(wordData.RowIndex);
                if (row != null)
                {
                    row.SetGameplayWord(wordData.Word);
                    row.RevealAllLetters();
                    Debug.Log($"[GameplayUI] Owner row {wordData.RowIndex + 1}: Set word '{wordData.Word}' (revealed)");
                }
                else
                {
                    Debug.LogError($"[GameplayUI] Owner row {wordData.RowIndex} is NULL! Cannot set word '{wordData.Word}'");
                }
            }

            // Hide unused word rows
            int wordCount = _playerSetupData.PlacedWords.Count;
            var allRows = _ownerPanel.GetWordPatternRows();
            if (allRows != null)
            {
                for (int i = 0; i < allRows.Length; i++)
                {
                    if (allRows[i] != null)
                    {
                        bool shouldBeActive = i < wordCount;
                        allRows[i].gameObject.SetActive(shouldBeActive);
                    }
                }
                Debug.Log($"[GameplayUI] Owner panel: Showing {wordCount} rows, hiding {allRows.Length - wordCount} unused rows");
            }

            Debug.Log(string.Format("[GameplayUI] Configured owner panel: {0} words placed",
                _playerSetupData.PlacedWords.Count));
        }

        /// <summary>
        /// Configures the opponent panel with opponent's data (hidden)
        /// </summary>
private void ConfigureOpponentPanel()
        {
            if (_opponentPanel == null || _opponentSetupData == null) return;

            _opponentPanel.SetMode(PlayerGridPanel.PanelMode.Gameplay);
            _opponentPanel.InitializeGrid(_opponentSetupData.GridSize);
            _opponentPanel.SetPlayerName(_opponentSetupData.PlayerName);
            _opponentPanel.SetPlayerColor(_opponentSetupData.PlayerColor);
            _opponentPanel.SetWordLengths(_opponentSetupData.WordLengths);

            // CRITICAL: Ensure word pattern rows are cached before we try to use them
            // This is needed because Start() hasn't run yet on newly activated panels
            _opponentPanel.CacheWordPatternRows();

            // Place words on the grid AND set up word pattern rows
            foreach (var wordData in _opponentSetupData.PlacedWords)
            {
                // Place letters on grid (hidden)
                PlaceWordOnPanelHidden(_opponentPanel, wordData);

                // Set up the word pattern row to show underscores (hidden)
                var row = _opponentPanel.GetWordPatternRow(wordData.RowIndex);
                if (row != null)
                {
                    row.SetGameplayWord(wordData.Word);
                    Debug.Log($"[GameplayUI] Opponent row {wordData.RowIndex + 1}: Set word '{wordData.Word}' (hidden)");
                }
                else
                {
                    Debug.LogError($"[GameplayUI] Opponent row {wordData.RowIndex} is NULL! Cannot set word '{wordData.Word}'");
                }
            }

            // Hide unused word rows
            int wordCount = _opponentSetupData.PlacedWords.Count;
            var allRows = _opponentPanel.GetWordPatternRows();
            if (allRows != null)
            {
                for (int i = 0; i < allRows.Length; i++)
                {
                    if (allRows[i] != null)
                    {
                        bool shouldBeActive = i < wordCount;
                        allRows[i].gameObject.SetActive(shouldBeActive);
                    }
                }
                Debug.Log($"[GameplayUI] Opponent panel: Showing {wordCount} rows, hiding {allRows.Length - wordCount} unused rows");
            }

            SubscribeToPanelEvents();

            Debug.Log(string.Format("[GameplayUI] Configured opponent panel: {0} words placed (hidden)",
                _opponentSetupData.PlacedWords.Count));
        }

        /// <summary>
        /// Places a word on a panel's grid with letters fully revealed
        /// </summary>
        private void PlaceWordOnPanelRevealed(PlayerGridPanel panel, WordPlacementData wordData)
        {
            int col = wordData.StartCol;
            int row = wordData.StartRow;

            for (int i = 0; i < wordData.Word.Length; i++)
            {
                char letter = wordData.Word[i];
                var cellUI = panel.GetCell(col, row);

                if (cellUI != null)
                {
                    cellUI.SetLetter(letter);
                    cellUI.SetState(CellState.Filled);
                }

                col += wordData.DirCol;
                row += wordData.DirRow;
            }
        }

        /// <summary>
        /// Places a word on a panel's grid with letters hidden
        /// </summary>
        private void PlaceWordOnPanelHidden(PlayerGridPanel panel, WordPlacementData wordData)
        {
            int col = wordData.StartCol;
            int row = wordData.StartRow;

            Debug.Log(string.Format("[GameplayUI] PlaceWordOnPanelHidden: '{0}' at ({1},{2}) dir({3},{4})",
                wordData.Word, col, row, wordData.DirCol, wordData.DirRow));

            for (int i = 0; i < wordData.Word.Length; i++)
            {
                char letter = wordData.Word[i];
                var cellUI = panel.GetCell(col, row);

                if (cellUI != null)
                {
                    cellUI.SetHiddenLetter(letter);
                }

                col += wordData.DirCol;
                row += wordData.DirRow;
            }
        }

        #endregion

        #region Event Handling

        private void SubscribeToPanelEvents()
        {
            if (_opponentPanel != null)
            {
                _opponentPanel.OnLetterClicked += HandleLetterGuess;
                _opponentPanel.OnCellClicked += HandleCellGuess;
            }
        }

        private void UnsubscribeFromPanelEvents()
        {
            if (_opponentPanel != null)
            {
                _opponentPanel.OnLetterClicked -= HandleLetterGuess;
                _opponentPanel.OnCellClicked -= HandleCellGuess;
            }
        }

        private void HandleLetterGuess(char letter)
        {
            Debug.Log(string.Format("[GameplayUI] Letter guessed: {0}", letter));
            // TODO: Implement letter guessing logic
        }

        private void HandleCellGuess(int column, int row)
        {
            Debug.Log(string.Format("[GameplayUI] Cell guessed: ({0}, {1})", column, row));
            // TODO: Implement coordinate guessing logic
        }

        #endregion

        #region Miss Counter

        private void UpdateMissCounters()
        {
            if (_player1MissCounter != null && _opponentSetupData != null)
            {
                int missLimit = CalculateMissLimit(_playerSetupData.DifficultyLevel, _opponentSetupData);
                _player1MissCounter.text = string.Format("0 / {0}", missLimit);
            }

            if (_player2MissCounter != null && _playerSetupData != null)
            {
                int missLimit = CalculateMissLimit(_opponentSetupData.DifficultyLevel, _playerSetupData);
                _player2MissCounter.text = string.Format("0 / {0}", missLimit);
            }
        }

        private int CalculateMissLimit(DifficultySetting playerDifficulty, SetupData opponentData)
        {
            if (opponentData == null)
                return 21;

            int baseMisses = 15;
            int gridBonus = GetGridBonus(opponentData.GridSize);
            int wordModifier = opponentData.WordCount == 4 ? -2 : 0;
            int difficultyModifier = GetDifficultyModifier(playerDifficulty);

            return baseMisses + gridBonus + wordModifier + difficultyModifier;
        }

        private int GetGridBonus(int gridSize)
        {
            switch (gridSize)
            {
                case 6: return 3;
                case 7: return 4;
                case 8: return 6;
                case 9: return 8;
                case 10: return 10;
                case 11: return 12;
                case 12: return 13;
                default: return 6;
            }
        }

        private int GetDifficultyModifier(DifficultySetting difficulty)
        {
            switch (difficulty)
            {
                case DifficultySetting.Easy: return 4;
                case DifficultySetting.Normal: return 0;
                case DifficultySetting.Hard: return -4;
                default: return 0;
            }
        }

        #endregion
    }
}
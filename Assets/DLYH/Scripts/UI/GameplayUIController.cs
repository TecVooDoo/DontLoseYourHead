using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using TecVooDoo.DontLoseYourHead.Core;
using System.Linq;
using UnityEngine.InputSystem;

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

        [Header("Word Bank References (for word guess validation)")]
        [SerializeField] private WordListSO _threeLetterWords;
        [SerializeField] private WordListSO _fourLetterWords;
        [SerializeField] private WordListSO _fiveLetterWords;
        [SerializeField] private WordListSO _sixLetterWords;

        [Header("Word Guess Feedback")]
        [SerializeField] private TextMeshProUGUI _wordGuessFeedbackText;
        [SerializeField] private float _feedbackDisplayDuration = 2f;

        [Header("Guessed Word List References")]
        [SerializeField] private GuessedWordListController _player1GuessedWordList;
        [SerializeField] private GuessedWordListController _player2GuessedWordList;


        #endregion

        #region Private Fields

        // Captured setup data
        private SetupData _playerSetupData;
        private SetupData _opponentSetupData;

        // Reference to setup panel for data capture
        private SetupSettingsPanel _setupSettingsPanel;
        private PlayerGridPanel _setupGridPanel;

        // Guess processors for player and opponent
        private GuessProcessor _playerGuessProcessor;
        private GuessProcessor _opponentGuessProcessor;

        // Word guess mode controller
        private WordGuessModeController _wordGuessModeController;

        #endregion

        #region Player State Tracking

        // Turn management
        private bool _isPlayerTurn = true;
        private bool _gameOver = false;

        // Player gameplay state (guessing against opponent)
        private int _playerMisses = 0;
        private int _playerMissLimit = 0;
        private HashSet<char> _playerKnownLetters = new HashSet<char>();
        private HashSet<char> _playerGuessedLetters = new HashSet<char>();
        private HashSet<Vector2Int> _playerGuessedCoordinates = new HashSet<Vector2Int>();
        private HashSet<string> _playerGuessedWords = new HashSet<string>();
        private HashSet<int> _playerSolvedWordRows = new HashSet<int>();


        #endregion

        #region Opponent State Tracking

        // Opponent (Executioner) gameplay state (guessing against player)
        private int _opponentMisses = 0;
        private int _opponentMissLimit = 0;
        private HashSet<char> _opponentKnownLetters = new HashSet<char>();
        private HashSet<char> _opponentGuessedLetters = new HashSet<char>();
        private HashSet<Vector2Int> _opponentGuessedCoordinates = new HashSet<Vector2Int>();
        private HashSet<string> _opponentGuessedWords = new HashSet<string>();

        #endregion

        #region Guess Result Enum

        /// <summary>
        /// Result of a guess attempt - used to determine if turn should end
        /// </summary>
        private enum GuessResult
        {
            Hit,
            Miss,
            AlreadyGuessed,
            InvalidWord
        }

        #endregion

#if UNITY_EDITOR

        #region Testing - Simulate Opponent Turn

        [Title("TESTING: Turn Control")]
        [InfoBox("Use these buttons to manually control turns for testing")]

        [Button("Switch to Player Turn", ButtonSizes.Large)]
        [GUIColor(0.4f, 0.8f, 0.4f)]
        private void TestSwitchToPlayerTurn()
        {
            _isPlayerTurn = true;
            Debug.Log("[GameplayUI] === Manually switched to PLAYER'S turn ===");
        }

        [Button("Switch to Opponent Turn", ButtonSizes.Large)]
        [GUIColor(0.8f, 0.6f, 0.2f)]
        private void TestSwitchToOpponentTurn()
        {
            _isPlayerTurn = false;
            Debug.Log("[GameplayUI] === Manually switched to OPPONENT'S turn ===");
        }

        [Title("TESTING: Simulate Opponent (Executioner) Turn")]
        [InfoBox("Use these buttons to simulate the opponent guessing against YOUR words. Only works during opponent's turn.")]

        [SerializeField]
        private char _testOpponentLetter = 'E';

        [SerializeField]
        private Vector2Int _testOpponentCoordinate = new Vector2Int(0, 0);

        [SerializeField]
        private string _testOpponentWord = "NOV";

        [Button("Simulate Letter Guess", ButtonSizes.Large)]
        [GUIColor(0.8f, 0.4f, 0.2f)]
        private void TestSimulateLetterGuess()
        {
            if (_playerSetupData == null || _playerSetupData.PlacedWords.Count == 0)
            {
                Debug.LogWarning("[GameplayUI] No player data - start gameplay first!");
                return;
            }

            if (_isPlayerTurn)
            {
                Debug.LogWarning("[GameplayUI] It's player's turn! Use 'Switch to Opponent Turn' first.");
                return;
            }

            char letter = char.ToUpper(_testOpponentLetter);
            GuessResult result = ProcessOpponentLetterGuess(letter);

            if (result == GuessResult.AlreadyGuessed)
            {
                Debug.Log(string.Format("[GameplayUI] Opponent already guessed '{0}' - try a different letter!", letter));
                return;
            }

            Debug.Log(string.Format("[GameplayUI] Opponent guessed '{0}': {1}", letter, result == GuessResult.Hit ? "HIT" : "MISS"));
            EndOpponentTurn();
        }

        [Button("Simulate Coordinate Guess", ButtonSizes.Large)]
        [GUIColor(0.8f, 0.4f, 0.2f)]
        private void TestSimulateCoordinateGuess()
        {
            if (_playerSetupData == null || _playerSetupData.PlacedWords.Count == 0)
            {
                Debug.LogWarning("[GameplayUI] No player data - start gameplay first!");
                return;
            }

            if (_isPlayerTurn)
            {
                Debug.LogWarning("[GameplayUI] It's player's turn! Use 'Switch to Opponent Turn' first.");
                return;
            }

            GuessResult result = ProcessOpponentCoordinateGuess(_testOpponentCoordinate.x, _testOpponentCoordinate.y);

            string colLabel = ((char)('A' + _testOpponentCoordinate.x)).ToString();
            string coordLabel = colLabel + (_testOpponentCoordinate.y + 1);

            if (result == GuessResult.AlreadyGuessed)
            {
                Debug.Log(string.Format("[GameplayUI] Opponent already guessed {0} - try different coordinates!", coordLabel));
                return;
            }

            Debug.Log(string.Format("[GameplayUI] Opponent guessed {0}: {1}", coordLabel, result == GuessResult.Hit ? "HIT" : "MISS"));
            EndOpponentTurn();
        }

        [Button("Simulate Word Guess", ButtonSizes.Large)]
        [GUIColor(0.8f, 0.4f, 0.2f)]
        private void TestSimulateWordGuess()
        {
            if (_playerSetupData == null || _playerSetupData.PlacedWords.Count == 0)
            {
                Debug.LogWarning("[GameplayUI] No player data - start gameplay first!");
                return;
            }

            if (_isPlayerTurn)
            {
                Debug.LogWarning("[GameplayUI] It's player's turn! Use 'Switch to Opponent Turn' first.");
                return;
            }

            GuessResult result = ProcessOpponentWordGuess(_testOpponentWord, 0);

            if (result == GuessResult.InvalidWord)
            {
                Debug.Log(string.Format("[GameplayUI] Opponent word '{0}' is not a valid English word - rejected!", _testOpponentWord));
                return;
            }

            if (result == GuessResult.AlreadyGuessed)
            {
                Debug.Log(string.Format("[GameplayUI] Opponent already guessed word '{0}' - try a different word!", _testOpponentWord));
                return;
            }

            Debug.Log(string.Format("[GameplayUI] Opponent guessed word '{0}': {1}",
                _testOpponentWord, result == GuessResult.Hit ? "CORRECT" : "WRONG (+2 misses)"));
            EndOpponentTurn();
        }

        [Button("Show Player's Words (Targets)")]
        [GUIColor(0.5f, 0.7f, 1f)]
        private void TestShowPlayerWords()
        {
            if (_playerSetupData == null || _playerSetupData.PlacedWords.Count == 0)
            {
                Debug.LogWarning("[GameplayUI] No player data!");
                return;
            }

            Debug.Log("[GameplayUI] === Player's Words (Opponent's Targets) ===");
            foreach (WordPlacementData word in _playerSetupData.PlacedWords)
            {
                string colLabel = ((char)('A' + word.StartCol)).ToString();
                string coordLabel = colLabel + (word.StartRow + 1);
                string direction = word.DirCol == 1 ? "Horizontal" : (word.DirRow == 1 ? "Vertical" : "Diagonal");

                Debug.Log(string.Format("  {0}. {1} at {2} ({3})",
                    word.RowIndex + 1, word.Word, coordLabel, direction));
            }
        }

        [Button("Show Opponent's Words (Your Targets)")]
        [GUIColor(0.5f, 0.7f, 1f)]
        private void TestShowOpponentWords()
        {
            if (_opponentSetupData == null || _opponentSetupData.PlacedWords.Count == 0)
            {
                Debug.LogWarning("[GameplayUI] No opponent data!");
                return;
            }

            Debug.Log("[GameplayUI] === Opponent's Words (Your Targets) ===");
            foreach (WordPlacementData word in _opponentSetupData.PlacedWords)
            {
                string colLabel = ((char)('A' + word.StartCol)).ToString();
                string coordLabel = colLabel + (word.StartRow + 1);
                string direction = word.DirCol == 1 ? "Horizontal" : (word.DirRow == 1 ? "Vertical" : "Diagonal");

                Debug.Log(string.Format("  {0}. {1} at {2} ({3})",
                    word.RowIndex + 1, word.Word, coordLabel, direction));
            }
        }

        [Button("Show Known Letters")]
        [GUIColor(0.5f, 0.7f, 1f)]
        private void TestShowKnownLetters()
        {
            List<char> playerSorted = new List<char>(_playerKnownLetters);
            playerSorted.Sort();
            List<char> opponentSorted = new List<char>(_opponentKnownLetters);
            opponentSorted.Sort();

            Debug.Log(string.Format("[GameplayUI] Your Known Letters: {0}",
                playerSorted.Count > 0 ? string.Join(", ", playerSorted) : "(none)"));
            Debug.Log(string.Format("[GameplayUI] Opponent's Known Letters: {0}",
                opponentSorted.Count > 0 ? string.Join(", ", opponentSorted) : "(none)"));
        }

        #endregion

#endif

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

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            if (_setupContainer != null)
            {
                _setupSettingsPanel = _setupContainer.GetComponentInChildren<SetupSettingsPanel>(true);
                _setupGridPanel = _setupContainer.GetComponentInChildren<PlayerGridPanel>(true);
            }
        }

        private void Start()
        {
            if (_startGameButton != null)
            {
                _startGameButton.onClick.AddListener(StartGameplay);
            }

            if (_ownerPanel != null)
                _ownerPanel.gameObject.SetActive(false);
            if (_opponentPanel != null)
                _opponentPanel.gameObject.SetActive(false);

            if (_wordGuessFeedbackText != null)
                _wordGuessFeedbackText.gameObject.SetActive(false);
        }

        private void Update()
        {
            // Delegate keyboard input to word guess mode controller
            _wordGuessModeController?.ProcessKeyboardInput();
        }

        private void OnDestroy()
        {
            if (_startGameButton != null)
            {
                _startGameButton.onClick.RemoveListener(StartGameplay);
            }
            UnsubscribeFromPanelEvents();
            UnsubscribeFromWordGuessModeController();
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Called when the Start Game button is clicked in setup.
        /// </summary>
        public void StartGameplay()
        {

            if (_setupGridPanel != null)
            {
                WordPatternRow[] rows = _setupGridPanel.GetWordPatternRows();
                if (rows != null)
                {
                    foreach (WordPatternRow row in rows)
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

            CaptureSetupData();
            GenerateOpponentData();
            InitializePlayerState();
            InitializeOpponentState();
            InitializeGuessProcessors();

            if (_setupContainer != null)
                _setupContainer.SetActive(false);

            if (_gameplayContainer != null)
                _gameplayContainer.SetActive(true);

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

            InitializeWordGuessModeController();
            UpdateMissCounters();

        }

        /// <summary>
        /// Returns to setup mode
        /// </summary>
        public void ReturnToSetup()
        {

            _wordGuessModeController?.ExitWordGuessMode();
            UnsubscribeFromPanelEvents();
            UnsubscribeFromWordGuessModeController();

            if (_ownerPanel != null)
                _ownerPanel.gameObject.SetActive(false);
            if (_opponentPanel != null)
                _opponentPanel.gameObject.SetActive(false);

            if (_gameplayContainer != null)
                _gameplayContainer.SetActive(false);

            if (_setupContainer != null)
                _setupContainer.SetActive(true);
        }

        #endregion

        #region Data Capture

        private void CaptureSetupData()
        {
            _playerSetupData = new SetupData();

            if (_setupSettingsPanel != null)
            {
                (string name, Color color) = _setupSettingsPanel.GetPlayerSettings();
                (int gridSize, WordCountOption wordCount, DifficultySetting difficulty) = _setupSettingsPanel.GetDifficultySettings();

                _playerSetupData.PlayerName = name;
                _playerSetupData.PlayerColor = color;
                _playerSetupData.GridSize = gridSize;
                _playerSetupData.WordCount = (int)wordCount + 3;
                _playerSetupData.DifficultyLevel = difficulty;

            }

            if (_setupGridPanel != null)
            {
                CaptureWordsFromGrid(_setupGridPanel, _playerSetupData);

                _playerSetupData.WordLengths = _playerSetupData.PlacedWords
                    .Select(w => w.Word.Length)
                    .ToArray();
            }
        }

        private void CaptureWordsFromGrid(PlayerGridPanel panel, SetupData setupData)
        {
            WordPatternRow[] rows = panel.GetWordPatternRows();
            if (rows == null)
            {
                Debug.LogError("[GameplayUI] No word pattern rows found!");
                return;
            }

            for (int i = 0; i < rows.Length; i++)
            {
                WordPatternRow row = rows[i];
                if (row != null && row.gameObject.activeSelf && row.HasWord && row.IsPlaced)
                {
                    WordPlacementData wordData = new WordPlacementData
                    {
                        Word = row.CurrentWord,
                        StartCol = row.PlacedStartCol,
                        StartRow = row.PlacedStartRow,
                        DirCol = row.PlacedDirCol,
                        DirRow = row.PlacedDirRow,
                        RowIndex = i
                    };

                    setupData.PlacedWords.Add(wordData);

                }
            }
        }

        private void GenerateOpponentData()
        {
            _opponentSetupData = new SetupData
            {
                PlayerName = "Executioner",
                PlayerColor = Color.brown,
                GridSize = 10,
                WordCount = 4,
                DifficultyLevel = _playerSetupData.DifficultyLevel,
                WordLengths = _playerSetupData.WordLengths
            };

            string[] testWords = { "HOG", "ROAD", "SNORE", "BRIDGE" };
            int[] testStartCols = { 2, 5, 3, 7 };
            int[] testStartRows = { 1, 3, 5, 7 };

            for (int i = 0; i < 4; i++)
            {
                WordPlacementData opponentWord = new WordPlacementData
                {
                    Word = testWords[i],
                    StartCol = testStartCols[i],
                    StartRow = testStartRows[i],
                    DirCol = 1,
                    DirRow = 0,
                    RowIndex = i
                };

                _opponentSetupData.PlacedWords.Add(opponentWord);
            }
        }

        #endregion

        #region Panel Configuration

        private void ConfigureOwnerPanel()
        {
            if (_ownerPanel == null || _playerSetupData == null)
            {
                Debug.LogError("[GameplayUI] Cannot configure owner panel - missing references!");
                return;
            }

            _ownerPanel.InitializeGrid(_playerSetupData.GridSize);
            _ownerPanel.SetPlayerName(_playerSetupData.PlayerName);
            _ownerPanel.SetPlayerColor(_playerSetupData.PlayerColor);
            _ownerPanel.SetMode(PlayerGridPanel.PanelMode.Gameplay);
            _ownerPanel.CacheWordPatternRows();

            foreach (WordPlacementData wordData in _playerSetupData.PlacedWords)
            {
                PlaceWordOnPanelRevealed(_ownerPanel, wordData);

                WordPatternRow row = _ownerPanel.GetWordPatternRow(wordData.RowIndex);
                if (row != null)
                {
                    row.SetGameplayWord(wordData.Word);
                    row.RevealAllLetters();
                    row.SetAsOwnerPanel();
                }
                else
                {
                    Debug.LogError(string.Format("[GameplayUI] Owner row {0} is NULL! Cannot set word '{1}'", wordData.RowIndex, wordData.Word));
                }
            }

            int wordCount = _playerSetupData.PlacedWords.Count;
            WordPatternRow[] allRows = _ownerPanel.GetWordPatternRows();
            if (allRows != null)
            {
                for (int i = 0; i < allRows.Length; i++)
                {
                    if (allRows[i] != null)
                    {
                        bool shouldBeActive = i < wordCount;
                        allRows[i].gameObject.SetActive(shouldBeActive);
                        allRows[i].SetAsOwnerPanel();
                    }
                }
            }
        }

        private void ConfigureOpponentPanel()
        {
            if (_opponentPanel == null || _opponentSetupData == null)
            {
                Debug.LogError("[GameplayUI] Cannot configure opponent panel - missing references!");
                return;
            }

            _opponentPanel.InitializeGrid(_opponentSetupData.GridSize);
            _opponentPanel.SetMode(PlayerGridPanel.PanelMode.Gameplay);
            _opponentPanel.SetPlayerName(_opponentSetupData.PlayerName);
            _opponentPanel.SetPlayerColor(_opponentSetupData.PlayerColor);
            _opponentPanel.CacheWordPatternRows();

            foreach (WordPlacementData wordData in _opponentSetupData.PlacedWords)
            {
                PlaceWordOnPanelHidden(_opponentPanel, wordData);

                WordPatternRow row = _opponentPanel.GetWordPatternRow(wordData.RowIndex);
                if (row != null)
                {
                    row.SetGameplayWord(wordData.Word);
                }
                else
                {
                    Debug.LogError(string.Format("[GameplayUI] Opponent row {0} is NULL! Cannot set word '{1}'", wordData.RowIndex, wordData.Word));
                }
            }

            int wordCount = _opponentSetupData.PlacedWords.Count;
            WordPatternRow[] allRows = _opponentPanel.GetWordPatternRows();
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
            }

            SubscribeToPanelEvents();
        }

        private void PlaceWordOnPanelRevealed(PlayerGridPanel panel, WordPlacementData wordData)
        {
            int col = wordData.StartCol;
            int row = wordData.StartRow;

            for (int i = 0; i < wordData.Word.Length; i++)
            {
                char letter = wordData.Word[i];
                GridCellUI cellUI = panel.GetCell(col, row);

                if (cellUI != null)
                {
                    cellUI.SetLetter(letter);
                    cellUI.SetState(CellState.Filled);
                }
                else
                {
                    Debug.LogError(string.Format("[GameplayUI]   FAILED: GetCell({0},{1}) returned NULL!", col, row));
                }

                col += wordData.DirCol;
                row += wordData.DirRow;
            }
        }

        private void PlaceWordOnPanelHidden(PlayerGridPanel panel, WordPlacementData wordData)
        {
            int col = wordData.StartCol;
            int row = wordData.StartRow;

            for (int i = 0; i < wordData.Word.Length; i++)
            {
                char letter = wordData.Word[i];
                GridCellUI cellUI = panel.GetCell(col, row);

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
                _opponentPanel.OnLetterClicked += HandleLetterClicked;
                _opponentPanel.OnCellClicked += HandleCellGuess;
            }
        }

        private void UnsubscribeFromPanelEvents()
        {
            if (_opponentPanel != null)
            {
                _opponentPanel.OnLetterClicked -= HandleLetterClicked;
                _opponentPanel.OnCellClicked -= HandleCellGuess;
            }
        }

        private void HandleLetterClicked(char letter)
        {
            // Check if we're in word guess keyboard mode
            if (_wordGuessModeController != null && _wordGuessModeController.IsInKeyboardMode)
            {
                _wordGuessModeController.HandleKeyboardLetterInput(letter);
                return;
            }

            HandleLetterGuess(letter);
        }

        private void HandleLetterGuess(char letter)
        {
            if (!_isPlayerTurn || _gameOver)
            {
                Debug.LogWarning("[GameplayUI] Not player's turn or game is over!");
                return;
            }

            letter = char.ToUpper(letter);
            GuessResult result = ProcessPlayerLetterGuess(letter);

            if (result == GuessResult.AlreadyGuessed)
            {
                return;
            }

            EndPlayerTurn();
        }

        private void HandleCellGuess(int column, int row)
        {
            // Block coordinate guesses during word guess mode
            if (_wordGuessModeController != null && _wordGuessModeController.IsInKeyboardMode)
            {
                return;
            }

            if (!_isPlayerTurn || _gameOver)
            {
                Debug.LogWarning("[GameplayUI] Not player's turn or game is over!");
                return;
            }

            string colLabel = ((char)('A' + column)).ToString();
            string coordLabel = colLabel + (row + 1);

            GuessResult result = ProcessPlayerCoordinateGuess(column, row);

            if (result == GuessResult.AlreadyGuessed)
            {
                return;
            }

            EndPlayerTurn();
        }

        #endregion

        #region Word Guess Mode Controller

        private void InitializeWordGuessModeController()
        {
            _wordGuessModeController = new WordGuessModeController(
                _opponentPanel,
                ProcessWordGuessForController,
                () => _isPlayerTurn && !_gameOver,
                rowIndex => _playerSolvedWordRows.Contains(rowIndex),
                rowIndex => _playerSolvedWordRows.Add(rowIndex)
            );

            // Subscribe to controller events
            _wordGuessModeController.OnFeedbackRequested += ShowFeedback;
            _wordGuessModeController.OnTurnEnded += EndPlayerTurn;

            // Subscribe word pattern rows to controller
            WordPatternRow[] rows = _opponentPanel.GetWordPatternRows();
            if (rows != null)
            {
                foreach (WordPatternRow row in rows)
                {
                    if (row != null)
                    {
                        row.OnWordGuessStarted += _wordGuessModeController.HandleWordGuessStarted;
                        row.OnWordGuessSubmitted += _wordGuessModeController.HandleWordGuessSubmitted;
                        row.OnWordGuessCancelled += _wordGuessModeController.HandleWordGuessCancelled;
                    }
                }
            }
        }

        private void UnsubscribeFromWordGuessModeController()
        {
            if (_wordGuessModeController == null) return;

            _wordGuessModeController.OnFeedbackRequested -= ShowFeedback;
            _wordGuessModeController.OnTurnEnded -= EndPlayerTurn;

            if (_opponentPanel != null)
            {
                WordPatternRow[] rows = _opponentPanel.GetWordPatternRows();
                if (rows != null)
                {
                    foreach (WordPatternRow row in rows)
                    {
                        if (row != null)
                        {
                            row.OnWordGuessStarted -= _wordGuessModeController.HandleWordGuessStarted;
                            row.OnWordGuessSubmitted -= _wordGuessModeController.HandleWordGuessSubmitted;
                            row.OnWordGuessCancelled -= _wordGuessModeController.HandleWordGuessCancelled;
                        }
                    }
                }
            }

            _wordGuessModeController = null;
        }

        /// <summary>
        /// Bridge method to convert between controller's WordGuessResult and internal processing
        /// </summary>
        private WordGuessResult ProcessWordGuessForController(string word, int rowIndex)
        {
            GuessResult result = ProcessPlayerWordGuess(word, rowIndex);

            switch (result)
            {
                case GuessResult.Hit:
                    return WordGuessResult.Hit;
                case GuessResult.Miss:
                    return WordGuessResult.Miss;
                case GuessResult.AlreadyGuessed:
                    return WordGuessResult.AlreadyGuessed;
                case GuessResult.InvalidWord:
                    return WordGuessResult.InvalidWord;
                default:
                    return WordGuessResult.Miss;
            }
        }

        #endregion

        #region Feedback Display

        private void ShowFeedback(string message)
        {

            if (_wordGuessFeedbackText != null)
            {
                _wordGuessFeedbackText.text = message;
                _wordGuessFeedbackText.gameObject.SetActive(true);

                CancelInvoke("HideFeedback");
                Invoke("HideFeedback", _feedbackDisplayDuration);
            }
        }

        private void HideFeedback()
        {
            if (_wordGuessFeedbackText != null)
            {
                _wordGuessFeedbackText.gameObject.SetActive(false);
            }
        }

        #endregion

        #region Turn Management

        private void EndPlayerTurn()
        {
            if (_gameOver) return;

            _wordGuessModeController?.ExitWordGuessMode();

            _isPlayerTurn = false;
        }

        private void EndOpponentTurn()
        {
            if (_gameOver) return;

            _isPlayerTurn = true;
        }

        public bool IsPlayerTurn => _isPlayerTurn;
        public bool IsGameOver => _gameOver;

        #endregion

        #region Miss Counter

        private void UpdateMissCounters()
        {
            if (_player1MissCounter != null && _opponentSetupData != null)
            {
                _player1MissCounter.text = string.Format("{0} / {1}", _playerMisses, _playerMissLimit);
            }

            if (_player2MissCounter != null && _playerSetupData != null)
            {
                _player2MissCounter.text = string.Format("{0} / {1}", _opponentMisses, _opponentMissLimit);
            }
        }

        private void UpdatePlayerMissCounter()
        {
            if (_player1MissCounter != null)
            {
                _player1MissCounter.text = string.Format("{0} / {1}", _playerMisses, _playerMissLimit);
            }


            if (_playerMisses >= _playerMissLimit)
            {
                Debug.Log("[GameplayUI] === PLAYER LOSES! Opponent wins! ===");
                _gameOver = true;
            }
        }

        private void UpdateOpponentMissCounter()
        {
            if (_player2MissCounter != null)
            {
                _player2MissCounter.text = string.Format("{0} / {1}", _opponentMisses, _opponentMissLimit);
            }


            if (_opponentMisses >= _opponentMissLimit)
            {
                Debug.Log("[GameplayUI] === OPPONENT LOSES! Player wins! ===");
                _gameOver = true;
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

        #region Player Guess Processing

        private void InitializePlayerState()
        {
            _playerMisses = 0;
            _playerKnownLetters.Clear();
            _playerGuessedLetters.Clear();
            _playerGuessedCoordinates.Clear();
            _playerGuessedWords.Clear();
            _playerSolvedWordRows.Clear();

            _playerMissLimit = CalculateMissLimit(_playerSetupData.DifficultyLevel, _opponentSetupData);

        }

        /// <summary>
        /// Initialize guess processors for both players
        /// </summary>
        private void InitializeGuessProcessors()
        {
            // Create player's processor (guesses against opponent's data)
            _playerGuessProcessor = new GuessProcessor(
                _opponentSetupData.PlacedWords.ConvertAll(w => new WordPlacementData
                {
                    Word = w.Word,
                    StartCol = w.StartCol,
                    StartRow = w.StartRow,
                    DirCol = w.DirCol,
                    DirRow = w.DirRow,
                    RowIndex = w.RowIndex
                }),
                _opponentPanel,
                "Player",
                () => { _playerMisses++; UpdatePlayerMissCounter(); },
                (letter, state) => _opponentPanel.SetLetterState(letter, state),
                word => IsValidWord(word),
                (word, correct) => _player1GuessedWordList.AddGuessedWord(word, correct)
            );
            _playerGuessProcessor.Initialize(_playerMissLimit);

            // Create opponent's processor (guesses against player's data)
            _opponentGuessProcessor = new GuessProcessor(
                _playerSetupData.PlacedWords.ConvertAll(w => new WordPlacementData
                {
                    Word = w.Word,
                    StartCol = w.StartCol,
                    StartRow = w.StartRow,
                    DirCol = w.DirCol,
                    DirRow = w.DirRow,
                    RowIndex = w.RowIndex
                }),
                _ownerPanel,
                "Opponent",
                () => { _opponentMisses++; UpdateOpponentMissCounter(); },
                (letter, state) => _ownerPanel.SetLetterState(letter, state),
                word => IsValidWord(word),
                (word, correct) => _player2GuessedWordList.AddGuessedWord(word, correct)
            );
            _opponentGuessProcessor.Initialize(_opponentMissLimit);
        }

        /// <summary>
        /// Convert GuessProcessor.GuessResult to local GuessResult enum
        /// </summary>
        private GuessResult ConvertGuessResult(GuessProcessor.GuessResult result)
        {
            switch (result)
            {
                case GuessProcessor.GuessResult.Hit:
                    return GuessResult.Hit;
                case GuessProcessor.GuessResult.Miss:
                    return GuessResult.Miss;
                case GuessProcessor.GuessResult.AlreadyGuessed:
                    return GuessResult.AlreadyGuessed;
                case GuessProcessor.GuessResult.InvalidWord:
                    return GuessResult.InvalidWord;
                default:
                    return GuessResult.Miss;
            }
        }

        /// <summary>
        /// Process player guessing a letter against opponent's words
        /// </summary>
        private GuessResult ProcessPlayerLetterGuess(char letter)
        {
            GuessProcessor.GuessResult result = _playerGuessProcessor.ProcessLetterGuess(letter);
            return ConvertGuessResult(result);
        }

        /// <summary>
        /// Process player guessing a coordinate on opponent's grid
        /// </summary>
        private GuessResult ProcessPlayerCoordinateGuess(int col, int row)
        {
            GuessProcessor.GuessResult result = _playerGuessProcessor.ProcessCoordinateGuess(col, row);
            return ConvertGuessResult(result);
        }

        /// <summary>
        /// Process player guessing a complete word
        /// </summary>
        private GuessResult ProcessPlayerWordGuess(string word, int rowIndex)
        {
            GuessProcessor.GuessResult result = _playerGuessProcessor.ProcessWordGuess(word, rowIndex);

            // Track solved rows locally for UI button management
            if (result == GuessProcessor.GuessResult.Hit)
            {
                _playerSolvedWordRows.Add(rowIndex);
            }

            return ConvertGuessResult(result);
        }

        /// <summary>
        /// Validate a word against the word bank
        /// </summary>
        private bool IsValidWord(string word)
        {
            if (string.IsNullOrEmpty(word)) return false;

            string normalized = word.Trim().ToUpper();
            WordListSO wordList = GetWordListForLength(normalized.Length);

            if (wordList == null)
            {
                Debug.LogWarning(string.Format("[GameplayUI] No word list for {0}-letter words!", normalized.Length));
                return false;
            }

            return wordList.Contains(normalized);
        }

        private WordListSO GetWordListForLength(int length)
        {
            switch (length)
            {
                case 3: return _threeLetterWords;
                case 4: return _fourLetterWords;
                case 5: return _fiveLetterWords;
                case 6: return _sixLetterWords;
                default: return null;
            }
        }

        #endregion

        #region Opponent Guess Processing

        private void InitializeOpponentState()
        {
            _opponentMisses = 0;
            _opponentKnownLetters.Clear();
            _opponentGuessedLetters.Clear();
            _opponentGuessedCoordinates.Clear();
            _opponentGuessedWords.Clear();
            _opponentMissLimit = CalculateMissLimit(_opponentSetupData.DifficultyLevel, _playerSetupData);

        }

        /// <summary>
        /// Process opponent guessing a letter against player's words
        /// </summary>
        private GuessResult ProcessOpponentLetterGuess(char letter)
        {
            GuessProcessor.GuessResult result = _opponentGuessProcessor.ProcessLetterGuess(letter);
            return ConvertGuessResult(result);
        }

        /// <summary>
        /// Process opponent guessing a coordinate on player's grid
        /// </summary>
        private GuessResult ProcessOpponentCoordinateGuess(int col, int row)
        {
            GuessProcessor.GuessResult result = _opponentGuessProcessor.ProcessCoordinateGuess(col, row);
            return ConvertGuessResult(result);
        }

        /// <summary>
        /// Process opponent guessing a complete word
        /// </summary>
        private GuessResult ProcessOpponentWordGuess(string word, int rowIndex)
        {
            GuessProcessor.GuessResult result = _opponentGuessProcessor.ProcessWordGuess(word, rowIndex);
            return ConvertGuessResult(result);
        }

        #endregion
    }
}
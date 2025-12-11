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
        private HashSet<int> _playerSolvedWordRows = new HashSet<int>();  // Track which opponent word rows player has solved


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

        #region Word Guess Mode State

        // Track which row (if any) is currently in word guess mode
        private WordPatternRow _activeWordGuessRow = null;

        // Save letter tracker states when switching to keyboard mode
        private Dictionary<char, LetterButton.LetterState> _savedLetterStates = new Dictionary<char, LetterButton.LetterState>();

        // Flag to indicate letter tracker is in keyboard mode
        private bool _letterTrackerInKeyboardMode = false;

        #endregion

        #region Guess Result Enum

        /// <summary>
        /// Result of a guess attempt - used to determine if turn should end
        /// </summary>
        private enum GuessResult
        {
            Hit,            // Valid guess that hit
            Miss,           // Valid guess that missed
            AlreadyGuessed, // Duplicate guess - don't end turn
            InvalidWord     // Word not in dictionary - don't end turn
        }

        #endregion

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
                return; // Don't end turn
            }

            Debug.Log(string.Format("[GameplayUI] Opponent guessed '{0}': {1}", letter, result == GuessResult.Hit ? "HIT" : "MISS"));

            // End opponent's turn after valid guess
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
                return; // Don't end turn
            }

            Debug.Log(string.Format("[GameplayUI] Opponent guessed {0}: {1}", coordLabel, result == GuessResult.Hit ? "HIT" : "MISS"));

            // End opponent's turn after valid guess
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

            GuessResult result = ProcessOpponentWordGuess(_testOpponentWord);

            // ADD THIS CHECK
            if (result == GuessResult.InvalidWord)
            {
                Debug.Log(string.Format("[GameplayUI] Opponent word '{0}' is not a valid English word - rejected!", _testOpponentWord));
                return; // Don't end turn, don't count as guess
            }

            if (result == GuessResult.AlreadyGuessed)
            {
                Debug.Log(string.Format("[GameplayUI] Opponent already guessed word '{0}' - try a different word!", _testOpponentWord));
                return; // Don't end turn
            }

            Debug.Log(string.Format("[GameplayUI] Opponent guessed word '{0}': {1}",
                _testOpponentWord, result == GuessResult.Hit ? "CORRECT" : "WRONG (+2 misses)"));

            // End opponent's turn after valid guess
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
            foreach (var word in _playerSetupData.PlacedWords)
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
            foreach (var word in _opponentSetupData.PlacedWords)
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
            var playerSorted = new List<char>(_playerKnownLetters);
            playerSorted.Sort();
            var opponentSorted = new List<char>(_opponentKnownLetters);
            opponentSorted.Sort();

            Debug.Log(string.Format("[GameplayUI] Your Known Letters: {0}",
                playerSorted.Count > 0 ? string.Join(", ", playerSorted) : "(none)"));
            Debug.Log(string.Format("[GameplayUI] Opponent's Known Letters: {0}",
                opponentSorted.Count > 0 ? string.Join(", ", opponentSorted) : "(none)"));
        }

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

            // Hide feedback text initially
            if (_wordGuessFeedbackText != null)
                _wordGuessFeedbackText.gameObject.SetActive(false);
        }



        private void Update()
        {
            // Handle physical keyboard input during word guess mode
            if (_letterTrackerInKeyboardMode && _activeWordGuessRow != null)
            {
                var keyboard = Keyboard.current;
                if (keyboard == null) return;

                // Check for letter keys (A-Z)
                for (int i = 0; i < 26; i++)
                {
                    Key key = Key.A + i;
                    if (keyboard[key].wasPressedThisFrame)
                    {
                        char letter = (char)('A' + i);
                        HandleKeyboardLetterInput(letter);
                    }
                }

                // Check for backspace
                if (keyboard.backspaceKey.wasPressedThisFrame)
                {
                    _activeWordGuessRow.BackspaceGuessLetter();
                }

                // Check for Enter to submit
                if (keyboard.enterKey.wasPressedThisFrame || keyboard.numpadEnterKey.wasPressedThisFrame)
                {
                    if (_activeWordGuessRow.IsGuessComplete())
                    {
                        _activeWordGuessRow.ExitWordGuessMode(true);
                    }
                }

                // Check for Escape to cancel
                if (keyboard.escapeKey.wasPressedThisFrame)
                {
                    _activeWordGuessRow.ExitWordGuessMode(false);
                }
            }
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

            // Initialize both player and opponent state tracking
            InitializePlayerState();
            InitializeOpponentState();

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

            // Exit any active word guess mode
            ExitWordGuessMode();

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
                // Use the correct tuple-returning methods
                var (name, color) = _setupSettingsPanel.GetPlayerSettings();
                var (gridSize, wordCount, difficulty) = _setupSettingsPanel.GetDifficultySettings();

                _playerSetupData.PlayerName = name;
                _playerSetupData.PlayerColor = color;
                _playerSetupData.GridSize = gridSize;
                _playerSetupData.WordCount = (int)wordCount + 3;
                _playerSetupData.DifficultyLevel = difficulty;

                Debug.Log(string.Format("[GameplayUI] Captured: {0}, {1}x{1} grid, {2} words, {3}",
                    _playerSetupData.PlayerName,
                    _playerSetupData.GridSize,
                    _playerSetupData.WordCount,
                    _playerSetupData.DifficultyLevel));
            }

            // Capture placed words from setup grid
            if (_setupGridPanel != null)
            {
                CaptureWordsFromGrid(_setupGridPanel, _playerSetupData);

                // Derive WordLengths from captured words
                _playerSetupData.WordLengths = _playerSetupData.PlacedWords
                    .Select(w => w.Word.Length)
                    .ToArray();
            }
        }

        /// <summary>
        /// Captures word placement data from a grid panel
        /// </summary>
        private void CaptureWordsFromGrid(PlayerGridPanel panel, SetupData setupData)
        {
            var rows = panel.GetWordPatternRows();
            if (rows == null)
            {
                Debug.LogError("[GameplayUI] No word pattern rows found!");
                return;
            }

            for (int i = 0; i < rows.Length; i++)
            {
                var row = rows[i];
                if (row != null && row.gameObject.activeSelf && row.HasWord && row.IsPlaced)
                {
                    var wordData = new WordPlacementData
                    {
                        Word = row.CurrentWord,
                        StartCol = row.PlacedStartCol,
                        StartRow = row.PlacedStartRow,
                        DirCol = row.PlacedDirCol,
                        DirRow = row.PlacedDirRow,
                        RowIndex = i
                    };

                    setupData.PlacedWords.Add(wordData);

                    Debug.Log(string.Format("[GameplayUI] Captured word {0}: '{1}' at ({2},{3}) dir({4},{5})",
                        i + 1, wordData.Word, wordData.StartCol, wordData.StartRow, wordData.DirCol, wordData.DirRow));
                }
            }
        }

        /// <summary>
        /// Generates opponent data (AI placeholder for now - uses same words as player)
        /// </summary>
        private void GenerateOpponentData()
        {
            _opponentSetupData = new SetupData
            {
                PlayerName = "Executioner",
                PlayerColor = Color.brown,
                GridSize = 10,  // Test with 10x10 grid
                WordCount = 4,
                DifficultyLevel = _playerSetupData.DifficultyLevel,
                WordLengths = _playerSetupData.WordLengths
            };

            // For testing, generate different words for opponent
            // In the future, this would be AI-generated or second player's input
            string[] testWords = { "HOG", "ROAD", "SNORE", "BRIDGE" };
            int[] testStartCols = { 2, 5, 3, 7 };
            int[] testStartRows = { 1, 3, 5, 7 };

            for (int i = 0; i < 4; i++)
            {
                var opponentWord = new WordPlacementData
                {
                    Word = testWords[i],
                    StartCol = testStartCols[i],
                    StartRow = testStartRows[i],
                    DirCol = 1,  // Horizontal
                    DirRow = 0,
                    RowIndex = i
                };

                _opponentSetupData.PlacedWords.Add(opponentWord);

                Debug.Log(string.Format("[GameplayUI] Generated opponent word {0}: '{1}' at ({2},{3})",
                    i + 1, opponentWord.Word, opponentWord.StartCol, opponentWord.StartRow));
            }
        }

        #endregion

        #region Panel Configuration

        /// <summary>
        /// Configures the owner panel (player's own words - fully revealed)
        /// </summary>
        private void ConfigureOwnerPanel()
        {
            if (_ownerPanel == null || _playerSetupData == null)
            {
                Debug.LogError("[GameplayUI] Cannot configure owner panel - missing references!");
                return;
            }

            // Initialize grid
            _ownerPanel.InitializeGrid(_playerSetupData.GridSize);

            // Set player name and color
            _ownerPanel.SetPlayerName(_playerSetupData.PlayerName);
            _ownerPanel.SetPlayerColor(_playerSetupData.PlayerColor);

            // Set to Gameplay mode FIRST
            _ownerPanel.SetMode(PlayerGridPanel.PanelMode.Gameplay);

            // CRITICAL: Cache word pattern rows before we try to use them
            _ownerPanel.CacheWordPatternRows();

            // Place words on the grid AND set up word pattern rows
            foreach (var wordData in _playerSetupData.PlacedWords)
            {
                // Place letters on grid (revealed)
                PlaceWordOnPanelRevealed(_ownerPanel, wordData);

                // Set up the word pattern row to show revealed word
                var row = _ownerPanel.GetWordPatternRow(wordData.RowIndex);
                if (row != null)
                {
                    row.SetGameplayWord(wordData.Word);
                    row.RevealAllLetters();
                    row.SetAsOwnerPanel();  // Permanently prevents guess buttons
                    Debug.Log(string.Format("[GameplayUI] Owner row {0}: Set word '{1}' (revealed)", wordData.RowIndex + 1, wordData.Word));
                }
                else
                {
                    Debug.LogError(string.Format("[GameplayUI] Owner row {0} is NULL! Cannot set word '{1}'", wordData.RowIndex, wordData.Word));
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

                        // ALSO hide guess buttons on ALL owner rows (active or not)
                        allRows[i].SetAsOwnerPanel();
                    }
                }
                Debug.Log(string.Format("[GameplayUI] Owner panel: Showing {0} rows, hiding {1} unused rows", wordCount, allRows.Length - wordCount));
            }
        }

        /// <summary>
        /// Configures the opponent panel (opponent's words - hidden)
        /// </summary>
        private void ConfigureOpponentPanel()
        {
            if (_opponentPanel == null || _opponentSetupData == null)
            {
                Debug.LogError("[GameplayUI] Cannot configure opponent panel - missing references!");
                return;
            }

            // Initialize grid
            _opponentPanel.InitializeGrid(_opponentSetupData.GridSize);

            // Set to Gameplay mode FIRST
            _opponentPanel.SetMode(PlayerGridPanel.PanelMode.Gameplay);

            _opponentPanel.SetPlayerName(_opponentSetupData.PlayerName);
            _opponentPanel.SetPlayerColor(_opponentSetupData.PlayerColor);

            // CRITICAL: Cache word pattern rows before we try to use them
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
                    Debug.Log(string.Format("[GameplayUI] Opponent row {0}: Set word '{1}' (hidden)", wordData.RowIndex + 1, wordData.Word));
                }
                else
                {
                    Debug.LogError(string.Format("[GameplayUI] Opponent row {0} is NULL! Cannot set word '{1}'", wordData.RowIndex, wordData.Word));
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
                Debug.Log(string.Format("[GameplayUI] Opponent panel: Showing {0} rows, hiding {1} unused rows", wordCount, allRows.Length - wordCount));
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
                _opponentPanel.OnLetterClicked += HandleLetterClicked;
                _opponentPanel.OnCellClicked += HandleCellGuess;

                // Subscribe to word guess events from opponent panel's word pattern rows
                var rows = _opponentPanel.GetWordPatternRows();
                if (rows != null)
                {
                    foreach (var row in rows)
                    {
                        if (row != null)
                        {
                            row.OnWordGuessStarted += HandleWordGuessStarted;
                            row.OnWordGuessSubmitted += HandleWordGuessSubmitted;
                            row.OnWordGuessCancelled += HandleWordGuessCancelled;
                        }
                    }
                }
            }
        }

        private void UnsubscribeFromPanelEvents()
        {
            if (_opponentPanel != null)
            {
                _opponentPanel.OnLetterClicked -= HandleLetterClicked;
                _opponentPanel.OnCellClicked -= HandleCellGuess;

                // Unsubscribe from word guess events
                var rows = _opponentPanel.GetWordPatternRows();
                if (rows != null)
                {
                    foreach (var row in rows)
                    {
                        if (row != null)
                        {
                            row.OnWordGuessStarted -= HandleWordGuessStarted;
                            row.OnWordGuessSubmitted -= HandleWordGuessSubmitted;
                            row.OnWordGuessCancelled -= HandleWordGuessCancelled;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Handle player clicking a letter in opponent's letter tracker
        /// Routes to either keyboard mode (typing) or letter guess mode
        /// </summary>
        private void HandleLetterClicked(char letter)
        {
            // If in word guess mode, route letter to the active word guess row
            if (_letterTrackerInKeyboardMode && _activeWordGuessRow != null)
            {
                HandleKeyboardLetterInput(letter);
                return;
            }

            // Otherwise, treat as a letter guess
            HandleLetterGuess(letter);
        }

        /// <summary>
        /// Handle letter input during word guess keyboard mode
        /// </summary>
        private void HandleKeyboardLetterInput(char letter)
        {
            if (_activeWordGuessRow == null)
            {
                Debug.LogWarning("[GameplayUI] No active word guess row for keyboard input!");
                return;
            }

            letter = char.ToUpper(letter);
            bool success = _activeWordGuessRow.TypeGuessLetter(letter);

            if (success)
            {
                Debug.Log(string.Format("[GameplayUI] Typed '{0}' in word guess row", letter));
            }
        }

        /// <summary>
        /// Handle player clicking a letter to make a letter guess
        /// </summary>
        private void HandleLetterGuess(char letter)
        {
            // Block if not player's turn or game is over
            if (!_isPlayerTurn || _gameOver)
            {
                Debug.LogWarning("[GameplayUI] Not player's turn or game is over!");
                return;
            }

            letter = char.ToUpper(letter);
            GuessResult result = ProcessPlayerLetterGuess(letter);

            if (result == GuessResult.AlreadyGuessed)
            {
                Debug.Log(string.Format("[GameplayUI] Letter '{0}' already guessed - try again!", letter));
                return; // Don't end turn - let player guess again
            }

            Debug.Log(string.Format("[GameplayUI] Player guessed letter '{0}': {1}", letter, result == GuessResult.Hit ? "HIT" : "MISS"));

            // End player's turn after making a valid guess
            EndPlayerTurn();
        }

        /// <summary>
        /// Handle player clicking a cell in opponent's grid
        /// </summary>
        private void HandleCellGuess(int column, int row)
        {
            // Block cell guesses while in word guess mode
            if (_letterTrackerInKeyboardMode)
            {
                Debug.Log("[GameplayUI] Cannot guess coordinates while in word guess mode!");
                return;
            }

            // Block if not player's turn or game is over
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
                Debug.Log(string.Format("[GameplayUI] Coordinate {0} already guessed - try again!", coordLabel));
                return; // Don't end turn - let player guess again
            }

            Debug.Log(string.Format("[GameplayUI] Player guessed coordinate {0}: {1}", coordLabel, result == GuessResult.Hit ? "HIT" : "MISS"));

            // End player's turn after making a valid guess
            EndPlayerTurn();
        }

        #endregion

        #region Word Guess Mode Event Handlers

        /// <summary>
        /// Handle when a word pattern row enters word guess mode
        /// </summary>
        private void HandleWordGuessStarted(int rowNumber)
        {
            int rowIndex = rowNumber - 1;  // Convert 1-indexed to 0-indexed
            // Block if not player's turn or game is over
            if (!_isPlayerTurn || _gameOver)
            {
                Debug.LogWarning("[GameplayUI] Cannot start word guess - not player's turn or game is over!");
                // Force exit the row's guess mode
                var rows = _opponentPanel.GetWordPatternRows();
                if (rows != null && rowIndex < rows.Length && rows[rowIndex] != null)
                {
                    rows[rowIndex].ExitWordGuessMode(false);
                }
                return;
            }

            // If another row is already in guess mode, cancel it first
            if (_activeWordGuessRow != null)
            {
                _activeWordGuessRow.ExitWordGuessMode(false);
            }

            // Get the new active row
            var allRows = _opponentPanel.GetWordPatternRows();
            if (allRows != null && rowIndex < allRows.Length)
            {
                _activeWordGuessRow = allRows[rowIndex];
            }

            // Switch letter tracker to keyboard mode
            SwitchLetterTrackerToKeyboardMode();

            Debug.Log(string.Format("[GameplayUI] Word guess mode started for row {0}", rowIndex + 1));

            // Hide guess word buttons on ALL other rows            
            if (allRows != null)
            {
                for (int i = 0; i < allRows.Length; i++)
                {
                    if (allRows[i] != null && i != rowIndex)
                    {
                        allRows[i].HideGuessWordButton();
                    }
                }
            }
        }

        /// <summary>
        /// Handle when a word guess is submitted
        /// </summary>
private void HandleWordGuessSubmitted(int rowNumber, string guessedWord)
        {
            int rowIndex = rowNumber - 1;  // Convert 1-indexed to 0-indexed

            Debug.Log(string.Format("[GameplayUI] === HandleWordGuessSubmitted START: rowNumber={0}, rowIndex={1}, word='{2}' ===", 
                rowNumber, rowIndex, guessedWord));
            
            // Restore letter tracker before processing (so colors update correctly)
            RestoreLetterTrackerFromKeyboardMode();
            _activeWordGuessRow = null;

            // Process the word guess
            GuessResult result = ProcessPlayerWordGuess(guessedWord, rowIndex);
            Debug.Log(string.Format("[GameplayUI] ProcessPlayerWordGuess returned: {0}", result));

            switch (result)
            {
                case GuessResult.InvalidWord:
                    ShowFeedback("Not a valid word - try again!");
                    // Re-enter guess mode for the same row
                    var rows = _opponentPanel.GetWordPatternRows();
                    if (rows != null && rowIndex < rows.Length && rows[rowIndex] != null)
                    {
                        rows[rowIndex].EnterWordGuessMode();
                    }
                    return; // Don't end turn

                case GuessResult.AlreadyGuessed:
                    ShowFeedback("Already guessed that word!");
                    return; // Don't end turn

                case GuessResult.Hit:
                    ShowFeedback("Correct!");
                    Debug.Log(string.Format("[GameplayUI] Player guessed word '{0}': CORRECT!", guessedWord));
                    break;

                case GuessResult.Miss:
                    ShowFeedback("Wrong! (+2 misses)");
                    Debug.Log(string.Format("[GameplayUI] Player guessed word '{0}': WRONG (+2 misses)", guessedWord));
                    break;
            }

            Debug.Log(string.Format("[GameplayUI] Before EndPlayerTurn, solved rows: [{0}]", 
                string.Join(", ", _playerSolvedWordRows)));

            // End player's turn after valid (non-invalid) guess
            EndPlayerTurn();

            Debug.Log(string.Format("[GameplayUI] Before ShowAllGuessWordButtons, solved rows: [{0}]", 
                string.Join(", ", _playerSolvedWordRows)));

            // Show guess word buttons on all rows again
            ShowAllGuessWordButtons();

            Debug.Log("[GameplayUI] === HandleWordGuessSubmitted END ===");
        }

        /// <summary>
        /// Handle when a word guess is cancelled
        /// </summary>
        private void HandleWordGuessCancelled(int rowNumber)
        {
            int rowIndex = rowNumber - 1;  // Convert 1-indexed to 0-indexed

            Debug.Log(string.Format("[GameplayUI] Word guess cancelled for row {0}", rowNumber));

            // Restore letter tracker
            RestoreLetterTrackerFromKeyboardMode();
            _activeWordGuessRow = null;

            // Show guess word buttons on all rows again
            ShowAllGuessWordButtons();
        }

        /// <summary>
        /// Show guess word buttons on all opponent panel rows
        /// </summary>
private void ShowAllGuessWordButtons()
        {
            Debug.Log(string.Format("[GameplayUI] ShowAllGuessWordButtons called. Solved rows: [{0}]", 
                string.Join(", ", _playerSolvedWordRows)));

            var allRows = _opponentPanel.GetWordPatternRows();
            if (allRows != null)
            {
                for (int i = 0; i < allRows.Length; i++)
                {
                    if (allRows[i] != null)
                    {
                        // Explicitly HIDE solved rows (belt-and-suspenders approach)
                        if (_playerSolvedWordRows.Contains(i))
                        {
                            Debug.Log(string.Format("[GameplayUI] Row {0} is SOLVED - hiding button", i));
                            allRows[i].HideGuessWordButton();
                            continue;
                        }
                        Debug.Log(string.Format("[GameplayUI] Row {0} is NOT solved - showing button", i));
                        allRows[i].ShowGuessWordButton();
                    }
                }
            }
        }

        /// <summary>
        /// Force exit any active word guess mode
        /// </summary>
        private void ExitWordGuessMode()
        {
            if (_activeWordGuessRow != null)
            {
                _activeWordGuessRow.ExitWordGuessMode(false);
                _activeWordGuessRow = null;
            }

            if (_letterTrackerInKeyboardMode)
            {
                RestoreLetterTrackerFromKeyboardMode();
            }
        }

        #endregion

        #region Letter Tracker Keyboard Mode

        /// <summary>
        /// Save current letter states and switch tracker to keyboard mode (all white)
        /// </summary>
        private void SwitchLetterTrackerToKeyboardMode()
        {
            if (_opponentPanel == null) return;

            _savedLetterStates.Clear();

            // Save current states for all letters
            for (char c = 'A'; c <= 'Z'; c++)
            {
                var state = _opponentPanel.GetLetterState(c);
                _savedLetterStates[c] = state;

                // Set all to Normal (white) for keyboard mode
                _opponentPanel.SetLetterState(c, LetterButton.LetterState.Normal);
            }

            _letterTrackerInKeyboardMode = true;
            Debug.Log("[GameplayUI] Letter tracker switched to keyboard mode");
        }

        /// <summary>
        /// Restore saved letter states when exiting keyboard mode
        /// </summary>
        private void RestoreLetterTrackerFromKeyboardMode()
        {
            if (_opponentPanel == null || !_letterTrackerInKeyboardMode) return;

            // Restore saved states
            foreach (var kvp in _savedLetterStates)
            {
                _opponentPanel.SetLetterState(kvp.Key, kvp.Value);
            }

            _savedLetterStates.Clear();
            _letterTrackerInKeyboardMode = false;
            Debug.Log("[GameplayUI] Letter tracker restored from keyboard mode");
        }

        #endregion

        #region Feedback Display

        /// <summary>
        /// Show feedback message to player
        /// </summary>
        private void ShowFeedback(string message)
        {
            Debug.Log(string.Format("[GameplayUI] Feedback: {0}", message));

            if (_wordGuessFeedbackText != null)
            {
                _wordGuessFeedbackText.text = message;
                _wordGuessFeedbackText.gameObject.SetActive(true);

                // Auto-hide after duration
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

        /// <summary>
        /// End player's turn and switch to opponent's turn
        /// </summary>
        private void EndPlayerTurn()
        {
            if (_gameOver) return;

            // Exit any active word guess mode
            ExitWordGuessMode();

            _isPlayerTurn = false;
            Debug.Log("[GameplayUI] === Player's turn ended. Opponent's turn. ===");

            // In a real game, AI would take its turn here
            // For now, use test buttons to simulate opponent turn
        }

        /// <summary>
        /// End opponent's turn and switch to player's turn
        /// </summary>
        private void EndOpponentTurn()
        {
            if (_gameOver) return;

            _isPlayerTurn = true;
            Debug.Log("[GameplayUI] === Opponent's turn ended. Player's turn. ===");
        }

        /// <summary>
        /// Check if it's currently the player's turn
        /// </summary>
        public bool IsPlayerTurn => _isPlayerTurn;

        /// <summary>
        /// Check if the game is over
        /// </summary>
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

            Debug.Log(string.Format("[GameplayUI] Player misses: {0} / {1}", _playerMisses, _playerMissLimit));

            // Check for player lose condition
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

            Debug.Log(string.Format("[GameplayUI] Opponent misses: {0} / {1}", _opponentMisses, _opponentMissLimit));

            // Check for opponent lose condition
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

        /// <summary>
        /// Initialize player state when gameplay starts
        /// </summary>
        private void InitializePlayerState()
        {
            _playerMisses = 0;
            _playerKnownLetters.Clear();
            _playerGuessedLetters.Clear();
            _playerGuessedCoordinates.Clear();
            _playerGuessedWords.Clear();
            _playerSolvedWordRows.Clear();  // Reset solved word rows for new game

            _playerMissLimit = CalculateMissLimit(_playerSetupData.DifficultyLevel, _opponentSetupData);

            Debug.Log(string.Format("[GameplayUI] Player miss limit: {0}", _playerMissLimit));
        }

        /// <summary>
        /// Process player guessing a letter against opponent's words
        /// </summary>
        private GuessResult ProcessPlayerLetterGuess(char letter)
        {
            letter = char.ToUpper(letter);

            // Check for duplicate guess
            if (_playerGuessedLetters.Contains(letter))
            {
                Debug.LogWarning(string.Format("[GameplayUI] Already guessed letter '{0}'!", letter));
                return GuessResult.AlreadyGuessed;
            }

            // Mark as guessed
            _playerGuessedLetters.Add(letter);

            bool foundLetter = false;

            // Check if letter exists in any of opponent's words
            foreach (var word in _opponentSetupData.PlacedWords)
            {
                if (word.Word.ToUpper().Contains(letter))
                {
                    foundLetter = true;
                    break;
                }
            }

            if (foundLetter)
            {
                // Add to known letters
                _playerKnownLetters.Add(letter);

                // Update opponent panel - reveal this letter in word pattern rows
                UpdateOpponentPanelForLetter(letter);

                // Update any cells that contain this letter AND were already coordinate-guessed
                UpdateOpponentGridCellsForLetter(letter);

                // Mark letter button as HIT (green) in opponent's letter tracker
                _opponentPanel.SetLetterState(letter, LetterButton.LetterState.Hit);

                // Upgrade any yellow "hit but letter unknown" cells to green
                UpgradeOpponentGridCellsForLetter(letter);

                return GuessResult.Hit;
            }
            else
            {
                // Miss - increment counter
                _playerMisses++;
                UpdatePlayerMissCounter();

                // Mark letter button as MISS (red) in opponent's letter tracker
                _opponentPanel.SetLetterState(letter, LetterButton.LetterState.Miss);

                return GuessResult.Miss;
            }
        }

        /// <summary>
        /// Process player guessing a coordinate against opponent's grid
        /// </summary>
        private GuessResult ProcessPlayerCoordinateGuess(int col, int row)
        {
            Vector2Int coord = new Vector2Int(col, row);

            // Check for duplicate guess
            if (_playerGuessedCoordinates.Contains(coord))
            {
                Debug.LogWarning(string.Format("[GameplayUI] Already guessed coordinate ({0}, {1})!", col, row));
                return GuessResult.AlreadyGuessed;
            }

            // Mark as guessed
            _playerGuessedCoordinates.Add(coord);

            // Check if coordinate hits any letter in opponent's words
            char? hitLetter = null;

            foreach (var word in _opponentSetupData.PlacedWords)
            {
                int checkCol = word.StartCol;
                int checkRow = word.StartRow;

                for (int i = 0; i < word.Word.Length; i++)
                {
                    if (checkCol == col && checkRow == row)
                    {
                        hitLetter = word.Word[i];
                        break;
                    }
                    checkCol += word.DirCol;
                    checkRow += word.DirRow;
                }

                if (hitLetter.HasValue) break;
            }

            if (hitLetter.HasValue)
            {
                // Get cell - mark differently based on whether letter is known
                var cell = _opponentPanel.GetCell(col, row);
                if (cell != null)
                {
                    char upperLetter = char.ToUpper(hitLetter.Value);
                    if (_playerKnownLetters.Contains(upperLetter))
                    {
                        // Letter is known - mark green and reveal
                        cell.MarkAsGuessed(true);
                        cell.RevealHiddenLetter();
                    }
                    else
                    {
                        // Letter NOT known yet - mark yellow/orange
                        cell.MarkAsHitButLetterUnknown();
                    }
                }

                return GuessResult.Hit;
            }
            else
            {
                // Miss - mark cell (red background) and increment counter
                var cell = _opponentPanel.GetCell(col, row);
                if (cell != null)
                {
                    cell.MarkAsGuessed(false);
                }

                _playerMisses++;
                UpdatePlayerMissCounter();

                return GuessResult.Miss;
            }
        }

        /// <summary>
        /// Process player guessing a complete word against opponent
        /// </summary>
private GuessResult ProcessPlayerWordGuess(string guessedWord, int rowIndex)
        {
            string normalizedGuess = guessedWord.Trim().ToUpper();

            Debug.Log(string.Format("[GameplayUI] ProcessPlayerWordGuess: word='{0}', rowIndex={1}", normalizedGuess, rowIndex));

            // First validate against word bank
            if (!ValidateWordAgainstWordBank(normalizedGuess))
            {
                Debug.LogWarning(string.Format("[GameplayUI] '{0}' is not a valid word in the word bank!", normalizedGuess));
                return GuessResult.InvalidWord;
            }

            // Check for duplicate guess
            if (_playerGuessedWords.Contains(normalizedGuess))
            {
                Debug.LogWarning(string.Format("[GameplayUI] Already guessed word '{0}'!", normalizedGuess));
                return GuessResult.AlreadyGuessed;
            }

            // Mark as guessed
            _playerGuessedWords.Add(normalizedGuess);

            // Check if word matches the opponent's word at this row index
            WordPlacementData targetWord = null;
            if (rowIndex < _opponentSetupData.PlacedWords.Count)
            {
                targetWord = _opponentSetupData.PlacedWords[rowIndex];
                Debug.Log(string.Format("[GameplayUI] Target word at index {0}: '{1}'", rowIndex, targetWord.Word));
            }

            if (targetWord != null && targetWord.Word.ToUpper() == normalizedGuess)
            {
                // Correct guess - add all letters to known
                foreach (char c in targetWord.Word.ToUpper())
                {
                    _playerKnownLetters.Add(c);
                    _playerGuessedLetters.Add(c);
                }

                // CRITICAL: Add to solved rows FIRST, before any UI updates
                _playerSolvedWordRows.Add(rowIndex);
                Debug.Log(string.Format("[GameplayUI] SOLVED: Added rowIndex {0} to solved rows. Set now: [{1}]",
                    rowIndex, string.Join(", ", _playerSolvedWordRows)));

                // Update word pattern row for the guessed word - reveal all letters
                var row = _opponentPanel.GetWordPatternRow(rowIndex);
                if (row != null)
                {
                    row.RevealAllLetters();
                    row.MarkWordSolved();
                    Debug.Log(string.Format("[GameplayUI] Called HideGuessWordButton on row object for rowIndex {0}", rowIndex));
                }
                else
                {
                    Debug.LogError(string.Format("[GameplayUI] GetWordPatternRow({0}) returned NULL!", rowIndex));
                }

                // Update OTHER word pattern rows to show any matching letters
                foreach (char c in targetWord.Word.ToUpper())
                {
                    UpdateOpponentPanelForLetter(c);
                    _opponentPanel.SetLetterState(c, LetterButton.LetterState.Hit);
                    UpgradeOpponentGridCellsForLetter(c);
                }

                // Add to player's guessed word list (correct guess)
                if (_player1GuessedWordList != null)
                {
                    _player1GuessedWordList.AddGuessedWord(normalizedGuess, true);
                }

                return GuessResult.Hit;
            }
            else
            {
                // Wrong guess - double penalty
                _playerMisses += 2;
                UpdatePlayerMissCounter();

                // Add to player's guessed word list (wrong guess)
                if (_player1GuessedWordList != null)
                {
                    _player1GuessedWordList.AddGuessedWord(normalizedGuess, false);
                }

                return GuessResult.Miss;
            }
        }

        /// <summary>
        /// Validate a word against the word bank
        /// </summary>
        private bool ValidateWordAgainstWordBank(string word)
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

        /// <summary>
        /// Get the appropriate word list for a given word length
        /// </summary>
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

        /// <summary>
        /// Update opponent panel word pattern rows when player discovers a letter
        /// </summary>
        private void UpdateOpponentPanelForLetter(char letter)
        {
            var rows = _opponentPanel.GetWordPatternRows();
            if (rows == null) return;

            for (int i = 0; i < _opponentSetupData.PlacedWords.Count && i < rows.Length; i++)
            {
                var wordData = _opponentSetupData.PlacedWords[i];
                var row = rows[i];

                if (row != null && wordData.Word.ToUpper().Contains(letter))
                {
                    // Reveal all instances of this letter in the word pattern row
                    // NOTE: Use RevealAllInstancesOfLetter(char), NOT RevealLetter(int index)!
                    int revealed = row.RevealAllInstancesOfLetter(letter);
                    if (revealed > 0)
                    {
                        Debug.Log(string.Format("[GameplayUI] Revealed {0} instance(s) of '{1}' in opponent word row {2}",
                            revealed, letter, i + 1));
                    }
                }
            }
        }

        /// <summary>
        /// Update opponent grid cells when player discovers a letter
        /// </summary>
        private void UpdateOpponentGridCellsForLetter(char letter)
        {
            foreach (var word in _opponentSetupData.PlacedWords)
            {
                int col = word.StartCol;
                int row = word.StartRow;

                for (int i = 0; i < word.Word.Length; i++)
                {
                    if (char.ToUpper(word.Word[i]) == letter)
                    {
                        var cell = _opponentPanel.GetCell(col, row);
                        // ONLY reveal letter if cell was already coordinate-guessed (showing asterisk)
                        // Letter guesses should NOT reveal unguessed cells on the grid
                        if (cell != null && cell.HasBeenGuessed)
                        {
                            cell.RevealHiddenLetter();
                        }
                    }
                    col += word.DirCol;
                    row += word.DirRow;
                }
            }
        }

        /// <summary>
        /// Reveal all cells of a word on the opponent panel
        /// </summary>
        private void RevealWordOnOpponentPanel(WordPlacementData wordData)
        {
            int col = wordData.StartCol;
            int row = wordData.StartRow;

            for (int i = 0; i < wordData.Word.Length; i++)
            {
                var cell = _opponentPanel.GetCell(col, row);
                if (cell != null)
                {
                    cell.MarkAsGuessed(true);
                    cell.RevealHiddenLetter();
                }
                col += wordData.DirCol;
                row += wordData.DirRow;
            }
        }

        #endregion

        #region Opponent Guess Processing

        /// <summary>
        /// Initialize opponent state when gameplay starts
        /// </summary>
        private void InitializeOpponentState()
        {
            _opponentMisses = 0;
            _opponentKnownLetters.Clear();
            _opponentGuessedLetters.Clear();
            _opponentGuessedCoordinates.Clear();
            _opponentGuessedWords.Clear();
            _opponentMissLimit = CalculateMissLimit(_opponentSetupData.DifficultyLevel, _playerSetupData);

            Debug.Log(string.Format("[GameplayUI] Opponent miss limit: {0}", _opponentMissLimit));
        }

        /// <summary>
        /// Process opponent guessing a letter against player's words
        /// </summary>
        private GuessResult ProcessOpponentLetterGuess(char letter)
        {
            letter = char.ToUpper(letter);

            // Check for duplicate guess
            if (_opponentGuessedLetters.Contains(letter))
            {
                Debug.LogWarning(string.Format("[GameplayUI] Opponent already guessed letter '{0}'!", letter));
                return GuessResult.AlreadyGuessed;
            }

            // Mark as guessed
            _opponentGuessedLetters.Add(letter);

            bool foundLetter = false;

            // Check if letter exists in any of player's words
            foreach (var word in _playerSetupData.PlacedWords)
            {
                if (word.Word.ToUpper().Contains(letter))
                {
                    foundLetter = true;
                    break;
                }
            }

            if (foundLetter)
            {
                // Add to known letters
                _opponentKnownLetters.Add(letter);

                // Update owner panel - reveal this letter in word pattern rows
                UpdateOwnerPanelForLetter(letter);

                // Upgrade any yellow "hit but letter unknown" cells to green
                UpgradeOwnerGridCellsForLetter(letter);

                // Mark letter button as HIT (green) in owner's letter tracker
                _ownerPanel.SetLetterState(letter, LetterButton.LetterState.Hit);

                return GuessResult.Hit;
            }
            else
            {
                // Miss - increment counter
                _opponentMisses++;
                UpdateOpponentMissCounter();

                // Mark letter button as MISS (red) in owner's letter tracker
                _ownerPanel.SetLetterState(letter, LetterButton.LetterState.Miss);

                return GuessResult.Miss;
            }
        }

        /// <summary>
        /// Process opponent guessing a coordinate against player's grid
        /// </summary>
        private GuessResult ProcessOpponentCoordinateGuess(int col, int row)
        {
            Vector2Int coord = new Vector2Int(col, row);

            // Check for duplicate guess
            if (_opponentGuessedCoordinates.Contains(coord))
            {
                Debug.LogWarning(string.Format("[GameplayUI] Opponent already guessed coordinate ({0}, {1})!", col, row));
                return GuessResult.AlreadyGuessed;
            }

            // Mark as guessed
            _opponentGuessedCoordinates.Add(coord);

            // Check if coordinate hits any letter in player's words
            char? hitLetter = null;

            foreach (var word in _playerSetupData.PlacedWords)
            {
                int checkCol = word.StartCol;
                int checkRow = word.StartRow;

                for (int i = 0; i < word.Word.Length; i++)
                {
                    if (checkCol == col && checkRow == row)
                    {
                        hitLetter = word.Word[i];
                        break;
                    }
                    checkCol += word.DirCol;
                    checkRow += word.DirRow;
                }

                if (hitLetter.HasValue) break;
            }

            if (hitLetter.HasValue)
            {
                // Get cell - mark differently based on whether letter is known
                var cell = _ownerPanel.GetCell(col, row);
                if (cell != null)
                {
                    char upperLetter = char.ToUpper(hitLetter.Value);
                    if (_opponentKnownLetters.Contains(upperLetter))
                    {
                        // Letter is known - mark green
                        cell.MarkAsGuessed(true);
                    }
                    else
                    {
                        // Letter NOT known yet - mark yellow/orange
                        cell.MarkAsHitButLetterUnknown();
                    }
                }

                return GuessResult.Hit;
            }
            else
            {
                // Miss - mark cell (red background) and increment counter
                var cell = _ownerPanel.GetCell(col, row);
                if (cell != null)
                {
                    cell.MarkAsGuessed(false);
                }

                _opponentMisses++;
                UpdateOpponentMissCounter();

                return GuessResult.Miss;
            }
        }

        /// <summary>
        /// Process opponent guessing a complete word
        /// </summary>
        private GuessResult ProcessOpponentWordGuess(string guessedWord)
        {
            string normalizedGuess = guessedWord.Trim().ToUpper();

            // ADD THIS BLOCK - Validate against word bank first
            if (!ValidateWordAgainstWordBank(normalizedGuess))
            {
                Debug.LogWarning(string.Format("[GameplayUI] Opponent word '{0}' is not a valid word in the word bank!", normalizedGuess));
                return GuessResult.InvalidWord;
            }

            // Check for duplicate guess (existing code continues...)
            if (_opponentGuessedWords.Contains(normalizedGuess))

            {
                Debug.LogWarning(string.Format("[GameplayUI] Opponent already guessed word '{0}'!", normalizedGuess));
                return GuessResult.AlreadyGuessed;
            }

            // Mark as guessed
            _opponentGuessedWords.Add(normalizedGuess);

            // Check if word matches any of player's words
            WordPlacementData matchedWord = null;
            foreach (var word in _playerSetupData.PlacedWords)
            {
                if (word.Word.ToUpper() == normalizedGuess)
                {
                    matchedWord = word;
                    break;
                }
            }

            if (matchedWord != null)
            {
                // Correct guess - add all letters to known
                foreach (char c in matchedWord.Word.ToUpper())
                {
                    _opponentKnownLetters.Add(c);

                    // Also mark as guessed letter (for tracking)
                    _opponentGuessedLetters.Add(c);
                }

                // Update word pattern row for the guessed word
                var row = _ownerPanel.GetWordPatternRow(matchedWord.RowIndex);
                if (row != null)
                {
                    row.RevealAllLetters();
                }

                // Update OTHER word pattern rows to show any matching letters
                foreach (char c in matchedWord.Word.ToUpper())
                {
                    UpdateOwnerPanelForLetter(c);

                    // Update letter tracker to show these letters as known (Hit/green)
                    _ownerPanel.SetLetterState(c, LetterButton.LetterState.Hit);

                    // Upgrade any yellow cells to green
                    

                // Add to opponent's guessed word list (correct guess)
                if (_player2GuessedWordList != null)
                {
                    _player2GuessedWordList.AddGuessedWord(normalizedGuess, true);
                }

                
UpgradeOwnerGridCellsForLetter(c);
                }

                return GuessResult.Hit;
            }
            else
            {
                // Wrong guess - double penalty
                _opponentMisses += 2;
                UpdateOpponentMissCounter();

                // Add to opponent's guessed word list (wrong guess)
                if (_player2GuessedWordList != null)
                {
                    _player2GuessedWordList.AddGuessedWord(normalizedGuess, false);
                }


                return GuessResult.Miss;
            }
        }

        /// <summary>
        /// Update owner panel word pattern rows when opponent discovers a letter
        /// </summary>
        private void UpdateOwnerPanelForLetter(char letter)
        {
            var rows = _ownerPanel.GetWordPatternRows();
            if (rows == null) return;

            for (int i = 0; i < _playerSetupData.PlacedWords.Count && i < rows.Length; i++)
            {
                var wordData = _playerSetupData.PlacedWords[i];
                var row = rows[i];

                if (row != null && wordData.Word.ToUpper().Contains(letter))
                {
                    // Reveal all instances of this letter in the word pattern row
                    // NOTE: Use RevealAllInstancesOfLetter(char), NOT RevealLetter(int index)!
                    int revealed = row.RevealAllInstancesOfLetter(letter);
                    if (revealed > 0)
                    {
                        Debug.Log(string.Format("[GameplayUI] Revealed {0} instance(s) of '{1}' in owner word row {2}",
                            revealed, letter, i + 1));
                    }
                }
            }
        }

        /// <summary>
        /// Update grid cells when opponent discovers a letter - mark matching cells as guessed/hit
        /// </summary>
        private void UpdateOwnerGridCellsForLetter(char letter)
        {
            foreach (var word in _playerSetupData.PlacedWords)
            {
                int col = word.StartCol;
                int row = word.StartRow;

                for (int i = 0; i < word.Word.Length; i++)
                {
                    if (char.ToUpper(word.Word[i]) == letter)
                    {
                        var cell = _ownerPanel.GetCell(col, row);
                        if (cell != null && !cell.HasBeenGuessed)
                        {
                            cell.MarkAsGuessed(true);
                        }
                    }
                    col += word.DirCol;
                    row += word.DirRow;
                }
            }
        }

        /// <summary>
        /// Upgrade any "hit but letter unknown" (yellow) cells to "hit with known letter" (green)
        /// Called when opponent discovers a letter AFTER having guessed coordinates
        /// </summary>
        private void UpgradeOwnerGridCellsForLetter(char letter)
        {
            letter = char.ToUpper(letter);

            foreach (var word in _playerSetupData.PlacedWords)
            {
                int col = word.StartCol;
                int row = word.StartRow;

                for (int i = 0; i < word.Word.Length; i++)
                {
                    if (char.ToUpper(word.Word[i]) == letter)
                    {
                        var cell = _ownerPanel.GetCell(col, row);
                        // Only upgrade cells that are yellow (hit but letter unknown)
                        if (cell != null && cell.IsHitButLetterUnknown)
                        {
                            cell.UpgradeToKnownHit();
                            Debug.Log(string.Format("[GameplayUI] Upgraded cell ({0},{1}) from yellow to green for letter '{2}'", col, row, letter));
                        }
                    }
                    col += word.DirCol;
                    row += word.DirRow;
                }
            }
        }

        /// <summary>
        /// Upgrade any "hit but letter unknown" (yellow) cells to "hit with known letter" (green)
        /// Called when player discovers a letter AFTER having guessed coordinates
        /// </summary>
        private void UpgradeOpponentGridCellsForLetter(char letter)
        {
            letter = char.ToUpper(letter);

            foreach (var word in _opponentSetupData.PlacedWords)
            {
                int col = word.StartCol;
                int row = word.StartRow;

                for (int i = 0; i < word.Word.Length; i++)
                {
                    if (char.ToUpper(word.Word[i]) == letter)
                    {
                        var cell = _opponentPanel.GetCell(col, row);
                        // Only upgrade cells that are yellow (hit but letter unknown)
                        if (cell != null && cell.IsHitButLetterUnknown)
                        {
                            cell.UpgradeToKnownHit();
                            cell.RevealHiddenLetter();
                            Debug.Log(string.Format("[GameplayUI] Upgraded opponent cell ({0},{1}) from yellow to green for letter '{2}'", col, row, letter));
                        }
                    }
                    col += word.DirCol;
                    row += word.DirRow;
                }
            }
        }

        /// <summary>
        /// Mark all cells of a word as guessed/hit on owner panel
        /// </summary>
        private void RevealWordOnOwnerPanel(WordPlacementData wordData)
        {
            int col = wordData.StartCol;
            int row = wordData.StartRow;

            for (int i = 0; i < wordData.Word.Length; i++)
            {
                var cell = _ownerPanel.GetCell(col, row);
                if (cell != null)
                {
                    cell.MarkAsGuessed(true);
                }
                col += wordData.DirCol;
                row += wordData.DirRow;
            }
        }

        #endregion
    }
}
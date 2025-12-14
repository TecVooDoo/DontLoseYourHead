using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using TecVooDoo.DontLoseYourHead.Core;
using System.Linq;
using UnityEngine.InputSystem;
using Cysharp.Threading.Tasks;
using DLYH.AI.Core;
using DLYH.AI.Config;
using DLYH.AI.Strategies;

namespace TecVooDoo.DontLoseYourHead.UI
{
    /// <summary>
    /// Controls the Gameplay UI phase, managing two PlayerGridPanel instances
    /// (owner and opponent) and handling the transition from Setup to Gameplay.
    /// Now integrated with ExecutionerAI for single-player vs AI gameplay.
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

        [Header("Center Panel Name Labels")]
        [SerializeField] private TextMeshProUGUI _player1NameLabel;
        [SerializeField] private TextMeshProUGUI _player2NameLabel;
        [SerializeField] private Image _player1ColorIndicator;
        [SerializeField] private Image _player2ColorIndicator;

        [Header("Start Button (for event subscription)")]
        [SerializeField] private Button _startGameButton;

        [Header("Autocomplete (Setup Mode Only)")]
        [SerializeField] private AutocompleteDropdown _autocompleteDropdown;


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

        [Header("AI Configuration")]
        [SerializeField] private ExecutionerConfigSO _aiConfig;

        [Header("Turn Indicator (Optional)")]
        [SerializeField] private TextMeshProUGUI _turnIndicatorText;


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

        // AI System
        private ExecutionerAI _executionerAI;
        private bool _aiInitialized = false;

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
        private HashSet<int> _opponentSolvedWordRows = new HashSet<int>();

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

        #region Editor Testing

        [FoldoutGroup("Editor Testing")]
        [SerializeField]
        [GUIColor(0.8f, 1f, 0.8f)]
        private string _testOpponentLetter = "E";

        [FoldoutGroup("Editor Testing")]
        [SerializeField]
        [GUIColor(0.8f, 1f, 0.8f)]
        private int _testOpponentCol = 0;

        [FoldoutGroup("Editor Testing")]
        [SerializeField]
        [GUIColor(0.8f, 1f, 0.8f)]
        private int _testOpponentRow = 0;

        [FoldoutGroup("Editor Testing")]
        [SerializeField]
        [GUIColor(0.8f, 1f, 0.8f)]
        private string _testOpponentWord = "RAW";

        [FoldoutGroup("Editor Testing")]
        [Button("Switch to Opponent Turn")]
        [GUIColor(0.8f, 0.6f, 1f)]
        private void TestSwitchToOpponentTurn()
        {
            if (_isPlayerTurn)
            {
                _isPlayerTurn = false;
                Debug.Log("[GameplayUI] Switched to Opponent's turn (manual)");
            }
            else
            {
                Debug.Log("[GameplayUI] Already Opponent's turn!");
            }
        }

        [FoldoutGroup("Editor Testing")]
        [Button("Switch to Player Turn")]
        [GUIColor(0.8f, 0.6f, 1f)]
        private void TestSwitchToPlayerTurn()
        {
            if (!_isPlayerTurn)
            {
                _isPlayerTurn = true;
                Debug.Log("[GameplayUI] Switched to Player's turn (manual)");
            }
            else
            {
                Debug.Log("[GameplayUI] Already Player's turn!");
            }
        }

        [FoldoutGroup("Editor Testing")]
        [Button("Simulate Opponent Letter Guess")]
        [GUIColor(0.8f, 0.4f, 0.2f)]
        private void TestSimulateOpponentLetter()
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

            char letter = _testOpponentLetter.Length > 0 ? char.ToUpper(_testOpponentLetter[0]) : 'E';
            GuessResult result = ProcessOpponentLetterGuess(letter);

            if (result == GuessResult.AlreadyGuessed)
            {
                Debug.Log(string.Format("[GameplayUI] Opponent already guessed letter '{0}' - try a different letter!", letter));
                return;
            }

            Debug.Log(string.Format("[GameplayUI] Opponent guessed letter '{0}': {1}", letter, result == GuessResult.Hit ? "HIT" : "MISS"));
            EndOpponentTurn();
        }

        [FoldoutGroup("Editor Testing")]
        [Button("Simulate Opponent Coordinate Guess")]
        [GUIColor(0.8f, 0.4f, 0.2f)]
        private void TestSimulateOpponentCoordinate()
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

            string colLabel = ((char)('A' + _testOpponentCol)).ToString();
            string coordLabel = colLabel + (_testOpponentRow + 1);

            GuessResult result = ProcessOpponentCoordinateGuess(_testOpponentCol, _testOpponentRow);

            if (result == GuessResult.AlreadyGuessed)
            {
                Debug.Log(string.Format("[GameplayUI] Opponent already guessed coordinate {0} - try a different cell!", coordLabel));
                return;
            }

            Debug.Log(string.Format("[GameplayUI] Opponent guessed coordinate {0}: {1}", coordLabel, result == GuessResult.Hit ? "HIT" : "MISS"));
            EndOpponentTurn();
        }

        [FoldoutGroup("Editor Testing")]
        [Button("Simulate Opponent Word Guess")]
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

        [Button("Trigger AI Turn (Debug)")]
        [GUIColor(1f, 0.8f, 0.4f)]
        private void TestTriggerAITurn()
        {
            if (_executionerAI == null)
            {
                Debug.LogWarning("[GameplayUI] AI not initialized!");
                return;
            }

            if (_isPlayerTurn)
            {
                Debug.LogWarning("[GameplayUI] It's player's turn! Switch to opponent turn first.");
                return;
            }

            TriggerAITurn();
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
            UnsubscribeFromAIEvents();
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
            InitializeAI();

            // Hide autocomplete dropdown (it's a sibling of SetupContainer, not a child)
            if (_autocompleteDropdown != null)
            {
                _autocompleteDropdown.Hide();
            }


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
            UpdateCenterPanelNames();
            UpdateTurnIndicator();

            // Game starts with player's turn
            _isPlayerTurn = true;

        }

        /// <summary>
        /// Returns to setup mode
        /// </summary>
        public void ReturnToSetup()
        {

            _wordGuessModeController?.ExitWordGuessMode();
            UnsubscribeFromPanelEvents();
            UnsubscribeFromWordGuessModeController();
            UnsubscribeFromAIEvents();

            if (_ownerPanel != null)
                _ownerPanel.gameObject.SetActive(false);

            if (_opponentPanel != null)
                _opponentPanel.gameObject.SetActive(false);

            if (_gameplayContainer != null)
                _gameplayContainer.SetActive(false);

            if (_setupContainer != null)
                _setupContainer.SetActive(true);

            _playerSetupData = null;
            _opponentSetupData = null;
            _playerGuessProcessor = null;
            _opponentGuessProcessor = null;
            _executionerAI = null;
            _aiInitialized = false;
            _gameOver = false;
        }

        #endregion

        #region Data Capture

        private void CaptureSetupData()
        {
            if (_setupSettingsPanel == null)
            {
                Debug.LogError("[GameplayUI] SetupSettingsPanel reference missing!");
                return;
            }

            if (_setupGridPanel == null)
            {
                Debug.LogError("[GameplayUI] Setup Grid Panel reference missing!");
                return;
            }

            // Get settings using tuple methods
            (string playerName, Color playerColor) = _setupSettingsPanel.GetPlayerSettings();
            (int gridSize, WordCountOption wordCount, DifficultySetting difficulty) = _setupSettingsPanel.GetDifficultySettings();

            _playerSetupData = new SetupData
            {
                PlayerName = playerName,
                PlayerColor = playerColor,
                GridSize = gridSize,
                WordCount = (int)wordCount,
                DifficultyLevel = difficulty,
                WordLengths = DifficultyCalculator.GetWordLengths(wordCount)
            };

            WordPatternRow[] wordRows = _setupGridPanel.GetWordPatternRows();
            if (wordRows != null)
            {
                int rowIndex = 0;
                foreach (WordPatternRow row in wordRows)
                {
                    if (row != null && row.gameObject.activeSelf && row.HasWord && row.IsPlaced)
                    {
                        // Construct WordPlacementData from row properties
                        WordPlacementData wordData = new WordPlacementData
                        {
                            Word = row.CurrentWord,
                            StartCol = row.PlacedStartCol,
                            StartRow = row.PlacedStartRow,
                            DirCol = row.PlacedDirCol,
                            DirRow = row.PlacedDirRow,
                            RowIndex = rowIndex
                        };
                        _playerSetupData.PlacedWords.Add(wordData);
                    }
                    rowIndex++;
                }
            }

        }

        /// <summary>
        /// Generate opponent data using AISetupManager for intelligent word selection and placement.
        /// </summary>
        private void GenerateOpponentData()
        {
            // Match opponent settings to player settings (symmetric setup)
            int gridSize = _playerSetupData.GridSize;
            int wordCount = _playerSetupData.WordCount;
            int[] wordLengths = _playerSetupData.WordLengths;

            // Create AI setup manager
            AISetupManager setupManager = new AISetupManager(gridSize, wordCount, wordLengths);

            // Build word list dictionary
            Dictionary<int, WordListSO> wordLists = new Dictionary<int, WordListSO>();
            if (_threeLetterWords != null) wordLists[3] = _threeLetterWords;
            if (_fourLetterWords != null) wordLists[4] = _fourLetterWords;
            if (_fiveLetterWords != null) wordLists[5] = _fiveLetterWords;
            if (_sixLetterWords != null) wordLists[6] = _sixLetterWords;

            // Perform AI setup (select and place words)
            bool setupSuccess = setupManager.PerformSetup(wordLists);

            if (!setupSuccess)
            {
                Debug.LogError("[GameplayUI] AI setup failed! Using fallback.");
                GenerateOpponentDataFallback();
                return;
            }

            // Create opponent setup data from AI results
            _opponentSetupData = new SetupData
            {
                PlayerName = "Executioner",
                PlayerColor = new Color(0.6f, 0.2f, 0.2f, 1f), // Dark red
                GridSize = gridSize,
                WordCount = wordCount,
                DifficultyLevel = _playerSetupData.DifficultyLevel,
                WordLengths = wordLengths,
                PlacedWords = setupManager.Placements
            };

            Debug.Log(setupManager.GetDebugSummary());
        }

        /// <summary>
        /// Fallback opponent data generation if AI setup fails.
        /// </summary>
        private void GenerateOpponentDataFallback()
        {
            _opponentSetupData = new SetupData
            {
                PlayerName = "Executioner",
                PlayerColor = new Color(0.6f, 0.2f, 0.2f, 1f),
                GridSize = _playerSetupData.GridSize,
                WordCount = _playerSetupData.WordCount,
                DifficultyLevel = _playerSetupData.DifficultyLevel,
                WordLengths = _playerSetupData.WordLengths
            };

            // Simple fallback words
            string[] fallbackWords = { "CAT", "ROAD", "SNORE", "BRIDGE" };
            int wordCount = Mathf.Min(_playerSetupData.WordCount, fallbackWords.Length);

            for (int i = 0; i < wordCount; i++)
            {
                WordPlacementData fallbackWord = new WordPlacementData
                {
                    Word = fallbackWords[i],
                    StartCol = i,
                    StartRow = i * 2,
                    DirCol = 1,
                    DirRow = 0,
                    RowIndex = i
                };
                _opponentSetupData.PlacedWords.Add(fallbackWord);
            }

            Debug.LogWarning("[GameplayUI] Using fallback opponent data!");
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
                    row.ResetRevealedLetters();
                    // Note: SetGameplayWord already configures row for opponent display
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
                        // Note: SetGameplayWord already configures row for opponent display
                    }
                }
            }

            SubscribeToPanelEvents();
        }

        private void PlaceWordOnPanelRevealed(PlayerGridPanel panel, WordPlacementData wordData)
        {
            for (int i = 0; i < wordData.Word.Length; i++)
            {
                int col = wordData.StartCol + (i * wordData.DirCol);
                int rowIdx = wordData.StartRow + (i * wordData.DirRow);
                char letter = wordData.Word[i];

                GridCellUI cell = panel.GetCell(col, rowIdx);
                if (cell != null)
                {
                    cell.SetLetter(letter);
                    cell.SetState(CellState.Filled);
                }
            }
        }

        private void PlaceWordOnPanelHidden(PlayerGridPanel panel, WordPlacementData wordData)
        {
            for (int i = 0; i < wordData.Word.Length; i++)
            {
                int col = wordData.StartCol + (i * wordData.DirCol);
                int rowIdx = wordData.StartRow + (i * wordData.DirRow);
                char letter = wordData.Word[i];

                GridCellUI cell = panel.GetCell(col, rowIdx);
                if (cell != null)
                {
                    // For hidden letters (opponent grid), use SetHiddenLetter
                    cell.SetHiddenLetter(letter);
                }
            }
        }

        #endregion

        #region Panel Events

        private void SubscribeToPanelEvents()
        {
            if (_opponentPanel == null) return;

            _opponentPanel.OnLetterClicked += HandleLetterGuess;
            _opponentPanel.OnCellClicked += HandleCellGuess;
        }

        private void UnsubscribeFromPanelEvents()
        {
            if (_opponentPanel == null) return;

            _opponentPanel.OnLetterClicked -= HandleLetterGuess;
            _opponentPanel.OnCellClicked -= HandleCellGuess;
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

            // Record for AI rubber-banding
            if (_executionerAI != null)
            {
                _executionerAI.RecordPlayerGuess(result == GuessResult.Hit);
            }

            // Check if any words are now fully revealed via letters
            if (result == GuessResult.Hit)
            {
                CheckAndMarkFullyRevealedWords();
            }

            // Check win condition
            CheckPlayerWinCondition();

            if (!_gameOver)
            {
                EndPlayerTurn();
            }
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

            // Record for AI rubber-banding
            if (_executionerAI != null)
            {
                _executionerAI.RecordPlayerGuess(result == GuessResult.Hit);
            }

            // Check win condition after coordinate guess
            CheckPlayerWinCondition();

            if (!_gameOver)
            {
                EndPlayerTurn();
            }
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
            _wordGuessModeController.OnTurnEnded += HandleWordGuessTurnEnded;

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

        /// <summary>
        /// Handler for word guess turn ended - records result for AI then ends turn.
        /// </summary>
        private void HandleWordGuessTurnEnded()
        {
            // Word guess results are tracked in ProcessWordGuessForController
            // Check win condition after word guess
            CheckPlayerWinCondition();

            if (!_gameOver)
            {
                EndPlayerTurn();
            }
        }

        private void UnsubscribeFromWordGuessModeController()
        {
            if (_wordGuessModeController == null) return;

            _wordGuessModeController.OnFeedbackRequested -= ShowFeedback;
            _wordGuessModeController.OnTurnEnded -= HandleWordGuessTurnEnded;

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

            // Record for AI rubber-banding
            if (_executionerAI != null && result != GuessResult.AlreadyGuessed && result != GuessResult.InvalidWord)
            {
                _executionerAI.RecordPlayerGuess(result == GuessResult.Hit);
            }

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
            UpdateTurnIndicator();

            // Trigger AI turn
            if (_aiInitialized && _executionerAI != null)
            {
                TriggerAITurn();
            }
        }

        private void EndOpponentTurn()
        {
            if (_gameOver) return;

            _isPlayerTurn = true;
            UpdateTurnIndicator();

            // Advance AI memory turn counter
            if (_executionerAI != null)
            {
                _executionerAI.AdvanceTurn();
            }
        }

        private void UpdateTurnIndicator()
        {
            if (_turnIndicatorText != null)
            {
                if (_gameOver)
                {
                    _turnIndicatorText.text = "GAME OVER";
                }
                else if (_isPlayerTurn)
                {
                    _turnIndicatorText.text = "Your Turn";
                }
                else
                {
                    _turnIndicatorText.text = "Executioner's Turn...";
                }
            }
        }

        public bool IsPlayerTurn => _isPlayerTurn;
        public bool IsGameOver => _gameOver;

        #endregion

        #region AI System

        private void InitializeAI()
        {
            if (_aiConfig == null)
            {
                Debug.LogWarning("[GameplayUI] No AI config assigned! AI will not function.");
                return;
            }

            // Create ExecutionerAI as a component (it's a MonoBehaviour)
            _executionerAI = gameObject.AddComponent<ExecutionerAI>();

            // Set the config via reflection since _config is private
            System.Reflection.FieldInfo configField = typeof(ExecutionerAI).GetField("_config",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (configField != null)
            {
                configField.SetValue(_executionerAI, _aiConfig);
            }
            else
            {
                Debug.LogError("[GameplayUI] Could not set AI config via reflection!");
                return;
            }

            // Initialize AI with player difficulty
            _executionerAI.Initialize(_playerSetupData.DifficultyLevel);

            // Subscribe to AI events
            _executionerAI.OnLetterGuess += HandleAILetterGuess;
            _executionerAI.OnCoordinateGuess += HandleAICoordinateGuess;
            _executionerAI.OnWordGuess += HandleAIWordGuess;
            _executionerAI.OnThinkingStarted += HandleAIThinkingStarted;
            _executionerAI.OnThinkingComplete += HandleAIThinkingComplete;

            _aiInitialized = true;
            Debug.Log("[GameplayUI] AI initialized successfully!");
        }

        private void UnsubscribeFromAIEvents()
        {
            if (_executionerAI == null) return;

            _executionerAI.OnLetterGuess -= HandleAILetterGuess;
            _executionerAI.OnCoordinateGuess -= HandleAICoordinateGuess;
            _executionerAI.OnWordGuess -= HandleAIWordGuess;
            _executionerAI.OnThinkingStarted -= HandleAIThinkingStarted;
            _executionerAI.OnThinkingComplete -= HandleAIThinkingComplete;
        }

        /// <summary>
        /// Build AIGameState from current game state for AI decision making.
        /// </summary>
        private AIGameState BuildAIGameState()
        {
            AIGameState state = new AIGameState();

            state.GridSize = _playerSetupData.GridSize;
            state.WordCount = _playerSetupData.WordCount;

            // Copy guessed letters (all letters AI has tried)
            state.GuessedLetters = new HashSet<char>(_opponentGuessedLetters);

            // Copy hit letters (letters that exist in player's words)
            state.HitLetters = new HashSet<char>(_opponentKnownLetters);

            // Get skill level from AI (if available)
            state.SkillLevel = _executionerAI != null ? _executionerAI.CurrentSkill : 0.5f;

            // Calculate fill ratio (approximate letters / total cells)
            float avgWordLength = 4.5f; // Average of 3-6 letter words
            state.FillRatio = (_playerSetupData.WordCount * avgWordLength) / (_playerSetupData.GridSize * _playerSetupData.GridSize);

            // Copy ALL guessed coordinates (not just hits) so AI knows what NOT to guess
            state.GuessedCoordinates = new HashSet<(int, int)>();
            foreach (Vector2Int coord in _opponentGuessedCoordinates)
            {
                state.GuessedCoordinates.Add((coord.y, coord.x)); // Note: (row, col)
            }

            // Convert coordinates from Vector2Int to tuples - only track hits
            state.HitCoordinates = new HashSet<(int, int)>();

            foreach (Vector2Int coord in _opponentGuessedCoordinates)
            {
                // Check if this was a hit by checking if a letter exists there
                bool wasHit = false;
                foreach (WordPlacementData word in _playerSetupData.PlacedWords)
                {
                    for (int i = 0; i < word.Word.Length; i++)
                    {
                        int col = word.StartCol + (i * word.DirCol);
                        int row = word.StartRow + (i * word.DirRow);
                        if (col == coord.x && row == coord.y)
                        {
                            wasHit = true;
                            break;
                        }
                    }
                    if (wasHit) break;
                }

                if (wasHit)
                {
                    state.HitCoordinates.Add((coord.y, coord.x)); // Note: (row, col)
                }
            }

            // Build word patterns from player's words
            state.WordPatterns = new List<string>();
            foreach (WordPlacementData word in _playerSetupData.PlacedWords)
            {
                System.Text.StringBuilder pattern = new System.Text.StringBuilder();
                for (int i = 0; i < word.Word.Length; i++)
                {
                    char letter = word.Word[i];
                    if (_opponentKnownLetters.Contains(letter))
                    {
                        pattern.Append(letter);
                    }
                    else
                    {
                        pattern.Append('_');
                    }
                }
                state.WordPatterns.Add(pattern.ToString());
            }

            // Populate word bank for AI word guessing - only include words of relevant lengths
            state.WordBank = new HashSet<string>();
            HashSet<int> neededLengths = new HashSet<int>();
            foreach (WordPlacementData word in _playerSetupData.PlacedWords)
            {
                neededLengths.Add(word.Word.Length);
            }

            foreach (int length in neededLengths)
            {
                WordListSO wordList = GetWordListForLength(length);
                if (wordList != null && wordList.Words != null)
                {
                    foreach (string word in wordList.Words)
                    {
                        state.WordBank.Add(word.ToUpper());
                    }
                }
            }

            // Initialize WordsSolved list (all false initially, updated as words are solved)
            state.WordsSolved = new List<bool>();
            foreach (WordPlacementData word in _playerSetupData.PlacedWords)
            {
                state.WordsSolved.Add(_opponentSolvedWordRows.Contains(word.RowIndex));
            }

            return state;
        }

        /// <summary>
        /// Trigger the AI to take its turn.
        /// </summary>
        private void TriggerAITurn()
        {
            if (_gameOver || _isPlayerTurn || _executionerAI == null)
            {
                return;
            }

            AIGameState gameState = BuildAIGameState();
            // ExecuteTurnAsync returns UniTaskVoid which is fire-and-forget
            _executionerAI.ExecuteTurnAsync(gameState).Forget();
        }

        private void HandleAIThinkingStarted()
        {
            Debug.Log("[GameplayUI] AI is thinking...");
            // Could show a visual indicator here
        }

        private void HandleAIThinkingComplete()
        {
            Debug.Log("[GameplayUI] AI finished thinking.");
        }

        private void HandleAILetterGuess(char letter)
        {
            if (_gameOver) return;

            Debug.Log(string.Format("[GameplayUI] AI guesses letter: {0}", letter));

            GuessResult result = ProcessOpponentLetterGuess(letter);

            if (result == GuessResult.Hit)
            {
                _opponentKnownLetters.Add(letter);
                _executionerAI?.RecordRevealedLetter(letter);
            }

            if (result != GuessResult.AlreadyGuessed)
            {
                _opponentGuessedLetters.Add(letter);
            }

            // Check opponent win condition
            CheckOpponentWinCondition();

            if (!_gameOver)
            {
                EndOpponentTurn();
            }
        }

        private void HandleAICoordinateGuess(int row, int col)
        {
            if (_gameOver) return;

            string colLabel = ((char)('A' + col)).ToString();
            string coordLabel = colLabel + (row + 1);
            Debug.Log(string.Format("[GameplayUI] AI guesses coordinate: {0}", coordLabel));

            GuessResult result = ProcessOpponentCoordinateGuess(col, row);

            Vector2Int coord = new Vector2Int(col, row);
            if (result != GuessResult.AlreadyGuessed)
            {
                _opponentGuessedCoordinates.Add(coord);
            }

            if (result == GuessResult.Hit)
            {
                _executionerAI?.RecordAIHit(row, col);
            }

            // Check opponent win condition
            CheckOpponentWinCondition();

            if (!_gameOver)
            {
                EndOpponentTurn();
            }
        }

        private void HandleAIWordGuess(string word, int rowIndex)
        {
            if (_gameOver) return;

            Debug.Log(string.Format("[GameplayUI] AI guesses word: {0} (row {1})", word, rowIndex + 1));

            GuessResult result = ProcessOpponentWordGuess(word, rowIndex);

            if (result == GuessResult.Hit)
            {
                _opponentSolvedWordRows.Add(rowIndex);
                // Record all letters in the word
                foreach (char letter in word.ToUpper())
                {
                    _opponentKnownLetters.Add(letter);
                    _executionerAI?.RecordRevealedLetter(letter);
                }
            }

            if (result != GuessResult.AlreadyGuessed && result != GuessResult.InvalidWord)
            {
                _opponentGuessedWords.Add(word.ToUpper());
            }

            // Check opponent win condition
            CheckOpponentWinCondition();

            if (!_gameOver)
            {
                EndOpponentTurn();
            }
        }

        #endregion

        #region Win Condition Checking

        /// <summary>
        /// Check if any opponent word rows have all letters revealed via letter guessing.
        /// If so, mark them as solved to hide the Guess Word button.
        /// </summary>
        private void CheckAndMarkFullyRevealedWords()
        {
            if (_opponentPanel == null || _opponentSetupData == null) return;

            WordPatternRow[] rows = _opponentPanel.GetWordPatternRows();

            for (int i = 0; i < _opponentSetupData.PlacedWords.Count && i < rows.Length; i++)
            {
                WordPlacementData wordData = _opponentSetupData.PlacedWords[i];
                WordPatternRow row = rows[i];

                // Skip if already solved
                if (_playerSolvedWordRows.Contains(i)) continue;

                // Check if all letters in this word are known
                bool allLettersKnown = true;
                foreach (char letter in wordData.Word)
                {
                    if (!_playerKnownLetters.Contains(char.ToUpper(letter)))
                    {
                        allLettersKnown = false;
                        break;
                    }
                }

                if (allLettersKnown)
                {
                    Debug.Log(string.Format("[GameplayUI] Word row {0} fully revealed via letters: {1}", i + 1, wordData.Word));
                    row.MarkWordSolved();
                    _playerSolvedWordRows.Add(i);
                }
            }
        }

        /// <summary>
        /// Check if player has won by revealing all letters AND all grid positions for all opponent words.
        /// </summary>
        private void CheckPlayerWinCondition()
        {
            if (_gameOver || _opponentSetupData == null) return;

            bool allLettersRevealed = true;
            bool allPositionsRevealed = true;

            foreach (WordPlacementData wordData in _opponentSetupData.PlacedWords)
            {
                // Check all letters in this word are known
                foreach (char letter in wordData.Word)
                {
                    if (!_playerKnownLetters.Contains(char.ToUpper(letter)))
                    {
                        allLettersRevealed = false;
                        break;
                    }
                }

                if (!allLettersRevealed) break;

                // Check all positions for this word are guessed
                for (int i = 0; i < wordData.Word.Length; i++)
                {
                    int col = wordData.StartCol + (i * wordData.DirCol);
                    int row = wordData.StartRow + (i * wordData.DirRow);
                    Vector2Int coord = new Vector2Int(col, row);

                    if (!_playerGuessedCoordinates.Contains(coord))
                    {
                        allPositionsRevealed = false;
                        break;
                    }
                }

                if (!allPositionsRevealed) break;
            }

            if (allLettersRevealed && allPositionsRevealed)
            {
                Debug.Log("[GameplayUI] === PLAYER WINS! All words and positions revealed! ===");
                _gameOver = true;
                UpdateTurnIndicator();
            }
        }

        /// <summary>
        /// Check if opponent (AI) has won by revealing all letters AND all grid positions for all player words.
        /// </summary>
        private void CheckOpponentWinCondition()
        {
            if (_gameOver || _playerSetupData == null) return;

            bool allLettersRevealed = true;
            bool allPositionsRevealed = true;

            foreach (WordPlacementData wordData in _playerSetupData.PlacedWords)
            {
                // Check all letters in this word are known by opponent
                foreach (char letter in wordData.Word)
                {
                    if (!_opponentKnownLetters.Contains(char.ToUpper(letter)))
                    {
                        allLettersRevealed = false;
                        break;
                    }
                }

                if (!allLettersRevealed) break;

                // Check all positions for this word are guessed by opponent
                for (int i = 0; i < wordData.Word.Length; i++)
                {
                    int col = wordData.StartCol + (i * wordData.DirCol);
                    int row = wordData.StartRow + (i * wordData.DirRow);
                    Vector2Int coord = new Vector2Int(col, row);

                    if (!_opponentGuessedCoordinates.Contains(coord))
                    {
                        allPositionsRevealed = false;
                        break;
                    }
                }

                if (!allPositionsRevealed) break;
            }

            if (allLettersRevealed && allPositionsRevealed)
            {
                Debug.Log("[GameplayUI] === OPPONENT WINS! All words and positions revealed! ===");
                _gameOver = true;
                UpdateTurnIndicator();
            }
        }

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

        private void UpdateCenterPanelNames()
        {
            if (_player1NameLabel != null && _playerSetupData != null)
            {
                _player1NameLabel.text = _playerSetupData.PlayerName;
            }

            if (_player2NameLabel != null && _opponentSetupData != null)
            {
                _player2NameLabel.text = _opponentSetupData.PlayerName;
            }

            if (_player1ColorIndicator != null && _playerSetupData != null)
            {
                _player1ColorIndicator.color = _playerSetupData.PlayerColor;
            }

            if (_player2ColorIndicator != null && _opponentSetupData != null)
            {
                _player2ColorIndicator.color = _opponentSetupData.PlayerColor;
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
                UpdateTurnIndicator();
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
                UpdateTurnIndicator();
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

            int missLimit = baseMisses + gridBonus + wordModifier + difficultyModifier;

            return missLimit;
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

        #region Player State Initialization

        private void InitializePlayerState()
        {
            _playerMisses = 0;
            _playerKnownLetters.Clear();
            _playerGuessedLetters.Clear();
            _playerGuessedCoordinates.Clear();
            _playerGuessedWords.Clear();
            _playerSolvedWordRows.Clear();
            _playerMissLimit = CalculateMissLimit(_playerSetupData.DifficultyLevel, _opponentSetupData);
            _isPlayerTurn = true;
            _gameOver = false;

        }

        #endregion

        #region Guess Processors

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
                (amount) => { _playerMisses += amount; UpdatePlayerMissCounter(); },
                (letter, state) => _opponentPanel.SetLetterState(letter, state),
                word => IsValidWord(word),
                (word, correct) => _player1GuessedWordList?.AddGuessedWord(word, correct)
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
                (amount) => { _opponentMisses += amount; UpdateOpponentMissCounter(); },
                (letter, state) => _ownerPanel.SetLetterState(letter, state),
                word => IsValidWord(word),
                (word, correct) => _player2GuessedWordList?.AddGuessedWord(word, correct)
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

            if (result == GuessProcessor.GuessResult.Hit)
            {
                _playerKnownLetters.Add(letter);
            }
            _playerGuessedLetters.Add(letter);

            return ConvertGuessResult(result);
        }

        /// <summary>
        /// Process player guessing a coordinate on opponent's grid
        /// </summary>
        private GuessResult ProcessPlayerCoordinateGuess(int col, int row)
        {
            GuessProcessor.GuessResult result = _playerGuessProcessor.ProcessCoordinateGuess(col, row);

            _playerGuessedCoordinates.Add(new Vector2Int(col, row));

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
            _opponentSolvedWordRows.Clear();
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
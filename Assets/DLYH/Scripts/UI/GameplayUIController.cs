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
using DLYH.Telemetry;
using DLYH.Networking;
// Type alias for cleaner code after extraction
using GuessResult = TecVooDoo.DontLoseYourHead.UI.GuessProcessingManager.GuessResult;

namespace TecVooDoo.DontLoseYourHead.UI
{
    /// <summary>
    /// Controls the Gameplay UI phase, managing two PlayerGridPanel instances
    /// (owner and opponent) and handling the transition from Setup to Gameplay.
    /// Supports both AI opponents (The Executioner) and remote network players via IOpponent interface.
    /// </summary>
    public class GameplayUIController : MonoBehaviour
    {
        #region Serialized Fields

        [Header("Container References")]
        [SerializeField] private GameObject _setupContainer;
        [SerializeField] private GameObject _gameplayContainer;
        [SerializeField] private GameObject _helpOverlayPanel;

        [Header("Pre-Placed Panel References")]
        [SerializeField] private PlayerGridPanel _ownerPanel;
        [SerializeField] private PlayerGridPanel _opponentPanel;

        [Header("Miss Counter References")]
        [SerializeField] private TextMeshProUGUI _player1MissCounter;
        [SerializeField] private TextMeshProUGUI _player2MissCounter;
        [SerializeField] private Image _player1MissLabelBackground;
        [SerializeField] private Image _player2MissLabelBackground;

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
        [SerializeField] private Image _player1GuessedWordsLabelBackground;
        [SerializeField] private Image _player2GuessedWordsLabelBackground;

        [Header("AI Configuration")]
        [SerializeField] private ExecutionerConfigSO _aiConfig;

        [Header("Turn Indicator (Optional)")]
        [SerializeField] private TextMeshProUGUI _turnIndicatorText;

        [Header("Main Menu Access")]
        [SerializeField] private Button _mainMenuButton;

        [Header("Guillotine Displays")]
        [SerializeField] private GuillotineDisplay _player1Guillotine;
        [SerializeField] private GuillotineDisplay _player2Guillotine;

        #endregion

        #region Private Fields

        // Captured setup data
        private SetupData _playerSetupData;
        private SetupData _opponentSetupData;

        // Reference to setup panel for data capture
        private SetupSettingsPanel _setupSettingsPanel;
        private PlayerGridPanel _setupGridPanel;

        // Guess processing manager (handles both player and opponent guesses)
        private GuessProcessingManager _guessProcessingManager;

        // Word guess mode controller
        private WordGuessModeController _wordGuessModeController;

        // State tracking and win condition checking (extracted services)
        private GameplayStateTracker _stateTracker;
        private WinConditionChecker _winChecker;

        // Extracted controllers
        private GameplayPanelConfigurator _panelConfigurator;
        private GameplayUIUpdater _uiUpdater;
        private PopupMessageController _popupController;
        private OpponentTurnManager _opponentTurnManager;

        // Telemetry tracking
        private int _totalTurns = 0;

        // Extra turn queue - stores words that grant extra turns (FIFO)
        private Queue<string> _extraTurnQueue = new Queue<string>();

        // AI opponent settings are now dynamic based on player difficulty
        // Easy player = smaller AI grid (6-8), more words (4) = easier for player to find
        // Normal player = medium AI grid (8-10), random words (3-4) = balanced
        // Hard player = larger AI grid (10-12), fewer words (3) = harder for player to find

        #endregion

        #region Events

        /// <summary>Fired when Main Menu is requested from Gameplay</summary>
        public event System.Action OnMainMenuRequested;

        /// <summary>Fired when gameplay starts (after setup complete)</summary>
        public event System.Action OnGameStarted;

        /// <summary>Fired when game ends. Parameter indicates if player won.</summary>
        public event System.Action<bool> OnGameEnded;

        #endregion

        #region State Accessors (delegate to GameplayStateTracker)

        // These properties delegate to _stateTracker for backwards compatibility
        private bool _isPlayerTurn
        {
            get => _stateTracker?.IsPlayerTurn ?? true;
            set { if (_stateTracker != null) _stateTracker.IsPlayerTurn = value; }
        }

        private bool _gameOver
        {
            get => _stateTracker?.GameOver ?? false;
            set { if (_stateTracker != null) _stateTracker.GameOver = value; }
        }

        private int _playerMisses => _stateTracker?.PlayerMisses ?? 0;
        private int _playerMissLimit => _stateTracker?.PlayerMissLimit ?? 0;
        private HashSet<char> _playerKnownLetters => _stateTracker?.PlayerKnownLetters ?? new HashSet<char>();
        private HashSet<char> _playerGuessedLetters => _stateTracker?.PlayerGuessedLetters ?? new HashSet<char>();
        private HashSet<Vector2Int> _playerGuessedCoordinates => _stateTracker?.PlayerGuessedCoordinates ?? new HashSet<Vector2Int>();
        private HashSet<int> _playerSolvedWordRows => _stateTracker?.PlayerSolvedWordRows ?? new HashSet<int>();

        // Stores the pending game result to fire OnGameEnded after Continue is clicked
        private bool? _pendingGameResult = null;

        private int _opponentMisses => _stateTracker?.OpponentMisses ?? 0;
        private int _opponentMissLimit => _stateTracker?.OpponentMissLimit ?? 0;
        private HashSet<char> _opponentKnownLetters => _stateTracker?.OpponentKnownLetters ?? new HashSet<char>();
        private HashSet<char> _opponentGuessedLetters => _stateTracker?.OpponentGuessedLetters ?? new HashSet<char>();
        private HashSet<Vector2Int> _opponentGuessedCoordinates => _stateTracker?.OpponentGuessedCoordinates ?? new HashSet<Vector2Int>();
        private HashSet<int> _opponentSolvedWordRows => _stateTracker?.OpponentSolvedWordRows ?? new HashSet<int>();

        #endregion

        // GuessResult enum moved to GuessProcessingManager
        // GameOverReason enum moved to PopupMessageController

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
            if (_opponentTurnManager == null || !_opponentTurnManager.IsOpponentInitialized)
            {
                Debug.LogWarning("[GameplayUI] Opponent not initialized!");
                return;
            }

            if (_isPlayerTurn)
            {
                Debug.LogWarning("[GameplayUI] It's player's turn! Switch to opponent turn first.");
                return;
            }

            _opponentTurnManager.TriggerOpponentTurn();
        }

        #endregion

#endif

        // SetupData class moved to GameplayPanelConfigurator.cs

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

            if (_mainMenuButton != null)
            {
                _mainMenuButton.onClick.AddListener(OnMainMenuButtonClicked);
            }

            if (_ownerPanel != null)
                _ownerPanel.gameObject.SetActive(false);
            if (_opponentPanel != null)
                _opponentPanel.gameObject.SetActive(false);

            if (_wordGuessFeedbackText != null)
                _wordGuessFeedbackText.gameObject.SetActive(false);

            // Initialize popup controller (handles MessagePopup subscription internally)
            _popupController = new PopupMessageController();
            _popupController.OnGameOverContinueClicked += HandleGameOverContinue;

            // Initialize panel configurator
            _panelConfigurator = new GameplayPanelConfigurator();
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
            if (_mainMenuButton != null)
            {
                _mainMenuButton.onClick.RemoveListener(OnMainMenuButtonClicked);
            }
            UnsubscribeFromPanelEvents();
            UnsubscribeFromWordGuessModeController();

            // Dispose extracted controllers
            if (_popupController != null)
            {
                _popupController.OnGameOverContinueClicked -= HandleGameOverContinue;
                _popupController.Dispose();
            }

            // Cleanup opponent manager
            _opponentTurnManager?.Dispose();
        }

        /// <summary>
        /// Called when the Main Menu button (gear icon) is clicked during gameplay.
        /// </summary>
        private void OnMainMenuButtonClicked()
        {
            Debug.Log("[GameplayUI] Main Menu button clicked");
            OnMainMenuRequested?.Invoke();
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

            // Reset previous game state before starting new game
            ResetGameplayState();

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

            if (_helpOverlayPanel != null)
                _helpOverlayPanel.SetActive(true);

            if (_ownerPanel != null)
            {
                _ownerPanel.gameObject.SetActive(true);
                _panelConfigurator.ConfigureOwnerPanel(_ownerPanel, _playerSetupData, _opponentSetupData.PlayerColor);
            }

            if (_opponentPanel != null)
            {
                _opponentPanel.gameObject.SetActive(true);
                _panelConfigurator.ConfigureOpponentPanel(_opponentPanel, _opponentSetupData, _playerSetupData.PlayerColor);
                SubscribeToPanelEvents();
            }

            InitializeWordGuessModeController();

            // Initialize UI updater with all references
            _uiUpdater = new GameplayUIUpdater(
                _player1MissCounter, _player2MissCounter,
                _player1MissLabelBackground, _player2MissLabelBackground,
                _player1NameLabel, _player2NameLabel,
                _player1ColorIndicator, _player2ColorIndicator,
                _player1GuessedWordsLabelBackground, _player2GuessedWordsLabelBackground,
                _turnIndicatorText,
                _player1Guillotine, _player2Guillotine);
            _uiUpdater.Initialize(_stateTracker, _playerSetupData, _opponentSetupData);

            // Initialize popup controller with setup data
            _popupController.Initialize(_playerSetupData, _opponentSetupData);

            // Game starts with player's turn
            _isPlayerTurn = true;
            _totalTurns = 0;

            // Send telemetry for game start
            PlaytestTelemetry.GameStart(
                _playerSetupData.PlayerName,
                _playerSetupData.GridSize,
                _playerSetupData.WordCount,
                _playerSetupData.DifficultyLevel.ToString(),
                _opponentSetupData.GridSize,
                _opponentSetupData.WordCount,
                _opponentSetupData.DifficultyLevel.ToString()
            );

            // Reset popup position for new game
            MessagePopup.ResetPosition();

            // Notify that game has started
            OnGameStarted?.Invoke();
            Debug.Log("[GameplayUI] Game started - OnGameStarted fired");

        }

        /// <summary>
        /// Returns to setup mode
        /// </summary>
        public void ReturnToSetup()
        {
            // Track if player abandoned a game in progress
            if (_stateTracker != null && !_stateTracker.GameOver)
            {
                PlaytestTelemetry.GameAbandon("gameplay", _totalTurns);
            }

            _wordGuessModeController?.ExitWordGuessMode();
            UnsubscribeFromPanelEvents();
            UnsubscribeFromWordGuessModeController();
            _opponentTurnManager?.Dispose();

            if (_ownerPanel != null)
                _ownerPanel.gameObject.SetActive(false);

            if (_opponentPanel != null)
                _opponentPanel.gameObject.SetActive(false);

            if (_gameplayContainer != null)
                _gameplayContainer.SetActive(false);

            if (_helpOverlayPanel != null)
                _helpOverlayPanel.SetActive(false);

            if (_setupContainer != null)
                _setupContainer.SetActive(true);

            // Reset guillotines via UI updater if available, otherwise directly
            _uiUpdater?.ResetGuillotines();
            if (_uiUpdater == null)
            {
                if (_player1Guillotine != null)
                    _player1Guillotine.Reset();
                if (_player2Guillotine != null)
                    _player2Guillotine.Reset();
            }

            _playerSetupData = null;
            _opponentSetupData = null;
            _guessProcessingManager?.Dispose();
            _guessProcessingManager = null;
            _stateTracker = null;
            _winChecker = null;
            _uiUpdater = null;
            _opponentTurnManager = null;
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
        /// Gets AI grid size and word count based on player's difficulty setting.
        /// Adds variety while scaling appropriately with difficulty.
        /// </summary>
        /// <param name="playerDifficulty">The difficulty level chosen by the player</param>
        /// <returns>Tuple of (gridSize, wordCount)</returns>
        private (int gridSize, int wordCount) GetAISettingsForPlayerDifficulty(DifficultySetting playerDifficulty)
        {
            int gridSize;
            int wordCount;

            switch (playerDifficulty)
            {
                case DifficultySetting.Easy:
                    // Player is on Easy = AI should be easier to beat
                    // Smaller grids (6-8), more words (4) = easier to find AI's words
                    int[] easyGrids = { 6, 7, 8 };
                    gridSize = easyGrids[UnityEngine.Random.Range(0, easyGrids.Length)];
                    wordCount = 4;
                    Debug.Log($"[GameplayUI] AI Settings (Player Easy): {gridSize}x{gridSize} grid, {wordCount} words");
                    break;

                case DifficultySetting.Normal:
                    // Player is on Normal = balanced challenge
                    // Medium grids (8-10), random word count (3-4)
                    int[] normalGrids = { 8, 9, 10 };
                    gridSize = normalGrids[UnityEngine.Random.Range(0, normalGrids.Length)];
                    wordCount = UnityEngine.Random.Range(0, 2) == 0 ? 3 : 4;
                    Debug.Log($"[GameplayUI] AI Settings (Player Normal): {gridSize}x{gridSize} grid, {wordCount} words");
                    break;

                case DifficultySetting.Hard:
                    // Player is on Hard = AI should be harder to beat
                    // Larger grids (10-12), fewer words (3) = harder to find AI's words
                    int[] hardGrids = { 10, 11, 12 };
                    gridSize = hardGrids[UnityEngine.Random.Range(0, hardGrids.Length)];
                    wordCount = 3;
                    Debug.Log($"[GameplayUI] AI Settings (Player Hard): {gridSize}x{gridSize} grid, {wordCount} words");
                    break;

                default:
                    // Fallback to medium settings
                    gridSize = 8;
                    wordCount = 3;
                    Debug.LogWarning($"[GameplayUI] Unknown difficulty {playerDifficulty}, using default 8x8/3 words");
                    break;
            }

            return (gridSize, wordCount);
        }

        /// <summary>
        /// Generate opponent data using AISetupManager for intelligent word selection and placement.
        /// AI settings vary based on player difficulty for variety and appropriate challenge.
        /// </summary>
        private void GenerateOpponentData()
        {
            // Get AI settings based on player's chosen difficulty
            var (gridSize, wordCount) = GetAISettingsForPlayerDifficulty(_playerSetupData.DifficultyLevel);

            int[] wordLengths = DifficultyCalculator.GetWordLengths(
                wordCount == 3 ? WordCountOption.Three : WordCountOption.Four);

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
            // Invert difficulty: Player Easy = Opponent Hard, Player Hard = Opponent Easy
            DifficultySetting opponentDifficulty = GetInverseDifficulty(_playerSetupData.DifficultyLevel);

            _opponentSetupData = new SetupData
            {
                PlayerName = "EXECUTIONER",
                PlayerColor = new Color(0.1f, 0.15f, 0.3f, 1f), // Dark blue - evokes executioner's hood
                GridSize = gridSize,
                WordCount = wordCount,
                DifficultyLevel = opponentDifficulty,
                WordLengths = wordLengths,
                PlacedWords = setupManager.Placements
            };

            Debug.Log(setupManager.GetDebugSummary());
            Debug.Log($"[GameplayUI] Player difficulty: {_playerSetupData.DifficultyLevel}, Opponent difficulty: {opponentDifficulty}");
        }

        /// <summary>
        /// Fallback opponent data generation if AI setup fails.
        /// Uses dynamic AI settings based on player difficulty.
        /// </summary>
        private void GenerateOpponentDataFallback()
        {
            // Invert difficulty for opponent
            DifficultySetting opponentDifficulty = GetInverseDifficulty(_playerSetupData.DifficultyLevel);

            // Get AI settings based on player's chosen difficulty
            var (gridSize, wordCount) = GetAISettingsForPlayerDifficulty(_playerSetupData.DifficultyLevel);

            int[] wordLengths = DifficultyCalculator.GetWordLengths(
                wordCount == 3 ? WordCountOption.Three : WordCountOption.Four);

            _opponentSetupData = new SetupData
            {
                PlayerName = "EXECUTIONER",
                PlayerColor = new Color(0.1f, 0.15f, 0.3f, 1f), // Dark blue - evokes executioner's hood
                GridSize = gridSize,
                WordCount = wordCount,
                DifficultyLevel = opponentDifficulty,
                WordLengths = wordLengths
            };

            // Simple fallback words
            string[] fallbackWords = { "CAT", "ROAD", "SNORE", "BRIDGE" };
            int fallbackWordCount = Mathf.Min(wordCount, fallbackWords.Length);

            for (int i = 0; i < fallbackWordCount; i++)
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

        /// <summary>
        /// Get the inverse difficulty setting (Easy <-> Hard, Normal stays Normal)
        /// </summary>
        private DifficultySetting GetInverseDifficulty(DifficultySetting playerDifficulty)
        {
            switch (playerDifficulty)
            {
                case DifficultySetting.Easy:
                    return DifficultySetting.Hard;
                case DifficultySetting.Hard:
                    return DifficultySetting.Easy;
                default:
                    return DifficultySetting.Normal;
            }
        }

        #endregion

        // Panel Configuration methods moved to GameplayPanelConfigurator.cs

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

            // Route letter input to word guess mode if active
            if (_wordGuessModeController != null && _wordGuessModeController.IsInKeyboardMode)
            {
                _wordGuessModeController.HandleKeyboardLetterInput(letter);
                return;
            }

            letter = char.ToUpper(letter);
            GuessResult result = ProcessPlayerLetterGuess(letter);

            if (result == GuessResult.AlreadyGuessed)
            {
                _popupController.ShowErrorPopup(string.Format("Letter '{0}' already guessed. Try again!", letter));
                return;
            }

            // Record for opponent rubber-banding (AI) or state sync (network)
            _opponentTurnManager?.RecordPlayerGuess(result == GuessResult.Hit);

            // Check if any words are now fully revealed via letters - queue them for extra turns
            if (result == GuessResult.Hit)
            {
                List<string> completedWords = CheckAndMarkFullyRevealedWords();
                foreach (string word in completedWords)
                {
                    _extraTurnQueue.Enqueue(word);
                }
            }

            // Check win condition BEFORE showing popup
            CheckPlayerWinCondition();

            // Only show turn popup if game isn't over (game over has its own popup)
            if (!_gameOver)
            {
                string playerName = _playerSetupData?.PlayerName ?? "Player";
                string resultText = result == GuessResult.Hit ? "Hit" : "Miss";

                // Show guess result and handle extra turn logic
                ShowGuessResultAndProcessTurn(
                    string.Format("{0} guessed letter '{1}' - {2}.", playerName, letter, resultText));
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
                _popupController.ShowErrorPopup(string.Format("Coordinate '{0}' already guessed. Try again!", coordLabel));
                return;
            }

            // Record for opponent rubber-banding (AI) or state sync (network)
            _opponentTurnManager?.RecordPlayerGuess(result == GuessResult.Hit);

            // Check if any words are now fully revealed via coordinate guessing - queue them for extra turns
            if (result == GuessResult.Hit)
            {
                List<string> completedWords = CheckAndMarkFullyRevealedWords();
                foreach (string word in completedWords)
                {
                    _extraTurnQueue.Enqueue(word);
                }
            }

            // Check win condition BEFORE showing popup
            CheckPlayerWinCondition();

            // Only show turn popup if game isn't over (game over has its own popup)
            if (!_gameOver)
            {
                string playerName = _playerSetupData?.PlayerName ?? "Player";
                string resultText = result == GuessResult.Hit ? "Hit" : "Miss";

                // Show guess result and handle extra turn logic
                ShowGuessResultAndProcessTurn(
                    string.Format("{0} guessed {1} - {2}.", playerName, coordLabel, resultText));
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
            _wordGuessModeController.OnWordGuessProcessed += HandleWordGuessProcessed;

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

            // Turn ending is handled in HandleWordGuessProcessed via ShowGuessResultAndProcessTurn
        }

        /// <summary>
        /// Handler for word guess processed - shows popup message and queues extra turn if correct.
        /// </summary>
        private void HandleWordGuessProcessed(int rowIndex, string guessedWord, bool wasCorrect)
        {
            // Queue correct word guesses for extra turns
            if (wasCorrect)
            {
                _extraTurnQueue.Enqueue(guessedWord.ToUpper());
            }

            // Only show turn popup if game isn't over (game over has its own popup)
            if (!_gameOver)
            {
                string playerName = _playerSetupData?.PlayerName ?? "Player";
                string resultText = wasCorrect ? "Correct" : "Incorrect";

                // Show guess result and handle extra turn logic
                ShowGuessResultAndProcessTurn(
                    string.Format("{0} guessed word '{1}' - {2}.", playerName, guessedWord.ToUpper(), resultText));
            }
        }

        private void UnsubscribeFromWordGuessModeController()
        {
            if (_wordGuessModeController == null) return;

            _wordGuessModeController.OnFeedbackRequested -= ShowFeedback;
            _wordGuessModeController.OnTurnEnded -= HandleWordGuessTurnEnded;
            _wordGuessModeController.OnWordGuessProcessed -= HandleWordGuessProcessed;

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

            // Record for opponent rubber-banding (AI) or state sync (network)
            if (result != GuessResult.AlreadyGuessed && result != GuessResult.InvalidWord)
            {
                _opponentTurnManager?.RecordPlayerGuess(result == GuessResult.Hit);
            }

            switch (result)
            {
                case GuessResult.Hit:
                    return WordGuessResult.Hit;
                case GuessResult.Miss:
                    return WordGuessResult.Miss;
                case GuessResult.AlreadyGuessed:
                    _popupController.ShowErrorPopup(string.Format("Word '{0}' already guessed. Try again!", word.ToUpper()));
                    return WordGuessResult.AlreadyGuessed;
                case GuessResult.InvalidWord:
                    _popupController.ShowErrorPopup(string.Format("'{0}' is not a valid word. Try again!", word.ToUpper()));
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

        /// <summary>
        /// Shows the guess result message, then checks if player has earned an extra turn.
        /// If an extra turn is earned, appends the extra turn message.
        /// Otherwise, appends opponent's turn message and ends the turn.
        /// </summary>
        /// <param name="guessResultMessage">The message describing the guess result (e.g., "Player guessed letter 'A' - Hit.")</param>
        private void ShowGuessResultAndProcessTurn(string guessResultMessage)
        {
            if (_gameOver) return;

            // Check if there's a queued extra turn
            if (_extraTurnQueue.Count > 0)
            {
                string completedWord = _extraTurnQueue.Dequeue();

                // Combine the guess result with the extra turn message
                string message = string.Format("{0} You completed \"{1}\" - EXTRA TURN!",
                    guessResultMessage, completedWord);
                _popupController.ShowTurnPopup(message);

                Debug.Log($"[GameplayUI] Extra turn granted for completing word: {completedWord}. {_extraTurnQueue.Count} extra turns remaining in queue.");

                // Don't end turn - player gets another turn
                // Turn indicator stays as player's turn
            }
            else
            {
                // No extra turns - end player's turn normally
                string opponentName = _opponentSetupData?.PlayerName ?? "Opponent";
                string message = string.Format("{0} {1}'s turn!", guessResultMessage, opponentName);
                _popupController.ShowTurnPopup(message);
                EndPlayerTurn();
            }
        }

        private void EndPlayerTurn()
        {
            if (_gameOver) return;

            _totalTurns++;
            PlaytestTelemetry.SetTurnNumber(_totalTurns);
            _wordGuessModeController?.ExitWordGuessMode();

            _isPlayerTurn = false;
            UpdateTurnIndicator();

            // Trigger opponent turn
            if (_opponentTurnManager != null && _opponentTurnManager.IsOpponentInitialized)
            {
                _opponentTurnManager.TriggerOpponentTurn();
            }
        }

        private void EndOpponentTurn()
        {
            if (_gameOver) return;

            _totalTurns++;
            PlaytestTelemetry.SetTurnNumber(_totalTurns);
            _isPlayerTurn = true;
            UpdateTurnIndicator();

            // Advance opponent turn counter (memory for AI, sync for network)
            _opponentTurnManager?.AdvanceTurn();
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
                    _turnIndicatorText.text = "EXECUTIONER's Turn...";
                }
            }
        }

        public bool IsPlayerTurn => _isPlayerTurn;
        public bool IsGameOver => _gameOver;

        #endregion

        #region Opponent System

        /// <summary>
        /// Initialize the opponent turn manager and AI opponent.
        /// </summary>
        private void InitializeAI()
        {
            // Build word list dictionary
            Dictionary<int, WordListSO> wordLists = new Dictionary<int, WordListSO>();
            if (_threeLetterWords != null) wordLists[3] = _threeLetterWords;
            if (_fourLetterWords != null) wordLists[4] = _fourLetterWords;
            if (_fiveLetterWords != null) wordLists[5] = _fiveLetterWords;
            if (_sixLetterWords != null) wordLists[6] = _sixLetterWords;

            // Create opponent turn manager
            _opponentTurnManager = new OpponentTurnManager(_aiConfig, gameObject, wordLists);

            // Set callbacks for guess processing (returns true if hit)
            _opponentTurnManager.SetGuessCallbacks(
                letter => ProcessOpponentLetterGuess(letter) == GuessResult.Hit,
                (col, row) => ProcessOpponentCoordinateGuess(col, row) == GuessResult.Hit,
                (word, rowIndex) =>
                {
                    GuessResult result = ProcessOpponentWordGuess(word, rowIndex);
                    if (result != GuessResult.AlreadyGuessed && result != GuessResult.InvalidWord)
                    {
                        _stateTracker.AddOpponentGuessedWord(word);
                    }
                    return result == GuessResult.Hit;
                }
            );

            // Set state accessor callbacks
            _opponentTurnManager.SetStateCallbacks(
                () => _gameOver,
                () => _isPlayerTurn
            );

            // Set player data for AI game state building
            _opponentTurnManager.SetPlayerData(_playerSetupData, _stateTracker);

            // Subscribe to opponent events
            _opponentTurnManager.OnLetterGuessProcessed += HandleOpponentLetterGuessProcessed;
            _opponentTurnManager.OnCoordinateGuessProcessed += HandleOpponentCoordinateGuessProcessed;
            _opponentTurnManager.OnWordGuessProcessed += HandleOpponentWordGuessProcessed;
            _opponentTurnManager.OnOpponentDisconnected += HandleOpponentDisconnected;
            _opponentTurnManager.OnOpponentReconnected += HandleOpponentReconnected;

            // Create PlayerSetupData for IOpponent interface
            var playerSetup = new PlayerSetupData
            {
                PlayerName = _playerSetupData.PlayerName,
                PlayerColor = _playerSetupData.PlayerColor,
                GridSize = _playerSetupData.GridSize,
                WordCount = _playerSetupData.WordCount,
                DifficultyLevel = _playerSetupData.DifficultyLevel,
                WordLengths = _playerSetupData.WordLengths,
                PlacedWords = _playerSetupData.PlacedWords
            };

            // Initialize opponent
            _opponentTurnManager.InitializeOpponent(playerSetup);
        }

        private void HandleOpponentLetterGuessProcessed(char letter, bool wasHit)
        {
            // Check opponent win condition BEFORE showing popup
            CheckOpponentWinCondition();

            if (!_gameOver)
            {
                _popupController.ShowOpponentLetterGuessResult(letter, wasHit,
                    _opponentSetupData?.PlayerName ?? "Opponent",
                    _playerSetupData?.PlayerName ?? "Player");
                EndOpponentTurn();
            }
        }

        private void HandleOpponentCoordinateGuessProcessed(int col, int row, bool wasHit)
        {
            // Check opponent win condition BEFORE showing popup
            CheckOpponentWinCondition();

            if (!_gameOver)
            {
                _popupController.ShowOpponentCoordinateGuessResult(col, row, wasHit,
                    _opponentSetupData?.PlayerName ?? "Opponent",
                    _playerSetupData?.PlayerName ?? "Player");
                EndOpponentTurn();
            }
        }

        private void HandleOpponentWordGuessProcessed(string word, int rowIndex, bool wasCorrect)
        {
            // Check opponent win condition BEFORE showing popup
            CheckOpponentWinCondition();

            if (!_gameOver)
            {
                _popupController.ShowOpponentWordGuessResult(word, wasCorrect,
                    _opponentSetupData?.PlayerName ?? "Opponent",
                    _playerSetupData?.PlayerName ?? "Player");
                EndOpponentTurn();
            }
        }

        private void HandleOpponentDisconnected()
        {
            _popupController.ShowOpponentDisconnected();
        }

        private void HandleOpponentReconnected()
        {
            _popupController.ShowOpponentReconnected();
        }

        #endregion

        #region Game Over Continue Handler

        /// <summary>
        /// Called when the Continue button is clicked on the game over popup.
        /// Fires the pending OnGameEnded event to transition to feedback screen.
        /// </summary>
        private void HandleGameOverContinue()
        {
            if (_pendingGameResult.HasValue)
            {
                bool playerWon = _pendingGameResult.Value;
                _pendingGameResult = null;

                Debug.Log($"[GameplayUI] Continue clicked - firing OnGameEnded(playerWon: {playerWon})");
                OnGameEnded?.Invoke(playerWon);
            }
        }

        #endregion

        #region Win Condition Checking

        /// <summary>
        /// Check if any opponent word rows have all letters revealed via letter guessing.
        /// If so, mark them as solved to hide the Guess Word button.
        /// Returns list of word names that were newly completed this turn.
        /// </summary>
        private List<string> CheckAndMarkFullyRevealedWords()
        {
            List<string> completedWords = new List<string>();

            if (_opponentPanel == null || _opponentSetupData == null || _winChecker == null)
                return completedWords;

            WordPatternRow[] rows = _opponentPanel.GetWordPatternRows();
            List<int> newlyRevealed = _winChecker.FindNewlyRevealedWordRows(_opponentSetupData.PlacedWords);

            foreach (int rowIndex in newlyRevealed)
            {
                if (rowIndex < rows.Length && rows[rowIndex] != null)
                {
                    string word = _opponentSetupData.PlacedWords[rowIndex].Word.ToUpper();
                    Debug.Log(string.Format("[GameplayUI] Word row {0} fully revealed via letters: {1}",
                        rowIndex + 1, word));
                    rows[rowIndex].MarkWordSolved();
                    _stateTracker.AddPlayerSolvedRow(rowIndex);
                    completedWords.Add(word);
                }
            }

            return completedWords;
        }

        /// <summary>
        /// Check if player has won by revealing all letters AND all grid positions for all opponent words.
        /// </summary>
        private void CheckPlayerWinCondition()
        {
            if (_winChecker == null || _opponentSetupData == null) return;

            if (_winChecker.CheckPlayerWinCondition(_opponentSetupData.PlacedWords))
            {
                _gameOver = true;
                UpdateTurnIndicator();

                // Trigger opponent guillotine defeat animation (player found all opponent's words)
                _uiUpdater?.TriggerOpponentGuillotineDefeatByWords();

                // Show game over popup
                _popupController.ShowGameOverPopup(true, GameOverReason.AllWordsFound);

                // Send telemetry: Player won
                PlaytestTelemetry.GameEnd(
                    true,
                    _playerMisses, _playerMissLimit,
                    _opponentMisses, _opponentMissLimit,
                    _totalTurns
                );

                // Store pending result - OnGameEnded fires when Continue is clicked
                _pendingGameResult = true;
                Debug.Log("[GameplayUI] Player won - waiting for Continue click");

                // Reveal any remaining unfound positions
                RevealOpponentWordsAndPositions();
            }
            else if (_winChecker.CheckPlayerLoseCondition())
            {
                _gameOver = true;
                UpdateTurnIndicator();

                // Trigger guillotine game over animation for player
                _uiUpdater?.TriggerPlayerGuillotineGameOver();

                // Show game over popup
                _popupController.ShowGameOverPopup(false, GameOverReason.MissLimitReached);

                // Send telemetry: Player lost (exceeded miss limit)
                PlaytestTelemetry.GameEnd(
                    false,
                    _playerMisses, _playerMissLimit,
                    _opponentMisses, _opponentMissLimit,
                    _totalTurns
                );

                // Store pending result - OnGameEnded fires when Continue is clicked
                _pendingGameResult = false;
                Debug.Log("[GameplayUI] Player lost (miss limit) - waiting for Continue click");

                // Reveal opponent words at end of game
                RevealOpponentWordsAndPositions();
            }
        }

        /// <summary>
        /// Check if opponent (AI) has won by revealing all letters AND all grid positions for all player words.
        /// </summary>
        private void CheckOpponentWinCondition()
        {
            if (_winChecker == null || _playerSetupData == null) return;

            if (_winChecker.CheckOpponentWinCondition(_playerSetupData.PlacedWords))
            {
                _gameOver = true;
                UpdateTurnIndicator();

                // Trigger player guillotine defeat animation (opponent found all player's words)
                _uiUpdater?.TriggerPlayerGuillotineDefeatByWords();

                // Show game over popup
                _popupController.ShowGameOverPopup(false, GameOverReason.OpponentFoundAllWords);

                // Send telemetry: Player lost (opponent found all words)
                PlaytestTelemetry.GameEnd(
                    false,
                    _playerMisses, _playerMissLimit,
                    _opponentMisses, _opponentMissLimit,
                    _totalTurns
                );

                // Store pending result - OnGameEnded fires when Continue is clicked
                _pendingGameResult = false;
                Debug.Log("[GameplayUI] Opponent won - waiting for Continue click");

                // Reveal opponent words at end of game
                RevealOpponentWordsAndPositions();
            }
            else if (_winChecker.CheckOpponentLoseCondition())
            {
                _gameOver = true;
                UpdateTurnIndicator();

                // Trigger guillotine game over animation for opponent
                _uiUpdater?.TriggerOpponentGuillotineGameOver();

                // Show game over popup
                _popupController.ShowGameOverPopup(true, GameOverReason.OpponentMissLimitReached);

                // Send telemetry: Player won (opponent exceeded miss limit)
                PlaytestTelemetry.GameEnd(
                    true,
                    _playerMisses, _playerMissLimit,
                    _opponentMisses, _opponentMissLimit,
                    _totalTurns
                );

                // Store pending result - OnGameEnded fires when Continue is clicked
                _pendingGameResult = true;
                Debug.Log("[GameplayUI] Opponent lost (miss limit) - waiting for Continue click");

                // Reveal any remaining unfound positions
                RevealOpponentWordsAndPositions();
            }
        }

        #endregion

        #region End Game Reveal

        /// <summary>
        /// Reveals all unfound opponent words, positions, and letters at end of game.
        /// Only reveals elements that were not found during gameplay (keeps gameplay colors intact).
        /// </summary>
        private void RevealOpponentWordsAndPositions()
        {
            if (_opponentSetupData == null || _opponentPanel == null) return;

            Debug.Log("[GameplayUI] Revealing unfound opponent words and positions...");

            // 1. Reveal all letters in word pattern rows
            WordPatternRow[] rows = _opponentPanel.GetWordPatternRows();
            for (int i = 0; i < _opponentSetupData.PlacedWords.Count && i < rows.Length; i++)
            {
                if (rows[i] != null)
                {
                    rows[i].RevealAllLetters();
                }
            }

            // 2. Reveal unfound grid positions (yellow) and reveal letters on yellow cells
            foreach (WordPlacementData wordData in _opponentSetupData.PlacedWords)
            {
                int col = wordData.StartCol;
                int row = wordData.StartRow;

                for (int i = 0; i < wordData.Word.Length; i++)
                {
                    GridCellUI cell = _opponentPanel.GetCell(col, row);
                    if (cell != null)
                    {
                        // MarkAsRevealed only affects cells that were not guessed during gameplay
                        cell.MarkAsRevealed();
                        // Also reveal any hidden letters (yellow cells, etc.) without changing color
                        cell.RevealHiddenLetterKeepColor();
                    }

                    col += wordData.DirCol;
                    row += wordData.DirRow;
                }

                // 3. Reveal unfound letters on letter tracker (yellow)
                foreach (char letter in wordData.Word.ToUpper())
                {
                    LetterButton letterBtn = _opponentPanel.GetLetterButton(letter);
                    if (letterBtn != null)
                    {
                        // MarkAsRevealed only affects letters that are in Normal state
                        letterBtn.MarkAsRevealed();
                    }
                }
            }

            Debug.Log("[GameplayUI] End-of-game reveal complete");
        }

        #endregion

        // Guillotine Display methods delegated to _uiUpdater (GameplayUIUpdater.cs)

        #region Miss Counter (Delegated to GameplayUIUpdater)

        /// <summary>
        /// Update player's miss counter and guillotine via the UI updater.
        /// </summary>
        private void UpdatePlayerMissCounter()
        {
            _uiUpdater?.UpdatePlayerMissCounter();
        }

        /// <summary>
        /// Update opponent's miss counter and guillotine via the UI updater.
        /// </summary>
        private void UpdateOpponentMissCounter()
        {
            _uiUpdater?.UpdateOpponentMissCounter();
        }

        #endregion

        #region Game State Reset

        /// <summary>
        /// Resets all gameplay state for a new game.
        /// Clears guessed word lists, letter tracker states, and grid cells.
        /// </summary>
        private void ResetGameplayState()
        {
            Debug.Log("[GameplayUI] Resetting gameplay state for new game...");

            // Clear guessed word lists
            if (_player1GuessedWordList != null)
            {
                _player1GuessedWordList.ClearAllWords();
            }
            if (_player2GuessedWordList != null)
            {
                _player2GuessedWordList.ClearAllWords();
            }

            // Reset letter tracker buttons on both panels
            if (_ownerPanel != null)
            {
                _ownerPanel.ResetAllLetterButtons();
            }
            if (_opponentPanel != null)
            {
                _opponentPanel.ResetAllLetterButtons();
            }

            // Reset guillotines
            if (_player1Guillotine != null)
            {
                _player1Guillotine.Reset();
            }
            if (_player2Guillotine != null)
            {
                _player2Guillotine.Reset();
            }

            // Clear pending game result
            _pendingGameResult = null;

            Debug.Log("[GameplayUI] Gameplay state reset complete");
        }

        #endregion

        #region Player State Initialization

        private void InitializePlayerState()
        {
            // Create state tracker and win condition checker
            _stateTracker = new GameplayStateTracker();
            _winChecker = new WinConditionChecker(_stateTracker);

            // Calculate miss limit and initialize player state
            int missLimit = GameplayStateTracker.CalculateMissLimit(
                _playerSetupData.DifficultyLevel,
                _opponentSetupData.GridSize,
                _opponentSetupData.WordCount);
            _stateTracker.InitializePlayerState(missLimit);

            // Clear any leftover extra turns from previous game
            _extraTurnQueue.Clear();
        }

        #endregion

        #region Guess Processing (Delegated to GuessProcessingManager)

        private void InitializeGuessProcessors()
        {
            // Set hit colors on guessed word lists
            _player1GuessedWordList?.SetHitColor(_playerSetupData.PlayerColor);
            _player2GuessedWordList?.SetHitColor(_opponentSetupData.PlayerColor);

            // Create and initialize guess processing manager
            _guessProcessingManager = new GuessProcessingManager(_stateTracker);
            _guessProcessingManager.SetPanels(_ownerPanel, _opponentPanel);
            _guessProcessingManager.SetSetupData(_playerSetupData, _opponentSetupData);
            _guessProcessingManager.SetCallbacks(
                IsValidWord,
                UpdatePlayerMissCounter,
                UpdateOpponentMissCounter,
                (word, correct) => _player1GuessedWordList?.AddGuessedWord(word, correct),
                (word, correct) => _player2GuessedWordList?.AddGuessedWord(word, correct)
            );
            _guessProcessingManager.Initialize();
        }

        private void InitializeOpponentState()
        {
            int missLimit = GameplayStateTracker.CalculateMissLimit(
                _opponentSetupData.DifficultyLevel,
                _playerSetupData.GridSize,
                _playerSetupData.WordCount);
            _stateTracker.InitializeOpponentState(missLimit);
        }

        // Player guess processing - delegates to GuessProcessingManager
        private GuessResult ProcessPlayerLetterGuess(char letter) => _guessProcessingManager.ProcessPlayerLetterGuess(letter);
        private GuessResult ProcessPlayerCoordinateGuess(int col, int row) => _guessProcessingManager.ProcessPlayerCoordinateGuess(col, row);
        private GuessResult ProcessPlayerWordGuess(string word, int rowIndex) => _guessProcessingManager.ProcessPlayerWordGuess(word, rowIndex);

        // Opponent guess processing - delegates to GuessProcessingManager
        private GuessResult ProcessOpponentLetterGuess(char letter) => _guessProcessingManager.ProcessOpponentLetterGuess(letter);
        private GuessResult ProcessOpponentCoordinateGuess(int col, int row) => _guessProcessingManager.ProcessOpponentCoordinateGuess(col, row);
        private GuessResult ProcessOpponentWordGuess(string word, int rowIndex) => _guessProcessingManager.ProcessOpponentWordGuess(word, rowIndex);

        /// <summary>
        /// Validate a word against the word bank.
        /// </summary>
        private bool IsValidWord(string word)
        {
            if (string.IsNullOrEmpty(word)) return false;

            string normalized = word.Trim().ToUpper();
            WordListSO wordList = GetWordListForLength(normalized.Length);

            return wordList != null && wordList.Contains(normalized);
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
    }
} 
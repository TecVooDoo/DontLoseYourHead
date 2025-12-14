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

        // State tracking and win condition checking (extracted services)
        private GameplayStateTracker _stateTracker;
        private WinConditionChecker _winChecker;

        // AI System
        private ExecutionerAI _executionerAI;
        private bool _aiInitialized = false;

        // AI opponent settings are now dynamic based on player difficulty
        // Easy player = smaller AI grid (6-8), more words (4) = easier for player to find
        // Normal player = medium AI grid (8-10), random words (3-4) = balanced
        // Hard player = larger AI grid (10-12), fewer words (3) = harder for player to find

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

        private int _opponentMisses => _stateTracker?.OpponentMisses ?? 0;
        private int _opponentMissLimit => _stateTracker?.OpponentMissLimit ?? 0;
        private HashSet<char> _opponentKnownLetters => _stateTracker?.OpponentKnownLetters ?? new HashSet<char>();
        private HashSet<char> _opponentGuessedLetters => _stateTracker?.OpponentGuessedLetters ?? new HashSet<char>();
        private HashSet<Vector2Int> _opponentGuessedCoordinates => _stateTracker?.OpponentGuessedCoordinates ?? new HashSet<Vector2Int>();
        private HashSet<int> _opponentSolvedWordRows => _stateTracker?.OpponentSolvedWordRows ?? new HashSet<int>();

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
            _stateTracker = null;
            _winChecker = null;
            _executionerAI = null;
            _aiInitialized = false;
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
                PlayerName = "Executioner",
                PlayerColor = new Color(0.6f, 0.2f, 0.2f, 1f), // Dark red
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
                PlayerName = "Executioner",
                PlayerColor = new Color(0.6f, 0.2f, 0.2f, 1f),
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

            // Check if any words are now fully revealed via coordinate guessing
            if (result == GuessResult.Hit)
            {
                CheckAndMarkFullyRevealedWords();
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

            // Copy previously guessed words so AI doesn't repeat them
            state.GuessedWords = new HashSet<string>(_stateTracker.OpponentGuessedWords);

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

            // Record revealed letter for AI memory (ProcessOpponentLetterGuess handles state tracker)
            if (result == GuessResult.Hit)
            {
                _executionerAI?.RecordRevealedLetter(letter);
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

            // Record hit for AI memory (ProcessOpponentCoordinateGuess handles state tracker)
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

            // Record revealed letters for AI memory (ProcessOpponentWordGuess handles state tracker)
            if (result == GuessResult.Hit)
            {
                foreach (char letter in word.ToUpper())
                {
                    _executionerAI?.RecordRevealedLetter(letter);
                }
            }

            if (result != GuessResult.AlreadyGuessed && result != GuessResult.InvalidWord)
            {
                _stateTracker.AddOpponentGuessedWord(word);
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
            if (_opponentPanel == null || _opponentSetupData == null || _winChecker == null) return;

            WordPatternRow[] rows = _opponentPanel.GetWordPatternRows();
            List<int> newlyRevealed = _winChecker.FindNewlyRevealedWordRows(_opponentSetupData.PlacedWords);

            foreach (int rowIndex in newlyRevealed)
            {
                if (rowIndex < rows.Length && rows[rowIndex] != null)
                {
                    Debug.Log(string.Format("[GameplayUI] Word row {0} fully revealed via letters: {1}",
                        rowIndex + 1, _opponentSetupData.PlacedWords[rowIndex].Word));
                    rows[rowIndex].MarkWordSolved();
                    _stateTracker.AddPlayerSolvedRow(rowIndex);
                }
            }
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
            }
            else if (_winChecker.CheckPlayerLoseCondition())
            {
                _gameOver = true;
                UpdateTurnIndicator();
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
            }
            else if (_winChecker.CheckOpponentLoseCondition())
            {
                _gameOver = true;
                UpdateTurnIndicator();
            }
        }

        #endregion

        #region Miss Counter

        private void UpdateMissCounters()
        {
            if (_player1MissCounter != null && _stateTracker != null)
            {
                _player1MissCounter.text = _stateTracker.GetPlayerMissCounterText();
            }

            if (_player2MissCounter != null && _stateTracker != null)
            {
                _player2MissCounter.text = _stateTracker.GetOpponentMissCounterText();
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

            // Apply player colors to miss counter labels and guessed word list labels
            ApplyPlayerColorsToLabels();
        }

        /// <summary>
        /// Sets the background Image color of miss counter labels and guessed word list labels
        /// to match each player's chosen color.
        /// </summary>
        private void ApplyPlayerColorsToLabels()
        {
            // Player 1 label backgrounds
            if (_playerSetupData != null)
            {
                Color player1Color = _playerSetupData.PlayerColor;

                if (_player1MissLabelBackground != null)
                    _player1MissLabelBackground.color = player1Color;

                if (_player1GuessedWordsLabelBackground != null)
                    _player1GuessedWordsLabelBackground.color = player1Color;
            }

            // Player 2 (opponent) label backgrounds
            if (_opponentSetupData != null)
            {
                Color player2Color = _opponentSetupData.PlayerColor;

                if (_player2MissLabelBackground != null)
                    _player2MissLabelBackground.color = player2Color;

                if (_player2GuessedWordsLabelBackground != null)
                    _player2GuessedWordsLabelBackground.color = player2Color;
            }
        }

        private void UpdatePlayerMissCounter()
        {
            if (_player1MissCounter != null && _stateTracker != null)
            {
                _player1MissCounter.text = _stateTracker.GetPlayerMissCounterText();
            }

            // Win/lose checking delegated to WinConditionChecker via CheckPlayerWinCondition
        }

        private void UpdateOpponentMissCounter()
        {
            if (_player2MissCounter != null && _stateTracker != null)
            {
                _player2MissCounter.text = _stateTracker.GetOpponentMissCounterText();
            }

            // Win/lose checking delegated to WinConditionChecker via CheckOpponentWinCondition
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
                (amount) => { _stateTracker.AddPlayerMisses(amount); UpdatePlayerMissCounter(); },
                (letter, state) => _opponentPanel.SetLetterState(letter, state),
                word => IsValidWord(word),
                (word, correct) => _player1GuessedWordList?.AddGuessedWord(word, correct)
            );
            _playerGuessProcessor.Initialize(_stateTracker.PlayerMissLimit);

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
                (amount) => { _stateTracker.AddOpponentMisses(amount); UpdateOpponentMissCounter(); },
                (letter, state) => _ownerPanel.SetLetterState(letter, state),
                word => IsValidWord(word),
                (word, correct) => _player2GuessedWordList?.AddGuessedWord(word, correct)
            );
            _opponentGuessProcessor.Initialize(_stateTracker.OpponentMissLimit);
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
                _stateTracker.AddPlayerKnownLetter(letter);
            }
            _stateTracker.AddPlayerGuessedLetter(letter);

            return ConvertGuessResult(result);
        }

        /// <summary>
        /// Process player guessing a coordinate on opponent's grid
        /// </summary>
        private GuessResult ProcessPlayerCoordinateGuess(int col, int row)
        {
            GuessProcessor.GuessResult result = _playerGuessProcessor.ProcessCoordinateGuess(col, row);

            _stateTracker.AddPlayerGuessedCoordinate(col, row);

            return ConvertGuessResult(result);
        }

        /// <summary>
        /// Process player guessing a complete word.
        /// NOTE: Correct word guess reveals LETTERS but NOT grid positions.
        /// Grid positions must be guessed via coordinate guessing for win condition.
        /// </summary>
        private GuessResult ProcessPlayerWordGuess(string word, int rowIndex)
        {
            GuessProcessor.GuessResult result = _playerGuessProcessor.ProcessWordGuess(word, rowIndex);

            // Track solved rows via state tracker for UI button management
            if (result == GuessProcessor.GuessResult.Hit)
            {
                _stateTracker.AddPlayerSolvedRow(rowIndex);

                // When a word is correctly guessed, add all its letters as known
                // NOTE: Do NOT add coordinates - those must be guessed via coordinate guessing
                if (rowIndex < _opponentSetupData.PlacedWords.Count)
                {
                    WordPlacementData wordData = _opponentSetupData.PlacedWords[rowIndex];

                    // Add all letters as known
                    foreach (char c in wordData.Word.ToUpper())
                    {
                        _stateTracker.AddPlayerKnownLetter(c);
                        _stateTracker.AddPlayerGuessedLetter(c);
                    }
                }
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
            // Calculate miss limit and initialize opponent state
            int missLimit = GameplayStateTracker.CalculateMissLimit(
                _opponentSetupData.DifficultyLevel,
                _playerSetupData.GridSize,
                _playerSetupData.WordCount);
            _stateTracker.InitializeOpponentState(missLimit);
        }

        /// <summary>
        /// Process opponent guessing a letter against player's words
        /// </summary>
        private GuessResult ProcessOpponentLetterGuess(char letter)
        {
            GuessProcessor.GuessResult result = _opponentGuessProcessor.ProcessLetterGuess(letter);

            if (result == GuessProcessor.GuessResult.Hit)
            {
                _stateTracker.AddOpponentKnownLetter(letter);
            }
            _stateTracker.AddOpponentGuessedLetter(letter);

            return ConvertGuessResult(result);
        }

        /// <summary>
        /// Process opponent guessing a coordinate on player's grid
        /// </summary>
        private GuessResult ProcessOpponentCoordinateGuess(int col, int row)
        {
            GuessProcessor.GuessResult result = _opponentGuessProcessor.ProcessCoordinateGuess(col, row);

            _stateTracker.AddOpponentGuessedCoordinate(col, row);

            return ConvertGuessResult(result);
        }

        /// <summary>
        /// Process opponent guessing a complete word.
        /// NOTE: Correct word guess reveals LETTERS but NOT grid positions.
        /// Grid positions must be guessed via coordinate guessing for win condition.
        /// </summary>
        private GuessResult ProcessOpponentWordGuess(string word, int rowIndex)
        {
            GuessProcessor.GuessResult result = _opponentGuessProcessor.ProcessWordGuess(word, rowIndex);

            // Track solved rows and update known letters for win condition
            if (result == GuessProcessor.GuessResult.Hit)
            {
                _stateTracker.AddOpponentSolvedRow(rowIndex);

                // When a word is correctly guessed, add all its letters as known
                // NOTE: Do NOT add coordinates - those must be guessed via coordinate guessing
                if (rowIndex < _playerSetupData.PlacedWords.Count)
                {
                    WordPlacementData wordData = _playerSetupData.PlacedWords[rowIndex];

                    // Add all letters as known
                    foreach (char c in wordData.Word.ToUpper())
                    {
                        _stateTracker.AddOpponentKnownLetter(c);
                        _stateTracker.AddOpponentGuessedLetter(c);
                    }
                }
            }

            return ConvertGuessResult(result);
        }

        #endregion
    }
}
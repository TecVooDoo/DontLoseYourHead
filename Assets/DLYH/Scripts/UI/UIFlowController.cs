using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.InputSystem;
using TecVooDoo.DontLoseYourHead.Core;
using TecVooDoo.DontLoseYourHead.UI;
using DLYH.Audio;
using DLYH.Telemetry;
using DLYH.Networking;
using DLYH.Networking.UI;
using DLYH.Networking.Services;
using DLYH.AI.Config;
using DLYH.AI.Strategies;
using DLYH.Core.Utilities;
using DLYH.UI.Managers;
using Cysharp.Threading.Tasks;

namespace DLYH.TableUI
{
    /// <summary>
    /// The game mode selected from the main menu.
    /// Determines which flow path the setup wizard takes.
    /// </summary>
    public enum GameMode
    {
        Solo,       // vs AI (The Executioner)
        Online,     // vs another player online
        JoinGame    // Joining an existing game via code
    }

    /// <summary>
    /// Manages the flow between UI screens (Main Menu -> Setup Wizard -> Gameplay).
    /// Uses a single UIDocument and swaps content by showing/hiding containers.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public class UIFlowController : MonoBehaviour
    {
        [Header("UXML Assets")]
        [SerializeField] private VisualTreeAsset _mainMenuUxml;
        [SerializeField] private VisualTreeAsset _setupWizardUxml;
        [SerializeField] private VisualTreeAsset _gameplayUxml;
        [SerializeField] private VisualTreeAsset _guillotineOverlayUxml;

        [Header("USS Assets")]
        [SerializeField] private StyleSheet _mainMenuUss;
        [SerializeField] private StyleSheet _setupWizardUss;
        [SerializeField] private StyleSheet _tableViewUss;
        [SerializeField] private StyleSheet _feedbackModalUss;
        [SerializeField] private StyleSheet _hamburgerMenuUss;
        [SerializeField] private StyleSheet _gameplayUss;
        [SerializeField] private StyleSheet _guillotineOverlayUss;

        [Header("Modal Assets")]
        [SerializeField] private VisualTreeAsset _feedbackModalUxml;
        [SerializeField] private VisualTreeAsset _hamburgerMenuUxml;

        [Header("Networking UI Assets")]
        [SerializeField] private VisualTreeAsset _matchmakingOverlayUxml;
        [SerializeField] private VisualTreeAsset _waitingRoomUxml;
        [SerializeField] private VisualTreeAsset _joinCodeEntryUxml;
        [SerializeField] private StyleSheet _matchmakingOverlayUss;
        [SerializeField] private StyleSheet _waitingRoomUss;
        [SerializeField] private StyleSheet _joinCodeEntryUss;

        [Header("Networking Backend")]
        [SerializeField] private SupabaseConfig _supabaseConfig;

        [Header("Word Lists")]
        [SerializeField] private WordListSO _threeLetterWords;
        [SerializeField] private WordListSO _fourLetterWords;
        [SerializeField] private WordListSO _fiveLetterWords;
        [SerializeField] private WordListSO _sixLetterWords;

        private UIDocument _uiDocument;
        private VisualElement _root;

        // Screen containers
        private VisualElement _mainMenuScreen;
        private VisualElement _setupWizardScreen;
        private VisualElement _gameplayScreen;

        // Gameplay managers
        private GameplayScreenManager _gameplayManager;
        private GuillotineOverlayManager _guillotineOverlayManager;

        // Wizard state (managed inline since we can't use SetupWizardController as MonoBehaviour)
        private SetupWizardUIManager _wizardManager;

        // Table components (for placement phase)
        private TableModel _tableModel;
        private TableView _tableView;
        private TableLayout _tableLayout;
        private WordRowsContainer _wordRowsContainer;
        private PlacementAdapter _placementAdapter;
        private WordSuggestionDropdown _wordSuggestionDropdown;

        // Defense view components (player's grid for opponent to attack)
        private TableModel _defenseTableModel;
        private WordRowsContainer _defenseWordRows;

        // Attack view components (opponent's grid for player to attack)
        private TableModel _attackTableModel;
        private TableLayout _attackTableLayout;
        private TableView _attackTableView;

        // Services
        private WordValidationService _wordValidationService;

        // Networking UI
        private NetworkingUIManager _networkingUIManager;
        private NetworkingUIResult _matchmakingResult; // Stores result from matchmaking (phantom AI name, etc.)

        // Networking services (created from SupabaseConfig)
        private SupabaseClient _supabaseClient;
        private PlayerService _playerService;
        private GameSessionService _gameSessionService;
        private MatchmakingService _matchmakingService;

        // Game mode tracking
        private GameMode _currentGameMode = GameMode.Solo;
        private string _currentGameCode; // Current online game code (for resume, networking)

        private bool _isInitialized = false;
        private bool _keyboardWiredUp = false;
        private bool _hasActiveGame = false;

        // Continue game button
        private Button _continueGameButton;

        // Settings constants (match SettingsPanel.cs)
        private const string PREFS_SFX_VOLUME = "DLYH_SFXVolume";
        private const string PREFS_MUSIC_VOLUME = "DLYH_MusicVolume";
        private const string PREFS_QWERTY_KEYBOARD = "DLYH_QwertyKeyboard";
        private const float DEFAULT_VOLUME = 0.5f;

        // Settings UI elements
        private Slider _sfxSlider;
        private Slider _musicSlider;
        private Toggle _qwertyToggle;
        private Label _sfxValueLabel;
        private Label _musicValueLabel;
        private Label _triviaLabel;
        private Label _authStatusLabel;

        // Guillotine and beheading trivia facts (matches MainMenuController.cs)
        private static readonly string[] TRIVIA_FACTS = new string[]
        {
            "The guillotine was used in France until 1977.",
            "Dr. Joseph-Ignace Guillotin proposed the device as a humane execution method.",
            "During the Reign of Terror, over 16,000 were guillotined in France.",
            "The guillotine was nicknamed 'The National Razor' in France.",
            "Marie Antoinette was executed by guillotine on October 16, 1793.",
            "King Louis XVI was guillotined on January 21, 1793.",
            "The last public guillotine execution in France was in 1939.",
            "Executioners in France were often from families that held the job for generations.",
            "The guillotine blade falls at approximately 21 feet per second.",
            "Anne Boleyn was beheaded with a sword, not an axe, at her request.",
            "The word 'decapitate' comes from Latin 'de' (off) and 'caput' (head).",
            "Henry VIII had two of his six wives beheaded.",
            "Sir Walter Raleigh was beheaded in 1618 after 13 years in the Tower of London.",
            "Mary, Queen of Scots required three blows of the axe to be beheaded.",
            "The Halifax Gibbet was used in England from 1286 to 1650.",
            "Thomas More was beheaded for refusing to acknowledge Henry VIII as head of the Church.",
            "Scotland's 'Maiden' guillotine was used from 1564 to 1708.",
            "Legend says the guillotine blade weighs about 88 pounds.",
            "Heads were sometimes held up to the crowd after execution.",
            "Some executioners became celebrities in revolutionary France.",
            "The guillotine was considered more egalitarian than other methods.",
            "Charlotte Corday was guillotined for assassinating Jean-Paul Marat.",
            "Lavoisier, the father of chemistry, was guillotined in 1794.",
            "The term 'guillotine' was not used until after Dr. Guillotin's proposal."
        };

        // Trivia rotation state
        private Coroutine _triviaCoroutine;
        private int _currentTriviaIndex = -1;
        private const float TRIVIA_DISPLAY_DURATION = 5f;
        private const float TRIVIA_FADE_DURATION = 0.5f;

        // Feedback modal state
        private VisualElement _feedbackModalContainer;
        private TextField _feedbackInput;
        private Label _feedbackTitle;
        private bool _feedbackIsPostGame = false;
        private bool _feedbackPlayerWon = false;

        // Modal managers (extracted to DLYH.UI.Managers)
        private ConfirmationModalManager _confirmationModal;
        private HelpModalManager _helpModal;
        private ActiveGamesManager _activeGamesManager;

        // Gameplay guess manager
        private GameplayGuessManager _guessManager;
        private bool _isPlayerTurn = true;
        private bool _isGameOver = false;
        private bool _waitingForOpponent = false; // True when online game waiting for opponent to join

        // Extra turn tracking
        private Queue<int> _playerExtraTurnQueue = new Queue<int>(); // Word indices for player extra turns
        private Queue<int> _opponentExtraTurnQueue = new Queue<int>(); // Word indices for opponent extra turns

        // Game state tracking for persistence (Session 3)
        // Tracks what player revealed on opponent's grid (for saving to Supabase)
        private Dictionary<Vector2Int, (char letter, bool isHit)> _playerRevealedCells = new Dictionary<Vector2Int, (char, bool)>();
        // Tracks what opponent revealed on player's grid (for saving to Supabase)
        private Dictionary<Vector2Int, (char letter, bool isHit)> _opponentRevealedCells = new Dictionary<Vector2Int, (char, bool)>();
        // Tracks letters guessed via keyboard by player
        private HashSet<char> _playerGuessedLetters = new HashSet<char>();
        // Tracks solved word indices for player
        private HashSet<int> _playerSolvedWords = new HashSet<int>();
        // Tracks solved word indices for opponent (defense words they've correctly guessed)
        private HashSet<int> _opponentSolvedWords = new HashSet<int>();
        // Tracks incorrect word guesses for display in Guessed Words panel
        private List<(string word, bool isPlayer, bool isCorrect)> _allWordGuesses = new List<(string, bool, bool)>();

        // AI Opponent
        [SerializeField] private ExecutionerConfigSO _aiConfig;
        [SerializeField] private List<WordListSO> _wordLists;
        private IOpponent _opponent;
        private PlayerSetupData _playerSetupData;
        private List<WordPlacementData> _opponentWordPlacements; // Stored for end-game reveal
        private Coroutine _turnDelayCoroutine;
        private Coroutine _opponentTurnTimeoutCoroutine;
        private Coroutine _gameOverSequenceCoroutine;
        private const float TURN_SWITCH_DELAY = 0.8f; // Delay before switching turns
        private const float OPPONENT_THINK_MIN_DELAY = 0.5f; // Minimum AI "thinking" time
        private const float OPPONENT_TURN_TIMEOUT = 15f; // Maximum time to wait for AI turn
        private const float OPPONENT_JOIN_POLL_INTERVAL = 3f; // Seconds between opponent join checks (Session 4)

        // Guillotine stage tracking for audio
        private int _playerPreviousStage = 1;
        private int _opponentPreviousStage = 1;
        private bool _isGamePausedForStageTransition = false;
        private bool _wasPlayerTurnBeforePause = true;

        // Hamburger menu state
        private VisualElement _hamburgerMenuContainer;
        private VisualElement _hamburgerOverlay;
        private Button _hamburgerButton;
        private Button _resumeButton;
        private Slider _hbSfxSlider;
        private Slider _hbMusicSlider;
        private Toggle _hbQwertyToggle;
        private Label _hbSfxValueLabel;
        private Label _hbMusicValueLabel;

        private void Awake()
        {
            _uiDocument = GetComponent<UIDocument>();
        }

        private void Start()
        {
            Initialize();
        }

        private void OnEnable()
        {
            // Re-initialize if returning from a disable (like domain reload)
            if (_isInitialized && _root == null && Application.isPlaying)
            {
                Initialize();
            }
        }

        private void Update()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null)
            {
                return;
            }

            // Check for word guess mode keyboard input during gameplay
            if (_gameplayScreen != null && _gameplayScreen.style.display == DisplayStyle.Flex)
            {
                if (_attackWordRows != null && _attackWordRows.IsInWordGuessMode)
                {
                    ProcessWordGuessKeyboardInput(keyboard);
                    return;
                }
            }

            // Handle physical keyboard input during placement panel (word entry)
            if (_setupWizardScreen == null || _setupWizardScreen.style.display == DisplayStyle.None)
            {
                return;
            }

            // Only process keyboard when in word entry mode (placement panel visible)
            if (_wordRowsContainer == null)
            {
                return;
            }

            // Check for letter keys A-Z
            for (int i = 0; i < 26; i++)
            {
                Key key = (Key)((int)Key.A + i);
                if (keyboard[key].wasPressedThisFrame)
                {
                    char letter = (char)('A' + i);
                    HandleLetterKeyPressed(letter);
                    return; // Only process one key per frame
                }
            }

            // Check for backspace
            if (keyboard.backspaceKey.wasPressedThisFrame)
            {
                HandleBackspacePressed();
            }

            // Check for Escape to cancel placement mode
            if (keyboard.escapeKey.wasPressedThisFrame)
            {
                if (_placementAdapter != null && _placementAdapter.IsInPlacementMode)
                {
                    _placementAdapter.CancelPlacementMode();
                }
            }
        }

        /// <summary>
        /// Processes keyboard input during word guess mode.
        /// </summary>
        private void ProcessWordGuessKeyboardInput(Keyboard keyboard)
        {
            // Check for letter keys A-Z
            for (int i = 0; i < 26; i++)
            {
                Key key = (Key)((int)Key.A + i);
                if (keyboard[key].wasPressedThisFrame)
                {
                    char letter = (char)('A' + i);
                    _attackWordRows.TypeLetterInGuessMode(letter);
                    return;
                }
            }

            // Check for backspace
            if (keyboard.backspaceKey.wasPressedThisFrame)
            {
                int wordIndex = _attackWordRows.WordGuessRowIndex;
                if (wordIndex >= 0)
                {
                    WordRowView row = _attackWordRows.GetRow(wordIndex);
                    row?.Backspace();
                }
            }

            // Check for Enter to submit
            if (keyboard.enterKey.wasPressedThisFrame || keyboard.numpadEnterKey.wasPressedThisFrame)
            {
                int wordIndex = _attackWordRows.WordGuessRowIndex;
                if (wordIndex >= 0)
                {
                    WordRowView row = _attackWordRows.GetRow(wordIndex);
                    if (row != null && row.IsGuessComplete())
                    {
                        // Submit the guess
                        string guessedWord = row.GetFullGuessWord();
                        row.ExitWordGuessMode();
                        HandleInlineWordGuessSubmitted(wordIndex, guessedWord);
                    }
                }
            }

            // Check for Escape to cancel
            if (keyboard.escapeKey.wasPressedThisFrame)
            {
                _attackWordRows.ExitWordGuessMode();
            }
        }

        private void Initialize()
        {
            // Guard against editor-time or double initialization
            if (!Application.isPlaying) return;
            if (_isInitialized && _root != null) return;

            // Initialize services (handle null word lists gracefully)
            if (_threeLetterWords != null || _fourLetterWords != null ||
                _fiveLetterWords != null || _sixLetterWords != null)
            {
                _wordValidationService = new WordValidationService(
                    _threeLetterWords,
                    _fourLetterWords,
                    _fiveLetterWords,
                    _sixLetterWords);
            }

            // Get root visual element - check for null (can happen during Unity reload)
            if (_uiDocument == null)
            {
                _uiDocument = GetComponent<UIDocument>();
            }

            _root = _uiDocument.rootVisualElement;

            if (_root == null)
            {
                Debug.LogError("[UIFlowController] rootVisualElement is null - UIDocument may not be properly configured");
                return;
            }

            _root.Clear();

            // Make root fill the screen
            _root.style.flexGrow = 1;

            // Apply all stylesheets to root
            if (_mainMenuUss != null) _root.styleSheets.Add(_mainMenuUss);
            if (_setupWizardUss != null) _root.styleSheets.Add(_setupWizardUss);
            if (_tableViewUss != null) _root.styleSheets.Add(_tableViewUss);
            if (_feedbackModalUss != null) _root.styleSheets.Add(_feedbackModalUss);
            if (_hamburgerMenuUss != null) _root.styleSheets.Add(_hamburgerMenuUss);
            if (_gameplayUss != null) _root.styleSheets.Add(_gameplayUss);
            if (_guillotineOverlayUss != null) _root.styleSheets.Add(_guillotineOverlayUss);

            // Initialize modal managers
            _confirmationModal = new ConfirmationModalManager();
            _confirmationModal.Initialize(_root);
            _helpModal = new HelpModalManager();
            _helpModal.Initialize(_root);

            // Create screens - wizard first so menu is on top
            CreateSetupWizardScreen();
            CreateGameplayScreen();
            CreateMainMenuScreen();
            CreateFeedbackModal();
            CreateHamburgerMenu();

            // Initialize networking UI manager
            InitializeNetworkingUI();

            // Show main menu first (hides wizard)
            ShowMainMenu();

            _isInitialized = true;
        }

        private void CreateMainMenuScreen()
        {
            _mainMenuScreen = new VisualElement();
            _mainMenuScreen.name = "main-menu-screen";
            _mainMenuScreen.style.flexGrow = 1;
            _mainMenuScreen.style.position = Position.Absolute;
            _mainMenuScreen.style.left = 0;
            _mainMenuScreen.style.right = 0;
            _mainMenuScreen.style.top = 0;
            _mainMenuScreen.style.bottom = 0;

            // Clone the main menu UXML
            if (_mainMenuUxml != null)
            {
                VisualElement menuContent = _mainMenuUxml.CloneTree();
                // Make the TemplateContainer fill the parent - need all sizing properties
                menuContent.style.flexGrow = 1;
                menuContent.style.width = Length.Percent(100);
                menuContent.style.height = Length.Percent(100);
                menuContent.style.position = Position.Absolute;
                menuContent.style.left = 0;
                menuContent.style.top = 0;
                menuContent.style.right = 0;
                menuContent.style.bottom = 0;
                _mainMenuScreen.Add(menuContent);

                // Also ensure the inner main-menu-root fills its container
                var menuRoot = menuContent.Q<VisualElement>("main-menu-root");
                if (menuRoot != null)
                {
                    menuRoot.style.flexGrow = 1;
                    menuRoot.style.width = Length.Percent(100);
                    menuRoot.style.height = Length.Percent(100);
                }

            }

            _root.Add(_mainMenuScreen);

            // Set up button handlers
            _continueGameButton = _mainMenuScreen.Q<Button>("btn-continue-game");
            Button playSoloBtn = _mainMenuScreen.Q<Button>("btn-play-solo");
            Button playOnlineBtn = _mainMenuScreen.Q<Button>("btn-play-online");
            Button joinGameBtn = _mainMenuScreen.Q<Button>("btn-join-game");
            Button howToPlayBtn = _mainMenuScreen.Q<Button>("btn-how-to-play");
            Button feedbackBtn = _mainMenuScreen.Q<Button>("btn-feedback");

            if (_continueGameButton != null)
            {
                _continueGameButton.clicked += HandleContinueGameClicked;
            }
            if (playSoloBtn != null)
            {
                playSoloBtn.clicked += () => HandleGameModeSelected(GameMode.Solo);
            }
            if (playOnlineBtn != null)
            {
                playOnlineBtn.clicked += () => HandleGameModeSelected(GameMode.Online);
            }
            if (joinGameBtn != null)
            {
                joinGameBtn.clicked += () => HandleGameModeSelected(GameMode.JoinGame);
            }
            if (howToPlayBtn != null)
            {
                howToPlayBtn.clicked += HandleHowToPlayClicked;
            }
            if (feedbackBtn != null)
            {
                feedbackBtn.clicked += HandleFeedbackClicked;
            }

            Button exitBtn = _mainMenuScreen.Q<Button>("btn-exit");
            if (exitBtn != null)
            {
                exitBtn.clicked += HandleExitClicked;
            }

            // Set up inline settings
            SetupInlineSettings();

            // Set up My Active Games section
            SetupMyActiveGames();

            // Set up trivia marquee
            _triviaLabel = _mainMenuScreen.Q<Label>("trivia-label");

            // Set version
            Label versionLabel = _mainMenuScreen.Q<Label>("version-label");
            if (versionLabel != null)
            {
                versionLabel.text = $"v{Application.version}";
            }

            // Auth status label
            _authStatusLabel = _mainMenuScreen.Q<Label>("auth-status-label");
            UpdateAuthStatusDisplay();
        }

        private void SetupInlineSettings()
        {
            // Cache slider and toggle elements
            _sfxSlider = _mainMenuScreen.Q<Slider>("sfx-slider");
            _musicSlider = _mainMenuScreen.Q<Slider>("music-slider");
            _qwertyToggle = _mainMenuScreen.Q<Toggle>("qwerty-toggle");
            _sfxValueLabel = _mainMenuScreen.Q<Label>("sfx-value");
            _musicValueLabel = _mainMenuScreen.Q<Label>("music-value");

            // Load saved values
            float savedSfx = PlayerPrefs.GetFloat(PREFS_SFX_VOLUME, DEFAULT_VOLUME);
            float savedMusic = PlayerPrefs.GetFloat(PREFS_MUSIC_VOLUME, DEFAULT_VOLUME);
            bool savedQwerty = PlayerPrefs.GetInt(PREFS_QWERTY_KEYBOARD, 0) == 1;

            // Initialize sliders
            if (_sfxSlider != null)
            {
                _sfxSlider.value = savedSfx;
                _sfxSlider.RegisterValueChangedCallback(OnSfxVolumeChanged);
                UpdateSfxLabel(savedSfx);
            }

            if (_musicSlider != null)
            {
                _musicSlider.value = savedMusic;
                _musicSlider.RegisterValueChangedCallback(OnMusicVolumeChanged);
                UpdateMusicLabel(savedMusic);
            }

            if (_qwertyToggle != null)
            {
                _qwertyToggle.value = savedQwerty;
                _qwertyToggle.RegisterValueChangedCallback(OnQwertyToggleChanged);
            }
        }

        private void SetupMyActiveGames()
        {
            // Initialize ActiveGamesManager (handles My Games UI, hidden games persistence)
            _activeGamesManager = new ActiveGamesManager();
            _activeGamesManager.Initialize(_mainMenuScreen, _gameSessionService, _playerService);
            _activeGamesManager.OnResumeRequested = HandleResumeGameFromActiveGames;
        }

        /// <summary>
        /// Updates the auth status display in the main menu footer.
        /// Shows "Playing as Guest" or player name if available.
        /// </summary>
        private void UpdateAuthStatusDisplay()
        {
            if (_authStatusLabel == null)
            {
                return;
            }

            // Check if we have a player record
            if (_playerService != null && _playerService.HasPlayerRecord)
            {
                string playerName = _playerService.CurrentPlayerName;
                // Treat default "Player" name as Guest (not authenticated)
                if (!string.IsNullOrEmpty(playerName) && playerName != "Player")
                {
                    _authStatusLabel.text = $"Playing as {playerName}";
                    _authStatusLabel.AddToClassList("signed-in");
                }
                else
                {
                    _authStatusLabel.text = "Playing as Guest";
                    _authStatusLabel.RemoveFromClassList("signed-in");
                }
            }
            else
            {
                _authStatusLabel.text = "Playing as Guest";
                _authStatusLabel.RemoveFromClassList("signed-in");
            }
        }

        // NOTE: LoadMyActiveGamesAsync, ShowMyActiveGames, HideMyActiveGames, CreateMyGameItem
        // moved to ActiveGamesManager (Session 4 extraction)

        /// <summary>
        /// Callback handler for when user clicks Resume on an active game.
        /// Called by ActiveGamesManager.OnResumeRequested.
        /// </summary>
        private void HandleResumeGameFromActiveGames(string gameCode)
        {
            Debug.Log($"[UIFlowController] Resume game: {gameCode}");
            UIAudioManager.ButtonClick();

            // Check if there's an active game in progress
            if (_hasActiveGame)
            {
                // If resuming the same game, no need for confirmation
                if (_currentGameCode == gameCode)
                {
                    Debug.Log($"[UIFlowController] Already playing game {gameCode}, just showing gameplay");
                    ShowGameplayScreen();
                    return;
                }

                // Different game - warn the player
                string warningMessage = _currentGameMode == GameMode.Solo
                    ? "Resuming this game will end your current solo game. Continue?"
                    : "Resuming this game will leave your current online game. Continue?";

                _confirmationModal.Show(
                    "End Current Game?",
                    warningMessage,
                    () => ResumeGameAfterConfirmation(gameCode)
                );
                return;
            }

            // Always reset state before resuming, even if no "active" game
            // This clears any residual state from previous games (solo, online, etc.)
            ResetGameState();
            HandleResumeGameAsync(gameCode).Forget();
        }

        private void ResumeGameAfterConfirmation(string gameCode)
        {
            // Clean up current game state
            ResetGameState();

            // If leaving an online game, we don't need to do anything special -
            // the game stays in Supabase and can be resumed later

            // Now resume the selected game
            HandleResumeGameAsync(gameCode).Forget();
        }

        private async UniTask HandleResumeGameAsync(string gameCode)
        {
            if (_gameSessionService == null)
            {
                Debug.LogError("[UIFlowController] Cannot resume game - GameSessionService not available");
                return;
            }

            // Load game with player data from Supabase
            GameSessionWithPlayers gameWithPlayers = await _gameSessionService.GetGameWithPlayers(gameCode);
            if (gameWithPlayers == null || gameWithPlayers.Session == null)
            {
                Debug.LogError($"[UIFlowController] Failed to load game {gameCode}");
                return;
            }

            Debug.Log($"[UIFlowController] Loaded game {gameCode} with {gameWithPlayers.Players?.Length ?? 0} players");

            // Determine which player we are
            string myPlayerId = _playerService?.CurrentPlayerId;
            int myPlayerNumber = 0;
            foreach (var player in gameWithPlayers.Players ?? Array.Empty<SessionPlayer>())
            {
                if (player.PlayerId == myPlayerId)
                {
                    myPlayerNumber = player.PlayerNumber;
                    break;
                }
            }

            if (myPlayerNumber == 0)
            {
                Debug.LogError($"[UIFlowController] Current player not found in game {gameCode}");
                return;
            }

            Debug.Log($"[UIFlowController] I am player {myPlayerNumber} in game {gameCode}");

            // Check if opponent is phantom AI (has the special executioner player ID)
            bool isPhantomAI = false;
            int opponentPlayerNumber = myPlayerNumber == 1 ? 2 : 1;
            foreach (var player in gameWithPlayers.Players ?? Array.Empty<SessionPlayer>())
            {
                if (player.PlayerNumber == opponentPlayerNumber &&
                    player.PlayerId == DLYH.Networking.Services.PlayerService.EXECUTIONER_PLAYER_ID)
                {
                    isPhantomAI = true;
                    Debug.Log($"[UIFlowController] Opponent is phantom AI (Executioner)");
                    break;
                }
            }

            // Parse the game state JSON
            Debug.Log($"[UIFlowController] Raw StateJson: {gameWithPlayers.Session.StateJson}");

            DLYHGameState gameState = GameStateManager.ParseGameStateJson(gameWithPlayers.Session.StateJson);
            if (gameState == null)
            {
                Debug.LogError($"[UIFlowController] Failed to parse game state for {gameCode}");
                return;
            }

            Debug.Log($"[UIFlowController] Parsed state - player1: {(gameState.player1 != null ? gameState.player1.name : "null")}, " +
                      $"player2: {(gameState.player2 != null ? gameState.player2.name : "null")}");

            // Get my player data and opponent data
            DLYHPlayerData myData = myPlayerNumber == 1 ? gameState.player1 : gameState.player2;
            DLYHPlayerData opponentData = myPlayerNumber == 1 ? gameState.player2 : gameState.player1;

            if (myData == null || myData.gridSize == 0)
            {
                Debug.LogError($"[UIFlowController] My player data not found in game {gameCode}. " +
                              $"myData null: {myData == null}, gridSize: {myData?.gridSize ?? 0}");
                return;
            }

            // Store game info for networking
            _currentGameCode = gameCode;
            _currentGameMode = GameMode.Online;
            _matchmakingResult = new NetworkingUIResult
            {
                Success = true,
                GameCode = gameCode,
                IsHost = myPlayerNumber == 1,
                IsPhantomAI = isPhantomAI,
                OpponentName = opponentData?.name ?? "Waiting..."
            };

            // Transition to gameplay with loaded state
            await TransitionToGameplayFromSavedStateAsync(gameCode, myPlayerNumber, myData, opponentData, gameState, isPhantomAI);
        }

        // NOTE: ParseGameStateJson, ParsePlayerData, ParseGameplayState moved to GameStateManager
        // NOTE: ExtractStringField, ExtractIntField, ExtractBoolField moved to JsonParsingUtility

        /// <summary>
        /// Saves the local player's setup data to Supabase for online games.
        /// Called after CapturePlayerSetupData for non-phantom-AI online games.
        /// </summary>
        private async UniTask SavePlayerSetupToSupabaseAsync(string gameCode, bool isHost)
        {
            if (_gameSessionService == null || _playerSetupData == null)
            {
                Debug.LogWarning("[UIFlowController] Cannot save setup - service or data not available");
                return;
            }

            // Get current game state
            GameSession game = await _gameSessionService.GetGame(gameCode);
            if (game == null)
            {
                Debug.LogError($"[UIFlowController] Cannot save setup - game {gameCode} not found");
                return;
            }

            // Build player data from local setup (flat structure - no nested setupData)
            DLYHPlayerData playerData = new DLYHPlayerData
            {
                name = _playerSetupData.PlayerName,
                color = ColorToHex(_playerSetupData.PlayerColor),
                gridSize = _playerSetupData.GridSize,
                wordCount = _playerSetupData.WordCount,
                difficulty = _playerSetupData.DifficultyLevel.ToString(),
                ready = true,
                setupComplete = true,
                lastActivityAt = DateTime.UtcNow.ToString("o"),
                wordPlacementsEncrypted = GameStateManager.EncryptWordPlacements(_playerSetupData.PlacedWords),
                gameplayState = new DLYHGameplayState
                {
                    misses = 0,
                    missLimit = GameStateManager.CalculateMissLimit(_playerSetupData.GridSize, _playerSetupData.WordCount, _playerSetupData.DifficultyLevel)
                }
            };

            // Parse existing state (or create new one)
            DLYHGameState state = GameStateManager.ParseGameStateJson(game.StateJson) ?? new DLYHGameState
            {
                version = 1,
                status = "waiting",
                currentTurn = null,
                turnNumber = 0,
                winner = null
            };

            // Set player data based on host status
            if (isHost)
            {
                state.player1 = playerData;
            }
            else
            {
                state.player2 = playerData;
            }

            // Update game state in Supabase
            bool success = await _gameSessionService.UpdateGameState(gameCode, state);
            if (success)
            {
                Debug.Log($"[UIFlowController] Saved player setup to Supabase for game {gameCode}");
            }
            else
            {
                Debug.LogError($"[UIFlowController] Failed to save player setup for game {gameCode}");
            }
        }

        // NOTE: EncryptWordPlacements, DecryptWordPlacements moved to GameStateManager

        /// <summary>
        /// Saves the phantom AI's setup data to Supabase so the game can be resumed.
        /// Called after initializing a phantom AI opponent for online games.
        /// </summary>
        private async UniTask SavePhantomAISetupToSupabaseAsync(string gameCode, string aiName, Color aiColor,
            int gridSize, int wordCount, List<WordPlacementData> wordPlacements)
        {
            if (_gameSessionService == null)
            {
                Debug.LogWarning("[UIFlowController] Cannot save phantom AI setup - service not available");
                return;
            }

            // Get current game state
            GameSession game = await _gameSessionService.GetGame(gameCode);
            if (game == null)
            {
                Debug.LogError($"[UIFlowController] Cannot save phantom AI setup - game {gameCode} not found");
                return;
            }

            // Parse existing state
            DLYHGameState state = GameStateManager.ParseGameStateJson(game.StateJson);
            if (state == null)
            {
                Debug.LogError($"[UIFlowController] Cannot save phantom AI setup - failed to parse state for {gameCode}");
                return;
            }

            // Build phantom AI player data (player 2)
            DLYHPlayerData aiData = new DLYHPlayerData
            {
                name = aiName,
                color = ColorToHex(aiColor),
                gridSize = gridSize,
                wordCount = wordCount,
                difficulty = "Normal", // AI uses normal difficulty
                ready = true,
                setupComplete = true,
                lastActivityAt = DateTime.UtcNow.ToString("o"),
                wordPlacementsEncrypted = GameStateManager.EncryptWordPlacements(wordPlacements),
                gameplayState = new DLYHGameplayState
                {
                    misses = 0,
                    missLimit = GameStateManager.CalculateMissLimit(gridSize, wordCount, DifficultySetting.Normal)
                }
            };

            // Set as player 2
            state.player2 = aiData;

            // Both players are now ready - set game to playing and determine initial turn
            state.status = "playing";
            // Randomly determine first turn (like the local game does)
            bool player1First = UnityEngine.Random.value >= 0.5f;
            state.currentTurn = player1First ? "player1" : "player2";
            state.turnNumber = 1;
            state.updatedAt = DateTime.UtcNow.ToString("o");

            Debug.Log($"[UIFlowController] Setting game {gameCode} to playing, first turn: {state.currentTurn}");

            // Update game state in Supabase
            bool success = await _gameSessionService.UpdateGameState(gameCode, state);
            if (success)
            {
                Debug.Log($"[UIFlowController] Saved phantom AI setup to Supabase for game {gameCode}");
            }
            else
            {
                Debug.LogError($"[UIFlowController] Failed to save phantom AI setup for game {gameCode}");
            }
        }

        /// <summary>
        /// Saves the current gameplay state to Supabase for game persistence.
        /// Called after each turn to allow resuming games later.
        /// </summary>
        private async UniTask SaveGameStateToSupabaseAsync()
        {
            if (_gameSessionService == null || string.IsNullOrEmpty(_currentGameCode))
            {
                Debug.LogWarning("[UIFlowController] Cannot save game state - service or game code not available");
                return;
            }

            try
            {
                // Get current game state
                GameSession game = await _gameSessionService.GetGame(_currentGameCode);
                if (game == null)
                {
                    Debug.LogError($"[UIFlowController] Cannot save game state - game {_currentGameCode} not found");
                    return;
                }

                // Parse existing state
                DLYHGameState state = GameStateManager.ParseGameStateJson(game.StateJson);
                if (state == null)
                {
                    Debug.LogError($"[UIFlowController] Cannot save game state - failed to parse state for {_currentGameCode}");
                    return;
                }

                // Determine which player we are
                bool isPlayer1 = (_matchmakingResult?.IsHost ?? true);

                // Update gameplay state for player 1 (the local player's attacks on opponent)
                DLYHPlayerData myData = isPlayer1 ? state.player1 : state.player2;
                if (myData != null)
                {
                    // Initialize gameplay state if null
                    if (myData.gameplayState == null)
                    {
                        myData.gameplayState = new DLYHGameplayState();
                    }

                    // Update miss count from guess manager
                    myData.gameplayState.misses = _guessManager?.GetPlayerMissCount() ?? 0;
                    myData.gameplayState.missLimit = _guessManager?.GetPlayerMissLimit() ?? 0;

                    // Convert tracked data to arrays for serialization
                    myData.gameplayState.knownLetters = _playerGuessedLetters
                        .Select(c => c.ToString())
                        .ToArray();

                    myData.gameplayState.solvedWordRows = _playerSolvedWords.ToArray();

                    // Save player's incorrect word guesses (for Guessed Words panel on resume)
                    myData.gameplayState.incorrectWordGuesses = _allWordGuesses
                        .Where(g => g.isPlayer && !g.isCorrect)
                        .Select(g => g.word)
                        .ToArray();

                    // Convert revealed cells dictionary to array
                    myData.gameplayState.revealedCells = _playerRevealedCells
                        .Select(kvp => new RevealedCellData(
                            kvp.Key.y,  // row
                            kvp.Key.x,  // col
                            kvp.Value.letter == '\0' ? "" : kvp.Value.letter.ToString(),
                            kvp.Value.isHit))
                        .ToArray();

                    myData.lastActivityAt = DateTime.UtcNow.ToString("o");
                }

                // Update gameplay state for opponent (opponent's attacks on player's grid)
                DLYHPlayerData opponentData = isPlayer1 ? state.player2 : state.player1;
                if (opponentData != null)
                {
                    // Initialize gameplay state if null
                    if (opponentData.gameplayState == null)
                    {
                        opponentData.gameplayState = new DLYHGameplayState();
                    }

                    // Update opponent's miss count
                    opponentData.gameplayState.misses = _guessManager?.GetOpponentMissCount() ?? 0;
                    opponentData.gameplayState.missLimit = _guessManager?.GetOpponentMissLimit() ?? 0;

                    // Convert opponent revealed cells to array
                    opponentData.gameplayState.revealedCells = _opponentRevealedCells
                        .Select(kvp => new RevealedCellData(
                            kvp.Key.y,  // row
                            kvp.Key.x,  // col
                            kvp.Value.letter == '\0' ? "" : kvp.Value.letter.ToString(),
                            kvp.Value.isHit))
                        .ToArray();

                    // Save opponent's guessed letters (ALL letters they've guessed - both hits AND misses)
                    // This is needed for defense card keyboard restoration to show missed letters in red
                    HashSet<char> opponentGuessedLetters = _guessManager?.GetOpponentGuessedLetters() ?? new HashSet<char>();
                    opponentData.gameplayState.knownLetters = opponentGuessedLetters.Select(c => c.ToString()).ToArray();

                    // Save opponent's solved word indices (defense words they've correctly guessed)
                    opponentData.gameplayState.solvedWordRows = _opponentSolvedWords.ToArray();

                    // Save opponent's incorrect word guesses (for Guessed Words panel on resume)
                    opponentData.gameplayState.incorrectWordGuesses = _allWordGuesses
                        .Where(g => !g.isPlayer && !g.isCorrect)
                        .Select(g => g.word)
                        .ToArray();
                }

                // Update turn info
                state.currentTurn = _isPlayerTurn ? (isPlayer1 ? "player1" : "player2") : (isPlayer1 ? "player2" : "player1");
                state.turnNumber = state.turnNumber + 1;
                state.status = "playing";
                state.updatedAt = DateTime.UtcNow.ToString("o");

                // Save to Supabase
                bool success = await _gameSessionService.UpdateGameState(_currentGameCode, state);
                if (success)
                {
                    Debug.Log($"[UIFlowController] Saved game state to Supabase - turn {state.turnNumber}");
                }
                else
                {
                    Debug.LogError($"[UIFlowController] Failed to save game state for {_currentGameCode}");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[UIFlowController] Error saving game state: {ex.Message}");
            }
        }

        /// <summary>
        /// Converts a Color to hex string (#RRGGBB format).
        /// </summary>
        private string ColorToHex(Color color)
        {
            int r = Mathf.RoundToInt(color.r * 255);
            int g = Mathf.RoundToInt(color.g * 255);
            int b = Mathf.RoundToInt(color.b * 255);
            return $"#{r:X2}{g:X2}{b:X2}";
        }

        // NOTE: CalculateMissLimit moved to GameStateManager

        private async UniTask TransitionToGameplayFromSavedStateAsync(string gameCode, int myPlayerNumber,
            DLYHPlayerData myData, DLYHPlayerData opponentData, DLYHGameState gameState, bool isPhantomAI)
        {
            if (_gameplayManager == null)
            {
                Debug.LogError("[UIFlowController] GameplayManager not available");
                return;
            }

            // Parse colors
            Color myColor = ParseColorFromHex(myData.color) ?? new Color(0.2f, 0.6f, 0.8f);
            Color opponentColor = opponentData != null
                ? (ParseColorFromHex(opponentData.color) ?? new Color(0.6f, 0.1f, 0.1f))
                : new Color(0.4f, 0.4f, 0.4f); // Gray for waiting opponent

            // Get difficulty setting (flat structure - no nested setupData)
            DifficultySetting myDifficulty = GetDifficultySettingFromString(myData.difficulty ?? "Normal");

            // Get grid sizes and word counts
            int myGridSize = myData.gridSize > 0 ? myData.gridSize : 8;
            int myWordCount = myData.wordCount > 0 ? myData.wordCount : 5;

            int opponentGridSize = opponentData != null && opponentData.gridSize > 0 ? opponentData.gridSize : myGridSize;
            int opponentWordCount = opponentData != null && opponentData.wordCount > 0 ? opponentData.wordCount : myWordCount;

            // Use stored miss limits if available, otherwise calculate
            // Miss limits are stored in gameplayState and should be consistent across devices
            int myMissLimit = myData.gameplayState?.missLimit > 0
                ? myData.gameplayState.missLimit
                : DifficultyCalculator.CalculateMissLimitForPlayer(myDifficulty, opponentGridSize, opponentWordCount);

            int opponentMissLimit = opponentData?.gameplayState?.missLimit > 0
                ? opponentData.gameplayState.missLimit
                : DifficultyCalculator.CalculateMissLimitForPlayer(GetInverseDifficulty(myDifficulty), myGridSize, myWordCount);

            // Create player tab data
            PlayerTabData playerData = new PlayerTabData
            {
                Name = myData.name ?? "Player",
                Color = myColor,
                GridSize = myGridSize,
                WordCount = myWordCount,
                MissCount = myData.gameplayState?.misses ?? 0,
                MissLimit = myMissLimit,
                IsLocalPlayer = true
            };

            // Create opponent tab data
            string opponentName = opponentData?.name ?? "Waiting...";
            PlayerTabData opponentTabData = new PlayerTabData
            {
                Name = opponentName,
                Color = opponentColor,
                GridSize = opponentGridSize,
                WordCount = opponentWordCount,
                MissCount = opponentData?.gameplayState?.misses ?? 0,
                MissLimit = opponentMissLimit,
                IsLocalPlayer = false
            };

            _gameplayManager.SetPlayerData(playerData, opponentTabData);

            // Set turn based on game state
            bool isMyTurn = (gameState.currentTurn == "player1" && myPlayerNumber == 1) ||
                            (gameState.currentTurn == "player2" && myPlayerNumber == 2);
            _gameplayManager.SetPlayerTurn(isMyTurn);
            _isPlayerTurn = isMyTurn;
            _isGameOver = false;

            // Decrypt word placements from Supabase data (flat structure - no nested setupData)
            List<WordPlacementData> myPlacements = GameStateManager.DecryptWordPlacements(myData.wordPlacementsEncrypted);
            List<WordPlacementData> opponentPlacements = !string.IsNullOrEmpty(opponentData?.wordPlacementsEncrypted)
                ? GameStateManager.DecryptWordPlacements(opponentData.wordPlacementsEncrypted)
                : new List<WordPlacementData>();

            Debug.Log($"[UIFlowController] Resume: My placements: {myPlacements.Count}, Opponent placements: {opponentPlacements.Count}");

            // Set up player setup data BEFORE InitializeGuessManagerWithBothSides
            // The guess manager needs this to build the playerPlacedLetters dictionary for defense card restore
            _playerSetupData = new PlayerSetupData
            {
                PlayerName = myData.name ?? "Player",
                PlayerColor = myColor,
                GridSize = myGridSize,
                WordCount = myWordCount,
                DifficultyLevel = myDifficulty,
                PlacedWords = myPlacements
            };

            // Validate grid size matches placement coordinates
            int requiredMyGridSize = GetRequiredGridSizeForPlacements(myPlacements);
            int requiredOpponentGridSize = GetRequiredGridSizeForPlacements(opponentPlacements);

            if (requiredMyGridSize > myGridSize)
            {
                Debug.LogWarning($"[UIFlowController] Grid size mismatch! myGridSize={myGridSize} but placements require at least {requiredMyGridSize}. Using placement-based size.");
                myGridSize = requiredMyGridSize;
            }
            if (requiredOpponentGridSize > opponentGridSize)
            {
                Debug.LogWarning($"[UIFlowController] Grid size mismatch! opponentGridSize={opponentGridSize} but placements require at least {requiredOpponentGridSize}. Using placement-based size.");
                opponentGridSize = requiredOpponentGridSize;
            }

            // Create models from decrypted data (not from local _placementAdapter!)
            CreateAttackModel(opponentGridSize, opponentPlacements, opponentColor);
            CreateDefenseModelFromPlacements(myGridSize, myPlacements, myColor);
            SetupGameplayTableViews(opponentGridSize);

            // Initialize guess manager with opponent placements for attack side
            InitializeGuessManagerWithBothSides(myMissLimit, opponentMissLimit, opponentPlacements);

            // Set up word rows with both player and opponent placements
            SetupGameplayWordRowsFromSavedState(myWordCount, opponentWordCount, myColor, myPlacements, opponentPlacements);

            // Set status message and waiting state
            string statusMsg = isMyTurn ? "Your turn! Tap a letter or cell to attack." : "Opponent's turn...";
            if (opponentData == null)
            {
                statusMsg = "Waiting for opponent to join...";
                _waitingForOpponent = true;
                Debug.Log($"[UIFlowController] Resume: No opponent data, waiting for opponent to join game {gameCode}");
            }
            else
            {
                _waitingForOpponent = false;
            }
            _gameplayManager.SetStatusMessage(statusMsg, GameplayScreenManager.StatusType.Normal);

            // Clear turn queues
            _playerExtraTurnQueue.Clear();
            _opponentExtraTurnQueue.Clear();

            // Restore game state from saved gameplay data (Session 3)
            RestoreGameplayStateFromSaved(myData, opponentData, myColor, opponentColor);

            // Update guillotine based on restored miss counts
            int playerMissCount = myData.gameplayState?.misses ?? 0;
            int opponentMissCount = opponentData?.gameplayState?.misses ?? 0;
            _playerPreviousStage = GetStageFromMissCount(playerMissCount, myMissLimit);
            _opponentPreviousStage = GetStageFromMissCount(opponentMissCount, opponentMissLimit);

            // Set guillotine overlay to correct stage without animation
            _guillotineOverlayManager?.ResetGameOverState();
            if (_playerPreviousStage > 1)
            {
                _guillotineOverlayManager?.SetBladeStageImmediately(true, _playerPreviousStage);
            }
            if (_opponentPreviousStage > 1)
            {
                _guillotineOverlayManager?.SetBladeStageImmediately(false, _opponentPreviousStage);
            }

            // Store current game code for networking
            _currentGameCode = gameCode;

            // Mark that a game is now in progress
            _hasActiveGame = true;

            // Initialize AI opponent if this is a phantom AI game
            // Note: _playerSetupData was already set above for InitializeGuessManagerWithBothSides
            if (isPhantomAI && opponentData != null)
            {
                Debug.Log($"[UIFlowController] Resume: Initializing phantom AI opponent '{opponentName}'");

                // Initialize the AI opponent with opponent's grid settings
                await InitializeOpponentAsync(opponentGridSize, opponentWordCount, myDifficulty, myColor, opponentName);
            }

            // Show gameplay screen
            ShowGameplayScreen();

            // Force colors to be set on the TableView - SelectAttackTab may skip this if tab already active
            // This is critical for correct cell rendering after restore
            Debug.Log($"[UIFlowController] Setting TableView colors - myColor: RGB({myColor.r:F2}, {myColor.g:F2}, {myColor.b:F2}), opponentColor: RGB({opponentColor.r:F2}, {opponentColor.g:F2}, {opponentColor.b:F2})");
            _tableView?.SetPlayerColors(myColor, opponentColor);

            Debug.Log($"[UIFlowController] Resumed game {gameCode} - Turn: {(isMyTurn ? "mine" : "opponent")}, Status: {gameState.status}, PlayerMisses: {playerMissCount}, OpponentMisses: {opponentMissCount}, IsPhantomAI: {isPhantomAI}");

            // Session 4: Start opponent join polling if waiting for opponent
            if (_waitingForOpponent)
            {
                StartOpponentJoinPolling(gameCode);
            }

            // Handle turn state on resume
            if (!isMyTurn)
            {
                if (isPhantomAI && _opponent != null)
                {
                    // AI's turn - disable tab switching and trigger AI
                    _gameplayManager?.SetAllowManualTabSwitch(false);
                    Debug.Log($"[UIFlowController] Resume: AI's turn, triggering opponent turn");
                    await UniTask.Delay(500);
                    TriggerOpponentTurn();
                }
                else
                {
                    // Real multiplayer - allow tab switching while waiting for remote opponent
                    _gameplayManager?.SetAllowManualTabSwitch(true);
                    Debug.Log("[UIFlowController] Resume: Opponent's turn (multiplayer - tab switching enabled)");
                    // TODO Session 5: Start polling for opponent moves here
                }
            }
            else
            {
                // It's player's turn - enable everything
                _gameplayManager?.SetAllowManualTabSwitch(true);
            }
        }

        /// <summary>
        /// Calculates the minimum grid size required to contain the given word placements.
        /// Returns the minimum gridSize where all placement coordinates fit within 0 to gridSize-1.
        /// </summary>
        private int GetRequiredGridSizeForPlacements(List<WordPlacementData> placements)
        {
            if (placements == null || placements.Count == 0)
            {
                return 0;
            }

            int maxRow = 0;
            int maxCol = 0;

            foreach (WordPlacementData placement in placements)
            {
                for (int i = 0; i < placement.Word.Length; i++)
                {
                    int row = placement.StartRow + (i * placement.DirRow);
                    int col = placement.StartCol + (i * placement.DirCol);

                    if (row > maxRow) maxRow = row;
                    if (col > maxCol) maxCol = col;
                }
            }

            // Grid size must be at least max+1 (since coordinates are 0-indexed)
            return System.Math.Max(maxRow, maxCol) + 1;
        }

        /// <summary>
        /// Restores gameplay state from saved data - reconstructs revealed cells, keyboard states,
        /// word row reveals, and miss counts from the saved gameplayState.
        /// </summary>
        private void RestoreGameplayStateFromSaved(DLYHPlayerData myData, DLYHPlayerData opponentData,
            Color myColor, Color opponentColor)
        {
            Debug.Log($"[UIFlowController] RestoreGameplayStateFromSaved - cells: {myData?.gameplayState?.revealedCells?.Length ?? 0}, letters: {myData?.gameplayState?.knownLetters?.Length ?? 0}");

            // Clear tracking dictionaries before restoring
            _playerRevealedCells.Clear();
            _opponentRevealedCells.Clear();
            _playerGuessedLetters.Clear();
            _playerSolvedWords.Clear();
            _opponentSolvedWords.Clear();
            _allWordGuesses.Clear();

            // Build a set of keyboard-guessed letters for quick lookup during cell restoration
            HashSet<char> keyboardGuessedLetters = new HashSet<char>();
            if (myData?.gameplayState?.knownLetters != null)
            {
                foreach (string letterStr in myData.gameplayState.knownLetters)
                {
                    if (!string.IsNullOrEmpty(letterStr))
                    {
                        keyboardGuessedLetters.Add(char.ToUpper(letterStr[0]));
                    }
                }
            }

            // Restore player's revealed cells (my attacks on opponent's grid)
            // PASS 1: Populate _playerRevealedCells dictionary first (needed for allCoordsKnown checks)
            if (myData?.gameplayState?.revealedCells != null)
            {

                foreach (RevealedCellData cell in myData.gameplayState.revealedCells)
                {
                    Vector2Int pos = new Vector2Int(cell.col, cell.row);
                    char letter = string.IsNullOrEmpty(cell.letter) ? '\0' : cell.letter[0];
                    _playerRevealedCells[pos] = (letter, cell.isHit);
                }

                // PASS 2: Update attack grid visuals now that we have complete revealed cells data
                foreach (RevealedCellData cell in myData.gameplayState.revealedCells)
                {
                    char letter = string.IsNullOrEmpty(cell.letter) ? '\0' : cell.letter[0];

                    if (cell.isHit)
                    {
                        // Hit cell - check if the letter was also guessed via keyboard
                        bool letterWasKeyboardGuessed = letter != '\0' && keyboardGuessedLetters.Contains(char.ToUpper(letter));

                        if (letterWasKeyboardGuessed)
                        {
                            // Letter was keyboard-guessed AND coordinate was hit
                            // Check if ALL coordinates for this letter are in our revealed cells
                            // (Can't use GuessManager here since it doesn't have the restore data yet)
                            List<Vector2Int> letterPositions = _guessManager?.GetOpponentLetterPositions(letter) ?? new List<Vector2Int>();
                            bool allCoordsKnown = letterPositions.Count > 0;
                            foreach (Vector2Int pos in letterPositions)
                            {
                                if (!_playerRevealedCells.ContainsKey(pos))
                                {
                                    allCoordsKnown = false;
                                    break;
                                }
                            }
                            RevealGridCellFully(cell.col, cell.row, letter, allCoordsKnown);
                        }
                        else
                        {
                            // Coordinate hit but letter not yet keyboard-guessed - show as yellow (Revealed state)
                            MarkGridCellCoordinateHit(cell.col, cell.row);
                        }
                    }
                    else
                    {
                        // Miss - show the cell as miss
                        MarkGridCellMiss(cell.col, cell.row);
                    }
                }
            }

            // Restore player's guessed letters (keyboard)
            if (myData?.gameplayState?.knownLetters != null)
            {
                foreach (string letterStr in myData.gameplayState.knownLetters)
                {
                    if (!string.IsNullOrEmpty(letterStr))
                    {
                        char letter = char.ToUpper(letterStr[0]);
                        _playerGuessedLetters.Add(letter);

                        // Check if letter exists in opponent's words using GetOpponentLetterPositions
                        // (Can't use IsPlayerLetterHit because GuessManager doesn't know about previous guesses yet)
                        List<Vector2Int> letterPositions = _guessManager?.GetOpponentLetterPositions(letter) ?? new List<Vector2Int>();
                        bool isHit = letterPositions.Count > 0;

                        if (isHit)
                        {
                            // Check if all coordinates for this letter were coordinate-guessed
                            // Use revealed cells to determine if coords are known (since GuessManager doesn't have guess history)
                            bool allCoordsKnown = true;
                            foreach (Vector2Int pos in letterPositions)
                            {
                                if (!_playerRevealedCells.ContainsKey(pos))
                                {
                                    allCoordsKnown = false;
                                    break;
                                }
                            }

                            if (allCoordsKnown)
                            {
                                _gameplayManager?.SetKeyboardLetterState(letter, LetterKeyState.Hit);
                            }
                            else
                            {
                                _gameplayManager?.SetKeyboardLetterState(letter, LetterKeyState.Found);
                            }

                            // Reveal letter in attack word rows
                            if (allCoordsKnown)
                            {
                                _attackWordRows?.RevealLetterInAllWords(letter, myColor);
                            }
                            else
                            {
                                _attackWordRows?.RevealLetterAsFoundInAllWords(letter);
                            }
                        }
                        else
                        {
                            _gameplayManager?.SetKeyboardLetterState(letter, LetterKeyState.Miss);
                        }
                    }
                }
            }

            // Restore player solved word indices and add to guessed words panel
            if (myData?.gameplayState?.solvedWordRows != null)
            {
                string playerName = _playerService?.CurrentPlayerName ?? "You";

                foreach (int wordIndex in myData.gameplayState.solvedWordRows)
                {
                    _playerSolvedWords.Add(wordIndex);
                    _attackWordRows?.HideGuessButton(wordIndex);

                    // Get the actual word to add to guessed words panel
                    string solvedWord = _attackWordRows?.GetWord(wordIndex);
                    if (!string.IsNullOrEmpty(solvedWord))
                    {
                        _gameplayManager?.AddGuessedWord(playerName, solvedWord, true, true, myColor);
                        _allWordGuesses.Add((solvedWord.ToUpper(), true, true));
                    }
                }
            }

            // Restore player incorrect word guesses
            if (myData?.gameplayState?.incorrectWordGuesses != null)
            {
                string playerName = _playerService?.CurrentPlayerName ?? "You";

                foreach (string incorrectWord in myData.gameplayState.incorrectWordGuesses)
                {
                    if (!string.IsNullOrEmpty(incorrectWord))
                    {
                        _gameplayManager?.AddGuessedWord(playerName, incorrectWord, false, true, myColor);
                        _allWordGuesses.Add((incorrectWord.ToUpper(), true, false));
                    }
                }
            }

            // Restore opponent solved word indices (defense words they correctly guessed)
            if (opponentData?.gameplayState?.solvedWordRows != null)
            {
                string opponentName = _opponent?.OpponentName ?? "Opponent";

                foreach (int wordIndex in opponentData.gameplayState.solvedWordRows)
                {
                    _opponentSolvedWords.Add(wordIndex);

                    // Get the actual word from defense word rows
                    string solvedWord = _defenseWordRows?.GetWord(wordIndex);
                    if (!string.IsNullOrEmpty(solvedWord))
                    {
                        _gameplayManager?.AddGuessedWord(opponentName, solvedWord, true, false, opponentColor);
                        _allWordGuesses.Add((solvedWord.ToUpper(), false, true));
                    }
                }
            }

            // Restore opponent incorrect word guesses
            if (opponentData?.gameplayState?.incorrectWordGuesses != null)
            {
                string opponentName = _opponent?.OpponentName ?? "Opponent";

                foreach (string incorrectWord in opponentData.gameplayState.incorrectWordGuesses)
                {
                    if (!string.IsNullOrEmpty(incorrectWord))
                    {
                        _gameplayManager?.AddGuessedWord(opponentName, incorrectWord, false, false, opponentColor);
                        _allWordGuesses.Add((incorrectWord.ToUpper(), false, false));
                    }
                }
            }

            // =====================================================================
            // DEFENSE CARD RESTORATION
            // Simplified flow:
            // Phase 1: Collect ALL data first (known letters, known positions)
            // Phase 2: Apply ALL highlighting at the end
            // =====================================================================

            // PHASE 1: DATA COLLECTION
            // -----------------------

            // 1a. Collect opponent's known letters (from knownLetters - includes keyboard guesses AND word guesses)
            // These letters should appear on keyboard/tracker and word rows
            HashSet<char> opponentKnownLetters = new HashSet<char>();

            if (opponentData?.gameplayState?.knownLetters != null)
            {
                foreach (string letterStr in opponentData.gameplayState.knownLetters)
                {
                    if (!string.IsNullOrEmpty(letterStr))
                    {
                        opponentKnownLetters.Add(char.ToUpper(letterStr[0]));
                    }
                }
            }

            // 1b. Collect opponent's revealed positions (coordinate guesses)
            HashSet<Vector2Int> opponentRevealedPositions = new HashSet<Vector2Int>();
            Dictionary<Vector2Int, bool> opponentCellHitStatus = new Dictionary<Vector2Int, bool>();

            if (opponentData?.gameplayState?.revealedCells != null)
            {
                foreach (RevealedCellData cell in opponentData.gameplayState.revealedCells)
                {
                    Vector2Int pos = new Vector2Int(cell.col, cell.row);
                    char letter = string.IsNullOrEmpty(cell.letter) ? '\0' : char.ToUpper(cell.letter[0]);

                    // Track in local dictionary
                    _opponentRevealedCells[pos] = (letter, cell.isHit);
                    opponentCellHitStatus[pos] = cell.isHit;

                    if (cell.isHit)
                    {
                        opponentRevealedPositions.Add(pos);
                    }
                }
            }

            // 1c. For each known letter, determine if ALL its positions are coordinate-revealed
            Dictionary<char, bool> letterAllCoordsKnown = new Dictionary<char, bool>();
            Dictionary<char, List<Vector2Int>> letterPositionsCache = new Dictionary<char, List<Vector2Int>>();

            foreach (char letter in opponentKnownLetters)
            {
                List<Vector2Int> positions = _guessManager?.GetPlayerLetterPositions(letter) ?? new List<Vector2Int>();
                letterPositionsCache[letter] = positions;

                if (positions.Count > 0)
                {
                    bool allKnown = true;
                    foreach (Vector2Int pos in positions)
                    {
                        if (!opponentRevealedPositions.Contains(pos))
                        {
                            allKnown = false;
                            break;
                        }
                    }
                    letterAllCoordsKnown[letter] = allKnown;
                }
                else
                {
                    letterAllCoordsKnown[letter] = false; // Letter is a miss
                }
            }

            // PHASE 2: APPLY HIGHLIGHTING
            // ---------------------------
            // At this point, word rows already have ALL letters visible (from SetWord in setup).
            // Grid cells already have letters (from CreateDefenseModelFromPlacements).
            // Now we just need to apply the correct COLORS.

            // 2a. Highlight KEYBOARD/TRACKER (ONLY for keyboard-guessed letters, not coordinate-revealed)
            foreach (char letter in opponentKnownLetters)
            {
                List<Vector2Int> positions = letterPositionsCache.ContainsKey(letter) ? letterPositionsCache[letter] : new List<Vector2Int>();
                bool isHit = positions.Count > 0;
                bool allCoordsKnown = letterAllCoordsKnown.ContainsKey(letter) && letterAllCoordsKnown[letter];

                if (isHit)
                {
                    if (allCoordsKnown)
                    {
                        _gameplayManager?.MarkOpponentLetterHit(letter, opponentColor);
                    }
                    else
                    {
                        _gameplayManager?.MarkOpponentLetterFound(letter);
                    }
                }
                else
                {
                    _gameplayManager?.MarkOpponentLetterMiss(letter);
                }
            }

            // 2b. Highlight WORD ROWS
            // Known letters (from keyboard guesses OR word guesses) - yellow or opponent color
            foreach (char letter in opponentKnownLetters)
            {
                List<Vector2Int> positions = letterPositionsCache.ContainsKey(letter) ? letterPositionsCache[letter] : new List<Vector2Int>();
                bool isHit = positions.Count > 0;
                bool allCoordsKnown = letterAllCoordsKnown.ContainsKey(letter) && letterAllCoordsKnown[letter];

                if (isHit)
                {
                    if (allCoordsKnown)
                    {
                        _defenseWordRows?.RevealLetterInAllWords(letter, opponentColor);
                    }
                    else
                    {
                        _defenseWordRows?.RevealLetterAsFoundInAllWords(letter);
                    }
                }
                // Misses don't need highlighting in word rows - letter just stays without highlight
            }

            // 2c. Highlight GRID CELLS
            // Grid highlighting rules now match word rows/keyboard:
            // - Yellow (Found): coordinate was revealed AND letter was guessed, but NOT all coords for that letter known
            // - Yellow (Revealed): coordinate was revealed but letter NOT guessed
            // - Opponent Color (Hit): coordinate was revealed AND letter was guessed AND all coords for that letter known
            // First, handle coordinate-guessed cells (hits and misses)
            if (opponentData?.gameplayState?.revealedCells != null)
            {
                foreach (RevealedCellData cell in opponentData.gameplayState.revealedCells)
                {
                    char letter = string.IsNullOrEmpty(cell.letter) ? '\0' : char.ToUpper(cell.letter[0]);

                    if (cell.isHit)
                    {
                        // Check if the letter was guessed (is in opponentKnownLetters)
                        bool letterWasGuessed = letter != '\0' && opponentKnownLetters.Contains(letter);

                        if (letterWasGuessed)
                        {
                            // Letter was guessed - check if ALL coords for this letter are known
                            bool allCoordsKnown = letterAllCoordsKnown.ContainsKey(letter) && letterAllCoordsKnown[letter];
                            MarkDefenseGridCellHit(cell.col, cell.row, allCoordsKnown);
                        }
                        else
                        {
                            // Coordinate revealed but letter not guessed -> yellow (Revealed)
                            MarkDefenseGridCellRevealedByCoord(cell.col, cell.row);
                        }
                    }
                    else
                    {
                        MarkDefenseGridCellMiss(cell.col, cell.row);
                    }
                }
            }

            // NOTE: Grid cells are ONLY highlighted if the coordinate was guessed
            // If a letter is known but the coordinate wasn't guessed, the grid cell stays unhighlighted

            // Restore miss counts in guess manager
            if (_guessManager != null)
            {
                int playerMisses = myData?.gameplayState?.misses ?? 0;
                int opponentMisses = opponentData?.gameplayState?.misses ?? 0;

                // Set the initial miss counts (doesn't trigger game over checks)
                _guessManager.SetInitialMissCounts(playerMisses, opponentMisses);

                // Update the miss count display in the UI
                _gameplayManager?.SetPlayerMissCount(playerMisses, _guessManager.GetPlayerMissLimit());
                _gameplayManager?.SetOpponentMissCount(opponentMisses, _guessManager.GetOpponentMissLimit());

                // Restore opponent's guessed letters and coordinates to GuessManager
                // This ensures opponent guesses are tracked for subsequent saves
                _guessManager.RestoreOpponentGuessState(opponentKnownLetters, _opponentRevealedCells);
            }

            Debug.Log("[UIFlowController] Gameplay state restoration complete");
        }

        private Color? ParseColorFromHex(string hex)
        {
            if (string.IsNullOrEmpty(hex)) return null;

            // Remove # if present
            if (hex.StartsWith("#")) hex = hex.Substring(1);

            if (hex.Length != 6) return null;

            try
            {
                byte r = Convert.ToByte(hex.Substring(0, 2), 16);
                byte g = Convert.ToByte(hex.Substring(2, 2), 16);
                byte b = Convert.ToByte(hex.Substring(4, 2), 16);
                return new Color(r / 255f, g / 255f, b / 255f);
            }
            catch
            {
                return null;
            }
        }

        private DifficultySetting GetDifficultySettingFromString(string difficulty)
        {
            return difficulty?.ToLower() switch
            {
                "easy" => DifficultySetting.Easy,
                "hard" => DifficultySetting.Hard,
                _ => DifficultySetting.Normal
            };
        }

        // NOTE: HandleRemoveGame, HandleRemoveGameAsync, LoadHiddenGames, SaveHiddenGames,
        // AddToHiddenGames, RemoveFromHiddenGames, IsGameHidden moved to ActiveGamesManager

        private void OnSfxVolumeChanged(ChangeEvent<float> evt)
        {
            float volume = evt.newValue;
            PlayerPrefs.SetFloat(PREFS_SFX_VOLUME, volume);
            PlayerPrefs.Save();
            UpdateSfxLabel(volume);

            // Refresh audio manager cache
            if (UIAudioManager.Instance != null)
            {
                UIAudioManager.Instance.RefreshVolumeCache();
            }
        }

        private void OnMusicVolumeChanged(ChangeEvent<float> evt)
        {
            float volume = evt.newValue;
            PlayerPrefs.SetFloat(PREFS_MUSIC_VOLUME, volume);
            PlayerPrefs.Save();
            UpdateMusicLabel(volume);

            // Refresh music manager cache
            if (MusicManager.Instance != null)
            {
                MusicManager.Instance.RefreshVolumeCache();
            }
        }

        private void OnQwertyToggleChanged(ChangeEvent<bool> evt)
        {
            PlayerPrefs.SetInt(PREFS_QWERTY_KEYBOARD, evt.newValue ? 1 : 0);
            PlayerPrefs.Save();

            // Update keyboard layout if wizard is open
            RefreshKeyboardIfNeeded();
        }

        /// <summary>
        /// Refreshes the letter keyboard layout and re-wires the button handlers.
        /// Called when QWERTY preference changes.
        /// </summary>
        private void RefreshKeyboardIfNeeded()
        {
            // Refresh wizard keyboard if open
            if (_wizardManager != null)
            {
                // Rebuild the keyboard with new layout
                _wizardManager.RefreshKeyboardLayout();

                // Re-wire the new buttons
                _keyboardWiredUp = false;
                VisualElement keyboard = _setupWizardScreen?.Q<VisualElement>("letter-keyboard");
                if (keyboard != null)
                {
                    keyboard.Query<Button>(className: "letter-key").ForEach(keyButton =>
                    {
                        if (keyButton.ClassListContains("backspace-key"))
                        {
                            keyButton.clicked += HandleBackspacePressed;
                        }
                        else if (keyButton.text.Length == 1)
                        {
                            char letter = keyButton.text[0];
                            keyButton.clicked += () => HandleLetterKeyPressed(letter);
                        }
                    });
                    _keyboardWiredUp = true;
                }
            }

            // Refresh gameplay keyboard if it exists
            if (_gameplayManager != null)
            {
                bool useQwerty = PlayerPrefs.GetInt(PREFS_QWERTY_KEYBOARD, 0) == 1;
                _gameplayManager.SetQwertyLayout(useQwerty);
            }
        }

        private void UpdateSfxLabel(float volume)
        {
            if (_sfxValueLabel != null)
            {
                _sfxValueLabel.text = string.Format("{0:0}%", volume * 100f);
            }
        }

        private void UpdateMusicLabel(float volume)
        {
            if (_musicValueLabel != null)
            {
                _musicValueLabel.text = string.Format("{0:0}%", volume * 100f);
            }
        }

        private void StartTriviaRotation()
        {
            if (_triviaLabel == null) return;

            StopTriviaRotation();
            _currentTriviaIndex = UnityEngine.Random.Range(0, TRIVIA_FACTS.Length);
            _triviaCoroutine = StartCoroutine(TriviaRotationCoroutine());
        }

        private void StopTriviaRotation()
        {
            if (_triviaCoroutine != null)
            {
                StopCoroutine(_triviaCoroutine);
                _triviaCoroutine = null;
            }
        }

        private IEnumerator TriviaRotationCoroutine()
        {
            while (true)
            {
                // Set new trivia text
                if (_triviaLabel != null && TRIVIA_FACTS.Length > 0)
                {
                    _triviaLabel.text = TRIVIA_FACTS[_currentTriviaIndex];
                }

                // Fade in
                yield return FadeTriviaCoroutine(0f, 1f);

                // Wait for display duration
                yield return new WaitForSeconds(TRIVIA_DISPLAY_DURATION);

                // Fade out
                yield return FadeTriviaCoroutine(1f, 0f);

                // Move to next trivia (wrap around)
                _currentTriviaIndex = (_currentTriviaIndex + 1) % TRIVIA_FACTS.Length;
            }
        }

        private IEnumerator FadeTriviaCoroutine(float startAlpha, float endAlpha)
        {
            if (_triviaLabel == null) yield break;

            float elapsed = 0f;

            while (elapsed < TRIVIA_FADE_DURATION)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / TRIVIA_FADE_DURATION;
                float alpha = Mathf.Lerp(startAlpha, endAlpha, t);
                _triviaLabel.style.opacity = alpha;
                yield return null;
            }

            _triviaLabel.style.opacity = endAlpha;
        }

        private void CreateSetupWizardScreen()
        {
            _setupWizardScreen = new VisualElement();
            _setupWizardScreen.name = "setup-wizard-screen";
            _setupWizardScreen.style.flexGrow = 1;
            _setupWizardScreen.style.position = Position.Absolute;
            _setupWizardScreen.style.left = 0;
            _setupWizardScreen.style.right = 0;
            _setupWizardScreen.style.top = 0;
            _setupWizardScreen.style.bottom = 0;

            // Clone the setup wizard UXML
            if (_setupWizardUxml != null)
            {
                VisualElement wizardContent = _setupWizardUxml.CloneTree();
                // Make the TemplateContainer fill the parent
                wizardContent.style.flexGrow = 1;
                _setupWizardScreen.Add(wizardContent);
            }

            _root.Add(_setupWizardScreen);

            // Create the wizard UI manager (plain C# class, not MonoBehaviour)
            _wizardManager = new SetupWizardUIManager(_setupWizardScreen);
            _wizardManager.OnSetupComplete += HandleSetupComplete;
            _wizardManager.OnQuickSetupRequested += HandleQuickSetup;
            _wizardManager.OnJoinCodeSubmitted += HandleJoinCodeSubmitted;

            // Start hidden
            _setupWizardScreen.style.display = DisplayStyle.None;
        }

        private void CreateGameplayScreen()
        {
            _gameplayScreen = new VisualElement();
            _gameplayScreen.name = "gameplay-screen";
            _gameplayScreen.style.flexGrow = 1;
            _gameplayScreen.style.position = Position.Absolute;
            _gameplayScreen.style.left = 0;
            _gameplayScreen.style.right = 0;
            _gameplayScreen.style.top = 0;
            _gameplayScreen.style.bottom = 0;

            // Clone the gameplay UXML
            if (_gameplayUxml != null)
            {
                VisualElement gameplayContent = _gameplayUxml.CloneTree();
                gameplayContent.style.flexGrow = 1;
                gameplayContent.style.width = Length.Percent(100);
                gameplayContent.style.height = Length.Percent(100);
                _gameplayScreen.Add(gameplayContent);

                // Also ensure the inner gameplay-root fills its container
                VisualElement gameplayRoot = gameplayContent.Q<VisualElement>("gameplay-root");
                if (gameplayRoot != null)
                {
                    gameplayRoot.style.flexGrow = 1;
                    gameplayRoot.style.width = Length.Percent(100);
                    gameplayRoot.style.height = Length.Percent(100);
                }
            }

            // Add guillotine overlay on top (inside gameplay screen)
            if (_guillotineOverlayUxml != null)
            {
                VisualElement overlayContent = _guillotineOverlayUxml.CloneTree();
                // Position the overlay container absolutely so it covers the entire gameplay screen
                overlayContent.style.position = Position.Absolute;
                overlayContent.style.left = 0;
                overlayContent.style.right = 0;
                overlayContent.style.top = 0;
                overlayContent.style.bottom = 0;
                // Allow clicks to pass through the container to gameplay elements below
                overlayContent.pickingMode = PickingMode.Ignore;
                _gameplayScreen.Add(overlayContent);
            }

            _root.Add(_gameplayScreen);

            // Initialize gameplay manager
            _gameplayManager = new GameplayScreenManager();
            _gameplayManager.Initialize(_gameplayScreen);
            _gameplayManager.OnHamburgerClicked += HandleGameplayHamburgerClicked;
            _gameplayManager.OnLetterKeyClicked += HandleGameplayLetterClicked;
            _gameplayManager.OnGridCellClicked += HandleGameplayGridCellClicked;
            _gameplayManager.OnWordGuessClicked += HandleGameplayWordGuessClicked;
            _gameplayManager.OnShowGuillotineOverlay += ShowGuillotineOverlay;

            // Initialize guillotine overlay manager
            _guillotineOverlayManager = new GuillotineOverlayManager();
            _guillotineOverlayManager.Initialize(_gameplayScreen);
            _guillotineOverlayManager.OnClosed += HandleGuillotineOverlayClosed;

            // Set QWERTY preference
            bool useQwerty = PlayerPrefs.GetInt(PREFS_QWERTY_KEYBOARD, 0) == 1;
            _gameplayManager.SetQwertyLayout(useQwerty);

            // Start hidden
            _gameplayScreen.style.display = DisplayStyle.None;
        }

        private void ShowGameplayScreen()
        {
            _mainMenuScreen.style.display = DisplayStyle.None;
            _setupWizardScreen.style.display = DisplayStyle.None;
            _gameplayScreen.style.display = DisplayStyle.Flex;

            // Hide the shared hamburger button - gameplay has its own in the header bar
            HideHamburgerButton();
        }

        private void HandleGameplayHamburgerClicked()
        {
            ShowHamburgerOverlay();
        }

        private void HandleGameplayLetterClicked(char letter)
        {
            // Play keyboard click sound immediately for responsiveness
            DLYH.Audio.UIAudioManager.KeyboardClick();

            // Block input after game over
            if (_isGameOver)
            {
                Debug.Log("[UIFlowController] Input blocked - game is over");
                return;
            }

            // Block input during stage transition animation
            if (_isGamePausedForStageTransition)
            {
                Debug.Log("[UIFlowController] Input blocked - stage transition in progress");
                return;
            }

            // If in word guess mode, type the letter into the guess row instead
            if (_attackWordRows != null && _attackWordRows.IsInWordGuessMode)
            {
                _attackWordRows.TypeLetterInGuessMode(letter);
                return;
            }

            if (_guessManager == null || !_isPlayerTurn || _waitingForOpponent)
            {
                Debug.Log($"[UIFlowController] Cannot process letter - manager null, not player turn, or waiting for opponent");
                return;
            }

            // Capture pre-guess state (zero allocation - uses internal cache)
            _attackWordRows?.CapturePreGuessSnapshot();

            GuessResult result = _guessManager.ProcessPlayerLetterGuess(letter);

            // Track guessed letters for game state persistence (both hits and misses)
            if (result == GuessResult.Hit || result == GuessResult.Miss)
            {
                _playerGuessedLetters.Add(char.ToUpper(letter));
                Debug.Log($"[UIFlowController] Added letter '{char.ToUpper(letter)}' to _playerGuessedLetters, new count: {_playerGuessedLetters.Count}");
            }

            switch (result)
            {
                case GuessResult.Hit:
                    DLYH.Audio.UIAudioManager.Success();
                    // Keyboard state is set by HandleLetterHit event handler
                    // (Found/yellow if not all coords known, Hit/player color if all coords known)
                    _gameplayManager?.SetStatusMessage($"HIT! '{letter}' is in their words!", GameplayScreenManager.StatusType.Hit);
                    _opponent?.RecordPlayerGuess(true);
                    RefreshKeyboardLetterStates(); // Ensure keyboard colors are correct

                    // Check for newly completed words and queue extra turns
                    QueueExtraTurnsForCompletedWords(null, true);

                    CheckForPlayerWin();
                    ProcessPlayerTurnEnd();
                    break;

                case GuessResult.Miss:
                    DLYH.Audio.UIAudioManager.Error();
                    _gameplayManager?.SetKeyboardLetterState(letter, LetterKeyState.Miss);
                    _gameplayManager?.SetStatusMessage($"Miss! '{letter}' not in any word.", GameplayScreenManager.StatusType.Miss);
                    UpdateMissCountDisplay(true);
                    _opponent?.RecordPlayerGuess(false);
                    EndPlayerTurn(); // Miss always ends turn - no extra turn possible
                    break;

                case GuessResult.AlreadyGuessed:
                    _gameplayManager?.SetStatusMessage($"Already guessed '{letter}'", GameplayScreenManager.StatusType.Normal);
                    // Don't end turn - let player try again
                    break;
            }
        }

        private void HandleGameplayGridCellClicked(int tableRow, int tableCol, bool isAttackGrid)
        {
            // Play grid cell click sound immediately for responsiveness
            DLYH.Audio.UIAudioManager.GridCellClick();

            // Block input after game over
            if (_isGameOver)
            {
                Debug.Log("[UIFlowController] Input blocked - game is over");
                return;
            }

            // Block input during stage transition animation
            if (_isGamePausedForStageTransition)
            {
                Debug.Log("[UIFlowController] Input blocked - stage transition in progress");
                return;
            }

            if (_guessManager == null || !_isPlayerTurn || !isAttackGrid || _waitingForOpponent)
            {
                Debug.Log($"[UIFlowController] Cannot process coordinate - manager null, not player turn, not attack grid, or waiting for opponent");
                return;
            }

            // Convert from table coordinates to grid coordinates
            // For attack grid, use _attackTableLayout (opponent's grid)
            TableLayout layoutToUse = isAttackGrid ? _attackTableLayout : _tableLayout;
            if (layoutToUse == null) return;

            (int gridRow, int gridCol) = layoutToUse.TableToGrid(tableRow, tableCol);
            if (gridRow < 0 || gridCol < 0)
            {
                Debug.Log($"[UIFlowController] Click was outside grid area");
                return;
            }

            // GuessManager uses Vector2Int(col, row) format - PlacedLetters uses (col, row)
            GuessResult result = _guessManager.ProcessPlayerCoordinateGuess(gridCol, gridRow);

            // Format display coordinates with column letter (A=0, B=1, etc.)
            char colLetter = (char)('A' + gridCol);
            int displayRow = gridRow + 1; // 1-indexed for display

            switch (result)
            {
                case GuessResult.Hit:
                    DLYH.Audio.UIAudioManager.Success();
                    _gameplayManager?.SetStatusMessage($"HIT! Letter found at {colLetter}{displayRow}!", GameplayScreenManager.StatusType.Hit);
                    _opponent?.RecordPlayerGuess(true);
                    RefreshKeyboardLetterStates(); // Ensure keyboard colors are correct
                    CheckForPlayerWin();
                    EndPlayerTurn();
                    break;

                case GuessResult.Miss:
                    DLYH.Audio.UIAudioManager.Error();
                    _gameplayManager?.SetStatusMessage($"Miss at {colLetter}{displayRow}", GameplayScreenManager.StatusType.Miss);
                    UpdateMissCountDisplay(true);
                    _opponent?.RecordPlayerGuess(false);
                    EndPlayerTurn();
                    break;

                case GuessResult.AlreadyGuessed:
                    _gameplayManager?.SetStatusMessage($"Already guessed {colLetter}{displayRow}", GameplayScreenManager.StatusType.Normal);
                    // Don't end turn - let player try again
                    break;
            }
        }

        private void HandleGameplayWordGuessClicked(int wordIndex)
        {
            // This is now handled by the inline word guess mode on the word row itself.
            // The WordRowView's GUESS button now calls EnterWordGuessMode directly.
            // This callback is kept for backward compatibility but no longer used.
            Debug.Log($"[UIFlowController] HandleGameplayWordGuessClicked called for word {wordIndex} - now using inline mode");
        }

        private int _wordGuessTargetIndex = -1;
        private bool _isWordGuessMode = false;

        /// <summary>
        /// Called when inline word guess mode starts on a word row.
        /// </summary>
        private void HandleInlineWordGuessStarted(int wordIndex)
        {
            if (_guessManager == null || !_isPlayerTurn || _isGameOver || _waitingForOpponent)
            {
                Debug.Log("[UIFlowController] Cannot start word guess - not player's turn, game over, or waiting for opponent");
                _attackWordRows?.ExitWordGuessMode();
                return;
            }

            if (_guessManager.IsWordSolved(wordIndex))
            {
                Debug.Log($"[UIFlowController] Word {wordIndex} already solved");
                _attackWordRows?.ExitWordGuessMode();
                return;
            }

            _wordGuessTargetIndex = wordIndex;
            _isWordGuessMode = true;
            Debug.Log($"[UIFlowController] Inline word guess started for word {wordIndex}");
        }

        /// <summary>
        /// Called when inline word guess is cancelled.
        /// </summary>
        private void HandleInlineWordGuessCancelled(int wordIndex)
        {
            _wordGuessTargetIndex = -1;
            _isWordGuessMode = false;
            Debug.Log($"[UIFlowController] Inline word guess cancelled for word {wordIndex}");
        }

        /// <summary>
        /// Called when inline word guess is submitted.
        /// </summary>
        private void HandleInlineWordGuessSubmitted(int wordIndex, string guessedWord)
        {
            _wordGuessTargetIndex = -1;
            _isWordGuessMode = false;

            if (string.IsNullOrWhiteSpace(guessedWord))
            {
                Debug.Log("[UIFlowController] Word guess cancelled - empty input");
                return;
            }

            // Process the word guess
            GuessResult result = _guessManager.ProcessPlayerWordGuess(guessedWord, wordIndex);

            // Get player name for logging
            string playerName = _wizardManager?.PlayerName ?? "Player";

            switch (result)
            {
                case GuessResult.Hit:
                    DLYH.Audio.UIAudioManager.Success();
                    _gameplayManager?.SetStatusMessage($"Correct! '{guessedWord.ToUpper()}'", GameplayScreenManager.StatusType.Hit);
                    _gameplayManager?.AddGuessedWord(playerName, guessedWord.ToUpper(), true, true);

                    // Track for saving to Supabase
                    _allWordGuesses.Add((guessedWord.ToUpper(), true, true));
                    Debug.Log($"[UIFlowController] Added correct word guess '{guessedWord.ToUpper()}' to _allWordGuesses. Total count: {_allWordGuesses.Count}");

                    // Correct word guess always grants an extra turn
                    _playerExtraTurnQueue.Enqueue(wordIndex);
                    Debug.Log($"[UIFlowController] Correct word guess - queued extra turn for word {wordIndex}");

                    CheckForPlayerWin();
                    ProcessPlayerTurnEnd();
                    break;

                case GuessResult.Miss:
                    DLYH.Audio.UIAudioManager.Error();
                    _gameplayManager?.SetStatusMessage($"Wrong! '{guessedWord.ToUpper()}' +2 misses", GameplayScreenManager.StatusType.Miss);
                    _gameplayManager?.AddGuessedWord(playerName, guessedWord.ToUpper(), false, true);

                    // Track for saving to Supabase
                    _allWordGuesses.Add((guessedWord.ToUpper(), true, false));
                    Debug.Log($"[UIFlowController] Added incorrect word guess '{guessedWord.ToUpper()}' to _allWordGuesses. Total count: {_allWordGuesses.Count}");

                    EndPlayerTurn(); // Wrong guess always ends turn
                    break;

                case GuessResult.Invalid:
                    _gameplayManager?.SetStatusMessage($"'{guessedWord}' is not a valid word", GameplayScreenManager.StatusType.Normal);
                    // Re-enter guess mode for retry - don't log invalid words
                    _attackWordRows?.GetRow(wordIndex)?.EnterWordGuessMode();
                    return;

                case GuessResult.AlreadyGuessed:
                    _gameplayManager?.SetStatusMessage($"Already guessed '{guessedWord}'", GameplayScreenManager.StatusType.Normal);
                    // Don't re-enter guess mode for this - already logged
                    return;
            }
        }

        private void HandleWordGuessProcessed(int wordIndex, string guessedWord, bool wasCorrect)
        {
            Debug.Log($"[UIFlowController] Word guess processed: word {wordIndex}, '{guessedWord}', correct: {wasCorrect}");

            if (wasCorrect)
            {
                Color playerColor = _wizardManager?.PlayerColor ?? Color.cyan;

                // Reveal all letters in the solved word row AND in other rows
                // Each letter uses yellow or player color based on whether ALL coords for that letter are known
                foreach (char letter in guessedWord.ToUpper().Distinct())
                {
                    // IMPORTANT: Add letter to tracked guessed letters for save/restore
                    if (!_playerGuessedLetters.Contains(letter))
                    {
                        _playerGuessedLetters.Add(letter);
                        Debug.Log($"[UIFlowController] Word guess: Added '{letter}' to _playerGuessedLetters");
                    }
                    bool allCoordinatesKnown = _guessManager?.AreAllLetterCoordinatesKnown(letter) ?? false;

                    // Get positions for this letter in opponent's grid
                    List<Vector2Int> letterPositions = _guessManager?.GetOpponentLetterPositions(letter) ?? new List<Vector2Int>();

                    if (allCoordinatesKnown)
                    {
                        // All coordinates known for this letter - use player color
                        _attackWordRows?.RevealLetterInAllWords(letter, playerColor);
                        _gameplayManager?.MarkLetterHit(letter, playerColor);
                        Debug.Log($"[UIFlowController] Word guess: '{letter}' -> player color (all coords known)");

                        // Upgrade ALL grid cells with this letter to Hit (player color)
                        UpgradeAttackGridCellsToHit(letter);
                    }
                    else
                    {
                        // Not all coordinates known - use yellow/found color
                        _attackWordRows?.RevealLetterAsFoundInAllWords(letter);
                        _gameplayManager?.MarkLetterFound(letter);
                        Debug.Log($"[UIFlowController] Word guess: '{letter}' -> yellow (coords not all known)");
                    }

                    // Also upgrade any grid cells that are in Revealed (yellow, no letter) state to show the letter
                    // This ensures yellow cells get upgraded when letter is discovered via word guess
                    foreach (Vector2Int pos in letterPositions)
                    {
                        if (IsGridCellInState(pos.x, pos.y, TableCellState.Revealed))
                        {
                            RevealGridCellFully(pos.x, pos.y, letter, allCoordinatesKnown);
                            Debug.Log($"[UIFlowController] Word guess: Upgraded Revealed cell ({pos.x}, {pos.y}) to show '{letter}' (allCoordsKnown={allCoordinatesKnown})");
                        }
                    }
                }

                // Refresh all keyboard states to ensure correct colors
                RefreshKeyboardLetterStates();

                // Hide the guess button for this solved word
                _attackWordRows?.HideGuessButton(wordIndex);
            }
        }

        private void HandleWordSolved(int wordIndex)
        {
            Debug.Log($"[UIFlowController] Word {wordIndex} solved!");

            // Track solved word for game state persistence
            _playerSolvedWords.Add(wordIndex);

            // Hide the guess button for this word
            _attackWordRows?.HideGuessButton(wordIndex);

            // Check for player win
            CheckForPlayerWin();
        }

        #region Guess Manager Event Handlers

        private void HandleMissCountChanged(bool isPlayer, int newMissCount, int missLimit)
        {
            Debug.Log($"[UIFlowController] Miss count changed - isPlayer: {isPlayer}, count: {newMissCount}/{missLimit}");
            UpdateMissCountDisplay(isPlayer);

            // Check for stage transition
            int newStage = GetStageFromMissCount(newMissCount, missLimit);
            int previousStage = isPlayer ? _playerPreviousStage : _opponentPreviousStage;

            // Update tracked stage first
            if (isPlayer)
            {
                _playerPreviousStage = newStage;
            }
            else
            {
                _opponentPreviousStage = newStage;
            }

            if (newStage > previousStage)
            {
                // Stage increased - pause game and show guillotine overlay with animation
                Debug.Log($"[UIFlowController] Stage transition: {previousStage} -> {newStage} (isPlayer: {isPlayer}) - Pausing for animation");

                // Pause the game
                _isGamePausedForStageTransition = true;
                _wasPlayerTurnBeforePause = _isPlayerTurn;

                // Stop opponent turn timeout if running (we're paused)
                if (_opponentTurnTimeoutCoroutine != null)
                {
                    StopCoroutine(_opponentTurnTimeoutCoroutine);
                    _opponentTurnTimeoutCoroutine = null;
                }

                // Show guillotine overlay with blade at PREVIOUS position (for delayed animation)
                ShowGuillotineOverlayWithDelayedAnimation(isPlayer, previousStage);

                // After delay, animate blade to current position and play sound
                StartCoroutine(AnimateBladeAfterDelay(1.5f, isPlayer));
            }
        }

        private IEnumerator AnimateBladeAfterDelay(float delay, bool isPlayer)
        {
            yield return new WaitForSeconds(delay);

            // Animate blade and lever to current position
            _guillotineOverlayManager?.AnimateToCurrentStage(isPlayer);

            // Play blade raise sound
            DLYH.Audio.GuillotineAudioManager.BladeRaise();
        }

        private void HandleGuillotineOverlayClosed()
        {
            DLYH.Audio.UIAudioManager.PopupClose();

            // If we were paused for a stage transition, unpause and resume game
            if (_isGamePausedForStageTransition)
            {
                Debug.Log($"[UIFlowController] Guillotine overlay closed - Resuming game (wasPlayerTurn: {_wasPlayerTurnBeforePause})");
                _isGamePausedForStageTransition = false;

                // Resume the appropriate turn
                if (!_wasPlayerTurnBeforePause && !_isGameOver)
                {
                    // It was opponent's turn - restart their turn timer
                    _opponentTurnTimeoutCoroutine = StartCoroutine(OpponentTurnTimeoutCoroutine());
                }
            }
        }

        /// <summary>
        /// Converts miss count to stage number (1-5) matching GuillotineOverlayManager.
        /// Uses 25% increments during gameplay (stages 1-4), stage 5 only at execution (100%).
        /// </summary>
        private int GetStageFromMissCount(int missCount, int missLimit)
        {
            if (missLimit <= 0) return 1;
            float percent = Mathf.Clamp01((float)missCount / missLimit) * 100f;

            if (percent >= 100f) return 5;
            if (percent >= 75f) return 4;
            if (percent >= 50f) return 3;
            if (percent >= 25f) return 2;
            return 1;
        }

        private void HandleGameOver(bool playerLost)
        {
            Debug.Log($"[UIFlowController] GAME OVER - Player lost: {playerLost}");
            _isGameOver = true;

            // Stop all gameplay coroutines
            if (_turnDelayCoroutine != null)
            {
                StopCoroutine(_turnDelayCoroutine);
                _turnDelayCoroutine = null;
            }

            if (_opponentTurnTimeoutCoroutine != null)
            {
                StopCoroutine(_opponentTurnTimeoutCoroutine);
                _opponentTurnTimeoutCoroutine = null;
            }

            // Exit word guess mode if active
            _attackWordRows?.ExitWordGuessMode();

            // Re-enable tab switching so player can view both boards after game over
            _gameplayManager?.SetAllowManualTabSwitch(true);

            // Switch to Attack tab to show what the player didn't find
            _gameplayManager?.SelectAttackTab(isAutoSwitch: true);

            // Reveal any remaining unfound opponent words/positions
            RevealUnfoundOpponentWords();

            // Start game end sequence with guillotine animation
            _gameOverSequenceCoroutine = StartCoroutine(GameOverSequenceCoroutine(!playerLost));
        }

        /// <summary>
        /// Game end sequence coroutine - shows guillotine animation and then feedback modal.
        /// </summary>
        /// <param name="playerWon">True if player won, false if player lost</param>
        private IEnumerator GameOverSequenceCoroutine(bool playerWon)
        {
            // Animation timing constants (matching GuillotineDisplay.cs)
            const float PAUSE_BEFORE_EXECUTION = 2f;
            const float FINAL_RAISE_ANIMATION = 0.5f;    // Time for blade to animate from stage-4 to stage-5
            const float PAUSE_AT_TOP = 2f;               // Dramatic pause at peak before drop
            const float PAUSE_BEFORE_HOOK_UNLOCK = 0.3f;
            const float PAUSE_AFTER_HOOK_UNLOCK = 0.4f;
            const float BLADE_DROP_DURATION = 0.15f;
            const float HEAD_FALL_DURATION = 0.4f;
            const float PAUSE_AFTER_HEAD_FALL = 1.5f;

            // Show initial status message
            _gameplayManager?.SetPlayerTurn(true); // Reset turn indicator
            _gameplayManager?.SetStatusMessage(
                playerWon ? "YOU WIN!" : "GAME OVER",
                playerWon ? GameplayScreenManager.StatusType.Hit : GameplayScreenManager.StatusType.Miss
            );

            // Brief pause for players to see the result
            yield return new WaitForSeconds(PAUSE_BEFORE_EXECUTION);

            // Prepare guillotine data
            PlayerTabData playerTabData = _gameplayManager?.PlayerData;
            PlayerTabData opponentTabData = _gameplayManager?.OpponentData;

            GuillotineData playerData = new GuillotineData
            {
                Name = playerTabData?.Name ?? "You",
                Color = playerTabData?.Color ?? _wizardManager?.PlayerColor ?? ColorRules.SelectableColors[0],
                MissCount = playerTabData?.MissCount ?? 0,
                MissLimit = playerTabData?.MissLimit ?? 20,
                IsLocalPlayer = true
            };

            GuillotineData opponentData = new GuillotineData
            {
                Name = opponentTabData?.Name ?? "EXECUTIONER",
                Color = opponentTabData?.Color ?? ColorRules.SelectableColors[1],
                MissCount = opponentTabData?.MissCount ?? 0,
                MissLimit = opponentTabData?.MissLimit ?? 18,
                IsLocalPlayer = false
            };

            // Reset any lingering game over state from previous games
            _guillotineOverlayManager?.ResetGameOverState();

            // Show guillotine overlay in game over state (blade starts at stage-4 for loser)
            _guillotineOverlayManager?.ShowGameOver(playerData, opponentData, playerWon);

            // Wait one frame for the overlay to render with blade at stage-4 position
            // This ensures the user sees the starting position before the raise animation
            yield return null;

            // Play execution audio sequence
            // Part 1: Final raise - animate blade from stage-4 to stage-5 (peak position)
            DLYH.Audio.GuillotineAudioManager.FinalRaise();
            _guillotineOverlayManager?.TriggerFinalRaise(!playerWon);
            yield return new WaitForSeconds(FINAL_RAISE_ANIMATION);

            // Part 2: Dramatic pause at peak (blade fully raised, tension building)
            yield return new WaitForSeconds(PAUSE_AT_TOP);

            // Part 3: Hook unlock - lever drops first (releases the blade)
            yield return new WaitForSeconds(PAUSE_BEFORE_HOOK_UNLOCK);
            DLYH.Audio.GuillotineAudioManager.HookUnlock();
            _guillotineOverlayManager?.TriggerLeverDrop(!playerWon);
            yield return new WaitForSeconds(PAUSE_AFTER_HOOK_UNLOCK);

            // Part 4: Blade drop (chop) - blade falls after lever releases it
            DLYH.Audio.GuillotineAudioManager.FinalChop();
            _guillotineOverlayManager?.TriggerBladeDrop(!playerWon);
            yield return new WaitForSeconds(BLADE_DROP_DURATION);

            // Head falls - sync animation with audio
            DLYH.Audio.GuillotineAudioManager.HeadRemoved();
            _guillotineOverlayManager?.TriggerHeadFall(!playerWon);
            yield return new WaitForSeconds(HEAD_FALL_DURATION);

            // Pause to let player see the result
            yield return new WaitForSeconds(PAUSE_AFTER_HEAD_FALL);

            // Hide guillotine overlay
            _guillotineOverlayManager?.Hide();

            // Show feedback modal
            ShowFeedbackModal(
                playerWon ? "Victory!" : "Defeat!",
                isPostGame: true,
                playerWon: playerWon
            );

            _gameOverSequenceCoroutine = null;
        }

        #endregion

        #region Turn Management

        /// <summary>
        /// Ends the player's turn and switches to opponent's turn after a delay.
        /// Called when there are no extra turns to process.
        /// </summary>
        private void EndPlayerTurn()
        {
            if (_isGameOver) return;

            // Clear any remaining extra turns (shouldn't happen, but safety)
            _playerExtraTurnQueue.Clear();

            // Immediately block player input
            _isPlayerTurn = false;

            // Save game state to Supabase (fire and forget for performance)
            if (!string.IsNullOrEmpty(_currentGameCode) && _currentGameMode != GameMode.Solo)
            {
                Debug.Log($"[UIFlowController] EndPlayerTurn - Saving state for game {_currentGameCode}");
                SaveGameStateToSupabaseAsync().Forget();
            }
            else
            {
                Debug.Log($"[UIFlowController] EndPlayerTurn - Skip save (code={_currentGameCode}, mode={_currentGameMode})");
            }

            // Start turn switch with delay for UI feedback
            if (_turnDelayCoroutine != null)
            {
                StopCoroutine(_turnDelayCoroutine);
            }
            _turnDelayCoroutine = StartCoroutine(SwitchToOpponentTurnCoroutine());
        }

        /// <summary>
        /// Checks for newly completed words after a guess and queues extra turns.
        /// Uses internal pre-guess snapshot captured by CapturePreGuessSnapshot().
        /// </summary>
        /// <param name="preGuessSnapshot">Snapshot from before guess (pass null to use internal cache)</param>
        /// <param name="isPlayer">True if player, false if opponent</param>
        private void QueueExtraTurnsForCompletedWords(bool[] preGuessSnapshot, bool isPlayer)
        {
            WordRowsContainer wordRows = isPlayer ? _attackWordRows : _defenseWordRows;
            if (wordRows == null) return;

            // Pass null to use internal pre-guess snapshot (zero allocation)
            List<int> newlyCompleted = wordRows.GetNewlyCompletedWords(preGuessSnapshot);

            foreach (int wordIndex in newlyCompleted)
            {
                if (isPlayer)
                {
                    _playerExtraTurnQueue.Enqueue(wordIndex);
                    Debug.Log($"[UIFlowController] Player completed word {wordIndex} - queued extra turn");
                }
                else
                {
                    _opponentExtraTurnQueue.Enqueue(wordIndex);
                    Debug.Log($"[UIFlowController] Opponent completed word {wordIndex} - queued extra turn");
                }
            }
        }

        /// <summary>
        /// Processes the end of player's action - grants extra turn if queued, otherwise ends turn.
        /// </summary>
        private void ProcessPlayerTurnEnd()
        {
            if (_isGameOver) return;

            if (_playerExtraTurnQueue.Count > 0)
            {
                int completedWordIndex = _playerExtraTurnQueue.Dequeue();
                string completedWord = _attackWordRows?.GetActualWord(completedWordIndex) ?? $"Word {completedWordIndex + 1}";

                // Show extra turn message
                _gameplayManager?.SetStatusMessage($"EXTRA TURN! Completed '{completedWord}'", GameplayScreenManager.StatusType.Hit);
                Debug.Log($"[UIFlowController] Player extra turn granted for completing '{completedWord}'. {_playerExtraTurnQueue.Count} extra turns remaining.");

                // Player keeps their turn - don't call EndPlayerTurn
                // The turn indicator stays as player's turn
            }
            else
            {
                // No extra turns - end turn normally
                EndPlayerTurn();
            }
        }

        /// <summary>
        /// Processes the end of opponent's action - grants extra turn if queued, otherwise ends turn.
        /// </summary>
        private void ProcessOpponentTurnEnd()
        {
            if (_isGameOver) return;

            if (_opponentExtraTurnQueue.Count > 0)
            {
                int completedWordIndex = _opponentExtraTurnQueue.Dequeue();
                string completedWord = _defenseWordRows?.GetActualWord(completedWordIndex) ?? $"Word {completedWordIndex + 1}";

                // Show extra turn message
                string opponentName = _opponent?.OpponentName ?? "Opponent";
                _gameplayManager?.SetStatusMessage($"{opponentName} EXTRA TURN! Completed '{completedWord}'", GameplayScreenManager.StatusType.Miss);
                Debug.Log($"[UIFlowController] Opponent extra turn granted for completing '{completedWord}'. {_opponentExtraTurnQueue.Count} extra turns remaining.");

                // Cancel the old timeout coroutine - we'll start a new one after the delay
                if (_opponentTurnTimeoutCoroutine != null)
                {
                    StopCoroutine(_opponentTurnTimeoutCoroutine);
                    _opponentTurnTimeoutCoroutine = null;
                }

                // Opponent keeps their turn - trigger another opponent guess after a short delay
                // The delay is needed because the AI is still processing its previous turn (async)
                // and will reject ExecuteTurn calls while "thinking"
                StartCoroutine(TriggerOpponentExtraTurnCoroutine());
            }
            else
            {
                // No extra turns - end opponent's turn normally
                EndOpponentTurn();
            }
        }

        /// <summary>
        /// Triggers opponent extra turn after a short delay to let the AI finish processing.
        /// </summary>
        private IEnumerator TriggerOpponentExtraTurnCoroutine()
        {
            // Wait for the AI to finish its current async operation
            yield return new WaitForSeconds(OPPONENT_THINK_MIN_DELAY);

            if (_isGameOver) yield break;

            // Trigger the extra turn
            TriggerOpponentTurn();

            // Start a new timeout coroutine for the extra turn
            _opponentTurnTimeoutCoroutine = StartCoroutine(OpponentTurnTimeoutCoroutine());
        }

        /// <summary>
        /// Ends the opponent's turn and switches back to player's turn.
        /// Called when there are no extra turns to process.
        /// </summary>
        private void EndOpponentTurn()
        {
            if (_isGameOver) return;

            // Clear any remaining extra turns (shouldn't happen, but safety)
            _opponentExtraTurnQueue.Clear();

            // Cancel the timeout coroutine since opponent completed their turn
            if (_opponentTurnTimeoutCoroutine != null)
            {
                StopCoroutine(_opponentTurnTimeoutCoroutine);
                _opponentTurnTimeoutCoroutine = null;
            }

            // Save game state to Supabase after opponent's turn (fire and forget)
            if (!string.IsNullOrEmpty(_currentGameCode) && _currentGameMode != GameMode.Solo)
            {
                SaveGameStateToSupabaseAsync().Forget();
            }

            // Start turn switch with delay
            if (_turnDelayCoroutine != null)
            {
                StopCoroutine(_turnDelayCoroutine);
            }
            _turnDelayCoroutine = StartCoroutine(SwitchToPlayerTurnCoroutine());
        }

        private IEnumerator SwitchToOpponentTurnCoroutine()
        {
            yield return new WaitForSeconds(TURN_SWITCH_DELAY);

            if (_isGameOver) yield break;

            // _isPlayerTurn already set to false in EndPlayerTurn()
            _gameplayManager?.SetPlayerTurn(false);
            _gameplayManager?.SetStatusMessage("Opponent's turn...", GameplayScreenManager.StatusType.Normal);

            // Auto-switch to Defend tab
            _gameplayManager?.SelectDefendTab(isAutoSwitch: true);

            // For AI opponents: disable manual tab switching during their turn
            // For real multiplayer: keep tab switching enabled so player can view both tabs while waiting
            if (_opponent != null)
            {
                _gameplayManager?.SetAllowManualTabSwitch(false);
                Debug.Log("[UIFlowController] Switched to opponent's turn (AI - tab switching disabled)");

                yield return new WaitForSeconds(OPPONENT_THINK_MIN_DELAY);
                TriggerOpponentTurn();

                // Start a timeout coroutine as a safety net
                _opponentTurnTimeoutCoroutine = StartCoroutine(OpponentTurnTimeoutCoroutine());
            }
            else
            {
                // Real multiplayer - allow viewing both tabs while waiting for opponent
                _gameplayManager?.SetAllowManualTabSwitch(true);
                Debug.Log("[UIFlowController] Switched to opponent's turn (multiplayer - tab switching enabled, waiting for remote player)");
                // TODO Session 5: Start polling for opponent moves here
            }
        }

        /// <summary>
        /// Safety timeout in case opponent doesn't respond within expected time.
        /// Works for both AI and network opponents.
        /// </summary>
        private IEnumerator OpponentTurnTimeoutCoroutine()
        {
            yield return new WaitForSeconds(OPPONENT_TURN_TIMEOUT);

            // If we're still in opponent's turn after timeout, something went wrong
            if (!_isPlayerTurn && !_isGameOver)
            {
                Debug.LogWarning("[UIFlowController] Opponent turn timeout - forcing switch to player turn");
                EndOpponentTurn();
            }
        }

        private IEnumerator SwitchToPlayerTurnCoroutine()
        {
            yield return new WaitForSeconds(TURN_SWITCH_DELAY);

            if (_isGameOver) yield break;

            _isPlayerTurn = true;
            _gameplayManager?.SetPlayerTurn(true);
            _gameplayManager?.SetStatusMessage("Your turn! Tap a letter or cell.", GameplayScreenManager.StatusType.Normal);

            // Re-enable manual tab switching and auto-switch to Attack tab
            _gameplayManager?.SetAllowManualTabSwitch(true);
            _gameplayManager?.SelectAttackTab(isAutoSwitch: true);

            Debug.Log("[UIFlowController] Switched to player's turn (auto-switched to Attack tab)");
        }

        #endregion

        #region AI Opponent

        /// <summary>
        /// Initializes the AI opponent for solo mode gameplay.
        /// This must be called AFTER CapturePlayerSetupData() has populated _playerSetupData.
        /// </summary>
        private async UniTask InitializeOpponentAsync(int gridSize, int wordCount, DifficultySetting difficulty, Color playerColor, string phantomName = null)
        {
            if (_aiConfig == null)
            {
                Debug.LogWarning("[UIFlowController] No AI config assigned - AI opponent will not function");
                return;
            }

            if (_playerSetupData == null)
            {
                Debug.LogError("[UIFlowController] _playerSetupData is null - call CapturePlayerSetupData first!");
                return;
            }

            // Build word lists dictionary
            Dictionary<int, WordListSO> wordListDict = new Dictionary<int, WordListSO>();
            if (_wordLists != null)
            {
                foreach (WordListSO wordList in _wordLists)
                {
                    if (wordList != null && !wordListDict.ContainsKey(wordList.WordLength))
                    {
                        wordListDict[wordList.WordLength] = wordList;
                    }
                }
            }

            // Create AI opponent with optional phantom name
            _opponent = new LocalAIOpponent(_aiConfig, gameObject, wordListDict, phantomName);

            // Subscribe to AI events
            _opponent.OnLetterGuess += HandleOpponentLetterGuess;
            _opponent.OnCoordinateGuess += HandleOpponentCoordinateGuess;
            _opponent.OnWordGuess += HandleOpponentWordGuess;
            _opponent.OnThinkingStarted += HandleOpponentThinkingStarted;
            _opponent.OnThinkingComplete += HandleOpponentThinkingComplete;

            // Initialize AI with player's setup data (already captured with word placements)
            await _opponent.InitializeAsync(_playerSetupData);

            Debug.Log($"[UIFlowController] AI opponent initialized: {_opponent.OpponentName}, " +
                      $"Grid: {_opponent.GridSize}x{_opponent.GridSize}, Words: {_opponent.WordCount}");
        }

        /// <summary>
        /// Triggers the AI to take its turn.
        /// </summary>
        private void TriggerOpponentTurn()
        {
            if (_opponent == null)
            {
                Debug.LogError("[UIFlowController] TriggerOpponentTurn: _opponent is NULL!");
                return;
            }
            if (_isGameOver)
            {
                Debug.Log("[UIFlowController] TriggerOpponentTurn: Game is over, skipping");
                return;
            }
            if (_isPlayerTurn)
            {
                Debug.LogWarning("[UIFlowController] TriggerOpponentTurn: Still player's turn, skipping");
                return;
            }

            Debug.Log($"[UIFlowController] TriggerOpponentTurn: Triggering {_opponent.OpponentName}'s turn");

            // Build game state for AI decision making
            AIGameState gameState = BuildOpponentGameState();
            _opponent.ExecuteTurn(gameState);
        }

        /// <summary>
        /// Builds the current game state for AI decision making.
        /// Includes AI's tracked guesses and a word bank for pattern matching.
        /// </summary>
        private AIGameState BuildOpponentGameState()
        {
            AIGameState state = new AIGameState();

            int gridSize = _tableLayout?.GridSize ?? 6;
            state.GridSize = gridSize;
            state.WordCount = _playerSetupData?.WordCount ?? 4;

            // Get AI's guessed letters, coordinates, and words from guess manager
            // This prevents the AI from guessing the same thing twice
            if (_guessManager != null)
            {
                state.GuessedLetters = _guessManager.GetOpponentGuessedLetters();
                state.HitLetters = _guessManager.GetOpponentHitLetters();
                state.GuessedCoordinates = _guessManager.GetOpponentGuessedCoordinatesAsTuples();
                state.HitCoordinates = _guessManager.GetOpponentHitCoordinatesAsTuples();
                state.GuessedWords = _guessManager.GetOpponentGuessedWords();
            }
            else
            {
                state.GuessedLetters = new HashSet<char>();
                state.HitLetters = new HashSet<char>();
                state.GuessedCoordinates = new HashSet<(int, int)>();
                state.HitCoordinates = new HashSet<(int, int)>();
                state.GuessedWords = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            }

            // Build word patterns from player's word setup (what AI is trying to guess)
            // Patterns show known letters, e.g., "_A_E" for a 4-letter word with A and E known
            state.WordPatterns = BuildWordPatternsForOpponent();

            // Build WordsSolved list (parallel to WordPatterns) - AI tracks which words it has completely guessed
            state.WordsSolved = BuildWordsSolvedForOpponent();

            // Build word bank from available word lists (for AI pattern matching)
            state.WordBank = BuildWordBankForOpponent();

            // Calculate skill level based on difficulty
            state.SkillLevel = GetSkillLevelFromDifficulty(_playerSetupData?.DifficultyLevel ?? DifficultySetting.Normal);

            // Calculate fill ratio (letters / total cells)
            int totalCells = gridSize * gridSize;
            int letterCount = _playerSetupData?.PlacedWords?.Count > 0
                ? _playerSetupData.PlacedWords.Sum(w => w.Word?.Length ?? 0)
                : 0;
            state.FillRatio = totalCells > 0 ? (float)letterCount / totalCells : 0.3f;

            return state;
        }

        /// <summary>
        /// Builds word patterns showing what the AI knows about each player word.
        /// '_' = unknown letter, uppercase letter = known letter.
        /// </summary>
        private List<string> BuildWordPatternsForOpponent()
        {
            List<string> patterns = new List<string>();

            if (_playerSetupData?.PlacedWords == null || _guessManager == null)
                return patterns;

            foreach (var wordPlacement in _playerSetupData.PlacedWords)
            {
                if (string.IsNullOrEmpty(wordPlacement.Word)) continue;

                char[] pattern = new char[wordPlacement.Word.Length];
                for (int i = 0; i < wordPlacement.Word.Length; i++)
                {
                    char letter = char.ToUpper(wordPlacement.Word[i]);
                    // If AI has guessed this letter, show it in the pattern
                    if (_guessManager.HasOpponentGuessedLetter(letter) &&
                        _guessManager.IsOpponentLetterHit(letter))
                    {
                        pattern[i] = letter;
                    }
                    else
                    {
                        pattern[i] = '_';
                    }
                }
                patterns.Add(new string(pattern));
            }

            return patterns;
        }

        /// <summary>
        /// Builds a list of booleans indicating which player words the AI has fully solved.
        /// Must be parallel to WordPatterns (same index = same word).
        /// </summary>
        private List<bool> BuildWordsSolvedForOpponent()
        {
            List<bool> solved = new List<bool>();

            if (_playerSetupData?.PlacedWords == null)
                return solved;

            // For each player word, check if all letters have been guessed by the opponent
            foreach (var wordPlacement in _playerSetupData.PlacedWords)
            {
                if (string.IsNullOrEmpty(wordPlacement.Word))
                {
                    solved.Add(false);
                    continue;
                }

                // A word is "solved" if all its letters have been guessed by the opponent
                bool allLettersKnown = true;
                foreach (char letter in wordPlacement.Word)
                {
                    if (_guessManager != null && !_guessManager.HasOpponentGuessedLetter(letter))
                    {
                        allLettersKnown = false;
                        break;
                    }
                }
                solved.Add(allLettersKnown);
            }

            return solved;
        }

        /// <summary>
        /// Builds a word bank from available word lists for AI pattern matching.
        /// </summary>
        private HashSet<string> BuildWordBankForOpponent()
        {
            HashSet<string> wordBank = new HashSet<string>(System.StringComparer.OrdinalIgnoreCase);

            // Add words from all available word lists
            AddWordsFromList(_threeLetterWords, wordBank);
            AddWordsFromList(_fourLetterWords, wordBank);
            AddWordsFromList(_fiveLetterWords, wordBank);
            AddWordsFromList(_sixLetterWords, wordBank);

            return wordBank;
        }

        /// <summary>
        /// Adds words from a WordListSO to the word bank.
        /// </summary>
        private void AddWordsFromList(WordListSO wordList, HashSet<string> wordBank)
        {
            if (wordList == null || wordList.Words == null) return;

            foreach (string word in wordList.Words)
            {
                if (!string.IsNullOrEmpty(word))
                {
                    wordBank.Add(word.ToUpper());
                }
            }
        }

        /// <summary>
        /// Converts difficulty setting to skill level (0-1).
        /// </summary>
        private float GetSkillLevelFromDifficulty(DifficultySetting difficulty)
        {
            return difficulty switch
            {
                DifficultySetting.Easy => 0.3f,
                DifficultySetting.Normal => 0.5f,
                DifficultySetting.Hard => 0.8f,
                _ => 0.5f
            };
        }

        private void HandleOpponentLetterGuess(char letter)
        {
            if (_isGameOver || _isPlayerTurn) return;

            Debug.Log($"[UIFlowController] Opponent guesses letter: {letter}");

            // Capture pre-guess state (zero allocation - uses internal cache)
            _defenseWordRows?.CapturePreGuessSnapshot();

            // Process the guess against player's words
            bool wasHit = ProcessOpponentLetterGuess(letter);

            // Update UI to show opponent's guess
            string resultText = wasHit ? "HIT" : "MISS";
            _gameplayManager?.SetStatusMessage($"Opponent guessed '{letter}' - {resultText}!",
                wasHit ? GameplayScreenManager.StatusType.Hit : GameplayScreenManager.StatusType.Miss);

            // Get opponent color from IOpponent
            Color opponentColor = _opponent?.OpponentColor ?? _gameplayManager?.OpponentData?.Color ?? ColorRules.SelectableColors[1];

            // Update opponent's keyboard state (shown on Defend tab)
            if (wasHit)
            {
                _opponent?.RecordRevealedLetter(letter);

                // Check if all coordinates for this letter are now known by opponent
                bool allCoordsKnown = _guessManager?.AreAllPlayerLetterCoordinatesKnownByOpponent(letter) ?? false;

                // Reveal letter in defense word rows with appropriate color
                if (_defenseWordRows != null)
                {
                    if (allCoordsKnown)
                    {
                        _defenseWordRows.RevealLetterInAllWords(letter, opponentColor);
                    }
                    else
                    {
                        _defenseWordRows.RevealLetterAsFoundInAllWords(letter);
                    }
                }

                // NOTE: Letter guesses do NOT directly reveal grid cells!
                // Grid cells only get highlighted when coordinates are guessed.
                // If opponent already guessed coordinates containing this letter,
                // those cells should be updated with appropriate state based on allCoordsKnown.
                // Update grid cells that have been coordinate-guessed with this letter
                MarkDefenseGridLetterFound(letter, allCoordsKnown);

                // If all coords now known, also upgrade any cells that were previously Found to Hit
                if (allCoordsKnown)
                {
                    UpgradeDefenseGridLetterToHit(letter);
                }

                // Update opponent keyboard with appropriate color
                if (allCoordsKnown)
                {
                    _gameplayManager?.MarkOpponentLetterHit(letter, opponentColor);
                }
                else
                {
                    _gameplayManager?.MarkOpponentLetterFound(letter);
                }

                // Refresh all opponent keyboard states in case other letters are now fully known
                RefreshOpponentKeyboardStates();

                // Check for newly completed words and queue extra turns
                QueueExtraTurnsForCompletedWords(null, false);
            }
            else
            {
                _gameplayManager?.MarkOpponentLetterMiss(letter);
            }

            _opponent?.AdvanceTurn();

            // Check for opponent win (player loss)
            CheckForOpponentWin();

            if (!_isGameOver)
            {
                // Process turn end with extra turn logic
                if (wasHit)
                {
                    ProcessOpponentTurnEnd();
                }
                else
                {
                    // Miss always ends turn - no extra turn possible
                    EndOpponentTurn();
                }
            }
        }

        private void HandleOpponentCoordinateGuess(int row, int col)
        {
            if (_isGameOver || _isPlayerTurn) return;

            char colLetter = (char)('A' + col);
            int displayRow = row + 1;
            Debug.Log($"[UIFlowController] Opponent guesses coordinate: {colLetter}{displayRow}");

            // Process the guess against player's grid
            bool wasHit = ProcessOpponentCoordinateGuess(col, row);

            // Track opponent's revealed cell for game state persistence
            Vector2Int position = new Vector2Int(col, row);
            if (wasHit)
            {
                char? letterAtPos = _guessManager?.GetPlayerLetterAtPosition(col, row);
                _opponentRevealedCells[position] = (letterAtPos ?? '\0', true);
            }
            else
            {
                _opponentRevealedCells[position] = ('\0', false);
            }

            // Update UI
            string resultText = wasHit ? "HIT" : "MISS";
            _gameplayManager?.SetStatusMessage($"Opponent guessed {colLetter}{displayRow} - {resultText}!",
                wasHit ? GameplayScreenManager.StatusType.Hit : GameplayScreenManager.StatusType.Miss);

            // Update the defense grid visual
            if (wasHit)
            {
                _opponent?.RecordOpponentHit(row, col);

                // Get the letter at this position to determine if it's been guessed via keyboard
                char? letterAtPos = _guessManager?.GetPlayerLetterAtPosition(col, row);

                if (letterAtPos.HasValue && (_guessManager?.HasOpponentGuessedLetter(letterAtPos.Value) ?? false))
                {
                    // Letter was already guessed via keyboard
                    // Check if ALL coordinates for this letter are now known
                    bool allCoordsKnown = _guessManager?.AreAllPlayerLetterCoordinatesKnownByOpponent(letterAtPos.Value) ?? false;
                    MarkDefenseGridCellHit(col, row, allCoordsKnown);

                    // If all coords now known, upgrade all cells with this letter to Hit
                    if (allCoordsKnown)
                    {
                        UpgradeDefenseGridLetterToHit(letterAtPos.Value);
                    }
                }
                else
                {
                    // Letter not yet guessed - just coordinate revealed (yellow)
                    MarkDefenseGridCellRevealedByCoord(col, row);
                }

                // Check if this coordinate hit reveals any letters whose coords are now fully known
                RefreshOpponentKeyboardStates();
            }
            else
            {
                MarkDefenseGridCellMiss(col, row);
            }

            _opponent?.AdvanceTurn();

            // Check for opponent win (player loss)
            CheckForOpponentWin();

            if (!_isGameOver)
            {
                EndOpponentTurn();
            }
        }

        /// <summary>
        /// Refreshes opponent keyboard letter states based on current knowledge.
        /// Called after opponent guesses to update yellow -> opponent color transitions.
        /// Also updates defense grid cells and word rows accordingly.
        /// </summary>
        private void RefreshOpponentKeyboardStates()
        {
            if (_guessManager == null || _gameplayManager == null) return;

            Color opponentColor = _opponent?.OpponentColor ?? _gameplayManager.OpponentData?.Color ?? ColorRules.SelectableColors[1];

            // Get all unique letters in the player's words
            HashSet<char> playerLetters = _guessManager.GetAllPlayerLetters();

            // Check each letter that exists in the player's words
            foreach (char letter in playerLetters)
            {
                // Check if letter was guessed via keyboard (not just via coordinate)
                bool letterKnown = _guessManager.IsOpponentLetterHit(letter);
                // Check if all coordinates for this letter have been guessed by opponent
                bool allCoordsKnown = _guessManager.AreAllPlayerLetterCoordinatesKnownByOpponent(letter);

                // Debug: Log state for letters that opponent has guessed
                if (letterKnown)
                {
                    List<Vector2Int> positions = _guessManager.GetPlayerLetterPositions(letter);
                    Debug.Log($"[RefreshOpponentKeyboard] Letter '{letter}': letterKnown={letterKnown}, allCoordsKnown={allCoordsKnown}, positions={positions.Count}");
                }

                if (letterKnown && allCoordsKnown)
                {
                    // BOTH letter AND all coordinates known - upgrade to opponent color
                    _gameplayManager.MarkOpponentLetterHit(letter, opponentColor);
                    _defenseWordRows?.UpgradeLetterToPlayerColorInAllWords(letter, opponentColor);
                    UpgradeDefenseGridLetterToHit(letter);
                    Debug.Log($"[RefreshOpponentKeyboard] '{letter}' -> OPPONENT COLOR (both known)");
                }
                else if (letterKnown)
                {
                    // Letter guessed via keyboard but not all coords known - yellow
                    _gameplayManager.MarkOpponentLetterFound(letter);
                    _defenseWordRows?.RevealLetterAsFoundInAllWords(letter);
                    Debug.Log($"[RefreshOpponentKeyboard] '{letter}' -> YELLOW (letter known, coords incomplete)");
                }
                // If letter not guessed via keyboard, don't touch keyboard state
                // (grid cells may still show yellow for coordinate-only reveals)
            }
        }

        private bool ProcessOpponentLetterGuess(char letter)
        {
            // Process opponent's letter guess against player's defense grid
            GuessResult result = _guessManager?.ProcessOpponentLetterGuess(letter) ?? GuessResult.Invalid;
            return result == GuessResult.Hit;
        }

        private bool ProcessOpponentCoordinateGuess(int col, int row)
        {
            // Process opponent's coordinate guess against player's defense grid
            GuessResult result = _guessManager?.ProcessOpponentCoordinateGuess(col, row) ?? GuessResult.Invalid;
            return result == GuessResult.Hit;
        }

        private void HandleOpponentWordGuess(string guessedWord, int wordIndex)
        {
            if (_isGameOver || _isPlayerTurn) return;

            Debug.Log($"[UIFlowController] Opponent guesses word: '{guessedWord}' for word index {wordIndex}");

            // Record the word guess and check if already guessed
            bool isNewGuess = _guessManager?.RecordOpponentWordGuess(guessedWord) ?? false;
            if (!isNewGuess)
            {
                // Already guessed this word - skip processing, just advance turn
                Debug.Log($"[UIFlowController] Opponent already guessed '{guessedWord}' - skipping");
                _opponent?.AdvanceTurn();
                EndOpponentTurn();
                return;
            }

            // Get the player's actual word at this index
            string actualWord = null;
            if (_playerSetupData?.PlacedWords != null && wordIndex >= 0 && wordIndex < _playerSetupData.PlacedWords.Count)
            {
                actualWord = _playerSetupData.PlacedWords[wordIndex].Word?.ToUpper();
            }

            string normalizedGuess = guessedWord?.Trim().ToUpper() ?? "";
            bool wasCorrect = !string.IsNullOrEmpty(actualWord) && normalizedGuess == actualWord;

            Color opponentColor = _opponent?.OpponentColor ?? _gameplayManager?.OpponentData?.Color ?? ColorRules.SelectableColors[1];

            if (wasCorrect)
            {
                // CORRECT word guess!
                Debug.Log($"[UIFlowController] Opponent correctly guessed word '{normalizedGuess}'!");
                _gameplayManager?.SetStatusMessage($"Opponent guessed '{normalizedGuess}' - CORRECT!",
                    GameplayScreenManager.StatusType.Hit);

                // Track solved word index for saving to Supabase
                _opponentSolvedWords.Add(wordIndex);
                _allWordGuesses.Add((normalizedGuess, false, true));
                Debug.Log($"[UIFlowController] Added opponent correct word guess '{normalizedGuess}' to _allWordGuesses. Total count: {_allWordGuesses.Count}");

                // Track guessed word (correct - shown with opponent color background)
                string opponentName = _opponent?.OpponentName ?? "Opponent";
                _gameplayManager?.AddGuessedWord(opponentName, normalizedGuess, true, false);

                // Word guess reveals letters - add to known letters and update keyboard/tracker + word rows
                foreach (char letter in actualWord)
                {
                    _opponent?.RecordRevealedLetter(letter);

                    // Process letter as guessed - adds to known letters for saving
                    _guessManager?.ProcessOpponentLetterGuess(letter);

                    // Check if all coordinates for this letter are now known
                    bool allCoordsKnown = _guessManager?.AreAllPlayerLetterCoordinatesKnownByOpponent(letter) ?? false;

                    if (allCoordsKnown)
                    {
                        // Letter known AND all coords known -> player color
                        _gameplayManager?.MarkOpponentLetterHit(letter, opponentColor);
                        _defenseWordRows?.RevealLetterInAllWords(letter, opponentColor);
                    }
                    else
                    {
                        // Letter known but NOT all coords known -> yellow
                        _gameplayManager?.MarkOpponentLetterFound(letter);
                        _defenseWordRows?.RevealLetterAsFoundInAllWords(letter);
                    }
                }

                // Word guess does NOT directly reveal grid cells
                // Grid cells only show what was coordinate-guessed
                // Update cells that were already coordinate-guessed with appropriate state
                if (_playerSetupData?.PlacedWords != null && wordIndex < _playerSetupData.PlacedWords.Count)
                {
                    WordPlacementData wordData = _playerSetupData.PlacedWords[wordIndex];
                    for (int i = 0; i < wordData.Word.Length; i++)
                    {
                        int cellCol = wordData.StartCol + (i * wordData.DirCol);
                        int cellRow = wordData.StartRow + (i * wordData.DirRow);
                        char letterAtCell = char.ToUpper(wordData.Word[i]);

                        // Only update cells that were already coordinate-guessed (in _opponentRevealedCells)
                        Vector2Int position = new Vector2Int(cellCol, cellRow);
                        if (_opponentRevealedCells.ContainsKey(position) && _opponentRevealedCells[position].isHit)
                        {
                            // Cell was coordinate-guessed - update with appropriate state based on allCoordsKnown
                            bool allCoordsKnown = _guessManager?.AreAllPlayerLetterCoordinatesKnownByOpponent(letterAtCell) ?? false;
                            MarkDefenseGridCellHit(cellCol, cellRow, allCoordsKnown);
                        }

                        _opponent?.RecordOpponentHit(cellRow, cellCol);
                    }
                }

                // Refresh keyboard states to update any letters that now have all coords known
                RefreshOpponentKeyboardStates();

                // Correct word guess always grants opponent an extra turn
                _opponentExtraTurnQueue.Enqueue(wordIndex);
                Debug.Log($"[UIFlowController] Opponent correct word guess - queued extra turn for word {wordIndex}");
            }
            else
            {
                // WRONG word guess - +2 misses for opponent
                Debug.Log($"[UIFlowController] Opponent incorrectly guessed word '{normalizedGuess}' (actual: {actualWord ?? "unknown"})");
                _gameplayManager?.SetStatusMessage($"Opponent guessed '{normalizedGuess}' - WRONG! (+2 misses)",
                    GameplayScreenManager.StatusType.Miss);

                // Track incorrect word guess for saving to Supabase
                _allWordGuesses.Add((normalizedGuess, false, false));
                Debug.Log($"[UIFlowController] Added opponent incorrect word guess '{normalizedGuess}' to _allWordGuesses. Total count: {_allWordGuesses.Count}");

                // Track guessed word (incorrect - shown with red background)
                string opponentName = _opponent?.OpponentName ?? "Opponent";
                _gameplayManager?.AddGuessedWord(opponentName, normalizedGuess, false, false);

                // Add 2 misses to opponent
                _guessManager?.AddOpponentMisses(2);

                // Update miss display
                UpdateMissCountDisplay(false);
            }

            _opponent?.AdvanceTurn();

            // Check for opponent win (player loss)
            CheckForOpponentWin();

            if (!_isGameOver)
            {
                // Process turn end with extra turn logic
                if (wasCorrect)
                {
                    ProcessOpponentTurnEnd();
                }
                else
                {
                    // Wrong guess always ends turn
                    EndOpponentTurn();
                }
            }
        }

        /// <summary>
        /// Marks a cell on the defense grid with the appropriate state based on whether all coords for the letter are known.
        /// Uses Found (yellow) if not all coords known, Hit (opponent color) if all coords known.
        /// </summary>
        /// <param name="allCoordsKnown">True if all coordinates for this letter are known by opponent</param>
        private void MarkDefenseGridCellHit(int gridCol, int gridRow, bool allCoordsKnown = true)
        {
            if (_defenseTableModel == null || _tableLayout == null) return;

            (int tableRow, int tableCol) = _tableLayout.GridToTable(gridRow, gridCol);

            // Use Hit (opponent color) only if ALL coordinates for this letter are known
            // Otherwise use Found (yellow with letter shown)
            TableCellState state = allCoordsKnown ? TableCellState.Hit : TableCellState.Found;
            _defenseTableModel.SetCellState(tableRow, tableCol, state);

            if (allCoordsKnown)
            {
                // Set owner to Opponent so it shows in opponent color when hit
                _defenseTableModel.SetCellOwner(tableRow, tableCol, CellOwner.Opponent);
            }

            Debug.Log($"[UIFlowController] Defense grid cell ({gridCol}, {gridRow}) marked as {state} (allCoordsKnown={allCoordsKnown})");
        }

        /// <summary>
        /// Marks a cell on the defense grid as revealed by coordinate guess (yellow).
        /// Coordinate is known but letter hasn't been guessed yet.
        /// </summary>
        private void MarkDefenseGridCellRevealedByCoord(int gridCol, int gridRow)
        {
            if (_defenseTableModel == null || _tableLayout == null) return;

            (int tableRow, int tableCol) = _tableLayout.GridToTable(gridRow, gridCol);

            // Only set to Revealed if not already Hit (letter was already guessed)
            TableCell cell = _defenseTableModel.GetCell(tableRow, tableCol);
            if (cell.State != TableCellState.Hit)
            {
                _defenseTableModel.SetCellState(tableRow, tableCol, TableCellState.Revealed);
                // Revealed cells are yellow, no owner color needed
                Debug.Log($"[UIFlowController] Defense grid cell ({gridCol}, {gridRow}) marked as REVEALED (coord known, letter unknown)");
            }
        }

        /// <summary>
        /// Marks a cell on the defense grid as missed by opponent (shows red).
        /// </summary>
        private void MarkDefenseGridCellMiss(int gridCol, int gridRow)
        {
            if (_defenseTableModel == null || _tableLayout == null) return;

            (int tableRow, int tableCol) = _tableLayout.GridToTable(gridRow, gridCol);
            _defenseTableModel.SetCellState(tableRow, tableCol, TableCellState.Miss);

            Debug.Log($"[UIFlowController] Defense grid cell ({gridCol}, {gridRow}) marked as MISS");
        }

        /// <summary>
        /// Marks a cell on the defense grid as "found" by opponent (yellow - letter known but coord not guessed).
        /// </summary>
        private void MarkDefenseGridCellFound(int gridCol, int gridRow)
        {
            if (_defenseTableModel == null || _tableLayout == null) return;

            (int tableRow, int tableCol) = _tableLayout.GridToTable(gridRow, gridCol);
            // Only mark as Revealed if it's not already Hit (coordinate was guessed)
            TableCell cell = _defenseTableModel.GetCell(tableRow, tableCol);
            if (cell.State != TableCellState.Hit)
            {
                _defenseTableModel.SetCellState(tableRow, tableCol, TableCellState.Revealed);
                // Don't set owner - yellow cells don't have owner color
                Debug.Log($"[UIFlowController] Defense grid cell ({gridCol}, {gridRow}) marked as FOUND (yellow)");
            }
        }

        /// <summary>
        /// Updates defense grid cells when opponent guesses a letter.
        /// - If coordinate was already guessed AND all coords for letter known: upgrade to Hit (opponent color)
        /// - If coordinate was already guessed but not all coords known: mark as Found (yellow with letter)
        /// - If coordinate not yet guessed: leave as is (letter is shown but no highlight)
        /// </summary>
        private void MarkDefenseGridLetterFound(char letter, bool allCoordsKnown)
        {
            if (_guessManager == null) return;

            List<Vector2Int> positions = _guessManager.GetPlayerLetterPositions(letter);
            foreach (Vector2Int pos in positions)
            {
                if (_guessManager.HasOpponentGuessedCoordinate(pos.x, pos.y))
                {
                    // Coordinate was guessed - mark with appropriate state based on allCoordsKnown
                    MarkDefenseGridCellHit(pos.x, pos.y, allCoordsKnown);
                }
                // If coordinate not yet guessed, the cell stays as Normal (letters are always visible on defense grid)
            }
        }

        /// <summary>
        /// Upgrades defense grid cells with a specific letter from "found" (yellow) to "hit" (opponent color).
        /// Called when all coordinates for this letter are now known by opponent.
        /// </summary>
        private void UpgradeDefenseGridLetterToHit(char letter)
        {
            if (_guessManager == null || _defenseTableModel == null || _tableLayout == null) return;

            List<Vector2Int> positions = _guessManager.GetPlayerLetterPositions(letter);
            foreach (Vector2Int pos in positions)
            {
                // Only upgrade if opponent has actually guessed this coordinate
                if (_guessManager.HasOpponentGuessedCoordinate(pos.x, pos.y))
                {
                    (int tableRow, int tableCol) = _tableLayout.GridToTable(pos.y, pos.x);
                    TableCell cell = _defenseTableModel.GetCell(tableRow, tableCol);
                    TableCellState currentState = cell.State;

                    // Only upgrade cells that are in Found or Revealed state
                    if (currentState == TableCellState.Found || currentState == TableCellState.Revealed)
                    {
                        _defenseTableModel.SetCellState(tableRow, tableCol, TableCellState.Hit);
                        _defenseTableModel.SetCellOwner(tableRow, tableCol, CellOwner.Opponent);
                        Debug.Log($"[UIFlowController] Upgraded defense grid cell ({pos.x}, {pos.y}) from {currentState} to Hit for letter '{letter}'");
                    }
                }
            }
        }

        private void HandleOpponentThinkingStarted()
        {
            Debug.Log("[UIFlowController] Opponent is thinking...");
            _gameplayManager?.SetStatusMessage("Opponent is thinking...", GameplayScreenManager.StatusType.Normal);
        }

        private void HandleOpponentThinkingComplete()
        {
            Debug.Log("[UIFlowController] Opponent finished thinking");
        }

        #endregion

        #region Win/Lose Detection

        /// <summary>
        /// Checks if the player has won.
        /// Win condition: All letters in word rows revealed AND all coordinates found on grid.
        /// </summary>
        private void CheckForPlayerWin()
        {
            if (_isGameOver || _guessManager == null) return;

            // Check win condition: all letters known AND all coordinates known
            if (_guessManager.HasPlayerWon())
            {
                int totalWords = _guessManager.GetTotalWordCount();
                Debug.Log($"[UIFlowController] PLAYER WINS - All {totalWords} words complete and all coordinates found!");

                TriggerPlayerWin("YOU WIN! All words found!");
            }
        }

        /// <summary>
        /// Triggers player victory.
        /// </summary>
        private void TriggerPlayerWin(string message)
        {
            if (_isGameOver) return;

            _isGameOver = true;

            if (_turnDelayCoroutine != null)
            {
                StopCoroutine(_turnDelayCoroutine);
                _turnDelayCoroutine = null;
            }

            if (_opponentTurnTimeoutCoroutine != null)
            {
                StopCoroutine(_opponentTurnTimeoutCoroutine);
                _opponentTurnTimeoutCoroutine = null;
            }

            // Re-enable tab switching so player can view both boards after game over
            _gameplayManager?.SetAllowManualTabSwitch(true);

            // Switch to Attack tab to show the revealed opponent words
            _gameplayManager?.SelectAttackTab(isAutoSwitch: true);

            Debug.Log($"[UIFlowController] Game Over - Player Wins: {message}");

            // Reveal any remaining unfound opponent words/positions
            RevealUnfoundOpponentWords();

            // Start game end sequence with guillotine animation (player won)
            _gameOverSequenceCoroutine = StartCoroutine(GameOverSequenceCoroutine(true));
        }

        /// <summary>
        /// Checks if the AI has won by revealing all player's letters.
        /// </summary>
        private void CheckForOpponentWin()
        {
            if (_isGameOver || _guessManager == null) return;

            // Check win condition: opponent has found all player's letters AND all coordinates
            if (_guessManager.HasOpponentWon())
            {
                Debug.Log("[UIFlowController] OPPONENT WINS - All player words found!");
                HandleGameOver(true); // true = player lost
            }
        }

        /// <summary>
        /// Reveals all unfound words and positions on the attack grid at game end.
        /// Shows what the player didn't find in a neutral "reveal" color (grey/white).
        /// </summary>
        private void RevealUnfoundOpponentWords()
        {
            if (_opponentWordPlacements == null || _attackWordRows == null) return;

            Debug.Log("[UIFlowController] Revealing unfound opponent words at game end...");

            // Define reveal color - a neutral grey/white to distinguish from gameplay colors
            Color revealColor = new Color(0.7f, 0.7f, 0.7f, 1f); // Light grey

            // 1. Reveal all remaining letters in word rows
            for (int i = 0; i < _opponentWordPlacements.Count && i < _attackWordRows.WordCount; i++)
            {
                WordRowView row = _attackWordRows.GetRow(i);
                if (row != null && !row.IsFullyRevealed())
                {
                    row.RevealAllLetters(revealColor);
                    row.HideGuessButton(); // Hide GUESS button for revealed words
                }
            }

            // 2. Reveal all remaining grid cells that weren't found
            int totalCellsRevealed = 0;
            foreach (WordPlacementData wordData in _opponentWordPlacements)
            {
                int col = wordData.StartCol;
                int row = wordData.StartRow;

                for (int i = 0; i < wordData.Word.Length; i++)
                {
                    char letter = char.ToUpper(wordData.Word[i]);

                    // Check if this coordinate was already guessed
                    bool wasGuessed = _guessManager?.HasPlayerGuessedCoordinate(col, row) ?? false;

                    if (!wasGuessed)
                    {
                        // Cell was never guessed - reveal it with the letter
                        RevealGridCellAtEndGame(col, row, letter, revealColor);
                        totalCellsRevealed++;
                    }

                    col += wordData.DirCol;
                    row += wordData.DirRow;
                }
            }
            Debug.Log($"[UIFlowController] End-game reveal: {totalCellsRevealed} grid cells revealed");

            Debug.Log("[UIFlowController] End-game reveal complete");
        }

        /// <summary>
        /// Reveals a grid cell at end of game (unfound cell).
        /// </summary>
        private void RevealGridCellAtEndGame(int gridCol, int gridRow, char letter, Color revealColor)
        {
            if (_attackTableModel == null || _attackTableLayout == null) return;

            (int tableRow, int tableCol) = _attackTableLayout.GridToTable(gridRow, gridCol);
            if (tableRow < 0 || tableCol < 0) return;

            TableCell cell = _attackTableModel.GetCell(tableRow, tableCol);

            // Only reveal cells that are still in Fog state (not already guessed)
            if (cell.State == TableCellState.Fog)
            {
                // Use Hit state (not Revealed) because attack grids hide letters in Revealed state
                // Hit state will show the letter with the owner's color
                // Use CellOwner.None which will render as grey via ColorRules
                _attackTableModel.SetCellChar(tableRow, tableCol, letter);
                _attackTableModel.SetCellState(tableRow, tableCol, TableCellState.Hit);
                _attackTableModel.SetCellOwner(tableRow, tableCol, CellOwner.None); // None = end-game reveal (grey)
                Debug.Log($"[UIFlowController] End-game reveal: grid ({gridCol},{gridRow}) -> table ({tableRow},{tableCol}) letter '{letter}'");
            }
        }

        #endregion

        #region Guess Event Handlers

        private void HandleLetterHit(char letter, List<Vector2Int> positions)
        {
            Debug.Log($"[UIFlowController] Letter '{letter}' hit at {positions.Count} positions");

            Color playerColor = _wizardManager?.PlayerColor ?? ColorRules.SelectableColors[0];

            // Check if all coordinates for this letter are known
            bool allCoordinatesKnown = AreAllLetterPositionsCoordinateGuessed(letter, positions);

            // Reveal the letter in the word rows (WORDS TO FIND section)
            int revealed = 0;
            if (_attackWordRows != null)
            {
                if (allCoordinatesKnown)
                {
                    // All coordinates known - show in player color
                    revealed = _attackWordRows.RevealLetterInAllWords(letter, playerColor);
                    Debug.Log($"[UIFlowController] Revealed letter '{letter}' in {revealed} word positions (player color - all coords known)");
                }
                else
                {
                    // Not all coordinates known - show in yellow/found color
                    revealed = _attackWordRows.RevealLetterAsFoundInAllWords(letter);
                    Debug.Log($"[UIFlowController] Revealed letter '{letter}' in {revealed} word positions (yellow - not all coords known)");
                }
            }

            // Upgrade any grid cells that are ALREADY yellow (Revealed state from coordinate guesses)
            // Cells still in Fog stay hidden - letter guesses don't reveal grid positions
            foreach (Vector2Int pos in positions)
            {
                if (IsGridCellInState(pos.x, pos.y, TableCellState.Revealed))
                {
                    RevealGridCellFully(pos.x, pos.y, letter, allCoordinatesKnown);
                    Debug.Log($"[UIFlowController] Upgraded yellow cell ({pos.x}, {pos.y}) to show '{letter}' (allCoordsKnown={allCoordinatesKnown})");
                }
            }

            // Update keyboard letter state based on whether ALL coordinates for this letter are known
            if (revealed > 0 && _gameplayManager != null)
            {
                if (allCoordinatesKnown)
                {
                    _gameplayManager.SetKeyboardLetterState(letter, LetterKeyState.Hit);
                    Debug.Log($"[UIFlowController] Keyboard '{letter}' -> Hit (all {positions.Count} coordinates known)");
                }
                else
                {
                    _gameplayManager.SetKeyboardLetterState(letter, LetterKeyState.Found);
                    Debug.Log($"[UIFlowController] Keyboard '{letter}' -> Found (yellow, not all coordinates known yet)");
                }
            }
        }

        /// <summary>
        /// Checks if all grid positions for a letter have been coordinate-guessed.
        /// Keyboard should only show player color when ALL coordinates for that letter are known.
        /// </summary>
        private bool AreAllLetterPositionsCoordinateGuessed(char letter, List<Vector2Int> positions)
        {
            if (_guessManager == null) return false;

            foreach (Vector2Int pos in positions)
            {
                if (!_guessManager.HasPlayerGuessedCoordinate(pos.x, pos.y))
                {
                    return false;
                }
            }
            return positions.Count > 0;
        }

        /// <summary>
        /// Checks if a grid cell is in a specific state on the attack grid.
        /// </summary>
        private bool IsGridCellInState(int gridCol, int gridRow, TableCellState state)
        {
            if (_attackTableModel == null || _attackTableLayout == null) return false;

            (int tableRow, int tableCol) = _attackTableLayout.GridToTable(gridRow, gridCol);
            TableCell cell = _attackTableModel.GetCell(tableRow, tableCol);
            return cell.State == state;
        }

        private void HandleLetterMiss(char letter)
        {
            Debug.Log($"[UIFlowController] Letter '{letter}' missed");
            // Letter misses don't affect individual grid cells
        }

        private void HandleCoordinateHit(Vector2Int position, char letter)
        {
            Debug.Log($"[UIFlowController] Coordinate hit at ({position.x}, {position.y}) - letter '{letter}'");

            // Track revealed cell for game state persistence (hit = true)
            _playerRevealedCells[position] = (letter, true);

            // Check if this letter has already been guessed
            bool letterAlreadyKnown = _guessManager?.HasPlayerGuessedLetter(letter) ?? false;
            Debug.Log($"[UIFlowController] Letter '{letter}' already known? {letterAlreadyKnown}");

            if (letterAlreadyKnown)
            {
                // Check if ALL coordinates for this letter are now known
                List<Vector2Int> allPositions = _guessManager?.GetOpponentLetterPositions(letter) ?? new List<Vector2Int>();
                bool allCoordinatesKnown = AreAllLetterPositionsCoordinateGuessed(letter, allPositions);

                // Letter already guessed - show it with appropriate state
                // Uses Found (yellow) if not all coords known, Hit (player color) if all coords known
                RevealGridCellFully(position.x, position.y, letter, allCoordinatesKnown);

                if (allCoordinatesKnown)
                {
                    Color playerColor = _wizardManager?.PlayerColor ?? ColorRules.SelectableColors[0];

                    // Upgrade keyboard to player color
                    if (_gameplayManager != null)
                    {
                        _gameplayManager.SetKeyboardLetterState(letter, LetterKeyState.Hit);
                        Debug.Log($"[UIFlowController] Keyboard '{letter}' -> Hit (all {allPositions.Count} coordinates now known)");
                    }

                    // Upgrade word row letters from yellow to player color
                    if (_attackWordRows != null)
                    {
                        _attackWordRows.UpgradeLetterToPlayerColorInAllWords(letter, playerColor);
                        Debug.Log($"[UIFlowController] Word rows '{letter}' -> player color (all coordinates now known)");
                    }

                    // Upgrade ALL grid cells with this letter from Found to Hit
                    UpgradeAttackGridCellsToHit(letter);
                }
                else
                {
                    Debug.Log($"[UIFlowController] Keyboard '{letter}' stays Found (yellow) - not all coordinates known yet");
                }
            }
            else
            {
                // Letter not yet guessed - mark as Revealed (yellow) but don't show letter
                MarkGridCellCoordinateHit(position.x, position.y);

                // The letter will be revealed in word rows via OnLetterHit event
                // which will also set the keyboard to Found (yellow)
            }
        }

        /// <summary>
        /// Called after coordinate and letter processing to ensure keyboard state is correct.
        /// This re-evaluates all GUESSED letters (via keyboard) and updates their keyboard state.
        /// NOTE: Only letters guessed via keyboard should appear on the keyboard tracker.
        /// Coordinate hits do NOT update the keyboard - they only update the grid.
        /// </summary>
        private void RefreshKeyboardLetterStates()
        {
            if (_guessManager == null || _gameplayManager == null) return;

            Color playerColor = _wizardManager?.PlayerColor ?? ColorRules.SelectableColors[0];

            // Only check letters that were GUESSED VIA KEYBOARD, not just hit via coordinate
            foreach (char letter in "ABCDEFGHIJKLMNOPQRSTUVWXYZ")
            {
                // Only update keyboard for letters the player explicitly guessed
                if (_guessManager.HasPlayerGuessedLetter(letter))
                {
                    // Check if this letter exists in opponent's words (was it a hit?)
                    if (_guessManager.IsPlayerLetterHit(letter))
                    {
                        // Letter was guessed AND exists - check if all coords known for player color
                        bool allCoordsKnown = _guessManager.AreAllLetterCoordinatesKnown(letter);
                        if (allCoordsKnown)
                        {
                            _gameplayManager.SetKeyboardLetterState(letter, LetterKeyState.Hit);
                            // Also upgrade attack grid cells and word rows to player color
                            _attackWordRows?.UpgradeLetterToPlayerColorInAllWords(letter, playerColor);
                            UpgradeAttackGridCellsToHit(letter);
                        }
                        else
                        {
                            _gameplayManager.SetKeyboardLetterState(letter, LetterKeyState.Found);
                        }
                    }
                    // If letter was guessed but NOT a hit, it's already marked as Miss by HandleLetterMiss
                }
            }
        }

        private void HandleCoordinateMiss(Vector2Int position)
        {
            Debug.Log($"[UIFlowController] Coordinate miss at ({position.x}, {position.y})");

            // Track revealed cell for game state persistence (miss = empty letter, isHit = false)
            _playerRevealedCells[position] = ('\0', false);

            MarkGridCellMiss(position.x, position.y);
        }

        #endregion

        #region Grid Cell State Helpers

        /// <summary>
        /// Reveals a grid cell with letter on the attack grid.
        /// Uses Found state (yellow) if not all coords for this letter are known.
        /// Uses Hit state (player color) if all coords for this letter are known.
        /// </summary>
        /// <param name="allCoordsKnown">True if all coordinates for this letter are known</param>
        private void RevealGridCellFully(int gridCol, int gridRow, char letter, bool allCoordsKnown = false)
        {
            if (_attackTableModel == null || _attackTableLayout == null) return;

            (int tableRow, int tableCol) = _attackTableLayout.GridToTable(gridRow, gridCol);
            _attackTableModel.SetCellChar(tableRow, tableCol, letter);

            // Use Hit (player color) only if ALL coordinates for this letter are known
            // Otherwise use Found (yellow with letter shown)
            TableCellState state = allCoordsKnown ? TableCellState.Hit : TableCellState.Found;
            _attackTableModel.SetCellState(tableRow, tableCol, state);

            // Set owner to Player1 so hit cells show in PLAYER color, not opponent color
            _attackTableModel.SetCellOwner(tableRow, tableCol, CellOwner.Player);

            Debug.Log($"[UIFlowController] Revealed cell ({gridCol}, {gridRow}) with letter '{letter}' - state: {state} (allCoordsKnown={allCoordsKnown})");
        }

        /// <summary>
        /// Marks a grid cell as coordinate hit but letter unknown (yellow, no letter shown) on the attack grid.
        /// </summary>
        private void MarkGridCellCoordinateHit(int gridCol, int gridRow)
        {
            if (_attackTableModel == null || _attackTableLayout == null) return;

            (int tableRow, int tableCol) = _attackTableLayout.GridToTable(gridRow, gridCol);
            // Don't set the letter - keep it hidden
            _attackTableModel.SetCellState(tableRow, tableCol, TableCellState.Revealed);
            // Set owner to Player1 for player's coordinate hits
            _attackTableModel.SetCellOwner(tableRow, tableCol, CellOwner.Player);

            Debug.Log($"[UIFlowController] Marked cell ({gridCol}, {gridRow}) as coordinate hit (yellow)");
        }

        /// <summary>
        /// Marks a grid cell as a miss (coordinate guess, no letter there) on the attack grid - red.
        /// </summary>
        private void MarkGridCellMiss(int gridCol, int gridRow)
        {
            if (_attackTableModel == null || _attackTableLayout == null) return;

            (int tableRow, int tableCol) = _attackTableLayout.GridToTable(gridRow, gridCol);
            _attackTableModel.SetCellState(tableRow, tableCol, TableCellState.Miss);

            Debug.Log($"[UIFlowController] Marked cell ({gridCol}, {gridRow}) as miss (red)");
        }

        /// <summary>
        /// Upgrades all attack grid cells with the given letter from Found (yellow) to Hit (player color).
        /// Called when all coordinates for a letter become known.
        /// </summary>
        private void UpgradeAttackGridCellsToHit(char letter)
        {
            if (_attackTableModel == null || _attackTableLayout == null || _guessManager == null) return;

            List<Vector2Int> positions = _guessManager.GetOpponentLetterPositions(letter);
            foreach (Vector2Int pos in positions)
            {
                (int tableRow, int tableCol) = _attackTableLayout.GridToTable(pos.y, pos.x);
                TableCell cell = _attackTableModel.GetCell(tableRow, tableCol);
                TableCellState currentState = cell.State;

                // Only upgrade cells that are in Found or Revealed state
                if (currentState == TableCellState.Found || currentState == TableCellState.Revealed)
                {
                    _attackTableModel.SetCellChar(tableRow, tableCol, letter);
                    _attackTableModel.SetCellState(tableRow, tableCol, TableCellState.Hit);
                    _attackTableModel.SetCellOwner(tableRow, tableCol, CellOwner.Player);
                    Debug.Log($"[UIFlowController] Upgraded attack grid cell ({pos.x}, {pos.y}) from {currentState} to Hit for letter '{letter}'");
                }
            }
        }

        /// <summary>
        /// Creates the defense TableModel with player's letters fully visible.
        /// This is the grid the opponent attacks.
        /// </summary>
        private void CreateDefenseModel(int gridSize, Color playerColor)
        {
            if (_tableLayout == null || _placementAdapter == null) return;

            // Create a new TableModel for the defense view
            _defenseTableModel = new TableModel();
            _defenseTableModel.Initialize(_tableLayout);

            // Copy the player's placed letters to the defense model (fully visible)
            IReadOnlyDictionary<Vector2Int, char> placedLetters = _placementAdapter.PlacedLetters;

            foreach (KeyValuePair<Vector2Int, char> kvp in placedLetters)
            {
                Vector2Int gridPos = kvp.Key;
                char letter = kvp.Value;

                // Convert grid position to table position
                (int tableRow, int tableCol) = _tableLayout.GridToTable(gridPos.y, gridPos.x);

                // Set the cell to show the letter (Normal state = visible, no owner color)
                // Letters are visible but uncolored until hit by opponent
                _defenseTableModel.SetCellChar(tableRow, tableCol, letter);
                _defenseTableModel.SetCellState(tableRow, tableCol, TableCellState.Normal);
                _defenseTableModel.SetCellOwner(tableRow, tableCol, CellOwner.None);
            }

            // Set empty cells to Fog (no letter there)
            for (int gridRow = 0; gridRow < gridSize; gridRow++)
            {
                for (int gridCol = 0; gridCol < gridSize; gridCol++)
                {
                    Vector2Int pos = new Vector2Int(gridCol, gridRow);
                    if (!placedLetters.ContainsKey(pos))
                    {
                        (int tableRow, int tableCol) = _tableLayout.GridToTable(gridRow, gridCol);
                        _defenseTableModel.SetCellState(tableRow, tableCol, TableCellState.Fog);
                    }
                }
            }

            Debug.Log($"[UIFlowController] Created defense model with {placedLetters.Count} visible letters");
        }

        /// <summary>
        /// Creates the defense TableModel from explicit word placements (for resumed games).
        /// Does NOT use _placementAdapter - uses the provided placements list directly.
        /// </summary>
        private void CreateDefenseModelFromPlacements(int gridSize, List<WordPlacementData> placements, Color playerColor)
        {
            Debug.Log($"[UIFlowController] CreateDefenseModelFromPlacements: gridSize={gridSize}, placements={placements?.Count ?? 0}");

            // Create a layout for this grid size
            _tableLayout = TableLayout.CreateForGameplay(gridSize, placements.Count);

            // Create a new TableModel for the defense view
            _defenseTableModel = new TableModel();
            _defenseTableModel.Initialize(_tableLayout);

            // Build a dictionary of placed letters from the placements
            Dictionary<Vector2Int, char> placedLetters = new Dictionary<Vector2Int, char>();
            foreach (WordPlacementData placement in placements)
            {
                Debug.Log($"[UIFlowController] Defense placement: word={placement.Word}, start=({placement.StartRow},{placement.StartCol}), dir=({placement.DirRow},{placement.DirCol})");

                for (int i = 0; i < placement.Word.Length; i++)
                {
                    int col = placement.StartCol + (i * placement.DirCol);
                    int row = placement.StartRow + (i * placement.DirRow);

                    // Bounds check
                    if (row < 0 || row >= gridSize || col < 0 || col >= gridSize)
                    {
                        Debug.LogWarning($"[UIFlowController] Defense placement out of bounds: ({row},{col}) for gridSize={gridSize}, word={placement.Word}[{i}]='{placement.Word[i]}'");
                        continue;
                    }

                    char letter = placement.Word[i];
                    Vector2Int pos = new Vector2Int(col, row);
                    placedLetters[pos] = letter;
                }
            }

            // Set placed letters as visible
            foreach (KeyValuePair<Vector2Int, char> kvp in placedLetters)
            {
                Vector2Int gridPos = kvp.Key;
                char letter = kvp.Value;

                (int tableRow, int tableCol) = _tableLayout.GridToTable(gridPos.y, gridPos.x);

                _defenseTableModel.SetCellChar(tableRow, tableCol, letter);
                _defenseTableModel.SetCellState(tableRow, tableCol, TableCellState.Normal);
                _defenseTableModel.SetCellOwner(tableRow, tableCol, CellOwner.None);
            }

            // Set empty cells to Fog
            for (int gridRow = 0; gridRow < gridSize; gridRow++)
            {
                for (int gridCol = 0; gridCol < gridSize; gridCol++)
                {
                    Vector2Int pos = new Vector2Int(gridCol, gridRow);
                    if (!placedLetters.ContainsKey(pos))
                    {
                        (int tableRow, int tableCol) = _tableLayout.GridToTable(gridRow, gridCol);
                        _defenseTableModel.SetCellState(tableRow, tableCol, TableCellState.Fog);
                    }
                }
            }

            Debug.Log($"[UIFlowController] Created defense model from placements with {placedLetters.Count} visible letters");
        }

        // Store reference to attack word rows for updates
        private WordRowsContainer _attackWordRows;

        #endregion

        #region UI Update Helpers

        private void UpdateMissCountDisplay(bool isPlayer)
        {
            if (_guessManager == null || _gameplayManager == null) return;

            if (isPlayer)
            {
                int missCount = _guessManager.GetPlayerMissCount();
                int missLimit = _guessManager.GetPlayerMissLimit();
                _gameplayManager.SetPlayerMissCount(missCount, missLimit);
            }
            else
            {
                int missCount = _guessManager.GetOpponentMissCount();
                int missLimit = _guessManager.GetOpponentMissLimit();
                _gameplayManager.SetOpponentMissCount(missCount, missLimit);
            }
        }

        private void ShowGuillotineOverlay()
        {
            if (_guillotineOverlayManager == null) return;

            // Get data from GameplayScreenManager
            PlayerTabData playerTabData = _gameplayManager?.PlayerData;
            PlayerTabData opponentTabData = _gameplayManager?.OpponentData;

            // Create guillotine data from player tab data
            GuillotineData playerData = new GuillotineData
            {
                Name = playerTabData?.Name ?? "You",
                Color = playerTabData?.Color ?? _wizardManager?.PlayerColor ?? ColorRules.SelectableColors[0],
                MissCount = playerTabData?.MissCount ?? 0,
                MissLimit = playerTabData?.MissLimit ?? 20,
                IsLocalPlayer = true
            };

            GuillotineData opponentData = new GuillotineData
            {
                Name = opponentTabData?.Name ?? "EXECUTIONER",
                Color = opponentTabData?.Color ?? ColorRules.SelectableColors[1],
                MissCount = opponentTabData?.MissCount ?? 0,
                MissLimit = opponentTabData?.MissLimit ?? 18,
                IsLocalPlayer = false
            };

            DLYH.Audio.UIAudioManager.PopupOpen();
            _guillotineOverlayManager.Show(playerData, opponentData);
        }

        /// <summary>
        /// Shows guillotine overlay with the specified player's blade at a previous position
        /// for delayed animation effect.
        /// </summary>
        private void ShowGuillotineOverlayWithDelayedAnimation(bool isPlayer, int previousStage)
        {
            DLYH.Audio.UIAudioManager.PopupOpen();

            if (_guillotineOverlayManager == null) return;

            // Get data from GameplayScreenManager
            PlayerTabData playerTabData = _gameplayManager?.PlayerData;
            PlayerTabData opponentTabData = _gameplayManager?.OpponentData;

            // Create guillotine data from player tab data
            GuillotineData playerData = new GuillotineData
            {
                Name = playerTabData?.Name ?? "You",
                Color = playerTabData?.Color ?? _wizardManager?.PlayerColor ?? ColorRules.SelectableColors[0],
                MissCount = playerTabData?.MissCount ?? 0,
                MissLimit = playerTabData?.MissLimit ?? 20,
                IsLocalPlayer = true
            };

            GuillotineData opponentData = new GuillotineData
            {
                Name = opponentTabData?.Name ?? "EXECUTIONER",
                Color = opponentTabData?.Color ?? ColorRules.SelectableColors[1],
                MissCount = opponentTabData?.MissCount ?? 0,
                MissLimit = opponentTabData?.MissLimit ?? 18,
                IsLocalPlayer = false
            };

            // Pass initial stage for the transitioning player so blade starts at previous position
            int initialPlayerStage = isPlayer ? previousStage : -1;
            int initialOpponentStage = isPlayer ? -1 : previousStage;

            _guillotineOverlayManager.Show(playerData, opponentData, initialPlayerStage, initialOpponentStage);
        }

        #endregion

        // === Navigation Handlers ===

        private void HandleGameModeSelected(GameMode mode)
        {
            DLYH.Audio.UIAudioManager.ButtonClick();

            // Check if there's an active game in progress
            if (_hasActiveGame)
            {
                _confirmationModal.Show(
                    "End Current Game?",
                    "Starting a new game will end your current game. Are you sure you want to continue?",
                    () => StartNewGame(mode)
                );
                return;
            }

            StartNewGame(mode);
        }

        private void StartNewGame(GameMode mode)
        {
            Debug.Log($"[UIFlowController] StartNewGame called with mode: {mode}");

            // Clear any existing game state
            ResetGameState();

            // Reset wizard state for a new game
            _wizardManager?.Reset();

            _currentGameMode = mode;
            // Don't set _hasActiveGame here - it should only be true when gameplay starts
            _wizardManager?.SetGameMode(mode);
            ShowSetupWizard();

            Debug.Log("[UIFlowController] StartNewGame completed - setup wizard should be visible");
        }

        private void ResetGameState()
        {
            // Reset game-related flags
            _isGameOver = false;
            _hasActiveGame = false;
            _isPlayerTurn = true;
            _isGamePausedForStageTransition = false;
            _waitingForOpponent = false;
            _currentGameCode = null; // Clear game code to prevent resume conflicts

            // Clear extra turn queues
            _playerExtraTurnQueue.Clear();
            _opponentExtraTurnQueue.Clear();

            // Stop any running coroutines
            if (_opponentTurnTimeoutCoroutine != null)
            {
                StopCoroutine(_opponentTurnTimeoutCoroutine);
                _opponentTurnTimeoutCoroutine = null;
            }
            if (_gameOverSequenceCoroutine != null)
            {
                StopCoroutine(_gameOverSequenceCoroutine);
                _gameOverSequenceCoroutine = null;
            }

            // Reset guess manager
            _guessManager = null;

            // Dispose existing opponent
            if (_opponent != null)
            {
                _opponent.OnLetterGuess -= HandleOpponentLetterGuess;
                _opponent.OnCoordinateGuess -= HandleOpponentCoordinateGuess;
                _opponent.OnWordGuess -= HandleOpponentWordGuess;
                _opponent.OnThinkingStarted -= HandleOpponentThinkingStarted;
                _opponent.OnThinkingComplete -= HandleOpponentThinkingComplete;
                _opponent.Dispose();
                _opponent = null;
            }

            // Reset guillotine overlay
            _guillotineOverlayManager?.ResetGameOverState();
            _guillotineOverlayManager?.Hide();

            // Reset tracked stages
            _playerPreviousStage = 1;
            _opponentPreviousStage = 1;

            // Reset gameplay manager (keyboard states, guessed words, etc.)
            _gameplayManager?.Reset();

            // Clear game state persistence tracking
            _playerRevealedCells.Clear();
            _opponentRevealedCells.Clear();
            _playerGuessedLetters.Clear();
            _playerSolvedWords.Clear();
            _opponentSolvedWords.Clear();
            _allWordGuesses.Clear();

            Debug.Log("[UIFlowController] Game state reset for new game");
        }

        private void HandleContinueGameClicked()
        {
            DLYH.Audio.UIAudioManager.ButtonClick();

            // If there's an active game in gameplay, show gameplay screen
            if (_hasActiveGame)
            {
                ShowGameplayScreen();
                return;
            }

            // Otherwise, this shouldn't happen - Continue button should be hidden
            // But as a fallback, show main menu
            Debug.LogWarning("[UIFlowController] Continue clicked but no active game - showing main menu");
            ShowMainMenu();
        }

        private void HandleHowToPlayClicked()
        {
            DLYH.Audio.UIAudioManager.ButtonClick();
            _helpModal.Show();
        }

        private void HandleFeedbackClicked()
        {
            DLYH.Audio.UIAudioManager.ButtonClick();
            ShowFeedbackModal("Share Feedback", false, false);
        }

        private void HandleExitClicked()
        {
            DLYH.Audio.UIAudioManager.ButtonClick();

#if UNITY_WEBGL && !UNITY_EDITOR
            // WebGL: Redirect parent/top window to TecVooDoo games page
            // Use top.location to escape iframe, fallback to window.location if not in iframe
            Application.ExternalEval("if (window.top !== window.self) { window.top.location.href = 'https://tecvoodoo.com/games'; } else { window.location.href = 'https://tecvoodoo.com/games'; }");
#else
            // Desktop/Editor: Quit the application
            Application.Quit();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#endif
#endif
        }

        /// <summary>
        /// Shows the feedback modal with the given title.
        /// </summary>
        /// <param name="title">Modal title (e.g., "Share Feedback", "Victory!", "Defeated")</param>
        /// <param name="isPostGame">True if shown after a game ends</param>
        /// <param name="playerWon">If post-game, whether the player won</param>
        public void ShowFeedbackModal(string title, bool isPostGame, bool playerWon)
        {
            if (_feedbackModalContainer == null) return;

            DLYH.Audio.UIAudioManager.PopupOpen();

            _feedbackIsPostGame = isPostGame;
            _feedbackPlayerWon = playerWon;

            // Update title
            if (_feedbackTitle != null)
            {
                _feedbackTitle.text = title;
            }

            // Clear previous input
            if (_feedbackInput != null)
            {
                _feedbackInput.value = "";
            }

            // Show modal
            _feedbackModalContainer.RemoveFromClassList("hidden");
        }

        private void HideFeedbackModal()
        {
            DLYH.Audio.UIAudioManager.PopupClose();

            // Reset word guess mode if active
            _isWordGuessMode = false;
            _wordGuessTargetIndex = -1;

            if (_feedbackModalContainer != null)
            {
                _feedbackModalContainer.AddToClassList("hidden");
            }

            // If this was post-game feedback, return to main menu
            if (_feedbackIsPostGame)
            {
                _feedbackIsPostGame = false;
                _hasActiveGame = false; // Clear active game state
                ShowMainMenu();
            }
        }

        private void CreateFeedbackModal()
        {
            if (_feedbackModalUxml == null)
            {
                Debug.LogWarning("[UIFlowController] FeedbackModal UXML asset not assigned in Inspector!");
                return;
            }

            _feedbackModalContainer = _feedbackModalUxml.CloneTree();
            _feedbackModalContainer.style.position = Position.Absolute;
            _feedbackModalContainer.style.left = 0;
            _feedbackModalContainer.style.right = 0;
            _feedbackModalContainer.style.top = 0;
            _feedbackModalContainer.style.bottom = 0;

            // Cache elements
            _feedbackTitle = _feedbackModalContainer.Q<Label>("modal-title");
            _feedbackInput = _feedbackModalContainer.Q<TextField>("feedback-input");

            // Wire up buttons
            Button closeBtn = _feedbackModalContainer.Q<Button>("btn-close");
            Button cancelBtn = _feedbackModalContainer.Q<Button>("btn-cancel");
            Button submitBtn = _feedbackModalContainer.Q<Button>("btn-submit");

            if (closeBtn != null)
            {
                closeBtn.clicked += HideFeedbackModal;
            }
            if (cancelBtn != null)
            {
                cancelBtn.clicked += HideFeedbackModal;
            }
            if (submitBtn != null)
            {
                submitBtn.clicked += HandleFeedbackSubmit;
            }

            // Click on overlay background closes modal
            VisualElement overlay = _feedbackModalContainer.Q<VisualElement>("modal-overlay");
            if (overlay != null)
            {
                overlay.RegisterCallback<ClickEvent>(evt =>
                {
                    // Only close if clicking directly on the overlay, not on modal content
                    if (evt.target == overlay)
                    {
                        HideFeedbackModal();
                    }
                });
            }

            _root.Add(_feedbackModalContainer);

            // Start hidden
            _feedbackModalContainer.AddToClassList("hidden");
        }

        private void HandleFeedbackSubmit()
        {
            string feedbackText = _feedbackInput?.value ?? "";

            if (string.IsNullOrWhiteSpace(feedbackText))
            {
                // Don't submit empty feedback
                HideFeedbackModal();
                return;
            }

            // Send to telemetry
            PlaytestTelemetry.Feedback(feedbackText, _feedbackPlayerWon);

            Debug.Log($"[UIFlowController] Feedback submitted: {feedbackText.Substring(0, Mathf.Min(50, feedbackText.Length))}...");

            HideFeedbackModal();
        }

        // NOTE: Confirmation modal moved to ConfirmationModalManager
        // NOTE: Help modal moved to HelpModalManager

        // === Hamburger Menu ===

        private void CreateHamburgerMenu()
        {
            if (_hamburgerMenuUxml == null)
            {
                Debug.LogWarning("[UIFlowController] HamburgerMenu UXML asset not assigned in Inspector!");
                return;
            }

            _hamburgerMenuContainer = _hamburgerMenuUxml.CloneTree();
            _hamburgerMenuContainer.style.position = Position.Absolute;
            _hamburgerMenuContainer.style.left = 0;
            _hamburgerMenuContainer.style.right = 0;
            _hamburgerMenuContainer.style.top = 0;
            _hamburgerMenuContainer.style.bottom = 0;
            _hamburgerMenuContainer.pickingMode = PickingMode.Ignore;

            // Cache elements
            _hamburgerButton = _hamburgerMenuContainer.Q<Button>("hamburger-button");
            _hamburgerOverlay = _hamburgerMenuContainer.Q<VisualElement>("hamburger-overlay");
            _resumeButton = _hamburgerMenuContainer.Q<Button>("btn-resume");

            // Settings controls in hamburger
            _hbSfxSlider = _hamburgerMenuContainer.Q<Slider>("hb-sfx-slider");
            _hbMusicSlider = _hamburgerMenuContainer.Q<Slider>("hb-music-slider");
            _hbQwertyToggle = _hamburgerMenuContainer.Q<Toggle>("hb-qwerty-toggle");
            _hbSfxValueLabel = _hamburgerMenuContainer.Q<Label>("hb-sfx-value");
            _hbMusicValueLabel = _hamburgerMenuContainer.Q<Label>("hb-music-value");

            // Wire up hamburger button
            if (_hamburgerButton != null)
            {
                _hamburgerButton.clicked += ShowHamburgerOverlay;
            }

            // Wire up menu items
            Button mainMenuBtn = _hamburgerMenuContainer.Q<Button>("btn-main-menu");
            if (mainMenuBtn != null)
            {
                mainMenuBtn.clicked += () =>
                {
                    HideHamburgerOverlay();
                    ShowMainMenu();
                };
            }

            if (_resumeButton != null)
            {
                _resumeButton.clicked += HideHamburgerOverlay;
            }

            // Wire up settings sliders (sync with main menu settings)
            if (_hbSfxSlider != null)
            {
                float savedSfx = PlayerPrefs.GetFloat(PREFS_SFX_VOLUME, DEFAULT_VOLUME);
                _hbSfxSlider.value = savedSfx;
                _hbSfxSlider.RegisterValueChangedCallback(OnHbSfxVolumeChanged);
                UpdateHbSfxLabel(savedSfx);
            }

            if (_hbMusicSlider != null)
            {
                float savedMusic = PlayerPrefs.GetFloat(PREFS_MUSIC_VOLUME, DEFAULT_VOLUME);
                _hbMusicSlider.value = savedMusic;
                _hbMusicSlider.RegisterValueChangedCallback(OnHbMusicVolumeChanged);
                UpdateHbMusicLabel(savedMusic);
            }

            if (_hbQwertyToggle != null)
            {
                bool savedQwerty = PlayerPrefs.GetInt(PREFS_QWERTY_KEYBOARD, 0) == 1;
                _hbQwertyToggle.value = savedQwerty;
                _hbQwertyToggle.RegisterValueChangedCallback(OnHbQwertyToggleChanged);
            }

            // Click on overlay background closes menu
            if (_hamburgerOverlay != null)
            {
                _hamburgerOverlay.RegisterCallback<ClickEvent>(evt =>
                {
                    // Close if clicking on overlay background (not the panel)
                    VisualElement panel = _hamburgerMenuContainer.Q<VisualElement>("hamburger-panel");
                    if (evt.target == _hamburgerOverlay || (panel != null && !panel.worldBound.Contains(evt.position)))
                    {
                        HideHamburgerOverlay();
                    }
                });
            }

            _root.Add(_hamburgerMenuContainer);

            // Start with button hidden (shown on setup wizard)
            HideHamburgerButton();
        }

        private void InitializeNetworkingUI()
        {
            // Create networking services if we have config
            if (_supabaseConfig != null && _supabaseConfig.IsValid)
            {
                // Create Supabase client (no auth token needed for anon access to players table)
                _supabaseClient = new SupabaseClient(_supabaseConfig, null);

                // Create services
                _playerService = new PlayerService(_supabaseClient);
                _gameSessionService = new GameSessionService(_supabaseClient, _supabaseConfig);
                _matchmakingService = new MatchmakingService(
                    _supabaseClient,
                    _supabaseConfig,
                    _gameSessionService,
                    _playerService
                );

                Debug.Log("[UIFlowController] Networking services initialized");

                // Update ActiveGamesManager with the new services so My Active Games works
                _activeGamesManager?.UpdateServices(_gameSessionService, _playerService);
            }
            else
            {
                Debug.LogWarning("[UIFlowController] SupabaseConfig not assigned or invalid - networking will use dummy mode");
            }

            // Create networking UI manager with the UI assets
            _networkingUIManager = new NetworkingUIManager(
                _matchmakingOverlayUxml,
                _waitingRoomUxml,
                _joinCodeEntryUxml,
                _matchmakingOverlayUss,
                _waitingRoomUss,
                _joinCodeEntryUss
            );

            // Initialize with root element and services (services may be null if config missing)
            _networkingUIManager.Initialize(_root, _matchmakingService, _playerService, _gameSessionService);

            // Wire up events
            _networkingUIManager.OnNetworkingComplete += HandleNetworkingComplete;
            _networkingUIManager.OnCancelled += HandleNetworkingCancelled;

            Debug.Log("[UIFlowController] Networking UI initialized");
        }

        private void HandleNetworkingComplete(NetworkingUIResult result)
        {
            if (result.Success)
            {
                Debug.Log($"[UIFlowController] Networking complete - GameCode: {result.GameCode}, " +
                          $"IsHost: {result.IsHost}, IsPhantomAI: {result.IsPhantomAI}, " +
                          $"Opponent: {result.OpponentName}");

                // If rejoining a hidden game (via code entry), unhide it so it shows in My Active Games
                if (!string.IsNullOrEmpty(result.GameCode))
                {
                    _activeGamesManager?.RemoveFromHiddenGames(result.GameCode);
                }

                // Start the game with the matched opponent
                StartGameAfterMatchmaking(result).Forget();
            }
            else
            {
                Debug.LogWarning($"[UIFlowController] Networking failed: {result.ErrorMessage}");
                // Stay on setup wizard - user can try again or go back
            }
        }

        private void HandleNetworkingCancelled()
        {
            Debug.Log("[UIFlowController] Networking cancelled - returning to setup wizard");
            // User cancelled - stay on setup wizard
        }

        private void ShowHamburgerButton()
        {
            if (_hamburgerButton != null)
            {
                _hamburgerButton.RemoveFromClassList("hidden");
            }
        }

        private void HideHamburgerButton()
        {
            if (_hamburgerButton != null)
            {
                _hamburgerButton.AddToClassList("hidden");
            }
            HideHamburgerOverlay();
        }

        private void ShowHamburgerOverlay()
        {
            DLYH.Audio.UIAudioManager.ButtonClick();

            if (_hamburgerOverlay != null)
            {
                // Sync settings values before showing
                SyncHamburgerSettings();
                _hamburgerOverlay.RemoveFromClassList("hidden");
            }
        }

        private void HideHamburgerOverlay()
        {
            DLYH.Audio.UIAudioManager.ButtonClick();

            if (_hamburgerOverlay != null)
            {
                _hamburgerOverlay.AddToClassList("hidden");
            }
        }

        private void SyncHamburgerSettings()
        {
            // Sync hamburger menu settings with stored values
            float sfx = PlayerPrefs.GetFloat(PREFS_SFX_VOLUME, DEFAULT_VOLUME);
            float music = PlayerPrefs.GetFloat(PREFS_MUSIC_VOLUME, DEFAULT_VOLUME);
            bool qwerty = PlayerPrefs.GetInt(PREFS_QWERTY_KEYBOARD, 0) == 1;

            if (_hbSfxSlider != null) _hbSfxSlider.SetValueWithoutNotify(sfx);
            if (_hbMusicSlider != null) _hbMusicSlider.SetValueWithoutNotify(music);
            if (_hbQwertyToggle != null) _hbQwertyToggle.SetValueWithoutNotify(qwerty);

            UpdateHbSfxLabel(sfx);
            UpdateHbMusicLabel(music);
        }

        private void OnHbSfxVolumeChanged(ChangeEvent<float> evt)
        {
            float volume = evt.newValue;
            PlayerPrefs.SetFloat(PREFS_SFX_VOLUME, volume);
            PlayerPrefs.Save();
            UpdateHbSfxLabel(volume);

            // Sync main menu slider
            if (_sfxSlider != null) _sfxSlider.SetValueWithoutNotify(volume);
            UpdateSfxLabel(volume);

            // Refresh audio manager
            if (UIAudioManager.Instance != null)
            {
                UIAudioManager.Instance.RefreshVolumeCache();
            }
        }

        private void OnHbMusicVolumeChanged(ChangeEvent<float> evt)
        {
            float volume = evt.newValue;
            PlayerPrefs.SetFloat(PREFS_MUSIC_VOLUME, volume);
            PlayerPrefs.Save();
            UpdateHbMusicLabel(volume);

            // Sync main menu slider
            if (_musicSlider != null) _musicSlider.SetValueWithoutNotify(volume);
            UpdateMusicLabel(volume);

            // Refresh music manager
            if (MusicManager.Instance != null)
            {
                MusicManager.Instance.RefreshVolumeCache();
            }
        }

        private void OnHbQwertyToggleChanged(ChangeEvent<bool> evt)
        {
            PlayerPrefs.SetInt(PREFS_QWERTY_KEYBOARD, evt.newValue ? 1 : 0);
            PlayerPrefs.Save();

            // Sync main menu toggle
            if (_qwertyToggle != null) _qwertyToggle.SetValueWithoutNotify(evt.newValue);

            // Update keyboard layout immediately
            RefreshKeyboardIfNeeded();
        }

        private void UpdateHbSfxLabel(float volume)
        {
            if (_hbSfxValueLabel != null)
            {
                _hbSfxValueLabel.text = string.Format("{0:0}%", volume * 100f);
            }
        }

        private void UpdateHbMusicLabel(float volume)
        {
            if (_hbMusicValueLabel != null)
            {
                _hbMusicValueLabel.text = string.Format("{0:0}%", volume * 100f);
            }
        }

        private void HandleQuickSetup()
        {
            // Auto-fill random words and placement for Quick Setup mode
            HandleRandomWords();
            HandleRandomPlacement();
        }

        private void HandleSetupComplete(SetupWizardUIManager.SetupData data)
        {
            InitializeTableForPlacement(data);
        }

        // === Screen Visibility ===

        private void ShowMainMenu()
        {
            // Stop any running game over sequence
            if (_gameOverSequenceCoroutine != null)
            {
                StopCoroutine(_gameOverSequenceCoroutine);
                _gameOverSequenceCoroutine = null;
            }

            // Stop opponent join polling when leaving gameplay (Session 4)
            StopOpponentJoinPolling();

            // Hide guillotine overlay if visible
            _guillotineOverlayManager?.Hide();

            if (_mainMenuScreen != null)
            {
                _mainMenuScreen.style.display = DisplayStyle.Flex;
                _mainMenuScreen.visible = true;
            }

            if (_setupWizardScreen != null)
            {
                _setupWizardScreen.style.display = DisplayStyle.None;
                _setupWizardScreen.visible = false;
            }

            // Hide hamburger on main menu (settings are inline there)
            HideHamburgerButton();

            // Update Continue Game button visibility
            UpdateContinueGameButton();

            // Sync main menu settings with stored values
            SyncMainMenuSettings();

            // Load My Active Games list (async, non-blocking)
            _activeGamesManager?.LoadMyActiveGamesAsync().Forget();

            // Update auth status display
            UpdateAuthStatusDisplay();

            // Start trivia rotation with fade cycling
            StartTriviaRotation();

            // Note: We don't reset wizard state here anymore - only when starting a NEW game
        }

        private void UpdateContinueGameButton()
        {
            if (_continueGameButton == null) return;

            if (_hasActiveGame)
            {
                _continueGameButton.RemoveFromClassList("hidden");
            }
            else
            {
                _continueGameButton.AddToClassList("hidden");
            }
        }

        /// <summary>
        /// Clears the active game state (called when game ends or player explicitly starts new game)
        /// </summary>
        public void ClearActiveGame()
        {
            _hasActiveGame = false;
            _wizardManager?.Reset();
        }

        private void ShowSetupWizard()
        {
            // Stop trivia rotation when leaving main menu
            StopTriviaRotation();

            // Hide all other screens
            if (_mainMenuScreen != null)
            {
                _mainMenuScreen.style.display = DisplayStyle.None;
                _mainMenuScreen.visible = false;
            }
            if (_gameplayScreen != null)
            {
                _gameplayScreen.style.display = DisplayStyle.None;
            }

            // Show setup wizard
            if (_setupWizardScreen != null)
            {
                _setupWizardScreen.style.display = DisplayStyle.Flex;
                _setupWizardScreen.visible = true;
            }

            // Show hamburger button on setup wizard
            ShowHamburgerButton();
        }

        private void SyncMainMenuSettings()
        {
            // Sync main menu settings sliders with stored values
            float sfx = PlayerPrefs.GetFloat(PREFS_SFX_VOLUME, DEFAULT_VOLUME);
            float music = PlayerPrefs.GetFloat(PREFS_MUSIC_VOLUME, DEFAULT_VOLUME);
            bool qwerty = PlayerPrefs.GetInt(PREFS_QWERTY_KEYBOARD, 0) == 1;

            if (_sfxSlider != null) _sfxSlider.SetValueWithoutNotify(sfx);
            if (_musicSlider != null) _musicSlider.SetValueWithoutNotify(music);
            if (_qwertyToggle != null) _qwertyToggle.SetValueWithoutNotify(qwerty);

            UpdateSfxLabel(sfx);
            UpdateMusicLabel(music);
        }

        // === Table Initialization ===

        /// <summary>
        /// Cleans up existing table/grid visual components to prevent stacking.
        /// Called before creating new table components for a new game.
        /// </summary>
        private void CleanupTableComponents()
        {
            // Clean up placement adapter
            if (_placementAdapter != null)
            {
                _placementAdapter.OnWordPlaced -= HandleWordPlacedOnGrid;
                _placementAdapter.OnPlacementCancelled -= HandlePlacementCancelled;
                _placementAdapter.Dispose();
                _placementAdapter = null;
            }

            // Clean up word suggestion dropdown
            if (_wordSuggestionDropdown != null)
            {
                _wordSuggestionDropdown.OnWordSelected -= HandleWordSuggestionSelected;
                _wordSuggestionDropdown.Root?.RemoveFromHierarchy();
                _wordSuggestionDropdown = null;
            }

            // Clean up word rows container
            if (_wordRowsContainer != null)
            {
                _wordRowsContainer.OnPlacementRequested -= HandlePlacementRequested;
                _wordRowsContainer.OnWordCleared -= HandleWordCleared;
                _wordRowsContainer.OnLetterCellClicked -= HandleWordRowCellClicked;
                _wordRowsContainer.Root?.RemoveFromHierarchy();
                _wordRowsContainer.Dispose();
                _wordRowsContainer = null;
            }

            // Clean up table view (grid)
            if (_tableView != null)
            {
                _tableView.OnCellClicked -= HandleGridCellClicked;
                _tableView.OnCellHovered -= HandleGridCellHovered;
                _tableView.Unbind();
                _tableView.TableRoot?.RemoveFromHierarchy();
                _tableView = null;
            }

            // Clean up table models
            _tableModel = null;
            _tableLayout = null;
            _attackTableModel = null;
            _attackTableLayout = null;
            _defenseTableModel = null;

            Debug.Log("[UIFlowController] Table components cleaned up for new game");
        }

        private void InitializeTableForPlacement(SetupWizardUIManager.SetupData data)
        {
            // Clean up existing table/grid components before creating new ones
            CleanupTableComponents();

            // Create layout based on setup data
            _tableLayout = TableLayout.CreateForSetup(data.GridSize, data.WordCount);

            // Create table model (grid only - word rows are separate)
            _tableModel = new TableModel();
            _tableModel.Initialize(_tableLayout);

            // Find containers in the wizard's placement panel
            VisualElement wordRowsContainer = _setupWizardScreen.Q<VisualElement>("word-rows-container");
            VisualElement tableContainer = _setupWizardScreen.Q<VisualElement>("table-container");

            if (tableContainer == null)
            {
                Debug.LogError("[UIFlowController] Could not find table-container element");
                return;
            }

            // Create word rows container (separate from grid)
            _wordRowsContainer = new WordRowsContainer(data.WordCount, _tableLayout.WordLengths);
            _wordRowsContainer.SetPlayerColor(data.PlayerColor);

            // Add word rows to UI - either in dedicated container or before table
            if (wordRowsContainer != null)
            {
                wordRowsContainer.Clear();
                wordRowsContainer.Add(_wordRowsContainer.Root);
            }
            else
            {
                // Insert before table container
                int tableIndex = tableContainer.parent.IndexOf(tableContainer);
                tableContainer.parent.Insert(tableIndex, _wordRowsContainer.Root);
            }

            // Create table view (grid only)
            _tableView = new TableView(tableContainer);
            _tableView.SetPlayerColors(data.PlayerColor, ColorRules.SelectableColors[1]);
            _tableView.Bind(_tableModel);

            // Sync sizes with grid cell sizes - apply to word rows and placement panel
            string sizeClass = _tableView.GetSizeClassName();
            _wordRowsContainer.SetSizeClass(sizeClass);

            // Apply viewport-aware sizing (inline styles override CSS class defaults)
            int cellSize = _tableView.GetCalculatedCellSize();
            int fontSize = _tableView.GetCalculatedFontSize();
            if (cellSize > 0)
            {
                _wordRowsContainer.ApplyViewportAwareSizing(cellSize, fontSize);
            }

            // Apply size class to placement panel for keyboard/button scaling
            VisualElement placementPanel = _setupWizardScreen.Q<VisualElement>("placement-panel");
            if (placementPanel != null)
            {
                placementPanel.RemoveFromClassList("size-tiny");
                placementPanel.RemoveFromClassList("size-xsmall");
                placementPanel.RemoveFromClassList("size-small");
                placementPanel.RemoveFromClassList("size-med-small");
                placementPanel.RemoveFromClassList("size-medium");
                placementPanel.RemoveFromClassList("size-med-large");
                placementPanel.RemoveFromClassList("size-large");
                placementPanel.AddToClassList($"size-{sizeClass}");
            }

            // Create word suggestion dropdown
            _wordSuggestionDropdown = new WordSuggestionDropdown();
            _wordSuggestionDropdown.OnWordSelected += HandleWordSuggestionSelected;

            // Add dropdown to placement panel (at the end so it renders on top of everything)
            // This ensures it appears above the grid
            VisualElement placementPanelForDropdown = _setupWizardScreen.Q<VisualElement>("placement-panel");
            placementPanelForDropdown?.Add(_wordSuggestionDropdown.Root);

            // Wire up word row events
            _wordRowsContainer.OnPlacementRequested += HandlePlacementRequested;
            _wordRowsContainer.OnWordCleared += HandleWordCleared;
            _wordRowsContainer.OnLetterCellClicked += HandleWordRowCellClicked;

            // Wire up grid cell clicks for placement
            _tableView.OnCellClicked += HandleGridCellClicked;
            _tableView.OnCellHovered += HandleGridCellHovered;

            // Create placement adapter
            _placementAdapter = new PlacementAdapter(_tableView, _tableModel, _tableLayout, _wordRowsContainer);
            _placementAdapter.OnWordPlaced += HandleWordPlacedOnGrid;
            _placementAdapter.OnPlacementCancelled += HandlePlacementCancelled;

            // Wire up letter keyboard (buttons are inside keyboard-row elements)
            // Only wire up once to prevent multiple handlers being added
            if (!_keyboardWiredUp)
            {
                VisualElement keyboard = _setupWizardScreen.Q<VisualElement>("letter-keyboard");
                if (keyboard != null)
                {
                    // Query all letter-key buttons within the keyboard (including nested in rows)
                    keyboard.Query<Button>(className: "letter-key").ForEach(keyButton =>
                    {
                        // Wire up backspace button
                        if (keyButton.ClassListContains("backspace-key"))
                        {
                            keyButton.clicked += HandleBackspacePressed;
                        }
                        // Wire up letter buttons
                        else if (keyButton.text.Length == 1)
                        {
                            char letter = keyButton.text[0];
                            keyButton.clicked += () => HandleLetterKeyPressed(letter);
                        }
                    });
                    _keyboardWiredUp = true;
                }
            }

            // Wire up placement action buttons
            Button randomWordsBtn = _setupWizardScreen.Q<Button>("btn-random-words");
            Button randomPlacementBtn = _setupWizardScreen.Q<Button>("btn-random-placement");
            Button clearPlacementBtn = _setupWizardScreen.Q<Button>("btn-clear-placement");

            if (randomWordsBtn != null)
            {
                randomWordsBtn.clicked += HandleRandomWords;
            }
            if (randomPlacementBtn != null)
            {
                randomPlacementBtn.clicked += HandleRandomPlacement;
            }
            if (clearPlacementBtn != null)
            {
                clearPlacementBtn.clicked += () =>
                {
                    // Clear only grid placements, keep words in word rows
                    _wordRowsContainer?.ClearAllPlacements();
                    ClearGridPlacements();
                    // Update Ready button state (all placements cleared)
                    UpdateReadyButtonState();
                };
            }

            // Wire up Ready button
            Button readyBtn = _setupWizardScreen.Q<Button>("btn-ready");
            if (readyBtn != null)
            {
                readyBtn.clicked += HandleReadyClicked;
                // Initially disable Ready button until all words are placed
                readyBtn.SetEnabled(false);
            }
        }

        /// <summary>
        /// Updates the Ready button's enabled state based on whether all words are placed.
        /// Called whenever a word is placed or removed from the grid.
        /// </summary>
        private void UpdateReadyButtonState()
        {
            Button readyBtn = _setupWizardScreen?.Q<Button>("btn-ready");
            if (readyBtn == null || _wordRowsContainer == null) return;

            bool allPlaced = _wordRowsContainer.AreAllWordsPlaced();
            readyBtn.SetEnabled(allPlaced);
        }

        // === Word Entry Handlers ===

        private void HandleLetterKeyPressed(char letter)
        {
            int activeRow = _wordRowsContainer?.ActiveRowIndex ?? -1;

            // If no row is active, auto-select the first empty/incomplete row
            if (activeRow < 0)
            {
                activeRow = _wordRowsContainer.GetFirstEmptyRowIndex();
                if (activeRow >= 0)
                {
                    _wordRowsContainer.SetActiveRow(activeRow);
                }
            }

            if (activeRow >= 0)
            {
                string currentWord = _wordRowsContainer.GetWord(activeRow);
                int maxLength = _wordRowsContainer.GetWordLength(activeRow);

                if (currentWord.Length < maxLength)
                {
                    string newWord = currentWord + letter;
                    _wordRowsContainer.SetWord(activeRow, newWord);

                    // Update word suggestion dropdown
                    UpdateWordSuggestionDropdown(activeRow, newWord, maxLength);

                    // If word is complete, validate it and hide dropdown
                    if (newWord.Length == maxLength)
                    {
                        _wordSuggestionDropdown?.Hide();
                        ValidateWord(activeRow, newWord);
                    }
                }
            }
        }

        private void HandleBackspacePressed()
        {
            int activeRow = _wordRowsContainer?.ActiveRowIndex ?? -1;
            if (activeRow >= 0)
            {
                // Check if this word is placed on the grid
                WordRowView row = _wordRowsContainer.GetRow(activeRow);
                bool wasPlaced = row != null && row.IsPlaced;

                string currentWord = _wordRowsContainer.GetWord(activeRow);
                if (currentWord.Length > 0 || wasPlaced)
                {
                    // Cancel placement mode if we're placing this word
                    if (_placementAdapter != null && _placementAdapter.IsInPlacementMode &&
                        _placementAdapter.PlacementWordRowIndex == activeRow)
                    {
                        _placementAdapter.CancelPlacementMode();
                    }

                    // If word was placed on grid, clear it from the grid first
                    if (wasPlaced && _placementAdapter != null)
                    {
                        _placementAdapter.ClearWordFromGrid(activeRow);
                        _wordRowsContainer.SetWordPlaced(activeRow, false);
                        UpdateReadyButtonState();
                    }

                    // Clear invalid feedback (red highlight) when modifying word
                    _wordRowsContainer.ClearInvalidFeedback(activeRow);

                    // Reset validity (word is now incomplete)
                    _wordRowsContainer.SetWordValid(activeRow, false);

                    // Only delete a letter if there are letters to delete
                    if (currentWord.Length > 0)
                    {
                        string newWord = currentWord.Substring(0, currentWord.Length - 1);
                        _wordRowsContainer.SetWord(activeRow, newWord);

                        // Update word suggestion dropdown
                        int maxLength = _wordRowsContainer.GetWordLength(activeRow);
                        UpdateWordSuggestionDropdown(activeRow, newWord, maxLength);
                    }
                }
            }
        }

        private void HandleWordRowCellClicked(int wordIndex, int letterIndex)
        {
            // When a word row cell is clicked, make that row active for editing
            _wordRowsContainer?.SetActiveRow(wordIndex);

            // Update dropdown for new active row
            if (_wordRowsContainer != null)
            {
                string currentWord = _wordRowsContainer.GetWord(wordIndex);
                int maxLength = _wordRowsContainer.GetWordLength(wordIndex);
                UpdateWordSuggestionDropdown(wordIndex, currentWord, maxLength);
            }
        }

        /// <summary>
        /// Updates the word suggestion dropdown based on current input.
        /// </summary>
        private void UpdateWordSuggestionDropdown(int rowIndex, string currentWord, int wordLength)
        {
            if (_wordSuggestionDropdown == null) return;

            // Set the word list for this word length
            WordListSO wordList = GetWordListForLength(wordLength);
            _wordSuggestionDropdown.SetWordList(wordList);
            _wordSuggestionDropdown.SetRequiredLength(wordLength);

            // Update filter
            _wordSuggestionDropdown.UpdateFilter(currentWord);

            // Position dropdown below the active word row
            PositionDropdownBelowRow(rowIndex);
        }

        /// <summary>
        /// Gets the appropriate word list ScriptableObject for the given word length.
        /// </summary>
        private WordListSO GetWordListForLength(int length)
        {
            return length switch
            {
                3 => _threeLetterWords,
                4 => _fourLetterWords,
                5 => _fiveLetterWords,
                6 => _sixLetterWords,
                _ => null
            };
        }

        /// <summary>
        /// Positions the dropdown below the specified word row.
        /// </summary>
        private void PositionDropdownBelowRow(int rowIndex)
        {
            if (_wordSuggestionDropdown == null || _wordRowsContainer == null) return;

            // Get the word row element
            WordRowView rowView = _wordRowsContainer.GetRow(rowIndex);
            if (rowView == null) return;

            VisualElement rowRoot = rowView.Root;
            if (rowRoot == null) return;

            // The dropdown is a child of placement-panel, so we need to calculate
            // the row's position relative to the placement panel
            VisualElement placementPanel = _setupWizardScreen?.Q<VisualElement>("placement-panel");
            if (placementPanel == null) return;

            // Get the world position of the row and convert to placement panel local coords
            Rect rowWorldBound = rowRoot.worldBound;
            Rect panelWorldBound = placementPanel.worldBound;

            // Calculate position relative to placement panel
            float relativeTop = rowWorldBound.yMax - panelWorldBound.y;
            float relativeLeft = rowWorldBound.x - panelWorldBound.x;

            // Set dropdown position
            _wordSuggestionDropdown.Root.style.position = Position.Absolute;
            _wordSuggestionDropdown.Root.style.top = relativeTop + 4; // 4px margin
            _wordSuggestionDropdown.Root.style.left = relativeLeft;
        }

        /// <summary>
        /// Handles when a word is selected from the suggestion dropdown.
        /// </summary>
        private void HandleWordSuggestionSelected(string word)
        {
            int activeRow = _wordRowsContainer?.ActiveRowIndex ?? -1;
            if (activeRow < 0) return;

            // Set the word in the active row
            _wordRowsContainer.SetWord(activeRow, word.ToUpper());

            // Hide dropdown
            _wordSuggestionDropdown?.Hide();

            // Word is complete, validate and auto-advance
            int expectedLength = _wordRowsContainer.GetWordLength(activeRow);
            if (word.Length == expectedLength)
            {
                ValidateWord(activeRow, word);
            }
        }

        private void ValidateWord(int rowIndex, string word)
        {
            if (_wordValidationService == null) return;

            int expectedLength = _wordRowsContainer.GetWordLength(rowIndex);
            bool isValid = _wordValidationService.ValidateWord(word, expectedLength);

            // Update validity state (controls placement button enabled)
            _wordRowsContainer.SetWordValid(rowIndex, isValid);

            if (isValid)
            {
                // Clear any previous invalid feedback
                _wordRowsContainer.ClearInvalidFeedback(rowIndex);

                // Auto-advance to next empty row
                int nextRow = _wordRowsContainer.GetFirstEmptyRowIndex();
                if (nextRow >= 0)
                {
                    _wordRowsContainer.SetActiveRow(nextRow);
                }
                else
                {
                    _wordRowsContainer.ClearActiveRow();
                }
            }
            else
            {
                // Show invalid word feedback (red highlight + shake)
                _wordRowsContainer.ShowInvalidFeedback(rowIndex);
                Debug.Log($"[UIFlowController] Invalid word: {word}");
            }
        }

        private void HandlePlacementRequested(int wordIndex, string word)
        {
            if (_placementAdapter == null) return;

            // Placement button should already be disabled for invalid words,
            // but double-check just in case
            if (!_wordRowsContainer.IsWordValid(wordIndex))
            {
                Debug.Log($"[UIFlowController] Cannot place invalid word: {word}");
                return;
            }

            // Hide dropdown when entering placement mode
            _wordSuggestionDropdown?.Hide();

            // Enter placement mode for this word
            _placementAdapter.EnterPlacementMode(wordIndex, word);
            Debug.Log($"[UIFlowController] Entered placement mode for word {wordIndex + 1}: {word}");
        }

        private void HandleWordCleared(int wordIndex)
        {
            if (_placementAdapter == null) return;

            // Hide dropdown when word is cleared
            _wordSuggestionDropdown?.Hide();

            // Clear invalid feedback (red highlight)
            _wordRowsContainer?.ClearInvalidFeedback(wordIndex);

            // Reset validity state (clears green highlight)
            _wordRowsContainer?.SetWordValid(wordIndex, false);

            // If we're currently placing this word, cancel placement mode first
            // This clears the preview (first letter on grid) before clearing the word
            if (_placementAdapter.IsInPlacementMode && _placementAdapter.PlacementWordRowIndex == wordIndex)
            {
                _placementAdapter.CancelPlacementMode();
            }

            // Clear word from grid if it was placed
            _placementAdapter.ClearWordFromGrid(wordIndex);

            // Update Ready button state (word was removed)
            UpdateReadyButtonState();
        }

        private void HandleGridCellClicked(int row, int col, TableCell cell)
        {
            // Only handle grid cell clicks
            if (cell.Kind != TableCellKind.GridCell) return;

            // Hide dropdown when clicking on grid
            _wordSuggestionDropdown?.Hide();

            if (_placementAdapter != null && _placementAdapter.IsInPlacementMode)
            {
                // Validate current word before allowing placement
                // (word may have changed since entering placement mode)
                int wordIndex = _placementAdapter.PlacementWordRowIndex;
                string currentWord = _wordRowsContainer.GetWord(wordIndex);
                int expectedLength = _wordRowsContainer.GetWordLength(wordIndex);

                if (_wordValidationService != null && !_wordValidationService.ValidateWord(currentWord, expectedLength))
                {
                    // Word is now invalid - cancel placement and show feedback
                    _placementAdapter.CancelPlacementMode();
                    _wordRowsContainer.ShowInvalidFeedback(wordIndex);
                    Debug.Log($"[UIFlowController] Cannot place - word changed to invalid: {currentWord}");
                    return;
                }

                // Convert table coordinates to grid coordinates
                // TableView reports clicks as (tableRow, tableCol), we need (gridCol, gridRow)
                int gridRow = row - 1;  // Subtract 1 for column header row
                int gridCol = col - 1;  // Subtract 1 for row header column

                _placementAdapter.HandleCellClick(gridCol, gridRow);
            }
        }

        private void HandleGridCellHovered(int row, int col, TableCell cell)
        {
            // Only handle grid cell hovers
            if (cell.Kind != TableCellKind.GridCell) return;

            if (_placementAdapter != null && _placementAdapter.IsInPlacementMode)
            {
                // Convert table coordinates to grid coordinates
                int gridRow = row - 1;
                int gridCol = col - 1;

                _placementAdapter.UpdatePlacementPreview(gridCol, gridRow);
            }
        }

        private void HandleWordPlacedOnGrid(int rowIndex, string word, System.Collections.Generic.List<UnityEngine.Vector2Int> positions)
        {
            Debug.Log($"[UIFlowController] Word '{word}' placed on grid at {positions.Count} positions");

            // Update Ready button state
            UpdateReadyButtonState();

            // The PlacementAdapter already calls _wordRowsContainer.SetWordPlaced
            // Check if all words are now placed
            if (_wordRowsContainer != null && _wordRowsContainer.AreAllWordsPlaced())
            {
                Debug.Log("[UIFlowController] All words placed! Ready to start game.");
            }
        }

        private void HandlePlacementCancelled()
        {
            Debug.Log("[UIFlowController] Placement cancelled");
        }

        private void ClearGridPlacements()
        {
            if (_placementAdapter != null)
            {
                _placementAdapter.ClearAllPlacedWords();
            }
            else
            {
                // Fallback: Reset all grid cells to fog state directly
                if (_tableModel == null || _tableLayout == null) return;

                for (int gridRow = 0; gridRow < _tableLayout.GridSize; gridRow++)
                {
                    for (int gridCol = 0; gridCol < _tableLayout.GridSize; gridCol++)
                    {
                        (int tableRow, int tableCol) = _tableLayout.GridToTable(gridRow, gridCol);
                        _tableModel.SetCellChar(tableRow, tableCol, '\0');
                        _tableModel.SetCellState(tableRow, tableCol, TableCellState.Fog);
                        _tableModel.SetCellOwner(tableRow, tableCol, CellOwner.None);
                    }
                }
            }
        }

        // === Placement Handlers ===

        private void HandleRandomWords()
        {
            DLYH.Audio.UIAudioManager.ButtonClick();

            if (_wordRowsContainer == null || _tableLayout == null) return;

            // Hide dropdown when filling random words
            _wordSuggestionDropdown?.Hide();

            if (_wordValidationService == null)
            {
                Debug.LogWarning("[UIFlowController] WordValidationService not initialized - check word list assignments");
                return;
            }

            // Only fill rows that don't have a complete valid word
            for (int i = 0; i < _tableLayout.WordCount; i++)
            {
                int expectedLength = _tableLayout.GetWordLength(i);
                string currentWord = _wordRowsContainer.GetWord(i);

                // Skip rows that already have a complete word of correct length
                if (currentWord.Length == expectedLength)
                {
                    // Still validate it to ensure placement button is enabled
                    ValidateWord(i, currentWord);
                    continue;
                }

                string randomWord = _wordValidationService.GetRandomWordOfLength(expectedLength);
                if (!string.IsNullOrEmpty(randomWord))
                {
                    _wordRowsContainer.SetWord(i, randomWord.ToUpper());
                    // Validate the word to enable placement button
                    ValidateWord(i, randomWord);
                }
            }

            // Clear active row since we auto-filled
            _wordRowsContainer.ClearActiveRow();
        }

        private void HandleRandomPlacement()
        {
            DLYH.Audio.UIAudioManager.ButtonClick();

            if (_wordRowsContainer == null || _placementAdapter == null || _tableLayout == null) return;

            // Clear existing placements first
            _placementAdapter.ClearAllPlacedWords();
            _wordRowsContainer.ClearAllPlacements();

            // Place words longest-first for better success on smaller grids
            // Get word indices sorted by word length descending
            System.Collections.Generic.List<int> wordIndices = new System.Collections.Generic.List<int>();
            for (int i = 0; i < _tableLayout.WordCount; i++)
            {
                wordIndices.Add(i);
            }

            // Sort by word length descending (longest first)
            wordIndices.Sort((a, b) =>
            {
                int lengthA = _wordRowsContainer.GetWordLength(a);
                int lengthB = _wordRowsContainer.GetWordLength(b);
                return lengthB.CompareTo(lengthA);
            });

            int successCount = 0;
            int invalidCount = 0;
            foreach (int wordIndex in wordIndices)
            {
                string word = _wordRowsContainer.GetWord(wordIndex);
                int expectedLength = _wordRowsContainer.GetWordLength(wordIndex);

                // Skip if word is incomplete
                if (string.IsNullOrEmpty(word) || word.Length != expectedLength)
                {
                    Debug.LogWarning($"[UIFlowController] Skipping random placement for word {wordIndex + 1} - word incomplete");
                    continue;
                }

                // Skip if word is invalid (not in dictionary)
                if (_wordValidationService != null && !_wordValidationService.ValidateWord(word, expectedLength))
                {
                    Debug.LogWarning($"[UIFlowController] Skipping random placement for word {wordIndex + 1}: {word} - invalid word");
                    _wordRowsContainer.ShowInvalidFeedback(wordIndex);
                    _wordRowsContainer.SetWordValid(wordIndex, false);
                    invalidCount++;
                    continue;
                }

                bool placed = _placementAdapter.PlaceWordRandomly(wordIndex, word);
                if (placed)
                {
                    successCount++;
                }
                else
                {
                    Debug.LogWarning($"[UIFlowController] Failed to place word {wordIndex + 1}: {word}");
                }
            }

            if (invalidCount > 0)
            {
                Debug.Log($"[UIFlowController] Random placement skipped {invalidCount} invalid word(s)");
            }

            Debug.Log($"[UIFlowController] Random placement complete: {successCount}/{_tableLayout.WordCount} words placed");
        }

        private void HandleReadyClicked()
        {
            DLYH.Audio.UIAudioManager.ButtonClick();

            if (_wordRowsContainer == null) return;

            // Check if all words are placed
            if (!_wordRowsContainer.AreAllWordsPlaced())
            {
                Debug.LogWarning("[UIFlowController] Cannot start game - not all words are placed");
                return;
            }

            // Branch based on game mode
            if (_currentGameMode == GameMode.Online)
            {
                // Start matchmaking - will call StartGameAfterMatchmaking when done
                StartOnlineMatchmaking();
            }
            else if (_currentGameMode == GameMode.JoinGame)
            {
                // Show join code entry - joiner needs to enter the host's code
                ShowJoinCodeEntryForJoinGame();
            }
            else
            {
                // Solo mode - go directly to gameplay
                TransitionToGameplay();
            }
        }

        /// <summary>
        /// Shows the join code entry UI for JoinGame mode.
        /// Called after the player has completed their setup (words placed).
        /// </summary>
        private void ShowJoinCodeEntryForJoinGame()
        {
            Debug.Log("[UIFlowController] Showing join code entry for JoinGame mode");

            // Show the join code card from the wizard
            if (_wizardManager != null)
            {
                _wizardManager.ShowJoinCodeCard();
            }
        }

        private void StartOnlineMatchmaking()
        {
            // Async wrapper to handle player record creation
            StartOnlineMatchmakingAsync().Forget();
        }

        private async UniTask StartOnlineMatchmakingAsync()
        {
            if (_networkingUIManager == null)
            {
                Debug.LogError("[UIFlowController] NetworkingUIManager not initialized");
                TransitionToGameplay(); // Fallback to solo AI
                return;
            }

            // Ensure player record exists before any networking
            if (_playerService != null)
            {
                string playerName = _wizardManager?.PlayerName ?? "Player";
                string playerId = await _playerService.EnsurePlayerRecordAsync(playerName);
                if (string.IsNullOrEmpty(playerId))
                {
                    Debug.LogError("[UIFlowController] Failed to create player record - falling back to solo AI");
                    TransitionToGameplay();
                    return;
                }
                Debug.Log($"[UIFlowController] Player record ready: {playerId}");
            }

            int gridSize = _wizardManager?.GridSize ?? 8;
            int difficultyIndex = _wizardManager?.Difficulty ?? 1;
            string difficulty = difficultyIndex switch
            {
                0 => "easy",
                2 => "hard",
                _ => "normal"
            };

            // Check which online mode was selected
            OnlineMode onlineMode = _wizardManager?.SelectedOnlineMode ?? OnlineMode.FindOpponent;

            if (onlineMode == OnlineMode.PrivateGame)
            {
                // Show WaitingRoom with join code for private game
                Debug.Log($"[UIFlowController] Starting private game - showing waiting room");
                await _networkingUIManager.ShowWaitingRoomAsync(gridSize, difficulty);
            }
            else
            {
                // Start quick matchmaking with anyone
                Debug.Log($"[UIFlowController] Starting matchmaking for {gridSize}x{gridSize} grid, {difficulty} difficulty");
                await _networkingUIManager.StartMatchmakingAsync(gridSize, difficulty);
            }
        }

        /// <summary>
        /// Called when user submits a join code from the wizard's join code card.
        /// </summary>
        private void HandleJoinCodeSubmitted(string code)
        {
            Debug.Log($"[UIFlowController] Join code submitted: {code}");
            HandleJoinCodeSubmittedAsync(code).Forget();
        }

        private async UniTask HandleJoinCodeSubmittedAsync(string code)
        {
            if (_networkingUIManager == null)
            {
                Debug.LogError("[UIFlowController] NetworkingUIManager not initialized");
                return;
            }

            // Ensure player record exists before joining
            if (_playerService != null)
            {
                string playerName = _wizardManager?.PlayerName ?? "Player";
                string playerId = await _playerService.EnsurePlayerRecordAsync(playerName);
                if (string.IsNullOrEmpty(playerId))
                {
                    Debug.LogError("[UIFlowController] Failed to create player record - cannot join game");
                    return;
                }
                Debug.Log($"[UIFlowController] Player record ready for join: {playerId}");
            }

            // Attempt to join the game with the code
            await _networkingUIManager.JoinWithCodeAsync(code);
        }

        private async UniTask StartGameAfterMatchmaking(NetworkingUIResult result)
        {
            // Store the result so TransitionToGameplay can use the phantom AI name
            _matchmakingResult = result;

            // Store the game code for state persistence
            _currentGameCode = result.GameCode;

            if (result.IsPhantomAI)
            {
                Debug.Log($"[UIFlowController] Starting game vs phantom AI: {result.OpponentName}");
            }
            else
            {
                Debug.Log($"[UIFlowController] Starting game vs real opponent: {result.OpponentName}");
                // TODO: Set up RemotePlayerOpponent with result.GameCode
            }

            // Proceed to gameplay
            TransitionToGameplay();
        }

        private async void TransitionToGameplay()
        {
            // Set up gameplay screen with player data
            if (_gameplayManager == null || _wizardManager == null) return;

            // Get player setup data
            string playerName = _wizardManager.PlayerName ?? "Player";
            Color playerColor = _wizardManager.PlayerColor;
            int playerGridSize = _wizardManager.GridSize;
            int playerWordCount = _wizardManager.WordCount;
            int difficultyIndex = _wizardManager.Difficulty;
            DifficultySetting playerDifficulty = GetDifficultySettingFromIndex(difficultyIndex);

            // Capture player's word placements BEFORE transitioning
            CapturePlayerSetupData(playerName, playerColor, playerGridSize, playerWordCount, playerDifficulty);

            // For all online games (including phantom AI) and JoinGame mode, save player setup to Supabase
            // Phantom AI games are stored in session_players and can be resumed, so they need state saved too
            bool isOnlineGame = _currentGameMode == GameMode.Online && _matchmakingResult != null;
            bool isJoinGame = _currentGameMode == GameMode.JoinGame && _matchmakingResult != null;
            if (isOnlineGame || isJoinGame)
            {
                await SavePlayerSetupToSupabaseAsync(_matchmakingResult.GameCode, _matchmakingResult.IsHost);
            }

            // For Solo mode, initialize AI FIRST to get their grid settings
            int opponentGridSize = playerGridSize;
            int opponentWordCount = playerWordCount;
            Color opponentColor = new Color(0.6f, 0.1f, 0.1f, 1f); // Default dark red
            string opponentName = "EXECUTIONER";
            List<WordPlacementData> opponentWordPlacements = new List<WordPlacementData>();

            bool shouldInitAI = _currentGameMode == GameMode.Solo ||
                (_currentGameMode == GameMode.Online && (_matchmakingResult == null || _matchmakingResult.IsPhantomAI));

            Debug.Log($"[UIFlowController] TransitionToGameplay - Mode: {_currentGameMode}, " +
                      $"MatchmakingResult: {(_matchmakingResult != null ? "present" : "null")}, " +
                      $"IsPhantomAI: {_matchmakingResult?.IsPhantomAI}, " +
                      $"ShouldInitAI: {shouldInitAI}");

            if (shouldInitAI)
            {
                // Initialize AI opponent - this generates their grid, words, and placements
                // For Online mode with phantom AI, we use the phantom name but still create LocalAIOpponent
                string phantomName = (_currentGameMode == GameMode.Online && _matchmakingResult != null)
                    ? _matchmakingResult.OpponentName
                    : null;

                Debug.Log($"[UIFlowController] Initializing AI opponent with phantom name: {phantomName ?? "null"}");
                await InitializeOpponentAsync(playerGridSize, playerWordCount, playerDifficulty, playerColor, phantomName);

                if (_opponent != null)
                {
                    // Get AI's actual settings
                    opponentGridSize = _opponent.GridSize;
                    opponentWordCount = _opponent.WordCount;
                    opponentColor = _opponent.OpponentColor;
                    opponentName = _opponent.OpponentName;
                    opponentWordPlacements = _opponent.WordPlacements;

                    Debug.Log($"[UIFlowController] AI settings: {opponentGridSize}x{opponentGridSize} grid, {opponentWordCount} words, {opponentWordPlacements.Count} placements");

                    // Debug: log all opponent words
                    for (int i = 0; i < opponentWordPlacements.Count; i++)
                    {
                        var p = opponentWordPlacements[i];
                        Debug.Log($"[UIFlowController] AI word {i}: '{p.Word}' at ({p.StartCol},{p.StartRow}) dir({p.DirCol},{p.DirRow})");
                    }

                    // For phantom AI games, save the AI's setup to Supabase so the game can be resumed
                    if (_currentGameMode == GameMode.Online && _matchmakingResult != null && _matchmakingResult.IsPhantomAI)
                    {
                        await SavePhantomAISetupToSupabaseAsync(_matchmakingResult.GameCode, opponentName, opponentColor,
                            opponentGridSize, opponentWordCount, opponentWordPlacements);
                    }
                }
            }
            else if (_currentGameMode == GameMode.JoinGame && _matchmakingResult != null)
            {
                // JoinGame mode - joiner has joined the host's game
                // The host's data will come from Supabase
                opponentName = "Host"; // Will be updated when we load host's data
                _waitingForOpponent = false; // Joiner doesn't wait - host was already there
                Debug.Log($"[UIFlowController] JoinGame mode - joined game {_matchmakingResult.GameCode}");
                // TODO: Load host's setup data from Supabase to get their grid, words, etc.
            }
            else if (_currentGameMode == GameMode.Online && _matchmakingResult != null && !_matchmakingResult.IsPhantomAI)
            {
                // Real online game - check if opponent has joined
                bool hasOpponent = !string.IsNullOrEmpty(_matchmakingResult.OpponentName) &&
                                   _matchmakingResult.OpponentName != "Waiting...";

                if (hasOpponent)
                {
                    // Opponent has joined - game can proceed
                    opponentName = _matchmakingResult.OpponentName;
                    _waitingForOpponent = false;
                    Debug.Log($"[UIFlowController] Online game with opponent: {opponentName}");
                }
                else
                {
                    // Waiting for opponent to join - block gameplay
                    opponentName = "Waiting...";
                    _waitingForOpponent = true;
                    Debug.Log("[UIFlowController] Online game waiting for opponent to join");
                }
            }

            // Calculate miss limits using the CORRECT formula:
            // Player's miss limit is based on OPPONENT's grid (what they're guessing on) + player's difficulty
            int playerMissLimit = DifficultyCalculator.CalculateMissLimitForPlayer(
                playerDifficulty, opponentGridSize, opponentWordCount);

            // AI's miss limit is based on PLAYER's grid + inverse difficulty
            DifficultySetting inverseDifficulty = GetInverseDifficulty(playerDifficulty);
            int opponentMissLimit = DifficultyCalculator.CalculateMissLimitForPlayer(
                inverseDifficulty, playerGridSize, playerWordCount);

            Debug.Log($"[UIFlowController] Miss limits - Player: {playerMissLimit} (vs {opponentGridSize}x{opponentGridSize}), " +
                      $"AI: {opponentMissLimit} (vs {playerGridSize}x{playerGridSize})");

            // Create player tab data
            PlayerTabData playerData = new PlayerTabData
            {
                Name = playerName,
                Color = playerColor,
                GridSize = playerGridSize,
                WordCount = playerWordCount,
                MissCount = 0,
                MissLimit = playerMissLimit,
                IsLocalPlayer = true
            };

            // Create opponent tab data (AI or other player)
            PlayerTabData opponentData = new PlayerTabData
            {
                Name = opponentName,
                Color = opponentColor,
                GridSize = opponentGridSize,
                WordCount = opponentWordCount,
                MissCount = 0,
                MissLimit = opponentMissLimit,
                IsLocalPlayer = false
            };

            _gameplayManager.SetPlayerData(playerData, opponentData);
            _gameplayManager.SetPlayerTurn(true); // Player goes first

            // Store opponent placements for end-game reveal
            _opponentWordPlacements = opponentWordPlacements;

            // Create ATTACK model based on AI's grid size (this is what player attacks)
            CreateAttackModel(opponentGridSize, opponentWordPlacements, opponentColor);

            // Create DEFENSE model based on player's grid (this is what AI attacks)
            CreateDefenseModel(playerGridSize, playerColor);

            // Set up the table views for gameplay
            SetupGameplayTableViews(opponentGridSize);

            // Initialize guess manager with BOTH sets of placement data
            InitializeGuessManagerWithBothSides(playerMissLimit, opponentMissLimit, opponentWordPlacements);

            // Set up word rows for attack view (opponent's words with underscores)
            // and defense view (player's words fully visible)
            SetupGameplayWordRowsWithOpponentData(playerWordCount, opponentWordCount, playerColor, opponentWordPlacements);

            // Set status message and turn state
            if (_waitingForOpponent)
            {
                _gameplayManager.SetStatusMessage("Waiting for opponent to join...", GameplayScreenManager.StatusType.Normal);

                // Session 4: Start polling for opponent to join
                // This handles private games where host starts before opponent joins
                if (_matchmakingResult != null && !string.IsNullOrEmpty(_matchmakingResult.GameCode))
                {
                    StartOpponentJoinPolling(_matchmakingResult.GameCode);
                }
            }
            else
            {
                _gameplayManager.SetStatusMessage("Game started! Tap a letter or cell to attack.", GameplayScreenManager.StatusType.Normal);
            }
            _isPlayerTurn = true;
            _isGameOver = false;

            // Clear extra turn queues for new game
            _playerExtraTurnQueue.Clear();
            _opponentExtraTurnQueue.Clear();

            // Reset guillotine stage tracking for new game
            _playerPreviousStage = 1;
            _opponentPreviousStage = 1;

            // Reset guillotine overlay styling for new game
            _guillotineOverlayManager?.ResetGameOverState();

            // Mark that a game is now in progress
            _hasActiveGame = true;

            // Show gameplay screen
            ShowGameplayScreen();

            Debug.Log("[UIFlowController] Transitioned to gameplay phase");
        }

        /// <summary>
        /// Captures player's word placements from the placement adapter.
        /// </summary>
        private void CapturePlayerSetupData(string playerName, Color playerColor, int gridSize, int wordCount, DifficultySetting difficulty)
        {
            _playerSetupData = new PlayerSetupData
            {
                PlayerName = playerName,
                PlayerColor = playerColor,
                GridSize = gridSize,
                WordCount = wordCount,
                DifficultyLevel = difficulty,
                WordLengths = TableLayout.GetStandardWordLengths(wordCount),
                PlacedWords = new List<WordPlacementData>()
            };

            // Get word placements from the placement adapter
            if (_placementAdapter != null)
            {
                List<(int rowIndex, string word, int startCol, int startRow, int dCol, int dRow)> placements =
                    _placementAdapter.GetAllWordPlacements();

                foreach ((int rowIndex, string word, int startCol, int startRow, int dCol, int dRow) placement in placements)
                {
                    WordPlacementData wordData = new WordPlacementData
                    {
                        Word = placement.word,
                        StartCol = placement.startCol,
                        StartRow = placement.startRow,
                        DirCol = placement.dCol,
                        DirRow = placement.dRow,
                        RowIndex = placement.rowIndex
                    };
                    _playerSetupData.PlacedWords.Add(wordData);
                }

                Debug.Log($"[UIFlowController] Captured {_playerSetupData.PlacedWords.Count} player word placements");
            }
        }

        /// <summary>
        /// Gets the inverse difficulty setting (Easy <-> Hard, Normal stays Normal).
        /// </summary>
        private DifficultySetting GetInverseDifficulty(DifficultySetting difficulty)
        {
            return difficulty switch
            {
                DifficultySetting.Easy => DifficultySetting.Hard,
                DifficultySetting.Hard => DifficultySetting.Easy,
                _ => DifficultySetting.Normal
            };
        }

        /// <summary>
        /// Creates the attack TableModel based on opponent's grid and word placements.
        /// This is the grid the player attacks (fog of war with opponent's hidden letters).
        /// </summary>
        private void CreateAttackModel(int opponentGridSize, List<WordPlacementData> opponentPlacements, Color opponentColor)
        {
            // Create layout for opponent's grid size
            _attackTableLayout = TableLayout.CreateForGameplay(opponentGridSize, opponentPlacements.Count);

            // Create the attack model
            _attackTableModel = new TableModel();
            _attackTableModel.Initialize(_attackTableLayout);

            Debug.Log($"[UIFlowController] CreateAttackModel: gridSize={opponentGridSize}, placements={opponentPlacements.Count}, " +
                      $"tableSize={_attackTableModel.Rows}x{_attackTableModel.Cols}");

            // Place opponent's letters in fog (hidden from player)
            foreach (WordPlacementData wordData in opponentPlacements)
            {
                Debug.Log($"[UIFlowController] Placing word '{wordData.Word}' at ({wordData.StartCol},{wordData.StartRow}) dir({wordData.DirCol},{wordData.DirRow})");

                for (int i = 0; i < wordData.Word.Length; i++)
                {
                    int col = wordData.StartCol + (i * wordData.DirCol);
                    int row = wordData.StartRow + (i * wordData.DirRow);
                    char letter = wordData.Word[i];

                    // Bounds check before conversion
                    if (row < 0 || row >= opponentGridSize || col < 0 || col >= opponentGridSize)
                    {
                        Debug.LogWarning($"[UIFlowController] Skipping out-of-bounds letter '{letter}' at grid ({col},{row}) - grid is {opponentGridSize}x{opponentGridSize}");
                        continue;
                    }

                    (int tableRow, int tableCol) = _attackTableLayout.GridToTable(row, col);

                    // Store the letter but hide it in fog
                    _attackTableModel.SetCellChar(tableRow, tableCol, letter);
                    _attackTableModel.SetCellState(tableRow, tableCol, TableCellState.Fog);
                    _attackTableModel.SetCellOwner(tableRow, tableCol, CellOwner.Opponent); // Opponent owns these cells
                }
            }

            // Set remaining cells to fog
            for (int gridRow = 0; gridRow < opponentGridSize; gridRow++)
            {
                for (int gridCol = 0; gridCol < opponentGridSize; gridCol++)
                {
                    (int tableRow, int tableCol) = _attackTableLayout.GridToTable(gridRow, gridCol);
                    TableCell cell = _attackTableModel.GetCell(tableRow, tableCol);

                    if (cell.TextChar == '\0') // No letter placed here
                    {
                        _attackTableModel.SetCellState(tableRow, tableCol, TableCellState.Fog);
                    }
                }
            }

            Debug.Log($"[UIFlowController] Created attack model: {opponentGridSize}x{opponentGridSize} with {opponentPlacements.Count} word placements");
        }

        /// <summary>
        /// Sets up table views for gameplay, creating a new attack TableView if needed.
        /// </summary>
        private void SetupGameplayTableViews(int opponentGridSize)
        {
            if (_attackTableModel == null)
            {
                Debug.LogError("[UIFlowController] SetupGameplayTableViews: _attackTableModel is null");
                return;
            }

            // Get the gameplay table container
            VisualElement gameplayTableContainer = _gameplayScreen?.Q<VisualElement>("table-container");
            if (gameplayTableContainer == null)
            {
                Debug.LogError("[UIFlowController] SetupGameplayTableViews: gameplay table-container not found");
                return;
            }

            // Create TableView if it doesn't exist (e.g., when resuming a game without going through setup)
            if (_tableView == null)
            {
                Debug.Log("[UIFlowController] Creating new TableView for resumed game");
                _tableView = new TableView(gameplayTableContainer);

                // Wire up cell click handlers for gameplay
                _tableView.OnCellClicked += HandleGridCellClicked;
                _tableView.OnCellHovered += HandleGridCellHovered;
            }

            _tableView.SetSetupMode(false); // Switch to gameplay mode colors

            // Rebind to attack model (this regenerates cells based on the model's dimensions)
            _tableView.Bind(_attackTableModel);

            // Reparent the TableView's visual content to the gameplay screen's grid area
            if (_tableView.TableRoot != null && _tableView.TableRoot.parent != gameplayTableContainer)
            {
                _tableView.TableRoot.RemoveFromHierarchy();
                gameplayTableContainer.Add(_tableView.TableRoot);
                Debug.Log("[UIFlowController] Reparented TableView to gameplay grid area (now showing attack grid)");
            }

            _gameplayManager?.SetTableView(_tableView);

            // Set table models for tab switching
            if (_attackTableModel != null && _defenseTableModel != null)
            {
                _gameplayManager?.SetTableModels(_attackTableModel, _defenseTableModel);
            }
        }

        /// <summary>
        /// Initializes the guess manager with placement data from both player and opponent.
        /// </summary>
        private void InitializeGuessManagerWithBothSides(int playerMissLimit, int opponentMissLimit, List<WordPlacementData> opponentPlacements)
        {
            _guessManager = new GameplayGuessManager();

            // Build player's placed letters dictionary
            Dictionary<Vector2Int, char> playerPlacedLetters = new Dictionary<Vector2Int, char>();
            HashSet<Vector2Int> playerPlacedPositions = new HashSet<Vector2Int>();

            if (_playerSetupData?.PlacedWords != null)
            {
                foreach (WordPlacementData wordData in _playerSetupData.PlacedWords)
                {
                    for (int i = 0; i < wordData.Word.Length; i++)
                    {
                        int col = wordData.StartCol + (i * wordData.DirCol);
                        int row = wordData.StartRow + (i * wordData.DirRow);
                        Vector2Int pos = new Vector2Int(col, row);

                        playerPlacedLetters[pos] = wordData.Word[i];
                        playerPlacedPositions.Add(pos);
                    }
                }
            }

            // Build opponent's placed letters dictionary and word list
            // IMPORTANT: Sort by word length to match word row ordering (shortest first)
            // This ensures word indices in GuessManager match word row indices in UI
            Dictionary<Vector2Int, char> opponentPlacedLetters = new Dictionary<Vector2Int, char>();
            HashSet<Vector2Int> opponentPlacedPositions = new HashSet<Vector2Int>();
            List<string> opponentWords = new List<string>();

            List<WordPlacementData> sortedOpponentPlacements = new List<WordPlacementData>(opponentPlacements);
            sortedOpponentPlacements.Sort((a, b) => a.Word.Length.CompareTo(b.Word.Length));

            Debug.Log($"[UIFlowController] InitializeGuessManager - opponentPlacements count: {sortedOpponentPlacements?.Count ?? -1}");
            foreach (WordPlacementData wordData in sortedOpponentPlacements)
            {
                Debug.Log($"[UIFlowController] InitializeGuessManager - opponent word: '{wordData.Word}' at ({wordData.StartCol},{wordData.StartRow}) dir({wordData.DirCol},{wordData.DirRow})");
                opponentWords.Add(wordData.Word);

                for (int i = 0; i < wordData.Word.Length; i++)
                {
                    int col = wordData.StartCol + (i * wordData.DirCol);
                    int row = wordData.StartRow + (i * wordData.DirRow);
                    Vector2Int pos = new Vector2Int(col, row);

                    opponentPlacedLetters[pos] = wordData.Word[i];
                    opponentPlacedPositions.Add(pos);
                }
            }

            // Log unique letters in opponent's words for debugging
            HashSet<char> uniqueOpponentLetters = new HashSet<char>();
            foreach (char c in opponentPlacedLetters.Values)
            {
                uniqueOpponentLetters.Add(char.ToUpper(c));
            }
            Debug.Log($"[UIFlowController] InitializeGuessManager - opponent letters: [{string.Join(",", uniqueOpponentLetters)}]");

            // Initialize with both sets of data, including opponent words for word guessing
            _guessManager.Initialize(
                playerPlacedLetters,    // What opponent attacks
                playerPlacedPositions,
                opponentPlacedLetters,  // What player attacks
                opponentPlacedPositions,
                playerMissLimit,
                opponentMissLimit,
                opponentWords,          // Opponent's actual words for word guessing
                word => _wordValidationService?.ValidateWord(word, word?.Length ?? 0) ?? true // Word validation callback
            );

            // Wire up events
            _guessManager.OnMissCountChanged += HandleMissCountChanged;
            _guessManager.OnGameOver += HandleGameOver;
            _guessManager.OnLetterHit += HandleLetterHit;
            _guessManager.OnLetterMiss += HandleLetterMiss;
            _guessManager.OnCoordinateHit += HandleCoordinateHit;
            _guessManager.OnCoordinateMiss += HandleCoordinateMiss;
            _guessManager.OnWordGuessProcessed += HandleWordGuessProcessed;
            _guessManager.OnWordSolved += HandleWordSolved;

            Debug.Log($"[UIFlowController] GuessManager initialized - Player has {playerPlacedPositions.Count} positions, " +
                      $"Opponent has {opponentPlacedPositions.Count} positions, {opponentWords.Count} words");
        }

        /// <summary>
        /// Sets up word rows for gameplay with correct opponent data.
        /// </summary>
        private void SetupGameplayWordRowsWithOpponentData(int playerWordCount, int opponentWordCount, Color playerColor, List<WordPlacementData> opponentPlacements)
        {
            // Get opponent's word lengths
            int[] opponentWordLengths = new int[opponentWordCount];
            for (int i = 0; i < opponentWordCount && i < opponentPlacements.Count; i++)
            {
                opponentWordLengths[i] = opponentPlacements[i].Word.Length;
            }

            // Get opponent's words (for display as underscores)
            string[] opponentWords = new string[opponentWordCount];
            for (int i = 0; i < opponentWordCount && i < opponentPlacements.Count; i++)
            {
                opponentWords[i] = opponentPlacements[i].Word;
            }

            // Create attack word rows (opponent's words - shown as underscores)
            WordRowsContainer attackWordRows = new WordRowsContainer(opponentWordCount, opponentWordLengths);
            attackWordRows.SetPlayerColor(playerColor);
            attackWordRows.SetGameplayMode(true);
            attackWordRows.SetWordsForGameplay(opponentWords); // Shows as underscores

            // Subscribe to word guess events
            attackWordRows.OnWordGuessSubmitted += HandleInlineWordGuessSubmitted;
            attackWordRows.OnWordGuessCancelled += HandleInlineWordGuessCancelled;
            attackWordRows.OnWordGuessStarted += HandleInlineWordGuessStarted;

            // Get player's word data
            int[] playerWordLengths = TableLayout.GetStandardWordLengths(playerWordCount);
            string[] playerWords = new string[playerWordCount];
            for (int i = 0; i < playerWordCount; i++)
            {
                playerWords[i] = _wordRowsContainer?.GetWord(i) ?? "";
            }

            // Create defense word rows (player's words - fully visible)
            _defenseWordRows = new WordRowsContainer(playerWordCount, playerWordLengths);
            _defenseWordRows.SetPlayerColor(playerColor);
            _defenseWordRows.SetGameplayMode(false); // NOT gameplay mode - shows full letters
            _defenseWordRows.HideAllButtons(); // No interaction on defense view
            for (int i = 0; i < playerWordCount; i++)
            {
                _defenseWordRows.SetWord(i, playerWords[i]);
            }

            // Set up the gameplay manager with both word row containers
            _gameplayManager?.SetWordRowContainers(attackWordRows, _defenseWordRows);

            // Store reference for letter reveal updates
            _attackWordRows = attackWordRows;

            // Apply viewport-aware sizing to match grid cell sizes
            if (_tableView != null)
            {
                int cellSize = _tableView.GetCalculatedCellSize();
                int fontSize = _tableView.GetCalculatedFontSize();
                if (cellSize > 0)
                {
                    attackWordRows.ApplyViewportAwareSizing(cellSize, fontSize);
                    _defenseWordRows?.ApplyViewportAwareSizing(cellSize, fontSize);
                    _gameplayManager?.ApplyKeyboardViewportSizing(cellSize, fontSize);
                }
            }

            Debug.Log($"[UIFlowController] Set up gameplay word rows: {opponentWordCount} attack words (opponent's), {playerWordCount} defense words (player's)");
        }

        /// <summary>
        /// Sets up gameplay word rows from saved state (for resumed games).
        /// Uses explicit placement data instead of _wordRowsContainer.
        /// </summary>
        private void SetupGameplayWordRowsFromSavedState(int playerWordCount, int opponentWordCount, Color playerColor, List<WordPlacementData> myPlacements, List<WordPlacementData> opponentPlacements)
        {
            // Sort opponent placements by word length (shortest first) to match standard word row order
            List<WordPlacementData> sortedOpponentPlacements = new List<WordPlacementData>(opponentPlacements);
            sortedOpponentPlacements.Sort((a, b) => a.Word.Length.CompareTo(b.Word.Length));

            // Get opponent word data from sorted placements
            int[] opponentWordLengths = new int[sortedOpponentPlacements.Count];
            string[] opponentWords = new string[sortedOpponentPlacements.Count];
            for (int i = 0; i < sortedOpponentPlacements.Count; i++)
            {
                opponentWordLengths[i] = sortedOpponentPlacements[i].Word.Length;
                opponentWords[i] = sortedOpponentPlacements[i].Word;
            }

            Debug.Log($"[UIFlowController] Setting up attack word rows with {sortedOpponentPlacements.Count} opponent words");
            for (int i = 0; i < opponentWords.Length; i++)
            {
                Debug.Log($"[UIFlowController] Opponent word {i}: '{opponentWords[i]}' (length {opponentWordLengths[i]})");
            }

            // Create attack word rows with actual opponent words
            WordRowsContainer attackWordRows = new WordRowsContainer(sortedOpponentPlacements.Count, opponentWordLengths);
            attackWordRows.SetPlayerColor(playerColor);
            attackWordRows.SetGameplayMode(true);
            attackWordRows.SetWordsForGameplay(opponentWords);

            // Subscribe to word guess events
            attackWordRows.OnWordGuessSubmitted += HandleInlineWordGuessSubmitted;
            attackWordRows.OnWordGuessCancelled += HandleInlineWordGuessCancelled;
            attackWordRows.OnWordGuessStarted += HandleInlineWordGuessStarted;

            // Sort placements by word length (shortest first) to match standard word row order
            // Word rows always display: slot 0 = 3 letters, slot 1 = 4 letters, etc.
            List<WordPlacementData> sortedPlacements = new List<WordPlacementData>(myPlacements);
            sortedPlacements.Sort((a, b) => a.Word.Length.CompareTo(b.Word.Length));

            // Get player's word data from sorted placements
            int[] playerWordLengths = new int[sortedPlacements.Count];
            string[] playerWords = new string[sortedPlacements.Count];
            for (int i = 0; i < sortedPlacements.Count; i++)
            {
                playerWordLengths[i] = sortedPlacements[i].Word.Length;
                playerWords[i] = sortedPlacements[i].Word;
            }

            // Create defense word rows (player's words - fully visible)
            _defenseWordRows = new WordRowsContainer(sortedPlacements.Count, playerWordLengths);
            _defenseWordRows.SetPlayerColor(playerColor);
            _defenseWordRows.SetGameplayMode(false); // NOT gameplay mode - shows full letters
            _defenseWordRows.HideAllButtons(); // No interaction on defense view
            for (int i = 0; i < sortedPlacements.Count; i++)
            {
                _defenseWordRows.SetWord(i, playerWords[i]);
            }

            // Set up the gameplay manager with both word row containers
            _gameplayManager?.SetWordRowContainers(attackWordRows, _defenseWordRows);

            // Store reference for letter reveal updates
            _attackWordRows = attackWordRows;

            // Apply viewport-aware sizing to match grid cell sizes
            if (_tableView != null)
            {
                int cellSize = _tableView.GetCalculatedCellSize();
                int fontSize = _tableView.GetCalculatedFontSize();
                if (cellSize > 0)
                {
                    attackWordRows.ApplyViewportAwareSizing(cellSize, fontSize);
                    _defenseWordRows?.ApplyViewportAwareSizing(cellSize, fontSize);
                    _gameplayManager?.ApplyKeyboardViewportSizing(cellSize, fontSize);
                }
            }

            Debug.Log($"[UIFlowController] Set up gameplay word rows from saved state: {opponentWordCount} attack words, {sortedPlacements.Count} defense words");
        }

        /// <summary>
        /// Converts UI difficulty index (0=Easy, 1=Normal, 2=Hard) to DifficultySetting enum.
        /// </summary>
        private DifficultySetting GetDifficultySettingFromIndex(int difficultyIndex)
        {
            return difficultyIndex switch
            {
                0 => DifficultySetting.Easy,
                1 => DifficultySetting.Normal,
                2 => DifficultySetting.Hard,
                _ => DifficultySetting.Normal
            };
        }

        private void OnDestroy()
        {
            if (_placementAdapter != null)
            {
                _placementAdapter.OnWordPlaced -= HandleWordPlacedOnGrid;
                _placementAdapter.OnPlacementCancelled -= HandlePlacementCancelled;
                _placementAdapter.Dispose();
            }
            if (_wordSuggestionDropdown != null)
            {
                _wordSuggestionDropdown.OnWordSelected -= HandleWordSuggestionSelected;
            }
            if (_wordRowsContainer != null)
            {
                _wordRowsContainer.OnPlacementRequested -= HandlePlacementRequested;
                _wordRowsContainer.OnWordCleared -= HandleWordCleared;
                _wordRowsContainer.OnLetterCellClicked -= HandleWordRowCellClicked;
                _wordRowsContainer.Dispose();
            }
            if (_tableView != null)
            {
                _tableView.OnCellClicked -= HandleGridCellClicked;
                _tableView.OnCellHovered -= HandleGridCellHovered;
                _tableView.Unbind();
            }
            if (_gameplayManager != null)
            {
                _gameplayManager.OnHamburgerClicked -= HandleGameplayHamburgerClicked;
                _gameplayManager.OnLetterKeyClicked -= HandleGameplayLetterClicked;
                _gameplayManager.OnGridCellClicked -= HandleGameplayGridCellClicked;
                _gameplayManager.OnWordGuessClicked -= HandleGameplayWordGuessClicked;
                _gameplayManager.OnShowGuillotineOverlay -= ShowGuillotineOverlay;
                _gameplayManager.Dispose();
            }
            if (_guillotineOverlayManager != null)
            {
                _guillotineOverlayManager.OnClosed -= HandleGuillotineOverlayClosed;
                _guillotineOverlayManager.Dispose();
            }
            // Clean up AI opponent
            if (_opponent != null)
            {
                _opponent.OnLetterGuess -= HandleOpponentLetterGuess;
                _opponent.OnCoordinateGuess -= HandleOpponentCoordinateGuess;
                _opponent.OnWordGuess -= HandleOpponentWordGuess;
                _opponent.OnThinkingStarted -= HandleOpponentThinkingStarted;
                _opponent.OnThinkingComplete -= HandleOpponentThinkingComplete;
                _opponent.Dispose();
                _opponent = null;
            }

            // Stop opponent join polling if active
            StopOpponentJoinPolling();
        }

        // ============================================================
        // SESSION 4: OPPONENT JOIN POLLING
        // ============================================================

        /// <summary>
        /// Starts polling for opponent to join a private game.
        /// Called when host starts game before opponent joins.
        /// Polling is tied to the current gameplay session - stops when user navigates away.
        /// </summary>
        private void StartOpponentJoinPolling(string gameCode)
        {
            StopOpponentJoinPolling(); // Stop any existing polling

            Debug.Log($"[UIFlowController] Starting opponent join polling for game {gameCode}");
            // Fire and forget - the async method manages its own lifecycle
            OpponentJoinPollingAsync(gameCode).Forget();
        }

        /// <summary>
        /// Stops the opponent join polling by setting the flag that causes the async loop to exit.
        /// </summary>
        private void StopOpponentJoinPolling()
        {
            // The async polling loop checks _waitingForOpponent and _hasActiveGame
            // Setting _hasActiveGame = false (done elsewhere) will cause the loop to exit
            // We also explicitly log when this is called for debugging
            Debug.Log("[UIFlowController] Stop opponent join polling requested");
        }

        /// <summary>
        /// Async method that polls Supabase for opponent joining a private game.
        /// Continues until opponent joins, game ends, or user navigates away.
        /// </summary>
        private async UniTaskVoid OpponentJoinPollingAsync(string gameCode)
        {
            Debug.Log($"[UIFlowController] Opponent join polling started for game {gameCode}");

            try
            {
                while (_waitingForOpponent && _hasActiveGame && !_isGameOver)
                {
                    // Wait between polls
                    await UniTask.Delay((int)(OPPONENT_JOIN_POLL_INTERVAL * 1000));

                    // Check if we should stop
                    if (!_waitingForOpponent || !_hasActiveGame || _isGameOver)
                    {
                        Debug.Log("[UIFlowController] Opponent polling stopped - game state changed");
                        break;
                    }

                    if (_gameSessionService == null)
                    {
                        Debug.LogWarning("[UIFlowController] GameSessionService not available for polling");
                        continue;
                    }

                    try
                    {
                        // Check player count
                        int playerCount = await _gameSessionService.GetPlayerCount(gameCode);
                        Debug.Log($"[UIFlowController] Poll: game {gameCode} has {playerCount} players");

                        if (playerCount >= 2)
                        {
                            // Opponent has joined! Get their info
                            GameSessionWithPlayers gameWithPlayers = await _gameSessionService.GetGameWithPlayers(gameCode);
                            if (gameWithPlayers?.Players != null)
                            {
                                // Find the opponent (player 2)
                                SessionPlayer opponent = null;
                                foreach (SessionPlayer player in gameWithPlayers.Players)
                                {
                                    if (player.PlayerNumber == 2)
                                    {
                                        opponent = player;
                                        break;
                                    }
                                }

                                string opponentName = opponent?.PlayerName ?? "Opponent";
                                Debug.Log($"[UIFlowController] Opponent '{opponentName}' joined game {gameCode}!");

                                // Handle opponent join
                                HandleOpponentJoined(opponentName, gameCode);
                                break;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.LogWarning($"[UIFlowController] Polling error: {ex.Message}");
                        // Continue polling despite errors
                    }
                }
            }
            catch (OperationCanceledException)
            {
                Debug.Log("[UIFlowController] Opponent polling cancelled");
            }

            Debug.Log("[UIFlowController] Opponent join polling ended");
        }

        /// <summary>
        /// Called when opponent joins a private game.
        /// Updates UI, enables gameplay, and refreshes Active Games list.
        /// </summary>
        private void HandleOpponentJoined(string opponentName, string gameCode)
        {
            Debug.Log($"[UIFlowController] Handling opponent join: {opponentName}");

            // Update state
            _waitingForOpponent = false;

            // Update matchmaking result for consistency
            if (_matchmakingResult != null)
            {
                _matchmakingResult.OpponentName = opponentName;
            }

            // Update gameplay screen
            _gameplayManager?.SetOpponentName(opponentName);
            _gameplayManager?.SetStatusMessage("Opponent joined! Tap a letter or cell to attack.", GameplayScreenManager.StatusType.Normal);

            // Play sound effect for opponent joining
            DLYH.Audio.UIAudioManager.Success();

            // Refresh Active Games list so it shows opponent name instead of "Waiting..."
            _activeGamesManager?.LoadMyActiveGamesAsync().Forget();

            // TODO Session 5: Load opponent's setup data from Supabase (grid, placements, etc.)
            // For now, gameplay can proceed but we'll need their actual data for proper attack grid

            Debug.Log($"[UIFlowController] Opponent '{opponentName}' joined - gameplay enabled");
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            // Prevent editor inspection from interfering with running UI
            // This empty OnValidate prevents Unity from re-serializing during play
        }
#endif
    }
}

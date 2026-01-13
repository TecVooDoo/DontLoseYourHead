using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.InputSystem;
using TecVooDoo.DontLoseYourHead.Core;
using TecVooDoo.DontLoseYourHead.UI;
using DLYH.Audio;
using DLYH.Telemetry;

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

        // Services
        private WordValidationService _wordValidationService;

        // Game mode tracking
        private GameMode _currentGameMode = GameMode.Solo;

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

        // Gameplay guess manager
        private GameplayGuessManager _guessManager;
        private bool _isPlayerTurn = true;

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

            Keyboard keyboard = Keyboard.current;
            if (keyboard == null)
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

            // Create screens - wizard first so menu is on top
            CreateSetupWizardScreen();
            CreateGameplayScreen();
            CreateMainMenuScreen();
            CreateFeedbackModal();
            CreateHamburgerMenu();

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

            // Set up inline settings
            SetupInlineSettings();

            // Set up trivia marquee
            _triviaLabel = _mainMenuScreen.Q<Label>("trivia-label");

            // Set version
            Label versionLabel = _mainMenuScreen.Q<Label>("version-label");
            if (versionLabel != null)
            {
                versionLabel.text = $"v{Application.version}";
            }
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
            _currentTriviaIndex = Random.Range(0, TRIVIA_FACTS.Length);
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
            if (_guessManager == null || !_isPlayerTurn)
            {
                Debug.Log($"[UIFlowController] Cannot process letter - manager null or not player turn");
                return;
            }

            GuessResult result = _guessManager.ProcessPlayerLetterGuess(letter);

            switch (result)
            {
                case GuessResult.Hit:
                    // Letter exists in words - start as Found (yellow)
                    // Will upgrade to Hit (player color) when coordinate is also known
                    _gameplayManager?.SetKeyboardLetterState(letter, LetterKeyState.Found);
                    _gameplayManager?.SetStatusMessage($"HIT! '{letter}' is in their words!", GameplayScreenManager.StatusType.Hit);
                    // TODO: End turn after UI updates
                    break;

                case GuessResult.Miss:
                    _gameplayManager?.SetKeyboardLetterState(letter, LetterKeyState.Miss);
                    _gameplayManager?.SetStatusMessage($"Miss! '{letter}' not in any word.", GameplayScreenManager.StatusType.Miss);
                    // Update miss count in UI
                    UpdateMissCountDisplay(true);
                    // TODO: End turn
                    break;

                case GuessResult.AlreadyGuessed:
                    _gameplayManager?.SetStatusMessage($"Already guessed '{letter}'", GameplayScreenManager.StatusType.Normal);
                    // Don't end turn
                    break;
            }
        }

        private void HandleGameplayGridCellClicked(int tableRow, int tableCol, bool isAttackGrid)
        {
            if (_guessManager == null || !_isPlayerTurn || !isAttackGrid)
            {
                Debug.Log($"[UIFlowController] Cannot process coordinate - manager null, not player turn, or not attack grid");
                return;
            }

            // Convert from table coordinates to grid coordinates
            // TableView reports table row/col which includes headers
            if (_tableLayout == null) return;

            (int gridRow, int gridCol) = _tableLayout.TableToGrid(tableRow, tableCol);
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
                    _gameplayManager?.SetStatusMessage($"HIT! Letter found at {colLetter}{displayRow}!", GameplayScreenManager.StatusType.Hit);
                    break;

                case GuessResult.Miss:
                    _gameplayManager?.SetStatusMessage($"Miss at {colLetter}{displayRow}", GameplayScreenManager.StatusType.Miss);
                    // Update miss count in UI
                    UpdateMissCountDisplay(true);
                    break;

                case GuessResult.AlreadyGuessed:
                    _gameplayManager?.SetStatusMessage($"Already guessed {colLetter}{displayRow}", GameplayScreenManager.StatusType.Normal);
                    // Don't end turn
                    break;
            }
        }

        private void HandleGameplayWordGuessClicked(int wordIndex)
        {
            // TODO: Wire to word guess mode
            Debug.Log($"[UIFlowController] Gameplay word guess clicked: word {wordIndex}");
        }

        private void InitializeGuessManager(int playerMissLimit, int opponentMissLimit)
        {
            _guessManager = new GameplayGuessManager();

            // Get placement data from the placement adapter
            if (_placementAdapter != null)
            {
                var placedLetters = _placementAdapter.PlacedLetters;
                var placedPositions = _placementAdapter.AllPlacedPositions;

                // For now, use the same data for both sides (testing mode)
                // In real gameplay, opponent would have their own placement data
                _guessManager.InitializeForTesting(placedLetters, placedPositions, playerMissLimit);

                Debug.Log($"[UIFlowController] GuessManager initialized with {placedPositions.Count} positions");
            }
            else
            {
                Debug.LogWarning("[UIFlowController] PlacementAdapter not available for GuessManager initialization");
            }

            // Wire up events
            _guessManager.OnMissCountChanged += HandleMissCountChanged;
            _guessManager.OnGameOver += HandleGameOver;
            _guessManager.OnLetterHit += HandleLetterHit;
            _guessManager.OnLetterMiss += HandleLetterMiss;
            _guessManager.OnCoordinateHit += HandleCoordinateHit;
            _guessManager.OnCoordinateMiss += HandleCoordinateMiss;
        }

        private void HandleMissCountChanged(bool isPlayer, int newMissCount, int missLimit)
        {
            Debug.Log($"[UIFlowController] Miss count changed - isPlayer: {isPlayer}, count: {newMissCount}/{missLimit}");
            UpdateMissCountDisplay(isPlayer);
        }

        private void HandleGameOver(bool playerLost)
        {
            Debug.Log($"[UIFlowController] GAME OVER - Player lost: {playerLost}");
            // TODO: Show game over screen with guillotine animation
        }

        private void HandleLetterHit(char letter, List<Vector2Int> positions)
        {
            Debug.Log($"[UIFlowController] Letter '{letter}' hit at {positions.Count} positions");

            // Reveal the letter in the word rows (WORDS TO FIND section)
            if (_attackWordRows != null)
            {
                Color playerColor = _wizardManager?.PlayerColor ?? ColorRules.SelectableColors[0];
                int revealed = _attackWordRows.RevealLetterInAllWords(letter, playerColor);
                Debug.Log($"[UIFlowController] Revealed letter '{letter}' in {revealed} word positions");
            }

            // Only reveal cells that are ALREADY yellow (Revealed state from coordinate guesses)
            // Cells still in Fog stay hidden - letter guesses don't reveal positions
            bool anyRevealed = false;
            foreach (Vector2Int pos in positions)
            {
                // Check if this cell is in Revealed state (yellow)
                if (IsGridCellInState(pos.x, pos.y, TableCellState.Revealed))
                {
                    RevealGridCellFully(pos.x, pos.y, letter);
                    anyRevealed = true;
                    Debug.Log($"[UIFlowController] Upgraded yellow cell ({pos.x}, {pos.y}) to show '{letter}'");
                }
            }

            // If any cells were revealed, upgrade keyboard letter from Found (yellow) to Hit (player color)
            if (anyRevealed)
            {
                _gameplayManager?.SetKeyboardLetterState(letter, LetterKeyState.Hit);
            }
        }

        /// <summary>
        /// Checks if a grid cell is in a specific state.
        /// </summary>
        private bool IsGridCellInState(int gridCol, int gridRow, TableCellState state)
        {
            if (_tableModel == null || _tableLayout == null) return false;

            (int tableRow, int tableCol) = _tableLayout.GridToTable(gridRow, gridCol);
            TableCell cell = _tableModel.GetCell(tableRow, tableCol);
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

            // Check if this letter has already been guessed
            bool letterAlreadyKnown = _guessManager?.HasPlayerGuessedLetter(letter) ?? false;
            Debug.Log($"[UIFlowController] Letter '{letter}' already known? {letterAlreadyKnown}");

            if (letterAlreadyKnown)
            {
                // Letter already guessed - show it immediately with Hit state (player color)
                RevealGridCellFully(position.x, position.y, letter);

                // Upgrade keyboard letter from Found (yellow) to Hit (player color)
                if (_gameplayManager != null)
                {
                    Debug.Log($"[UIFlowController] Upgrading keyboard letter '{letter}' to Hit (player color)");
                    _gameplayManager.SetKeyboardLetterState(letter, LetterKeyState.Hit);
                }
                else
                {
                    Debug.LogError("[UIFlowController] _gameplayManager is NULL - cannot upgrade keyboard letter!");
                }
            }
            else
            {
                // Letter not yet guessed - mark as Revealed (yellow) but don't show letter
                MarkGridCellCoordinateHit(position.x, position.y);
            }
        }

        private void HandleCoordinateMiss(Vector2Int position)
        {
            Debug.Log($"[UIFlowController] Coordinate miss at ({position.x}, {position.y})");
            MarkGridCellMiss(position.x, position.y);
        }

        /// <summary>
        /// Fully reveals a grid cell with a hit letter (player color + letter shown).
        /// Used when letter is already known or when letter is guessed.
        /// </summary>
        private void RevealGridCellFully(int gridCol, int gridRow, char letter)
        {
            if (_tableModel == null || _tableLayout == null) return;

            (int tableRow, int tableCol) = _tableLayout.GridToTable(gridRow, gridCol);
            _tableModel.SetCellChar(tableRow, tableCol, letter);
            _tableModel.SetCellState(tableRow, tableCol, TableCellState.Hit);

            Debug.Log($"[UIFlowController] Fully revealed cell ({gridCol}, {gridRow}) with letter '{letter}'");
        }

        /// <summary>
        /// Marks a grid cell as coordinate hit but letter unknown (yellow, no letter shown).
        /// </summary>
        private void MarkGridCellCoordinateHit(int gridCol, int gridRow)
        {
            if (_tableModel == null || _tableLayout == null) return;

            (int tableRow, int tableCol) = _tableLayout.GridToTable(gridRow, gridCol);
            // Don't set the letter - keep it hidden
            _tableModel.SetCellState(tableRow, tableCol, TableCellState.Revealed);

            Debug.Log($"[UIFlowController] Marked cell ({gridCol}, {gridRow}) as coordinate hit (yellow)");
        }

        /// <summary>
        /// Marks a grid cell as a miss (coordinate guess, no letter there) - red.
        /// </summary>
        private void MarkGridCellMiss(int gridCol, int gridRow)
        {
            if (_tableModel == null || _tableLayout == null) return;

            (int tableRow, int tableCol) = _tableLayout.GridToTable(gridRow, gridCol);
            _tableModel.SetCellState(tableRow, tableCol, TableCellState.Miss);

            Debug.Log($"[UIFlowController] Marked cell ({gridCol}, {gridRow}) as miss (red)");
        }

        /// <summary>
        /// Hides all grid cells by setting them to Fog state (fog of war).
        /// Used when transitioning to gameplay to hide the letters.
        /// </summary>
        /// <param name="gridSize">Size of the grid</param>
        private void HideAllGridCells(int gridSize)
        {
            if (_tableModel == null || _tableLayout == null) return;

            for (int gridRow = 0; gridRow < gridSize; gridRow++)
            {
                for (int gridCol = 0; gridCol < gridSize; gridCol++)
                {
                    (int tableRow, int tableCol) = _tableLayout.GridToTable(gridRow, gridCol);
                    _tableModel.SetCellChar(tableRow, tableCol, '\0');
                    _tableModel.SetCellState(tableRow, tableCol, TableCellState.Fog);
                }
            }

            Debug.Log($"[UIFlowController] Hidden all {gridSize}x{gridSize} grid cells (fog of war)");
        }

        /// <summary>
        /// Sets up the word rows for gameplay, showing opponent's words as underscores.
        /// </summary>
        private void SetupGameplayWordRows(int wordCount, Color playerColor)
        {
            if (_wordRowsContainer == null || _gameplayManager == null) return;

            // Get the opponent's words from the setup word rows
            // For testing, we use the player's own words as the "opponent"
            string[] opponentWords = new string[wordCount];
            int[] wordLengths = new int[wordCount];

            for (int i = 0; i < wordCount; i++)
            {
                opponentWords[i] = _wordRowsContainer.GetWord(i);
                wordLengths[i] = _wordRowsContainer.GetWordLength(i);
            }

            // Create a new WordRowsContainer for attack view (opponent's words)
            WordRowsContainer attackWordRows = new WordRowsContainer(wordCount, wordLengths);
            attackWordRows.SetPlayerColor(playerColor);
            attackWordRows.SetGameplayMode(true);
            attackWordRows.SetWordsForGameplay(opponentWords);

            // Set up the gameplay manager with the word rows
            _gameplayManager.SetWordRowContainers(attackWordRows, null); // null for defend view (not implemented yet)

            // Store reference for letter reveal updates
            _attackWordRows = attackWordRows;

            Debug.Log($"[UIFlowController] Set up gameplay word rows with {wordCount} words");
        }

        // Store reference to attack word rows for updates
        private WordRowsContainer _attackWordRows;

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

            _guillotineOverlayManager.Show(playerData, opponentData);
        }

        // === Navigation Handlers ===

        private void HandleGameModeSelected(GameMode mode)
        {
            // Reset wizard state for a new game
            _wizardManager?.Reset();

            _currentGameMode = mode;
            _hasActiveGame = true; // Mark that a game is now in progress
            _wizardManager?.SetGameMode(mode);
            ShowSetupWizard();
        }

        private void HandleContinueGameClicked()
        {
            // Return to the setup wizard where the game was in progress
            ShowSetupWizard();
        }

        private void HandleHowToPlayClicked()
        {
            // TODO: Implement how to play screen
        }

        private void HandleFeedbackClicked()
        {
            ShowFeedbackModal("Share Feedback", false, false);
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
            if (_feedbackModalContainer != null)
            {
                _feedbackModalContainer.AddToClassList("hidden");
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
            if (_hamburgerOverlay != null)
            {
                // Sync settings values before showing
                SyncHamburgerSettings();
                _hamburgerOverlay.RemoveFromClassList("hidden");
            }
        }

        private void HideHamburgerOverlay()
        {
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

            if (_mainMenuScreen != null)
            {
                _mainMenuScreen.style.display = DisplayStyle.None;
                _mainMenuScreen.visible = false;
            }
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

        private void InitializeTableForPlacement(SetupWizardUIManager.SetupData data)
        {
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

            // Apply size class to placement panel for keyboard/button scaling
            VisualElement placementPanel = _setupWizardScreen.Q<VisualElement>("placement-panel");
            if (placementPanel != null)
            {
                placementPanel.RemoveFromClassList("size-tiny");
                placementPanel.RemoveFromClassList("size-small");
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
            if (_wordRowsContainer == null) return;

            // Check if all words are placed
            if (!_wordRowsContainer.AreAllWordsPlaced())
            {
                Debug.LogWarning("[UIFlowController] Cannot start game - not all words are placed");
                return;
            }

            // Transition to gameplay phase
            TransitionToGameplay();
        }

        private void TransitionToGameplay()
        {
            // Set up gameplay screen with player data
            if (_gameplayManager != null && _wizardManager != null)
            {
                // Get player setup data
                string playerName = _wizardManager.PlayerName ?? "Player";
                Color playerColor = _wizardManager.PlayerColor;
                int gridSize = _wizardManager.GridSize;
                int wordCount = _wizardManager.WordCount;
                int difficulty = _wizardManager.Difficulty;

                // Calculate miss limits using the proper formula from DifficultyCalculator
                // Player's miss limit is based on OPPONENT's grid (what they're guessing on) + player's difficulty
                // For Solo mode, AI picks its own grid settings based on player difficulty
                DifficultySetting playerDifficulty = GetDifficultySettingFromIndex(difficulty);

                // TODO: In multiplayer, use actual opponent grid settings
                // For now in solo mode, AI uses same grid/words as player (will be fixed when AI setup is integrated)
                int opponentGridSize = gridSize;   // AI's grid that player attacks
                int opponentWordCount = wordCount; // AI's word count

                int playerMissLimit = DifficultyCalculator.CalculateMissLimitForPlayer(
                    playerDifficulty, opponentGridSize, opponentWordCount);

                // AI's miss limit is based on PLAYER's grid + Normal difficulty
                int opponentMissLimit = DifficultyCalculator.CalculateMissLimitForPlayer(
                    DifficultySetting.Normal, gridSize, wordCount);

                // Create player tab data
                PlayerTabData playerData = new PlayerTabData
                {
                    Name = playerName,
                    Color = playerColor,
                    GridSize = gridSize,
                    WordCount = wordCount,
                    MissCount = 0,
                    MissLimit = playerMissLimit,
                    IsLocalPlayer = true
                };

                // Create opponent tab data (AI or other player)
                PlayerTabData opponentData = new PlayerTabData
                {
                    Name = _currentGameMode == GameMode.Solo ? "EXECUTIONER" : "Opponent",
                    Color = ColorRules.SelectableColors[1], // TODO: Get actual opponent color
                    GridSize = gridSize, // TODO: Could be different for asymmetric play
                    WordCount = wordCount, // TODO: Could be different
                    MissCount = 0,
                    MissLimit = opponentMissLimit,
                    IsLocalPlayer = false
                };

                _gameplayManager.SetPlayerData(playerData, opponentData);
                _gameplayManager.SetPlayerTurn(true); // Player goes first

                // Set up the table view for gameplay
                if (_tableView != null)
                {
                    _tableView.SetSetupMode(false); // Switch to gameplay mode colors

                    // Reparent the TableView's visual content to the gameplay screen's grid area
                    VisualElement gameplayTableContainer = _gameplayScreen.Q<VisualElement>("table-container");
                    if (gameplayTableContainer != null && _tableView.TableRoot != null)
                    {
                        // Remove from old parent (setup wizard) and add to new parent (gameplay)
                        _tableView.TableRoot.RemoveFromHierarchy();
                        gameplayTableContainer.Add(_tableView.TableRoot);
                        Debug.Log("[UIFlowController] Reparented TableView to gameplay grid area");
                    }

                    _gameplayManager.SetTableView(_tableView);
                }

                // TODO: Create attack grid model for opponent's board
                // For now we just use the player's table model (shows player's own grid)
                // In actual gameplay, attack tab would show opponent's fog-of-war grid
                if (_tableModel != null)
                {
                    _gameplayManager.SetTableModels(_tableModel, _tableModel);
                }

                // Initialize guess manager with placement data
                InitializeGuessManager(playerMissLimit, opponentMissLimit);

                // Hide all grid cells for testing (fog of war)
                // In real gameplay, the attack grid would start hidden
                // For now, we hide the player's own grid to simulate attacking opponent
                HideAllGridCells(gridSize);

                // Set up word rows for attack view (opponent's words with underscores)
                SetupGameplayWordRows(wordCount, playerColor);

                // Set status message
                _gameplayManager.SetStatusMessage("Game started! Tap a letter or cell to attack.", GameplayScreenManager.StatusType.Normal);
                _isPlayerTurn = true;
            }

            // Show gameplay screen
            ShowGameplayScreen();

            Debug.Log("[UIFlowController] Transitioned to gameplay phase");
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
                _guillotineOverlayManager.Dispose();
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            // Prevent editor inspection from interfering with running UI
            // This empty OnValidate prevents Unity from re-serializing during play
        }
#endif
    }

    /// <summary>
    /// Manages the setup wizard UI without being a MonoBehaviour.
    /// Operates directly on the VisualElement tree.
    /// </summary>
    public class SetupWizardUIManager
    {
        // Events
        public event System.Action<SetupData> OnSetupComplete;
        public event System.Action OnQuickSetupRequested;

        // Data class
        public class SetupData
        {
            public string PlayerName;
            public Color PlayerColor;
            public int GridSize;
            public int WordCount;
            public int Difficulty;
            public GameMode GameMode;
            public bool UseQuickSetup;
        }

        // Defaults
        private const string DEFAULT_PLAYER_NAME = "Player";
        private const int DEFAULT_GRID_SIZE = 6;
        private const int DEFAULT_WORD_COUNT = 3;
        private const int DEFAULT_DIFFICULTY = 1;

        // Settings constants (duplicate from UIFlowController for access)
        private const string PREFS_QWERTY_KEYBOARD = "DLYH_QwertyKeyboard";

        // Root element
        private VisualElement _root;

        // Cards
        private VisualElement _cardProfile;
        private VisualElement _cardGridSize;
        private VisualElement _cardWordCount;
        private VisualElement _cardDifficulty;
        private VisualElement _cardBoardSetup;
        private VisualElement _cardJoinCode;
        private VisualElement _cardsContainer;
        private VisualElement _placementPanel;

        // Game mode (set from main menu)
        private GameMode _gameMode = GameMode.Solo;

        // Board setup mode cards
        private VisualElement _setupQuickCard;
        private VisualElement _setupManualCard;

        // Join code elements
        private TextField _joinCodeInput;
        private Button _joinCodeSubmitBtn;

        // Inputs
        private TextField _playerNameInput;
        private VisualElement _colorPicker;

        // State
        private string _playerName = DEFAULT_PLAYER_NAME;
        private Color _playerColor;
        private int _selectedColorIndex = 0;
        private int _gridSize = DEFAULT_GRID_SIZE;
        private int _wordCount = DEFAULT_WORD_COUNT;
        private int _difficulty = DEFAULT_DIFFICULTY;
        private bool _useQuickSetup = true;

        // Public properties for accessing current wizard state
        public string PlayerName => _playerName;
        public Color PlayerColor => _playerColor;
        public int GridSize => _gridSize;
        public int WordCount => _wordCount;
        public int Difficulty => _difficulty;
        public bool UseQuickSetup => _useQuickSetup;
        public GameMode CurrentGameMode => _gameMode;

        // UI arrays
        private Button[] _gridButtons;
        private Button[] _wordCountButtons;
        private Button[] _difficultyButtons;
        private VisualElement[] _colorSwatches;

        // Content/Summary elements
        private VisualElement _profileContent;
        private VisualElement _profileSummary;
        private VisualElement _gridContent;
        private VisualElement _gridSummary;
        private VisualElement _wordsContent;
        private VisualElement _wordsSummary;
        private VisualElement _difficultyContent;
        private VisualElement _difficultySummary;

        // Summary labels
        private Label _profileSummaryText;
        private VisualElement _profileColorBadge;
        private Label _gridSummaryText;
        private Label _wordsSummaryText;
        private Label _difficultySummaryText;

        public SetupWizardUIManager(VisualElement root)
        {
            _root = root;
            _playerColor = ColorRules.SelectableColors[0];
            Initialize();
        }

        private void Initialize()
        {
            CacheElements();
            SetupColorPicker();
            SetupGridSizeButtons();
            SetupWordCountButtons();
            SetupDifficultyButtons();
            SetupBoardSetupCards();
            SetupJoinCodeCard();
            SetupActionButtons();
            SetupLetterKeyboard();
            SetupCollapsedCardClickHandlers();

            // Set initial input value
            if (_playerNameInput != null)
            {
                _playerNameInput.value = DEFAULT_PLAYER_NAME;
                _playerNameInput.RegisterValueChangedCallback(OnPlayerNameChanged);
            }

            // Initial visibility
            HideElement(_cardGridSize);
            HideElement(_cardWordCount);
            HideElement(_cardDifficulty);
            HideElement(_cardBoardSetup);
            HideElement(_cardJoinCode);
            HideElement(_placementPanel);

            ShowElement(_cardProfile);
            ExpandCard(_cardProfile, _profileContent, _profileSummary);
        }

        private void CacheElements()
        {
            _cardsContainer = _root.Q<VisualElement>("cards-container");
            _cardProfile = _root.Q<VisualElement>("card-profile");
            _cardGridSize = _root.Q<VisualElement>("card-grid-size");
            _cardWordCount = _root.Q<VisualElement>("card-word-count");
            _cardDifficulty = _root.Q<VisualElement>("card-difficulty");
            _cardBoardSetup = _root.Q<VisualElement>("card-board-setup");
            _cardJoinCode = _root.Q<VisualElement>("card-join-code");
            _placementPanel = _root.Q<VisualElement>("placement-panel");

            _setupQuickCard = _root.Q<VisualElement>("setup-quick");
            _setupManualCard = _root.Q<VisualElement>("setup-manual");

            // Join code elements
            _joinCodeInput = _root.Q<TextField>("join-code-input");
            _joinCodeSubmitBtn = _root.Q<Button>("btn-join-code-submit");

            _playerNameInput = _root.Q<TextField>("player-name-input");
            _colorPicker = _root.Q<VisualElement>("color-picker");

            _profileContent = _root.Q<VisualElement>("profile-content");
            _profileSummary = _root.Q<VisualElement>("profile-summary");
            _gridContent = _root.Q<VisualElement>("grid-content");
            _gridSummary = _root.Q<VisualElement>("grid-summary");
            _wordsContent = _root.Q<VisualElement>("words-content");
            _wordsSummary = _root.Q<VisualElement>("words-summary");
            _difficultyContent = _root.Q<VisualElement>("difficulty-content");
            _difficultySummary = _root.Q<VisualElement>("difficulty-summary");

            _profileSummaryText = _root.Q<Label>("profile-summary-text");
            _profileColorBadge = _root.Q<VisualElement>("profile-color-badge");
            _gridSummaryText = _root.Q<Label>("grid-summary-text");
            _wordsSummaryText = _root.Q<Label>("words-summary-text");
            _difficultySummaryText = _root.Q<Label>("difficulty-summary-text");
        }

        private void SetupColorPicker()
        {
            if (_colorPicker == null) return;

            _colorPicker.Clear();
            _colorSwatches = new VisualElement[ColorRules.SelectableColors.Length];

            for (int i = 0; i < ColorRules.SelectableColors.Length; i++)
            {
                int colorIndex = i;
                Color color = ColorRules.SelectableColors[i];

                VisualElement swatch = new VisualElement();
                swatch.AddToClassList("color-swatch");
                swatch.style.backgroundColor = color;
                swatch.RegisterCallback<ClickEvent>(evt => SelectColor(colorIndex));

                _colorSwatches[i] = swatch;
                _colorPicker.Add(swatch);
            }

            SelectColor(0);
        }

        private void SetupGridSizeButtons()
        {
            _gridButtons = new Button[7];
            int[] sizes = { 6, 7, 8, 9, 10, 11, 12 };

            for (int i = 0; i < sizes.Length; i++)
            {
                int size = sizes[i];
                Button button = _root.Q<Button>($"grid-{size}");
                if (button != null)
                {
                    _gridButtons[i] = button;
                    button.clicked += () => SelectGridSize(size);
                }
            }
        }

        private void SetupWordCountButtons()
        {
            _wordCountButtons = new Button[2];

            Button btn3 = _root.Q<Button>("words-3");
            Button btn4 = _root.Q<Button>("words-4");

            if (btn3 != null)
            {
                _wordCountButtons[0] = btn3;
                btn3.clicked += () => SelectWordCount(3);
            }
            if (btn4 != null)
            {
                _wordCountButtons[1] = btn4;
                btn4.clicked += () => SelectWordCount(4);
            }
        }

        private void SetupDifficultyButtons()
        {
            _difficultyButtons = new Button[3];

            Button easy = _root.Q<Button>("diff-easy");
            Button normal = _root.Q<Button>("diff-normal");
            Button hard = _root.Q<Button>("diff-hard");

            if (easy != null)
            {
                _difficultyButtons[0] = easy;
                easy.clicked += () => SelectDifficulty(0);
            }
            if (normal != null)
            {
                _difficultyButtons[1] = normal;
                normal.clicked += () => SelectDifficulty(1);
            }
            if (hard != null)
            {
                _difficultyButtons[2] = hard;
                hard.clicked += () => SelectDifficulty(2);
            }
        }

        private void SetupBoardSetupCards()
        {
            if (_setupQuickCard != null)
            {
                _setupQuickCard.RegisterCallback<ClickEvent>(evt => SelectBoardSetupMode(true));
            }
            if (_setupManualCard != null)
            {
                _setupManualCard.RegisterCallback<ClickEvent>(evt => SelectBoardSetupMode(false));
            }
        }

        private void SetupJoinCodeCard()
        {
            if (_joinCodeSubmitBtn != null)
            {
                _joinCodeSubmitBtn.clicked += HandleJoinCodeSubmit;
            }

            // Auto-uppercase the join code input
            if (_joinCodeInput != null)
            {
                _joinCodeInput.RegisterValueChangedCallback(evt =>
                {
                    string upper = evt.newValue.ToUpper();
                    if (upper != evt.newValue)
                    {
                        _joinCodeInput.SetValueWithoutNotify(upper);
                    }
                });
            }
        }

        private void HandleJoinCodeSubmit()
        {
            string code = _joinCodeInput?.value?.Trim().ToUpper() ?? "";

            if (string.IsNullOrEmpty(code) || code.Length < 4)
            {
                Debug.LogWarning("[SetupWizard] Invalid join code - must be at least 4 characters");
                return;
            }

            Debug.Log($"[SetupWizard] Attempting to join game with code: {code}");

            // TODO: Actually join the game via networking
            // For now, just log and show a placeholder message
            // In the future this will:
            // 1. Send the code to the server
            // 2. Receive game info (grid size, word count)
            // 3. Transition to the placement panel with received settings
        }

        private void SetupActionButtons()
        {
            // Note: btn-ready is now handled by UIFlowController.HandleReadyClicked()
            // to transition to gameplay. Do NOT wire it here.
            Button backToSettings = _root.Q<Button>("btn-back-to-settings");

            if (backToSettings != null)
            {
                backToSettings.clicked += ShowSettingsCards;
            }
        }

        private void SetupLetterKeyboard()
        {
            VisualElement keyboard = _root.Q<VisualElement>("letter-keyboard");
            if (keyboard == null) return;

            keyboard.Clear();

            // Check QWERTY preference
            bool useQwerty = PlayerPrefs.GetInt(PREFS_QWERTY_KEYBOARD, 0) == 1;

            // Create 3-row keyboard layout
            string[] rows;
            if (useQwerty)
            {
                // QWERTY layout
                rows = new string[] { "QWERTYUIOP", "ASDFGHJKL", "ZXCVBNM" };
            }
            else
            {
                // Alphabetical layout
                // Row 1: A-I (9 letters)
                // Row 2: J-R (9 letters)
                // Row 3: S-Z (8 letters)
                rows = new string[] { "ABCDEFGHI", "JKLMNOPQR", "STUVWXYZ" };
            }

            for (int rowIndex = 0; rowIndex < rows.Length; rowIndex++)
            {
                VisualElement rowElement = new VisualElement();
                rowElement.AddToClassList("keyboard-row");

                foreach (char letter in rows[rowIndex])
                {
                    Button key = new Button();
                    key.text = letter.ToString();
                    key.AddToClassList("letter-key");
                    // Note: Letter key clicks are handled by UIFlowController via HandleLetterKeyPressed
                    rowElement.Add(key);
                }

                // Add backspace button at the end of the last row
                if (rowIndex == rows.Length - 1)
                {
                    Button backspaceBtn = new Button();
                    backspaceBtn.text = "←";
                    backspaceBtn.tooltip = "Backspace";
                    backspaceBtn.AddToClassList("letter-key");
                    backspaceBtn.AddToClassList("backspace-key");
                    // Note: Backspace click is wired up by UIFlowController
                    rowElement.Add(backspaceBtn);
                }

                keyboard.Add(rowElement);
            }
        }

        /// <summary>
        /// Refreshes the letter keyboard layout based on current QWERTY preference.
        /// Call this when the QWERTY setting changes while the wizard is open.
        /// </summary>
        public void RefreshKeyboardLayout()
        {
            // Re-setup the keyboard with the new layout
            SetupLetterKeyboard();
        }

        private void SetupCollapsedCardClickHandlers()
        {
            _cardProfile?.RegisterCallback<ClickEvent>(evt =>
            {
                if (_cardProfile.ClassListContains("collapsed"))
                {
                    ExpandCard(_cardProfile, _profileContent, _profileSummary);
                    CollapseOtherCards(_cardProfile);
                    evt.StopPropagation();
                }
            });

            _cardGridSize?.RegisterCallback<ClickEvent>(evt =>
            {
                if (_cardGridSize.ClassListContains("collapsed"))
                {
                    ExpandCard(_cardGridSize, _gridContent, _gridSummary);
                    CollapseOtherCards(_cardGridSize);
                    evt.StopPropagation();
                }
            });

            _cardWordCount?.RegisterCallback<ClickEvent>(evt =>
            {
                if (_cardWordCount.ClassListContains("collapsed"))
                {
                    ExpandCard(_cardWordCount, _wordsContent, _wordsSummary);
                    CollapseOtherCards(_cardWordCount);
                    evt.StopPropagation();
                }
            });

            _cardDifficulty?.RegisterCallback<ClickEvent>(evt =>
            {
                if (_cardDifficulty.ClassListContains("collapsed"))
                {
                    ExpandCard(_cardDifficulty, _difficultyContent, _difficultySummary);
                    CollapseOtherCards(_cardDifficulty);
                    evt.StopPropagation();
                }
            });
        }

        // === Selection Handlers ===

        private void SelectColor(int index)
        {
            _selectedColorIndex = index;
            _playerColor = ColorRules.SelectableColors[index];

            for (int i = 0; i < _colorSwatches.Length; i++)
            {
                if (_colorSwatches[i] != null)
                {
                    if (i == index)
                        _colorSwatches[i].AddToClassList("selected");
                    else
                        _colorSwatches[i].RemoveFromClassList("selected");
                }
            }

            _playerName = _playerNameInput?.value ?? DEFAULT_PLAYER_NAME;
            RevealNextCardAfterProfile();
        }

        private void OnPlayerNameChanged(ChangeEvent<string> evt)
        {
            _playerName = evt.newValue;
            if (!string.IsNullOrEmpty(evt.newValue))
            {
                RevealNextCardAfterProfile();
            }
        }

        /// <summary>
        /// Reveals the appropriate next card after Profile based on game mode.
        /// For JoinGame mode, shows Difficulty card (skips Grid and Words).
        /// For other modes, shows Grid Size card.
        /// </summary>
        private void RevealNextCardAfterProfile()
        {
            if (_gameMode == GameMode.JoinGame)
            {
                // Skip Grid and Words for Join Game - go straight to Difficulty
                RevealNextCard(_cardDifficulty);
            }
            else
            {
                RevealNextCard(_cardGridSize);
            }
        }

        private void SelectGridSize(int size)
        {
            _gridSize = size;
            UpdateButtonSelection(_gridButtons, size - 6);
            UpdateGridSummary();

            _playerName = _playerNameInput?.value ?? DEFAULT_PLAYER_NAME;
            UpdateProfileSummary();
            CollapseCard(_cardProfile, _profileContent, _profileSummary);

            RevealNextCard(_cardWordCount);
        }

        private void SelectWordCount(int count)
        {
            _wordCount = count;
            UpdateButtonSelection(_wordCountButtons, count - 3);
            UpdateWordsSummary();

            CollapseCard(_cardGridSize, _gridContent, _gridSummary);
            RevealNextCard(_cardDifficulty);
        }

        private void SelectDifficulty(int difficulty)
        {
            _difficulty = difficulty;
            UpdateButtonSelection(_difficultyButtons, difficulty);
            UpdateDifficultySummary();

            if (_gameMode == GameMode.JoinGame)
            {
                // For Join Game, collapse Profile and show Join Code card
                CollapseCard(_cardProfile, _profileContent, _profileSummary);
                CollapseCard(_cardDifficulty, _difficultyContent, _difficultySummary);
                RevealNextCard(_cardJoinCode);
            }
            else
            {
                CollapseCard(_cardWordCount, _wordsContent, _wordsSummary);
                RevealNextCard(_cardBoardSetup);
            }
        }

        private void SelectBoardSetupMode(bool useQuickSetup)
        {
            _useQuickSetup = useQuickSetup;

            CollapseCard(_cardDifficulty, _difficultyContent, _difficultySummary);

            if (_setupQuickCard != null)
            {
                if (useQuickSetup)
                    _setupQuickCard.AddToClassList("selected");
                else
                    _setupQuickCard.RemoveFromClassList("selected");
            }
            if (_setupManualCard != null)
            {
                if (!useQuickSetup)
                    _setupManualCard.AddToClassList("selected");
                else
                    _setupManualCard.RemoveFromClassList("selected");
            }

            // Proceed to placement panel
            _playerName = _playerNameInput?.value ?? DEFAULT_PLAYER_NAME;
            ShowPlacementPanel();
        }

        // === Action Handlers ===

        private void ShowSettingsCards()
        {
            ShowElement(_cardsContainer);
            HideElement(_placementPanel);
        }

        private void ShowPlacementPanel()
        {
            HideElement(_cardsContainer);
            ShowElement(_placementPanel);

            Label nameDisplay = _root.Q<Label>("player-name-display");
            VisualElement colorBadge = _root.Q<VisualElement>("player-color-badge");

            if (nameDisplay != null)
                nameDisplay.text = _playerName.ToUpperInvariant();
            if (colorBadge != null)
                colorBadge.style.backgroundColor = _playerColor;

            // Fire setup complete to initialize table
            SetupData data = new SetupData
            {
                PlayerName = _playerName,
                PlayerColor = _playerColor,
                GridSize = _gridSize,
                WordCount = _wordCount,
                Difficulty = _difficulty,
                GameMode = _gameMode,
                UseQuickSetup = _useQuickSetup
            };
            OnSetupComplete?.Invoke(data);

            // If Quick Setup mode, auto-fill with random words and placement
            if (_useQuickSetup)
            {
                OnQuickSetupRequested?.Invoke();
            }
        }

        /// <summary>
        /// Sets the game mode (called from main menu selection).
        /// Updates the wizard title accordingly.
        /// </summary>
        public void SetGameMode(GameMode mode)
        {
            _gameMode = mode;

            // Update wizard title based on game mode
            Label title = _root.Q<Label>("title");
            if (title != null)
            {
                title.text = mode switch
                {
                    GameMode.Solo => "Play Solo",
                    GameMode.Online => "Play Online",
                    GameMode.JoinGame => "Join Game",
                    _ => "Game Setup"
                };
            }

            // For JoinGame mode, hide Grid, Words, Difficulty, and BoardSetup cards
            // The host's settings determine everything - joiner only needs profile + game code
            if (mode == GameMode.JoinGame)
            {
                HideElement(_cardGridSize);
                HideElement(_cardWordCount);
                HideElement(_cardDifficulty);
                HideElement(_cardBoardSetup);

                // Show a "Join Code" input instead (TODO: implement join code UI)
                // For now, just show the profile card
            }
            else
            {
                // Show all cards for Solo and Online modes
                ShowElement(_cardProfile);
                // Other cards are revealed progressively via the normal flow
            }
        }

        // === Collapse/Expand ===

        private void CollapseCard(VisualElement card, VisualElement content, VisualElement summary)
        {
            if (card == null || content == null || summary == null) return;
            card.AddToClassList("collapsed");
            HideElement(content);
            ShowElement(summary);
        }

        private void ExpandCard(VisualElement card, VisualElement content, VisualElement summary)
        {
            if (card == null || content == null || summary == null) return;
            card.RemoveFromClassList("collapsed");
            ShowElement(content);
            HideElement(summary);
        }

        private void CollapseOtherCards(VisualElement exceptCard)
        {
            if (exceptCard != _cardProfile && !_cardProfile.ClassListContains("hidden"))
            {
                CollapseCard(_cardProfile, _profileContent, _profileSummary);
                UpdateProfileSummary();
            }
            if (exceptCard != _cardGridSize && !_cardGridSize.ClassListContains("hidden"))
            {
                CollapseCard(_cardGridSize, _gridContent, _gridSummary);
            }
            if (exceptCard != _cardWordCount && !_cardWordCount.ClassListContains("hidden"))
            {
                CollapseCard(_cardWordCount, _wordsContent, _wordsSummary);
            }
            if (exceptCard != _cardDifficulty && !_cardDifficulty.ClassListContains("hidden"))
            {
                CollapseCard(_cardDifficulty, _difficultyContent, _difficultySummary);
            }
        }

        private void RevealNextCard(VisualElement card)
        {
            if (card != null && card.ClassListContains("hidden"))
            {
                EnsureCardExpanded(card);
                card.AddToClassList("revealing");
                ShowElement(card);

                card.schedule.Execute(() =>
                {
                    card.RemoveFromClassList("revealing");
                    if (_cardsContainer is ScrollView scrollView)
                    {
                        scrollView.ScrollTo(card);
                    }
                }).ExecuteLater(20);
            }
        }

        private void EnsureCardExpanded(VisualElement card)
        {
            if (card == _cardProfile)
                ExpandCard(_cardProfile, _profileContent, _profileSummary);
            else if (card == _cardGridSize)
                ExpandCard(_cardGridSize, _gridContent, _gridSummary);
            else if (card == _cardWordCount)
                ExpandCard(_cardWordCount, _wordsContent, _wordsSummary);
            else if (card == _cardDifficulty)
                ExpandCard(_cardDifficulty, _difficultyContent, _difficultySummary);
        }

        // === Summary Updates ===

        private void UpdateProfileSummary()
        {
            if (_profileSummaryText != null)
                _profileSummaryText.text = string.IsNullOrWhiteSpace(_playerName) ? "Player" : _playerName;
            if (_profileColorBadge != null)
                _profileColorBadge.style.backgroundColor = _playerColor;
        }

        private void UpdateGridSummary()
        {
            if (_gridSummaryText != null)
                _gridSummaryText.text = $"{_gridSize}x{_gridSize}";
        }

        private void UpdateWordsSummary()
        {
            if (_wordsSummaryText != null)
                _wordsSummaryText.text = _wordCount.ToString();
        }

        private void UpdateDifficultySummary()
        {
            if (_difficultySummaryText != null)
            {
                string[] names = { "Easy", "Normal", "Hard" };
                _difficultySummaryText.text = names[_difficulty];
            }
        }

        // === Utility ===

        private void UpdateButtonSelection(Button[] buttons, int selectedIndex)
        {
            if (buttons == null) return;
            for (int i = 0; i < buttons.Length; i++)
            {
                if (buttons[i] != null)
                {
                    if (i == selectedIndex)
                        buttons[i].AddToClassList("selected");
                    else
                        buttons[i].RemoveFromClassList("selected");
                }
            }
        }

        private void ShowElement(VisualElement element)
        {
            element?.RemoveFromClassList("hidden");
        }

        private void HideElement(VisualElement element)
        {
            element?.AddToClassList("hidden");
        }

        public void Reset()
        {
            _playerName = DEFAULT_PLAYER_NAME;
            _gridSize = DEFAULT_GRID_SIZE;
            _wordCount = DEFAULT_WORD_COUNT;
            _difficulty = DEFAULT_DIFFICULTY;
            _useQuickSetup = true;
            _selectedColorIndex = 0;
            _playerColor = ColorRules.SelectableColors[0];

            if (_playerNameInput != null) _playerNameInput.value = DEFAULT_PLAYER_NAME;

            _setupQuickCard?.RemoveFromClassList("selected");
            _setupManualCard?.RemoveFromClassList("selected");

            HideElement(_cardGridSize);
            HideElement(_cardWordCount);
            HideElement(_cardDifficulty);
            HideElement(_cardBoardSetup);
            HideElement(_cardJoinCode);
            HideElement(_placementPanel);

            // Clear join code input
            if (_joinCodeInput != null) _joinCodeInput.value = "";

            ExpandCard(_cardProfile, _profileContent, _profileSummary);
            ExpandCard(_cardGridSize, _gridContent, _gridSummary);
            ExpandCard(_cardWordCount, _wordsContent, _wordsSummary);
            ExpandCard(_cardDifficulty, _difficultyContent, _difficultySummary);

            ShowElement(_cardsContainer);
            ShowElement(_cardProfile);

            for (int i = 0; i < _colorSwatches.Length; i++)
            {
                if (_colorSwatches[i] != null)
                {
                    if (i == 0)
                        _colorSwatches[i].AddToClassList("selected");
                    else
                        _colorSwatches[i].RemoveFromClassList("selected");
                }
            }
        }

        public SetupData GetCurrentSetup()
        {
            return new SetupData
            {
                PlayerName = _playerName,
                PlayerColor = _playerColor,
                GridSize = _gridSize,
                WordCount = _wordCount,
                Difficulty = _difficulty,
                GameMode = _gameMode,
                UseQuickSetup = _useQuickSetup
            };
        }
    }
}

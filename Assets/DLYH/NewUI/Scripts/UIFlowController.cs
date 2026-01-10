using UnityEngine;
using UnityEngine.UIElements;
using TecVooDoo.DontLoseYourHead.Core;
using TecVooDoo.DontLoseYourHead.UI;

namespace DLYH.TableUI
{
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

        [Header("USS Assets")]
        [SerializeField] private StyleSheet _mainMenuUss;
        [SerializeField] private StyleSheet _setupWizardUss;
        [SerializeField] private StyleSheet _tableViewUss;

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

        // Wizard state (managed inline since we can't use SetupWizardController as MonoBehaviour)
        private SetupWizardUIManager _wizardManager;

        // Table components (for placement phase)
        private TableModel _tableModel;
        private TableView _tableView;
        private TableLayout _tableLayout;
        private WordRowsContainer _wordRowsContainer;

        // Services
        private WordValidationService _wordValidationService;

        private bool _isInitialized = false;
        private bool _keyboardWiredUp = false;

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

            // Create screens - wizard first so menu is on top
            CreateSetupWizardScreen();
            CreateMainMenuScreen();

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
            Button startGameBtn = _mainMenuScreen.Q<Button>("btn-start-game");
            Button howToPlayBtn = _mainMenuScreen.Q<Button>("btn-how-to-play");
            Button settingsBtn = _mainMenuScreen.Q<Button>("btn-settings");

            if (startGameBtn != null)
            {
                startGameBtn.clicked += HandleStartGameClicked;
            }
            if (howToPlayBtn != null)
            {
                howToPlayBtn.clicked += HandleHowToPlayClicked;
            }
            if (settingsBtn != null)
            {
                settingsBtn.clicked += HandleSettingsClicked;
            }

            // Set version
            Label versionLabel = _mainMenuScreen.Q<Label>("version-label");
            if (versionLabel != null)
            {
                versionLabel.text = $"v{Application.version}";
            }
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
            _wizardManager.OnStartGame += HandleWizardStartGame;

            // Start hidden
            _setupWizardScreen.style.display = DisplayStyle.None;
        }

        // === Navigation Handlers ===

        private void HandleStartGameClicked()
        {
            ShowSetupWizard();
        }

        private void HandleHowToPlayClicked()
        {
            // TODO: Implement how to play screen
        }

        private void HandleSettingsClicked()
        {
            // TODO: Implement settings screen
        }

        private void HandleWizardStartGame()
        {
            // The wizard handles showing its own placement panel
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

            // Reset wizard state
            _wizardManager?.Reset();
        }

        private void ShowSetupWizard()
        {
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

            // Wire up word row events
            _wordRowsContainer.OnPlacementRequested += HandlePlacementRequested;
            _wordRowsContainer.OnWordCleared += HandleWordCleared;
            _wordRowsContainer.OnLetterCellClicked += HandleWordRowCellClicked;

            // Wire up grid cell clicks for placement
            _tableView.OnCellClicked += HandleGridCellClicked;

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
                };
            }

            // Wire up Ready button
            Button readyBtn = _setupWizardScreen.Q<Button>("btn-ready");
            if (readyBtn != null)
            {
                readyBtn.clicked += HandleReadyClicked;
            }
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

                    // If word is complete, validate it
                    if (newWord.Length == maxLength)
                    {
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
                string currentWord = _wordRowsContainer.GetWord(activeRow);
                if (currentWord.Length > 0)
                {
                    _wordRowsContainer.SetWord(activeRow, currentWord.Substring(0, currentWord.Length - 1));
                }
            }
        }

        private void HandleWordRowCellClicked(int wordIndex, int letterIndex)
        {
            // When a word row cell is clicked, make that row active for editing
            _wordRowsContainer?.SetActiveRow(wordIndex);
        }

        private void ValidateWord(int rowIndex, string word)
        {
            if (_wordValidationService == null) return;

            int expectedLength = _wordRowsContainer.GetWordLength(rowIndex);
            bool isValid = _wordValidationService.ValidateWord(word, expectedLength);

            if (isValid)
            {
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
            // TODO: Show visual feedback for invalid words (e.g., red highlight, shake animation)
        }

        private void HandlePlacementRequested(int wordIndex, string word)
        {
            // TODO: Enter placement mode, highlight valid starting cells
            // This will be connected to PlacementAdapter later
        }

        private void HandleWordCleared(int wordIndex)
        {
            // TODO: Clear word from grid if it was placed
        }

        private void HandleGridCellClicked(int row, int col, TableCell cell)
        {
            // Only handle grid cell clicks
            if (cell.Kind != TableCellKind.GridCell) return;
            // TODO: Handle placement via PlacementAdapter
        }

        private void ClearGridPlacements()
        {
            // Reset all grid cells to fog state
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

        // === Placement Handlers ===

        private void HandleRandomWords()
        {
            if (_wordRowsContainer == null || _tableLayout == null) return;

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
                    continue;
                }

                string randomWord = _wordValidationService.GetRandomWordOfLength(expectedLength);
                if (!string.IsNullOrEmpty(randomWord))
                {
                    _wordRowsContainer.SetWord(i, randomWord.ToUpper());
                }
            }
        }

        private void HandleRandomPlacement()
        {
            if (_wordRowsContainer == null) return;
            // TODO: Implement random placement algorithm using CoordinatePlacementController
        }

        private void HandleReadyClicked()
        {
            if (_wordRowsContainer == null) return;

            // Check if all words are placed
            if (!_wordRowsContainer.AreAllWordsPlaced())
            {
                // TODO: Show error message in UI
                return;
            }

            // TODO: Transition to gameplay phase
        }

        private void OnDestroy()
        {
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
                _tableView.Unbind();
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
        public event System.Action OnStartGame;

        // Data class
        public class SetupData
        {
            public string PlayerName;
            public Color PlayerColor;
            public int GridSize;
            public int WordCount;
            public int Difficulty;
            public bool IsSinglePlayer;
            public string GameCode;
        }

        // Defaults
        private const string DEFAULT_PLAYER_NAME = "Player";
        private const int DEFAULT_GRID_SIZE = 6;
        private const int DEFAULT_WORD_COUNT = 3;
        private const int DEFAULT_DIFFICULTY = 1;

        // Root element
        private VisualElement _root;

        // Cards
        private VisualElement _cardProfile;
        private VisualElement _cardGridSize;
        private VisualElement _cardWordCount;
        private VisualElement _cardDifficulty;
        private VisualElement _cardMode;
        private VisualElement _cardsContainer;
        private VisualElement _placementPanel;

        // Elements inside mode card
        private Button _btnStartGame;
        private VisualElement _multiplayerOptions;
        private VisualElement _joinGameSection;

        // Inputs
        private TextField _playerNameInput;
        private VisualElement _colorPicker;
        private TextField _gameCodeInput;

        // State
        private string _playerName = DEFAULT_PLAYER_NAME;
        private Color _playerColor;
        private int _selectedColorIndex = 0;
        private int _gridSize = DEFAULT_GRID_SIZE;
        private int _wordCount = DEFAULT_WORD_COUNT;
        private int _difficulty = DEFAULT_DIFFICULTY;
        private bool _isSinglePlayer = true;

        // UI arrays
        private Button[] _gridButtons;
        private Button[] _wordCountButtons;
        private Button[] _difficultyButtons;
        private VisualElement[] _colorSwatches;
        private VisualElement _mode1PlayerCard;
        private VisualElement _mode2PlayerCard;

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
            SetupModeCards();
            SetupMultiplayerButtons();
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
            HideElement(_cardMode);
            HideElement(_placementPanel);
            HideElement(_btnStartGame);
            HideElement(_multiplayerOptions);
            HideElement(_joinGameSection);

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
            _cardMode = _root.Q<VisualElement>("card-mode");
            _placementPanel = _root.Q<VisualElement>("placement-panel");

            _btnStartGame = _root.Q<Button>("btn-start-game");
            _multiplayerOptions = _root.Q<VisualElement>("multiplayer-options");
            _joinGameSection = _root.Q<VisualElement>("join-game-section");

            _playerNameInput = _root.Q<TextField>("player-name-input");
            _colorPicker = _root.Q<VisualElement>("color-picker");
            _gameCodeInput = _root.Q<TextField>("game-code-input");

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

        private void SetupModeCards()
        {
            _mode1PlayerCard = _root.Q<VisualElement>("mode-1player");
            _mode2PlayerCard = _root.Q<VisualElement>("mode-2player");

            if (_mode1PlayerCard != null)
            {
                _mode1PlayerCard.RegisterCallback<ClickEvent>(evt => SelectMode(true));
            }
            if (_mode2PlayerCard != null)
            {
                _mode2PlayerCard.RegisterCallback<ClickEvent>(evt => SelectMode(false));
            }
        }

        private void SetupMultiplayerButtons()
        {
            Button findOpponent = _root.Q<Button>("btn-find-opponent");
            Button inviteFriend = _root.Q<Button>("btn-invite-friend");
            Button joinGame = _root.Q<Button>("btn-join-game");
            Button joinWithCode = _root.Q<Button>("btn-join-with-code");

            if (findOpponent != null)
            {
                findOpponent.clicked += () => HandleMultiplayerAction("find");
            }
            if (inviteFriend != null)
            {
                inviteFriend.clicked += () => HandleMultiplayerAction("invite");
            }
            if (joinGame != null)
            {
                joinGame.clicked += ShowJoinGameSection;
            }
            if (joinWithCode != null)
            {
                joinWithCode.clicked += HandleJoinWithCode;
            }
        }

        private void SetupActionButtons()
        {
            Button startGame = _root.Q<Button>("btn-start-game");
            Button ready = _root.Q<Button>("btn-ready");
            Button backToSettings = _root.Q<Button>("btn-back-to-settings");

            if (startGame != null)
            {
                startGame.clicked += HandleStartGame;
            }
            if (ready != null)
            {
                ready.clicked += HandleReady;
            }
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

            // Create 3-row keyboard layout
            // Row 1: A-I (9 letters)
            // Row 2: J-R (9 letters)
            // Row 3: S-Z (8 letters) + Backspace
            string[] rows = { "ABCDEFGHI", "JKLMNOPQR", "STUVWXYZ" };

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
            RevealNextCard(_cardGridSize);
        }

        private void OnPlayerNameChanged(ChangeEvent<string> evt)
        {
            _playerName = evt.newValue;
            if (!string.IsNullOrEmpty(evt.newValue))
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

            CollapseCard(_cardWordCount, _wordsContent, _wordsSummary);
            RevealNextCard(_cardMode);
        }

        private void SelectMode(bool isSinglePlayer)
        {
            _isSinglePlayer = isSinglePlayer;

            CollapseCard(_cardDifficulty, _difficultyContent, _difficultySummary);

            if (_mode1PlayerCard != null)
            {
                if (isSinglePlayer)
                    _mode1PlayerCard.AddToClassList("selected");
                else
                    _mode1PlayerCard.RemoveFromClassList("selected");
            }
            if (_mode2PlayerCard != null)
            {
                if (!isSinglePlayer)
                    _mode2PlayerCard.AddToClassList("selected");
                else
                    _mode2PlayerCard.RemoveFromClassList("selected");
            }

            if (isSinglePlayer)
            {
                HideElement(_multiplayerOptions);
                HideElement(_joinGameSection);
                ShowElement(_btnStartGame);
            }
            else
            {
                HideElement(_btnStartGame);
                HideElement(_joinGameSection);
                ShowElement(_multiplayerOptions);
            }
        }

        // === Action Handlers ===

        private void HandleStartGame()
        {
            _playerName = _playerNameInput?.value ?? DEFAULT_PLAYER_NAME;
            if (string.IsNullOrWhiteSpace(_playerName))
                _playerName = DEFAULT_PLAYER_NAME;

            ShowPlacementPanel();
            OnStartGame?.Invoke();
        }

        private void HandleMultiplayerAction(string action)
        {
            _playerName = _playerNameInput?.value ?? DEFAULT_PLAYER_NAME;
            ShowPlacementPanel();
        }

        private void ShowJoinGameSection()
        {
            ShowElement(_joinGameSection);
            _gameCodeInput?.Focus();
        }

        private void HandleJoinWithCode()
        {
            string code = _gameCodeInput?.value?.ToUpperInvariant() ?? "";
            if (code.Length != 6)
            {
                // TODO: Show invalid code feedback in UI
                return;
            }

            _playerName = _playerNameInput?.value ?? DEFAULT_PLAYER_NAME;
            ShowPlacementPanel();
        }

        private void HandleReady()
        {
            SetupData data = new SetupData
            {
                PlayerName = _playerName,
                PlayerColor = _playerColor,
                GridSize = _gridSize,
                WordCount = _wordCount,
                Difficulty = _difficulty,
                IsSinglePlayer = _isSinglePlayer,
                GameCode = _gameCodeInput?.value
            };

            OnSetupComplete?.Invoke(data);
        }

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
                IsSinglePlayer = _isSinglePlayer,
                GameCode = _gameCodeInput?.value
            };
            OnSetupComplete?.Invoke(data);
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
            _isSinglePlayer = true;
            _selectedColorIndex = 0;
            _playerColor = ColorRules.SelectableColors[0];

            if (_playerNameInput != null) _playerNameInput.value = DEFAULT_PLAYER_NAME;
            if (_gameCodeInput != null) _gameCodeInput.value = "";

            _mode1PlayerCard?.RemoveFromClassList("selected");
            _mode2PlayerCard?.RemoveFromClassList("selected");

            HideElement(_cardGridSize);
            HideElement(_cardWordCount);
            HideElement(_cardDifficulty);
            HideElement(_cardMode);
            HideElement(_placementPanel);

            ExpandCard(_cardProfile, _profileContent, _profileSummary);
            ExpandCard(_cardGridSize, _gridContent, _gridSummary);
            ExpandCard(_cardWordCount, _wordsContent, _wordsSummary);
            ExpandCard(_cardDifficulty, _difficultyContent, _difficultySummary);

            HideElement(_btnStartGame);
            HideElement(_multiplayerOptions);
            HideElement(_joinGameSection);

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
                IsSinglePlayer = _isSinglePlayer,
                GameCode = _gameCodeInput?.value
            };
        }
    }
}

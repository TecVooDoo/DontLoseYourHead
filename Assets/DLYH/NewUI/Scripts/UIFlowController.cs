using UnityEngine;
using UnityEngine.UIElements;

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
        private WordPlacementController _placementController;

        private void Awake()
        {
            _uiDocument = GetComponent<UIDocument>();
        }

        private void Start()
        {
            Initialize();
        }

        private void Initialize()
        {
            _root = _uiDocument.rootVisualElement;
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

            Debug.Log("[UIFlowController] Initialized - showing main menu");
            Debug.Log($"[UIFlowController] Main menu display: {_mainMenuScreen.style.display.value}");
            Debug.Log($"[UIFlowController] Wizard display: {_setupWizardScreen.style.display.value}");
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
                // Make the TemplateContainer fill the parent
                menuContent.style.flexGrow = 1;
                _mainMenuScreen.Add(menuContent);
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
            Debug.Log("[UIFlowController] Start Game clicked");
            ShowSetupWizard();
        }

        private void HandleHowToPlayClicked()
        {
            Debug.Log("[UIFlowController] How to Play clicked - not implemented yet");
        }

        private void HandleSettingsClicked()
        {
            Debug.Log("[UIFlowController] Settings clicked - not implemented yet");
        }

        private void HandleWizardStartGame()
        {
            Debug.Log("[UIFlowController] Wizard Start Game - transitioning to placement");
            // The wizard handles showing its own placement panel
        }

        private void HandleSetupComplete(SetupWizardUIManager.SetupData data)
        {
            Debug.Log($"[UIFlowController] Setup complete - {data.PlayerName}, {data.GridSize}x{data.GridSize}, {data.WordCount} words");
            InitializeTableForPlacement(data);
        }

        // === Screen Visibility ===

        private void ShowMainMenu()
        {
            // Explicitly set display styles
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

            Debug.Log("[UIFlowController] Showing main menu");
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

            Debug.Log("[UIFlowController] Showing setup wizard");
        }

        // === Table Initialization ===

        private void InitializeTableForPlacement(SetupWizardUIManager.SetupData data)
        {
            // Create layout based on setup data
            _tableLayout = TableLayout.CreateForSetup(data.GridSize, data.WordCount);

            // Create table model
            _tableModel = new TableModel();
            _tableModel.Initialize(_tableLayout);

            // Find the table container in the wizard's placement panel
            VisualElement tableContainer = _setupWizardScreen.Q<VisualElement>("table-container");

            if (tableContainer == null)
            {
                Debug.LogError("[UIFlowController] Could not find table-container element");
                return;
            }

            // Create table view
            _tableView = new TableView(tableContainer);
            _tableView.SetPlayerColors(data.PlayerColor, ColorRules.SelectableColors[1]);
            _tableView.Bind(_tableModel);

            // Create placement controller
            _placementController = new WordPlacementController();
            _placementController.Initialize(_tableModel, _tableView, _tableLayout, data.PlayerColor);

            // Wire up placement events
            _placementController.OnWordPlaced += (index, word) =>
                Debug.Log($"[UIFlowController] Word {index + 1} placed: {word}");
            _placementController.OnAllWordsPlaced += () =>
                Debug.Log("[UIFlowController] All words placed - ready!");

            // Wire up letter keyboard
            VisualElement keyboard = _setupWizardScreen.Q<VisualElement>("letter-keyboard");
            if (keyboard != null)
            {
                foreach (VisualElement child in keyboard.Children())
                {
                    if (child is Button keyButton && keyButton.text.Length == 1)
                    {
                        char letter = keyButton.text[0];
                        keyButton.clicked += () => _placementController?.AddLetterToSelectedWord(letter);
                    }
                }
            }

            Debug.Log($"[UIFlowController] Table initialized: {_tableLayout.TotalRows}x{_tableLayout.TotalCols}");
        }

        private void OnDestroy()
        {
            _placementController?.Dispose();
            _tableView?.Unbind();
        }
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

            Debug.Log("[SetupWizardUIManager] Initialized");
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

            for (char c = 'A'; c <= 'Z'; c++)
            {
                char letter = c;
                Button key = new Button();
                key.text = letter.ToString();
                key.AddToClassList("letter-key");
                key.clicked += () => Debug.Log($"[Wizard] Letter: {letter}");
                keyboard.Add(key);
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

            Debug.Log($"[Wizard] Starting game - {_playerName}, {_gridSize}x{_gridSize}");
            ShowPlacementPanel();
            OnStartGame?.Invoke();
        }

        private void HandleMultiplayerAction(string action)
        {
            _playerName = _playerNameInput?.value ?? DEFAULT_PLAYER_NAME;
            Debug.Log($"[Wizard] Multiplayer action: {action}");
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
                Debug.LogWarning("[Wizard] Invalid game code");
                return;
            }

            _playerName = _playerNameInput?.value ?? DEFAULT_PLAYER_NAME;
            Debug.Log($"[Wizard] Joining with code: {code}");
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

            Debug.Log("[Wizard] Ready - firing OnSetupComplete");
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

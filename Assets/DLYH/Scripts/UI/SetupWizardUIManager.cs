using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace DLYH.TableUI
{
    /// <summary>
    /// Online mode selection for Play Online.
    /// </summary>
    public enum OnlineMode
    {
        FindOpponent,   // Quick matchmaking with anyone
        PrivateGame     // Create private game with join code
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
        public event System.Action<string> OnJoinCodeSubmitted;

        // Data class
        public class SetupData
        {
            public string PlayerName;
            public Color PlayerColor;
            public int GridSize;
            public int WordCount;
            public int Difficulty;
            public GameMode GameMode;
            public OnlineMode OnlineMode;
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
        private VisualElement _cardOnlineMode;
        private VisualElement _cardBoardSetup;
        private VisualElement _cardJoinCode;
        private VisualElement _cardsContainer;
        private VisualElement _placementPanel;

        // Game mode (set from main menu)
        private GameMode _gameMode = GameMode.Solo;
        private OnlineMode _onlineMode = OnlineMode.FindOpponent;

        // Online mode choice cards
        private VisualElement _modeFindOpponent;
        private VisualElement _modePrivateGame;

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
        public OnlineMode SelectedOnlineMode => _onlineMode;

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
            SetupOnlineModeCards();
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
            HideElement(_cardOnlineMode);
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
            _cardOnlineMode = _root.Q<VisualElement>("card-online-mode");
            _cardBoardSetup = _root.Q<VisualElement>("card-board-setup");
            _cardJoinCode = _root.Q<VisualElement>("card-join-code");
            _placementPanel = _root.Q<VisualElement>("placement-panel");

            // Online mode choice cards
            _modeFindOpponent = _root.Q<VisualElement>("mode-find-opponent");
            _modePrivateGame = _root.Q<VisualElement>("mode-private-game");

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

        private void SetupOnlineModeCards()
        {
            if (_modeFindOpponent != null)
            {
                _modeFindOpponent.RegisterCallback<ClickEvent>(evt => SelectOnlineMode(OnlineMode.FindOpponent));
            }
            if (_modePrivateGame != null)
            {
                _modePrivateGame.RegisterCallback<ClickEvent>(evt => SelectOnlineMode(OnlineMode.PrivateGame));
            }
        }

        private void SelectOnlineMode(OnlineMode mode)
        {
            _onlineMode = mode;

            // Update visual selection state
            if (_modeFindOpponent != null)
            {
                if (mode == OnlineMode.FindOpponent)
                    _modeFindOpponent.AddToClassList("selected");
                else
                    _modeFindOpponent.RemoveFromClassList("selected");
            }
            if (_modePrivateGame != null)
            {
                if (mode == OnlineMode.PrivateGame)
                    _modePrivateGame.AddToClassList("selected");
                else
                    _modePrivateGame.RemoveFromClassList("selected");
            }

            // Collapse difficulty card and proceed to board setup
            CollapseCard(_cardDifficulty, _difficultyContent, _difficultySummary);
            RevealNextCard(_cardBoardSetup);
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

            if (string.IsNullOrEmpty(code) || code.Length < 6)
            {
                Debug.LogWarning("[SetupWizard] Invalid join code - must be 6 characters");
                return;
            }

            Debug.Log($"[SetupWizard] Submitting join code: {code}");
            OnJoinCodeSubmitted?.Invoke(code);
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
                    backspaceBtn.text = "<-";
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
        /// All modes follow the same flow: Profile -> Grid Size -> Word Count -> Difficulty -> Board Setup.
        /// </summary>
        private void RevealNextCardAfterProfile()
        {
            // All modes (Solo, Online, JoinGame) need to pick grid size next
            RevealNextCard(_cardGridSize);
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

            if (_gameMode == GameMode.Online)
            {
                // For Online mode, show the online mode choice (Find Opponent vs Private Game)
                CollapseCard(_cardWordCount, _wordsContent, _wordsSummary);
                RevealNextCard(_cardOnlineMode);
            }
            else
            {
                // For Solo and JoinGame modes, go straight to board setup
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
                OnlineMode = _onlineMode,
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

            // For JoinGame mode, only hide OnlineMode card (joiner doesn't choose Find/Private)
            // Joiner still needs to pick grid size, word count, difficulty, and place words
            if (mode == GameMode.JoinGame)
            {
                HideElement(_cardOnlineMode);
                // Grid, Words, Difficulty, BoardSetup are all revealed progressively
            }

            // Show profile card - all modes start the same way
            ShowElement(_cardProfile);
            // Other cards are revealed progressively via the normal flow
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

        /// <summary>
        /// Shows the join code card, hiding the placement panel.
        /// Called by UIFlowController when JoinGame mode is ready to enter the code.
        /// </summary>
        public void ShowJoinCodeCard()
        {
            HideElement(_placementPanel);
            ShowElement(_cardsContainer);

            // Collapse all cards and show only join code
            CollapseCard(_cardProfile, _profileContent, _profileSummary);
            CollapseCard(_cardGridSize, _gridContent, _gridSummary);
            CollapseCard(_cardWordCount, _wordsContent, _wordsSummary);
            CollapseCard(_cardDifficulty, _difficultyContent, _difficultySummary);
            CollapseCard(_cardBoardSetup, _root.Q<VisualElement>("board-setup-content"), _root.Q<VisualElement>("board-setup-summary"));

            HideElement(_cardOnlineMode);
            RevealNextCard(_cardJoinCode);
        }

        public void Reset()
        {
            _playerName = DEFAULT_PLAYER_NAME;
            _gridSize = DEFAULT_GRID_SIZE;
            _wordCount = DEFAULT_WORD_COUNT;
            _difficulty = DEFAULT_DIFFICULTY;
            _useQuickSetup = true;
            _onlineMode = OnlineMode.FindOpponent;
            _selectedColorIndex = 0;
            _playerColor = ColorRules.SelectableColors[0];

            if (_playerNameInput != null) _playerNameInput.value = DEFAULT_PLAYER_NAME;

            _setupQuickCard?.RemoveFromClassList("selected");
            _setupManualCard?.RemoveFromClassList("selected");
            _modeFindOpponent?.RemoveFromClassList("selected");
            _modePrivateGame?.RemoveFromClassList("selected");

            HideElement(_cardGridSize);
            HideElement(_cardWordCount);
            HideElement(_cardDifficulty);
            HideElement(_cardOnlineMode);
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
                OnlineMode = _onlineMode,
                UseQuickSetup = _useQuickSetup
            };
        }
    }
}

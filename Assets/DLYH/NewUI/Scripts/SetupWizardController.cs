using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace DLYH.TableUI
{
    /// <summary>
    /// Controls the setup wizard flow with progressive card reveal.
    /// Cards appear one at a time as the user interacts with each section.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public class SetupWizardController : MonoBehaviour
    {
        [Header("Default Settings")]
        [SerializeField] private string _defaultPlayerName = "Player";
        [SerializeField] private int _defaultGridSize = 6;
        [SerializeField] private int _defaultWordCount = 3;
        [SerializeField] private int _defaultDifficulty = 1; // 0=Easy, 1=Normal, 2=Hard

        private UIDocument _uiDocument;
        private VisualElement _root;

        // Cards (progressive reveal)
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
        private string _playerName;
        private Color _playerColor;
        private int _selectedColorIndex = 0;
        private int _gridSize;
        private int _wordCount;
        private int _difficulty;
        private GameMode _gameMode = GameMode.None;
        private bool _profileInteracted = false;

        // UI Element references for selection state
        private Button[] _gridButtons;
        private Button[] _wordCountButtons;
        private Button[] _difficultyButtons;
        private VisualElement[] _colorSwatches;
        private VisualElement _mode1PlayerCard;
        private VisualElement _mode2PlayerCard;

        // Card content and summary elements for collapse/expand
        private VisualElement _profileContent;
        private VisualElement _profileSummary;
        private VisualElement _gridContent;
        private VisualElement _gridSummary;
        private VisualElement _wordsContent;
        private VisualElement _wordsSummary;
        private VisualElement _difficultyContent;
        private VisualElement _difficultySummary;

        // Summary text labels
        private Label _profileSummaryText;
        private VisualElement _profileColorBadge;
        private Label _gridSummaryText;
        private Label _wordsSummaryText;
        private Label _difficultySummaryText;

        // Events
        public event Action<SetupData> OnSetupComplete;
        public event Action OnBackToMenu;

        public enum GameMode
        {
            None,
            SinglePlayer,
            Multiplayer
        }

        public enum MultiplayerAction
        {
            FindOpponent,
            InviteFriend,
            JoinGame
        }

        /// <summary>
        /// Data class containing all setup configuration.
        /// </summary>
        public class SetupData
        {
            public string PlayerName;
            public Color PlayerColor;
            public int GridSize;
            public int WordCount;
            public int Difficulty;
            public GameMode Mode;
            public string GameCode;
        }

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

            // Cache card references
            _cardsContainer = _root.Q<VisualElement>("cards-container");
            _cardProfile = _root.Q<VisualElement>("card-profile");
            _cardGridSize = _root.Q<VisualElement>("card-grid-size");
            _cardWordCount = _root.Q<VisualElement>("card-word-count");
            _cardDifficulty = _root.Q<VisualElement>("card-difficulty");
            _cardMode = _root.Q<VisualElement>("card-mode");
            _placementPanel = _root.Q<VisualElement>("placement-panel");

            // Cache elements inside mode card
            _btnStartGame = _root.Q<Button>("btn-start-game");
            _multiplayerOptions = _root.Q<VisualElement>("multiplayer-options");
            _joinGameSection = _root.Q<VisualElement>("join-game-section");

            // Cache input references
            _playerNameInput = _root.Q<TextField>("player-name-input");
            _colorPicker = _root.Q<VisualElement>("color-picker");
            _gameCodeInput = _root.Q<TextField>("game-code-input");

            // Cache content and summary elements for collapse/expand
            _profileContent = _root.Q<VisualElement>("profile-content");
            _profileSummary = _root.Q<VisualElement>("profile-summary");
            _gridContent = _root.Q<VisualElement>("grid-content");
            _gridSummary = _root.Q<VisualElement>("grid-summary");
            _wordsContent = _root.Q<VisualElement>("words-content");
            _wordsSummary = _root.Q<VisualElement>("words-summary");
            _difficultyContent = _root.Q<VisualElement>("difficulty-content");
            _difficultySummary = _root.Q<VisualElement>("difficulty-summary");

            // Cache summary text labels
            _profileSummaryText = _root.Q<Label>("profile-summary-text");
            _profileColorBadge = _root.Q<VisualElement>("profile-color-badge");
            _gridSummaryText = _root.Q<Label>("grid-summary-text");
            _wordsSummaryText = _root.Q<Label>("words-summary-text");
            _difficultySummaryText = _root.Q<Label>("difficulty-summary-text");

            // Set up initial values
            _playerName = _defaultPlayerName;
            _gridSize = _defaultGridSize;
            _wordCount = _defaultWordCount;
            _difficulty = _defaultDifficulty;
            _playerColor = ColorRules.SelectableColors[_selectedColorIndex];

            // Initialize UI components
            SetupColorPicker();
            SetupGridSizeButtons();
            SetupWordCountButtons();
            SetupDifficultyButtons();
            SetupModeCards();
            SetupMultiplayerButtons();
            SetupActionButtons();
            SetupLetterKeyboard();

            // Set initial input values
            _playerNameInput.value = _defaultPlayerName;
            _playerNameInput.RegisterValueChangedCallback(OnPlayerNameChanged);

            // Set up click handlers for collapsed cards (to re-expand)
            SetupCollapsedCardClickHandlers();

            // Ensure all cards except profile start hidden
            HideElement(_cardGridSize);
            HideElement(_cardWordCount);
            HideElement(_cardDifficulty);
            HideElement(_cardMode);
            HideElement(_placementPanel);

            // Hide elements inside mode card
            HideElement(_btnStartGame);
            HideElement(_multiplayerOptions);
            HideElement(_joinGameSection);

            // Profile card always visible and EXPANDED (not collapsed)
            ShowElement(_cardProfile);
            ExpandCard(_cardProfile, _profileContent, _profileSummary);

            Debug.Log("[SetupWizard] Initialized with progressive card reveal");
        }

        private void SetupColorPicker()
        {
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

            // Select first color by default
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
                _mode1PlayerCard.RegisterCallback<ClickEvent>(evt => SelectMode(GameMode.SinglePlayer));
            }
            if (_mode2PlayerCard != null)
            {
                _mode2PlayerCard.RegisterCallback<ClickEvent>(evt => SelectMode(GameMode.Multiplayer));
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
                findOpponent.clicked += () => HandleMultiplayerAction(MultiplayerAction.FindOpponent);
            }
            if (inviteFriend != null)
            {
                inviteFriend.clicked += () => HandleMultiplayerAction(MultiplayerAction.InviteFriend);
            }
            if (joinGame != null)
            {
                joinGame.clicked += ShowJoinGameCard;
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
            Button randomWords = _root.Q<Button>("btn-random-words");
            Button randomPlacement = _root.Q<Button>("btn-random-placement");
            Button clearPlacement = _root.Q<Button>("btn-clear-placement");

            if (startGame != null)
            {
                startGame.clicked += StartGame;
            }
            if (ready != null)
            {
                ready.clicked += HandleReady;
            }
            if (backToSettings != null)
            {
                backToSettings.clicked += ShowSettingsCards;
            }
            if (randomWords != null)
            {
                randomWords.clicked += HandleRandomWords;
            }
            if (randomPlacement != null)
            {
                randomPlacement.clicked += HandleRandomPlacement;
            }
            if (clearPlacement != null)
            {
                clearPlacement.clicked += HandleClearPlacement;
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
                key.clicked += () => HandleLetterKeyPressed(letter);
                keyboard.Add(key);
            }
        }

        private void SetupCollapsedCardClickHandlers()
        {
            // When a collapsed card is clicked, expand it (and collapse others)
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

        // === Collapse/Expand Methods ===

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
            // Collapse all cards except the one being expanded
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
            // Note: Mode card doesn't collapse - it's the final decision
        }

        private void UpdateProfileSummary()
        {
            if (_profileSummaryText != null)
            {
                _profileSummaryText.text = string.IsNullOrWhiteSpace(_playerName) ? "Player" : _playerName;
            }
            if (_profileColorBadge != null)
            {
                _profileColorBadge.style.backgroundColor = _playerColor;
            }
        }

        private void UpdateGridSummary()
        {
            if (_gridSummaryText != null)
            {
                _gridSummaryText.text = $"{_gridSize}x{_gridSize}";
            }
        }

        private void UpdateWordsSummary()
        {
            if (_wordsSummaryText != null)
            {
                _wordsSummaryText.text = _wordCount.ToString();
            }
        }

        private void UpdateDifficultySummary()
        {
            if (_difficultySummaryText != null)
            {
                string[] names = { "Easy", "Normal", "Hard" };
                _difficultySummaryText.text = names[_difficulty];
            }
        }

        // === Selection Handlers with Progressive Reveal ===

        private void SelectColor(int index)
        {
            _selectedColorIndex = index;
            _playerColor = ColorRules.SelectableColors[index];

            // Update visual selection
            for (int i = 0; i < _colorSwatches.Length; i++)
            {
                if (_colorSwatches[i] != null)
                {
                    if (i == index)
                    {
                        _colorSwatches[i].AddToClassList("selected");
                    }
                    else
                    {
                        _colorSwatches[i].RemoveFromClassList("selected");
                    }
                }
            }

            // Just reveal grid size card - don't collapse profile yet
            // Profile will collapse when user selects a grid size
            _playerName = _playerNameInput?.value ?? _defaultPlayerName;
            RevealNextCard(_cardGridSize);

            Debug.Log($"[SetupWizard] Selected color: {ColorRules.SelectableColorNames[index]}");
        }

        private void OnPlayerNameChanged(ChangeEvent<string> evt)
        {
            _playerName = evt.newValue;

            // Progressive reveal: show grid size card on any name input
            if (!string.IsNullOrEmpty(evt.newValue))
            {
                RevealNextCard(_cardGridSize);
            }
        }

        private void SelectGridSize(int size)
        {
            _gridSize = size;
            int index = size - 6;
            UpdateButtonSelection(_gridButtons, index);
            UpdateGridSummary();

            // Collapse profile card now (user finished with it)
            _playerName = _playerNameInput?.value ?? _defaultPlayerName;
            UpdateProfileSummary();
            CollapseCard(_cardProfile, _profileContent, _profileSummary);

            // DON'T collapse grid card yet - it collapses when user interacts with word count
            // Progressive reveal: show word count card
            RevealNextCard(_cardWordCount);

            Debug.Log($"[SetupWizard] Selected grid size: {size}x{size}");
        }

        private void SelectWordCount(int count)
        {
            _wordCount = count;
            int index = count - 3;
            UpdateButtonSelection(_wordCountButtons, index);
            UpdateWordsSummary();

            // Collapse grid card now (user finished with it)
            CollapseCard(_cardGridSize, _gridContent, _gridSummary);

            // DON'T collapse word count card yet - it collapses when user interacts with difficulty
            // Progressive reveal: show difficulty card
            RevealNextCard(_cardDifficulty);

            Debug.Log($"[SetupWizard] Selected word count: {count}");
        }

        private void SelectDifficulty(int difficulty)
        {
            _difficulty = difficulty;
            UpdateButtonSelection(_difficultyButtons, difficulty);
            UpdateDifficultySummary();

            // Collapse word count card now (user finished with it)
            CollapseCard(_cardWordCount, _wordsContent, _wordsSummary);

            // DON'T collapse difficulty card yet - it collapses when user selects a mode
            // Progressive reveal: show mode card
            RevealNextCard(_cardMode);

            string[] names = { "Easy", "Normal", "Hard" };
            Debug.Log($"[SetupWizard] Selected difficulty: {names[difficulty]}");
        }

        private void SelectMode(GameMode mode)
        {
            _gameMode = mode;

            // Collapse difficulty card when mode is selected
            CollapseCard(_cardDifficulty, _difficultyContent, _difficultySummary);

            // Update mode card visuals
            if (_mode1PlayerCard != null)
            {
                if (mode == GameMode.SinglePlayer)
                {
                    _mode1PlayerCard.AddToClassList("selected");
                }
                else
                {
                    _mode1PlayerCard.RemoveFromClassList("selected");
                }
            }
            if (_mode2PlayerCard != null)
            {
                if (mode == GameMode.Multiplayer)
                {
                    _mode2PlayerCard.AddToClassList("selected");
                }
                else
                {
                    _mode2PlayerCard.RemoveFromClassList("selected");
                }
            }

            // Show appropriate options inside mode card
            if (mode == GameMode.SinglePlayer)
            {
                HideElement(_multiplayerOptions);
                HideElement(_joinGameSection);
                ShowElement(_btnStartGame);
            }
            else if (mode == GameMode.Multiplayer)
            {
                HideElement(_btnStartGame);
                HideElement(_joinGameSection);
                ShowElement(_multiplayerOptions);
            }

            Debug.Log($"[SetupWizard] Selected mode: {mode}");
        }

        private void RevealNextCard(VisualElement card)
        {
            if (card != null && card.ClassListContains("hidden"))
            {
                // Ensure the card is expanded (not collapsed) when revealed
                EnsureCardExpanded(card);

                // Start with revealing class (opacity 0, translated up)
                card.AddToClassList("revealing");
                ShowElement(card);

                // Remove revealing class after a frame to trigger animation
                card.schedule.Execute(() =>
                {
                    card.RemoveFromClassList("revealing");

                    // Scroll to show the new card
                    ScrollView scrollView = _cardsContainer as ScrollView;
                    if (scrollView != null)
                    {
                        scrollView.ScrollTo(card);
                    }
                }).ExecuteLater(20);
            }
        }

        private void EnsureCardExpanded(VisualElement card)
        {
            // Make sure the card shows its content, not summary
            if (card == _cardProfile)
            {
                ExpandCard(_cardProfile, _profileContent, _profileSummary);
            }
            else if (card == _cardGridSize)
            {
                ExpandCard(_cardGridSize, _gridContent, _gridSummary);
            }
            else if (card == _cardWordCount)
            {
                ExpandCard(_cardWordCount, _wordsContent, _wordsSummary);
            }
            else if (card == _cardDifficulty)
            {
                ExpandCard(_cardDifficulty, _difficultyContent, _difficultySummary);
            }
            // Mode card doesn't have collapse state
        }

        // === Action Handlers ===

        private void StartGame()
        {
            _playerName = _playerNameInput.value;
            if (string.IsNullOrWhiteSpace(_playerName))
            {
                _playerName = _defaultPlayerName;
            }

            Debug.Log($"[SetupWizard] Starting game - Name: {_playerName}, Grid: {_gridSize}, Words: {_wordCount}, Difficulty: {_difficulty}");
            ShowPlacementPanel();
        }

        private void HandleMultiplayerAction(MultiplayerAction action)
        {
            _playerName = _playerNameInput.value;
            if (string.IsNullOrWhiteSpace(_playerName))
            {
                _playerName = _defaultPlayerName;
            }

            switch (action)
            {
                case MultiplayerAction.FindOpponent:
                    Debug.Log("[SetupWizard] Finding opponent...");
                    ShowPlacementPanel();
                    break;

                case MultiplayerAction.InviteFriend:
                    Debug.Log("[SetupWizard] Creating game for invite...");
                    ShowPlacementPanel();
                    break;
            }
        }

        private void ShowJoinGameCard()
        {
            ShowElement(_joinGameSection);
            _gameCodeInput?.Focus();
        }

        private void HandleJoinWithCode()
        {
            string code = _gameCodeInput?.value?.ToUpperInvariant() ?? "";
            if (code.Length != 6)
            {
                Debug.LogWarning("[SetupWizard] Invalid game code - must be 6 characters");
                return;
            }

            _playerName = _playerNameInput.value;
            if (string.IsNullOrWhiteSpace(_playerName))
            {
                _playerName = _defaultPlayerName;
            }

            Debug.Log($"[SetupWizard] Joining game with code: {code}");
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
                Mode = _gameMode,
                GameCode = _gameCodeInput?.value
            };

            Debug.Log($"[SetupWizard] Ready - Firing OnSetupComplete");
            OnSetupComplete?.Invoke(data);
        }

        private void HandleRandomWords()
        {
            Debug.Log("[SetupWizard] Random words requested");
        }

        private void HandleRandomPlacement()
        {
            Debug.Log("[SetupWizard] Random placement requested");
        }

        private void HandleClearPlacement()
        {
            Debug.Log("[SetupWizard] Clear placement requested");
        }

        private void HandleLetterKeyPressed(char letter)
        {
            Debug.Log($"[SetupWizard] Letter key pressed: {letter}");
        }

        // === Panel Visibility ===

        private void ShowSettingsCards()
        {
            ShowElement(_cardsContainer);
            HideElement(_placementPanel);
            Debug.Log("[SetupWizard] Showing settings cards");
        }

        private void ShowPlacementPanel()
        {
            HideElement(_cardsContainer);
            ShowElement(_placementPanel);

            // Update player display
            Label nameDisplay = _root.Q<Label>("player-name-display");
            VisualElement colorBadge = _root.Q<VisualElement>("player-color-badge");

            if (nameDisplay != null)
            {
                nameDisplay.text = _playerName.ToUpperInvariant();
            }
            if (colorBadge != null)
            {
                colorBadge.style.backgroundColor = _playerColor;
            }

            Debug.Log("[SetupWizard] Showing placement panel");
        }

        // === Utility Methods ===

        private void UpdateButtonSelection(Button[] buttons, int selectedIndex)
        {
            if (buttons == null) return;

            for (int i = 0; i < buttons.Length; i++)
            {
                if (buttons[i] != null)
                {
                    if (i == selectedIndex)
                    {
                        buttons[i].AddToClassList("selected");
                    }
                    else
                    {
                        buttons[i].RemoveFromClassList("selected");
                    }
                }
            }
        }

        private void ShowElement(VisualElement element)
        {
            if (element != null)
            {
                element.RemoveFromClassList("hidden");
            }
        }

        private void HideElement(VisualElement element)
        {
            if (element != null)
            {
                element.AddToClassList("hidden");
            }
        }

        // === Public API ===

        public SetupData GetCurrentSetup()
        {
            return new SetupData
            {
                PlayerName = _playerName,
                PlayerColor = _playerColor,
                GridSize = _gridSize,
                WordCount = _wordCount,
                Difficulty = _difficulty,
                Mode = _gameMode,
                GameCode = _gameCodeInput?.value
            };
        }

        public void Reset()
        {
            _playerName = _defaultPlayerName;
            _gridSize = _defaultGridSize;
            _wordCount = _defaultWordCount;
            _difficulty = _defaultDifficulty;
            _gameMode = GameMode.None;
            _selectedColorIndex = 0;
            _playerColor = ColorRules.SelectableColors[0];
            _profileInteracted = false;

            if (_playerNameInput != null)
            {
                _playerNameInput.value = _defaultPlayerName;
            }
            if (_gameCodeInput != null)
            {
                _gameCodeInput.value = "";
            }

            // Clear selections
            _mode1PlayerCard?.RemoveFromClassList("selected");
            _mode2PlayerCard?.RemoveFromClassList("selected");

            // Reset to initial card state (all hidden and expanded)
            HideElement(_cardGridSize);
            HideElement(_cardWordCount);
            HideElement(_cardDifficulty);
            HideElement(_cardMode);
            HideElement(_placementPanel);

            // Reset collapsed states - expand all cards
            ExpandCard(_cardProfile, _profileContent, _profileSummary);
            ExpandCard(_cardGridSize, _gridContent, _gridSummary);
            ExpandCard(_cardWordCount, _wordsContent, _wordsSummary);
            ExpandCard(_cardDifficulty, _difficultyContent, _difficultySummary);

            // Hide elements inside mode card
            HideElement(_btnStartGame);
            HideElement(_multiplayerOptions);
            HideElement(_joinGameSection);

            ShowElement(_cardsContainer);
            ShowElement(_cardProfile);

            // Reset color selection visual (but don't trigger collapse)
            _selectedColorIndex = 0;
            _playerColor = ColorRules.SelectableColors[0];
            for (int i = 0; i < _colorSwatches.Length; i++)
            {
                if (_colorSwatches[i] != null)
                {
                    if (i == 0)
                    {
                        _colorSwatches[i].AddToClassList("selected");
                    }
                    else
                    {
                        _colorSwatches[i].RemoveFromClassList("selected");
                    }
                }
            }

            Debug.Log("[SetupWizard] Reset to initial state");
        }
    }
}

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace DLYH.TableUI
{
    /// <summary>
    /// Data class for player/opponent info displayed in gameplay tabs.
    /// </summary>
    public class PlayerTabData
    {
        public string Name;
        public Color Color;
        public int GridSize;
        public int WordCount;
        public int MissCount;
        public int MissLimit;
        public string[] Words;
        public bool IsLocalPlayer;
    }

    /// <summary>
    /// Manages the gameplay UI screen including:
    /// - Tab switching between Attack (opponent's grid) and Defend (your grid)
    /// - Miss counter displays and guillotine overlay triggers
    /// - Letter keyboard state
    /// - Word row displays
    /// - Status messages
    /// - Guessed words list
    /// </summary>
    public class GameplayScreenManager
    {
        #region Private Fields

        private VisualElement _root;
        private bool _isInitialized;

        // Header elements
        private Button _hamburgerButton;
        private Label _turnIndicator;

        // Tab elements
        private VisualElement _tabAttack;
        private VisualElement _tabDefend;
        private Label _opponentNameLabel;
        private Label _playerNameLabel;
        private Label _opponentGridSizeLabel;
        private Label _opponentWordCountLabel;
        private Label _playerGridSizeLabel;
        private Label _playerWordCountLabel;
        private Button _opponentMissCounterButton;
        private Button _playerMissCounterButton;
        private Label _opponentMissCountText;
        private Label _playerMissCountText;
        private VisualElement _opponentMissFill;
        private VisualElement _playerMissFill;

        // Grid elements
        private VisualElement _gridArea;
        private VisualElement _tableContainer;
        private TableView _tableView;
        private TableModel _attackTableModel;
        private TableModel _defendTableModel;

        // Word rows
        private VisualElement _wordsSection;
        private VisualElement _wordRowsContainer;
        private WordRowsContainer _attackWordRows;
        private WordRowsContainer _defendWordRows;

        // Letter keyboard
        private VisualElement _letterKeyboard;
        private Dictionary<char, Button> _letterKeys = new Dictionary<char, Button>();
        private HashSet<char> _hitLetters = new HashSet<char>();
        private HashSet<char> _missLetters = new HashSet<char>();

        // Status bar
        private Label _statusMessage;

        // Guessed words
        private Button _guessedWordsButton;
        private VisualElement _guessedWordsPanel;
        private VisualElement _yourGuessesList;
        private VisualElement _opponentGuessesList;
        private Label _yourGuessesHeader;
        private Label _opponentGuessesHeader;
        private Button _closeGuessedButton;
        private List<GuessedWordEntry> _guessedWords = new List<GuessedWordEntry>();

        // Tab state
        private bool _isAttackTabActive = true;
        private bool _isPlayerTurn = true;

        // Player data
        private PlayerTabData _playerData;
        private PlayerTabData _opponentData;
        private Color _playerColor = ColorRules.SelectableColors[0];
        private Color _opponentColor = ColorRules.SelectableColors[1];

        // QWERTY keyboard layout
        private bool _useQwertyLayout = false;
        private static readonly string[] QWERTY_ROWS = new string[]
        {
            "QWERTYUIOP",
            "ASDFGHJKL",
            "ZXCVBNM"
        };
        private static readonly string[] ABC_ROWS = new string[]
        {
            "ABCDEFGHI",
            "JKLMNOPQR",
            "STUVWXYZ"
        };

        // Flavor text for danger levels
        private static readonly string[] FLAVOR_SAFE = new string[]
        {
            "Safe for now...",
            "Breathing easy.",
            "No worries yet."
        };
        private static readonly string[] FLAVOR_WARM = new string[]
        {
            "Getting warm...",
            "Starting to sweat.",
            "The blade rises."
        };
        private static readonly string[] FLAVOR_DANGER = new string[]
        {
            "In danger!",
            "Neck is exposed!",
            "One wrong move..."
        };
        private static readonly string[] FLAVOR_CRITICAL = new string[]
        {
            "CRITICAL!",
            "Final moments!",
            "Say your prayers!"
        };

        #endregion

        #region Events

        /// <summary>Fired when hamburger menu is clicked.</summary>
        public event Action OnHamburgerClicked;

        /// <summary>Fired when a letter key is clicked. Parameter: letter.</summary>
        public event Action<char> OnLetterKeyClicked;

        /// <summary>Fired when a grid cell is clicked. Parameters: row, col, isAttackGrid.</summary>
        public event Action<int, int, bool> OnGridCellClicked;

        /// <summary>Fired when a word guess button is clicked. Parameter: word index.</summary>
        public event Action<int> OnWordGuessClicked;

        /// <summary>Fired when miss counter is clicked to show guillotine overlay.</summary>
        public event Action OnShowGuillotineOverlay;

        /// <summary>Fired when attack tab is selected.</summary>
        public event Action OnAttackTabSelected;

        /// <summary>Fired when defend tab is selected.</summary>
        public event Action OnDefendTabSelected;

        #endregion

        #region Data Types

        private class GuessedWordEntry
        {
            public string PlayerName;
            public string Word;
            public bool WasHit;
            public bool IsPlayerGuess;
        }

        #endregion

        #region Initialization

        /// <summary>
        /// Initializes the gameplay screen manager with the given root element.
        /// </summary>
        public void Initialize(VisualElement root)
        {
            _root = root ?? throw new ArgumentNullException(nameof(root));

            QueryElements();
            WireEvents();
            BuildKeyboard();
            UpdateGuessedWordsButton();

            // Ensure overlay panels don't block clicks when hidden
            if (_guessedWordsPanel != null)
            {
                _guessedWordsPanel.pickingMode = PickingMode.Ignore;
            }

            _isInitialized = true;
        }

        /// <summary>
        /// Query and cache all UI elements.
        /// </summary>
        private void QueryElements()
        {
            // Header
            _hamburgerButton = _root.Q<Button>("btn-hamburger");
            _turnIndicator = _root.Q<Label>("turn-indicator");

            // Tabs
            _tabAttack = _root.Q<VisualElement>("tab-attack");
            _tabDefend = _root.Q<VisualElement>("tab-defend");
            _opponentNameLabel = _root.Q<Label>("opponent-name");
            _playerNameLabel = _root.Q<Label>("player-name");
            _opponentGridSizeLabel = _root.Q<Label>("opponent-grid-size");
            _opponentWordCountLabel = _root.Q<Label>("opponent-word-count");
            _playerGridSizeLabel = _root.Q<Label>("player-grid-size");
            _playerWordCountLabel = _root.Q<Label>("player-word-count");
            _opponentMissCounterButton = _root.Q<Button>("btn-opponent-miss-counter");
            _playerMissCounterButton = _root.Q<Button>("btn-player-miss-counter");
            _opponentMissCountText = _root.Q<Label>("opponent-miss-count");
            _playerMissCountText = _root.Q<Label>("player-miss-count");
            _opponentMissFill = _root.Q<VisualElement>("opponent-miss-fill");
            _playerMissFill = _root.Q<VisualElement>("player-miss-fill");

            // Grid
            _gridArea = _root.Q<VisualElement>("grid-area");
            _tableContainer = _root.Q<VisualElement>("table-container");

            // Word rows
            _wordsSection = _root.Q<VisualElement>("words-section");
            _wordRowsContainer = _root.Q<VisualElement>("word-rows-container");

            // Keyboard
            _letterKeyboard = _root.Q<VisualElement>("letter-keyboard");

            // Status
            _statusMessage = _root.Q<Label>("status-message");

            // Guessed words
            _guessedWordsButton = _root.Q<Button>("btn-guessed-words");
            _guessedWordsPanel = _root.Q<VisualElement>("guessed-words-panel");
            _yourGuessesList = _root.Q<VisualElement>("your-guesses-list");
            _opponentGuessesList = _root.Q<VisualElement>("opponent-guesses-list");
            _yourGuessesHeader = _root.Q<Label>("your-guesses-header");
            _opponentGuessesHeader = _root.Q<Label>("opponent-guesses-header");
            _closeGuessedButton = _root.Q<Button>("btn-close-guessed");
        }

        /// <summary>
        /// Wire up button click events.
        /// </summary>
        private void WireEvents()
        {
            if (_hamburgerButton != null)
            {
                _hamburgerButton.clicked += () => OnHamburgerClicked?.Invoke();
            }

            if (_tabAttack != null)
            {
                _tabAttack.RegisterCallback<ClickEvent>(evt => SelectAttackTab());
            }

            if (_tabDefend != null)
            {
                _tabDefend.RegisterCallback<ClickEvent>(evt => SelectDefendTab());
            }

            if (_opponentMissCounterButton != null)
            {
                _opponentMissCounterButton.clicked += () => OnShowGuillotineOverlay?.Invoke();
            }

            if (_playerMissCounterButton != null)
            {
                _playerMissCounterButton.clicked += () => OnShowGuillotineOverlay?.Invoke();
            }

            if (_guessedWordsButton != null)
            {
                _guessedWordsButton.clicked += ShowGuessedWordsPanel;
            }

            if (_closeGuessedButton != null)
            {
                _closeGuessedButton.clicked += HideGuessedWordsPanel;
            }
        }

        #endregion

        #region Keyboard

        /// <summary>
        /// Builds the letter keyboard based on current layout preference.
        /// </summary>
        public void BuildKeyboard()
        {
            if (_letterKeyboard == null) return;

            _letterKeyboard.Clear();
            _letterKeys.Clear();

            string[] rows = _useQwertyLayout ? QWERTY_ROWS : ABC_ROWS;

            foreach (string row in rows)
            {
                VisualElement rowElement = new VisualElement();
                rowElement.AddToClassList("keyboard-row");

                foreach (char letter in row)
                {
                    Button key = new Button();
                    key.text = letter.ToString();
                    key.AddToClassList("letter-key");

                    char capturedLetter = letter;
                    key.clicked += () => HandleLetterKeyClick(capturedLetter);

                    _letterKeys[letter] = key;
                    rowElement.Add(key);
                }

                _letterKeyboard.Add(rowElement);
            }

            // Re-apply hit/miss states
            RefreshKeyboardStates();
        }

        /// <summary>
        /// Sets whether to use QWERTY keyboard layout.
        /// </summary>
        public void SetQwertyLayout(bool useQwerty)
        {
            if (_useQwertyLayout != useQwerty)
            {
                _useQwertyLayout = useQwerty;
                BuildKeyboard();
            }
        }

        /// <summary>
        /// Handles letter key click.
        /// </summary>
        private void HandleLetterKeyClick(char letter)
        {
            OnLetterKeyClicked?.Invoke(letter);
        }

        /// <summary>
        /// Marks a letter as hit (found in opponent's words).
        /// </summary>
        public void MarkLetterHit(char letter, Color playerColor)
        {
            _hitLetters.Add(letter);
            _missLetters.Remove(letter);

            if (_letterKeys.TryGetValue(letter, out Button key))
            {
                key.RemoveFromClassList("letter-miss");
                key.AddToClassList("letter-hit");
                key.style.backgroundColor = playerColor;
                key.style.color = ColorRules.GetContrastingTextColor(playerColor);
            }
        }

        /// <summary>
        /// Marks a letter as miss (not in opponent's words).
        /// </summary>
        public void MarkLetterMiss(char letter)
        {
            if (_hitLetters.Contains(letter)) return; // Hit takes precedence

            _missLetters.Add(letter);

            if (_letterKeys.TryGetValue(letter, out Button key))
            {
                key.AddToClassList("letter-miss");
            }
        }

        /// <summary>
        /// Refreshes keyboard visual states from stored hit/miss sets.
        /// </summary>
        private void RefreshKeyboardStates()
        {
            foreach (KeyValuePair<char, Button> kvp in _letterKeys)
            {
                char letter = kvp.Key;
                Button key = kvp.Value;

                key.RemoveFromClassList("letter-hit");
                key.RemoveFromClassList("letter-miss");
                key.style.backgroundColor = StyleKeyword.Null;
                key.style.color = StyleKeyword.Null;

                if (_hitLetters.Contains(letter))
                {
                    key.AddToClassList("letter-hit");
                    key.style.backgroundColor = _playerColor;
                    key.style.color = ColorRules.GetContrastingTextColor(_playerColor);
                }
                else if (_missLetters.Contains(letter))
                {
                    key.AddToClassList("letter-miss");
                }
            }
        }

        /// <summary>
        /// Clears all letter keyboard states for a new game.
        /// </summary>
        public void ClearKeyboardStates()
        {
            _hitLetters.Clear();
            _missLetters.Clear();
            RefreshKeyboardStates();
        }

        #endregion

        #region Tab Switching

        /// <summary>
        /// Selects the Attack tab (opponent's grid).
        /// </summary>
        public void SelectAttackTab()
        {
            if (_isAttackTabActive) return;

            _isAttackTabActive = true;

            _tabAttack?.AddToClassList("tab-active");
            _tabDefend?.RemoveFromClassList("tab-active");

            // Switch grid model
            if (_tableView != null && _attackTableModel != null)
            {
                _tableView.Bind(_attackTableModel);
                _tableView.SetSetupMode(false);
            }

            // Switch word rows
            ShowAttackWordRows();

            OnAttackTabSelected?.Invoke();
        }

        /// <summary>
        /// Selects the Defend tab (your grid).
        /// </summary>
        public void SelectDefendTab()
        {
            if (!_isAttackTabActive) return;

            _isAttackTabActive = false;

            _tabDefend?.AddToClassList("tab-active");
            _tabAttack?.RemoveFromClassList("tab-active");

            // Switch grid model
            if (_tableView != null && _defendTableModel != null)
            {
                _tableView.Bind(_defendTableModel);
                _tableView.SetSetupMode(false);
            }

            // Switch word rows
            ShowDefendWordRows();

            OnDefendTabSelected?.Invoke();
        }

        /// <summary>
        /// Returns true if the attack tab is currently active.
        /// </summary>
        public bool IsAttackTabActive => _isAttackTabActive;

        #endregion

        #region Player Data

        /// <summary>
        /// Gets the current player data (for guillotine overlay sync).
        /// </summary>
        public PlayerTabData PlayerData => _playerData;

        /// <summary>
        /// Gets the current opponent data (for guillotine overlay sync).
        /// </summary>
        public PlayerTabData OpponentData => _opponentData;

        /// <summary>
        /// Sets the player and opponent data for the tabs.
        /// </summary>
        public void SetPlayerData(PlayerTabData playerData, PlayerTabData opponentData)
        {
            _playerData = playerData;
            _opponentData = opponentData;

            if (playerData != null)
            {
                _playerColor = playerData.Color;
            }

            if (opponentData != null)
            {
                _opponentColor = opponentData.Color;
            }

            UpdateTabDisplays();
        }

        /// <summary>
        /// Updates the tab UI elements with current player data.
        /// </summary>
        private void UpdateTabDisplays()
        {
            // Opponent tab (Attack)
            if (_opponentData != null)
            {
                if (_opponentNameLabel != null)
                {
                    _opponentNameLabel.text = _opponentData.Name ?? "OPPONENT";
                }
                if (_opponentGridSizeLabel != null)
                {
                    _opponentGridSizeLabel.text = $"{_opponentData.GridSize}x{_opponentData.GridSize}";
                }
                if (_opponentWordCountLabel != null)
                {
                    _opponentWordCountLabel.text = $"{_opponentData.WordCount} words";
                }

                UpdateOpponentMissDisplay();
            }

            // Player tab (Defend)
            if (_playerData != null)
            {
                if (_playerNameLabel != null)
                {
                    _playerNameLabel.text = _playerData.Name ?? "You";
                }
                if (_playerGridSizeLabel != null)
                {
                    _playerGridSizeLabel.text = $"{_playerData.GridSize}x{_playerData.GridSize}";
                }
                if (_playerWordCountLabel != null)
                {
                    _playerWordCountLabel.text = $"{_playerData.WordCount} words";
                }

                UpdatePlayerMissDisplay();
            }

            // Update color badges in tabs
            VisualElement playerBadge = _tabDefend?.Q<VisualElement>("player-color-badge");
            if (playerBadge != null && _playerData != null)
            {
                playerBadge.style.backgroundColor = _playerData.Color;
            }

            VisualElement opponentBadge = _tabAttack?.Q<VisualElement>("opponent-color-badge");
            if (opponentBadge != null && _opponentData != null)
            {
                opponentBadge.style.backgroundColor = _opponentData.Color;
            }
        }

        #endregion

        #region Miss Counter Updates

        /// <summary>
        /// Updates the player's miss count display.
        /// </summary>
        public void SetPlayerMissCount(int misses, int missLimit)
        {
            if (_playerData != null)
            {
                _playerData.MissCount = misses;
                _playerData.MissLimit = missLimit;
            }
            UpdatePlayerMissDisplay();
        }

        /// <summary>
        /// Updates the opponent's miss count display.
        /// </summary>
        public void SetOpponentMissCount(int misses, int missLimit)
        {
            if (_opponentData != null)
            {
                _opponentData.MissCount = misses;
                _opponentData.MissLimit = missLimit;
            }
            UpdateOpponentMissDisplay();
        }

        private void UpdatePlayerMissDisplay()
        {
            if (_playerData == null) return;

            int misses = _playerData.MissCount;
            int limit = _playerData.MissLimit;

            if (_playerMissCountText != null)
            {
                _playerMissCountText.text = $"{misses}/{limit}";
            }

            if (_playerMissFill != null)
            {
                float percent = limit > 0 ? (float)misses / limit * 100f : 0f;
                _playerMissFill.style.width = new StyleLength(new Length(percent, LengthUnit.Percent));
                UpdateDangerClass(_playerMissFill, percent);
            }
        }

        private void UpdateOpponentMissDisplay()
        {
            if (_opponentData == null) return;

            int misses = _opponentData.MissCount;
            int limit = _opponentData.MissLimit;

            if (_opponentMissCountText != null)
            {
                _opponentMissCountText.text = $"{misses}/{limit}";
            }

            if (_opponentMissFill != null)
            {
                float percent = limit > 0 ? (float)misses / limit * 100f : 0f;
                _opponentMissFill.style.width = new StyleLength(new Length(percent, LengthUnit.Percent));
                UpdateDangerClass(_opponentMissFill, percent);
            }
        }

        private void UpdateDangerClass(VisualElement fillBar, float percent)
        {
            fillBar.RemoveFromClassList("danger-low");
            fillBar.RemoveFromClassList("danger-medium");
            fillBar.RemoveFromClassList("danger-high");
            fillBar.RemoveFromClassList("danger-critical");

            if (percent >= 95)
            {
                fillBar.AddToClassList("danger-critical");
            }
            else if (percent >= 80)
            {
                fillBar.AddToClassList("danger-high");
            }
            else if (percent >= 50)
            {
                fillBar.AddToClassList("danger-medium");
            }
            else
            {
                fillBar.AddToClassList("danger-low");
            }
        }

        /// <summary>
        /// Gets flavor text based on danger level (miss percentage).
        /// </summary>
        public string GetFlavorText(float missPercent)
        {
            string[] pool;
            if (missPercent >= 95)
            {
                pool = FLAVOR_CRITICAL;
            }
            else if (missPercent >= 80)
            {
                pool = FLAVOR_DANGER;
            }
            else if (missPercent >= 50)
            {
                pool = FLAVOR_WARM;
            }
            else
            {
                pool = FLAVOR_SAFE;
            }

            return pool[UnityEngine.Random.Range(0, pool.Length)];
        }

        #endregion

        #region Turn Indicator

        /// <summary>
        /// Sets whether it's the player's turn and updates the display.
        /// </summary>
        public void SetPlayerTurn(bool isPlayerTurn)
        {
            _isPlayerTurn = isPlayerTurn;

            if (_turnIndicator != null)
            {
                _turnIndicator.text = isPlayerTurn ? "YOUR TURN" : "OPPONENT'S TURN";
                _turnIndicator.EnableInClassList("opponent-turn", !isPlayerTurn);
            }
        }

        #endregion

        #region Status Messages

        /// <summary>
        /// Sets the status message at the bottom of the screen.
        /// </summary>
        public void SetStatusMessage(string message, StatusType type = StatusType.Normal)
        {
            if (_statusMessage == null) return;

            _statusMessage.text = message;

            _statusMessage.RemoveFromClassList("status-hit");
            _statusMessage.RemoveFromClassList("status-miss");
            _statusMessage.RemoveFromClassList("status-word-found");

            switch (type)
            {
                case StatusType.Hit:
                    _statusMessage.AddToClassList("status-hit");
                    break;
                case StatusType.Miss:
                    _statusMessage.AddToClassList("status-miss");
                    break;
                case StatusType.WordFound:
                    _statusMessage.AddToClassList("status-word-found");
                    break;
            }
        }

        public enum StatusType
        {
            Normal,
            Hit,
            Miss,
            WordFound
        }

        #endregion

        #region Guessed Words Panel

        /// <summary>
        /// Adds a word to the guessed words list.
        /// </summary>
        public void AddGuessedWord(string playerName, string word, bool wasHit, bool isPlayerGuess)
        {
            GuessedWordEntry entry = new GuessedWordEntry
            {
                PlayerName = playerName,
                Word = word,
                WasHit = wasHit,
                IsPlayerGuess = isPlayerGuess
            };

            _guessedWords.Add(entry);
            UpdateGuessedWordsButton();
        }

        /// <summary>
        /// Clears all guessed words for a new game.
        /// </summary>
        public void ClearGuessedWords()
        {
            _guessedWords.Clear();
            UpdateGuessedWordsButton();

            _yourGuessesList?.Clear();
            _opponentGuessesList?.Clear();
        }

        private void UpdateGuessedWordsButton()
        {
            if (_guessedWordsButton != null)
            {
                int yourCount = _guessedWords.FindAll(e => e.IsPlayerGuess).Count;
                int opponentCount = _guessedWords.FindAll(e => !e.IsPlayerGuess).Count;
                _guessedWordsButton.text = $"Guessed Words: You ({yourCount}) | Opponent ({opponentCount})";
            }
        }

        private void ShowGuessedWordsPanel()
        {
            if (_guessedWordsPanel == null) return;

            // Separate guesses by player
            var yourGuesses = _guessedWords.FindAll(e => e.IsPlayerGuess);
            var opponentGuesses = _guessedWords.FindAll(e => !e.IsPlayerGuess);

            // Update headers with counts
            if (_yourGuessesHeader != null)
            {
                _yourGuessesHeader.text = $"Your Guesses ({yourGuesses.Count})";
            }
            if (_opponentGuessesHeader != null)
            {
                string opponentName = _opponentData?.Name ?? "Opponent";
                _opponentGuessesHeader.text = $"{opponentName}'s Guesses ({opponentGuesses.Count})";
            }

            // Rebuild your guesses list
            if (_yourGuessesList != null)
            {
                _yourGuessesList.Clear();

                if (yourGuesses.Count == 0)
                {
                    Label emptyLabel = new Label("No guesses yet");
                    emptyLabel.AddToClassList("guessed-section-empty");
                    _yourGuessesList.Add(emptyLabel);
                }
                else
                {
                    foreach (GuessedWordEntry entry in yourGuesses)
                    {
                        _yourGuessesList.Add(CreateGuessedWordRow(entry));
                    }
                }
            }

            // Rebuild opponent guesses list
            if (_opponentGuessesList != null)
            {
                _opponentGuessesList.Clear();

                if (opponentGuesses.Count == 0)
                {
                    Label emptyLabel = new Label("No guesses yet");
                    emptyLabel.AddToClassList("guessed-section-empty");
                    _opponentGuessesList.Add(emptyLabel);
                }
                else
                {
                    foreach (GuessedWordEntry entry in opponentGuesses)
                    {
                        _opponentGuessesList.Add(CreateGuessedWordRow(entry));
                    }
                }
            }

            _guessedWordsPanel.RemoveFromClassList("hidden");
            _guessedWordsPanel.pickingMode = PickingMode.Position;
        }

        private VisualElement CreateGuessedWordRow(GuessedWordEntry entry)
        {
            VisualElement row = new VisualElement();
            row.AddToClassList("guessed-word-entry");

            Label wordLabel = new Label(entry.Word);
            wordLabel.AddToClassList("guessed-word-text");
            row.Add(wordLabel);

            Label resultLabel = new Label(entry.WasHit ? "HIT" : "MISS");
            resultLabel.AddToClassList("guessed-word-result");
            resultLabel.AddToClassList(entry.WasHit ? "result-hit" : "result-miss");
            row.Add(resultLabel);

            return row;
        }

        private void HideGuessedWordsPanel()
        {
            if (_guessedWordsPanel != null)
            {
                _guessedWordsPanel.AddToClassList("hidden");
                _guessedWordsPanel.pickingMode = PickingMode.Ignore;
            }
        }

        #endregion

        #region Grid and Word Rows

        /// <summary>
        /// Sets the table view for grid rendering.
        /// </summary>
        public void SetTableView(TableView tableView)
        {
            _tableView = tableView;

            if (_tableView != null)
            {
                _tableView.OnCellClicked += HandleGridCellClick;
            }
        }

        /// <summary>
        /// Sets the table models for attack and defend grids.
        /// </summary>
        public void SetTableModels(TableModel attackModel, TableModel defendModel)
        {
            _attackTableModel = attackModel;
            _defendTableModel = defendModel;

            // Bind initial model (attack by default)
            if (_tableView != null && _attackTableModel != null && _isAttackTabActive)
            {
                _tableView.Bind(_attackTableModel);
                _tableView.SetSetupMode(false);
            }
        }

        /// <summary>
        /// Sets the word row containers for attack and defend views.
        /// </summary>
        public void SetWordRowContainers(WordRowsContainer attackRows, WordRowsContainer defendRows)
        {
            _attackWordRows = attackRows;
            _defendWordRows = defendRows;

            // Wire events
            if (_attackWordRows != null)
            {
                _attackWordRows.OnGuessRequested += (wordIndex, word) => OnWordGuessClicked?.Invoke(wordIndex);
            }

            // Show initial rows
            if (_isAttackTabActive)
            {
                ShowAttackWordRows();
            }
            else
            {
                ShowDefendWordRows();
            }
        }

        private void ShowAttackWordRows()
        {
            if (_wordRowsContainer == null) return;

            _wordRowsContainer.Clear();

            if (_attackWordRows != null)
            {
                _wordRowsContainer.Add(_attackWordRows.Root);
            }
        }

        private void ShowDefendWordRows()
        {
            if (_wordRowsContainer == null) return;

            _wordRowsContainer.Clear();

            if (_defendWordRows != null)
            {
                _wordRowsContainer.Add(_defendWordRows.Root);
            }
        }

        private void HandleGridCellClick(int row, int col, TableCell cell)
        {
            // Only allow clicks on grid cells (not headers)
            if (cell.Kind != TableCellKind.GridCell) return;

            OnGridCellClicked?.Invoke(row, col, _isAttackTabActive);
        }

        #endregion

        #region Visual Effects

        /// <summary>
        /// Flashes the screen briefly to indicate a miss.
        /// </summary>
        public void FlashMiss()
        {
            if (_root == null) return;

            VisualElement gameplayRoot = _root.Q<VisualElement>("gameplay-root");
            if (gameplayRoot != null)
            {
                gameplayRoot.AddToClassList("flash-miss");

                // Remove class after brief delay (handled by USS transition)
                gameplayRoot.schedule.Execute(() =>
                {
                    gameplayRoot.RemoveFromClassList("flash-miss");
                }).StartingIn(100);
            }
        }

        #endregion

        #region Reset

        /// <summary>
        /// Resets the gameplay screen for a new game.
        /// </summary>
        public void Reset()
        {
            ClearKeyboardStates();
            ClearGuessedWords();
            SetStatusMessage("");
            SelectAttackTab();
            SetPlayerTurn(true);

            _playerData = null;
            _opponentData = null;
        }

        #endregion

        #region Cleanup

        /// <summary>
        /// Cleans up event subscriptions.
        /// </summary>
        public void Dispose()
        {
            if (_tableView != null)
            {
                _tableView.OnCellClicked -= HandleGridCellClick;
            }

            _letterKeys.Clear();
            _hitLetters.Clear();
            _missLetters.Clear();
            _guessedWords.Clear();
        }

        #endregion
    }
}

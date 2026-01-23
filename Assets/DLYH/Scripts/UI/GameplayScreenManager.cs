using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace DLYH.TableUI
{
    /// <summary>
    /// State of a letter key on the gameplay keyboard.
    /// </summary>
    public enum LetterKeyState
    {
        Default,    // Not yet guessed
        Hit,        // Letter AND coordinate known (player color)
        Miss,       // Letter not in any word (red)
        Found       // Letter in words but no coordinate known yet (yellow)
    }

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
        private VisualElement _opponentColorSwatch;
        private VisualElement _playerColorSwatch;
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
        private HashSet<char> _hitLetters = new HashSet<char>();      // All coords known - player color
        private HashSet<char> _foundLetters = new HashSet<char>();    // Letter known but not all coords - yellow
        private HashSet<char> _missLetters = new HashSet<char>();     // Letter not in any word - red

        // Opponent keyboard state (shown on Defend tab)
        private HashSet<char> _opponentHitLetters = new HashSet<char>(); // All coords known - show opponent color
        private HashSet<char> _opponentFoundLetters = new HashSet<char>(); // Letter known but not all coords - show yellow
        private HashSet<char> _opponentMissLetters = new HashSet<char>();

        // Tab switching control
        private bool _allowManualTabSwitch = true;

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
            public Color GuesserColor; // Color of the guesser (player or opponent color)
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
            _opponentColorSwatch = _root.Q<VisualElement>("opponent-color-swatch");
            _playerColorSwatch = _root.Q<VisualElement>("player-color-swatch");
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
        /// Applies viewport-aware sizing to keyboard keys.
        /// Keys are scaled to match word row cell size (85% of grid cell size).
        /// </summary>
        /// <param name="gridCellSize">The grid cell size in pixels</param>
        /// <param name="gridFontSize">The grid font size in pixels</param>
        public void ApplyKeyboardViewportSizing(int gridCellSize, int gridFontSize)
        {
            if (gridCellSize <= 0) return;

            // Keyboard keys should match word row cells (85% of grid)
            int keySize = Mathf.Max(24, (int)(gridCellSize * 0.85f));
            int keyFontSize = Mathf.Max(10, (int)(gridFontSize * 0.85f));

            foreach (KeyValuePair<char, Button> kvp in _letterKeys)
            {
                Button key = kvp.Value;
                key.style.width = keySize;
                key.style.height = keySize;
                key.style.fontSize = keyFontSize;
            }
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
        /// Only allows clicks when on Attack tab (player's keyboard is interactive).
        /// </summary>
        private void HandleLetterKeyClick(char letter)
        {
            // Only allow keyboard clicks when on Attack tab
            if (!_isAttackTabActive) return;

            OnLetterKeyClicked?.Invoke(letter);
        }

        /// <summary>
        /// Marks a letter as hit (letter AND coordinate known) - player color.
        /// </summary>
        public void MarkLetterHit(char letter, Color playerColor)
        {
            letter = char.ToUpper(letter);
            _hitLetters.Add(letter);
            _foundLetters.Remove(letter); // Upgrade from Found to Hit
            _missLetters.Remove(letter);

            if (_letterKeys.TryGetValue(letter, out Button key))
            {
                key.RemoveFromClassList("letter-miss");
                key.RemoveFromClassList("letter-found");
                key.AddToClassList("letter-hit");
                key.style.backgroundColor = playerColor;
                key.style.color = ColorRules.GetContrastingTextColor(playerColor);
                Debug.Log($"[GameplayScreenManager] Letter '{letter}' upgraded to Hit (player color)");
            }
        }

        /// <summary>
        /// Marks a letter as miss (not in opponent's words) - red.
        /// </summary>
        public void MarkLetterMiss(char letter)
        {
            letter = char.ToUpper(letter);
            if (_hitLetters.Contains(letter)) return; // Hit takes precedence

            _missLetters.Add(letter);

            if (_letterKeys.TryGetValue(letter, out Button key))
            {
                key.RemoveFromClassList("letter-hit");
                key.RemoveFromClassList("letter-found");
                key.AddToClassList("letter-miss");
                key.style.backgroundColor = ColorRules.SystemRed;
                key.style.color = Color.white;
            }
        }

        /// <summary>
        /// Refreshes keyboard visual states from stored hit/found/miss sets.
        /// Priority: Hit > Found > Miss
        /// </summary>
        private void RefreshKeyboardStates()
        {
            foreach (KeyValuePair<char, Button> kvp in _letterKeys)
            {
                char letter = kvp.Key;
                Button key = kvp.Value;

                key.RemoveFromClassList("letter-hit");
                key.RemoveFromClassList("letter-miss");
                key.RemoveFromClassList("letter-found");
                key.style.backgroundColor = StyleKeyword.Null;
                key.style.color = StyleKeyword.Null;

                if (_hitLetters.Contains(letter))
                {
                    key.AddToClassList("letter-hit");
                    key.style.backgroundColor = _playerColor;
                    key.style.color = ColorRules.GetContrastingTextColor(_playerColor);
                }
                else if (_foundLetters.Contains(letter))
                {
                    key.AddToClassList("letter-found");
                    key.style.backgroundColor = ColorRules.SystemYellow;
                    key.style.color = ColorRules.GetContrastingTextColor(ColorRules.SystemYellow);
                }
                else if (_missLetters.Contains(letter))
                {
                    key.AddToClassList("letter-miss");
                    key.style.backgroundColor = ColorRules.SystemRed;
                    key.style.color = Color.white;
                }
            }
        }

        /// <summary>
        /// Clears all letter keyboard states for a new game.
        /// </summary>
        public void ClearKeyboardStates()
        {
            _hitLetters.Clear();
            _foundLetters.Clear();
            _missLetters.Clear();
            RefreshKeyboardStates();
        }

        /// <summary>
        /// Sets the state of a letter key on the keyboard.
        /// Convenience method that delegates to MarkLetterHit/MarkLetterMiss.
        /// </summary>
        public void SetKeyboardLetterState(char letter, LetterKeyState state)
        {
            switch (state)
            {
                case LetterKeyState.Hit:
                    // Letter AND coordinate known - player color
                    MarkLetterHit(letter, _playerColor);
                    break;
                case LetterKeyState.Miss:
                    // Letter not in any word - red
                    MarkLetterMiss(letter);
                    break;
                case LetterKeyState.Found:
                    // Letter in words but no coordinate known yet - yellow
                    MarkLetterFound(letter);
                    break;
                case LetterKeyState.Default:
                    // Remove from all sets and refresh
                    letter = char.ToUpper(letter);
                    _hitLetters.Remove(letter);
                    _foundLetters.Remove(letter);
                    _missLetters.Remove(letter);
                    if (_letterKeys.TryGetValue(letter, out Button key))
                    {
                        key.RemoveFromClassList("letter-hit");
                        key.RemoveFromClassList("letter-miss");
                        key.RemoveFromClassList("letter-found");
                        key.style.backgroundColor = StyleKeyword.Null;
                        key.style.color = StyleKeyword.Null;
                    }
                    break;
            }
        }

        /// <summary>
        /// Marks a letter key as found (yellow) - letter exists but no coordinate known.
        /// Public for word guess reveal logic.
        /// </summary>
        public void MarkLetterFound(char letter)
        {
            letter = char.ToUpper(letter);

            // Don't downgrade from Hit to Found - Hit takes precedence
            if (_hitLetters.Contains(letter))
            {
                Debug.Log($"[GameplayScreenManager] Letter '{letter}' already Hit, not downgrading to Found");
                return;
            }

            _foundLetters.Add(letter); // Track as "found" letter (yellow)
            _missLetters.Remove(letter);

            if (_letterKeys.TryGetValue(letter, out Button key))
            {
                key.RemoveFromClassList("letter-miss");
                key.RemoveFromClassList("letter-hit");
                key.AddToClassList("letter-found");
                key.style.backgroundColor = ColorRules.SystemYellow;
                key.style.color = ColorRules.GetContrastingTextColor(ColorRules.SystemYellow);
                Debug.Log($"[GameplayScreenManager] Letter '{letter}' marked as Found (yellow)");
            }
        }

        #endregion

        #region Tab Switching

        /// <summary>
        /// Selects the Attack tab (opponent's grid).
        /// Called by UI click or auto-switch on turn change.
        /// </summary>
        /// <param name="isAutoSwitch">True if called by turn system, false if user clicked</param>
        public void SelectAttackTab(bool isAutoSwitch = false)
        {
            // Block manual switching if not allowed
            if (!isAutoSwitch && !_allowManualTabSwitch) return;
            if (_isAttackTabActive) return;

            _isAttackTabActive = true;

            _tabAttack?.AddToClassList("tab-active");
            _tabDefend?.RemoveFromClassList("tab-active");

            // Switch grid model
            if (_tableView != null && _attackTableModel != null)
            {
                // Attack grid: Player1=player's hit markers, Player2=opponent's letters (hidden)
                _tableView.SetPlayerColors(_playerColor, _opponentColor);
                _tableView.Bind(_attackTableModel);
                _tableView.SetSetupMode(false);
                _tableView.SetDefenseGrid(false); // Attack grid - hide letters in Revealed state
            }

            // Switch word rows
            ShowAttackWordRows();

            // Switch keyboard to show player's guesses
            RefreshKeyboardForCurrentTab();

            OnAttackTabSelected?.Invoke();
        }

        /// <summary>
        /// Selects the Defend tab (your grid).
        /// Called by UI click or auto-switch on turn change.
        /// </summary>
        /// <param name="isAutoSwitch">True if called by turn system, false if user clicked</param>
        public void SelectDefendTab(bool isAutoSwitch = false)
        {
            // Block manual switching if not allowed
            if (!isAutoSwitch && !_allowManualTabSwitch) return;
            if (!_isAttackTabActive) return;

            _isAttackTabActive = false;

            _tabDefend?.AddToClassList("tab-active");
            _tabAttack?.RemoveFromClassList("tab-active");

            // Switch grid model
            if (_tableView != null && _defendTableModel != null)
            {
                // Defense grid: Player1=player's letters, Player2=opponent's hit markers
                Debug.Log($"[GameplayScreenManager] SelectDefendTab - Setting colors: Player RGB({_playerColor.r:F2}, {_playerColor.g:F2}, {_playerColor.b:F2}), Opponent RGB({_opponentColor.r:F2}, {_opponentColor.g:F2}, {_opponentColor.b:F2})");
                _tableView.SetPlayerColors(_playerColor, _opponentColor);
                _tableView.Bind(_defendTableModel);
                _tableView.SetSetupMode(false);
                _tableView.SetDefenseGrid(true); // Defense grid - show letters in Revealed state
            }

            // Switch word rows
            ShowDefendWordRows();

            // Switch keyboard to show opponent's guesses
            RefreshKeyboardForCurrentTab();

            OnDefendTabSelected?.Invoke();
        }

        /// <summary>
        /// Returns true if the attack tab is currently active.
        /// </summary>
        public bool IsAttackTabActive => _isAttackTabActive;

        /// <summary>
        /// Controls whether the user can manually switch tabs.
        /// Set to false during opponent's turn.
        /// </summary>
        public void SetAllowManualTabSwitch(bool allow)
        {
            _allowManualTabSwitch = allow;
        }

        /// <summary>
        /// Refreshes the keyboard display to show the correct player's guesses
        /// based on which tab is active.
        /// </summary>
        private void RefreshKeyboardForCurrentTab()
        {
            // Clear all keyboard visual states first
            foreach (KeyValuePair<char, Button> kvp in _letterKeys)
            {
                Button key = kvp.Value;
                key.RemoveFromClassList("letter-hit");
                key.RemoveFromClassList("letter-miss");
                key.RemoveFromClassList("letter-found");
                key.style.backgroundColor = StyleKeyword.Null;
                key.style.color = StyleKeyword.Null;
            }

            // Apply the appropriate keyboard state
            if (_isAttackTabActive)
            {
                // Show player's guesses (attack keyboard)
                // Priority: Hit > Found > Miss
                foreach (char letter in _hitLetters)
                {
                    if (_letterKeys.TryGetValue(letter, out Button key))
                    {
                        key.AddToClassList("letter-hit");
                        key.style.backgroundColor = _playerColor;
                        key.style.color = ColorRules.GetContrastingTextColor(_playerColor);
                    }
                }
                foreach (char letter in _foundLetters)
                {
                    // Only show Found if not already Hit
                    if (!_hitLetters.Contains(letter) && _letterKeys.TryGetValue(letter, out Button key))
                    {
                        key.AddToClassList("letter-found");
                        key.style.backgroundColor = ColorRules.SystemYellow;
                        key.style.color = ColorRules.GetContrastingTextColor(ColorRules.SystemYellow);
                    }
                }
                foreach (char letter in _missLetters)
                {
                    // Only show Miss if not Hit or Found
                    if (!_hitLetters.Contains(letter) && !_foundLetters.Contains(letter) && _letterKeys.TryGetValue(letter, out Button key))
                    {
                        key.AddToClassList("letter-miss");
                        key.style.backgroundColor = ColorRules.SystemRed;
                        key.style.color = Color.white;
                    }
                }
            }
            else
            {
                // Show opponent's guesses (defend keyboard) - dim unguessed letters
                // First, dim all letters to indicate keyboard is view-only
                foreach (KeyValuePair<char, Button> kvp in _letterKeys)
                {
                    char letter = kvp.Key;
                    Button key = kvp.Value;

                    if (_opponentHitLetters.Contains(letter))
                    {
                        // Hit - all coords known, show opponent color
                        key.AddToClassList("letter-hit");
                        key.RemoveFromClassList("letter-found");
                        key.RemoveFromClassList("letter-miss");
                        key.style.backgroundColor = _opponentColor;
                        key.style.color = ColorRules.GetContrastingTextColor(_opponentColor);
                    }
                    else if (_opponentFoundLetters.Contains(letter))
                    {
                        // Found - letter known but not all coords, show yellow
                        key.AddToClassList("letter-found");
                        key.RemoveFromClassList("letter-hit");
                        key.RemoveFromClassList("letter-miss");
                        key.style.backgroundColor = ColorRules.SystemYellow;
                        key.style.color = ColorRules.GetContrastingTextColor(ColorRules.SystemYellow);
                    }
                    else if (_opponentMissLetters.Contains(letter))
                    {
                        // Miss - show red
                        key.AddToClassList("letter-miss");
                        key.RemoveFromClassList("letter-hit");
                        key.RemoveFromClassList("letter-found");
                        key.style.backgroundColor = ColorRules.SystemRed;
                        key.style.color = Color.white;
                    }
                    else
                    {
                        // Unguessed - dim/grey out to indicate view-only
                        key.RemoveFromClassList("letter-hit");
                        key.RemoveFromClassList("letter-found");
                        key.RemoveFromClassList("letter-miss");
                        key.style.backgroundColor = new Color(0.3f, 0.3f, 0.3f, 0.6f);
                        key.style.color = new Color(0.6f, 0.6f, 0.6f, 1f);
                    }
                }
            }
        }

        #endregion

        #region Opponent Keyboard State

        /// <summary>
        /// Marks a letter as hit by opponent with all coords known (shown in opponent color on Defend tab keyboard).
        /// </summary>
        public void MarkOpponentLetterHit(char letter, Color opponentColor)
        {
            letter = char.ToUpper(letter);
            _opponentHitLetters.Add(letter);
            _opponentFoundLetters.Remove(letter);
            _opponentMissLetters.Remove(letter);
            _opponentColor = opponentColor; // Update color in case it's different

            // If defend tab is active, update display immediately
            if (!_isAttackTabActive)
            {
                RefreshKeyboardForCurrentTab();
            }
        }

        /// <summary>
        /// Legacy method - marks letter as hit with default opponent color.
        /// </summary>
        public void MarkOpponentLetterHit(char letter)
        {
            MarkOpponentLetterHit(letter, _opponentColor);
        }

        /// <summary>
        /// Marks a letter as found by opponent but not all coords known (shown in yellow on Defend tab keyboard).
        /// </summary>
        public void MarkOpponentLetterFound(char letter)
        {
            letter = char.ToUpper(letter);
            // Don't downgrade from Hit to Found
            if (_opponentHitLetters.Contains(letter)) return;

            _opponentFoundLetters.Add(letter);
            _opponentMissLetters.Remove(letter);

            // If defend tab is active, update display immediately
            if (!_isAttackTabActive)
            {
                RefreshKeyboardForCurrentTab();
            }
        }

        /// <summary>
        /// Marks a letter as missed by opponent (shown on Defend tab keyboard).
        /// </summary>
        public void MarkOpponentLetterMiss(char letter)
        {
            letter = char.ToUpper(letter);
            if (_opponentHitLetters.Contains(letter)) return;
            if (_opponentFoundLetters.Contains(letter)) return;

            _opponentMissLetters.Add(letter);

            // If defend tab is active, update display immediately
            if (!_isAttackTabActive)
            {
                RefreshKeyboardForCurrentTab();
            }
        }

        /// <summary>
        /// Clears opponent's keyboard state for a new game.
        /// </summary>
        public void ClearOpponentKeyboardStates()
        {
            _opponentHitLetters.Clear();
            _opponentFoundLetters.Clear();
            _opponentMissLetters.Clear();
        }

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
                Debug.Log($"[GameplayScreenManager] SetPlayerData - Player color: RGB({_playerColor.r:F2}, {_playerColor.g:F2}, {_playerColor.b:F2})");
            }

            if (opponentData != null)
            {
                _opponentColor = opponentData.Color;
                Debug.Log($"[GameplayScreenManager] SetPlayerData - Opponent color: RGB({_opponentColor.r:F2}, {_opponentColor.g:F2}, {_opponentColor.b:F2})");
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

            // Update color swatches in tabs
            if (_playerColorSwatch != null && _playerData != null)
            {
                _playerColorSwatch.style.backgroundColor = _playerData.Color;
            }

            if (_opponentColorSwatch != null && _opponentData != null)
            {
                _opponentColorSwatch.style.backgroundColor = _opponentData.Color;
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

        /// <summary>
        /// Updates the opponent's name in the UI when they join during gameplay.
        /// Used for private games where host starts before opponent joins.
        /// </summary>
        public void SetOpponentName(string name)
        {
            if (_opponentData != null)
            {
                _opponentData.Name = name;
            }
            if (_opponentNameLabel != null)
            {
                _opponentNameLabel.text = name ?? "OPPONENT";
            }
            Debug.Log($"[GameplayScreenManager] Updated opponent name to: {name}");
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
        /// <param name="playerName">Name of the guesser</param>
        /// <param name="word">The word that was guessed</param>
        /// <param name="wasHit">True if guess was correct</param>
        /// <param name="isPlayerGuess">True if this was the player's guess, false for opponent</param>
        /// <param name="guesserColor">Optional color for correct guesses (player/opponent color)</param>
        public void AddGuessedWord(string playerName, string word, bool wasHit, bool isPlayerGuess, Color? guesserColor = null)
        {
            GuessedWordEntry entry = new GuessedWordEntry
            {
                PlayerName = playerName,
                Word = word,
                WasHit = wasHit,
                IsPlayerGuess = isPlayerGuess,
                GuesserColor = guesserColor ?? (isPlayerGuess ? _playerColor : _opponentColor)
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

            // Set background color based on result
            // Correct = guesser's color, Incorrect = red
            if (entry.WasHit)
            {
                row.style.backgroundColor = entry.GuesserColor;
            }
            else
            {
                row.style.backgroundColor = ColorRules.SystemRed;
            }

            Label wordLabel = new Label(entry.Word);
            wordLabel.AddToClassList("guessed-word-text");
            // Use contrasting text color
            wordLabel.style.color = ColorRules.GetContrastingTextColor(
                entry.WasHit ? entry.GuesserColor : ColorRules.SystemRed);
            row.Add(wordLabel);

            Label resultLabel = new Label(entry.WasHit ? "CORRECT" : "WRONG");
            resultLabel.AddToClassList("guessed-word-result");
            resultLabel.AddToClassList(entry.WasHit ? "result-hit" : "result-miss");
            resultLabel.style.color = ColorRules.GetContrastingTextColor(
                entry.WasHit ? entry.GuesserColor : ColorRules.SystemRed);
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
                _tableView.SetDefenseGrid(false); // Attack grid - hide letters in Revealed state
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
            ClearOpponentKeyboardStates();
            ClearGuessedWords();
            SetStatusMessage("");
            SelectAttackTab(true); // Force switch even if tab switching disabled
            SetPlayerTurn(true);
            SetAllowManualTabSwitch(true);

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

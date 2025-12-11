using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using Sirenix.OdinInspector;
using System;

namespace TecVooDoo.DontLoseYourHead.UI
{
    /// <summary>
    /// Manages a single word pattern row in the UI.
    /// Setup Mode: Shows word entry with select/coordinate/delete buttons
    /// Gameplay Mode: Shows word pattern with revealed letters
    /// Word Guess Mode: Allows player to type letters into unrevealed positions
    /// Uses a single combined text field showing "1. _ _ _" format.
    /// Implements IPointerClickHandler to make the row selectable by clicking.
    /// </summary>
    public class WordPatternRow : MonoBehaviour, IPointerClickHandler
    {
        #region Enums
        /// <summary>
        /// Current state of this word row
        /// </summary>
        public enum RowState
        {
            /// <summary>No word entered yet, showing underscores</summary>
            Empty,
            /// <summary>Currently typing/entering a word</summary>
            Entering,
            /// <summary>Word entered but not yet placed on grid</summary>
            WordEntered,
            /// <summary>Word placed on grid, setup complete for this row</summary>
            Placed,
            /// <summary>Gameplay mode - showing discovered pattern</summary>
            Gameplay
        }
        #endregion

        #region Serialized Fields - References
        [TitleGroup("References")]
        [SerializeField, Required, Tooltip("Single text field showing '1. _ _ _' format")]
        private TextMeshProUGUI _combinedText;

        [SerializeField, Tooltip("Button to select this row for input (was AcceptButton)")]
        private Button _selectButton;

        [SerializeField]
        private Button _coordinateModeButton;

        [SerializeField]
        private Button _deleteButton;

        [SerializeField]
        private Image _backgroundImage;

        [TitleGroup("Button Icons")]
        [SerializeField]
        private Image _selectButtonIcon;

        [SerializeField]
        private Image _coordinateModeIcon;

        [SerializeField]
        private Image _deleteButtonIcon;

        [TitleGroup("Gameplay Guess Buttons")]
        [SerializeField, Tooltip("Button to enter word guess mode")]
        private Button _guessWordButton;

        [SerializeField, Tooltip("Backspace button for guess mode")]
        private Button _guessBackspaceButton;

        [SerializeField, Tooltip("Accept/checkmark button for guess mode")]
        private Button _guessAcceptButton;

        [SerializeField, Tooltip("Cancel/X button for guess mode")]
        private Button _guessCancelButton;
        #endregion

        #region Serialized Fields - Configuration
        [TitleGroup("Configuration")]
        [SerializeField, Range(3, 6)]
        private int _requiredWordLength = 3;

        [SerializeField]
        private int _rowNumber = 1;

        [TitleGroup("Colors")]
        [SerializeField]
        private Color _emptyColor = new Color(0.95f, 0.95f, 0.95f, 1f);

        [SerializeField]
        private Color _enteringColor = new Color(1f, 1f, 0.8f, 1f);

        [SerializeField]
        private Color _wordEnteredColor = new Color(0.8f, 1f, 0.8f, 1f);

        [SerializeField]
        private Color _placedColor = new Color(0.7f, 0.9f, 0.7f, 1f);

        [SerializeField]
        private Color _selectedColor = new Color(0.8f, 0.9f, 1f, 1f);

        [SerializeField, Tooltip("Color for player-typed letters in guess mode")]
        private Color _guessTypedLetterColor = new Color(0.85f, 0.65f, 0f, 1f); // Gold/yellow

        [TitleGroup("Text Settings")]
        [SerializeField]
        private char _unknownLetterChar = '_';

        [SerializeField]
        private char _letterSeparator = ' ';

        [SerializeField]
        private string _numberSeparator = ". ";
        #endregion

        #region Private Fields
        private RowState _currentState = RowState.Empty;
        private string _currentWord = "";
        private string _enteredText = "";
        private bool[] _revealedLetters;
        private bool _isSelected;
        private Func<string, int, bool> _wordValidator;

        // Word Guess Mode fields
        private bool _inWordGuessMode = false;
        private bool _isOwnerPanel = false;
        private bool _wordSolved = false; // True when word has been correctly guessed - never show button again
 // Owner panels never show guess buttons

        private char[] _guessedLetters; // Player-typed letters (null char = not typed)
        private int _guessCursorPosition = 0;
        // Placement position tracking
        private int _placedStartCol = -1;
        private int _placedStartRow = -1;
        private int _placedDirCol = 0;
        private int _placedDirRow = 0;
        #endregion

        #region Events
        /// <summary>
        /// Fired when the row is clicked/selected. Parameter: row number (1-based)
        /// </summary>
        public event Action<int> OnRowSelected;

        /// <summary>
        /// Fired when coordinate mode button is clicked. Parameter: row number
        /// </summary>
        public event Action<int> OnCoordinateModeClicked;

        /// <summary>
        /// Fired when delete button is clicked. Parameter: row number, wasPlaced (true if word was on grid)
        /// </summary>
        public event Action<int, bool> OnDeleteClicked;

        /// <summary>
        /// Fired when word entry changes. Parameters: row number, current text
        /// </summary>
        public event Action<int, string> OnWordTextChanged;

        /// <summary>
        /// Fired when a word is auto-accepted (reached correct length). Parameter: row number, word
        /// </summary>
        public event Action<int, string> OnWordAccepted;

        /// <summary>
        /// Fired when an invalid word is rejected. Parameters: row number, rejected word
        /// </summary>
        public event Action<int, string> OnInvalidWordRejected;

        /// <summary>
        /// Fired when word guess mode is entered. Parameter: row number
        /// </summary>
        public event Action<int> OnWordGuessStarted;

        /// <summary>
        /// Fired when word guess is submitted. Parameters: row number, guessed word
        /// </summary>
        public event Action<int, string> OnWordGuessSubmitted;

        /// <summary>
        /// Fired when word guess mode is cancelled. Parameter: row number
        /// </summary>
        public event Action<int> OnWordGuessCancelled;
        #endregion

        #region Properties
        /// <summary>
        /// The row number (1-based) for display
        /// </summary>
        public int RowNumber => _rowNumber;

        /// <summary>
        /// Required word length for this row (3, 4, 5, or 6)
        /// </summary>
        public int RequiredWordLength => _requiredWordLength;

        /// <summary>
        /// Current state of this row
        /// </summary>
        public RowState CurrentState => _currentState;

        /// <summary>
        /// The confirmed word (after correct length is reached)
        /// </summary>
        public string CurrentWord => _currentWord;

        /// <summary>
        /// Text currently being entered
        /// </summary>
        public string EnteredText => _enteredText;

        /// <summary>
        /// Whether this row is currently selected for input
        /// </summary>
        public bool IsSelected => _isSelected;

        /// <summary>
        /// Whether a valid word has been entered (correct length reached)
        /// </summary>
        public bool HasWord => !string.IsNullOrEmpty(_currentWord);

        /// <summary>
        /// Whether the word has been placed on the grid
        /// </summary>
        public bool IsPlaced => _currentState == RowState.Placed;

        /// <summary>
        /// Whether this row is currently in word guess mode
        /// </summary>
        public bool InWordGuessMode => _inWordGuessMode;
        #endregion

        /// <summary>
        /// Starting column of placed word (-1 if not placed)
        /// </summary>
        public int PlacedStartCol => _placedStartCol;

        /// <summary>
        /// Starting row of placed word (-1 if not placed)
        /// </summary>
        public int PlacedStartRow => _placedStartRow;

        /// <summary>
        /// Column direction of placed word (1, 0, or -1)
        /// </summary>
        public int PlacedDirCol => _placedDirCol;

        /// <summary>
        /// Row direction of placed word (1, 0, or -1)
        /// </summary>
        public int PlacedDirRow => _placedDirRow;

        #region Unity Lifecycle
        private void Awake()
        {
            // Only create the array if it hasn't been set already
            // (SetGameplayWord may have been called before Awake on inactive objects)
            if (_revealedLetters == null || _revealedLetters.Length == 0)
            {
                _revealedLetters = new bool[_requiredWordLength];
            }

            // Initialize guessed letters array
            if (_guessedLetters == null || _guessedLetters.Length == 0)
            {
                _guessedLetters = new char[_requiredWordLength];
            }

            // If we already have a word set and are in Gameplay mode, update display
            if (_currentState == RowState.Gameplay && !string.IsNullOrEmpty(_currentWord))
            {
                UpdateDisplay();
                Debug.Log($"[WordPatternRow] Awake: Row {_rowNumber} already in Gameplay mode with word '{_currentWord}', refreshing display");
            }
        }

        private void OnEnable()
        {
            SubscribeToButtons();
        }

        private void OnDisable()
        {
            UnsubscribeFromButtons();
        }

        private void Start()
        {
            UpdateDisplay();
            UpdateButtonStates();
        }
        #endregion

        #region IPointerClickHandler
        /// <summary>
        /// Handles clicks on the row background to select it.
        /// </summary>
        public void OnPointerClick(PointerEventData eventData)
        {
            // Only respond to left clicks
            if (eventData.button != PointerEventData.InputButton.Left) return;

            // Don't select if already placed or in gameplay mode
            if (_currentState == RowState.Placed || _currentState == RowState.Gameplay) return;

            Debug.Log($"[WordPatternRow] Row {_rowNumber} background clicked");
            OnRowSelected?.Invoke(_rowNumber);
        }
        #endregion

        #region Public Methods - Initialization
        /// <summary>
        /// Initialize the row with its number and required word length.
        /// </summary>
        /// <param name="rowNumber">Row number (1-based)</param>
        /// <param name="requiredLength">Required word length (3-6)</param>
        public void Initialize(int rowNumber, int requiredLength)
        {
            _rowNumber = rowNumber;
            _requiredWordLength = Mathf.Clamp(requiredLength, 3, 6);
            _revealedLetters = new bool[_requiredWordLength];
            _guessedLetters = new char[_requiredWordLength];

            SetState(RowState.Empty);
        }

        /// <summary>
        /// Sets the required word length for this row.
        /// Resets the row to empty state.
        /// </summary>
        /// <param name="requiredLength">Required word length (3-6)</param>
        public void SetRequiredLength(int requiredLength)
        {
            _requiredWordLength = Mathf.Clamp(requiredLength, 3, 6);
            _revealedLetters = new bool[_requiredWordLength];
            _guessedLetters = new char[_requiredWordLength];
            UpdateDisplay();
        }

        /// <summary>
        /// Sets a validator function to check if entered words are valid.
        /// Validator receives (word, requiredLength) and returns true if valid.
        /// </summary>
        public void SetWordValidator(Func<string, int, bool> validator)
        {
            _wordValidator = validator;
        }
        #endregion

        #region Public Methods - Word Entry (Setup Mode)
        /// <summary>
        /// Adds a letter to the current entry.
        /// Auto-accepts when correct length is reached.
        /// </summary>
        /// <param name="letter">Letter to add</param>
        /// <returns>True if letter was added, false if at max length or placed</returns>
        public bool AddLetter(char letter)
        {
            if (_currentState == RowState.Placed) return false;
            if (_currentState == RowState.WordEntered) return false; // Already have a complete word
            if (_enteredText.Length >= _requiredWordLength) return false;

            _enteredText += char.ToUpper(letter);

            if (_enteredText.Length == _requiredWordLength)
            {
                // Validate word before accepting
                if (_wordValidator != null && !_wordValidator(_enteredText, _requiredWordLength))
                {
                    // Invalid word - stay in entering state, remove last letter
                    string rejectedWord = _enteredText;
                    _enteredText = _enteredText.Substring(0, _enteredText.Length - 1);
                    SetState(RowState.Entering);
                    UpdateDisplay();
                    OnInvalidWordRejected?.Invoke(_rowNumber, rejectedWord);
                    Debug.LogWarning($"[WordPatternRow] Row {_rowNumber}: Invalid word rejected: {rejectedWord}");
                    return false;
                }

                // Auto-accept when word is complete and valid
                _currentWord = _enteredText.ToUpper();
                SetState(RowState.WordEntered);
                OnWordAccepted?.Invoke(_rowNumber, _currentWord);
                Debug.Log($"[WordPatternRow] Row {_rowNumber}: Word auto-accepted: {_currentWord}");
            }
            else
            {
                SetState(RowState.Entering);
            }

            UpdateDisplay();
            OnWordTextChanged?.Invoke(_rowNumber, _enteredText);

            Debug.Log($"[WordPatternRow] Row {_rowNumber}: Added letter '{letter}', now: {_enteredText}");
            return true;
        }

        /// <summary>
        /// Removes the last letter from the current entry.
        /// </summary>
        /// <returns>True if a letter was removed</returns>
        public bool RemoveLastLetter()
        {
            if (_currentState == RowState.Placed) return false;
            if (_enteredText.Length == 0) return false;

            // If we had a complete word, go back to entering state
            bool wasComplete = (_currentState == RowState.WordEntered);

            _enteredText = _enteredText.Substring(0, _enteredText.Length - 1);

            if (wasComplete)
            {
                _currentWord = ""; // Clear the accepted word
            }

            if (_enteredText.Length == 0)
            {
                SetState(RowState.Empty);
            }
            else
            {
                SetState(RowState.Entering);
            }

            UpdateDisplay();
            OnWordTextChanged?.Invoke(_rowNumber, _enteredText);

            return true;
        }

        /// <summary>
        /// Sets the entered text directly (e.g., from autocomplete selection).
        /// Auto-accepts if correct length. Validates against word bank if validator is set.
        /// </summary>
        /// <param name="word">The word to set</param>
        public void SetEnteredText(string word)
        {
            if (_currentState == RowState.Placed) return;

            _enteredText = word?.ToUpper() ?? "";

            if (_enteredText.Length > _requiredWordLength)
            {
                _enteredText = _enteredText.Substring(0, _requiredWordLength);
            }

            if (_enteredText.Length == 0)
            {
                _currentWord = "";
                SetState(RowState.Empty);
            }
            else if (_enteredText.Length == _requiredWordLength)
            {
                // Validate word before accepting (autocomplete words should already be valid,
                // but this adds an extra safety check)
                if (_wordValidator != null && !_wordValidator(_enteredText, _requiredWordLength))
                {
                    // Invalid word - clear and go to empty state
                    string rejectedWord = _enteredText;
                    _enteredText = "";
                    _currentWord = "";
                    SetState(RowState.Empty);
                    OnInvalidWordRejected?.Invoke(_rowNumber, rejectedWord);
                    Debug.LogWarning($"[WordPatternRow] Row {_rowNumber}: Invalid word rejected from autocomplete: {rejectedWord}");
                    UpdateDisplay();
                    return;
                }

                // Auto-accept when word is complete and valid
                _currentWord = _enteredText.ToUpper();
                SetState(RowState.WordEntered);
                OnWordAccepted?.Invoke(_rowNumber, _currentWord);
                Debug.Log($"[WordPatternRow] Row {_rowNumber}: Word auto-accepted from autocomplete: {_currentWord}");
            }
            else
            {
                _currentWord = "";
                SetState(RowState.Entering);
            }

            UpdateDisplay();
            OnWordTextChanged?.Invoke(_rowNumber, _enteredText);
        }

        /// <summary>
        /// Manually accepts the current entry as the word for this row.
        /// Usually not needed since words auto-accept at correct length.
        /// </summary>
        /// <returns>True if word was accepted (correct length)</returns>
        public bool AcceptWord()
        {
            if (_enteredText.Length != _requiredWordLength) return false;

            _currentWord = _enteredText.ToUpper();
            SetState(RowState.WordEntered);
            UpdateDisplay();

            return true;
        }

        /// <summary>
        /// Clears the word entry and resets to empty state.
        /// </summary>
        public void ClearWord()
        {
            _currentWord = "";
            _enteredText = "";
            ResetRevealedLetters();
            ClearGuessedLetters();
            SetState(RowState.Empty);
            UpdateDisplay();
        }

        /// <summary>
        /// Marks the word as placed on the grid.
        /// </summary>
        public void MarkAsPlaced()
        {
            if (!HasWord) return;

            SetState(RowState.Placed);
            UpdateDisplay();
            UpdateButtonStates();
        }

        /// <summary>
        /// Sets the placement position data for this word.
        /// Called by PlayerGridPanel when word is placed on grid.
        /// </summary>
        public void SetPlacementPosition(int startCol, int startRow, int dirCol, int dirRow)
        {
            _placedStartCol = startCol;
            _placedStartRow = startRow;
            _placedDirCol = dirCol;
            _placedDirRow = dirRow;
        }

        /// <summary>
        /// Resets the row to empty state, clearing all word data.
        /// Used when player wants to re-enter a word.
        /// </summary>
        public void ResetToEmpty()
        {
            _currentWord = "";
            _enteredText = "";
            ResetRevealedLetters();
            ClearGuessedLetters();
            SetState(RowState.Empty);
            UpdateDisplay();
            Debug.Log($"[WordPatternRow] Row {_rowNumber} reset to empty");
        }

        /// <summary>
        /// Resets the row to WordEntered state, keeping the word but allowing re-placement.
        /// Used when grid is cleared but words should be retained.
        /// </summary>
        public void ResetToWordEntered()
        {
            if (string.IsNullOrEmpty(_currentWord))
            {
                ResetToEmpty();
                return;
            }

            SetState(RowState.WordEntered);
            UpdateDisplay();
            Debug.Log($"[WordPatternRow] Row {_rowNumber} reset to WordEntered (word: {_currentWord})");
        }

        /// <summary>
        /// Selects this row for input.
        /// </summary>
        public void Select()
        {
            _isSelected = true;
            UpdateDisplay();
            Debug.Log($"[WordPatternRow] Row {_rowNumber} selected");
        }

        /// <summary>
        /// Deselects this row.
        /// </summary>
        public void Deselect()
        {
            _isSelected = false;
            UpdateDisplay();
        }
        #endregion

        #region Public Methods - Letter Reveal (Gameplay Mode)
        /// <summary>
        /// Sets the word for gameplay mode (opponent's hidden word).
        /// For owner panel, call RevealAllLetters() after this to show full word.
        /// </summary>
        /// <param name="word">The hidden word</param>
        public void SetGameplayWord(string word)
        {
            _currentWord = word?.ToUpper() ?? "";
            _requiredWordLength = _currentWord.Length;
            _revealedLetters = new bool[_requiredWordLength];
            _guessedLetters = new char[_requiredWordLength];
            _isSelected = false;
            _inWordGuessMode = false;

            // Set state directly
            _currentState = RowState.Gameplay;

            // EXPLICITLY hide ALL setup buttons - don't rely on UpdateButtonStates timing
            if (_selectButton != null)
            {
                _selectButton.gameObject.SetActive(false);
                Debug.Log($"[WordPatternRow] Row {_rowNumber}: Hid select button");
            }
            if (_coordinateModeButton != null)
            {
                _coordinateModeButton.gameObject.SetActive(false);
            }
            if (_deleteButton != null)
            {
                _deleteButton.gameObject.SetActive(false);
            }

            UpdateBackgroundColor();
            UpdateDisplay();
            UpdateGuessButtonStates();

            Debug.Log($"[WordPatternRow] Row {_rowNumber} set to gameplay with word: '{_currentWord}'");
        }

        /// <summary>
        /// Reveals a specific letter position.
        /// </summary>
        /// <param name="index">Index of the letter to reveal (0-based)</param>
        public void RevealLetter(int index)
        {
            if (index < 0 || index >= _revealedLetters.Length) return;

            _revealedLetters[index] = true;
            UpdateDisplay();
        }

        /// <summary>
        /// Reveals all instances of a letter in the word.
        /// </summary>
        /// <param name="letter">The letter to reveal</param>
        /// <returns>Number of letters revealed</returns>
        public int RevealAllInstancesOfLetter(char letter)
        {
            int count = 0;
            char upperLetter = char.ToUpper(letter);

            for (int i = 0; i < _currentWord.Length; i++)
            {
                if (_currentWord[i] == upperLetter && !_revealedLetters[i])
                {
                    _revealedLetters[i] = true;
                    count++;
                }
            }

            if (count > 0)
            {
                UpdateDisplay();
            }

            return count;
        }

        /// <summary>
        /// Reveals the entire word.
        /// </summary>
        public void RevealAllLetters()
        {
            for (int i = 0; i < _revealedLetters.Length; i++)
            {
                _revealedLetters[i] = true;
            }
            UpdateDisplay();
        }

        /// <summary>
        /// Checks if all letters have been revealed.
        /// </summary>
        public bool IsFullyRevealed()
        {
            foreach (bool revealed in _revealedLetters)
            {
                if (!revealed) return false;
            }
            return true;
        }

        /// <summary>
        /// Resets all revealed letters to hidden.
        /// </summary>
        public void ResetRevealedLetters()
        {
            for (int i = 0; i < _revealedLetters.Length; i++)
            {
                _revealedLetters[i] = false;
            }
            UpdateDisplay();
        }
        #endregion

        #region Public Methods - Word Guess Mode
        /// <summary>
        /// Enters word guess mode, allowing player to type letters into unrevealed positions.
        /// </summary>
        public void EnterWordGuessMode()
        {
            if (_currentState != RowState.Gameplay) return;
            if (_inWordGuessMode) return;

            _inWordGuessMode = true;
            ClearGuessedLetters();

            // Find first unrevealed position for cursor
            _guessCursorPosition = FindNextUnrevealedPosition(-1);

            UpdateDisplay();
            UpdateGuessButtonStates();

            Debug.Log($"[WordPatternRow] Row {_rowNumber} entered word guess mode, cursor at {_guessCursorPosition}");
            OnWordGuessStarted?.Invoke(_rowNumber);
        }

        /// <summary>
        /// Exits word guess mode.
        /// </summary>
        /// <param name="submit">If true, submits the guess before exiting</param>
        public void ExitWordGuessMode(bool submit)
        {
            if (!_inWordGuessMode) return;

            if (submit)
            {
                string guessedWord = GetFullGuessWord();
                Debug.Log($"[WordPatternRow] Row {_rowNumber} submitting guess: {guessedWord}");
                OnWordGuessSubmitted?.Invoke(_rowNumber, guessedWord);
            }
            else
            {
                Debug.Log($"[WordPatternRow] Row {_rowNumber} cancelled word guess");
                OnWordGuessCancelled?.Invoke(_rowNumber);
            }

            _inWordGuessMode = false;
            ClearGuessedLetters();
            _guessCursorPosition = 0;

            UpdateDisplay();
            UpdateGuessButtonStates();
        }

        /// <summary>
        /// Types a letter into the current cursor position (guess mode only).
        /// Auto-advances to next unrevealed position.
        /// </summary>
        /// <param name="letter">The letter to type</param>
        /// <returns>True if letter was typed</returns>
        public bool TypeGuessLetter(char letter)
        {
            if (!_inWordGuessMode) return false;
            if (_guessCursorPosition < 0 || _guessCursorPosition >= _guessedLetters.Length) return false;

            // Can only type into unrevealed positions
            if (_revealedLetters[_guessCursorPosition])
            {
                // Move to next unrevealed position
                _guessCursorPosition = FindNextUnrevealedPosition(_guessCursorPosition);
                if (_guessCursorPosition < 0) return false;
            }

            _guessedLetters[_guessCursorPosition] = char.ToUpper(letter);
            Debug.Log($"[WordPatternRow] Row {_rowNumber} typed '{letter}' at position {_guessCursorPosition}");

            // Auto-advance to next unrevealed position
            _guessCursorPosition = FindNextUnrevealedPosition(_guessCursorPosition);

            UpdateDisplay();
            return true;
        }

        /// <summary>
        /// Handles backspace in guess mode.
        /// First click: clears letter at current position (stays there)
        /// Second click (if position empty): moves back and clears that letter
        /// </summary>
        /// <returns>True if backspace was handled</returns>
        public bool BackspaceGuessLetter()
        {
            if (!_inWordGuessMode) return false;

            // If cursor is past end, move back to last unrevealed position
            if (_guessCursorPosition < 0)
            {
                _guessCursorPosition = FindPreviousUnrevealedPosition(_guessedLetters.Length);
                if (_guessCursorPosition < 0) return false;
            }

            // Check if current position has a typed letter
            if (_guessCursorPosition >= 0 && _guessCursorPosition < _guessedLetters.Length)
            {
                if (_guessedLetters[_guessCursorPosition] != '\0')
                {
                    // Clear letter at current position, stay there
                    _guessedLetters[_guessCursorPosition] = '\0';
                    Debug.Log($"[WordPatternRow] Row {_rowNumber} cleared letter at position {_guessCursorPosition}");
                    UpdateDisplay();
                    return true;
                }
                else
                {
                    // Current position empty, move back to previous unrevealed position and clear
                    int prevPos = FindPreviousUnrevealedPosition(_guessCursorPosition);
                    if (prevPos >= 0 && _guessedLetters[prevPos] != '\0')
                    {
                        _guessedLetters[prevPos] = '\0';
                        _guessCursorPosition = prevPos;
                        Debug.Log($"[WordPatternRow] Row {_rowNumber} moved back and cleared at position {prevPos}");
                        UpdateDisplay();
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Gets the full guessed word (revealed letters + typed letters).
        /// </summary>
        /// <returns>The complete guessed word</returns>
        public string GetFullGuessWord()
        {
            char[] result = new char[_currentWord.Length];

            for (int i = 0; i < _currentWord.Length; i++)
            {
                if (_revealedLetters[i])
                {
                    result[i] = _currentWord[i];
                }
                else if (_guessedLetters[i] != '\0')
                {
                    result[i] = _guessedLetters[i];
                }
                else
                {
                    result[i] = '_'; // Still unknown
                }
            }

            return new string(result);
        }

        /// <summary>
        /// Checks if all unrevealed positions have been filled with guessed letters.
        /// </summary>
        /// <returns>True if guess is complete</returns>
        public bool IsGuessComplete()
        {
            for (int i = 0; i < _currentWord.Length; i++)
            {
                if (!_revealedLetters[i] && _guessedLetters[i] == '\0')
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Hide the "Guess Word" button (used when another row enters guess mode)
        /// </summary>
        public void HideGuessWordButton()
        {
            if (_guessWordButton != null)
            {
                _guessWordButton.gameObject.SetActive(false);
            }
        }

        /// <summary>
        /// Show the "Guess Word" button (used when exiting guess mode)
        /// Only shows if in Gameplay state, not currently in guess mode, and not solved
        /// </summary>
        public void ShowGuessWordButton()
        {
            // Never show if word has been solved
            if (_wordSolved)
            {
                Debug.Log($"[WordPatternRow] Row {_rowNumber} ShowGuessWordButton blocked - word is SOLVED");
                return;
            }

            if (_guessWordButton != null && _currentState == RowState.Gameplay && !_inWordGuessMode)
            {
                _guessWordButton.gameObject.SetActive(true);
            }
        }

        /// <summary>
        /// Mark this row as belonging to the owner panel (never shows guess buttons)
        /// </summary>
        public void SetAsOwnerPanel()
        {
            _isOwnerPanel = true;
            HideAllGuessButtons();
        }

        /// <summary>
        /// Permanently hide all guess-related buttons (for owner panel)
        /// </summary>
        /// <summary>
        /// Mark this word as solved (correctly guessed) - permanently hides guess button
        /// </summary>
        public void MarkWordSolved()
        {
            _wordSolved = true;
            HideGuessWordButton();
            Debug.Log($"[WordPatternRow] Row {_rowNumber} marked as SOLVED - button permanently hidden");
        }

        
public void HideAllGuessButtons()
        {
            if (_guessWordButton != null) _guessWordButton.gameObject.SetActive(false);
            if (_guessBackspaceButton != null) _guessBackspaceButton.gameObject.SetActive(false);
            if (_guessAcceptButton != null) _guessAcceptButton.gameObject.SetActive(false);
            if (_guessCancelButton != null) _guessCancelButton.gameObject.SetActive(false);
        }
        #endregion

        #region Private Methods - Word Guess Helpers
        private void ClearGuessedLetters()
        {
            if (_guessedLetters == null) return;
            for (int i = 0; i < _guessedLetters.Length; i++)
            {
                _guessedLetters[i] = '\0';
            }
        }

        private int FindNextUnrevealedPosition(int fromPosition)
        {
            for (int i = fromPosition + 1; i < _revealedLetters.Length; i++)
            {
                if (!_revealedLetters[i])
                {
                    return i;
                }
            }
            return -1; // No more unrevealed positions
        }

        private int FindPreviousUnrevealedPosition(int fromPosition)
        {
            for (int i = fromPosition - 1; i >= 0; i--)
            {
                if (!_revealedLetters[i])
                {
                    return i;
                }
            }
            return -1; // No previous unrevealed positions
        }

        private void UpdateGuessButtonStates()
        {
            // Owner panels NEVER show guess buttons
            if (_isOwnerPanel)
            {
                if (_guessWordButton != null) _guessWordButton.gameObject.SetActive(false);
                if (_guessBackspaceButton != null) _guessBackspaceButton.gameObject.SetActive(false);
                if (_guessAcceptButton != null) _guessAcceptButton.gameObject.SetActive(false);
                if (_guessCancelButton != null) _guessCancelButton.gameObject.SetActive(false);
                return;
            }

            if (_currentState != RowState.Gameplay)
            {
                // Hide all guess buttons when not in gameplay
                if (_guessWordButton != null) _guessWordButton.gameObject.SetActive(false);
                if (_guessBackspaceButton != null) _guessBackspaceButton.gameObject.SetActive(false);
                if (_guessAcceptButton != null) _guessAcceptButton.gameObject.SetActive(false);
                if (_guessCancelButton != null) _guessCancelButton.gameObject.SetActive(false);
                return;
            }

            if (_inWordGuessMode)
            {
                // In guess mode: show backspace, accept, cancel; hide word button
                if (_guessWordButton != null) _guessWordButton.gameObject.SetActive(false);
                if (_guessBackspaceButton != null) _guessBackspaceButton.gameObject.SetActive(true);
                if (_guessAcceptButton != null) _guessAcceptButton.gameObject.SetActive(true);
                if (_guessCancelButton != null) _guessCancelButton.gameObject.SetActive(true);
            }
            else
            {
                // Not in guess mode: show word button (unless solved), hide others
                if (_guessWordButton != null && !_wordSolved) _guessWordButton.gameObject.SetActive(true);
                if (_guessBackspaceButton != null) _guessBackspaceButton.gameObject.SetActive(false);
                if (_guessAcceptButton != null) _guessAcceptButton.gameObject.SetActive(false);
                if (_guessCancelButton != null) _guessCancelButton.gameObject.SetActive(false);
            }
        }
        #endregion

        #region Private Methods - State Management
        private void SetState(RowState newState)
        {
            _currentState = newState;
            UpdateButtonStates();
            UpdateBackgroundColor();
        }

        private void UpdateButtonStates()
        {
            // Select button: visible unless placed or in gameplay
            // Allows player to select any row at any time during entry
            if (_selectButton != null)
            {
                bool showSelect = _currentState != RowState.Gameplay && _currentState != RowState.Placed;
                _selectButton.gameObject.SetActive(showSelect);
                _selectButton.interactable = showSelect;
            }

            // Coordinate mode button: only VISIBLE when word is entered but not yet placed
            // Hides completely when empty, entering, placed, or gameplay
            if (_coordinateModeButton != null)
            {
                bool showCoordinate = _currentState == RowState.WordEntered;
                _coordinateModeButton.gameObject.SetActive(showCoordinate);
                _coordinateModeButton.interactable = showCoordinate;
            }

            // Delete button: visible when there's any content (including placed words)
            // Hidden only when empty or in gameplay
            if (_deleteButton != null)
            {
                bool showDelete = _currentState != RowState.Empty && _currentState != RowState.Gameplay;
                _deleteButton.gameObject.SetActive(showDelete);
                _deleteButton.interactable = showDelete;
            }

            // Update guess button states
            UpdateGuessButtonStates();
        }

        private void UpdateBackgroundColor()
        {
            if (_backgroundImage == null) return;

            Color bgColor;

            if (_isSelected)
            {
                bgColor = _selectedColor;
            }
            else
            {
                switch (_currentState)
                {
                    case RowState.Entering:
                        bgColor = _enteringColor;
                        break;
                    case RowState.WordEntered:
                        bgColor = _wordEnteredColor;
                        break;
                    case RowState.Placed:
                        bgColor = _placedColor;
                        break;
                    case RowState.Empty:
                    case RowState.Gameplay:
                    default:
                        bgColor = _emptyColor;
                        break;
                }
            }

            _backgroundImage.color = bgColor;
        }
        #endregion

        #region Private Methods - Display
        private void UpdateDisplay()
        {
            if (_combinedText == null) return;

            string displayText = BuildDisplayText();
            _combinedText.text = displayText;

            UpdateBackgroundColor();
        }

        private string BuildDisplayText()
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();

            // Add row number prefix
            sb.Append(_rowNumber);
            sb.Append(_numberSeparator);

            // Use monospace for consistent letter width (0.6em works well for most fonts)
            sb.Append("<mspace=0.6em>");

            switch (_currentState)
            {
                case RowState.Empty:
                    // Show all underscores (not underlined - these are placeholders)
                    for (int i = 0; i < _requiredWordLength; i++)
                    {
                        if (i > 0) sb.Append(_letterSeparator);
                        sb.Append(_unknownLetterChar);
                    }
                    break;

                case RowState.Entering:
                    // Show entered letters (underlined) + remaining underscores
                    for (int i = 0; i < _requiredWordLength; i++)
                    {
                        if (i > 0) sb.Append(_letterSeparator);
                        if (i < _enteredText.Length)
                        {
                            // Underline entered letters
                            sb.Append("<u>");
                            sb.Append(_enteredText[i]);
                            sb.Append("</u>");
                        }
                        else
                        {
                            sb.Append(_unknownLetterChar);
                        }
                    }
                    break;

                case RowState.WordEntered:
                case RowState.Placed:
                    // Show the full word with underlined letters
                    for (int i = 0; i < _currentWord.Length; i++)
                    {
                        if (i > 0) sb.Append(_letterSeparator);
                        sb.Append("<u>");
                        sb.Append(_currentWord[i]);
                        sb.Append("</u>");
                    }
                    break;

                case RowState.Gameplay:
                    // Build gameplay display (handles both normal and guess mode)
                    BuildGameplayDisplayText(sb);
                    break;
            }

            sb.Append("</mspace>");

            return sb.ToString();
        }

        private void BuildGameplayDisplayText(System.Text.StringBuilder sb)
        {
            // Convert color to hex for TMP rich text
            string typedColorHex = ColorUtility.ToHtmlStringRGB(_guessTypedLetterColor);

            for (int i = 0; i < _currentWord.Length; i++)
            {
                if (i > 0) sb.Append(_letterSeparator);

                if (_revealedLetters[i])
                {
                    // Revealed letters are underlined (discovered through gameplay)
                    sb.Append("<u>");
                    sb.Append(_currentWord[i]);
                    sb.Append("</u>");
                }
                else if (_inWordGuessMode && _guessedLetters[i] != '\0')
                {
                    // Player-typed letters in guess mode: colored, NO underline
                    sb.Append("<color=#");
                    sb.Append(typedColorHex);
                    sb.Append(">");
                    sb.Append(_guessedLetters[i]);
                    sb.Append("</color>");
                }
                else
                {
                    // Hidden letters show underscore (not underlined)
                    sb.Append(_unknownLetterChar);
                }
            }
        }
        #endregion

        #region Private Methods - Button Events
        private void SubscribeToButtons()
        {
            if (_selectButton != null)
            {
                _selectButton.onClick.AddListener(HandleSelectClick);
            }

            if (_coordinateModeButton != null)
            {
                _coordinateModeButton.onClick.AddListener(HandleCoordinateModeClick);
            }

            if (_deleteButton != null)
            {
                _deleteButton.onClick.AddListener(HandleDeleteClick);
            }

            // Gameplay guess buttons
            if (_guessWordButton != null)
            {
                _guessWordButton.onClick.AddListener(HandleGuessWordClick);
            }

            if (_guessBackspaceButton != null)
            {
                _guessBackspaceButton.onClick.AddListener(HandleGuessBackspaceClick);
            }

            if (_guessAcceptButton != null)
            {
                _guessAcceptButton.onClick.AddListener(HandleGuessAcceptClick);
            }

            if (_guessCancelButton != null)
            {
                _guessCancelButton.onClick.AddListener(HandleGuessCancelClick);
            }
        }

        private void UnsubscribeFromButtons()
        {
            if (_selectButton != null)
            {
                _selectButton.onClick.RemoveListener(HandleSelectClick);
            }

            if (_coordinateModeButton != null)
            {
                _coordinateModeButton.onClick.RemoveListener(HandleCoordinateModeClick);
            }

            if (_deleteButton != null)
            {
                _deleteButton.onClick.RemoveListener(HandleDeleteClick);
            }

            // Gameplay guess buttons
            if (_guessWordButton != null)
            {
                _guessWordButton.onClick.RemoveListener(HandleGuessWordClick);
            }

            if (_guessBackspaceButton != null)
            {
                _guessBackspaceButton.onClick.RemoveListener(HandleGuessBackspaceClick);
            }

            if (_guessAcceptButton != null)
            {
                _guessAcceptButton.onClick.RemoveListener(HandleGuessAcceptClick);
            }

            if (_guessCancelButton != null)
            {
                _guessCancelButton.onClick.RemoveListener(HandleGuessCancelClick);
            }
        }

        private void HandleSelectClick()
        {
            // Select button just selects this row - doesn't accept anything
            Debug.Log($"[WordPatternRow] Row {_rowNumber} select button clicked");
            OnRowSelected?.Invoke(_rowNumber);
        }

        private void HandleCoordinateModeClick()
        {
            OnCoordinateModeClicked?.Invoke(_rowNumber);
        }

        private void HandleDeleteClick()
        {
            // Track if the word was placed (needs grid clearing)
            bool wasPlaced = _currentState == RowState.Placed;

            // Clear the row
            ClearWord();

            // Fire event with wasPlaced flag so listeners know to clear the grid too
            OnDeleteClicked?.Invoke(_rowNumber, wasPlaced);

            Debug.Log($"[WordPatternRow] Row {_rowNumber} deleted (wasPlaced: {wasPlaced})");
        }

        private void HandleGuessWordClick()
        {
            Debug.Log($"[WordPatternRow] Row {_rowNumber} guess word button clicked");
            EnterWordGuessMode();
        }

        private void HandleGuessBackspaceClick()
        {
            Debug.Log($"[WordPatternRow] Row {_rowNumber} backspace button clicked");
            BackspaceGuessLetter();
        }

        private void HandleGuessAcceptClick()
        {
            Debug.Log($"[WordPatternRow] Row {_rowNumber} accept button clicked");
            ExitWordGuessMode(true); // Submit the guess
        }

        private void HandleGuessCancelClick()
        {
            Debug.Log($"[WordPatternRow] Row {_rowNumber} cancel button clicked");
            ExitWordGuessMode(false); // Cancel without submitting
        }
        #endregion

        #region Editor Helpers
#if UNITY_EDITOR
        [TitleGroup("Debug")]
        [Button("Test Add Letters")]
        private void TestAddLetters()
        {
            ClearWord();
            AddLetter('T');
            AddLetter('E');
            AddLetter('S');
            if (_requiredWordLength >= 4) AddLetter('T');
            if (_requiredWordLength >= 5) AddLetter('S');
        }

        [Button("Test Accept Word")]
        private void TestAcceptWord()
        {
            AcceptWord();
        }

        [Button("Test Mark Placed")]
        private void TestMarkPlaced()
        {
            MarkAsPlaced();
        }

        [Button("Test Gameplay Mode")]
        private void TestGameplayMode()
        {
            SetGameplayWord("BEAST");
            RevealLetter(0);
            RevealLetter(2);
        }

        [Button("Test Enter Guess Mode")]
        private void TestEnterGuessMode()
        {
            if (_currentState == RowState.Gameplay)
            {
                EnterWordGuessMode();
            }
            else
            {
                Debug.LogWarning("Must be in Gameplay mode first. Use 'Test Gameplay Mode' button.");
            }
        }

        [Button("Clear")]
        private void TestClear()
        {
            ClearWord();
        }

        [Button("Refresh Display")]
        private void RefreshDisplay()
        {
            UpdateDisplay();
        }
#endif
        #endregion
    }
}
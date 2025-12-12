using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using Sirenix.OdinInspector;
using System;
using TecVooDoo.DontLoseYourHead.UI.Controllers;

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
        private Color _guessTypedLetterColor = new Color(0.85f, 0.65f, 0f, 1f);

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

        // Word Guess Mode
        private WordGuessInputController _wordGuessController;
        private bool _isOwnerPanel = false;
        private bool _wordSolved = false;

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
        public bool InWordGuessMode => _wordGuessController != null && _wordGuessController.IsActive;

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
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            InitializeArraysIfNeeded();
            InitializeWordGuessController();

            if (_currentState == RowState.Gameplay && !string.IsNullOrEmpty(_currentWord))
            {
                UpdateDisplay();
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

        private void OnDestroy()
        {
            UnsubscribeFromControllerEvents();
        }
        #endregion

        #region IPointerClickHandler
        /// <summary>
        /// Handles clicks on the row background to select it.
        /// </summary>
        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left) return;
            if (_currentState == RowState.Placed || _currentState == RowState.Gameplay) return;

            OnRowSelected?.Invoke(_rowNumber);
        }
        #endregion

        #region Public Methods - Initialization
        /// <summary>
        /// Initialize the row with its number and required word length.
        /// </summary>
        public void Initialize(int rowNumber, int requiredLength)
        {
            _rowNumber = rowNumber;
            _requiredWordLength = Mathf.Clamp(requiredLength, 3, 6);
            _revealedLetters = new bool[_requiredWordLength];

            InitializeWordGuessController();
            _wordGuessController.SetWordLength(_requiredWordLength);

            SetState(RowState.Empty);
        }

        /// <summary>
        /// Sets the required word length for this row.
        /// Resets the row to empty state.
        /// </summary>
        public void SetRequiredLength(int requiredLength)
        {
            _requiredWordLength = Mathf.Clamp(requiredLength, 3, 6);
            _revealedLetters = new bool[_requiredWordLength];

            if (_wordGuessController != null)
            {
                _wordGuessController.SetWordLength(_requiredWordLength);
            }

            UpdateDisplay();
        }

        /// <summary>
        /// Sets a validator function to check if entered words are valid.
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
        public bool AddLetter(char letter)
        {
            if (_currentState == RowState.Placed) return false;
            if (_currentState == RowState.WordEntered) return false;
            if (_enteredText.Length >= _requiredWordLength) return false;

            _enteredText += char.ToUpper(letter);

            if (_enteredText.Length == _requiredWordLength)
            {
                if (_wordValidator != null && !_wordValidator(_enteredText, _requiredWordLength))
                {
                    string rejectedWord = _enteredText;
                    _enteredText = _enteredText.Substring(0, _enteredText.Length - 1);
                    SetState(RowState.Entering);
                    UpdateDisplay();
                    OnInvalidWordRejected?.Invoke(_rowNumber, rejectedWord);
                    Debug.LogWarning(string.Format("[WordPatternRow] Row {0}: Invalid word rejected: {1}", _rowNumber, rejectedWord));
                    return false;
                }

                _currentWord = _enteredText.ToUpper();
                SetState(RowState.WordEntered);
                OnWordAccepted?.Invoke(_rowNumber, _currentWord);
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
        /// Removes the last letter from the current entry.
        /// </summary>
        public bool RemoveLastLetter()
        {
            if (_currentState == RowState.Placed) return false;
            if (_enteredText.Length == 0) return false;

            bool wasComplete = (_currentState == RowState.WordEntered);
            _enteredText = _enteredText.Substring(0, _enteredText.Length - 1);

            if (wasComplete)
            {
                _currentWord = "";
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
        /// </summary>
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
                if (_wordValidator != null && !_wordValidator(_enteredText, _requiredWordLength))
                {
                    string rejectedWord = _enteredText;
                    _enteredText = "";
                    _currentWord = "";
                    SetState(RowState.Empty);
                    OnInvalidWordRejected?.Invoke(_rowNumber, rejectedWord);
                    Debug.LogWarning(string.Format("[WordPatternRow] Row {0}: Invalid word rejected from autocomplete: {1}", _rowNumber, rejectedWord));
                    UpdateDisplay();
                    return;
                }

                _currentWord = _enteredText.ToUpper();
                SetState(RowState.WordEntered);
                OnWordAccepted?.Invoke(_rowNumber, _currentWord);
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
        /// </summary>
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
            if (_wordGuessController != null)
            {
                _wordGuessController.ClearGuessedLetters();
            }
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
        /// </summary>
        public void ResetToEmpty()
        {
            _currentWord = "";
            _enteredText = "";
            ResetRevealedLetters();
            if (_wordGuessController != null)
            {
                _wordGuessController.ClearGuessedLetters();
            }
            SetState(RowState.Empty);
            UpdateDisplay();
        }

        /// <summary>
        /// Resets the row to WordEntered state, keeping the word but allowing re-placement.
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
        }

        /// <summary>
        /// Selects this row for input.
        /// </summary>
        public void Select()
        {
            _isSelected = true;
            UpdateDisplay();
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
        /// </summary>
        public void SetGameplayWord(string word)
        {
            _currentWord = word?.ToUpper() ?? "";
            _requiredWordLength = _currentWord.Length;
            _revealedLetters = new bool[_requiredWordLength];
            _isSelected = false;

            InitializeWordGuessController();
            _wordGuessController.SetWordLength(_requiredWordLength);

            _currentState = RowState.Gameplay;

            HideSetupButtons();
            UpdateBackgroundColor();
            UpdateDisplay();
            UpdateGuessButtonStates();
        }

        /// <summary>
        /// Reveals a specific letter position.
        /// </summary>
        public void RevealLetter(int index)
        {
            if (index < 0 || index >= _revealedLetters.Length) return;

            _revealedLetters[index] = true;
            UpdateDisplay();
        }

        /// <summary>
        /// Reveals all instances of a letter in the word.
        /// </summary>
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

        #region Public Methods - Word Guess Mode (Delegated to Controller)
        /// <summary>
        /// Enters word guess mode, allowing player to type letters into unrevealed positions.
        /// </summary>
        public void EnterWordGuessMode()
        {
            if (_currentState != RowState.Gameplay) return;
            if (_wordGuessController == null) return;

            if (_wordGuessController.Enter())
            {
                UpdateGuessButtonStates();
            }
        }

        /// <summary>
        /// Exits word guess mode.
        /// </summary>
        public void ExitWordGuessMode(bool submit)
        {
            if (_wordGuessController == null) return;

            _wordGuessController.Exit(submit);
            UpdateGuessButtonStates();
        }

        /// <summary>
        /// Types a letter into the current cursor position (guess mode only).
        /// </summary>
        public bool TypeGuessLetter(char letter)
        {
            if (_wordGuessController == null) return false;
            return _wordGuessController.TypeLetter(letter);
        }

        /// <summary>
        /// Handles backspace in guess mode.
        /// </summary>
        public bool BackspaceGuessLetter()
        {
            if (_wordGuessController == null) return false;
            return _wordGuessController.Backspace();
        }

        /// <summary>
        /// Gets the full guessed word (revealed letters + typed letters).
        /// </summary>
        public string GetFullGuessWord()
        {
            if (_wordGuessController == null) return string.Empty;
            return _wordGuessController.GetFullGuessWord();
        }

        /// <summary>
        /// Checks if all unrevealed positions have been filled with guessed letters.
        /// </summary>
        public bool IsGuessComplete()
        {
            if (_wordGuessController == null) return false;
            return _wordGuessController.IsGuessComplete();
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
        /// </summary>
        public void ShowGuessWordButton()
        {
            if (_wordSolved) return;

            if (_guessWordButton != null && _currentState == RowState.Gameplay && !InWordGuessMode)
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
        /// Mark this word as solved (correctly guessed) - permanently hides guess button
        /// </summary>
        public void MarkWordSolved()
        {
            _wordSolved = true;
            HideGuessWordButton();
        }

        /// <summary>
        /// Permanently hide all guess-related buttons (for owner panel)
        /// </summary>
        public void HideAllGuessButtons()
        {
            if (_guessWordButton != null) _guessWordButton.gameObject.SetActive(false);
            if (_guessBackspaceButton != null) _guessBackspaceButton.gameObject.SetActive(false);
            if (_guessAcceptButton != null) _guessAcceptButton.gameObject.SetActive(false);
            if (_guessCancelButton != null) _guessCancelButton.gameObject.SetActive(false);
        }
        #endregion

        #region Private Methods - Controller Management
        private void InitializeArraysIfNeeded()
        {
            if (_revealedLetters == null || _revealedLetters.Length == 0)
            {
                _revealedLetters = new bool[_requiredWordLength];
            }
        }

        private void InitializeWordGuessController()
        {
            if (_wordGuessController != null) return;

            _wordGuessController = new WordGuessInputController(
                IsLetterRevealedAt,
                GetRevealedLetterAt,
                () => _currentWord
            );
            _wordGuessController.Initialize(_requiredWordLength);

            SubscribeToControllerEvents();
        }

        private void SubscribeToControllerEvents()
        {
            if (_wordGuessController == null) return;

            _wordGuessController.OnGuessStarted += HandleControllerGuessStarted;
            _wordGuessController.OnGuessSubmitted += HandleControllerGuessSubmitted;
            _wordGuessController.OnGuessCancelled += HandleControllerGuessCancelled;
            _wordGuessController.OnDisplayUpdateNeeded += HandleControllerDisplayUpdate;
        }

        private void UnsubscribeFromControllerEvents()
        {
            if (_wordGuessController == null) return;

            _wordGuessController.OnGuessStarted -= HandleControllerGuessStarted;
            _wordGuessController.OnGuessSubmitted -= HandleControllerGuessSubmitted;
            _wordGuessController.OnGuessCancelled -= HandleControllerGuessCancelled;
            _wordGuessController.OnDisplayUpdateNeeded -= HandleControllerDisplayUpdate;
        }

        private void HandleControllerGuessStarted()
        {
            OnWordGuessStarted?.Invoke(_rowNumber);
        }

        private void HandleControllerGuessSubmitted(string guessedWord)
        {
            OnWordGuessSubmitted?.Invoke(_rowNumber, guessedWord);
        }

        private void HandleControllerGuessCancelled()
        {
            OnWordGuessCancelled?.Invoke(_rowNumber);
        }

        private void HandleControllerDisplayUpdate()
        {
            UpdateDisplay();
        }

        private bool IsLetterRevealedAt(int index)
        {
            if (_revealedLetters == null || index < 0 || index >= _revealedLetters.Length)
            {
                return false;
            }
            return _revealedLetters[index];
        }

        private char GetRevealedLetterAt(int index)
        {
            if (string.IsNullOrEmpty(_currentWord) || index < 0 || index >= _currentWord.Length)
            {
                return '\0';
            }
            return _currentWord[index];
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
            if (_selectButton != null)
            {
                bool showSelect = _currentState != RowState.Gameplay && _currentState != RowState.Placed;
                _selectButton.gameObject.SetActive(showSelect);
                _selectButton.interactable = showSelect;
            }

            if (_coordinateModeButton != null)
            {
                bool showCoordinate = _currentState == RowState.WordEntered;
                _coordinateModeButton.gameObject.SetActive(showCoordinate);
                _coordinateModeButton.interactable = showCoordinate;
            }

            if (_deleteButton != null)
            {
                bool showDelete = _currentState != RowState.Empty && _currentState != RowState.Gameplay;
                _deleteButton.gameObject.SetActive(showDelete);
                _deleteButton.interactable = showDelete;
            }

            UpdateGuessButtonStates();
        }

        private void UpdateGuessButtonStates()
        {
            if (_isOwnerPanel)
            {
                HideAllGuessButtons();
                return;
            }

            if (_currentState != RowState.Gameplay)
            {
                HideAllGuessButtons();
                return;
            }

            if (InWordGuessMode)
            {
                if (_guessWordButton != null) _guessWordButton.gameObject.SetActive(false);
                if (_guessBackspaceButton != null) _guessBackspaceButton.gameObject.SetActive(true);
                if (_guessAcceptButton != null) _guessAcceptButton.gameObject.SetActive(true);
                if (_guessCancelButton != null) _guessCancelButton.gameObject.SetActive(true);
            }
            else
            {
                if (_guessWordButton != null && !_wordSolved) _guessWordButton.gameObject.SetActive(true);
                if (_guessBackspaceButton != null) _guessBackspaceButton.gameObject.SetActive(false);
                if (_guessAcceptButton != null) _guessAcceptButton.gameObject.SetActive(false);
                if (_guessCancelButton != null) _guessCancelButton.gameObject.SetActive(false);
            }
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

        private void HideSetupButtons()
        {
            if (_selectButton != null)
            {
                _selectButton.gameObject.SetActive(false);
            }
            if (_coordinateModeButton != null)
            {
                _coordinateModeButton.gameObject.SetActive(false);
            }
            if (_deleteButton != null)
            {
                _deleteButton.gameObject.SetActive(false);
            }
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

            sb.Append(_rowNumber);
            sb.Append(_numberSeparator);

            switch (_currentState)
            {
                case RowState.Empty:
                    for (int i = 0; i < _requiredWordLength; i++)
                    {
                        if (i > 0) sb.Append(_letterSeparator);
                        sb.Append(_unknownLetterChar);
                    }
                    break;

                case RowState.Entering:
                    for (int i = 0; i < _requiredWordLength; i++)
                    {
                        if (i > 0) sb.Append(_letterSeparator);
                        if (i < _enteredText.Length)
                        {
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
                    for (int i = 0; i < _currentWord.Length; i++)
                    {
                        if (i > 0) sb.Append(_letterSeparator);
                        sb.Append("<u>");
                        sb.Append(_currentWord[i]);
                        sb.Append("</u>");
                    }
                    break;

                case RowState.Gameplay:
                    BuildGameplayDisplayText(sb);
                    break;
            }

            return sb.ToString();
        }

        private void BuildGameplayDisplayText(System.Text.StringBuilder sb)
        {
            string typedColorHex = ColorUtility.ToHtmlStringRGB(_guessTypedLetterColor);

            for (int i = 0; i < _currentWord.Length; i++)
            {
                if (i > 0) sb.Append(_letterSeparator);

                if (_revealedLetters[i])
                {
                    sb.Append("<u>");
                    sb.Append(_currentWord[i]);
                    sb.Append("</u>");
                }
                else if (InWordGuessMode && _wordGuessController != null && _wordGuessController.GetGuessedLetterAt(i) != '\0')
                {
                    sb.Append("<color=#");
                    sb.Append(typedColorHex);
                    sb.Append(">");
                    sb.Append(_wordGuessController.GetGuessedLetterAt(i));
                    sb.Append("</color>");
                }
                else
                {
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
            OnRowSelected?.Invoke(_rowNumber);
        }

        private void HandleCoordinateModeClick()
        {
            OnCoordinateModeClicked?.Invoke(_rowNumber);
        }

        private void HandleDeleteClick()
        {
            bool wasPlaced = _currentState == RowState.Placed;
            ClearWord();
            OnDeleteClicked?.Invoke(_rowNumber, wasPlaced);
        }

        private void HandleGuessWordClick()
        {
            EnterWordGuessMode();
        }

        private void HandleGuessBackspaceClick()
        {
            BackspaceGuessLetter();
        }

        private void HandleGuessAcceptClick()
        {
            ExitWordGuessMode(true);
        }

        private void HandleGuessCancelClick()
        {
            ExitWordGuessMode(false);
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
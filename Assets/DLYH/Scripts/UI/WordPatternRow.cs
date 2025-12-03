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
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            _revealedLetters = new bool[_requiredWordLength];
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
        /// Resets the row to empty state, clearing all word data.
        /// Used when player wants to re-enter a word.
        /// </summary>
        public void ResetToEmpty()
        {
            _currentWord = "";
            _enteredText = "";
            ResetRevealedLetters();
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
        /// </summary>
        /// <param name="word">The hidden word</param>
        public void SetGameplayWord(string word)
        {
            _currentWord = word?.ToUpper() ?? "";
            _requiredWordLength = _currentWord.Length;
            _revealedLetters = new bool[_requiredWordLength];
            SetState(RowState.Gameplay);
            UpdateDisplay();
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

            switch (_currentState)
            {
                case RowState.Empty:
                    // Show all underscores
                    for (int i = 0; i < _requiredWordLength; i++)
                    {
                        if (i > 0) sb.Append(_letterSeparator);
                        sb.Append(_unknownLetterChar);
                    }
                    break;

                case RowState.Entering:
                    // Show entered letters + remaining underscores
                    for (int i = 0; i < _requiredWordLength; i++)
                    {
                        if (i > 0) sb.Append(_letterSeparator);
                        if (i < _enteredText.Length)
                        {
                            sb.Append(_enteredText[i]);
                        }
                        else
                        {
                            sb.Append(_unknownLetterChar);
                        }
                    }
                    break;

                case RowState.WordEntered:
                case RowState.Placed:
                    // Show the full word
                    for (int i = 0; i < _currentWord.Length; i++)
                    {
                        if (i > 0) sb.Append(_letterSeparator);
                        sb.Append(_currentWord[i]);
                    }
                    break;

                case RowState.Gameplay:
                    // Show revealed letters, underscores for hidden
                    for (int i = 0; i < _currentWord.Length; i++)
                    {
                        if (i > 0) sb.Append(_letterSeparator);
                        if (_revealedLetters[i])
                        {
                            sb.Append(_currentWord[i]);
                        }
                        else
                        {
                            sb.Append(_unknownLetterChar);
                        }
                    }
                    break;
            }

            return sb.ToString();
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
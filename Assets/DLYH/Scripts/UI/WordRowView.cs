using System;
using UnityEngine;
using UnityEngine.UIElements;
using DG.Tweening;

namespace DLYH.TableUI
{
    /// <summary>
    /// Renders a single word row with variable-length letter cells and control buttons.
    /// Structure: [row number] [letter cells...] [spacer] [placement button] [clear button]
    /// During gameplay, control buttons become a single [GUESS] button.
    /// </summary>
    public class WordRowView
    {
        private VisualElement _root;
        private Label _rowNumberLabel;
        private VisualElement _letterContainer;
        private VisualElement[] _letterCells;
        private Label[] _letterLabels;
        private VisualElement _spacer;
        private Button _placementButton;
        private Button _clearButton;
        private Button _guessButton;

        private int _wordIndex;
        private int _wordLength;
        private string _word = "";
        private bool _isGameplayMode;
        private bool _isPlaced;
        private bool _isValidWord;
        private Color _playerColor;

        // Word guess mode state
        private bool _isInWordGuessMode;
        private char[] _guessedLetters; // Letters typed by player
        private bool[] _revealedPositions; // Which positions are already revealed
        private int _guessCursorPosition; // Current typing position
        private Button _backspaceButton;
        private Button _acceptButton;
        private Button _cancelButton;

        // USS class names
        private static readonly string ClassWordRow = "word-row";
        private static readonly string ClassRowNumber = "word-row-number";
        private static readonly string ClassLetterContainer = "word-row-letters";
        private static readonly string ClassLetterCell = "word-row-letter-cell";
        private static readonly string ClassLetterLabel = "word-row-letter-label";
        private static readonly string ClassSpacer = "word-row-spacer";
        private static readonly string ClassControlButton = "word-row-control-button";
        private static readonly string ClassPlacementButton = "word-row-placement-button";
        private static readonly string ClassClearButton = "word-row-clear-button";
        private static readonly string ClassGuessButton = "word-row-guess-button";
        private static readonly string ClassFilled = "word-row-filled";
        private static readonly string ClassPlaced = "word-row-placed";
        private static readonly string ClassActive = "word-row-active";
        private static readonly string ClassValid = "word-row-valid";
        private static readonly string ClassInvalid = "word-row-invalid";
        private static readonly string ClassSizeTiny = "word-row-size-tiny";
        private static readonly string ClassSizeXSmall = "word-row-size-xsmall";
        private static readonly string ClassSizeSmall = "word-row-size-small";
        private static readonly string ClassSizeMedSmall = "word-row-size-med-small";
        private static readonly string ClassSizeMedLarge = "word-row-size-med-large";
        private static readonly string ClassSizeLarge = "word-row-size-large";
        private static readonly string ClassGuessMode = "word-row-guess-mode";
        private static readonly string ClassGuessCursor = "word-row-guess-cursor";
        private static readonly string ClassGuessTyped = "word-row-guess-typed";

        /// <summary>
        /// Event fired when placement button is clicked. Parameter: word index.
        /// </summary>
        public event Action<int> OnPlacementRequested;

        /// <summary>
        /// Event fired when clear button is clicked. Parameter: word index.
        /// </summary>
        public event Action<int> OnClearRequested;

        /// <summary>
        /// Event fired when guess button is clicked (gameplay mode). Parameter: word index.
        /// </summary>
        public event Action<int> OnGuessRequested;

        /// <summary>
        /// Event fired when word guess is submitted. Parameters: word index, guessed word.
        /// </summary>
        public event Action<int, string> OnWordGuessSubmitted;

        /// <summary>
        /// Event fired when word guess is cancelled. Parameter: word index.
        /// </summary>
        public event Action<int> OnWordGuessCancelled;

        /// <summary>
        /// Event fired when word guess mode is entered. Parameter: word index.
        /// </summary>
        public event Action<int> OnWordGuessStarted;

        /// <summary>
        /// Event fired when a letter cell is clicked. Parameters: word index, letter index.
        /// </summary>
        public event Action<int, int> OnLetterCellClicked;

        /// <summary>
        /// The root VisualElement for this word row.
        /// </summary>
        public VisualElement Root => _root;

        /// <summary>
        /// The index of this word (0-based).
        /// </summary>
        public int WordIndex => _wordIndex;

        /// <summary>
        /// The expected word length for this row.
        /// </summary>
        public int WordLength => _wordLength;

        /// <summary>
        /// The current word value.
        /// </summary>
        public string Word => _word;

        /// <summary>
        /// Whether this word has been placed on the grid.
        /// </summary>
        public bool IsPlaced => _isPlaced;

        /// <summary>
        /// Whether this row is currently in word guess mode.
        /// </summary>
        public bool IsInWordGuessMode => _isInWordGuessMode;

        /// <summary>
        /// Creates a new WordRowView for the specified word index and length.
        /// </summary>
        public WordRowView(int wordIndex, int wordLength)
        {
            _wordIndex = wordIndex;
            _wordLength = wordLength;
            _playerColor = ColorRules.SelectableColors[0];
            BuildUI();
        }

        /// <summary>
        /// Builds the visual hierarchy for this word row.
        /// </summary>
        private void BuildUI()
        {
            _root = new VisualElement();
            _root.AddToClassList(ClassWordRow);

            // Row number label (1, 2, 3, 4)
            _rowNumberLabel = new Label((_wordIndex + 1).ToString() + ".");
            _rowNumberLabel.AddToClassList(ClassRowNumber);
            _root.Add(_rowNumberLabel);

            // Letter container
            _letterContainer = new VisualElement();
            _letterContainer.AddToClassList(ClassLetterContainer);

            _letterCells = new VisualElement[_wordLength];
            _letterLabels = new Label[_wordLength];

            for (int i = 0; i < _wordLength; i++)
            {
                VisualElement cell = new VisualElement();
                cell.AddToClassList(ClassLetterCell);

                Label label = new Label("_");
                label.AddToClassList(ClassLetterLabel);
                cell.Add(label);

                int capturedIndex = i;
                cell.RegisterCallback<ClickEvent>(evt =>
                {
                    OnLetterCellClicked?.Invoke(_wordIndex, capturedIndex);
                });

                _letterCells[i] = cell;
                _letterLabels[i] = label;
                _letterContainer.Add(cell);
            }

            _root.Add(_letterContainer);

            // Flexible spacer
            _spacer = new VisualElement();
            _spacer.AddToClassList(ClassSpacer);
            _root.Add(_spacer);

            // Setup mode controls
            _placementButton = new Button(() => OnPlacementRequested?.Invoke(_wordIndex));
            _placementButton.text = "+";
            _placementButton.tooltip = "Place word on grid";
            _placementButton.AddToClassList(ClassControlButton);
            _placementButton.AddToClassList(ClassPlacementButton);
            _root.Add(_placementButton);

            _clearButton = new Button(() => OnClearRequested?.Invoke(_wordIndex));
            _clearButton.text = "X";
            _clearButton.tooltip = "Clear word";
            _clearButton.AddToClassList(ClassControlButton);
            _clearButton.AddToClassList(ClassClearButton);
            _root.Add(_clearButton);

            // Gameplay mode control (hidden by default)
            _guessButton = new Button(EnterWordGuessMode);
            _guessButton.text = "GUESS";
            _guessButton.AddToClassList(ClassControlButton);
            _guessButton.AddToClassList(ClassGuessButton);
            _guessButton.style.display = DisplayStyle.None;
            _root.Add(_guessButton);

            // Word guess mode controls (hidden by default)
            _backspaceButton = new Button(HandleBackspace);
            _backspaceButton.text = "<-";
            _backspaceButton.tooltip = "Backspace";
            _backspaceButton.AddToClassList(ClassControlButton);
            _backspaceButton.AddToClassList("word-row-backspace-button");
            _backspaceButton.style.display = DisplayStyle.None;
            _root.Add(_backspaceButton);

            _acceptButton = new Button(SubmitGuess);
            _acceptButton.text = "OK";
            _acceptButton.tooltip = "Submit guess";
            _acceptButton.AddToClassList(ClassControlButton);
            _acceptButton.AddToClassList("word-row-accept-button");
            _acceptButton.style.display = DisplayStyle.None;
            _root.Add(_acceptButton);

            _cancelButton = new Button(CancelGuess);
            _cancelButton.text = "X";
            _cancelButton.tooltip = "Cancel guess";
            _cancelButton.AddToClassList(ClassControlButton);
            _cancelButton.AddToClassList("word-row-cancel-button");
            _cancelButton.style.display = DisplayStyle.None;
            _root.Add(_cancelButton);

            // Initialize guess arrays
            _guessedLetters = new char[_wordLength];
            _revealedPositions = new bool[_wordLength];

            UpdateControlState();
        }

        /// <summary>
        /// Sets the word value and updates the display.
        /// </summary>
        public void SetWord(string word)
        {
            _word = word?.ToUpper() ?? "";

            // Update letter cells
            for (int i = 0; i < _wordLength; i++)
            {
                if (i < _word.Length)
                {
                    _letterLabels[i].text = _word[i].ToString().ToUpper();
                    _letterCells[i].AddToClassList(ClassFilled);
                }
                else
                {
                    _letterLabels[i].text = "_";
                    _letterCells[i].RemoveFromClassList(ClassFilled);
                }
            }

            UpdateControlState();
        }

        /// <summary>
        /// Sets a single letter at the specified index.
        /// </summary>
        public void SetLetter(int index, char letter)
        {
            if (index < 0 || index >= _wordLength) return;

            if (letter == '\0' || letter == '_')
            {
                _letterLabels[index].text = "_";
                _letterCells[index].RemoveFromClassList(ClassFilled);
            }
            else
            {
                _letterLabels[index].text = letter.ToString().ToUpper();
                _letterCells[index].AddToClassList(ClassFilled);
            }

            // Rebuild word string
            char[] chars = new char[_wordLength];
            for (int i = 0; i < _wordLength; i++)
            {
                string text = _letterLabels[i].text;
                chars[i] = text == "_" ? '\0' : text[0];
            }
            _word = new string(chars).Replace('\0', ' ').Trim();

            UpdateControlState();
        }

        /// <summary>
        /// Marks the word as placed on the grid.
        /// During setup mode, placed words stay green.
        /// During gameplay mode, placed words use player color.
        /// </summary>
        public void SetPlaced(bool placed)
        {
            _isPlaced = placed;

            if (placed)
            {
                _root.AddToClassList(ClassPlaced);

                if (_isGameplayMode)
                {
                    // Gameplay mode: use player color for placed words
                    _root.RemoveFromClassList(ClassValid);
                    for (int i = 0; i < _wordLength; i++)
                    {
                        _letterCells[i].style.backgroundColor = _playerColor;
                        _letterLabels[i].style.color = ColorRules.GetContrastingTextColor(_playerColor);
                    }
                }
                else
                {
                    // Setup mode: keep green styling via ClassValid
                    // Don't apply player color - let USS handle it
                    if (_isValidWord)
                    {
                        _root.AddToClassList(ClassValid);
                    }
                }
            }
            else
            {
                _root.RemoveFromClassList(ClassPlaced);
                // Reset to default styling
                for (int i = 0; i < _wordLength; i++)
                {
                    _letterCells[i].style.backgroundColor = StyleKeyword.Null;
                    _letterLabels[i].style.color = StyleKeyword.Null;
                }
                // Re-apply valid class if word is valid
                if (_isValidWord && !_isGameplayMode)
                {
                    _root.AddToClassList(ClassValid);
                }
            }

            UpdateControlState();
        }

        /// <summary>
        /// Sets the player color for placed word visualization.
        /// </summary>
        public void SetPlayerColor(Color color)
        {
            _playerColor = color;
            if (_isPlaced)
            {
                SetPlaced(true); // Re-apply color
            }
        }

        /// <summary>
        /// Sets whether this row is currently active (being edited/placed).
        /// </summary>
        public void SetActive(bool active)
        {
            if (active)
            {
                _root.AddToClassList(ClassActive);
            }
            else
            {
                _root.RemoveFromClassList(ClassActive);
            }
        }

        /// <summary>
        /// Sets whether the current word is valid (exists in dictionary).
        /// Controls placement button enabled state and shows green highlight when valid.
        /// </summary>
        public void SetWordValid(bool isValid)
        {
            _isValidWord = isValid;

            // Apply/remove valid class for green highlight (setup mode only, not when placed)
            if (isValid && !_isPlaced && !_isGameplayMode)
            {
                _root.AddToClassList(ClassValid);
            }
            else
            {
                _root.RemoveFromClassList(ClassValid);
            }

            UpdateControlState();
        }

        /// <summary>
        /// Whether the current word is valid.
        /// </summary>
        public bool IsValidWord => _isValidWord;

        /// <summary>
        /// Shows visual feedback for an invalid word (red highlight + shake animation).
        /// The red highlight persists until ClearInvalidFeedback() is called.
        /// The shake animation runs once then stops (but red remains).
        /// </summary>
        public void ShowInvalidFeedback()
        {
            // Add invalid styling (red highlight - persists until cleared)
            _root.AddToClassList(ClassInvalid);

            // Shake animation using DOTween on the letter container
            // UI Toolkit doesn't support CSS keyframes, so we animate via code
            float originalLeft = _letterContainer.resolvedStyle.marginLeft;

            // Create shake sequence
            Sequence shakeSequence = DOTween.Sequence();
            shakeSequence.Append(DOTween.To(
                () => _letterContainer.style.marginLeft.value.value,
                x => _letterContainer.style.marginLeft = x,
                originalLeft - 8f, 0.05f));
            shakeSequence.Append(DOTween.To(
                () => _letterContainer.style.marginLeft.value.value,
                x => _letterContainer.style.marginLeft = x,
                originalLeft + 8f, 0.05f));
            shakeSequence.Append(DOTween.To(
                () => _letterContainer.style.marginLeft.value.value,
                x => _letterContainer.style.marginLeft = x,
                originalLeft - 6f, 0.05f));
            shakeSequence.Append(DOTween.To(
                () => _letterContainer.style.marginLeft.value.value,
                x => _letterContainer.style.marginLeft = x,
                originalLeft + 6f, 0.05f));
            shakeSequence.Append(DOTween.To(
                () => _letterContainer.style.marginLeft.value.value,
                x => _letterContainer.style.marginLeft = x,
                originalLeft - 3f, 0.05f));
            shakeSequence.Append(DOTween.To(
                () => _letterContainer.style.marginLeft.value.value,
                x => _letterContainer.style.marginLeft = x,
                originalLeft + 3f, 0.05f));
            shakeSequence.Append(DOTween.To(
                () => _letterContainer.style.marginLeft.value.value,
                x => _letterContainer.style.marginLeft = x,
                originalLeft, 0.05f));
        }

        /// <summary>
        /// Clears the invalid word visual feedback (red highlight).
        /// Called when user modifies the word (backspace, clear, etc).
        /// </summary>
        public void ClearInvalidFeedback()
        {
            _root.RemoveFromClassList(ClassInvalid);
        }

        /// <summary>
        /// Switches to gameplay mode (shows GUESS button instead of placement/clear).
        /// </summary>
        public void SetGameplayMode(bool gameplay)
        {
            _isGameplayMode = gameplay;

            if (gameplay)
            {
                _placementButton.style.display = DisplayStyle.None;
                _clearButton.style.display = DisplayStyle.None;
                _guessButton.style.display = DisplayStyle.Flex;
            }
            else
            {
                _placementButton.style.display = DisplayStyle.Flex;
                _clearButton.style.display = DisplayStyle.Flex;
                _guessButton.style.display = DisplayStyle.None;
            }

            UpdateControlState();
        }

        /// <summary>
        /// Hides all control buttons (for defense view where no interaction is needed).
        /// </summary>
        public void HideAllButtons()
        {
            _placementButton.style.display = DisplayStyle.None;
            _clearButton.style.display = DisplayStyle.None;
            _guessButton.style.display = DisplayStyle.None;
        }

        /// <summary>
        /// Clears the word and resets the display.
        /// </summary>
        public void Clear()
        {
            SetWord("");
            SetPlaced(false);
            SetActive(false);
        }

        /// <summary>
        /// Sets the size class for responsive scaling.
        /// </summary>
        /// <param name="sizeClass">"tiny", "xsmall", "small", "med-small", "medium", "med-large", or "large"</param>
        public void SetSizeClass(string sizeClass)
        {
            _root.RemoveFromClassList(ClassSizeTiny);
            _root.RemoveFromClassList(ClassSizeXSmall);
            _root.RemoveFromClassList(ClassSizeSmall);
            _root.RemoveFromClassList(ClassSizeMedSmall);
            _root.RemoveFromClassList(ClassSizeMedLarge);
            _root.RemoveFromClassList(ClassSizeLarge);

            switch (sizeClass)
            {
                case "tiny":
                    _root.AddToClassList(ClassSizeTiny);
                    break;
                case "xsmall":
                    _root.AddToClassList(ClassSizeXSmall);
                    break;
                case "small":
                    _root.AddToClassList(ClassSizeSmall);
                    break;
                case "med-small":
                    _root.AddToClassList(ClassSizeMedSmall);
                    break;
                case "med-large":
                    _root.AddToClassList(ClassSizeMedLarge);
                    break;
                case "large":
                    _root.AddToClassList(ClassSizeLarge);
                    break;
                // "medium" is the default, no class needed
            }
        }

        /// <summary>
        /// Applies viewport-aware sizing to letter cells via inline styles.
        /// This overrides CSS class sizes for better viewport responsiveness.
        /// Word row cells are scaled to 85% of grid cell size for better visual hierarchy.
        /// </summary>
        /// <param name="cellSize">Grid cell size in pixels (will be scaled down for word rows)</param>
        /// <param name="fontSize">Font size in pixels for cell labels (will be scaled down)</param>
        public void ApplyViewportAwareSizing(int cellSize, int fontSize)
        {
            if (cellSize <= 0) return;

            // Word row cells should be smaller than grid cells for visual hierarchy
            // Scale to 85% of grid size, with minimum of 18px
            int wordRowCellSize = Mathf.Max(18, (int)(cellSize * 0.85f));
            int wordRowFontSize = Mathf.Max(9, (int)(fontSize * 0.85f));

            for (int i = 0; i < _wordLength; i++)
            {
                if (_letterCells[i] != null)
                {
                    _letterCells[i].style.width = wordRowCellSize;
                    _letterCells[i].style.height = wordRowCellSize;
                    _letterCells[i].style.minWidth = wordRowCellSize;
                    _letterCells[i].style.minHeight = wordRowCellSize;
                }
                if (_letterLabels[i] != null && wordRowFontSize > 0)
                {
                    _letterLabels[i].style.fontSize = wordRowFontSize;
                }
            }

            // Also scale row number label proportionally
            if (_rowNumberLabel != null && wordRowFontSize > 0)
            {
                _rowNumberLabel.style.fontSize = wordRowFontSize;
                _rowNumberLabel.style.minWidth = wordRowCellSize;
            }
        }

        /// <summary>
        /// Updates the enabled state of control buttons based on current state.
        /// </summary>
        private void UpdateControlState()
        {
            bool hasWord = _word.Length == _wordLength;

            // Placement button: enabled when word is complete, valid, and not placed
            _placementButton.SetEnabled(hasWord && _isValidWord && !_isPlaced);

            // Clear button: enabled when there's any content or placement
            _clearButton.SetEnabled(_word.Length > 0 || _isPlaced);

            // Guess button: enabled when in gameplay mode
            _guessButton.SetEnabled(_isGameplayMode);
        }

        #region Gameplay Display Methods

        /// <summary>
        /// Sets up the word row for gameplay display with underscores.
        /// The actual word is stored internally but only revealed letters are shown.
        /// </summary>
        /// <param name="actualWord">The actual word (stored internally)</param>
        public void SetWordForGameplay(string actualWord)
        {
            _word = actualWord?.ToUpper() ?? "";

            // Display all cells as underscores initially
            for (int i = 0; i < _wordLength; i++)
            {
                _letterLabels[i].text = "_";
                _letterCells[i].RemoveFromClassList(ClassFilled);
            }

            UpdateControlState();
        }

        /// <summary>
        /// Reveals a specific letter in the word display (for gameplay hit feedback).
        /// </summary>
        /// <param name="letterIndex">The index in the word to reveal</param>
        /// <param name="playerColor">The color to highlight the revealed letter</param>
        public void RevealLetter(int letterIndex, Color playerColor)
        {
            if (letterIndex < 0 || letterIndex >= _wordLength || letterIndex >= _word.Length) return;

            char letter = _word[letterIndex];
            _letterLabels[letterIndex].text = letter.ToString();
            _letterCells[letterIndex].AddToClassList(ClassFilled);
            _letterCells[letterIndex].style.backgroundColor = playerColor;
            _letterLabels[letterIndex].style.color = ColorRules.GetContrastingTextColor(playerColor);

            // Track as revealed for word guess mode
            SetPositionRevealed(letterIndex, true);

            // Auto-hide GUESS button if word is now fully revealed
            CheckAndHideGuessButtonIfComplete();
        }

        /// <summary>
        /// Reveals a letter in "found" state (yellow) - coordinate not yet known.
        /// </summary>
        /// <param name="letterIndex">The index in the word to reveal</param>
        public void RevealLetterAsFound(int letterIndex)
        {
            if (letterIndex < 0 || letterIndex >= _wordLength || letterIndex >= _word.Length) return;

            char letter = _word[letterIndex];
            _letterLabels[letterIndex].text = letter.ToString();
            _letterCells[letterIndex].AddToClassList(ClassFilled);
            // Yellow/amber color for "found but coordinate unknown"
            Color foundColor = new Color(0.85f, 0.65f, 0.2f, 1f); // Amber/yellow
            _letterCells[letterIndex].style.backgroundColor = foundColor;
            _letterLabels[letterIndex].style.color = new Color(0.12f, 0.12f, 0.12f, 1f); // Dark text

            // Track as revealed for word guess mode
            SetPositionRevealed(letterIndex, true);

            // Auto-hide GUESS button if word is now fully revealed
            CheckAndHideGuessButtonIfComplete();
        }

        /// <summary>
        /// Upgrades a found letter to full reveal with player color.
        /// </summary>
        /// <param name="letterIndex">The index in the word to upgrade</param>
        /// <param name="playerColor">The player's color</param>
        public void UpgradeLetterToPlayerColor(int letterIndex, Color playerColor)
        {
            if (letterIndex < 0 || letterIndex >= _wordLength) return;

            // Only upgrade if the cell already has content (was revealed)
            if (_letterLabels[letterIndex].text != "_")
            {
                _letterCells[letterIndex].style.backgroundColor = playerColor;
                _letterLabels[letterIndex].style.color = ColorRules.GetContrastingTextColor(playerColor);
            }
        }

        /// <summary>
        /// Reveals all occurrences of a letter in this word.
        /// </summary>
        /// <param name="letter">The letter to reveal</param>
        /// <param name="playerColor">The color to highlight revealed letters</param>
        /// <returns>Number of positions revealed</returns>
        public int RevealAllOccurrences(char letter, Color playerColor)
        {
            letter = char.ToUpper(letter);
            int count = 0;

            for (int i = 0; i < _word.Length && i < _wordLength; i++)
            {
                if (_word[i] == letter)
                {
                    RevealLetter(i, playerColor);
                    count++;
                }
            }

            return count;
        }

        /// <summary>
        /// Checks if all letters in the word have been revealed.
        /// </summary>
        public bool IsFullyRevealed()
        {
            for (int i = 0; i < _wordLength; i++)
            {
                if (_letterLabels[i].text == "_")
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Gets the actual word stored (for gameplay validation).
        /// </summary>
        public string ActualWord => _word;

        /// <summary>
        /// Gets the currently displayed word (with underscores for unrevealed letters).
        /// </summary>
        public string GetDisplayedWord()
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            for (int i = 0; i < _wordLength; i++)
            {
                sb.Append(_letterLabels[i].text);
            }
            return sb.ToString();
        }

        /// <summary>
        /// Reveals all letters in the word (when word is correctly guessed).
        /// </summary>
        public void RevealAllLetters(Color playerColor)
        {
            for (int i = 0; i < _word.Length && i < _wordLength; i++)
            {
                RevealLetter(i, playerColor);
            }
        }

        /// <summary>
        /// Hides the guess button (when word is solved).
        /// </summary>
        public void HideGuessButton()
        {
            _guessButton.style.display = DisplayStyle.None;
        }

        /// <summary>
        /// Shows the guess button.
        /// </summary>
        public void ShowGuessButton()
        {
            _guessButton.style.display = DisplayStyle.Flex;
        }

        /// <summary>
        /// Checks if all letters are revealed and hides GUESS button if so.
        /// </summary>
        private void CheckAndHideGuessButtonIfComplete()
        {
            if (IsFullyRevealed())
            {
                HideGuessButton();
            }
        }

        #endregion

        #region Word Guess Mode (Inline Typing)

        /// <summary>
        /// Enters word guess mode, allowing inline typing of unrevealed letters.
        /// </summary>
        public void EnterWordGuessMode()
        {
            if (!_isGameplayMode) return;
            if (_isInWordGuessMode) return;

            _isInWordGuessMode = true;
            _root.AddToClassList(ClassGuessMode);

            // Clear previous guess
            for (int i = 0; i < _wordLength; i++)
            {
                _guessedLetters[i] = '\0';
            }

            // Find first unrevealed position
            _guessCursorPosition = FindNextUnrevealedPosition(-1);

            // Show/hide buttons
            _guessButton.style.display = DisplayStyle.None;
            _backspaceButton.style.display = DisplayStyle.Flex;
            _acceptButton.style.display = DisplayStyle.Flex;
            _cancelButton.style.display = DisplayStyle.Flex;

            UpdateGuessDisplay();
            OnWordGuessStarted?.Invoke(_wordIndex);

            Debug.Log($"[WordRowView] Entered word guess mode for word {_wordIndex}, cursor at {_guessCursorPosition}");
        }

        /// <summary>
        /// Exits word guess mode without submitting.
        /// </summary>
        public void ExitWordGuessMode()
        {
            if (!_isInWordGuessMode) return;

            _isInWordGuessMode = false;
            _root.RemoveFromClassList(ClassGuessMode);

            // Clear guess letters
            for (int i = 0; i < _wordLength; i++)
            {
                _guessedLetters[i] = '\0';
            }

            // Restore buttons
            _guessButton.style.display = DisplayStyle.Flex;
            _backspaceButton.style.display = DisplayStyle.None;
            _acceptButton.style.display = DisplayStyle.None;
            _cancelButton.style.display = DisplayStyle.None;

            UpdateGuessDisplay();
        }

        /// <summary>
        /// Types a letter at the current cursor position.
        /// </summary>
        public bool TypeLetter(char letter)
        {
            if (!_isInWordGuessMode) return false;
            if (_guessCursorPosition < 0 || _guessCursorPosition >= _wordLength) return false;

            letter = char.ToUpper(letter);
            if (letter < 'A' || letter > 'Z') return false;

            // Type at current position
            _guessedLetters[_guessCursorPosition] = letter;

            // Move to next unrevealed position
            _guessCursorPosition = FindNextUnrevealedPosition(_guessCursorPosition);

            UpdateGuessDisplay();
            return true;
        }

        /// <summary>
        /// Handles backspace - removes last typed letter.
        /// </summary>
        private void HandleBackspace()
        {
            Backspace();
        }

        /// <summary>
        /// Removes the last typed letter (backspace functionality).
        /// </summary>
        public bool Backspace()
        {
            if (!_isInWordGuessMode) return false;

            // Find last typed position
            int lastTyped = FindPreviousTypedPosition(_guessCursorPosition >= 0 ? _guessCursorPosition : _wordLength);
            if (lastTyped >= 0)
            {
                _guessedLetters[lastTyped] = '\0';
                _guessCursorPosition = lastTyped;
                UpdateGuessDisplay();
                return true;
            }
            return false;
        }

        /// <summary>
        /// Submits the current guess.
        /// </summary>
        private void SubmitGuess()
        {
            if (!_isInWordGuessMode) return;

            string guessedWord = GetFullGuessWord();

            // Check if complete
            if (guessedWord.Contains("_"))
            {
                Debug.Log("[WordRowView] Cannot submit incomplete guess");
                return;
            }

            _isInWordGuessMode = false;
            _root.RemoveFromClassList(ClassGuessMode);

            // Clear guess letters so they don't persist after wrong guess
            for (int i = 0; i < _wordLength; i++)
            {
                _guessedLetters[i] = '\0';
            }

            // Restore buttons (will be updated by handler based on result)
            _guessButton.style.display = DisplayStyle.Flex;
            _backspaceButton.style.display = DisplayStyle.None;
            _acceptButton.style.display = DisplayStyle.None;
            _cancelButton.style.display = DisplayStyle.None;

            // Update display to show underscores for unrevealed positions
            UpdateGuessDisplay();

            OnWordGuessSubmitted?.Invoke(_wordIndex, guessedWord);
        }

        /// <summary>
        /// Cancels the current guess.
        /// </summary>
        private void CancelGuess()
        {
            if (!_isInWordGuessMode) return;

            ExitWordGuessMode();
            OnWordGuessCancelled?.Invoke(_wordIndex);
        }

        /// <summary>
        /// Gets the full guessed word (revealed + typed letters).
        /// </summary>
        public string GetFullGuessWord()
        {
            char[] result = new char[_wordLength];
            for (int i = 0; i < _wordLength; i++)
            {
                if (_revealedPositions[i] && i < _word.Length)
                {
                    result[i] = _word[i];
                }
                else if (_guessedLetters[i] != '\0')
                {
                    result[i] = _guessedLetters[i];
                }
                else
                {
                    result[i] = '_';
                }
            }
            return new string(result);
        }

        /// <summary>
        /// Checks if all positions have been filled (revealed or typed).
        /// </summary>
        public bool IsGuessComplete()
        {
            for (int i = 0; i < _wordLength; i++)
            {
                if (!_revealedPositions[i] && _guessedLetters[i] == '\0')
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Finds the next unrevealed position after the given index.
        /// </summary>
        private int FindNextUnrevealedPosition(int afterIndex)
        {
            for (int i = afterIndex + 1; i < _wordLength; i++)
            {
                if (!_revealedPositions[i])
                {
                    return i;
                }
            }
            return -1; // No more unrevealed positions
        }

        /// <summary>
        /// Finds the previous typed (non-revealed) position before the given index.
        /// </summary>
        private int FindPreviousTypedPosition(int beforeIndex)
        {
            for (int i = beforeIndex - 1; i >= 0; i--)
            {
                if (!_revealedPositions[i] && _guessedLetters[i] != '\0')
                {
                    return i;
                }
            }
            // If no typed letters found, find last unrevealed position
            for (int i = beforeIndex - 1; i >= 0; i--)
            {
                if (!_revealedPositions[i])
                {
                    return i;
                }
            }
            return -1;
        }

        /// <summary>
        /// Updates the display during word guess mode.
        /// </summary>
        private void UpdateGuessDisplay()
        {
            for (int i = 0; i < _wordLength; i++)
            {
                // Remove cursor/typed classes
                _letterCells[i].RemoveFromClassList(ClassGuessCursor);
                _letterCells[i].RemoveFromClassList(ClassGuessTyped);

                if (_isInWordGuessMode)
                {
                    if (_revealedPositions[i])
                    {
                        // Already revealed - show letter with player color
                        _letterLabels[i].text = _word[i].ToString();
                    }
                    else if (_guessedLetters[i] != '\0')
                    {
                        // Typed letter - show with typed style (orange)
                        _letterLabels[i].text = _guessedLetters[i].ToString();
                        _letterCells[i].AddToClassList(ClassGuessTyped);
                    }
                    else
                    {
                        // Unrevealed, not typed - show underscore
                        _letterLabels[i].text = "_";
                    }

                    // Mark cursor position
                    if (i == _guessCursorPosition)
                    {
                        _letterCells[i].AddToClassList(ClassGuessCursor);
                    }
                }
                else
                {
                    // Not in guess mode - show normal display
                    if (_revealedPositions[i] && i < _word.Length)
                    {
                        _letterLabels[i].text = _word[i].ToString();
                    }
                    else
                    {
                        _letterLabels[i].text = "_";
                    }
                }
            }
        }

        /// <summary>
        /// Marks a position as revealed (used when letters are found on grid).
        /// </summary>
        public void SetPositionRevealed(int index, bool revealed)
        {
            if (index < 0 || index >= _wordLength) return;
            _revealedPositions[index] = revealed;
        }

        /// <summary>
        /// Checks if a position is revealed.
        /// </summary>
        public bool IsPositionRevealed(int index)
        {
            if (index < 0 || index >= _wordLength) return false;
            return _revealedPositions[index];
        }

        #endregion
    }
}

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
        private static readonly string ClassSizeSmall = "word-row-size-small";
        private static readonly string ClassSizeLarge = "word-row-size-large";

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
            _placementButton.text = "⊕";
            _placementButton.tooltip = "Place word on grid";
            _placementButton.AddToClassList(ClassControlButton);
            _placementButton.AddToClassList(ClassPlacementButton);
            _root.Add(_placementButton);

            _clearButton = new Button(() => OnClearRequested?.Invoke(_wordIndex));
            _clearButton.text = "✕";
            _clearButton.tooltip = "Clear word";
            _clearButton.AddToClassList(ClassControlButton);
            _clearButton.AddToClassList(ClassClearButton);
            _root.Add(_clearButton);

            // Gameplay mode control (hidden by default)
            _guessButton = new Button(() => OnGuessRequested?.Invoke(_wordIndex));
            _guessButton.text = "GUESS";
            _guessButton.AddToClassList(ClassControlButton);
            _guessButton.AddToClassList(ClassGuessButton);
            _guessButton.style.display = DisplayStyle.None;
            _root.Add(_guessButton);

            UpdateControlState();
        }

        /// <summary>
        /// Sets the word value and updates the display.
        /// </summary>
        public void SetWord(string word)
        {
            _word = word ?? "";

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
        /// <param name="sizeClass">"tiny", "small", "medium", or "large"</param>
        public void SetSizeClass(string sizeClass)
        {
            _root.RemoveFromClassList(ClassSizeTiny);
            _root.RemoveFromClassList(ClassSizeSmall);
            _root.RemoveFromClassList(ClassSizeLarge);

            switch (sizeClass)
            {
                case "tiny":
                    _root.AddToClassList(ClassSizeTiny);
                    break;
                case "small":
                    _root.AddToClassList(ClassSizeSmall);
                    break;
                case "large":
                    _root.AddToClassList(ClassSizeLarge);
                    break;
                // "medium" is the default, no class needed
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

        #endregion
    }
}

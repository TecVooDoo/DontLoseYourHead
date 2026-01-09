using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace DLYH.TableUI
{
    /// <summary>
    /// Container that manages all word rows above the grid table.
    /// Handles word entry, placement coordination, and state management.
    /// </summary>
    public class WordRowsContainer
    {
        private VisualElement _root;
        private WordRowView[] _wordRows;
        private int _wordCount;
        private int[] _wordLengths;
        private int _activeRowIndex = -1;
        private bool _isGameplayMode;
        private Color _playerColor;

        // USS class names
        private static readonly string ClassContainer = "word-rows-container";

        /// <summary>
        /// Event fired when placement is requested for a word.
        /// Parameters: word index, word text.
        /// </summary>
        public event Action<int, string> OnPlacementRequested;

        /// <summary>
        /// Event fired when a word is cleared.
        /// Parameter: word index.
        /// </summary>
        public event Action<int> OnWordCleared;

        /// <summary>
        /// Event fired when guess is requested (gameplay mode).
        /// Parameters: word index, word text.
        /// </summary>
        public event Action<int, string> OnGuessRequested;

        /// <summary>
        /// Event fired when a letter cell is clicked for text entry.
        /// Parameters: word index, letter index.
        /// </summary>
        public event Action<int, int> OnLetterCellClicked;

        /// <summary>
        /// Event fired when all words have been placed.
        /// </summary>
        public event Action OnAllWordsPlaced;

        /// <summary>
        /// The root VisualElement for the container.
        /// </summary>
        public VisualElement Root => _root;

        /// <summary>
        /// Number of word rows.
        /// </summary>
        public int WordCount => _wordCount;

        /// <summary>
        /// Index of currently active (editing) row, or -1 if none.
        /// </summary>
        public int ActiveRowIndex => _activeRowIndex;

        /// <summary>
        /// Creates a new WordRowsContainer with the specified word configuration.
        /// </summary>
        public WordRowsContainer(int wordCount, int[] wordLengths = null)
        {
            _wordCount = wordCount;
            _wordLengths = wordLengths ?? TableLayout.GetStandardWordLengths(wordCount);
            _playerColor = ColorRules.SelectableColors[0];

            BuildUI();
        }

        /// <summary>
        /// Builds the visual hierarchy.
        /// </summary>
        private void BuildUI()
        {
            _root = new VisualElement();
            _root.AddToClassList(ClassContainer);

            _wordRows = new WordRowView[_wordCount];

            for (int i = 0; i < _wordCount; i++)
            {
                int length = i < _wordLengths.Length ? _wordLengths[i] : 3 + i;
                WordRowView row = new WordRowView(i, length);

                // Subscribe to row events
                row.OnPlacementRequested += HandlePlacementRequested;
                row.OnClearRequested += HandleClearRequested;
                row.OnGuessRequested += HandleGuessRequested;
                row.OnLetterCellClicked += HandleLetterCellClicked;

                row.SetPlayerColor(_playerColor);

                _wordRows[i] = row;
                _root.Add(row.Root);
            }
        }

        /// <summary>
        /// Sets the word for a specific row.
        /// </summary>
        public void SetWord(int rowIndex, string word)
        {
            if (rowIndex < 0 || rowIndex >= _wordCount) return;
            _wordRows[rowIndex].SetWord(word);
        }

        /// <summary>
        /// Gets the word from a specific row.
        /// </summary>
        public string GetWord(int rowIndex)
        {
            if (rowIndex < 0 || rowIndex >= _wordCount) return "";
            return _wordRows[rowIndex].Word;
        }

        /// <summary>
        /// Gets the expected word length for a row.
        /// </summary>
        public int GetWordLength(int rowIndex)
        {
            if (rowIndex < 0 || rowIndex >= _wordCount) return 0;
            return _wordRows[rowIndex].WordLength;
        }

        /// <summary>
        /// Marks a word as placed on the grid.
        /// </summary>
        public void SetWordPlaced(int rowIndex, bool placed)
        {
            if (rowIndex < 0 || rowIndex >= _wordCount) return;
            _wordRows[rowIndex].SetPlaced(placed);

            CheckAllWordsPlaced();
        }

        /// <summary>
        /// Sets the active row for editing/placement.
        /// </summary>
        public void SetActiveRow(int rowIndex)
        {
            // Deactivate previous
            if (_activeRowIndex >= 0 && _activeRowIndex < _wordCount)
            {
                _wordRows[_activeRowIndex].SetActive(false);
            }

            _activeRowIndex = rowIndex;

            // Activate new
            if (_activeRowIndex >= 0 && _activeRowIndex < _wordCount)
            {
                _wordRows[_activeRowIndex].SetActive(true);
            }
        }

        /// <summary>
        /// Clears the active row selection.
        /// </summary>
        public void ClearActiveRow()
        {
            SetActiveRow(-1);
        }

        /// <summary>
        /// Sets the player color for all word rows.
        /// </summary>
        public void SetPlayerColor(Color color)
        {
            _playerColor = color;
            for (int i = 0; i < _wordCount; i++)
            {
                _wordRows[i].SetPlayerColor(color);
            }
        }

        /// <summary>
        /// Switches all rows to gameplay mode.
        /// </summary>
        public void SetGameplayMode(bool gameplay)
        {
            _isGameplayMode = gameplay;
            for (int i = 0; i < _wordCount; i++)
            {
                _wordRows[i].SetGameplayMode(gameplay);
            }
        }

        /// <summary>
        /// Clears all words and resets state.
        /// </summary>
        public void ClearAll()
        {
            for (int i = 0; i < _wordCount; i++)
            {
                _wordRows[i].Clear();
            }
            _activeRowIndex = -1;
        }

        /// <summary>
        /// Returns true if all words are placed.
        /// </summary>
        public bool AreAllWordsPlaced()
        {
            for (int i = 0; i < _wordCount; i++)
            {
                if (!_wordRows[i].IsPlaced)
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Returns true if all words are filled (regardless of placement).
        /// </summary>
        public bool AreAllWordsFilled()
        {
            for (int i = 0; i < _wordCount; i++)
            {
                if (_wordRows[i].Word.Length != _wordRows[i].WordLength)
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Gets the index of the first empty/incomplete word row.
        /// Returns -1 if all rows are complete.
        /// </summary>
        public int GetFirstEmptyRowIndex()
        {
            for (int i = 0; i < _wordCount; i++)
            {
                if (_wordRows[i].Word.Length < _wordRows[i].WordLength)
                {
                    return i;
                }
            }
            return -1;
        }

        /// <summary>
        /// Gets the index of the first unplaced word row.
        /// Returns -1 if all rows are placed.
        /// </summary>
        public int GetFirstUnplacedRowIndex()
        {
            for (int i = 0; i < _wordCount; i++)
            {
                if (!_wordRows[i].IsPlaced)
                {
                    return i;
                }
            }
            return -1;
        }

        /// <summary>
        /// Gets the WordRowView for a specific index.
        /// </summary>
        public WordRowView GetRow(int index)
        {
            if (index < 0 || index >= _wordCount) return null;
            return _wordRows[index];
        }

        private void HandlePlacementRequested(int wordIndex)
        {
            if (wordIndex < 0 || wordIndex >= _wordCount) return;
            string word = _wordRows[wordIndex].Word;
            if (word.Length == _wordRows[wordIndex].WordLength)
            {
                SetActiveRow(wordIndex);
                OnPlacementRequested?.Invoke(wordIndex, word);
            }
        }

        private void HandleClearRequested(int wordIndex)
        {
            if (wordIndex < 0 || wordIndex >= _wordCount) return;
            _wordRows[wordIndex].Clear();
            OnWordCleared?.Invoke(wordIndex);
        }

        private void HandleGuessRequested(int wordIndex)
        {
            if (wordIndex < 0 || wordIndex >= _wordCount) return;
            string word = _wordRows[wordIndex].Word;
            OnGuessRequested?.Invoke(wordIndex, word);
        }

        private void HandleLetterCellClicked(int wordIndex, int letterIndex)
        {
            OnLetterCellClicked?.Invoke(wordIndex, letterIndex);
        }

        private void CheckAllWordsPlaced()
        {
            if (AreAllWordsPlaced())
            {
                OnAllWordsPlaced?.Invoke();
            }
        }

        /// <summary>
        /// Cleans up event subscriptions.
        /// </summary>
        public void Dispose()
        {
            for (int i = 0; i < _wordCount; i++)
            {
                _wordRows[i].OnPlacementRequested -= HandlePlacementRequested;
                _wordRows[i].OnClearRequested -= HandleClearRequested;
                _wordRows[i].OnGuessRequested -= HandleGuessRequested;
                _wordRows[i].OnLetterCellClicked -= HandleLetterCellClicked;
            }
        }
    }
}

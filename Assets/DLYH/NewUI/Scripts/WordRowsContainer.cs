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
        private int _wordGuessRowIndex = -1; // Which row is currently in word guess mode

        // USS class names
        private static readonly string ClassContainer = "word-rows-content";

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
        /// Event fired when word guess is submitted.
        /// Parameters: word index, guessed word.
        /// </summary>
        public event Action<int, string> OnWordGuessSubmitted;

        /// <summary>
        /// Event fired when word guess is cancelled.
        /// Parameter: word index.
        /// </summary>
        public event Action<int> OnWordGuessCancelled;

        /// <summary>
        /// Event fired when word guess mode is entered.
        /// Parameter: word index.
        /// </summary>
        public event Action<int> OnWordGuessStarted;

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
                row.OnWordGuessSubmitted += HandleWordGuessSubmitted;
                row.OnWordGuessCancelled += HandleWordGuessCancelled;
                row.OnWordGuessStarted += HandleWordGuessStarted;

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
        /// Hides all control buttons on all word rows (for defense view).
        /// </summary>
        public void HideAllButtons()
        {
            for (int i = 0; i < _wordCount; i++)
            {
                _wordRows[i].HideAllButtons();
            }
        }

        /// <summary>
        /// Sets the size class for all word rows based on grid dimensions.
        /// </summary>
        /// <param name="sizeClass">"small", "medium", or "large"</param>
        public void SetSizeClass(string sizeClass)
        {
            for (int i = 0; i < _wordCount; i++)
            {
                _wordRows[i].SetSizeClass(sizeClass);
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
        /// Clears only placement status, keeping words intact.
        /// </summary>
        public void ClearAllPlacements()
        {
            for (int i = 0; i < _wordCount; i++)
            {
                _wordRows[i].SetPlaced(false);
            }
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

        /// <summary>
        /// Shows invalid word feedback (red highlight + shake) on a specific row.
        /// Red highlight persists until ClearInvalidFeedback is called.
        /// </summary>
        public void ShowInvalidFeedback(int rowIndex)
        {
            if (rowIndex < 0 || rowIndex >= _wordCount) return;
            _wordRows[rowIndex].ShowInvalidFeedback();
        }

        /// <summary>
        /// Clears invalid word feedback (red highlight) on a specific row.
        /// </summary>
        public void ClearInvalidFeedback(int rowIndex)
        {
            if (rowIndex < 0 || rowIndex >= _wordCount) return;
            _wordRows[rowIndex].ClearInvalidFeedback();
        }

        /// <summary>
        /// Sets whether a word is valid (exists in dictionary).
        /// Controls placement button enabled state.
        /// </summary>
        public void SetWordValid(int rowIndex, bool isValid)
        {
            if (rowIndex < 0 || rowIndex >= _wordCount) return;
            _wordRows[rowIndex].SetWordValid(isValid);
        }

        /// <summary>
        /// Gets whether a word is valid.
        /// </summary>
        public bool IsWordValid(int rowIndex)
        {
            if (rowIndex < 0 || rowIndex >= _wordCount) return false;
            return _wordRows[rowIndex].IsValidWord;
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

        private void HandleWordGuessSubmitted(int wordIndex, string guessedWord)
        {
            _wordGuessRowIndex = -1;
            OnWordGuessSubmitted?.Invoke(wordIndex, guessedWord);
        }

        private void HandleWordGuessCancelled(int wordIndex)
        {
            _wordGuessRowIndex = -1;
            OnWordGuessCancelled?.Invoke(wordIndex);
        }

        private void HandleWordGuessStarted(int wordIndex)
        {
            // Exit any previous row's guess mode
            if (_wordGuessRowIndex >= 0 && _wordGuessRowIndex != wordIndex && _wordGuessRowIndex < _wordCount)
            {
                _wordRows[_wordGuessRowIndex].ExitWordGuessMode();
            }
            _wordGuessRowIndex = wordIndex;
            OnWordGuessStarted?.Invoke(wordIndex);
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
                _wordRows[i].OnWordGuessSubmitted -= HandleWordGuessSubmitted;
                _wordRows[i].OnWordGuessCancelled -= HandleWordGuessCancelled;
                _wordRows[i].OnWordGuessStarted -= HandleWordGuessStarted;
            }
        }

        #region Gameplay Display Methods

        /// <summary>
        /// Sets up all word rows for gameplay display (opponent's words with underscores).
        /// </summary>
        /// <param name="words">The opponent's actual words (will be hidden initially)</param>
        public void SetWordsForGameplay(string[] words)
        {
            for (int i = 0; i < _wordCount && i < words.Length; i++)
            {
                _wordRows[i].SetWordForGameplay(words[i]);
            }
        }

        /// <summary>
        /// Reveals all occurrences of a letter across all words.
        /// </summary>
        /// <param name="letter">The letter to reveal</param>
        /// <param name="playerColor">The color to highlight revealed letters</param>
        /// <returns>Total number of positions revealed</returns>
        public int RevealLetterInAllWords(char letter, Color playerColor)
        {
            int total = 0;
            for (int i = 0; i < _wordCount; i++)
            {
                total += _wordRows[i].RevealAllOccurrences(letter, playerColor);
            }
            return total;
        }

        /// <summary>
        /// Reveals all occurrences of a letter as "found" (yellow) - coordinates not yet known.
        /// </summary>
        /// <param name="letter">The letter to reveal</param>
        /// <returns>Total number of positions revealed</returns>
        public int RevealLetterAsFoundInAllWords(char letter)
        {
            letter = char.ToUpper(letter);
            int total = 0;
            for (int i = 0; i < _wordCount; i++)
            {
                string word = _wordRows[i].ActualWord;
                for (int j = 0; j < word.Length; j++)
                {
                    if (word[j] == letter)
                    {
                        _wordRows[i].RevealLetterAsFound(j);
                        total++;
                    }
                }
            }
            return total;
        }

        /// <summary>
        /// Upgrades all occurrences of a letter from "found" (yellow) to player color.
        /// Called when all coordinates for this letter are now known.
        /// </summary>
        /// <param name="letter">The letter to upgrade</param>
        /// <param name="playerColor">The player's color</param>
        public void UpgradeLetterToPlayerColorInAllWords(char letter, Color playerColor)
        {
            letter = char.ToUpper(letter);
            for (int i = 0; i < _wordCount; i++)
            {
                string word = _wordRows[i].ActualWord;
                for (int j = 0; j < word.Length; j++)
                {
                    if (word[j] == letter)
                    {
                        _wordRows[i].UpgradeLetterToPlayerColor(j, playerColor);
                    }
                }
            }
        }

        /// <summary>
        /// Reveals a specific letter at a specific position in a word.
        /// </summary>
        /// <param name="wordIndex">The word row index</param>
        /// <param name="letterIndex">The position in the word</param>
        /// <param name="playerColor">The color to highlight</param>
        public void RevealLetterAt(int wordIndex, int letterIndex, Color playerColor)
        {
            if (wordIndex < 0 || wordIndex >= _wordCount) return;
            _wordRows[wordIndex].RevealLetter(letterIndex, playerColor);
        }

        /// <summary>
        /// Checks if all words have been fully revealed (game won).
        /// </summary>
        public bool AreAllWordsRevealed()
        {
            for (int i = 0; i < _wordCount; i++)
            {
                if (!_wordRows[i].IsFullyRevealed())
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Gets a snapshot of which word rows are currently fully revealed.
        /// Used to detect newly completed words after a guess.
        /// </summary>
        /// <returns>Array of booleans indicating which words are fully revealed</returns>
        public bool[] GetRevealedSnapshot()
        {
            bool[] snapshot = new bool[_wordCount];
            for (int i = 0; i < _wordCount; i++)
            {
                snapshot[i] = _wordRows[i].IsFullyRevealed();
            }
            return snapshot;
        }

        /// <summary>
        /// Compares current state to a previous snapshot and returns indices of newly completed words.
        /// </summary>
        /// <param name="previousSnapshot">Snapshot from before the guess</param>
        /// <returns>List of word indices that are now complete but weren't before</returns>
        public System.Collections.Generic.List<int> GetNewlyCompletedWords(bool[] previousSnapshot)
        {
            System.Collections.Generic.List<int> newlyCompleted = new System.Collections.Generic.List<int>();

            if (previousSnapshot == null || previousSnapshot.Length != _wordCount)
            {
                // No valid snapshot - return empty list
                return newlyCompleted;
            }

            for (int i = 0; i < _wordCount; i++)
            {
                bool wasComplete = previousSnapshot[i];
                bool isComplete = _wordRows[i].IsFullyRevealed();

                if (!wasComplete && isComplete)
                {
                    newlyCompleted.Add(i);
                }
            }

            return newlyCompleted;
        }

        /// <summary>
        /// Gets the actual word for a row (for gameplay validation).
        /// </summary>
        public string GetActualWord(int rowIndex)
        {
            if (rowIndex < 0 || rowIndex >= _wordCount) return "";
            return _wordRows[rowIndex].ActualWord;
        }

        /// <summary>
        /// Gets all actual words (for gameplay validation).
        /// </summary>
        public string[] GetAllActualWords()
        {
            string[] words = new string[_wordCount];
            for (int i = 0; i < _wordCount; i++)
            {
                words[i] = _wordRows[i].ActualWord;
            }
            return words;
        }

        /// <summary>
        /// Gets the revealed word (with underscores for unrevealed letters).
        /// </summary>
        public string GetRevealedWord(int rowIndex)
        {
            if (rowIndex < 0 || rowIndex >= _wordCount) return "";
            return _wordRows[rowIndex].GetDisplayedWord();
        }

        /// <summary>
        /// Reveals all letters in a specific word row (when word is guessed correctly).
        /// </summary>
        public void RevealAllLettersInWord(int wordIndex, Color playerColor)
        {
            if (wordIndex < 0 || wordIndex >= _wordCount) return;
            _wordRows[wordIndex].RevealAllLetters(playerColor);
        }

        /// <summary>
        /// Hides the guess button for a specific word row (when word is solved).
        /// </summary>
        public void HideGuessButton(int wordIndex)
        {
            if (wordIndex < 0 || wordIndex >= _wordCount) return;
            _wordRows[wordIndex].HideGuessButton();
        }

        #endregion

        #region Word Guess Mode Keyboard Support

        /// <summary>
        /// Whether any row is currently in word guess mode.
        /// </summary>
        public bool IsInWordGuessMode => _wordGuessRowIndex >= 0;

        /// <summary>
        /// Gets the index of the row currently in word guess mode, or -1 if none.
        /// </summary>
        public int WordGuessRowIndex => _wordGuessRowIndex;

        /// <summary>
        /// Types a letter into the active word guess row.
        /// </summary>
        public bool TypeLetterInGuessMode(char letter)
        {
            if (_wordGuessRowIndex < 0 || _wordGuessRowIndex >= _wordCount) return false;
            return _wordRows[_wordGuessRowIndex].TypeLetter(letter);
        }

        /// <summary>
        /// Exits the current word guess mode.
        /// </summary>
        public void ExitWordGuessMode()
        {
            if (_wordGuessRowIndex >= 0 && _wordGuessRowIndex < _wordCount)
            {
                _wordRows[_wordGuessRowIndex].ExitWordGuessMode();
                _wordGuessRowIndex = -1;
            }
        }

        /// <summary>
        /// Marks a letter position as revealed for a specific word.
        /// </summary>
        public void SetLetterRevealed(int wordIndex, int letterIndex, bool revealed)
        {
            if (wordIndex < 0 || wordIndex >= _wordCount) return;
            _wordRows[wordIndex].SetPositionRevealed(letterIndex, revealed);
        }

        /// <summary>
        /// Marks all positions of a letter as revealed across all words.
        /// </summary>
        public void SetLetterRevealedInAllWords(char letter, bool revealed)
        {
            for (int wordIdx = 0; wordIdx < _wordCount; wordIdx++)
            {
                string word = _wordRows[wordIdx].ActualWord;
                for (int i = 0; i < word.Length; i++)
                {
                    if (char.ToUpper(word[i]) == char.ToUpper(letter))
                    {
                        _wordRows[wordIdx].SetPositionRevealed(i, revealed);
                    }
                }
            }
        }

        #endregion
    }
}

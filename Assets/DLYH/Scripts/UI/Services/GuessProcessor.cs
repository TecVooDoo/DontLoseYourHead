using UnityEngine;
using System;
using System.Collections.Generic;

namespace TecVooDoo.DontLoseYourHead.UI
{
    /// <summary>
    /// Generic guess processor that handles letter, coordinate, and word guesses.
    /// Can be configured for either player (guessing against opponent) or opponent (guessing against player).
    /// </summary>
    public class GuessProcessor
    {
        #region Enums

        /// <summary>
        /// Result of a guess attempt - used to determine if turn should end
        /// </summary>
        public enum GuessResult
        {
            Hit,            // Valid guess that hit
            Miss,           // Valid guess that missed
            AlreadyGuessed, // Duplicate guess - don't end turn
            InvalidWord     // Word not in dictionary - don't end turn
        }

        #endregion

        #region Dependencies

        // Target data (the words we're guessing against)
        private readonly List<WordPlacementData> _targetWords;
        private readonly PlayerGridPanel _targetPanel;
        private readonly string _processorName;

        // Callbacks for external operations
        private readonly Action _onMissIncrement;
        private readonly Action<char, LetterButton.LetterState> _setLetterState;
        private readonly Func<string, bool> _validateWord;
        private readonly Action<string, bool> _addToGuessedWordList;

        #endregion

        #region State

        private int _misses;
        private int _missLimit;
        private HashSet<char> _knownLetters;
        private HashSet<char> _guessedLetters;
        private HashSet<Vector2Int> _guessedCoordinates;
        private HashSet<string> _guessedWords;
        private HashSet<int> _solvedWordRows;

        #endregion

        #region Properties

        public int Misses => _misses;
        public int MissLimit => _missLimit;
        public HashSet<char> KnownLetters => _knownLetters;
        public HashSet<int> SolvedWordRows => _solvedWordRows;

        #endregion

        #region Constructor

        /// <summary>
        /// Creates a new GuessProcessor configured for a specific target.
        /// </summary>
        /// <param name="targetWords">The words to guess against</param>
        /// <param name="targetPanel">The panel to update when guesses are made</param>
        /// <param name="processorName">Name for debug logging (e.g., "Player" or "Opponent")</param>
        /// <param name="onMissIncrement">Callback when miss count changes</param>
        /// <param name="setLetterState">Callback to update letter tracker state</param>
        /// <param name="validateWord">Callback to validate words against word bank</param>
        /// <param name="addToGuessedWordList">Callback to add word to guessed list UI</param>
        public GuessProcessor(
            List<WordPlacementData> targetWords,
            PlayerGridPanel targetPanel,
            string processorName,
            Action onMissIncrement,
            Action<char, LetterButton.LetterState> setLetterState,
            Func<string, bool> validateWord,
            Action<string, bool> addToGuessedWordList)
        {
            _targetWords = targetWords;
            _targetPanel = targetPanel;
            _processorName = processorName;
            _onMissIncrement = onMissIncrement;
            _setLetterState = setLetterState;
            _validateWord = validateWord;
            _addToGuessedWordList = addToGuessedWordList;

            // Initialize state collections
            _knownLetters = new HashSet<char>();
            _guessedLetters = new HashSet<char>();
            _guessedCoordinates = new HashSet<Vector2Int>();
            _guessedWords = new HashSet<string>();
            _solvedWordRows = new HashSet<int>();
        }

        #endregion

        #region Initialization

        /// <summary>
        /// Initialize/reset processor state for a new game
        /// </summary>
        public void Initialize(int missLimit)
        {
            _misses = 0;
            _missLimit = missLimit;
            _knownLetters.Clear();
            _guessedLetters.Clear();
            _guessedCoordinates.Clear();
            _guessedWords.Clear();
            _solvedWordRows.Clear();

            Debug.Log(string.Format("[GuessProcessor:{0}] Initialized with miss limit: {1}",
                _processorName, _missLimit));
        }

        #endregion

        #region Letter Guessing

        /// <summary>
        /// Process a letter guess against the target words
        /// </summary>
        public GuessResult ProcessLetterGuess(char letter)
        {
            letter = char.ToUpper(letter);

            // Check for duplicate guess
            if (_guessedLetters.Contains(letter))
            {
                Debug.LogWarning(string.Format("[GuessProcessor:{0}] Already guessed letter '{1}'!",
                    _processorName, letter));
                return GuessResult.AlreadyGuessed;
            }

            // Mark as guessed
            _guessedLetters.Add(letter);

            // Check if letter exists in any target word
            bool foundLetter = CheckLetterInWords(letter);

            if (foundLetter)
            {
                // Add to known letters
                _knownLetters.Add(letter);

                // Update target panel - reveal letter in word pattern rows
                UpdatePanelForLetter(letter);

                // Upgrade any yellow cells to green
                UpgradeGridCellsForLetter(letter);

                // Mark letter button as HIT (green)
                _setLetterState?.Invoke(letter, LetterButton.LetterState.Hit);

                return GuessResult.Hit;
            }
            else
            {
                // Miss - increment counter
                IncrementMisses(1);

                // Mark letter button as MISS (red)
                _setLetterState?.Invoke(letter, LetterButton.LetterState.Miss);

                return GuessResult.Miss;
            }
        }

        /// <summary>
        /// Check if a letter exists in any of the target words
        /// </summary>
        private bool CheckLetterInWords(char letter)
        {
            foreach (WordPlacementData word in _targetWords)
            {
                if (word.Word.ToUpper().Contains(letter))
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Update word pattern rows when a letter is discovered
        /// </summary>
        private void UpdatePanelForLetter(char letter)
        {
            WordPatternRow[] rows = _targetPanel.GetWordPatternRows();
            if (rows == null) return;

            for (int i = 0; i < _targetWords.Count && i < rows.Length; i++)
            {
                WordPlacementData wordData = _targetWords[i];
                WordPatternRow row = rows[i];

                if (row != null && wordData.Word.ToUpper().Contains(letter))
                {
                    int revealed = row.RevealAllInstancesOfLetter(letter);
                    if (revealed > 0)
                    {
                        Debug.Log(string.Format("[GuessProcessor:{0}] Revealed {1} instance(s) of '{2}' in word row {3}",
                            _processorName, revealed, letter, i + 1));
                    }
                }
            }
        }

        /// <summary>
        /// Upgrade yellow cells to green when a letter is discovered
        /// </summary>
        private void UpgradeGridCellsForLetter(char letter)
        {
            letter = char.ToUpper(letter);

            foreach (WordPlacementData word in _targetWords)
            {
                int col = word.StartCol;
                int row = word.StartRow;

                for (int i = 0; i < word.Word.Length; i++)
                {
                    if (char.ToUpper(word.Word[i]) == letter)
                    {
                        GridCellUI cell = _targetPanel.GetCell(col, row);
                        if (cell != null && cell.IsHitButLetterUnknown)
                        {
                            cell.UpgradeToKnownHit();
                            cell.RevealHiddenLetter();
                            Debug.Log(string.Format("[GuessProcessor:{0}] Upgraded cell ({1},{2}) to green for letter '{3}'",
                                _processorName, col, row, letter));
                        }
                    }
                    col += word.DirCol;
                    row += word.DirRow;
                }
            }
        }

        #endregion

        #region Coordinate Guessing

        /// <summary>
        /// Process a coordinate guess against the target grid
        /// </summary>
        public GuessResult ProcessCoordinateGuess(int col, int row)
        {
            Vector2Int coord = new Vector2Int(col, row);

            // Check for duplicate guess
            if (_guessedCoordinates.Contains(coord))
            {
                Debug.LogWarning(string.Format("[GuessProcessor:{0}] Already guessed coordinate ({1}, {2})!",
                    _processorName, col, row));
                return GuessResult.AlreadyGuessed;
            }

            // Mark as guessed
            _guessedCoordinates.Add(coord);

            // Check if coordinate hits any letter
            char? hitLetter = FindLetterAtCoordinate(col, row);

            if (hitLetter.HasValue)
            {
                GridCellUI cell = _targetPanel.GetCell(col, row);
                if (cell != null)
                {
                    char upperLetter = char.ToUpper(hitLetter.Value);
                    if (_knownLetters.Contains(upperLetter))
                    {
                        // Letter is known - mark green and reveal
                        cell.MarkAsGuessed(true);
                        cell.RevealHiddenLetter();
                    }
                    else
                    {
                        // Letter NOT known yet - mark yellow
                        cell.MarkAsHitButLetterUnknown();
                    }
                }
                return GuessResult.Hit;
            }
            else
            {
                // Miss - mark cell red and increment counter
                GridCellUI cell = _targetPanel.GetCell(col, row);
                if (cell != null)
                {
                    cell.MarkAsGuessed(false);
                }

                IncrementMisses(1);
                return GuessResult.Miss;
            }
        }

        /// <summary>
        /// Find which letter (if any) is at the given coordinate
        /// </summary>
        private char? FindLetterAtCoordinate(int col, int row)
        {
            foreach (WordPlacementData word in _targetWords)
            {
                int checkCol = word.StartCol;
                int checkRow = word.StartRow;

                for (int i = 0; i < word.Word.Length; i++)
                {
                    if (checkCol == col && checkRow == row)
                    {
                        return word.Word[i];
                    }
                    checkCol += word.DirCol;
                    checkRow += word.DirRow;
                }
            }
            return null;
        }

        #endregion

        #region Word Guessing

        /// <summary>
        /// Process a word guess against the target
        /// </summary>
        public GuessResult ProcessWordGuess(string guessedWord, int rowIndex)
        {
            string normalizedGuess = guessedWord.Trim().ToUpper();

            Debug.Log(string.Format("[GuessProcessor:{0}] ProcessWordGuess: word='{1}', rowIndex={2}",
                _processorName, normalizedGuess, rowIndex));

            // Validate against word bank
            if (_validateWord != null && !_validateWord(normalizedGuess))
            {
                Debug.LogWarning(string.Format("[GuessProcessor:{0}] '{1}' is not a valid word!",
                    _processorName, normalizedGuess));
                return GuessResult.InvalidWord;
            }

            // Check for duplicate guess
            if (_guessedWords.Contains(normalizedGuess))
            {
                Debug.LogWarning(string.Format("[GuessProcessor:{0}] Already guessed word '{1}'!",
                    _processorName, normalizedGuess));
                return GuessResult.AlreadyGuessed;
            }

            // Mark as guessed
            _guessedWords.Add(normalizedGuess);

            // Check if word matches the target word at this row index
            WordPlacementData targetWord = null;
            if (rowIndex < _targetWords.Count)
            {
                targetWord = _targetWords[rowIndex];
                Debug.Log(string.Format("[GuessProcessor:{0}] Target word at index {1}: '{2}'",
                    _processorName, rowIndex, targetWord.Word));
            }

            if (targetWord != null && targetWord.Word.ToUpper() == normalizedGuess)
            {
                return HandleCorrectWordGuess(targetWord, normalizedGuess, rowIndex);
            }
            else
            {
                return HandleIncorrectWordGuess(normalizedGuess);
            }
        }

        /// <summary>
        /// Handle a correct word guess
        /// </summary>
        private GuessResult HandleCorrectWordGuess(WordPlacementData targetWord, string normalizedGuess, int rowIndex)
        {
            // Add all letters to known
            foreach (char c in targetWord.Word.ToUpper())
            {
                _knownLetters.Add(c);
                _guessedLetters.Add(c);
            }

            // Track solved row
            _solvedWordRows.Add(rowIndex);
            Debug.Log(string.Format("[GuessProcessor:{0}] SOLVED: Added rowIndex {1} to solved rows.",
                _processorName, rowIndex));

            // Update word pattern row - reveal all letters and mark solved
            WordPatternRow row = _targetPanel.GetWordPatternRow(rowIndex);
            if (row != null)
            {
                row.RevealAllLetters();
                row.MarkWordSolved();
            }

            // Update OTHER word pattern rows and upgrade cells for discovered letters
            foreach (char c in targetWord.Word.ToUpper())
            {
                UpdatePanelForLetter(c);
                _setLetterState?.Invoke(c, LetterButton.LetterState.Hit);
                UpgradeGridCellsForLetter(c);
            }

            // Add to guessed word list (correct)
            _addToGuessedWordList?.Invoke(normalizedGuess, true);

            return GuessResult.Hit;
        }

        /// <summary>
        /// Handle an incorrect word guess
        /// </summary>
        private GuessResult HandleIncorrectWordGuess(string normalizedGuess)
        {
            // Wrong guess - double penalty
            IncrementMisses(2);

            // Add to guessed word list (wrong)
            _addToGuessedWordList?.Invoke(normalizedGuess, false);

            return GuessResult.Miss;
        }

        #endregion

        #region Miss Management

        /// <summary>
        /// Increment miss count and notify callback
        /// </summary>
        private void IncrementMisses(int amount)
        {
            _misses += amount;
            _onMissIncrement?.Invoke();
        }

        /// <summary>
        /// Check if the guesser has exceeded their miss limit
        /// </summary>
        public bool HasExceededMissLimit()
        {
            return _misses >= _missLimit;
        }

        #endregion
    }

    #region Data Structure

    /// <summary>
    /// Data structure for a placed word with position and direction.
    /// Moved here to be accessible by GuessProcessor.
    /// </summary>
    public class WordPlacementData
    {
        public string Word;
        public int StartCol;
        public int StartRow;
        public int DirCol;
        public int DirRow;
        public int RowIndex;
    }

    #endregion
}

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace TecVooDoo.DontLoseYourHead.UI
{
    /// <summary>
    /// Manages word guess mode state machine, keyboard mode switching,
    /// and keyboard input handling during word guessing.
    /// Extracted from GameplayUIController.
    /// </summary>
    public class WordGuessModeController
    {
        #region Events

        /// <summary>Raised when a word guess is processed (after validation)</summary>
        public event Action<int, string, bool> OnWordGuessProcessed;

        /// <summary>Raised when feedback should be displayed</summary>
        public event Action<string> OnFeedbackRequested;

        /// <summary>Raised when player turn should end</summary>
        public event Action OnTurnEnded;

        #endregion

        #region Dependencies

        private readonly PlayerGridPanel _panel;
        private readonly Func<string, int, WordGuessResult> _processWordGuess;
        private readonly Func<bool> _canStartGuess;
        private readonly Func<int, bool> _isRowSolved;
        private readonly Action<int> _markRowSolved;

        #endregion

        #region State

        private WordPatternRow _activeWordGuessRow = null;
        private Dictionary<char, LetterButton.LetterState> _savedLetterStates = new Dictionary<char, LetterButton.LetterState>();
        private bool _letterTrackerInKeyboardMode = false;

        #endregion

        #region Public Properties

        public bool IsInKeyboardMode => _letterTrackerInKeyboardMode;
        public WordPatternRow ActiveRow => _activeWordGuessRow;

        #endregion

        #region Constructor

        /// <summary>
        /// Create a new WordGuessModeController
        /// </summary>
        /// <param name="panel">The opponent panel to manage</param>
        /// <param name="processWordGuess">Callback to process word guess, returns result</param>
        /// <param name="canStartGuess">Callback to check if guessing is allowed (turn/game state)</param>
        /// <param name="isRowSolved">Callback to check if a row is already solved</param>
        /// <param name="markRowSolved">Callback to mark a row as solved</param>
        public WordGuessModeController(
            PlayerGridPanel panel,
            Func<string, int, WordGuessResult> processWordGuess,
            Func<bool> canStartGuess,
            Func<int, bool> isRowSolved,
            Action<int> markRowSolved)
        {
            _panel = panel;
            _processWordGuess = processWordGuess;
            _canStartGuess = canStartGuess;
            _isRowSolved = isRowSolved;
            _markRowSolved = markRowSolved;
        }

        #endregion

        #region Word Guess Event Handlers

        /// <summary>
        /// Handle when a word guess is started from a row
        /// </summary>
        public void HandleWordGuessStarted(int rowNumber)
        {
            int rowIndex = rowNumber - 1;

            if (!_canStartGuess())
            {
                Debug.LogWarning("[WordGuessMode] Cannot start word guess - not allowed!");
                WordPatternRow[] rows = _panel.GetWordPatternRows();
                if (rows != null && rowIndex < rows.Length && rows[rowIndex] != null)
                {
                    rows[rowIndex].ExitWordGuessMode(false);
                }
                return;
            }

            if (_activeWordGuessRow != null)
            {
                _activeWordGuessRow.ExitWordGuessMode(false);
            }

            WordPatternRow[] allRows = _panel.GetWordPatternRows();
            if (allRows != null && rowIndex < allRows.Length)
            {
                _activeWordGuessRow = allRows[rowIndex];
            }

            SwitchLetterTrackerToKeyboardMode();

            Debug.Log(string.Format("[WordGuessMode] Word guess mode started for row {0}", rowIndex + 1));

            if (allRows != null)
            {
                for (int i = 0; i < allRows.Length; i++)
                {
                    if (allRows[i] != null && i != rowIndex)
                    {
                        allRows[i].HideGuessWordButton();
                    }
                }
            }
        }

        /// <summary>
        /// Handle when a word guess is submitted
        /// </summary>
        public void HandleWordGuessSubmitted(int rowNumber, string guessedWord)
        {
            int rowIndex = rowNumber - 1;

            Debug.Log(string.Format("[WordGuessMode] === HandleWordGuessSubmitted START: rowNumber={0}, rowIndex={1}, word='{2}' ===",
                rowNumber, rowIndex, guessedWord));

            RestoreLetterTrackerFromKeyboardMode();
            _activeWordGuessRow = null;

            WordGuessResult result = _processWordGuess(guessedWord, rowIndex);
            Debug.Log(string.Format("[WordGuessMode] ProcessWordGuess returned: {0}", result));

            switch (result)
            {
                case WordGuessResult.InvalidWord:
                    OnFeedbackRequested?.Invoke("Not a valid word - try again!");
                    WordPatternRow[] rows = _panel.GetWordPatternRows();
                    if (rows != null && rowIndex < rows.Length && rows[rowIndex] != null)
                    {
                        rows[rowIndex].EnterWordGuessMode();
                    }
                    return;

                case WordGuessResult.AlreadyGuessed:
                    OnFeedbackRequested?.Invoke("Already guessed that word!");
                    return;

                case WordGuessResult.Hit:
                    OnFeedbackRequested?.Invoke("Correct!");
                    _markRowSolved(rowIndex);
                    Debug.Log(string.Format("[WordGuessMode] Player guessed word '{0}': CORRECT!", guessedWord));
                    break;

                case WordGuessResult.Miss:
                    OnFeedbackRequested?.Invoke("Wrong! (+2 misses)");
                    Debug.Log(string.Format("[WordGuessMode] Player guessed word '{0}': WRONG (+2 misses)", guessedWord));
                    break;
            }

            OnWordGuessProcessed?.Invoke(rowIndex, guessedWord, result == WordGuessResult.Hit);
            OnTurnEnded?.Invoke();
            ShowAllGuessWordButtons();

            Debug.Log("[WordGuessMode] === HandleWordGuessSubmitted END ===");
        }

        /// <summary>
        /// Handle when a word guess is cancelled
        /// </summary>
        public void HandleWordGuessCancelled(int rowNumber)
        {
            int rowIndex = rowNumber - 1;

            Debug.Log(string.Format("[WordGuessMode] Word guess cancelled for row {0}", rowNumber));

            RestoreLetterTrackerFromKeyboardMode();
            _activeWordGuessRow = null;
            ShowAllGuessWordButtons();
        }

        /// <summary>
        /// Show guess word buttons on all unsolved rows
        /// </summary>
        public void ShowAllGuessWordButtons()
        {
            WordPatternRow[] allRows = _panel.GetWordPatternRows();
            if (allRows == null) return;

            for (int i = 0; i < allRows.Length; i++)
            {
                if (allRows[i] == null) continue;

                if (_isRowSolved(i))
                {
                    Debug.Log(string.Format("[WordGuessMode] Row {0} is SOLVED - hiding button", i));
                    allRows[i].HideGuessWordButton();
                }
                else
                {
                    Debug.Log(string.Format("[WordGuessMode] Row {0} is NOT solved - showing button", i));
                    allRows[i].ShowGuessWordButton();
                }
            }
        }

        /// <summary>
        /// Exit word guess mode if active
        /// </summary>
        public void ExitWordGuessMode()
        {
            if (_activeWordGuessRow != null)
            {
                _activeWordGuessRow.ExitWordGuessMode(false);
                _activeWordGuessRow = null;
            }

            if (_letterTrackerInKeyboardMode)
            {
                RestoreLetterTrackerFromKeyboardMode();
            }
        }

        #endregion

        #region Keyboard Input

        /// <summary>
        /// Handle keyboard letter input during word guess mode
        /// </summary>
        public void HandleKeyboardLetterInput(char letter)
        {
            if (_activeWordGuessRow == null)
            {
                Debug.LogWarning("[WordGuessMode] No active word guess row for keyboard input!");
                return;
            }

            letter = char.ToUpper(letter);
            bool success = _activeWordGuessRow.TypeGuessLetter(letter);

            if (success)
            {
                Debug.Log(string.Format("[WordGuessMode] Typed '{0}' in word guess row", letter));
            }
        }

        /// <summary>
        /// Process keyboard input during Update(). Returns true if input was handled.
        /// </summary>
        public bool ProcessKeyboardInput()
        {
            if (!_letterTrackerInKeyboardMode || _activeWordGuessRow == null)
                return false;

            Keyboard keyboard = Keyboard.current;
            if (keyboard == null) return false;

            for (int i = 0; i < 26; i++)
            {
                Key key = Key.A + i;
                if (keyboard[key].wasPressedThisFrame)
                {
                    char letter = (char)('A' + i);
                    HandleKeyboardLetterInput(letter);
                    return true;
                }
            }

            if (keyboard.backspaceKey.wasPressedThisFrame)
            {
                _activeWordGuessRow.BackspaceGuessLetter();
                return true;
            }

            if (keyboard.enterKey.wasPressedThisFrame || keyboard.numpadEnterKey.wasPressedThisFrame)
            {
                if (_activeWordGuessRow.IsGuessComplete())
                {
                    _activeWordGuessRow.ExitWordGuessMode(true);
                }
                return true;
            }

            if (keyboard.escapeKey.wasPressedThisFrame)
            {
                _activeWordGuessRow.ExitWordGuessMode(false);
                return true;
            }

            return false;
        }

        #endregion

        #region Letter Tracker Keyboard Mode

        private void SwitchLetterTrackerToKeyboardMode()
        {
            if (_panel == null) return;

            _savedLetterStates.Clear();

            for (char c = 'A'; c <= 'Z'; c++)
            {
                LetterButton.LetterState state = _panel.GetLetterState(c);
                _savedLetterStates[c] = state;
                _panel.SetLetterState(c, LetterButton.LetterState.Normal);
            }

            _letterTrackerInKeyboardMode = true;
            Debug.Log("[WordGuessMode] Letter tracker switched to keyboard mode");
        }

        private void RestoreLetterTrackerFromKeyboardMode()
        {
            if (_panel == null || !_letterTrackerInKeyboardMode) return;

            foreach (KeyValuePair<char, LetterButton.LetterState> kvp in _savedLetterStates)
            {
                _panel.SetLetterState(kvp.Key, kvp.Value);
            }

            _savedLetterStates.Clear();
            _letterTrackerInKeyboardMode = false;
            Debug.Log("[WordGuessMode] Letter tracker restored from keyboard mode");
        }

        #endregion
    }

    /// <summary>
    /// Result of a word guess attempt
    /// </summary>
    public enum WordGuessResult
    {
        Hit,
        Miss,
        AlreadyGuessed,
        InvalidWord
    }
}

using System;

namespace TecVooDoo.DontLoseYourHead.UI.Controllers
{
    /// <summary>
    /// Manages word guess input state machine for WordPatternRow.
    /// Handles cursor position, typed letters, and input logic.
    /// </summary>
    public class WordGuessInputController
    {
        #region Events
        /// <summary>Fired when guess mode is entered</summary>
        public event Action OnGuessStarted;

        /// <summary>Fired when guess is submitted. Parameter: guessed word</summary>
        public event Action<string> OnGuessSubmitted;

        /// <summary>Fired when guess mode is cancelled</summary>
        public event Action OnGuessCancelled;

        /// <summary>Fired when display needs updating</summary>
        public event Action OnDisplayUpdateNeeded;
        #endregion

        #region Private Fields
        private readonly Func<int, bool> _isLetterRevealed;
        private readonly Func<int, char> _getRevealedLetter;
        private readonly Func<string> _getCurrentWord;

        private bool _isActive;
        private char[] _guessedLetters;
        private int _cursorPosition;
        private int _wordLength;
        #endregion

        #region Properties
        /// <summary>Whether word guess mode is currently active</summary>
        public bool IsActive => _isActive;

        /// <summary>Current cursor position</summary>
        public int CursorPosition => _cursorPosition;

        /// <summary>Access to guessed letters array for display</summary>
        public char[] GuessedLetters => _guessedLetters;
        #endregion

        #region Constructor
        /// <summary>
        /// Creates a new WordGuessInputController.
        /// </summary>
        /// <param name="isLetterRevealed">Callback to check if position is revealed</param>
        /// <param name="getRevealedLetter">Callback to get revealed letter at position</param>
        /// <param name="getCurrentWord">Callback to get the current hidden word</param>
        public WordGuessInputController(
            Func<int, bool> isLetterRevealed,
            Func<int, char> getRevealedLetter,
            Func<string> getCurrentWord)
        {
            _isLetterRevealed = isLetterRevealed;
            _getRevealedLetter = getRevealedLetter;
            _getCurrentWord = getCurrentWord;
            _isActive = false;
            _cursorPosition = 0;
        }
        #endregion

        #region Public Methods - Initialization
        /// <summary>
        /// Initializes the controller for a specific word length.
        /// Must be called before using guess mode.
        /// </summary>
        public void Initialize(int wordLength)
        {
            _wordLength = wordLength;
            _guessedLetters = new char[wordLength];
            _cursorPosition = 0;
            _isActive = false;
        }

        /// <summary>
        /// Updates the word length and resets guessed letters array.
        /// </summary>
        public void SetWordLength(int wordLength)
        {
            _wordLength = wordLength;
            _guessedLetters = new char[wordLength];
            ClearGuessedLetters();
        }
        #endregion

        #region Public Methods - Mode Control
        /// <summary>
        /// Enters word guess mode. Returns false if cannot enter.
        /// </summary>
        public bool Enter()
        {
            // Check if we can enter (need valid word length)
            string currentWord = _getCurrentWord();
            if (string.IsNullOrEmpty(currentWord)) return false;

            // Ensure guessed letters array matches current word length
            // This handles cases where the row's word changed or we're re-entering
            if (_guessedLetters == null || _guessedLetters.Length != currentWord.Length)
            {
                _guessedLetters = new char[currentWord.Length];
                _wordLength = currentWord.Length;
            }

            // Always clear guessed letters when entering (even if re-entering after invalid word)
            ClearGuessedLetters();
            _cursorPosition = FindNextUnrevealedPosition(-1);

            // Only fire events if not already active (avoid double-firing)
            bool wasActive = _isActive;
            _isActive = true;

            OnDisplayUpdateNeeded?.Invoke();

            if (!wasActive)
            {
                OnGuessStarted?.Invoke();
            }

            return true;
        }

        /// <summary>
        /// Exits word guess mode.
        /// </summary>
        /// <param name="submit">If true, submits the guess before exiting</param>
        public void Exit(bool submit)
        {
            if (!_isActive) return;

            // IMPORTANT: Set _isActive = false BEFORE firing events
            // This ensures InWordGuessMode returns false when ShowAllGuessWordButtons()
            // is called from the event handler chain
            _isActive = false;

            if (submit)
            {
                string guessedWord = GetFullGuessWord();
                OnGuessSubmitted?.Invoke(guessedWord);
            }
            else
            {
                OnGuessCancelled?.Invoke();
            }

            ClearGuessedLetters();
            _cursorPosition = 0;

            OnDisplayUpdateNeeded?.Invoke();
        }
        #endregion

        #region Public Methods - Input
        /// <summary>
        /// Types a letter into the current cursor position.
        /// Auto-advances to next unrevealed position.
        /// </summary>
        /// <returns>True if letter was typed</returns>
        public bool TypeLetter(char letter)
        {
            if (!_isActive) return false;
            if (_cursorPosition < 0 || _cursorPosition >= _guessedLetters.Length) return false;

            // Can only type into unrevealed positions
            if (_isLetterRevealed(_cursorPosition))
            {
                // Move to next unrevealed position
                _cursorPosition = FindNextUnrevealedPosition(_cursorPosition);
                if (_cursorPosition < 0) return false;
            }

            _guessedLetters[_cursorPosition] = char.ToUpper(letter);

            // Auto-advance to next unrevealed position
            _cursorPosition = FindNextUnrevealedPosition(_cursorPosition);

            OnDisplayUpdateNeeded?.Invoke();
            return true;
        }

        /// <summary>
        /// Handles backspace in guess mode.
        /// First click: clears letter at current position (stays there)
        /// Second click (if position empty): moves back and clears that letter
        /// </summary>
        /// <returns>True if backspace was handled</returns>
        public bool Backspace()
        {
            if (!_isActive) return false;

            // If cursor is past end, move back to last unrevealed position
            if (_cursorPosition < 0)
            {
                _cursorPosition = FindPreviousUnrevealedPosition(_guessedLetters.Length);
                if (_cursorPosition < 0) return false;
            }

            // Check if current position has a typed letter
            if (_cursorPosition >= 0 && _cursorPosition < _guessedLetters.Length)
            {
                if (_guessedLetters[_cursorPosition] != '\0')
                {
                    // Clear letter at current position, stay there
                    _guessedLetters[_cursorPosition] = '\0';
                    OnDisplayUpdateNeeded?.Invoke();
                    return true;
                }
                else
                {
                    // Current position empty, move back to previous unrevealed position and clear
                    int prevPos = FindPreviousUnrevealedPosition(_cursorPosition);
                    if (prevPos >= 0 && _guessedLetters[prevPos] != '\0')
                    {
                        _guessedLetters[prevPos] = '\0';
                        _cursorPosition = prevPos;
                        OnDisplayUpdateNeeded?.Invoke();
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Gets the character that was guessed at a specific position.
        /// </summary>
        public char GetGuessedLetterAt(int index)
        {
            if (_guessedLetters == null || index < 0 || index >= _guessedLetters.Length)
            {
                return '\0';
            }
            return _guessedLetters[index];
        }
        #endregion

        #region Public Methods - Query
        /// <summary>
        /// Gets the full guessed word (revealed letters + typed letters).
        /// </summary>
        public string GetFullGuessWord()
        {
            string currentWord = _getCurrentWord();
            if (string.IsNullOrEmpty(currentWord)) return string.Empty;

            char[] result = new char[currentWord.Length];

            for (int i = 0; i < currentWord.Length; i++)
            {
                if (_isLetterRevealed(i))
                {
                    result[i] = _getRevealedLetter(i);
                }
                else if (_guessedLetters != null && i < _guessedLetters.Length && _guessedLetters[i] != '\0')
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
        public bool IsGuessComplete()
        {
            string currentWord = _getCurrentWord();
            if (string.IsNullOrEmpty(currentWord)) return false;

            for (int i = 0; i < currentWord.Length; i++)
            {
                if (!_isLetterRevealed(i) && 
                    (_guessedLetters == null || i >= _guessedLetters.Length || _guessedLetters[i] == '\0'))
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Clears all guessed letters.
        /// </summary>
        public void ClearGuessedLetters()
        {
            if (_guessedLetters == null) return;
            for (int i = 0; i < _guessedLetters.Length; i++)
            {
                _guessedLetters[i] = '\0';
            }
        }
        #endregion

        #region Private Methods - Navigation
        private int FindNextUnrevealedPosition(int fromPosition)
        {
            for (int i = fromPosition + 1; i < _wordLength; i++)
            {
                if (!_isLetterRevealed(i))
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
                if (!_isLetterRevealed(i))
                {
                    return i;
                }
            }
            return -1; // No previous unrevealed positions
        }
        #endregion
    }
}

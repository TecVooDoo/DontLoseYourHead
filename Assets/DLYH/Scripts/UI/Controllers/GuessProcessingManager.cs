using UnityEngine;
using System;
using System.Collections.Generic;

namespace TecVooDoo.DontLoseYourHead.UI
{
    /// <summary>
    /// Manages guess processing for both player and opponent.
    /// Handles letter, coordinate, and word guesses against the target grids.
    /// Extracted from GameplayUIController to reduce file size.
    /// </summary>
    public class GuessProcessingManager : IDisposable
    {
        #region Enums

        /// <summary>
        /// Result of a guess operation.
        /// </summary>
        public enum GuessResult
        {
            Hit,
            Miss,
            AlreadyGuessed,
            InvalidWord
        }

        #endregion

        #region Private Fields

        private GuessProcessor _playerGuessProcessor;
        private GuessProcessor _opponentGuessProcessor;
        private GameplayStateTracker _stateTracker;

        // References for guess processing
        private PlayerGridPanel _ownerPanel;
        private PlayerGridPanel _opponentPanel;
        private SetupData _playerSetupData;
        private SetupData _opponentSetupData;

        // Word validation callback
        private Func<string, bool> _isValidWord;

        // Miss counter update callbacks
        private Action _updatePlayerMissCounter;
        private Action _updateOpponentMissCounter;

        // Guessed word list callbacks
        private Action<string, bool> _addToPlayerGuessedList;
        private Action<string, bool> _addToOpponentGuessedList;

        #endregion

        #region Constructor

        public GuessProcessingManager(GameplayStateTracker stateTracker)
        {
            _stateTracker = stateTracker;
        }

        #endregion

        #region Initialization

        /// <summary>
        /// Set the panel references needed for guess processing.
        /// </summary>
        public void SetPanels(PlayerGridPanel ownerPanel, PlayerGridPanel opponentPanel)
        {
            _ownerPanel = ownerPanel;
            _opponentPanel = opponentPanel;
        }

        /// <summary>
        /// Set the setup data for both player and opponent.
        /// </summary>
        public void SetSetupData(SetupData playerSetupData, SetupData opponentSetupData)
        {
            _playerSetupData = playerSetupData;
            _opponentSetupData = opponentSetupData;
        }

        /// <summary>
        /// Set callbacks for guess processing.
        /// </summary>
        public void SetCallbacks(
            Func<string, bool> isValidWord,
            Action updatePlayerMissCounter,
            Action updateOpponentMissCounter,
            Action<string, bool> addToPlayerGuessedList,
            Action<string, bool> addToOpponentGuessedList)
        {
            _isValidWord = isValidWord;
            _updatePlayerMissCounter = updatePlayerMissCounter;
            _updateOpponentMissCounter = updateOpponentMissCounter;
            _addToPlayerGuessedList = addToPlayerGuessedList;
            _addToOpponentGuessedList = addToOpponentGuessedList;
        }

        /// <summary>
        /// Initialize the guess processors for both player and opponent.
        /// Call this after setting panels, setup data, and callbacks.
        /// </summary>
        public void Initialize()
        {
            if (_opponentPanel == null || _ownerPanel == null)
            {
                Debug.LogError("[GuessProcessingManager] Panels not set!");
                return;
            }

            if (_playerSetupData == null || _opponentSetupData == null)
            {
                Debug.LogError("[GuessProcessingManager] Setup data not set!");
                return;
            }

            // Set hit colors on guessed word lists (if callbacks are set)
            // Player1 list shows player's guesses - use player's color
            // Player2 list shows opponent's guesses - use opponent's color

            // Create player's processor (guesses against opponent's data)
            _playerGuessProcessor = new GuessProcessor(
                _opponentSetupData.PlacedWords.ConvertAll(w => new WordPlacementData
                {
                    Word = w.Word,
                    StartCol = w.StartCol,
                    StartRow = w.StartRow,
                    DirCol = w.DirCol,
                    DirRow = w.DirRow,
                    RowIndex = w.RowIndex
                }),
                _opponentPanel,
                "Player",
                (amount) => { _stateTracker.AddPlayerMisses(amount); _updatePlayerMissCounter?.Invoke(); },
                (letter, state) => _opponentPanel.SetLetterState(letter, state),
                word => _isValidWord?.Invoke(word) ?? false,
                (word, correct) => _addToPlayerGuessedList?.Invoke(word, correct)
            );
            _playerGuessProcessor.Initialize(_stateTracker.PlayerMissLimit);

            // Create opponent's processor (guesses against player's data)
            _opponentGuessProcessor = new GuessProcessor(
                _playerSetupData.PlacedWords.ConvertAll(w => new WordPlacementData
                {
                    Word = w.Word,
                    StartCol = w.StartCol,
                    StartRow = w.StartRow,
                    DirCol = w.DirCol,
                    DirRow = w.DirRow,
                    RowIndex = w.RowIndex
                }),
                _ownerPanel,
                "Opponent",
                (amount) => { _stateTracker.AddOpponentMisses(amount); _updateOpponentMissCounter?.Invoke(); },
                (letter, state) => _ownerPanel.SetLetterState(letter, state),
                word => _isValidWord?.Invoke(word) ?? false,
                (word, correct) => _addToOpponentGuessedList?.Invoke(word, correct)
            );
            _opponentGuessProcessor.Initialize(_stateTracker.OpponentMissLimit);

            Debug.Log("[GuessProcessingManager] Guess processors initialized");
        }

        #endregion

        #region Player Guess Processing

        /// <summary>
        /// Process player guessing a letter against opponent's words.
        /// </summary>
        public GuessResult ProcessPlayerLetterGuess(char letter)
        {
            GuessProcessor.GuessResult result = _playerGuessProcessor.ProcessLetterGuess(letter);

            if (result == GuessProcessor.GuessResult.Hit)
            {
                _stateTracker.AddPlayerKnownLetter(letter);
            }
            _stateTracker.AddPlayerGuessedLetter(letter);

            return ConvertGuessResult(result);
        }

        /// <summary>
        /// Process player guessing a coordinate on opponent's grid.
        /// </summary>
        public GuessResult ProcessPlayerCoordinateGuess(int col, int row)
        {
            GuessProcessor.GuessResult result = _playerGuessProcessor.ProcessCoordinateGuess(col, row);

            _stateTracker.AddPlayerGuessedCoordinate(col, row);

            // NOTE: Do NOT add letters to known letters here!
            // Coordinate hits for unknown letters result in yellow cells.
            // Letters only become known through letter guessing or correct word guessing.
            // The GuessProcessor handles green vs yellow cell display based on _knownLetters.

            return ConvertGuessResult(result);
        }

        /// <summary>
        /// Process player guessing a complete word.
        /// NOTE: Correct word guess reveals LETTERS but NOT grid positions.
        /// Grid positions must be guessed via coordinate guessing for win condition.
        /// </summary>
        public GuessResult ProcessPlayerWordGuess(string word, int rowIndex)
        {
            GuessProcessor.GuessResult result = _playerGuessProcessor.ProcessWordGuess(word, rowIndex);

            // Track solved rows via state tracker for UI button management
            if (result == GuessProcessor.GuessResult.Hit)
            {
                _stateTracker.AddPlayerSolvedRow(rowIndex);

                // When a word is correctly guessed, add all its letters as known
                // NOTE: Do NOT add coordinates - those must be guessed via coordinate guessing
                if (rowIndex < _opponentSetupData.PlacedWords.Count)
                {
                    WordPlacementData wordData = _opponentSetupData.PlacedWords[rowIndex];

                    // Add all letters as known
                    foreach (char c in wordData.Word.ToUpper())
                    {
                        _stateTracker.AddPlayerKnownLetter(c);
                        _stateTracker.AddPlayerGuessedLetter(c);
                    }
                }
            }

            return ConvertGuessResult(result);
        }

        #endregion

        #region Opponent Guess Processing

        /// <summary>
        /// Process opponent guessing a letter against player's words.
        /// </summary>
        public GuessResult ProcessOpponentLetterGuess(char letter)
        {
            GuessProcessor.GuessResult result = _opponentGuessProcessor.ProcessLetterGuess(letter);

            if (result == GuessProcessor.GuessResult.Hit)
            {
                _stateTracker.AddOpponentKnownLetter(letter);
            }
            _stateTracker.AddOpponentGuessedLetter(letter);

            return ConvertGuessResult(result);
        }

        /// <summary>
        /// Process opponent guessing a coordinate on player's grid.
        /// </summary>
        public GuessResult ProcessOpponentCoordinateGuess(int col, int row)
        {
            GuessProcessor.GuessResult result = _opponentGuessProcessor.ProcessCoordinateGuess(col, row);

            _stateTracker.AddOpponentGuessedCoordinate(col, row);

            // If hit, find the letter at this coordinate and add to known letters
            if (result == GuessProcessor.GuessResult.Hit)
            {
                char? letter = FindLetterAtCoordinate(_playerSetupData.PlacedWords, col, row);
                if (letter.HasValue)
                {
                    _stateTracker.AddOpponentKnownLetter(char.ToUpper(letter.Value));
                }
            }

            return ConvertGuessResult(result);
        }

        /// <summary>
        /// Process opponent guessing a complete word.
        /// NOTE: Correct word guess reveals LETTERS but NOT grid positions.
        /// Grid positions must be guessed via coordinate guessing for win condition.
        /// </summary>
        public GuessResult ProcessOpponentWordGuess(string word, int rowIndex)
        {
            GuessProcessor.GuessResult result = _opponentGuessProcessor.ProcessWordGuess(word, rowIndex);

            // Track solved rows and update known letters for win condition
            if (result == GuessProcessor.GuessResult.Hit)
            {
                _stateTracker.AddOpponentSolvedRow(rowIndex);

                // When a word is correctly guessed, add all its letters as known
                // NOTE: Do NOT add coordinates - those must be guessed via coordinate guessing
                if (rowIndex < _playerSetupData.PlacedWords.Count)
                {
                    WordPlacementData wordData = _playerSetupData.PlacedWords[rowIndex];

                    // Add all letters as known
                    foreach (char c in wordData.Word.ToUpper())
                    {
                        _stateTracker.AddOpponentKnownLetter(c);
                        _stateTracker.AddOpponentGuessedLetter(c);
                    }
                }
            }

            return ConvertGuessResult(result);
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Convert GuessProcessor.GuessResult to local GuessResult enum.
        /// </summary>
        private GuessResult ConvertGuessResult(GuessProcessor.GuessResult result)
        {
            switch (result)
            {
                case GuessProcessor.GuessResult.Hit:
                    return GuessResult.Hit;
                case GuessProcessor.GuessResult.Miss:
                    return GuessResult.Miss;
                case GuessProcessor.GuessResult.AlreadyGuessed:
                    return GuessResult.AlreadyGuessed;
                case GuessProcessor.GuessResult.InvalidWord:
                    return GuessResult.InvalidWord;
                default:
                    return GuessResult.Miss;
            }
        }

        /// <summary>
        /// Find the letter at a given coordinate in the word placements.
        /// </summary>
        private char? FindLetterAtCoordinate(List<WordPlacementData> words, int col, int row)
        {
            foreach (WordPlacementData word in words)
            {
                for (int i = 0; i < word.Word.Length; i++)
                {
                    int wordCol = word.StartCol + (i * word.DirCol);
                    int wordRow = word.StartRow + (i * word.DirRow);
                    if (wordCol == col && wordRow == row)
                    {
                        return word.Word[i];
                    }
                }
            }
            return null;
        }

        #endregion

        #region IDisposable

        public void Dispose()
        {
            _playerGuessProcessor = null;
            _opponentGuessProcessor = null;
            _stateTracker = null;
        }

        #endregion
    }
}

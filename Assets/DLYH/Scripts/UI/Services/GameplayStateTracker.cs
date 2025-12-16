using UnityEngine;
using System;
using System.Collections.Generic;
using TecVooDoo.DontLoseYourHead.Core;

namespace TecVooDoo.DontLoseYourHead.UI
{
    /// <summary>
    /// Tracks gameplay state for both player and opponent including:
    /// - Miss counts and limits
    /// - Known/guessed letters
    /// - Guessed coordinates
    /// - Guessed words
    /// - Solved word rows
    /// Extracted from GameplayUIController to reduce file size.
    /// </summary>
    public class GameplayStateTracker
    {
        #region Player State

        private int _playerMisses;
        private int _playerMissLimit;
        private HashSet<char> _playerKnownLetters = new HashSet<char>();
        private HashSet<char> _playerGuessedLetters = new HashSet<char>();
        private HashSet<Vector2Int> _playerGuessedCoordinates = new HashSet<Vector2Int>();
        private HashSet<string> _playerGuessedWords = new HashSet<string>();
        private HashSet<int> _playerSolvedWordRows = new HashSet<int>();

        #endregion

        #region Opponent State

        private int _opponentMisses;
        private int _opponentMissLimit;
        private HashSet<char> _opponentKnownLetters = new HashSet<char>();
        private HashSet<char> _opponentGuessedLetters = new HashSet<char>();
        private HashSet<Vector2Int> _opponentGuessedCoordinates = new HashSet<Vector2Int>();
        private HashSet<string> _opponentGuessedWords = new HashSet<string>();
        private HashSet<int> _opponentSolvedWordRows = new HashSet<int>();

        #endregion

        #region Turn/Game State

        private bool _isPlayerTurn = true;
        private bool _gameOver = false;

        #endregion

        #region Properties - Player

        public int PlayerMisses => _playerMisses;
        public int PlayerMissLimit => _playerMissLimit;
        public HashSet<char> PlayerKnownLetters => _playerKnownLetters;
        public HashSet<char> PlayerGuessedLetters => _playerGuessedLetters;
        public HashSet<Vector2Int> PlayerGuessedCoordinates => _playerGuessedCoordinates;
        public HashSet<string> PlayerGuessedWords => _playerGuessedWords;
        public HashSet<int> PlayerSolvedWordRows => _playerSolvedWordRows;

        #endregion

        #region Properties - Opponent

        public int OpponentMisses => _opponentMisses;
        public int OpponentMissLimit => _opponentMissLimit;
        public HashSet<char> OpponentKnownLetters => _opponentKnownLetters;
        public HashSet<char> OpponentGuessedLetters => _opponentGuessedLetters;
        public HashSet<Vector2Int> OpponentGuessedCoordinates => _opponentGuessedCoordinates;
        public HashSet<string> OpponentGuessedWords => _opponentGuessedWords;
        public HashSet<int> OpponentSolvedWordRows => _opponentSolvedWordRows;

        #endregion

        #region Properties - Turn/Game

        public bool IsPlayerTurn
        {
            get => _isPlayerTurn;
            set => _isPlayerTurn = value;
        }

        public bool GameOver
        {
            get => _gameOver;
            set => _gameOver = value;
        }

        #endregion

        #region Initialization

        /// <summary>
        /// Initialize player state for a new game
        /// </summary>
        public void InitializePlayerState(int missLimit)
        {
            _playerMisses = 0;
            _playerMissLimit = missLimit;
            _playerKnownLetters.Clear();
            _playerGuessedLetters.Clear();
            _playerGuessedCoordinates.Clear();
            _playerGuessedWords.Clear();
            _playerSolvedWordRows.Clear();
            _isPlayerTurn = true;
            _gameOver = false;
        }

        /// <summary>
        /// Initialize opponent state for a new game
        /// </summary>
        public void InitializeOpponentState(int missLimit)
        {
            _opponentMisses = 0;
            _opponentMissLimit = missLimit;
            _opponentKnownLetters.Clear();
            _opponentGuessedLetters.Clear();
            _opponentGuessedCoordinates.Clear();
            _opponentGuessedWords.Clear();
            _opponentSolvedWordRows.Clear();
        }

        #endregion

        #region Miss Management

        /// <summary>
        /// Add misses to player count
        /// </summary>
        public void AddPlayerMisses(int amount)
        {
            _playerMisses += amount;
        }

        /// <summary>
        /// Add misses to opponent count
        /// </summary>
        public void AddOpponentMisses(int amount)
        {
            _opponentMisses += amount;
        }

        /// <summary>
        /// Check if player has exceeded miss limit
        /// </summary>
        public bool HasPlayerExceededMissLimit()
        {
            return _playerMisses >= _playerMissLimit;
        }

        /// <summary>
        /// Check if opponent has exceeded miss limit
        /// </summary>
        public bool HasOpponentExceededMissLimit()
        {
            return _opponentMisses >= _opponentMissLimit;
        }

        #endregion

        #region Miss Limit Calculation

        /// <summary>
        /// Calculate miss limit based on opponent's grid and difficulty
        /// </summary>
        public static int CalculateMissLimit(DifficultySetting playerDifficulty, int opponentGridSize, int opponentWordCount)
        {
            int baseMisses = 15;
            int gridBonus = GetGridBonus(opponentGridSize);
            int wordModifier = opponentWordCount == 4 ? -2 : 0;
            int difficultyModifier = GetDifficultyModifier(playerDifficulty);

            return baseMisses + gridBonus + wordModifier + difficultyModifier;
        }

        private static int GetGridBonus(int gridSize)
        {
            switch (gridSize)
            {
                case 6: return 3;
                case 7: return 4;
                case 8: return 6;
                case 9: return 8;
                case 10: return 10;
                case 11: return 12;
                case 12: return 13;
                default: return 6;
            }
        }

        private static int GetDifficultyModifier(DifficultySetting difficulty)
        {
            switch (difficulty)
            {
                case DifficultySetting.Easy: return 4;
                case DifficultySetting.Normal: return 0;
                case DifficultySetting.Hard: return -4;
                default: return 0;
            }
        }

        #endregion

        #region State Recording

        /// <summary>
        /// Record that player knows a letter
        /// </summary>
        public void AddPlayerKnownLetter(char letter)
        {
            _playerKnownLetters.Add(char.ToUpper(letter));
        }

        /// <summary>
        /// Record that player guessed a letter
        /// </summary>
        public void AddPlayerGuessedLetter(char letter)
        {
            _playerGuessedLetters.Add(char.ToUpper(letter));
        }

        /// <summary>
        /// Record that player guessed a coordinate
        /// </summary>
        public void AddPlayerGuessedCoordinate(int col, int row)
        {
            _playerGuessedCoordinates.Add(new Vector2Int(col, row));
        }

        /// <summary>
        /// Record that player solved a word row
        /// </summary>
        public void AddPlayerSolvedRow(int rowIndex)
        {
            _playerSolvedWordRows.Add(rowIndex);
        }

        /// <summary>
        /// Record that opponent knows a letter
        /// </summary>
        public void AddOpponentKnownLetter(char letter)
        {
            _opponentKnownLetters.Add(char.ToUpper(letter));
        }

        /// <summary>
        /// Record that opponent guessed a letter
        /// </summary>
        public void AddOpponentGuessedLetter(char letter)
        {
            _opponentGuessedLetters.Add(char.ToUpper(letter));
        }

        /// <summary>
        /// Record that opponent guessed a coordinate
        /// </summary>
        public void AddOpponentGuessedCoordinate(int col, int row)
        {
            _opponentGuessedCoordinates.Add(new Vector2Int(col, row));
        }

        /// <summary>
        /// Record that opponent guessed a word
        /// </summary>
        public void AddOpponentGuessedWord(string word)
        {
            _opponentGuessedWords.Add(word.ToUpper());
        }

        /// <summary>
        /// Record that opponent solved a word row
        /// </summary>
        public void AddOpponentSolvedRow(int rowIndex)
        {
            _opponentSolvedWordRows.Add(rowIndex);
        }

        #endregion

        #region Formatted Output

        /// <summary>
        /// Get formatted miss counter text for player
        /// </summary>
        public string GetPlayerMissCounterText()
        {
            return string.Format("{0} / {1}", _playerMisses, _playerMissLimit);
        }

        /// <summary>
        /// Get formatted miss counter text for opponent
        /// </summary>
        public string GetOpponentMissCounterText()
        {
            return string.Format("{0} / {1}", _opponentMisses, _opponentMissLimit);
        }

        #endregion
    }
}

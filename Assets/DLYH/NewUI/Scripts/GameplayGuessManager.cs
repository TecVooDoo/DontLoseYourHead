using System;
using System.Collections.Generic;
using UnityEngine;

namespace DLYH.TableUI
{
    /// <summary>
    /// Result of a guess attempt.
    /// </summary>
    public enum GuessResult
    {
        Hit,            // Letter/coordinate found in opponent's words
        Miss,           // Letter/coordinate not in opponent's words
        AlreadyGuessed, // Already tried this letter/coordinate
        Invalid         // Invalid input
    }

    /// <summary>
    /// Tracks the state of guesses for one player's attack on opponent's grid.
    /// </summary>
    public class GuessState
    {
        public HashSet<char> GuessedLetters { get; } = new HashSet<char>();
        public HashSet<Vector2Int> GuessedCoordinates { get; } = new HashSet<Vector2Int>();
        public HashSet<char> HitLetters { get; } = new HashSet<char>();
        public int MissCount { get; set; }
        public int MissLimit { get; set; }
    }

    /// <summary>
    /// Manages guess processing for the new UI Toolkit gameplay.
    /// Works directly with TableModel and PlacementAdapter data.
    /// </summary>
    public class GameplayGuessManager
    {
        #region Events

        /// <summary>Fired when a letter guess results in a hit. Parameters: letter, positions revealed.</summary>
        public event Action<char, List<Vector2Int>> OnLetterHit;

        /// <summary>Fired when a letter guess results in a miss. Parameters: letter.</summary>
        public event Action<char> OnLetterMiss;

        /// <summary>Fired when a coordinate guess results in a hit. Parameters: position, letter revealed.</summary>
        public event Action<Vector2Int, char> OnCoordinateHit;

        /// <summary>Fired when a coordinate guess results in a miss. Parameters: position.</summary>
        public event Action<Vector2Int> OnCoordinateMiss;

        /// <summary>Fired when miss count changes. Parameters: isPlayer, newMissCount, missLimit.</summary>
        public event Action<bool, int, int> OnMissCountChanged;

        /// <summary>Fired when a player loses (reaches miss limit). Parameters: isPlayer (true = player lost).</summary>
        public event Action<bool> OnGameOver;

        #endregion

        #region Private Fields

        // Opponent's word data (what the player is guessing against)
        private IReadOnlyDictionary<Vector2Int, char> _opponentPlacedLetters;
        private IReadOnlyCollection<Vector2Int> _opponentPlacedPositions;

        // Player's word data (what the opponent/AI is guessing against)
        private IReadOnlyDictionary<Vector2Int, char> _playerPlacedLetters;
        private IReadOnlyCollection<Vector2Int> _playerPlacedPositions;

        // Guess state for each side
        private GuessState _playerGuessState;   // Player's guesses against opponent
        private GuessState _opponentGuessState; // Opponent's guesses against player

        private bool _isInitialized;

        #endregion

        #region Initialization

        /// <summary>
        /// Initializes the guess manager with word placement data from both sides.
        /// </summary>
        /// <param name="playerPlacedLetters">Player's placed letters (opponent attacks this)</param>
        /// <param name="playerPlacedPositions">Player's placed positions</param>
        /// <param name="opponentPlacedLetters">Opponent's placed letters (player attacks this)</param>
        /// <param name="opponentPlacedPositions">Opponent's placed positions</param>
        /// <param name="playerMissLimit">Player's miss limit</param>
        /// <param name="opponentMissLimit">Opponent's miss limit</param>
        public void Initialize(
            IReadOnlyDictionary<Vector2Int, char> playerPlacedLetters,
            IReadOnlyCollection<Vector2Int> playerPlacedPositions,
            IReadOnlyDictionary<Vector2Int, char> opponentPlacedLetters,
            IReadOnlyCollection<Vector2Int> opponentPlacedPositions,
            int playerMissLimit,
            int opponentMissLimit)
        {
            _playerPlacedLetters = playerPlacedLetters;
            _playerPlacedPositions = playerPlacedPositions;
            _opponentPlacedLetters = opponentPlacedLetters;
            _opponentPlacedPositions = opponentPlacedPositions;

            _playerGuessState = new GuessState { MissLimit = playerMissLimit };
            _opponentGuessState = new GuessState { MissLimit = opponentMissLimit };

            _isInitialized = true;

            Debug.Log($"[GameplayGuessManager] Initialized - Player limit: {playerMissLimit}, Opponent limit: {opponentMissLimit}");
        }

        /// <summary>
        /// Simplified initialization for testing - uses same data for both sides.
        /// </summary>
        public void InitializeForTesting(
            IReadOnlyDictionary<Vector2Int, char> placedLetters,
            IReadOnlyCollection<Vector2Int> placedPositions,
            int missLimit)
        {
            Initialize(placedLetters, placedPositions, placedLetters, placedPositions, missLimit, missLimit);
        }

        #endregion

        #region Letter Guessing

        /// <summary>
        /// Process a letter guess from the player against the opponent's words.
        /// </summary>
        public GuessResult ProcessPlayerLetterGuess(char letter)
        {
            return ProcessLetterGuess(letter, true);
        }

        /// <summary>
        /// Process a letter guess from the opponent against the player's words.
        /// </summary>
        public GuessResult ProcessOpponentLetterGuess(char letter)
        {
            return ProcessLetterGuess(letter, false);
        }

        private GuessResult ProcessLetterGuess(char letter, bool isPlayerGuessing)
        {
            if (!_isInitialized)
            {
                Debug.LogWarning("[GameplayGuessManager] Not initialized");
                return GuessResult.Invalid;
            }

            letter = char.ToUpper(letter);
            if (!char.IsLetter(letter))
            {
                return GuessResult.Invalid;
            }

            // Get the appropriate state and target data
            GuessState guessState = isPlayerGuessing ? _playerGuessState : _opponentGuessState;
            IReadOnlyDictionary<Vector2Int, char> targetLetters = isPlayerGuessing ? _opponentPlacedLetters : _playerPlacedLetters;
            IReadOnlyCollection<Vector2Int> targetPositions = isPlayerGuessing ? _opponentPlacedPositions : _playerPlacedPositions;

            // Check if already guessed
            if (guessState.GuessedLetters.Contains(letter))
            {
                Debug.Log($"[GameplayGuessManager] Letter '{letter}' already guessed");
                return GuessResult.AlreadyGuessed;
            }

            // Mark as guessed
            guessState.GuessedLetters.Add(letter);

            // Search for the letter in target's placed words
            List<Vector2Int> hitPositions = new List<Vector2Int>();
            foreach (var kvp in targetLetters)
            {
                if (kvp.Value == letter)
                {
                    hitPositions.Add(kvp.Key);
                }
            }

            if (hitPositions.Count > 0)
            {
                // HIT
                guessState.HitLetters.Add(letter);
                Debug.Log($"[GameplayGuessManager] HIT! Letter '{letter}' found at {hitPositions.Count} position(s)");

                if (isPlayerGuessing)
                {
                    OnLetterHit?.Invoke(letter, hitPositions);
                }

                return GuessResult.Hit;
            }
            else
            {
                // MISS
                guessState.MissCount++;
                Debug.Log($"[GameplayGuessManager] MISS! Letter '{letter}' not found. Miss count: {guessState.MissCount}/{guessState.MissLimit}");

                if (isPlayerGuessing)
                {
                    OnLetterMiss?.Invoke(letter);
                }

                OnMissCountChanged?.Invoke(isPlayerGuessing, guessState.MissCount, guessState.MissLimit);

                // Check for game over
                if (guessState.MissCount >= guessState.MissLimit)
                {
                    Debug.Log($"[GameplayGuessManager] GAME OVER - {(isPlayerGuessing ? "Player" : "Opponent")} reached miss limit!");
                    OnGameOver?.Invoke(isPlayerGuessing);
                }

                return GuessResult.Miss;
            }
        }

        #endregion

        #region Coordinate Guessing

        /// <summary>
        /// Process a coordinate guess from the player against the opponent's grid.
        /// </summary>
        public GuessResult ProcessPlayerCoordinateGuess(int col, int row)
        {
            return ProcessCoordinateGuess(col, row, true);
        }

        /// <summary>
        /// Process a coordinate guess from the opponent against the player's grid.
        /// </summary>
        public GuessResult ProcessOpponentCoordinateGuess(int col, int row)
        {
            return ProcessCoordinateGuess(col, row, false);
        }

        private GuessResult ProcessCoordinateGuess(int col, int row, bool isPlayerGuessing)
        {
            if (!_isInitialized)
            {
                Debug.LogWarning("[GameplayGuessManager] Not initialized");
                return GuessResult.Invalid;
            }

            Vector2Int position = new Vector2Int(col, row);

            // Get the appropriate state and target data
            GuessState guessState = isPlayerGuessing ? _playerGuessState : _opponentGuessState;
            IReadOnlyDictionary<Vector2Int, char> targetLetters = isPlayerGuessing ? _opponentPlacedLetters : _playerPlacedLetters;
            IReadOnlyCollection<Vector2Int> targetPositions = isPlayerGuessing ? _opponentPlacedPositions : _playerPlacedPositions;

            // Check if already guessed this coordinate
            if (guessState.GuessedCoordinates.Contains(position))
            {
                Debug.Log($"[GameplayGuessManager] Coordinate ({col}, {row}) already guessed");
                return GuessResult.AlreadyGuessed;
            }

            // Mark as guessed
            guessState.GuessedCoordinates.Add(position);

            // Check if there's a letter at this position
            if (targetLetters.TryGetValue(position, out char letter))
            {
                // HIT
                Debug.Log($"[GameplayGuessManager] HIT! Letter '{letter}' at ({col}, {row})");

                if (isPlayerGuessing)
                {
                    OnCoordinateHit?.Invoke(position, letter);
                }

                return GuessResult.Hit;
            }
            else
            {
                // MISS
                guessState.MissCount++;
                Debug.Log($"[GameplayGuessManager] MISS! No letter at ({col}, {row}). Miss count: {guessState.MissCount}/{guessState.MissLimit}");

                if (isPlayerGuessing)
                {
                    OnCoordinateMiss?.Invoke(position);
                }

                OnMissCountChanged?.Invoke(isPlayerGuessing, guessState.MissCount, guessState.MissLimit);

                // Check for game over
                if (guessState.MissCount >= guessState.MissLimit)
                {
                    Debug.Log($"[GameplayGuessManager] GAME OVER - {(isPlayerGuessing ? "Player" : "Opponent")} reached miss limit!");
                    OnGameOver?.Invoke(isPlayerGuessing);
                }

                return GuessResult.Miss;
            }
        }

        #endregion

        #region State Queries

        /// <summary>
        /// Gets the player's current miss count (player guessing against opponent).
        /// </summary>
        public int GetPlayerMissCount() => _playerGuessState?.MissCount ?? 0;

        /// <summary>
        /// Gets the opponent's current miss count (opponent guessing against player).
        /// </summary>
        public int GetOpponentMissCount() => _opponentGuessState?.MissCount ?? 0;

        /// <summary>
        /// Gets the player's miss limit.
        /// </summary>
        public int GetPlayerMissLimit() => _playerGuessState?.MissLimit ?? 0;

        /// <summary>
        /// Gets the opponent's miss limit.
        /// </summary>
        public int GetOpponentMissLimit() => _opponentGuessState?.MissLimit ?? 0;

        /// <summary>
        /// Checks if a letter has been guessed by the player.
        /// </summary>
        public bool HasPlayerGuessedLetter(char letter) => _playerGuessState?.GuessedLetters.Contains(char.ToUpper(letter)) ?? false;

        /// <summary>
        /// Checks if a letter was a hit for the player.
        /// </summary>
        public bool IsPlayerLetterHit(char letter) => _playerGuessState?.HitLetters.Contains(char.ToUpper(letter)) ?? false;

        /// <summary>
        /// Checks if a coordinate has been guessed by the player.
        /// </summary>
        public bool HasPlayerGuessedCoordinate(int col, int row) =>
            _playerGuessState?.GuessedCoordinates.Contains(new Vector2Int(col, row)) ?? false;

        /// <summary>
        /// Gets all positions where a specific letter appears in the opponent's words.
        /// Used to check if all instances of a letter have been found on the grid.
        /// </summary>
        public List<Vector2Int> GetOpponentLetterPositions(char letter)
        {
            List<Vector2Int> positions = new List<Vector2Int>();
            if (_opponentPlacedLetters == null) return positions;

            letter = char.ToUpper(letter);
            foreach (KeyValuePair<Vector2Int, char> kvp in _opponentPlacedLetters)
            {
                if (kvp.Value == letter)
                {
                    positions.Add(kvp.Key);
                }
            }
            return positions;
        }

        #endregion
    }
}

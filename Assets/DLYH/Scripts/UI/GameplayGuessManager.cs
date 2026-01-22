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

        /// <summary>Fired when a word guess is processed. Parameters: wordIndex, guessedWord, wasCorrect.</summary>
        public event Action<int, string, bool> OnWordGuessProcessed;

        /// <summary>Fired when a word is completely solved (all letters revealed). Parameters: wordIndex.</summary>
        public event Action<int> OnWordSolved;

        #endregion

        #region Private Fields

        // Opponent's word data (what the player is guessing against)
        private IReadOnlyDictionary<Vector2Int, char> _opponentPlacedLetters;
        private IReadOnlyCollection<Vector2Int> _opponentPlacedPositions;
        private List<string> _opponentWords; // Opponent's actual words (for word guessing)
        private HashSet<int> _opponentSolvedWordIndices; // Which opponent words have been guessed

        // Player's word data (what the opponent/AI is guessing against)
        private IReadOnlyDictionary<Vector2Int, char> _playerPlacedLetters;
        private IReadOnlyCollection<Vector2Int> _playerPlacedPositions;

        // Guess state for each side
        private GuessState _playerGuessState;   // Player's guesses against opponent
        private GuessState _opponentGuessState; // Opponent's guesses against player

        // Word guess tracking
        private HashSet<string> _playerGuessedWords; // Words the player has guessed
        private HashSet<string> _opponentGuessedWords; // Words the opponent has guessed
        private Func<string, bool> _validateWord; // Word validation callback

        private bool _isInitialized;

        // Pooled list to avoid per-guess allocations
        private readonly List<Vector2Int> _hitPositionsPool = new List<Vector2Int>(16);

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
        /// <param name="opponentWords">Optional list of opponent's actual words for word guessing</param>
        /// <param name="validateWord">Optional callback to validate word against dictionary</param>
        public void Initialize(
            IReadOnlyDictionary<Vector2Int, char> playerPlacedLetters,
            IReadOnlyCollection<Vector2Int> playerPlacedPositions,
            IReadOnlyDictionary<Vector2Int, char> opponentPlacedLetters,
            IReadOnlyCollection<Vector2Int> opponentPlacedPositions,
            int playerMissLimit,
            int opponentMissLimit,
            List<string> opponentWords = null,
            Func<string, bool> validateWord = null)
        {
            _playerPlacedLetters = playerPlacedLetters;
            _playerPlacedPositions = playerPlacedPositions;
            _opponentPlacedLetters = opponentPlacedLetters;
            _opponentPlacedPositions = opponentPlacedPositions;
            _opponentWords = opponentWords ?? new List<string>();
            _validateWord = validateWord;

            _playerGuessState = new GuessState { MissLimit = playerMissLimit };
            _opponentGuessState = new GuessState { MissLimit = opponentMissLimit };

            _playerGuessedWords = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            _opponentGuessedWords = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            _opponentSolvedWordIndices = new HashSet<int>();

            _isInitialized = true;

            Debug.Log($"[GameplayGuessManager] Initialized - Player limit: {playerMissLimit}, Opponent limit: {opponentMissLimit}, Words: {_opponentWords.Count}");
            Debug.Log($"[GameplayGuessManager] Initialized - opponentPlacedLetters count: {_opponentPlacedLetters?.Count ?? -1}");
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

            // Search for the letter in target's placed words - reuse pooled list to avoid allocation
            _hitPositionsPool.Clear();
            foreach (KeyValuePair<Vector2Int, char> kvp in targetLetters)
            {
                if (kvp.Value == letter)
                {
                    _hitPositionsPool.Add(kvp.Key);
                }
            }

            if (_hitPositionsPool.Count > 0)
            {
                // HIT
                guessState.HitLetters.Add(letter);
                Debug.Log($"[GameplayGuessManager] HIT! Letter '{letter}' found at {_hitPositionsPool.Count} position(s)");

                if (isPlayerGuessing)
                {
                    // Note: Handler must use positions immediately - list is reused
                    OnLetterHit?.Invoke(letter, _hitPositionsPool);
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
                // HIT - coordinate revealed, letter hidden
                // NOTE: Do NOT add letter to HitLetters here! Coordinate guesses only reveal positions,
                // not letters. The keyboard tracker should only show letters guessed via keyboard.
                Debug.Log($"[GameplayGuessManager] HIT! Letter at ({col}, {row}) - coordinate revealed, letter hidden");

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

        #region Word Guessing

        /// <summary>
        /// Process a word guess from the player against the opponent's words.
        /// </summary>
        /// <param name="guessedWord">The word the player is guessing</param>
        /// <param name="wordIndex">The index of the word row being guessed</param>
        /// <returns>GuessResult indicating the outcome</returns>
        public GuessResult ProcessPlayerWordGuess(string guessedWord, int wordIndex)
        {
            if (!_isInitialized)
            {
                Debug.LogWarning("[GameplayGuessManager] Not initialized");
                return GuessResult.Invalid;
            }

            string normalizedGuess = guessedWord?.Trim().ToUpper() ?? "";
            if (string.IsNullOrEmpty(normalizedGuess))
            {
                return GuessResult.Invalid;
            }

            Debug.Log($"[GameplayGuessManager] Word guess: '{normalizedGuess}' for word index {wordIndex}");

            // Check if word is valid (in dictionary)
            if (_validateWord != null && !_validateWord(normalizedGuess))
            {
                Debug.Log($"[GameplayGuessManager] '{normalizedGuess}' is not a valid word");
                return GuessResult.Invalid;
            }

            // Check if already guessed this word
            if (_playerGuessedWords.Contains(normalizedGuess))
            {
                Debug.Log($"[GameplayGuessManager] Already guessed word '{normalizedGuess}'");
                return GuessResult.AlreadyGuessed;
            }

            // Mark as guessed
            _playerGuessedWords.Add(normalizedGuess);

            // Check if word matches the target word at this index
            if (wordIndex >= 0 && wordIndex < _opponentWords.Count)
            {
                string targetWord = _opponentWords[wordIndex].ToUpper();
                if (normalizedGuess == targetWord)
                {
                    // CORRECT!
                    Debug.Log($"[GameplayGuessManager] CORRECT! Word '{normalizedGuess}' matches target");
                    _opponentSolvedWordIndices.Add(wordIndex);

                    // Mark all letters in this word as known/hit
                    foreach (char c in targetWord)
                    {
                        _playerGuessState.GuessedLetters.Add(c);
                        _playerGuessState.HitLetters.Add(c);
                    }

                    OnWordGuessProcessed?.Invoke(wordIndex, normalizedGuess, true);
                    OnWordSolved?.Invoke(wordIndex);
                    return GuessResult.Hit;
                }
            }

            // WRONG - add 2 misses (word guess penalty)
            Debug.Log($"[GameplayGuessManager] WRONG! Word '{normalizedGuess}' does not match. +2 misses");
            _playerGuessState.MissCount += 2;

            OnMissCountChanged?.Invoke(true, _playerGuessState.MissCount, _playerGuessState.MissLimit);
            OnWordGuessProcessed?.Invoke(wordIndex, normalizedGuess, false);

            // Check for game over
            if (_playerGuessState.MissCount >= _playerGuessState.MissLimit)
            {
                Debug.Log("[GameplayGuessManager] GAME OVER - Player reached miss limit from word guess!");
                OnGameOver?.Invoke(true);
            }

            return GuessResult.Miss;
        }

        /// <summary>
        /// Checks if a word at the given index has been solved.
        /// </summary>
        public bool IsWordSolved(int wordIndex) => _opponentSolvedWordIndices?.Contains(wordIndex) ?? false;

        /// <summary>
        /// Gets the target word at the given index (for debugging/testing).
        /// </summary>
        public string GetOpponentWord(int wordIndex)
        {
            if (_opponentWords == null || wordIndex < 0 || wordIndex >= _opponentWords.Count)
                return null;
            return _opponentWords[wordIndex];
        }

        /// <summary>
        /// Checks if the opponent has already guessed a specific word.
        /// </summary>
        public bool HasOpponentGuessedWord(string word)
        {
            if (string.IsNullOrEmpty(word)) return false;
            return _opponentGuessedWords?.Contains(word.ToUpper()) ?? false;
        }

        /// <summary>
        /// Records that the opponent guessed a word (regardless of correctness).
        /// Returns true if this is a new guess, false if already guessed.
        /// </summary>
        public bool RecordOpponentWordGuess(string word)
        {
            if (string.IsNullOrEmpty(word)) return false;
            string normalized = word.Trim().ToUpper();

            if (_opponentGuessedWords == null)
            {
                _opponentGuessedWords = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            }

            if (_opponentGuessedWords.Contains(normalized))
            {
                Debug.Log($"[GameplayGuessManager] Opponent already guessed word '{normalized}'");
                return false;
            }

            _opponentGuessedWords.Add(normalized);
            Debug.Log($"[GameplayGuessManager] Recorded opponent word guess: '{normalized}'");
            return true;
        }

        /// <summary>
        /// Gets all words the opponent has guessed (for AI state building).
        /// </summary>
        public HashSet<string> GetOpponentGuessedWords()
        {
            return _opponentGuessedWords != null
                ? new HashSet<string>(_opponentGuessedWords, StringComparer.OrdinalIgnoreCase)
                : new HashSet<string>(StringComparer.OrdinalIgnoreCase);
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
        /// Adds misses to the opponent's count (e.g., for incorrect word guesses).
        /// </summary>
        public void AddOpponentMisses(int count)
        {
            if (_opponentGuessState == null) return;

            _opponentGuessState.MissCount += count;
            Debug.Log($"[GameplayGuessManager] Added {count} misses to opponent. Total: {_opponentGuessState.MissCount}/{_opponentGuessState.MissLimit}");

            OnMissCountChanged?.Invoke(false, _opponentGuessState.MissCount, _opponentGuessState.MissLimit);

            // Check for game over
            if (_opponentGuessState.MissCount >= _opponentGuessState.MissLimit)
            {
                Debug.Log("[GameplayGuessManager] GAME OVER - Opponent reached miss limit!");
                OnGameOver?.Invoke(false); // false = opponent lost
            }
        }

        /// <summary>
        /// Adds misses to the player's count (e.g., for incorrect word guesses).
        /// </summary>
        public void AddPlayerMisses(int count)
        {
            if (_playerGuessState == null) return;

            _playerGuessState.MissCount += count;
            Debug.Log($"[GameplayGuessManager] Added {count} misses to player. Total: {_playerGuessState.MissCount}/{_playerGuessState.MissLimit}");

            OnMissCountChanged?.Invoke(true, _playerGuessState.MissCount, _playerGuessState.MissLimit);

            // Check for game over
            if (_playerGuessState.MissCount >= _playerGuessState.MissLimit)
            {
                Debug.Log("[GameplayGuessManager] GAME OVER - Player reached miss limit!");
                OnGameOver?.Invoke(true); // true = player lost
            }
        }

        /// <summary>
        /// Sets the initial miss counts for a resumed game.
        /// Does NOT trigger game over checks - use only when restoring state.
        /// </summary>
        /// <param name="playerMisses">Player's miss count to restore</param>
        /// <param name="opponentMisses">Opponent's miss count to restore</param>
        public void SetInitialMissCounts(int playerMisses, int opponentMisses)
        {
            if (_playerGuessState != null)
            {
                _playerGuessState.MissCount = playerMisses;
            }

            if (_opponentGuessState != null)
            {
                _opponentGuessState.MissCount = opponentMisses;
            }
        }

        /// <summary>
        /// Restores opponent's guessed letters and coordinates from saved state.
        /// Call this when restoring a game to ensure opponent guesses are tracked.
        /// </summary>
        /// <param name="guessedLetters">Set of letters the opponent has guessed</param>
        /// <param name="revealedCoordinates">Dictionary of revealed coordinates (position -> (letter, isHit))</param>
        public void RestoreOpponentGuessState(HashSet<char> guessedLetters, Dictionary<Vector2Int, (char letter, bool isHit)> revealedCoordinates)
        {
            if (_opponentGuessState == null)
            {
                Debug.LogWarning("[GameplayGuessManager] Cannot restore opponent guess state - _opponentGuessState is null");
                return;
            }

            // Restore guessed letters
            if (guessedLetters != null)
            {
                foreach (char letter in guessedLetters)
                {
                    _opponentGuessState.GuessedLetters.Add(char.ToUpper(letter));

                    // Check if this letter is a hit (exists in player's words)
                    if (_playerPlacedLetters != null)
                    {
                        foreach (var kvp in _playerPlacedLetters)
                        {
                            if (kvp.Value == char.ToUpper(letter))
                            {
                                _opponentGuessState.HitLetters.Add(char.ToUpper(letter));
                                break;
                            }
                        }
                    }
                }
            }

            // Restore guessed coordinates
            if (revealedCoordinates != null)
            {
                foreach (var kvp in revealedCoordinates)
                {
                    _opponentGuessState.GuessedCoordinates.Add(kvp.Key);
                }
            }
        }

        /// <summary>
        /// Gets all letters the opponent has guessed (for AI state building).
        /// </summary>
        public HashSet<char> GetOpponentGuessedLetters() =>
            _opponentGuessState?.GuessedLetters != null
                ? new HashSet<char>(_opponentGuessState.GuessedLetters)
                : new HashSet<char>();

        /// <summary>
        /// Gets all letters that were hits for the opponent (for AI state building).
        /// </summary>
        public HashSet<char> GetOpponentHitLetters() =>
            _opponentGuessState?.HitLetters != null
                ? new HashSet<char>(_opponentGuessState.HitLetters)
                : new HashSet<char>();

        /// <summary>
        /// Gets all coordinates the opponent has guessed (for AI state building).
        /// </summary>
        public HashSet<(int row, int col)> GetOpponentGuessedCoordinatesAsTuples()
        {
            var result = new HashSet<(int row, int col)>();
            if (_opponentGuessState?.GuessedCoordinates != null)
            {
                foreach (var pos in _opponentGuessState.GuessedCoordinates)
                {
                    result.Add((pos.y, pos.x)); // Convert Vector2Int(col, row) to tuple(row, col)
                }
            }
            return result;
        }

        /// <summary>
        /// Gets all coordinates that were hits for the opponent (for AI state building).
        /// </summary>
        public HashSet<(int row, int col)> GetOpponentHitCoordinatesAsTuples()
        {
            var result = new HashSet<(int row, int col)>();
            if (_opponentGuessState?.GuessedCoordinates != null && _playerPlacedLetters != null)
            {
                foreach (var pos in _opponentGuessState.GuessedCoordinates)
                {
                    // Check if this coordinate has a letter (was a hit)
                    if (_playerPlacedLetters.ContainsKey(pos))
                    {
                        result.Add((pos.y, pos.x)); // Convert Vector2Int(col, row) to tuple(row, col)
                    }
                }
            }
            return result;
        }

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
        /// Checks if a letter has been guessed by the opponent.
        /// </summary>
        public bool HasOpponentGuessedLetter(char letter) => _opponentGuessState?.GuessedLetters.Contains(char.ToUpper(letter)) ?? false;

        /// <summary>
        /// Checks if a letter was a hit for the opponent.
        /// </summary>
        public bool IsOpponentLetterHit(char letter) => _opponentGuessState?.HitLetters.Contains(char.ToUpper(letter)) ?? false;

        /// <summary>
        /// Checks if a coordinate has been guessed by the opponent.
        /// </summary>
        public bool HasOpponentGuessedCoordinate(int col, int row) =>
            _opponentGuessState?.GuessedCoordinates.Contains(new Vector2Int(col, row)) ?? false;

        /// <summary>
        /// Gets the letter at a specific position in the player's grid.
        /// Returns null if no letter exists at that position.
        /// </summary>
        public char? GetPlayerLetterAtPosition(int col, int row)
        {
            if (_playerPlacedLetters == null) return null;

            Vector2Int pos = new Vector2Int(col, row);
            if (_playerPlacedLetters.TryGetValue(pos, out char letter))
            {
                return char.ToUpper(letter);
            }
            return null;
        }

        /// <summary>
        /// Gets all positions where a specific letter appears in the player's words.
        /// Used to check if all instances of a letter have been found by the opponent.
        /// </summary>
        public List<Vector2Int> GetPlayerLetterPositions(char letter)
        {
            List<Vector2Int> positions = new List<Vector2Int>();
            if (_playerPlacedLetters == null) return positions;

            letter = char.ToUpper(letter);
            foreach (KeyValuePair<Vector2Int, char> kvp in _playerPlacedLetters)
            {
                if (kvp.Value == letter)
                {
                    positions.Add(kvp.Key);
                }
            }
            return positions;
        }

        /// <summary>
        /// Checks if all coordinates for a letter in player's words have been guessed by the opponent.
        /// </summary>
        public bool AreAllPlayerLetterCoordinatesKnownByOpponent(char letter)
        {
            List<Vector2Int> positions = GetPlayerLetterPositions(letter);
            if (positions.Count == 0) return false;

            foreach (Vector2Int pos in positions)
            {
                if (!HasOpponentGuessedCoordinate(pos.x, pos.y))
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Gets all unique letters in the player's placed words.
        /// Used for iterating through all letters to check coordinate knowledge.
        /// </summary>
        public HashSet<char> GetAllPlayerLetters()
        {
            HashSet<char> letters = new HashSet<char>();
            if (_playerPlacedLetters == null) return letters;

            foreach (char letter in _playerPlacedLetters.Values)
            {
                letters.Add(char.ToUpper(letter));
            }
            return letters;
        }

        /// <summary>
        /// Gets all positions where a specific letter appears in the opponent's words.
        /// Used to check if all instances of a letter have been found on the grid.
        /// </summary>
        public List<Vector2Int> GetOpponentLetterPositions(char letter)
        {
            List<Vector2Int> positions = new List<Vector2Int>();
            if (_opponentPlacedLetters == null)
            {
                Debug.Log($"[GameplayGuessManager] GetOpponentLetterPositions('{letter}') - _opponentPlacedLetters is NULL");
                return positions;
            }

            Debug.Log($"[GameplayGuessManager] GetOpponentLetterPositions('{letter}') - _opponentPlacedLetters has {_opponentPlacedLetters.Count} entries");

            letter = char.ToUpper(letter);
            foreach (KeyValuePair<Vector2Int, char> kvp in _opponentPlacedLetters)
            {
                char storedLetter = char.ToUpper(kvp.Value);
                if (storedLetter == letter)
                {
                    positions.Add(kvp.Key);
                }
            }
            Debug.Log($"[GameplayGuessManager] GetOpponentLetterPositions('{letter}') - found {positions.Count} positions");
            return positions;
        }

        /// <summary>
        /// Checks if all coordinates for a letter have been guessed by the player.
        /// Returns true if every position where this letter appears has been guessed.
        /// </summary>
        public bool AreAllLetterCoordinatesKnown(char letter)
        {
            List<Vector2Int> positions = GetOpponentLetterPositions(letter);
            if (positions.Count == 0) return false;

            foreach (Vector2Int pos in positions)
            {
                if (!HasPlayerGuessedCoordinate(pos.x, pos.y))
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Checks if all opponent words have been correctly guessed via GUESS button.
        /// </summary>
        public bool AreAllOpponentWordsGuessed()
        {
            if (_opponentWords == null || _opponentSolvedWordIndices == null) return false;
            return _opponentSolvedWordIndices.Count >= _opponentWords.Count;
        }

        /// <summary>
        /// Checks if all coordinates containing letters have been guessed by the player.
        /// </summary>
        public bool AreAllOpponentCoordinatesKnown()
        {
            if (_opponentPlacedPositions == null || _playerGuessState == null) return false;

            foreach (Vector2Int pos in _opponentPlacedPositions)
            {
                if (!_playerGuessState.GuessedCoordinates.Contains(pos))
                {
                    return false;
                }
            }
            return _opponentPlacedPositions.Count > 0;
        }

        /// <summary>
        /// Checks if all letters in opponent's words have been GUESSED via keyboard.
        /// This means all word rows are complete (letters revealed in word display).
        /// NOTE: This checks GuessedLetters (keyboard guesses), NOT HitLetters (coordinate reveals).
        /// </summary>
        public bool AreAllOpponentLettersKnown()
        {
            if (_opponentPlacedLetters == null || _playerGuessState == null) return false;

            // Get all unique letters in opponent's words
            HashSet<char> requiredLetters = new HashSet<char>();
            foreach (char letter in _opponentPlacedLetters.Values)
            {
                requiredLetters.Add(char.ToUpper(letter));
            }

            // Check if all required letters have been GUESSED via keyboard
            // This is what fills in the word rows
            foreach (char letter in requiredLetters)
            {
                if (!_playerGuessState.GuessedLetters.Contains(letter))
                {
                    return false;
                }
            }
            return requiredLetters.Count > 0;
        }

        /// <summary>
        /// Checks the win condition: all letters in word rows revealed AND all coordinates known on grid.
        /// Win is achieved when:
        /// 1. All letters in opponent's words are discovered (word rows complete)
        /// 2. All grid coordinates containing letters have been found
        /// </summary>
        public bool HasPlayerWon()
        {
            return AreAllOpponentLettersKnown() && AreAllOpponentCoordinatesKnown();
        }

        /// <summary>
        /// Checks if the opponent has won the game by finding all player's letters and coordinates.
        /// </summary>
        public bool HasOpponentWon()
        {
            return AreAllPlayerLettersKnownByOpponent() && AreAllPlayerCoordinatesKnownByOpponent();
        }

        /// <summary>
        /// Checks if all unique letters in player's words have had all their coordinates guessed by opponent.
        /// </summary>
        private bool AreAllPlayerLettersKnownByOpponent()
        {
            if (_playerPlacedLetters == null || _playerPlacedLetters.Count == 0) return false;

            HashSet<char> uniqueLetters = GetAllPlayerLetters();
            foreach (char letter in uniqueLetters)
            {
                if (!AreAllPlayerLetterCoordinatesKnownByOpponent(letter))
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Checks if all coordinates containing letters in player's grid have been guessed by opponent.
        /// </summary>
        private bool AreAllPlayerCoordinatesKnownByOpponent()
        {
            if (_playerPlacedPositions == null || _opponentGuessState == null) return false;

            foreach (Vector2Int pos in _playerPlacedPositions)
            {
                if (!_opponentGuessState.GuessedCoordinates.Contains(pos))
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Gets the number of solved opponent words (via GUESS button).
        /// </summary>
        public int GetSolvedWordCount() => _opponentSolvedWordIndices?.Count ?? 0;

        /// <summary>
        /// Gets the total number of opponent words.
        /// </summary>
        public int GetTotalWordCount() => _opponentWords?.Count ?? 0;

        #endregion
    }
}

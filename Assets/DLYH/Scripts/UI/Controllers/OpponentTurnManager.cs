using UnityEngine;
using System;
using System.Collections.Generic;
using TecVooDoo.DontLoseYourHead.Core;
using DLYH.AI.Core;
using DLYH.AI.Config;
using DLYH.AI.Strategies;
using DLYH.Networking;
using Cysharp.Threading.Tasks;

namespace TecVooDoo.DontLoseYourHead.UI
{
    /// <summary>
    /// Manages opponent (AI or Remote player) initialization and turn execution.
    /// Handles IOpponent interface, event subscriptions, and game state building for AI.
    /// Extracted from GameplayUIController to reduce file size.
    /// </summary>
    public class OpponentTurnManager : IDisposable
    {
        #region Events

        /// <summary>Fired when opponent guesses a letter. Parameters: (letter, wasHit)</summary>
        public event Action<char, bool> OnLetterGuessProcessed;

        /// <summary>Fired when opponent guesses a coordinate. Parameters: (col, row, wasHit)</summary>
        public event Action<int, int, bool> OnCoordinateGuessProcessed;

        /// <summary>Fired when opponent guesses a word. Parameters: (word, rowIndex, wasCorrect)</summary>
        public event Action<string, int, bool> OnWordGuessProcessed;

        /// <summary>Fired when opponent disconnects (network only)</summary>
        public event Action OnOpponentDisconnected;

        /// <summary>Fired when opponent reconnects (network only)</summary>
        public event Action OnOpponentReconnected;

        /// <summary>Fired when opponent starts thinking</summary>
        public event Action OnThinkingStarted;

        /// <summary>Fired when opponent finishes thinking</summary>
        public event Action OnThinkingComplete;

        #endregion

        #region Private Fields

        private IOpponent _opponent;
        private bool _opponentInitialized = false;
        private ExecutionerConfigSO _aiConfig;
        private GameObject _gameObject;
        private Dictionary<int, WordListSO> _wordLists;

        // Callback for processing guesses
        private Func<char, bool> _processLetterGuess;
        private Func<int, int, bool> _processCoordinateGuess;
        private Func<string, int, bool> _processWordGuess;

        // State accessor callbacks
        private Func<bool> _isGameOver;
        private Func<bool> _isPlayerTurn;

        // Player setup data for building AI game state
        private SetupData _playerSetupData;
        private GameplayStateTracker _stateTracker;

        #endregion

        #region Properties

        public bool IsOpponentInitialized => _opponentInitialized;
        public IOpponent Opponent => _opponent;
        public string OpponentName => _opponent?.OpponentName ?? "Opponent";
        public bool IsAI => _opponent?.IsAI ?? true;

        #endregion

        #region Constructor

        /// <summary>
        /// Create a new OpponentTurnManager.
        /// </summary>
        /// <param name="aiConfig">AI configuration scriptable object</param>
        /// <param name="gameObject">MonoBehaviour's game object for coroutines</param>
        /// <param name="wordLists">Dictionary of word lists by length</param>
        public OpponentTurnManager(
            ExecutionerConfigSO aiConfig,
            GameObject gameObject,
            Dictionary<int, WordListSO> wordLists)
        {
            _aiConfig = aiConfig;
            _gameObject = gameObject;
            _wordLists = wordLists;
        }

        #endregion

        #region Initialization

        /// <summary>
        /// Set callbacks for guess processing.
        /// </summary>
        public void SetGuessCallbacks(
            Func<char, bool> processLetterGuess,
            Func<int, int, bool> processCoordinateGuess,
            Func<string, int, bool> processWordGuess)
        {
            _processLetterGuess = processLetterGuess;
            _processCoordinateGuess = processCoordinateGuess;
            _processWordGuess = processWordGuess;
        }

        /// <summary>
        /// Set state accessor callbacks.
        /// </summary>
        public void SetStateCallbacks(
            Func<bool> isGameOver,
            Func<bool> isPlayerTurn)
        {
            _isGameOver = isGameOver;
            _isPlayerTurn = isPlayerTurn;
        }

        /// <summary>
        /// Set player setup data for AI game state building.
        /// </summary>
        public void SetPlayerData(SetupData playerSetupData, GameplayStateTracker stateTracker)
        {
            _playerSetupData = playerSetupData;
            _stateTracker = stateTracker;
        }

        /// <summary>
        /// Initialize the opponent (AI or Remote player).
        /// For single-player games, creates a LocalAIOpponent.
        /// For multiplayer, pass a pre-created IOpponent.
        /// </summary>
        public async void InitializeOpponent(PlayerSetupData playerSetup, IOpponent opponent = null)
        {
            if (opponent == null)
            {
                if (_aiConfig == null)
                {
                    Debug.LogWarning("[OpponentTurnManager] No AI config assigned! AI will not function.");
                    return;
                }

                _opponent = OpponentFactory.CreateAIOpponent(_aiConfig, _gameObject, _wordLists);
            }
            else
            {
                _opponent = opponent;
            }

            await _opponent.InitializeAsync(playerSetup);
            SubscribeToOpponentEvents();

            _opponentInitialized = true;
            Debug.Log($"[OpponentTurnManager] Opponent initialized: {_opponent.OpponentName} (IsAI: {_opponent.IsAI})");
        }

        #endregion

        #region Event Subscriptions

        private void SubscribeToOpponentEvents()
        {
            if (_opponent == null) return;

            _opponent.OnLetterGuess += HandleLetterGuess;
            _opponent.OnCoordinateGuess += HandleCoordinateGuess;
            _opponent.OnWordGuess += HandleWordGuess;
            _opponent.OnThinkingStarted += HandleThinkingStarted;
            _opponent.OnThinkingComplete += HandleThinkingComplete;
            _opponent.OnDisconnected += HandleDisconnected;
            _opponent.OnReconnected += HandleReconnected;
        }

        private void UnsubscribeFromOpponentEvents()
        {
            if (_opponent == null) return;

            _opponent.OnLetterGuess -= HandleLetterGuess;
            _opponent.OnCoordinateGuess -= HandleCoordinateGuess;
            _opponent.OnWordGuess -= HandleWordGuess;
            _opponent.OnThinkingStarted -= HandleThinkingStarted;
            _opponent.OnThinkingComplete -= HandleThinkingComplete;
            _opponent.OnDisconnected -= HandleDisconnected;
            _opponent.OnReconnected -= HandleReconnected;
        }

        #endregion

        #region Event Handlers

        private void HandleLetterGuess(char letter)
        {
            if (_isGameOver?.Invoke() ?? false) return;

            Debug.Log($"[OpponentTurnManager] Opponent guesses letter: {letter}");
            bool wasHit = _processLetterGuess?.Invoke(letter) ?? false;

            if (wasHit)
            {
                _opponent?.RecordRevealedLetter(letter);
            }

            OnLetterGuessProcessed?.Invoke(letter, wasHit);
        }

        private void HandleCoordinateGuess(int row, int col)
        {
            if (_isGameOver?.Invoke() ?? false) return;

            string colLabel = ((char)('A' + col)).ToString();
            string coordLabel = colLabel + (row + 1);
            Debug.Log($"[OpponentTurnManager] Opponent guesses coordinate: {coordLabel}");

            bool wasHit = _processCoordinateGuess?.Invoke(col, row) ?? false;

            if (wasHit)
            {
                _opponent?.RecordOpponentHit(row, col);
            }

            OnCoordinateGuessProcessed?.Invoke(col, row, wasHit);
        }

        private void HandleWordGuess(string word, int rowIndex)
        {
            if (_isGameOver?.Invoke() ?? false) return;

            Debug.Log($"[OpponentTurnManager] Opponent guesses word: {word} (row {rowIndex + 1})");
            bool wasCorrect = _processWordGuess?.Invoke(word, rowIndex) ?? false;

            if (wasCorrect)
            {
                foreach (char letter in word.ToUpper())
                {
                    _opponent?.RecordRevealedLetter(letter);
                }
            }

            OnWordGuessProcessed?.Invoke(word, rowIndex, wasCorrect);
        }

        private void HandleThinkingStarted()
        {
            Debug.Log("[OpponentTurnManager] Opponent is thinking...");
            OnThinkingStarted?.Invoke();
        }

        private void HandleThinkingComplete()
        {
            Debug.Log("[OpponentTurnManager] Opponent finished thinking.");
            OnThinkingComplete?.Invoke();
        }

        private void HandleDisconnected()
        {
            Debug.Log("[OpponentTurnManager] Opponent disconnected!");
            OnOpponentDisconnected?.Invoke();
        }

        private void HandleReconnected()
        {
            Debug.Log("[OpponentTurnManager] Opponent reconnected!");
            OnOpponentReconnected?.Invoke();
        }

        #endregion

        #region Turn Execution

        /// <summary>
        /// Trigger the opponent to take their turn.
        /// </summary>
        public void TriggerOpponentTurn()
        {
            if (_isGameOver?.Invoke() ?? false) return;
            if (_isPlayerTurn?.Invoke() ?? true) return;
            if (_opponent == null) return;

            AIGameState gameState = BuildAIGameState();
            _opponent.ExecuteTurn(gameState);
        }

        /// <summary>
        /// Record a player guess result for opponent rubber-banding.
        /// </summary>
        public void RecordPlayerGuess(bool wasHit)
        {
            _opponent?.RecordPlayerGuess(wasHit);
        }

        /// <summary>
        /// Advance opponent turn counter.
        /// </summary>
        public void AdvanceTurn()
        {
            _opponent?.AdvanceTurn();
        }

        /// <summary>
        /// Record a guessed word for AI tracking.
        /// </summary>
        public void RecordGuessedWord(string word)
        {
            _stateTracker?.AddOpponentGuessedWord(word);
        }

        #endregion

        #region AI Game State Building

        /// <summary>
        /// Build AIGameState from current game state for AI decision making.
        /// </summary>
        private AIGameState BuildAIGameState()
        {
            if (_playerSetupData == null || _stateTracker == null)
            {
                Debug.LogError("[OpponentTurnManager] Cannot build AI game state - missing data!");
                return new AIGameState();
            }

            AIGameState state = new AIGameState();

            state.GridSize = _playerSetupData.GridSize;
            state.WordCount = _playerSetupData.WordCount;

            // Copy guessed letters (all letters AI has tried)
            state.GuessedLetters = new HashSet<char>(_stateTracker.OpponentGuessedLetters);

            // Copy hit letters (letters that exist in player's words)
            state.HitLetters = new HashSet<char>(_stateTracker.OpponentKnownLetters);

            // Skill level is handled internally by LocalAIOpponent
            state.SkillLevel = 0.5f;

            // Calculate fill ratio
            float avgWordLength = 4.5f;
            state.FillRatio = (_playerSetupData.WordCount * avgWordLength) /
                (_playerSetupData.GridSize * _playerSetupData.GridSize);

            // Copy ALL guessed coordinates
            state.GuessedCoordinates = new HashSet<(int, int)>();
            foreach (Vector2Int coord in _stateTracker.OpponentGuessedCoordinates)
            {
                state.GuessedCoordinates.Add((coord.y, coord.x)); // Note: (row, col)
            }

            // Track only hit coordinates
            state.HitCoordinates = new HashSet<(int, int)>();
            foreach (Vector2Int coord in _stateTracker.OpponentGuessedCoordinates)
            {
                bool wasHit = CheckIfCoordinateWasHit(coord.x, coord.y);
                if (wasHit)
                {
                    state.HitCoordinates.Add((coord.y, coord.x));
                }
            }

            // Build word patterns
            state.WordPatterns = new List<string>();
            foreach (WordPlacementData word in _playerSetupData.PlacedWords)
            {
                System.Text.StringBuilder pattern = new System.Text.StringBuilder();
                for (int i = 0; i < word.Word.Length; i++)
                {
                    char letter = word.Word[i];
                    if (_stateTracker.OpponentKnownLetters.Contains(letter))
                    {
                        pattern.Append(letter);
                    }
                    else
                    {
                        pattern.Append('_');
                    }
                }
                state.WordPatterns.Add(pattern.ToString());
            }

            // Populate word bank for AI word guessing
            state.WordBank = new HashSet<string>();
            HashSet<int> neededLengths = new HashSet<int>();
            foreach (WordPlacementData word in _playerSetupData.PlacedWords)
            {
                neededLengths.Add(word.Word.Length);
            }

            foreach (int length in neededLengths)
            {
                if (_wordLists.TryGetValue(length, out WordListSO wordList) && wordList?.Words != null)
                {
                    foreach (string word in wordList.Words)
                    {
                        state.WordBank.Add(word.ToUpper());
                    }
                }
            }

            // Initialize WordsSolved list
            state.WordsSolved = new List<bool>();
            foreach (WordPlacementData word in _playerSetupData.PlacedWords)
            {
                state.WordsSolved.Add(_stateTracker.OpponentSolvedWordRows.Contains(word.RowIndex));
            }

            // Copy previously guessed words
            state.GuessedWords = new HashSet<string>(_stateTracker.OpponentGuessedWords);

            return state;
        }

        /// <summary>
        /// Check if a coordinate was a hit by looking at word placements.
        /// </summary>
        private bool CheckIfCoordinateWasHit(int col, int row)
        {
            if (_playerSetupData == null) return false;

            foreach (WordPlacementData word in _playerSetupData.PlacedWords)
            {
                for (int i = 0; i < word.Word.Length; i++)
                {
                    int wordCol = word.StartCol + (i * word.DirCol);
                    int wordRow = word.StartRow + (i * word.DirRow);
                    if (wordCol == col && wordRow == row)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        #endregion

        #region IDisposable

        public void Dispose()
        {
            UnsubscribeFromOpponentEvents();
            _opponent?.Dispose();
            _opponent = null;
            _opponentInitialized = false;
        }

        #endregion
    }
}

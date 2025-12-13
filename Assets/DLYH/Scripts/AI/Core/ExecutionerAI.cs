// ExecutionerAI.cs
// Main AI controller for "The Executioner" opponent
// Created: December 13, 2025
// Developer: TecVooDoo LLC

using System;
using System.Collections.Generic;
using UnityEngine;
using Cysharp.Threading.Tasks;
using TecVooDoo.DontLoseYourHead.Core;
using DLYH.AI.Config;
using DLYH.AI.Data;
using DLYH.AI.Strategies;

namespace DLYH.AI.Core
{
    /// <summary>
    /// Main AI controller for the Executioner opponent.
    /// 
    /// Coordinates:
    /// - Strategy selection (letter, coordinate, word)
    /// - Difficulty adaptation (rubber-banding)
    /// - Memory management (skill-based recall)
    /// - Turn execution with think time delays
    /// 
    /// Usage:
    /// 1. Call Initialize() with config and player difficulty
    /// 2. Call ExecuteTurn() when it's the AI's turn
    /// 3. Call RecordPlayerGuess() after player makes a guess
    /// </summary>
    public class ExecutionerAI : MonoBehaviour
    {
        // ============================================================
        // EVENTS
        // ============================================================

        /// <summary>Fired when AI starts thinking (for UI feedback)</summary>
        public event Action OnThinkingStarted;

        /// <summary>Fired when AI finishes thinking and is about to act</summary>
        public event Action OnThinkingComplete;

        /// <summary>Fired when AI makes a letter guess. Params: letter</summary>
        public event Action<char> OnLetterGuess;

        /// <summary>Fired when AI makes a coordinate guess. Params: row, col</summary>
        public event Action<int, int> OnCoordinateGuess;

        /// <summary>Fired when AI makes a word guess. Params: word, wordIndex</summary>
        public event Action<string, int> OnWordGuess;

        // ============================================================
        // SERIALIZED FIELDS
        // ============================================================

        [Header("Configuration")]
        [SerializeField]
        [Tooltip("AI configuration ScriptableObject")]
        private ExecutionerConfigSO _config;

        [Header("Debug")]
        [SerializeField]
        [Tooltip("Enable detailed debug logging")]
        private bool _debugMode = false;

        // ============================================================
        // RUNTIME STATE
        // ============================================================

        private DifficultyAdapter _difficultyAdapter;
        private MemoryManager _memoryManager;

        private LetterGuessStrategy _letterStrategy;
        private CoordinateGuessStrategy _coordinateStrategy;
        private WordGuessStrategy _wordStrategy;

        private DifficultySetting _aiDifficulty;
        private bool _isInitialized;
        private bool _isThinking;

        // ============================================================
        // PROPERTIES
        // ============================================================

        /// <summary>Current AI skill level (0.15 to 0.95)</summary>
        public float CurrentSkill => _difficultyAdapter?.CurrentSkill ?? 0.5f;

        /// <summary>AI difficulty setting (inverted from player's choice)</summary>
        public DifficultySetting AIDifficulty => _aiDifficulty;

        /// <summary>Whether the AI is currently thinking</summary>
        public bool IsThinking => _isThinking;

        /// <summary>Whether the AI has been initialized</summary>
        public bool IsInitialized => _isInitialized;

        // ============================================================
        // INITIALIZATION
        // ============================================================

        /// <summary>
        /// Initializes the AI for a new game.
        /// </summary>
        /// <param name="playerDifficulty">The difficulty the PLAYER selected</param>
        public void Initialize(DifficultySetting playerDifficulty)
        {
            if (_config == null)
            {
                Debug.LogError("[ExecutionerAI] No config assigned! Assign ExecutionerConfigSO in inspector.");
                return;
            }

            // Invert difficulty: Player Easy = AI Hard, etc.
            _aiDifficulty = GetInverseDifficulty(playerDifficulty);

            Debug.Log(string.Format("[ExecutionerAI] Initializing - Player: {0}, AI: {1}",
                playerDifficulty, _aiDifficulty));

            // Initialize core systems
            _difficultyAdapter = new DifficultyAdapter(_config, _aiDifficulty);
            _memoryManager = new MemoryManager(_config);

            // Initialize strategies
            _letterStrategy = new LetterGuessStrategy(_config);
            _coordinateStrategy = new CoordinateGuessStrategy(_config);
            _wordStrategy = new WordGuessStrategy(_config);

            _isInitialized = true;
            _isThinking = false;

            if (_debugMode)
            {
                Debug.Log(_difficultyAdapter.GetDebugSummary());
            }
        }

        /// <summary>
        /// Resets the AI for a new game with the same settings.
        /// </summary>
        public void Reset()
        {
            if (!_isInitialized)
            {
                Debug.LogWarning("[ExecutionerAI] Cannot reset - not initialized");
                return;
            }

            _difficultyAdapter.Reset(_aiDifficulty);
            _memoryManager.Reset();
            _isThinking = false;

            Debug.Log("[ExecutionerAI] Reset for new game");
        }

        // ============================================================
        // DIFFICULTY INVERSION
        // ============================================================

        /// <summary>
        /// Inverts the player's difficulty selection for the AI.
        /// Player Easy = AI Hard (AI has fewer misses allowed, plays smarter)
        /// Player Normal = AI Normal
        /// Player Hard = AI Easy (AI has more misses allowed, plays worse)
        /// </summary>
        /// <param name="playerDifficulty">Player's selected difficulty</param>
        /// <returns>Inverted difficulty for AI</returns>
        private DifficultySetting GetInverseDifficulty(DifficultySetting playerDifficulty)
        {
            switch (playerDifficulty)
            {
                case DifficultySetting.Easy:
                    return DifficultySetting.Hard;
                case DifficultySetting.Hard:
                    return DifficultySetting.Easy;
                default:
                    return DifficultySetting.Normal;
            }
        }

        // ============================================================
        // TURN EXECUTION
        // ============================================================

        /// <summary>
        /// Executes the AI's turn asynchronously.
        /// Includes think time delay for human-like pacing.
        /// </summary>
        /// <param name="gameState">Current game state for decision making</param>
        public async UniTaskVoid ExecuteTurnAsync(AIGameState gameState)
        {
            if (!_isInitialized)
            {
                Debug.LogError("[ExecutionerAI] Cannot execute turn - not initialized");
                return;
            }

            if (_isThinking)
            {
                Debug.LogWarning("[ExecutionerAI] Already thinking, ignoring ExecuteTurn call");
                return;
            }

            _isThinking = true;
            OnThinkingStarted?.Invoke();

            try
            {
                // Apply current skill level to game state
                gameState.SkillLevel = _difficultyAdapter.CurrentSkill;

                // Think time delay
                float thinkTime = _config.GetRandomThinkTime();
                if (_debugMode)
                {
                    Debug.Log(string.Format("[ExecutionerAI] Thinking for {0:F1}s...", thinkTime));
                }

                await UniTask.Delay(TimeSpan.FromSeconds(thinkTime));

                // Make decision
                GuessRecommendation recommendation = DecideGuess(gameState);

                OnThinkingComplete?.Invoke();

                // Execute the guess
                ExecuteGuess(recommendation);
            }
            finally
            {
                _isThinking = false;
            }
        }

        /// <summary>
        /// Decides what guess to make based on current game state.
        /// </summary>
        /// <param name="gameState">Current game state</param>
        /// <returns>The recommended guess</returns>
        private GuessRecommendation DecideGuess(AIGameState gameState)
        {
            // Update game state with memory-filtered information
            ApplyMemoryFilter(gameState);

            // Step 1: Check for high-confidence word guess opportunity
            GuessRecommendation wordRec = _wordStrategy.Evaluate(gameState);
            if (wordRec.IsValid)
            {
                if (_debugMode)
                {
                    Debug.Log(string.Format("[ExecutionerAI] Word guess opportunity: {0} (confidence: {1:P0})",
                        wordRec.WordGuess, wordRec.Confidence));
                }
                return wordRec;
            }

            // Step 2: Choose between letter and coordinate based on grid density
            _config.GetStrategyWeightsForDensity(gameState.FillRatio, out float letterWeight, out float coordWeight);

            // Normalize weights
            float totalWeight = letterWeight + coordWeight;
            float letterChance = letterWeight / totalWeight;

            bool chooseLetter = UnityEngine.Random.value < letterChance;

            if (_debugMode)
            {
                Debug.Log(string.Format("[ExecutionerAI] Strategy weights - Letter: {0:P0}, Coord: {1:P0}, Chose: {2}",
                    letterChance, 1 - letterChance, chooseLetter ? "Letter" : "Coordinate"));
            }

            // Step 3: Evaluate chosen strategy
            if (chooseLetter)
            {
                GuessRecommendation letterRec = _letterStrategy.Evaluate(gameState);
                if (letterRec.IsValid)
                {
                    return letterRec;
                }

                // Fallback to coordinate if no letters available
                if (_debugMode)
                {
                    Debug.Log("[ExecutionerAI] No valid letter guess, falling back to coordinate");
                }
                return _coordinateStrategy.Evaluate(gameState);
            }
            else
            {
                GuessRecommendation coordRec = _coordinateStrategy.Evaluate(gameState);
                if (coordRec.IsValid)
                {
                    return coordRec;
                }

                // Fallback to letter if no coordinates available
                if (_debugMode)
                {
                    Debug.Log("[ExecutionerAI] No valid coordinate guess, falling back to letter");
                }
                return _letterStrategy.Evaluate(gameState);
            }
        }

        /// <summary>
        /// Applies memory filtering to the game state based on current skill.
        /// Lower skill = may "forget" some known information.
        /// </summary>
        /// <param name="gameState">Game state to filter</param>
        private void ApplyMemoryFilter(AIGameState gameState)
        {
            float skill = _difficultyAdapter.CurrentSkill;

            // Filter hit coordinates through memory
            HashSet<(int row, int col)> rememberedHits = _memoryManager.GetEffectiveKnownHits(skill);

            // For now, we don't actually modify the game state's hit coordinates
            // because the AI should still know what cells have been guessed (can't re-guess)
            // Memory affects strategy scoring, not available moves

            if (_debugMode && skill < 0.8f)
            {
                int forgotten = gameState.HitCoordinates.Count - rememberedHits.Count;
                if (forgotten > 0)
                {
                    Debug.Log(string.Format("[ExecutionerAI] Memory: Forgot {0} hit coordinate(s)", forgotten));
                }
            }
        }

        /// <summary>
        /// Executes the recommended guess by firing the appropriate event.
        /// </summary>
        /// <param name="recommendation">The guess to execute</param>
        private void ExecuteGuess(GuessRecommendation recommendation)
        {
            if (!recommendation.IsValid)
            {
                Debug.LogError("[ExecutionerAI] Attempted to execute invalid recommendation");
                return;
            }

            switch (recommendation.Type)
            {
                case GuessType.Letter:
                    if (_debugMode)
                    {
                        Debug.Log(string.Format("[ExecutionerAI] Guessing letter: {0}", recommendation.Letter));
                    }
                    OnLetterGuess?.Invoke(recommendation.Letter);
                    break;

                case GuessType.Coordinate:
                    string coordStr = GridAnalyzer.CoordinateToString(recommendation.Row, recommendation.Col);
                    if (_debugMode)
                    {
                        Debug.Log(string.Format("[ExecutionerAI] Guessing coordinate: {0}", coordStr));
                    }
                    OnCoordinateGuess?.Invoke(recommendation.Row, recommendation.Col);
                    break;

                case GuessType.Word:
                    if (_debugMode)
                    {
                        Debug.Log(string.Format("[ExecutionerAI] Guessing word: {0} (index {1})",
                            recommendation.WordGuess, recommendation.WordIndex));
                    }
                    OnWordGuess?.Invoke(recommendation.WordGuess, recommendation.WordIndex);
                    break;
            }
        }

        // ============================================================
        // PLAYER GUESS RECORDING
        // ============================================================

        /// <summary>
        /// Records the result of a player's guess for rubber-banding adjustment.
        /// Call this after each player guess to update AI difficulty.
        /// </summary>
        /// <param name="wasHit">True if the player's guess was correct</param>
        public void RecordPlayerGuess(bool wasHit)
        {
            if (!_isInitialized)
            {
                return;
            }

            _difficultyAdapter.RecordPlayerGuess(wasHit);

            if (_debugMode)
            {
                Debug.Log(_difficultyAdapter.GetDebugSummary());
            }
        }

        /// <summary>
        /// Records an AI hit for memory tracking.
        /// Call this when AI successfully hits a coordinate.
        /// </summary>
        /// <param name="row">Row of the hit</param>
        /// <param name="col">Column of the hit</param>
        public void RecordAIHit(int row, int col)
        {
            if (!_isInitialized)
            {
                return;
            }

            _memoryManager.RecordHit(row, col);
        }

        /// <summary>
        /// Records a revealed letter for memory tracking.
        /// Call this when a letter is revealed through any means.
        /// </summary>
        /// <param name="letter">The revealed letter</param>
        public void RecordRevealedLetter(char letter)
        {
            if (!_isInitialized)
            {
                return;
            }

            _memoryManager.RecordRevealedLetter(letter);
        }

        /// <summary>
        /// Advances the memory turn counter.
        /// Call this at the end of each AI turn.
        /// </summary>
        public void AdvanceTurn()
        {
            if (!_isInitialized)
            {
                return;
            }

            _memoryManager.AdvanceTurn();
        }

        // ============================================================
        // DEBUG
        // ============================================================

        /// <summary>
        /// Gets a comprehensive debug summary of AI state.
        /// </summary>
        /// <returns>Formatted debug string</returns>
        public string GetDebugSummary()
        {
            if (!_isInitialized)
            {
                return "[ExecutionerAI] Not initialized";
            }

            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            sb.AppendLine("=== EXECUTIONER AI DEBUG ===");
            sb.AppendLine(string.Format("AI Difficulty: {0} (inverted from player)", _aiDifficulty));
            sb.AppendLine();
            sb.AppendLine(_difficultyAdapter.GetDebugSummary());
            sb.AppendLine();
            sb.AppendLine(_memoryManager.GetDebugSummary(_difficultyAdapter.CurrentSkill));

            return sb.ToString();
        }

        /// <summary>
        /// Gets debug analysis for a specific game state.
        /// </summary>
        /// <param name="gameState">Game state to analyze</param>
        /// <returns>Formatted analysis string</returns>
        public string GetStrategyAnalysis(AIGameState gameState)
        {
            if (!_isInitialized)
            {
                return "[ExecutionerAI] Not initialized";
            }

            gameState.SkillLevel = _difficultyAdapter.CurrentSkill;

            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            sb.AppendLine("=== STRATEGY ANALYSIS ===");
            sb.AppendLine();
            sb.AppendLine(_letterStrategy.GetDebugScoreBreakdown(gameState, 5));
            sb.AppendLine();
            sb.AppendLine(_coordinateStrategy.GetDebugScoreBreakdown(gameState, 5));
            sb.AppendLine();
            sb.AppendLine(_wordStrategy.GetDebugAnalysis(gameState));

            return sb.ToString();
        }
    }
}
// LocalAIOpponent.cs
// Wraps ExecutionerAI to implement IOpponent interface for single-player games
// Created: January 4, 2026
// Developer: TecVooDoo LLC

using System;
using System.Collections.Generic;
using UnityEngine;
using Cysharp.Threading.Tasks;
using DLYH.AI.Config;
using DLYH.AI.Core;
using DLYH.AI.Strategies;
using DLYH.Core.GameState;

namespace DLYH.Networking
{
    /// <summary>
    /// Wraps ExecutionerAI to implement IOpponent interface.
    /// Used for single-player games against The Executioner AI.
    ///
    /// This class:
    /// - Creates and manages an ExecutionerAI component
    /// - Generates AI setup data using AISetupManager
    /// - Forwards events from ExecutionerAI to IOpponent event handlers
    /// - Handles AI initialization and turn execution
    /// </summary>
    public class LocalAIOpponent : IOpponent
    {
        // ============================================================
        // EVENTS (forwarded from ExecutionerAI)
        // ============================================================

        public event Action OnThinkingStarted;
        public event Action OnThinkingComplete;
        public event Action<char> OnLetterGuess;
        public event Action<int, int> OnCoordinateGuess;
        public event Action<string, int> OnWordGuess;
        public event Action OnDisconnected; // Never fired for AI
        public event Action OnReconnected;  // Never fired for AI

        // ============================================================
        // CONFIGURATION
        // ============================================================

        private readonly ExecutionerConfigSO _config;
        private readonly GameObject _hostGameObject;
        private readonly Dictionary<int, WordListSO> _wordLists;

        // ============================================================
        // RUNTIME STATE
        // ============================================================

        private ExecutionerAI _executionerAI;
        private AISetupManager _setupManager;
        private PlayerSetupData _opponentSetupData;
        private int _missLimit;
        private bool _isInitialized;
        private bool _isDisposed;
        private string _customOpponentName; // For phantom AI names

        // ============================================================
        // PROPERTIES
        // ============================================================

        /// <summary>Default color for The Executioner - Royal Blue RGB(60, 90, 180).</summary>
        public static readonly Color ExecutionerDefaultColor = new Color(0.235f, 0.353f, 0.706f, 1f);

        public string OpponentName => _customOpponentName ?? _opponentSetupData?.PlayerName ?? "The Executioner";
        public Color OpponentColor => _opponentSetupData?.PlayerColor ?? ExecutionerDefaultColor;
        public int GridSize => _opponentSetupData?.GridSize ?? 8;
        public int WordCount => _opponentSetupData?.WordCount ?? 3;
        public List<WordPlacementData> WordPlacements => _opponentSetupData?.PlacedWords ?? new List<WordPlacementData>();
        public bool IsConnected => true; // AI is always "connected"
        public bool IsThinking => _executionerAI?.IsThinking ?? false;
        public bool IsAI => true;
        public int MissLimit => _missLimit;

        // ============================================================
        // CONSTRUCTOR
        // ============================================================

        /// <summary>
        /// Creates a new LocalAIOpponent.
        /// </summary>
        /// <param name="config">ExecutionerAI configuration ScriptableObject</param>
        /// <param name="hostGameObject">GameObject to attach ExecutionerAI component to</param>
        /// <param name="wordLists">Dictionary of word lists by length for AI word selection</param>
        public LocalAIOpponent(ExecutionerConfigSO config, GameObject hostGameObject, Dictionary<int, WordListSO> wordLists, string customName = null)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _hostGameObject = hostGameObject ?? throw new ArgumentNullException(nameof(hostGameObject));
            _wordLists = wordLists ?? throw new ArgumentNullException(nameof(wordLists));
            _customOpponentName = customName; // For phantom AI names
        }

        /// <summary>
        /// Sets a custom opponent name (used for phantom AI).
        /// </summary>
        public void SetCustomOpponentName(string name)
        {
            _customOpponentName = name;
        }

        // ============================================================
        // INITIALIZATION
        // ============================================================

        /// <summary>
        /// Initializes the AI opponent based on the local player's setup.
        /// Generates AI grid size, word count, words, and placements.
        /// </summary>
        public async UniTask InitializeAsync(PlayerSetupData localPlayerSetup)
        {
            if (_isDisposed)
            {
                Debug.LogError("[LocalAIOpponent] Cannot initialize - already disposed");
                return;
            }

            if (_isInitialized)
            {
                Debug.LogWarning("[LocalAIOpponent] Already initialized, resetting first");
                Reset();
            }

            // Generate opponent setup data based on player's difficulty
            _opponentSetupData = GenerateOpponentSetup(localPlayerSetup.DifficultyLevel);

            // Calculate miss limit based on player's grid settings
            _missLimit = GameplayStateTracker.CalculateMissLimit(
                (int)localPlayerSetup.DifficultyLevel,
                localPlayerSetup.GridSize,
                localPlayerSetup.WordCount
            );

            // Create ExecutionerAI component
            _executionerAI = _hostGameObject.AddComponent<ExecutionerAI>();

            // Set config via reflection (it's a private serialized field)
            SetExecutionerConfig();

            // Initialize the AI with player's difficulty (AI will invert it)
            _executionerAI.Initialize(localPlayerSetup.DifficultyLevel);

            // Subscribe to AI events and forward them
            SubscribeToAIEvents();

            _isInitialized = true;

            Debug.Log($"[LocalAIOpponent] Initialized - Grid: {GridSize}x{GridSize}, Words: {WordCount}, Miss Limit: {MissLimit}");

            // Small delay to simulate async setup (for interface consistency)
            await UniTask.Yield();
        }

        /// <summary>
        /// Generates AI opponent setup data based on player's difficulty.
        /// AI grid size and word count vary based on difficulty for variety.
        /// </summary>
        private PlayerSetupData GenerateOpponentSetup(DifficultySetting playerDifficulty)
        {
            PlayerSetupData setup = new PlayerSetupData
            {
                PlayerName = "The Executioner",
                PlayerColor = ExecutionerDefaultColor // Royal Blue - consistent with default
            };

            // Determine grid size and word count based on player difficulty
            // (Inverted: Player Easy = harder for player to find AI's words)
            switch (playerDifficulty)
            {
                case DifficultySetting.Easy:
                    // AI uses larger grids with fewer words (harder for player to find)
                    int[] easyGrids = { 6, 7, 8 };
                    setup.GridSize = easyGrids[UnityEngine.Random.Range(0, easyGrids.Length)];
                    setup.WordCount = 4;
                    break;

                case DifficultySetting.Normal:
                    int[] normalGrids = { 8, 9, 10 };
                    setup.GridSize = normalGrids[UnityEngine.Random.Range(0, normalGrids.Length)];
                    setup.WordCount = UnityEngine.Random.value > 0.5f ? 3 : 4;
                    break;

                case DifficultySetting.Hard:
                    // AI uses larger grids with fewer words (hardest for player)
                    int[] hardGrids = { 10, 11, 12 };
                    setup.GridSize = hardGrids[UnityEngine.Random.Range(0, hardGrids.Length)];
                    setup.WordCount = 3;
                    break;

                default:
                    setup.GridSize = 8;
                    setup.WordCount = 3;
                    break;
            }

            // Determine word lengths
            setup.WordLengths = DetermineWordLengths(setup.WordCount);
            setup.DifficultyLevel = playerDifficulty;

            // Generate words and placements
            _setupManager = new AISetupManager(setup.GridSize, setup.WordCount, setup.WordLengths);

            if (_setupManager.SelectWords(_wordLists))
            {
                if (_setupManager.PlaceWords())
                {
                    setup.PlacedWords = new List<WordPlacementData>(_setupManager.Placements);
                }
                else
                {
                    Debug.LogError("[LocalAIOpponent] Failed to place AI words");
                }
            }
            else
            {
                Debug.LogError("[LocalAIOpponent] Failed to select AI words");
            }

            return setup;
        }

        /// <summary>
        /// Determines word lengths based on word count.
        /// </summary>
        private int[] DetermineWordLengths(int wordCount)
        {
            if (wordCount == 4)
            {
                return new int[] { 3, 4, 5, 6 };
            }
            else // 3 words
            {
                return new int[] { 4, 5, 6 };
            }
        }

        /// <summary>
        /// Sets the ExecutionerAI config via reflection.
        /// </summary>
        private void SetExecutionerConfig()
        {
            var configField = typeof(ExecutionerAI).GetField("_config",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            if (configField != null)
            {
                configField.SetValue(_executionerAI, _config);
            }
            else
            {
                Debug.LogError("[LocalAIOpponent] Could not find _config field in ExecutionerAI");
            }
        }

        /// <summary>
        /// Subscribes to ExecutionerAI events and forwards them through IOpponent events.
        /// </summary>
        private void SubscribeToAIEvents()
        {
            if (_executionerAI == null) return;

            _executionerAI.OnThinkingStarted += HandleThinkingStarted;
            _executionerAI.OnThinkingComplete += HandleThinkingComplete;
            _executionerAI.OnLetterGuess += HandleLetterGuess;
            _executionerAI.OnCoordinateGuess += HandleCoordinateGuess;
            _executionerAI.OnWordGuess += HandleWordGuess;
        }

        /// <summary>
        /// Unsubscribes from ExecutionerAI events.
        /// </summary>
        private void UnsubscribeFromAIEvents()
        {
            if (_executionerAI == null) return;

            _executionerAI.OnThinkingStarted -= HandleThinkingStarted;
            _executionerAI.OnThinkingComplete -= HandleThinkingComplete;
            _executionerAI.OnLetterGuess -= HandleLetterGuess;
            _executionerAI.OnCoordinateGuess -= HandleCoordinateGuess;
            _executionerAI.OnWordGuess -= HandleWordGuess;
        }

        // Event handlers that forward to IOpponent events
        private void HandleThinkingStarted() => OnThinkingStarted?.Invoke();
        private void HandleThinkingComplete() => OnThinkingComplete?.Invoke();
        private void HandleLetterGuess(char letter) => OnLetterGuess?.Invoke(letter);
        private void HandleCoordinateGuess(int row, int col) => OnCoordinateGuess?.Invoke(row, col);
        private void HandleWordGuess(string word, int index) => OnWordGuess?.Invoke(word, index);

        // ============================================================
        // RESET
        // ============================================================

        public void Reset()
        {
            if (_executionerAI != null)
            {
                _executionerAI.Reset();
            }
        }

        // ============================================================
        // TURN EXECUTION
        // ============================================================

        public void ExecuteTurn(AIGameState gameState)
        {
            if (!_isInitialized || _executionerAI == null)
            {
                Debug.LogError("[LocalAIOpponent] Cannot execute turn - not initialized");
                return;
            }

            // ExecutionerAI handles the async execution internally
            _executionerAI.ExecuteTurnAsync(gameState).Forget();
        }

        // ============================================================
        // FEEDBACK
        // ============================================================

        public void RecordPlayerGuess(bool wasHit)
        {
            _executionerAI?.RecordPlayerGuess(wasHit);
        }

        public void RecordOpponentHit(int row, int col)
        {
            _executionerAI?.RecordAIHit(row, col);
        }

        public void RecordRevealedLetter(char letter)
        {
            _executionerAI?.RecordRevealedLetter(letter);
        }

        public void AdvanceTurn()
        {
            _executionerAI?.AdvanceTurn();
        }

        // ============================================================
        // DEBUG
        // ============================================================

        public string GetDebugSummary()
        {
            if (!_isInitialized)
            {
                return "[LocalAIOpponent] Not initialized";
            }

            var sb = new System.Text.StringBuilder();
            sb.AppendLine("=== LOCAL AI OPPONENT ===");
            sb.AppendLine($"Name: {OpponentName}");
            sb.AppendLine($"Grid: {GridSize}x{GridSize}");
            sb.AppendLine($"Words: {WordCount}");
            sb.AppendLine($"Miss Limit: {MissLimit}");
            sb.AppendLine();

            if (_executionerAI != null)
            {
                sb.AppendLine(_executionerAI.GetDebugSummary());
            }

            return sb.ToString();
        }

        // ============================================================
        // DISPOSE
        // ============================================================

        public void Dispose()
        {
            if (_isDisposed) return;

            UnsubscribeFromAIEvents();

            if (_executionerAI != null)
            {
                UnityEngine.Object.Destroy(_executionerAI);
                _executionerAI = null;
            }

            _isInitialized = false;
            _isDisposed = true;

            Debug.Log("[LocalAIOpponent] Disposed");
        }
    }
}

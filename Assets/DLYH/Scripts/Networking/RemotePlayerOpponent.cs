// RemotePlayerOpponent.cs
// Implements IOpponent for network multiplayer games
// Created: January 4, 2026
// Developer: TecVooDoo LLC

using System;
using System.Collections.Generic;
using UnityEngine;
using Cysharp.Threading.Tasks;
using DLYH.AI.Strategies;
using DLYH.Networking.Services;
using TecVooDoo.DontLoseYourHead.Core;
using TecVooDoo.DontLoseYourHead.UI;

namespace DLYH.Networking
{
    /// <summary>
    /// Implements IOpponent for remote human players in network multiplayer.
    ///
    /// This class:
    /// - Manages connection to Supabase for game state
    /// - Subscribes to realtime updates for opponent actions
    /// - Translates network state changes into IOpponent events
    /// - Handles disconnect/reconnect scenarios
    /// </summary>
    public class RemotePlayerOpponent : IOpponent
    {
        // ============================================================
        // EVENTS
        // ============================================================

        public event Action OnThinkingStarted;
        public event Action OnThinkingComplete;
        public event Action<char> OnLetterGuess;
        public event Action<int, int> OnCoordinateGuess;
        public event Action<string, int> OnWordGuess;
        public event Action OnDisconnected;
        public event Action OnReconnected;

        // ============================================================
        // CONFIGURATION
        // ============================================================

        private readonly SupabaseConfig _config;
        private readonly string _gameCode;
        private readonly bool _isLocalPlayerHost; // Is local player Player 1?
        private readonly string _localPlayerId;

        // ============================================================
        // SERVICES
        // ============================================================

        private SupabaseClient _supabaseClient;
        private AuthService _authService;
        private GameSessionService _sessionService;
        private GameSubscription _subscription;
        private GameStateSynchronizer _synchronizer;

        // ============================================================
        // STATE
        // ============================================================

        private PlayerSetupData _opponentSetupData;
        private int _missLimit;
        private bool _isInitialized;
        private bool _isConnected;
        private bool _isThinking;
        private bool _isDisposed;
        private bool _waitingForOpponentTurn;
        private DLYHGameState _lastGameState;
        private DLYHGameplayState _lastOpponentGameplayState;

        // ============================================================
        // PROPERTIES
        // ============================================================

        public string OpponentName => _opponentSetupData?.PlayerName ?? "Opponent";
        public Color OpponentColor => _opponentSetupData?.PlayerColor ?? Color.blue;
        public int GridSize => _opponentSetupData?.GridSize ?? 8;
        public int WordCount => _opponentSetupData?.WordCount ?? 3;
        public List<WordPlacementData> WordPlacements => _opponentSetupData?.PlacedWords ?? new List<WordPlacementData>();
        public bool IsConnected => _isConnected;
        public bool IsThinking => _isThinking;
        public bool IsAI => false;
        public int MissLimit => _missLimit;

        // ============================================================
        // CONSTRUCTOR
        // ============================================================

        /// <summary>
        /// Creates a new RemotePlayerOpponent.
        /// </summary>
        /// <param name="config">Supabase configuration</param>
        /// <param name="gameCode">6-character game code</param>
        /// <param name="isLocalPlayerHost">True if local player is Player 1 (host)</param>
        public RemotePlayerOpponent(SupabaseConfig config, string gameCode, bool isLocalPlayerHost)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _gameCode = gameCode ?? throw new ArgumentNullException(nameof(gameCode));
            _isLocalPlayerHost = isLocalPlayerHost;

            Debug.Log($"[RemotePlayerOpponent] Created for game {_gameCode}, local is {(_isLocalPlayerHost ? "host" : "guest")}");
        }

        // ============================================================
        // INITIALIZATION
        // ============================================================

        /// <summary>
        /// Initializes the remote opponent connection and waits for opponent setup data.
        /// </summary>
        public async UniTask InitializeAsync(PlayerSetupData localPlayerSetup)
        {
            if (_isDisposed)
            {
                Debug.LogError("[RemotePlayerOpponent] Cannot initialize - already disposed");
                return;
            }

            if (_isInitialized)
            {
                Debug.LogWarning("[RemotePlayerOpponent] Already initialized");
                return;
            }

            Debug.Log("[RemotePlayerOpponent] Initializing...");

            // Create services
            _supabaseClient = new SupabaseClient(_config);
            _authService = new AuthService(_config);
            _sessionService = new GameSessionService(_supabaseClient, _config);

            // Ensure we have auth
            var session = await _authService.EnsureValidSessionAsync();
            if (session == null)
            {
                Debug.LogError("[RemotePlayerOpponent] Failed to authenticate");
                return;
            }

            // Update client with auth token
            _supabaseClient = new SupabaseClient(_config, session.AccessToken);
            _sessionService = new GameSessionService(_supabaseClient, _config);

            // Subscribe to game updates
            _subscription = new GameSubscription(_config, _gameCode, session.UserId, session.AccessToken);

            bool subscribed = await _subscription.StartAsync();
            if (!subscribed)
            {
                Debug.LogError("[RemotePlayerOpponent] Failed to subscribe to game updates");
                return;
            }

            // Create synchronizer
            _synchronizer = new GameStateSynchronizer(
                _sessionService,
                _subscription,
                _gameCode,
                session.UserId,
                _isLocalPlayerHost
            );

            // Subscribe to events
            _subscription.OnConnectionLost += HandleConnectionLost;
            _subscription.OnReconnected += HandleReconnection;
            _synchronizer.OnRemoteStateReceived += HandleRemoteStateUpdate;

            // Fetch current game state
            var gameState = await _synchronizer.FetchCurrentStateAsync();
            if (gameState == null)
            {
                Debug.LogError("[RemotePlayerOpponent] Failed to fetch game state");
                return;
            }

            _lastGameState = gameState;

            // Push local player setup
            await _synchronizer.PushSetupDataAsync(
                localPlayerSetup.PlayerName,
                ColorToHex(localPlayerSetup.PlayerColor),
                localPlayerSetup.GridSize,
                localPlayerSetup.WordCount,
                localPlayerSetup.DifficultyLevel.ToString(),
                EncryptWordPlacements(localPlayerSetup.PlacedWords)
            );

            // Calculate miss limit based on opponent's grid
            // (Will be set when opponent data arrives)
            _missLimit = GameplayStateTracker.CalculateMissLimit(
                (int)localPlayerSetup.DifficultyLevel,
                localPlayerSetup.GridSize,
                localPlayerSetup.WordCount
            );

            // Wait for opponent setup data
            await WaitForOpponentSetupAsync();

            _isConnected = true;
            _isInitialized = true;

            Debug.Log($"[RemotePlayerOpponent] Initialized - Opponent: {OpponentName}, Grid: {GridSize}x{GridSize}");
        }

        /// <summary>
        /// Waits for the opponent to complete their setup.
        /// </summary>
        private async UniTask WaitForOpponentSetupAsync()
        {
            Debug.Log("[RemotePlayerOpponent] Waiting for opponent setup...");

            int timeoutSeconds = 300; // 5 minute timeout
            float startTime = Time.realtimeSinceStartup;

            while (Time.realtimeSinceStartup - startTime < timeoutSeconds)
            {
                // Refresh game state
                var state = await _synchronizer.FetchCurrentStateAsync();
                if (state == null)
                {
                    await UniTask.Delay(1000);
                    continue;
                }

                // Check opponent data
                var opponentData = _isLocalPlayerHost ? state.player2 : state.player1;

                if (opponentData != null && opponentData.setupComplete)
                {
                    // Extract opponent setup
                    _opponentSetupData = new PlayerSetupData
                    {
                        PlayerName = opponentData.name,
                        PlayerColor = HexToColor(opponentData.color),
                        GridSize = opponentData.setupData?.gridSize ?? 8,
                        WordCount = opponentData.setupData?.wordCount ?? 3,
                        DifficultyLevel = ParseDifficulty(opponentData.setupData?.difficulty),
                        PlacedWords = new List<WordPlacementData>() // Encrypted, revealed at game end
                    };

                    // Update miss limit based on opponent's actual grid
                    _missLimit = GameplayStateTracker.CalculateMissLimit(
                        (int)_opponentSetupData.DifficultyLevel,
                        _opponentSetupData.GridSize,
                        _opponentSetupData.WordCount
                    );

                    Debug.Log($"[RemotePlayerOpponent] Opponent setup received: {opponentData.name}");
                    return;
                }

                // Check subscription for updates
                _subscription.Tick();
                await UniTask.Delay(2000);
            }

            Debug.LogWarning("[RemotePlayerOpponent] Timed out waiting for opponent setup");
        }

        // ============================================================
        // TURN EXECUTION
        // ============================================================

        public void ExecuteTurn(AIGameState gameState)
        {
            if (!_isInitialized)
            {
                Debug.LogError("[RemotePlayerOpponent] Cannot execute turn - not initialized");
                return;
            }

            // For remote opponent, we don't "execute" their turn
            // Instead, we wait for state updates via subscription
            _isThinking = true;
            _waitingForOpponentTurn = true;
            OnThinkingStarted?.Invoke();

            Debug.Log("[RemotePlayerOpponent] Waiting for opponent's turn...");

            // Start polling/waiting for opponent action
            WaitForOpponentActionAsync().Forget();
        }

        /// <summary>
        /// Waits for the opponent to take their turn action.
        /// </summary>
        private async UniTask WaitForOpponentActionAsync()
        {
            int pollIntervalMs = 500;
            int maxWaitMs = 300000; // 5 minutes
            float startTime = Time.realtimeSinceStartup;

            while (_waitingForOpponentTurn && !_isDisposed)
            {
                _subscription.Tick();
                await UniTask.Delay(pollIntervalMs);

                if ((Time.realtimeSinceStartup - startTime) * 1000 > maxWaitMs)
                {
                    Debug.LogWarning("[RemotePlayerOpponent] Opponent turn timeout");
                    _isThinking = false;
                    _waitingForOpponentTurn = false;
                    // Could trigger disconnect or forfeit here
                    break;
                }
            }
        }

        // ============================================================
        // STATE UPDATES
        // ============================================================

        private void HandleRemoteStateUpdate(DLYHGameState newState)
        {
            if (_isDisposed) return;

            Debug.Log($"[RemotePlayerOpponent] Remote state update: turn {newState.turnNumber}");

            // Get opponent's gameplay state
            var opponentState = _isLocalPlayerHost ? newState.player2?.gameplayState : newState.player1?.gameplayState;

            if (opponentState != null && _lastOpponentGameplayState != null)
            {
                // Detect what changed
                DetectOpponentAction(opponentState);
            }

            _lastOpponentGameplayState = opponentState;
            _lastGameState = newState;

            // Check if it's now local player's turn
            string currentTurn = newState.currentTurn;
            bool isLocalTurn = (_isLocalPlayerHost && currentTurn == "player1") ||
                               (!_isLocalPlayerHost && currentTurn == "player2");

            if (isLocalTurn && _waitingForOpponentTurn)
            {
                _waitingForOpponentTurn = false;
                _isThinking = false;
                OnThinkingComplete?.Invoke();
            }
        }

        /// <summary>
        /// Detects what action the opponent took by comparing gameplay states.
        /// </summary>
        private void DetectOpponentAction(DLYHGameplayState newState)
        {
            if (_lastOpponentGameplayState == null) return;

            // Check for new guessed coordinates
            if (newState.guessedCoordinates != null && _lastOpponentGameplayState.guessedCoordinates != null)
            {
                if (newState.guessedCoordinates.Length > _lastOpponentGameplayState.guessedCoordinates.Length)
                {
                    var lastCoord = newState.guessedCoordinates[newState.guessedCoordinates.Length - 1];
                    Debug.Log($"[RemotePlayerOpponent] Opponent guessed coordinate: ({lastCoord.row}, {lastCoord.col})");
                    OnCoordinateGuess?.Invoke(lastCoord.row, lastCoord.col);
                    return;
                }
            }

            // Check for new known letters (letter guess that revealed letters)
            if (newState.knownLetters != null && _lastOpponentGameplayState.knownLetters != null)
            {
                if (newState.knownLetters.Length > _lastOpponentGameplayState.knownLetters.Length)
                {
                    string lastLetter = newState.knownLetters[newState.knownLetters.Length - 1];
                    if (!string.IsNullOrEmpty(lastLetter))
                    {
                        Debug.Log($"[RemotePlayerOpponent] Opponent guessed letter: {lastLetter}");
                        OnLetterGuess?.Invoke(lastLetter[0]);
                        return;
                    }
                }
            }

            // Check for newly solved word rows
            if (newState.solvedWordRows != null && _lastOpponentGameplayState.solvedWordRows != null)
            {
                if (newState.solvedWordRows.Length > _lastOpponentGameplayState.solvedWordRows.Length)
                {
                    int lastRow = newState.solvedWordRows[newState.solvedWordRows.Length - 1];
                    Debug.Log($"[RemotePlayerOpponent] Opponent solved word row: {lastRow}");
                    OnWordGuess?.Invoke("", lastRow); // Word text not available until game end
                    return;
                }
            }
        }

        // ============================================================
        // CONNECTION HANDLING
        // ============================================================

        private void HandleConnectionLost()
        {
            Debug.LogWarning("[RemotePlayerOpponent] Connection lost");
            _isConnected = false;
            OnDisconnected?.Invoke();
        }

        private void HandleReconnection()
        {
            Debug.Log("[RemotePlayerOpponent] Reconnected");
            _isConnected = true;
            OnReconnected?.Invoke();
        }

        // ============================================================
        // FEEDBACK
        // ============================================================

        public void RecordPlayerGuess(bool wasHit)
        {
            // For remote, we push our state to server after each guess
            // This is handled by GameplayUIController pushing via synchronizer
        }

        public void RecordOpponentHit(int row, int col)
        {
            // State is received from server, no local tracking needed
        }

        public void RecordRevealedLetter(char letter)
        {
            // State is received from server
        }

        public void AdvanceTurn()
        {
            // Turn advancement is handled by server state
        }

        public void Reset()
        {
            _waitingForOpponentTurn = false;
            _isThinking = false;
            _lastOpponentGameplayState = null;
        }

        // ============================================================
        // DEBUG
        // ============================================================

        public string GetDebugSummary()
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("=== REMOTE PLAYER OPPONENT ===");
            sb.AppendLine($"Game Code: {_gameCode}");
            sb.AppendLine($"Opponent: {OpponentName}");
            sb.AppendLine($"Connected: {_isConnected}");
            sb.AppendLine($"Thinking: {_isThinking}");
            sb.AppendLine($"Grid: {GridSize}x{GridSize}");
            sb.AppendLine($"Words: {WordCount}");
            sb.AppendLine($"Miss Limit: {MissLimit}");
            return sb.ToString();
        }

        // ============================================================
        // TICK (call from Update)
        // ============================================================

        /// <summary>
        /// Must be called each frame to maintain subscription.
        /// </summary>
        public void Tick()
        {
            _subscription?.Tick();
        }

        // ============================================================
        // HELPERS
        // ============================================================

        private string ColorToHex(Color color)
        {
            return $"#{ColorUtility.ToHtmlStringRGB(color)}";
        }

        private Color HexToColor(string hex)
        {
            if (string.IsNullOrEmpty(hex)) return Color.white;
            if (!hex.StartsWith("#")) hex = "#" + hex;
            ColorUtility.TryParseHtmlString(hex, out Color color);
            return color;
        }

        private DifficultySetting ParseDifficulty(string difficulty)
        {
            return difficulty?.ToLower() switch
            {
                "easy" => DifficultySetting.Easy,
                "hard" => DifficultySetting.Hard,
                _ => DifficultySetting.Normal
            };
        }

        private string EncryptWordPlacements(List<WordPlacementData> placements)
        {
            // Simple base64 encoding for now - could use proper encryption
            if (placements == null || placements.Count == 0)
            {
                return "";
            }

            var sb = new System.Text.StringBuilder();
            foreach (var p in placements)
            {
                // DirCol=1 means horizontal, DirRow=1 means vertical
                string direction = p.DirCol == 1 ? "H" : "V";
                sb.AppendFormat("{0}:{1},{2},{3};",
                    p.Word,
                    p.StartRow,
                    p.StartCol,
                    direction
                );
            }

            return Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(sb.ToString()));
        }

        // ============================================================
        // DISPOSE
        // ============================================================

        public void Dispose()
        {
            if (_isDisposed) return;
            _isDisposed = true;

            _waitingForOpponentTurn = false;

            if (_subscription != null)
            {
                _subscription.OnConnectionLost -= HandleConnectionLost;
                _subscription.OnReconnected -= HandleReconnection;
                _subscription.StopAsync().Forget();
                _subscription.Dispose();
            }

            if (_synchronizer != null)
            {
                _synchronizer.OnRemoteStateReceived -= HandleRemoteStateUpdate;
                _synchronizer.Dispose();
            }

            Debug.Log("[RemotePlayerOpponent] Disposed");
        }
    }
}

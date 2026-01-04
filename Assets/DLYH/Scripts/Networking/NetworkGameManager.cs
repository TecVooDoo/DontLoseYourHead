// NetworkGameManager.cs
// Coordinates all networking components for multiplayer games
// Created: January 4, 2026
// Developer: TecVooDoo LLC

using System;
using UnityEngine;
using Cysharp.Threading.Tasks;
using DLYH.Networking.Services;
using DLYH.Networking.UI;
using TecVooDoo.DontLoseYourHead.UI;

namespace DLYH.Networking
{
    /// <summary>
    /// Game mode for session management.
    /// </summary>
    public enum GameMode
    {
        SinglePlayer,   // Playing against AI
        Multiplayer     // Playing against remote player
    }

    /// <summary>
    /// High-level manager that coordinates all networking components.
    /// Handles game lifecycle, disconnect recovery, and forfeit logic.
    /// </summary>
    public class NetworkGameManager : MonoBehaviour
    {
        // ============================================================
        // SINGLETON
        // ============================================================

        private static NetworkGameManager _instance;
        public static NetworkGameManager Instance => _instance;

        // ============================================================
        // EVENTS
        // ============================================================

        /// <summary>Fired when game is ready to start (both players connected and ready)</summary>
        public event Action OnGameReady;

        /// <summary>Fired when game ends (win, loss, or forfeit)</summary>
        public event Action<string> OnGameEnded; // reason

        /// <summary>Fired when opponent disconnects</summary>
        public event Action OnOpponentDisconnected;

        /// <summary>Fired when opponent reconnects</summary>
        public event Action OnOpponentReconnected;

        /// <summary>Fired when opponent forfeits (timeout)</summary>
        public event Action OnOpponentForfeit;

        // ============================================================
        // CONFIGURATION
        // ============================================================

        [Header("Configuration")]
        [SerializeField] private SupabaseConfig _supabaseConfig;
        [SerializeField] private float _disconnectGracePeriodSeconds = 60f;
        [SerializeField] private float _inactivityTimeoutSeconds = 259200f; // 3 days in seconds

        // ============================================================
        // STATE
        // ============================================================

        private GameMode _currentMode = GameMode.SinglePlayer;
        private string _currentGameCode;
        private bool _isHost;
        private bool _isInGame;
        private float _opponentDisconnectTime;
        private bool _opponentDisconnected;

        // Services
        private SupabaseClient _supabaseClient;
        private AuthService _authService;
        private GameSessionService _sessionService;
        private GameSubscription _subscription;
        private GameStateSynchronizer _synchronizer;

        // Current opponent
        private IOpponent _currentOpponent;

        // ============================================================
        // PROPERTIES
        // ============================================================

        public GameMode CurrentMode => _currentMode;
        public string CurrentGameCode => _currentGameCode;
        public bool IsHost => _isHost;
        public bool IsInGame => _isInGame;
        public bool IsMultiplayer => _currentMode == GameMode.Multiplayer;
        public IOpponent CurrentOpponent => _currentOpponent;
        public SupabaseConfig SupabaseConfig => _supabaseConfig;
        public GameStateSynchronizer Synchronizer => _synchronizer;

        // ============================================================
        // UNITY LIFECYCLE
        // ============================================================

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Update()
        {
            if (_isInGame && _currentMode == GameMode.Multiplayer)
            {
                // Tick subscription
                _subscription?.Tick();

                // Check disconnect timeout
                if (_opponentDisconnected)
                {
                    float disconnectedDuration = Time.realtimeSinceStartup - _opponentDisconnectTime;
                    if (disconnectedDuration > _disconnectGracePeriodSeconds)
                    {
                        HandleOpponentForfeit().Forget();
                    }
                }

                // Tick remote opponent if applicable
                if (_currentOpponent is RemotePlayerOpponent remote)
                {
                    remote.Tick();
                }
            }
        }

        private void OnDestroy()
        {
            Cleanup();
        }

        // ============================================================
        // INITIALIZATION
        // ============================================================

        /// <summary>
        /// Initializes services for multiplayer.
        /// </summary>
        public async UniTask InitializeAsync()
        {
            if (_supabaseConfig == null)
            {
                Debug.LogError("[NetworkGameManager] SupabaseConfig not assigned!");
                return;
            }

            _authService = new AuthService(_supabaseConfig);

            var session = await _authService.EnsureValidSessionAsync();
            if (session != null)
            {
                _supabaseClient = new SupabaseClient(_supabaseConfig, session.AccessToken);
                _sessionService = new GameSessionService(_supabaseClient, _supabaseConfig);
                Debug.Log($"[NetworkGameManager] Initialized with user {session.UserId}");
            }
        }

        // ============================================================
        // GAME SESSION MANAGEMENT
        // ============================================================

        /// <summary>
        /// Starts a single player game against AI.
        /// </summary>
        public void StartSinglePlayerGame(IOpponent aiOpponent)
        {
            _currentMode = GameMode.SinglePlayer;
            _currentGameCode = null;
            _isHost = true;
            _currentOpponent = aiOpponent;
            _isInGame = true;

            Debug.Log("[NetworkGameManager] Started single player game");
        }

        /// <summary>
        /// Starts a multiplayer game.
        /// </summary>
        /// <param name="gameCode">The game code</param>
        /// <param name="isHost">Whether local player is host (player 1)</param>
        public async UniTask StartMultiplayerGameAsync(string gameCode, bool isHost)
        {
            _currentMode = GameMode.Multiplayer;
            _currentGameCode = gameCode;
            _isHost = isHost;

            // Ensure services are initialized
            if (_authService == null)
            {
                await InitializeAsync();
            }

            // Get fresh auth
            var session = await _authService.EnsureValidSessionAsync();
            if (session == null)
            {
                Debug.LogError("[NetworkGameManager] Failed to authenticate");
                return;
            }

            // Create subscription
            _subscription = new GameSubscription(_supabaseConfig, gameCode, session.UserId, session.AccessToken);
            _subscription.OnConnectionLost += HandleConnectionLost;
            _subscription.OnReconnected += HandleReconnection;

            bool subscribed = await _subscription.StartAsync();
            if (!subscribed)
            {
                Debug.LogError("[NetworkGameManager] Failed to subscribe to game");
                return;
            }

            // Create synchronizer
            _synchronizer = new GameStateSynchronizer(
                _sessionService,
                _subscription,
                gameCode,
                session.UserId,
                isHost
            );

            // Create remote opponent
            _currentOpponent = OpponentFactory.CreateRemoteOpponent(_supabaseConfig, gameCode, isHost);

            if (_currentOpponent != null)
            {
                _currentOpponent.OnDisconnected += HandleOpponentDisconnected;
                _currentOpponent.OnReconnected += HandleOpponentReconnected;
            }

            _isInGame = true;

            Debug.Log($"[NetworkGameManager] Started multiplayer game: {gameCode} (host: {isHost})");
        }

        /// <summary>
        /// Ends the current game.
        /// </summary>
        /// <param name="reason">Reason for ending (win/loss/forfeit/quit)</param>
        public async UniTask EndGameAsync(string reason)
        {
            Debug.Log($"[NetworkGameManager] Ending game: {reason}");

            if (_currentMode == GameMode.Multiplayer && _synchronizer != null)
            {
                // Determine winner
                string winner = null;
                if (reason == "player_win")
                {
                    winner = _isHost ? "player1" : "player2";
                }
                else if (reason == "opponent_win" || reason == "player_forfeit")
                {
                    winner = _isHost ? "player2" : "player1";
                }
                else if (reason == "opponent_forfeit")
                {
                    winner = _isHost ? "player1" : "player2";
                }

                await _synchronizer.EndGameAsync(winner);
            }

            _isInGame = false;
            OnGameEnded?.Invoke(reason);

            Cleanup();
        }

        /// <summary>
        /// Forfeits the current game.
        /// </summary>
        public async UniTask ForfeitGameAsync()
        {
            await EndGameAsync("player_forfeit");
        }

        // ============================================================
        // CONNECTION HANDLING
        // ============================================================

        private void HandleConnectionLost()
        {
            Debug.LogWarning("[NetworkGameManager] Connection to server lost");
            // Connection to Supabase lost - will attempt reconnect automatically
        }

        private void HandleReconnection()
        {
            Debug.Log("[NetworkGameManager] Reconnected to server");
        }

        private void HandleOpponentDisconnected()
        {
            _opponentDisconnected = true;
            _opponentDisconnectTime = Time.realtimeSinceStartup;

            Debug.LogWarning("[NetworkGameManager] Opponent disconnected");
            OnOpponentDisconnected?.Invoke();
        }

        private void HandleOpponentReconnected()
        {
            _opponentDisconnected = false;

            Debug.Log("[NetworkGameManager] Opponent reconnected");
            OnOpponentReconnected?.Invoke();
        }

        private async UniTask HandleOpponentForfeit()
        {
            if (!_opponentDisconnected) return;

            Debug.Log("[NetworkGameManager] Opponent forfeited due to disconnect timeout");
            _opponentDisconnected = false;

            OnOpponentForfeit?.Invoke();
            await EndGameAsync("opponent_forfeit");
        }

        // ============================================================
        // STATE SYNC
        // ============================================================

        /// <summary>
        /// Pushes local game state to server.
        /// Call after each local player action.
        /// </summary>
        public async UniTask PushGameStateAsync(GameplayStateTracker stateTracker, bool isPlayerTurn)
        {
            if (_currentMode != GameMode.Multiplayer || _synchronizer == null)
            {
                return;
            }

            await _synchronizer.PushLocalStateAsync(stateTracker, isPlayerTurn);
        }

        /// <summary>
        /// Gets the current network game state.
        /// </summary>
        public async UniTask<DLYHGameState> FetchGameStateAsync()
        {
            if (_synchronizer == null) return null;
            return await _synchronizer.FetchCurrentStateAsync();
        }

        // ============================================================
        // ACTIVITY TRACKING
        // ============================================================

        /// <summary>
        /// Records local player activity (prevents inactivity forfeit).
        /// Call this when player takes any action.
        /// </summary>
        public void RecordLocalActivity()
        {
            // Activity is automatically recorded when pushing state
            // This is here for explicit tracking if needed
        }

        /// <summary>
        /// Checks if opponent has been inactive too long.
        /// </summary>
        public async UniTask<bool> CheckOpponentInactivityAsync()
        {
            if (_currentMode != GameMode.Multiplayer || _synchronizer == null)
            {
                return false;
            }

            var state = await _synchronizer.FetchCurrentStateAsync();
            if (state == null) return false;

            var opponentData = _isHost ? state.player2 : state.player1;
            if (opponentData == null) return false;

            // Parse last activity time
            if (DateTime.TryParse(opponentData.lastActivityAt, out DateTime lastActivity))
            {
                TimeSpan inactiveTime = DateTime.UtcNow - lastActivity;
                return inactiveTime.TotalSeconds > _inactivityTimeoutSeconds;
            }

            return false;
        }

        // ============================================================
        // CLEANUP
        // ============================================================

        private void Cleanup()
        {
            if (_currentOpponent != null)
            {
                _currentOpponent.OnDisconnected -= HandleOpponentDisconnected;
                _currentOpponent.OnReconnected -= HandleOpponentReconnected;
                _currentOpponent.Dispose();
                _currentOpponent = null;
            }

            if (_subscription != null)
            {
                _subscription.OnConnectionLost -= HandleConnectionLost;
                _subscription.OnReconnected -= HandleReconnection;
                _subscription.StopAsync().Forget();
                _subscription.Dispose();
                _subscription = null;
            }

            if (_synchronizer != null)
            {
                _synchronizer.Dispose();
                _synchronizer = null;
            }

            _opponentDisconnected = false;
            _currentGameCode = null;

            Debug.Log("[NetworkGameManager] Cleaned up");
        }
    }
}

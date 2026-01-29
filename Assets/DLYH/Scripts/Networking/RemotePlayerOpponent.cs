// RemotePlayerOpponent.cs
// Implements IOpponent for network multiplayer games
// Created: January 4, 2026
// Updated: January 16, 2026 - Enhanced turn detection
// Developer: TecVooDoo LLC

using System;
using System.Collections.Generic;
using UnityEngine;
using Cysharp.Threading.Tasks;
using DLYH.AI.Strategies;
using DLYH.Networking.Services;
using DLYH.Core.GameState;

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
        /// Lightweight initialization for use with existing services (Session 5).
        /// Call this instead of InitializeAsync() when UIFlowController already has services.
        /// This avoids creating duplicate SupabaseClient/GameSessionService instances.
        /// </summary>
        /// <param name="existingService">Existing GameSessionService from UIFlowController</param>
        /// <param name="opponentData">Opponent setup data already loaded from Supabase</param>
        /// <param name="missLimit">Pre-calculated miss limit for opponent</param>
        public void InitializeWithExistingService(GameSessionService existingService, PlayerSetupData opponentData, int missLimit)
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

            // Use existing service (no duplicate creation)
            _sessionService = existingService;

            // Set opponent data
            _opponentSetupData = opponentData;
            _missLimit = missLimit;

            // Mark as ready
            _isConnected = true;
            _isInitialized = true;

            Debug.Log($"[RemotePlayerOpponent] Lightweight init complete - Opponent: {OpponentName}, Grid: {GridSize}x{GridSize}, MissLimit: {_missLimit}");
        }

        /// <summary>
        /// Called by UIFlowController when polling detects a new game state.
        /// Compares states and fires appropriate events.
        /// </summary>
        /// <param name="newState">New game state from Supabase</param>
        public void ProcessStateUpdate(DLYHGameState newState)
        {
            if (_isDisposed || !_isInitialized) return;

            // Get opponent's gameplay state (opponent is the OTHER player)
            DLYHGameplayState opponentGameplayState = _isLocalPlayerHost
                ? newState.player2?.gameplayState
                : newState.player1?.gameplayState;

            if (opponentGameplayState != null && _lastOpponentGameplayState != null)
            {
                // Detect what changed
                DetectOpponentAction(opponentGameplayState);
            }

            _lastOpponentGameplayState = opponentGameplayState;
            _lastGameState = newState;

            // Check if opponent's turn is complete (turn switched back to local player)
            string currentTurn = newState.currentTurn;
            bool isLocalTurn = (_isLocalPlayerHost && currentTurn == "player1") ||
                               (!_isLocalPlayerHost && currentTurn == "player2");

            if (isLocalTurn && _waitingForOpponentTurn)
            {
                Debug.Log("[RemotePlayerOpponent] Turn returned to local player");
                _waitingForOpponentTurn = false;
                _isThinking = false;
                OnThinkingComplete?.Invoke();
            }
        }

        /// <summary>
        /// Sets the initial game state for comparison.
        /// Call this before starting turn detection.
        /// </summary>
        /// <param name="initialState">Initial game state</param>
        public void SetInitialState(DLYHGameState initialState)
        {
            _lastGameState = initialState;
            _lastOpponentGameplayState = _isLocalPlayerHost
                ? initialState.player2?.gameplayState
                : initialState.player1?.gameplayState;

            int revealedCount = _lastOpponentGameplayState?.revealedCells?.Length ?? 0;
            int letterCount = _lastOpponentGameplayState?.knownLetters?.Length ?? 0;
            Debug.Log($"[RemotePlayerOpponent] Initial state set - isHost={_isLocalPlayerHost}, turnNumber={initialState?.turnNumber ?? -1}, opponentRevealed={revealedCount}, opponentLetters={letterCount}");
        }

        /// <summary>
        /// Marks that we're waiting for opponent's turn.
        /// Called when local player's turn ends.
        /// </summary>
        public void StartWaitingForOpponentTurn()
        {
            _waitingForOpponentTurn = true;
            _isThinking = true;
            OnThinkingStarted?.Invoke();
            Debug.Log("[RemotePlayerOpponent] Now waiting for opponent's turn");
        }

        /// <summary>
        /// IOpponent interface requirement. Do NOT use - call InitializeWithExistingService instead.
        /// </summary>
        public UniTask InitializeAsync(PlayerSetupData localPlayerSetup)
        {
            Debug.LogError("[RemotePlayerOpponent] InitializeAsync should not be called - use InitializeWithExistingService instead");
            return UniTask.CompletedTask;
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
        /// Uses revealedCells (which is saved to Supabase) instead of guessedCoordinates.
        /// </summary>
        private void DetectOpponentAction(DLYHGameplayState newState)
        {
            // Initialize last state arrays if needed for comparison
            int lastRevealedCount = _lastOpponentGameplayState?.revealedCells?.Length ?? 0;
            int lastLetterCount = _lastOpponentGameplayState?.knownLetters?.Length ?? 0;
            int lastSolvedCount = _lastOpponentGameplayState?.solvedWordRows?.Length ?? 0;

            Debug.Log($"[RemotePlayerOpponent] DetectOpponentAction - lastRevealed={lastRevealedCount}, newRevealed={newState.revealedCells?.Length ?? 0}, lastLetters={lastLetterCount}, newLetters={newState.knownLetters?.Length ?? 0}");

            // Check for new revealed cells (coordinate guess)
            // revealedCells is populated by SaveGameStateToSupabaseAsync from _playerRevealedCells
            if (newState.revealedCells != null && newState.revealedCells.Length > lastRevealedCount)
            {
                RevealedCellData lastCell = newState.revealedCells[newState.revealedCells.Length - 1];
                Debug.Log($"[RemotePlayerOpponent] Opponent guessed coordinate: ({lastCell.row}, {lastCell.col}), letter={lastCell.letter}, isHit={lastCell.isHit}");
                OnCoordinateGuess?.Invoke(lastCell.row, lastCell.col);
                return;
            }

            // Check for new known letters (letter guess that revealed letters)
            if (newState.knownLetters != null && newState.knownLetters.Length > lastLetterCount)
            {
                // Multiple letters might have been revealed - report the most recent
                string lastLetter = newState.knownLetters[newState.knownLetters.Length - 1];
                if (!string.IsNullOrEmpty(lastLetter))
                {
                    Debug.Log($"[RemotePlayerOpponent] Opponent guessed letter: {lastLetter}");
                    OnLetterGuess?.Invoke(lastLetter[0]);
                    return;
                }
            }

            // Check for newly solved word rows (word guess)
            if (newState.solvedWordRows != null && newState.solvedWordRows.Length > lastSolvedCount)
            {
                int lastRow = newState.solvedWordRows[newState.solvedWordRows.Length - 1];
                Debug.Log($"[RemotePlayerOpponent] Opponent solved word row: {lastRow}");
                OnWordGuess?.Invoke("", lastRow); // Word text not available until game end
                return;
            }

            // Check for miss count increase (opponent made a miss)
            int lastMisses = _lastOpponentGameplayState?.misses ?? 0;
            if (newState.misses > lastMisses)
            {
                Debug.Log($"[RemotePlayerOpponent] Opponent misses increased: {lastMisses} -> {newState.misses}");
                // No event for this - turn will pass naturally
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

            Debug.Log("[RemotePlayerOpponent] Disposed");
        }
    }
}

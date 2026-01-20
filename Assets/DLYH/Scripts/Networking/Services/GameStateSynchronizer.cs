// GameStateSynchronizer.cs
// Synchronizes local game state with network state
// Created: January 4, 2026
// Updated: January 16, 2026 - Added opponent setup waiting
// Developer: TecVooDoo LLC

using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using Cysharp.Threading.Tasks;
using TecVooDoo.DontLoseYourHead.UI;
using DLYH.Core.Utilities;

namespace DLYH.Networking.Services
{
    /// <summary>
    /// Synchronizes local GameplayStateTracker with network DLYHGameState.
    /// Handles conversion between local state format and network JSON format.
    /// Manages state updates and conflict resolution.
    /// </summary>
    public class GameStateSynchronizer : IDisposable
    {
        // ============================================================
        // EVENTS
        // ============================================================

        /// <summary>Fired when remote state is received and applied</summary>
        public event Action<DLYHGameState> OnRemoteStateReceived;

        /// <summary>Fired when local state is pushed to server</summary>
        public event Action OnLocalStatePushed;

        /// <summary>Fired when sync error occurs</summary>
        public event Action<string> OnSyncError;

        /// <summary>Fired when opponent completes setup (provides opponent's DLYHPlayerData)</summary>
        public event Action<DLYHPlayerData> OnOpponentSetupComplete;

        /// <summary>Fired when both players are ready and game starts</summary>
        public event Action OnGameStarted;

        // ============================================================
        // STATE
        // ============================================================

        private readonly GameSessionService _sessionService;
        private readonly GameSubscription _subscription;
        private readonly string _gameCode;
        private readonly string _localPlayerId;
        private readonly bool _isPlayer1;

        private DLYHGameState _lastKnownState;
        private int _lastSyncedTurn;
        private bool _isDisposed;

        // ============================================================
        // PROPERTIES
        // ============================================================

        public DLYHGameState CurrentNetworkState => _lastKnownState;
        public bool IsPlayer1 => _isPlayer1;
        public string LocalPlayerKey => _isPlayer1 ? "player1" : "player2";
        public string RemotePlayerKey => _isPlayer1 ? "player2" : "player1";

        // ============================================================
        // CONSTRUCTOR
        // ============================================================

        public GameStateSynchronizer(
            GameSessionService sessionService,
            GameSubscription subscription,
            string gameCode,
            string localPlayerId,
            bool isPlayer1)
        {
            _sessionService = sessionService ?? throw new ArgumentNullException(nameof(sessionService));
            _subscription = subscription ?? throw new ArgumentNullException(nameof(subscription));
            _gameCode = gameCode ?? throw new ArgumentNullException(nameof(gameCode));
            _localPlayerId = localPlayerId ?? throw new ArgumentNullException(nameof(localPlayerId));
            _isPlayer1 = isPlayer1;

            // Subscribe to state updates
            _subscription.OnGameStateUpdated += HandleRemoteStateUpdate;
        }

        // ============================================================
        // INITIAL STATE FETCH
        // ============================================================

        /// <summary>
        /// Fetches the current game state from server.
        /// Call this when first connecting or reconnecting.
        /// </summary>
        public async UniTask<DLYHGameState> FetchCurrentStateAsync()
        {
            var session = await _sessionService.GetGame(_gameCode);
            if (session == null)
            {
                Debug.LogError($"[GameStateSynchronizer] Failed to fetch game {_gameCode}");
                return null;
            }

            _lastKnownState = ParseGameState(session.StateJson);
            _lastSyncedTurn = _lastKnownState?.turnNumber ?? 0;

            Debug.Log($"[GameStateSynchronizer] Fetched state: turn {_lastSyncedTurn}");
            return _lastKnownState;
        }

        // ============================================================
        // PUSH LOCAL STATE
        // ============================================================

        /// <summary>
        /// Pushes local player state to server after a turn.
        /// </summary>
        /// <param name="stateTracker">Local gameplay state</param>
        /// <param name="isPlayerTurn">Whether it's still player's turn after this action</param>
        public async UniTask<bool> PushLocalStateAsync(
            GameplayStateTracker stateTracker,
            bool isPlayerTurn)
        {
            if (_lastKnownState == null)
            {
                Debug.LogError("[GameStateSynchronizer] No known state to update");
                return false;
            }

            // Update local player's gameplay state
            var localPlayerData = _isPlayer1 ? _lastKnownState.player1 : _lastKnownState.player2;
            if (localPlayerData == null)
            {
                Debug.LogError("[GameStateSynchronizer] Local player data is null");
                return false;
            }

            // Build gameplay state from tracker
            localPlayerData.gameplayState = BuildGameplayState(stateTracker, !_isPlayer1);
            localPlayerData.lastActivityAt = DateTime.UtcNow.ToString("o");

            // Update turn info
            _lastKnownState.turnNumber++;
            _lastKnownState.currentTurn = isPlayerTurn ? LocalPlayerKey : RemotePlayerKey;
            _lastKnownState.updatedAt = DateTime.UtcNow.ToString("o");

            // Push to server
            bool success = await _sessionService.UpdateGameState(_gameCode, _lastKnownState);

            if (success)
            {
                _lastSyncedTurn = _lastKnownState.turnNumber;
                OnLocalStatePushed?.Invoke();
                Debug.Log($"[GameStateSynchronizer] Pushed state: turn {_lastSyncedTurn}");
            }
            else
            {
                OnSyncError?.Invoke("Failed to push state to server");
            }

            return success;
        }

        /// <summary>
        /// Pushes setup data for local player.
        /// </summary>
        public async UniTask<bool> PushSetupDataAsync(
            string playerName,
            string playerColor,
            int gridSize,
            int wordCount,
            string difficulty,
            string encryptedPlacements)
        {
            if (_lastKnownState == null)
            {
                await FetchCurrentStateAsync();
            }

            if (_lastKnownState == null)
            {
                Debug.LogError("[GameStateSynchronizer] Cannot push setup - no game state");
                return false;
            }

            DLYHPlayerData playerData = new DLYHPlayerData
            {
                name = playerName,
                color = playerColor,
                gridSize = gridSize,
                wordCount = wordCount,
                difficulty = difficulty,
                ready = false,
                setupComplete = true,
                lastActivityAt = DateTime.UtcNow.ToString("o"),
                wordPlacementsEncrypted = encryptedPlacements,
                gameplayState = null
            };

            if (_isPlayer1)
            {
                _lastKnownState.player1 = playerData;
            }
            else
            {
                _lastKnownState.player2 = playerData;
            }

            _lastKnownState.updatedAt = DateTime.UtcNow.ToString("o");

            bool success = await _sessionService.UpdateGameState(_gameCode, _lastKnownState);

            if (success)
            {
                Debug.Log($"[GameStateSynchronizer] Setup data pushed for {LocalPlayerKey}");
            }

            return success;
        }

        /// <summary>
        /// Marks local player as ready to play.
        /// </summary>
        public async UniTask<bool> SetPlayerReadyAsync()
        {
            if (_lastKnownState == null)
            {
                return false;
            }

            var localPlayer = _isPlayer1 ? _lastKnownState.player1 : _lastKnownState.player2;
            if (localPlayer != null)
            {
                localPlayer.ready = true;
                localPlayer.lastActivityAt = DateTime.UtcNow.ToString("o");
            }

            // Check if both players are ready - if so, start the game
            var player1 = _lastKnownState.player1;
            var player2 = _lastKnownState.player2;

            if (player1?.ready == true && player2?.ready == true)
            {
                _lastKnownState.status = "playing";
                _lastKnownState.currentTurn = "player1"; // Player 1 goes first
                _lastKnownState.turnNumber = 1;

                // Initialize gameplay states
                if (player1.gameplayState == null)
                {
                    player1.gameplayState = new DLYHGameplayState
                    {
                        misses = 0,
                        missLimit = CalculateMissLimit(player2), // Based on opponent's grid
                        knownLetters = Array.Empty<string>(),
                        guessedCoordinates = Array.Empty<CoordinatePair>(),
                        solvedWordRows = Array.Empty<int>()
                    };
                }
                if (player2.gameplayState == null)
                {
                    player2.gameplayState = new DLYHGameplayState
                    {
                        misses = 0,
                        missLimit = CalculateMissLimit(player1),
                        knownLetters = Array.Empty<string>(),
                        guessedCoordinates = Array.Empty<CoordinatePair>(),
                        solvedWordRows = Array.Empty<int>()
                    };
                }

                bool success = await _sessionService.UpdateGameState(_gameCode, _lastKnownState, "active");
                if (success)
                {
                    OnGameStarted?.Invoke();
                }
                return success;
            }

            return await _sessionService.UpdateGameState(_gameCode, _lastKnownState);
        }

        /// <summary>
        /// Waits for opponent to complete their setup.
        /// Polls the server until opponent's setupComplete is true.
        /// </summary>
        /// <param name="timeoutSeconds">Max time to wait (default 300 = 5 minutes)</param>
        /// <param name="pollIntervalMs">Polling interval in ms (default 2000)</param>
        /// <returns>Opponent's player data, or null on timeout</returns>
        public async UniTask<DLYHPlayerData> WaitForOpponentSetupAsync(
            float timeoutSeconds = 300f,
            int pollIntervalMs = 2000)
        {
            float elapsedTime = 0f;

            Debug.Log("[GameStateSynchronizer] Waiting for opponent setup...");

            while (elapsedTime < timeoutSeconds)
            {
                await UniTask.Delay(pollIntervalMs);
                elapsedTime += pollIntervalMs / 1000f;

                // Fetch current state
                DLYHGameState state = await FetchCurrentStateAsync();
                if (state == null)
                {
                    continue;
                }

                // Check opponent's setup status
                DLYHPlayerData opponentData = _isPlayer1 ? state.player2 : state.player1;
                if (opponentData != null && opponentData.setupComplete)
                {
                    Debug.Log($"[GameStateSynchronizer] Opponent setup complete: {opponentData.name}");
                    OnOpponentSetupComplete?.Invoke(opponentData);
                    return opponentData;
                }
            }

            Debug.LogWarning("[GameStateSynchronizer] Timed out waiting for opponent setup");
            return null;
        }

        /// <summary>
        /// Checks if opponent has completed setup (non-blocking).
        /// </summary>
        /// <returns>True if opponent setup is complete</returns>
        public bool IsOpponentSetupComplete()
        {
            if (_lastKnownState == null) return false;

            var opponentData = _isPlayer1 ? _lastKnownState.player2 : _lastKnownState.player1;
            return opponentData != null && opponentData.setupComplete;
        }

        /// <summary>
        /// Gets opponent's setup data if available.
        /// Returns the full DLYHPlayerData which contains setup fields directly.
        /// </summary>
        public DLYHPlayerData GetOpponentSetupData()
        {
            if (_lastKnownState == null) return null;

            DLYHPlayerData opponentData = _isPlayer1 ? _lastKnownState.player2 : _lastKnownState.player1;
            return opponentData;
        }

        /// <summary>
        /// Gets opponent's player data if available.
        /// </summary>
        public DLYHPlayerData GetOpponentPlayerData()
        {
            if (_lastKnownState == null) return null;
            return _isPlayer1 ? _lastKnownState.player2 : _lastKnownState.player1;
        }

        /// <summary>
        /// Ends the game with a winner.
        /// </summary>
        public async UniTask<bool> EndGameAsync(string winner)
        {
            if (_lastKnownState == null) return false;

            _lastKnownState.status = "finished";
            _lastKnownState.winner = winner;
            _lastKnownState.updatedAt = DateTime.UtcNow.ToString("o");

            return await _sessionService.UpdateGameState(_gameCode, _lastKnownState, "completed");
        }

        // ============================================================
        // HANDLE REMOTE UPDATES
        // ============================================================

        private void HandleRemoteStateUpdate(string stateJson)
        {
            try
            {
                var newState = ParseGameState(stateJson);
                if (newState == null)
                {
                    Debug.LogWarning("[GameStateSynchronizer] Failed to parse remote state");
                    return;
                }

                // Check for turn advancement
                if (newState.turnNumber > _lastSyncedTurn)
                {
                    Debug.Log($"[GameStateSynchronizer] Remote turn advanced: {_lastSyncedTurn} -> {newState.turnNumber}");
                    _lastKnownState = newState;
                    _lastSyncedTurn = newState.turnNumber;
                    OnRemoteStateReceived?.Invoke(newState);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[GameStateSynchronizer] Error handling remote update: {ex.Message}");
                OnSyncError?.Invoke(ex.Message);
            }
        }

        // ============================================================
        // STATE CONVERSION
        // ============================================================

        /// <summary>
        /// Builds DLYHGameplayState from local GameplayStateTracker.
        /// </summary>
        private DLYHGameplayState BuildGameplayState(GameplayStateTracker tracker, bool usePlayerState)
        {
            var state = new DLYHGameplayState();

            if (usePlayerState)
            {
                state.misses = tracker.PlayerMisses;
                state.missLimit = tracker.PlayerMissLimit;
                state.knownLetters = HashSetToStringArray(tracker.PlayerKnownLetters);
                state.guessedCoordinates = HashSetToCoordinateArray(tracker.PlayerGuessedCoordinates);
                state.solvedWordRows = HashSetToIntArray(tracker.PlayerSolvedWordRows);
                state.revealedCells = DictionaryToRevealedCellArray(tracker.PlayerRevealedCells);
            }
            else
            {
                state.misses = tracker.OpponentMisses;
                state.missLimit = tracker.OpponentMissLimit;
                state.knownLetters = HashSetToStringArray(tracker.OpponentKnownLetters);
                state.guessedCoordinates = HashSetToCoordinateArray(tracker.OpponentGuessedCoordinates);
                state.solvedWordRows = HashSetToIntArray(tracker.OpponentSolvedWordRows);
                state.revealedCells = DictionaryToRevealedCellArray(tracker.OpponentRevealedCells);
            }

            return state;
        }

        /// <summary>
        /// Applies remote gameplay state to local tracker.
        /// </summary>
        public void ApplyRemoteStateToTracker(GameplayStateTracker tracker, DLYHGameplayState remoteState, bool applyToPlayer)
        {
            if (remoteState == null) return;

            if (applyToPlayer)
            {
                // This shouldn't normally happen - player state is local
                Debug.LogWarning("[GameStateSynchronizer] Applying remote state to player (unusual)");
            }
            else
            {
                // Apply to opponent state
                tracker.InitializeOpponentState(remoteState.missLimit);

                // Add known letters
                if (remoteState.knownLetters != null)
                {
                    foreach (var letter in remoteState.knownLetters)
                    {
                        if (!string.IsNullOrEmpty(letter))
                        {
                            tracker.OpponentKnownLetters.Add(letter[0]);
                        }
                    }
                }

                // Add guessed coordinates
                if (remoteState.guessedCoordinates != null)
                {
                    foreach (var coord in remoteState.guessedCoordinates)
                    {
                        tracker.OpponentGuessedCoordinates.Add(new Vector2Int(coord.row, coord.col));
                    }
                }

                // Add solved word rows
                if (remoteState.solvedWordRows != null)
                {
                    foreach (var row in remoteState.solvedWordRows)
                    {
                        tracker.OpponentSolvedWordRows.Add(row);
                    }
                }

                // Add revealed cells
                if (remoteState.revealedCells != null)
                {
                    foreach (var cell in remoteState.revealedCells)
                    {
                        Vector2Int pos = new Vector2Int(cell.col, cell.row);
                        char letter = string.IsNullOrEmpty(cell.letter) ? '\0' : cell.letter[0];
                        tracker.RecordOpponentRevealedCell(pos, letter, cell.isHit);
                    }
                }

                // Set misses
                for (int i = 0; i < remoteState.misses; i++)
                {
                    tracker.AddOpponentMisses(1);
                }
            }
        }

        // ============================================================
        // PARSING
        // ============================================================

        private DLYHGameState ParseGameState(string json)
        {
            if (string.IsNullOrEmpty(json))
            {
                return null;
            }

            try
            {
                var state = new DLYHGameState();
                state.version = JsonParsingUtility.ExtractIntField(json, "version");
                state.status = JsonParsingUtility.ExtractStringField(json, "status");
                state.currentTurn = JsonParsingUtility.ExtractStringField(json, "currentTurn");
                state.turnNumber = JsonParsingUtility.ExtractIntField(json, "turnNumber");
                state.createdAt = JsonParsingUtility.ExtractStringField(json, "createdAt");
                state.updatedAt = JsonParsingUtility.ExtractStringField(json, "updatedAt");
                state.winner = JsonParsingUtility.ExtractStringField(json, "winner");

                // Parse player data
                string player1Json = JsonParsingUtility.ExtractObjectField(json, "player1");
                string player2Json = JsonParsingUtility.ExtractObjectField(json, "player2");

                state.player1 = ParsePlayerData(player1Json);
                state.player2 = ParsePlayerData(player2Json);

                return state;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[GameStateSynchronizer] Parse error: {ex.Message}");
                return null;
            }
        }

        private DLYHPlayerData ParsePlayerData(string json)
        {
            if (string.IsNullOrEmpty(json) || json == "null")
            {
                return null;
            }

            DLYHPlayerData data = new DLYHPlayerData();

            // Identity
            data.name = JsonParsingUtility.ExtractStringField(json, "name");
            data.color = JsonParsingUtility.ExtractStringField(json, "color");

            // Setup config (flat structure, no nested setupData)
            data.gridSize = JsonParsingUtility.ExtractIntField(json, "gridSize");
            data.wordCount = JsonParsingUtility.ExtractIntField(json, "wordCount");
            data.difficulty = JsonParsingUtility.ExtractStringField(json, "difficulty");

            // Dynamic state
            data.ready = JsonParsingUtility.ExtractBoolField(json, "ready");
            data.setupComplete = JsonParsingUtility.ExtractBoolField(json, "setupComplete");
            data.lastActivityAt = JsonParsingUtility.ExtractStringField(json, "lastActivityAt");
            data.wordPlacementsEncrypted = JsonParsingUtility.ExtractStringField(json, "wordPlacementsEncrypted");

            string gameplayJson = JsonParsingUtility.ExtractObjectField(json, "gameplayState");
            if (!string.IsNullOrEmpty(gameplayJson) && gameplayJson != "null")
            {
                data.gameplayState = ParseGameplayState(gameplayJson);
            }

            return data;
        }

        private DLYHGameplayState ParseGameplayState(string json)
        {
            var state = new DLYHGameplayState();
            state.misses = JsonParsingUtility.ExtractIntField(json, "misses");
            state.missLimit = JsonParsingUtility.ExtractIntField(json, "missLimit");
            state.knownLetters = JsonParsingUtility.ExtractStringArray(json, "knownLetters");
            state.guessedCoordinates = ConvertToCoordinatePairs(
                JsonParsingUtility.ExtractCoordinateArray(json, "guessedCoordinates"));
            state.solvedWordRows = JsonParsingUtility.ExtractIntArray(json, "solvedWordRows");
            return state;
        }

        /// <summary>
        /// Converts tuple array from JsonParsingUtility to CoordinatePair array.
        /// </summary>
        private CoordinatePair[] ConvertToCoordinatePairs((int row, int col)[] tuples)
        {
            CoordinatePair[] result = new CoordinatePair[tuples.Length];
            for (int i = 0; i < tuples.Length; i++)
            {
                result[i] = new CoordinatePair(tuples[i].row, tuples[i].col);
            }
            return result;
        }

        // ============================================================
        // HELPER METHODS
        // ============================================================

        private int CalculateMissLimit(DLYHPlayerData playerData)
        {
            if (playerData == null) return 6;

            // Match the formula from GameplayStateTracker
            int gridSize = playerData.gridSize;
            int wordCount = playerData.wordCount;

            // Base formula: smaller grid = fewer misses allowed
            int baseMisses = gridSize switch
            {
                <= 6 => 4,
                <= 8 => 5,
                <= 10 => 6,
                _ => 7
            };

            // Adjust for word count
            baseMisses += wordCount - 3;

            // Clamp to reasonable range
            return Mathf.Clamp(baseMisses, 3, 10);
        }

        private string[] HashSetToStringArray(HashSet<char> set)
        {
            var result = new string[set.Count];
            int i = 0;
            foreach (var c in set)
            {
                result[i++] = c.ToString();
            }
            return result;
        }

        private CoordinatePair[] HashSetToCoordinateArray(HashSet<Vector2Int> set)
        {
            var result = new CoordinatePair[set.Count];
            int i = 0;
            foreach (var coord in set)
            {
                result[i++] = new CoordinatePair(coord.x, coord.y);
            }
            return result;
        }

        private int[] HashSetToIntArray(HashSet<int> set)
        {
            var result = new int[set.Count];
            int i = 0;
            foreach (var val in set)
            {
                result[i++] = val;
            }
            return result;
        }

        private RevealedCellData[] DictionaryToRevealedCellArray(Dictionary<Vector2Int, RevealedCellInfo> dict)
        {
            var result = new RevealedCellData[dict.Count];
            int i = 0;
            foreach (var kvp in dict)
            {
                result[i++] = new RevealedCellData(
                    kvp.Key.y,  // row (Vector2Int.y)
                    kvp.Key.x,  // col (Vector2Int.x)
                    kvp.Value.Letter == '\0' ? "" : kvp.Value.Letter.ToString(),
                    kvp.Value.IsHit);
            }
            return result;
        }

        // ============================================================
        // DISPOSE
        // ============================================================

        public void Dispose()
        {
            if (_isDisposed) return;
            _isDisposed = true;

            if (_subscription != null)
            {
                _subscription.OnGameStateUpdated -= HandleRemoteStateUpdate;
            }
        }
    }
}

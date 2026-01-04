// GameStateSynchronizer.cs
// Synchronizes local game state with network state
// Created: January 4, 2026
// Developer: TecVooDoo LLC

using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using Cysharp.Threading.Tasks;
using TecVooDoo.DontLoseYourHead.UI;

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

            var playerData = new DLYHPlayerData
            {
                name = playerName,
                color = playerColor,
                ready = false,
                setupComplete = true,
                lastActivityAt = DateTime.UtcNow.ToString("o"),
                setupData = new DLYHSetupData
                {
                    gridSize = gridSize,
                    wordCount = wordCount,
                    difficulty = difficulty,
                    wordPlacementsEncrypted = encryptedPlacements
                },
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
                        missLimit = CalculateMissLimit(player2.setupData), // Based on opponent's grid
                        knownLetters = new string[0],
                        guessedCoordinates = new CoordinatePair[0],
                        solvedWordRows = new int[0]
                    };
                }
                if (player2.gameplayState == null)
                {
                    player2.gameplayState = new DLYHGameplayState
                    {
                        misses = 0,
                        missLimit = CalculateMissLimit(player1.setupData),
                        knownLetters = new string[0],
                        guessedCoordinates = new CoordinatePair[0],
                        solvedWordRows = new int[0]
                    };
                }

                return await _sessionService.UpdateGameState(_gameCode, _lastKnownState, "active");
            }

            return await _sessionService.UpdateGameState(_gameCode, _lastKnownState);
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
            }
            else
            {
                state.misses = tracker.OpponentMisses;
                state.missLimit = tracker.OpponentMissLimit;
                state.knownLetters = HashSetToStringArray(tracker.OpponentKnownLetters);
                state.guessedCoordinates = HashSetToCoordinateArray(tracker.OpponentGuessedCoordinates);
                state.solvedWordRows = HashSetToIntArray(tracker.OpponentSolvedWordRows);
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
                state.version = ExtractIntField(json, "version");
                state.status = ExtractStringField(json, "status");
                state.currentTurn = ExtractStringField(json, "currentTurn");
                state.turnNumber = ExtractIntField(json, "turnNumber");
                state.createdAt = ExtractStringField(json, "createdAt");
                state.updatedAt = ExtractStringField(json, "updatedAt");
                state.winner = ExtractStringField(json, "winner");

                // Parse player data
                string player1Json = ExtractObjectField(json, "player1");
                string player2Json = ExtractObjectField(json, "player2");

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

            var data = new DLYHPlayerData();
            data.name = ExtractStringField(json, "name");
            data.color = ExtractStringField(json, "color");
            data.ready = ExtractBoolField(json, "ready");
            data.setupComplete = ExtractBoolField(json, "setupComplete");
            data.lastActivityAt = ExtractStringField(json, "lastActivityAt");

            string setupJson = ExtractObjectField(json, "setupData");
            if (!string.IsNullOrEmpty(setupJson) && setupJson != "null")
            {
                data.setupData = new DLYHSetupData
                {
                    gridSize = ExtractIntField(setupJson, "gridSize"),
                    wordCount = ExtractIntField(setupJson, "wordCount"),
                    difficulty = ExtractStringField(setupJson, "difficulty"),
                    wordPlacementsEncrypted = ExtractStringField(setupJson, "wordPlacementsEncrypted")
                };
            }

            string gameplayJson = ExtractObjectField(json, "gameplayState");
            if (!string.IsNullOrEmpty(gameplayJson) && gameplayJson != "null")
            {
                data.gameplayState = ParseGameplayState(gameplayJson);
            }

            return data;
        }

        private DLYHGameplayState ParseGameplayState(string json)
        {
            var state = new DLYHGameplayState();
            state.misses = ExtractIntField(json, "misses");
            state.missLimit = ExtractIntField(json, "missLimit");
            state.knownLetters = ExtractStringArray(json, "knownLetters");
            state.guessedCoordinates = ExtractCoordinateArray(json, "guessedCoordinates");
            state.solvedWordRows = ExtractIntArray(json, "solvedWordRows");
            return state;
        }

        // ============================================================
        // HELPER METHODS
        // ============================================================

        private int CalculateMissLimit(DLYHSetupData setupData)
        {
            if (setupData == null) return 6;

            // Match the formula from GameplayStateTracker
            int gridSize = setupData.gridSize;
            int wordCount = setupData.wordCount;

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

        // ============================================================
        // JSON FIELD EXTRACTION
        // ============================================================

        private string ExtractStringField(string json, string key)
        {
            string pattern = $"\"{key}\":\"";
            int start = json.IndexOf(pattern);
            if (start < 0) return null;
            start += pattern.Length;
            int end = json.IndexOf("\"", start);
            if (end < 0) return null;
            return json.Substring(start, end - start);
        }

        private int ExtractIntField(string json, string key)
        {
            string pattern = $"\"{key}\":";
            int start = json.IndexOf(pattern);
            if (start < 0) return 0;
            start += pattern.Length;
            int end = start;
            while (end < json.Length && (char.IsDigit(json[end]) || json[end] == '-'))
            {
                end++;
            }
            if (end == start) return 0;
            return int.Parse(json.Substring(start, end - start));
        }

        private bool ExtractBoolField(string json, string key)
        {
            string pattern = $"\"{key}\":";
            int start = json.IndexOf(pattern);
            if (start < 0) return false;
            start += pattern.Length;
            return json.Substring(start, 4) == "true";
        }

        private string ExtractObjectField(string json, string key)
        {
            string pattern = $"\"{key}\":";
            int start = json.IndexOf(pattern);
            if (start < 0) return null;
            start += pattern.Length;

            // Skip whitespace
            while (start < json.Length && char.IsWhiteSpace(json[start])) start++;

            if (start >= json.Length) return null;

            if (json[start] == 'n') return "null"; // null value

            if (json[start] != '{') return null;

            int depth = 0;
            int end = start;
            while (end < json.Length)
            {
                if (json[end] == '{') depth++;
                else if (json[end] == '}') depth--;
                if (depth == 0) break;
                end++;
            }
            return json.Substring(start, end - start + 1);
        }

        private string[] ExtractStringArray(string json, string key)
        {
            string pattern = $"\"{key}\":[";
            int start = json.IndexOf(pattern);
            if (start < 0) return new string[0];
            start += pattern.Length;

            int end = json.IndexOf("]", start);
            if (end < 0) return new string[0];

            string content = json.Substring(start, end - start);
            if (string.IsNullOrWhiteSpace(content)) return new string[0];

            var result = new List<string>();
            int pos = 0;
            while (pos < content.Length)
            {
                int quoteStart = content.IndexOf("\"", pos);
                if (quoteStart < 0) break;
                int quoteEnd = content.IndexOf("\"", quoteStart + 1);
                if (quoteEnd < 0) break;
                result.Add(content.Substring(quoteStart + 1, quoteEnd - quoteStart - 1));
                pos = quoteEnd + 1;
            }
            return result.ToArray();
        }

        private int[] ExtractIntArray(string json, string key)
        {
            string pattern = $"\"{key}\":[";
            int start = json.IndexOf(pattern);
            if (start < 0) return new int[0];
            start += pattern.Length;

            int end = json.IndexOf("]", start);
            if (end < 0) return new int[0];

            string content = json.Substring(start, end - start);
            if (string.IsNullOrWhiteSpace(content)) return new int[0];

            var result = new List<int>();
            var parts = content.Split(',');
            foreach (var part in parts)
            {
                if (int.TryParse(part.Trim(), out int val))
                {
                    result.Add(val);
                }
            }
            return result.ToArray();
        }

        private CoordinatePair[] ExtractCoordinateArray(string json, string key)
        {
            string pattern = $"\"{key}\":[";
            int start = json.IndexOf(pattern);
            if (start < 0) return new CoordinatePair[0];
            start += pattern.Length;

            // Find matching ]
            int depth = 1;
            int end = start;
            while (end < json.Length && depth > 0)
            {
                if (json[end] == '[') depth++;
                else if (json[end] == ']') depth--;
                end++;
            }
            end--;

            string content = json.Substring(start, end - start);
            if (string.IsNullOrWhiteSpace(content)) return new CoordinatePair[0];

            var result = new List<CoordinatePair>();

            // Parse [[row,col],[row,col],...]
            int pos = 0;
            while (pos < content.Length)
            {
                int innerStart = content.IndexOf("[", pos);
                if (innerStart < 0) break;
                int innerEnd = content.IndexOf("]", innerStart);
                if (innerEnd < 0) break;

                string pair = content.Substring(innerStart + 1, innerEnd - innerStart - 1);
                var parts = pair.Split(',');
                if (parts.Length == 2 &&
                    int.TryParse(parts[0].Trim(), out int row) &&
                    int.TryParse(parts[1].Trim(), out int col))
                {
                    result.Add(new CoordinatePair(row, col));
                }
                pos = innerEnd + 1;
            }

            return result.ToArray();
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

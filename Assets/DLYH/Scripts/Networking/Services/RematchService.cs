// RematchService.cs
// Handles rematch requests and acceptance flow
// Created: January 16, 2026
// Developer: TecVooDoo LLC

using System;
using UnityEngine;
using Cysharp.Threading.Tasks;

namespace DLYH.Networking.Services
{
    /// <summary>
    /// Rematch status enum.
    /// </summary>
    public enum RematchStatus
    {
        None,
        RequestedByMe,
        RequestedByOpponent,
        Accepted,
        Declined,
        Expired
    }

    /// <summary>
    /// Result of a rematch request.
    /// </summary>
    public class RematchResult
    {
        public bool Success;
        public string NewGameCode;
        public bool IsHost;
        public string ErrorMessage;
    }

    /// <summary>
    /// Service for handling rematch requests after a game ends.
    /// Both players can request a rematch, and if both accept,
    /// a new game is created with swapped first-turn.
    /// </summary>
    public class RematchService
    {
        // ============================================================
        // CONSTANTS
        // ============================================================

        private const string TABLE_REMATCH_REQUESTS = "rematch_requests";
        private const float REMATCH_TIMEOUT_SECONDS = 30f;
        private const float REMATCH_POLL_INTERVAL_MS = 1000f;

        // ============================================================
        // EVENTS
        // ============================================================

        /// <summary>Fired when rematch status changes</summary>
        public event Action<RematchStatus> OnRematchStatusChanged;

        /// <summary>Fired when opponent requests rematch</summary>
        public event Action OnOpponentRequestedRematch;

        /// <summary>Fired when rematch is accepted by both players</summary>
        public event Action<RematchResult> OnRematchAccepted;

        /// <summary>Fired when rematch is declined or expires</summary>
        public event Action<string> OnRematchDeclined;

        // ============================================================
        // STATE
        // ============================================================

        private readonly SupabaseClient _client;
        private readonly SupabaseConfig _config;
        private readonly GameSessionService _sessionService;
        private readonly PlayerService _playerService;

        private RematchStatus _currentStatus = RematchStatus.None;
        private string _currentGameCode;
        private string _currentRequestId;
        private bool _isPolling;

        // ============================================================
        // PROPERTIES
        // ============================================================

        public RematchStatus CurrentStatus => _currentStatus;
        public bool IsRematchPending => _currentStatus == RematchStatus.RequestedByMe ||
                                        _currentStatus == RematchStatus.RequestedByOpponent;

        // ============================================================
        // CONSTRUCTOR
        // ============================================================

        public RematchService(
            SupabaseClient client,
            SupabaseConfig config,
            GameSessionService sessionService,
            PlayerService playerService)
        {
            _client = client ?? throw new ArgumentNullException(nameof(client));
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _sessionService = sessionService ?? throw new ArgumentNullException(nameof(sessionService));
            _playerService = playerService ?? throw new ArgumentNullException(nameof(playerService));
        }

        // ============================================================
        // REMATCH REQUEST
        // ============================================================

        /// <summary>
        /// Requests a rematch with the opponent from the specified game.
        /// </summary>
        /// <param name="gameCode">The game code of the just-finished game</param>
        /// <returns>True if request was created successfully</returns>
        public async UniTask<bool> RequestRematchAsync(string gameCode)
        {
            if (string.IsNullOrEmpty(gameCode))
            {
                Debug.LogError("[RematchService] Game code is required");
                return false;
            }

            _currentGameCode = gameCode;
            string playerId = _playerService.CurrentPlayerId;

            if (string.IsNullOrEmpty(playerId))
            {
                Debug.LogError("[RematchService] Not signed in");
                return false;
            }

            try
            {
                // Check if opponent already requested a rematch
                RematchStatus existingStatus = await CheckRematchStatusAsync(gameCode, playerId);
                if (existingStatus == RematchStatus.RequestedByOpponent)
                {
                    // Accept the existing request
                    return await AcceptRematchAsync(gameCode);
                }

                // Create new rematch request
                string json = $"{{\"game_id\":\"{gameCode}\",\"requester_id\":\"{playerId}\",\"status\":\"pending\"}}";

                SupabaseResponse response = await _client.Post(TABLE_REMATCH_REQUESTS, json);

                if (!response.Success)
                {
                    Debug.LogError($"[RematchService] Failed to create rematch request: {response.Error}");
                    return false;
                }

                _currentRequestId = ExtractJsonValue(response.Body, "id");
                SetStatus(RematchStatus.RequestedByMe);

                Debug.Log($"[RematchService] Rematch requested for game: {gameCode}");

                // Start polling for opponent's response
                PollForRematchResponseAsync().Forget();

                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[RematchService] Error requesting rematch: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Accepts a rematch request from the opponent.
        /// </summary>
        /// <param name="gameCode">The game code of the original game</param>
        /// <returns>True if acceptance was successful</returns>
        public async UniTask<bool> AcceptRematchAsync(string gameCode)
        {
            if (string.IsNullOrEmpty(gameCode))
            {
                Debug.LogError("[RematchService] Game code is required");
                return false;
            }

            string playerId = _playerService.CurrentPlayerId;

            try
            {
                // Find the opponent's rematch request
                string query = $"game_id=eq.{gameCode}&requester_id=neq.{playerId}&status=eq.pending";
                SupabaseResponse response = await _client.Get(TABLE_REMATCH_REQUESTS, query);

                if (!response.Success || string.IsNullOrEmpty(response.Body) || response.Body == "[]")
                {
                    Debug.LogWarning("[RematchService] No pending rematch request found");
                    return false;
                }

                string requestId = ExtractJsonValue(response.Body, "id");
                string requesterId = ExtractJsonValue(response.Body, "requester_id");

                if (string.IsNullOrEmpty(requestId))
                {
                    return false;
                }

                // Update the request to accepted
                string updateJson = $"{{\"status\":\"accepted\",\"accepter_id\":\"{playerId}\"}}";
                SupabaseResponse updateResponse = await _client.Patch(TABLE_REMATCH_REQUESTS, $"id=eq.{requestId}", updateJson);

                if (!updateResponse.Success)
                {
                    Debug.LogError($"[RematchService] Failed to accept rematch: {updateResponse.Error}");
                    return false;
                }

                // Create the new game
                RematchResult result = await CreateRematchGameAsync(gameCode, requesterId, playerId);

                if (result.Success)
                {
                    SetStatus(RematchStatus.Accepted);
                    OnRematchAccepted?.Invoke(result);
                    Debug.Log($"[RematchService] Rematch accepted, new game: {result.NewGameCode}");
                }

                return result.Success;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[RematchService] Error accepting rematch: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Declines a rematch request.
        /// </summary>
        /// <param name="gameCode">The game code of the original game</param>
        public async UniTask DeclineRematchAsync(string gameCode)
        {
            if (string.IsNullOrEmpty(gameCode)) return;

            string playerId = _playerService.CurrentPlayerId;

            try
            {
                // Find and update the rematch request
                string query = $"game_id=eq.{gameCode}&status=eq.pending";
                SupabaseResponse response = await _client.Get(TABLE_REMATCH_REQUESTS, query);

                if (response.Success && !string.IsNullOrEmpty(response.Body) && response.Body != "[]")
                {
                    string requestId = ExtractJsonValue(response.Body, "id");
                    if (!string.IsNullOrEmpty(requestId))
                    {
                        string updateJson = "{\"status\":\"declined\"}";
                        await _client.Patch(TABLE_REMATCH_REQUESTS, $"id=eq.{requestId}", updateJson);
                    }
                }

                SetStatus(RematchStatus.Declined);
                OnRematchDeclined?.Invoke("Rematch declined");
                Debug.Log("[RematchService] Rematch declined");
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[RematchService] Error declining rematch: {ex.Message}");
            }

            StopPolling();
        }

        /// <summary>
        /// Cancels the current rematch request.
        /// </summary>
        public async UniTask CancelRematchAsync()
        {
            if (_currentStatus != RematchStatus.RequestedByMe) return;

            if (!string.IsNullOrEmpty(_currentRequestId))
            {
                try
                {
                    await _client.Delete(TABLE_REMATCH_REQUESTS, $"id=eq.{_currentRequestId}");
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[RematchService] Error cancelling rematch: {ex.Message}");
                }
            }

            StopPolling();
            SetStatus(RematchStatus.None);
            Debug.Log("[RematchService] Rematch cancelled");
        }

        // ============================================================
        // STATUS CHECKING
        // ============================================================

        /// <summary>
        /// Checks if there's a pending rematch request for the game.
        /// </summary>
        public async UniTask<RematchStatus> CheckRematchStatusAsync(string gameCode, string playerId = null)
        {
            if (string.IsNullOrEmpty(gameCode)) return RematchStatus.None;

            if (string.IsNullOrEmpty(playerId))
            {
                playerId = _playerService.CurrentPlayerId;
            }

            try
            {
                string query = $"game_id=eq.{gameCode}&status=eq.pending";
                SupabaseResponse response = await _client.Get(TABLE_REMATCH_REQUESTS, query);

                if (!response.Success || string.IsNullOrEmpty(response.Body) || response.Body == "[]")
                {
                    return RematchStatus.None;
                }

                string requesterId = ExtractJsonValue(response.Body, "requester_id");

                if (requesterId == playerId)
                {
                    return RematchStatus.RequestedByMe;
                }
                else
                {
                    return RematchStatus.RequestedByOpponent;
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[RematchService] Error checking rematch status: {ex.Message}");
                return RematchStatus.None;
            }
        }

        /// <summary>
        /// Starts watching for rematch requests on the specified game.
        /// </summary>
        public void StartWatchingForRematch(string gameCode)
        {
            _currentGameCode = gameCode;
            SetStatus(RematchStatus.None);
            PollForOpponentRematchAsync().Forget();
        }

        /// <summary>
        /// Stops watching for rematch requests.
        /// </summary>
        public void StopWatching()
        {
            StopPolling();
            _currentGameCode = null;
            _currentRequestId = null;
            SetStatus(RematchStatus.None);
        }

        // ============================================================
        // POLLING
        // ============================================================

        /// <summary>
        /// Polls for opponent's response to our rematch request.
        /// </summary>
        private async UniTaskVoid PollForRematchResponseAsync()
        {
            _isPolling = true;
            float elapsedTime = 0f;

            while (_isPolling && elapsedTime < REMATCH_TIMEOUT_SECONDS)
            {
                await UniTask.Delay((int)REMATCH_POLL_INTERVAL_MS);
                elapsedTime += REMATCH_POLL_INTERVAL_MS / 1000f;

                if (!_isPolling) break;

                try
                {
                    // Check if our request was accepted
                    string query = $"id=eq.{_currentRequestId}";
                    SupabaseResponse response = await _client.Get(TABLE_REMATCH_REQUESTS, query);

                    if (response.Success && !string.IsNullOrEmpty(response.Body) && response.Body != "[]")
                    {
                        string status = ExtractJsonValue(response.Body, "status");
                        string newGameId = ExtractJsonValue(response.Body, "new_game_id");

                        if (status == "accepted" && !string.IsNullOrEmpty(newGameId))
                        {
                            // Rematch accepted!
                            _isPolling = false;
                            SetStatus(RematchStatus.Accepted);

                            RematchResult result = new RematchResult
                            {
                                Success = true,
                                NewGameCode = newGameId,
                                IsHost = true // We requested, so we're host of new game
                            };

                            OnRematchAccepted?.Invoke(result);
                            Debug.Log($"[RematchService] Rematch accepted! New game: {newGameId}");
                            return;
                        }
                        else if (status == "declined")
                        {
                            _isPolling = false;
                            SetStatus(RematchStatus.Declined);
                            OnRematchDeclined?.Invoke("Opponent declined rematch");
                            return;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[RematchService] Error polling for response: {ex.Message}");
                }
            }

            // Timeout
            if (_isPolling)
            {
                _isPolling = false;
                SetStatus(RematchStatus.Expired);
                OnRematchDeclined?.Invoke("Rematch request expired");
                Debug.Log("[RematchService] Rematch request expired");
            }
        }

        /// <summary>
        /// Polls for opponent's rematch request.
        /// </summary>
        private async UniTaskVoid PollForOpponentRematchAsync()
        {
            _isPolling = true;
            float elapsedTime = 0f;

            while (_isPolling && elapsedTime < REMATCH_TIMEOUT_SECONDS)
            {
                await UniTask.Delay((int)REMATCH_POLL_INTERVAL_MS);
                elapsedTime += REMATCH_POLL_INTERVAL_MS / 1000f;

                if (!_isPolling || string.IsNullOrEmpty(_currentGameCode)) break;

                try
                {
                    RematchStatus status = await CheckRematchStatusAsync(_currentGameCode);

                    if (status == RematchStatus.RequestedByOpponent && _currentStatus != RematchStatus.RequestedByOpponent)
                    {
                        SetStatus(RematchStatus.RequestedByOpponent);
                        OnOpponentRequestedRematch?.Invoke();
                        Debug.Log("[RematchService] Opponent requested rematch");
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[RematchService] Error polling for opponent rematch: {ex.Message}");
                }
            }
        }

        private void StopPolling()
        {
            _isPolling = false;
        }

        // ============================================================
        // GAME CREATION
        // ============================================================

        /// <summary>
        /// Creates a new game for the rematch.
        /// The player who was NOT host last time becomes host.
        /// </summary>
        private async UniTask<RematchResult> CreateRematchGameAsync(string originalGameCode, string requesterId, string accepterId)
        {
            try
            {
                // Get original game to determine who was host
                GameSession originalGame = await _sessionService.GetGame(originalGameCode);

                // Swap roles: requester becomes player 2, accepter becomes player 1 (host)
                // This alternates who goes first
                GameSession newGame = await _sessionService.CreateGame(accepterId);

                if (newGame == null)
                {
                    return new RematchResult
                    {
                        Success = false,
                        ErrorMessage = "Failed to create new game"
                    };
                }

                // Join both players
                await _sessionService.JoinGame(newGame.Id, accepterId, 1);
                await _sessionService.JoinGame(newGame.Id, requesterId, 2);

                // Update the rematch request with new game ID
                string updateJson = $"{{\"new_game_id\":\"{newGame.Id}\"}}";
                await _client.Patch(TABLE_REMATCH_REQUESTS, $"game_id=eq.{originalGameCode}&status=eq.accepted", updateJson);

                return new RematchResult
                {
                    Success = true,
                    NewGameCode = newGame.Id,
                    IsHost = accepterId == _playerService.CurrentPlayerId
                };
            }
            catch (Exception ex)
            {
                Debug.LogError($"[RematchService] Error creating rematch game: {ex.Message}");
                return new RematchResult
                {
                    Success = false,
                    ErrorMessage = ex.Message
                };
            }
        }

        // ============================================================
        // HELPERS
        // ============================================================

        private void SetStatus(RematchStatus status)
        {
            if (_currentStatus != status)
            {
                _currentStatus = status;
                OnRematchStatusChanged?.Invoke(status);
            }
        }

        private string ExtractJsonValue(string json, string key)
        {
            if (string.IsNullOrEmpty(json)) return null;

            string searchKey = $"\"{key}\":\"";
            int startIndex = json.IndexOf(searchKey);
            if (startIndex < 0)
            {
                searchKey = $"\"{key}\":";
                startIndex = json.IndexOf(searchKey);
                if (startIndex < 0) return null;

                startIndex += searchKey.Length;
                int endIndex = json.IndexOfAny(new[] { ',', '}', ']' }, startIndex);
                if (endIndex < 0) return null;
                return json.Substring(startIndex, endIndex - startIndex).Trim();
            }

            startIndex += searchKey.Length;
            int endQuote = json.IndexOf('"', startIndex);
            if (endQuote < 0) return null;

            return json.Substring(startIndex, endQuote - startIndex);
        }
    }
}

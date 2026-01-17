// MatchmakingService.cs
// Handles matchmaking queue and private game creation
// Created: January 16, 2026
// Developer: TecVooDoo LLC

using System;
using System.Collections.Generic;
using UnityEngine;
using Cysharp.Threading.Tasks;

namespace DLYH.Networking.Services
{
    /// <summary>
    /// Result of a matchmaking attempt.
    /// </summary>
    public class MatchmakingResult
    {
        public bool Success;
        public string GameCode;
        public bool IsHost;
        public bool IsPhantomAI;
        public string OpponentName;
        public string ErrorMessage;
    }

    /// <summary>
    /// Service for matchmaking and private game creation.
    /// Supports:
    /// - Public matchmaking queue with 6-second timeout
    /// - Phantom AI fallback when no match found
    /// - Private games with shareable join codes
    /// </summary>
    public class MatchmakingService
    {
        // ============================================================
        // CONSTANTS
        // ============================================================

        private const string TABLE_MATCHMAKING_QUEUE = "matchmaking_queue";
        private const float MATCHMAKING_TIMEOUT_SECONDS = 6f;
        private const float MATCHMAKING_POLL_INTERVAL_MS = 500f;

        // Phantom AI names (realistic names to disguise AI opponents)
        private static readonly string[] PHANTOM_AI_NAMES = new string[]
        {
            "Alex", "Jordan", "Taylor", "Morgan", "Casey",
            "Riley", "Avery", "Quinn", "Reese", "Skyler",
            "Charlie", "Drew", "Emery", "Finley", "Harper",
            "Jamie", "Kelly", "Lane", "Micah", "Peyton",
            "Sam", "Spencer", "Sydney", "Tatum", "Blake",
            "Cameron", "Devon", "Elliott", "Frankie", "Gray"
        };

        // ============================================================
        // EVENTS
        // ============================================================

        /// <summary>Fired when matchmaking status changes</summary>
        public event Action<string> OnMatchmakingStatusChanged;

        /// <summary>Fired when a match is found</summary>
        public event Action<MatchmakingResult> OnMatchFound;

        /// <summary>Fired when matchmaking fails</summary>
        public event Action<string> OnMatchmakingFailed;

        // ============================================================
        // STATE
        // ============================================================

        private readonly SupabaseClient _client;
        private readonly SupabaseConfig _config;
        private readonly GameSessionService _sessionService;
        private readonly PlayerService _playerService;

        private bool _isMatchmaking;
        private string _currentQueueEntryId;

        // ============================================================
        // PROPERTIES
        // ============================================================

        public bool IsMatchmaking => _isMatchmaking;

        // ============================================================
        // CONSTRUCTOR
        // ============================================================

        public MatchmakingService(
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
        // PUBLIC MATCHMAKING
        // ============================================================

        /// <summary>
        /// Starts public matchmaking. Attempts to find an opponent for 6 seconds.
        /// If no match found, spawns a phantom AI opponent.
        /// </summary>
        /// <param name="gridSize">Preferred grid size</param>
        /// <param name="difficulty">Preferred difficulty</param>
        /// <returns>MatchmakingResult with game details</returns>
        public async UniTask<MatchmakingResult> StartMatchmakingAsync(int gridSize, string difficulty)
        {
            if (_isMatchmaking)
            {
                return new MatchmakingResult
                {
                    Success = false,
                    ErrorMessage = "Already matchmaking"
                };
            }

            _isMatchmaking = true;
            OnMatchmakingStatusChanged?.Invoke("Searching for opponent...");

            try
            {
                string playerId = _playerService.CurrentPlayerId;
                if (string.IsNullOrEmpty(playerId))
                {
                    return new MatchmakingResult
                    {
                        Success = false,
                        ErrorMessage = "Not signed in"
                    };
                }

                // Step 1: Check for existing waiting games
                MatchmakingResult existingMatch = await TryJoinExistingGameAsync(gridSize, difficulty);
                if (existingMatch != null && existingMatch.Success)
                {
                    _isMatchmaking = false;
                    OnMatchFound?.Invoke(existingMatch);
                    return existingMatch;
                }

                // Step 2: Create a new game and add to queue
                GameSession newGame = await _sessionService.CreateGame(playerId);
                if (newGame == null)
                {
                    _isMatchmaking = false;
                    return new MatchmakingResult
                    {
                        Success = false,
                        ErrorMessage = "Failed to create game"
                    };
                }

                // Add to matchmaking queue
                _currentQueueEntryId = await AddToQueueAsync(newGame.Id, playerId, gridSize, difficulty);
                if (string.IsNullOrEmpty(_currentQueueEntryId))
                {
                    _isMatchmaking = false;
                    return new MatchmakingResult
                    {
                        Success = false,
                        ErrorMessage = "Failed to join matchmaking queue"
                    };
                }

                // Step 3: Poll for opponent
                float elapsedTime = 0f;
                while (elapsedTime < MATCHMAKING_TIMEOUT_SECONDS)
                {
                    await UniTask.Delay((int)MATCHMAKING_POLL_INTERVAL_MS);
                    elapsedTime += MATCHMAKING_POLL_INTERVAL_MS / 1000f;

                    if (!_isMatchmaking)
                    {
                        // Cancelled
                        await RemoveFromQueueAsync(_currentQueueEntryId);
                        return new MatchmakingResult
                        {
                            Success = false,
                            ErrorMessage = "Matchmaking cancelled"
                        };
                    }

                    OnMatchmakingStatusChanged?.Invoke($"Searching... ({(int)(MATCHMAKING_TIMEOUT_SECONDS - elapsedTime)}s)");

                    // Check if someone joined our game
                    int playerCount = await _sessionService.GetPlayerCount(newGame.Id);
                    if (playerCount >= 2)
                    {
                        // Found a match!
                        await RemoveFromQueueAsync(_currentQueueEntryId);
                        _isMatchmaking = false;

                        MatchmakingResult result = new MatchmakingResult
                        {
                            Success = true,
                            GameCode = newGame.Id,
                            IsHost = true,
                            IsPhantomAI = false,
                            OpponentName = "Opponent" // Will be updated when game state syncs
                        };

                        OnMatchFound?.Invoke(result);
                        return result;
                    }
                }

                // Step 4: Timeout - spawn phantom AI
                await RemoveFromQueueAsync(_currentQueueEntryId);
                _isMatchmaking = false;

                OnMatchmakingStatusChanged?.Invoke("Opponent found!");

                string phantomName = GetRandomPhantomName();
                MatchmakingResult phantomResult = new MatchmakingResult
                {
                    Success = true,
                    GameCode = newGame.Id,
                    IsHost = true,
                    IsPhantomAI = true,
                    OpponentName = phantomName
                };

                Debug.Log($"[MatchmakingService] No match found, spawning phantom AI: {phantomName}");
                OnMatchFound?.Invoke(phantomResult);
                return phantomResult;
            }
            catch (Exception ex)
            {
                _isMatchmaking = false;
                Debug.LogError($"[MatchmakingService] Matchmaking error: {ex.Message}");

                MatchmakingResult errorResult = new MatchmakingResult
                {
                    Success = false,
                    ErrorMessage = ex.Message
                };

                OnMatchmakingFailed?.Invoke(ex.Message);
                return errorResult;
            }
        }

        /// <summary>
        /// Cancels the current matchmaking search.
        /// </summary>
        public async UniTask CancelMatchmakingAsync()
        {
            if (!_isMatchmaking) return;

            _isMatchmaking = false;

            if (!string.IsNullOrEmpty(_currentQueueEntryId))
            {
                await RemoveFromQueueAsync(_currentQueueEntryId);
                _currentQueueEntryId = null;
            }

            OnMatchmakingStatusChanged?.Invoke("Matchmaking cancelled");
            Debug.Log("[MatchmakingService] Matchmaking cancelled");
        }

        // ============================================================
        // PRIVATE GAMES (JOIN CODES)
        // ============================================================

        /// <summary>
        /// Creates a private game and returns the join code.
        /// The game is NOT added to the matchmaking queue.
        /// </summary>
        /// <returns>MatchmakingResult with game code, or error</returns>
        public async UniTask<MatchmakingResult> CreatePrivateGameAsync()
        {
            try
            {
                string playerId = _playerService.CurrentPlayerId;
                if (string.IsNullOrEmpty(playerId))
                {
                    return new MatchmakingResult
                    {
                        Success = false,
                        ErrorMessage = "Not signed in"
                    };
                }

                GameSession game = await _sessionService.CreateGame(playerId);
                if (game == null)
                {
                    return new MatchmakingResult
                    {
                        Success = false,
                        ErrorMessage = "Failed to create game"
                    };
                }

                // Join as player 1
                bool joined = await _sessionService.JoinGame(game.Id, playerId, 1);
                if (!joined)
                {
                    return new MatchmakingResult
                    {
                        Success = false,
                        ErrorMessage = "Failed to join game"
                    };
                }

                Debug.Log($"[MatchmakingService] Created private game: {game.Id}");

                return new MatchmakingResult
                {
                    Success = true,
                    GameCode = game.Id,
                    IsHost = true,
                    IsPhantomAI = false
                };
            }
            catch (Exception ex)
            {
                Debug.LogError($"[MatchmakingService] Error creating private game: {ex.Message}");
                return new MatchmakingResult
                {
                    Success = false,
                    ErrorMessage = ex.Message
                };
            }
        }

        /// <summary>
        /// Joins a private game using a join code.
        /// </summary>
        /// <param name="gameCode">The 6-character game code</param>
        /// <returns>MatchmakingResult with game details, or error</returns>
        public async UniTask<MatchmakingResult> JoinWithCodeAsync(string gameCode)
        {
            if (string.IsNullOrEmpty(gameCode))
            {
                return new MatchmakingResult
                {
                    Success = false,
                    ErrorMessage = "Game code is required"
                };
            }

            // Normalize code (uppercase, trim)
            gameCode = gameCode.Trim().ToUpperInvariant();

            if (gameCode.Length != 6)
            {
                return new MatchmakingResult
                {
                    Success = false,
                    ErrorMessage = "Invalid game code format"
                };
            }

            try
            {
                string playerId = _playerService.CurrentPlayerId;
                if (string.IsNullOrEmpty(playerId))
                {
                    return new MatchmakingResult
                    {
                        Success = false,
                        ErrorMessage = "Not signed in"
                    };
                }

                // Check if game exists
                GameSession game = await _sessionService.GetGame(gameCode);
                if (game == null)
                {
                    return new MatchmakingResult
                    {
                        Success = false,
                        ErrorMessage = "Game not found"
                    };
                }

                // Check game status
                if (game.Status != "waiting")
                {
                    return new MatchmakingResult
                    {
                        Success = false,
                        ErrorMessage = game.Status == "playing" ? "Game already in progress" : "Game has ended"
                    };
                }

                // Check player count
                int playerCount = await _sessionService.GetPlayerCount(gameCode);
                if (playerCount >= 2)
                {
                    return new MatchmakingResult
                    {
                        Success = false,
                        ErrorMessage = "Game is full"
                    };
                }

                // Join as player 2
                bool joined = await _sessionService.JoinGame(gameCode, playerId, 2);
                if (!joined)
                {
                    return new MatchmakingResult
                    {
                        Success = false,
                        ErrorMessage = "Failed to join game"
                    };
                }

                Debug.Log($"[MatchmakingService] Joined game: {gameCode}");

                return new MatchmakingResult
                {
                    Success = true,
                    GameCode = gameCode,
                    IsHost = false,
                    IsPhantomAI = false
                };
            }
            catch (Exception ex)
            {
                Debug.LogError($"[MatchmakingService] Error joining game: {ex.Message}");
                return new MatchmakingResult
                {
                    Success = false,
                    ErrorMessage = ex.Message
                };
            }
        }

        // ============================================================
        // QUEUE OPERATIONS
        // ============================================================

        /// <summary>
        /// Tries to find and join an existing waiting game.
        /// </summary>
        private async UniTask<MatchmakingResult> TryJoinExistingGameAsync(int gridSize, string difficulty)
        {
            try
            {
                string playerId = _playerService.CurrentPlayerId;

                // Query matchmaking queue for compatible games
                // Prefer same grid size and difficulty, but accept any
                string query = $"game_type=eq.{_config.GameTypeId}&status=eq.waiting&player_id=neq.{playerId}&order=created_at.asc&limit=1";

                SupabaseResponse response = await _client.Get(TABLE_MATCHMAKING_QUEUE, query);

                if (!response.Success || string.IsNullOrEmpty(response.Body) || response.Body == "[]")
                {
                    return null; // No waiting games
                }

                // Parse queue entry
                string gameCode = ExtractJsonValue(response.Body, "game_id");
                string queueEntryId = ExtractJsonValue(response.Body, "id");

                if (string.IsNullOrEmpty(gameCode))
                {
                    return null;
                }

                // Try to join the game
                bool joined = await _sessionService.JoinGame(gameCode, playerId, 2);
                if (!joined)
                {
                    return null; // Someone else got it
                }

                // Remove the queue entry (the host will see we joined)
                if (!string.IsNullOrEmpty(queueEntryId))
                {
                    await RemoveFromQueueAsync(queueEntryId);
                }

                Debug.Log($"[MatchmakingService] Joined existing game: {gameCode}");

                return new MatchmakingResult
                {
                    Success = true,
                    GameCode = gameCode,
                    IsHost = false,
                    IsPhantomAI = false
                };
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[MatchmakingService] Error finding existing game: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Adds a game to the matchmaking queue.
        /// </summary>
        private async UniTask<string> AddToQueueAsync(string gameCode, string playerId, int gridSize, string difficulty)
        {
            try
            {
                string json = $"{{\"game_id\":\"{gameCode}\",\"game_type\":\"{_config.GameTypeId}\",\"player_id\":\"{playerId}\",\"grid_size\":{gridSize},\"difficulty\":\"{difficulty}\",\"status\":\"waiting\"}}";

                SupabaseResponse response = await _client.Post(TABLE_MATCHMAKING_QUEUE, json);

                if (!response.Success)
                {
                    Debug.LogError($"[MatchmakingService] Failed to add to queue: {response.Error}");
                    return null;
                }

                string entryId = ExtractJsonValue(response.Body, "id");
                Debug.Log($"[MatchmakingService] Added to queue: {entryId}");
                return entryId;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[MatchmakingService] Error adding to queue: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Removes an entry from the matchmaking queue.
        /// </summary>
        private async UniTask RemoveFromQueueAsync(string entryId)
        {
            if (string.IsNullOrEmpty(entryId)) return;

            try
            {
                await _client.Delete(TABLE_MATCHMAKING_QUEUE, $"id=eq.{entryId}");
                Debug.Log($"[MatchmakingService] Removed from queue: {entryId}");
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[MatchmakingService] Error removing from queue: {ex.Message}");
            }
        }

        // ============================================================
        // HELPERS
        // ============================================================

        /// <summary>
        /// Gets a random phantom AI name.
        /// </summary>
        private string GetRandomPhantomName()
        {
            int index = UnityEngine.Random.Range(0, PHANTOM_AI_NAMES.Length);
            return PHANTOM_AI_NAMES[index];
        }

        /// <summary>
        /// Extracts a JSON value from a response.
        /// </summary>
        private string ExtractJsonValue(string json, string key)
        {
            if (string.IsNullOrEmpty(json)) return null;

            string searchKey = $"\"{key}\":\"";
            int startIndex = json.IndexOf(searchKey);
            if (startIndex < 0)
            {
                // Try non-string value
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

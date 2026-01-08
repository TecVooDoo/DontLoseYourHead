// GameSessionService.cs
// CRUD operations for game_sessions table
// Created: January 4, 2026
// Developer: TecVooDoo LLC

using System;
using System.Text;
using UnityEngine;
using Cysharp.Threading.Tasks;

namespace DLYH.Networking.Services
{
    /// <summary>
    /// Service for managing DLYH game sessions in Supabase.
    /// Handles create, read, update operations for the game_sessions table.
    /// </summary>
    public class GameSessionService
    {
        // ============================================================
        // CONSTANTS
        // ============================================================

        private const string TABLE_GAME_SESSIONS = "game_sessions";
        private const string TABLE_SESSION_PLAYERS = "session_players";

        // Characters for generating game codes (excluding ambiguous: 0, O, 1, I, L)
        private const string CODE_CHARS = "ABCDEFGHJKMNPQRSTUVWXYZ23456789";
        private const int CODE_LENGTH = 6;

        // ============================================================
        // DEPENDENCIES
        // ============================================================

        private readonly SupabaseClient _client;
        private readonly SupabaseConfig _config;

        // ============================================================
        // CONSTRUCTOR
        // ============================================================

        public GameSessionService(SupabaseClient client, SupabaseConfig config)
        {
            _client = client ?? throw new ArgumentNullException(nameof(client));
            _config = config ?? throw new ArgumentNullException(nameof(config));
        }

        // ============================================================
        // GAME CODE GENERATION
        // ============================================================

        /// <summary>
        /// Generates a random 6-character game code.
        /// </summary>
        public static string GenerateGameCode()
        {
            var sb = new StringBuilder(CODE_LENGTH);
            for (int i = 0; i < CODE_LENGTH; i++)
            {
                sb.Append(CODE_CHARS[UnityEngine.Random.Range(0, CODE_CHARS.Length)]);
            }
            return sb.ToString();
        }

        // ============================================================
        // CREATE GAME
        // ============================================================

        /// <summary>
        /// Creates a new game session with a unique code.
        /// </summary>
        /// <param name="playerId">Creating player's ID (from players table, or null for anonymous)</param>
        /// <returns>The created game session, or null on failure</returns>
        public async UniTask<GameSession> CreateGame(string playerId = null)
        {
            // Generate unique game code (retry if collision)
            string gameCode = null;
            int attempts = 0;
            const int maxAttempts = 5;

            while (attempts < maxAttempts)
            {
                gameCode = GenerateGameCode();

                // Check if code already exists
                var existing = await GetGame(gameCode);
                if (existing == null)
                {
                    break; // Code is unique
                }

                attempts++;
                Debug.Log($"[GameSessionService] Code collision on {gameCode}, retrying...");
            }

            if (attempts >= maxAttempts)
            {
                Debug.LogError("[GameSessionService] Failed to generate unique game code");
                return null;
            }

            // Build initial game state
            var initialState = new DLYHGameState
            {
                version = 1,
                status = "waiting",
                currentTurn = null,
                turnNumber = 0,
                player1 = null,
                player2 = null,
                winner = null,
                createdAt = DateTime.UtcNow.ToString("o"),
                updatedAt = DateTime.UtcNow.ToString("o")
            };

            // Build JSON for insert
            string json = BuildCreateGameJson(gameCode, playerId, initialState);

            var response = await _client.Post(TABLE_GAME_SESSIONS, json);

            if (!response.Success)
            {
                Debug.LogError($"[GameSessionService] Failed to create game: {response.Error}");
                return null;
            }

            Debug.Log($"[GameSessionService] Created game: {gameCode}");

            // Parse response to get created session
            return ParseGameSession(response.Body);
        }

        /// <summary>
        /// Builds JSON for creating a new game session.
        /// </summary>
        private string BuildCreateGameJson(string gameCode, string playerId, DLYHGameState state)
        {
            var sb = new StringBuilder();
            sb.Append("{");
            sb.AppendFormat("\"id\":\"{0}\",", gameCode);
            sb.AppendFormat("\"game_type\":\"{0}\",", _config.GameTypeId);
            sb.Append("\"status\":\"waiting\",");

            if (!string.IsNullOrEmpty(playerId))
            {
                sb.AppendFormat("\"created_by\":\"{0}\",", playerId);
            }

            // Serialize state as JSONB
            sb.Append("\"state\":");
            sb.Append(SerializeGameState(state));

            sb.Append("}");
            return sb.ToString();
        }

        // ============================================================
        // GET GAME
        // ============================================================

        /// <summary>
        /// Gets a game session by code.
        /// </summary>
        /// <param name="gameCode">6-character game code</param>
        /// <returns>Game session or null if not found</returns>
        public async UniTask<GameSession> GetGame(string gameCode)
        {
            if (string.IsNullOrEmpty(gameCode))
            {
                return null;
            }

            var response = await _client.Get(TABLE_GAME_SESSIONS, $"id=eq.{gameCode}");

            if (!response.Success)
            {
                Debug.LogWarning($"[GameSessionService] Failed to get game {gameCode}: {response.Error}");
                return null;
            }

            return ParseGameSession(response.Body);
        }

        /// <summary>
        /// Gets a game session with player information.
        /// </summary>
        public async UniTask<GameSessionWithPlayers> GetGameWithPlayers(string gameCode)
        {
            if (string.IsNullOrEmpty(gameCode))
            {
                return null;
            }

            // Get game session
            var game = await GetGame(gameCode);
            if (game == null)
            {
                return null;
            }

            // Get session players
            var playersResponse = await _client.Get(TABLE_SESSION_PLAYERS, $"session_id=eq.{gameCode}");

            var result = new GameSessionWithPlayers
            {
                Session = game,
                Players = new SessionPlayer[0]
            };

            if (playersResponse.Success && !string.IsNullOrEmpty(playersResponse.Body))
            {
                result.Players = ParseSessionPlayers(playersResponse.Body);
            }

            return result;
        }

        // ============================================================
        // UPDATE GAME STATE
        // ============================================================

        /// <summary>
        /// Updates the game state in an existing session.
        /// </summary>
        /// <param name="gameCode">Game code</param>
        /// <param name="state">New game state</param>
        /// <param name="status">Optional new status (waiting/active/completed/abandoned)</param>
        /// <returns>True if successful</returns>
        public async UniTask<bool> UpdateGameState(string gameCode, DLYHGameState state, string status = null)
        {
            if (string.IsNullOrEmpty(gameCode))
            {
                return false;
            }

            // Update timestamp
            state.updatedAt = DateTime.UtcNow.ToString("o");

            var sb = new StringBuilder();
            sb.Append("{");
            sb.Append("\"state\":");
            sb.Append(SerializeGameState(state));

            if (!string.IsNullOrEmpty(status))
            {
                sb.AppendFormat(",\"status\":\"{0}\"", status);
            }

            sb.Append(",\"updated_at\":\"now()\"");
            sb.Append("}");

            var response = await _client.Patch(TABLE_GAME_SESSIONS, $"id=eq.{gameCode}", sb.ToString());

            if (!response.Success)
            {
                Debug.LogError($"[GameSessionService] Failed to update game {gameCode}: {response.Error}");
                return false;
            }

            Debug.Log($"[GameSessionService] Updated game state: {gameCode}");
            return true;
        }

        /// <summary>
        /// Updates just the game status.
        /// </summary>
        public async UniTask<bool> UpdateGameStatus(string gameCode, string status)
        {
            var json = $"{{\"status\":\"{status}\",\"updated_at\":\"now()\"}}";
            var response = await _client.Patch(TABLE_GAME_SESSIONS, $"id=eq.{gameCode}", json);

            return response.Success;
        }

        // ============================================================
        // JOIN GAME
        // ============================================================

        /// <summary>
        /// Adds a player to a game session.
        /// </summary>
        /// <param name="gameCode">Game code to join</param>
        /// <param name="playerId">Player's ID (auth user ID, can be null for anonymous)</param>
        /// <param name="playerNumber">1 or 2</param>
        /// <returns>True if successful</returns>
        public async UniTask<bool> JoinGame(string gameCode, string playerId, int playerNumber)
        {
            var sb = new StringBuilder();
            sb.Append("{");
            sb.AppendFormat("\"session_id\":\"{0}\",", gameCode);

            if (!string.IsNullOrEmpty(playerId))
            {
                sb.AppendFormat("\"player_id\":\"{0}\",", playerId);
            }

            sb.AppendFormat("\"player_number\":{0}", playerNumber);
            sb.Append("}");

            var response = await _client.Post(TABLE_SESSION_PLAYERS, sb.ToString());

            if (!response.Success)
            {
                Debug.LogError($"[GameSessionService] Failed to join game {gameCode}: {response.Error}");
                return false;
            }

            Debug.Log($"[GameSessionService] Player {playerNumber} joined game {gameCode}");
            return true;
        }

        /// <summary>
        /// Gets the number of players currently in a game.
        /// </summary>
        public async UniTask<int> GetPlayerCount(string gameCode)
        {
            // Select player_number since session_players doesn't have an 'id' column
            var response = await _client.Get(TABLE_SESSION_PLAYERS, $"session_id=eq.{gameCode}&select=player_number");

            if (!response.Success)
            {
                Debug.LogWarning($"[GameSessionService] GetPlayerCount failed for {gameCode}: {response.Error}");
                return 0;
            }

            if (string.IsNullOrEmpty(response.Body) || response.Body == "[]")
            {
                Debug.Log($"[GameSessionService] GetPlayerCount for {gameCode}: 0 (empty response)");
                return 0;
            }

            // Count opening braces to count objects in array
            int count = 0;
            foreach (char c in response.Body)
            {
                if (c == '{') count++;
            }

            Debug.Log($"[GameSessionService] GetPlayerCount for {gameCode}: {count}");
            return count;
        }

        // ============================================================
        // JSON SERIALIZATION (Manual - Unity's JsonUtility doesn't handle nested objects well)
        // ============================================================

        private string SerializeGameState(DLYHGameState state)
        {
            var sb = new StringBuilder();
            sb.Append("{");
            sb.AppendFormat("\"version\":{0},", state.version);
            sb.AppendFormat("\"status\":\"{0}\",", state.status ?? "waiting");
            sb.AppendFormat("\"currentTurn\":{0},", state.currentTurn == null ? "null" : $"\"{state.currentTurn}\"");
            sb.AppendFormat("\"turnNumber\":{0},", state.turnNumber);
            sb.AppendFormat("\"createdAt\":\"{0}\",", state.createdAt ?? DateTime.UtcNow.ToString("o"));
            sb.AppendFormat("\"updatedAt\":\"{0}\",", state.updatedAt ?? DateTime.UtcNow.ToString("o"));

            // Player 1 data
            sb.Append("\"player1\":");
            sb.Append(state.player1 == null ? "null" : SerializePlayerData(state.player1));
            sb.Append(",");

            // Player 2 data
            sb.Append("\"player2\":");
            sb.Append(state.player2 == null ? "null" : SerializePlayerData(state.player2));
            sb.Append(",");

            // Winner
            sb.AppendFormat("\"winner\":{0}", state.winner == null ? "null" : $"\"{state.winner}\"");

            sb.Append("}");
            return sb.ToString();
        }

        private string SerializePlayerData(DLYHPlayerData player)
        {
            var sb = new StringBuilder();
            sb.Append("{");
            sb.AppendFormat("\"name\":\"{0}\",", EscapeJson(player.name ?? ""));
            sb.AppendFormat("\"color\":\"{0}\",", player.color ?? "#FFFFFF");
            sb.AppendFormat("\"ready\":{0},", player.ready ? "true" : "false");
            sb.AppendFormat("\"setupComplete\":{0},", player.setupComplete ? "true" : "false");
            sb.AppendFormat("\"lastActivityAt\":\"{0}\"", player.lastActivityAt ?? DateTime.UtcNow.ToString("o"));

            // Setup data (if present)
            if (player.setupData != null)
            {
                sb.Append(",\"setupData\":");
                sb.Append(SerializeSetupData(player.setupData));
            }

            // Gameplay state (if present)
            if (player.gameplayState != null)
            {
                sb.Append(",\"gameplayState\":");
                sb.Append(SerializeGameplayState(player.gameplayState));
            }

            sb.Append("}");
            return sb.ToString();
        }

        private string SerializeSetupData(DLYHSetupData setup)
        {
            var sb = new StringBuilder();
            sb.Append("{");
            sb.AppendFormat("\"gridSize\":{0},", setup.gridSize);
            sb.AppendFormat("\"wordCount\":{0},", setup.wordCount);
            sb.AppendFormat("\"difficulty\":\"{0}\"", setup.difficulty ?? "Normal");

            // Word placements are sensitive - only include hash or encrypted version
            // Full data is stored but only revealed at game end
            if (setup.wordPlacementsEncrypted != null)
            {
                sb.AppendFormat(",\"wordPlacementsEncrypted\":\"{0}\"", setup.wordPlacementsEncrypted);
            }

            sb.Append("}");
            return sb.ToString();
        }

        private string SerializeGameplayState(DLYHGameplayState gameplay)
        {
            var sb = new StringBuilder();
            sb.Append("{");
            sb.AppendFormat("\"misses\":{0},", gameplay.misses);
            sb.AppendFormat("\"missLimit\":{0},", gameplay.missLimit);

            // Known letters as array
            sb.Append("\"knownLetters\":[");
            if (gameplay.knownLetters != null)
            {
                for (int i = 0; i < gameplay.knownLetters.Length; i++)
                {
                    if (i > 0) sb.Append(",");
                    sb.AppendFormat("\"{0}\"", gameplay.knownLetters[i]);
                }
            }
            sb.Append("],");

            // Guessed coordinates as array of arrays
            sb.Append("\"guessedCoordinates\":[");
            if (gameplay.guessedCoordinates != null)
            {
                for (int i = 0; i < gameplay.guessedCoordinates.Length; i++)
                {
                    if (i > 0) sb.Append(",");
                    sb.AppendFormat("[{0},{1}]", gameplay.guessedCoordinates[i].row, gameplay.guessedCoordinates[i].col);
                }
            }
            sb.Append("],");

            // Solved word rows
            sb.Append("\"solvedWordRows\":[");
            if (gameplay.solvedWordRows != null)
            {
                for (int i = 0; i < gameplay.solvedWordRows.Length; i++)
                {
                    if (i > 0) sb.Append(",");
                    sb.Append(gameplay.solvedWordRows[i]);
                }
            }
            sb.Append("]");

            sb.Append("}");
            return sb.ToString();
        }

        // ============================================================
        // JSON PARSING (Simple manual parsing)
        // ============================================================

        private GameSession ParseGameSession(string json)
        {
            if (string.IsNullOrEmpty(json) || json == "[]")
            {
                return null;
            }

            // If response is array, take first element
            if (json.StartsWith("["))
            {
                if (json == "[]") return null;
                // Extract first object from array
                int start = json.IndexOf('{');
                int depth = 0;
                int end = start;
                for (int i = start; i < json.Length; i++)
                {
                    if (json[i] == '{') depth++;
                    else if (json[i] == '}') depth--;
                    if (depth == 0)
                    {
                        end = i;
                        break;
                    }
                }
                json = json.Substring(start, end - start + 1);
            }

            // Simple field extraction (not a full JSON parser)
            var session = new GameSession();
            session.Id = ExtractStringField(json, "id");
            session.GameType = ExtractStringField(json, "game_type");
            session.Status = ExtractStringField(json, "status");
            session.CreatedBy = ExtractStringField(json, "created_by");

            // State is complex - store as raw JSON for now
            int stateStart = json.IndexOf("\"state\":");
            if (stateStart >= 0)
            {
                stateStart += 8; // Skip "state":
                // Find matching closing brace
                int depth = 0;
                int stateEnd = stateStart;
                for (int i = stateStart; i < json.Length; i++)
                {
                    if (json[i] == '{') depth++;
                    else if (json[i] == '}') depth--;
                    if (depth == 0)
                    {
                        stateEnd = i;
                        break;
                    }
                }
                session.StateJson = json.Substring(stateStart, stateEnd - stateStart + 1);
            }

            return session;
        }

        private SessionPlayer[] ParseSessionPlayers(string json)
        {
            if (string.IsNullOrEmpty(json) || json == "[]")
            {
                return new SessionPlayer[0];
            }

            // Count players by counting '{' characters
            int count = 0;
            foreach (char c in json)
            {
                if (c == '{') count++;
            }

            var players = new SessionPlayer[count];
            int index = 0;
            int pos = 0;

            while (index < count && pos < json.Length)
            {
                int start = json.IndexOf('{', pos);
                if (start < 0) break;

                int depth = 0;
                int end = start;
                for (int i = start; i < json.Length; i++)
                {
                    if (json[i] == '{') depth++;
                    else if (json[i] == '}') depth--;
                    if (depth == 0)
                    {
                        end = i;
                        break;
                    }
                }

                string playerJson = json.Substring(start, end - start + 1);
                players[index] = new SessionPlayer
                {
                    SessionId = ExtractStringField(playerJson, "session_id"),
                    PlayerId = ExtractStringField(playerJson, "player_id"),
                    PlayerNumber = ExtractIntField(playerJson, "player_number")
                };

                index++;
                pos = end + 1;
            }

            return players;
        }

        private string ExtractStringField(string json, string fieldName)
        {
            string pattern = $"\"{fieldName}\":\"";
            int start = json.IndexOf(pattern);
            if (start < 0) return null;

            start += pattern.Length;
            int end = json.IndexOf("\"", start);
            if (end < 0) return null;

            return json.Substring(start, end - start);
        }

        private int ExtractIntField(string json, string fieldName)
        {
            string pattern = $"\"{fieldName}\":";
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

        private string EscapeJson(string str)
        {
            if (string.IsNullOrEmpty(str)) return "";
            return str.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n").Replace("\r", "\\r");
        }
    }

    // ============================================================
    // DATA CLASSES
    // ============================================================

    /// <summary>
    /// Represents a game session row from game_sessions table.
    /// </summary>
    [Serializable]
    public class GameSession
    {
        public string Id;           // 6-char game code
        public string GameType;     // "dlyh"
        public string Status;       // waiting, active, completed, abandoned
        public string CreatedBy;    // Player UUID
        public string StateJson;    // Raw JSON of game state
    }

    /// <summary>
    /// Represents a session_players row.
    /// </summary>
    [Serializable]
    public class SessionPlayer
    {
        public string SessionId;
        public string PlayerId;
        public int PlayerNumber;    // 1 or 2
    }

    /// <summary>
    /// Game session with associated players.
    /// </summary>
    public class GameSessionWithPlayers
    {
        public GameSession Session;
        public SessionPlayer[] Players;
    }

    // ============================================================
    // DLYH GAME STATE STRUCTURES
    // ============================================================

    /// <summary>
    /// Full DLYH game state stored in game_sessions.state JSONB.
    /// </summary>
    [Serializable]
    public class DLYHGameState
    {
        public int version;
        public string status;           // setup, waiting, playing, finished
        public string currentTurn;      // "player1" or "player2"
        public int turnNumber;
        public string createdAt;
        public string updatedAt;
        public DLYHPlayerData player1;
        public DLYHPlayerData player2;
        public string winner;           // null, "player1", "player2"
    }

    /// <summary>
    /// Player data within game state.
    /// </summary>
    [Serializable]
    public class DLYHPlayerData
    {
        public string name;
        public string color;
        public bool ready;
        public bool setupComplete;
        public string lastActivityAt;
        public DLYHSetupData setupData;
        public DLYHGameplayState gameplayState;
    }

    /// <summary>
    /// Player's setup configuration.
    /// </summary>
    [Serializable]
    public class DLYHSetupData
    {
        public int gridSize;
        public int wordCount;
        public string difficulty;
        public string wordPlacementsEncrypted;  // Encrypted word placements (revealed at game end)
    }

    /// <summary>
    /// Player's gameplay state (what opponent knows about their grid).
    /// </summary>
    [Serializable]
    public class DLYHGameplayState
    {
        public int misses;
        public int missLimit;
        public string[] knownLetters;
        public CoordinatePair[] guessedCoordinates;
        public int[] solvedWordRows;
    }

    /// <summary>
    /// Simple coordinate pair for serialization.
    /// </summary>
    [Serializable]
    public struct CoordinatePair
    {
        public int row;
        public int col;

        public CoordinatePair(int row, int col)
        {
            this.row = row;
            this.col = col;
        }
    }
}

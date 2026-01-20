// PlayerService.cs
// Service for managing player records in the players table
// Created: January 7, 2026
// Developer: TecVooDoo LLC
//
// NOTE: The players table does NOT require auth. Any user can create a player
// record with just a display_name. This matches how DAB handles guests.

using System;
using UnityEngine;
using Cysharp.Threading.Tasks;
using DLYH.Core.Utilities;

namespace DLYH.Networking.Services
{
    /// <summary>
    /// Service for managing player records in the Supabase players table.
    /// Creates player records that can be used with session_players.
    /// Does NOT require Supabase Auth - works with anonymous players.
    /// </summary>
    public class PlayerService
    {
        // ============================================================
        // CONSTANTS
        // ============================================================

        private const string TABLE_PLAYERS = "players";
        private const string PREFS_PLAYER_ID = "DLYH_PlayerId";
        private const string PREFS_PLAYER_NAME = "DLYH_PlayerName";

        // The Executioner AI reserved player ID (matches DAB)
        public const string EXECUTIONER_PLAYER_ID = "00000000-0000-0000-0000-000000000001";

        // ============================================================
        // DEPENDENCIES
        // ============================================================

        private readonly SupabaseClient _client;

        // ============================================================
        // STATE
        // ============================================================

        private string _currentPlayerId;
        private string _currentPlayerName;

        // ============================================================
        // PROPERTIES
        // ============================================================

        /// <summary>Current player's ID from the players table (UUID)</summary>
        public string CurrentPlayerId => _currentPlayerId;

        /// <summary>Current player's display name</summary>
        public string CurrentPlayerName => _currentPlayerName;

        /// <summary>Whether we have a valid player record</summary>
        public bool HasPlayerRecord => !string.IsNullOrEmpty(_currentPlayerId);

        // ============================================================
        // CONSTRUCTOR
        // ============================================================

        public PlayerService(SupabaseClient client)
        {
            _client = client ?? throw new ArgumentNullException(nameof(client));
            RestoreFromPrefs();
        }

        // ============================================================
        // ENSURE PLAYER RECORD
        // ============================================================

        /// <summary>
        /// Ensures we have a player record in the players table.
        /// Creates a new record if needed, or returns existing cached ID.
        ///
        /// The player ID is the user's IDENTITY - it persists across all games and sessions.
        /// Per-game data (nickname, color, grid size, etc.) is stored in session_players,
        /// not here. This allows the same player to have different setups in different games.
        ///
        /// The displayName parameter is only used when creating a NEW player record
        /// as a default/fallback. It does NOT affect existing player identity.
        /// </summary>
        /// <param name="displayName">Default name for new player record (not per-game nickname)</param>
        /// <returns>Player ID (UUID) or null on failure</returns>
        public async UniTask<string> EnsurePlayerRecordAsync(string displayName = "Player")
        {
            if (string.IsNullOrEmpty(displayName))
            {
                displayName = "Player";
            }

            // If we have a cached player ID, verify it still exists in the database
            if (!string.IsNullOrEmpty(_currentPlayerId))
            {
                bool exists = await VerifyPlayerExistsAsync(_currentPlayerId);
                if (exists)
                {
                    Debug.Log($"[PlayerService] Using existing player record: {_currentPlayerId}");
                    return _currentPlayerId;
                }
                else
                {
                    // Cached ID is stale (record deleted from DB), clear it
                    Debug.Log("[PlayerService] Cached player ID is stale, creating new record");
                    _currentPlayerId = null;
                    _currentPlayerName = null;
                }
            }

            // Create new player record with default name
            string playerId = await CreatePlayerRecordAsync(displayName);
            if (!string.IsNullOrEmpty(playerId))
            {
                _currentPlayerId = playerId;
                _currentPlayerName = displayName;
                SaveToPrefs();
                Debug.Log($"[PlayerService] Created new player record: {playerId}");
            }

            return playerId;
        }

        // ============================================================
        // CREATE PLAYER
        // ============================================================

        /// <summary>
        /// Creates a new player record in the players table.
        /// </summary>
        /// <param name="displayName">Player's display name</param>
        /// <returns>New player ID (UUID) or null on failure</returns>
        private async UniTask<string> CreatePlayerRecordAsync(string displayName)
        {
            // Build JSON - no auth_id needed, just display_name and is_ai
            string json = $"{{\"display_name\":\"{EscapeJson(displayName)}\",\"is_ai\":false}}";

            SupabaseResponse response = await _client.Post(TABLE_PLAYERS, json);

            if (!response.Success)
            {
                Debug.LogError($"[PlayerService] Failed to create player record: {response.Error}");
                return null;
            }

            // Extract id from response
            // Response format: [{"id":"uuid-here","display_name":"..."}]
            string playerId = JsonParsingUtility.ExtractStringField(response.Body, "id");

            if (string.IsNullOrEmpty(playerId))
            {
                Debug.LogError("[PlayerService] Failed to parse player ID from response");
                return null;
            }

            return playerId;
        }

        // ============================================================
        // VERIFY PLAYER EXISTS
        // ============================================================

        /// <summary>
        /// Verifies that a player record exists in the database.
        /// </summary>
        private async UniTask<bool> VerifyPlayerExistsAsync(string playerId)
        {
            SupabaseResponse response = await _client.Get(TABLE_PLAYERS, $"id=eq.{playerId}&select=id");

            if (!response.Success)
            {
                return false;
            }

            // Check if we got any results
            return !string.IsNullOrEmpty(response.Body) && response.Body != "[]";
        }

        // ============================================================
        // PHANTOM AI PLAYER
        // ============================================================

        /// <summary>
        /// Ensures the phantom AI (Executioner) player record exists in the database.
        /// The phantom AI uses a reserved UUID so all phantom AI games use the same player record.
        /// This is called during matchmaking timeout to create a real session_players entry.
        /// </summary>
        /// <returns>True if the phantom AI player exists or was created successfully</returns>
        public async UniTask<bool> EnsurePhantomAIPlayerExistsAsync()
        {
            // Check if the phantom AI player already exists
            bool exists = await VerifyPlayerExistsAsync(EXECUTIONER_PLAYER_ID);
            if (exists)
            {
                Debug.Log("[PlayerService] Phantom AI player already exists");
                return true;
            }

            // Create the phantom AI player with the reserved UUID
            // Note: Supabase normally auto-generates UUIDs, but we can insert with a specific ID
            string json = $"{{\"id\":\"{EXECUTIONER_PLAYER_ID}\",\"display_name\":\"The Executioner\",\"is_ai\":true}}";

            SupabaseResponse response = await _client.Post(TABLE_PLAYERS, json);

            if (!response.Success)
            {
                // If error is duplicate key, the record already exists (race condition) - that's fine
                if (response.Error != null && response.Error.Contains("duplicate"))
                {
                    Debug.Log("[PlayerService] Phantom AI player created by another process");
                    return true;
                }

                Debug.LogError($"[PlayerService] Failed to create phantom AI player: {response.Error}");
                return false;
            }

            Debug.Log("[PlayerService] Created phantom AI player record");
            return true;
        }

        // ============================================================
        // UPDATE PLAYER NAME
        // ============================================================

        /// <summary>
        /// Updates the player's display name.
        /// </summary>
        private async UniTask<bool> UpdatePlayerNameAsync(string playerId, string newName)
        {
            string json = $"{{\"display_name\":\"{EscapeJson(newName)}\"}}";
            SupabaseResponse response = await _client.Patch(TABLE_PLAYERS, $"id=eq.{playerId}", json);

            if (!response.Success)
            {
                Debug.LogWarning($"[PlayerService] Failed to update player name: {response.Error}");
            }

            return response.Success;
        }

        // ============================================================
        // GET PLAYER BY ID
        // ============================================================

        /// <summary>
        /// Gets a player record by ID.
        /// </summary>
        public async UniTask<PlayerRecord> GetPlayerAsync(string playerId)
        {
            if (string.IsNullOrEmpty(playerId))
            {
                return null;
            }

            SupabaseResponse response = await _client.Get(TABLE_PLAYERS, $"id=eq.{playerId}");

            if (!response.Success || string.IsNullOrEmpty(response.Body) || response.Body == "[]")
            {
                return null;
            }

            return ParsePlayerRecord(response.Body);
        }

        // ============================================================
        // CLEAR PLAYER
        // ============================================================

        /// <summary>
        /// Clears the cached player ID (for sign-out scenarios).
        /// Does NOT delete the database record.
        /// </summary>
        public void ClearCachedPlayer()
        {
            _currentPlayerId = null;
            _currentPlayerName = null;
            PlayerPrefs.DeleteKey(PREFS_PLAYER_ID);
            PlayerPrefs.DeleteKey(PREFS_PLAYER_NAME);
            PlayerPrefs.Save();
            Debug.Log("[PlayerService] Cleared cached player");
        }

        // ============================================================
        // PERSISTENCE
        // ============================================================

        private void SaveToPrefs()
        {
            if (!string.IsNullOrEmpty(_currentPlayerId))
            {
                PlayerPrefs.SetString(PREFS_PLAYER_ID, _currentPlayerId);
                PlayerPrefs.SetString(PREFS_PLAYER_NAME, _currentPlayerName ?? "");
                PlayerPrefs.Save();
            }
        }

        private void RestoreFromPrefs()
        {
            _currentPlayerId = PlayerPrefs.GetString(PREFS_PLAYER_ID, "");
            _currentPlayerName = PlayerPrefs.GetString(PREFS_PLAYER_NAME, "");

            if (string.IsNullOrEmpty(_currentPlayerId))
            {
                _currentPlayerId = null;
                _currentPlayerName = null;
            }
            else
            {
                Debug.Log($"[PlayerService] Restored player from prefs: {_currentPlayerId}");
            }
        }

        // ============================================================
        // JSON HELPERS
        // ============================================================

        private PlayerRecord ParsePlayerRecord(string json)
        {
            if (string.IsNullOrEmpty(json) || json == "[]")
            {
                return null;
            }

            // If array, extract first element
            if (json.StartsWith("["))
            {
                int objStart = json.IndexOf('{');
                int objEnd = json.LastIndexOf('}');
                if (objStart >= 0 && objEnd > objStart)
                {
                    json = json.Substring(objStart, objEnd - objStart + 1);
                }
            }

            PlayerRecord record = new PlayerRecord();
            record.Id = JsonParsingUtility.ExtractStringField(json, "id");
            record.DisplayName = JsonParsingUtility.ExtractStringField(json, "display_name");
            record.AuthId = JsonParsingUtility.ExtractStringField(json, "auth_id");
            record.IsAI = JsonParsingUtility.ExtractBoolField(json, "is_ai");

            return record;
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
    /// Represents a player record from the players table.
    /// </summary>
    [Serializable]
    public class PlayerRecord
    {
        public string Id;           // UUID from players table
        public string DisplayName;  // Player's display name
        public string AuthId;       // Optional auth.users.id (null for guests)
        public bool IsAI;           // True for AI players (The Executioner)
    }
}

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
        /// </summary>
        /// <param name="displayName">Player's display name</param>
        /// <returns>Player ID (UUID) or null on failure</returns>
        public async UniTask<string> EnsurePlayerRecordAsync(string displayName)
        {
            if (string.IsNullOrEmpty(displayName))
            {
                displayName = "Player";
            }

            // If we have a cached player ID with the SAME name, verify it still exists
            // Different names = different players (important for testing multiple players on same machine)
            if (!string.IsNullOrEmpty(_currentPlayerId) && _currentPlayerName == displayName)
            {
                bool exists = await VerifyPlayerExistsAsync(_currentPlayerId);
                if (exists)
                {
                    Debug.Log($"[PlayerService] Using existing player record: {_currentPlayerId}");
                    return _currentPlayerId;
                }
                else
                {
                    // Cached ID is stale, clear it
                    Debug.Log("[PlayerService] Cached player ID is stale, creating new record");
                    _currentPlayerId = null;
                    _currentPlayerName = null;
                }
            }
            else if (!string.IsNullOrEmpty(_currentPlayerId) && _currentPlayerName != displayName)
            {
                // Different name requested - create a new player record
                Debug.Log($"[PlayerService] Different player name requested ({displayName} vs {_currentPlayerName}), creating new record");
                _currentPlayerId = null;
                _currentPlayerName = null;
            }

            // Create new player record
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
            string playerId = ExtractIdFromResponse(response.Body);

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

        private string ExtractIdFromResponse(string json)
        {
            // Response format: [{"id":"uuid-here",...}] or {"id":"uuid-here",...}
            if (string.IsNullOrEmpty(json))
            {
                return null;
            }

            string pattern = "\"id\":\"";
            int start = json.IndexOf(pattern);
            if (start < 0)
            {
                return null;
            }

            start += pattern.Length;
            int end = json.IndexOf("\"", start);
            if (end < 0)
            {
                return null;
            }

            return json.Substring(start, end - start);
        }

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
            record.Id = ExtractStringField(json, "id");
            record.DisplayName = ExtractStringField(json, "display_name");
            record.AuthId = ExtractStringField(json, "auth_id");

            string isAiStr = ExtractStringField(json, "is_ai");
            record.IsAI = isAiStr == "true";

            return record;
        }

        private string ExtractStringField(string json, string fieldName)
        {
            string pattern = $"\"{fieldName}\":\"";
            int start = json.IndexOf(pattern);
            if (start < 0)
            {
                // Try boolean/null format
                pattern = $"\"{fieldName}\":";
                start = json.IndexOf(pattern);
                if (start >= 0)
                {
                    start += pattern.Length;
                    int end = json.IndexOfAny(new[] { ',', '}' }, start);
                    if (end > start)
                    {
                        string value = json.Substring(start, end - start).Trim();
                        if (value == "null") return null;
                        return value;
                    }
                }
                return null;
            }

            start += pattern.Length;
            int endQuote = json.IndexOf("\"", start);
            if (endQuote < 0)
            {
                return null;
            }

            return json.Substring(start, endQuote - start);
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

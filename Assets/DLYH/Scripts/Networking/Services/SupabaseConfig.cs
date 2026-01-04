// SupabaseConfig.cs
// ScriptableObject for Supabase configuration
// Created: January 4, 2026
// Developer: TecVooDoo LLC

using UnityEngine;

namespace DLYH.Networking.Services
{
    /// <summary>
    /// ScriptableObject holding Supabase connection configuration.
    /// Create via Assets > Create > DLYH > Supabase Config
    /// </summary>
    [CreateAssetMenu(fileName = "SupabaseConfig", menuName = "DLYH/Supabase Config")]
    public class SupabaseConfig : ScriptableObject
    {
        [Header("Supabase Project")]
        [Tooltip("Supabase project URL (e.g., https://api.tecvoodoo.com or https://xxx.supabase.co)")]
        [SerializeField] private string _projectUrl = "https://api.tecvoodoo.com";

        [Tooltip("Supabase anon/public key (safe to include in client builds)")]
        [SerializeField] private string _anonKey = "";

        [Header("Game Settings")]
        [Tooltip("Game type identifier for this game in the game_types table")]
        [SerializeField] private string _gameTypeId = "dlyh";

        [Header("Timeouts")]
        [Tooltip("HTTP request timeout in seconds")]
        [SerializeField] private int _requestTimeoutSeconds = 30;

        [Tooltip("Days of inactivity before a player forfeits")]
        [SerializeField] private int _inactivityForfeitDays = 3;

        // ============================================================
        // PROPERTIES
        // ============================================================

        /// <summary>Supabase project URL</summary>
        public string ProjectUrl => _projectUrl;

        /// <summary>Supabase anon/public API key</summary>
        public string AnonKey => _anonKey;

        /// <summary>Game type identifier (e.g., "dlyh")</summary>
        public string GameTypeId => _gameTypeId;

        /// <summary>HTTP request timeout in seconds</summary>
        public int RequestTimeoutSeconds => _requestTimeoutSeconds;

        /// <summary>Days of inactivity before forfeit</summary>
        public int InactivityForfeitDays => _inactivityForfeitDays;

        // ============================================================
        // DERIVED URLs
        // ============================================================

        /// <summary>REST API base URL</summary>
        public string RestUrl => $"{_projectUrl}/rest/v1";

        /// <summary>Auth API base URL</summary>
        public string AuthUrl => $"{_projectUrl}/auth/v1";

        /// <summary>Realtime WebSocket URL</summary>
        public string RealtimeUrl
        {
            get
            {
                // Convert https:// to wss://
                string wsUrl = _projectUrl.Replace("https://", "wss://").Replace("http://", "ws://");
                return $"{wsUrl}/realtime/v1/websocket";
            }
        }

        // ============================================================
        // VALIDATION
        // ============================================================

        /// <summary>Check if configuration is valid</summary>
        public bool IsValid => !string.IsNullOrEmpty(_projectUrl) && !string.IsNullOrEmpty(_anonKey);

        /// <summary>Get validation error message if invalid</summary>
        public string ValidationError
        {
            get
            {
                if (string.IsNullOrEmpty(_projectUrl))
                    return "Project URL is not configured";
                if (string.IsNullOrEmpty(_anonKey))
                    return "Anon Key is not configured";
                return null;
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            // Ensure URL doesn't have trailing slash
            if (!string.IsNullOrEmpty(_projectUrl) && _projectUrl.EndsWith("/"))
            {
                _projectUrl = _projectUrl.TrimEnd('/');
            }
        }
#endif
    }
}

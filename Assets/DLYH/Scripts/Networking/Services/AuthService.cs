// AuthService.cs
// Anonymous authentication service for Supabase
// Created: January 4, 2026
// Developer: TecVooDoo LLC

using System;
using UnityEngine;
using Cysharp.Threading.Tasks;

namespace DLYH.Networking.Services
{
    /// <summary>
    /// Data class for authentication session.
    /// </summary>
    [Serializable]
    public class AuthSession
    {
        public string AccessToken;
        public string RefreshToken;
        public string UserId;
        public long ExpiresAt;

        /// <summary>Check if session is expired (with 60 second buffer)</summary>
        public bool IsExpired => DateTimeOffset.UtcNow.ToUnixTimeSeconds() >= (ExpiresAt - 60);
    }

    /// <summary>
    /// Handles anonymous authentication with Supabase Auth.
    /// Stores session in PlayerPrefs for persistence.
    /// </summary>
    public class AuthService
    {
        // ============================================================
        // EVENTS
        // ============================================================

        /// <summary>Fired when user signs in</summary>
        public event Action<AuthSession> OnSignedIn;

        /// <summary>Fired when user signs out</summary>
        public event Action OnSignedOut;

        /// <summary>Fired when session is refreshed</summary>
        public event Action<AuthSession> OnSessionRefreshed;

        // ============================================================
        // CONSTANTS
        // ============================================================

        private const string PREFS_ACCESS_TOKEN = "DLYH_AccessToken";
        private const string PREFS_REFRESH_TOKEN = "DLYH_RefreshToken";
        private const string PREFS_USER_ID = "DLYH_UserId";
        private const string PREFS_EXPIRES_AT = "DLYH_ExpiresAt";

        // ============================================================
        // STATE
        // ============================================================

        private readonly SupabaseConfig _config;
        private readonly SupabaseClient _client;
        private AuthSession _currentSession;

        // ============================================================
        // PROPERTIES
        // ============================================================

        public AuthSession CurrentSession => _currentSession;
        public bool IsSignedIn => _currentSession != null && !string.IsNullOrEmpty(_currentSession.AccessToken);
        public string UserId => _currentSession?.UserId;
        public string AccessToken => _currentSession?.AccessToken;

        // ============================================================
        // CONSTRUCTOR
        // ============================================================

        public AuthService(SupabaseConfig config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _client = new SupabaseClient(config);

            // Try to restore session from PlayerPrefs
            RestoreSession();
        }

        // ============================================================
        // ANONYMOUS SIGN IN
        // ============================================================

        /// <summary>
        /// Signs in anonymously. Creates a new anonymous user if not already signed in.
        /// </summary>
        public async UniTask<AuthSession> SignInAnonymouslyAsync()
        {
            // Check if we have a valid existing session
            if (_currentSession != null && !_currentSession.IsExpired)
            {
                Debug.Log($"[AuthService] Using existing session for user {_currentSession.UserId}");
                return _currentSession;
            }

            // Try to refresh if we have a refresh token
            if (_currentSession != null && !string.IsNullOrEmpty(_currentSession.RefreshToken))
            {
                var refreshed = await RefreshSessionAsync();
                if (refreshed != null)
                {
                    return refreshed;
                }
            }

            // Create new anonymous user
            Debug.Log("[AuthService] Creating new anonymous user...");

            string url = $"{_config.AuthUrl}/signup";
            string body = "{}"; // Empty body for anonymous signup

            var response = await _client.PostToUrl(url, body);

            if (response.Success)
            {
                var session = ParseAuthResponse(response.Body);
                if (session != null)
                {
                    _currentSession = session;
                    SaveSession();
                    OnSignedIn?.Invoke(session);
                    Debug.Log($"[AuthService] Signed in anonymously as {session.UserId}");
                    return session;
                }
            }

            Debug.LogError($"[AuthService] Anonymous sign in failed: {response.Error ?? response.Body}");
            return null;
        }

        // ============================================================
        // REFRESH SESSION
        // ============================================================

        /// <summary>
        /// Refreshes the current session using the refresh token.
        /// </summary>
        public async UniTask<AuthSession> RefreshSessionAsync()
        {
            if (_currentSession == null || string.IsNullOrEmpty(_currentSession.RefreshToken))
            {
                Debug.LogWarning("[AuthService] No refresh token available");
                return null;
            }

            Debug.Log("[AuthService] Refreshing session...");

            string url = $"{_config.AuthUrl}/token?grant_type=refresh_token";
            string body = $"{{\"refresh_token\":\"{_currentSession.RefreshToken}\"}}";

            var response = await _client.PostToUrl(url, body);

            if (response.Success)
            {
                var session = ParseAuthResponse(response.Body);
                if (session != null)
                {
                    _currentSession = session;
                    SaveSession();
                    OnSessionRefreshed?.Invoke(session);
                    Debug.Log("[AuthService] Session refreshed successfully");
                    return session;
                }
            }

            Debug.LogWarning($"[AuthService] Session refresh failed: {response.Error ?? response.Body}");
            return null;
        }

        // ============================================================
        // SIGN OUT
        // ============================================================

        /// <summary>
        /// Signs out the current user and clears the session.
        /// </summary>
        public async UniTask SignOutAsync()
        {
            if (_currentSession != null && !string.IsNullOrEmpty(_currentSession.AccessToken))
            {
                // Call logout endpoint (optional, just invalidates server-side)
                string url = $"{_config.AuthUrl}/logout";
                _client.SetAccessToken(_currentSession.AccessToken);
                await _client.PostToUrl(url, "{}");
            }

            ClearSession();
            OnSignedOut?.Invoke();
            Debug.Log("[AuthService] Signed out");
        }

        // ============================================================
        // ENSURE VALID SESSION
        // ============================================================

        /// <summary>
        /// Ensures we have a valid session, refreshing or signing in as needed.
        /// </summary>
        public async UniTask<AuthSession> EnsureValidSessionAsync()
        {
            if (_currentSession == null)
            {
                return await SignInAnonymouslyAsync();
            }

            if (_currentSession.IsExpired)
            {
                var refreshed = await RefreshSessionAsync();
                if (refreshed != null) return refreshed;

                // Refresh failed, sign in again
                return await SignInAnonymouslyAsync();
            }

            return _currentSession;
        }

        // ============================================================
        // SESSION PERSISTENCE
        // ============================================================

        private void SaveSession()
        {
            if (_currentSession == null) return;

            PlayerPrefs.SetString(PREFS_ACCESS_TOKEN, _currentSession.AccessToken ?? "");
            PlayerPrefs.SetString(PREFS_REFRESH_TOKEN, _currentSession.RefreshToken ?? "");
            PlayerPrefs.SetString(PREFS_USER_ID, _currentSession.UserId ?? "");
            PlayerPrefs.SetString(PREFS_EXPIRES_AT, _currentSession.ExpiresAt.ToString());
            PlayerPrefs.Save();
        }

        private void RestoreSession()
        {
            string accessToken = PlayerPrefs.GetString(PREFS_ACCESS_TOKEN, "");
            string refreshToken = PlayerPrefs.GetString(PREFS_REFRESH_TOKEN, "");
            string userId = PlayerPrefs.GetString(PREFS_USER_ID, "");
            string expiresAtStr = PlayerPrefs.GetString(PREFS_EXPIRES_AT, "0");

            if (!string.IsNullOrEmpty(accessToken) && !string.IsNullOrEmpty(userId))
            {
                long.TryParse(expiresAtStr, out long expiresAt);

                _currentSession = new AuthSession
                {
                    AccessToken = accessToken,
                    RefreshToken = refreshToken,
                    UserId = userId,
                    ExpiresAt = expiresAt
                };

                Debug.Log($"[AuthService] Restored session for user {userId}");
            }
        }

        private void ClearSession()
        {
            _currentSession = null;
            PlayerPrefs.DeleteKey(PREFS_ACCESS_TOKEN);
            PlayerPrefs.DeleteKey(PREFS_REFRESH_TOKEN);
            PlayerPrefs.DeleteKey(PREFS_USER_ID);
            PlayerPrefs.DeleteKey(PREFS_EXPIRES_AT);
            PlayerPrefs.Save();
        }

        // ============================================================
        // RESPONSE PARSING
        // ============================================================

        private AuthSession ParseAuthResponse(string json)
        {
            try
            {
                // Extract fields from Supabase auth response
                // Format: {"access_token":"...", "refresh_token":"...", "expires_at":123, "user":{"id":"..."}}

                string accessToken = ExtractJsonValue(json, "access_token");
                string refreshToken = ExtractJsonValue(json, "refresh_token");
                string expiresAtStr = ExtractJsonValue(json, "expires_at");
                string userId = ExtractNestedValue(json, "user", "id");

                if (string.IsNullOrEmpty(accessToken) || string.IsNullOrEmpty(userId))
                {
                    Debug.LogError("[AuthService] Failed to parse auth response - missing required fields");
                    return null;
                }

                long.TryParse(expiresAtStr, out long expiresAt);

                return new AuthSession
                {
                    AccessToken = accessToken,
                    RefreshToken = refreshToken,
                    UserId = userId,
                    ExpiresAt = expiresAt
                };
            }
            catch (Exception ex)
            {
                Debug.LogError($"[AuthService] Error parsing auth response: {ex.Message}");
                return null;
            }
        }

        private string ExtractJsonValue(string json, string key)
        {
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

        private string ExtractNestedValue(string json, string parent, string key)
        {
            string parentKey = $"\"{parent}\":{{";
            int parentStart = json.IndexOf(parentKey);
            if (parentStart < 0) return null;

            string searchKey = $"\"{key}\":\"";
            int keyStart = json.IndexOf(searchKey, parentStart);
            if (keyStart < 0) return null;

            keyStart += searchKey.Length;
            int endQuote = json.IndexOf('"', keyStart);
            if (endQuote < 0) return null;

            return json.Substring(keyStart, endQuote - keyStart);
        }
    }
}

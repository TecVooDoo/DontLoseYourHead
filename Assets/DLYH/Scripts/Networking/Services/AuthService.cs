// AuthService.cs
// Authentication service for Supabase with OAuth support
// Created: January 4, 2026
// Updated: January 16, 2026 - Added OAuth (Google, Facebook, Magic Link)
// Developer: TecVooDoo LLC

using System;
using UnityEngine;
using Cysharp.Threading.Tasks;

namespace DLYH.Networking.Services
{
    /// <summary>
    /// Authentication state enum matching Supabase JS pattern.
    /// </summary>
    public enum AuthState
    {
        SignedOut,
        SignedIn,
        Loading
    }

    /// <summary>
    /// OAuth provider types.
    /// </summary>
    public enum OAuthProvider
    {
        Google,
        Facebook
    }

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
        public string Email;
        public string DisplayName;
        public bool IsAnonymous;

        /// <summary>Check if session is expired (with 60 second buffer)</summary>
        public bool IsExpired => DateTimeOffset.UtcNow.ToUnixTimeSeconds() >= (ExpiresAt - 60);
    }

    /// <summary>
    /// Handles authentication with Supabase Auth.
    /// Supports OAuth (Google, Facebook), Magic Link, and Anonymous sign-in.
    /// Stores session in PlayerPrefs for persistence.
    /// </summary>
    public class AuthService
    {
        // ============================================================
        // EVENTS
        // ============================================================

        /// <summary>Fired when auth state changes (signed in, signed out, loading)</summary>
        public event Action<AuthState> OnAuthStateChanged;

        /// <summary>Fired when user signs in</summary>
        public event Action<AuthSession> OnSignedIn;

        /// <summary>Fired when user signs out</summary>
        public event Action OnSignedOut;

        /// <summary>Fired when session is refreshed</summary>
        public event Action<AuthSession> OnSessionRefreshed;

        /// <summary>Fired when OAuth flow starts (browser opens)</summary>
        public event Action<OAuthProvider> OnOAuthStarted;

        /// <summary>Fired when magic link is sent</summary>
        public event Action<string> OnMagicLinkSent;

        /// <summary>Fired when auth error occurs</summary>
        public event Action<string> OnAuthError;

        // ============================================================
        // CONSTANTS
        // ============================================================

        private const string PREFS_ACCESS_TOKEN = "DLYH_AccessToken";
        private const string PREFS_REFRESH_TOKEN = "DLYH_RefreshToken";
        private const string PREFS_USER_ID = "DLYH_UserId";
        private const string PREFS_EXPIRES_AT = "DLYH_ExpiresAt";
        private const string PREFS_EMAIL = "DLYH_Email";
        private const string PREFS_DISPLAY_NAME = "DLYH_DisplayName";
        private const string PREFS_IS_ANONYMOUS = "DLYH_IsAnonymous";

        // OAuth redirect URL - must match Supabase dashboard configuration
        // Using dlyh.pages.dev (Cloudflare Pages) - add this URL to Supabase Dashboard > Auth > URL Configuration
        private const string OAUTH_REDIRECT_URL = "https://dlyh.pages.dev/auth-callback";

        // ============================================================
        // STATE
        // ============================================================

        private readonly SupabaseConfig _config;
        private readonly SupabaseClient _client;
        private AuthSession _currentSession;
        private AuthState _currentState = AuthState.SignedOut;
        private bool _isOAuthPending;

        // ============================================================
        // PROPERTIES
        // ============================================================

        public AuthSession CurrentSession => _currentSession;
        public AuthState CurrentState => _currentState;
        public bool IsSignedIn => _currentSession != null && !string.IsNullOrEmpty(_currentSession.AccessToken);
        public bool IsAnonymous => _currentSession?.IsAnonymous ?? true;
        public string UserId => _currentSession?.UserId;
        public string AccessToken => _currentSession?.AccessToken;
        public string Email => _currentSession?.Email;
        public string DisplayName => _currentSession?.DisplayName;
        public bool IsOAuthPending => _isOAuthPending;

        // ============================================================
        // CONSTRUCTOR
        // ============================================================

        public AuthService(SupabaseConfig config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _client = new SupabaseClient(config);

            // Try to restore session from PlayerPrefs
            RestoreSession();

            // Set initial state based on restored session
            if (_currentSession != null && !_currentSession.IsExpired)
            {
                SetAuthState(AuthState.SignedIn);
            }
            else
            {
                SetAuthState(AuthState.SignedOut);
            }
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
                AuthSession refreshed = await RefreshSessionAsync();
                if (refreshed != null)
                {
                    return refreshed;
                }
            }

            SetAuthState(AuthState.Loading);

            // Create new anonymous user
            Debug.Log("[AuthService] Creating new anonymous user...");

            string url = $"{_config.AuthUrl}/signup";
            string body = "{}"; // Empty body for anonymous signup

            SupabaseResponse response = await _client.PostToUrl(url, body);

            if (response.Success)
            {
                AuthSession session = ParseAuthResponse(response.Body);
                if (session != null)
                {
                    session.IsAnonymous = true;
                    _currentSession = session;
                    SaveSession();
                    SetAuthState(AuthState.SignedIn);
                    OnSignedIn?.Invoke(session);
                    Debug.Log($"[AuthService] Signed in anonymously as {session.UserId}");
                    return session;
                }
            }

            SetAuthState(AuthState.SignedOut);
            string errorMsg = $"Anonymous sign in failed: {response.Error ?? response.Body}";
            Debug.LogError($"[AuthService] {errorMsg}");
            OnAuthError?.Invoke(errorMsg);
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
        // OAUTH SIGN IN
        // ============================================================

        /// <summary>
        /// Initiates Google OAuth sign-in flow.
        /// Opens system browser for authentication.
        /// Call HandleAuthCallbackAsync when redirect is received.
        /// </summary>
        public void SignInWithGoogle()
        {
            SignInWithOAuth(OAuthProvider.Google);
        }

        /// <summary>
        /// Initiates Facebook OAuth sign-in flow.
        /// Opens system browser for authentication.
        /// Call HandleAuthCallbackAsync when redirect is received.
        /// </summary>
        public void SignInWithFacebook()
        {
            SignInWithOAuth(OAuthProvider.Facebook);
        }

        /// <summary>
        /// Initiates OAuth sign-in flow for the specified provider.
        /// </summary>
        private void SignInWithOAuth(OAuthProvider provider)
        {
            string providerName = provider.ToString().ToLower();

            // Build OAuth URL
            // Format: {auth_url}/authorize?provider={provider}&redirect_to={redirect_url}
            string oauthUrl = $"{_config.AuthUrl}/authorize?provider={providerName}&redirect_to={Uri.EscapeDataString(OAUTH_REDIRECT_URL)}";

            // Add prompt=select_account for Google to force account selection
            if (provider == OAuthProvider.Google)
            {
                oauthUrl += "&query_params=" + Uri.EscapeDataString("prompt=select_account");
            }

            Debug.Log($"[AuthService] Starting OAuth flow for {providerName}");
            Debug.Log($"[AuthService] OAuth URL: {oauthUrl}");

            _isOAuthPending = true;
            SetAuthState(AuthState.Loading);
            OnOAuthStarted?.Invoke(provider);

            // Open system browser
            Application.OpenURL(oauthUrl);
        }

        /// <summary>
        /// Handles the OAuth callback URL containing tokens.
        /// Call this when the redirect is received (from deep link or WebGL bridge).
        /// </summary>
        /// <param name="callbackUrl">The full callback URL with tokens in hash fragment</param>
        public async UniTask<AuthSession> HandleAuthCallbackAsync(string callbackUrl)
        {
            Debug.Log($"[AuthService] Handling auth callback: {callbackUrl}");
            _isOAuthPending = false;

            try
            {
                // Extract tokens from URL hash fragment
                // Format: https://dlyh.tecvoodoo.com/auth-callback#access_token=...&refresh_token=...&expires_at=...
                int hashIndex = callbackUrl.IndexOf('#');
                if (hashIndex < 0)
                {
                    // Try query string format (some providers use this)
                    hashIndex = callbackUrl.IndexOf('?');
                }

                if (hashIndex < 0)
                {
                    string errorMsg = "Auth callback missing token fragment";
                    Debug.LogError($"[AuthService] {errorMsg}");
                    SetAuthState(AuthState.SignedOut);
                    OnAuthError?.Invoke(errorMsg);
                    return null;
                }

                string fragment = callbackUrl.Substring(hashIndex + 1);
                System.Collections.Generic.Dictionary<string, string> parameters = ParseQueryString(fragment);

                string accessToken = GetParameterValue(parameters, "access_token");
                string refreshToken = GetParameterValue(parameters, "refresh_token");
                string expiresAtStr = GetParameterValue(parameters, "expires_at");

                if (string.IsNullOrEmpty(accessToken))
                {
                    // Check for error in callback
                    string error = GetParameterValue(parameters, "error");
                    string errorDescription = GetParameterValue(parameters, "error_description");
                    string errorMsg = !string.IsNullOrEmpty(errorDescription) ? errorDescription : (error ?? "Unknown OAuth error");
                    Debug.LogError($"[AuthService] OAuth error: {errorMsg}");
                    SetAuthState(AuthState.SignedOut);
                    OnAuthError?.Invoke(errorMsg);
                    return null;
                }

                // Get user info from Supabase
                _client.SetAccessToken(accessToken);
                SupabaseResponse userResponse = await _client.PostToUrl($"{_config.AuthUrl}/user", "{}");

                AuthSession session = new AuthSession
                {
                    AccessToken = accessToken,
                    RefreshToken = refreshToken,
                    IsAnonymous = false
                };

                if (long.TryParse(expiresAtStr, out long expiresAt))
                {
                    session.ExpiresAt = expiresAt;
                }

                // Parse user info from response
                if (userResponse.Success)
                {
                    session.UserId = ExtractJsonValue(userResponse.Body, "id");
                    session.Email = ExtractJsonValue(userResponse.Body, "email");

                    // Try to get display name from user_metadata
                    string userMetadata = ExtractJsonObject(userResponse.Body, "user_metadata");
                    if (!string.IsNullOrEmpty(userMetadata))
                    {
                        session.DisplayName = ExtractJsonValue(userMetadata, "full_name");
                        if (string.IsNullOrEmpty(session.DisplayName))
                        {
                            session.DisplayName = ExtractJsonValue(userMetadata, "name");
                        }
                    }

                    // Fallback display name from email
                    if (string.IsNullOrEmpty(session.DisplayName) && !string.IsNullOrEmpty(session.Email))
                    {
                        int atIndex = session.Email.IndexOf('@');
                        session.DisplayName = atIndex > 0 ? session.Email.Substring(0, atIndex) : session.Email;
                    }
                }

                _currentSession = session;
                SaveSession();
                SetAuthState(AuthState.SignedIn);
                OnSignedIn?.Invoke(session);
                Debug.Log($"[AuthService] OAuth sign-in successful: {session.Email ?? session.UserId}");
                return session;
            }
            catch (Exception ex)
            {
                string errorMsg = $"Error handling auth callback: {ex.Message}";
                Debug.LogError($"[AuthService] {errorMsg}");
                SetAuthState(AuthState.SignedOut);
                OnAuthError?.Invoke(errorMsg);
                return null;
            }
        }

        // ============================================================
        // MAGIC LINK SIGN IN
        // ============================================================

        /// <summary>
        /// Sends a magic link (passwordless sign-in) to the specified email.
        /// User clicks the link in their email to complete sign-in.
        /// </summary>
        /// <param name="email">Email address to send magic link to</param>
        /// <returns>True if magic link was sent successfully</returns>
        public async UniTask<bool> SignInWithMagicLinkAsync(string email)
        {
            if (string.IsNullOrEmpty(email))
            {
                OnAuthError?.Invoke("Email address is required");
                return false;
            }

            Debug.Log($"[AuthService] Sending magic link to {email}...");
            SetAuthState(AuthState.Loading);

            string url = $"{_config.AuthUrl}/otp";
            string body = $"{{\"email\":\"{EscapeJson(email)}\",\"options\":{{\"emailRedirectTo\":\"{OAUTH_REDIRECT_URL}\"}}}}";

            SupabaseResponse response = await _client.PostToUrl(url, body);

            if (response.Success)
            {
                Debug.Log($"[AuthService] Magic link sent to {email}");
                SetAuthState(AuthState.SignedOut); // Still signed out until they click the link
                OnMagicLinkSent?.Invoke(email);
                return true;
            }

            SetAuthState(AuthState.SignedOut);
            string errorMsg = $"Failed to send magic link: {response.Error ?? response.Body}";
            Debug.LogError($"[AuthService] {errorMsg}");
            OnAuthError?.Invoke(errorMsg);
            return false;
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
            SetAuthState(AuthState.SignedOut);
            OnSignedOut?.Invoke();
            Debug.Log("[AuthService] Signed out");
        }

        /// <summary>
        /// Cancels a pending OAuth flow.
        /// </summary>
        public void CancelOAuth()
        {
            if (_isOAuthPending)
            {
                _isOAuthPending = false;
                SetAuthState(AuthState.SignedOut);
                Debug.Log("[AuthService] OAuth flow cancelled");
            }
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
            PlayerPrefs.SetString(PREFS_EMAIL, _currentSession.Email ?? "");
            PlayerPrefs.SetString(PREFS_DISPLAY_NAME, _currentSession.DisplayName ?? "");
            PlayerPrefs.SetInt(PREFS_IS_ANONYMOUS, _currentSession.IsAnonymous ? 1 : 0);
            PlayerPrefs.Save();
        }

        private void RestoreSession()
        {
            string accessToken = PlayerPrefs.GetString(PREFS_ACCESS_TOKEN, "");
            string refreshToken = PlayerPrefs.GetString(PREFS_REFRESH_TOKEN, "");
            string userId = PlayerPrefs.GetString(PREFS_USER_ID, "");
            string expiresAtStr = PlayerPrefs.GetString(PREFS_EXPIRES_AT, "0");
            string email = PlayerPrefs.GetString(PREFS_EMAIL, "");
            string displayName = PlayerPrefs.GetString(PREFS_DISPLAY_NAME, "");
            int isAnonymousInt = PlayerPrefs.GetInt(PREFS_IS_ANONYMOUS, 1);

            if (!string.IsNullOrEmpty(accessToken) && !string.IsNullOrEmpty(userId))
            {
                long.TryParse(expiresAtStr, out long expiresAt);

                _currentSession = new AuthSession
                {
                    AccessToken = accessToken,
                    RefreshToken = refreshToken,
                    UserId = userId,
                    ExpiresAt = expiresAt,
                    Email = string.IsNullOrEmpty(email) ? null : email,
                    DisplayName = string.IsNullOrEmpty(displayName) ? null : displayName,
                    IsAnonymous = isAnonymousInt == 1
                };

                Debug.Log($"[AuthService] Restored session for user {userId} (anonymous: {_currentSession.IsAnonymous})");
            }
        }

        private void ClearSession()
        {
            _currentSession = null;
            PlayerPrefs.DeleteKey(PREFS_ACCESS_TOKEN);
            PlayerPrefs.DeleteKey(PREFS_REFRESH_TOKEN);
            PlayerPrefs.DeleteKey(PREFS_USER_ID);
            PlayerPrefs.DeleteKey(PREFS_EXPIRES_AT);
            PlayerPrefs.DeleteKey(PREFS_EMAIL);
            PlayerPrefs.DeleteKey(PREFS_DISPLAY_NAME);
            PlayerPrefs.DeleteKey(PREFS_IS_ANONYMOUS);
            PlayerPrefs.Save();
        }

        // ============================================================
        // AUTH STATE
        // ============================================================

        private void SetAuthState(AuthState state)
        {
            if (_currentState != state)
            {
                _currentState = state;
                OnAuthStateChanged?.Invoke(state);
            }
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

        /// <summary>
        /// Extracts a JSON object as a string from the response.
        /// </summary>
        private string ExtractJsonObject(string json, string key)
        {
            string searchKey = $"\"{key}\":{{";
            int startIndex = json.IndexOf(searchKey);
            if (startIndex < 0) return null;

            startIndex += searchKey.Length - 1; // Include the opening brace

            // Find matching closing brace
            int depth = 1;
            int endIndex = startIndex + 1;
            while (endIndex < json.Length && depth > 0)
            {
                if (json[endIndex] == '{') depth++;
                else if (json[endIndex] == '}') depth--;
                endIndex++;
            }

            if (depth != 0) return null;

            return json.Substring(startIndex, endIndex - startIndex);
        }

        // ============================================================
        // URL/QUERY STRING HELPERS
        // ============================================================

        /// <summary>
        /// Parses a query string into a dictionary.
        /// </summary>
        private System.Collections.Generic.Dictionary<string, string> ParseQueryString(string queryString)
        {
            System.Collections.Generic.Dictionary<string, string> result = new System.Collections.Generic.Dictionary<string, string>();

            if (string.IsNullOrEmpty(queryString)) return result;

            string[] pairs = queryString.Split('&');
            foreach (string pair in pairs)
            {
                int equalsIndex = pair.IndexOf('=');
                if (equalsIndex > 0)
                {
                    string key = Uri.UnescapeDataString(pair.Substring(0, equalsIndex));
                    string value = equalsIndex < pair.Length - 1
                        ? Uri.UnescapeDataString(pair.Substring(equalsIndex + 1))
                        : "";
                    result[key] = value;
                }
            }

            return result;
        }

        /// <summary>
        /// Gets a value from the parameters dictionary, returning null if not found.
        /// </summary>
        private string GetParameterValue(System.Collections.Generic.Dictionary<string, string> parameters, string key)
        {
            if (parameters.TryGetValue(key, out string value))
            {
                return value;
            }
            return null;
        }

        /// <summary>
        /// Escapes a string for JSON.
        /// </summary>
        private string EscapeJson(string str)
        {
            if (string.IsNullOrEmpty(str)) return "";
            return str
                .Replace("\\", "\\\\")
                .Replace("\"", "\\\"")
                .Replace("\n", "\\n")
                .Replace("\r", "\\r")
                .Replace("\t", "\\t");
        }
    }
}

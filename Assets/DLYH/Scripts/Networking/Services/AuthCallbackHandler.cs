// AuthCallbackHandler.cs
// Handles OAuth callbacks from browser redirects
// Created: January 16, 2026
// Developer: TecVooDoo LLC
//
// Platform handling:
// - WebGL: JavaScript bridge calls OnAuthCallback via SendMessage
// - Desktop/Editor: Deep link handler or URL check on focus
// - Mobile: Deep link handler (dlyh://auth-callback)

using System;
using UnityEngine;
using Cysharp.Threading.Tasks;

namespace DLYH.Networking.Services
{
    /// <summary>
    /// MonoBehaviour that handles OAuth callbacks from browser redirects.
    /// Attach to a persistent GameObject (e.g., NetworkGameManager).
    /// </summary>
    public class AuthCallbackHandler : MonoBehaviour
    {
        // ============================================================
        // SINGLETON
        // ============================================================

        private static AuthCallbackHandler _instance;
        public static AuthCallbackHandler Instance => _instance;

        // ============================================================
        // EVENTS
        // ============================================================

        /// <summary>Fired when an auth callback URL is received</summary>
        public event Action<string> OnCallbackReceived;

        // ============================================================
        // STATE
        // ============================================================

        private AuthService _authService;
        private bool _isWaitingForCallback;

#if UNITY_WEBGL && !UNITY_EDITOR
        [System.Runtime.InteropServices.DllImport("__Internal")]
        private static extern void RegisterAuthCallback();

        [System.Runtime.InteropServices.DllImport("__Internal")]
        private static extern string GetPendingAuthCallback();

        [System.Runtime.InteropServices.DllImport("__Internal")]
        private static extern void ClearPendingAuthCallback();
#endif

        // ============================================================
        // UNITY LIFECYCLE
        // ============================================================

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;

#if UNITY_WEBGL && !UNITY_EDITOR
            // Register JavaScript callback handler
            RegisterAuthCallback();
#endif
        }

        private void Start()
        {
            // Check for auth callback in URL on startup (WebGL)
            CheckForAuthCallback();
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            // When app regains focus after OAuth redirect, check for callback
            if (hasFocus && _isWaitingForCallback)
            {
                CheckForAuthCallback();
            }
        }

        // ============================================================
        // INITIALIZATION
        // ============================================================

        /// <summary>
        /// Initializes the callback handler with an AuthService.
        /// </summary>
        public void Initialize(AuthService authService)
        {
            _authService = authService;

            // Subscribe to OAuth started to know when to watch for callbacks
            _authService.OnOAuthStarted += HandleOAuthStarted;
            _authService.OnSignedIn += HandleSignedIn;
            _authService.OnAuthError += HandleAuthError;
        }

        private void OnDestroy()
        {
            if (_authService != null)
            {
                _authService.OnOAuthStarted -= HandleOAuthStarted;
                _authService.OnSignedIn -= HandleSignedIn;
                _authService.OnAuthError -= HandleAuthError;
            }
        }

        // ============================================================
        // CALLBACK HANDLING
        // ============================================================

        /// <summary>
        /// Called when OAuth flow starts - begin watching for callback.
        /// </summary>
        private void HandleOAuthStarted(OAuthProvider provider)
        {
            _isWaitingForCallback = true;
            Debug.Log($"[AuthCallbackHandler] Waiting for OAuth callback from {provider}");
        }

        /// <summary>
        /// Called when sign-in completes - stop watching.
        /// </summary>
        private void HandleSignedIn(AuthSession session)
        {
            _isWaitingForCallback = false;
        }

        /// <summary>
        /// Called on auth error - stop watching.
        /// </summary>
        private void HandleAuthError(string error)
        {
            _isWaitingForCallback = false;
        }

        /// <summary>
        /// Check for auth callback in current context.
        /// </summary>
        private void CheckForAuthCallback()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            // WebGL: Check for pending callback from JavaScript
            string pendingCallback = GetPendingAuthCallback();
            if (!string.IsNullOrEmpty(pendingCallback))
            {
                Debug.Log("[AuthCallbackHandler] Found pending WebGL callback");
                ClearPendingAuthCallback();
                ProcessCallback(pendingCallback);
            }
#elif UNITY_STANDALONE || UNITY_EDITOR
            // Desktop/Editor: Check command line args or deep link
            CheckCommandLineArgs();
#elif UNITY_IOS || UNITY_ANDROID
            // Mobile: Deep links handled via OnDeepLink
#endif
        }

#if UNITY_STANDALONE || UNITY_EDITOR
        /// <summary>
        /// Check command line arguments for auth callback (desktop).
        /// </summary>
        private void CheckCommandLineArgs()
        {
            string[] args = Environment.GetCommandLineArgs();
            foreach (string arg in args)
            {
                if (arg.Contains("auth-callback") && (arg.Contains("access_token") || arg.Contains("error")))
                {
                    Debug.Log($"[AuthCallbackHandler] Found callback in command line: {arg}");
                    ProcessCallback(arg);
                    return;
                }
            }
        }
#endif

        /// <summary>
        /// Called by JavaScript bridge when OAuth redirect is received (WebGL).
        /// This method name must match what the JavaScript calls via SendMessage.
        /// </summary>
        public void OnAuthCallback(string callbackUrl)
        {
            Debug.Log($"[AuthCallbackHandler] Received callback from JavaScript: {callbackUrl}");
            ProcessCallback(callbackUrl);
        }

        /// <summary>
        /// Process the auth callback URL.
        /// </summary>
        private void ProcessCallback(string callbackUrl)
        {
            if (string.IsNullOrEmpty(callbackUrl))
            {
                Debug.LogWarning("[AuthCallbackHandler] Empty callback URL");
                return;
            }

            _isWaitingForCallback = false;
            OnCallbackReceived?.Invoke(callbackUrl);

            if (_authService != null)
            {
                // Let AuthService handle the callback
                _authService.HandleAuthCallbackAsync(callbackUrl).Forget();
            }
            else
            {
                Debug.LogError("[AuthCallbackHandler] AuthService not initialized");
            }
        }

        // ============================================================
        // DEEP LINK HANDLING
        // ============================================================

#if UNITY_IOS || UNITY_ANDROID
        /// <summary>
        /// Handle deep link on mobile platforms.
        /// Register this with Application.deepLinkActivated in a startup script.
        /// </summary>
        public void HandleDeepLink(string url)
        {
            Debug.Log($"[AuthCallbackHandler] Deep link received: {url}");

            // Check if this is an auth callback
            if (url.Contains("auth-callback") || url.Contains("access_token"))
            {
                ProcessCallback(url);
            }
        }
#endif

        // ============================================================
        // MANUAL CALLBACK (for testing)
        // ============================================================

        /// <summary>
        /// Manually inject a callback URL for testing purposes.
        /// </summary>
        public void InjectCallback(string callbackUrl)
        {
            Debug.Log($"[AuthCallbackHandler] Manual callback injection: {callbackUrl}");
            ProcessCallback(callbackUrl);
        }
    }
}

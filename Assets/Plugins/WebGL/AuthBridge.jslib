// AuthBridge.jslib
// JavaScript bridge for OAuth callback handling in WebGL builds
// Created: January 16, 2026
// Developer: TecVooDoo LLC

mergeInto(LibraryManager.library, {

    // Store for pending auth callback
    _pendingAuthCallback: null,
    _authCallbackGameObject: 'AuthCallbackHandler',

    // Register the auth callback handler
    // Called from Unity on startup
    RegisterAuthCallback: function() {
        var self = this;

        // Check if we already have tokens in the URL hash (page load with OAuth redirect)
        if (window.location.hash && window.location.hash.includes('access_token')) {
            console.log('[AuthBridge] Found auth tokens in URL hash on load');
            self._pendingAuthCallback = window.location.href;

            // Clean up the URL (remove hash)
            if (window.history && window.history.replaceState) {
                window.history.replaceState(null, '', window.location.pathname + window.location.search);
            }
        }

        // Listen for hash changes (in case of SPA-style redirects)
        window.addEventListener('hashchange', function() {
            if (window.location.hash && window.location.hash.includes('access_token')) {
                console.log('[AuthBridge] Auth callback detected via hashchange');
                var callbackUrl = window.location.href;

                // Clean up the URL
                if (window.history && window.history.replaceState) {
                    window.history.replaceState(null, '', window.location.pathname + window.location.search);
                }

                // Try to send to Unity immediately
                try {
                    SendMessage(self._authCallbackGameObject, 'OnAuthCallback', callbackUrl);
                } catch (e) {
                    // Unity not ready, store for later
                    console.log('[AuthBridge] Unity not ready, storing callback');
                    self._pendingAuthCallback = callbackUrl;
                }
            }
        });

        // Also listen for popstate (browser back/forward)
        window.addEventListener('popstate', function() {
            if (window.location.hash && window.location.hash.includes('access_token')) {
                console.log('[AuthBridge] Auth callback detected via popstate');
                self._pendingAuthCallback = window.location.href;
            }
        });

        console.log('[AuthBridge] Auth callback handler registered');
    },

    // Get any pending auth callback
    // Returns empty string if none
    GetPendingAuthCallback: function() {
        var callback = this._pendingAuthCallback || '';
        var bufferSize = lengthBytesUTF8(callback) + 1;
        var buffer = _malloc(bufferSize);
        stringToUTF8(callback, buffer, bufferSize);
        return buffer;
    },

    // Clear the pending callback after it's been processed
    ClearPendingAuthCallback: function() {
        this._pendingAuthCallback = null;
        console.log('[AuthBridge] Pending auth callback cleared');
    },

    // Set the GameObject name that receives auth callbacks
    // Default is 'AuthCallbackHandler'
    SetAuthCallbackGameObject: function(gameObjectNamePtr) {
        this._authCallbackGameObject = UTF8ToString(gameObjectNamePtr);
        console.log('[AuthBridge] Callback target set to: ' + this._authCallbackGameObject);
    },

    // Open URL in same window (for OAuth redirects)
    // This ensures the redirect comes back to the game
    OpenAuthURL: function(urlPtr) {
        var url = UTF8ToString(urlPtr);
        console.log('[AuthBridge] Opening auth URL: ' + url);
        window.location.href = url;
    },

    // Open URL in popup window (alternative approach)
    // Returns window handle ID or 0 if blocked
    OpenAuthPopup: function(urlPtr, widthPtr, heightPtr) {
        var url = UTF8ToString(urlPtr);
        var width = widthPtr || 500;
        var height = heightPtr || 600;

        var left = (window.screen.width - width) / 2;
        var top = (window.screen.height - height) / 2;

        var features = 'width=' + width + ',height=' + height + ',left=' + left + ',top=' + top;
        features += ',menubar=no,toolbar=no,location=yes,status=no,resizable=yes,scrollbars=yes';

        var popup = window.open(url, 'dlyh_auth', features);

        if (!popup || popup.closed || typeof popup.closed === 'undefined') {
            console.log('[AuthBridge] Popup blocked');
            return 0;
        }

        console.log('[AuthBridge] Auth popup opened');

        // Monitor popup for closure or redirect
        var checkInterval = setInterval(function() {
            try {
                if (popup.closed) {
                    clearInterval(checkInterval);
                    console.log('[AuthBridge] Auth popup closed');
                    return;
                }

                // Check if we've been redirected back
                var currentUrl = popup.location.href;
                if (currentUrl && currentUrl.includes('auth-callback')) {
                    console.log('[AuthBridge] Auth callback in popup: ' + currentUrl);
                    clearInterval(checkInterval);
                    popup.close();

                    try {
                        SendMessage(this._authCallbackGameObject, 'OnAuthCallback', currentUrl);
                    } catch (e) {
                        this._pendingAuthCallback = currentUrl;
                    }
                }
            } catch (e) {
                // Cross-origin error - popup is on OAuth provider's domain
                // This is expected, keep checking
            }
        }.bind(this), 500);

        return 1;
    },

    // Get current page URL (for checking redirects)
    GetCurrentURL: function() {
        var url = window.location.href;
        var bufferSize = lengthBytesUTF8(url) + 1;
        var buffer = _malloc(bufferSize);
        stringToUTF8(url, buffer, bufferSize);
        return buffer;
    },

    // Store auth session in localStorage (persists across browser sessions)
    StoreAuthSession: function(keyPtr, valuePtr) {
        var key = UTF8ToString(keyPtr);
        var value = UTF8ToString(valuePtr);
        try {
            localStorage.setItem(key, value);
            console.log('[AuthBridge] Stored auth session: ' + key);
        } catch (e) {
            console.error('[AuthBridge] Failed to store auth session: ' + e);
        }
    },

    // Retrieve auth session from localStorage
    GetStoredAuthSession: function(keyPtr) {
        var key = UTF8ToString(keyPtr);
        var value = '';
        try {
            value = localStorage.getItem(key) || '';
        } catch (e) {
            console.error('[AuthBridge] Failed to retrieve auth session: ' + e);
        }
        var bufferSize = lengthBytesUTF8(value) + 1;
        var buffer = _malloc(bufferSize);
        stringToUTF8(value, buffer, bufferSize);
        return buffer;
    },

    // Remove auth session from localStorage
    RemoveStoredAuthSession: function(keyPtr) {
        var key = UTF8ToString(keyPtr);
        try {
            localStorage.removeItem(key);
            console.log('[AuthBridge] Removed auth session: ' + key);
        } catch (e) {
            console.error('[AuthBridge] Failed to remove auth session: ' + e);
        }
    },

    // Clear URL hash fragment (called from C# after processing OAuth callback)
    // Uses history.replaceState to avoid page reload
    ClearUrlHash: function() {
        if (window.location.hash) {
            if (window.history && window.history.replaceState) {
                window.history.replaceState(null, '', window.location.pathname + window.location.search);
                console.log('[AuthBridge] URL hash cleared via replaceState');
            } else {
                // Fallback for older browsers (will cause a scroll to top)
                window.location.hash = '';
                console.log('[AuthBridge] URL hash cleared via direct assignment');
            }
        }
    }
});

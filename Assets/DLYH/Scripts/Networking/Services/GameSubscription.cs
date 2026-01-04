// GameSubscription.cs
// High-level game session subscription manager
// Created: January 4, 2026
// Developer: TecVooDoo LLC

using System;
using UnityEngine;
using Cysharp.Threading.Tasks;

namespace DLYH.Networking.Services
{
    /// <summary>
    /// Manages realtime subscriptions for a game session.
    /// Provides game-specific events for state changes, opponent actions, etc.
    /// </summary>
    public class GameSubscription : IDisposable
    {
        // ============================================================
        // EVENTS
        // ============================================================

        /// <summary>Fired when the game state is updated by the opponent</summary>
        public event Action<string> OnGameStateUpdated;

        /// <summary>Fired when opponent connects</summary>
        public event Action OnOpponentConnected;

        /// <summary>Fired when opponent disconnects</summary>
        public event Action OnOpponentDisconnected;

        /// <summary>Fired when it becomes our turn</summary>
        public event Action OnTurnReceived;

        /// <summary>Fired when connection to realtime server is lost</summary>
        public event Action OnConnectionLost;

        /// <summary>Fired when reconnected after connection loss</summary>
        public event Action OnReconnected;

        // ============================================================
        // STATE
        // ============================================================

        private readonly RealtimeClient _client;
        private readonly string _sessionId;
        private readonly string _playerId;
        private bool _isSubscribed;
        private bool _isDisposed;
        private int _reconnectAttempts;
        private const int MAX_RECONNECT_ATTEMPTS = 5;

        // ============================================================
        // PROPERTIES
        // ============================================================

        public bool IsConnected => _client?.IsConnected ?? false;
        public bool IsSubscribed => _isSubscribed;
        public string SessionId => _sessionId;

        // ============================================================
        // CONSTRUCTOR
        // ============================================================

        public GameSubscription(SupabaseConfig config, string sessionId, string playerId, string accessToken = null)
        {
            if (config == null) throw new ArgumentNullException(nameof(config));
            if (string.IsNullOrEmpty(sessionId)) throw new ArgumentNullException(nameof(sessionId));
            if (string.IsNullOrEmpty(playerId)) throw new ArgumentNullException(nameof(playerId));

            _sessionId = sessionId;
            _playerId = playerId;
            _client = new RealtimeClient(config, accessToken);

            // Subscribe to client events
            _client.OnConnected += HandleConnected;
            _client.OnDisconnected += HandleDisconnected;
            _client.OnError += HandleError;
            _client.OnMessage += HandleMessage;
        }

        // ============================================================
        // CONNECTION
        // ============================================================

        /// <summary>
        /// Connects to realtime and subscribes to game session updates.
        /// </summary>
        public async UniTask<bool> StartAsync()
        {
            if (_isDisposed)
            {
                Debug.LogError("[GameSubscription] Cannot start - already disposed");
                return false;
            }

            Debug.Log($"[GameSubscription] Starting subscription for session {_sessionId}...");

            // Connect to realtime server
            bool connected = await _client.ConnectAsync();
            if (!connected)
            {
                Debug.LogError("[GameSubscription] Failed to connect to realtime server");
                return false;
            }

            // Subscribe to game session updates
            bool subscribed = await _client.SubscribeToGameSession(_sessionId);
            if (!subscribed)
            {
                Debug.LogError("[GameSubscription] Failed to subscribe to game session");
                await _client.DisconnectAsync();
                return false;
            }

            _isSubscribed = true;
            _reconnectAttempts = 0;
            Debug.Log($"[GameSubscription] Successfully subscribed to session {_sessionId}");

            return true;
        }

        /// <summary>
        /// Stops the subscription and disconnects.
        /// </summary>
        public async UniTask StopAsync()
        {
            if (!_isSubscribed) return;

            _isSubscribed = false;
            await _client.DisconnectAsync();
            Debug.Log("[GameSubscription] Stopped");
        }

        // ============================================================
        // TICK (call from MonoBehaviour Update)
        // ============================================================

        /// <summary>
        /// Must be called each frame to maintain connection.
        /// </summary>
        public void Tick()
        {
            _client?.Tick();
        }

        // ============================================================
        // EVENT HANDLERS
        // ============================================================

        private void HandleConnected()
        {
            Debug.Log("[GameSubscription] Connected to realtime server");

            if (_reconnectAttempts > 0)
            {
                OnReconnected?.Invoke();
            }
        }

        private void HandleDisconnected()
        {
            Debug.Log("[GameSubscription] Disconnected from realtime server");

            if (_isSubscribed && !_isDisposed)
            {
                OnConnectionLost?.Invoke();
                AttemptReconnect().Forget();
            }
        }

        private void HandleError(string error)
        {
            Debug.LogError($"[GameSubscription] Error: {error}");
        }

        private void HandleMessage(string topic, string eventName, string payload)
        {
            Debug.Log($"[GameSubscription] Message - Topic: {topic}, Event: {eventName}");

            if (!topic.Contains(_sessionId)) return;

            try
            {
                if (eventName == "postgres_changes")
                {
                    ProcessGameStateChange(payload);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[GameSubscription] Error processing message: {ex.Message}");
            }
        }

        // ============================================================
        // MESSAGE PROCESSING
        // ============================================================

        private void ProcessGameStateChange(string payload)
        {
            // Extract the relevant fields from the postgres_changes payload
            // Format: {"data":{"record":{"id":..., "state":..., ...}, "old_record":..., "type":"UPDATE"}}

            // Quick extraction of state field
            string state = ExtractNestedValue(payload, "record", "state");
            string currentTurn = ExtractNestedValue(payload, "record", "current_turn_player_id");
            string status = ExtractNestedValue(payload, "record", "status");

            if (state != null)
            {
                Debug.Log($"[GameSubscription] Game state updated, current turn: {currentTurn}");

                OnGameStateUpdated?.Invoke(state);

                // Check if it's now our turn
                if (currentTurn == _playerId)
                {
                    Debug.Log("[GameSubscription] It's our turn!");
                    OnTurnReceived?.Invoke();
                }
            }

            // Check for opponent connection status (could be in a presence channel)
            if (status == "completed" || status == "forfeit")
            {
                Debug.Log($"[GameSubscription] Game ended with status: {status}");
            }
        }

        private string ExtractNestedValue(string json, string parent, string key)
        {
            // Find parent object
            string parentKey = $"\"{parent}\":{{";
            int parentStart = json.IndexOf(parentKey);
            if (parentStart < 0) return null;

            // Find key within parent
            string searchKey = $"\"{key}\":";
            int keyStart = json.IndexOf(searchKey, parentStart);
            if (keyStart < 0) return null;

            keyStart += searchKey.Length;

            // Handle string vs object values
            if (json[keyStart] == '"')
            {
                keyStart++;
                int endQuote = json.IndexOf('"', keyStart);
                if (endQuote < 0) return null;
                return json.Substring(keyStart, endQuote - keyStart);
            }
            else if (json[keyStart] == '{' || json[keyStart] == '[')
            {
                // Extract full object/array
                char open = json[keyStart];
                char close = open == '{' ? '}' : ']';
                int depth = 1;
                int endIndex = keyStart + 1;
                while (endIndex < json.Length && depth > 0)
                {
                    if (json[endIndex] == open) depth++;
                    else if (json[endIndex] == close) depth--;
                    endIndex++;
                }
                return json.Substring(keyStart, endIndex - keyStart);
            }
            else
            {
                // Primitive value
                int endIndex = json.IndexOfAny(new[] { ',', '}', ']' }, keyStart);
                if (endIndex < 0) return null;
                return json.Substring(keyStart, endIndex - keyStart).Trim();
            }
        }

        // ============================================================
        // RECONNECTION
        // ============================================================

        private async UniTask AttemptReconnect()
        {
            while (_isSubscribed && !_isDisposed && _reconnectAttempts < MAX_RECONNECT_ATTEMPTS)
            {
                _reconnectAttempts++;
                int delayMs = Math.Min(1000 * (int)Math.Pow(2, _reconnectAttempts - 1), 30000);

                Debug.Log($"[GameSubscription] Reconnect attempt {_reconnectAttempts}/{MAX_RECONNECT_ATTEMPTS} in {delayMs}ms...");

                await UniTask.Delay(delayMs);

                if (!_isSubscribed || _isDisposed) break;

                bool success = await StartAsync();
                if (success)
                {
                    Debug.Log("[GameSubscription] Reconnected successfully");
                    return;
                }
            }

            if (_reconnectAttempts >= MAX_RECONNECT_ATTEMPTS)
            {
                Debug.LogError("[GameSubscription] Max reconnect attempts reached");
            }
        }

        // ============================================================
        // DISPOSE
        // ============================================================

        public void Dispose()
        {
            if (_isDisposed) return;
            _isDisposed = true;

            _client.OnConnected -= HandleConnected;
            _client.OnDisconnected -= HandleDisconnected;
            _client.OnError -= HandleError;
            _client.OnMessage -= HandleMessage;

            _client?.Dispose();

            Debug.Log("[GameSubscription] Disposed");
        }
    }
}

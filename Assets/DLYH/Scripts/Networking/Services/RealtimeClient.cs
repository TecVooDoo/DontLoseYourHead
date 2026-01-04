// RealtimeClient.cs
// WebSocket client for Supabase Realtime subscriptions
// Created: January 4, 2026
// Developer: TecVooDoo LLC

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using UnityEngine;
using Cysharp.Threading.Tasks;

#if !UNITY_WEBGL || UNITY_EDITOR
using System.Net.WebSockets;
#endif

namespace DLYH.Networking.Services
{
    /// <summary>
    /// WebSocket client for Supabase Realtime.
    /// Handles connection, heartbeat, and channel subscriptions.
    ///
    /// Supabase Realtime uses Phoenix Channels protocol.
    /// </summary>
    public class RealtimeClient : IDisposable
    {
        // ============================================================
        // EVENTS
        // ============================================================

        /// <summary>Fired when connection is established</summary>
        public event Action OnConnected;

        /// <summary>Fired when connection is lost</summary>
        public event Action OnDisconnected;

        /// <summary>Fired when an error occurs</summary>
        public event Action<string> OnError;

        /// <summary>Fired when a message is received (topic, event, payload)</summary>
        public event Action<string, string, string> OnMessage;

        // ============================================================
        // CONFIGURATION
        // ============================================================

        private readonly SupabaseConfig _config;
        private readonly string _accessToken;

        // ============================================================
        // STATE
        // ============================================================

#if !UNITY_WEBGL || UNITY_EDITOR
        private ClientWebSocket _webSocket;
#endif
        private CancellationTokenSource _cts;
        private bool _isConnected;
        private bool _isDisposed;
        private int _messageRef;
        private readonly Dictionary<string, int> _channelRefs = new Dictionary<string, int>();

        // Heartbeat
        private const float HEARTBEAT_INTERVAL = 30f;
        private float _lastHeartbeatTime;
        private bool _heartbeatPending;

        // ============================================================
        // PROPERTIES
        // ============================================================

        public bool IsConnected => _isConnected;

        // ============================================================
        // CONSTRUCTOR
        // ============================================================

        public RealtimeClient(SupabaseConfig config, string accessToken = null)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _accessToken = accessToken;
        }

        // ============================================================
        // CONNECTION
        // ============================================================

        /// <summary>
        /// Connects to the Supabase Realtime server.
        /// </summary>
        public async UniTask<bool> ConnectAsync()
        {
            if (_isDisposed)
            {
                Debug.LogError("[RealtimeClient] Cannot connect - already disposed");
                return false;
            }

            if (_isConnected)
            {
                Debug.LogWarning("[RealtimeClient] Already connected");
                return true;
            }

#if UNITY_WEBGL && !UNITY_EDITOR
            Debug.LogWarning("[RealtimeClient] WebGL requires JavaScript WebSocket bridge - not yet implemented");
            return false;
#else
            try
            {
                _cts = new CancellationTokenSource();
                _webSocket = new ClientWebSocket();

                // Build URL with auth token
                string url = $"{_config.RealtimeUrl}?apikey={_config.AnonKey}&vsn=1.0.0";
                if (!string.IsNullOrEmpty(_accessToken))
                {
                    url += $"&token={_accessToken}";
                }

                Debug.Log($"[RealtimeClient] Connecting to {_config.RealtimeUrl}...");

                await _webSocket.ConnectAsync(new Uri(url), _cts.Token);

                if (_webSocket.State == WebSocketState.Open)
                {
                    _isConnected = true;
                    _lastHeartbeatTime = Time.realtimeSinceStartup;
                    Debug.Log("[RealtimeClient] Connected successfully");
                    OnConnected?.Invoke();

                    // Start receive loop
                    _ = ReceiveLoopAsync();

                    return true;
                }
                else
                {
                    Debug.LogError($"[RealtimeClient] Connection failed, state: {_webSocket.State}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[RealtimeClient] Connection error: {ex.Message}");
                OnError?.Invoke(ex.Message);
                return false;
            }
#endif
        }

        /// <summary>
        /// Disconnects from the Realtime server.
        /// </summary>
        public async UniTask DisconnectAsync()
        {
            if (!_isConnected) return;

#if !UNITY_WEBGL || UNITY_EDITOR
            try
            {
                _cts?.Cancel();

                if (_webSocket != null && _webSocket.State == WebSocketState.Open)
                {
                    await _webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Client disconnect", CancellationToken.None);
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[RealtimeClient] Disconnect error: {ex.Message}");
            }
            finally
            {
                _webSocket?.Dispose();
                _webSocket = null;
                _isConnected = false;
                _channelRefs.Clear();
                OnDisconnected?.Invoke();
            }
#endif
        }

        // ============================================================
        // RECEIVE LOOP
        // ============================================================

#if !UNITY_WEBGL || UNITY_EDITOR
        private async UniTask ReceiveLoopAsync()
        {
            var buffer = new byte[4096];
            var messageBuilder = new StringBuilder();

            try
            {
                while (_isConnected && _webSocket?.State == WebSocketState.Open && !_cts.Token.IsCancellationRequested)
                {
                    var segment = new ArraySegment<byte>(buffer);
                    var result = await _webSocket.ReceiveAsync(segment, _cts.Token);

                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        Debug.Log("[RealtimeClient] Server closed connection");
                        break;
                    }

                    if (result.MessageType == WebSocketMessageType.Text)
                    {
                        messageBuilder.Append(Encoding.UTF8.GetString(buffer, 0, result.Count));

                        if (result.EndOfMessage)
                        {
                            string message = messageBuilder.ToString();
                            messageBuilder.Clear();
                            ProcessMessage(message);
                        }
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // Expected when disconnecting
            }
            catch (Exception ex)
            {
                Debug.LogError($"[RealtimeClient] Receive error: {ex.Message}");
                OnError?.Invoke(ex.Message);
            }
            finally
            {
                if (_isConnected)
                {
                    _isConnected = false;
                    OnDisconnected?.Invoke();
                }
            }
        }
#endif

        // ============================================================
        // MESSAGE PROCESSING
        // ============================================================

        private void ProcessMessage(string rawMessage)
        {
            try
            {
                // Parse Phoenix message format: [join_ref, ref, topic, event, payload]
                // We use simple JSON parsing since Unity's JsonUtility doesn't handle arrays well

                // Quick parse for topic, event, and payload
                string topic = ExtractJsonValue(rawMessage, "topic");
                string eventName = ExtractJsonValue(rawMessage, "event");
                string payload = ExtractJsonObject(rawMessage, "payload");

                if (eventName == "phx_reply")
                {
                    // Handle join/leave replies
                    string status = ExtractJsonValue(payload ?? "", "status");
                    Debug.Log($"[RealtimeClient] Reply for {topic}: {status}");

                    if (_heartbeatPending && topic == "phoenix")
                    {
                        _heartbeatPending = false;
                    }
                }
                else if (eventName == "postgres_changes" || eventName == "broadcast")
                {
                    // Forward to subscribers
                    OnMessage?.Invoke(topic, eventName, payload);
                }
                else if (eventName == "presence_state" || eventName == "presence_diff")
                {
                    OnMessage?.Invoke(topic, eventName, payload);
                }
                else
                {
                    Debug.Log($"[RealtimeClient] Event: {eventName} on {topic}");
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[RealtimeClient] Failed to process message: {ex.Message}");
            }
        }

        // ============================================================
        // CHANNEL SUBSCRIPTIONS
        // ============================================================

        /// <summary>
        /// Subscribes to postgres changes on a table.
        /// </summary>
        /// <param name="table">Table name to watch</param>
        /// <param name="filter">Optional filter (e.g., "id=eq.123")</param>
        /// <param name="eventType">INSERT, UPDATE, DELETE, or * for all</param>
        public async UniTask<bool> SubscribeToTable(string table, string filter = null, string eventType = "*")
        {
            if (!_isConnected)
            {
                Debug.LogError("[RealtimeClient] Cannot subscribe - not connected");
                return false;
            }

            string topic = $"realtime:public:{table}";

            if (_channelRefs.ContainsKey(topic))
            {
                Debug.LogWarning($"[RealtimeClient] Already subscribed to {topic}");
                return true;
            }

            int joinRef = ++_messageRef;
            int msgRef = ++_messageRef;
            _channelRefs[topic] = joinRef;

            // Build postgres_changes config
            string filterPart = string.IsNullOrEmpty(filter) ? "" : $",\"filter\":\"{filter}\"";
            string payload = $"{{\"config\":{{\"postgres_changes\":[{{\"event\":\"{eventType}\",\"schema\":\"public\",\"table\":\"{table}\"{filterPart}}}]}}}}";

            string message = $"{{\"topic\":\"{topic}\",\"event\":\"phx_join\",\"payload\":{payload},\"ref\":\"{msgRef}\",\"join_ref\":\"{joinRef}\"}}";

            return await SendAsync(message);
        }

        /// <summary>
        /// Subscribes to a specific game session for state changes.
        /// </summary>
        public async UniTask<bool> SubscribeToGameSession(string sessionId)
        {
            return await SubscribeToTable("game_sessions", $"id=eq.{sessionId}", "UPDATE");
        }

        /// <summary>
        /// Unsubscribes from a channel.
        /// </summary>
        public async UniTask<bool> Unsubscribe(string topic)
        {
            if (!_channelRefs.TryGetValue(topic, out int joinRef))
            {
                return true; // Not subscribed
            }

            int msgRef = ++_messageRef;
            string message = $"{{\"topic\":\"{topic}\",\"event\":\"phx_leave\",\"payload\":{{}},\"ref\":\"{msgRef}\",\"join_ref\":\"{joinRef}\"}}";

            _channelRefs.Remove(topic);
            return await SendAsync(message);
        }

        // ============================================================
        // HEARTBEAT
        // ============================================================

        /// <summary>
        /// Call this from Update() to maintain connection.
        /// </summary>
        public void Tick()
        {
            if (!_isConnected) return;

            float now = Time.realtimeSinceStartup;
            if (now - _lastHeartbeatTime >= HEARTBEAT_INTERVAL && !_heartbeatPending)
            {
                SendHeartbeat().Forget();
                _lastHeartbeatTime = now;
            }
        }

        private async UniTask SendHeartbeat()
        {
            int msgRef = ++_messageRef;
            string message = $"{{\"topic\":\"phoenix\",\"event\":\"heartbeat\",\"payload\":{{}},\"ref\":\"{msgRef}\"}}";
            _heartbeatPending = true;
            await SendAsync(message);
        }

        // ============================================================
        // SEND
        // ============================================================

        private async UniTask<bool> SendAsync(string message)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            return false;
#else
            if (_webSocket?.State != WebSocketState.Open)
            {
                Debug.LogError("[RealtimeClient] Cannot send - not connected");
                return false;
            }

            try
            {
                var bytes = Encoding.UTF8.GetBytes(message);
                await _webSocket.SendAsync(
                    new ArraySegment<byte>(bytes),
                    WebSocketMessageType.Text,
                    true,
                    _cts.Token
                );
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[RealtimeClient] Send error: {ex.Message}");
                OnError?.Invoke(ex.Message);
                return false;
            }
#endif
        }

        // ============================================================
        // JSON HELPERS (simple parsing for Phoenix format)
        // ============================================================

        private string ExtractJsonValue(string json, string key)
        {
            string searchKey = $"\"{key}\":\"";
            int startIndex = json.IndexOf(searchKey);
            if (startIndex < 0)
            {
                // Try without quotes (for non-string values)
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

        private string ExtractJsonObject(string json, string key)
        {
            string searchKey = $"\"{key}\":{{";
            int startIndex = json.IndexOf(searchKey);
            if (startIndex < 0)
            {
                // Try array format
                searchKey = $"\"{key}\":[";
                startIndex = json.IndexOf(searchKey);
                if (startIndex < 0) return null;
            }

            startIndex += searchKey.Length - 1; // Include opening brace/bracket
            int depth = 1;
            int endIndex = startIndex + 1;
            char openChar = json[startIndex];
            char closeChar = openChar == '{' ? '}' : ']';

            while (endIndex < json.Length && depth > 0)
            {
                if (json[endIndex] == openChar) depth++;
                else if (json[endIndex] == closeChar) depth--;
                endIndex++;
            }

            if (depth != 0) return null;
            return json.Substring(startIndex, endIndex - startIndex);
        }

        // ============================================================
        // DISPOSE
        // ============================================================

        public void Dispose()
        {
            if (_isDisposed) return;
            _isDisposed = true;

            DisconnectAsync().Forget();
        }
    }
}

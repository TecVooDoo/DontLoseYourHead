// PlaytestTelemetry.cs
// Sends gameplay telemetry to Cloudflare Worker for playtest analytics
// Created: December 15, 2025

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

namespace DLYH.Telemetry
{
    /// <summary>
    /// Singleton manager for sending playtest telemetry events.
    /// Events are sent to a Cloudflare Worker endpoint for storage and analysis.
    /// </summary>
    public class PlaytestTelemetry : MonoBehaviour
    {
        #region Constants

        private const string TELEMETRY_ENDPOINT = "https://dlyh-telemetry.runeduvall.workers.dev/event";
        private const string SESSION_ID_KEY = "DLYH_SessionId";

        #endregion

        #region Singleton

        private static PlaytestTelemetry _instance;

        public static PlaytestTelemetry Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindFirstObjectByType<PlaytestTelemetry>();

                    if (_instance == null)
                    {
                        GameObject go = new GameObject("PlaytestTelemetry");
                        _instance = go.AddComponent<PlaytestTelemetry>();
                        DontDestroyOnLoad(go);
                    }
                }
                return _instance;
            }
        }

        #endregion

        #region Private Fields

        private string _sessionId;
        private bool _isEnabled = true;
        private Queue<TelemetryEvent> _eventQueue = new Queue<TelemetryEvent>();
        private bool _isSending = false;

        #endregion

        #region Properties

        /// <summary>
        /// Enable or disable telemetry sending
        /// </summary>
        public bool IsEnabled
        {
            get { return _isEnabled; }
            set { _isEnabled = value; }
        }

        /// <summary>
        /// Current session ID
        /// </summary>
        public string SessionId
        {
            get { return _sessionId; }
        }

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);

            InitializeSession();

            // Subscribe to Unity log messages to capture errors
            Application.logMessageReceived += OnLogMessageReceived;
        }

        private void OnDestroy()
        {
            Application.logMessageReceived -= OnLogMessageReceived;
        }

        private void OnApplicationQuit()
        {
            // Send session end event
            LogEvent("session_end", null);
        }

        /// <summary>
        /// Captures Unity errors and exceptions for telemetry
        /// </summary>
        private void OnLogMessageReceived(string condition, string stackTrace, LogType type)
        {
            // Only capture errors and exceptions
            if (type != LogType.Error && type != LogType.Exception)
            {
                return;
            }

            Dictionary<string, object> errorData = new Dictionary<string, object>
            {
                { "error_type", type.ToString() },
                { "message", condition.Length > 500 ? condition.Substring(0, 500) : condition },
                { "stack_trace", stackTrace.Length > 1000 ? stackTrace.Substring(0, 1000) : stackTrace }
            };

            LogEvent("error", errorData);
        }

        #endregion

        #region Initialization

        private void InitializeSession()
        {
            // Generate a unique session ID
            _sessionId = Guid.NewGuid().ToString();

            Debug.Log(string.Format("[Telemetry] Session started: {0}", _sessionId));

            // Log session start with platform info
            Dictionary<string, object> sessionData = new Dictionary<string, object>
            {
                { "platform", Application.platform.ToString() },
                { "version", Application.version },
                { "unity_version", Application.unityVersion },
                { "screen_width", Screen.width },
                { "screen_height", Screen.height }
            };

            LogEvent("session_start", sessionData);
        }

        #endregion

        #region Public Methods - Game Events

        /// <summary>
        /// Log when a player completes setup
        /// </summary>
        public void LogSetupComplete(string playerName, int gridSize, int wordCount, string difficulty)
        {
            Dictionary<string, object> data = new Dictionary<string, object>
            {
                { "player_name", playerName },
                { "grid_size", gridSize },
                { "word_count", wordCount },
                { "difficulty", difficulty }
            };

            LogEvent("setup_complete", data);
        }

        /// <summary>
        /// Log when a game starts
        /// </summary>
        public void LogGameStart(int playerGridSize, int playerWordCount, string playerDifficulty,
                                  int opponentGridSize, int opponentWordCount, string opponentDifficulty)
        {
            Dictionary<string, object> data = new Dictionary<string, object>
            {
                { "player_grid_size", playerGridSize },
                { "player_word_count", playerWordCount },
                { "player_difficulty", playerDifficulty },
                { "opponent_grid_size", opponentGridSize },
                { "opponent_word_count", opponentWordCount },
                { "opponent_difficulty", opponentDifficulty }
            };

            LogEvent("game_start", data);
        }

        /// <summary>
        /// Log when a game ends
        /// </summary>
        public void LogGameEnd(bool playerWon, int playerMisses, int playerMissLimit,
                               int opponentMisses, int opponentMissLimit, int totalTurns)
        {
            Dictionary<string, object> data = new Dictionary<string, object>
            {
                { "player_won", playerWon },
                { "player_misses", playerMisses },
                { "player_miss_limit", playerMissLimit },
                { "opponent_misses", opponentMisses },
                { "opponent_miss_limit", opponentMissLimit },
                { "total_turns", totalTurns }
            };

            LogEvent("game_end", data);
        }

        /// <summary>
        /// Log a player guess
        /// </summary>
        public void LogGuess(string guessType, bool isHit, string guessValue)
        {
            Dictionary<string, object> data = new Dictionary<string, object>
            {
                { "guess_type", guessType },
                { "is_hit", isHit },
                { "guess_value", guessValue }
            };

            LogEvent("player_guess", data);
        }

        /// <summary>
        /// Log when player abandons a game (quits mid-game)
        /// </summary>
        public void LogGameAbandon(string phase, int turnNumber)
        {
            Dictionary<string, object> data = new Dictionary<string, object>
            {
                { "phase", phase },
                { "turn_number", turnNumber }
            };

            LogEvent("game_abandon", data);
        }

        /// <summary>
        /// Log a custom event
        /// </summary>
        public void LogEvent(string eventType, Dictionary<string, object> eventData)
        {
            if (!_isEnabled)
            {
                return;
            }

            TelemetryEvent evt = new TelemetryEvent
            {
                session_id = _sessionId,
                event_type = eventType,
                event_data = eventData ?? new Dictionary<string, object>()
            };

            _eventQueue.Enqueue(evt);

            if (!_isSending)
            {
                StartCoroutine(ProcessEventQueue());
            }
        }

        #endregion

        #region Static Convenience Methods

        public static void SetupComplete(string playerName, int gridSize, int wordCount, string difficulty)
        {
            if (Instance != null)
            {
                Instance.LogSetupComplete(playerName, gridSize, wordCount, difficulty);
            }
        }

        public static void GameStart(int playerGridSize, int playerWordCount, string playerDifficulty,
                                      int opponentGridSize, int opponentWordCount, string opponentDifficulty)
        {
            if (Instance != null)
            {
                Instance.LogGameStart(playerGridSize, playerWordCount, playerDifficulty,
                                       opponentGridSize, opponentWordCount, opponentDifficulty);
            }
        }

        public static void GameEnd(bool playerWon, int playerMisses, int playerMissLimit,
                                   int opponentMisses, int opponentMissLimit, int totalTurns)
        {
            if (Instance != null)
            {
                Instance.LogGameEnd(playerWon, playerMisses, playerMissLimit,
                                    opponentMisses, opponentMissLimit, totalTurns);
            }
        }

        public static void Guess(string guessType, bool isHit, string guessValue)
        {
            if (Instance != null)
            {
                Instance.LogGuess(guessType, isHit, guessValue);
            }
        }

        public static void GameAbandon(string phase, int turnNumber)
        {
            if (Instance != null)
            {
                Instance.LogGameAbandon(phase, turnNumber);
            }
        }

        public static void Event(string eventType, Dictionary<string, object> eventData = null)
        {
            if (Instance != null)
            {
                Instance.LogEvent(eventType, eventData);
            }
        }

        #endregion

        #region Network

        private IEnumerator ProcessEventQueue()
        {
            _isSending = true;

            while (_eventQueue.Count > 0)
            {
                TelemetryEvent evt = _eventQueue.Dequeue();
                yield return StartCoroutine(SendEvent(evt));
            }

            _isSending = false;
        }

        private IEnumerator SendEvent(TelemetryEvent evt)
        {
            string json = JsonUtility.ToJson(evt);

            // JsonUtility doesn't handle Dictionary well, so we build JSON manually
            json = BuildEventJson(evt);

            using (UnityWebRequest request = new UnityWebRequest(TELEMETRY_ENDPOINT, "POST"))
            {
                byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(json);
                request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");

                yield return request.SendWebRequest();

                if (request.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogWarning(string.Format("[Telemetry] Failed to send event: {0}", request.error));
                }
                else
                {
                    Debug.Log(string.Format("[Telemetry] Event sent: {0}", evt.event_type));
                }
            }
        }

        private string BuildEventJson(TelemetryEvent evt)
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            sb.Append("{");
            sb.AppendFormat("\"session_id\":\"{0}\",", EscapeJson(evt.session_id));
            sb.AppendFormat("\"event_type\":\"{0}\",", EscapeJson(evt.event_type));
            sb.Append("\"event_data\":{");

            bool first = true;
            foreach (var kvp in evt.event_data)
            {
                if (!first) sb.Append(",");
                first = false;

                sb.AppendFormat("\"{0}\":", EscapeJson(kvp.Key));

                if (kvp.Value == null)
                {
                    sb.Append("null");
                }
                else if (kvp.Value is bool)
                {
                    sb.Append((bool)kvp.Value ? "true" : "false");
                }
                else if (kvp.Value is int || kvp.Value is float || kvp.Value is double)
                {
                    sb.Append(kvp.Value.ToString());
                }
                else
                {
                    sb.AppendFormat("\"{0}\"", EscapeJson(kvp.Value.ToString()));
                }
            }

            sb.Append("}}");
            return sb.ToString();
        }

        private string EscapeJson(string str)
        {
            if (string.IsNullOrEmpty(str)) return "";
            return str.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n").Replace("\r", "\\r");
        }

        #endregion

        #region Data Classes

        [Serializable]
        private class TelemetryEvent
        {
            public string session_id;
            public string event_type;
            public Dictionary<string, object> event_data;
        }

        #endregion
    }
}

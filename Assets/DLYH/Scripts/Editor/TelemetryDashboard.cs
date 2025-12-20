// TelemetryDashboard.cs
// Editor window for viewing DLYH telemetry data from Cloudflare Worker
// Created: December 19, 2025

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;

namespace DLYH.Editor
{
    public class TelemetryDashboard : EditorWindow
    {
        #region Constants

        private const string BASE_URL = "https://dlyh-telemetry.runeduvall.workers.dev";

        #endregion

        #region Private Fields

        // Data
        private List<EventSummary> _summary = new List<EventSummary>();
        private List<TelemetryEvent> _recentEvents = new List<TelemetryEvent>();
        private List<FeedbackEntry> _feedback = new List<FeedbackEntry>();

        // Computed Stats
        private GameStats _gameStats = new GameStats();

        // State
        private int _selectedTab = 0;
        private readonly string[] _tabNames = { "Summary", "Game Stats", "Recent Events", "Feedback" };
        private Vector2 _scrollPosition;
        private bool _isLoading = false;
        private string _lastError = null;
        private DateTime _lastRefresh = DateTime.MinValue;

        // Requests
        private UnityWebRequest _activeRequest;

        #endregion

        #region Menu Item

        [MenuItem("DLYH/Telemetry Dashboard")]
        public static void ShowWindow()
        {
            var window = GetWindow<TelemetryDashboard>("Telemetry Dashboard");
            window.minSize = new Vector2(450, 350);
            window.Show();
        }

        #endregion

        #region Unity Lifecycle

        private void OnEnable()
        {
            EditorApplication.update += OnEditorUpdate;
            RefreshAllData();
        }

        private void OnDisable()
        {
            EditorApplication.update -= OnEditorUpdate;

            if (_activeRequest != null)
            {
                _activeRequest.Abort();
                _activeRequest.Dispose();
                _activeRequest = null;
            }
        }

        private void OnEditorUpdate()
        {
            if (_activeRequest != null && _activeRequest.isDone)
            {
                var request = _activeRequest;
                _activeRequest = null;
                _isLoading = false; // Set false BEFORE callback so chained requests can start

                ProcessRequestResult(request);
                request.Dispose();
                Repaint();
            }
        }

        #endregion

        #region GUI

        private void OnGUI()
        {
            DrawToolbar();

            EditorGUILayout.Space(5);

            _selectedTab = GUILayout.Toolbar(_selectedTab, _tabNames);

            EditorGUILayout.Space(5);

            if (!string.IsNullOrEmpty(_lastError))
            {
                EditorGUILayout.HelpBox(_lastError, MessageType.Error);
            }

            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

            switch (_selectedTab)
            {
                case 0:
                    DrawSummaryTab();
                    break;
                case 1:
                    DrawGameStatsTab();
                    break;
                case 2:
                    DrawEventsTab();
                    break;
                case 3:
                    DrawFeedbackTab();
                    break;
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            if (_isLoading)
            {
                GUILayout.Label("Loading...", EditorStyles.toolbarButton);
            }
            else
            {
                if (GUILayout.Button("Refresh", EditorStyles.toolbarButton, GUILayout.Width(60)))
                {
                    RefreshAllData();
                }
            }

            // Export button
            bool hasData = _summary.Count > 0 || _recentEvents.Count > 0 || _feedback.Count > 0;
            EditorGUI.BeginDisabledGroup(!hasData || _isLoading);
            if (GUILayout.Button("Export CSV", EditorStyles.toolbarButton, GUILayout.Width(75)))
            {
                ExportToCSV();
            }
            EditorGUI.EndDisabledGroup();

            GUILayout.FlexibleSpace();

            if (_lastRefresh != DateTime.MinValue)
            {
                GUILayout.Label($"Last updated: {_lastRefresh:HH:mm:ss}", EditorStyles.miniLabel);
            }

            EditorGUILayout.EndHorizontal();
        }

        private void DrawSummaryTab()
        {
            if (_summary.Count == 0)
            {
                EditorGUILayout.HelpBox("No data available. Click Refresh to load.", MessageType.Info);
                return;
            }

            EditorGUILayout.LabelField("Event Type Breakdown", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);

            // Calculate total
            int total = 0;
            foreach (var item in _summary)
            {
                total += item.count;
            }

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField($"Total Events: {total:N0}", EditorStyles.boldLabel);
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(10);

            // Draw each event type with a bar
            foreach (var item in _summary)
            {
                DrawEventBar(item.event_type, item.count, total);
            }
        }

        private void DrawGameStatsTab()
        {
            if (_gameStats.TotalGamesStarted == 0)
            {
                EditorGUILayout.HelpBox("No game data available. Click Refresh to load.", MessageType.Info);
                return;
            }

            // Overview section
            EditorGUILayout.LabelField("Game Overview", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            DrawStatRow("Total Sessions", _gameStats.TotalSessions.ToString());
            DrawStatRow("Games Started", _gameStats.TotalGamesStarted.ToString());
            DrawStatRow("Games Completed", _gameStats.TotalGamesCompleted.ToString());
            DrawStatRow("Games Abandoned", _gameStats.TotalGamesAbandoned.ToString());

            EditorGUILayout.Space(5);

            // Completion rate bar
            float completionRate = _gameStats.TotalGamesStarted > 0
                ? (float)_gameStats.TotalGamesCompleted / _gameStats.TotalGamesStarted
                : 0;
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Completion Rate", GUILayout.Width(120));
            Rect completionRect = EditorGUILayout.GetControlRect(GUILayout.Height(18));
            EditorGUI.ProgressBar(completionRect, completionRate, $"{completionRate:P1}");
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(10);

            // Win/Loss section
            EditorGUILayout.LabelField("Win/Loss Stats", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            DrawStatRow("Player Wins", _gameStats.PlayerWins.ToString());
            DrawStatRow("Player Losses", _gameStats.PlayerLosses.ToString());

            float winRate = _gameStats.TotalGamesCompleted > 0
                ? (float)_gameStats.PlayerWins / _gameStats.TotalGamesCompleted
                : 0;
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Win Rate", GUILayout.Width(120));
            Rect winRect = EditorGUILayout.GetControlRect(GUILayout.Height(18));
            EditorGUI.ProgressBar(winRect, winRate, $"{winRate:P1}");
            EditorGUILayout.EndHorizontal();

            if (_gameStats.TotalGamesCompleted > 0)
            {
                EditorGUILayout.Space(5);
                DrawStatRow("Avg Turns/Game", $"{_gameStats.AverageTurns:F1}");
            }

            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(10);

            // Player Names section
            if (_gameStats.PlayerNames.Count > 0)
            {
                EditorGUILayout.LabelField("Players", EditorStyles.boldLabel);
                EditorGUILayout.Space(5);

                EditorGUILayout.BeginVertical(EditorStyles.helpBox);

                // Sort by game count descending
                var sortedPlayers = new List<KeyValuePair<string, int>>(_gameStats.PlayerNames);
                sortedPlayers.Sort((a, b) => b.Value.CompareTo(a.Value));

                int shown = 0;
                foreach (var player in sortedPlayers)
                {
                    if (shown >= 10) break;
                    DrawStatRow(player.Key, $"{player.Value} games");
                    shown++;
                }

                if (sortedPlayers.Count > 10)
                {
                    EditorGUILayout.LabelField($"... and {sortedPlayers.Count - 10} more", EditorStyles.miniLabel);
                }

                EditorGUILayout.EndVertical();
            }

            EditorGUILayout.Space(10);

            // Difficulty breakdown
            if (_gameStats.DifficultyBreakdown.Count > 0)
            {
                EditorGUILayout.LabelField("Difficulty Distribution", EditorStyles.boldLabel);
                EditorGUILayout.Space(5);

                int totalDiff = 0;
                foreach (var d in _gameStats.DifficultyBreakdown.Values)
                    totalDiff += d;

                foreach (var kvp in _gameStats.DifficultyBreakdown)
                {
                    DrawEventBar(kvp.Key, kvp.Value, totalDiff);
                }
            }
        }

        private void DrawStatRow(string label, string value)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(label, GUILayout.Width(120));
            EditorGUILayout.LabelField(value, EditorStyles.boldLabel);
            EditorGUILayout.EndHorizontal();
        }

        private void DrawEventBar(string eventType, int count, int total)
        {
            EditorGUILayout.BeginHorizontal();

            // Event type label
            EditorGUILayout.LabelField(FormatEventType(eventType), GUILayout.Width(120));

            // Progress bar
            float ratio = total > 0 ? (float)count / total : 0;
            Rect rect = EditorGUILayout.GetControlRect(GUILayout.Height(18));
            EditorGUI.ProgressBar(rect, ratio, $"{count:N0} ({ratio:P1})");

            EditorGUILayout.EndHorizontal();
        }

        private void DrawEventsTab()
        {
            if (_recentEvents.Count == 0)
            {
                EditorGUILayout.HelpBox("No recent events. Click Refresh to load.", MessageType.Info);
                return;
            }

            EditorGUILayout.LabelField($"Recent Events (Last {_recentEvents.Count})", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);

            foreach (var evt in _recentEvents)
            {
                DrawEventEntry(evt);
            }
        }

        private void DrawEventEntry(TelemetryEvent evt)
        {
            Color bgColor = GetEventColor(evt.event_type);

            var style = new GUIStyle(EditorStyles.helpBox);

            EditorGUILayout.BeginVertical(style);

            EditorGUILayout.BeginHorizontal();

            // Colored label for event type
            var labelStyle = new GUIStyle(EditorStyles.boldLabel);
            labelStyle.normal.textColor = bgColor;
            EditorGUILayout.LabelField(FormatEventType(evt.event_type), labelStyle, GUILayout.Width(120));

            // Timestamp
            EditorGUILayout.LabelField(FormatTimestamp(evt.timestamp), EditorStyles.miniLabel);

            EditorGUILayout.EndHorizontal();

            // Session ID (truncated)
            EditorGUILayout.LabelField($"Session: {TruncateSessionId(evt.session_id)}", EditorStyles.miniLabel);

            // Event data preview - format nicely for specific types
            if (!string.IsNullOrEmpty(evt.event_data))
            {
                string displayData = FormatEventData(evt.event_type, evt.event_data);
                EditorGUILayout.LabelField(displayData, EditorStyles.wordWrappedMiniLabel);
            }

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(2);
        }

        private void DrawFeedbackTab()
        {
            if (_feedback.Count == 0)
            {
                EditorGUILayout.HelpBox("No feedback received yet. Click Refresh to load.", MessageType.Info);
                return;
            }

            EditorGUILayout.LabelField($"Player Feedback ({_feedback.Count} entries)", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);

            foreach (var entry in _feedback)
            {
                DrawFeedbackEntry(entry);
            }
        }

        private void DrawFeedbackEntry(FeedbackEntry entry)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            EditorGUILayout.BeginHorizontal();

            // Win/loss indicator
            string resultText = entry.PlayerWon ? "WIN" : "LOSS";
            Color resultColor = entry.PlayerWon ? new Color(0.2f, 0.8f, 0.2f) : new Color(0.9f, 0.3f, 0.3f);
            var resultStyle = new GUIStyle(EditorStyles.boldLabel);
            resultStyle.normal.textColor = resultColor;
            EditorGUILayout.LabelField(resultText, resultStyle, GUILayout.Width(50));

            // Timestamp
            EditorGUILayout.LabelField(FormatTimestamp(entry.timestamp), EditorStyles.miniLabel);

            EditorGUILayout.EndHorizontal();

            // Feedback text
            if (!string.IsNullOrEmpty(entry.comment))
            {
                EditorGUILayout.LabelField(entry.comment, EditorStyles.wordWrappedLabel);
            }
            else
            {
                EditorGUILayout.LabelField("(No comment)", EditorStyles.miniLabel);
            }

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(2);
        }

        #endregion

        #region Data Fetching

        private void RefreshAllData()
        {
            _lastError = null;
            FetchData("/summary", OnSummaryReceived);
        }

        private void FetchData(string endpoint, Action<string> callback)
        {
            if (_isLoading) return;

            _isLoading = true;
            _pendingCallback = callback;
            _pendingEndpoint = endpoint;

            _activeRequest = UnityWebRequest.Get(BASE_URL + endpoint);
            _activeRequest.SendWebRequest();
        }

        private Action<string> _pendingCallback;
        private string _pendingEndpoint;

        private void ProcessRequestResult(UnityWebRequest request)
        {
            if (request.result != UnityWebRequest.Result.Success)
            {
                _lastError = $"Failed to fetch {_pendingEndpoint}: {request.error}";
                return;
            }

            string json = request.downloadHandler.text;
            _pendingCallback?.Invoke(json);
        }

        private void OnSummaryReceived(string json)
        {
            _summary.Clear();

            try
            {
                var wrapper = JsonUtility.FromJson<EventSummaryList>("{\"items\":" + json + "}");
                if (wrapper.items != null)
                {
                    _summary.AddRange(wrapper.items);
                }
            }
            catch (Exception e)
            {
                _lastError = $"Failed to parse summary: {e.Message}";
                return;
            }

            _lastRefresh = DateTime.Now;

            // Extract counts for game stats from summary
            _gameStats.Reset();
            foreach (var item in _summary)
            {
                switch (item.event_type)
                {
                    case "session_start":
                        _gameStats.TotalSessions = item.count;
                        break;
                    case "game_start":
                        _gameStats.TotalGamesStarted = item.count;
                        break;
                    case "game_end":
                        _gameStats.TotalGamesCompleted = item.count;
                        break;
                    case "game_abandon":
                        _gameStats.TotalGamesAbandoned = item.count;
                        break;
                }
            }

            // Chain to next request
            FetchData("/events", OnEventsReceived);
        }

        private void OnEventsReceived(string json)
        {
            _recentEvents.Clear();

            try
            {
                var wrapper = JsonUtility.FromJson<TelemetryEventList>("{\"items\":" + json + "}");
                if (wrapper.items != null)
                {
                    _recentEvents.AddRange(wrapper.items);

                    // Parse event data for stats
                    ParseEventsForStats();
                }
            }
            catch (Exception e)
            {
                _lastError = $"Failed to parse events: {e.Message}";
                return;
            }

            // Chain to next request
            FetchData("/feedback", OnFeedbackReceived);
        }

        private void ParseEventsForStats()
        {
            int totalTurns = 0;
            int gameEndCount = 0;

            foreach (var evt in _recentEvents)
            {
                if (string.IsNullOrEmpty(evt.event_data)) continue;

                switch (evt.event_type)
                {
                    case "game_start":
                        // Extract player name
                        string playerName = ExtractJsonValue(evt.event_data, "player_name");
                        if (!string.IsNullOrEmpty(playerName) && playerName != "Unknown")
                        {
                            if (!_gameStats.PlayerNames.ContainsKey(playerName))
                                _gameStats.PlayerNames[playerName] = 0;
                            _gameStats.PlayerNames[playerName]++;
                        }

                        // Extract difficulty
                        string difficulty = ExtractJsonValue(evt.event_data, "player_difficulty");
                        if (!string.IsNullOrEmpty(difficulty))
                        {
                            if (!_gameStats.DifficultyBreakdown.ContainsKey(difficulty))
                                _gameStats.DifficultyBreakdown[difficulty] = 0;
                            _gameStats.DifficultyBreakdown[difficulty]++;
                        }
                        break;

                    case "game_end":
                        // Extract win/loss
                        string playerWon = ExtractJsonValue(evt.event_data, "player_won");
                        if (playerWon == "true")
                            _gameStats.PlayerWins++;
                        else if (playerWon == "false")
                            _gameStats.PlayerLosses++;

                        // Extract turns
                        string turns = ExtractJsonValue(evt.event_data, "total_turns");
                        if (int.TryParse(turns, out int turnCount))
                        {
                            totalTurns += turnCount;
                            gameEndCount++;
                        }
                        break;
                }
            }

            if (gameEndCount > 0)
            {
                _gameStats.AverageTurns = (float)totalTurns / gameEndCount;
            }
        }

        private string ExtractJsonValue(string json, string key)
        {
            // Simple regex to extract JSON values - works for basic cases
            var match = Regex.Match(json, $"\"{key}\"\\s*:\\s*\"?([^,\"}}]+)\"?");
            if (match.Success)
            {
                return match.Groups[1].Value.Trim();
            }
            return null;
        }

        private void OnFeedbackReceived(string json)
        {
            _feedback.Clear();

            try
            {
                // Debug: log raw response
                Debug.Log($"[TelemetryDashboard] Feedback raw JSON: {json}");

                // JsonUtility can't handle int->bool, so we parse manually
                // Match each feedback object in the array
                var matches = Regex.Matches(json, @"\{[^{}]+\}");
                foreach (Match match in matches)
                {
                    string obj = match.Value;
                    var entry = new FeedbackEntry();

                    // Extract fields manually
                    var idMatch = Regex.Match(obj, @"""id""\s*:\s*(\d+)");
                    if (idMatch.Success) entry.id = int.Parse(idMatch.Groups[1].Value);

                    var tsMatch = Regex.Match(obj, @"""timestamp""\s*:\s*""([^""]+)""");
                    if (tsMatch.Success) entry.timestamp = tsMatch.Groups[1].Value;

                    var sessionMatch = Regex.Match(obj, @"""session_id""\s*:\s*""([^""]+)""");
                    if (sessionMatch.Success) entry.session_id = sessionMatch.Groups[1].Value;

                    var commentMatch = Regex.Match(obj, @"""comment""\s*:\s*""([^""]+)""");
                    if (commentMatch.Success) entry.comment = commentMatch.Groups[1].Value;

                    var wonMatch = Regex.Match(obj, @"""player_won""\s*:\s*(\d+)");
                    if (wonMatch.Success) entry.player_won = int.Parse(wonMatch.Groups[1].Value);

                    _feedback.Add(entry);
                }

                Debug.Log($"[TelemetryDashboard] Parsed {_feedback.Count} feedback entries");
            }
            catch (Exception e)
            {
                _lastError = $"Failed to parse feedback: {e.Message}";
                Debug.LogError($"[TelemetryDashboard] Feedback parse error: {e}");
            }
        }

        #endregion

        #region Helpers

        private string FormatEventType(string eventType)
        {
            if (string.IsNullOrEmpty(eventType)) return "Unknown";

            // Convert snake_case to Title Case
            string[] parts = eventType.Split('_');
            for (int i = 0; i < parts.Length; i++)
            {
                if (parts[i].Length > 0)
                {
                    parts[i] = char.ToUpper(parts[i][0]) + parts[i].Substring(1);
                }
            }
            return string.Join(" ", parts);
        }

        private string FormatEventData(string eventType, string eventData)
        {
            if (string.IsNullOrEmpty(eventData)) return "";

            switch (eventType)
            {
                case "game_start":
                    string name = ExtractJsonValue(eventData, "player_name") ?? "?";
                    string grid = ExtractJsonValue(eventData, "player_grid_size") ?? "?";
                    string words = ExtractJsonValue(eventData, "player_word_count") ?? "?";
                    string diff = ExtractJsonValue(eventData, "player_difficulty") ?? "?";
                    return $"{name} - {grid}x{grid} grid, {words} words, {diff}";

                case "game_end":
                    string won = ExtractJsonValue(eventData, "player_won");
                    string result = won == "true" ? "WIN" : "LOSS";
                    string turns = ExtractJsonValue(eventData, "total_turns") ?? "?";
                    string misses = ExtractJsonValue(eventData, "player_misses") ?? "?";
                    return $"{result} - {turns} turns, {misses} misses";

                case "game_abandon":
                    string phase = ExtractJsonValue(eventData, "phase") ?? "?";
                    string turn = ExtractJsonValue(eventData, "turn_number") ?? "?";
                    return $"Quit at turn {turn} ({phase})";

                case "error":
                    string msg = ExtractJsonValue(eventData, "message") ?? eventData;
                    return TruncateData(msg);

                default:
                    return TruncateData(eventData);
            }
        }

        private string FormatTimestamp(string timestamp)
        {
            if (DateTime.TryParse(timestamp, out DateTime dt))
            {
                TimeSpan ago = DateTime.UtcNow - dt;

                if (ago.TotalMinutes < 1) return "Just now";
                if (ago.TotalMinutes < 60) return $"{(int)ago.TotalMinutes}m ago";
                if (ago.TotalHours < 24) return $"{(int)ago.TotalHours}h ago";
                if (ago.TotalDays < 7) return $"{(int)ago.TotalDays}d ago";

                return dt.ToString("MMM d");
            }
            return timestamp ?? "";
        }

        private string TruncateSessionId(string sessionId)
        {
            if (string.IsNullOrEmpty(sessionId)) return "unknown";
            if (sessionId.Length <= 8) return sessionId;
            return sessionId.Substring(0, 8) + "...";
        }

        private string TruncateData(string data)
        {
            if (string.IsNullOrEmpty(data)) return "";
            if (data.Length <= 100) return data;
            return data.Substring(0, 100) + "...";
        }

        private Color GetEventColor(string eventType)
        {
            switch (eventType)
            {
                case "session_start":
                    return new Color(0.3f, 0.7f, 1.0f); // Blue
                case "session_end":
                    return new Color(0.5f, 0.5f, 0.7f); // Gray-blue
                case "game_start":
                    return new Color(0.3f, 0.9f, 0.3f); // Green
                case "game_end":
                    return new Color(0.9f, 0.7f, 0.2f); // Gold
                case "player_guess":
                    return new Color(0.6f, 0.8f, 0.6f); // Light green
                case "game_abandon":
                    return new Color(0.9f, 0.5f, 0.2f); // Orange
                case "player_feedback":
                    return new Color(0.8f, 0.4f, 0.9f); // Purple
                case "error":
                    return new Color(1.0f, 0.3f, 0.3f); // Red
                default:
                    return Color.white;
            }
        }

        #endregion

        #region Export

        private void ExportToCSV()
        {
            string defaultName = $"DLYH_Telemetry_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
            string path = EditorUtility.SaveFilePanel("Export Telemetry Data", "", defaultName, "csv");

            if (string.IsNullOrEmpty(path))
                return;

            try
            {
                var sb = new StringBuilder();

                // Summary section
                sb.AppendLine("=== EVENT SUMMARY ===");
                sb.AppendLine("Event Type,Count");
                foreach (var item in _summary)
                {
                    sb.AppendLine($"{EscapeCSV(item.event_type)},{item.count}");
                }

                sb.AppendLine();

                // Game Stats section
                sb.AppendLine("=== GAME STATS ===");
                sb.AppendLine("Metric,Value");
                sb.AppendLine($"Total Sessions,{_gameStats.TotalSessions}");
                sb.AppendLine($"Games Started,{_gameStats.TotalGamesStarted}");
                sb.AppendLine($"Games Completed,{_gameStats.TotalGamesCompleted}");
                sb.AppendLine($"Games Abandoned,{_gameStats.TotalGamesAbandoned}");
                sb.AppendLine($"Player Wins,{_gameStats.PlayerWins}");
                sb.AppendLine($"Player Losses,{_gameStats.PlayerLosses}");
                sb.AppendLine($"Average Turns,{_gameStats.AverageTurns:F1}");

                if (_gameStats.TotalGamesStarted > 0)
                {
                    float completionRate = (float)_gameStats.TotalGamesCompleted / _gameStats.TotalGamesStarted;
                    sb.AppendLine($"Completion Rate,{completionRate:P1}");
                }

                if (_gameStats.TotalGamesCompleted > 0)
                {
                    float winRate = (float)_gameStats.PlayerWins / _gameStats.TotalGamesCompleted;
                    sb.AppendLine($"Win Rate,{winRate:P1}");
                }

                sb.AppendLine();

                // Players section
                if (_gameStats.PlayerNames.Count > 0)
                {
                    sb.AppendLine("=== PLAYERS ===");
                    sb.AppendLine("Player Name,Games Played");
                    foreach (var kvp in _gameStats.PlayerNames)
                    {
                        sb.AppendLine($"{EscapeCSV(kvp.Key)},{kvp.Value}");
                    }
                    sb.AppendLine();
                }

                // Difficulty breakdown
                if (_gameStats.DifficultyBreakdown.Count > 0)
                {
                    sb.AppendLine("=== DIFFICULTY DISTRIBUTION ===");
                    sb.AppendLine("Difficulty,Count");
                    foreach (var kvp in _gameStats.DifficultyBreakdown)
                    {
                        sb.AppendLine($"{EscapeCSV(kvp.Key)},{kvp.Value}");
                    }
                    sb.AppendLine();
                }

                // Recent Events section
                sb.AppendLine("=== RECENT EVENTS ===");
                sb.AppendLine("ID,Timestamp,Session ID,Event Type,Event Data");
                foreach (var evt in _recentEvents)
                {
                    sb.AppendLine($"{evt.id},{EscapeCSV(evt.timestamp)},{EscapeCSV(evt.session_id)},{EscapeCSV(evt.event_type)},{EscapeCSV(evt.event_data)}");
                }

                sb.AppendLine();

                // Feedback section
                sb.AppendLine("=== PLAYER FEEDBACK ===");
                sb.AppendLine("ID,Timestamp,Session ID,Player Won,Comment");
                foreach (var entry in _feedback)
                {
                    string won = entry.PlayerWon ? "Yes" : "No";
                    sb.AppendLine($"{entry.id},{EscapeCSV(entry.timestamp)},{EscapeCSV(entry.session_id)},{won},{EscapeCSV(entry.comment)}");
                }

                File.WriteAllText(path, sb.ToString());
                EditorUtility.RevealInFinder(path);
                Debug.Log($"[TelemetryDashboard] Exported telemetry data to: {path}");
            }
            catch (Exception e)
            {
                EditorUtility.DisplayDialog("Export Failed", $"Failed to export data: {e.Message}", "OK");
                Debug.LogError($"[TelemetryDashboard] Export failed: {e}");
            }
        }

        private string EscapeCSV(string value)
        {
            if (string.IsNullOrEmpty(value))
                return "";

            // If contains comma, quote, or newline, wrap in quotes and escape quotes
            if (value.Contains(",") || value.Contains("\"") || value.Contains("\n") || value.Contains("\r"))
            {
                return "\"" + value.Replace("\"", "\"\"") + "\"";
            }
            return value;
        }

        #endregion

        #region Data Classes

        [Serializable]
        private class EventSummary
        {
            public string event_type;
            public int count;
        }

        [Serializable]
        private class EventSummaryList
        {
            public EventSummary[] items;
        }

        [Serializable]
        private class TelemetryEvent
        {
            public int id;
            public string session_id;
            public string event_type;
            public string event_data;
            public string timestamp;
        }

        [Serializable]
        private class TelemetryEventList
        {
            public TelemetryEvent[] items;
        }

        [Serializable]
        private class FeedbackEntry
        {
            public int id;
            public string timestamp;
            public string session_id;
            public string comment;
            public int player_won; // 0 or 1 from SQLite

            public bool PlayerWon => player_won != 0;
        }

        [Serializable]
        private class FeedbackEntryList
        {
            public FeedbackEntry[] items;
        }

        private class GameStats
        {
            public int TotalSessions;
            public int TotalGamesStarted;
            public int TotalGamesCompleted;
            public int TotalGamesAbandoned;
            public int PlayerWins;
            public int PlayerLosses;
            public float AverageTurns;
            public Dictionary<string, int> PlayerNames = new Dictionary<string, int>();
            public Dictionary<string, int> DifficultyBreakdown = new Dictionary<string, int>();

            public void Reset()
            {
                TotalSessions = 0;
                TotalGamesStarted = 0;
                TotalGamesCompleted = 0;
                TotalGamesAbandoned = 0;
                PlayerWins = 0;
                PlayerLosses = 0;
                AverageTurns = 0;
                PlayerNames.Clear();
                DifficultyBreakdown.Clear();
            }
        }

        #endregion
    }
}

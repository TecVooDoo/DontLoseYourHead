// NetworkingTestController.cs
// Odin-based debug testing for multiplayer connection
// Created: January 7, 2026
// Purpose: Phase 0.5 verification - test networking before UI Toolkit migration
// NOTE: This is throwaway test code - delete after verification complete

using UnityEngine;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using DLYH.Networking.Services;
using TecVooDoo.DontLoseYourHead.UI;

namespace DLYH.Networking.UI
{
    /// <summary>
    /// Debug controller for testing multiplayer connection using Odin Inspector buttons.
    /// No UI needed - just use Inspector buttons during Play mode.
    /// </summary>
    public class NetworkingTestController : MonoBehaviour
    {
        #region Serialized Fields

        [TitleGroup("References")]
        [SerializeField, Required] private SupabaseConfig _supabaseConfig;
        [SerializeField] private NetworkGameManager _networkManager;
        [SerializeField] private GameplayUIController _gameplayController;

        [TitleGroup("Join Game")]
        [SerializeField] private string _joinGameCode = "";

        [TitleGroup("Player Info")]
        [SerializeField] private string _playerName = "TestPlayer";

        #endregion

        #region Status Display (Read-Only in Inspector)

        [TitleGroup("Status")]
        [ShowInInspector, ReadOnly] private string _status = "Not initialized";
        [ShowInInspector, ReadOnly] private bool _initialized;
        [ShowInInspector, ReadOnly] private bool _isHost;
        [ShowInInspector, ReadOnly] private string _currentGameCode;
        [ShowInInspector, ReadOnly] private string _playerId;

        #endregion

        #region Private Fields

        private GameSessionService _sessionService;
        private PlayerService _playerService;
        private SupabaseClient _supabaseClient;
        private bool _waitingForOpponent;

        #endregion

        #region Odin Buttons - Initialization

        [TitleGroup("Step 1 - Initialize")]
        [Button("Initialize Services", ButtonSizes.Large), GUIColor(0.4f, 0.8f, 0.4f)]
        [EnableIf("@!_initialized && UnityEngine.Application.isPlaying")]
        private void InitializeButton()
        {
            InitializeServicesAsync().Forget();
        }

        #endregion

        #region Odin Buttons - Host/Join

        [TitleGroup("Step 2 - Host or Join")]
        [Button("HOST Game", ButtonSizes.Large), GUIColor(0.4f, 0.6f, 1f)]
        [EnableIf("@_initialized && string.IsNullOrEmpty(_currentGameCode) && UnityEngine.Application.isPlaying")]
        private void HostGameButton()
        {
            HostGameAsync().Forget();
        }

        [TitleGroup("Step 2 - Host or Join")]
        [Button("JOIN Game", ButtonSizes.Large), GUIColor(1f, 0.8f, 0.4f)]
        [EnableIf("@_initialized && string.IsNullOrEmpty(_currentGameCode) && !string.IsNullOrEmpty(_joinGameCode) && UnityEngine.Application.isPlaying")]
        private void JoinGameButton()
        {
            JoinGameAsync().Forget();
        }

        #endregion

        #region Odin Buttons - Control

        [TitleGroup("Step 3 - Control")]
        [Button("Cancel / Reset", ButtonSizes.Medium), GUIColor(1f, 0.4f, 0.4f)]
        [EnableIf("@!string.IsNullOrEmpty(_currentGameCode) && UnityEngine.Application.isPlaying")]
        private void CancelButton()
        {
            _waitingForOpponent = false;
            _currentGameCode = null;
            _isHost = false;
            _status = "Cancelled - ready to host or join";
            Debug.Log("[NetworkingTest] Cancelled");
        }

        [TitleGroup("Step 3 - Control")]
        [Button("Log Full Status", ButtonSizes.Medium)]
        private void LogStatusButton()
        {
            LogConnectionStatus();
        }

        #endregion

        #region Initialization

        private async UniTaskVoid InitializeServicesAsync()
        {
            _status = "Initializing...";

            if (_supabaseConfig == null)
            {
                _status = "ERROR: SupabaseConfig not assigned!";
                Debug.LogError("[NetworkingTest] SupabaseConfig not assigned!");
                return;
            }

            // Create Supabase client (no auth needed - using anon key)
            _supabaseClient = new SupabaseClient(_supabaseConfig);
            _sessionService = new GameSessionService(_supabaseClient, _supabaseConfig);
            _playerService = new PlayerService(_supabaseClient);

            // Ensure we have a player record in the players table
            string playerName = string.IsNullOrEmpty(_playerName) ? "TestPlayer" : _playerName;
            string playerId = await _playerService.EnsurePlayerRecordAsync(playerName);

            if (string.IsNullOrEmpty(playerId))
            {
                _status = "ERROR: Failed to create player record!";
                Debug.LogError("[NetworkingTest] Failed to create player record!");
                return;
            }

            _playerId = playerId;
            _initialized = true;
            _status = "Ready - Host or Join a game";
            Debug.Log($"[NetworkingTest] Initialized with player ID: {playerId}");
        }

        #endregion

        #region Host Game

        private async UniTaskVoid HostGameAsync()
        {
            _isHost = true;
            _status = "Creating game...";

            // Create game session (created_by can be null for simplicity)
            GameSession gameSession = await _sessionService.CreateGame(null);
            if (gameSession == null)
            {
                _status = "ERROR: Failed to create game!";
                return;
            }

            _currentGameCode = gameSession.Id;

            // Add self as player 1
            bool joined = await _sessionService.JoinGame(_currentGameCode, _playerId, 1);
            if (!joined)
            {
                _status = "ERROR: Failed to join own game as host!";
                Debug.LogError("[NetworkingTest] Host failed to join own game");
                return;
            }

            _status = $"HOSTING: {_currentGameCode} - Waiting for opponent...";
            Debug.Log($"[NetworkingTest] Created and joined game: {_currentGameCode}");

            await WaitForOpponentAsync();
        }

        private async UniTask WaitForOpponentAsync()
        {
            _waitingForOpponent = true;
            int attempts = 0;
            int maxAttempts = 300;

            while (_waitingForOpponent && attempts < maxAttempts)
            {
                int playerCount = await _sessionService.GetPlayerCount(_currentGameCode);

                if (playerCount >= 2)
                {
                    _status = "Opponent joined! Starting multiplayer...";
                    Debug.Log("[NetworkingTest] Opponent joined!");
                    await StartMultiplayerGameAsync();
                    return;
                }

                attempts++;
                if (attempts % 10 == 0)
                {
                    _status = $"HOSTING: {_currentGameCode} - Waiting... ({attempts}s)";
                }

                await UniTask.Delay(1000);
            }

            if (_waitingForOpponent)
            {
                _status = "Timeout - no opponent joined";
                _currentGameCode = null;
                _isHost = false;
            }
        }

        #endregion

        #region Join Game

        private async UniTaskVoid JoinGameAsync()
        {
            string gameCode = _joinGameCode?.Trim().ToUpper();
            if (string.IsNullOrEmpty(gameCode) || gameCode.Length != 6)
            {
                _status = "Enter a valid 6-character game code";
                return;
            }

            _isHost = false;
            _currentGameCode = gameCode;
            _status = $"Joining {gameCode}...";

            GameSession existingGame = await _sessionService.GetGame(_currentGameCode);
            if (existingGame == null)
            {
                _status = "ERROR: Game not found!";
                _currentGameCode = null;
                return;
            }

            int playerCount = await _sessionService.GetPlayerCount(_currentGameCode);
            if (playerCount >= 2)
            {
                _status = "ERROR: Game is full!";
                _currentGameCode = null;
                return;
            }

            // Join as player 2 using our players table ID
            bool joined = await _sessionService.JoinGame(
                _currentGameCode,
                _playerId,
                2
            );

            if (!joined)
            {
                _status = "ERROR: Failed to join game!";
                _currentGameCode = null;
                return;
            }

            _status = $"JOINED: {_currentGameCode} - Starting multiplayer...";
            Debug.Log($"[NetworkingTest] Joined game: {_currentGameCode}");

            await StartMultiplayerGameAsync();
        }

        #endregion

        #region Start Multiplayer

        private async UniTask StartMultiplayerGameAsync()
        {
            // Phase 0.5 verification: Just confirm both players joined successfully
            // Full NetworkGameManager integration will be tested in Phase 1

            int playerCount = await _sessionService.GetPlayerCount(_currentGameCode);

            if (playerCount >= 2)
            {
                _status = $"SUCCESS! Game {_currentGameCode} - {playerCount} players connected";
                Debug.Log($"[NetworkingTest] *** PHASE 0.5 VERIFICATION COMPLETE ***");
                Debug.Log($"[NetworkingTest] Game Code: {_currentGameCode}");
                Debug.Log($"[NetworkingTest] Player Count: {playerCount}");
                Debug.Log($"[NetworkingTest] Role: {(_isHost ? "HOST" : "CLIENT")}");
                Debug.Log($"[NetworkingTest] Player ID: {_playerId}");
                Debug.Log("[NetworkingTest] Database operations working correctly!");

                // Optional: Try full NetworkGameManager if assigned and configured
                if (_networkManager != null)
                {
                    try
                    {
                        await _networkManager.InitializeAsync();
                        await _networkManager.StartMultiplayerGameAsync(_currentGameCode, _isHost);

                        IOpponent remoteOpponent = _networkManager.CurrentOpponent;
                        if (remoteOpponent != null)
                        {
                            Debug.Log($"[NetworkingTest] Remote opponent created: {remoteOpponent.OpponentName}");
                            _status = $"READY! Opponent: {remoteOpponent.OpponentName}";

                            if (_gameplayController != null)
                            {
                                _gameplayController.SetOpponent(remoteOpponent);
                                Debug.Log("[NetworkingTest] Opponent injected into GameplayUIController");
                            }
                        }
                    }
                    catch (System.Exception ex)
                    {
                        Debug.LogWarning($"[NetworkingTest] NetworkGameManager integration skipped: {ex.Message}");
                        Debug.Log("[NetworkingTest] (This is OK for Phase 0.5 - core DB operations verified)");
                    }
                }
            }
            else
            {
                _status = $"Waiting for players... ({playerCount}/2)";
            }
        }

        #endregion

        #region Runtime Debug GUI

        private bool _showDebugGUI = true;
        private string _guiJoinCode = "";
        private string _guiPlayerName = "Player";
        private Rect _windowRect = new Rect(10, 10, 280, 380);

        private void OnGUI()
        {
            if (!_showDebugGUI) return;

            _windowRect = GUI.Window(0, _windowRect, DrawDebugWindow, "Multiplayer Test");
        }

        private void DrawDebugWindow(int windowId)
        {
            GUILayout.BeginVertical();

            // Status
            GUILayout.Label($"Status: {_status}", GUI.skin.box);
            GUILayout.Space(5);

            // Player ID (truncated)
            if (!string.IsNullOrEmpty(_playerId))
            {
                string shortId = _playerId.Length > 12 ? _playerId.Substring(0, 12) + "..." : _playerId;
                GUILayout.Label($"Player: {shortId}");
            }

            GUILayout.Space(5);

            // Player name input (before init)
            if (!_initialized)
            {
                GUILayout.Label("Player Name:");
                _guiPlayerName = GUILayout.TextField(_guiPlayerName, 20);
            }

            GUILayout.Space(10);

            // Initialize button
            GUI.enabled = !_initialized && Application.isPlaying;
            if (GUILayout.Button("Initialize Services", GUILayout.Height(40)))
            {
                _playerName = _guiPlayerName;
                InitializeServicesAsync().Forget();
            }

            GUILayout.Space(10);

            // Host button
            GUI.enabled = _initialized && string.IsNullOrEmpty(_currentGameCode) && Application.isPlaying;
            if (GUILayout.Button("HOST Game", GUILayout.Height(40)))
            {
                HostGameAsync().Forget();
            }

            GUILayout.Space(10);

            // Join section
            GUILayout.Label("Game Code:");
            _guiJoinCode = GUILayout.TextField(_guiJoinCode, 6).ToUpper();

            GUI.enabled = _initialized && string.IsNullOrEmpty(_currentGameCode)
                          && !string.IsNullOrEmpty(_guiJoinCode) && _guiJoinCode.Length == 6
                          && Application.isPlaying;
            if (GUILayout.Button("JOIN Game", GUILayout.Height(40)))
            {
                _joinGameCode = _guiJoinCode;
                JoinGameAsync().Forget();
            }

            GUILayout.Space(10);

            // Cancel button
            GUI.enabled = !string.IsNullOrEmpty(_currentGameCode) && Application.isPlaying;
            if (GUILayout.Button("Cancel / Reset", GUILayout.Height(30)))
            {
                CancelButton();
            }

            GUI.enabled = true;

            GUILayout.Space(5);

            // Current game code display
            if (!string.IsNullOrEmpty(_currentGameCode))
            {
                GUILayout.Label($"Game Code: {_currentGameCode}", GUI.skin.box);
                GUILayout.Label(_isHost ? "Role: HOST" : "Role: CLIENT");
            }

            GUILayout.EndVertical();

            // Make window draggable
            GUI.DragWindow();
        }

        #endregion

        #region Debug

        private void LogConnectionStatus()
        {
            Debug.Log("=== NetworkingTest Status ===");
            Debug.Log($"  Initialized: {_initialized}");
            Debug.Log($"  UserId: {_playerId}");
            Debug.Log($"  IsHost: {_isHost}");
            Debug.Log($"  GameCode: {_currentGameCode}");
            Debug.Log($"  Status: {_status}");

            if (_networkManager != null)
            {
                Debug.Log($"  NetworkManager.IsInGame: {_networkManager.IsInGame}");
                Debug.Log($"  NetworkManager.CurrentOpponent: {_networkManager.CurrentOpponent?.OpponentName ?? "null"}");
            }
            else
            {
                Debug.Log("  NetworkManager: NOT ASSIGNED");
            }

            if (_gameplayController != null)
            {
                var opponent = _gameplayController.GetCurrentOpponent();
                Debug.Log($"  GameplayController.Opponent: {opponent?.OpponentName ?? "null"}");
            }
            else
            {
                Debug.Log("  GameplayController: NOT ASSIGNED");
            }
            Debug.Log("=============================");
        }

        #endregion
    }
}

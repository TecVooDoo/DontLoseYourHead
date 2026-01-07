// NetworkingTestController.cs
// Minimal debug UI for testing multiplayer connection
// Created: January 7, 2026
// Purpose: Phase 0.5 verification - test networking before UI Toolkit migration
// NOTE: This is throwaway test code - delete after verification complete

using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Cysharp.Threading.Tasks;
using DLYH.Networking.Services;
using TecVooDoo.DontLoseYourHead.UI;

namespace DLYH.Networking.UI
{
    /// <summary>
    /// Debug controller for testing multiplayer connection.
    /// Provides Host/Join UI and bridges NetworkGameManager to GameplayUIController.
    /// </summary>
    public class NetworkingTestController : MonoBehaviour
    {
        #region Serialized Fields

        [Header("References")]
        [SerializeField] private NetworkGameManager _networkManager;
        [SerializeField] private GameplayUIController _gameplayController;
        [SerializeField] private SupabaseConfig _supabaseConfig;

        [Header("UI - Connection Panel")]
        [SerializeField] private GameObject _connectionPanel;
        [SerializeField] private Button _hostButton;
        [SerializeField] private Button _joinButton;
        [SerializeField] private TMP_InputField _gameCodeInput;
        [SerializeField] private TextMeshProUGUI _statusText;

        [Header("UI - Waiting Panel")]
        [SerializeField] private GameObject _waitingPanel;
        [SerializeField] private TextMeshProUGUI _gameCodeDisplay;
        [SerializeField] private TextMeshProUGUI _waitingStatusText;
        [SerializeField] private Button _cancelButton;

        [Header("UI - Setup Panel")]
        [SerializeField] private GameObject _setupPanel;

        #endregion

        #region Private Fields

        private bool _isHost;
        private string _currentGameCode;
        private bool _initialized;
        private GameSessionService _sessionService;
        private AuthService _authService;
        private SupabaseClient _supabaseClient;

        #endregion

        #region Unity Lifecycle

        private async void Start()
        {
            SetupUI();
            await InitializeServicesAsync();
        }

        private void OnDestroy()
        {
            CleanupUI();
        }

        #endregion

        #region Initialization

        private void SetupUI()
        {
            if (_hostButton != null)
                _hostButton.onClick.AddListener(OnHostClicked);

            if (_joinButton != null)
                _joinButton.onClick.AddListener(OnJoinClicked);

            if (_cancelButton != null)
                _cancelButton.onClick.AddListener(OnCancelClicked);

            ShowConnectionPanel();
        }

        private void CleanupUI()
        {
            if (_hostButton != null)
                _hostButton.onClick.RemoveListener(OnHostClicked);

            if (_joinButton != null)
                _joinButton.onClick.RemoveListener(OnJoinClicked);

            if (_cancelButton != null)
                _cancelButton.onClick.RemoveListener(OnCancelClicked);
        }

        private async UniTask InitializeServicesAsync()
        {
            UpdateStatus("Initializing...");

            if (_supabaseConfig == null)
            {
                UpdateStatus("ERROR: SupabaseConfig not assigned!");
                return;
            }

            _authService = new AuthService(_supabaseConfig);

            AuthSession session = await _authService.SignInAnonymouslyAsync();
            if (session == null)
            {
                UpdateStatus("ERROR: Failed to authenticate!");
                return;
            }

            _supabaseClient = new SupabaseClient(_supabaseConfig, session.AccessToken);
            _sessionService = new GameSessionService(_supabaseClient, _supabaseConfig);

            _initialized = true;
            UpdateStatus("Ready - Host or Join a game");
            Debug.Log($"[NetworkingTest] Initialized with user: {session.UserId}");
        }

        #endregion

        #region Panel Management

        private void ShowConnectionPanel()
        {
            if (_connectionPanel != null) _connectionPanel.SetActive(true);
            if (_waitingPanel != null) _waitingPanel.SetActive(false);
            if (_setupPanel != null) _setupPanel.SetActive(false);
        }

        private void ShowWaitingPanel()
        {
            if (_connectionPanel != null) _connectionPanel.SetActive(false);
            if (_waitingPanel != null) _waitingPanel.SetActive(true);
            if (_setupPanel != null) _setupPanel.SetActive(false);
        }

        private void ShowSetupPanel()
        {
            if (_connectionPanel != null) _connectionPanel.SetActive(false);
            if (_waitingPanel != null) _waitingPanel.SetActive(false);
            if (_setupPanel != null) _setupPanel.SetActive(true);
        }

        private void UpdateStatus(string message)
        {
            Debug.Log($"[NetworkingTest] {message}");
            if (_statusText != null)
                _statusText.text = message;
        }

        private void UpdateWaitingStatus(string message)
        {
            Debug.Log($"[NetworkingTest] {message}");
            if (_waitingStatusText != null)
                _waitingStatusText.text = message;
        }

        #endregion

        #region Button Handlers

        private async void OnHostClicked()
        {
            if (!_initialized)
            {
                UpdateStatus("Not initialized yet!");
                return;
            }

            _isHost = true;
            UpdateStatus("Creating game...");

            AuthSession session = await _authService.EnsureValidSessionAsync();
            if (session == null)
            {
                UpdateStatus("ERROR: Authentication failed!");
                return;
            }

            GameSession gameSession = await _sessionService.CreateGame(session.UserId);
            if (gameSession == null)
            {
                UpdateStatus("ERROR: Failed to create game!");
                return;
            }

            _currentGameCode = gameSession.Id;

            ShowWaitingPanel();
            if (_gameCodeDisplay != null)
                _gameCodeDisplay.text = $"Game Code: {_currentGameCode}";

            UpdateWaitingStatus("Waiting for opponent to join...");

            await WaitForOpponentAsync();
        }

        private async void OnJoinClicked()
        {
            if (!_initialized)
            {
                UpdateStatus("Not initialized yet!");
                return;
            }

            string gameCode = _gameCodeInput?.text?.Trim().ToUpper();
            if (string.IsNullOrEmpty(gameCode) || gameCode.Length != 6)
            {
                UpdateStatus("Enter a valid 6-character game code");
                return;
            }

            _isHost = false;
            _currentGameCode = gameCode;
            UpdateStatus("Joining game...");

            AuthSession session = await _authService.EnsureValidSessionAsync();
            if (session == null)
            {
                UpdateStatus("ERROR: Authentication failed!");
                return;
            }

            int playerCount = await _sessionService.GetPlayerCount(_currentGameCode);
            if (playerCount < 0)
            {
                UpdateStatus("ERROR: Game not found!");
                return;
            }

            if (playerCount >= 2)
            {
                UpdateStatus("ERROR: Game is full!");
                return;
            }

            bool joined = await _sessionService.JoinGame(
                _currentGameCode,
                session.UserId,
                2,
                "Player2",
                "#0000FF"
            );

            if (!joined)
            {
                UpdateStatus("ERROR: Failed to join game!");
                return;
            }

            ShowWaitingPanel();
            if (_gameCodeDisplay != null)
                _gameCodeDisplay.text = $"Joined: {_currentGameCode}";

            UpdateWaitingStatus("Connected! Waiting for host to start...");

            await StartMultiplayerGameAsync();
        }

        private void OnCancelClicked()
        {
            _currentGameCode = null;
            ShowConnectionPanel();
            UpdateStatus("Cancelled");
        }

        #endregion

        #region Game Flow

        private async UniTask WaitForOpponentAsync()
        {
            int attempts = 0;
            int maxAttempts = 300;

            while (attempts < maxAttempts)
            {
                int playerCount = await _sessionService.GetPlayerCount(_currentGameCode);

                if (playerCount >= 2)
                {
                    UpdateWaitingStatus("Opponent joined! Starting game...");
                    await StartMultiplayerGameAsync();
                    return;
                }

                attempts++;
                if (attempts % 10 == 0)
                {
                    UpdateWaitingStatus($"Waiting for opponent... ({attempts}s)");
                }

                await UniTask.Delay(1000);
            }

            UpdateWaitingStatus("Timeout - no opponent joined");
            ShowConnectionPanel();
            UpdateStatus("Timeout waiting for opponent");
        }

        private async UniTask StartMultiplayerGameAsync()
        {
            UpdateWaitingStatus("Initializing multiplayer...");

            if (_networkManager != null)
            {
                await _networkManager.InitializeAsync();
                await _networkManager.StartMultiplayerGameAsync(_currentGameCode, _isHost);

                IOpponent remoteOpponent = _networkManager.CurrentOpponent;
                if (remoteOpponent != null)
                {
                    Debug.Log($"[NetworkingTest] Remote opponent created: {remoteOpponent.OpponentName}");

                    if (_gameplayController != null)
                    {
                        _gameplayController.SetOpponent(remoteOpponent);
                        Debug.Log("[NetworkingTest] Opponent injected into GameplayUIController");
                    }
                    else
                    {
                        Debug.LogWarning("[NetworkingTest] GameplayUIController not assigned - opponent not wired!");
                    }

                    UpdateWaitingStatus("Multiplayer ready!");
                    ShowSetupPanel();
                }
                else
                {
                    UpdateWaitingStatus("ERROR: Failed to create remote opponent");
                }
            }
            else
            {
                UpdateWaitingStatus("ERROR: NetworkGameManager not assigned!");
            }
        }

        #endregion

        #region Debug Methods

        [ContextMenu("Log Connection Status")]
        public void LogConnectionStatus()
        {
            Debug.Log($"[NetworkingTest] Initialized: {_initialized}");
            Debug.Log($"[NetworkingTest] IsHost: {_isHost}");
            Debug.Log($"[NetworkingTest] GameCode: {_currentGameCode}");

            if (_networkManager != null)
            {
                Debug.Log($"[NetworkingTest] NetworkManager.IsInGame: {_networkManager.IsInGame}");
                Debug.Log($"[NetworkingTest] NetworkManager.CurrentOpponent: {_networkManager.CurrentOpponent?.OpponentName}");
            }
        }

        #endregion
    }
}

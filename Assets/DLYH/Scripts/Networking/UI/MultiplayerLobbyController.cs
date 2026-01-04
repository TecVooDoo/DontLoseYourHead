// MultiplayerLobbyController.cs
// Controls the multiplayer lobby UI for creating/joining games
// Created: January 4, 2026
// Developer: TecVooDoo LLC

using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Cysharp.Threading.Tasks;
using DLYH.Networking.Services;

namespace DLYH.Networking.UI
{
    /// <summary>
    /// Lobby state machine states
    /// </summary>
    public enum LobbyState
    {
        Idle,           // Initial state, showing create/join options
        Creating,       // Creating a new game
        WaitingHost,    // Waiting for player 2 to join (host)
        Joining,        // Joining an existing game
        WaitingGuest,   // Waiting for host to be ready (guest)
        Matchmaking,    // Looking for opponent with AI fallback
        Ready,          // Both players ready, about to start
        Error           // Error state
    }

    /// <summary>
    /// Controls the multiplayer lobby UI.
    /// Handles game creation, joining, and matchmaking with AI fallback.
    /// </summary>
    public class MultiplayerLobbyController : MonoBehaviour
    {
        // ============================================================
        // EVENTS
        // ============================================================

        /// <summary>Fired when both players are ready to start</summary>
        public event Action<string, bool> OnGameReady; // gameCode, isHost

        /// <summary>Fired when user cancels and wants to return to main menu</summary>
        public event Action OnCancel;

        /// <summary>Fired when matchmaking times out and AI fallback is triggered</summary>
        public event Action OnAIFallback;

        // ============================================================
        // UI REFERENCES
        // ============================================================

        [Header("Panel References")]
        [SerializeField] private GameObject _lobbyPanel;
        [SerializeField] private GameObject _createJoinPanel;
        [SerializeField] private GameObject _waitingPanel;
        [SerializeField] private GameObject _gameCodePanel;

        [Header("Create/Join UI")]
        [SerializeField] private Button _createGameButton;
        [SerializeField] private Button _joinGameButton;
        [SerializeField] private Button _quickMatchButton;
        [SerializeField] private Button _backButton;

        [Header("Join Game UI")]
        [SerializeField] private TMP_InputField _gameCodeInput;
        [SerializeField] private Button _submitCodeButton;
        [SerializeField] private Button _cancelJoinButton;
        [SerializeField] private TextMeshProUGUI _joinErrorText;

        [Header("Waiting UI")]
        [SerializeField] private TextMeshProUGUI _waitingStatusText;
        [SerializeField] private TextMeshProUGUI _gameCodeDisplayText;
        [SerializeField] private Button _copyCodeButton;
        [SerializeField] private Button _cancelWaitButton;
        [SerializeField] private GameObject _loadingSpinner;

        [Header("Matchmaking UI")]
        [SerializeField] private TextMeshProUGUI _matchmakingTimerText;
        [SerializeField] private TextMeshProUGUI _matchmakingStatusText;

        [Header("Configuration")]
        [SerializeField] private SupabaseConfig _supabaseConfig;
        [SerializeField] private float _matchmakingTimeoutSeconds = 5f;
        [SerializeField] private float _pollingIntervalSeconds = 2f;

        // ============================================================
        // STATE
        // ============================================================

        private LobbyState _currentState = LobbyState.Idle;
        private string _currentGameCode;
        private bool _isHost;
        private float _matchmakingStartTime;
        private bool _matchmakingActive;

        // Services
        private SupabaseClient _supabaseClient;
        private AuthService _authService;
        private GameSessionService _sessionService;

        // ============================================================
        // PROPERTIES
        // ============================================================

        public LobbyState CurrentState => _currentState;
        public string CurrentGameCode => _currentGameCode;
        public bool IsHost => _isHost;

        // ============================================================
        // UNITY LIFECYCLE
        // ============================================================

        private void Awake()
        {
            WireButtonEvents();
        }

        private void Start()
        {
            InitializeServices();
            ShowCreateJoinPanel();
        }

        private void Update()
        {
            if (_matchmakingActive)
            {
                UpdateMatchmakingTimer();
            }
        }

        private void OnDestroy()
        {
            UnwireButtonEvents();
        }

        // ============================================================
        // INITIALIZATION
        // ============================================================

        private void InitializeServices()
        {
            if (_supabaseConfig == null)
            {
                Debug.LogError("[MultiplayerLobbyController] SupabaseConfig not assigned!");
                return;
            }

            _supabaseClient = new SupabaseClient(_supabaseConfig);
            _authService = new AuthService(_supabaseConfig);
            _sessionService = new GameSessionService(_supabaseClient, _supabaseConfig);
        }

        private void WireButtonEvents()
        {
            if (_createGameButton != null)
                _createGameButton.onClick.AddListener(OnCreateGameClicked);

            if (_joinGameButton != null)
                _joinGameButton.onClick.AddListener(OnJoinGameClicked);

            if (_quickMatchButton != null)
                _quickMatchButton.onClick.AddListener(OnQuickMatchClicked);

            if (_backButton != null)
                _backButton.onClick.AddListener(OnBackClicked);

            if (_submitCodeButton != null)
                _submitCodeButton.onClick.AddListener(OnSubmitCodeClicked);

            if (_cancelJoinButton != null)
                _cancelJoinButton.onClick.AddListener(ShowCreateJoinPanel);

            if (_copyCodeButton != null)
                _copyCodeButton.onClick.AddListener(OnCopyCodeClicked);

            if (_cancelWaitButton != null)
                _cancelWaitButton.onClick.AddListener(OnCancelWaitClicked);
        }

        private void UnwireButtonEvents()
        {
            if (_createGameButton != null)
                _createGameButton.onClick.RemoveListener(OnCreateGameClicked);

            if (_joinGameButton != null)
                _joinGameButton.onClick.RemoveListener(OnJoinGameClicked);

            if (_quickMatchButton != null)
                _quickMatchButton.onClick.RemoveListener(OnQuickMatchClicked);

            if (_backButton != null)
                _backButton.onClick.RemoveListener(OnBackClicked);

            if (_submitCodeButton != null)
                _submitCodeButton.onClick.RemoveListener(OnSubmitCodeClicked);

            if (_copyCodeButton != null)
                _copyCodeButton.onClick.RemoveListener(OnCopyCodeClicked);

            if (_cancelWaitButton != null)
                _cancelWaitButton.onClick.RemoveListener(OnCancelWaitClicked);
        }

        // ============================================================
        // PANEL MANAGEMENT
        // ============================================================

        public void Show()
        {
            if (_lobbyPanel != null)
                _lobbyPanel.SetActive(true);

            ShowCreateJoinPanel();
        }

        public void Hide()
        {
            if (_lobbyPanel != null)
                _lobbyPanel.SetActive(false);

            _matchmakingActive = false;
        }

        private void ShowCreateJoinPanel()
        {
            SetState(LobbyState.Idle);

            if (_createJoinPanel != null) _createJoinPanel.SetActive(true);
            if (_gameCodePanel != null) _gameCodePanel.SetActive(false);
            if (_waitingPanel != null) _waitingPanel.SetActive(false);

            if (_joinErrorText != null) _joinErrorText.text = "";
        }

        private void ShowGameCodeInputPanel()
        {
            if (_createJoinPanel != null) _createJoinPanel.SetActive(false);
            if (_gameCodePanel != null) _gameCodePanel.SetActive(true);
            if (_waitingPanel != null) _waitingPanel.SetActive(false);

            if (_gameCodeInput != null)
            {
                _gameCodeInput.text = "";
                _gameCodeInput.Select();
            }

            if (_joinErrorText != null) _joinErrorText.text = "";
        }

        private void ShowWaitingPanel(string statusText)
        {
            if (_createJoinPanel != null) _createJoinPanel.SetActive(false);
            if (_gameCodePanel != null) _gameCodePanel.SetActive(false);
            if (_waitingPanel != null) _waitingPanel.SetActive(true);

            if (_waitingStatusText != null) _waitingStatusText.text = statusText;
            if (_gameCodeDisplayText != null) _gameCodeDisplayText.text = _currentGameCode ?? "";
            if (_loadingSpinner != null) _loadingSpinner.SetActive(true);
        }

        // ============================================================
        // STATE MANAGEMENT
        // ============================================================

        private void SetState(LobbyState newState)
        {
            Debug.Log($"[MultiplayerLobbyController] State: {_currentState} -> {newState}");
            _currentState = newState;
        }

        // ============================================================
        // BUTTON HANDLERS
        // ============================================================

        private void OnCreateGameClicked()
        {
            CreateGameAsync().Forget();
        }

        private void OnJoinGameClicked()
        {
            ShowGameCodeInputPanel();
        }

        private void OnQuickMatchClicked()
        {
            StartQuickMatchAsync().Forget();
        }

        private void OnBackClicked()
        {
            OnCancel?.Invoke();
        }

        private void OnSubmitCodeClicked()
        {
            if (_gameCodeInput != null && !string.IsNullOrEmpty(_gameCodeInput.text))
            {
                string code = _gameCodeInput.text.ToUpper().Trim();
                JoinGameAsync(code).Forget();
            }
        }

        private void OnCopyCodeClicked()
        {
            if (!string.IsNullOrEmpty(_currentGameCode))
            {
                GUIUtility.systemCopyBuffer = _currentGameCode;
                Debug.Log($"[MultiplayerLobbyController] Copied code: {_currentGameCode}");

                // Visual feedback
                if (_copyCodeButton != null)
                {
                    var text = _copyCodeButton.GetComponentInChildren<TextMeshProUGUI>();
                    if (text != null)
                    {
                        string original = text.text;
                        text.text = "Copied!";
                        RestoreButtonTextAsync(text, original, 1.5f).Forget();
                    }
                }
            }
        }

        private async UniTask RestoreButtonTextAsync(TextMeshProUGUI text, string original, float delay)
        {
            await UniTask.Delay((int)(delay * 1000));
            if (text != null) text.text = original;
        }

        private void OnCancelWaitClicked()
        {
            _matchmakingActive = false;
            // TODO: Cancel game on server if needed
            ShowCreateJoinPanel();
        }

        // ============================================================
        // CREATE GAME
        // ============================================================

        private async UniTask CreateGameAsync()
        {
            SetState(LobbyState.Creating);

            // Ensure auth
            var session = await _authService.EnsureValidSessionAsync();
            if (session == null)
            {
                ShowError("Authentication failed");
                return;
            }

            // Update client with auth
            _supabaseClient = new SupabaseClient(_supabaseConfig, session.AccessToken);
            _sessionService = new GameSessionService(_supabaseClient, _supabaseConfig);

            // Create game
            var game = await _sessionService.CreateGame(session.UserId);
            if (game == null)
            {
                ShowError("Failed to create game");
                return;
            }

            _currentGameCode = game.Id;
            _isHost = true;

            // Join as player 1
            await _sessionService.JoinGame(game.Id, session.UserId, 1, "Player 1", "#4CAF50");

            // Show waiting UI
            SetState(LobbyState.WaitingHost);
            ShowWaitingPanel("Waiting for opponent...\nShare this code:");

            // Start polling for player 2
            WaitForOpponentAsync().Forget();
        }

        // ============================================================
        // JOIN GAME
        // ============================================================

        private async UniTask JoinGameAsync(string gameCode)
        {
            if (string.IsNullOrEmpty(gameCode) || gameCode.Length != 6)
            {
                ShowJoinError("Invalid game code");
                return;
            }

            SetState(LobbyState.Joining);

            // Ensure auth
            var session = await _authService.EnsureValidSessionAsync();
            if (session == null)
            {
                ShowJoinError("Authentication failed");
                return;
            }

            // Update client with auth
            _supabaseClient = new SupabaseClient(_supabaseConfig, session.AccessToken);
            _sessionService = new GameSessionService(_supabaseClient, _supabaseConfig);

            // Check if game exists
            var game = await _sessionService.GetGame(gameCode);
            if (game == null)
            {
                ShowJoinError("Game not found");
                return;
            }

            if (game.Status != "waiting")
            {
                ShowJoinError("Game already in progress");
                return;
            }

            // Check player count
            int playerCount = await _sessionService.GetPlayerCount(gameCode);
            if (playerCount >= 2)
            {
                ShowJoinError("Game is full");
                return;
            }

            // Join as player 2
            bool joined = await _sessionService.JoinGame(gameCode, session.UserId, 2, "Player 2", "#2196F3");
            if (!joined)
            {
                ShowJoinError("Failed to join game");
                return;
            }

            _currentGameCode = gameCode;
            _isHost = false;

            // Trigger ready
            SetState(LobbyState.Ready);
            OnGameReady?.Invoke(_currentGameCode, _isHost);
        }

        // ============================================================
        // QUICK MATCH (with AI fallback)
        // ============================================================

        private async UniTask StartQuickMatchAsync()
        {
            SetState(LobbyState.Matchmaking);
            ShowWaitingPanel("Finding opponent...");

            _matchmakingActive = true;
            _matchmakingStartTime = Time.realtimeSinceStartup;

            // Ensure auth
            var session = await _authService.EnsureValidSessionAsync();
            if (session == null)
            {
                ShowError("Authentication failed");
                return;
            }

            // Update client with auth
            _supabaseClient = new SupabaseClient(_supabaseConfig, session.AccessToken);
            _sessionService = new GameSessionService(_supabaseClient, _supabaseConfig);

            // Create game and wait for match
            var game = await _sessionService.CreateGame(session.UserId);
            if (game == null)
            {
                ShowError("Failed to create game");
                return;
            }

            _currentGameCode = game.Id;
            _isHost = true;

            // Join as player 1
            await _sessionService.JoinGame(game.Id, session.UserId, 1, "Player 1", "#4CAF50");

            // Poll for player 2 with timeout
            while (_matchmakingActive && Time.realtimeSinceStartup - _matchmakingStartTime < _matchmakingTimeoutSeconds)
            {
                int playerCount = await _sessionService.GetPlayerCount(_currentGameCode);
                if (playerCount >= 2)
                {
                    // Opponent found!
                    _matchmakingActive = false;
                    SetState(LobbyState.Ready);
                    OnGameReady?.Invoke(_currentGameCode, _isHost);
                    return;
                }

                await UniTask.Delay((int)(_pollingIntervalSeconds * 1000));
            }

            // Timeout - trigger AI fallback
            if (_matchmakingActive)
            {
                _matchmakingActive = false;
                Debug.Log("[MultiplayerLobbyController] Matchmaking timeout - falling back to AI");
                OnAIFallback?.Invoke();
            }
        }

        private void UpdateMatchmakingTimer()
        {
            float elapsed = Time.realtimeSinceStartup - _matchmakingStartTime;
            float remaining = Mathf.Max(0, _matchmakingTimeoutSeconds - elapsed);

            if (_matchmakingTimerText != null)
            {
                _matchmakingTimerText.text = $"{remaining:F1}s";
            }

            if (_matchmakingStatusText != null)
            {
                _matchmakingStatusText.text = remaining > 2f
                    ? "Looking for opponent..."
                    : "No opponent found, AI backup in...";
            }
        }

        // ============================================================
        // WAIT FOR OPPONENT (Host)
        // ============================================================

        private async UniTask WaitForOpponentAsync()
        {
            while (_currentState == LobbyState.WaitingHost)
            {
                int playerCount = await _sessionService.GetPlayerCount(_currentGameCode);
                if (playerCount >= 2)
                {
                    Debug.Log("[MultiplayerLobbyController] Opponent joined!");
                    SetState(LobbyState.Ready);
                    OnGameReady?.Invoke(_currentGameCode, _isHost);
                    return;
                }

                await UniTask.Delay((int)(_pollingIntervalSeconds * 1000));
            }
        }

        // ============================================================
        // ERROR HANDLING
        // ============================================================

        private void ShowError(string message)
        {
            SetState(LobbyState.Error);
            Debug.LogError($"[MultiplayerLobbyController] Error: {message}");

            if (_waitingStatusText != null)
            {
                _waitingStatusText.text = $"Error: {message}";
            }

            // Return to main panel after delay
            ReturnToMainAfterDelay(3f).Forget();
        }

        private void ShowJoinError(string message)
        {
            if (_joinErrorText != null)
            {
                _joinErrorText.text = message;
                _joinErrorText.color = Color.red;
            }
            SetState(LobbyState.Idle);
        }

        private async UniTask ReturnToMainAfterDelay(float seconds)
        {
            await UniTask.Delay((int)(seconds * 1000));
            if (_currentState == LobbyState.Error)
            {
                ShowCreateJoinPanel();
            }
        }
    }
}

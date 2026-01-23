// NetworkingUIManager.cs
// Manages UI Toolkit networking overlays (matchmaking, waiting room, join code)
// Created: January 17, 2026
// Developer: TecVooDoo LLC

using System;
using UnityEngine;
using UnityEngine.UIElements;
using Cysharp.Threading.Tasks;
using DLYH.Networking.Services;

namespace DLYH.Networking.UI
{
    /// <summary>
    /// Result of a networking operation.
    /// </summary>
    public class NetworkingUIResult
    {
        public bool Success;
        public bool Cancelled;
        public string GameCode;
        public bool IsHost;
        public bool IsPhantomAI;
        public string OpponentName;
        public string ErrorMessage;
    }

    /// <summary>
    /// Manages UI Toolkit networking overlays for matchmaking, waiting room, and join code entry.
    /// Works with the existing MatchmakingService backend.
    /// </summary>
    public class NetworkingUIManager
    {
        // ============================================================
        // EVENTS
        // ============================================================

        /// <summary>Fired when matchmaking/join completes successfully</summary>
        public event Action<NetworkingUIResult> OnNetworkingComplete;

        /// <summary>Fired when user cancels</summary>
        public event Action OnCancelled;

        // ============================================================
        // UI ASSETS
        // ============================================================

        private readonly VisualTreeAsset _matchmakingOverlayUxml;
        private readonly VisualTreeAsset _waitingRoomUxml;
        private readonly VisualTreeAsset _joinCodeEntryUxml;
        private readonly StyleSheet _matchmakingOverlayUss;
        private readonly StyleSheet _waitingRoomUss;
        private readonly StyleSheet _joinCodeEntryUss;

        // ============================================================
        // UI ELEMENTS
        // ============================================================

        private VisualElement _root;
        private VisualElement _currentOverlay;

        // Matchmaking overlay elements
        private Label _matchmakingTitle;
        private Label _countdownTimer;
        private VisualElement _progressBarFill;
        private VisualElement _searchingState;
        private VisualElement _foundState;
        private Label _opponentNameLabel;
        private Button _matchmakingCancelBtn;

        // Waiting room elements
        private Label _joinCodeLabel;
        private Button _copyCodeBtn;
        private Button _shareCodeBtn;
        private Label _waitingStatusText;
        private VisualElement _joinedState;
        private Label _joinedNameLabel;
        private Button _waitingCancelBtn;
        private Button _startGameBtn;

        // Current game code for cleanup on cancel
        private string _currentGameCode;
        private GameSessionService _gameSessionService;

        // Join code entry elements
        private TextField _codeInput;
        private Label _joinErrorLabel;
        private VisualElement _joiningState;
        private Button _joinCancelBtn;
        private Button _joinBtn;

        // ============================================================
        // STATE
        // ============================================================

        private MatchmakingService _matchmakingService;
        private PlayerService _playerService;
        private bool _isActive;
        private float _matchmakingStartTime;
        private const float MATCHMAKING_DURATION = 6f;

        // ============================================================
        // CONSTRUCTOR
        // ============================================================

        public NetworkingUIManager(
            VisualTreeAsset matchmakingOverlayUxml,
            VisualTreeAsset waitingRoomUxml,
            VisualTreeAsset joinCodeEntryUxml,
            StyleSheet matchmakingOverlayUss,
            StyleSheet waitingRoomUss,
            StyleSheet joinCodeEntryUss)
        {
            _matchmakingOverlayUxml = matchmakingOverlayUxml;
            _waitingRoomUxml = waitingRoomUxml;
            _joinCodeEntryUxml = joinCodeEntryUxml;
            _matchmakingOverlayUss = matchmakingOverlayUss;
            _waitingRoomUss = waitingRoomUss;
            _joinCodeEntryUss = joinCodeEntryUss;
        }

        // ============================================================
        // INITIALIZATION
        // ============================================================

        /// <summary>
        /// Initializes the manager with the root element and networking services.
        /// </summary>
        public void Initialize(VisualElement root, MatchmakingService matchmakingService, PlayerService playerService, GameSessionService gameSessionService = null)
        {
            _root = root;
            _matchmakingService = matchmakingService;
            _playerService = playerService;
            _gameSessionService = gameSessionService;
        }

        // ============================================================
        // PUBLIC API - MATCHMAKING (Play Online)
        // ============================================================

        /// <summary>
        /// Shows the matchmaking overlay and starts searching for an opponent.
        /// After 6 seconds with no match, spawns a phantom AI.
        /// </summary>
        /// <param name="gridSize">Player's grid size for matchmaking preferences</param>
        /// <param name="difficulty">Player's difficulty for matchmaking preferences</param>
        public async UniTask StartMatchmakingAsync(int gridSize, string difficulty)
        {
            if (_isActive) return;
            _isActive = true;

            ShowMatchmakingOverlay();
            _matchmakingStartTime = Time.realtimeSinceStartup;

            // Start countdown animation
            UpdateMatchmakingUIAsync().Forget();

            // Start actual matchmaking
            if (_matchmakingService != null)
            {
                MatchmakingResult result = await _matchmakingService.StartMatchmakingAsync(gridSize, difficulty);
                HandleMatchmakingResult(result);
            }
            else
            {
                // No service - simulate phantom AI fallback
                await UniTask.Delay((int)(MATCHMAKING_DURATION * 1000));
                SimulatePhantomAIMatch();
            }
        }

        /// <summary>
        /// Cancels the current matchmaking search.
        /// </summary>
        public void CancelMatchmaking()
        {
            if (!_isActive) return;

            _isActive = false;
            _matchmakingService?.CancelMatchmakingAsync().Forget();
            HideCurrentOverlay();
            OnCancelled?.Invoke();
        }

        // ============================================================
        // PUBLIC API - WAITING ROOM (Create Private Game)
        // ============================================================

        /// <summary>
        /// Shows the waiting room with a join code for another player to join.
        /// </summary>
        /// <param name="gridSize">Grid size for the private game</param>
        /// <param name="difficulty">Difficulty setting for the private game</param>
        public async UniTask ShowWaitingRoomAsync(int gridSize = 8, string difficulty = "normal")
        {
            if (_isActive) return;
            _isActive = true;
            _currentGameCode = null; // Reset

            // Create private game and get code
            if (_matchmakingService != null)
            {
                MatchmakingResult result = await _matchmakingService.CreatePrivateGameAsync();
                if (result.Success)
                {
                    _currentGameCode = result.GameCode; // Store for cleanup on cancel
                    ShowWaitingRoomOverlay(result.GameCode);
                    // Start polling for opponent
                    PollForOpponentAsync(result.GameCode).Forget();
                }
                else
                {
                    _isActive = false;
                    NetworkingUIResult errorResult = new NetworkingUIResult
                    {
                        Success = false,
                        ErrorMessage = result.ErrorMessage
                    };
                    OnNetworkingComplete?.Invoke(errorResult);
                }
            }
            else
            {
                // No service - show with dummy code
                string dummyCode = GenerateDummyCode();
                _currentGameCode = dummyCode; // Store for reference
                Debug.Log($"[NetworkingUIManager] No matchmaking service - showing dummy code: {dummyCode}");
                ShowWaitingRoomOverlay(dummyCode);
            }
        }

        private string GenerateDummyCode()
        {
            const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789"; // Avoiding confusing characters
            char[] code = new char[6];
            for (int i = 0; i < 6; i++)
            {
                code[i] = chars[UnityEngine.Random.Range(0, chars.Length)];
            }
            return new string(code);
        }

        /// <summary>
        /// Cancels waiting for an opponent and deletes the created game.
        /// </summary>
        public void CancelWaiting()
        {
            if (!_isActive) return;

            _isActive = false;

            // Delete the game that was created
            if (!string.IsNullOrEmpty(_currentGameCode) && _gameSessionService != null && _playerService != null)
            {
                string playerId = _playerService.CurrentPlayerId;
                if (!string.IsNullOrEmpty(playerId))
                {
                    Debug.Log($"[NetworkingUIManager] Cancelling - deleting game {_currentGameCode}");
                    _gameSessionService.RemovePlayerFromGame(_currentGameCode, playerId).Forget();
                }
            }

            _currentGameCode = null;
            HideCurrentOverlay();
            OnCancelled?.Invoke();
        }

        // ============================================================
        // PUBLIC API - JOIN CODE ENTRY (Join Game)
        // ============================================================

        /// <summary>
        /// Shows the join code entry modal.
        /// </summary>
        public void ShowJoinCodeEntry()
        {
            if (_isActive) return;
            _isActive = true;

            ShowJoinCodeOverlay();
        }

        /// <summary>
        /// Attempts to join a game with the entered code.
        /// Can be called directly (without showing overlay) or from the join code overlay.
        /// </summary>
        public async UniTask JoinWithCodeAsync(string code)
        {
            if (string.IsNullOrEmpty(code) || code.Length != 6)
            {
                ShowJoinError("Invalid code - must be 6 characters");
                return;
            }

            // Ensure _isActive is set - this method can be called directly without ShowJoinCodeEntry
            _isActive = true;

            ShowJoiningState();

            if (_matchmakingService != null)
            {
                MatchmakingResult result = await _matchmakingService.JoinWithCodeAsync(code);
                HandleJoinResult(result);
            }
            else
            {
                // No service - simulate success
                await UniTask.Delay(1000);
                NetworkingUIResult successResult = new NetworkingUIResult
                {
                    Success = true,
                    GameCode = code,
                    IsHost = false,
                    IsPhantomAI = false
                };
                _isActive = false;
                HideCurrentOverlay();
                OnNetworkingComplete?.Invoke(successResult);
            }
        }

        /// <summary>
        /// Cancels join code entry.
        /// </summary>
        public void CancelJoinCode()
        {
            if (!_isActive) return;

            _isActive = false;
            HideCurrentOverlay();
            OnCancelled?.Invoke();
        }

        // ============================================================
        // OVERLAY CREATION
        // ============================================================

        private void ShowMatchmakingOverlay()
        {
            if (_matchmakingOverlayUxml == null || _root == null) return;

            // Create and add overlay
            _currentOverlay = _matchmakingOverlayUxml.CloneTree();
            if (_matchmakingOverlayUss != null)
            {
                _currentOverlay.styleSheets.Add(_matchmakingOverlayUss);
            }

            // Make the TemplateContainer fill the screen
            _currentOverlay.style.position = Position.Absolute;
            _currentOverlay.style.left = 0;
            _currentOverlay.style.right = 0;
            _currentOverlay.style.top = 0;
            _currentOverlay.style.bottom = 0;

            _root.Add(_currentOverlay);

            // Cache elements
            _matchmakingTitle = _currentOverlay.Q<Label>("modal-title");
            _countdownTimer = _currentOverlay.Q<Label>("countdown-timer");
            _progressBarFill = _currentOverlay.Q<VisualElement>("progress-bar-fill");
            _searchingState = _currentOverlay.Q<VisualElement>("searching-state");
            _foundState = _currentOverlay.Q<VisualElement>("found-state");
            _opponentNameLabel = _currentOverlay.Q<Label>("opponent-name");
            _matchmakingCancelBtn = _currentOverlay.Q<Button>("btn-cancel");

            // Wire events
            if (_matchmakingCancelBtn != null)
            {
                _matchmakingCancelBtn.clicked += CancelMatchmaking;
            }

            // Reset UI state
            if (_searchingState != null) _searchingState.RemoveFromClassList("hidden");
            if (_foundState != null) _foundState.AddToClassList("hidden");
            if (_countdownTimer != null) _countdownTimer.text = "6";
            if (_progressBarFill != null) _progressBarFill.style.width = Length.Percent(100);
        }

        private void ShowWaitingRoomOverlay(string gameCode)
        {
            if (_waitingRoomUxml == null || _root == null) return;

            _currentOverlay = _waitingRoomUxml.CloneTree();
            if (_waitingRoomUss != null)
            {
                _currentOverlay.styleSheets.Add(_waitingRoomUss);
            }

            // Make the TemplateContainer fill the screen
            _currentOverlay.style.position = Position.Absolute;
            _currentOverlay.style.left = 0;
            _currentOverlay.style.right = 0;
            _currentOverlay.style.top = 0;
            _currentOverlay.style.bottom = 0;

            _root.Add(_currentOverlay);

            // Cache elements
            _joinCodeLabel = _currentOverlay.Q<Label>("join-code");
            _copyCodeBtn = _currentOverlay.Q<Button>("btn-copy");
            _shareCodeBtn = _currentOverlay.Q<Button>("btn-share");
            _waitingStatusText = _currentOverlay.Q<Label>("status-text");
            _joinedState = _currentOverlay.Q<VisualElement>("joined-state");
            _joinedNameLabel = _currentOverlay.Q<Label>("joined-name");
            _waitingCancelBtn = _currentOverlay.Q<Button>("btn-cancel");
            _startGameBtn = _currentOverlay.Q<Button>("btn-start");

            // Wire events
            if (_copyCodeBtn != null)
            {
                _copyCodeBtn.clicked += () => CopyCodeToClipboard(gameCode);
            }
            if (_shareCodeBtn != null)
            {
                _shareCodeBtn.clicked += () => ShareGameCode(gameCode);
            }
            if (_waitingCancelBtn != null)
            {
                _waitingCancelBtn.clicked += CancelWaiting;
            }
            if (_startGameBtn != null)
            {
                _startGameBtn.clicked += () => StartGameWithoutOpponent(gameCode);
            }

            // Set initial state
            if (_joinCodeLabel != null) _joinCodeLabel.text = gameCode;
            if (_joinedState != null) _joinedState.AddToClassList("hidden");
            // Start Game button is now visible by default (user can start without waiting)
        }

        private void ShowJoinCodeOverlay()
        {
            if (_joinCodeEntryUxml == null || _root == null) return;

            _currentOverlay = _joinCodeEntryUxml.CloneTree();
            if (_joinCodeEntryUss != null)
            {
                _currentOverlay.styleSheets.Add(_joinCodeEntryUss);
            }

            // Make the TemplateContainer fill the screen
            _currentOverlay.style.position = Position.Absolute;
            _currentOverlay.style.left = 0;
            _currentOverlay.style.right = 0;
            _currentOverlay.style.top = 0;
            _currentOverlay.style.bottom = 0;

            _root.Add(_currentOverlay);

            // Cache elements
            _codeInput = _currentOverlay.Q<TextField>("code-input");
            _joinErrorLabel = _currentOverlay.Q<Label>("error-message");
            _joiningState = _currentOverlay.Q<VisualElement>("joining-state");
            _joinCancelBtn = _currentOverlay.Q<Button>("btn-cancel");
            _joinBtn = _currentOverlay.Q<Button>("btn-join");

            // Wire events
            if (_joinCancelBtn != null)
            {
                _joinCancelBtn.clicked += CancelJoinCode;
            }
            if (_joinBtn != null)
            {
                _joinBtn.clicked += () =>
                {
                    string code = _codeInput?.value?.Trim().ToUpper() ?? "";
                    JoinWithCodeAsync(code).Forget();
                };
            }

            // Auto-uppercase input
            if (_codeInput != null)
            {
                _codeInput.RegisterValueChangedCallback(evt =>
                {
                    string upper = evt.newValue.ToUpper();
                    if (upper != evt.newValue)
                    {
                        _codeInput.SetValueWithoutNotify(upper);
                    }
                });
            }

            // Reset state
            if (_joinErrorLabel != null) _joinErrorLabel.AddToClassList("hidden");
            if (_joiningState != null) _joiningState.AddToClassList("hidden");
        }

        private void HideCurrentOverlay()
        {
            if (_currentOverlay != null)
            {
                _currentOverlay.RemoveFromHierarchy();
                _currentOverlay = null;
            }
        }

        // ============================================================
        // MATCHMAKING UI UPDATES
        // ============================================================

        private async UniTask UpdateMatchmakingUIAsync()
        {
            while (_isActive && _currentOverlay != null)
            {
                float elapsed = Time.realtimeSinceStartup - _matchmakingStartTime;
                float remaining = Mathf.Max(0, MATCHMAKING_DURATION - elapsed);
                float progress = remaining / MATCHMAKING_DURATION;

                // Update countdown
                if (_countdownTimer != null)
                {
                    _countdownTimer.text = Mathf.CeilToInt(remaining).ToString();
                }

                // Update progress bar
                if (_progressBarFill != null)
                {
                    _progressBarFill.style.width = Length.Percent(progress * 100);
                }

                await UniTask.Delay(100);
            }
        }

        private void ShowMatchFound(string opponentName)
        {
            if (_searchingState != null) _searchingState.AddToClassList("hidden");
            if (_foundState != null) _foundState.RemoveFromClassList("hidden");
            if (_opponentNameLabel != null) _opponentNameLabel.text = opponentName;
            if (_matchmakingCancelBtn != null) _matchmakingCancelBtn.AddToClassList("hidden");
        }

        private void HandleMatchmakingResult(MatchmakingResult result)
        {
            if (!_isActive) return;

            if (result.Success)
            {
                ShowMatchFound(result.OpponentName ?? "Opponent");

                // Delay before transitioning to game
                DelayThenComplete(result).Forget();
            }
            else
            {
                _isActive = false;
                HideCurrentOverlay();
                NetworkingUIResult errorResult = new NetworkingUIResult
                {
                    Success = false,
                    ErrorMessage = result.ErrorMessage
                };
                OnNetworkingComplete?.Invoke(errorResult);
            }
        }

        private async UniTask DelayThenComplete(MatchmakingResult result)
        {
            await UniTask.Delay(1500); // Show "found" state briefly

            _isActive = false;
            HideCurrentOverlay();

            NetworkingUIResult uiResult = new NetworkingUIResult
            {
                Success = true,
                GameCode = result.GameCode,
                IsHost = result.IsHost,
                IsPhantomAI = result.IsPhantomAI,
                OpponentName = result.OpponentName
            };
            OnNetworkingComplete?.Invoke(uiResult);
        }

        private void SimulatePhantomAIMatch()
        {
            if (!_isActive) return;

            string[] phantomNames = { "Alex", "Jordan", "Taylor", "Morgan", "Casey", "Riley" };
            string phantomName = phantomNames[UnityEngine.Random.Range(0, phantomNames.Length)];

            ShowMatchFound(phantomName);

            // Delay then complete
            SimulateDelayThenComplete(phantomName).Forget();
        }

        private async UniTask SimulateDelayThenComplete(string phantomName)
        {
            await UniTask.Delay(1500);

            _isActive = false;
            HideCurrentOverlay();

            NetworkingUIResult result = new NetworkingUIResult
            {
                Success = true,
                GameCode = "LOCAL",
                IsHost = true,
                IsPhantomAI = true,
                OpponentName = phantomName
            };
            OnNetworkingComplete?.Invoke(result);
        }

        // ============================================================
        // WAITING ROOM UPDATES
        // ============================================================

        private async UniTask PollForOpponentAsync(string gameCode)
        {
            const int POLL_INTERVAL_MS = 2000;

            Debug.Log($"[NetworkingUIManager] Starting opponent polling for game {gameCode}");

            while (_isActive)
            {
                await UniTask.Delay(POLL_INTERVAL_MS);

                if (!_isActive) break;

                // Check if opponent has joined
                if (_gameSessionService != null)
                {
                    int playerCount = await _gameSessionService.GetPlayerCount(gameCode);
                    Debug.Log($"[NetworkingUIManager] Poll: game {gameCode} has {playerCount} players");

                    if (playerCount >= 2)
                    {
                        // Opponent joined! Get their info
                        GameSessionWithPlayers gameWithPlayers = await _gameSessionService.GetGameWithPlayers(gameCode);
                        if (gameWithPlayers != null && gameWithPlayers.Players != null)
                        {
                            // Find player 2 (the opponent who joined)
                            SessionPlayer opponent = null;
                            foreach (SessionPlayer player in gameWithPlayers.Players)
                            {
                                if (player.PlayerNumber == 2)
                                {
                                    opponent = player;
                                    break;
                                }
                            }

                            string opponentName = opponent?.PlayerName ?? "Opponent";
                            Debug.Log($"[NetworkingUIManager] Opponent joined: {opponentName}");

                            // Update UI to show opponent joined
                            ShowOpponentJoined(opponentName);
                        }
                        break;
                    }
                }
                else
                {
                    Debug.LogWarning("[NetworkingUIManager] GameSessionService is null - cannot poll for opponent");
                }
            }
        }

        /// <summary>
        /// Updates the Waiting Room UI to show that an opponent has joined.
        /// </summary>
        private void ShowOpponentJoined(string opponentName)
        {
            // Update status text
            if (_waitingStatusText != null)
            {
                _waitingStatusText.text = "Opponent joined!";
            }

            // Show the joined state with opponent name
            if (_joinedState != null)
            {
                _joinedState.RemoveFromClassList("hidden");
            }
            if (_joinedNameLabel != null)
            {
                _joinedNameLabel.text = opponentName;
            }

            Debug.Log($"[NetworkingUIManager] UI updated - opponent '{opponentName}' joined");
        }

        private void CopyCodeToClipboard(string code)
        {
            GUIUtility.systemCopyBuffer = code;

            // Visual feedback
            if (_copyCodeBtn != null)
            {
                _copyCodeBtn.text = "Copied!";
                ResetCopyButtonTextAsync().Forget();
            }
        }

        private async UniTask ResetCopyButtonTextAsync()
        {
            await UniTask.Delay(1500);
            if (_copyCodeBtn != null)
            {
                _copyCodeBtn.text = "Copy Code";
            }
        }

        private void ShareGameCode(string code)
        {
            // Create email sharing link using mailto:
            // The game URL would be tecvoodoo.com/dlyh/join?code=XXXXXX
            string gameUrl = $"https://tecvoodoo.com/dlyh/join?code={code}";
            string subject = "Join my Don't Lose Your Head game!";
            string body = $"I've started a game of Don't Lose Your Head! Join me using this code:\n\n{code}\n\nOr click this link: {gameUrl}";

            // URL encode the subject and body
            string encodedSubject = UnityEngine.Networking.UnityWebRequest.EscapeURL(subject);
            string encodedBody = UnityEngine.Networking.UnityWebRequest.EscapeURL(body);

            string mailtoUrl = $"mailto:?subject={encodedSubject}&body={encodedBody}";

            Debug.Log($"[NetworkingUIManager] Opening email share: {mailtoUrl}");
            Application.OpenURL(mailtoUrl);
        }

        private void StartGameWithoutOpponent(string gameCode)
        {
            if (!_isActive) return;

            Debug.Log($"[NetworkingUIManager] Starting game {gameCode} without waiting for opponent");

            _isActive = false;
            _currentGameCode = null; // Don't delete - we're using it

            HideCurrentOverlay();

            // Fire completion event - host starts the game, opponent slot shows "Waiting..."
            NetworkingUIResult result = new NetworkingUIResult
            {
                Success = true,
                GameCode = gameCode,
                IsHost = true,
                IsPhantomAI = false,
                OpponentName = null // No opponent yet - will show "Waiting..." in game
            };
            OnNetworkingComplete?.Invoke(result);
        }

        // ============================================================
        // JOIN CODE UPDATES
        // ============================================================

        private void ShowJoinError(string message)
        {
            if (_joinErrorLabel != null)
            {
                _joinErrorLabel.text = message;
                _joinErrorLabel.RemoveFromClassList("hidden");
            }
            if (_joiningState != null) _joiningState.AddToClassList("hidden");
        }

        private void ShowJoiningState()
        {
            if (_joinErrorLabel != null) _joinErrorLabel.AddToClassList("hidden");
            if (_joiningState != null) _joiningState.RemoveFromClassList("hidden");
            if (_joinBtn != null) _joinBtn.SetEnabled(false);
        }

        private void HandleJoinResult(MatchmakingResult result)
        {
            if (!_isActive) return;

            if (result.Success)
            {
                _isActive = false;
                HideCurrentOverlay();

                NetworkingUIResult uiResult = new NetworkingUIResult
                {
                    Success = true,
                    GameCode = result.GameCode,
                    IsHost = false,
                    IsPhantomAI = false
                };
                OnNetworkingComplete?.Invoke(uiResult);
            }
            else
            {
                ShowJoinError(result.ErrorMessage ?? "Failed to join game");
                if (_joinBtn != null) _joinBtn.SetEnabled(true);
            }
        }

        // ============================================================
        // CLEANUP
        // ============================================================

        public void Dispose()
        {
            _isActive = false;
            HideCurrentOverlay();
        }
    }
}

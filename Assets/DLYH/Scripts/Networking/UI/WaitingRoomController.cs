// WaitingRoomController.cs
// Waiting room UI for multiplayer setup phase
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
    /// Controls the waiting room UI during multiplayer setup phase.
    /// Shows status of both players completing their setup.
    /// </summary>
    public class WaitingRoomController : MonoBehaviour
    {
        // ============================================================
        // EVENTS
        // ============================================================

        /// <summary>Fired when both players are ready to start gameplay</summary>
        public event Action OnBothPlayersReady;

        /// <summary>Fired when user cancels and wants to leave</summary>
        public event Action OnCancel;

        /// <summary>Fired when opponent times out (no activity)</summary>
        public event Action OnOpponentTimeout;

        // ============================================================
        // UI REFERENCES
        // ============================================================

        [Header("Panel Reference")]
        [SerializeField] private GameObject _waitingRoomPanel;

        [Header("Player 1 Status")]
        [SerializeField] private TextMeshProUGUI _player1NameText;
        [SerializeField] private Image _player1StatusIcon;
        [SerializeField] private TextMeshProUGUI _player1StatusText;
        [SerializeField] private GameObject _player1ReadyCheckmark;

        [Header("Player 2 Status")]
        [SerializeField] private TextMeshProUGUI _player2NameText;
        [SerializeField] private Image _player2StatusIcon;
        [SerializeField] private TextMeshProUGUI _player2StatusText;
        [SerializeField] private GameObject _player2ReadyCheckmark;

        [Header("Game Info")]
        [SerializeField] private TextMeshProUGUI _gameCodeText;
        [SerializeField] private TextMeshProUGUI _waitingMessageText;

        [Header("Buttons")]
        [SerializeField] private Button _readyButton;
        [SerializeField] private Button _cancelButton;

        [Header("Settings")]
        [SerializeField] private float _pollingIntervalSeconds = 2f;
        [SerializeField] private float _timeoutSeconds = 300f; // 5 minutes

        [Header("Colors")]
        [SerializeField] private Color _readyColor = new Color(0.3f, 0.8f, 0.3f);
        [SerializeField] private Color _waitingColor = new Color(0.8f, 0.8f, 0.3f);
        [SerializeField] private Color _notReadyColor = new Color(0.5f, 0.5f, 0.5f);

        // ============================================================
        // STATE
        // ============================================================

        private string _gameCode;
        private bool _isHost;
        private bool _localPlayerReady;
        private bool _opponentReady;
        private bool _isPolling;
        private float _startTime;

        private GameStateSynchronizer _synchronizer;

        // ============================================================
        // UNITY LIFECYCLE
        // ============================================================

        private void Awake()
        {
            WireButtonEvents();
        }

        private void OnDestroy()
        {
            UnwireButtonEvents();
            _isPolling = false;
        }

        // ============================================================
        // INITIALIZATION
        // ============================================================

        private void WireButtonEvents()
        {
            if (_readyButton != null)
                _readyButton.onClick.AddListener(OnReadyClicked);

            if (_cancelButton != null)
                _cancelButton.onClick.AddListener(OnCancelClicked);
        }

        private void UnwireButtonEvents()
        {
            if (_readyButton != null)
                _readyButton.onClick.RemoveListener(OnReadyClicked);

            if (_cancelButton != null)
                _cancelButton.onClick.RemoveListener(OnCancelClicked);
        }

        // ============================================================
        // PUBLIC API
        // ============================================================

        /// <summary>
        /// Shows the waiting room for the given game.
        /// </summary>
        /// <param name="gameCode">Game code</param>
        /// <param name="isHost">Whether local player is host (player 1)</param>
        /// <param name="synchronizer">Game state synchronizer</param>
        /// <param name="localPlayerName">Local player's display name</param>
        public void Show(string gameCode, bool isHost, GameStateSynchronizer synchronizer, string localPlayerName)
        {
            _gameCode = gameCode;
            _isHost = isHost;
            _synchronizer = synchronizer;
            _localPlayerReady = false;
            _opponentReady = false;
            _startTime = Time.realtimeSinceStartup;

            if (_waitingRoomPanel != null)
                _waitingRoomPanel.SetActive(true);

            // Set initial UI
            if (_gameCodeText != null)
                _gameCodeText.text = $"Game: {gameCode}";

            // Set player names
            if (isHost)
            {
                SetPlayerUI(1, localPlayerName, false);
                SetPlayerUI(2, "Waiting...", false);
            }
            else
            {
                SetPlayerUI(1, "Opponent", false);
                SetPlayerUI(2, localPlayerName, false);
            }

            // Update ready button
            UpdateReadyButton();

            // Start polling for opponent status
            _isPolling = true;
            PollOpponentStatusAsync().Forget();
        }

        /// <summary>
        /// Hides the waiting room.
        /// </summary>
        public void Hide()
        {
            _isPolling = false;

            if (_waitingRoomPanel != null)
                _waitingRoomPanel.SetActive(false);
        }

        /// <summary>
        /// Call this when local player has completed their setup.
        /// </summary>
        public void SetLocalSetupComplete()
        {
            UpdateLocalStatus(true, false);

            if (_waitingMessageText != null)
                _waitingMessageText.text = "Setup complete! Click Ready when you want to start.";
        }

        // ============================================================
        // BUTTON HANDLERS
        // ============================================================

        private void OnReadyClicked()
        {
            if (_localPlayerReady) return;

            _localPlayerReady = true;
            UpdateLocalStatus(true, true);

            // Push ready status to server
            if (_synchronizer != null)
            {
                _synchronizer.SetPlayerReadyAsync().Forget();
            }

            UpdateReadyButton();
            CheckBothReady();
        }

        private void OnCancelClicked()
        {
            _isPolling = false;
            OnCancel?.Invoke();
        }

        // ============================================================
        // STATUS POLLING
        // ============================================================

        private async UniTask PollOpponentStatusAsync()
        {
            while (_isPolling)
            {
                // Check timeout
                if (Time.realtimeSinceStartup - _startTime > _timeoutSeconds)
                {
                    Debug.LogWarning("[WaitingRoomController] Opponent timeout");
                    _isPolling = false;
                    OnOpponentTimeout?.Invoke();
                    return;
                }

                // Fetch latest state
                if (_synchronizer != null)
                {
                    var state = await _synchronizer.FetchCurrentStateAsync();
                    if (state != null)
                    {
                        ProcessGameState(state);
                    }
                }

                await UniTask.Delay((int)(_pollingIntervalSeconds * 1000));
            }
        }

        private void ProcessGameState(DLYHGameState state)
        {
            // Get opponent data
            var opponentData = _isHost ? state.player2 : state.player1;
            var localData = _isHost ? state.player1 : state.player2;

            // Update opponent info
            if (opponentData != null)
            {
                string opponentName = opponentData.name ?? "Opponent";
                int playerNum = _isHost ? 2 : 1;

                SetPlayerUI(playerNum, opponentName, opponentData.setupComplete);

                if (opponentData.ready && !_opponentReady)
                {
                    _opponentReady = true;
                    SetPlayerReady(playerNum, true);
                    CheckBothReady();
                }
            }

            // Check if game has started
            if (state.status == "playing")
            {
                _isPolling = false;
                OnBothPlayersReady?.Invoke();
            }
        }

        private void CheckBothReady()
        {
            if (_localPlayerReady && _opponentReady)
            {
                if (_waitingMessageText != null)
                    _waitingMessageText.text = "Both players ready! Starting game...";

                // Game start is handled by server state change
            }
        }

        // ============================================================
        // UI HELPERS
        // ============================================================

        private void UpdateLocalStatus(bool setupComplete, bool ready)
        {
            int playerNum = _isHost ? 1 : 2;
            SetPlayerSetupComplete(playerNum, setupComplete);
            SetPlayerReady(playerNum, ready);
        }

        private void SetPlayerUI(int playerNum, string name, bool setupComplete)
        {
            TextMeshProUGUI nameText = playerNum == 1 ? _player1NameText : _player2NameText;
            Image statusIcon = playerNum == 1 ? _player1StatusIcon : _player2StatusIcon;
            TextMeshProUGUI statusText = playerNum == 1 ? _player1StatusText : _player2StatusText;

            if (nameText != null) nameText.text = name;
            if (statusIcon != null) statusIcon.color = setupComplete ? _waitingColor : _notReadyColor;
            if (statusText != null) statusText.text = setupComplete ? "Setup complete" : "Setting up...";
        }

        private void SetPlayerSetupComplete(int playerNum, bool complete)
        {
            Image statusIcon = playerNum == 1 ? _player1StatusIcon : _player2StatusIcon;
            TextMeshProUGUI statusText = playerNum == 1 ? _player1StatusText : _player2StatusText;

            if (statusIcon != null) statusIcon.color = complete ? _waitingColor : _notReadyColor;
            if (statusText != null) statusText.text = complete ? "Setup complete" : "Setting up...";
        }

        private void SetPlayerReady(int playerNum, bool ready)
        {
            Image statusIcon = playerNum == 1 ? _player1StatusIcon : _player2StatusIcon;
            TextMeshProUGUI statusText = playerNum == 1 ? _player1StatusText : _player2StatusText;
            GameObject checkmark = playerNum == 1 ? _player1ReadyCheckmark : _player2ReadyCheckmark;

            if (ready)
            {
                if (statusIcon != null) statusIcon.color = _readyColor;
                if (statusText != null) statusText.text = "Ready!";
                if (checkmark != null) checkmark.SetActive(true);
            }
        }

        private void UpdateReadyButton()
        {
            if (_readyButton != null)
            {
                _readyButton.interactable = !_localPlayerReady;

                var buttonText = _readyButton.GetComponentInChildren<TextMeshProUGUI>();
                if (buttonText != null)
                {
                    buttonText.text = _localPlayerReady ? "Waiting..." : "Ready!";
                }
            }
        }
    }
}

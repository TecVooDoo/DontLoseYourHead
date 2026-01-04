// ConnectionStatusIndicator.cs
// Visual indicator for network connection status
// Created: January 4, 2026
// Developer: TecVooDoo LLC

using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DLYH.Networking;

namespace DLYH.Networking.UI
{
    /// <summary>
    /// Connection status types for display.
    /// </summary>
    public enum ConnectionStatus
    {
        Disconnected,
        Connecting,
        Connected,
        Reconnecting,
        OpponentDisconnected
    }

    /// <summary>
    /// Visual indicator showing network connection status during multiplayer games.
    /// Shows opponent connection state and handles reconnection UI.
    /// </summary>
    public class ConnectionStatusIndicator : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private GameObject _indicatorContainer;
        [SerializeField] private Image _statusIcon;
        [SerializeField] private TextMeshProUGUI _statusText;
        [SerializeField] private GameObject _reconnectingSpinner;

        [Header("Status Colors")]
        [SerializeField] private Color _connectedColor = new Color(0.3f, 0.8f, 0.3f);
        [SerializeField] private Color _disconnectedColor = new Color(0.8f, 0.3f, 0.3f);
        [SerializeField] private Color _connectingColor = new Color(0.8f, 0.8f, 0.3f);

        [Header("Settings")]
        [SerializeField] private bool _hideWhenConnected = true;
        [SerializeField] private float _showConnectedDuration = 2f;

        private ConnectionStatus _currentStatus = ConnectionStatus.Disconnected;
        private float _showConnectedUntil;
        private IOpponent _opponent;

        // ============================================================
        // PROPERTIES
        // ============================================================

        public ConnectionStatus CurrentStatus => _currentStatus;

        // ============================================================
        // UNITY LIFECYCLE
        // ============================================================

        private void Start()
        {
            if (_indicatorContainer != null)
                _indicatorContainer.SetActive(false);
        }

        private void Update()
        {
            // Auto-hide after connected for a while
            if (_hideWhenConnected && _currentStatus == ConnectionStatus.Connected)
            {
                if (Time.realtimeSinceStartup > _showConnectedUntil)
                {
                    Hide();
                }
            }
        }

        // ============================================================
        // PUBLIC API
        // ============================================================

        /// <summary>
        /// Binds to an opponent to automatically track connection status.
        /// </summary>
        public void BindToOpponent(IOpponent opponent)
        {
            UnbindFromOpponent();

            _opponent = opponent;

            if (_opponent != null)
            {
                _opponent.OnDisconnected += HandleOpponentDisconnected;
                _opponent.OnReconnected += HandleOpponentReconnected;

                // Show initial connected state for non-AI
                if (!_opponent.IsAI)
                {
                    SetStatus(_opponent.IsConnected ? ConnectionStatus.Connected : ConnectionStatus.Disconnected);
                }
            }
        }

        /// <summary>
        /// Unbinds from the current opponent.
        /// </summary>
        public void UnbindFromOpponent()
        {
            if (_opponent != null)
            {
                _opponent.OnDisconnected -= HandleOpponentDisconnected;
                _opponent.OnReconnected -= HandleOpponentReconnected;
                _opponent = null;
            }
        }

        /// <summary>
        /// Sets the connection status and updates UI.
        /// </summary>
        public void SetStatus(ConnectionStatus status)
        {
            _currentStatus = status;
            UpdateUI();
        }

        /// <summary>
        /// Shows the indicator.
        /// </summary>
        public void Show()
        {
            if (_indicatorContainer != null)
                _indicatorContainer.SetActive(true);
        }

        /// <summary>
        /// Hides the indicator.
        /// </summary>
        public void Hide()
        {
            if (_indicatorContainer != null)
                _indicatorContainer.SetActive(false);
        }

        // ============================================================
        // EVENT HANDLERS
        // ============================================================

        private void HandleOpponentDisconnected()
        {
            SetStatus(ConnectionStatus.OpponentDisconnected);
        }

        private void HandleOpponentReconnected()
        {
            SetStatus(ConnectionStatus.Connected);
        }

        // ============================================================
        // UI UPDATE
        // ============================================================

        private void UpdateUI()
        {
            if (_indicatorContainer != null)
            {
                _indicatorContainer.SetActive(true);
            }

            switch (_currentStatus)
            {
                case ConnectionStatus.Disconnected:
                    SetUIState(_disconnectedColor, "Disconnected", false);
                    break;

                case ConnectionStatus.Connecting:
                    SetUIState(_connectingColor, "Connecting...", true);
                    break;

                case ConnectionStatus.Connected:
                    SetUIState(_connectedColor, "Connected", false);
                    _showConnectedUntil = Time.realtimeSinceStartup + _showConnectedDuration;
                    break;

                case ConnectionStatus.Reconnecting:
                    SetUIState(_connectingColor, "Reconnecting...", true);
                    break;

                case ConnectionStatus.OpponentDisconnected:
                    SetUIState(_disconnectedColor, "Opponent disconnected", false);
                    break;
            }
        }

        private void SetUIState(Color color, string text, bool showSpinner)
        {
            if (_statusIcon != null)
                _statusIcon.color = color;

            if (_statusText != null)
                _statusText.text = text;

            if (_reconnectingSpinner != null)
                _reconnectingSpinner.SetActive(showSpinner);
        }

        // ============================================================
        // CLEANUP
        // ============================================================

        private void OnDestroy()
        {
            UnbindFromOpponent();
        }
    }
}

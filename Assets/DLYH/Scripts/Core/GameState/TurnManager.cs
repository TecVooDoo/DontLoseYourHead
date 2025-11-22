using UnityEngine;
using Sirenix.OdinInspector;

namespace TecVooDoo.DontLoseYourHead.Core.GameState
{
    /// <summary>
    /// Manages turn flow between players, validates turn actions, and raises turn-related events
    /// </summary>
    public class TurnManager : MonoBehaviour
    {
        #region Serialized Fields
        [Title("Dependencies")]
        [Required]
        [SerializeField] private IntVariableSO _currentPlayerTurn;
        
        [Title("Turn Events")]
        [Required]
        [SerializeField] private GameEventSO _onTurnStarted;
        [Required]
        [SerializeField] private GameEventSO _onTurnEnded;
        [Required]
        [SerializeField] private GameEventSO _onTurnChanged;
        
        [Title("Configuration")]
        [SerializeField] private int _player1Index = 0;
        [SerializeField] private int _player2Index = 1;
        [SerializeField] private float _turnDelaySeconds = 0.5f;
        #endregion
        
        #region Private Fields
        private bool _isTurnInProgress;
        private int _totalPlayers = 2;
        #endregion
        
        #region Properties
        public int CurrentPlayerIndex => _currentPlayerTurn.Value;
        public bool IsPlayer1Turn => _currentPlayerTurn.Value == _player1Index;
        public bool IsPlayer2Turn => _currentPlayerTurn.Value == _player2Index;
        public bool IsTurnInProgress => _isTurnInProgress;
        #endregion
        
        #region Unity Lifecycle
        private void Awake()
        {
            ValidateDependencies();
        }
        
        private void Start()
        {
            InitializeTurn();
        }
        #endregion
        
        #region Public Methods
        /// <summary>
        /// Validates if the specified player can take an action on the current turn
        /// </summary>
        public bool CanTakeAction(int playerIndex)
        {
            if (!_isTurnInProgress)
            {
                Debug.LogWarning($"[TurnManager] No turn in progress. Cannot validate action for player {playerIndex}");
                return false;
            }
            
            if (playerIndex != CurrentPlayerIndex)
            {
                Debug.LogWarning($"[TurnManager] Player {playerIndex} tried to act on Player {CurrentPlayerIndex}'s turn");
                return false;
            }
            
            return true;
        }
        
        /// <summary>
        /// Ends the current player's turn and switches to the next player
        /// </summary>
        public void EndTurn()
        {
            if (!_isTurnInProgress)
            {
                Debug.LogWarning("[TurnManager] Cannot end turn - no turn in progress");
                return;
            }
            
            _isTurnInProgress = false;
            _onTurnEnded?.Raise();
            
            Debug.Log($"[TurnManager] Player {CurrentPlayerIndex}'s turn ended");
            
            // Delay before switching to next turn
            Invoke(nameof(SwitchToNextPlayer), _turnDelaySeconds);
        }
        
        /// <summary>
        /// Starts a new turn for the current player
        /// </summary>
        public void StartTurn()
        {
            if (_isTurnInProgress)
            {
                Debug.LogWarning("[TurnManager] Turn already in progress");
                return;
            }
            
            _isTurnInProgress = true;
            _onTurnStarted?.Raise();
            
            Debug.Log($"[TurnManager] Player {CurrentPlayerIndex}'s turn started");
        }
        
        /// <summary>
        /// Resets turn manager to initial state (Player 1's turn)
        /// </summary>
        public void ResetTurns()
        {
            _currentPlayerTurn.Value = _player1Index;
            _isTurnInProgress = false;
            
            Debug.Log("[TurnManager] Turn system reset to Player 1");
            
            // Start first turn after reset
            Invoke(nameof(StartTurn), _turnDelaySeconds);
        }
        #endregion
        
        #region Private Methods
        private void InitializeTurn()
        {
            // Set to Player 1's turn initially
            _currentPlayerTurn.Value = _player1Index;
            _isTurnInProgress = false;
            
            // Start the first turn
            Invoke(nameof(StartTurn), _turnDelaySeconds);
        }
        
        private void SwitchToNextPlayer()
        {
            int previousPlayer = CurrentPlayerIndex;
            
            // Simple two-player switching
            _currentPlayerTurn.Value = (CurrentPlayerIndex + 1) % _totalPlayers;
            
            _onTurnChanged?.Raise();
            
            Debug.Log($"[TurnManager] Turn switched from Player {previousPlayer} to Player {CurrentPlayerIndex}");
            
            // Start the new turn
            StartTurn();
        }
        
        private void ValidateDependencies()
        {
            if (_currentPlayerTurn == null)
            {
                Debug.LogError("[TurnManager] Missing CurrentPlayerTurn IntVariableSO!");
            }
            
            if (_onTurnStarted == null)
            {
                Debug.LogWarning("[TurnManager] Missing OnTurnStarted event");
            }
            
            if (_onTurnEnded == null)
            {
                Debug.LogWarning("[TurnManager] Missing OnTurnEnded event");
            }
            
            if (_onTurnChanged == null)
            {
                Debug.LogWarning("[TurnManager] Missing OnTurnChanged event");
            }
        }
        #endregion
        
        #region Debug Helpers
        [Button("Force Switch Turn")]
        [DisableInEditorMode]
        private void DebugForceSwitchTurn()
        {
            EndTurn();
        }
        
        [Button("Reset to Player 1")]
        [DisableInEditorMode]
        private void DebugResetToPlayer1()
        {
            ResetTurns();
        }
        #endregion
    }
}

using Sirenix.OdinInspector;
using TecVooDoo.DontLoseYourHead.Core.GameState;
using UnityEngine;

namespace TecVooDoo.DontLoseYourHead.Core
{
    /// <summary>
    /// Defines the different phases of the game
    /// </summary>
    public enum GamePhase
    {
        MainMenu,
        DifficultySelection,
        WordSelection,
        WordPlacement,
        GameplayActive,
        GameOver
    }

    /// <summary>
    /// Manages the overall game flow and state transitions
    /// </summary>
    public class GameStateMachine : MonoBehaviour
    {
        #region Serialized Fields
        [Title("Events")]
        [Required]
        [SerializeField] private GameEventSO _onGameStart;
        [Required]
        [SerializeField] private GameEventSO _onSetupComplete;
        [Required]
        [SerializeField] private GameEventSO _onGameplayStart;
        [Required]
        [SerializeField] private GameEventSO _onGameOver;
        [Required]
        [SerializeField] private TurnManager _turnManager;

        [Title("State Display")]
        [ReadOnly]
        [ShowInInspector]
        private GamePhase _currentPhase = GamePhase.MainMenu;
        #endregion

        #region Properties
        public GamePhase CurrentPhase => _currentPhase;
        public bool IsInGameplay => _currentPhase == GamePhase.GameplayActive;
        public bool IsInSetup => _currentPhase == GamePhase.WordSelection ||
                                  _currentPhase == GamePhase.WordPlacement;
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            SetPhase(GamePhase.MainMenu);
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Transition to a new game phase
        /// </summary>
        public void SetPhase(GamePhase newPhase)
        {
            if (_currentPhase == newPhase)
                return;

            GamePhase previousPhase = _currentPhase;
            _currentPhase = newPhase;

            Debug.Log($"[GameStateMachine] Phase changed: {previousPhase} → {newPhase}");

            HandlePhaseTransition(previousPhase, newPhase);
        }

        /// <summary>
        /// Start a new game (called from Main Menu)
        /// </summary>
        [Button("Start New Game", ButtonSizes.Large)]
        public void StartNewGame()
        {
            Debug.Log("[GameStateMachine] Starting new game...");
            SetPhase(GamePhase.DifficultySelection);
            _onGameStart?.Raise();
        }

        /// <summary>
        /// Move from difficulty selection to word selection
        /// </summary>
        public void OnDifficultySelected()
        {
            SetPhase(GamePhase.WordSelection);
        }

        /// <summary>
        /// Move from word selection to word placement
        /// </summary>
        public void OnWordsSelected()
        {
            SetPhase(GamePhase.WordPlacement);
        }

        /// <summary>
        /// Complete setup and begin gameplay (called by TestController)
        /// REMOVED [Button] - use TestController Step 3 instead
        /// </summary>
        public void CompleteSetup()
        {
            Debug.Log("[GameStateMachine] Setup complete, starting gameplay...");
            SetPhase(GamePhase.GameplayActive);
            _onSetupComplete?.Raise();
            _onGameplayStart?.Raise();

            // Start the first turn automatically
            if (_turnManager != null)
            {
                _turnManager.StartFirstTurn();
                Debug.Log("[GameStateMachine] First turn started");
            }
        }

        /// <summary>
        /// End the game and transition to game over
        /// </summary>
        public void EndGame(string winnerName)
        {
            Debug.Log($"[GameStateMachine] Game Over! Winner: {winnerName}");
            SetPhase(GamePhase.GameOver);
            _onGameOver?.Raise();
        }

        /// <summary>
        /// Return to main menu
        /// </summary>
        public void ReturnToMainMenu()
        {
            SetPhase(GamePhase.MainMenu);
        }
        #endregion

        #region Private Methods
        private void HandlePhaseTransition(GamePhase from, GamePhase to)
        {
            // Exit previous phase
            switch (from)
            {
                case GamePhase.GameplayActive:
                    Debug.Log("[GameStateMachine] Exiting gameplay phase");
                    break;
            }

            // Enter new phase
            switch (to)
            {
                case GamePhase.MainMenu:
                    Debug.Log("[GameStateMachine] Entered Main Menu");
                    break;
                case GamePhase.DifficultySelection:
                    Debug.Log("[GameStateMachine] Entered Difficulty Selection");
                    break;
                case GamePhase.WordSelection:
                    Debug.Log("[GameStateMachine] Entered Word Selection");
                    break;
                case GamePhase.WordPlacement:
                    Debug.Log("[GameStateMachine] Entered Word Placement");
                    break;
                case GamePhase.GameplayActive:
                    Debug.Log("[GameStateMachine] Entered Active Gameplay");
                    break;
                case GamePhase.GameOver:
                    Debug.Log("[GameStateMachine] Entered Game Over");
                    break;
            }
        }
        #endregion
    }
}
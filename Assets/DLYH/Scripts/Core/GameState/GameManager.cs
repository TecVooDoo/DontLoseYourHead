using UnityEngine;
using Sirenix.OdinInspector;
using TecVooDoo.DontLoseYourHead.Core.GameState;

namespace TecVooDoo.DontLoseYourHead.Core
{
    public class GameManager : MonoBehaviour
    {
        [Title("Dependencies")]
        [Required]
        [SerializeField] private DifficultySO _difficulty;

        [Required]
        [SerializeField] private IntVariableSO _missCount;

        [Required]
        [SerializeField] private TurnManager _turnManager;

        [Required]
        [SerializeField] private GameStateMachine _stateMachine;

        [Required]
        [SerializeField] private PlayerManager _playerManager;

        [Title("Game State")]
        [ReadOnly]
        [ShowInInspector]
        private Grid _playerGrid;

        [ReadOnly]
        [ShowInInspector]
        private Grid _opponentGrid;

        public Grid PlayerGrid => _playerGrid;
        public Grid OpponentGrid => _opponentGrid;
        public int CurrentPlayerIndex => _turnManager.CurrentPlayerIndex;
        public int MaxMisses => _difficulty.MissLimit;

        private void Awake()
        {
            InitializeGame();
        }

        [Button("Initialize Game")]
        private void InitializeGame()
        {
            _playerGrid = new Grid(_difficulty.GridSize);
            _opponentGrid = new Grid(_difficulty.GridSize);
            _missCount.Value = 0;

            Debug.Log($"Game initialized with {_difficulty.DifficultyName} difficulty: {_difficulty.GridSize}x{_difficulty.GridSize} grid, {_difficulty.MissLimit} miss limit");
        }

        /// <summary>
        /// Process a letter guess for the specified player against target grid
        /// </summary>
        /// <param name="playerIndex">Index of player making the guess (0 or 1)</param>
        /// <param name="targetGrid">Grid being guessed against</param>
        /// <param name="letter">Letter being guessed</param>
        /// <returns>True if letter was found, false if miss</returns>
        public bool ProcessLetterGuess(int playerIndex, Grid targetGrid, char letter)
        {
            // Check if game is in active gameplay phase
            if (!_stateMachine.IsInGameplay)
            {
                Debug.LogWarning($"[GameManager] Cannot process guess - game is not in gameplay phase!");
                return false;
            }

            // Validate it's this player's turn
            if (!_turnManager.CanTakeAction(playerIndex))
            {
                Debug.LogWarning($"[GameManager] Player {playerIndex} cannot guess - not their turn!");
                return false;
            }

            bool foundLetter = false;

            foreach (var word in targetGrid.PlacedWords)
            {
                if (word.Text.Contains(letter))
                {
                    foundLetter = true;
                    break;
                }
            }

            if (!foundLetter)
            {
                _missCount.Add(1);
                Debug.Log($"[GameManager] Player {playerIndex} guessed '{letter}' - MISS! ({_missCount.Value}/{MaxMisses})");
            }
            else
            {
                Debug.Log($"[GameManager] Player {playerIndex} guessed '{letter}' - HIT!");
            }

            // Check for lose condition
            if (CheckLoseCondition())
            {
                HandleGameOver(GetOpponentIndex(playerIndex));
                return foundLetter;
            }

            // Check for win condition
            if (CheckWinCondition(targetGrid))
            {
                HandleGameOver(playerIndex);
                return foundLetter;
            }

            // End turn after processing guess
            _turnManager.EndTurn();

            return foundLetter;
        }

        /// <summary>
        /// Process a coordinate guess for the specified player against target grid
        /// </summary>
        /// <param name="playerIndex">Index of player making the guess (0 or 1)</param>
        /// <param name="targetGrid">Grid being guessed against</param>
        /// <param name="coordinate">Coordinate being guessed</param>
        /// <returns>True if hit a letter, false if miss</returns>
        public bool ProcessCoordinateGuess(int playerIndex, Grid targetGrid, Vector2Int coordinate)
        {
            // Check if game is in active gameplay phase
            if (!_stateMachine.IsInGameplay)
            {
                Debug.LogWarning($"[GameManager] Cannot process guess - game is not in gameplay phase!");
                return false;
            }

            // Validate it's this player's turn
            if (!_turnManager.CanTakeAction(playerIndex))
            {
                Debug.LogWarning($"[GameManager] Player {playerIndex} cannot guess - not their turn!");
                return false;
            }

            GridCell cell = targetGrid.GetCell(coordinate);

            if (cell == null)
            {
                Debug.LogWarning($"[GameManager] Invalid coordinate: {coordinate}");
                return false;
            }

            bool isHit = false;

            if (cell.IsEmpty)
            {
                cell.SetState(CellState.Miss);
                _missCount.Add(1);
                Debug.Log($"[GameManager] Player {playerIndex} guessed {coordinate} - MISS! ({_missCount.Value}/{MaxMisses})");
            }
            else
            {
                cell.SetState(CellState.PartiallyKnown);
                isHit = true;
                Debug.Log($"[GameManager] Player {playerIndex} guessed {coordinate} - HIT!");
            }

            // Check for lose condition
            if (CheckLoseCondition())
            {
                HandleGameOver(GetOpponentIndex(playerIndex));
                return isHit;
            }

            // Check for win condition
            if (CheckWinCondition(targetGrid))
            {
                HandleGameOver(playerIndex);
                return isHit;
            }

            // End turn after processing guess
            _turnManager.EndTurn();

            return isHit;
        }

        public bool CheckWinCondition(Grid targetGrid)
        {
            foreach (var word in targetGrid.PlacedWords)
            {
                word.CheckIfFullyRevealed();

                if (!word.IsFullyRevealed)
                {
                    return false;
                }
            }

            return targetGrid.PlacedWords.Count > 0;
        }

        public bool CheckLoseCondition()
        {
            return _missCount.Value >= _difficulty.MissLimit;
        }

        private void HandleGameOver(int winnerIndex)
        {
            string winnerName = _playerManager.GetPlayerName(winnerIndex);
            Debug.Log($"[GameManager] Game Over! Winner: {winnerName}");
            _stateMachine.EndGame(winnerName);
        }

        private int GetOpponentIndex(int playerIndex)
        {
            return playerIndex == 0 ? 1 : 0;
        }
    }
}
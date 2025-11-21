using UnityEngine;
using Sirenix.OdinInspector;

namespace TecVooDoo.DontLoseYourHead.Core
{
    public class GameManager : MonoBehaviour
    {
        [Title("Dependencies")]
        [Required]
        [SerializeField] private DifficultySO _difficulty;

        [Required]
        [SerializeField] private IntVariableSO _missCount;

        [Title("Game State")]
        [ReadOnly]
        [ShowInInspector]
        private Grid _playerGrid;

        [ReadOnly]
        [ShowInInspector]
        private Grid _opponentGrid;

        [ReadOnly]
        [ShowInInspector]
        private bool _isPlayerTurn = true;

        public Grid PlayerGrid => _playerGrid;
        public Grid OpponentGrid => _opponentGrid;
        public bool IsPlayerTurn => _isPlayerTurn;
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
            _isPlayerTurn = true;

            Debug.Log($"Game initialized with {_difficulty.DifficultyName} difficulty: {_difficulty.GridSize}x{_difficulty.GridSize} grid, {_difficulty.MissLimit} miss limit");
        }

        public bool ProcessLetterGuess(Grid targetGrid, char letter)
        {
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
            }

            return foundLetter;
        }

        public bool ProcessCoordinateGuess(Grid targetGrid, Vector2Int coordinate)
        {
            GridCell cell = targetGrid.GetCell(coordinate);

            if (cell == null)
            {
                return false;
            }

            if (cell.IsEmpty)
            {
                cell.SetState(CellState.Miss);
                _missCount.Add(1);
                return false;
            }

            cell.SetState(CellState.PartiallyKnown);
            return true;
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

        public void SwitchTurn()
        {
            _isPlayerTurn = !_isPlayerTurn;
        }
    }
}
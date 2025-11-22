using UnityEngine;
using Sirenix.OdinInspector;
using TecVooDoo.DontLoseYourHead.Core.GameState;

namespace TecVooDoo.DontLoseYourHead.Core
{
    /// <summary>
    /// Manages both players and coordinates player-related operations
    /// </summary>
    public class PlayerManager : MonoBehaviour
    {
        #region Serialized Fields
        [Title("Player References")]
        [Required]
        [SerializeField] private PlayerSO _player1;

        [Required]
        [SerializeField] private PlayerSO _player2;

        [Title("Dependencies")]
        [Required]
        [SerializeField] private TurnManager _turnManager;

        [Required]
        [SerializeField] private DifficultySO _difficulty;
        #endregion

        #region Properties
        public PlayerSO Player1 => _player1;
        public PlayerSO Player2 => _player2;

        /// <summary>
        /// Get the player whose turn it currently is
        /// </summary>
        public PlayerSO CurrentPlayer => _turnManager.CurrentPlayerIndex == 0 ? _player1 : _player2;

        /// <summary>
        /// Get the opponent of the current player
        /// </summary>
        public PlayerSO OpponentPlayer => _turnManager.CurrentPlayerIndex == 0 ? _player2 : _player1;

        /// <summary>
        /// Get player by index (0 or 1)
        /// </summary>
        public PlayerSO GetPlayer(int index)
        {
            return index == 0 ? _player1 : _player2;
        }

        /// <summary>
        /// Get the opponent of a specific player
        /// </summary>
        public PlayerSO GetOpponent(PlayerSO player)
        {
            return player == _player1 ? _player2 : _player1;
        }

        /// <summary>
        /// Get the opponent of a player by index
        /// </summary>
        public PlayerSO GetOpponent(int playerIndex)
        {
            return playerIndex == 0 ? _player2 : _player1;
        }
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            ValidateDependencies();
        }

        private void Start()
        {
            InitializePlayers();
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Initialize both players with grids based on difficulty
        /// </summary>
        [Button("Initialize Players")]
        public void InitializePlayers()
        {
            if (_difficulty == null)
            {
                Debug.LogError("[PlayerManager] Cannot initialize - Difficulty not set!");
                return;
            }

            _player1.Initialize(_difficulty.GridSize);
            _player2.Initialize(_difficulty.GridSize);

            Debug.Log($"[PlayerManager] Both players initialized with {_difficulty.GridSize}x{_difficulty.GridSize} grids");
        }

        /// <summary>
        /// Reset both players to initial state
        /// </summary>
        [Button("Reset All Players")]
        public void ResetPlayers()
        {
            _player1.ResetState();
            _player2.ResetState();

            Debug.Log("[PlayerManager] All players reset");
        }

        /// <summary>
        /// Check if current player has won (found all opponent's words)
        /// </summary>
        public bool HasCurrentPlayerWon()
        {
            return CurrentPlayer.HasFoundAllWords(OpponentPlayer.Grid);
        }

        /// <summary>
        /// Check if current player has lost (exceeded miss limit)
        /// </summary>
        public bool HasCurrentPlayerLost()
        {
            return CurrentPlayer.MissCount >= _difficulty.MissLimit;
        }

        /// <summary>
        /// Process a letter guess from current player against opponent
        /// </summary>
        public bool ProcessLetterGuess(char letter)
        {
            // Check if already known
            if (CurrentPlayer.IsLetterKnown(letter))
            {
                Debug.LogWarning($"[PlayerManager] {CurrentPlayer.PlayerName} already knows letter '{letter}'");
                return false;
            }

            // Check opponent's grid for this letter
            bool foundLetter = false;
            Grid opponentGrid = OpponentPlayer.Grid;

            foreach (var word in opponentGrid.PlacedWords)
            {
                if (word.Text.Contains(letter))
                {
                    foundLetter = true;
                    CurrentPlayer.AddKnownLetter(letter);

                    // Update any revealed cells that match this letter
                    UpdateRevealedCellsForLetter(opponentGrid, letter);
                    break;
                }
            }

            if (!foundLetter)
            {
                // Increment current player's miss count
                CurrentPlayer.MissCountVariable.Add(1);
                Debug.Log($"[PlayerManager] {CurrentPlayer.PlayerName} guessed '{letter}' - MISS ({CurrentPlayer.MissCount}/{_difficulty.MissLimit})");
            }
            else
            {
                Debug.Log($"[PlayerManager] {CurrentPlayer.PlayerName} guessed '{letter}' - HIT!");
            }

            return foundLetter;
        }

        /// <summary>
        /// Process a coordinate guess from current player against opponent
        /// </summary>
        public bool ProcessCoordinateGuess(Vector2Int coordinate)
        {
            Grid opponentGrid = OpponentPlayer.Grid;
            GridCell cell = opponentGrid.GetCell(coordinate);

            if (cell == null)
            {
                Debug.LogWarning($"[PlayerManager] Invalid coordinate: {coordinate}");
                return false;
            }

            if (cell.IsEmpty)
            {
                cell.SetState(CellState.Miss);
                CurrentPlayer.MissCountVariable.Add(1);
                Debug.Log($"[PlayerManager] {CurrentPlayer.PlayerName} guessed {coordinate} - MISS ({CurrentPlayer.MissCount}/{_difficulty.MissLimit})");
                return false;
            }

            // Hit a letter
            cell.SetState(CellState.PartiallyKnown);

            // If this letter was already known, reveal it
            if (cell.Letter.HasValue && CurrentPlayer.IsLetterKnown(cell.Letter.Value))
            {
                cell.SetState(CellState.Revealed);
            }

            Debug.Log($"[PlayerManager] {CurrentPlayer.PlayerName} guessed {coordinate} - HIT!");
            return true;
        }
        #endregion

        #region Private Methods
        private void ValidateDependencies()
        {
            if (_player1 == null)
            {
                Debug.LogError("[PlayerManager] Player 1 not assigned!");
            }

            if (_player2 == null)
            {
                Debug.LogError("[PlayerManager] Player 2 not assigned!");
            }

            if (_turnManager == null)
            {
                Debug.LogError("[PlayerManager] TurnManager not assigned!");
            }

            if (_difficulty == null)
            {
                Debug.LogError("[PlayerManager] Difficulty not assigned!");
            }
        }

        private void UpdateRevealedCellsForLetter(Grid grid, char letter)
        {
            // Update any cells that were previously revealed as * to show the actual letter
            foreach (var word in grid.PlacedWords)
            {
                foreach (var cell in word.OccupiedCells)
                {
                    if (cell.Letter == letter && cell.State == CellState.PartiallyKnown)
                    {
                        cell.SetState(CellState.Revealed);
                    }
                }
            }
        }
        #endregion
    }
}
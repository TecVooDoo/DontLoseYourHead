using UnityEngine;
using Sirenix.OdinInspector;
using TecVooDoo.DontLoseYourHead.Core.GameState;
using System.Linq;

namespace TecVooDoo.DontLoseYourHead.Core
{
    public class GameManager : MonoBehaviour
    {
        [Title("Dependencies")]
        [Required]
        [SerializeField] private DifficultySO _difficulty;

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

            Debug.Log($"Game initialized with {_difficulty.DifficultyName} difficulty: {_difficulty.GridSize}x{_difficulty.GridSize} grid, {_difficulty.MissLimit} miss limit");
        }

        #region Letter Guessing

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

            PlayerSO currentPlayer = _playerManager.GetPlayer(playerIndex);

            if (!foundLetter)
            {
                currentPlayer.MissCountVariable.Add(1);
                Debug.Log($"[GameManager] Player {playerIndex} guessed '{letter}' - MISS! ({currentPlayer.MissCount}/{MaxMisses})");
            }
            else
            {
                Debug.Log($"[GameManager] Player {playerIndex} guessed '{letter}' - HIT!");
            }

            // Check for lose condition
            if (CheckLoseCondition(playerIndex))
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

        #endregion

        #region Coordinate Guessing

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
            PlayerSO currentPlayer = _playerManager.GetPlayer(playerIndex);

            if (cell.IsEmpty)
            {
                cell.SetState(CellState.Miss);
                currentPlayer.MissCountVariable.Add(1);
                Debug.Log($"[GameManager] Player {playerIndex} guessed {coordinate} - MISS! ({currentPlayer.MissCount}/{MaxMisses})");
            }
            else
            {
                cell.SetState(CellState.PartiallyKnown);
                isHit = true;
                Debug.Log($"[GameManager] Player {playerIndex} guessed {coordinate} - HIT!");
            }

            // Check for lose condition
            if (CheckLoseCondition(playerIndex))
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

        #endregion

        #region Word Guessing

        /// <summary>
        /// Process a complete word guess for the specified player against target grid
        /// </summary>
        /// <param name="playerIndex">Index of player making the guess (0 or 1)</param>
        /// <param name="targetGrid">Grid being guessed against</param>
        /// <param name="guessedWord">Word being guessed (case-insensitive)</param>
        /// <returns>True if word was found, false if incorrect</returns>
        public bool ProcessWordGuess(int playerIndex, Grid targetGrid, string guessedWord)
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

            // Validate input
            if (string.IsNullOrWhiteSpace(guessedWord))
            {
                Debug.LogWarning($"[GameManager] Invalid word guess - empty or whitespace!");
                return false;
            }

            // Convert to uppercase for comparison (case-insensitive)
            string normalizedGuess = guessedWord.Trim().ToUpper();

            // Check if the word matches any word in the target grid
            Word matchedWord = targetGrid.PlacedWords.FirstOrDefault(w => w.Text.ToUpper() == normalizedGuess);

            PlayerSO currentPlayer = _playerManager.GetPlayer(playerIndex);

            if (matchedWord != null)
            {
                // Correct word guess - reveal all letters of this word
                RevealWord(matchedWord);
                Debug.Log($"[GameManager] Player {playerIndex} guessed word '{normalizedGuess}' - CORRECT!");

                // Check for win condition (all words revealed)
                if (CheckWinCondition(targetGrid))
                {
                    HandleGameOver(playerIndex);
                    return true;
                }

                // End turn after processing guess
                _turnManager.EndTurn();
                return true;
            }
            else
            {
                // Wrong word guess - double penalty (2 misses)
                currentPlayer.MissCountVariable.Add(2);
                Debug.Log($"[GameManager] Player {playerIndex} guessed word '{normalizedGuess}' - WRONG! +2 misses ({currentPlayer.MissCount}/{MaxMisses})");

                // Check for lose condition
                if (CheckLoseCondition(playerIndex))
                {
                    HandleGameOver(GetOpponentIndex(playerIndex));
                    return false;
                }

                // End turn after processing guess
                _turnManager.EndTurn();
                return false;
            }
        }

        /// <summary>
        /// Reveal all letters of a correctly guessed word
        /// </summary>
        private void RevealWord(Word word)
        {
            // Mark word as fully revealed
            word.MarkAsFullyRevealed();

            // Reveal all cells that belong to this word
            foreach (var cell in word.GetCells())
            {
                if (cell != null)
                {
                    cell.SetState(CellState.Revealed);
                }
            }

            Debug.Log($"[GameManager] Revealed word: {word.Text}");
        }

        #endregion

        #region Win/Lose Conditions

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

        public bool CheckLoseCondition(int playerIndex)
        {
            PlayerSO player = _playerManager.GetPlayer(playerIndex);
            return player.MissCount >= _difficulty.MissLimit;
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

        #endregion
    }
}
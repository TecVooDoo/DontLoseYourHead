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
        public void InitializeGame()
        {
            _playerGrid = new Grid(_difficulty.GridSize);
            _opponentGrid = new Grid(_difficulty.GridSize);

            Debug.Log(string.Format("Game initialized with {0} difficulty: {1}x{1} grid, {2} miss limit", 
                _difficulty.DifficultyName, _difficulty.GridSize, _difficulty.MissLimit));
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
                Debug.LogWarning("[GameManager] Cannot process guess - game is not in gameplay phase!");
                return false;
            }

            // Validate it is this player's turn
            if (!_turnManager.CanTakeAction(playerIndex))
            {
                Debug.LogWarning(string.Format("[GameManager] Player {0} cannot guess - not their turn!", playerIndex));
                return false;
            }

            // Normalize to uppercase
            letter = char.ToUpper(letter);

            bool foundLetter = false;

            foreach (var word in targetGrid.PlacedWords)
            {
                if (word.Text.ToUpper().Contains(letter))
                {
                    foundLetter = true;
                    break;
                }
            }

            PlayerSO currentPlayer = _playerManager.GetPlayer(playerIndex);

            if (!foundLetter)
            {
                currentPlayer.MissCountVariable.Add(1);
                Debug.Log(string.Format("[GameManager] Player {0} guessed '{1}' - MISS! ({2}/{3})", 
                    playerIndex, letter, currentPlayer.MissCount, MaxMisses));
            }
            else
            {
                // Add letter to known letters
                currentPlayer.AddKnownLetter(letter);
                
                // Update any PartiallyKnown cells that contain this letter to Revealed
                UpdateRevealedCellsForLetter(targetGrid, letter, currentPlayer);
                
                Debug.Log(string.Format("[GameManager] Player {0} guessed '{1}' - HIT!", playerIndex, letter));
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

        /// <summary>
        /// Update all PartiallyKnown cells containing the specified letter to Revealed
        /// </summary>
        private void UpdateRevealedCellsForLetter(Grid targetGrid, char letter, PlayerSO player)
        {
            int revealedCount = 0;
            
            for (int x = 0; x < targetGrid.Size; x++)
            {
                for (int y = 0; y < targetGrid.Size; y++)
                {
                    GridCell cell = targetGrid.GetCell(new Vector2Int(x, y));
                    
                    if (cell != null && 
                        cell.State == CellState.PartiallyKnown && 
                        cell.Letter.HasValue && 
                        char.ToUpper(cell.Letter.Value) == letter)
                    {
                        cell.SetState(CellState.Revealed);
                        revealedCount++;
                    }
                }
            }
            
            if (revealedCount > 0)
            {
                Debug.Log(string.Format("[GameManager] Updated {0} cell(s) from * to '{1}'", revealedCount, letter));
            }
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
                Debug.LogWarning("[GameManager] Cannot process guess - game is not in gameplay phase!");
                return false;
            }

            // Validate it is this player's turn
            if (!_turnManager.CanTakeAction(playerIndex))
            {
                Debug.LogWarning(string.Format("[GameManager] Player {0} cannot guess - not their turn!", playerIndex));
                return false;
            }

            GridCell cell = targetGrid.GetCell(coordinate);

            if (cell == null)
            {
                Debug.LogWarning(string.Format("[GameManager] Invalid coordinate: {0}", coordinate));
                return false;
            }

            bool isHit = false;
            PlayerSO currentPlayer = _playerManager.GetPlayer(playerIndex);

            if (cell.IsEmpty)
            {
                cell.SetState(CellState.Miss);
                currentPlayer.MissCountVariable.Add(1);
                Debug.Log(string.Format("[GameManager] Player {0} guessed {1} - MISS! ({2}/{3})", 
                    playerIndex, coordinate, currentPlayer.MissCount, MaxMisses));
            }
            else
            {
                isHit = true;
                char cellLetter = char.ToUpper(cell.Letter.Value);
                
                // Check if player already knows this letter
                if (currentPlayer.IsLetterKnown(cellLetter))
                {
                    // Player knows the letter - reveal it fully
                    cell.SetState(CellState.Revealed);
                    Debug.Log(string.Format("[GameManager] Player {0} guessed {1} - HIT! Letter '{2}' (already known)", 
                        playerIndex, coordinate, cellLetter));
                }
                else
                {
                    // Player does not know this letter yet - show as *
                    cell.SetState(CellState.PartiallyKnown);
                    Debug.Log(string.Format("[GameManager] Player {0} guessed {1} - HIT! (letter hidden as *)", 
                        playerIndex, coordinate));
                }
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
                Debug.LogWarning("[GameManager] Cannot process guess - game is not in gameplay phase!");
                return false;
            }

            // Validate it is this player's turn
            if (!_turnManager.CanTakeAction(playerIndex))
            {
                Debug.LogWarning(string.Format("[GameManager] Player {0} cannot guess - not their turn!", playerIndex));
                return false;
            }

            // Validate input
            if (string.IsNullOrWhiteSpace(guessedWord))
            {
                Debug.LogWarning("[GameManager] Invalid word guess - empty or whitespace!");
                return false;
            }

            // Convert to uppercase for comparison (case-insensitive)
            string normalizedGuess = guessedWord.Trim().ToUpper();

            // Check if the word matches any word in the target grid
            Word matchedWord = targetGrid.PlacedWords.FirstOrDefault(w => w.Text.ToUpper() == normalizedGuess);

            PlayerSO currentPlayer = _playerManager.GetPlayer(playerIndex);

            if (matchedWord != null)
            {
                // Correct word guess - add all letters to known letters and reveal
                AddWordLettersToKnown(currentPlayer, matchedWord);
                RevealWord(matchedWord, targetGrid, currentPlayer);
                currentPlayer.AddFoundWord(matchedWord);
                
                Debug.Log(string.Format("[GameManager] Player {0} guessed word '{1}' - CORRECT!", playerIndex, normalizedGuess));

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
                currentPlayer.AddGuessedWord(normalizedGuess);
                
                Debug.Log(string.Format("[GameManager] Player {0} guessed word '{1}' - WRONG! +2 misses ({2}/{3})", 
                    playerIndex, normalizedGuess, currentPlayer.MissCount, MaxMisses));

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
        /// Add all letters from a word to the player's known letters
        /// </summary>
        private void AddWordLettersToKnown(PlayerSO player, Word word)
        {
            foreach (char c in word.Text.ToUpper())
            {
                player.AddKnownLetter(c);
            }
        }

        /// <summary>
        /// Reveal all letters of a correctly guessed word and update other cells
        /// </summary>
        private void RevealWord(Word word, Grid targetGrid, PlayerSO player)
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

            // Now update any OTHER PartiallyKnown cells that contain letters from this word
            foreach (char c in word.Text.ToUpper())
            {
                UpdateRevealedCellsForLetter(targetGrid, c, player);
            }

            Debug.Log(string.Format("[GameManager] Revealed word: {0}", word.Text));
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
            Debug.Log(string.Format("[GameManager] Game Over! Winner: {0}", winnerName));
            _stateMachine.EndGame(winnerName);
        }

        private int GetOpponentIndex(int playerIndex)
        {
            return playerIndex == 0 ? 1 : 0;
        }

        #endregion

        #region Debug Helpers

        /// <summary>
        /// Get display character for a cell (for testing/debugging)
        /// </summary>
        public char GetCellDisplayCharacter(GridCell cell, PlayerSO viewer)
        {
            if (cell == null) return ' ';
            
            switch (cell.State)
            {
                case CellState.Hidden:
                    return '.';
                case CellState.Miss:
                    return 'X';
                case CellState.PartiallyKnown:
                    // Show * unless viewer knows the letter
                    if (cell.Letter.HasValue && viewer.IsLetterKnown(cell.Letter.Value))
                    {
                        return char.ToUpper(cell.Letter.Value);
                    }
                    return '*';
                case CellState.Revealed:
                    return cell.Letter.HasValue ? char.ToUpper(cell.Letter.Value) : '?';
                default:
                    return '?';
            }
        }

        /// <summary>
        /// Print grid state to console (for debugging)
        /// </summary>
        [Button("Debug: Print Opponent Grid")]
        public void DebugPrintOpponentGrid()
        {
            if (_opponentGrid == null)
            {
                Debug.Log("[Debug] Opponent grid not initialized");
                return;
            }

            PlayerSO player = _playerManager.GetPlayer(0);

            // Build header
            string header = "    ";
            for (int x = 0; x < _opponentGrid.Size; x++)
            {
                header += x.ToString() + " ";
            }

            // Build grid visualization
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            sb.AppendLine("=== Opponent Grid State ===");
            sb.AppendLine(string.Format("Known Letters: [{0}]",
                player.KnownLetters.Count > 0 ? string.Join(", ", player.KnownLetters) : "none"));
            sb.AppendLine(header);
            sb.AppendLine("   " + new string('-', _opponentGrid.Size * 2 + 1));

            for (int y = 0; y < _opponentGrid.Size; y++)
            {
                string row = string.Format("{0,2} |", y);
                for (int x = 0; x < _opponentGrid.Size; x++)
                {
                    GridCell cell = _opponentGrid.GetCell(new Vector2Int(x, y));
                    char displayChar = GetCellDisplayCharacter(cell, player);
                    row += displayChar + "|";
                }
                sb.AppendLine(row);
            }

            sb.AppendLine("   " + new string('-', _opponentGrid.Size * 2 + 1));
            sb.AppendLine("Legend: [.]=hidden  [*]=hit(unknown)  [X]=miss  [A-Z]=revealed");

            Debug.Log(sb.ToString());
        }

        #endregion
    }
}

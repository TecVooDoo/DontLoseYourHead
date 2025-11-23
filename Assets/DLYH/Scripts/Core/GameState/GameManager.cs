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

        [Title("Word Guess Testing")]
        [SerializeField] private string _testWordGuess = "CAT";

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
                _missCount.Add(2);
                Debug.Log($"[GameManager] Player {playerIndex} guessed word '{normalizedGuess}' - WRONG! +2 misses ({_missCount.Value}/{MaxMisses})");

                // Check for lose condition
                if (CheckLoseCondition())
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

        #endregion

        #region Inspector Testing Buttons

        [Title("Testing - Word Guessing")]
        [Button("Test Correct Word Guess", ButtonSizes.Large)]
        [GUIColor(0.3f, 1f, 0.3f)]
        private void TestCorrectWordGuess()
        {
            if (_opponentGrid == null || _opponentGrid.PlacedWords.Count == 0)
            {
                Debug.LogError("[Test] No words placed on opponent grid! Place words first.");
                return;
            }

            // Get the first word from the opponent grid
            Word firstWord = _opponentGrid.PlacedWords[0];
            _testWordGuess = firstWord.Text;

            Debug.Log($"[Test] Testing CORRECT word guess: '{_testWordGuess}'");
            bool result = ProcessWordGuess(CurrentPlayerIndex, _opponentGrid, _testWordGuess);
            Debug.Log($"[Test] Result: {(result ? "SUCCESS" : "FAILED")}");
        }

        [Button("Test Wrong Word Guess", ButtonSizes.Large)]
        [GUIColor(1f, 0.3f, 0.3f)]
        private void TestWrongWordGuess()
        {
            string wrongWord = "ZZZZZ"; // Guaranteed to be wrong
            Debug.Log($"[Test] Testing WRONG word guess: '{wrongWord}' (should add 2 misses)");

            int missesBefore = _missCount.Value;
            bool result = ProcessWordGuess(CurrentPlayerIndex, _opponentGrid, wrongWord);
            int missesAfter = _missCount.Value;

            Debug.Log($"[Test] Result: {(result ? "UNEXPECTED SUCCESS" : "CORRECTLY FAILED")}");
            Debug.Log($"[Test] Misses: {missesBefore} -> {missesAfter} (expected +2)");
        }

        [Button("Test Custom Word Guess")]
        private void TestCustomWordGuess()
        {
            if (string.IsNullOrWhiteSpace(_testWordGuess))
            {
                Debug.LogError("[Test] Enter a word in 'Test Word Guess' field!");
                return;
            }

            Debug.Log($"[Test] Testing custom word guess: '{_testWordGuess}'");
            bool result = ProcessWordGuess(CurrentPlayerIndex, _opponentGrid, _testWordGuess);
            Debug.Log($"[Test] Result: {(result ? "CORRECT" : "WRONG")}");
        }

        [Button("Show Opponent Words")]
        private void ShowOpponentWords()
        {
            if (_opponentGrid == null || _opponentGrid.PlacedWords.Count == 0)
            {
                Debug.LogWarning("[Test] No words placed on opponent grid!");
                return;
            }

            Debug.Log("[Test] Opponent's words:");
            for (int i = 0; i < _opponentGrid.PlacedWords.Count; i++)
            {
                Word word = _opponentGrid.PlacedWords[i];
                Debug.Log($"  {i + 1}. {word.Text} - Revealed: {word.IsFullyRevealed}");
            }
        }

        #endregion
    }
}
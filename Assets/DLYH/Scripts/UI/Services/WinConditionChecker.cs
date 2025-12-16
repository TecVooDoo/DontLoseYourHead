using UnityEngine;
using System.Collections.Generic;

namespace TecVooDoo.DontLoseYourHead.UI
{
    /// <summary>
    /// Checks win/lose conditions for gameplay.
    /// Extracted from GameplayUIController to reduce file size.
    ///
    /// Win conditions:
    /// - All letters in opponent's words are known AND all grid positions for those words have been guessed
    /// - OR opponent exceeds their miss limit
    ///
    /// Lose condition:
    /// - You exceed your miss limit
    /// </summary>
    public class WinConditionChecker
    {
        #region Dependencies

        private readonly GameplayStateTracker _stateTracker;

        #endregion

        #region Constructor

        public WinConditionChecker(GameplayStateTracker stateTracker)
        {
            _stateTracker = stateTracker;
        }

        #endregion

        #region Player Win Condition

        /// <summary>
        /// Check if player has won by revealing all letters AND all grid positions for all opponent words.
        /// Win condition: All letters in all words known AND all grid positions guessed.
        /// </summary>
        /// <param name="opponentWords">List of opponent's word placements</param>
        /// <returns>True if player has won</returns>
        public bool CheckPlayerWinCondition(List<WordPlacementData> opponentWords)
        {
            if (_stateTracker.GameOver || opponentWords == null || opponentWords.Count == 0)
                return false;

            // Check ALL words - must have all letters known AND all positions guessed
            foreach (WordPlacementData wordData in opponentWords)
            {
                // Check all letters in this word are known
                foreach (char letter in wordData.Word)
                {
                    if (!_stateTracker.PlayerKnownLetters.Contains(char.ToUpper(letter)))
                    {
                        return false; // Letter not known yet
                    }
                }

                // Check all positions for this word are guessed
                for (int i = 0; i < wordData.Word.Length; i++)
                {
                    int col = wordData.StartCol + (i * wordData.DirCol);
                    int row = wordData.StartRow + (i * wordData.DirRow);
                    Vector2Int coord = new Vector2Int(col, row);

                    if (!_stateTracker.PlayerGuessedCoordinates.Contains(coord))
                    {
                        return false; // Position not guessed yet
                    }
                }
            }

            // All letters AND all positions revealed - player wins!
            Debug.Log("[WinConditionChecker] === PLAYER WINS! All letters AND positions revealed! ===");
            return true;
        }

        /// <summary>
        /// Check if player has lost by exceeding miss limit
        /// </summary>
        public bool CheckPlayerLoseCondition()
        {
            if (_stateTracker.HasPlayerExceededMissLimit())
            {
                Debug.Log("[WinConditionChecker] === PLAYER LOSES! Opponent wins! ===");
                return true;
            }
            return false;
        }

        #endregion

        #region Opponent Win Condition

        /// <summary>
        /// Check if opponent (AI) has won by revealing all letters AND all grid positions for all player words.
        /// Win condition: All letters in all words known AND all grid positions guessed.
        /// </summary>
        /// <param name="playerWords">List of player's word placements</param>
        /// <returns>True if opponent has won</returns>
        public bool CheckOpponentWinCondition(List<WordPlacementData> playerWords)
        {
            if (_stateTracker.GameOver || playerWords == null || playerWords.Count == 0)
                return false;

            bool allLettersRevealed = true;
            bool allPositionsRevealed = true;

            foreach (WordPlacementData wordData in playerWords)
            {
                // Check all letters in this word are known by opponent
                foreach (char letter in wordData.Word)
                {
                    if (!_stateTracker.OpponentKnownLetters.Contains(char.ToUpper(letter)))
                    {
                        allLettersRevealed = false;
                        break;
                    }
                }

                if (!allLettersRevealed) break;

                // Check all positions for this word are guessed by opponent
                for (int i = 0; i < wordData.Word.Length; i++)
                {
                    int col = wordData.StartCol + (i * wordData.DirCol);
                    int row = wordData.StartRow + (i * wordData.DirRow);
                    Vector2Int coord = new Vector2Int(col, row);

                    if (!_stateTracker.OpponentGuessedCoordinates.Contains(coord))
                    {
                        allPositionsRevealed = false;
                        break;
                    }
                }

                if (!allPositionsRevealed) break;
            }

            if (allLettersRevealed && allPositionsRevealed)
            {
                Debug.Log("[WinConditionChecker] === OPPONENT WINS! All words and positions revealed! ===");
                return true;
            }

            return false;
        }

        /// <summary>
        /// Check if opponent has lost by exceeding miss limit
        /// </summary>
        public bool CheckOpponentLoseCondition()
        {
            if (_stateTracker.HasOpponentExceededMissLimit())
            {
                Debug.Log("[WinConditionChecker] === OPPONENT LOSES! Player wins! ===");
                return true;
            }
            return false;
        }

        #endregion

        #region Auto-Reveal Check

        /// <summary>
        /// Check if a word has been fully revealed through letter guessing.
        /// Used to auto-hide the Guess Word button.
        /// </summary>
        /// <param name="wordData">The word placement to check</param>
        /// <returns>True if all letters in the word are known</returns>
        public bool IsWordFullyRevealed(WordPlacementData wordData)
        {
            foreach (char letter in wordData.Word)
            {
                if (!_stateTracker.PlayerKnownLetters.Contains(char.ToUpper(letter)))
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Find all word rows that have been fully revealed via letter guessing
        /// but not yet marked as solved.
        /// </summary>
        /// <param name="opponentWords">List of opponent's word placements</param>
        /// <returns>List of row indices that are newly fully revealed</returns>
        public List<int> FindNewlyRevealedWordRows(List<WordPlacementData> opponentWords)
        {
            List<int> newlyRevealed = new List<int>();

            if (opponentWords == null) return newlyRevealed;

            for (int i = 0; i < opponentWords.Count; i++)
            {
                // Skip if already solved
                if (_stateTracker.PlayerSolvedWordRows.Contains(i))
                    continue;

                if (IsWordFullyRevealed(opponentWords[i]))
                {
                    newlyRevealed.Add(i);
                }
            }

            return newlyRevealed;
        }

        #endregion
    }
}

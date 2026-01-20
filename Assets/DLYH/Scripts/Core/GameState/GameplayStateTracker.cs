// GameplayStateTracker.cs
// Tracks gameplay state for both player and opponent
// Extracted from Services/GameplayStateTracker.cs during Phase 2 refactoring
// Updated: January 20, 2026 - Added RevealedCells for game state persistence
// Developer: TecVooDoo LLC

using System.Collections.Generic;
using UnityEngine;

namespace TecVooDoo.DontLoseYourHead.UI
{
    /// <summary>
    /// Data for a revealed cell, used for reconstructing grid state on resume.
    /// </summary>
    public struct RevealedCellInfo
    {
        public char Letter;     // The letter at this cell (or '\0' for miss)
        public bool IsHit;      // True if hit, false if miss

        public RevealedCellInfo(char letter, bool isHit)
        {
            Letter = letter;
            IsHit = isHit;
        }
    }

    /// <summary>
    /// Tracks gameplay state for network synchronization.
    /// Maintains state for both player and opponent.
    /// </summary>
    public class GameplayStateTracker
    {
        // Player state (tracking player's attacks on opponent's grid)
        public int PlayerMisses { get; set; }
        public int PlayerMissLimit { get; set; }
        public HashSet<char> PlayerKnownLetters { get; private set; }
        public HashSet<Vector2Int> PlayerGuessedCoordinates { get; private set; }
        public HashSet<int> PlayerSolvedWordRows { get; private set; }
        public Dictionary<Vector2Int, RevealedCellInfo> PlayerRevealedCells { get; private set; }

        // Opponent state (tracking opponent's attacks on player's grid)
        public int OpponentMisses { get; set; }
        public int OpponentMissLimit { get; set; }
        public HashSet<char> OpponentKnownLetters { get; private set; }
        public HashSet<Vector2Int> OpponentGuessedCoordinates { get; private set; }
        public HashSet<int> OpponentSolvedWordRows { get; private set; }
        public Dictionary<Vector2Int, RevealedCellInfo> OpponentRevealedCells { get; private set; }

        public GameplayStateTracker()
        {
            PlayerKnownLetters = new HashSet<char>();
            PlayerGuessedCoordinates = new HashSet<Vector2Int>();
            PlayerSolvedWordRows = new HashSet<int>();
            PlayerRevealedCells = new Dictionary<Vector2Int, RevealedCellInfo>();
            OpponentKnownLetters = new HashSet<char>();
            OpponentGuessedCoordinates = new HashSet<Vector2Int>();
            OpponentSolvedWordRows = new HashSet<int>();
            OpponentRevealedCells = new Dictionary<Vector2Int, RevealedCellInfo>();
        }

        /// <summary>
        /// Initializes player state with the given miss limit.
        /// </summary>
        public void InitializePlayerState(int missLimit)
        {
            PlayerMisses = 0;
            PlayerMissLimit = missLimit;
            PlayerKnownLetters.Clear();
            PlayerGuessedCoordinates.Clear();
            PlayerSolvedWordRows.Clear();
            PlayerRevealedCells.Clear();
        }

        /// <summary>
        /// Initializes opponent state with the given miss limit.
        /// </summary>
        public void InitializeOpponentState(int missLimit)
        {
            OpponentMisses = 0;
            OpponentMissLimit = missLimit;
            OpponentKnownLetters.Clear();
            OpponentGuessedCoordinates.Clear();
            OpponentSolvedWordRows.Clear();
            OpponentRevealedCells.Clear();
        }

        /// <summary>
        /// Records a cell reveal for the player (player attacking opponent's grid).
        /// </summary>
        /// <param name="position">Grid position (col, row)</param>
        /// <param name="letter">Letter revealed (or '\0' for miss)</param>
        /// <param name="isHit">True if hit, false if miss</param>
        public void RecordPlayerRevealedCell(Vector2Int position, char letter, bool isHit)
        {
            PlayerRevealedCells[position] = new RevealedCellInfo(letter, isHit);
        }

        /// <summary>
        /// Records a cell reveal for the opponent (opponent attacking player's grid).
        /// </summary>
        /// <param name="position">Grid position (col, row)</param>
        /// <param name="letter">Letter revealed (or '\0' for miss)</param>
        /// <param name="isHit">True if hit, false if miss</param>
        public void RecordOpponentRevealedCell(Vector2Int position, char letter, bool isHit)
        {
            OpponentRevealedCells[position] = new RevealedCellInfo(letter, isHit);
        }

        /// <summary>
        /// Adds misses to the opponent's count.
        /// </summary>
        /// <param name="count">Number of misses to add</param>
        public void AddOpponentMisses(int count)
        {
            OpponentMisses += count;
        }

        /// <summary>
        /// Calculates the miss limit based on difficulty, grid size, and word count.
        /// </summary>
        /// <param name="difficultyLevel">0 = Easy, 1 = Normal, 2 = Hard</param>
        /// <param name="gridSize">Size of the grid (6-12)</param>
        /// <param name="wordCount">Number of words (3-4)</param>
        /// <returns>Maximum allowed misses before losing</returns>
        public static int CalculateMissLimit(int difficultyLevel, int gridSize, int wordCount)
        {
            // Base formula: smaller grid = fewer misses allowed
            int baseMisses = gridSize switch
            {
                <= 6 => 4,
                <= 8 => 5,
                <= 10 => 6,
                _ => 7
            };

            // Adjust for word count (more words = more misses allowed)
            baseMisses += wordCount - 3;

            // Adjust for difficulty
            // Easy: +1, Normal: 0, Hard: -1
            baseMisses += (1 - difficultyLevel);

            // Clamp to reasonable range
            return Mathf.Clamp(baseMisses, 3, 10);
        }
    }
}

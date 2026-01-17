// GameplayStateTracker.cs
// Tracks gameplay state for both player and opponent
// Extracted from Services/GameplayStateTracker.cs during Phase 2 refactoring
// Developer: TecVooDoo LLC

using System.Collections.Generic;
using UnityEngine;

namespace TecVooDoo.DontLoseYourHead.UI
{
    /// <summary>
    /// Tracks gameplay state for network synchronization.
    /// Maintains state for both player and opponent.
    /// </summary>
    public class GameplayStateTracker
    {
        // Player state
        public int PlayerMisses { get; set; }
        public int PlayerMissLimit { get; set; }
        public HashSet<char> PlayerKnownLetters { get; private set; }
        public HashSet<Vector2Int> PlayerGuessedCoordinates { get; private set; }
        public HashSet<int> PlayerSolvedWordRows { get; private set; }

        // Opponent state
        public int OpponentMisses { get; set; }
        public int OpponentMissLimit { get; set; }
        public HashSet<char> OpponentKnownLetters { get; private set; }
        public HashSet<Vector2Int> OpponentGuessedCoordinates { get; private set; }
        public HashSet<int> OpponentSolvedWordRows { get; private set; }

        public GameplayStateTracker()
        {
            PlayerKnownLetters = new HashSet<char>();
            PlayerGuessedCoordinates = new HashSet<Vector2Int>();
            PlayerSolvedWordRows = new HashSet<int>();
            OpponentKnownLetters = new HashSet<char>();
            OpponentGuessedCoordinates = new HashSet<Vector2Int>();
            OpponentSolvedWordRows = new HashSet<int>();
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

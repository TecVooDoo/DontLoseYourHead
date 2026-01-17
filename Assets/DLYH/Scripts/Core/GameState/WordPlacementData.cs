// WordPlacementData.cs
// Data structure for word placement on the game grid
// Extracted from GuessProcessor.cs during Phase 2 refactoring
// Developer: TecVooDoo LLC

namespace TecVooDoo.DontLoseYourHead.UI
{
    /// <summary>
    /// Represents a word placed on the game grid.
    /// Contains the word, starting position, and direction.
    /// </summary>
    public class WordPlacementData
    {
        /// <summary>The word that was placed</summary>
        public string Word;

        /// <summary>Starting column (0-indexed)</summary>
        public int StartCol;

        /// <summary>Starting row (0-indexed)</summary>
        public int StartRow;

        /// <summary>Column direction: 1 for right, 0 for no horizontal movement</summary>
        public int DirCol;

        /// <summary>Row direction: 1 for down, 0 for no vertical movement</summary>
        public int DirRow;

        /// <summary>The row index (word slot) this word occupies</summary>
        public int RowIndex;
    }
}

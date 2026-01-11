// AISetupManager.cs
// Handles AI setup phase: word selection and grid placement
// Created: December 13, 2025
// Updated: December 13, 2025 - Use existing WordPlacementData from GuessProcessor
// Developer: TecVooDoo LLC

using System.Collections.Generic;
using UnityEngine;
using TecVooDoo.DontLoseYourHead.Core;
using TecVooDoo.DontLoseYourHead.UI;

namespace DLYH.AI.Core
{
    /// <summary>
    /// Manages the AI's setup phase - selecting words and placing them on the grid.
    /// 
    /// Usage:
    /// 1. Create instance with grid size and word count
    /// 2. Call SelectWords() with available word lists
    /// 3. Call PlaceWords() to get placement data
    /// 
    /// Uses existing WordPlacementData from TecVooDoo.DontLoseYourHead.UI for compatibility.
    /// </summary>
    public class AISetupManager
    {
        // ============================================================
        // CONFIGURATION
        // ============================================================

        private readonly int _gridSize;
        private readonly int _wordCount;
        private readonly int[] _wordLengths;

        // ============================================================
        // STATE
        // ============================================================

        private List<string> _selectedWords;
        private List<WordPlacementData> _placements;

        // ============================================================
        // CONSTRUCTOR
        // ============================================================

        /// <summary>
        /// Creates a new AISetupManager.
        /// </summary>
        /// <param name="gridSize">Size of the grid (6-12)</param>
        /// <param name="wordCount">Number of words to place (3 or 4)</param>
        /// <param name="wordLengths">Array of word lengths to use (e.g., [3, 4, 5, 6])</param>
        public AISetupManager(int gridSize, int wordCount, int[] wordLengths)
        {
            _gridSize = gridSize;
            _wordCount = wordCount;
            _wordLengths = wordLengths;
            _selectedWords = new List<string>();
            _placements = new List<WordPlacementData>();
        }

        // ============================================================
        // PROPERTIES
        // ============================================================

        /// <summary>Words selected by the AI</summary>
        public List<string> SelectedWords => _selectedWords;

        /// <summary>Placement data for selected words (compatible with GuessProcessor)</summary>
        public List<WordPlacementData> Placements => _placements;

        /// <summary>Whether setup is complete</summary>
        public bool IsSetupComplete => _placements.Count == _wordCount;

        // ============================================================
        // WORD SELECTION
        // ============================================================

        /// <summary>
        /// Selects random words from the provided word lists.
        /// </summary>
        /// <param name="wordLists">Dictionary mapping word length to WordListSO</param>
        /// <returns>True if selection successful</returns>
        public bool SelectWords(Dictionary<int, WordListSO> wordLists)
        {
            _selectedWords.Clear();

            // Determine which lengths to use based on word count
            List<int> lengthsToUse = DetermineLengthsToUse();

            foreach (int length in lengthsToUse)
            {
                if (!wordLists.ContainsKey(length))
                {
                    Debug.LogError(string.Format("[AISetupManager] No word list for length {0}", length));
                    return false;
                }

                WordListSO wordList = wordLists[length];
                if (wordList.Words == null || wordList.Words.Count == 0)
                {
                    Debug.LogError(string.Format("[AISetupManager] Word list for length {0} is empty", length));
                    return false;
                }

                // Pick a random word that isn't already selected
                string word = SelectRandomWord(wordList.Words, _selectedWords);
                if (string.IsNullOrEmpty(word))
                {
                    Debug.LogError(string.Format("[AISetupManager] Could not find unique word of length {0}", length));
                    return false;
                }

                _selectedWords.Add(word.ToUpperInvariant());
            }

            Debug.Log(string.Format("[AISetupManager] Selected words: {0}", string.Join(", ", _selectedWords)));
            return true;
        }

        /// <summary>
        /// Determines which word lengths to use based on word count and available lengths.
        /// </summary>
        /// <returns>List of lengths, one per word needed</returns>
        private List<int> DetermineLengthsToUse()
        {
            List<int> result = new List<int>();

            if (_wordLengths == null || _wordLengths.Length == 0)
            {
                // Default: 3, 4, 5 letter words
                int[] defaultLengths = { 3, 4, 5, 6 };
                for (int i = 0; i < _wordCount && i < defaultLengths.Length; i++)
                {
                    result.Add(defaultLengths[i]);
                }
            }
            else
            {
                // Use provided lengths, cycling if needed
                for (int i = 0; i < _wordCount; i++)
                {
                    int lengthIndex = i % _wordLengths.Length;
                    result.Add(_wordLengths[lengthIndex]);
                }
            }

            return result;
        }

        /// <summary>
        /// Selects a random word from the list that isn't in the exclude set.
        /// </summary>
        /// <param name="words">Available words</param>
        /// <param name="exclude">Words to exclude</param>
        /// <returns>Selected word or null if none available</returns>
        private string SelectRandomWord(List<string> words, List<string> exclude)
        {
            // Create list of valid candidates
            List<string> candidates = new List<string>();
            HashSet<string> excludeSet = new HashSet<string>(exclude, System.StringComparer.OrdinalIgnoreCase);

            foreach (string word in words)
            {
                if (!excludeSet.Contains(word))
                {
                    candidates.Add(word);
                }
            }

            if (candidates.Count == 0)
            {
                return null;
            }

            int index = Random.Range(0, candidates.Count);
            return candidates[index];
        }

        // ============================================================
        // WORD PLACEMENT
        // ============================================================

        /// <summary>
        /// Places selected words on the grid randomly.
        /// Must call SelectWords() first.
        /// Supports crossword-style placement where words can share cells if letters match.
        /// Crossword placement is randomly enabled/disabled for variety.
        /// </summary>
        /// <param name="maxAttempts">Maximum placement attempts per word</param>
        /// <param name="crosswordProbability">Probability (0-1) of allowing crossword placement. Default 0.4 (40%)</param>
        /// <returns>True if all words placed successfully</returns>
        public bool PlaceWords(int maxAttempts = 100, float crosswordProbability = 0.4f)
        {
            if (_selectedWords.Count == 0)
            {
                Debug.LogError("[AISetupManager] No words selected. Call SelectWords() first.");
                return false;
            }

            _placements.Clear();

            // Randomly decide if this setup will use crossword placement
            bool allowCrossword = Random.value < crosswordProbability;
            Debug.Log(string.Format("[AISetupManager] Crossword placement: {0}", allowCrossword ? "enabled" : "disabled"));

            // Track occupied cells with their letters (for crossword validation)
            Dictionary<(int row, int col), char> occupiedCells = new Dictionary<(int, int), char>();

            // Sort words by length descending (place longer words first)
            List<(string word, int originalIndex)> sortedWords = new List<(string, int)>();
            for (int i = 0; i < _selectedWords.Count; i++)
            {
                sortedWords.Add((_selectedWords[i], i));
            }
            sortedWords.Sort((a, b) => b.word.Length.CompareTo(a.word.Length));

            foreach ((string word, int originalIndex) in sortedWords)
            {
                bool placed = TryPlaceWord(word, originalIndex, occupiedCells, maxAttempts, allowCrossword);
                if (!placed)
                {
                    Debug.LogError(string.Format("[AISetupManager] Failed to place word: {0}", word));
                    return false;
                }
            }

            // Sort placements by RowIndex to match word order
            _placements.Sort((a, b) => a.RowIndex.CompareTo(b.RowIndex));

            Debug.Log(string.Format("[AISetupManager] Successfully placed {0} words", _placements.Count));
            return true;
        }

        // 8 directions: E, S, SE, NE, W, N, NW, SW (same as player's TablePlacementController)
        private static readonly int[] DirCols = { 1, 0, 1, 1, -1, 0, -1, -1 };
        private static readonly int[] DirRows = { 0, 1, 1, -1, 0, -1, -1, 1 };
        private static readonly string[] DirNames = { "E", "S", "SE", "NE", "W", "N", "NW", "SW" };

        /// <summary>
        /// Attempts to place a single word on the grid.
        /// Supports crossword-style placement where overlapping cells must have matching letters.
        /// </summary>
        /// <param name="word">Word to place</param>
        /// <param name="rowIndex">The row index (word slot) for this word</param>
        /// <param name="occupiedCells">Currently occupied cells with their letters</param>
        /// <param name="maxAttempts">Maximum attempts</param>
        /// <param name="allowCrossword">If true, allows overlapping cells with matching letters</param>
        /// <returns>True if placed successfully</returns>
        private bool TryPlaceWord(string word, int rowIndex, Dictionary<(int row, int col), char> occupiedCells, int maxAttempts, bool allowCrossword)
        {
            int wordLength = word.Length;

            for (int attempt = 0; attempt < maxAttempts; attempt++)
            {
                // Random direction from all 8
                int dirIndex = Random.Range(0, 8);
                int dirCol = DirCols[dirIndex];
                int dirRow = DirRows[dirIndex];

                // Calculate valid position range for this direction
                int minStartCol, maxStartCol, minStartRow, maxStartRow;
                GetValidStartRange(wordLength, dirCol, dirRow, out minStartCol, out maxStartCol, out minStartRow, out maxStartRow);

                if (maxStartCol < minStartCol || maxStartRow < minStartRow)
                {
                    // Word doesn't fit in this direction, try another attempt
                    continue;
                }

                // Random position within valid range
                int startCol = Random.Range(minStartCol, maxStartCol + 1);
                int startRow = Random.Range(minStartRow, maxStartRow + 1);

                // Check if placement is valid
                List<(int row, int col, char letter)> cellsWithLetters = GetWordCellsWithLetters(word, startRow, startCol, dirRow, dirCol);

                if (IsPlacementValid(cellsWithLetters, occupiedCells, allowCrossword))
                {
                    // Place the word using existing WordPlacementData structure
                    WordPlacementData placement = new WordPlacementData
                    {
                        Word = word,
                        StartCol = startCol,
                        StartRow = startRow,
                        DirCol = dirCol,
                        DirRow = dirRow,
                        RowIndex = rowIndex
                    };

                    _placements.Add(placement);

                    // Mark cells as occupied with their letters
                    foreach ((int row, int col, char letter) cell in cellsWithLetters)
                    {
                        occupiedCells[(cell.row, cell.col)] = cell.letter;
                    }

                    string coordStr = string.Format("{0}{1}", (char)('A' + startCol), startRow + 1);
                    Debug.Log(string.Format("[AISetupManager] Placed {0} at {1} direction {2}",
                        word, coordStr, DirNames[dirIndex]));

                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Calculates the valid start position range for a word in a given direction.
        /// </summary>
        private void GetValidStartRange(int wordLength, int dirCol, int dirRow,
            out int minStartCol, out int maxStartCol, out int minStartRow, out int maxStartRow)
        {
            // Default to full grid
            minStartCol = 0;
            maxStartCol = _gridSize - 1;
            minStartRow = 0;
            maxStartRow = _gridSize - 1;

            // Adjust based on direction so word stays in bounds
            // For positive direction, need room at the end
            // For negative direction, need room at the start
            int extent = wordLength - 1;

            if (dirCol > 0)
            {
                // Moving right, need room on the right
                maxStartCol = _gridSize - 1 - extent;
            }
            else if (dirCol < 0)
            {
                // Moving left, need room on the left (start from at least extent)
                minStartCol = extent;
            }

            if (dirRow > 0)
            {
                // Moving down, need room at the bottom
                maxStartRow = _gridSize - 1 - extent;
            }
            else if (dirRow < 0)
            {
                // Moving up, need room at the top (start from at least extent)
                minStartRow = extent;
            }
        }

        /// <summary>
        /// Gets all cells that a word would occupy, along with the letter at each position.
        /// </summary>
        private List<(int row, int col, char letter)> GetWordCellsWithLetters(string word, int startRow, int startCol, int dirRow, int dirCol)
        {
            List<(int row, int col, char letter)> cells = new List<(int, int, char)>();

            for (int i = 0; i < word.Length; i++)
            {
                int row = startRow + (i * dirRow);
                int col = startCol + (i * dirCol);
                cells.Add((row, col, word[i]));
            }

            return cells;
        }

        /// <summary>
        /// Gets all cells that a word would occupy.
        /// </summary>
        /// <param name="word">The word</param>
        /// <param name="startRow">Starting row</param>
        /// <param name="startCol">Starting column</param>
        /// <param name="dirRow">Row direction (-1, 0, or 1)</param>
        /// <param name="dirCol">Column direction (-1, 0, or 1)</param>
        /// <returns>List of (row, col) tuples</returns>
        private List<(int row, int col)> GetWordCells(string word, int startRow, int startCol, int dirRow, int dirCol)
        {
            List<(int row, int col)> cells = new List<(int, int)>();

            for (int i = 0; i < word.Length; i++)
            {
                int row = startRow + (i * dirRow);
                int col = startCol + (i * dirCol);
                cells.Add((row, col));
            }

            return cells;
        }

        /// <summary>
        /// Checks if a placement is valid.
        /// When allowCrossword is true, overlapping cells are allowed if letters match.
        /// When allowCrossword is false, any overlap is rejected.
        /// </summary>
        /// <param name="cellsWithLetters">Cells the word would occupy with their letters</param>
        /// <param name="occupiedCells">Currently occupied cells with their letters</param>
        /// <param name="allowCrossword">If true, allows overlapping cells with matching letters</param>
        /// <returns>True if valid</returns>
        private bool IsPlacementValid(List<(int row, int col, char letter)> cellsWithLetters, Dictionary<(int row, int col), char> occupiedCells, bool allowCrossword)
        {
            foreach ((int row, int col, char letter) cell in cellsWithLetters)
            {
                // Check bounds
                if (cell.row < 0 || cell.row >= _gridSize || cell.col < 0 || cell.col >= _gridSize)
                {
                    return false;
                }

                // Check overlap
                if (occupiedCells.TryGetValue((cell.row, cell.col), out char existingLetter))
                {
                    if (!allowCrossword)
                    {
                        // No crossword allowed - any overlap is invalid
                        return false;
                    }

                    if (existingLetter != cell.letter)
                    {
                        // Crossword allowed but letters don't match
                        return false;
                    }
                    // Letters match - valid crossword intersection
                }
            }

            return true;
        }

        // ============================================================
        // COMBINED SETUP
        // ============================================================

        /// <summary>
        /// Performs complete AI setup: selects words and places them.
        /// </summary>
        /// <param name="wordLists">Dictionary mapping word length to WordListSO</param>
        /// <returns>True if setup successful</returns>
        public bool PerformSetup(Dictionary<int, WordListSO> wordLists)
        {
            if (!SelectWords(wordLists))
            {
                return false;
            }

            if (!PlaceWords())
            {
                return false;
            }

            return true;
        }

        // ============================================================
        // DEBUG
        // ============================================================

        /// <summary>
        /// Gets a debug summary of the current setup state.
        /// </summary>
        /// <returns>Formatted debug string</returns>
        public string GetDebugSummary()
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            sb.AppendLine("=== AI SETUP SUMMARY ===");
            sb.AppendLine(string.Format("Grid Size: {0}x{0}", _gridSize));
            sb.AppendLine(string.Format("Word Count: {0}", _wordCount));
            sb.AppendLine();

            sb.AppendLine("Selected Words:");
            for (int i = 0; i < _selectedWords.Count; i++)
            {
                sb.AppendLine(string.Format("  {0}. {1}", i + 1, _selectedWords[i]));
            }
            sb.AppendLine();

            sb.AppendLine("Placements:");
            foreach (WordPlacementData p in _placements)
            {
                string coordStr = string.Format("{0}{1}", (char)('A' + p.StartRow), p.StartCol + 1);
                string direction = (p.DirCol == 1) ? "H" : "V";
                sb.AppendLine(string.Format("  Row {0}: {1} at {2} ({3})",
                    p.RowIndex + 1, p.Word, coordStr, direction));
            }

            return sb.ToString();
        }

        // ============================================================
        // UTILITY
        // ============================================================

        /// <summary>
        /// Gets all cells occupied by a placed word.
        /// Helper for external use.
        /// </summary>
        /// <param name="placement">The placement data</param>
        /// <returns>List of (row, col) tuples</returns>
        public static List<(int row, int col)> GetCellsForPlacement(WordPlacementData placement)
        {
            List<(int row, int col)> cells = new List<(int, int)>();

            for (int i = 0; i < placement.Word.Length; i++)
            {
                int row = placement.StartRow + (i * placement.DirRow);
                int col = placement.StartCol + (i * placement.DirCol);
                cells.Add((row, col));
            }

            return cells;
        }

        /// <summary>
        /// Gets the letter at a specific cell from a placement, or null if not part of word.
        /// </summary>
        /// <param name="placement">The placement data</param>
        /// <param name="row">Row to check</param>
        /// <param name="col">Column to check</param>
        /// <returns>Letter at position or null</returns>
        public static char? GetLetterAtCell(WordPlacementData placement, int row, int col)
        {
            // Check if we're on the word's line
            if (placement.DirCol == 1) // Horizontal
            {
                if (row != placement.StartRow) return null;
                int index = col - placement.StartCol;
                if (index >= 0 && index < placement.Word.Length)
                {
                    return placement.Word[index];
                }
            }
            else // Vertical (DirRow == 1)
            {
                if (col != placement.StartCol) return null;
                int index = row - placement.StartRow;
                if (index >= 0 && index < placement.Word.Length)
                {
                    return placement.Word[index];
                }
            }

            return null;
        }
    }
}
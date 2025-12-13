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
        /// </summary>
        /// <param name="maxAttempts">Maximum placement attempts per word</param>
        /// <returns>True if all words placed successfully</returns>
        public bool PlaceWords(int maxAttempts = 100)
        {
            if (_selectedWords.Count == 0)
            {
                Debug.LogError("[AISetupManager] No words selected. Call SelectWords() first.");
                return false;
            }

            _placements.Clear();

            // Track occupied cells
            HashSet<(int row, int col)> occupiedCells = new HashSet<(int, int)>();

            // Sort words by length descending (place longer words first)
            List<(string word, int originalIndex)> sortedWords = new List<(string, int)>();
            for (int i = 0; i < _selectedWords.Count; i++)
            {
                sortedWords.Add((_selectedWords[i], i));
            }
            sortedWords.Sort((a, b) => b.word.Length.CompareTo(a.word.Length));

            foreach ((string word, int originalIndex) in sortedWords)
            {
                bool placed = TryPlaceWord(word, originalIndex, occupiedCells, maxAttempts);
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

        /// <summary>
        /// Attempts to place a single word on the grid.
        /// </summary>
        /// <param name="word">Word to place</param>
        /// <param name="rowIndex">The row index (word slot) for this word</param>
        /// <param name="occupiedCells">Currently occupied cells</param>
        /// <param name="maxAttempts">Maximum attempts</param>
        /// <returns>True if placed successfully</returns>
        private bool TryPlaceWord(string word, int rowIndex, HashSet<(int row, int col)> occupiedCells, int maxAttempts)
        {
            for (int attempt = 0; attempt < maxAttempts; attempt++)
            {
                // Random orientation: horizontal (DirCol=1, DirRow=0) or vertical (DirCol=0, DirRow=1)
                bool isHorizontal = Random.value > 0.5f;
                int dirCol = isHorizontal ? 1 : 0;
                int dirRow = isHorizontal ? 0 : 1;

                // Calculate valid position range
                int maxStartCol = isHorizontal ? _gridSize - word.Length : _gridSize - 1;
                int maxStartRow = isHorizontal ? _gridSize - 1 : _gridSize - word.Length;

                if (maxStartCol < 0 || maxStartRow < 0)
                {
                    // Word doesn't fit in this orientation, try the other
                    isHorizontal = !isHorizontal;
                    dirCol = isHorizontal ? 1 : 0;
                    dirRow = isHorizontal ? 0 : 1;
                    maxStartCol = isHorizontal ? _gridSize - word.Length : _gridSize - 1;
                    maxStartRow = isHorizontal ? _gridSize - 1 : _gridSize - word.Length;

                    if (maxStartCol < 0 || maxStartRow < 0)
                    {
                        // Word doesn't fit at all
                        Debug.LogError(string.Format("[AISetupManager] Word {0} too long for grid size {1}",
                            word, _gridSize));
                        return false;
                    }
                }

                // Random position
                int startCol = Random.Range(0, maxStartCol + 1);
                int startRow = Random.Range(0, maxStartRow + 1);

                // Check if placement is valid
                List<(int row, int col)> cells = GetWordCells(word, startRow, startCol, dirRow, dirCol);

                if (IsPlacementValid(cells, occupiedCells))
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

                    // Mark cells as occupied
                    foreach ((int row, int col) cell in cells)
                    {
                        occupiedCells.Add(cell);
                    }

                    string coordStr = string.Format("{0}{1}", (char)('A' + startRow), startCol + 1);
                    Debug.Log(string.Format("[AISetupManager] Placed {0} at {1} {2}",
                        word, coordStr, isHorizontal ? "horizontal" : "vertical"));

                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Gets all cells that a word would occupy.
        /// </summary>
        /// <param name="word">The word</param>
        /// <param name="startRow">Starting row</param>
        /// <param name="startCol">Starting column</param>
        /// <param name="dirRow">Row direction (0 or 1)</param>
        /// <param name="dirCol">Column direction (0 or 1)</param>
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
        /// Checks if a placement is valid (no overlaps with occupied cells).
        /// </summary>
        /// <param name="cells">Cells the word would occupy</param>
        /// <param name="occupiedCells">Currently occupied cells</param>
        /// <returns>True if valid</returns>
        private bool IsPlacementValid(List<(int row, int col)> cells, HashSet<(int row, int col)> occupiedCells)
        {
            foreach ((int row, int col) cell in cells)
            {
                // Check bounds
                if (cell.row < 0 || cell.row >= _gridSize || cell.col < 0 || cell.col >= _gridSize)
                {
                    return false;
                }

                // Check overlap
                if (occupiedCells.Contains(cell))
                {
                    return false;
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
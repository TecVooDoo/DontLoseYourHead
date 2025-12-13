// GridAnalyzer.cs
// Static utility class for grid analysis and calculations
// Created: December 13, 2025
// Developer: TecVooDoo LLC

using System.Collections.Generic;
using UnityEngine;

namespace DLYH.AI.Data
{
    /// <summary>
    /// Static utility class for analyzing grid state.
    /// Provides calculations for fill ratio, adjacency, center bias,
    /// and other grid-related metrics used by AI strategies.
    /// </summary>
    public static class GridAnalyzer
    {
        // ============================================================
        // FILL RATIO CALCULATIONS
        // ============================================================

        /// <summary>
        /// Calculates the fill ratio of a grid based on word configuration.
        /// Fill ratio = total letters / total cells.
        /// </summary>
        /// <param name="gridSize">Size of the grid (e.g., 8 for 8x8)</param>
        /// <param name="wordCount">Number of words placed</param>
        /// <param name="averageWordLength">Average length of words (default 4.5 for 3-6 letter words)</param>
        /// <returns>Fill ratio between 0 and 1</returns>
        public static float CalculateFillRatio(int gridSize, int wordCount, float averageWordLength = 4.5f)
        {
            if (gridSize <= 0)
            {
                return 0f;
            }

            int totalCells = gridSize * gridSize;
            float estimatedLetters = wordCount * averageWordLength;

            return Mathf.Clamp01(estimatedLetters / totalCells);
        }

        /// <summary>
        /// Calculates fill ratio using actual letter count.
        /// </summary>
        /// <param name="gridSize">Size of the grid</param>
        /// <param name="letterCount">Actual number of letters on the grid</param>
        /// <returns>Fill ratio between 0 and 1</returns>
        public static float CalculateFillRatioExact(int gridSize, int letterCount)
        {
            if (gridSize <= 0)
            {
                return 0f;
            }

            int totalCells = gridSize * gridSize;
            return Mathf.Clamp01((float)letterCount / totalCells);
        }

        /// <summary>
        /// Gets a description of the grid density category.
        /// </summary>
        /// <param name="fillRatio">The fill ratio (0-1)</param>
        /// <returns>Density category description</returns>
        public static string GetDensityCategory(float fillRatio)
        {
            if (fillRatio >= 0.35f)
            {
                return "High";
            }
            else if (fillRatio >= 0.20f)
            {
                return "Medium";
            }
            else if (fillRatio >= 0.12f)
            {
                return "Low";
            }
            else
            {
                return "Very Low";
            }
        }

        // ============================================================
        // ADJACENCY CALCULATIONS
        // ============================================================

        /// <summary>
        /// Gets all valid adjacent coordinates (4-directional: up, down, left, right).
        /// </summary>
        /// <param name="row">Current row (0-indexed)</param>
        /// <param name="col">Current column (0-indexed)</param>
        /// <param name="gridSize">Size of the grid</param>
        /// <returns>List of valid adjacent coordinates as (row, col) tuples</returns>
        public static List<(int row, int col)> GetAdjacentCoordinates(int row, int col, int gridSize)
        {
            List<(int row, int col)> adjacent = new List<(int, int)>(4);

            // Up
            if (row > 0)
            {
                adjacent.Add((row - 1, col));
            }

            // Down
            if (row < gridSize - 1)
            {
                adjacent.Add((row + 1, col));
            }

            // Left
            if (col > 0)
            {
                adjacent.Add((row, col - 1));
            }

            // Right
            if (col < gridSize - 1)
            {
                adjacent.Add((row, col + 1));
            }

            return adjacent;
        }

        /// <summary>
        /// Gets all valid adjacent coordinates (8-directional: includes diagonals).
        /// </summary>
        /// <param name="row">Current row (0-indexed)</param>
        /// <param name="col">Current column (0-indexed)</param>
        /// <param name="gridSize">Size of the grid</param>
        /// <returns>List of valid adjacent coordinates as (row, col) tuples</returns>
        public static List<(int row, int col)> GetAdjacentCoordinates8Way(int row, int col, int gridSize)
        {
            List<(int row, int col)> adjacent = new List<(int, int)>(8);

            for (int dRow = -1; dRow <= 1; dRow++)
            {
                for (int dCol = -1; dCol <= 1; dCol++)
                {
                    // Skip the center cell
                    if (dRow == 0 && dCol == 0)
                    {
                        continue;
                    }

                    int newRow = row + dRow;
                    int newCol = col + dCol;

                    if (IsValidCoordinate(newRow, newCol, gridSize))
                    {
                        adjacent.Add((newRow, newCol));
                    }
                }
            }

            return adjacent;
        }

        /// <summary>
        /// Checks if a coordinate is adjacent to any coordinate in a set.
        /// </summary>
        /// <param name="row">Row to check</param>
        /// <param name="col">Column to check</param>
        /// <param name="knownHits">Set of known hit coordinates</param>
        /// <param name="gridSize">Size of the grid</param>
        /// <returns>True if adjacent to at least one known hit</returns>
        public static bool IsAdjacentToAny(int row, int col, HashSet<(int row, int col)> knownHits, int gridSize)
        {
            List<(int row, int col)> adjacent = GetAdjacentCoordinates(row, col, gridSize);

            foreach ((int adjRow, int adjCol) in adjacent)
            {
                if (knownHits.Contains((adjRow, adjCol)))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Counts how many known hits are adjacent to a coordinate.
        /// </summary>
        /// <param name="row">Row to check</param>
        /// <param name="col">Column to check</param>
        /// <param name="knownHits">Set of known hit coordinates</param>
        /// <param name="gridSize">Size of the grid</param>
        /// <returns>Number of adjacent known hits (0-4)</returns>
        public static int CountAdjacentHits(int row, int col, HashSet<(int row, int col)> knownHits, int gridSize)
        {
            int count = 0;
            List<(int row, int col)> adjacent = GetAdjacentCoordinates(row, col, gridSize);

            foreach ((int adjRow, int adjCol) in adjacent)
            {
                if (knownHits.Contains((adjRow, adjCol)))
                {
                    count++;
                }
            }

            return count;
        }

        // ============================================================
        // LINE EXTENSION DETECTION
        // ============================================================

        /// <summary>
        /// Checks if a coordinate would extend an existing line of hits (horizontal or vertical).
        /// Words are placed in lines, so extending a line of hits is often strategic.
        /// </summary>
        /// <param name="row">Row to check</param>
        /// <param name="col">Column to check</param>
        /// <param name="knownHits">Set of known hit coordinates</param>
        /// <param name="gridSize">Size of the grid</param>
        /// <returns>True if this coordinate extends a line of 2+ hits</returns>
        public static bool ExtendsHitLine(int row, int col, HashSet<(int row, int col)> knownHits, int gridSize)
        {
            // Check horizontal line extension
            // Pattern: [hit][hit][candidate] or [candidate][hit][hit]

            // Check if there are 2 hits to the left
            if (col >= 2 && knownHits.Contains((row, col - 1)) && knownHits.Contains((row, col - 2)))
            {
                return true;
            }

            // Check if there are 2 hits to the right
            if (col <= gridSize - 3 && knownHits.Contains((row, col + 1)) && knownHits.Contains((row, col + 2)))
            {
                return true;
            }

            // Check if candidate is between two hits horizontally
            if (col >= 1 && col <= gridSize - 2 && knownHits.Contains((row, col - 1)) && knownHits.Contains((row, col + 1)))
            {
                return true;
            }

            // Check vertical line extension

            // Check if there are 2 hits above
            if (row >= 2 && knownHits.Contains((row - 1, col)) && knownHits.Contains((row - 2, col)))
            {
                return true;
            }

            // Check if there are 2 hits below
            if (row <= gridSize - 3 && knownHits.Contains((row + 1, col)) && knownHits.Contains((row + 2, col)))
            {
                return true;
            }

            // Check if candidate is between two hits vertically
            if (row >= 1 && row <= gridSize - 2 && knownHits.Contains((row - 1, col)) && knownHits.Contains((row + 1, col)))
            {
                return true;
            }

            return false;
        }

        // ============================================================
        // CENTER BIAS CALCULATIONS
        // ============================================================

        /// <summary>
        /// Calculates the distance from a coordinate to the grid center.
        /// </summary>
        /// <param name="row">Row (0-indexed)</param>
        /// <param name="col">Column (0-indexed)</param>
        /// <param name="gridSize">Size of the grid</param>
        /// <returns>Distance to center (0 = at center)</returns>
        public static float GetDistanceFromCenter(int row, int col, int gridSize)
        {
            float centerRow = (gridSize - 1) / 2f;
            float centerCol = (gridSize - 1) / 2f;

            float dRow = row - centerRow;
            float dCol = col - centerCol;

            return Mathf.Sqrt(dRow * dRow + dCol * dCol);
        }

        /// <summary>
        /// Calculates a center bias score for a coordinate.
        /// Higher score = closer to center (words often pass through center).
        /// </summary>
        /// <param name="row">Row (0-indexed)</param>
        /// <param name="col">Column (0-indexed)</param>
        /// <param name="gridSize">Size of the grid</param>
        /// <returns>Score from 0 to 1, where 1 = at center</returns>
        public static float GetCenterBiasScore(int row, int col, int gridSize)
        {
            float maxDistance = GetDistanceFromCenter(0, 0, gridSize);

            if (maxDistance <= 0)
            {
                return 1f;
            }

            float distance = GetDistanceFromCenter(row, col, gridSize);
            return 1f - (distance / maxDistance);
        }

        // ============================================================
        // COORDINATE SCORING
        // ============================================================

        /// <summary>
        /// Calculates a combined score for a coordinate based on multiple factors.
        /// Higher score = better candidate for guessing.
        /// </summary>
        /// <param name="row">Row to score</param>
        /// <param name="col">Column to score</param>
        /// <param name="knownHits">Set of known hit coordinates</param>
        /// <param name="gridSize">Size of the grid</param>
        /// <param name="fillRatio">Current grid fill ratio</param>
        /// <returns>Combined score for this coordinate</returns>
        public static float CalculateCoordinateScore(
            int row,
            int col,
            HashSet<(int row, int col)> knownHits,
            int gridSize,
            float fillRatio)
        {
            float score = 0f;

            // Adjacency bonus - scales inversely with density (more valuable on sparse grids)
            float adjacencyBonusMultiplier = Mathf.Lerp(3f, 1f, fillRatio);
            int adjacentHitCount = CountAdjacentHits(row, col, knownHits, gridSize);
            score += adjacentHitCount * adjacencyBonusMultiplier;

            // Line extension bonus
            if (ExtendsHitLine(row, col, knownHits, gridSize))
            {
                score += 0.5f;
            }

            // Center bias (weighted lower than adjacency)
            float centerBias = GetCenterBiasScore(row, col, gridSize);
            score += centerBias * 0.3f;

            return score;
        }

        // ============================================================
        // UTILITY METHODS
        // ============================================================

        /// <summary>
        /// Checks if a coordinate is within valid grid bounds.
        /// </summary>
        /// <param name="row">Row to check</param>
        /// <param name="col">Column to check</param>
        /// <param name="gridSize">Size of the grid</param>
        /// <returns>True if coordinate is valid</returns>
        public static bool IsValidCoordinate(int row, int col, int gridSize)
        {
            return row >= 0 && row < gridSize && col >= 0 && col < gridSize;
        }

        /// <summary>
        /// Converts a coordinate to a string representation (e.g., "A1", "B3").
        /// </summary>
        /// <param name="row">Row (0-indexed)</param>
        /// <param name="col">Column (0-indexed)</param>
        /// <returns>String like "A1" where A is column, 1 is row+1</returns>
        public static string CoordinateToString(int row, int col)
        {
            char colChar = (char)('A' + col);
            int rowNum = row + 1;
            return string.Format("{0}{1}", colChar, rowNum);
        }

        /// <summary>
        /// Parses a coordinate string (e.g., "A1") to row and column.
        /// </summary>
        /// <param name="coordinate">String like "A1"</param>
        /// <param name="row">Output row (0-indexed)</param>
        /// <param name="col">Output column (0-indexed)</param>
        /// <returns>True if parsing succeeded</returns>
        public static bool TryParseCoordinate(string coordinate, out int row, out int col)
        {
            row = -1;
            col = -1;

            if (string.IsNullOrEmpty(coordinate) || coordinate.Length < 2)
            {
                return false;
            }

            char colChar = char.ToUpper(coordinate[0]);

            if (colChar < 'A' || colChar > 'Z')
            {
                return false;
            }

            col = colChar - 'A';

            string rowStr = coordinate.Substring(1);
            if (int.TryParse(rowStr, out int rowNum) && rowNum >= 1)
            {
                row = rowNum - 1;
                return true;
            }

            return false;
        }

        /// <summary>
        /// Gets all unguessed coordinates on the grid.
        /// </summary>
        /// <param name="gridSize">Size of the grid</param>
        /// <param name="guessedCoordinates">Set of already guessed coordinates</param>
        /// <returns>List of unguessed coordinates</returns>
        public static List<(int row, int col)> GetUnguessedCoordinates(
            int gridSize,
            HashSet<(int row, int col)> guessedCoordinates)
        {
            List<(int row, int col)> unguessed = new List<(int, int)>();

            for (int row = 0; row < gridSize; row++)
            {
                for (int col = 0; col < gridSize; col++)
                {
                    if (!guessedCoordinates.Contains((row, col)))
                    {
                        unguessed.Add((row, col));
                    }
                }
            }

            return unguessed;
        }
    }
}
// CoordinateGuessStrategy.cs
// AI strategy for guessing coordinates based on adjacency and pattern analysis
// Created: December 13, 2025
// Developer: TecVooDoo LLC

using System.Collections.Generic;
using UnityEngine;
using DLYH.AI.Config;
using DLYH.AI.Data;

namespace DLYH.AI.Strategies
{
    /// <summary>
    /// Strategy for selecting which coordinate to guess.
    /// 
    /// Scores coordinates based on:
    /// - Adjacency to known hits (words are contiguous)
    /// - Line extension potential (words are placed in rows/columns)
    /// - Center bias (longer words often pass through center)
    /// 
    /// Skill level affects selection pool size similar to letter strategy.
    /// </summary>
    public class CoordinateGuessStrategy : IGuessStrategy
    {
        // ============================================================
        // CONFIGURATION
        // ============================================================

        private readonly ExecutionerConfigSO _config;

        // ============================================================
        // INTERFACE IMPLEMENTATION
        // ============================================================

        public GuessType StrategyType => GuessType.Coordinate;

        // ============================================================
        // CONSTRUCTOR
        // ============================================================

        /// <summary>
        /// Creates a new CoordinateGuessStrategy.
        /// </summary>
        /// <param name="config">Configuration ScriptableObject</param>
        public CoordinateGuessStrategy(ExecutionerConfigSO config)
        {
            _config = config;
        }

        // ============================================================
        // MAIN EVALUATION
        // ============================================================

        /// <summary>
        /// Evaluates the game state and recommends a coordinate to guess.
        /// </summary>
        /// <param name="state">Current game state</param>
        /// <returns>Coordinate guess recommendation</returns>
        public GuessRecommendation Evaluate(AIGameState state)
        {
            // Check if all coordinates containing letters are already found
            // If so, no point guessing more coordinates - focus on letters instead
            if (state.AreAllCoordinatesFound())
            {
                Debug.Log("[CoordinateGuessStrategy] All letter coordinates already found - skipping coordinate guess");
                return GuessRecommendation.CreateInvalid();
            }

            // Get unguessed coordinates
            List<(int row, int col)> unguessedCoords = state.GetUnguessedCoordinates();

            if (unguessedCoords.Count == 0)
            {
                Debug.LogWarning("[CoordinateGuessStrategy] No unguessed coordinates remaining");
                return GuessRecommendation.CreateInvalid();
            }

            // Score each unguessed coordinate
            List<((int row, int col) coord, float score)> scoredCoords = new List<((int, int), float)>();

            foreach ((int row, int col) coord in unguessedCoords)
            {
                float score = CalculateCoordinateScore(coord.row, coord.col, state);
                scoredCoords.Add((coord, score));
            }

            // Sort by score descending
            scoredCoords.Sort((a, b) => b.score.CompareTo(a.score));

            // Select based on skill level
            int poolSize = GetSelectionPoolSize(state.SkillLevel);
            poolSize = Mathf.Min(poolSize, scoredCoords.Count);

            // Pick randomly from the top N candidates
            int selectedIndex = Random.Range(0, poolSize);
            (int row, int col) selectedCoord = scoredCoords[selectedIndex].coord;
            float selectedScore = scoredCoords[selectedIndex].score;

            // Calculate confidence based on relative score
            float maxScore = scoredCoords[0].score;
            float confidence = maxScore > 0 ? selectedScore / maxScore : 0.5f;

            // Adjust confidence based on fill ratio (lower density = lower confidence for coordinates)
            confidence *= Mathf.Lerp(0.5f, 1.0f, state.FillRatio / 0.35f);
            confidence = Mathf.Clamp01(confidence);

            return GuessRecommendation.CreateCoordinateGuess(selectedCoord.row, selectedCoord.col, confidence);
        }

        // ============================================================
        // SCORING
        // ============================================================

        /// <summary>
        /// Calculates a score for a coordinate based on multiple factors.
        /// </summary>
        /// <param name="row">Row to score</param>
        /// <param name="col">Column to score</param>
        /// <param name="state">Current game state</param>
        /// <returns>Combined score (higher = better candidate)</returns>
        private float CalculateCoordinateScore(int row, int col, AIGameState state)
        {
            float score = 0f;

            // Use GridAnalyzer for calculations
            score = GridAnalyzer.CalculateCoordinateScore(
                row,
                col,
                state.HitCoordinates,
                state.GridSize,
                state.FillRatio);

            // Additional bonus: if we have hits but no adjacency, prefer edges of known word areas
            if (state.HitCoordinates.Count > 0)
            {
                float proximityBonus = CalculateProximityBonus(row, col, state.HitCoordinates, state.GridSize);
                score += proximityBonus;
            }

            return score;
        }

        /// <summary>
        /// Calculates a proximity bonus for coordinates near (but not adjacent to) known hits.
        /// This helps find the ends of words when we've found the middle.
        /// </summary>
        /// <param name="row">Row to check</param>
        /// <param name="col">Column to check</param>
        /// <param name="hitCoordinates">Known hit coordinates</param>
        /// <param name="gridSize">Size of the grid</param>
        /// <returns>Proximity bonus (0 to 0.5)</returns>
        private float CalculateProximityBonus(int row, int col, HashSet<(int row, int col)> hitCoordinates, int gridSize)
        {
            // If already adjacent, GridAnalyzer handles it
            if (GridAnalyzer.IsAdjacentToAny(row, col, hitCoordinates, gridSize))
            {
                return 0f;
            }

            // Find minimum distance to any hit
            float minDistance = float.MaxValue;

            foreach ((int hitRow, int hitCol) in hitCoordinates)
            {
                float distance = Mathf.Abs(row - hitRow) + Mathf.Abs(col - hitCol); // Manhattan distance
                if (distance < minDistance)
                {
                    minDistance = distance;
                }
            }

            // Small bonus for being within 2-3 cells of a hit (not adjacent, not far)
            if (minDistance >= 2 && minDistance <= 3)
            {
                return 0.3f;
            }

            return 0f;
        }

        /// <summary>
        /// Gets the selection pool size based on skill level.
        /// Uses similar scaling to letter strategy but slightly larger pools
        /// since coordinate guessing is inherently more random.
        /// </summary>
        /// <param name="skillLevel">Current AI skill level (0-1)</param>
        /// <returns>Number of top candidates to select from</returns>
        private int GetSelectionPoolSize(float skillLevel)
        {
            if (skillLevel >= 0.9f)
            {
                return 1;  // Expert: always pick optimal
            }
            else if (skillLevel >= 0.7f)
            {
                return 3;  // Hard: top 3
            }
            else if (skillLevel >= 0.4f)
            {
                return 8;  // Normal: top 8
            }
            else
            {
                return 15; // Easy: top 15 (fairly random)
            }
        }

        // ============================================================
        // DEBUG
        // ============================================================

        /// <summary>
        /// Gets a debug breakdown of coordinate scores for the current state.
        /// </summary>
        /// <param name="state">Current game state</param>
        /// <param name="topN">Number of top coordinates to include</param>
        /// <returns>Debug string showing coordinate scores</returns>
        public string GetDebugScoreBreakdown(AIGameState state, int topN = 10)
        {
            List<(int row, int col)> unguessedCoords = state.GetUnguessedCoordinates();
            List<((int row, int col) coord, float score)> scoredCoords = new List<((int, int), float)>();

            foreach ((int row, int col) coord in unguessedCoords)
            {
                float score = CalculateCoordinateScore(coord.row, coord.col, state);
                scoredCoords.Add((coord, score));
            }

            scoredCoords.Sort((a, b) => b.score.CompareTo(a.score));

            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            sb.AppendLine("Coordinate Scores (top candidates):");

            int count = Mathf.Min(topN, scoredCoords.Count);
            for (int i = 0; i < count; i++)
            {
                (int row, int col) coord = scoredCoords[i].coord;
                string coordStr = GridAnalyzer.CoordinateToString(coord.row, coord.col);
                sb.AppendLine(string.Format("  {0}. {1}: {2:F2}", i + 1, coordStr, scoredCoords[i].score));
            }

            int poolSize = GetSelectionPoolSize(state.SkillLevel);
            sb.AppendLine(string.Format("Selection pool size: {0} (skill: {1:F2})", poolSize, state.SkillLevel));
            sb.AppendLine(string.Format("Fill ratio: {0:P1} ({1})", state.FillRatio, GridAnalyzer.GetDensityCategory(state.FillRatio)));

            return sb.ToString();
        }
    }
}

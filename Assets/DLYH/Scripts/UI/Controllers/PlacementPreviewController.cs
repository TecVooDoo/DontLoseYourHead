using System;
using System.Collections.Generic;
using UnityEngine;

namespace TecVooDoo.DontLoseYourHead.UI
{
    /// <summary>
    /// Handles visual preview of word placement during coordinate selection.
    /// Stateless helper class - receives all state via method parameters.
    /// Used internally by CoordinatePlacementController.
    /// </summary>
    public class PlacementPreviewController
    {
        private readonly IGridColorManager _colorManager;
        private readonly Func<int, int, GridCellUI> _getCell;
        private readonly Func<int, int, bool> _isValidCoordinate;

        /// <summary>
        /// Creates a new PlacementPreviewController.
        /// </summary>
        /// <param name="colorManager">Manages cell highlight colors</param>
        /// <param name="getCell">Function to get a cell by column and row</param>
        /// <param name="isValidCoordinate">Function to check if coordinate is valid</param>
        public PlacementPreviewController(
            IGridColorManager colorManager,
            Func<int, int, GridCellUI> getCell,
            Func<int, int, bool> isValidCoordinate)
        {
            _colorManager = colorManager ?? throw new ArgumentNullException(nameof(colorManager));
            _getCell = getCell ?? throw new ArgumentNullException(nameof(getCell));
            _isValidCoordinate = isValidCoordinate ?? throw new ArgumentNullException(nameof(isValidCoordinate));
        }

        /// <summary>
        /// Shows preview when selecting the first cell.
        /// Highlights hover cell as cursor and valid direction cells in green.
        /// </summary>
        public void ShowFirstCellPreview(
            int hoverCol,
            int hoverRow,
            List<Vector2Int> validDirections)
        {
            if (validDirections == null) return;

            // Highlight hover cell as cursor
            GridCellUI hoverCell = _getCell(hoverCol, hoverRow);
            if (hoverCell != null)
            {
                _colorManager.SetCellHighlight(hoverCell, GridHighlightType.Cursor);
            }

            // Highlight valid direction cells
            foreach (Vector2Int pos in validDirections)
            {
                GridCellUI cell = _getCell(pos.x, pos.y);
                if (cell != null)
                {
                    _colorManager.SetCellHighlight(cell, GridHighlightType.ValidPlacement);
                }
            }

            // Highlight invalid adjacent cells
            HighlightInvalidAdjacentCells(hoverCol, hoverRow, validDirections);
        }

        /// <summary>
        /// Shows preview when selecting direction (second cell).
        /// First cell stays as cursor, valid directions highlighted.
        /// If hovering over valid direction, shows full word preview.
        /// </summary>
        public void ShowDirectionPreview(
            int firstCol,
            int firstRow,
            int hoverCol,
            int hoverRow,
            string word,
            List<Vector2Int> validDirections)
        {
            if (validDirections == null || string.IsNullOrEmpty(word)) return;

            // First cell is cursor
            GridCellUI firstCell = _getCell(firstCol, firstRow);
            if (firstCell != null)
            {
                _colorManager.SetCellHighlight(firstCell, GridHighlightType.Cursor);
            }

            // Show valid direction cells
            foreach (Vector2Int pos in validDirections)
            {
                GridCellUI cell = _getCell(pos.x, pos.y);
                if (cell != null)
                {
                    _colorManager.SetCellHighlight(cell, GridHighlightType.ValidPlacement);
                }
            }

            // If hovering over a valid direction, preview the full word
            Vector2Int hoverPos = new Vector2Int(hoverCol, hoverRow);
            if (validDirections.Contains(hoverPos))
            {
                int dCol = hoverCol - firstCol;
                int dRow = hoverRow - firstRow;
                ShowWordPreview(firstCol, firstRow, dCol, dRow, word);
            }
        }

        /// <summary>
        /// Shows preview of the full word placement along a direction.
        /// </summary>
        public void ShowWordPreview(
            int startCol,
            int startRow,
            int dCol,
            int dRow,
            string word)
        {
            if (string.IsNullOrEmpty(word)) return;

            for (int i = 0; i < word.Length; i++)
            {
                int col = startCol + (i * dCol);
                int row = startRow + (i * dRow);

                GridCellUI cell = _getCell(col, row);
                if (cell != null)
                {
                    cell.SetLetter(word[i]);

                    // First cell is cursor color, rest are valid placement
                    GridHighlightType highlightType = (i == 0)
                        ? GridHighlightType.Cursor
                        : GridHighlightType.ValidPlacement;
                    _colorManager.SetCellHighlight(cell, highlightType);
                }
            }
        }

        /// <summary>
        /// Clears all placement preview highlighting.
        /// Restores permanently placed letters to their proper state.
        /// </summary>
        public void ClearAllPreviews(
            int gridSize,
            HashSet<Vector2Int> placedPositions,
            Dictionary<Vector2Int, char> placedLetters)
        {
            for (int col = 0; col < gridSize; col++)
            {
                for (int row = 0; row < gridSize; row++)
                {
                    GridCellUI cell = _getCell(col, row);
                    if (cell == null) continue;

                    // Clear highlighting
                    cell.ClearHighlight();

                    Vector2Int pos = new Vector2Int(col, row);

                    if (placedPositions != null && placedPositions.Contains(pos))
                    {
                        // Restore permanently placed letter
                        if (placedLetters != null && placedLetters.TryGetValue(pos, out char letter))
                        {
                            cell.SetLetter(letter);
                            cell.SetState(CellState.Filled);
                        }
                    }
                    else
                    {
                        // Clear any preview letters
                        cell.ClearLetter();
                    }
                }
            }
        }

        /// <summary>
        /// Highlights adjacent cells that are NOT valid directions as invalid.
        /// </summary>
        private void HighlightInvalidAdjacentCells(
            int centerCol,
            int centerRow,
            List<Vector2Int> validDirections)
        {
            // 8 directions
            int[] dCols = { 1, 0, 1, 1, -1, 0, -1, -1 };
            int[] dRows = { 0, 1, 1, -1, 0, -1, -1, 1 };

            for (int d = 0; d < 8; d++)
            {
                int adjCol = centerCol + dCols[d];
                int adjRow = centerRow + dRows[d];

                if (!_isValidCoordinate(adjCol, adjRow)) continue;

                Vector2Int adjPos = new Vector2Int(adjCol, adjRow);

                if (!validDirections.Contains(adjPos))
                {
                    GridCellUI cell = _getCell(adjCol, adjRow);
                    if (cell != null)
                    {
                        _colorManager.SetCellHighlight(cell, GridHighlightType.InvalidPlacement);
                    }
                }
            }
        }
    }
}

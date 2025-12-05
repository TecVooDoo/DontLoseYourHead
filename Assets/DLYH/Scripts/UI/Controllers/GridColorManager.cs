using UnityEngine;

namespace TecVooDoo.DontLoseYourHead.UI
{
    /// <summary>
    /// Manages grid cell colors and highlighting during placement mode.
    /// Plain class - no MonoBehaviour required.
    /// </summary>
    public class GridColorManager : IGridColorManager
    {
        private Color _cursorColor;
        private Color _validPlacementColor;
        private Color _invalidPlacementColor;
        private Color _placedLetterColor;

        public Color CursorColor
        {
            get => _cursorColor;
            set => _cursorColor = value;
        }

        public Color ValidPlacementColor
        {
            get => _validPlacementColor;
            set => _validPlacementColor = value;
        }

        public Color InvalidPlacementColor
        {
            get => _invalidPlacementColor;
            set => _invalidPlacementColor = value;
        }

        public Color PlacedLetterColor
        {
            get => _placedLetterColor;
            set => _placedLetterColor = value;
        }

        /// <summary>
        /// Creates a new GridColorManager with default colors.
        /// </summary>
        public GridColorManager()
        {
            _cursorColor = new Color(0.13f, 0.85f, 0.13f, 1f);       // Stoplight green
            _validPlacementColor = new Color(0.6f, 1f, 0.6f, 0.8f);  // Light mint green
            _invalidPlacementColor = new Color(1f, 0f, 0f, 0.7f);    // Red
            _placedLetterColor = new Color(0.5f, 0.8f, 1f, 1f);      // Light blue
        }

        /// <summary>
        /// Creates a new GridColorManager with custom colors.
        /// </summary>
        public GridColorManager(Color cursor, Color valid, Color invalid, Color placed)
        {
            _cursorColor = cursor;
            _validPlacementColor = valid;
            _invalidPlacementColor = invalid;
            _placedLetterColor = placed;
        }

        /// <summary>
        /// Sets a cell's highlight color based on the highlight type.
        /// </summary>
        public void SetCellHighlight(GridCellUI cell, GridHighlightType highlightType)
        {
            if (cell == null) return;

            switch (highlightType)
            {
                case GridHighlightType.Cursor:
                    cell.SetHighlightColor(_cursorColor);
                    break;
                case GridHighlightType.ValidPlacement:
                    cell.SetHighlightColor(_validPlacementColor);
                    break;
                case GridHighlightType.InvalidPlacement:
                    cell.SetHighlightColor(_invalidPlacementColor);
                    break;
                case GridHighlightType.PlacedLetter:
                    cell.SetHighlightColor(_placedLetterColor);
                    break;
                case GridHighlightType.None:
                default:
                    cell.ClearHighlight();
                    break;
            }
        }

        /// <summary>
        /// Clears a cell's highlight.
        /// </summary>
        public void ClearCellHighlight(GridCellUI cell)
        {
            if (cell == null) return;
            cell.ClearHighlight();
        }

        /// <summary>
        /// Clears all highlights from the grid.
        /// </summary>
        public void ClearAllHighlights(IGridDisplayController gridDisplay)
        {
            if (gridDisplay == null) return;

            int gridSize = gridDisplay.CurrentGridSize;
            for (int col = 0; col < gridSize; col++)
            {
                for (int row = 0; row < gridSize; row++)
                {
                    var cell = gridDisplay.GetCell(col, row);
                    if (cell != null)
                    {
                        cell.ClearHighlight();
                    }
                }
            }
        }

        /// <summary>
        /// Gets the color for a specific highlight type.
        /// </summary>
        public Color GetColorForType(GridHighlightType highlightType)
        {
            switch (highlightType)
            {
                case GridHighlightType.Cursor:
                    return _cursorColor;
                case GridHighlightType.ValidPlacement:
                    return _validPlacementColor;
                case GridHighlightType.InvalidPlacement:
                    return _invalidPlacementColor;
                case GridHighlightType.PlacedLetter:
                    return _placedLetterColor;
                default:
                    return Color.clear;
            }
        }
    }
}

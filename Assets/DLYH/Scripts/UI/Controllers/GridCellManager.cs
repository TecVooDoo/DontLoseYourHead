using UnityEngine;

namespace TecVooDoo.DontLoseYourHead.UI
{
    /// <summary>
    /// Manages the grid cell array and provides coordinate utilities.
    /// Handles cell storage, retrieval, and coordinate validation.
    /// </summary>
    public class GridCellManager
    {
        #region Constants
        public const int MAX_GRID_SIZE = 12;
        public const int MIN_GRID_SIZE = 6;
        #endregion

        #region Private Fields
        private readonly GridCellUI[,] _cells;
        #endregion

        #region Constructor
        public GridCellManager()
        {
            _cells = new GridCellUI[MAX_GRID_SIZE, MAX_GRID_SIZE];
        }
        #endregion

        #region Properties
        /// <summary>
        /// Direct access to cells array for GridLayoutManager population.
        /// </summary>
        public GridCellUI[,] Cells => _cells;
        #endregion

        #region Public Methods - Cell Access
        /// <summary>
        /// Gets the cell at the specified column and row.
        /// </summary>
        /// <param name="column">Column index (0-based)</param>
        /// <param name="row">Row index (0-based)</param>
        /// <returns>The cell at the position, or null if out of bounds</returns>
        public GridCellUI GetCell(int column, int row)
        {
            if (column >= 0 && column < MAX_GRID_SIZE && row >= 0 && row < MAX_GRID_SIZE)
            {
                return _cells[column, row];
            }
            return null;
        }

        /// <summary>
        /// Sets a cell at the specified position.
        /// Used by GridLayoutManager during cell creation.
        /// </summary>
        /// <param name="column">Column index (0-based)</param>
        /// <param name="row">Row index (0-based)</param>
        /// <param name="cell">The cell to place</param>
        public void SetCell(int column, int row, GridCellUI cell)
        {
            if (column >= 0 && column < MAX_GRID_SIZE && row >= 0 && row < MAX_GRID_SIZE)
            {
                _cells[column, row] = cell;
            }
        }
        #endregion

        #region Public Methods - Coordinate Utilities
        /// <summary>
        /// Checks if the given coordinates are within the current grid bounds.
        /// </summary>
        /// <param name="column">Column index to check</param>
        /// <param name="row">Row index to check</param>
        /// <param name="currentGridSize">The current grid size</param>
        /// <returns>True if coordinates are valid</returns>
        public bool IsValidCoordinate(int column, int row, int currentGridSize)
        {
            return column >= 0 && column < currentGridSize && row >= 0 && row < currentGridSize;
        }

        /// <summary>
        /// Converts a column index to its letter representation (A-L).
        /// </summary>
        /// <param name="column">Column index (0 = A, 1 = B, etc.)</param>
        /// <returns>The column letter</returns>
        public char GetColumnLetter(int column)
        {
            return (char)('A' + column);
        }

        /// <summary>
        /// Converts a column letter to its index.
        /// </summary>
        /// <param name="letter">Column letter (A-L)</param>
        /// <returns>The column index (0-based)</returns>
        public int GetColumnIndex(char letter)
        {
            return char.ToUpper(letter) - 'A';
        }
        #endregion

        #region Public Methods - Grid Management
        /// <summary>
        /// Clears all cell references from the array.
        /// Does not destroy GameObjects - that's GridLayoutManager's job.
        /// </summary>
        public void ClearCellArray()
        {
            for (int col = 0; col < MAX_GRID_SIZE; col++)
            {
                for (int row = 0; row < MAX_GRID_SIZE; row++)
                {
                    _cells[col, row] = null;
                }
            }
        }

        /// <summary>
        /// Gets the count of non-null cells in the array.
        /// Useful for debugging.
        /// </summary>
        /// <returns>Number of cells currently stored</returns>
        public int GetCellCount()
        {
            int count = 0;
            for (int col = 0; col < MAX_GRID_SIZE; col++)
            {
                for (int row = 0; row < MAX_GRID_SIZE; row++)
                {
                    if (_cells[col, row] != null)
                    {
                        count++;
                    }
                }
            }
            return count;
        }

        /// <summary>
        /// Sets the hit color for all grid cells.
        /// Called during panel setup to apply the guesser's chosen color.
        /// </summary>
        public void SetHitColor(Color color)
        {
            for (int col = 0; col < MAX_GRID_SIZE; col++)
            {
                for (int row = 0; row < MAX_GRID_SIZE; row++)
                {
                    if (_cells[col, row] != null)
                    {
                        _cells[col, row].SetHitColor(color);
                    }
                }
            }
        }
        #endregion
    }
}

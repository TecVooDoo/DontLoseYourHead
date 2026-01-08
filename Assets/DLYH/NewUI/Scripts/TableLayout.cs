namespace DLYH.TableUI
{
    /// <summary>
    /// Defines the layout structure of a table, mapping logical regions to cell coordinates.
    /// Provides factory methods for creating setup and gameplay layouts.
    ///
    /// Layout Formula:
    ///   Rows = wordCount + 1 (column header row) + gridSize
    ///   Cols = 1 (row header column) + gridSize
    /// </summary>
    public class TableLayout
    {
        /// <summary>Total rows in the table.</summary>
        public int TotalRows { get; private set; }

        /// <summary>Total columns in the table.</summary>
        public int TotalCols { get; private set; }

        /// <summary>Grid size (e.g., 6 for 6x6).</summary>
        public int GridSize { get; private set; }

        /// <summary>Number of words.</summary>
        public int WordCount { get; private set; }

        /// <summary>Region containing word rows.</summary>
        public TableRegion WordRowsRegion { get; private set; }

        /// <summary>Region containing column headers (A, B, C...).</summary>
        public TableRegion ColHeaderRegion { get; private set; }

        /// <summary>Region containing row headers (1, 2, 3...).</summary>
        public TableRegion RowHeaderRegion { get; private set; }

        /// <summary>Region containing the playable grid.</summary>
        public TableRegion GridRegion { get; private set; }

        /// <summary>
        /// Creates a layout for the setup phase.
        /// Word rows at top, then column headers, then grid with row headers.
        /// </summary>
        public static TableLayout CreateForSetup(int gridSize, int wordCount)
        {
            TableLayout layout = new TableLayout();
            layout.GridSize = gridSize;
            layout.WordCount = wordCount;

            // Calculate total dimensions
            // Rows: wordCount word rows + 1 column header row + gridSize grid rows
            // Cols: 1 row header column + gridSize grid columns
            layout.TotalRows = wordCount + 1 + gridSize;
            layout.TotalCols = 1 + gridSize;

            // Word rows at top (rows 0 to wordCount-1)
            // Column 0 is spacer (where row headers will be for grid)
            // Columns 1 to gridSize are word slots
            layout.WordRowsRegion = new TableRegion(
                "WordRows",
                rowStart: 0,
                colStart: 1,
                rowCount: wordCount,
                colCount: gridSize
            );

            // Column headers in row after word rows
            // Column 0 is spacer, columns 1 to gridSize are A, B, C...
            layout.ColHeaderRegion = new TableRegion(
                "ColHeaders",
                rowStart: wordCount,
                colStart: 1,
                rowCount: 1,
                colCount: gridSize
            );

            // Row headers in column 0, starting after column header row
            layout.RowHeaderRegion = new TableRegion(
                "RowHeaders",
                rowStart: wordCount + 1,
                colStart: 0,
                rowCount: gridSize,
                colCount: 1
            );

            // Grid cells
            layout.GridRegion = new TableRegion(
                "Grid",
                rowStart: wordCount + 1,
                colStart: 1,
                rowCount: gridSize,
                colCount: gridSize
            );

            return layout;
        }

        /// <summary>
        /// Creates a layout for the gameplay phase.
        /// Same structure as setup - word rows, headers, grid.
        /// </summary>
        public static TableLayout CreateForGameplay(int gridSize, int wordCount)
        {
            // Gameplay uses the same layout structure as setup
            return CreateForSetup(gridSize, wordCount);
        }

        /// <summary>
        /// Gets the region that contains the given coordinates, or null if none.
        /// </summary>
        public TableRegion? GetRegionAt(int row, int col)
        {
            if (WordRowsRegion.Contains(row, col))
            {
                return WordRowsRegion;
            }
            if (ColHeaderRegion.Contains(row, col))
            {
                return ColHeaderRegion;
            }
            if (RowHeaderRegion.Contains(row, col))
            {
                return RowHeaderRegion;
            }
            if (GridRegion.Contains(row, col))
            {
                return GridRegion;
            }
            return null;
        }

        /// <summary>
        /// Returns true if the coordinates are within the playable grid.
        /// </summary>
        public bool IsInGrid(int row, int col)
        {
            return GridRegion.Contains(row, col);
        }

        /// <summary>
        /// Returns true if the coordinates are within word rows.
        /// </summary>
        public bool IsInWordRows(int row, int col)
        {
            return WordRowsRegion.Contains(row, col);
        }

        /// <summary>
        /// Converts grid-local coordinates (0-based row/col within grid) to table coordinates.
        /// </summary>
        public (int tableRow, int tableCol) GridToTable(int gridRow, int gridCol)
        {
            return GridRegion.ToTable(gridRow, gridCol);
        }

        /// <summary>
        /// Converts table coordinates to grid-local coordinates.
        /// Returns (-1, -1) if not in grid.
        /// </summary>
        public (int gridRow, int gridCol) TableToGrid(int tableRow, int tableCol)
        {
            return GridRegion.ToLocal(tableRow, tableCol);
        }

        /// <summary>
        /// Converts word row index and letter position to table coordinates.
        /// </summary>
        public (int tableRow, int tableCol) WordSlotToTable(int wordIndex, int letterIndex)
        {
            return WordRowsRegion.ToTable(wordIndex, letterIndex);
        }

        /// <summary>
        /// Converts table coordinates to word row index and letter position.
        /// Returns (-1, -1) if not in word rows.
        /// </summary>
        public (int wordIndex, int letterIndex) TableToWordSlot(int tableRow, int tableCol)
        {
            return WordRowsRegion.ToLocal(tableRow, tableCol);
        }

        /// <summary>
        /// Gets the column header character for a grid column (0 = A, 1 = B, etc.).
        /// </summary>
        public static char GetColumnHeaderChar(int gridCol)
        {
            return (char)('A' + gridCol);
        }

        /// <summary>
        /// Gets the row header number for a grid row (0 = 1, 1 = 2, etc.).
        /// </summary>
        public static int GetRowHeaderNumber(int gridRow)
        {
            return gridRow + 1;
        }
    }
}

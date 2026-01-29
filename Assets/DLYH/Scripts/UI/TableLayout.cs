namespace DLYH.TableUI
{
    /// <summary>
    /// Defines the layout structure of a table, mapping logical regions to cell coordinates.
    /// Provides factory methods for creating setup and gameplay layouts.
    ///
    /// Layout (grid table only - word rows are separate):
    ///   Row 0: Column headers (A, B, C...)
    ///   Rows 1+: Grid cells with row headers (1, 2, 3...)
    ///
    ///   TotalRows = 1 (column header) + gridSize
    ///   TotalCols = 1 (row header) + gridSize 
    ///
    /// Word rows are managed separately via WordLengths array.
    /// Standard word lengths: word 1 = 3 letters, word 2 = 4, word 3 = 5, word 4 = 6.
    /// </summary>
    public class TableLayout
    {
        /// <summary>Total rows in the grid table (NOT including word rows).</summary>
        public int TotalRows { get; private set; }

        /// <summary>Total columns in the grid table.</summary>
        public int TotalCols { get; private set; }

        /// <summary>Grid size (e.g., 6 for 6x6).</summary>
        public int GridSize { get; private set; }

        /// <summary>Number of words.</summary>
        public int WordCount { get; private set; }

        /// <summary>Length of each word (index 0 = word 1, etc.).</summary>
        public int[] WordLengths { get; private set; }

        /// <summary>Region containing column headers (A, B, C...).</summary>
        public TableRegion ColHeaderRegion { get; private set; }

        /// <summary>Region containing row headers (1, 2, 3...).</summary>
        public TableRegion RowHeaderRegion { get; private set; }

        /// <summary>Region containing the playable grid.</summary>
        public TableRegion GridRegion { get; private set; }

        /// <summary>
        /// Creates a layout for the setup phase.
        /// Grid table has column headers in row 0, then grid cells below.
        /// Word rows are separate - use WordLengths for their sizes.
        /// </summary>
        public static TableLayout CreateForSetup(int gridSize, int wordCount)
        {
            TableLayout layout = new TableLayout();
            layout.GridSize = gridSize;
            layout.WordCount = wordCount;

            // Standard word lengths: 3, 4, 5, 6
            layout.WordLengths = GetStandardWordLengths(wordCount);

            // Calculate grid table dimensions (word rows are separate now)
            // Rows: 1 column header row + gridSize grid rows
            // Cols: 1 row header column + gridSize grid columns
            layout.TotalRows = 1 + gridSize;
            layout.TotalCols = 1 + gridSize;

            // Column headers in row 0
            // Column 0 is spacer (corner), columns 1 to gridSize are A, B, C...
            layout.ColHeaderRegion = new TableRegion(
                "ColHeaders",
                rowStart: 0,
                colStart: 1,
                rowCount: 1,
                colCount: gridSize
            );

            // Row headers in column 0, starting at row 1
            layout.RowHeaderRegion = new TableRegion(
                "RowHeaders",
                rowStart: 1,
                colStart: 0,
                rowCount: gridSize,
                colCount: 1
            );

            // Grid cells starting at row 1, column 1
            layout.GridRegion = new TableRegion(
                "Grid",
                rowStart: 1,
                colStart: 1,
                rowCount: gridSize,
                colCount: gridSize
            );

            return layout;
        }

        /// <summary>
        /// Creates a layout for the gameplay phase.
        /// Same structure as setup.
        /// </summary>
        public static TableLayout CreateForGameplay(int gridSize, int wordCount)
        {
            return CreateForSetup(gridSize, wordCount);
        }

        /// <summary>
        /// Gets the standard word lengths for a given word count.
        /// Word 1 = 3 letters, Word 2 = 4 letters, Word 3 = 5 letters, Word 4 = 6 letters.
        /// </summary>
        public static int[] GetStandardWordLengths(int wordCount)
        {
            int[] lengths = new int[wordCount];
            for (int i = 0; i < wordCount; i++)
            {
                lengths[i] = 3 + i; // 3, 4, 5, 6...
            }
            return lengths;
        }

        /// <summary>
        /// Gets the word length for a specific word index.
        /// </summary>
        public int GetWordLength(int wordIndex)
        {
            if (wordIndex < 0 || wordIndex >= WordCount || WordLengths == null)
            {
                return 0;
            }
            return WordLengths[wordIndex];
        }

        /// <summary>
        /// Gets the region that contains the given coordinates, or null if none.
        /// </summary>
        public TableRegion? GetRegionAt(int row, int col)
        {
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

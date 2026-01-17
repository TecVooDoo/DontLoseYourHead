namespace DLYH.TableUI
{
    /// <summary>
    /// Defines a named rectangular region within the table.
    /// Used to map logical areas (word rows, grid, headers) to cell coordinates.
    /// </summary>
    public struct TableRegion
    {
        /// <summary>Identifier for this region.</summary>
        public string Name;

        /// <summary>Starting row (0-indexed).</summary>
        public int RowStart;

        /// <summary>Starting column (0-indexed).</summary>
        public int ColStart;

        /// <summary>Number of rows in this region.</summary>
        public int RowCount;

        /// <summary>Number of columns in this region.</summary>
        public int ColCount;

        /// <summary>
        /// Creates a new table region.
        /// </summary>
        public TableRegion(string name, int rowStart, int colStart, int rowCount, int colCount)
        {
            Name = name;
            RowStart = rowStart;
            ColStart = colStart;
            RowCount = rowCount;
            ColCount = colCount;
        }

        /// <summary>
        /// Returns the ending row (exclusive).
        /// </summary>
        public int RowEnd => RowStart + RowCount;

        /// <summary>
        /// Returns the ending column (exclusive).
        /// </summary>
        public int ColEnd => ColStart + ColCount;

        /// <summary>
        /// Returns true if the given coordinates are within this region.
        /// </summary>
        public bool Contains(int row, int col)
        {
            return row >= RowStart && row < RowEnd &&
                   col >= ColStart && col < ColEnd;
        }

        /// <summary>
        /// Converts table coordinates to region-local coordinates.
        /// Returns (-1, -1) if coordinates are outside this region.
        /// </summary>
        public (int localRow, int localCol) ToLocal(int row, int col)
        {
            if (!Contains(row, col))
            {
                return (-1, -1);
            }
            return (row - RowStart, col - ColStart);
        }

        /// <summary>
        /// Converts region-local coordinates to table coordinates.
        /// </summary>
        public (int tableRow, int tableCol) ToTable(int localRow, int localCol)
        {
            return (RowStart + localRow, ColStart + localCol);
        }

        /// <summary>
        /// Returns the total number of cells in this region.
        /// </summary>
        public int CellCount => RowCount * ColCount;
    }
}

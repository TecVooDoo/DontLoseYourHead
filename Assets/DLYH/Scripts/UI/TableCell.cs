namespace DLYH.TableUI
{
    /// <summary>
    /// Represents a single cell in the table model. 
    /// Struct for memory efficiency - no heap allocations per cell. 
    /// </summary>
    public struct TableCell
    {
        /// <summary>What type of cell this is.</summary>
        public TableCellKind Kind;

        /// <summary>Current visual/interaction state.</summary>
        public TableCellState State;

        /// <summary>Who owns this cell's content.</summary>
        public CellOwner Owner;

        /// <summary>
        /// Single character content (letters, column headers A-Z).
        /// Use '\0' for empty/no character.
        /// </summary>
        public char TextChar;

        /// <summary>
        /// Integer value for row headers (1, 2, 3...).
        /// Use -1 for non-numeric cells.
        /// </summary>
        public int IntValue;

        /// <summary>Row position in the table (0-indexed).</summary>
        public int Row;

        /// <summary>Column position in the table (0-indexed).</summary>
        public int Col;

        /// <summary>
        /// Creates a new table cell with default values.
        /// </summary>
        public static TableCell Create(int row, int col)
        {
            return new TableCell
            {
                Kind = TableCellKind.Spacer,
                State = TableCellState.None,
                Owner = CellOwner.None,
                TextChar = '\0',
                IntValue = -1,
                Row = row,
                Col = col
            };
        }

        /// <summary>
        /// Creates a spacer cell.
        /// </summary>
        public static TableCell CreateSpacer(int row, int col)
        {
            return new TableCell
            {
                Kind = TableCellKind.Spacer,
                State = TableCellState.None,
                Owner = CellOwner.None,
                TextChar = '\0',
                IntValue = -1,
                Row = row,
                Col = col
            };
        }

        /// <summary>
        /// Creates a column header cell (A, B, C...).
        /// </summary>
        public static TableCell CreateColumnHeader(int row, int col, char letter)
        {
            return new TableCell
            {
                Kind = TableCellKind.HeaderCol,
                State = TableCellState.ReadOnly,
                Owner = CellOwner.None,
                TextChar = letter,
                IntValue = -1,
                Row = row,
                Col = col
            };
        }

        /// <summary>
        /// Creates a row header cell (1, 2, 3...).
        /// </summary>
        public static TableCell CreateRowHeader(int row, int col, int number)
        {
            return new TableCell
            {
                Kind = TableCellKind.HeaderRow,
                State = TableCellState.ReadOnly,
                Owner = CellOwner.None,
                TextChar = '\0',
                IntValue = number,
                Row = row,
                Col = col
            };
        }

        /// <summary>
        /// Creates a word slot cell.
        /// </summary>
        public static TableCell CreateWordSlot(int row, int col, char letter = '\0')
        {
            return new TableCell
            {
                Kind = TableCellKind.WordSlot,
                State = TableCellState.Normal,
                Owner = CellOwner.None,
                TextChar = letter,
                IntValue = -1,
                Row = row,
                Col = col
            };
        }

        /// <summary>
        /// Creates a grid cell.
        /// </summary>
        public static TableCell CreateGridCell(int row, int col, CellOwner owner = CellOwner.None)
        {
            return new TableCell
            {
                Kind = TableCellKind.GridCell,
                State = TableCellState.Fog,
                Owner = owner,
                TextChar = '\0',
                IntValue = -1,
                Row = row,
                Col = col
            };
        }

        /// <summary>
        /// Returns true if this cell has displayable text content.
        /// </summary>
        public bool HasText()
        {
            return TextChar != '\0' || IntValue >= 0;
        }

        /// <summary>
        /// Gets the display text for this cell.
        /// </summary>
        public string GetDisplayText()
        {
            if (TextChar != '\0')
            {
                return TextChar.ToString();
            }
            if (IntValue >= 0)
            {
                return IntValue.ToString();
            }
            return string.Empty;
        }
    }
}

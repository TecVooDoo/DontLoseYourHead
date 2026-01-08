using System;

namespace DLYH.TableUI
{
    /// <summary>
    /// The data model for a table UI. Contains all cell data and tracks changes.
    /// Designed to be allocated once and reused via Clear() to avoid GC pressure.
    /// No Unity UI dependencies - can be unit tested independently.
    /// </summary>
    public class TableModel
    {
        private TableCell[,] _cells;
        private TableLayout _layout;
        private int _version;
        private bool _dirty;

        /// <summary>Total rows in the table.</summary>
        public int Rows => _layout?.TotalRows ?? 0;

        /// <summary>Total columns in the table.</summary>
        public int Cols => _layout?.TotalCols ?? 0;

        /// <summary>
        /// Version number that increments on each change.
        /// View can use this to detect if model has changed.
        /// </summary>
        public int Version => _version;

        /// <summary>
        /// True if model has been modified since last ClearDirty() call.
        /// </summary>
        public bool Dirty => _dirty;

        /// <summary>
        /// The layout defining regions within this table.
        /// </summary>
        public TableLayout Layout => _layout;

        /// <summary>
        /// Event fired when any cell changes. Parameters: row, col, newCell.
        /// </summary>
        public event Action<int, int, TableCell> OnCellChanged;

        /// <summary>
        /// Event fired when the model is cleared/reset.
        /// </summary>
        public event Action OnCleared;

        /// <summary>
        /// Creates a new table model. Call Initialize() before use.
        /// </summary>
        public TableModel()
        {
            _version = 0;
            _dirty = false;
        }

        /// <summary>
        /// Initializes the model with a new layout.
        /// Allocates cell array and populates with default cells based on layout.
        /// </summary>
        public void Initialize(TableLayout layout)
        {
            _layout = layout ?? throw new ArgumentNullException(nameof(layout));

            // Allocate cell array
            _cells = new TableCell[layout.TotalRows, layout.TotalCols];

            // Initialize all cells based on layout
            PopulateCells();

            _version++;
            _dirty = true;
            OnCleared?.Invoke();
        }

        /// <summary>
        /// Clears all cell states to defaults without reallocating.
        /// Use this to reset for a new game.
        /// </summary>
        public void Clear()
        {
            if (_layout == null || _cells == null)
            {
                return;
            }

            PopulateCells();

            _version++;
            _dirty = true;
            OnCleared?.Invoke();
        }

        /// <summary>
        /// Populates cells based on the current layout.
        /// </summary>
        private void PopulateCells()
        {
            for (int row = 0; row < _layout.TotalRows; row++)
            {
                for (int col = 0; col < _layout.TotalCols; col++)
                {
                    _cells[row, col] = CreateCellForPosition(row, col);
                }
            }
        }

        /// <summary>
        /// Creates the appropriate cell type for a given position.
        /// </summary>
        private TableCell CreateCellForPosition(int row, int col)
        {
            // Check if in grid region
            if (_layout.GridRegion.Contains(row, col))
            {
                return TableCell.CreateGridCell(row, col);
            }

            // Check if in column header region
            if (_layout.ColHeaderRegion.Contains(row, col))
            {
                (int _, int localCol) = _layout.ColHeaderRegion.ToLocal(row, col);
                char headerChar = TableLayout.GetColumnHeaderChar(localCol);
                return TableCell.CreateColumnHeader(row, col, headerChar);
            }

            // Check if in row header region
            if (_layout.RowHeaderRegion.Contains(row, col))
            {
                (int localRow, int _) = _layout.RowHeaderRegion.ToLocal(row, col);
                int headerNum = TableLayout.GetRowHeaderNumber(localRow);
                return TableCell.CreateRowHeader(row, col, headerNum);
            }

            // Check if in word rows region
            if (_layout.WordRowsRegion.Contains(row, col))
            {
                return TableCell.CreateWordSlot(row, col);
            }

            // Default to spacer (e.g., top-left corner)
            return TableCell.CreateSpacer(row, col);
        }

        /// <summary>
        /// Gets the cell at the specified coordinates.
        /// </summary>
        public TableCell GetCell(int row, int col)
        {
            ValidateCoordinates(row, col);
            return _cells[row, col];
        }

        /// <summary>
        /// Sets the entire cell at the specified coordinates.
        /// </summary>
        public void SetCell(int row, int col, TableCell cell)
        {
            ValidateCoordinates(row, col);
            cell.Row = row;
            cell.Col = col;
            _cells[row, col] = cell;
            MarkChanged(row, col);
        }

        /// <summary>
        /// Sets the character content of a cell.
        /// </summary>
        public void SetCellChar(int row, int col, char c)
        {
            ValidateCoordinates(row, col);
            _cells[row, col].TextChar = c;
            MarkChanged(row, col);
        }

        /// <summary>
        /// Sets the state of a cell.
        /// </summary>
        public void SetCellState(int row, int col, TableCellState state)
        {
            ValidateCoordinates(row, col);
            _cells[row, col].State = state;
            MarkChanged(row, col);
        }

        /// <summary>
        /// Sets the kind of a cell.
        /// </summary>
        public void SetCellKind(int row, int col, TableCellKind kind)
        {
            ValidateCoordinates(row, col);
            _cells[row, col].Kind = kind;
            MarkChanged(row, col);
        }

        /// <summary>
        /// Sets the owner of a cell.
        /// </summary>
        public void SetCellOwner(int row, int col, CellOwner owner)
        {
            ValidateCoordinates(row, col);
            _cells[row, col].Owner = owner;
            MarkChanged(row, col);
        }

        /// <summary>
        /// Sets both character and state in a single call.
        /// </summary>
        public void SetCellCharAndState(int row, int col, char c, TableCellState state)
        {
            ValidateCoordinates(row, col);
            _cells[row, col].TextChar = c;
            _cells[row, col].State = state;
            MarkChanged(row, col);
        }

        /// <summary>
        /// Sets a word slot cell's letter.
        /// </summary>
        public void SetWordSlotLetter(int wordIndex, int letterIndex, char letter)
        {
            (int row, int col) = _layout.WordSlotToTable(wordIndex, letterIndex);
            SetCellChar(row, col, letter);
        }

        /// <summary>
        /// Sets a word slot cell's state.
        /// </summary>
        public void SetWordSlotState(int wordIndex, int letterIndex, TableCellState state)
        {
            (int row, int col) = _layout.WordSlotToTable(wordIndex, letterIndex);
            SetCellState(row, col, state);
        }

        /// <summary>
        /// Sets a grid cell's letter.
        /// </summary>
        public void SetGridCellLetter(int gridRow, int gridCol, char letter)
        {
            (int row, int col) = _layout.GridToTable(gridRow, gridCol);
            SetCellChar(row, col, letter);
        }

        /// <summary>
        /// Sets a grid cell's state.
        /// </summary>
        public void SetGridCellState(int gridRow, int gridCol, TableCellState state)
        {
            (int row, int col) = _layout.GridToTable(gridRow, gridCol);
            SetCellState(row, col, state);
        }

        /// <summary>
        /// Sets a grid cell's owner.
        /// </summary>
        public void SetGridCellOwner(int gridRow, int gridCol, CellOwner owner)
        {
            (int row, int col) = _layout.GridToTable(gridRow, gridCol);
            SetCellOwner(row, col, owner);
        }

        /// <summary>
        /// Gets a grid cell using grid-local coordinates.
        /// </summary>
        public TableCell GetGridCell(int gridRow, int gridCol)
        {
            (int row, int col) = _layout.GridToTable(gridRow, gridCol);
            return GetCell(row, col);
        }

        /// <summary>
        /// Gets a word slot cell using word/letter indices.
        /// </summary>
        public TableCell GetWordSlot(int wordIndex, int letterIndex)
        {
            (int row, int col) = _layout.WordSlotToTable(wordIndex, letterIndex);
            return GetCell(row, col);
        }

        /// <summary>
        /// Clears the dirty flag. Call after view has processed changes.
        /// </summary>
        public void ClearDirty()
        {
            _dirty = false;
        }

        /// <summary>
        /// Marks the model as dirty and increments version.
        /// </summary>
        public void MarkDirty()
        {
            _version++;
            _dirty = true;
        }

        private void MarkChanged(int row, int col)
        {
            _version++;
            _dirty = true;
            OnCellChanged?.Invoke(row, col, _cells[row, col]);
        }

        private void ValidateCoordinates(int row, int col)
        {
            if (_cells == null)
            {
                throw new InvalidOperationException("TableModel not initialized. Call Initialize() first.");
            }
            if (row < 0 || row >= Rows || col < 0 || col >= Cols)
            {
                throw new ArgumentOutOfRangeException(
                    $"Coordinates ({row}, {col}) out of bounds for table of size ({Rows}, {Cols})");
            }
        }
    }
}

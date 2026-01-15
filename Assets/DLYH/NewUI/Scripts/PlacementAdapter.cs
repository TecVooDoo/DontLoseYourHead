using System;
using System.Collections.Generic;
using UnityEngine;

namespace DLYH.TableUI
{
    /// <summary>
    /// Placement state for table-based word placement.
    /// </summary>
    public enum TablePlacementState
    {
        Inactive,
        SelectingFirstCell,
        SelectingDirection
    }

    /// <summary>
    /// Adapts the UI Toolkit table system for word placement.
    /// Works directly with TableModel instead of requiring GridCellUI instances.
    /// Replicates the 8-direction placement logic from CoordinatePlacementController.
    /// </summary>
    public class PlacementAdapter : IDisposable
    {
        private readonly TableView _tableView;
        private readonly TableModel _tableModel;
        private readonly TableLayout _tableLayout;
        private readonly WordRowsContainer _wordRowsContainer;

        private TablePlacementController _placementController;

        /// <summary>
        /// Event fired when a word is successfully placed on the grid.
        /// Parameters: word row index, word text, list of grid positions.
        /// </summary>
        public event Action<int, string, List<Vector2Int>> OnWordPlaced;

        /// <summary>
        /// Event fired when placement mode is cancelled.
        /// </summary>
        public event Action OnPlacementCancelled;

        /// <summary>
        /// Returns true if currently in placement mode.
        /// </summary>
        public bool IsInPlacementMode => _placementController?.IsInPlacementMode ?? false;

        /// <summary>
        /// The current placement state.
        /// </summary>
        public TablePlacementState CurrentPlacementState => _placementController?.CurrentPlacementState ?? TablePlacementState.Inactive;

        /// <summary>
        /// The word row index currently being placed, or -1 if not in placement mode.
        /// </summary>
        public int PlacementWordRowIndex => _placementController?.PlacementWordRowIndex ?? -1;

        /// <summary>
        /// Gets all positions where letters have been placed.
        /// </summary>
        public IReadOnlyCollection<Vector2Int> AllPlacedPositions => _placementController?.AllPlacedPositions;

        /// <summary>
        /// Gets a dictionary of all placed letters by position.
        /// </summary>
        public IReadOnlyDictionary<Vector2Int, char> PlacedLetters => _placementController?.PlacedLetters;

        /// <summary>
        /// Creates a new PlacementAdapter.
        /// </summary>
        public PlacementAdapter(
            TableView tableView,
            TableModel tableModel,
            TableLayout tableLayout,
            WordRowsContainer wordRowsContainer)
        {
            _tableView = tableView;
            _tableModel = tableModel;
            _tableLayout = tableLayout;
            _wordRowsContainer = wordRowsContainer;

            Initialize();
        }

        private void Initialize()
        {
            _placementController = new TablePlacementController(_tableModel, _tableLayout);
            _placementController.OnWordPlaced += HandleWordPlaced;
            _placementController.OnPlacementCancelled += HandlePlacementCancelled;
        }

        /// <summary>
        /// Enters placement mode for a specific word.
        /// </summary>
        public void EnterPlacementMode(int wordRowIndex, string word)
        {
            if (string.IsNullOrEmpty(word))
            {
                Debug.LogError("[PlacementAdapter] Cannot enter placement mode - no word provided");
                return;
            }

            _placementController.EnterPlacementMode(wordRowIndex, word);
        }

        /// <summary>
        /// Cancels placement mode.
        /// </summary>
        public void CancelPlacementMode()
        {
            _placementController.CancelPlacementMode();
        }

        /// <summary>
        /// Handles a grid cell click during placement mode.
        /// </summary>
        /// <returns>True if the click was handled by placement mode.</returns>
        public bool HandleCellClick(int gridCol, int gridRow)
        {
            return _placementController.HandleCellClick(gridCol, gridRow);
        }

        /// <summary>
        /// Updates the placement preview when hovering over a cell.
        /// </summary>
        public void UpdatePlacementPreview(int gridCol, int gridRow)
        {
            _placementController.UpdatePlacementPreview(gridCol, gridRow);
        }

        /// <summary>
        /// Places the current word randomly on the grid.
        /// </summary>
        public bool PlaceWordRandomly()
        {
            return _placementController.PlaceWordRandomly();
        }

        /// <summary>
        /// Places a specific word randomly on the grid.
        /// Enters placement mode, places randomly, then exits.
        /// </summary>
        public bool PlaceWordRandomly(int wordRowIndex, string word)
        {
            if (string.IsNullOrEmpty(word))
            {
                Debug.LogWarning($"[PlacementAdapter] Cannot place word at row {wordRowIndex} - word is empty");
                return false;
            }

            _placementController.EnterPlacementMode(wordRowIndex, word);
            bool success = _placementController.PlaceWordRandomly();

            if (!success)
            {
                _placementController.CancelPlacementMode();
            }

            return success;
        }

        /// <summary>
        /// Clears a word from the grid.
        /// </summary>
        public void ClearWordFromGrid(int wordRowIndex)
        {
            _placementController.ClearWordFromGrid(wordRowIndex);
        }

        /// <summary>
        /// Clears all placed words from the grid.
        /// </summary>
        public void ClearAllPlacedWords()
        {
            _placementController.ClearAllPlacedWords();
        }

        /// <summary>
        /// Gets the positions for a specific word row.
        /// </summary>
        public List<Vector2Int> GetWordPositions(int rowIndex)
        {
            return _placementController.GetWordPositions(rowIndex);
        }

        /// <summary>
        /// Gets the word placement data for all placed words.
        /// Returns a list of tuples: (rowIndex, word, startCol, startRow, dirCol, dirRow)
        /// </summary>
        public List<(int rowIndex, string word, int startCol, int startRow, int dCol, int dRow)> GetAllWordPlacements()
        {
            return _placementController.GetAllWordPlacements();
        }

        /// <summary>
        /// Checks if a position has a placed letter.
        /// </summary>
        public bool HasPlacedLetter(int gridCol, int gridRow)
        {
            return _placementController.HasPlacedLetter(gridCol, gridRow);
        }

        /// <summary>
        /// Gets the letter at a specific grid position.
        /// </summary>
        public char GetLetterAtPosition(int gridCol, int gridRow)
        {
            return _placementController.GetLetterAtPosition(new Vector2Int(gridCol, gridRow));
        }

        private void HandleWordPlaced(int rowIndex, string word, List<Vector2Int> positions)
        {
            // Mark word row as placed
            _wordRowsContainer?.SetWordPlaced(rowIndex, true);

            // Forward event
            OnWordPlaced?.Invoke(rowIndex, word, positions);
        }

        private void HandlePlacementCancelled()
        {
            OnPlacementCancelled?.Invoke();
        }

        public void Dispose()
        {
            if (_placementController != null)
            {
                _placementController.OnWordPlaced -= HandleWordPlaced;
                _placementController.OnPlacementCancelled -= HandlePlacementCancelled;
            }
        }
    }

    /// <summary>
    /// Handles word placement logic for the UI Toolkit table system.
    /// Works directly with TableModel - no GridCellUI dependency.
    /// Replicates 8-direction placement from the original CoordinatePlacementController.
    /// </summary>
    public class TablePlacementController
    {
        private readonly TableModel _tableModel;
        private readonly TableLayout _tableLayout;

        private TablePlacementState _placementState = TablePlacementState.Inactive;
        private int _placementWordRowIndex = -1;
        private string _placementWord = "";
        private int _firstCellCol = -1;
        private int _firstCellRow = -1;

        private readonly List<Vector2Int> _placedCellPositions = new List<Vector2Int>();
        private readonly HashSet<Vector2Int> _allPlacedPositions = new HashSet<Vector2Int>();
        private readonly Dictionary<Vector2Int, char> _placedLetters = new Dictionary<Vector2Int, char>();
        private readonly Dictionary<int, List<Vector2Int>> _wordRowPositions = new Dictionary<int, List<Vector2Int>>();

        // Store word placement data for each row (word, start position, direction)
        private readonly Dictionary<int, (string word, int startCol, int startRow, int dCol, int dRow)> _wordPlacementData =
            new Dictionary<int, (string, int, int, int, int)>();

        // 8 directions: E, S, SE, NE, W, N, NW, SW
        private static readonly int[] DirCols = { 1, 0, 1, 1, -1, 0, -1, -1 };
        private static readonly int[] DirRows = { 0, 1, 1, -1, 0, -1, -1, 1 };

        public event Action OnPlacementCancelled;
        public event Action<int, string, List<Vector2Int>> OnWordPlaced;

        public bool IsInPlacementMode => _placementState != TablePlacementState.Inactive;
        public TablePlacementState CurrentPlacementState => _placementState;
        public int PlacementWordRowIndex => _placementWordRowIndex;

        public IReadOnlyCollection<Vector2Int> AllPlacedPositions => _allPlacedPositions;
        public IReadOnlyDictionary<Vector2Int, char> PlacedLetters => _placedLetters;

        /// <summary>
        /// Gets the word placement data for all placed words.
        /// Returns a list of tuples: (rowIndex, word, startCol, startRow, dirCol, dirRow)
        /// </summary>
        public List<(int rowIndex, string word, int startCol, int startRow, int dCol, int dRow)> GetAllWordPlacements()
        {
            List<(int, string, int, int, int, int)> result = new List<(int, string, int, int, int, int)>();
            foreach (KeyValuePair<int, (string word, int startCol, int startRow, int dCol, int dRow)> kvp in _wordPlacementData)
            {
                result.Add((kvp.Key, kvp.Value.word, kvp.Value.startCol, kvp.Value.startRow, kvp.Value.dCol, kvp.Value.dRow));
            }
            return result;
        }

        public TablePlacementController(TableModel tableModel, TableLayout tableLayout)
        {
            _tableModel = tableModel;
            _tableLayout = tableLayout;
        }

        public void EnterPlacementMode(int wordRowIndex, string word)
        {
            if (string.IsNullOrEmpty(word))
            {
                Debug.LogError("[TablePlacementController] Cannot enter placement mode - no word provided");
                return;
            }

            _placementWordRowIndex = wordRowIndex;
            _placementWord = word.ToUpper();
            _placementState = TablePlacementState.SelectingFirstCell;
            _firstCellCol = -1;
            _firstCellRow = -1;
            _placedCellPositions.Clear();

            Debug.Log($"[TablePlacementController] Entered placement mode for word: {_placementWord}");
        }

        public void CancelPlacementMode()
        {
            if (_placementState == TablePlacementState.Inactive) return;

            // Clear any preview from first cell
            if (_firstCellCol >= 0 && _firstCellRow >= 0)
            {
                Vector2Int pos = new Vector2Int(_firstCellCol, _firstCellRow);
                if (!_allPlacedPositions.Contains(pos))
                {
                    SetCellState(_firstCellCol, _firstCellRow, TableCellState.Fog, '\0');
                }
                else if (_placedLetters.TryGetValue(pos, out char existingLetter))
                {
                    SetCellState(_firstCellCol, _firstCellRow, TableCellState.Normal, existingLetter);
                }
            }

            ClearPlacementHighlighting();

            _placementState = TablePlacementState.Inactive;
            _placementWordRowIndex = -1;
            _placementWord = "";
            _firstCellCol = -1;
            _firstCellRow = -1;
            _placedCellPositions.Clear();

            OnPlacementCancelled?.Invoke();
            Debug.Log("[TablePlacementController] Placement mode cancelled");
        }

        public bool HandleCellClick(int gridCol, int gridRow)
        {
            if (_placementState == TablePlacementState.Inactive)
            {
                return false;
            }

            int gridSize = _tableLayout.GridSize;
            if (gridCol < 0 || gridCol >= gridSize || gridRow < 0 || gridRow >= gridSize)
            {
                return false;
            }

            if (_placementState == TablePlacementState.SelectingFirstCell)
            {
                List<Vector2Int> validDirections = GetValidDirectionsFromCell(gridCol, gridRow);
                if (validDirections.Count == 0)
                {
                    Debug.Log("[TablePlacementController] Invalid starting position: no valid directions");
                    return true; // Handled but invalid
                }

                _firstCellCol = gridCol;
                _firstCellRow = gridRow;
                _placementState = TablePlacementState.SelectingDirection;

                // Show first letter immediately
                if (!string.IsNullOrEmpty(_placementWord))
                {
                    SetCellState(gridCol, gridRow, TableCellState.PlacementAnchor, _placementWord[0]);
                }

                UpdatePlacementPreview(gridCol, gridRow);
                Debug.Log($"[TablePlacementController] First cell selected: col={gridCol}, row={gridRow}");
                return true;
            }
            else if (_placementState == TablePlacementState.SelectingDirection)
            {
                if (IsValidDirectionCell(gridCol, gridRow))
                {
                    int dCol = gridCol - _firstCellCol;
                    int dRow = gridRow - _firstCellRow;
                    PlaceWordInDirection(_firstCellCol, _firstCellRow, dCol, dRow);
                }
                else
                {
                    CancelPlacementMode();
                }
                return true;
            }

            return false;
        }

        public void UpdatePlacementPreview(int hoverCol, int hoverRow)
        {
            ClearPlacementHighlighting();

            if (_placementState == TablePlacementState.SelectingFirstCell)
            {
                List<Vector2Int> validDirections = GetValidDirectionsFromCell(hoverCol, hoverRow);

                // Highlight hover cell as cursor
                SetCellState(hoverCol, hoverRow, TableCellState.PlacementAnchor, '\0');

                // Highlight valid direction cells
                foreach (Vector2Int pos in validDirections)
                {
                    SetCellState(pos.x, pos.y, TableCellState.PlacementValid, '\0');
                }

                // Highlight invalid cells
                HighlightInvalidCells(hoverCol, hoverRow, validDirections);
            }
            else if (_placementState == TablePlacementState.SelectingDirection)
            {
                // First cell stays highlighted with first letter
                SetCellState(_firstCellCol, _firstCellRow, TableCellState.PlacementAnchor, _placementWord[0]);

                // Show valid second cells
                List<Vector2Int> validDirections = GetValidDirectionsFromCell(_firstCellCol, _firstCellRow);
                foreach (Vector2Int pos in validDirections)
                {
                    SetCellState(pos.x, pos.y, TableCellState.PlacementValid, '\0');
                }

                // If hovering over valid direction, preview full word
                if (validDirections.Contains(new Vector2Int(hoverCol, hoverRow)))
                {
                    PreviewWordPlacement(hoverCol, hoverRow);
                }
            }
        }

        public bool PlaceWordRandomly()
        {
            if (_placementState == TablePlacementState.Inactive)
            {
                Debug.LogError("[TablePlacementController] Not in placement mode");
                return false;
            }

            if (string.IsNullOrEmpty(_placementWord))
            {
                Debug.LogError("[TablePlacementController] No word to place");
                return false;
            }

            List<(int col, int row, int dCol, int dRow)> validPlacements = GetAllValidPlacements();

            if (validPlacements.Count == 0)
            {
                Debug.LogWarning("[TablePlacementController] No valid placements found");
                return false;
            }

            int randomIndex = UnityEngine.Random.Range(0, validPlacements.Count);
            (int col, int row, int dCol, int dRow) placement = validPlacements[randomIndex];

            return PlaceWordInDirection(placement.col, placement.row, placement.dCol, placement.dRow);
        }

        public void ClearWordFromGrid(int rowIndex)
        {
            if (!_wordRowPositions.TryGetValue(rowIndex, out List<Vector2Int> positions))
            {
                Debug.LogWarning($"[TablePlacementController] No position tracking for row {rowIndex + 1}");
                return;
            }

            foreach (Vector2Int pos in positions)
            {
                // Check if another word shares this cell
                bool sharedCell = false;
                foreach (KeyValuePair<int, List<Vector2Int>> kvp in _wordRowPositions)
                {
                    if (kvp.Key != rowIndex && kvp.Value.Contains(pos))
                    {
                        sharedCell = true;
                        break;
                    }
                }

                if (!sharedCell)
                {
                    SetCellState(pos.x, pos.y, TableCellState.Fog, '\0');
                    _allPlacedPositions.Remove(pos);
                    _placedLetters.Remove(pos);
                }
            }

            _wordRowPositions.Remove(rowIndex);
            _wordPlacementData.Remove(rowIndex);
            Debug.Log($"[TablePlacementController] Cleared grid cells for row {rowIndex + 1}");
        }

        public void ClearAllPlacedWords()
        {
            int gridSize = _tableLayout.GridSize;
            for (int col = 0; col < gridSize; col++)
            {
                for (int row = 0; row < gridSize; row++)
                {
                    SetCellState(col, row, TableCellState.Fog, '\0');
                }
            }

            _allPlacedPositions.Clear();
            _placedLetters.Clear();
            _wordRowPositions.Clear();
            _wordPlacementData.Clear();

            Debug.Log("[TablePlacementController] Cleared all placed words");
        }

        public List<Vector2Int> GetWordPositions(int rowIndex)
        {
            if (_wordRowPositions.TryGetValue(rowIndex, out List<Vector2Int> positions))
            {
                return new List<Vector2Int>(positions);
            }
            return null;
        }

        public bool HasPlacedLetter(int col, int row)
        {
            return _allPlacedPositions.Contains(new Vector2Int(col, row));
        }

        public char GetLetterAtPosition(Vector2Int pos)
        {
            return _placedLetters.TryGetValue(pos, out char letter) ? letter : '\0';
        }

        public bool IsPositionOccupied(Vector2Int pos)
        {
            return _allPlacedPositions.Contains(pos);
        }

        private void ClearPlacementHighlighting()
        {
            int gridSize = _tableLayout.GridSize;
            for (int col = 0; col < gridSize; col++)
            {
                for (int row = 0; row < gridSize; row++)
                {
                    Vector2Int pos = new Vector2Int(col, row);
                    if (_allPlacedPositions.Contains(pos))
                    {
                        if (_placedLetters.TryGetValue(pos, out char letter))
                        {
                            SetCellState(col, row, TableCellState.Normal, letter);
                        }
                    }
                    else
                    {
                        SetCellState(col, row, TableCellState.Fog, '\0');
                    }
                }
            }
        }

        private List<(int col, int row, int dCol, int dRow)> GetAllValidPlacements()
        {
            List<(int, int, int, int)> validPlacements = new List<(int, int, int, int)>();

            if (string.IsNullOrEmpty(_placementWord)) return validPlacements;

            int wordLength = _placementWord.Length;
            int gridSize = _tableLayout.GridSize;

            for (int startCol = 0; startCol < gridSize; startCol++)
            {
                for (int startRow = 0; startRow < gridSize; startRow++)
                {
                    for (int d = 0; d < 8; d++)
                    {
                        if (IsValidPlacement(startCol, startRow, DirCols[d], DirRows[d], wordLength))
                        {
                            validPlacements.Add((startCol, startRow, DirCols[d], DirRows[d]));
                        }
                    }
                }
            }

            ShuffleList(validPlacements);
            return validPlacements;
        }

        private bool IsValidPlacement(int startCol, int startRow, int dCol, int dRow, int wordLength)
        {
            int gridSize = _tableLayout.GridSize;

            for (int i = 0; i < wordLength; i++)
            {
                int col = startCol + (i * dCol);
                int row = startRow + (i * dRow);

                if (col < 0 || col >= gridSize || row < 0 || row >= gridSize)
                {
                    return false;
                }

                Vector2Int pos = new Vector2Int(col, row);
                if (_allPlacedPositions.Contains(pos))
                {
                    if (_placedLetters.TryGetValue(pos, out char existingLetter))
                    {
                        if (existingLetter != _placementWord[i])
                        {
                            return false;
                        }
                    }
                }
            }

            return true;
        }

        private List<Vector2Int> GetValidDirectionsFromCell(int startCol, int startRow)
        {
            List<Vector2Int> validCells = new List<Vector2Int>();

            if (string.IsNullOrEmpty(_placementWord)) return validCells;

            int wordLength = _placementWord.Length;

            for (int d = 0; d < 8; d++)
            {
                if (IsValidPlacement(startCol, startRow, DirCols[d], DirRows[d], wordLength))
                {
                    int secondCol = startCol + DirCols[d];
                    int secondRow = startRow + DirRows[d];
                    validCells.Add(new Vector2Int(secondCol, secondRow));
                }
            }

            return validCells;
        }

        private bool IsValidDirectionCell(int col, int row)
        {
            if (_firstCellCol < 0 || _firstCellRow < 0) return false;
            List<Vector2Int> validDirections = GetValidDirectionsFromCell(_firstCellCol, _firstCellRow);
            return validDirections.Contains(new Vector2Int(col, row));
        }

        private void HighlightInvalidCells(int hoverCol, int hoverRow, List<Vector2Int> validDirections)
        {
            int gridSize = _tableLayout.GridSize;

            for (int d = 0; d < 8; d++)
            {
                int adjCol = hoverCol + DirCols[d];
                int adjRow = hoverRow + DirRows[d];

                if (adjCol >= 0 && adjCol < gridSize && adjRow >= 0 && adjRow < gridSize)
                {
                    Vector2Int adjPos = new Vector2Int(adjCol, adjRow);
                    if (!validDirections.Contains(adjPos))
                    {
                        SetCellState(adjCol, adjRow, TableCellState.PlacementInvalid, '\0');
                    }
                }
            }
        }

        private void PreviewWordPlacement(int secondCol, int secondRow)
        {
            if (_firstCellCol < 0 || _firstCellRow < 0) return;

            int dCol = secondCol - _firstCellCol;
            int dRow = secondRow - _firstCellRow;

            for (int i = 0; i < _placementWord.Length; i++)
            {
                int col = _firstCellCol + (i * dCol);
                int row = _firstCellRow + (i * dRow);

                TableCellState state = (i == 0) ? TableCellState.PlacementAnchor : TableCellState.PlacementPath;
                SetCellState(col, row, state, _placementWord[i]);
            }
        }

        private bool PlaceWordInDirection(int startCol, int startRow, int dCol, int dRow)
        {
            if (string.IsNullOrEmpty(_placementWord)) return false;

            ClearPlacementHighlighting();
            _placedCellPositions.Clear();

            for (int i = 0; i < _placementWord.Length; i++)
            {
                int col = startCol + (i * dCol);
                int row = startRow + (i * dRow);

                char letter = _placementWord[i];
                SetCellState(col, row, TableCellState.Normal, letter);

                Vector2Int pos = new Vector2Int(col, row);
                _placedCellPositions.Add(pos);
                _allPlacedPositions.Add(pos);
                _placedLetters[pos] = letter;
            }

            _wordRowPositions[_placementWordRowIndex] = new List<Vector2Int>(_placedCellPositions);

            // Store the word placement data for later retrieval
            _wordPlacementData[_placementWordRowIndex] = (_placementWord, startCol, startRow, dCol, dRow);

            int placedRowIndex = _placementWordRowIndex;
            string placedWord = _placementWord;
            List<Vector2Int> placedPositions = new List<Vector2Int>(_placedCellPositions);

            _placementState = TablePlacementState.Inactive;
            _placementWordRowIndex = -1;
            _placementWord = "";
            _firstCellCol = -1;
            _firstCellRow = -1;
            _placedCellPositions.Clear();

            OnWordPlaced?.Invoke(placedRowIndex, placedWord, placedPositions);
            Debug.Log($"[TablePlacementController] Word '{placedWord}' placed at ({startCol},{startRow}) dir ({dCol},{dRow})");
            return true;
        }

        private void SetCellState(int gridCol, int gridRow, TableCellState state, char letter)
        {
            (int tableRow, int tableCol) = _tableLayout.GridToTable(gridRow, gridCol);
            _tableModel.SetCellState(tableRow, tableCol, state);
            _tableModel.SetCellChar(tableRow, tableCol, letter);

            // Set owner for placed cells
            if (state == TableCellState.Normal && letter != '\0')
            {
                _tableModel.SetCellOwner(tableRow, tableCol, CellOwner.Player1);
            }
            else if (state == TableCellState.Fog)
            {
                _tableModel.SetCellOwner(tableRow, tableCol, CellOwner.None);
            }
        }

        private void ShuffleList<T>(List<T> list)
        {
            int n = list.Count;
            while (n > 1)
            {
                n--;
                int k = UnityEngine.Random.Range(0, n + 1);
                T value = list[k];
                list[k] = list[n];
                list[n] = value;
            }
        }
    }

    /// <summary>
    /// Manages cell highlighting colors for the TableView placement system.
    /// </summary>
    public class TableGridColorManager
    {
        private readonly TableModel _tableModel;
        private readonly TableLayout _tableLayout;

        public Color CursorColor { get; set; } = new Color(0.2f, 0.6f, 1f, 1f);
        public Color ValidPlacementColor { get; set; } = new Color(0.2f, 0.8f, 0.2f, 1f);
        public Color InvalidPlacementColor { get; set; } = new Color(0.8f, 0.2f, 0.2f, 1f);
        public Color PlacedLetterColor { get; set; } = new Color(0.9f, 0.9f, 0.9f, 1f);

        public TableGridColorManager(TableModel tableModel, TableLayout tableLayout)
        {
            _tableModel = tableModel;
            _tableLayout = tableLayout;
        }

        public void ClearAllHighlights()
        {
            int gridSize = _tableLayout.GridSize;
            for (int gridRow = 0; gridRow < gridSize; gridRow++)
            {
                for (int gridCol = 0; gridCol < gridSize; gridCol++)
                {
                    (int tableRow, int tableCol) = _tableLayout.GridToTable(gridRow, gridCol);
                    TableCell cell = _tableModel.GetCell(tableRow, tableCol);

                    // If cell has a letter, set to Normal, otherwise Fog
                    if (cell.TextChar != '\0')
                    {
                        _tableModel.SetCellState(tableRow, tableCol, TableCellState.Normal);
                    }
                    else
                    {
                        _tableModel.SetCellState(tableRow, tableCol, TableCellState.Fog);
                    }
                }
            }
        }
    }
}

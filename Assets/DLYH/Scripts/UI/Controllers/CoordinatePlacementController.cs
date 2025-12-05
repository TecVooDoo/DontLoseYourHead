using System;
using System.Collections.Generic;
using UnityEngine;

namespace TecVooDoo.DontLoseYourHead.UI
{
    /// <summary>
    /// Manages coordinate placement mode for word placement on the grid.
    /// Plain class - receives dependencies via constructor.
    /// </summary>
    public class CoordinatePlacementController : ICoordinatePlacementController
    {
        private readonly IGridColorManager _colorManager;
        private readonly Func<int, int, GridCellUI> _getCellFunc;
        private readonly Func<int> _getGridSizeFunc;

        private PlacementState _placementState = PlacementState.Inactive;
        private int _placementWordRowIndex = -1;
        private string _placementWord = "";
        private int _firstCellCol = -1;
        private int _firstCellRow = -1;

        private readonly List<Vector2Int> _placedCellPositions = new List<Vector2Int>();
        private readonly HashSet<Vector2Int> _allPlacedPositions = new HashSet<Vector2Int>();
        private readonly Dictionary<Vector2Int, char> _placedLetters = new Dictionary<Vector2Int, char>();
        private readonly Dictionary<int, List<Vector2Int>> _wordRowPositions = new Dictionary<int, List<Vector2Int>>();

        public event Action OnPlacementCancelled;
        public event Action<int, string, List<Vector2Int>> OnWordPlaced;

        public bool IsInPlacementMode => _placementState != PlacementState.Inactive;
        public PlacementState CurrentPlacementState => _placementState;
        public int PlacementWordRowIndex => _placementWordRowIndex;

        /// <summary>
        /// Creates a new CoordinatePlacementController.
        /// </summary>
        /// <param name="colorManager">Color manager for highlighting</param>
        /// <param name="getCellFunc">Function to get a cell by column and row</param>
        /// <param name="getGridSizeFunc">Function to get current grid size</param>
        public CoordinatePlacementController(
            IGridColorManager colorManager,
            Func<int, int, GridCellUI> getCellFunc,
            Func<int> getGridSizeFunc)
        {
            _colorManager = colorManager;
            _getCellFunc = getCellFunc;
            _getGridSizeFunc = getGridSizeFunc;
        }

        /// <summary>
        /// Enters coordinate placement mode for a specific word.
        /// </summary>
        public void EnterPlacementMode(int wordRowIndex, string word)
        {
            if (string.IsNullOrEmpty(word))
            {
                Debug.LogError("[CoordinatePlacementController] Cannot enter placement mode - no word provided");
                return;
            }

            _placementWordRowIndex = wordRowIndex;
            _placementWord = word;
            _placementState = PlacementState.SelectingFirstCell;
            _firstCellCol = -1;
            _firstCellRow = -1;
            _placedCellPositions.Clear();

            Debug.Log(string.Format("[CoordinatePlacementController] Entered placement mode for word: {0}", _placementWord));
        }

        /// <summary>
        /// Cancels coordinate placement mode.
        /// </summary>
        public void CancelPlacementMode()
        {
            if (_placementState == PlacementState.Inactive) return;

            // Clear the first cell's temporary letter if one was placed
            if (_firstCellCol >= 0 && _firstCellRow >= 0)
            {
                var firstCell = _getCellFunc(_firstCellCol, _firstCellRow);
                if (firstCell != null)
                {
                    var pos = new Vector2Int(_firstCellCol, _firstCellRow);
                    if (!_allPlacedPositions.Contains(pos))
                    {
                        firstCell.ClearLetter();
                    }
                    else if (_placedLetters.TryGetValue(pos, out char existingLetter))
                    {
                        firstCell.SetLetter(existingLetter);
                    }
                }
            }

            ClearPlacementHighlighting();

            _placementState = PlacementState.Inactive;
            _placementWordRowIndex = -1;
            _placementWord = "";
            _firstCellCol = -1;
            _firstCellRow = -1;
            _placedCellPositions.Clear();

            OnPlacementCancelled?.Invoke();
            Debug.Log("[CoordinatePlacementController] Placement mode cancelled");
        }

        /// <summary>
        /// Attempts to place the current word randomly on the grid.
        /// </summary>
        public bool PlaceWordRandomly()
        {
            if (_placementState == PlacementState.Inactive)
            {
                Debug.LogError("[CoordinatePlacementController] Not in placement mode");
                return false;
            }

            if (string.IsNullOrEmpty(_placementWord))
            {
                Debug.LogError("[CoordinatePlacementController] No word to place");
                return false;
            }

            var validPlacements = GetAllValidPlacements();

            if (validPlacements.Count == 0)
            {
                Debug.LogWarning("[CoordinatePlacementController] No valid placements found");
                return false;
            }

            int randomIndex = UnityEngine.Random.Range(0, validPlacements.Count);
            var placement = validPlacements[randomIndex];

            return PlaceWordInDirection(placement.col, placement.row, placement.dCol, placement.dRow);
        }

        /// <summary>
        /// Handles a cell click during placement mode.
        /// </summary>
        /// <returns>True if the click was handled by placement mode</returns>
        public bool HandleCellClick(int column, int row)
        {
            if (_placementState == PlacementState.Inactive)
            {
                return false;
            }

            int gridSize = _getGridSizeFunc();
            if (column < 0 || column >= gridSize || row < 0 || row >= gridSize)
            {
                return false;
            }

            if (_placementState == PlacementState.SelectingFirstCell)
            {
                var validDirections = GetValidDirectionsFromCell(column, row);
                if (validDirections.Count == 0)
                {
                    Debug.Log(string.Format("[CoordinatePlacementController] Invalid starting position: no valid directions"));
                    return true; // Handled but invalid
                }

                _firstCellCol = column;
                _firstCellRow = row;
                _placementState = PlacementState.SelectingDirection;

                // Show first letter immediately
                var cell = _getCellFunc(column, row);
                if (cell != null && !string.IsNullOrEmpty(_placementWord))
                {
                    cell.SetLetter(_placementWord[0]);
                    _colorManager.SetCellHighlight(cell, GridHighlightType.Cursor);
                }

                UpdatePlacementPreview(column, row);
                Debug.Log(string.Format("[CoordinatePlacementController] First cell selected: col={0}, row={1}", column, row));
                return true;
            }
            else if (_placementState == PlacementState.SelectingDirection)
            {
                if (IsValidDirectionCell(column, row))
                {
                    int dCol = column - _firstCellCol;
                    int dRow = row - _firstCellRow;
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

        /// <summary>
        /// Updates the placement preview highlighting.
        /// </summary>
        public void UpdatePlacementPreview(int hoverCol, int hoverRow)
        {
            ClearPlacementHighlighting();

            if (_placementState == PlacementState.SelectingFirstCell)
            {
                var validDirections = GetValidDirectionsFromCell(hoverCol, hoverRow);

                // Highlight current cell as cursor
                var hoverCell = _getCellFunc(hoverCol, hoverRow);
                if (hoverCell != null)
                {
                    _colorManager.SetCellHighlight(hoverCell, GridHighlightType.Cursor);
                }

                // Highlight valid direction cells
                foreach (var pos in validDirections)
                {
                    var cell = _getCellFunc(pos.x, pos.y);
                    if (cell != null)
                    {
                        _colorManager.SetCellHighlight(cell, GridHighlightType.ValidPlacement);
                    }
                }

                // Highlight invalid cells
                HighlightInvalidCells(hoverCol, hoverRow, validDirections);
            }
            else if (_placementState == PlacementState.SelectingDirection)
            {
                // First cell stays highlighted
                var firstCell = _getCellFunc(_firstCellCol, _firstCellRow);
                if (firstCell != null)
                {
                    _colorManager.SetCellHighlight(firstCell, GridHighlightType.Cursor);
                }

                // Show valid second cells
                var validDirections = GetValidDirectionsFromCell(_firstCellCol, _firstCellRow);
                foreach (var pos in validDirections)
                {
                    var cell = _getCellFunc(pos.x, pos.y);
                    if (cell != null)
                    {
                        _colorManager.SetCellHighlight(cell, GridHighlightType.ValidPlacement);
                    }
                }

                // If hovering over valid direction, preview full word
                if (validDirections.Contains(new Vector2Int(hoverCol, hoverRow)))
                {
                    PreviewWordPlacement(hoverCol, hoverRow);
                }
            }
        }

        /// <summary>
        /// Clears all placement highlighting.
        /// </summary>
        public void ClearPlacementHighlighting()
        {
            int gridSize = _getGridSizeFunc();
            for (int col = 0; col < gridSize; col++)
            {
                for (int row = 0; row < gridSize; row++)
                {
                    var cell = _getCellFunc(col, row);
                    if (cell != null)
                    {
                        _colorManager.ClearCellHighlight(cell);

                        var pos = new Vector2Int(col, row);
                        if (_allPlacedPositions.Contains(pos))
                        {
                            if (_placedLetters.TryGetValue(pos, out char letter))
                            {
                                cell.SetLetter(letter);
                                cell.SetState(CellState.Filled);
                            }
                        }
                        else
                        {
                            cell.ClearLetter();
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Clears a word's cells from the grid.
        /// </summary>
        public void ClearWordFromGrid(int rowIndex)
        {
            if (!_wordRowPositions.TryGetValue(rowIndex, out List<Vector2Int> positions))
            {
                Debug.LogWarning(string.Format("[CoordinatePlacementController] No position tracking for row {0}", rowIndex + 1));
                return;
            }

            foreach (var pos in positions)
            {
                var cell = _getCellFunc(pos.x, pos.y);
                if (cell == null) continue;

                // Check if another word shares this cell
                bool sharedCell = false;
                foreach (var kvp in _wordRowPositions)
                {
                    if (kvp.Key != rowIndex && kvp.Value.Contains(pos))
                    {
                        sharedCell = true;
                        break;
                    }
                }

                if (!sharedCell)
                {
                    cell.SetState(CellState.Empty);
                    cell.ClearLetter();
                    cell.ClearHighlight();
                    _allPlacedPositions.Remove(pos);
                    _placedLetters.Remove(pos);
                }
            }

            _wordRowPositions.Remove(rowIndex);
            Debug.Log(string.Format("[CoordinatePlacementController] Cleared grid cells for row {0}", rowIndex + 1));
        }

        /// <summary>
        /// Clears all placed words from the grid.
        /// </summary>
        public void ClearAllPlacedWords()
        {
            int gridSize = _getGridSizeFunc();
            for (int col = 0; col < gridSize; col++)
            {
                for (int row = 0; row < gridSize; row++)
                {
                    var cell = _getCellFunc(col, row);
                    if (cell != null)
                    {
                        cell.SetState(CellState.Empty);
                        cell.ClearLetter();
                        cell.ClearHighlight();
                    }
                }
            }

            _allPlacedPositions.Clear();
            _placedLetters.Clear();
            _wordRowPositions.Clear();

            Debug.Log("[CoordinatePlacementController] Cleared all placed words");
        }

        /// <summary>
        /// Gets positions for a specific word row.
        /// </summary>
        public List<Vector2Int> GetWordPositions(int rowIndex)
        {
            if (_wordRowPositions.TryGetValue(rowIndex, out List<Vector2Int> positions))
            {
                return new List<Vector2Int>(positions);
            }
            return null;
        }

        /// <summary>
        /// Checks if a position has a placed letter.
        /// </summary>
        public bool HasPlacedLetter(int col, int row)
        {
            return _allPlacedPositions.Contains(new Vector2Int(col, row));
        }

        private List<(int col, int row, int dCol, int dRow)> GetAllValidPlacements()
        {
            var validPlacements = new List<(int, int, int, int)>();

            if (string.IsNullOrEmpty(_placementWord)) return validPlacements;

            int wordLength = _placementWord.Length;
            int gridSize = _getGridSizeFunc();

            int[] dCols = { 1, 0, 1, 1, -1, 0, -1, -1 };
            int[] dRows = { 0, 1, 1, -1, 0, -1, -1, 1 };

            for (int startCol = 0; startCol < gridSize; startCol++)
            {
                for (int startRow = 0; startRow < gridSize; startRow++)
                {
                    for (int d = 0; d < 8; d++)
                    {
                        if (IsValidPlacement(startCol, startRow, dCols[d], dRows[d], wordLength))
                        {
                            validPlacements.Add((startCol, startRow, dCols[d], dRows[d]));
                        }
                    }
                }
            }

            ShuffleList(validPlacements);
            return validPlacements;
        }

        private bool IsValidPlacement(int startCol, int startRow, int dCol, int dRow, int wordLength)
        {
            int gridSize = _getGridSizeFunc();

            for (int i = 0; i < wordLength; i++)
            {
                int col = startCol + (i * dCol);
                int row = startRow + (i * dRow);

                if (col < 0 || col >= gridSize || row < 0 || row >= gridSize)
                {
                    return false;
                }

                var pos = new Vector2Int(col, row);
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
            var validCells = new List<Vector2Int>();

            if (string.IsNullOrEmpty(_placementWord)) return validCells;

            int wordLength = _placementWord.Length;
            int[] dCols = { 1, 0, 1, 1, -1, 0, -1, -1 };
            int[] dRows = { 0, 1, 1, -1, 0, -1, -1, 1 };

            for (int d = 0; d < 8; d++)
            {
                if (IsValidPlacement(startCol, startRow, dCols[d], dRows[d], wordLength))
                {
                    int secondCol = startCol + dCols[d];
                    int secondRow = startRow + dRows[d];
                    validCells.Add(new Vector2Int(secondCol, secondRow));
                }
            }

            return validCells;
        }

        private bool IsValidDirectionCell(int col, int row)
        {
            if (_firstCellCol < 0 || _firstCellRow < 0) return false;
            var validDirections = GetValidDirectionsFromCell(_firstCellCol, _firstCellRow);
            return validDirections.Contains(new Vector2Int(col, row));
        }

        private void HighlightInvalidCells(int hoverCol, int hoverRow, List<Vector2Int> validDirections)
        {
            int[] dCols = { 1, 0, 1, 1, -1, 0, -1, -1 };
            int[] dRows = { 0, 1, 1, -1, 0, -1, -1, 1 };
            int gridSize = _getGridSizeFunc();

            for (int d = 0; d < 8; d++)
            {
                int adjCol = hoverCol + dCols[d];
                int adjRow = hoverRow + dRows[d];

                if (adjCol >= 0 && adjCol < gridSize && adjRow >= 0 && adjRow < gridSize)
                {
                    var adjPos = new Vector2Int(adjCol, adjRow);
                    if (!validDirections.Contains(adjPos))
                    {
                        var cell = _getCellFunc(adjCol, adjRow);
                        if (cell != null)
                        {
                            _colorManager.SetCellHighlight(cell, GridHighlightType.InvalidPlacement);
                        }
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

                var cell = _getCellFunc(col, row);
                if (cell != null)
                {
                    cell.SetLetter(_placementWord[i]);
                    _colorManager.SetCellHighlight(cell, i == 0 ? GridHighlightType.Cursor : GridHighlightType.ValidPlacement);
                }
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

                var cell = _getCellFunc(col, row);
                if (cell != null)
                {
                    cell.SetLetter(_placementWord[i]);
                    cell.SetState(CellState.Filled);

                    var pos = new Vector2Int(col, row);
                    _placedCellPositions.Add(pos);
                    _allPlacedPositions.Add(pos);
                    _placedLetters[pos] = _placementWord[i];
                }
            }

            _wordRowPositions[_placementWordRowIndex] = new List<Vector2Int>(_placedCellPositions);

            int placedRowIndex = _placementWordRowIndex;
            string placedWord = _placementWord;
            var placedPositions = new List<Vector2Int>(_placedCellPositions);

            _placementState = PlacementState.Inactive;
            _placementWordRowIndex = -1;
            _placementWord = "";

            ClearPlacementHighlighting();

            OnWordPlaced?.Invoke(placedRowIndex, placedWord, placedPositions);
            Debug.Log("[CoordinatePlacementController] Word placed successfully");
            return true;
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
}

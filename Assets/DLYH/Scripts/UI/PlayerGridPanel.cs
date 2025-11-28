using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Sirenix.OdinInspector;
using System;

namespace TecVooDoo.DontLoseYourHead.UI
{
    /// <summary>
    /// Manages a player's grid panel UI including the grid cells,
    /// row/column labels, letter tracker, and word patterns display.
    /// Supports dynamic grid sizes from 6x6 to 12x12.
    /// </summary>
    public class PlayerGridPanel : MonoBehaviour
    {
        #region Constants
        public const int MAX_GRID_SIZE = 12;
        public const int MIN_GRID_SIZE = 6;

        // Layout constants - adjust these to match your prefab settings
        private const float CELL_SIZE = 40f;
        private const float CELL_SPACING = 2f;
        private const float ROW_LABEL_HEIGHT = 40f;
        private const float ROW_LABEL_SPACING = 2f;
        #endregion

        #region Serialized Fields - References
        [TitleGroup("Prefab Reference")]
        [SerializeField, Required]
        private GridCellUI _cellPrefab;

        [TitleGroup("Container References")]
        [SerializeField, Required]
        private Transform _gridContainer;

        [SerializeField]
        private Transform _rowLabelsContainer;

        [SerializeField]
        private Transform _columnLabelsContainer;

        [TitleGroup("Display References")]
        [SerializeField]
        private TextMeshProUGUI _playerNameLabel;

        [SerializeField]
        private Transform _letterTrackerContainer;

        [SerializeField]
        private Transform _wordPatternsContainer;

        [TitleGroup("Layout References")]
        [SerializeField, Tooltip("The RectTransform of the GridWithRowLabels container")]
        private RectTransform _gridWithRowLabelsRect;

        [SerializeField, Tooltip("The LayoutElement on GridContainer for height control")]
        private LayoutElement _gridContainerLayout;

        [SerializeField, Tooltip("The LayoutElement on RowLabelsContainer for height control")]
        private LayoutElement _rowLabelsLayout;
        #endregion

        #region Serialized Fields - Configuration
        [TitleGroup("Configuration")]
        [SerializeField, Range(MIN_GRID_SIZE, MAX_GRID_SIZE)]
        private int _currentGridSize = 8;

        [SerializeField]
        private Color _playerColor = Color.white;

        [TitleGroup("Layout Settings")]
        [SerializeField, Tooltip("Height of non-grid elements (header, word patterns, letter tracker, column labels)")]
        private float _fixedElementsHeight = 300f;
        #endregion

        #region Private Fields
        private GridCellUI[,] _cells = new GridCellUI[MAX_GRID_SIZE, MAX_GRID_SIZE];
        private GameObject[] _rowLabelObjects = new GameObject[MAX_GRID_SIZE];
        private GameObject[] _columnLabelObjects = new GameObject[MAX_GRID_SIZE];
        private GridLayoutGroup _gridLayoutGroup;
        private RectTransform _panelRectTransform;
        private LayoutElement _panelLayoutElement;
        private bool _isInitialized;
        #endregion

        #region Events
        /// <summary>
        /// Fired when a cell is clicked. Parameters: column, row
        /// </summary>
        public event Action<int, int> OnCellClicked;

        /// <summary>
        /// Fired when mouse enters a cell. Parameters: column, row
        /// </summary>
        public event Action<int, int> OnCellHoverEnter;

        /// <summary>
        /// Fired when mouse exits a cell. Parameters: column, row
        /// </summary>
        public event Action<int, int> OnCellHoverExit;
        #endregion

        #region Properties
        public int CurrentGridSize => _currentGridSize;
        public bool IsInitialized => _isInitialized;
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            CachePanelReferences();
            CacheGridLayoutGroup();
            CacheExistingLabels();
        }
        #endregion

        #region Public Methods - Initialization
        /// <summary>
        /// Creates all grid cells and initializes the panel.
        /// Call this once at game start or when setting up a new game.
        /// </summary>
        [Button("Initialize Grid (Editor Test)")]
        public void InitializeGrid()
        {
            InitializeGrid(_currentGridSize);
        }

        /// <summary>
        /// Creates all grid cells for the specified grid size.
        /// </summary>
        /// <param name="gridSize">Size of the grid (6, 8, 10, or 12)</param>
        public void InitializeGrid(int gridSize)
        {
            _currentGridSize = Mathf.Clamp(gridSize, MIN_GRID_SIZE, MAX_GRID_SIZE);

            if (_cellPrefab == null)
            {
                Debug.LogError("[PlayerGridPanel] Cell prefab is not assigned!");
                return;
            }

            if (_gridContainer == null)
            {
                Debug.LogError("[PlayerGridPanel] Grid container is not assigned!");
                return;
            }

            // Cache references if not already done
            CachePanelReferences();
            CacheGridLayoutGroup();
            CacheExistingLabels();

            // Clear any existing cells
            ClearGrid();

            // Update GridLayoutGroup constraint to match grid size
            UpdateGridLayoutConstraint();

            // Create only the cells we need for this grid size
            CreateCellsForCurrentSize();

            // Update label visibility
            UpdateLabelVisibility();

            // Update panel height based on grid size
            UpdatePanelHeight();

            _isInitialized = true;

            Debug.Log($"[PlayerGridPanel] Grid initialized with size {_currentGridSize}x{_currentGridSize}");
        }

        /// <summary>
        /// Changes the grid size and recreates the grid.
        /// </summary>
        /// <param name="newSize">New grid size (6, 8, 10, or 12)</param>
        public void SetGridSize(int newSize)
        {
            int clampedSize = Mathf.Clamp(newSize, MIN_GRID_SIZE, MAX_GRID_SIZE);

            if (clampedSize != _currentGridSize)
            {
                InitializeGrid(clampedSize);
            }
        }

        /// <summary>
        /// Sets the player name displayed at the top of the panel.
        /// </summary>
        public void SetPlayerName(string playerName)
        {
            if (_playerNameLabel != null)
            {
                _playerNameLabel.text = playerName;
            }
        }

        /// <summary>
        /// Sets the player's theme color.
        /// </summary>
        public void SetPlayerColor(Color color)
        {
            _playerColor = color;
        }

        /// <summary>
        /// Forces a recache of label references. Call this if labels were created after Awake.
        /// </summary>
        [Button("Recache Labels")]
        public void RecacheLabels()
        {
            CacheExistingLabels();
            UpdateLabelVisibility();
        }
        #endregion

        #region Public Methods - Cell Access
        /// <summary>
        /// Gets the cell at the specified coordinates.
        /// </summary>
        public GridCellUI GetCell(int column, int row)
        {
            if (!IsValidCoordinate(column, row))
            {
                return null;
            }
            return _cells[column, row];
        }

        /// <summary>
        /// Checks if the coordinate is valid for the current grid size.
        /// </summary>
        public bool IsValidCoordinate(int column, int row)
        {
            return column >= 0 && column < _currentGridSize &&
                   row >= 0 && row < _currentGridSize;
        }

        /// <summary>
        /// Sets the state of a specific cell.
        /// </summary>
        public void SetCellState(int column, int row, CellState state)
        {
            var cell = GetCell(column, row);
            if (cell != null)
            {
                cell.SetState(state);
            }
        }

        /// <summary>
        /// Sets the letter displayed in a specific cell.
        /// </summary>
        public void SetCellLetter(int column, int row, char letter)
        {
            var cell = GetCell(column, row);
            if (cell != null)
            {
                cell.SetLetter(letter);
            }
        }

        /// <summary>
        /// Clears the letter from a specific cell.
        /// </summary>
        public void ClearCellLetter(int column, int row)
        {
            var cell = GetCell(column, row);
            if (cell != null)
            {
                cell.ClearLetter();
            }
        }

        /// <summary>
        /// Resets all cells to empty state.
        /// </summary>
        [Button("Reset All Cells")]
        public void ResetAllCells()
        {
            for (int col = 0; col < _currentGridSize; col++)
            {
                for (int row = 0; row < _currentGridSize; row++)
                {
                    if (_cells[col, row] != null)
                    {
                        _cells[col, row].SetState(CellState.Empty);
                        _cells[col, row].ClearLetter();
                    }
                }
            }
        }
        #endregion

        #region Private Methods - Caching
        private void CachePanelReferences()
        {
            if (_panelRectTransform == null)
            {
                _panelRectTransform = GetComponent<RectTransform>();
            }

            if (_panelLayoutElement == null)
            {
                _panelLayoutElement = GetComponent<LayoutElement>();
            }

            // Try to auto-find layout references if not assigned
            if (_gridWithRowLabelsRect == null && _gridContainer != null)
            {
                var parent = _gridContainer.parent;
                if (parent != null)
                {
                    _gridWithRowLabelsRect = parent.GetComponent<RectTransform>();
                }
            }

            if (_gridContainerLayout == null && _gridContainer != null)
            {
                _gridContainerLayout = _gridContainer.GetComponent<LayoutElement>();
            }

            if (_rowLabelsLayout == null && _rowLabelsContainer != null)
            {
                _rowLabelsLayout = _rowLabelsContainer.GetComponent<LayoutElement>();
            }
        }

        private void CacheGridLayoutGroup()
        {
            if (_gridLayoutGroup == null && _gridContainer != null)
            {
                _gridLayoutGroup = _gridContainer.GetComponent<GridLayoutGroup>();
                if (_gridLayoutGroup == null)
                {
                    Debug.LogError("[PlayerGridPanel] GridContainer is missing GridLayoutGroup component!");
                }
            }
        }

        private void CacheExistingLabels()
        {
            // Clear existing cached references
            for (int i = 0; i < MAX_GRID_SIZE; i++)
            {
                _rowLabelObjects[i] = null;
                _columnLabelObjects[i] = null;
            }

            // Cache row labels (1-12) - store GameObjects for visibility control
            if (_rowLabelsContainer != null)
            {
                int labelIndex = 0;
                for (int i = 0; i < _rowLabelsContainer.childCount && labelIndex < MAX_GRID_SIZE; i++)
                {
                    var child = _rowLabelsContainer.GetChild(i);
                    // Check if this looks like a row label (has TMP or is named appropriately)
                    var tmp = child.GetComponent<TextMeshProUGUI>();
                    if (tmp != null || child.name.Contains("Row") || child.name.Contains("Label"))
                    {
                        _rowLabelObjects[labelIndex] = child.gameObject;
                        labelIndex++;
                    }
                }
                Debug.Log($"[PlayerGridPanel] Cached {labelIndex} row labels");
            }

            // Cache column labels (A-L) - store GameObjects for visibility control
            if (_columnLabelsContainer != null)
            {
                int labelIndex = 0;
                for (int i = 0; i < _columnLabelsContainer.childCount && labelIndex < MAX_GRID_SIZE; i++)
                {
                    var child = _columnLabelsContainer.GetChild(i);

                    // Skip spacer
                    if (child.name.ToLower().Contains("spacer"))
                    {
                        continue;
                    }

                    // Check if this looks like a column label
                    var tmp = child.GetComponent<TextMeshProUGUI>();
                    if (tmp != null || child.name.Contains("Label"))
                    {
                        _columnLabelObjects[labelIndex] = child.gameObject;
                        labelIndex++;
                    }
                }
                Debug.Log($"[PlayerGridPanel] Cached {labelIndex} column labels");
            }
        }
        #endregion

        #region Private Methods - Grid Layout
        private void UpdateGridLayoutConstraint()
        {
            if (_gridLayoutGroup != null)
            {
                _gridLayoutGroup.constraintCount = _currentGridSize;
                Debug.Log($"[PlayerGridPanel] GridLayoutGroup constraintCount set to {_currentGridSize}");
            }
        }

        private void UpdatePanelHeight()
        {
            // Calculate grid height: cells + spacing
            float gridHeight = (_currentGridSize * CELL_SIZE) + ((_currentGridSize - 1) * CELL_SPACING);

            // Update GridContainer LayoutElement if available
            if (_gridContainerLayout != null)
            {
                _gridContainerLayout.preferredHeight = gridHeight;
            }

            // Update RowLabelsContainer LayoutElement if available
            if (_rowLabelsLayout != null)
            {
                float rowLabelsHeight = (_currentGridSize * ROW_LABEL_HEIGHT) + ((_currentGridSize - 1) * ROW_LABEL_SPACING);
                _rowLabelsLayout.preferredHeight = rowLabelsHeight;
            }

            // Update GridWithRowLabels container if available
            if (_gridWithRowLabelsRect != null)
            {
                var layoutElement = _gridWithRowLabelsRect.GetComponent<LayoutElement>();
                if (layoutElement != null)
                {
                    layoutElement.preferredHeight = gridHeight;
                }
            }

            // Calculate total panel height
            float totalHeight = _fixedElementsHeight + gridHeight;

            // Update panel size
            if (_panelLayoutElement != null)
            {
                _panelLayoutElement.preferredHeight = totalHeight;
            }

            if (_panelRectTransform != null)
            {
                var sizeDelta = _panelRectTransform.sizeDelta;
                sizeDelta.y = totalHeight;
                _panelRectTransform.sizeDelta = sizeDelta;
            }

            // Force layout rebuild
            LayoutRebuilder.ForceRebuildLayoutImmediate(_panelRectTransform);

            Debug.Log($"[PlayerGridPanel] Panel height updated. Grid: {gridHeight}px, Total: {totalHeight}px");
        }
        #endregion

        #region Private Methods - Grid Creation
        private void CreateCellsForCurrentSize()
        {
            for (int row = 0; row < _currentGridSize; row++)
            {
                for (int col = 0; col < _currentGridSize; col++)
                {
                    var cell = Instantiate(_cellPrefab, _gridContainer);
                    cell.name = $"Cell_{GetColumnLetter(col)}{row + 1}";
                    cell.Initialize(col, row);

                    int capturedCol = col;
                    int capturedRow = row;
                    cell.OnCellClicked += () => HandleCellClicked(capturedCol, capturedRow);
                    cell.OnCellHoverEnter += () => HandleCellHoverEnter(capturedCol, capturedRow);
                    cell.OnCellHoverExit += () => HandleCellHoverExit(capturedCol, capturedRow);

                    _cells[col, row] = cell;
                }
            }
        }

        private void ClearGrid()
        {
            for (int col = 0; col < MAX_GRID_SIZE; col++)
            {
                for (int row = 0; row < MAX_GRID_SIZE; row++)
                {
                    _cells[col, row] = null;
                }
            }

            if (_gridContainer != null)
            {
                for (int i = _gridContainer.childCount - 1; i >= 0; i--)
                {
                    var child = _gridContainer.GetChild(i);
                    if (Application.isPlaying)
                    {
                        Destroy(child.gameObject);
                    }
                    else
                    {
                        DestroyImmediate(child.gameObject);
                    }
                }
            }
        }
        #endregion

        #region Private Methods - Labels
        private void UpdateLabelVisibility()
        {
            int hiddenRows = 0;
            int hiddenCols = 0;

            // Update row labels visibility
            for (int i = 0; i < MAX_GRID_SIZE; i++)
            {
                if (_rowLabelObjects[i] != null)
                {
                    bool shouldBeVisible = i < _currentGridSize;
                    _rowLabelObjects[i].SetActive(shouldBeVisible);
                    if (!shouldBeVisible) hiddenRows++;
                }
            }

            // Update column labels visibility
            for (int i = 0; i < MAX_GRID_SIZE; i++)
            {
                if (_columnLabelObjects[i] != null)
                {
                    bool shouldBeVisible = i < _currentGridSize;
                    _columnLabelObjects[i].SetActive(shouldBeVisible);
                    if (!shouldBeVisible) hiddenCols++;
                }
            }

            Debug.Log($"[PlayerGridPanel] Labels updated. Hidden: {hiddenRows} rows, {hiddenCols} columns");
        }

        private char GetColumnLetter(int columnIndex)
        {
            return (char)('A' + columnIndex);
        }
        #endregion

        #region Private Methods - Event Handlers
        private void HandleCellClicked(int column, int row)
        {
            if (!IsValidCoordinate(column, row)) return;

            Debug.Log($"[PlayerGridPanel] Cell clicked: {GetColumnLetter(column)}{row + 1}");
            OnCellClicked?.Invoke(column, row);
        }

        private void HandleCellHoverEnter(int column, int row)
        {
            if (!IsValidCoordinate(column, row)) return;
            OnCellHoverEnter?.Invoke(column, row);
        }

        private void HandleCellHoverExit(int column, int row)
        {
            if (!IsValidCoordinate(column, row)) return;
            OnCellHoverExit?.Invoke(column, row);
        }
        #endregion

        #region Editor Helpers
#if UNITY_EDITOR
        [TitleGroup("Debug")]
        [Button("Clear Grid (Editor)")]
        private void EditorClearGrid()
        {
            ClearGrid();
            _isInitialized = false;
        }

        [Button("Log Cell Count")]
        private void LogCellCount()
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
            Debug.Log($"[PlayerGridPanel] Total cells in array: {count}");
            Debug.Log($"[PlayerGridPanel] Children in container: {_gridContainer?.childCount ?? 0}");
            Debug.Log($"[PlayerGridPanel] GridLayoutGroup constraintCount: {_gridLayoutGroup?.constraintCount ?? -1}");
        }

        [Button("Log Label Status")]
        private void LogLabelStatus()
        {
            int rowCount = 0;
            int colCount = 0;

            for (int i = 0; i < MAX_GRID_SIZE; i++)
            {
                if (_rowLabelObjects[i] != null)
                {
                    rowCount++;
                    Debug.Log($"Row label {i}: {_rowLabelObjects[i].name} - Active: {_rowLabelObjects[i].activeSelf}");
                }
            }

            for (int i = 0; i < MAX_GRID_SIZE; i++)
            {
                if (_columnLabelObjects[i] != null)
                {
                    colCount++;
                    Debug.Log($"Column label {i}: {_columnLabelObjects[i].name} - Active: {_columnLabelObjects[i].activeSelf}");
                }
            }

            Debug.Log($"[PlayerGridPanel] Cached labels - Rows: {rowCount}, Columns: {colCount}");
        }
#endif
        #endregion
    }
}
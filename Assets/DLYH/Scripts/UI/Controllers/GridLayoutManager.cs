using UnityEngine;
using UnityEngine.UI;
using System;
using System.Linq;

namespace TecVooDoo.DontLoseYourHead.UI
{
    /// <summary>
    /// Manages grid layout, cell creation, and label visibility.
    /// Extracted from PlayerGridPanel to handle all layout-related responsibilities.
    /// </summary>
    public class GridLayoutManager
    {
        #region Constants
        public const int MAX_GRID_SIZE = 12;
        public const int MIN_GRID_SIZE = 6;

        private const float MAX_CELL_SIZE = 55f;
        private const float MIN_CELL_SIZE = 32f;
        private const float CELL_SPACING = 2f;
        #endregion

        #region Private Fields - References
        private readonly Transform _gridContainer;
        private readonly Transform _rowLabelsContainer;
        private readonly Transform _columnLabelsContainer;
        private readonly RectTransform _gridWithRowLabelsRect;
        private readonly LayoutElement _gridContainerLayout;
        private readonly LayoutElement _rowLabelsLayout;
        private readonly GridCellUI _cellPrefab;
        private readonly RectTransform _panelRectTransform;

        private GridLayoutGroup _gridLayoutGroup;
        private GameObject[] _rowLabelObjects = new GameObject[MAX_GRID_SIZE];
        private GameObject[] _columnLabelObjects = new GameObject[MAX_GRID_SIZE];
        #endregion

        #region Private Fields - State
        private float _currentCellSize = 40f;
        private bool _labelsInitialized = false;
        #endregion

        #region Delegates
        /// <summary>
        /// Callback invoked when a cell is created. Parameters: cell, column, row
        /// </summary>
        public Action<GridCellUI, int, int> OnCellCreated;
        #endregion

        #region Constructor
        /// <summary>
        /// Creates a new GridLayoutManager with the required references.
        /// </summary>
        public GridLayoutManager(
            Transform gridContainer,
            Transform rowLabelsContainer,
            Transform columnLabelsContainer,
            RectTransform gridWithRowLabelsRect,
            LayoutElement gridContainerLayout,
            LayoutElement rowLabelsLayout,
            GridCellUI cellPrefab,
            RectTransform panelRectTransform)
        {
            _gridContainer = gridContainer;
            _rowLabelsContainer = rowLabelsContainer;
            _columnLabelsContainer = columnLabelsContainer;
            _gridWithRowLabelsRect = gridWithRowLabelsRect;
            _gridContainerLayout = gridContainerLayout;
            _rowLabelsLayout = rowLabelsLayout;
            _cellPrefab = cellPrefab;
            _panelRectTransform = panelRectTransform;

            CacheGridLayoutGroup();
        }
        #endregion

        #region Properties
        public float CurrentCellSize => _currentCellSize;
        public bool IsInitialized => _labelsInitialized;
        #endregion

        #region Public Methods - Initialization
        /// <summary>
        /// Caches references to the GridLayoutGroup component.
        /// </summary>
        public void CacheGridLayoutGroup()
        {
            if (_gridLayoutGroup == null && _gridContainer != null)
            {
                _gridLayoutGroup = _gridContainer.GetComponent<GridLayoutGroup>();
                if (_gridLayoutGroup == null)
                {
                    Debug.LogError("[GridLayoutManager] GridContainer is missing GridLayoutGroup component!");
                }
            }
        }

        /// <summary>
        /// Caches existing label objects from the label containers.
        /// Call this before any layout operations.
        /// </summary>
        public void CacheExistingLabels()
        {
            // Clear existing cached references
            for (int i = 0; i < MAX_GRID_SIZE; i++)
            {
                _rowLabelObjects[i] = null;
                _columnLabelObjects[i] = null;
            }

            CacheRowLabels();
            CacheColumnLabels();
            _labelsInitialized = true;
        }

        private void CacheRowLabels()
        {
            if (_rowLabelsContainer == null) return;

            for (int i = 0; i < _rowLabelsContainer.childCount; i++)
            {
                Transform child = _rowLabelsContainer.GetChild(i);
                string childName = child.name;

                // Try to extract the number from the name (e.g., "Label_1" -> 1, "1" -> 1)
                for (int labelNum = 1; labelNum <= MAX_GRID_SIZE; labelNum++)
                {
                    if (childName.Contains(labelNum.ToString()) &&
                        (childName.Contains("Label") || childName.Contains("Row") || childName == labelNum.ToString()))
                    {
                        string numStr = labelNum.ToString();
                        bool exactMatch = childName.EndsWith(numStr) ||
                                          childName.EndsWith("_" + numStr) ||
                                          childName == numStr;

                        // For single digits, make sure it's not part of 10, 11, 12
                        if (labelNum < 10)
                        {
                            exactMatch = exactMatch && !childName.Contains("1" + numStr);
                        }

                        if (exactMatch && _rowLabelObjects[labelNum - 1] == null)
                        {
                            _rowLabelObjects[labelNum - 1] = child.gameObject;
                            break;
                        }
                    }
                }
            }

            int rowCount = _rowLabelObjects.Count(x => x != null);
            Debug.Log(string.Format("[GridLayoutManager] Cached {0} row labels by name", rowCount));
        }

        private void CacheColumnLabels()
        {
            if (_columnLabelsContainer == null) return;

            for (int i = 0; i < _columnLabelsContainer.childCount; i++)
            {
                Transform child = _columnLabelsContainer.GetChild(i);
                string childName = child.name;

                // Skip spacer
                if (childName.ToLower().Contains("spacer"))
                {
                    continue;
                }

                // Try to extract the letter from the name (e.g., "Label_A" -> A)
                for (int letterIndex = 0; letterIndex < MAX_GRID_SIZE; letterIndex++)
                {
                    char letter = (char)('A' + letterIndex);
                    if (childName.Contains(letter.ToString()) &&
                        (childName.Contains("Label") || childName.Contains("Col") || childName == letter.ToString()))
                    {
                        if (_columnLabelObjects[letterIndex] == null)
                        {
                            _columnLabelObjects[letterIndex] = child.gameObject;
                            break;
                        }
                    }
                }
            }

            int colCount = _columnLabelObjects.Count(x => x != null);
            Debug.Log(string.Format("[GridLayoutManager] Cached {0} column labels by name", colCount));
        }
        #endregion

        #region Public Methods - Layout Operations
        /// <summary>
        /// Updates the GridLayoutGroup constraint count for the current grid size.
        /// </summary>
        public void UpdateGridLayoutConstraint(int gridSize)
        {
            if (_gridLayoutGroup != null)
            {
                _gridLayoutGroup.constraintCount = gridSize;
                Debug.Log(string.Format("[GridLayoutManager] GridLayoutGroup constraintCount set to {0}", gridSize));
            }
        }

        /// <summary>
        /// Updates all panel heights and cell sizes for the current grid size.
        /// Skips dynamic sizing in Gameplay mode - uses prefab layout instead.
        /// </summary>
        public void UpdatePanelHeight(int gridSize, bool isGameplayMode)
        {
            // In Gameplay mode, use prefab sizing - don't override
            if (isGameplayMode)
            {
                Debug.Log("[GridLayoutManager] Gameplay mode - using prefab layout, skipping dynamic sizing");
                return;
            }

            // With horizontal layout, we have full panel height available
            float availableHeight = 1080f;

            // Fixed elements: header(~40) + word patterns(~120) + letter tracker(~80) + column labels(~30) + padding(~30)
            float fixedElementsHeight = 380f;
            float maxGridHeight = availableHeight - fixedElementsHeight;

            // Calculate optimal cell size
            float optimalCellSize = (maxGridHeight - ((gridSize - 1) * CELL_SPACING)) / gridSize;

            // For grids 9x9 and larger, use smaller cell sizes to fit better
            float effectiveMaxCellSize = gridSize >= 9 ? 45f : MAX_CELL_SIZE;
            float effectiveMinCellSize = gridSize >= 9 ? MIN_CELL_SIZE : 38f;

            // Clamp between min and max cell sizes
            _currentCellSize = Mathf.Clamp(optimalCellSize, effectiveMinCellSize, effectiveMaxCellSize);

            Debug.Log(string.Format("[GridLayoutManager] Grid {0}x{0}: optimal={1:F1}px, clamped={2:F1}px",
                gridSize, optimalCellSize, _currentCellSize));

            // Update GridLayoutGroup cell size
            UpdateGridLayoutGroupCellSize();

            float gridHeight = (gridSize * _currentCellSize) + ((gridSize - 1) * CELL_SPACING);
            float gridWidth = gridHeight;

            // Set grid container size
            UpdateGridContainerSize(gridHeight, gridWidth);

            // Update row labels
            UpdateRowLabelsLayout(gridSize, gridHeight);

            // Update column labels
            UpdateColumnLabelsLayout(gridSize);

            // Update GridWithRowLabels container
            UpdateGridWithRowLabelsContainer(gridHeight);

            // Force layout rebuild
            ForceLayoutRebuild();

            Debug.Log(string.Format("[GridLayoutManager] Cell size: {0:F1}px, Grid: {1:F0}px",
                _currentCellSize, gridHeight));
        }

        private void UpdateGridLayoutGroupCellSize()
        {
            if (_gridLayoutGroup != null)
            {
                _gridLayoutGroup.cellSize = new Vector2(_currentCellSize, _currentCellSize);
                _gridLayoutGroup.spacing = new Vector2(CELL_SPACING, CELL_SPACING);
            }
        }

        private void UpdateGridContainerSize(float gridHeight, float gridWidth)
        {
            if (_gridContainerLayout != null)
            {
                _gridContainerLayout.preferredHeight = gridHeight;
                _gridContainerLayout.preferredWidth = gridWidth;
                _gridContainerLayout.minHeight = gridHeight;
                _gridContainerLayout.minWidth = gridWidth;
            }
        }

        private void UpdateRowLabelsLayout(int gridSize, float gridHeight)
        {
            float rowLabelSize = _currentCellSize;

            if (_rowLabelsContainer == null) return;

            // Set width on row labels container to match cell size
            LayoutElement rowLabelsContainerLayout = _rowLabelsContainer.GetComponent<LayoutElement>();
            if (rowLabelsContainerLayout == null)
            {
                rowLabelsContainerLayout = _rowLabelsContainer.gameObject.AddComponent<LayoutElement>();
            }
            rowLabelsContainerLayout.preferredWidth = rowLabelSize;
            rowLabelsContainerLayout.minWidth = rowLabelSize;
            rowLabelsContainerLayout.flexibleWidth = 0;

            // Configure VerticalLayoutGroup to NOT stretch children
            VerticalLayoutGroup rowLayoutGroup = _rowLabelsContainer.GetComponent<VerticalLayoutGroup>();
            if (rowLayoutGroup != null)
            {
                rowLayoutGroup.spacing = CELL_SPACING;
                rowLayoutGroup.childControlHeight = false;
                rowLayoutGroup.childControlWidth = false;
                rowLayoutGroup.childForceExpandHeight = false;
                rowLayoutGroup.childForceExpandWidth = false;
                rowLayoutGroup.childAlignment = TextAnchor.UpperCenter;
            }

            // Set each row label to be square (same as cell size)
            for (int i = 0; i < MAX_GRID_SIZE; i++)
            {
                if (_rowLabelObjects[i] != null)
                {
                    UpdateLabelLayoutElement(_rowLabelObjects[i], rowLabelSize, rowLabelSize);
                }
            }

            // Set row labels container height
            if (_rowLabelsLayout != null)
            {
                _rowLabelsLayout.preferredHeight = gridHeight;
                _rowLabelsLayout.minHeight = gridHeight;
                _rowLabelsLayout.flexibleHeight = 0;
            }
        }

        private void UpdateColumnLabelsLayout(int gridSize)
        {
            float columnLabelHeight = _currentCellSize;
            float rowLabelSize = _currentCellSize;

            if (_columnLabelsContainer == null) return;

            // Set fixed height on column labels container
            LayoutElement colContainerLayout = _columnLabelsContainer.GetComponent<LayoutElement>();
            if (colContainerLayout == null)
            {
                colContainerLayout = _columnLabelsContainer.gameObject.AddComponent<LayoutElement>();
            }
            colContainerLayout.preferredHeight = columnLabelHeight;
            colContainerLayout.minHeight = columnLabelHeight;
            colContainerLayout.flexibleHeight = 0;

            // Configure HorizontalLayoutGroup to NOT stretch children
            HorizontalLayoutGroup colLayoutGroup = _columnLabelsContainer.GetComponent<HorizontalLayoutGroup>();
            if (colLayoutGroup != null)
            {
                colLayoutGroup.spacing = CELL_SPACING;
                colLayoutGroup.childControlWidth = false;
                colLayoutGroup.childControlHeight = false;
                colLayoutGroup.childForceExpandWidth = false;
                colLayoutGroup.childForceExpandHeight = false;
                colLayoutGroup.childAlignment = TextAnchor.MiddleLeft;
            }

            // Find and size the spacer to match row labels width
            UpdateColumnLabelsSpacer(rowLabelSize, columnLabelHeight);

            // Set each column label to exact cell size (square)
            for (int i = 0; i < MAX_GRID_SIZE; i++)
            {
                if (_columnLabelObjects[i] != null)
                {
                    UpdateLabelLayoutElement(_columnLabelObjects[i], _currentCellSize, columnLabelHeight);
                }
            }
        }

        private void UpdateColumnLabelsSpacer(float rowLabelSize, float columnLabelHeight)
        {
            if (_columnLabelsContainer == null) return;

            for (int i = 0; i < _columnLabelsContainer.childCount; i++)
            {
                Transform child = _columnLabelsContainer.GetChild(i);
                if (child.name.ToLower().Contains("spacer"))
                {
                    LayoutElement spacerLayout = child.GetComponent<LayoutElement>();
                    if (spacerLayout == null)
                    {
                        spacerLayout = child.gameObject.AddComponent<LayoutElement>();
                    }
                    // Spacer should be square, matching row label size
                    spacerLayout.preferredWidth = rowLabelSize;
                    spacerLayout.minWidth = rowLabelSize;
                    spacerLayout.preferredHeight = columnLabelHeight;
                    spacerLayout.minHeight = columnLabelHeight;
                    spacerLayout.flexibleWidth = 0;
                    spacerLayout.flexibleHeight = 0;

                    // Also set RectTransform directly
                    RectTransform spacerRect = child.GetComponent<RectTransform>();
                    if (spacerRect != null)
                    {
                        spacerRect.sizeDelta = new Vector2(rowLabelSize, columnLabelHeight);
                    }

                    Debug.Log(string.Format("[GridLayoutManager] Set spacer size to {0}x{1}px", rowLabelSize, columnLabelHeight));
                    break;
                }
            }
        }

        private void UpdateLabelLayoutElement(GameObject labelObj, float width, float height)
        {
            LayoutElement labelLayout = labelObj.GetComponent<LayoutElement>();
            if (labelLayout == null)
            {
                labelLayout = labelObj.AddComponent<LayoutElement>();
            }
            labelLayout.preferredWidth = width;
            labelLayout.minWidth = width;
            labelLayout.preferredHeight = height;
            labelLayout.minHeight = height;
            labelLayout.flexibleWidth = 0;
            labelLayout.flexibleHeight = 0;

            // Also set RectTransform directly
            RectTransform labelRect = labelObj.GetComponent<RectTransform>();
            if (labelRect != null)
            {
                labelRect.sizeDelta = new Vector2(width, height);
            }
        }

        private void UpdateGridWithRowLabelsContainer(float gridHeight)
        {
            if (_gridWithRowLabelsRect != null)
            {
                LayoutElement layoutElement = _gridWithRowLabelsRect.GetComponent<LayoutElement>();
                if (layoutElement != null)
                {
                    layoutElement.preferredHeight = gridHeight;
                    layoutElement.minHeight = gridHeight;
                }
            }
        }

        private void ForceLayoutRebuild()
        {
            Canvas.ForceUpdateCanvases();
            if (_panelRectTransform != null)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(_panelRectTransform);
            }
        }
        #endregion

        #region Public Methods - Label Visibility
        /// <summary>
        /// Updates label visibility based on the current grid size.
        /// Shows labels 1-gridSize, hides labels beyond.
        /// </summary>
        public void UpdateLabelVisibility(int gridSize)
        {
            Debug.Log(string.Format("[GridLayoutManager] === UpdateLabelVisibility START for {0}x{0} ===", gridSize));

            // Step 1: Enable all labels first
            EnableAllLabels();

            // Step 2: Force layout rebuild while all are active
            Canvas.ForceUpdateCanvases();
            RebuildLabelContainerLayouts();

            // Step 3: Now hide the labels beyond our grid size
            HideLabelsOutsideGridSize(gridSize);

            // Step 4: Final layout rebuild
            Canvas.ForceUpdateCanvases();
            RebuildLabelContainerLayouts();

            Debug.Log("[GridLayoutManager] === UpdateLabelVisibility END ===");
        }

        private void EnableAllLabels()
        {
            for (int i = 0; i < MAX_GRID_SIZE; i++)
            {
                if (_rowLabelObjects[i] != null)
                {
                    _rowLabelObjects[i].SetActive(true);
                }
                if (_columnLabelObjects[i] != null)
                {
                    _columnLabelObjects[i].SetActive(true);
                }
            }
        }

        private void HideLabelsOutsideGridSize(int gridSize)
        {
            for (int i = gridSize; i < MAX_GRID_SIZE; i++)
            {
                if (_rowLabelObjects[i] != null)
                {
                    _rowLabelObjects[i].SetActive(false);
                }
                if (_columnLabelObjects[i] != null)
                {
                    _columnLabelObjects[i].SetActive(false);
                }
            }
        }

        private void RebuildLabelContainerLayouts()
        {
            if (_rowLabelsContainer != null)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(_rowLabelsContainer as RectTransform);
            }
            if (_columnLabelsContainer != null)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(_columnLabelsContainer as RectTransform);
            }
        }
        #endregion

        #region Public Methods - Cell Creation
        /// <summary>
        /// Creates all cells for the specified grid size.
        /// Cells are created in row-major order to match GridLayoutGroup behavior.
        /// </summary>
        /// <param name="gridSize">The size of the grid (6-12)</param>
        /// <param name="cells">The cell array to populate</param>
        public void CreateCellsForCurrentSize(int gridSize, GridCellUI[,] cells)
        {
            if (_cellPrefab == null || _gridContainer == null)
            {
                Debug.LogError("[GridLayoutManager] Cell prefab or grid container is not assigned!");
                return;
            }

            // Create cells in row-major order (row 0, then row 1, etc.)
            // This matches how GridLayoutGroup lays out children
            for (int row = 0; row < gridSize; row++)
            {
                for (int col = 0; col < gridSize; col++)
                {
                    CreateCell(col, row, cells);
                }
            }

            Debug.Log(string.Format("[GridLayoutManager] Created {0} cells for {1}x{1} grid",
                gridSize * gridSize, gridSize));
        }

        private void CreateCell(int column, int row, GridCellUI[,] cells)
        {
            if (_cellPrefab == null || _gridContainer == null) return;

            GameObject cellGO = UnityEngine.Object.Instantiate(_cellPrefab.gameObject, _gridContainer);
            cellGO.name = string.Format("Cell_{0}{1}", GetColumnLetter(column), row + 1);

            GridCellUI cell = cellGO.GetComponent<GridCellUI>();
            if (cell != null)
            {
                cell.Initialize(column, row);
                cells[column, row] = cell;

                // Notify owner to wire up events
                OnCellCreated?.Invoke(cell, column, row);
            }
        }

        /// <summary>
        /// Converts a column index to a letter (A-L).
        /// </summary>
        public char GetColumnLetter(int column)
        {
            return (char)('A' + column);
        }
        #endregion

        #region Public Methods - Cleanup
        /// <summary>
        /// Clears all cells from the grid container.
        /// </summary>
        public void ClearGrid()
        {
            if (_gridContainer == null) return;

            for (int i = _gridContainer.childCount - 1; i >= 0; i--)
            {
                if (Application.isPlaying)
                {
                    UnityEngine.Object.Destroy(_gridContainer.GetChild(i).gameObject);
                }
                else
                {
                    UnityEngine.Object.DestroyImmediate(_gridContainer.GetChild(i).gameObject);
                }
            }
        }
        #endregion
    }
}

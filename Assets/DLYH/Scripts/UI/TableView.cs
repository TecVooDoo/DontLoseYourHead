using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace DLYH.TableUI
{
    /// <summary>
    /// UI Toolkit renderer for TableModel.
    /// Generates cells once on initialization, then updates styles/content on model changes.
    /// No per-frame allocations.
    /// </summary>
    public class TableView
    {
        private TableModel _model;
        private VisualElement _root;
        private VisualElement _tableRoot;
        private VisualElement _measurementSlot; // Stable slot element for measuring available space (e.g., grid-area)
        private VisualElement[,] _cellElements;
        private Label[,] _cellLabels;
        private bool _geometryCallbackRegistered = false;
        private int _lastCalculatedCellSize = 0; // Track last calculated cell size to prevent unnecessary recalcs

        private int _lastVersion = -1;
        private Color _player1Color = ColorRules.SelectableColors[0]; // Blue default
        private Color _player2Color = ColorRules.SelectableColors[1]; // Purple default
        private bool _isSetupMode = true; // Default to setup mode
        private bool _isDefenseGrid = false; // If true, show letters in Revealed state (player's own grid)

        // USS class names (cached to avoid string allocations)
        private static readonly string ClassTableRow = "table-row";
        private static readonly string ClassTableCell = "table-cell";
        private static readonly string ClassCellLabel = "table-cell-label";

        // Kind classes
        private static readonly string ClassCellSpacer = "cell-spacer";
        private static readonly string ClassCellWordSlot = "cell-word-slot";
        private static readonly string ClassCellHeaderCol = "cell-header-col";
        private static readonly string ClassCellHeaderRow = "cell-header-row";
        private static readonly string ClassCellGrid = "cell-grid";

        // Size classes - gradual scaling from 6x6 to 12x12
        private static readonly string ClassCellSizeTiny = "cell-size-tiny";         // 22px - for 12x12
        private static readonly string ClassCellSizeXSmall = "cell-size-xsmall";     // 24px - for 11x11
        private static readonly string ClassCellSizeSmall = "cell-size-small";       // 26px - for 10x10
        private static readonly string ClassCellSizeMedSmall = "cell-size-med-small";// 28px - for 9x9
        private static readonly string ClassCellSizeMedium = "cell-size-medium";     // 30px - for 8x8
        private static readonly string ClassCellSizeMedLarge = "cell-size-med-large";// 32px - for 7x7
        private static readonly string ClassCellSizeLarge = "cell-size-large";       // 34px - for 6x6

        // Current size class (determined by grid dimensions)
        private string _currentSizeClass = ClassCellSizeMedium;

        // Calculated cell size for viewport-aware sizing (0 = use CSS class defaults)
        private int _calculatedCellSize = 0;

        // Constants for container-aware sizing
        // IMPORTANT: CELL_MARGIN_PX must match USS margin: 1px on .table-cell
        private const int CELL_MARGIN_PX = 1;              // Must match USS margin value
        private const int MIN_CELL_SIZE = 18;              // Minimum readable cell size
        private const int MAX_CELL_SIZE = 52;              // Maximum cell size (prevent oversized cells)

        // State classes - must match TableCellState enum order
        private static readonly string[] StateClasses = new string[]
        {
            "state-none",
            "state-normal",
            "state-disabled",
            "state-hidden",
            "state-selected",
            "state-hovered",
            "state-locked",
            "state-read-only",
            "state-placement-valid",
            "state-placement-invalid",
            "state-placement-path",
            "state-placement-anchor",
            "state-placement-second",
            "state-fog",
            "state-revealed",
            "state-found",       // Letter known but not all coords known (yellow + letter shown)
            "state-hit",
            "state-miss",
            "state-wrong-word",
            "state-warning"
        };

        /// <summary>
        /// Event fired when a cell is clicked. Parameters: row, col, cell data.
        /// </summary>
        public event Action<int, int, TableCell> OnCellClicked;

        /// <summary>
        /// Event fired when a cell is hovered. Parameters: row, col, cell data.
        /// </summary>
        public event Action<int, int, TableCell> OnCellHovered;

        /// <summary>
        /// Event fired when hover leaves a cell.
        /// </summary>
        public event Action OnCellHoverExit;

        /// <summary>
        /// Gets the root visual element containing all table content.
        /// Use this to reparent the table view to a different container.
        /// </summary>
        public VisualElement TableRoot => _tableRoot;

        /// <summary>
        /// Creates a new TableView attached to the given root element.
        /// </summary>
        public TableView(VisualElement root)
        {
            _root = root ?? throw new ArgumentNullException(nameof(root));
            _tableRoot = _root.Q<VisualElement>("table-root");

            if (_tableRoot == null)
            {
                // Create table root if not in UXML
                _tableRoot = new VisualElement();
                _tableRoot.name = "table-root";
                _tableRoot.AddToClassList("table-root");
                _root.Add(_tableRoot);
            }

            // Don't auto-detect measurement slot here - caller should set it explicitly
            // via SetMeasurementSlot() to ensure we measure from a stable layout slot
            _measurementSlot = null;
        }

        /// <summary>
        /// Sets the stable slot element used for measuring available space.
        /// IMPORTANT: Pass a layout-stable element like 'grid-area' that is sized by its parent,
        /// NOT a content-driven element like 'table-container' that shrinks with its children.
        /// This prevents cascading resize loops.
        /// </summary>
        public void SetMeasurementSlot(VisualElement slot)
        {
            if (slot == null) return;

            // Unregister old callback if needed
            if (_geometryCallbackRegistered && _measurementSlot != null)
            {
                _measurementSlot.UnregisterCallback<GeometryChangedEvent>(OnSlotGeometryChanged);
                _geometryCallbackRegistered = false;
            }

            _measurementSlot = slot;

            // Reset so first real resize is not skipped
            _lastCalculatedCellSize = 0;

            // Register geometry callback on new slot
            RegisterGeometryCallback();
        }

        /// <summary>
        /// Legacy method - redirects to SetMeasurementSlot.
        /// </summary>
        public void SetContainer(VisualElement container)
        {
            SetMeasurementSlot(container);
        }

        /// <summary>
        /// Registers the GeometryChangedEvent callback on the measurement slot.
        /// </summary>
        private void RegisterGeometryCallback()
        {
            if (_measurementSlot != null && !_geometryCallbackRegistered)
            {
                _measurementSlot.RegisterCallback<GeometryChangedEvent>(OnSlotGeometryChanged);
                _geometryCallbackRegistered = true;
            }
        }

        /// <summary>
        /// Called when the measurement slot's geometry changes (resize, orientation change, etc.)
        /// Because we measure a stable slot (not content-driven), this won't cascade.
        /// </summary>
        private void OnSlotGeometryChanged(GeometryChangedEvent evt)
        {
            // Only recalculate if we have a model bound
            if (_model == null || _cellElements == null) return;

            // Get slot dimensions
            float availW = _measurementSlot.contentRect.width;
            float availH = _measurementSlot.contentRect.height;

            if (availW <= 0 || availH <= 0) return;

            // Calculate what the new cell size would be
            int newCellSize = CalculateContainerAwareCellSize(_model.Rows, _model.Cols, availW, availH);

            // Guard: only apply if cell size actually changed (avoids unnecessary layout thrash)
            if (newCellSize == _lastCalculatedCellSize)
            {
                return;
            }

            _lastCalculatedCellSize = newCellSize;
            _calculatedCellSize = newCellSize;
            ApplyCellSizes(newCellSize);
        }

        /// <summary>
        /// Binds the view to a model and generates all cell elements.
        /// </summary>
        public void Bind(TableModel model)
        {
            if (_model != null)
            {
                _model.OnCellChanged -= HandleCellChanged;
                _model.OnCleared -= HandleModelCleared;
            }

            _model = model ?? throw new ArgumentNullException(nameof(model));
            _model.OnCellChanged += HandleCellChanged;
            _model.OnCleared += HandleModelCleared;

            // Reset so first resize is not skipped
            _lastCalculatedCellSize = 0;

            GenerateCells();
            RefreshAll();
        }

        /// <summary>
        /// Sets player colors for hit/reveal feedback.
        /// </summary>
        public void SetPlayerColors(Color player1, Color player2)
        {
            _player1Color = player1;
            _player2Color = player2;

            // Force refresh if bound
            if (_model != null)
            {
                RefreshAll();
            }
        }

        /// <summary>
        /// Sets whether the view is in setup mode (uses green for placements)
        /// or gameplay mode (uses player colors).
        /// </summary>
        public void SetSetupMode(bool isSetupMode)
        {
            _isSetupMode = isSetupMode;

            // Force refresh if bound
            if (_model != null)
            {
                RefreshAll();
            }
        }

        /// <summary>
        /// Sets whether this is a defense grid (player's own grid being attacked).
        /// Defense grids show letters even in Revealed state since the player can see their own letters.
        /// Attack grids hide letters in Revealed state since the opponent's letters are unknown.
        /// </summary>
        public void SetDefenseGrid(bool isDefense)
        {
            _isDefenseGrid = isDefense;

            // Force refresh if bound
            if (_model != null)
            {
                RefreshAll();
            }
        }

        /// <summary>
        /// Returns true if the view is in setup mode.
        /// </summary>
        public bool IsSetupMode => _isSetupMode;

        /// <summary>
        /// Determines the appropriate cell size class based on grid dimensions.
        /// Gradual scaling from large (6x6) to tiny (12x12).
        /// Each grid size gets its own cell size for smooth scaling.
        /// </summary>
        private string DetermineSizeClass(int rows, int cols)
        {
            int maxDimension = Mathf.Max(rows, cols);

            // Note: rows/cols include headers, so:
            // 6x6 grid = 7 rows/cols, 7x7 = 8, 8x8 = 9, 9x9 = 10, 10x10 = 11, 11x11 = 12, 12x12 = 13
            if (maxDimension >= 13) // 12x12 grid
            {
                return ClassCellSizeTiny;      // 22px cells
            }
            else if (maxDimension >= 12) // 11x11 grid
            {
                return ClassCellSizeXSmall;    // 24px cells
            }
            else if (maxDimension >= 11) // 10x10 grid
            {
                return ClassCellSizeSmall;     // 26px cells
            }
            else if (maxDimension >= 10) // 9x9 grid
            {
                return ClassCellSizeMedSmall;  // 28px cells
            }
            else if (maxDimension >= 9) // 8x8 grid
            {
                return ClassCellSizeMedium;    // 30px cells
            }
            else if (maxDimension >= 8) // 7x7 grid
            {
                return ClassCellSizeMedLarge;  // 32px cells
            }
            else // 6x6 grid and smaller
            {
                return ClassCellSizeLarge;     // 34px cells
            }
        }

        /// <summary>
        /// Calculates the optimal cell size based on the container's actual resolved dimensions.
        /// Uses WIDTH only - vertical overflow triggers scroll.
        /// Returns the cell size in pixels.
        /// </summary>
        private int CalculateContainerAwareCellSize(int rows, int cols, float availableWidth, float availableHeight)
        {
            // Each cell occupies (cellSize + margin on both sides)
            float perCellMargin = CELL_MARGIN_PX * 2f;

            // Calculate max cell size that fits width constraint only
            // Height overflow is handled by scroll view
            float sizeFromWidth = (availableWidth / cols) - perCellMargin;

            int idealCellSize = Mathf.FloorToInt(sizeFromWidth);

            // Clamp to reasonable bounds
            int finalCellSize = Mathf.Clamp(idealCellSize, MIN_CELL_SIZE, MAX_CELL_SIZE);

            int gridSize = rows - 1; // rows includes header
            Debug.Log($"[TableView] Container sizing: Available {availableWidth:F0}x{availableHeight:F0}px, Grid {gridSize}x{gridSize} ({rows} rows), " +
                      $"FromW={sizeFromWidth:F1}px -> Final {finalCellSize}px");

            return finalCellSize;
        }

        /// <summary>
        /// Fallback calculation using Screen dimensions when container size is not yet available.
        /// Uses a conservative estimate for the grid area.
        /// </summary>
        private int CalculateFallbackCellSize(int rows, int cols)
        {
            // Use screen dimensions as fallback, with conservative estimates
            float availableWidth = Screen.width * 0.9f;  // Assume 90% of screen width
            float availableHeight = Screen.height * 0.5f; // Assume 50% of screen height for grid

            return CalculateContainerAwareCellSize(rows, cols, availableWidth, availableHeight);
        }

        /// <summary>
        /// Gets the font size appropriate for a given cell size.
        /// </summary>
        private int GetFontSizeForCellSize(int cellSize)
        {
            // Font size roughly 40-50% of cell size, with minimum of 9px
            int fontSize = Mathf.Max(9, (int)(cellSize * 0.45f));
            return fontSize;
        }

        /// <summary>
        /// Generates all cell VisualElements. Called once on bind.
        /// </summary>
        private void GenerateCells()
        {
            // Clear existing
            _tableRoot.Clear();

            int rows = _model.Rows;
            int cols = _model.Cols;

            // Determine cell size based on grid dimensions (CSS class fallback)
            _currentSizeClass = DetermineSizeClass(rows, cols);

            // Try to calculate cell size from measurement slot, fall back to screen-based estimate
            if (_measurementSlot != null && _measurementSlot.resolvedStyle.width > 0 && _measurementSlot.resolvedStyle.height > 0)
            {
                float availW = _measurementSlot.contentRect.width;
                float availH = _measurementSlot.contentRect.height;
                _calculatedCellSize = CalculateContainerAwareCellSize(rows, cols, availW, availH);
            }
            else
            {
                // Slot not yet laid out, use fallback
                _calculatedCellSize = CalculateFallbackCellSize(rows, cols);
            }
            _lastCalculatedCellSize = _calculatedCellSize;

            _cellElements = new VisualElement[rows, cols];
            _cellLabels = new Label[rows, cols];

            for (int row = 0; row < rows; row++)
            {
                VisualElement rowElement = new VisualElement();
                rowElement.AddToClassList(ClassTableRow);

                for (int col = 0; col < cols; col++)
                {
                    VisualElement cellElement = CreateCellElement(row, col);
                    _cellElements[row, col] = cellElement;
                    rowElement.Add(cellElement);
                }

                _tableRoot.Add(rowElement);
            }

            // Note: We do NOT set explicit size on _tableRoot anymore.
            // Since we measure from a stable slot (grid-area), there's no feedback loop.

            // Register geometry callback to recalculate when slot resizes
            RegisterGeometryCallback();
        }

        /// <summary>
        /// Creates a single cell VisualElement with label and event handlers.
        /// </summary>
        private VisualElement CreateCellElement(int row, int col)
        {
            VisualElement cell = new VisualElement();
            cell.AddToClassList(ClassTableCell);
            cell.AddToClassList(_currentSizeClass);

            // Apply viewport-aware sizing via inline styles (overrides CSS class)
            if (_calculatedCellSize > 0)
            {
                cell.style.width = _calculatedCellSize;
                cell.style.height = _calculatedCellSize;
            }

            Label label = new Label();
            label.AddToClassList(ClassCellLabel);

            // Apply viewport-aware font size
            if (_calculatedCellSize > 0)
            {
                label.style.fontSize = GetFontSizeForCellSize(_calculatedCellSize);
            }

            cell.Add(label);
            _cellLabels[row, col] = label;

            // Capture row/col for closures
            int capturedRow = row;
            int capturedCol = col;

            cell.RegisterCallback<ClickEvent>(evt =>
            {
                if (_model != null)
                {
                    TableCell cellData = _model.GetCell(capturedRow, capturedCol);
                    OnCellClicked?.Invoke(capturedRow, capturedCol, cellData);
                }
            });

            cell.RegisterCallback<MouseEnterEvent>(evt =>
            {
                if (_model != null)
                {
                    TableCell cellData = _model.GetCell(capturedRow, capturedCol);
                    OnCellHovered?.Invoke(capturedRow, capturedCol, cellData);
                }
            });

            cell.RegisterCallback<MouseLeaveEvent>(evt =>
            {
                OnCellHoverExit?.Invoke();
            });

            return cell;
        }

        /// <summary>
        /// Refreshes all cells from the model.
        /// </summary>
        public void RefreshAll()
        {
            if (_model == null || _cellElements == null)
            {
                return;
            }

            for (int row = 0; row < _model.Rows; row++)
            {
                for (int col = 0; col < _model.Cols; col++)
                {
                    UpdateCellVisual(row, col);
                }
            }

            _lastVersion = _model.Version;
            _model.ClearDirty();
        }

        /// <summary>
        /// Updates if model has changed. Call from Update loop if desired.
        /// </summary>
        public void RefreshIfDirty()
        {
            if (_model != null && _model.Dirty && _model.Version != _lastVersion)
            {
                RefreshAll();
            }
        }

        /// <summary>
        /// Updates a single cell's visual state.
        /// </summary>
        private void UpdateCellVisual(int row, int col)
        {
            TableCell cell = _model.GetCell(row, col);
            VisualElement element = _cellElements[row, col];
            Label label = _cellLabels[row, col];

            // Update text - hide letters in Fog state, and in Revealed state for attack grids
            // Fog = completely hidden
            // Revealed = coordinate hit but letter not guessed yet (yellow)
            //   - Attack grid: hide letter (opponent's letters are unknown)
            //   - Defense grid: show letter (player's own letters are visible)
            if (cell.State == TableCellState.Fog)
            {
                // Fog - never show letters
                label.text = string.Empty;
            }
            else if (cell.State == TableCellState.Revealed && !_isDefenseGrid)
            {
                // Revealed on attack grid - don't show letter (opponent's letters unknown)
                label.text = string.Empty;
            }
            else if (cell.TextChar != '\0')
            {
                label.text = cell.TextChar.ToString();
            }
            else if (cell.IntValue >= 0)
            {
                label.text = cell.IntValue.ToString();
            }
            else
            {
                label.text = string.Empty;
            }

            // Clear previous kind classes
            element.RemoveFromClassList(ClassCellSpacer);
            element.RemoveFromClassList(ClassCellWordSlot);
            element.RemoveFromClassList(ClassCellHeaderCol);
            element.RemoveFromClassList(ClassCellHeaderRow);
            element.RemoveFromClassList(ClassCellGrid);

            // Apply kind class
            switch (cell.Kind)
            {
                case TableCellKind.Spacer:
                    element.AddToClassList(ClassCellSpacer);
                    break;
                case TableCellKind.WordSlot:
                    element.AddToClassList(ClassCellWordSlot);
                    break;
                case TableCellKind.HeaderCol:
                    element.AddToClassList(ClassCellHeaderCol);
                    break;
                case TableCellKind.HeaderRow:
                    element.AddToClassList(ClassCellHeaderRow);
                    break;
                case TableCellKind.GridCell:
                    element.AddToClassList(ClassCellGrid);
                    break;
            }

            // Clear previous state classes
            for (int i = 0; i < StateClasses.Length; i++)
            {
                element.RemoveFromClassList(StateClasses[i]);
            }

            // Apply state class
            int stateIndex = (int)cell.State;
            if (stateIndex >= 0 && stateIndex < StateClasses.Length)
            {
                element.AddToClassList(StateClasses[stateIndex]);
            }

            // Apply dynamic colors based on state and mode
            if (cell.State == TableCellState.Hit)
            {
                Color ownerColor = GetOwnerColor(cell.Owner);
                element.style.backgroundColor = ownerColor;
                label.style.color = ColorRules.GetContrastingTextColor(ownerColor);
            }
            else if (cell.State == TableCellState.Miss)
            {
                // Coordinate miss - red (no letter at this position)
                element.style.backgroundColor = ColorRules.SystemRed;
                label.style.color = Color.white;
            }
            else if (cell.State == TableCellState.Revealed)
            {
                // Coordinate hit but letter not yet guessed - yellow
                element.style.backgroundColor = ColorRules.SystemYellow;
                label.style.color = ColorRules.GetContrastingTextColor(ColorRules.SystemYellow);
            }
            else if (cell.State == TableCellState.Found)
            {
                // Letter known but not all coordinates for this letter known - yellow with letter shown
                element.style.backgroundColor = ColorRules.SystemYellow;
                label.style.color = ColorRules.GetContrastingTextColor(ColorRules.SystemYellow);
            }
            else if (cell.State == TableCellState.PlacementAnchor ||
                     cell.State == TableCellState.PlacementSecond ||
                     cell.State == TableCellState.PlacementPath)
            {
                // Handle placement preview states - use green during setup
                Color placementColor = ColorRules.GetPlacementColor(cell.State, _player1Color, _isSetupMode);
                element.style.backgroundColor = placementColor;
                label.style.color = ColorRules.GetContrastingTextColor(placementColor);
            }
            else if (cell.State == TableCellState.Normal && cell.TextChar != '\0' && cell.Kind == TableCellKind.GridCell)
            {
                // During setup, placed letters show green; during gameplay, player color
                Color cellColor = _isSetupMode ? ColorRules.GetSetupPlacedColor() : GetOwnerColor(cell.Owner);
                element.style.backgroundColor = cellColor;
                label.style.color = ColorRules.GetContrastingTextColor(cellColor);
            }
            else
            {
                // Reset to USS-defined colors
                element.style.backgroundColor = StyleKeyword.Null;
                label.style.color = StyleKeyword.Null;
            }
        }

        /// <summary>
        /// Gets the color for a cell owner.
        /// Uses ColorRules.GetOwnerColor to maintain single source of truth.
        /// </summary>
        private Color GetOwnerColor(CellOwner owner)
        {
            return ColorRules.GetOwnerColor(owner, _player1Color, _player2Color);
        }

        /// <summary>
        /// Handles model cell change event.
        /// </summary>
        private void HandleCellChanged(int row, int col, TableCell cell)
        {
            if (_cellElements != null && row >= 0 && row < _model.Rows && col >= 0 && col < _model.Cols)
            {
                UpdateCellVisual(row, col);
            }
        }

        /// <summary>
        /// Handles model cleared/reset event.
        /// </summary>
        private void HandleModelCleared()
        {
            GenerateCells();
            RefreshAll();
        }

        /// <summary>
        /// Cleans up event subscriptions.
        /// </summary>
        public void Unbind()
        {
            // Unregister geometry callback
            if (_geometryCallbackRegistered && _measurementSlot != null)
            {
                _measurementSlot.UnregisterCallback<GeometryChangedEvent>(OnSlotGeometryChanged);
                _geometryCallbackRegistered = false;
            }

            if (_model != null)
            {
                _model.OnCellChanged -= HandleCellChanged;
                _model.OnCleared -= HandleModelCleared;
                _model = null;
            }
        }

        /// <summary>
        /// Gets the current size class name for coordinating with word rows.
        /// Returns "tiny", "xsmall", "small", "med-small", "medium", "med-large", or "large".
        /// </summary>
        public string GetSizeClassName()
        {
            if (_currentSizeClass == ClassCellSizeTiny) return "tiny";
            if (_currentSizeClass == ClassCellSizeXSmall) return "xsmall";
            if (_currentSizeClass == ClassCellSizeSmall) return "small";
            if (_currentSizeClass == ClassCellSizeMedSmall) return "med-small";
            if (_currentSizeClass == ClassCellSizeMedLarge) return "med-large";
            if (_currentSizeClass == ClassCellSizeLarge) return "large";
            return "medium";
        }

        /// <summary>
        /// Gets the calculated cell size in pixels for viewport-aware sizing.
        /// Returns 0 if using CSS class defaults.
        /// </summary>
        public int GetCalculatedCellSize()
        {
            return _calculatedCellSize;
        }

        /// <summary>
        /// Gets the font size for the current calculated cell size.
        /// Returns 0 if using CSS class defaults.
        /// </summary>
        public int GetCalculatedFontSize()
        {
            if (_calculatedCellSize <= 0) return 0;
            return GetFontSizeForCellSize(_calculatedCellSize);
        }

        /// <summary>
        /// Forces recalculation of cell sizes based on container dimensions.
        /// Automatically called on GeometryChangedEvent.
        /// </summary>
        public void RecalculateSizes()
        {
            RecalculateSizesFromContainer();
        }

        /// <summary>
        /// Recalculates cell sizes using the measurement slot's actual resolved dimensions.
        /// This is the preferred method - uses real layout measurements instead of Screen size.
        /// </summary>
        private void RecalculateSizesFromContainer()
        {
            if (_model == null || _cellElements == null) return;

            int rows = _model.Rows;
            int cols = _model.Cols;

            // Get slot dimensions
            float availW = 0;
            float availH = 0;

            if (_measurementSlot != null && _measurementSlot.resolvedStyle.width > 0)
            {
                availW = _measurementSlot.contentRect.width;
                availH = _measurementSlot.contentRect.height;
            }

            // If slot size is valid, use it; otherwise use fallback
            int newCellSize;
            if (availW > 0 && availH > 0)
            {
                newCellSize = CalculateContainerAwareCellSize(rows, cols, availW, availH);
            }
            else
            {
                newCellSize = CalculateFallbackCellSize(rows, cols);
            }

            // Only apply if size actually changed
            if (newCellSize != _lastCalculatedCellSize)
            {
                _lastCalculatedCellSize = newCellSize;
                _calculatedCellSize = newCellSize;
                ApplyCellSizes(newCellSize);
            }
        }

        /// <summary>
        /// Applies the calculated cell size to all cells.
        /// </summary>
        private void ApplyCellSizes(int cellSize)
        {
            if (_cellElements == null || cellSize <= 0) return;

            int fontSize = GetFontSizeForCellSize(cellSize);

            for (int row = 0; row < _model.Rows; row++)
            {
                for (int col = 0; col < _model.Cols; col++)
                {
                    VisualElement cell = _cellElements[row, col];
                    if (cell != null)
                    {
                        cell.style.width = cellSize;
                        cell.style.height = cellSize;
                    }

                    Label label = _cellLabels[row, col];
                    if (label != null)
                    {
                        label.style.fontSize = fontSize;
                    }
                }
            }

            // Note: We do NOT set explicit size on _tableRoot anymore.
            // Since we measure from a stable slot, there's no feedback loop.
        }
    }
}

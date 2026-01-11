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
        private VisualElement[,] _cellElements;
        private Label[,] _cellLabels;

        private int _lastVersion = -1;
        private Color _player1Color = ColorRules.SelectableColors[0]; // Blue default
        private Color _player2Color = ColorRules.SelectableColors[1]; // Purple default
        private bool _isSetupMode = true; // Default to setup mode

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

        // Size classes
        private static readonly string ClassCellSizeTiny = "cell-size-tiny";
        private static readonly string ClassCellSizeSmall = "cell-size-small";
        private static readonly string ClassCellSizeMedium = "cell-size-medium";
        private static readonly string ClassCellSizeLarge = "cell-size-large";

        // Current size class (determined by grid dimensions)
        private string _currentSizeClass = ClassCellSizeMedium;

        // State classes
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
        /// Returns true if the view is in setup mode.
        /// </summary>
        public bool IsSetupMode => _isSetupMode;

        /// <summary>
        /// Determines the appropriate cell size class based on grid dimensions.
        /// Larger grids get smaller cells to fit on screen.
        /// </summary>
        private string DetermineSizeClass(int rows, int cols)
        {
            int maxDimension = Mathf.Max(rows, cols);

            if (maxDimension >= 12)
            {
                return ClassCellSizeTiny;
            }
            else if (maxDimension >= 10)
            {
                return ClassCellSizeSmall;
            }
            else if (maxDimension >= 8)
            {
                return ClassCellSizeMedium;
            }
            else
            {
                return ClassCellSizeLarge;
            }
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

            // Determine cell size based on grid dimensions
            _currentSizeClass = DetermineSizeClass(rows, cols);

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
        }

        /// <summary>
        /// Creates a single cell VisualElement with label and event handlers.
        /// </summary>
        private VisualElement CreateCellElement(int row, int col)
        {
            VisualElement cell = new VisualElement();
            cell.AddToClassList(ClassTableCell);
            cell.AddToClassList(_currentSizeClass);

            Label label = new Label();
            label.AddToClassList(ClassCellLabel);
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

            // Update text
            if (cell.TextChar != '\0')
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
        /// </summary>
        private Color GetOwnerColor(CellOwner owner)
        {
            switch (owner)
            {
                case CellOwner.Player1:
                    return _player1Color;
                case CellOwner.Player2:
                    return _player2Color;
                case CellOwner.ExecutionerAI:
                    return new Color(0.6f, 0.1f, 0.15f, 1f);
                case CellOwner.PhantomAI:
                    return new Color(0.5f, 0.5f, 0.6f, 1f);
                default:
                    return ColorRules.CellDefault;
            }
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
            if (_model != null)
            {
                _model.OnCellChanged -= HandleCellChanged;
                _model.OnCleared -= HandleModelCleared;
                _model = null;
            }
        }

        /// <summary>
        /// Gets the current size class name for coordinating with word rows.
        /// Returns "tiny", "small", "medium", or "large".
        /// </summary>
        public string GetSizeClassName()
        {
            if (_currentSizeClass == ClassCellSizeTiny) return "tiny";
            if (_currentSizeClass == ClassCellSizeSmall) return "small";
            if (_currentSizeClass == ClassCellSizeLarge) return "large";
            return "medium";
        }
    }
}

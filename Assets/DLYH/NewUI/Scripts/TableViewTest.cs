using UnityEngine;
using UnityEngine.UIElements;

namespace DLYH.TableUI
{
    /// <summary>
    /// Test MonoBehaviour for TableView. Attach to a GameObject with UIDocument.
    /// Creates a sample table layout and allows testing interactions.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public class TableViewTest : MonoBehaviour
    {
        [Header("Test Configuration")]
        [SerializeField] private int _gridSize = 6;
        [SerializeField] private int _wordCount = 3;

        [Header("Player Colors")]
        [SerializeField] private Color _player1Color = new Color(0.2f, 0.4f, 0.9f, 1f);
        [SerializeField] private Color _player2Color = new Color(0.6f, 0.2f, 0.8f, 1f);

        // Note: Word rows are now separate from the table (WordRowsContainer)

        private UIDocument _uiDocument;
        private TableModel _model;
        private TableView _view;
        private TableLayout _layout;

        private void Awake()
        {
            _uiDocument = GetComponent<UIDocument>();
        }

        private void Start()
        {
            InitializeTable();
        }

        private void InitializeTable()
        {
            // Create layout
            _layout = TableLayout.CreateForSetup(_gridSize, _wordCount);

            // Create model
            _model = new TableModel();
            _model.Initialize(_layout);

            // Create view
            VisualElement root = _uiDocument.rootVisualElement;
            _view = new TableView(root);
            _view.SetPlayerColors(_player1Color, _player2Color);
            _view.Bind(_model);

            // Subscribe to events
            _view.OnCellClicked += HandleCellClicked;
            _view.OnCellHovered += HandleCellHovered;

            Debug.Log($"[TableViewTest] Initialized {_layout.TotalRows}x{_layout.TotalCols} table (grid: {_gridSize}x{_gridSize}, words: {_wordCount})");
        }

        private void HandleCellClicked(int row, int col, TableCell cell)
        {
            Debug.Log($"[TableViewTest] Clicked ({row}, {col}) - Kind: {cell.Kind}, State: {cell.State}, Char: '{cell.TextChar}'");

            // Demo: cycle through states on grid cells
            if (cell.Kind == TableCellKind.GridCell)
            {
                CycleGridCellState(row, col, cell);
            }
        }

        private void CycleGridCellState(int row, int col, TableCell cell)
        {
            // Cycle: Fog -> Hit -> Revealed -> Miss -> Fog
            TableCellState newState;
            CellOwner newOwner = cell.Owner;

            switch (cell.State)
            {
                case TableCellState.Fog:
                    newState = TableCellState.Hit;
                    newOwner = CellOwner.Player;
                    _model.SetCellChar(row, col, 'X');
                    break;
                case TableCellState.Hit:
                    newState = TableCellState.Revealed;
                    newOwner = CellOwner.None;
                    break;
                case TableCellState.Revealed:
                    newState = TableCellState.Miss;
                    break;
                default:
                    newState = TableCellState.Fog;
                    _model.SetCellChar(row, col, '\0');
                    break;
            }

            _model.SetCellState(row, col, newState);
            _model.SetCellOwner(row, col, newOwner);
        }

        private void HandleCellHovered(int row, int col, TableCell cell)
        {
            // Optional: show hover feedback in console
            // Debug.Log($"[TableViewTest] Hover ({row}, {col})");
        }

        private void OnDestroy()
        {
            if (_view != null)
            {
                _view.OnCellClicked -= HandleCellClicked;
                _view.OnCellHovered -= HandleCellHovered;
                _view.Unbind();
            }
        }

        // === Editor Test Buttons ===

#if UNITY_EDITOR
        [ContextMenu("Test: Set Random Grid Letters")]
        private void TestSetRandomGridLetters()
        {
            if (_model == null) return;

            for (int gridRow = 0; gridRow < _gridSize; gridRow++)
            {
                for (int gridCol = 0; gridCol < _gridSize; gridCol++)
                {
                    if (Random.value > 0.7f)
                    {
                        char letter = (char)('A' + Random.Range(0, 26));
                        _model.SetGridCellLetter(gridRow, gridCol, letter);
                        _model.SetGridCellState(gridRow, gridCol, TableCellState.Hit);
                        _model.SetGridCellOwner(gridRow, gridCol,
                            Random.value > 0.5f ? CellOwner.Player : CellOwner.Opponent);
                    }
                }
            }
            Debug.Log("[TableViewTest] Set random grid letters");
        }

        [ContextMenu("Test: Clear Grid")]
        private void TestClearGrid()
        {
            if (_model == null) return;

            for (int gridRow = 0; gridRow < _gridSize; gridRow++)
            {
                for (int gridCol = 0; gridCol < _gridSize; gridCol++)
                {
                    _model.SetGridCellLetter(gridRow, gridCol, '\0');
                    _model.SetGridCellState(gridRow, gridCol, TableCellState.Fog);
                    _model.SetGridCellOwner(gridRow, gridCol, CellOwner.None);
                }
            }
            Debug.Log("[TableViewTest] Cleared grid");
        }

        [ContextMenu("Test: Show Placement Valid")]
        private void TestShowPlacementValid()
        {
            if (_model == null) return;

            // Simulate word placement preview
            _model.SetGridCellState(0, 0, TableCellState.PlacementAnchor);
            _model.SetGridCellState(0, 1, TableCellState.PlacementPath);
            _model.SetGridCellState(0, 2, TableCellState.PlacementPath);
            _model.SetGridCellState(0, 3, TableCellState.PlacementSecond);

            Debug.Log("[TableViewTest] Showing placement preview");
        }

        [ContextMenu("Test: Show Placement Invalid")]
        private void TestShowPlacementInvalid()
        {
            if (_model == null) return;

            _model.SetGridCellState(1, 0, TableCellState.PlacementInvalid);
            _model.SetGridCellState(1, 1, TableCellState.PlacementInvalid);

            Debug.Log("[TableViewTest] Showing invalid placement");
        }

        [ContextMenu("Test: Reset Model")]
        private void TestResetModel()
        {
            if (_model == null) return;

            _model.Clear();
            Debug.Log("[TableViewTest] Model reset");
        }

        [ContextMenu("Test: Change Grid Size (8x8, 4 words)")]
        private void TestChangeGridSize()
        {
            _gridSize = 8;
            _wordCount = 4;

            _layout = TableLayout.CreateForSetup(_gridSize, _wordCount);
            _model.Initialize(_layout);

            Debug.Log($"[TableViewTest] Changed to {_gridSize}x{_gridSize} grid with {_wordCount} words");
        }
#endif
    }
}

using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Sirenix.OdinInspector;
using System;
using System.Collections.Generic;

namespace TecVooDoo.DontLoseYourHead.UI
{
    /// <summary>
    /// Manages a player's grid panel UI including the grid cells,
    /// row/column labels, letter tracker, and word patterns display.
    /// Supports dynamic grid sizes from 6x6 to 12x12.
    /// Operates in two modes: Setup (word entry/placement) and Gameplay (guessing).
    /// </summary>
    public class PlayerGridPanel : MonoBehaviour
    {
        #region Enums
        /// <summary>
        /// Operating mode of the panel
        /// </summary>
        public enum PanelMode
        {
            /// <summary>Setup phase - entering and placing words</summary>
            Setup,
            /// <summary>Gameplay phase - guessing opponent's words</summary>
            Gameplay
        }

        /// <summary>
        /// State of coordinate placement mode
        /// </summary>
        public enum PlacementState
        {
            /// <summary>Not in placement mode</summary>
            Inactive,
            /// <summary>Waiting for first letter position</summary>
            SelectingFirstCell,
            /// <summary>First letter placed, waiting for direction</summary>
            SelectingDirection
        }
        #endregion

        #region Constants
        public const int MAX_GRID_SIZE = 12;
        public const int MIN_GRID_SIZE = 6;
        public const int MAX_WORD_ROWS = 4;

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

        [TitleGroup("Setup Mode References")]
        [SerializeField, Tooltip("Button for random word placement")]
        private Button _randomPlacementButton;

        [SerializeField]
        private AutocompleteDropdown _autocompleteDropdown;
        #endregion

        #region Serialized Fields - Configuration
        [TitleGroup("Configuration")]
        [SerializeField, Range(MIN_GRID_SIZE, MAX_GRID_SIZE)]
        private int _currentGridSize = 8;

        [SerializeField]
        private Color _playerColor = Color.white;

        [SerializeField]
        private PanelMode _currentMode = PanelMode.Setup;

        [TitleGroup("Layout Settings")]
        [SerializeField, Tooltip("Height of non-grid elements (header, word patterns, letter tracker, column labels)")]
        private float _fixedElementsHeight = 300f;

        [TitleGroup("Placement Colors")]
        [SerializeField]
        private Color _cursorColor = new Color(1f, 1f, 0f, 1f);

        [SerializeField]
        private Color _validPlacementColor = new Color(0f, 1f, 0f, 0.7f);

        [SerializeField]
        private Color _invalidPlacementColor = new Color(1f, 0f, 0f, 0.7f);

        [SerializeField]
        private Color _placedLetterColor = new Color(0.5f, 0.8f, 1f, 1f);
        #endregion

        #region Private Fields - Grid
        private GridCellUI[,] _cells = new GridCellUI[MAX_GRID_SIZE, MAX_GRID_SIZE];
        private GameObject[] _rowLabelObjects = new GameObject[MAX_GRID_SIZE];
        private GameObject[] _columnLabelObjects = new GameObject[MAX_GRID_SIZE];
        private GridLayoutGroup _gridLayoutGroup;
        private RectTransform _panelRectTransform;
        private LayoutElement _panelLayoutElement;
        private bool _isInitialized;
        #endregion

        #region Private Fields - Letter Tracker
        private Dictionary<char, LetterButton> _letterButtons = new Dictionary<char, LetterButton>();
        private bool _letterButtonsCached;
        #endregion

        #region Private Fields - Word Patterns
        private List<WordPatternRow> _wordPatternRows = new List<WordPatternRow>();
        private int _selectedWordRowIndex = -1;
        #endregion

        #region Private Fields - Coordinate Placement
        private PlacementState _placementState = PlacementState.Inactive;
        private int _placementWordRowIndex = -1;
        private string _placementWord = "";
        private int _firstCellCol = -1;
        private int _firstCellRow = -1;
        private List<Vector2Int> _placedCellPositions = new List<Vector2Int>();
        private HashSet<Vector2Int> _allPlacedPositions = new HashSet<Vector2Int>();
        private Dictionary<Vector2Int, char> _placedLetters = new Dictionary<Vector2Int, char>();
        #endregion

        #region Events - Grid
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

        #region Events - Letter Tracker
        /// <summary>
        /// Fired when a letter button is clicked. Parameter: letter
        /// </summary>
        public event Action<char> OnLetterClicked;

        /// <summary>
        /// Fired when letter hover enters. Parameter: letter
        /// </summary>
        public event Action<char> OnLetterHoverEnter;

        /// <summary>
        /// Fired when letter hover exits. Parameter: letter
        /// </summary>
        public event Action<char> OnLetterHoverExit;
        #endregion

        #region Events - Word Patterns
        /// <summary>
        /// Fired when a word pattern row is selected. Parameter: row index (0-based)
        /// </summary>
        public event Action<int> OnWordRowSelected;

        /// <summary>
        /// Fired when coordinate mode is requested. Parameter: row index (0-based)
        /// </summary>
        public event Action<int> OnCoordinateModeRequested;

        /// <summary>
        /// Fired when a word is successfully placed. Parameters: row index, word, positions
        /// </summary>
        public event Action<int, string, List<Vector2Int>> OnWordPlaced;

        /// <summary>
        /// Fired when placement mode is cancelled
        /// </summary>
        public event Action OnPlacementCancelled;
        #endregion

        #region Properties
        public int CurrentGridSize => _currentGridSize;
        public bool IsInitialized => _isInitialized;
        public PanelMode CurrentMode => _currentMode;
        public PlacementState CurrentPlacementState => _placementState;
        public int SelectedWordRowIndex => _selectedWordRowIndex;
        public bool IsInPlacementMode => _placementState != PlacementState.Inactive;
        public int WordRowCount => _wordPatternRows.Count;
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            CachePanelReferences();
            CacheGridLayoutGroup();
            CacheExistingLabels();
        }

        private void Start()
        {
            CacheLetterButtons();
            CacheWordPatternRows();
        }

        private void OnEnable()
        {
            SubscribeToRandomPlacementButton();
        }

        private void OnDisable()
        {
            UnsubscribeFromRandomPlacementButton();
        }
        #endregion

        #region Public Methods - Mode
        /// <summary>
        /// Sets the operating mode of the panel.
        /// </summary>
        /// <param name="mode">Setup or Gameplay mode</param>
        public void SetMode(PanelMode mode)
        {
            _currentMode = mode;

            // Exit placement mode if switching modes
            if (_placementState != PlacementState.Inactive)
            {
                CancelPlacementMode();
            }

            UpdateModeVisuals();
        }

        private void UpdateModeVisuals()
        {
            // Update random placement button visibility
            if (_randomPlacementButton != null)
            {
                _randomPlacementButton.gameObject.SetActive(_currentMode == PanelMode.Setup);
            }

            // Update word pattern rows for mode
            foreach (var row in _wordPatternRows)
            {
                // WordPatternRow handles its own button visibility based on state
            }
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

            // Clear placement tracking
            _allPlacedPositions.Clear();
            _placedLetters.Clear();

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

            _allPlacedPositions.Clear();
            _placedLetters.Clear();
        }
        #endregion

        #region Public Methods - Letter Tracker
        /// <summary>
        /// Gets a letter button by its letter.
        /// </summary>
        public LetterButton GetLetterButton(char letter)
        {
            char upperLetter = char.ToUpper(letter);
            if (_letterButtons.TryGetValue(upperLetter, out LetterButton button))
            {
                return button;
            }
            return null;
        }

        /// <summary>
        /// Sets the state of a letter button.
        /// </summary>
        public void SetLetterState(char letter, LetterButton.LetterState state)
        {
            var button = GetLetterButton(letter);
            if (button != null)
            {
                button.SetState(state);
            }
        }

        /// <summary>
        /// Resets all letter buttons to normal state.
        /// </summary>
        public void ResetAllLetterButtons()
        {
            foreach (var button in _letterButtons.Values)
            {
                button.ResetState();
            }
        }

        /// <summary>
        /// Sets whether letter buttons are interactable.
        /// </summary>
        public void SetLetterButtonsInteractable(bool interactable)
        {
            foreach (var button in _letterButtons.Values)
            {
                button.IsInteractable = interactable;
            }
        }

        /// <summary>
        /// Caches letter button references from the letter tracker container.
        /// </summary>
        [Button("Cache Letter Buttons")]
        public void CacheLetterButtons()
        {
            _letterButtons.Clear();

            if (_letterTrackerContainer == null)
            {
                Debug.LogWarning("[PlayerGridPanel] Letter tracker container not assigned.");
                return;
            }

            // Find all LetterButton components in children
            var buttons = _letterTrackerContainer.GetComponentsInChildren<LetterButton>(true);

            foreach (var button in buttons)
            {
                // Ensure the button is initialized (auto-detects letter from text or name)
                button.EnsureInitialized();

                // Skip if still not initialized or invalid letter
                if (!button.IsInitialized || button.Letter == '\0')
                {
                    Debug.LogWarning($"[PlayerGridPanel] LetterButton on {button.gameObject.name} could not be initialized.");
                    continue;
                }

                // Subscribe to events
                button.OnLetterClicked -= HandleLetterClicked;
                button.OnLetterClicked += HandleLetterClicked;
                button.OnLetterHoverEnter -= HandleLetterHoverEnter;
                button.OnLetterHoverEnter += HandleLetterHoverEnter;
                button.OnLetterHoverExit -= HandleLetterHoverExit;
                button.OnLetterHoverExit += HandleLetterHoverExit;

                // Add to dictionary (skip duplicates)
                if (!_letterButtons.ContainsKey(button.Letter))
                {
                    _letterButtons[button.Letter] = button;
                }
                else
                {
                    Debug.LogWarning($"[PlayerGridPanel] Duplicate letter button for '{button.Letter}' on {button.gameObject.name}");
                }
            }

            _letterButtonsCached = true;
            Debug.Log($"[PlayerGridPanel] Cached {_letterButtons.Count} letter buttons (found {buttons.Length} components)");
        }
        #endregion

        #region Public Methods - Word Pattern Rows
        /// <summary>
        /// Gets a word pattern row by index.
        /// </summary>
        public WordPatternRow GetWordPatternRow(int index)
        {
            if (index >= 0 && index < _wordPatternRows.Count)
            {
                return _wordPatternRows[index];
            }
            return null;
        }

        /// <summary>
        /// Selects a word pattern row for input.
        /// </summary>
        public void SelectWordRow(int index)
        {
            // Deselect previous
            if (_selectedWordRowIndex >= 0 && _selectedWordRowIndex < _wordPatternRows.Count)
            {
                _wordPatternRows[_selectedWordRowIndex].Deselect();
            }

            _selectedWordRowIndex = index;

            // Select new
            if (_selectedWordRowIndex >= 0 && _selectedWordRowIndex < _wordPatternRows.Count)
            {
                _wordPatternRows[_selectedWordRowIndex].Select();

                // Update autocomplete for this row's word length
                if (_autocompleteDropdown != null)
                {
                    _autocompleteDropdown.SetRequiredWordLength(_wordPatternRows[_selectedWordRowIndex].RequiredWordLength);
                    _autocompleteDropdown.ClearFilter();
                }
            }

            OnWordRowSelected?.Invoke(_selectedWordRowIndex);
        }

        /// <summary>
        /// Adds a letter to the currently selected word row.
        /// </summary>
        public bool AddLetterToSelectedRow(char letter)
        {
            if (_selectedWordRowIndex < 0 || _selectedWordRowIndex >= _wordPatternRows.Count)
            {
                return false;
            }

            var row = _wordPatternRows[_selectedWordRowIndex];
            bool added = row.AddLetter(letter);

            if (added && _autocompleteDropdown != null)
            {
                _autocompleteDropdown.UpdateFilter(row.EnteredText);
            }

            return added;
        }

        /// <summary>
        /// Removes the last letter from the currently selected word row.
        /// </summary>
        public bool RemoveLastLetterFromSelectedRow()
        {
            if (_selectedWordRowIndex < 0 || _selectedWordRowIndex >= _wordPatternRows.Count)
            {
                return false;
            }

            var row = _wordPatternRows[_selectedWordRowIndex];
            bool removed = row.RemoveLastLetter();

            if (removed && _autocompleteDropdown != null)
            {
                _autocompleteDropdown.UpdateFilter(row.EnteredText);
            }

            return removed;
        }

        /// <summary>
        /// Caches word pattern row references.
        /// </summary>
        [Button("Cache Word Pattern Rows")]
        public void CacheWordPatternRows()
        {
            _wordPatternRows.Clear();

            if (_wordPatternsContainer == null)
            {
                Debug.LogWarning("[PlayerGridPanel] Word patterns container not assigned.");
                return;
            }

            // Find all WordPatternRow components in children
            var rows = _wordPatternsContainer.GetComponentsInChildren<WordPatternRow>(true);

            foreach (var row in rows)
            {
                // Subscribe to events
                row.OnRowSelected -= HandleWordRowSelected;
                row.OnRowSelected += HandleWordRowSelected;
                row.OnCoordinateModeClicked -= HandleCoordinateModeClicked;
                row.OnCoordinateModeClicked += HandleCoordinateModeClicked;

                _wordPatternRows.Add(row);
            }

            Debug.Log($"[PlayerGridPanel] Cached {_wordPatternRows.Count} word pattern rows");
        }

        /// <summary>
        /// Checks if all words are placed.
        /// </summary>
        public bool AreAllWordsPlaced()
        {
            foreach (var row in _wordPatternRows)
            {
                if (row.gameObject.activeSelf && !row.IsPlaced)
                {
                    return false;
                }
            }
            return true;
        }
        #endregion

        #region Public Methods - Coordinate Placement Mode
        /// <summary>
        /// Enters coordinate placement mode for a specific word.
        /// </summary>
        /// <param name="wordRowIndex">Index of the word row</param>
        public void EnterPlacementMode(int wordRowIndex)
        {
            if (wordRowIndex < 0 || wordRowIndex >= _wordPatternRows.Count)
            {
                Debug.LogError($"[PlayerGridPanel] Invalid word row index: {wordRowIndex}");
                return;
            }

            var row = _wordPatternRows[wordRowIndex];
            if (!row.HasWord)
            {
                Debug.LogError("[PlayerGridPanel] Cannot enter placement mode - no word entered");
                return;
            }

            _placementWordRowIndex = wordRowIndex;
            _placementWord = row.CurrentWord;
            _placementState = PlacementState.SelectingFirstCell;
            _firstCellCol = -1;
            _firstCellRow = -1;
            _placedCellPositions.Clear();

            Debug.Log($"[PlayerGridPanel] Entered placement mode for word: {_placementWord}");
        }

        /// <summary>
        /// Cancels coordinate placement mode.
        /// </summary>
        public void CancelPlacementMode()
        {
            if (_placementState == PlacementState.Inactive) return;

            // Clear any preview highlighting
            ClearPlacementHighlighting();

            _placementState = PlacementState.Inactive;
            _placementWordRowIndex = -1;
            _placementWord = "";
            _firstCellCol = -1;
            _firstCellRow = -1;
            _placedCellPositions.Clear();

            OnPlacementCancelled?.Invoke();
            Debug.Log("[PlayerGridPanel] Placement mode cancelled");
        }

        /// <summary>
        /// Handles cell hover during placement mode - shows valid/invalid positions.
        /// </summary>
        public void UpdatePlacementPreview(int column, int row)
        {
            if (_placementState == PlacementState.Inactive) return;

            // Clear previous highlighting
            ClearPlacementHighlighting();

            if (_placementState == PlacementState.SelectingFirstCell)
            {
                // Highlight cursor position
                HighlightCellForPlacement(column, row, _cursorColor);

                // Show valid adjacent cells for next letter
                ShowValidDirections(column, row);
            }
            else if (_placementState == PlacementState.SelectingDirection)
            {
                // Keep first cell highlighted
                HighlightCellForPlacement(_firstCellCol, _firstCellRow, _cursorColor);

                // Check if this position is valid for direction
                bool isValidDirection = IsValidDirectionCell(column, row);

                if (isValidDirection)
                {
                    // Preview the full word placement
                    PreviewWordPlacement(column, row);
                }
                else
                {
                    // Highlight as invalid
                    HighlightCellForPlacement(column, row, _invalidPlacementColor);
                }
            }
        }

        /// <summary>
        /// Places word randomly on the grid.
        /// </summary>
        [Button("Random Placement (Test)")]
        public bool PlaceWordRandomly()
        {
            if (_placementState == PlacementState.Inactive)
            {
                Debug.LogWarning("[PlayerGridPanel] Not in placement mode");
                return false;
            }

            // Try random positions until we find a valid one
            List<Vector2Int> validStartPositions = GetAllValidStartPositions();

            if (validStartPositions.Count == 0)
            {
                Debug.LogWarning("[PlayerGridPanel] No valid placement positions found");
                return false;
            }

            // Shuffle and try each position
            ShuffleList(validStartPositions);

            foreach (var startPos in validStartPositions)
            {
                var validDirections = GetValidDirectionsFromCell(startPos.x, startPos.y);

                if (validDirections.Count > 0)
                {
                    // Pick random direction
                    int dirIndex = UnityEngine.Random.Range(0, validDirections.Count);
                    Vector2Int direction = validDirections[dirIndex];

                    // Place the word
                    return PlaceWordInDirection(startPos.x, startPos.y, direction.x, direction.y);
                }
            }

            Debug.LogWarning("[PlayerGridPanel] Could not find valid random placement");
            return false;
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
            float gridHeight = (_currentGridSize * CELL_SIZE) + ((_currentGridSize - 1) * CELL_SPACING);

            if (_gridContainerLayout != null)
            {
                _gridContainerLayout.preferredHeight = gridHeight;
            }

            if (_rowLabelsLayout != null)
            {
                float rowLabelsHeight = (_currentGridSize * ROW_LABEL_HEIGHT) + ((_currentGridSize - 1) * ROW_LABEL_SPACING);
                _rowLabelsLayout.preferredHeight = rowLabelsHeight;
            }

            if (_gridWithRowLabelsRect != null)
            {
                var layoutElement = _gridWithRowLabelsRect.GetComponent<LayoutElement>();
                if (layoutElement != null)
                {
                    layoutElement.preferredHeight = gridHeight;
                }
            }

            float totalHeight = _fixedElementsHeight + gridHeight;

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

            for (int i = 0; i < MAX_GRID_SIZE; i++)
            {
                if (_rowLabelObjects[i] != null)
                {
                    bool shouldBeVisible = i < _currentGridSize;
                    _rowLabelObjects[i].SetActive(shouldBeVisible);
                    if (!shouldBeVisible) hiddenRows++;
                }
            }

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

        #region Private Methods - Placement Helpers
        private void ClearPlacementHighlighting()
        {
            for (int col = 0; col < _currentGridSize; col++)
            {
                for (int row = 0; row < _currentGridSize; row++)
                {
                    if (_cells[col, row] != null)
                    {
                        var pos = new Vector2Int(col, row);
                        if (!_allPlacedPositions.Contains(pos))
                        {
                            _cells[col, row].SetState(CellState.Empty);
                            _cells[col, row].ClearHighlight();
                        }
                    }
                }
            }
        }

        private void HighlightCellForPlacement(int col, int row, Color color)
        {
            var cell = GetCell(col, row);
            if (cell != null)
            {
                cell.SetHighlightColor(color);
            }
        }

        private void ShowValidDirections(int col, int row)
        {
            int[] dCols = { -1, 0, 1, -1, 1, -1, 0, 1 };
            int[] dRows = { -1, -1, -1, 0, 0, 1, 1, 1 };

            for (int i = 0; i < 8; i++)
            {
                if (CanPlaceWordInDirection(col, row, dCols[i], dRows[i]))
                {
                    int nextCol = col + dCols[i];
                    int nextRow = row + dRows[i];
                    HighlightCellForPlacement(nextCol, nextRow, _validPlacementColor);
                }
            }
        }

        private bool CanPlaceWordInDirection(int startCol, int startRow, int dCol, int dRow)
        {
            if (_placementWord.Length == 0) return false;

            for (int i = 0; i < _placementWord.Length; i++)
            {
                int col = startCol + (i * dCol);
                int row = startRow + (i * dRow);

                if (!IsValidCoordinate(col, row))
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

        private bool IsValidDirectionCell(int col, int row)
        {
            if (_firstCellCol < 0 || _firstCellRow < 0) return false;

            int dCol = col - _firstCellCol;
            int dRow = row - _firstCellRow;

            if (Mathf.Abs(dCol) > 1 || Mathf.Abs(dRow) > 1) return false;
            if (dCol == 0 && dRow == 0) return false;

            return CanPlaceWordInDirection(_firstCellCol, _firstCellRow, dCol, dRow);
        }

        private void PreviewWordPlacement(int secondCol, int secondRow)
        {
            int dCol = secondCol - _firstCellCol;
            int dRow = secondRow - _firstCellRow;

            for (int i = 0; i < _placementWord.Length; i++)
            {
                int col = _firstCellCol + (i * dCol);
                int row = _firstCellRow + (i * dRow);

                Color highlightColor = (i == 0) ? _cursorColor : _validPlacementColor;
                HighlightCellForPlacement(col, row, highlightColor);
            }
        }

        private List<Vector2Int> GetAllValidStartPositions()
        {
            List<Vector2Int> validPositions = new List<Vector2Int>();

            for (int col = 0; col < _currentGridSize; col++)
            {
                for (int row = 0; row < _currentGridSize; row++)
                {
                    if (GetValidDirectionsFromCell(col, row).Count > 0)
                    {
                        validPositions.Add(new Vector2Int(col, row));
                    }
                }
            }

            return validPositions;
        }

        private List<Vector2Int> GetValidDirectionsFromCell(int col, int row)
        {
            List<Vector2Int> validDirections = new List<Vector2Int>();

            int[] dCols = { -1, 0, 1, -1, 1, -1, 0, 1 };
            int[] dRows = { -1, -1, -1, 0, 0, 1, 1, 1 };

            for (int i = 0; i < 8; i++)
            {
                if (CanPlaceWordInDirection(col, row, dCols[i], dRows[i]))
                {
                    validDirections.Add(new Vector2Int(dCols[i], dRows[i]));
                }
            }

            return validDirections;
        }

        private bool PlaceWordInDirection(int startCol, int startRow, int dCol, int dRow)
        {
            _placedCellPositions.Clear();

            for (int i = 0; i < _placementWord.Length; i++)
            {
                int col = startCol + (i * dCol);
                int row = startRow + (i * dRow);

                var cell = GetCell(col, row);
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

            if (_placementWordRowIndex >= 0 && _placementWordRowIndex < _wordPatternRows.Count)
            {
                _wordPatternRows[_placementWordRowIndex].MarkAsPlaced();
            }

            OnWordPlaced?.Invoke(_placementWordRowIndex, _placementWord, new List<Vector2Int>(_placedCellPositions));

            _placementState = PlacementState.Inactive;
            _placementWordRowIndex = -1;
            _placementWord = "";

            ClearPlacementHighlighting();

            Debug.Log($"[PlayerGridPanel] Word placed successfully");
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
        #endregion

        #region Private Methods - Event Handlers
        private void HandleCellClicked(int column, int row)
        {
            if (!IsValidCoordinate(column, row)) return;

            if (_placementState == PlacementState.SelectingFirstCell)
            {
                _firstCellCol = column;
                _firstCellRow = row;
                _placementState = PlacementState.SelectingDirection;
                UpdatePlacementPreview(column, row);
                Debug.Log($"[PlayerGridPanel] First cell selected: {GetColumnLetter(column)}{row + 1}");
                return;
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
                return;
            }

            Debug.Log($"[PlayerGridPanel] Cell clicked: {GetColumnLetter(column)}{row + 1}");
            OnCellClicked?.Invoke(column, row);
        }

        private void HandleCellHoverEnter(int column, int row)
        {
            if (!IsValidCoordinate(column, row)) return;

            if (_placementState != PlacementState.Inactive)
            {
                UpdatePlacementPreview(column, row);
            }

            OnCellHoverEnter?.Invoke(column, row);
        }

        private void HandleCellHoverExit(int column, int row)
        {
            if (!IsValidCoordinate(column, row)) return;
            OnCellHoverExit?.Invoke(column, row);
        }

private void HandleLetterClicked(char letter)
        {
            Debug.Log($"[PlayerGridPanel] Letter clicked: {letter}");

            if (_currentMode == PanelMode.Setup)
            {
                if (_selectedWordRowIndex >= 0)
                {
                    bool added = AddLetterToSelectedRow(letter);
                    Debug.Log($"[PlayerGridPanel] Added to row {_selectedWordRowIndex + 1}: {added}");
                }
                else
                {
                    Debug.Log("[PlayerGridPanel] No word row selected - click a word row first!");
                }
            }

            OnLetterClicked?.Invoke(letter);
        }

        private void HandleLetterHoverEnter(char letter)
        {
            OnLetterHoverEnter?.Invoke(letter);
        }

        private void HandleLetterHoverExit(char letter)
        {
            OnLetterHoverExit?.Invoke(letter);
        }

        private void HandleWordRowSelected(int rowNumber)
        {
            int index = rowNumber - 1;
            SelectWordRow(index);
        }

        private void HandleCoordinateModeClicked(int rowNumber)
        {
            int index = rowNumber - 1;
            if (index >= 0 && index < _wordPatternRows.Count)
            {
                EnterPlacementMode(index);
                OnCoordinateModeRequested?.Invoke(index);
            }
        }

        private void SubscribeToRandomPlacementButton()
        {
            if (_randomPlacementButton != null)
            {
                _randomPlacementButton.onClick.AddListener(HandleRandomPlacementClick);
            }
        }

        private void UnsubscribeFromRandomPlacementButton()
        {
            if (_randomPlacementButton != null)
            {
                _randomPlacementButton.onClick.RemoveListener(HandleRandomPlacementClick);
            }
        }

        private void HandleRandomPlacementClick()
        {
            if (_placementState != PlacementState.Inactive)
            {
                PlaceWordRandomly();
            }
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

        [Button("Test Setup Mode")]
        private void TestSetupMode()
        {
            SetMode(PanelMode.Setup);
        }

        [Button("Test Gameplay Mode")]
        private void TestGameplayMode()
        {
            SetMode(PanelMode.Gameplay);
        }
#endif
        #endregion
    }
}

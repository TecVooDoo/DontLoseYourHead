using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Sirenix.OdinInspector;
using System;
using System.Collections.Generic;
using System.Linq;

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


        #endregion

        #region Constants
        public const int MAX_GRID_SIZE = 12;
        public const int MIN_GRID_SIZE = 6;
        public const int MAX_WORD_ROWS = 4;

        // Layout constants - adjust these to match your prefab settings
        private const float MAX_CELL_SIZE = 40f;
        private const float MIN_CELL_SIZE = 25f;
        private float _currentCellSize = 40f;
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
        private Color _cursorColor = new Color(0.13f, 0.85f, 0.13f, 1f); // Stoplight green

        [SerializeField]
        private Color _validPlacementColor = new Color(0.6f, 1f, 0.6f, 0.8f); // Light mint green

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
        private LetterTrackerController _letterTrackerController;
        #endregion

        #region Private Fields - Grid Color Manager
        private GridColorManager _gridColorManager;
        #endregion

        #region Private Fields - Placement Preview
        private PlacementPreviewController _placementPreviewController;
        #endregion

        #region Private Fields - Word Pattern Row Manager
        private WordPatternRowManager _wordPatternRowManager;
        #endregion

        #region Private Fields - Word Patterns
        private List<WordPatternRow> _wordPatternRows = new List<WordPatternRow>();
        private int _selectedWordRowIndex = -1;
        private Func<string, int, bool> _wordValidator;
        #endregion

        #region Private Fields - Coordinate Placement Controller
        private CoordinatePlacementController _coordinatePlacementController;
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

        /// <summary>
        /// Fired when an invalid word is rejected. Parameters: word, required length
        /// </summary>
        public event Action<string, int> OnInvalidWordRejected;

        /// <summary>
        /// Fired when a letter is input from the letter tracker (for routing to name field)
        /// </summary>
        public event Action<char> OnLetterInput;

        /// <summary>
        /// Fired when word lengths are changed (rows may need re-placement)
        /// </summary>
        public event Action OnWordLengthsChanged;
        #endregion

        #region Properties
        public int CurrentGridSize => _currentGridSize;
        public bool IsInitialized => _isInitialized;
        public PanelMode CurrentMode => _currentMode;
        public PlacementState CurrentPlacementState => _coordinatePlacementController?.CurrentPlacementState ?? PlacementState.Inactive;
        public int SelectedWordRowIndex => _selectedWordRowIndex;
        public bool IsInPlacementMode => _coordinatePlacementController?.IsInPlacementMode ?? false;
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
            Debug.Log("[PlayerGridPanel] START() called - script is running!");

            // Initialize grid color manager with serialized colors
            _gridColorManager = new GridColorManager(
                _cursorColor,
                _validPlacementColor,
                _invalidPlacementColor,
                _placedLetterColor
            );

            // Initialize placement preview controller
            _placementPreviewController = new PlacementPreviewController(
                _gridColorManager,
                GetCell,
                IsValidCoordinate
            );

            // Initialize coordinate placement controller
            _coordinatePlacementController = new CoordinatePlacementController(
                _gridColorManager,
                GetCell,
                () => _currentGridSize
            );

            // Wire coordinate placement controller events
            _coordinatePlacementController.OnPlacementCancelled += HandleCoordinatePlacementCancelled;
            _coordinatePlacementController.OnWordPlaced += HandleCoordinatePlacementWordPlaced;

            // Initialize letter tracker controller
            _letterTrackerController = new LetterTrackerController(_letterTrackerContainer);
            _letterTrackerController.CacheLetterButtons();

            // Wire controller events to panel events
            _letterTrackerController.OnLetterClicked += HandleLetterClicked;
            _letterTrackerController.OnLetterHoverEnter += HandleLetterHoverEnter;
            _letterTrackerController.OnLetterHoverExit += HandleLetterHoverExit;

            // Initialize word pattern row manager
            _wordPatternRowManager = new WordPatternRowManager(_wordPatternsContainer, _autocompleteDropdown);
            _wordPatternRowManager.CacheWordPatternRows();

            // Wire manager events
            _wordPatternRowManager.OnWordRowSelected += HandleManagerWordRowSelected;
            _wordPatternRowManager.OnCoordinateModeRequested += HandleManagerCoordinateModeRequested;
            _wordPatternRowManager.OnDeleteClicked += HandleManagerDeleteClicked;
            _wordPatternRowManager.OnWordLengthsChanged += HandleManagerWordLengthsChanged;

            // Also cache to legacy list for backward compatibility during transition
            CacheWordPatternRows();

            // Initialize grid with events wired up
            if (!_isInitialized)
            {
                InitializeGrid(_currentGridSize);
            }
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
            if (IsInPlacementMode)
            {
                CancelPlacementMode();
            }

            UpdateModeVisuals();
        }

        private void UpdateModeVisuals()
        {

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

            // Clear placement tracking via controller
            _coordinatePlacementController?.ClearAllPlacedWords();

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
        #endregion

        #region Public Methods - Grid Size
        /// <summary>
        /// Changes the grid size and reinitializes.
        /// </summary>
        public void SetGridSize(int newSize)
        {
            if (newSize == _currentGridSize) return;

            // Before reinitializing, reset any placed words to WordEntered state
            // This keeps the word text but requires re-placement on the new grid
            foreach (var row in _wordPatternRows)
            {
                if (row != null && row.gameObject.activeSelf && row.IsPlaced)
                {
                    row.ResetToWordEntered();
                }
            }

            _currentGridSize = Mathf.Clamp(newSize, MIN_GRID_SIZE, MAX_GRID_SIZE);
            InitializeGrid(_currentGridSize);

            Debug.Log($"[PlayerGridPanel] Grid size changed to: {_currentGridSize}x{_currentGridSize}. Placed words reset for re-placement.");
        }
        #endregion

        #region Public Methods - Player Display
        /// <summary>
        /// Sets the player name displayed in the label.
        /// </summary>
        public void SetPlayerName(string name)
        {
            if (_playerNameLabel != null)
            {
                _playerNameLabel.text = name;
            }
        }

        /// <summary>
        /// Sets the player color for visual elements.
        /// </summary>
        public void SetPlayerColor(Color color)
        {
            _playerColor = color;
            UpdatePlayerColorVisuals();
        }

        private void UpdatePlayerColorVisuals()
        {
            if (_playerNameLabel != null)
            {
                var parentTransform = _playerNameLabel.transform.parent;
                if (parentTransform != null)
                {
                    var bgImage = parentTransform.GetComponent<Image>();
                    if (bgImage != null)
                    {
                        bgImage.color = _playerColor;
                    }
                }
            }
        }
        #endregion

        #region Public Methods - Word Lengths
        /// <summary>
        /// Sets the required word lengths for each row.
        /// </summary>
        public void SetWordLengths(int[] lengths)
        {
            if (lengths == null || lengths.Length == 0)
            {
                Debug.LogWarning("[PlayerGridPanel] SetWordLengths called with null or empty array");
                return;
            }

            // Show/hide rows based on array length
            for (int i = 0; i < _wordPatternRows.Count; i++)
            {
                if (i < lengths.Length)
                {
                    _wordPatternRows[i].gameObject.SetActive(true);
                    _wordPatternRows[i].SetRequiredLength(lengths[i]);

                    // If row was placed, reset to WordEntered since grid will be cleared
                    // This keeps the word text but requires re-placement
                    if (_wordPatternRows[i].IsPlaced)
                    {
                        _wordPatternRows[i].ResetToWordEntered();
                    }
                }
                else
                {
                    _wordPatternRows[i].gameObject.SetActive(false);
                }
            }

            // Clear all placed words from grid
            ClearAllPlacedWords();

            Debug.Log($"[PlayerGridPanel] Set word lengths: {string.Join(", ", lengths)}. All placements reset.");
            // Notify listeners that word lengths changed (for Start button state update)
            OnWordLengthsChanged?.Invoke();
        }

        /// <summary>
        /// Clears all placed words from the grid but keeps word entries.
        /// </summary>
        public void ClearAllPlacedWords()
        {
            _coordinatePlacementController?.ClearAllPlacedWords();
            Debug.Log("[PlayerGridPanel] Cleared all placed words");
        }
        #endregion

        #region Public Methods - Letter Tracker
        /// <summary>
        /// Gets a letter button by its letter.
        /// </summary>
        public LetterButton GetLetterButton(char letter)
        {
            if (_letterTrackerController == null) return null;
            return _letterTrackerController.GetLetterButton(letter);
        }

        /// <summary>
        /// Sets the state of a letter button.
        /// </summary>
        public void SetLetterState(char letter, LetterButton.LetterState state)
        {
            if (_letterTrackerController == null) return;
            _letterTrackerController.SetLetterState(letter, state);
        }

        /// <summary>
        /// Gets the current state of a letter button.
        /// </summary>
        public LetterButton.LetterState GetLetterState(char letter)
        {
            if (_letterTrackerController == null) return LetterButton.LetterState.Normal;
            return _letterTrackerController.GetLetterState(letter);
        }

        /// <summary>
        /// Resets all letter buttons to normal state.
        /// </summary>
        public void ResetAllLetterButtons()
        {
            if (_letterTrackerController == null) return;
            _letterTrackerController.ResetAllLetterButtons();
        }

        /// <summary>
        /// Sets whether letter buttons are interactable.
        /// </summary>
        public void SetLetterButtonsInteractable(bool interactable)
        {
            if (_letterTrackerController == null) return;
            _letterTrackerController.SetLetterButtonsInteractable(interactable);
        }

        /// <summary>
        /// Caches letter button references from the letter tracker container.
        /// </summary>
        [Button("Cache Letter Buttons")]
        public void CacheLetterButtons()
        {
            if (_letterTrackerController == null)
            {
                _letterTrackerController = new LetterTrackerController(_letterTrackerContainer);
                _letterTrackerController.OnLetterClicked += HandleLetterClicked;
                _letterTrackerController.OnLetterHoverEnter += HandleLetterHoverEnter;
                _letterTrackerController.OnLetterHoverExit += HandleLetterHoverExit;
            }
            _letterTrackerController.CacheLetterButtons();
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
        /// Returns all WordPatternRow components for event subscription.
        /// Used by SetupSettingsPanel to subscribe to OnInvalidWordRejected events.
        /// </summary>
        public WordPatternRow[] GetWordPatternRows()
        {
            // Auto-cache if list is empty but container exists
            if ((_wordPatternRows == null || _wordPatternRows.Count == 0) && _wordPatternsContainer != null)
            {
                Debug.Log("[PlayerGridPanel] GetWordPatternRows: Auto-caching word pattern rows");
                CacheWordPatternRows();
            }

            if (_wordPatternRows == null || _wordPatternRows.Count == 0)
            {
                Debug.LogWarning("[PlayerGridPanel] GetWordPatternRows: No word pattern rows found");
                return new WordPatternRow[0];
            }

            return _wordPatternRows.ToArray();
        }


        #region Structs
        /// <summary>
        /// Data structure for word placement information.
        /// Used to transfer placement data to gameplay panels.
        /// </summary>
        public struct WordPlacement
        {
            public string word;
            public int startCol;
            public int startRow;
            public int dirCol;
            public int dirRow;
            public int rowIndex;
        }
        #endregion

        /// <summary>
        /// Gets all placed words with their positions and directions.
        /// Used by GameplayUIController to transfer setup data to gameplay panels.
        /// </summary>
        public List<WordPlacement> GetAllWordPlacements()
        {
            var placements = new List<WordPlacement>();

            if (_coordinatePlacementController == null) return placements;

            foreach (var kvp in _coordinatePlacementController.WordRowPositions)
            {
                int rowIndex = kvp.Key;
                List<Vector2Int> positions = kvp.Value;

                if (positions == null || positions.Count < 2) continue;

                // Get the word from the word pattern row
                string word = "";
                if (rowIndex >= 0 && rowIndex < _wordPatternRows.Count)
                {
                    var wordRow = _wordPatternRows[rowIndex];
                    if (wordRow != null)
                    {
                        word = wordRow.CurrentWord;
                    }
                }

                if (string.IsNullOrEmpty(word)) continue;

                // Calculate direction from first two positions
                Vector2Int first = positions[0];
                Vector2Int second = positions[1];
                int dirCol = second.x - first.x;
                int dirRow = second.y - first.y;

                placements.Add(new WordPlacement
                {
                    word = word,
                    startCol = first.x,
                    startRow = first.y,
                    dirCol = dirCol,
                    dirRow = dirRow,
                    rowIndex = rowIndex
                });

                Debug.Log(string.Format("[PlayerGridPanel] GetAllWordPlacements: {0} at ({1},{2}) dir({3},{4})", word, first.x, first.y, dirCol, dirRow));
            }

            return placements;
        }


        /// <summary>
        /// Selects a word pattern row for input.
        /// </summary>
        public void SelectWordRow(int index)
        {
            // If we're in placement mode and selecting a DIFFERENT row, cancel placement
            int currentPlacementRow = _coordinatePlacementController?.PlacementWordRowIndex ?? -1;
            if (IsInPlacementMode && index != currentPlacementRow)
            {
                CancelPlacementMode();
            }

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
        /// Clears a placed word from the grid and resets the row for re-entry.
        /// Only clears the specific word's cells, preserving other words.
        /// </summary>
        /// <param name="rowIndex">The index of the word row to clear</param>
        /// <returns>True if the word was cleared successfully</returns>
        public bool ClearPlacedWord(int rowIndex)
        {
            if (rowIndex < 0 || rowIndex >= _wordPatternRows.Count)
            {
                return false;
            }

            var row = _wordPatternRows[rowIndex];

            // Only clear if the row has a placed word
            if (!row.IsPlaced)
            {
                Debug.Log(string.Format("[PlayerGridPanel] Row {0} is not placed, nothing to clear from grid", rowIndex + 1));
                return false;
            }

            // Delegate grid clearing to the controller
            _coordinatePlacementController?.ClearWordFromGrid(rowIndex);

            // Reset the target row to empty (for re-entry)
            row.ResetToEmpty();

            return true;
        }

        /// <summary>
        /// Clears the currently selected word row's placed word.
        /// </summary>
        public bool ClearSelectedPlacedWord()
        {
            if (_selectedWordRowIndex < 0) return false;
            return ClearPlacedWord(_selectedWordRowIndex);
        }

        /// <summary>
        /// Sets the word validator for all word rows.
        /// Validator function receives (word, requiredLength) and returns true if valid.
        /// </summary>
        public void SetWordValidator(Func<string, int, bool> validator)
        {
            _wordValidator = validator;

            // Apply to all existing word pattern rows
            foreach (var row in _wordPatternRows)
            {
                if (row != null)
                {
                    row.SetWordValidator(validator);
                }
            }

            Debug.Log("[PlayerGridPanel] Word validator set");
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

            // Sort by sibling index to ensure correct visual order (top to bottom)
            var sortedRows = rows.OrderBy(r => r.transform.GetSiblingIndex()).ToArray();

            Debug.Log($"[PlayerGridPanel] CacheWordPatternRows: Found {rows.Length} rows, sorting by sibling index");

            foreach (var row in sortedRows)
            {
                // Subscribe to events
                row.OnRowSelected -= HandleWordRowSelected;
                row.OnRowSelected += HandleWordRowSelected;
                row.OnCoordinateModeClicked -= HandleCoordinateModeClicked;
                row.OnCoordinateModeClicked += HandleCoordinateModeClicked;
                row.OnDeleteClicked -= HandleDeleteClicked;
                row.OnDeleteClicked += HandleDeleteClicked;

                // Apply word validator if set
                if (_wordValidator != null)
                {
                    row.SetWordValidator(_wordValidator);
                }

                _wordPatternRows.Add(row);
                Debug.Log($"[PlayerGridPanel] Cached row: sibling={row.transform.GetSiblingIndex()}, name={row.gameObject.name}");
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
                Debug.LogError(string.Format("[PlayerGridPanel] Invalid word row index: {0}", wordRowIndex));
                return;
            }

            var row = _wordPatternRows[wordRowIndex];
            if (!row.HasWord)
            {
                Debug.LogError("[PlayerGridPanel] Cannot enter placement mode - no word entered");
                return;
            }

            // Delegate to controller
            _coordinatePlacementController?.EnterPlacementMode(wordRowIndex, row.CurrentWord);
        }

        /// <summary>
        /// Cancels coordinate placement mode.
        /// </summary>
        public void CancelPlacementMode()
        {
            _coordinatePlacementController?.CancelPlacementMode();
        }

        /// <summary>
        /// Attempts to place the current word randomly on the grid.
        /// </summary>
        public bool PlaceWordRandomly()
        {
            return _coordinatePlacementController?.PlaceWordRandomly() ?? false;
        }

        /// <summary>
        /// Places all unplaced words randomly on the grid.
        /// Called by SetupSettingsPanel when "Place Random Positions" button is clicked.
        /// </summary>
        public void PlaceAllWordsRandomly()
        {
            HandleRandomPlacementClick();
        }

        #endregion

        #region Public Methods - Grid Cells
        /// <summary>
        /// Gets a cell by column and row.
        /// </summary>
        public GridCellUI GetCell(int column, int row)
        {
            if (column >= 0 && column < MAX_GRID_SIZE && row >= 0 && row < MAX_GRID_SIZE)
            {
                return _cells[column, row];
            }
            return null;
        }

        /// <summary>
        /// Checks if a coordinate is within the current grid bounds.
        /// </summary>
        public bool IsValidCoordinate(int column, int row)
        {
            return column >= 0 && column < _currentGridSize && row >= 0 && row < _currentGridSize;
        }

        /// <summary>
        /// Converts a column index to a letter (A-L).
        /// </summary>
        public char GetColumnLetter(int column)
        {
            return (char)('A' + column);
        }

        /// <summary>
        /// Clears all cells in the grid.
        /// </summary>
        public void ClearGrid()
        {
            // Destroy existing cell GameObjects
            if (_gridContainer != null)
            {
                for (int i = _gridContainer.childCount - 1; i >= 0; i--)
                {
                    if (Application.isPlaying)
                    {
                        Destroy(_gridContainer.GetChild(i).gameObject);
                    }
                    else
                    {
                        DestroyImmediate(_gridContainer.GetChild(i).gameObject);
                    }
                }
            }

            // Clear the cell array
            for (int col = 0; col < MAX_GRID_SIZE; col++)
            {
                for (int row = 0; row < MAX_GRID_SIZE; row++)
                {
                    _cells[col, row] = null;
                }
            }

            _isInitialized = false;
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

            // Cache row labels (1-12) by NAME, not sibling order
            if (_rowLabelsContainer != null)
            {
                for (int i = 0; i < _rowLabelsContainer.childCount; i++)
                {
                    var child = _rowLabelsContainer.GetChild(i);
                    string childName = child.name;

                    // Try to extract the number from the name (e.g., "Label_1" -> 1, "1" -> 1)
                    for (int labelNum = 1; labelNum <= MAX_GRID_SIZE; labelNum++)
                    {
                        if (childName.Contains(labelNum.ToString()) &&
                            (childName.Contains("Label") || childName.Contains("Row") || childName == labelNum.ToString()))
                        {
                            // Avoid false matches like "Label_12" matching "1"
                            string numStr = labelNum.ToString();
                            bool exactMatch = childName.EndsWith(numStr) ||
                                              childName.EndsWith("_" + numStr) ||
                                              childName == numStr;

                            // For two-digit numbers, also check they're not part of a larger number
                            if (labelNum < 10)
                            {
                                // Single digit - make sure it's not part of 10, 11, 12
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
                Debug.Log($"[PlayerGridPanel] Cached {rowCount} row labels by name");

                // Log what we found for debugging
                for (int i = 0; i < MAX_GRID_SIZE; i++)
                {
                    if (_rowLabelObjects[i] != null)
                    {
                        Debug.Log($"[PlayerGridPanel] Row label [{i}] = {_rowLabelObjects[i].name}");
                    }
                }
            }

            // Cache column labels (A-L) by NAME, not sibling order
            if (_columnLabelsContainer != null)
            {
                for (int i = 0; i < _columnLabelsContainer.childCount; i++)
                {
                    var child = _columnLabelsContainer.GetChild(i);
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
                Debug.Log($"[PlayerGridPanel] Cached {colCount} column labels by name");

                // Log what we found for debugging
                for (int i = 0; i < MAX_GRID_SIZE; i++)
                {
                    if (_columnLabelObjects[i] != null)
                    {
                        Debug.Log($"[PlayerGridPanel] Column label [{i}] = {_columnLabelObjects[i].name}");
                    }
                }
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
            // In Gameplay mode, use prefab sizing - don't override
            if (_currentMode == PanelMode.Gameplay)
            {
                Debug.Log("[PlayerGridPanel] Gameplay mode - using prefab layout, skipping dynamic sizing");
                return;
            }

            // With horizontal layout, we have full panel height available
            float availableHeight = 1080f;

            // Fixed elements: header(~40) + word patterns(~120) + letter tracker(~80) + column labels(~30) + padding(~30)
            float fixedElementsHeight = 300f;
            float maxGridHeight = availableHeight - fixedElementsHeight;

            // Calculate optimal cell size
            float optimalCellSize = (maxGridHeight - ((_currentGridSize - 1) * CELL_SPACING)) / _currentGridSize;

            // Clamp between min and max cell sizes
            float maxCellSize = 65f;
            float minCellSize = 40f;
            _currentCellSize = Mathf.Clamp(optimalCellSize, minCellSize, maxCellSize);

            Debug.Log($"[PlayerGridPanel] Grid {_currentGridSize}x{_currentGridSize}: optimal={optimalCellSize:F1}px, clamped={_currentCellSize:F1}px");

            // Update GridLayoutGroup cell size
            if (_gridLayoutGroup != null)
            {
                _gridLayoutGroup.cellSize = new Vector2(_currentCellSize, _currentCellSize);
                _gridLayoutGroup.spacing = new Vector2(CELL_SPACING, CELL_SPACING);
            }

            float gridHeight = (_currentGridSize * _currentCellSize) + ((_currentGridSize - 1) * CELL_SPACING);
            float gridWidth = gridHeight;

            // Set grid container size
            if (_gridContainerLayout != null)
            {
                _gridContainerLayout.preferredHeight = gridHeight;
                _gridContainerLayout.preferredWidth = gridWidth;
                _gridContainerLayout.minHeight = gridHeight;
                _gridContainerLayout.minWidth = gridWidth;
            }

            // Row labels should be square (same size as cells)
            float rowLabelSize = _currentCellSize;

            // Update row labels container and individual labels
            if (_rowLabelsContainer != null)
            {
                // Set width on row labels container to match cell size
                var rowLabelsContainerLayout = _rowLabelsContainer.GetComponent<LayoutElement>();
                if (rowLabelsContainerLayout == null)
                {
                    rowLabelsContainerLayout = _rowLabelsContainer.gameObject.AddComponent<LayoutElement>();
                }
                rowLabelsContainerLayout.preferredWidth = rowLabelSize;
                rowLabelsContainerLayout.minWidth = rowLabelSize;
                rowLabelsContainerLayout.flexibleWidth = 0;

                // Configure VerticalLayoutGroup to NOT stretch children
                var rowLayoutGroup = _rowLabelsContainer.GetComponent<VerticalLayoutGroup>();
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
                        var labelLayout = _rowLabelObjects[i].GetComponent<LayoutElement>();
                        if (labelLayout == null)
                        {
                            labelLayout = _rowLabelObjects[i].AddComponent<LayoutElement>();
                        }
                        labelLayout.preferredHeight = rowLabelSize;
                        labelLayout.minHeight = rowLabelSize;
                        labelLayout.preferredWidth = rowLabelSize;
                        labelLayout.minWidth = rowLabelSize;
                        labelLayout.flexibleHeight = 0;
                        labelLayout.flexibleWidth = 0;

                        // Also set the RectTransform directly
                        var labelRect = _rowLabelObjects[i].GetComponent<RectTransform>();
                        if (labelRect != null)
                        {
                            labelRect.sizeDelta = new Vector2(rowLabelSize, rowLabelSize);
                        }
                    }
                }
            }

            // Set row labels container height
            if (_rowLabelsLayout != null)
            {
                _rowLabelsLayout.preferredHeight = gridHeight;
                _rowLabelsLayout.minHeight = gridHeight;
                _rowLabelsLayout.flexibleHeight = 0;
            }

            // Column label height (same as cell size for consistency)
            float columnLabelHeight = _currentCellSize;

            // Update column labels container and spacer
            if (_columnLabelsContainer != null)
            {
                // Set fixed height on column labels container
                var colContainerLayout = _columnLabelsContainer.GetComponent<LayoutElement>();
                if (colContainerLayout == null)
                {
                    colContainerLayout = _columnLabelsContainer.gameObject.AddComponent<LayoutElement>();
                }
                colContainerLayout.preferredHeight = columnLabelHeight;
                colContainerLayout.minHeight = columnLabelHeight;
                colContainerLayout.flexibleHeight = 0;

                // Configure HorizontalLayoutGroup to NOT stretch children
                var colLayoutGroup = _columnLabelsContainer.GetComponent<HorizontalLayoutGroup>();
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
                for (int i = 0; i < _columnLabelsContainer.childCount; i++)
                {
                    var child = _columnLabelsContainer.GetChild(i);
                    if (child.name.ToLower().Contains("spacer"))
                    {
                        var spacerLayout = child.GetComponent<LayoutElement>();
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
                        var spacerRect = child.GetComponent<RectTransform>();
                        if (spacerRect != null)
                        {
                            spacerRect.sizeDelta = new Vector2(rowLabelSize, columnLabelHeight);
                        }

                        Debug.Log($"[PlayerGridPanel] Set spacer size to {rowLabelSize}x{columnLabelHeight}px");
                        break;
                    }
                }

                // Set each column label to exact cell size (square)
                for (int i = 0; i < MAX_GRID_SIZE; i++)
                {
                    if (_columnLabelObjects[i] != null)
                    {
                        var labelLayout = _columnLabelObjects[i].GetComponent<LayoutElement>();
                        if (labelLayout == null)
                        {
                            labelLayout = _columnLabelObjects[i].AddComponent<LayoutElement>();
                        }
                        labelLayout.preferredWidth = _currentCellSize;
                        labelLayout.minWidth = _currentCellSize;
                        labelLayout.preferredHeight = columnLabelHeight;
                        labelLayout.minHeight = columnLabelHeight;
                        labelLayout.flexibleWidth = 0;
                        labelLayout.flexibleHeight = 0;

                        // Also set RectTransform directly
                        var labelRect = _columnLabelObjects[i].GetComponent<RectTransform>();
                        if (labelRect != null)
                        {
                            labelRect.sizeDelta = new Vector2(_currentCellSize, columnLabelHeight);
                        }
                    }
                }
            }

            // Update GridWithRowLabels container
            if (_gridWithRowLabelsRect != null)
            {
                var layoutElement = _gridWithRowLabelsRect.GetComponent<LayoutElement>();
                if (layoutElement != null)
                {
                    layoutElement.preferredHeight = gridHeight;
                    layoutElement.minHeight = gridHeight;
                }
            }

            // Force layout rebuild
            Canvas.ForceUpdateCanvases();
            if (_panelRectTransform != null)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(_panelRectTransform);
            }

            Debug.Log($"[PlayerGridPanel] Cell size: {_currentCellSize:F1}px, Grid: {gridHeight:F0}px, Labels: {rowLabelSize:F1}px");
        }



        private void UpdateLabelVisibility()
        {
            Debug.Log($"[PlayerGridPanel] === UpdateLabelVisibility START for {_currentGridSize}x{_currentGridSize} ===");

            // Count how many labels we have cached
            int rowLabelsCached = 0;
            int colLabelsCached = 0;
            for (int i = 0; i < MAX_GRID_SIZE; i++)
            {
                if (_rowLabelObjects[i] != null) rowLabelsCached++;
                if (_columnLabelObjects[i] != null) colLabelsCached++;
            }
            Debug.Log($"[PlayerGridPanel] Cached labels: {rowLabelsCached} rows, {colLabelsCached} columns");

            // Step 1: Enable all row labels and log their state
            for (int i = 0; i < MAX_GRID_SIZE; i++)
            {
                if (_rowLabelObjects[i] != null)
                {
                    bool wasBefore = _rowLabelObjects[i].activeSelf;
                    _rowLabelObjects[i].SetActive(true);
                    bool isAfter = _rowLabelObjects[i].activeSelf;
                    if (i < 6) // Only log first 6 to reduce spam
                    {
                        Debug.Log($"[PlayerGridPanel] Row label {i + 1}: was={wasBefore}, now={isAfter}, name={_rowLabelObjects[i].name}");
                    }
                }
                else
                {
                    if (i < 6)
                    {
                        Debug.LogWarning($"[PlayerGridPanel] Row label {i + 1}: NULL!");
                    }
                }
            }

            // Step 2: Enable all column labels and log their state
            for (int i = 0; i < MAX_GRID_SIZE; i++)
            {
                if (_columnLabelObjects[i] != null)
                {
                    bool wasBefore = _columnLabelObjects[i].activeSelf;
                    _columnLabelObjects[i].SetActive(true);
                    bool isAfter = _columnLabelObjects[i].activeSelf;
                    if (i < 6) // Only log first 6 to reduce spam
                    {
                        Debug.Log($"[PlayerGridPanel] Col label {(char)('A' + i)}: was={wasBefore}, now={isAfter}, name={_columnLabelObjects[i].name}");
                    }
                }
                else
                {
                    if (i < 6)
                    {
                        Debug.LogWarning($"[PlayerGridPanel] Col label {(char)('A' + i)}: NULL!");
                    }
                }
            }

            // Step 3: Force layout rebuild while all are active
            Canvas.ForceUpdateCanvases();

            if (_rowLabelsContainer != null)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(_rowLabelsContainer as RectTransform);
            }
            if (_columnLabelsContainer != null)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(_columnLabelsContainer as RectTransform);
            }

            // Step 4: Now hide the labels beyond our grid size
            for (int i = _currentGridSize; i < MAX_GRID_SIZE; i++)
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

            // Step 5: Final layout rebuild
            Canvas.ForceUpdateCanvases();

            if (_rowLabelsContainer != null)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(_rowLabelsContainer as RectTransform);
            }
            if (_columnLabelsContainer != null)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(_columnLabelsContainer as RectTransform);
            }

            Debug.Log($"[PlayerGridPanel] === UpdateLabelVisibility END ===");
        }

        private void CreateCellsForCurrentSize()
        {
            // Create cells in row-major order (row 0, then row 1, etc.)
            // This matches how GridLayoutGroup lays out children
            for (int row = 0; row < _currentGridSize; row++)
            {
                for (int col = 0; col < _currentGridSize; col++)
                {
                    CreateCell(col, row);
                }
            }

            Debug.Log($"[PlayerGridPanel] Created {_currentGridSize * _currentGridSize} cells for {_currentGridSize}x{_currentGridSize} grid");
        }

        private void CreateCell(int column, int row)
        {
            if (_cellPrefab == null || _gridContainer == null) return;

            GameObject cellGO = Instantiate(_cellPrefab.gameObject, _gridContainer);
            cellGO.name = $"Cell_{GetColumnLetter(column)}{row + 1}";

            GridCellUI cell = cellGO.GetComponent<GridCellUI>();
            if (cell != null)
            {
                cell.Initialize(column, row);

                // Subscribe to cell events
                cell.OnCellClicked += HandleCellClicked;
                cell.OnCellHoverEnter += HandleCellHoverEnter;
                cell.OnCellHoverExit += HandleCellHoverExit;

                _cells[column, row] = cell;
            }
        }
        #endregion

        #region Private Methods - Event Handlers
        private void HandleCellClicked(int column, int row)
        {
            if (!IsValidCoordinate(column, row)) return;

            // Let the controller handle placement clicks
            if (_coordinatePlacementController != null && _coordinatePlacementController.HandleCellClick(column, row))
            {
                return; // Controller handled the click
            }

            Debug.Log(string.Format("[PlayerGridPanel] Cell clicked: {0}{1}", GetColumnLetter(column), row + 1));
            OnCellClicked?.Invoke(column, row);
        }

        private void HandleCellHoverEnter(int column, int row)
        {
            if (!IsValidCoordinate(column, row)) return;

            // Let the controller handle placement preview
            if (IsInPlacementMode)
            {
                _coordinatePlacementController?.UpdatePlacementPreview(column, row);
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
            // Fire event for SetupSettingsPanel to route to name input if focused
            OnLetterInput?.Invoke(letter);

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

        private void HandleDeleteClicked(int rowNumber, bool wasPlaced)
        {
            int index = rowNumber - 1;
            Debug.Log($"[PlayerGridPanel] Delete clicked on row {rowNumber}, wasPlaced: {wasPlaced}");

            if (wasPlaced && index >= 0 && index < _wordPatternRows.Count)
            {
                // Clear this word from the grid
                // Note: The WordPatternRow already called ClearWord() on itself,
                // but we need to clear the grid cells too
                ClearWordFromGrid(index);
            }

            // Select this row for new input
            SelectWordRow(index);
        }

        // Manager event handlers (from WordPatternRowManager)
        private void HandleManagerWordRowSelected(int index)
        {
            _selectedWordRowIndex = index;
            OnWordRowSelected?.Invoke(index);
        }

        private void HandleManagerCoordinateModeRequested(int index)
        {
            if (index >= 0 && index < _wordPatternRows.Count)
            {
                EnterPlacementMode(index);
                OnCoordinateModeRequested?.Invoke(index);
            }
        }

        private void HandleManagerDeleteClicked(int index, bool wasPlaced)
        {
            Debug.Log($"[PlayerGridPanel] Manager delete on row {index + 1}, wasPlaced: {wasPlaced}");
            if (wasPlaced && index >= 0 && index < _wordPatternRows.Count)
            {
                ClearWordFromGrid(index);
            }
            _selectedWordRowIndex = index;
        }

        private void HandleManagerWordLengthsChanged()
        {
            OnWordLengthsChanged?.Invoke();
        }

        // Controller event handlers (from CoordinatePlacementController)
        private void HandleCoordinatePlacementCancelled()
        {
            Debug.Log("[PlayerGridPanel] Placement cancelled by controller");
            OnPlacementCancelled?.Invoke();
        }

        private void HandleCoordinatePlacementWordPlaced(int rowIndex, string word, List<Vector2Int> positions)
        {
            Debug.Log(string.Format("[PlayerGridPanel] Word placed by controller: {0} at row {1}", word, rowIndex + 1));

            // Update the word pattern row state
            if (rowIndex >= 0 && rowIndex < _wordPatternRows.Count)
            {
                _wordPatternRows[rowIndex].MarkAsPlaced();
            }

            OnWordPlaced?.Invoke(rowIndex, word, positions);
        }

        /// <summary>
        /// Clears a word's cells from the grid without resetting the row.
        /// Called when delete button is pressed on a placed word.
        /// </summary>
        private void ClearWordFromGrid(int rowIndex)
        {
            if (_coordinatePlacementController == null)
            {
                Debug.LogWarning("[PlayerGridPanel] Controller not initialized");
                return;
            }

            // Get positions from controller before clearing
            List<Vector2Int> positions = _coordinatePlacementController.GetPositionsForRow(rowIndex);

            if (positions != null && positions.Count > 0)
            {
                foreach (Vector2Int pos in positions)
                {
                    GridCellUI cell = GetCell(pos.x, pos.y);
                    if (cell != null)
                    {
                        // Check if another word shares this cell
                        bool sharedCell = false;
                        IReadOnlyDictionary<int, List<Vector2Int>> wordRowPositions = _coordinatePlacementController.WordRowPositions;

                        foreach (KeyValuePair<int, List<Vector2Int>> kvp in wordRowPositions)
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
                        }
                    }
                }

                // Let controller handle its internal state cleanup
                _coordinatePlacementController.ClearWordFromGrid(rowIndex);
                Debug.Log(string.Format("[PlayerGridPanel] Cleared grid cells for row {0}", rowIndex + 1));
            }
            else
            {
                Debug.LogWarning(string.Format("[PlayerGridPanel] No position tracking for row {0}", rowIndex + 1));
            }
        }

        private void HandleRandomPlacementClick()
        {
            Debug.Log("[PlayerGridPanel] === RANDOM PLACEMENT BUTTON CLICKED ===");

            // Build list of rows that need placement, sorted by word length (longest first)
            // This prevents shorter words from blocking longer words on smaller grids
            var rowsToPlace = new List<int>();

            for (int i = 0; i < _wordPatternRows.Count; i++)
            {
                var row = _wordPatternRows[i];
                if (row == null || !row.gameObject.activeSelf) continue;
                if (row.IsPlaced || !row.HasWord) continue;

                rowsToPlace.Add(i);
            }

            // Sort by word length descending (longest words first)
            rowsToPlace.Sort((a, b) =>
            {
                int lengthA = _wordPatternRows[a].CurrentWord?.Length ?? 0;
                int lengthB = _wordPatternRows[b].CurrentWord?.Length ?? 0;
                return lengthB.CompareTo(lengthA); // Descending order
            });

            Debug.Log(string.Format("[PlayerGridPanel] Placement order (longest first): {0}", string.Join(", ", rowsToPlace.Select(i => string.Format("Row{0}({1})", i + 1, _wordPatternRows[i].CurrentWord)))));

            // Place words in sorted order
            int placedCount = 0;

            foreach (int i in rowsToPlace)
            {
                WordPatternRow row = _wordPatternRows[i];

                Debug.Log(string.Format("[PlayerGridPanel] Placing Row {0}: Word='{1}' (length {2})", i + 1, row.CurrentWord, row.CurrentWord?.Length ?? 0));

                // Enter placement mode for this row and place randomly
                EnterPlacementMode(i);

                if (IsInPlacementMode)
                {
                    bool success = PlaceWordRandomly();
                    if (success)
                    {
                        placedCount++;
                        Debug.Log(string.Format("[PlayerGridPanel] Randomly placed word {0}: {1}", i + 1, row.CurrentWord));
                    }
                    else
                    {
                        // Failed to place - cancel and continue to next word
                        CancelPlacementMode();
                        Debug.LogWarning(string.Format("[PlayerGridPanel] Could not find valid placement for word {0}: {1}", i + 1, row.CurrentWord));
                    }
                }
            }

            Debug.Log(string.Format("[PlayerGridPanel] Random placement complete. Placed {0}/{1} word(s).", placedCount, rowsToPlace.Count));
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
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
        private Dictionary<char, LetterButton> _letterButtons = new Dictionary<char, LetterButton>();
        private bool _letterButtonsCached;
        #endregion

        #region Private Fields - Word Patterns
        private List<WordPatternRow> _wordPatternRows = new List<WordPatternRow>();
        private int _selectedWordRowIndex = -1;
        private Func<string, int, bool> _wordValidator;
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

        // Track which cells belong to which word row for proper deletion
        private Dictionary<int, List<Vector2Int>> _wordRowPositions = new Dictionary<int, List<Vector2Int>>();
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
            Debug.Log("[PlayerGridPanel] START() called - script is running!");
            
            CacheLetterButtons();
            CacheWordPatternRows();

            // Initialize grid with events wired up
            if (!_isInitialized)
            {
                InitializeGrid(_currentGridSize);
            }
            
            // Double-check button reference
            if (_randomPlacementButton != null)
            {
                Debug.Log($"[PlayerGridPanel] Random placement button reference is VALID: {_randomPlacementButton.gameObject.name}");
            }
            else
            {
                Debug.LogError("[PlayerGridPanel] Random placement button reference is NULL in Start()!");
            }
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
            _wordRowPositions.Clear();

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
                }
                else
                {
                    _wordPatternRows[i].gameObject.SetActive(false);
                }
            }

            // Clear all placed words from grid
            ClearAllPlacedWords();

            Debug.Log($"[PlayerGridPanel] Set word lengths: {string.Join(", ", lengths)}");
        }

        /// <summary>
        /// Clears all placed words from the grid but keeps word entries.
        /// </summary>
        public void ClearAllPlacedWords()
        {
            // Clear all cells
            for (int col = 0; col < _currentGridSize; col++)
            {
                for (int row = 0; row < _currentGridSize; row++)
                {
                    var cell = GetCell(col, row);
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

            Debug.Log("[PlayerGridPanel] Cleared all placed words");
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
        /// Returns all WordPatternRow components for event subscription.
        /// Used by SetupSettingsPanel to subscribe to OnInvalidWordRejected events.
        /// </summary>
        public WordPatternRow[] GetWordPatternRows()
        {
            if (_wordPatternRows == null || _wordPatternRows.Count == 0)
            {
                return new WordPatternRow[0];
            }

            return _wordPatternRows.ToArray();
        }

        /// <summary>
        /// Selects a word pattern row for input.
        /// </summary>
        public void SelectWordRow(int index)
        {
            // If we're in placement mode and selecting a DIFFERENT row, cancel placement
            if (_placementState != PlacementState.Inactive && index != _placementWordRowIndex)
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
                Debug.Log($"[PlayerGridPanel] Row {rowIndex + 1} is not placed, nothing to clear from grid");
                return false;
            }

            // Check if we have position tracking for this word
            if (_wordRowPositions.TryGetValue(rowIndex, out List<Vector2Int> positions))
            {
                // Clear only this word's cells
                foreach (var pos in positions)
                {
                    var cell = GetCell(pos.x, pos.y);
                    if (cell != null)
                    {
                        // Check if another word shares this cell
                        bool sharedCell = false;
                        char sharedLetter = '\0';

                        foreach (var kvp in _wordRowPositions)
                        {
                            if (kvp.Key != rowIndex && kvp.Value.Contains(pos))
                            {
                                sharedCell = true;
                                // Get the letter from the other word
                                if (_placedLetters.TryGetValue(pos, out char letter))
                                {
                                    sharedLetter = letter;
                                }
                                break;
                            }
                        }

                        if (!sharedCell)
                        {
                            // No other word uses this cell - clear it
                            cell.SetState(CellState.Empty);
                            cell.ClearLetter();
                            cell.ClearHighlight();
                            _allPlacedPositions.Remove(pos);
                            _placedLetters.Remove(pos);
                        }
                        // If shared, leave the cell as is (other word still uses it)
                    }
                }

                // Remove this word's position tracking
                _wordRowPositions.Remove(rowIndex);
                Debug.Log($"[PlayerGridPanel] Cleared word from row {rowIndex + 1} - preserved other words");
            }
            else
            {
                // Fallback: No position tracking - clear all and reset other rows
                Debug.LogWarning($"[PlayerGridPanel] No position tracking for row {rowIndex + 1}, using fallback clear");

                // Reset the grid completely
                for (int col = 0; col < _currentGridSize; col++)
                {
                    for (int r = 0; r < _currentGridSize; r++)
                    {
                        var cell = GetCell(col, r);
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

                // Reset other placed rows to WordEntered (need re-placement)
                foreach (var otherRow in _wordPatternRows)
                {
                    if (otherRow != row && otherRow.IsPlaced)
                    {
                        otherRow.ResetToWordEntered();
                    }
                }
            }

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

            foreach (var row in rows)
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

            // Clear the first cell's temporary letter if one was placed
            if (_firstCellCol >= 0 && _firstCellRow >= 0)
            {
                var firstCell = GetCell(_firstCellCol, _firstCellRow);
                if (firstCell != null)
                {
                    var pos = new Vector2Int(_firstCellCol, _firstCellRow);
                    // Only clear if this cell doesn't have a permanently placed letter
                    if (!_allPlacedPositions.Contains(pos))
                    {
                        firstCell.ClearLetter();
                    }
                    else if (_placedLetters.TryGetValue(pos, out char existingLetter))
                    {
                        // Restore the original letter if there was one
                        firstCell.SetLetter(existingLetter);
                    }
                }
            }

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
        /// Attempts to place the current word randomly on the grid.
        /// </summary>
        public bool PlaceWordRandomly()
        {
            if (_placementState == PlacementState.Inactive)
            {
                Debug.LogError("[PlayerGridPanel] Not in placement mode");
                return false;
            }

            if (string.IsNullOrEmpty(_placementWord))
            {
                Debug.LogError("[PlayerGridPanel] No word to place");
                return false;
            }

            // Get all valid placements
            var validPlacements = GetAllValidPlacements();

            if (validPlacements.Count == 0)
            {
                Debug.LogWarning("[PlayerGridPanel] No valid placements found");
                return false;
            }

            // Pick a random valid placement
            int randomIndex = UnityEngine.Random.Range(0, validPlacements.Count);
            var (startCol, startRow, dCol, dRow) = validPlacements[randomIndex];

            return PlaceWordInDirection(startCol, startRow, dCol, dRow);
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

        private void UpdateLabelVisibility()
        {
            // Show/hide row labels based on current grid size
            for (int i = 0; i < MAX_GRID_SIZE; i++)
            {
                if (_rowLabelObjects[i] != null)
                {
                    _rowLabelObjects[i].SetActive(i < _currentGridSize);
                }
            }

            // Show/hide column labels based on current grid size
            for (int i = 0; i < MAX_GRID_SIZE; i++)
            {
                if (_columnLabelObjects[i] != null)
                {
                    _columnLabelObjects[i].SetActive(i < _currentGridSize);
                }
            }

            Debug.Log($"[PlayerGridPanel] Labels updated. Hidden: {MAX_GRID_SIZE - _currentGridSize} rows, {MAX_GRID_SIZE - _currentGridSize} columns");
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

        #region Private Methods - Placement Validation
        private List<(int col, int row, int dCol, int dRow)> GetAllValidPlacements()
        {
            var validPlacements = new List<(int, int, int, int)>();

            if (string.IsNullOrEmpty(_placementWord)) return validPlacements;

            int wordLength = _placementWord.Length;

            // 8 directions: horizontal, vertical, diagonal (including backwards)
            int[] dCols = { 1, 0, 1, 1, -1, 0, -1, -1 };
            int[] dRows = { 0, 1, 1, -1, 0, -1, -1, 1 };

            for (int startCol = 0; startCol < _currentGridSize; startCol++)
            {
                for (int startRow = 0; startRow < _currentGridSize; startRow++)
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

            // Shuffle for randomness
            ShuffleList(validPlacements);

            return validPlacements;
        }

        private bool IsValidPlacement(int startCol, int startRow, int dCol, int dRow, int wordLength)
        {
            // Check each letter position
            for (int i = 0; i < wordLength; i++)
            {
                int col = startCol + (i * dCol);
                int row = startRow + (i * dRow);

                // Check bounds
                if (!IsValidCoordinate(col, row))
                {
                    return false;
                }

                // Check for conflicts with existing letters
                var pos = new Vector2Int(col, row);
                if (_allPlacedPositions.Contains(pos))
                {
                    // There's already a letter here - check if it matches
                    if (_placedLetters.TryGetValue(pos, out char existingLetter))
                    {
                        if (existingLetter != _placementWord[i])
                        {
                            return false; // Conflict - different letter
                        }
                        // Same letter - valid overlap
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

            // 8 directions
            int[] dCols = { 1, 0, 1, 1, -1, 0, -1, -1 };
            int[] dRows = { 0, 1, 1, -1, 0, -1, -1, 1 };

            for (int d = 0; d < 8; d++)
            {
                if (IsValidPlacement(startCol, startRow, dCols[d], dRows[d], wordLength))
                {
                    // The second cell in this direction
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
        #endregion

        #region Private Methods - Placement Preview
        private void UpdatePlacementPreview(int hoverCol, int hoverRow)
        {
            // Clear previous highlighting
            ClearPlacementHighlighting();

            if (_placementState == PlacementState.SelectingFirstCell)
            {
                // Show valid directions from hover cell
                var validDirections = GetValidDirectionsFromCell(hoverCol, hoverRow);

                // Highlight current cell as cursor
                var hoverCell = GetCell(hoverCol, hoverRow);
                if (hoverCell != null)
                {
                    hoverCell.SetHighlightColor(_cursorColor);
                }

                // Highlight valid direction cells in green
                foreach (var pos in validDirections)
                {
                    var cell = GetCell(pos.x, pos.y);
                    if (cell != null)
                    {
                        cell.SetHighlightColor(_validPlacementColor);
                    }
                }

                // Highlight invalid cells (cells that would make word go out of bounds)
                HighlightInvalidCells(hoverCol, hoverRow, validDirections);
            }
            else if (_placementState == PlacementState.SelectingDirection)
            {
                // First cell stays highlighted as cursor
                var firstCell = GetCell(_firstCellCol, _firstCellRow);
                if (firstCell != null)
                {
                    firstCell.SetHighlightColor(_cursorColor);
                }

                // Show valid second cells
                var validDirections = GetValidDirectionsFromCell(_firstCellCol, _firstCellRow);
                foreach (var pos in validDirections)
                {
                    var cell = GetCell(pos.x, pos.y);
                    if (cell != null)
                    {
                        cell.SetHighlightColor(_validPlacementColor);
                    }
                }

                // If hovering over a valid direction, preview the full word
                if (validDirections.Contains(new Vector2Int(hoverCol, hoverRow)))
                {
                    PreviewWordPlacement(hoverCol, hoverRow);
                }
            }
        }

        private void HighlightInvalidCells(int hoverCol, int hoverRow, List<Vector2Int> validDirections)
        {
            // For each adjacent cell that is NOT in validDirections, mark as invalid
            int[] dCols = { 1, 0, 1, 1, -1, 0, -1, -1 };
            int[] dRows = { 0, 1, 1, -1, 0, -1, -1, 1 };

            for (int d = 0; d < 8; d++)
            {
                int adjCol = hoverCol + dCols[d];
                int adjRow = hoverRow + dRows[d];

                if (IsValidCoordinate(adjCol, adjRow))
                {
                    var adjPos = new Vector2Int(adjCol, adjRow);
                    if (!validDirections.Contains(adjPos))
                    {
                        var cell = GetCell(adjCol, adjRow);
                        if (cell != null)
                        {
                            cell.SetHighlightColor(_invalidPlacementColor);
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

            // Preview all letters in the word
            for (int i = 0; i < _placementWord.Length; i++)
            {
                int col = _firstCellCol + (i * dCol);
                int row = _firstCellRow + (i * dRow);

                var cell = GetCell(col, row);
                if (cell != null)
                {
                    cell.SetLetter(_placementWord[i]);

                    // First cell is cursor color, rest are valid placement color
                    if (i == 0)
                    {
                        cell.SetHighlightColor(_cursorColor);
                    }
                    else
                    {
                        cell.SetHighlightColor(_validPlacementColor);
                    }
                }
            }
        }

        private void ClearPlacementHighlighting()
        {
            for (int col = 0; col < _currentGridSize; col++)
            {
                for (int row = 0; row < _currentGridSize; row++)
                {
                    var cell = GetCell(col, row);
                    if (cell != null)
                    {
                        // Only clear highlighting, not permanently placed letters
                        cell.ClearHighlight();

                        var pos = new Vector2Int(col, row);
                        if (_allPlacedPositions.Contains(pos))
                        {
                            // Restore placed letter
                            if (_placedLetters.TryGetValue(pos, out char letter))
                            {
                                cell.SetLetter(letter);
                                cell.SetState(CellState.Filled);
                            }
                        }
                        else
                        {
                            // Clear any preview letters
                            cell.ClearLetter();
                        }
                    }
                }
            }
        }
        #endregion

        #region Private Methods - Word Placement
        private bool PlaceWordInDirection(int startCol, int startRow, int dCol, int dRow)
        {
            if (string.IsNullOrEmpty(_placementWord)) return false;

            // Clear previous highlighting
            ClearPlacementHighlighting();

            // Place all letters
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

            // Track which positions belong to this word row
            _wordRowPositions[_placementWordRowIndex] = new List<Vector2Int>(_placedCellPositions);

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
                // Check if there's at least one valid direction from this cell
                var validDirections = GetValidDirectionsFromCell(column, row);
                if (validDirections.Count == 0)
                {
                    Debug.Log($"[PlayerGridPanel] Invalid starting position: {GetColumnLetter(column)}{row + 1} - no valid directions for word");
                    return; // Don't allow placement here
                }

                _firstCellCol = column;
                _firstCellRow = row;
                _placementState = PlacementState.SelectingDirection;

                // Show first letter immediately as visual feedback
                var cell = GetCell(column, row);
                if (cell != null && !string.IsNullOrEmpty(_placementWord))
                {
                    cell.SetLetter(_placementWord[0]);
                    cell.SetHighlightColor(_cursorColor);
                }

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

        /// <summary>
        /// Clears a word's cells from the grid without resetting the row.
        /// Called when delete button is pressed on a placed word.
        /// </summary>
        private void ClearWordFromGrid(int rowIndex)
        {
            if (_wordRowPositions.TryGetValue(rowIndex, out List<Vector2Int> positions))
            {
                foreach (var pos in positions)
                {
                    var cell = GetCell(pos.x, pos.y);
                    if (cell != null)
                    {
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
                }

                _wordRowPositions.Remove(rowIndex);
                Debug.Log($"[PlayerGridPanel] Cleared grid cells for row {rowIndex + 1}");
            }
            else
            {
                Debug.LogWarning($"[PlayerGridPanel] No position tracking for row {rowIndex + 1}");
            }
        }

private void SubscribeToRandomPlacementButton()
        {
            if (_randomPlacementButton != null)
            {
                _randomPlacementButton.onClick.AddListener(HandleRandomPlacementClick);
                Debug.Log("[PlayerGridPanel] Random placement button subscribed successfully");
            }
            else
            {
                Debug.LogWarning("[PlayerGridPanel] Random placement button is NULL - not assigned in Inspector!");
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
            Debug.Log("[PlayerGridPanel] === RANDOM PLACEMENT BUTTON CLICKED ===");
            
            // Place all unplaced words that have text entered
            int placedCount = 0;
            int checkedCount = 0;

            for (int i = 0; i < _wordPatternRows.Count; i++)
            {
                var row = _wordPatternRows[i];
                if (row == null || !row.gameObject.activeSelf) continue;
                
                checkedCount++;
                Debug.Log($"[PlayerGridPanel] Row {i + 1}: HasWord={row.HasWord}, IsPlaced={row.IsPlaced}, Word='{row.CurrentWord}'");

                // Skip if already placed or no word entered
                if (row.IsPlaced || !row.HasWord) continue;

                // Enter placement mode for this row and place randomly
                EnterPlacementMode(i);

                if (_placementState != PlacementState.Inactive)
                {
                    bool success = PlaceWordRandomly();
                    if (success)
                    {
                        placedCount++;
                        Debug.Log($"[PlayerGridPanel] Randomly placed word {i + 1}: {row.CurrentWord}");
                    }
                    else
                    {
                        // Failed to place - cancel and continue to next word
                        CancelPlacementMode();
                        Debug.LogWarning($"[PlayerGridPanel] Could not find valid placement for word {i + 1}: {row.CurrentWord}");
                    }
                }
            }

            Debug.Log($"[PlayerGridPanel] Random placement complete. Checked {checkedCount} rows, placed {placedCount} word(s).");
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
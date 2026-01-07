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
        public enum PanelMode
        {
            Setup,
            Gameplay
        }
        #endregion

        #region Constants
        public const int MAX_GRID_SIZE = 12;
        public const int MIN_GRID_SIZE = 6;
        public const int MAX_WORD_ROWS = 4;
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

        [TitleGroup("Placement Colors")]
        [SerializeField]
        private Color _cursorColor = new Color(0.13f, 0.85f, 0.13f, 1f);

        [SerializeField]
        private Color _validPlacementColor = new Color(0.6f, 1f, 0.6f, 0.8f);

        [SerializeField]
        private Color _invalidPlacementColor = new Color(1f, 0f, 0f, 0.7f);

        [SerializeField]
        private Color _placedLetterColor = new Color(0.5f, 0.8f, 1f, 1f);
        #endregion

        #region Private Fields - Controllers
        private GridCellManager _gridCellManager;
        private LetterTrackerController _letterTrackerController;
        private GridColorManager _gridColorManager;
        private PlacementPreviewController _placementPreviewController;
        private WordPatternRowManager _wordPatternRowManager;
        private CoordinatePlacementController _coordinatePlacementController;
        private GridLayoutManager _gridLayoutManager;
        #endregion

        #region Private Fields - State
        private RectTransform _panelRectTransform;
        private bool _isInitialized;
        private bool _eventsWired;
        private List<WordPatternRow> _wordPatternRows = new List<WordPatternRow>();
        private int _selectedWordRowIndex = -1;
        private Func<string, int, bool> _wordValidator;
        #endregion

        #region Events
        public event Action<int, int> OnCellClicked;
        public event Action<int, int> OnCellHoverEnter;
        public event Action<int, int> OnCellHoverExit;
        public event Action<char> OnLetterClicked;
        public event Action<char> OnLetterHoverEnter;
        public event Action<char> OnLetterHoverExit;
        public event Action<int> OnWordRowSelected;
        public event Action<int> OnCoordinateModeRequested;
        public event Action<int, string, List<Vector2Int>> OnWordPlaced;
        public event Action OnPlacementCancelled;
        public event Action<char> OnLetterInput;
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
        }

        private void Start()
        {
            InitializeControllers();
            WireControllerEvents();
            CacheWordPatternRows();

            if (!_isInitialized)
            {
                InitializeGrid(_currentGridSize);
            }
        }
        #endregion

        #region Controller Initialization
        private void InitializeControllers()
        {
            // Check if already initialized (e.g., from EnsureControllersInitialized)
            if (_gridCellManager != null) return;

            _gridCellManager = new GridCellManager();

            _gridColorManager = new GridColorManager(
                _cursorColor,
                _validPlacementColor,
                _invalidPlacementColor,
                _placedLetterColor
            );

            _placementPreviewController = new PlacementPreviewController(
                _gridColorManager,
                _gridCellManager.GetCell,
                (col, row) => _gridCellManager.IsValidCoordinate(col, row, _currentGridSize)
            );

            _coordinatePlacementController = new CoordinatePlacementController(
                _gridColorManager,
                _gridCellManager.GetCell,
                () => _currentGridSize
            );

            _letterTrackerController = new LetterTrackerController(_letterTrackerContainer);
            _letterTrackerController.CacheLetterButtons();

            _wordPatternRowManager = new WordPatternRowManager(_wordPatternsContainer, _autocompleteDropdown);
            _wordPatternRowManager.CacheWordPatternRows();

            _gridLayoutManager = new GridLayoutManager(
                _gridContainer,
                _rowLabelsContainer,
                _columnLabelsContainer,
                _gridWithRowLabelsRect,
                _gridContainerLayout,
                _rowLabelsLayout,
                _cellPrefab,
                _panelRectTransform
            );
            _gridLayoutManager.CacheExistingLabels();
            _gridLayoutManager.OnCellCreated = HandleCellCreated;
        }

        private void WireControllerEvents()
        {
            if (_eventsWired) return;

            _coordinatePlacementController.OnPlacementCancelled += HandleCoordinatePlacementCancelled;
            _coordinatePlacementController.OnWordPlaced += HandleCoordinatePlacementWordPlaced;

            _letterTrackerController.OnLetterClicked += HandleLetterClicked;
            _letterTrackerController.OnLetterHoverEnter += HandleLetterHoverEnter;
            _letterTrackerController.OnLetterHoverExit += HandleLetterHoverExit;

            _wordPatternRowManager.OnWordRowSelected += HandleManagerWordRowSelected;
            _wordPatternRowManager.OnCoordinateModeRequested += HandleManagerCoordinateModeRequested;
            _wordPatternRowManager.OnDeleteClicked += HandleManagerDeleteClicked;
            _wordPatternRowManager.OnWordLengthsChanged += HandleManagerWordLengthsChanged;

            _eventsWired = true;
        }

        /// <summary>
        /// Wires controller events if they haven't been wired yet.
        /// Safe to call multiple times.
        /// </summary>
        private void WireControllerEventsIfNeeded()
        {
            if (_eventsWired) return;
            if (_coordinatePlacementController == null) return;
            if (_letterTrackerController == null) return;
            if (_wordPatternRowManager == null) return;

            WireControllerEvents();
        }
        #endregion

        #region Public Methods - Mode
        public void SetMode(PanelMode mode)
        {
            _currentMode = mode;

            if (IsInPlacementMode)
            {
                CancelPlacementMode();
            }
        }
        #endregion

        #region Public Methods - Initialization
        [Button("Initialize Grid")]
        public void InitializeGrid()
        {
            InitializeGrid(_currentGridSize);
        }

        public void InitializeGrid(int gridSize)
        {
            _currentGridSize = Mathf.Clamp(gridSize, MIN_GRID_SIZE, MAX_GRID_SIZE);

            if (_cellPrefab == null || _gridContainer == null)
            {
                Debug.LogError("[PlayerGridPanel] Cell prefab or grid container not assigned!");
                return;
            }

            CachePanelReferences();
            EnsureControllersInitialized();
            EnsureLayoutManagerInitialized();
            EnsureWordPatternsMinHeight();
            ClearGrid();
            _coordinatePlacementController?.ClearAllPlacedWords();

            _gridLayoutManager.UpdateGridLayoutConstraint(_currentGridSize);
            _gridLayoutManager.CreateCellsForCurrentSize(_currentGridSize, _gridCellManager.Cells);
            _gridLayoutManager.UpdateLabelVisibility(_currentGridSize);
            _gridLayoutManager.UpdatePanelHeight(_currentGridSize, _currentMode == PanelMode.Gameplay);

            _isInitialized = true;
        }

        private void EnsureLayoutManagerInitialized()
        {
            if (_gridLayoutManager != null) return;

            _gridLayoutManager = new GridLayoutManager(
                _gridContainer,
                _rowLabelsContainer,
                _columnLabelsContainer,
                _gridWithRowLabelsRect,
                _gridContainerLayout,
                _rowLabelsLayout,
                _cellPrefab,
                _panelRectTransform
            );
            _gridLayoutManager.CacheExistingLabels();
            _gridLayoutManager.OnCellCreated = HandleCellCreated;
        }

        /// <summary>
        /// Ensures word patterns container has a minimum height to prevent compression on larger grids.
        /// </summary>
        private void EnsureWordPatternsMinHeight()
        {
            if (_wordPatternsContainer == null) return;

            LayoutElement layoutElement = _wordPatternsContainer.GetComponent<LayoutElement>();
            if (layoutElement == null)
            {
                layoutElement = _wordPatternsContainer.gameObject.AddComponent<LayoutElement>();
            }

            // Set minimum height to prevent compression on larger grids (11x11, 12x12)
            layoutElement.minHeight = 100f;
            layoutElement.flexibleHeight = 0f;
        }

        /// <summary>
        /// Ensures all controllers are initialized. Called when methods are invoked
        /// before Start() has run (e.g., when panels are activated and configured
        /// in the same frame).
        /// </summary>
        private void EnsureControllersInitialized()
        {
            // Check if controllers need initialization
            if (_gridCellManager != null) return;

            Debug.Log("[PlayerGridPanel] EnsureControllersInitialized - initializing controllers before Start()");

            // Initialize all controllers (same as InitializeControllers but safe to call early)
            _gridCellManager = new GridCellManager();

            _gridColorManager = new GridColorManager(
                _cursorColor,
                _validPlacementColor,
                _invalidPlacementColor,
                _placedLetterColor
            );

            _placementPreviewController = new PlacementPreviewController(
                _gridColorManager,
                _gridCellManager.GetCell,
                (col, row) => _gridCellManager.IsValidCoordinate(col, row, _currentGridSize)
            );

            _coordinatePlacementController = new CoordinatePlacementController(
                _gridColorManager,
                _gridCellManager.GetCell,
                () => _currentGridSize
            );

            // Initialize letter tracker controller if container exists
            if (_letterTrackerContainer != null && _letterTrackerController == null)
            {
                _letterTrackerController = new LetterTrackerController(_letterTrackerContainer);
                _letterTrackerController.CacheLetterButtons();
            }

            // Initialize word pattern row manager if container exists
            if (_wordPatternsContainer != null && _wordPatternRowManager == null)
            {
                _wordPatternRowManager = new WordPatternRowManager(_wordPatternsContainer, _autocompleteDropdown);
                _wordPatternRowManager.CacheWordPatternRows();
            }

            // Wire controller events if not already wired
            WireControllerEventsIfNeeded();
        }
        #endregion

        #region Public Methods - Grid Size
        public void SetGridSize(int newSize)
        {
            if (newSize == _currentGridSize) return;

            foreach (WordPatternRow row in _wordPatternRows)
            {
                if (row != null && row.gameObject.activeSelf && row.IsPlaced)
                {
                    row.ResetToWordEntered();
                }
            }

            _currentGridSize = Mathf.Clamp(newSize, MIN_GRID_SIZE, MAX_GRID_SIZE);
            InitializeGrid(_currentGridSize);
        }
        #endregion

        #region Public Methods - Player Display
        public void SetPlayerName(string name)
        {
            if (_playerNameLabel != null)
            {
                _playerNameLabel.text = name;
            }
        }

        public void SetPlayerColor(Color color)
        {
            _playerColor = color;
            UpdatePlayerColorVisuals();
        }

        private void UpdatePlayerColorVisuals()
        {
            if (_playerNameLabel == null) return;

            Transform parentTransform = _playerNameLabel.transform.parent;
            if (parentTransform != null)
            {
                Image bgImage = parentTransform.GetComponent<Image>();
                if (bgImage != null)
                {
                    bgImage.color = _playerColor;
                }
            }
        }

        /// <summary>
        /// Sets the hit color for grid cells and letter tracker buttons.
        /// This is the color shown when the guesser makes correct guesses on this panel.
        /// Should be called with the guesser's color (not the panel owner's color).
        /// </summary>
        public void SetGuesserHitColor(Color color)
        {
            // Set hit color on all grid cells
            _gridCellManager?.SetHitColor(color);

            // Set hit color on all letter tracker buttons
            _letterTrackerController?.SetHitColor(color);

            Debug.Log($"[PlayerGridPanel] Set guesser hit color to: {color}");
        }
        #endregion

        #region Public Methods - Word Lengths
        public void SetWordLengths(int[] lengths)
        {
            if (lengths == null || lengths.Length == 0) return;

            for (int i = 0; i < _wordPatternRows.Count; i++)
            {
                if (i < lengths.Length)
                {
                    _wordPatternRows[i].gameObject.SetActive(true);
                    _wordPatternRows[i].SetRequiredLength(lengths[i]);

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

            ClearAllPlacedWords();
            OnWordLengthsChanged?.Invoke();
        }

        public void ClearAllPlacedWords()
        {
            _coordinatePlacementController?.ClearAllPlacedWords();
        }
        #endregion

        #region Public Methods - Letter Tracker
        public LetterButton GetLetterButton(char letter)
        {
            return _letterTrackerController?.GetLetterButton(letter);
        }

        public void SetLetterState(char letter, LetterButton.LetterState state)
        {
            _letterTrackerController?.SetLetterState(letter, state);
        }

        public LetterButton.LetterState GetLetterState(char letter)
        {
            return _letterTrackerController?.GetLetterState(letter) ?? LetterButton.LetterState.Normal;
        }

        public void ResetAllLetterButtons()
        {
            _letterTrackerController?.ResetAllLetterButtons();
        }

        public void SetLetterButtonsInteractable(bool interactable)
        {
            _letterTrackerController?.SetLetterButtonsInteractable(interactable);
        }

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
        public WordPatternRow GetWordPatternRow(int index)
        {
            if (index >= 0 && index < _wordPatternRows.Count)
            {
                return _wordPatternRows[index];
            }
            return null;
        }

        public WordPatternRow[] GetWordPatternRows()
        {
            if ((_wordPatternRows == null || _wordPatternRows.Count == 0) && _wordPatternsContainer != null)
            {
                CacheWordPatternRows();
            }

            return _wordPatternRows?.ToArray() ?? new WordPatternRow[0];
        }

        public struct WordPlacement
        {
            public string word;
            public int startCol;
            public int startRow;
            public int dirCol;
            public int dirRow;
            public int rowIndex;
        }

        public List<WordPlacement> GetAllWordPlacements()
        {
            List<WordPlacement> placements = new List<WordPlacement>();

            if (_coordinatePlacementController == null) return placements;

            foreach (KeyValuePair<int, List<Vector2Int>> kvp in _coordinatePlacementController.WordRowPositions)
            {
                int rowIndex = kvp.Key;
                List<Vector2Int> positions = kvp.Value;

                if (positions == null || positions.Count < 2) continue;

                string word = GetWordFromRow(rowIndex);
                if (string.IsNullOrEmpty(word)) continue;

                Vector2Int first = positions[0];
                Vector2Int second = positions[1];

                placements.Add(new WordPlacement
                {
                    word = word,
                    startCol = first.x,
                    startRow = first.y,
                    dirCol = second.x - first.x,
                    dirRow = second.y - first.y,
                    rowIndex = rowIndex
                });
            }

            return placements;
        }

        private string GetWordFromRow(int rowIndex)
        {
            if (rowIndex >= 0 && rowIndex < _wordPatternRows.Count)
            {
                return _wordPatternRows[rowIndex]?.CurrentWord;
            }
            return null;
        }

        public void SelectWordRow(int index)
        {
            int currentPlacementRow = _coordinatePlacementController?.PlacementWordRowIndex ?? -1;
            if (IsInPlacementMode && index != currentPlacementRow)
            {
                CancelPlacementMode();
            }

            if (_selectedWordRowIndex >= 0 && _selectedWordRowIndex < _wordPatternRows.Count)
            {
                _wordPatternRows[_selectedWordRowIndex].Deselect();
            }

            _selectedWordRowIndex = index;

            if (_selectedWordRowIndex >= 0 && _selectedWordRowIndex < _wordPatternRows.Count)
            {
                _wordPatternRows[_selectedWordRowIndex].Select();

                if (_autocompleteDropdown != null)
                {
                    _autocompleteDropdown.SetRequiredWordLength(_wordPatternRows[_selectedWordRowIndex].RequiredWordLength);
                    _autocompleteDropdown.ClearFilter();
                }
            }

            OnWordRowSelected?.Invoke(_selectedWordRowIndex);
        }

        public bool AddLetterToSelectedRow(char letter)
        {
            if (_selectedWordRowIndex < 0 || _selectedWordRowIndex >= _wordPatternRows.Count)
            {
                return false;
            }

            WordPatternRow row = _wordPatternRows[_selectedWordRowIndex];
            bool added = row.AddLetter(letter);

            if (added && _autocompleteDropdown != null)
            {
                _autocompleteDropdown.UpdateFilter(row.EnteredText);
            }

            return added;
        }

        public bool RemoveLastLetterFromSelectedRow()
        {
            if (_selectedWordRowIndex < 0 || _selectedWordRowIndex >= _wordPatternRows.Count)
            {
                return false;
            }

            WordPatternRow row = _wordPatternRows[_selectedWordRowIndex];
            bool removed = row.RemoveLastLetter();

            if (removed && _autocompleteDropdown != null)
            {
                _autocompleteDropdown.UpdateFilter(row.EnteredText);
            }

            return removed;
        }

        public bool ClearPlacedWord(int rowIndex)
        {
            if (rowIndex < 0 || rowIndex >= _wordPatternRows.Count)
            {
                return false;
            }

            WordPatternRow row = _wordPatternRows[rowIndex];

            if (!row.IsPlaced)
            {
                return false;
            }

            _coordinatePlacementController?.ClearWordFromGrid(rowIndex);
            row.ResetToEmpty();

            return true;
        }

        public bool ClearSelectedPlacedWord()
        {
            if (_selectedWordRowIndex < 0) return false;
            return ClearPlacedWord(_selectedWordRowIndex);
        }

        public void SetWordValidator(Func<string, int, bool> validator)
        {
            _wordValidator = validator;

            foreach (WordPatternRow row in _wordPatternRows)
            {
                row?.SetWordValidator(validator);
            }
        }

        [Button("Cache Word Pattern Rows")]
        public void CacheWordPatternRows()
        {
            _wordPatternRows.Clear();

            if (_wordPatternsContainer == null) return;

            WordPatternRow[] rows = _wordPatternsContainer.GetComponentsInChildren<WordPatternRow>(true);
            WordPatternRow[] sortedRows = rows.OrderBy(r => r.transform.GetSiblingIndex()).ToArray();

            foreach (WordPatternRow row in sortedRows)
            {
                row.OnRowSelected -= HandleWordRowSelected;
                row.OnRowSelected += HandleWordRowSelected;
                row.OnCoordinateModeClicked -= HandleCoordinateModeClicked;
                row.OnCoordinateModeClicked += HandleCoordinateModeClicked;
                row.OnDeleteClicked -= HandleDeleteClicked;
                row.OnDeleteClicked += HandleDeleteClicked;

                if (_wordValidator != null)
                {
                    row.SetWordValidator(_wordValidator);
                }

                _wordPatternRows.Add(row);
            }
        }

        public bool AreAllWordsPlaced()
        {
            foreach (WordPatternRow row in _wordPatternRows)
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
        public void EnterPlacementMode(int wordRowIndex)
        {
            if (wordRowIndex < 0 || wordRowIndex >= _wordPatternRows.Count)
            {
                return;
            }

            WordPatternRow row = _wordPatternRows[wordRowIndex];
            if (!row.HasWord)
            {
                return;
            }

            _coordinatePlacementController?.EnterPlacementMode(wordRowIndex, row.CurrentWord);
        }

        public void CancelPlacementMode()
        {
            _coordinatePlacementController?.CancelPlacementMode();
        }

        public bool PlaceWordRandomly()
        {
            return _coordinatePlacementController?.PlaceWordRandomly() ?? false;
        }

        public void PlaceAllWordsRandomly()
        {
            List<int> rowsToPlace = new List<int>();

            for (int i = 0; i < _wordPatternRows.Count; i++)
            {
                WordPatternRow row = _wordPatternRows[i];
                if (row == null || !row.gameObject.activeSelf) continue;
                if (row.IsPlaced || !row.HasWord) continue;

                rowsToPlace.Add(i);
            }

            // Sort by word length descending (place longest words first)
            rowsToPlace.Sort((a, b) =>
            {
                int lengthA = _wordPatternRows[a].CurrentWord?.Length ?? 0;
                int lengthB = _wordPatternRows[b].CurrentWord?.Length ?? 0;
                return lengthB.CompareTo(lengthA);
            });

            foreach (int i in rowsToPlace)
            {
                EnterPlacementMode(i);

                if (IsInPlacementMode)
                {
                    bool success = PlaceWordRandomly();
                    if (!success)
                    {
                        CancelPlacementMode();
                    }
                }
            }
        }
        #endregion

        #region Public Methods - Grid Cells
        public GridCellUI GetCell(int column, int row)
        {
            return _gridCellManager?.GetCell(column, row);
        }

        public bool IsValidCoordinate(int column, int row)
        {
            return _gridCellManager?.IsValidCoordinate(column, row, _currentGridSize) ?? false;
        }

        public char GetColumnLetter(int column)
        {
            return _gridCellManager?.GetColumnLetter(column) ?? (char)('A' + column);
        }

        public void ClearGrid()
        {
            _gridLayoutManager?.ClearGrid();
            _gridCellManager?.ClearCellArray();
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

            if (_gridWithRowLabelsRect == null && _gridContainer != null)
            {
                Transform parent = _gridContainer.parent;
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
        #endregion

        #region Private Methods - Cell Event Handlers
        private void HandleCellCreated(GridCellUI cell, int column, int row)
        {
            cell.OnCellClicked += HandleCellClicked;
            cell.OnCellHoverEnter += HandleCellHoverEnter;
            cell.OnCellHoverExit += HandleCellHoverExit;
        }

        private void HandleCellClicked(int column, int row)
        {
            if (!IsValidCoordinate(column, row)) return;

            if (_coordinatePlacementController != null && _coordinatePlacementController.HandleCellClick(column, row))
            {
                return;
            }

            OnCellClicked?.Invoke(column, row);
        }

        private void HandleCellHoverEnter(int column, int row)
        {
            if (!IsValidCoordinate(column, row)) return;

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
        #endregion

        #region Private Methods - Letter Event Handlers
        private void HandleLetterClicked(char letter)
        {
            OnLetterInput?.Invoke(letter);

            if (_currentMode == PanelMode.Setup && _selectedWordRowIndex >= 0)
            {
                AddLetterToSelectedRow(letter);
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
        #endregion

        #region Private Methods - Word Row Event Handlers
        private void HandleWordRowSelected(int rowNumber)
        {
            SelectWordRow(rowNumber - 1);
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

            if (wasPlaced && index >= 0 && index < _wordPatternRows.Count)
            {
                ClearWordFromGrid(index);
            }

            SelectWordRow(index);
        }

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
        #endregion

        #region Private Methods - Placement Event Handlers
        private void HandleCoordinatePlacementCancelled()
        {
            OnPlacementCancelled?.Invoke();
        }

private void HandleCoordinatePlacementWordPlaced(int rowIndex, string word, List<Vector2Int> positions)
        {
            if (rowIndex >= 0 && rowIndex < _wordPatternRows.Count)
            {
                WordPatternRow row = _wordPatternRows[rowIndex];

                // Extract placement position data from positions list
                if (positions != null && positions.Count >= 2)
                {
                    int startCol = positions[0].x;
                    int startRow = positions[0].y;
                    int dirCol = positions[1].x - positions[0].x;
                    int dirRow = positions[1].y - positions[0].y;
                    row.SetPlacementPosition(startCol, startRow, dirCol, dirRow);
                    Debug.Log(string.Format("[PlayerGridPanel] Set placement position for row {0}: ({1},{2}) dir({3},{4})", rowIndex + 1, startCol, startRow, dirCol, dirRow));
                }

                row.MarkAsPlaced();
            }

            OnWordPlaced?.Invoke(rowIndex, word, positions);
        }

        private void ClearWordFromGrid(int rowIndex)
        {
            if (_coordinatePlacementController == null) return;

            List<Vector2Int> positions = _coordinatePlacementController.GetPositionsForRow(rowIndex);

            if (positions == null || positions.Count == 0) return;

            foreach (Vector2Int pos in positions)
            {
                GridCellUI cell = GetCell(pos.x, pos.y);
                if (cell == null) continue;

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

            _coordinatePlacementController.ClearWordFromGrid(rowIndex);
        }
        #endregion

        #region Editor Helpers
#if UNITY_EDITOR
        [TitleGroup("Debug")]
        [Button("Clear Grid (Editor)")]
        private void EditorClearGrid()
        {
            ClearGrid();
        }

        [Button("Log Cell Count")]
        private void LogCellCount()
        {
            int count = _gridCellManager?.GetCellCount() ?? 0;
            Debug.Log(string.Format("[PlayerGridPanel] Cells: {0}, Children: {1}", count, _gridContainer?.childCount ?? 0));
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
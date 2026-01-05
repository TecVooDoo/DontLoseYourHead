// WordPatternRowUI.cs
// Manages a row of LetterCellUI components for word pattern display
// Used in both Setup (word entry) and Gameplay (word reveal) phases
// Created: January 4, 2026
// Updated: January 4, 2026 - Unified cell approach (all cells are LetterCellUI)
// Developer: TecVooDoo LLC

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

namespace TecVooDoo.DontLoseYourHead.UI
{
    /// <summary>
    /// Mode of operation for the word pattern row.
    /// </summary>
    public enum WordRowMode
    {
        Setup,      // Word entry mode - user types letters
        Gameplay    // Word reveal mode - letters discovered through gameplay
    }

    /// <summary>
    /// Manages a row of LetterCellUI components for displaying word patterns.
    ///
    /// Unified Cell Structure (all cells are LetterCellUI):
    /// - Cell 0: Row label (displays "1", "2", etc.)
    /// - Cells 1-8: Letter cells (8 total, hide unused based on word length)
    /// - Cells 9-11: Action cells (Setup: SEL/PLC/DEL icons, Gameplay: GW icon)
    ///
    /// Total: 12 cells, all structurally identical
    /// Benefits:
    /// - Uniform sizing and scaling
    /// - Any cell can animate (spin, scale, etc.)
    /// - Easy word length changes (just visibility)
    /// - Grid Layout Group compatible
    /// </summary>
    public class WordPatternRowUI : MonoBehaviour
    {
        // ============================================================
        // CONSTANTS
        // ============================================================

        public const int ROW_LABEL_INDEX = 0;
        public const int FIRST_LETTER_INDEX = 1;
        public const int MAX_LETTER_CELLS = 8;
        public const int FIRST_ACTION_INDEX = 9;
        public const int ACTION_CELL_COUNT = 3;
        public const int TOTAL_CELLS = 1 + MAX_LETTER_CELLS + ACTION_CELL_COUNT; // 12

        // ============================================================
        // SERIALIZED FIELDS
        // ============================================================

        [Header("Row Configuration")]
        [SerializeField] private int _rowIndex;
        [SerializeField] private int _wordLength = 3;
        [SerializeField] private WordRowMode _mode = WordRowMode.Setup;

        [Header("Cell Setup")]
        [SerializeField] private Transform _cellContainer;
        [SerializeField] private LetterCellUI _cellPrefab;

        [Header("Action Icons")]
        [SerializeField] private Sprite _selectIcon;
        [SerializeField] private Sprite _placeIcon;
        [SerializeField] private Sprite _deleteIcon;
        [SerializeField] private Sprite _guessWordIcon;

        [Header("Player Color")]
        [SerializeField] private Color _playerColor = Color.cyan;

        // ============================================================
        // EVENTS
        // ============================================================

        /// <summary>Fired when a letter cell is clicked. Params: rowIndex, letterIndex (0-based within word)</summary>
        public event Action<int, int> OnLetterCellClicked;

        /// <summary>Fired when select/compass button clicked. Param: rowIndex</summary>
        public event Action<int> OnSelectClicked;

        /// <summary>Fired when place button clicked. Param: rowIndex</summary>
        public event Action<int> OnPlaceClicked;

        /// <summary>Fired when delete button clicked. Param: rowIndex</summary>
        public event Action<int> OnDeleteClicked;

        /// <summary>Fired when guess word button clicked. Param: rowIndex</summary>
        public event Action<int> OnGuessWordClicked;

        /// <summary>Fired when word is complete (all letters filled). Params: rowIndex, word</summary>
        public event Action<int, string> OnWordComplete;

        // ============================================================
        // PRIVATE STATE
        // ============================================================

        private List<LetterCellUI> _allCells = new List<LetterCellUI>();
        private string _currentWord = "";
        private bool _isSelected = false;
        private bool _isPlaced = false;
        private bool _isWordSolved = false;
        private bool _isInitialized = false;

        // ============================================================
        // PROPERTIES
        // ============================================================

        public int RowIndex => _rowIndex;
        public int WordLength => _wordLength;
        public string CurrentWord => _currentWord;
        public bool IsSelected => _isSelected;
        public bool IsPlaced => _isPlaced;
        public bool IsWordSolved => _isWordSolved;
        public bool HasWord => !string.IsNullOrEmpty(_currentWord) && _currentWord.Length == _wordLength;
        public WordRowMode Mode => _mode;

        // Cell accessors
        private LetterCellUI RowLabelCell => _allCells.Count > ROW_LABEL_INDEX ? _allCells[ROW_LABEL_INDEX] : null;
        private LetterCellUI SelectCell => _allCells.Count > FIRST_ACTION_INDEX ? _allCells[FIRST_ACTION_INDEX] : null;
        private LetterCellUI PlaceCell => _allCells.Count > FIRST_ACTION_INDEX + 1 ? _allCells[FIRST_ACTION_INDEX + 1] : null;
        private LetterCellUI DeleteCell => _allCells.Count > FIRST_ACTION_INDEX + 2 ? _allCells[FIRST_ACTION_INDEX + 2] : null;

        // ============================================================
        // UNITY LIFECYCLE
        // ============================================================

        private void Start()
        {
            // Auto-initialize if not already initialized (for testing/prefab preview)
            if (!_isInitialized && _cellPrefab != null && _cellContainer != null)
            {
                Initialize(_rowIndex, _wordLength, _mode);
            }
        }

        // ============================================================
        // INITIALIZATION
        // ============================================================

        /// <summary>
        /// Initializes the row with specified settings.
        /// </summary>
        /// <param name="rowIndex">Row index (0-based)</param>
        /// <param name="wordLength">Required word length (3-8)</param>
        /// <param name="mode">Setup or Gameplay mode</param>
        public void Initialize(int rowIndex, int wordLength, WordRowMode mode = WordRowMode.Setup)
        {
            _rowIndex = rowIndex;
            _wordLength = Mathf.Clamp(wordLength, 3, MAX_LETTER_CELLS);
            _mode = mode;

            // Create all cells if needed
            if (!_isInitialized)
            {
                CreateAllCells();
                _isInitialized = true;
            }

            // Configure row label
            ConfigureRowLabel();

            // Configure cell visibility based on word length
            UpdateCellVisibility();

            // Configure action cells based on mode
            ConfigureActionCells();

            // Reset state
            ResetToEmpty();
        }

        /// <summary>
        /// Creates all cells for the row (label + letters + actions).
        /// </summary>
        private void CreateAllCells()
        {
            if (_cellContainer == null || _cellPrefab == null)
            {
                Debug.LogError("[WordPatternRowUI] Cell container or prefab not assigned!");
                return;
            }

            // Create all 12 cells
            for (int i = 0; i < TOTAL_CELLS; i++)
            {
                var cell = Instantiate(_cellPrefab, _cellContainer);
                cell.name = GetCellName(i);

                // Determine cell type and interactivity
                if (i == ROW_LABEL_INDEX)
                {
                    // Row label cell - displays number, non-interactive
                    cell.Initialize(i, CellContentType.Letter, false);
                }
                else if (i >= FIRST_LETTER_INDEX && i < FIRST_ACTION_INDEX)
                {
                    // Letter cell - displays letter or underscore, non-interactive (typing handled by parent)
                    cell.Initialize(i, CellContentType.Letter, false);
                    cell.OnCellClicked += HandleCellClicked;
                }
                else
                {
                    // Action cell - displays icon, interactive
                    cell.Initialize(i, CellContentType.Icon, true);
                    cell.OnCellClicked += HandleCellClicked;
                }

                _allCells.Add(cell);
            }

            Debug.Log($"[WordPatternRowUI] Created {_allCells.Count} cells for row {_rowIndex}");
        }

        private string GetCellName(int index)
        {
            if (index == ROW_LABEL_INDEX) return "Cell_RowLabel";
            if (index >= FIRST_LETTER_INDEX && index < FIRST_ACTION_INDEX)
                return $"Cell_Letter{index - FIRST_LETTER_INDEX}";
            if (index == FIRST_ACTION_INDEX) return "Cell_Select";
            if (index == FIRST_ACTION_INDEX + 1) return "Cell_Place";
            if (index == FIRST_ACTION_INDEX + 2) return "Cell_Delete";
            return $"Cell_{index}";
        }

        /// <summary>
        /// Configures the row label cell.
        /// </summary>
        private void ConfigureRowLabel()
        {
            if (RowLabelCell != null)
            {
                RowLabelCell.SetLetter((char)('0' + (_rowIndex + 1))); // "1", "2", etc.
                RowLabelCell.SetState(LetterCellState.Default);
            }
        }

        /// <summary>
        /// Updates cell visibility based on word length.
        /// </summary>
        private void UpdateCellVisibility()
        {
            for (int i = FIRST_LETTER_INDEX; i < FIRST_ACTION_INDEX; i++)
            {
                int letterIndex = i - FIRST_LETTER_INDEX;
                var cell = _allCells[i];

                if (letterIndex < _wordLength)
                {
                    cell.Show();
                    cell.SetContentType(CellContentType.Letter);
                }
                else
                {
                    cell.Hide();
                    cell.SetContentType(CellContentType.Empty);
                }
            }
        }

        /// <summary>
        /// Configures action cells based on current mode.
        /// </summary>
        private void ConfigureActionCells()
        {
            if (_mode == WordRowMode.Setup)
            {
                // Setup mode: Show Select, Place, Delete icons
                if (SelectCell != null)
                {
                    SelectCell.Show();
                    SelectCell.SetContentType(CellContentType.Icon);
                    SelectCell.SetIcon(_selectIcon);
                    SelectCell.SetInteractive(true);
                }

                if (PlaceCell != null)
                {
                    PlaceCell.Show();
                    PlaceCell.SetContentType(CellContentType.Icon);
                    PlaceCell.SetIcon(_placeIcon);
                    PlaceCell.SetInteractive(false); // Enable when word entered
                }

                if (DeleteCell != null)
                {
                    DeleteCell.Show();
                    DeleteCell.SetContentType(CellContentType.Icon);
                    DeleteCell.SetIcon(_deleteIcon);
                    DeleteCell.SetInteractive(false); // Enable when word entered
                }
            }
            else
            {
                // Gameplay mode: Show Guess Word icon in first action cell, hide others
                if (SelectCell != null)
                {
                    SelectCell.Show();
                    SelectCell.SetContentType(CellContentType.Icon);
                    SelectCell.SetIcon(_guessWordIcon);
                    SelectCell.SetInteractive(!_isWordSolved);
                }

                if (PlaceCell != null)
                {
                    PlaceCell.Hide();
                }

                if (DeleteCell != null)
                {
                    DeleteCell.Hide();
                }
            }
        }

        // ============================================================
        // WORD MANAGEMENT
        // ============================================================

        /// <summary>
        /// Sets the word to display.
        /// </summary>
        public void SetWord(string word)
        {
            _currentWord = word?.ToUpper() ?? "";

            for (int i = 0; i < _wordLength; i++)
            {
                var cell = GetLetterCell(i);
                if (cell != null)
                {
                    if (i < _currentWord.Length)
                    {
                        cell.SetLetter(_currentWord[i]);
                    }
                    else
                    {
                        cell.ShowUnderscore();
                    }
                }
            }

            UpdateActionCellStates();

            if (HasWord)
            {
                OnWordComplete?.Invoke(_rowIndex, _currentWord);
            }
        }

        /// <summary>
        /// Adds a letter to the current word (for typing).
        /// </summary>
        public bool AddLetter(char letter)
        {
            if (_currentWord.Length >= _wordLength) return false;

            _currentWord += char.ToUpper(letter);
            int letterIndex = _currentWord.Length - 1;

            var cell = GetLetterCell(letterIndex);
            if (cell != null)
            {
                cell.SetLetter(letter);
                cell.AnimatePunch(1.1f, 0.15f);
            }

            UpdateActionCellStates();

            if (HasWord)
            {
                OnWordComplete?.Invoke(_rowIndex, _currentWord);
            }

            return true;
        }

        /// <summary>
        /// Removes the last letter from the current word (backspace).
        /// </summary>
        public bool RemoveLastLetter()
        {
            if (_currentWord.Length == 0) return false;

            int letterIndex = _currentWord.Length - 1;
            _currentWord = _currentWord.Substring(0, _currentWord.Length - 1);

            var cell = GetLetterCell(letterIndex);
            if (cell != null)
            {
                cell.ShowUnderscore();
            }

            UpdateActionCellStates();
            return true;
        }

        /// <summary>
        /// Reveals a letter at the specified position (gameplay).
        /// </summary>
        public void RevealLetter(int position, char letter)
        {
            if (position < 0 || position >= _wordLength) return;

            var cell = GetLetterCell(position);
            if (cell != null)
            {
                cell.SetLetter(letter);
                cell.SetState(LetterCellState.Revealed);
                cell.SetPlayerColor(_playerColor);
                cell.AnimateReveal(0.3f);
            }

            // Update current word tracking
            if (position < _currentWord.Length)
            {
                char[] chars = _currentWord.ToCharArray();
                chars[position] = char.ToUpper(letter);
                _currentWord = new string(chars);
            }
            else
            {
                while (_currentWord.Length < position)
                {
                    _currentWord += "_";
                }
                _currentWord += char.ToUpper(letter);
            }
        }

        /// <summary>
        /// Reveals all letters in the word (gameplay - word solved).
        /// </summary>
        public void RevealAllLetters(string word)
        {
            _isWordSolved = true;
            _currentWord = word?.ToUpper() ?? "";

            var sequence = DOTween.Sequence();

            for (int i = 0; i < _wordLength && i < _currentWord.Length; i++)
            {
                int letterIndex = i;
                char letter = _currentWord[i];

                sequence.AppendCallback(() =>
                {
                    var cell = GetLetterCell(letterIndex);
                    if (cell != null)
                    {
                        cell.SetLetter(letter);
                        cell.SetState(LetterCellState.Revealed);
                        cell.SetPlayerColor(_playerColor);
                        cell.AnimateSpin(0.3f);
                    }
                });
                sequence.AppendInterval(0.1f);
            }

            // Disable guess word cell when solved
            if (_mode == WordRowMode.Gameplay && SelectCell != null)
            {
                SelectCell.SetInteractive(false);
                SelectCell.SetState(LetterCellState.Disabled);
            }
        }

        /// <summary>
        /// Resets the row to empty state.
        /// </summary>
        public void ResetToEmpty()
        {
            _currentWord = "";
            _isSelected = false;
            _isPlaced = false;
            _isWordSolved = false;

            // Reset letter cells
            for (int i = 0; i < MAX_LETTER_CELLS; i++)
            {
                var cell = GetLetterCell(i);
                if (cell != null)
                {
                    cell.Reset();
                    cell.ShowUnderscore();
                    cell.SetState(LetterCellState.Default);
                }
            }

            UpdateActionCellStates();
        }

        // ============================================================
        // SELECTION & PLACEMENT
        // ============================================================

        /// <summary>
        /// Sets whether this row is currently selected.
        /// </summary>
        public void SetSelected(bool selected)
        {
            _isSelected = selected;
            LetterCellState state = selected ? LetterCellState.Selected : LetterCellState.Default;

            for (int i = 0; i < _wordLength; i++)
            {
                var cell = GetLetterCell(i);
                if (cell != null && !_isWordSolved)
                {
                    cell.SetState(state);
                }
            }
        }

        /// <summary>
        /// Marks the word as placed on the grid.
        /// </summary>
        public void SetPlaced(bool placed)
        {
            _isPlaced = placed;

            if (placed)
            {
                for (int i = 0; i < _wordLength; i++)
                {
                    var cell = GetLetterCell(i);
                    if (cell != null)
                    {
                        cell.SetState(LetterCellState.Locked);
                    }
                }
            }

            UpdateActionCellStates();
        }

        /// <summary>
        /// Sets the player color for revealed letters.
        /// </summary>
        public void SetPlayerColor(Color color)
        {
            _playerColor = color;

            for (int i = 0; i < MAX_LETTER_CELLS; i++)
            {
                var cell = GetLetterCell(i);
                if (cell != null)
                {
                    cell.SetPlayerColor(color);
                }
            }
        }

        // ============================================================
        // PRIVATE HELPERS
        // ============================================================

        /// <summary>
        /// Gets a letter cell by its index within the word (0 to wordLength-1).
        /// </summary>
        private LetterCellUI GetLetterCell(int letterIndex)
        {
            int cellIndex = FIRST_LETTER_INDEX + letterIndex;
            if (cellIndex >= 0 && cellIndex < _allCells.Count)
            {
                return _allCells[cellIndex];
            }
            return null;
        }

        private void UpdateActionCellStates()
        {
            if (_mode == WordRowMode.Setup)
            {
                // Select always enabled
                if (SelectCell != null)
                {
                    SelectCell.SetInteractive(true);
                }

                // Place enabled when word complete and not yet placed
                if (PlaceCell != null)
                {
                    PlaceCell.SetInteractive(HasWord && !_isPlaced);
                }

                // Delete enabled when there's content
                if (DeleteCell != null)
                {
                    DeleteCell.SetInteractive(_currentWord.Length > 0 || _isPlaced);
                }
            }
            else if (_mode == WordRowMode.Gameplay)
            {
                // Guess Word enabled when not solved
                if (SelectCell != null)
                {
                    SelectCell.SetInteractive(!_isWordSolved);
                }
            }
        }

        private void HandleCellClicked(int cellIndex)
        {
            if (cellIndex >= FIRST_LETTER_INDEX && cellIndex < FIRST_ACTION_INDEX)
            {
                // Letter cell clicked
                int letterIndex = cellIndex - FIRST_LETTER_INDEX;
                OnLetterCellClicked?.Invoke(_rowIndex, letterIndex);
            }
            else if (cellIndex >= FIRST_ACTION_INDEX)
            {
                // Action cell clicked
                int actionIndex = cellIndex - FIRST_ACTION_INDEX;

                if (_mode == WordRowMode.Setup)
                {
                    switch (actionIndex)
                    {
                        case 0: OnSelectClicked?.Invoke(_rowIndex); break;
                        case 1: OnPlaceClicked?.Invoke(_rowIndex); break;
                        case 2: OnDeleteClicked?.Invoke(_rowIndex); break;
                    }
                }
                else
                {
                    // Gameplay mode - only first action cell (Guess Word)
                    if (actionIndex == 0)
                    {
                        OnGuessWordClicked?.Invoke(_rowIndex);
                    }
                }
            }
        }

        // ============================================================
        // CLEANUP
        // ============================================================

        private void OnDestroy()
        {
            foreach (var cell in _allCells)
            {
                if (cell != null)
                {
                    cell.OnCellClicked -= HandleCellClicked;
                }
            }
        }
    }
}

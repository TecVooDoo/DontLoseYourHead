using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;

namespace TecVooDoo.DontLoseYourHead.UI
{
    /// <summary>
    /// Manages word pattern rows: caching, selection, input routing, and state queries.
    /// Does NOT directly manipulate grid cells - raises events for grid operations.
    /// </summary>
    public class WordPatternRowManager
    {
        #region Events
        /// <summary>
        /// Fired when a word pattern row is selected. Parameter: row index (0-based)
        /// </summary>
        public event Action<int> OnWordRowSelected;

        /// <summary>
        /// Fired when coordinate mode is requested. Parameter: row index (0-based)
        /// </summary>
        public event Action<int> OnCoordinateModeRequested;

        /// <summary>
        /// Fired when delete is clicked on a row. Parameters: row index (0-based), was placed
        /// </summary>
        public event Action<int, bool> OnDeleteClicked;

        /// <summary>
        /// Fired when word lengths change (rows may need re-placement)
        /// </summary>
        public event Action OnWordLengthsChanged;
        #endregion

        #region Fields
        private readonly Transform _wordPatternsContainer;
        private readonly AutocompleteDropdown _autocompleteDropdown;
        private List<WordPatternRow> _wordPatternRows = new List<WordPatternRow>();
        private int _selectedWordRowIndex = -1;
        private Func<string, int, bool> _wordValidator;
        #endregion

        #region Properties
        public int SelectedRowIndex => _selectedWordRowIndex;
        public int RowCount => _wordPatternRows.Count;
        public bool HasSelection => _selectedWordRowIndex >= 0 && _selectedWordRowIndex < _wordPatternRows.Count;
        #endregion

        #region Constructor
        public WordPatternRowManager(Transform wordPatternsContainer, AutocompleteDropdown autocompleteDropdown)
        {
            _wordPatternsContainer = wordPatternsContainer;
            _autocompleteDropdown = autocompleteDropdown;
        }
        #endregion

        #region Public Methods - Initialization
        /// <summary>
        /// Caches word pattern row references from the container.
        /// </summary>
        public void CacheWordPatternRows()
        {
            _wordPatternRows.Clear();

            if (_wordPatternsContainer == null)
            {
                Debug.LogWarning("[WordPatternRowManager] Word patterns container not assigned.");
                return;
            }

            // Find all WordPatternRow components in children
            WordPatternRow[] rows = _wordPatternsContainer.GetComponentsInChildren<WordPatternRow>(true);

            // Sort by sibling index to ensure correct visual order (top to bottom)
            WordPatternRow[] sortedRows = rows.OrderBy(r => r.transform.GetSiblingIndex()).ToArray();

            Debug.Log($"[WordPatternRowManager] CacheWordPatternRows: Found {rows.Length} rows, sorting by sibling index");

            foreach (WordPatternRow row in sortedRows)
            {
                // Unsubscribe first to prevent duplicates
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
                Debug.Log($"[WordPatternRowManager] Cached row: sibling={row.transform.GetSiblingIndex()}, name={row.gameObject.name}");
            }

            Debug.Log($"[WordPatternRowManager] Cached {_wordPatternRows.Count} word pattern rows");
        }

        /// <summary>
        /// Sets the word validator for all word rows.
        /// Validator function receives (word, requiredLength) and returns true if valid.
        /// </summary>
        public void SetWordValidator(Func<string, int, bool> validator)
        {
            _wordValidator = validator;

            // Apply to all existing word pattern rows
            foreach (WordPatternRow row in _wordPatternRows)
            {
                if (row != null)
                {
                    row.SetWordValidator(validator);
                }
            }

            Debug.Log("[WordPatternRowManager] Word validator set");
        }
        #endregion

        #region Public Methods - Row Access
        /// <summary>
        /// Gets a word pattern row by index.
        /// </summary>
        public WordPatternRow GetRow(int index)
        {
            if (index >= 0 && index < _wordPatternRows.Count)
            {
                return _wordPatternRows[index];
            }
            return null;
        }

        /// <summary>
        /// Returns all WordPatternRow components.
        /// </summary>
        public WordPatternRow[] GetAllRows()
        {
            // Auto-cache if list is empty but container exists
            if ((_wordPatternRows == null || _wordPatternRows.Count == 0) && _wordPatternsContainer != null)
            {
                Debug.Log("[WordPatternRowManager] GetAllRows: Auto-caching word pattern rows");
                CacheWordPatternRows();
            }

            if (_wordPatternRows == null || _wordPatternRows.Count == 0)
            {
                Debug.LogWarning("[WordPatternRowManager] GetAllRows: No word pattern rows found");
                return new WordPatternRow[0];
            }

            return _wordPatternRows.ToArray();
        }
        #endregion

        #region Public Methods - Selection
        /// <summary>
        /// Selects a word pattern row for input.
        /// </summary>
        /// <param name="index">Row index (0-based)</param>
        /// <param name="cancelPlacementCallback">Optional callback to cancel placement mode if switching rows</param>
        public void SelectRow(int index, Action cancelPlacementCallback = null)
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
        /// Deselects the currently selected row.
        /// </summary>
        public void DeselectCurrentRow()
        {
            if (_selectedWordRowIndex >= 0 && _selectedWordRowIndex < _wordPatternRows.Count)
            {
                _wordPatternRows[_selectedWordRowIndex].Deselect();
            }
            _selectedWordRowIndex = -1;
        }
        #endregion

        #region Public Methods - Letter Input
        /// <summary>
        /// Adds a letter to the currently selected word row.
        /// </summary>
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

        /// <summary>
        /// Removes the last letter from the currently selected word row.
        /// </summary>
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
        #endregion

        #region Public Methods - Word Lengths
        /// <summary>
        /// Sets the required word lengths for each row.
        /// Returns list of row indices that need re-placement (were previously placed).
        /// </summary>
        public List<int> SetWordLengths(int[] lengths)
        {
            List<int> rowsNeedingReplacement = new List<int>();

            if (lengths == null || lengths.Length == 0)
            {
                Debug.LogWarning("[WordPatternRowManager] SetWordLengths called with null or empty array");
                return rowsNeedingReplacement;
            }

            // Show/hide rows based on array length
            for (int i = 0; i < _wordPatternRows.Count; i++)
            {
                if (i < lengths.Length)
                {
                    _wordPatternRows[i].gameObject.SetActive(true);
                    _wordPatternRows[i].SetRequiredLength(lengths[i]);

                    // If row was placed, reset to WordEntered since grid will be cleared
                    if (_wordPatternRows[i].IsPlaced)
                    {
                        _wordPatternRows[i].ResetToWordEntered();
                        rowsNeedingReplacement.Add(i);
                    }
                }
                else
                {
                    _wordPatternRows[i].gameObject.SetActive(false);
                }
            }

            Debug.Log($"[WordPatternRowManager] Set word lengths: {string.Join(", ", lengths)}. {rowsNeedingReplacement.Count} rows need re-placement.");
            OnWordLengthsChanged?.Invoke();

            return rowsNeedingReplacement;
        }
        #endregion

        #region Public Methods - State Queries
        /// <summary>
        /// Checks if all active words are placed.
        /// </summary>
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

        /// <summary>
        /// Gets the word from a specific row.
        /// </summary>
        public string GetWordFromRow(int index)
        {
            WordPatternRow row = GetRow(index);
            if (row != null)
            {
                return row.CurrentWord;
            }
            return string.Empty;
        }

        /// <summary>
        /// Checks if a row has a word entered.
        /// </summary>
        public bool RowHasWord(int index)
        {
            WordPatternRow row = GetRow(index);
            return row != null && row.HasWord;
        }

        /// <summary>
        /// Checks if a row is placed.
        /// </summary>
        public bool RowIsPlaced(int index)
        {
            WordPatternRow row = GetRow(index);
            return row != null && row.IsPlaced;
        }

        /// <summary>
        /// Gets indices of rows that have words but are not placed.
        /// Sorted by word length descending (longest first).
        /// </summary>
        public List<int> GetUnplacedRowIndices()
        {
            List<int> unplacedRows = new List<int>();

            for (int i = 0; i < _wordPatternRows.Count; i++)
            {
                WordPatternRow row = _wordPatternRows[i];
                if (row == null || !row.gameObject.activeSelf) continue;
                if (row.IsPlaced || !row.HasWord) continue;

                unplacedRows.Add(i);
            }

            // Sort by word length descending (longest words first)
            unplacedRows.Sort((a, b) =>
            {
                int lengthA = _wordPatternRows[a].CurrentWord?.Length ?? 0;
                int lengthB = _wordPatternRows[b].CurrentWord?.Length ?? 0;
                return lengthB.CompareTo(lengthA);
            });

            return unplacedRows;
        }

        /// <summary>
        /// Gets the count of active (visible) rows.
        /// </summary>
        public int GetActiveRowCount()
        {
            int count = 0;
            foreach (WordPatternRow row in _wordPatternRows)
            {
                if (row.gameObject.activeSelf)
                {
                    count++;
                }
            }
            return count;
        }
        #endregion

        #region Public Methods - Row State Management
        /// <summary>
        /// Marks a row as placed.
        /// </summary>
        public void MarkRowAsPlaced(int index)
        {
            WordPatternRow row = GetRow(index);
            if (row != null)
            {
                row.MarkAsPlaced();
            }
        }

        /// <summary>
        /// Sets placement position on a row.
        /// </summary>
        public void SetRowPlacementPosition(int index, int startCol, int startRow, int dirCol, int dirRow)
        {
            WordPatternRow row = GetRow(index);
            if (row != null)
            {
                row.SetPlacementPosition(startCol, startRow, dirCol, dirRow);
            }
        }

        /// <summary>
        /// Resets a row to empty state.
        /// </summary>
        public void ResetRowToEmpty(int index)
        {
            WordPatternRow row = GetRow(index);
            if (row != null)
            {
                row.ResetToEmpty();
            }
        }

        /// <summary>
        /// Resets a row to word entered state (keeps word, needs placement).
        /// </summary>
        public void ResetRowToWordEntered(int index)
        {
            WordPatternRow row = GetRow(index);
            if (row != null)
            {
                row.ResetToWordEntered();
            }
        }
        #endregion

        #region Private Methods - Event Handlers
        private void HandleWordRowSelected(int rowNumber)
        {
            int index = rowNumber - 1;
            SelectRow(index);
        }

        private void HandleCoordinateModeClicked(int rowNumber)
        {
            int index = rowNumber - 1;
            OnCoordinateModeRequested?.Invoke(index);
        }

        private void HandleDeleteClicked(int rowNumber, bool wasPlaced)
        {
            int index = rowNumber - 1;
            Debug.Log($"[WordPatternRowManager] Delete clicked on row {rowNumber}, wasPlaced: {wasPlaced}");
            OnDeleteClicked?.Invoke(index, wasPlaced);
        }
        #endregion

        #region Cleanup
        /// <summary>
        /// Unsubscribes from all row events.
        /// </summary>
        public void Cleanup()
        {
            foreach (WordPatternRow row in _wordPatternRows)
            {
                if (row != null)
                {
                    row.OnRowSelected -= HandleWordRowSelected;
                    row.OnCoordinateModeClicked -= HandleCoordinateModeClicked;
                    row.OnDeleteClicked -= HandleDeleteClicked;
                }
            }
            _wordPatternRows.Clear();
            _selectedWordRowIndex = -1;
        }
        #endregion
    }
}

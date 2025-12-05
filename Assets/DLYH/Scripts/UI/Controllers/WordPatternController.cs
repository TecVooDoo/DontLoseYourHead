using System;
using System.Collections.Generic;
using UnityEngine;

namespace TecVooDoo.DontLoseYourHead.UI
{
    /// <summary>
    /// Manages word pattern rows for word entry and display.
    /// Plain class - receives container reference via constructor.
    /// </summary>
    public class WordPatternController : IWordPatternController
    {
        private readonly Transform _container;
        private readonly List<WordPatternRow> _wordPatternRows;
        private readonly AutocompleteDropdown _autocompleteDropdown;
        
        private int _selectedWordRowIndex = -1;
        private Func<string, int, bool> _wordValidator;

        public event Action<int> OnWordRowSelected;
        public event Action<int> OnCoordinateModeRequested;
        public event Action<int, string, List<Vector2Int>> OnWordPlaced;
        public event Action<int, bool> OnDeleteClicked;

        public int WordRowCount => _wordPatternRows.Count;
        public int SelectedWordRowIndex => _selectedWordRowIndex;

        /// <summary>
        /// Creates a new WordPatternController.
        /// </summary>
        /// <param name="wordPatternsContainer">Transform containing WordPatternRow components</param>
        /// <param name="autocompleteDropdown">Optional autocomplete dropdown for filtering</param>
        public WordPatternController(Transform wordPatternsContainer, AutocompleteDropdown autocompleteDropdown = null)
        {
            _container = wordPatternsContainer;
            _autocompleteDropdown = autocompleteDropdown;
            _wordPatternRows = new List<WordPatternRow>();
        }

        /// <summary>
        /// Caches word pattern row references from the container.
        /// </summary>
        public void CacheWordPatternRows()
        {
            _wordPatternRows.Clear();

            if (_container == null)
            {
                Debug.LogWarning("[WordPatternController] Container not assigned.");
                return;
            }

            var rows = _container.GetComponentsInChildren<WordPatternRow>(true);

            foreach (var row in rows)
            {
                // Unsubscribe first to prevent duplicates
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

            Debug.Log(string.Format("[WordPatternController] Cached {0} word pattern rows", _wordPatternRows.Count));
        }

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
        /// Returns all WordPatternRow components.
        /// </summary>
        public WordPatternRow[] GetWordPatternRows()
        {
            return _wordPatternRows.ToArray();
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
        /// Sets the required word lengths for each row.
        /// </summary>
        public void SetWordLengths(int[] lengths)
        {
            if (lengths == null || lengths.Length == 0)
            {
                Debug.LogWarning("[WordPatternController] SetWordLengths called with null or empty array");
                return;
            }

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

            Debug.Log(string.Format("[WordPatternController] Set word lengths: {0}", string.Join(", ", lengths)));
        }

        /// <summary>
        /// Sets the word validator for all word rows.
        /// </summary>
        public void SetWordValidator(Func<string, int, bool> validator)
        {
            _wordValidator = validator;

            foreach (var row in _wordPatternRows)
            {
                if (row != null)
                {
                    row.SetWordValidator(validator);
                }
            }

            Debug.Log("[WordPatternController] Word validator set");
        }

        /// <summary>
        /// Checks if all visible words are placed.
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

        /// <summary>
        /// Clears a placed word (resets the row for re-entry).
        /// Note: Grid clearing must be handled separately by the caller.
        /// </summary>
        public bool ClearPlacedWord(int rowIndex)
        {
            if (rowIndex < 0 || rowIndex >= _wordPatternRows.Count)
            {
                return false;
            }

            var row = _wordPatternRows[rowIndex];
            if (!row.IsPlaced)
            {
                return false;
            }

            row.ResetToEmpty();
            return true;
        }

        /// <summary>
        /// Gets the word at a specific row index.
        /// </summary>
        public string GetWordAtRow(int rowIndex)
        {
            var row = GetWordPatternRow(rowIndex);
            return row != null ? row.CurrentWord : null;
        }

        /// <summary>
        /// Checks if a row has a word entered.
        /// </summary>
        public bool HasWordAtRow(int rowIndex)
        {
            var row = GetWordPatternRow(rowIndex);
            return row != null && row.HasWord;
        }

        /// <summary>
        /// Checks if a row is placed on the grid.
        /// </summary>
        public bool IsRowPlaced(int rowIndex)
        {
            var row = GetWordPatternRow(rowIndex);
            return row != null && row.IsPlaced;
        }

        /// <summary>
        /// Marks a row as placed.
        /// </summary>
        public void MarkRowAsPlaced(int rowIndex)
        {
            var row = GetWordPatternRow(rowIndex);
            if (row != null)
            {
                row.MarkAsPlaced();
            }
        }

        /// <summary>
        /// Resets a row to WordEntered state (keeps word but needs re-placement).
        /// </summary>
        public void ResetRowToWordEntered(int rowIndex)
        {
            var row = GetWordPatternRow(rowIndex);
            if (row != null)
            {
                row.ResetToWordEntered();
            }
        }

        /// <summary>
        /// Disposes event subscriptions. Call when destroying the controller.
        /// </summary>
        public void Dispose()
        {
            foreach (var row in _wordPatternRows)
            {
                if (row != null)
                {
                    row.OnRowSelected -= HandleWordRowSelected;
                    row.OnCoordinateModeClicked -= HandleCoordinateModeClicked;
                    row.OnDeleteClicked -= HandleDeleteClicked;
                }
            }
            _wordPatternRows.Clear();
        }

        private void HandleWordRowSelected(int rowNumber)
        {
            int index = rowNumber - 1;
            SelectWordRow(index);
        }

        private void HandleCoordinateModeClicked(int rowNumber)
        {
            int index = rowNumber - 1;
            OnCoordinateModeRequested?.Invoke(index);
        }

        private void HandleDeleteClicked(int rowNumber, bool wasPlaced)
        {
            int index = rowNumber - 1;
            OnDeleteClicked?.Invoke(index, wasPlaced);
        }
    }
}

using System;
using System.Collections.Generic;
using UnityEngine;

namespace DLYH.TableUI
{
    /// <summary>
    /// Handles word placement logic during game setup.
    /// Manages word selection, grid placement, validation, and random placement.
    /// </summary>
    public class WordPlacementController
    {
        private TableModel _model;
        private TableView _view;
        private TableLayout _layout;
        private Color _playerColor;

        private int _gridSize;
        private int _wordCount;
        private string[] _words;
        private WordPlacement[] _placements;

        // Current interaction state
        private int _selectedWordIndex = -1;
        private int _anchorRow = -1;
        private int _anchorCol = -1;
        private PlacementDirection _direction = PlacementDirection.None;

        // Events
        public event Action<int, string> OnWordPlaced;
        public event Action<int> OnWordCleared;
        public event Action OnAllWordsPlaced;
        public event Action<string> OnValidationError;

        public enum PlacementDirection
        {
            None,
            Horizontal,
            Vertical
        }

        public struct WordPlacement
        {
            public string Word;
            public int StartRow;
            public int StartCol;
            public PlacementDirection Direction;
            public bool IsPlaced;
        }

        /// <summary>
        /// Initializes the placement controller with a model, view, and layout.
        /// </summary>
        public void Initialize(TableModel model, TableView view, TableLayout layout, Color playerColor)
        {
            _model = model ?? throw new ArgumentNullException(nameof(model));
            _view = view ?? throw new ArgumentNullException(nameof(view));
            _layout = layout ?? throw new ArgumentNullException(nameof(layout));
            _playerColor = playerColor;

            _gridSize = layout.GridSize;
            _wordCount = layout.WordCount;
            _words = new string[_wordCount];
            _placements = new WordPlacement[_wordCount];

            // Initialize empty words
            for (int i = 0; i < _wordCount; i++)
            {
                _words[i] = "";
                _placements[i] = new WordPlacement
                {
                    Word = "",
                    StartRow = -1,
                    StartCol = -1,
                    Direction = PlacementDirection.None,
                    IsPlaced = false
                };
            }

            // Subscribe to view events
            _view.OnCellClicked += HandleCellClicked;
        }

        /// <summary>
        /// Cleans up event subscriptions.
        /// </summary>
        public void Dispose()
        {
            if (_view != null)
            {
                _view.OnCellClicked -= HandleCellClicked;
            }
        }

        // === Word Input ===

        /// <summary>
        /// Sets the word for a specific word slot.
        /// </summary>
        public bool SetWord(int wordIndex, string word)
        {
            if (wordIndex < 0 || wordIndex >= _wordCount)
            {
                return false;
            }

            word = word?.ToUpperInvariant() ?? "";

            // Validate word length
            if (word.Length > _gridSize)
            {
                OnValidationError?.Invoke($"Word too long. Max length is {_gridSize}");
                return false;
            }

            // Validate characters (letters only)
            foreach (char c in word)
            {
                if (c < 'A' || c > 'Z')
                {
                    OnValidationError?.Invoke("Words can only contain letters A-Z");
                    return false;
                }
            }

            // Clear existing placement if word changed
            if (_words[wordIndex] != word && _placements[wordIndex].IsPlaced)
            {
                ClearPlacement(wordIndex);
            }

            _words[wordIndex] = word;

            // Update word slot display
            UpdateWordSlotDisplay(wordIndex);

            Debug.Log($"[WordPlacement] Set word {wordIndex + 1}: {word}");
            return true;
        }

        /// <summary>
        /// Adds a letter to the selected word slot.
        /// </summary>
        public void AddLetterToSelectedWord(char letter)
        {
            if (_selectedWordIndex < 0 || _selectedWordIndex >= _wordCount)
            {
                return;
            }

            string currentWord = _words[_selectedWordIndex];
            if (currentWord.Length >= _gridSize)
            {
                OnValidationError?.Invoke($"Word at max length ({_gridSize})");
                return;
            }

            SetWord(_selectedWordIndex, currentWord + letter);
        }

        /// <summary>
        /// Removes the last letter from the selected word.
        /// </summary>
        public void RemoveLetterFromSelectedWord()
        {
            if (_selectedWordIndex < 0 || _selectedWordIndex >= _wordCount)
            {
                return;
            }

            string currentWord = _words[_selectedWordIndex];
            if (currentWord.Length > 0)
            {
                SetWord(_selectedWordIndex, currentWord.Substring(0, currentWord.Length - 1));
            }
        }

        /// <summary>
        /// Selects a word slot for input.
        /// </summary>
        public void SelectWordSlot(int wordIndex)
        {
            if (wordIndex < 0 || wordIndex >= _wordCount)
            {
                _selectedWordIndex = -1;
                return;
            }

            _selectedWordIndex = wordIndex;
            Debug.Log($"[WordPlacement] Selected word slot: {wordIndex + 1}");

            // Update visual selection (highlight the word row)
            UpdateWordSlotSelectionVisuals();
        }

        // === Grid Placement ===

        private void HandleCellClicked(int row, int col, TableCell cell)
        {
            // Determine what kind of cell was clicked
            if (cell.Kind == TableCellKind.WordSlot)
            {
                // Clicked a word slot - select that word for editing
                int wordIndex = row; // Word rows are indexed by their row
                SelectWordSlot(wordIndex);
            }
            else if (cell.Kind == TableCellKind.GridCell)
            {
                // Clicked a grid cell - handle placement
                HandleGridCellClick(row, col, cell);
            }
        }

        private void HandleGridCellClick(int row, int col, TableCell cell)
        {
            // Convert table coordinates to grid coordinates
            int gridRow = row - _layout.GridRegion.RowStart;
            int gridCol = col - _layout.GridRegion.ColStart;

            if (gridRow < 0 || gridRow >= _gridSize || gridCol < 0 || gridCol >= _gridSize)
            {
                return;
            }

            // Check if we have a word selected with content
            if (_selectedWordIndex < 0 || string.IsNullOrEmpty(_words[_selectedWordIndex]))
            {
                OnValidationError?.Invoke("Select a word first, then place it on the grid");
                return;
            }

            // If no anchor set, this click sets the anchor
            if (_anchorRow < 0)
            {
                SetAnchor(gridRow, gridCol);
            }
            // If anchor is set but direction not determined, this click determines direction
            else if (_direction == PlacementDirection.None)
            {
                DetermineDirection(gridRow, gridCol);
            }
            // If both set, this click confirms or cancels
            else
            {
                // If clicked same cell, cancel placement
                if (gridRow == _anchorRow && gridCol == _anchorCol)
                {
                    CancelCurrentPlacement();
                }
                else
                {
                    // Try to confirm placement at this position
                    ConfirmPlacement(gridRow, gridCol);
                }
            }
        }

        private void SetAnchor(int gridRow, int gridCol)
        {
            _anchorRow = gridRow;
            _anchorCol = gridCol;

            // Show anchor visual
            int tableRow = _layout.GridRegion.RowStart + gridRow;
            int tableCol = _layout.GridRegion.ColStart + gridCol;
            _model.SetCellState(tableRow, tableCol, TableCellState.PlacementAnchor);

            Debug.Log($"[WordPlacement] Anchor set at ({gridRow}, {gridCol})");
        }

        private void DetermineDirection(int gridRow, int gridCol)
        {
            string word = _words[_selectedWordIndex];

            // Determine direction based on second click
            if (gridRow == _anchorRow && gridCol != _anchorCol)
            {
                _direction = PlacementDirection.Horizontal;
            }
            else if (gridCol == _anchorCol && gridRow != _anchorRow)
            {
                _direction = PlacementDirection.Vertical;
            }
            else
            {
                // Diagonal not allowed
                OnValidationError?.Invoke("Words must be placed horizontally or vertically");
                return;
            }

            // Try to show placement preview
            if (CanPlaceWord(word, _anchorRow, _anchorCol, _direction))
            {
                ShowPlacementPreview(word, _anchorRow, _anchorCol, _direction);
            }
            else
            {
                OnValidationError?.Invoke("Cannot place word here - check boundaries and overlaps");
                CancelCurrentPlacement();
            }
        }

        private void ConfirmPlacement(int gridRow, int gridCol)
        {
            string word = _words[_selectedWordIndex];

            // Validate placement
            if (!CanPlaceWord(word, _anchorRow, _anchorCol, _direction))
            {
                OnValidationError?.Invoke("Invalid placement");
                CancelCurrentPlacement();
                return;
            }

            // Commit placement
            PlaceWord(_selectedWordIndex, word, _anchorRow, _anchorCol, _direction);

            // Clear interaction state
            _anchorRow = -1;
            _anchorCol = -1;
            _direction = PlacementDirection.None;

            // Check if all words are placed
            if (AreAllWordsPlaced())
            {
                OnAllWordsPlaced?.Invoke();
            }
        }

        private void CancelCurrentPlacement()
        {
            // Clear preview
            ClearPlacementPreview();

            _anchorRow = -1;
            _anchorCol = -1;
            _direction = PlacementDirection.None;

            Debug.Log("[WordPlacement] Placement cancelled");
        }

        // === Placement Logic ===

        private bool CanPlaceWord(string word, int startRow, int startCol, PlacementDirection direction)
        {
            if (string.IsNullOrEmpty(word))
            {
                return false;
            }

            int length = word.Length;

            // Check boundaries
            if (direction == PlacementDirection.Horizontal)
            {
                if (startCol + length > _gridSize)
                {
                    return false;
                }
            }
            else if (direction == PlacementDirection.Vertical)
            {
                if (startRow + length > _gridSize)
                {
                    return false;
                }
            }
            else
            {
                return false;
            }

            // Check for conflicts with other placed words
            for (int i = 0; i < length; i++)
            {
                int row = direction == PlacementDirection.Vertical ? startRow + i : startRow;
                int col = direction == PlacementDirection.Horizontal ? startCol + i : startCol;

                // Check if cell is already occupied by a different word
                for (int w = 0; w < _wordCount; w++)
                {
                    if (w == _selectedWordIndex || !_placements[w].IsPlaced)
                    {
                        continue;
                    }

                    if (IsCellOccupiedByWord(row, col, w))
                    {
                        // Check if the letters match (intersection allowed if same letter)
                        char existingLetter = GetLetterAtPosition(row, col, w);
                        if (existingLetter != word[i])
                        {
                            return false;
                        }
                    }
                }
            }

            return true;
        }

        private bool IsCellOccupiedByWord(int gridRow, int gridCol, int wordIndex)
        {
            WordPlacement placement = _placements[wordIndex];
            if (!placement.IsPlaced)
            {
                return false;
            }

            int length = placement.Word.Length;
            for (int i = 0; i < length; i++)
            {
                int row = placement.Direction == PlacementDirection.Vertical ? placement.StartRow + i : placement.StartRow;
                int col = placement.Direction == PlacementDirection.Horizontal ? placement.StartCol + i : placement.StartCol;

                if (row == gridRow && col == gridCol)
                {
                    return true;
                }
            }

            return false;
        }

        private char GetLetterAtPosition(int gridRow, int gridCol, int wordIndex)
        {
            WordPlacement placement = _placements[wordIndex];
            if (!placement.IsPlaced)
            {
                return '\0';
            }

            int length = placement.Word.Length;
            for (int i = 0; i < length; i++)
            {
                int row = placement.Direction == PlacementDirection.Vertical ? placement.StartRow + i : placement.StartRow;
                int col = placement.Direction == PlacementDirection.Horizontal ? placement.StartCol + i : placement.StartCol;

                if (row == gridRow && col == gridCol)
                {
                    return placement.Word[i];
                }
            }

            return '\0';
        }

        private void PlaceWord(int wordIndex, string word, int startRow, int startCol, PlacementDirection direction)
        {
            // Store placement
            _placements[wordIndex] = new WordPlacement
            {
                Word = word,
                StartRow = startRow,
                StartCol = startCol,
                Direction = direction,
                IsPlaced = true
            };

            // Update grid cells
            int length = word.Length;
            for (int i = 0; i < length; i++)
            {
                int gridRow = direction == PlacementDirection.Vertical ? startRow + i : startRow;
                int gridCol = direction == PlacementDirection.Horizontal ? startCol + i : startCol;

                int tableRow = _layout.GridRegion.RowStart + gridRow;
                int tableCol = _layout.GridRegion.ColStart + gridCol;

                _model.SetCellChar(tableRow, tableCol, word[i]);
                _model.SetCellState(tableRow, tableCol, TableCellState.PlacementValid);
                _model.SetCellOwner(tableRow, tableCol, CellOwner.Player1);
            }

            OnWordPlaced?.Invoke(wordIndex, word);
            Debug.Log($"[WordPlacement] Placed word {wordIndex + 1}: {word} at ({startRow}, {startCol}) {direction}");
        }

        /// <summary>
        /// Clears a word's placement from the grid.
        /// </summary>
        public void ClearPlacement(int wordIndex)
        {
            if (wordIndex < 0 || wordIndex >= _wordCount)
            {
                return;
            }

            WordPlacement placement = _placements[wordIndex];
            if (!placement.IsPlaced)
            {
                return;
            }

            // Clear grid cells
            int length = placement.Word.Length;
            for (int i = 0; i < length; i++)
            {
                int gridRow = placement.Direction == PlacementDirection.Vertical ? placement.StartRow + i : placement.StartRow;
                int gridCol = placement.Direction == PlacementDirection.Horizontal ? placement.StartCol + i : placement.StartCol;

                int tableRow = _layout.GridRegion.RowStart + gridRow;
                int tableCol = _layout.GridRegion.ColStart + gridCol;

                // Only clear if not occupied by another word
                bool occupiedByOther = false;
                for (int w = 0; w < _wordCount; w++)
                {
                    if (w != wordIndex && IsCellOccupiedByWord(gridRow, gridCol, w))
                    {
                        occupiedByOther = true;
                        break;
                    }
                }

                if (!occupiedByOther)
                {
                    _model.SetCellChar(tableRow, tableCol, '\0');
                    _model.SetCellState(tableRow, tableCol, TableCellState.Fog);
                    _model.SetCellOwner(tableRow, tableCol, CellOwner.None);
                }
            }

            // Clear placement data
            _placements[wordIndex] = new WordPlacement
            {
                Word = _words[wordIndex],
                StartRow = -1,
                StartCol = -1,
                Direction = PlacementDirection.None,
                IsPlaced = false
            };

            OnWordCleared?.Invoke(wordIndex);
            Debug.Log($"[WordPlacement] Cleared placement for word {wordIndex + 1}");
        }

        /// <summary>
        /// Clears all word placements.
        /// </summary>
        public void ClearAllPlacements()
        {
            for (int i = 0; i < _wordCount; i++)
            {
                ClearPlacement(i);
            }
        }

        // === Preview ===

        private void ShowPlacementPreview(string word, int startRow, int startCol, PlacementDirection direction)
        {
            int length = word.Length;
            for (int i = 0; i < length; i++)
            {
                int gridRow = direction == PlacementDirection.Vertical ? startRow + i : startRow;
                int gridCol = direction == PlacementDirection.Horizontal ? startCol + i : startCol;

                int tableRow = _layout.GridRegion.RowStart + gridRow;
                int tableCol = _layout.GridRegion.ColStart + gridCol;

                if (i == 0)
                {
                    _model.SetCellState(tableRow, tableCol, TableCellState.PlacementAnchor);
                }
                else if (i == length - 1)
                {
                    _model.SetCellState(tableRow, tableCol, TableCellState.PlacementSecond);
                }
                else
                {
                    _model.SetCellState(tableRow, tableCol, TableCellState.PlacementPath);
                }

                _model.SetCellChar(tableRow, tableCol, word[i]);
            }
        }

        private void ClearPlacementPreview()
        {
            // Reset all grid cells that were in preview state
            for (int gridRow = 0; gridRow < _gridSize; gridRow++)
            {
                for (int gridCol = 0; gridCol < _gridSize; gridCol++)
                {
                    int tableRow = _layout.GridRegion.RowStart + gridRow;
                    int tableCol = _layout.GridRegion.ColStart + gridCol;

                    TableCell cell = _model.GetCell(tableRow, tableCol);
                    if (cell.State == TableCellState.PlacementAnchor ||
                        cell.State == TableCellState.PlacementPath ||
                        cell.State == TableCellState.PlacementSecond)
                    {
                        // Check if this cell is part of a confirmed placement
                        bool isPlaced = false;
                        for (int w = 0; w < _wordCount; w++)
                        {
                            if (_placements[w].IsPlaced && IsCellOccupiedByWord(gridRow, gridCol, w))
                            {
                                isPlaced = true;
                                break;
                            }
                        }

                        if (!isPlaced)
                        {
                            _model.SetCellChar(tableRow, tableCol, '\0');
                            _model.SetCellState(tableRow, tableCol, TableCellState.Fog);
                        }
                    }
                }
            }
        }

        // === Visual Updates ===

        private void UpdateWordSlotDisplay(int wordIndex)
        {
            string word = _words[wordIndex];
            for (int i = 0; i < _gridSize; i++)
            {
                char letter = i < word.Length ? word[i] : '\0';
                _model.SetWordSlotLetter(wordIndex, i, letter);
            }
        }

        private void UpdateWordSlotSelectionVisuals()
        {
            // Update word slot visual states to show which is selected
            for (int w = 0; w < _wordCount; w++)
            {
                TableCellState state = (w == _selectedWordIndex) ? TableCellState.Selected : TableCellState.Normal;
                for (int i = 0; i < _gridSize; i++)
                {
                    int tableRow = _layout.WordRowsRegion.RowStart + w;
                    int tableCol = _layout.WordRowsRegion.ColStart + i;
                    _model.SetCellState(tableRow, tableCol, state);
                }
            }
        }

        // === Queries ===

        /// <summary>
        /// Returns true if all words have been placed on the grid.
        /// </summary>
        public bool AreAllWordsPlaced()
        {
            for (int i = 0; i < _wordCount; i++)
            {
                if (string.IsNullOrEmpty(_words[i]) || !_placements[i].IsPlaced)
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Returns true if all word slots have content (but may not be placed).
        /// </summary>
        public bool AreAllWordsEntered()
        {
            for (int i = 0; i < _wordCount; i++)
            {
                if (string.IsNullOrEmpty(_words[i]))
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Gets the current placements.
        /// </summary>
        public WordPlacement[] GetPlacements()
        {
            return _placements;
        }

        /// <summary>
        /// Gets the words array.
        /// </summary>
        public string[] GetWords()
        {
            return _words;
        }
    }
}

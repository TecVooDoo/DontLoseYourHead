using System;
using System.Collections.Generic;
using UnityEngine;

namespace TecVooDoo.DontLoseYourHead.UI
{
    /// <summary>
    /// Interface for grid display operations (cell creation, sizing, labels).
    /// </summary>
    public interface IGridDisplayController
    {
        int CurrentGridSize { get; }
        bool IsInitialized { get; }
        
        void Initialize(int gridSize);
        void SetGridSize(int newSize);
        void ClearGrid();
        GridCellUI GetCell(int column, int row);
        bool IsValidCoordinate(int column, int row);
        char GetColumnLetter(int column);
        
        event Action<int, int> OnCellClicked;
        event Action<int, int> OnCellHoverEnter;
        event Action<int, int> OnCellHoverExit;
    }

    /// <summary>
    /// Interface for letter tracker/keyboard operations.
    /// </summary>
    public interface ILetterTrackerController
    {
        void CacheLetterButtons();
        LetterButton GetLetterButton(char letter);
        void SetLetterState(char letter, LetterButton.LetterState state);
        void ResetAllLetterButtons();
        void SetLetterButtonsInteractable(bool interactable);
        
        event Action<char> OnLetterClicked;
        event Action<char> OnLetterHoverEnter;
        event Action<char> OnLetterHoverExit;
    }

    /// <summary>
    /// Interface for word pattern row management.
    /// </summary>
    public interface IWordPatternController
    {
        int WordRowCount { get; }
        int SelectedWordRowIndex { get; }
        
        void CacheWordPatternRows();
        WordPatternRow GetWordPatternRow(int index);
        WordPatternRow[] GetWordPatternRows();
        void SelectWordRow(int index);
        bool AddLetterToSelectedRow(char letter);
        bool RemoveLastLetterFromSelectedRow();
        void SetWordLengths(int[] lengths);
        void SetWordValidator(Func<string, int, bool> validator);
        bool AreAllWordsPlaced();
        bool ClearPlacedWord(int rowIndex);
        
        event Action<int> OnWordRowSelected;
        event Action<int> OnCoordinateModeRequested;
        event Action<int, string, List<Vector2Int>> OnWordPlaced;
    }

    /// <summary>
    /// Interface for coordinate placement mode operations.
    /// </summary>
    public interface ICoordinatePlacementController
    {
        bool IsInPlacementMode { get; }
        PlacementState CurrentPlacementState { get; }
        
        void EnterPlacementMode(int wordRowIndex, string word);
        void CancelPlacementMode();
        bool PlaceWordRandomly();
        void UpdatePlacementPreview(int hoverCol, int hoverRow);
        void ClearPlacementHighlighting();
        bool HandleCellClick(int column, int row);
        
        event Action OnPlacementCancelled;
        event Action<int, string, List<Vector2Int>> OnWordPlaced;
    }

    /// <summary>
    /// Interface for grid color/highlighting operations.
    /// </summary>
    public interface IGridColorManager
    {
        Color CursorColor { get; set; }
        Color ValidPlacementColor { get; set; }
        Color InvalidPlacementColor { get; set; }
        Color PlacedLetterColor { get; set; }
        
        void SetCellHighlight(GridCellUI cell, GridHighlightType highlightType);
        void ClearCellHighlight(GridCellUI cell);
        void ClearAllHighlights(IGridDisplayController gridDisplay);
    }

    /// <summary>
    /// Types of cell highlighting during placement mode.
    /// </summary>
    public enum GridHighlightType
    {
        None,
        Cursor,
        ValidPlacement,
        InvalidPlacement,
        PlacedLetter
    }

    /// <summary>
    /// State of coordinate placement mode.
    /// </summary>
    public enum PlacementState
    {
        Inactive,
        SelectingFirstCell,
        SelectingDirection
    }
}

# PlayerGridPanel Refactoring Analysis

**File:** `Assets/DLYH/Scripts/UI/PlayerGridPanel.cs`  
**Current Lines:** 2,251  
**Target Lines:** ~300-400 (main panel)  
**Analysis Date:** December 11, 2025  
**Analyzer:** Claude + Rune  

---

## Executive Summary

PlayerGridPanel is currently a "God Object" that manages 13+ distinct responsibilities across 2,251 lines. This document identifies each responsibility, its line count, dependencies, and recommended extraction target.

**Priority Extractions (by impact):**

| Priority | Extraction | Lines | Reason |
|----------|-----------|-------|--------|
| HIGH | CoordinatePlacementController | ~400 | Largest single responsibility, complex state machine |
| HIGH | WordPatternRowManager | ~300 | High coupling, manages row selection and input |
| MEDIUM | GridLayoutManager | ~350 | Complex layout calculations, Unity-specific |
| MEDIUM | PlacementPreviewController | ~150 | Isolated responsibility, easy extraction |
| LOW | GridLabelManager | ~200 | Self-contained label caching/visibility |
| LOW | GridCellManager | ~200 | Cell creation/access/clearing |

---

## Current Structure Overview

```
PlayerGridPanel (2,251 lines)
    |
    +-- PanelMode enum (Setup/Gameplay)
    |
    +-- Serialized Fields (~87 lines)
    |   +-- Prefab Reference
    |   +-- Container References  
    |   +-- Display References
    |   +-- Layout References
    |   +-- Configuration
    |   +-- Placement Colors
    |
    +-- Private Fields (~70 lines)
    |   +-- Grid cells array
    |   +-- Label objects arrays
    |   +-- Layout references
    |   +-- Word pattern tracking
    |   +-- Placement state tracking
    |
    +-- Events (~65 lines)
    |   +-- Grid events (OnCellClicked, OnCellHoverEnter/Exit)
    |   +-- Letter tracker events (OnLetterClicked, OnLetterHoverEnter/Exit)
    |   +-- Word pattern events (OnWordRowSelected, OnCoordinateModeRequested, etc.)
    |
    +-- Properties (~20 lines)
    |
    +-- 13 Responsibility Groups (detailed below)
```

---

## Responsibility Analysis

### 1. Coordinate Placement System (~400 lines)

**Location:** Lines 939-1041, 1628-1940, 1942-2010 (event handlers)

**Fields:**
```csharp
private PlacementState _placementState = PlacementState.Inactive;
private int _placementWordRowIndex = -1;
private string _placementWord = "";
private int _firstCellCol = -1;
private int _firstCellRow = -1;
private List<Vector2Int> _placedCellPositions = new List<Vector2Int>();
private HashSet<Vector2Int> _allPlacedPositions = new HashSet<Vector2Int>();
private Dictionary<Vector2Int, char> _placedLetters = new Dictionary<Vector2Int, char>();
private Dictionary<int, List<Vector2Int>> _wordRowPositions = new Dictionary<int, List<Vector2Int>>();
```

**Methods:**
- `EnterPlacementMode(int wordRowIndex)` - 25 lines
- `CancelPlacementMode()` - 35 lines
- `PlaceWordRandomly()` - 30 lines
- `PlaceAllWordsRandomly()` - 5 lines
- `GetAllValidPlacements()` - 30 lines
- `IsValidPlacement(...)` - 25 lines
- `GetValidDirectionsFromCell(...)` - 20 lines
- `IsValidDirectionCell(...)` - 5 lines
- `PlaceWordInDirection(...)` - 50 lines
- `ClearWordFromGrid(int rowIndex)` - 35 lines
- `ClearPlacedWord(int rowIndex)` - 70 lines
- `ClearSelectedPlacedWord()` - 5 lines
- `ClearAllPlacedWords()` - 25 lines
- `HandleRandomPlacementClick()` - 55 lines
- `ShuffleList<T>(...)` - 10 lines

**Events Used:**
- `OnWordPlaced`
- `OnPlacementCancelled`

**Dependencies:**
- GridColorManager (for highlighting)
- WordPatternRow (for word data)
- GridCellUI (for placement)

**Extraction Target:** `CoordinatePlacementController`

---

### 2. Word Pattern Row Management (~300 lines)

**Location:** Lines 563-920, 2043-2074

**Fields:**
```csharp
private List<WordPatternRow> _wordPatternRows = new List<WordPatternRow>();
private int _selectedWordRowIndex = -1;
private Func<string, int, bool> _wordValidator;
private AutocompleteDropdown _autocompleteDropdown; // serialized
```

**Methods:**
- `CacheWordPatternRows()` - 40 lines
- `GetWordPatternRow(int index)` - 7 lines
- `GetWordPatternRows()` - 15 lines
- `GetAllWordPlacements()` - 45 lines
- `SelectWordRow(int index)` - 30 lines
- `AddLetterToSelectedRow(char letter)` - 15 lines
- `RemoveLastLetterFromSelectedRow()` - 15 lines
- `SetWordValidator(Func<...>)` - 15 lines
- `SetWordLengths(int[] lengths)` - 30 lines
- `AreAllWordsPlaced()` - 10 lines
- `HandleWordRowSelected(int rowNumber)` - 5 lines
- `HandleCoordinateModeClicked(int rowNumber)` - 10 lines
- `HandleDeleteClicked(int rowNumber, bool wasPlaced)` - 15 lines

**Events Used:**
- `OnWordRowSelected`
- `OnCoordinateModeRequested`
- `OnWordLengthsChanged`
- `OnInvalidWordRejected`

**Dependencies:**
- WordPatternRow (managed objects)
- AutocompleteDropdown
- CoordinatePlacementController (triggers placement mode)

**Extraction Target:** `WordPatternRowManager`

---

### 3. Grid Layout Management (~350 lines)

**Location:** Lines 1266-1489

**Fields:**
```csharp
private GridLayoutGroup _gridLayoutGroup;
private RectTransform _panelRectTransform;
private LayoutElement _panelLayoutElement;
private float _currentCellSize = 40f;

// Constants
private const float MAX_CELL_SIZE = 40f;
private const float MIN_CELL_SIZE = 25f;
private const float CELL_SPACING = 2f;
private const float ROW_LABEL_HEIGHT = 40f;
private const float ROW_LABEL_SPACING = 2f;
```

**Serialized Fields:**
```csharp
private RectTransform _gridWithRowLabelsRect;
private LayoutElement _gridContainerLayout;
private LayoutElement _rowLabelsLayout;
private float _fixedElementsHeight = 300f;
```

**Methods:**
- `UpdateGridLayoutConstraint()` - 8 lines
- `UpdatePanelHeight()` - 120 lines (largest method!)
- `CreateCellsForCurrentSize()` - 15 lines
- `CreateCell(int column, int row)` - 20 lines

**Dependencies:**
- GridCellUI (prefab instantiation)
- Unity Layout system

**Extraction Target:** `GridLayoutManager`

---

### 4. Placement Preview System (~150 lines)

**Location:** Lines 1729-1875

**Methods:**
- `UpdatePlacementPreview(int hoverCol, int hoverRow)` - 55 lines
- `HighlightInvalidCells(...)` - 25 lines
- `PreviewWordPlacement(int secondCol, int secondRow)` - 30 lines
- `ClearPlacementHighlighting()` - 30 lines

**Dependencies:**
- GridColorManager
- GridCellUI
- Placement state fields

**Extraction Target:** `PlacementPreviewController`

---

### 5. Grid Label Management (~200 lines)

**Location:** Lines 1163-1263, 1493-1589

**Fields:**
```csharp
private GameObject[] _rowLabelObjects = new GameObject[MAX_GRID_SIZE];
private GameObject[] _columnLabelObjects = new GameObject[MAX_GRID_SIZE];
```

**Serialized Fields:**
```csharp
private Transform _rowLabelsContainer;
private Transform _columnLabelsContainer;
```

**Methods:**
- `CacheExistingLabels()` - 100 lines
- `UpdateLabelVisibility()` - 95 lines

**Dependencies:**
- Unity UI layout system
- Grid size settings

**Extraction Target:** `GridLabelManager`

---

### 6. Grid Cell Management (~200 lines)

**Location:** Lines 306-365, 1054-1114, 1591-1625

**Fields:**
```csharp
private GridCellUI[,] _cells = new GridCellUI[MAX_GRID_SIZE, MAX_GRID_SIZE];
public const int MAX_GRID_SIZE = 12;
public const int MIN_GRID_SIZE = 6;
```

**Methods:**
- `InitializeGrid()` - 5 lines
- `InitializeGrid(int gridSize)` - 45 lines
- `SetGridSize(int newSize)` - 20 lines
- `GetCell(int column, int row)` - 8 lines
- `IsValidCoordinate(int column, int row)` - 3 lines
- `GetColumnLetter(int column)` - 3 lines
- `ClearGrid()` - 30 lines
- `CreateCellsForCurrentSize()` - 15 lines (shared with layout)
- `CreateCell(int column, int row)` - 20 lines (shared with layout)

**Dependencies:**
- GridCellUI (prefab, instances)
- GridLayoutManager (for cell sizing)

**Extraction Target:** `GridCellManager`

---

### 7. Letter Tracker Integration (~100 lines)

**Location:** Lines 500-560, 2010-2041

**Status:** ALREADY EXTRACTED to `LetterTrackerController`

**Wrapper Methods (keep in panel):**
- `GetLetterButton(char letter)` - 5 lines
- `SetLetterState(char letter, LetterState state)` - 5 lines
- `GetLetterState(char letter)` - 5 lines
- `ResetAllLetterButtons()` - 5 lines
- `SetLetterButtonsInteractable(bool interactable)` - 5 lines
- `CacheLetterButtons()` - 10 lines

**Event Handlers:**
- `HandleLetterClicked(char letter)` - 20 lines
- `HandleLetterHoverEnter(char letter)` - 3 lines
- `HandleLetterHoverExit(char letter)` - 3 lines

---

### 8. Grid Color Manager Integration (~30 lines)

**Location:** Lines 248-254

**Status:** ALREADY EXTRACTED to `GridColorManager`

The panel creates the manager in Start() and uses it for placement highlighting.

---

### 9. Player Display (~50 lines)

**Location:** Lines 393-428

**Methods:**
- `SetPlayerName(string name)` - 8 lines
- `SetPlayerColor(Color color)` - 5 lines
- `UpdatePlayerColorVisuals()` - 15 lines

**Decision:** Keep in panel (too small to extract)

---

### 10. Mode Management (~50 lines)

**Location:** Lines 277-304

**Methods:**
- `SetMode(PanelMode mode)` - 15 lines
- `UpdateModeVisuals()` - 10 lines

**Decision:** Keep in panel (too small to extract)

---

### 11. Caching (~50 lines)

**Location:** Lines 1117-1161

**Methods:**
- `CachePanelReferences()` - 30 lines
- `CacheGridLayoutGroup()` - 10 lines

**Decision:** Keep in panel (initialization logic)

---

### 12. Editor Helpers (~70 lines)

**Location:** Lines 2181-2248

**Methods:**
- `EditorClearGrid()` - 5 lines
- `LogCellCount()` - 15 lines
- `LogLabelStatus()` - 30 lines
- `TestSetupMode()` - 5 lines
- `TestGameplayMode()` - 5 lines

**Decision:** Keep in panel (debug only, #if UNITY_EDITOR)

---

### 13. Unity Lifecycle (~35 lines)

**Location:** Lines 236-274

**Methods:**
- `Awake()` - 5 lines
- `Start()` - 25 lines

**Decision:** Keep in panel (entry points)

---

## Dependency Graph

```
PlayerGridPanel
    |
    +-- GridCellManager (NEW)
    |       |-- GridCellUI[]
    |       |-- Cell creation/destruction
    |
    +-- GridLayoutManager (NEW)
    |       |-- GridLayoutGroup
    |       |-- Cell sizing
    |       |-- GridLabelManager (nested or separate)
    |
    +-- WordPatternRowManager (NEW)
    |       |-- WordPatternRow[]
    |       |-- Selection state
    |       |-- AutocompleteDropdown
    |
    +-- CoordinatePlacementController (NEW)
    |       |-- PlacementState
    |       |-- Position tracking
    |       |-- PlacementPreviewController (nested)
    |       |-- Uses: GridCellManager, GridColorManager
    |
    +-- LetterTrackerController (EXISTS)
    |       |-- LetterButton[]
    |
    +-- GridColorManager (EXISTS)
            |-- Highlight colors
```

---

## Extraction Order (Recommended)

### Phase 1: Isolated Systems (Low Risk)

1. **PlacementPreviewController** (~150 lines)
   - Most isolated
   - Only depends on GridColorManager
   - Low coupling

2. **GridLabelManager** (~200 lines)
   - Self-contained
   - No dependencies on other extractions
   - Pure Unity layout code

### Phase 2: Medium Coupling

3. **GridCellManager** (~200 lines)
   - Manages cell array
   - Provides GetCell, IsValidCoordinate
   - Used by placement and preview

4. **GridLayoutManager** (~350 lines)
   - Can include GridLabelManager
   - Handles all sizing calculations
   - One of the largest responsibilities

### Phase 3: High Coupling

5. **WordPatternRowManager** (~300 lines)
   - Manages row selection
   - Coordinates with placement
   - Complex event wiring

6. **CoordinatePlacementController** (~400 lines)
   - Largest extraction
   - State machine
   - Uses multiple other managers
   - Extract LAST because it depends on others

---

## Estimated Final Structure

After refactoring:

```
PlayerGridPanel.cs (~300 lines)
    |-- Mode management
    |-- Player display
    |-- Initialization/Lifecycle
    |-- Event forwarding
    |-- Manager coordination

GridCellManager.cs (~200 lines)
    |-- Cell array management
    |-- GetCell, IsValidCoordinate
    |-- CreateCell, ClearGrid

GridLayoutManager.cs (~350 lines)
    |-- UpdatePanelHeight
    |-- Cell sizing
    |-- Layout group configuration

GridLabelManager.cs (~150 lines)
    |-- CacheExistingLabels
    |-- UpdateLabelVisibility

WordPatternRowManager.cs (~250 lines)
    |-- CacheWordPatternRows
    |-- Row selection
    |-- Letter input routing

CoordinatePlacementController.cs (~350 lines)
    |-- Placement state machine
    |-- Validation logic
    |-- Position tracking

PlacementPreviewController.cs (~120 lines)
    |-- UpdatePlacementPreview
    |-- Highlight management
```

---

## Interface Definitions (Proposed)

### IGridCellProvider

```csharp
public interface IGridCellProvider
{
    GridCellUI GetCell(int column, int row);
    bool IsValidCoordinate(int column, int row);
    int CurrentGridSize { get; }
    char GetColumnLetter(int column);
}
```

### IPlacementTarget

```csharp
public interface IPlacementTarget
{
    void SetCellLetter(int col, int row, char letter);
    void SetCellState(int col, int row, CellState state);
    char? GetPlacedLetter(int col, int row);
    bool IsPositionOccupied(int col, int row);
}
```

### IWordRowProvider

```csharp
public interface IWordRowProvider
{
    WordPatternRow GetWordPatternRow(int index);
    int SelectedRowIndex { get; }
    string GetWordForRow(int index);
    bool IsRowPlaced(int index);
}
```

---

## Memory Efficiency Concerns

### Current Issues Found

1. **LINQ in Hot Paths** (Line 896, 1208, 1251)
   ```csharp
   var sortedRows = rows.OrderBy(r => r.transform.GetSiblingIndex()).ToArray();
   int rowCount = _rowLabelObjects.Count(x => x != null);
   ```
   - These run during caching, not every frame - LOW priority

2. **No Object Pooling**
   - Cells are created/destroyed, not pooled
   - For grid resize operations, pooling would help

3. **String Allocations in Debug Logs**
   - Many `Debug.Log($"...")` calls
   - Consider `#if UNITY_EDITOR` guards or log levels

4. **GetComponent in UpdatePanelHeight**
   - Multiple `GetComponent<LayoutElement>()` calls
   - Should cache these references

---

## Next Steps

1. **Verify compilation** of current file in Unity
2. **Create test scene** to verify functionality before refactoring
3. **Extract PlacementPreviewController** (safest first extraction)
4. **Verify functionality** after each extraction
5. **Continue with extraction order** as specified above

---

## Questions for Rune

Before proceeding with extractions:

1. Should GridLabelManager be nested inside GridLayoutManager or separate?
2. Preferred event pattern for manager-to-panel communication?
3. Any functionality that should remain in the main panel for access reasons?
4. Priority between line count reduction vs. clean architecture?

---

**End of Analysis Document**

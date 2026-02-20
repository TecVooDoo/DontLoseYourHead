# PlayerGridPanel Refactoring Analysis

**File:** `Assets/DLYH/Scripts/UI/PlayerGridPanel.cs`  
**Current Lines:** ~2,191 (down from 2,251)  
**Target Lines:** ~300-400 (main panel)  
**Analysis Date:** December 11, 2025  
**Last Updated:** December 12, 2025  
**Analyzer:** Claude + Rune  

---

## Progress Status

| Extraction | Status | Lines | File Location |
|------------|--------|-------|---------------|
| LetterTrackerController | COMPLETE | ~150 | Controllers/LetterTrackerController.cs |
| GridColorManager | COMPLETE | ~50 | Controllers/GridColorManager.cs |
| PlacementPreviewController | COMPLETE | ~50 | Controllers/PlacementPreviewController.cs |
| WordPatternRowManager | COMPLETE | ~400 | Controllers/WordPatternRowManager.cs |
| CoordinatePlacementController | PENDING | ~350 | Next extraction |
| GridLayoutManager | PENDING | ~350 | After CoordinatePlacementController |
| GridLabelManager | PENDING | ~150 | Can merge with GridLayoutManager |
| GridCellManager | PENDING | ~200 | Lower priority |

**Estimated lines removed from PlayerGridPanel:** ~200 (via delegation, not full removal yet)  
**Next session should start with:** CoordinatePlacementController extraction

---

## Executive Summary

PlayerGridPanel is currently a "God Object" that manages 13+ distinct responsibilities across ~2,191 lines. This document identifies each responsibility, its line count, dependencies, and recommended extraction target.

**Priority Extractions (by impact):**

| Priority | Extraction | Lines | Status | Reason |
|----------|-----------|-------|--------|--------|
| HIGH | CoordinatePlacementController | ~350 | PENDING | Largest single responsibility, complex state machine |
| HIGH | WordPatternRowManager | ~400 | COMPLETE | High coupling, manages row selection and input |
| MEDIUM | GridLayoutManager | ~350 | PENDING | Complex layout calculations, Unity-specific |
| MEDIUM | PlacementPreviewController | ~50 | COMPLETE | Isolated responsibility, easy extraction |
| LOW | GridLabelManager | ~150 | PENDING | Self-contained label caching/visibility |
| LOW | GridCellManager | ~200 | PENDING | Cell creation/access/clearing |

---

## Current Structure Overview

```
PlayerGridPanel (~2,191 lines)
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
    +-- Private Fields (~75 lines)
    |   +-- Grid cells array
    |   +-- Label objects arrays
    |   +-- Layout references
    |   +-- Word pattern tracking
    |   +-- Placement state tracking
    |   +-- Controller references (NEW)
    |
    +-- Events (~65 lines)
    |   +-- Grid events (OnCellClicked, OnCellHoverEnter/Exit)
    |   +-- Letter tracker events (OnLetterClicked, OnLetterHoverEnter/Exit)
    |   +-- Word pattern events (OnWordRowSelected, OnCoordinateModeRequested, etc.)
    |
    +-- Properties (~20 lines)
    |
    +-- Integrated Controllers
    |   +-- LetterTrackerController (COMPLETE)
    |   +-- GridColorManager (COMPLETE)
    |   +-- PlacementPreviewController (COMPLETE)
    |   +-- WordPatternRowManager (COMPLETE)
    |
    +-- Remaining Responsibilities (to extract)
        +-- Coordinate Placement System (~350 lines) - NEXT
        +-- Grid Layout Management (~350 lines)
        +-- Grid Label Management (~150 lines)
        +-- Grid Cell Management (~200 lines)
```

---

## Completed Extractions

### LetterTrackerController (COMPLETE)

**File:** `Assets/DLYH/Scripts/UI/Controllers/LetterTrackerController.cs`  
**Lines:** ~150

Manages letter button caching, click handling, and state management. Integrated via events.

### GridColorManager (COMPLETE)

**File:** `Assets/DLYH/Scripts/UI/Controllers/GridColorManager.cs`  
**Lines:** ~50

Provides color values for grid cell states (cursor, valid, invalid, placed). Pure data class.

### PlacementPreviewController (COMPLETE)

**File:** `Assets/DLYH/Scripts/UI/Controllers/PlacementPreviewController.cs`  
**Lines:** ~50

Handles `ClearPlacementHighlighting()` - clearing visual feedback from grid cells during placement mode.

### WordPatternRowManager (COMPLETE)

**File:** `Assets/DLYH/Scripts/UI/Controllers/WordPatternRowManager.cs`  
**Lines:** ~400

Manages word pattern row collection, caching, selection, letter input routing, and event handling. Integrated via 4 events:
- OnWordRowSelected
- OnCoordinateModeRequested
- OnDeleteClicked
- OnWordLengthsChanged

---

## Pending Extractions

### 1. CoordinatePlacementController (~350 lines) - NEXT

**Priority:** HIGH  
**Reason:** Largest remaining responsibility, complex state machine

**Fields to extract:**
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

**Methods to extract:**
- `EnterPlacementMode(int wordRowIndex)`
- `CancelPlacementMode()`
- `PlaceWordRandomly()`
- `GetAllValidPlacements()`
- `IsValidPlacement(...)`
- `GetValidDirectionsFromCell(...)`
- `PlaceWordInDirection(...)`
- `ClearWordFromGrid(int rowIndex)`
- `ClearPlacedWord(int rowIndex)`
- `HandleRandomPlacementClick()`

**Dependencies:**
- GridColorManager (for highlighting)
- PlacementPreviewController (for preview)
- WordPatternRowManager (for word data)
- GridCellUI (for placement)

### 2. GridLayoutManager (~350 lines)

**Priority:** MEDIUM

**Methods to extract:**
- `UpdateGridLayoutConstraint()`
- `UpdatePanelHeight()` - largest method (~120 lines)
- `CreateCellsForCurrentSize()`
- `CreateCell(int column, int row)`

### 3. GridLabelManager (~150 lines)

**Priority:** LOW  
**Note:** Could be merged into GridLayoutManager

**Methods to extract:**
- `CacheExistingLabels()`
- `UpdateLabelVisibility()`

### 4. GridCellManager (~200 lines)

**Priority:** LOW

**Methods to extract:**
- `InitializeGrid()`
- `SetGridSize(int newSize)`
- `GetCell(int column, int row)`
- `IsValidCoordinate(int column, int row)`
- `ClearGrid()`

---

## Extraction Order (Updated)

### Phase 1: COMPLETE
1. LetterTrackerController - DONE
2. GridColorManager - DONE
3. PlacementPreviewController - DONE
4. WordPatternRowManager - DONE

### Phase 2: In Progress
5. **CoordinatePlacementController** - NEXT SESSION
   - Extract placement state machine
   - Extract validation logic
   - Extract position tracking

### Phase 3: Remaining
6. GridLayoutManager (~350 lines)
7. GridLabelManager (~150 lines) - may merge with above
8. GridCellManager (~200 lines)

---

## Estimated Final Structure

After all extractions:

```
PlayerGridPanel.cs (~300-400 lines)
    |-- Mode management
    |-- Player display
    |-- Initialization/Lifecycle
    |-- Event forwarding
    |-- Manager coordination

Controllers/
    |-- LetterTrackerController.cs (~150 lines) - COMPLETE
    |-- GridColorManager.cs (~50 lines) - COMPLETE
    |-- PlacementPreviewController.cs (~50 lines) - COMPLETE
    |-- WordPatternRowManager.cs (~400 lines) - COMPLETE
    |-- CoordinatePlacementController.cs (~350 lines) - PENDING
    |-- GridLayoutManager.cs (~350 lines) - PENDING
    |-- GridCellManager.cs (~200 lines) - PENDING
```

---

## Notes for Next Session

1. **Start with CoordinatePlacementController** - largest remaining extraction
2. **MCP stability concern** - Previous session had MCP instability during WordPatternRowManager integration. Consider providing complete file replacements if MCP becomes unstable.
3. **Test after each integration** - Verify word placement, compass buttons, and grid interaction still work
4. **Commit frequently** - Commit after each successful extraction

---

**End of Analysis Document**

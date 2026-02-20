# Don't Lose Your Head - Refactoring Instructions

**Version:** 1.4  
**Date Created:** December 11, 2025  
**Last Updated:** December 12, 2025 (v4 - PlayerGridPanel COMPLETE)  
**Developer:** TecVooDoo LLC  

---

## Current Progress

### PlayerGridPanel - COMPLETE

| Controller | Lines | Status |
|------------|-------|--------|
| LetterTrackerController | ~150 | COMPLETE |
| GridColorManager | ~50 | COMPLETE |
| PlacementPreviewController | ~50 | COMPLETE |
| WordPatternRowManager | ~400 | COMPLETE |
| CoordinatePlacementController | ~616 | COMPLETE |
| GridLayoutManager | ~593 | COMPLETE |

**PlayerGridPanel.cs:** 2,192 -> 1,120 lines (49% reduction) - **COMPLETE**

**Critical Bug Fixed:** Unity lifecycle timing issue where controllers were null when panels activated before `Start()` ran. Solution: Added `EnsureControllersInitialized()` pattern.

### Next Target: GameplayUIController

**Current Lines:** 2,112  
**Target Lines:** ~400-500  
**Analysis Document:** `DLYH_GameplayUIController_RefactoringAnalysis_12122025.md`

**Controller Files Location:** `Assets/DLYH/Scripts/UI/Controllers/`

---

## Purpose

This document defines the standards and goals for refactoring the DLYH codebase. Reference this document at the start of every refactoring session.

---

## Platform

**Unity Version:** 6.3 LTS  
**Approach:** Utilize Unity 6.3 capabilities fully. Use Unity types (Vector2Int, Color, etc.) where appropriate. Reference official Unity 6.3 documentation for current best practices.

---

## Critical Pattern: Defensive Controller Initialization

**Problem Discovered (Dec 12, 2025):**  
When GameObjects are activated and immediately configured, `Start()` hasn't run yet. Controllers initialized in `Start()` are null when methods are called from external code.

**Solution:** Add `EnsureControllersInitialized()` pattern:

```csharp
private bool _eventsWired;

private void Start()
{
    InitializeControllers();
    WireControllerEvents();
}

private void InitializeControllers()
{
    // Guard against double-initialization
    if (_gridCellManager != null) return;
    
    // Initialize all controllers
    _gridCellManager = new GridCellManager();
    // ... more controllers
}

private void EnsureControllersInitialized()
{
    // Called from any public method that might be invoked before Start()
    if (_gridCellManager != null) return;
    
    Debug.Log("[ClassName] EnsureControllersInitialized - initializing before Start()");
    
    // Same initialization as InitializeControllers()
    _gridCellManager = new GridCellManager();
    // ... more controllers
    
    WireControllerEventsIfNeeded();
}

private void WireControllerEvents()
{
    if (_eventsWired) return;
    // Subscribe to events
    _eventsWired = true;
}

private void WireControllerEventsIfNeeded()
{
    if (_eventsWired) return;
    if (_someController == null) return;  // Guard against partial init
    WireControllerEvents();
}

// PUBLIC METHOD - call EnsureControllersInitialized first
public void InitializeGrid(int gridSize)
{
    EnsureControllersInitialized();  // Safe to call before Start()
    // ... rest of method
}
```

**When to apply:** Any MonoBehaviour that:
- Has controllers initialized in `Start()`
- Has public methods called by other scripts
- Might be activated and configured in the same frame

---

## Primary Goals

### 1. Reduce Script Size for Tooling
**Problem:** Scripts over ~800 lines cause Claude/MCP timeouts and freezes.  
**Target:** No script exceeds 400-500 lines. Ideally under 300.

### 2. Future Readability
**Goal:** Return to any script after months away and immediately understand what it does.  
**Method:** Self-documenting code, clear naming, single responsibility.

### 3. Full Separation of Concerns
**Goal:** UI knows nothing about game logic. Game logic knows nothing about UI.  
**Method:** Communication through events, interfaces, and services.

### 4. Memory Efficiency
**Goal:** Minimize allocations, avoid unnecessary work, cache everything reusable.  
**Method:** Cache components, early-exit patterns, object pooling, allocation-free hot paths.

---

## Code Style Requirements

### No "var" - Explicit Types Always

```csharp
// BAD
var button = GetComponent<Button>();

// GOOD
Button button = GetComponent<Button>();
```

### Self-Documenting Code Over Comments

```csharp
// BAD
if (w.Length >= min && w.Length <= max && dict.Contains(w.ToUpper()))

// GOOD
bool isCorrectLength = word.Length >= minimumLength && word.Length <= maximumLength;
bool existsInDictionary = wordDictionary.Contains(word.ToUpper());
if (isCorrectLength && existsInDictionary)
```

### Naming Conventions

| Element | Convention | Example |
|---------|------------|---------|
| Private fields | _camelCase | `_gridSize` |
| Public properties | PascalCase | `GridSize` |
| Methods | PascalCase verb | `ValidateWord()` |
| Booleans | is/has/can prefix | `isValid`, `hasPlacedWord` |
| Events | On + PastTense | `OnWordValidated` |
| Interfaces | I + Noun | `IWordValidator` |

### Method Length

**Target:** Methods under 20 lines. If longer, extract helper methods.

---

## Controller Integration Pattern

Established pattern for integrating extracted controllers:

```csharp
// 1. Declare controller field
private CoordinatePlacementController _coordinatePlacementController;

// 2. Initialize in InitializeControllers() with dependencies
private void InitializeControllers()
{
    if (_coordinatePlacementController != null) return;  // Guard
    
    _coordinatePlacementController = new CoordinatePlacementController(
        _gridColorManager,
        GetCell,
        () => _currentGridSize
    );
}

// 3. Subscribe to controller events in WireControllerEvents()
private void WireControllerEvents()
{
    if (_eventsWired) return;
    
    _coordinatePlacementController.OnPlacementCancelled += HandlePlacementCancelled;
    _coordinatePlacementController.OnWordPlaced += HandleWordPlaced;
    
    _eventsWired = true;
}

// 4. Delegate public methods to controller
public void EnterPlacementMode(int wordRowIndex)
{
    string word = GetWordFromRow(wordRowIndex);
    _coordinatePlacementController.EnterPlacementMode(wordRowIndex, word);
}

// 5. Handle controller events
private void HandleWordPlaced(int rowIndex, string word, List<Vector2Int> positions)
{
    _wordPatternRows[rowIndex].MarkAsPlaced();
    OnWordPlaced?.Invoke(rowIndex, word, positions);
}
```

---

## Refactoring Process

### Per-Session Workflow

1. **Start:** State which script you're refactoring
2. **Upload:** Provide current version of the file
3. **Scope:** Define ONE extraction
4. **Execute:** Make the extraction
5. **Verify:** Confirm compilation, test basic functionality
6. **Commit:** Git commit with clear message
7. **Document:** Update analysis doc with completion status

### Extraction Checklist

Before extracting, verify:
- [ ] New class has single responsibility
- [ ] No "var" in new code
- [ ] Methods under 20 lines
- [ ] Explicit types everywhere
- [ ] Events for communication (not direct calls)

After extracting, verify:
- [ ] Original file compiles
- [ ] New file compiles
- [ ] Basic functionality still works
- [ ] No circular dependencies introduced

---

## Reference: Current Script Status

| Script | Lines | Target | Status | Notes |
|--------|-------|--------|--------|-------|
| PlayerGridPanel.cs | ~1,120 | ~300 | **COMPLETE** | 49% reduction, lifecycle bug fixed |
| GameplayUIController.cs | 2,112 | ~500 | **NEXT** | Analysis doc created |
| WordPatternRow.cs | ~800 | ~200 | PENDING | |
| SetupSettingsPanel.cs | ~760 | ~200 | PENDING | Some extractions done |
| GridCellUI.cs | ~250 | ~150 | LOW | |

## Extracted Controllers (All Projects)

| Controller | Lines | Source | Date |
|------------|-------|--------|------|
| LetterTrackerController | ~150 | PlayerGridPanel | Dec 5 |
| GridColorManager | ~50 | PlayerGridPanel | Dec 5 |
| PlacementPreviewController | ~50 | PlayerGridPanel | Dec 11 |
| WordPatternRowManager | ~400 | PlayerGridPanel | Dec 11 |
| CoordinatePlacementController | ~616 | PlayerGridPanel | Dec 12 |
| GridLayoutManager | ~593 | PlayerGridPanel | Dec 12 |
| PlayerColorController | ~80 | SetupSettingsPanel | Dec 5 |
| WordValidationService | ~60 | SetupSettingsPanel | Dec 5 |

---

## GameplayUIController Extraction Plan

See `DLYH_GameplayUIController_RefactoringAnalysis_12122025.md` for full details.

**Summary:**

| Extraction | Est. Lines | Priority |
|------------|------------|----------|
| GuessProcessor (generic) | ~400 | HIGH - replaces ~740 duplicate lines |
| WordGuessModeController | ~175 | HIGH |
| TestingHelper | ~194 | MEDIUM |
| PanelConfigurationManager | ~180 | MEDIUM |

**Key Insight:** Player and Opponent guess processing are nearly identical. A generic `GuessProcessor` service parameterized by target data and panel would eliminate ~340 lines of duplication.

---

## Success Metrics

| Metric | Target | Current |
|--------|--------|---------|
| Largest script | Under 500 lines | 2,112 (GameplayUIController) |
| PlayerGridPanel | Under 400 lines | 1,120 (**COMPLETE - 49%**) |
| Methods over 20 lines | 0 | TBD |
| Uses of "var" | 0 | 0 |
| Scripts with lifecycle bug fix | All that need it | 1 (PlayerGridPanel) |

---

**End of Refactoring Instructions**

Reference this document at the start of every refactoring session.

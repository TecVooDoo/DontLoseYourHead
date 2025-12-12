# Don't Lose Your Head - Refactoring Instructions

**Version:** 1.6  
**Date Created:** December 11, 2025  
**Last Updated:** December 12, 2025 (v6 - GameplayUIController cleanup COMPLETE)  
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

### GameplayUIController - COMPLETE (44% Reduction)

| Extraction | Lines | Status |
|------------|-------|--------|
| GuessProcessor | ~400 | **COMPLETE** |
| WordGuessModeController | ~290 | **COMPLETE** |
| Debug Log Cleanup | -32 | **COMPLETE** |

**GameplayUIController.cs:** 2,112 -> 1,179 lines (44% reduction) - **COMPLETE**

**Extracted Files:**
- `Assets/DLYH/Scripts/UI/Services/GuessProcessor.cs` (~400 lines)
- `Assets/DLYH/Scripts/UI/Controllers/WordGuessModeController.cs` (~290 lines)

### Bug Fixes (December 12, 2025)

1. **Word Placement Coordinates** - Fixed `HandleCoordinatePlacementWordPlaced()` in PlayerGridPanel.cs to call `SetPlacementPosition()`. Words now appear on grids in Gameplay Mode.

2. **Letter Width in Word Rows** - Removed `<mspace>` tags from WordPatternRow.cs `BuildDisplayText()`. Implemented monospace font (Consolas SDF) for consistent letter widths.

3. **Orphan Code Line** - Fixed incomplete Debug.Log removal that left orphan continuation line in `GenerateOpponentData()`.

**Phase 4 TODO:** Find medieval/carnival themed monospace font that matches game aesthetic.

---

## Code Quality Verification (December 12, 2025)

Final pass completed against all refactoring requirements:

| Check | Status | Notes |
|-------|--------|-------|
| No `var` usage | PASS | 0 occurrences |
| No GetComponent in hot paths | PASS | Only in initialization |
| No allocations in Update | PASS | Update delegates to controller only |
| Uses string.Format (no concat) | PASS | All formatting correct |
| Private field naming (_prefix) | PASS | All fields follow convention |
| HashSets reused with Clear() | PASS | Good memory pattern |
| Update method minimal | PASS | 1 line delegation |
| No orphan lines | PASS | Fixed during cleanup |

**Remaining Debug Statements (Appropriate):**
- 6 `Debug.LogError` calls (critical failures)
- 8 `Debug.LogWarning` calls (invalid states)
- 2 game outcome logs (win/lose announcements)
- Testing region logs (wrapped in `#if UNITY_EDITOR`)

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
**Target:** No script exceeds 500 lines. Ideally under 300.

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

**Acceptable Exceptions:** Initialization/configuration methods that run once (e.g., `ConfigureOwnerPanel`, `InitializeGuessProcessors`).

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

## Service Pattern (GuessProcessor)

For shared logic used by multiple callers, use a service with callback injection:

```csharp
public class GuessProcessor
{
    // Dependencies via constructor
    private readonly List<WordPlacementData> _targetWords;
    private readonly PlayerGridPanel _targetPanel;
    
    // Callbacks for external operations (avoids tight coupling)
    private readonly Action _onMissIncrement;
    private readonly Action<char, LetterState> _setLetterState;
    private readonly Func<string, bool> _validateWord;
    
    public GuessProcessor(
        List<WordPlacementData> targetWords,
        PlayerGridPanel targetPanel,
        Action onMissIncrement,
        Action<char, LetterState> setLetterState,
        Func<string, bool> validateWord)
    {
        _targetWords = targetWords;
        _targetPanel = targetPanel;
        _onMissIncrement = onMissIncrement;
        _setLetterState = setLetterState;
        _validateWord = validateWord;
    }
    
    public GuessResult ProcessLetterGuess(char letter)
    {
        // Logic here - uses callbacks for side effects
        _setLetterState?.Invoke(letter, LetterState.Hit);
    }
}
```

**Benefits:**
- Same service used for player AND opponent (parameterized)
- No direct references to GameplayUIController
- Easy to test in isolation
- Callbacks allow different behaviors without inheritance

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

| Script | Original | Current | Target | Status |
|--------|----------|---------|--------|--------|
| PlayerGridPanel.cs | 2,192 | 1,120 | ~300 | **COMPLETE (49%)** |
| GameplayUIController.cs | 2,112 | 1,179 | ~500 | **COMPLETE (44%)** |
| WordPatternRow.cs | ~800 | ~800 | ~200 | PENDING |
| SetupSettingsPanel.cs | ~760 | ~760 | ~200 | PENDING |
| GridCellUI.cs | ~250 | ~250 | ~150 | LOW |

## Extracted Files Summary

| File | Lines | Source | Date |
|------|-------|--------|------|
| LetterTrackerController.cs | ~150 | PlayerGridPanel | Dec 5 |
| GridColorManager.cs | ~50 | PlayerGridPanel | Dec 5 |
| PlacementPreviewController.cs | ~50 | PlayerGridPanel | Dec 11 |
| WordPatternRowManager.cs | ~400 | PlayerGridPanel | Dec 11 |
| CoordinatePlacementController.cs | ~616 | PlayerGridPanel | Dec 12 |
| GridLayoutManager.cs | ~593 | PlayerGridPanel | Dec 12 |
| PlayerColorController.cs | ~80 | SetupSettingsPanel | Dec 5 |
| WordValidationService.cs | ~60 | SetupSettingsPanel | Dec 5 |
| **GuessProcessor.cs** | **~400** | **GameplayUIController** | **Dec 12** |
| **WordGuessModeController.cs** | **~290** | **GameplayUIController** | **Dec 12** |

---

## Success Metrics

| Metric | Target | Current | Status |
|--------|--------|---------|--------|
| Largest script | Under 500 lines | 1,179 (GameplayUIController) | In Progress |
| PlayerGridPanel | Under 400 lines | 1,120 | **COMPLETE (49%)** |
| GameplayUIController | Under 500 lines | 1,179 | **COMPLETE (44%)** |
| Methods over 20 lines | 0 | ~10 (init methods) | Acceptable |
| Uses of "var" | 0 | 0 | **PASS** |
| Memory efficiency | No hot path allocs | Verified | **PASS** |

---

## Phase 4 Polish TODOs

| Item | Notes |
|------|-------|
| Medieval monospace font | Replace Consolas with theme-appropriate font |
| Invalid word feedback UI | Toast/popup for rejected words |
| Profanity filter | Some inappropriate words in word bank |
| Grid row labels compression | Labels don't resize with grid size changes |

---

## Future Refactoring (Optional)

These extractions are optional - current state is acceptable:

| Potential Extraction | Est. Lines | Priority |
|---------------------|------------|----------|
| PanelConfigurationManager | ~150 | Low |
| MissCounterController | ~80 | Low |
| TestingHelper to Editor/ | ~170 | Low |

---

**End of Refactoring Instructions**

Reference this document at the start of every refactoring session.

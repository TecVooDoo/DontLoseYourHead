# Don't Lose Your Head - Refactoring Instructions

**Version:** 1.3  
**Date Created:** December 11, 2025  
**Last Updated:** December 12, 2025 (v3)  
**Developer:** TecVooDoo LLC  

---

## Current Progress

**PlayerGridPanel Extractions Completed:**

| Controller | Lines | Status | Integrated |
|------------|-------|--------|------------|
| LetterTrackerController | ~150 | COMPLETE | Yes |
| GridColorManager | ~50 | COMPLETE | Yes |
| PlacementPreviewController | ~50 | COMPLETE | Yes |
| WordPatternRowManager | ~400 | COMPLETE | Yes |
| CoordinatePlacementController | ~616 | COMPLETE | Yes |
| GridLayoutManager | ~593 | COMPLETE | Yes |

**PlayerGridPanel.cs:** 2,192 -> 1,117 lines (49% reduction)

**Next Options:** GridCellManager extraction (~200 lines) OR move to other scripts

**Controller Files Location:** `Assets/DLYH/Scripts/UI/Controllers/`

---

## Purpose

This document defines the standards and goals for refactoring the DLYH codebase. Reference this document at the start of every refactoring session.

---

## Platform

**Unity Version:** 6.3 LTS  
**Approach:** Utilize Unity 6.3 capabilities fully. This is a Unity project - use Unity types (Vector2Int, Color, etc.) where appropriate. Reference official Unity 6.3 documentation for current best practices.

---

## Dependency Management

**Preferred:** ScriptableObjects for shared data and configuration  
**Flexible:** Use whatever pattern works best for the specific use case:
- ScriptableObjects for persistent data, events, configuration
- Constructor injection for services that need dependencies
- Service Locator only if constructor injection becomes unwieldy
- Direct references where appropriate (e.g., UI component references)

**Avoid:** Singletons with static Instance properties (use ScriptableObjects instead)

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

## Memory Efficiency Requirements

### Cache All GetComponent Calls

GetComponent allocates memory even when it finds nothing. Never call it repeatedly.

```csharp
// BAD - Allocates every frame
private void Update()
{
    Button button = GetComponent<Button>();
    button.interactable = _isReady;
}

// GOOD - Cache in Awake or Start
private Button _button;

private void Awake()
{
    _button = GetComponent<Button>();
}

private void Update()
{
    _button.interactable = _isReady;
}
```

### Early Exit in Update Methods

Don't run code that doesn't need to run.

```csharp
// GOOD - Only process when in input mode
private bool _isAcceptingKeyboardInput = false;

private void Update()
{
    if (!_isAcceptingKeyboardInput) return;  // Early exit
    
    Keyboard keyboard = Keyboard.current;
    if (keyboard == null) return;
    
    for (int i = 0; i < 26; i++)
    {
        Key key = Key.A + i;
        if (keyboard[key].wasPressedThisFrame)
        {
            ProcessLetterInput((char)('A' + i));
            return;  // Exit after finding a key
        }
    }
}
```

### Avoid Allocations in Hot Paths

```csharp
// BAD - Allocates new list every call
public List<GridCellUI> GetAdjacentCells(int row, int col)
{
    List<GridCellUI> adjacent = new List<GridCellUI>();
    // ... populate list
    return adjacent;
}

// GOOD - Reuse pre-allocated list
private readonly List<GridCellUI> _adjacentCellsBuffer = new List<GridCellUI>(8);

public List<GridCellUI> GetAdjacentCells(int row, int col)
{
    _adjacentCellsBuffer.Clear();
    // ... populate list
    return _adjacentCellsBuffer;
}
```

### Event Subscription Cleanup

Always unsubscribe to prevent memory leaks.

```csharp
private void OnEnable()
{
    _guessProcessor.OnLetterGuessProcessed += HandleLetterResult;
}

private void OnDisable()
{
    _guessProcessor.OnLetterGuessProcessed -= HandleLetterResult;
}
```

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
| Methods | PascalCase verb | `ValidateWord()`, `GetCurrentPlayer()` |
| Booleans | is/has/can prefix | `isValid`, `hasPlacedWord`, `canSubmit` |
| Events | On + PastTense | `OnWordValidated`, `OnGuessSubmitted` |
| Interfaces | I + Noun | `IWordValidator`, `IGuessHandler` |

### Method Length

**Target:** Methods under 20 lines. If longer, extract helper methods.

---

## Architecture Patterns

### Single Responsibility Services

Each service does ONE thing and does it well.

```csharp
// WordValidationService.cs - ONLY validates words
public class WordValidationService
{
    private readonly HashSet<string> _validWords;
    
    public bool IsValidWord(string word, int requiredLength)
    {
        if (string.IsNullOrEmpty(word)) return false;
        if (word.Length != requiredLength) return false;
        return _validWords.Contains(word.ToUpper());
    }
}
```

### Event-Driven Communication

UI and game logic never call each other directly.

```csharp
// Game logic raises events
public class GuessProcessor
{
    public event Action<char, bool> OnLetterGuessProcessed;
    
    public void ProcessLetterGuess(char letter)
    {
        bool wasHit = CheckLetterHit(letter);
        OnLetterGuessProcessed?.Invoke(letter, wasHit);
    }
}

// UI subscribes to events
public class LetterTrackerUI : MonoBehaviour
{
    private void OnEnable()
    {
        _guessProcessor.OnLetterGuessProcessed += HandleLetterResult;
    }
    
    private void HandleLetterResult(char letter, bool wasHit)
    {
        LetterButton button = GetButtonForLetter(letter);
        button.SetColor(wasHit ? _hitColor : _missColor);
    }
}
```

### Controller Integration Pattern

Established pattern for integrating extracted controllers:

```csharp
// 1. Declare controller field
private CoordinatePlacementController _coordinatePlacementController;

// 2. Initialize in InitializeControllers() with dependencies
private void InitializeControllers()
{
    _coordinatePlacementController = new CoordinatePlacementController(
        _gridColorManager,
        GetCell,
        () => _currentGridSize
    );
    
    _gridLayoutManager = new GridLayoutManager(
        _gridContainer, _rowLabelsContainer, _columnLabelsContainer,
        _gridWithRowLabelsRect, _gridContainerLayout, _rowLabelsLayout,
        _cellPrefab, _panelRectTransform
    );
}

// 3. Subscribe to controller events in WireControllerEvents()
private void WireControllerEvents()
{
    _coordinatePlacementController.OnPlacementCancelled += HandlePlacementCancelled;
    _coordinatePlacementController.OnWordPlaced += HandleWordPlaced;
    _gridLayoutManager.OnCellCreated = HandleCellCreated;
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

## File Organization

### Current Structure

```
Assets/DLYH/Scripts/
  Core/
    Interfaces/
    Services/
    Models/
    Enums/
  
  Data/
    ScriptableObjects/
  
  Managers/
    GameManager.cs
    TurnManager.cs
    PlayerManager.cs
  
  UI/
    Base/
    Components/
    Panels/
    Controllers/
      LetterTrackerController.cs (~150 lines) - COMPLETE
      GridColorManager.cs (~50 lines) - COMPLETE
      PlacementPreviewController.cs (~50 lines) - COMPLETE
      WordPatternRowManager.cs (~400 lines) - COMPLETE
      CoordinatePlacementController.cs (~616 lines) - COMPLETE
      GridLayoutManager.cs (~593 lines) - COMPLETE
      GridCellManager.cs (~200 lines) - OPTIONAL
```

---

## Refactoring Process

### Per-Session Workflow

1. **Start:** State which script you're refactoring
2. **Upload:** Provide current version of the file
3. **Scope:** Define ONE extraction (e.g., "Extract GridLayoutManager")
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

## Reference: Current Problem Scripts

| Script | Current Lines | Target Lines | Priority | Notes |
|--------|---------------|--------------|----------|-------|
| PlayerGridPanel.cs | ~1,117 | ~300 | MEDIUM | 6 controllers extracted, 49% reduction achieved |
| GameplayUIController.cs | ~1,600 | ~300 | HIGH | Not started - NEXT TARGET |
| WordPatternRow.cs | ~800 | ~200 | MEDIUM | Not started |
| SetupSettingsPanel.cs | ~760 | ~200 | MEDIUM | Some extractions done previously |
| GridCellUI.cs | ~250 | ~150 | LOW | Not started |

## Extracted Controllers

| Controller | Lines | Source | Location | Date |
|------------|-------|--------|----------|------|
| LetterTrackerController | ~150 | PlayerGridPanel | Controllers/ | Dec 5 |
| GridColorManager | ~50 | PlayerGridPanel | Controllers/ | Dec 5 |
| PlacementPreviewController | ~50 | PlayerGridPanel | Controllers/ | Dec 11 |
| WordPatternRowManager | ~400 | PlayerGridPanel | Controllers/ | Dec 11 |
| CoordinatePlacementController | ~616 | PlayerGridPanel | Controllers/ | Dec 12 |
| GridLayoutManager | ~593 | PlayerGridPanel | Controllers/ | Dec 12 |
| PlayerColorController | ~80 | SetupSettingsPanel | Controllers/ | Dec 5 |
| WordValidationService | ~60 | SetupSettingsPanel | Services/ | Dec 5 |

---

## Success Metrics

After refactoring is complete:

| Metric | Target | Current |
|--------|--------|---------|
| Largest script | Under 400 lines | 1,600 (GameplayUIController) |
| PlayerGridPanel | Under 400 lines | 1,117 (49% done) |
| Methods over 20 lines | 0 | TBD |
| Uses of "var" | 0 | 0 |
| Direct UI-to-logic calls | 0 | TBD |
| Time to understand any script | Under 2 minutes | TBD |

---

## Next Steps

1. **Option A:** Continue PlayerGridPanel - Extract GridCellManager (~200 lines)
2. **Option B:** Move to GameplayUIController (~1,600 lines) - Higher priority due to size
3. **Option C:** Consolidate and test existing extractions

**Recommendation:** Consider moving to GameplayUIController as it's now the largest script and PlayerGridPanel has achieved significant reduction (49%).

---

**End of Refactoring Instructions**

Reference this document at the start of every refactoring session.

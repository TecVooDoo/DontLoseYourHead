# Don't Lose Your Head - Design Decisions and Insights

**Version:** 1.8  
**Date:** November 22, 2025  
**Last Updated:** December 11, 2025  
**Developer:** TecVooDoo LLC  
**Designer:** Rune (Stephen Brandon)  

---

## Recent Changes (December 11, 2025)

### Gameplay Mode UI - COMPLETE

All turn-based interaction systems are now fully implemented:

| Feature | Status | Notes |
|---------|--------|-------|
| Letter guessing | COMPLETE | Click opponent's letter tracker |
| Coordinate guessing | COMPLETE | Click opponent's grid cells |
| Word guessing | COMPLETE | Guess Word button + keyboard mode |
| Three-color grid cells | COMPLETE | Green/yellow/red system |
| Yellow-to-green upgrade | COMPLETE | When letter is discovered |
| Duplicate guess prevention | COMPLETE | GuessResult enum |
| Solved word tracking | COMPLETE | _wordSolved flag pattern |
| Guessed word lists | COMPLETE | Under guillotines |

### Bug Fix: Solved Word Row Buttons

**Problem Discovered:**
When a player correctly guessed a word, the "Guess Word" button would hide momentarily but then reappear on the next turn.

**Root Cause:**
`WordPatternRow.ExitWordGuessMode()` calls `UpdateGuessButtonStates()` which blindly re-shows the guess button when `_inWordGuessMode = false`, overriding the hide call from `GameplayUIController.ProcessPlayerWordGuess()`.

**Solution Applied:**
Added `_wordSolved` flag to `WordPatternRow` that permanently prevents button display after correct guess:

```csharp
// In WordPatternRow.cs
private bool _wordSolved = false;

public void MarkWordSolved()
{
    _wordSolved = true;
    HideGuessWordButton();
}

public void ShowGuessWordButton()
{
    if (_wordSolved) return; // Never show if solved
    // ... rest of method
}
```

**Files Modified:**
- `WordPatternRow.cs` - Added `_wordSolved` flag, `MarkWordSolved()` method, updated `ShowGuessWordButton()` and `UpdateGuessButtonStates()`
- `GameplayUIController.cs` - Changed `row.HideGuessWordButton()` to `row.MarkWordSolved()`

### Bug Fix: New Input System Migration

**Problem Discovered:**
Using `Input.inputString` (legacy Input class) caused `InvalidOperationException` because the project uses Unity's New Input System.

**Solution Applied:**
Updated keyboard input handling to use `Keyboard.current`:

```csharp
using UnityEngine.InputSystem;

var keyboard = Keyboard.current;
if (keyboard == null) return;

for (int i = 0; i < 26; i++)
{
    Key key = Key.A + i;
    if (keyboard[key].wasPressedThisFrame)
    {
        char letter = (char)('A' + i);
        HandleKeyboardLetterInput(letter);
    }
}
```

### Bug Fix: Off-By-One Indexing Error

**Problem Discovered:**
Row numbers are 1-indexed (displayed as "1. CAT") but array indices are 0-indexed. Using `RowNumber` directly as an array index caused incorrect row targeting during word guess operations.

**Solution Applied:**
Convert between display numbers and array indices in event handlers:
- `rowIndex = rowNumber - 1` when accessing arrays
- `rowNumber = rowIndex + 1` when displaying to users

### Design Decision: Grid Cells Do NOT Reveal on Word Guess

**Decision:** When a player correctly guesses a complete word, the grid cells containing that word do NOT reveal. Grid cell letters are only revealed through coordinate guesses.

**Rationale:**
- Preserves strategic gameplay - knowing a word doesn't mean you know WHERE it is
- Players must still use coordinate guesses to locate words on the grid
- Creates interesting decisions: guess the word for the pattern, or hunt for coordinates?
- The word pattern row updates to show the discovered letters, but grid remains hidden

**Implementation:**
- `ProcessPlayerWordGuess()` updates word patterns and letter tracker
- Grid cells are NOT modified by word guesses
- Only `ProcessPlayerCoordinateGuess()` reveals grid cells
- Yellow cells DO upgrade to green when the guessed word's letters are discovered

### Design Decision: Three-Color Grid Cell System

| Color | Meaning | Visual |
|-------|---------|--------|
| Green | Hit - letter known | Green background + revealed letter |
| Red | Miss - empty cell | Red background |
| Yellow | Hit - letter unknown | Yellow/orange background + asterisk |

**Rationale:**
- Provides more strategic information than binary hit/miss
- Yellow "partial hit" shows progress without full revelation
- Upgrade mechanism creates satisfying "aha" moments when letters are discovered
- Consistent across both player and opponent panels

**Implementation:**
- `GridCellUI._isHitButLetterUnknown` flag tracks yellow state
- `MarkAsHitButLetterUnknown()` sets yellow state
- `UpgradeToKnownHit(char letter)` converts yellow to green
- `UpgradeOpponentGridCellsForLetter()` in GameplayUIController handles batch upgrades

### Design Decision: Word Guess Mode Button Flow

**Flow:**
1. All opponent word rows show "Guess Word" button initially
2. Player clicks one "Guess Word" button
3. ALL "Guess Word" buttons hide (prevent multiple active guesses)
4. Active row shows: Backspace | Accept | Cancel
5. Letter tracker converts to keyboard mode (all buttons white)
6. Player types letters (discovered letters stay fixed, can't be overwritten)
7. On Accept: Validate word, process guess, show result
8. On Cancel: Clear typed letters, exit mode, restore buttons
9. After correct guess: That row's button stays permanently hidden (_wordSolved)

### Deferred: Profanity Filter

During testing, some inappropriate words were discovered in the word bank. This is deferred to Phase 4 (Polish) since it doesn't affect core gameplay.

---

## Previous Changes (December 9, 2025)

### Unity Version Upgrade

**Changed:** Unity 6.2 -> Unity 6.3

All documentation now references Unity 6.3 as the development platform. The official Unity 6.3 documentation should be the source for Unity recommendations.

### MCP for Unity Update

**Changed:** MCP 8.1.x -> MCP 8.2.1

Key updates in MCP 8.2:
- **`batch_execute`** - New tool for running multiple MCP commands in a single call
- **HTTP-First Transport** - Now the default (stdio still available as fallback)
- **`manage_material`** - Enhanced material management capabilities

### Package Versions (Current as of Dec 9, 2025)

| Package | Version |
|---------|---------|
| DOTween Pro | 1.0.386 |
| Feel | 5.9.1 |
| Odin Inspector and Serializer | 4.0.1.0 |
| Odin Validator | 4.0.1.1 |
| SOAP - ScriptableObject Architecture Pattern | 3.6.1 |
| MCP for Unity | 8.2.1 (Local) |

### Gameplay Mode UI - Word Pattern Rows Fix

**Problem Discovered:**
Word pattern rows were showing blank/empty when transitioning to Gameplay Mode because `GetWordPatternRow()` was being called before the rows were cached. The panels are activated before `Start()` runs, so the word pattern rows array hadn't been populated.

**Solution Applied:**
Added `CacheWordPatternRows()` call in both `ConfigureOwnerPanel()` and `ConfigureOpponentPanel()` before attempting to access individual word pattern rows.

```csharp
// CRITICAL: Ensure word pattern rows are cached before we try to use them
// This is needed because Start() hasn't run yet on newly activated panels
_ownerPanel.CacheWordPatternRows();
```

---

## Previous Changes (December 6-8, 2025)

### CRITICAL: Opponent-Based Miss Limits

**Problem Discovered:**
The original miss limit calculation used YOUR grid settings to determine YOUR miss limit. But you're guessing against your OPPONENT's grid, not your own. This created backwards difficulty scaling.

**Old (Wrong) Approach:**
```
YourMissLimit = Base + YourGridBonus + YourWordModifier + YourDifficultyModifier
```

**New (Correct) Approach:**
```
YourMissLimit = Base + OpponentGridBonus + OpponentWordModifier + YourDifficultyModifier
```

**Implementation:**
- DifficultySO now has `CalculateMissLimitVsOpponent(DifficultySO opponentDifficulty)` method
- Old `MissLimit` property marked deprecated but kept for backward compatibility
- DifficultyCalculator has `CalculateMissLimitForPlayer()` for solo calculation
- SetupSettingsPanel preview shows estimate based on own settings (opponent unknown)
- GameplayUIController calculates actual miss limit using opponent's settings

### UI Layout Restructure (Setup Mode)

**Changed from vertical to horizontal 50/50 split:**
- **Old:** Settings panel stacked above grid panel
- **New:** Settings panel (left 50%) | Grid panel (right 50%)

**Reason:** Larger grid sizes (10x10, 11x11, 12x12) need more vertical space. Horizontal layout accommodates all grid sizes with consistent cell dimensions.

### Dynamic Cell Sizing

Grid cells now resize dynamically based on grid dimensions:
```csharp
float availableWidth = gridContainer.rect.width - (gridSize + 1) * spacing;
float availableHeight = gridContainer.rect.height - (gridSize + 1) * spacing;
float cellSize = Mathf.Min(availableWidth / gridSize, availableHeight / gridSize);
```

### Package Cleanup

Reduced from 16 packages to 6 core packages:

**Kept:**
1. DOTween Pro - Core animations
2. Feel - Game juice
3. Odin Inspector - Custom editors
4. Odin Validator - Project validation
5. SOAP - ScriptableObject architecture
6. MCP for Unity - Development workflow

---

## Previous Changes (December 5, 2025)

### Code Refactoring - Phase 1 Complete

Extracted focused controllers and services from "God Object" classes to improve maintainability:

| Extraction | Source | Target | Status |
|------------|--------|--------|--------|
| LetterTrackerController | PlayerGridPanel | Controllers/ | COMPLETE |
| GridColorManager | PlayerGridPanel | Controllers/ | COMPLETE |
| PlayerColorController | SetupSettingsPanel | Controllers/ | COMPLETE |
| WordValidationService | SetupSettingsPanel | Services/ | COMPLETE |
| Difficulty Dropdown Rename | SetupSettingsPanel | (inline) | COMPLETE |

**Results:**
- SetupSettingsPanel: ~965 lines -> ~760 lines (21% reduction)
- PlayerGridPanel: Still ~1,871 lines (more extraction marked TODO)

### Controller Pattern Established

All extracted controllers follow this pattern:

```csharp
public class ExampleController
{
    // Events for communication
    public event Action<SomeType> OnSomethingChanged;
    
    // Constructor injection
    public ExampleController(Transform container, SomeConfig config)
    {
        _container = container;
        _config = config;
    }
    
    // Lifecycle methods
    public void Initialize() { /* Setup */ }
    public void Cleanup() { /* Teardown */ }
    
    // Public API
    public void DoSomething() { /* Logic */ }
}
```

---

## Playtesting Insights

### Session 1: Excel Prototype with Spouse (November 2025)

**Critical Discovery:**
- **First game required 25 misses to solve the puzzle!**
- Original miss limits (8-12) are FAR too restrictive

### Session 2: Claude Playtest (November 24, 2025)

**Critical UX Issues Discovered:**
1. **Letter Selector Confusion** - "Hard for me to remember which letter selector goes with which grid"
2. **Color Coding Problems** - "Green on my words is confusing"
3. **Tracking Complexity** - Coordinate tracking became overwhelming
4. **Solution:** Integrated displays, distinct player colors, computer handles tracking

---

## The Counterintuitive Word Density Discovery

**Original Assumption:** More words = harder game

**Reality:** **More words = EASIER game!**
- Higher letter density means more hits per guess
- Fewer empty spaces = fewer opportunities for incorrect coordinate guesses

**Implications:**
- Word count directly affects difficulty, but inversely to expectations
- Fewer words + larger grid = hardest configuration
- More words + smaller grid = easiest configuration
- **Miss formula accounts for this:** 4 words gets -2 modifier (fewer allowed misses because it's easier)

---

## Implementation Status

### Completed Systems

| System | Status |
|--------|--------|
| Grid System (6x6-12x12) | COMPLETE |
| Word Placement | COMPLETE |
| Letter Guessing | COMPLETE |
| Coordinate Guessing | COMPLETE |
| Word Guessing (2-miss penalty) | COMPLETE |
| Letter Reveal (* -> letter) | COMPLETE |
| Turn Management | COMPLETE |
| Player System | COMPLETE |
| Game State Machine | COMPLETE |
| Difficulty System | COMPLETE |
| Word Bank (25,000+) | COMPLETE |
| Word Validation | COMPLETE |
| PlayerGridPanel | COMPLETE |
| SetupSettingsPanel | COMPLETE |
| GameplayUIController | COMPLETE (Dec 11) |
| Three-Color Grid Cells | COMPLETE (Dec 11) |
| Word Guess Mode | COMPLETE (Dec 11) |
| Solved Word Tracking | COMPLETE (Dec 11) |
| Duplicate Guess Prevention | COMPLETE (Dec 11) |
| Guessed Word Lists | COMPLETE (Dec 11) |

### In Progress

| Component | Status |
|-----------|--------|
| AI Opponent | TODO |
| Win/Lose UI Feedback | TODO |

### Deferred to Final Polish Phase

| Component | Status |
|-----------|--------|
| Invalid Word Feedback UI | Toast/popup deferred |
| Profanity Filter | Phase 4 |
| Grid row labels resize | Low priority |
| WordPatternRowManager | Extraction TODO |
| CoordinatePlacementController | Extraction TODO |

---

## Lessons Learned

### 1. Excel Prototyping Was Invaluable
- Caught balance issues before coding
- Multiple playtests showed skill progression curve

### 2. Assumptions Must Be Tested
- "More words = harder" was completely wrong
- Miss limits needed 2-3x increase from original design
- **Miss limit source matters** - must use opponent's grid, not your own

### 3. Asymmetric Difficulty Is A Strength
- Turns a problem (skill gap) into a feature
- Enables mixed-skill gameplay

### 4. Complete File Replacements Save Time
- Searching for specific lines is error-prone
- Full file replacement is faster for multi-line changes

### 5. Event-Driven UI Is Cleaner Than Polling
- Button state management via event subscriptions
- More responsive, less CPU usage

### 6. Controller Extraction Improves Maintainability (Dec 5, 2025)
- Breaking "God Objects" into focused controllers
- Constructor injection for dependencies
- Events for loose coupling between components

### 7. Unity Lifecycle Timing (Dec 6, 2025)
- Inactive GameObject configuration happens before Awake()
- SetActive(true) triggers Awake() which can wipe state
- Solution: Defensive Awake() that preserves existing data

### 8. MCP Hierarchy Modifications Cause Lockups (Dec 6, 2025)
- Using manage_gameobject for hierarchy changes freezes Unity
- Script edits via script_apply_edits are reliable

### 9. Collection Order Is Not Guaranteed (Dec 6, 2025)
- GetComponentsInChildren returns unpredictable order
- Sort by sibling index for consistent ordering

### 10. Word Placement Order Matters (Dec 6, 2025)
- Shorter words can block longer words on small grids
- Place longest words first for better success rate

### 11. Cache Before Access on Inactive Panels (Dec 9, 2025)
- Word pattern rows not cached until Start() runs
- Always call CacheWordPatternRows() before accessing individual rows

### 12. Simple Hide/Show Insufficient for Persistent States (Dec 11, 2025)
- Hide() can be overridden by subsequent Show() calls
- Use boolean flags (_wordSolved) for permanent state changes
- Check flags in ALL show methods

### 13. Input System Migration Matters (Dec 11, 2025)
- Legacy Input class causes errors when New Input System is active
- Use Keyboard.current instead of Input.inputString
- Check for null keyboard before accessing

### 14. Three-State Cells Provide Better Feedback (Dec 11, 2025)
- Binary hit/miss is less informative than green/yellow/red
- Yellow "partial hit" adds strategic depth
- Upgrade mechanism creates satisfying discovery moments

### 15. Off-By-One Indexing (Dec 11, 2025)
- Row numbers displayed to users are 1-indexed
- Array indices are 0-indexed
- Always convert: rowIndex = rowNumber - 1

---

## Git Commits Since Nov 28, 2025

| Date | Summary |
|------|---------|
| Nov 29 | Setup mode UI improvements, button behaviors |
| Nov 30 | Auto-accept, compass hide, placement colors, SetWordLengths |
| Dec 2 | Color buttons fix, random words, word validation, letter tracker fixes |
| Dec 4 | Setup Mode complete, event-driven button states, documentation update |
| Dec 5 | Code refactoring: Controllers and Services extracted |
| Dec 8 | Gameplay UI, layout restructure, opponent-based miss limits, package cleanup |
| Dec 9 | Unity 6.3, word pattern rows fix, gameplay mode functional |
| Dec 11 | Gameplay Mode COMPLETE: letter/coordinate/word guessing, three-color cells, solved row tracking |

---

## File Structure (Dec 11, 2025)

```
Assets/DLYH/Scripts/
  Core/
    DifficultyCalculator.cs
    DifficultySO.cs
    GameManager.cs
    TurnManager.cs
    PlayerSO.cs
    PlayerManager.cs
    Grid.cs
    GridCell.cs
    Word.cs
    WordListSO.cs
    ...
  UI/
    PlayerGridPanel.cs (~1,871 lines)
    SetupSettingsPanel.cs (~760 lines)
    GameplayUIController.cs (~1,600 lines)
    SetupModeController.cs
    WordPatternRow.cs (~800 lines)
    LetterButton.cs
    GridCellUI.cs (~250 lines)
    Controllers/
      LetterTrackerController.cs
      GridColorManager.cs
      PlayerColorController.cs
    Services/
      WordValidationService.cs
```

---

**End of Design Decisions Document**

This is a living document updated as:
- New playtesting reveals insights
- Design questions are resolved
- Balance adjustments are made
- Implementation uncovers new considerations

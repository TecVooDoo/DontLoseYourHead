# Don't Lose Your Head - Design Decisions and Insights

**Version:** 2.2  
**Date:** November 22, 2025  
**Last Updated:** December 13, 2025  
**Developer:** TecVooDoo LLC  
**Designer:** Rune (Stephen Brandon)  

---

## Recent Changes (December 13, 2025)

### Autocomplete Dropdowns - COMPLETE

Word autocomplete system fully implemented for Setup Mode:

| Feature | Status |
|---------|--------|
| AutocompleteManager controller | COMPLETE |
| AutocompleteDropdown UI component | COMPLETE |
| AutocompleteItem prefab | COMPLETE |
| Position below word rows | COMPLETE |
| Max 5 visible items | COMPLETE |
| Font size 24pt | COMPLETE |
| Hide on mode transitions | COMPLETE |

**Key Implementation Details:**
- Dropdown positioned using RectTransform anchoring below active word row
- ScrollRect with vertical scrollbar for overflow
- Filters word bank in real-time based on prefix match
- Hides automatically when: Pick Random Words clicked, word completed to correct length, word accepted, transitioning to Gameplay Mode

### Main Menu and Settings Panel - Scripts Created

**MainMenuController.cs (~130 lines):**
- Handles New Game, Settings, Exit buttons
- Controls container visibility (MainMenu/Setup/Gameplay)
- Exit uses `Application.Quit()` with Editor fallback

**SettingsPanel.cs (~270 lines):**
- SFX and Music volume sliders (0-100%)
- Default 50% for both
- Persists to PlayerPrefs
- Static methods for retrieving saved values without panel instance

### Lesson Learned: Never Assume Methods Exist (Dec 13, 2025)

**Problem:** MainMenuController was written to call `SetupSettingsPanel.ResetForNewGame()` which doesn't exist.

**Root Cause:** Assumed a method existed without verifying the actual script.

**Solution:** Added to project instructions:
- **Always:** Ask user to upload scripts or use MCP to verify before referencing methods/classes
- **Never:** Assume scripts, classes, or methods exist without verification

---

## Previous Changes (December 12, 2025)

### Code Refactoring - COMPLETE

Major refactoring effort completed to improve maintainability and reduce script sizes:

| Script | Original | Final | Reduction |
|--------|----------|-------|-----------|
| PlayerGridPanel.cs | 2,192 | 1,120 | 49% |
| GameplayUIController.cs | 2,112 | 1,179 | 44% |
| WordPatternRow.cs | 1,378 | 1,199 | 13% |

### Extracted Controllers/Services

**From PlayerGridPanel:**
- CoordinatePlacementController (~616 lines)
- GridLayoutManager (~593 lines)
- WordPatternRowManager (~400 lines)
- LetterTrackerController (~150 lines)
- PlacementPreviewController (~50 lines)
- GridColorManager (~50 lines)

**From GameplayUIController:**
- GuessProcessor (~400 lines) - Generic service for player/opponent
- WordGuessModeController (~290 lines) - Word guess state machine

**From WordPatternRow:**
- WordGuessInputController delegation (~290 lines)
- RowDisplayBuilder (~207 lines) - Display text utility

**From SetupSettingsPanel:**
- AutocompleteManager (~200 lines) - Word suggestion logic

### Critical Bug Fix: Unity Lifecycle Timing

**Problem Discovered:**
When `StartGameplay()` activates panels and immediately calls configuration methods, controllers are null because `Start()` hasn't run yet.

**Root Cause:**
1. `SetActive(true)` triggers `Awake()` immediately
2. `ConfigureOwnerPanel()` called immediately, BEFORE `Start()` runs on next frame
3. Controllers initialized in `Start()` are null

**Solution Applied - EnsureControllersInitialized Pattern:**

```csharp
private bool _eventsWired;

private void EnsureControllersInitialized()
{
    if (_gridCellManager != null) return;  // Already initialized
    
    Debug.Log("[PlayerGridPanel] EnsureControllersInitialized - initializing before Start()");
    
    // Initialize all controllers
    _gridCellManager = new GridCellManager();
    _gridColorManager = new GridColorManager(...);
    // ... more controllers
    
    WireControllerEventsIfNeeded();
}

public void InitializeGrid(int gridSize)
{
    EnsureControllersInitialized();  // Safe to call before Start()
    // ... rest of method
}
```

### Bug Fixes (December 12, 2025)

1. **Word Placement Coordinates** - Fixed `HandleCoordinatePlacementWordPlaced()` in PlayerGridPanel.cs to call `SetPlacementPosition()`. Words now appear on grids in Gameplay Mode.

2. **Letter Width in Word Rows** - Removed `<mspace>` tags from WordPatternRow.cs `BuildDisplayText()`. Implemented monospace font (Consolas SDF) for consistent letter widths.

3. **Orphan Code Line** - Fixed incomplete Debug.Log removal that left orphan continuation line in `GenerateOpponentData()`.

### WordPatternRow Refactoring (December 12, 2025)

**Priority 1: WordGuessInputController delegation**
- Removed `_inWordGuessMode`, `_guessedLetters`, `_guessCursorPosition` fields
- Removed helper methods for cursor navigation
- Added controller initialization with callbacks

**Priority 2: RowDisplayBuilder extraction**
- Created `Assets/DLYH/Scripts/UI/Utilities/RowDisplayBuilder.cs` (~207 lines)
- Extracted 80-line `BuildDisplayText()` method
- Uses shared StringBuilder to avoid allocations
- Pure functions with no side effects

### Code Quality Verification - PASSED

| Check | Status | Notes |
|-------|--------|-------|
| No `var` usage | PASS | 0 occurrences |
| No GetComponent in hot paths | PASS | Only in initialization |
| No allocations in Update | PASS | Update delegates to controller only |
| Uses string.Format (no concat) | PASS | All formatting correct |
| Private field naming (_prefix) | PASS | All fields follow convention |
| HashSets reused with Clear() | PASS | Good memory pattern |
| Update method minimal | PASS | 1 line delegation |

---

## Previous Changes (December 11, 2025)

### Gameplay Mode UI - COMPLETE

All turn-based interaction systems fully implemented:

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

**Problem:** "Guess Word" button would hide momentarily but reappear on next turn.

**Root Cause:** `WordPatternRow.ExitWordGuessMode()` calls `UpdateGuessButtonStates()` which re-shows the button.

**Solution:** Added `_wordSolved` flag for permanent state:

```csharp
private bool _wordSolved = false;

public void MarkWordSolved()
{
    _wordSolved = true;
    HideGuessWordButton();
}

public void ShowGuessWordButton()
{
    if (_wordSolved) return; // Never show if solved
}
```

### Bug Fix: New Input System Migration

**Problem:** `Input.inputString` caused `InvalidOperationException` with New Input System.

**Solution:** Use `Keyboard.current`:

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

---

## Previous Changes (December 9, 2025)

### Unity Version Upgrade

**Changed:** Unity 6.2 -> Unity 6.3

### MCP for Unity Update

**Changed:** MCP 8.1.x -> MCP 8.2.1

Key updates:
- `batch_execute` - New tool for running multiple MCP commands
- HTTP-First Transport - Now the default
- `manage_material` - Enhanced material management

### Package Versions (Current)

| Package | Version |
|---------|---------|
| DOTween Pro | 1.0.386 |
| Feel | 5.9.1 |
| Odin Inspector and Serializer | 4.0.1.0 |
| Odin Validator | 4.0.1.1 |
| SOAP | 3.6.1 |
| MCP for Unity | 8.2.1 (Local) |

---

## Previous Changes (December 6-8, 2025)

### CRITICAL: Opponent-Based Miss Limits

**Problem:** Original miss limit calculation used YOUR grid settings. But you're guessing against your OPPONENT's grid.

**Old (Wrong):**
```
YourMissLimit = Base + YourGridBonus + YourWordModifier + YourDifficultyModifier
```

**New (Correct):**
```
YourMissLimit = Base + OpponentGridBonus + OpponentWordModifier + YourDifficultyModifier
```

### UI Layout Restructure

**Changed from vertical to horizontal 50/50 split:**
- **Old:** Settings panel stacked above grid panel
- **New:** Settings panel (left 50%) | Grid panel (right 50%)

**Reason:** Larger grid sizes need more vertical space.

### Dynamic Cell Sizing

Grid cells resize dynamically based on grid dimensions:
```csharp
float availableWidth = gridContainer.rect.width - (gridSize + 1) * spacing;
float availableHeight = gridContainer.rect.height - (gridSize + 1) * spacing;
float cellSize = Mathf.Min(availableWidth / gridSize, availableHeight / gridSize);
```

### Package Cleanup

Reduced from 16 packages to 6 core packages.

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
| PlayerGridPanel | COMPLETE (1,120 lines) |
| SetupSettingsPanel | COMPLETE (~760 lines) |
| GameplayUIController | COMPLETE (1,179 lines) |
| WordPatternRow | COMPLETE (1,199 lines) |
| Three-Color Grid Cells | COMPLETE |
| Word Guess Mode | COMPLETE |
| Solved Word Tracking | COMPLETE |
| Duplicate Guess Prevention | COMPLETE |
| Guessed Word Lists | COMPLETE |
| Code Refactoring | COMPLETE |
| Grid Row Labels Resize | COMPLETE |
| Autocomplete Dropdowns | COMPLETE |

### In Progress (Dec 13, 2025)

| Component | Status | Notes |
|-----------|--------|-------|
| Main Menu | IN PROGRESS | Script created, UI pending |
| Settings Panel | IN PROGRESS | Script created, UI pending |

### Phase 3: AI Opponent

| Component | Status |
|-----------|--------|
| AI Opponent | TODO |
| Win/Lose UI Feedback | TODO |
| Turn Indicator Improvements | TODO |

### Phase 4: Polish and Features

| Component | Status |
|-----------|--------|
| Visual Polish (DOTween/Feel) | TODO |
| Audio Implementation | TODO |
| Invalid Word Feedback UI | TODO |
| Profanity Filter | TODO |
| Medieval Monospace Font | TODO |

### Phase 5: Multiplayer and Mobile

| Component | Status |
|-----------|--------|
| 2-Player Networking | TODO |
| Mobile Implementation | TODO |

---

## Audio Design Decisions

### Default Volume Settings

- **Sound Effects:** 50% (0.5f)
- **Music:** 50% (0.5f)

**Rationale:** Starting at 50% gives players room to adjust in either direction. Many players find games launch too loud, so a moderate default is player-friendly.

### Persistence

- Volume settings saved to PlayerPrefs
- Keys: `DLYH_SFXVolume`, `DLYH_MusicVolume`
- Static accessor methods available for audio system integration

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

### 6. Controller Extraction Improves Maintainability
- Breaking "God Objects" into focused controllers
- Constructor injection for dependencies
- Events for loose coupling between components

### 7. Unity Lifecycle Timing Matters (Dec 12, 2025)
- When activating GameObjects and immediately calling methods, `Start()` hasn't run yet
- Only `Awake()` has executed
- **Solution:** Add `EnsureControllersInitialized()` pattern

### 8. Defensive Initialization Pattern (Dec 12, 2025)
For scripts that might be configured before `Start()`:
```csharp
private void EnsureInitialized()
{
    if (_alreadyInitialized) return;
    // Initialize everything needed
    _alreadyInitialized = true;
}
```

### 9. Event Subscription Guards (Dec 12, 2025)
Always track whether events have been subscribed:
```csharp
private bool _eventsWired;

private void WireEvents()
{
    if (_eventsWired) return;
    // Subscribe to events
    _eventsWired = true;
}
```

### 10. Service Pattern with Callbacks (Dec 12, 2025)
Creating parameterized services eliminates duplicate code:
- Same `GuessProcessor` used for player AND opponent
- Callbacks allow different behaviors without inheritance

### 11. Debug Log Cleanup Requires Care (Dec 12, 2025)
- Removing multi-line Debug.Log statements can leave orphan continuation lines
- Always verify no orphans remain after cleanup

### 12. Static Utility Classes for Pure Functions (Dec 12, 2025)
- `RowDisplayBuilder` uses static methods with no side effects
- Shared StringBuilder avoids allocations
- Easy to test in isolation

### 13. Simple Hide/Show Insufficient for Persistent States
- Hide() can be overridden by subsequent Show() calls
- Use boolean flags (_wordSolved) for permanent state changes
- Check flags in ALL show methods

### 14. Input System Migration Matters
- Legacy Input class causes errors when New Input System is active
- Use Keyboard.current instead of Input.inputString

### 15. Three-State Cells Provide Better Feedback
- Binary hit/miss is less informative than green/yellow/red
- Yellow "partial hit" adds strategic depth

### 16. Off-By-One Indexing
- Row numbers displayed to users are 1-indexed
- Array indices are 0-indexed
- Always convert: rowIndex = rowNumber - 1

### 17. Never Assume Methods Exist (Dec 13, 2025)
- Always verify scripts via upload or MCP before calling methods
- Assuming methods exist causes delays and rework
- Added to project instructions as mandatory rule

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
| Dec 12 | Refactoring COMPLETE: WordPatternRow 13%, PlayerGridPanel 49%, GameplayUIController 44%, bug fixes, code quality verification, RowDisplayBuilder extraction |
| Dec 13 | Autocomplete COMPLETE, Main Menu/Settings Panel scripts created |

---

## File Structure (Dec 13, 2025)

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
    PlayerGridPanel.cs (~1,120 lines)
    SetupSettingsPanel.cs (~760 lines)
    GameplayUIController.cs (~1,179 lines)
    SetupModeController.cs (~150 lines)
    WordPatternRow.cs (~1,199 lines)
    LetterButton.cs (~200 lines)
    GridCellUI.cs (~250 lines)
    AutocompleteDropdown.cs (~450 lines)
    AutocompleteItem.cs (~140 lines)
    MainMenuController.cs (~130 lines) - NEW
    SettingsPanel.cs (~270 lines) - NEW
    Controllers/
      LetterTrackerController.cs (~150 lines)
      GridColorManager.cs (~50 lines)
      PlacementPreviewController.cs (~50 lines)
      WordPatternRowManager.cs (~400 lines)
      CoordinatePlacementController.cs (~616 lines)
      GridLayoutManager.cs (~593 lines)
      PlayerColorController.cs (~80 lines)
      WordGuessModeController.cs (~290 lines)
      WordGuessInputController.cs (~290 lines)
      AutocompleteManager.cs (~200 lines)
    Services/
      WordValidationService.cs (~60 lines)
      GuessProcessor.cs (~400 lines)
    Utilities/
      RowDisplayBuilder.cs (~207 lines)
```

---

**End of Design Decisions Document**

This is a living document updated as:
- New playtesting reveals insights
- Design questions are resolved
- Balance adjustments are made
- Implementation uncovers new considerations

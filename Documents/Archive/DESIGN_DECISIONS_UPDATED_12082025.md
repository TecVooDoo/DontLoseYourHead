# Don't Lose Your Head - Design Decisions and Insights

**Version:** 1.6  
**Date:** November 22, 2025  
**Last Updated:** December 8, 2025  
**Developer:** TecVooDoo LLC  
**Designer:** Rune (Stephen Brandon)  

---

## Recent Changes (December 6-8, 2025)

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

**Why This Matters:**
- If opponent has a small 6x6 grid with 4 words, their words are easier to find
- You should get FEWER misses because it's easier
- If opponent has a large 12x12 grid with 3 words, their words are harder to find
- You should get MORE misses because it's harder

**Implementation:**
- DifficultySO now has `CalculateMissLimitVsOpponent(DifficultySO opponentDifficulty)` method
- Old `MissLimit` property marked deprecated but kept for backward compatibility
- DifficultyCalculator has `CalculateMissLimitForPlayer()` for solo calculation
- SetupSettingsPanel preview shows estimate based on own settings (opponent unknown)
- GameplayUIController calculates actual miss limit using opponent's settings

### Gameplay Mode UI Implementation

| Component | Status | Notes |
|-----------|--------|-------|
| GameplayUIController | COMPLETE | Manages two PlayerGridPanel instances |
| Owner Panel | COMPLETE | Shows YOUR words fully revealed |
| Opponent Panel | COMPLETE | Shows opponent's words with hidden letters |
| Hidden Letter Support | COMPLETE | GridCellUI shows asterisks, upgrades to letters |
| Data Transfer | COMPLETE | Captures setup data, transfers to gameplay panels |
| Grid Initialization | COMPLETE | Forces initialization before word placement |

### UI Layout Restructure (Setup Mode)

**Changed from vertical to horizontal 50/50 split:**
- **Old:** Settings panel stacked above grid panel
- **New:** Settings panel (left 50%) | Grid panel (right 50%)

**Reason:** Larger grid sizes (10x10, 11x11, 12x12) need more vertical space. Horizontal layout accommodates all grid sizes with consistent cell dimensions.

**Implementation:**
- SetupContainer changed from VerticalLayoutGroup to HorizontalLayoutGroup
- Both child panels set to 50% width via LayoutElement preferredWidth
- Dynamic cell sizing calculates optimal cell size based on available grid space

### Dynamic Cell Sizing

Grid cells now resize dynamically based on grid dimensions:
```csharp
float availableWidth = gridContainer.rect.width - (gridSize + 1) * spacing;
float availableHeight = gridContainer.rect.height - (gridSize + 1) * spacing;
float cellSize = Mathf.Min(availableWidth / gridSize, availableHeight / gridSize);
```

### Bug Fixes (Dec 6-8, 2025)

| Bug | Fix Applied | Script(s) Modified |
|-----|-------------|-------------------|
| Word rows in wrong order | Sort by sibling index instead of GetComponentsInChildren order | WordPatternRow[], GameplayUIController |
| Start button not enabling | Added event subscriptions for OnWordPlaced, OnWordDeleted, OnWordLengthsChanged | GameplayUIController, SetupSettingsPanel |
| Random placement fails on small grids | Changed to longest-to-shortest word placement order | PlayerGridPanel |
| Panel data wiped on SetActive | Defensive Awake() preserves existing data | WordPatternRow |
| Labels misaligned with grid | Cache labels by name, not hierarchy order | PlayerGridPanel |
| DifficultySO.MissLimit removed | Added backward-compatible deprecated property | DifficultySO |

### Package Cleanup

Reduced from 16 packages to 6 core packages:

**Kept:**
1. DOTween Pro - Core animations
2. Feel - Game juice
3. Odin Inspector - Custom editors
4. Odin Validator - Project validation
5. SOAP - ScriptableObject architecture
6. MCP for Unity - Development workflow

**Removed:**
- Scriptable Sheets (word bank complete)
- Hot Reload (dev convenience)
- UniTask (not using async patterns)
- vHierarchy 2 / vFavorites 2 (editor only)
- Various others

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

### Remaining Refactoring (Deferred to Final Polish)

PlayerGridPanel still contains these large sections that could be extracted:

| Region | Lines | Potential Class |
|--------|-------|-----------------|
| Word Pattern Rows | ~292 | WordPatternRowManager |
| Coordinate Placement | ~213 | CoordinatePlacementController |
| Placement Preview | ~147 | PlacementPreviewController |
| Grid Layout | ~111 | GridLayoutManager |

---

## Previous Changes (December 4, 2025)

### Setup Mode Completed

| Feature | Implementation | Notes |
|---------|----------------|-------|
| Pick Random Words button | Event-driven state management | Fills only empty rows, preserves manual entries |
| Place Random Positions button | Event-driven state management | Enables when words entered, disables after placement |
| Grid clearing on size change | Confirmed working | Grid clears when dropdown changed |
| Random compass in grid corner | REMOVED | Replaced by Place Random Positions button |

### Button State Management Pattern

Both random action buttons now use event subscriptions rather than polling:

```csharp
// Subscribe to word row events
row.OnWordAccepted += OnWordAcceptedHandler;
row.OnDeleteClicked += OnWordDeletedHandler;
_playerGridPanel.OnWordPlaced += OnWordPlacedHandler;

// Update button states in handlers
private void OnWordAcceptedHandler(int rowNumber, string word)
{
    UpdatePickRandomWordsButtonState();
    UpdatePlaceRandomPositionsButtonState();
}
```

---

## Previous Changes (December 2, 2025)

### Bug Fixes Completed

| Bug | Fix Applied | Script(s) Modified |
|-----|-------------|-------------------|
| Single color cycling button | Replaced with 8 preset color buttons + outline highlight | SetupSettingsPanel.cs |
| Color selection not updating visuals | Added UpdatePlayerColorVisuals() method | PlayerGridPanel.cs |
| Word count showing 4 rows on start | Added InitializeWordRows() called in Start() | SetupSettingsPanel.cs |
| Typing in name field also typing in word rows | Added input field focus check to SetupModeController | SetupModeController.cs |
| Letter buttons staying grey after name input | Fixed IsInteractable setter to restore Normal state | LetterButton.cs |
| 6-letter word list had 5-letter words | Reimported word bank using WordBankImporter | WordListSO asset |
| Word validation not connected | Added ValidateWord method + SetWordValidator call | SetupSettingsPanel.cs |

### Features Added (Dec 2)

1. **Expanded Grid Size Support** - Now supports 6x6, 7x7, 8x8, 9x9, 10x10, 11x11, 12x12
2. **Miss Limit Formula Updated** - Scales smoothly across all grid sizes
3. **Invalid Word Feedback System** - ShowFeedback() method backend ready (UI deferred)
4. **Word Validation Integration** - Full pipeline connecting SetupSettingsPanel to WordListSO

---

## Playtesting Insights

### Session 1: Excel Prototype with Spouse (November 2025)

**Setup:**
- 3 words (3, 4, 5 letters = 12 total letters)
- Grid size: 8x8
- Original miss limit concept: 8-12 misses

**Critical Discovery:**
- **First game required 25 misses to solve the puzzle!**
- Original miss limits (8-12) are FAR too restrictive
- Player would have lost multiple times over before solving

**Positive Findings:**
- Spouse enjoyed the gameplay despite frustration with difficulty
- Core mechanics validated - the game loop is fun!
- Player improved significantly over multiple sessions
- Strategic decisions (letter vs coordinate guessing) mattered

### Session 2: Claude Playtest (November 24, 2025)

**Setup:**
- 8x8 grid, 3 words each, 23 miss limit, Excel-based tracking

**Game Statistics:**
- Duration: 25 turns (50 total moves)
- Move Breakdown: 72% Letter, 26% Coordinate, 2% Word
- Final Miss Counts: Both players 16/23 (70% of limit)

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

**Example:**
- 3 words on 8x8: 12 letters / 64 cells = 19% density
- 4 words on 8x8: 18 letters / 64 cells = 28% density

**Implications:**
- Word count directly affects difficulty, but inversely to expectations
- Fewer words + larger grid = hardest configuration
- More words + smaller grid = easiest configuration
- **Miss formula accounts for this:** 4 words gets -2 modifier (fewer allowed misses because it's easier)

---

## UI Architecture Decisions

### Decision: Shared PlayerGridPanel (Nov 28, 2025)

Single PlayerGridPanel component used in both phases with different modes:

| Feature | Setup Mode | Gameplay Mode |
|---------|------------|---------------|
| Grid visibility | Own words visible | Own visible, opponent hidden |
| Letter tracker | Acts as input keyboard | Shows guessed letters only |
| Word patterns | Shows entered words | Shows discovered patterns |
| Compass icons | Active for placement | Hidden |
| Click behavior | Places letters on grid | Makes guesses |
| Panels visible | Single panel | Two panels side-by-side |

### Decision: Letter Tracker as Keyboard (Nov 28, 2025)

The Letter Tracker row doubles as the keyboard:
- Two rows: A-M and N-Z
- In Setup Mode: Click letters to type words
- In Gameplay Mode: Shows which letters have been guessed
- Also routes to player name input when that field is focused

### Decision: 8 Preset Color Buttons (Dec 2, 2025)

Replaced single color cycling button with 8 preset buttons:
- Each button has its own color image
- Outline component shows selection
- Colors: Blue, Green, Orange, Purple, Cyan, Pink, Yellow, Brown
- Reserved colors (not available): White, Red, Black, Grey (UI states)

### Decision: Auto-Accept Words (Nov 29, 2025)

Words automatically accept when they reach required length:
- Reduces clicks needed
- Immediate feedback on word completion
- Compass button becomes active automatically

### Decision: Event-Driven Button States (Dec 4, 2025)

Random action buttons use event subscriptions instead of polling:
- **Pick Random Words:** Subscribes to OnWordAccepted, OnDeleteClicked
- **Place Random Positions:** Subscribes to OnWordAccepted, OnDeleteClicked, OnWordPlaced
- **Start Button:** Subscribes to OnWordPlaced, OnWordDeleted, OnWordLengthsChanged
- More responsive than Update() polling
- Cleaner separation of concerns

### Decision: Place Random Positions Button Replaces Grid Compass (Dec 4, 2025)

Original design had a compass icon in the grid corner for random placement of single word.
Replaced with "Place Random Positions" button that places ALL unplaced words at once:
- Simpler UX - one button instead of per-word compass clicks
- Faster setup for players who don't care about word positions
- Button state clearly indicates when it's useful

### Decision: Controller/Service Extraction Pattern (Dec 5, 2025)

Large "God Object" classes broken into focused components:
- **Controllers** handle UI interactions and state for specific UI areas
- **Services** handle business logic without UI dependencies
- Main panels coordinate between controllers via events
- Constructor injection for dependencies
- Initialize()/Cleanup() lifecycle pattern

### Decision: Horizontal 50/50 Layout (Dec 6, 2025)

Changed Setup Mode from vertical stacking to horizontal split:
- Settings panel takes left 50%
- Grid panel takes right 50%
- Accommodates larger grid sizes (10x10, 11x11, 12x12)
- Dynamic cell sizing adjusts to available space

### Decision: Opponent-Based Miss Limits (Dec 6-8, 2025)

**CRITICAL DESIGN FIX:** Your miss limit is calculated from your OPPONENT's grid settings, not your own.

**Rationale:**
- You guess against opponent's grid, not your own
- Opponent's grid size/word count determines how hard it is for you
- Your difficulty preference still applies (Easy/Normal/Hard)
- This creates proper asymmetric difficulty

### Decision: Longest-to-Shortest Word Placement (Dec 6, 2025)

Random word placement now places longest words first:
- Prevents shorter words from "blocking" longer words
- Reduces placement failures on smaller grids
- More reliable random placement overall

### Decision: Defensive Awake() Programming (Dec 6, 2025)

When configuring inactive GameObjects before SetActive(true):
- Awake() runs when object becomes active
- Can wipe out pre-configured data
- Solution: Check if already configured before reinitializing
- Preserve existing arrays and state

### Decision: Label Caching by Name (Dec 6, 2025)

Grid column/row labels cached by name instead of hierarchy order:
- GetComponentsInChildren returns unpredictable order
- Caching by name ("ColLabel_A", "RowLabel_1") is reliable
- Labels always align with correct grid positions

---

## Difficulty System (Implemented)

### Miss Limit Formula (Updated Dec 8, 2025)

```
MissLimit = Base + OpponentGridBonus + OpponentWordModifier + YourDifficultyModifier

Constants:
  BASE_MISSES = 15

  OPPONENT_GRID_BONUS:
    6x6 = 3, 7x7 = 4, 8x8 = 6, 9x9 = 8
    10x10 = 10, 11x11 = 12, 12x12 = 13

  OPPONENT_WORD_MODIFIER:
    3 words = 0 (harder for you - fewer letters to find)
    4 words = -2 (easier for you - more letters to find)

  YOUR_DIFFICULTY_MODIFIER:
    Hard = -4
    Normal = 0
    Easy = +4
```

### Asymmetric Difficulty

Each player can independently choose grid size, word count, and difficulty.

**Example Scenarios:**
- Veteran vs Newcomer: Veteran on 10x10/3 words/Hard, Newcomer on 6x6/4 words/Easy
- Parent vs Child: Parent handicaps with harder settings
- Skill Development: Start Easy, progress to Normal, then Hard

---

## Critical UI Requirements (From Playtesting)

### 1. Integrated Letter Displays
- Each grid gets its own letter tracker
- Visual connection between letters and their grid
- Letter tracker doubles as keyboard in setup mode

### 2. Player Color Coding
- 8 distinct preset colors
- Apply to: grid borders, labels, found words
- Reserved: White, Red, Black, Grey (UI states)

### 3. Clear Coordinate Labels
- Large, readable labels (A-L columns, 1-12 rows)
- Always visible (not just on hover)

### 4. Automatic State Tracking
- Computer tracks all known letters
- Computer tracks all revealed cells
- Computer tracks miss counts
- Computer prevents invalid/repeated guesses

### 5. Word Validation Feedback
- Invalid words rejected with console message
- Visual toast UI deferred to Polish Phase

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
| Difficulty System | COMPLETE (opponent-based Dec 8) |
| Word Bank (25,000+) | COMPLETE |
| Word Validation | COMPLETE |
| PlayerGridPanel | COMPLETE |
| SetupSettingsPanel | COMPLETE |
| Color Picker (8 buttons) | COMPLETE |
| Pick Random Words | COMPLETE |
| Place Random Positions | COMPLETE |
| Setup Mode | COMPLETE |
| LetterTrackerController | COMPLETE (Dec 5) |
| GridColorManager | COMPLETE (Dec 5) |
| PlayerColorController | COMPLETE (Dec 5) |
| WordValidationService | COMPLETE (Dec 5) |
| GameplayUIController | COMPLETE (Dec 8) |
| Horizontal 50/50 Layout | COMPLETE (Dec 6) |
| Dynamic Cell Sizing | COMPLETE (Dec 6) |
| Owner/Opponent Panels | COMPLETE (Dec 8) |
| Hidden Letter Support | COMPLETE (Dec 6) |

### In Progress

| Component | Status |
|-----------|--------|
| Turn-based interaction | TODO |
| Center guillotine area | TODO |

### Deferred to Final Polish Phase

| Component | Status |
|-----------|--------|
| Invalid Word Feedback UI | Toast/popup deferred |
| Grid row labels resize | Low priority |
| WordPatternRowManager | Extraction TODO |
| CoordinatePlacementController | Extraction TODO |
| PlacementPreviewController | Extraction TODO |
| GridLayoutManager | Extraction TODO |

---

## Lessons Learned

### 1. Excel Prototyping Was Invaluable
- Caught balance issues before coding
- Multiple playtests showed skill progression curve
- Iterative improvements during play revealed UX issues

### 2. Assumptions Must Be Tested
- "More words = harder" was completely wrong
- Miss limits needed 2-3x increase from original design
- Coordinate tracking harder than expected
- **Miss limit source matters** - must use opponent's grid, not your own

### 3. Asymmetric Difficulty Is A Strength
- Turns a problem (skill gap) into a feature
- Enables mixed-skill gameplay
- Unique selling point for the game

### 4. Backend First, Polish Later
- Focus on mechanics and balance first
- Use placeholders for art/UI
- Polish comes after core loop is proven fun

### 5. Complete File Replacements Save Time
- Searching for specific lines is error-prone
- Full file replacement is faster for multi-line changes

### 6. Input Field Focus Matters
- Keyboard input routes globally unless checked
- Must check if TMP input fields are focused before processing
- Letter tracker should work for name input too

### 7. Event-Driven UI Is Cleaner Than Polling
- Button state management via event subscriptions
- More responsive, less CPU usage
- Cleaner code organization

### 8. Avoid Verbose MCP Tool Output
- Displaying full "action: read" tool calls causes UI lockup
- Summarize findings instead of showing raw calls

### 9. Controller Extraction Improves Maintainability (Dec 5, 2025)
- Breaking "God Objects" into focused controllers
- Constructor injection for dependencies
- Events for loose coupling between components
- Initialize()/Cleanup() lifecycle pattern
- Can refactor incrementally without breaking functionality

### 10. Unity Lifecycle Timing (Dec 6, 2025)
- Inactive GameObject configuration happens before Awake()
- SetActive(true) triggers Awake() which can wipe state
- Solution: Defensive Awake() that preserves existing data
- Always check if already configured before reinitializing

### 11. MCP Hierarchy Modifications Cause Lockups (Dec 6, 2025)
- Using manage_gameobject for hierarchy changes freezes Unity
- Script edits via script_apply_edits are reliable
- Fallback: Provide complete files for manual replacement

### 12. Collection Order Is Not Guaranteed (Dec 6, 2025)
- GetComponentsInChildren returns unpredictable order
- Sort by sibling index for consistent ordering
- Cache references by name for reliable lookup

### 13. Word Placement Order Matters (Dec 6, 2025)
- Shorter words can block longer words on small grids
- Place longest words first for better success rate
- Random placement more reliable with sorted word list

---

## Git Commits Since Nov 28, 2025

| Date | Summary |
|------|---------|
| Nov 29 | Setup mode UI improvements, button behaviors |
| Nov 30 | Auto-accept, compass hide, placement colors, SetWordLengths |
| Dec 2 | Color buttons fix, random words, word validation, letter tracker fixes |
| Dec 4 | Setup Mode complete, event-driven button states, documentation update |
| Dec 5 | Code refactoring: LetterTrackerController, GridColorManager, PlayerColorController, WordValidationService |
| Dec 8 | Gameplay UI, layout restructure, opponent-based miss limits, package cleanup |

---

## File Structure (Dec 8, 2025)

```
Assets/DLYH/Scripts/
  Core/
    DifficultyCalculator.cs
    DifficultySO.cs (updated: opponent-based miss limits)
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
    PlayerGridPanel.cs (~1,871 lines - more extraction TODO)
    SetupSettingsPanel.cs (~760 lines)
    GameplayUIController.cs (~400 lines - NEW Dec 6-8)
    SetupModeController.cs
    WordPatternRow.cs
    LetterButton.cs
    GridCellUI.cs (enhanced: hidden letter support)
    Controllers/
      LetterTrackerController.cs (Dec 5)
      GridColorManager.cs (Dec 5)
      PlayerColorController.cs (Dec 5)
    Services/
      WordValidationService.cs (Dec 5)
```

---

**End of Design Decisions Document**

This is a living document updated as:
- New playtesting reveals insights
- Design questions are resolved
- Balance adjustments are made
- Implementation uncovers new considerations

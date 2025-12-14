# Don't Lose Your Head - Design Decisions and Insights

**Version:** 1.4  
**Date:** November 22, 2025  
**Last Updated:** December 4, 2025  
**Developer:** TecVooDoo LLC  
**Designer:** Rune (Stephen Brandon)  

---

## Recent Changes (December 4, 2025)

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

### Deferred Items

| Item | Reason | Target Phase |
|------|--------|--------------|
| Invalid word toast/popup | Easy Popup System removed from project | Phase 4 |
| Grid row labels resize | Low priority polish | Phase 4 |
| Rename Forgiveness dropdown | Low priority polish | Phase 4 |

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
- More responsive than Update() polling
- Cleaner separation of concerns

### Decision: Place Random Positions Button Replaces Grid Compass (Dec 4, 2025)

Original design had a compass icon in the grid corner for random placement of single word.
Replaced with "Place Random Positions" button that places ALL unplaced words at once:
- Simpler UX - one button instead of per-word compass clicks
- Faster setup for players who don't care about word positions
- Button state clearly indicates when it's useful

---

## Difficulty System (Implemented)

### Miss Limit Formula (Final Implementation)

```
MissLimit = Base + GridBonus + WordModifier + DifficultyModifier

Constants:
  BASE_MISSES = 15

  GRID_BONUS:
    6x6 = 3, 7x7 = 4, 8x8 = 6, 9x9 = 8
    10x10 = 10, 11x11 = 12, 12x12 = 13

  WORD_MODIFIER:
    3 words = 0 (harder - fewer letters)
    4 words = -2 (easier - more letters, so fewer misses allowed)

  DIFFICULTY_MODIFIER (formerly Forgiveness):
    Hard (Strict) = -4
    Normal = 0
    Easy (Forgiving) = +4
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
- Visual toast UI deferred to Phase 4

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
| Color Picker (8 buttons) | COMPLETE |
| Pick Random Words | COMPLETE |
| Place Random Positions | COMPLETE |
| Setup Mode | COMPLETE |

### Deferred to Phase 4

| Component | Status |
|-----------|--------|
| Invalid Word Feedback UI | Toast/popup deferred |
| Grid row labels resize | Low priority |
| Rename Forgiveness dropdown | Low priority |

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

---

## Git Commits Since Nov 28, 2025

| Date | Summary |
|------|---------|
| Nov 29 | Setup mode UI improvements, button behaviors |
| Nov 30 | Auto-accept, compass hide, placement colors, SetWordLengths |
| Dec 2 | Color buttons fix, random words, word validation, letter tracker fixes |
| Dec 4 | Setup Mode complete, event-driven button states, documentation update |

---

**End of Design Decisions Document**

This is a living document updated as:
- New playtesting reveals insights
- Design questions are resolved
- Balance adjustments are made
- Implementation uncovers new considerations

# Don't Lose Your Head - Game Design Document

**Version:** 1.8  
**Date:** November 20, 2025  
**Last Updated:** December 9, 2025  
**Developer:** TecVooDoo LLC  
**Designer:** Rune (Stephen Brandon)  
**Platform:** Unity 6.3 (2D)  
**Target:** PC/Mobile  

---

## High Concept

A competitive word game combining Hangman's letter-guessing with Battleship's grid-based hidden information. Players place hidden words on grids and take turns guessing letters or coordinates to find their opponent's words before the guillotine blade falls.

**Key Innovation:** Asymmetric difficulty system allows mixed-skill players (parent/child, veteran/newbie) to compete fairly by choosing different grid sizes, word counts, and difficulty settings.

---

## Game Flow Overview

The game consists of two distinct phases, each with different UI layouts:

### Phase 1: Setup Phase (Per-Player)
Each player configures their settings and places words on their grid. Players take turns in setup - Player 1 completes setup, then Player 2.

### Phase 2: Gameplay Phase (Alternating Turns)
Both players' grids are visible. Players alternate turns guessing letters, coordinates, or words to find their opponent's hidden words.

---

## Phase 1: Setup Phase

### Setup Screen Layout (Horizontal 50/50 Split)

```
+------------------------------------------------------------------+
| [MAIN MENU]              SETUP                    [START]        |
+------------------------------------------------------------------+
|                          |                                       |
| PLAYER NAME [____]       |      A  B  C  D  E  F  G  H           |
| PLAYER COLOR [8 buttons] |   1 [  ][  ][  ][  ][  ][  ][  ][  ]  |
| GRID SIZE    [8x8 v]     |   2 [  ][  ][  ][  ][  ][  ][  ][  ]  |
| # OF WORDS   [3   v]     |   3 [  ][  ][  ][  ][  ][  ][  ][  ]  |
| DIFFICULTY   [Normal v]  |   4 [C ][  ][  ][  ][  ][  ][  ][  ]  |
| MISS LIMIT: 21           |   5 [  ][A ][  ][  ][  ][  ][  ][  ]  |
|                          |   6 [  ][  ][T ][  ][  ][  ][  ][  ]  |
| [PICK RANDOM WORDS]      |   7 [  ][  ][  ][  ][  ][  ][  ][  ]  |
| [PLACE RANDOM POSITIONS] |   8 [  ][  ][  ][  ][  ][  ][  ][  ]  |
|                          |                                       |
| 1. C A T      [compass]  |   A  B  C  D  E  F  G  H  I           |
| 2. R A C K    [compass]  |   J  K  L  M  N  O  P  Q  R           |
| 3. _ _ _ _ _  [compass]  |   S  T  U  V  W  X  Y  Z              |
+------------------------------------------------------------------+
```

### Setup UI Components

**Top Bar:**
- Main Menu button - returns to main menu
- "SETUP" label - current phase indicator
- Start button - begins gameplay (disabled until all words placed)

**Settings Panel (Left Side):**
- Player Name - editable text field
- Player Color - 8 preset color buttons with selection highlight
- Grid Size dropdown: 6x6, 7x7, 8x8, 9x9, 10x10, 11x11, 12x12
- # of Words dropdown: 3, 4
- Difficulty dropdown: Easy, Normal, Hard
- Miss Limit display - updates dynamically based on settings
- "Pick Random Words" button - auto-selects valid words for EMPTY rows only
- "Place Random Positions" button - places all unplaced words randomly on grid
- Word rows with compass buttons for placement mode

**Grid Panel (Right Side):**
- Column labels (A-L depending on grid size)
- Row labels (1-12 depending on grid size)
- Grid cells showing placed letters (dynamic sizing based on grid dimensions)
- Letter Tracker - two rows (A-M and N-Z) doubles as keyboard
- During placement mode: Yellow = current position, Green = valid next, Red = invalid

### Button State Management

**Pick Random Words Button:**
- ENABLED when any word row is empty
- DISABLED when all word rows are filled
- Only fills empty rows - preserves manually entered words

**Place Random Positions Button:**
- ENABLED when any word is entered but not yet placed on grid
- DISABLED when no words entered OR all words already placed
- Places words in longest-to-shortest order to prevent blocking

**Start Button:**
- ENABLED when all words are placed on grid
- Subscribes to OnWordPlaced, OnWordDeleted, OnWordLengthsChanged events

### Word Entry Flow

1. **Select Word Row** - Click on a word row to make it active
2. **Type or Select Word** - Either:
   - Click letters in the Letter Tracker to spell word
   - Type on keyboard (triggers autocomplete dropdown)
   - Select from autocomplete suggestions (filtered by required word length)
3. **Auto-Accept** - Word automatically accepts when reaching required length
4. **Enter Placement Mode** - Click [compass] button (now active)
5. **Place Word on Grid** - Either:
   - Use "Place Random Positions" button to place all words at once
   - Click first cell, click second cell to set direction, remaining letters auto-fill

### Word Placement Flow (Coordinate Mode)

1. **Activate Coordinate Mode** - Click compass icon next to word row
2. **Hover Over Grid** - Visual feedback shows:
   - **Yellow cell** - Current cursor position (first letter placement)
   - **Green cells** - Valid positions for next letter (8 directions)
   - **Red cells** - Invalid positions (word wouldn't fit)
3. **Click First Cell** - Places first letter (displays immediately)
4. **Click Second Cell** - Determines direction (horizontal, vertical, diagonal, or backwards)
5. **Auto-Fill** - Remaining letters fill automatically in chosen direction
6. **Compass Hides** - After successful placement, compass button hides for that row

### Word Validation

- Words are validated against the word bank (25,000+ words)
- Invalid words are rejected with console message (toast UI deferred to Polish Phase)
- Validation happens when word reaches required length
- Case-insensitive matching (cat = CAT = Cat)

### Setup Completion

- All words must be entered and placed on grid
- Start button enables when setup is complete
- Player 2 then completes their setup
- Once both players ready, Gameplay Phase begins

---

## Phase 2: Gameplay Phase

### Gameplay Screen Layout

```
+-------------------------------------------------------------------+
|  PLAYER1        |  [guillotine]  [guillotine]  |        OPPONENT  |
|  1. L O P       |                              |  1. _ _ _        |
|  2. C R A X     |   PLAYER1                    |  2. _ _ _ _      |
|  3. S U R G Y   |   0 / 21                     |  3. _ _ _ _ _    |
|  4. H I L T E D |                              |  4. _ _ _ _ _ _  |
|                 |   OPPONENT                   |                  |
|  A B C D E F... |   0 / 21                     |  A B C D E F...  |
|  N O P Q R...   |                              |  N O P Q R...    |
|                 |                              |                  |
|  [grid 9x9]     |                              |  [grid 9x9]      |
|  with letters   |                              |  with hidden     |
|  revealed       |                              |  cells           |
+-------------------------------------------------------------------+
```

### Two-Panel Display

**Owner Panel (Left):**
- Shows YOUR words fully revealed
- Grid displays all YOUR placed letters
- Letter tracker shows which letters YOU have used
- Visual reference only - no interaction during opponent's turn

**Opponent Panel (Right):**
- Shows OPPONENT's words as patterns (underscores and discovered letters)
- Grid shows asterisks (*) for hit cells, revealed letters for guessed letters
- Letter tracker shows which letters have been guessed against opponent
- Click to make guesses during your turn

### Turn Actions

**Option 1: Guess a Letter**
- Click any letter in opponent's Letter Tracker section
- **If correct:** Letter highlights, added to known letters, asterisks upgrade to letters
- **If incorrect:** Miss counter increases by 1

**Option 2: Guess a Coordinate**
- Click any cell in opponent's grid
- **If hit:** Cell reveals as `*` (or actual letter if previously guessed)
- **If miss:** Miss counter increases by 1

**Option 3: Guess a Complete Word**
- **If correct:** Word pattern updates, asterisks upgrade to letters
- **If incorrect:** Counts as **2 misses** (double penalty)

---

## Game Mechanics

### Difficulty System (Implemented)

#### Configuration Options

**Grid Size (7 options):** 6x6, 7x7, 8x8, 9x9, 10x10, 11x11, 12x12

**Word Count:** 3 words (HARDER) or 4 words (EASIER)

**Difficulty Setting:** Easy, Normal, Hard

#### Miss Limit Formula (CRITICAL: Uses Opponent's Grid)

**Important:** Your miss limit is calculated using your OPPONENT's grid settings, not your own. This is because you are guessing against your opponent's grid, so their grid size and word count determine how hard it is for you to find their words.

```
MissLimit = Base + OpponentGridBonus + OpponentWordModifier + YourDifficultyModifier

Where:
  Base = 15
  OpponentGridBonus: 6x6=+3, 7x7=+4, 8x8=+6, 9x9=+8, 10x10=+10, 11x11=+12, 12x12=+13
  OpponentWordModifier: 3 words=+0, 4 words=-2
  YourDifficultyModifier: Easy=+4, Normal=+0, Hard=-4
```

**Example:**
- You chose "Hard" difficulty
- Opponent has 10x10 grid with 3 words
- Your miss limit = 15 + 10 + 0 + (-4) = 21 misses allowed

### Win/Lose Conditions

- **Win:** Reveal all opponent's letters OR opponent reaches their miss limit
- **Lose:** Reach your miss limit OR opponent reveals all your words

---

## Technical Specifications

### Core Systems Status

| System | Status |
|--------|--------|
| Grid System (6x6-12x12) | COMPLETE |
| Word Management | COMPLETE |
| Game State Manager | COMPLETE |
| Difficulty System | COMPLETE (opponent-based Dec 8) |
| Word Bank (25,000+ words) | COMPLETE |
| Letter Reveal Mechanics | COMPLETE |
| Word Validation | COMPLETE |
| AI Opponent | TODO |
| UI System - Setup Mode | COMPLETE |
| UI System - Gameplay Mode | IN PROGRESS (Dec 9) |

---

## Development Phases

### Phase 1: Core Mechanics - COMPLETE
### Phase 2: UI Implementation

**Setup Mode - COMPLETE (Dec 4, 2025):**
- PlayerGridPanel, GridCellUI, LetterButton prefabs
- SetupSettingsPanel with all controls
- Color picker (8 presets with highlight)
- Word entry and validation
- Coordinate placement mode with visual feedback
- Pick Random Words button (event-driven, fills only empty rows)
- Place Random Positions button (longest-to-shortest order)
- Grid clears when grid size changes
- Code refactoring: Controllers and Services extracted (Dec 5)
- Horizontal 50/50 layout with dynamic cell sizing (Dec 6)

**Gameplay Mode - IN PROGRESS (Dec 6-9, 2025):**
- [X] GameplayUIController with two-panel system
- [X] Owner panel shows player's words fully revealed
- [X] Opponent panel with hidden letter support (asterisks)
- [X] Data transfer from setup to gameplay
- [X] Grid initialization and word placement on gameplay panels
- [X] Miss limit calculation using opponent's grid settings
- [X] Guillotine graphics and miss counter display
- [X] Word pattern rows display correctly (CacheWordPatternRows fix)
- [X] Unused word rows hide properly based on word count
- [ ] Turn-based interaction (click to guess)

**Deferred to Polish Phase:**
- Invalid word feedback UI (toast/popup)

### Phase 3: AI Opponent - TODO
### Phase 4: Polish and Features - TODO
### Phase 5: Art and Theme - TODO

---

## Known Issues / TODO List

### Deferred to Polish Phase

| Issue | Notes |
|-------|-------|
| Invalid word feedback UI | Toast/popup for rejected words |
| Grid row labels compression | Labels don't resize with grid size changes |
| PlayerGridPanel further refactoring | Extract WordPatternRowManager, CoordinatePlacementController, etc. |

### Design Considerations

| Item | Notes |
|------|-------|
| Word count difficulty | 3 words HARDER than 4 - formula accounts for this with -2 modifier for 4 words |
| Miss limit source | Uses OPPONENT's grid settings + YOUR difficulty preference |

---

## Version History

- **v1.8** (Dec 9, 2025): Unity 6.3, Gameplay Mode UI functional, guillotines displayed, word rows working
- **v1.7** (Dec 8, 2025): Gameplay Mode UI progress, opponent-based miss limits, horizontal layout, package cleanup
- **v1.6** (Dec 5, 2025): Code refactoring (Controllers/Services), difficulty dropdown order fixed, documentation update
- **v1.5** (Dec 4, 2025): Setup Mode complete, button state management, documentation update
- **v1.4** (Dec 2, 2025): Major bug fixes and UI completion
- **v1.3** (Nov 28, 2025): Major UI design update
- **v1.2** (Nov 25, 2025): Major implementation update
- **v1.1** (Nov 22, 2025): Playtesting updates
- **v1.0** (Nov 20, 2025): Initial GDD

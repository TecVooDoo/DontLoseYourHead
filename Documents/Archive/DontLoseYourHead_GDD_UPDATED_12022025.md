# Don't Lose Your Head - Game Design Document

**Version:** 1.4  
**Date:** November 20, 2025  
**Last Updated:** December 2, 2025  
**Developer:** TecVooDoo LLC  
**Designer:** Rune (Stephen Brandon)  
**Platform:** Unity 6.2 (2D)  
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

### Setup Screen Layout

```
+--------------------------------------------------+
| [MAIN MENU]      SETUP        [START]            |
+--------------------------------------------------+
| PLAYER NAME [____] [pencil]  | GRID SIZE    [8x8 v] |
| PLAYER COLOR [8 buttons]     | # OF WORDS   [3   v] |
| [PICK RANDOM WORDS]          | DIFFICULTY   [Normal v] |
|                              | MISS LIMIT: 21        |
+--------------------------------------------------+
|                  PLAYER 1         [pencil][eraser]|
+--------------------------------------------------+
| 1. C A T                          [+] [compass] [X] |
| 2. R A C K                        [+] [compass] [X] |
| 3. _ _ _ _ _                      [+] [compass] [X] |
+--------------------------------------------------+
| A  B  C  D  E  F  G  H  I  J  K  L  M            |
| N  O  P  Q  R  S  T  U  V  W  X  Y  Z            |
+--------------------------------------------------+
| [compass] A  B  C  D  E  F  G  H                 |
|        1 [  ][  ][  ][  ][  ][  ][  ][  ]        |
|        2 [  ][  ][  ][  ][  ][  ][  ][  ]        |
|        3 [  ][  ][  ][  ][  ][  ][  ][  ]        |
|        4 [C ][  ][  ][  ][  ][  ][  ][  ]        |
|        5 [  ][A ][  ][  ][  ][  ][  ][  ]        |
|        6 [  ][  ][T ][  ][  ][  ][  ][  ]        |
|        7 [  ][  ][  ][  ][  ][  ][  ][  ]        |
|        8 [  ][  ][  ][  ][  ][  ][  ][  ]        |
+--------------------------------------------------+
```

### Setup UI Components

**Top Bar:**
- Main Menu button - returns to main menu
- "SETUP" label - current phase indicator
- Start button - begins gameplay (disabled until all words placed)

**Settings Panel (Right Side):**
- Grid Size dropdown: 6x6, 7x7, 8x8, 9x9, 10x10, 11x11, 12x12
- # of Words dropdown: 3, 4
- Difficulty dropdown: Hard, Normal, Easy (formerly "Forgiveness")
- Miss Limit display - updates dynamically based on settings
- All settings editable until Start is pressed

**Player Configuration (Left Side):**
- Player Name - editable text field with pencil icon
- Player Color - 8 preset color buttons with selection highlight
- "Pick Random Words" button - auto-selects valid words from word bank

**Word Entry Section:**
- Player name header with edit/delete icons
- Word rows (3 or 4 depending on settings):
  - Row number (1, 2, 3, 4)
  - Word display (underscores or letters)
  - Accept button [+] - confirms word entry (auto-triggers at correct length)
  - Coordinate Mode button [compass] - enters placement mode (hides after placement)
  - Delete button [X] - clears the word from row AND grid

**Letter Tracker / Input Row:**
- Two rows: A-M and N-Z
- Doubles as keyboard for word input
- Letters highlight when used in placed words
- Click letters to type words OR use physical keyboard
- Also routes to player name input when that field is focused

**Grid Section:**
- Compass icon in top-left corner - random placement for current word
- Column labels (A-L depending on grid size)
- Row labels (1-12 depending on grid size)
- Grid cells showing placed letters
- During placement mode: Yellow = current position, Green = valid next positions, Red = invalid

### Word Entry Flow

1. **Select Word Row** - Click on a word row to make it active
2. **Type or Select Word** - Either:
   - Click letters in the Letter Tracker to spell word
   - Type on keyboard (triggers autocomplete dropdown)
   - Select from autocomplete suggestions (filtered by required word length)
3. **Auto-Accept** - Word automatically accepts when reaching required length
4. **Enter Placement Mode** - Click [compass] button (now active)
5. **Place Word on Grid** - Either:
   - Click compass in grid corner for random valid placement
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
- Invalid words are rejected with feedback message
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
|  ROGER            |  [guillotine]  [guillotine]  |           MARY  |
|  1. _ _ _         |                              |  1. _ _ _ _     |
|  2. _ _ _ _       |   ROGER                      |  2. _ _ _ _ _   |
|  3. _ _ _ _ _     |   MISSES: 0 / 21             |  3. _ _ _ _ _ _ |
|  4. _ _ _ _ _ _   |                              |  4. _ _ _ _ _ _ |
|                   |   MARY                       |                 |
|  A B C D E F G H  |   MISSES: 0 / 25             |  A B C D E F G H|
|  I J K L M        |                              |  I J K L M      |
|  N O P Q R S T U  |                              |  N O P Q R S T U|
|  V W X Y Z        |                              |  V W X Y Z      |
|                   |                              |                 |
|  [compass] A-L    |                              |  [compass] A-H  |
|  1-12 grid        |                              |  1-8 grid       |
|  (12x12)          |                              |  (8x8)          |
+-------------------------------------------------------------------+
```

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

**Difficulty Setting:** Hard, Normal, Easy

#### Miss Limit Formula

```
MissLimit = Base + GridBonus + WordModifier + DifficultyModifier

Where:
  Base = 15
  GridBonus: 6x6=+3, 7x7=+4, 8x8=+6, 9x9=+8, 10x10=+10, 11x11=+12, 12x12=+13
  WordModifier: 3 words=+0, 4 words=-2
  DifficultyModifier: Hard=-4, Normal=+0, Easy=+4
```

### Win/Lose Conditions

- **Win:** Reveal all opponent's letters OR opponent reaches miss limit
- **Lose:** Reach your miss limit OR opponent reveals all your words

---

## Technical Specifications

### Core Systems Status

| System | Status |
|--------|--------|
| Grid System (6x6-12x12) | COMPLETE |
| Word Management | COMPLETE |
| Game State Manager | COMPLETE |
| Difficulty System | COMPLETE |
| Word Bank (25,000+ words) | COMPLETE |
| Letter Reveal Mechanics | COMPLETE |
| Word Validation | COMPLETE |
| AI Opponent | TODO |
| UI System | ~85% COMPLETE |

---

## Development Phases

### Phase 1: Core Mechanics - COMPLETE
### Phase 2: UI Implementation - ~85% COMPLETE

**Completed:**
- PlayerGridPanel, GridCellUI, LetterButton prefabs
- SetupSettingsPanel with all controls
- Color picker (8 presets with highlight)
- Word entry and validation
- Coordinate placement mode

**In Progress:**
- Invalid word feedback UI (toast/popup)
- Random word placement (compass in grid corner)

**TODO:**
- Gameplay screen layout
- Turn-based interaction

### Phase 3: AI Opponent - TODO
### Phase 4: Polish and Features - TODO
### Phase 5: Art and Theme - TODO

---

## Known Issues / TODO List

### Bugs to Fix

| Issue | Priority |
|-------|----------|
| Grid row labels compression on size change | LOW (Phase 4) |
| Grid not clearing when size changed (words placed) | MEDIUM |

### Features to Implement

| Feature | Priority |
|---------|----------|
| Invalid word feedback UI (toast/popup) | HIGH |
| Random word placement (compass button) | MEDIUM |
| Rename Forgiveness to Difficulty (Hard/Normal/Easy) | LOW |

### Design Considerations

| Item | Notes |
|------|-------|
| Word count difficulty | 3 words HARDER than 4 - formula accounts for this with -2 modifier for 4 words |

---

## Version History

- **v1.4** (Dec 2, 2025): Major bug fixes and UI completion
- **v1.3** (Nov 28, 2025): Major UI design update
- **v1.2** (Nov 25, 2025): Major implementation update
- **v1.1** (Nov 22, 2025): Playtesting updates
- **v1.0** (Nov 20, 2025): Initial GDD

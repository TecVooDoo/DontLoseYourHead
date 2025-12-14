# Don't Lose Your Head - Game Design Document

**Version:** 1.3  
**Date:** November 20, 2025  
**Last Updated:** November 28, 2025  
**Developer:** TecVooDoo LLC  
**Designer:** Rune (Stephen Brandon)  
**Platform:** Unity 6.2 (2D)  
**Target:** PC/Mobile  

---

## High Concept

A competitive word game combining Hangman's letter-guessing with Battleship's grid-based hidden information. Players place hidden words on grids and take turns guessing letters or coordinates to find their opponent's words before the guillotine blade falls.

**Key Innovation:** Asymmetric difficulty system allows mixed-skill players (parent/child, veteran/newbie) to compete fairly by choosing different grid sizes, word counts, and forgiveness settings.

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
| PLAYER NAME [____] [pencil]  | GRID SIZE    [6x6 v] |
| PLAYER COLOR [palette]       | # OF WORDS   [3   v] |
| [PICK RANDOM WORDS]          | FORGIVENESS  [---  v] |
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
- Grid Size dropdown: 6x6, 8x8, 10x10
- # of Words dropdown: 3, 4
- Forgiveness dropdown: Strict, Normal, Forgiving
- All settings editable until Start is pressed

**Player Configuration (Left Side):**
- Player Name - editable text field with pencil icon
- Player Color - color picker (avoid white, red, black, grey)
- "Pick Random Words" button - auto-selects valid words from word bank

**Word Entry Section:**
- Player name header with edit/delete icons
- Word rows (3 or 4 depending on settings):
  - Row number (1, 2, 3, 4)
  - Word display (underscores or letters)
  - Accept button [+] - confirms word entry
  - Coordinate Mode button [compass] - enters placement mode (active only after word chosen)
  - Delete button [X] - clears the word

**Letter Tracker / Input Row:**
- Two rows: A-M and N-Z
- Doubles as keyboard for word input
- Letters highlight when used in placed words
- Click letters to type words OR use autocomplete dropdown

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
3. **Confirm Word** - Click [+] accept button (word displays in ALL CAPS)
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
3. **Click First Cell** - Places first letter
4. **Click Second Cell** - Determines direction (horizontal, vertical, diagonal, or backwards)
5. **Auto-Fill** - Remaining letters fill automatically in chosen direction
6. **Confirm or Retry** - Word appears on grid; click [X] to clear and retry

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
|  3. _ _ _ _ _     |   MISSES: 0                  |  3. _ _ _ _ _ _ |
|  4. _ _ _ _ _ _   |                              |  4. _ _ _ _ _ _ |
|                   |   MARY                       |                 |
|  A B C D E F G H  |   MISSES: 0                  |  A B C D E F G H|
|  I J K L M        |                              |  I J K L M      |
|  N O P Q R S T U  |                              |  N O P Q R S T U|
|  V W X Y Z        |                              |  V W X Y Z      |
|                   |                              |                 |
|  [compass] A-L    |                              |  [compass] A-H  |
|  1-12 grid        |                              |  1-8 grid       |
|  (12x12)          |                              |  (8x8)          |
+-------------------------------------------------------------------+
```

### Gameplay UI Components

**Two PlayerGridPanels (Left and Right):**
Each panel contains:
- Player name header (colored with player's chosen color)
- Word patterns display (shows _ _ _ for unknown, letters for known)
- Letter Tracker (A-M row, N-Z row) - shows guessed letters
- Grid with column/row labels
- Grid sized according to player's difficulty settings

**Center Section:**
- Two guillotine visuals (one per player)
- Miss counters for both players ("MISSES: X / Y")
- Turn indicator (whose turn it is)
- Visual feedback for hits/misses

### Turn Actions

**Option 1: Guess a Letter**
- Click any letter in opponent's Letter Tracker section
- **If correct:**
  - Letter highlights in opponent's tracker
  - Letter is added to "known letters" list
  - Does NOT reveal position on grid
  - Any previously revealed `*` on grid that matches this letter updates to show the letter
- **If incorrect:**
  - Miss counter increases by 1
  - Guillotine blade raises one notch

**Option 2: Guess a Coordinate**
- Click any cell in opponent's grid
- **If hit (cell contains a letter):**
  - Cell reveals as `*` (asterisk)
  - If that letter was previously guessed, shows actual letter instead of `*`
- **If miss (empty cell):**
  - Cell shows as empty/crossed out
  - Miss counter increases by 1
  - Guillotine blade raises one notch

**Option 3: Guess a Complete Word**
- Type or select a word to guess
- **If correct:**
  - Word is added to "found words" list
  - Word pattern updates to show all letters
  - Any `*` symbols on grid that belong to this word update to show actual letters
  - Must still find word's location through coordinate guesses to win
- **If incorrect:**
  - Counts as **2 misses** (double penalty for wrong word guess)
  - Does NOT reveal any information about letters

---

## Shared UI Component: PlayerGridPanel

The same PlayerGridPanel prefab is used in both Setup and Gameplay phases, configured differently:

### Setup Mode Configuration
- Single panel visible (current player only)
- Grid shows player's own placed words (visible)
- Letter Tracker doubles as input keyboard
- Coordinate mode enabled for word placement
- Compass icons active

### Gameplay Mode Configuration
- Two panels visible (both players)
- Own grid shows your words (visible to you)
- Opponent grid shows guessed cells only
- Letter Tracker shows guessed letters (not input)
- Click on opponent's elements to make guesses

### PlayerGridPanel Structure
```
PlayerGridPanel
  - PlayerNameLabel (Image + Text)
  - WordPatternsContainer
    - WordPattern_1 (Image + Text): "1. _ _ _"
    - WordPattern_2 (Image + Text): "2. _ _ _ _"
    - WordPattern_3 (Image + Text): "3. _ _ _ _ _"
    - WordPattern_4 (Image + Text): "4. _ _ _ _ _ _" (hidden if 3 words)
  - LetterTrackerContainer
    - LetterRow_1: A B C D E F G H I J K L M
    - LetterRow_2: N O P Q R S T U V W X Y Z
  - ColumnLabelsContainer
    - Spacer + Label_A through Label_L (hide unused)
  - GridWithRowLabels
    - RowLabelsContainer: Label_1 through Label_12 (hide unused)
    - GridContainer: 12x12 cells (hide unused based on grid size)
```

---

## Playtesting Insights

### Excel Prototype Testing (November 2025)

Real playtesting with spouse using Excel sheets revealed critical design insights:

**Major Discovery #1: Miss Limits Too Restrictive**
- Original design: 8-12 misses
- Reality: First playtest required 25 misses to solve
- **Conclusion:** Miss limits need 2-3x increase from original concept

**Major Discovery #2: Counterintuitive Word Density**
- Original assumption: More words = harder game
- Reality: **More words = EASIER game!**
- Reason: Higher letter density = more hits, fewer empty spaces to miss
- Impact: Word count now a difficulty variable (fewer words = harder)

**Positive Validations:**
- Core mechanics are fun and engaging
- Strategic depth confirmed (letter vs coordinate decisions matter)
- Player skill improves over multiple sessions
- Game loop works as intended

### Claude Playtest (November 24, 2025)

Live playtest conducted between Rune and Claude using Excel tracking:

**Game Configuration:**
- 8x8 grid
- 3 words each (3, 4, and 5 letters)
- 23 miss limit

**Results:**
- 25 turns (50 total moves) before game ended early due to coordinate confusion
- Both players: 16/23 misses (70% of limit)
- Move distribution: 72% letter guesses, 26% coordinate guesses, 2% word guesses
- All words successfully identified by both players

**Key UX Discoveries:**
- Coordinate tracking becomes extremely difficult without visual aids
- Letter pattern tracking across multiple words creates cognitive overload
- Color coding essential for distinguishing player elements
- Need integrated letter displays tied to each grid

**Design Validations:**
- 23-miss limit for 8x8 grid feels appropriate
- Letter guessing is dominant early-game strategy
- Word guessing is high-risk (Claude's wrong "RAT" guess cost 2 misses)
- Progressive information reveal creates satisfying tension

**Full playtest data available in:** DLYH_Claude_Playtest_Game_Data_Complete.pdf

---

## Game Mechanics

### Word Selection and Placement

**Word Requirements (3-Word Configuration):**
- 1 word of 3 letters
- 1 word of 4 letters  
- 1 word of 5 letters
- Total: 12 letters per player (before overlapping)

**Word Requirements (4-Word Configuration):**
- 1 word of 3 letters
- 1 word of 4 letters
- 1 word of 5 letters
- 1 word of 6 letters
- Total: 18 letters per player (before overlapping)

**Placement Rules:**
- Words can be placed in any orientation:
  - Horizontal (left-to-right)
  - Vertical (top-to-bottom)
  - Diagonal (any direction)
  - Backwards (any of the above reversed)
- **Overlapping is allowed** when words share the same letter at the same grid position
- Words must fit entirely within the grid boundaries
- Autocomplete validates words against word bank during setup

### Difficulty System (Implemented)

#### Configuration Options

**Grid Size:**
- 6x6 (36 cells) - Small/Easy
- 8x8 (64 cells) - Medium
- 10x10 (100 cells) - Large/Hard

**Word Count:**
- 3 words (3-4-5 letter words)
- 4 words (3-4-5-6 letter words)

**Forgiveness Setting:**
- Strict: For experienced players wanting challenge
- Normal: Default balanced experience
- Forgiving: For new players, kids, casual play

#### Miss Limit Formula (Implemented)

```
MissLimit = Base + GridBonus + WordModifier + ForgivenessModifier

Where:
  Base = 15

  GridBonus:
    6x6:   +3 misses
    8x8:   +6 misses
    10x10: +10 misses

  WordModifier:
    3 words: +0 misses (baseline)
    4 words: -2 misses (more letters = easier)

  ForgivenessModifier:
    Strict:    -4 misses
    Normal:    +0 misses
    Forgiving: +4 misses
```

**Example Calculations:**

| Grid | Words | Forgiveness | Calculation | Miss Limit |
|------|-------|-------------|-------------|------------|
| 6x6 | 3 | Normal | 15 + 3 + 0 + 0 | 18 |
| 6x6 | 3 | Forgiving | 15 + 3 + 0 + 4 | 22 |
| 8x8 | 3 | Normal | 15 + 6 + 0 + 0 | 21 |
| 8x8 | 3 | Strict | 15 + 6 + 0 - 4 | 17 |
| 10x10 | 4 | Normal | 15 + 10 - 2 + 0 | 23 |
| 10x10 | 3 | Forgiving | 15 + 10 + 0 + 4 | 29 |

#### Asymmetric Difficulty

Each player can independently choose:
- Their grid size
- Their word count
- Their forgiveness setting

This enables:
- Parent/child play (parent on Strict, child on Forgiving)
- Veteran vs newcomer matches
- Self-balancing as players improve
- Different grid sizes displayed side-by-side (e.g., 12x12 vs 8x8)

### Win/Lose Conditions

**Win Conditions:**
1. **Complete Discovery:** Reveal all letters of all opponent's words on the grid
2. **Opponent Elimination:** Opponent reaches their miss limit first

**Lose Conditions:**
1. Reach the miss limit for your chosen difficulty
2. Opponent reveals all your words first

---

## Technical Specifications

### Core Systems (All Completed)

1. **Grid System** [COMPLETED]
   - Dynamic grid generation (6x6, 8x8, 10x10)
   - Cell coordinate system (A-J, 0-9)
   - Cell state management
   - Word placement validation

2. **Word Management System** [COMPLETED]
   - Word placement validator
   - Overlap detection and validation
   - Boundary checking
   - Word reveal tracking

3. **Game State Manager** [COMPLETED]
   - Turn tracking (TurnManager)
   - Miss counting per player
   - Win/lose condition checking
   - Game flow control (GameStateMachine)
   - Player state management (PlayerManager)

4. **Difficulty System** [COMPLETED]
   - DifficultyEnums (GridSizeOption, WordCountOption, ForgivenessSetting)
   - DifficultyCalculator with dynamic miss limit formula
   - DifficultySO ScriptableObject assets

5. **Word Bank** [COMPLETED]
   - WordListSO ScriptableObject
   - WordBankImporter editor tool
   - 25,237 filtered words total

6. **Letter Reveal Mechanics** [COMPLETED]
   - Coordinate guess reveals asterisk
   - Letter guess adds to known letters
   - Asterisks upgrade to actual letters when learned

7. **AI Opponent** [TODO]
   - Word selection algorithm
   - Word placement logic
   - Guessing strategy
   - Difficulty scaling

8. **UI System** [IN PROGRESS]
   - PlayerGridPanel (scalable 6x6 to 12x12) [COMPLETED]
   - Setup mode UI [IN PROGRESS]
   - Gameplay UI [TODO]
   - Visual feedback and animations [TODO]

---

## Data Resources

### Word Bank (Implemented)

**Source:** dwyl/english-words GitHub repository
- **File:** words_alpha.txt
- **License:** MIT (free to use)
- **URL:** https://github.com/dwyl/english-words

**Filtered Word Counts:**
| Length | Count |
|--------|-------|
| 3-letter | 2,130 |
| 4-letter | 7,186 |
| 5-letter | 15,921 |
| 6-letter | Available |
| **Total** | **25,237+** |

**Implementation:**
- WordBankImporter editor tool (DLYH > Tools menu)
- Generates WordListSO ScriptableObject assets
- Filters for alphabetic characters only
- Converts to uppercase

---

## Development Phases

### Phase 1: Core Mechanics (MVP) - COMPLETE

**All Completed:**
- [X] Folder structure (Assets/DLYH/)
- [X] Grid system with 3 sizes
- [X] Word placement (manual) with all orientations
- [X] Letter guessing
- [X] Coordinate guessing
- [X] Word guessing with 2-miss penalty
- [X] Miss counting
- [X] Win/lose detection
- [X] Turn management (TurnManager)
- [X] Player system (PlayerSO, PlayerManager)
- [X] Game flow state machine (6 phases)
- [X] ScriptableObject architecture
- [X] Difficulty system with enums and calculator
- [X] Word bank integration (25,000+ words)
- [X] Letter reveal mechanics (asterisk -> letter)

### Phase 2: UI Implementation - IN PROGRESS

**Completed:**
- [X] PlayerGridPanel prefab (scalable 6x6 to 12x12)
- [X] GridCellUI prefab
- [X] LetterButton prefab
- [X] AutocompleteItem prefab
- [X] Canvas setup (Scale With Screen Size, 1080x1920)

**In Progress:**
- [ ] Setup mode screen layout
- [ ] Settings panel (dropdowns for difficulty)
- [ ] Word entry with autocomplete
- [ ] Coordinate placement mode (yellow/green/red feedback)

**TODO:**
- [ ] Gameplay screen layout (two PlayerGridPanels)
- [ ] Turn-based interaction
- [ ] Visual feedback (hit/miss indicators)
- [ ] Toast notifications (Easy Popup System)

### Phase 3: AI Opponent

- [ ] AI word selection
- [ ] AI word placement  
- [ ] AI guessing strategy (basic)
- [ ] Difficulty scaling

### Phase 4: Polish and Features

- [ ] Animations (DOTween)
- [ ] Enhanced visual feedback
- [ ] Sound effects
- [ ] Tutorial/how-to-play

### Phase 5: Art and Theme

- [ ] Replace placeholder art
- [ ] Medieval carnival theme
- [ ] Character avatars
- [ ] Guillotine animations
- [ ] Background art
- [ ] UI skinning

---

## Asset Requirements

### Core Toolkit (Minimal)

**Primary Tools:**
- Odin Inspector and Validator (data management, validation)
- DOTween Pro (all animations and tweening)
- Easy Popup System (popups, toasts, dialogs)
- SOAP (ScriptableObject architecture patterns)

**Development Tools:**
- MCP for Unity (Claude integration)
- Scriptable Sheets (future Google Sheets integration)

**Unity Built-ins:**
- TextMeshPro (all text rendering)
- Unity UI (UGUI) (Canvas, Image, Button)
- New Input System (all input handling)

### Deferred to Polish Phase

- UI Toolkit Filters (requires Unity 6.3+)
- GUI Art Packs (pick one: Fantasy RPG, Classic RPG, or Cartoon)

---

## Design Philosophy

### Development Priorities

1. **Backend First:** Complete mechanics and logic before visual polish
2. **Placeholder Graphics:** Use simple shapes/colors for testing
3. **Ship Fast:** Get playable prototype done quickly
4. **Core Loop:** Nail the fun before adding features
5. **Iterative:** Build -> Test -> Refine
6. **Scope Control:** Resist feature creep

### Success Metrics

**Technical:**
- Code follows SOLID principles
- ScriptableObject architecture implemented
- All assets used appropriately
- No deprecated API usage
- Clean, maintainable code

**Design:**
- Core loop is engaging
- Strategic depth (letter vs coordinate decisions matter)
- Balanced difficulty progression
- Asymmetric difficulty enables mixed-skill play

**Playtesting:**
- Games complete in 5-15 minutes
- Players improve with practice
- Miss limits feel fair
- AI provides appropriate challenge

---

## Version History

- **v1.3** (Nov 28, 2025): Major UI design update
  - Added detailed Setup Phase and Gameplay Phase documentation
  - Clarified two-phase game flow (Setup then Gameplay)
  - Documented shared PlayerGridPanel component usage
  - Added Setup screen layout with all UI elements
  - Added Gameplay screen layout with two-panel view
  - Documented word entry flow with autocomplete
  - Documented coordinate placement mode (yellow/green/red feedback)
  - Updated development phase status

- **v1.2** (Nov 25, 2025): Major implementation update
  - Added Claude playtest results and analysis
  - Updated difficulty system to reflect actual implementation
  - Added actual word bank counts (25,237 words)
  - Updated development phases (Phase 1 COMPLETE)
  - Simplified asset requirements to minimal toolkit
  - Added 6-letter word support documentation

- **v1.1** (Nov 22, 2025): Major update based on playtesting
  - Added playtesting insights section
  - Introduced hybrid difficulty system concept
  - Updated miss limits based on real gameplay data
  - Added word bank information
  - Documented asymmetric difficulty innovation

- **v1.0** (Nov 20, 2025): Initial GDD created

---

## Notes

- This is a living document updated as development progresses
- Design decisions may change based on playtesting feedback
- Scope is intentionally kept small for fast completion
- **Phase 1 complete, Phase 2 UI implementation in progress**
- Real playtesting (Excel prototype + Claude playtest) validated core mechanics
- PlayerGridPanel is the core reusable component for both phases

# Don't Lose Your Head - Game Design Document

**Version:** 3.1  
**Date:** November 20, 2025  
**Last Updated:** December 13, 2025  
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

The game consists of three distinct phases:

### Main Menu Phase
Players can start a new game, access settings, or exit.

### Phase 1: Setup Phase (Per-Player)
Each player configures their settings and places words on their grid. Players take turns in setup - Player 1 completes setup, then Player 2 (or AI).

### Phase 2: Gameplay Phase (Alternating Turns)
Both players' grids are visible. Players alternate turns guessing letters, coordinates, or words to find their opponent's hidden words.

---

## Main Menu

### Layout
```
+----------------------------------+
|       DON'T LOSE YOUR HEAD       |
|                                  |
|         [  NEW GAME  ]           |
|         [  SETTINGS  ]           |
|         [    EXIT    ]           |
+----------------------------------+
```

### Buttons
- **New Game:** Transitions to Setup Phase for Player 1
- **Settings:** Opens Settings Panel overlay
- **Exit:** Quits the application

---

## Settings Panel

### Layout
```
+----------------------------------+
|          SETTINGS                |
|                                  |
|  Sound Effects  [====|====] 50%  |
|  Music          [====|====] 50%  |
|                                  |
|         [   BACK   ]             |
+----------------------------------+
```

### Controls
- **Sound Effects Slider:** 0-100%, default 50%
- **Music Slider:** 0-100%, default 50%
- **Back Button:** Returns to Main Menu

### Persistence
- Settings saved to PlayerPrefs
- Values persist between sessions

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
|   [autocomplete dropdown]|                                       |
+------------------------------------------------------------------+
```

### Autocomplete Feature
- Dropdown appears below active word row during typing
- Shows up to 5 matching words from word bank
- Click suggestion to auto-fill word
- Hides on: word completion, acceptance, Pick Random Words, mode transition

### Setup Completion

- All words must be entered and placed on grid
- Start button enables when setup is complete
- Player 2 (or AI) then completes their setup
- Once both players ready, Gameplay Phase begins

---

## Phase 2: Gameplay Phase

### Gameplay Screen Layout

```
+-------------------------------------------------------------------+
|  PLAYER1        |  [guillotine]  [guillotine]  |        OPPONENT  |
|  1. L O P       |                              |  1. _ _ _        |
|  2. C R A X     |   PLAYER1      OPPONENT      |  2. _ _ _ _      |
|  3. S U R G Y   |   0 / 21       0 / 21        |  3. _ _ _ _ _    |
|  4. H I L T E D |                              |  4. _ _ _ _ _ _  |
|                 |   [word list]  [word list]   |                  |
|  A B C D E F... |                              |  A B C D E F...  |
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
- Visual reference only during your turn

**Opponent Panel (Right):**
- Shows OPPONENT's words as patterns (underscores and discovered letters)
- Grid shows colored cells (green/red/yellow) for guessed coordinates
- Letter tracker shows which letters have been guessed against opponent
- Click to make guesses during your turn

### Turn Actions

**Option 1: Guess a Letter**
- Click any letter in opponent's Letter Tracker section
- **If correct:** Letter highlights green, asterisks upgrade to letters in word patterns
- **If incorrect:** Miss counter increases by 1
- **If already guessed:** "Already guessed" message, try again (no penalty)

**Option 2: Guess a Coordinate**
- Click any cell in opponent's grid
- **If hit (letter known):** Cell turns green, reveals letter
- **If hit (letter unknown):** Cell turns yellow, shows asterisk
- **If miss:** Cell turns red, miss counter increases by 1
- **If already guessed:** "Already guessed" message, try again (no penalty)

**Option 3: Guess a Complete Word**
- Click "Guess Word" button on a word pattern row
- Letter tracker converts to keyboard mode (all buttons white)
- Type letters using letter tracker or physical keyboard
- Discovered letters stay fixed (can't be typed over)
- **Accept:** Validates word, if valid processes guess
  - **If correct:** Word reveals, yellow cells upgrade to green, button permanently hidden
  - **If incorrect:** Counts as **2 misses** (double penalty)
- **Cancel:** Clears typed letters, exits guess mode
- **Invalid word:** "Not a valid word" message, try again (no penalty)

### Three-Color Grid Cell System

| Color | Meaning | Visual |
|-------|---------|--------|
| Green | Hit - letter known | Green background + revealed letter |
| Red | Miss - empty cell | Red background |
| Yellow | Hit - letter unknown | Yellow/orange background + asterisk |

**Upgrade Flow:** Yellow cells automatically upgrade to green when the letter is discovered through a letter guess.

**Important:** Grid cells are ONLY revealed through coordinate guesses. Correctly guessing a word does NOT reveal the grid cells - it only updates the word pattern and letter tracker. This preserves strategic gameplay.

### Guessed Word Lists

Each player has a scrollable list under their guillotine showing:
- All words guessed (both correct and incorrect)
- Green background = correct guess
- Red background = incorrect guess

### Auto-Hide Guess Word Buttons

When a word is fully revealed through letter guessing (all letters discovered), the "Guess Word" button automatically hides since there's nothing left to guess.

---

## Game Mechanics

### Difficulty System (Implemented)

#### Configuration Options

**Grid Size (7 options):** 6x6, 7x7, 8x8, 9x9, 10x10, 11x11, 12x12

**Word Count:** 3 words (HARDER) or 4 words (EASIER)

**Difficulty Setting:** Easy, Normal, Hard

#### Miss Limit Formula (Uses Opponent's Grid)

**Important:** Your miss limit is calculated using your OPPONENT's grid settings, not your own. This is because you are guessing against your opponent's grid, so their grid size and word count determine how hard it is for you to find their words.

```
MissLimit = Base + OpponentGridBonus + OpponentWordModifier + YourDifficultyModifier

Where:
  Base = 15
  OpponentGridBonus: 6x6=+3, 7x7=+4, 8x8=+6, 9x9=+8, 10x10=+10, 11x11=+12, 12x12=+13
  OpponentWordModifier: 3 words=+0, 4 words=-2
  YourDifficultyModifier: Easy=+4, Normal=+0, Hard=-4
```

### Win/Lose Conditions

- **Win:** Reveal all opponent's letters OR opponent reaches their miss limit
- **Lose:** Reach your miss limit OR opponent reveals all your words

---

## Phase 3: AI Opponent - "The Executioner"

### Overview

The Executioner is an adaptive AI opponent that:
1. Makes intelligent guesses based on probability analysis
2. Adjusts difficulty via rubber-banding based on player performance
3. Adapts its own adaptation rate based on player trends
4. Feels human with variable think times and imperfect memory

### AI Turn Flow

```
1. Wait (human-like think time: 0.8-2.5 seconds, randomized)
2. Check for high-confidence word guess opportunities
3. If no word guess, choose between letter or coordinate guess
4. Execute guess and update game state
5. End turn
```

### Strategy 1: Letter Guessing

**Goal:** Pick the letter most likely to reveal information.

**Algorithm:**
1. Get list of unguessed letters
2. Score each letter by:
   - English frequency weight (E=12.7%, T=9.1%, A=8.2%, etc.)
   - Bonus if letter could complete partially-revealed word patterns
3. Sort by score descending
4. Apply skill modifier to pick from top N candidates

**Skill Impact:**
| Skill Level | Selection Pool |
|-------------|----------------|
| 0.2 (Easy) | Top 10 or random |
| 0.5 (Normal) | Top 5 |
| 0.8 (Hard) | Top 2 |
| 0.95 (Expert) | Optimal choice |

### Strategy 2: Coordinate Guessing

**Goal:** Pick the cell most likely to contain a letter.

**Algorithm:**
1. Get list of unguessed coordinates
2. Score each coordinate by:
   - Adjacency bonus (next to known hits)
   - Line extension bonus (extends a row/column of hits)
   - Center bias (longer words pass through center)
3. Apply skill modifier

**Grid Density Impact:**

| Grid | Cells | 3 Words | 4 Words | Fill Ratio |
|------|-------|---------|---------|------------|
| 6x6 | 36 | ~14 letters | ~18 letters | 39-50% |
| 8x8 | 64 | ~14 letters | ~18 letters | 22-28% |
| 10x10 | 100 | ~14 letters | ~18 letters | 14-18% |
| 12x12 | 144 | ~14 letters | ~18 letters | 10-13% |

**Strategy Preference by Density:**
- High density (>35% fill): Favor coordinate guessing
- Medium density (12-35%): Balanced approach
- Low density (<12%): Strongly favor letter guessing

### Strategy 3: Word Guessing

**Goal:** Decide IF and WHAT word to guess.

**Risk:** Wrong word guess costs 2 misses.

**Algorithm:**
1. For each unsolved word pattern, find matching words in bank
2. Calculate confidence:
   - 1 match = 95% confidence
   - 2-3 matches = 50-33% confidence
   - 4+ matches = Low confidence
3. Compare confidence to skill-based threshold
4. If above threshold, attempt guess

**Confidence Thresholds by Skill:**
| Skill Level | Min Confidence to Attempt |
|-------------|---------------------------|
| 0.2 (Easy) | 90%+ (rarely attempts) |
| 0.5 (Normal) | 70%+ |
| 0.8 (Hard) | 50%+ |
| 0.95 (Expert) | 30%+ (risk-taker) |

### Rubber-Banding System

**Purpose:** Keep games competitive regardless of skill gap.

**Initial Settings by Player Difficulty:**

| Player's Difficulty | AI Starting Skill | Hits to Increase AI | Misses to Decrease AI |
|--------------------|-------------------|---------------------|----------------------|
| Easy | 0.25 | 5 | 2 |
| Normal | 0.50 | 3 | 3 |
| Hard | 0.75 | 2 | 5 |

**Skill Adjustment:**
- When player gets N consecutive hits, AI skill increases by 0.15
- When player gets N consecutive misses, AI skill decreases by 0.15

**Skill Bounds:**
- Minimum: 0.15 (never completely random)
- Maximum: 0.95 (never perfect)

### Adaptive Threshold System

**Purpose:** The rubber-banding system itself adapts if the player is consistently struggling or dominating.

**Tracking:** Count consecutive skill adjustments in the same direction.

**Adaptation Rules (after 2+ same-direction adjustments):**

| Player Pattern | Threshold Change | Effect |
|----------------|------------------|--------|
| Struggling (AI decreased 2+ times) | HitsToIncrease +1, MissesToDecrease -1 | Protects player |
| Dominating (AI increased 2+ times) | HitsToIncrease -1, MissesToDecrease +1 | Challenges player |

**Threshold Bounds:**
- HitsToIncrease: 1 to 7
- MissesToDecrease: 1 to 7

**Example Flow:**
```
Player chooses Easy:
  AI Skill: 0.25, HitsToIncrease: 5, MissesToDecrease: 2

Player struggles -> AI decreased:
  AI Skill: 0.15

Player keeps struggling -> AI decreased again:
  AI Skill: 0.15 (at min)
  Thresholds adapt: HitsToIncrease: 6, MissesToDecrease: 1
  
Now AI needs 6 consecutive player hits to get harder!
```

### Memory System

**Skill-Based Memory:**
- High skill (0.8+): Perfect recall of all guesses and hits
- Medium skill: May "forget" older information
- Low skill: Only remembers recent 3 guesses reliably

**Forget Chance Formula:**
```
forgetChance = (1.0 - skillLevel) * 0.3  // Max 30% at lowest skill
```

### Think Time

**Purpose:** Make AI feel human, not instant.

**Range:** 0.8 to 2.5 seconds (randomized per turn)

**Scaling:** Could be extended based on "difficulty" of decision (more candidates = longer think time)

---

## Audio Settings

### Default Volume

- **Sound Effects:** 50% (0.5f)
- **Music:** 50% (0.5f)

Players can adjust both via Settings Panel (accessible from Main Menu).

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
| Turn Management | COMPLETE |
| Grid Row Labels Resize | COMPLETE |
| Autocomplete Dropdowns | COMPLETE |
| Main Menu | COMPLETE |
| Settings Panel | COMPLETE |
| AI Opponent Scripts | COMPLETE |
| AI Integration | COMPLETE |
| AI Playtest | COMPLETE |
| UI System - Setup Mode | COMPLETE |
| UI System - Gameplay Mode | COMPLETE |

---

## Development Phases

### Phase 1: Core Mechanics - COMPLETE

### Phase 2: UI Implementation - COMPLETE

**Setup Mode - COMPLETE (Dec 4, 2025):**
- PlayerGridPanel, GridCellUI, LetterButton prefabs
- SetupSettingsPanel with all controls
- Color picker (8 presets with highlight)
- Word entry and validation
- Coordinate placement mode with visual feedback
- Pick Random Words / Place Random Positions buttons

**Gameplay Mode - COMPLETE (Dec 11, 2025):**
- [X] GameplayUIController with two-panel system
- [X] Owner panel shows player's words fully revealed
- [X] Opponent panel with hidden letter support
- [X] Data transfer from setup to gameplay
- [X] Grid initialization and word placement
- [X] Miss limit calculation using opponent's grid settings
- [X] Guillotine graphics and miss counter display
- [X] Letter guessing (click letter tracker)
- [X] Coordinate guessing (click grid cells)
- [X] Three-color grid cell system (green/red/yellow)
- [X] Yellow-to-green cell upgrade when letter discovered
- [X] Duplicate guess prevention
- [X] Word guessing via row buttons (Guess Word, Backspace, Accept, Cancel)
- [X] Letter tracker keyboard mode
- [X] Word validation before acceptance
- [X] Solved word row tracking (permanent button hiding)
- [X] Guessed word lists under guillotines

### Phase 2.5: Code Refactoring - COMPLETE (Dec 12, 2025)

Major refactoring effort completed to improve maintainability:

| Script | Original | Final | Reduction |
|--------|----------|-------|-----------|
| PlayerGridPanel.cs | 2,192 | 1,120 | 49% |
| GameplayUIController.cs | 2,112 | 1,179 | 44% |
| WordPatternRow.cs | 1,378 | 1,199 | 13% |

**Extracted Controllers/Services:**
- LetterTrackerController, GridColorManager, PlacementPreviewController
- WordPatternRowManager, CoordinatePlacementController, GridLayoutManager
- PlayerColorController, WordValidationService
- GuessProcessor, WordGuessModeController, WordGuessInputController
- RowDisplayBuilder (utility)

### Phase 2.6: Pre-AI Features - COMPLETE (Dec 13, 2025)

- [X] Autocomplete row dropdowns - COMPLETE
- [X] Main Menu - COMPLETE
- [X] Settings Panel - COMPLETE
- [X] Full game flow tested (Main Menu -> Setup -> Gameplay)

### Phase 2.7: Architecture Documentation - COMPLETE (Dec 13, 2025)

- [X] DLYH_Architecture_v3 document created
- [X] All 44 scripts cataloged and documented
- [X] IGridControllers.cs interfaces documented (5 interfaces, 2 enums)
- [X] WordPatternController documented
- [X] All 10 AI scripts reviewed and documented
- [X] Data flow diagrams created
- [X] Event architecture mapped

### Phase 3: AI Opponent - INTEGRATED & PLAYTESTED (Dec 13, 2025)

**Scripts Complete (11 scripts):**
- [X] ExecutionerConfigSO.cs (~412 lines) - All tunable parameters
- [X] ExecutionerAI.cs (~493 lines) - Main AI MonoBehaviour
- [X] DifficultyAdapter.cs (~268 lines) - Rubber-banding + adaptive thresholds
- [X] MemoryManager.cs (~442 lines) - Skill-based memory filtering
- [X] AISetupManager.cs (~468 lines) - Word selection and placement
- [X] IGuessStrategy.cs (~493 lines) - Interface + AIGameState + GuessRecommendation
- [X] LetterGuessStrategy.cs (~327 lines) - Frequency + pattern analysis
- [X] CoordinateGuessStrategy.cs (~262 lines) - Adjacency + density awareness
- [X] WordGuessStrategy.cs (~327 lines) - Confidence thresholds
- [X] LetterFrequency.cs (~442 lines) - Static English frequency data
- [X] GridAnalyzer.cs (~442 lines) - Fill ratio and coordinate scoring

**Integration Complete (Dec 13, 2025):**
- [X] ExecutionerAI wired to GameplayUIController
- [X] AI events (OnLetterGuess, OnCoordinateGuess, OnWordGuess) connected
- [X] BuildAIGameState() method implemented
- [X] TriggerAITurn() executes after player's turn ends
- [X] Rubber-banding connected via RecordPlayerGuess() calls
- [X] GenerateOpponentData() replaced with AISetupManager
- [X] 16+ compilation errors fixed
- [X] Win condition checking implemented

**First Playtest (Dec 13, 2025 - Stacey vs AI):**
Playtest revealed 4 bugs:
1. Letter tracker not updating green when words are guessed correctly
2. Player names showing "Player 1/Player 2" instead of actual names under guillotines
3. Player's guessed word list not populating (correct behavior - player used letter reveals)
4. AI missed word guesses counting 1 miss instead of 2

**Bug Fixes In Progress:**
- [ ] GuessProcessor callback changed from Action to Action<int> for proper miss counting
- [ ] Center panel name labels added to GameplayUIController
- [ ] Letter tracker update on correct word guess

### Phase 4: Polish and Features - TODO

- [ ] Complete playtest bug fixes
- [ ] Visual polish (DOTween animations, Feel effects)
- [ ] Audio implementation
- [ ] Invalid word feedback UI (toast/popup)
- [ ] Profanity filtering
- [ ] Medieval/carnival themed monospace font
- [ ] Turn indicator improvements

### Phase 5: Multiplayer and Mobile - TODO

- [ ] 2-player networking mode (human vs human online)
- [ ] Mobile implementation

---

## Known Issues / TODO List

### Active Bug Fixes (Dec 13, 2025)

| Bug | Description | Status |
|-----|-------------|--------|
| Letter tracker on word guess | Letters don't turn green when word is correctly guessed | IN PROGRESS |
| Center panel names | Shows "Player 1/Player 2" instead of actual player names | IN PROGRESS |
| Miss count for word guesses | Wrong word guess counts 1 miss instead of 2 | IN PROGRESS |
| Guess Word button auto-hide | Buttons now auto-hide when words fully revealed via letters | FIXED |

### Deferred to Polish Phase

| Issue | Notes |
|-------|-------|
| Invalid word feedback UI | Toast/popup for rejected words |
| Profanity filter | Some inappropriate words in word bank |
| Medieval monospace font | Replace Consolas with theme-appropriate font |

### Design Considerations

| Item | Notes |
|------|-------|
| Word count difficulty | 3 words HARDER than 4 - formula accounts for this with -2 modifier for 4 words |
| Miss limit source | Uses OPPONENT's grid settings + YOUR difficulty preference |
| Grid cells on word guess | Do NOT reveal - only coordinate guesses reveal grid cells |
| Default audio volume | 50% for both SFX and Music |
| AI word bank access | Full access (fair since players could memorize it) |
| AI skill persistence | Per session (per user later with login) |
| AI threshold adaptation | Protects struggling players, challenges dominant ones |

---

## Project Documents

| Document | Purpose | Version |
|----------|---------|---------|
| DontLoseYourHead_GDD | Game design, mechanics, phases | v3.1 |
| DontLoseYourHead_ProjectInstructions | Development protocols, MCP tools | v3.0 |
| DESIGN_DECISIONS | Technical decisions, lessons learned | v3.0 |
| DLYH_Architecture | Script catalog, data flow, patterns | v3.0 |

---

## Version History

- **v3.1** (Dec 13, 2025): Phase 3 AI INTEGRATED & PLAYTESTED, first playtest with Stacey, 4 bugs identified and fixes in progress, auto-hide Guess Word buttons on letter reveal
- **v3.0** (Dec 13, 2025): Phase 3 AI IMPLEMENTED (10 scripts), Phase 2.7 Architecture Documentation COMPLETE, integration TODO
- **v2.3** (Dec 13, 2025): Phase 3 AI Opponent design complete with adaptive thresholds, Phase 2.6 COMPLETE
- **v2.2** (Dec 13, 2025): Autocomplete COMPLETE, Main Menu/Settings Panel scripts created
- **v2.1** (Dec 12, 2025): Added Pre-AI features (Autocomplete, Main Menu, Settings), Phase 5 (Multiplayer/Mobile), audio defaults
- **v2.0** (Dec 12, 2025): Code refactoring COMPLETE - PlayerGridPanel 49%, GameplayUIController 44%, WordPatternRow 13%
- **v1.9** (Dec 11, 2025): Gameplay Mode COMPLETE - letter/coordinate/word guessing, three-color cells, solved row tracking, guessed word lists
- **v1.8** (Dec 9, 2025): Unity 6.3, Gameplay Mode UI functional, guillotines displayed
- **v1.7** (Dec 8, 2025): Opponent-based miss limits, horizontal layout, package cleanup
- **v1.6** (Dec 5, 2025): Code refactoring (Controllers/Services)
- **v1.5** (Dec 4, 2025): Setup Mode complete
- **v1.4** (Dec 2, 2025): Major bug fixes and UI completion
- **v1.3** (Nov 28, 2025): Major UI design update
- **v1.2** (Nov 25, 2025): Major implementation update
- **v1.1** (Nov 22, 2025): Playtesting updates
- **v1.0** (Nov 20, 2025): Initial GDD

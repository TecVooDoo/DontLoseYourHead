# Don't Lose Your Head - Game Design Document

**Version:** 8.0
**Date:** November 20, 2025
**Last Updated:** December 16, 2025
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
Players can start a new game, access settings, submit feedback, or exit.

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
|         [  FEEDBACK  ]           |
|         [    EXIT    ]           |
+----------------------------------+
```

### Buttons
- **New Game:** Transitions to Setup Phase for Player 1
- **Settings:** Opens Settings Panel overlay
- **Feedback:** Opens Feedback Panel for player comments
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

### Player Configuration Options

**Player Name:** Text input field

**Player Color:** 8 preset color buttons (Blue, Green, Orange, Purple, Cyan, Pink, Yellow, Brown)

**Grid Size (7 options):** 6x6, 7x7, 8x8, 9x9, 10x10, 11x11, 12x12

**Word Count:** 3 words (HARDER) or 4 words (EASIER)

**Difficulty Setting:** Easy, Normal, Hard

### Word Entry

- Type letters using letter tracker keyboard or physical keyboard
- Words auto-validate against 25,000+ word bank
- Autocomplete dropdown shows up to 5 matching suggestions
- Click suggestion or complete typing to enter word
- Word list filtered for profanity and inappropriate content

### Word Placement

- Click compass button to enter placement mode
- Click starting cell on grid
- Choose horizontal or vertical direction
- Word appears on grid with placed letters

### Quick Setup Options

- **Pick Random Words:** Auto-fills empty word rows with valid words
- **Place Random Positions:** Auto-places all entered words on grid

### Setup Completion

- All words must be entered and placed on grid
- Start button enables when setup is complete
- Player 2 (or AI) then completes their setup

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

**Important:** Grid cells are ONLY revealed through coordinate guesses. Correctly guessing a word does NOT reveal the grid cells - it only updates the word pattern and letter tracker.

### Guessed Word Lists

Each player has a scrollable list under their guillotine showing:
- All words guessed (both correct and incorrect)
- Green background = correct guess
- Red background = incorrect guess

### Auto-Hide Guess Word Buttons

When a word is fully revealed through letter guessing (all letters discovered), the "Guess Word" button automatically hides since there's nothing left to guess.

---

## Game Mechanics

### Miss Limit Formula (Uses Opponent's Grid)

**Important:** Your miss limit is calculated using your OPPONENT's grid settings, not your own.

```
MissLimit = Base + OpponentGridBonus + OpponentWordModifier + YourDifficultyModifier

Where:
  Base = 15
  OpponentGridBonus: 6x6=+3, 7x7=+4, 8x8=+6, 9x9=+8, 10x10=+10, 11x11=+12, 12x12=+13
  OpponentWordModifier: 3 words=+0, 4 words=-2
  YourDifficultyModifier: Easy=+4, Normal=+0, Hard=-4
```

### Win/Lose Conditions

- **Win:** Reveal all opponent's letters in all words AND guess all grid positions for those words, OR opponent reaches their miss limit
- **Lose:** Reach your miss limit, OR opponent reveals all your letters AND all grid positions for your words

---

## AI Opponent - "The Executioner"

### Overview

The Executioner is an adaptive AI opponent that:
1. Makes intelligent guesses based on probability analysis
2. Adjusts difficulty via rubber-banding based on player performance
3. Adapts its own adaptation rate based on player trends
4. Feels human with variable think times and imperfect memory

### AI Grid and Word Selection

The AI randomly selects grid size and word count based on player difficulty:

| Player Difficulty | AI Grid Size (Random) | AI Word Count |
|-------------------|----------------------|---------------|
| **Easy** | 6x6, 7x7, or 8x8 | 4 words |
| **Normal** | 8x8, 9x9, or 10x10 | 3 or 4 words (random) |
| **Hard** | 10x10, 11x11, or 12x12 | 3 words |

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
| 0.25 (Easy) | Top 10 or random |
| 0.5 (Normal) | Top 5 |
| 0.8 (Hard) | Top 2 |
| 0.95 (Expert) | Optimal choice |

### Strategy 2: Coordinate Guessing

**Goal:** Pick the cell most likely to contain a letter.

**Scoring Factors:**
- Adjacency bonus (next to known hits)
- Line extension bonus (extends a row/column of hits)
- Center bias (longer words pass through center)

**Strategy Preference by Grid Density:**
- High density (>35% fill): Favor coordinate guessing
- Medium density (12-35%): Balanced approach
- Low density (<12%): Strongly favor letter guessing

### Strategy 3: Word Guessing

**Goal:** Decide IF and WHAT word to guess.

**Risk:** Wrong word guess costs 2 misses.

**Confidence Thresholds by Skill:**
| Skill Level | Min Confidence to Attempt |
|-------------|---------------------------|
| 0.25 (Easy) | 90%+ (rarely attempts) |
| 0.5 (Normal) | 70%+ |
| 0.8 (Hard) | 50%+ |
| 0.95 (Expert) | 30%+ (risk-taker) |

### Rubber-Banding System

**Purpose:** Keep games competitive regardless of skill gap.

**Initial Settings by Player Difficulty:**

| Player's Difficulty | AI Starting Skill | Hits to Increase AI | Misses to Decrease AI |
|--------------------|-------------------|---------------------|----------------------|
| Easy | 0.25 | 5 | 4 |
| Normal | 0.50 | 3 | 3 |
| Hard | 0.75 | 2 | 5 |

**Skill Adjustment:**
- When player gets N consecutive hits, AI skill increases by 0.10
- When player gets N consecutive misses, AI skill decreases by 0.10

**Skill Bounds:**
- Minimum: 0.25 (ensures AI still makes smart moves)
- Maximum: 0.95 (never perfect)

### Adaptive Threshold System

**Purpose:** The rubber-banding system itself adapts if the player is consistently struggling or dominating.

**Adaptation Rules (after 2+ same-direction adjustments):**

| Player Pattern | Threshold Change | Effect |
|----------------|------------------|--------|
| Struggling (AI decreased 2+ times) | HitsToIncrease +1, MissesToDecrease -1 | Protects player |
| Dominating (AI increased 2+ times) | HitsToIncrease -1, MissesToDecrease +1 | Challenges player |

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

**Range:** 0.8 to 2.5 seconds (randomized per turn)

---

## Audio Settings

### Default Volume

- **Sound Effects:** 50% (0.5f)
- **Music:** 50% (0.5f)

Players can adjust both via Settings Panel (accessible from Main Menu).

---

## Feedback System

Players can submit feedback:
- After each game (win or lose) via Feedback Panel
- From Main Menu via Feedback button
- Optional text comments sent to telemetry system

---

## Design Considerations

| Item | Notes |
|------|-------|
| Word count difficulty | 3 words HARDER than 4 - formula accounts for this with -2 modifier for 4 words |
| Miss limit source | Uses OPPONENT's grid settings + YOUR difficulty preference |
| Grid cells on word guess | Do NOT reveal - only coordinate guesses reveal grid cells |
| Default audio volume | 50% for both SFX and Music |
| AI word bank access | Full access (fair since players could memorize it) |
| AI grid variety | Random selection within difficulty-appropriate ranges |
| Word list content | Filtered for profanity; report missed words via Feedback |

---

**End of Game Design Document**

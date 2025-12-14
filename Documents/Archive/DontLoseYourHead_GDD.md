# Don't Lose Your Head - Game Design Document

**Version:** 1.0  
**Date:** November 20, 2025  
**Developer:** TecVooDoo LLC  
**Designer:** Rune (Stephen Brandon)  
**Platform:** Unity 6.2 (2D)  
**Target:** PC/Mobile  

---

## High Concept

A competitive word game combining Hangman's letter-guessing with Battleship's grid-based hidden information. Players place three hidden words on grids and take turns guessing letters or coordinates to find their opponent's words before the guillotine blade falls.

---

## Core Gameplay Loop

1. **Setup Phase:** Each player selects and places 3 words on their hidden grid
2. **Guessing Phase:** Players alternate turns choosing to either:
   - Guess a letter (reveals if letter exists, but not location)
   - Guess a coordinate (reveals if that space contains a letter)
3. **Win Condition:** First player to reveal all opponent's words wins
4. **Lose Condition:** Accumulate too many misses and the guillotine falls

---

## Game Mechanics

### Word Selection & Placement

**Word Requirements:**
- Players must select exactly 3 words:
  - 1 word of 3 letters
  - 1 word of 4 letters  
  - 1 word of 5 letters
- Total of 12 letters per player (before overlapping)

**Placement Rules:**
- Words can be placed in any orientation:
  - Horizontal (left-to-right)
  - Vertical (top-to-bottom)
  - Diagonal (any direction)
  - Backwards (any of the above reversed)
- **Overlapping is allowed** when words share the same letter at the same grid position
  - Example: "CAT" (horizontal) can intersect "WALK" (vertical) sharing the "A"
  - Scrabble-style intersection
- Words must fit entirely within the grid boundaries
- No validation against dictionary - trust system for valid words

### Turn Actions

**Option 1: Guess a Letter**
- Player names any letter A-Z
- **If correct:**
  - Letter is added to "known letters" list (visible to guesser)
  - Does NOT reveal position on grid
  - Any previously revealed `*` on grid that matches this letter updates to show the letter
- **If incorrect:**
  - Miss counter increases by 1
  - Guillotine blade raises one notch

**Option 2: Guess a Coordinate**
- Player selects a specific grid cell (e.g., "B5")
- **If hit (cell contains a letter):**
  - Cell reveals as `*` (asterisk)
  - If that letter was previously guessed, shows actual letter instead of `*`
- **If miss (empty cell):**
  - Cell shows as empty/crossed out
  - Miss counter increases by 1
  - Guillotine blade raises one notch

**Option 3: Guess a Complete Word**
- Player can guess a complete word at any time
- **If correct:**
  - Word is added to "found words" list
  - Any `*` symbols on grid that belong to this word update to show actual letters
  - Must still find word's location through coordinate guesses
- **If incorrect:**
  - Counts as **2 misses** (double penalty for wrong word guess)
  - Does NOT reveal any information about letters

### Difficulty Levels

Grid size determines difficulty and affects game length:

| Difficulty | Grid Size | Miss Limit | Density |
|-----------|-----------|------------|---------|
| **Easy** | 6x6 (36 cells) | 8 misses | 33% |
| **Medium** | 8x8 (64 cells) | 10 misses | 19% |
| **Hard** | 10x10 (100 cells) | 12 misses | 12% |

**Miss Limit:** When a player reaches their miss limit, their guillotine falls and they lose.

### Win/Lose Conditions

**Win Conditions:**
1. **Complete Discovery:** Reveal all letters of all 3 opponent words on the grid
2. **Opponent Elimination:** Opponent reaches their miss limit first

**Lose Conditions:**
1. Reach the miss limit for your chosen difficulty
2. Opponent reveals all your words first

**Important:** Finding all words triggers guillotine drop regardless of opponent's current miss count.

---

## Game Flow

### Pre-Game
1. **Difficulty Selection:** Both players choose grid size (can be different)
2. **Word Selection:** Players input their 3 words (3, 4, 5 letters)
3. **Word Placement:** Players position words on their grid
   - Can be manual or auto-placed
4. **Confirm Ready:** Both players ready → game starts

### In-Game
1. **Turn Indicator:** Shows whose turn it is
2. **Action Selection:** Current player chooses:
   - Guess letter
   - Guess coordinate  
   - Guess word
3. **Result Display:** Show hit/miss, update grids, increment counters
4. **Turn Switch:** Next player's turn
5. **Repeat** until win/lose condition met

### Post-Game
1. **Winner Declared:** Show guillotine animation (loser's head falls)
2. **Score Summary:** Display final stats (hits, misses, words found)
3. **Rematch Option:** Play again or return to menu

---

## UI/UX Design

### Main Screen Elements

**Two-Grid Layout:**
- **Your Grid** (left/top): Shows your word placements and opponent's guesses
- **Opponent Grid** (right/bottom): Shows your guesses and discovered letters

**Information Display:**
- **Miss Counter:** "Misses: X / [limit]"
- **Guillotine Visual:** Blade height indicator showing progress toward death
- **Known Letters:** List of correctly guessed letters
- **Found Words:** List of complete words guessed correctly
- **Turn Indicator:** Whose turn it is

**Input Controls:**
- **Letter Buttons:** A-Z keyboard for letter guessing
- **Grid Cells:** Clickable cells for coordinate guessing
- **Word Input Field:** Text field + submit button for word guessing

### Visual Feedback

**Grid Cell States:**
- **Hidden:** Gray/neutral (opponent's grid)
- **Your Letters:** Visible on your grid
- **Empty Miss:** Red X or crossed out
- **Hit (Unknown Letter):** Yellow asterisk `*`
- **Hit (Known Letter):** Green letter display
- **Complete Word:** Highlighted/colored differently

**Guillotine States:**
- **Safe:** Blade at top, green zone
- **Warning:** Blade mid-height, yellow zone  
- **Danger:** Blade near bottom, red zone
- **Death:** Blade drops, dramatic animation

---

## Technical Specifications

### Core Systems Required

1. **Grid System**
   - Dynamic grid generation (6x6, 8x8, 10x10)
   - Cell coordinate system (A-J, 0-9)
   - Cell state management

2. **Word Validation System**
   - Word placement validator
   - Overlap detection and validation
   - Boundary checking

3. **Game State Manager**
   - Turn tracking
   - Miss counting
   - Win/lose condition checking
   - Game flow control

4. **AI Opponent** (for single-player)
   - Word selection algorithm
   - Word placement logic
   - Guessing strategy (letter frequency, grid patterns)
   - Difficulty scaling

5. **UI System**
   - Grid rendering and interaction
   - Input handling (letters, coordinates, words)
   - Visual feedback and animations
   - Score/stat display

### Data Structures

```csharp
// Core game data
public class GameState
{
    public Player player1;
    public Player player2;
    public Player currentPlayer;
    public DifficultyLevel difficulty;
    public int maxMisses;
    public GamePhase currentPhase;
}

public class Player
{
    public string playerName;
    public Grid grid;
    public List<Word> words;
    public HashSet<char> guessedLetters;
    public HashSet<string> guessedWords;
    public int missCount;
    public bool isAI;
}

public class Grid
{
    public int size; // 6, 8, or 10
    public GridCell[,] cells;
}

public class GridCell
{
    public Vector2Int coordinate;
    public char letter; // null if empty
    public Word belongsToWord; // reference to word if part of one
    public CellState state; // Hidden, Revealed, Miss, etc.
}

public class Word
{
    public string text;
    public Vector2Int startPosition;
    public WordDirection direction;
    public bool isFullyRevealed;
}

public enum WordDirection
{
    Horizontal,
    Vertical,
    DiagonalDownRight,
    DiagonalDownLeft,
    HorizontalReverse,
    VerticalReverse,
    DiagonalUpRight,
    DiagonalUpLeft
}

public enum CellState
{
    Hidden,
    Revealed,
    Miss,
    PartiallyKnown // Shows as *
}
```

---

## Development Phases

### Phase 1: Core Mechanics (MVP)
- [ ] Grid system with 3 sizes
- [ ] Word placement (manual)
- [ ] Letter guessing
- [ ] Coordinate guessing
- [ ] Miss counting
- [ ] Win/lose detection
- [ ] Turn management
- [ ] Basic UI (placeholder art)

### Phase 2: AI Opponent
- [ ] AI word selection
- [ ] AI word placement  
- [ ] AI guessing strategy (basic)
- [ ] Difficulty scaling

### Phase 3: Polish & Features
- [ ] Word guessing mechanic
- [ ] Auto-placement option
- [ ] Better visual feedback
- [ ] Animations (guillotine, reveals)
- [ ] Sound effects
- [ ] Tutorial/how-to-play

### Phase 4: Multiplayer (Optional)
- [ ] Local multiplayer (hot-seat)
- [ ] Online multiplayer (if successful)

### Phase 5: Art & Theme
- [ ] Replace placeholder art
- [ ] Medieval carnival theme (or pivot to different theme)
- [ ] Character avatars
- [ ] Guillotine animations
- [ ] Background art
- [ ] UI skinning

---

## Design Philosophy

### Development Priorities

1. **Ship Fast:** Get playable prototype done quickly
2. **Placeholder First:** Use simple shapes/colors before custom art
3. **Core Loop:** Nail the fun before adding features
4. **Iterative:** Build → Test → Refine
5. **Scope Control:** Resist feature creep

### Success Metrics

**Technical:**
- Game is bug-free and stable
- AI provides reasonable challenge
- Games complete in 5-15 minutes

**Design:**
- Core loop is engaging
- Strategic depth (letter vs coordinate decisions matter)
- Balanced difficulty progression

**Business:**
- Complete and publish within 2-3 months
- Build portfolio for TecVooDoo LLC
- Learn full dev-to-publish pipeline
- Foundation for next game (Shrunken Head Toss)

---

## Asset Requirements

### Essential (Phase 1)
- Odin Inspector (data management)
- DOTween Pro (animations)
- Unity MCP (development tool)
- TextMeshPro (text rendering)

### Optional (Later Phases)
- Feel (juice/feedback effects)
- UGUI Super ScrollView (word lists, leaderboards)
- Custom UI pack (if theme determined)

### Art Assets
- Placeholder: Colored squares, basic shapes
- Final: TBD - commission or asset store depending on budget

---

## Open Questions & Future Decisions

### To Be Decided:
- [ ] Exact theme/visual style (medieval carnival vs. other)
- [ ] Monetization strategy (free with ads? premium? IAP?)
- [ ] Platform priority (PC first? Mobile first? Both?)
- [ ] Word list source (built-in dictionary? curated list?)
- [ ] Dictionary validation (enforce valid words or trust players?)
- [ ] Profanity filtering needed?

### Potential Features (Post-Launch):
- Daily challenges
- Leaderboards
- Stats tracking (games played, win rate, etc.)
- Achievement system
- More word lengths (2-letter, 6-letter?)
- Custom word lists
- Themed word packs
- Power-ups or special abilities

---

## References & Inspiration

**Game Mechanics:**
- Hangman (letter guessing, progressive failure)
- Battleship (grid-based hidden information)
- Scrabble (word overlapping mechanics)
- Wordle (word guessing with feedback)

**Visual Style:**
- Original concept art: Medieval carnival with guillotines and spectators
- Potential pivot to different theme during art phase

---

## Version History

- **v1.0** (Nov 20, 2025): Initial GDD created from design discussions
  - Core mechanics defined
  - Difficulty system established
  - Win/lose conditions clarified
  - Development phases outlined

---

## Notes

- This is a living document and will be updated as development progresses
- Design decisions may change based on playtesting feedback
- Scope is intentionally kept small for fast completion
- This is game #1 in TecVooDoo's "small games first" strategy
- Success enables next project: Shrunken Head Toss, then A Quokka Story

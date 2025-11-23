# Don't Lose Your Head - Game Design Document

**Version:** 1.1  
**Date:** November 20, 2025  
**Last Updated:** November 22, 2025  
**Developer:** TecVooDoo LLC  
**Designer:** Rune (Stephen Brandon)  
**Platform:** Unity 6.2 (2D)  
**Target:** PC/Mobile  

---

## High Concept

A competitive word game combining Hangman's letter-guessing with Battleship's grid-based hidden information. Players place hidden words on grids and take turns guessing letters or coordinates to find their opponent's words before the guillotine blade falls.

**Key Innovation:** Asymmetric difficulty system allows mixed-skill players (parent/child, veteran/newbie) to compete fairly by choosing different grid sizes and word counts.

---

## Core Gameplay Loop

1. **Setup Phase:** Each player selects difficulty settings and places words on their hidden grid
2. **Guessing Phase:** Players alternate turns choosing to either:
   - Guess a letter (reveals if letter exists, but not location)
   - Guess a coordinate (reveals if that space contains a letter)
   - Guess a complete word (double penalty if wrong)
3. **Win Condition:** First player to reveal all opponent's words wins
4. **Lose Condition:** Accumulate too many misses and the guillotine falls

---

## Playtesting Insights (November 2025)

### Excel Prototype Testing

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

**See DESIGN_DECISIONS.md for complete playtesting analysis**

---

## Game Mechanics

### Word Selection & Placement

**Word Requirements:**
- Players must select exactly 3 words:
  - 1 word of 3 letters
  - 1 word of 4 letters  
  - 1 word of 5 letters
- Total of 12 letters per player (before overlapping)

**Future Expansion:** Hybrid difficulty system will allow 2-4 words per player

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
- No validation against dictionary - trust system for valid words (MVP)

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

### Difficulty System

#### Traditional Difficulty (Phase 1 - MVP)

Fixed difficulty tiers for initial implementation:

| Difficulty | Grid Size | Miss Limit | Word Density |
|-----------|-----------|------------|--------------|
| **Easy** | 6x6 (36 cells) | 20 misses | 33% |
| **Medium** | 8x8 (64 cells) | 23 misses | 19% |
| **Hard** | 10x10 (100 cells) | 27 misses | 12% |

**Note:** Miss limits adjusted based on playtesting (originally 8/10/12)

#### Hybrid Difficulty System (Phase 2 - Post-MVP)

**Revolutionary Feature:** Asymmetric difficulty for mixed-skill play

**Player-Specific Settings:**
- **Grid Size:** 6x6, 8x8, or 10x10 (independent per player)
- **Word Count:** 2, 3, or 4 words (independent per player)
- **Miss Limit:** Dynamically calculated based on choices

**Miss Limit Formula:**
```
BaseMisses = 15

GridSizeBonus:
- 6x6: +5 misses
- 8x8: +8 misses
- 10x10: +12 misses

WordCountModifier:
- 2 words: +5 misses (harder - more empty space)
- 3 words: +0 misses (baseline)
- 4 words: -3 misses (easier - higher density)

MissLimit = BaseMisses + GridSizeBonus + WordCountModifier
```

**Example Configurations:**
- Veteran Player: 10x10 grid, 2 words = 32 misses (challenging)
- New Player: 6x6 grid, 4 words = 17 misses (easier)
- Both players have appropriate challenge for their skill level

**Benefits:**
- Enables parent/child play
- Veteran vs newcomer matches
- Self-balancing as players improve
- Unique selling point

### Win/Lose Conditions

**Win Conditions:**
1. **Complete Discovery:** Reveal all letters of all opponent's words on the grid
2. **Opponent Elimination:** Opponent reaches their miss limit first

**Lose Conditions:**
1. Reach the miss limit for your chosen difficulty
2. Opponent reveals all your words first

**Important:** Finding all words triggers guillotine drop regardless of opponent's current miss count.

---

## Game Flow

### Pre-Game
1. **Difficulty Selection:** Players choose grid size (can be asymmetric in Phase 2)
2. **Word Count Selection:** Players choose word count (Phase 2)
3. **Word Selection:** Players input their words (3, 4, 5 letters for MVP)
4. **Word Placement:** Players position words on their grid
   - Can be manual or auto-placed
5. **Confirm Ready:** Both players ready -> game starts

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
- **Miss Counter:** "Misses: X / [limit]" format (improved clarity)
  - Color-coded: Green (safe), Yellow (warning), Red (danger)
  - Pulse animation on increment
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

**Color Coding:**
- Player 1 (Home): Blue theme
- Player 2 (Guest): Green theme
- Consistent across grids, counters, guillotines, character heads

---

## Technical Specifications

### Core Systems Required

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
   - Miss counting (IntVariableSO)
   - Win/lose condition checking
   - Game flow control (GameStateMachine)
   - Player state management (PlayerManager)

4. **AI Opponent** [TODO]
   - Word selection algorithm
   - Word placement logic
   - Guessing strategy (letter frequency, grid patterns)
   - Difficulty scaling

5. **UI System** [TODO]
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

public enum GamePhase
{
    MainMenu,
    DifficultySelection,
    WordSelection,
    WordPlacement,
    GameplayActive,
    GameOver
}
```

---

## Development Phases

### Phase 1: Core Mechanics (MVP) - IN PROGRESS

**Completed:**
- [X] Folder structure (Assets/DLYH/)
- [X] Grid system with 3 sizes
- [X] Word placement (manual) with all orientations
- [X] Letter guessing
- [X] Coordinate guessing
- [X] Miss counting
- [X] Win/lose detection
- [X] Turn management (TurnManager)
- [X] Player system (PlayerSO, PlayerManager)
- [X] Game flow state machine (6 phases)
- [X] ScriptableObject architecture
- [X] Difficulty system (3 ScriptableObject assets)

**In Progress:**
- [ ] Word guessing mechanic (2-miss penalty)
- [ ] Basic UI (grids, input, feedback)
- [ ] Word bank integration

**Next:**
- [ ] UI implementation
- [ ] Visual feedback polish

### Phase 2: AI Opponent
- [ ] AI word selection
- [ ] AI word placement  
- [ ] AI guessing strategy (basic)
- [ ] Difficulty scaling

### Phase 3: Hybrid Difficulty System
- [ ] Independent grid size selection per player
- [ ] Independent word count selection per player
- [ ] Dynamic miss limit calculation
- [ ] UI for asymmetric setup
- [ ] Playtesting and balance validation

### Phase 4: Polish & Features
- [ ] Word guessing mechanic refinement
- [ ] Auto-placement option
- [ ] Enhanced visual feedback
- [ ] Animations (guillotine, reveals)
- [ ] Sound effects (Feel asset integration)
- [ ] Tutorial/how-to-play

### Phase 5: Multiplayer (Optional)
- [ ] Local multiplayer (hot-seat)
- [ ] Online multiplayer (if successful)

### Phase 6: Art & Theme
- [ ] Replace placeholder art
- [ ] Medieval carnival theme (or pivot to different theme)
- [ ] Character avatars
- [ ] Guillotine animations
- [ ] Background art
- [ ] UI skinning

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
- Miss limits feel fair (based on formula validation)
- AI provides appropriate challenge

**Business:**
- Complete and publish within 2-3 months
- Build portfolio for TecVooDoo LLC
- Learn full dev-to-publish pipeline
- Foundation for next game (Shrunken Head Toss)

---

## Data Resources

### Word Bank

**Source:** dwyl/english-words GitHub repository
- **File:** words_alpha.txt
- **License:** MIT (free to use)
- **Content:** ~479,000 English words
- **URL:** https://github.com/dwyl/english-words

**Implementation Plan:**
1. Download words_alpha.txt from repository
2. Filter for 3-letter, 4-letter, and 5-letter words
3. Create three separate word lists
4. Import as ScriptableObject word list assets
5. Use for word selection during gameplay setup
6. (Future) Use for dictionary validation if toggle enabled

**Why This Choice:**
- Free and open source (MIT license)
- Comprehensive word list
- Perfect for indie development budget
- No licensing fees (vs Wordnik $1,250 license)
- Already available on GitHub

---

## Asset Requirements

### Core Toolkit (Always Used)

**Tier 1: Primary Tools**
- Odin Inspector & Validator (data management, validation)
- DOTween Pro (all animations and tweening)
- Feel (game polish and juice effects)
- SOAP (ScriptableObject architecture patterns)

**Tier 2: UI/UX**
- UI Assistant (UI components with animations)
- UGUI Super ScrollView (word lists, leaderboards)
- Classic RPG GUI (medieval theme UI sprites)
- Easy Popup System (dialogs, confirmations)
- UIColor System (centralized color management)
- Text Auto Size for UI Toolkit (responsive text)

**Tier 3: System Assets**
- Easy Save 3 (save/load functionality)
- Code Monkey Toolkit (utilities)
- Scriptable Sheets (Google Sheets integration - future)
- All In 1 Sprite Shader (advanced sprite effects)

**Development Tools:**
- Unity MCP (Claude integration during development)

**Unity Built-ins:**
- TextMeshPro (all text rendering)
- Unity UI (UGUI) (Canvas, Image, Button)
- New Input System (all input handling)

### Art Assets
- Placeholder: Colored squares, basic shapes (current)
- Final: TBD - commission or asset store depending on budget

---

## Open Questions & Future Decisions

### To Be Decided:
- [ ] Exact theme/visual style (medieval carnival vs. other)
- [ ] Monetization strategy (free with ads? premium? IAP?)
- [ ] Platform priority (PC first? Mobile first? Both?)
- [ ] Dictionary validation (enforce valid words or trust players?)
  - Current: Trust system for MVP
  - Future: Optional toggle in settings
- [ ] Autocomplete for word input?
  - Current: Skip for MVP
  - Future: Consider for polish phase
- [ ] Profanity filtering needed?

### Design Questions (See DESIGN_DECISIONS.md):
- Word validation rules (plurals, possessives, proper nouns)
- Autocomplete dropdown functionality
- Word display placement on UI
- Color coding consistency
- Miss counter format and placement

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

- **v1.1** (Nov 22, 2025): Major update based on playtesting
  - Added playtesting insights section
  - Introduced hybrid difficulty system concept
  - Updated miss limits based on real gameplay data
  - Added word bank information (dwyl/english-words)
  - Updated development phases completion status
  - Added new assets to requirements
  - Documented asymmetric difficulty innovation

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
- **Backend first, polish later** - focus on mechanics before visuals
- Real playtesting (Excel prototype) already validated core mechanics

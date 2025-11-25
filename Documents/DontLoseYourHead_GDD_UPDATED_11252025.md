# Don't Lose Your Head - Game Design Document

**Version:** 1.2  
**Date:** November 20, 2025  
**Last Updated:** November 25, 2025  
**Developer:** TecVooDoo LLC  
**Designer:** Rune (Stephen Brandon)  
**Platform:** Unity 6.2 (2D)  
**Target:** PC/Mobile  

---

## High Concept

A competitive word game combining Hangman's letter-guessing with Battleship's grid-based hidden information. Players place hidden words on grids and take turns guessing letters or coordinates to find their opponent's words before the guillotine blade falls.

**Key Innovation:** Asymmetric difficulty system allows mixed-skill players (parent/child, veteran/newbie) to compete fairly by choosing different grid sizes, word counts, and forgiveness settings.

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

### Word Selection & Placement

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

### Win/Lose Conditions

**Win Conditions:**
1. **Complete Discovery:** Reveal all letters of all opponent's words on the grid
2. **Opponent Elimination:** Opponent reaches their miss limit first

**Lose Conditions:**
1. Reach the miss limit for your chosen difficulty
2. Opponent reveals all your words first

---

## UI/UX Design

### Main Screen Elements

**Two-Grid Layout:**
- **Your Grid** (left/top): Shows your word placements and opponent's guesses
- **Opponent Grid** (right/bottom): Shows your guesses and discovered letters

**Information Display:**
- **Miss Counter:** "Misses: X / [limit]" format
  - Color-coded: Green (safe), Yellow (warning), Red (danger)
  - Pulse animation on increment
- **Guillotine Visual:** Blade height indicator showing progress toward death
- **Known Letters:** List of correctly guessed letters (tied to each grid)
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

**Color Coding:**
- Player 1 (Home): Blue theme
- Player 2 (Guest): Green theme
- Consistent across grids, counters, guillotines, character heads

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

8. **UI System** [TODO]
   - Grid rendering and interaction
   - Input handling
   - Visual feedback and animations

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

- [ ] Grid rendering and interaction
- [ ] Setup mode UI (autocomplete, placement validation)
- [ ] Gameplay UI (letter/coordinate/word input)
- [ ] Visual feedback (hit/miss indicators)
- [ ] Toast notifications (Easy Popup System)

### Phase 3: AI Opponent

- [ ] AI word selection
- [ ] AI word placement  
- [ ] AI guessing strategy (basic)
- [ ] Difficulty scaling

### Phase 4: Polish & Features

- [ ] Animations (DOTween)
- [ ] Enhanced visual feedback
- [ ] Sound effects
- [ ] Tutorial/how-to-play

### Phase 5: Art & Theme

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
- Odin Inspector & Validator (data management, validation)
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
- **Backend complete, UI implementation next**
- Real playtesting (Excel prototype + Claude playtest) validated core mechanics

# Don't Lose Your Head - Design Decisions & Insights

**Version:** 1.1  
**Date:** November 22, 2025  
**Last Updated:** November 25, 2025  
**Developer:** TecVooDoo LLC  
**Designer:** Rune (Stephen Brandon)  

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
- 8x8 grid
- 3 words each (3, 4, 5 letters)
- 23 miss limit
- Excel-based tracking with progressive improvements during play

**Game Statistics:**
```
Duration: 25 turns (50 total moves)
Ended: Early due to coordinate confusion

Move Breakdown:
- Letter Guesses: 36 (72%)
- Coordinate Guesses: 13 (26%)
- Word Guesses: 1 (2%)

Final Miss Counts:
- Rune: 16/23 (70% of limit)
- Claude: 16/23 (70% of limit)

Words Found:
- Rune's words: RAW, HOLD, BEAST (all identified by Claude)
- Claude's words: HOG, ROAD, SNORE (all identified by Rune)
```

**Critical UX Issues Discovered:**

1. **Letter Selector Confusion**
   - Quote: "Hard for me to remember which letter selector goes with which grid"
   - **Solution:** Integrated letter displays tied directly to each grid

2. **Color Coding Problems**
   - Quote: "Green on my words is confusing"
   - **Solution:** Distinct player colors (pink/cyan) applied consistently

3. **Tracking Complexity**
   - Quote: "Still feels awkward counting opponent grid near opponent but tracked guessed letters near my grid"
   - **Solution:** Each grid needs its own integrated display elements

4. **Coordinate Confusion**
   - Game ended early because coordinate tracking became overwhelming
   - **Solution:** Clear labels, visual feedback, computer handles tracking

**Iterative Improvements During Play:**
- Added labels to player grids
- Changed green colors to match player label colors
- Added "Move" column to track total moves
- Fixed filtering issues with miss count formula
- Renamed labels from "You/Opponent" to "ME/CLAUDE" for clarity

**Design Validations:**
- Core gameplay IS fun and engaging
- Strategic depth exists (letter vs coordinate decisions matter)
- 2-miss penalty works (Claude's wrong "RAT" guess was costly!)
- 23-miss limit for 8x8 feels appropriate (70% utilization)
- Progressive information reveal creates satisfying tension

**Full Data:** See DLYH_Claude_Playtest_Game_Data_Complete.pdf

---

## The Counterintuitive Word Density Discovery

**Original Assumption:**
- More words = harder game (more things to find)

**Reality:**
- **More words = EASIER game!**
- Higher letter density means more hits per guess
- Fewer empty spaces = fewer opportunities for incorrect coordinate guesses

**Example:**
- 3 words on 8x8: 12 letters / 64 cells = 19% density
- 4 words on 8x8: 18 letters / 64 cells = 28% density

**Implications:**
- Word count directly affects difficulty, but inversely to expectations
- Fewer words + larger grid = hardest configuration
- More words + smaller grid = easiest configuration

---

## Difficulty System (Implemented)

### Design Philosophy

Instead of exposing raw numbers to players, the system uses intuitive labels:
- **Grid Size:** Small (6x6), Medium (8x8), Large (10x10)
- **Word Count:** 3 words or 4 words
- **Forgiveness:** Strict, Normal, Forgiving

Players understand "Forgiving gives more room for mistakes" without doing math.

### Miss Limit Formula (Final Implementation)

```
MissLimit = Base + GridBonus + WordModifier + ForgivenessModifier

Constants:
  BASE_MISSES = 15

  GRID_BONUS_6X6 = 3
  GRID_BONUS_8X8 = 6
  GRID_BONUS_10X10 = 10

  WORD_MODIFIER_3_WORDS = 0
  WORD_MODIFIER_4_WORDS = -2

  FORGIVENESS_STRICT = -4
  FORGIVENESS_NORMAL = 0
  FORGIVENESS_FORGIVING = +4
```

**Example Calculations:**

| Configuration | Calculation | Result |
|--------------|-------------|--------|
| 6x6, 3 words, Normal | 15 + 3 + 0 + 0 | 18 misses |
| 8x8, 3 words, Normal | 15 + 6 + 0 + 0 | 21 misses |
| 8x8, 3 words, Strict | 15 + 6 + 0 - 4 | 17 misses |
| 8x8, 4 words, Forgiving | 15 + 6 - 2 + 4 | 23 misses |
| 10x10, 3 words, Normal | 15 + 10 + 0 + 0 | 25 misses |
| 10x10, 4 words, Strict | 15 + 10 - 2 - 4 | 19 misses |

### Asymmetric Difficulty

**Problem Solved:** Mixed-skill gameplay

**Example Scenarios:**

1. **Veteran vs Newcomer:**
   - Veteran: 10x10 grid, 3 words, Strict (21 misses)
   - Newcomer: 6x6 grid, 4 words, Forgiving (20 misses)
   - Both have appropriate challenge

2. **Parent vs Child:**
   - Parent handicaps self with harder settings
   - Child gets easier settings for confidence building
   - Game remains competitive for both

3. **Skill Development:**
   - Start on Forgiving, progress to Normal, then Strict
   - Self-balancing as players improve

---

## 6-Letter Word Support

**Addition:** November 25, 2025

**Implementation:**
- WordBankImporter now generates 4 word lists (3/4/5/6 letters)
- 4-word configurations use all four lengths: 3 + 4 + 5 + 6 = 18 letters
- Provides more variety and longer words for experienced players

**Word Distribution:**
| Length | Count | Notes |
|--------|-------|-------|
| 3-letter | 2,130 | Standard |
| 4-letter | 7,186 | Most common |
| 5-letter | 15,921 | Largest pool |
| 6-letter | TBD | For 4-word games |

---

## Critical UI Requirements (From Playtesting)

### 1. Integrated Letter Displays
- Each grid gets its own letter selector
- Visual connection between letters and their grid
- Prevents confusion about which letters apply where

### 2. Player Color Coding
- Distinct colors per player
- Apply to: grid borders, labels, found words, miss counters
- Must be consistent throughout UI
- Consider colorblind accessibility

### 3. Clear Coordinate Labels
- Large, readable labels (A-H columns, 1-8 rows)
- Always visible (not just on hover)
- Highlight on hover/selection

### 4. Automatic State Tracking
- Computer tracks all known letters
- Computer tracks all revealed cells
- Computer tracks miss counts
- Computer prevents invalid/repeated guesses

### 5. Word Pattern Display
- Show `_EA_T` style patterns above grids
- Lock in revealed letters
- Update automatically when letters learned

### 6. Miss Counter Clarity
- Format: "Misses: X / Y"
- Color states: Green (safe), Yellow (warning), Red (danger)
- Visual progress bar or guillotine animation
- Pulse/animation on increment

---

## Open Design Questions

### 1. Word Validation Rules

**Question:** Should we enforce strict word validation?

**Considerations:**
- **Plurals:** Allow? (CAT vs CATS)
- **Possessives:** Allow? (JOHN'S, DOG'S)
- **Proper nouns:** Allow? (PARIS, RUNE)
- **Abbreviations:** Allow? (USA, NASA)

**Current Decision:** Trust system (no validation) for MVP
**Future Consideration:** Optional dictionary validation toggle

### 2. Autocomplete Dropdown

**Question:** Should word input have autocomplete/suggestions?

**Pros:**
- Helps with spelling
- Reduces typos
- Better mobile experience

**Cons:**
- Could make guessing too easy
- Additional development time

**Current Decision:** Include in setup mode (word entry), skip for gameplay guessing

### 3. Dictionary Validation Toggle

**Question:** Should players be able to enable dictionary validation?

**Options:**
- Trust system only (current MVP)
- Optional toggle in settings
- Required validation for competitive play

**Current Decision:** Defer to post-MVP

---

## Implementation Status

### Completed Systems

| System | Status | Notes |
|--------|--------|-------|
| Grid System | COMPLETE | All sizes, orientations |
| Word Placement | COMPLETE | Validation, overlapping |
| Letter Guessing | COMPLETE | Known letters tracking |
| Coordinate Guessing | COMPLETE | Asterisk reveal |
| Word Guessing | COMPLETE | 2-miss penalty |
| Letter Reveal | COMPLETE | * -> letter upgrade |
| Turn Management | COMPLETE | Player switching |
| Player System | COMPLETE | Per-player state |
| Game State Machine | COMPLETE | 6 phases |
| Difficulty System | COMPLETE | Enums + Calculator |
| Word Bank | COMPLETE | 25,000+ words |
| Win/Lose Detection | COMPLETE | Automatic |

### Next Priority: UI Implementation

| Component | Priority | Notes |
|-----------|----------|-------|
| Grid Rendering | HIGH | Core gameplay |
| Setup Mode UI | HIGH | Word entry, placement |
| Input Controls | HIGH | Letter/coord/word |
| Visual Feedback | MEDIUM | Hit/miss indicators |
| Toast Notifications | MEDIUM | Easy Popup System |
| Animations | LOW | DOTween polish |

---

## Lessons Learned

### 1. Excel Prototyping Was Invaluable
- Caught balance issues before coding them
- Allowed real gameplay testing with minimal investment
- Multiple playtests showed skill progression curve
- Iterative improvements during play revealed UX issues

### 2. Assumptions Must Be Tested
- "More words = harder" was completely wrong
- Miss limits needed 2-3x increase from original design
- UI clarity issues only apparent in real play
- Coordinate tracking harder than expected

### 3. Asymmetric Difficulty Is A Strength
- Turns a problem (skill gap) into a feature
- Enables mixed-skill gameplay
- Self-balancing as players improve
- Unique selling point for the game

### 4. Backend First, Polish Later
- Focus on mechanics and balance first
- Use placeholders for art/UI
- Polish comes after core loop is proven fun
- Saves wasted effort on features that might change

### 5. Complete File Replacements Save Time
- Searching for specific lines is error-prone
- Full file replacement is faster for multi-line changes
- Single-line changes with line numbers are fine
- Prevents context confusion and mismatched edits

---

## Tracked Design Decisions

| Decision | Status | Priority | Notes |
|----------|--------|----------|-------|
| Difficulty system with forgiveness | IMPLEMENTED | HIGH | DifficultyEnums + Calculator |
| Miss limit formula | IMPLEMENTED | HIGH | Validated by playtesting |
| Word validation | DEFERRED | LOW | Trust system for MVP |
| Autocomplete | PLANNED | MEDIUM | For setup mode only |
| Color coding | IN DESIGN | MEDIUM | Follow playtest colors |
| Miss counter format | DECIDED | MEDIUM | "X / Max" with color states |
| Asymmetric gameplay | IMPLEMENTED | HIGH | Key differentiator |
| 6-letter words | IMPLEMENTED | MEDIUM | For 4-word configurations |
| ASCII encoding only | IMPLEMENTED | HIGH | Prevents Unicode issues |

---

**End of Design Decisions Document**

This is a living document updated as:
- New playtesting reveals insights
- Design questions are resolved
- Balance adjustments are made
- Implementation uncovers new considerations

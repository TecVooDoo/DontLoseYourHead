# Don't Lose Your Head - Design Decisions & Insights

**Version:** 1.0  
**Date:** November 22, 2025  
**Developer:** TecVooDoo LLC  
**Designer:** Rune (Stephen Brandon)  

---

## Playtesting Insights (Excel Prototype)

### Session 1: Initial Playtest with Spouse

**Setup:**
- 3 words (3, 4, 5 letters = 12 total letters)
- Grid size: TBD (Excel prototype)
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

### The Counterintuitive Word Density Discovery

**Original Assumption:**
- More words = harder game (more things to find)

**Reality:**
- **More words = EASIER game!**
- Higher letter density means more hits per guess
- Fewer empty spaces = fewer opportunities for incorrect coordinate guesses
- Example: Grid with 3 words vs grid with 4 words
  - 3 words: 12 letters spread across grid (assuming no overlaps)
  - 4 words: 16+ letters = significantly less empty space

**Implications:**
- Word count directly affects difficulty, but inversely to expectations
- Fewer words + larger grid = hardest configuration
- More words + smaller grid = easiest configuration

---

## Proposed Hybrid Difficulty System

### Concept: Decoupled Grid Size & Word Count

Instead of fixed difficulty tiers, allow players to choose:

**Grid Size Options:**
- 6x6 (36 cells)
- 8x8 (64 cells)
- 10x10 (100 cells)

**Word Count Options:**
- 2 words (fewer letters, harder)
- 3 words (original design)
- 4 words (more letters, easier)

**Miss Limit Scaling:**
- TBD based on further playtesting
- Likely needs to scale with grid size AND word count
- Formula approach: `BaseMisses + (GridBonus) - (WordCountPenalty)`

### Asymmetric Difficulty (Key Innovation!)

**Problem Solved:** Mixed-skill gameplay

**Example Scenarios:**

1. **Veteran vs Newcomer:**
   - Veteran: 10x10 grid, 2 words, 12 miss limit (HARD)
   - Newcomer: 6x6 grid, 4 words, 20 miss limit (EASY)
   - Both players have fair challenge at their skill level

2. **Parent vs Child:**
   - Parent: Handicaps self with harder settings
   - Child: Gets easier settings for confidence building
   - Game remains competitive and fun for both

3. **Skill Development:**
   - New players start easy, gradually increase difficulty
   - Self-balancing as players improve

**Implementation Requirements:**
- Each player chooses their own grid size
- Each player chooses their own word count
- Each player has independent miss limits
- UI must clearly show asymmetric settings

---

## Open Design Questions

### 1. Word Validation Rules

**Question:** Should we enforce strict word validation?

**Considerations:**
- **Plurals:** Are they allowed? (CAT vs CATS)
- **Possessives:** Are they allowed? (JOHN'S, DOG'S)
- **Proper nouns:** Are they allowed? (PARIS, RUNE)
- **Abbreviations:** Are they allowed? (USA, NASA)

**Current Stance:** Trust system (no validation) for MVP
**Future Consideration:** Optional dictionary validation toggle in settings

**Decision Needed Before:** Word selection UI implementation

---

### 2. Autocomplete Dropdown Functionality

**Question:** Should word input have autocomplete/suggestions?

**Pros:**
- Helps players who know the word but can't remember exact spelling
- Reduces typos
- Shows available words in dictionary (if validated)
- Better mobile experience

**Cons:**
- Could make guessing too easy
- Might break immersion
- Additional development time
- Requires word dictionary integration

**Current Stance:** Skip for MVP, consider for polish phase
**Alternative:** Simple text input with no suggestions

**Decision Needed Before:** Word input UI design

---

### 3. UI/UX Layout Questions

#### 3.a. Word Display Placement

**Question:** Where should the "words found" list appear?

**Options:**
1. **Below opponent's grid** (current concept art)
   - Pro: Keeps grid area clean
   - Con: Far from guessing action
   
2. **Sidebar next to grid**
   - Pro: Always visible during play
   - Con: Takes up screen space
   
3. **Collapsible panel**
   - Pro: Space-efficient
   - Con: Requires extra click to view

**Current Layout (from concept art):**
- Words listed below opponent grid
- Shows found words in red
- Shows known letters in green on right side

**Decision Needed Before:** Grid UI implementation

---

#### 3.b. Color Coding System

**Question:** How should we color-code player elements to match banners?

**Current Concept Art Colors:**
- **Player 1 (Home):** Blue banner -> Blue head
- **Player 2 (Guest):** Green banner -> Green head

**Elements to Color Code:**
- Player grids (border/background tint?)
- Miss counter displays
- Guillotine frames
- Character heads
- Found word lists
- Letter displays

**Consistency Requirements:**
- Each player's elements should use their color
- Need sufficient contrast for readability
- Colors must work for colorblind players

**Decision Needed Before:** UI sprite creation

---

#### 3.c. Miss Counter Placement

**Playtester Feedback:** Miss counter placement was confusing

**Current Design (from concept art):**
- Miss counters show below guillotines
- Format: Number display (2, 1, etc.)

**Questions:**
- Should miss counter show "X / MaxMisses" format? (e.g., "5 / 12")
- Should it be near guillotine or near grid?
- Should it pulse/flash when incremented?
- Should it color-code (green->yellow->red) as limit approaches?

**Proposed Improvements:**
- Format: "Misses: 5 / 12" for clarity
- Add color states: Green (safe), Yellow (warning), Red (danger)
- Add pulse animation on increment
- Keep near guillotine for visual association

**Decision Needed Before:** UI layout finalization

---

## Miss Limit Recalibration

### Original Design vs Playtest Reality

**Original Design:**
- Easy: 8 misses
- Medium: 10 misses
- Hard: 12 misses

**Playtest Reality:**
- First game: 25 misses needed
- Improvement over sessions, but still >15 misses

**Factors Affecting Miss Count:**
- Grid size (more cells = more chances to miss)
- Word count (fewer words = more empty spaces)
- Player skill (improves with practice)
- Letter frequency knowledge
- Strategic guess patterns

### Proposed New Formula (Needs Testing)

```
BaseMisses = 15  // Minimum safety buffer

GridSizeBonus:
- 6x6: +5 misses  (Total: 20)
- 8x8: +8 misses  (Total: 23)
- 10x10: +12 misses (Total: 27)

WordCountModifier:
- 2 words: +5 misses (harder, more empty space)
- 3 words: +0 misses (baseline)
- 4 words: -3 misses (easier, more letters)

Final Formula:
MissLimit = BaseMisses + GridSizeBonus + WordCountModifier
```

**Examples:**
- 6x6, 2 words: 15 + 5 + 5 = 25 misses
- 8x8, 3 words: 15 + 8 + 0 = 23 misses
- 10x10, 4 words: 15 + 12 - 3 = 24 misses

**Status:** Requires extensive playtesting to validate
**Priority:** HIGH - directly affects game balance

---

## Implementation Priority

### Phase 1: Core Systems (Completed)
- [X] Grid system
- [X] Word placement
- [X] Letter/coordinate guessing
- [X] ScriptableObject architecture
- [X] Turn management
- [X] Player system
- [X] Win/lose conditions

### Phase 2: Hybrid Difficulty System
- [ ] Independent grid size selection per player
- [ ] Independent word count selection per player
- [ ] Dynamic miss limit calculation
- [ ] UI for asymmetric setup

### Phase 3: Playtesting Refinement
- [ ] Test new miss limit formulas
- [ ] Gather data on actual games
- [ ] Adjust balance based on results
- [ ] Validate asymmetric difficulty fairness

### Phase 4: Polish (Post-Balance)
- [ ] Finalize color coding
- [ ] Improve miss counter clarity
- [ ] Add visual feedback improvements
- [ ] Consider autocomplete (if desired)

---

## Lessons Learned

### 1. Excel Prototyping Was Invaluable
- Caught balance issues before coding them
- Allowed real gameplay testing with minimal investment
- Spouse's feedback was honest and detailed
- Multiple playtests showed skill progression curve

### 2. Assumptions Must Be Tested
- "More words = harder" was completely wrong
- Miss limits needed 2-3x increase from original design
- UI clarity issues only apparent in real play

### 3. Asymmetric Difficulty Is A Strength
- Turns a problem (skill gap) into a feature
- Enables mixed-skill gameplay (parent/child, veteran/newbie)
- Self-balancing as players improve
- Unique selling point for the game

### 4. Backend First, Polish Later
- Focus on mechanics and balance first
- Use placeholders for art/UI
- Polish comes after core loop is proven fun

---

## Next Steps

### Immediate (This Week)
1. Implement word guessing mechanics (2-miss penalty)
2. Create word bank integration (filter dwyl/english-words)
3. Build basic UI for grids and input

### Short Term (Next 2 Weeks)
1. Implement hybrid difficulty selection UI
2. Code dynamic miss limit calculation
3. Create asymmetric setup screen
4. Build basic AI opponent

### Medium Term (Next Month)
1. Extensive playtesting with new balance
2. Gather quantitative data (games played, miss counts, win rates)
3. Refine miss limit formulas
4. Polish UI based on playtest feedback

### Long Term (Post-MVP)
1. Consider dictionary validation toggle
2. Evaluate autocomplete feature
3. Add tutorial system
4. Implement advanced AI strategies

---

## Tracked Design Decisions

| Decision | Status | Priority | Notes |
|----------|--------|----------|-------|
| Hybrid difficulty system | Approved | HIGH | Core innovation, implement in Phase 2 |
| Miss limit formula | Proposed | HIGH | Needs playtesting validation |
| Word validation | Deferred | LOW | Trust system for MVP |
| Autocomplete | Deferred | LOW | Not needed for MVP |
| Color coding | In Design | MEDIUM | Follow concept art scheme |
| Miss counter format | Proposed | MEDIUM | "X / Max" with color states |
| Asymmetric gameplay | Approved | HIGH | Key differentiator |

---

**End of Design Decisions Document**

This is a living document and will be updated as:
- New playtesting reveals insights
- Design questions are resolved
- Balance adjustments are made
- Implementation uncovers new considerations

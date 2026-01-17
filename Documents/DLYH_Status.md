# Don't Lose Your Head - Project Status

**Project:** Don't Lose Your Head (DLYH)
**Developer:** TecVooDoo LLC / Rune (Stephen Brandon)
**Platform:** Unity 6.3 (6000.0.38f1)
**Source:** `E:\Unity\DontLoseYourHead`
**Document Version:** 57
**Last Updated:** January 16, 2026

**Archive:** `DLYH_Status_Archive.md` - Historical designs, old version history, completed phase details

---

## Quick Context

**What is this game?** A competitive word game combining Hangman's letter-guessing with Battleship's grid-based hidden information. Players place hidden words on grids and take turns guessing letters or coordinates to find opponent's words before the guillotine blade falls.

**Key Innovation:** Asymmetric difficulty - mixed-skill players compete fairly with different grid sizes, word counts, and difficulty settings.

**Current Phase:** Phase E - Networking & Auth

**Last Session (Jan 16, 2026):** Forty-sixth session - **Refactoring Finalized & Folder Cleanup!**
- Reorganized folder structure: moved NewUI/Scripts/* to Scripts/UI/, NewUI assets to UI/
- Cleaned up scene: removed GameObjects with missing scripts (SetupModelController, GameplayUIController)
- Removed development comments (TODO, FIXME, Debug.Log) from UI scripts
- Phase 2 refactoring plan marked as FINAL

---

## Roadmap to Playtest Release

### Key Design Decisions

| Decision | Choice |
|----------|--------|
| Solo auth | Not required (play vs AI anonymously) |
| PVP auth | Required (Google/Facebook sign-in) |
| No PVP match found | Phantom AI after 6 seconds (fake name like "FluffyKitty") |
| Game duration | Async - can span days |
| Inactivity timeout | 5 days no move = abandonment loss |
| Post-game flow | Feedback modal -> Rematch option -> Main Menu |
| How to Play | Text instructions for playtest (tutorial later) |
| Playtest platform | WebGL on Cloudflare (dlyh.pages.dev), must work on mobile |
| Production platforms | Steam (PC), mobile apps (iOS/Android) - post-playtest |

### Phase D: Gameplay UI (COMPLETE - Jan 16, 2026)
- [x] Testing (grid colors, defense view, guillotine 5-stage)
- [x] Extra turn logic (word completed via GUESS button OR all letters found)
- [x] Win/lose detection (wire to existing WinConditionChecker)
- [x] End-game reveal (unfound words shown in grey)
- [x] Game end sequence (guillotine animation -> feedback modal with Continue)
- [x] Audio wiring - guillotine sounds (blade raise, execution sequence)
- [x] Audio sync - stage transition and game-over animations synced with audio
- [x] Audio wiring - gameplay sounds (hit/miss, button clicks, etc.)
- [x] New game confirmation popup (prevents accidental game loss)
- [x] How to Play screen (scrollable modal with help content)

### Phase E: Networking & Auth
**Auth (port from Dots and Boxes):**
- [ ] Sign in with Google/Facebook
- [ ] Persist name, color, preferences when signed in
- [ ] Solo vs AI = no auth required
- [ ] PVP = auth required

**Matchmaking:**
- [ ] Wire Join Code to Supabase
- [ ] 6-second matchmaking attempt
- [ ] No match found = spawn phantom AI with fake name
- [ ] Player never knows it's phantom AI

**Async Gameplay:**
- [ ] Games can span days (state persisted in Supabase)
- [ ] 5-day inactivity = auto-win for last player who moved
- [ ] Push notifications? (or just check on return)

**Multiplayer Flow:**
- [ ] Setup data exchange between players
- [ ] Turn synchronization
- [ ] State sync during gameplay
- [ ] Opponent disconnect/abandonment handling
- [ ] Rematch option after game end

### Phase F: Cleanup & Polish
- [ ] Remove legacy uGUI components
- [ ] Delete abandoned prefabs/scripts
- [ ] Namespace standardization (decide: TecVooDoo.DontLoseYourHead.* or DLYH.*)
- [ ] Essential animations (guillotine blade drop, head fall)
- [ ] Screen transitions

**Deferred to post-playtest:**
- Cell animations
- Screen shake
- Victory/defeat celebrations beyond guillotine

### Phase G: Deploy to Playtest
- [ ] WebGL build pipeline
- [ ] Deploy to Cloudflare (replace dlyh.pages.dev)
- [ ] Mobile browser testing & fixes
- [ ] Verify telemetry working
- [ ] Playtest feedback collection

---

## Active TODO

**Phase E:** Networking & Auth - Not started
- [ ] Port auth from Dots and Boxes (Google/Facebook sign-in)
- [ ] Wire Join Code to Supabase
- [ ] Implement 6-second matchmaking with phantom AI fallback
- [ ] Setup data exchange between players
- [ ] Turn synchronization
- [ ] State sync during gameplay

**Deferred to Phase F:**
- [ ] Document new interfaces/controllers in Architecture section
- [ ] Standardize namespace convention (choose TecVooDoo.DontLoseYourHead.* OR DLYH.*)
- [ ] Delete NetworkingTest.unity scene after UI Toolkit complete

---

## Development Priorities (Ordered)

1. **SOLID principles first** - single responsibility, open/closed, Liskov substitution, interface segregation, dependency inversion
2. **Memory efficiency second** - no per-frame allocations, no per-frame LINQ, object pooling where appropriate
3. **Clean code third** - readable, maintainable, consistent formatting
4. **Self-documenting code fourth** - clear naming over comments; if code needs a comment, consider refactoring first
5. **Platform best practices fifth** - Unity > C#, Cloudflare/Supabase > HTML/JS (platform-specific wins over language-generic)

---

## What Works (Completed Features)

**Core Mechanics:**
- Grid placement and word entry
- Letter, coordinate, and word guessing
- Miss limit calculation formula
- Win and loss condition detection
- Extra turn on word completion (queued if multiple)

**AI Opponent ("The Executioner"):**
- Adaptive difficulty with rubber-banding
- Strategy switching (letter, coordinate, word guesses)
- Memory-based decision making
- Variable think times (0.8-2.5s)
- Grid density analysis for strategy selection

**Audio:**
- Music playback with shuffle (Fisher-Yates) and crossfade (1.5s)
- SFX system with mute controls
- Dynamic tempo changes under danger (1.08x at 80%, 1.12x at 95%)
- Guillotine execution sound sequence (3-part)

**Telemetry:**
- Cloudflare workers endpoint for event capture
- Editor-accessible analytics dashboard
- Session, game, guess, feedback, and error tracking

**Networking (Phase 0.5 Complete):**
- IOpponent interface for opponent abstraction
- LocalAIOpponent wrapping ExecutionerAI
- RemotePlayerOpponent for network play
- OpponentFactory for creating opponents
- Supabase services (Auth, GameSession, Realtime, StateSynchronizer)
- PlayerService for creating player records without auth
- Database operations verified: player creation, game creation, game joining

**UI Toolkit (Phases A-C Complete):**
- TableModel data layer (no UI dependencies)
- TableView renderer (non-virtualized, state-driven)
- Setup wizard with progressive card disclosure
- Word rows with variable lengths (3,4,5,6)
- Autocomplete dropdown for word entry
- 8-direction grid placement with preview
- Main menu with inline settings
- Hamburger menu navigation
- Feedback modal

---

## Known Issues

**Architecture:**
- UIFlowController at ~4400 lines (could extract Turn Management and AI Opponent coordination if needed)
- Inconsistent namespace convention (TecVooDoo.DontLoseYourHead.* vs DLYH.*)

**Networking:**
- NetworkGameManager still uses AuthService (needs update for Phase 1)
- Full multiplayer gameplay not yet tested (setup exchange, turns, state sync)

**Audio:**
- Music crossfading/switching too frequently (should only switch at end of track)

---

## Architecture

### Namespaces

| Namespace | Scripts | Purpose |
|-----------|---------|---------|
| `TecVooDoo.DontLoseYourHead.UI` | 28 | Main UI scripts |
| `TecVooDoo.DontLoseYourHead.Core` | 4 | Game state, difficulty |
| `DLYH.AI.Core` | 4 | AI controllers |
| `DLYH.AI.Strategies` | 4 | AI guess strategies |
| `DLYH.Audio` | 5 | UI, guillotine, music audio |
| `DLYH.UI` | 1 | Audio settings helper (SettingsPanel) |
| `DLYH.TableUI` | 15+ | UI Toolkit implementation |
| `DLYH.Networking` | 4 | Opponent abstraction, factory |
| `DLYH.Networking.Services` | 8 | Supabase, auth, player, realtime |

### Key Types by Namespace

**TecVooDoo.DontLoseYourHead.UI:**
- WordPlacementData, GameplayStateTracker (Core/GameState/)
- Services/: WordValidationService

**DLYH.TableUI:**
- TableCellKind, TableCellState, CellOwner (enums)
- TableCell (struct), TableRegion (struct)
- TableLayout, TableModel, ColorRules, TableView
- WordRowView, WordRowsContainer
- UIFlowController, GameplayScreenManager, GuillotineOverlayManager

**DLYH.AI.Core:**
- ExecutionerAI, AISetupManager, DifficultyAdapter

**DLYH.Networking:**
- IOpponent, LocalAIOpponent, RemotePlayerOpponent, OpponentFactory

### Opponent Abstraction (v46 Refactor)

The game logic is now completely opponent-agnostic. Whether the opponent is:
- The Executioner AI (solo mode)
- A Phantom AI (fake player when no PVP match found)
- A Remote Human Player (PVP mode)

...the game controller doesn't know or care. It just:
1. Fires `OnOpponentTurnStarted` event
2. Waits for a guess via `IOpponent.OnLetterGuess` / `OnCoordinateGuess` / `OnWordGuess`
3. Processes the guess using the SAME code path as player guesses
4. Updates UI using opponent's color from `IOpponent.OpponentColor`
5. Fires `OnOpponentTurnEnded` event

**Key Changes (v46):**
- `CellOwner` enum simplified: `Player`, `Opponent` (removed `ExecutionerAI`, `PhantomAI`)
- `_aiOpponent` renamed to `_opponent` throughout
- `HandleAI*` methods renamed to `HandleOpponent*`
- Removed `GameMode.Solo` checks from turn switching
- Single color source: `IOpponent.OpponentColor`

### Key Folders

```
Assets/DLYH/
  Scripts/
    AI/           - ExecutionerAI, strategies, config
    Audio/        - UIAudioManager, MusicManager, GuillotineAudioManager
    Core/         - WordListSO, DifficultyCalculator, GameState
    Editor/       - Editor scripts
    Networking/   - IOpponent, services
    Telemetry/    - Analytics
    UI/           - All UI scripts (TableModel, TableView, UIFlowController, etc.)
      Services/   - WordValidationService
  UI/             - UI Toolkit assets (not scripts)
    USS/          - Stylesheets
    UXML/         - Layout files
    Prefabs/      - UI Toolkit prefabs
  Scenes/
    NewUIScene.unity     - Primary active scene
    NetworkingTest.unity - Multiplayer testing
```

### Key Scripts

| Script | Lines | Purpose |
|--------|-------|---------|
| UIFlowController | ~4400 | Screen flow + gameplay + modals |
| SetupWizardUIManager | ~817 | Setup wizard flow (extracted from UIFlowController) |
| GameplayScreenManager | ~650 | Gameplay UI state, tab switching, keyboard |
| GuillotineOverlayManager | ~450 | Guillotine overlay modal controller |
| ExecutionerAI | ~493 | AI opponent coordination |

### Packages

- Odin Inspector 4.0.1.2
- DOTween Pro 1.0.386
- UniTask 2.5.10
- New Input System 1.16.0
- Classic_RPG_GUI (UI theme assets)
- MCP for Unity 9.0.3 (Local)

---

## Game Rules (Complete Reference)

### Core Concept
DLYH is Battleship with words. Both players (P/O = Player/Opponent) see the same board structure with Attack and Defend sides. The only difference is whose words are shown where.

### Board Views

**Attack Board (viewing opponent's hidden words):**
- Grid: Starts empty/hidden - shows results of YOUR guesses against opponent
- Word Rows: Start as underscores (e.g., `_ _ _`, `_ _ _ _`)
- Keyboard Tracker: Tracks YOUR letter guesses

**Defend Board (viewing your own words):**
- Grid: Shows YOUR words and positions (what you placed during setup)
- Word Rows: Shows YOUR words fully visible
- Keyboard Tracker: Tracks OPPONENT's letter guesses against you

### Turn Flow
1. First turn is random between P/O
2. P/O makes ONE action: pick letter, pick coordinate, or guess word
3. Turn switches to other P/O
4. **Exception:** Extra turns earned for completing words (see below)

### Action: Pick a Letter (Keyboard)

**If letter IS in opponent's words:**
- Word Rows: Replace underscore with that letter in ALL words containing it
- Keyboard Tracker: Highlight based on coordinate knowledge (see Highlight Rules)
- Grid: NO CHANGE (letter picking doesn't reveal coordinates)

**If letter is NOT in opponent's words:**
- Keyboard Tracker: Red (miss)
- Miss count: +1

### Action: Pick a Coordinate (Grid Cell)

**If there IS a letter at that coordinate:**
- Grid Cell: Highlight based on letter knowledge (see Highlight Rules)
- Word Rows: NO CHANGE (coordinate picking doesn't reveal letters)
- Keyboard Tracker: NO CHANGE

**If there is NO letter at that coordinate:**
- Grid Cell: Red (miss)
- Miss count: +1

### Action: Guess Word (GUESS Button)

**If CORRECT:**
- That word's letters fill in (as if each letter was guessed individually)
- Other words in word rows also get those letters filled in
- Keyboard Tracker and Grid update accordingly
- Extra turn awarded

**If INCORRECT:**
- No change to board
- Miss count: +2 (double penalty)

### Highlight Hit Rules (Critical)

**Keyboard Tracker & Word Rows:**
| Condition | Color |
|-----------|-------|
| Letter is in words, but NOT all coordinates for that letter known on grid | Yellow |
| Letter is in words AND all coordinates for that letter known on grid | Player/Opponent Color |

**Grid Cells:**
| Condition | Color | Letter Visibility |
|-----------|-------|-------------------|
| Valid coordinate, but letter NOT known | Yellow | Hidden (Attack) / Shown (Defend) |
| Valid coordinate AND letter known | Player/Opponent Color | Shown (both boards) |

### How Letters Become "Fully Known" (Player Color)

A letter transitions from Yellow to Player Color when BOTH are true:
1. The letter itself is known (via letter guess or word guess)
2. ALL coordinates containing that letter are known (via coordinate guesses)

**Grid vs Keyboard difference:**
- **Grid cells:** Transition to Player Color individually when BOTH letter AND that specific coordinate are known
- **Keyboard/Word Rows:** Transition to Player Color only when ALL coordinates containing that letter are known

### Extra Turn Logic

Extra turn awarded when ALL letters in a word row are revealed (underscores replaced).

**Can chain:** If completing one word causes another word to complete (shared letters), each completion awards an extra turn.

**Example:**
- `_ _ T` and `_ _ L F`
- Guess "C" -> `C _ T` and `C _ L F` (no word complete, turn ends)
- Guess Word "CAT" -> correct -> `C A T` complete -> +1 extra turn
- "A" fills into second word -> `C A L F` complete -> +1 extra turn
- Total: 2 extra turns earned

### Miss Count
- Letter miss: +1
- Coordinate miss: +1
- Incorrect word guess: +2

### Miss Limit Formula

```
MissLimit = 15 + OpponentGridBonus + OpponentWordModifier + YourDifficultyModifier

OpponentGridBonus: 6x6=+3, 7x7=+4, 8x8=+6, 9x9=+8, 10x10=+10, 11x11=+12, 12x12=+13
OpponentWordModifier: 3 words=+0, 4 words=-2
YourDifficultyModifier: Easy=+4, Normal=+0, Hard=-4
```

### Win Conditions
1. P/O reveals ALL opponent's words AND all their grid coordinates
2. P/O wins by default if opponent reaches their miss limit
3. (Online PVP only) P/O wins if opponent abandons after 5 days

### Key Mechanics Summary

- 3 words HARDER than 4 (fewer letters to find)
- Wrong word guess = 2 misses (double penalty)
- Complete a word = extra turn (queued if multiple)
- Letter guesses reveal letters in word rows, NOT grid positions
- Coordinate guesses reveal grid positions, NOT letters (unless letter already known)
- Grid cells only show letter when BOTH letter AND coordinate are known

---

## AI System ("The Executioner")

### AI Grid Selection by Player Difficulty

| Player Difficulty | AI Grid Size | AI Word Count |
|-------------------|--------------|---------------|
| Easy | 6x6, 7x7, or 8x8 | 4 words |
| Normal | 8x8, 9x9, or 10x10 | 3 or 4 words |
| Hard | 10x10, 11x11, or 12x12 | 3 words |

### Rubber-Banding

| Player Difficulty | AI Start Skill | Hits to Increase | Misses to Decrease |
|-------------------|----------------|------------------|-------------------|
| Easy | 0.25 | 5 | 4 |
| Normal | 0.50 | 3 | 3 |
| Hard | 0.75 | 2 | 5 |

**Bounds:** Min 0.25, Max 0.95, Step 0.10

---

## Color Rules (Hard Requirements)

| Color | Usage | Selectable by Player? |
|-------|-------|----------------------|
| Red | System warnings, errors, PlacementInvalid, Miss | NO |
| Yellow | Revealed (hit but letter unknown) | NO |
| Green | Setup placement feedback (PlacementValid) | YES (but won't show green feedback) |
| Other colors | Player colors, reveal/hit feedback | YES |

**Gameplay Color Rules:**
- Red = Miss (coordinate guessed, no letter there)
- Yellow = Revealed (hit, letter unknown - attack grid hides letter, defense shows it)
- Player Color = Hit (letter AND coordinate known, letter shown on both grids)

---

## Key Patterns (with examples)

### 1. Event Timing Pattern

```csharp
// CORRECT: State before event
_isActive = false;
OnSomeEvent?.Invoke();

// WRONG: Handlers see stale state
OnSomeEvent?.Invoke();
_isActive = false;
```

### 2. Kill DOTween Before Reset

```csharp
private void ResetHeadPosition() {
    if (_headTransform != null) {
        DOTween.Kill(_headTransform);  // Kill first!
        if (_headPositionStored)
            _headTransform.anchoredPosition = _originalHeadPosition;
    }
}
```

### 3. Extra Turn Queue Pattern

```csharp
Queue<string> _extraTurnQueue = new Queue<string>();

// On word completion:
_extraTurnQueue.Enqueue(completedWord);

// After guess result:
if (_extraTurnQueue.Count > 0) {
    string word = _extraTurnQueue.Dequeue();
    // Show extra turn message, don't end turn
}
```

---

## Lessons Learned (Don't Repeat)

### Unity/C# Patterns
1. **Set state BEFORE firing events** - handlers may check state immediately
2. **Initialize UI to known states** - don't rely on defaults
3. **Kill DOTween before reset** - prevents animation conflicts
4. **Store original positions** - for proper reset after animations
5. **Use New Input System** - `Keyboard.current`, not `Input.GetKeyDown`
6. **No `var` keyword** - explicit types always
7. **800 lines max** - extract controllers when approaching limit
8. **Prefer UniTask over coroutines** - `await UniTask.Delay(1000)` not `yield return`
9. **No allocations in Update** - cache references, use object pooling
10. **Validate after MCP edits** - run validate_script to catch syntax errors

### Project-Specific
11. **Unity 6 UIDocument bug (IN-127759)** - UIDocument inspector destroys runtime UI; assign Source Asset (even empty placeholder) to prevent blue screen
12. **Use E: drive path** - never worktree paths like `C:\Users\steph\.claude-worktrees\...`
13. **Check scene file diffs** - layout can be accidentally modified
14. **Reuse existing systems** - Don't rebuild WordListSO, WordValidationService, CoordinatePlacementController - create thin adapters
15. **Prevent duplicate event handlers** - use flags like `_keyboardWiredUp` when handlers persist across screen rebuilds
16. **Reset validity on clear** - SetWordValid(false) must be called when clearing words to remove green styling

---

## Bug Patterns to Avoid

| Bug Pattern | Cause | Prevention |
|-------------|-------|------------|
| Guess Word buttons disappearing wrong rows | State set AFTER events | Set state BEFORE firing events |
| Autocomplete floating at top | Not hidden at init | Call Hide() in Initialize() |
| Board not resetting | No reset logic | Call ResetGameplayState() on new game |
| Guillotine head stuck | No stored position | Store original position on Initialize |
| Green cells after clear | Validity not reset | Call SetWordValid(false) in HandleWordCleared |

---

## Coding Standards (Enforced)

- Prefer async/await (UniTask) over coroutines unless trivial
- Avoid allocations in Update
- No per-frame LINQ
- Clear separation between logic and UI
- ASCII-only documentation and identifiers
- No `var` keyword - explicit types always

### Refactoring Guidelines

**Goal Range:** Files should be 800-1200 lines for optimal Claude compatibility. Files up to 1300 lines are acceptable if that's the minimum achievable without adding unnecessary complexity.

**When to Refactor:**
- Extract when a file exceeds 1200 lines AND has clear, separable responsibilities
- Extract when duplicate code appears across multiple locations

**When NOT to Refactor:**
- Don't refactor to hit an arbitrary line count
- Don't extract if it adds indirection without reducing complexity

---

## AI Rules (Embedded)

### Critical Protocols
1. **Verify names exist** - search before referencing files/methods/classes
2. **Step-by-step verification** - one step at a time, wait for confirmation
3. **Read before editing** - always read files before modifying
4. **ASCII only** - no smart quotes, em-dashes, or special characters
5. **Prefer structured edits** - use script_apply_edits for method changes
6. **Be direct** - give honest assessments, don't sugar-coat

---

## Session Close Checklist

After each work session, update this document:

- [ ] Move completed TODOs to "What Works" section
- [ ] Add any new issues to "Known Issues"
- [ ] Update "Last Session" with date and summary
- [ ] Add new lessons to "Lessons Learned" if applicable
- [ ] Update Architecture section if files were added/extracted
- [ ] Increment version number in header
- [ ] **Archive old content** to DLYH_Status_Archive.md:
  - Version history entries older than 5 sessions
  - Completed phase design details
  - Implemented UX designs (keep only active/in-progress)
  - Resolved bug details (keep pattern, archive fix history)

---

## Version History (Recent)

| Version | Date | Summary |
|---------|------|---------|
| 57 | Jan 16, 2026 | Forty-sixth session - **Refactoring Finalized!** Reorganized folders (NewUI -> Scripts/UI, UI/). Scene cleanup. Removed dev comments from UI scripts. Phase 2 refactoring plan marked FINAL. |
| 56 | Jan 16, 2026 | Forty-fifth session - **Phase 2 Refactoring Complete!** Extracted SetupWizardUIManager. Deleted legacy UI controllers, scripts, and prefabs. Created AudioSettings.cs. Fixed compile errors. Full game tested successfully. |
| 55 | Jan 16, 2026 | Forty-fourth session - **Phase D Complete!** Implemented How to Play modal with scrollable help content. Moved DefenseViewPlan.md and UI_Toolkit_Integration_Plan.md to Archive. |
| 54 | Jan 16, 2026 | Forty-third session - **Gameplay Audio & New Game Confirmation!** Wired UIAudioManager (keyboard, grid, hit/miss, buttons, popups). Added confirmation popup when starting new game during active game. Added ResetGameState() for proper cleanup. |
| 53 | Jan 16, 2026 | Forty-second session - **Guillotine Polish & Audio Sync!** Fixed lever positioning (inner posts). Fixed executioner z-order and vertical position. Removed invalid USS properties. Synced stage transition audio (1.5s delay). Synced game-over animations with audio (blade drop, head fall). |

**Full version history:** See `DLYH_Status_Archive.md`

---

## Known Issues (v57)

**Architecture:**
- UIFlowController at ~4400 lines (could extract Turn Management and AI Opponent coordination if needed)
- Inconsistent namespace convention (TecVooDoo.DontLoseYourHead.* vs DLYH.*)

**Networking:**
- NetworkGameManager still uses AuthService (needs update for Phase 1)
- Full multiplayer gameplay not yet tested (setup exchange, turns, state sync)

**Audio:**
- Music crossfading/switching too frequently (should only switch at end of track)

---

## Next Session Instructions

**Starting Point:** This document (DLYH_Status.md v55)

**Scene to Use:** NetworkingTest.unity (for Phase E networking work)

**Current State:**
- Phase A, B, C, D COMPLETE - Full single-player gameplay working!
- Phase E next - Networking & Auth

**Important:** Game logic is now opponent-agnostic! Use `_opponent` (not `_aiOpponent`), handlers are `HandleOpponent*` (not `HandleAI*`), and `CellOwner` only has `Player` and `Opponent` values.

**Phase E Starting Point:**
1. Review existing networking code in `DLYH.Networking` and `DLYH.Networking.Services`
2. Port auth from Dots and Boxes project
3. Wire Join Code to Supabase
4. Implement matchmaking with phantom AI fallback

**Existing Networking Foundation (Phase 0.5):**
- `IOpponent` interface for opponent abstraction
- `LocalAIOpponent` wrapping ExecutionerAI
- `RemotePlayerOpponent` for network play (stub)
- `OpponentFactory` for creating opponents
- Supabase services (Auth, GameSession, Realtime, StateSynchronizer)
- `PlayerService` for creating player records without auth

**Tab Behavior Summary:**
| Tab | Grid Shows | Words Show | Keyboard Shows | Whose Guesses |
|-----|------------|------------|----------------|---------------|
| Attack | Opponent's (fog + your guesses) | Opponent's (hidden) | YOUR guesses | Yours |
| Defend | YOUR (visible + AI guesses) | YOUR (visible) | AI's guesses | AI's |

**Do NOT:**
- Delete NetworkingTest.unity scene (needed for Phase E)
- Over-polish visuals yet (functional first, polish in Phase F)

---

**End of Project Status**

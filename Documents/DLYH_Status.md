# Don't Lose Your Head - Project Status

**Project:** Don't Lose Your Head (DLYH)
**Developer:** TecVooDoo LLC / Rune (Stephen Brandon)
**Platform:** Unity 6.3 (6000.0.38f1)
**Source:** `C:\Unity\DontLoseYourHead`
**Supabase:** Direct MCP access available (game_sessions, session_players, players tables)
**Document Version:** 94
**Last Updated:** January 25, 2026

**Archive:** `DLYH_Status_Archive.md` - Historical designs, old version history, completed phase details, DAB reference patterns

**Completed Refactoring:** `Documents/Refactor/DLYH_RefactoringPlan_Phase3_01192026.md` - Phase 3 complete (Sessions 1-4 + Memory Audit)

**Active Plan:** `Documents/DLYH_NetworkingPlan_Phase_E.md` - Networking implementation (7 sessions planned)

---

## Quick Context

**What is this game?** A competitive word game combining Hangman's letter-guessing with Battleship's grid-based hidden information. Players place hidden words on grids and take turns guessing letters or coordinates to find opponent's words before the guillotine blade falls.

**Key Innovation:** Asymmetric difficulty - mixed-skill players compete fairly with different grid sizes, word counts, and difficulty settings.

**Current Phase:** Phase E - Networking & Auth

**Scene to Use:** NetworkingScene.unity

---

## Last Session (Jan 25, 2026)

Session 80 - **Mobile Layout Fix: Option C (Keyboard Shrink + ScrollView Wrappers)**

**Issue:** Height starvation causes element overlap on mobile/small viewports
- Keyboard was fixed width (wouldn't shrink below 32px per key)
- No scrolling mechanism when content exceeds viewport height

**Solution Implemented (Option C):**
1. **Keyboard keys shrink to fit viewport width** - flex-shrink: 1 with min-width: 22px
2. **ScrollView wrappers on all screens** - graceful scroll when height-starved

**Files Changed:**

**SetupWizard.uss:**
- Letter keys: Added flex-shrink: 1, min-width: 22px, min-height: 22px
- Added .placement-scroll styling with #unity-content-viewport and #unity-content-container targeting

**Gameplay.uss:**
- Same keyboard shrink changes as SetupWizard
- Added .gameplay-scroll styling with ID selectors for Unity internals

**MainMenu.uss:**
- Added .menu-scroll styling with centering via ID selectors

**SetupWizard.uxml:**
- Wrapped placement-panel content in `<ui:ScrollView name="placement-scroll">`

**Gameplay.uxml:**
- Wrapped gameplay content in `<ui:ScrollView name="gameplay-scroll">`

**MainMenu.uxml:**
- Wrapped menu-content in `<ui:ScrollView name="menu-scroll">`
- Footer stays OUTSIDE scroll (fixed at bottom)

**Key CSS Fix (Round 2):**
Initial attempt used class selectors (`.unity-scroll-view__content-viewport`) which didn't work.
Changed to ID selectors (`#unity-content-viewport`, `#unity-content-container`) which target Unity's internal element names correctly. This fixed content centering.

**Test Results:**
- PC Unity Editor: Content centered properly
- PC WebGL: Scrollbar appears on Setup when it shouldn't (regression)
- Mobile Gameplay: Scrollbar visible but doesn't scroll, overlap still present
- Mobile Setup: Almost perfect, but word row buttons overlap letters, horizontal scrollbar

**New Issues Found:**
1. ScrollView not scrolling on mobile gameplay (scrollbar visible but broken)
2. Word row action buttons (GUESS, +/X) overlap letter cells on both screens
3. Unwanted horizontal scrollbar on Setup mobile
4. Grid too small on mobile - not using available container width
5. Attack card "The Executioner" text overflows card
6. PC scrollbar regression (shows when not needed)

**Next Steps:**
1. Fix word row button overlap (highest priority)
2. Debug ScrollView scrolling behavior
3. Remove unwanted horizontal scrollbar
4. Improve grid sizing for mobile
5. Fix Attack card text overflow

**Previous Session:** Session 79 - Mobile layout debugging (debug overlay added)

---

## Previous Session (Jan 25, 2026)

Session 79 - **Mobile Layout Debugging (Height Starvation Issue)**

**Issue:** Grid cascade loop fixed, but elements still overlap on mobile iframe

**Two Distinct Problems Identified:**
1. Cascade loop (FIXED in Session 78) - measuring stable slot instead of content-driven element
2. Height starvation overlap (NEW) - when viewport too short, elements overlap instead of yielding

**Debug Overlay Added:**
- Shows screen dimensions, root height, tabs height, grid height
- Words section height, keyboard height, sum of both
- Overflow indicator (YES/no)

**Files Changed:**
1. Gameplay.uxml - Added debug-layout label
2. Gameplay.uss - Added .debug-layout styling
3. UIFlowController.cs - Added `SetupLayoutDebugOverlay()` method

**Previous Session:** Session 78 - Mobile WebGL Grid Layout Fix (cascade loop)

---

## Previous Session (Jan 25, 2026)

Session 78 - **Mobile WebGL Grid Layout Fix**

**Issue:** Grid overlaps other UI elements on mobile WebGL; cascading resize loop

**Root Cause (identified via cross-AI collaboration with ChatGPT):**
- TableView was measuring `table-container` which is CONTENT-DRIVEN (shrinks with children)
- When cells resize smaller, container shrinks, triggering GeometryChangedEvent
- Caused infinite cascade: resize -> shrink -> resize -> shrink...

**Solution:** Measure from a STABLE SLOT instead of content-driven element
- `grid-area` and `placement-panel` are sized by their PARENT (stable)
- Their size doesn't change when children resize
- No feedback loop possible

**Changes Made (3 rounds of fixes):**

Round 1 - Initial container-based sizing:
- TableView.cs: GeometryChangedEvent callback, contentRect-based sizing, CELL_MARGIN fix
- Gameplay.uss / SetupWizard.uss: Added min-height: 0, min-width: 0 for flex shrinking

Round 2 - Band-aids (later replaced):
- 5% threshold guard, explicit _tableRoot sizing

Round 3 - Proper architectural fix:
- TableView.cs: Renamed `_tableContainer` -> `_measurementSlot`, added `SetMeasurementSlot()` method
- UIFlowController.cs: Pass `placement-panel` (setup) and `grid-area` (gameplay) as measurement slots
- Cell-size based guard instead of percentage threshold
- Removed explicit _tableRoot sizing

**Test Results:** Grid cascade loop FIXED
- Grid no longer triggers infinite resize loop
- But height starvation overlap issue discovered (see Session 79)

**Key Lesson Added:** Never size UI Toolkit content from an element whose size is affected by that content. Always measure from a parent-allocated, layout-stable slot.

---

## Previous Session (Jan 25, 2026)

Session 77 - **Session 4 Fixes: Root Cause Analysis, Implementation & Testing**

**Root Cause Analysis (with cross-AI troubleshooting):**

Identified 4 independent authority gaps causing the multiplayer issues:

1. **Polling Race Condition** - `_hasActiveGame` set 22 lines AFTER `StartOpponentJoinPolling()` was called, causing polling loop to exit immediately on first iteration

2. **Turn Authority Split** - Multiple places tried to initialize `currentTurn`:
   - `GameStateSynchronizer.SetPlayerReadyAsync()` - sets it correctly
   - `MatchmakingService` - only sets status, NOT currentTurn
   - Result: `currentTurn == null` for real multiplayer, both players see "Opponent's turn"

3. **Placeholder Name Never Updated** - `opponentName = "Host"` in JoinGame mode was a TODO that never got implemented

4. **Identity Mismatch Risk** - `isPlayer1` passed to GameStateSynchronizer could be reconstructed incorrectly on resume

**Fixes Implemented (Round 1):**

1. **UIFlowController.cs** - Polling Race Condition Fix (lines ~5942-5978)
   - Moved `_isPlayerTurn`, `_isGameOver`, `_hasActiveGame` to BEFORE `StartOpponentJoinPolling()`
   - Polling loop now sees correct flag values on first iteration

2. **GameSessionService.cs** - Authoritative Turn Initialization (lines ~230-254)
   - Added guard in `UpdateGameState()` that checks:
     - `currentTurn == null` AND `player1.setupComplete` AND `player2.setupComplete`
   - When all conditions met, initializes: `status = "playing"`, `currentTurn = "player1"`, `turnNumber = 1`
   - Single source of truth for game start - no more race conditions

3. **UIFlowController.cs** - Opponent Name Resolution for Joiner (lines ~5852-5886)
   - Replaced `"Host"` placeholder with actual Supabase lookup
   - Fetches from `session_players.player_name` (priority) or `game_state.player1.name` (fallback)
   - Falls back to "Opponent" only if both unavailable

**WebGL Test Results:**
- Polling fix WORKS - mobile detected PC joining (went from "Waiting..." to "Opponent joined!")
- Turn initialization WORKS - PC (joiner) correctly shows "YOUR TURN" at game start
- Joiner sees host name correctly - PC log shows "host: Playerlaptop"
- Both players could take one turn each

**Issues Found:**
- Host sees "Opponent" instead of joiner's actual name (polling code only checked session_players, not game state)
- After both take one turn, game stuck on "Opponent's turn" for both (expected - Session 5 turn sync not implemented)
- Attack grid shows 0 word placements for joiner (expected - Session 5 will sync opponent's placements)

**Fixes Implemented (Round 2):**

4. **UIFlowController.cs** - Host Opponent Name Resolution (lines ~6550-6590)
   - Updated polling code to also check `game_state.player2.name` when `session_players.player_name` is null
   - Names are stored in game state JSON, not session_players table

**Key Insight:** These weren't networking bugs - they were authority/initialization bugs that manifested as networking issues in WebGL due to timing differences.

**Previous Session:** Session 76 - Opponent Join Detection (implementation + testing revealed issues)

---

## Next Session Priorities

**Test Session 4 Fixes (Round 2):**
1. Verify host sees joiner's actual name (not "Opponent")
2. Verify joiner sees host's name (already working)

**Session 5 - Turn Synchronization:**
- Implement polling for opponent's turn completion so game doesn't lock up
- Sync opponent's word placements so attack grid shows their words
- This is required for playable multiplayer

---

## Active TODO

**Full Plan:** See `Documents/DLYH_NetworkingPlan_Phase_E.md` for detailed tasks and implementation notes.

### Phase E Sessions Overview

| Session | Focus | Status |
|---------|-------|--------|
| 1 | Foundation & Editor Identity | COMPLETE |
| 2 | Phantom AI as Session Player | COMPLETE |
| 3 | Game State Persistence | COMPLETE |
| 4 | Opponent Join Detection | IN PROGRESS (polling works, name fix pending test) |
| 5 | Turn Synchronization | PENDING |
| 6 | Activity Tracking & Auto-Win | PENDING |
| 7 | Rematch UI Integration | PENDING |
| 8 | Code Quality & Polish | PENDING |

### Current: Session 3 - Game State Persistence (continued)

- [x] Save revealed cells to Supabase
- [x] Restore cells from saved state
- [x] Fix cell C3 showing wrong state (letter when not keyboard-guessed)
- [x] Fix case-sensitivity in GetOpponentLetterPositions
- [x] Fix case-sensitivity in WordRowView.SetWord (uppercase word storage)
- [x] Fix word guess letters not being saved to knownLetters
- [x] Fix solved words not appearing in Guessed Words panel on restore
- [x] Restructure defense card restore (data collection then highlighting)
- [x] Clarify Defense card logic (keyboard/word rows vs grid cells)
- [x] Fix word guess handling (add to knownLetters, only upgrade existing grid cells)
- [x] Fix attack card cell highlights not restoring (two-pass approach)
- [x] Fix opponent guessed letters not persisting (RestoreOpponentGuessState)
- [x] Fix word index mismatch between GuessManager and UI (sort by length)
- [x] **VERIFIED: Attack card matches original game state on resume**
- [x] **VERIFIED: Defense card matches original game state on resume**
- [x] **FIXED: Incorrect word guesses now restore on resume** (serialization + parsing)
- [x] Remove debug logging after all fixes confirmed

### Session 4 - Opponent Join Detection (COMPLETE)

- [x] Implement opponent join polling (moved from WaitingRoom to UIFlowController gameplay screen)
- [x] Detect when real player joins private game (polls Supabase every 3 seconds)
- [x] Update UI to show opponent name when joined (SetOpponentName method)
- [x] Handle host starting game before opponent joins (waiting state with polling)
- [x] Refresh Active Games list when opponent joins

### Session 5 - Turn Synchronization (Next)

- [ ] Implement realtime sync for opponent moves during gameplay
- [ ] Currently requires logout/login to see opponent's moves
- [ ] Add WebSocket or polling for turn updates

### Phase F: Cleanup & Polish

- [ ] Remove legacy uGUI components
- [ ] Delete abandoned prefabs/scripts
- [ ] Namespace standardization (TecVooDoo.DontLoseYourHead.* or DLYH.*)
- [ ] Essential animations (guillotine blade drop, head fall)
- [ ] Screen transitions
- [ ] Character art update: selectable heads, swappable hair, hair color matches player color

### Phase G: Deploy to Playtest

- [ ] WebGL build pipeline
- [ ] Deploy to Cloudflare (dlyh.pages.dev)
- [ ] Mobile browser testing & fixes
- [ ] Verify telemetry working

---

## Known Issues

**UI/Layout:**
- **PARTIALLY FIXED:** Viewport scaling now works at 1366x768 (no overlap)
- 1920x1080 has too much empty space at bottom - needs grid/cell size tuning
- Player tabs don't shrink to give more room for grid content
- **MOBILE:** Grid cascade loop fixed (Session 78), ScrollView approach partially working (Session 80) - scrollbar visible but not scrolling, word row button overlap, grid sizing issues

**Architecture:**
- UIFlowController at ~5,298 lines - **Phase 3 COMPLETE** (see `Documents/Refactor/DLYH_RefactoringPlan_Phase3_01192026.md`)
- Inconsistent namespace convention (TecVooDoo.DontLoseYourHead.* vs DLYH.*) - deferred to Phase F

**Networking:** (See `DLYH_NetworkingPlan_Phase_E.md` for full gap analysis)
- ~~Editor identity persistence needs verification~~ VERIFIED - PlayerPrefs works correctly
- **Opponent join polling implemented but NOT WORKING** - UI stays on "Waiting...", needs debugging
- **Turn state mismatch** - Both players see "Opponent's turn" after joining
- **Find Opponent shows "Host" instead of player name** - Name not passed correctly
- **Find Opponent games fail to resume** - Both players get errors
- ~~Phantom AI not inserted into session_players~~ FIXED - Now creates player record and session_players row
- 5-day auto-win not implemented (needs Supabase edge function) - Session 6
- RematchService not wired to UI - Session 7
- Word placement encryption is just Base64 (not secure) - Session 8
- WebGL realtime incomplete (WebSocket bridge missing) - Session 5
- ~~WebGL used secret key instead of anon key~~ FIXED - Using JWT anon key now
- ~~Exit button nested iframe in WebGL~~ FIXED - Uses window.top.location
- ~~Game locked after move in multiplayer~~ FIXED - Tab switching enabled during opponent turn
- ~~Miss limit mismatch between devices~~ FIXED - Uses stored missLimit from Supabase

**Audio:**
- Music crossfading/switching too frequently (should only switch at end of track)

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

**Audio:**
- Music playback with shuffle and crossfade
- SFX system with mute controls
- Dynamic tempo changes under danger
- Guillotine execution sound sequence

**Telemetry:**
- Cloudflare workers endpoint for event capture
- Session, game, guess, feedback, and error tracking

**UI Toolkit (Phases A-D Complete):**
- TableModel/TableView data-driven grid
- Setup wizard with progressive card disclosure
- Word rows with variable lengths (3,4,5,6)
- Autocomplete dropdown for word entry
- 8-direction grid placement with preview
- Main menu with inline settings
- Hamburger menu navigation
- Attack/Defend tab switching
- Guillotine overlay with 5-stage visual
- Game end sequence with animations
- How to Play modal

**Networking UI (Phase E In Progress):**
- Online mode choice (Find Opponent vs Private Game)
- MatchmakingOverlay, WaitingRoom, JoinCodeEntry overlays
- NetworkingUIManager manages all overlays
- Phantom AI fallback with random names
- Private Game creates Supabase record with join code
- JoinCodeEntry looks up and joins existing games
- "My Active Games" list on main menu
- Resume game loads state from Supabase
- Waiting state blocks input until opponent joins

---

## Architecture

### Key Scripts

| Script | Lines | Purpose |
|--------|-------|---------|
| UIFlowController | ~5298 | Screen flow + gameplay orchestration |
| SetupWizardUIManager | ~820 | Setup wizard flow |
| GameplayScreenManager | ~650 | Gameplay UI state, tab switching, keyboard |
| GuillotineOverlayManager | ~450 | Guillotine overlay modal controller |
| NetworkingUIManager | ~640 | Networking overlay management |
| ExecutionerAI | ~493 | AI opponent coordination |
| GameStateManager | ~400 | Game state parsing/serialization |
| ActiveGamesManager | ~360 | My Active Games list management |
| ConfirmationModalManager | ~200 | Confirmation dialog management |
| HelpModalManager | ~230 | How to Play modal management |
| JsonParsingUtility | ~100 | Unified JSON field extraction |

### Namespaces

| Namespace | Purpose |
|-----------|---------|
| `TecVooDoo.DontLoseYourHead.UI` | Main UI scripts |
| `TecVooDoo.DontLoseYourHead.Core` | Game state, difficulty |
| `DLYH.AI.Core` | AI controllers |
| `DLYH.AI.Strategies` | AI guess strategies |
| `DLYH.Audio` | Audio managers |
| `DLYH.TableUI` | UI Toolkit implementation |
| `DLYH.UI.Managers` | Extracted UI managers (Phase 3) |
| `DLYH.Core.Utilities` | Shared utilities (JsonParsingUtility) |
| `DLYH.Networking` | Opponent abstraction |
| `DLYH.Networking.UI` | Networking overlays |
| `DLYH.Networking.Services` | Supabase, auth, player, realtime |

### Key Folders

```
Assets/DLYH/
  Scripts/
    AI/           - ExecutionerAI, strategies, config
    Audio/        - UIAudioManager, MusicManager, GuillotineAudioManager
    Core/
      Utilities/  - JsonParsingUtility (Phase 3)
    Networking/   - IOpponent, services
    UI/
      Managers/   - GameStateManager, ActiveGamesManager, modals (Phase 3)
      ...         - TableModel, TableView, UIFlowController, etc.
  UI/             - UI Toolkit assets (UXML, USS)
  Scenes/
    NetworkingScene.unity  - Primary active scene
    NetworkingBackup.unity - Backup before networking work
```

### Opponent Abstraction

Game logic is opponent-agnostic. Whether AI, Phantom AI, or Remote Player:
1. Fires `OnOpponentTurnStarted` event
2. Waits for guess via `IOpponent.OnLetterGuess` / `OnCoordinateGuess` / `OnWordGuess`
3. Processes guess using SAME code path as player guesses
4. Updates UI using `IOpponent.OpponentColor`
5. Fires `OnOpponentTurnEnded` event

**Important:** Use `_opponent` (not `_aiOpponent`), handlers are `HandleOpponent*`, `CellOwner` only has `Player` and `Opponent`.

---

## Game Rules

### Core Concept
DLYH is Battleship with words. Both players see Attack and Defend boards - Attack shows opponent's hidden words (where you guess), Defend shows your words (what opponent found).

### Turn Flow
1. First turn random
2. Make ONE action: pick letter, pick coordinate, or guess word
3. Turn switches (except: extra turn on word completion)

### Actions

**Pick Letter:** Hit = letter fills in word rows (yellow or player color based on coordinate knowledge). Miss = +1 miss.

**Pick Coordinate:** Hit = cell highlights (yellow if letter unknown, player color if known). Miss = +1 miss.

**Guess Word:** Correct = word fills in + extra turn. Incorrect = +2 misses.

### Color Rules
- Red = Miss
- Yellow = Hit but incomplete (letter OR coordinate unknown)
- Player Color = Fully known (both letter AND coordinate)

### Miss Limit Formula
```
MissLimit = 15 + OpponentGridBonus + OpponentWordModifier + YourDifficultyModifier

OpponentGridBonus: 6x6=+3, 7x7=+4, 8x8=+6, 9x9=+8, 10x10=+10, 11x11=+12, 12x12=+13
OpponentWordModifier: 3 words=+0, 4 words=-2
YourDifficultyModifier: Easy=+4, Normal=+0, Hard=-4
```

### Win Conditions
1. Reveal ALL opponent's words AND grid coordinates
2. Opponent reaches miss limit
3. (Online) Opponent abandons after 5 days

---

## Lessons Learned

### Unity/C# Patterns
1. **Set state BEFORE firing events** - handlers may check state immediately
2. **Initialize UI to known states** - don't rely on defaults
3. **Kill DOTween before reset** - prevents animation conflicts
4. **Store original positions** - for proper reset after animations
5. **Use New Input System** - `Keyboard.current`, not `Input.GetKeyDown`
6. **No `var` keyword** - explicit types always
7. **800 lines max** - extract controllers when approaching limit
8. **Prefer UniTask over coroutines** - `await UniTask.Delay(1000)`
9. **No allocations in Update** - cache references, use object pooling
10. **Validate after MCP edits** - run validate_script to catch syntax errors

### Project-Specific
11. **Unity 6 UIDocument bug (IN-127759)** - assign Source Asset to prevent blue screen
12. **Use E: drive path** - never worktree paths
13. **Check scene file diffs** - layout can be accidentally modified
14. **Reuse existing systems** - create thin adapters instead of rebuilding
15. **Prevent duplicate event handlers** - use flags like `_keyboardWiredUp`
16. **Reset validity on clear** - SetWordValid(false) when clearing words
17. **Case-sensitivity in char comparisons** - always ToUpper() both sides when comparing letters
18. **Supabase anon vs secret keys** - secret keys blocked in browsers; use anon key (JWT) for client code
19. **Iframe navigation** - use `window.top.location` to escape iframe context
20. **UI Toolkit stable slot measurement** - never measure from content-driven elements; use parent-allocated stable slots (grid-area, placement-panel) for GeometryChangedEvent sizing
21. **Unity UI Toolkit ScrollView internals** - use ID selectors (#unity-content-viewport, #unity-content-container) not class selectors to target internal elements

---

## Bug Patterns to Avoid

| Bug Pattern | Cause | Prevention |
|-------------|-------|------------|
| State set AFTER events | Handlers see stale state | Set state BEFORE firing events |
| Autocomplete floating at top | Not hidden at init | Call Hide() in Initialize() |
| Board not resetting | No reset logic | Call ResetGameplayState() on new game |
| Guillotine head stuck | No stored position | Store original position on Initialize |
| Green cells after clear | Validity not reset | SetWordValid(false) in HandleWordCleared |
| Old screen still visible | Only showing new | Hide ALL other screens when showing new |
| Unicode not rendering in WebGL | Font support | Use ASCII fallbacks |
| Letter comparison fails | Case mismatch | ToUpper() both sides in comparisons |

---

## Coding Standards

- Prefer async/await (UniTask) over coroutines
- Avoid allocations in Update
- No per-frame LINQ
- Clear separation between logic and UI
- ASCII-only documentation and identifiers
- No `var` keyword - explicit types always
- 800-1200 lines per file (extract when exceeding with clear responsibilities)

---

## Development Priorities (Ordered)

1. **SOLID principles first**
2. **Memory efficiency second** - no per-frame allocations
3. **Clean code third** - readable, maintainable
4. **Self-documenting code fourth** - clear naming over comments
5. **Platform best practices fifth** - Unity > C#, Cloudflare/Supabase > HTML/JS

---

## AI Rules

1. **Verify names exist** - search before referencing files/methods/classes
2. **Step-by-step verification** - one step at a time, wait for confirmation
3. **Read before editing** - always read files before modifying
4. **ASCII only** - no smart quotes, em-dashes, or special characters
5. **Prefer structured edits** - use script_apply_edits for method changes
6. **Be direct** - give honest assessments, don't sugar-coat

---

## Reference Documents

| Document | Path | Purpose |
|----------|------|---------|
| Networking Plan | `Documents/DLYH_NetworkingPlan_Phase_E.md` | Phase E implementation plan (7 sessions) |
| Phase 3 Refactor | `Documents/Refactor/DLYH_RefactoringPlan_Phase3_01192026.md` | Completed architecture refactor |
| DAB Status | `E:\TecVooDoo\Projects\Games\4 Playtesting\Dots and Boxes\Documents\DAB_Status.md` | Working async multiplayer reference |
| TecVooDoo Web Status | `E:\TecVooDoo\Projects\Other\TecVooDooWebsite\Documents\TecVooDoo_Web_Status.md` | Supabase backend, auth |
| DLYH Archive | `DLYH_Status_Archive.md` | Historical designs, DAB patterns, old versions |

---

## Version History (Recent)

| Version | Date | Summary |
|---------|------|---------|
| 94 | Jan 25, 2026 | Session 80 - Option C (keyboard shrink + ScrollView wrappers on all screens) |
| 93 | Jan 25, 2026 | Session 79 - Mobile layout debugging (height starvation, debug overlay) |
| 92 | Jan 25, 2026 | Session 78 - Mobile WebGL grid layout fix (stable slot measurement) |
| 91 | Jan 25, 2026 | Session 77 - Session 4 fixes (polling, turn init, opponent names) |
| 88 | Jan 23, 2026 | Session 75 - WebGL multiplayer fixes (API key, exit button iframe, game lock, miss limit) |
| 87 | Jan 22, 2026 | Session 74 - Viewport scaling tuning, WebGL build prep |
| 86 | Jan 22, 2026 | Session 73 - UI viewport scaling (partial) - 768p working, 1080p needs tuning |
| 85 | Jan 22, 2026 | Session 72 - Guillotine stage movement fix, debug logging cleanup, Session 3 COMPLETE |
| 84 | Jan 22, 2026 | Session 72 - Guillotine stage movement fix (synced thresholds, slower stage 5 animation) |
| 83 | Jan 22, 2026 | Session 71 - Guillotine stage movement debugging, threshold rework (needs further discussion) |

**Full version history:** See `DLYH_Status_Archive.md`

---

## Session Close Checklist

- [x] Update "Last Session" with date and summary
- [x] Update Active TODO (mark complete, add new)
- [x] Add any new issues to Known Issues
- [x] Add new lessons to Lessons Learned if applicable
- [x] Update Architecture if files added/extracted
- [x] Increment version number
- [ ] Archive old version history entries (keep ~6)

---

**End of Project Status**

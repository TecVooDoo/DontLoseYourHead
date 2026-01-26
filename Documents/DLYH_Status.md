# Don't Lose Your Head - Project Status

**Project:** Don't Lose Your Head (DLYH)
**Developer:** TecVooDoo LLC / Rune (Stephen Brandon)
**Platform:** Unity 6.3 (6000.0.38f1)
**Source:** `C:\Unity\DontLoseYourHead`
**Supabase:** Direct MCP access available (game_sessions, session_players, players tables)
**Document Version:** 98
**Last Updated:** January 26, 2026

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

## Last Session (Jan 26, 2026)

Session 83 - **Session 5 Analysis (Turn Synchronization)**

**Goal:** Analyze codebase for Session 5 implementation requirements.

**Analysis Findings:**

1. **State Push (5.1) - VERIFIED COMPLETE:**
   - `SaveGameStateToSupabaseAsync()` in UIFlowController.cs (line 925) handles all state persistence
   - Called after player turn (line 2585) and opponent turn (line 2729)
   - Saves: `currentTurn`, `turnNumber`, `misses`, `revealedCells`, `knownLetters`, `solvedWordRows`

2. **RemotePlayerOpponent Status:**
   - Class exists with full turn detection logic (`DetectOpponentAction()` at line 350)
   - Detects: coordinate guesses, letter guesses, word solves, miss increases
   - **NOT WIRED UP** - TODO at UIFlowController.cs:5786 says "TODO: Set up RemotePlayerOpponent"
   - Currently only phantom AI games work; real multiplayer has no turn sync

3. **HandleOpponentJoined() Gap:**
   - When opponent joins (line 6733), only updates name in UI
   - TODO at line 6756: "Load opponent's setup data from Supabase (grid, placements, etc.)"
   - Attack grid not created for real multiplayer - needs opponent's grid size/word count

4. **State Synchronization Architecture:**
   - `GameStateSynchronizer.PushLocalStateAsync()` exists but never called
   - `NetworkGameManager.PushGameStateAsync()` wraps it but never called
   - UIFlowController uses direct `SaveGameStateToSupabaseAsync()` instead (works fine)

**Previous Session:** Session 82 - Layout Polish (Setup + Gameplay)

---

## Next Session Priorities

**Session 5 - Turn Synchronization (Ready to Implement):**

Start from the analysis above. Key tasks in order:

1. **5.2: Load opponent setup when they join**
   - Modify `HandleOpponentJoined()` to fetch opponent's DLYHPlayerData
   - Get gridSize, wordCount, color from game state
   - Create attack grid with fog (opponent's word placements stay encrypted)

2. **5.3: Wire up RemotePlayerOpponent for real multiplayer**
   - Create RemotePlayerOpponent in `TransitionToGameplay()` when `!IsPhantomAI`
   - Pass game code and player info to constructor
   - Subscribe to `OnLetterGuess`, `OnCoordinateGuess`, `OnWordGuess` events

3. **5.4: Turn detection via state diff**
   - RemotePlayerOpponent already has `DetectOpponentAction()` logic
   - Need to poll or subscribe to Supabase state changes
   - Fire events when opponent's gameplay state changes

4. **5.5: "Waiting for opponent..." indicator**
   - Show during opponent's turn in real multiplayer
   - Hide when turn changes back to player

5. **5.6: Turn number conflicts**
   - Current implementation increments turn number on save
   - May need optimistic locking or Supabase RPC for race conditions

6. **5.7: End-to-end test**
   - Two browser windows playing against each other

---

## Active TODO

**Full Plan:** See `Documents/DLYH_NetworkingPlan_Phase_E.md` for detailed tasks and implementation notes.

### Phase E Sessions Overview

| Session | Focus | Status |
|---------|-------|--------|
| 1 | Foundation & Editor Identity | COMPLETE |
| 2 | Phantom AI as Session Player | COMPLETE |
| 3 | Game State Persistence | COMPLETE |
| 4 | Opponent Join Detection | COMPLETE |
| 5 | Turn Synchronization | IN PROGRESS (analysis complete, implementation next) |
| 6 | Activity Tracking & Auto-Win | PENDING |
| 7 | Rematch UI Integration | PENDING |
| 8 | Code Quality & Polish | PENDING |

### Session 3 - Game State Persistence (COMPLETE)

All game state persistence tasks completed - attack/defense cards restore correctly on resume.

### Session 4 - Opponent Join Detection (COMPLETE)

- [x] Implement opponent join polling (moved from WaitingRoom to UIFlowController gameplay screen)
- [x] Detect when real player joins private game (polls Supabase every 3 seconds)
- [x] Update UI to show opponent name when joined (SetOpponentName method)
- [x] Handle host starting game before opponent joins (waiting state with polling)
- [x] Refresh Active Games list when opponent joins

### Session 5 - Turn Synchronization (IN PROGRESS - Analysis Complete)

**State Push (5.1):** VERIFIED - `SaveGameStateToSupabaseAsync()` saves after each turn

**Key Files:**
- `UIFlowController.cs` - Main state save logic, needs RemotePlayerOpponent wiring
- `RemotePlayerOpponent.cs` - Has turn detection logic, not connected
- `GameStateSynchronizer.cs` - State sync helpers (optional use)

**Implementation Tasks:**
- [ ] 5.2: Load opponent setup data when they join (grid size, word count, color)
- [ ] 5.3: Create RemotePlayerOpponent for real multiplayer (not phantom AI)
- [ ] 5.4: Wire RemotePlayerOpponent events to update local state
- [ ] 5.5: Add "Waiting for opponent..." indicator during their turn
- [ ] 5.6: Handle turn number conflicts (race conditions)
- [ ] 5.7: Test full 2-player game flow

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

**UI/Layout:** (Sessions 78-82 troubleshooting COMPLETE - see `DLYH_Troubleshooting_Archive.md`)
- ~~Viewport scaling~~ FIXED - content-column + flex-shrink: 0 + width-only sizing
- ~~Mobile grid/word row/keyboard overlap~~ FIXED - vertical scroll triggers when content exceeds viewport
- ~~Word row button overlap~~ FIXED - spacer pushes buttons right
- ~~Grid/word row misalignment~~ FIXED - align-items: flex-start
- Minor polish deferred to Steam_UI_Polish.md and Mobile_UI_Polish.md (future)

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
| 97 | Jan 26, 2026 | Session 82 - Layout troubleshooting CLOSED, verified on PC + mobile |
| 96 | Jan 26, 2026 | Session 82 - Layout polish (grid alignment, cell overlap, section spacing) |
| 95 | Jan 26, 2026 | Session 81 - Content column layout refactoring |
| 94 | Jan 25, 2026 | Session 80 - Option C (keyboard shrink + ScrollView wrappers on all screens) |

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

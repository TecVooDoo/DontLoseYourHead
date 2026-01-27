# Don't Lose Your Head - Project Status

**Project:** Don't Lose Your Head (DLYH)
**Developer:** TecVooDoo LLC / Rune (Stephen Brandon)
**Platform:** Unity 6.3 (6000.0.38f1)
**Source:** `C:\Unity\DontLoseYourHead`
**Supabase:** Direct MCP access available (game_sessions, session_players, players tables)
**Document Version:** 104
**Last Updated:** January 27, 2026

**Archive:** `DLYH_Status_Archive.md` - Historical designs, old version history, completed phase details, DAB reference patterns

**Completed Refactoring:** `Documents/Refactor/DLYH_RefactoringPlan_Phase3_01192026.md` - Phase 3 complete (Sessions 1-4 + Memory Audit)

**Active Plan:** `Documents/DLYH_NetworkingPlan_Phase_E_Updated.md` - Networking implementation (REVISED Jan 26, 2026)

---

## Build Checklist

Before each build:
1. **Update version number** in Player Settings (Edit > Project Settings > Player > Version)
   - Current: 3.1 (last known)
   - Format: Major.Minor (increment minor for each test build)
2. **Debug overlay** is currently DISABLED (line 443 in UIFlowController.cs)
   - To re-enable for layout debugging: uncomment `CreateGlobalDebugOverlay();`
3. Version displays on main menu via `Application.version`

---

## Quick Context

**What is this game?** A competitive word game combining Hangman's letter-guessing with Battleship's grid-based hidden information. Players place hidden words on grids and take turns guessing letters or coordinates to find opponent's words before the guillotine blade falls.

**Key Innovation:** Asymmetric difficulty - mixed-skill players compete fairly with different grid sizes, word counts, and difficulty settings.

**Current Phase:** Phase E - Networking & Auth

**Scene to Use:** NetworkingScene.unity

---

## Last Session (Jan 27, 2026)

Session 85 - **Build 5, 5b, 5c: Multiplayer Turn Sync Fixes**

### Summary

Fixed multiple issues blocking real-time multiplayer turn synchronization:
1. Build 5: UI rebuild when opponent joins + Resume path for real multiplayer
2. Build 5b: JoinGame path word placements decryption
3. Build 5c: Turn switching when opponent completes turn + word row sorting

### Build 5c Changes (Latest)

**File:** `UIFlowController.cs`

**Fix 4: HandleOpponentThinkingComplete Turn Switch (line ~3711)**
- Previous handler only logged, didn't actually switch turn
- Now properly sets `_isPlayerTurn = true`
- Updates UI via `SetPlayerTurn(true)` and `SelectAttackTab()`
- Enables player to take their turn after opponent finishes

**Fix 5: Word Row Sorting (line ~6686)**
- Added sorting to `SetupGameplayWordRowsWithOpponentData`
- Word rows now display shortest to longest (consistent with resume path)

### Build 5b Fix

**Fix 3: JoinGame Path Word Placements (line ~6067)**
- JoinGame path (joiner) wasn't decrypting host's `wordPlacementsEncrypted`
- Added decryption so joiner has opponent data for attack grid and word rows

### Build 5 Fixes

**Fix 1: RebuildUIForOpponentJoinAsync() (lines 7219-7293)**
- Rebuilds gameplay UI when opponent joins with their actual setup data

**Fix 2: Resume Path for Real Multiplayer (lines 1259-1306)**
- Creates `RemotePlayerOpponent` for real multiplayer resume
- Starts turn detection polling

### Test Results

**Build 5b Testing (Mobile host, PC joiner):**
- ✅ Both sides loaded opponent setup data
- ✅ Attack grids showed correct sizes
- ✅ Word rows displayed (but wrong order)
- ❌ Both stuck on "Opponent's Turn" after one turn each
- ❌ Word rows not sorted ascending by length

**Build 5c should fix:** Turn switching and word row sorting

**Known Issue (Deferred):** Miss count values differ between players (PC shows 0/27 for both, Mobile shows 0/20 and 0/34)

**See:** `DLYH_Troubleshooting.md` for full implementation details

---

## Next Session Priorities

### Build 5c Deployed - Verify Turn Switching

Build 5c adds the critical `HandleOpponentThinkingComplete` fix. **Testing required:**

1. **Test Turn Switching:**
   - Start private game (either device host)
   - Take turns on both devices
   - Verify turn indicator updates correctly
   - Verify polling detects opponent moves and switches turn back
   - Console log to verify: `HandleOpponentThinkingComplete - turn switched to player`

2. **Test Word Row Order:**
   - Verify word rows display shortest to longest on both devices

3. **If turn switching works - move to Session 6:**
   - 5-day inactivity auto-win (Supabase edge function)
   - Investigate miss count mismatch
   - Turn/version guarding if race conditions found

### Session 6 - Activity Tracking & Auto-Win (After Build 5c Verified)

---

## Active TODO

**Full Plan:** See `Documents/DLYH_NetworkingPlan_Phase_E_Updated.md` for detailed tasks and implementation notes.

### Phase E Sessions Overview

| Session | Focus | Status |
|---------|-------|--------|
| 1 | Foundation & Editor Identity | COMPLETE |
| 2 | Phantom AI as Session Player | COMPLETE |
| 3 | Game State Persistence | COMPLETE |
| 4 | Opponent Join Detection | COMPLETE |
| 5 | Turn Synchronization | **BUILD 5 - TESTING** |
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

### Session 5 - Turn Synchronization (Build 5 Ready for Testing)

**Implementation Tasks:**
- [x] 5.1: Extend NetworkingUIResult with opponent setup fields
- [x] 5.2: Complete HandleOpponentJoined() - fetches data and creates RemotePlayerOpponent
- [x] ~~5.2b~~ Data fetch already works - `HandleOpponentJoined` stores in `_matchmakingResult`
- [ ] 5.2c: Rebuild attack grid/word rows when opponent joins - **IMPLEMENTED (Build 5), NEEDS TESTING**
- [x] 5.3: Build attack grid using opponent setup dimensions (works on Resume)
- [x] 5.4: Create RemotePlayerOpponent for real multiplayer (live join path)
- [x] 5.5: Implement 2-second polling for turn detection
- [x] 5.6: Add "Waiting for opponent..." indicator
- [ ] 5.7: End-to-end test (two browser windows) - **READY FOR TESTING**
- [ ] 5.8: Block gameplay until UI correctly built - deferred (may not be needed)
- [x] ~~5.9~~ DetectOpponentAction player selection is CORRECT (verified in code review)
- [ ] 5.10: Fix Resume path: create RemotePlayerOpponent - **IMPLEMENTED (Build 5), NEEDS TESTING**
- [ ] 5.11: Fix Resume path: initialize `_lastKnownTurnNumber` - **IMPLEMENTED (Build 5), NEEDS TESTING**

**Build 5 Status:** Implementation complete. Deploy and test to verify fixes work correctly.

**Deferred to Session 6:** Turn/version guarding (not needed for async turn-based)

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

**Networking:** (See `DLYH_NetworkingPlan_Phase_E_Updated.md` for full plan)
- ~~Editor identity persistence needs verification~~ VERIFIED - PlayerPrefs works correctly
- ~~Opponent join polling implemented but NOT WORKING~~ FIXED (Session 4) - UI updates correctly
- ~~Turn state mismatch - Both players see "Opponent's turn"~~ FIXED (Build 4) - Turn tracking works
- ~~Find Opponent shows "Host" instead of player name~~ FIXED (Session 4)
- **Find Opponent games fail to resume** - Both players get errors (needs investigation)
- ~~Phantom AI not inserted into session_players~~ FIXED - Now creates player record and session_players row
- ~~Opponent setup not fetched on live join~~ CORRECTED - Data IS fetched, stored in `_matchmakingResult`
- **CRITICAL: UI not rebuilt when opponent joins** - Data loaded but attack grid/word rows not updated (Session 5)
- ~~RemotePlayerOpponent not wired~~ FIXED - Now created in HandleOpponentJoined() (Build 4)
- ~~DetectOpponentAction reads wrong player data~~ CORRECTED - Player selection logic is correct (verified)
- **Gameplay allowed on incomplete UI** - Can make moves before UI rebuilt with opponent data (Session 5)
- **Resume path missing RemotePlayerOpponent** - Real multiplayer games don't create opponent object (Session 5)
- **Resume path missing `_lastKnownTurnNumber` init** - Not initialized for real multiplayer (Session 5)
- 5-day auto-win not implemented (needs Supabase edge function) - Session 6
- RematchService not wired to UI - Session 7
- Word placement encryption is just Base64 (not secure) - Session 8
- WebGL realtime incomplete (WebSocket bridge missing) - using POLLING instead (Session 5)
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

1. **Read DLYH_CodeReference.md first** - check existing APIs before writing new code
2. **Working directory is C:\Unity\DontLoseYourHead** - NOT backup folders
3. **Verify names exist** - search before referencing files/methods/classes
4. **Step-by-step verification** - one step at a time, wait for confirmation
5. **Read before editing** - always read files before modifying
6. **ASCII only** - no smart quotes, em-dashes, or special characters
7. **Prefer structured edits** - use script_apply_edits for method changes
8. **Be direct** - give honest assessments, don't sugar-coat

---

## Reference Documents

| Document | Path | Purpose |
|----------|------|---------|
| **Code Reference** | `Documents/DLYH_CodeReference.md` | **READ FIRST** - Namespaces, classes, APIs, key methods |
| Networking Plan | `Documents/DLYH_NetworkingPlan_Phase_E_Updated.md` | Phase E implementation plan (REVISED) |
| Phase 3 Refactor | `Documents/Refactor/DLYH_RefactoringPlan_Phase3_01192026.md` | Completed architecture refactor |
| DAB Status | `E:\TecVooDoo\Projects\Games\4 Playtesting\Dots and Boxes\Documents\DAB_Status.md` | Working async multiplayer reference |
| TecVooDoo Web Status | `E:\TecVooDoo\Projects\Other\TecVooDooWebsite\Documents\TecVooDoo_Web_Status.md` | Supabase backend, auth |
| DLYH Archive | `DLYH_Status_Archive.md` | Historical designs, DAB patterns, old versions |

---

## Version History (Recent)

| Version | Date | Summary |
|---------|------|---------|
| 103 | Jan 27, 2026 | Session 85 - Build 5: UI rebuild & Resume path fixes implemented |
| 102 | Jan 27, 2026 | Session 84 - Corrected analysis: UI rebuild issue, not data fetch issue |
| 101 | Jan 26, 2026 | Session 84 - Build 4 tested, opponent data loading issue identified |
| 100 | Jan 26, 2026 | Session 84 - Turn tracking fixes (Build 4) |
| 99 | Jan 26, 2026 | Session 84 - Networking plan review & revision |
| 98 | Jan 26, 2026 | Session 83 - Session 5 analysis |

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
- [ ] **Update DLYH_CodeReference.md** if any scripts, APIs, or data models were added/changed

---

**End of Project Status**

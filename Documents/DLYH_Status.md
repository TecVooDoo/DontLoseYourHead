# Don't Lose Your Head - Project Status

**Project:** Don't Lose Your Head (DLYH)
**Developer:** TecVooDoo LLC / Rune (Stephen Brandon)
**Platform:** Unity 6.3 (6000.0.38f1)
**Source:** `C:\Unity\DontLoseYourHead`
**Supabase:** Direct MCP access available (game_sessions, session_players, players tables)
**Document Version:** 109
**Last Updated:** January 28, 2026

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

## Last Session (Jan 28, 2026)

Session 87 - **Session 8: Code Quality & Polish**

### Session 8 Summary

Completed all Session 8 tasks: dead code removal, error handling polish, encryption improvements, and namespace cleanup.

### 1. Dead Code Removal (~1,800+ lines removed)

Deleted obsolete files that were never used or superseded:
- `NetworkGameManager.cs` (~435 lines) - Unused orchestration layer
- `WaitingRoomController.cs` (~300 lines) - Replaced by UIFlowController overlays
- `GameStateSynchronizer.cs` (~689 lines) - Never wired, UIFlowController handles state sync
- `RematchService.cs` (~562 lines) - Session 7 deferred indefinitely

Cleaned up `RemotePlayerOpponent.cs`:
- Removed unused `_synchronizer` field
- Removed full `InitializeAsync()` implementation (replaced with stub to satisfy interface)
- Removed `WaitForOpponentSetupAsync()` (not used)
- Removed duplicate `EncryptWordPlacements()` method

### 2. Error Handling Polish

**StatusType.Error enum** (GameplayScreenManager.cs):
- Added `Error` status type for displaying error messages in red
- Added `.status-error` CSS styling in `Gameplay.uss`

**Retry Logic** (UIFlowController.cs):
- Added 2-attempt retry with 500ms delay for save failures
- User-visible error messages on persistent failures
- Graceful degradation instead of silent failures

### 3. Word Placement Encryption

Changed from trivial Base64 to XOR cipher with salt:
- Salt: `"DLYH2026TecVooDoo"` + optional game code
- Updated `EncryptWordPlacements(placements, gameCode)` signature
- Updated `DecryptWordPlacements(encrypted, gameCode)` signature
- Backward compatible - auto-detects legacy Base64 data
- Updated all callers in `UIFlowController.cs` to pass game code

### 4. Namespace Cleanup

Standardized to `DLYH.*` namespace pattern:
- `TecVooDoo.DontLoseYourHead.Core` → `DLYH.Core.GameState`
- `TecVooDoo.DontLoseYourHead.UI` → `DLYH.Core.GameState` or `DLYH.UI.Services`
- `TecVooDoo.DontLoseYourHead.Editor` → `DLYH.Editor`

Files updated:
- `WordPlacementData.cs`, `DifficultyEnums.cs`, `DifficultyCalculator.cs`
- `WordListSO.cs`, `DifficultySO.cs`, `GameplayStateTracker.cs`
- `WordValidationService.cs`, `WordBankImporter.cs`

### Files Modified

| File | Changes |
|------|---------|
| `GameplayScreenManager.cs` | Added StatusType.Error enum |
| `Gameplay.uss` | Added .status-error CSS |
| `UIFlowController.cs` | Retry logic, encryption calls updated |
| `GameStateManager.cs` | XOR encryption with salt |
| `RemotePlayerOpponent.cs` | Cleaned dead code |
| 8 namespace files | Standardized namespaces |

### Files Deleted

- `Scripts/Networking/NetworkGameManager.cs`
- `Scripts/Networking/WaitingRoomController.cs`
- `Scripts/Networking/GameStateSynchronizer.cs`
- `Scripts/Networking/RematchService.cs`

### Test Plan

- [ ] Build and verify no compilation errors
- [ ] Test AI game (LocalAIOpponent path)
- [ ] Test multiplayer game (RemotePlayerOpponent path)
- [ ] Verify word placements encrypt/decrypt correctly
- [ ] Verify error messages display in red on network failures

---

## Previous Session (Jan 28, 2026)

Session 86 - **Build 6b + Session 6: Activity Tracking & Auto-Win**

### Build 6b Summary ✅ VERIFIED

Fixed matchmaking (Find Opponent) not loading opponent setup data. Both players save setup at roughly the same time, causing a race condition where the joiner tried to fetch before the host had saved.

### Session 6: Activity Tracking & Auto-Win ✅ COMPLETE

Implemented inactivity tracking and automatic forfeit system:
- Activity timestamps already existed (`lastActivityAt`)
- Created Supabase edge function `check-inactivity` for 5-day auto-forfeit
- Deployed edge function and set up daily cron job via pg_cron
- Client-side inactivity check on resume (`HandleResumeGameAsync`)
- Version guarding in `SaveGameStateToSupabaseAsync`

**Test Results:**
- ✅ Edge function deployed and tested (200 response)
- ✅ pg_cron scheduled for daily execution
- ✅ Matchmaking and private games working

---

## Next Session Priorities

### Next Session - Build and Test

1. **Build Unity project** and verify no compilation errors
2. **Test AI game** to verify LocalAIOpponent path works
3. **Test multiplayer game** to verify RemotePlayerOpponent path works
4. **Verify encryption** - word placements encrypt/decrypt correctly

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
| 5 | Turn Synchronization | COMPLETE |
| 6 | Activity Tracking & Auto-Win | COMPLETE |
| 7 | Rematch UI Integration | DEFERRED |
| 8 | Code Quality & Polish | **COMPLETE** |

### Sessions 3-6 Summary (COMPLETE)

- **Session 3:** Game state persistence - attack/defense cards restore correctly on resume
- **Session 4:** Opponent join detection - polling, UI updates, waiting state handling
- **Session 5:** Turn synchronization - polling-based turn detection, RemotePlayerOpponent wired
- **Session 6:** Activity tracking & auto-win - edge function deployed, cron scheduled

### Session 7 - Rematch UI Integration (DEFERRED)

Deferred indefinitely. Players can start new games from main menu. Rematch flow may never be needed.

### Session 8 - Code Quality & Polish (COMPLETE)

- [x] Dead code removal (~1,800+ lines)
- [x] Error handling polish (StatusType.Error, retry logic)
- [x] Word placement encryption (XOR cipher with salt)
- [x] Namespace cleanup (standardized to DLYH.*)

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
- Minor polish deferred to Steam_UI_Polish.md and Mobile_UI_Polish.md (future)

**Networking:** (See `DLYH_NetworkingPlan_Phase_E_Updated.md` for full plan)
- **Find Opponent games fail to resume** - Both players get errors (needs investigation)
- WebGL realtime incomplete (WebSocket bridge missing) - using POLLING instead (Session 5)

**Audio:**
- Music crossfading/switching too frequently (should only switch at end of track)

**Resolved Issues (Session 87):** 20+ networking/architecture issues resolved in Sessions 4-8, archived to `DLYH_Status_Archive.md`

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
| `DLYH.Core.GameState` | Game state, difficulty, word data |
| `DLYH.AI.Core` | AI controllers |
| `DLYH.AI.Strategies` | AI guess strategies |
| `DLYH.Audio` | Audio managers |
| `DLYH.TableUI` | UI Toolkit implementation |
| `DLYH.UI.Managers` | Extracted UI managers (Phase 3) |
| `DLYH.UI.Services` | Word validation service |
| `DLYH.Core.Utilities` | Shared utilities (JsonParsingUtility) |
| `DLYH.Networking` | Opponent abstraction |
| `DLYH.Networking.UI` | Networking overlays |
| `DLYH.Networking.Services` | Supabase, auth, player, realtime |
| `DLYH.Editor` | Editor-only scripts |

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
| 109 | Jan 28, 2026 | Session 87 - Session 8: Code quality & polish (dead code, encryption, namespaces) |
| 108 | Jan 28, 2026 | Session 86 - Session 6: Activity tracking, auto-win, version guarding |
| 107 | Jan 28, 2026 | Session 86 - Build 6b: Matchmaking fix verified |
| 106 | Jan 28, 2026 | Session 86 - Build 6: Matchmaking opponent data loading fix |
| 105 | Jan 27, 2026 | Session 85 - Build 5d: Miss limit calculation using opponent difficulty |
| 104 | Jan 27, 2026 | Session 85 - Build 5c: Turn switching + word row sorting |

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

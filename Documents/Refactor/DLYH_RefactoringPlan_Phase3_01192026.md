# Don't Lose Your Head - Architecture & Refactoring Analysis (Phase 3)

**Version:** 1.5
**Date Created:** January 19, 2026
**Last Updated:** January 19, 2026 (Memory Audit Complete)
**Status:** COMPLETE (Sessions 1-4 + Memory Audit)
**Purpose:** Fresh architecture analysis with focus on redundancies, SOLID principles, and code organization
**Philosophy:** Refactor only when it improves maintainability; prioritize working code over perfect code

---

## Session Progress

| Session | Task | Status | Date |
|---------|------|--------|------|
| 1 | JSON Utility Extraction | COMPLETE | Jan 19, 2026 |
| 2 | GameStateManager Extraction | COMPLETE | Jan 19, 2026 |
| 3 | ModalManager Extraction | COMPLETE | Jan 19, 2026 |
| 4 | ActiveGamesManager Extraction | COMPLETE | Jan 19, 2026 |
| 5 | TurnCoordinator Extraction | SKIPPED | Jan 19, 2026 |
| 6 | Professional Memory Audit | COMPLETE | Jan 19, 2026 |

### Session 1 Results (Complete)

**Created:**
- `Assets/DLYH/Scripts/Core/Utilities/JsonParsingUtility.cs` (274 lines)
  - ExtractStringField(), ExtractIntField(), ExtractBoolField()
  - ExtractObjectField(), ExtractStringArray(), ExtractIntArray()
  - ExtractCoordinateArray() - returns (int row, int col)[] tuples

**Updated:**
- UIFlowController.cs - Added using, replaced calls, removed private methods (~56 lines saved)
- GameSessionService.cs - Added using, replaced calls, removed private methods (~29 lines saved)
- GameStateSynchronizer.cs - Added using, replaced calls, added ConvertToCoordinatePairs(), removed methods (~150 lines saved)
- PlayerService.cs - Added using, replaced calls, removed private methods (~59 lines saved)

**Line Counts After Session 1:**
- UIFlowController.cs: 6,023 lines (was 6,079)
- GameSessionService.cs: 1,017 lines (was ~1,046)
- GameStateSynchronizer.cs: 660 lines (was ~810)
- PlayerService.cs: 325 lines (was ~384)
- JsonParsingUtility.cs: 274 lines (new)

**Verification:** Unity compiled with no errors.

### Session 2 Results (Complete)

**Created:**
- `Assets/DLYH/Scripts/UI/Managers/GameStateManager.cs` (320 lines)
  - ParseGameStateJson() - parses full game state JSON from Supabase
  - ParsePlayerData() - parses player object from JSON
  - ParseGameplayState() - parses gameplay state from player JSON
  - EncryptWordPlacements() - Base64 encodes word placements for storage
  - DecryptWordPlacements() - decodes word placements from storage
  - CalculateMissLimit() - calculates miss limit from grid/word/difficulty (2 overloads)

**Updated:**
- UIFlowController.cs - Added using, replaced 6 calls with GameStateManager.* static calls, removed 6 private methods (~228 lines saved)

**Line Counts After Session 2:**
- UIFlowController.cs: 5,795 lines (was 6,023)
- GameStateManager.cs: 320 lines (new)

**Verification:** Unity compiled with no errors.

### Session 3 Results (Complete)

**Created:**
- `Assets/DLYH/Scripts/UI/Managers/ConfirmationModalManager.cs` (191 lines)
  - Initialize() - sets up with root VisualElement
  - Show(title, message, onConfirm) - displays confirmation dialog
  - Hide() - hides the modal
  - IsVisible property

- `Assets/DLYH/Scripts/UI/Managers/HelpModalManager.cs` (233 lines)
  - Initialize() - sets up with root VisualElement
  - Show() - displays How to Play help modal
  - Hide() - hides the modal
  - IsVisible property
  - Contains HELP_CONTENT constant with game instructions

**Updated:**
- UIFlowController.cs - Added manager fields, initialization, replaced 3 Show/Hide calls, removed 287 lines of modal code

**Line Counts After Session 3:**
- UIFlowController.cs: 5,509 lines (was 5,795)
- ConfirmationModalManager.cs: 191 lines (new)
- HelpModalManager.cs: 233 lines (new)

**Note:** Hamburger menu was NOT extracted - too tightly coupled with settings persistence and callbacks. Would require significant refactoring to decouple.

**Verification:** Unity compiled with no errors.

### Session 4 Results (Complete)

**Created:**
- `Assets/DLYH/Scripts/UI/Managers/ActiveGamesManager.cs` (358 lines)
  - Initialize() - sets up with main menu screen and services
  - UpdateServices() - updates service references after init
  - LoadMyActiveGamesAsync() - loads and displays active games from Supabase
  - HideMyActiveGames() - hides the section
  - RemoveFromHiddenGames() - unhides a game (public for UIFlowController)
  - IsGameHidden() - checks if game is hidden (public)
  - OnResumeRequested callback - delegates resume to UIFlowController
  - OnGameRemoved callback - notifies when game is hidden
  - Contains hidden games persistence (PREFS_HIDDEN_GAMES)

**Updated:**
- UIFlowController.cs - Added _activeGamesManager field, replaced SetupMyActiveGames with manager init, replaced LoadMyActiveGamesAsync calls, replaced RemoveFromHiddenGames call, removed ~211 lines

**Line Counts After Session 4:**
- UIFlowController.cs: 5,298 lines (was 5,509)
- ActiveGamesManager.cs: 358 lines (new)

**Note:** HandleResumeGameFromActiveGames and related async methods remain in UIFlowController since they need to access game state and flow control. ActiveGamesManager uses callback pattern to delegate these actions.

**Verification:** Unity compiled with no errors.

### Session 5: TurnCoordinator (Skipped)

**Decision:** After analysis, TurnCoordinator extraction was skipped because:
- Turn handling methods deeply coordinate 9+ dependencies (services, managers, UI components)
- Extraction would move coupling, not reduce it
- The coordination IS UIFlowController's job - it's cohesive, not coupled
- No practical benefit to extraction

### Session 6: Professional Memory Audit (Complete)

**Approach:** Audited entire codebase as a memory efficiency expert, focusing on:
- Per-frame/per-guess allocations in hot paths
- Unnecessary object creation patterns
- Real coupling issues vs. acceptable coordination
- Over-decoupling (unnecessary abstractions)

**Memory Fixes Implemented:**

1. **Array.Empty<T>() - 21 instances across 4 files**
   - JsonParsingUtility.cs (11 instances)
   - GameSessionService.cs (5 instances)
   - GameStateSynchronizer.cs (6 instances - merged during edit)
   - UIFlowController.cs (1 instance)
   - Impact: Zero allocation on error/empty paths

2. **Ring Buffer in DifficultyAdapter.cs**
   - Changed: `Queue<bool>.ToArray()` called per guess
   - To: Pre-allocated `bool[]` ring buffer with manual indexing
   - Impact: **CRITICAL** - eliminated per-guess allocation

3. **Pooled List in GameplayGuessManager.cs**
   - Added: `_hitPositionsPool` field (reusable List<Vector2Int>)
   - Changed: `new List<Vector2Int>()` per letter guess to reused pool
   - Impact: **CRITICAL** - eliminated per-letter-guess allocation

4. **Cached Snapshots in WordRowsContainer.cs**
   - Added: `_preGuessSnapshot[]`, `_postGuessSnapshot[]`, `_newlyCompletedCache`
   - Added: `CapturePreGuessSnapshot()` method
   - Changed: `GetRevealedSnapshot()` and `GetNewlyCompletedWords()` to use cached arrays
   - Updated: UIFlowController callers to use new zero-allocation pattern
   - Impact: **HIGH** - eliminated per-guess array allocations

**Files Modified:**
- `Core/Utilities/JsonParsingUtility.cs` - Array.Empty<T>()
- `Networking/Services/GameSessionService.cs` - Array.Empty<T>()
- `Networking/Services/GameStateSynchronizer.cs` - Array.Empty<T>()
- `UI/UIFlowController.cs` - Array.Empty<T>() + snapshot API changes
- `AI/Core/DifficultyAdapter.cs` - Ring buffer refactor
- `UI/GameplayGuessManager.cs` - Pooled list
- `UI/WordRowsContainer.cs` - Cached snapshots

**Audit Findings (Not Implemented - Lower Priority):**

1. **LocalAIOpponent reflection hack** (15 min fix)
   - Uses reflection to set private field on ExecutionerAI
   - Fix: Add public config property to ExecutionerAI

2. **UIAudioManager static calls** (1-2 hrs)
   - 48 references across codebase, hard to test/mock
   - Fix: Extract IAudioProvider interface, inject via constructor

3. **Unused OpponentFactory.CreateAIOpponent()** (5 min)
   - Method defined but never called
   - Fix: Remove or use consistently

**Verification:** Unity compiled with no errors.

---

## Final Results

### UIFlowController Line Count Progression

| Session | Lines | Change |
|---------|-------|--------|
| Start | 6,079 | - |
| Session 1 | 6,023 | -56 |
| Session 2 | 5,795 | -228 |
| Session 3 | 5,509 | -286 |
| Session 4 | 5,298 | -211 |
| Final | ~5,298 | -781 total (-13%) |

### New Files Created

| File | Lines | Purpose |
|------|-------|---------|
| JsonParsingUtility.cs | 274 | Unified JSON field extraction |
| GameStateManager.cs | 320 | Game state parsing, encryption, miss limit |
| ConfirmationModalManager.cs | 191 | Confirmation dialog modal |
| HelpModalManager.cs | 233 | How to Play modal |
| ActiveGamesManager.cs | 358 | My Active Games list UI |

### Memory Optimization Impact

**Before:** Every player guess caused:
- 1 bool[] allocation (snapshot)
- 1 List<int> allocation (newly completed)
- 1 List<Vector2Int> allocation (hit positions)
- 1 bool[] allocation (Queue.ToArray in AI)

**After:** Zero allocations per guess in gameplay loop.

---

## Refactoring Philosophy

**THE GOLDEN RULE: Don't refactor for the sake of refactoring. Every change needs a reason.**

### Priority Order (When Making Decisions)

1. **Memory Efficiency** - Nothing kills fun like lag. No per-frame allocations, no unnecessary object creation.
2. **SOLID Principles** - Single responsibility, open/closed, Liskov substitution, interface segregation, dependency inversion.
3. **Self-Documenting Code** - If code is written well, it shouldn't need comments. Clear naming > comments.
4. **Clean & Maintainable** - Easy to read, easy to modify, consistent patterns.
5. **Reusability** (lowest priority) - Nice if code can be reused, but never at the expense of current functionality.

### Line Count Guidelines

- **< 500 lines** - Leave alone unless clear SRP violation
- **500-1000 lines** - Review for extraction opportunities
- **1000-2000 lines** - Strong candidate for refactoring
- **> 2000 lines** - CRITICAL - must evaluate and likely refactor

### When TO Refactor

- A file violates single responsibility (doing multiple unrelated things)
- The same code is literally copy-pasted in multiple places (DRY violation)
- A file is so large that it's difficult to navigate or understand
- Code violates LSP (substitution doesn't work as expected)

### When NOT TO Refactor

- A large file is cohesive and handles one concern well
- "Similar" code serves different purposes
- Extraction would create unnecessary indirection without benefit
- The refactoring would hurt readability or performance

---

## Executive Summary

### Codebase Metrics (Current State - January 19, 2026)

| Metric | Value |
|--------|-------|
| Total C# Files | 69 scripts (+1 JsonParsingUtility) |
| Total Lines | ~33,000 |
| Namespaces | 16 (added DLYH.Core.Utilities) |
| Largest File | UIFlowController.cs (6,023 lines) |
| Files > 1000 lines | 4 |
| Files > 500 lines | 13 |

### Critical Issues Summary

| Issue | Severity | SOLID Principle | Status |
|-------|----------|-----------------|--------|
| UIFlowController.cs 6,023 lines | CRITICAL | SRP | Session 2-5 planned |
| JSON Parsing Duplication | ~~HIGH~~ | ~~DRY~~ | FIXED (Session 1) |
| IOpponent LSP Violation | MEDIUM | LSP | Deferred |
| Namespace Inconsistency | MEDIUM | - | Deferred |

---

## Complete Architecture Overview

### Namespace Structure

```
DLYH.AI.Config/           - AI configuration ScriptableObjects
DLYH.AI.Core/             - AI controllers and managers
DLYH.AI.Data/             - AI data structures and utilities
DLYH.AI.Strategies/       - AI guess strategy implementations
DLYH.Audio/               - Audio managers and settings
DLYH.Core.Utilities/      - NEW: Shared utilities (JsonParsingUtility)
DLYH.Editor/              - Custom editor tools
DLYH.Networking/          - Opponent abstraction, factories
DLYH.Networking.Services/ - Supabase services (Auth, Player, Session, etc.)
DLYH.Networking.UI/       - Networking overlays and UI
DLYH.TableUI/             - Grid/table UI components
DLYH.Telemetry/           - Analytics and telemetry
DLYH.UI/                  - Main UI scripts

TecVooDoo.DontLoseYourHead.Core/   - Legacy: DifficultySetting enum
TecVooDoo.DontLoseYourHead.UI/     - Legacy: GameplayStateTracker, etc.
TecVooDoo.DontLoseYourHead.Editor/ - Legacy: WordBankImporter
```

### Class Inventory by Folder

#### Core/Utilities/ (NEW - 1 file, 274 lines)

| File | Lines | Namespace | Responsibility |
|------|-------|-----------|----------------|
| JsonParsingUtility.cs | 274 | DLYH.Core.Utilities | Unified JSON field extraction |

#### AI/ (11 files, ~2,800 lines)

| File | Lines | Namespace | Responsibility |
|------|-------|-----------|----------------|
| ExecutionerConfigSO.cs | 411 | DLYH.AI.Config | AI tunable parameters |
| AISetupManager.cs | 542 | DLYH.AI.Core | AI word selection/placement |
| DifficultyAdapter.cs | 344 | DLYH.AI.Core | Runtime difficulty with rubber-banding |
| ExecutionerAI.cs | 492 | DLYH.AI.Core | Main AI controller |
| MemoryManager.cs | 303 | DLYH.AI.Core | Skill-based memory filtering |
| GridAnalyzer.cs | 441 | DLYH.AI.Data | Static grid analysis utilities |
| LetterFrequency.cs | 211 | DLYH.AI.Data | English letter frequency data |
| IGuessStrategy.cs | 382 | DLYH.AI.Strategies | Interface + supporting structs |
| LetterGuessStrategy.cs | 326 | DLYH.AI.Strategies | Letter guessing strategy |
| CoordinateGuessStrategy.cs | 249 | DLYH.AI.Strategies | Coordinate guessing strategy |
| WordGuessStrategy.cs | 338 | DLYH.AI.Strategies | Word guessing strategy |

**Status:** Well-structured, follows strategy pattern correctly. No refactoring needed.

#### Audio/ (6 files, ~1,600 lines)

| File | Lines | Namespace | Responsibility |
|------|-------|-----------|----------------|
| GuillotineAudioManager.cs | 327 | DLYH.Audio | Guillotine SFX (Singleton) |
| MusicManager.cs | 669 | DLYH.Audio | Background music (Singleton) |
| UIAudioManager.cs | 415 | DLYH.Audio | UI sound effects (Singleton) |
| UIButtonAudio.cs | 77 | DLYH.Audio | Button interaction audio |
| SFXClipGroup.cs | 73 | DLYH.Audio | Audio clip container |
| AudioSettings.cs | 58 | DLYH.Audio | Audio settings data |

**Status:** Well-structured Singleton pattern. No refactoring needed.

#### Networking/ (15 files, ~5,200 lines after Session 1)

| File | Lines | Namespace | Responsibility |
|------|-------|-----------|----------------|
| IOpponent.cs | 177 | DLYH.Networking | Opponent interface |
| LocalAIOpponent.cs | 394 | DLYH.Networking | Wraps AI as IOpponent |
| RemotePlayerOpponent.cs | 556 | DLYH.Networking | Wraps remote player as IOpponent |
| OpponentFactory.cs | 62 | DLYH.Networking | Creates opponent instances |
| NetworkGameManager.cs | 435 | DLYH.Networking | Game session coordinator |
| AuthService.cs | 739 | DLYH.Networking.Services | Supabase authentication |
| AuthCallbackHandler.cs | 255 | DLYH.Networking.Services | Auth callback handling |
| SupabaseClient.cs | 305 | DLYH.Networking.Services | HTTP client wrapper |
| SupabaseConfig.cs | 106 | DLYH.Networking.Services | Configuration ScriptableObject |
| GameSessionService.cs | 1,017 | DLYH.Networking.Services | Session CRUD operations |
| GameStateSynchronizer.cs | 660 | DLYH.Networking.Services | State sync between clients |
| GameSubscription.cs | 326 | DLYH.Networking.Services | Real-time subscriptions |
| MatchmakingService.cs | 590 | DLYH.Networking.Services | Opponent matching |
| PlayerService.cs | 325 | DLYH.Networking.Services | Player profile management |
| RealtimeClient.cs | 482 | DLYH.Networking.Services | WebSocket handling |

**Status:** JSON parsing duplication FIXED (Session 1). IOpponent LSP violation deferred.

#### UI/ (23 files, ~12,600 lines after Session 1)

| File | Lines | Namespace | Responsibility |
|------|-------|-----------|----------------|
| **UIFlowController.cs** | **6,023** | DLYH.UI | **CRITICAL: 12+ concerns mixed** |
| GameplayScreenManager.cs | 1,390 | DLYH.UI | Gameplay screen UI |
| SetupWizardController.cs | 943 | DLYH.UI | Setup wizard logic |
| SetupWizardUIManager.cs | 897 | DLYH.UI | Setup wizard UI |
| GameplayGuessManager.cs | 841 | DLYH.UI | Guess input handling |
| WordRowView.cs | 1,040 | DLYH.UI | Word row display |
| PlacementAdapter.cs | 801 | DLYH.UI | Word placement to UI |
| GuillotineOverlayManager.cs | 760 | DLYH.UI | Guillotine animation |
| WordRowsContainer.cs | 713 | DLYH.UI | Word rows container |
| TableView.cs | 511 | DLYH.UI | Grid table component |
| WordSuggestionDropdown.cs | 310 | DLYH.UI | Autocomplete dropdown |
| TableModel.cs | 286 | DLYH.UI | Grid data model |
| ColorRules.cs | 246 | DLYH.UI | Color scheme rules |
| NetworkingUIManager.cs | 740 | DLYH.Networking.UI | Networking overlays |
| + 9 smaller files | ~800 | Various | Supporting components |

---

## Remaining Extractions

### Extraction 2: GameStateManager (Priority: HIGH) - Session 2

**Create:** `Assets/DLYH/Scripts/UI/Managers/GameStateManager.cs`

**Extract from UIFlowController.cs:**
- ParseGameStateJson(), ParsePlayerData(), ParseGameplayState() (lines ~839-986)
- EncryptWordPlacements(), DecryptWordPlacements() (lines ~1093-1168)
- CalculateMissLimit() (lines ~1185-1215)

**Estimated size:** ~400 lines

**Effort:** 4 hours | **Risk:** MEDIUM | **Impact:** HIGH

---

### Extraction 3: ModalManager (Priority: MEDIUM) - Session 3

**Create:** `Assets/DLYH/Scripts/UI/Managers/ModalManager.cs`

**Extract from UIFlowController.cs:**
- CreateConfirmationModal(), ShowConfirmationModal(), HideConfirmationModal()
- CreateHelpModal(), ShowHelpModal(), HideHelpModal()
- CreateHamburgerMenu(), ShowHamburgerOverlay(), HideHamburgerOverlay()

**Estimated size:** ~400 lines

**Effort:** 3 hours | **Risk:** LOW | **Impact:** MEDIUM

---

### Extraction 4: ActiveGamesManager (Priority: MEDIUM) - Session 4

**Create:** `Assets/DLYH/Scripts/UI/Managers/ActiveGamesManager.cs`

**Extract from UIFlowController.cs:**
- SetupMyActiveGames()
- LoadMyActiveGamesAsync()
- ShowMyActiveGames(), HideMyActiveGames(), CreateMyGameItem()
- HandleResumeGame(), ResumeGameAfterConfirmation(), HandleResumeGameAsync()

**Estimated size:** ~300 lines

**Effort:** 3 hours | **Risk:** MEDIUM | **Impact:** MEDIUM

---

### Extraction 5: TurnCoordinator (Priority: MEDIUM) - Session 5

**Create:** `Assets/DLYH/Scripts/UI/Managers/TurnCoordinator.cs`

**Extract from UIFlowController.cs:**
- TriggerOpponentTurn()
- BuildOpponentGameState()
- HandleOpponentLetterGuess(), HandleOpponentCoordinateGuess(), HandleOpponentWordGuess()

**Estimated size:** ~500 lines

**Effort:** 4 hours | **Risk:** MEDIUM | **Impact:** HIGH

---

## NOT Recommended Changes

1. **Do NOT refactor AI Strategy classes** - Well-structured, working correctly
2. **Do NOT consolidate namespaces yet** - Low ROI, high churn
3. **Do NOT add service interfaces for DIP yet** - UIFlowController needs SRP first
4. **Do NOT refactor TableView.cs** - At threshold but cohesive
5. **Do NOT extract AudioManagerBase** - Pattern is consistent, low value

---

## Verification Checklist (After Each Session)

### Compile Check
- [ ] No compile errors
- [ ] No new warnings

### Functionality Check
- [ ] Game launches
- [ ] Main menu displays
- [ ] Solo game: start, play, win/lose
- [ ] Settings: SFX, music, keyboard layout persist
- [ ] Help modal opens/closes
- [ ] Confirmation dialogs work

### Networking Check (if applicable)
- [ ] Create private game
- [ ] Join game with code
- [ ] My Active Games loads
- [ ] Resume game works
- [ ] Game state syncs

---

## Post-Refactoring Expected Results (After All Sessions)

| Metric | Before | After | Change |
|--------|--------|-------|--------|
| UIFlowController.cs lines | 6,023 | ~3,400 | -44% |
| Duplicated JSON methods | ~~4 files~~ | 0 files | DONE |
| Files > 2000 lines | 1 | 0 | -100% |
| New manager classes | 1 | 5 | +4 more |

### Target File Structure
```
Assets/DLYH/Scripts/
  Core/
    Utilities/
      JsonParsingUtility.cs (274 lines) - DONE
  UI/
    Managers/
      GameStateManager.cs (~400 lines) - Session 2
      ModalManager.cs (~400 lines) - Session 3
      ActiveGamesManager.cs (~300 lines) - Session 4
      TurnCoordinator.cs (~500 lines) - Session 5
    UIFlowController.cs (~3,400 lines) - orchestration only
```

---

## Code Style Requirements

- No `var` usage - explicit types always
- No GetComponent in hot paths
- No allocations in Update
- Private fields use `_camelCase`
- Methods under 20 lines (initialization excepted)
- Events named `On` + PastTense (e.g., `OnWordValidated`)
- Prefer UniTask over coroutines
- Prefer async over coroutines
- ASCII-only in documentation

---

**End of Architecture & Refactoring Analysis**

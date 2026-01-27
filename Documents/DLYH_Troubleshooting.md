# DLYH Troubleshooting (Active)

## Document Purpose

This document defines a repeatable troubleshooting method used to diagnose, track, and resolve complex issues across the DLYH project.

The method is **process-first**, not domain-specific. While the current issue focus may be UI/Layout, this same structure is intended to be reused for:
- UI / Layout
- Audio
- Input
- Networking
- Performance
- Build / Platform-specific issues

Each troubleshooting effort should follow the same lifecycle: observation → hypothesis → intervention → validation → classification.

---

## Troubleshooting Principles

- Evidence over assumption
- Measure real runtime state, not editor expectations
- One variable change per iteration
- Fix root causes, not symptoms
- Explicitly document constraints and tradeoffs

---

## Issue Classification

All discovered issues must be classified into one of the following categories:

- **Engineering Bug** – Incorrect logic, layout, configuration, or implementation
- **System Constraint** – Engine, browser, platform, or hardware limitation
- **Design / UX Decision** – Tradeoffs that require intentional acceptance
- **Unknown** – Requires additional instrumentation or telemetry

Classification determines whether an issue is fixed, deferred, accepted, or redesigned.

---

## Current Investigation

**Issue: Real-time multiplayer turn synchronization not working**

- **Domain:** Networking
- **Session:** 84 (continued)
- **Date opened:** January 26, 2026
- **Status:** Build 4 tested - fundamental data loading issue identified

---

### Problem Statement

Two players can join a private game (PC host + mobile guest), and the turn indicator shows correctly (one sees "YOUR TURN", the other sees "OPPONENT'S TURN"). However, when a player makes a move, the other player does NOT see the move in real-time. The game state only updates after manually reloading the browser and resuming the game.

---

### Test Configuration

- **Host:** PC (Chrome browser on tecvoodoo.com)
- **Guest:** Mobile (Samsung S25 Ultra, Chrome browser)
- **Game mode:** Private game with code
- **Expected behavior:** When mobile makes a move, PC should see the cell/letter revealed within 2 seconds (polling interval)

---

### Observations

**Build 1 (Initial Implementation):**
- Both players showed "Your Turn" simultaneously
- Moves did not sync

**Build 2 (Turn indicator fix):**
- Turn indicator now correct (one shows YOUR TURN, other shows OPPONENT'S TURN)
- Moves still do not sync in real-time
- Moves only visible after browser reload + game resume

**Build 3 (Pending test):**
- Fixed `DetectOpponentAction()` to use `revealedCells` instead of `guessedCoordinates`
- Added debug logging throughout polling mechanism

---

### Hypotheses

| # | Hypothesis | Status | Evidence |
|---|------------|--------|----------|
| H1 | Both players default to `_isPlayerTurn = true` | CONFIRMED/FIXED | Build 1 showed both players as "Your Turn". Fixed by fetching `currentTurn` from Supabase. |
| H2 | JoinGame mode doesn't create `RemotePlayerOpponent` | CONFIRMED/FIXED | Added `CreateRemotePlayerOpponentAsync()` call to JoinGame branch. |
| H3 | `DetectOpponentAction()` checks wrong field | CONFIRMED/FIXED | Was checking `guessedCoordinates` which is never populated. `SaveGameStateToSupabaseAsync()` only populates `revealedCells`. |
| H4 | Polling loop not starting | UNCONFIRMED | Added debug logging to verify. |
| H5 | Polling loop exits immediately | UNCONFIRMED | Loop conditions check `_pollingForOpponentTurn`, `_hasActiveGame`, `_isGameOver`, `_isPlayerTurn`. |
| H6 | `ProcessStateUpdate()` not being called | UNCONFIRMED | Added logging to verify. |
| H7 | `_lastOpponentGameplayState` baseline incorrect | UNCONFIRMED | Added logging to `SetInitialState()` to verify baseline values. |
| H8 | Events fire but handlers fail | UNCONFIRMED | Handlers are wired same as AI opponent which works. |

---

### Code Changes (Build 3)

**File: `RemotePlayerOpponent.cs`**

1. **`DetectOpponentAction()` (line ~351)** - Changed from `guessedCoordinates` to `revealedCells`:
```csharp
// OLD (broken):
int lastCoordCount = _lastOpponentGameplayState?.guessedCoordinates?.Length ?? 0;
if (newState.guessedCoordinates != null && newState.guessedCoordinates.Length > lastCoordCount)

// NEW (fixed):
int lastRevealedCount = _lastOpponentGameplayState?.revealedCells?.Length ?? 0;
if (newState.revealedCells != null && newState.revealedCells.Length > lastRevealedCount)
```

2. **`SetInitialState()` (line ~189)** - Added debug logging:
```csharp
int revealedCount = _lastOpponentGameplayState?.revealedCells?.Length ?? 0;
int letterCount = _lastOpponentGameplayState?.knownLetters?.Length ?? 0;
Debug.Log($"[RemotePlayerOpponent] Initial state set - isHost={_isLocalPlayerHost}, turnNumber={initialState?.turnNumber ?? -1}, opponentRevealed={revealedCount}, opponentLetters={letterCount}");
```

3. **`DetectOpponentAction()` (line ~358)** - Added comparison logging:
```csharp
Debug.Log($"[RemotePlayerOpponent] DetectOpponentAction - lastRevealed={lastRevealedCount}, newRevealed={newState.revealedCells?.Length ?? 0}, lastLetters={lastLetterCount}, newLetters={newState.knownLetters?.Length ?? 0}");
```

**File: `UIFlowController.cs`**

4. **`TurnDetectionPollingAsync()` (line ~7025)** - Added fetch logging:
```csharp
Debug.Log($"[UIFlowController] Polling turn detection - fetching state for {gameCode}");
```

5. **`TurnDetectionPollingAsync()` (line ~7041)** - Added result logging:
```csharp
Debug.Log($"[UIFlowController] Poll result - turnNumber: {gameState.turnNumber}, lastKnown: {_lastKnownTurnNumber}, currentTurn: {gameState.currentTurn}");
```

6. **`TurnDetectionPollingAsync()` (line ~7050)** - Added processing logging:
```csharp
Debug.Log($"[UIFlowController] Processing state update via RemotePlayerOpponent");
// ... or if _opponent is wrong type:
Debug.LogWarning($"[UIFlowController] Cannot process state update - _opponent is not RemotePlayerOpponent (is {_opponent?.GetType().Name ?? "null"})");
```

---

### Key Data Flow (Expected)

1. **Player A makes move** -> `SaveGameStateToSupabaseAsync()` saves state with:
   - `player1.gameplayState.revealedCells[]` (cells A revealed on B's defense)
   - `turnNumber` incremented
   - `currentTurn` switched to opponent

2. **Player B polling detects change** -> `TurnDetectionPollingAsync()`:
   - Fetches game state from Supabase
   - Compares `gameState.turnNumber > _lastKnownTurnNumber`
   - Calls `remoteOpponent.ProcessStateUpdate(gameState)`

3. **RemotePlayerOpponent processes state** -> `ProcessStateUpdate()`:
   - Gets opponent's gameplay state (A's `gameplayState`)
   - Calls `DetectOpponentAction()`
   - Compares `revealedCells.Length` to detect new cells
   - Fires `OnCoordinateGuess` event

4. **UIFlowController handles event** -> `HandleOpponentCoordinateGuess()`:
   - Processes guess against player's grid
   - Updates UI (marks cell as hit/miss)
   - Calls `EndOpponentTurn()` if no extra turns

---

### Validation Plan (Build 3)

1. **Check browser console** for these log messages:
   - `[UIFlowController] Turn detection polling started for game XXXXXX` - confirms polling started
   - `[UIFlowController] Polling turn detection - fetching state for XXXXXX` - confirms poll loop running
   - `[UIFlowController] Poll result - turnNumber: X, lastKnown: Y, currentTurn: playerZ` - confirms state fetched
   - `[UIFlowController] Turn number changed: X -> Y` - confirms change detected
   - `[UIFlowController] Processing state update via RemotePlayerOpponent` - confirms processing
   - `[RemotePlayerOpponent] DetectOpponentAction - lastRevealed=X, newRevealed=Y` - confirms detection
   - `[RemotePlayerOpponent] Opponent guessed coordinate: (X, Y)` - confirms event fired

2. **If polling not starting:**
   - Check if `_opponent` is null (wrong code path)
   - Check if `_opponent.IsAI` returns true (wrong opponent type)
   - Check `_pollingForOpponentTurn` flag

3. **If polling starts but no change detected:**
   - Compare `turnNumber` values in logs
   - Verify `_lastKnownTurnNumber` baseline is correct

4. **If change detected but no event fired:**
   - Check `revealedCells` comparison in logs
   - Verify `_lastOpponentGameplayState` baseline has correct counts

---

### Previous Investigation

Last closed: Layout Composition & Flex-Shrink (Sessions 78-82)
- Date closed: January 26, 2026
- Platforms tested: PC + Mobile (Samsung S25 Ultra)
- Outcome: All core issues resolved
- Archive: See `DLYH_Troubleshooting_Archive.md`

Minor platform-specific polish deferred to:
- Steam_UI_Polish.md (future)
- Mobile_UI_Polish.md (future)

---

## Notes

- Remote USB debugging blocked on Samsung S25 Ultra (ADB authorization never appears)
- On-device overlay is the primary mobile diagnostic tool

---

### Clarified Intended Multiplayer Flow (Authoritative)

Private games in DLYH are asynchronous by design.

**The correct and intended behavior is:**

1. **Host creates a private game**
   - Completes their own setup (grid, words, difficulty)
   - Receives a join code
   - Game is persisted in Supabase
   - Host may leave for minutes, hours, or days

2. **Host cannot make moves until an opponent joins and completes setup**
   - Gameplay input is blocked
   - UI should indicate "Waiting for opponent..."

3. **Guest joins later using the code**
   - Completes their own setup independently (grid/words may differ)
   - Once guest setup is complete, the game becomes playable

4. **Only after BOTH setups are complete does gameplay begin**
   - Attack grid uses opponent's grid size and word count
   - Defend grid uses local player's grid size and word count
   - "Words to find" rows must be populated with underscores immediately

This asynchronous model is intentional and mirrors DAB behavior.

---

### New Finding: Gameplay Is Starting Too Early

**Observed behavior (Build 3):**
- Gameplay UI loads immediately after host setup
- Opponent setup data is not yet available

**Resulting symptoms:**
- "Words to find" rows are blank (no underscores)
- Opponent grid size is incorrect or missing
- `_opponent` object is not created
- Turn polling runs without a valid opponent handler

This is not a design issue - it is an implementation order bug.

---

### Root Cause (Engineering Bug)

Gameplay is transitioning before both player setups are complete and before a RemotePlayerOpponent is created and wired.

**Evidence:**
- Console warning during polling: `Cannot process state update - _opponent is not RemotePlayerOpponent (is null)`
- Polling loop runs and detects turnNumber changes
- State updates cannot be applied because `_opponent` does not exist
- Both clients become stuck on "Opponent's turn"

---

### Required Gating Rules (Must Be Enforced)

**Gate A - Opponent Setup Gate**

Gameplay UI must not be constructed until:
- `player1.setupComplete == true`
- `player2.setupComplete == true`
- Client knows whether it is player1 or player2

If this gate fails:
- Show waiting UI
- Do not build grids
- Do not build word rows
- Do not start polling

**Gate B - Opponent Object Gate**

Turn detection polling must not start until:
- `_opponent` is a valid RemotePlayerOpponent
- Initial baseline state has been set
- `_lastKnownTurnNumber` is initialized from Supabase

Polling while `_opponent == null` is invalid and must be prevented.

---

### Why Reload "Fixes" Some UI but Still Breaks Turns

**On reload:**
- Resume path loads full game state from Supabase
- Opponent setup data is now present
- UI builds correctly (grids + underscores appear)

**However:**
- `_opponent` is still not guaranteed to be created before polling
- First turn still locks because state updates cannot be processed

This confirms the issue is ordering, not persistence.

---

### Updated Classification

| Issue | Classification |
|-------|----------------|
| Gameplay UI built without opponent setup | Engineering Bug |
| `_opponent` null during polling | Engineering Bug |
| Async private game flow | Correct by design |
| Mismatched grid sizes (12x12 vs 6x6) | Correct by design |

---

### Resolution Direction (High Level)

1. Treat "Start Game" as enter waiting state, not gameplay
2. Only transition to gameplay after both setups are complete
3. Create and wire RemotePlayerOpponent before starting polling
4. Block polling if `_opponent` is null

---

### Build 4 Fixes (Session 84 continued)

**Files Changed: `UIFlowController.cs`**

**Fix 1: Create RemotePlayerOpponent when opponent joins (HandleOpponentJoined)**
- Location: Line ~7101
- Problem: When host created private game, they transitioned to gameplay before opponent joined. `HandleOpponentJoined()` was called when opponent joined, but it only updated `_matchmakingResult` - it never created the `RemotePlayerOpponent`.
- Fix: Modified `HandleOpponentJoined()` to:
  1. Made method `async void` instead of `void`
  2. Added `await CreateRemotePlayerOpponentAsync()` when opponent setup is complete and `_opponent == null`

**Fix 2: Initialize _lastKnownTurnNumber when creating RemotePlayerOpponent**
- Location: Line ~2936
- Problem: `_lastKnownTurnNumber` was initialized to 0 in `TransitionToGameplay()`, but for hosts waiting for opponent, this initialization was skipped because `isRealMultiplayer` was false (no opponent yet).
- Fix: Added initialization of `_lastKnownTurnNumber` in `CreateRemotePlayerOpponentAsync()` after fetching initial state.

**Fix 3: Update _lastKnownTurnNumber when saving state**
- Location: Line ~1043
- Problem: After player saves their turn to Supabase (incrementing turnNumber), `_lastKnownTurnNumber` was not updated locally. This caused polling to see a larger-than-expected jump (e.g., 0 -> 3 instead of 2 -> 3).
- Fix: Added `_lastKnownTurnNumber = state.turnNumber` in `SaveGameStateToSupabaseAsync()` after successful save.

**Expected Outcome:**
1. Host creates private game -> transitions to gameplay in waiting state
2. Mobile joins -> `HandleOpponentJoined()` creates RemotePlayerOpponent and initializes `_lastKnownTurnNumber`
3. Host makes move -> saves state with turn 2, updates `_lastKnownTurnNumber = 2`
4. Polling starts -> detects only changes from turn 2 onwards
5. Mobile makes move -> polling detects turn 2 -> 3, processes via `RemotePlayerOpponent`
6. Events fire, UI updates correctly

---

### Build 4 Test Results (Session 84)

**Test Configuration:**
- PC (host): 12x12 grid, 4 words
- Mobile (joiner): 6x6 grid, 3 words

**What Worked:**
- `_lastKnownTurnNumber` tracking fixed: Log shows `turnNumber: 3, lastKnown: 2` (was `lastKnown: 0` before)
- `RemotePlayerOpponent` created: Log shows `[RemotePlayerOpponent] Now waiting for opponent's turn`
- Turn detection working: Log shows `Turn number changed: 2 -> 3`
- State processing attempted: Log shows `Processing state update via RemotePlayerOpponent`

**What Failed:**
- **Opponent data never loaded on either client:**
  - PC attack tab showed 12x12 grid (should be 6x6 - Mobile's grid)
  - Mobile attack tab showed 6x6 grid (should be 12x12 - PC's grid)
  - Word rows empty on both (no underscores)
  - Only opponent NAME transferred ("Mobile" / "pc")
  - Grid size, word count, word placements, colors - all missing

- **DetectOpponentAction found no changes:**
  ```
  [RemotePlayerOpponent] DetectOpponentAction - lastRevealed=0, newRevealed=0, lastLetters=0, newLetters=0
  ```
  Reading wrong player data OR opponent data never populated

- **Both clients stuck on "Opponent's Turn":**
  - PC made move → turn to player2 → PC shows "Opponent's Turn" ✓
  - Mobile made move → turn to player1 → Mobile shows "Opponent's Turn" ✓
  - PC polling detected turn change but no events fired (no data to detect)
  - Turn switch never completed

**Key Insight: Resume Path Works, Live Path Doesn't**

When browser reloads and game resumes from Active Games:
- Full game state fetched from Supabase
- Both player setups loaded
- UI built with correct opponent data (grids correct size, word rows have underscores)

When playing live after opponent joins:
- UI already built with placeholder/default values
- `HandleOpponentJoined` stores data in `_matchmakingResult` but doesn't rebuild UI
- Player allowed to make moves on incorrect/incomplete grid

---

### Root Cause Analysis (Updated)

**The fundamental problem is NOT turn synchronization - it's opponent data loading.**

The data exists in Supabase:
- PC saved: 12x12, 4 words, word placements, color
- Mobile saved: 6x6, 3 words, word placements, color

But when opponent joins a live game:
1. `HandleOpponentJoined()` fires
2. Opponent name is displayed
3. `RemotePlayerOpponent` is created (Build 4 fix)
4. **Opponent setup data is NOT fetched from Supabase**
5. **Attack grid is NOT rebuilt with opponent's grid size**
6. **Word rows are NOT populated with opponent's words**
7. Player can make moves on wrong-sized grid with no word data

Turn sync cannot work because there's no opponent data to sync against.

---

### Two Code Paths Comparison

| Aspect | Resume Path (Works) | Live Join Path (Broken) |
|--------|---------------------|------------------------|
| Fetches opponent setup from Supabase | ✓ Yes | ✗ No |
| Builds attack grid with opponent's size | ✓ Yes | ✗ Uses default/local size |
| Populates word rows with underscores | ✓ Yes | ✗ Empty |
| Has opponent word placements | ✓ Yes | ✗ Missing |
| `DetectOpponentAction` has data to compare | ✓ Yes | ✗ All zeros |

---

### Required Fixes (Next Session)

**Priority 1: Block gameplay until opponent data loads**
- If attack grid size is wrong OR word rows are empty → block input
- Show "Loading opponent data..." or "Waiting for opponent setup..."
- Do NOT allow moves on incomplete data

**Priority 2: Fetch and apply opponent data when they join**
- When `HandleOpponentJoined()` fires:
  1. Fetch opponent's full setup from Supabase (grid size, word count, word placements, color)
  2. Rebuild attack grid with correct size
  3. Populate word rows with underscores for opponent's words
  4. Update attack card info (grid size, word count)
- This should use same logic as Resume path

**Priority 3: Fix DetectOpponentAction player data selection**
- Currently reading wrong player's `revealedCells` (local player's data, not opponent's)
- Need to flip player1/player2 based on `_isLocalPlayerHost`
- This matters even after data loads correctly

---

### Updated Classification

| Issue | Classification |
|-------|----------------|
| Opponent setup data not fetched on live join | Engineering Bug |
| UI not rebuilt when opponent joins | Engineering Bug |
| Gameplay allowed on incomplete data | Engineering Bug |
| DetectOpponentAction reads wrong player | Engineering Bug |
| Turn tracking (`_lastKnownTurnNumber`) | FIXED in Build 4 |
| RemotePlayerOpponent creation | FIXED in Build 4 |
| Async private game flow | Correct by design |
| Mismatched grid sizes (12x12 vs 6x6) | Correct by design |

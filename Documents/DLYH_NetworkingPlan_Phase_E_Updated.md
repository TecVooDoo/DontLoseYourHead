# Don't Lose Your Head - Networking Implementation Plan (Phase E) — REVISED (Jan 26, 2026)

**IMPORTANT: Working Directory is `C:\Unity\DontLoseYourHead` — NOT the backup folder.**

This is the **Phase E** networking build plan for **DLYH**, revised after codebase analysis on Jan 26, 2026.

---

## Executive Summary

Goal: Make **DLYH multiplayer** fully playable between two human players.

**Current State (verified Jan 26, 2026):**
- Sessions 1-4 COMPLETE
- State persistence works (`SaveGameStateToSupabaseAsync()` at UIFlowController.cs:925)
- Opponent join detection works (`HandleOpponentJoined()` at UIFlowController.cs:6733)
- Turn detection logic exists in `RemotePlayerOpponent.cs` but is NOT WIRED

**Session 5 Goal:** Wire up turn synchronization so both players see each other's moves.

---

## Key Rules (Do Not Violate)

### Rule 1 — Non-blocking auth UI
Update UI immediately on auth success. Database work happens in background.

### Rule 2 — Auth handoff to Unity must be explicit
When DLYH runs inside the tecvoodoo.com iframe, Unity must obtain and use the same Supabase session identity.

### Rule 3 — Use POLLING for Session 5 (not Realtime)
WebGL Realtime has incomplete WebSocket bridge. Polling is proven (opponent join uses 3-second polling).
**Decision: Use 2-second polling for turn detection.**

### Rule 4 — Keep the proven push path
Use `SaveGameStateToSupabaseAsync()` (UIFlowController.cs:925). Do NOT resurrect unused wrappers:
- `GameStateSynchronizer.PushLocalStateAsync()` — exists but never called
- `NetworkGameManager.PushGameStateAsync()` — exists but never called

### Rule 5 — Load opponent setup BEFORE building attack grid
Order: load setup → cache in context → build attack grid → start turn sync

### Rule 6 — IOpponent abstraction is opponent-agnostic
The game does not care if opponent is LocalAIOpponent (phantom AI) or RemotePlayerOpponent (human).
Both implement `IOpponent`. UIFlowController talks to `_opponent` interface only.

---

## Architecture Reference

### Key Files (C:\Unity\DontLoseYourHead\Assets\DLYH\Scripts)

| File | Purpose | Key Lines |
|------|---------|-----------|
| `UI/UIFlowController.cs` | Main flow, state save, opponent handling | 925 (save), 6733 (opponent joined) |
| `Networking/RemotePlayerOpponent.cs` | Turn detection logic | 350 (DetectOpponentAction) |
| `Networking/UI/NetworkingUIManager.cs` | `NetworkingUIResult` class | 17-26 |
| `Networking/Services/GameSessionService.cs` | Supabase CRUD | Data models |

### Session Context (NetworkingUIResult)

`NetworkingUIResult` already exists and serves as session context:
```csharp
public class NetworkingUIResult
{
    public bool Success;
    public bool Cancelled;
    public string GameCode;
    public bool IsHost;
    public bool IsPhantomAI;
    public string OpponentName;
    public string ErrorMessage;
}
```

**Session 5 extends this** with opponent setup fields rather than creating a new class.

---

## Session Progress

| Session | Focus | Status |
|---------|-------|--------|
| 1 | Foundation & Editor Identity | COMPLETE |
| 2 | Phantom AI as Session Player | COMPLETE |
| 3 | Game State Persistence | COMPLETE |
| 4 | Opponent Join Detection | COMPLETE |
| 5 | Turn Synchronization | COMPLETE |
| 6 | Activity Tracking & Auto-Win | COMPLETE |
| 7 | Rematch UI Integration | **DEFERRED** |
| 8 | Code Quality & Polish | COMPLETE |

---

## Session 5: Turn Synchronization (IMPLEMENTED - Session 84)

### What's Implemented
- [x] `NetworkingUIResult` extended with opponent setup fields
- [x] `HandleOpponentJoined()` loads opponent setup from Supabase
- [x] Attack grid uses opponent's dimensions
- [x] `RemotePlayerOpponent` created with lightweight init (no duplicate services)
- [x] 2-second polling for turn detection
- [x] "Waiting for opponent..." indicator shown during opponent's turn

### What Needs Testing
- End-to-end test with two browser windows
- Letter guess sync
- Coordinate guess sync
- Word guess sync
- Miss counter sync
- Turn indicator sync

---

### Tasks (in order)

#### 5.1 Extend NetworkingUIResult with opponent setup fields

Add to `NetworkingUIResult`:
```csharp
public int OpponentGridSize;
public int OpponentWordCount;
public Color OpponentColor;
public bool OpponentSetupLoaded;
```

This reuses existing session context rather than creating a new class.

#### 5.2 Complete HandleOpponentJoined() — load opponent setup

Modify `HandleOpponentJoined()` (line 6733) to:
1. Fetch opponent's player data from Supabase (grid size, word count, color)
2. Store in `_matchmakingResult` (the NetworkingUIResult)
3. Set `OpponentSetupLoaded = true`

**Note:** For resumed games where opponent already joined, skip join detection and load setup directly.

#### 5.3 Build attack grid using opponent setup

In `TransitionToGameplay()` or equivalent, when building attack grid:
- Use `_matchmakingResult.OpponentGridSize` for dimensions
- Use `_matchmakingResult.OpponentWordCount` for word count
- Opponent word placements remain encrypted until game end

#### 5.4 Create RemotePlayerOpponent for real multiplayer

When `!IsPhantomAI`:
1. Create `RemotePlayerOpponent` instance
2. **Do NOT call full `InitializeAsync()`** — it creates duplicate services
3. Instead, wire only the turn detection polling using existing `_gameSessionService`
4. Subscribe to `OnLetterGuess`, `OnCoordinateGuess`, `OnWordGuess` events

When `IsPhantomAI`:
- Continue using `LocalAIOpponent` (no change needed)

#### 5.5 Implement polling-based turn detection

**Trigger:** When local player's turn ends, start polling for opponent's move.

Polling logic (2-second interval):
1. Fetch game state from Supabase
2. Compare `turnNumber` — if increased, opponent moved
3. Call `DetectOpponentAction()` to determine what changed
4. Fire appropriate event (`OnLetterGuess`, `OnCoordinateGuess`, `OnWordGuess`)
5. Update local state and UI
6. Stop polling when `currentTurn` is back to local player

**Cancellation:** Stop polling on game exit, disconnect, or game end.

#### 5.6 Add "Waiting for opponent..." indicator

During opponent's turn in real multiplayer:
- Show status message "Waiting for opponent..."
- Disable input (already handled by turn system)

When turn returns to local player:
- Clear status message
- Enable input

#### 5.7 End-to-end test

Test with two browser windows:
1. Host creates private game, gets code
2. Guest joins with code
3. Both complete setup
4. Host makes move → Guest sees it
5. Guest makes move → Host sees it
6. Continue for 10+ moves including misses, letter guesses, word solves

---

### What's Deferred to Session 6

**Turn/version guarding** — DLYH is async turn-based. Race conditions only matter if both players click within milliseconds of each other, which the turn-based UI prevents. If testing reveals actual race bugs, add version guarding in Session 6.

---

### Verification Checklist (Session 5)

#### Setup & Join
- [ ] Host creates game, sees "Waiting for opponent..."
- [ ] Guest joins, both see correct opponent names
- [ ] Both have opponent setup loaded (grid size, word count)

#### Turn Synchronization
- [ ] Host makes coordinate guess → Guest sees cell revealed
- [ ] Guest makes letter guess → Host sees letters fill in
- [ ] Host makes word guess → Guest sees word solved
- [ ] Miss counter increments correctly on both sides
- [ ] Turn indicator correct on both sides

#### Phantom AI Regression
- [ ] Phantom AI games still work (LocalAIOpponent, no polling)
- [ ] Phantom AI setup saves to Supabase correctly

#### Resume
- [ ] Resumed game with opponent already joined goes straight to turn sync
- [ ] State restores correctly on both sides

---

## Session 6: Activity Tracking & Auto-Win ✅ IMPLEMENTED

(After Session 5 is verified working)

- [x] 5-day inactivity auto-win (Supabase edge function) - `supabase/functions/check-inactivity/`
- [x] Turn/version guarding if race conditions found - Added to `SaveGameStateToSupabaseAsync()`
- [x] Activity timestamp updates - Already existed in `lastActivityAt`
- [x] Client-side inactivity check on resume - Added to `HandleResumeGameAsync()`
- [x] `ClaimInactivityVictoryAsync()` method for claiming wins

**Deployment Required:**
- Deploy edge function: `supabase functions deploy check-inactivity`
- Set up cron schedule (see `supabase/README.md`)

---

## Session 7: Rematch UI Integration (DEFERRED)

Deferred indefinitely. Players can start new games from main menu.
`RematchService.cs` was deleted in Session 8.

---

## Session 8: Code Quality & Polish ✅ COMPLETE

### Completed Tasks

**1. Dead Code Removal (~1,800+ lines)**
- Deleted `NetworkGameManager.cs` (~435 lines)
- Deleted `WaitingRoomController.cs` (~300 lines)
- Deleted `GameStateSynchronizer.cs` (~689 lines)
- Deleted `RematchService.cs` (~562 lines)
- Cleaned up `RemotePlayerOpponent.cs` - removed unused methods

**2. Error Handling Polish**
- Added `StatusType.Error` enum for red error messages
- Added `.status-error` CSS styling
- Added retry logic (2 attempts, 500ms delay) for save failures
- User-visible error messages on persistent failures

**3. Word Placement Encryption**
- Changed from Base64 to XOR cipher with salt
- Salt: `"DLYH2026TecVooDoo"` + game code
- Backward compatible with legacy Base64 data
- Updated all callers to pass game code

**4. Namespace Cleanup**
- Standardized all to `DLYH.*` pattern
- Removed legacy `TecVooDoo.DontLoseYourHead.*` namespaces

---

## Testing Strategy

### Manual Test Checklist
1. Fresh load on tecvoodoo.com
2. Sign in (verify UI updates immediately)
3. Load DLYH iframe
4. Verify main menu shows signed-in identity
5. Host create private game
6. Guest join with code (second browser/tab)
7. Both complete setup
8. Play 10+ alternating moves
9. Verify state syncs correctly both directions

### Edge Cases to Test
- Host resumes game where opponent already joined
- Guest resumes game mid-turn
- Network disconnect during opponent's turn
- Rapid clicking (spam prevention)

---

## Session Close Checklist

After each session:
- [ ] Update DLYH_Status.md with changes and verified behavior
- [ ] Note any remaining issues in Known Issues
- [ ] Confirm sync method used (Polling for Session 5)
- [ ] Update session status in this document

---

**End of Networking Plan**

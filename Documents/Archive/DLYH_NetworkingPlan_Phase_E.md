# Don't Lose Your Head - Networking Implementation Plan (Phase E)

**Version:** 1.0
**Date Created:** January 19, 2026
**Last Updated:** January 20, 2026
**Status:** IN PROGRESS
**Purpose:** Complete multiplayer networking implementation with Supabase backend
**Reference:** Dots and Boxes (DAB) networking patterns

---

## Executive Summary

This plan covers the complete implementation of online multiplayer for DLYH, including:
- Authentication via TecVooDoo/Supabase
- Public matchmaking with phantom AI fallback
- Private games with join codes
- Real-time game state synchronization
- 5-day inactivity auto-win rules
- Rematch functionality

**Target:** Fully functional 2-player online games that persist in "My Active Games" across sessions.

---

## Session Progress

| Session | Task | Status | Date |
|---------|------|--------|------|
| 1 | Foundation & Editor Identity | COMPLETE | Jan 19, 2026 |
| 2 | Phantom AI as Session Player | COMPLETE | Jan 20, 2026 |
| 3 | Game State Persistence (NEW) | COMPLETE | Jan 20, 2026 |
| 4 | Opponent Join Detection | COMPLETE | Jan 26, 2026 |
| 5 | Turn Synchronization | IN PROGRESS | Jan 26, 2026 |
| 6 | Activity Tracking & Auto-Win | PENDING | - |
| 7 | Rematch UI Integration | PENDING | - |
| 8 | Code Quality & Polish | PENDING | - |

---

## Architecture Reference

### Flowchart (Revised Jan 19, 2026)

```
GAME ENTRY
    Launch DLYH -> NetworkingScene.unity -> UIFlowController Init
    |
    v
    Online Mode Selected?
    |-- No --> Offline / AI / Local Play
    |-- Yes --> Authenticated?
                |-- No --> TecVooDoo/Supabase sign-in
                |-- Yes --> Update Auth UI -> Ensure Player Record -> Load My Active Games

ONLINE MENU
    Choose Online Action:
    |-- Find Opponent --> Matchmaking Overlay --> Poll for match
    |                     |-- Match Found --> Join session --> Subscribe --> Start
    |                     |-- Timeout (6s) --> Insert Phantom AI --> Shows in Active Games --> Start
    |
    |-- Private Game --> Create session with code --> Waiting Room
    |                    |-- Opponent joins --> Start
    |                    |-- Player exits --> Return to Menu
    |
    |-- Join Code --> Lookup session --> Valid? --> Join --> Subscribe --> Start
    |                                --> Invalid/Full --> Error --> Menu
    |
    |-- My Active Games --> Load state --> Waiting? --> Waiting Room
                                       --> Active? --> Start

GAMEPLAY
    Start --> Gameplay Loop <--> Sync state to Supabase
                |
                |--> Record last-move timestamp
                |--> 5 days no opponent move? --> Supabase marks auto-win --> Unity receives outcome
                |
                |--> Game finished? --> End Screen
                                        |-- Rematch --> Menu
                                        |-- Exit --> Menu
```

### Key Tables (Supabase)

| Table | Purpose |
|-------|---------|
| `players` | Player identity records (id, display_name, is_ai) |
| `game_sessions` | Game records (id/code, status, state JSONB, created_by) |
| `session_players` | Player-to-game mapping (session_id, player_id, player_number, setup data) |
| `matchmaking_queue` | Temporary queue for public matchmaking |
| `rematch_requests` | Rematch request tracking |

### Key Services (Unity)

| Service | File | Purpose |
|---------|------|---------|
| AuthService | AuthService.cs | OAuth, Magic Link, Anonymous auth |
| PlayerService | PlayerService.cs | Player record CRUD |
| GameSessionService | GameSessionService.cs | Game session CRUD |
| MatchmakingService | MatchmakingService.cs | Matchmaking + private games |
| GameStateSynchronizer | GameStateSynchronizer.cs | State push/pull |
| RealtimeClient | RealtimeClient.cs | WebSocket subscriptions |
| RematchService | RematchService.cs | Rematch flow |

---

## Session 1: Foundation & Editor Identity

**Goal:** Ensure stable player identity in Editor for testing, verify auth flow works.

### Tasks

| # | Task | File(s) | Est. Time |
|---|------|---------|-----------|
| 1.1 | Verify PlayerService persists player_id in PlayerPrefs | PlayerService.cs | 15 min |
| 1.2 | Test: Change display name, verify same player_id used | Manual test | 15 min |
| 1.3 | Clear URL hash after OAuth callback (replaceState) | AuthCallbackHandler.cs | 30 min |
| 1.4 | Add auth state display to main menu (logged in as X) | UIFlowController.cs | 1 hr |
| 1.5 | Test full auth flow: Anonymous -> OAuth -> Restore session | Manual test | 30 min |

### Verification Checklist

- [ ] Editor: Start game, note player_id in console
- [ ] Editor: Change name in setup, start another game, same player_id
- [ ] Editor: Restart Unity, player_id restored from PlayerPrefs
- [ ] WebGL: OAuth redirect clears URL hash after callback
- [ ] Main menu shows current auth state (Guest/Signed In)

### Implementation Notes

**1.1 PlayerService Verification:**
PlayerService already has `SaveToPrefs()` and `RestoreFromPrefs()` (lines 246-269). Need to verify:
- `PREFS_PLAYER_ID` is persisted correctly
- `EnsurePlayerRecordAsync()` checks PlayerPrefs BEFORE creating new record

**1.3 Clear URL Hash:**
After `HandleAuthCallbackAsync()` succeeds, need to call JavaScript to clear hash:
```csharp
#if UNITY_WEBGL && !UNITY_EDITOR
Application.ExternalCall("window.history.replaceState", null, null, window.location.pathname);
#endif
```

---

## Session 2: Phantom AI as Session Player

**Goal:** When matchmaking times out, phantom AI becomes a real session_players entry so the game appears in My Active Games.

### Tasks

| # | Task | File(s) | Est. Time |
|---|------|---------|-----------|
| 2.1 | Create phantom AI player record if not exists | PlayerService.cs | 30 min |
| 2.2 | On matchmaking timeout, insert phantom AI into session_players | MatchmakingService.cs | 1 hr |
| 2.3 | Store phantom AI name in session_players.player_name | MatchmakingService.cs | 30 min |
| 2.4 | Update game status from "waiting" to "active" | MatchmakingService.cs | 15 min |
| 2.5 | Test: Matchmaking timeout -> game appears in My Active Games | Manual test | 30 min |

### Verification Checklist

- [ ] Start matchmaking, wait 6 seconds for timeout
- [ ] Phantom AI name displayed (e.g., "Alex")
- [ ] Game shows in My Active Games with phantom AI as opponent
- [ ] Resume phantom AI game works correctly
- [ ] LocalAIOpponent used for phantom AI games (not RemotePlayerOpponent)

### Implementation Notes

**2.1 Phantom AI Player Record:**
`PlayerService.EXECUTIONER_PLAYER_ID` already defined as `00000000-0000-0000-0000-000000000001`.
Create method to ensure this record exists:
```csharp
public async UniTask EnsurePhantomAIPlayerExistsAsync()
{
    // Check if EXECUTIONER_PLAYER_ID exists in players table
    // If not, create it with is_ai=true
}
```

**2.2 Insert Phantom AI:**
In `MatchmakingService.StartMatchmakingAsync()`, after timeout (line 213-231):
```csharp
// Instead of just setting IsPhantomAI=true, actually join the game
await _sessionService.JoinGame(newGame.Id, PlayerService.EXECUTIONER_PLAYER_ID, 2,
    new SessionPlayer { PlayerName = phantomName, PlayerColor = "#808080" });
await _sessionService.UpdateGameStatus(newGame.Id, "active");
```

---

## Session 3: Game State Persistence (NEW)

**Goal:** Save game progress to Supabase so games can be resumed with all guesses/reveals intact.

### Critical Requirement

Games that span multiple sessions (days) MUST save progress. Without this, resuming a game resets to the initial state, making the "My Active Games" feature useless for ongoing games.

### What Needs to Be Saved (per player)

| Field | Purpose |
|-------|---------|
| `currentTurn` | Whose turn it is ("player1" or "player2") |
| `misses` | Current miss count |
| `revealedCells` | Array of {row, col, letter, isHit} for cells revealed by guessing |
| `foundWords` | Array of word indices that are fully revealed |
| `guessedLetters` | Array of letters guessed via keyboard (for keyboard highlighting) |

### Tasks

| # | Task | File(s) | Est. Time |
|---|------|---------|-----------|
| 3.1 | Expand `DLYHGameplayState` class to include revealedCells, foundWords, guessedLetters | GameStateManager.cs | 30 min |
| 3.2 | After each guess, update local gameplay state | UIFlowController.cs | 1 hr |
| 3.3 | Save updated state to Supabase after each turn | UIFlowController.cs | 1 hr |
| 3.4 | On resume, reconstruct attack grid from revealedCells | UIFlowController.cs | 1 hr |
| 3.5 | On resume, reconstruct defense grid from opponent's revealedCells | UIFlowController.cs | 1 hr |
| 3.6 | On resume, restore keyboard highlighting from guessedLetters | UIFlowController.cs | 30 min |
| 3.7 | Test: Play several turns, exit, resume - all state restored | Manual test | 30 min |

### Implementation Notes

**3.1 Expanded DLYHGameplayState:**
```csharp
public class DLYHGameplayState
{
    public int misses;
    public int missLimit;
    public List<RevealedCell> revealedCells;    // NEW
    public List<int> foundWords;                 // NEW - indices of completed words
    public List<char> guessedLetters;           // NEW - for keyboard state
}

public class RevealedCell
{
    public int row;
    public int col;
    public char letter;
    public bool isHit;
}
```

**3.3 When to Save:**
- After player completes a turn (hit or miss)
- After opponent completes a turn (for multiplayer)
- Before exiting to main menu (safety save)

**3.4-3.5 Reconstructing Grid State:**
```csharp
// In TransitionToGameplayFromSavedState:
foreach (var cell in myData.gameplayState.revealedCells)
{
    // These are cells OPPONENT revealed on MY grid (defense)
    _defenseTableModel.SetCellState(cell.row, cell.col,
        cell.isHit ? TableCellState.Hit : TableCellState.Miss);
}

foreach (var cell in opponentData.gameplayState.revealedCells)
{
    // These are cells I revealed on OPPONENT's grid (attack)
    _attackTableModel.SetCellState(cell.row, cell.col,
        cell.isHit ? TableCellState.Hit : TableCellState.Miss);
    _attackTableModel.SetCellChar(cell.row, cell.col, cell.letter);
}
```

### Verification Checklist

- [x] Start phantom AI game, make 3-4 guesses (mix of hits and misses)
- [x] Exit to main menu
- [x] Resume game from My Active Games
- [x] Attack grid shows previously revealed cells correctly
- [x] Defense grid shows where opponent guessed
- [x] Miss counts are correct
- [x] Keyboard shows previously guessed letters
- [x] Found words are marked as complete
- [x] Whose turn is correct

### Implementation Summary (Completed Jan 20, 2026)

**Data Structures Added:**

1. **RevealedCellData** (GameSessionService.cs) - Serializable struct for Supabase storage:
   ```csharp
   public struct RevealedCellData { int row, col; string letter; bool isHit; }
   ```

2. **RevealedCellInfo** (GameplayStateTracker.cs) - Local tracking struct:
   ```csharp
   public struct RevealedCellInfo { char Letter; bool IsHit; }
   ```

3. **DLYHGameplayState** expanded to include `revealedCells` array

**Files Modified:**

| File | Changes |
|------|---------|
| GameSessionService.cs | Added RevealedCellData struct, added revealedCells to DLYHGameplayState |
| GameplayStateTracker.cs | Added RevealedCellInfo struct, PlayerRevealedCells/OpponentRevealedCells dictionaries |
| JsonParsingUtility.cs | Added ExtractRevealedCellsArray() method for JSON parsing |
| GameStateManager.cs | Updated ParseGameplayState() to parse revealedCells array |
| GameStateSynchronizer.cs | Added DictionaryToRevealedCellArray(), updated BuildGameplayState/ApplyRemoteStateToTracker |
| UIFlowController.cs | Added tracking dictionaries, SaveGameStateToSupabaseAsync(), RestoreGameplayStateFromSaved() |
| GameplayGuessManager.cs | Added SetInitialMissCounts() for resume without triggering game over |
| GuillotineOverlayManager.cs | Added SetBladeStageImmediately() for visual state on resume |

**Key Implementation Details:**

- State saved to Supabase after each turn ends (EndPlayerTurn/EndOpponentTurn)
- Revealed cells tracked as Dictionary<Vector2Int, (char, bool)> during gameplay
- On resume: grids reconstructed from revealedCells, keyboard from knownLetters, guillotine from miss counts
- Miss counts set without triggering game over checks via SetInitialMissCounts()
- Guillotine blade position set immediately without animation via SetBladeStageImmediately()

---

## Session 4: Opponent Join Detection

**Goal:** Private games detect when opponent joins and transition to gameplay.

### Tasks

| # | Task | File(s) | Est. Time |
|---|------|---------|-----------|
| 4.1 | Implement actual opponent join polling in WaitingRoom | NetworkingUIManager.cs | 1 hr |
| 4.2 | On opponent join, update UI to show opponent name | NetworkingUIManager.cs | 30 min |
| 4.3 | Transition to gameplay when both players ready | NetworkingUIManager.cs | 1 hr |
| 4.4 | Handle host starting game before opponent joins (waiting state) | UIFlowController.cs | 1 hr |
| 4.5 | Test: Create private game -> Share code -> Join -> Both see each other | Manual test | 30 min |

### Verification Checklist

- [ ] Create private game, see join code
- [ ] Second player joins with code
- [ ] Host sees "Player joined!" with opponent name
- [ ] Both players complete setup
- [ ] Game transitions to gameplay
- [ ] If host starts before opponent joins, game shows "Waiting for opponent..."

### Implementation Notes

**4.1 Opponent Join Polling:**
Replace stub in `PollForOpponentAsync()` (line 609-619):
```csharp
private async UniTask PollForOpponentAsync(string gameCode)
{
    while (_isActive)
    {
        await UniTask.Delay(2000);

        int playerCount = await _gameSessionService.GetPlayerCount(gameCode);
        if (playerCount >= 2)
        {
            // Get opponent info
            GameSessionWithPlayers gameWithPlayers = await _gameSessionService.GetGameWithPlayers(gameCode);
            SessionPlayer opponent = gameWithPlayers.Players.FirstOrDefault(p => p.PlayerNumber == 2);

            ShowOpponentJoined(opponent?.PlayerName ?? "Opponent");
            break;
        }
    }
}
```

---

## Session 5: Turn Synchronization

**Goal:** Turns sync correctly between two real players via Supabase.

### Tasks

| # | Task | File(s) | Est. Time |
|---|------|---------|-----------|
| 5.1 | Verify GameStateSynchronizer pushes state after each turn | GameStateSynchronizer.cs | 30 min |
| 5.2 | Verify RemotePlayerOpponent detects opponent actions from state diff | RemotePlayerOpponent.cs | 1 hr |
| 5.3 | Test turn detection: letter guess, coordinate guess, word guess | Manual test | 1 hr |
| 5.4 | Handle turn number conflicts (prevent race conditions) | GameStateSynchronizer.cs | 1 hr |
| 5.5 | Add "Waiting for opponent..." indicator during opponent turn | GameplayScreenManager.cs | 30 min |
| 5.6 | Test full 2-player game from start to finish | Manual test | 1 hr |

### Verification Checklist

- [ ] Player 1 makes letter guess -> Player 2 sees it
- [ ] Player 2 makes coordinate guess -> Player 1 sees it
- [ ] Word completion triggers extra turn correctly
- [ ] Miss count syncs correctly
- [ ] Win/loss detected correctly on both clients
- [ ] No duplicate or missed turns

### Implementation Notes

**5.4 Turn Number Conflicts:**
Current implementation uses monotonic `turnNumber` in state. Need to verify:
- Server rejects updates with stale turn numbers
- Client retries with fresh state on conflict

Consider adding Supabase RPC function:
```sql
CREATE FUNCTION update_game_state_if_current(
    p_game_id TEXT,
    p_expected_turn INT,
    p_new_state JSONB
) RETURNS BOOLEAN AS $$
BEGIN
    UPDATE game_sessions
    SET state = p_new_state
    WHERE id = p_game_id
    AND (state->>'turnNumber')::INT = p_expected_turn;
    RETURN FOUND;
END;
$$ LANGUAGE plpgsql;
```

---

## Session 6: Activity Tracking & Auto-Win

**Goal:** Implement 5-day inactivity rule via Supabase.

### Tasks

| # | Task | File(s) | Est. Time |
|---|------|---------|-----------|
| 6.1 | Update `lastActivityAt` timestamp on each turn | GameStateSynchronizer.cs | 30 min |
| 6.2 | Create Supabase edge function to check inactive games | Supabase Dashboard | 1 hr |
| 6.3 | Edge function marks winner and sets status="completed" | Supabase Dashboard | 30 min |
| 6.4 | Schedule edge function to run daily (pg_cron or external) | Supabase Dashboard | 30 min |
| 6.5 | Unity detects auto-win on game load, shows appropriate message | UIFlowController.cs | 1 hr |
| 6.6 | Test: Create game, don't move for 5+ days (simulate), verify auto-win | Manual test | 30 min |

### Verification Checklist

- [ ] Each turn updates player's lastActivityAt timestamp
- [ ] Supabase function identifies games with 5+ day inactivity
- [ ] Inactive games marked with winner = last active player
- [ ] Unity shows "Opponent abandoned - You win!" on load
- [ ] Game removed from "My Active Games" or marked completed

### Implementation Notes

**6.2 Supabase Edge Function:**
```typescript
// supabase/functions/check-inactive-games/index.ts
import { createClient } from '@supabase/supabase-js'

const INACTIVITY_DAYS = 5;

Deno.serve(async (req) => {
    const supabase = createClient(
        Deno.env.get('SUPABASE_URL')!,
        Deno.env.get('SUPABASE_SERVICE_ROLE_KEY')!
    );

    const cutoff = new Date();
    cutoff.setDate(cutoff.getDate() - INACTIVITY_DAYS);

    // Find active games where current player hasn't moved in 5 days
    const { data: games } = await supabase
        .from('game_sessions')
        .select('id, state')
        .eq('status', 'active')
        .eq('game_type', 'dlyh');

    for (const game of games || []) {
        const state = game.state;
        const currentPlayer = state.currentTurn; // "player1" or "player2"
        const currentPlayerData = state[currentPlayer];

        if (currentPlayerData?.lastActivityAt) {
            const lastActivity = new Date(currentPlayerData.lastActivityAt);
            if (lastActivity < cutoff) {
                // Auto-win for the OTHER player
                const winner = currentPlayer === 'player1' ? 'player2' : 'player1';

                await supabase
                    .from('game_sessions')
                    .update({
                        status: 'completed',
                        state: { ...state, winner, status: 'finished' }
                    })
                    .eq('id', game.id);
            }
        }
    }

    return new Response(JSON.stringify({ success: true }));
});
```

---

## Session 7: Rematch UI Integration

**Goal:** Wire RematchService to End Screen UI.

### Tasks

| # | Task | File(s) | Est. Time |
|---|------|---------|-----------|
| 7.1 | Add "Rematch" button to game end screen | GameEndOverlay.uxml | 30 min |
| 7.2 | Wire button to RematchService.RequestRematchAsync() | UIFlowController.cs | 1 hr |
| 7.3 | Show "Waiting for opponent..." when rematch requested | UIFlowController.cs | 30 min |
| 7.4 | Show "Opponent wants rematch!" when opponent requests | UIFlowController.cs | 30 min |
| 7.5 | Handle accept/decline rematch | UIFlowController.cs | 1 hr |
| 7.6 | On rematch accepted, start new game with swapped first turn | UIFlowController.cs | 1 hr |
| 7.7 | Test full rematch flow | Manual test | 30 min |

### Verification Checklist

- [ ] End screen shows "Rematch" button for online games
- [ ] Clicking Rematch shows waiting state
- [ ] Opponent sees rematch request notification
- [ ] Both accept -> new game created
- [ ] Decline -> both return to menu
- [ ] Timeout (30s) -> request expires
- [ ] First turn alternates between rematches

### Implementation Notes

**RematchService already implemented** with full functionality:
- `RequestRematchAsync(gameCode)` - Creates request, polls for response
- `AcceptRematchAsync(gameCode)` - Accepts and creates new game
- `DeclineRematchAsync(gameCode)` - Declines request
- `OnOpponentRequestedRematch` - Event when opponent requests
- `OnRematchAccepted` - Event with new game code

Just need to wire these to UI.

---

## Session 8: Code Quality & Polish

**Goal:** Address technical debt and recommendations from architecture review.

### Tasks

| # | Task | File(s) | Est. Time | Priority |
|---|------|---------|-----------|----------|
| 8.1 | Replace manual JSON parsing with JsonUtility | Multiple | 2 hrs | HIGH |
| 8.2 | Add exponential backoff to polling loops | Multiple | 1 hr | MEDIUM |
| 8.3 | Implement real encryption for word placements | GameStateManager.cs | 2 hrs | MEDIUM |
| 8.4 | Add input validation/sanitization for player names | PlayerService.cs | 30 min | MEDIUM |
| 8.5 | Extract hardcoded URLs/timeouts to SupabaseConfig | Multiple | 1 hr | LOW |
| 8.6 | Document state consistency model | DLYH_Status.md | 30 min | LOW |
| 8.7 | WebGL WebSocket bridge (or accept polling fallback) | RealtimeClient.cs | 2 hrs | MEDIUM |

### Verification Checklist

- [ ] JSON parsing uses proper serialization library
- [ ] Polling backs off on repeated failures
- [ ] Word placements encrypted (not just Base64)
- [ ] Player names sanitized (no XSS, length limits)
- [ ] All config values in SupabaseConfig ScriptableObject
- [ ] Consistency model documented
- [ ] WebGL either has realtime or graceful polling fallback

### Implementation Notes

**8.1 JSON Parsing:**
Options:
1. Unity's `JsonUtility` - Built-in, but limited (no dictionaries, strict typing)
2. `Newtonsoft.Json` - Full-featured, add via Package Manager
3. Keep `JsonParsingUtility` but improve robustness

Recommendation: Keep `JsonParsingUtility` for simple extractions, use Newtonsoft for complex serialization.

**8.3 Word Placement Encryption:**
Current Base64 is just encoding. Need real encryption:
```csharp
// Use AES-256 with key derived from game code + player salt
public static string EncryptWordPlacements(string placements, string gameCode, string playerId)
{
    byte[] key = DeriveKey(gameCode, playerId); // PBKDF2 or similar
    byte[] encrypted = AesEncrypt(Encoding.UTF8.GetBytes(placements), key);
    return Convert.ToBase64String(encrypted);
}
```

---

## Known Dependencies

### Supabase Schema Requirements

Verify these exist in Supabase:

```sql
-- game_sessions table
CREATE TABLE game_sessions (
    id TEXT PRIMARY KEY,           -- 6-char game code
    game_type TEXT NOT NULL,       -- 'dlyh'
    status TEXT DEFAULT 'waiting', -- waiting, active, completed, abandoned
    state JSONB,                   -- Full game state
    created_by UUID REFERENCES players(id),
    created_at TIMESTAMPTZ DEFAULT NOW(),
    updated_at TIMESTAMPTZ DEFAULT NOW()
);

-- session_players table
CREATE TABLE session_players (
    session_id TEXT REFERENCES game_sessions(id),
    player_id UUID REFERENCES players(id),
    player_number INT NOT NULL,    -- 1 or 2
    player_name TEXT,
    player_color TEXT,
    grid_size INT,
    word_count INT,
    difficulty TEXT,
    PRIMARY KEY (session_id, player_number)
);

-- matchmaking_queue table
CREATE TABLE matchmaking_queue (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    game_id TEXT REFERENCES game_sessions(id),
    game_type TEXT NOT NULL,
    player_id UUID REFERENCES players(id),
    grid_size INT,
    status TEXT DEFAULT 'waiting',
    created_at TIMESTAMPTZ DEFAULT NOW()
);

-- rematch_requests table
CREATE TABLE rematch_requests (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    game_id TEXT REFERENCES game_sessions(id),
    requester_id UUID REFERENCES players(id),
    accepter_id UUID,
    new_game_id TEXT,
    status TEXT DEFAULT 'pending', -- pending, accepted, declined
    created_at TIMESTAMPTZ DEFAULT NOW()
);
```

### External Dependencies

| Dependency | Purpose | Status |
|------------|---------|--------|
| Supabase Project | Backend | Configured |
| Supabase Auth | OAuth providers | Configured |
| Cloudflare Pages | WebGL hosting | dlyh.pages.dev |
| TecVooDoo Website | Auth callback | tecvoodoo.com |

---

## Risk Assessment

| Risk | Impact | Likelihood | Mitigation |
|------|--------|------------|------------|
| Turn race conditions | HIGH | MEDIUM | Server-side turn number validation |
| WebGL realtime not working | MEDIUM | HIGH | Fall back to polling |
| Player identity confusion | HIGH | LOW | Robust PlayerPrefs persistence |
| Phantom AI games not appearing | MEDIUM | MEDIUM | Proper session_players insertion |
| 5-day auto-win edge cases | LOW | MEDIUM | Thorough edge function testing |

---

## Testing Strategy

### Unit Tests (If Applicable)

- `JsonParsingUtility` - All extraction methods
- `GameStateManager` - Parse/serialize round-trip
- Miss limit calculation

### Integration Tests

| Test Case | Steps | Expected |
|-----------|-------|----------|
| Auth Flow | Sign in -> Sign out -> Restore | Session persists |
| Matchmaking Timeout | Start matchmaking, wait 6s | Phantom AI game starts |
| Private Game Join | Create game, share code, join | Both players in game |
| Turn Sync | P1 guesses letter | P2 sees letter revealed |
| Resume Game | Start game, close, reopen | Game state restored |
| Auto-Win | Simulate 5-day inactivity | Correct player wins |
| Rematch | Request, accept | New game starts |

### Manual Test Checklist (Before Each Session)

- [ ] Game launches without errors
- [ ] Main menu displays
- [ ] Solo game works
- [ ] Settings persist
- [ ] My Active Games loads (if applicable)

---

## Code Style Requirements

- No `var` usage - explicit types always
- Private fields use `_camelCase`
- Methods under 50 lines (prefer under 20)
- Events named `On` + PastTense (e.g., `OnOpponentJoined`)
- Prefer UniTask over coroutines
- ASCII-only in documentation and strings
- No allocations in hot paths (Update, per-guess)

---

## References

| Document | Path | Purpose |
|----------|------|---------|
| DLYH Status | `Documents/DLYH_Status.md` | Main project status |
| DAB Status | `E:\TecVooDoo\...\DAB_Status.md` | Reference multiplayer implementation |
| Phase 3 Refactor | `Documents/Refactor/DLYH_RefactoringPlan_Phase3_01192026.md` | Recent architecture work |
| Supabase Docs | supabase.com/docs | Backend reference |

---

## Session Close Checklist

After each session:
- [ ] Update session status in table above
- [ ] Update DLYH_Status.md "Last Session" section
- [ ] Add any new issues to Known Issues
- [ ] Commit changes with descriptive message
- [ ] Note any blockers for next session

---

**End of Networking Implementation Plan**

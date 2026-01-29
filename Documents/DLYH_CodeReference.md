# Don't Lose Your Head - Code Reference

**Purpose:** Quick reference for existing code, APIs, and conventions. **READ THIS FIRST** before writing new code to avoid referencing non-existent classes or methods.

**Last Updated:** January 28, 2026 (Session 87 - Session 8: Code Quality & Polish)

**Working Directory:** `C:\Unity\DontLoseYourHead` (NOT the backup folder)

---

## Namespaces

| Namespace | Purpose | Status |
|-----------|---------|--------|
| `DLYH.TableUI` | Main UI scripts (UIFlowController, TableView, etc.) | Active |
| `DLYH.Networking` | Opponent abstraction, IOpponent, RemotePlayerOpponent | Active |
| `DLYH.Networking.Services` | Supabase services (Auth, GameSession, etc.) | Active |
| `DLYH.Networking.UI` | Networking overlays (matchmaking, waiting room) | Active |
| `DLYH.UI.Managers` | Extracted UI managers (GameStateManager, ActiveGamesManager) | Active |
| `DLYH.UI.Services` | UI services (WordValidationService) | Active |
| `DLYH.AI.Core` | AI controllers (ExecutionerAI, MemoryManager) | Active |
| `DLYH.AI.Strategies` | AI guess strategies | Active |
| `DLYH.AI.Config` | AI configuration ScriptableObjects | Active |
| `DLYH.AI.Data` | AI data structures (GridAnalyzer, LetterFrequency) | Active |
| `DLYH.Audio` | Audio managers (UIAudioManager, MusicManager) | Active |
| `DLYH.Core.Utilities` | Shared utilities (JsonParsingUtility) | Active |
| `DLYH.Core.GameState` | Game state (WordPlacementData, DifficultySO, enums) | Active |
| `DLYH.Telemetry` | Playtest telemetry | Active |
| `DLYH.Editor` | Editor tools (WordBankImporter, TelemetryDashboard) | Active |

---

## Key Scripts - Networking

### IOpponent.cs
**Path:** `Assets/DLYH/Scripts/Networking/IOpponent.cs`
**Type:** Interface

**Purpose:** Abstraction for opponent (AI or Remote). UIFlowController talks to `_opponent` via this interface without knowing the implementation.

**Events:**
| Event | Params | Description |
|-------|--------|-------------|
| `OnThinkingStarted` | none | Opponent starts processing turn |
| `OnThinkingComplete` | none | Opponent finished thinking |
| `OnLetterGuess` | char | Opponent guessed a letter |
| `OnCoordinateGuess` | int row, int col | Opponent guessed a coordinate |
| `OnWordGuess` | string word, int wordIndex | Opponent guessed a word |
| `OnDisconnected` | none | Remote opponent disconnected |
| `OnReconnected` | none | Remote opponent reconnected |

**Properties:**
| Property | Type | Description |
|----------|------|-------------|
| `OpponentName` | string | Display name |
| `OpponentColor` | Color | Player color |
| `GridSize` | int | Opponent's grid size |
| `WordCount` | int | Opponent's word count |
| `WordPlacements` | List<WordPlacementData> | Opponent's word placements |
| `IsConnected` | bool | Always true for AI |
| `IsThinking` | bool | Currently processing turn |
| `IsAI` | bool | True for LocalAIOpponent |
| `MissLimit` | int | Calculated miss limit |

**Methods:**
| Method | Returns | Description |
|--------|---------|-------------|
| `InitializeAsync(PlayerSetupData)` | UniTask | Initialize opponent |
| `ExecuteTurn(AIGameState)` | void | Trigger opponent's turn |
| `RecordPlayerGuess(bool)` | void | Record player hit/miss |
| `RecordOpponentHit(int, int)` | void | Record opponent hit |
| `RecordRevealedLetter(char)` | void | Record revealed letter |
| `AdvanceTurn()` | void | Advance turn counter |
| `Reset()` | void | Reset for new game |

---

### NetworkingUIResult
**Path:** `Assets/DLYH/Scripts/Networking/UI/NetworkingUIManager.cs:17`
**Type:** Class

**Purpose:** Session context - stores result of matchmaking/join operations. Used by UIFlowController as `_matchmakingResult`.

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

**Session 5 Note:** This will be extended with opponent setup fields (OpponentGridSize, OpponentWordCount, OpponentColor, OpponentSetupLoaded).

---

### RemotePlayerOpponent.cs
**Path:** `Assets/DLYH/Scripts/Networking/RemotePlayerOpponent.cs`
**Type:** Class (implements IOpponent)

**Purpose:** Handles network multiplayer via Supabase. Contains turn detection logic.

**Key Methods:**
| Method | Line | Description |
|--------|------|-------------|
| `InitializeAsync()` | 114 | Full init (creates services) - **DO NOT USE AS-IS** |
| `ExecuteTurn()` | 267 | Starts waiting for opponent turn |
| `DetectOpponentAction()` | 350 | Compares states to detect what opponent did |
| `WaitForOpponentActionAsync()` | 290 | Polling loop for opponent moves |

**Session 5 Note:** Do not call full `InitializeAsync()` - it creates duplicate services. Wire events only using existing `_gameSessionService` from UIFlowController.

---

### LocalAIOpponent.cs
**Path:** `Assets/DLYH/Scripts/Networking/LocalAIOpponent.cs`
**Type:** Class (implements IOpponent)

**Purpose:** Wraps ExecutionerAI for local/phantom AI games. Generates guesses locally, no network.

**Usage:** Created when `IsPhantomAI == true` or for solo games.

---

### GameSessionService.cs
**Path:** `Assets/DLYH/Scripts/Networking/Services/GameSessionService.cs`
**Type:** Class

**Purpose:** CRUD operations for `game_sessions` table in Supabase.

**Key Methods:**
| Method | Returns | Description |
|--------|---------|-------------|
| `CreateGame(playerId)` | UniTask<GameSession> | Create new game with unique code |
| `GetGame(gameCode)` | UniTask<GameSession> | Get game by code |
| `GetGameWithPlayers(gameCode)` | UniTask<GameSessionWithPlayers> | Get game with session_players |
| `UpdateGameState(gameCode, state)` | UniTask<bool> | Update game state JSON |
| `JoinGame(gameCode, playerId)` | UniTask<bool> | Add player to game |

---

## Key Scripts - UI Flow

### UIFlowController.cs
**Path:** `Assets/DLYH/Scripts/UI/UIFlowController.cs`
**Type:** MonoBehaviour (~6770 lines)
**Namespace:** `DLYH.TableUI`

**Purpose:** Main orchestrator for all game screens and gameplay logic.

**Key Fields:**
| Field | Type | Line | Description |
|-------|------|------|-------------|
| `_matchmakingResult` | NetworkingUIResult | 114 | Session context from matchmaking |
| `_currentGameCode` | string | 124 | Current online game code |
| `_opponent` | IOpponent | ~181 | Current opponent (AI or Remote) |
| `_gameSessionService` | GameSessionService | ~200 | Supabase service |
| `_isPlayerTurn` | bool | ~250 | Whose turn it is |
| `_waitingForOpponent` | bool | ~260 | Waiting for opponent to join |

**Key Methods (Networking):**
| Method | Line | Description |
|--------|------|-------------|
| `SaveGameStateToSupabaseAsync()` | ~966 | **PUSH PATH** - saves state after turns (includes version guard) |
| `HandleOpponentJoined()` | ~7330 | Called when opponent joins private game |
| `FetchOpponentSetupForMatchmakingAsync()` | ~7280 | Fetches opponent data for matchmaking games (Build 6) |
| `StartOpponentJoinPolling()` | ~6850 | Polls Supabase for opponent join |
| `SavePlayerSetupToSupabaseAsync()` | ~820 | Saves player setup to game state |
| `SavePhantomAISetupToSupabaseAsync()` | ~890 | Saves phantom AI setup |
| `RebuildUIForOpponentJoinAsync()` | ~7450 | Rebuilds UI when opponent joins live game |
| `ClaimInactivityVictoryAsync()` | ~1110 | Claims victory when opponent inactive 5+ days (Session 6) |

**Activity & Version Tracking (Session 6):**
- `lastActivityAt` updated on each turn in `SaveGameStateToSupabaseAsync()` (line ~1031)
- Version guard checks `turnNumber` before saving (lines ~986-995)
- Inactivity check in `HandleResumeGameAsync()` (lines ~767-797)

**Turn Flow:**
1. `EndPlayerTurn()` (line ~2580) - saves state, switches turn
2. If online: calls `SaveGameStateToSupabaseAsync()` (with version guard)
3. If opponent's turn: calls `StartTurnDetectionPolling()`

---

### GameStateManager.cs
**Path:** `Assets/DLYH/Scripts/UI/Managers/GameStateManager.cs`
**Type:** Static class
**Namespace:** `DLYH.UI.Managers`

**Purpose:** Parses and serializes DLYHGameState JSON.

**Key Methods:**
| Method | Returns | Description |
|--------|---------|-------------|
| `ParseGameStateJson(string)` | DLYHGameState | Parse state from Supabase |
| `SerializeGameState(DLYHGameState)` | string | Serialize state to JSON |

---

### ActiveGamesManager.cs
**Path:** `Assets/DLYH/Scripts/UI/Managers/ActiveGamesManager.cs`
**Type:** Class
**Namespace:** `DLYH.UI.Managers`

**Purpose:** Manages "My Active Games" list on main menu.

**Key Methods:**
| Method | Returns | Description |
|--------|---------|-------------|
| `LoadMyActiveGamesAsync()` | UniTask | Fetch and display active games |
| `RefreshGamesList()` | void | Trigger refresh |

---

## Data Models

### DLYHGameState
**Path:** `Assets/DLYH/Scripts/Networking/Services/GameSessionService.cs`

```csharp
public class DLYHGameState
{
    public int version;
    public string status;           // waiting, setup, playing, finished
    public string currentTurn;      // "player1" or "player2"
    public int turnNumber;
    public string createdAt;
    public string updatedAt;
    public DLYHPlayerData player1;
    public DLYHPlayerData player2;
    public string winner;           // null, "player1", "player2"
}
```

### DLYHPlayerData
```csharp
public class DLYHPlayerData
{
    public string name;
    public string color;
    public bool ready;
    public bool setupComplete;
    public int gridSize;
    public int wordCount;
    public string difficulty;
    public string lastActivityAt;
    public DLYHGameplayState gameplayState;
}
```

### DLYHGameplayState
```csharp
public class DLYHGameplayState
{
    public int misses;
    public int missLimit;
    public string[] knownLetters;
    public int[] solvedWordRows;
    public string[] incorrectWordGuesses;
    public RevealedCellData[] revealedCells;
}
```

### PlayerSetupData
**Path:** `Assets/DLYH/Scripts/Networking/IOpponent.cs`

```csharp
public class PlayerSetupData
{
    public string PlayerName;
    public Color PlayerColor;
    public int GridSize;
    public int WordCount;
    public DifficultySetting DifficultyLevel;
    public int[] WordLengths;
    public List<WordPlacementData> PlacedWords;
}
```

---

## Supabase Tables

| Table | Purpose |
|-------|---------|
| `game_sessions` | Game state, turn info, player data |
| `session_players` | Links players to games |
| `players` | Player records (id, name, settings) |

---

## Scene Structure

### NetworkingScene.unity
**Path:** `Assets/DLYH/Scenes/NetworkingScene.unity`

**Primary scene for all gameplay.** Contains UIDocument with all screens.

**Key GameObjects:**
- `Main Camera`
- `Directional Light`
- `UIDocument` - UI Toolkit root
- `AudioManager` - Music and SFX

---

## Conventions

### Coding Standards
- No `var` keyword - explicit types always
- No per-frame allocations or LINQ
- Prefer async/await (UniTask) over coroutines
- ASCII-only in code and comments
- 800-1200 lines per file target

### IOpponent Usage
```csharp
// UIFlowController talks to _opponent interface
_opponent.OnLetterGuess += HandleOpponentLetterGuess;
_opponent.OnCoordinateGuess += HandleOpponentCoordinateGuess;
_opponent.OnWordGuess += HandleOpponentWordGuess;

// Execute opponent turn
_opponent.ExecuteTurn(gameState);
```

### State Save Pattern
```csharp
// After player turn ends
if (!string.IsNullOrEmpty(_currentGameCode) && _currentGameMode != GameMode.Solo)
{
    await SaveGameStateToSupabaseAsync();
}
```

### Opponent Join Polling Pattern
```csharp
// 3-second interval for join detection
while (!_opponentJoinCts.Token.IsCancellationRequested)
{
    await UniTask.Delay(3000, cancellationToken: _opponentJoinCts.Token);
    // Check Supabase for player2 data
}
```

---

## Dependencies Reference

### UniTask
```csharp
using Cysharp.Threading.Tasks;

// Async method
public async UniTask DoSomethingAsync()
{
    await UniTask.Delay(1000);
}

// Fire and forget
DoSomethingAsync().Forget();
```

### DOTween
```csharp
using DG.Tweening;

// Kill tweens before reset
DOTween.Kill(gameObject);

// Animate
transform.DOScale(1.2f, 0.3f).SetEase(Ease.OutBack);
```

---

## File Locations Quick Reference

| Type | Location |
|------|----------|
| Scripts | `Assets/DLYH/Scripts/<Namespace>/` |
| UI (UXML/USS) | `Assets/DLYH/UI/` |
| Scenes | `Assets/DLYH/Scenes/` |
| ScriptableObjects | `Assets/DLYH/Data/` |
| Documents | `Documents/` |

---

## Session 5 Implementation (COMPLETE)

**Session 5 added turn synchronization for real multiplayer:**

### New Methods Added

**RemotePlayerOpponent.cs:**
| Method | Description |
|--------|-------------|
| `InitializeWithExistingService()` | Lightweight init using existing GameSessionService |
| `ProcessStateUpdate()` | Called by polling to detect opponent action and fire events |
| `SetInitialState()` | Sets baseline state for comparison |
| `StartWaitingForOpponentTurn()` | Marks waiting state and fires OnThinkingStarted |

**UIFlowController.cs:**
| Method | Description |
|--------|-------------|
| `CreateRemotePlayerOpponentAsync()` | Creates and wires RemotePlayerOpponent |
| `StartTurnDetectionPolling()` | Starts 2-second polling for opponent moves |
| `StopTurnDetectionPolling()` | Stops polling on game end or turn return |
| `TurnDetectionPollingAsync()` | Async polling loop |
| `HandleOpponentDisconnected()` | Handler for disconnect event |
| `HandleOpponentReconnected()` | Handler for reconnect event |

### New Fields Added

**NetworkingUIResult:**
| Field | Type | Description |
|-------|------|-------------|
| `OpponentGridSize` | int | Opponent's grid dimensions |
| `OpponentWordCount` | int | Opponent's word count |
| `OpponentColor` | Color | Opponent's player color |
| `OpponentSetupLoaded` | bool | True when setup data is available |

**UIFlowController:**
| Field | Type | Description |
|-------|------|-------------|
| `_pollingForOpponentTurn` | bool | True when polling for opponent's move |
| `_lastKnownTurnNumber` | int | For detecting turn changes |
| `TURN_DETECTION_POLL_INTERVAL` | const | 2 seconds |

### Key Patterns

**Turn Detection Flow:**
1. Local player's turn ends -> `EndPlayerTurn()`
2. `SwitchToOpponentTurnCoroutine()` checks `_opponent.IsAI`
3. If not AI: `StartTurnDetectionPolling()`
4. Polling fetches state, compares `turnNumber`
5. `RemotePlayerOpponent.ProcessStateUpdate()` fires events
6. Events handled by existing handlers (letter/coordinate/word guess)
7. Polling stops when turn returns to local player

**Real Multiplayer Setup Flow:**
1. `TransitionToGameplay()` detects real multiplayer (not phantom AI)
2. Loads opponent setup from `_matchmakingResult`
3. Calls `CreateRemotePlayerOpponentAsync()`
4. Wires events (same as LocalAIOpponent)
5. Attack grid built with opponent's dimensions

### Polling Intervals
- Opponent join: 3 seconds (Session 4)
- Turn detection: 2 seconds (Session 5)

### What NOT to Use
- `RemotePlayerOpponent.InitializeAsync()` - stub that logs error; use `InitializeWithExistingService()` instead

### What TO Use
- `SaveGameStateToSupabaseAsync()` - working push path
- `_gameSessionService` - existing service instance
- `RemotePlayerOpponent.ProcessStateUpdate()` - for turn detection

---

## Session 5 Bug Fixes (Session 84 continued)

### Issue: Polling not detecting opponent moves

**Root Cause:** `DetectOpponentAction()` was checking `guessedCoordinates` field, but `SaveGameStateToSupabaseAsync()` only populates `revealedCells`.

**Fix:** Updated `DetectOpponentAction()` to use `revealedCells` instead of `guessedCoordinates`:
```csharp
// OLD (broken):
if (newState.guessedCoordinates != null && newState.guessedCoordinates.Length > lastCoordCount)

// NEW (fixed):
if (newState.revealedCells != null && newState.revealedCells.Length > lastRevealedCount)
```

**Files Changed:**
- `RemotePlayerOpponent.cs:DetectOpponentAction()` - Changed from guessedCoordinates to revealedCells

### Debug Logging Added

Added debug logging to diagnose polling issues:
- `TurnDetectionPollingAsync()` - logs each poll fetch and result
- `ProcessStateUpdate()` - logs when processing via RemotePlayerOpponent
- `SetInitialState()` - logs baseline state values
- `DetectOpponentAction()` - logs comparison values

---

## Session 8: Code Quality & Polish (COMPLETE)

### Dead Code Removed

**Files Deleted:**
| File | Lines | Reason |
|------|-------|--------|
| `NetworkGameManager.cs` | ~435 | Never instantiated |
| `WaitingRoomController.cs` | ~300 | Never used |
| `GameStateSynchronizer.cs` | ~689 | Only used by deleted code |
| `RematchService.cs` | ~562 | Session 7 deferred |

**Methods Removed:**
- `RemotePlayerOpponent.InitializeAsync()` body - replaced with stub that logs error
- `RemotePlayerOpponent.WaitForOpponentSetupAsync()` - only used by deleted method
- `RemotePlayerOpponent.EncryptWordPlacements()` - duplicate of GameStateManager method

### Error Handling Improvements

**GameplayScreenManager.cs:**
- Added `StatusType.Error` enum value
- CSS styling added: `.status-error { color: rgb(255, 100, 100); font-style: bold; }`

**UIFlowController.cs `SaveGameStateToSupabaseAsync()`:**
- Added retry logic: 2 attempts with 500ms delay
- User-visible error messages on failure

### Word Placement Encryption

**GameStateManager.cs:**
- Changed from plain Base64 to XOR cipher with salt
- Salt: `"DLYH2026TecVooDoo"` + optional game code
- Backward compatible with legacy Base64 data

```csharp
// New signature (optional gameCode for key entropy)
public static string EncryptWordPlacements(List<WordPlacementData> placements, string gameCode = null)
public static List<WordPlacementData> DecryptWordPlacements(string encrypted, string gameCode = null)
```

### Namespace Cleanup

**Old (Removed):**
- `TecVooDoo.DontLoseYourHead.Core`
- `TecVooDoo.DontLoseYourHead.UI`
- `TecVooDoo.DontLoseYourHead.Editor`

**New (Standard):**
- `DLYH.Core.GameState` - WordPlacementData, DifficultySO, DifficultyEnums, DifficultyCalculator, GameplayStateTracker, WordListSO
- `DLYH.UI.Services` - WordValidationService
- `DLYH.Editor` - WordBankImporter

**Files Updated:** 11 files had `using` statements updated from old to new namespaces.

---

**End of Code Reference**

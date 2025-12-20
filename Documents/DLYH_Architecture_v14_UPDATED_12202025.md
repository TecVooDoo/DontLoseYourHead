# Don't Lose Your Head - Architecture Document

**Version:** 14.0
**Date Created:** December 13, 2025
**Last Updated:** December 20, 2025
**Developer:** TecVooDoo LLC
**Total Scripts:** 63

---

## System Overview

```
                                    +-------------------+
                                    | MainMenuController|
                                    | (Container Mgmt)  |
                                    +---------+---------+
                                              |
                    +-------------------------+-------------------------+
                    |                                                   |
           +--------v--------+                                 +--------v--------+
           | SetupSettings   |                                 | GameplayUI      |
           | Panel           |                                 | Controller      |
           +--------+--------+                                 +--------+--------+
                    |                                                   |
           +--------v--------+                                 +--------+--------+
           | PlayerGridPanel |                                 |        |        |
           | (Setup Mode)    |                        +--------v--+ +---v----+ +-v---------+
           +--------+--------+                        | PlayerGrid| |Execu-  | |GuessProc- |
                    |                                 | Panel (x2)| |tionerAI| |essor (x2) |
     +--------------+---------------+                 +-----------+ +--------+ +-----------+
     |    |    |    |    |    |     |
     v    v    v    v    v    v     v
  [Extracted Controllers + Services]
```

---

## Namespaces

| Namespace | Scripts | Purpose |
|-----------|---------|---------|
| `TecVooDoo.DontLoseYourHead.UI` | 26 | Main UI scripts |
| `DLYH.UI` | 6 | Main menu, settings, help overlay, tooltips |
| `TecVooDoo.DontLoseYourHead.UI.Utilities` | 1 | RowDisplayBuilder |
| `TecVooDoo.DontLoseYourHead.Core` | 4 | Game state/difficulty |
| `DLYH.AI.Config` | 1 | AI configuration |
| `DLYH.AI.Core` | 4 | AI controllers |
| `DLYH.AI.Data` | 2 | AI data utilities |
| `DLYH.AI.Strategies` | 4 | AI guess strategies |
| `DLYH.Audio` | 5 | UI, guillotine, and music audio systems |
| `DLYH.Telemetry` | 1 | Playtest analytics [UPDATED v13] |
| `DLYH.Editor` | 1 | Telemetry Dashboard [NEW v13] |

---

## File Structure

```
Assets/DLYH/Scripts/
|
+-- AI/
|   |-- Config/
|   |   +-- ExecutionerConfigSO.cs      (~412 lines)
|   |-- Core/
|   |   |-- AISetupManager.cs           (~468 lines)
|   |   |-- DifficultyAdapter.cs        (~268 lines)
|   |   |-- ExecutionerAI.cs            (~493 lines)
|   |   +-- MemoryManager.cs            (~442 lines)
|   |-- Data/
|   |   |-- GridAnalyzer.cs             (~442 lines)
|   |   +-- LetterFrequency.cs          (~442 lines)
|   +-- Strategies/
|       |-- CoordinateGuessStrategy.cs  (~262 lines)
|       |-- IGuessStrategy.cs           (~493 lines)
|       |-- LetterGuessStrategy.cs      (~327 lines)
|       +-- WordGuessStrategy.cs        (~327 lines)
|
+-- Audio/
|   |-- GuillotineAudioManager.cs       (~335 lines)
|   |-- MusicManager.cs                 (~420 lines)
|   |-- SFXClipGroup.cs                 (~75 lines)
|   |-- UIAudioManager.cs               (~415 lines)
|   +-- UIButtonAudio.cs                (~70 lines)
|
+-- Editor/
|   +-- TelemetryDashboard.cs           (~920 lines) [NEW v13]
|
+-- Telemetry/
|   +-- PlaytestTelemetry.cs            (~460 lines) [UPDATED v13]
|
+-- Core/
|   |-- DifficultyCalculator.cs
|   |-- DifficultySO.cs
|   |-- Grid.cs
|   |-- GridCell.cs
|   |-- Word.cs
|   |-- WordListSO.cs
|   +-- ...
|
+-- UI/
    |-- GameplayUIController.cs         (~2,150 lines) [UPDATED v13]
    |-- GridCellUI.cs                   (~250 lines)
    |-- GuessedWordListController.cs    (~260 lines)
    |-- GuillotineDisplay.cs            (~460 lines)
    |-- HeadFaceController.cs           (~205 lines)
    |-- LetterButton.cs                 (~200 lines)
    |-- MainMenuController.cs           (~455 lines) [UPDATED v14]
    |-- MessagePopup.cs                 (~450 lines) [UPDATED v14]
    |-- FeedbackPanel.cs                (~195 lines)
    |-- HelpOverlay.cs                  (~415 lines)
    |-- ButtonTooltip.cs                (~145 lines)
    |-- TooltipPanel.cs                 (~30 lines)
    |-- PlayerGridPanel.cs              (~1,120 lines)
    |-- SettingsPanel.cs                (~270 lines)
    |-- SetupModeController.cs          (~150 lines)
    |-- SetupSettingsPanel.cs           (~850 lines)
    |-- WordPatternRow.cs               (~1,199 lines)
    |-- AutocompleteDropdown.cs         (~450 lines)
    |-- AutocompleteItem.cs             (~140 lines)
    |-- Controllers/
    |   |-- AutocompleteManager.cs      (~385 lines)
    |   |-- CoordinatePlacementController.cs (~620 lines)
    |   |-- GridCellManager.cs          (~150 lines)
    |   |-- GridColorManager.cs         (~130 lines)
    |   |-- GridLayoutManager.cs        (~600 lines)
    |   |-- LetterTrackerController.cs  (~175 lines)
    |   |-- PlacementPreviewController.cs (~200 lines)
    |   |-- PlayerColorController.cs    (~80 lines)
    |   |-- WordGuessInputController.cs (~310 lines)
    |   |-- WordGuessModeController.cs  (~290 lines)
    |   |-- WordPatternController.cs    (~285 lines)
    |   +-- WordPatternRowManager.cs    (~400 lines)
    |-- Interfaces/
    |   +-- IGridControllers.cs         (~115 lines)
    |-- Services/
    |   |-- GameplayStateTracker.cs     (~300 lines)
    |   |-- GuessProcessor.cs           (~400 lines)
    |   |-- WinConditionChecker.cs      (~225 lines)
    |   +-- WordValidationService.cs    (~60 lines)
    +-- Utilities/
        +-- RowDisplayBuilder.cs        (~207 lines)
```

---

## Layer 0: Interfaces (IGridControllers.cs)

Contains 5 interfaces and 2 enums for controller contracts:

### IGridDisplayController
Grid display operations (cell creation, sizing, labels).

### ILetterTrackerController
Letter tracker/keyboard operations.

### IWordPatternController
Word pattern row management.

### ICoordinatePlacementController
Coordinate placement mode operations.

### IGridColorManager
Grid color/highlighting operations.

### Enums
- `GridHighlightType`: None, Cursor, ValidPlacement, InvalidPlacement, PlacedLetter
- `PlacementState`: Inactive, SelectingFirstCell, SelectingDirection

---

## Layer 1: Main Orchestrators

### MainMenuController.cs (~455 lines) [UPDATED v14]
**Namespace:** `DLYH.UI`
**Purpose:** Controls game flow between Main Menu, Setup, and Gameplay phases.

**v14 Updates:**
- Added trivia display system with 25 historical facts
- Trivia fades in/out every 5 seconds
- Trivia starts/stops when main menu shown/hidden

### GameplayUIController.cs (~2,150 lines) [UPDATED v13]
**Namespace:** `TecVooDoo.DontLoseYourHead.UI`
**Purpose:** Master controller for gameplay phase. Manages two PlayerGridPanels, guess processing, turn management, win/lose conditions, AI opponent integration, extra turn queue, guillotine face updates, and telemetry reporting.

**v13 Updates:**
- Now calls `PlaytestTelemetry.SetTurnNumber()` after each turn
- Calls `MessagePopup.ResetPosition()` at game start
- Added game abandon tracking in `ReturnToSetup()`

### SetupSettingsPanel.cs (~850 lines)
**Namespace:** `TecVooDoo.DontLoseYourHead.UI`
**Purpose:** Manages player configuration during setup phase.

### PlayerGridPanel.cs (~1,120 lines)
**Namespace:** `TecVooDoo.DontLoseYourHead.UI`
**Purpose:** Manages a single player's grid display. Used in both Setup and Gameplay modes.

---

## Layer 2: Extracted Controllers

| Controller | Lines | Purpose |
|------------|-------|---------|
| WordPatternController | ~285 | Word pattern row management |
| GridLayoutManager | ~600 | Grid cell creation, sizing, labels |
| LetterTrackerController | ~175 | Letter button management |
| GridColorManager | ~130 | Grid cell color state |
| PlacementPreviewController | ~200 | Placement preview display |
| CoordinatePlacementController | ~620 | Coordinate placement mode state machine |
| WordPatternRowManager | ~400 | Word row collection management |
| GridCellManager | ~150 | Cell array management |
| WordGuessModeController | ~290 | Word guess mode state machine |
| WordGuessInputController | ~310 | Word guess input handling |
| PlayerColorController | ~80 | Color picker management |
| AutocompleteManager | ~385 | Word autocomplete logic |

---

## Layer 3: Services

| Service | Lines | Purpose |
|---------|-------|---------|
| GuessProcessor | ~400 | Generic guess processing for player/opponent |
| WordValidationService | ~60 | Word bank validation |
| GameplayStateTracker | ~300 | Player/opponent state tracking |
| WinConditionChecker | ~225 | Win/lose condition checking |

---

## Layer 4: UI Components

| Component | Lines | Purpose |
|-----------|-------|---------|
| WordPatternRow | ~1,199 | Individual word entry row |
| GridCellUI | ~250 | Individual grid cell |
| LetterButton | ~200 | Letter tracker button |
| GuessedWordListController | ~260 | Guessed words display |
| SettingsPanel | ~270 | Audio settings |
| FeedbackPanel | ~195 | End-game/menu feedback collection |
| HelpOverlay | ~415 | Draggable gameplay help panel |
| AutocompleteDropdown | ~450 | Word suggestions |
| AutocompleteItem | ~140 | Dropdown entry |
| GuillotineDisplay | ~460 | Animated guillotine with blade/head/face |
| HeadFaceController | ~205 | Head facial expression controller |
| MessagePopup | ~450 | Turn notifications with position memory [UPDATED v13] |

---

## Layer 5: AI System (11 Scripts)

### ExecutionerConfigSO.cs (~412 lines)
**Namespace:** `DLYH.AI.Config`
**Purpose:** ScriptableObject containing all tunable AI parameters.

### ExecutionerAI.cs (~493 lines)
**Namespace:** `DLYH.AI.Core`
**Purpose:** Main AI MonoBehaviour coordinating turn execution.

### DifficultyAdapter.cs (~268 lines)
**Namespace:** `DLYH.AI.Core`
**Purpose:** Rubber-banding system with adaptive thresholds.

### MemoryManager.cs (~442 lines)
**Namespace:** `DLYH.AI.Core`
**Purpose:** Skill-based memory filtering.

### AISetupManager.cs (~468 lines)
**Namespace:** `DLYH.AI.Core`
**Purpose:** AI word selection and grid placement.

### IGuessStrategy.cs (~493 lines)
**Namespace:** `DLYH.AI.Strategies`
**Purpose:** Interface and data structures (AIGameState, GuessRecommendation).

### LetterGuessStrategy.cs (~327 lines)
**Namespace:** `DLYH.AI.Strategies`
**Purpose:** Letter selection based on frequency + pattern analysis.

### CoordinateGuessStrategy.cs (~262 lines)
**Namespace:** `DLYH.AI.Strategies`
**Purpose:** Coordinate selection based on adjacency and patterns.

### WordGuessStrategy.cs (~327 lines)
**Namespace:** `DLYH.AI.Strategies`
**Purpose:** Word guess decisions based on confidence thresholds.

### LetterFrequency.cs (~442 lines)
**Namespace:** `DLYH.AI.Data`
**Purpose:** Static English letter frequency data.

### GridAnalyzer.cs (~442 lines)
**Namespace:** `DLYH.AI.Data`
**Purpose:** Fill ratio and coordinate scoring utilities.

---

## Layer 6: Audio System (5 Scripts)

### UIAudioManager.cs (~415 lines)
**Namespace:** `DLYH.Audio`
**Purpose:** Singleton manager for UI sound effects.

**Features:**
- `_isMuted` field and `IsMuted` property
- `ToggleMute()`, `Mute()`, `Unmute()` methods
- Static `ToggleMuteSFX()` and `IsSFXMuted()` methods

### MusicManager.cs (~420 lines)
**Namespace:** `DLYH.Audio`
**Purpose:** Singleton manager for background music with shuffle playlist and crossfade.

**Features:**
- Shuffle playlist (Fisher-Yates algorithm)
- Crossfade between tracks (1.5 seconds default)
- Never repeats same song consecutively
- Mute toggle support
- Dynamic tempo/pitch adjustment for tension
- Starts automatically at Main Menu

**Key Methods:**
- `PlayMusic()` / `StopMusic()` - Playback control
- `PlayNextTrack()` / `PlayPreviousTrack()` - Manual track control
- `ToggleMute()` / `Mute()` / `Unmute()` - Mute control
- `SetTensionLevel(float)` - Adjust tempo based on danger (0-1)
- `ResetTension()` - Return to normal tempo

**Static Convenience Methods:**
- `MusicManager.ToggleMuteMusic()`
- `MusicManager.SetTension(float)`
- `MusicManager.ResetMusicTension()`
- `MusicManager.IsMusicMuted()`

**Tension Levels:**
| Danger % | Pitch |
|----------|-------|
| 0-79% | 1.0x (normal) |
| 80-94% | 1.08x (medium tension) |
| 95%+ | 1.12x (high tension) |

### GuillotineAudioManager.cs (~335 lines)
**Namespace:** `DLYH.Audio`
**Purpose:** Singleton manager for guillotine-specific sounds (blade raise, execution chops, head removal).

**Features:**
- Layered audio (rope stretch + blade movement together)
- Fast chop (miss limit execution) vs slow chop (words found execution)
- Static convenience methods: `BladeRaise()`, `ExecutionFast()`, `ExecutionSlow()`, `HeadRemoved()`

### SFXClipGroup.cs (~75 lines)
**Namespace:** `DLYH.Audio`
**Purpose:** ScriptableObject for grouping audio clips with randomization.

### UIButtonAudio.cs (~70 lines)
**Namespace:** `DLYH.Audio`
**Purpose:** Component to auto-add click sounds to buttons.

---

## Layer 7: Guillotine Visual System

### GuillotineDisplay.cs (~460 lines)
**Namespace:** `TecVooDoo.DontLoseYourHead.UI`
**Purpose:** Controls guillotine visual assembly and animations.

**Components:**
- Frame (left post, right post, top beam)
- Hash marks (dynamically generated based on miss limit)
- Blade group (animates up/down)
- Head (colored circle, player's color)
- Lunette (neck restraint)
- Basket (catches fallen head)

**Key Methods:**
- `Initialize(missLimit, playerColor)` - Sets up hash marks and head color
- `UpdateMissCount(misses)` - Animates blade position
- `UpdateFace(opponentMisses, opponentMissLimit)` - Updates facial expression
- `SetExecutionFace(isBeingExecuted)` - Sets horror/victory face
- `AnimateGameOver()` - Fast blade drop (miss limit reached)
- `AnimateDefeatByWordsFound()` - Dramatic raise then drop

### HeadFaceController.cs (~205 lines)
**Namespace:** `TecVooDoo.DontLoseYourHead.UI`
**Purpose:** Controls head facial expressions based on game state.

**Face States:**
- **Positive:** Happy, Smug, Evil Smile (when opponent in danger)
- **Neutral:** Neutral, Concerned
- **Negative:** Worried, Scared, Terror (when self in danger)
- **Execution:** Horror (being executed), Victory (winner)

**Thresholds:**
| Threshold | Percentage | Face |
|-----------|------------|------|
| Concerned | 25% | Concerned |
| Worried | 50% | Worried |
| Scared | 75% | Scared |
| Terror | 90% | Terror |

**Key Methods:**
- `Initialize(lookLeft)` - Sets face direction and default expression
- `UpdateFace(ownMisses, ownLimit, oppMisses, oppLimit)` - Dynamic expression
- `SetExecutionFace(isBeingExecuted)` - Execution expressions

---

## Layer 8: Telemetry System [UPDATED v13]

### PlaytestTelemetry.cs (~460 lines) [UPDATED v13]
**Namespace:** `DLYH.Telemetry`
**Purpose:** Collects and sends playtest data to Cloudflare Worker.

**v13 Updates:**
- Tracks `_isGameInProgress` and `_currentTurnNumber` for abandon detection
- `LogGameStart()` now includes player name
- `LogGameAbandon()` called on quit if game in progress
- `UpdateTurnNumber()` called by GameplayUIController each turn

**Events Tracked:**
| Event | Data |
|-------|------|
| session_start | Platform, version, screen size |
| session_end | Automatic on quit |
| game_start | Player name, grid sizes, word counts, difficulties |
| game_end | Win/loss, misses, total turns |
| game_abandon | Phase (gameplay/quit), turn number |
| player_guess | Guess type, hit/miss, value |
| player_feedback | Feedback text, win/loss context |
| error | Unity errors/exceptions |

**Static Methods:**
- `SessionStart()`, `SessionEnd()`
- `GameStart(playerName, ...)`, `GameEnd(...)`, `GameAbandon(...)`
- `SetTurnNumber(int)` [NEW v13]
- `Guess(...)`, `Feedback(...)`

### TelemetryDashboard.cs (~920 lines) [NEW v13]
**Namespace:** `DLYH.Editor`
**Purpose:** Editor window for viewing telemetry data from Cloudflare Worker.

**Menu:** DLYH > Telemetry Dashboard

**Features:**
- 4 tabs: Summary, Game Stats, Recent Events, Feedback
- Fetches from `/summary`, `/events`, `/feedback` endpoints
- Parses event data for player names, difficulties, win rates
- Export to CSV button

**Tabs:**
| Tab | Content |
|-----|---------|
| Summary | Event type breakdown with visual bars |
| Game Stats | Sessions, games, completion rate, win rate, player leaderboard, difficulty distribution |
| Recent Events | Last 100 events with formatted data display |
| Feedback | Player feedback comments with WIN/LOSS indicators |

---

## Layer 9: MessagePopup System [UPDATED v14]

### MessagePopup.cs (~450 lines) [UPDATED v14]
**Namespace:** `TecVooDoo.DontLoseYourHead.UI`
**Purpose:** Displays turn notifications, guess results, and game over messages.

**v14 Updates:**
- Default position at bottom of gameplay area (Y=62.5 with bottom-center anchors)
- Simplified `GetDefaultBottomPosition()` returns fixed position
- Remembers dragged position via `_lastDraggedPosition` and `_hasBeenDragged`
- `ResetDraggedPosition()` called at game start

**Key Methods:**
- `ShowMessage(string)` - Auto-fading message
- `ShowGameOverMessage(string)` - Persistent with Continue button
- `ResetDraggedPosition()` - Resets position memory
- `GetDefaultBottomPosition()` - Returns bottom-of-screen position

**Static Methods:**
- `Show(string)`, `Show(string, float duration)`
- `ShowGameOver(string)`
- `ResetPosition()`

---

## Key Patterns

### 1. Controller Extraction Pattern
Large MonoBehaviours delegate to plain C# controller classes that receive dependencies via constructor.

### 2. Callback Injection Pattern
Services receive Actions/Funcs for operations they need but don't own.

### 3. Defensive Initialization Pattern
`EnsureControllersInitialized()` allows safe calling before Start() runs.

### 4. Event-Driven Communication
Controllers publish events; parents subscribe. No tight coupling.

### 5. Event Timing Pattern
When event handlers check state, update state BEFORE firing events.

```csharp
// CORRECT:
_isActive = false;
OnSomeEvent?.Invoke();

// WRONG:
OnSomeEvent?.Invoke();
_isActive = false;
```

### 6. UI Initialization Pattern
Always explicitly set UI components to known states during initialization.

### 7. Position Validation Pattern
Validate positions before using them for UI placement.

### 8. Event Re-trigger Guard Pattern
Hide/reset UI both before and after batch operations.

### 9. Input Mode Routing Pattern
Check for active modal states at start of input handlers.

### 10. Strategy Pattern (AI)
IGuessStrategy implementations can be swapped or weighted based on game state.

### 11. Extra Turn Queue Pattern
Completed words are queued and processed one at a time, giving players reward turns.

```csharp
private Queue<string> _extraTurnQueue = new Queue<string>();

// On word completion:
_extraTurnQueue.Enqueue(completedWord);

// After guess result:
if (_extraTurnQueue.Count > 0)
{
    string word = _extraTurnQueue.Dequeue();
    // Show extra turn message, don't end turn
}
```

### 12. Singleton Audio Manager Pattern
Audio managers use singleton pattern with static convenience methods for easy access.

```csharp
// Instance access
MusicManager.Instance.PlayNextTrack();

// Static convenience
MusicManager.ToggleMuteMusic();
UIAudioManager.ToggleMuteSFX();
```

### 13. Position Memory Pattern [NEW v13]
UI elements remember user-modified positions and restore them on subsequent displays.

```csharp
private Vector2 _lastDraggedPosition;
private bool _hasBeenDragged;

// On drag:
_lastDraggedPosition = _popupRect.anchoredPosition;
_hasBeenDragged = true;

// On show:
if (_hasBeenDragged)
    _popupRect.anchoredPosition = _lastDraggedPosition;
else
    _popupRect.anchoredPosition = GetDefaultPosition();
```

---

## Data Flow Diagrams

### Setup Mode Word Entry Flow

```
User types 'C'
    -> SetupSettingsPanel.HandleKeyboardInput()
    -> WordPatternController.AddLetterToSelectedRow('C')
    -> WordPatternRow.AddLetter('C')
    -> [Check if word complete]
        -> (complete) -> WordValidationService.IsValidWord()
            -> (valid) -> Enable compass button
```

### Gameplay Guess Flow

```
Player clicks opponent's letter 'E'
    -> GameplayUIController.HandleLetterGuess('E')
        -> [Check if word guess mode active]
            -> (yes) -> Route to WordGuessModeController
            -> (no) -> GuessProcessor.ProcessLetterGuess('E')
                -> Hit/Miss/AlreadyGuessed result
                -> [Check for completed words]
                    -> (completed) -> Queue extra turns
```

### AI Turn Flow

```
EndPlayerTurn()
    -> RecordPlayerGuess(wasHit) -> DifficultyAdapter updates skill
    -> PlaytestTelemetry.SetTurnNumber(_totalTurns) [NEW v13]
    -> TriggerAITurn()
    -> ExecutionerAI.ExecuteTurnAsync()
    -> Wait think time (0.8-2.5s)
    -> BuildAIGameState()
    -> SelectStrategy() based on grid density
    -> Fire OnLetterGuess/OnCoordinateGuess/OnWordGuess event
    -> GameplayUIController.HandleAI*Guess() processes result
    -> EndOpponentTurn()
```

### Guillotine Face Update Flow

```
Miss occurs
    -> UpdateMissCount(misses)
    -> AnimateBladeToPosition()
    -> UpdateGuillotineFaces()
        -> Player1Guillotine.UpdateFace(oppMisses, oppLimit)
        -> Player2Guillotine.UpdateFace(playerMisses, playerLimit)
        -> HeadFaceController.UpdateFace() determines expression
```

### Music Playback Flow

```
Game starts
    -> MusicManager.Start()
    -> PlayMusic()
    -> ShufflePlaylist()
    -> PlayCurrentTrack()

Track ends
    -> Update() detects !isPlaying
    -> PlayNextTrack()
    -> StartCrossfade(nextClip)
    -> Fade out old, fade in new
    -> Swap active/inactive sources

Miss occurs (danger check)
    -> GameplayUIController updates miss count
    -> MusicManager.SetTension(misses / missLimit)
    -> Pitch adjusts smoothly via coroutine
```

### Game Abandon Flow [NEW v13]

```
Player closes browser mid-game
    -> OnApplicationQuit()
    -> [Check _isGameInProgress]
        -> (true) -> LogGameAbandon("quit", _currentTurnNumber)
    -> LogEvent("session_end", null)

Player clicks Return to Setup mid-game
    -> ReturnToSetup()
    -> [Check _stateTracker != null && !_stateTracker.GameOver]
        -> (true) -> PlaytestTelemetry.GameAbandon("gameplay", _totalTurns)
```

---

## Event Architecture

```
MainMenuController
    +--- OnNewGameClicked ---> SetupSettingsPanel
    +--- OnSettingsClicked ---> SettingsPanel
    +--- OnFeedbackClicked ---> FeedbackPanel

SetupSettingsPanel
    +--- OnGridSizeChanged ---> PlayerGridPanel.SetGridSize()
    +--- OnWordCountChanged ---> WordPatternController.SetWordLengths()
    +--- OnSetupComplete ---> GameplayUIController

PlayerGridPanel
    +--- OnCellClicked ---> CoordinatePlacementController
    +--- OnLetterClicked ---> GameplayUIController (gameplay mode)

WordPatternController
    +--- OnWordRowSelected ---> SetupSettingsPanel
    +--- OnCoordinateModeRequested ---> PlayerGridPanel
    +--- OnWordPlaced ---> SetupSettingsPanel

GameplayUIController
    +--- OnMissCountChanged ---> UpdateGuillotineFaces()
    +--- OnWordCompleted ---> Queue extra turn
    +--- OnGameEnded(playerWon) ---> MainMenuController -> FeedbackPanel
    +--- (subscribes to) ExecutionerAI events

ExecutionerAI
    +--- OnThinkingStarted ---> UI thinking indicator
    +--- OnLetterGuess ---> GameplayUIController.HandleAILetterGuess()
    +--- OnCoordinateGuess ---> GameplayUIController.HandleAICoordinateGuess()
    +--- OnWordGuess ---> GameplayUIController.HandleAIWordGuess()

GuillotineDisplay
    +--- (calls) GuillotineAudioManager static methods
    +--- (calls) HeadFaceController.UpdateFace/SetExecutionFace

FeedbackPanel
    +--- OnFeedbackComplete ---> MainMenuController.ShowMainMenu()

MusicManager
    +--- Auto-starts in Start()
    +--- Responds to SettingsPanel volume changes (via polling)
    +--- Responds to mute button clicks via ToggleMute()

MessagePopup [UPDATED v13]
    +--- Responds to drag events -> remembers position
    +--- OnContinueClicked ---> GameplayUIController
```

---

**End of Architecture Document**

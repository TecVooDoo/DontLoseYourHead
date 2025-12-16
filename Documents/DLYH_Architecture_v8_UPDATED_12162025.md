# Don't Lose Your Head - Architecture Document

**Version:** 8.0
**Date Created:** December 13, 2025
**Last Updated:** December 16, 2025
**Developer:** TecVooDoo LLC
**Total Scripts:** 53

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
| `TecVooDoo.DontLoseYourHead.UI` | 24 | Main UI scripts |
| `TecVooDoo.DontLoseYourHead.UI.Utilities` | 1 | RowDisplayBuilder |
| `TecVooDoo.DontLoseYourHead.Core` | 4 | Game state/difficulty |
| `DLYH.UI` | 1 | Main menu |
| `DLYH.AI.Config` | 1 | AI configuration |
| `DLYH.AI.Core` | 4 | AI controllers |
| `DLYH.AI.Data` | 2 | AI data utilities |
| `DLYH.AI.Strategies` | 4 | AI guess strategies |
| `DLYH.Audio` | 3 | UI audio system |
| `DLYH.Telemetry` | 1 | Playtest analytics |

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
|   |-- SFXClipGroup.cs                 (~75 lines)
|   |-- UIAudioManager.cs               (~280 lines)
|   +-- UIButtonAudio.cs                (~70 lines)
|
+-- Telemetry/
|   +-- PlaytestTelemetry.cs            (~320 lines)
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
    |-- GameplayUIController.cs         (~1,800 lines)
    |-- GridCellUI.cs                   (~250 lines)
    |-- GuessedWordListController.cs    (~180 lines)
    |-- LetterButton.cs                 (~200 lines)
    |-- MainMenuController.cs           (~150 lines)
    |-- MessagePopup.cs                 (~275 lines)
    |-- FeedbackPanel.cs                (~195 lines)
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

### MainMenuController.cs (~150 lines)
**Namespace:** `DLYH.UI`
**Purpose:** Controls game flow between Main Menu, Setup, and Gameplay phases.

### GameplayUIController.cs (~1,750 lines)
**Namespace:** `TecVooDoo.DontLoseYourHead.UI`
**Purpose:** Master controller for gameplay phase. Manages two PlayerGridPanels, guess processing, turn management, win/lose conditions, and AI opponent integration.

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
| GuessedWordListController | ~180 | Guessed words display |
| SettingsPanel | ~270 | Audio settings |
| FeedbackPanel | ~195 | End-game/menu feedback collection |
| AutocompleteDropdown | ~450 | Word suggestions |
| AutocompleteItem | ~140 | Dropdown entry |

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
```

### AI Turn Flow

```
EndPlayerTurn()
    -> RecordPlayerGuess(wasHit) -> DifficultyAdapter updates skill
    -> TriggerAITurn()
    -> ExecutionerAI.ExecuteTurnAsync()
    -> Wait think time (0.8-2.5s)
    -> BuildAIGameState()
    -> SelectStrategy() based on grid density
    -> Fire OnLetterGuess/OnCoordinateGuess/OnWordGuess event
    -> GameplayUIController.HandleAI*Guess() processes result
    -> EndOpponentTurn()
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
    +--- OnMissCountChanged ---> UI update
    +--- OnGameEnded(playerWon) ---> MainMenuController -> FeedbackPanel
    +--- (subscribes to) ExecutionerAI events

ExecutionerAI
    +--- OnThinkingStarted ---> UI thinking indicator
    +--- OnLetterGuess ---> GameplayUIController.HandleAILetterGuess()
    +--- OnCoordinateGuess ---> GameplayUIController.HandleAICoordinateGuess()
    +--- OnWordGuess ---> GameplayUIController.HandleAIWordGuess()

FeedbackPanel
    +--- OnFeedbackComplete ---> MainMenuController.ShowMainMenu()
```

---

**End of Architecture Document**

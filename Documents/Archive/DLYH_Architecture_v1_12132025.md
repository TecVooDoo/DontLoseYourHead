# Don't Lose Your Head - Architecture Document

**Version:** 1.0  
**Date Created:** December 13, 2025  
**Developer:** TecVooDoo LLC  
**Total Scripts:** 43  

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
                    |                                          +--------+--------+
                    |                                          |                 |
           +--------v--------+                        +--------v----+   +--------v----+
           | PlayerGridPanel |                        | PlayerGrid  |   | PlayerGrid  |
           | (Setup Mode)    |                        | Panel Owner |   | Panel Opp   |
           +--------+--------+                        +-------------+   +-------------+
                    |                                          |                 |
     +--------------+---------------+                 +--------v-----------------v--------+
     |    |    |    |    |    |     |                 |           GuessProcessor          |
     v    v    v    v    v    v     v                 |    (processes guesses for both)   |
  [6 Extracted Controllers]                          +-----------------------------------+
```

---

## Script Catalog (43 Scripts)

### Namespaces

| Namespace | Scripts | Purpose |
|-----------|---------|---------|
| `TecVooDoo.DontLoseYourHead.UI` | 27 | All UI scripts |
| `TecVooDoo.DontLoseYourHead.Core` | 4 | Game state/difficulty |
| `DLYH.UI` | 1 | Main menu (different namespace) |
| `DLYH.AI.Config` | 1 | AI configuration |
| `DLYH.AI.Core` | 4 | AI controllers |
| `DLYH.AI.Data` | 2 | AI data utilities |
| `DLYH.AI.Strategies` | 4 | AI guess strategies |

---

## Layer 1: Main Orchestrators

### MainMenuController.cs (~150 lines)
**Namespace:** `DLYH.UI`  
**Location:** `Scripts/UI/`  
**Purpose:** Controls game flow between Main Menu, Setup, and Gameplay phases.

| Dependencies | Direction |
|--------------|-----------|
| SetupContainer (GameObject) | Reference |
| GameplayContainer (GameObject) | Reference |
| SettingsPanel (GameObject) | Reference |

**Key Methods:**
- `ShowMainMenu()` - Show main menu, hide others
- `StartNewGame()` - Transition to Setup phase
- `ShowSettingsPanel()` / `HideSettingsPanel()`

---

### GameplayUIController.cs (~1,185 lines)
**Namespace:** `TecVooDoo.DontLoseYourHead.UI`  
**Location:** `Scripts/UI/`  
**Purpose:** Master controller for gameplay phase. Manages two PlayerGridPanels, guess processing, turn management, win/lose conditions.

| Dependencies | Direction |
|--------------|-----------|
| PlayerGridPanel (x2) | Owns (Owner + Opponent) |
| SetupSettingsPanel | Reads data from |
| GuessProcessor (x2) | Creates instances |
| WordGuessModeController | Creates instance |
| WordListSO (x4) | Word validation |
| GuessedWordListController (x2) | Owns |
| AutocompleteDropdown | Reference |

**Key Data Structures:**
```csharp
private class SetupData {
    string PlayerName;
    Color PlayerColor;
    int GridSize;
    int WordCount;
    DifficultySetting DifficultyLevel;
    int[] WordLengths;
    List<WordPlacementData> PlacedWords;
}
```

**Key Methods:**
- `StartGameplay()` - Transition from Setup, configure panels
- `ProcessPlayerLetterGuess(char)` - Handle letter guesses
- `ProcessPlayerCoordinateGuess(int, int)` - Handle grid clicks
- `ProcessPlayerWordGuess(string, int)` - Handle word guesses
- `EndPlayerTurn()` / `EndOpponentTurn()` - Turn management

**Events Subscribed:**
- `PlayerGridPanel.OnLetterClicked`
- `PlayerGridPanel.OnCellClicked`
- `WordPatternRow.OnWordGuessStarted/Submitted/Cancelled`

---

### PlayerGridPanel.cs (~1,033 lines)
**Namespace:** `TecVooDoo.DontLoseYourHead.UI`  
**Location:** `Scripts/UI/`  
**Purpose:** Main grid container supporting Setup and Gameplay modes. Manages cells, letter tracker, word rows.

| Dependencies | Direction |
|--------------|-----------|
| GridCellManager | Creates |
| GridLayoutManager | Creates |
| LetterTrackerController | Creates |
| GridColorManager | Creates |
| PlacementPreviewController | Creates |
| CoordinatePlacementController | Creates |
| WordPatternRowManager | Creates |
| GridCellUI (prefab) | Instantiates |
| WordPatternRow[] | Contains |
| AutocompleteDropdown | Reference |

**Modes:**
```csharp
public enum PanelMode { Setup, Gameplay }
```

**Key Methods:**
- `InitializeGrid(int gridSize)` - Create/resize grid (6x6 to 12x12)
- `SetMode(PanelMode)` - Switch between Setup/Gameplay
- `SetWordLengths(int[])` - Configure word rows
- `SelectWordRow(int)` - Select row for input
- `EnterPlacementMode(int)` - Start coordinate placement
- `PlaceAllWordsRandomly()` - Auto-place all words
- `GetCell(int, int)` - Get cell by coordinates

**Events Published:**
- `OnCellClicked(int, int)`
- `OnLetterClicked(char)`
- `OnWordPlaced(int, string, List<Vector2Int>)`
- `OnWordRowSelected(int)`
- `OnCoordinateModeRequested(int)`

---

### SetupSettingsPanel.cs (~1,189 lines)
**Namespace:** `TecVooDoo.DontLoseYourHead.UI`  
**Location:** `Scripts/UI/`  
**Purpose:** Manages setup phase configuration: difficulty settings, player info, word validation.

| Dependencies | Direction |
|--------------|-----------|
| PlayerGridPanel | Reference |
| PlayerColorController | Creates |
| WordValidationService | Creates |
| WordListSO (x4) | Word bank |
| DifficultyCalculator | Uses (static) |
| AutocompleteDropdown | Reference |

**Key Methods:**
- `GetDifficultySettings()` - Returns (gridSize, wordCount, difficulty)
- `GetPlayerSettings()` - Returns (name, color)
- `PickRandomWords()` - Fill empty rows with random words
- `ValidateWord(string, int)` - Check word against bank
- `UpdateStartButtonState()` - Enable when all words placed

**Events Published:**
- `OnGridSizeChanged(int)`
- `OnWordCountChanged(WordCountOption)`
- `OnPlayerSettingsChanged(string, Color)`

---

## Layer 2: Extracted Controllers

### From PlayerGridPanel (6 Controllers)

| Controller | Lines | Purpose |
|------------|-------|---------|
| **GridCellManager.cs** | ~150 | Cell storage and lookup |
| **GridLayoutManager.cs** | ~593 | Grid sizing, label visibility |
| **LetterTrackerController.cs** | ~150 | Letter button management |
| **GridColorManager.cs** | ~50 | Cell color constants |
| **PlacementPreviewController.cs** | ~50 | Preview during placement |
| **CoordinatePlacementController.cs** | ~616 | Placement state machine |
| **WordPatternRowManager.cs** | ~400 | Word row collection management |

### From GameplayUIController (2 Controllers)

| Controller | Lines | Purpose |
|------------|-------|---------|
| **WordGuessModeController.cs** | ~290 | Word guess state machine |
| **WordGuessInputController.cs** | ~290 | Keyboard input during guess |

### From SetupSettingsPanel (2 Controllers)

| Controller | Lines | Purpose |
|------------|-------|---------|
| **PlayerColorController.cs** | ~80 | Color button management |
| **WordValidationService.cs** | ~60 | Word bank validation |

---

## Layer 3: UI Components

### GridCellUI.cs (~250 lines)
**Purpose:** Individual grid cell with states (Empty, Filled, Hit, Miss, PartialHit).

| Dependencies | Direction |
|--------------|-----------|
| Button, Image, TMP_Text | Unity UI |

**States:**
```csharp
public enum CellState { Empty, Filled, Hit, Miss, PartialHit }
```

**Key Methods:**
- `SetState(CellState)` - Update visual state
- `SetLetter(char)` - Set visible letter
- `SetHiddenLetter(char)` - Store but don't show
- `RevealHiddenLetter()` - Show hidden letter

**Events:** `OnCellClicked(int, int)`, `OnCellHoverEnter/Exit`

---

### LetterButton.cs (~200 lines)
**Purpose:** Individual letter tracker button with states.

**States:**
```csharp
public enum LetterState { Normal, Used, Hit, Miss }
```

**Key Methods:**
- `SetState(LetterState)` - Update visual
- `SetInteractable(bool)` - Enable/disable

**Events:** `OnLetterClicked(char)`

---

### WordPatternRow.cs (~1,199 lines)
**Purpose:** Word entry row with pattern display, placement controls, guess mode.

| Dependencies | Direction |
|--------------|-----------|
| WordGuessInputController | Delegates to |
| RowDisplayBuilder | Uses (static) |
| AutocompleteDropdown | Reference |

**Key Properties:**
- `HasWord` - Word entered
- `IsPlaced` - Word placed on grid
- `CurrentWord` - Current word text
- `PlacedStartCol/Row, PlacedDirCol/Row` - Placement data

**Key Methods:**
- `SetRequiredLength(int)` - Configure word length
- `AddLetter(char)` / `DeleteLetter()` - Word entry
- `SetGameplayWord(string)` - Initialize for gameplay
- `EnterWordGuessMode()` / `ExitWordGuessMode()` - Guess mode
- `RevealLetter(int, char)` - Show discovered letter
- `MarkWordSolved()` - Permanently hide guess button

**Events:**
- `OnWordAccepted(int, string)`
- `OnWordGuessStarted/Submitted/Cancelled(int, string)`
- `OnInvalidWordRejected(int, string)`

---

### AutocompleteDropdown.cs (~450 lines)
**Purpose:** Word suggestion dropdown during typing.

**Key Methods:**
- `Show(string prefix, List<string> suggestions)`
- `Hide()`
- `UpdatePosition(RectTransform anchor)`

---

### SettingsPanel.cs (~270 lines)
**Purpose:** Audio settings with PlayerPrefs persistence.

**Key Methods:**
- `GetSFXVolume()` / `GetMusicVolume()` (static)
- `SaveSettings()`

---

## Layer 4: Services & Utilities

### GuessProcessor.cs (~470 lines)
**Location:** `Scripts/UI/Services/`  
**Purpose:** Generic guess processing used by both player and opponent.

**Key Data Structure:**
```csharp
public class WordPlacementData {
    string Word;
    int StartCol, StartRow;
    int DirCol, DirRow;  // Direction (1,0) or (0,1)
    int RowIndex;        // Which word slot (0-3)
}
```

**Key Methods:**
- `ProcessLetterGuess(char)` - Returns GuessResult
- `ProcessCoordinateGuess(int, int)` - Returns GuessResult
- `ProcessWordGuess(string, int)` - Returns GuessResult

**GuessResult Enum:**
```csharp
public enum GuessResult { Hit, Miss, AlreadyGuessed, InvalidWord }
```

---

### WordValidationService.cs (~60 lines)
**Purpose:** Validates words against word bank by length.

---

### RowDisplayBuilder.cs (~207 lines)
**Purpose:** Static utility for building word pattern display text.

---

### GuessedWordListController.cs
**Purpose:** Displays guessed words under guillotines.

---

## Layer 5: Core Data

### DifficultyCalculator.cs
**Location:** `Scripts/Core/GameState/`  
**Purpose:** Static methods for difficulty calculations.

**Key Methods:**
- `GetWordLengths(WordCountOption)` - Returns int[] of lengths
- `CalculateMissLimit(...)` - Calculate miss limit from settings

---

### DifficultySO.cs
**Purpose:** ScriptableObject for difficulty presets.

---

### DifficultyEnums.cs
**Purpose:** Enum definitions.

```csharp
public enum DifficultySetting { Easy, Normal, Hard }
public enum WordCountOption { Three = 0, Four = 1 }
```

---

### WordListSO.cs
**Purpose:** ScriptableObject containing word lists.

**Key Methods:**
- `Contains(string)` - Check if word exists
- `GetRandomWord()` - Get random word

---

## Layer 6: AI System (Phase 3)

### ExecutionerConfigSO.cs
**Location:** `Scripts/AI/Config/`  
**Purpose:** ScriptableObject with all AI tuning parameters.

---

### ExecutionerAI.cs
**Location:** `Scripts/AI/Core/`  
**Purpose:** MonoBehaviour that coordinates AI turns.

| Dependencies | Direction |
|--------------|-----------|
| ExecutionerConfigSO | Configuration |
| DifficultyAdapter | Creates |
| MemoryManager | Creates |
| IGuessStrategy implementations | Uses |
| GridAnalyzer | Uses |

---

### DifficultyAdapter.cs
**Purpose:** Rubber-banding and adaptive threshold tracking.

---

### MemoryManager.cs
**Purpose:** Skill-based memory filtering.

---

### AISetupManager.cs
**Purpose:** AI word selection and placement.

| Dependencies | Direction |
|--------------|-----------|
| WordPlacementData | Uses (from GuessProcessor) |
| WordListSO | Word selection |

---

### Strategies

| Strategy | Purpose |
|----------|---------|
| **IGuessStrategy.cs** | Interface |
| **LetterGuessStrategy.cs** | Frequency-based letter selection |
| **CoordinateGuessStrategy.cs** | Adjacency-based cell selection |
| **WordGuessStrategy.cs** | Confidence-based word guessing |

### Data Utilities

| Utility | Purpose |
|---------|---------|
| **LetterFrequency.cs** | English letter frequencies |
| **GridAnalyzer.cs** | Grid density calculations |

---

## Data Flow Diagrams

### Setup -> Gameplay Transition

```
SetupSettingsPanel                 GameplayUIController
      |                                   |
      | GetDifficultySettings()           |
      | GetPlayerSettings()               |
      +---------------------------------->|
                                          | CaptureSetupData()
PlayerGridPanel (Setup)                   |
      |                                   |
      | GetWordPatternRows()              |
      | .CurrentWord, .PlacedStartCol...  |
      +---------------------------------->|
                                          | Creates SetupData with
                                          | List<WordPlacementData>
                                          |
                                          | GenerateOpponentData()
                                          |   (AI words - hardcoded for now)
                                          |
                                          | ConfigureOwnerPanel()
                                          | ConfigureOpponentPanel()
                                          |
                                    +-----v-----+
                                    | Gameplay  |
                                    | Phase     |
                                    +-----------+
```

### Player Guess Flow

```
User clicks opponent grid cell
           |
           v
PlayerGridPanel.OnCellClicked(col, row)
           |
           v
GameplayUIController.HandleCellGuess(col, row)
           |
           v
ProcessPlayerCoordinateGuess(col, row)
           |
           v
_playerGuessProcessor.ProcessCoordinateGuess(col, row)
           |
           +---> Updates _opponentPanel cells
           +---> Updates letter tracker states
           +---> Returns GuessResult
           |
           v
EndPlayerTurn() if not AlreadyGuessed
```

### Word Guess Mode Flow

```
User clicks "Guess Word" button
           |
           v
WordPatternRow.OnGuessWordClicked()
           |
           v
WordGuessModeController.HandleWordGuessStarted(rowIndex, pattern)
           |
           v
EnterKeyboardMode()
  - Set letter tracker to white (keyboard mode)
  - Track active row
           |
           v
User types letters (keyboard or clicks)
           |
           v
HandleKeyboardLetterInput(letter)
  - Add to typed letters
  - Update row display
           |
           v
User clicks "Accept"
           |
           v
HandleWordGuessSubmitted(rowIndex, guess)
           |
           v
ProcessWordGuessForController(word, rowIndex)
           |
           v
_playerGuessProcessor.ProcessWordGuess(word, rowIndex)
           |
           +---> Returns Hit/Miss/Invalid
           |
           v
If Hit: MarkWordSolved(), update cells
If Miss: +2 misses
If Invalid: Show feedback, no penalty
```

---

## Event Architecture

### Publisher -> Subscriber Map

| Event | Publisher | Subscribers |
|-------|-----------|-------------|
| `OnCellClicked` | GridCellUI | PlayerGridPanel |
| `OnCellClicked` | PlayerGridPanel | GameplayUIController |
| `OnLetterClicked` | LetterButton | LetterTrackerController |
| `OnLetterClicked` | PlayerGridPanel | GameplayUIController |
| `OnWordPlaced` | CoordinatePlacementController | PlayerGridPanel |
| `OnWordPlaced` | PlayerGridPanel | SetupSettingsPanel |
| `OnWordRowSelected` | WordPatternRow | WordPatternRowManager |
| `OnWordRowSelected` | PlayerGridPanel | SetupSettingsPanel |
| `OnWordAccepted` | WordPatternRow | SetupSettingsPanel |
| `OnWordGuessStarted` | WordPatternRow | WordGuessModeController |
| `OnWordGuessSubmitted` | WordPatternRow | WordGuessModeController |
| `OnInvalidWordRejected` | WordPatternRow | SetupSettingsPanel |
| `OnGridSizeChanged` | SetupSettingsPanel | PlayerGridPanel (indirect) |

---

## Interfaces

### IGridControllers.cs
```csharp
// Location: Scripts/UI/Interfaces/
// Contains interface definitions for grid controllers
```

---

## Key Patterns Used

### 1. Defensive Controller Initialization
```csharp
private void EnsureControllersInitialized() {
    if (_gridCellManager != null) return;
    // Initialize controllers...
    WireControllerEventsIfNeeded();
}
```

### 2. Callback Injection for Services
```csharp
_playerGuessProcessor = new GuessProcessor(
    targetWords,
    targetPanel,
    "Player",
    () => { _playerMisses++; UpdateCounter(); },  // onMissIncrement
    (letter, state) => panel.SetLetterState(...), // setLetterState
    word => IsValidWord(word),                     // validateWord
    (word, correct) => list.AddGuessedWord(...)   // onWordGuessed
);
```

### 3. Permanent State Pattern
```csharp
private bool _wordSolved = false;

public void MarkWordSolved() {
    _wordSolved = true;
    HideGuessWordButton();
}

public void ShowGuessWordButton() {
    if (_wordSolved) return; // Never show after solved
    // ...
}
```

---

## File Locations Summary

```
Assets/DLYH/Scripts/
|-- AI/
|   |-- Config/
|   |   +-- ExecutionerConfigSO.cs
|   |-- Core/
|   |   |-- AISetupManager.cs
|   |   |-- DifficultyAdapter.cs
|   |   |-- ExecutionerAI.cs
|   |   +-- MemoryManager.cs
|   |-- Data/
|   |   |-- GridAnalyzer.cs
|   |   +-- LetterFrequency.cs
|   +-- Strategies/
|       |-- CoordinateGuessStrategy.cs
|       |-- IGuessStrategy.cs
|       |-- LetterGuessStrategy.cs
|       +-- WordGuessStrategy.cs
|-- Core/
|   +-- GameState/
|       |-- DifficultyCalculator.cs
|       |-- DifficultyEnums.cs
|       |-- DifficultySO.cs
|       +-- WordListSO.cs
|-- Editor/
|   +-- WordBankImporter.cs
+-- UI/
    |-- AutocompleteDropdown.cs
    |-- AutocompleteItem.cs
    |-- GameplayUIController.cs
    |-- GridCellUI.cs
    |-- GuessedWordListController.cs
    |-- LetterButton.cs
    |-- MainMenuController.cs
    |-- PlayerGridPanel.cs
    |-- SettingsPanel.cs
    |-- SetupModeController.cs
    |-- SetupSettingsPanel.cs
    |-- WordPatternRow.cs
    |-- Controllers/
    |   |-- AutocompleteManager.cs
    |   |-- CoordinatePlacementController.cs
    |   |-- GridCellManager.cs
    |   |-- GridColorManager.cs
    |   |-- GridLayoutManager.cs
    |   |-- LetterTrackerController.cs
    |   |-- PlacementPreviewController.cs
    |   |-- PlayerColorController.cs
    |   |-- WordGuessInputController.cs
    |   |-- WordGuessModeController.cs
    |   |-- WordPatternController.cs
    |   +-- WordPatternRowManager.cs
    |-- Interfaces/
    |   +-- IGridControllers.cs
    |-- Services/
    |   |-- GuessProcessor.cs
    |   +-- WordValidationService.cs
    +-- Utilities/
        +-- RowDisplayBuilder.cs
```

---

## AI Integration Points

When integrating ExecutionerAI, it needs to:

1. **Receive game state from GameplayUIController:**
   - Player's placed words (from `_playerSetupData.PlacedWords`)
   - Current known letters, guessed coordinates
   - Miss counts

2. **Call existing guess processing:**
   - `ProcessOpponentLetterGuess(char)` 
   - `ProcessOpponentCoordinateGuess(int, int)`
   - `ProcessOpponentWordGuess(string, int)`

3. **Use GuessProcessor's GuessResult:**
   - Same enum used by player and AI

4. **Call turn management:**
   - `EndOpponentTurn()` after AI makes guess

---

**End of Architecture Document**

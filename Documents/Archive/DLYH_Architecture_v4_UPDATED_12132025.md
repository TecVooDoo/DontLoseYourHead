# Don't Lose Your Head - Architecture Document

**Version:** 3.2
**Date Created:** December 13, 2025
**Last Updated:** December 13, 2025
**Developer:** TecVooDoo LLC
**Total Scripts:** 36 UI + 11 AI = 47 Scripts

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
                    |                                          |        |        |
           +--------v--------+                        +--------v--+ +---v----+ +-v---------+
           | PlayerGridPanel |                        | PlayerGrid| |Execu-  | |GuessProc- |
           | (Setup Mode)    |                        | Panel (x2)| |tionerAI| |essor (x2) |
           +--------+--------+                        +-----------+ +--------+ +-----------+
                    |
     +--------------+---------------+
     |    |    |    |    |    |     |
     v    v    v    v    v    v     v
  [Extracted Controllers + Interfaces]
```

---

## Script Catalog (45 Scripts)

### Namespaces

| Namespace | Scripts | Purpose |
|-----------|---------|---------|
| `TecVooDoo.DontLoseYourHead.UI` | 23 | Main UI scripts |
| `TecVooDoo.DontLoseYourHead.UI.Utilities` | 1 | RowDisplayBuilder |
| `TecVooDoo.DontLoseYourHead.Core` | 4 | Game state/difficulty |
| `DLYH.UI` | 1 | Main menu |
| `DLYH.AI.Config` | 1 | AI configuration |
| `DLYH.AI.Core` | 4 | AI controllers |
| `DLYH.AI.Data` | 2 | AI data utilities |
| `DLYH.AI.Strategies` | 4 | AI guess strategies |

---

## Layer 0: Interfaces (IGridControllers.cs)

### IGridControllers.cs (~115 lines)
**Namespace:** `TecVooDoo.DontLoseYourHead.UI`  
**Location:** `Scripts/UI/Interfaces/`  
**Purpose:** Defines contracts for all extracted grid controllers. Enables loose coupling and testability.

This file contains 5 interfaces and 2 enums:

---

### IGridDisplayController Interface

**Purpose:** Contract for grid display operations (cell creation, sizing, labels).

```csharp
public interface IGridDisplayController
{
    int CurrentGridSize { get; }
    bool IsInitialized { get; }
    
    void Initialize(int gridSize);
    void SetGridSize(int newSize);
    void ClearGrid();
    GridCellUI GetCell(int column, int row);
    bool IsValidCoordinate(int column, int row);
    char GetColumnLetter(int column);
    
    event Action<int, int> OnCellClicked;
    event Action<int, int> OnCellHoverEnter;
    event Action<int, int> OnCellHoverExit;
}
```

**Implementations:** GridLayoutManager (partial), GridCellManager (partial)

---

### ILetterTrackerController Interface

**Purpose:** Contract for letter tracker/keyboard operations.

```csharp
public interface ILetterTrackerController
{
    void CacheLetterButtons();
    LetterButton GetLetterButton(char letter);
    void SetLetterState(char letter, LetterButton.LetterState state);
    void ResetAllLetterButtons();
    void SetLetterButtonsInteractable(bool interactable);
    
    event Action<char> OnLetterClicked;
    event Action<char> OnLetterHoverEnter;
    event Action<char> OnLetterHoverExit;
}
```

**Implementations:** LetterTrackerController

---

### IWordPatternController Interface

**Purpose:** Contract for word pattern row management.

```csharp
public interface IWordPatternController
{
    int WordRowCount { get; }
    int SelectedWordRowIndex { get; }
    
    void CacheWordPatternRows();
    WordPatternRow GetWordPatternRow(int index);
    WordPatternRow[] GetWordPatternRows();
    void SelectWordRow(int index);
    bool AddLetterToSelectedRow(char letter);
    bool RemoveLastLetterFromSelectedRow();
    void SetWordLengths(int[] lengths);
    void SetWordValidator(Func<string, int, bool> validator);
    bool AreAllWordsPlaced();
    bool ClearPlacedWord(int rowIndex);
    
    event Action<int> OnWordRowSelected;
    event Action<int> OnCoordinateModeRequested;
    event Action<int, string, List<Vector2Int>> OnWordPlaced;
}
```

**Implementations:** WordPatternController, WordPatternRowManager (partial)

---

### ICoordinatePlacementController Interface

**Purpose:** Contract for coordinate placement mode operations.

```csharp
public interface ICoordinatePlacementController
{
    bool IsInPlacementMode { get; }
    PlacementState CurrentPlacementState { get; }
    
    void EnterPlacementMode(int wordRowIndex, string word);
    void CancelPlacementMode();
    bool PlaceWordRandomly();
    void UpdatePlacementPreview(int hoverCol, int hoverRow);
    void ClearPlacementHighlighting();
    bool HandleCellClick(int column, int row);
    
    event Action OnPlacementCancelled;
    event Action<int, string, List<Vector2Int>> OnWordPlaced;
}
```

**Implementations:** CoordinatePlacementController

---

### IGridColorManager Interface

**Purpose:** Contract for grid color/highlighting operations.

```csharp
public interface IGridColorManager
{
    Color CursorColor { get; set; }
    Color ValidPlacementColor { get; set; }
    Color InvalidPlacementColor { get; set; }
    Color PlacedLetterColor { get; set; }
    
    void SetCellHighlight(GridCellUI cell, GridHighlightType highlightType);
    void ClearCellHighlight(GridCellUI cell);
    void ClearAllHighlights(IGridDisplayController gridDisplay);
}
```

**Implementations:** GridColorManager

---

### GridHighlightType Enum

**Purpose:** Types of cell highlighting during placement mode.

```csharp
public enum GridHighlightType
{
    None,
    Cursor,           // Current hover cell
    ValidPlacement,   // Word can be placed here
    InvalidPlacement, // Word cannot be placed here
    PlacedLetter      // Letter already placed
}
```

---

### PlacementState Enum

**Purpose:** State of coordinate placement mode.

```csharp
public enum PlacementState
{
    Inactive,
    SelectingFirstCell,
    SelectingDirection
}
```

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

### GameplayUIController.cs (~1,700 lines)
**Namespace:** `TecVooDoo.DontLoseYourHead.UI`  
**Location:** `Scripts/UI/`  
**Purpose:** Master controller for gameplay phase. Manages two PlayerGridPanels, guess processing, turn management, win/lose conditions, and AI opponent integration.

| Dependencies | Direction |
|--------------|-----------|
| PlayerGridPanel (x2) | Owns (Owner + Opponent) |
| SetupSettingsPanel | Reads data from |
| GuessProcessor (x2) | Creates instances |
| WordGuessModeController | Creates instance |
| GameplayStateTracker | Creates instance |
| WinConditionChecker | Creates instance |
| WordListSO (x4) | Word validation |
| GuessedWordListController (x2) | Owns |
| ExecutionerAI | Reference (AI opponent) |
| ExecutionerConfigSO | Configuration |

**Key Data Structures:**
```csharp
private class SetupData {
    string PlayerName;
    Color PlayerColor;
    int GridSize;
    int WordCount;
    DifficultySetting DifficultyLevel;
    int MissLimit;
    List<WordPlacementData> PlacedWords;
}
```

**Events Published:**
```csharp
public event Action<int> OnMissCountChanged;  // Player miss count
public event Action<int> OnOpponentMissCountChanged;
public event Action OnGameOver;
```

**Key Methods:**

| Method | Purpose | Returns |
|--------|---------|---------|
| `StartGameplay()` | Initialize both panels from setup data | void |
| `ProcessLetterGuess(char)` | Player guesses letter against opponent | void |
| `ProcessCoordinateGuess(int, int)` | Player guesses cell | void |
| `ProcessWordGuess(string, int)` | Player guesses complete word | void |
| `EndPlayerTurn()` | Switch to opponent's turn, trigger AI | void |
| `EndOpponentTurn()` | Switch to player's turn | void |
| `CheckWinCondition()` | Check if either player won | bool |

**AI Integration Methods (Dec 13, 2025):**

| Method | Purpose | Returns |
|--------|---------|---------|
| `WireAIEvents()` | Subscribe to ExecutionerAI events | void |
| `HandleAILetterGuess(char)` | Process AI letter guess | void |
| `HandleAICoordinateGuess(int, int)` | Process AI coordinate guess | void |
| `HandleAIWordGuess(string, int)` | Process AI word guess | void |
| `BuildAIGameState()` | Create AIGameState for AI decisions | AIGameState |
| `TriggerAITurn()` | Start AI turn after player ends | void |

**Initialization Flow:**
```
StartGameplay()
    -> CaptureSetupData() for player
    -> GenerateAISetupData() using AISetupManager
    -> ConfigureOwnerPanel() 
    -> ConfigureOpponentPanel()
    -> CreateGuessProcessors()
    -> WireAIEvents()
    -> StartFirstTurn()
```

**AI Event Flow:**
```
EndPlayerTurn()
    -> RecordPlayerGuess(wasHit) for rubber-banding
    -> TriggerAITurn()
    -> ExecutionerAI.ExecuteTurnAsync()
    -> AI fires OnLetterGuess/OnCoordinateGuess/OnWordGuess
    -> HandleAI*Guess() processes result
    -> EndOpponentTurn()
```

---

### SetupSettingsPanel.cs (~800 lines)
**Namespace:** `TecVooDoo.DontLoseYourHead.UI`  
**Location:** `Scripts/UI/`  
**Purpose:** Manages player configuration during setup phase: name, color, grid size, word count, difficulty. Creates word validator and manages word entry.

| Dependencies | Direction |
|--------------|-----------|
| PlayerGridPanel | Sibling reference |
| WordListSO (x4) | Word validation |
| DifficultySO | Miss limit calculation |
| PlayerColorController | Owns |
| WordValidationService | Owns |

**Key Properties Exposed:**
```csharp
public string PlayerName { get; }
public Color PlayerColor { get; }
public int GridSize { get; }           // 6-12
public int WordCount { get; }          // 3 or 4
public DifficultySetting Difficulty { get; }
public int MissLimit { get; }
```

**Events Published:**
```csharp
public event Action<int> OnGridSizeChanged;
public event Action<int> OnWordCountChanged;
public event Action OnSetupComplete;
```

**Word Entry Flow:**
```
User types letter
    -> WordPatternRow.AddLetter()
    -> If complete: WordValidationService.Validate()
    -> If valid: Enable compass button
    -> User clicks compass -> Enter placement mode
    -> User places word -> MarkAsPlaced()
```

---

### PlayerGridPanel.cs (~1,120 lines)
**Namespace:** `TecVooDoo.DontLoseYourHead.UI`  
**Location:** `Scripts/UI/`  
**Purpose:** Manages a single player's grid display. Used in both Setup (word placement) and Gameplay (guessing). Contains most extracted controllers.

| Dependencies | Direction |
|--------------|-----------|
| GridLayoutManager | Owns |
| GridCellManager | Owns |
| LetterTrackerController | Owns |
| GridColorManager | Owns |
| PlacementPreviewController | Owns |
| CoordinatePlacementController | Owns |
| WordPatternRowManager | Owns |

**Modes:**
```csharp
public enum PanelMode
{
    Setup,      // Word entry and placement
    Gameplay    // Guessing (as owner or opponent view)
}
```

**Key Methods:**

| Method | Purpose |
|--------|---------|
| `InitializeGrid(int size)` | Create grid cells |
| `SetMode(PanelMode)` | Switch between modes |
| `ConfigureAsOwner(SetupData)` | Show own words revealed |
| `ConfigureAsOpponent(SetupData)` | Show opponent's grid hidden |
| `EnterPlacementMode(int, string)` | Start placing a word |
| `RevealLetter(char)` | Show letter in patterns |
| `RevealCoordinate(int, int)` | Show cell contents |

**Controller Initialization Pattern:**
```csharp
private void EnsureControllersInitialized()
{
    if (_gridLayoutManager != null) return;
    
    _gridLayoutManager = new GridLayoutManager(...);
    _gridCellManager = new GridCellManager();
    _letterTrackerController = new LetterTrackerController(...);
    // ... etc
    
    WireControllerEvents();
}
```

---

## Layer 2: Extracted Controllers

### WordPatternController.cs (~285 lines)
**Namespace:** `TecVooDoo.DontLoseYourHead.UI`  
**Location:** `Scripts/UI/Controllers/`  
**Purpose:** Manages word pattern rows for word entry and display. Implements `IWordPatternController`. Plain C# class that receives container reference via constructor.

**Architecture Pattern:** Plain C# class implementing interface. Event-driven communication.

| Dependencies | Direction |
|--------------|-----------|
| Transform (container) | Injected via constructor |
| WordPatternRow | Finds in children |
| AutocompleteDropdown | Optional, injected |
| Func<string, int, bool> | Word validator callback |

**Events Published:**
```csharp
public event Action<int> OnWordRowSelected;
public event Action<int> OnCoordinateModeRequested;
public event Action<int, string, List<Vector2Int>> OnWordPlaced;
public event Action<int, bool> OnDeleteClicked;
```

**Properties:**
```csharp
public int WordRowCount => _wordPatternRows.Count;
public int SelectedWordRowIndex => _selectedWordRowIndex;
```

**Key Methods:**

| Method | Purpose | Returns |
|--------|---------|---------|
| `CacheWordPatternRows()` | Find and subscribe to rows | void |
| `GetWordPatternRow(int)` | Get row by index | WordPatternRow |
| `GetWordPatternRows()` | Get all rows | WordPatternRow[] |
| `SelectWordRow(int)` | Select row for input | void |
| `AddLetterToSelectedRow(char)` | Add letter to active row | bool |
| `RemoveLastLetterFromSelectedRow()` | Backspace | bool |
| `SetWordLengths(int[])` | Configure required lengths | void |
| `SetWordValidator(Func)` | Set validation callback | void |
| `AreAllWordsPlaced()` | Check completion | bool |
| `ClearPlacedWord(int)` | Reset row for re-entry | bool |
| `GetWordAtRow(int)` | Get current word | string |
| `HasWordAtRow(int)` | Check if has word | bool |
| `IsRowPlaced(int)` | Check if placed on grid | bool |
| `MarkRowAsPlaced(int)` | Mark row complete | void |
| `ResetRowToWordEntered(int)` | Keep word, clear placement | void |
| `Dispose()` | Cleanup subscriptions | void |

**Row Number Conversion:**
Note: WordPatternRow uses 1-indexed row numbers, but controller uses 0-indexed:
```csharp
private void HandleWordRowSelected(int rowNumber)
{
    int index = rowNumber - 1;  // Convert to 0-indexed
    SelectWordRow(index);
}
```

**Caching Pattern:**
```csharp
public void CacheWordPatternRows()
{
    _wordPatternRows.Clear();
    var rows = _container.GetComponentsInChildren<WordPatternRow>(true);
    
    foreach (var row in rows)
    {
        // Unsubscribe first to prevent duplicates
        row.OnRowSelected -= HandleWordRowSelected;
        row.OnRowSelected += HandleWordRowSelected;
        // ... other events
        
        if (_wordValidator != null)
            row.SetWordValidator(_wordValidator);
            
        _wordPatternRows.Add(row);
    }
}
```

**Usage (from SetupSettingsPanel or PlayerGridPanel):**
```csharp
_wordPatternController = new WordPatternController(
    _wordPatternsContainer, _autocompleteDropdown);
_wordPatternController.CacheWordPatternRows();
_wordPatternController.OnWordRowSelected += HandleRowSelected;
_wordPatternController.OnCoordinateModeRequested += HandleCoordinateMode;
```

---

### GridLayoutManager.cs (~600 lines)
**Namespace:** `TecVooDoo.DontLoseYourHead.UI`  
**Location:** `Scripts/UI/Controllers/`  
**Purpose:** Handles grid cell creation, sizing, and label management. Calculates dynamic cell sizes based on available space.

**Architecture Pattern:** Plain C# class with injected UI references.

| Dependencies | Direction |
|--------------|-----------|
| RectTransform (gridContainer) | Injected |
| RectTransform (rowLabels) | Injected |
| RectTransform (columnLabels) | Injected |
| GridCellUI prefab | Injected |

**Events Published:**
```csharp
public event Action<GridCellUI, int, int> OnCellCreated;
```

**Key Methods:**

| Method | Purpose | Returns |
|--------|---------|---------|
| `CreateCellsForCurrentSize(int, GridCellUI[,])` | Generate grid cells | void |
| `CalculateCellSize(int)` | Dynamic sizing | float |
| `UpdateLabels(int)` | Row/column labels | void |
| `ClearGrid(GridCellUI[,])` | Destroy all cells | void |

**Cell Size Calculation:**
```csharp
public float CalculateCellSize(int gridSize)
{
    float availableWidth = _gridContainer.rect.width - (gridSize + 1) * _spacing;
    float availableHeight = _gridContainer.rect.height - (gridSize + 1) * _spacing;
    return Mathf.Min(availableWidth / gridSize, availableHeight / gridSize);
}
```

**Cell Creation Loop:**
```csharp
for (int row = 0; row < gridSize; row++)
    for (int col = 0; col < gridSize; col++)
        CreateCell(col, row, cells);
```

---

### LetterTrackerController.cs (~175 lines)
**Namespace:** `TecVooDoo.DontLoseYourHead.UI`  
**Location:** `Scripts/UI/Controllers/`  
**Purpose:** Manages the A-Z letter tracker/keyboard buttons. Handles caching, state changes, and event forwarding.

**Architecture Pattern:** Plain C# class implementing `ILetterTrackerController`. Event-driven communication.

| Dependencies | Direction |
|--------------|-----------|
| Transform (container) | Injected |
| LetterButton | Finds in children |
| Dictionary<char, LetterButton> | Internal cache |

**Events Published:**
```csharp
public event Action<char> OnLetterClicked;
public event Action<char> OnLetterHoverEnter;
public event Action<char> OnLetterHoverExit;
```

**Key Methods:**

| Method | Purpose | Returns |
|--------|---------|---------|
| `CacheLetterButtons()` | Find and subscribe to all LetterButtons | void |
| `GetLetterButton(char)` | Get button by letter | LetterButton |
| `GetLetterState(char)` | Get current state | LetterState |
| `SetLetterState(char, LetterState)` | Update visual state | void |
| `ResetAllLetterButtons()` | Set all to Normal | void |
| `SetLetterButtonsInteractable(bool)` | Enable/disable all | void |
| `Dispose()` | Unsubscribe all events | void |

---

### GridColorManager.cs (~130 lines)
**Namespace:** `TecVooDoo.DontLoseYourHead.UI`  
**Location:** `Scripts/UI/Controllers/`  
**Purpose:** Centralizes grid cell color definitions and highlighting operations. Implements `IGridColorManager`.

**Architecture Pattern:** Plain C# class with color state.

| Dependencies | Direction |
|--------------|-----------|
| GridCellUI | Operates on |
| IGridDisplayController | Uses for bulk operations |

**Color Properties:**
```csharp
public Color CursorColor { get; set; }         // Stoplight green
public Color ValidPlacementColor { get; set; } // Light mint green
public Color InvalidPlacementColor { get; set; } // Red
public Color PlacedLetterColor { get; set; }   // Light blue
```

**Default Colors:**
```csharp
_cursorColor = new Color(0.13f, 0.85f, 0.13f, 1f);      // Stoplight green
_validPlacementColor = new Color(0.6f, 1f, 0.6f, 0.8f); // Light mint
_invalidPlacementColor = new Color(1f, 0f, 0f, 0.7f);   // Red
_placedLetterColor = new Color(0.5f, 0.8f, 1f, 1f);     // Light blue
```

**Key Methods:**

| Method | Purpose |
|--------|---------|
| `SetCellHighlight(GridCellUI, GridHighlightType)` | Apply highlight color |
| `ClearCellHighlight(GridCellUI)` | Remove highlight |
| `ClearAllHighlights(IGridDisplayController)` | Clear entire grid |
| `GetColorForType(GridHighlightType)` | Get color for type |

---

### PlacementPreviewController.cs (~200 lines)
**Namespace:** `TecVooDoo.DontLoseYourHead.UI`  
**Location:** `Scripts/UI/Controllers/`  
**Purpose:** Handles visual preview during word placement - shows where word will go, highlights valid/invalid directions.

**Architecture Pattern:** Stateless helper class. All state passed via method parameters.

| Dependencies | Direction |
|--------------|-----------|
| IGridColorManager | Injected |
| Func<int, int, GridCellUI> | Injected |
| Func<int, int, bool> | Injected |

**Constructor (Dependency Injection via Funcs):**
```csharp
public PlacementPreviewController(
    IGridColorManager colorManager,
    Func<int, int, GridCellUI> getCell,
    Func<int, int, bool> isValidCoordinate)
```

**Key Methods:**

| Method | Purpose |
|--------|---------|
| `ShowPreview(startCol, startRow, word, isHorizontal)` | Display placement preview |
| `ClearPreview()` | Remove preview highlighting |
| `CanPlaceWord(...)` | Check if placement is valid |
| `GetPlacementCells(...)` | Get cells for placement |

---

### CoordinatePlacementController.cs (~620 lines)
**Namespace:** `TecVooDoo.DontLoseYourHead.UI`  
**Location:** `Scripts/UI/Controllers/`  
**Purpose:** State machine for word placement mode. Handles the two-click placement flow (first cell, then direction).

**Architecture Pattern:** Implements `ICoordinatePlacementController`. State machine with PlacementState enum.

| Dependencies | Direction |
|--------------|-----------|
| GridCellManager | Injected |
| IGridColorManager | Injected |
| PlacementPreviewController | Owns |
| Func delegates | For grid access |

**Events Published:**
```csharp
public event Action OnPlacementCancelled;
public event Action<int, string, List<Vector2Int>> OnWordPlaced;
```

**State Machine:**
```
Inactive -> SelectingFirstCell -> SelectingDirection -> Inactive
              (click cell)         (click direction)
                   |                      |
                   +-------(cancel)-------+
```

**Key Methods:**

| Method | Purpose | Returns |
|--------|---------|---------|
| `EnterPlacementMode(wordIndex, word)` | Start placement | void |
| `CancelPlacementMode()` | Exit without placing | void |
| `HandleCellClick(col, row)` | Process click | bool |
| `PlaceWordRandomly()` | Auto-place word | bool |
| `UpdatePlacementPreview(col, row)` | Show preview on hover | void |

**Placement Flow:**
```
EnterPlacementMode("CAT", 0)
    -> State = SelectingFirstCell
    -> User hovers cells -> UpdatePlacementPreview()
    -> User clicks cell -> State = SelectingDirection
    -> Show horizontal/vertical options
    -> User clicks direction -> PlaceWord()
    -> Fire OnWordPlaced event
    -> State = Inactive
```

---

### WordPatternRowManager.cs (~400 lines)
**Namespace:** `TecVooDoo.DontLoseYourHead.UI`  
**Location:** `Scripts/UI/Controllers/`  
**Purpose:** Manages collection of WordPatternRow components for a panel. Handles row selection, word lengths, and validation delegation.

| Dependencies | Direction |
|--------------|-----------|
| WordPatternRow[] | Manages |
| Func<string, int, bool> | Word validator callback |

**Key Methods:**

| Method | Purpose |
|--------|---------|
| `CacheWordPatternRows()` | Find rows in hierarchy |
| `SetWordLengths(int[])` | Configure row lengths |
| `SelectWordRow(int)` | Set active row |
| `GetPlacedWords()` | Get all placed word data |

---

### GridCellManager.cs (~150 lines)
**Namespace:** `TecVooDoo.DontLoseYourHead.UI`  
**Location:** `Scripts/UI/Controllers/`  
**Purpose:** Manages the 2D array of GridCellUI components. Provides cell lookup and iteration.

**Key Properties:**
```csharp
public GridCellUI[,] Cells { get; }
public int GridSize { get; }
```

**Key Methods:**

| Method | Purpose |
|--------|---------|
| `GetCell(col, row)` | Get specific cell |
| `IsValidCoordinate(col, row)` | Bounds check |
| `ClearAllCells()` | Reset all cells |

---

### WordGuessModeController.cs (~290 lines)
**Namespace:** `TecVooDoo.DontLoseYourHead.UI`  
**Location:** `Scripts/UI/Controllers/`  
**Purpose:** State machine for word guess mode during gameplay. Manages the UI flow when player clicks "Guess Word" on a row.

**Events Published:**
```csharp
public event Action<int> OnWordGuessModeEntered;  // rowIndex
public event Action OnWordGuessModeExited;
public event Action<string, int> OnWordGuessSubmitted;  // word, rowIndex
```

**State Machine:**
```
Normal -> WordGuessActive -> Normal
  (click Guess Word)  (Accept/Cancel)
```

---

### WordGuessInputController.cs (~290 lines)
**Namespace:** `TecVooDoo.DontLoseYourHead.UI`  
**Location:** `Scripts/UI/Controllers/`  
**Purpose:** Handles keyboard input during word guess mode. Converts letter tracker to keyboard mode.

---

### PlayerColorController.cs (~80 lines)
**Namespace:** `TecVooDoo.DontLoseYourHead.UI`  
**Location:** `Scripts/UI/Controllers/`  
**Purpose:** Manages the 8-button color picker. Handles selection state and outline highlighting.

**Events Published:**
```csharp
public event Action<Color> OnColorSelected;
```

---

## Layer 3: Services and Utilities

### GuessProcessor.cs (~400 lines)
**Namespace:** `TecVooDoo.DontLoseYourHead.UI`  
**Location:** `Scripts/UI/Services/`  
**Purpose:** Generic guess processing used for both player and AI guesses. Determines hit/miss, updates game state.

**Architecture Pattern:** Service with callback injection. Same instance processes guesses against one player's grid.

**Constructor (Updated Dec 13, 2025):**
```csharp
public GuessProcessor(
    List<WordPlacementData> targetWords,
    PlayerGridPanel targetPanel,
    Action<int> onMissIncrement,        // Changed from Action to Action<int>
    Action<char, LetterState> setLetterState,
    Func<string, bool> validateWord)
```

**Note:** The `onMissIncrement` callback now takes an `int` parameter to support variable miss counts. Wrong word guesses count as 2 misses, while letter/coordinate misses count as 1.

**GuessResult Enum:**
```csharp
public enum GuessResult
{
    Hit,
    Miss,
    AlreadyGuessed,
    InvalidWord,
    WordCorrect,
    WordIncorrect
}
```

**Key Methods:**

| Method | Purpose | Returns | Miss Count |
|--------|---------|---------|------------|
| `ProcessLetterGuess(char)` | Check if letter in words | GuessResult | 1 on miss |
| `ProcessCoordinateGuess(row, col)` | Check if cell has letter | GuessResult | 1 on miss |
| `ProcessWordGuess(word, rowIndex)` | Validate and check word | GuessResult | 2 on incorrect |

---

### WordValidationService.cs (~60 lines)
**Namespace:** `TecVooDoo.DontLoseYourHead.UI`
**Location:** `Scripts/UI/Services/`
**Purpose:** Validates words against loaded WordListSO dictionaries.

**Key Methods:**
```csharp
public bool IsValidWord(string word, int length);
public void LoadWordLists(WordListSO word3, WordListSO word4, ...);
```

---

### GameplayStateTracker.cs (~300 lines)
**Namespace:** `TecVooDoo.DontLoseYourHead.UI`
**Location:** `Scripts/UI/Services/`
**Purpose:** Tracks gameplay state for both player and opponent. Extracted from GameplayUIController to reduce file size.

**Architecture Pattern:** Plain C# class with state encapsulation.

**Tracked State:**
- Miss counts and limits (player/opponent)
- Known letters (HashSet<char>)
- Guessed letters (HashSet<char>)
- Guessed coordinates (HashSet<Vector2Int>)
- Guessed words (HashSet<string>)
- Solved word rows (HashSet<int>)
- Turn state (IsPlayerTurn, GameOver)

**Key Methods:**

| Method | Purpose |
|--------|---------|
| `InitializePlayerState(missLimit)` | Reset player state for new game |
| `InitializeOpponentState(missLimit)` | Reset opponent state for new game |
| `AddPlayerMisses(amount)` | Increment player miss count |
| `AddOpponentMisses(amount)` | Increment opponent miss count |
| `HasPlayerExceededMissLimit()` | Check if player lost |
| `HasOpponentExceededMissLimit()` | Check if opponent lost |
| `GetPlayerMissCounterText()` | Formatted "X / Y" display |
| `GetOpponentMissCounterText()` | Formatted "X / Y" display |
| `CalculateMissLimit(difficulty, gridSize, wordCount)` | Static calculation |

---

### WinConditionChecker.cs (~225 lines)
**Namespace:** `TecVooDoo.DontLoseYourHead.UI`
**Location:** `Scripts/UI/Services/`
**Purpose:** Checks win/lose conditions for gameplay. Extracted from GameplayUIController to reduce file size.

**Architecture Pattern:** Plain C# class with injected GameplayStateTracker dependency.

**Win Condition:**
- All letters in opponent's words are known AND
- All grid positions for those words have been guessed

**Lose Condition:**
- Miss count exceeds miss limit

**Key Methods:**

| Method | Purpose | Returns |
|--------|---------|---------|
| `CheckPlayerWinCondition(opponentWords)` | Check if player won | bool |
| `CheckPlayerLoseCondition()` | Check if player lost | bool |
| `CheckOpponentWinCondition(playerWords)` | Check if AI won | bool |
| `CheckOpponentLoseCondition()` | Check if AI lost | bool |
| `IsWordFullyRevealed(wordData)` | Check if all letters known | bool |
| `FindNewlyRevealedWordRows(opponentWords)` | Find auto-solved rows | List<int> |

**Usage in GameplayUIController:**
```csharp
_stateTracker = new GameplayStateTracker();
_winChecker = new WinConditionChecker(_stateTracker);

// Check win conditions
if (_winChecker.CheckPlayerWinCondition(_opponentSetupData.PlacedWords))
{
    _gameOver = true;
}
```

---

### RowDisplayBuilder.cs (~207 lines)
**Namespace:** `TecVooDoo.DontLoseYourHead.UI.Utilities`  
**Location:** `Scripts/UI/Utilities/`  
**Purpose:** Static utility for building word pattern display text with proper formatting.

**Architecture Pattern:** Static utility class with shared StringBuilder.

```csharp
public static class RowDisplayBuilder
{
    private static readonly StringBuilder SharedBuilder = new StringBuilder(64);
    
    public static string Build(RowDisplayData data)
    {
        SharedBuilder.Clear();
        // Build formatted string
        return SharedBuilder.ToString();
    }
}
```

---

## Layer 4: UI Components

### WordPatternRow.cs (~1,199 lines)
**Namespace:** `TecVooDoo.DontLoseYourHead.UI`  
**Location:** `Scripts/UI/`  
**Purpose:** Individual word entry row. Handles letter input, display, validation, placement state.

**Key States:**
```csharp
public enum RowState
{
    Empty,          // No letters entered
    Entering,       // Partial word
    WordEntered,    // Complete word, not placed
    Placed,         // Word placed on grid
    Solved          // Word guessed correctly (gameplay)
}
```

**Events Published:**
```csharp
public event Action<int> OnRowSelected;           // rowNumber (1-indexed)
public event Action<int> OnCoordinateModeClicked; // rowNumber
public event Action<int, bool> OnDeleteClicked;   // rowNumber, wasPlaced
```

**Key Properties:**
```csharp
public string CurrentWord { get; }
public string EnteredText { get; }
public int RequiredWordLength { get; }
public bool HasWord { get; }
public bool IsPlaced { get; }
public bool IsSolved { get; }
```

**Auto-Hide Guess Word Buttons (Dec 13, 2025):**
When a word is fully revealed through letter guessing (all letters discovered), the "Guess Word" button automatically hides since there's nothing left to guess.

---

### GridCellUI.cs (~250 lines)
**Namespace:** `TecVooDoo.DontLoseYourHead.UI`  
**Location:** `Scripts/UI/`  
**Purpose:** Individual grid cell. Displays letter, handles click/hover, shows hit/miss colors.

**Cell States (Gameplay):**
```csharp
public enum CellState
{
    Hidden,         // Not yet guessed
    HitKnown,       // Green - letter revealed
    HitUnknown,     // Yellow - hit but letter not known
    Miss            // Red - empty cell
}
```

**Key Methods:**
```csharp
public void SetLetter(char letter);
public void SetHidden();
public void SetState(CellState state);
public void SetHighlightColor(Color color);
```

---

### LetterButton.cs (~200 lines)
**Namespace:** `TecVooDoo.DontLoseYourHead.UI`  
**Location:** `Scripts/UI/`  
**Purpose:** Individual letter button (A-Z). Used in letter tracker for keyboard input and guessing.

**Letter States:**
```csharp
public enum LetterState
{
    Normal,     // Not yet guessed (white)
    Hit,        // Guessed and found (green)
    Miss,       // Guessed and not found (red)
    Disabled    // Cannot click (grayed)
}
```

---

### GuessedWordListController.cs (~180 lines)
**Namespace:** `TecVooDoo.DontLoseYourHead.UI`  
**Location:** `Scripts/UI/`  
**Purpose:** Manages the list of guessed words displayed under guillotines. Shows correct (green) and incorrect (red) guesses.

---

### SettingsPanel.cs (~270 lines)
**Namespace:** `TecVooDoo.DontLoseYourHead.UI`  
**Location:** `Scripts/UI/`  
**Purpose:** Audio settings panel with volume sliders. Persists to PlayerPrefs.

**Default Values:**
```csharp
public const float DEFAULT_SFX_VOLUME = 0.5f;
public const float DEFAULT_MUSIC_VOLUME = 0.5f;
```

---

### SetupModeController.cs (~150 lines)
**Namespace:** `TecVooDoo.DontLoseYourHead.UI`  
**Location:** `Scripts/UI/`  
**Purpose:** Keyboard input routing during setup phase. Routes physical keyboard to letter tracker.

---

## Layer 5: AI System (11 Scripts)

### ExecutionerConfigSO.cs (~412 lines)
**Namespace:** `DLYH.AI.Config`  
**Location:** `Scripts/AI/Config/`  
**Purpose:** ScriptableObject containing all tunable AI parameters.

**Configuration Groups:**

| Group | Parameters |
|-------|------------|
| Skill Bounds | Min (0.15), Max (0.95), Step (0.15) |
| Initial Skills | Easy (0.25), Normal (0.50), Hard (0.75) |
| Rubber-banding | Hits to increase, Misses to decrease per difficulty |
| Adaptive Thresholds | Min/Max for threshold adaptation (1-7) |
| Strategy Weights | Grid density thresholds (0.35, 0.12) |
| Memory | Max forget chance (0.3), Always remember count (3) |
| Timing | Think time range (0.8-2.5 seconds) |

**Helper Methods:**
```csharp
public (float letterWeight, float coordWeight) GetStrategyWeightsForDensity(float fillRatio)
public float GetWordGuessThresholdForSkill(float skill)
public int GetLetterSelectionPoolSize(float skill)
```

---

### ExecutionerAI.cs (~493 lines)
**Namespace:** `DLYH.AI.Core`  
**Location:** `Scripts/AI/Core/`  
**Purpose:** Main AI MonoBehaviour coordinating turn execution.

| Dependencies | Direction |
|--------------|-----------|
| ExecutionerConfigSO | Configuration |
| DifficultyAdapter | Owns |
| MemoryManager | Owns |
| LetterGuessStrategy | Owns |
| CoordinateGuessStrategy | Owns |
| WordGuessStrategy | Owns |

**Events Published:**
```csharp
public event Action OnThinkingStarted;
public event Action<char> OnLetterGuess;
public event Action<int, int> OnCoordinateGuess;
public event Action<string, int> OnWordGuess;
```

**Key Methods:**

| Method | Purpose |
|--------|---------|
| `Initialize(DifficultySetting, AIGameState)` | Setup AI for game |
| `ExecuteTurnAsync()` | Execute one AI turn (async) |
| `RecordPlayerGuess(bool)` | Update rubber-banding |
| `RecordAIHit()` | Track AI success |

**Strategy Selection:**
```
1. Check WordGuessStrategy (high confidence only)
2. Calculate grid density
3. Get weights from config
4. Random weighted selection (Letter vs Coordinate)
5. Fallback if selected returns invalid
```

---

### DifficultyAdapter.cs (~268 lines)
**Namespace:** `DLYH.AI.Core`  
**Location:** `Scripts/AI/Core/`  
**Purpose:** Rubber-banding system with adaptive thresholds.

**Adaptation Logic:**
- Track consecutive player hits/misses
- After N hits: Increase AI skill
- After N misses: Decrease AI skill
- After 2+ same-direction adjustments: Adapt thresholds

```csharp
public float CurrentSkill { get; }           // 0.15 - 0.95
public int CurrentHitsToIncrease { get; }    // Threshold for AI harder
public int CurrentMissesToDecrease { get; }  // Threshold for AI easier
```

---

### MemoryManager.cs (~442 lines)
**Namespace:** `DLYH.AI.Core`  
**Location:** `Scripts/AI/Core/`  
**Purpose:** Skill-based memory filtering. Lower skill AI "forgets" older information.

**Key Methods:**
```csharp
public void RecordHit(int row, int col);
public void RecordRevealedLetter(char letter);
public HashSet<(int, int)> GetEffectiveKnownHits(float skill);
public HashSet<char> GetEffectiveRevealedLetters(float skill);
```

**Forget Chance Formula:**
```csharp
forgetChance = (1.0 - skillLevel) * maxForgetChance  // Max 30%
```

---

### AISetupManager.cs (~468 lines)
**Namespace:** `DLYH.AI.Core`  
**Location:** `Scripts/AI/Core/`  
**Purpose:** Handles AI word selection and grid placement. Replaces GenerateOpponentData() in GameplayUIController.

**Key Methods:**
```csharp
public List<string> SelectWords(int count, int[] lengths, WordListSO[] wordLists);
public List<WordPlacementData> PlaceWords(List<string> words, int gridSize);
```

**Integration (Dec 13, 2025):**
GameplayUIController now uses AISetupManager instead of the old GenerateOpponentData() method for creating AI's words and grid placement.

---

### IGuessStrategy.cs (~493 lines)
**Namespace:** `DLYH.AI.Strategies`  
**Location:** `Scripts/AI/Strategies/`  
**Purpose:** Interface and data structures for AI strategies.

**Key Types:**
```csharp
public enum GuessType { Letter, Coordinate, Word }
public struct GuessRecommendation { Type, Letter, Row, Col, WordGuess, Confidence, IsValid }
public class AIGameState { GridSize, GuessedLetters, HitCoordinates, WordPatterns, ... }
public interface IGuessStrategy { GuessType StrategyType; GuessRecommendation Evaluate(AIGameState); }
```

---

### LetterGuessStrategy.cs (~327 lines)
**Namespace:** `DLYH.AI.Strategies`  
**Location:** `Scripts/AI/Strategies/`  
**Purpose:** Letter selection based on frequency + pattern analysis.

**Algorithm:**
1. Score each unguessed letter by English frequency
2. Add pattern bonus (letters that complete word patterns)
3. Sort by score
4. Pick from top N based on skill (Expert=1, Easy=10)

---

### CoordinateGuessStrategy.cs (~262 lines)
**Namespace:** `DLYH.AI.Strategies`  
**Location:** `Scripts/AI/Strategies/`  
**Purpose:** Coordinate selection based on adjacency and patterns.

**Scoring Factors:**
- Adjacency to known hits (scaled by density)
- Line extension (2+ hits in row/column)
- Center bias
- Proximity bonus (2-3 cells from hits)

---

### WordGuessStrategy.cs (~327 lines)
**Namespace:** `DLYH.AI.Strategies`  
**Location:** `Scripts/AI/Strategies/`  
**Purpose:** Word guess decisions based on confidence thresholds.

**Risk:** Wrong word costs 2 misses.

**Confidence Formula:**
```csharp
confidence = matchCount == 1 ? 0.95f : 1.0f / matchCount;
threshold = 1.0f - (skill * riskFactor);  // riskFactor = 0.7
```

---

### LetterFrequency.cs (~442 lines)
**Namespace:** `DLYH.AI.Data`  
**Location:** `Scripts/AI/Data/`  
**Purpose:** Static English letter frequency data.

**Data:**
```csharp
E=12.7%, T=9.1%, A=8.2%, O=7.5%, I=7.0%, N=6.7%, S=6.3%, H=6.1%, R=6.0%...
```

**Key Methods:**
```csharp
public static float GetFrequency(char letter);
public static float GetNormalizedFrequency(char letter);
public static char[] GetUnguessedLettersByFrequency(HashSet<char> guessed);
```

---

### GridAnalyzer.cs (~442 lines)
**Namespace:** `DLYH.AI.Data`  
**Location:** `Scripts/AI/Data/`  
**Purpose:** Static grid analysis utilities.

**Key Methods:**
```csharp
public static float CalculateFillRatio(int wordCount, int gridSize);
public static string GetDensityCategory(float fillRatio);  // High/Medium/Low/VeryLow
public static bool IsAdjacentToAny(int row, int col, HashSet<(int,int)> hits);
public static bool ExtendsHitLine(int row, int col, HashSet<(int,int)> hits);
public static float CalculateCoordinateScore(...);
```

---

## Data Flow Diagrams

### Setup Mode Word Entry Flow

```
User types 'C'
    |
    v
SetupSettingsPanel.HandleKeyboardInput()
    |
    v
WordPatternController.AddLetterToSelectedRow('C')
    |
    v
WordPatternRow.AddLetter('C')
    |
    v
[Check if word complete]
    |
    +---(incomplete)---> Update display, wait
    |
    +---(complete)---> WordValidationService.IsValidWord()
                            |
                            +---(invalid)---> Show error, disable compass
                            |
                            +---(valid)---> Enable compass button
```

### Gameplay Guess Flow

```
Player clicks opponent's letter 'E'
    |
    v
GameplayUIController.HandleOpponentLetterClicked('E')
    |
    v
GuessProcessor.ProcessLetterGuess('E')
    |
    +---(AlreadyGuessed)---> Show message, try again
    |
    +---(Hit)---> RevealLetter('E'), Update patterns, End turn
    |
    +---(Miss)---> IncrementMiss(1), Check lose condition, End turn
```

### AI Turn Flow (INTEGRATED - Dec 13, 2025)

```
EndPlayerTurn()
    |
    v
RecordPlayerGuess(wasHit) -> DifficultyAdapter updates skill
    |
    v
TriggerAITurn()
    |
    v
ExecutionerAI.ExecuteTurnAsync()
    |
    v
Wait think time (0.8-2.5s)
    |
    v
BuildAIGameState() -> Create current game state snapshot
    |
    v
SelectStrategy() based on grid density
    |
    +---(WordGuessStrategy)---> If high confidence word available
    |
    +---(LetterGuessStrategy)---> Score letters, pick from top N
    |
    +---(CoordinateGuessStrategy)---> Score coords, pick from top N
    |
    v
Fire OnLetterGuess/OnCoordinateGuess/OnWordGuess event
    |
    v
GameplayUIController.HandleAI*Guess() processes result
    |
    v
GuessProcessor determines hit/miss
    |
    +---(WordIncorrect)---> IncrementMiss(2)  // Double penalty
    |
    +---(Miss)---> IncrementMiss(1)
    |
    +---(Hit/WordCorrect)---> Update UI, check win
    |
    v
EndOpponentTurn() -> Player's turn begins
```

---

## Event Architecture

```
MainMenuController
    |
    +--- OnNewGameClicked ---> SetupSettingsPanel
    +--- OnSettingsClicked ---> SettingsPanel

SetupSettingsPanel
    |
    +--- OnGridSizeChanged ---> PlayerGridPanel.SetGridSize()
    +--- OnWordCountChanged ---> WordPatternController.SetWordLengths()
    +--- OnSetupComplete ---> MainMenuController / GameplayUIController

PlayerGridPanel
    |
    +--- OnCellClicked ---> CoordinatePlacementController
    +--- OnLetterClicked ---> GameplayUIController (gameplay mode)

WordPatternController
    |
    +--- OnWordRowSelected ---> SetupSettingsPanel
    +--- OnCoordinateModeRequested ---> PlayerGridPanel
    +--- OnWordPlaced ---> SetupSettingsPanel
    +--- OnDeleteClicked ---> SetupSettingsPanel

CoordinatePlacementController
    |
    +--- OnPlacementCancelled ---> PlayerGridPanel
    +--- OnWordPlaced ---> WordPatternController, SetupSettingsPanel

GameplayUIController
    |
    +--- OnMissCountChanged ---> UI update
    +--- OnOpponentMissCountChanged ---> UI update
    +--- OnGameOver ---> Show win/lose screen
    |
    +--- (subscribes to) ExecutionerAI.OnLetterGuess
    +--- (subscribes to) ExecutionerAI.OnCoordinateGuess
    +--- (subscribes to) ExecutionerAI.OnWordGuess

ExecutionerAI
    |
    +--- OnThinkingStarted ---> UI thinking indicator
    +--- OnLetterGuess ---> GameplayUIController.HandleAILetterGuess()
    +--- OnCoordinateGuess ---> GameplayUIController.HandleAICoordinateGuess()
    +--- OnWordGuess ---> GameplayUIController.HandleAIWordGuess()
```

---

## Key Patterns

### 1. Controller Extraction Pattern
Large MonoBehaviours delegate to plain C# controller classes that receive dependencies via constructor.

### 2. Callback Injection Pattern
Services receive Actions/Funcs for operations they need but don't own. Updated to use `Action<int>` for variable miss counts.

### 3. Defensive Initialization Pattern
`EnsureControllersInitialized()` allows safe calling before Start() runs.

### 4. Event-Driven Communication
Controllers publish events; parents subscribe. No tight coupling.

### 5. Interface Segregation
IGridControllers.cs defines focused interfaces for each responsibility.

### 6. Strategy Pattern (AI)
IGuessStrategy implementations can be swapped or weighted based on game state.

---

## Complete File Structure

```
Assets/DLYH/Scripts/
|
+-- AI/
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
|
+-- Core/
|   |-- DifficultyCalculator.cs
|   |-- DifficultySO.cs
|   |-- Grid.cs
|   +-- ...
|
+-- UI/
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
    |   |-- GameplayStateTracker.cs
    |   |-- GuessProcessor.cs
    |   |-- WinConditionChecker.cs
    |   +-- WordValidationService.cs
    +-- Utilities/
        +-- RowDisplayBuilder.cs
```

---

## AI Integration Status (COMPLETE - Dec 13, 2025)

All AI integration points have been implemented:

| Integration Point | Status | Details |
|-------------------|--------|---------|
| ExecutionerAI wired to GameplayUIController | COMPLETE | WireAIEvents() method |
| AI events connected | COMPLETE | OnLetterGuess, OnCoordinateGuess, OnWordGuess |
| BuildAIGameState() | COMPLETE | Creates snapshot for AI decisions |
| TriggerAITurn() | COMPLETE | Executes after player turn ends |
| Rubber-banding connected | COMPLETE | RecordPlayerGuess() calls |
| AISetupManager integration | COMPLETE | Replaces GenerateOpponentData() |
| Win condition checking | COMPLETE | Implemented in gameplay loop |

---

## Known Playtest Issues (Dec 13, 2025)

First playtest (Stacey vs AI) revealed these issues:

| Bug | Description | Status |
|-----|-------------|--------|
| Letter tracker on word guess | Letters don't turn green when word is correctly guessed (both panels) | IN PROGRESS |
| Center panel names | Shows "Player 1/Player 2" instead of actual names under guillotines | IN PROGRESS |
| Miss count for word guesses | Wrong word guess counts 1 miss instead of 2 | IN PROGRESS |
| Guess Word button auto-hide | Buttons now auto-hide when words fully revealed via letters | FIXED |

---

## Refactoring History

### December 13, 2025 - State Tracker Extraction
**Target:** GameplayUIController.cs (was ~1,830 lines, now ~1,700 lines)

**Extracted Services:**
1. **GameplayStateTracker.cs** (~300 lines)
   - All player/opponent state tracking
   - Miss counts, known letters, guessed coordinates
   - Solved word rows
   - Miss limit calculation

2. **WinConditionChecker.cs** (~225 lines)
   - Player/opponent win condition checking
   - Player/opponent lose condition checking
   - Word reveal detection (for auto-hiding guess buttons)

**Integration Pattern:**
GameplayUIController now uses property accessors that delegate to `_stateTracker` for backwards compatibility with existing code that references fields like `_playerMisses` and `_playerKnownLetters`.

---

**End of Architecture Document**

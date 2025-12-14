# Don't Lose Your Head - Architecture Document

**Version:** 2.0  
**Date Created:** December 13, 2025  
**Last Updated:** December 13, 2025  
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
| `TecVooDoo.DontLoseYourHead.UI.Utilities` | 1 | RowDisplayBuilder |
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

## Layer 2: Extracted Controllers (DETAILED)

### From PlayerGridPanel (7 Controllers)

---

### GridCellManager.cs (~130 lines)
**Namespace:** `TecVooDoo.DontLoseYourHead.UI`  
**Location:** `Scripts/UI/Controllers/`  
**Purpose:** Manages the 2D cell array and provides coordinate utilities. Separation of cell storage from cell creation/layout.

**Architecture Pattern:** Plain C# class with fixed-size array.

| Dependencies | Direction |
|--------------|-----------|
| GridCellUI | Stores references |

**Constants:**
```csharp
public const int MAX_GRID_SIZE = 12;
public const int MIN_GRID_SIZE = 6;
```

**Internal Storage:**
```csharp
private readonly GridCellUI[,] _cells;  // 12x12 fixed array
```

**Key Methods:**

| Method | Purpose | Returns |
|--------|---------|---------|
| `GetCell(int col, int row)` | Get cell at position | GridCellUI or null |
| `SetCell(int col, int row, GridCellUI)` | Store cell reference | void |
| `IsValidCoordinate(int col, int row, int gridSize)` | Bounds check | bool |
| `GetColumnLetter(int col)` | 0 -> 'A', 1 -> 'B' | char |
| `GetColumnIndex(char letter)` | 'A' -> 0, 'B' -> 1 | int |
| `ClearCellArray()` | Null all references | void |
| `GetCellCount()` | Debug utility | int |

**Usage (from PlayerGridPanel):**
```csharp
_gridCellManager = new GridCellManager();
GridCellUI cell = _gridCellManager.GetCell(col, row);
```

---

### GridLayoutManager.cs (~593 lines)
**Namespace:** `TecVooDoo.DontLoseYourHead.UI`  
**Location:** `Scripts/UI/Controllers/`  
**Purpose:** Handles all layout operations: cell creation, sizing, label visibility, GridLayoutGroup configuration. The largest extracted controller.

**Architecture Pattern:** Plain C# class with extensive Unity UI dependencies.

| Dependencies | Direction |
|--------------|-----------|
| Transform (multiple containers) | Injected |
| RectTransform | Injected |
| LayoutElement | Injected |
| GridLayoutGroup | Cached |
| GridCellUI (prefab) | Injected |

**Constructor (8 parameters):**
```csharp
public GridLayoutManager(
    Transform gridContainer,           // Parent for cells
    Transform rowLabelsContainer,      // 1-12 labels
    Transform columnLabelsContainer,   // A-L labels
    RectTransform gridWithRowLabelsRect,
    LayoutElement gridContainerLayout,
    LayoutElement rowLabelsLayout,
    GridCellUI cellPrefab,
    RectTransform panelRectTransform)
```

**Constants:**
```csharp
public const int MAX_GRID_SIZE = 12;
public const int MIN_GRID_SIZE = 6;
private const float MAX_CELL_SIZE = 65f;
private const float MIN_CELL_SIZE = 40f;
private const float CELL_SPACING = 2f;
```

**Key Methods:**

| Method | Purpose |
|--------|---------|
| `CacheExistingLabels()` | Find row/column label GameObjects by name |
| `UpdateGridLayoutConstraint(int)` | Set GridLayoutGroup.constraintCount |
| `UpdatePanelHeight(int, bool)` | Recalculate heights (skips in Gameplay mode) |
| `UpdateCellSizes(int)` | Adjust cell dimensions for grid size |
| `UpdateLabelVisibility(int)` | Show/hide labels 1-12 and A-L |
| `CreateCellsForCurrentSize(int, GridCellUI[,])` | Instantiate cells in row-major order |
| `ClearGrid()` | Destroy all cell GameObjects |

**Delegate for Cell Creation:**
```csharp
public Action<GridCellUI, int, int> OnCellCreated;
```

**Label Caching Logic:**
```csharp
// Finds labels by name pattern: "Label_1", "Row_1", "1" -> index 0
// Handles edge cases: "10", "11", "12" not confused with "1"
```

**Cell Creation Order:**
```csharp
// Row-major order to match GridLayoutGroup behavior
for (int row = 0; row < gridSize; row++)
    for (int col = 0; col < gridSize; col++)
        CreateCell(col, row, cells);
```

**Usage (from PlayerGridPanel):**
```csharp
_gridLayoutManager = new GridLayoutManager(
    _gridContainer, _rowLabels, _columnLabels, ...);
_gridLayoutManager.OnCellCreated += HandleCellCreated;
_gridLayoutManager.CreateCellsForCurrentSize(gridSize, _gridCellManager.Cells);
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

**Caching Logic:**
```csharp
public void CacheLetterButtons()
{
    var buttons = _container.GetComponentsInChildren<LetterButton>(true);
    foreach (var button in buttons)
    {
        button.EnsureInitialized();
        // Unsubscribe first to prevent duplicates
        button.OnLetterClicked -= HandleLetterClicked;
        button.OnLetterClicked += HandleLetterClicked;
        _letterButtons[button.Letter] = button;
    }
}
```

**Usage (from PlayerGridPanel):**
```csharp
_letterTrackerController = new LetterTrackerController(_letterTrackerContainer);
_letterTrackerController.CacheLetterButtons();
_letterTrackerController.OnLetterClicked += HandleLetterClicked;
```

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
public GridColorManager()
{
    _cursorColor = new Color(0.13f, 0.85f, 0.13f, 1f);      // Stoplight green
    _validPlacementColor = new Color(0.6f, 1f, 0.6f, 0.8f); // Light mint
    _invalidPlacementColor = new Color(1f, 0f, 0f, 0.7f);   // Red
    _placedLetterColor = new Color(0.5f, 0.8f, 1f, 1f);     // Light blue
}
```

**Key Methods:**

| Method | Purpose |
|--------|---------|
| `SetCellHighlight(GridCellUI, GridHighlightType)` | Apply highlight color |
| `ClearCellHighlight(GridCellUI)` | Remove highlight |
| `ClearAllHighlights(IGridDisplayController)` | Clear entire grid |
| `GetColorForType(GridHighlightType)` | Get color for type |

**GridHighlightType Enum:**
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

**Usage (from CoordinatePlacementController):**
```csharp
_colorManager = new GridColorManager();
_colorManager.SetCellHighlight(cell, GridHighlightType.Cursor);
```

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
| `ShowFirstCellPreview(col, row, validDirections)` | Highlight first cell + valid directions |
| `ShowDirectionPreview(firstCol, firstRow, hoverCol, hoverRow, word, validDirections)` | Show full word preview when hovering valid direction |
| `ShowWordPreview(startCol, startRow, dCol, dRow, word)` | Display word letters along direction |
| `ClearAllPreviews(gridSize, placedPositions, placedLetters)` | Clear preview, restore placed letters |

**Preview Flow (First Cell Selection):**
```
ShowFirstCellPreview(hoverCol, hoverRow, validDirections)
    |
    +-- Highlight hover cell as Cursor (green)
    |
    +-- For each valid direction cell:
    |   +-- Highlight as ValidPlacement (light green)
    |
    +-- For each adjacent cell NOT in validDirections:
        +-- Highlight as InvalidPlacement (red)
```

**Preview Flow (Direction Selection):**
```
ShowDirectionPreview(firstCol, firstRow, hoverCol, hoverRow, word, validDirections)
    |
    +-- First cell stays as Cursor
    |
    +-- Show valid direction cells
    |
    +-- If hovering over valid direction:
        +-- ShowWordPreview() - display actual letters
```

**Clear Preview Flow:**
```
ClearAllPreviews(gridSize, placedPositions, placedLetters)
    |
    +-- For each cell:
        +-- Clear highlighting
        +-- If in placedPositions:
        |   +-- Restore permanently placed letter
        +-- Else:
            +-- Clear any preview letters
```

**Usage (from CoordinatePlacementController):**
```csharp
_previewController = new PlacementPreviewController(
    _colorManager,
    (col, row) => _getCell(col, row),
    (col, row) => _isValidCoordinate(col, row));

_previewController.ShowFirstCellPreview(col, row, validDirections);
```

---

### CoordinatePlacementController.cs (~616 lines)
**Namespace:** `TecVooDoo.DontLoseYourHead.UI`  
**Location:** `Scripts/UI/Controllers/`  
**Purpose:** Manages the two-step word placement state machine (select first cell, then direction). The most complex extracted controller with full placement validation logic.

**Architecture Pattern:** State machine with `PlacementState` enum. Implements `ICoordinatePlacementController`.

| Dependencies | Direction |
|--------------|-----------|
| IGridColorManager | Injected |
| Func<int, int, GridCellUI> | Injected |
| Func<int> (getGridSize) | Injected |

**State Machine:**
```
PlacementState.Inactive
    |
    v (EnterPlacementMode)
PlacementState.SelectingFirstCell
    |
    v (Click valid cell)
PlacementState.SelectingDirection
    |
    v (Click direction cell)
PlacementState.Inactive + OnWordPlaced event
```

**Events Published:**
```csharp
public event Action OnPlacementCancelled;
public event Action<int, string, List<Vector2Int>> OnWordPlaced;  // rowIndex, word, positions
```

**Internal State Tracking:**
```csharp
private readonly List<Vector2Int> _placedCellPositions;           // Current word being placed
private readonly HashSet<Vector2Int> _allPlacedPositions;         // All placed positions (O(1) lookup)
private readonly Dictionary<Vector2Int, char> _placedLetters;     // Position -> letter mapping
private readonly Dictionary<int, List<Vector2Int>> _wordRowPositions; // Row index -> positions
```

**Key Methods:**

| Method | Purpose |
|--------|---------|
| `EnterPlacementMode(int rowIndex, string word)` | Start placement for a word |
| `CancelPlacementMode()` | Cancel and restore grid state |
| `HandleCellClick(int col, int row)` | Process click during placement |
| `PlaceWordRandomly()` | Auto-place using random valid position |
| `RemoveWordFromGrid(int rowIndex)` | Clear a specific word's placement |
| `ClearAllPlacedWords()` | Reset all placements |
| `GetValidDirectionsFromCell(col, row)` | Find valid directions for word |
| `IsValidPlacement(col, row, dCol, dRow, length)` | Check if word fits |

**8-Direction Support:**
```csharp
int[] dCols = { 1, 0, 1, 1, -1, 0, -1, -1 };  // Right, Down, DR, UR, Left, Up, UL, DL
int[] dRows = { 0, 1, 1, -1, 0, -1, -1, 1 };
```

**Placement Validation Logic:**
```csharp
// Word can overlap existing letters ONLY if same letter at same position
if (_allPlacedPositions.Contains(pos))
{
    if (_placedLetters.TryGetValue(pos, out char existing))
    {
        if (existing != _placementWord[i])
            return false;  // Conflict!
    }
}
```

**Usage (from PlayerGridPanel):**
```csharp
_coordinatePlacementController = new CoordinatePlacementController(
    _gridColorManager,
    (col, row) => _gridCellManager.GetCell(col, row),
    () => _currentGridSize);
_coordinatePlacementController.OnWordPlaced += HandleWordPlaced;
```

---

### WordPatternRowManager.cs (~473 lines)
**Namespace:** `TecVooDoo.DontLoseYourHead.UI`  
**Location:** `Scripts/UI/Controllers/`  
**Purpose:** Manages collection of WordPatternRow components. Handles caching, selection, input routing, and state queries. Does NOT manipulate grid cells directly.

**Architecture Pattern:** Plain C# class. Collection manager with event forwarding.

| Dependencies | Direction |
|--------------|-----------|
| Transform (container) | Injected |
| AutocompleteDropdown | Injected |
| WordPatternRow[] | Found in children |

**Events Published:**
```csharp
public event Action<int> OnWordRowSelected;           // Row index (0-based)
public event Action<int> OnCoordinateModeRequested;   // Row index (0-based)
public event Action<int, bool> OnDeleteClicked;       // Row index, wasPlaced
public event Action OnWordLengthsChanged;
```

**Key Properties:**
```csharp
public int SelectedRowIndex => _selectedWordRowIndex;
public int RowCount => _wordPatternRows.Count;
public bool HasSelection => _selectedWordRowIndex >= 0 && _selectedWordRowIndex < _wordPatternRows.Count;
```

**Key Methods:**

| Method | Purpose |
|--------|---------|
| `CacheWordPatternRows()` | Find and subscribe to all rows |
| `SetWordValidator(Func<string, int, bool>)` | Set validation callback |
| `SelectRow(int index)` | Select row for input |
| `AddLetterToSelectedRow(char)` | Route letter input |
| `RemoveLastLetterFromSelectedRow()` | Route backspace |
| `SetWordLengths(int[])` | Configure row lengths, returns rows needing re-placement |
| `GetUnplacedRowIndices()` | Get unplaced rows sorted by length (longest first) |
| `AreAllWordsPlaced()` | Check completion state |

**Row Number to Index Conversion:**
```csharp
// WordPatternRow uses 1-based row numbers for display
// Manager uses 0-based indices internally
private void HandleWordRowSelected(int rowNumber)
{
    int index = rowNumber - 1;
    SelectRow(index);
}
```

**Sibling Index Sorting (Critical!):**
```csharp
// Sort by sibling index to ensure correct visual order (top to bottom)
WordPatternRow[] sortedRows = rows.OrderBy(r => r.transform.GetSiblingIndex()).ToArray();
```

**Usage (from PlayerGridPanel):**
```csharp
_wordPatternRowManager = new WordPatternRowManager(_wordPatternsContainer, _autocompleteDropdown);
_wordPatternRowManager.CacheWordPatternRows();
_wordPatternRowManager.OnWordRowSelected += HandleWordRowSelected;
_wordPatternRowManager.OnCoordinateModeRequested += HandleCoordinateModeRequested;
```

---

### From GameplayUIController (2 Controllers)

---

### WordGuessModeController.cs (~290 lines)
**Namespace:** `TecVooDoo.DontLoseYourHead.UI`  
**Location:** `Scripts/UI/Controllers/`  
**Purpose:** Manages word guess mode during gameplay. Handles keyboard mode switching, letter tracker state saving/restoring, and guess submission flow.

**Architecture Pattern:** Plain C# class with callback injection. State machine for keyboard mode.

| Dependencies | Direction |
|--------------|-----------|
| PlayerGridPanel | Injected |
| Func<string, int, WordGuessResult> | Injected (processWordGuess) |
| Func<bool> | Injected (canStartGuess) |
| Func<int, bool> | Injected (isRowSolved) |
| Action<int> | Injected (markRowSolved) |

**Events Published:**
```csharp
public event Action<int, string, bool> OnWordGuessProcessed;  // rowIndex, word, wasCorrect
public event Action<string> OnFeedbackRequested;               // Message to display
public event Action OnTurnEnded;
```

**WordGuessResult Enum:**
```csharp
public enum WordGuessResult
{
    Hit,           // Correct guess
    Miss,          // Wrong word (+2 misses)
    AlreadyGuessed,
    InvalidWord    // Not in word bank
}
```

**Key State:**
```csharp
private WordPatternRow _activeWordGuessRow = null;
private Dictionary<char, LetterButton.LetterState> _savedLetterStates;
private bool _letterTrackerInKeyboardMode = false;
```

**Key Methods:**

| Method | Purpose |
|--------|---------|
| `HandleWordGuessStarted(int rowNumber)` | Enter guess mode for row |
| `HandleWordGuessSubmitted(int rowNumber, string word)` | Process submission |
| `HandleWordGuessCancelled(int rowNumber)` | Cancel guess mode |
| `ProcessKeyboardInput()` | Handle A-Z, Backspace, Enter, Escape |
| `ShowAllGuessWordButtons()` | Show buttons on unsolved rows |
| `ExitWordGuessMode()` | Force exit if active |

**Letter Tracker Keyboard Mode:**
```csharp
// When entering word guess mode:
// 1. Save current letter states (green/red from guessing)
// 2. Reset all to Normal (white) for typing
// 3. On exit, restore saved states

private void SwitchLetterTrackerToKeyboardMode()
{
    _savedLetterStates.Clear();
    for (char c = 'A'; c <= 'Z'; c++)
    {
        _savedLetterStates[c] = _panel.GetLetterState(c);
        _panel.SetLetterState(c, LetterButton.LetterState.Normal);
    }
    _letterTrackerInKeyboardMode = true;
}
```

**Keyboard Input Processing (New Input System):**
```csharp
public bool ProcessKeyboardInput()
{
    Keyboard keyboard = Keyboard.current;
    if (keyboard == null) return false;

    for (int i = 0; i < 26; i++)
    {
        Key key = Key.A + i;
        if (keyboard[key].wasPressedThisFrame)
        {
            char letter = (char)('A' + i);
            HandleKeyboardLetterInput(letter);
            return true;
        }
    }
    // ... handle Backspace, Enter, Escape
}
```

---

### WordGuessInputController.cs (~290 lines)
**Namespace:** `TecVooDoo.DontLoseYourHead.UI.Controllers`  
**Location:** `Scripts/UI/Controllers/`  
**Purpose:** Manages cursor position and typed letters within a single WordPatternRow during word guess mode. Handles revealed letter skipping.

**Architecture Pattern:** Plain C# class with callback injection. Cursor-based input state.

| Dependencies | Direction |
|--------------|-----------|
| Func<int, bool> (isLetterRevealed) | Injected |
| Func<int, char> (getRevealedLetter) | Injected |
| Func<string> (getCurrentWord) | Injected |

**Events Published:**
```csharp
public event Action OnGuessStarted;
public event Action<string> OnGuessSubmitted;  // Guessed word
public event Action OnGuessCancelled;
public event Action OnDisplayUpdateNeeded;
```

**Key State:**
```csharp
private bool _isActive;
private char[] _guessedLetters;  // Typed letters (revealed positions stay '\0')
private int _cursorPosition;
private int _wordLength;
```

**Key Methods:**

| Method | Purpose |
|--------|---------|
| `Initialize(int wordLength)` | Setup for word length |
| `Enter()` | Enter guess mode, cursor to first unrevealed |
| `Exit(bool submit)` | Exit with optional submission |
| `TypeLetter(char)` | Type at cursor, auto-advance |
| `Backspace()` | Clear current or move back |
| `GetFullGuessWord()` | Combine revealed + typed letters |
| `IsGuessComplete()` | All unrevealed positions filled? |
| `GetGuessedLetterAt(int)` | For display building |

**Cursor Navigation (Skip Revealed):**
```csharp
private int FindNextUnrevealedPosition(int fromPosition)
{
    for (int i = fromPosition + 1; i < _wordLength; i++)
    {
        if (!_isLetterRevealed(i))
            return i;
    }
    return -1;  // No more unrevealed positions
}
```

**Backspace Behavior:**
```csharp
// First click: clears letter at current position (stays there)
// Second click (if position empty): moves back and clears that letter
```

**Full Guess Word Construction:**
```csharp
public string GetFullGuessWord()
{
    char[] result = new char[currentWord.Length];
    for (int i = 0; i < currentWord.Length; i++)
    {
        if (_isLetterRevealed(i))
            result[i] = _getRevealedLetter(i);
        else if (_guessedLetters[i] != '\0')
            result[i] = _guessedLetters[i];
        else
            result[i] = '_';
    }
    return new string(result);
}
```

---

### From SetupSettingsPanel (2 Controllers)

---

### PlayerColorController.cs (~165 lines)
**Namespace:** `TecVooDoo.DontLoseYourHead.UI`  
**Location:** `Scripts/UI/Controllers/`  
**Purpose:** Manages 8-button color picker with outline selection highlight. Extracts color from button Image component.

**Architecture Pattern:** Plain C# class. UI state management.

| Dependencies | Direction |
|--------------|-----------|
| Transform (container) | Injected |
| Button (children) | Found |
| Outline (per button) | Created if missing |

**Events Published:**
```csharp
public event Action<Color> OnColorChanged;
```

**Key State:**
```csharp
private readonly List<Button> _colorButtons;
private readonly List<Outline> _colorButtonOutlines;
private int _currentColorIndex = 0;
private Color _currentColor;
```

**Key Methods:**

| Method | Purpose |
|--------|---------|
| `Initialize()` | Find buttons, add outlines, wire clicks |
| `SelectColor(int index)` | Update selection, fire event |
| `GetCurrentColor()` | Get selected color |
| `RefreshSelectionVisual()` | Update outline states |
| `Cleanup()` | Remove click listeners |

**Outline Selection Pattern:**
```csharp
public void Initialize()
{
    for (int i = 0; i < _colorButtonsContainer.childCount; i++)
    {
        var outline = child.GetComponent<Outline>();
        if (outline == null)
        {
            outline = child.gameObject.AddComponent<Outline>();
        }
        outline.effectColor = Color.white;
        outline.effectDistance = new Vector2(3, 3);
        outline.enabled = false;  // Only enabled on selected button
        _colorButtonOutlines.Add(outline);
    }
}
```

**Usage (from SetupSettingsPanel):**
```csharp
_playerColorController = new PlayerColorController(_colorButtonsContainer);
_playerColorController.Initialize();
_playerColorController.OnColorChanged += HandleColorChanged;
```

---

### AutocompleteManager.cs (~350 lines)
**Namespace:** `TecVooDoo.DontLoseYourHead.UI.Controllers`  
**Location:** `Scripts/UI/Controllers/`  
**Purpose:** Coordinates autocomplete dropdown behavior during Setup Mode. Listens to WordPatternRow text changes and manages dropdown positioning/visibility.

**Architecture Pattern:** MonoBehaviour (needs Update for keyboard navigation). Event-driven coordination.

| Dependencies | Direction |
|--------------|-----------|
| PlayerGridPanel | Serialized |
| AutocompleteDropdown | Serialized |
| List<WordListSO> | Serialized (by length) |

**Serialized Fields:**
```csharp
[SerializeField] private PlayerGridPanel _playerGridPanel;
[SerializeField] private AutocompleteDropdown _autocompleteDropdown;
[SerializeField] private List<WordListSO> _wordListsByLength;  // index 0 = 3-letter, etc.
[SerializeField] private Vector2 _dropdownOffset = new Vector2(-350f, -35f);
```

**Key State:**
```csharp
private int _activeRowIndex = -1;
private WordPatternRow _activeRow;
private Dictionary<WordPatternRow, bool> _subscribedRows;  // Track subscriptions
```

**Event Subscriptions:**
```csharp
// From PlayerGridPanel:
_playerGridPanel.OnWordRowSelected += HandleRowSelected;
_playerGridPanel.OnWordLengthsChanged += HandleWordLengthsChanged;

// From each WordPatternRow:
row.OnWordTextChanged += HandleWordTextChanged;
row.OnWordAccepted += HandleWordAccepted;

// From AutocompleteDropdown:
_autocompleteDropdown.OnWordSelected += HandleWordSelected;
```

**Key Methods:**

| Method | Purpose |
|--------|---------|
| `Initialize()` | Wire all event subscriptions |
| `HandleRowSelected(int)` | Position dropdown near row |
| `HandleWordTextChanged(int, string)` | Update filter as user types |
| `HandleWordAccepted(int, string)` | Hide dropdown on completion |
| `HandleWordSelected(string)` | Apply selected word to row |
| `ProcessNavigationInput()` | Handle Up/Down/Enter/Escape |
| `HideDropdown()` | Force hide |

**Dropdown Positioning:**
```csharp
private void PositionDropdownNearRow(WordPatternRow row)
{
    Vector3 rowWorldPos = rowRect.position;
    Vector3 dropdownPos = rowWorldPos + new Vector3(_dropdownOffset.x, _dropdownOffset.y, 0f);
    _dropdownRectTransform.position = dropdownPos;
}
```

**Auto-Hide on Completion:**
```csharp
private void HandleWordTextChanged(int rowNumber, string currentText)
{
    // Hide when word reaches required length
    if (_activeRow != null && currentText.Length >= _activeRow.RequiredWordLength)
    {
        _autocompleteDropdown.Hide();
        return;
    }
    _autocompleteDropdown.UpdateFilter(currentText);
}
```

**Keyboard Navigation (in Update):**
```csharp
private void ProcessNavigationInput()
{
    if (keyboard.upArrowKey.wasPressedThisFrame)
        _autocompleteDropdown.SelectPrevious();
    else if (keyboard.downArrowKey.wasPressedThisFrame)
        _autocompleteDropdown.SelectNext();
    else if (keyboard.enterKey.wasPressedThisFrame)
        _autocompleteDropdown.ConfirmSelection();
    else if (keyboard.escapeKey.wasPressedThisFrame)
        _autocompleteDropdown.Hide();
}
```

---

## Layer 2B: Additional UI Components

### AutocompleteDropdown.cs (~522 lines)
**Namespace:** `TecVooDoo.DontLoseYourHead.UI`  
**Location:** `Scripts/UI/`  
**Purpose:** Displays filtered word suggestions as user types. Manages item pooling, filtering, selection state, and keyboard navigation.

**Architecture Pattern:** MonoBehaviour with object pooling for dropdown items.

**Serialized Fields:**
```csharp
[SerializeField] private Transform _itemContainer;
[SerializeField] private GameObject _itemPrefab;
[SerializeField] private ScrollRect _scrollRect;
[SerializeField] private CanvasGroup _canvasGroup;
[SerializeField] private int _maxVisibleItems = 8;
[SerializeField] private float _itemHeight = 40f;
[SerializeField] private int _minCharsToShow = 2;
```

**Events Published:**
```csharp
public event Action<string> OnWordSelected;
public event Action OnDropdownShown;
public event Action OnDropdownHidden;
```

**Key State:**
```csharp
private List<string> _currentWordList;     // Full word list
private List<string> _filteredWords;       // After prefix filter
private List<AutocompleteItem> _itemInstances;  // Pooled UI items
private string _currentFilter = "";
private int _currentWordLength;
private int _selectedIndex = -1;
```

**Key Methods:**

| Method | Purpose |
|--------|---------|
| `SetWordList(List<string>)` | Load word list for filtering |
| `SetWordListFromSO(ScriptableObject)` | Load via reflection (avoids hard dependency) |
| `SetRequiredWordLength(int)` | Filter by exact word length |
| `UpdateFilter(string)` | Filter by prefix, auto-show/hide |
| `SelectPrevious() / SelectNext()` | Keyboard navigation |
| `ConfirmSelection()` | Select current or auto-select if single result |

**Filtering Logic:**
```csharp
private void RefreshFilter()
{
    foreach (string word in _currentWordList)
    {
        if (_currentWordLength > 0 && word.Length != _currentWordLength)
            continue;
        if (word.StartsWith(_currentFilter, StringComparison.OrdinalIgnoreCase))
        {
            _filteredWords.Add(word);
            if (_filteredWords.Count >= 50) break;  // Performance limit
        }
    }
}
```

**Object Pooling Pattern:**
```csharp
private void EnsureItemInstances(int count)
{
    while (_itemInstances.Count < count && _itemInstances.Count < _maxVisibleItems * 2)
    {
        CreateItemInstance();
    }
}
```

---

### AutocompleteItem.cs (~165 lines)
**Namespace:** `TecVooDoo.DontLoseYourHead.UI`  
**Location:** `Scripts/UI/`  
**Purpose:** Individual item in autocomplete dropdown. Handles hover/selection states and prefix highlighting.

**Architecture Pattern:** MonoBehaviour implementing `IPointerEnterHandler`, `IPointerExitHandler`.

**Serialized Fields:**
```csharp
[SerializeField] private TextMeshProUGUI _wordText;
[SerializeField] private Image _backgroundImage;
[SerializeField] private Button _button;
[SerializeField] private Color _normalColor, _hoverColor, _selectedColor;
[SerializeField] private Color _matchedTextColor;  // For prefix highlight
```

**Events Published:**
```csharp
public event Action OnItemClicked;
```

**Key Methods:**

| Method | Purpose |
|--------|---------|
| `SetWord(string word, string matchedPrefix)` | Display with prefix highlighting |
| `SetSelected(bool)` | Update selection visual state |

**Prefix Highlighting (Rich Text):**
```csharp
public void SetWord(string word, string matchedPrefix)
{
    if (word.StartsWith(matchedPrefix, StringComparison.OrdinalIgnoreCase))
    {
        string matched = word.Substring(0, matchedPrefix.Length);
        string remainder = word.Substring(matchedPrefix.Length);
        string hexColor = ColorUtility.ToHtmlStringRGB(_matchedTextColor);
        _wordText.text = string.Format("<color=#{0}>{1}</color>{2}", hexColor, matched, remainder);
    }
}
```

**Three-State Visual System:**
```csharp
private void UpdateVisuals()
{
    Color bgColor = _isSelected ? _selectedColor
                  : _isHovered ? _hoverColor
                  : _normalColor;
    _backgroundImage.color = bgColor;
}
```

---

### GuessedWordListController.cs (~140 lines)
**Namespace:** `TecVooDoo.DontLoseYourHead.UI`  
**Location:** `Scripts/UI/`  
**Purpose:** Displays list of guessed words sorted by length then alphabetically. Shows green for hits, red for misses.

**Architecture Pattern:** MonoBehaviour with data-driven UI rebuild.

**Serialized Fields:**
```csharp
[SerializeField] private GameObject _guessedWordPrefab;
[SerializeField] private Color _hitColor = new Color(0.2f, 0.8f, 0.2f, 1f);
[SerializeField] private Color _missColor = new Color(0.8f, 0.2f, 0.2f, 1f);
```

**Internal Data Structure:**
```csharp
private struct GuessedWordData
{
    public string Word;
    public bool IsHit;
}
private List<GuessedWordData> _guessedWords;
private List<GameObject> _instantiatedEntries;
```

**Key Methods:**

| Method | Purpose |
|--------|---------|
| `AddGuessedWord(string word, bool isHit)` | Add and refresh display |
| `ClearAllWords()` | Reset list |

**Sorting Strategy:**
```csharp
_guessedWords.Sort((a, b) =>
{
    int lengthCompare = a.Word.Length.CompareTo(b.Word.Length);
    if (lengthCompare != 0) return lengthCompare;
    return string.Compare(a.Word, b.Word, StringComparison.Ordinal);
});
```

**Rebuild Pattern:**
```csharp
private void RebuildDisplay()
{
    DestroyAllEntries();
    foreach (var wordData in _guessedWords)
    {
        CreateWordEntry(wordData);
    }
}
```

---

### SettingsPanel.cs (~270 lines)
**Namespace:** `DLYH.UI`  
**Location:** `Scripts/UI/`  
**Purpose:** Audio settings panel with SFX and Music volume sliders. Persists to PlayerPrefs.

**Architecture Pattern:** MonoBehaviour with PlayerPrefs persistence and static accessors.

**Constants:**
```csharp
private const string PREFS_SFX_VOLUME = "DLYH_SFXVolume";
private const string PREFS_MUSIC_VOLUME = "DLYH_MusicVolume";
private const float DEFAULT_VOLUME = 0.5f;
```

**Serialized Fields:**
```csharp
[SerializeField] private Slider _sfxVolumeSlider;
[SerializeField] private Slider _musicVolumeSlider;
[SerializeField] private TMP_Text _sfxVolumeLabel;
[SerializeField] private TMP_Text _musicVolumeLabel;
[SerializeField] private Button _backButton;
[SerializeField] private MainMenuController _mainMenuController;
```

**Key Methods:**

| Method | Purpose |
|--------|---------|
| `LoadSettings()` | Load from PlayerPrefs on Awake |
| `SaveSettings()` | Save to PlayerPrefs on change |
| `ResetToDefaults()` | Set both to 50% |

**Static Accessors (No Instance Needed):**
```csharp
public static float GetSavedSFXVolume()
{
    return PlayerPrefs.GetFloat(PREFS_SFX_VOLUME, DEFAULT_VOLUME);
}

public static float GetSavedMusicVolume()
{
    return PlayerPrefs.GetFloat(PREFS_MUSIC_VOLUME, DEFAULT_VOLUME);
}
```

**Usage (Audio System Integration):**
```csharp
// Anywhere in code without needing SettingsPanel reference:
float sfxVolume = SettingsPanel.GetSavedSFXVolume();
float musicVolume = SettingsPanel.GetSavedMusicVolume();
```

---

### SetupModeController.cs (~560 lines)
**Namespace:** `TecVooDoo.DontLoseYourHead.UI`  
**Location:** `Scripts/UI/`  
**Purpose:** Routes keyboard input during Setup Mode. Handles letter entry, Tab navigation, Enter/Escape/Delete keys. Bridges between physical keyboard and PlayerGridPanel.

**Architecture Pattern:** MonoBehaviour with Update-based input processing (New Input System).

**Serialized Fields:**
```csharp
[SerializeField] private PlayerGridPanel _playerGridPanel;
[SerializeField] private AutocompleteDropdown _autocompleteDropdown;
[SerializeField] private bool _enableKeyboardLetterInput = true;
[SerializeField] private bool _autoSelectFirstRow = true;
```

**Events Published:**
```csharp
public event Action OnSetupComplete;
public event Action<int, string> OnWordAccepted;
public event Action<int, string> OnWordPlacedOnGrid;
```

**Key Methods:**

| Method | Purpose |
|--------|---------|
| `Activate() / Deactivate()` | Enable/disable input processing |
| `SelectWordRow(int index)` | Programmatic row selection |
| `CheckSetupComplete()` | Check and fire event if all placed |

**Keyboard Handling (Update):**
```csharp
private void ProcessKeyboardInput()
{
    var keyboard = Keyboard.current;
    if (keyboard.enterKey.wasPressedThisFrame)  HandleEnterKey();
    if (keyboard.backspaceKey.wasPressedThisFrame)  HandleBackspaceKey();
    if (keyboard.deleteKey.wasPressedThisFrame)  HandleDeleteKey();
    if (keyboard.escapeKey.wasPressedThisFrame)  HandleEscapeKey();
    if (keyboard.tabKey.wasPressedThisFrame)  HandleTabKey(keyboard.shiftKey.isPressed);
    if (_enableKeyboardLetterInput && !IsInputFieldFocused())
        ProcessLetterKeys(keyboard);
}
```

**Input Field Focus Check (Critical!):**
```csharp
private bool IsInputFieldFocused()
{
    var selected = EventSystem.current?.currentSelectedGameObject;
    if (selected == null) return false;
    return selected.GetComponent<TMP_InputField>() != null
        || selected.GetComponent<InputField>() != null;
}
```

**Tab Navigation (Skip Placed Rows):**
```csharp
private void HandleTabKey(bool shiftHeld)
{
    int attempts = 0;
    while (attempts < rowCount)
    {
        var row = _playerGridPanel.GetWordPatternRow(newIndex);
        if (row != null && !row.IsPlaced) break;  // Found unplaced row
        
        newIndex = shiftHeld 
            ? (newIndex <= 0 ? rowCount - 1 : newIndex - 1)
            : (newIndex >= rowCount - 1 ? 0 : newIndex + 1);
        attempts++;
    }
}
```

**Letter Key Mapping:**
```csharp
private Key GetKeyForLetter(char letter)
{
    switch (char.ToUpper(letter))
    {
        case 'A': return Key.A;
        case 'B': return Key.B;
        // ... all 26 letters
    }
}
```

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
| RowDisplayBuilder | Uses (static) |
| WordGuessInputController | Creates |
| TMP_Text, Button | Unity UI |

**States:**
```csharp
public enum RowState { Empty, Entering, WordEntered, Placed, Gameplay }
```

**Key Methods:**
- `SetRequiredLength(int)` - Configure word length
- `AddLetter(char)` / `RemoveLetter()` - Word entry
- `AcceptWord()` - Finalize entered word
- `RevealAllInstancesOfLetter(char)` - Gameplay reveals
- `EnterWordGuessMode()` / `ExitWordGuessMode()` - Guess mode

**Events:**
- `OnWordAccepted(int, string)`
- `OnWordGuessStarted(int, string)`
- `OnWordGuessSubmitted(int, string)`
- `OnInvalidWordRejected(int, string)`

---

## Layer 4: Services & Utilities (DETAILED)

### GuessProcessor.cs (~474 lines)
**Namespace:** `TecVooDoo.DontLoseYourHead.UI`  
**Location:** `Scripts/UI/Services/`  
**Purpose:** Central service for processing all guess types (letter, coordinate, word). Designed to work for BOTH player and AI - same logic, different targets.

**Architecture Pattern:** Callback Injection - external operations passed as delegates to avoid tight coupling.

| Dependencies | Direction |
|--------------|-----------|
| PlayerGridPanel | Reference (target panel) |
| WordPatternRow | Uses (via panel) |
| GridCellUI | Uses (via panel) |
| LetterButton.LetterState | Uses enum |

**Constructor Parameters (Dependency Injection):**
```csharp
public GuessProcessor(
    List<WordPlacementData> targetWords,  // Words to guess against
    PlayerGridPanel targetPanel,           // Panel to update
    string processorName,                  // "Player" or "Opponent" for logging
    Action onMissIncrement,                // Callback when miss occurs
    Action<char, LetterState> setLetterState,  // Update letter tracker
    Func<string, bool> validateWord,       // Word bank validation
    Action<string, bool> addToGuessedWordList  // Add to UI list
)
```

**Internal State (HashSets for O(1) lookup):**
```csharp
private HashSet<char> _knownLetters;       // Letters confirmed in words
private HashSet<char> _guessedLetters;     // All guessed letters
private HashSet<Vector2Int> _guessedCoordinates;  // All guessed coords
private HashSet<string> _guessedWords;     // All guessed words
private HashSet<int> _solvedWordRows;      // Rows with solved words
```

**Public Enums:**
```csharp
public enum GuessResult {
    Hit,            // Valid guess that hit
    Miss,           // Valid guess that missed  
    AlreadyGuessed, // Duplicate - don't end turn
    InvalidWord     // Not in dictionary - don't end turn
}
```

**Key Methods:**

| Method | Purpose | Returns |
|--------|---------|---------|
| `Initialize(int missLimit)` | Reset state for new game | void |
| `ProcessLetterGuess(char)` | Check letter against all words | GuessResult |
| `ProcessCoordinateGuess(int, int)` | Check coordinate for letter | GuessResult |
| `ProcessWordGuess(string, int)` | Check word against specific row | GuessResult |
| `HasExceededMissLimit()` | Check if game over | bool |

**Letter Guess Flow:**
```
ProcessLetterGuess(letter)
    |
    +-- Check _guessedLetters (duplicate?) --> AlreadyGuessed
    |
    +-- Add to _guessedLetters
    |
    +-- CheckLetterInWords(letter)
        |
        +-- Found: 
        |   +-- Add to _knownLetters
        |   +-- UpdatePanelForLetter() (reveal in word rows)
        |   +-- UpgradeGridCellsForLetter() (yellow -> green)
        |   +-- _setLetterState(letter, Hit)
        |   +-- Return Hit
        |
        +-- Not Found:
            +-- IncrementMisses(1)
            +-- _setLetterState(letter, Miss)
            +-- Return Miss
```

**Coordinate Guess Flow:**
```
ProcessCoordinateGuess(col, row)
    |
    +-- Check _guessedCoordinates (duplicate?) --> AlreadyGuessed
    |
    +-- Add to _guessedCoordinates
    |
    +-- FindLetterAtCoordinate(col, row)
        |
        +-- Found letter:
        |   +-- If _knownLetters.Contains(letter):
        |   |   +-- cell.MarkAsGuessed(true) [GREEN]
        |   |   +-- cell.RevealHiddenLetter()
        |   +-- Else:
        |   |   +-- cell.MarkAsHitButLetterUnknown() [YELLOW]
        |   +-- Return Hit
        |
        +-- No letter:
            +-- cell.MarkAsGuessed(false) [RED]
            +-- IncrementMisses(1)
            +-- Return Miss
```

**Word Guess Flow:**
```
ProcessWordGuess(word, rowIndex)
    |
    +-- _validateWord(word) --> false? Return InvalidWord
    |
    +-- Check _guessedWords (duplicate?) --> AlreadyGuessed
    |
    +-- Add to _guessedWords
    |
    +-- Compare to _targetWords[rowIndex]
        |
        +-- Match:
        |   +-- Add all letters to _knownLetters
        |   +-- Add rowIndex to _solvedWordRows
        |   +-- row.RevealAllLetters()
        |   +-- row.MarkWordSolved()
        |   +-- Update other rows for discovered letters
        |   +-- _addToGuessedWordList(word, true)
        |   +-- Return Hit
        |
        +-- No Match:
            +-- IncrementMisses(2) [DOUBLE PENALTY]
            +-- _addToGuessedWordList(word, false)
            +-- Return Miss
```

**Also Defines:**
```csharp
public class WordPlacementData {
    public string Word;
    public int StartCol;
    public int StartRow;
    public int DirCol;    // Direction: 0=horizontal, 1=vertical
    public int DirRow;
    public int RowIndex;  // Which word row this belongs to
}
```

**Usage Example (from GameplayUIController):**
```csharp
_playerGuessProcessor = new GuessProcessor(
    _opponentSetupData.PlacedWords,   // Target: opponent's words
    _opponentPanel,                    // Panel to update
    "Player",                          // For logging
    () => { _playerMisses++; UpdatePlayerMissCounter(); },
    (letter, state) => _opponentPanel.SetLetterState(letter, state),
    word => IsValidWord(word, word.Length),
    (word, correct) => _playerGuessedWordList.AddGuessedWord(word, correct)
);
```

---

### WordValidationService.cs (~95 lines)
**Namespace:** `TecVooDoo.DontLoseYourHead.UI`  
**Location:** `Scripts/UI/Services/`  
**Purpose:** Validates words against word bank lists. Provides random word selection for "Pick Random Words" feature.

**Architecture Pattern:** Simple service with constructor injection of dependencies.

| Dependencies | Direction |
|--------------|-----------|
| WordListSO (x4) | Injected via constructor |

**Constructor:**
```csharp
public WordValidationService(
    WordListSO threeLetterWords,
    WordListSO fourLetterWords,
    WordListSO fiveLetterWords,
    WordListSO sixLetterWords)
```

**Key Methods:**

| Method | Purpose | Returns |
|--------|---------|---------|
| `ValidateWord(string word, int requiredLength)` | Check word exists in bank | bool |
| `GetRandomWordOfLength(int length)` | Pick random word for auto-fill | string |
| `GetWordListForLength(int length)` | Get appropriate WordListSO | WordListSO |

**Validation Logic:**
```csharp
public bool ValidateWord(string word, int requiredLength)
{
    // 1. Check not empty
    // 2. Convert to uppercase
    // 3. Check length matches required
    // 4. Get correct WordListSO for length
    // 5. Check wordList.Contains(word)
}
```

**Word List Selection (switch expression):**
```csharp
public WordListSO GetWordListForLength(int length)
{
    return length switch
    {
        3 => _threeLetterWords,
        4 => _fourLetterWords,
        5 => _fiveLetterWords,
        6 => _sixLetterWords,
        _ => null
    };
}
```

**Usage Example (from SetupSettingsPanel):**
```csharp
_wordValidationService = new WordValidationService(
    threeLetterWords,
    fourLetterWords, 
    fiveLetterWords,
    sixLetterWords
);

bool isValid = _wordValidationService.ValidateWord("CAT", 3);
string randomWord = _wordValidationService.GetRandomWordOfLength(5);
```

---

### RowDisplayBuilder.cs (~207 lines)
**Namespace:** `TecVooDoo.DontLoseYourHead.UI.Utilities`  
**Location:** `Scripts/UI/Utilities/`  
**Purpose:** Static utility for building word pattern row display strings with rich text formatting. Pure functions with no side effects.

**Architecture Pattern:** Static utility class with shared StringBuilder to avoid allocations.

| Dependencies | Direction |
|--------------|-----------|
| None (pure static) | - |

**Data Structure:**
```csharp
public struct RowDisplayData
{
    public int RowNumber;           // 1-based row number
    public string NumberSeparator;  // e.g., ". "
    public char LetterSeparator;    // e.g., ' '
    public char UnknownLetterChar;  // e.g., '_'
    public RowState State;          // Current row state
    public string CurrentWord;      // The actual word
    public string EnteredText;      // Text being typed
    public int RequiredLength;      // Expected word length
    public bool[] RevealedLetters;  // Which positions revealed
    public bool InWordGuessMode;    // Currently guessing?
    public Func<int, char> GetGuessedLetterAt;  // Callback for guess letters
    public string GuessTypedLetterColorHex;     // Color for typed guesses

    public enum RowState {
        Empty,       // All underscores
        Entering,    // Typing in progress
        WordEntered, // Word complete, not placed
        Placed,      // Word placed on grid
        Gameplay     // During gameplay (reveals + guessing)
    }
}
```

**Key Method:**
```csharp
public static string Build(RowDisplayData data)
```

**Output Examples by State:**

| State | Input | Output |
|-------|-------|--------|
| Empty | length=5 | `1. _ _ _ _ _` |
| Entering | "CAT", length=5 | `1. <u>C</u> <u>A</u> <u>T</u> _ _` |
| WordEntered | "HORSE" | `1. <u>H</u> <u>O</u> <u>R</u> <u>S</u> <u>E</u>` |
| Gameplay (partial) | revealed=[T,F,T,F,F] | `1. <u>H</u> _ <u>R</u> _ _` |
| Gameplay (guessing) | guess="HO" at pos 0,1 | `1. <u>H</u> <color=#FF0000>O</color> _ _ _` |

**Build Flow:**
```
Build(data)
    |
    +-- Clear SharedBuilder
    |
    +-- Append row number + separator
    |
    +-- Switch on State:
        |
        +-- Empty: BuildEmptyState()
        |   +-- Loop: append underscore + separator
        |
        +-- Entering: BuildEnteringState()
        |   +-- Loop: entered chars get <u> tags, rest underscores
        |
        +-- WordEntered/Placed: BuildWordEnteredState()
        |   +-- Loop: all chars get <u> tags
        |
        +-- Gameplay: BuildGameplayState()
            +-- Loop:
                +-- If revealed: <u>letter</u>
                +-- Else if guessing + has guess: <color>guess</color>
                +-- Else: underscore
```

**Memory Optimization:**
```csharp
// Reusable StringBuilder to avoid allocations on repeated calls
private static readonly StringBuilder SharedBuilder = new StringBuilder(64);
```

**Usage Example (from WordPatternRow):**
```csharp
RowDisplayData data = new RowDisplayData
{
    RowNumber = _rowIndex + 1,
    NumberSeparator = ". ",
    LetterSeparator = ' ',
    UnknownLetterChar = '_',
    State = _currentState,
    CurrentWord = _currentWord,
    RequiredLength = _requiredLength,
    RevealedLetters = _revealedLetters,
    InWordGuessMode = _inWordGuessMode,
    GetGuessedLetterAt = GetGuessedLetterAtPosition,
    GuessTypedLetterColorHex = "FF6600"
};

_displayText.text = RowDisplayBuilder.Build(data);
```

---

## Layer 5: Core Data

### DifficultySO.cs
**Purpose:** ScriptableObject defining difficulty presets.

### WordListSO.cs  
**Purpose:** ScriptableObject containing word bank (List<string>).

### DifficultyCalculator.cs
**Purpose:** Static class calculating miss limits based on grid size, word count, difficulty.

### DifficultyEnums.cs
**Purpose:** Enum definitions for DifficultySetting, WordCountOption, GridSizeOption.

---

## Layer 6: AI System

*(See GDD for full design - implementation files created Dec 13)*

| Script | Lines | Purpose |
|--------|-------|---------|
| ExecutionerConfigSO.cs | ~120 | Tunable AI parameters |
| ExecutionerAI.cs | ~350 | Main AI controller |
| DifficultyAdapter.cs | ~200 | Rubber-banding system |
| MemoryManager.cs | ~100 | Skill-based memory |
| LetterGuessStrategy.cs | ~150 | Letter selection |
| CoordinateGuessStrategy.cs | ~180 | Coordinate selection |
| WordGuessStrategy.cs | ~120 | Word guess decisions |
| IGuessStrategy.cs | ~30 | Strategy interface |
| LetterFrequency.cs | ~50 | English letter frequencies |
| GridAnalyzer.cs | ~80 | Grid density calculations |
| AISetupManager.cs | ~100 | AI word generation |

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

### 4. Static Utility with Shared Resources
```csharp
public static class RowDisplayBuilder {
    private static readonly StringBuilder SharedBuilder = new StringBuilder(64);
    
    public static string Build(RowDisplayData data) {
        SharedBuilder.Clear();
        // Build string...
        return SharedBuilder.ToString();
    }
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

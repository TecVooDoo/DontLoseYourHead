# Don't Lose Your Head - Refactoring Instructions

**Version:** 1.0  
**Date Created:** December 11, 2025  
**Developer:** TecVooDoo LLC  

---

## Purpose

This document defines the standards and goals for refactoring the DLYH codebase. Reference this document at the start of every refactoring session.

---

## Platform

**Unity Version:** 6.3 LTS  
**Approach:** Utilize Unity 6.3 capabilities fully. This is a Unity project - use Unity types (Vector2Int, Color, etc.) where appropriate. Reference official Unity 6.3 documentation for current best practices.

---

## Dependency Management

**Preferred:** ScriptableObjects for shared data and configuration  
**Flexible:** Use whatever pattern works best for the specific use case:
- ScriptableObjects for persistent data, events, configuration
- Constructor injection for services that need dependencies
- Service Locator only if constructor injection becomes unwieldy
- Direct references where appropriate (e.g., UI component references)

**Avoid:** Singletons with static Instance properties (use ScriptableObjects instead)

---

## Primary Goals

### 1. Reduce Script Size for Tooling
**Problem:** Scripts over ~800 lines cause Claude/MCP timeouts and freezes.  
**Target:** No script exceeds 400-500 lines. Ideally under 300.

### 2. Future Readability
**Goal:** Return to any script after months away and immediately understand what it does.  
**Method:** Self-documenting code, clear naming, single responsibility.

### 3. Full Separation of Concerns
**Goal:** UI knows nothing about game logic. Game logic knows nothing about UI.  
**Method:** Communication through events, interfaces, and services.

### 4. Memory Efficiency
**Goal:** Minimize allocations, avoid unnecessary work, cache everything reusable.  
**Method:** Cache components, early-exit patterns, object pooling, allocation-free hot paths.

---

## Memory Efficiency Requirements

### Cache All GetComponent Calls

GetComponent allocates memory even when it finds nothing. Never call it repeatedly.

```csharp
// BAD - Allocates every frame
private void Update()
{
    Button button = GetComponent<Button>();
    button.interactable = _isReady;
}

// BAD - Allocates every call
public void UpdateDisplay()
{
    TextMeshProUGUI label = transform.Find("Label").GetComponent<TextMeshProUGUI>();
    label.text = _currentValue.ToString();
}

// GOOD - Cache in Awake or Start
private Button _button;
private TextMeshProUGUI _label;

private void Awake()
{
    _button = GetComponent<Button>();
    _label = transform.Find("Label").GetComponent<TextMeshProUGUI>();
}

private void Update()
{
    _button.interactable = _isReady;
}
```

### Early Exit in Update Methods

Don't run code that doesn't need to run.

```csharp
// BAD - Always processes keyboard even when not needed
private void Update()
{
    Keyboard keyboard = Keyboard.current;
    if (keyboard == null) return;
    
    for (int i = 0; i < 26; i++)
    {
        Key key = Key.A + i;
        if (keyboard[key].wasPressedThisFrame)
        {
            ProcessLetterInput((char)('A' + i));
        }
    }
}

// GOOD - Only process when in input mode
private bool _isAcceptingKeyboardInput = false;

private void Update()
{
    if (!_isAcceptingKeyboardInput) return;  // Early exit
    
    Keyboard keyboard = Keyboard.current;
    if (keyboard == null) return;
    
    for (int i = 0; i < 26; i++)
    {
        Key key = Key.A + i;
        if (keyboard[key].wasPressedThisFrame)
        {
            ProcessLetterInput((char)('A' + i));
            return;  // Exit after finding a key - only one per frame
        }
    }
}

public void EnableKeyboardInput() => _isAcceptingKeyboardInput = true;
public void DisableKeyboardInput() => _isAcceptingKeyboardInput = false;
```

### Disable Update When Not Needed

If a MonoBehaviour only needs Update sometimes, disable it.

```csharp
// GOOD - Component only enabled when actively needed
public class KeyboardInputHandler : MonoBehaviour
{
    private void OnEnable()
    {
        // Component was enabled - start listening
    }
    
    private void OnDisable()
    {
        // Component was disabled - stop listening
    }
    
    private void Update()
    {
        // Only runs when component is enabled
        ProcessKeyboardInput();
    }
}

// Usage from controller:
_keyboardHandler.enabled = true;   // Start listening
_keyboardHandler.enabled = false;  // Stop listening (Update no longer called)
```

### Avoid Allocations in Hot Paths

Hot paths = code that runs every frame or very frequently.

```csharp
// BAD - Allocates new list every call
public List<GridCellUI> GetAdjacentCells(int row, int col)
{
    List<GridCellUI> adjacent = new List<GridCellUI>();  // ALLOCATION
    // ... populate list
    return adjacent;
}

// GOOD - Reuse pre-allocated list
private readonly List<GridCellUI> _adjacentCellsBuffer = new List<GridCellUI>(8);

public List<GridCellUI> GetAdjacentCells(int row, int col)
{
    _adjacentCellsBuffer.Clear();  // No allocation
    // ... populate list
    return _adjacentCellsBuffer;
}

// ALSO GOOD - Let caller provide the buffer
public void GetAdjacentCells(int row, int col, List<GridCellUI> results)
{
    results.Clear();
    // ... populate list
}
```

### String Handling

Strings are immutable - concatenation creates garbage.

```csharp
// BAD - Creates multiple string allocations
private void UpdateCoordinateDisplay(int row, int col)
{
    string display = "(" + row + ", " + col + ")";  // Multiple allocations
    _label.text = display;
}

// BAD - Still allocates
private void UpdateCoordinateDisplay(int row, int col)
{
    _label.text = $"({row}, {col})";  // Allocates interpolated string
}

// GOOD - Use StringBuilder for complex strings (cache the builder)
private readonly StringBuilder _stringBuilder = new StringBuilder(32);

private void UpdateCoordinateDisplay(int row, int col)
{
    _stringBuilder.Clear();
    _stringBuilder.Append('(');
    _stringBuilder.Append(row);
    _stringBuilder.Append(", ");
    _stringBuilder.Append(col);
    _stringBuilder.Append(')');
    _label.text = _stringBuilder.ToString();
}

// BETTER - For simple cases, avoid strings entirely or cache them
private readonly string[] _rowLabels = { "A", "B", "C", "D", "E", "F", "G", "H", "I", "J", "K", "L" };

private string GetRowLabel(int row)
{
    return _rowLabels[row];  // No allocation - returns cached string
}
```

### Event Subscription Cleanup

Always unsubscribe to prevent memory leaks and zombie callbacks.

```csharp
// GOOD - Proper subscription lifecycle
private void OnEnable()
{
    _guessProcessor.OnLetterGuessProcessed += HandleLetterResult;
    _guessProcessor.OnWordGuessProcessed += HandleWordResult;
}

private void OnDisable()
{
    _guessProcessor.OnLetterGuessProcessed -= HandleLetterResult;
    _guessProcessor.OnWordGuessProcessed -= HandleWordResult;
}

// ALSO GOOD - For ScriptableObject events
private void OnEnable()
{
    _onGuessEvent.RegisterListener(this);
}

private void OnDisable()
{
    _onGuessEvent.UnregisterListener(this);
}
```

### Object Pooling for Frequently Created/Destroyed Objects

```csharp
// For grid cells, letter buttons, etc. that may be recreated
public class ComponentPool<T> where T : Component
{
    private readonly Queue<T> _available = new Queue<T>();
    private readonly T _prefab;
    private readonly Transform _parent;
    
    public ComponentPool(T prefab, Transform parent, int preWarmCount = 0)
    {
        _prefab = prefab;
        _parent = parent;
        
        for (int i = 0; i < preWarmCount; i++)
        {
            T instance = Object.Instantiate(_prefab, _parent);
            instance.gameObject.SetActive(false);
            _available.Enqueue(instance);
        }
    }
    
    public T Get()
    {
        T instance;
        if (_available.Count > 0)
        {
            instance = _available.Dequeue();
            instance.gameObject.SetActive(true);
        }
        else
        {
            instance = Object.Instantiate(_prefab, _parent);
        }
        return instance;
    }
    
    public void Return(T instance)
    {
        instance.gameObject.SetActive(false);
        _available.Enqueue(instance);
    }
}
```

### LINQ Avoidance in Hot Paths

LINQ is readable but allocates. Avoid in Update, frequently-called methods.

```csharp
// BAD - Allocates enumerator and potentially intermediate collections
private GridCellUI FindCellWithLetter(char letter)
{
    return _cells.FirstOrDefault(c => c.Letter == letter);
}

// GOOD - Simple loop, no allocation
private GridCellUI FindCellWithLetter(char letter)
{
    for (int i = 0; i < _cells.Length; i++)
    {
        if (_cells[i].Letter == letter)
        {
            return _cells[i];
        }
    }
    return null;
}

// LINQ is fine for:
// - Initialization code (runs once)
// - Editor scripts
// - Code that runs infrequently (button clicks, scene transitions)
```

### Unity-Specific Allocations to Avoid

```csharp
// BAD - Camera.main does a FindGameObjectWithTag internally
private void Update()
{
    Camera cam = Camera.main;  // Allocation + search every frame
}

// GOOD - Cache it
private Camera _mainCamera;

private void Awake()
{
    _mainCamera = Camera.main;
}

// BAD - CompareTag allocates, tag comparison doesn't
if (other.gameObject.tag == "Player")  // Allocates string

// GOOD
if (other.gameObject.CompareTag("Player"))  // No allocation

// BAD - GetComponentsInChildren allocates array every call
private void RefreshCells()
{
    GridCellUI[] cells = GetComponentsInChildren<GridCellUI>();
}

// GOOD - Cache or use non-alloc version
private GridCellUI[] _cells;

private void Awake()
{
    _cells = GetComponentsInChildren<GridCellUI>();
}

// Or use List version and reuse list
private readonly List<GridCellUI> _cellsList = new List<GridCellUI>();

private void RefreshCells()
{
    GetComponentsInChildren<GridCellUI>(_cellsList);  // Populates existing list
}
```

---

## Code Style Requirements

### No "var" - Explicit Types Always

```csharp
// BAD
var button = GetComponent<Button>();
var count = 5;
var player = PlayerManager.GetCurrentPlayer();

// GOOD
Button button = GetComponent<Button>();
int count = 5;
PlayerSO player = PlayerManager.GetCurrentPlayer();
```

**Rationale:** Instantly know what type you're dealing with without hovering or searching.

### Self-Documenting Code Over Comments

```csharp
// BAD
// Check if the word is valid
if (w.Length >= min && w.Length <= max && dict.Contains(w.ToUpper()))

// GOOD
bool isCorrectLength = word.Length >= minimumLength && word.Length <= maximumLength;
bool existsInDictionary = wordDictionary.Contains(word.ToUpper());
if (isCorrectLength && existsInDictionary)
```

**When comments ARE appropriate:**
- "Why" explanations (not "what")
- Unity-specific gotchas (lifecycle timing, etc.)
- Public API documentation (XML docs)

### Naming Conventions

| Element | Convention | Example |
|---------|------------|---------|
| Private fields | _camelCase | `_gridSize` |
| Public properties | PascalCase | `GridSize` |
| Methods | PascalCase verb | `ValidateWord()`, `GetCurrentPlayer()` |
| Booleans | is/has/can prefix | `isValid`, `hasPlacedWord`, `canSubmit` |
| Events | On + PastTense | `OnWordValidated`, `OnGuessSubmitted` |
| Interfaces | I + Noun | `IWordValidator`, `IGuessHandler` |

### Method Length

**Target:** Methods under 20 lines. If longer, extract helper methods.

```csharp
// BAD - 50 line method doing multiple things

// GOOD - Main method orchestrates, helpers do work
public void ProcessGuess(string guess)
{
    GuessType guessType = DetermineGuessType(guess);
    bool isValid = ValidateGuess(guess, guessType);
    
    if (isValid)
    {
        ApplyGuessResult(guess, guessType);
        UpdateUI();
    }
}
```

---

## Architecture Patterns

### Single Responsibility Services

Each service does ONE thing and does it well.

```csharp
// WordValidationService.cs - ONLY validates words
public class WordValidationService
{
    private readonly HashSet<string> _validWords;
    
    public WordValidationService(WordListSO wordList)
    {
        _validWords = new HashSet<string>(wordList.Words);
    }
    
    public bool IsValidWord(string word, int requiredLength)
    {
        if (string.IsNullOrEmpty(word)) return false;
        if (word.Length != requiredLength) return false;
        return _validWords.Contains(word.ToUpper());
    }
}
```

**Not responsible for:** UI feedback, word entry, storing guessed words.

### Event-Driven Communication

UI and game logic never call each other directly.

```csharp
// Game logic raises events
public class GuessProcessor
{
    public event Action<char, bool> OnLetterGuessProcessed;  // letter, wasHit
    public event Action<string, bool> OnWordGuessProcessed;  // word, wasCorrect
    
    public void ProcessLetterGuess(char letter)
    {
        bool wasHit = CheckLetterHit(letter);
        OnLetterGuessProcessed?.Invoke(letter, wasHit);
    }
}

// UI subscribes to events
public class LetterTrackerUI : MonoBehaviour
{
    private void OnEnable()
    {
        _guessProcessor.OnLetterGuessProcessed += HandleLetterResult;
    }
    
    private void HandleLetterResult(char letter, bool wasHit)
    {
        LetterButton button = GetButtonForLetter(letter);
        Color color = wasHit ? _hitColor : _missColor;
        button.SetColor(color);
    }
}
```

### Interface Segregation

Small, focused interfaces over large ones.

```csharp
// BAD - One interface doing everything
public interface IGameController
{
    void StartGame();
    void EndGame();
    void ProcessGuess(string guess);
    void UpdateUI();
    void SaveGame();
    void LoadGame();
}

// GOOD - Focused interfaces
public interface IGuessProcessor
{
    GuessResult ProcessLetterGuess(char letter);
    GuessResult ProcessCoordinateGuess(int row, int col);
    GuessResult ProcessWordGuess(string word, int rowIndex);
}

public interface IGameStateController
{
    void StartGame();
    void EndGame();
    GamePhase CurrentPhase { get; }
}

public interface ISaveSystem
{
    void SaveGame();
    void LoadGame();
}
```

### Generics Where Appropriate

```csharp
// Generic object pool
public class ObjectPool<T> where T : Component
{
    private readonly Queue<T> _pool = new Queue<T>();
    private readonly T _prefab;
    private readonly Transform _parent;
    
    public T Get()
    {
        return _pool.Count > 0 ? _pool.Dequeue() : CreateNew();
    }
    
    public void Return(T item)
    {
        item.gameObject.SetActive(false);
        _pool.Enqueue(item);
    }
}

// Usage
ObjectPool<GridCellUI> cellPool = new ObjectPool<GridCellUI>(cellPrefab, gridContainer);
GridCellUI cell = cellPool.Get();
```

### Pattern Selection Guide

| Situation | Pattern | Example |
|-----------|---------|---------|
| One class, many consumers | Events/Observer | GuessProcessor raising OnGuessProcessed |
| Stateless operations | Service class | WordValidationService |
| Complex object creation | Factory | GridCellFactory |
| Shared state access | ScriptableObject | PlayerSO, DifficultySO |
| Behavior that varies | Strategy | IDifficultyCalculator implementations |
| Step-by-step process | Command | UndoableGuessCommand |
| Global singleton need | Service Locator | ServiceLocator.Get<IGuessProcessor>() |

---

## Layer Separation

### Layer 1: Core (No Unity dependencies where possible)
- Pure C# classes
- Game rules, validation, calculations
- Can be unit tested without Unity

```
Core/
  Services/
    WordValidationService.cs
    DifficultyCalculator.cs
    GuessProcessor.cs
  Models/
    GridModel.cs
    WordModel.cs
  Interfaces/
    IWordValidator.cs
    IGuessProcessor.cs
```

### Layer 2: Unity Integration
- MonoBehaviours that bridge Core and UI
- ScriptableObjects for data
- Managers that coordinate systems

```
Managers/
  GameManager.cs
  TurnManager.cs
  PlayerManager.cs
Data/
  PlayerSO.cs
  DifficultySO.cs
  WordListSO.cs
```

### Layer 3: UI (No game logic)
- Display only
- User input capture
- Raises events, doesn't process them

```
UI/
  Components/
    GridCellUI.cs
    LetterButtonUI.cs
    WordPatternRowUI.cs
  Panels/
    SetupPanelUI.cs
    GameplayPanelUI.cs
  Controllers/
    SetupUIController.cs
    GameplayUIController.cs
```

### Communication Flow

```
User clicks button
    -> UI captures click, raises event
    -> Controller receives event, calls Service
    -> Service processes, returns result
    -> Controller raises result event
    -> UI receives result event, updates display
```

**UI never:**
- Validates words
- Calculates miss limits
- Determines win/lose
- Knows about opponent's data

**Core never:**
- References MonoBehaviour
- Accesses UI components
- Uses Unity-specific types (where avoidable)

---

## File Organization

### Target Structure

```
Assets/DLYH/Scripts/
  Core/
    Interfaces/
      IWordValidator.cs
      IGuessProcessor.cs
      IGridModel.cs
    Services/
      WordValidationService.cs
      GuessProcessingService.cs
      DifficultyCalculationService.cs
    Models/
      GridModel.cs
      WordModel.cs
      GuessResult.cs
    Enums/
      GuessType.cs
      GamePhase.cs
      CellState.cs
  
  Data/
    ScriptableObjects/
      PlayerSO.cs
      DifficultySO.cs
      WordListSO.cs
      GameEventSO.cs
  
  Managers/
    GameManager.cs
    TurnManager.cs
    PlayerManager.cs
    GameStateMachine.cs
  
  UI/
    Base/
      UIPanel.cs
      UIButton.cs
    Components/
      GridCellUI.cs
      LetterButtonUI.cs
      WordPatternRowUI.cs
    Panels/
      SetupPanelUI.cs
      GameplayPanelUI.cs
    Controllers/
      SetupUIController.cs
      GameplayUIController.cs
      LetterTrackerController.cs
      GridDisplayController.cs
```

### Naming Files

- Services end in `Service`: `WordValidationService.cs`
- UI components end in `UI`: `GridCellUI.cs`
- Controllers end in `Controller`: `SetupUIController.cs`
- Interfaces start with `I`: `IWordValidator.cs`
- ScriptableObjects end in `SO`: `PlayerSO.cs`

---

## Refactoring Process

### Per-Session Workflow

1. **Start:** State which script you're refactoring
2. **Upload:** Provide current version of the file
3. **Scope:** Define ONE extraction (e.g., "Extract WordPatternRowManager")
4. **Execute:** Make the extraction
5. **Verify:** Confirm compilation, test basic functionality
6. **Commit:** Git commit with clear message
7. **Document:** Update analysis doc with completion status

### Extraction Checklist

Before extracting, verify:
- [ ] New class has single responsibility
- [ ] No "var" in new code
- [ ] Methods under 20 lines
- [ ] Explicit types everywhere
- [ ] Events for communication (not direct calls)
- [ ] Interface defined if class will have multiple consumers

After extracting, verify:
- [ ] Original file compiles
- [ ] New file compiles
- [ ] Basic functionality still works
- [ ] No circular dependencies introduced

---

## Anti-Patterns to Eliminate

### God Objects
**Symptom:** One class that knows/does everything  
**Fix:** Extract focused services and controllers

### Spaghetti Events
**Symptom:** Events calling events calling events, hard to trace  
**Fix:** Clear event naming, document flow, consider mediator pattern

### Primitive Obsession
**Symptom:** Passing around ints and strings instead of types  
**Fix:** Create small value types

```csharp
// BAD
void ProcessGuess(int row, int col, string word, int playerIndex)

// GOOD
void ProcessGuess(Coordinate coord, Word word, PlayerId player)
```

### Feature Envy
**Symptom:** Method uses more of another class's data than its own  
**Fix:** Move method to the class whose data it uses

### Long Parameter Lists
**Symptom:** Methods with 4+ parameters  
**Fix:** Create parameter objects or builder pattern

```csharp
// BAD
void ConfigurePanel(string name, Color color, int gridSize, int wordCount, 
                    Difficulty diff, bool isOwner, PlayerSO player)

// GOOD
void ConfigurePanel(PanelConfiguration config)
```

---

## Success Metrics

After refactoring is complete:

| Metric | Target |
|--------|--------|
| Largest script | Under 400 lines |
| Average script | Under 200 lines |
| Methods over 20 lines | 0 |
| Uses of "var" | 0 |
| Direct UI-to-logic calls | 0 |
| Time to understand any script | Under 2 minutes |
| MCP timeout frequency | Rare |

---

## Reference: Current Problem Scripts

| Script | Current Lines | Target Lines | Priority |
|--------|---------------|--------------|----------|
| PlayerGridPanel.cs | ~1,871 | ~300 | HIGH |
| GameplayUIController.cs | ~1,600 | ~300 | HIGH |
| WordPatternRow.cs | ~800 | ~200 | MEDIUM |
| SetupSettingsPanel.cs | ~760 | ~200 | MEDIUM |
| GridCellUI.cs | ~250 | ~150 | LOW |

---

**End of Refactoring Instructions**

Reference this document at the start of every refactoring session.

# GameplayUIController Refactoring Analysis

**File:** `Assets/DLYH/Scripts/UI/GameplayUIController.cs`  
**Current Lines:** 2,112  
**Target Lines:** ~400-500 (main controller)  
**Analysis Date:** December 12, 2025  
**Analyzer:** Claude + Rune  

---

## Progress Status

| Extraction | Status | Est. Lines | Priority | Notes |
|------------|--------|------------|----------|-------|
| PlayerGuessProcessor | PENDING | ~360 | HIGH | Largest responsibility |
| OpponentGuessProcessor | PENDING | ~380 | HIGH | Mirror of player processing |
| WordGuessModeController | PENDING | ~175 | HIGH | Complex state management |
| TestingHelper | PENDING | ~194 | MEDIUM | Odin Inspector test buttons |
| PanelConfigurationManager | PENDING | ~180 | MEDIUM | Setup-to-gameplay transfer |
| TurnController | PENDING | ~100 | LOW | Turn switching logic |
| MissCounterController | PENDING | ~90 | LOW | Counter updates |

**Estimated total extractable:** ~1,480 lines (70% reduction potential)

---

## Executive Summary

GameplayUIController is currently a "God Object" at 2,112 lines managing 20 distinct regions/responsibilities. This is the largest script in the project and the next priority after completing PlayerGridPanel.

**Key Problem:** Player and Opponent guess processing are nearly identical (~360 and ~380 lines respectively). This is a clear candidate for a generic `GuessProcessor` service with player/opponent configuration.

---

## Current Structure Overview

```
GameplayUIController (2,112 lines)
    |
    +-- Serialized Fields (32 lines)
    |   +-- Container References
    |   +-- Panel References
    |   +-- Miss Counter References
    |   +-- Word Bank References
    |   +-- Guessed Word List References
    |
    +-- Private Fields (10 lines)
    |   +-- Setup data references
    |
    +-- Player State Tracking (16 lines)
    |   +-- Turn management
    |   +-- Miss tracking
    |   +-- Known/Guessed letters
    |   +-- Guessed coordinates/words
    |
    +-- Opponent State Tracking (10 lines)
    |   +-- Mirror of player tracking
    |
    +-- Word Guess Mode State (11 lines)
    |   +-- Active row tracking
    |   +-- Saved letter states
    |   +-- Keyboard mode flag
    |
    +-- Guess Result Enum (13 lines)
    |
    +-- Testing Region (194 lines) - EXTRACT
    |   +-- Odin Inspector test buttons
    |   +-- Simulate opponent turns
    |   +-- Debug display methods
    |
    +-- Data Structures (29 lines)
    |   +-- SetupData class
    |   +-- PlacedWordData class
    |
    +-- Unity Lifecycle (87 lines)
    |   +-- Awake/Start/OnEnable/OnDisable
    |   +-- Update (keyboard input)
    |
    +-- Public Methods (92 lines)
    |   +-- StartGameplay()
    |   +-- ReturnToSetup()
    |
    +-- Data Capture (115 lines)
    |   +-- CaptureSetupData()
    |   +-- GenerateOpponentData()
    |
    +-- Panel Configuration (181 lines) - CANDIDATE
    |   +-- ConfigureOwnerPanel()
    |   +-- ConfigureOpponentPanel()
    |   +-- Word/Grid placement
    |
    +-- Event Handling (150 lines)
    |   +-- Opponent panel click handlers
    |   +-- Letter/Coordinate/Word guess routing
    |
    +-- Word Guess Mode Event Handlers (175 lines) - EXTRACT
    |   +-- OnOpponentGuessWordClicked
    |   +-- OnOpponentGuessWordBackspace
    |   +-- OnOpponentGuessWordAccept
    |   +-- OnOpponentGuessWordCancel
    |
    +-- Letter Tracker Keyboard Mode (43 lines)
    |   +-- EnterLetterTrackerKeyboardMode()
    |   +-- ExitLetterTrackerKeyboardMode()
    |
    +-- Feedback Display (28 lines)
    |   +-- ShowFeedbackMessage()
    |   +-- HideFeedbackMessage()
    |
    +-- Turn Management (40 lines)
    |   +-- EndPlayerTurn()
    |   +-- EndOpponentTurn()
    |
    +-- Miss Counter (88 lines)
    |   +-- CalculateMissLimit()
    |   +-- UpdatePlayerMissCounter()
    |   +-- UpdateOpponentMissCounter()
    |
    +-- Player Guess Processing (360 lines) - EXTRACT
    |   +-- InitializePlayerState()
    |   +-- ProcessPlayerLetterGuess()
    |   +-- ProcessPlayerCoordinateGuess()
    |   +-- ProcessPlayerWordGuess()
    |   +-- UpdateOpponentPanelForLetter()
    |   +-- UpgradeOpponentGridCellsForLetter()
    |
    +-- Opponent Guess Processing (381 lines) - EXTRACT
        +-- InitializeOpponentState()
        +-- ProcessOpponentLetterGuess()
        +-- ProcessOpponentCoordinateGuess()
        +-- ProcessOpponentWordGuess()
        +-- UpdateOwnerPanelForLetter()
        +-- UpgradeOwnerGridCellsForLetter()
```

---

## Recommended Extractions

### Priority 1: GuessProcessor Service (~740 lines combined)

**Problem:** Player and Opponent guess processing are nearly identical code with different variable names.

**Solution:** Create a generic `GuessProcessor` class that can be configured for either player or opponent:

```csharp
public class GuessProcessor
{
    // Configuration
    private readonly SetupData _targetData;      // Words to guess against
    private readonly PlayerGridPanel _targetPanel;  // Panel to update
    private readonly Action<int> _onMissIncrement;   // Callback for miss updates
    
    // State
    private HashSet<char> _knownLetters = new HashSet<char>();
    private HashSet<char> _guessedLetters = new HashSet<char>();
    private HashSet<Vector2Int> _guessedCoordinates = new HashSet<Vector2Int>();
    private HashSet<string> _guessedWords = new HashSet<string>();
    private HashSet<int> _solvedWordRows = new HashSet<int>();
    
    public GuessProcessor(SetupData targetData, PlayerGridPanel targetPanel, 
                          Action<int> onMissIncrement)
    {
        _targetData = targetData;
        _targetPanel = targetPanel;
        _onMissIncrement = onMissIncrement;
    }
    
    public GuessResult ProcessLetterGuess(char letter) { ... }
    public GuessResult ProcessCoordinateGuess(int col, int row) { ... }
    public GuessResult ProcessWordGuess(string word, int rowIndex) { ... }
    
    // Helper methods
    private void UpdatePanelForLetter(char letter) { ... }
    private void UpgradeGridCellsForLetter(char letter) { ... }
}
```

**Usage in GameplayUIController:**
```csharp
private GuessProcessor _playerGuessProcessor;
private GuessProcessor _opponentGuessProcessor;

private void InitializeGuessProcessors()
{
    _playerGuessProcessor = new GuessProcessor(
        _opponentSetupData,  // Player guesses against opponent's words
        _opponentPanel,
        missCount => { _playerMisses = missCount; UpdatePlayerMissCounter(); }
    );
    
    _opponentGuessProcessor = new GuessProcessor(
        _playerSetupData,  // Opponent guesses against player's words
        _ownerPanel,
        missCount => { _opponentMisses = missCount; UpdateOpponentMissCounter(); }
    );
}
```

**Impact:** Reduces ~740 lines of duplicate code to ~400 lines in one reusable service.

---

### Priority 2: WordGuessModeController (~175 lines)

**Responsibility:** Manages the word guess mode state machine.

**Methods to extract:**
- `OnOpponentGuessWordClicked(int rowNumber)`
- `OnOpponentGuessWordBackspace(int rowNumber)`
- `OnOpponentGuessWordAccept(int rowNumber)`
- `OnOpponentGuessWordCancel(int rowNumber)`
- `EnterLetterTrackerKeyboardMode()`
- `ExitLetterTrackerKeyboardMode()`

**Events:**
- `OnWordGuessAccepted(string word, int rowIndex)`
- `OnWordGuessCancelled()`

---

### Priority 3: TestingHelper (~194 lines)

**Problem:** Testing/debugging code mixed with production code.

**Solution:** Extract to Editor-only script or conditional compilation:

```csharp
#if UNITY_EDITOR
public class GameplayTestingHelper
{
    [Button("Switch to Player Turn")]
    public void TestSwitchToPlayerTurn() { ... }
    
    [Button("Simulate Letter Guess")]
    public void TestSimulateLetterGuess() { ... }
    
    // ... all testing buttons
}
#endif
```

**Alternative:** Keep in main class but wrap in `#if UNITY_EDITOR` blocks.

---

### Priority 4: PanelConfigurationManager (~180 lines)

**Methods to extract:**
- `ConfigureOwnerPanel()`
- `ConfigureOpponentPanel()`
- Helper methods for word placement

**Dependencies:**
- SetupData
- PlayerGridPanel references

---

## Extraction Order (Recommended)

### Phase 1: High Impact (reduce ~740 lines)
1. **GuessProcessor** - Combine Player/Opponent processing into generic service
   - Create `GuessProcessor.cs` (~400 lines)
   - Replace ~740 lines of duplicate code
   - Net reduction: ~340 lines

### Phase 2: Separation of Concerns
2. **WordGuessModeController** (~175 lines)
   - Extract word guess mode state machine
   - Clean interface between UI and game logic

3. **TestingHelper** (~194 lines)
   - Move to Editor folder or use conditional compilation
   - Keeps production code clean

### Phase 3: Optional
4. **PanelConfigurationManager** (~180 lines) - If still too large
5. **TurnController** (~100 lines) - If needed
6. **MissCounterController** (~90 lines) - If needed

---

## Estimated Final Structure

```
GameplayUIController.cs (~400-500 lines)
    |-- Container/panel management
    |-- Lifecycle methods
    |-- Processor initialization
    |-- Event routing
    |-- Turn orchestration
    |-- Win/Lose detection

Services/
    |-- GuessProcessor.cs (~400 lines) - Generic guess handling

Controllers/
    |-- WordGuessModeController.cs (~175 lines)
    |-- PanelConfigurationManager.cs (~180 lines) - optional

Editor/ (or #if UNITY_EDITOR)
    |-- GameplayTestingHelper.cs (~194 lines)
```

---

## Key Patterns to Apply

### 1. Generic Service Pattern (GuessProcessor)

Avoid code duplication by parameterizing the differences:
```csharp
// Instead of ProcessPlayerLetterGuess() and ProcessOpponentLetterGuess()
public GuessResult ProcessLetterGuess(char letter)
{
    // Same logic, different data sources passed via constructor
}
```

### 2. Callback Injection

Pass callbacks for side effects instead of direct references:
```csharp
public GuessProcessor(
    SetupData targetData,
    Func<int, int, GridCellUI> getCellFunc,  // Get cell by coordinate
    Action<char, LetterState> setLetterStateFunc,  // Update letter tracker
    Action<int> onMissIncrement  // Increment miss counter
)
```

### 3. Event-Based UI Updates

GuessProcessor raises events, controller routes to appropriate panel:
```csharp
public event Action<char, bool> OnLetterGuessProcessed;
public event Action<int, int, bool, char?> OnCoordinateGuessProcessed;
public event Action<string, int, bool> OnWordGuessProcessed;
```

---

## Dependencies to Consider

### Word Bank References
Currently serialized directly. Consider:
- Creating a `WordBankService` that encapsulates validation
- Injecting via constructor to GuessProcessor

### Panel References
GuessProcessor needs to update panels. Options:
1. Pass panel references directly (simpler)
2. Pass callback functions (more flexible)
3. Raise events, let controller route (cleanest separation)

### Miss Limit Calculation
Currently in `CalculateMissLimit()`. Consider:
- Keep in GameplayUIController (orchestration responsibility)
- Or move to `DifficultyCalculator` service (already exists in Core/)

---

## Notes for Next Session

1. **Start with GuessProcessor** - Biggest impact, clearest extraction
2. **Test thoroughly** - Guess processing is core gameplay
3. **Consider generic approach** - Avoid duplicating player/opponent code
4. **Preserve Odin attributes** - Testing buttons are useful during development
5. **Apply lifecycle fix pattern** - If panels activated before Start(), use EnsureInitialized()

---

## Comparison: Before/After Estimates

| Metric | Current | After Phase 1 | After All |
|--------|---------|---------------|-----------|
| GameplayUIController | 2,112 | ~1,370 | ~500 |
| New files created | 0 | 1 | 4 |
| Code duplication | High | Low | Minimal |
| Testability | Low | Medium | High |

---

**End of Analysis Document**

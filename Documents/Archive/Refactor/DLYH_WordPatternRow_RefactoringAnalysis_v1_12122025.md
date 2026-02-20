# WordPatternRow Refactoring Analysis

**File:** `Assets/DLYH/Scripts/UI/WordPatternRow.cs`  
**Current Lines:** 1,379  
**Target Lines:** ~300-400  
**Analysis Date:** December 12, 2025  
**Status:** PENDING  
**Analyzer:** Claude + Rune  

---

## Executive Summary

WordPatternRow.cs is a large MonoBehaviour (1,379 lines) that manages individual word rows in both Setup Mode and Gameplay Mode. It handles multiple responsibilities: word entry, validation, display rendering, button state management, and word guess mode. The file is a good candidate for extraction of focused controllers, particularly the word guess mode logic which was previously extracted from GameplayUIController.

**Recommended Reduction:** ~900 lines (65% reduction)  
**Priority:** HIGH - Third largest script after completed refactoring

---

## Current Structure Analysis

### Line Count by Region

| Region | Lines | % of Total | Notes |
|--------|-------|------------|-------|
| Enums | ~15 | 1% | RowState enum - keep inline |
| Serialized Fields | ~70 | 5% | UI references, config - must stay |
| Private Fields | ~25 | 2% | State tracking - must stay |
| Events | ~45 | 3% | 8 events - must stay |
| Properties | ~55 | 4% | Public accessors - must stay |
| Unity Lifecycle | ~40 | 3% | Awake, Start, etc. - must stay |
| IPointerClickHandler | ~15 | 1% | Interface impl - must stay |
| Initialization Methods | ~35 | 3% | Initialize, SetRequiredLength - must stay |
| **Word Entry (Setup)** | **~175** | **13%** | **EXTRACTION CANDIDATE** |
| **Letter Reveal (Gameplay)** | **~115** | **8%** | Moderate - could stay |
| **Word Guess Mode** | **~230** | **17%** | **EXTRACTION CANDIDATE** |
| **Word Guess Helpers** | **~75** | **5%** | Goes with Word Guess Mode |
| **State Management** | **~75** | **5%** | Button states - **EXTRACTION CANDIDATE** |
| **Display Building** | **~100** | **7%** | **EXTRACTION CANDIDATE** |
| **Button Events** | **~110** | **8%** | **EXTRACTION CANDIDATE** |
| Editor Helpers | ~60 | 4% | #if UNITY_EDITOR - can stay |

**Total Extractable:** ~765 lines (55%)

---

## Identified Responsibilities

WordPatternRow currently handles **6 distinct responsibilities**:

### 1. Word Entry (Setup Mode)
**Methods:** AddLetter, RemoveLastLetter, SetEnteredText, AcceptWord, ClearWord, MarkAsPlaced, ResetToEmpty, ResetToWordEntered, Select, Deselect
**Lines:** ~175
**Coupling:** Uses _enteredText, _currentWord, _wordValidator, fires OnWordTextChanged/OnWordAccepted/OnInvalidWordRejected

### 2. Letter Reveal (Gameplay Mode)
**Methods:** SetGameplayWord, RevealLetter, RevealAllInstancesOfLetter, RevealAllLetters, IsFullyRevealed, ResetRevealedLetters
**Lines:** ~115
**Coupling:** Uses _revealedLetters, _currentWord

### 3. Word Guess Mode
**Methods:** EnterWordGuessMode, ExitWordGuessMode, TypeGuessLetter, BackspaceGuessLetter, GetFullGuessWord, IsGuessComplete, HideGuessWordButton, ShowGuessWordButton, SetAsOwnerPanel, MarkWordSolved, HideAllGuessButtons
**Lines:** ~230 + ~75 helpers = **305 total**
**Coupling:** Uses _inWordGuessMode, _guessedLetters, _guessCursorPosition, _wordSolved, _isOwnerPanel

### 4. Display Rendering
**Methods:** UpdateDisplay, BuildDisplayText, BuildGameplayDisplayText
**Lines:** ~100
**Coupling:** Uses StringBuilder, all state fields for display

### 5. Button State Management
**Methods:** UpdateButtonStates, UpdateGuessButtonStates, SubscribeToButtons, UnsubscribeFromButtons
**Lines:** ~110
**Coupling:** All button references, _currentState, _inWordGuessMode

### 6. Button Click Handlers
**Methods:** HandleSelectClick, HandleCoordinateModeClick, HandleDeleteClick, HandleGuessWordClick, HandleGuessBackspaceClick, HandleGuessAcceptClick, HandleGuessCancelClick
**Lines:** ~50
**Coupling:** Fires events, calls mode methods

---

## Proposed Extractions

### Priority 1: WordGuessInputController (~305 lines)

**Rationale:** Word guess mode is a complete state machine that was already extracted on the GameplayUIController side. Extract the row-side logic to match.

**Methods to Extract:**
- EnterWordGuessMode()
- ExitWordGuessMode(bool submit)
- TypeGuessLetter(char letter)
- BackspaceGuessLetter()
- GetFullGuessWord()
- IsGuessComplete()
- ClearGuessedLetters()
- FindNextUnrevealedPosition(int fromPosition)
- FindPreviousUnrevealedPosition(int fromPosition)

**State to Move:**
- _inWordGuessMode
- _guessedLetters
- _guessCursorPosition

**Events to Expose:**
- OnWordGuessStarted
- OnWordGuessSubmitted
- OnWordGuessCancelled

**Architecture:**
```csharp
public class WordGuessInputController
{
    // Events
    public event Action OnGuessStarted;
    public event Action<string> OnGuessSubmitted;
    public event Action OnGuessCancelled;
    
    // Dependencies via constructor
    private readonly Func<int, bool> _isLetterRevealed;
    private readonly Func<int, char> _getRevealedLetter;
    private readonly int _wordLength;
    
    // State
    private bool _isActive;
    private char[] _guessedLetters;
    private int _cursorPosition;
    
    public WordGuessInputController(int wordLength, 
        Func<int, bool> isLetterRevealed,
        Func<int, char> getRevealedLetter)
    {
        _wordLength = wordLength;
        _isLetterRevealed = isLetterRevealed;
        _getRevealedLetter = getRevealedLetter;
        _guessedLetters = new char[wordLength];
    }
    
    public void Enter() { ... }
    public void Exit(bool submit) { ... }
    public bool TypeLetter(char letter) { ... }
    public bool Backspace() { ... }
    public string GetFullWord() { ... }
    public bool IsComplete() { ... }
}
```

**Estimated Reduction:** ~305 lines -> ~50 lines (delegation code)

---

### Priority 2: RowDisplayBuilder (~100 lines)

**Rationale:** Display building is pure logic with no side effects. Easy to extract and test.

**Methods to Extract:**
- BuildDisplayText()
- BuildGameplayDisplayText(StringBuilder sb)

**Architecture:**
```csharp
public static class RowDisplayBuilder
{
    public static string Build(RowDisplayData data)
    {
        // Pure function - no side effects
    }
}

public struct RowDisplayData
{
    public int RowNumber;
    public string NumberSeparator;
    public char LetterSeparator;
    public char UnknownLetterChar;
    public RowState State;
    public string CurrentWord;
    public string EnteredText;
    public int RequiredLength;
    public bool[] RevealedLetters;
    public bool InWordGuessMode;
    public char[] GuessedLetters;
    public Color GuessTypedLetterColor;
}
```

**Estimated Reduction:** ~100 lines -> ~15 lines (call site)

---

### Priority 3: RowButtonStateManager (~110 lines)

**Rationale:** Button state logic is complex with many conditions. Extracting clarifies the rules.

**Methods to Extract:**
- UpdateButtonStates()
- UpdateGuessButtonStates()
- SubscribeToButtons()
- UnsubscribeFromButtons()

**Architecture:**
```csharp
public class RowButtonStateManager
{
    private readonly Button _selectButton;
    private readonly Button _coordinateModeButton;
    private readonly Button _deleteButton;
    private readonly Button _guessWordButton;
    private readonly Button _guessBackspaceButton;
    private readonly Button _guessAcceptButton;
    private readonly Button _guessCancelButton;
    
    // Callbacks for button clicks
    public event Action OnSelectClicked;
    public event Action OnCoordinateModeClicked;
    public event Action OnDeleteClicked;
    public event Action OnGuessWordClicked;
    public event Action OnGuessBackspaceClicked;
    public event Action OnGuessAcceptClicked;
    public event Action OnGuessCancelClicked;
    
    public void UpdateForState(RowState state, bool isOwnerPanel, 
        bool inWordGuessMode, bool wordSolved) { ... }
    
    public void SubscribeAll() { ... }
    public void UnsubscribeAll() { ... }
}
```

**Estimated Reduction:** ~110 lines -> ~30 lines (delegation)

---

### Priority 4: SetupModeWordEntry (~175 lines) - OPTIONAL

**Rationale:** Lower priority since Setup Mode is complete and working. Only extract if needed for maintenance.

**Methods:**
- AddLetter(char letter)
- RemoveLastLetter()
- SetEnteredText(string word)
- AcceptWord()
- ClearWord()

**Estimated Reduction:** ~175 lines -> ~40 lines (delegation)

---

## Code Quality Issues

### Debug.Log Cleanup Needed

**Current:** 25+ Debug.Log statements throughout the file
**Action:** Remove verbose development logs, keep only errors/warnings

**Examples to Remove:**
```csharp
Debug.Log($"[WordPatternRow] Row {_rowNumber}: Added letter '{letter}', now: {_enteredText}");
Debug.Log($"[WordPatternRow] Row {_rowNumber} selected");
Debug.Log($"[WordPatternRow] Row {_rowNumber} guess word button clicked");
```

**Keep:**
```csharp
Debug.LogWarning($"[WordPatternRow] Row {_rowNumber}: Invalid word rejected: {rejectedWord}");
```

### Duplicate Null Check Pattern

**Issue:** Button null checks repeated in multiple methods

**Current Pattern (repeated ~20 times):**
```csharp
if (_guessWordButton != null) _guessWordButton.gameObject.SetActive(false);
if (_guessBackspaceButton != null) _guessBackspaceButton.gameObject.SetActive(false);
if (_guessAcceptButton != null) _guessAcceptButton.gameObject.SetActive(false);
if (_guessCancelButton != null) _guessCancelButton.gameObject.SetActive(false);
```

**Solution:** Extract to helper method or RowButtonStateManager

### Long Methods

| Method | Lines | Action |
|--------|-------|--------|
| SetGameplayWord | 30 | Extract button hiding to helper |
| AddLetter | 35 | Acceptable - complex validation |
| SetEnteredText | 45 | Could split validation/acceptance |
| BackspaceGuessLetter | 35 | Acceptable - state machine logic |

---

## Extraction Order

| Order | Extraction | Est. Lines | Cumulative Reduction |
|-------|------------|------------|---------------------|
| 1 | WordGuessInputController | ~305 | 22% |
| 2 | RowDisplayBuilder | ~100 | 29% |
| 3 | RowButtonStateManager | ~110 | 37% |
| 4 | Debug Log Cleanup | ~50 | 41% |
| - | SetupModeWordEntry (optional) | ~175 | 54% |

**Projected Final Size:** ~550-650 lines (without optional extraction)

---

## Dependencies and Risks

### Internal Dependencies
- WordGuessInputController needs access to _revealedLetters (via callbacks)
- RowDisplayBuilder needs all display-related state (via struct)
- RowButtonStateManager needs button references (via constructor)

### External Dependencies
- Events subscribed by PlayerGridPanel and GameplayUIController
- WordPatternRowManager caches references to rows

### Risks
- **Low:** Extractions are internal refactoring, public API unchanged
- **Medium:** Button state logic is complex - thorough testing needed
- **Low:** Display builder is pure function - easy to test

---

## Metrics Summary

| Metric | Current | Target | After Extraction |
|--------|---------|--------|------------------|
| Total Lines | 1,379 | ~300-400 | ~550-650 |
| Methods > 20 lines | 5 | 0 | ~2 |
| Debug.Log statements | 25+ | ~5 | ~5 |
| Responsibilities | 6 | 2-3 | 2-3 |

---

## Comparison with Completed Refactorings

| Script | Original | Final | Reduction | Sessions |
|--------|----------|-------|-----------|----------|
| PlayerGridPanel | 2,192 | 1,120 | 49% | 3 |
| GameplayUIController | 2,112 | 1,173 | 44% | 2 |
| **WordPatternRow** | **1,379** | **~600** | **~56%** | **Est. 2** |

---

## Recommended First Session

**Focus:** WordGuessInputController extraction

**Steps:**
1. Create `Assets/DLYH/Scripts/UI/Controllers/WordGuessInputController.cs`
2. Move word guess state and methods
3. Update WordPatternRow to use controller
4. Verify compilation
5. Test word guess functionality
6. Commit

**Estimated Time:** 1-2 hours

---

## File Structure After Refactoring

```
Assets/DLYH/Scripts/UI/
    |-- WordPatternRow.cs (~550-650 lines) - Row management, state, events
    |-- Controllers/
        |-- WordGuessInputController.cs (~150 lines) - Word guess state machine
        |-- RowButtonStateManager.cs (~80 lines) - Button visibility/subscription
        |-- ... existing controllers ...
    |-- Utilities/
        |-- RowDisplayBuilder.cs (~60 lines) - Static display text builder
```

---

**End of Analysis Document**

Ready to begin extraction when user confirms.

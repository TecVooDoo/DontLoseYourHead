# Don't Lose Your Head - Architecture Document

**Version:** 4.0
**Date Created:** December 13, 2025
**Last Updated:** December 14, 2025
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

## Script Catalog (47 Scripts)

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

## Key Script Updates (December 14, 2025)

### WordGuessInputController.cs
**Critical Fix:** Event timing for state checks

```csharp
public void Exit(bool submit)
{
    if (!_isActive) return;

    // IMPORTANT: Set _isActive = false BEFORE firing events
    // This ensures InWordGuessMode returns false when ShowAllGuessWordButtons()
    // is called from the event handler chain
    _isActive = false;

    if (submit)
    {
        OnGuessSubmitted?.Invoke(guessedWord);
    }
    // ...
}
```

### AutocompleteManager.cs
**Fixes:** Initialization hiding, position validation, event re-triggering

```csharp
public void Initialize()
{
    // Ensure dropdown starts hidden
    _autocompleteDropdown.Hide();
    // ...
}

private void PositionDropdownNearRow(WordPatternRow row)
{
    // Validate position before using
    if (rowRect.position.y == 0 && rowRect.position.x == 0)
    {
        return;  // Delay positioning
    }
    // ...
}
```

### SetupSettingsPanel.cs
**Fix:** Hide autocomplete before and after random operations

```csharp
private void OnPickRandomWordsClicked()
{
    _autocompleteDropdown?.Hide();  // Before
    PickRandomWords();
    _autocompleteDropdown?.Hide();  // After (events may re-trigger)
}
```

### WordGuessModeController.cs
**Fix:** Restore buttons on all exit paths

```csharp
case WordGuessResult.AlreadyGuessed:
    OnFeedbackRequested?.Invoke("Already guessed that word!");
    ShowAllGuessWordButtons();  // Must restore before return
    return;
```

### GameplayUIController.cs
**Fixes:** Letter tracker routing, AI grid variety

```csharp
private void HandleLetterGuess(char letter)
{
    // Route to word guess mode if active
    if (_wordGuessModeController != null && _wordGuessModeController.IsInKeyboardMode)
    {
        _wordGuessModeController.HandleKeyboardLetterInput(letter);
        return;
    }
    // Normal letter guess processing...
}

private (int gridSize, int wordCount) GetAISettingsForPlayerDifficulty(DifficultySetting playerDifficulty)
{
    switch (playerDifficulty)
    {
        case DifficultySetting.Easy:
            int[] easyGrids = { 6, 7, 8 };
            return (easyGrids[Random.Range(0, 3)], 4);
        case DifficultySetting.Normal:
            int[] normalGrids = { 8, 9, 10 };
            return (normalGrids[Random.Range(0, 3)], Random.Range(0, 2) == 0 ? 3 : 4);
        case DifficultySetting.Hard:
            int[] hardGrids = { 10, 11, 12 };
            return (hardGrids[Random.Range(0, 3)], 3);
    }
}
```

### ExecutionerConfigSO.cs
**Updated Defaults:**
- `_minSkillLevel`: 0.15 -> 0.25
- `_skillAdjustmentStep`: 0.15 -> 0.10
- `_easyMissesToDecrease`: 2 -> 4

---

## Complete File Structure

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
+-- Core/
|   |-- DifficultyCalculator.cs
|   |-- DifficultySO.cs
|   |-- Grid.cs
|   +-- ...
|
+-- UI/
    |-- GameplayUIController.cs         (~1,750 lines)
    |-- GridCellUI.cs                   (~250 lines)
    |-- GuessedWordListController.cs    (~180 lines)
    |-- LetterButton.cs                 (~200 lines)
    |-- MainMenuController.cs           (~150 lines)
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

## Key Patterns

### 1. Event Timing Pattern (Dec 14, 2025)
**CRITICAL:** When event handlers check state, update state BEFORE firing events.

```csharp
// CORRECT:
_isActive = false;  // State updated first
OnSomeEvent?.Invoke();  // Handlers see correct state

// WRONG:
OnSomeEvent?.Invoke();  // Handlers see stale state
_isActive = false;  // Too late!
```

### 2. UI Initialization Pattern (Dec 14, 2025)
Always explicitly set UI components to known states during initialization.

```csharp
public void Initialize()
{
    _dropdown.Hide();  // Explicit known state
    // ... rest of initialization
}
```

### 3. Position Validation Pattern (Dec 14, 2025)
Validate positions before using them for UI placement.

```csharp
if (rect.position == Vector3.zero)
{
    return;  // Layout not ready, delay positioning
}
```

### 4. Event Re-trigger Guard Pattern (Dec 14, 2025)
Hide/reset UI both before and after batch operations.

```csharp
_ui.Hide();
PerformBatchOperation();  // May trigger events
_ui.Hide();  // Guard against re-triggering
```

### 5. Input Mode Routing Pattern (Dec 14, 2025)
Check for active modal states at start of input handlers.

```csharp
private void HandleInput(char letter)
{
    if (_modalController.IsActive)
    {
        _modalController.HandleInput(letter);
        return;  // Route to modal
    }
    // Normal handling...
}
```

---

## AI Integration Status (COMPLETE - Dec 14, 2025)

| Integration Point | Status |
|-------------------|--------|
| ExecutionerAI wired to GameplayUIController | COMPLETE |
| AI events connected | COMPLETE |
| BuildAIGameState() | COMPLETE |
| TriggerAITurn() | COMPLETE |
| Rubber-banding connected | COMPLETE |
| AISetupManager integration | COMPLETE |
| Win condition checking | COMPLETE |
| AI grid/word variety | COMPLETE |
| Rubber-banding balance | COMPLETE |

---

**End of Architecture Document**

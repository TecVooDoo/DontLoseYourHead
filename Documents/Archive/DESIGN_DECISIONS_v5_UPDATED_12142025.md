# Don't Lose Your Head - Design Decisions and Insights

**Version:** 4.0
**Date:** November 22, 2025
**Last Updated:** December 14, 2025
**Developer:** TecVooDoo LLC
**Designer:** Rune (Stephen Brandon)

---

## Recent Changes (December 14, 2025)

### Playtest Bug Fixes - Session 2

Multiple bugs discovered and fixed during continued playtesting:

| Bug | Root Cause | Fix | Status |
|-----|------------|-----|--------|
| Guess Word buttons disappearing on wrong rows | `_isActive = false` set AFTER firing events in WordGuessInputController | Moved `_isActive = false` BEFORE firing events | FIXED |
| Autocomplete dropdown floating at top | Dropdown visible before positioning, origin position check missing | Added Hide() in Initialize(), position validation check | FIXED |
| Autocomplete appearing after Pick Random/Place Random | Row selection events re-triggering dropdown | Added Hide() calls before and after random operations | FIXED |
| Guess Word buttons disappearing after "Already guessed" | Early return without calling ShowAllGuessWordButtons() | Added ShowAllGuessWordButtons() call before return | FIXED |
| Letter Tracker not routing to Word Guess mode | HandleLetterGuess() not checking for active word guess mode | Added IsInKeyboardMode check to route input | FIXED |
| AI always picks 8x8/3 words | Hardcoded constants | Dynamic selection based on player difficulty | FIXED |
| AI too easy when player misses | Rubber-banding too aggressive at reducing skill | Adjusted minSkill, missesToDecrease, adjustmentStep | FIXED |

### Bug Fix: WordGuessInputController Event Timing

**Problem:** After guessing a wrong word on row 1, the Guess Word buttons on rows 2 and 3 disappeared.

**Root Cause:** In `WordGuessInputController.Exit()`, the `_isActive = false` was set AFTER firing events:
```csharp
// OLD (wrong):
if (submit)
{
    OnGuessSubmitted?.Invoke(guessedWord);  // Event fires while _isActive still true
}
_isActive = false;  // Set AFTER event chain completes
```

When the event handler chain called `ShowAllGuessWordButtons()`, the `InWordGuessMode` property still returned `true`, blocking buttons from showing.

**Fix:** Move `_isActive = false` BEFORE firing events:
```csharp
// NEW (correct):
_isActive = false;  // Set BEFORE event chain
if (submit)
{
    OnGuessSubmitted?.Invoke(guessedWord);
}
```

### Bug Fix: Autocomplete Dropdown Positioning

**Problem:** Autocomplete dropdown appeared at the top of the screen during setup.

**Root Cause:** Multiple issues:
1. Dropdown not explicitly hidden during initialization
2. Position validation missing (could position at origin)
3. Row selection events triggering dropdown before layout calculated

**Fixes Applied:**
1. Added `_autocompleteDropdown.Hide()` in `AutocompleteManager.Initialize()`
2. Added origin position check in `PositionDropdownNearRow()`:
```csharp
if (rowRect.position.y == 0 && rowRect.position.x == 0)
{
    Debug.LogWarning("[AutocompleteManager] Row position at origin - delaying");
    return;
}
```
3. Call `PositionDropdownNearRow()` before showing dropdown in `HandleWordTextChanged()`

### Bug Fix: Autocomplete After Random Operations

**Problem:** Autocomplete dropdown appeared after clicking "Pick Random Words" or "Place Random Positions".

**Root Cause:** Row selection events during random operations triggered autocomplete to show.

**Fix:** Added `Hide()` calls before AND after random operations in SetupSettingsPanel:
```csharp
private void OnPickRandomWordsClicked()
{
    _autocompleteDropdown?.Hide();  // Before
    PickRandomWords();
    _autocompleteDropdown?.Hide();  // After (events may have re-triggered)
}
```

### Bug Fix: Already Guessed Word Hiding Buttons

**Problem:** After guessing a word that was already guessed, other Guess Word buttons disappeared.

**Root Cause:** In `WordGuessModeController.HandleWordGuessSubmitted()`, the AlreadyGuessed case returned early without restoring buttons:
```csharp
case WordGuessResult.AlreadyGuessed:
    OnFeedbackRequested?.Invoke("Already guessed that word!");
    return;  // Early return - buttons not restored!
```

**Fix:** Added `ShowAllGuessWordButtons()` before the return:
```csharp
case WordGuessResult.AlreadyGuessed:
    OnFeedbackRequested?.Invoke("Already guessed that word!");
    ShowAllGuessWordButtons();  // Restore buttons
    return;
```

### Bug Fix: Letter Tracker Not Routing to Word Guess Mode

**Problem:** When in Word Guess mode, clicking letters on the Letter Tracker processed them as letter guesses instead of typing into the word guess input.

**Root Cause:** `HandleLetterGuess()` in GameplayUIController was not checking if word guess mode was active.

**Fix:** Added check at start of `HandleLetterGuess()`:
```csharp
if (_wordGuessModeController != null && _wordGuessModeController.IsInKeyboardMode)
{
    _wordGuessModeController.HandleKeyboardLetterInput(letter);
    return;
}
```

### Feature: AI Grid/Word Count Variety

**Problem:** AI always chose 8x8 grid with 3 words, making gameplay repetitive.

**Design Decision:** AI grid size and word count should vary based on player difficulty to:
1. Add gameplay variety
2. Scale appropriately with difficulty
3. Give players different challenges each game

**Implementation:**
```csharp
private (int gridSize, int wordCount) GetAISettingsForPlayerDifficulty(DifficultySetting playerDifficulty)
{
    switch (playerDifficulty)
    {
        case DifficultySetting.Easy:
            // Smaller grids, more words = easier for player
            int[] easyGrids = { 6, 7, 8 };
            return (easyGrids[Random.Range(0, 3)], 4);

        case DifficultySetting.Normal:
            // Medium grids, random words = balanced
            int[] normalGrids = { 8, 9, 10 };
            return (normalGrids[Random.Range(0, 3)], Random.Range(0, 2) == 0 ? 3 : 4);

        case DifficultySetting.Hard:
            // Larger grids, fewer words = harder for player
            int[] hardGrids = { 10, 11, 12 };
            return (hardGrids[Random.Range(0, 3)], 3);
    }
}
```

### AI Rubber-Banding Balance Adjustments

**Problem:** AI became too easy too quickly when player missed guesses, even when player was deliberately losing.

**Root Cause:** Original rubber-banding settings were too aggressive:
- `_minSkillLevel = 0.15` (too low - nearly random)
- `_easyMissesToDecrease = 2` (too few - skill dropped quickly)
- `_skillAdjustmentStep = 0.15` (too large - skill swung dramatically)

**Fixes (in ExecutionerConfigSO):**
- `_minSkillLevel`: 0.15 -> **0.25** (AI always makes some smart moves)
- `_easyMissesToDecrease`: 2 -> **4** (requires more consecutive misses)
- `_skillAdjustmentStep`: 0.15 -> **0.10** (more gradual changes)

**Note:** These are default values in the ScriptableObject. Existing asset instances need manual update in Inspector.

---

## Recent Changes (December 13, 2025 - Evening)

### Phase 3: AI Integration - COMPLETE

ExecutionerAI fully wired to GameplayUIController:

| Integration Task | Status |
|------------------|--------|
| WireAIEvents() method | COMPLETE |
| HandleAILetterGuess() | COMPLETE |
| HandleAICoordinateGuess() | COMPLETE |
| HandleAIWordGuess() | COMPLETE |
| BuildAIGameState() | COMPLETE |
| TriggerAITurn() after player turn | COMPLETE |
| RecordPlayerGuess() for rubber-banding | COMPLETE |
| AISetupManager replaces GenerateOpponentData() | COMPLETE |
| Win condition checking | COMPLETE |

### First Playtest (Stacey vs AI)

Real user testing revealed 4 bugs (all addressed in Dec 14 session or earlier):

| Bug | Description | Status |
|-----|-------------|--------|
| 1 | Letter tracker not green on word guess | IN PROGRESS |
| 2 | Player names wrong under guillotines | IN PROGRESS |
| 3 | Player's guessed word list empty | N/A (correct behavior) |
| 4 | AI wrong word = 1 miss instead of 2 | IN PROGRESS |

---

## AI Design Details

### ExecutionerAI Integration Pattern

ExecutionerAI fires events that GameplayUIController subscribes to:

```csharp
// In GameplayUIController
private void WireAIEvents()
{
    _executionerAI.OnThinkingStarted += HandleAIThinkingStarted;
    _executionerAI.OnLetterGuess += HandleAILetterGuess;
    _executionerAI.OnCoordinateGuess += HandleAICoordinateGuess;
    _executionerAI.OnWordGuess += HandleAIWordGuess;
}
```

### Grid Density Analysis

Grid density affects which strategy is more efficient:

**Fill Ratio Formula:**
```
fillRatio = (wordCount * averageWordLength) / (gridSize * gridSize)
         = (wordCount * 4.5) / (gridSize * gridSize)
```

**Strategy Preference Calculation:**
```csharp
if (fillRatio >= 0.35f)      // High density: 40% letter, 60% coordinate
else if (fillRatio >= 0.20f) // Medium density: 50% letter, 50% coordinate
else if (fillRatio >= 0.12f) // Low density: 65% letter, 35% coordinate
else                         // Very low density: 80% letter, 20% coordinate
```

### Rubber-Banding System

**Updated Values (Dec 14, 2025):**

| Player Difficulty | AI Start Skill | Hits to Increase | Misses to Decrease |
|-------------------|----------------|------------------|-------------------|
| Easy | 0.25 | 5 | 4 |
| Normal | 0.50 | 3 | 3 |
| Hard | 0.75 | 2 | 5 |

**Skill Bounds:**
- Minimum: 0.25 (was 0.15)
- Maximum: 0.95
- Adjustment Step: 0.10 (was 0.15)

---

## Implementation Status

### Completed Systems

| System | Status |
|--------|--------|
| Grid System (6x6-12x12) | COMPLETE |
| Word Placement | COMPLETE |
| Letter Guessing | COMPLETE |
| Coordinate Guessing | COMPLETE |
| Word Guessing (2-miss penalty) | COMPLETE |
| Letter Reveal (* -> letter) | COMPLETE |
| Turn Management | COMPLETE |
| Player System | COMPLETE |
| Game State Machine | COMPLETE |
| Difficulty System | COMPLETE |
| Word Bank (25,000+) | COMPLETE |
| Word Validation | COMPLETE |
| Three-Color Grid Cells | COMPLETE |
| Word Guess Mode | COMPLETE |
| Solved Word Tracking | COMPLETE |
| Duplicate Guess Prevention | COMPLETE |
| Guessed Word Lists | COMPLETE |
| Code Refactoring | COMPLETE |
| Grid Row Labels Resize | COMPLETE |
| Autocomplete Dropdowns | COMPLETE |
| Main Menu | COMPLETE |
| Settings Panel | COMPLETE |
| AI Design | COMPLETE |
| AI Scripts (11) | COMPLETE |
| AI Integration | COMPLETE |
| AI Grid Variety | COMPLETE |

### Phase 4: Polish and Features - TODO

| Component | Status |
|-----------|--------|
| Visual Polish (DOTween/Feel) | TODO |
| Audio Implementation | TODO |
| Invalid Word Feedback UI | TODO |
| Profanity Filter | TODO |
| Medieval Monospace Font | TODO |

---

## Lessons Learned

### 1-26: Previous Lessons (see archive)

### 27. Event Timing Matters for State Checks (Dec 14, 2025)
When event handlers check object state, ensure the state is updated BEFORE firing events, not after.
- `_isActive = false` must come BEFORE `OnGuessSubmitted?.Invoke()`
- Otherwise, handlers see stale state

### 28. Initialize UI Components to Known States (Dec 14, 2025)
Always explicitly set UI components to a known state during initialization:
- Call `Hide()` on dropdowns/popups in `Initialize()`
- Don't rely on default states or Awake() alone

### 29. Validate Positions Before UI Placement (Dec 14, 2025)
Before positioning UI elements relative to other elements:
- Check if the reference element has valid position (not at origin)
- Layout calculations may not be complete during early lifecycle

### 30. Guard Against Event Re-triggering (Dec 14, 2025)
When performing batch operations that trigger events:
- Hide/reset UI before the operation
- Hide/reset UI again after (events may have re-triggered display)

### 31. Check All Code Paths for State Restoration (Dec 14, 2025)
When a method has multiple exit paths (return statements):
- Ensure each path restores necessary state
- Early returns often skip cleanup that happens at the end

### 32. Route Input Based on Active Mode (Dec 14, 2025)
Input handlers should check for active modal states first:
- Word guess mode intercepts letter tracker clicks
- Check mode state at start of handler, route appropriately

### 33. Add Variety Within Constraints (Dec 14, 2025)
Hardcoded values make gameplay repetitive. Use random selection within appropriate ranges:
- AI grid size varies based on difficulty
- Maintains balance while adding variety

### 34. ScriptableObject Defaults vs Asset Instances (Dec 14, 2025)
Changing default values in ScriptableObject code does NOT update existing asset instances:
- New assets get new defaults
- Existing assets keep their serialized values
- Must manually update in Inspector or reset to defaults

### 35. Rubber-Banding Needs Bounds Testing (Dec 14, 2025)
Rubber-banding systems can over-correct:
- Minimum skill should still be competent (not random)
- Adjustment thresholds should prevent rapid swings
- Test with intentionally poor play to find edge cases

---

## File Structure (Dec 14, 2025)

```
Assets/DLYH/Scripts/
  Core/
    DifficultyCalculator.cs
    DifficultySO.cs
    GameManager.cs
    TurnManager.cs
    PlayerSO.cs
    PlayerManager.cs
    Grid.cs
    GridCell.cs
    Word.cs
    WordListSO.cs
    ...
  AI/
    Config/
      ExecutionerConfigSO.cs       (~412 lines)
    Core/
      ExecutionerAI.cs             (~493 lines)
      DifficultyAdapter.cs         (~268 lines)
      MemoryManager.cs             (~442 lines)
      AISetupManager.cs            (~468 lines)
    Strategies/
      IGuessStrategy.cs            (~493 lines)
      LetterGuessStrategy.cs       (~327 lines)
      CoordinateGuessStrategy.cs   (~262 lines)
      WordGuessStrategy.cs         (~327 lines)
    Data/
      LetterFrequency.cs           (~442 lines)
      GridAnalyzer.cs              (~442 lines)
  UI/
    MainMenuController.cs (~130 lines)
    SettingsPanel.cs (~270 lines)
    PlayerGridPanel.cs (~1,120 lines)
    SetupSettingsPanel.cs (~850 lines)
    GameplayUIController.cs (~1,750 lines)
    SetupModeController.cs (~150 lines)
    WordPatternRow.cs (~1,199 lines)
    LetterButton.cs (~200 lines)
    GridCellUI.cs (~250 lines)
    AutocompleteDropdown.cs (~450 lines)
    AutocompleteItem.cs (~140 lines)
    Controllers/
      LetterTrackerController.cs (~175 lines)
      GridColorManager.cs (~130 lines)
      PlacementPreviewController.cs (~200 lines)
      WordPatternRowManager.cs (~400 lines)
      WordPatternController.cs (~285 lines)
      CoordinatePlacementController.cs (~620 lines)
      GridLayoutManager.cs (~600 lines)
      GridCellManager.cs (~150 lines)
      PlayerColorController.cs (~80 lines)
      WordGuessModeController.cs (~290 lines)
      WordGuessInputController.cs (~310 lines)
      AutocompleteManager.cs (~385 lines)
    Interfaces/
      IGridControllers.cs (~115 lines)
    Services/
      WordValidationService.cs (~60 lines)
      GuessProcessor.cs (~400 lines)
      GameplayStateTracker.cs (~300 lines)
      WinConditionChecker.cs (~225 lines)
    Utilities/
      RowDisplayBuilder.cs (~207 lines)
```

---

## Project Documents

| Document | Purpose | Version |
|----------|---------|---------|
| DontLoseYourHead_GDD | Game design, mechanics, phases | v4.0 |
| DontLoseYourHead_ProjectInstructions | Development protocols, MCP tools | v4.0 |
| DESIGN_DECISIONS | Technical decisions, lessons learned | v4.0 |
| DLYH_Architecture | Script catalog, data flow, patterns | v4.0 |

---

**End of Design Decisions Document**

This is a living document updated as:
- New playtesting reveals insights
- Design questions are resolved
- Balance adjustments are made
- Implementation uncovers new considerations

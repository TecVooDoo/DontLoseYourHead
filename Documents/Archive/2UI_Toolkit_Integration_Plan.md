# Defense View Implementation Plan

## Overview
Implement the Defend tab to show the player's own grid with opponent's guesses, and add auto-switching between tabs based on whose turn it is.

## Current Architecture Summary

### Existing Components (Can Reuse)
1. **TableModel** - Data model for grid cells, already supports multiple instances
2. **TableView** - Renders a TableModel, already has `Bind(model)` to swap models
3. **WordRowsContainer** - Manages word rows, already supports separate attack/defend containers
4. **GameplayScreenManager** - Already has:
   - `_attackTableModel` and `_defendTableModel` fields
   - `_attackWordRows` and `_defendWordRows` fields
   - `SelectAttackTab()` / `SelectDefendTab()` methods
   - Keyboard state tracking (`_hitLetters`, `_missLetters`)
   - Tab switching via `_isAttackTabActive`
5. **GameplayGuessManager** - Already tracks:
   - `_playerGuessState` (player's guesses against opponent)
   - `_opponentGuessState` (opponent's guesses against player)
   - Events for both player and opponent guesses
6. **PlacementAdapter** - Stores `PlacedLetters` and `AllPlacedPositions`

### What Needs to Be Added/Modified

## Implementation Steps

### Step 1: Create Defense TableModel and WordRows
**File: UIFlowController.cs**

Add fields for defense-specific components:
```csharp
private TableModel _defenseTableModel;       // Player's grid for opponent to attack
private TableLayout _defenseTableLayout;
private WordRowsContainer _defenseWordRows;  // Player's words (fully visible)
```

In `HandleSetupComplete()`:
- Create `_defenseTableModel` using same layout as attack
- Create `_defenseWordRows` with player's words (NOT gameplay mode - all letters visible)
- Store player's word placements separately

### Step 2: Initialize Defense Grid with Player's Words
**File: UIFlowController.cs**

After player places words and transitions to gameplay:
1. Copy the current placement data to defense model
2. Set all cells to show letters (not fog) - player's own words are visible to them
3. Pass both models to `GameplayScreenManager.SetTableModels(attack, defend)`

### Step 3: Separate Keyboard State for Attack vs Defend
**File: GameplayScreenManager.cs**

Add opponent's keyboard tracking:
```csharp
private HashSet<char> _opponentHitLetters = new HashSet<char>();
private HashSet<char> _opponentMissLetters = new HashSet<char>();
```

Modify tab switching to swap keyboard state:
- On `SelectAttackTab()`: Show player's guess keyboard state
- On `SelectDefendTab()`: Show opponent's guess keyboard state

Add methods:
```csharp
public void MarkOpponentLetterHit(char letter, Color opponentColor) { ... }
public void MarkOpponentLetterMiss(char letter) { ... }
```

### Step 4: Show AI Guesses on Defense Grid
**File: UIFlowController.cs**

In `HandleAILetterGuess()` and `HandleAICoordinateGuess()`:
1. Update `_defenseTableModel` cells to show hit/miss states
2. Call `MarkOpponentLetterHit/Miss` on GameplayScreenManager
3. Reveal letters in `_defenseWordRows` when AI guesses correctly

### Step 5: Auto-Switch Tabs Based on Turn
**File: UIFlowController.cs**

In `SwitchToOpponentTurnCoroutine()`:
```csharp
_gameplayManager?.SelectDefendTab(); // Auto-switch to Defend during opponent turn
```

In `SwitchToPlayerTurnCoroutine()`:
```csharp
_gameplayManager?.SelectAttackTab(); // Auto-switch to Attack during player turn
```

### Step 6: Allow Manual Tab Switching Only During Player Turn
**File: GameplayScreenManager.cs**

Add `_allowManualTabSwitch` field:
```csharp
private bool _allowManualTabSwitch = true;

public void SetAllowManualTabSwitch(bool allow)
{
    _allowManualTabSwitch = allow;
}
```

In tab click handlers:
```csharp
if (!_allowManualTabSwitch) return; // Ignore clicks during opponent turn
```

**File: UIFlowController.cs**
- Call `SetAllowManualTabSwitch(false)` in `EndPlayerTurn()`
- Call `SetAllowManualTabSwitch(true)` in `SwitchToPlayerTurnCoroutine()`

## Detailed Changes by File

### GameplayScreenManager.cs
1. Add opponent keyboard state tracking fields
2. Add `MarkOpponentLetterHit(char, Color)` method
3. Add `MarkOpponentLetterMiss(char)` method
4. Add `RefreshKeyboardForTab()` private method to swap keyboard display
5. Add `SetAllowManualTabSwitch(bool)` method
6. Modify `SelectAttackTab()` to call `RefreshKeyboardForTab()`
7. Modify `SelectDefendTab()` to call `RefreshKeyboardForTab()`
8. Add guard in tab click handlers for `_allowManualTabSwitch`

### UIFlowController.cs
1. Add `_defenseTableModel`, `_defenseTableLayout`, `_defenseWordRows` fields
2. In `TransitionToGameplay()` or `HandleSetupComplete()`:
   - Create defense TableModel with same layout
   - Copy player's placed letters to defense model (visible state, not fog)
   - Create defense WordRowsContainer with player's words (visible, not underscores)
   - Pass both models to GameplayScreenManager
3. In AI guess handlers:
   - Update defense model cells for hit/miss
   - Update opponent keyboard state
   - Reveal letters in defense word rows
4. In turn switching:
   - Auto-switch tabs
   - Control manual tab switching allowance

### Summary of Tab Behavior

| Tab | Grid Shows | Words Show | Keyboard Shows | Whose Guesses |
|-----|------------|------------|----------------|---------------|
| Attack | Opponent's grid (fog + your guesses) | Opponent's words (hidden, reveal on guess) | YOUR letter guesses | Yours |
| Defend | YOUR grid (visible + opponent guesses) | YOUR words (all visible) | OPPONENT's letter guesses | Opponent's |

### Auto-Switch Logic
- Player's turn starts -> Switch to Attack tab, enable manual switching
- Opponent's turn starts -> Switch to Defend tab, disable manual switching
- During player's turn: Can manually switch between tabs to review

## Testing Checklist
- [ ] Defense grid shows player's placed letters
- [ ] Defense word rows show player's words (not underscores)
- [ ] AI hit on defense grid shows player color for hit cells
- [ ] AI miss on defense grid shows red for miss cells
- [ ] Keyboard on Defend tab shows opponent's guesses (not player's)
- [ ] Auto-switch to Defend tab when opponent's turn starts
- [ ] Auto-switch to Attack tab when player's turn starts
- [ ] Cannot manually switch tabs during opponent's turn
- [ ] Can manually switch tabs during player's turn

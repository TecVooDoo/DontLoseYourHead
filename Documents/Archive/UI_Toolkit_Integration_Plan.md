# UI Toolkit Integration Plan
## Reusing Existing Systems with New UI

**Created:** January 9, 2026
**Purpose:** Plan how to integrate existing word list, autocomplete, and placement systems with the new UI Toolkit interface while supporting both desktop and mobile.

---

## 1. Current State Assessment

### What Exists (DO NOT REBUILD)

| System | File | Status |
|--------|------|--------|
| Word Lists | `WordListSO.cs` | Working - 3,4,5,6 letter ScriptableObjects |
| Word Validation | `WordValidationService.cs` | Working - validates against word lists |
| Autocomplete Logic | `AutocompleteDropdown.cs` | Working - uGUI-based, logic reusable |
| Placement Logic | `CoordinatePlacementController.cs` | Working - 8 directions, intersections |
| Grid Highlighting | `GridColorManager.cs` | Working - valid/invalid/cursor colors |

### What Was Built for UI Toolkit (NEEDS REVISION)

| Component | File | Issue |
|-----------|------|-------|
| WordPlacementController | `NewUI/Scripts/WordPlacementController.cs` | Reimplements placement poorly (2 directions only) |
| TableLayout | `NewUI/Scripts/TableLayout.cs` | OK but word rows need variable lengths |
| TableModel | `NewUI/Scripts/TableModel.cs` | OK - generic data model |
| TableView | `NewUI/Scripts/TableView.cs` | OK - renders cells, needs better sizing |

---

## 2. Word Entry System

### Current Behavior (uGUI)
- Player types letters → autocomplete dropdown shows filtered words
- Word length enforced per row (row 1 = 3 letters, row 2 = 4, etc.)
- Keyboard: physical typing + on-screen letter buttons both work
- Enter/click confirms word, Escape cancels

### UI Toolkit Approach

**Desktop:**
- TextField with autocomplete dropdown (VisualElement popup)
- Physical keyboard typing triggers autocomplete filter
- Arrow keys navigate suggestions, Enter confirms

**Mobile:**
- Same TextField, but system keyboard appears on focus
- On-screen letter keyboard as backup (already in UXML)
- Tap suggestion to select

**Implementation:**
```
┌─────────────────────────────────────────────────┐
│ AutocompleteTextField (new UI Toolkit component)│
├─────────────────────────────────────────────────┤
│ - Wraps TextField                               │
│ - Injects WordListSO for filtering              │
│ - Shows dropdown popup on input                 │
│ - Handles keyboard navigation                   │
│ - Events: OnWordAccepted, OnWordCleared         │
└─────────────────────────────────────────────────┘
                    │
                    ▼
┌─────────────────────────────────────────────────┐
│ WordValidationService (existing)                │
│ - ValidateWord(string, requiredLength)          │
│ - GetRandomWordOfLength(length)                 │
└─────────────────────────────────────────────────┘
```

---

## 3. Word Row Structure

### Required Changes

**Current (broken):** All word rows same width as grid
**Correct:** Word rows have variable lengths

| Row | Word Length | Cells |
|-----|-------------|-------|
| 1   | 3 letters   | 3 cells + controls |
| 2   | 4 letters   | 4 cells + controls |
| 3   | 5 letters   | 5 cells + controls |
| 4   | 6 letters   | 6 cells + controls (if 4 words) |

### Per-Row Controls
Each word row needs:
- **Word cells** - display entered letters (variable count)
- **Placement button** - enter grid placement mode for this word
- **Clear button** - remove word and clear from grid

### Layout Structure
```
Word Row 1: [_][_][_]         [⊕][X]   ← 3 letter slots + buttons
Word Row 2: [_][_][_][_]      [⊕][X]   ← 4 letter slots + buttons
Word Row 3: [_][_][_][_][_]   [⊕][X]   ← 5 letter slots + buttons
─────────────────────────────────────
           A  B  C  D  E  F  G  H     ← Column headers
        1  [ ][ ][ ][ ][ ][ ][ ][ ]   ← Grid cells
        2  [ ][ ][ ][ ][ ][ ][ ][ ]
        ...
```

---

## 4. Grid Placement System

### Existing Logic (CoordinatePlacementController)

**Two-Step Placement:**
1. `SelectingFirstCell` - Player clicks/taps starting position
2. `SelectingDirection` - Player clicks/taps to define direction (8 possible)

**Validation:**
- Check grid bounds for word length in chosen direction
- Check intersections (allowed if same letter at crossing)
- Track all placed positions to prevent invalid overlaps

**Highlighting:**
- Cursor position: Yellow
- Valid placement cells: Green
- Invalid placement cells: Red
- Placed letters: Player color

### Mobile Considerations

**Touch vs Mouse:**
- Mouse: Hover shows preview, click confirms
- Touch: No hover - need alternative preview method

**Mobile Placement Flow:**
1. Tap word row's placement button (⊕) → enters placement mode
2. Tap grid cell → sets anchor (yellow), shows valid directions (green)
3. Tap a green cell → confirms direction, word is placed
4. Tap anchor again or outside → cancels

**Alternative: Drag Gesture**
1. Touch and hold starting cell
2. Drag in direction to place
3. Release to confirm (if valid) or cancel (if invalid)

### UI Toolkit Implementation

```
┌─────────────────────────────────────────────────┐
│ PlacementAdapter (new - bridges UI Toolkit)     │
├─────────────────────────────────────────────────┤
│ - Receives cell click/touch events from TableView│
│ - Translates to CoordinatePlacementController   │
│ - Receives highlight commands                   │
│ - Updates TableModel cell states                │
└─────────────────────────────────────────────────┘
                    │
                    ▼
┌─────────────────────────────────────────────────┐
│ CoordinatePlacementController (existing)        │
│ - HandleCellClick(col, row)                     │
│ - UpdatePlacementPreview(hoverCol, hoverRow)    │
│ - PlaceWordInDirection(...)                     │
│ - GetValidDirections(startCol, startRow, word)  │
└─────────────────────────────────────────────────┘
```

---

## 5. Integration Architecture

### Dependency Flow

```
┌──────────────────────────────────────────────────────────────┐
│                    UIFlowController                          │
│              (orchestrates screen flow)                      │
└──────────────────────────────────────────────────────────────┘
                              │
          ┌───────────────────┼───────────────────┐
          ▼                   ▼                   ▼
┌─────────────────┐  ┌─────────────────┐  ┌─────────────────┐
│   MainMenu      │  │  SetupWizard    │  │   Gameplay      │
│   (UI Toolkit)  │  │  (UI Toolkit)   │  │   (Future)      │
└─────────────────┘  └─────────────────┘  └─────────────────┘
                              │
                              ▼
┌──────────────────────────────────────────────────────────────┐
│                   PlacementPanel                             │
│                   (UI Toolkit)                               │
├──────────────────────────────────────────────────────────────┤
│  WordRowsContainer          │  GridContainer                 │
│  ├─ WordRow (3 letters)     │  ├─ ColumnHeaders              │
│  ├─ WordRow (4 letters)     │  ├─ RowHeaders                 │
│  ├─ WordRow (5 letters)     │  └─ GridCells                  │
│  └─ WordRow (6 letters)     │                                │
└──────────────────────────────────────────────────────────────┘
          │                              │
          ▼                              ▼
┌─────────────────┐            ┌─────────────────┐
│ WordEntryManager│            │ PlacementAdapter│
│ (new)           │            │ (new)           │
└─────────────────┘            └─────────────────┘
          │                              │
          ▼                              ▼
┌─────────────────┐            ┌─────────────────────────────┐
│WordValidation   │            │CoordinatePlacementController│
│Service(existing)│            │(existing)                   │
└─────────────────┘            └─────────────────────────────┘
          │
          ▼
┌─────────────────┐
│ WordListSO      │
│ (existing)      │
└─────────────────┘
```

---

## 6. Files to Create/Modify

### New Files (UI Toolkit Layer)

| File | Purpose |
|------|---------|
| `NewUI/Scripts/WordEntryManager.cs` | Manages word input with autocomplete, delegates to WordValidationService |
| `NewUI/Scripts/PlacementAdapter.cs` | Bridges TableView events to CoordinatePlacementController |
| `NewUI/Scripts/WordRowView.cs` | UI for single word row (variable length cells + controls) |
| `NewUI/Scripts/AutocompletePopup.cs` | UI Toolkit dropdown for word suggestions |

### Files to Modify

| File | Changes |
|------|---------|
| `NewUI/Scripts/TableLayout.cs` | Support variable word row lengths |
| `NewUI/Scripts/TableModel.cs` | Add word row length tracking |
| `NewUI/Scripts/TableView.cs` | Render variable-width word rows |
| `NewUI/Scripts/UIFlowController.cs` | Inject WordListSO references, use new managers |
| `NewUI/UXML/SetupWizard.uxml` | Restructure placement panel |
| `NewUI/USS/SetupWizard.uss` | Styling for variable word rows |
| `NewUI/USS/TableView.uss` | Fix cell sizing/clipping |

### Files to DELETE (redundant)

| File | Reason |
|------|--------|
| `NewUI/Scripts/WordPlacementController.cs` | Replaced by PlacementAdapter + existing controller |

---

## 7. Implementation Order

### Phase 1: Fix Layout & Sizing
1. Update TableLayout for variable word row lengths
2. Fix cell sizing in TableView.uss (no clipping)
3. Update TableModel to track per-row word lengths
4. Test: Word rows display correctly with 3,4,5,6 cells

### Phase 2: Word Entry Integration
1. Create WordEntryManager (wraps WordValidationService)
2. Create AutocompletePopup (UI Toolkit dropdown)
3. Wire to existing WordListSO assets
4. Test: Can type words with autocomplete, validation works

### Phase 3: Placement Integration
1. Create PlacementAdapter
2. Connect to existing CoordinatePlacementController
3. Implement cell highlighting via TableModel states
4. Test: Desktop click placement works (8 directions)

### Phase 4: Mobile Support
1. Add touch event handling to PlacementAdapter
2. Implement tap-to-place flow (no hover)
3. Test drag-to-place gesture (optional enhancement)
4. Test: Mobile placement works

### Phase 5: Polish
1. Per-word-row controls (placement button, clear button)
2. Visual feedback (placed word colors, error states)
3. Random words/placement using existing service methods
4. Ready button validation

---

## 8. Questions to Resolve

1. **Diagonal placement** - Original supported 8 directions. Keep all 8 or simplify to 4 (H/V only)?
   - *Recommendation:* Keep 8 for strategic depth

2. **Intersection rules** - Words can cross if they share a letter. Keep this?
   - *Recommendation:* Yes, it's a core mechanic

3. **Mobile drag gesture** - Worth implementing or tap-tap sufficient?
   - *Recommendation:* Start with tap-tap, add drag later if needed

4. **Letter keyboard** - Keep on-screen keyboard for mobile or rely on system keyboard?
   - *Recommendation:* Keep as option, especially for autocomplete selection

---

## 9. Success Criteria

- [ ] Word rows display correct number of cells (3, 4, 5, 6)
- [ ] No cell clipping or overflow
- [ ] Autocomplete filters from WordListSO
- [ ] Words validate against approved list
- [ ] Grid placement uses existing 8-direction logic
- [ ] Intersection validation works (shared letters allowed)
- [ ] Cell highlighting shows valid/invalid positions
- [ ] Works on both desktop (mouse) and mobile (touch)
- [ ] Random words pulls from word lists
- [ ] Random placement uses existing algorithm
- [ ] Clear button removes word from row and grid

---

## 10. References

### Existing Files to Study
- `Assets/DLYH/Scripts/UI/Controllers/CoordinatePlacementController.cs` - Placement logic
- `Assets/DLYH/Scripts/UI/WordPatternRow.cs` - Word row UI structure
- `Assets/DLYH/Scripts/UI/AutocompleteDropdown.cs` - Autocomplete behavior
- `Assets/DLYH/Scripts/Core/GameState/WordListSO.cs` - Word data structure
- `Assets/DLYH/Scripts/UI/Services/WordValidationService.cs` - Validation logic

### UI Toolkit Docs
- VisualElement events: ClickEvent, PointerDownEvent, PointerMoveEvent
- TextField: TextInputBaseField events for autocomplete
- Popup/dropdown patterns in UI Toolkit

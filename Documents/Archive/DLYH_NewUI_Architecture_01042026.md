# Don't Lose Your Head - New UI System Architecture

**Project:** Don't Lose Your Head
**Developer:** TecVooDoo LLC
**Created:** January 4, 2026
**Status:** In Development

---

## Overview

The New UI System is a rebuild of the word pattern row components using a unified cell-based approach. All elements in a row (label, letters, action buttons) use the same `LetterCellUI` component for uniform appearance, sizing, and animation capability.

---

## Design Principles

1. **Mobile-Forward**: Built for 1080x1920 reference resolution from the start
2. **Unified Cells**: All row elements are structurally identical `LetterCellUI` instances
3. **Animation-Ready**: DOTween integration for spin, scale, shake, color transitions
4. **Player Color Theming**: Cells support player color for revealed letters
5. **Dynamic Creation**: Cells instantiated at runtime, not preset in prefab

---

## File Structure

```
Assets/DLYH/
├── NewUI/
│   └── Prefabs/
│       └── LetterCellUI.prefab
├── Scripts/
│   └── UI/
│       ├── LetterCellUI.cs      (~543 lines)
│       └── WordPatternRowUI.cs  (~655 lines)
└── Scenes/
    └── NewUIDesign.unity        (test scene)
```

---

## Canvas Configuration

```
Canvas
├── Render Mode: Screen Space - Overlay
├── Canvas Scaler
│   ├── UI Scale Mode: Scale With Screen Size
│   ├── Reference Resolution: 1080 x 1920
│   ├── Screen Match Mode: Match Width Or Height
│   └── Match: 0.5
└── Graphic Raycaster
```

---

## Component Details

### LetterCellUI.cs

**Namespace:** `TecVooDoo.DontLoseYourHead.UI`

**Purpose:** Unified cell component for word pattern rows. Supports letters, icons, and action buttons with animation-ready structure.

#### Hierarchy Structure
```
LetterCellUI (GameObject)
├── RectTransform (animatable: scale, rotation, position)
├── Image (_background) - cell background, color animatable
├── CanvasGroup - for alpha fading
├── Layout Element - preferred size 150x150, flexible width 1
├── LetterCellUI (Script)
└── Children:
    ├── LetterText (TextMeshProUGUI)
    │   ├── RectTransform: stretch fill (0,0,0,0)
    │   ├── Alignment: Center + Middle
    │   ├── Auto Size: 12-48
    │   └── Overflow: Truncate
    └── IconImage (Image)
        ├── RectTransform: stretch with 8px padding
        ├── Raycast Target: false
        └── Enabled: false (script controls visibility)
```

#### Enums

```csharp
public enum CellContentType
{
    Letter,     // Shows letter character (A-Z) or underscore
    Icon,       // Shows sprite image (for action buttons)
    Empty       // Invisible placeholder (maintains layout spacing)
}

public enum LetterCellState
{
    Default,        // Normal state
    Selected,       // Currently selected/active
    Revealed,       // Letter has been revealed (gameplay)
    Locked,         // Cannot be modified
    Disabled,       // Grayed out, non-interactive
    Highlighted     // Temporary highlight (hover, etc.)
}
```

#### Key Properties

| Property | Type | Description |
|----------|------|-------------|
| CellIndex | int | Position in row (0-11) |
| ContentType | CellContentType | Current content mode |
| State | LetterCellState | Visual state |
| CurrentLetter | char | Displayed letter |
| IsInteractive | bool | Responds to clicks |
| HasLetter | bool | Has valid letter (not null/underscore) |

#### Key Methods

| Method | Description |
|--------|-------------|
| Initialize(index, contentType, isInteractive) | Set up cell |
| SetLetter(char) | Display letter or underscore |
| SetIcon(Sprite) | Display icon sprite |
| SetState(state) | Change visual state |
| SetPlayerColor(Color) | Set color for revealed state |
| AnimateSpin/Punch/Scale/Shake/Color/Fade/Reveal | DOTween animations |

#### Events

```csharp
public event Action<int> OnCellClicked;      // Cell index
public event Action<int> OnCellHoverEnter;   // Cell index
public event Action<int> OnCellHoverExit;    // Cell index
```

---

### WordPatternRowUI.cs

**Namespace:** `TecVooDoo.DontLoseYourHead.UI`

**Purpose:** Manages a row of 12 LetterCellUI components for displaying word patterns.

#### Constants

```csharp
public const int ROW_LABEL_INDEX = 0;
public const int FIRST_LETTER_INDEX = 1;
public const int MAX_LETTER_CELLS = 8;
public const int FIRST_ACTION_INDEX = 9;
public const int ACTION_CELL_COUNT = 3;
public const int TOTAL_CELLS = 12;
```

#### Cell Layout

| Index | Name | Purpose |
|-------|------|---------|
| 0 | Cell_RowLabel | Displays "1", "2", etc. |
| 1-8 | Cell_Letter0-7 | Letter or underscore |
| 9 | Cell_Select | Setup: compass icon, Gameplay: guess word icon |
| 10 | Cell_Place | Setup: checkmark icon |
| 11 | Cell_Delete | Setup: X/trash icon |

#### Modes

```csharp
public enum WordRowMode
{
    Setup,      // Word entry mode - user types letters
    Gameplay    // Word reveal mode - letters discovered through gameplay
}
```

#### Setup Mode Actions
- **Select**: Compass button - activates row for coordinate placement
- **Place**: Checkmark button - enabled when word complete, places word on grid
- **Delete**: X button - enabled when has content, clears word

#### Gameplay Mode Actions
- **GuessWord**: Question mark button - attempt to guess entire word

#### Key Properties

| Property | Type | Description |
|----------|------|-------------|
| RowIndex | int | Row number (0-4) |
| WordLength | int | Current word length (3-8) |
| CurrentWord | string | Entered/revealed letters |
| IsSelected | bool | Row is active |
| IsPlaced | bool | Word placed on grid |
| HasWord | bool | Word complete |

#### Key Methods

| Method | Description |
|--------|-------------|
| Initialize(rowIndex, wordLength, mode) | Set up row |
| SetWord(string) | Set full word |
| AddLetter(char) | Add letter (typing) |
| RemoveLastLetter() | Backspace |
| RevealLetter(position, char) | Reveal single letter |
| RevealAllLetters(string) | Reveal entire word |
| SetSelected(bool) | Toggle selection state |
| SetPlaced(bool) | Mark as placed |
| SetPlayerColor(Color) | Set player color |

#### Events

```csharp
public event Action<int, int> OnLetterCellClicked;  // rowIndex, letterIndex
public event Action<int> OnSelectClicked;           // rowIndex
public event Action<int> OnPlaceClicked;            // rowIndex
public event Action<int> OnDeleteClicked;           // rowIndex
public event Action<int> OnGuessWordClicked;        // rowIndex
public event Action<int, string> OnWordComplete;    // rowIndex, word
```

---

## Prefab Configuration

### LetterCellUI Prefab

**Location:** `Assets/DLYH/NewUI/Prefabs/LetterCellUI.prefab`

```
LetterCellUI
├── RectTransform
│   ├── Width: (driven by layout)
│   ├── Height: (driven by layout)
│   └── Scale: 1, 1, 1
├── Image
│   ├── Source Image: UISprite
│   └── Color: White
├── Canvas Group
│   ├── Alpha: 1
│   └── Interactable: true
├── Layout Element
│   ├── Preferred Width: 150
│   ├── Preferred Height: 150
│   └── Flexible Width: 1
└── LetterCellUI (Script)
    └── (default serialized values)

Children:
├── LetterText
│   ├── RectTransform: stretch (0,0,0,0)
│   └── TextMeshProUGUI
│       ├── Font Size: Auto (12-48)
│       └── Alignment: Center + Middle
└── IconImage
    ├── RectTransform: stretch (8,8,8,8 padding)
    ├── Image
    │   ├── Raycast Target: false
    │   └── Preserve Aspect: true (if available)
    └── Enabled: false
```

### WordPatternRowUI Scene Setup

```
WordPatternRowUI
├── RectTransform
│   ├── Anchor: stretch horizontal
│   ├── Height: 100
│   └── Left: 0, Right: 0
└── WordPatternRowUI (Script)
    ├── Row Index: 0
    ├── Word Length: 3
    ├── Mode: Setup
    ├── Cell Container: (ref to CellContainer)
    └── Cell Prefab: (ref to LetterCellUI prefab)

Children:
└── CellContainer
    ├── RectTransform: stretch both (0,0,0,0)
    └── Horizontal Layout Group
        ├── Spacing: 4 (set to 0 for seamless appearance)
        ├── Child Alignment: Middle Left
        ├── Control Child Size: Width ✓, Height ✓
        └── Child Force Expand: Width ✓
```

---

## Visual Design Notes

### Continuous Row Appearance

To make 12 cells appear as one continuous bar:
1. Set Horizontal Layout Group **Spacing** to `0`
2. Cells will touch edge-to-edge
3. Individual cell backgrounds create visual separation through state colors

### Cell State Colors (defaults in LetterCellUI)

| State | Color |
|-------|-------|
| Default | Light gray (0.9, 0.9, 0.9) |
| Selected | Yellow (1, 0.95, 0.6) |
| Revealed | Green (0.7, 0.9, 0.7) or player color |
| Disabled | Gray (0.6, 0.6, 0.6, 0.5) |
| Highlighted | Light blue (0.8, 0.9, 1) |

---

## Integration Points

### With Existing Systems

The new UI components need to integrate with:

1. **SetupSettingsPanel** - keyboard input routing
2. **WordValidationService** - word validation
3. **CoordinatePlacementController** - grid placement mode
4. **GameplayUIController** - guess processing
5. **GuessProcessor** - hit/miss logic
6. **PlayerColorController** - player theming

### Events to Wire

```csharp
// Setup Mode
wordRow.OnSelectClicked += HandleSelectClicked;
wordRow.OnPlaceClicked += HandlePlaceClicked;
wordRow.OnDeleteClicked += HandleDeleteClicked;
wordRow.OnWordComplete += HandleWordComplete;

// Gameplay Mode
wordRow.OnGuessWordClicked += HandleGuessWordClicked;
wordRow.OnLetterCellClicked += HandleLetterCellClicked;
```

---

## Pending Work

1. [ ] Set cell spacing to 0 for continuous row appearance
2. [ ] Create action icon sprites (Select, Place, Delete, GuessWord)
3. [ ] Save WordPatternRowUI as prefab
4. [ ] Build word pattern panel (5 rows with Vertical Layout Group)
5. [ ] Build complete Setup screen layout
6. [ ] Build complete Gameplay screen layout
7. [ ] Wire up to existing game systems
8. [ ] Test all animations
9. [ ] Remove/archive legacy UI components

---

**End of New UI Architecture Document**

# Don't Lose Your Head - Developer Todo

**Project:** Don't Lose Your Head
**Developer:** TecVooDoo LLC
**Last Updated:** January 4, 2026

---

## Current Sprint: New UI System Rebuild

### Completed Tasks

- [x] Create LetterCellUI component (`Assets/DLYH/Scripts/UI/LetterCellUI.cs`)
- [x] Create WordPatternRowUI component (`Assets/DLYH/Scripts/UI/WordPatternRowUI.cs`)
- [x] Create LetterCellUI prefab (`Assets/DLYH/NewUI/Prefabs/LetterCellUI.prefab`)
- [x] Configure Canvas for mobile-forward design (1080x1920 reference, Scale With Screen Size)
- [x] Set up WordPatternRowUI in NewUIDesign scene (cells rendering correctly)

### In Progress

- [ ] Make cells appear as continuous row (no visual gaps between cells)
  - Current: Cells render with spacing between them
  - Goal: 12 cells should appear as one continuous horizontal bar
  - Fix: Set Horizontal Layout Group spacing to 0, may need to adjust cell backgrounds

### Pending Tasks

- [ ] Create action button icons (sprites):
  - Select icon (compass/target)
  - Place icon (checkmark)
  - Delete icon (X/trash)
  - GuessWord icon (question mark/brain)
- [ ] Save WordPatternRowUI as prefab
- [ ] Build word pattern panel (5 rows stacked vertically)
- [ ] Build Setup screen layout
- [ ] Build Gameplay screen layout
- [ ] Integrate new UI with existing game systems
- [ ] Remove/archive old UI components

---

## New UI Architecture

### Design Principles

1. **Mobile-Forward**: 1080x1920 reference resolution, Scale With Screen Size (Match 0.5)
2. **Unified Cell System**: All row elements use LetterCellUI (label, letters, action buttons)
3. **Animation-Ready**: DOTween integration for spin, scale, shake, color transitions
4. **Player Color Theming**: Cells support player color for revealed letters

### Component Hierarchy

```
Canvas (Screen Space - Overlay)
├── Canvas Scaler (1080x1920, Scale With Screen Size, Match 0.5)
└── WordPatternRowUI
    └── CellContainer (Horizontal Layout Group)
        ├── Cell_RowLabel (LetterCellUI) - shows "1", "2", etc.
        ├── Cell_Letter0-7 (LetterCellUI) - letter or underscore
        ├── Cell_Select (LetterCellUI) - action icon
        ├── Cell_Place (LetterCellUI) - action icon
        └── Cell_Delete (LetterCellUI) - action icon
```

### Key Files

| File | Purpose |
|------|---------|
| `Assets/DLYH/Scripts/UI/LetterCellUI.cs` | Unified cell component |
| `Assets/DLYH/Scripts/UI/WordPatternRowUI.cs` | Row manager for 12 cells |
| `Assets/DLYH/NewUI/Prefabs/LetterCellUI.prefab` | Cell prefab |
| `Scenes/NewUIDesign.unity` | Test scene for new UI |

### LetterCellUI Structure

```
LetterCellUI (RectTransform + Image + CanvasGroup + Layout Element + LetterCellUI script)
├── LetterText (TextMeshProUGUI) - stretch fill, auto-size 12-48
└── IconImage (Image) - stretch with 8px padding, disabled by default
```

### Cell Content Types

- **Letter**: Shows A-Z or underscore (LetterText enabled, IconImage disabled)
- **Icon**: Shows sprite (IconImage enabled, LetterText disabled)
- **Empty**: Invisible placeholder (both disabled, maintains layout)

### Cell States

- **Default**: Normal background color
- **Selected**: Yellow highlight
- **Revealed**: Green or player color
- **Locked**: Cannot be modified
- **Disabled**: Grayed out
- **Highlighted**: Hover state

### Row Layout

- **Total Cells**: 12
- **Cell 0**: Row label (displays row number)
- **Cells 1-8**: Letter cells (visibility based on word length 3-8)
- **Cells 9-11**: Action cells (Setup: Select/Place/Delete, Gameplay: GuessWord)

### Prefab Settings

**LetterCellUI Prefab:**
- Layout Element: Preferred 150x150 (adjustable), Flexible Width 1
- Image: UISprite, white color
- CanvasGroup: Alpha 1, Interactable checked

**CellContainer Settings:**
- RectTransform: Stretch both directions (0,0,0,0)
- Horizontal Layout Group: Spacing 4, Child Alignment Middle Left
- Control Child Size: Width checked, Height checked
- No Layout Element (removed to allow proper stretching)

**WordPatternRowUI Settings:**
- RectTransform: Stretch horizontal, Height 100
- No Horizontal Layout Group on row itself (only on CellContainer)

---

## Known Issues

1. **Visual Gap Between Cells**: Cells currently have spacing. Need to set spacing to 0 for continuous appearance.

---

## Session Notes (January 4, 2026)

### What We Accomplished

1. Created new UI system from scratch in NewUIDesign scene
2. Built LetterCellUI as unified cell component supporting letters, icons, and animations
3. Built WordPatternRowUI with unified 12-cell approach (row label + 8 letters + 3 actions)
4. Fixed multiple layout issues:
   - Text overflow (LetterText needed stretch anchors)
   - Tiny cells (container was too small)
   - Layout group conflicts (removed from row, kept on container)

### Key Decisions

1. **Unified cells everywhere**: User requested all row elements be LetterCellUI instances for uniform appearance and animation capability
2. **Mobile-forward**: Starting with 1080x1920 reference to ensure mobile compatibility from day one
3. **Dynamic cell creation**: Cells instantiated at runtime, not preset in prefab

### Next Session

1. Fix cell spacing for continuous row appearance
2. Create action button icon sprites
3. Save WordPatternRowUI as prefab
4. Build full word pattern panel with 5 rows

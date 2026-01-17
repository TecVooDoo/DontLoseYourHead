# Don't Lose Your Head - Refactoring Plan (Phase 2)

**Version:** 1.2 FINAL
**Date Created:** January 16, 2026
**Last Updated:** January 16, 2026
**Status:** ✅ COMPLETE
**Purpose:** Clean up codebase after UI Toolkit migration, consolidate genuine duplication, delete unused legacy files
**Philosophy:** Refactor only when it improves the code, not to hit arbitrary targets

---

## Refactoring Philosophy

**THE GOLDEN RULE: Don't refactor for the sake of refactoring. Every change needs a reason.**

### Priority Order (When Making Decisions)

1. **Memory Efficiency** - Nothing kills fun like lag. No per-frame allocations, no unnecessary object creation.
2. **SOLID Principles** - Single responsibility, open/closed, Liskov substitution, interface segregation, dependency inversion.
3. **Self-Documenting Code** - If code is written well, it shouldn't need comments. Clear naming > comments.
4. **Clean & Maintainable** - Easy to read, easy to modify, consistent patterns.
5. **Reusability** (lowest priority) - Nice if code can be reused in future projects, but never at the expense of the current project.

### Line Count Guidelines

- **800-1200 lines** is a target range for Claude compatibility, NOT a mandate
- If a file is 1,300+ lines but cohesive and well-organized, **leave it alone**
- Only extract if it creates clearer responsibilities or reduces genuine duplication
- **Never degrade code quality just to reduce line count**

### When TO Refactor

- A file is doing multiple unrelated things (violates single responsibility)
- The same code is literally copy-pasted in multiple places (maintenance burden)
- A file is so large that Claude struggles to work with it effectively
- Legacy code is genuinely unused and creates confusion

### When NOT TO Refactor

- A large file is cohesive and handles one concern well
- "Similar" code serves different purposes (e.g., AI word lists vs player word lists)
- Extraction would create unnecessary indirection without benefit
- The refactoring would hurt readability or performance

---

## Executive Summary

After completing the UI Toolkit migration (Phases A-D), the codebase has:
- **Active UI Toolkit code** in `Assets/DLYH/NewUI/Scripts/` (20 scripts)
- **Legacy uGUI code** in `Assets/DLYH/Scripts/UI/` (67 scripts) - verify unused, then delete
- **UIFlowController.cs at 5,510 lines** - legitimately too large, doing too many unrelated things
- **Some duplicate patterns** - modal creation is literally copy-pasted 3x (genuine duplication)

---

## Phase 2.1: UIFlowController Analysis Results

**Current State:** 5,510 lines total
- `UIFlowController` class: Lines 1-4686 (~4,686 lines)
- `SetupWizardUIManager` class: Lines 4692-5509 (~817 lines) - **already separate, just in same file**

### Why UIFlowController is Large (Justified vs Not)

| Responsibility | Lines | Justified? | Action |
|----------------|-------|------------|--------|
| SetupWizardUIManager (separate class) | ~817 | No - should be own file | **MOVE to separate file** |
| Screen orchestration (Main Menu, Setup, Gameplay) | ~600 | Yes - that's its job | Keep |
| Turn Management (#region) | ~220 | Maybe - distinct concern | Evaluate |
| AI Opponent coordination (#region) | ~670 | Maybe - distinct concern | Evaluate |
| Win/Lose Detection (#region) | ~150 | Maybe - distinct concern | Evaluate |
| Modal creation (3 modals) | ~360 | Yes - UI concerns | Keep (minor duplication) |
| Gameplay event handlers | ~800 | Yes - gameplay flow | Keep |
| Setup/Placement handlers | ~600 | Yes - setup flow | Keep |

### Recommended Extractions (Justified)

| Extraction | Lines | Reason | Risk |
|------------|-------|--------|------|
| **SetupWizardUIManager.cs** | ~817 | Already a separate class, just in wrong file | **Zero** |
| **TurnManager.cs** (maybe) | ~220 | Distinct responsibility, clear boundaries | Low |
| **OpponentCoordinator.cs** (maybe) | ~670 | Distinct responsibility, clear boundaries | Medium |

**NOT recommended:**
- ModalManager.cs - Feedback uses UXML, others are programmatic. Only ~15 lines of shared container setup. Not worth the abstraction.
- UIHelpers.cs - ShowElement/HideElement are 2-line methods. Creating a file for this adds indirection without benefit.
- AudioCoordinator.cs - Audio calls are simple one-liners. No consolidation needed.

### Post-Extraction Estimate

After moving SetupWizardUIManager: **~3,870 lines** (still large, but cohesive)

If we also extract TurnManager + OpponentCoordinator: **~2,980 lines** (much better)

**Decision:** Start with SetupWizardUIManager (zero risk), then evaluate if Turn/Opponent extraction improves clarity.

### Extraction Order (Dependency-Safe)

1. **UIHelpers.cs** - No dependencies, used by everything else
2. **ModalManager.cs** - Self-contained, enables testing modals independently
3. **AudioCoordinator.cs** - Simple utility, no UI dependencies
4. **MainMenuManager.cs** - Entry point, depends on UIHelpers
5. **SetupFlowManager.cs** - Depends on UIHelpers, ModalManager
6. **GameplayFlowManager.cs** - Most complex, depends on all above

---

## Phase 2.2: Code Consolidation

### A. Modal Pattern Consolidation

**Current:** 3 modals with duplicate creation/show/hide logic (200+ duplicate lines)

**Before (repeated 3x):**
```csharp
private void CreateXxxModal()
{
    _xxxModalContainer = new VisualElement();
    _xxxModalContainer.style.position = Position.Absolute;
    _xxxModalContainer.style.left = 0;
    // ... 50+ lines of identical setup
    _xxxModalContainer.RegisterCallback<ClickEvent>(evt => {
        if (evt.target == _xxxModalContainer) HideXxxModal();
    });
    _root.Add(_xxxModalContainer);
    _xxxModalContainer.AddToClassList("hidden");
}

private void ShowXxxModal()
{
    DLYH.Audio.UIAudioManager.PopupOpen();
    _xxxModalContainer.RemoveFromClassList("hidden");
}

private void HideXxxModal()
{
    DLYH.Audio.UIAudioManager.PopupClose();
    _xxxModalContainer.AddToClassList("hidden");
}
```

**After (ModalManager.cs):**
```csharp
public class ModalManager
{
    private readonly VisualElement _root;

    public VisualElement CreateModalContainer(string name, Action onBackgroundClick = null)
    {
        VisualElement container = new VisualElement();
        container.name = name;
        container.style.position = Position.Absolute;
        container.style.left = 0;
        container.style.right = 0;
        container.style.top = 0;
        container.style.bottom = 0;
        container.style.alignItems = Align.Center;
        container.style.justifyContent = Justify.Center;
        container.style.backgroundColor = UIColors.ModalOverlay;
        container.pickingMode = PickingMode.Position;

        if (onBackgroundClick != null)
        {
            container.RegisterCallback<ClickEvent>(evt => {
                if (evt.target == container) onBackgroundClick();
            });
        }

        _root.Add(container);
        container.AddToClassList("hidden");
        return container;
    }

    public VisualElement CreateModalPanel(int minWidth = 340, int maxWidth = 500)
    {
        VisualElement panel = new VisualElement();
        panel.style.backgroundColor = UIColors.ModalBackground;
        panel.style.borderTopLeftRadius = 12;
        panel.style.borderTopRightRadius = 12;
        panel.style.borderBottomLeftRadius = 12;
        panel.style.borderBottomRightRadius = 12;
        panel.style.paddingLeft = 24;
        panel.style.paddingRight = 24;
        panel.style.paddingTop = 20;
        panel.style.paddingBottom = 20;
        panel.style.minWidth = minWidth;
        panel.style.maxWidth = maxWidth;
        panel.pickingMode = PickingMode.Position;
        return panel;
    }

    public void Show(VisualElement container)
    {
        DLYH.Audio.UIAudioManager.PopupOpen();
        container?.RemoveFromClassList("hidden");
    }

    public void Hide(VisualElement container)
    {
        DLYH.Audio.UIAudioManager.PopupClose();
        container?.AddToClassList("hidden");
    }
}
```

### B. Color Constants Consolidation

**Current:** Inline color definitions scattered across 5+ files

**Add to ColorRules.cs:**
```csharp
// UI Colors (Modal/Button styling)
public static readonly Color ModalOverlay = new Color(0f, 0f, 0f, 0.7f);
public static readonly Color ModalBackground = new Color(0.15f, 0.15f, 0.2f, 1f);
public static readonly Color ButtonRed = new Color(0.6f, 0.2f, 0.2f, 1f);
public static readonly Color ButtonRedDark = new Color(0.4f, 0.2f, 0.2f, 1f);
public static readonly Color TextLight = new Color(0.9f, 0.9f, 0.9f, 1f);
public static readonly Color TextMuted = new Color(0.8f, 0.8f, 0.8f, 1f);
public static readonly Color KeyDisabled = new Color(0.3f, 0.3f, 0.3f, 0.6f);
```

### C. Show/Hide Helper Consolidation

**Current:** UIFlowController has private helpers at lines 5441-5449, but they're not used consistently

**Create UIHelpers.cs:**
```csharp
public static class UIHelpers
{
    public static void Show(VisualElement element)
    {
        element?.RemoveFromClassList("hidden");
    }

    public static void Hide(VisualElement element)
    {
        element?.AddToClassList("hidden");
    }

    public static void SetVisible(VisualElement element, bool visible)
    {
        if (visible)
            element?.RemoveFromClassList("hidden");
        else
            element?.AddToClassList("hidden");
    }
}
```

**Apply to:** UIFlowController (28 occurrences), GameplayScreenManager, GuillotineOverlayManager, SetupWizardController

---

## Phase 2.3: Legacy File Deletion

### Analysis Results

**Files still referenced by NewUI (KEEP for now):**
1. `WordValidationService.cs` - Used by UIFlowController for word validation
2. `GuillotineDisplay.cs` - Animation constants referenced (could copy constants and delete later)
3. `MainMenuController.cs` - Still attached to GameObjects in scenes (need scene cleanup first)
4. `AutocompleteDropdown.cs` - Still attached to GameObjects in scenes (need scene cleanup first)

**Files safe to delete (no code references):**
- GameplayUIController.cs
- PlayerGridPanel.cs
- SetupSettingsPanel.cs
- WordPatternRow.cs
- WordPatternRowUI.cs
- LetterCellUI.cs
- SetupModeController.cs
- LetterButton.cs
- WordPatternPanelUI.cs
- GridCellUI.cs
- GameplayStateTracker.cs
- WinConditionChecker.cs
- GuessProcessor.cs
- All Controllers/ folder files
- HelpOverlay.cs

### Deletion Process (Safe Order)

**Step 1: Scene Cleanup (MUST DO FIRST)**
Before deleting any scripts, remove legacy component references from scenes:
- [ ] Open NewUIScene.unity
- [ ] Delete GameObjects with legacy uGUI components (MainMenuController, AutocompleteDropdown, etc.)
- [ ] Save scene
- [ ] Repeat for NetworkingTest.unity

**Step 2: Delete Unused Scripts**
After scene cleanup:
- [ ] Delete all files in Assets/DLYH/Scripts/UI/Controllers/
- [ ] Delete unused scripts listed above
- [ ] Keep WordValidationService.cs (still used)

**Step 3: Evaluate Remaining References**
- [ ] Check if GuillotineDisplay constants can be copied to GuillotineOverlayManager
- [ ] If so, delete GuillotineDisplay.cs

**Step 4: Delete Prefabs**
- [ ] Delete all prefabs in Assets/DLYH/Prefabs/UI/ (all uGUI-based)

**Step 5: Delete Test Scenes**
- [ ] Delete GuillotineTesting.unity
- [ ] Delete NewPlayTesting.unity
- [ ] KEEP NetworkingTest.unity (needed for Phase E)

### Prefabs to DELETE (Assets/DLYH/Prefabs/UI/)

- [ ] GridCellUI.prefab
- [ ] GuessedWord.prefab
- [ ] AutocompleteItem.prefab
- [ ] WordPatternsContainer.prefab
- [ ] PlayerGridPanel.prefab

### Scenes to DELETE

- [ ] GuillotineTesting.unity - testing scene, no longer needed
- [ ] NewPlayTesting.unity - testing scene, superseded by NewUIScene

### Scenes to KEEP

- [x] NewUIScene.unity - primary active scene
- [x] NetworkingTest.unity - needed for Phase E

---

## Phase 2.4: GameplayScreenManager Evaluation

**Current:** 1,390 lines (slightly over 1,200 guideline)

### Question to Answer: Is this file cohesive?

If GameplayScreenManager handles ONE concern well (managing the gameplay screen), then 1,390 lines is acceptable.

**Do NOT refactor if:**
- The file has a single, clear responsibility
- Extracting would create unnecessary indirection
- The code is readable and maintainable as-is

**Only refactor if:**
- There are genuinely unrelated responsibilities mixed together
- Claude consistently struggles to work with this file

**Decision:** Evaluate after reviewing the actual code structure. Line count alone is not a reason to refactor.

---

## Phase 2.5: Verification Checklist

### Before Deleting Any File

1. [ ] Search for references in NewUI scripts
2. [ ] Search for references in scene files
3. [ ] Check if any ScriptableObjects reference it
4. [ ] Run game and verify functionality

### After Each Extraction

1. [ ] Run Unity - no compile errors
2. [ ] Test main menu navigation
3. [ ] Test setup wizard flow
4. [ ] Test gameplay (start game, make guesses, win/lose)
5. [ ] Test all modals (How to Play, Feedback, Confirmation)
6. [ ] Test audio (all sounds working)

### Final Verification

1. [ ] All scripts under 1,200 lines
2. [ ] No duplicate code patterns
3. [ ] No references to deleted files
4. [ ] Game fully functional
5. [ ] No console errors/warnings

---

## Revised Session Breakdown

Based on analysis, here's a realistic plan that only does justified refactoring:

### Session 1: Zero-Risk Extraction + Scene Cleanup (COMPLETE - Jan 16, 2026)
- [x] Move SetupWizardUIManager to its own file (already a separate class, just in wrong file)
- [x] Open NewUIScene.unity and delete legacy uGUI GameObjects (Canvas deleted)
- [x] Verify game still works
- [x] Git commit

**Why:** SetupWizardUIManager extraction is zero-risk (no code changes, just file move). Scene cleanup is required before we can delete legacy scripts.

### Session 2: Delete Unused Legacy Files (COMPLETE - Jan 16, 2026)
- [x] Delete all files in Scripts/UI/Controllers/ folder
- [x] Delete unused scripts (GameplayUIController, PlayerGridPanel, SetupSettingsPanel, WordPatternRow, etc.)
- [x] Delete unused prefabs in Prefabs/UI/
- [x] Keep: WordValidationService.cs (still used)
- [x] Verify game still works (full game played beginning to end)
- [x] Created AudioSettings.cs (replacement for deleted SettingsPanel volume methods)
- [x] Added WordPlacementData.RowIndex property (required by AISetupManager)
- [x] Added GameplayStateTracker.AddOpponentMisses method (required by GameStateSynchronizer)
- [x] Fixed DifficultySetting to int casts in LocalAIOpponent and RemotePlayerOpponent

**Why:** These files are genuinely unused and create confusion.

### Session 3: Folder Cleanup & Finalization (COMPLETE - Jan 16, 2026)
- [x] Reorganized folder structure: moved NewUI/Scripts/* to Scripts/UI/
- [x] Moved NewUI assets (USS, UXML, Prefabs) to UI/ folder
- [x] Deleted empty NewUI folder
- [x] Cleaned up scene: removed GameObjects with missing scripts
- [x] Removed development comments (TODO, FIXME, Debug.Log) from UI scripts
- [x] Updated DLYH_Status.md to version 57
- [x] Git commit

**Decision:** UIFlowController at ~4400 lines is cohesive and working well. No further extraction needed at this time. Further extraction (TurnManager, OpponentCoordinator) deferred to Phase F if needed.

**Why:** The codebase is now clean, organized, and functional. Further refactoring would be premature optimization.

---

## Files After Refactoring

### NewUI/Scripts/ (Target Structure)

```
NewUI/Scripts/
  Core/
    UIHelpers.cs (~100 lines)
    AudioCoordinator.cs (~80 lines)
    ModalManager.cs (~300 lines)
  Screens/
    MainMenuManager.cs (~400 lines)
    SetupFlowManager.cs (~600 lines)
    GameplayFlowManager.cs (~800 lines)
  Components/
    GameplayScreenManager.cs (~1000-1200 lines)
    GuillotineOverlayManager.cs (~450 lines)
    SetupWizardController.cs (~943 lines)
    WordRowView.cs (~1040 lines)
    WordRowsContainer.cs (~300 lines)
    WordSuggestionDropdown.cs (~200 lines)
    PlacementAdapter.cs (~801 lines)
    GameplayGuessManager.cs (~841 lines)
  Table/
    TableModel.cs
    TableView.cs
    TableLayout.cs
    TableCell.cs
    TableCellKind.cs
    TableCellState.cs
    CellOwner.cs
    TableRegion.cs
    ColorRules.cs (expanded with UI colors)
  UIFlowController.cs (~800-1000 lines - orchestration only)
```

---

## Code Style Requirements (from original refactoring)

- [ ] No `var` usage - explicit types always
- [ ] No GetComponent in hot paths
- [ ] No allocations in Update
- [ ] Private fields use _camelCase
- [ ] Methods under 20 lines (initialization excepted)
- [ ] Events named On + PastTense (OnWordValidated)

---

## Risk Mitigation

1. **Git commits after each session** - easy rollback
2. **Test after each extraction** - catch issues early
3. **Keep deleted files in Archive folder first** - can restore if needed
4. **Don't delete Services until verified unused** - some may still be referenced
5. **Full backup available** - `E:\Unity\DontLoseYourHeadBackup` contains pre-refactoring copy of the project

---

**End of Refactoring Plan**

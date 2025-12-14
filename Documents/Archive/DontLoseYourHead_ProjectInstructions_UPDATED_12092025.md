# Don't Lose Your Head - Claude Project Instructions

**Project:** Don't Lose Your Head  
**Developer:** TecVooDoo LLC  
**Designer:** Rune (Stephen Brandon)  
**Unity Version:** 6.3 (2D Template)  
**Date Created:** November 20, 2025  
**Last Updated:** December 9, 2025  

---

## Core Development Philosophy

### Step-by-Step Verification Protocol

**CRITICAL: Never rush ahead with multiple steps**

- Provide ONE step at a time
- Wait for user confirmation via text OR screenshot before proceeding
- User will verify each step is complete before moving forward
- If a step fails, troubleshoot that specific step before continuing
- Assume nothing - verify everything

### File Encoding Rule

**CRITICAL: ASCII Only**

- All scripts and text files MUST use ASCII encoding
- Do NOT use UTF-8 or other encodings
- Avoid special characters, smart quotes, em-dashes, etc.
- Use standard apostrophes (') not curly quotes
- Use regular hyphens (-) not em-dashes

### File Naming Convention

**When creating updated documentation or files:**

Format: `OriginalFileName_UPDATED_MMDDYYYY.ext`

**Examples:**
- `ProjectInstructions_UPDATED_12092025.md`
- `GDD_UPDATED_12092025.md`

### Code Editing Preference

**For manual copy/paste (when providing code to user):**
- Provide COMPLETE file replacements
- Easier than hunting for specific lines
- Reduces errors from partial edits

**For MCP direct edits (Claude editing via tools):**
- Use `script_apply_edits` for method-level changes
- Use `apply_text_edits` for precise line/column edits
- Use `validate_script` to check for errors
- Direct MCP edits are preferred when connection is stable

---

## Unity MCP Tools (v8.2.1)

### Overview

Unity MCP allows Claude to directly interact with the Unity Editor. The project uses:
- **MCP Package:** Installed from local files at `E:\Unity\unity-mcp-main`
- **Transport:** Stdio (required for Claude Desktop) - Note: HTTP-First Transport is now the default in MCP, but stdio remains available
- **Server Source:** Local override pointing to downloaded repository

### Script Operations

| Tool | Purpose | When to Use |
|------|---------|-------------|
| `manage_script` (read) | Read script contents | Legacy - still works but deprecated |
| `get_sha` | Get SHA256 hash + file size | Precondition checks before edits |
| `validate_script` | Check syntax/structure | Before and after edits |
| `create_script` | Create new C# scripts | New files (also overwrites existing) |
| `delete_script` | Remove scripts | By URI or Assets-relative path |

### Script Editing Tools (Preferred)

**`script_apply_edits`** - Structured C# edits with safer boundaries:
- `replace_method` - Replace entire method body
- `insert_method` - Add new method (position: start/end/after/before)
- `delete_method` - Remove a method
- `replace_class` / `delete_class` - Class-level operations
- `anchor_insert` / `anchor_delete` / `anchor_replace` - Pattern-based edits

**`apply_text_edits`** - Precise text edits:
- Line/column coordinate-based changes
- Atomic multi-edit batches
- Precondition SHA256 hashes for safety

### Asset and Scene Operations

| Tool | Purpose |
|------|---------|
| `manage_asset` | Import, create, modify, delete, search assets |
| `manage_scene` | Load, save, create scenes, get hierarchy |
| `manage_gameobject` | Create, modify, find GameObjects and components |
| `manage_prefabs` | Open/close prefab stage, create from GameObject |
| `manage_editor` | Play/pause/stop, tags, layers, editor state |
| `manage_material` | Create materials, set properties, colors, assign to renderers |

### Console and Testing

| Tool | Purpose |
|------|---------|
| `read_console` | Get Unity console messages (errors, warnings, logs) |
| `run_tests` | Execute EditMode or PlayMode tests |

### Batch Operations (NEW in v8.2)

| Tool | Purpose |
|------|---------|
| `batch_execute` | Run multiple MCP commands in a single call for efficiency |

### Script Path Format

Always use Assets-relative paths:
```
Assets/DLYH/Scripts/UI/SetupSettingsPanel.cs
Assets/DLYH/Scripts/Core/GameManager.cs
```

### MCP Best Practices

1. **Read before editing:** Use `manage_script` read or view the file before making changes
2. **Validate after edits:** Run `validate_script` to catch syntax errors immediately
3. **Use structured edits:** Prefer `script_apply_edits` over raw text edits for methods
4. **Check console:** Use `read_console` after changes to catch runtime issues
5. **SHA256 preconditions:** For critical edits, use `get_sha` to ensure file hasn't changed
6. **Avoid verbose output:** Don't display full tool call details for read operations (causes UI lockup)
7. **Avoid hierarchy modifications:** MCP hierarchy changes can cause Unity lockups - use script edits instead
8. **Use batch_execute:** For multiple related operations, batch them for efficiency

### Troubleshooting MCP Connection

If MCP tools fail:
1. Check Unity Editor is open with the correct project
2. Verify MCP window shows "Session Active" (Window > MCP for Unity)
3. Check terminal window running the server is still open
4. Restart: Stop server, restart Unity, start server again

### MCP Limitations (Dec 2025)

- **Hierarchy modifications cause Unity lockups** - Avoid `manage_gameobject` for creating/modifying hierarchy
- **Script edits are reliable** - `script_apply_edits` and `create_script` work well
- **Fallback to manual replacement** - When MCP is unstable, provide complete file for manual copy/paste

---

## Current Project Status

### Completed Systems [X]

**Core Foundation:**
- [X] Folder structure (Assets/DLYH/)
- [X] Difficulty system with enums and calculator
- [X] Grid system (Grid, GridCell, Word classes)
- [X] Word placement validation and placement logic
- [X] Reactive variable system (IntVariableSO)
- [X] ScriptableObject event system (GameEventSO, GameEventListener)

**Game Systems:**
- [X] GameManager with letter and coordinate guessing
- [X] Turn Management System (TurnManager, CurrentPlayerTurn)
- [X] Player System (PlayerSO, PlayerManager)
- [X] Game Flow State Machine (6 phases: MainMenu -> GameOver)
- [X] Win/Lose condition checking (automatic detection)
- [X] Word guessing with 2-miss penalty for incorrect guesses
- [X] Letter reveal mechanics (asterisks upgrade to letters when learned)

**Word Bank:**
- [X] WordListSO ScriptableObject for word collections
- [X] WordBankImporter editor tool (DLYH > Tools menu)
- [X] 3-letter words: 2,130 words
- [X] 4-letter words: 7,186 words
- [X] 5-letter words: 15,921 words
- [X] 6-letter words: Available (reimported correctly Dec 2025)

**UI Foundation:**
- [X] PlayerGridPanel prefab (scalable 6x6 to 12x12)
- [X] GridCellUI prefab with hidden letter support
- [X] LetterButton prefab with state management
- [X] AutocompleteItem prefab
- [X] Canvas setup (Scale With Screen Size, 1080x1920)

**Setup Mode UI (COMPLETE - Dec 4, 2025):**
- [X] SetupSettingsPanel with all controls
- [X] 8-button color picker with outline selection highlight
- [X] Grid size dropdown (6x6 through 12x12)
- [X] Word count dropdown (3 or 4)
- [X] Difficulty dropdown (Easy/Normal/Hard)
- [X] Miss limit live preview
- [X] Word entry via letter tracker and keyboard
- [X] Word validation against word bank
- [X] Coordinate placement mode with visual feedback
- [X] Auto-accept words at correct length
- [X] Delete button clears word from row AND grid
- [X] Compass button hides after placement
- [X] Input field focus handling (name field vs word rows)
- [X] Pick Random Words button (event-driven, fills only empty rows)
- [X] Place Random Positions button (longest-to-shortest order)
- [X] Grid clears when grid size changes

**Code Refactoring (Dec 5, 2025):**
- [X] LetterTrackerController extracted from PlayerGridPanel
- [X] GridColorManager extracted from PlayerGridPanel
- [X] PlayerColorController extracted from SetupSettingsPanel
- [X] WordValidationService extracted from SetupSettingsPanel
- [X] Difficulty dropdown renamed (Forgiveness -> Difficulty, reordered to Easy/Normal/Hard)

**Gameplay Mode UI (Dec 6-9, 2025):**
- [X] GameplayUIController with two-panel system
- [X] Horizontal 50/50 layout for Setup Mode
- [X] Dynamic cell sizing for larger grids
- [X] Owner panel shows player's words fully revealed
- [X] Opponent panel with hidden letter support
- [X] Data transfer from setup to gameplay
- [X] Miss limit calculation using opponent's grid settings
- [X] Word row ordering fix (sibling index sort)
- [X] Start button event subscriptions
- [X] DifficultySO backward-compatible MissLimit property
- [X] CacheWordPatternRows() fix for inactive panel activation
- [X] Guillotine graphics and miss counters displayed
- [ ] Turn-based interaction (click to guess)

**Package Cleanup (Dec 8, 2025):**
- Reduced from 16 packages to 6 core packages

### Deferred to Final Polish Phase

**UI Polish:**
- [ ] Invalid word feedback UI (toast/popup)
- [ ] Grid row labels compression fix

**Remaining Refactoring (PlayerGridPanel ~1,871 lines):**
- [ ] WordPatternRowManager extraction (~292 lines)
- [ ] CoordinatePlacementController extraction (~213 lines)
- [ ] PlacementPreviewController extraction (~147 lines)
- [ ] GridLayoutManager extraction (~111 lines)

### Next Development Priorities

**Immediate:**
1. Complete Gameplay Mode UI - Turn-based interaction
2. Letter/coordinate/word guessing mechanics

**Next Phase:**
3. AI opponent system
4. Visual polish (animations, effects)
5. Audio implementation

---

## UI Architecture Overview

### Two-Phase Design

**Setup Phase (Per-Player):**
- Horizontal 50/50 split layout (settings left, grid right)
- Dynamic cell sizing based on grid dimensions
- Word entry with validation
- Coordinate placement with visual feedback

**Gameplay Phase (Alternating Turns):**
- Two PlayerGridPanels visible (owner left, opponent right)
- Center area with guillotines and miss counters
- Click opponent's elements to make guesses

### Key UI Scripts

| Script | Purpose | Lines | Notes |
|--------|---------|-------|-------|
| PlayerGridPanel.cs | Main grid container, mode switching | ~1,871 | Has 2 controllers integrated, more extraction TODO |
| SetupSettingsPanel.cs | Player config, difficulty, word lists | ~760 | Refactored Dec 5, 2025 |
| GameplayUIController.cs | Two-panel gameplay system | ~400 | NEW Dec 6-9, 2025 |
| SetupModeController.cs | Keyboard input routing | ~150 | |
| WordPatternRow.cs | Individual word entry rows | ~300 | |
| LetterButton.cs | Letter tracker buttons with states | ~200 | |
| GridCellUI.cs | Individual grid cells, hidden letter support | ~200 | Enhanced Dec 6, 2025 |

### Extracted Controllers/Services (Dec 5, 2025)

| File | Purpose | Location |
|------|---------|----------|
| LetterTrackerController.cs | Letter button management, click handling | Assets/DLYH/Scripts/UI/Controllers/ |
| GridColorManager.cs | Grid cell color state management | Assets/DLYH/Scripts/UI/Controllers/ |
| PlayerColorController.cs | Color picker button management | Assets/DLYH/Scripts/UI/Controllers/ |
| WordValidationService.cs | Word validation against word bank | Assets/DLYH/Scripts/UI/Services/ |

### Event-Driven Button State Management

Both random action buttons use event subscriptions for dynamic state:

**Pick Random Words:**
- Subscribes to: `WordPatternRow.OnWordAccepted`, `OnDeleteClicked`
- ENABLED when any row is empty
- DISABLED when all rows filled

**Place Random Positions:**
- Subscribes to: `WordPatternRow.OnWordAccepted`, `OnDeleteClicked`, `PlayerGridPanel.OnWordPlaced`
- ENABLED when any word entered but not placed
- DISABLED when all placed or none entered

**Start Button:**
- Subscribes to: `OnWordPlaced`, `OnWordDeleted`, `OnWordLengthsChanged`
- ENABLED when all words placed on grid
- DISABLED otherwise

### Input Field Focus Handling

**Critical:** SetupModeController must check if a TMP input field is focused before processing keyboard input. The pattern:

```csharp
// In Update(), check focus before processing letters
if (EventSystem.current.currentSelectedGameObject != null)
{
    var inputField = EventSystem.current.currentSelectedGameObject
        .GetComponent<TMP_InputField>();
    if (inputField != null)
    {
        return; // Skip keyboard processing - input field handles it
    }
}
```

---

## Architecture Principles

### SOLID Principles (Mandatory)

- **S** - Single Responsibility: Each class has one reason to change
- **O** - Open/Closed: Use inheritance and interfaces
- **L** - Liskov Substitution: Derived classes substitutable for base
- **I** - Interface Segregation: Many specific interfaces
- **D** - Dependency Inversion: Depend on abstractions

### ScriptableObject Architecture

**Primary Pattern:**
- Game data as ScriptableObjects
- Game events via ScriptableObject events
- Runtime sets for object tracking
- Variables as ScriptableObjects
- Word lists as ScriptableObjects (WordListSO)

### Controller/Service Pattern (Dec 5, 2025)

**For UI Panels:**
- Extract focused responsibilities into Controller classes
- Controllers receive dependencies via constructor injection
- Controllers expose events for communication
- Controllers have Initialize() and Cleanup() lifecycle methods
- Main panel coordinates between controllers

**For Business Logic:**
- Extract into Service classes
- Services are stateless where possible
- Services receive data dependencies via constructor
- Services return results, don't modify UI directly

### Defensive Awake() Programming (Dec 6, 2025)

**Critical for inactive GameObject configuration:**
- Check if already configured before reinitializing arrays
- Preserve existing data when SetActive(true) triggers Awake()
- Refresh display if already in target mode

```csharp
private void Awake()
{
    // Preserve existing data if already configured
    if (_revealedLetters != null && _revealedLetters.Length > 0)
    {
        // Already configured - just refresh display if in Gameplay mode
        if (_mode == PanelMode.Gameplay)
        {
            RefreshDisplay();
        }
        return;
    }
    
    // Normal initialization for fresh instances
    _revealedLetters = new bool[MAX_WORD_LENGTH];
}
```

---

## Asset Priority System

### Tier 1: Core Dependencies (6 Packages - Dec 9, 2025)

| Package | Version | Purpose |
|---------|---------|---------|
| DOTween Pro | 1.0.386 | ALL animations and tweening |
| Feel | 5.9.1 | Game juice and feedback effects |
| Odin Inspector and Serializer | 4.0.1.0 | Custom editors, data structures |
| Odin Validator | 4.0.1.1 | Project validation |
| SOAP | 3.6.1 | ScriptableObject Architecture Pattern |
| MCP for Unity | 8.2.1 (Local) | Development workflow |

### Unity Built-ins

- TextMeshPro (all text rendering)
- Unity UI (UGUI)
- New Input System

### Removed Assets (Dec 8, 2025)

- Easy Popup System (toast UI deferred)
- Scriptable Sheets (word bank complete)
- Hot Reload (dev convenience, adds complexity)
- UniTask (not using async/await patterns)
- vHierarchy 2 / vFavorites 2 (editor only)
- Various other packages (reduced from 16 to 6)

---

## Key Reminders

**Always:**
- [X] Wait for user verification before proceeding
- [X] Provide COMPLETE file replacements when giving code to user manually
- [X] Use MCP tools for direct edits when connection is stable
- [X] Use ASCII encoding only
- [X] Look up current Unity 6.3 documentation
- [X] Use Odin Inspector for data structures
- [X] Use DOTween Pro for animations
- [X] Follow SOLID principles
- [X] Use ScriptableObject architecture
- [X] Check input field focus before processing keyboard input
- [X] Validate scripts after MCP edits
- [X] Avoid verbose tool call output (causes UI lockup)
- [X] Extract controllers/services following established patterns
- [X] Use defensive Awake() programming for inactive GameObjects
- [X] Sort word rows by sibling index, not GetComponentsInChildren order
- [X] Call CacheWordPatternRows() before accessing rows on newly activated panels

**Never:**
- [ ] Rush ahead with multiple steps
- [ ] Use incremental edits when providing code for manual copy/paste
- [ ] Use UTF-8 or special characters
- [ ] Assume old Unity knowledge is current
- [ ] Use deprecated APIs
- [ ] Create duplicate UI components when one can serve multiple purposes
- [ ] Display full "action: read" tool calls (causes lockup)
- [ ] Use MCP for hierarchy modifications (causes Unity lockup)

---

## Questions Protocol

1. **Clarify the context** - What are you trying to accomplish?
2. **Look up current information** - Search Unity 6.3 docs
3. **Provide step-by-step solution** - One step at a time
4. **Explain the reasoning** - Why this approach?

---

**End of Project Instructions**

These instructions should be followed for every conversation in this project.

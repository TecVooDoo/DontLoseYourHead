# Don't Lose Your Head - Claude Project Instructions

**Project:** Don't Lose Your Head  
**Developer:** TecVooDoo LLC  
**Designer:** Rune (Stephen Brandon)  
**Unity Version:** 6.3 (2D Template)  
**Date Created:** November 20, 2025  
**Last Updated:** December 13, 2025  

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

Format: `OriginalFileName_v#_UPDATED_MMDDYYYY.ext`

Where `v#` is the version number (v1, v2, v3, etc.)

**Examples:**
- `ProjectInstructions_v2_UPDATED_12132025.md`
- `GDD_v2_UPDATED_12132025.md`
- `DESIGN_DECISIONS_v3_UPDATED_12152025.md`

**Version Incrementing:**
- Increment version number when making significant updates
- Use the same version number for related batch updates on the same day

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

### Batch Operations

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

## Pragmatic Architecture Principles

### Core Philosophy

**Only use patterns if they add value, not for the sake of having them.**

The AI system was designed by evaluating 9 Unity AI assets (Behavior Designer, GOAP, NodeCanvas, etc.) and determining all were overkill for turn-based word guessing. A custom ~300-500 line solution beats learning curve + overhead of spatial AI systems designed for real-time games.

### Pattern Decision Framework

| Pattern | Use When | Skip When |
|---------|----------|-----------|
| Interface | Multiple implementations needed, testing isolation required | Only one implementation ever |
| ScriptableObject | Designer needs to tweak values without recompiling | Values never change after initial setup |
| Async/Await | Delays, parallel operations, cleaner than coroutines | Simple sequential code |
| DI Container | Complex dependency graphs, large team | Constructor params suffice |
| State Machine | Many states with complex transitions | Simple if/else covers it |
| Event System | Decoupling unrelated systems | Direct method calls are clearer |

### Established Project Patterns

**ScriptableObjects:** Used for configuration only, not for runtime state or strategy logic.
- ExecutionerConfigSO: AI tuning parameters
- DifficultySO: Difficulty presets
- WordListSO: Word bank data

**Interfaces:** Used for interchangeable strategies.
- IGuessStrategy: Allows swapping letter/coordinate/word strategies

**Async/Await:** Used for think time delays, future animations.
- UniTask preferred over coroutines for cleaner syntax

**Constructor Injection:** Pass dependencies directly, no DI container.
```csharp
public ExecutionerAI(ExecutionerConfigSO config, DifficultyAdapter adapter)
```

### What We Explicitly Avoid

- **Multiple ScriptableObject configs for each difficulty** - Rubber-banding handles this dynamically
- **DI containers (Extenject/VContainer)** - Constructor parameters are sufficient for this project size
- **Complex state machine assets** - Simple enums and switch statements work fine
- **Observable pattern everywhere** - Events handle the few places needing decoupling
- **Over-abstraction** - If there's only one implementation, don't add an interface

---

## AI System Architecture (Phase 3)

### File Structure

```
Assets/DLYH/
  ScriptableObjects/
    AI/
      ExecutionerConfig.asset
  Scripts/
    AI/
      Config/
        ExecutionerConfigSO.cs
      Core/
        ExecutionerAI.cs          (MonoBehaviour - coordinates turn execution)
        DifficultyAdapter.cs      (Runtime state - rubber-banding + adaptive thresholds)
        MemoryManager.cs          (Skill-based information filtering)
      Strategies/
        IGuessStrategy.cs         (Interface)
        LetterGuessStrategy.cs
        CoordinateGuessStrategy.cs
        WordGuessStrategy.cs
      Data/
        LetterFrequency.cs        (Static reference data)
        GridAnalyzer.cs           (Fill ratio calculations)
```

### Key Classes

**ExecutionerConfigSO:** ScriptableObject with all tunable parameters.
- Rubber-banding settings (skill bounds, adjustment step)
- Initial skill and thresholds per difficulty (Easy/Normal/Hard)
- Adaptive threshold settings (bounds, adjustment triggers)
- Strategy preferences (density thresholds, word guess risk)
- Memory settings (forget chance, recent memory count)
- Think time range

**ExecutionerAI:** MonoBehaviour that coordinates turn execution.
- Selects appropriate strategy based on game state
- Manages async think time
- Delegates to DifficultyAdapter for skill tracking

**DifficultyAdapter:** Plain C# class tracking runtime difficulty state.
- Current skill level
- Current hit/miss thresholds (adapt over time)
- Recent player guess history
- Consecutive adjustment tracking for threshold adaptation

**IGuessStrategy:** Interface for interchangeable guess strategies.
```csharp
public interface IGuessStrategy
{
    GuessResult Evaluate(GameState state, float skillLevel);
}
```

### Why This Architecture

1. **Single config ScriptableObject** - All AI parameters in one place, easy to tweak in Inspector
2. **Separate adapter class** - Runtime state separated from configuration
3. **Strategy interface** - Easy to add new strategies or modify existing ones
4. **No DI container** - Dependencies passed through constructors, simple and traceable

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
- [X] 6-letter words: Available

**UI Foundation:**
- [X] PlayerGridPanel prefab (scalable 6x6 to 12x12)
- [X] GridCellUI prefab with hidden letter support
- [X] LetterButton prefab with state management
- [X] AutocompleteItem prefab
- [X] AutocompleteDropdown prefab
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
- [X] Grid row labels resize with grid size changes

**Autocomplete Dropdowns (COMPLETE - Dec 13, 2025):**
- [X] AutocompleteManager controller for word suggestions
- [X] AutocompleteDropdown UI component
- [X] AutocompleteItem prefab for dropdown entries
- [X] Positioned below word rows (not overlapping)
- [X] Max 5 visible items with scrolling
- [X] Font size increased (24pt)
- [X] Hides on: Pick Random Words, word completion, acceptance, mode transition
- [X] Only appears in Setup Mode

**Gameplay Mode UI (COMPLETE - Dec 11, 2025):**
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
- [X] Letter guessing (click opponent's letter tracker)
- [X] Coordinate guessing (click opponent's grid cells)
- [X] Three-color grid cell system (green/red/yellow)
- [X] Yellow-to-green cell upgrade when letter discovered
- [X] Word guessing via row buttons (Guess Word, Backspace, Accept, Cancel)
- [X] Letter tracker keyboard mode during word guess
- [X] Word validation before acceptance
- [X] Solved word row tracking (_wordSolved flag + MarkWordSolved())
- [X] Duplicate guess prevention (GuessResult enum)
- [X] Guessed word lists under guillotines

**Code Refactoring (COMPLETE - Dec 12, 2025):**
- [X] PlayerGridPanel: 2,192 -> 1,120 lines (49% reduction)
- [X] GameplayUIController: 2,112 -> 1,179 lines (44% reduction)
- [X] WordPatternRow: 1,378 -> 1,199 lines (13% reduction)
- [X] Critical bug fix: Unity lifecycle timing (EnsureControllersInitialized pattern)
- [X] Code quality verification passed (no var, no hot path allocations)

**Extracted Controllers/Services:**
- [X] LetterTrackerController (PlayerGridPanel)
- [X] GridColorManager (PlayerGridPanel)
- [X] PlacementPreviewController (PlayerGridPanel)
- [X] WordPatternRowManager (PlayerGridPanel)
- [X] CoordinatePlacementController (PlayerGridPanel)
- [X] GridLayoutManager (PlayerGridPanel)
- [X] PlayerColorController (SetupSettingsPanel)
- [X] WordValidationService (SetupSettingsPanel)
- [X] GuessProcessor (GameplayUIController)
- [X] WordGuessModeController (GameplayUIController)
- [X] WordGuessInputController (WordPatternRow delegation)
- [X] RowDisplayBuilder (WordPatternRow utility)
- [X] AutocompleteManager (SetupSettingsPanel)

**Main Menu and Settings Panel (COMPLETE - Dec 13, 2025):**
- [X] MainMenuController.cs script
- [X] SettingsPanel.cs script (audio volume with PlayerPrefs)
- [X] UI hierarchy created in Unity
- [X] Button references wired
- [X] Navigation flow tested (Main Menu -> Setup -> Gameplay)

**Phase 3: AI Opponent (DESIGNED - Dec 13, 2025):**
- [X] Letter guessing strategy (frequency + pattern analysis)
- [X] Coordinate guessing strategy (adjacency + density awareness)
- [X] Word guessing strategy (confidence thresholds)
- [X] Rubber-banding system (adaptive difficulty)
- [X] Adaptive threshold system (meta-rubber-banding)
- [X] Memory system (skill-based recall)
- [X] Grid density impact on strategy selection

**Package Cleanup (Dec 8, 2025):**
- Reduced from 16 packages to 6 core packages

### Next Development Priorities

**Phase 3: AI Opponent Implementation**
1. ExecutionerConfigSO (tunable parameters)
2. ExecutionerAI controller (MonoBehaviour)
3. DifficultyAdapter (rubber-banding + adaptive thresholds)
4. MemoryManager (skill-based filtering)
5. Strategy classes (Letter, Coordinate, Word)
6. LetterFrequency static data
7. GridAnalyzer utility
8. Win/Lose UI feedback
9. Turn indicator improvements

### Phase 4: Polish and Features

10. Visual polish (DOTween animations, Feel effects)
11. Audio implementation
12. Invalid word feedback UI (toast/popup)
13. Profanity filter for word bank
14. Medieval/carnival themed monospace font

### Phase 5: Multiplayer and Mobile

15. 2-player networking mode (human vs human online)
16. Mobile implementation

### Deferred to Final Polish Phase

**UI Polish:**
- [ ] Invalid word feedback UI (toast/popup)
- [ ] Profanity filter for word bank
- [ ] Medieval/carnival themed monospace font
- [ ] Compiler warnings for unused animation/event fields

---

## UI Architecture Overview

### Three-Phase Design

**Main Menu Phase:**
- Title display
- New Game button -> Setup Phase
- Settings button -> Settings Panel overlay
- Exit button -> Quit application

**Setup Phase (Per-Player):**
- Horizontal 50/50 split layout (settings left, grid right)
- Dynamic cell sizing based on grid dimensions
- Word entry with validation and autocomplete
- Coordinate placement with visual feedback

**Gameplay Phase (Alternating Turns):**
- Two PlayerGridPanels visible (owner left, opponent right)
- Center area with guillotines and miss counters
- Click opponent's elements to make guesses

### Key UI Scripts

| Script | Purpose | Lines | Notes |
|--------|---------|-------|-------|
| MainMenuController.cs | Main menu navigation | ~130 | Added Dec 13 |
| SettingsPanel.cs | Audio settings with persistence | ~270 | Added Dec 13 |
| PlayerGridPanel.cs | Main grid container, mode switching | ~1,120 | Refactored Dec 12 (49% reduction) |
| SetupSettingsPanel.cs | Player config, difficulty, word lists | ~760 | Refactored Dec 5 |
| GameplayUIController.cs | Two-panel gameplay system | ~1,179 | Refactored Dec 12 (44% reduction) |
| SetupModeController.cs | Keyboard input routing | ~150 | |
| WordPatternRow.cs | Individual word entry rows | ~1,199 | Refactored Dec 12 (13% reduction) |
| LetterButton.cs | Letter tracker buttons with states | ~200 | |
| GridCellUI.cs | Individual grid cells, hidden letter support | ~250 | Three-color system Dec 11 |
| AutocompleteDropdown.cs | Word suggestion dropdown | ~450 | Added Dec 13 |
| AutocompleteItem.cs | Individual dropdown entry | ~140 | Added Dec 13 |

### Extracted Controllers/Services

| File | Purpose | Location |
|------|---------|----------|
| LetterTrackerController.cs | Letter button management | Controllers/ |
| GridColorManager.cs | Grid cell color state | Controllers/ |
| PlacementPreviewController.cs | Placement preview display | Controllers/ |
| WordPatternRowManager.cs | Word row management | Controllers/ |
| CoordinatePlacementController.cs | Coordinate placement mode | Controllers/ |
| GridLayoutManager.cs | Grid layout and sizing | Controllers/ |
| PlayerColorController.cs | Color picker management | Controllers/ |
| WordValidationService.cs | Word bank validation | Services/ |
| GuessProcessor.cs | Generic guess processing | Services/ |
| WordGuessModeController.cs | Word guess state machine | Controllers/ |
| WordGuessInputController.cs | Word guess input handling | Controllers/ |
| RowDisplayBuilder.cs | Display text building | Utilities/ |
| AutocompleteManager.cs | Word autocomplete logic | Controllers/ |

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

### Controller/Service Pattern

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

### Defensive Controller Initialization Pattern (Dec 12, 2025)

**CRITICAL for inactive GameObject configuration:**

When GameObjects are activated and immediately configured, `Start()` hasn't run yet. Controllers initialized in `Start()` are null when methods are called from external code.

**Solution:**
```csharp
private bool _eventsWired;

private void Start()
{
    InitializeControllers();
    WireControllerEvents();
}

private void InitializeControllers()
{
    if (_gridCellManager != null) return;  // Guard
    _gridCellManager = new GridCellManager();
    // ... more controllers
}

private void EnsureControllersInitialized()
{
    if (_gridCellManager != null) return;
    Debug.Log("[ClassName] EnsureControllersInitialized - initializing before Start()");
    // Same initialization as InitializeControllers()
    WireControllerEventsIfNeeded();
}

public void InitializeGrid(int gridSize)
{
    EnsureControllersInitialized();  // Safe to call before Start()
    // ... rest of method
}
```

### Service Pattern with Callback Injection (Dec 12, 2025)

For shared logic used by multiple callers:

```csharp
public class GuessProcessor
{
    private readonly Action _onMissIncrement;
    private readonly Action<char, LetterState> _setLetterState;
    private readonly Func<string, bool> _validateWord;
    
    public GuessProcessor(
        List<WordPlacementData> targetWords,
        PlayerGridPanel targetPanel,
        Action onMissIncrement,
        Action<char, LetterState> setLetterState,
        Func<string, bool> validateWord)
    {
        // Store dependencies
    }
}
```

### Permanent State Pattern

**For UI elements that should NEVER reappear after certain conditions:**

```csharp
private bool _wordSolved = false;

public void MarkWordSolved()
{
    _wordSolved = true;
    HideGuessWordButton();
}

public void ShowGuessWordButton()
{
    if (_wordSolved) return; // Never show if solved
}
```

---

## Asset Priority System

### Tier 1: Core Dependencies (6 Packages)

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

---

## Audio Settings

### Default Volume Settings

- **Sound Effects:** 50% (0.5f)
- **Music:** 50% (0.5f)
- Player can adjust both via Settings Panel
- Values persist via PlayerPrefs

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
- [X] Use EnsureControllersInitialized() pattern for inactive GameObjects
- [X] Sort word rows by sibling index, not GetComponentsInChildren order
- [X] Call CacheWordPatternRows() before accessing rows on newly activated panels
- [X] Use MarkWordSolved() for permanent button hiding after correct guesses
- [X] Use Keyboard.current (New Input System) instead of legacy Input.inputString
- [X] Use boolean flags for persistent UI state (not just Hide/Show)
- [X] Ask user to upload scripts or use MCP to verify before referencing methods/classes
- [X] Only use patterns if they add value (pragmatic architecture)

**Never:**
- [ ] Rush ahead with multiple steps
- [ ] Use incremental edits when providing code for manual copy/paste
- [ ] Use UTF-8 or special characters
- [ ] Assume old Unity knowledge is current
- [ ] Use deprecated APIs
- [ ] Create duplicate UI components when one can serve multiple purposes
- [ ] Display full "action: read" tool calls (causes lockup)
- [ ] Use MCP for hierarchy modifications (causes Unity lockup)
- [ ] Rely on simple hide/show calls for persistent state (use flags)
- [ ] Use legacy Input class (Input.inputString) - use New Input System
- [ ] Assume scripts, classes, or methods exist without verification (ask for upload or use MCP)
- [ ] Over-engineer with patterns that don't add value

---

## Questions Protocol

1. **Clarify the context** - What are you trying to accomplish?
2. **Look up current information** - Search Unity 6.3 docs
3. **Provide step-by-step solution** - One step at a time
4. **Explain the reasoning** - Why this approach?

---

**End of Project Instructions**

These instructions should be followed for every conversation in this project.

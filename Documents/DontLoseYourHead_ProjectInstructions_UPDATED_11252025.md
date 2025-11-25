# Don't Lose Your Head - Claude Project Instructions

**Project:** Don't Lose Your Head  
**Developer:** TecVooDoo LLC  
**Designer:** Rune (Stephen Brandon)  
**Unity Version:** 6.2 (2D Template)  
**Date Created:** November 20, 2025  
**Last Updated:** November 25, 2025  

---

## Core Development Philosophy

### Step-by-Step Verification Protocol

**CRITICAL: Never rush ahead with multiple steps**

- Provide ONE step at a time
- Wait for user confirmation via text OR screenshot before proceeding
- User will verify each step is complete before moving forward
- If a step fails, troubleshoot that specific step before continuing
- Assume nothing - verify everything

**Example Flow:**
```
Claude: "First, let's create a new folder: Assets/Scripts/Core"
User: [creates folder, sends screenshot]
Claude: "Perfect! I can see the folder. Next, let's create..."
```

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
- `ProjectInstructions_UPDATED_11252025.md`
- `GDD_UPDATED_11252025.md`
- `GameManager_UPDATED_11252025.cs`

**Rules:**
- Always use underscore separators
- Date format: MMDDYYYY (month/day/year)
- Use "UPDATED" for revised versions of existing files
- Keep original filename recognizable

### Code Editing Preference

**CRITICAL: User strongly prefers complete file replacements**

**When to use complete file replacement:**
- Multiple changes needed in a script
- Changes span more than a few lines
- Code blocks being added, removed, or restructured
- Any change that requires searching through the file

**When line-by-line edits are acceptable:**
- Single word change (with line number provided)
- One line of code change (with line number provided)
- Very small, isolated fix where line number is clear

**Why:** User finds it much faster to replace entire files than search for specific lines to modify. This prevents errors from mismatched line numbers or context confusion.

**Example:**
```
[X] GOOD: "Here's the complete updated GameManager.cs: [full file contents]"
[X] GOOD: "On line 42, change 'private' to 'public'"
[ ] BAD: "Find the ProcessLetterGuess method and add these 15 lines after the if statement..."
[ ] BAD: "Replace lines 42-67 with this code block..."
```

### Documentation Standards

**Always Use Current Documentation**

- [X] Look up Unity 6.2 documentation for every Unity API
- [X] Check asset documentation for current versions
- [X] Verify APIs are not deprecated before suggesting
- [X] Search for "Unity 6.2 [feature name]" when recommending built-in features
- [X] Check asset changelogs if behavior seems unexpected

**Never:**
- [ ] Rely on memory of older Unity versions
- [ ] Assume APIs work the same as Unity 5.x or 2020.x
- [ ] Suggest deprecated methods
- [ ] Reference outdated tutorials without verification

**When Uncertain:**
- State: "Let me look up the current Unity 6.2 documentation for [feature]"
- Use web search to verify current API
- Cite the documentation source

---

## Unity MCP Tool Guidelines

### Known Issues and Workarounds

**Script Reading:**
- `manage_script` with action `read` is deprecated/problematic
- Use `read_resource` with `unity://path/Assets/...` URI format instead
- If that fails, user may need to provide file contents directly

**Script Paths:**
- Always include `Assets/` prefix in paths
- Full example: `Assets/DLYH/Scripts/Core/GameManager.cs`

**Parameter Types:**
- Some tools require string parameters even for numbers
- When in doubt, pass numbers as strings: `"10"` instead of `10`

**Script Creation/Updates:**
- `create_script` works for new files
- Cannot overwrite existing scripts directly
- For updates: provide complete file contents for user to replace manually
- Or use `delete_script` followed by `create_script` (risky - verify first)

**Console Reading:**
- `read_console` without type filters provides more complete output
- Compilation errors appear immediately
- Warnings may be delayed

---

## Current Project Status

### Completed Systems [X]

**Core Foundation:**
- [X] Folder structure (Assets/DLYH/)
- [X] Difficulty system with enums and calculator
  - GridSizeOption (6x6, 8x8, 10x10)
  - WordCountOption (3 or 4 words)
  - ForgivenessSetting (Strict/Normal/Forgiving)
  - Dynamic miss limit calculation
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
- [X] Word bank integration complete
- [X] WordListSO ScriptableObject for word collections
- [X] WordBankImporter editor tool (DLYH > Tools menu)
- [X] 3-letter words: 2,130 words
- [X] 4-letter words: 7,186 words
- [X] 5-letter words: 15,921 words
- [X] 6-letter words: Support added for 4-word configurations
- [X] Total: 25,000+ filtered words from dwyl/english-words

**Testing:**
- [X] TestController with comprehensive test scenarios
- [X] All core mechanics verified working
- [X] Letter reveal verified (coordinate guess -> * -> letter guess -> actual letter)
- [X] Debug grid visualization with formatted output

### Next Development Priorities

**Immediate:**
1. **UI implementation** (grids, input controls, feedback displays)
   - Follow UI concept PDF for setup mode
   - Autocomplete word entry
   - Color-coded grid placement validation

**Soon:**
2. AI opponent system
3. Visual polish (animations, effects)
4. Audio implementation

**Later:**
5. Playtesting and balancing
6. Art replacement (move from placeholder to final)
7. Mobile optimization

### Future Refactoring Note

**Goal:** Create generic, reusable code systems for future TecVooDoo projects

**Not focusing on this now** - priority is completing a working prototype. However, once DLYH is complete, consider refactoring core systems (Grid, Turn Management, Player System, State Machine) to use generics and abstraction for easy reuse in:
- Shrunken Head Toss (next project)
- A Quokka Story (future project)
- Other TecVooDoo games

---

## Architecture Principles

### SOLID Principles (Mandatory)

**S - Single Responsibility Principle**
- Each class has one reason to change
- Separate concerns (data, logic, presentation)
- Example: GridManager handles grid operations, not UI rendering

**O - Open/Closed Principle**
- Open for extension, closed for modification
- Use inheritance and interfaces appropriately
- ScriptableObjects for data-driven design

**L - Liskov Substitution Principle**
- Derived classes must be substitutable for base classes
- Proper interface implementation
- No breaking base class contracts

**I - Interface Segregation Principle**
- Many specific interfaces > one general interface
- Clients shouldn't depend on methods they don't use
- Example: IGuessable, IPlaceable rather than IGameEntity

**D - Dependency Inversion Principle**
- Depend on abstractions, not concretions
- Use dependency injection
- ScriptableObjects as injected dependencies

### ScriptableObject Architecture (Heavy Use)

**Primary Pattern:**
- Game data as ScriptableObjects
- Game events via ScriptableObject events
- Runtime sets for object tracking
- Variables as ScriptableObjects

**Key Uses:**
- Difficulty settings (GridSize, MissLimit, etc.)
- Word lists
- Game state
- Player data
- UI configuration
- Event channels

**Benefits:**
- Designer-friendly
- Decoupled systems
- Easy testing
- Persistent between scenes
- Inspector-editable

---

## Asset Priority System

### Tier 1: Core Dependencies (Always Loaded)

**1. Odin Inspector & Validator (v4.0.1.0)**
- **Use for:** All data structures, custom editors, inspector enhancement
- **Key Features:** Enhanced serialization, validation rules, custom drawers
- **When to use:** Any time creating data structures or custom editors

**2. DOTween Pro (v1.0.380)**
- **Use for:** ALL animations and tweening
- **Key Features:** Transform animations, UI animations, sequences, ease curves
- **When to use:** Any animation, UI transitions, visual feedback
- **Never use:** Unity's legacy Animation system for simple tweens

**3. Easy Popup System (v1.0)**
- **Use for:** Popups, toasts, dialogs, confirmations
- **Key Features:** One-line API (EasyPopup.Create(), EasyToast.Create())
- **When to use:** Hit/miss notifications, turn announcements, game over dialogs

**4. SOAP - ScriptableObject Architecture Pattern (v3.6.1)**
- **Use for:** Advanced ScriptableObject patterns
- **Key Features:** SO events, SO variables, runtime sets
- **When to use:** Implementing ScriptableObject-based architecture

### Tier 2: Development & Data Tools

**5. Scriptable Sheets (v1.8.0)**
- **Use for:** Google Sheets integration (future)
- **Status:** Available but not yet implemented

**6. MCP for Unity (v7.0.0)**
- **Use for:** Claude integration during development
- **Status:** Development tool only

### Tier 3: Deferred to Polish Phase

**UI Toolkit Filters, Effects & Shaders**
- Requires Unity 6.3+ (currently on 6.2)
- Defer until Unity upgrade and polish phase

**GUI Art Packs (pick ONE during polish):**
- GUI Pro - Fantasy RPG
- Classic RPG GUI
- Cartoon GUI Pack

### Tier 4: Not Needed for MVP

**Skipped Assets:**
- Feel (DOTween covers animation needs)
- UI Assistant (overlaps with DOTween + Easy Popup)
- UGUI Super ScrollView (scale doesn't require virtualization)
- Flexalon (Unity built-in layouts sufficient)

### Unity Built-ins

**TextMeshPro**
- Use for all text rendering
- Preferred over legacy Text component

**Unity UI (UGUI)**
- Canvas, Image, Button
- Standard UI components

**New Input System**
- Use for all input handling
- Prefer Input Actions over old Input Manager

---

## Code Style Guidelines

### Naming Conventions

**Classes:**
```csharp
public class GridManager { }
public class WordValidator { }
```

**Interfaces:**
```csharp
public interface IGuessable { }
public interface IPlaceable { }
```

**ScriptableObjects:**
```csharp
[CreateAssetMenu(fileName = "NewDifficulty", menuName = "Game/Difficulty")]
public class DifficultySO : ScriptableObject { }
```

**Private Fields:**
```csharp
private int _missCount;
private GridCell[,] _cells;
```

**Public Properties:**
```csharp
public int MissCount { get; private set; }
public GridSize CurrentSize => _currentDifficulty.GridSize;
```

**Methods:**
```csharp
public void GuessLetter(char letter) { }
private bool ValidatePlacement() { }
```

### File Organization

```
Assets/
  DLYH/
    Art/
      Sprites/
      Materials/
      Prefabs/
    Audio/
      Music/
      SFX/
    ScriptableObjects/
      Difficulty/
      Events/
      Variables/
      Words/
      Players/
    Scripts/
      Core/
        Grid/
        Words/
        GameState/
      UI/
      AI/
      Utilities/
      Testing/
    Scenes/
      MainMenu.unity
      GameScene.unity
```

---

## Testing Strategy

### Manual Testing Checklist

```markdown
- [X] Word placement works in all orientations
- [X] Overlapping words share letters correctly
- [X] Letter guessing reveals correctly
- [X] Coordinate guesses show * correctly
- [X] Previously guessed letters update * to letter
- [X] Word guesses update * to letters
- [X] Wrong word guesses count as 2 misses
- [X] Miss counter increments correctly
- [ ] Guillotine animates on each miss
- [X] Win condition triggers correctly
- [X] Lose condition triggers correctly
- [ ] Game can be restarted
- [ ] Settings save/load correctly
```

---

## Data Resources

### Word Bank

**Source:** dwyl/english-words GitHub repository
- **File:** words_alpha.txt
- **License:** MIT (free to use)
- **Content:** ~479,000 English words (filtered to 25,000+)
- **URL:** https://github.com/dwyl/english-words

**Generated Word Lists:**
- 3-letter: 2,130 words
- 4-letter: 7,186 words
- 5-letter: 15,921 words
- 6-letter: Available for 4-word configurations

---

## Key Reminders

**Always:**
- [X] Wait for user verification before proceeding
- [X] Provide COMPLETE file replacements for multi-line changes
- [X] Use ASCII encoding only (no UTF-8)
- [X] Look up current Unity 6.2 documentation
- [X] Use Odin Inspector for data structures
- [X] Use DOTween Pro for animations
- [X] Follow SOLID principles
- [X] Use ScriptableObject architecture
- [X] Check asset capabilities before custom code

**Never:**
- [ ] Rush ahead with multiple steps
- [ ] Use incremental edits for code blocks (single line with line number is OK)
- [ ] Use UTF-8 or special characters in code
- [ ] Assume old Unity knowledge is current
- [ ] Use deprecated APIs
- [ ] Skip verification steps
- [ ] Violate SOLID principles
- [ ] Write custom code when asset solution exists
- [ ] Use Unity Animator for simple tweens (use DOTween)
- [ ] Use legacy Text (use TextMeshPro)

---

## Questions Protocol

**When User Asks a Question:**

1. **Clarify the context**
   - What are you trying to accomplish?
   - What step are you on?
   - What have you tried?

2. **Look up current information**
   - Search Unity 6.2 docs
   - Check asset documentation
   - Verify API status

3. **Provide step-by-step solution**
   - One step at a time
   - Wait for verification
   - Adjust based on results

4. **Explain the reasoning**
   - Why this approach?
   - How does it fit SOLID?
   - How does it use ScriptableObjects?

---

**End of Project Instructions**

These instructions should be followed for every conversation in this project. They ensure consistent, high-quality development that respects the user's learning pace and coding preferences, and produces maintainable, professional code.

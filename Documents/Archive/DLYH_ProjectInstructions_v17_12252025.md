# Don't Lose Your Head - Claude Project Instructions

**Project:** Don't Lose Your Head
**Developer:** TecVooDoo LLC
**Designer:** Rune (Stephen Brandon)
**Unity Version:** 6.3 (2D Template)
**Project Path:** E:\Unity\DontLoseYourHead
**Document Version:** 17
**Last Updated:** December 25, 2025

---

## IMPORTANT: Project Path

**The Unity project is located at: `E:\Unity\DontLoseYourHead`**

Do NOT use worktree paths like `C:\Users\steph\.claude-worktrees\...` - always use the E: drive path above for all file operations.

---

## Critical Development Protocols

### NEVER Assume Names Exist

**CRITICAL: Verify before referencing**

- NEVER assume file names, method names, or class names exist
- ALWAYS read/search for the actual names in the codebase first
- If a script might be named "PlayerController" or "PlayerManager", search to find the actual name
- Incorrect assumptions waste context and force complete rewrites

### Step-by-Step Verification Protocol

**CRITICAL: Never rush ahead with multiple steps**

- Provide ONE step at a time
- Wait for user confirmation via text OR screenshot before proceeding
- User will verify each step is complete before moving forward
- If a step fails, troubleshoot that specific step before continuing
- Assume nothing - verify everything

**Why this matters:** Jumping ahead when errors occur forces entire scripts to be redone, wasting context and causing chats to run out of room.

### Documentation Research Protocol

**CRITICAL: Use current documentation**

- ALWAYS fetch the most up-to-date documentation for Unity and packages before making recommendations
- User prefers waiting for accurate information over redoing work later
- Do not rely on potentially outdated knowledge - verify current APIs and patterns
- This applies to Unity 6.3, DOTween, Feel, and all installed packages

---

## File Conventions

### Encoding Rule

**CRITICAL: ASCII Only**

- All scripts and text files MUST use ASCII encoding
- Do NOT use UTF-8 or other encodings
- Avoid special characters, smart quotes, em-dashes
- Use standard apostrophes (') not curly quotes
- Use regular hyphens (-) not em-dashes

### Core Document Naming Convention

**Format:** `DLYH_DocumentName_v#_MMDDYYYY.md`

**Rules:**
- All four core documents share the SAME version number
- Increment version for ALL documents when ANY document is updated
- If a document has no changes, update the filename only (no content changes needed)
- Move old versions to `Documents/Archive/` folder

**Core Documents:**
- `DLYH_ProjectInstructions_v#_MMDDYYYY.md`
- `DLYH_GDD_v#_MMDDYYYY.md`
- `DLYH_Architecture_v#_MMDDYYYY.md`
- `DLYH_DesignDecisions_v#_MMDDYYYY.md`

**Example version bump:**
```
v17 -> v18 (all four files)
Old files moved to Documents/Archive/
```

---

## Code Editing Preferences

### Manual Copy/Paste (providing code to user)

- Provide COMPLETE file replacements
- Easier than hunting for specific lines
- Reduces errors from partial edits

### MCP Direct Edits (Claude editing via tools)

- Use `script_apply_edits` for method-level changes
- Use `apply_text_edits` for precise line/column edits
- Use `validate_script` to check for errors
- Direct MCP edits are preferred when connection is stable

---

## Unity MCP Tools Reference

### Script Operations

| Tool | Purpose |
|------|---------|
| `manage_script` (read) | Read script contents |
| `get_sha` | Get SHA256 hash + file size |
| `validate_script` | Check syntax/structure |
| `create_script` | Create new C# scripts |
| `delete_script` | Remove scripts |

### Script Editing (Preferred)

**`script_apply_edits`** - Structured C# edits:
- `replace_method` - Replace entire method body
- `insert_method` - Add new method
- `delete_method` - Remove a method
- `anchor_insert/delete/replace` - Pattern-based edits

**`apply_text_edits`** - Precise text edits:
- Line/column coordinate-based changes
- Atomic multi-edit batches

### Other Operations

| Tool | Purpose |
|------|---------|
| `manage_asset` | Import, create, modify, delete assets |
| `manage_scene` | Load, save, create scenes |
| `manage_gameobject` | Create, modify, find GameObjects |
| `manage_prefabs` | Open/close prefab stage |
| `manage_editor` | Play/pause/stop, tags, layers |
| `read_console` | Get Unity console messages |
| `run_tests` | Execute EditMode or PlayMode tests |

### MCP Best Practices

1. **Read before editing** - View the file before making changes
2. **Validate after edits** - Run `validate_script` to catch syntax errors
3. **Use structured edits** - Prefer `script_apply_edits` for methods
4. **Check console** - Use `read_console` after changes
5. **Avoid verbose output** - Don't display full read tool calls (causes lockup)
6. **Avoid hierarchy modifications** - MCP hierarchy changes can cause Unity lockups

---

## Coding Standards

### Required Frameworks

- **DOTween Pro** - For animations and juice
- **Feel** - For MMFeedbacks, impacts, screen shake
- **New Input System** - Use `Keyboard.current` from InputSystem

### Code Style

**CRITICAL: No `var` keyword**

- Always use explicit type declarations
- Clear, readable code wins over clever code
- PascalCase for public, camelCase for private
- Avoid God classes - keep classes focused

### File Size Limits

**CRITICAL: 800 lines maximum per script**

- When a script approaches 800 lines, it is time to refactor
- Extract logic into separate classes, services, or controllers
- Do not wait until scripts are unmanageable

### Architecture Patterns

**Controller Extraction Pattern:**
- Large MonoBehaviours should delegate to plain C# controller classes
- Controllers receive dependencies via constructor
- Keeps MonoBehaviours small and focused

**Callback Injection Pattern:**
- Services receive Actions/Funcs for operations they need but don't own

**Event-Driven Communication:**
- Controllers publish events; parents subscribe
- No tight coupling between systems

### SOLID Principles

**S - Single Responsibility:**
- One reason to change per class
- Extract controllers/services early to avoid large MonoBehaviours

**O - Open/Closed:**
- Open for extension, closed for modification
- Use interfaces and abstract classes

**L - Liskov Substitution:**
- Subtypes must be substitutable for base types

**I - Interface Segregation:**
- Many specific interfaces over one general interface

**D - Dependency Inversion:**
- Depend on abstractions, not concretions

### Event Patterns

- Set state BEFORE firing events that handlers may check
- Initialize UI to known states (explicit Hide/Show calls)
- Guard against event re-triggering during batch operations

### Performance Rules

**In Gameplay Loops (Update, FixedUpdate):**
- NO allocations (`new`, string concatenation, LINQ)
- Cache all component references in Awake/Start
- Use object pooling for frequently created/destroyed objects

---

## Telemetry System

### Cloudflare Worker
- **Endpoint:** `https://dlyh-telemetry.runeduvall.workers.dev`
- **Database:** D1 (dlyh-telemetry)
- **Tables:** events, sessions

### Available Endpoints
| Endpoint | Method | Purpose |
|----------|--------|---------|
| `/event` | POST | Receive telemetry events |
| `/events` | GET | View last 100 events |
| `/summary` | GET | Event type counts |
| `/feedback` | GET | Player feedback entries |
| `/stats` | GET | Aggregated game statistics |

### Events Tracked
- `session_start` - Platform, version, screen size
- `session_end` - Automatic on quit
- `game_start` - Player name, grid sizes, word counts, difficulties
- `game_end` - Win/loss, misses, total turns
- `game_abandon` - Phase (gameplay/quit), turn number
- `player_guess` - Guess type, hit/miss, value
- `player_feedback` - Feedback text, win/loss context
- `error` - Unity errors/exceptions

### Editor Dashboard
- **Menu:** DLYH > Telemetry Dashboard
- **Tabs:** Summary, Game Stats, Recent Events, Feedback
- **Features:**
  - Event type breakdown with visual bars
  - Player name leaderboard
  - Win rate and completion rate stats
  - Difficulty distribution
  - Export to CSV button

---

## Event System Rules

### Event Handling Safety

**ALWAYS:** Set state values BEFORE firing events.

**Example:**
```csharp
// CORRECT
_isWordGuessMode = false;  // State set FIRST
OnWordGuessModeExited?.Invoke();  // Event fired AFTER

// WRONG - Can cause inconsistent state
OnWordGuessModeExited?.Invoke();  // Event fired while state is wrong
_isWordGuessMode = false;  // State changed too late
```

---

## Layout Constraints

### Use Layout Builders
- **GridLayoutGroup** for uniform grids
- **VerticalLayoutGroup** for lists/menus
- **ContentSizeFitter** for dynamic sizing
- **LayoutElement** for minimum/preferred sizes

### Golden Rules
1. Never mix absolute and relative positioning in same container
2. Use anchors for responsive positioning
3. Test on multiple resolutions

### CRITICAL: Protect Scene Files

**IMPORTANT:** Unity scene files (.unity) contain layout data that can be accidentally modified when selecting objects in the Inspector. If grid cell sizing or other layout issues appear unexpectedly after code changes:

1. Check if the scene file was modified: `git diff Assets/DLYH/Scenes/GuillotineTesting.unity`
2. If layout values changed, revert the scene: `git checkout HEAD -- "Assets/DLYH/Scenes/GuillotineTesting.unity"`
3. Scene changes should only be committed intentionally, not as side effects of code work

---

## Project Documents

| Document | Purpose |
|----------|---------|
| DLYH_GDD | Game design and mechanics |
| DLYH_Architecture | Script catalog, packages, code structure |
| DLYH_DesignDecisions | History, lessons learned, version tracking |
| DLYH_ProjectInstructions | Development protocols (this document) |

**Note:** User will ask Claude to review core docs at the start of each chat. Read and follow these instructions carefully.

---

## Development Status

### Completed Phases
- Phase 1: Core Mechanics - COMPLETE
- Phase 2: UI Implementation - COMPLETE
- Phase 3: AI Opponent - COMPLETE

### Phase 4: Polish and Features - IN PROGRESS

| Item | Status |
|------|--------|
| Sound effects (UI audio system) | COMPLETE |
| Profanity filtering | COMPLETE |
| Drug word filtering | COMPLETE |
| Help overlay / Tutorial | COMPLETE |
| Feedback panel | COMPLETE |
| Playtest telemetry | COMPLETE |
| Tooltip system | COMPLETE |
| Fix layout (ButtonBar, guillotine assembly) | COMPLETE |
| Guillotine animations (raise on miss, drop on lose) | COMPLETE |
| GuillotineDisplay script (blade/head animations) | COMPLETE |
| Guillotine audio (blade raise, chop, head fall) | COMPLETE |
| Extra turn system (word completion rewards) | COMPLETE |
| Background music (MusicManager with shuffle/crossfade) | COMPLETE |
| Mute buttons during gameplay (SFX and Music) | COMPLETE |
| Telemetry Dashboard (Editor window) | COMPLETE |
| Enhanced telemetry (player name, game abandon) | COMPLETE |
| MessagePopup positioning (bottom of screen) | COMPLETE |
| Main menu trivia display | COMPLETE |
| Head face expressions | COMPLETE |
| Final execution audio sequence (3-part) | COMPLETE |
| Board reset on new game | COMPLETE |
| Settings button during gameplay | COMPLETE |
| Version display on main menu | COMPLETE |
| DOTween animations (reveals, transitions, feedback) | TODO |
| Feel effects (screen shake, juice) | TODO |
| Win/Loss tracker vs AI (session stats) | TODO |
| Medieval/carnival themed monospace font | TODO |
| UI skinning (medieval carnival theme) | TODO |
| Character avatars | TODO |
| Background art | TODO |

### Phase 5: Multiplayer and Mobile - TODO

| Item | Status |
|------|--------|
| Word row cell-based display (replace text fields with individual letter cells) | TODO |
| 2-player networking mode | TODO |
| Mobile implementation | TODO |

---

## Quick Reference Checklist

**Always:**
- [ ] Verify file/method/class names exist before referencing
- [ ] Wait for user verification before proceeding
- [ ] Use ASCII encoding only
- [ ] Use explicit types (no `var` keyword)
- [ ] Use New Input System (Keyboard.current)
- [ ] Validate scripts after MCP edits
- [ ] Set state BEFORE firing events
- [ ] Follow SOLID principles
- [ ] Fetch current documentation before recommendations
- [ ] Profile and test regularly

**Never:**
- [ ] Assume names exist without checking
- [ ] Rush ahead with multiple steps
- [ ] Use UTF-8 or special characters
- [ ] Use `var` keyword
- [ ] Use legacy Input class
- [ ] Display full read tool calls
- [ ] Use MCP for hierarchy modifications
- [ ] Let scripts grow past 800 lines without refactoring
- [ ] Make recommendations based on potentially outdated docs
- [ ] Create allocations in Update loops

---

## Known Issues / TODO for Next Session

- (None currently)

## Recent Changes (v16-v17)

### Drug Word Filtering

Added banned words filter in WordBankImporter.cs to prevent drug-related words from appearing in gameplay:

**Banned Words:** heroin, cocaine, meth, crack, weed, opium, morphine, ecstasy, molly, dope, smack, coke

Implementation:
- HashSet in `WordBankImporter.cs` with case-insensitive matching
- `FilterWordsByLength()` skips any word in the banned list
- "heroin" also removed directly from `words_alpha.txt`

### Board Reset on New Game

Game state now properly resets when starting a new game after completing one:

**GameplayUIController.ResetGameplayState():**
- Clears player 1 and player 2 guessed word lists
- Resets all letter tracker buttons (both panels)
- Resets both guillotines (blade position, head position, face)
- Clears pending game result

**SetupSettingsPanel.ResetForNewGame():**
- Clears placed words on player grid
- Resets word pattern rows to empty
- Reinitializes grid to selected size
- Resets player name input
- Updates all button states

**GuillotineDisplay.Reset():**
- Restores blade to starting position
- Restores head to original position (stored on Initialize)
- Resets face expression via HeadFaceController.ResetFace()

### Settings Button During Gameplay

Players can now access Settings from the gameplay screen:

**GameplaySettingsButton.cs:**
- Simple button component that calls `SettingsPanel.ShowFromGameplay()`
- Add to any button in the gameplay ButtonBarStrip

**SettingsPanel Contextual Behavior:**
- `ShowFromGameplay()` - Opens settings, hides main menu elements, sets gameplay flag
- `ShowFromMenu()` - Standard menu behavior (called by MainMenuController)
- Back button returns to correct screen based on where Settings was opened

### Version Display on Main Menu

**VersionDisplay.cs:**
- Displays Application.version on main menu
- Configurable format string (default: "v{0}")
- Attach to TextMeshProUGUI component

**Unity Setup:**
1. Create TextMeshProUGUI in MainMenuContainer
2. Add VersionDisplay component
3. Set Project Settings > Player > Version to current version

### Document Naming Convention Standardization (v17)

All core documents now follow consistent naming:
- `DLYH_` prefix for all documents
- No `_UPDATED_` suffix
- All 4 documents share same version number
- Old versions archived

---

## Lessons Learned from Previous Projects

1. **Extract controllers early** - Don't let MonoBehaviours grow large
2. **Event timing matters** - Update state BEFORE firing events
3. **Initialize UI explicitly** - Don't rely on default states
4. **Validate positions** - Check before UI placement
5. **Guard batch operations** - Hide/reset UI before AND after
6. **Use boolean flags** - For state that persists across show/hide
7. **Test edge cases early** - Don't wait for playtest to find bugs
8. **Scene file protection** - Check git diff before committing scene changes

---

## Reminders for Future Implementation

- Random eye blink on severed head (future polish)

---

**End of Project Instructions**

These instructions should be followed for every conversation in this project. User will ask Claude to review these docs at the start of each chat session.

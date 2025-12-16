# Don't Lose Your Head - Claude Project Instructions

**Project:** Don't Lose Your Head
**Developer:** TecVooDoo LLC
**Designer:** Rune (Stephen Brandon)
**Unity Version:** 6.3 (2D Template)
**Date Created:** November 20, 2025
**Last Updated:** December 14, 2025

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
- `ProjectInstructions_v5_UPDATED_12142025.md`
- `GDD_v5_UPDATED_12142025.md`
- `DESIGN_DECISIONS_v5_UPDATED_12142025.md`
- `DLYH_Architecture_v5_UPDATED_12142025.md`

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
- **Transport:** Stdio (required for Claude Desktop)
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

### MCP Best Practices

1. **Read before editing:** Use `manage_script` read or view the file before making changes
2. **Validate after edits:** Run `validate_script` to catch syntax errors immediately
3. **Use structured edits:** Prefer `script_apply_edits` over raw text edits for methods
4. **Check console:** Use `read_console` after changes to catch runtime issues
5. **SHA256 preconditions:** For critical edits, use `get_sha` to ensure file hasn't changed
6. **Avoid verbose output:** Don't display full tool call details for read operations (causes UI lockup)
7. **Avoid hierarchy modifications:** MCP hierarchy changes can cause Unity lockups - use script edits instead

---

## Current Project Status

### Completed Systems [X]

**Core Foundation:**
- [X] Folder structure (Assets/DLYH/)
- [X] Difficulty system with enums and calculator
- [X] Grid system (Grid, GridCell, Word classes)
- [X] Word placement validation and placement logic
- [X] ScriptableObject event system

**Game Systems:**
- [X] GameManager with letter and coordinate guessing
- [X] Turn Management System
- [X] Player System
- [X] Game Flow State Machine
- [X] Win/Lose condition checking
- [X] Word guessing with 2-miss penalty

**Word Bank:**
- [X] WordListSO ScriptableObject for word collections
- [X] 3-letter words: 2,130 words
- [X] 4-letter words: 7,186 words
- [X] 5-letter words: 15,921 words
- [X] 6-letter words: Available

**UI Foundation:**
- [X] PlayerGridPanel prefab (scalable 6x6 to 12x12)
- [X] GridCellUI prefab with hidden letter support
- [X] LetterButton prefab with state management
- [X] AutocompleteDropdown prefab
- [X] Canvas setup (Scale With Screen Size, 1080x1920)

**Setup Mode UI - COMPLETE:**
- [X] SetupSettingsPanel with all controls
- [X] 8-button color picker
- [X] Grid size dropdown (6x6 through 12x12)
- [X] Word count dropdown (3 or 4)
- [X] Difficulty dropdown (Easy/Normal/Hard)
- [X] Word entry and validation
- [X] Coordinate placement mode
- [X] Pick Random Words / Place Random Positions buttons

**Gameplay Mode UI - COMPLETE:**
- [X] GameplayUIController with two-panel system
- [X] Letter guessing, coordinate guessing, word guessing
- [X] Three-color grid cell system
- [X] Solved word row tracking
- [X] Guessed word lists under guillotines

**Phase 3: AI Opponent - COMPLETE (Dec 13-14, 2025):**
- [X] All 11 AI scripts implemented
- [X] AI integration with GameplayUIController
- [X] Rubber-banding system
- [X] AI grid/word variety by difficulty
- [X] Multiple playtest bug fixes

### Next Development Priorities

**Phase 4: Polish and Features**
1. Visual polish (DOTween animations, Feel effects)
2. Audio implementation
3. Invalid word feedback UI (toast/popup)
4. Profanity filtering
5. Medieval/carnival themed monospace font

**Phase 5: Multiplayer and Mobile**
6. 2-player networking mode
7. Mobile implementation

---

## Project Documents

| Document | Purpose | Current Version |
|----------|---------|-----------------|
| DontLoseYourHead_GDD | Game design, mechanics, phases | v4.0 |
| DontLoseYourHead_ProjectInstructions | Development protocols, MCP tools | v4.0 |
| DESIGN_DECISIONS | Technical decisions, lessons learned | v4.0 |
| DLYH_Architecture | Script catalog, data flow, patterns | v4.0 |

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
- [X] Validate scripts after MCP edits
- [X] Extract controllers/services following established patterns
- [X] Use EnsureControllersInitialized() pattern for inactive GameObjects
- [X] Use MarkWordSolved() for permanent button hiding
- [X] Use Keyboard.current (New Input System)
- [X] Use boolean flags for persistent UI state
- [X] Reference Architecture document for script catalog and patterns
- [X] Set state BEFORE firing events that handlers may check
- [X] Initialize UI to known states (explicit Hide/Show calls)
- [X] Validate positions before UI placement
- [X] Guard against event re-triggering during batch operations

**Never:**
- [ ] Rush ahead with multiple steps
- [ ] Use incremental edits when providing code for manual copy/paste
- [ ] Use UTF-8 or special characters
- [ ] Assume old Unity knowledge is current
- [ ] Use deprecated APIs
- [ ] Display full "action: read" tool calls (causes lockup)
- [ ] Use MCP for hierarchy modifications (causes Unity lockup)
- [ ] Rely on simple hide/show calls for persistent state (use flags)
- [ ] Use legacy Input class (Input.inputString)
- [ ] Set state after firing events that depend on that state

---

**End of Project Instructions**

These instructions should be followed for every conversation in this project.

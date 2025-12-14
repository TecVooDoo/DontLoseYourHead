# Don't Lose Your Head - Claude Project Instructions

**Project:** Don't Lose Your Head
**Developer:** TecVooDoo LLC
**Designer:** Rune (Stephen Brandon)
**Unity Version:** 6.3 (2D Template)
**Last Updated:** December 14, 2025

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

---

## File Conventions

### Encoding Rule

**CRITICAL: ASCII Only**

- All scripts and text files MUST use ASCII encoding
- Do NOT use UTF-8 or other encodings
- Avoid special characters, smart quotes, em-dashes
- Use standard apostrophes (') not curly quotes
- Use regular hyphens (-) not em-dashes

### File Naming Convention

**For updated documentation:**

Format: `OriginalFileName_v#_UPDATED_MMDDYYYY.ext`

Examples:
- `ProjectInstructions_v6_UPDATED_12142025.md`
- `GDD_v6_UPDATED_12142025.md`
- `DLYH_Architecture_v6_UPDATED_12142025.md`

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

- **Odin Inspector** - For data structures and Inspector UI
- **DOTween Pro** - For animations
- **New Input System** - Use `Keyboard.current`, not legacy `Input.inputString`

### Architecture Patterns

- Follow SOLID principles
- Use ScriptableObject architecture for data
- Extract controllers/services from large MonoBehaviours
- Use `EnsureControllersInitialized()` pattern for inactive GameObjects
- Use boolean flags for persistent UI state

### Event Patterns

- Set state BEFORE firing events that handlers may check
- Initialize UI to known states (explicit Hide/Show calls)
- Validate positions before UI placement
- Guard against event re-triggering during batch operations

---

## Project Documents

| Document | Purpose |
|----------|---------|
| DontLoseYourHead_GDD | Game design and mechanics |
| DLYH_Architecture | Script catalog and code structure |
| DESIGN_DECISIONS | History, lessons learned, version tracking |
| DontLoseYourHead_ProjectInstructions | Development protocols (this document) |

---

## Quick Reference Checklist

**Always:**
- [ ] Verify file/method/class names exist before referencing
- [ ] Wait for user verification before proceeding
- [ ] Use ASCII encoding only
- [ ] Use New Input System (Keyboard.current)
- [ ] Validate scripts after MCP edits
- [ ] Set state BEFORE firing events

**Never:**
- [ ] Assume names exist without checking
- [ ] Rush ahead with multiple steps
- [ ] Use UTF-8 or special characters
- [ ] Use legacy Input class
- [ ] Display full read tool calls
- [ ] Use MCP for hierarchy modifications

---

**End of Project Instructions**

These instructions should be followed for every conversation in this project.

# Don't Lose Your Head - Claude Project Instructions

**Project:** Don't Lose Your Head
**Developer:** TecVooDoo LLC
**Designer:** Rune (Stephen Brandon)
**Unity Version:** 6.3 (2D Template)
**Project Path:** E:\Unity\DontLoseYourHead
**Last Updated:** December 18, 2025

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

**IMPORTANT:** Increment the version number (v#) for EVERY update, even if updating multiple times on the same day. This prevents files from being overwritten.

Examples:
- `ProjectInstructions_v12_UPDATED_12182025.md`
- `ProjectInstructions_v13_UPDATED_12182025.md` (second update same day)
- `GDD_v12_UPDATED_12182025.md`
- `DLYH_Architecture_v12_UPDATED_12182025.md`

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

## Cloudflare Infrastructure

### Telemetry Worker (COMPLETE)
- **Endpoint:** `dlyh-telemetry.runeduvall.workers.dev`
- **Database:** D1 (dlyh-telemetry)
- **Tables:** events, sessions
- **Status:** Deployed and operational

### WebGL Playtest Hosting (COMPLETE)
- **URL:** `dlyh.pages.dev`
- **Platform:** Cloudflare Pages
- **Build 1:** Dec 16, 2025 - Initial playtest
- **Build 2:** Dec 18, 2025 - Added background music, mute buttons [NEW v12]

### Deployment Steps (WebGL)
1. Unity: File > Build Settings > WebGL > Build
2. Choose output folder (e.g., `Builds/WebGL`)
3. Open Windows Terminal, PowerShell, or Command Prompt
4. Run: `npx wrangler pages deploy E:\Unity\DontLoseYourHead\Builds\WebGL --project-name=dlyh`
5. Access at: `dlyh.pages.dev`

Note: May need `npx wrangler login` first if not authenticated.

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
| Help overlay / Tutorial | COMPLETE |
| Feedback panel | COMPLETE |
| Playtest telemetry | COMPLETE |
| Tooltip system | COMPLETE |
| Fix layout (ButtonBar, guillotine assembly) | COMPLETE |
| Guillotine animations (raise on miss, drop on lose) | COMPLETE |
| GuillotineDisplay script (blade/head animations) | COMPLETE |
| Guillotine audio (blade raise, chop, head fall) | COMPLETE |
| Extra turn system (word completion rewards) | COMPLETE |
| Background music (MusicManager with shuffle/crossfade) | COMPLETE [NEW v12] |
| Mute buttons during gameplay (SFX and Music) | COMPLETE [NEW v12] |
| Head face expressions | IN PROGRESS (sprites need assignment) |
| Dynamic music tempo (increase near miss limit) | COMPLETE (built into MusicManager) [NEW v12] |
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

## Known Issues / TODO for Next Session

- Extend game over popup duration (2-3 more seconds)
- Gameplay Guide scroll speed is too slow
- **HeadFaceController needs Unity setup:** Add Face Image child to head, assign sprites

## Current Work in Progress

### HeadFaceController Setup (Incomplete)

The `HeadFaceController.cs` script is created and integrated with `GuillotineDisplay.cs`, but needs Unity Inspector setup:

1. **Create Face Image:**
   - Add a child Image to each guillotine head
   - Position/size to overlay the head properly

2. **Add HeadFaceController component:**
   - Either on the head or as a sibling

3. **Assign references:**
   - `_faceImage` slot: The Image that will display face sprites
   - Face sprite slots: Happy, Smug, Evil Smile, Neutral, Concerned, Worried, Scared, Terror, Horror, Victory

4. **Reference in GuillotineDisplay:**
   - Drag HeadFaceController into `_faceController` slot
   - Set `_faceLooksLeft` (true for Player 1, false for Player 2)

### MusicManager Setup [NEW v12]

1. **Create MusicManager GameObject** in scene
2. **Add MusicManager component**
3. **Assign tracks to `_tracks` array:**
   - A tavern, a bard, a quest.wav
   - An Ocean of Stars - Mastered.wav
   - The Monster Within - Mastered.wav

### Face Sprites Available

Located at: `Assets/DLYH/DLYHGraphicAssets/HeadFaces/`
- 16 face images at 158x158 pixels
- Naming: Expression_Emotion_Perspective_EyeDirection
- Direction is from face's perspective (Left = face looking to its left)

## Reminders for Future Implementation

- Random eye blink on severed head (future polish)

---

**End of Project Instructions**

These instructions should be followed for every conversation in this project.

# Don't Lose Your Head - Claude Project Instructions

**Project:** Don't Lose Your Head
**Developer:** TecVooDoo LLC
**Designer:** Rune (Stephen Brandon)
**Unity Version:** 6.3 (2D Template)
**Project Path:** E:\Unity\DontLoseYourHead
**Last Updated:** December 19, 2025

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
- `ProjectInstructions_v13_UPDATED_12192025.md`
- `ProjectInstructions_v14_UPDATED_12192025.md` (second update same day)
- `GDD_v13_UPDATED_12192025.md`
- `DLYH_Architecture_v13_UPDATED_12192025.md`

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

### Edit Tools (Choose Based on Change Type)

| Tool | Best For |
|------|----------|
| `script_apply_edits` | Method-level changes (add/replace/delete methods) |
| `apply_text_edits` | Precise line/column edits |

### Resources

| Resource | Purpose |
|----------|---------|
| `list_resources` | List .cs files in project |
| `read_resource` | Read script with line slicing |
| `find_in_file` | Search patterns in specific file |

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
- `game_start` - Player name, grid sizes, word counts, difficulties [UPDATED v13]
- `game_end` - Win/loss, misses, total turns
- `game_abandon` - Phase (gameplay/quit), turn number [NEW v13]
- `player_guess` - Guess type, hit/miss, value
- `player_feedback` - Feedback text, win/loss context
- `error` - Unity errors/exceptions

### Editor Dashboard [NEW v13]
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
| Background music (MusicManager with shuffle/crossfade) | COMPLETE |
| Mute buttons during gameplay (SFX and Music) | COMPLETE |
| Telemetry Dashboard (Editor window) | COMPLETE [NEW v13] |
| Enhanced telemetry (player name, game abandon) | COMPLETE [NEW v13] |
| MessagePopup positioning fix | COMPLETE [NEW v13] |
| Head face expressions | IN PROGRESS (sprites need assignment) |
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

### MusicManager Setup

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

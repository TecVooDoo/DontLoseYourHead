# Don't Lose Your Head - Claude Project Instructions

**Project:** Don't Lose Your Head
**Developer:** TecVooDoo LLC
**Designer:** Rune (Stephen Brandon)
**Unity Version:** 6.3 (2D Template)
**Project Path:** E:\Unity\DontLoseYourHead
**Last Updated:** December 21, 2025

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
- `ProjectInstructions_v15_UPDATED_12212025.md`
- `ProjectInstructions_v16_UPDATED_12212025.md` (second update same day)
- `GDD_v15_UPDATED_12212025.md`
- `DLYH_Architecture_v15_UPDATED_12212025.md`

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
| Telemetry Dashboard (Editor window) | COMPLETE |
| Enhanced telemetry (player name, game abandon) | COMPLETE |
| MessagePopup positioning (bottom of screen) | COMPLETE |
| Main menu trivia display | COMPLETE |
| Head face expressions | COMPLETE [NEW v15] |
| Final execution audio sequence (3-part) | COMPLETE [NEW v15] |
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

## Recent Changes (v15)

### Head Face Expressions - COMPLETE [NEW v15]

The head face expression system is now fully implemented and configured in Unity.

### Final Execution Audio Sequence [NEW v15]

The guillotine execution now uses a dramatic 3-part audio sequence:

1. **Final Guillotine Raise** - Blade rises to top (above hash marks)
2. **Hook Unlock** - Dramatic pause with release sound
3. **Final Guillotine Chop** - Blade drops for execution

**Audio Files (assign in GuillotineAudioManager):**
- `1_final_guillotine_raise` - Final blade raise sound
- `2_final_hook_unlock` - Hook unlock/release sound
- `3_final_guillotine_chop` - Final blade drop/chop sound

**Timing Settings (configurable in GuillotineDisplay):**
- `_pauseBeforeExecution` - Pause before execution sequence begins (default 3s)
- `_finalRaiseDuration` - Duration for blade to rise to top (default 6s)
- `_pauseBeforeUnlock` - Pause after raise before hook unlock (default 0.3s)
- `_pauseAfterUnlock` - Pause after unlock before blade drops (default 0.4s)

### Miss Limit Difficulty Modifiers [UPDATED v15]

Miss limits increased to improve win rates:

| Difficulty | Modifier | Example (6x6, 4 words) |
|------------|----------|------------------------|
| Easy | +9 | 25 misses |
| Normal | +6 | 22 misses |
| Hard | +2 | 18 misses |

Formula: Base (15) + Grid Bonus + Word Modifier (-2 for 4 words) + Difficulty Modifier

This sequence is used for both execution types:
- `AnimateGameOver()` - When player reaches miss limit
- `AnimateDefeatByWordsFound()` - When opponent finds all words

### MusicManager Setup

1. **Create MusicManager GameObject** in scene
2. **Add MusicManager component**
3. **Assign tracks to `_tracks` array:**
   - A tavern, a bard, a quest.wav
   - An Ocean of Stars - Mastered.wav
   - The Monster Within - Mastered.wav

## Reminders for Future Implementation

- Random eye blink on severed head (future polish)

---

**End of Project Instructions**

These instructions should be followed for every conversation in this project.

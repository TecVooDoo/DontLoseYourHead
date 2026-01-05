# Don't Lose Your Head - Project Instructions

**Project:** Don't Lose Your Head
**Developer:** TecVooDoo LLC
**Designer:** Rune (Stephen Brandon)
**Unity Version:** 6.3 (2D Template)
**Project Path:** E:\Unity\DontLoseYourHead
**Document Version:** 18
**Last Updated:** December 28, 2025

---

## Shared Documentation

**This project follows TecVooDoo standards. Review these documents:**

| Document | Location | Purpose |
|----------|----------|---------|
| Core Protocols | `E:\TecVooDoo\Projects\Documents\CORE_DevelopmentProtocols.md` | Universal development rules |
| Unity Standards | `E:\TecVooDoo\Projects\Documents\Type\TYPE_Unity.md` | Unity-specific patterns and tools |

---

## Project Path

**The Unity project is located at:** `E:\Unity\DontLoseYourHead`

Do NOT use worktree paths like `C:\Users\steph\.claude-worktrees\...` - always use the E: drive path for all file operations.

---

## Project Documents

| Document | Purpose |
|----------|---------|
| DLYH_GDD | Game design and mechanics |
| DLYH_Architecture | Script catalog, packages, code structure |
| DLYH_DesignDecisions | History, lessons learned, version tracking |
| DLYH_ProjectInstructions | Development protocols (this document) |

**Naming Convention:** `DLYH_DocumentName_v#_MMDDYYYY.md`

All four documents share the same version number. Increment all when any document is updated.

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
- **Features:** Event breakdown, leaderboard, win rate stats, CSV export

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
| Word row cell-based display | TODO |
| 2-player networking mode | TODO |
| Mobile implementation | TODO |

---

## Project-Specific Notes

### Drug Word Filtering

Banned words filter in WordBankImporter.cs prevents drug-related words from appearing:

**Banned Words:** heroin, cocaine, meth, crack, weed, opium, morphine, ecstasy, molly, dope, smack, coke

### Board Reset on New Game

Game state properly resets when starting a new game:
- `GameplayUIController.ResetGameplayState()` - Clears guessed words, resets trackers and guillotines
- `SetupSettingsPanel.ResetForNewGame()` - Clears grid, resets word rows
- `GuillotineDisplay.Reset()` - Restores blade/head positions

### Settings Button During Gameplay

- `GameplaySettingsButton.cs` calls `SettingsPanel.ShowFromGameplay()`
- `SettingsPanel` has contextual behavior based on where it was opened

### Scene File Protection

Check git diff before committing scene changes - layout values can be accidentally modified.

---

## Known Issues / TODO for Next Session

- (None currently)

---

## Reminders for Future Implementation

- Random eye blink on severed head (future polish)

---

**End of Project Instructions**

Review CORE_DevelopmentProtocols.md and TYPE_Unity.md for full development standards.

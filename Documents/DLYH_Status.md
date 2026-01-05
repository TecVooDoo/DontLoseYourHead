# Don't Lose Your Head - Project Status

**Project:** Don't Lose Your Head
**Developer:** TecVooDoo LLC / Rune (Stephen Brandon)
**Platform:** Unity 6.3 (6000.3.0f1)
**Source:** `E:\Unity\DontLoseYourHead`
**Document Version:** 7
**Last Updated:** January 6, 2026

---

## Quick Context

**What is this game?** A competitive word game combining Hangman's letter-guessing with Battleship's grid-based hidden information. Players place hidden words on grids and take turns guessing letters or coordinates to find opponent's words before the guillotine blade falls.

**Key Innovation:** Asymmetric difficulty - mixed-skill players compete fairly with different grid sizes, word counts, and difficulty settings.

**Current Phase:** Phase 4 (Polish) wrapping up, Phase 5 (UX + Multiplayer) starting

**Last Session (Jan 6, 2026):** Cell sizing partially fixed - cells now square (40x40) when prefab settings correct. Fixed row width issue by delaying initialization one frame (WaitForEndOfFrame) so RectTransform layout is calculated before reading container width. Rows now fit inside panel, but cells are vertically stretched again. Next session: investigate why cells stretch vertically at runtime despite correct prefab settings.

---

## Active TODO

### Immediate (New UI System)
- [x] Set CellContainer Horizontal Layout Group spacing to 0
- [x] Create action button icon sprites (Select, Place, Delete, GuessWord)
- [x] Save WordPatternRowUI as prefab
- [x] Create WordPatternPanelUI script
- [x] Build word pattern panel hierarchy (Background + RowContainer)
- [ ] **TROUBLESHOOT: Cell vertical stretching** - Cells square in prefab but stretch vertically at runtime
  - LetterCellUI prefab: 40x40 RectTransform, Layout Element Preferred 40x40, Flexible unchecked
  - CellContainer: Horizontal Layout Group with Control Child Size OFF for both
  - Row width now correct (fits in panel) after delayed initialization fix
  - Cells still getting stretched vertically - may be CellContainer height or row height issue
  - Try: Check if CellContainer has stretch anchors affecting height
  - Try: Set explicit height on CellContainer or check Horizontal Layout Group height settings
- [ ] Build complete Setup screen layout
- [ ] Build complete Gameplay screen layout
- [ ] Wire new UI to existing game systems
- [ ] Test all DOTween animations
- [ ] Remove/archive legacy WordPatternRow components

### Phase 4 Remaining (Polish)
- [ ] DOTween animations (reveals, transitions, feedback)
- [ ] Feel effects (screen shake, juice)
- [ ] Win/Loss tracker vs AI (session stats)
- [ ] Medieval/carnival themed monospace font
- [ ] UI skinning (medieval carnival theme)
- [ ] Character avatars
- [ ] Background art

### Phase 5 (Future)
- [ ] Setup screen UX redesign (wizard flow)
- [ ] 2-player networking mode
- [ ] Mobile implementation

### Future Polish Ideas
- [ ] Random eye blink on severed head

---

## What Works (Completed Features)

**Core Mechanics:** Grid placement, word entry, letter/coordinate/word guessing, miss limit formula, win/lose conditions

**AI Opponent ("The Executioner"):** Adaptive difficulty, rubber-banding, strategy selection (letter/coordinate/word), memory system, think time variation

**Audio:** SFX system, background music with shuffle/crossfade, dynamic tempo on danger, mute buttons, guillotine sounds (3-part execution sequence)

**Polish:** Help overlay, feedback panel, tooltips, profanity/drug word filtering, telemetry system, trivia display, head face expressions, extra turn on word completion, version display, settings from gameplay

---

## What Doesn't Work / Known Issues

- Legacy WordPatternRow uses text field (migrating to cell-based system)
- Some scripts exceed 800 lines (GameplayUIController ~2150 lines needs extraction)
- Scene files can get accidentally modified - check git diff before commits

---

## Architecture

### Namespaces

| Namespace | Scripts | Purpose |
|-----------|---------|---------|
| `TecVooDoo.DontLoseYourHead.UI` | 28 | Main UI scripts (including new UI) |
| `DLYH.UI` | 6 | Main menu, settings, help, tooltips |
| `TecVooDoo.DontLoseYourHead.UI.Utilities` | 1 | RowDisplayBuilder |
| `TecVooDoo.DontLoseYourHead.Core` | 4 | Game state/difficulty |
| `DLYH.AI.Config` | 1 | AI configuration |
| `DLYH.AI.Core` | 4 | AI controllers |
| `DLYH.AI.Data` | 2 | AI data utilities |
| `DLYH.AI.Strategies` | 4 | AI guess strategies |
| `DLYH.Audio` | 5 | UI, guillotine, music audio |
| `DLYH.Telemetry` | 1 | Playtest analytics |
| `DLYH.Editor` | 1 | Telemetry Dashboard |

### Key Folders

```
Assets/DLYH/
  Scripts/
    AI/           - ExecutionerAI, strategies, rubber-banding
    Audio/        - UIAudioManager, MusicManager, GuillotineAudioManager
    Core/         - Grid, Word, DifficultyCalculator
    Editor/       - TelemetryDashboard
    Telemetry/    - PlaytestTelemetry
    UI/           - All UI controllers and components
      Controllers/ - Extracted controller classes
      Services/    - GuessProcessor, WinConditionChecker
  NewUI/          - LetterCellUI.prefab
  Scenes/
    Main.unity    - Production scene
    NewUIDesign.unity - Test scene for new UI
```

### Key Scripts

| Script | Lines | Namespace | Purpose |
|--------|-------|-----------|---------|
| MainMenuController | ~455 | DLYH.UI | Game flow, menu navigation |
| GameplayUIController | ~2150 | TecVooDoo...UI | Master gameplay controller |
| SetupSettingsPanel | ~850 | TecVooDoo...UI | Player setup configuration |
| PlayerGridPanel | ~1120 | TecVooDoo...UI | Single player grid display |
| ExecutionerAI | ~493 | DLYH.AI.Core | AI opponent coordination |
| LetterCellUI | ~543 | TecVooDoo...UI | Unified cell (letters/icons) |
| WordPatternRowUI | ~655 | TecVooDoo...UI | Row manager (12 cells) |

### Packages

- Odin Inspector 4.0.1.2
- DOTween Pro 1.0.386
- Feel 5.9.1
- UniTask 2.5.10
- New Input System 1.16.0
- Classic_RPG_GUI (UI theme assets)

### UI Icon Mapping (Classic_RPG_GUI/Parts/)

| Action | Active Icon | Inactive Icon |
|--------|-------------|---------------|
| Select | Mini_arrow_right2.png | Mini_arrow_right2_t.png |
| Place | Mini_add.png | Mini_add_t.png |
| Delete | Mini_exit.png | Mini_exit_t.png |
| GuessWord | Mini_help.png | Mini_help_t.png |

---

## Game Rules

### Miss Limit Formula

```
MissLimit = 15 + OpponentGridBonus + OpponentWordModifier + YourDifficultyModifier

OpponentGridBonus: 6x6=+3, 7x7=+4, 8x8=+6, 9x9=+8, 10x10=+10, 11x11=+12, 12x12=+13
OpponentWordModifier: 3 words=+0, 4 words=-2
YourDifficultyModifier: Easy=+4, Normal=+0, Hard=-4
```

### Win Conditions

- Reveal all opponent's letters AND grid positions, OR
- Opponent reaches their miss limit

### Key Mechanics

- 3 words HARDER than 4 (fewer letters to find)
- Wrong word guess = 2 misses (double penalty)
- Complete a word = extra turn (queued if multiple)
- Grid cells only revealed via coordinate guesses (NOT word guesses)

---

## AI System ("The Executioner")

### AI Grid Selection by Player Difficulty

| Player Difficulty | AI Grid Size | AI Word Count |
|-------------------|--------------|---------------|
| Easy | 6x6, 7x7, or 8x8 | 4 words |
| Normal | 8x8, 9x9, or 10x10 | 3 or 4 words |
| Hard | 10x10, 11x11, or 12x12 | 3 words |

### Rubber-Banding

| Player Difficulty | AI Start Skill | Hits to Increase | Misses to Decrease |
|-------------------|----------------|------------------|-------------------|
| Easy | 0.25 | 5 | 4 |
| Normal | 0.50 | 3 | 3 |
| Hard | 0.75 | 2 | 5 |

**Bounds:** Min 0.25, Max 0.95, Step 0.10

### Strategy Selection by Grid Density

| Density | Strategy Preference |
|---------|---------------------|
| >35% fill | Favor coordinate guessing |
| 12-35% fill | Balanced approach |
| <12% fill | Favor letter guessing |

### Word Guess Confidence Thresholds

| Skill Level | Min Confidence |
|-------------|----------------|
| 0.25 (Easy) | 90%+ |
| 0.50 (Normal) | 70%+ |
| 0.80 (Hard) | 50%+ |
| 0.95 (Expert) | 30%+ |

---

## Audio System

### Sound Effects (UIAudioManager)

| Sound Group | Usage |
|-------------|-------|
| KeyboardClicks | Letter tracker, physical keyboard |
| GridCellClicks | Grid cell clicks |
| ButtonClicks | General UI buttons |
| PopupOpen | MessagePopup |
| ErrorSounds | Invalid actions |

### Guillotine Audio (GuillotineAudioManager)

| Sound | When Played |
|-------|-------------|
| Rope Stretch + Blade Raise | On each miss |
| Final Guillotine Raise | Part 1 of execution |
| Hook Unlock | Part 2 of execution |
| Final Guillotine Chop | Part 3 of execution |
| Head Removed | Head falls into basket |

### Music System (MusicManager)

**Features:**
- Shuffle playlist (Fisher-Yates), never repeats consecutively
- Crossfade 1.5 seconds between tracks
- Dynamic tempo: 1.0x (normal), 1.08x (80-94% danger), 1.12x (95%+ danger)

**Static Methods:** `ToggleMuteMusic()`, `SetTension(float)`, `ResetMusicTension()`, `IsMusicMuted()`

---

## Data Flow Diagrams

### Gameplay Guess Flow

```
Player clicks opponent's letter 'E'
  -> GameplayUIController.HandleLetterGuess('E')
    -> [Check word guess mode active?]
      -> (yes) Route to WordGuessModeController
      -> (no) GuessProcessor.ProcessLetterGuess('E')
        -> Hit/Miss/AlreadyGuessed result
        -> [Check completed words] -> Queue extra turns
```

### AI Turn Flow

```
EndPlayerTurn()
  -> RecordPlayerGuess(wasHit) -> DifficultyAdapter updates skill
  -> TriggerAITurn()
  -> ExecutionerAI.ExecuteTurnAsync()
  -> Wait think time (0.8-2.5s)
  -> BuildAIGameState()
  -> SelectStrategy() based on grid density
  -> Fire OnLetterGuess/OnCoordinateGuess/OnWordGuess
  -> GameplayUIController.HandleAI*Guess()
  -> EndOpponentTurn()
```

### Board Reset Flow

```
StartNewGame()
  -> GameplayUIController.ResetGameplayState()
    -> Clear guessed word lists, letter trackers, guillotines
  -> SetupSettingsPanel.ResetForNewGame()
    -> Clear grid, word rows, player name
  -> GuillotineDisplay.Reset()
    -> Restore blade/head positions, HeadFaceController.ResetFace()
```

---

## Telemetry

**Endpoint:** `https://dlyh-telemetry.runeduvall.workers.dev`
**Dashboard:** DLYH > Telemetry Dashboard (Unity Editor menu)

### Events Tracked

| Event | Data |
|-------|------|
| session_start | Platform, version, screen |
| session_end | Auto on quit |
| game_start | Player name, grids, words, difficulties |
| game_end | Win/loss, misses, turns |
| game_abandon | Phase, turn number |
| player_guess | Type, hit/miss, value |
| player_feedback | Text, win/loss context |
| error | Unity errors with stack |

### Cloudflare Endpoints

| Endpoint | Purpose |
|----------|---------|
| `/event` POST | Receive events |
| `/events` GET | Last 100 events |
| `/summary` GET | Event type counts |
| `/feedback` GET | Player comments |
| `/stats` GET | Aggregated stats |

---

## Key Patterns (with examples)

### 1. Event Timing Pattern

```csharp
// CORRECT: State before event
_isActive = false;
OnSomeEvent?.Invoke();

// WRONG: Handlers see stale state
OnSomeEvent?.Invoke();
_isActive = false;
```

### 2. Position Memory Pattern

```csharp
private Vector2 _lastDraggedPosition;
private bool _hasBeenDragged;

void OnDrag(PointerEventData e) {
    _popupRect.anchoredPosition = localPoint + _dragOffset;
    _lastDraggedPosition = _popupRect.anchoredPosition;
    _hasBeenDragged = true;
}

void OnShow() {
    if (_hasBeenDragged)
        _popupRect.anchoredPosition = _lastDraggedPosition;
    else
        _popupRect.anchoredPosition = GetDefaultPosition();
}
```

### 3. Kill DOTween Before Reset

```csharp
private void ResetHeadPosition() {
    if (_headTransform != null) {
        DOTween.Kill(_headTransform);  // Kill first!
        if (_headPositionStored)
            _headTransform.anchoredPosition = _originalHeadPosition;
    }
}
```

### 4. Extra Turn Queue Pattern

```csharp
Queue<string> _extraTurnQueue = new Queue<string>();

// On word completion:
_extraTurnQueue.Enqueue(completedWord);

// After guess result:
if (_extraTurnQueue.Count > 0) {
    string word = _extraTurnQueue.Dequeue();
    // Show extra turn message, don't end turn
}
```

### 5. SQLite Boolean Handling

```csharp
// SQLite returns 0 or 1, not true/false
public int player_won;  // Parse as int
public bool PlayerWon => player_won != 0;  // Convert
```

---

## Lessons Learned (Don't Repeat)

### Unity/C# Patterns
1. **Set state BEFORE firing events** - handlers may check state immediately
2. **Initialize UI to known states** - don't rely on defaults
3. **Kill DOTween before reset** - prevents animation conflicts
4. **Store original positions** - for proper reset after animations
5. **Use New Input System** - `Keyboard.current`, not `Input.GetKeyDown`
6. **No `var` keyword** - explicit types always
7. **800 lines max** - extract controllers when approaching limit
8. **Prefer UniTask over coroutines** - `await UniTask.Delay(1000)` not `yield return`
9. **No allocations in Update** - cache references, use object pooling
10. **Controller extraction** - large MonoBehaviours delegate to plain C# classes
11. **Callback injection** - services receive Actions/Funcs for operations they don't own
12. **Validate after MCP edits** - run validate_script to catch syntax errors
13. **Avoid MCP hierarchy changes** - can cause Unity lockups

### Project-Specific
14. **Use E: drive path** - never worktree paths like `C:\Users\steph\.claude-worktrees\...`
15. **Check scene file diffs** - layout can be accidentally modified
16. **SQLite booleans are integers** - parse as int, convert to bool
17. **Drug words filtered** - heroin, cocaine, meth, crack, weed, opium, morphine, ecstasy, molly, dope, smack, coke
18. **EditorWebRequest loading order** - set `_isLoading = false` BEFORE callback that may start next request

---

## Bug Patterns to Avoid

| Bug Pattern | Cause | Prevention |
|-------------|-------|------------|
| Guess Word buttons disappearing wrong rows | State set AFTER events | Set state BEFORE firing events |
| Autocomplete floating at top | Not hidden at init | Call Hide() in Initialize() |
| Board not resetting | No reset logic | Call ResetGameplayState() on new game |
| Guillotine head stuck | No stored position | Store original position on Initialize |
| MessagePopup off-screen | Complex calculations | Use fixed Y for known anchors |

---

## AI Rules (Embedded)

### Critical Protocols
1. **Verify names exist** - search before referencing files/methods/classes
2. **Step-by-step verification** - one step at a time, wait for confirmation
3. **Read before editing** - always read files before modifying
4. **ASCII only** - no smart quotes, em-dashes, or special characters
5. **Fetch current docs** - don't rely on potentially outdated knowledge
6. **Prefer structured edits** - use script_apply_edits for method changes
7. **Full document review** - read uploaded files thoroughly, don't skim
8. **Be direct** - give honest assessments, don't sugar-coat
9. **Acknowledge gaps** - say explicitly when something is missing or unclear

---

## Session Close Checklist

After each work session, update this document:

- [ ] Move completed TODOs to "What Works" section
- [ ] Add any new issues to "What Doesn't Work"
- [ ] Update "Last Session" with date and summary
- [ ] Add new lessons to "Lessons Learned" if applicable
- [ ] Increment version number in header

---

## Version History

| Version | Date | Summary |
|---------|------|---------|
| 7 | Jan 6, 2026 | Fixed row width with delayed init, cell vertical stretch still needs fix |
| 6 | Jan 5, 2026 | WordPatternPanelUI created, cell sizing troubleshooting needed |
| 5 | Jan 5, 2026 | WordPatternRowUI complete with icons, active/inactive states, Classic_RPG_GUI integration |
| 4 | Jan 5, 2026 | Removed shared doc pointers (captured in Lessons Learned), streamlined AI Rules |
| 3 | Jan 5, 2026 | Enhanced with AI system, audio, patterns, data flows from archived v19 docs |
| 2 | Jan 5, 2026 | Archived NewUI architecture doc |
| 1 | Jan 4, 2026 | Initial consolidated document (replaces 4-doc system) |

---

**End of Project Status**

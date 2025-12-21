# Don't Lose Your Head - Design Decisions and Lessons Learned

**Version:** 15.0
**Date Created:** November 22, 2025
**Last Updated:** December 21, 2025
**Developer:** TecVooDoo LLC

---

## Purpose

This document captures design decisions, technical insights, and lessons learned during development. It serves as a historical record and reference for future development.

---

## Version History

| Version | Date | Summary |
|---------|------|---------|
| 1.0 | Nov 22, 2025 | Initial document |
| 2.0 | Dec 8, 2025 | Added refactoring patterns |
| 3.0 | Dec 12, 2025 | Autocomplete system decisions |
| 4.0 | Dec 13, 2025 | AI system integration |
| 5.0 | Dec 14, 2025 | Playtest bug fixes, AI variety |
| 6.0 | Dec 14, 2025 | Document refactoring (single responsibility) |
| 7.0 | Dec 16, 2025 | UI audio system, playtest telemetry |
| 8.0 | Dec 16, 2025 | Feedback panel, word list filtering, coordinate hit fix |
| 9.0 | Dec 17, 2025 | Restored Phase 4 TODO items, Cloudflare deployment complete |
| 9.1 | Dec 17, 2025 | Guillotine assembly hierarchy built (UI rectangles), layout restructure in progress |
| 10.0 | Dec 17, 2025 | Version sync with other docs (no content changes) |
| 11.0 | Dec 18, 2025 | Extra turn system, guillotine audio, head face controller |
| 12.0 | Dec 18, 2025 | Background music system, mute buttons, dynamic tempo, Cloudflare build 2 |
| 13.0 | Dec 19, 2025 | Telemetry Dashboard, enhanced telemetry (player name, game abandon), MessagePopup positioning fix |
| 14.0 | Dec 20, 2025 | Main menu trivia display, MessagePopup positioned at bottom |
| 15.0 | Dec 21, 2025 | Head face expressions complete, 3-part final execution audio sequence |

---

## Design Decisions

### Word Count Difficulty

**Decision:** 3 words is HARDER than 4 words.

**Rationale:** Fewer words means fewer letters on the grid, making coordinate guessing less effective. The miss limit formula accounts for this with a -2 modifier for 4 words.

### Miss Limit Uses Opponent's Grid

**Decision:** Your miss limit is calculated using your OPPONENT's grid settings, not your own.

**Rationale:** This creates fair asymmetric difficulty. If your opponent has a larger grid with fewer words, you get more misses to find them.

### Grid Cells Not Revealed on Word Guess

**Decision:** Correctly guessing a word does NOT reveal the grid cells - only coordinate guesses reveal cells.

**Rationale:** This maintains strategic depth. Players must still use coordinate guesses to fully clear the grid for win conditions.

### AI Word Bank Access

**Decision:** AI has full access to the word bank.

**Rationale:** Fair since players could memorize it. AI doesn't need to be handicapped on vocabulary.

### AI Grid Variety by Difficulty

**Decision:** AI randomly selects grid size within difficulty-appropriate ranges.

| Player Difficulty | AI Grid Size | AI Word Count |
|-------------------|--------------|---------------|
| Easy | 6x6, 7x7, or 8x8 | 4 words |
| Normal | 8x8, 9x9, or 10x10 | 3 or 4 words |
| Hard | 10x10, 11x11, or 12x12 | 3 words |

**Rationale:** Adds gameplay variety while maintaining balance. Larger grids with fewer words are harder for the player to find.

### Rubber-Banding Bounds

**Decision:** AI skill bounded between 0.25 and 0.95.

**Rationale:**
- Minimum 0.25 ensures AI always makes somewhat intelligent moves (not random)
- Maximum 0.95 ensures AI is never perfect (always beatable)
- Adjustment step of 0.10 prevents wild skill swings

### Extra Turn on Word Completion

**Decision:** Completing a word (revealing all letters) grants an extra turn.

**Rules:**
- Works for letter guesses, coordinate guesses, AND correct word guesses
- Multiple words completed at once queue multiple extra turns (one per word)
- Extra turns processed one at a time (FIFO queue)
- Popup shows combined message: guess result + extra turn notification

**Rationale:** Rewards strategic play. Completing words should feel rewarding and encourage players to think about which guesses might complete multiple words at once.

### Head Face Expressions

**Decision:** Guillotine heads display dynamic facial expressions based on game state.

**Expression Logic:**
- Own danger level takes priority (high miss % = fear regardless of opponent)
- When safe, react to opponent's danger (opponent struggling = happiness)
- Execution faces: Horror for executed player, Evil Smile for winner

**Thresholds:**
| Percentage | Own State | Opponent Reaction (when safe) |
|------------|-----------|-------------------------------|
| 0-25% | Neutral/Happy | Neutral |
| 25-50% | Concerned | Happy |
| 50-75% | Worried | Smug |
| 75-90% | Scared | Evil Smile |
| 90%+ | Terror | Evil Smile |

**Rationale:** Adds personality and visual feedback. Players can "read" the game state from facial expressions without checking numbers.

### Guillotine Audio Layering

**Decision:** Blade raise plays two sounds simultaneously (rope stretch + blade movement).

**Rationale:** Creates richer audio experience. Single sounds felt thin; layering adds depth and tension.

### Final Execution Audio Sequence [NEW v15]

**Decision:** Replace simple chop sounds with a dramatic 3-part audio sequence for executions.

**Sequence:**
1. **Final Guillotine Raise** - Blade rises to top (above hash marks)
2. **Hook Unlock** - Dramatic pause with mechanical release sound
3. **Final Guillotine Chop** - Blade drops for execution

**Audio Files:**
- `1_final_guillotine_raise` - Final blade raise sound
- `2_final_hook_unlock` - Hook unlock/release sound
- `3_final_guillotine_chop` - Final blade drop/chop sound

**Timing (configurable in GuillotineDisplay):**
- `_pauseBeforeExecution` - Pause before execution sequence begins (default 3s)
- `_finalRaiseDuration` - Duration for blade to rise to top (default 6s)
- `_pauseBeforeUnlock` - Pause after raise before hook unlock (default 0.3s)
- `_pauseAfterUnlock` - Pause after unlock before blade drops (default 0.4s)

**Rationale:** The original execution happened too quickly. The 3-part sequence builds tension and gives players time to appreciate the dramatic moment. The space above the hash marks allows the blade to rise to a visible "ready" position before dropping.

### Miss Limit Difficulty Modifiers [UPDATED v15]

**Decision:** Increase miss limits across all difficulties to improve player win rates.

**Previous Values:**
- Easy: +4, Normal: 0, Hard: -4

**New Values:**
- Easy: +9, Normal: +6, Hard: +2

**Example (6x6 grid, 4 words):**
| Difficulty | Calculation | Total Misses |
|------------|-------------|--------------|
| Easy | 15 + 3 - 2 + 9 | 25 |
| Normal | 15 + 3 - 2 + 6 | 22 |
| Hard | 15 + 3 - 2 + 2 | 18 |

**Rationale:** Telemetry showed more losses than wins. Increasing miss allowances gives players more room for error while maintaining difficulty differentiation.

### Face Direction from Face's Perspective

**Decision:** Face sprite naming uses the face's perspective for eye direction (Left = face looking to its left).

**Rationale:** More intuitive for asset organization. When viewing the sprite, "Left" means eyes pointing left.

### Background Music System

**Decision:** Implement shuffle playlist with crossfade between tracks.

**Features:**
- Fisher-Yates shuffle ensures random order
- Never repeats same song consecutively (swap if shuffle produces same first track)
- 1.5 second crossfade between tracks using two AudioSources
- Starts automatically at Main Menu
- Respects Music volume from SettingsPanel

**Rationale:** Continuous music enhances atmosphere. Shuffle prevents repetition fatigue. Crossfade sounds more professional than hard cuts.

### Dynamic Music Tempo

**Decision:** Increase music tempo/pitch based on danger level.

**Levels:**
| Danger % | Pitch | Effect |
|----------|-------|--------|
| 0-79% | 1.0x | Normal |
| 80-94% | 1.08x | Subtle tension |
| 95%+ | 1.12x | Noticeable urgency |

**Rationale:** Builds tension without being annoying. 1.12x is noticeable but doesn't sound "chipmunk". Smooth transitions via coroutine prevent jarring changes.

### Mute Buttons During Gameplay

**Decision:** Add separate SFX and Music mute toggle buttons accessible during gameplay.

**Implementation:**
- `UIAudioManager.ToggleMute()` for SFX
- `MusicManager.ToggleMute()` for Music
- Both accessible from gameplay button bar

**Rationale:** Players requested quick toggle without navigating to Settings. Separate controls let players mute music but keep SFX (or vice versa).

### Default Audio Volume 50%

**Decision:** Both SFX and Music default to 50% volume.

**Rationale:** Middle ground that works for most setups. Players can adjust up or down as needed. Prevents either being too loud on first play.

### MessagePopup Positioning

**Decision:** MessagePopup defaults to bottom of gameplay area and remembers player's dragged position.

**Behavior:**
- Default position: Bottom of screen (Y=62.5 with bottom-center anchors)
- If player drags the popup, that position is remembered for subsequent popups
- Position memory resets when starting a new game

**Rationale:** Bottom positioning keeps the popup out of the way of gameplay elements. Position memory respects player preferences during gameplay.

### Telemetry Dashboard as Editor Window

**Decision:** Create an Editor window for viewing telemetry data rather than a runtime UI.

**Rationale:**
- No runtime performance impact
- Accessible during development without affecting builds
- Can view data across multiple play sessions
- CSV export for external analysis

### Main Menu Trivia Display

**Decision:** Display rotating historical trivia facts on the main menu screen.

**Features:**
- 25 facts about guillotines and famous beheadings
- Fades in, displays for 5 seconds, fades out (configurable)
- Starts at random fact, cycles sequentially through all
- Stops when leaving main menu

**Sample Facts:**
- "The guillotine was used in France until 1977."
- "Marie Antoinette was executed by guillotine on October 16, 1793."
- "Mary, Queen of Scots required three blows of the axe."
- "Lavoisier, the father of chemistry, was guillotined in 1794."

**Rationale:** Adds thematic flavor and educational content while players are on the main menu. Keeps the menu screen interesting during repeated visits.

---

## Lessons Learned

### Unity/C# Patterns

**1. Event Timing for State Checks**
When event handlers check object state, update state BEFORE firing events:
```csharp
// CORRECT:
_isActive = false;
OnSomeEvent?.Invoke();

// WRONG:
OnSomeEvent?.Invoke();
_isActive = false;
```

**2. Initialize UI to Known States**
Always explicitly set UI components to known states during initialization. Don't rely on default states or Awake() alone.

**3. Validate Positions Before UI Placement**
Check if reference elements have valid positions (not at origin) before positioning relative to them. Layout calculations may not be complete during early lifecycle.

**4. Guard Against Event Re-triggering**
When performing batch operations that trigger events, hide/reset UI before AND after the operation.

**5. Check All Code Paths for State Restoration**
When methods have multiple exit paths, ensure each path restores necessary state. Early returns often skip cleanup.

**6. Route Input Based on Active Mode**
Input handlers should check for active modal states first and route appropriately.

**7. ScriptableObject Defaults vs Asset Instances**
Changing default values in ScriptableObject code does NOT update existing asset instances. Must manually update in Inspector.

**8. Controller Extraction Pattern**
Large MonoBehaviours should delegate to plain C# controller classes that receive dependencies via constructor.

**9. Callback Injection Pattern**
Services receive Actions/Funcs for operations they need but don't own.

**10. Defensive Initialization Pattern**
`EnsureControllersInitialized()` allows safe calling before Start() runs.

**11. Boolean Flags for Persistent UI State**
Use boolean flags (e.g., `_isWordSolved`) for state that must persist across show/hide cycles. Simple hide/show calls are unreliable.

**12. Use New Input System**
Use `Keyboard.current` from New Input System, not legacy `Input.inputString`.

**13. Queue Pattern for Sequential Rewards**
When multiple rewards can occur at once but should be shown sequentially, use a queue:
```csharp
Queue<string> _rewardQueue = new Queue<string>();
// Add rewards as they occur
_rewardQueue.Enqueue(reward);
// Process one at a time
if (_rewardQueue.Count > 0) {
    var reward = _rewardQueue.Dequeue();
    // Show reward, don't end turn
}
```

**14. Two AudioSources for Crossfade**
Music crossfade requires two AudioSources - one fading out, one fading in. Swap references after fade completes.

**15. Singleton with Static Convenience Methods**
For managers that need global access, combine singleton pattern with static methods for cleaner calling code:
```csharp
// Instead of:
MusicManager.Instance.ToggleMute();
// Allow:
MusicManager.ToggleMuteMusic();
```

**16. Use E: Drive Path, Not Worktree**
Project path is `E:\Unity\DontLoseYourHead`. Never use worktree paths like `C:\Users\steph\.claude-worktrees\...` for file operations.

**17. SQLite Boolean Returns as Integer**
SQLite (used by Cloudflare D1) returns boolean values as integers (0/1). When parsing JSON from D1 queries in Unity, use `int` type and convert:
```csharp
public int player_won;  // SQLite returns 0 or 1
public bool PlayerWon => player_won != 0;
```

**18. EditorWebRequest Loading Flag Order**
When chaining EditorWebRequests, set `_isLoading = false` BEFORE invoking the callback that may start the next request:
```csharp
// CORRECT:
_isLoading = false;
ProcessRequestResult();  // May start next request

// WRONG:
ProcessRequestResult();  // Tries to start next request but isLoading still true
_isLoading = false;
```

**19. Position Memory Pattern for Draggable UI**
For UI elements that can be dragged, remember the position for subsequent shows:
```csharp
private Vector2 _lastDraggedPosition;
private bool _hasBeenDragged;

void OnDrag(PointerEventData eventData) {
    _popupRect.anchoredPosition = localPoint + _dragOffset;
    _lastDraggedPosition = _popupRect.anchoredPosition;
    _hasBeenDragged = true;
}
```

**20. Fixed Position for Known Anchor Setup**
When anchors and pivot are known, use fixed position values rather than calculating from canvas dimensions:
```csharp
// With bottom-center anchors (0.5, 0), pivot (0.5, 0.5), height 125:
// Y=62.5 places bottom edge at canvas bottom
return new Vector2(0, 62.5f);
```

### AI-Specific Insights

**21. Grid Density Affects Strategy**
- High density (>35%): Favor coordinate guessing
- Medium density (12-35%): Balanced approach
- Low density (<12%): Favor letter guessing

**22. Rubber-Banding Needs Bounds Testing**
Test with intentionally poor play to find edge cases. Systems can over-correct without proper bounds.

**23. Adaptive Thresholds**
The rubber-banding system itself adapts if the player is consistently struggling or dominating.

---

## Bug Fix History (December 14, 2025)

| Bug | Root Cause | Fix |
|-----|------------|-----|
| Guess Word buttons disappearing on wrong rows | `_isActive = false` set AFTER firing events | Moved state update BEFORE events |
| Autocomplete dropdown floating at top | Not hidden at init, no position validation | Added Hide() in Initialize(), position check |
| Autocomplete appearing after random operations | Row selection events re-triggering | Added Hide() before AND after operations |
| Guess Word buttons disappearing after "Already guessed" | Early return without restoring buttons | Added ShowAllGuessWordButtons() before return |
| Letter Tracker not routing to Word Guess mode | HandleLetterGuess() not checking for active mode | Added IsInKeyboardMode check at start |
| AI always picks 8x8/3 words | Hardcoded constants | Dynamic selection by player difficulty |
| AI too easy when player misses | Rubber-banding too aggressive | Adjusted minSkill, missesToDecrease, adjustmentStep |

## Bug Fix History (December 16, 2025)

| Bug | Root Cause | Fix |
|-----|------------|-----|
| MessagePopup not appearing | Unknown (from previous session) | Bug fixed |
| Coordinate hits not tracking letters | `ProcessPlayerCoordinateGuess()` not adding letters to known set | Added `FindLetterAtCoordinate()` helper and letter tracking on hits |
| Game over popup showing wrong message | Multiple win conditions but generic message | Added `GameOverReason` enum and `ShowGameOverPopup()` with specific messages |

## Bug Fix History (December 19, 2025)

| Bug | Root Cause | Fix |
|-----|------------|-----|
| Telemetry Dashboard not loading events/feedback | `_isLoading` set to false AFTER callback | Reordered: set `_isLoading = false` BEFORE `ProcessRequestResult()` |
| Feedback not displaying in Dashboard | SQLite returns booleans as integers (0/1) | Changed `player_won` from `bool` to `int`, added `PlayerWon` property |
| Game over popup covering guillotine | Popup forced to center, ignoring player drag | Default to top (35%), remember dragged position, game over always at top |
| MessagePopup appearing partially off-screen | Initial position was center which could overflow | Changed default position to top of screen with proper Y offset |

## Bug Fix History (December 20, 2025)

| Bug | Root Cause | Fix |
|-----|------------|-----|
| MessagePopup position calculation wrong | Complex calculation based on canvas height | Simplified to fixed Y=62.5 for bottom-center anchors |

---

## New Systems (December 16, 2025)

### UI Audio System

**Components:**
- `SFXClipGroup.cs` - ScriptableObject for grouping audio clips with randomization
- `UIAudioManager.cs` - Singleton manager for playing UI sounds
- `UIButtonAudio.cs` - Component to auto-add click sounds to buttons

**Design Decisions:**
- Sounds are the same for player and opponent actions (player sees their own grid affected by opponent)
- Random clip selection with volume/pitch variation for natural feel
- Respects SFX volume setting from SettingsPanel
- Static convenience methods for easy calling (e.g., `UIAudioManager.KeyboardClick()`)

**Sound Categories:**
| Sound Group | Usage |
|-------------|-------|
| KeyboardClicks | Letter tracker buttons, physical keyboard input |
| GridCellClicks | Grid cell clicks during gameplay |
| ButtonClicks | General UI buttons |
| PopupOpen | MessagePopup display |
| ErrorSounds | Invalid actions |

### Playtest Telemetry System

**Components:**
- `PlaytestTelemetry.cs` - Singleton manager for sending events
- Cloudflare Worker backend at `dlyh-telemetry.runeduvall.workers.dev`
- D1 database for storage

**Events Tracked:**
| Event Type | Data |
|------------|------|
| session_start | Platform, version, screen size |
| session_end | (automatic on quit) |
| game_start | Player name, grid sizes, word counts, difficulties |
| game_end | Win/loss, miss counts, total turns |
| game_abandon | Phase (gameplay/quit), turn number |
| player_feedback | Feedback text, win/loss context |
| error | Unity errors/exceptions with stack traces |

**Design Decisions:**
- No individual guess tracking (too noisy for basic playtesting)
- Automatic error capture via `Application.logMessageReceived`
- Queue-based sending to handle network delays
- Volume caching for performance
- Game abandon tracking for both voluntary exits and browser/app closes

### Feedback Panel System

**Components:**
- `FeedbackPanel.cs` - UI panel for collecting player feedback at game end or from main menu

**Features:**
- Shows after game ends with win/loss title ("VICTORY!" or "DEFEATED!")
- Accessible from main menu with neutral title ("FEEDBACK")
- Optional text input for player comments
- Submit sends feedback to telemetry system
- Skip option to bypass without feedback

**Design Decisions:**
- Feedback is optional to reduce friction
- Win/loss context included with game-end feedback
- Separate telemetry event type (`player_feedback`) for easy filtering

### Word List Filtering

**Changes:**
- Word list reviewed and filtered for profanity and inappropriate content
- Removed: cums, fags, dicks, horny, faggot, orgasm, shitty, sodomy, whores (9 words)
- Note: Word list filtered to best of ability; use Feedback to report missed words

### Help Overlay System

**Components:**
- `HelpOverlay.cs` - Draggable help panel with gameplay instructions
- Scrollable content with rich text formatting (colors for grid states)

**Features:**
- Shows automatically on first game of session
- Hidden on subsequent games (player can toggle with "?" button)
- Draggable panel with screen bounds clamping
- Scroll view for lengthy help content
- Static session tracking via `_hasShownThisSession`

**Design Decisions:**
- First-session auto-show teaches new players without being annoying on replay
- Draggable so players can position it out of the way while reading
- Uses `OnEnable` coroutine to show after GameplayUIController activates the panel

### Tooltip System

**Components:**
- `ButtonTooltip.cs` - Hover tooltip trigger (attach to any UI element)
- `TooltipPanel.cs` - Self-registering tooltip display panel

**Features:**
- Configurable hover delay (default 0.5s)
- Auto-sizes to fit text content
- Positions above button, with fallback below if off-screen
- Screen edge clamping to keep tooltip visible

**Design Decisions:**
- Static panel registration allows single tooltip shared across all buttons
- Uses New Input System (Mouse.current) for position
- Panel auto-hides via TooltipPanel.Awake() registration

---

## New Systems (December 18, 2025)

### Extra Turn System [v11]

**Components:**
- Queue<string> `_extraTurnQueue` in GameplayUIController
- Modified `HandleLetterGuess()`, `HandleCellGuess()`, `HandleWordGuessProcessed()`
- New `ShowGuessResultAndProcessTurn()` method

**Flow:**
1. Guess is processed, result determined
2. Check if any words were completed by this guess
3. For each completed word, add to `_extraTurnQueue`
4. Show result popup with extra turn message if queue has items
5. Don't end turn if extra turn was granted

**Design Decisions:**
- Combined popup message (guess result + extra turn) to avoid rapid popup flashing
- Queue ensures multiple completed words each give one extra turn
- Clear queue on new game initialization

### Guillotine Audio System [v11] [UPDATED v15]

**Components:**
- `GuillotineAudioManager.cs` - Singleton for guillotine sounds

**Sound Types:**
| Sound | When Played |
|-------|-------------|
| Rope Stretch + Blade Raise | On each miss (blade rises) |
| Final Guillotine Raise | Part 1 of execution sequence [NEW v15] |
| Hook Unlock | Part 2 of execution sequence [NEW v15] |
| Final Guillotine Chop | Part 3 of execution sequence [NEW v15] |
| Head Removed | When head falls into basket |

**Design Decisions:**
- Two AudioSources for layered playback
- 3-part execution sequence replaces simple chop sounds [v15]
- Static convenience methods for easy calling from GuillotineDisplay

### Head Face Controller [v11] - COMPLETE [v15]

**Components:**
- `HeadFaceController.cs` - Facial expression manager
- Integrated with `GuillotineDisplay.cs`

**Sprite Slots:**
- Positive: Happy, Smug, Evil Smile
- Neutral: Neutral, Concerned
- Negative: Worried, Scared, Terror
- Execution: Horror, Victory

**Features:**
- Face direction controlled via `_lookLeft` (flips sprite via localScale.x)
- Automatic fallbacks if sprites not assigned (e.g., Terror falls back to Scared)
- Updates on every miss count change

**Status:** COMPLETE - All sprites assigned and working [v15]

### Music Manager [v12]

**Components:**
- `MusicManager.cs` - Singleton for background music

**Features:**
- Shuffle playlist with Fisher-Yates algorithm
- Crossfade between tracks (1.5 seconds)
- Never repeats same song consecutively
- Mute toggle support
- Dynamic tempo/pitch based on danger level
- Starts at Main Menu

**Music Tracks:**
- A tavern, a bard, a quest.wav
- An Ocean of Stars - Mastered.wav
- The Monster Within - Mastered.wav

**Static Methods:**
- `MusicManager.ToggleMuteMusic()`
- `MusicManager.SetTension(float dangerPercentage)`
- `MusicManager.ResetMusicTension()`
- `MusicManager.IsMusicMuted()`

### SFX Mute Support [v12]

**Changes to UIAudioManager:**
- Added `_isMuted` field and `IsMuted` property
- Added `ToggleMute()`, `Mute()`, `Unmute()` methods
- Added static `ToggleMuteSFX()` and `IsSFXMuted()` methods
- `GetSFXVolume()` returns 0 when muted

---

## New Systems (December 19, 2025)

### Telemetry Dashboard (Editor Window) [v13]

**Location:** `Assets/DLYH/Scripts/Editor/TelemetryDashboard.cs`

**Access:** DLYH > Telemetry Dashboard (menu)

**Tabs:**
1. **Summary** - Event type breakdown with visual bars
2. **Game Stats** - Player name leaderboard, win rate, completion rate, difficulty distribution
3. **Recent Events** - Last 100 events with timestamp, type, and JSON data
4. **Feedback** - Player feedback entries with win/loss context

**Features:**
- Auto-refresh button (fetches latest data from Cloudflare)
- Export CSV button (saves all data to timestamped file)
- Scroll views for long lists
- Visual percentage bars for event distribution

**Implementation Notes:**
- Uses `UnityWebRequest` for HTTP requests to Cloudflare Worker
- Editor-only (no runtime impact)
- Handles SQLite integer booleans via manual parsing

### Enhanced Telemetry [v13]

**New Data in game_start:**
- `player_name` - Name entered on setup screen

**New Event: game_abandon**
- `phase` - Either "gameplay" (returned to setup) or "quit" (closed browser/app)
- `turn_number` - How many turns were played before abandoning

**Turn Tracking:**
- `PlaytestTelemetry.SetTurnNumber(int)` - Called after each turn
- `_isGameInProgress` flag tracks if game is active
- `OnApplicationQuit()` sends game_abandon if game was in progress

### Cloudflare Worker /stats Endpoint [v13]

**URL:** `https://dlyh-telemetry.runeduvall.workers.dev/stats`

**Returns:**
- Total games played
- Wins/losses/abandons with percentages
- Average turns per game
- Difficulty distribution
- Player name leaderboard (top 10 by games played)

---

## New Systems (December 20, 2025) [v14]

### Main Menu Trivia Display

**Location:** `MainMenuController.cs`

**Features:**
- 25 historical facts about guillotines and beheadings
- Rotating display with fade in/out transitions
- Configurable timing (default: 5 second display, 0.5 second fade)
- Starts at random fact, cycles sequentially
- Stops automatically when leaving main menu

**Trivia Facts Include:**
- Historical execution dates and methods
- Famous people who were guillotined
- Technical details about the guillotine
- Lesser-known facts about beheadings

**Unity Setup Required:**
1. Create TextMeshProUGUI below title in MainMenuContainer
2. Assign to `_triviaText` slot on MainMenuController
3. Style as desired (smaller/italic font recommended)

**Serialized Fields:**
- `_triviaText` - TextMeshProUGUI reference
- `_triviaDisplayDuration` - How long each fact shows (default 5f)
- `_triviaFadeDuration` - Fade in/out duration (default 0.5f)

---

## New Systems (December 21, 2025) [NEW v15]

### 3-Part Final Execution Audio Sequence

**Location:** `GuillotineAudioManager.cs`, `GuillotineDisplay.cs`

**Audio Slots (GuillotineAudioManager):**
- `_finalGuillotineRaise` - Final blade raise sound (replaces old chop sounds)
- `_finalHookUnlock` - Hook unlock/release sound
- `_finalGuillotineChop` - Final guillotine chop sound

**Volume Settings:**
- `_finalRaiseVolume` - Volume for final raise (default 0.9)
- `_hookUnlockVolume` - Volume for hook unlock (default 0.85)
- `_finalChopVolume` - Volume for final chop (default 1.0)

**Static Methods:**
- `GuillotineAudioManager.FinalRaise()` - Play final raise sound
- `GuillotineAudioManager.HookUnlock()` - Play hook unlock sound
- `GuillotineAudioManager.FinalChop()` - Play final chop sound

**Timing Settings (GuillotineDisplay):**
- `_finalRaiseDuration` - Duration for blade to rise to top (default 0.5s)
- `_pauseBeforeUnlock` - Pause after raise before hook unlock (default 0.3s)
- `_pauseAfterUnlock` - Pause after unlock before blade drops (default 0.4s)

**Animation Flow:**
1. Play `FinalRaise()` sound
2. Animate blade to top (`_bladeEndY`) over `_finalRaiseDuration`
3. Pause for `_pauseBeforeUnlock`
4. Play `HookUnlock()` sound
5. Pause for `_pauseAfterUnlock`
6. Play `FinalChop()` sound
7. Animate blade drop to bottom (`_bladeStartY`)
8. Continue with head shake and fall into basket

**Used By:**
- `AnimateGameOver()` - When player reaches miss limit
- `AnimateDefeatByWordsFound()` - When opponent finds all words

---

## Phase 4 Polish TODO Items

| Item | Category | Status |
|------|----------|--------|
| Gameplay layout restructure | Layout Fix | COMPLETE |
| Guillotine sprite system | Visual Polish | COMPLETE |
| Guillotine audio (blade raise, chop, head fall) | Audio | COMPLETE |
| Extra turn system (word completion rewards) | Gameplay | COMPLETE |
| Background music (MusicManager) | Audio | COMPLETE [v12] |
| Dynamic music tempo | Audio | COMPLETE [v12] |
| Mute buttons during gameplay | UX | COMPLETE [v12] |
| Telemetry Dashboard (Editor window) | Development | COMPLETE [v13] |
| Enhanced telemetry (player name, game abandon) | Development | COMPLETE [v13] |
| MessagePopup positioning (bottom of screen) | UX | COMPLETE [v14] |
| Main menu trivia display | UX | COMPLETE [v14] |
| Head face expressions | Visual Polish | COMPLETE [v15] |
| Final execution audio sequence (3-part) | Audio | COMPLETE [NEW v15] |
| DOTween animations (reveals, transitions, feedback) | Visual Polish | TODO |
| Feel effects (screen shake, juice) | Visual Polish | TODO |
| Win/Loss tracker vs AI (session stats) | UX | TODO |
| Medieval/carnival themed monospace font | Theme | TODO |
| UI skinning (medieval carnival theme) | Theme | TODO |
| Character avatars | Theme | TODO |
| Background art | Theme | TODO |

---

## Cloudflare Deployment History

| Build | Date | Changes |
|-------|------|---------|
| 1 | Dec 16, 2025 | Initial playtest build |
| 2 | Dec 18, 2025 | Background music, mute buttons, dynamic tempo |
| 3 | Dec 19, 2025 | Enhanced telemetry (player name, game abandon), /stats endpoint |

**URL:** `dlyh.pages.dev`

---

## Playtest Feedback (December 2025)

### Local Playtest - Neighbor (Dec 16, 2025)

**Tester:** Neighbor (4 games, won all 4)

**No Issues Found:** Gameplay worked without bugs.

**Suggestions:**
1. **Mute buttons during gameplay** - COMPLETE [v12]
2. **Background music** - COMPLETE [v12]
3. **Dynamic music tempo** - COMPLETE [v12]
4. **Win/Loss tracker** - TODO
5. **Popup tutorial** - COMPLETE (Help Overlay)

**Bug Noted:**
- Settings panel sliders showed 80%+ instead of 50% default - FIXED (before playtest build)

### Remote Playtest Feedback (Dec 19, 2025)

**Bug Reported:**
- Game over popup appeared in center covering guillotine animation
- Player had dragged popup during gameplay but game over reset to center
- Popup also appeared partially off bottom of screen initially

**Fix:**
- MessagePopup now defaults to bottom of screen
- Position memory system preserves player's dragged position
- Position resets on new game start

---

## Gameplay Layout Restructure Design

### Current Problems (FIXED Dec 17)
1. ~~ButtonBar is inside Player2Section (band-aid placement)~~ - FIXED: ButtonBarStrip created above GameplayContainer
2. ~~Player1Info and Player2Info layouts are asymmetric~~ - FIXED: Both now have matching MinWidth 180
3. ~~Guillotines are static placeholder images, too small~~ - FIXED: GuillotineAssembly hierarchy built, GuillotineDisplay.cs complete
4. ~~Center panel guillotines should fill vertical space~~ - No center panel; guillotines stay with player sections for mobile support

### Implementation Progress (Dec 17-21, 2025)

**COMPLETED:**
- ButtonBarStrip created with GameplayRoot wrapper (VerticalLayoutGroup)
- Buttons moved from Player2Info to ButtonBarStrip
- Player1Info and Player2Info symmetry fixed (matching LayoutElement settings)
- Player1GuillotineAssembly and Player2GuillotineAssembly hierarchy built with UI rectangles
- GuillotineDisplay.cs script created with blade/head animations
- GuillotineAudioManager.cs for guillotine sounds
- HeadFaceController.cs for facial expressions
- Extra turn system for word completion rewards
- MusicManager.cs for background music with shuffle/crossfade [v12]
- Mute buttons wired to UIAudioManager and MusicManager [v12]
- MessagePopup positioned at bottom of screen [v14]
- Main menu trivia display [v14]
- Head face sprites assigned in Unity [v15]
- 3-part final execution audio sequence [v15]

### Target Layout Structure

```
+------------------------------------------------------------------+
| [MainMenu] [Help] [MuteSFX] [MuteMusic]        <- ButtonBar Strip |
+------------------------------------------------------------------+
|  PLAYER1 SECTION  |  CENTER PANEL  |  PLAYER2 SECTION            |
|                   |                |                              |
|  Word Patterns    |  [Guillotine1] |  Word Patterns               |
|  Letter Tracker   |  Miss Counter  |  Letter Tracker              |
|  Grid             |  Guessed Words |  Grid                        |
|                   |                |                              |
|                   |  [Guillotine2] |                              |
|                   |  Miss Counter  |                              |
|                   |  Guessed Words |                              |
+------------------------------------------------------------------+
```

### Guillotine Sprite System

**Component Sprites:**
1. **Frame** - Three rectangles forming the structure
2. **Hash marks** - Variable tick marks on sides (based on miss limit per game)
3. **Head platform** - Single rectangle where head sits
4. **Head** - Circle sprite (player's avatar placeholder)
5. **Blade** - Rectangle sprite that animates
6. **Face** - Image overlay for expressions

**Animation Behavior:**
- Blade Y position moves UP along hash marks as miss count increases
- Hash mark count adjusts per game based on calculated miss limit
- HashMarksContainer shortened to leave space at top for final raise [v15]
- On execution: 3-part audio sequence (raise, unlock, chop) then blade drops [v15]
- Head shakes and falls on game over
- Face changes expression based on danger levels

---

## Technical Debt and Future Considerations

### Known Technical Debt
- Some scripts exceed 1000 lines (candidates for further extraction)
- Event subscription cleanup could be more consistent
- Some magic numbers remain in UI positioning

### Future Considerations
- Consider pooling for grid cells if performance issues arise
- Word bank could support multiple languages
- Replay system would need game state serialization

---

## Phase 5 Feature: Word Row Cell-Based Display

### Design Decision

**Current System:** WordPatternRow uses a TMP_Text field displaying letters/underscores as a formatted string (e.g., "C A T" or "_ _ _").

**Proposed Change:** Replace text field with individual letter cells (non-clickable buttons or UI panels).

### Rationale

1. **Layout Consistency:** All word rows have identical structure regardless of word length
2. **Flexible Word Lengths:** Players no longer locked into fixed 3/4/5/6 letter slots
3. **Visual Consistency:** Matches grid cell aesthetic
4. **Animation Ready:** Individual cells are easier DOTween targets for reveal effects
5. **Simplified Logic:** No string formatting/parsing needed

### Implementation Approach

```
WordPatternRow
+-- RowNumber (Text)
+-- LetterCellsContainer (HorizontalLayoutGroup)
|   +-- LetterCell[0] (Button or Panel)
|   +-- LetterCell[1]
|   +-- LetterCell[2]
|   +-- ... (8 cells total for max word length)
+-- CompassButton / GuessWordButton
```

**Cell Behavior:**
- Setup mode: Cells fill left-to-right as player types
- Gameplay mode: Shows underscore for unknown, letter when discovered
- Unused cells: Hidden or empty (maintain spacing)
- Max cells: 8 (supports 3-8 letter words)

**Benefits over text field:**
- No layout shifts between 3-letter and 6-letter words
- Cells can have individual backgrounds, colors, animations
- Backspace removes from rightmost filled cell
- Clear visual feedback per letter position

---

**End of Design Decisions Document**

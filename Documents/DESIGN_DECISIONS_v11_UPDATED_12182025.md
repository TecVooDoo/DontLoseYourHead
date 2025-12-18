# Don't Lose Your Head - Design Decisions and Lessons Learned

**Version:** 11.0
**Date Created:** November 22, 2025
**Last Updated:** December 18, 2025
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

### Extra Turn on Word Completion [NEW v11]

**Decision:** Completing a word (revealing all letters) grants an extra turn.

**Rules:**
- Works for letter guesses, coordinate guesses, AND correct word guesses
- Multiple words completed at once queue multiple extra turns (one per word)
- Extra turns processed one at a time (FIFO queue)
- Popup shows combined message: guess result + extra turn notification

**Rationale:** Rewards strategic play. Completing words should feel rewarding and encourage players to think about which guesses might complete multiple words at once.

### Head Face Expressions [NEW v11]

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

### Guillotine Audio Layering [NEW v11]

**Decision:** Blade raise plays two sounds simultaneously (rope stretch + blade movement).

**Rationale:** Creates richer audio experience. Single sounds felt thin; layering adds depth and tension.

### Face Direction from Face's Perspective [NEW v11]

**Decision:** Face sprite naming uses the face's perspective for eye direction (Left = face looking to its left).

**Rationale:** More intuitive for asset organization. When viewing the sprite, "Left" means eyes pointing left.

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

**13. Queue Pattern for Sequential Rewards [NEW v11]**
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

### AI-Specific Insights

**14. Grid Density Affects Strategy**
- High density (>35%): Favor coordinate guessing
- Medium density (12-35%): Balanced approach
- Low density (<12%): Favor letter guessing

**15. Rubber-Banding Needs Bounds Testing**
Test with intentionally poor play to find edge cases. Systems can over-correct without proper bounds.

**16. Adaptive Thresholds**
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
| game_start | Player/opponent grid sizes, word counts, difficulties |
| game_end | Win/loss, miss counts, total turns |
| error | Unity errors/exceptions with stack traces |

**Design Decisions:**
- No individual guess tracking (too noisy for basic playtesting)
- Automatic error capture via `Application.logMessageReceived`
- Queue-based sending to handle network delays
- Volume caching for performance

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

## New Systems (December 18, 2025) [NEW v11]

### Extra Turn System

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

### Guillotine Audio System

**Components:**
- `GuillotineAudioManager.cs` - Singleton for guillotine sounds

**Sound Types:**
| Sound | When Played |
|-------|-------------|
| Rope Stretch + Blade Raise | On each miss (blade rises) |
| Chop Fast | Execution from miss limit |
| Chop Slow | Execution from words found |
| Head Removed | When head falls into basket |

**Design Decisions:**
- Two AudioSources for layered playback
- Fast vs slow chop distinguishes execution types
- Static convenience methods for easy calling from GuillotineDisplay

### Head Face Controller

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

**Unity Setup Required:**
1. Add HeadFaceController component to head area
2. Create child Image for face display
3. Assign Image to `_faceImage` slot
4. Assign face sprites to appropriate slots
5. Reference HeadFaceController in GuillotineDisplay

---

## Phase 4 Polish TODO Items

| Item | Category | Status |
|------|----------|--------|
| Gameplay layout restructure (see design below) | Layout Fix | COMPLETE |
| Guillotine sprite system (see design below) | Visual Polish | COMPLETE |
| Guillotine audio (blade raise, chop, head fall) | Audio | COMPLETE |
| Extra turn system (word completion rewards) | Gameplay | COMPLETE |
| Head face expressions | Visual Polish | IN PROGRESS (sprites needed) |
| DOTween animations (reveals, transitions, feedback) | Visual Polish | TODO |
| Feel effects (screen shake, juice) | Visual Polish | TODO |
| Background music | Audio | TODO |
| Dynamic music tempo (increase near miss limit) | Audio | TODO |
| Mute buttons during gameplay (SFX and Music) | UX | TODO |
| Win/Loss tracker vs AI (session stats) | UX | TODO |
| Medieval/carnival themed monospace font | Theme | TODO |
| UI skinning (medieval carnival theme) | Theme | TODO |
| Character avatars | Theme | TODO |
| Background art | Theme | TODO |

---

## Playtest Feedback (December 2025)

### Local Playtest - Neighbor (Dec 16, 2025)

**Tester:** Neighbor (4 games, won all 4)

**No Issues Found:** Gameplay worked without bugs.

**Suggestions:**
1. **Mute buttons during gameplay** - Wanted quick SFX/Music toggle without going to Main Menu > Settings
2. **Background music** - Would enhance the experience (already planned)
3. **Dynamic music tempo** - Music should increase tempo when player or opponent approaches miss limit
4. **Win/Loss tracker** - Wanted to see total wins against The Executioner during session
5. **Popup tutorial** - COMPLETE (Help Overlay implemented before this playtest)

**Bug Noted:**
- Settings panel sliders showed 80%+ instead of 50% default - FIXED (before playtest build)

---

## Gameplay Layout Restructure Design

### Current Problems (FIXED Dec 17)
1. ~~ButtonBar is inside Player2Section (band-aid placement)~~ - FIXED: ButtonBarStrip created above GameplayContainer
2. ~~Player1Info and Player2Info layouts are asymmetric~~ - FIXED: Both now have matching MinWidth 180
3. ~~Guillotines are static placeholder images, too small~~ - FIXED: GuillotineAssembly hierarchy built, GuillotineDisplay.cs complete
4. ~~Center panel guillotines should fill vertical space~~ - No center panel; guillotines stay with player sections for mobile support

### Implementation Progress (Dec 17-18, 2025)

**COMPLETED:**
- ButtonBarStrip created with GameplayRoot wrapper (VerticalLayoutGroup)
- Buttons moved from Player2Info to ButtonBarStrip
- Player1Info and Player2Info symmetry fixed (matching LayoutElement settings)
- Player1GuillotineAssembly and Player2GuillotineAssembly hierarchy built with UI rectangles
- GuillotineDisplay.cs script created with blade/head animations
- GuillotineAudioManager.cs for guillotine sounds [v11]
- HeadFaceController.cs for facial expressions [v11]
- Extra turn system for word completion rewards [v11]

**IN PROGRESS:**
- Head face sprites need to be assigned in Unity Inspector

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
6. **Face** - Image overlay for expressions [v11]

**Animation Behavior:**
- Blade Y position moves UP along hash marks as miss count increases
- Hash mark count adjusts per game based on calculated miss limit
- On miss limit reached: blade drops with DOTween animation
- Head shakes and falls on game over
- Face changes expression based on danger levels [v11]

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

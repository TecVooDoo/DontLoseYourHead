# Don't Lose Your Head - Design Decisions and Lessons Learned

**Version:** 8.0
**Date Created:** November 22, 2025
**Last Updated:** December 16, 2025
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

### AI-Specific Insights

**13. Grid Density Affects Strategy**
- High density (>35%): Favor coordinate guessing
- Medium density (12-35%): Balanced approach
- Low density (<12%): Favor letter guessing

**14. Rubber-Banding Needs Bounds Testing**
Test with intentionally poor play to find edge cases. Systems can over-correct without proper bounds.

**15. Adaptive Thresholds**
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

**End of Design Decisions Document**

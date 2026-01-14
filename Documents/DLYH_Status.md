# Don't Lose Your Head - Project Status

**Project:** Don't Lose Your Head (DLYH)
**Developer:** TecVooDoo LLC / Rune (Stephen Brandon)
**Platform:** Unity 6.3 (6000.0.38f1)
**Source:** `E:\Unity\DontLoseYourHead`
**Document Version:** 41
**Last Updated:** January 13, 2026

---

## Quick Context

**What is this game?** A competitive word game combining Hangman's letter-guessing with Battleship's grid-based hidden information. Players place hidden words on grids and take turns guessing letters or coordinates to find opponent's words before the guillotine blade falls.

**Key Innovation:** Asymmetric difficulty - mixed-skill players compete fairly with different grid sizes, word counts, and difficulty settings.

**Current Phase:** Phase D IN PROGRESS - Defense view and turn switching implemented!

**Last Session (Jan 13, 2026):** Thirty-first session - **Defense View & Turn Switching!** Implemented dual-view tab system: Attack tab (opponent's fog-of-war grid, player's guesses, player's keyboard state) vs Defend tab (player's visible grid, opponent's guesses, opponent's keyboard state). Auto-switches tabs based on turn (Attack during player turn, Defend during opponent turn). Manual tab switching blocked during opponent's turn. AI guesses now update defense grid visually (hit=player color, miss=red) and opponent keyboard state.

**TODO for next session:**

**Phase C Setup Polish:** COMPLETE
- ~~Add visual feedback for invalid words (red highlight, shake)~~ DONE
- ~~Connect physical keyboard input for word entry~~ DONE
- ~~Word list preview dropdown (autocomplete as user types)~~ DONE
- ~~Test crossword-style overlapping words (shared letters)~~ DONE (AI + Player both support)
- ~~Green for valid words in word rows~~ DONE
- ~~Green for placed letters on grid during setup~~ DONE
- ~~Backspace clears grid placement~~ DONE
- ~~Random Words enables placement buttons~~ DONE
- ~~Clear button resets validity state~~ DONE

**Phase D:** Gameplay UI conversion - Testing and integration in progress
- [x] Create `Gameplay.uxml` - Main gameplay layout with tabs, grid area, word rows, keyboard
- [x] Create `Gameplay.uss` - Styles with responsive placeholders
- [x] Create `GuillotineOverlay.uxml` - Modal for viewing guillotines
- [x] Create `GuillotineOverlay.uss` - Overlay styles with blade positions
- [x] Create `GameplayScreenManager.cs` - Tab switching, keyboard, status, guessed words
- [x] Create `GuillotineOverlayManager.cs` - Overlay controller with animations
- [x] Wire to UIFlowController (TransitionToGameplay method)
- [x] Fix header bar/hamburger button layout (CSS updates)
- [x] Fix guillotine overlay positioning (absolute positioning, pickingMode)
- [x] Implement separate guessed words lists (player vs opponent)
- [x] Wire QWERTY toggle to update gameplay keyboard
- [x] Fix overlay click-through issues (pickingMode.Ignore when hidden)
- [x] Fix gameplay hamburger button click area (renamed class to avoid CSS conflict)
- [x] Fix miss count sync between cards and guillotine overlay
- [x] Redesign guillotine visual (blade with holder, oval lunette with divider, proper z-order)
- [x] Fix miss limit calculation bug (was using wrong formula, now uses DifficultyCalculator)
- [x] Redesign guillotine to 5-stage system (no more per-miss hash marks)
- [x] Fix grid cell state logic (coordinate vs letter guessing)
- [x] Fix keyboard letter state colors (red miss, yellow found, player color hit)
- [x] Implement "WORDS TO FIND" section with underscores and letter reveal
- [x] Implement turn switching after guesses
- [x] Add AI opponent turn logic
- [x] Add defense view (player's grid with opponent's guesses)
- [x] Auto-switch tabs based on turn
- [x] Separate keyboard states for Attack vs Defend tabs
- [x] **FIXED:** Defense word rows buttons hidden (no interaction on Defend tab)
- [x] **FIXED:** Keyboard clicks disabled on Defend tab (view-only)
- [x] **FIXED:** Keyboard letters dimmed/greyed on Defend tab initially
- [ ] **NEEDS TESTING:** Guillotine 5-stage visual and blade positions
- [ ] **NEEDS TESTING:** Defense view switching and AI guess visuals
- [ ] Add extra turn on word completion (player or AI guessing full word)
- [ ] Add win/lose detection

**Phase E (Networking - batch together, requires C: drive copy):**
- Wire up Join Code submit to actual networking code
- Receive game settings (grid size, word count) from host
- Transition to placement panel with received settings

---

## UX Design: Navigation & Settings (IMPLEMENTED Jan 10, 2026)

**Design Philosophy:** Fewer screens, faster access, consistent patterns.

### Main Menu Layout
```
DON'T LOSE YOUR HEAD
"A game of words and wits"

[Play Solo]
[Play Online]
[Join Game]
[How to Play]
[Feedback]              ← Opens modal overlay

─────────────────────────
🔊 [━━━━●━━━━━] 50%     ← SFX slider (inline)
🎵 [━━━━●━━━━━] 50%     ← Music slider (inline)
☑ QWERTY Keyboard       ← Checkbox (inline)

"The guillotine was used in France until 1977."  ← Trivia marquee
```

**Key decisions:**
- Settings inline on main menu (no separate screen)
- Default volumes: 50% (persist to PlayerPrefs)
- Remove Settings button, controls always visible

### Feedback Modal (Reusable)
```
┌─────────────────────────────┐
│      [Title]                │  ← "Share Feedback" / "Victory!" / "Defeated"
│                             │
│  ┌───────────────────────┐  │
│  │  (text area)          │  │
│  └───────────────────────┘  │
│                             │
│    [Submit]    [Cancel]     │
└─────────────────────────────┘
```
- From main menu: Feedback button opens with "Share Feedback"
- After game ends: Auto-opens with win/lose title
- Integrates with `PlaytestTelemetry.Feedback()`

### Hamburger Menu (☰) for Setup/Gameplay
Top-left corner during setup wizard and gameplay:
```
☰ ←─── Always visible

Opens overlay:
┌────────────────┐
│ Main Menu      │  ← Returns to main menu
│ Settings       │  ← Shows SFX/Music/QWERTY inline
│ Resume         │  ← Closes overlay (gameplay only)
└────────────────┘
```

### Continue Game Flow
- If game in progress and user goes to Main Menu → add [Continue Game] button
- Continue Game returns to active gameplay state

### Files Created (Jan 10, 2026)
- `FeedbackModal.uxml` + `FeedbackModal.uss` - Modal overlay component (DONE)
- `HamburgerMenu.uxml` + `HamburgerMenu.uss` - Navigation overlay (DONE)
- Updated `MainMenu.uxml` - Added inline settings, feedback button, trivia label (DONE)
- Updated `MainMenu.uss` - Slider and checkbox styles (DONE)
- Updated `UIFlowController.cs` - Wired hamburger menu, feedback modal, settings persistence (DONE)

---

## Phase D: Gameplay UI Redesign (Jan 11, 2026)

**Status:** CORE IMPLEMENTATION - Files created, testing next

**Problem Analysis:**
The legacy uGUI gameplay screen shows both grids side-by-side with guillotines, but this creates several UX issues:
1. **Small touch targets** - Letter tracker buttons are too small on mobile
2. **Cramped layout** - Both grids + guillotines + word rows competing for space
3. **Cognitive overload** - Too much information visible at once
4. **Guillotines underutilized** - Always visible but small, not dramatic

**Design Solution: Focused Single-Grid View with Tab Switching**

Instead of showing both grids simultaneously, use a focused view with easy Attack/Defend toggle.

### Layout Structure
```
+-------------------------------------------------------------+
|  (hamburger)              YOUR TURN                          |
+-------------------------------------------------------------+
|  +-------------------------+   +-------------------------+   |
|  | ATTACK: EXECUTIONER    |   | DEFEND: You             |   |
|  | 12x12 - 3 words        |   | 6x6 - 4 words           |   |
|  | [coffin] 5/18 ████░░░░ |   | [coffin] 3/24 █░░░░░░░░ |   |
|  +-------------------------+   +-------------------------+   |
|           ^ ACTIVE TAB                                       |
|                                                              |
|         +-------------------------------+                    |
|         |                               |                    |
|         |     OPPONENT'S GRID           |                    |
|         |     (single large grid)       |                    |
|         |                               |                    |
|         +-------------------------------+                    |
|                                                              |
|  WORDS TO FIND:                                              |
|  +-------------+-------------+------------------+            |
|  | 1. _ _ _    | 2. C _ _ _  | 3. _ E _ _ _     |            |
|  |   [GUESS]   |   [GUESS]   |   [GUESS]        |            |
|  +-------------+-------------+------------------+            |
|                                                              |
|  +-----------------------------------------------------+    |
|  |  A   B   C   D   E   F   G   H   I                  |    |
|  |  J   K   L   M   N   O   P   Q   R                  |    |
|  |  S   T   U   V   W   X   Y   Z                      |    |
|  +-----------------------------------------------------+    |
|                                                              |
|  [Guessed Words: 5 v]                                        |
+-------------------------------------------------------------+
|  "EXECUTIONER guessed 'R' - Hit on your board!"             |
+-------------------------------------------------------------+
```

### Key UX Improvements

1. **Tab-Based Grid Switching**
   - ATTACK tab: Shows opponent's grid (where you guess)
   - DEFEND tab: Shows your grid (see what opponent found)
   - Each tab shows grid stats: size, word count, miss progress
   - Default to opponent's grid on your turn

2. **Event-Based Guillotines (Not Always Visible)**
   - Miss counter buttons are tappable -> opens guillotine overlay
   - On miss: Quick screen flash, progress bar animates
   - On critical miss (80%+): Auto-show guillotine overlay briefly
   - On game end: Full dramatic sequence with both guillotines

3. **Guillotine Overlay Modal**
   - Both guillotines side-by-side
   - Blade positions reflect current miss counts
   - Head expressions change based on danger level
   - Flavor text: "Getting warm...", "In danger!", etc.
   - Tap outside or X to dismiss

4. **Larger Letter Keyboard**
   - Reuse setup keyboard layout (3 rows, 44px+ buttons)
   - Row 1: A B C D E F G H I
   - Row 2: J K L M N O P Q R
   - Row 3: S T U V W X Y Z
   - Or QWERTY layout based on user preference
   - Color states: Default -> Hit (player color) -> Miss (red strikethrough)

5. **Asymmetric Difficulty Display**
   - Both tabs show grid size + word count
   - Players clearly see the difficulty imbalance
   - Example: Your 6x6/4words vs Their 12x12/3words

6. **Collapsible Guessed Words List**
   - Button shows count: "[Guessed Words: 5]"
   - Tap to expand into overlay/panel
   - Shows both players' word guesses with hit/miss status

### Game End Sequence

```
Win/Loss triggered ->
  1. Grids fade to 50% opacity (0.5s)
  2. "GAME OVER" banner drops down
  3. Guillotine overlay fades in (both guillotines)
  4. Executioner character appears center (future enhancement)
  5. Loser's blade drops with full animation + sound
  6. Head falls into basket
  7. 2 second hold
  8. Results panel slides up with stats + Play Again button
```

### Responsive Behavior

**Landscape (Wide):**
- Could show both grids side-by-side on tablet/desktop
- Letter keyboard spans bottom
- Similar to legacy layout but with larger touch targets

**Portrait (Tall):**
- Single grid view with tab toggle (as designed above)
- Letter keyboard is 3 rows
- Everything stacks vertically

### Files Created (Jan 11, 2026)

| File | Purpose | Status |
|------|---------|--------|
| `Gameplay.uxml` | Main gameplay screen layout | DONE |
| `Gameplay.uss` | Gameplay styles + responsive breakpoints | DONE |
| `GuillotineOverlay.uxml` | Modal guillotine view | DONE |
| `GuillotineOverlay.uss` | Overlay styles with blade positions | DONE |
| `GameplayScreenManager.cs` | Tab switching, keyboard, status, guessed words | DONE |
| `GuillotineOverlayManager.cs` | Overlay controller with game over states | DONE |
| `UIFlowController.cs` | Updated with gameplay screen creation and wiring | DONE |

### Integration Points

- Wire to existing `GameplayUIController` for guess processing
- Wire to existing `GuessProcessingManager` for hit/miss logic
- Wire to existing `GameplayStateTracker` for state management
- Wire to existing `OpponentTurnManager` for AI turns
- Reuse `TableView` and `TableModel` for grid rendering
- Reuse `WordRowView` for word progress display

---

## Development Priorities (Ordered)

1. **SOLID principles first** - single responsibility, open/closed, Liskov substitution, interface segregation, dependency inversion
2. **Memory efficiency second** - no per-frame allocations, no per-frame LINQ, object pooling where appropriate
3. **Clean code third** - readable, maintainable, consistent formatting
4. **Self-documenting code fourth** - clear naming over comments; if code needs a comment, consider refactoring first
5. **Platform best practices fifth** - Unity > C#, Cloudflare/Supabase > HTML/JS (platform-specific wins over language-generic)

---

## Active TODO

### Environment Fix (Before Coding)
- [x] Fix Claude Code tool approvals - updated E:\Unity\.claude\settings.local.json with broad permissions
- [x] Fix GitHub account default - deleted Rune1172 and stephenmbrandon credentials (TecVooDoo remains)

### Immediate (Phase 0: Refactor)
- [x] Extract GameplayUIController panel configuration -> GameplayPanelConfigurator (~140 lines)
- [x] Extract GameplayUIController UI updates -> GameplayUIUpdater (~290 lines)
- [x] Extract GameplayUIController popup messages -> PopupMessageController (~250 lines)
- [x] Extract GameplayUIController opponent system -> OpponentTurnManager (~380 lines)
- [x] Remove duplicate Guillotine/MissCounter code (delegate to GameplayUIUpdater)
- [x] Extract GuessProcessingManager (~365 lines) - guess processing for player and opponent
- [x] Remove duplicate popup code (delegate to PopupMessageController)
- [x] GameplayUIController at ~1321 lines (from ~2619, 50% reduction) - within acceptable range
- [x] Housekeeping cleanup: SetupSettingsPanel (~1051 lines), PlayerGridPanel (~1060 lines) - unused code removed
- [x] Verify game still works after cleanup - tested Jan 7, 2026
- [ ] Document new interfaces/controllers in Architecture section
- [ ] Standardize namespace convention (choose TecVooDoo.DontLoseYourHead.* OR DLYH.*)
- [ ] Verify game still works after each extraction (test in NewPlayTesting.unity)

### Current (Phase 0.5: Multiplayer Verification) - COMPLETE
- [x] Wire IOpponent interface to GameplayUIController (SetOpponent method added)
- [x] Create NetworkingTestController.cs for debug Host/Join UI
- [x] Create NetworkingTest.unity scene (duplicate of NewPlayTesting)
- [x] Delete abandoned NewUIDesign.unity and LetterCellUI/WordPatternRowUI prefabs
- [x] Add runtime OnGUI debug panel for Virtual Player testing
- [x] Fix JoinGame schema mismatch (remove player_name/player_color)
- [x] Fix created_by FK error (pass null for anonymous users)
- [x] Test game creation - works (game code generated, host polling)
- [x] **RESOLVED:** session_players.player_id FK issue
  - Created PlayerService.cs to create player records in `players` table (no auth required)
  - Matches DAB's approach: display_name + is_ai=false, auth_id is nullable
  - Different player names = different player records (for testing on same machine)
- [x] Fix GetPlayerCount query (session_players has no `id` column, use `player_number`)
- [x] Test Host/Join with two Unity instances - **SUCCESS!**
  - Both instances create unique player records
  - Host creates game, joins as player 1
  - Client joins with game code as player 2
  - Both detect 2 players connected
- [x] Document networking issues found (see Lessons Learned)
- [ ] Delete test scene after UI Toolkit complete (keep the wiring code)
- [ ] Update NetworkGameManager to use PlayerService instead of AuthService (Phase 1)

### Then (Phases A-F: UI Toolkit Implementation)
- [x] Phase A: Table data model foundation (no visual changes) - COMPLETE
- [x] Phase B: UI Toolkit table renderer MVP - COMPLETE
- [ ] Phase C: Setup wizard + placement using table UI - IN PROGRESS
  - [x] Setup wizard with progressive card disclosure
  - [x] Main menu working
  - [x] Word rows architecture redesigned (separate from grid)
  - [x] Word entry with autocomplete (WordSuggestionDropdown component)
  - [x] Grid placement (PlacementAdapter + TablePlacementController)
  - [x] Visual feedback polish (green valid/placed, red invalid, backspace clears grid)
- [ ] Phase D: Gameplay UI conversion
- [ ] Phase E: Networking integration (wire existing code)
- [ ] Phase F: Refactor and cleanup (remove legacy uGUI)

---

## What Works (Completed Features)

**Core Mechanics:**
- Grid placement and word entry
- Letter, coordinate, and word guessing
- Miss limit calculation formula
- Win and loss condition detection
- Extra turn on word completion (queued if multiple)

**AI Opponent ("The Executioner"):**
- Adaptive difficulty with rubber-banding
- Strategy switching (letter, coordinate, word guesses)
- Memory-based decision making
- Variable think times (0.8-2.5s)
- Grid density analysis for strategy selection

**Audio:**
- Music playback with shuffle (Fisher-Yates) and crossfade (1.5s)
- SFX system with mute controls
- Dynamic tempo changes under danger (1.08x at 80%, 1.12x at 95%)
- Guillotine execution sound sequence (3-part)

**Telemetry:**
- Cloudflare workers endpoint for event capture
- Editor-accessible analytics dashboard
- Session, game, guess, feedback, and error tracking

**Networking (Phase 0.5 Complete):**
- IOpponent interface for opponent abstraction
- LocalAIOpponent wrapping ExecutionerAI
- RemotePlayerOpponent for network play
- OpponentFactory for creating opponents
- Supabase services (Auth, GameSession, Realtime, StateSynchronizer)
- PlayerService for creating player records without auth (NEW)
- Lobby and WaitingRoom UI controllers (not integrated)
- Database operations verified: player creation, game creation, game joining

**Polish:**
- Help overlay and tooltips
- Feedback panel
- Profanity and drug word filtering
- Head face expressions
- Version display

---

## Known Issues

**Architecture:**
- GameplayUIController at ~1321 lines (reduced 50% from ~2619, within acceptable range)
- SetupSettingsPanel at ~1051 lines (reduced from ~1286, unused code removed)
- PlayerGridPanel at ~1060 lines (reduced from ~1067, unused code removed)
- Inconsistent namespace convention (TecVooDoo.DontLoseYourHead.* vs DLYH.*)

**UI (To Be Replaced):**
- Legacy WordPatternRow uses text field (migrating to table-based system)
- uGUI cell vertical stretching bug in LetterCellUI/WordPatternRowUI prefabs
- Scene files can get accidentally modified - check git diff before commits

**Networking:**
- IOpponent interface wired to GameplayUIController (SetOpponent method)
- Phase 0.5 verification COMPLETE - database operations working
- PlayerService.cs creates player records without Supabase Auth (matches DAB)
- NetworkGameManager still uses AuthService (needs update for Phase 1)
- Full multiplayer gameplay not yet tested (setup exchange, turns, state sync)

**Audio:**
- Music crossfading/switching too frequently (should only switch at end of track)

**Abandoned Work (Pivot to UI Toolkit):**
- LetterCellUI.cs, WordPatternRowUI.cs, WordPatternPanelUI.cs - uGUI approach abandoned
- NewUI/Prefabs/LetterCellUI.prefab, WordPatternRowUI.prefab - to be deleted after UI Toolkit complete
- NewUIDesign.unity scene - to be deleted when recoding starts
- WordPlacementController.cs (NewUI) - to be replaced by PlacementAdapter + existing CoordinatePlacementController

---

## Implementation Plan

### Phase 0: Refactor Large Scripts
Extract oversized MonoBehaviours into smaller, focused controllers and services. Goal is maintainability and Claude Code compatibility (<1000 lines ideal, <800 preferred but not mandatory). Avoid refactoring for its own sake - extractions should reduce complexity, not add indirection.

**Status:**
- GameplayUIController.cs: ~1321 lines (from ~2619, 50% reduction) - 7 extractions completed
- SetupSettingsPanel.cs: ~1051 lines (from ~1286, unused code removed)
- PlayerGridPanel.cs: ~1060 lines (from ~1067, unused code removed)
- WordPatternRow.cs: ~1198 lines (at upper edge of range, no unused code found)

### Phase 0.5: Multiplayer Verification
Create minimal test scene to verify networking works before building UI around it. This is throwaway work - just enough to validate the foundation.

**Verify:**
- Two instances can connect via game code
- Setup data exchanges correctly
- Turn events fire and sync
- State remains consistent

### Phase A: Foundation (No UI Changes)
- Implement TableModel, TableCell, TableCellKind, TableCellState, CellOwner
- Implement TableLayout and TableRegion for mapping
- Implement ColorRules service
- Unit test the model (no Unity UI dependencies)

### Phase B: UI Toolkit Table MVP
- Build table renderer using UI Toolkit (UXML/USS)
- Generate all cells once (non-virtualized)
- Update visuals via state changes only
- No per-frame allocations

### Phase C: Setup Wizard + Placement
- Replace monolithic setup screen with guided wizard
- Implement placement logic using table UI
- Preserve existing placement rules and validation

### Phase D: Gameplay UI Conversion
- Convert gameplay grids to table UI
- Wire table interactions to existing gameplay systems
- Preserve AI, audio, and telemetry behavior

### Phase E: Networking Integration
- Wire IOpponent to gameplay flow (already scaffolded in Phase 0.5)
- Implement phantom-AI fallback (5 second PVP timeout)
- Ensure UI supports both PVP and Executioner modes

### Phase F: Cleanup
- Remove legacy uGUI components
- Delete abandoned prefabs and scripts
- Final refactor pass
- Validate memory usage and allocations

---

## Architecture

### Word Rows Architecture (NEW - Phase C Redesign)

Word rows are now a **separate container** above the grid table. This allows:
- Variable word lengths (3, 4, 5, 6 letters for words 1-4)
- Independent layout from grid columns
- Control buttons per row (placement, clear, or guess)

**Layout Structure:**
```
Word Rows Container (border-aligned with grid below)
├── Row 1: [1.] [_][_][_]           [⊕][✕]  ← 3 letters + controls
├── Row 2: [2.] [_][_][_][_]        [⊕][✕]  ← 4 letters + controls
├── Row 3: [3.] [_][_][_][_][_]     [⊕][✕]  ← 5 letters + controls
└── Row 4: [4.] [_][_][_][_][_][_]  [⊕][✕]  ← 6 letters + controls
────────────────────────────────────────────
Grid Table Container
├── [spacer] [A] [B] [C] [D] [E] [F]  ← Column headers
├── [1]      [ ] [ ] [ ] [ ] [ ] [ ]  ← Grid row 1
├── [2]      [ ] [ ] [ ] [ ] [ ] [ ]  ← Grid row 2
└── ...
```

**Control Buttons:**
- During Setup: [⊕] placement button, [✕] clear button
- During Gameplay: [GUESS] button replaces both

**Files:**
- `WordRowView.cs` - Renders single word row with variable length
- `WordRowsContainer.cs` - Manages all word rows, events
- `TableLayout.cs` - Grid-only (no WordRowsRegion)
- `TableModel.cs` - Grid-only (no word slot methods)

### Setup Visual Feedback (NEW - Jan 11, 2026)

**Color Rules During Setup:**
- **Word Rows:** Green when word is valid (ClassValid USS class)
- **Grid Cells:** Green when letters are placed (ColorRules.GetSetupPlacedColor())
- **Placement Preview:** Green for anchor/path cells (ColorRules.GetPlacementColor with isSetupMode=true)
- **Invalid Words:** Red highlight + shake animation (ClassInvalid USS class + DOTween)

**Color Rules During Gameplay:**
- **Word Rows:** Player color when placed
- **Grid Cells:** Player color based on CellOwner
- **Placement Preview:** Player color

**Key Methods:**
- `WordRowView.SetWordValid(bool)` - Controls green/red styling
- `WordRowView.SetPlaced(bool)` - During setup keeps green, during gameplay uses player color
- `TableView.SetSetupMode(bool)` - Switches between setup (green) and gameplay (player color)
- `ColorRules.GetSetupPlacedColor()` - Returns SystemGreen for placed cells during setup

### Namespaces

| Namespace | Scripts | Purpose |
|-----------|---------|---------|
| `TecVooDoo.DontLoseYourHead.UI` | 28 | Main UI scripts |
| `TecVooDoo.DontLoseYourHead.UI.Utilities` | 1 | RowDisplayBuilder |
| `TecVooDoo.DontLoseYourHead.Core` | 4 | Game state, difficulty |
| `DLYH.UI` | 6 | Main menu, settings, help, tooltips |
| `DLYH.AI.Config` | 1 | AI configuration |
| `DLYH.AI.Core` | 4 | AI controllers |
| `DLYH.AI.Data` | 2 | AI data utilities |
| `DLYH.AI.Strategies` | 4 | AI guess strategies |
| `DLYH.Audio` | 5 | UI, guillotine, music audio |
| `DLYH.Telemetry` | 1 | Playtest analytics |
| `DLYH.Networking` | 4 | Opponent abstraction, factory |
| `DLYH.Networking.Services` | 8 | Supabase, auth, player, realtime |
| `DLYH.Networking.UI` | 3 | Lobby, waiting room |
| `DLYH.Editor` | 1 | Telemetry Dashboard |

### Key Types by Namespace

**TecVooDoo.DontLoseYourHead.UI:**
- GameplayUIController, PlayerGridPanel, SetupSettingsPanel, WordPatternRow
- GridCellUI, LetterButton, GuillotineDisplay, MessagePopup
- Controllers/: GameplayPanelConfigurator, GameplayUIUpdater, PopupMessageController, OpponentTurnManager, GuessProcessingManager
- Services/: GuessProcessor, GameplayStateTracker, WinConditionChecker, WordGuessModeController
- Data classes: SetupData (in GameplayPanelConfigurator), WordPlacementData (in GuessProcessor)
- Enums: GameOverReason (in PopupMessageController), GuessResult (in GuessProcessingManager), WordGuessResult

**TecVooDoo.DontLoseYourHead.Core:**
- WordListSO, DifficultyCalculator, DifficultySetting, WordCountOption

**DLYH.AI.Core:**
- ExecutionerAI, AISetupManager, DifficultyAdapter

**DLYH.AI.Config:**
- ExecutionerConfigSO

**DLYH.AI.Strategies:**
- IGuessStrategy, AIGameState, LetterFrequencyStrategy, CoordinateStrategy, WordGuessStrategy

**DLYH.TableUI (NEW - Phase A/B/C):**
- TableCellKind, TableCellState, CellOwner (enums)
- TableCell (struct), TableRegion (struct)
- TableLayout, TableModel, ColorRules, TableView, TableViewTest
- WordRowView, WordRowsContainer (NEW - Phase C)
- UIFlowController, SetupWizardUIManager (nested class)

**DLYH.Networking:**
- IOpponent, LocalAIOpponent, RemotePlayerOpponent, OpponentFactory
- PlayerSetupData

**DLYH.Networking.Services:**
- SupabaseClient, SupabaseConfig, SupabaseResponse
- AuthService, GameSessionService, PlayerService (NEW)
- GameSubscription, GameStateSynchronizer
- PlayerRecord, GameSession, SessionPlayer, DLYHGameState

### Key Folders

```
Assets/DLYH/
  Scripts/
    AI/
      Config/     - ExecutionerConfigSO.cs (DLYH.AI.Config)
      Core/       - ExecutionerAI, AISetupManager, DifficultyAdapter, MemoryManager (DLYH.AI.Core)
      Data/       - GridAnalyzer, LetterFrequency (DLYH.AI.Data)
      Strategies/ - IGuessStrategy, Letter/Coordinate/WordGuessStrategy (DLYH.AI.Strategies)
    Audio/        - UIAudioManager, MusicManager, GuillotineAudioManager (DLYH.Audio)
    Core/         - WordListSO, DifficultyCalculator, DifficultySetting (TecVooDoo.DontLoseYourHead.Core)
    Editor/       - TelemetryDashboard (DLYH.Editor)
    Networking/   - IOpponent, LocalAIOpponent, RemotePlayerOpponent, OpponentFactory (DLYH.Networking)
      Services/   - Supabase, Auth, Realtime, GameSession (DLYH.Networking.Services)
      UI/         - Lobby, WaitingRoom, ConnectionStatus (DLYH.Networking.UI)
    Telemetry/    - PlaytestTelemetry (DLYH.Telemetry)
    UI/           - Main UI scripts (TecVooDoo.DontLoseYourHead.UI)
      Controllers/ - Extracted controllers: GameplayPanelConfigurator, GameplayUIUpdater,
                    PopupMessageController, OpponentTurnManager, CoordinatePlacementController, etc.
      Interfaces/ - IGridControllers.cs
      Services/   - GuessProcessor, GameplayStateTracker, WinConditionChecker, WordValidationService
      Utilities/  - RowDisplayBuilder (TecVooDoo.DontLoseYourHead.UI.Utilities)
  NewUI/          - UI Toolkit implementation (DLYH.TableUI)
    Scripts/      - TableCellKind, TableCellState, CellOwner, TableCell, TableRegion,
                    TableLayout, TableModel, ColorRules, TableView, TableViewTest,
                    UIFlowController, WordRowView, WordRowsContainer,
                    GameplayScreenManager, GuillotineOverlayManager (NEW - Phase D)
    USS/          - TableView.uss, MainMenu.uss, SetupWizard.uss, FeedbackModal.uss, HamburgerMenu.uss, Gameplay.uss, GuillotineOverlay.uss
    UXML/         - TableView.uxml, MainMenu.uxml, SetupWizard.uxml, FeedbackModal.uxml, HamburgerMenu.uxml, Gameplay.uxml, GuillotineOverlay.uxml
    Prefabs/      - (empty, for future use)
  Scenes/
    NewPlayTesting.unity - Current working scene (single player)
    NetworkingTest.unity - Multiplayer testing (keep for Phase 1)
    NewUIScene.unity     - UI Toolkit development scene
    GuillotineTesting.unity - Guillotine visual testing
```

### Key Scripts

| Script | Lines | Purpose | Status |
|--------|-------|---------|--------|
| GameplayUIController | ~1321 | Master gameplay controller | OK (~1298 lines extracted to 7 classes) |
| GameplayUIController.Editor | ~230 | Editor testing (partial class) | OK (editor only) |
| SetupSettingsPanel | ~1051 | Player setup configuration | OK (unused code removed) |
| PlayerGridPanel | ~1060 | Single player grid display | OK (unused code removed) |
| WordPatternRow | ~1198 | Word pattern row UI | OK (at upper edge, no unused code) |
| ExecutionerAI | ~493 | AI opponent coordination | OK |
| IOpponent | ~177 | Opponent abstraction interface | OK (not wired) |
| LocalAIOpponent | ~300 | AI wrapper for IOpponent | OK (not wired) |
| RemotePlayerOpponent | ~400 | Network player opponent | OK (not wired) |
| UIFlowController | ~2700 | Screen flow + setup wizard + gameplay + modals (includes SetupWizardUIManager) | OK (large but cohesive) |
| GameplayScreenManager | ~650 | Gameplay UI state, tab switching, keyboard | NEW (Phase D) |
| GuillotineOverlayManager | ~450 | Guillotine overlay modal controller | NEW (Phase D) |
| WordRowView | ~460 | Single word row UI component | NEW (updated with validity styling) |
| WordRowsContainer | ~230 | Manages all word rows | NEW |
| PlacementAdapter | ~210 | Adapter for table-based word placement | NEW |
| TablePlacementController | ~500 | 8-direction placement logic for TableModel | NEW |
| WordSuggestionDropdown | ~310 | Autocomplete dropdown for word entry | NEW |
| ColorRules | ~225 | Color rules for setup/gameplay modes | NEW (added setup mode support) |
| TableView | ~425 | UI Toolkit table renderer | NEW (added setup mode coloring) |

### Extracted Controllers (Phase 0)

| Controller | Lines | Purpose | Extracted From |
|------------|-------|---------|---------------|
| GameplayPanelConfigurator | ~175 | Panel setup for owner/opponent grids, SetupData class | GameplayUIController |
| GameplayUIUpdater | ~380 | UI updates (miss counters, names, colors, guillotines) | GameplayUIController |
| PopupMessageController | ~245 | Popup message display, GameOverReason enum | GameplayUIController |
| OpponentTurnManager | ~380 | AI/opponent initialization, turn execution, game state building | GameplayUIController |
| GuessProcessingManager | ~365 | Guess processing for player and opponent | GameplayUIController |
| GameSetupDataCapture | ~240 | Player setup capture and AI opponent generation | GameplayUIController |
| GameplayUIController.Editor | ~230 | Editor testing buttons (partial class) | GameplayUIController |
| GameplayStateTracker | ~300 | State tracking (misses, letters, coordinates) | GameplayUIController (previous) |
| GuessProcessor | ~350 | Low-level guess processing logic | GameplayUIController (previous) |
| WinConditionChecker | ~150 | Win/lose condition checking | GameplayUIController (previous) |
| WordGuessModeController | ~400 | Word guess keyboard mode | GameplayUIController (previous) |

### Packages

- Odin Inspector 4.0.1.2
- DOTween Pro 1.0.386
- UniTask 2.5.10
- New Input System 1.16.0
- Classic_RPG_GUI (UI theme assets)
- MCP for Unity 9.0.3 (Local)
- Feel - REMOVED (can re-add if needed for screen effects)

---

## UI Direction (Locked)

**Technology:** Unity UI Toolkit (not uGUI)

**Approach:** Separate containers for:
- Word rows (variable length, with control buttons)
- Grid table (column headers, row headers, grid cells)

**Separate Panels (not table):**
- Setup wizard fields
- Guillotine visuals
- Guessed word list
- HUD elements

**Key Constraints:**
- Non-virtualized table (cells generated once)
- UI is pure view - game logic updates model, view renders it
- No per-frame allocations
- Model has no Unity UI references (testable)
- Reuse existing systems: WordListSO, WordValidationService, CoordinatePlacementController

---

## UX Redesign - Mode Selection (Jan 10, 2026) - IMPLEMENTED

**Status:** ✅ COMPLETE (commit 871cf525)

**Problem:** Current "START GAME" button lies - it doesn't start a game, it leads to board setup. This violates player trust and creates confusion about where they are in the flow.

**Player Mental Model:**
1. **Configuration** → Deciding HOW to play
2. **Preparation** → Setting up MY board
3. **Play** → The actual game begins

**Solution: Split modes at Main Menu, honest button labels**

### New Main Menu
```
DON'T LOSE YOUR HEAD
[Play Solo]        ← vs The Executioner AI
[Play Online]      ← vs Another Person (Find Opponent)
[Join Game]        ← Enter code to join existing game
[How to Play]
[Settings]
```

### Play Solo / Play Online Flow (same wizard)
```
Profile Card (Name + Color)
    ↓
Grid Size Card (6x6 to 12x12)
    ↓
Word Count Card (3 or 4 words)
    ↓
Difficulty Card (Easy/Normal/Hard)
    ↓
Board Setup Card:
    "How do you want to set up your board?"
    [Quick Setup]     ← Random words + random placement, go to placement panel with everything filled
    [Choose My Words] ← Go to placement panel empty
```

### Placement Panel
- Shows word rows, grid, keyboard
- [Random Words] and [Random Placement] buttons still available
- Bottom button: **"READY"** (not "START GAME")
- For 2P Online: Small "Invite Friend" link generates shareable code
- After READY:
  - 1P: Game starts immediately vs AI
  - 2P: "Waiting for opponent..." (Find Opponent matchmaking or wait for invited friend)

### Join Game Flow (different - reduced config)
```
Main Menu → [Join Game]
    ↓
Enter Code Panel (code input + Join button)
    ↓ (code validated, grid size & word count loaded from host)
Profile Card (Name + Color + Difficulty ONLY - no grid/words selection)
    ↓
Board Setup Card (Quick/Manual choice)
    ↓
Placement Panel (grid size & word count inherited from host's game)
    ↓
READY → Wait for host or start if host already ready
```

### Key Changes Summary
1. **Main Menu splits modes** - 3 clear entry points: Solo, Online, Join
2. **"START GAME" → "READY"** - Honest labeling throughout
3. **Board Setup Card** replaces mode selection card - Quick vs Manual choice
4. **Join Game is separate flow** - Doesn't show grid/words config (inherited from host)
5. **Invite Friend moves** to placement panel as secondary action

### Implementation Steps
1. Update MainMenu.uxml - Replace "START GAME" with "Play Solo", "Play Online", "Join Game"
2. Update SetupWizard.uxml - Remove card-mode, add card-board-setup with Quick/Manual buttons
3. Update UIFlowController - Track game mode (solo/online/join), handle reduced Join flow
4. Add invite code generation on placement panel for 2P mode

---

## Setup Wizard Flow (Phase C Reference - OUTDATED, see UX Redesign above)

Progressive disclosure pattern (like DAB). Single screen with show/hide panels.

**Game Modes (Simplified):**
- **1 Player** - vs The Executioner AI (same experience local or online, online tracks stats)
- **2 Players** - Online only (real opponent OR phantom AI fallback after 5-6 seconds)

Note: No local 2-player pass-and-play - defeats the purpose of hidden word/grid information.

**UI/UX Principles (learned from current implementation):**
- No numbered steps - good UI doesn't need instructions
- Progressive disclosure - don't show table until settings are chosen
- One decision at a time - reduce cognitive load
- Hide letter tracker during initial setup (only show during word placement)
- Settings (grid, words, difficulty) define YOUR setup; in 2-player, opponent picks independently

**Navigation:**
- Forward: click option cards to reveal next step
- Back: hide current panel, show previous
- No page transitions - all panels on single screen

---

## Multiplayer Model

**Game Modes (Simplified):**
- **vs The Executioner** - Local single-player against branded AI
- **vs Another Player** - Online PVP (or phantom AI fallback)

**Phantom AI Fallback:**
- If PVP matchmaking finds no opponent within 5-6 seconds
- Spawn AI with random human-style name (e.g., "FluffyKitty", "Bob", "xXSlayerXx")
- Player doesn't know it's AI - appears as real opponent
- Uses same ExecutionerAI logic, just different display name

**Implementation:**
- LocalAIOpponent: wraps ExecutionerAI for "vs The Executioner" mode
- RemotePlayerOpponent: real network player OR phantom AI (isPhantom flag)
- Phantom uses ExecutionerAI internally but displays fake name/color

**Phantom Name Pool (examples):**
```
FluffyKitty, Bob, WordNinja, LetterLord, xXSlayerXx,
GrammarQueen, SpellMaster, VowelHunter, AlphabetKing,
PuzzlePro, BrainStorm, QuickThinker, WordSmith
```

---

## End Game Vision (Phase F or Later)

**Current Problem:** Game end is anticlimactic - blade drops in a tiny corner while grids still dominate the screen. The game is called "Don't Lose Your Head" but the guillotine moment doesn't feel like an event.

**Proposed Flow:**
```
Game ends (win condition met)
  → Grids fade out
  → Dedicated guillotine scene appears
  → Two guillotines prominent (winner and loser)
  → Executioner character pulls lever
  → Loser's blade drops (slight slow-mo)
  → Head falls into basket
  → Dramatic pause
  → Results display
  → [Play Again] button immediately accessible
```

**Design Goals:**
- **Thematic payoff** - The guillotine moment should MEAN something
- **Emotional punctuation** - Clear "end" beat instead of just stopping
- **Spectacle for both outcomes** - Watch opponent's head roll OR get a dramatic death scene
- **Streamable/shareable** - A good end animation is clip-worthy
- **Quick enough for replays** - 3-4 seconds total, not tedious

**Audio:** Already have 3-part guillotine sequence (rope stretch + raise, hook unlock, chop)

**Note:** Save for Phase F or later - focus on setup/gameplay UI first.

---

## Color Rules (Hard Requirements)

| Color | Usage | Selectable by Player? |
|-------|-------|----------------------|
| Red | System warnings, errors, PlacementInvalid | NO |
| Yellow | System warnings, errors | NO |
| Green | Setup placement feedback (PlacementValid) | YES (but won't show green feedback) |
| Other colors | Player colors, reveal/hit feedback | YES |

**Rules:**
- During setup: green = valid placement, red = invalid placement
- During gameplay: reveal/hit feedback uses player's chosen color
- Red and Yellow only for system messages, never player feedback

---

## Table Model Spec

> Note: This section will be archived to a separate file after Phase B completion.

### Core Enums

```
TableCellKind:
- Spacer         (empty/padding)
- WordSlot       (word row letter slot) - NOW SEPARATE from table
- HeaderCol      (column header: A, B, C...)
- HeaderRow      (row header: 1, 2, 3...)
- GridCell       (board cell)
- KeyboardKey    (optional: gameplay keyboard)

TableCellState:
- None, Normal, Disabled, Hidden, Selected, Hovered, Locked, ReadOnly
- PlacementValid, PlacementInvalid, PlacementPath, PlacementAnchor, PlacementSecond
- Fog, Revealed, Hit, Miss, WrongWord, Warning

CellOwner:
- None, Player1, Player2, ExecutionerAI, PhantomAI
```

### Core Types

```
struct TableCell:
- TableCellKind Kind
- TableCellState State
- CellOwner Owner
- char TextChar        (prefer this for letters)
- string TextString    (avoid, use pooled strings if needed)
- int IntValue         (for header numbers)
- int Row, Col         (cached coordinates)

class TableModel:
- int Rows, Cols
- TableCell[,] Cells
- int Version          (increments on change)
- bool Dirty           (for view to check)
- Methods: ClearAll(), GetCell(), SetCellChar/State/Kind/Owner(), MarkDirty()

struct TableRegion:
- string Name
- int RowStart, ColStart, RowCount, ColCount

class TableLayout:
- TableRegion ColHeaderRegion, RowHeaderRegion, GridRegion
- int[] WordLengths (for word rows - separate container)
- static CreateForSetup(gridSize, wordCount)
- static CreateForGameplay(gridSize, wordCount)

class ColorRules:
- bool IsSelectablePlayerColor(color)
- UIColor GetPlacementColor(state, playerColor, isSetupMode)
- UIColor GetSetupPlacedColor()
- UIColor GetGameplayColor(owner, state, p1Color, p2Color)

class WordRowView:
- Variable length letter cells (3, 4, 5, 6)
- Control buttons (placement, clear, guess)
- Events: OnPlacementRequested, OnClearRequested, OnGuessRequested

class WordRowsContainer:
- Manages array of WordRowView
- Events: OnPlacementRequested, OnWordCleared, OnAllWordsPlaced
```

### Layout Formula

```
Grid Table:
  Rows = 1 (col header) + gridSize
  Cols = 1 (row header) + gridSize

Word Rows (separate):
  Word 1 = 3 letters
  Word 2 = 4 letters
  Word 3 = 5 letters
  Word 4 = 6 letters (if 4 words)
```

### Acceptance Checklist

- [x] TableModel constructed once, cleared/reused without allocations
- [x] TableLayout maps regions correctly for variable grid sizes
- [x] Setup can mark PlacementValid/Invalid/Path/Anchor/Second
- [ ] Gameplay can mark Revealed/Hit/Miss with owner-based colors
- [x] Red and Yellow not selectable as player colors
- [x] Green only for setup placement feedback
- [x] Model has no Unity UI references, can be unit tested
- [x] Word rows have variable lengths (3, 4, 5, 6)
- [x] Word rows integrate with WordValidationService for autocomplete (WordSuggestionDropdown)
- [x] Grid placement uses 8-direction logic (TablePlacementController)
- [x] Setup mode uses green for valid words and placed letters
- [x] Backspace clears grid placement when word is placed

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

- Shuffle playlist (Fisher-Yates), never repeats consecutively
- Crossfade 1.5 seconds between tracks
- Dynamic tempo: 1.0x (normal), 1.08x (80-94% danger), 1.12x (95%+ danger)

**Static Methods:** `ToggleMuteMusic()`, `SetTension(float)`, `ResetMusicTension()`, `IsMusicMuted()`

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

### IOpponent Flow (Future)

```
GameStart
  -> OpponentFactory.CreateAIOpponent() OR CreateRemoteOpponent()
  -> opponent.InitializeAsync(localPlayerSetup)
  -> [For AI: generate opponent setup]
  -> [For Remote: exchange setup via Supabase]

OpponentTurn
  -> opponent.ExecuteTurn(gameState)
  -> [AI: decision logic, think time]
  -> [Remote: wait for network event]
  -> opponent.OnLetterGuess/OnCoordinateGuess/OnWordGuess fires
  -> GameplayUIController handles guess
```

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

### 6. Interface-First Extraction (NEW)

```csharp
// When extracting from large controller:
// 1. Define interface first
public interface IGuessHandler {
    void HandleLetterGuess(char letter);
    void HandleCoordinateGuess(int row, int col);
    void HandleWordGuess(string word, int wordIndex);
}

// 2. Implement in extracted class
public class GuessHandler : IGuessHandler { ... }

// 3. Inject into controller
public class GameplayUIController {
    private IGuessHandler _guessHandler;
    public void Initialize(IGuessHandler guessHandler) {
        _guessHandler = guessHandler;
    }
}
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
14. **Unity 6 UIDocument bug (IN-127759)** - UIDocument inspector destroys runtime UI; assign Source Asset (even empty placeholder) to prevent blue screen
15. **Use E: drive path** - never worktree paths like `C:\Users\steph\.claude-worktrees\...`
16. **Check scene file diffs** - layout can be accidentally modified
17. **SQLite booleans are integers** - parse as int, convert to bool
18. **Drug words filtered** - heroin, cocaine, meth, crack, weed, opium, morphine, ecstasy, molly, dope, smack, coke
19. **EditorWebRequest loading order** - set `_isLoading = false` BEFORE callback that may start next request
20. **Test after each extraction** - verify game works before next extraction
21. **Document interfaces immediately** - update Architecture section after each extraction
22. **Reuse existing systems** - Don't rebuild WordListSO, WordValidationService, CoordinatePlacementController - create thin adapters
23. **Prevent duplicate event handlers** - use flags like `_keyboardWiredUp` when handlers persist across screen rebuilds
24. **Reset validity on clear** - SetWordValid(false) must be called when clearing words to remove green styling

---

## Bug Patterns to Avoid

| Bug Pattern | Cause | Prevention |
|-------------|-------|------------|
| Guess Word buttons disappearing wrong rows | State set AFTER events | Set state BEFORE firing events |
| Autocomplete floating at top | Not hidden at init | Call Hide() in Initialize() |
| Board not resetting | No reset logic | Call ResetGameplayState() on new game |
| Guillotine head stuck | No stored position | Store original position on Initialize |
| MessagePopup off-screen | Complex calculations | Use fixed Y for known anchors |
| Cell vertical stretching | uGUI layout issues | Use UI Toolkit (pivot away from uGUI) |
| Green cells after clear | Validity not reset | Call SetWordValid(false) in HandleWordCleared |
| Random Words no placement button | Words not validated | Call ValidateWord() after SetWord() |

---

## Cross-Project Reference

**All TecVooDoo projects:** `E:\TecVooDoo\Projects\Documents\TecVooDoo_Projects.csv`

---

## Coding Standards (Enforced)

- Prefer async/await (UniTask) over coroutines unless trivial
- Avoid allocations in Update
- No per-frame LINQ
- Clear separation between logic and UI
- ASCII-only documentation and identifiers
- No `var` keyword - explicit types always

### Refactoring Guidelines

**Goal Range:** Files should be 800-1200 lines for optimal Claude compatibility. Files up to 1300 lines are acceptable if that's the minimum achievable without adding unnecessary complexity.

**When to Refactor:**
- Extract when a file exceeds 1200 lines AND has clear, separable responsibilities
- Extract when duplicate code appears across multiple locations
- Extract when a class has multiple unrelated responsibilities

**When NOT to Refactor:**
- Don't refactor to hit an arbitrary line count
- Don't extract if it adds indirection without reducing complexity
- Don't create helper classes for one-off operations
- Don't extract if the code is cohesive and naturally belongs together

**Refactoring Priorities:**
1. Remove unused code and imports first
2. Eliminate duplicate code second
3. Extract genuinely separable responsibilities third
4. Accept the result if further extraction would degrade the design

**Post-Refactor Checklist:**
- [ ] Verify game still works after each extraction
- [ ] Remove any unused using statements
- [ ] Update Architecture section with new files
- [ ] Document new interfaces/controllers

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

- [x] Move completed TODOs to "What Works" section
- [x] Add any new issues to "What Doesn't Work"
- [x] Update "Last Session" with date and summary
- [x] Add new lessons to "Lessons Learned" if applicable
- [x] Update Architecture section if files were added/extracted
- [x] Increment version number in header

---

## Version History

| Version | Date | Summary |
|---------|------|---------|
| 41 | Jan 13, 2026 | Thirty-first session - **Defense View & Turn Switching!** Implemented dual-view tab system: Attack (opponent's grid, player's guesses/keyboard) vs Defend (player's grid, opponent's guesses/keyboard). Auto-switches tabs on turn change (Attack during player, Defend during opponent). Manual tab switching blocked during opponent turn. AI guesses update defense grid (Hit=player color, Miss=red) and opponent keyboard. Added _defenseTableModel, _defenseWordRows, CreateDefenseModel(), MarkDefenseGridCellHit/Miss() to UIFlowController. Added opponent keyboard state tracking to GameplayScreenManager. |
| 40 | Jan 13, 2026 | Thirtieth session - **Gameplay Guess Logic & Word Display!** Fixed grid/keyboard color state logic: grid cells (Fog → Yellow/Revealed → Player Color/Hit), keyboard letters (Default → Yellow/Found → Player Color/Hit), red for misses. Added "WORDS TO FIND" section showing opponent words as underscores. Letter hits reveal in word rows with player color. Added SetWordForGameplay, RevealLetter, RevealAllOccurrences to WordRowView. Added SetWordsForGameplay, RevealLetterInAllWords to WordRowsContainer. |
| 39 | Jan 13, 2026 | Twenty-ninth session - **Miss Limit Bug Fix & 5-Stage Guillotine!** Fixed critical miss limit bug in UIFlowController (was using wrong formula giving 101 misses, now uses DifficultyCalculator with correct 10-40 clamped range). Completely redesigned guillotine from per-miss hash marks to 5-stage system: blade moves only at 20/40/60/80/100% thresholds, 5-segment indicator track with color coding (green->yellow->orange->red), much cleaner visual. Smaller overlay (520x520 from 580x640), simpler guillotine (280px from 420px). |
| 38 | Jan 12, 2026 | Twenty-eighth session - **Guillotine Visual Redesign!** Fixed hamburger button click area (renamed CSS class to avoid conflict). Fixed miss count sync between cards and overlay (added PlayerData/OpponentData getters). Redesigned guillotine: blade-group with wood holder, oval lunette with divider, transparent hash marks, proper z-ordering. Height increased to 420px. Needs testing: blade visibility through hash marks. |
| 37 | Jan 11, 2026 | Twenty-seventh session - **Phase D Testing & Bug Fixes!** Fixed multiple gameplay UI issues: header bar hamburger button overlap (CSS layout with flex-shrink, explicit sizing), guillotine overlay positioning (absolute positioning + pickingMode.Ignore), enlarged guillotine visual to 300px height. Implemented separate guessed words lists for player vs opponent. Wired QWERTY toggle to update gameplay keyboard. Fixed overlay panels blocking clicks with pickingMode handling. **Still needs testing:** hamburger click area, guillotine overlay, separate counts. |
| 36 | Jan 11, 2026 | Twenty-sixth session - **Phase D Implementation Started!** Created all core gameplay UI files: Gameplay.uxml/uss (main layout with tabs, grid area, word rows, keyboard), GuillotineOverlay.uxml/uss (modal with blade positions and game over states), GameplayScreenManager.cs (~650 lines), GuillotineOverlayManager.cs (~450 lines). Updated UIFlowController with TransitionToGameplay(), CreateGameplayScreen(), and all event wiring. Ready button now transitions from setup wizard to gameplay screen. |
| 35 | Jan 11, 2026 | Twenty-fifth session - **Phase D Design Complete!** Analyzed legacy gameplay UI (GameplayUIController, PlayerGridPanel, GridCellUI). Designed new focused single-grid layout with Attack/Defend tab switching. Event-based guillotine overlay (miss counter buttons are tappable). Larger 3-row letter keyboard (reuse setup layout). Asymmetric difficulty display in tabs. Game end sequence with dramatic guillotine animation. |
| 34 | Jan 11, 2026 | Twenty-fourth session - **Setup Visual Polish!** Fixed green coloring consistency: valid words show green in word rows AND on grid during setup. Added backspace clearing grid placement. Fixed Random Words enabling placement buttons. Fixed clear button resetting validity. Added setup mode support to ColorRules and TableView. All setup colors now consistent. |
| 33 | Jan 11, 2026 | Twenty-third session - **Word Entry Polish & AI Crossword!** Invalid word feedback (red highlight + shake via USS). Physical keyboard input (A-Z, Backspace, Escape). AI placement now supports crossword-style overlapping with 8 directions and 40% random crossword probability. |
| 32 | Jan 10, 2026 | Twenty-second session - **Word Suggestion Dropdown!** Added WordSuggestionDropdown.cs - autocomplete that filters words as user types, touch-friendly (Button elements). Fixed z-index to appear above grid. Updated placement instructions: "Hide your words on the grid - your opponent will try to find them!" (playtesters confused about whose words). |
| 31 | Jan 10, 2026 | Twenty-first session - **UI Polish & Join Game Flow!** Fixed feedback modal text color. Added Continue Game button (orange, hidden by default). Fixed Join Game: Profile → Difficulty → Join Code flow (skips Grid/Words). Added Join Code card with input + submit. Moved hamburger USS to USS Assets section for consistency. |
| 30 | Jan 10, 2026 | Twentieth session - **Bug Fixes!** Fixed trivia: proper guillotine/beheading facts (24 total), centered below title, 5-sec display + fade cycling. Fixed Join Game: hides Grid/Words/Difficulty/BoardSetup cards. Added debug warnings for missing modal assets. Note: FeedbackModal/HamburgerMenu need Inspector assignment. |
| 29 | Jan 10, 2026 | Nineteenth session - **Navigation & Settings Complete!** Inline settings on main menu (SFX/Music sliders, QWERTY checkbox). FeedbackModal component with PlaytestTelemetry integration. HamburgerMenu for setup/gameplay navigation with synced settings. Trivia marquee with 14 random facts. Settings persist to PlayerPrefs. |
| 28 | Jan 10, 2026 | Eighteenth session - **UX Redesign Complete!** Mode selection redesign: Play Solo / Play Online / Join Game. Setup wizard uses Quick Setup / Choose My Words. Added GameMode enum. Ready button disabled until all words placed. Quick Setup auto-fills random words and placement. |
| 27 | Jan 10, 2026 | Seventeenth session - **PlacementAdapter Complete!** Created TablePlacementController (~500 lines) and PlacementAdapter in PlacementAdapter.cs (~760 lines total). Works directly with TableModel (no GridCellUI dependency). Two-click placement: start cell -> direction. 8-direction support with preview highlighting (green=valid, red=invalid, orange=anchor). Random Placement sorts longest-first. Fixed clear-during-placement bug. |
| 26 | Jan 9, 2026 | Sixteenth session - **Placement Panel Layout Fixed!** Fixed backspace button duplication, Unity 6 blue screen bug (EmptyRoot.uxml workaround), keyboard multiple input bug (_keyboardWiredUp flag). Implemented 3-row keyboard (ABCDEFGHI, JKLMNOPQR, STUVWXYZ+backspace). Added responsive sizing for action buttons (.size-large for 6x6/7x7). Notes: QWERTY keyboard option and longest-words-first placement for future. |
| 25 | Jan 9, 2026 | Fifteenth session - **WordValidationService Integrated!** Fixed compilation errors (deleted obsolete WordPlacementController.cs, SetupWizardTest.cs, fixed TableViewTest.cs). Integrated WordValidationService into UIFlowController. Random Words uses actual WordListSO. Word entry with row selection, backspace, validation, and auto-advance. |
| 24 | Jan 9, 2026 | Fourteenth session - **Word Rows Architecture Redesigned!** Major refactor separating word rows from grid table. Word rows now have variable lengths (3,4,5,6). Created WordRowView.cs and WordRowsContainer.cs. Updated TableLayout/TableModel to be grid-only. UIFlowController uses new system. Integration plan documented in UI_Toolkit_Integration_Plan.md. |
| 23 | Jan 9, 2026 | Thirteenth session - **Main Menu working!** Created MainMenu.uxml/uss with title, tagline, buttons. Created UIFlowController.cs for screen transitions. Fixed TemplateContainer sizing. Deleted conflicting UIRoot/TableTest GameObjects. Flow: Main Menu -> Setup Wizard working. Next: placement panel, "How many players?" UX. |
| 22 | Jan 8, 2026 | Twelfth session - **Setup Wizard UI working!** Created SetupWizard.uxml/uss/controller with progressive card-based disclosure. Cards collapse to summary line when moving forward, clickable to re-expand. Flow: Profile -> Grid -> Words -> Difficulty -> Mode. All collapse/expand sequences correct. Notes for next session: Main Menu needed, rethink "How many players?" UX. |
| 21 | Jan 8, 2026 | Eleventh session - Reviewed DAB UI for inspiration. Simplified game modes: 1 Player (vs AI) and 2 Players (online only, no pass-and-play due to hidden info). Updated Setup Wizard Flow with UI/UX principles. Added End Game Vision section (dramatic guillotine finale for Phase F). |
| 20 | Jan 8, 2026 | Tenth session - **Phase A & B COMPLETE!** Created DLYH.TableUI namespace with table data model (8 files) and UI Toolkit renderer. TableView renders word rows, headers, grid. Click interactions work. Added Setup Wizard Flow and simplified Multiplayer Model (phantom AI pattern) to status doc. |
| 19 | Jan 7, 2026 | Ninth session - Project restored to E: drive. Housekeeping: deleted orphaned .csproj files (42) and AnyPortrait .txt files (6) from removed packages. Consolidated Claude Code permissions into E:\.claude\settings.json, deleted redundant E:\Unity\.claude folder. Created NewUIScene.unity for Phase A work. |
| 18 | Jan 7, 2026 | Eighth session - **Phase 0.5 COMPLETE!** Created PlayerService.cs (~385 lines) to create player records without Supabase Auth (matches DAB pattern). Fixed GetPlayerCount query (session_players has no `id` column). Both Unity instances connect successfully. Anonymous users can now be disabled in Supabase. |
| 17 | Jan 7, 2026 | Seventh session - continued Phase 0.5. Added OnGUI debug panel for Virtual Player testing. Fixed schema mismatches (removed player_name/player_color, null for created_by). Hit blocking issue: session_players.player_id NOT NULL + FK to players prevents anonymous join. Project copied to C: drive for MPPM testing (E: not NTFS). |
| 16 | Jan 7, 2026 | Sixth session - Phase 0.5 started. Created NetworkingTestController.cs (~367 lines) for debug Host/Join UI. Added SetOpponent() to GameplayUIController for IOpponent injection. Created NetworkingTest.unity scene. Deleted abandoned NewUIDesign.unity and LetterCellUI/WordPatternRowUI prefabs. Fixed JoinGame parameter mismatch. |
| 15 | Jan 7, 2026 | Fifth session - housekeeping cleanup. Removed ~235 lines of unused dynamic sizing code from SetupSettingsPanel (1286->1051). Removed unused _panelLayoutElement from PlayerGridPanel (1067->1060). Verified WordPatternRow (1198 lines) has no unused code. All major UI scripts now within 800-1200 goal range. |
| 14 | Jan 7, 2026 | Fourth refactor session. Extracted Editor Testing to partial class and Data Capture to GameSetupDataCapture. GameplayUIController at ~1321 lines (50% reduction from ~2619). Added Refactoring Guidelines to Coding Standards. |
| 13 | Jan 6, 2026 | Third refactor session. Extracted GuessProcessingManager (~365 lines). Removed duplicate popup code. Removed Feel package. GameplayUIController at ~1761 lines (33% reduction from ~2619). |
| 12 | Jan 6, 2026 | Continued Phase 0. Extracted OpponentTurnManager (~380 lines). Removed duplicate Guillotine/MissCounter code. GameplayUIController now at ~2043 lines (from original ~2619). |
| 11 | Jan 6, 2026 | Phase 0 started. Extracted 3 controllers from GameplayUIController (GameplayPanelConfigurator, GameplayUIUpdater, PopupMessageController). Fixed Claude Code and GitHub settings. |
| 10 | Jan 6, 2026 | Consolidated from DLYH_Status.md (v7), DLYH_Status_REWRITTEN.md (v9), and Table Model Spec. New plan: Phase 0 refactor, Phase 0.5 multiplayer verify, then UI Toolkit. Pivot from uGUI to UI Toolkit documented. |
| 9 | Jan 6, 2026 | (REWRITTEN) Locked UI Toolkit + table approach, clarified networking |
| 8 | Jan 6, 2026 | (REWRITTEN) Initial UI Toolkit redesign plan |
| 7 | Jan 6, 2026 | Fixed row width with delayed init, cell vertical stretch issue |
| 6 | Jan 5, 2026 | WordPatternPanelUI created, cell sizing troubleshooting |
| 5 | Jan 5, 2026 | WordPatternRowUI complete with icons, Classic_RPG_GUI integration |
| 4 | Jan 5, 2026 | Removed shared doc pointers, streamlined AI Rules |
| 3 | Jan 5, 2026 | Enhanced with AI system, audio, patterns, data flows |
| 2 | Jan 5, 2026 | Archived NewUI architecture doc |
| 1 | Jan 4, 2026 | Initial consolidated document (replaces 4-doc system) |

---

## Next Session Instructions

**Starting Point:** This document (DLYH_Status.md v41)

**Scene to Use:** NewUIScene.unity (for UI Toolkit work - Phase D)

**Current State:**
- Phase A & B COMPLETE - table data model and UI Toolkit renderer working
- Phase C COMPLETE - Setup wizard fully functional with all polish
- Phase D IN PROGRESS - Defense view and turn switching implemented!

**Files Modified This Session:**
- `UIFlowController.cs` - Added defense view fields (_defenseTableModel, _defenseWordRows), CreateDefenseModel(), MarkDefenseGridCellHit/Miss(), updated TransitionToGameplay() to create defense grid/words, updated AI guess handlers to show on defense grid and update opponent keyboard, added HideAllButtons() call for defense word rows
- `GameplayScreenManager.cs` - Added opponent keyboard state (_opponentHitLetters, _opponentMissLetters), MarkOpponentLetterHit/Miss(), SetAllowManualTabSwitch(), RefreshKeyboardForCurrentTab(), updated SelectAttackTab/SelectDefendTab with isAutoSwitch parameter, added keyboard click guard for Defend tab, dimmed unguessed letters on Defend keyboard
- `WordRowView.cs` - Added HideAllButtons() method
- `WordRowsContainer.cs` - Added HideAllButtons() method

**Key Changes This Session:**
1. **Defense View Implemented:**
   - Player's grid with their letters fully visible
   - AI guesses mark cells as Hit (player color) or Miss (red)
   - Player's words fully visible in defense word rows
2. **Dual Keyboard State:**
   - Attack tab shows player's guess keyboard state
   - Defend tab shows opponent's guess keyboard state
   - Keyboard refreshes on tab switch
3. **Auto Tab Switching:**
   - Player's turn → Attack tab + manual switching enabled
   - Opponent's turn → Defend tab + manual switching disabled
4. **Tab Click Guarding:**
   - `_allowManualTabSwitch` flag blocks clicks during opponent turn
   - Auto-switch uses `isAutoSwitch: true` to bypass guard

**Tab Behavior Summary:**
| Tab | Grid Shows | Words Show | Keyboard Shows | Whose Guesses |
|-----|------------|------------|----------------|---------------|
| Attack | Opponent's (fog + your guesses) | Opponent's (hidden) | YOUR guesses | Yours |
| Defend | YOUR (visible + AI guesses) | YOUR (visible) | AI's guesses | AI's |

**Priority Tasks for Next Session:**
1. **TEST defense view** - Verify tab switching, defense grid visuals, opponent keyboard
2. **TEST guillotine 5-stage visual** - Verify blade positions, segment colors, stage labels
3. Add extra turn on word completion (player or AI completing a word gets another turn)
4. Add win/lose detection (AreAllWordsRevealed() for win, miss limit for lose)
5. Add game end sequence with guillotine animation

**Existing Systems to Wire (DO NOT REBUILD):**
- `GameplayStateTracker` - State tracking (misses, letters, coordinates)
- `WinConditionChecker` - Win/lose condition checking

**Deferred to Phase E (requires C: drive copy for networking):**
- Wire Join Code submit to networking code
- All networking integration work batched together

**Namespace Decision:**
- New UI code uses `DLYH.TableUI` namespace
- Existing code stays in original namespaces

**Do NOT:**
- Delete NetworkingTest.unity scene yet (may need for Phase E)
- Over-polish visuals yet (functional first, polish in Phase F)

---

**End of Project Status**

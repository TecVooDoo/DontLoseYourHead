# Don't Lose Your Head - Project Status

**Project:** Don't Lose Your Head (DLYH)
**Developer:** TecVooDoo LLC / Rune (Stephen Brandon)
**Platform:** Unity 6.3 (6000.0.38f1)
**Source:** `E:\Unity\DontLoseYourHead`
**Document Version:** 23
**Last Updated:** January 9, 2026

---

## Quick Context

**What is this game?** A competitive word game combining Hangman's letter-guessing with Battleship's grid-based hidden information. Players place hidden words on grids and take turns guessing letters or coordinates to find opponent's words before the guillotine blade falls.

**Key Innovation:** Asymmetric difficulty - mixed-skill players compete fairly with different grid sizes, word counts, and difficulty settings.

**Current Phase:** Phase C IN PROGRESS - Main menu complete, placement panel next

**Last Session (Jan 9, 2026):** Thirteenth session - **Main Menu working!** Created MainMenu.uxml/uss with title, tagline, and buttons. Created UIFlowController.cs to manage screen transitions (Main Menu -> Setup Wizard). Fixed TemplateContainer sizing issue. Deleted old UIRoot and TableTest GameObjects that were conflicting. Flow: Main Menu -> Setup Wizard -> (placement next).

**TODO for next session:**
- Rethink the "How many players?" section - current 1 Player/2 Players buttons need clearer UX design
- Wire word placement panel to table UI
- Test full flow: Menu -> Setup -> Placement
- Add marquee with cycling guillotine facts to Main Menu (later)
- Add feedback button to Main Menu (later)

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
- [ ] Phase C: Setup wizard + placement using table UI
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

**DLYH.TableUI (NEW - Phase A/B):**
- TableCellKind, TableCellState, CellOwner (enums)
- TableCell (struct), TableRegion (struct)
- TableLayout, TableModel, ColorRules, TableView, TableViewTest

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
                    TableLayout, TableModel, ColorRules, TableView, TableViewTest
    USS/          - TableView.uss (cell styling)
    UXML/         - TableView.uxml (container template)
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
- Feel - REMOVED (can re-add if needed for screen effects)

---

## UI Direction (Locked)

**Technology:** Unity UI Toolkit (not uGUI)

**Approach:** Unified table-style UI for:
- Word rows
- Column headers (A, B, C...)
- Row headers (1, 2, 3...)
- Grid cells

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

---

## Setup Wizard Flow (Phase C Reference)

Progressive disclosure pattern (like DAB). Single screen with show/hide panels.

**Game Modes (Simplified):**
- **1 Player** - vs The Executioner AI (same experience local or online, online tracks stats)
- **2 Players** - Online only (real opponent OR phantom AI fallback after 5-6 seconds)

Note: No local 2-player pass-and-play - defeats the purpose of hidden word/grid information.

```
Setup Screen (always visible)
├── Name input
├── Color picker (excluding red/yellow)
├── Grid Size (6x6 to 12x12)
├── Word Count (3 or 4)
├── Difficulty (Easy/Normal/Hard)
│
├── How many players?
│   ├── [1 Player] - "Play against The Executioner AI"
│   │   └── [START GAME] → Word placement phase
│   └── [2 Players] - "Play against another person"
│       ├── [Find Opponent] → Queue + 5-6s timeout → phantom AI fallback
│       ├── [Invite Friend] → Generate game code
│       └── [Join Game] → Enter code input
│
└── Word Placement Phase
    ├── Table UI shows word rows + grid
    ├── Letter tracker doubles as keyboard (mobile-friendly input)
    ├── Player places words on their grid
    └── [Ready] → Exchange setup, start gameplay
```

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
- WordSlot       (word row letter slot)
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
- TableRegion WordRowsRegion, ColHeaderRegion, RowHeaderRegion, GridRegion
- static CreateForSetup(gridSize, wordCount)
- static CreateForGameplay(gridSize, wordCount)

class ColorRules:
- bool IsSelectablePlayerColor(color)
- UIColor GetPlacementColor(state)
- UIColor GetGameplayColor(owner, state, p1Color, p2Color)
```

### Layout Formula

```
Rows = wordCount + 1 (col header) + gridSize
Cols = 1 (row header) + gridSize
```

### Acceptance Checklist

- [ ] TableModel constructed once, cleared/reused without allocations
- [ ] TableLayout maps regions correctly for variable grid sizes
- [ ] Setup can mark PlacementValid/Invalid/Path/Anchor/Second
- [ ] Gameplay can mark Revealed/Hit/Miss with owner-based colors
- [ ] Red and Yellow not selectable as player colors
- [ ] Green only for setup placement feedback
- [ ] Model has no Unity UI references, can be unit tested

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
14. **Use E: drive path** - never worktree paths like `C:\Users\steph\.claude-worktrees\...`
15. **Check scene file diffs** - layout can be accidentally modified
16. **SQLite booleans are integers** - parse as int, convert to bool
17. **Drug words filtered** - heroin, cocaine, meth, crack, weed, opium, morphine, ecstasy, molly, dope, smack, coke
18. **EditorWebRequest loading order** - set `_isLoading = false` BEFORE callback that may start next request
19. **Test after each extraction** - verify game works before next extraction
20. **Document interfaces immediately** - update Architecture section after each extraction

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

- [ ] Move completed TODOs to "What Works" section
- [ ] Add any new issues to "What Doesn't Work"
- [ ] Update "Last Session" with date and summary
- [ ] Add new lessons to "Lessons Learned" if applicable
- [ ] Update Architecture section if files were added/extracted
- [ ] Increment version number in header

---

## Version History

| Version | Date | Summary |
|---------|------|---------|
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

**Starting Point:** This document (DLYH_Status.md v22)

**Scene to Use:** NewUIScene.unity (for UI Toolkit work - Phase C)

**Current State:**
- Phase A & B COMPLETE - table data model and UI Toolkit renderer working
- Phase C IN PROGRESS - Setup wizard working with progressive card collapse
- SetupWizardController.cs manages wizard flow
- SetupWizard.uxml/uss define the UI structure and styling

**Files Created This Session:**
- `Assets/DLYH/NewUI/Scripts/UIFlowController.cs` - Screen flow manager (Main Menu <-> Setup Wizard)
- `Assets/DLYH/NewUI/Scripts/MainMenuController.cs` - Main menu controller (unused, logic in UIFlowController)
- `Assets/DLYH/NewUI/UXML/MainMenu.uxml` - Main menu layout with title, tagline, buttons
- `Assets/DLYH/NewUI/USS/MainMenu.uss` - Dark theme styling for main menu

**Priority Tasks for Next Session:**
1. **Rethink "How many players?" UX** - Current 1 Player/2 Players buttons work but need clearer design
   - Consider: What happens after clicking each option?
   - 1 Player should feel immediate (Start Game button appears)
   - 2 Players has multiple sub-options (Find Opponent, Invite, Join)
   - Maybe different card layout or clearer visual hierarchy?
2. **Wire word placement panel** - Connect to table UI after "Start Game" clicked
3. **Test full flow** - Menu -> Setup wizard -> Placement -> Ready
4. **Add marquee with guillotine facts** - Cycling historical facts on main menu (later)
5. **Add feedback button** - On main menu (later)

**Namespace Decision:**
- New UI code uses `DLYH.TableUI` namespace
- Existing code stays in original namespaces (no migration needed)

**Do NOT:**
- Delete NetworkingTest.unity scene yet (may need for Phase E)
- Polish visuals yet (save for Phase F)

---

**End of Project Status**

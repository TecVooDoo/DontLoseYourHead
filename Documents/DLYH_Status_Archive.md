# Don't Lose Your Head - Status Archive

**Purpose:** Historical reference for completed work. This document contains design decisions, version history, and implementation details that are no longer needed in active development but may be useful for reference.

**Related Document:** `DLYH_Status.md` (active status document)

**Last Updated:** January 16, 2026

---

## Archive Instructions

At the end of each session, move the following to this archive:

1. **Version History entries older than 5 sessions** - Keep only the most recent ~5 entries in the main status doc
2. **Completed phase design details** - Once a phase is fully complete and tested, move its detailed design here
3. **Implemented UX designs** - Once implemented and working, the detailed wireframes can be archived
4. **Resolved bug details** - Keep the bug pattern in the main doc, but detailed fix history can move here
5. **Superseded designs** - If a design was replaced, archive the old version here

**Do NOT archive:**
- Active TODO items
- Current phase work
- Architecture reference (namespaces, key files, key types)
- Lessons Learned (always needed)
- Bug Patterns to Avoid (always needed)
- Game Rules (needed during gameplay work)
- AI System parameters (needed during AI work)
- Coding Standards (always needed)

---

## Table of Contents

1. [Version History (Sessions 1-44)](#version-history-sessions-1-44)
2. [Phase A Design (Complete)](#phase-a-design-complete)
3. [Phase B Design (Complete)](#phase-b-design-complete)
4. [Phase C Design (Complete)](#phase-c-design-complete)
5. [Phase D Design (Complete)](#phase-d-design-complete)
6. [UX Design: Navigation & Settings (Implemented)](#ux-design-navigation--settings-implemented)
7. [UX Redesign: Mode Selection (Implemented)](#ux-redesign-mode-selection-implemented)
8. [Setup Wizard Flow (Superseded)](#setup-wizard-flow-superseded)
9. [Table Model Spec (Implemented)](#table-model-spec-implemented)
10. [Data Flow Diagrams](#data-flow-diagrams)

---

## Version History (Sessions 1-44)

| Version | Date | Summary |
|---------|------|---------|
| 55 | Jan 16, 2026 | Forty-fourth session - **Phase D Complete!** Implemented How to Play modal with scrollable help content. Moved DefenseViewPlan.md and UI_Toolkit_Integration_Plan.md to Archive. |
| 54 | Jan 16, 2026 | Forty-third session - **Gameplay Audio & New Game Confirmation!** Wired UIAudioManager (keyboard, grid, hit/miss, buttons, popups). Added confirmation popup when starting new game during active game. Added ResetGameState() for proper cleanup. |
| 53 | Jan 16, 2026 | Forty-second session - **Guillotine Polish & Audio Sync!** Fixed lever positioning (inner posts). Fixed executioner z-order and vertical position. Removed invalid USS properties. Synced stage transition audio (1.5s delay). Synced game-over animations with audio (blade drop, head fall). |
| 52 | Jan 16, 2026 | Forty-first session - **Guillotine Visual Fixes & Executioner Placeholder!** Fixed blade direction (rises with danger). Added executioner stick figure. Added lever assemblies. Auto-show overlay on stage transition. Blade raise audio. |
| 51 | Jan 16, 2026 | Fortieth session - Guillotine 5-stage implementation, game-over sequence, blade positions. |
| 50 | Jan 15, 2026 | Thirty-ninth session - Extra turn logic, win/lose detection wiring, end-game reveal. |
| 49 | Jan 15, 2026 | Thirty-eighth session - **Keyboard/Word Row Color Upgrade & Opponent Win Detection!** Fixed RefreshOpponentKeyboardStates. Added HasOpponentWon(), CheckForOpponentWin(). |
| 48 | Jan 15, 2026 | Thirty-seventh session - **Defense Board Color Fix & AI Word Guess Bug!** Fixed coordinate guesses incorrectly updating keyboard. Fixed AI guessing same wrong word repeatedly. |
| 38 | Jan 12, 2026 | Twenty-eighth session - **Guillotine Visual Redesign!** Fixed hamburger button click area (renamed CSS class to avoid conflict). Fixed miss count sync between cards and overlay (added PlayerData/OpponentData getters). Redesigned guillotine: blade-group with wood holder, oval lunette with divider, transparent hash marks, proper z-ordering. Height increased to 420px. Needs testing: blade visibility through hash marks. |
| 37 | Jan 11, 2026 | Twenty-seventh session - **Phase D Testing & Bug Fixes!** Fixed multiple gameplay UI issues: header bar hamburger button overlap (CSS layout with flex-shrink, explicit sizing), guillotine overlay positioning (absolute positioning + pickingMode.Ignore), enlarged guillotine visual to 300px height. Implemented separate guessed words lists for player vs opponent. Wired QWERTY toggle to update gameplay keyboard. Fixed overlay panels blocking clicks with pickingMode handling. **Still needs testing:** hamburger click area, guillotine overlay, separate counts. |
| 36 | Jan 11, 2026 | Twenty-sixth session - **Phase D Implementation Started!** Created all core gameplay UI files: Gameplay.uxml/uss (main layout with tabs, grid area, word rows, keyboard), GuillotineOverlay.uxml/uss (modal with blade positions and game over states), GameplayScreenManager.cs (~650 lines), GuillotineOverlayManager.cs (~450 lines). Updated UIFlowController with TransitionToGameplay(), CreateGameplayScreen(), and all event wiring. Ready button now transitions from setup wizard to gameplay screen. |
| 35 | Jan 11, 2026 | Twenty-fifth session - **Phase D Design Complete!** Analyzed legacy gameplay UI (GameplayUIController, PlayerGridPanel, GridCellUI). Designed new focused single-grid layout with Attack/Defend tab switching. Event-based guillotine overlay (miss counter buttons are tappable). Larger 3-row letter keyboard (reuse setup layout). Asymmetric difficulty display in tabs. Game end sequence with dramatic guillotine animation. |
| 34 | Jan 11, 2026 | Twenty-fourth session - **Setup Visual Polish!** Fixed green coloring consistency: valid words show green in word rows AND on grid during setup. Added backspace clearing grid placement. Fixed Random Words enabling placement buttons. Fixed clear button resetting validity. Added setup mode support to ColorRules and TableView. All setup colors now consistent. |
| 33 | Jan 11, 2026 | Twenty-third session - **Word Entry Polish & AI Crossword!** Invalid word feedback (red highlight + shake via USS). Physical keyboard input (A-Z, Backspace, Escape). AI placement now supports crossword-style overlapping with 8 directions and 40% random crossword probability. |
| 32 | Jan 10, 2026 | Twenty-second session - **Word Suggestion Dropdown!** Added WordSuggestionDropdown.cs - autocomplete that filters words as user types, touch-friendly (Button elements). Fixed z-index to appear above grid. Updated placement instructions: "Hide your words on the grid - your opponent will try to find them!" (playtesters confused about whose words). |
| 31 | Jan 10, 2026 | Twenty-first session - **UI Polish & Join Game Flow!** Fixed feedback modal text color. Added Continue Game button (orange, hidden by default). Fixed Join Game: Profile -> Difficulty -> Join Code flow (skips Grid/Words). Added Join Code card with input + submit. Moved hamburger USS to USS Assets section for consistency. |
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

## Phase A Design (Complete)

**Status:** COMPLETE (Jan 8, 2026)

### Goal
Implement TableModel, TableCell, TableCellKind, TableCellState, CellOwner - the data foundation with no visual changes.

### Implementation
- Created `DLYH.TableUI` namespace
- TableCellKind enum: Spacer, WordSlot, HeaderCol, HeaderRow, GridCell, KeyboardKey
- TableCellState enum: None, Normal, Disabled, Hidden, Selected, Hovered, Locked, ReadOnly, PlacementValid, PlacementInvalid, PlacementPath, PlacementAnchor, PlacementSecond, Fog, Revealed, Hit, Miss, WrongWord, Warning
- CellOwner enum: None, Player1, Player2, ExecutionerAI, PhantomAI
- TableCell struct with Kind, State, Owner, TextChar, TextString, IntValue, Row, Col
- TableModel class with Rows, Cols, Cells array, Version, Dirty flag
- TableLayout class with region definitions
- TableRegion struct for mapping areas

### Files Created
- `TableCellKind.cs`
- `TableCellState.cs`
- `CellOwner.cs`
- `TableCell.cs`
- `TableModel.cs`
- `TableLayout.cs`
- `TableRegion.cs`
- `ColorRules.cs`

---

## Phase B Design (Complete)

**Status:** COMPLETE (Jan 8, 2026)

### Goal
Build UI Toolkit table renderer that generates all cells once and updates via state changes only.

### Implementation
- TableView.cs renders TableModel using UI Toolkit
- Non-virtualized (all cells generated at startup)
- State changes trigger visual updates only
- No per-frame allocations
- Click interactions work

### Files Created
- `TableView.cs`
- `TableView.uxml`
- `TableView.uss`
- `TableViewTest.cs` (for testing)

---

## Phase C Design (Complete)

**Status:** COMPLETE (Jan 11, 2026)

### Goal
Replace monolithic setup screen with guided wizard using table UI.

### Key Decisions
- Word rows separated from grid table (variable lengths 3,4,5,6)
- Progressive card disclosure pattern
- Physical keyboard support
- Autocomplete dropdown for word entry
- 8-direction placement with preview

### Files Created
- `SetupWizard.uxml` / `SetupWizard.uss`
- `MainMenu.uxml` / `MainMenu.uss`
- `WordRowView.cs`
- `WordRowsContainer.cs`
- `PlacementAdapter.cs`
- `TablePlacementController.cs`
- `WordSuggestionDropdown.cs`
- `UIFlowController.cs`

---

## Phase D Design (Complete)

**Status:** COMPLETE (Jan 16, 2026)

### Goal
Convert gameplay UI to UI Toolkit with Attack/Defend tab switching, guillotine overlay, and full audio integration.

### Key Features Implemented
- **Attack/Defend Tabs** - Single-grid focused layout with tab switching
- **Defense View** - Player's grid showing opponent's guesses
- **Auto-tab Switching** - Switches to Defend during opponent's turn
- **Separate Keyboard States** - Attack shows player's guesses, Defend shows opponent's
- **Guillotine Overlay** - 5-stage visual (not per-miss hash marks)
- **Game End Sequence** - Blade drop, head fall animations synced with audio
- **Extra Turn Logic** - Queue-based for multiple word completions
- **How to Play Modal** - Scrollable help content accessible from main menu

### Audio Integration
- `UIAudioManager` - Keyboard clicks, grid clicks, hit/miss sounds, button clicks, popup sounds
- `GuillotineAudioManager` - Blade raise, hook unlock, chop, head removed
- Stage transition audio with 1.5s delay before animation

### Files Created/Modified
- `Gameplay.uxml` / `Gameplay.uss` - Main gameplay layout
- `GuillotineOverlay.uxml` / `GuillotineOverlay.uss` - Guillotine modal
- `GameplayScreenManager.cs` (~650 lines) - Tab switching, keyboard, status
- `GuillotineOverlayManager.cs` (~450 lines) - Overlay controller with animations
- `UIFlowController.cs` - Extended with gameplay transitions, help modal

### Tab Behavior
| Tab | Grid Shows | Words Show | Keyboard Shows | Whose Guesses |
|-----|------------|------------|----------------|---------------|
| Attack | Opponent's (fog + your guesses) | Opponent's (hidden) | YOUR guesses | Yours |
| Defend | YOUR (visible + opponent guesses) | YOUR (visible) | Opponent's guesses | Opponent's |

### Archived Plan Documents
- `DefenseViewPlan.md` - Moved to Archive/
- `UI_Toolkit_Integration_Plan.md` - Moved to Archive/

---

## UX Design: Navigation & Settings (Implemented)

**Status:** IMPLEMENTED (Jan 10, 2026)

**Design Philosophy:** Fewer screens, faster access, consistent patterns.

### Main Menu Layout
```
DON'T LOSE YOUR HEAD
"A game of words and wits"

[Play Solo]
[Play Online]
[Join Game]
[How to Play]
[Feedback]              <- Opens modal overlay

------------------------------
SFX [slider] 50%        <- SFX slider (inline)
Music [slider] 50%      <- Music slider (inline)
[x] QWERTY Keyboard     <- Checkbox (inline)

"The guillotine was used in France until 1977."  <- Trivia marquee
```

**Key decisions:**
- Settings inline on main menu (no separate screen)
- Default volumes: 50% (persist to PlayerPrefs)
- Remove Settings button, controls always visible

### Feedback Modal (Reusable)
```
+-----------------------------+
|      [Title]                |  <- "Share Feedback" / "Victory!" / "Defeated"
|                             |
|  +------------------------+ |
|  |  (text area)           | |
|  +------------------------+ |
|                             |
|    [Submit]    [Cancel]     |
+-----------------------------+
```
- From main menu: Feedback button opens with "Share Feedback"
- After game ends: Auto-opens with win/lose title
- Integrates with `PlaytestTelemetry.Feedback()`

### Hamburger Menu for Setup/Gameplay
Top-left corner during setup wizard and gameplay:
```
[=] <--- Always visible

Opens overlay:
+----------------+
| Main Menu      |  <- Returns to main menu
| Settings       |  <- Shows SFX/Music/QWERTY inline
| Resume         |  <- Closes overlay (gameplay only)
+----------------+
```

### Continue Game Flow
- If game in progress and user goes to Main Menu -> add [Continue Game] button
- Continue Game returns to active gameplay state

### Files Created (Jan 10, 2026)
- `FeedbackModal.uxml` + `FeedbackModal.uss` - Modal overlay component
- `HamburgerMenu.uxml` + `HamburgerMenu.uss` - Navigation overlay
- Updated `MainMenu.uxml` - Added inline settings, feedback button, trivia label
- Updated `MainMenu.uss` - Slider and checkbox styles
- Updated `UIFlowController.cs` - Wired hamburger menu, feedback modal, settings persistence

---

## UX Redesign: Mode Selection (Implemented)

**Status:** IMPLEMENTED (Jan 10, 2026, commit 871cf525)

**Problem:** Current "START GAME" button lies - it doesn't start a game, it leads to board setup. This violates player trust and creates confusion about where they are in the flow.

**Player Mental Model:**
1. **Configuration** -> Deciding HOW to play
2. **Preparation** -> Setting up MY board
3. **Play** -> The actual game begins

**Solution: Split modes at Main Menu, honest button labels**

### New Main Menu
```
DON'T LOSE YOUR HEAD
[Play Solo]        <- vs The Executioner AI
[Play Online]      <- vs Another Person (Find Opponent)
[Join Game]        <- Enter code to join existing game
[How to Play]
[Settings]
```

### Play Solo / Play Online Flow (same wizard)
```
Profile Card (Name + Color)
    |
Grid Size Card (6x6 to 12x12)
    |
Word Count Card (3 or 4 words)
    |
Difficulty Card (Easy/Normal/Hard)
    |
Board Setup Card:
    "How do you want to set up your board?"
    [Quick Setup]     <- Random words + random placement, go to placement panel with everything filled
    [Choose My Words] <- Go to placement panel empty
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
Main Menu -> [Join Game]
    |
Enter Code Panel (code input + Join button)
    | (code validated, grid size & word count loaded from host)
Profile Card (Name + Color + Difficulty ONLY - no grid/words selection)
    |
Board Setup Card (Quick/Manual choice)
    |
Placement Panel (grid size & word count inherited from host's game)
    |
READY -> Wait for host or start if host already ready
```

### Key Changes Summary
1. **Main Menu splits modes** - 3 clear entry points: Solo, Online, Join
2. **"START GAME" -> "READY"** - Honest labeling throughout
3. **Board Setup Card** replaces mode selection card - Quick vs Manual choice
4. **Join Game is separate flow** - Doesn't show grid/words config (inherited from host)
5. **Invite Friend moves** to placement panel as secondary action

---

## Setup Wizard Flow (Superseded)

**Note:** This section documents the original design before the Jan 10 UX Redesign. Kept for historical reference.

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

## Table Model Spec (Implemented)

**Note:** This spec was implemented in Phase A/B. Kept for reference.

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

### Acceptance Checklist (All Complete)

- [x] TableModel constructed once, cleared/reused without allocations
- [x] TableLayout maps regions correctly for variable grid sizes
- [x] Setup can mark PlacementValid/Invalid/Path/Anchor/Second
- [x] Gameplay can mark Revealed/Hit/Miss with owner-based colors
- [x] Red and Yellow not selectable as player colors
- [x] Green only for setup placement feedback
- [x] Model has no Unity UI references, can be unit tested
- [x] Word rows have variable lengths (3, 4, 5, 6)
- [x] Word rows integrate with WordValidationService for autocomplete
- [x] Grid placement uses 8-direction logic
- [x] Setup mode uses green for valid words and placed letters
- [x] Backspace clears grid placement when word is placed

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

## Phase D Design (Reference Only)

**Note:** Phase D is still in progress. This section contains the original design for reference. Current implementation status is tracked in the main status document.

### Problem Analysis
The legacy uGUI gameplay screen shows both grids side-by-side with guillotines, but this creates several UX issues:
1. **Small touch targets** - Letter tracker buttons are too small on mobile
2. **Cramped layout** - Both grids + guillotines + word rows competing for space
3. **Cognitive overload** - Too much information visible at once
4. **Guillotines underutilized** - Always visible but small, not dramatic

### Design Solution: Focused Single-Grid View with Tab Switching

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

---

**End of Archive**

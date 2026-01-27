# DLYH Troubleshooting Archive

## Purpose

This archive preserves completed or superseded troubleshooting investigations for historical reference.

Entries here should be considered **closed**, meaning:
- Root causes were identified
- Fixes were implemented or decisions accepted
- Learnings were extracted

Archived items should not be edited once moved here.

---

## Archive Entry: UI Toolkit Grid Scaling (WebGL / Mobile)

### Dates
Initial investigation through January 2026

### Summary

Mobile WebGL builds exhibited severe UI overlap and instability when rendering variable-sized grids (6x6–12x12). Initial implementations relied on screen-based sizing assumptions that failed under mobile browser constraints.

---

### Root Causes Identified

- Screen-based sizing (`Screen.height`) unreliable in WebGL
- Ignoring width constraints caused portrait overflow
- Margin mismatch between USS and code
- Flex containers lacked `min-height: 0 / min-width: 0`
- GeometryChangedEvent feedback loop

---

### Solutions Implemented

- Switched to container-aware sizing
- Used both width and height constraints
- Unified margin math
- Added explicit flex min constraints
- Added resize threshold guard
- Locked grid root dimensions post-calculation

---

### Outcome

- Grid resizing stabilized across platforms
- Desktop and mobile rendering consistent
- Remaining issues reclassified as layout composition, not sizing math

---

### Lessons Learned

- WebGL mobile layouts must be container-driven
- Flexbox defaults are unsafe without explicit min constraints
- Geometry callbacks require guardrails
- Instrumentation is essential when remote debugging is unavailable

---

### Status

Closed. Superseded by layout composition investigation (see next entry).

---

## Archive Entry: Layout Composition & Flex-Shrink (Sessions 78-82)

### Dates
January 25-26, 2026

### Platforms Tested
- Desktop (PC browsers, Unity Editor)
- Mobile (Samsung S25 Ultra, Android Chrome)

### Summary

Following the grid sizing stabilization, remaining issues involved layout composition: inconsistent widths between sections, word row button overlap, cell shrinking on narrow viewports, and section overlap in Gameplay screens.

---

### Root Causes Identified

- No shared width constraint between cards, grid, word rows, and keyboard
- Word row buttons inline with letters caused overlap on narrow viewports
- Default `flex-shrink: 1` caused elements to compress instead of triggering scroll
- Grid sizing based on both width AND height caused undersized cells on height-starved viewports
- Grid container centered instead of left-aligned with word rows

---

### Solutions Implemented

1. **Content-Column Wrapper**: Added `.content-column` (max-width: 600px, centered) to wrap all major sections in both Setup and Gameplay screens

2. **Flex-Shrink Prevention**: Added `flex-shrink: 0` to all fixed-size elements:
   - `.content-column`
   - `.table-root`, `.table-row`, `.table-cell`
   - `.word-rows-content`, `.word-row`, `.word-row-letters`, `.word-row-letter-cell`
   - `.player-tabs`, `.grid-area`, `.words-section`
   - `.letter-keyboard`, `.keyboard-row`, `.letter-key`

3. **Width-Only Grid Sizing**: Changed `CalculateContainerAwareCellSize()` to use WIDTH only - height overflow handled by ScrollView

4. **Setup Grid Alignment**: Changed `.table-container` from `align-items: center` to `align-items: flex-start`

5. **Section Spacing**: Added proper margins between sections in Gameplay (6px tabs/grid, 6px grid/words, 8px words/keyboard)

6. **Button Centering**: Added `align-self: center` to `.guessed-words-button`

---

### Outcome

- Grid aligns with word rows (left edges match)
- Cells remain square regardless of viewport size
- Vertical scroll triggers when content exceeds viewport height
- No horizontal scrollbar
- Sections properly spaced without overlap
- All platforms verified working

---

### Issue Classification

| Issue | Classification | Resolution |
|-------|---------------|------------|
| Grid width underutilization | Engineering Bug | Fixed via content-column wrapper |
| Word row button overlap | Engineering Bug | Fixed via vertical word row restructure (Session 81) |
| Card content overflow | Engineering Bug | Fixed via overflow: hidden + text-overflow: ellipsis |
| Horizontal scrollbar | Engineering Bug | Fixed via hidden horizontal scrollbar + proper width constraints |
| Cell overlap/shrinking | Engineering Bug | Fixed via flex-shrink: 0 on all cells |
| Grid/word row misalignment | Engineering Bug | Fixed via align-items: flex-start |
| Section overlap in Gameplay | Engineering Bug | Fixed via margins + flex-shrink: 0 |

---

### Lessons Learned

1. **Flex-shrink: 0 for fixed-size content** - When elements must maintain their size, explicitly set `flex-shrink: 0` to prevent compression. Let ScrollView handle overflow.

2. **Width-only sizing for scrollable content** - When vertical scroll is enabled, compute element sizes based on WIDTH only. Height overflow triggers scroll naturally.

3. **Content-column pattern** - Wrap all major sections in a single width-constrained column to ensure consistent alignment across components.

4. **Align-items: stretch vs flex-start** - Use `stretch` when children should fill width, `flex-start` when children should maintain natural size and align left.

5. **Stable slot measurement** - Measure from parent-allocated containers (content-column, grid-area), not content-driven elements that change size based on their children.

---

### Known Minor Follow-ups (Deferred to Build-Specific Passes)

- Steam build: density/comfort adjustments for larger screens
- Mobile build: minor visual refinements for tap targets

---

### Status

**Closed.** January 26, 2026. Verified on PC and mobile. Minor platform-specific polish deferred to Steam_UI_Polish and Mobile_UI_Polish tickets.

---

## Archive Entry: Private Game (Join Code) Multiplayer — Opponent Data Load & Turn Handoff

### Dates

January 26-27, 2026 (Sessions 84-85)

### Platforms Tested

- Desktop (PC Chrome on tecvoodoo.com)
- Mobile (Samsung S25 Ultra, Android Chrome)

### Summary

Private games using join codes were not synchronizing moves in real-time. When a player made a move, the opponent did not see the update until they manually reloaded the browser. Investigation spanned 5 builds and uncovered multiple issues in the data loading, UI rebuild, and turn handoff logic.

---

### Root Causes Identified

1. **Opponent setup data not fetched on live join** — When opponent joined, UI was already built with placeholder/default values
2. **UI not rebuilt when opponent joins** — `HandleOpponentJoined()` stored data but never rebuilt grids or word rows
3. **Missing RemotePlayerOpponent creation in resume path** — Real multiplayer games couldn't detect opponent moves after resume
4. **DetectOpponentAction checking wrong field** — Was checking `guessedCoordinates` (never populated) instead of `revealedCells`
5. **HandleOpponentThinkingComplete was empty** — Turn never switched back to player after opponent moved
6. **Miss limit using wrong difficulty** — Used inverse of local difficulty instead of opponent's actual difficulty setting
7. **JoinGame path missing word placement decryption** — Joiner had empty opponent data

---

### Solutions Implemented

**Build 3:**
- Fixed `DetectOpponentAction()` to use `revealedCells` instead of `guessedCoordinates`
- Added comprehensive debug logging throughout polling mechanism

**Build 4:**
- Create `RemotePlayerOpponent` in `HandleOpponentJoined()` when opponent setup completes
- Initialize `_lastKnownTurnNumber` in `CreateRemotePlayerOpponentAsync()`
- Update `_lastKnownTurnNumber` after saving state to Supabase

**Build 5:**
- Added `RebuildUIForOpponentJoinAsync()` to rebuild attack grid, word rows, and guess manager when opponent joins
- Updated resume path to create `RemotePlayerOpponent` for real multiplayer games
- Initialize `_lastKnownTurnNumber` from game state on resume

**Build 5b:**
- Decrypt host's `wordPlacementsEncrypted` in JoinGame path for joiner to have opponent data

**Build 5c:**
- Implemented `HandleOpponentThinkingComplete()` to actually switch turn (was logging only)
- Added word row sorting to `SetupGameplayWordRowsWithOpponentData`

**Build 5d:**
- Added `OpponentDifficulty` field to `NetworkingUIResult`
- Capture opponent's actual difficulty from `DLYHPlayerData.difficulty`
- Use opponent's real difficulty for miss limit calculation in all paths

---

### Files Modified

- `UIFlowController.cs` — All turn detection, UI rebuild, and data loading logic
- `RemotePlayerOpponent.cs` — DetectOpponentAction field fix and logging
- `NetworkingUIManager.cs` — Added OpponentDifficulty field

---

### Outcome

- Private games fully functional across PC and mobile
- Multiple turn exchanges confirmed on both devices
- Opponent grids and word rows load correctly on both host and joiner
- Turn switching works correctly via HandleOpponentThinkingComplete
- Miss counts match expectations for both players

---

### Issue Classification

| Issue | Classification | Resolution |
|-------|----------------|------------|
| DetectOpponentAction checking wrong field | Engineering Bug | Fixed (Build 3) |
| _lastKnownTurnNumber initialization | Engineering Bug | Fixed (Build 4) |
| RemotePlayerOpponent creation timing | Engineering Bug | Fixed (Build 4) |
| UI not rebuilt on opponent join | Engineering Bug | Fixed (Build 5) |
| Resume path missing RemotePlayerOpponent | Engineering Bug | Fixed (Build 5) |
| JoinGame path missing word placements | Engineering Bug | Fixed (Build 5b) |
| HandleOpponentThinkingComplete empty | Engineering Bug | Fixed (Build 5c) |
| Word rows not sorted | Engineering Bug | Fixed (Build 5c) |
| Miss limit using wrong difficulty | Engineering Bug | Fixed (Build 5d) |
| Async private game flow | Correct by design | N/A |
| Asymmetric grid sizes (12x12 vs 6x6) | Correct by design | N/A |

---

### Lessons Learned

1. **Opponent data must be loaded before gameplay UI construction** — Cannot build attack grids or word rows without opponent setup
2. **Resume and live join paths must share logic** — Both paths need RemotePlayerOpponent, _lastKnownTurnNumber, and opponent data
3. **Turn detection infrastructure was correct** — The polling mechanism worked; issues were in data loading and event handlers
4. **Event handlers must actually do work** — Empty handlers that only log cause silent failures
5. **Field name consistency matters** — `guessedCoordinates` vs `revealedCells` naming mismatch caused major debugging effort

---

### Guardrails for Future Work

1. No gameplay UI may be constructed without opponent setup data present
2. Resume, JoinGame, and Matchmaking paths must share opponent data-loading logic
3. Turn polling must never start unless a valid RemotePlayerOpponent exists
4. Turn synchronization logic (polling, DetectOpponentAction) is verified working — do not modify

---

### Status

**Closed.** January 27, 2026. Second-AI confirmation received. Verified on PC and mobile with multiple turn exchanges.


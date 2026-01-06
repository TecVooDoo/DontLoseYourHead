# Don't Lose Your Head - Project Status

Project: Don't Lose Your Head (DLYH)  
Developer: TecVooDoo LLC / Rune (Stephen Brandon)  
Engine: Unity 6.3  
Document Version: 9  
Last Updated: January 6, 2026

------------------------------------------------------------
OVERVIEW
------------------------------------------------------------

Don't Lose Your Head is a competitive word-and-grid guessing game inspired by Battleship and Hangman. The core gameplay, AI, audio systems, and telemetry are implemented and working. The current development focus is a full UI system redesign to address usability, scalability, and long-term maintainability.

This document defines the new direction for the project and serves as the authoritative status and execution plan.

------------------------------------------------------------
DEVELOPMENT PRIORITIES (ORDERED)
------------------------------------------------------------

1. Optimization and memory efficiency first
2. UX and player clarity second
3. Future-proofing with SOLID architecture third

------------------------------------------------------------
CURRENT PHASE
------------------------------------------------------------

Phase 3: UI System Rebuild (UI Toolkit + table-based word/grid UI)

This phase supersedes prior UI polish efforts. No major gameplay changes are planned during this phase.

------------------------------------------------------------
UI DIRECTION (LOCKED)
------------------------------------------------------------

- Backend gameplay logic is preserved wherever possible.
- UI implementation is migrating to Unity UI Toolkit.
- A unified table-style UI is used ONLY for:
  - Word rows
  - Column headers
  - Row headers
  - Grid cells
- All other UI elements remain separate panels:
  - Setup wizard fields
  - Guillotine visuals
  - Guessed word list
  - HUD elements

The table is non-virtualized and driven by a single logical data model.

------------------------------------------------------------
COLOR RULES (HARD REQUIREMENTS)
------------------------------------------------------------

- Red and Yellow are reserved system colors and cannot be selected as player colors.
- Green is used ONLY during setup for valid/invalid placement feedback.
- During gameplay, reveal and guess feedback uses the player's chosen color
  (unless the player selected green).
- Red and Yellow may be used for system warnings or errors only.

------------------------------------------------------------
MULTIPLAYER MODEL (CLARIFIED)
------------------------------------------------------------

Local play:
- Local means single-player versus AI ("The Executioner").

Two-player mode (goal: networking):
- Two-player modes are networked, not local pass-and-play.
- Supported modes:
  - Player vs Executioner (networked)
  - Player vs Player (PVP)
- Matchmaking fallback rule:
  - If PVP is selected and no opponent is found within 5 seconds,
    the game spawns a phantom AI with a random player-style name
    instead of The Executioner.
- This behavior mirrors the existing implementation in DAB
  and must be preserved when implemented in DLYH.

------------------------------------------------------------
WHAT WORKS (COMPLETED FEATURES)
------------------------------------------------------------

Core Mechanics:
- Grid placement
- Word entry
- Letter, coordinate, and word guessing
- Miss limit calculation
- Win and loss conditions

AI Opponent ("The Executioner"):
- Adaptive difficulty
- Strategy switching (letter, coordinate, word guesses)
- Memory-based decision making
- Variable think times

Audio:
- Music playback with shuffle and crossfade
- SFX system with mute controls
- Dynamic tempo changes under danger
- Guillotine execution sound sequence

Telemetry:
- Playtest data capture
- Editor-accessible analytics dashboard

------------------------------------------------------------
KNOWN ISSUES
------------------------------------------------------------

- Legacy UI uses multiple independent objects for rows, cells, headers,
  causing sizing and alignment issues.
- Some UI controllers are excessively large and require refactoring.
- Cell vertical stretching issues exist in the current UI implementation.

------------------------------------------------------------
IMPLEMENTATION PLAN
------------------------------------------------------------

PHASE A: FOUNDATION (NO UI REPLACEMENT)
- Implement table data model (cells, states, ownership)
- Centralize color rules
- No visual changes

PHASE B: UI TOOLKIT TABLE MVP
- Build table renderer using UI Toolkit
- Generate all cells once (non-virtualized)
- Update visuals via state changes only
- No per-frame allocations

PHASE C: SETUP WIZARD + PLACEMENT
- Replace monolithic setup screen with guided wizard
- Implement placement logic using the table UI
- Preserve existing placement rules and validation

PHASE D: GAMEPLAY UI CONVERSION
- Convert gameplay grids to table UI
- Wire table interactions to existing gameplay systems
- Preserve AI, audio, and telemetry behavior

PHASE E: NETWORKING PREP (NO IMPLEMENTATION YET)
- Ensure UI supports PVP and Executioner modes
- Implement phantom-AI fallback logic hooks
- No actual networking code in this phase

PHASE F: REFACTOR AND CLEANUP
- Remove legacy uGUI components
- Delete unused UI controllers
- Split oversized classes
- Validate memory usage and allocations

------------------------------------------------------------
PACKAGES
------------------------------------------------------------

Required (already in project):
- DOTween Pro
- UniTask
- New Input System
- Odin Inspector

Optional:
- Feel (screen-level effects only, not required)

------------------------------------------------------------
CODING STANDARDS (ENFORCED)
------------------------------------------------------------

- Prefer async/await (UniTask) over coroutines unless trivial
- Avoid allocations in Update
- No per-frame LINQ
- Clear separation between logic and UI
- ASCII-only documentation and identifiers

------------------------------------------------------------
VERSION HISTORY
------------------------------------------------------------

v9 - Jan 6, 2026
- Rewritten status document
- Locked UI Toolkit + table approach
- Clarified networking vs local play
- Enforced color system rules
- Removed non-ASCII references

v8 - Jan 6, 2026
- Initial UI Toolkit redesign plan

------------------------------------------------------------
END OF DOCUMENT

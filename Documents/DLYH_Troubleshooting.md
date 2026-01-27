# DLYH Troubleshooting (Active)

## Document Purpose

This document defines a repeatable troubleshooting method used to diagnose, track, and resolve complex issues across the DLYH project.

The method is **process-first**, not domain-specific. While the current issue focus may be UI/Layout, this same structure is intended to be reused for:
- UI / Layout
- Audio
- Input
- Networking
- Performance
- Build / Platform-specific issues

Each troubleshooting effort should follow the same lifecycle: observation → hypothesis → intervention → validation → classification.

---

## Troubleshooting Principles

- Evidence over assumption
- Measure real runtime state, not editor expectations
- One variable change per iteration
- Fix root causes, not symptoms
- Explicitly document constraints and tradeoffs

---

## Issue Classification

All discovered issues must be classified into one of the following categories:

- **Engineering Bug** – Incorrect logic, layout, configuration, or implementation
- **System Constraint** – Engine, browser, platform, or hardware limitation
- **Design / UX Decision** – Tradeoffs that require intentional acceptance
- **Unknown** – Requires additional instrumentation or telemetry

Classification determines whether an issue is fixed, deferred, accepted, or redesigned.

---

## Current Investigation

**Issue: Matchmaking Does Not Load Opponent Data**

- **Domain:** Networking
- **Session:** 85 (new investigation)
- **Date opened:** January 27, 2026
- **Status:** Root cause identified, fix needed

---

### Problem Statement

In matchmaking (non-join-code) multiplayer, both players see incorrect opponent data after being matched. The attack grid shows the wrong size, word rows are empty, and moves cannot sync because opponent setup data was never loaded.

---

### Test Configuration

- **Mode:** Matchmaking (public queue)
- **Expected behavior:** After match, both players should have opponent's full setup (grid size, word count, word placements)
- **Actual behavior:** Both players see defaults or duplicated local data

---

### Observations

**Console Evidence (Game AC2FQ2):**
```
[UIFlowController] Networking complete - GameCode: AC2FQ2, IsHost: True, IsPhantomAI: False, Opponent: Opponent
[UIFlowController] Online game with opponent: Opponent, setup not loaded - using defaults
[UIFlowController] CreateAttackModel: gridSize=11, placements=0, tableSize=12x12
[UIFlowController] GuessManager initialized - Player has 18 positions, Opponent has 0 positions, 0 words
```

**Key observations:**
- Both players become "host" simultaneously (`IsHost: True`)
- `OpponentSetupLoaded` is `false`
- Uses defaults (player's own grid size, 0 word placements)
- Neither player goes through JoinGame path

---

### Root Cause Analysis

In matchmaking, both players are matched simultaneously and both take the "Online" code path. Neither goes through the JoinGame path that loads opponent data.

**The flow difference:**

| Path | Private Games | Matchmaking |
|------|---------------|-------------|
| Host creates game | ✓ | ✓ (both) |
| Joiner uses JoinGame path | ✓ (loads host data) | ✗ |
| Host receives HandleOpponentJoined | ✓ (loads joiner data) | ✗ |
| Opponent data loaded | ✓ | ✗ |

**Code Path:**
1. `TransitionToGameplay` sees `GameMode.Online` with `IsHost: True`
2. Checks `_matchmakingResult.OpponentSetupLoaded` which is `false`
3. Falls through to use defaults (player's own grid size, 0 placements)

---

### Affected Code

**File:** `UIFlowController.cs`
**Location:** Lines ~6121-6157 (Online mode handling in `StartGameplayAsync`)

The Online mode branch does not fetch opponent data from Supabase before building the gameplay UI.

---

### Required Fix

For matchmaking, need to fetch opponent's setup data from Supabase before proceeding to gameplay:

1. After match is confirmed, fetch opponent's `DLYHPlayerData` from Supabase
2. Decrypt opponent's `wordPlacementsEncrypted`
3. Build attack grid with opponent's grid size
4. Populate word rows with opponent's words
5. Initialize guess manager with opponent placements

This should use the same logic as the JoinGame path fetches host data.

---

### Guardrails (From Previous Investigation)

The following rules must be maintained:

1. **Do NOT modify turn polling logic** — It is working correctly
2. **Do NOT modify DetectOpponentAction player selection** — It is correct
3. Resume, JoinGame, and Matchmaking paths must share opponent data-loading logic
4. No gameplay UI may be constructed without opponent setup data present

---

### Previous Investigation

**Last closed:** Private Game (Join Code) Multiplayer — Opponent Data Load & Turn Handoff
- **Date closed:** January 27, 2026
- **Sessions:** 84-85
- **Platforms tested:** PC + Mobile (Samsung S25 Ultra)
- **Outcome:** All private game issues resolved (Builds 3-5d)
- **Archive:** See `DLYH_Troubleshooting_Archive.md`

---

## Notes

- Remote USB debugging blocked on Samsung S25 Ultra (ADB authorization never appears)
- On-device overlay is the primary mobile diagnostic tool

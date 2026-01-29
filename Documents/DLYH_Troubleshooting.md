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

**No active investigation**

---

## Previous Investigations

All closed investigations have been archived to `DLYH_Troubleshooting_Archive.md`:

| Investigation | Date Closed | Sessions | Outcome |
|---------------|-------------|----------|---------|
| Matchmaking Does Not Load Opponent Data | Jan 28, 2026 | 86 | Fixed (Build 6) |
| Private Game Multiplayer — Opponent Data & Turn Handoff | Jan 27, 2026 | 84-85 | Fixed (Builds 3-5d) |
| Layout Composition & Flex-Shrink | Jan 26, 2026 | 78-82 | Fixed |
| UI Toolkit Grid Scaling (WebGL/Mobile) | Jan 2026 | Various | Fixed |

---

## Guardrails (From Networking Investigations)

The following rules must be maintained:

1. **Do NOT modify turn polling logic** — It is working correctly
2. **Do NOT modify DetectOpponentAction player selection** — It is correct
3. Resume, JoinGame, and Matchmaking paths must share opponent data-loading logic
4. No gameplay UI may be constructed without opponent setup data present

---

## Notes

- Remote USB debugging blocked on Samsung S25 Ultra (ADB authorization never appears)
- On-device overlay is the primary mobile diagnostic tool

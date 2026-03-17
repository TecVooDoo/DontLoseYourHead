# DLYH - Project Status

**Project:** Don't Lose Your Head (DLYH)
**Platform:** Unity 6.3 (6000.3.11f1), UI Toolkit
**DLYH Root:** `Assets/DLYH/`
**Scene:** NetworkingScene.unity
**Codebase:** ~65 scripts, ~33,000 lines, 15 namespaces
**Supabase:** Direct MCP access available (game_sessions, session_players, players tables)

**Reference docs:** `DLYH_DevReference.md` (architecture, standards, lessons), `DLYH_CodeReference.md` (script API)

---

## Current Work

**Phase E Networking -- COMPLETE (Sessions 1-8).** All 8 sessions done: editor identity, phantom AI, game state persistence, opponent join detection, turn synchronization, activity tracking & auto-win, code quality & polish. Session 7 (Rematch UI) deferred indefinitely. ~1,800+ lines dead code removed in Session 8. Namespace standardized to `DLYH.*`. XOR encryption with salt replaced Base64 for word placements.

**Compile status:** Clean. 9 obsolete files deleted across Sessions 8-90 (NetworkGameManager, WaitingRoomController, GameStateSynchronizer, RematchService, UIButtonAudio, OpponentFactory, ConnectionStatusIndicator, MultiplayerLobbyController, TableViewTest).

**MCP status:** Unity MCP (v0.47.1) via stdio on port 56056. Supabase MCP and Cloudflare MCP (9 servers) connected via mcp-remote.

**Deploy:** Cloudflare Pages at dlyh.pages.dev. Deploy via `npx wrangler pages deploy "C:/Unity/DontLoseYourHead/Builds/dlyh" --project-name dlyh`.

**Needs next:**
- Phase F: Essential animations (guillotine blade drop, head fall), screen transitions
- Phase G: Mobile browser testing, verify telemetry

---

## Build Checklist

Before each build:
1. **Update version number** in Player Settings (Edit > Project Settings > Player > Version)
   - Current: 3.1 (last known)
   - Format: Major.Minor (increment minor for each test build)
2. **Debug overlay** is currently DISABLED (line 443 in UIFlowController.cs)
   - To re-enable for layout debugging: uncomment `CreateGlobalDebugOverlay();`
3. Version displays on main menu via `Application.version`

---

## Sessions

**Session 90 (Feb 20, 2026):** Legacy cleanup (5 dead files: UIButtonAudio, OpponentFactory, ConnectionStatusIndicator, MultiplayerLobbyController, TableViewTest). Fixed AI solo play bug (ExecutionerAI guess firing order). CodeReference expanded from ~15 networking scripts to all ~65 scripts organized by namespace. DevReference script inventory updated to 50 entries with corrected line counts. Refactoring assessment: no changes needed.

**Session 89 (Feb 20, 2026):** Head persistence + guillotine integration. HeadIndex threaded through PlayerSetupData, DLYHPlayerData, GuillotineData, and all 3 GuillotineData construction sites in UIFlowController. Guillotine overlay heads replaced with layered modular textures (hair-back -> head -> face -> hair). 6 stage-based face expressions per character (neutral/worried/scared/horrified/dead/evil). Woman 1 gets 4-layer hair system (hair_back + hair_front). HeadCharacterData.asset rewired with 36 face GUIDs. PlayerPrefs persistence for head selection. Deployed to Cloudflare Pages.

**Session 88 (Feb 20, 2026):** MCP setup (Unity MCP v0.47.1 + Supabase + 8 Cloudflare servers). Document system refactor (DevReference, CodeReference, Migration Manifest, GDD folder). Head picker feature (HeadCharacterData SO, UXML/USS, SetupWizardUIManager wiring, 6 characters with modular layers). Cloudflare Pages deploy via wrangler CLI.

**Session 87 (Jan 28, 2026):** Session 8: Code Quality & Polish. Dead code removal (~1,800+ lines: NetworkGameManager, WaitingRoomController, GameStateSynchronizer, RematchService). Error handling polish (StatusType.Error enum, retry logic with 500ms delay). XOR cipher encryption with salt replaced Base64 for word placements. Namespace cleanup (standardized to DLYH.*). Cleaned RemotePlayerOpponent dead methods.

**Session 86 (Jan 28, 2026):** Build 6b + Session 6: Activity Tracking & Auto-Win. Fixed matchmaking race condition (opponent setup fetch timing). Implemented inactivity tracking: Supabase edge function `check-inactivity` for 5-day auto-forfeit, pg_cron daily job, client-side inactivity check on resume, version guarding in save.

**Session 85 (Jan 27, 2026):** Build 5c/5d. Miss limit calculation using opponent difficulty. Turn switching + word row sorting fixes.

**Session 84 (Jan 27, 2026):** Build 4. Turn tracking fixes, opponent data loading issue identified.

**Session 83 (Jan 27, 2026):** Session 5: Turn Synchronization. Polling-based turn detection (2s interval), RemotePlayerOpponent wired with InitializeWithExistingService(), ProcessStateUpdate() for detecting opponent actions. Fixed polling to use revealedCells instead of guessedCoordinates.

**Session 82 (Jan 26, 2026):** Session 4: Opponent Join Detection. 3-second polling for opponent join, UI updates, waiting state handling.

---

## Phase Progress

**Phase A: Core Mechanics -- COMPLETE.** Grid placement, word entry, letter/coordinate/word guessing, miss limit formula, win/loss detection, extra turn on word completion.

**Phase B: AI Opponent ("The Executioner") -- COMPLETE.** Adaptive difficulty with rubber-banding, strategy switching, memory-based decisions, variable think times (0.8-2.5s).

**Phase C: Audio -- COMPLETE (polish deferred).** Music playback with shuffle/crossfade, SFX with mute controls, dynamic tempo under danger, guillotine execution sequence.

**Phase D: UI Toolkit -- COMPLETE.** TableModel/TableView data-driven grid, setup wizard, word rows (3-6 length), autocomplete, 8-direction placement, main menu, hamburger nav, attack/defend tabs, guillotine overlay (5-stage), game end sequence, How to Play modal.

**Phase E: Networking & Auth -- COMPLETE (8 of 8 sessions).** Online mode (Find Opponent / Private Game), matchmaking overlays, phantom AI fallback, Supabase game sessions, "My Active Games" list, resume game, turn synchronization (polling), activity tracking & auto-win, code quality polish.

**Phase F: Cleanup & Polish -- IN PROGRESS.**
- [x] Delete abandoned prefabs/scripts (5 dead files removed in Session 90)
- [x] CodeReference expanded to all ~65 scripts, DevReference inventory updated (Session 90)
- [ ] Essential animations (guillotine blade drop, head fall)
- [ ] Screen transitions
- [x] Character art update: head picker in setup wizard (6 characters, modular layers, hair tinted by player color)
- [x] Head persistence: PlayerPrefs (local) + Supabase headIndex (multiplayer)
- [x] Guillotine overlay: layered modular heads replace static placeholder PNGs
- [x] Face expressions: 6 per character, change with guillotine stage (1-4 during play, 5/6 game over)
- [x] Woman 1: 4-layer hair system (hair_back behind head, hair_front on top)

**Phase G: Deploy to Playtest -- IN PROGRESS.**
- [x] WebGL build pipeline
- [x] Deploy to Cloudflare (dlyh.pages.dev) -- wrangler CLI configured
- [ ] Mobile browser testing & fixes
- [ ] Verify telemetry working

---

## Known Issues

| Issue | Severity | Status | Notes |
|-------|----------|--------|-------|
| Find Opponent games fail to resume | High | Fixed | Tested and passed (Session 89) |
| WebGL realtime incomplete | Medium | By design | WebSocket bridge missing; using polling instead (Session 5) |
| Music crossfading too frequent | Low | Noted | Should only switch at end of track |
| Man 1 head/hair misaligned with faces | Low | Fixed | Head and hair PNGs realigned with face PNGs (Session 89) |
| Minor UI polish items | Low | Deferred | See future Steam_UI_Polish.md and Mobile_UI_Polish.md |

---

## Session Close Checklist

- [ ] Update session summary (1-2 lines)
- [ ] Update phase progress if changed
- [ ] Update known issues if changed
- [ ] Update `DLYH_CodeReference.md` if APIs changed
- [ ] Update `DLYH_DevReference.md` if architecture/standards changed

---

**End of Project Status**

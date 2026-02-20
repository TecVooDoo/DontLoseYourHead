# DLYH - Development Reference

**Purpose:** Stable reference for DLYH architecture, coding standards, and conventions. Read on demand, not every session.
**Last Updated:** February 20, 2026

---

## Architecture

### Namespaces

| Namespace | Purpose | Status |
|-----------|---------|--------|
| `DLYH.TableUI` | Main UI scripts (UIFlowController, TableView, etc.) | Active |
| `DLYH.Networking` | Opponent abstraction, IOpponent, RemotePlayerOpponent | Active |
| `DLYH.Networking.Services` | Supabase services (Auth, GameSession, etc.) | Active |
| `DLYH.Networking.UI` | Networking overlays (matchmaking, waiting room) | Active |
| `DLYH.UI.Managers` | Extracted UI managers (GameStateManager, ActiveGamesManager) | Active |
| `DLYH.UI.Services` | UI services (WordValidationService) | Active |
| `DLYH.AI.Core` | AI controllers (ExecutionerAI, MemoryManager) | Active |
| `DLYH.AI.Strategies` | AI guess strategies | Active |
| `DLYH.AI.Config` | AI configuration ScriptableObjects | Active |
| `DLYH.AI.Data` | AI data structures (GridAnalyzer, LetterFrequency) | Active |
| `DLYH.Audio` | Audio managers (UIAudioManager, MusicManager) | Active |
| `DLYH.Core.Utilities` | Shared utilities (JsonParsingUtility) | Active |
| `DLYH.Core.GameState` | Game state (WordPlacementData, DifficultySO, enums) | Active |
| `DLYH.Telemetry` | Playtest telemetry | Active |
| `DLYH.Editor` | Editor tools (WordBankImporter, TelemetryDashboard) | Active |

### Folder Structure

```
Assets/DLYH/
  Scripts/
    AI/           - ExecutionerAI, strategies, config
    Audio/        - UIAudioManager, MusicManager, GuillotineAudioManager
    Core/
      Utilities/  - JsonParsingUtility (Phase 3)
    Networking/   - IOpponent, services
    UI/
      Managers/   - GameStateManager, ActiveGamesManager, modals (Phase 3)
      ...         - TableModel, TableView, UIFlowController, etc.
  UI/             - UI Toolkit assets (UXML, USS)
  Scenes/
    NetworkingScene.unity  - Primary active scene
    NetworkingBackup.unity - Backup before networking work
```

### Opponent Abstraction

Game logic is opponent-agnostic. Whether AI, Phantom AI, or Remote Player:
1. Fires `OnOpponentTurnStarted` event
2. Waits for guess via `IOpponent.OnLetterGuess` / `OnCoordinateGuess` / `OnWordGuess`
3. Processes guess using SAME code path as player guesses
4. Updates UI using `IOpponent.OpponentColor`
5. Fires `OnOpponentTurnEnded` event

**Important:** Use `_opponent` (not `_aiOpponent`), handlers are `HandleOpponent*`, `CellOwner` only has `Player` and `Opponent`.

---

## Coding Standards

- No `var` keyword -- explicit types always
- Prefer async/await (UniTask) over coroutines
- No per-frame allocations, no per-frame LINQ
- ASCII-only in docs and identifiers
- Clear separation between logic and UI
- File target: 800-1200 lines (extract when exceeding with clear responsibilities)

### Refactoring Rules

**Golden rule:** Don't refactor for the sake of refactoring. Priority order: Memory > SOLID > Self-documenting > Clean > Reusability.

**When TO refactor:** > 1200 lines AND has separable responsibilities, repeated code patterns across 3+ files, performance bottleneck identified by profiling, API is confusing to use correctly.

**When NOT TO refactor:** To hit a line count target, single-use helpers, code that is cohesive and naturally together, "might need it later" abstractions, during a feature milestone (do between milestones).

| Line Count | Action |
|------------|--------|
| < 500 | Leave alone unless clear SRP violation |
| 500-800 | Monitor, no action needed |
| 800-1200 | OPTIMAL -- Claude works effectively here |
| 1200-1500 | Review for extraction opportunities |
| > 1500 | Strong refactor candidate |

### Memory Efficiency

Zero-allocation hot paths required:

- **No per-frame allocations:** No `new List<T>()`, no string `+` concat, no LINQ, no `.ToArray()`/`.ToList()` in Update/FixedUpdate
- **No per-action allocations:** No allocations per guess, per turn, per UI refresh
- **Reuse collections:** Pool frequent lists, use `Array.Empty<T>()` for empty returns, pre-allocate when size is known
- **String efficiency:** `StringBuilder` for multi-part building, cache formatted strings, no string parsing in hot paths

### SOLID Principles (DLYH-Specific)

- **SRP:** Controllers coordinate flow. Managers handle one domain. Don't combine UI + input + state + audio in one class
- **OCP:** Use ScriptableObjects for data-driven behavior, strategy pattern for AI variants, events for loose coupling
- **LSP:** All opponents must behave as IOpponent. Avoid `if (opponent is RemotePlayerOpponent)` special-case checks
- **ISP:** Small focused interfaces over large ones
- **DIP:** Events for system communication. Inject dependencies through serialized fields. Avoid static manager refs

---

## Development Priorities (Ordered)

1. **SOLID principles first**
2. **Memory efficiency second** -- no per-frame allocations
3. **Clean code third** -- readable, maintainable
4. **Self-documenting code fourth** -- clear naming over comments
5. **Platform best practices fifth** -- Unity > C#, Cloudflare/Supabase > HTML/JS

---

## Script Inventory

**~65 scripts, ~33,000 lines.** Full API reference in `DLYH_CodeReference.md`.

| Script | Lines | Purpose |
|--------|-------|---------|
| UIFlowController | ~7629 | Screen flow + gameplay orchestration |
| GameplayScreenManager | ~1437 | Gameplay UI, tabs, keyboard, miss counters |
| WordRowView | ~1115 | Single word row (setup + gameplay + guess mode) |
| GameSessionService | ~1100 | Supabase game session CRUD |
| SetupWizardController | ~943 | Standalone setup wizard (MonoBehaviour) |
| GameplayGuessManager | ~919 | Guess processing, win detection |
| SetupWizardUIManager | ~820 | Managed setup wizard (used by UIFlowController) |
| NetworkingUIManager | ~815 | Networking overlay management |
| TableView | ~804 | UI Toolkit table rendering |
| PlacementAdapter | ~802 | Word placement on grid |
| WordRowsContainer | ~749 | Word rows collection management |
| MusicManager | ~669 | Background music, shuffle, crossfade, tempo |
| AISetupManager | ~542 | AI word selection and placement |
| AuthService | ~500 | Supabase auth (anon, OAuth, magic link) |
| ExecutionerAI | ~495 | AI opponent coordination |
| GuillotineOverlayManager | ~450 | Guillotine overlay, layered heads, faces |
| GridAnalyzer | ~442 | AI grid analysis utilities |
| UIAudioManager | ~415 | UI SFX playback |
| ExecutionerConfigSO | ~412 | AI configuration ScriptableObject |
| MatchmakingService | ~400 | Matchmaking queue, phantom AI fallback |
| GameStateManager | ~400 | Game state parsing, XOR encryption |
| IGuessStrategy | ~383 | AI game state + strategy interface |
| JsonParsingUtility | ~365 | Manual JSON parsing |
| ActiveGamesManager | ~360 | My Active Games list |
| DifficultyAdapter | ~358 | AI rubber-banding |
| WordGuessStrategy | ~339 | AI word guessing |
| LetterGuessStrategy | ~327 | AI letter selection |
| WordSuggestionDropdown | ~310 | Autocomplete dropdown |
| DifficultyCalculator | ~309 | Miss limit formula |
| MemoryManager | ~303 | AI memory with skill-based forgetting |
| PlayerService | ~300 | Player records |
| PlaytestTelemetry | ~300 | Cloudflare Worker telemetry |
| TableModel | ~287 | Table data model |
| CoordinateGuessStrategy | ~250 | AI coordinate selection |
| ColorRules | ~251 | Color constants and utilities |
| HelpModalManager | ~233 | How to Play modal |
| LetterFrequency | ~212 | English letter frequency data |
| GuillotineAudioManager | ~200 | Guillotine-specific sounds |
| DifficultySO | ~200 | Difficulty ScriptableObject |
| TableLayout | ~191 | Grid layout with regions |
| ConfirmationModalManager | ~191 | Confirmation dialog |
| GameplayStateTracker | ~147 | Gameplay state tracking |
| MainMenuController | ~126 | Main menu |
| WordValidationService | ~98 | Word bank validation |
| TableRegion | ~82 | Grid region struct |
| WordListSO | ~81 | Word list ScriptableObject |
| SFXClipGroup | ~73 | Audio clip group |
| SettingsPanel | ~59 | Volume settings (PlayerPrefs) |
| DifficultyEnums | ~37 | Enums (GridSize, Difficulty, WordCount) |
| WordPlacementData | ~32 | Word placement data struct |

---

## Dependencies (External)

| Dependency | Used By | Notes |
|------------|---------|-------|
| UniTask | Async flow | `Cysharp.Threading.Tasks` -- prefer over coroutines |
| DOTween / DOTweenPro | Animations | Kill tweens before reset, `DOTween.Kill(gameObject)` |
| Odin Inspector | Editor workflow | Serialization, inspector enhancement |
| Supabase (via REST) | Networking | Auth, game sessions, player data |
| UI Toolkit | All UI | UXML + USS, no uGUI |

### UniTask Pattern
```csharp
using Cysharp.Threading.Tasks;

public async UniTask DoSomethingAsync()
{
    await UniTask.Delay(1000);
}

// Fire and forget
DoSomethingAsync().Forget();
```

### DOTween Pattern
```csharp
using DG.Tweening;

// Kill tweens before reset
DOTween.Kill(gameObject);

// Animate
transform.DOScale(1.2f, 0.3f).SetEase(Ease.OutBack);
```

---

## Game Rules

### Core Concept
DLYH is Battleship with words. Both players see Attack and Defend boards -- Attack shows opponent's hidden words (where you guess), Defend shows your words (what opponent found).

### Turn Flow
1. First turn random
2. Make ONE action: pick letter, pick coordinate, or guess word
3. Turn switches (except: extra turn on word completion)

### Actions

**Pick Letter:** Hit = letter fills in word rows (yellow or player color based on coordinate knowledge). Miss = +1 miss.

**Pick Coordinate:** Hit = cell highlights (yellow if letter unknown, player color if known). Miss = +1 miss.

**Guess Word:** Correct = word fills in + extra turn. Incorrect = +2 misses.

### Color Rules
- Red = Miss
- Yellow = Hit but incomplete (letter OR coordinate unknown)
- Player Color = Fully known (both letter AND coordinate)

### Miss Limit Formula
```
MissLimit = 15 + OpponentGridBonus + OpponentWordModifier + YourDifficultyModifier

OpponentGridBonus: 6x6=+3, 7x7=+4, 8x8=+6, 9x9=+8, 10x10=+10, 11x11=+12, 12x12=+13
OpponentWordModifier: 3 words=+0, 4 words=-2
YourDifficultyModifier: Easy=+4, Normal=+0, Hard=-4
```

### Win Conditions
1. Reveal ALL opponent's words AND grid coordinates
2. Opponent reaches miss limit
3. (Online) Opponent abandons after 5 days

---

## Implementation Lessons

### Unity/C# Patterns
| # | Lesson | Source |
|---|--------|--------|
| 1 | Set state BEFORE firing events -- handlers may check state immediately | DLYH UI |
| 2 | Initialize UI to known states -- don't rely on defaults | DLYH UI |
| 3 | Kill DOTween before reset -- prevents animation conflicts | DLYH Gameplay |
| 4 | Store original positions -- for proper reset after animations | DLYH Gameplay |
| 5 | Use New Input System -- `Keyboard.current`, not `Input.GetKeyDown` | Unity 6 |
| 6 | No `var` keyword -- explicit types always | Standard |
| 7 | 800 lines max -- extract controllers when approaching limit | Refactoring |
| 8 | Prefer UniTask over coroutines -- `await UniTask.Delay(1000)` | Standard |
| 9 | No allocations in Update -- cache references, use object pooling | Performance |
| 10 | Validate after MCP edits -- run validate_script to catch syntax errors | MCP |

### Project-Specific
| # | Lesson | Source |
|---|--------|--------|
| 11 | Unity 6 UIDocument bug (IN-127759) -- assign Source Asset to prevent blue screen | Unity 6 |
| 12 | Check scene file diffs -- layout can be accidentally modified | Unity |
| 13 | Reuse existing systems -- create thin adapters instead of rebuilding | Architecture |
| 14 | Prevent duplicate event handlers -- use flags like `_keyboardWiredUp` | DLYH UI |
| 15 | Reset validity on clear -- SetWordValid(false) when clearing words | DLYH Gameplay |
| 16 | Case-sensitivity in char comparisons -- always ToUpper() both sides | DLYH Gameplay |
| 17 | Supabase anon vs secret keys -- secret keys blocked in browsers; use anon key (JWT) | Supabase |
| 18 | Iframe navigation -- use `window.top.location` to escape iframe context | WebGL |
| 19 | UI Toolkit stable slot measurement -- never measure from content-driven elements; use parent-allocated stable slots | UI Toolkit |
| 20 | Unity UI Toolkit ScrollView internals -- use ID selectors (#unity-content-viewport) not class selectors | UI Toolkit |
| 21 | MCP stable config: `.mcp.json` uses command-spawn with stdio transport. `keepServerRunning: false` prevents port conflicts | MCP |

---

## Bug Patterns to Avoid

| Bug Pattern | Cause | Prevention |
|-------------|-------|------------|
| State set AFTER events | Handlers see stale state | Set state BEFORE firing events |
| Autocomplete floating at top | Not hidden at init | Call Hide() in Initialize() |
| Board not resetting | No reset logic | Call ResetGameplayState() on new game |
| Guillotine head stuck | No stored position | Store original position on Initialize |
| Green cells after clear | Validity not reset | SetWordValid(false) in HandleWordCleared |
| Old screen still visible | Only showing new | Hide ALL other screens when showing new |
| Unicode not rendering in WebGL | Font support | Use ASCII fallbacks |
| Letter comparison fails | Case mismatch | ToUpper() both sides in comparisons |

---

## AI Rules

1. **Read DLYH_CodeReference.md first** -- check existing APIs before writing new code
2. **Working directory is C:\Unity\DontLoseYourHead** -- NOT backup folders
3. **Verify names exist** -- search before referencing files/methods/classes
4. **Step-by-step verification** -- one step at a time, wait for confirmation
5. **Read before editing** -- always read files before modifying
6. **ASCII only** -- no smart quotes, em-dashes, or special characters
7. **Be direct** -- give honest assessments, don't sugar-coat
8. **Acknowledge gaps** -- say explicitly when something is missing or unclear
9. **Flag broken systems** -- if a process, tool, or workflow stops working correctly, raise it immediately
10. **Flag stale docs** -- if a document becomes obsolete or contradicts reality, bring it up for revision

---

## Reference Documents

| Document | Purpose | When to Read |
|----------|---------|--------------|
| `DLYH_CodeReference.md` | Full script API reference | When working with specific scripts |
| `DLYH_NetworkingPlan_Phase_E_Updated.md` | Phase E implementation plan (REVISED) | When working on networking |
| `DLYH_Migration_Manifest.md` | Dependencies and packages | When adding/evaluating packages |
| `DLYH_Troubleshooting.md` | Active troubleshooting methodology | When investigating issues |
| `GDD/DLYH_GDD.md` | Game design document | When design questions arise |
| `DLYH_Status_Archive.md` | Historical designs, DAB patterns, old versions | When historical context needed |
| `DLYH_Troubleshooting_Archive.md` | Closed investigations | When checking if an issue was solved before |

---

**End of Development Reference**

# DLYH -- Dependency Manifest

## Project Dependency Reference

**Purpose:** Definitive list of every package and asset DLYH actually uses. Verified against code (`using` statements, manifest references) -- not speculative.

**Last Updated:** February 20, 2026

---

## Unity Registry Packages

Core packages from Unity Package Manager used by DLYH code or UI.

| Package | Registry ID | Used By | Notes |
|---------|------------|---------|-------|
| Universal Render Pipeline | `com.unity.render-pipelines.universal` 17.3.0 | Everything visual | URP template default |
| Input System | `com.unity.inputsystem` 1.18.0 | Keyboard input | New Input System (`Keyboard.current`) |
| Cinemachine | `com.unity.cinemachine` 3.1.5 | Camera | Main camera control |
| Newtonsoft JSON | `com.unity.nuget.newtonsoft-json` 3.2.2 | Networking | JSON serialization for Supabase |
| UI Toolkit | `com.unity.modules.uielements` 1.0.0 | All UI | UXML + USS, no uGUI (module) |
| TextMeshPro | (via uGUI/modules) | UI text | HUD and overlay text |
| Timeline | `com.unity.timeline` 1.8.10 | Animations | Guillotine sequences |
| Test Framework | `com.unity.test-framework` 1.6.0 | Testing | Unit tests |

### 2D Packages (Template Default)

Included from project template. May not all be actively used.

| Package | Registry ID |
|---------|------------|
| 2D Animation | `com.unity.2d.animation` 13.0.4 |
| 2D Aseprite | `com.unity.2d.aseprite` 3.0.1 |
| 2D Sprite | `com.unity.2d.sprite` 1.0.0 |
| 2D SpriteShape | `com.unity.2d.spriteshape` 13.0.0 |
| 2D Tilemap | `com.unity.2d.tilemap` 1.0.0 |
| 2D Tilemap Extras | `com.unity.2d.tilemap.extras` 6.0.1 |
| 2D PSD Importer | `com.unity.2d.psdimporter` 12.0.1 |
| 2D Enhancers | `com.unity.2d.enhancers` 1.0.0 |

### Multiplayer Packages

| Package | Registry ID | Notes |
|---------|------------|-------|
| Multiplayer Center | `com.unity.multiplayer.center` 1.0.1 | Unity multiplayer tools |
| Multiplayer Quickstart | `com.unity.multiplayer.center.quickstart` 1.1.1 | Setup assistant |
| Multiplayer Play Mode | `com.unity.multiplayer.playmode` 2.0.1 | Multi-instance testing |

---

## Local Packages

| Package | Path | Notes |
|---------|------|-------|
| UniTask | `file:E:/Unity/DefaultUnityPackages/com.cysharp.unitask` | Async/await for Unity. Local file reference (not registry) |

---

## Asset Store Packages -- Runtime Required

Must be imported from Asset Store. DLYH code directly references these.

| Package | What DLYH Uses | Notes |
|---------|---------------|-------|
| **DOTween + DOTweenPro** (Demigiant) | UI animations, scale/fade/move tweens, guillotine sequences | `using DG.Tweening;` throughout UI code |
| **Odin Inspector** (Sirenix) | Editor workflow, serialization, inspector enhancement | `Sirenix.OdinInspector` attributes in data classes |

---

## OpenUPM Packages

| Package | Registry ID | Notes |
|---------|------------|-------|
| MCP for Unity | `com.ivanmurzak.unity.mcp` 0.47.1 | Claude Code integration via stdio. Scoped registry: package.openupm.com |

---

## Development Tools

Editor-only. Not shipped with game.

| Package | Purpose | Required? |
|---------|---------|-----------|
| MCP for Unity (OpenUPM) | Claude Code integration, script execution | Yes (dev workflow) |
| Visual Studio IDE | `com.unity.ide.visualstudio` 2.0.27 | Yes (IDE integration) |
| AI Inference | `com.unity.ai.inference` 2.4.1 | Unity AI tools |

---

## Backend Services (Not Unity Packages)

| Service | Purpose | Access Method |
|---------|---------|---------------|
| **Supabase** | Auth, game sessions, player data | REST API via UnityWebRequest + anon JWT |
| **Cloudflare Workers** | Telemetry endpoint | HTTP POST from WebGL/PC |
| **Cloudflare Pages** | WebGL hosting (dlyh.pages.dev) | Deploy target |

### Cloudflare Pages Deployment

- **Project:** `dlyh`
- **Production URL:** https://dlyh.pages.dev
- **Dashboard:** https://dash.cloudflare.com/08cb6244511ae30e4fbef33d1d88aec0/pages/view/dlyh
- **Account:** Runeduvall@tecvoodoo.com (ID: `08cb6244511ae30e4fbef33d1d88aec0`)
- **Build output:** `C:\Unity\DontLoseYourHead\Builds\dlyh` (Unity WebGL build)
- **Deploy command:** `npx wrangler pages deploy "C:/Unity/DontLoseYourHead/Builds/dlyh" --project-name dlyh`
- **Auth:** Wrangler OAuth (auto-refreshes; run `npx wrangler login` if expired)

### MCP Servers (Claude Code Integration)

| Server | Purpose |
|--------|---------|
| `ai-game-developer` | Unity Editor integration (scene, scripts, assets) via stdio |
| `supabase` | Supabase project management, SQL execution, migrations |
| `cloudflare-docs` | Cloudflare documentation search |
| `cloudflare-workers-bindings` | Workers and bindings management |
| `cloudflare-workers-builds` | Workers build management |
| `cloudflare-observability` | Monitoring and observability |
| `cloudflare-logpush` | Log management |
| `cloudflare-autorag` | AutoRAG management |
| `cloudflare-casb` | CASB security scanning |
| `cloudflare-graphql` | Analytics via GraphQL API |

---

## Assembly Structure

DLYH does not use separate assembly definitions (asmdefs). All scripts compile into Assembly-CSharp. Namespace organization provides logical separation:

```
Assembly-CSharp
  |
  +-- DLYH.TableUI        (UI flow, gameplay orchestration)
  +-- DLYH.Networking      (opponent abstraction)
  +-- DLYH.Networking.Services  (Supabase CRUD)
  +-- DLYH.Networking.UI   (matchmaking overlays)
  +-- DLYH.UI.Managers     (extracted managers)
  +-- DLYH.UI.Services     (word validation)
  +-- DLYH.AI.Core         (AI controllers)
  +-- DLYH.AI.Strategies   (AI strategies)
  +-- DLYH.AI.Config       (AI ScriptableObjects)
  +-- DLYH.AI.Data         (AI data structures)
  +-- DLYH.Audio           (audio managers)
  +-- DLYH.Core.Utilities  (shared utilities)
  +-- DLYH.Core.GameState  (game state, difficulty)
  +-- DLYH.Telemetry       (playtest telemetry)

Assembly-CSharp-Editor
  +-- DLYH.Editor          (editor tools)
```

---

**End of Dependency Manifest**

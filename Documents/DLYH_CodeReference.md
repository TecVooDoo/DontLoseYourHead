# DLYH - Code Reference

**Purpose:** Complete API reference for the DLYH codebase. Per-script entries with public API, usage notes, and gotchas. Check this before writing new code to avoid referencing non-existent classes or methods.

**Last Updated:** February 20, 2026 (Session 89 -- full codebase expansion)

**Working Directory:** `C:\Unity\DontLoseYourHead`

---

## Table of Contents

1. [Namespaces](#namespaces)
2. [Core / GameState](#core--gamestate)
3. [Core / Utilities](#core--utilities)
4. [Table UI](#table-ui)
5. [UI Controllers](#ui-controllers)
6. [UI Managers](#ui-managers)
7. [UI Services](#ui-services)
8. [AI](#ai)
9. [Audio](#audio)
10. [Networking](#networking)
11. [Networking Services](#networking-services)
12. [Networking UI](#networking-ui)
13. [Telemetry](#telemetry)
14. [Data Models](#data-models)
15. [Supabase Tables](#supabase-tables)
16. [Scene Structure](#scene-structure)
17. [Usage Patterns](#usage-patterns)
18. [File Locations](#file-locations)

---

## Namespaces

| Namespace | Purpose | Status |
|-----------|---------|--------|
| `DLYH.TableUI` | Main UI scripts (UIFlowController, TableView, etc.) | Active |
| `DLYH.Networking` | Opponent abstraction, IOpponent, RemotePlayerOpponent | Active |
| `DLYH.Networking.Services` | Supabase services (Auth, GameSession, etc.) | Active |
| `DLYH.Networking.UI` | Networking overlays (matchmaking, waiting room) | Active |
| `DLYH.UI.Managers` | Extracted UI managers (GameStateManager, ActiveGamesManager) | Active |
| `DLYH.UI.Services` | UI services (WordValidationService) | Active |
| `DLYH.UI` | Audio volume settings (SettingsPanel) | Active |
| `DLYH.AI.Core` | AI controllers (ExecutionerAI, MemoryManager) | Active |
| `DLYH.AI.Strategies` | AI guess strategies | Active |
| `DLYH.AI.Config` | AI configuration ScriptableObjects | Active |
| `DLYH.AI.Data` | AI data structures (GridAnalyzer, LetterFrequency) | Active |
| `DLYH.Audio` | Audio managers (UIAudioManager, MusicManager) | Active |
| `DLYH.Core.Utilities` | Shared utilities (JsonParsingUtility) | Active |
| `DLYH.Core.GameState` | Game state (WordPlacementData, DifficultySO, enums) | Active |
| `DLYH.Telemetry` | Playtest telemetry | Active |
| `DLYH.Editor` | Editor tools (WordBankImporter, TelemetryDashboard) | Active |

---

## Core / GameState

### DifficultyEnums.cs (~37 lines)
**Path:** `Assets/DLYH/Scripts/Core/DifficultyEnums.cs`
**Namespace:** `DLYH.Core.GameState`

```
enum GridSizeOption   { Size6x6=6, Size7x7=7, Size8x8=8, Size9x9=9, Size10x10=10, Size11x11=11, Size12x12=12 }
enum DifficultySetting { Hard, Normal, Easy }
enum WordCountOption   { Three=3, Four=4 }
```

### WordPlacementData.cs (~32 lines)
**Path:** `Assets/DLYH/Scripts/Core/WordPlacementData.cs`
**Namespace:** `DLYH.Core.GameState`

| Property | Type | Description |
|----------|------|-------------|
| `Word` | string | The placed word |
| `StartCol` | int | Starting column (0-indexed) |
| `StartRow` | int | Starting row (0-indexed) |
| `DirCol` | int | Column direction (1=right, 0=none) |
| `DirRow` | int | Row direction (1=down, 0=none) |
| `RowIndex` | int | Word slot index |

### DifficultySO.cs (~200 lines)
**Path:** `Assets/DLYH/Scripts/Core/DifficultySO.cs`
**Namespace:** `DLYH.Core.GameState`
**Type:** ScriptableObject

| Property | Type | Description |
|----------|------|-------------|
| `DifficultyName` | string | Display name |
| `GridSizeOption` | GridSizeOption | Grid size enum |
| `WordCountOption` | WordCountOption | Word count enum |
| `Difficulty` | DifficultySetting | Difficulty enum |
| `GridSize` | int | Grid size as int |
| `WordCount` | int | Word count as int |
| `RequiredWordLengths` | int[] | Word lengths for this config |

| Method | Returns | Description |
|--------|---------|-------------|
| `CalculateMissLimitVsOpponent(int, int)` | int | Miss limit vs opponent settings |
| `SetConfiguration(GridSizeOption, WordCountOption, DifficultySetting)` | void | Set config at runtime |

### WordListSO.cs (~81 lines)
**Path:** `Assets/DLYH/Scripts/Core/WordListSO.cs`
**Namespace:** `DLYH.Core.GameState`
**Type:** ScriptableObject

| Property | Type | Description |
|----------|------|-------------|
| `WordLength` | int | Length of words in list |
| `Words` | List\<string\> | The word list |
| `Count` | int | Number of words |

| Method | Returns | Description |
|--------|---------|-------------|
| `GetRandomWord()` | string | Random word from list |
| `Contains(string)` | bool | Case-insensitive check |
| `SetWords(List\<string\>, int)` | void | Used by WordBankImporter |

### DifficultyCalculator.cs (~309 lines)
**Path:** `Assets/DLYH/Scripts/Core/DifficultyCalculator.cs`
**Namespace:** `DLYH.Core.GameState`
**Type:** Static utility

**Formula:** `MissLimit = Base(15) + OpponentGridBonus + OpponentWordModifier + PlayerDifficultyModifier`, clamped 10-40

| Method | Returns | Description |
|--------|---------|-------------|
| `CalculateMissLimitForPlayer(DifficultySetting, int, int)` | int | Main formula |
| `GetGridBonus(int)` | int | Grid size bonus (3-13) |
| `GetWordCountModifier(int)` | int | 3 words=0, 4 words=-2 |
| `GetDifficultyModifier(DifficultySetting)` | int | Hard=2, Normal=6, Easy=9 |
| `GetWordLengths(WordCountOption)` | int[] | Standard word lengths |
| `GetCalculationBreakdown(...)` | string | Debug breakdown |

### GameplayStateTracker.cs (~147 lines)
**Path:** `Assets/DLYH/Scripts/Core/GameplayStateTracker.cs`
**Namespace:** `DLYH.Core.GameState`

Tracks player/opponent gameplay state (misses, known letters, revealed cells).

| Property | Type | Description |
|----------|------|-------------|
| `PlayerMisses` / `OpponentMisses` | int | Miss counts |
| `PlayerMissLimit` / `OpponentMissLimit` | int | Miss limits |
| `PlayerKnownLetters` / `OpponentKnownLetters` | HashSet\<char\> | Known letters |
| `PlayerRevealedCells` / `OpponentRevealedCells` | Dictionary\<Vector2Int, RevealedCellInfo\> | Revealed cells |

---

## Core / Utilities

### JsonParsingUtility.cs (~365 lines)
**Path:** `Assets/DLYH/Scripts/Core/Utilities/JsonParsingUtility.cs`
**Namespace:** `DLYH.Core.Utilities`
**Type:** Static utility (manual JSON parsing without JsonUtility)

| Method | Returns | Description |
|--------|---------|-------------|
| `ExtractStringField(json, fieldName)` | string | Extract string value |
| `ExtractIntField(json, fieldName, default)` | int | Extract int value |
| `ExtractBoolField(json, fieldName)` | bool | Extract bool value |
| `ExtractObjectField(json, fieldName)` | string | Extract nested object as raw JSON |
| `ExtractStringArray(json, fieldName)` | string[] | Extract string array |
| `ExtractIntArray(json, fieldName)` | int[] | Extract int array |
| `ExtractRevealedCellsArray(json, fieldName)` | tuple[] | Extract revealed cells |

---

## Table UI

### TableModel.cs (~287 lines)
**Path:** `Assets/DLYH/Scripts/UI/TableModel.cs`
**Namespace:** `DLYH.TableUI`

**Events:** `OnCellChanged(int row, int col, TableCell)`, `OnCleared`

| Method | Returns | Description |
|--------|---------|-------------|
| `Initialize(TableLayout)` | void | Set up model with layout |
| `Clear()` | void | Clear all cells |
| `GetCell(row, col)` | TableCell | Get cell |
| `SetCell(row, col, cell)` | void | Set entire cell |
| `SetCellChar/State/Kind/Owner(...)` | void | Set individual properties |
| `SetGridCellLetter/State/Owner(...)` | void | Grid-local coordinates |
| `GetGridCell(gridRow, gridCol)` | TableCell | Get by grid coordinates |

### TableView.cs (~804 lines)
**Path:** `Assets/DLYH/Scripts/UI/TableView.cs`
**Namespace:** `DLYH.TableUI`

**Events:** `OnCellClicked(row, col, TableCell)`, `OnCellHovered(row, col, TableCell)`, `OnCellHoverExit`

| Method | Returns | Description |
|--------|---------|-------------|
| `SetMeasurementSlot(VisualElement)` | void | Stable slot for sizing |
| `Bind(TableModel)` | void | Bind view to model |
| `SetPlayerColors(Color, Color)` | void | Player 1 and 2 colors |
| `SetSetupMode(bool)` | void | Setup vs gameplay mode |
| `SetDefenseGrid(bool)` | void | Defense grid shows letters |
| `RefreshAll()` | void | Refresh all cells from model |
| `RefreshIfDirty()` | void | Update if model changed |
| `RecalculateSizes()` | void | Force size recalculation |
| `GetSizeClassName()` | string | Current size class name |
| `GetCalculatedCellSize()` | int | Cell size in pixels |
| `GetCalculatedFontSize()` | int | Font size in pixels |

### TableCell.cs (~163 lines) - struct
**Namespace:** `DLYH.TableUI`

| Field | Type | Description |
|-------|------|-------------|
| `Kind` | TableCellKind | Type of cell |
| `State` | TableCellState | Visual/interaction state |
| `Owner` | CellOwner | Who owns this cell |
| `TextChar` | char | Character content ('\0' for empty) |
| `Row` / `Col` | int | Position (0-indexed) |

**Factory:** `CreateSpacer()`, `CreateColumnHeader(char)`, `CreateRowHeader(int)`, `CreateWordSlot(char)`, `CreateGridCell(CellOwner)`

### Enums (TableCellKind, TableCellState, CellOwner)

```
enum TableCellKind  { Spacer, WordSlot, HeaderCol, HeaderRow, GridCell }
enum CellOwner      { None, Player, Opponent }
enum TableCellState { None, Normal, Disabled, Hidden, Selected, Hovered, Locked, ReadOnly,
                      PlacementValid, PlacementInvalid, PlacementPath, PlacementAnchor, PlacementSecond,
                      Fog, Revealed, Found, Hit, Miss, WrongWord, Warning }
```

### TableLayout.cs (~191 lines)
**Namespace:** `DLYH.TableUI`

| Property | Type | Description |
|----------|------|-------------|
| `TotalRows` / `TotalCols` | int | Grid table dimensions |
| `GridSize` | int | e.g. 6 for 6x6 |
| `GridRegion` | TableRegion | Playable grid region |

| Method | Returns | Description |
|--------|---------|-------------|
| `CreateForSetup(gridSize, wordCount)` | TableLayout | Static factory for setup |
| `CreateForGameplay(gridSize, wordCount)` | TableLayout | Static factory for gameplay |
| `GridToTable(gridRow, gridCol)` | (int, int) | Grid-local to table coords |
| `TableToGrid(tableRow, tableCol)` | (int, int) | Table to grid-local coords |
| `IsInGrid(row, col)` | bool | Check if in playable grid |

### TableRegion.cs (~82 lines) - struct
**Namespace:** `DLYH.TableUI`

Fields: `Name`, `RowStart`, `ColStart`, `RowCount`, `ColCount`. Methods: `Contains()`, `ToLocal()`, `ToTable()`.

### ColorRules.cs (~251 lines) - static
**Namespace:** `DLYH.TableUI`

| Field | Description |
|-------|-------------|
| `SystemRed` / `SystemYellow` / `SystemGreen` | System feedback colors |
| `CellDefault` / `CellFog` / `CellMiss` | Cell state colors |
| `SelectableColors` | Color[] - 8 player-selectable colors |
| `SelectableColorNames` | string[] - Names for selectable colors |

| Method | Returns | Description |
|--------|---------|-------------|
| `GetGameplayColor(state, owner, p1Color, p2Color)` | Color | Gameplay cell color |
| `GetContrastingTextColor(background)` | Color | Readable text color |
| `GetPlacementColor(state, playerColor, isSetup)` | Color | Placement feedback color |

### PlacementAdapter.cs (~802 lines)
**Path:** `Assets/DLYH/Scripts/UI/PlacementAdapter.cs`
**Namespace:** `DLYH.TableUI`

Contains `PlacementAdapter`, `TablePlacementController`, `TableGridColorManager`.

**PlacementAdapter Events:** `OnWordPlaced(rowIndex, word, positions)`, `OnPlacementCancelled`

| Method | Returns | Description |
|--------|---------|-------------|
| `EnterPlacementMode(wordRowIndex, word)` | void | Start placing a word |
| `CancelPlacementMode()` | void | Cancel placement |
| `HandleCellClick(gridCol, gridRow)` | bool | Process click during placement |
| `PlaceWordRandomly()` | bool | Auto-place current word |
| `ClearWordFromGrid(rowIndex)` | void | Remove word from grid |
| `ClearAllPlacedWords()` | void | Clear all placements |
| `GetAllWordPlacements()` | List | All placements with directions |

### WordRowView.cs (~1115 lines)
**Path:** `Assets/DLYH/Scripts/UI/WordRowView.cs`
**Namespace:** `DLYH.TableUI`

**Events:** `OnPlacementRequested(int)`, `OnClearRequested(int)`, `OnGuessRequested(int)`, `OnWordGuessSubmitted(int, string)`, `OnWordGuessCancelled(int)`, `OnLetterCellClicked(int, int)`

| Method | Returns | Description |
|--------|---------|-------------|
| `SetWord(string)` | void | Set word value |
| `SetPlaced(bool)` | void | Mark as placed |
| `SetWordValid(bool)` | void | Dictionary check result |
| `SetGameplayMode(bool)` | void | Switch to gameplay |
| `RevealLetter(index, color)` | void | Reveal specific letter |
| `RevealLetterAsFound(index)` | void | Reveal as yellow |
| `UpgradeLetterToPlayerColor(index, color)` | void | Yellow -> player color |
| `RevealAllOccurrences(char, color)` | int | Reveal all of a letter |
| `EnterWordGuessMode()` / `ExitWordGuessMode()` | void | Inline word guessing |
| `TypeLetter(char)` | bool | Type in guess mode |
| `IsFullyRevealed()` | bool | All letters shown |

### WordRowsContainer.cs (~749 lines)
**Path:** `Assets/DLYH/Scripts/UI/WordRowsContainer.cs`
**Namespace:** `DLYH.TableUI`

**Events:** `OnPlacementRequested(int, string)`, `OnWordCleared(int)`, `OnGuessRequested(int, string)`, `OnWordGuessSubmitted(int, string)`, `OnAllWordsPlaced`

| Method | Returns | Description |
|--------|---------|-------------|
| `SetWord(rowIndex, word)` / `GetWord(rowIndex)` | void/string | Word access |
| `SetWordPlaced(rowIndex, bool)` | void | Mark placed |
| `SetActiveRow(rowIndex)` | void | Set active for editing |
| `SetGameplayMode(bool)` | void | Switch all rows |
| `AreAllWordsPlaced()` / `AreAllWordsFilled()` | bool | Completion checks |
| `SetWordsForGameplay(string[])` | void | Opponent's words |
| `RevealLetterInAllWords(char, color)` | int | Reveal letter across rows |
| `CapturePreGuessSnapshot()` | void | Zero-alloc pre-guess state |
| `GetNewlyCompletedWords(snapshot)` | List\<int\> | Words completed since snapshot |

### WordSuggestionDropdown.cs (~310 lines)
**Path:** `Assets/DLYH/Scripts/UI/WordSuggestionDropdown.cs`
**Namespace:** `DLYH.TableUI`

**Events:** `OnWordSelected(string)`

| Method | Returns | Description |
|--------|---------|-------------|
| `SetWordList(WordListSO)` | void | Set word source |
| `SetRequiredLength(int)` | void | Filter by length |
| `UpdateFilter(string)` | void | Filter by input text |
| `Show()` / `Hide()` | void | Visibility control |
| `SelectPrevious()` / `SelectNext()` | void | Navigate dropdown |
| `ConfirmSelection()` | bool | Confirm and fire event |

---

## UI Controllers

### UIFlowController.cs (~7629 lines)
**Path:** `Assets/DLYH/Scripts/UI/UIFlowController.cs`
**Type:** MonoBehaviour
**Namespace:** `DLYH.TableUI`

Main orchestrator for all game screens and gameplay logic.

**Key Fields:**

| Field | Type | Description |
|-------|------|-------------|
| `_matchmakingResult` | NetworkingUIResult | Session context from matchmaking |
| `_currentGameCode` | string | Current online game code |
| `_opponent` | IOpponent | Current opponent (AI or Remote) |
| `_gameSessionService` | GameSessionService | Supabase service |
| `_isPlayerTurn` | bool | Whose turn it is |
| `_playerHeadIndex` / `_opponentHeadIndex` | int | Head selections |

**Key Methods (Networking):**

| Method | Description |
|--------|-------------|
| `SaveGameStateToSupabaseAsync()` | Push state after turns (version guard, retry) |
| `SavePlayerSetupToSupabaseAsync()` | Save player setup including headIndex |
| `HandleOpponentJoined()` | When opponent joins private game |
| `FetchOpponentSetupForMatchmakingAsync()` | Fetch opponent data for matchmaking |
| `StartOpponentJoinPolling()` | 3s polling for opponent join |
| `CreateRemotePlayerOpponentAsync()` | Create and wire RemotePlayerOpponent |
| `StartTurnDetectionPolling()` | 2s polling for opponent moves |
| `ClaimInactivityVictoryAsync()` | Victory when opponent inactive 5+ days |
| `CapturePlayerSetupData()` | Captures setup including headIndex |

**Gotcha:** In guess handlers (`HandleOpponentLetterGuess` etc.), there's a `if (_isPlayerTurn) return;` guard. The AI's `ExecuteGuess()` must fire BEFORE `OnThinkingComplete` (which sets `_isPlayerTurn = true`).

### SetupWizardController.cs (~943 lines)
**Path:** `Assets/DLYH/Scripts/UI/SetupWizardController.cs`
**Type:** MonoBehaviour (RequireComponent UIDocument)
**Namespace:** `DLYH.TableUI`

Progressive card reveal setup wizard (Profile -> Grid Size -> Word Count -> Difficulty -> Mode).

**Events:** `OnSetupComplete(SetupData)`, `OnBackToMenu`
**Enums:** `GameMode { None, SinglePlayer, Multiplayer }`, `MultiplayerAction { FindOpponent, InviteFriend, JoinGame }`

| Method | Returns | Description |
|--------|---------|-------------|
| `GetCurrentSetup()` | SetupData | Current configuration |
| `Reset()` | void | Reset to initial state |

**Note:** This is the standalone MonoBehaviour version. UIFlowController uses SetupWizardUIManager instead (managed class, not MonoBehaviour).

### MainMenuController.cs (~126 lines)
**Path:** `Assets/DLYH/Scripts/UI/MainMenuController.cs`
**Type:** MonoBehaviour (RequireComponent UIDocument)
**Namespace:** `DLYH.TableUI`

**Events:** `OnStartGame`, `OnHowToPlay`, `OnSettings`

| Method | Returns | Description |
|--------|---------|-------------|
| `Show()` | void | Show menu |
| `Hide()` | void | Hide menu |
| `ReturnToMenu()` | void | Show + reset wizard |

---

## UI Managers

### SetupWizardUIManager.cs (~820 lines)
**Path:** `Assets/DLYH/Scripts/UI/SetupWizardUIManager.cs`
**Namespace:** `DLYH.TableUI`

Managed setup wizard (not MonoBehaviour). Used by UIFlowController.

**Key API:** `SelectedHeadIndex`, head picker (prev/next), color picker, grid/word/difficulty selection. Persists head via `PlayerPrefs.GetInt/SetInt("DLYH_SelectedHead")`. Renders 4-layer head preview (hair-back, head, face, hair) with player color tinting.

### GameplayScreenManager.cs (~1437 lines)
**Path:** `Assets/DLYH/Scripts/UI/GameplayScreenManager.cs`
**Namespace:** `DLYH.TableUI`

**Enums:** `LetterKeyState { Default, Hit, Miss, Found }`, `StatusType { Normal, Hit, Miss, WordFound, Error }`, `PlayerTabData` class

**Events:** `OnHamburgerClicked`, `OnLetterKeyClicked(char)`, `OnGridCellClicked(row, col, isAttack)`, `OnWordGuessClicked(int)`, `OnShowGuillotineOverlay`, `OnAttackTabSelected`, `OnDefendTabSelected`

| Method | Returns | Description |
|--------|---------|-------------|
| `Initialize(VisualElement)` | void | Set up from root |
| `SelectAttackTab(isAutoSwitch)` | void | Switch to attack grid |
| `SelectDefendTab(isAutoSwitch)` | void | Switch to defend grid |
| `SetAllowManualTabSwitch(bool)` | void | Block during opponent turn |
| `MarkLetterHit(char, Color)` | void | Player color on keyboard |
| `MarkLetterMiss(char)` | void | Red on keyboard |
| `MarkLetterFound(char)` | void | Yellow on keyboard |
| `MarkOpponentLetterHit/Found/Miss(char)` | void | Defend tab keyboard |
| `SetPlayerData(PlayerTabData, PlayerTabData)` | void | Configure tabs |
| `SetPlayerMissCount(int, int)` | void | Update player miss display |
| `SetOpponentMissCount(int, int)` | void | Update opponent miss display |
| `SetPlayerTurn(bool)` | void | Turn indicator |
| `SetStatusMessage(string, StatusType)` | void | Status bar message |
| `AddGuessedWord(name, word, wasHit, isPlayer)` | void | Guessed words panel |
| `SetTableView(TableView)` | void | Bind grid rendering |
| `SetTableModels(attack, defend)` | void | Attack/defend models |
| `SetWordRowContainers(attack, defend)` | void | Word row displays |
| `ApplyKeyboardViewportSizing(cellSize, fontSize)` | void | Responsive keyboard |
| `FlashMiss()` | void | Visual miss feedback |
| `Reset()` | void | New game reset |

### GameplayGuessManager.cs (~919 lines)
**Path:** `Assets/DLYH/Scripts/UI/GameplayGuessManager.cs`
**Namespace:** `DLYH.TableUI`

**Enums:** `GuessResult { Hit, Miss, AlreadyGuessed, Invalid }`

**Events:** `OnLetterHit(char, List<Vector2Int>)`, `OnLetterMiss(char)`, `OnCoordinateHit(Vector2Int, char)`, `OnCoordinateMiss(Vector2Int)`, `OnMissCountChanged(isPlayer, misses, limit)`, `OnGameOver(isPlayer)`, `OnWordGuessProcessed(wordIndex, word, correct)`, `OnWordSolved(wordIndex)`

| Method | Returns | Description |
|--------|---------|-------------|
| `Initialize(playerLetters, playerPos, opponentLetters, opponentPos, pLimit, oLimit, words, validate)` | void | Full initialization |
| `ProcessPlayerLetterGuess(char)` | GuessResult | Player guesses letter |
| `ProcessOpponentLetterGuess(char)` | GuessResult | Opponent guesses letter |
| `ProcessPlayerCoordinateGuess(col, row)` | GuessResult | Player guesses coordinate |
| `ProcessOpponentCoordinateGuess(col, row)` | GuessResult | Opponent guesses coordinate |
| `ProcessPlayerWordGuess(word, wordIndex)` | GuessResult | Player guesses word (+2 miss penalty on wrong) |
| `AddPlayerMisses(int)` / `AddOpponentMisses(int)` | void | Manual miss addition |
| `SetInitialMissCounts(player, opponent)` | void | Resume game (no game-over check) |
| `RestoreOpponentGuessState(letters, coords)` | void | Restore from saved state |
| `HasPlayerWon()` / `HasOpponentWon()` | bool | Win condition check |
| `AreAllOpponentLettersKnown()` | bool | All word row letters found |
| `AreAllOpponentCoordinatesKnown()` | bool | All grid positions found |
| `AreAllLetterCoordinatesKnown(char)` | bool | All positions of letter known |
| `GetOpponentLetterPositions(char)` | List\<Vector2Int\> | Where letter appears |

**Note:** Uses pooled `_hitPositionsPool` list -- handlers must use positions immediately.

### GuillotineOverlayManager.cs (~450 lines)
**Path:** `Assets/DLYH/Scripts/UI/GuillotineOverlayManager.cs`
**Namespace:** `DLYH.TableUI`

**GuillotineData class:** `Name`, `Color`, `MissCount`, `MissLimit`, `HeadIndex`, `IsLocalPlayer`

| Method | Returns | Description |
|--------|---------|-------------|
| `Initialize(VisualElement, HeadCharacterData)` | void | Init with root + head data |
| `Show(GuillotineData player, GuillotineData opponent)` | void | Show overlay |
| `Hide()` | void | Hide overlay |
| `SetHeadTextures(isPlayer, headIndex, hairTint)` | void | Set 4-layer head textures |
| `UpdateFaceExpression(isPlayer, headIndex, faceIndex)` | void | Stage-based face change |
| `GetFaceIndexFromStage(stage)` | int | Stage -> face index mapping |

**Face expression mapping:** Stage 0-1=Neutral, 2=Worried, 3=Scared, 4=Horrified, GameOver loser=Dead, winner=Evil

**UXML layers:** `player-head-hair-back`, `player-head-base`, `player-head-face`, `player-head-hair` (same for opponent)

### HeadCharacterData.cs
**Path:** `Assets/DLYH/Scripts/UI/HeadCharacterData.cs`
**Namespace:** `DLYH.TableUI`
**Type:** ScriptableObject

```csharp
[CreateAssetMenu(fileName = "HeadCharacterData", menuName = "DLYH/Head Character Data")]
public class HeadCharacterData : ScriptableObject { public HeadCharacter[] Characters; }

[Serializable]
public class HeadCharacter {
    public string Name;
    public Texture2D HeadTexture, HairTexture, HairBackTexture; // HairBack only for Woman 1
    public Texture2D[] FaceTextures; // 6: Neutral, Worried, Scared, Horrified, Dead, Evil
}
```

**Asset:** `Assets/DLYH/Data/HeadCharacterData.asset` (6 characters, 36 faces)

### GameStateManager.cs (~400 lines)
**Path:** `Assets/DLYH/Scripts/UI/Managers/GameStateManager.cs`
**Namespace:** `DLYH.UI.Managers`
**Type:** Static class

| Method | Returns | Description |
|--------|---------|-------------|
| `ParseGameStateJson(string)` | DLYHGameState | Parse state from Supabase |
| `SerializeGameState(DLYHGameState)` | string | Serialize to JSON |
| `EncryptWordPlacements(placements, gameCode)` | string | XOR encrypt with salt |
| `DecryptWordPlacements(encrypted, gameCode)` | List | XOR decrypt (auto-detects legacy Base64) |

### ActiveGamesManager.cs (~360 lines)
**Path:** `Assets/DLYH/Scripts/UI/Managers/ActiveGamesManager.cs`
**Namespace:** `DLYH.UI.Managers`

| Method | Returns | Description |
|--------|---------|-------------|
| `LoadMyActiveGamesAsync()` | UniTask | Fetch and display active games |
| `RefreshGamesList()` | void | Trigger refresh |

### HelpModalManager.cs (~233 lines)
**Path:** `Assets/DLYH/Scripts/UI/Managers/HelpModalManager.cs`
**Namespace:** `DLYH.UI.Managers`

| Method | Returns | Description |
|--------|---------|-------------|
| `Initialize(VisualElement)` | void | Init with root |
| `Show()` | void | Show help (lazy-creates UI) |
| `Hide()` | void | Hide help |
| `IsVisible` | bool | Visibility state |

### ConfirmationModalManager.cs (~191 lines)
**Path:** `Assets/DLYH/Scripts/UI/Managers/ConfirmationModalManager.cs`
**Namespace:** `DLYH.UI.Managers`

| Method | Returns | Description |
|--------|---------|-------------|
| `Initialize(VisualElement)` | void | Init with root |
| `Show(title, message, onConfirm)` | void | Show with callback |
| `Hide()` | void | Hide |
| `IsVisible` | bool | Visibility state |

---

## UI Services

### WordValidationService.cs (~98 lines)
**Path:** `Assets/DLYH/Scripts/UI/Services/WordValidationService.cs`
**Namespace:** `DLYH.UI.Services`

Constructor takes 4 WordListSO refs (3-6 letter words).

| Method | Returns | Description |
|--------|---------|-------------|
| `ValidateWord(word, requiredLength)` | bool | Check against word bank |
| `GetRandomWordOfLength(int)` | string | Random word of length |
| `GetWordListForLength(int)` | WordListSO | Get appropriate list |

### SettingsPanel.cs (~59 lines)
**Path:** `Assets/DLYH/Scripts/Audio/AudioSettings.cs`
**Namespace:** `DLYH.UI`
**Type:** Static class

| Method | Returns | Description |
|--------|---------|-------------|
| `GetSavedSFXVolume()` | float | PlayerPrefs SFX volume (0-1) |
| `GetSavedMusicVolume()` | float | PlayerPrefs music volume (0-1) |
| `SetSFXVolume(float)` | void | Save SFX volume |
| `SetMusicVolume(float)` | void | Save music volume |

---

## AI

### ExecutionerAI.cs (~495 lines)
**Path:** `Assets/DLYH/Scripts/AI/Core/ExecutionerAI.cs`
**Type:** MonoBehaviour
**Namespace:** `DLYH.AI.Core`

**Events:** `OnThinkingStarted`, `OnThinkingComplete`, `OnLetterGuess(char)`, `OnCoordinateGuess(row, col)`, `OnWordGuess(word, wordIndex)`

| Property | Type | Description |
|----------|------|-------------|
| `CurrentSkill` | float | 0.15-0.95 skill level |
| `AIDifficulty` | DifficultySetting | Inverted from player |
| `IsThinking` | bool | Currently processing turn |

| Method | Returns | Description |
|--------|---------|-------------|
| `Initialize(DifficultySetting)` | void | Init with inverted player difficulty |
| `Reset()` | void | Reset for new game |
| `ExecuteTurnAsync(AIGameState)` | UniTaskVoid | Execute AI turn with think delay |
| `RecordPlayerGuess(bool)` | void | Record hit/miss for rubber-banding |
| `RecordAIHit(row, col)` | void | Record coordinate hit to memory |
| `RecordRevealedLetter(char)` | void | Record letter to memory |
| `AdvanceTurn()` | void | Advance memory turn counter |

**GOTCHA:** In `ExecuteTurnAsync()`, `ExecuteGuess()` MUST fire before `OnThinkingComplete`. `OnThinkingComplete` triggers `HandleOpponentThinkingComplete` which sets `_isPlayerTurn = true`, and guess handlers guard on `!_isPlayerTurn`.

### DifficultyAdapter.cs (~358 lines)
**Path:** `Assets/DLYH/Scripts/AI/Core/DifficultyAdapter.cs`
**Namespace:** `DLYH.AI.Core`

Rubber-banding: adjusts AI skill based on player performance using ring buffer.

| Property | Type | Description |
|----------|------|-------------|
| `CurrentSkill` | float | 0.15-0.95 |
| `CurrentHitsToIncrease` | int | Player hits before AI skill up |
| `CurrentMissesToDecrease` | int | Player misses before AI skill down |

| Method | Returns | Description |
|--------|---------|-------------|
| `RecordPlayerGuess(bool)` | void | Adjust skill via rubber-banding |
| `Reset(DifficultySetting)` | void | Reset for new game |

### MemoryManager.cs (~303 lines)
**Path:** `Assets/DLYH/Scripts/AI/Core/MemoryManager.cs`
**Namespace:** `DLYH.AI.Core`

AI "forgets" information based on skill level (lower skill = higher forget chance).

| Method | Returns | Description |
|--------|---------|-------------|
| `RecordHit(row, col)` | void | Record coordinate hit |
| `RecordRevealedLetter(char)` | void | Record letter |
| `GetEffectiveKnownHits(skillLevel)` | HashSet | Hits filtered by skill |
| `GetEffectiveRevealedLetters(skillLevel)` | HashSet | Letters filtered by skill |
| `RemembersHit(row, col, skillLevel)` | bool | Skill-filtered check |
| `RemembersLetter(char, skillLevel)` | bool | Skill-filtered check |

### AISetupManager.cs (~542 lines)
**Path:** `Assets/DLYH/Scripts/AI/Core/AISetupManager.cs`
**Namespace:** `DLYH.AI.Core`

| Property | Type | Description |
|----------|------|-------------|
| `SelectedWords` | List\<string\> | AI's chosen words |
| `Placements` | List\<WordPlacementData\> | Word placements |
| `IsSetupComplete` | bool | Setup done |

| Method | Returns | Description |
|--------|---------|-------------|
| `PerformSetup(wordLists)` | bool | Full setup (select + place) |
| `SelectWords(wordLists)` | bool | Choose random words |
| `PlaceWords(maxAttempts, crosswordProb)` | bool | Place on grid |
| `GetCellsForPlacement(placement)` | List | Static: cells for placement |
| `GetLetterAtCell(placement, row, col)` | char? | Static: letter at cell |

### ExecutionerConfigSO.cs (~412 lines)
**Path:** `Assets/DLYH/Scripts/AI/Config/ExecutionerConfigSO.cs`
**Namespace:** `DLYH.AI.Config`
**Type:** ScriptableObject

Inspector-configurable AI parameters: skill range (0.15-0.95), per-difficulty start skills, rubber-banding thresholds, think time (1-3s), forget chance, word guess risk.

| Method | Returns | Description |
|--------|---------|-------------|
| `GetStartSkillForDifficulty(difficulty)` | float | Starting skill |
| `GetRandomThinkTime()` | float | Random 1-3s |
| `GetForgetChanceForSkill(skillLevel)` | float | Forget probability |
| `GetStrategyWeightsForDensity(fillRatio, out letter, out coord)` | void | Strategy selection |

### IGuessStrategy.cs (~383 lines)
**Path:** `Assets/DLYH/Scripts/AI/Strategies/IGuessStrategy.cs`
**Namespace:** `DLYH.AI.Strategies`

**Types defined:**

```
enum GuessType { Letter, Coordinate, Word }

struct GuessRecommendation {
    GuessType Type; char Letter; int Row, Col; string WordGuess; int WordIndex;
    float Confidence; bool IsValid;
    static CreateLetterGuess(char, float) / CreateCoordinateGuess(row, col, float) /
           CreateWordGuess(word, index, float) / CreateInvalid()
}

class AIGameState {
    int GridSize, WordCount; float SkillLevel, FillRatio;
    HashSet<char> GuessedLetters, HitLetters;
    HashSet<(row, col)> GuessedCoordinates, HitCoordinates;
    List<string> WordPatterns; List<bool> WordsSolved;
    HashSet<string> GuessedWords, WordBank;
}

interface IGuessStrategy {
    GuessType StrategyType { get; }
    GuessRecommendation Evaluate(AIGameState state);
}
```

### Strategy Implementations

| Script | Lines | Description |
|--------|-------|-------------|
| `LetterGuessStrategy.cs` | ~327 | Frequency-based letter selection with pattern bonus |
| `CoordinateGuessStrategy.cs` | ~250 | Adjacent-to-hits and center-bias scoring |
| `WordGuessStrategy.cs` | ~339 | Pattern matching against word bank with confidence threshold |

All implement `IGuessStrategy`. Each has `Evaluate(AIGameState)` and `GetDebugScoreBreakdown/Analysis(AIGameState)`.

### GridAnalyzer.cs (~442 lines) - static
**Path:** `Assets/DLYH/Scripts/AI/Data/GridAnalyzer.cs`
**Namespace:** `DLYH.AI.Data`

Grid analysis utilities: fill ratio, adjacency checks, coordinate scoring, center bias.

| Method | Returns | Description |
|--------|---------|-------------|
| `CalculateFillRatio(gridSize, wordCount)` | float | Estimated fill ratio |
| `GetAdjacentCoordinates(row, col, gridSize)` | List | 4-directional neighbors |
| `IsAdjacentToAny(row, col, knownHits, gridSize)` | bool | Near any hit |
| `ExtendsHitLine(row, col, knownHits, gridSize)` | bool | Extends a line of hits |
| `CalculateCoordinateScore(row, col, hits, gridSize, fillRatio)` | float | Combined score |
| `GetUnguessedCoordinates(gridSize, guessed)` | List | Available coordinates |

### LetterFrequency.cs (~212 lines) - static
**Path:** `Assets/DLYH/Scripts/AI/Data/LetterFrequency.cs`
**Namespace:** `DLYH.AI.Data`

English letter frequency data (E=12.7%, Z=0.07%).

| Field / Method | Description |
|----------------|-------------|
| `LettersByFrequency` | char[] sorted E, T, A, O, I, N, S... |
| `Vowels` | E, A, O, I, U |
| `GetFrequency(char)` | Raw frequency (0-12.7) |
| `GetNormalizedFrequency(char)` | 0-1 scale |
| `IsVowel(char)` / `IsConsonant(char)` | Classification |
| `GetUnguessedLettersByFrequency(guessed)` | Remaining letters sorted |

---

## Audio

### MusicManager.cs (~669 lines)
**Path:** `Assets/DLYH/Scripts/Audio/MusicManager.cs`
**Namespace:** `DLYH.Audio`
**Type:** MonoBehaviour Singleton (`MusicManager.Instance`)

Shuffle playlist with crossfade between tracks. Dynamic tempo based on danger level.

| Property | Type | Description |
|----------|------|-------------|
| `IsMuted` | bool | Music muted |
| `IsPlaying` | bool | Music playing |

| Method | Returns | Description |
|--------|---------|-------------|
| `PlayMusic()` | void | Start playlist |
| `StopMusic()` | void | Stop all |
| `PlayNextTrack()` / `PlayPreviousTrack()` | void | Navigate with crossfade |
| `Mute()` / `Unmute()` / `ToggleMute()` | void | Mute control |
| `SetTensionLevel(float)` | void | 0-1 danger -> pitch shift |
| `ResetTension()` | void | Normal tempo |
| `RefreshVolumeCache()` | void | Refresh from PlayerPrefs |

**Static shortcuts:** `ToggleMuteMusic()`, `SetTension(float)`, `ResetMusicTension()`, `IsMusicMuted()`

### UIAudioManager.cs (~415 lines)
**Path:** `Assets/DLYH/Scripts/Audio/UIAudioManager.cs`
**Namespace:** `DLYH.Audio`
**Type:** MonoBehaviour Singleton (`UIAudioManager.Instance`)

SFX playback using SFXClipGroup ScriptableObjects for randomized sounds.

| Method | Returns | Description |
|--------|---------|-------------|
| `PlayKeyboardClick()` | void | Keyboard/letter clicks |
| `PlayGridCellClick()` | void | Grid cell clicks |
| `PlayButtonClick()` | void | General button clicks |
| `PlayPopupOpen()` / `PlayPopupClose()` | void | Modal sounds |
| `PlayError()` / `PlaySuccess()` | void | Feedback sounds |
| `PlayFromGroup(SFXClipGroup)` | void | Play from specific group |
| `PlayClip(AudioClip, volume, pitch)` | void | Play single clip |
| `Mute()` / `Unmute()` / `ToggleMute()` | void | Mute control |

**Static shortcuts:** `KeyboardClick()`, `GridCellClick()`, `ButtonClick()`, `PopupOpen()`, `PopupClose()`, `Error()`, `Success()`, `ToggleMuteSFX()`

### GuillotineAudioManager.cs (~200 lines)
**Path:** `Assets/DLYH/Scripts/Audio/GuillotineAudioManager.cs`
**Namespace:** `DLYH.Audio`
**Type:** MonoBehaviour Singleton (`GuillotineAudioManager.Instance`)

Guillotine-specific sounds: blade raise (rope + blade), final execution (3-part: raise, hook unlock, chop), head removed.

| Method | Returns | Description |
|--------|---------|-------------|
| `PlayBladeRaise()` | void | Rope stretch + blade up (layered) |
| `PlayFinalExecution()` | void | 3-part sequence |
| `PlayHeadRemoved()` | void | Head separation |

### SFXClipGroup.cs (~73 lines)
**Path:** `Assets/DLYH/Scripts/Audio/SFXClipGroup.cs`
**Namespace:** `DLYH.Audio`
**Type:** ScriptableObject (`[CreateAssetMenu(menuName = "DLYH/Audio/SFX Clip Group")]`)

| Property / Method | Returns | Description |
|-------------------|---------|-------------|
| `ClipCount` | int | Number of clips |
| `BaseVolume` | float | Base volume setting |
| `GetRandomClip()` | AudioClip | Random clip from group |
| `GetRandomVolume()` | float | Base +/- variation |
| `GetRandomPitch()` | float | Base +/- variation |

---

## Networking

### IOpponent.cs
**Path:** `Assets/DLYH/Scripts/Networking/IOpponent.cs`
**Namespace:** `DLYH.Networking`

**Events:** `OnThinkingStarted`, `OnThinkingComplete`, `OnLetterGuess(char)`, `OnCoordinateGuess(row, col)`, `OnWordGuess(word, wordIndex)`, `OnDisconnected`, `OnReconnected`

| Property | Type | Description |
|----------|------|-------------|
| `OpponentName` | string | Display name |
| `OpponentColor` | Color | Player color |
| `GridSize` / `WordCount` | int | Grid settings |
| `WordPlacements` | List\<WordPlacementData\> | Word placements |
| `IsConnected` / `IsThinking` / `IsAI` | bool | State flags |
| `MissLimit` | int | Calculated miss limit |

| Method | Returns | Description |
|--------|---------|-------------|
| `InitializeAsync(PlayerSetupData)` | UniTask | Initialize opponent |
| `ExecuteTurn(AIGameState)` | void | Trigger turn |
| `RecordPlayerGuess(bool)` | void | Record hit/miss |
| `RecordOpponentHit(row, col)` | void | Record hit |
| `RecordRevealedLetter(char)` | void | Record letter |
| `AdvanceTurn()` / `Reset()` | void | Turn/reset control |

### LocalAIOpponent.cs
**Path:** `Assets/DLYH/Scripts/Networking/LocalAIOpponent.cs`
**Type:** Implements IOpponent

Wraps ExecutionerAI for local/phantom AI games. `IsAI = true`. Created for solo games or when phantom AI fallback triggers.

### RemotePlayerOpponent.cs
**Path:** `Assets/DLYH/Scripts/Networking/RemotePlayerOpponent.cs`
**Type:** Implements IOpponent

Network multiplayer via Supabase polling. `IsAI = false`.

| Method | Description |
|--------|-------------|
| `InitializeAsync()` | **STUB -- DO NOT USE.** Logs error |
| `InitializeWithExistingService()` | Lightweight init with existing GameSessionService |
| `ExecuteTurn()` | Starts waiting for opponent turn |
| `ProcessStateUpdate()` | Detect opponent action from polled state, fire events |
| `SetInitialState()` | Set baseline for comparison |

---

## Networking Services

### AuthService.cs (~500 lines)
**Path:** `Assets/DLYH/Scripts/Networking/Services/AuthService.cs`
**Namespace:** `DLYH.Networking.Services`

**Enums:** `AuthState { SignedOut, SignedIn, Loading }`, `OAuthProvider { Google, Facebook }`
**Types:** `AuthSession` (AccessToken, RefreshToken, UserId, ExpiresAt, Email, DisplayName, IsAnonymous)

**Events:** `OnAuthStateChanged(AuthState)`, `OnSignedIn(AuthSession)`, `OnSignedOut`, `OnSessionRefreshed(AuthSession)`, `OnOAuthStarted(OAuthProvider)`, `OnMagicLinkSent(string)`, `OnAuthError(string)`

| Property | Type | Description |
|----------|------|-------------|
| `CurrentSession` | AuthSession | Active session |
| `IsSignedIn` | bool | Has valid token |
| `IsAnonymous` | bool | Anonymous user |
| `UserId` / `AccessToken` / `Email` | string | Session details |

| Method | Returns | Description |
|--------|---------|-------------|
| `SignInAnonymouslyAsync()` | UniTask\<AuthSession\> | Anonymous sign-in (auto-refreshes) |
| Session stored in PlayerPrefs | -- | Persists across restarts |

### GameSessionService.cs (~1100 lines)
**Path:** `Assets/DLYH/Scripts/Networking/Services/GameSessionService.cs`
**Namespace:** `DLYH.Networking.Services`

| Method | Returns | Description |
|--------|---------|-------------|
| `CreateGame(playerId)` | UniTask\<GameSession\> | Create with unique code |
| `GetGame(gameCode)` | UniTask\<GameSession\> | Get by code |
| `GetGameWithPlayers(gameCode)` | UniTask\<GameSessionWithPlayers\> | Game + session_players |
| `UpdateGameState(gameCode, state)` | UniTask\<bool\> | Update game state JSON |
| `JoinGame(gameCode, playerId)` | UniTask\<bool\> | Add player to game |

### MatchmakingService.cs (~400 lines)
**Path:** `Assets/DLYH/Scripts/Networking/Services/MatchmakingService.cs`
**Namespace:** `DLYH.Networking.Services`

**Types:** `MatchmakingResult` (Success, GameCode, IsHost, IsPhantomAI, OpponentName, ErrorMessage)
**Events:** `OnMatchmakingStatusChanged(string)`, `OnMatchFound(MatchmakingResult)`, `OnMatchmakingFailed(string)`
**Timeout:** 6 seconds before phantom AI fallback.

### PlayerService.cs (~300 lines)
**Path:** `Assets/DLYH/Scripts/Networking/Services/PlayerService.cs`
**Namespace:** `DLYH.Networking.Services`

| Property | Type | Description |
|----------|------|-------------|
| `CurrentPlayerId` | string | UUID from players table |
| `CurrentPlayerName` | string | Display name |
| `HasPlayerRecord` | bool | Has valid record |
| `EXECUTIONER_PLAYER_ID` | const string | AI reserved ID |

Persists in PlayerPrefs (`DLYH_PlayerId`, `DLYH_PlayerName`).

### SupabaseClient.cs / SupabaseConfig.cs
**Path:** `Assets/DLYH/Scripts/Networking/Services/`
**Namespace:** `DLYH.Networking.Services`

`SupabaseConfig` is a ScriptableObject with URL and anon key. `SupabaseClient` wraps HTTP requests to Supabase REST API.

---

## Networking UI

### NetworkingUIManager.cs (~815 lines)
**Path:** `Assets/DLYH/Scripts/Networking/UI/NetworkingUIManager.cs`
**Namespace:** `DLYH.Networking.UI`

**Types:** `NetworkingUIResult` (Success, Cancelled, GameCode, IsHost, IsPhantomAI, OpponentName, ErrorMessage, OpponentGridSize, OpponentWordCount, OpponentColor, OpponentSetupLoaded)

**Events:** `OnNetworkingComplete(NetworkingUIResult)`, `OnCancelled`

| Method | Returns | Description |
|--------|---------|-------------|
| `Initialize(root, matchmakingService, playerService, gameSessionService)` | void | Init with services |
| `StartMatchmakingAsync(gridSize, difficulty)` | UniTask | 6s matchmaking with phantom AI fallback |
| `ShowWaitingRoomAsync(gridSize, difficulty)` | UniTask | Private game waiting room |
| `ShowJoinCodeEntry()` | void | Join code modal |
| `JoinWithCodeAsync(code)` | UniTask | Join game with 6-char code |
| `CancelMatchmaking()` / `CancelWaiting()` / `CancelJoinCode()` | void | Cancel operations |

Three overlays: matchmaking (countdown + progress bar), waiting room (code + copy/share), join code entry (auto-uppercase).

---

## Telemetry

### PlaytestTelemetry.cs (~300 lines)
**Path:** `Assets/DLYH/Scripts/Telemetry/PlaytestTelemetry.cs`
**Namespace:** `DLYH.Telemetry`
**Type:** MonoBehaviour Singleton (DontDestroyOnLoad)

Sends events to Cloudflare Worker endpoint. Auto-captures Unity errors/exceptions.

| Property | Type | Description |
|----------|------|-------------|
| `IsEnabled` | bool | Enable/disable sending |
| `SessionId` | string | Unique session ID |

| Method | Returns | Description |
|--------|---------|-------------|
| `LogSetupComplete(name, gridSize, wordCount, difficulty)` | void | Setup event |
| `LogGameStart(playerName, pGrid, pWords, pDiff, oGrid, oWords, oDiff)` | void | Game start |
| `LogGameAbandon(reason, turnNumber)` | void | Game abandoned |
| `LogEvent(eventType, data)` | void | Generic event |

---

## Data Models

### DLYHGameState
**Path:** `Assets/DLYH/Scripts/Networking/Services/GameSessionService.cs`

```csharp
public class DLYHGameState {
    public int version;
    public string status;           // waiting, setup, playing, finished
    public string currentTurn;      // "player1" or "player2"
    public int turnNumber;
    public string createdAt, updatedAt;
    public DLYHPlayerData player1, player2;
    public string winner;           // null, "player1", "player2"
}
```

### DLYHPlayerData
```csharp
public class DLYHPlayerData {
    public string name, color, difficulty, lastActivityAt;
    public bool ready, setupComplete;
    public int gridSize, wordCount, headIndex;
    public DLYHGameplayState gameplayState;
}
```

### DLYHGameplayState
```csharp
public class DLYHGameplayState {
    public int misses, missLimit;
    public string[] knownLetters;
    public int[] solvedWordRows;
    public string[] incorrectWordGuesses;
    public RevealedCellData[] revealedCells;
}
```

### PlayerSetupData
**Path:** `Assets/DLYH/Scripts/Networking/IOpponent.cs`

```csharp
public class PlayerSetupData {
    public string PlayerName;
    public Color PlayerColor;
    public int GridSize, WordCount, HeadIndex;
    public DifficultySetting DifficultyLevel;
    public int[] WordLengths;
    public List<WordPlacementData> PlacedWords;
}
```

---

## Supabase Tables

| Table | Purpose |
|-------|---------|
| `game_sessions` | Game state, turn info, player data |
| `session_players` | Links players to games |
| `players` | Player records (id, name, settings) |
| `matchmaking_queue` | Matchmaking queue entries |

---

## Scene Structure

### NetworkingScene.unity
**Path:** `Assets/DLYH/Scenes/NetworkingScene.unity`

**Key GameObjects:**
- `Main Camera`
- `EventSystem`
- `UIFlowController`
- `GuillotineAudioManager`
- `MusicManager`
- `UIAudioManager`

---

## Usage Patterns

### IOpponent Usage
```csharp
_opponent.OnLetterGuess += HandleOpponentLetterGuess;
_opponent.OnCoordinateGuess += HandleOpponentCoordinateGuess;
_opponent.OnWordGuess += HandleOpponentWordGuess;
_opponent.ExecuteTurn(gameState);
```

### Turn Detection Flow (Online)
1. `EndPlayerTurn()` -> `SaveGameStateToSupabaseAsync()` (version guard, retry)
2. `SwitchToOpponentTurnCoroutine()` checks `_opponent.IsAI`
3. If not AI: `StartTurnDetectionPolling()` (2s interval)
4. `RemotePlayerOpponent.ProcessStateUpdate()` fires events
5. Events handled by same handlers as local AI
6. Polling stops when turn returns to local player

### State Save Pattern
```csharp
if (!string.IsNullOrEmpty(_currentGameCode) && _currentGameMode != GameMode.Solo)
{
    await SaveGameStateToSupabaseAsync();
}
```

### Polling Intervals
- Opponent join: 3 seconds
- Turn detection: 2 seconds

### What NOT to Use
- `RemotePlayerOpponent.InitializeAsync()` -- stub that logs error

---

## File Locations

| Type | Location |
|------|----------|
| Scripts | `Assets/DLYH/Scripts/<Namespace>/` |
| UI (UXML/USS) | `Assets/DLYH/UI/` |
| Scenes | `Assets/DLYH/Scenes/` |
| ScriptableObjects | `Assets/DLYH/Data/` |
| Head assets | `Assets/DLYH/DLYHGraphicAssets/Heads/` |
| Documents | `Documents/` |

### Head Asset Naming
```
{Gender}_char_{N}_head.png
{Gender}_char_{N}_hair.png (or hair_front for Woman 1)
{Gender}_char_{N}_hair_back.png (Woman 1 only)
{Gender}_char_{N}_face_{1-6}.png
```

---

**End of Code Reference**

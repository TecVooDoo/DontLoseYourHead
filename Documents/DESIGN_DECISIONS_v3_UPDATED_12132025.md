# Don't Lose Your Head - Design Decisions and Insights

**Version:** 3.0  
**Date:** November 22, 2025  
**Last Updated:** December 13, 2025  
**Developer:** TecVooDoo LLC  
**Designer:** Rune (Stephen Brandon)  

---

## Recent Changes (December 13, 2025)

### Phase 2.7: Architecture Documentation - COMPLETE

Comprehensive architecture document created documenting all 44 scripts:

| Component | Status |
|-----------|--------|
| DLYH_Architecture_v3 document | COMPLETE |
| All 44 scripts cataloged | COMPLETE |
| IGridControllers.cs (5 interfaces, 2 enums) | COMPLETE |
| WordPatternController documented | COMPLETE |
| All 10 AI scripts documented | COMPLETE |
| Data flow diagrams | COMPLETE |
| Event architecture map | COMPLETE |

### Phase 3: AI Opponent Scripts - IMPLEMENTED

All 10 AI scripts reviewed, implemented, and documented:

| Script | Lines | Purpose | Status |
|--------|-------|---------|--------|
| ExecutionerConfigSO.cs | ~412 | Tunable AI parameters | COMPLETE |
| ExecutionerAI.cs | ~493 | Main AI MonoBehaviour | COMPLETE |
| DifficultyAdapter.cs | ~268 | Rubber-banding + adaptive thresholds | COMPLETE |
| MemoryManager.cs | ~442 | Skill-based memory filtering | COMPLETE |
| AISetupManager.cs | ~468 | Word selection and placement | COMPLETE |
| IGuessStrategy.cs | ~493 | Interface + AIGameState + GuessRecommendation | COMPLETE |
| LetterGuessStrategy.cs | ~327 | Frequency + pattern analysis | COMPLETE |
| CoordinateGuessStrategy.cs | ~262 | Adjacency + density awareness | COMPLETE |
| WordGuessStrategy.cs | ~327 | Confidence thresholds | COMPLETE |
| LetterFrequency.cs | ~442 | Static English frequency data | COMPLETE |
| GridAnalyzer.cs | ~442 | Fill ratio and coordinate scoring | COMPLETE |

**Total AI Code:** ~4,376 lines across 11 files

### AI Integration TODO

Remaining work to connect AI to game:

1. **Wire ExecutionerAI events to GameplayUIController**
   - Subscribe to OnLetterGuess, OnCoordinateGuess, OnWordGuess
   - Call ProcessOpponentLetterGuess(), ProcessOpponentCoordinateGuess(), ProcessOpponentWordGuess()

2. **AI setup during Player 2 setup phase**
   - Use AISetupManager.SelectWords() and PlaceWords()
   - Create WordPlacementData for AI's words

3. **Win/Lose UI feedback**
   - Display win/lose screen on game end
   - Show final stats

4. **Turn indicator improvements**
   - Clear visual of whose turn it is
   - Thinking indicator during AI turn

---

## AI Design Details

### ExecutionerAI Integration Pattern

ExecutionerAI fires events that GameplayUIController should subscribe to:

```csharp
// In GameplayUIController
private void WireAIEvents()
{
    _executionerAI.OnThinkingStarted += HandleAIThinkingStarted;
    _executionerAI.OnLetterGuess += HandleAILetterGuess;
    _executionerAI.OnCoordinateGuess += HandleAICoordinateGuess;
    _executionerAI.OnWordGuess += HandleAIWordGuess;
}

private void HandleAILetterGuess(char letter)
{
    ProcessOpponentLetterGuess(letter);
    EndOpponentTurn();
}
```

### Grid Density Analysis

Grid density affects which strategy is more efficient:

**Fill Ratio Formula:**
```
fillRatio = (wordCount * averageWordLength) / (gridSize * gridSize)
         = (wordCount * 4.5) / (gridSize * gridSize)
```

**Density by Configuration:**

| Grid | Cells | 3 Words (~14 letters) | 4 Words (~18 letters) |
|------|-------|----------------------|----------------------|
| 6x6 | 36 | 39% | 50% |
| 8x8 | 64 | 22% | 28% |
| 10x10 | 100 | 14% | 18% |
| 12x12 | 144 | 10% | 13% |

**Strategy Preference Calculation:**
```csharp
float fillRatio = (wordCount * 4.5f) / (gridSize * gridSize);

if (fillRatio >= 0.35f)      // High density
    // 40% letter, 60% coordinate
else if (fillRatio >= 0.20f) // Medium density
    // 50% letter, 50% coordinate
else if (fillRatio >= 0.12f) // Low density
    // 65% letter, 35% coordinate
else                         // Very low density
    // 80% letter, 20% coordinate
```

### Letter Guessing Algorithm

**Purpose:** Pick the letter most likely to reveal information.

**Algorithm:**
```
CalculateLetterScores(unguessedLetters, revealedPatterns):
    scores = {}
    
    for letter in unguessedLetters:
        // Base score from English frequency
        score = BaseFrequency[letter]  // E=12.7, T=9.1, A=8.2, etc.
        
        // Bonus for pattern completion potential
        for pattern in revealedPatterns:
            if pattern.HasUnknownPositions:
                possibleWords = WordBank.FindMatches(pattern)
                letterAppearances = Count(letter appears in possibleWords)
                score += letterAppearances * PATTERN_BONUS_WEIGHT
        
        scores[letter] = score
    
    return scores sorted by value descending
```

**Selection by Skill:**
```csharp
int poolSize = skillLevel switch
{
    >= 0.9f => 1,   // Expert: always optimal
    >= 0.7f => 2,   // Hard: top 2
    >= 0.4f => 5,   // Normal: top 5
    _ => 10         // Easy: top 10 or random
};

int index = Random.Range(0, Mathf.Min(poolSize, sortedLetters.Count));
return sortedLetters[index];
```

### Coordinate Guessing Algorithm

**Purpose:** Pick the cell most likely to contain a letter.

**Algorithm:**
```
CalculateCoordinateScores(unguessedCoords, knownHits, gridSize):
    scores = {}
    
    // Adjacency bonus scales with sparsity (sparser = higher bonus)
    adjacencyBonus = Lerp(1.0, 3.0, 1.0 - fillRatio)
    
    for coord in unguessedCoords:
        score = 0
        
        // Adjacency bonus
        if IsAdjacentToAny(coord, knownHits):
            score += adjacencyBonus
        
        // Line extension bonus
        if ExtendsHitLine(coord, knownHits):
            score += LINE_EXTENSION_BONUS  // 0.5
        
        // Center bias (words often pass through center)
        distanceFromCenter = Distance(coord, gridCenter)
        maxDistance = gridSize / 2
        score += (1.0 - distanceFromCenter / maxDistance) * CENTER_BIAS_WEIGHT
        
        scores[coord] = score
    
    return scores sorted by value descending
```

### Word Guessing Algorithm

**Purpose:** Decide IF and WHAT word to guess.

**Risk:** Wrong word guess costs 2 misses.

**Algorithm:**
```
ShouldAttemptWordGuess(pattern, skillLevel, wordBank):
    // Find all words matching the pattern
    matches = wordBank.FindMatches(pattern)
    
    if matches.Count == 0:
        return (false, null)
    
    // Calculate confidence
    if matches.Count == 1:
        confidence = 0.95  // Very high
    else:
        confidence = 1.0 / matches.Count  // Decreases with more matches
    
    // Skill determines risk tolerance
    // Higher skill = willing to guess at lower confidence
    threshold = 1.0 - (skillLevel * RISK_FACTOR)  // RISK_FACTOR = 0.7
    
    // Example thresholds:
    // Skill 0.2 -> threshold 0.86 (needs 86%+ confidence)
    // Skill 0.5 -> threshold 0.65 (needs 65%+ confidence)
    // Skill 0.8 -> threshold 0.44 (needs 44%+ confidence)
    // Skill 0.95 -> threshold 0.34 (needs 34%+ confidence)
    
    if confidence >= threshold:
        return (true, matches[0])  // Attempt the most likely word
    
    return (false, null)
```

### Rubber-Banding System

**Purpose:** Keep games competitive regardless of skill gap.

**Initial Settings by Player Difficulty:**

| Player Difficulty | AI Start Skill | Hits to Increase | Misses to Decrease |
|-------------------|----------------|------------------|-------------------|
| Easy | 0.25 | 5 | 2 |
| Normal | 0.50 | 3 | 3 |
| Hard | 0.75 | 2 | 5 |

**Skill Adjustment Logic:**
```csharp
public void RecordPlayerGuess(bool wasHit)
{
    _recentGuesses.Enqueue(wasHit);
    if (_recentGuesses.Count > _config.RecentGuessesToTrack)
        _recentGuesses.Dequeue();
    
    int consecutiveHits = CountTrailingTrue();
    int consecutiveMisses = CountTrailingFalse();
    
    if (consecutiveHits >= _currentHitsToIncrease)
    {
        IncreaseSkill();
    }
    else if (consecutiveMisses >= _currentMissesToDecrease)
    {
        DecreaseSkill();
    }
}

private void IncreaseSkill()
{
    _currentSkill = Mathf.Min(_config.MaxSkillLevel, 
        _currentSkill + _config.SkillAdjustmentStep);
    
    _consecutiveIncreases++;
    _consecutiveDecreases = 0;
    
    if (_consecutiveIncreases >= _config.ConsecutiveAdjustmentsToAdapt)
    {
        AdaptThresholdsForDominatingPlayer();
    }
    
    ClearRecentGuesses();
}
```

### Adaptive Threshold System

**Purpose:** The rubber-banding system itself adapts if the player is consistently struggling or dominating.

**Tracking:** Count consecutive skill adjustments in the same direction.

**When Player Struggles (AI decreased 2+ times without increasing):**
```csharp
private void AdaptThresholdsForStrugglingPlayer()
{
    // Make it HARDER for AI to increase (protect the player)
    _currentHitsToIncrease = Mathf.Min(_config.MaxHitsToIncrease, 
        _currentHitsToIncrease + 1);
    
    // Make it EASIER for AI to decrease (help the player)
    _currentMissesToDecrease = Mathf.Max(_config.MinMissesToDecrease, 
        _currentMissesToDecrease - 1);
    
    Debug.Log(string.Format(
        "[DifficultyAdapter] Player struggling - HitsToIncrease={0}, MissesToDecrease={1}",
        _currentHitsToIncrease, _currentMissesToDecrease));
}
```

**When Player Dominates (AI increased 2+ times without decreasing):**
```csharp
private void AdaptThresholdsForDominatingPlayer()
{
    // Make it EASIER for AI to increase (challenge the player)
    _currentHitsToIncrease = Mathf.Max(_config.MinHitsToIncrease, 
        _currentHitsToIncrease - 1);
    
    // Make it HARDER for AI to decrease (maintain challenge)
    _currentMissesToDecrease = Mathf.Min(_config.MaxMissesToDecrease, 
        _currentMissesToDecrease + 1);
    
    Debug.Log(string.Format(
        "[DifficultyAdapter] Player dominating - HitsToIncrease={0}, MissesToDecrease={1}",
        _currentHitsToIncrease, _currentMissesToDecrease));
}
```

**Threshold Bounds:**
- HitsToIncrease: 1 (min) to 7 (max)
- MissesToDecrease: 1 (min) to 7 (max)

**Example Flow:**
```
Player chooses Easy:
  AI Skill: 0.25
  HitsToIncrease: 5
  MissesToDecrease: 2

Player struggles, AI decreases:
  AI Skill: 0.15 (at minimum)
  
Player keeps struggling, AI decreases again:
  Skill already at min, but threshold adapts!
  HitsToIncrease: 5 -> 6 (harder for AI to get stronger)
  MissesToDecrease: 2 -> 1 (easier for AI to get weaker)

Now player needs only 1 consecutive miss to weaken AI further,
but AI needs 6 consecutive player hits to recover!
```

### Memory System

**Purpose:** Lower-skill AI doesn't have perfect recall.

**Algorithm:**
```
GetEffectiveKnownHits(allKnownHits, skillLevel):
    if skillLevel >= 0.8:
        return allKnownHits  // Perfect memory at high skill
    
    forgetChance = (1.0 - skillLevel) * MAX_FORGET_CHANCE  // MAX = 0.3
    
    effectiveHits = []
    for i, hit in enumerate(allKnownHits):
        // Always remember most recent N guesses
        if i >= allKnownHits.Count - ALWAYS_REMEMBER_RECENT:
            effectiveHits.Add(hit)
        // Chance to forget older information
        else if Random.value > forgetChance:
            effectiveHits.Add(hit)
    
    return effectiveHits
```

**Forget Chances by Skill:**
| Skill Level | Forget Chance |
|-------------|---------------|
| 0.95 | 1.5% |
| 0.8 | 6% |
| 0.5 | 15% |
| 0.2 | 24% |
| 0.15 | 25.5% |

### ExecutionerConfigSO Full Specification

```csharp
[CreateAssetMenu(fileName = "ExecutionerConfig", menuName = "DLYH/AI/Executioner Config")]
public class ExecutionerConfigSO : ScriptableObject
{
    [Header("Skill Level Bounds")]
    [Range(0f, 1f)] public float MinSkillLevel = 0.15f;
    [Range(0f, 1f)] public float MaxSkillLevel = 0.95f;
    [Range(0f, 0.3f)] public float SkillAdjustmentStep = 0.15f;
    
    [Header("Initial Skill by Player Difficulty")]
    [Range(0f, 1f)] public float EasyStartSkill = 0.25f;
    [Range(0f, 1f)] public float NormalStartSkill = 0.50f;
    [Range(0f, 1f)] public float HardStartSkill = 0.75f;
    
    [Header("Initial Thresholds by Player Difficulty")]
    [Tooltip("Consecutive player hits before AI gets harder")]
    [Range(1, 7)] public int EasyHitsToIncrease = 5;
    [Range(1, 7)] public int EasyMissesToDecrease = 2;
    [Range(1, 7)] public int NormalHitsToIncrease = 3;
    [Range(1, 7)] public int NormalMissesToDecrease = 3;
    [Range(1, 7)] public int HardHitsToIncrease = 2;
    [Range(1, 7)] public int HardMissesToDecrease = 5;
    
    [Header("Adaptive Threshold Settings")]
    [Tooltip("Consecutive same-direction adjustments before thresholds adapt")]
    [Range(1, 5)] public int ConsecutiveAdjustmentsToAdapt = 2;
    [Range(1, 7)] public int MinHitsToIncrease = 1;
    [Range(1, 10)] public int MaxHitsToIncrease = 7;
    [Range(1, 7)] public int MinMissesToDecrease = 1;
    [Range(1, 10)] public int MaxMissesToDecrease = 7;
    
    [Header("Tracking")]
    [Tooltip("How many recent player guesses to track")]
    [Range(3, 10)] public int RecentGuessesToTrack = 5;
    
    [Header("Strategy - Grid Density Thresholds")]
    [Tooltip("Fill ratio above this = favor coordinates")]
    [Range(0f, 1f)] public float HighDensityThreshold = 0.35f;
    [Tooltip("Fill ratio below this = strongly favor letters")]
    [Range(0f, 1f)] public float LowDensityThreshold = 0.12f;
    
    [Header("Strategy - Word Guessing")]
    [Tooltip("Higher = more willing to risk word guesses at lower confidence")]
    [Range(0f, 1f)] public float WordGuessRiskFactor = 0.7f;
    
    [Header("Memory")]
    [Tooltip("Max chance to forget at lowest skill (0.3 = 30%)")]
    [Range(0f, 0.5f)] public float MaxForgetChance = 0.3f;
    [Tooltip("Always remember this many recent guesses")]
    [Range(1, 5)] public int AlwaysRememberRecent = 3;
    
    [Header("Timing")]
    public float MinThinkTime = 0.8f;
    public float MaxThinkTime = 2.5f;
}
```

---

## Previous Changes (December 12, 2025)

### Code Refactoring - COMPLETE

Major refactoring effort completed to improve maintainability and reduce script sizes:

| Script | Original | Final | Reduction |
|--------|----------|-------|-----------|
| PlayerGridPanel.cs | 2,192 | 1,120 | 49% |
| GameplayUIController.cs | 2,112 | 1,179 | 44% |
| WordPatternRow.cs | 1,378 | 1,199 | 13% |

### Extracted Controllers/Services

**From PlayerGridPanel:**
- CoordinatePlacementController (~616 lines)
- GridLayoutManager (~593 lines)
- WordPatternRowManager (~400 lines)
- LetterTrackerController (~150 lines)
- PlacementPreviewController (~50 lines)
- GridColorManager (~50 lines)

**From GameplayUIController:**
- GuessProcessor (~400 lines) - Generic service for player/opponent
- WordGuessModeController (~290 lines) - Word guess state machine

**From WordPatternRow:**
- WordGuessInputController delegation (~290 lines)
- RowDisplayBuilder (~207 lines) - Display text utility

**From SetupSettingsPanel:**
- AutocompleteManager (~200 lines) - Word suggestion logic

### Critical Bug Fix: Unity Lifecycle Timing

**Problem Discovered:**
When `StartGameplay()` activates panels and immediately calls configuration methods, controllers are null because `Start()` hasn't run yet.

**Root Cause:**
1. `SetActive(true)` triggers `Awake()` immediately
2. `ConfigureOwnerPanel()` called immediately, BEFORE `Start()` runs on next frame
3. Controllers initialized in `Start()` are null

**Solution Applied - EnsureControllersInitialized Pattern:**

```csharp
private bool _eventsWired;

private void EnsureControllersInitialized()
{
    if (_gridCellManager != null) return;  // Already initialized
    
    Debug.Log("[PlayerGridPanel] EnsureControllersInitialized - initializing before Start()");
    
    // Initialize all controllers
    _gridCellManager = new GridCellManager();
    _gridColorManager = new GridColorManager(...);
    // ... more controllers
    
    WireControllerEventsIfNeeded();
}

public void InitializeGrid(int gridSize)
{
    EnsureControllersInitialized();  // Safe to call before Start()
    // ... rest of method
}
```

### Bug Fixes (December 12, 2025)

1. **Word Placement Coordinates** - Fixed `HandleCoordinatePlacementWordPlaced()` in PlayerGridPanel.cs to call `SetPlacementPosition()`. Words now appear on grids in Gameplay Mode.

2. **Letter Width in Word Rows** - Removed `<mspace>` tags from WordPatternRow.cs `BuildDisplayText()`. Implemented monospace font (Consolas SDF) for consistent letter widths.

3. **Orphan Code Line** - Fixed incomplete Debug.Log removal that left orphan continuation line in `GenerateOpponentData()`.

### Code Quality Verification - PASSED

| Check | Status | Notes |
|-------|--------|-------|
| No `var` usage | PASS | 0 occurrences |
| No GetComponent in hot paths | PASS | Only in initialization |
| No allocations in Update | PASS | Update delegates to controller only |
| Uses string.Format (no concat) | PASS | All formatting correct |
| Private field naming (_prefix) | PASS | All fields follow convention |
| HashSets reused with Clear() | PASS | Good memory pattern |
| Update method minimal | PASS | 1 line delegation |

---

## Previous Changes (December 11, 2025)

### Gameplay Mode UI - COMPLETE

All turn-based interaction systems fully implemented:

| Feature | Status | Notes |
|---------|--------|-------|
| Letter guessing | COMPLETE | Click opponent's letter tracker |
| Coordinate guessing | COMPLETE | Click opponent's grid cells |
| Word guessing | COMPLETE | Guess Word button + keyboard mode |
| Three-color grid cells | COMPLETE | Green/yellow/red system |
| Yellow-to-green upgrade | COMPLETE | When letter is discovered |
| Duplicate guess prevention | COMPLETE | GuessResult enum |
| Solved word tracking | COMPLETE | _wordSolved flag pattern |
| Guessed word lists | COMPLETE | Under guillotines |

### Bug Fix: Solved Word Row Buttons

**Problem:** "Guess Word" button would hide momentarily but reappear on next turn.

**Root Cause:** `WordPatternRow.ExitWordGuessMode()` calls `UpdateGuessButtonStates()` which re-shows the button.

**Solution:** Added `_wordSolved` flag for permanent state:

```csharp
private bool _wordSolved = false;

public void MarkWordSolved()
{
    _wordSolved = true;
    HideGuessWordButton();
}

public void ShowGuessWordButton()
{
    if (_wordSolved) return; // Never show if solved
}
```

### Bug Fix: New Input System Migration

**Problem:** `Input.inputString` caused `InvalidOperationException` with New Input System.

**Solution:** Use `Keyboard.current`:

```csharp
using UnityEngine.InputSystem;

var keyboard = Keyboard.current;
if (keyboard == null) return;

for (int i = 0; i < 26; i++)
{
    Key key = Key.A + i;
    if (keyboard[key].wasPressedThisFrame)
    {
        char letter = (char)('A' + i);
        HandleKeyboardLetterInput(letter);
    }
}
```

---

## Previous Changes (December 9, 2025)

### Unity Version Upgrade

**Changed:** Unity 6.2 -> Unity 6.3

### MCP for Unity Update

**Changed:** MCP 8.1.x -> MCP 8.2.1

Key updates:
- `batch_execute` - New tool for running multiple MCP commands
- HTTP-First Transport - Now the default
- `manage_material` - Enhanced material management

### Package Versions (Current)

| Package | Version |
|---------|---------|
| DOTween Pro | 1.0.386 |
| Feel | 5.9.1 |
| Odin Inspector and Serializer | 4.0.1.0 |
| Odin Validator | 4.0.1.1 |
| SOAP | 3.6.1 |
| MCP for Unity | 8.2.1 (Local) |

---

## Previous Changes (December 6-8, 2025)

### CRITICAL: Opponent-Based Miss Limits

**Problem:** Original miss limit calculation used YOUR grid settings. But you're guessing against your OPPONENT's grid.

**Old (Wrong):**
```
YourMissLimit = Base + YourGridBonus + YourWordModifier + YourDifficultyModifier
```

**New (Correct):**
```
YourMissLimit = Base + OpponentGridBonus + OpponentWordModifier + YourDifficultyModifier
```

### UI Layout Restructure

**Changed from vertical to horizontal 50/50 split:**
- **Old:** Settings panel stacked above grid panel
- **New:** Settings panel (left 50%) | Grid panel (right 50%)

**Reason:** Larger grid sizes need more vertical space.

### Dynamic Cell Sizing

Grid cells resize dynamically based on grid dimensions:
```csharp
float availableWidth = gridContainer.rect.width - (gridSize + 1) * spacing;
float availableHeight = gridContainer.rect.height - (gridSize + 1) * spacing;
float cellSize = Mathf.Min(availableWidth / gridSize, availableHeight / gridSize);
```

### Package Cleanup

Reduced from 16 packages to 6 core packages.

---

## Implementation Status

### Completed Systems

| System | Status |
|--------|--------|
| Grid System (6x6-12x12) | COMPLETE |
| Word Placement | COMPLETE |
| Letter Guessing | COMPLETE |
| Coordinate Guessing | COMPLETE |
| Word Guessing (2-miss penalty) | COMPLETE |
| Letter Reveal (* -> letter) | COMPLETE |
| Turn Management | COMPLETE |
| Player System | COMPLETE |
| Game State Machine | COMPLETE |
| Difficulty System | COMPLETE |
| Word Bank (25,000+) | COMPLETE |
| Word Validation | COMPLETE |
| PlayerGridPanel | COMPLETE (1,120 lines) |
| SetupSettingsPanel | COMPLETE (~760 lines) |
| GameplayUIController | COMPLETE (1,179 lines) |
| WordPatternRow | COMPLETE (1,199 lines) |
| Three-Color Grid Cells | COMPLETE |
| Word Guess Mode | COMPLETE |
| Solved Word Tracking | COMPLETE |
| Duplicate Guess Prevention | COMPLETE |
| Guessed Word Lists | COMPLETE |
| Code Refactoring | COMPLETE |
| Grid Row Labels Resize | COMPLETE |
| Autocomplete Dropdowns | COMPLETE |
| Main Menu | COMPLETE |
| Settings Panel | COMPLETE |
| AI Design | COMPLETE |
| AI Scripts (10) | IMPLEMENTED |
| Architecture Documentation | COMPLETE |

### Phase 3: AI Opponent - IMPLEMENTED, Integration TODO

| Component | Status |
|-----------|--------|
| ExecutionerConfigSO | IMPLEMENTED |
| ExecutionerAI | IMPLEMENTED |
| DifficultyAdapter | IMPLEMENTED |
| MemoryManager | IMPLEMENTED |
| AISetupManager | IMPLEMENTED |
| IGuessStrategy | IMPLEMENTED |
| LetterGuessStrategy | IMPLEMENTED |
| CoordinateGuessStrategy | IMPLEMENTED |
| WordGuessStrategy | IMPLEMENTED |
| LetterFrequency | IMPLEMENTED |
| GridAnalyzer | IMPLEMENTED |
| **Wire to GameplayUIController** | **TODO** |
| Win/Lose UI Feedback | TODO |
| Turn Indicator Improvements | TODO |

### Phase 4: Polish and Features - TODO

| Component | Status |
|-----------|--------|
| Visual Polish (DOTween/Feel) | TODO |
| Audio Implementation | TODO |
| Invalid Word Feedback UI | TODO |
| Profanity Filter | TODO |
| Medieval Monospace Font | TODO |

### Phase 5: Multiplayer and Mobile - TODO

| Component | Status |
|-----------|--------|
| 2-Player Networking | TODO |
| Mobile Implementation | TODO |

---

## Audio Design Decisions

### Default Volume Settings

- **Sound Effects:** 50% (0.5f)
- **Music:** 50% (0.5f)

**Rationale:** Starting at 50% gives players room to adjust in either direction. Many players find games launch too loud, so a moderate default is player-friendly.

### Persistence

- Volume settings saved to PlayerPrefs
- Keys: `DLYH_SFXVolume`, `DLYH_MusicVolume`
- Static accessor methods available for audio system integration

---

## Lessons Learned

### 1. Excel Prototyping Was Invaluable
- Caught balance issues before coding
- Multiple playtests showed skill progression curve

### 2. Assumptions Must Be Tested
- "More words = harder" was completely wrong
- Miss limits needed 2-3x increase from original design
- **Miss limit source matters** - must use opponent's grid, not your own

### 3. Asymmetric Difficulty Is A Strength
- Turns a problem (skill gap) into a feature
- Enables mixed-skill gameplay

### 4. Complete File Replacements Save Time
- Searching for specific lines is error-prone
- Full file replacement is faster for multi-line changes

### 5. Event-Driven UI Is Cleaner Than Polling
- Button state management via event subscriptions
- More responsive, less CPU usage

### 6. Controller Extraction Improves Maintainability
- Breaking "God Objects" into focused controllers
- Constructor injection for dependencies
- Events for loose coupling between components

### 7. Unity Lifecycle Timing Matters (Dec 12, 2025)
- When activating GameObjects and immediately calling methods, `Start()` hasn't run yet
- Only `Awake()` has executed
- **Solution:** Add `EnsureControllersInitialized()` pattern

### 8. Defensive Initialization Pattern (Dec 12, 2025)
For scripts that might be configured before `Start()`:
```csharp
private void EnsureInitialized()
{
    if (_alreadyInitialized) return;
    // Initialize everything needed
    _alreadyInitialized = true;
}
```

### 9. Event Subscription Guards (Dec 12, 2025)
Always track whether events have been subscribed:
```csharp
private bool _eventsWired;

private void WireEvents()
{
    if (_eventsWired) return;
    // Subscribe to events
    _eventsWired = true;
}
```

### 10. Service Pattern with Callbacks (Dec 12, 2025)
Creating parameterized services eliminates duplicate code:
- Same `GuessProcessor` used for player AND opponent
- Callbacks allow different behaviors without inheritance

### 11. Debug Log Cleanup Requires Care (Dec 12, 2025)
- Removing multi-line Debug.Log statements can leave orphan continuation lines
- Always verify no orphans remain after cleanup

### 12. Static Utility Classes for Pure Functions (Dec 12, 2025)
- `RowDisplayBuilder` uses static methods with no side effects
- Shared StringBuilder avoids allocations
- Easy to test in isolation

### 13. Simple Hide/Show Insufficient for Persistent States
- Hide() can be overridden by subsequent Show() calls
- Use boolean flags (_wordSolved) for permanent state changes
- Check flags in ALL show methods

### 14. Input System Migration Matters
- Legacy Input class causes errors when New Input System is active
- Use Keyboard.current instead of Input.inputString

### 15. Three-State Cells Provide Better Feedback
- Binary hit/miss is less informative than green/yellow/red
- Yellow "partial hit" adds strategic depth

### 16. Off-By-One Indexing
- Row numbers displayed to users are 1-indexed
- Array indices are 0-indexed
- Always convert: rowIndex = rowNumber - 1

### 17. Never Assume Methods Exist (Dec 13, 2025)
- Always verify scripts via upload or MCP before calling methods
- Assuming methods exist causes delays and rework
- Added to project instructions as mandatory rule

### 18. Pragmatic Pattern Usage (Dec 13, 2025)
- Only use patterns if they add value
- Evaluated 9 AI assets, all overkill for turn-based word guessing
- Custom ~4,400 lines vs learning curve + overhead of spatial AI systems

### 19. Grid Density Affects AI Strategy (Dec 13, 2025)
- Coordinate guessing only efficient on dense grids (>35% fill)
- 12x12 with 3 words = 10% fill = 90% miss chance for coordinates
- AI must adapt strategy to grid configuration

### 20. Rubber-Banding Creates Dynamic Difficulty (Dec 13, 2025)
- Static difficulty settings feel either punishing or boring
- Tracking recent player performance allows natural adaptation
- Skill bounds prevent AI from being completely random or perfect

### 21. Adaptive Thresholds Create Meta-Adaptation (Dec 13, 2025)
- If player consistently struggles, even rubber-banding may not help enough
- Adapting the thresholds themselves provides another layer of protection
- Prevents frustration when base rubber-banding hits skill floor

### 22. Architecture Documentation Enables Faster Development (Dec 13, 2025)
- 44 scripts across 8 namespaces requires a map
- Script catalog with purposes, lines, and dependencies
- Data flow diagrams show how systems connect
- Interface contracts define boundaries between components

### 23. Interface Contracts Clarify Boundaries (Dec 13, 2025)
- IGridControllers.cs defines 5 interfaces for extracted controllers
- Clear contracts make it easier to understand what each controller does
- Enables future testing and swapping implementations

---

## Git Commits Since Nov 28, 2025

| Date | Summary |
|------|---------|
| Nov 29 | Setup mode UI improvements, button behaviors |
| Nov 30 | Auto-accept, compass hide, placement colors, SetWordLengths |
| Dec 2 | Color buttons fix, random words, word validation, letter tracker fixes |
| Dec 4 | Setup Mode complete, event-driven button states, documentation update |
| Dec 5 | Code refactoring: Controllers and Services extracted |
| Dec 8 | Gameplay UI, layout restructure, opponent-based miss limits, package cleanup |
| Dec 9 | Unity 6.3, word pattern rows fix, gameplay mode functional |
| Dec 11 | Gameplay Mode COMPLETE: letter/coordinate/word guessing, three-color cells, solved row tracking |
| Dec 12 | Refactoring COMPLETE: WordPatternRow 13%, PlayerGridPanel 49%, GameplayUIController 44%, bug fixes, code quality verification, RowDisplayBuilder extraction |
| Dec 13 | Autocomplete COMPLETE, Main Menu/Settings Panel COMPLETE, AI scripts IMPLEMENTED, Architecture doc v3 created |

---

## File Structure (Dec 13, 2025)

```
Assets/DLYH/Scripts/
  Core/
    DifficultyCalculator.cs
    DifficultySO.cs
    GameManager.cs
    TurnManager.cs
    PlayerSO.cs
    PlayerManager.cs
    Grid.cs
    GridCell.cs
    Word.cs
    WordListSO.cs
    ...
  AI/                              (Phase 3 - IMPLEMENTED)
    Config/
      ExecutionerConfigSO.cs       (~412 lines)
    Core/
      ExecutionerAI.cs             (~493 lines)
      DifficultyAdapter.cs         (~268 lines)
      MemoryManager.cs             (~442 lines)
      AISetupManager.cs            (~468 lines)
    Strategies/
      IGuessStrategy.cs            (~493 lines)
      LetterGuessStrategy.cs       (~327 lines)
      CoordinateGuessStrategy.cs   (~262 lines)
      WordGuessStrategy.cs         (~327 lines)
    Data/
      LetterFrequency.cs           (~442 lines)
      GridAnalyzer.cs              (~442 lines)
  UI/
    MainMenuController.cs (~130 lines)
    SettingsPanel.cs (~270 lines)
    PlayerGridPanel.cs (~1,120 lines)
    SetupSettingsPanel.cs (~760 lines)
    GameplayUIController.cs (~1,179 lines)
    SetupModeController.cs (~150 lines)
    WordPatternRow.cs (~1,199 lines)
    LetterButton.cs (~200 lines)
    GridCellUI.cs (~250 lines)
    AutocompleteDropdown.cs (~450 lines)
    AutocompleteItem.cs (~140 lines)
    Controllers/
      LetterTrackerController.cs (~150 lines)
      GridColorManager.cs (~130 lines)
      PlacementPreviewController.cs (~200 lines)
      WordPatternRowManager.cs (~400 lines)
      WordPatternController.cs (~285 lines)
      CoordinatePlacementController.cs (~616 lines)
      GridLayoutManager.cs (~593 lines)
      GridCellManager.cs (~150 lines)
      PlayerColorController.cs (~80 lines)
      WordGuessModeController.cs (~290 lines)
      WordGuessInputController.cs (~290 lines)
      AutocompleteManager.cs (~200 lines)
    Interfaces/
      IGridControllers.cs (~115 lines)
    Services/
      WordValidationService.cs (~60 lines)
      GuessProcessor.cs (~400 lines)
    Utilities/
      RowDisplayBuilder.cs (~207 lines)
```

---

## Project Documents

| Document | Purpose | Version |
|----------|---------|---------|
| DontLoseYourHead_GDD | Game design, mechanics, phases | v3.0 |
| DontLoseYourHead_ProjectInstructions | Development protocols, MCP tools | v3.0 |
| DESIGN_DECISIONS | Technical decisions, lessons learned | v3.0 |
| DLYH_Architecture | Script catalog, data flow, patterns | v3.0 |

---

**End of Design Decisions Document**

This is a living document updated as:
- New playtesting reveals insights
- Design questions are resolved
- Balance adjustments are made
- Implementation uncovers new considerations

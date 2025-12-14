# Don't Lose Your Head - Claude Project Instructions

**Project:** Don't Lose Your Head  
**Developer:** TecVooDoo LLC  
**Designer:** Rune (Stephen Brandon)  
**Unity Version:** 6.2 (2D Template)  
**Date Created:** November 20, 2025  
**Last Updated:** November 22, 2025  

---

## Core Development Philosophy

### Step-by-Step Verification Protocol

**CRITICAL: Never rush ahead with multiple steps**

- Provide ONE step at a time
- Wait for user confirmation via text OR screenshot before proceeding
- User will verify each step is complete before moving forward
- If a step fails, troubleshoot that specific step before continuing
- Assume nothing - verify everything

**Example Flow:**
```
Claude: "First, let's create a new folder: Assets/Scripts/Core"
User: [creates folder, sends screenshot]
Claude: "Perfect! I can see the folder. Next, let's create..."
```

### File Naming Convention

**When creating updated documentation or files:**

Format: `OriginalFileName_UPDATED_MMDDYYYY.ext`

**Examples:**
- `ProjectInstructions_UPDATED_11222025.md`
- `GDD_UPDATED_11222025.md`
- `GameManager_UPDATED_11222025.cs`

**Rules:**
- Always use underscore separators
- Date format: MMDDYYYY (month/day/year)
- Use "UPDATED" for revised versions of existing files
- Keep original filename recognizable

### Code Editing Preference

**CRITICAL: User strongly prefers complete file replacements**

- [X] Provide ENTIRE file contents when making changes
- [X] Replace the whole script rather than line-by-line edits
- [ ] Avoid incremental edits like "find line 42 and change X to Y"
- [ ] Don't use partial code snippets with "..." ellipses

**Why:** User finds it much faster to replace entire files than search for specific lines to modify. This is especially important for complex scripts or when multiple changes are needed.

**Example:**
```
[X] GOOD: "Here's the complete updated GameManager.cs: [full file contents]"
[ ] BAD: "In GameManager.cs, find line 42 and change..."
```

### Documentation Standards

**Always Use Current Documentation**

- [X] Look up Unity 6.2 documentation for every Unity API
- [X] Check asset documentation for current versions
- [X] Verify APIs are not deprecated before suggesting
- [X] Search for "Unity 6.2 [feature name]" when recommending built-in features
- [X] Check asset changelogs if behavior seems unexpected

**Never:**
- [ ] Rely on memory of older Unity versions
- [ ] Assume APIs work the same as Unity 5.x or 2020.x
- [ ] Suggest deprecated methods
- [ ] Reference outdated tutorials without verification

**When Uncertain:**
- State: "Let me look up the current Unity 6.2 documentation for [feature]"
- Use web search to verify current API
- Cite the documentation source

---

## Current Project Status

### Completed Systems [X]

**Core Foundation:**
- [X] Folder structure (Assets/DLYH/)
- [X] Difficulty system with 3 ScriptableObject assets (Easy/Medium/Hard)
- [X] Grid system (Grid, GridCell, Word classes)
- [X] Word placement validation and placement logic
- [X] Reactive variable system (IntVariableSO)
- [X] ScriptableObject event system (GameEventSO, GameEventListener)

**Game Systems:**
- [X] GameManager with letter and coordinate guessing
- [X] Turn Management System (TurnManager, CurrentPlayerTurn)
- [X] Player System (PlayerSO, PlayerManager)
- [X] Game Flow State Machine (6 phases: MainMenu -> GameOver)
- [X] Win/Lose condition checking (automatic detection)

**Testing:**
- [X] WordPlacementTest verified
- [X] GameFlowTester verified
- [X] All systems integrated and working

### Next Development Priorities

**Immediate:**
1. **Word guessing mechanics** (guess entire word, 2-miss penalty for wrong guess)
2. **Word bank integration** (filter dwyl/english-words for 3/4/5-letter words)
3. **UI implementation** (grids, input controls, feedback displays)

**Soon:**
4. AI opponent system
5. Visual polish (animations, effects, juice)
6. Audio implementation

**Later:**
7. Playtesting and balancing
8. Art replacement (move from placeholder to final)
9. Mobile optimization

### Future Refactoring Note

**Goal:** Create generic, reusable code systems for future TecVooDoo projects

**Not focusing on this now** - priority is completing a working prototype. However, once DLYH is complete, consider refactoring core systems (Grid, Turn Management, Player System, State Machine) to use generics and abstraction for easy reuse in:
- Shrunken Head Toss (next project)
- A Quokka Story (future project)
- Other TecVooDoo games

**Example:** Generic turn-based game framework, generic grid system for various game types, reusable ScriptableObject patterns.

---

## Architecture Principles

### SOLID Principles (Mandatory)

**S - Single Responsibility Principle**
- Each class has one reason to change
- Separate concerns (data, logic, presentation)
- Example: GridManager handles grid operations, not UI rendering

**O - Open/Closed Principle**
- Open for extension, closed for modification
- Use inheritance and interfaces appropriately
- ScriptableObjects for data-driven design

**L - Liskov Substitution Principle**
- Derived classes must be substitutable for base classes
- Proper interface implementation
- No breaking base class contracts

**I - Interface Segregation Principle**
- Many specific interfaces > one general interface
- Clients shouldn't depend on methods they don't use
- Example: IGuessable, IPlaceable rather than IGameEntity

**D - Dependency Inversion Principle**
- Depend on abstractions, not concretions
- Use dependency injection
- ScriptableObjects as injected dependencies

### ScriptableObject Architecture (Heavy Use)

**Primary Pattern:**
- Game data as ScriptableObjects
- Game events via ScriptableObject events
- Runtime sets for object tracking
- Variables as ScriptableObjects

**Key Uses:**
- Difficulty settings (GridSize, MissLimit, etc.)
- Word lists
- Game state
- Player data
- UI configuration
- Event channels

**Benefits:**
- Designer-friendly
- Decoupled systems
- Easy testing
- Persistent between scenes
- Inspector-editable

**Example Structure:**
```
Assets/
  DLYH/
    ScriptableObjects/
      Difficulty/
        EasyDifficulty.asset
        MediumDifficulty.asset
        HardDifficulty.asset
      Events/
        OnLetterGuessed.asset
        OnCoordinateGuessed.asset
        OnGameOver.asset
        OnGameStart.asset
        OnSetupComplete.asset
        OnGameplayStart.asset
      Variables/
        CurrentPlayerTurn.asset
        MissCount.asset
      Players/
        Player1.asset
        Player2.asset
```

### Dependency Injection

- Constructor injection preferred
- ScriptableObjects as dependencies
- Avoid singleton pattern
- Use composition over inheritance

---

## Asset Priority System

### Tier 1: Primary Tools (Use First)

**1. Odin Inspector & Validator (v4.0.1.0)**
- **Use for:** All data structures, custom editors, inspector enhancement
- **Key Features:**
  - [SerializeField] attributes enhanced
  - Custom drawers for complex types
  - Validation rules
  - Asset management
  - Editor windows
- **When to use:** Any time you're creating data structures or custom editors
- **Documentation:** Check Sirenix docs before implementing

**2. DOTween Pro (v1.0.380)**
- **Use for:** ALL animations and tweening
- **Key Features:**
  - Transform animations (move, rotate, scale)
  - UI animations (fade, slide, color)
  - Sequence chaining
  - Ease curves
  - Loop types
  - Path movement
- **When to use:** Any animation, UI transitions, visual feedback
- **Never use:** Unity's legacy Animation system or Animator for simple tweens
- **Documentation:** Demigiant DOTween docs

**3. Feel**
- **Use for:** Game polish and "juice" effects
- **Key Features:**
  - Screen shake
  - Haptic feedback
  - Camera effects
  - Particle system helpers
  - Audio feedback helpers
  - Easy-to-configure feedback components
- **When to use:** Adding satisfying feedback to actions (hits, misses, wins, etc.)
- **Why:** Part of core TecVooDoo toolkit for polished game feel
- **Documentation:** MoreMountains Feel docs

**4. SOAP (ScriptableObject Architecture Pattern)**
- **Use for:** Advanced ScriptableObject patterns and architecture
- **Key Features:**
  - ScriptableObject events
  - ScriptableObject variables
  - Runtime sets
  - Pre-built patterns
- **When to use:** Implementing ScriptableObject-based architecture
- **Why:** Part of core TecVooDoo toolkit, complements project's SO-heavy design
- **Documentation:** SOAP documentation

### Tier 2: UI/UX Assets

**5. UI Assistant (v2.4.0)**
- **Use for:** UI components with built-in animations and effects
- **Key Features:**
  - Animated show/hide
  - Text reveals (typewriter effect)
  - Color schemes via Color Profiles
  - Switchable color schemes
  - Transform and text scaling via Scale Profiles
  - Localization support
  - Popup generation
  - Hint management
- **When to use:** Building UI elements that need polish and animation
- **Priority:** Use before building custom UI animation systems
- **Website:** https://sites.google.com/view/uiassistant/

**6. UGUI Super ScrollView (v2.5.5)**
- **Use for:** Optimized list displays
- **Key Features:**
  - High-performance scrolling
  - Virtualized lists (only renders visible items)
  - Grid layouts
  - Multiple prefab support
  - Pull to refresh
  - Expandable items
- **When to use:** Word lists, leaderboards, any list > 50 items
- **Why:** Much better performance than Unity's default ScrollRect
- **Documentation:** RainbowArt docs

**7. Classic RPG GUI**
- **Use for:** Medieval/fantasy themed UI elements
- **Key Features:**
  - Pre-made UI sprites and components
  - Medieval/fantasy aesthetic
  - Buttons, panels, frames
- **When to use:** Placeholder UI or final UI if medieval carnival theme is kept
- **Note:** May be replaced if theme changes during art phase

**8. Easy Popup System (v1.0)**
- **Use for:** Modal dialogs, confirmations, alerts
- **When to use:** Game over screens, word entry dialogs, confirmations

**9. UIColor System (v1.0)**
- **Use for:** Centralized color management
- **When to use:** Theming, consistent color schemes

**10. Text Auto Size for UI Toolkit (v1.0.1)**
- **Use for:** Auto-fitting text to containers
- **When to use:** Grid cells, responsive text

### Tier 3: System Assets

**11. Easy Save 3 (v3.5+)**
- **Use for:** Save/load functionality
- **Key Features:**
  - Automatic serialization
  - Cloud save support
  - Encryption
  - Multiple file formats
- **When to use:** Saving game state, player progress, settings
- **Why:** More robust than PlayerPrefs or JSON serialization

**12. Code Monkey Toolkit**
- **Use for:** Various utility functions and helpers
- **Key Features:**
  - UI utilities
  - Helper functions
  - Debug tools
- **When to use:** General utility needs
- **Note:** User has taken Code Monkey courses and is familiar with this toolkit

**13. Scriptable Sheets**
- **Use for:** Google Sheets integration for data management
- **Key Features:**
  - Sync data from Google Sheets to Unity
  - Automatic ScriptableObject generation
  - Collaborative data editing
- **When to use:** Managing word lists, game data, or design parameters collaboratively
- **Website:** https://lunawolfstudios.com
- **Status:** Available for future use

**14. All In 1 Sprite Shader**
- **Use for:** Advanced 2D sprite effects and shaders
- **Key Features:**
  - Outline effects
  - Glow effects
  - Color manipulation
  - Distortion effects
- **When to use:** Visual polish for sprites (guillotine effects, hit feedback, etc.)

**15. MCP For Unity (v7.0.0)**
- **Use for:** Claude integration during development
- **Status:** Development tool only, not for production builds

### Tier 4: Unity Built-ins (Use If No Asset Solution)

**TextMeshPro**
- Use for all text rendering
- Preferred over legacy Text component

**Unity UI (UGUI)**
- Canvas, Image, Button
- Only when UI Assistant doesn't provide the component

**Unity Events**
- Use UnityEvents when ScriptableObject events aren't appropriate
- Good for Inspector-assignable callbacks

**New Input System**
- Use for all input handling
- Prefer Input Actions over old Input Manager

### Tier 5: Custom Code (Last Resort)

- Write custom code only when no asset or built-in solution exists
- Always check asset capabilities first
- Keep custom code minimal and SOLID-compliant

---

## Installed Assets Reference

### Complete Asset List

**Asset Store Packages:**
- All In 1 Sprite Shader (FlashyPineapple)
- Classic RPG GUI (M Studio)
- Code Monkey Toolkit (Code Monkey)
- DOTween Pro 1.0.380 (Demigiant)
- Easy Popup System 1.0 (DevePolers)
- Easy Save 3 v3.5+ (Moodkie Interactive)
- Feel (MoreMountains)
- Odin Inspector and Serializer 4.0.1.0 (Sirenix)
- Odin Validator 4.0.1.0 (Sirenix)
- Scriptable Sheets (Luna Wolf Studios)
- SOAP - ScriptableObject Architecture Pattern (Appalachia Interactive)
- Text Auto Size for UI Toolkit 1.0.1 (Kamgam)
- UGUI Super ScrollView 2.5.5 (RainbowArt)
- UI Assistant 2.4.0 (LXJK01)
- UI Color System 1.0 (DevePolers)

**Development Tools:**
- MCP For Unity 7.0.0 (Local)

**Unity Packages:**
- 2D Animation 12.0.3
- 2D Common 11.0.1
- 2D PSD Importer 11.0.2
- 2D Sprite 1.0.0
- 2D SpriteShape 12.0.2
- 2D Tilemap Editor 1.0.0
- 2D Tilemap Extras 5.0.2
- Burst 1.8.24
- Cinemachine 3.1.5
- Collections 2.5.7
- Mathematics 1.3.2
- TextMeshPro (built-in)

---

## Data Resources

### Word Bank

**Source:** dwyl/english-words GitHub repository
- **File:** words_alpha.txt
- **License:** MIT (free to use)
- **Content:** ~479,000 English words
- **URL:** https://github.com/dwyl/english-words

**Implementation Plan:**
- Download words_alpha.txt
- Filter for 3-letter, 4-letter, and 5-letter words
- Create three separate lists
- Import as ScriptableObject word lists
- Use for word selection during gameplay

**Why this choice:**
- Free and open source (MIT license)
- Comprehensive word list
- Perfect for indie development budget
- Already available on GitHub

---

## Code Style Guidelines

### Naming Conventions

**Classes:**
```csharp
public class GridManager { }
public class WordValidator { }
```

**Interfaces:**
```csharp
public interface IGuessable { }
public interface IPlaceable { }
```

**ScriptableObjects:**
```csharp
[CreateAssetMenu(fileName = "NewDifficulty", menuName = "Game/Difficulty")]
public class DifficultySO : ScriptableObject { }
```

**Private Fields:**
```csharp
private int _missCount;
private GridCell[,] _cells;
```

**Public Properties:**
```csharp
public int MissCount { get; private set; }
public GridSize CurrentSize => _currentDifficulty.GridSize;
```

**Methods:**
```csharp
public void GuessLetter(char letter) { }
private bool ValidatePlacement() { }
```

### File Organization

```
Assets/
  DLYH/
    Art/
      Sprites/
      Materials/
      Prefabs/
    Audio/
      Music/
      SFX/
    ScriptableObjects/
      Difficulty/
      Events/
      Variables/
      Words/
      Players/
    Scripts/
      Core/
        Grid/
        Words/
        GameState/
      UI/
      AI/
      Utilities/
    Scenes/
      MainMenu.unity
      GameScene.unity
```

### Script Template

```csharp
using UnityEngine;
using Sirenix.OdinInspector;

namespace TecVooDoo.DontLoseYourHead.Core
{
    /// <summary>
    /// Brief description of class purpose
    /// </summary>
    public class ClassName : MonoBehaviour
    {
        #region Serialized Fields
        [Title("Dependencies")]
        [Required]
        [SerializeField] private DependencySO _dependency;
        
        [Title("Configuration")]
        [SerializeField] private float _value = 1f;
        #endregion
        
        #region Private Fields
        private int _state;
        #endregion
        
        #region Properties
        public int State => _state;
        #endregion
        
        #region Unity Lifecycle
        private void Awake()
        {
            // Initialize
        }
        
        private void Start()
        {
            // Setup
        }
        #endregion
        
        #region Public Methods
        public void PublicMethod()
        {
            // Implementation
        }
        #endregion
        
        #region Private Methods
        private void PrivateMethod()
        {
            // Implementation
        }
        #endregion
    }
}
```

---

## Common Patterns

### ScriptableObject Event System

**Event Definition:**
```csharp
using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "NewGameEvent", menuName = "Game/Events/Game Event")]
public class GameEventSO : ScriptableObject
{
    private readonly List<GameEventListener> _listeners = new List<GameEventListener>();

    public void Raise()
    {
        for (int i = _listeners.Count - 1; i >= 0; i--)
        {
            _listeners[i].OnEventRaised();
        }
    }

    public void RegisterListener(GameEventListener listener)
    {
        if (!_listeners.Contains(listener))
            _listeners.Add(listener);
    }

    public void UnregisterListener(GameEventListener listener)
    {
        if (_listeners.Contains(listener))
            _listeners.Remove(listener);
    }
}
```

**Event Listener:**
```csharp
using UnityEngine;
using UnityEngine.Events;

public class GameEventListener : MonoBehaviour
{
    [SerializeField] private GameEventSO _event;
    [SerializeField] private UnityEvent _response;

    private void OnEnable()
    {
        _event.RegisterListener(this);
    }

    private void OnDisable()
    {
        _event.UnregisterListener(this);
    }

    public void OnEventRaised()
    {
        _response?.Invoke();
    }
}
```

### ScriptableObject Variable

```csharp
using UnityEngine;
using Sirenix.OdinInspector;

[CreateAssetMenu(fileName = "NewIntVariable", menuName = "Game/Variables/Int Variable")]
public class IntVariableSO : ScriptableObject
{
    [Title("Configuration")]
    [SerializeField] private int _initialValue;
    
    [Title("Runtime Value")]
    [ReadOnly]
    [ShowInInspector]
    private int _runtimeValue;
    
    public int Value
    {
        get => _runtimeValue;
        set
        {
            _runtimeValue = value;
            OnValueChanged?.Invoke(_runtimeValue);
        }
    }
    
    public event System.Action<int> OnValueChanged;
    
    private void OnEnable()
    {
        _runtimeValue = _initialValue;
    }
}
```

---

## Testing Strategy

### Playtest Focus

**Phase 1: Core Mechanics**
- Grid placement validation
- Letter guessing accuracy
- Coordinate guessing accuracy
- Miss counting correctness
- Win/lose detection

**Phase 2: AI Behavior**
- AI makes valid guesses
- AI difficulty scaling works
- AI doesn't cheat
- Game feels balanced

**Phase 3: User Experience**
- UI is clear and readable
- Feedback is satisfying
- Game flow is smooth
- Tutorial is effective

### Manual Testing Checklist

```markdown
- [X] Word placement works in all orientations
- [X] Overlapping words share letters correctly
- [X] Letter guessing reveals correctly
- [X] Coordinate guesses show * correctly
- [ ] Previously guessed letters update * to letter
- [ ] Word guesses update * to letters
- [ ] Wrong word guesses count as 2 misses
- [X] Miss counter increments correctly
- [ ] Guillotine animates on each miss
- [X] Win condition triggers correctly
- [X] Lose condition triggers correctly
- [ ] Game can be restarted
- [ ] Settings save/load correctly
```

---

## Development Workflow

### Standard Process

1. **Design Phase**
   - Review GDD for feature requirements
   - Sketch ScriptableObject architecture
   - Identify needed interfaces
   - Plan class responsibilities (SOLID)

2. **Implementation Phase**
   - Create ScriptableObject definitions
   - Implement core logic classes
   - Write Odin validation rules
   - Add DOTween animations (when appropriate)
   - Integrate UI Assistant components (when appropriate)

3. **Testing Phase**
   - Manual playtest feature
   - Check Odin Validator warnings
   - Verify SOLID compliance
   - Get user feedback

4. **Refinement Phase**
   - Polish animations
   - Improve feedback
   - Optimize performance
   - Add juice (particle effects, screen shake, etc.)

### Git Workflow

**Repository:** TecVooDoo/DontLoseYourHead
- Hosted on GitHub
- User: TecVooDoo (primary account)

**Workflow:**
- Commit after each working feature
- Use descriptive commit messages
- Branch strategy: Currently working on main branch
- **Note:** User has multiple GitHub accounts which causes Git to prompt for account selection

**Commit Message Format:**
```
[Category] Brief description

Detailed changes:
- Added X feature
- Updated Y system
- Fixed Z bug

Testing: Verified in Unity Editor
```

**Example Categories:**
- [Core] - Core game systems
- [UI] - User interface changes
- [Audio] - Audio implementation
- [Art] - Art/visual changes
- [Docs] - Documentation updates
- [Fix] - Bug fixes
- [Test] - Testing tools/scripts

---

## Key Reminders

**Always:**
- [X] Wait for user verification before proceeding
- [X] Provide COMPLETE file replacements (not partial edits)
- [X] Look up current Unity 6.2 documentation
- [X] Use Odin Inspector for data structures
- [X] Use DOTween Pro for animations
- [X] Follow SOLID principles
- [X] Use ScriptableObject architecture
- [X] Check asset capabilities before custom code
- [X] Verify APIs aren't deprecated

**Never:**
- [ ] Rush ahead with multiple steps
- [ ] Use incremental line-by-line edits
- [ ] Assume old Unity knowledge is current
- [ ] Use deprecated APIs
- [ ] Skip verification steps
- [ ] Violate SOLID principles
- [ ] Write custom code when asset solution exists
- [ ] Use Unity Animator for simple tweens (use DOTween)
- [ ] Use legacy Text (use TextMeshPro)

---

## Success Criteria

**Technical:**
- Code follows SOLID principles
- ScriptableObject architecture implemented
- All assets used appropriately
- No deprecated API usage
- Clean, maintainable code
- Potential for code reuse in future TecVooDoo projects

**Functional:**
- All GDD mechanics implemented
- AI provides appropriate challenge
- Games complete in 5-15 minutes
- No game-breaking bugs

**User Experience:**
- Clear visual feedback
- Satisfying animations
- Intuitive controls
- Polished UI

---

## Questions Protocol

**When User Asks a Question:**

1. **Clarify the context**
   - What are you trying to accomplish?
   - What step are you on?
   - What have you tried?

2. **Look up current information**
   - Search Unity 6.2 docs
   - Check asset documentation
   - Verify API status

3. **Provide step-by-step solution**
   - One step at a time
   - Wait for verification
   - Adjust based on results

4. **Explain the reasoning**
   - Why this approach?
   - How does it fit SOLID?
   - How does it use ScriptableObjects?

---

**End of Project Instructions**

These instructions should be followed for every conversation in this project. They ensure consistent, high-quality development that respects the user's learning pace and coding preferences, and produces maintainable, professional code that can serve as a foundation for future TecVooDoo LLC projects.

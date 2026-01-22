using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace DLYH.TableUI
{
    /// <summary>
    /// Data for displaying guillotine state in the overlay.
    /// </summary>
    public class GuillotineData
    {
        public string Name;
        public Color Color;
        public int MissCount;
        public int MissLimit;
        public bool IsLocalPlayer;
    }

    /// <summary>
    /// Manages the guillotine overlay modal that shows both players' danger status.
    /// Uses a 5-stage system where the blade only moves at stage transitions,
    /// not on every individual miss.
    ///
    /// Stage thresholds:
    /// - Stage 1: 0-20% misses (safe)
    /// - Stage 2: 20-40% misses (getting warm)
    /// - Stage 3: 40-60% misses (danger)
    /// - Stage 4: 60-80% misses (high danger)
    /// - Stage 5: 80-100% misses (critical)
    /// </summary>
    public class GuillotineOverlayManager
    {
        #region Constants

        private const int TOTAL_STAGES = 5;

        // Stage thresholds as percentages
        private static readonly float[] STAGE_THRESHOLDS = new float[] { 0f, 20f, 40f, 60f, 80f };

        // Face expressions based on stage
        private static readonly string FACE_HAPPY = ":-)";
        private static readonly string FACE_NEUTRAL = ":-|";
        private static readonly string FACE_WORRIED = ":-/";
        private static readonly string FACE_SCARED = ":-O";
        private static readonly string FACE_HORROR = "X-O";
        private static readonly string FACE_EVIL = ">:-)";

        // Flavor text by stage
        private static readonly string[][] STAGE_FLAVOR = new string[][]
        {
            new string[] { "Safe for now...", "Breathing easy.", "No worries yet.", "Sitting pretty." },
            new string[] { "Getting warm...", "Starting to sweat.", "The blade rises.", "Things heating up." },
            new string[] { "In danger!", "Neck exposed!", "One wrong move...", "Walking on thin ice!" },
            new string[] { "High danger!", "Almost there...", "Time running out!", "Feel the blade..." },
            new string[] { "CRITICAL!", "Final moments!", "Say your prayers!", "It's almost over!" }
        };

        #endregion

        #region Private Fields

        private VisualElement _root;
        private VisualElement _overlayRoot;
        private VisualElement _backdrop;
        private VisualElement _content;
        private Button _closeButton;
        private Label _titleLabel;

        // Player guillotine elements
        private VisualElement _playerPanel;
        private VisualElement _playerColorBadge;
        private Label _playerNameLabel;
        private VisualElement _playerBladeGroup;
        private VisualElement _playerRope;
        private VisualElement _playerHead;
        private Label _playerHeadFace;
        private Label _playerMissLabel;
        private VisualElement _playerDangerFill;
        private Label _playerFlavorText;
        private Label _playerStageLabel;
        private VisualElement _playerStageTrack;
        private List<VisualElement> _playerStageSegments = new List<VisualElement>();

        // Opponent guillotine elements
        private VisualElement _opponentPanel;
        private VisualElement _opponentColorBadge;
        private Label _opponentNameLabel;
        private VisualElement _opponentBladeGroup;
        private VisualElement _opponentRope;
        private VisualElement _opponentHead;
        private Label _opponentHeadFace;
        private Label _opponentMissLabel;
        private VisualElement _opponentDangerFill;
        private Label _opponentFlavorText;
        private Label _opponentStageLabel;
        private VisualElement _opponentStageTrack;
        private List<VisualElement> _opponentStageSegments = new List<VisualElement>();

        // Lever elements
        private VisualElement _playerLeverArm;
        private VisualElement _opponentLeverArm;

        // State
        private bool _isVisible;
        private GuillotineData _playerData;
        private GuillotineData _opponentData;

        #endregion

        #region Events

        /// <summary>Fired when the overlay is closed.</summary>
        public event Action OnClosed;

        #endregion

        #region Initialization

        /// <summary>
        /// Initializes the guillotine overlay manager with the given root element.
        /// </summary>
        public void Initialize(VisualElement root)
        {
            _root = root ?? throw new ArgumentNullException(nameof(root));

            _overlayRoot = _root.Q<VisualElement>("guillotine-overlay-root");

            if (_overlayRoot == null)
            {
                Debug.LogWarning("[GuillotineOverlayManager] Could not find guillotine-overlay-root element.");
                return;
            }

            QueryElements();
            WireEvents();

            // Ensure hidden on start
            Hide();
        }

        private void QueryElements()
        {
            _backdrop = _overlayRoot.Q<VisualElement>("overlay-backdrop");
            _content = _overlayRoot.Q<VisualElement>("overlay-content");
            _closeButton = _overlayRoot.Q<Button>("btn-close-overlay");
            _titleLabel = _overlayRoot.Q<Label>("overlay-title");

            // Player elements
            _playerPanel = _overlayRoot.Q<VisualElement>("guillotine-player");
            _playerColorBadge = _overlayRoot.Q<VisualElement>("player-color-badge");
            _playerNameLabel = _overlayRoot.Q<Label>("player-guillotine-name");
            _playerBladeGroup = _overlayRoot.Q<VisualElement>("player-blade-group");
            _playerRope = _overlayRoot.Q<VisualElement>("player-rope");
            _playerHead = _overlayRoot.Q<VisualElement>("player-head");
            _playerHeadFace = _overlayRoot.Q<Label>("player-head-face");
            _playerMissLabel = _overlayRoot.Q<Label>("player-miss-label");
            _playerDangerFill = _overlayRoot.Q<VisualElement>("player-danger-fill");
            _playerFlavorText = _overlayRoot.Q<Label>("player-flavor-text");
            _playerStageLabel = _overlayRoot.Q<Label>("player-stage-label");
            _playerStageTrack = _overlayRoot.Q<VisualElement>("player-stage-track");

            // Cache player stage segments
            _playerStageSegments.Clear();
            if (_playerStageTrack != null)
            {
                for (int i = 1; i <= TOTAL_STAGES; i++)
                {
                    VisualElement segment = _playerStageTrack.Q<VisualElement>(className: $"segment-{i}");
                    if (segment != null)
                    {
                        _playerStageSegments.Add(segment);
                    }
                }
            }

            // Opponent elements
            _opponentPanel = _overlayRoot.Q<VisualElement>("guillotine-opponent");
            _opponentColorBadge = _overlayRoot.Q<VisualElement>("opponent-color-badge");
            _opponentNameLabel = _overlayRoot.Q<Label>("opponent-guillotine-name");
            _opponentBladeGroup = _overlayRoot.Q<VisualElement>("opponent-blade-group");
            _opponentRope = _overlayRoot.Q<VisualElement>("opponent-rope");
            _opponentHead = _overlayRoot.Q<VisualElement>("opponent-head");
            _opponentHeadFace = _overlayRoot.Q<Label>("opponent-head-face");
            _opponentMissLabel = _overlayRoot.Q<Label>("opponent-miss-label");
            _opponentDangerFill = _overlayRoot.Q<VisualElement>("opponent-danger-fill");
            _opponentFlavorText = _overlayRoot.Q<Label>("opponent-flavor-text");
            _opponentStageLabel = _overlayRoot.Q<Label>("opponent-stage-label");
            _opponentStageTrack = _overlayRoot.Q<VisualElement>("opponent-stage-track");

            // Cache opponent stage segments
            _opponentStageSegments.Clear();
            if (_opponentStageTrack != null)
            {
                for (int i = 1; i <= TOTAL_STAGES; i++)
                {
                    VisualElement segment = _opponentStageTrack.Q<VisualElement>(className: $"segment-{i}");
                    if (segment != null)
                    {
                        _opponentStageSegments.Add(segment);
                    }
                }
            }

            // Lever elements
            _playerLeverArm = _overlayRoot.Q<VisualElement>("player-lever-arm");
            _opponentLeverArm = _overlayRoot.Q<VisualElement>("opponent-lever-arm");
        }

        private void WireEvents()
        {
            if (_closeButton != null)
            {
                _closeButton.clicked += Hide;
            }

            if (_backdrop != null)
            {
                _backdrop.RegisterCallback<ClickEvent>(evt => Hide());
            }
        }

        #endregion

        #region Show/Hide

        /// <summary>
        /// Shows the guillotine overlay with current player data.
        /// </summary>
        public void Show(GuillotineData playerData, GuillotineData opponentData)
        {
            Show(playerData, opponentData, -1, -1);
        }

        /// <summary>
        /// Shows the guillotine overlay with optional initial blade positions.
        /// If initialPlayerStage or initialOpponentStage is >= 1, the blade starts at that position
        /// instead of the current stage (for delayed animation effect).
        /// </summary>
        public void Show(GuillotineData playerData, GuillotineData opponentData, int initialPlayerStage, int initialOpponentStage)
        {
            _playerData = playerData;
            _opponentData = opponentData;

            UpdatePlayerDisplay(initialPlayerStage);
            UpdateOpponentDisplay(initialOpponentStage);

            _overlayRoot?.RemoveFromClassList("hidden");
            _isVisible = true;
        }

        /// <summary>
        /// Animates the blade and lever to the current stage position.
        /// Call this after a delay when showing the overlay with initial stages.
        /// </summary>
        public void AnimateToCurrentStage(bool isPlayer)
        {
            if (isPlayer && _playerData != null)
            {
                float percent = GetDangerPercent(_playerData);
                int stage = GetStageFromPercent(percent);
                UpdateBladePosition(_playerBladeGroup, stage);
                UpdateLeverPosition(_playerLeverArm, stage);
            }
            else if (!isPlayer && _opponentData != null)
            {
                float percent = GetDangerPercent(_opponentData);
                int stage = GetStageFromPercent(percent);
                UpdateBladePosition(_opponentBladeGroup, stage);
                UpdateLeverPosition(_opponentLeverArm, stage);
            }
        }

        /// <summary>
        /// Hides the guillotine overlay.
        /// </summary>
        public void Hide()
        {
            _overlayRoot?.AddToClassList("hidden");
            _isVisible = false;
            OnClosed?.Invoke();
        }

        /// <summary>
        /// Returns true if the overlay is currently visible.
        /// </summary>
        public bool IsVisible => _isVisible;

        #endregion

        #region Update Display

        private void UpdatePlayerDisplay(int initialStage = -1)
        {
            if (_playerData == null) return;

            // Name and color
            if (_playerNameLabel != null)
            {
                _playerNameLabel.text = _playerData.Name ?? "Player";
            }
            if (_playerColorBadge != null)
            {
                _playerColorBadge.style.backgroundColor = _playerData.Color;
            }
            // Head background color disabled - using art assets now
            // TODO: Re-enable when hair-colored head variants are available
            // if (_playerHead != null)
            // {
            //     _playerHead.style.backgroundColor = _playerData.Color;
            // }

            // Calculate stage and percentage
            float percent = GetDangerPercent(_playerData);
            int stage = GetStageFromPercent(percent);

            // Use initial stage for blade/lever if specified (for delayed animation)
            int bladeStage = initialStage >= 1 ? initialStage : stage;

            // Miss counter text
            if (_playerMissLabel != null)
            {
                _playerMissLabel.text = $"Misses: {_playerData.MissCount} / {_playerData.MissLimit}";
            }

            // Stage label
            if (_playerStageLabel != null)
            {
                _playerStageLabel.text = $"Stage {stage} / {TOTAL_STAGES}";
                UpdateStageLabelClass(_playerStageLabel, stage);
            }

            // Danger bar
            if (_playerDangerFill != null)
            {
                _playerDangerFill.style.width = new StyleLength(new Length(percent, LengthUnit.Percent));
                UpdateDangerClass(_playerDangerFill, percent);
            }

            // Stage segments (light up segments up to current stage)
            UpdateStageSegments(_playerStageSegments, stage);

            // Blade position (uses stage class) - may use initial stage for delayed animation
            UpdateBladePosition(_playerBladeGroup, bladeStage);

            // Rope length (matches blade position)
            UpdateRopeLength(_playerRope, bladeStage);

            // Lever position (uses stage class) - may use initial stage for delayed animation
            UpdateLeverPosition(_playerLeverArm, bladeStage);

            // Face expression
            if (_playerHeadFace != null)
            {
                _playerHeadFace.text = GetFaceExpression(stage, false);
            }

            // Flavor text
            if (_playerFlavorText != null)
            {
                _playerFlavorText.text = GetFlavorText(stage);
                UpdateFlavorTextClass(_playerFlavorText, stage);
            }
        }

        private void UpdateOpponentDisplay(int initialStage = -1)
        {
            if (_opponentData == null) return;

            // Name and color
            if (_opponentNameLabel != null)
            {
                _opponentNameLabel.text = _opponentData.Name ?? "Opponent";
            }
            if (_opponentColorBadge != null)
            {
                _opponentColorBadge.style.backgroundColor = _opponentData.Color;
            }
            // Head background color disabled - using art assets now
            // TODO: Re-enable when hair-colored head variants are available
            // if (_opponentHead != null)
            // {
            //     _opponentHead.style.backgroundColor = _opponentData.Color;
            // }

            // Calculate stage and percentage
            float percent = GetDangerPercent(_opponentData);
            int stage = GetStageFromPercent(percent);

            // Use initial stage for blade/lever if specified (for delayed animation)
            int bladeStage = initialStage >= 1 ? initialStage : stage;

            // Miss counter text
            if (_opponentMissLabel != null)
            {
                _opponentMissLabel.text = $"Misses: {_opponentData.MissCount} / {_opponentData.MissLimit}";
            }

            // Stage label
            if (_opponentStageLabel != null)
            {
                _opponentStageLabel.text = $"Stage {stage} / {TOTAL_STAGES}";
                UpdateStageLabelClass(_opponentStageLabel, stage);
            }

            // Danger bar
            if (_opponentDangerFill != null)
            {
                _opponentDangerFill.style.width = new StyleLength(new Length(percent, LengthUnit.Percent));
                UpdateDangerClass(_opponentDangerFill, percent);
            }

            // Stage segments (light up segments up to current stage)
            UpdateStageSegments(_opponentStageSegments, stage);

            // Blade position (uses stage class) - may use initial stage for delayed animation
            UpdateBladePosition(_opponentBladeGroup, bladeStage);

            // Rope length (matches blade position)
            UpdateRopeLength(_opponentRope, bladeStage);

            // Lever position (uses stage class) - may use initial stage for delayed animation
            UpdateLeverPosition(_opponentLeverArm, bladeStage);

            // Face expression
            if (_opponentHeadFace != null)
            {
                _opponentHeadFace.text = GetFaceExpression(stage, true);
            }

            // Flavor text
            if (_opponentFlavorText != null)
            {
                _opponentFlavorText.text = GetFlavorText(stage);
                UpdateFlavorTextClass(_opponentFlavorText, stage);
            }
        }

        /// <summary>
        /// Updates both displays with fresh data.
        /// </summary>
        public void RefreshDisplay(GuillotineData playerData, GuillotineData opponentData)
        {
            _playerData = playerData;
            _opponentData = opponentData;

            if (_isVisible)
            {
                UpdatePlayerDisplay();
                UpdateOpponentDisplay();
            }
        }

        #endregion

        #region Helper Methods

        private float GetDangerPercent(GuillotineData data)
        {
            if (data == null || data.MissLimit <= 0) return 0f;
            return Mathf.Clamp01((float)data.MissCount / data.MissLimit) * 100f;
        }

        /// <summary>
        /// Converts a danger percentage to a stage number (1-5).
        /// Each stage represents 1/5 of the travel distance.
        /// Stage 1: 0-20%, Stage 2: 20-40%, Stage 3: 40-60%, Stage 4: 60-80%, Stage 5: 80-100%
        /// At 100% (game over), blade is at stage 5 and execution sequence plays (lever drop, blade drop).
        /// </summary>
        private int GetStageFromPercent(float percent)
        {
            if (percent >= 80f) return 5;
            if (percent >= 60f) return 4;
            if (percent >= 40f) return 3;
            if (percent >= 20f) return 2;
            return 1;
        }

        private void UpdateStageSegments(List<VisualElement> segments, int currentStage)
        {
            for (int i = 0; i < segments.Count; i++)
            {
                VisualElement segment = segments[i];
                int segmentStage = i + 1;

                if (segmentStage <= currentStage)
                {
                    segment.AddToClassList("active");
                }
                else
                {
                    segment.RemoveFromClassList("active");
                }
            }
        }

        private void UpdateBladePosition(VisualElement bladeGroup, int stage)
        {
            if (bladeGroup == null) return;

            // Remove all stage classes
            for (int i = 1; i <= TOTAL_STAGES; i++)
            {
                bladeGroup.RemoveFromClassList($"stage-{i}");
            }

            // Add current stage class
            bladeGroup.AddToClassList($"stage-{stage}");
        }

        /// <summary>
        /// Sets the blade to a specific stage immediately without animation.
        /// Used when restoring game state from a resumed game.
        /// </summary>
        /// <param name="isPlayer">True for player's blade, false for opponent's blade</param>
        /// <param name="stage">Stage to set (1-5)</param>
        public void SetBladeStageImmediately(bool isPlayer, int stage)
        {
            stage = Mathf.Clamp(stage, 1, TOTAL_STAGES);

            if (isPlayer)
            {
                UpdateBladePosition(_playerBladeGroup, stage);
                UpdateRopeLength(_playerRope, stage);
                UpdateLeverPosition(_playerLeverArm, stage);
            }
            else
            {
                UpdateBladePosition(_opponentBladeGroup, stage);
                UpdateRopeLength(_opponentRope, stage);
                UpdateLeverPosition(_opponentLeverArm, stage);
            }

            Debug.Log($"[GuillotineOverlayManager] Set {(isPlayer ? "player" : "opponent")} blade to stage {stage} immediately");
        }

        private void UpdateRopeLength(VisualElement rope, int stage)
        {
            if (rope == null) return;

            // Remove all stage classes
            for (int i = 1; i <= TOTAL_STAGES; i++)
            {
                rope.RemoveFromClassList($"stage-{i}");
            }

            // Add current stage class
            rope.AddToClassList($"stage-{stage}");
        }

        private void UpdateLeverPosition(VisualElement leverArm, int stage)
        {
            if (leverArm == null) return;

            // Remove all stage classes
            for (int i = 1; i <= TOTAL_STAGES; i++)
            {
                leverArm.RemoveFromClassList($"stage-{i}");
            }

            // Add current stage class
            leverArm.AddToClassList($"stage-{stage}");
        }

        private void UpdateDangerClass(VisualElement fillBar, float percent)
        {
            fillBar.RemoveFromClassList("danger-low");
            fillBar.RemoveFromClassList("danger-medium");
            fillBar.RemoveFromClassList("danger-high");
            fillBar.RemoveFromClassList("danger-critical");

            if (percent >= 80)
            {
                fillBar.AddToClassList("danger-critical");
            }
            else if (percent >= 60)
            {
                fillBar.AddToClassList("danger-high");
            }
            else if (percent >= 40)
            {
                fillBar.AddToClassList("danger-medium");
            }
            else
            {
                fillBar.AddToClassList("danger-low");
            }
        }

        private void UpdateStageLabelClass(Label label, int stage)
        {
            label.RemoveFromClassList("danger");
            label.RemoveFromClassList("critical");

            if (stage >= 5)
            {
                label.AddToClassList("critical");
            }
            else if (stage >= 4)
            {
                label.AddToClassList("danger");
            }
        }

        private string GetFaceExpression(int stage, bool isOpponent)
        {
            // For opponent, we're happy when they're in danger
            if (isOpponent)
            {
                switch (stage)
                {
                    case 5: return FACE_EVIL;
                    case 4: return FACE_HAPPY;
                    case 3: return FACE_NEUTRAL;
                    case 2: return FACE_WORRIED;
                    default: return FACE_WORRIED;
                }
            }
            else
            {
                // For player, we're scared when in danger
                switch (stage)
                {
                    case 5: return FACE_HORROR;
                    case 4: return FACE_SCARED;
                    case 3: return FACE_WORRIED;
                    case 2: return FACE_NEUTRAL;
                    default: return FACE_NEUTRAL;
                }
            }
        }

        private string GetFlavorText(int stage)
        {
            int index = Mathf.Clamp(stage - 1, 0, STAGE_FLAVOR.Length - 1);
            string[] pool = STAGE_FLAVOR[index];
            return pool[UnityEngine.Random.Range(0, pool.Length)];
        }

        private void UpdateFlavorTextClass(Label label, int stage)
        {
            label.RemoveFromClassList("in-danger");
            label.RemoveFromClassList("critical");

            if (stage >= 5)
            {
                label.AddToClassList("critical");
            }
            else if (stage >= 4)
            {
                label.AddToClassList("in-danger");
            }
        }

        #endregion

        #region Game Over Display

        /// <summary>
        /// Shows the overlay in game over state with winner/loser styling.
        /// Does NOT trigger blade drop or head fall animations - call those separately to sync with audio.
        /// The loser's blade starts at stage-4 position to allow for a visible "final raise" animation.
        /// </summary>
        public void ShowGameOver(GuillotineData playerData, GuillotineData opponentData, bool playerWon)
        {
            _playerData = playerData;
            _opponentData = opponentData;

            // Start the loser's blade at stage-4 position for visible "final raise" animation
            // Winner's blade shows at their actual stage
            int playerInitialStage = playerWon ? -1 : 4;
            int opponentInitialStage = playerWon ? 4 : -1;

            Show(playerData, opponentData, playerInitialStage, opponentInitialStage);

            // Update title
            if (_titleLabel != null)
            {
                _titleLabel.text = playerWon ? "VICTORY!" : "DEFEATED!";
            }

            // Apply winner/loser styling (but NOT the animations yet)
            if (playerWon)
            {
                _playerPanel?.AddToClassList("winner");
                _playerPanel?.RemoveFromClassList("loser");
                _opponentPanel?.AddToClassList("loser");
                _opponentPanel?.RemoveFromClassList("winner");
            }
            else
            {
                _playerPanel?.AddToClassList("loser");
                _playerPanel?.RemoveFromClassList("winner");
                _opponentPanel?.AddToClassList("winner");
                _opponentPanel?.RemoveFromClassList("loser");
            }

            // Update face expressions for game over
            if (playerWon)
            {
                if (_playerHeadFace != null) _playerHeadFace.text = FACE_HAPPY;
                if (_opponentHeadFace != null) _opponentHeadFace.text = FACE_HORROR;
            }
            else
            {
                if (_playerHeadFace != null) _playerHeadFace.text = FACE_HORROR;
                if (_opponentHeadFace != null) _opponentHeadFace.text = FACE_EVIL;
            }
        }

        /// <summary>
        /// Triggers the final raise animation for the loser's blade.
        /// This animates the blade from stage-4 to stage-5 (the highest position)
        /// before the dramatic pause and drop.
        /// Call this when the final raise audio plays.
        /// </summary>
        public void TriggerFinalRaise(bool playerLost)
        {
            if (playerLost)
            {
                UpdateBladePosition(_playerBladeGroup, 5);
                UpdateRopeLength(_playerRope, 5);
                UpdateLeverPosition(_playerLeverArm, 5);
            }
            else
            {
                UpdateBladePosition(_opponentBladeGroup, 5);
                UpdateRopeLength(_opponentRope, 5);
                UpdateLeverPosition(_opponentLeverArm, 5);
            }
        }

        /// <summary>
        /// Triggers the lever drop animation for the loser (latch release).
        /// Call this when the hook unlock audio plays.
        /// The lever swings down, visually releasing the blade.
        /// </summary>
        public void TriggerLeverDrop(bool playerLost)
        {
            if (playerLost)
            {
                _playerLeverArm?.AddToClassList("dropped");
            }
            else
            {
                _opponentLeverArm?.AddToClassList("dropped");
            }
        }

        /// <summary>
        /// Triggers the blade drop animation for the loser.
        /// Call this when the blade drop audio plays (after lever has dropped).
        /// </summary>
        public void TriggerBladeDrop(bool playerLost)
        {
            if (playerLost)
            {
                _playerBladeGroup?.AddToClassList("dropped");
                _playerRope?.AddToClassList("dropped");
            }
            else
            {
                _opponentBladeGroup?.AddToClassList("dropped");
                _opponentRope?.AddToClassList("dropped");
            }
        }

        /// <summary>
        /// Triggers the head fall animation for the loser.
        /// Call this when the head removed audio plays.
        /// </summary>
        public void TriggerHeadFall(bool playerLost)
        {
            if (playerLost)
            {
                _playerHead?.AddToClassList("in-basket");
            }
            else
            {
                _opponentHead?.AddToClassList("in-basket");
            }
        }

        /// <summary>
        /// Resets the game over styling for a new game.
        /// </summary>
        public void ResetGameOverState()
        {
            _playerPanel?.RemoveFromClassList("winner");
            _playerPanel?.RemoveFromClassList("loser");
            _opponentPanel?.RemoveFromClassList("winner");
            _opponentPanel?.RemoveFromClassList("loser");

            _playerBladeGroup?.RemoveFromClassList("dropped");
            _playerRope?.RemoveFromClassList("dropped");
            _playerHead?.RemoveFromClassList("in-basket");
            _opponentBladeGroup?.RemoveFromClassList("dropped");
            _opponentRope?.RemoveFromClassList("dropped");
            _opponentHead?.RemoveFromClassList("in-basket");

            // Reset lever dropped state
            _playerLeverArm?.RemoveFromClassList("dropped");
            _opponentLeverArm?.RemoveFromClassList("dropped");

            // Remove all stage classes from blade groups
            if (_playerBladeGroup != null)
            {
                for (int i = 1; i <= TOTAL_STAGES; i++)
                {
                    _playerBladeGroup.RemoveFromClassList($"stage-{i}");
                }
            }
            if (_opponentBladeGroup != null)
            {
                for (int i = 1; i <= TOTAL_STAGES; i++)
                {
                    _opponentBladeGroup.RemoveFromClassList($"stage-{i}");
                }
            }

            // Remove all stage classes from ropes
            if (_playerRope != null)
            {
                for (int i = 1; i <= TOTAL_STAGES; i++)
                {
                    _playerRope.RemoveFromClassList($"stage-{i}");
                }
            }
            if (_opponentRope != null)
            {
                for (int i = 1; i <= TOTAL_STAGES; i++)
                {
                    _opponentRope.RemoveFromClassList($"stage-{i}");
                }
            }

            // Remove all stage classes from lever arms
            if (_playerLeverArm != null)
            {
                for (int i = 1; i <= TOTAL_STAGES; i++)
                {
                    _playerLeverArm.RemoveFromClassList($"stage-{i}");
                }
            }
            if (_opponentLeverArm != null)
            {
                for (int i = 1; i <= TOTAL_STAGES; i++)
                {
                    _opponentLeverArm.RemoveFromClassList($"stage-{i}");
                }
            }

            if (_titleLabel != null)
            {
                _titleLabel.text = "Danger Status";
            }
        }

        #endregion

        #region Cleanup

        /// <summary>
        /// Cleans up event subscriptions.
        /// </summary>
        public void Dispose()
        {
            if (_closeButton != null)
            {
                _closeButton.clicked -= Hide;
            }

            _playerStageSegments.Clear();
            _opponentStageSegments.Clear();
        }

        #endregion
    }
}

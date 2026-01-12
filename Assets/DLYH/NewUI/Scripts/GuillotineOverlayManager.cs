using System;
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
    /// Triggered by tapping the miss counter buttons in the gameplay tabs.
    /// </summary>
    public class GuillotineOverlayManager
    {
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
        private VisualElement _playerHead;
        private Label _playerHeadFace;
        private Label _playerMissLabel;
        private VisualElement _playerDangerFill;
        private Label _playerFlavorText;
        private VisualElement _playerHashMarks;

        // Opponent guillotine elements
        private VisualElement _opponentPanel;
        private VisualElement _opponentColorBadge;
        private Label _opponentNameLabel;
        private VisualElement _opponentBladeGroup;
        private VisualElement _opponentHead;
        private Label _opponentHeadFace;
        private Label _opponentMissLabel;
        private VisualElement _opponentDangerFill;
        private Label _opponentFlavorText;
        private VisualElement _opponentHashMarks;

        // State
        private bool _isVisible;
        private GuillotineData _playerData;
        private GuillotineData _opponentData;

        // Constants for blade position (percentage of travel from top to bottom)
        private const float BLADE_TOP_PERCENT = 0f;
        private const float BLADE_BOTTOM_PERCENT = 75f;

        // Face expressions based on danger level
        private static readonly string FACE_HAPPY = ":-)";
        private static readonly string FACE_NEUTRAL = ":-|";
        private static readonly string FACE_WORRIED = ":-/";
        private static readonly string FACE_SCARED = ":-O";
        private static readonly string FACE_HORROR = "X-O";
        private static readonly string FACE_EVIL = ">:-)";

        // Flavor text arrays
        private static readonly string[] FLAVOR_SAFE = new string[]
        {
            "Safe for now...",
            "Breathing easy.",
            "No worries yet.",
            "Sitting pretty."
        };
        private static readonly string[] FLAVOR_WARM = new string[]
        {
            "Getting warm...",
            "Starting to sweat.",
            "The blade rises.",
            "Things are heating up."
        };
        private static readonly string[] FLAVOR_DANGER = new string[]
        {
            "In danger!",
            "Neck is exposed!",
            "One wrong move...",
            "Walking on thin ice!"
        };
        private static readonly string[] FLAVOR_CRITICAL = new string[]
        {
            "CRITICAL!",
            "Final moments!",
            "Say your prayers!",
            "It's almost over!"
        };

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

        /// <summary>
        /// Query and cache all UI elements.
        /// </summary>
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
            _playerHead = _overlayRoot.Q<VisualElement>("player-head");
            _playerHeadFace = _overlayRoot.Q<Label>("player-head-face");
            _playerMissLabel = _overlayRoot.Q<Label>("player-miss-label");
            _playerDangerFill = _overlayRoot.Q<VisualElement>("player-danger-fill");
            _playerFlavorText = _overlayRoot.Q<Label>("player-flavor-text");
            _playerHashMarks = _overlayRoot.Q<VisualElement>("player-hash-marks");

            // Opponent elements
            _opponentPanel = _overlayRoot.Q<VisualElement>("guillotine-opponent");
            _opponentColorBadge = _overlayRoot.Q<VisualElement>("opponent-color-badge");
            _opponentNameLabel = _overlayRoot.Q<Label>("opponent-guillotine-name");
            _opponentBladeGroup = _overlayRoot.Q<VisualElement>("opponent-blade-group");
            _opponentHead = _overlayRoot.Q<VisualElement>("opponent-head");
            _opponentHeadFace = _overlayRoot.Q<Label>("opponent-head-face");
            _opponentMissLabel = _overlayRoot.Q<Label>("opponent-miss-label");
            _opponentDangerFill = _overlayRoot.Q<VisualElement>("opponent-danger-fill");
            _opponentFlavorText = _overlayRoot.Q<Label>("opponent-flavor-text");
            _opponentHashMarks = _overlayRoot.Q<VisualElement>("opponent-hash-marks");
        }

        /// <summary>
        /// Wire up click events.
        /// </summary>
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
            _playerData = playerData;
            _opponentData = opponentData;

            UpdatePlayerDisplay();
            UpdateOpponentDisplay();

            _overlayRoot?.RemoveFromClassList("hidden");
            _isVisible = true;
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

        /// <summary>
        /// Updates the player guillotine display.
        /// </summary>
        private void UpdatePlayerDisplay()
        {
            if (_playerData == null) return;

            // Name and color
            if (_playerNameLabel != null)
            {
                _playerNameLabel.text = _playerData.Name ?? "You";
            }
            if (_playerColorBadge != null)
            {
                _playerColorBadge.style.backgroundColor = _playerData.Color;
            }
            if (_playerHead != null)
            {
                _playerHead.style.backgroundColor = _playerData.Color;
            }

            // Miss counter
            if (_playerMissLabel != null)
            {
                _playerMissLabel.text = $"Misses: {_playerData.MissCount} / {_playerData.MissLimit}";
            }

            // Danger bar
            float percent = GetDangerPercent(_playerData);
            if (_playerDangerFill != null)
            {
                _playerDangerFill.style.width = new StyleLength(new Length(percent, LengthUnit.Percent));
                UpdateDangerClass(_playerDangerFill, percent);
            }

            // Blade position
            UpdateBladePosition(_playerBladeGroup, percent);

            // Face expression
            if (_playerHeadFace != null)
            {
                _playerHeadFace.text = GetFaceExpression(percent, false);
            }

            // Flavor text
            if (_playerFlavorText != null)
            {
                _playerFlavorText.text = GetFlavorText(percent);
                UpdateFlavorTextClass(_playerFlavorText, percent);
            }

            // Generate hash marks
            GenerateHashMarks(_playerHashMarks, _playerData.MissLimit, _playerData.MissCount);
        }

        /// <summary>
        /// Updates the opponent guillotine display.
        /// </summary>
        private void UpdateOpponentDisplay()
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
            if (_opponentHead != null)
            {
                _opponentHead.style.backgroundColor = _opponentData.Color;
            }

            // Miss counter
            if (_opponentMissLabel != null)
            {
                _opponentMissLabel.text = $"Misses: {_opponentData.MissCount} / {_opponentData.MissLimit}";
            }

            // Danger bar
            float percent = GetDangerPercent(_opponentData);
            if (_opponentDangerFill != null)
            {
                _opponentDangerFill.style.width = new StyleLength(new Length(percent, LengthUnit.Percent));
                UpdateDangerClass(_opponentDangerFill, percent);
            }

            // Blade position
            UpdateBladePosition(_opponentBladeGroup, percent);

            // Face expression
            if (_opponentHeadFace != null)
            {
                _opponentHeadFace.text = GetFaceExpression(percent, true);
            }

            // Flavor text
            if (_opponentFlavorText != null)
            {
                _opponentFlavorText.text = GetFlavorText(percent);
                UpdateFlavorTextClass(_opponentFlavorText, percent);
            }

            // Generate hash marks
            GenerateHashMarks(_opponentHashMarks, _opponentData.MissLimit, _opponentData.MissCount);
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

        private void UpdateDangerClass(VisualElement fillBar, float percent)
        {
            fillBar.RemoveFromClassList("danger-low");
            fillBar.RemoveFromClassList("danger-medium");
            fillBar.RemoveFromClassList("danger-high");
            fillBar.RemoveFromClassList("danger-critical");

            if (percent >= 95)
            {
                fillBar.AddToClassList("danger-critical");
            }
            else if (percent >= 80)
            {
                fillBar.AddToClassList("danger-high");
            }
            else if (percent >= 50)
            {
                fillBar.AddToClassList("danger-medium");
            }
            else
            {
                fillBar.AddToClassList("danger-low");
            }
        }

        private void UpdateBladePosition(VisualElement bladeGroup, float dangerPercent)
        {
            if (bladeGroup == null) return;

            // Blade starts at top (0%) and moves down as danger increases
            // Map danger percent to blade position (0-75% of container height)
            float bladePercent = Mathf.Lerp(BLADE_TOP_PERCENT, BLADE_BOTTOM_PERCENT, dangerPercent / 100f);
            bladeGroup.style.top = new StyleLength(new Length(bladePercent, LengthUnit.Percent));
        }

        private string GetFaceExpression(float dangerPercent, bool isOpponent)
        {
            // For opponent, we're happy when they're in danger
            if (isOpponent)
            {
                if (dangerPercent >= 95) return FACE_EVIL;
                if (dangerPercent >= 80) return FACE_HAPPY;
                if (dangerPercent >= 50) return FACE_NEUTRAL;
                return FACE_WORRIED;
            }
            else
            {
                // For player, we're scared when in danger
                if (dangerPercent >= 95) return FACE_HORROR;
                if (dangerPercent >= 80) return FACE_SCARED;
                if (dangerPercent >= 50) return FACE_WORRIED;
                return FACE_NEUTRAL;
            }
        }

        private string GetFlavorText(float dangerPercent)
        {
            string[] pool;
            if (dangerPercent >= 95)
            {
                pool = FLAVOR_CRITICAL;
            }
            else if (dangerPercent >= 80)
            {
                pool = FLAVOR_DANGER;
            }
            else if (dangerPercent >= 50)
            {
                pool = FLAVOR_WARM;
            }
            else
            {
                pool = FLAVOR_SAFE;
            }

            return pool[UnityEngine.Random.Range(0, pool.Length)];
        }

        private void UpdateFlavorTextClass(Label label, float percent)
        {
            label.RemoveFromClassList("in-danger");
            label.RemoveFromClassList("critical");

            if (percent >= 80)
            {
                label.AddToClassList("critical");
            }
            else if (percent >= 50)
            {
                label.AddToClassList("in-danger");
            }
        }

        private void GenerateHashMarks(VisualElement container, int missLimit, int currentMisses)
        {
            if (container == null || missLimit <= 0) return;

            container.Clear();

            // Generate hash marks evenly distributed
            float containerHeight = 100f; // Percentage
            float spacing = containerHeight / missLimit;

            for (int i = 1; i <= missLimit; i++)
            {
                VisualElement mark = new VisualElement();
                mark.AddToClassList("hash-mark");

                // Position from bottom (i * spacing)
                float yPercent = i * spacing;
                mark.style.bottom = new StyleLength(new Length(yPercent, LengthUnit.Percent));

                // Highlight marks that have been "passed" by current miss count
                if (i <= currentMisses)
                {
                    mark.style.backgroundColor = new Color(0.5f, 0.3f, 0.2f);
                }

                container.Add(mark);
            }
        }

        #endregion

        #region Game Over Display

        /// <summary>
        /// Shows the overlay in game over state with winner/loser styling.
        /// </summary>
        public void ShowGameOver(GuillotineData playerData, GuillotineData opponentData, bool playerWon)
        {
            _playerData = playerData;
            _opponentData = opponentData;

            Show(playerData, opponentData);

            // Update title
            if (_titleLabel != null)
            {
                _titleLabel.text = playerWon ? "VICTORY!" : "DEFEATED!";
            }

            // Apply winner/loser styling
            if (playerWon)
            {
                _playerPanel?.AddToClassList("winner");
                _playerPanel?.RemoveFromClassList("loser");
                _opponentPanel?.AddToClassList("loser");
                _opponentPanel?.RemoveFromClassList("winner");

                // Opponent's head falls
                _opponentBladeGroup?.AddToClassList("dropped");
                _opponentHead?.AddToClassList("in-basket");
            }
            else
            {
                _playerPanel?.AddToClassList("loser");
                _playerPanel?.RemoveFromClassList("winner");
                _opponentPanel?.AddToClassList("winner");
                _opponentPanel?.RemoveFromClassList("loser");

                // Player's head falls
                _playerBladeGroup?.AddToClassList("dropped");
                _playerHead?.AddToClassList("in-basket");
            }

            // Update face expressions for game over
            if (playerWon)
            {
                if (_playerHeadFace != null) _playerHeadFace.text = FACE_EVIL;
                if (_opponentHeadFace != null) _opponentHeadFace.text = FACE_HORROR;
            }
            else
            {
                if (_playerHeadFace != null) _playerHeadFace.text = FACE_HORROR;
                if (_opponentHeadFace != null) _opponentHeadFace.text = FACE_EVIL;
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
            _playerHead?.RemoveFromClassList("in-basket");
            _opponentBladeGroup?.RemoveFromClassList("dropped");
            _opponentHead?.RemoveFromClassList("in-basket");

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
        }

        #endregion
    }
}

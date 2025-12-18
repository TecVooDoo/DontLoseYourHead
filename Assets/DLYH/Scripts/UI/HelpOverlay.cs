// HelpOverlay.cs
// Draggable help panel that displays gameplay instructions
// Can be toggled via a help button during gameplay
// Created: December 16, 2025

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

namespace DLYH.UI
{
    /// <summary>
    /// A draggable help overlay panel that displays gameplay instructions.
    /// Players can show/hide it and drag it around the screen.
    /// </summary>
    public class HelpOverlay : MonoBehaviour, IDragHandler, IBeginDragHandler
    {
        #region Serialized Fields

        [Header("Panel References")]
        [SerializeField] private GameObject _overlayPanel;
        [SerializeField] private RectTransform _panelRect;
        [SerializeField] private Button _closeButton;

        [Header("Toggle Button (in gameplay UI)")]
        [SerializeField] private Button _helpToggleButton;
        [SerializeField] private TMP_Text _helpToggleButtonText;

        [Header("Content")]
        [SerializeField] private TMP_Text _helpContentText;
        [SerializeField] private ScrollRect _scrollRect;

        [Header("Settings")]
        [SerializeField, Tooltip("Scroll wheel sensitivity (higher = faster scroll)")]
        [Range(1f, 50f)]
        private float _scrollSensitivity = 25f;

        [SerializeField] private bool _startHidden = true;

        #endregion

        #region Private Fields

        private RectTransform _canvasRect;
        private Vector2 _dragOffset;
        private bool _isVisible;

        // Track if help has been shown this session
        private static bool _hasShownThisSession = false;

        #endregion

        #region Help Content

        private const string HELP_CONTENT = @"<b>GAMEPLAY GUIDE</b>

<b>Your Goal:</b>
Discover all of your opponent's words AND their grid positions before they find yours!

<b>Grid Colors:</b>
<color=#4CAF50>GREEN</color> = Hit - Letter is known
<color=#FFC107>YELLOW</color> = Hit - Letter unknown (discovered by coordinate)
<color=#F44336>RED</color> = Miss - Empty cell

<b>Three Ways to Guess:</b>

<b>1. Letter Guess</b>
Click a letter in the Letter Tracker (A-Z buttons)
- If the opponent has that letter, it reveals in their words
- Yellow cells upgrade to green when letter is discovered
- Miss if opponent has no words with that letter

<b>2. Coordinate Guess</b>
Click a cell on the opponent's grid
- Green if letter is already known
- Yellow if you hit a letter but don't know which one
- Red if empty (counts as a miss)

<b>3. Word Guess</b>
Click ""Guess Word"" button on a word row
- Type the full word using keyboard or letter buttons
- Correct guess reveals the word!
- <color=#F44336>WRONG guess = 2 misses!</color>

<b>Extra Turns:</b>
<color=#4CAF50>Complete a word = EXTRA TURN!</color>
- When your guess completes a word (all letters revealed), you get another turn
- This works for letter guesses, coordinate guesses, AND correct word guesses
- Multiple words completed at once = multiple extra turns (one per word)

<b>Win Conditions:</b>
- Reveal ALL opponent's letters AND grid positions
- OR opponent reaches their miss limit

<b>Tips:</b>
- Use letter guesses to discover common letters (E, T, A, O)
- Coordinate guesses near hits often find more letters
- Completing words gives extra turns - be strategic!
- Only guess words when you're confident!

<i>Drag this panel to move it. Click X or ? to close.</i>";

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            // Find canvas for bounds checking
            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas != null)
            {
                _canvasRect = canvas.GetComponent<RectTransform>();
            }

            // Ensure panel rect is set
            if (_panelRect == null && _overlayPanel != null)
            {
                _panelRect = _overlayPanel.GetComponent<RectTransform>();
            }
        }

        private void Start()
        {
            WireEvents();
            ConfigureScrollSpeed();
            SetHelpContent();

            // First session visibility is handled in OnEnable via coroutine
            // For subsequent games, respect _startHidden setting
            if (_hasShownThisSession && _startHidden)
            {
                Hide();
            }
        }

        private void OnDestroy()
        {
            UnwireEvents();
        }

        #endregion

        #region Initialization

        private void WireEvents()
        {
            if (_closeButton != null)
            {
                _closeButton.onClick.AddListener(OnCloseClicked);
            }

            if (_helpToggleButton != null)
            {
                _helpToggleButton.onClick.AddListener(OnToggleClicked);
            }
        }

        private void UnwireEvents()
        {
            if (_closeButton != null)
            {
                _closeButton.onClick.RemoveListener(OnCloseClicked);
            }

            if (_helpToggleButton != null)
            {
                _helpToggleButton.onClick.RemoveListener(OnToggleClicked);
            }
        }

        private void ConfigureScrollSpeed()
        {
            if (_scrollRect != null)
            {
                _scrollRect.scrollSensitivity = _scrollSensitivity;
            }
        }

        private void SetHelpContent()
        {
            if (_helpContentText != null)
            {
                _helpContentText.text = HELP_CONTENT;
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Show the help overlay
        /// </summary>
        public void Show()
        {
            if (_overlayPanel != null)
            {
                _overlayPanel.SetActive(true);
            }
            _isVisible = true;
            UpdateToggleButtonText();
            ResetScrollPosition();
        }

        /// <summary>
        /// Reset scroll position to top
        /// </summary>
        private void ResetScrollPosition()
        {
            if (_scrollRect != null)
            {
                // Reset to top (1 = top, 0 = bottom for vertical scroll)
                _scrollRect.verticalNormalizedPosition = 1f;
            }
        }

        /// <summary>
        /// Hide the help overlay
        /// </summary>
        public void Hide()
        {
            if (_overlayPanel != null)
            {
                _overlayPanel.SetActive(false);
            }
            _isVisible = false;
            UpdateToggleButtonText();
        }

        /// <summary>
        /// Toggle visibility
        /// </summary>
        public void Toggle()
        {
            if (_isVisible)
            {
                Hide();
            }
            else
            {
                Show();
            }
        }

        /// <summary>
        /// Check if overlay is currently visible
        /// </summary>
        public bool IsVisible => _isVisible;

        #endregion

        #region Event Handlers

        private void OnCloseClicked()
        {
            Hide();
            DLYH.Audio.UIAudioManager.ButtonClick();
        }

        private void OnToggleClicked()
        {
            Toggle();
            DLYH.Audio.UIAudioManager.ButtonClick();
        }

        private void UpdateToggleButtonText()
        {
            if (_helpToggleButtonText != null)
            {
                _helpToggleButtonText.text = "?";
            }
        }

        #endregion

        #region Drag Handling

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (_panelRect == null) return;

            // Calculate offset from panel center to mouse position
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _canvasRect,
                eventData.position,
                eventData.pressEventCamera,
                out Vector2 localPoint);

            _dragOffset = (Vector2)_panelRect.localPosition - localPoint;
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (_panelRect == null || _canvasRect == null) return;

            // Convert screen position to local canvas position
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _canvasRect,
                eventData.position,
                eventData.pressEventCamera,
                out Vector2 localPoint);

            // Apply offset to get new position
            Vector2 newPosition = localPoint + _dragOffset;

            // Clamp to canvas bounds
            newPosition = ClampToCanvas(newPosition);

            _panelRect.localPosition = newPosition;
        }

        private Vector2 ClampToCanvas(Vector2 position)
        {
            if (_canvasRect == null || _panelRect == null) return position;

            Vector2 canvasSize = _canvasRect.rect.size;
            Vector2 panelSize = _panelRect.rect.size;

            // Calculate bounds (assuming anchored at center)
            float minX = -canvasSize.x / 2 + panelSize.x / 2;
            float maxX = canvasSize.x / 2 - panelSize.x / 2;
            float minY = -canvasSize.y / 2 + panelSize.y / 2;
            float maxY = canvasSize.y / 2 - panelSize.y / 2;

            position.x = Mathf.Clamp(position.x, minX, maxX);
            position.y = Mathf.Clamp(position.y, minY, maxY);

            return position;
        }

        #endregion

        #region Static Access

        private static HelpOverlay _instance;

        /// <summary>
        /// Get the singleton instance (if exists)
        /// </summary>
        public static HelpOverlay Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindFirstObjectByType<HelpOverlay>();
                }
                return _instance;
            }
        }

        private void OnEnable()
        {
            _instance = this;

            // Show help on first game of session when panel becomes active
            if (!_hasShownThisSession)
            {
                // Delay slightly to ensure layout is ready
                StartCoroutine(ShowOnFirstSession());
            }
        }

        private System.Collections.IEnumerator ShowOnFirstSession()
        {
            yield return null; // Wait one frame for layout and Start() to run
            yield return null; // Extra frame to ensure content is set
            if (!_hasShownThisSession)
            {
                SetHelpContent(); // Ensure content is set before showing
                Show();
                _hasShownThisSession = true;
            }
        }

        /// <summary>
        /// Static method to show help overlay
        /// </summary>
        public static void ShowHelp()
        {
            if (Instance != null)
            {
                Instance.Show();
            }
        }

        /// <summary>
        /// Static method to hide help overlay
        /// </summary>
        public static void HideHelp()
        {
            if (Instance != null)
            {
                Instance.Hide();
            }
        }

        /// <summary>
        /// Static method to toggle help overlay
        /// </summary>
        public static void ToggleHelp()
        {
            if (Instance != null)
            {
                Instance.Toggle();
            }
        }

        #endregion
    }
}

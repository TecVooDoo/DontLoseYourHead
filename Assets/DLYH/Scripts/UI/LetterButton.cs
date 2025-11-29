using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using Sirenix.OdinInspector;
using System;

namespace TecVooDoo.DontLoseYourHead.UI
{
    /// <summary>
    /// Represents a single letter button in the Letter Tracker.
    /// Serves dual purposes:
    /// - Setup Mode: Acts as keyboard input for typing words
    /// - Gameplay Mode: Displays guessed letter status (hit/miss/unguessed)
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class LetterButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        #region Enums
        /// <summary>
        /// Visual state of the letter button
        /// </summary>
        public enum LetterState
        {
            /// <summary>Default state - letter has not been guessed</summary>
            Normal,
            /// <summary>Letter was guessed and found in opponent's words</summary>
            Hit,
            /// <summary>Letter was guessed but not in opponent's words</summary>
            Miss,
            /// <summary>Letter is currently selected/highlighted</summary>
            Selected,
            /// <summary>Letter is disabled and cannot be clicked</summary>
            Disabled
        }
        #endregion

        #region Serialized Fields
        [TitleGroup("References")]
        [SerializeField, Required]
        private Button _button;

        [SerializeField, Required]
        private TextMeshProUGUI _letterText;

        [SerializeField]
        private Image _backgroundImage;

        [TitleGroup("Colors")]
        [SerializeField]
        private Color _normalColor = new Color(0.9f, 0.9f, 0.9f, 1f);

        [SerializeField]
        private Color _hitColor = new Color(0.4f, 0.8f, 0.4f, 1f);

        [SerializeField]
        private Color _missColor = new Color(0.8f, 0.4f, 0.4f, 1f);

        [SerializeField]
        private Color _selectedColor = new Color(0.4f, 0.6f, 0.9f, 1f);

        [SerializeField]
        private Color _disabledColor = new Color(0.5f, 0.5f, 0.5f, 0.5f);

        [SerializeField]
        private Color _hoverColor = new Color(0.85f, 0.85f, 0.95f, 1f);

        [TitleGroup("Text Colors")]
        [SerializeField]
        private Color _normalTextColor = Color.black;

        [SerializeField]
        private Color _hitTextColor = Color.white;

        [SerializeField]
        private Color _missTextColor = Color.white;

        [SerializeField]
        private Color _disabledTextColor = new Color(0.3f, 0.3f, 0.3f, 1f);
        #endregion

        #region Private Fields
        private char _letter;
        private LetterState _currentState = LetterState.Normal;
        private bool _isHovered;
        private bool _isInteractable = true;
        private bool _isInitialized;
        #endregion

        #region Events
        /// <summary>
        /// Fired when the letter button is clicked. Parameter: the letter character
        /// </summary>
        public event Action<char> OnLetterClicked;

        /// <summary>
        /// Fired when mouse enters the button. Parameter: the letter character
        /// </summary>
        public event Action<char> OnLetterHoverEnter;

        /// <summary>
        /// Fired when mouse exits the button. Parameter: the letter character
        /// </summary>
        public event Action<char> OnLetterHoverExit;
        #endregion

        #region Properties
        /// <summary>
        /// The letter this button represents (A-Z)
        /// </summary>
        public char Letter
        {
            get
            {
                // Auto-detect if not initialized
                if (!_isInitialized)
                {
                    EnsureInitialized();
                }
                return _letter;
            }
        }

        /// <summary>
        /// Current visual state of the button
        /// </summary>
        public LetterState CurrentState => _currentState;

        /// <summary>
        /// Whether the button has been initialized with a letter
        /// </summary>
        public bool IsInitialized => _isInitialized;

        /// <summary>
        /// Whether the button can be clicked
        /// </summary>
        public bool IsInteractable
        {
            get => _isInteractable;
            set
            {
                _isInteractable = value;
                if (_button != null)
                {
                    _button.interactable = value;
                }
                if (!value)
                {
                    SetState(LetterState.Disabled);
                }
            }
        }
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            CacheReferences();
            EnsureInitialized();
        }

        private void OnEnable()
        {
            if (_button != null)
            {
                _button.onClick.AddListener(HandleClick);
            }
        }

        private void OnDisable()
        {
            if (_button != null)
            {
                _button.onClick.RemoveListener(HandleClick);
            }
        }

        private void Reset()
        {
            CacheReferences();
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Initialize the letter button with its letter character.
        /// </summary>
        /// <param name="letter">The letter (A-Z) this button represents</param>
        public void Initialize(char letter)
        {
            _letter = char.ToUpper(letter);
            _isInitialized = true;

            if (_letterText != null)
            {
                _letterText.text = _letter.ToString();
            }

            SetState(LetterState.Normal);
        }

        /// <summary>
        /// Ensures the button is initialized by auto-detecting the letter if needed.
        /// Can be called from editor scripts or during caching.
        /// </summary>
        public void EnsureInitialized()
        {
            if (_isInitialized) return;

            CacheReferences();
            AutoDetectLetter();
        }

        /// <summary>
        /// Sets the visual state of the letter button.
        /// </summary>
        /// <param name="state">The new state</param>
        public void SetState(LetterState state)
        {
            _currentState = state;
            UpdateVisuals();
        }

        /// <summary>
        /// Resets the button to Normal state.
        /// </summary>
        public void ResetState()
        {
            _isInteractable = true;
            if (_button != null)
            {
                _button.interactable = true;
            }
            SetState(LetterState.Normal);
        }

        /// <summary>
        /// Marks this letter as a hit (found in opponent's words).
        /// </summary>
        public void MarkAsHit()
        {
            SetState(LetterState.Hit);
        }

        /// <summary>
        /// Marks this letter as a miss (not in opponent's words).
        /// </summary>
        public void MarkAsMiss()
        {
            SetState(LetterState.Miss);
        }

        /// <summary>
        /// Selects this letter (highlights it).
        /// </summary>
        public void Select()
        {
            SetState(LetterState.Selected);
        }

        /// <summary>
        /// Deselects this letter (returns to normal if not hit/miss).
        /// </summary>
        public void Deselect()
        {
            if (_currentState == LetterState.Selected)
            {
                SetState(LetterState.Normal);
            }
        }

        /// <summary>
        /// Sets the hit color (used to match opponent's player color).
        /// </summary>
        /// <param name="color">The color to use for hit state</param>
        public void SetHitColor(Color color)
        {
            _hitColor = color;
            if (_currentState == LetterState.Hit)
            {
                UpdateVisuals();
            }
        }
        #endregion

        #region IPointerEnterHandler / IPointerExitHandler
        public void OnPointerEnter(PointerEventData eventData)
        {
            if (!_isInteractable) return;

            _isHovered = true;
            UpdateVisuals();
            OnLetterHoverEnter?.Invoke(_letter);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            _isHovered = false;
            UpdateVisuals();
            OnLetterHoverExit?.Invoke(_letter);
        }
        #endregion

        #region Private Methods
        private void CacheReferences()
        {
            if (_button == null)
            {
                _button = GetComponent<Button>();
            }

            if (_letterText == null)
            {
                _letterText = GetComponentInChildren<TextMeshProUGUI>();
            }

            if (_backgroundImage == null)
            {
                _backgroundImage = GetComponent<Image>();
            }
        }

        /// <summary>
        /// Automatically detects the letter from the TextMeshProUGUI content.
        /// Called during Awake to initialize the letter from existing UI text.
        /// </summary>
        private void AutoDetectLetter()
        {
            if (_isInitialized) return;

            // First try: Get from TextMeshProUGUI
            if (_letterText != null && !string.IsNullOrEmpty(_letterText.text))
            {
                string text = _letterText.text.Trim().ToUpper();
                if (text.Length > 0 && char.IsLetter(text[0]))
                {
                    _letter = text[0];
                    _isInitialized = true;
                    return;
                }
            }

            // Second try: Get from GameObject name (e.g., "Letter_A" -> 'A')
            string goName = gameObject.name;
            if (goName.Contains("_") && goName.Length > 0)
            {
                int underscoreIndex = goName.LastIndexOf('_');
                if (underscoreIndex >= 0 && underscoreIndex < goName.Length - 1)
                {
                    char potentialLetter = char.ToUpper(goName[underscoreIndex + 1]);
                    if (char.IsLetter(potentialLetter))
                    {
                        _letter = potentialLetter;
                        _isInitialized = true;
                        return;
                    }
                }
            }

            // Third try: Last character of name if it's a letter
            if (goName.Length > 0)
            {
                char lastChar = char.ToUpper(goName[goName.Length - 1]);
                if (char.IsLetter(lastChar))
                {
                    _letter = lastChar;
                    _isInitialized = true;
                }
            }
        }

        private void HandleClick()
        {
            if (!_isInteractable) return;

            OnLetterClicked?.Invoke(_letter);
        }

        private void UpdateVisuals()
        {
            if (_backgroundImage == null) return;

            Color bgColor;
            Color textColor = _normalTextColor;

            switch (_currentState)
            {
                case LetterState.Hit:
                    bgColor = _hitColor;
                    textColor = _hitTextColor;
                    break;

                case LetterState.Miss:
                    bgColor = _missColor;
                    textColor = _missTextColor;
                    break;

                case LetterState.Selected:
                    bgColor = _selectedColor;
                    textColor = _normalTextColor;
                    break;

                case LetterState.Disabled:
                    bgColor = _disabledColor;
                    textColor = _disabledTextColor;
                    break;

                case LetterState.Normal:
                default:
                    bgColor = _isHovered ? _hoverColor : _normalColor;
                    textColor = _normalTextColor;
                    break;
            }

            _backgroundImage.color = bgColor;

            if (_letterText != null)
            {
                _letterText.color = textColor;
            }
        }
        #endregion

        #region Editor Helpers
#if UNITY_EDITOR
        [TitleGroup("Debug")]
        [Button("Test Hit State")]
        private void TestHitState()
        {
            SetState(LetterState.Hit);
        }

        [Button("Test Miss State")]
        private void TestMissState()
        {
            SetState(LetterState.Miss);
        }

        [Button("Test Selected State")]
        private void TestSelectedState()
        {
            SetState(LetterState.Selected);
        }

        [Button("Reset to Normal")]
        private void TestResetState()
        {
            ResetState();
        }

        [Button("Auto Detect Letter")]
        private void TestAutoDetect()
        {
            _isInitialized = false;
            CacheReferences();
            AutoDetectLetter();
            Debug.Log($"[LetterButton] Detected letter: '{_letter}' (initialized: {_isInitialized})");
        }
#endif
        #endregion
    }
}

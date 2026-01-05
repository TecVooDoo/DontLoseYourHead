// LetterCellUI.cs
// Unified cell component for word pattern rows and letter tracker
// Supports letters, icons, and action buttons with animation-ready structure
// Created: January 4, 2026
// Developer: TecVooDoo LLC

using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using DG.Tweening;

namespace TecVooDoo.DontLoseYourHead.UI
{
    /// <summary>
    /// Content type for the cell display.
    /// </summary>
    public enum CellContentType
    {
        Letter,     // Shows letter character (A-Z) or underscore
        Icon,       // Shows sprite image (for action buttons)
        Empty       // Invisible placeholder (maintains layout spacing)
    }

    /// <summary>
    /// Visual state for the cell.
    /// </summary>
    public enum LetterCellState
    {
        Default,        // Normal state
        Selected,       // Currently selected/active
        Revealed,       // Letter has been revealed (gameplay)
        Locked,         // Cannot be modified
        Disabled,       // Grayed out, non-interactive
        Highlighted     // Temporary highlight (hover, etc.)
    }

    /// <summary>
    /// Unified cell component for word pattern rows and letter tracker.
    ///
    /// Structure:
    /// - RectTransform (animatable: scale, rotation, position)
    /// - Image _background (color animatable)
    /// - TextMeshProUGUI _letterText (for letter display)
    /// - Image _iconImage (for action button icons)
    /// - Button _button (optional, for interactive cells)
    /// - CanvasGroup (for alpha fading)
    ///
    /// Supports:
    /// - Letter display (A-Z, underscore for unknown)
    /// - Icon display (action buttons: select, place, delete, guess)
    /// - Animation via DOTween (spin, scale, color, shake)
    /// - Player color theming
    /// </summary>
    public class LetterCellUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
    {
        // ============================================================
        // SERIALIZED FIELDS
        // ============================================================

        [Header("Core Components")]
        [SerializeField] private Image _background;
        [SerializeField] private TextMeshProUGUI _letterText;
        [SerializeField] private Image _iconImage;
        [SerializeField] private Button _button;
        [SerializeField] private CanvasGroup _canvasGroup;

        [Header("Default Colors")]
        [SerializeField] private Color _defaultBackgroundColor = new Color(0.9f, 0.9f, 0.9f, 1f);
        [SerializeField] private Color _selectedColor = new Color(1f, 0.95f, 0.6f, 1f);
        [SerializeField] private Color _revealedColor = new Color(0.7f, 0.9f, 0.7f, 1f);
        [SerializeField] private Color _disabledColor = new Color(0.6f, 0.6f, 0.6f, 0.5f);
        [SerializeField] private Color _highlightColor = new Color(0.8f, 0.9f, 1f, 1f);

        [Header("Text Colors")]
        [SerializeField] private Color _letterColor = Color.black;
        [SerializeField] private Color _underscoreColor = new Color(0.4f, 0.4f, 0.4f, 1f);

        [Header("Animation Settings")]
        [SerializeField] private float _defaultAnimDuration = 0.2f;
        [SerializeField] private Ease _defaultEase = Ease.OutQuad;

        // ============================================================
        // EVENTS
        // ============================================================

        /// <summary>Fired when cell is clicked. Parameter: cell index.</summary>
        public event Action<int> OnCellClicked;

        /// <summary>Fired when pointer enters cell. Parameter: cell index.</summary>
        public event Action<int> OnCellHoverEnter;

        /// <summary>Fired when pointer exits cell. Parameter: cell index.</summary>
        public event Action<int> OnCellHoverExit;

        // ============================================================
        // PRIVATE STATE
        // ============================================================

        private int _cellIndex;
        private CellContentType _contentType = CellContentType.Letter;
        private LetterCellState _state = LetterCellState.Default;
        private char _currentLetter = '\0';
        private bool _isInteractive = false;
        private Color _playerColor = Color.white;
        private bool _usePlayerColorForRevealed = false;

        // Animation state
        private Tweener _currentTween;
        private Vector3 _originalScale;
        private Quaternion _originalRotation;

        // ============================================================
        // PROPERTIES
        // ============================================================

        public int CellIndex => _cellIndex;
        public CellContentType ContentType => _contentType;
        public LetterCellState State => _state;
        public char CurrentLetter => _currentLetter;
        public bool IsInteractive => _isInteractive;
        public bool HasLetter => _currentLetter != '\0';

        // ============================================================
        // INITIALIZATION
        // ============================================================

        /// <summary>
        /// Initializes the cell with an index and content type.
        /// </summary>
        /// <param name="index">Cell index in the row (0-based)</param>
        /// <param name="contentType">Type of content to display</param>
        /// <param name="isInteractive">Whether cell responds to clicks</param>
        public void Initialize(int index, CellContentType contentType = CellContentType.Letter, bool isInteractive = false)
        {
            _cellIndex = index;
            _contentType = contentType;
            _isInteractive = isInteractive;

            // Store original transform values for reset
            _originalScale = transform.localScale;
            _originalRotation = transform.localRotation;

            // Configure button interactivity
            if (_button != null)
            {
                _button.interactable = isInteractive;
            }

            // Set initial visibility based on content type
            UpdateContentVisibility();
            SetState(LetterCellState.Default);
        }

        /// <summary>
        /// Resets the cell to empty state.
        /// </summary>
        public void Reset()
        {
            KillCurrentAnimation();

            _currentLetter = '\0';
            _state = LetterCellState.Default;

            // Reset transform
            transform.localScale = _originalScale;
            transform.localRotation = _originalRotation;

            // Reset visuals
            if (_letterText != null)
            {
                _letterText.text = "";
            }

            if (_iconImage != null)
            {
                _iconImage.sprite = null;
            }

            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = 1f;
            }

            UpdateVisuals();
        }

        // ============================================================
        // CONTENT MANAGEMENT
        // ============================================================

        /// <summary>
        /// Sets the letter to display. Use '\0' or '_' for underscore.
        /// </summary>
        public void SetLetter(char letter)
        {
            _currentLetter = char.ToUpper(letter);

            if (_letterText != null)
            {
                if (_currentLetter == '\0' || _currentLetter == '_')
                {
                    _letterText.text = "_";
                    _letterText.color = _underscoreColor;
                }
                else
                {
                    _letterText.text = _currentLetter.ToString();
                    _letterText.color = _letterColor;
                }
            }
        }

        /// <summary>
        /// Clears the letter display (shows empty or underscore based on context).
        /// </summary>
        public void ClearLetter()
        {
            _currentLetter = '\0';

            if (_letterText != null)
            {
                _letterText.text = "";
            }
        }

        /// <summary>
        /// Shows an underscore placeholder.
        /// </summary>
        public void ShowUnderscore()
        {
            _currentLetter = '_';

            if (_letterText != null)
            {
                _letterText.text = "_";
                _letterText.color = _underscoreColor;
            }
        }

        /// <summary>
        /// Sets the icon sprite for action button mode.
        /// </summary>
        public void SetIcon(Sprite icon)
        {
            if (_iconImage != null)
            {
                _iconImage.sprite = icon;
                _iconImage.enabled = icon != null;
            }
        }

        /// <summary>
        /// Sets the content type and updates visibility.
        /// </summary>
        public void SetContentType(CellContentType contentType)
        {
            _contentType = contentType;
            UpdateContentVisibility();
        }

        private void UpdateContentVisibility()
        {
            switch (_contentType)
            {
                case CellContentType.Letter:
                    if (_letterText != null) _letterText.enabled = true;
                    if (_iconImage != null) _iconImage.enabled = false;
                    if (_background != null) _background.enabled = true;
                    break;

                case CellContentType.Icon:
                    if (_letterText != null) _letterText.enabled = false;
                    if (_iconImage != null) _iconImage.enabled = true;
                    if (_background != null) _background.enabled = true;
                    break;

                case CellContentType.Empty:
                    if (_letterText != null) _letterText.enabled = false;
                    if (_iconImage != null) _iconImage.enabled = false;
                    if (_background != null) _background.enabled = false;
                    break;
            }
        }

        // ============================================================
        // STATE MANAGEMENT
        // ============================================================

        /// <summary>
        /// Sets the visual state of the cell.
        /// </summary>
        public void SetState(LetterCellState state)
        {
            _state = state;
            UpdateVisuals();
        }

        /// <summary>
        /// Sets the player color used for revealed state.
        /// </summary>
        public void SetPlayerColor(Color color)
        {
            _playerColor = color;
            _usePlayerColorForRevealed = true;

            // Update visuals if currently in revealed state
            if (_state == LetterCellState.Revealed)
            {
                UpdateVisuals();
            }
        }

        /// <summary>
        /// Sets whether this cell is interactive (clickable).
        /// </summary>
        public void SetInteractive(bool interactive)
        {
            _isInteractive = interactive;

            if (_button != null)
            {
                _button.interactable = interactive;
            }
        }

        /// <summary>
        /// Hides the cell (sets alpha to 0 or disables).
        /// </summary>
        public void Hide()
        {
            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = 0f;
                _canvasGroup.blocksRaycasts = false;
            }
            else
            {
                gameObject.SetActive(false);
            }
        }

        /// <summary>
        /// Shows the cell.
        /// </summary>
        public void Show()
        {
            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = 1f;
                _canvasGroup.blocksRaycasts = _isInteractive;
            }
            else
            {
                gameObject.SetActive(true);
            }
        }

        private void UpdateVisuals()
        {
            if (_background == null) return;

            Color targetColor = _state switch
            {
                LetterCellState.Default => _defaultBackgroundColor,
                LetterCellState.Selected => _selectedColor,
                LetterCellState.Revealed => _usePlayerColorForRevealed ? _playerColor : _revealedColor,
                LetterCellState.Locked => _revealedColor,
                LetterCellState.Disabled => _disabledColor,
                LetterCellState.Highlighted => _highlightColor,
                _ => _defaultBackgroundColor
            };

            _background.color = targetColor;
        }

        // ============================================================
        // ANIMATIONS
        // ============================================================

        /// <summary>
        /// Plays a spin animation (full 360 rotation).
        /// </summary>
        public Tweener AnimateSpin(float duration = -1f, int rotations = 1)
        {
            KillCurrentAnimation();

            float dur = duration > 0 ? duration : _defaultAnimDuration;
            _currentTween = transform
                .DORotate(new Vector3(0, 0, 360f * rotations), dur, RotateMode.FastBeyond360)
                .SetEase(_defaultEase)
                .OnComplete(() => transform.localRotation = _originalRotation);

            return _currentTween;
        }

        /// <summary>
        /// Plays a scale punch animation (pop effect).
        /// </summary>
        public Tweener AnimatePunch(float scale = 1.2f, float duration = -1f)
        {
            KillCurrentAnimation();

            float dur = duration > 0 ? duration : _defaultAnimDuration;
            _currentTween = transform
                .DOPunchScale(Vector3.one * (scale - 1f), dur, 1, 0.5f);

            return _currentTween;
        }

        /// <summary>
        /// Plays a scale animation to target scale.
        /// </summary>
        public Tweener AnimateScale(float targetScale, float duration = -1f)
        {
            KillCurrentAnimation();

            float dur = duration > 0 ? duration : _defaultAnimDuration;
            _currentTween = transform
                .DOScale(targetScale, dur)
                .SetEase(_defaultEase);

            return _currentTween;
        }

        /// <summary>
        /// Plays a shake animation.
        /// </summary>
        public Tweener AnimateShake(float duration = -1f, float strength = 5f)
        {
            KillCurrentAnimation();

            float dur = duration > 0 ? duration : _defaultAnimDuration;
            _currentTween = transform
                .DOShakePosition(dur, strength, 10, 90, false, true);

            return _currentTween;
        }

        /// <summary>
        /// Plays a color transition animation on the background.
        /// </summary>
        public Tweener AnimateColor(Color targetColor, float duration = -1f)
        {
            if (_background == null) return null;

            float dur = duration > 0 ? duration : _defaultAnimDuration;
            return _background.DOColor(targetColor, dur).SetEase(_defaultEase);
        }

        /// <summary>
        /// Plays a fade animation.
        /// </summary>
        public Tweener AnimateFade(float targetAlpha, float duration = -1f)
        {
            if (_canvasGroup == null) return null;

            float dur = duration > 0 ? duration : _defaultAnimDuration;
            return _canvasGroup.DOFade(targetAlpha, dur).SetEase(_defaultEase);
        }

        /// <summary>
        /// Plays a reveal animation (scale up from 0 with spin).
        /// </summary>
        public Sequence AnimateReveal(float duration = -1f)
        {
            KillCurrentAnimation();

            float dur = duration > 0 ? duration : _defaultAnimDuration * 2f;

            transform.localScale = Vector3.zero;

            var seq = DOTween.Sequence();
            seq.Append(transform.DOScale(_originalScale, dur).SetEase(Ease.OutBack));
            seq.Join(transform.DORotate(new Vector3(0, 0, 360), dur, RotateMode.FastBeyond360));

            return seq;
        }

        /// <summary>
        /// Resets the cell to original transform state.
        /// </summary>
        public void ResetTransform()
        {
            KillCurrentAnimation();
            transform.localScale = _originalScale;
            transform.localRotation = _originalRotation;
        }

        private void KillCurrentAnimation()
        {
            if (_currentTween != null && _currentTween.IsActive())
            {
                _currentTween.Kill();
                _currentTween = null;
            }

            DOTween.Kill(transform);
        }

        // ============================================================
        // EVENT HANDLERS
        // ============================================================

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (_isInteractive && _state != LetterCellState.Disabled)
            {
                OnCellHoverEnter?.Invoke(_cellIndex);
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (_isInteractive)
            {
                OnCellHoverExit?.Invoke(_cellIndex);
            }
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (_isInteractive && _state != LetterCellState.Disabled)
            {
                // Play click sound
                DLYH.Audio.UIAudioManager.KeyboardClick();

                OnCellClicked?.Invoke(_cellIndex);
            }
        }

        // ============================================================
        // CLEANUP
        // ============================================================

        private void OnDestroy()
        {
            KillCurrentAnimation();
        }
    }
}

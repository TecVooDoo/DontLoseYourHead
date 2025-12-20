// MessagePopup.cs
// Generic message popup that displays center screen and fades out
// Created: December 14, 2025

using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using Sirenix.OdinInspector;
using UnityEngine.EventSystems;
using System;

namespace TecVooDoo.DontLoseYourHead.UI
{
    /// <summary>
    /// A generic message popup component that displays messages in the center
    /// of the screen and automatically fades out after a configurable duration.
    ///
    /// Usage:
    /// - Call ShowMessage("Your message here") from any script
    /// - The popup will appear, display for the configured duration, then fade out
    ///
    /// Messages can include:
    /// - Invalid word notifications
    /// - Turn change notifications (e.g., "Player 2's Turn")
    /// - Already guessed notifications (letter, coordinate, or word)
    /// - Any other transient feedback
    /// </summary>
    public class MessagePopup : MonoBehaviour, IBeginDragHandler, IDragHandler
    {
        #region Singleton

        private static MessagePopup _instance;
        public static MessagePopup Instance
        {
            get
            {
                if (_instance == null)
                {
                    // FindFirstObjectByType with includeInactive to find even if GameObject is disabled
                    _instance = FindFirstObjectByType<MessagePopup>(FindObjectsInactive.Include);
                }
                return _instance;
            }
        }

        #endregion

        #region Serialized Fields

        [Header("UI References")]
        [SerializeField, Required]
        private GameObject _popupContainer;

        [SerializeField, Required]
        private TextMeshProUGUI _messageText;

        [Header("Timing Settings")]
        [SerializeField, Tooltip("How long the message displays before fading (in seconds)")]
        [Range(0.5f, 10f)]
        private float _displayDuration = 2f;

        [SerializeField, Tooltip("How long the fade out animation takes (in seconds)")]
        [Range(0.1f, 2f)]
        private float _fadeOutDuration = 0.3f;

        [Header("Game Over Settings")]
        [SerializeField, Tooltip("Continue button shown during game over messages")]
        private Button _continueButton;

        #endregion

        #region Events

        /// <summary>
        /// Fired when the Continue button is clicked on a game over message.
        /// </summary>
        public event Action OnContinueClicked;

        #endregion

        #region Private Fields

        private Coroutine _currentPopupCoroutine;
        private CanvasGroup _canvasGroup;
        private RectTransform _popupRect;
        private RectTransform _canvasRect;
        private Vector2 _dragOffset;
        private bool _isGameOverMode;
        private Vector2 _lastDraggedPosition;
        private bool _hasBeenDragged;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            // Set up singleton
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;

            // Get or add CanvasGroup for fading
            _canvasGroup = _popupContainer?.GetComponent<CanvasGroup>();
            if (_canvasGroup == null && _popupContainer != null)
            {
                _canvasGroup = _popupContainer.AddComponent<CanvasGroup>();
            }

            // Get RectTransform for dragging
            _popupRect = _popupContainer?.GetComponent<RectTransform>();

            // Find canvas for bounds
            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas != null)
            {
                _canvasRect = canvas.GetComponent<RectTransform>();
            }

            // Wire up continue button
            if (_continueButton != null)
            {
                _continueButton.onClick.AddListener(HandleContinueClicked);
                _continueButton.gameObject.SetActive(false);
            }

            // Ensure popup starts hidden
            HideImmediate();
        }

        private void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Shows a message popup that will automatically hide after the configured duration.
        /// If a message is already showing, it will be replaced with the new one.
        /// </summary>
        /// <param name="message">The message to display</param>
        public void ShowMessage(string message)
        {
            ShowMessage(message, _displayDuration);
        }

        /// <summary>
        /// Shows a message popup with a custom duration.
        /// </summary>
        /// <param name="message">The message to display</param>
        /// <param name="duration">How long to display before fading (in seconds)</param>
        public void ShowMessage(string message, float duration)
        {
            if (_popupContainer == null || _messageText == null)
            {
                Debug.LogWarning("[MessagePopup] Cannot show message - references not set!");
                return;
            }

            // Ensure the MessagePopup GameObject itself is active (required for coroutines)
            if (!gameObject.activeInHierarchy)
            {
                gameObject.SetActive(true);
            }

            // Stop any existing popup coroutine
            if (_currentPopupCoroutine != null)
            {
                StopCoroutine(_currentPopupCoroutine);
            }

            // Set the message text
            _messageText.text = message;

            // Start the show/hide coroutine
            _currentPopupCoroutine = StartCoroutine(ShowAndHideCoroutine(duration));

            Debug.Log($"[MessagePopup] Showing message: {message}");
        }

        /// <summary>
        /// Immediately hides the popup without fade animation.
        /// </summary>
        public void HideImmediate()
        {
            if (_currentPopupCoroutine != null)
            {
                StopCoroutine(_currentPopupCoroutine);
                _currentPopupCoroutine = null;
            }

            if (_popupContainer != null)
            {
                _popupContainer.SetActive(false);
            }

            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = 1f;
            }

            // Reset game over mode
            _isGameOverMode = false;

            // Hide continue button
            if (_continueButton != null)
            {
                _continueButton.gameObject.SetActive(false);
            }

            // Don't reset position - preserve the player's dragged position for next popup
        }

        /// <summary>
        /// Static helper to show a message from anywhere without needing a reference.
        /// </summary>
        public static void Show(string message)
        {
            if (Instance != null)
            {
                Instance.ShowMessage(message);
            }
            else
            {
                Debug.LogWarning($"[MessagePopup] No instance found! Message: {message}");
            }
        }

        /// <summary>
        /// Static helper to show a message with custom duration.
        /// </summary>
        public static void Show(string message, float duration)
        {
            if (Instance != null)
            {
                Instance.ShowMessage(message, duration);
            }
            else
            {
                Debug.LogWarning($"[MessagePopup] No instance found! Message: {message}");
            }
        }

        #endregion

        #region Private Methods

        private IEnumerator ShowAndHideCoroutine(float displayDuration, bool playSound = true)
        {
            // Play popup sound (skip when just resetting timer from drag)
            if (playSound)
            {
                DLYH.Audio.UIAudioManager.PopupOpen();

                // Position popup - use last dragged position if player has moved it, otherwise use top of screen
                if (_popupRect != null)
                {
                    if (_hasBeenDragged)
                    {
                        _popupRect.anchoredPosition = _lastDraggedPosition;
                    }
                    else
                    {
                        // Default to top area of screen so it doesn't cover center content
                        _popupRect.anchoredPosition = GetDefaultTopPosition();
                    }
                }
            }

            // Show the popup
            _popupContainer.SetActive(true);
            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = 1f;
            }

            // Wait for display duration
            yield return new WaitForSeconds(displayDuration);

            // Fade out
            if (_canvasGroup != null)
            {
                float elapsed = 0f;
                while (elapsed < _fadeOutDuration)
                {
                    elapsed += Time.deltaTime;
                    _canvasGroup.alpha = 1f - (elapsed / _fadeOutDuration);
                    yield return null;
                }
                _canvasGroup.alpha = 0f;
            }

            // Hide completely
            _popupContainer.SetActive(false);

            // Reset alpha for next show
            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = 1f;
            }

            _currentPopupCoroutine = null;
        }

        /// <summary>
        /// Gets the default position at the top of the screen.
        /// </summary>
        private Vector2 GetDefaultTopPosition()
        {
            if (_canvasRect == null) return Vector2.zero;

            // Position near top of canvas, leaving some margin
            // Using about 35% from the top of the screen
            float topY = _canvasRect.rect.height * 0.35f;
            return new Vector2(0, topY);
        }

        /// <summary>
        /// Resets the dragged position memory (call when starting a new game).
        /// </summary>
        public void ResetDraggedPosition()
        {
            _hasBeenDragged = false;
            _lastDraggedPosition = Vector2.zero;
        }

        /// <summary>
        /// Static helper to reset dragged position.
        /// </summary>
        public static void ResetPosition()
        {
            if (Instance != null)
            {
                Instance.ResetDraggedPosition();
            }
        }

        #endregion

        #region Game Over Message

        /// <summary>
        /// Shows a game over message with Continue button. Does not auto-hide.
        /// The popup is draggable so players can move it to see the board.
        /// </summary>
        /// <param name="message">The game over message to display</param>
        public void ShowGameOverMessage(string message)
        {
            if (_popupContainer == null || _messageText == null)
            {
                Debug.LogWarning("[MessagePopup] Cannot show game over message - references not set!");
                return;
            }

            // Ensure the MessagePopup GameObject itself is active
            if (!gameObject.activeInHierarchy)
            {
                gameObject.SetActive(true);
            }

            // Stop any existing popup coroutine
            if (_currentPopupCoroutine != null)
            {
                StopCoroutine(_currentPopupCoroutine);
                _currentPopupCoroutine = null;
            }

            _isGameOverMode = true;

            // Set the message text
            _messageText.text = message;

            // Show the popup and continue button
            _popupContainer.SetActive(true);
            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = 1f;
            }

            if (_continueButton != null)
            {
                _continueButton.gameObject.SetActive(true);
            }

            // Position at top of screen so player can see the guillotine animation
            if (_popupRect != null)
            {
                _popupRect.anchoredPosition = GetDefaultTopPosition();
            }

            // Play popup sound
            DLYH.Audio.UIAudioManager.PopupOpen();

            Debug.Log($"[MessagePopup] Showing game over message: {message}");
        }

        /// <summary>
        /// Static helper to show a game over message.
        /// </summary>
        public static void ShowGameOver(string message)
        {
            if (Instance != null)
            {
                Instance.ShowGameOverMessage(message);
            }
            else
            {
                Debug.LogWarning($"[MessagePopup] No instance found! Game over message: {message}");
            }
        }

        private void HandleContinueClicked()
        {
            Debug.Log("[MessagePopup] Continue button clicked");

            // Play button click sound
            DLYH.Audio.UIAudioManager.ButtonClick();

            // Hide the popup
            HideImmediate();

            // Fire event
            OnContinueClicked?.Invoke();
        }

        #endregion

        #region Drag Handlers

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (_popupRect == null) return;

            // Calculate offset from pointer to popup center
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _canvasRect, eventData.position, eventData.pressEventCamera, out Vector2 localPoint);
            _dragOffset = _popupRect.anchoredPosition - localPoint;
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (_popupRect == null || _canvasRect == null) return;

            // Convert screen position to local position
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _canvasRect, eventData.position, eventData.pressEventCamera, out Vector2 localPoint);

            // Apply new position with offset
            _popupRect.anchoredPosition = localPoint + _dragOffset;

            // Remember this position for future popups
            _lastDraggedPosition = _popupRect.anchoredPosition;
            _hasBeenDragged = true;

            // If not in game over mode, reset the display timer (without replaying sound)
            if (!_isGameOverMode && _currentPopupCoroutine != null)
            {
                StopCoroutine(_currentPopupCoroutine);
                _currentPopupCoroutine = StartCoroutine(ShowAndHideCoroutine(_displayDuration, playSound: false));
            }
        }

        #endregion

        #region Editor Testing

#if UNITY_EDITOR
        [Button("Test Message")]
        private void TestMessage()
        {
            ShowMessage("This is a test message!");
        }

        [Button("Test Turn Change")]
        private void TestTurnChange()
        {
            ShowMessage("Player 2's Turn");
        }

        [Button("Test Already Guessed")]
        private void TestAlreadyGuessed()
        {
            ShowMessage("Letter 'E' already guessed - try again!");
        }
#endif

        #endregion
    }
}

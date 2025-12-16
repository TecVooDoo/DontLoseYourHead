// MessagePopup.cs
// Generic message popup that displays center screen and fades out
// Created: December 14, 2025

using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using Sirenix.OdinInspector;

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
    public class MessagePopup : MonoBehaviour
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

        #endregion

        #region Private Fields

        private Coroutine _currentPopupCoroutine;
        private CanvasGroup _canvasGroup;

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

        private IEnumerator ShowAndHideCoroutine(float displayDuration)
        {
            // Play popup sound
            DLYH.Audio.UIAudioManager.PopupOpen();

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

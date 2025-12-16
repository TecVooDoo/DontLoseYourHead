// FeedbackPanel.cs
// Handles player feedback submission at end of game
// Created: December 16, 2025

using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DLYH.Telemetry;

namespace TecVooDoo.DontLoseYourHead.UI
{
    /// <summary>
    /// Panel for collecting player feedback after a game ends.
    /// Sends feedback to telemetry system before returning to main menu.
    /// </summary>
    public class FeedbackPanel : MonoBehaviour
    {
        #region Serialized Fields

        [Header("UI References")]
        [SerializeField] private TMP_Text _titleText;
        [SerializeField] private TMP_InputField _feedbackInput;
        [SerializeField] private Button _submitButton;
        [SerializeField] private Button _skipButton;

        [Header("Title Text Options")]
        [SerializeField] private string _winTitle = "VICTORY!";
        [SerializeField] private string _loseTitle = "DEFEATED";
        [SerializeField] private string _menuTitle = "FEEDBACK";

        [Header("Placeholder Text")]
        [SerializeField] private string _placeholderText = "Share your thoughts, suggestions, or report any issues... (optional)";

        #endregion

        #region Private Fields

        private bool _playerWon;
        private bool _fromMenu = false;

        #endregion

        #region Events

        /// <summary>Fired when player finishes with feedback panel (submit or skip)</summary>
        public event System.Action OnFeedbackComplete;

        #endregion

        #region Unity Lifecycle

        private void Start()
        {
            if (_submitButton != null)
            {
                _submitButton.onClick.AddListener(OnSubmitClicked);
            }

            if (_skipButton != null)
            {
                _skipButton.onClick.AddListener(OnSkipClicked);
            }

            // Set placeholder text if input field exists
            if (_feedbackInput != null && _feedbackInput.placeholder != null)
            {
                TMP_Text placeholder = _feedbackInput.placeholder as TMP_Text;
                if (placeholder != null)
                {
                    placeholder.text = _placeholderText;
                }
            }

            // Start hidden
            gameObject.SetActive(false);
        }

        private void OnDestroy()
        {
            if (_submitButton != null)
            {
                _submitButton.onClick.RemoveListener(OnSubmitClicked);
            }

            if (_skipButton != null)
            {
                _skipButton.onClick.RemoveListener(OnSkipClicked);
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Show the feedback panel after game ends
        /// </summary>
        /// <param name="playerWon">Whether the player won the game</param>
        public void Show(bool playerWon)
        {
            _playerWon = playerWon;
            _fromMenu = false;

            // Update title based on win/lose
            if (_titleText != null)
            {
                _titleText.text = playerWon ? _winTitle : _loseTitle;
            }

            ShowCommon();
            Debug.Log($"[FeedbackPanel] Shown - Player {(playerWon ? "won" : "lost")}");
        }

        /// <summary>
        /// Show the feedback panel from main menu (not after a game)
        /// </summary>
        public void ShowFromMenu()
        {
            _playerWon = false;
            _fromMenu = true;

            // Show neutral title for menu access
            if (_titleText != null)
            {
                _titleText.text = _menuTitle;
            }

            ShowCommon();
            Debug.Log("[FeedbackPanel] Shown from menu");
        }

        private void ShowCommon()
        {
            // Clear previous feedback
            if (_feedbackInput != null)
            {
                _feedbackInput.text = "";
            }

            gameObject.SetActive(true);

            // Focus the input field for convenience
            if (_feedbackInput != null)
            {
                _feedbackInput.Select();
                _feedbackInput.ActivateInputField();
            }
        }

        /// <summary>
        /// Hide the feedback panel
        /// </summary>
        public void Hide()
        {
            gameObject.SetActive(false);
        }

        #endregion

        #region Button Handlers

        private void OnSubmitClicked()
        {
            string feedbackText = _feedbackInput != null ? _feedbackInput.text.Trim() : "";

            // Only send telemetry if there's actual feedback
            if (!string.IsNullOrEmpty(feedbackText))
            {
                // For menu feedback, we pass false for playerWon (not relevant)
                PlaytestTelemetry.Feedback(feedbackText, _fromMenu ? false : _playerWon);
                string source = _fromMenu ? "menu" : (_playerWon ? "win" : "loss");
                Debug.Log($"[FeedbackPanel] Feedback submitted ({source}): {feedbackText.Substring(0, Mathf.Min(50, feedbackText.Length))}...");
            }
            else
            {
                Debug.Log("[FeedbackPanel] Submit clicked but no feedback entered");
            }

            Hide();
            OnFeedbackComplete?.Invoke();
        }

        private void OnSkipClicked()
        {
            Debug.Log("[FeedbackPanel] Skipped");
            Hide();
            OnFeedbackComplete?.Invoke();
        }

        #endregion
    }
}

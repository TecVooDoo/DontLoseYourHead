using UnityEngine;
using System;

namespace TecVooDoo.DontLoseYourHead.UI
{
    /// <summary>
    /// Manages popup message display during gameplay.
    /// Handles turn popups, error popups, and game over messages.
    /// Extracted from GameplayUIController to reduce file size.
    /// </summary>
    public class PopupMessageController : IDisposable
    {
        #region Events

        /// <summary>Fired when Continue is clicked on game over popup. Main controller manages the result.</summary>
        public event Action OnGameOverContinueClicked;

        #endregion

        #region Private Fields

        private SetupData _playerData;
        private SetupData _opponentData;

        #endregion

        #region Constructor

        public PopupMessageController()
        {
            // Subscribe to MessagePopup Continue button
            if (MessagePopup.Instance != null)
            {
                MessagePopup.Instance.OnContinueClicked += HandleGameOverContinue;
            }
        }

        #endregion

        #region Initialization

        /// <summary>
        /// Set player and opponent data for message formatting.
        /// </summary>
        public void Initialize(SetupData playerData, SetupData opponentData)
        {
            _playerData = playerData;
            _opponentData = opponentData;
        }

        #endregion

        #region Popup Methods

        /// <summary>
        /// Shows a popup message for turn changes and feedback.
        /// </summary>
        public void ShowTurnPopup(string message)
        {
            if (MessagePopup.Instance != null)
            {
                MessagePopup.Instance.ShowMessage(message);
            }
            Debug.Log($"[PopupMessageController] Popup: {message}");
        }

        /// <summary>
        /// Shows a popup for invalid player actions (already guessed, invalid word, etc.)
        /// </summary>
        public void ShowErrorPopup(string message)
        {
            if (MessagePopup.Instance != null)
            {
                MessagePopup.Instance.ShowMessage(message, 1.5f); // Shorter duration for errors
            }
            Debug.Log($"[PopupMessageController] Error Popup: {message}");
        }

        /// <summary>
        /// Shows a game over popup with appropriate message based on win/lose reason.
        /// </summary>
        public void ShowGameOverPopup(bool playerWon, GameOverReason reason)
        {
            string playerName = _playerData?.PlayerName ?? "Player";
            string opponentName = _opponentData?.PlayerName ?? "Opponent";
            string message;

            switch (reason)
            {
                case GameOverReason.AllWordsFound:
                    message = $"{playerName} found all of {opponentName}'s words! VICTORY!";
                    break;
                case GameOverReason.MissLimitReached:
                    message = $"{playerName} reached the miss limit. {opponentName} wins by default!";
                    break;
                case GameOverReason.OpponentFoundAllWords:
                    message = $"{opponentName} found all of {playerName}'s words! DEFEATED!";
                    break;
                case GameOverReason.OpponentMissLimitReached:
                    message = $"{opponentName} reached the miss limit. {playerName} wins by default!";
                    break;
                default:
                    message = playerWon ? "VICTORY!" : "DEFEATED!";
                    break;
            }

            if (MessagePopup.Instance != null)
            {
                MessagePopup.Instance.ShowGameOverMessage(message);
            }
            Debug.Log($"[PopupMessageController] Game Over Popup: {message}");
        }

        /// <summary>
        /// Format and show player letter guess result.
        /// </summary>
        public void ShowPlayerLetterGuessResult(char letter, bool wasHit, string playerName)
        {
            string resultText = wasHit ? "Hit" : "Miss";
            ShowTurnPopup($"{playerName} guessed letter '{letter}' - {resultText}.");
        }

        /// <summary>
        /// Format and show player coordinate guess result.
        /// </summary>
        public void ShowPlayerCoordinateGuessResult(int col, int row, bool wasHit, string playerName)
        {
            string colLabel = ((char)('A' + col)).ToString();
            string coordLabel = colLabel + (row + 1);
            string resultText = wasHit ? "Hit" : "Miss";
            ShowTurnPopup($"{playerName} guessed {coordLabel} - {resultText}.");
        }

        /// <summary>
        /// Format and show player word guess result.
        /// </summary>
        public void ShowPlayerWordGuessResult(string word, bool wasCorrect, string playerName)
        {
            string resultText = wasCorrect ? "Correct" : "Incorrect";
            ShowTurnPopup($"{playerName} guessed word '{word.ToUpper()}' - {resultText}.");
        }

        /// <summary>
        /// Format and show opponent letter guess result.
        /// </summary>
        public void ShowOpponentLetterGuessResult(char letter, bool wasHit, string opponentName, string playerName)
        {
            string resultText = wasHit ? "Hit" : "Miss";
            ShowTurnPopup($"{opponentName} guessed letter '{letter}' - {resultText}. {playerName}'s turn!");
        }

        /// <summary>
        /// Format and show opponent coordinate guess result.
        /// </summary>
        public void ShowOpponentCoordinateGuessResult(int col, int row, bool wasHit, string opponentName, string playerName)
        {
            string colLabel = ((char)('A' + col)).ToString();
            string coordLabel = colLabel + (row + 1);
            string resultText = wasHit ? "Hit" : "Miss";
            ShowTurnPopup($"{opponentName} guessed {coordLabel} - {resultText}. {playerName}'s turn!");
        }

        /// <summary>
        /// Format and show opponent word guess result.
        /// </summary>
        public void ShowOpponentWordGuessResult(string word, bool wasCorrect, string opponentName, string playerName)
        {
            string resultText = wasCorrect ? "Correct" : "Incorrect";
            ShowTurnPopup($"{opponentName} guessed word '{word.ToUpper()}' - {resultText}. {playerName}'s turn!");
        }

        /// <summary>
        /// Show extra turn message.
        /// </summary>
        public void ShowExtraTurnMessage(string baseMessage, string completedWord)
        {
            string message = $"{baseMessage} You completed \"{completedWord}\" - EXTRA TURN!";
            ShowTurnPopup(message);
        }

        /// <summary>
        /// Show turn end message with opponent's turn.
        /// </summary>
        public void ShowTurnEndMessage(string baseMessage, string opponentName)
        {
            string message = $"{baseMessage} {opponentName}'s turn!";
            ShowTurnPopup(message);
        }

        /// <summary>
        /// Show opponent disconnect message.
        /// </summary>
        public void ShowOpponentDisconnected()
        {
            ShowTurnPopup("Opponent disconnected. Waiting for reconnection...");
        }

        /// <summary>
        /// Show opponent reconnect message.
        /// </summary>
        public void ShowOpponentReconnected()
        {
            ShowTurnPopup("Opponent reconnected!");
        }

        #endregion

        #region Event Handlers

        /// <summary>
        /// Called when the Continue button is clicked on the game over popup.
        /// Just relays the event - main controller manages the result state.
        /// </summary>
        private void HandleGameOverContinue()
        {
            Debug.Log("[PopupMessageController] Continue clicked - relaying to main controller");
            OnGameOverContinueClicked?.Invoke();
        }

        #endregion

        #region IDisposable

        public void Dispose()
        {
            if (MessagePopup.Instance != null)
            {
                MessagePopup.Instance.OnContinueClicked -= HandleGameOverContinue;
            }
        }

        #endregion
    }

    /// <summary>
    /// Reason for game ending - used for proper game over messages.
    /// </summary>
    public enum GameOverReason
    {
        AllWordsFound,              // Player found all opponent's words
        MissLimitReached,           // Player exceeded miss limit
        OpponentFoundAllWords,      // Opponent found all player's words
        OpponentMissLimitReached    // Opponent exceeded miss limit
    }
}

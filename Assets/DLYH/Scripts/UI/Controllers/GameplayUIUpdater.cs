using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace TecVooDoo.DontLoseYourHead.UI
{
    /// <summary>
    /// Manages UI updates during gameplay including:
    /// - Miss counter text
    /// - Center panel names and colors
    /// - Turn indicator text
    /// - Guillotine displays
    /// Extracted from GameplayUIController to reduce file size.
    /// </summary>
    public class GameplayUIUpdater
    {
        #region Private Fields

        // Miss counter references
        private TextMeshProUGUI _player1MissCounter;
        private TextMeshProUGUI _player2MissCounter;
        private Image _player1MissLabelBackground;
        private Image _player2MissLabelBackground;

        // Center panel name labels
        private TextMeshProUGUI _player1NameLabel;
        private TextMeshProUGUI _player2NameLabel;
        private Image _player1ColorIndicator;
        private Image _player2ColorIndicator;

        // Guessed word list label backgrounds
        private Image _player1GuessedWordsLabelBackground;
        private Image _player2GuessedWordsLabelBackground;

        // Turn indicator
        private TextMeshProUGUI _turnIndicatorText;

        // Guillotine displays
        private GuillotineDisplay _player1Guillotine;
        private GuillotineDisplay _player2Guillotine;

        // State tracker reference
        private GameplayStateTracker _stateTracker;

        // Setup data for names and colors
        private SetupData _playerData;
        private SetupData _opponentData;

        #endregion

        #region Constructor

        /// <summary>
        /// Create a new GameplayUIUpdater with UI references.
        /// </summary>
        public GameplayUIUpdater(
            TextMeshProUGUI player1MissCounter,
            TextMeshProUGUI player2MissCounter,
            Image player1MissLabelBackground,
            Image player2MissLabelBackground,
            TextMeshProUGUI player1NameLabel,
            TextMeshProUGUI player2NameLabel,
            Image player1ColorIndicator,
            Image player2ColorIndicator,
            Image player1GuessedWordsLabelBackground,
            Image player2GuessedWordsLabelBackground,
            TextMeshProUGUI turnIndicatorText,
            GuillotineDisplay player1Guillotine,
            GuillotineDisplay player2Guillotine)
        {
            _player1MissCounter = player1MissCounter;
            _player2MissCounter = player2MissCounter;
            _player1MissLabelBackground = player1MissLabelBackground;
            _player2MissLabelBackground = player2MissLabelBackground;
            _player1NameLabel = player1NameLabel;
            _player2NameLabel = player2NameLabel;
            _player1ColorIndicator = player1ColorIndicator;
            _player2ColorIndicator = player2ColorIndicator;
            _player1GuessedWordsLabelBackground = player1GuessedWordsLabelBackground;
            _player2GuessedWordsLabelBackground = player2GuessedWordsLabelBackground;
            _turnIndicatorText = turnIndicatorText;
            _player1Guillotine = player1Guillotine;
            _player2Guillotine = player2Guillotine;
        }

        #endregion

        #region Initialization

        /// <summary>
        /// Initialize the updater with game state and setup data.
        /// </summary>
        public void Initialize(GameplayStateTracker stateTracker, SetupData playerData, SetupData opponentData)
        {
            _stateTracker = stateTracker;
            _playerData = playerData;
            _opponentData = opponentData;

            // Initial UI updates
            UpdateMissCounters();
            UpdateCenterPanelNames();
            UpdateTurnIndicator();
            InitializeGuillotines();
        }

        #endregion

        #region Miss Counter Updates

        /// <summary>
        /// Update both player and opponent miss counters.
        /// </summary>
        public void UpdateMissCounters()
        {
            if (_player1MissCounter != null && _stateTracker != null)
            {
                _player1MissCounter.text = _stateTracker.GetPlayerMissCounterText();
            }

            if (_player2MissCounter != null && _stateTracker != null)
            {
                _player2MissCounter.text = _stateTracker.GetOpponentMissCounterText();
            }
        }

        /// <summary>
        /// Update player's miss counter and guillotine.
        /// </summary>
        public void UpdatePlayerMissCounter()
        {
            if (_player1MissCounter != null && _stateTracker != null)
            {
                _player1MissCounter.text = _stateTracker.GetPlayerMissCounterText();
            }

            UpdatePlayerGuillotine();
        }

        /// <summary>
        /// Update opponent's miss counter and guillotine.
        /// </summary>
        public void UpdateOpponentMissCounter()
        {
            if (_player2MissCounter != null && _stateTracker != null)
            {
                _player2MissCounter.text = _stateTracker.GetOpponentMissCounterText();
            }

            UpdateOpponentGuillotine();
        }

        #endregion

        #region Name and Color Updates

        /// <summary>
        /// Update center panel player names and color indicators.
        /// </summary>
        public void UpdateCenterPanelNames()
        {
            if (_player1NameLabel != null && _playerData != null)
            {
                _player1NameLabel.text = _playerData.PlayerName;
            }

            if (_player2NameLabel != null && _opponentData != null)
            {
                _player2NameLabel.text = _opponentData.PlayerName;
            }

            if (_player1ColorIndicator != null && _playerData != null)
            {
                _player1ColorIndicator.color = _playerData.PlayerColor;
            }

            if (_player2ColorIndicator != null && _opponentData != null)
            {
                _player2ColorIndicator.color = _opponentData.PlayerColor;
            }

            ApplyPlayerColorsToLabels();
        }

        /// <summary>
        /// Apply player colors to miss counter and guessed word list label backgrounds.
        /// </summary>
        private void ApplyPlayerColorsToLabels()
        {
            if (_playerData != null)
            {
                Color player1Color = _playerData.PlayerColor;

                if (_player1MissLabelBackground != null)
                    _player1MissLabelBackground.color = player1Color;

                if (_player1GuessedWordsLabelBackground != null)
                    _player1GuessedWordsLabelBackground.color = player1Color;
            }

            if (_opponentData != null)
            {
                Color player2Color = _opponentData.PlayerColor;

                if (_player2MissLabelBackground != null)
                    _player2MissLabelBackground.color = player2Color;

                if (_player2GuessedWordsLabelBackground != null)
                    _player2GuessedWordsLabelBackground.color = player2Color;
            }
        }

        #endregion

        #region Turn Indicator

        /// <summary>
        /// Update the turn indicator text based on current game state.
        /// </summary>
        public void UpdateTurnIndicator()
        {
            if (_turnIndicatorText == null || _stateTracker == null) return;

            if (_stateTracker.GameOver)
            {
                _turnIndicatorText.text = "GAME OVER";
            }
            else if (_stateTracker.IsPlayerTurn)
            {
                _turnIndicatorText.text = "Your Turn";
            }
            else
            {
                _turnIndicatorText.text = "EXECUTIONER's Turn...";
            }
        }

        #endregion

        #region Guillotine Display

        /// <summary>
        /// Initialize guillotine displays with miss limits and player colors.
        /// </summary>
        public void InitializeGuillotines()
        {
            if (_player1Guillotine != null && _playerData != null && _stateTracker != null)
            {
                _player1Guillotine.Initialize(_stateTracker.PlayerMissLimit, _playerData.PlayerColor);
            }

            if (_player2Guillotine != null && _opponentData != null && _stateTracker != null)
            {
                _player2Guillotine.Initialize(_stateTracker.OpponentMissLimit, _opponentData.PlayerColor);
            }
        }

        /// <summary>
        /// Update player's guillotine display and both faces.
        /// </summary>
        public void UpdatePlayerGuillotine()
        {
            if (_player1Guillotine != null && _stateTracker != null)
            {
                _player1Guillotine.UpdateMissCount(_stateTracker.PlayerMisses);
                UpdateGuillotineFaces();
            }
        }

        /// <summary>
        /// Update opponent's guillotine display and both faces.
        /// </summary>
        public void UpdateOpponentGuillotine()
        {
            if (_player2Guillotine != null && _stateTracker != null)
            {
                _player2Guillotine.UpdateMissCount(_stateTracker.OpponentMisses);
                UpdateGuillotineFaces();
            }
        }

        /// <summary>
        /// Update both guillotine faces based on danger levels.
        /// </summary>
        private void UpdateGuillotineFaces()
        {
            if (_stateTracker == null) return;

            // Player 1's face reacts to their own misses and opponent's misses
            if (_player1Guillotine != null)
            {
                _player1Guillotine.UpdateFace(_stateTracker.OpponentMisses, _stateTracker.OpponentMissLimit);
            }

            // Player 2's face reacts to their own misses and player's misses
            if (_player2Guillotine != null)
            {
                _player2Guillotine.UpdateFace(_stateTracker.PlayerMisses, _stateTracker.PlayerMissLimit);
            }
        }

        /// <summary>
        /// Trigger game over animation for player's guillotine (player reached miss limit).
        /// </summary>
        public void TriggerPlayerGuillotineGameOver()
        {
            if (_player1Guillotine != null)
            {
                _player1Guillotine.AnimateGameOver();
            }
            // Winner (player 2) gets evil smile
            if (_player2Guillotine != null)
            {
                _player2Guillotine.SetExecutionFace(false);
            }
        }

        /// <summary>
        /// Trigger game over animation for opponent's guillotine (opponent reached miss limit).
        /// </summary>
        public void TriggerOpponentGuillotineGameOver()
        {
            if (_player2Guillotine != null)
            {
                _player2Guillotine.AnimateGameOver();
            }
            // Winner (player 1) gets evil smile
            if (_player1Guillotine != null)
            {
                _player1Guillotine.SetExecutionFace(false);
            }
        }

        /// <summary>
        /// Trigger defeat animation for player's guillotine (opponent found all words).
        /// </summary>
        public void TriggerPlayerGuillotineDefeatByWords()
        {
            if (_player1Guillotine != null)
            {
                _player1Guillotine.AnimateDefeatByWordsFound();
            }
            // Winner (player 2) gets evil smile
            if (_player2Guillotine != null)
            {
                _player2Guillotine.SetExecutionFace(false);
            }
        }

        /// <summary>
        /// Trigger defeat animation for opponent's guillotine (player found all words).
        /// </summary>
        public void TriggerOpponentGuillotineDefeatByWords()
        {
            if (_player2Guillotine != null)
            {
                _player2Guillotine.AnimateDefeatByWordsFound();
            }
            // Winner (player 1) gets evil smile
            if (_player1Guillotine != null)
            {
                _player1Guillotine.SetExecutionFace(false);
            }
        }

        /// <summary>
        /// Reset both guillotines for a new game.
        /// </summary>
        public void ResetGuillotines()
        {
            if (_player1Guillotine != null)
            {
                _player1Guillotine.Reset();
            }
            if (_player2Guillotine != null)
            {
                _player2Guillotine.Reset();
            }
        }

        #endregion
    }
}

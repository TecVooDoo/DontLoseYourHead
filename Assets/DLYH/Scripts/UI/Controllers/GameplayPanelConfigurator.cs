using UnityEngine;
using System.Collections.Generic;
using TecVooDoo.DontLoseYourHead.Core;

namespace TecVooDoo.DontLoseYourHead.UI
{
    /// <summary>
    /// Configures PlayerGridPanel instances for gameplay mode.
    /// Handles placing words on grids, setting up word pattern rows, and managing visibility.
    /// Extracted from GameplayUIController to reduce file size.
    /// </summary>
    public class GameplayPanelConfigurator
    {
        #region Public Methods

        /// <summary>
        /// Configure the owner panel (player's own grid, fully revealed).
        /// </summary>
        /// <param name="panel">The PlayerGridPanel to configure</param>
        /// <param name="playerData">Player's setup data</param>
        /// <param name="opponentColor">Opponent's color for hit feedback</param>
        public void ConfigureOwnerPanel(
            PlayerGridPanel panel,
            SetupData playerData,
            Color opponentColor)
        {
            if (panel == null || playerData == null)
            {
                Debug.LogError("[GameplayPanelConfigurator] Cannot configure owner panel - missing references!");
                return;
            }

            panel.InitializeGrid(playerData.GridSize);
            panel.SetPlayerName(playerData.PlayerName);
            panel.SetPlayerColor(playerData.PlayerColor);
            panel.SetMode(PlayerGridPanel.PanelMode.Gameplay);
            panel.CacheWordPatternRows();

            // Set hit color to opponent's color (opponent guesses on this panel)
            panel.SetGuesserHitColor(opponentColor);

            // Place words on grid (revealed)
            foreach (WordPlacementData wordData in playerData.PlacedWords)
            {
                PlaceWordOnPanelRevealed(panel, wordData);

                WordPatternRow row = panel.GetWordPatternRow(wordData.RowIndex);
                if (row != null)
                {
                    row.SetGameplayWord(wordData.Word);
                    row.RevealAllLetters();
                    row.SetAsOwnerPanel();
                }
                else
                {
                    Debug.LogError($"[GameplayPanelConfigurator] Owner row {wordData.RowIndex} is NULL! Cannot set word '{wordData.Word}'");
                }
            }

            // Manage row visibility
            SetRowVisibility(panel, playerData.PlacedWords.Count, true);
        }

        /// <summary>
        /// Configure the opponent panel (enemy grid, hidden until revealed).
        /// </summary>
        /// <param name="panel">The PlayerGridPanel to configure</param>
        /// <param name="opponentData">Opponent's setup data</param>
        /// <param name="playerColor">Player's color for hit feedback</param>
        public void ConfigureOpponentPanel(
            PlayerGridPanel panel,
            SetupData opponentData,
            Color playerColor)
        {
            if (panel == null || opponentData == null)
            {
                Debug.LogError("[GameplayPanelConfigurator] Cannot configure opponent panel - missing references!");
                return;
            }

            panel.InitializeGrid(opponentData.GridSize);
            panel.SetMode(PlayerGridPanel.PanelMode.Gameplay);
            panel.SetPlayerName(opponentData.PlayerName);
            panel.SetPlayerColor(opponentData.PlayerColor);
            panel.CacheWordPatternRows();

            // Set hit color to player's color (player guesses on this panel)
            panel.SetGuesserHitColor(playerColor);

            // Place words on grid (hidden)
            foreach (WordPlacementData wordData in opponentData.PlacedWords)
            {
                PlaceWordOnPanelHidden(panel, wordData);

                WordPatternRow row = panel.GetWordPatternRow(wordData.RowIndex);
                if (row != null)
                {
                    row.SetGameplayWord(wordData.Word);
                    row.ResetRevealedLetters();
                }
            }

            // Manage row visibility
            SetRowVisibility(panel, opponentData.PlacedWords.Count, false);
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Place a word on the grid with letters visible.
        /// </summary>
        private void PlaceWordOnPanelRevealed(PlayerGridPanel panel, WordPlacementData wordData)
        {
            for (int i = 0; i < wordData.Word.Length; i++)
            {
                int col = wordData.StartCol + (i * wordData.DirCol);
                int row = wordData.StartRow + (i * wordData.DirRow);
                char letter = wordData.Word[i];

                GridCellUI cell = panel.GetCell(col, row);
                if (cell != null)
                {
                    cell.SetLetter(letter);
                    cell.SetState(CellState.Filled);
                }
            }
        }

        /// <summary>
        /// Place a word on the grid with letters hidden.
        /// </summary>
        private void PlaceWordOnPanelHidden(PlayerGridPanel panel, WordPlacementData wordData)
        {
            for (int i = 0; i < wordData.Word.Length; i++)
            {
                int col = wordData.StartCol + (i * wordData.DirCol);
                int row = wordData.StartRow + (i * wordData.DirRow);
                char letter = wordData.Word[i];

                GridCellUI cell = panel.GetCell(col, row);
                if (cell != null)
                {
                    cell.SetHiddenLetter(letter);
                }
            }
        }

        /// <summary>
        /// Set visibility for word pattern rows based on word count.
        /// </summary>
        private void SetRowVisibility(PlayerGridPanel panel, int wordCount, bool isOwnerPanel)
        {
            WordPatternRow[] allRows = panel.GetWordPatternRows();
            if (allRows == null) return;

            for (int i = 0; i < allRows.Length; i++)
            {
                if (allRows[i] != null)
                {
                    bool shouldBeActive = i < wordCount;
                    allRows[i].gameObject.SetActive(shouldBeActive);

                    if (isOwnerPanel)
                    {
                        allRows[i].SetAsOwnerPanel();
                    }
                }
            }
        }

        #endregion
    }

    /// <summary>
    /// Data structure to hold captured setup information.
    /// Used for passing setup data between controllers.
    /// </summary>
    public class SetupData
    {
        public string PlayerName;
        public Color PlayerColor;
        public int GridSize;
        public int WordCount;
        public DifficultySetting DifficultyLevel;
        public int[] WordLengths;
        public List<WordPlacementData> PlacedWords = new List<WordPlacementData>();
    }
}

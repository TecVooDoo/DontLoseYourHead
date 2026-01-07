using UnityEngine;
using System.Collections.Generic;
using TecVooDoo.DontLoseYourHead.Core;
using DLYH.AI.Core;

namespace TecVooDoo.DontLoseYourHead.UI
{
    /// <summary>
    /// Handles capturing player setup data from UI and generating opponent (AI) setup data.
    /// Extracted from GameplayUIController to reduce file size.
    /// </summary>
    public class GameSetupDataCapture
    {
        private readonly SetupSettingsPanel _setupSettingsPanel;
        private readonly PlayerGridPanel _setupGridPanel;
        private readonly Dictionary<int, WordListSO> _wordLists;

        public GameSetupDataCapture(
            SetupSettingsPanel setupSettingsPanel,
            PlayerGridPanel setupGridPanel,
            Dictionary<int, WordListSO> wordLists)
        {
            _setupSettingsPanel = setupSettingsPanel;
            _setupGridPanel = setupGridPanel;
            _wordLists = wordLists;
        }

        /// <summary>
        /// Captures setup data from the UI panels.
        /// </summary>
        /// <returns>The captured player setup data, or null if capture failed.</returns>
        public SetupData CapturePlayerSetupData()
        {
            if (_setupSettingsPanel == null)
            {
                Debug.LogError("[GameSetupDataCapture] SetupSettingsPanel reference missing!");
                return null;
            }

            if (_setupGridPanel == null)
            {
                Debug.LogError("[GameSetupDataCapture] Setup Grid Panel reference missing!");
                return null;
            }

            // Get settings using tuple methods
            (string playerName, Color playerColor) = _setupSettingsPanel.GetPlayerSettings();
            (int gridSize, WordCountOption wordCount, DifficultySetting difficulty) = _setupSettingsPanel.GetDifficultySettings();

            SetupData playerSetupData = new SetupData
            {
                PlayerName = playerName,
                PlayerColor = playerColor,
                GridSize = gridSize,
                WordCount = (int)wordCount,
                DifficultyLevel = difficulty,
                WordLengths = DifficultyCalculator.GetWordLengths(wordCount)
            };

            WordPatternRow[] wordRows = _setupGridPanel.GetWordPatternRows();
            if (wordRows != null)
            {
                int rowIndex = 0;
                foreach (WordPatternRow row in wordRows)
                {
                    if (row != null && row.gameObject.activeSelf && row.HasWord && row.IsPlaced)
                    {
                        // Construct WordPlacementData from row properties
                        WordPlacementData wordData = new WordPlacementData
                        {
                            Word = row.CurrentWord,
                            StartCol = row.PlacedStartCol,
                            StartRow = row.PlacedStartRow,
                            DirCol = row.PlacedDirCol,
                            DirRow = row.PlacedDirRow,
                            RowIndex = rowIndex
                        };
                        playerSetupData.PlacedWords.Add(wordData);
                    }
                    rowIndex++;
                }
            }

            return playerSetupData;
        }

        /// <summary>
        /// Generate opponent data using AISetupManager for intelligent word selection and placement.
        /// AI settings vary based on player difficulty for variety and appropriate challenge.
        /// </summary>
        /// <param name="playerSetupData">The player's setup data to base opponent settings on.</param>
        /// <returns>The generated opponent setup data.</returns>
        public SetupData GenerateOpponentData(SetupData playerSetupData)
        {
            // Get AI settings based on player's chosen difficulty
            (int gridSize, int wordCount) = GetAISettingsForPlayerDifficulty(playerSetupData.DifficultyLevel);

            int[] wordLengths = DifficultyCalculator.GetWordLengths(
                wordCount == 3 ? WordCountOption.Three : WordCountOption.Four);

            // Create AI setup manager
            AISetupManager setupManager = new AISetupManager(gridSize, wordCount, wordLengths);

            // Perform AI setup (select and place words)
            bool setupSuccess = setupManager.PerformSetup(_wordLists);

            if (!setupSuccess)
            {
                Debug.LogError("[GameSetupDataCapture] AI setup failed! Using fallback.");
                return GenerateOpponentDataFallback(playerSetupData);
            }

            // Create opponent setup data from AI results
            // Invert difficulty: Player Easy = Opponent Hard, Player Hard = Opponent Easy
            DifficultySetting opponentDifficulty = GetInverseDifficulty(playerSetupData.DifficultyLevel);

            SetupData opponentSetupData = new SetupData
            {
                PlayerName = "EXECUTIONER",
                PlayerColor = new Color(0.1f, 0.15f, 0.3f, 1f), // Dark blue - evokes executioner's hood
                GridSize = gridSize,
                WordCount = wordCount,
                DifficultyLevel = opponentDifficulty,
                WordLengths = wordLengths,
                PlacedWords = setupManager.Placements
            };

            Debug.Log(setupManager.GetDebugSummary());
            Debug.Log($"[GameSetupDataCapture] Player difficulty: {playerSetupData.DifficultyLevel}, Opponent difficulty: {opponentDifficulty}");

            return opponentSetupData;
        }

        /// <summary>
        /// Gets AI grid size and word count based on player's difficulty setting.
        /// Adds variety while scaling appropriately with difficulty.
        /// </summary>
        /// <param name="playerDifficulty">The difficulty level chosen by the player</param>
        /// <returns>Tuple of (gridSize, wordCount)</returns>
        public static (int gridSize, int wordCount) GetAISettingsForPlayerDifficulty(DifficultySetting playerDifficulty)
        {
            int gridSize;
            int wordCount;

            switch (playerDifficulty)
            {
                case DifficultySetting.Easy:
                    // Player is on Easy = AI should be easier to beat
                    // Smaller grids (6-8), more words (4) = easier to find AI's words
                    int[] easyGrids = { 6, 7, 8 };
                    gridSize = easyGrids[Random.Range(0, easyGrids.Length)];
                    wordCount = 4;
                    Debug.Log($"[GameSetupDataCapture] AI Settings (Player Easy): {gridSize}x{gridSize} grid, {wordCount} words");
                    break;

                case DifficultySetting.Normal:
                    // Player is on Normal = balanced challenge
                    // Medium grids (8-10), random word count (3-4)
                    int[] normalGrids = { 8, 9, 10 };
                    gridSize = normalGrids[Random.Range(0, normalGrids.Length)];
                    wordCount = Random.Range(0, 2) == 0 ? 3 : 4;
                    Debug.Log($"[GameSetupDataCapture] AI Settings (Player Normal): {gridSize}x{gridSize} grid, {wordCount} words");
                    break;

                case DifficultySetting.Hard:
                    // Player is on Hard = AI should be harder to beat
                    // Larger grids (10-12), fewer words (3) = harder to find AI's words
                    int[] hardGrids = { 10, 11, 12 };
                    gridSize = hardGrids[Random.Range(0, hardGrids.Length)];
                    wordCount = 3;
                    Debug.Log($"[GameSetupDataCapture] AI Settings (Player Hard): {gridSize}x{gridSize} grid, {wordCount} words");
                    break;

                default:
                    // Fallback to medium settings
                    gridSize = 8;
                    wordCount = 3;
                    Debug.LogWarning($"[GameSetupDataCapture] Unknown difficulty {playerDifficulty}, using default 8x8/3 words");
                    break;
            }

            return (gridSize, wordCount);
        }

        /// <summary>
        /// Get the inverse difficulty setting (Easy <-> Hard, Normal stays Normal)
        /// </summary>
        public static DifficultySetting GetInverseDifficulty(DifficultySetting playerDifficulty)
        {
            switch (playerDifficulty)
            {
                case DifficultySetting.Easy:
                    return DifficultySetting.Hard;
                case DifficultySetting.Hard:
                    return DifficultySetting.Easy;
                default:
                    return DifficultySetting.Normal;
            }
        }

        /// <summary>
        /// Fallback opponent data generation if AI setup fails.
        /// Uses dynamic AI settings based on player difficulty.
        /// </summary>
        private SetupData GenerateOpponentDataFallback(SetupData playerSetupData)
        {
            // Invert difficulty for opponent
            DifficultySetting opponentDifficulty = GetInverseDifficulty(playerSetupData.DifficultyLevel);

            // Get AI settings based on player's chosen difficulty
            (int gridSize, int wordCount) = GetAISettingsForPlayerDifficulty(playerSetupData.DifficultyLevel);

            int[] wordLengths = DifficultyCalculator.GetWordLengths(
                wordCount == 3 ? WordCountOption.Three : WordCountOption.Four);

            SetupData opponentSetupData = new SetupData
            {
                PlayerName = "EXECUTIONER",
                PlayerColor = new Color(0.1f, 0.15f, 0.3f, 1f), // Dark blue - evokes executioner's hood
                GridSize = gridSize,
                WordCount = wordCount,
                DifficultyLevel = opponentDifficulty,
                WordLengths = wordLengths
            };

            // Simple fallback words
            string[] fallbackWords = { "CAT", "ROAD", "SNORE", "BRIDGE" };
            int fallbackWordCount = Mathf.Min(wordCount, fallbackWords.Length);

            for (int i = 0; i < fallbackWordCount; i++)
            {
                WordPlacementData fallbackWord = new WordPlacementData
                {
                    Word = fallbackWords[i],
                    StartCol = i,
                    StartRow = i * 2,
                    DirCol = 1,
                    DirRow = 0,
                    RowIndex = i
                };
                opponentSetupData.PlacedWords.Add(fallbackWord);
            }

            Debug.LogWarning("[GameSetupDataCapture] Using fallback opponent data!");
            return opponentSetupData;
        }
    }
}

// GameStateManager.cs
// Manages game state parsing, serialization, encryption, and miss limit calculation
// Extracted from UIFlowController during Phase 3 refactoring (Session 2)
// Created: January 19, 2026
// Developer: TecVooDoo LLC

using System;
using System.Collections.Generic;
using UnityEngine;
using TecVooDoo.DontLoseYourHead.Core;
using TecVooDoo.DontLoseYourHead.UI;
using DLYH.Core.Utilities;
using DLYH.Networking.Services;

namespace DLYH.UI.Managers
{
    /// <summary>
    /// Handles game state parsing, encryption, and calculations.
    /// Extracted from UIFlowController to isolate state management concerns.
    /// </summary>
    public static class GameStateManager
    {
        // ============================================================
        // GAME STATE PARSING
        // ============================================================

        /// <summary>
        /// Parses the full game state JSON from Supabase into a DLYHGameState object.
        /// </summary>
        /// <param name="stateJson">The JSON string from game_state column</param>
        /// <returns>Parsed DLYHGameState or null on failure</returns>
        public static DLYHGameState ParseGameStateJson(string stateJson)
        {
            if (string.IsNullOrEmpty(stateJson))
            {
                return null;
            }

            try
            {
                // Simple manual parsing since Unity's JsonUtility doesn't handle nested objects well
                DLYHGameState state = new DLYHGameState();

                state.version = JsonParsingUtility.ExtractIntField(stateJson, "version");
                state.status = JsonParsingUtility.ExtractStringField(stateJson, "status");
                state.currentTurn = JsonParsingUtility.ExtractStringField(stateJson, "currentTurn");
                state.turnNumber = JsonParsingUtility.ExtractIntField(stateJson, "turnNumber");
                state.winner = JsonParsingUtility.ExtractStringField(stateJson, "winner");

                // Parse player1 and player2 objects
                state.player1 = ParsePlayerData(stateJson, "player1");
                state.player2 = ParsePlayerData(stateJson, "player2");

                return state;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[GameStateManager] Error parsing game state: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Parses a player data object from within the game state JSON.
        /// </summary>
        /// <param name="json">The full JSON containing the player object</param>
        /// <param name="playerKey">The key name ("player1" or "player2")</param>
        /// <returns>Parsed DLYHPlayerData or null if not found</returns>
        public static DLYHPlayerData ParsePlayerData(string json, string playerKey)
        {
            // Find the player object in the JSON
            string pattern = $"\"{playerKey}\":";
            int start = json.IndexOf(pattern);
            if (start < 0) return null;

            start += pattern.Length;

            // Skip whitespace
            while (start < json.Length && char.IsWhiteSpace(json[start])) start++;

            // Check for null
            if (start + 4 <= json.Length && json.Substring(start, 4) == "null") return null;

            // Find matching braces
            if (json[start] != '{') return null;

            int depth = 0;
            int end = start;
            for (int i = start; i < json.Length; i++)
            {
                if (json[i] == '{') depth++;
                else if (json[i] == '}') depth--;
                if (depth == 0)
                {
                    end = i;
                    break;
                }
            }

            string playerJson = json.Substring(start, end - start + 1);

            DLYHPlayerData data = new DLYHPlayerData();

            // Identity
            data.name = JsonParsingUtility.ExtractStringField(playerJson, "name");
            data.color = JsonParsingUtility.ExtractStringField(playerJson, "color");

            // Setup config (flat structure - no nested setupData)
            data.gridSize = JsonParsingUtility.ExtractIntField(playerJson, "gridSize");
            data.wordCount = JsonParsingUtility.ExtractIntField(playerJson, "wordCount");
            data.difficulty = JsonParsingUtility.ExtractStringField(playerJson, "difficulty");

            // Dynamic state
            data.ready = JsonParsingUtility.ExtractBoolField(playerJson, "ready");
            data.setupComplete = JsonParsingUtility.ExtractBoolField(playerJson, "setupComplete");
            data.wordPlacementsEncrypted = JsonParsingUtility.ExtractStringField(playerJson, "wordPlacementsEncrypted");

            // Parse gameplayState
            data.gameplayState = ParseGameplayState(playerJson);

            return data;
        }

        /// <summary>
        /// Parses the gameplay state object from within a player JSON object.
        /// </summary>
        /// <param name="playerJson">The player JSON containing gameplayState</param>
        /// <returns>Parsed DLYHGameplayState or null if not found</returns>
        public static DLYHGameplayState ParseGameplayState(string playerJson)
        {
            string pattern = "\"gameplayState\":";
            int start = playerJson.IndexOf(pattern);
            if (start < 0) return null;

            start += pattern.Length;
            while (start < playerJson.Length && char.IsWhiteSpace(playerJson[start])) start++;
            if (start >= playerJson.Length || playerJson[start] == 'n') return null; // null

            if (playerJson[start] != '{') return null;

            int depth = 0;
            int end = start;
            for (int i = start; i < playerJson.Length; i++)
            {
                if (playerJson[i] == '{') depth++;
                else if (playerJson[i] == '}') depth--;
                if (depth == 0)
                {
                    end = i;
                    break;
                }
            }

            string gameplayJson = playerJson.Substring(start, end - start + 1);

            DLYHGameplayState gameplay = new DLYHGameplayState();
            gameplay.misses = JsonParsingUtility.ExtractIntField(gameplayJson, "misses");
            gameplay.missLimit = JsonParsingUtility.ExtractIntField(gameplayJson, "missLimit");

            // Parse arrays
            gameplay.knownLetters = JsonParsingUtility.ExtractStringArray(gameplayJson, "knownLetters");
            gameplay.solvedWordRows = JsonParsingUtility.ExtractIntArray(gameplayJson, "solvedWordRows");

            // Parse coordinate pairs
            (int row, int col)[] coordTuples = JsonParsingUtility.ExtractCoordinateArray(gameplayJson, "guessedCoordinates");
            gameplay.guessedCoordinates = new CoordinatePair[coordTuples.Length];
            for (int i = 0; i < coordTuples.Length; i++)
            {
                gameplay.guessedCoordinates[i] = new CoordinatePair(coordTuples[i].row, coordTuples[i].col);
            }

            // Parse incorrect word guesses
            gameplay.incorrectWordGuesses = JsonParsingUtility.ExtractStringArray(gameplayJson, "incorrectWordGuesses");

            // Parse revealed cells
            (int row, int col, string letter, bool isHit)[] cellTuples =
                JsonParsingUtility.ExtractRevealedCellsArray(gameplayJson, "revealedCells");
            gameplay.revealedCells = new RevealedCellData[cellTuples.Length];
            for (int i = 0; i < cellTuples.Length; i++)
            {
                gameplay.revealedCells[i] = new RevealedCellData(
                    cellTuples[i].row,
                    cellTuples[i].col,
                    cellTuples[i].letter,
                    cellTuples[i].isHit);
            }

            return gameplay;
        }

        // ============================================================
        // WORD PLACEMENT ENCRYPTION
        // ============================================================

        /// <summary>
        /// Encrypts word placements for storage (hides words from opponent until game end).
        /// Format: Base64 encoded string of "WORD:row,col,dirRow,dirCol;" entries
        /// Supports all 8 directions (horizontal, vertical, and diagonals).
        /// </summary>
        /// <param name="placements">The word placements to encrypt</param>
        /// <returns>Base64 encoded string or empty string if no placements</returns>
        public static string EncryptWordPlacements(List<WordPlacementData> placements)
        {
            if (placements == null || placements.Count == 0)
            {
                return "";
            }

            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            foreach (WordPlacementData p in placements)
            {
                // Store full direction (DirRow, DirCol) to support all 8 directions
                sb.AppendFormat("{0}:{1},{2},{3},{4};",
                    p.Word,
                    p.StartRow,
                    p.StartCol,
                    p.DirRow,
                    p.DirCol
                );
            }

            return Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(sb.ToString()));
        }

        /// <summary>
        /// Decrypts word placements from storage format back to WordPlacementData list.
        /// Supports both old format (H/V direction) and new format (dirRow,dirCol).
        /// </summary>
        /// <param name="encrypted">Base64 encoded placement string</param>
        /// <returns>List of WordPlacementData (empty list on failure or empty input)</returns>
        public static List<WordPlacementData> DecryptWordPlacements(string encrypted)
        {
            List<WordPlacementData> placements = new List<WordPlacementData>();

            if (string.IsNullOrEmpty(encrypted))
            {
                return placements;
            }

            try
            {
                string decoded = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(encrypted));

                // New format: "WORD:row,col,dirRow,dirCol;"
                // Old format: "WORD:row,col,H;" or "WORD:row,col,V;"
                string[] entries = decoded.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);

                foreach (string entry in entries)
                {
                    string[] parts = entry.Split(':');
                    if (parts.Length != 2) continue;

                    string word = parts[0];
                    string[] posDir = parts[1].Split(',');

                    if (posDir.Length == 4)
                    {
                        // New format: row,col,dirRow,dirCol
                        if (int.TryParse(posDir[0], out int row) &&
                            int.TryParse(posDir[1], out int col) &&
                            int.TryParse(posDir[2], out int dirRow) &&
                            int.TryParse(posDir[3], out int dirCol))
                        {
                            placements.Add(new WordPlacementData
                            {
                                Word = word,
                                StartRow = row,
                                StartCol = col,
                                DirRow = dirRow,
                                DirCol = dirCol
                            });
                        }
                    }
                    else if (posDir.Length == 3)
                    {
                        // Old format: row,col,H/V (backwards compatibility)
                        if (int.TryParse(posDir[0], out int row) &&
                            int.TryParse(posDir[1], out int col))
                        {
                            bool horizontal = posDir[2] == "H";

                            placements.Add(new WordPlacementData
                            {
                                Word = word,
                                StartRow = row,
                                StartCol = col,
                                DirRow = horizontal ? 0 : 1,
                                DirCol = horizontal ? 1 : 0
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[GameStateManager] Error decrypting word placements: {ex.Message}");
            }

            return placements;
        }

        // ============================================================
        // MISS LIMIT CALCULATION
        // ============================================================

        /// <summary>
        /// Calculates the miss limit based on grid size, word count, and difficulty.
        /// Formula: 15 + GridBonus + WordModifier + DifficultyModifier
        ///
        /// GridBonus: 6x6=+3, 7x7=+4, 8x8=+6, 9x9=+8, 10x10=+10, 11x11=+12, 12x12=+13
        /// WordModifier: 3 words=+0, 4 words=-2
        /// DifficultyModifier: Easy=+4, Normal=+0, Hard=-4
        /// </summary>
        /// <param name="gridSize">The grid size (6-12)</param>
        /// <param name="wordCount">Number of words (3 or 4)</param>
        /// <param name="difficulty">The difficulty setting</param>
        /// <returns>The calculated miss limit</returns>
        public static int CalculateMissLimit(int gridSize, int wordCount, DifficultySetting difficulty)
        {
            // Grid bonus: 6x6=+3, 7x7=+4, 8x8=+6, 9x9=+8, 10x10=+10, 11x11=+12, 12x12=+13
            int gridBonus = gridSize switch
            {
                6 => 3,
                7 => 4,
                8 => 6,
                9 => 8,
                10 => 10,
                11 => 12,
                12 => 13,
                _ => 0
            };

            // Word modifier: 3 words=+0, 4 words=-2
            int wordModifier = wordCount == 4 ? -2 : 0;

            // Difficulty modifier: Easy=+4, Normal=+0, Hard=-4
            int difficultyModifier = difficulty switch
            {
                DifficultySetting.Easy => 4,
                DifficultySetting.Hard => -4,
                _ => 0
            };

            return 15 + gridBonus + wordModifier + difficultyModifier;
        }

        /// <summary>
        /// Calculates the miss limit using a difficulty string instead of enum.
        /// Converts string to DifficultySetting enum internally.
        /// </summary>
        /// <param name="gridSize">The grid size (6-12)</param>
        /// <param name="wordCount">Number of words (3 or 4)</param>
        /// <param name="difficultyString">Difficulty as string ("Easy", "Normal", "Hard")</param>
        /// <returns>The calculated miss limit</returns>
        public static int CalculateMissLimit(int gridSize, int wordCount, string difficultyString)
        {
            DifficultySetting difficulty = DifficultySetting.Normal;

            if (!string.IsNullOrEmpty(difficultyString))
            {
                if (Enum.TryParse(difficultyString, true, out DifficultySetting parsed))
                {
                    difficulty = parsed;
                }
            }

            return CalculateMissLimit(gridSize, wordCount, difficulty);
        }
    }
}

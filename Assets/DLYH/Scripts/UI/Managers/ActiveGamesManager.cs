// ActiveGamesManager.cs
// Manages the My Active Games list UI on the main menu
// Extracted from UIFlowController during Phase 3 refactoring (Session 4)
// Created: January 19, 2026
// Developer: TecVooDoo LLC

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using Cysharp.Threading.Tasks;
using DLYH.Audio;
using DLYH.Networking.Services;

namespace DLYH.UI.Managers
{
    /// <summary>
    /// Manages the My Active Games list on the main menu.
    /// Handles loading, displaying, and hiding games from Supabase.
    /// Uses callbacks for resume/remove actions that need UIFlowController coordination.
    /// </summary>
    public class ActiveGamesManager
    {
        // ============================================================
        // CONSTANTS
        // ============================================================

        private const string PREFS_HIDDEN_GAMES = "DLYH_HiddenGames";

        // ============================================================
        // UI ELEMENTS
        // ============================================================

        private VisualElement _myGamesSection;
        private VisualElement _myGamesList;
        private VisualElement _mainMenuScreen;

        // ============================================================
        // SERVICES
        // ============================================================

        private GameSessionService _gameSessionService;
        private PlayerService _playerService;

        // ============================================================
        // HIDDEN GAMES (Local persistence for X button removal)
        // ============================================================

        private HashSet<string> _hiddenGameCodes = new HashSet<string>();

        // ============================================================
        // CALLBACKS
        // ============================================================

        /// <summary>
        /// Called when user clicks Resume on a game item.
        /// UIFlowController should handle the actual resume logic.
        /// </summary>
        public Action<string> OnResumeRequested;

        /// <summary>
        /// Called after a game is removed (hidden) from the list.
        /// </summary>
        public Action<string> OnGameRemoved;

        // ============================================================
        // INITIALIZATION
        // ============================================================

        /// <summary>
        /// Initializes the manager with required dependencies.
        /// </summary>
        /// <param name="mainMenuScreen">The main menu screen VisualElement</param>
        /// <param name="gameSessionService">Service for loading games (can be null if offline)</param>
        /// <param name="playerService">Service for player info (can be null if offline)</param>
        public void Initialize(VisualElement mainMenuScreen, GameSessionService gameSessionService, PlayerService playerService)
        {
            _mainMenuScreen = mainMenuScreen ?? throw new ArgumentNullException(nameof(mainMenuScreen));
            _gameSessionService = gameSessionService;
            _playerService = playerService;

            SetupMyActiveGames();
            LoadHiddenGames();
        }

        /// <summary>
        /// Updates service references (called when services become available after init).
        /// </summary>
        public void UpdateServices(GameSessionService gameSessionService, PlayerService playerService)
        {
            _gameSessionService = gameSessionService;
            _playerService = playerService;
        }

        // ============================================================
        // SETUP
        // ============================================================

        private void SetupMyActiveGames()
        {
            // Cache the My Games UI elements
            _myGamesSection = _mainMenuScreen.Q<VisualElement>("my-games-section");
            _myGamesList = _mainMenuScreen.Q<VisualElement>("my-games-list");

            // Initially hidden - will be shown when games are loaded
            if (_myGamesSection != null)
            {
                _myGamesSection.AddToClassList("hidden");
            }
        }

        // ============================================================
        // LOAD AND DISPLAY
        // ============================================================

        /// <summary>
        /// Loads and displays the player's active games from Supabase.
        /// Call this when showing the main menu or after game state changes.
        /// </summary>
        public async UniTask LoadMyActiveGamesAsync()
        {
            if (_gameSessionService == null || _playerService == null)
            {
                Debug.Log("[ActiveGamesManager] Networking services not available - skipping My Active Games");
                HideMyActiveGames();
                return;
            }

            // Need a player record to query games
            if (!_playerService.HasPlayerRecord)
            {
                Debug.Log("[ActiveGamesManager] No player record - skipping My Active Games");
                HideMyActiveGames();
                return;
            }

            string playerId = _playerService.CurrentPlayerId;
            Debug.Log($"[ActiveGamesManager] Loading active games for player {playerId}");

            try
            {
                ActiveGameInfo[] games = await _gameSessionService.GetPlayerGames(playerId);

                if (games == null || games.Length == 0)
                {
                    HideMyActiveGames();
                    return;
                }

                // Filter out hidden games (games the user removed with X button)
                List<ActiveGameInfo> visibleGames = new List<ActiveGameInfo>();
                foreach (ActiveGameInfo game in games)
                {
                    if (!IsGameHidden(game.GameCode))
                    {
                        visibleGames.Add(game);
                    }
                }

                if (visibleGames.Count == 0)
                {
                    HideMyActiveGames();
                    return;
                }

                // Show and populate the list
                ShowMyActiveGames(visibleGames.ToArray());
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ActiveGamesManager] Error loading active games: {ex.Message}");
                HideMyActiveGames();
            }
        }

        private void ShowMyActiveGames(ActiveGameInfo[] games)
        {
            if (_myGamesSection == null || _myGamesList == null) return;

            // Clear existing items
            _myGamesList.Clear();

            // Add game items
            foreach (ActiveGameInfo game in games)
            {
                VisualElement item = CreateMyGameItem(game);
                _myGamesList.Add(item);
            }

            // Show the section
            _myGamesSection.RemoveFromClassList("hidden");
            Debug.Log($"[ActiveGamesManager] Showing {games.Length} active games");
        }

        /// <summary>
        /// Hides the My Active Games section.
        /// </summary>
        public void HideMyActiveGames()
        {
            if (_myGamesSection != null)
            {
                _myGamesSection.AddToClassList("hidden");
            }
        }

        // ============================================================
        // CREATE GAME ITEM
        // ============================================================

        private VisualElement CreateMyGameItem(ActiveGameInfo game)
        {
            VisualElement item = new VisualElement();
            item.AddToClassList("my-game-item");

            // Info section (opponent + status)
            VisualElement info = new VisualElement();
            info.AddToClassList("my-game-info");

            Label opponentLabel = new Label(game.OpponentName != null ? $"vs {game.OpponentName}" : "vs Waiting...");
            opponentLabel.AddToClassList("my-game-opponent");
            info.Add(opponentLabel);

            // Status text
            string statusText = game.WhoseTurn switch
            {
                "your_turn" => $"Code: {game.GameCode} | Your turn",
                "their_turn" => $"Code: {game.GameCode} | Their turn",
                "waiting" => $"Code: {game.GameCode} | Waiting",
                _ => $"Code: {game.GameCode}"
            };

            Label statusLabel = new Label(statusText);
            statusLabel.AddToClassList("my-game-status");
            if (game.WhoseTurn == "your_turn") statusLabel.AddToClassList("your-turn");
            if (game.WhoseTurn == "waiting") statusLabel.AddToClassList("waiting");
            info.Add(statusLabel);

            item.Add(info);

            // Actions section
            VisualElement actions = new VisualElement();
            actions.AddToClassList("my-game-actions");

            Button resumeBtn = new Button(() => HandleResumeGame(game.GameCode));
            resumeBtn.text = "Resume";
            resumeBtn.AddToClassList("my-game-btn");
            resumeBtn.AddToClassList("resume");
            actions.Add(resumeBtn);

            Button removeBtn = new Button(() => HandleRemoveGame(game.GameCode));
            removeBtn.text = "X";
            removeBtn.AddToClassList("my-game-btn");
            removeBtn.AddToClassList("remove");
            actions.Add(removeBtn);

            item.Add(actions);

            return item;
        }

        // ============================================================
        // RESUME GAME
        // ============================================================

        private void HandleResumeGame(string gameCode)
        {
            Debug.Log($"[ActiveGamesManager] Resume game requested: {gameCode}");
            UIAudioManager.ButtonClick();

            // Delegate to UIFlowController via callback
            OnResumeRequested?.Invoke(gameCode);
        }

        // ============================================================
        // REMOVE GAME
        // ============================================================

        private void HandleRemoveGame(string gameCode)
        {
            Debug.Log($"[ActiveGamesManager] Remove game: {gameCode}");
            UIAudioManager.ButtonClick();

            HandleRemoveGameAsync(gameCode).Forget();
        }

        private async UniTask HandleRemoveGameAsync(string gameCode)
        {
            // Instead of deleting from Supabase, just hide the game locally.
            // Player can rejoin by re-entering the game code.
            // The game remains in Supabase for the opponent and for abandonment tracking.
            AddToHiddenGames(gameCode);

            // Notify listeners
            OnGameRemoved?.Invoke(gameCode);

            // Reload the list (which will filter out hidden games)
            await LoadMyActiveGamesAsync();
        }

        // ============================================================
        // HIDDEN GAMES LIST (Local only - for X button removal)
        // ============================================================

        private void LoadHiddenGames()
        {
            _hiddenGameCodes.Clear();
            string saved = PlayerPrefs.GetString(PREFS_HIDDEN_GAMES, "");
            if (!string.IsNullOrEmpty(saved))
            {
                string[] codes = saved.Split(',');
                foreach (string code in codes)
                {
                    if (!string.IsNullOrEmpty(code))
                    {
                        _hiddenGameCodes.Add(code.Trim());
                    }
                }
            }
            Debug.Log($"[ActiveGamesManager] Loaded {_hiddenGameCodes.Count} hidden games");
        }

        private void SaveHiddenGames()
        {
            string joined = string.Join(",", _hiddenGameCodes);
            PlayerPrefs.SetString(PREFS_HIDDEN_GAMES, joined);
            PlayerPrefs.Save();
        }

        private void AddToHiddenGames(string gameCode)
        {
            if (_hiddenGameCodes.Add(gameCode))
            {
                SaveHiddenGames();
                Debug.Log($"[ActiveGamesManager] Hid game {gameCode} from active games list");
            }
        }

        /// <summary>
        /// Removes a game from the hidden list (makes it visible again).
        /// </summary>
        public void RemoveFromHiddenGames(string gameCode)
        {
            if (_hiddenGameCodes.Remove(gameCode))
            {
                SaveHiddenGames();
                Debug.Log($"[ActiveGamesManager] Unhid game {gameCode}");
            }
        }

        /// <summary>
        /// Checks if a game is currently hidden.
        /// </summary>
        public bool IsGameHidden(string gameCode)
        {
            return _hiddenGameCodes.Contains(gameCode);
        }
    }
}

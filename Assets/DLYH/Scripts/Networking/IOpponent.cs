// IOpponent.cs
// Interface for opponent abstraction (AI or Remote player)
// Created: January 4, 2026
// Developer: TecVooDoo LLC

using System;
using System.Collections.Generic;
using UnityEngine;
using Cysharp.Threading.Tasks;
using DLYH.AI.Strategies;
using TecVooDoo.DontLoseYourHead.Core;
using TecVooDoo.DontLoseYourHead.UI;

namespace DLYH.Networking
{
    /// <summary>
    /// Setup data for a player, used during setup phase exchange.
    /// Reuses WordPlacementData from TecVooDoo.DontLoseYourHead.UI.
    /// </summary>
    [Serializable]
    public class PlayerSetupData
    {
        public string PlayerName;
        public Color PlayerColor;
        public int GridSize;
        public int WordCount;
        public DifficultySetting DifficultyLevel;
        public int[] WordLengths;
        public List<WordPlacementData> PlacedWords;

        public PlayerSetupData()
        {
            PlacedWords = new List<WordPlacementData>();
        }
    }

    /// <summary>
    /// Interface for opponent abstraction.
    /// Implemented by both LocalAIOpponent (wraps ExecutionerAI) and RemotePlayerOpponent (network player).
    ///
    /// This interface allows GameplayUIController to work with any type of opponent
    /// without knowing whether it's playing against AI or a remote human player.
    /// </summary>
    public interface IOpponent : IDisposable
    {
        // ============================================================
        // EVENTS (fired when opponent makes a guess)
        // ============================================================

        /// <summary>Fired when opponent starts thinking (for UI feedback)</summary>
        event Action OnThinkingStarted;

        /// <summary>Fired when opponent finishes thinking and is about to act</summary>
        event Action OnThinkingComplete;

        /// <summary>Fired when opponent makes a letter guess. Params: letter</summary>
        event Action<char> OnLetterGuess;

        /// <summary>Fired when opponent makes a coordinate guess. Params: row, col</summary>
        event Action<int, int> OnCoordinateGuess;

        /// <summary>Fired when opponent makes a word guess. Params: word, wordIndex</summary>
        event Action<string, int> OnWordGuess;

        /// <summary>Fired when opponent disconnects (network only, AI never fires this)</summary>
        event Action OnDisconnected;

        /// <summary>Fired when opponent reconnects (network only, AI never fires this)</summary>
        event Action OnReconnected;

        // ============================================================
        // PROPERTIES
        // ============================================================

        /// <summary>Opponent's display name</summary>
        string OpponentName { get; }

        /// <summary>Opponent's color</summary>
        Color OpponentColor { get; }

        /// <summary>Opponent's grid size</summary>
        int GridSize { get; }

        /// <summary>Opponent's word count</summary>
        int WordCount { get; }

        /// <summary>Opponent's word placements (revealed at game end or for validation)</summary>
        List<WordPlacementData> WordPlacements { get; }

        /// <summary>Whether the opponent is currently connected (always true for AI)</summary>
        bool IsConnected { get; }

        /// <summary>Whether the opponent is currently thinking/processing</summary>
        bool IsThinking { get; }

        /// <summary>Whether this is an AI opponent</summary>
        bool IsAI { get; }

        /// <summary>Opponent's calculated miss limit based on local player's grid</summary>
        int MissLimit { get; }

        // ============================================================
        // INITIALIZATION
        // ============================================================

        /// <summary>
        /// Initializes the opponent with the local player's setup data.
        /// For AI: generates opponent setup and prepares strategies.
        /// For Remote: exchanges setup data with remote player.
        /// </summary>
        /// <param name="localPlayerSetup">The local player's setup configuration</param>
        /// <returns>Task that completes when opponent is ready to play</returns>
        UniTask InitializeAsync(PlayerSetupData localPlayerSetup);

        /// <summary>
        /// Resets the opponent for a new game with same settings.
        /// </summary>
        void Reset();

        // ============================================================
        // TURN EXECUTION
        // ============================================================

        /// <summary>
        /// Triggers the opponent's turn.
        /// For AI: executes AI decision-making and fires guess event.
        /// For Remote: waits for remote player's guess via network.
        /// </summary>
        /// <param name="gameState">Current game state for decision making (AI uses this)</param>
        void ExecuteTurn(AIGameState gameState);

        // ============================================================
        // FEEDBACK (notify opponent of results)
        // ============================================================

        /// <summary>
        /// Records the result of the local player's guess.
        /// For AI: updates rubber-banding difficulty.
        /// For Remote: may send to server for state sync.
        /// </summary>
        /// <param name="wasHit">Whether the player's guess was correct</param>
        void RecordPlayerGuess(bool wasHit);

        /// <summary>
        /// Records an opponent hit for tracking.
        /// For AI: updates memory manager.
        /// For Remote: updates local state.
        /// </summary>
        /// <param name="row">Row of the hit</param>
        /// <param name="col">Column of the hit</param>
        void RecordOpponentHit(int row, int col);

        /// <summary>
        /// Records a revealed letter for tracking.
        /// For AI: updates memory manager.
        /// For Remote: updates local state.
        /// </summary>
        /// <param name="letter">The revealed letter</param>
        void RecordRevealedLetter(char letter);

        /// <summary>
        /// Advances the turn counter.
        /// For AI: updates memory turn tracking.
        /// For Remote: may trigger async state sync.
        /// </summary>
        void AdvanceTurn();

        // ============================================================
        // DEBUG
        // ============================================================

        /// <summary>
        /// Gets a debug summary of opponent state.
        /// </summary>
        string GetDebugSummary();
    }
}

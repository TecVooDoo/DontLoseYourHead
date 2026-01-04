// OpponentFactory.cs
// Factory for creating IOpponent instances
// Created: January 4, 2026
// Developer: TecVooDoo LLC

using System.Collections.Generic;
using UnityEngine;
using DLYH.AI.Config;
using TecVooDoo.DontLoseYourHead.Core;

namespace DLYH.Networking
{
    /// <summary>
    /// Factory for creating IOpponent instances.
    /// Provides a centralized place to create either AI or Remote opponents.
    /// </summary>
    public static class OpponentFactory
    {
        /// <summary>
        /// Creates a local AI opponent (The Executioner).
        /// </summary>
        /// <param name="config">ExecutionerAI configuration</param>
        /// <param name="hostGameObject">GameObject to attach the AI component to</param>
        /// <param name="wordLists">Word lists for AI word selection</param>
        /// <returns>A new LocalAIOpponent instance</returns>
        public static IOpponent CreateAIOpponent(
            ExecutionerConfigSO config,
            GameObject hostGameObject,
            Dictionary<int, WordListSO> wordLists)
        {
            return new LocalAIOpponent(config, hostGameObject, wordLists);
        }

        /// <summary>
        /// Creates a remote player opponent (for network multiplayer).
        /// </summary>
        /// <param name="gameCode">The 6-character game code</param>
        /// <param name="isHost">Whether the local player is the host (player 1)</param>
        /// <returns>A new RemotePlayerOpponent instance</returns>
        /// <remarks>
        /// This will be implemented in Phase 5 when we add networking.
        /// For now, returns null as a placeholder.
        /// </remarks>
        public static IOpponent CreateRemoteOpponent(string gameCode, bool isHost)
        {
            // TODO: Implement in Phase 5
            Debug.LogWarning("[OpponentFactory] RemotePlayerOpponent not yet implemented");
            return null;
        }
    }
}

namespace DLYH.TableUI
{
    /// <summary>
    /// Identifies which player or AI owns a cell's content.
    /// Used for determining hit/reveal colors during gameplay.  
    /// </summary>
    public enum CellOwner
    {
        /// <summary>No owner (headers, spacers, empty cells).</summary>
        None,

        /// <summary>Player 1 (local player in single-player, host in multiplayer).</summary>
        Player1,

        /// <summary>Player 2 (opponent in multiplayer).</summary>
        Player2,

        /// <summary>The Executioner AI opponent.</summary>
        ExecutionerAI,

        /// <summary>Phantom AI (fallback when no PVP match found).</summary>
        PhantomAI
    }
}

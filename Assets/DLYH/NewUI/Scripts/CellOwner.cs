namespace DLYH.TableUI
{
    /// <summary>
    /// Identifies which player owns a cell's content. 
    /// Used for determining hit/reveal colors during gameplay.
    /// The game logic does not distinguish between AI and human opponents -
    /// both are simply "Opponent" with a name and color from IOpponent.
    /// </summary>
    public enum CellOwner
    {
        /// <summary>No owner (headers, spacers, empty cells).</summary>
        None,

        /// <summary>The local player (Player 1).</summary>
        Player,

        /// <summary>The opponent (Player 2) - could be AI or human, game doesn't care.</summary>
        Opponent
    }
}

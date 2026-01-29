namespace DLYH.TableUI
{
    /// <summary>
    /// Defines the visual/interaction state of a table cell.
    /// States are mutually exclusive - a cell can only be in one state at a time.
    /// </summary>
    public enum TableCellState
    {
        // === Base States ===

        /// <summary>No specific state.</summary>
        None,

        /// <summary>Default interactive state.</summary>
        Normal,

        /// <summary>Cell cannot be interacted with.</summary>
        Disabled,

        /// <summary>Cell is not visible.</summary>
        Hidden,

        /// <summary>Cell is currently selected.</summary>
        Selected,

        /// <summary>Cell is being hovered over.</summary>
        Hovered,

        /// <summary>Cell is locked and cannot change.</summary>
        Locked,

        /// <summary>Cell is read-only (display only).</summary>
        ReadOnly,

        // === Placement States (Setup Phase) ===

        /// <summary>Valid placement position (green).</summary>
        PlacementValid,

        /// <summary>Invalid placement position (red).</summary>
        PlacementInvalid,

        /// <summary>Path between anchor and current position.</summary>
        PlacementPath,

        /// <summary>First letter anchor point.</summary>
        PlacementAnchor,

        /// <summary>Second letter position (determines direction).</summary>
        PlacementSecond,

        // === Gameplay States ===

        /// <summary>Cell content is hidden (fog of war).</summary>
        Fog,

        /// <summary>Cell has been revealed by coordinate guess (no letter shown on attack grid).</summary>
        Revealed,

        /// <summary>Letter is known but not all coordinates for this letter are known yet (yellow + letter shown).</summary>
        Found,

        /// <summary>Cell was hit and all coordinates for this letter are known (player/opponent color).</summary>
        Hit,

        /// <summary>Coordinate guess missed (no letter).</summary>
        Miss,

        /// <summary>Wrong word was guessed (double penalty).</summary>
        WrongWord,

        /// <summary>Warning state for system messages.</summary>
        Warning
    }
}

using UnityEngine;

namespace DLYH.TableUI
{
    /// <summary>
    /// Defines color rules for the table UI. 
    /// Enforces hard requirements:
    /// - Red/Yellow: System warnings only, never player-selectable
    /// - Green: Setup placement feedback only (PlacementValid)
    /// - Other colors: Player colors, hit/reveal feedback
    /// </summary>
    public static class ColorRules
    {
        // === System Colors (Not Player-Selectable) ===

        /// <summary>System red for errors and invalid placement.</summary>
        public static readonly Color SystemRed = new Color(0.8f, 0.2f, 0.2f, 1f);

        /// <summary>System yellow for warnings.</summary>
        public static readonly Color SystemYellow = new Color(0.9f, 0.8f, 0.2f, 1f);

        /// <summary>System green for valid placement (setup only).</summary>
        public static readonly Color SystemGreen = new Color(0.2f, 0.8f, 0.3f, 1f);

        // === Neutral Colors ===

        /// <summary>Default cell background.</summary>
        public static readonly Color CellDefault = new Color(0.15f, 0.15f, 0.18f, 1f);

        /// <summary>Fog of war overlay.</summary>
        public static readonly Color CellFog = new Color(0.25f, 0.25f, 0.3f, 1f);

        /// <summary>Disabled/non-interactive cell.</summary>
        public static readonly Color CellDisabled = new Color(0.3f, 0.3f, 0.3f, 0.5f);

        /// <summary>Header cell background.</summary>
        public static readonly Color HeaderBackground = new Color(0.2f, 0.2f, 0.25f, 1f);

        /// <summary>Header text color.</summary>
        public static readonly Color HeaderText = new Color(0.8f, 0.8f, 0.8f, 1f);

        /// <summary>Cell revealed but empty (miss).</summary>
        public static readonly Color CellMiss = new Color(0.4f, 0.4f, 0.45f, 1f);

        // === Player-Selectable Colors ===

        /// <summary>
        /// Available colors for player selection.
        /// Excludes red, yellow (system), and very dark colors.
        /// Green is included but won't show green placement feedback.
        /// </summary>
        public static readonly Color[] SelectableColors = new Color[]
        {
            new Color(0.2f, 0.4f, 0.9f, 1f),   // Blue
            new Color(0.6f, 0.2f, 0.8f, 1f),   // Purple
            new Color(0.9f, 0.5f, 0.1f, 1f),   // Orange
            new Color(0.2f, 0.7f, 0.7f, 1f),   // Cyan/Teal
            new Color(0.9f, 0.4f, 0.6f, 1f),   // Pink
            new Color(0.2f, 0.8f, 0.3f, 1f),   // Green (selectable, but placement feedback differs)
            new Color(0.6f, 0.4f, 0.2f, 1f),   // Brown
            new Color(0.9f, 0.9f, 0.9f, 1f),   // White/Silver
        };

        /// <summary>
        /// Names for selectable colors (matches SelectableColors array).
        /// </summary>
        public static readonly string[] SelectableColorNames = new string[]
        {
            "Blue",
            "Purple",
            "Orange",
            "Cyan",
            "Pink",
            "Green",
            "Brown",
            "Silver"
        };

        /// <summary>
        /// Returns true if a color can be selected by players.
        /// Red and Yellow are reserved for system use.
        /// </summary>
        public static bool IsSelectablePlayerColor(Color color)
        {
            // Check against system colors
            if (ColorsApproximatelyEqual(color, SystemRed) ||
                ColorsApproximatelyEqual(color, SystemYellow))
            {
                return false;
            }

            // Check if it's in the selectable list
            for (int i = 0; i < SelectableColors.Length; i++)
            {
                if (ColorsApproximatelyEqual(color, SelectableColors[i]))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Gets the appropriate color for a cell during setup/placement.
        /// </summary>
        public static Color GetPlacementColor(TableCellState state, Color playerColor)
        {
            switch (state)
            {
                case TableCellState.PlacementValid:
                    // If player chose green, use a lighter/different shade for valid feedback
                    if (ColorsApproximatelyEqual(playerColor, SelectableColors[5])) // Green
                    {
                        return new Color(0.4f, 0.9f, 0.5f, 1f); // Lighter green
                    }
                    return SystemGreen;

                case TableCellState.PlacementInvalid:
                    return SystemRed;

                case TableCellState.PlacementPath:
                    return playerColor * 0.7f + CellDefault * 0.3f; // Dimmed player color

                case TableCellState.PlacementAnchor:
                    return playerColor;

                case TableCellState.PlacementSecond:
                    return playerColor * 0.85f + Color.white * 0.15f; // Slightly brighter

                default:
                    return CellDefault;
            }
        }

        /// <summary>
        /// Gets the appropriate color for a cell during gameplay.
        /// </summary>
        public static Color GetGameplayColor(
            TableCellState state,
            CellOwner owner,
            Color player1Color,
            Color player2Color)
        {
            switch (state)
            {
                case TableCellState.Fog:
                    return CellFog;

                case TableCellState.Revealed:
                case TableCellState.Miss:
                    return CellMiss;

                case TableCellState.Hit:
                    return GetOwnerColor(owner, player1Color, player2Color);

                case TableCellState.WrongWord:
                    return SystemRed;

                case TableCellState.Warning:
                    return SystemYellow;

                case TableCellState.Disabled:
                    return CellDisabled;

                case TableCellState.Selected:
                    Color baseColor = GetOwnerColor(owner, player1Color, player2Color);
                    return baseColor * 0.7f + Color.white * 0.3f; // Highlight

                case TableCellState.Hovered:
                    return CellFog * 0.8f + Color.white * 0.2f; // Slight highlight

                default:
                    return CellDefault;
            }
        }

        /// <summary>
        /// Gets the color associated with a cell owner.
        /// </summary>
        public static Color GetOwnerColor(CellOwner owner, Color player1Color, Color player2Color)
        {
            switch (owner)
            {
                case CellOwner.Player1:
                    return player1Color;

                case CellOwner.Player2:
                    return player2Color;

                case CellOwner.ExecutionerAI:
                    // Executioner uses a distinctive color (dark red/crimson)
                    return new Color(0.6f, 0.1f, 0.15f, 1f);

                case CellOwner.PhantomAI:
                    // Phantom AI uses a ghostly color
                    return new Color(0.5f, 0.5f, 0.6f, 1f);

                default:
                    return CellDefault;
            }
        }

        /// <summary>
        /// Gets text color that contrasts with the given background.
        /// </summary>
        public static Color GetContrastingTextColor(Color background)
        {
            // Calculate relative luminance
            float luminance = 0.299f * background.r + 0.587f * background.g + 0.114f * background.b;

            // Use white text on dark backgrounds, dark text on light backgrounds
            return luminance > 0.5f ? Color.black : Color.white;
        }

        /// <summary>
        /// Compares two colors with tolerance for floating point differences.
        /// </summary>
        private static bool ColorsApproximatelyEqual(Color a, Color b, float tolerance = 0.01f)
        {
            return Mathf.Abs(a.r - b.r) < tolerance &&
                   Mathf.Abs(a.g - b.g) < tolerance &&
                   Mathf.Abs(a.b - b.b) < tolerance;
        }
    }
}

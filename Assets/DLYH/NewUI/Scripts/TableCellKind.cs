namespace DLYH.TableUI
{
    /// <summary>
    /// Defines the type of content a table cell represents.  
    /// </summary>
    public enum TableCellKind
    {
        /// <summary>Empty or padding cell.</summary>
        Spacer,

        /// <summary>Letter slot in a word row.</summary>
        WordSlot,

        /// <summary>Column header (A, B, C...).</summary>
        HeaderCol,

        /// <summary>Row header (1, 2, 3...).</summary>
        HeaderRow,

        /// <summary>Playable grid cell.</summary>
        GridCell
    }
}

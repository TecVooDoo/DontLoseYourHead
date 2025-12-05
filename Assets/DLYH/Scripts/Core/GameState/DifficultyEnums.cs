namespace TecVooDoo.DontLoseYourHead.Core
{
    /// <summary>
    /// Available grid sizes for gameplay (6x6 through 12x12)
    /// Value equals the grid dimension
    /// </summary>
    public enum GridSizeOption
    {
        Size6x6 = 6,
        Size7x7 = 7,
        Size8x8 = 8,
        Size9x9 = 9,
        Size10x10 = 10,
        Size11x11 = 11,
        Size12x12 = 12
    }

    /// <summary>
    /// Difficulty setting affects miss limit calculation
    /// Hard/Normal/Easy replaces old Strict/Normal/Forgiving names
    /// </summary>
    public enum DifficultySetting
    {
        Hard,       // Base - 4 misses (challenging)
        Normal,     // Base + 0 misses (balanced)
        Easy        // Base + 4 misses (casual/learning)
    }

    /// <summary>
    /// Number of words each player places
    /// </summary>
    public enum WordCountOption
    {
        Three = 3,  // 3 words: uses 3-4-5 letter words (HARDER - fewer letters)
        Four = 4    // 4 words: uses 3-4-5-6 letter words (EASIER - more letters)
    }
}
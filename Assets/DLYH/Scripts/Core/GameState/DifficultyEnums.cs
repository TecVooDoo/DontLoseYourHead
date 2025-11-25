namespace TecVooDoo.DontLoseYourHead.Core
{
    /// <summary>
    /// Available grid sizes for gameplay
    /// </summary>
    public enum GridSizeOption
    {
        Small = 6,      // 6x6 - Easy (default)
        Medium = 8,     // 8x8 - Medium
        Large = 10      // 10x10 - Hard
    }

    /// <summary>
    /// Forgiveness setting affects miss limit calculation
    /// </summary>
    public enum ForgivenessSetting
    {
        Strict,     // Base - 4 misses (challenging)
        Normal,     // Base + 0 misses (balanced)
        Forgiving   // Base + 4 misses (casual/learning)
    }

    /// <summary>
    /// Number of words each player places
    /// </summary>
    public enum WordCountOption
    {
        Three = 3,  // 3 words: uses 3-4-5 letter words
        Four = 4    // 4 words: uses 3-4-5-6 letter words
    }
}

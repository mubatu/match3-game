namespace Enums
{
    public enum ItemType 
    {
        // Colored items (matchable)
        CubeRed,
        CubeYellow,
        CubeGreen,
        CubeBlue,
        
        // Power-ups
        RocketHorizontal,
        RocketVertical,
        Snitch,
        SnitchLucky,
    }
    
    /// <summary>
    /// Defines the orientation of a match line.
    /// </summary>
    public enum MatchOrientation
    {
        Horizontal,
        Vertical,
        Square // For 2x2 matches that create Snitch
    }
}
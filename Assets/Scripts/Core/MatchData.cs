using System.Collections.Generic;
using Enums;

namespace Core
{
    /// <summary>
    /// Immutable data structure containing information about a detected match.
    /// Now includes orientation and pivot information for power-up spawning.
    /// </summary>
    public class MatchData
    {
        /// <summary>
        /// All items that are part of this match.
        /// </summary>
        public IReadOnlyList<BoardItem> MatchedItems { get; }
        
        /// <summary>
        /// The number of items in this match.
        /// </summary>
        public int Count => MatchedItems.Count;
        
        /// <summary>
        /// The orientation of this match (Horizontal or Vertical).
        /// </summary>
        public MatchOrientation Orientation { get; }
        
        /// <summary>
        /// The pivot position where a power-up should spawn (typically the swap position).
        /// </summary>
        public (int X, int Y) PivotPosition { get; }
        
        /// <summary>
        /// Whether this match qualifies for a rocket power-up (4+ linear items).
        /// </summary>
        public bool IsPowerUpMatch => Count >= 4 && Orientation != MatchOrientation.Square;
        
        /// <summary>
        /// Whether this match qualifies for a Snitch power-up (2x2 square pattern).
        /// </summary>
        public bool IsSnitchMatch => Orientation == MatchOrientation.Square;
        
        public MatchData(List<BoardItem> matchedItems, MatchOrientation orientation, (int X, int Y) pivotPosition)
        {
            MatchedItems = matchedItems.AsReadOnly();
            Orientation = orientation;
            PivotPosition = pivotPosition;
        }
        
        /// <summary>
        /// Legacy constructor for backwards compatibility.
        /// </summary>
        public MatchData(List<BoardItem> matchedItems) 
            : this(matchedItems, MatchOrientation.Horizontal, (0, 0))
        {
        }
    }
}

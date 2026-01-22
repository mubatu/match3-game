using System.Collections.Generic;

namespace Core
{
    /// <summary>
    /// Immutable data structure containing information about a detected match.
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
        
        public MatchData(List<BoardItem> matchedItems)
        {
            MatchedItems = matchedItems.AsReadOnly();
        }
    }
}

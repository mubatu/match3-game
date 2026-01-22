using System.Collections.Generic;
using Enums;

namespace Core
{
    /// <summary>
    /// Detects matches of 3 or more consecutive same-type items on the grid.
    /// Scans horizontally and vertically, merging overlapping matches.
    /// </summary>
    public class MatchDetector
    {
        private readonly GridManager _gridManager;
        private const int MinMatchCount = 3;
        
        public MatchDetector(GridManager gridManager)
        {
            _gridManager = gridManager;
        }
        
        /// <summary>
        /// Finds all matches on the entire grid.
        /// </summary>
        public List<MatchData> FindAllMatches()
        {
            HashSet<BoardItem> allMatchedItems = new HashSet<BoardItem>();
            
            // Scan entire grid
            for (int x = 0; x < _gridManager.Width; x++)
            {
                for (int y = 0; y < _gridManager.Height; y++)
                {
                    FindMatchesAt(x, y, allMatchedItems);
                }
            }
            
            // Convert to single MatchData if any matches found
            List<MatchData> matches = new List<MatchData>();
            if (allMatchedItems.Count > 0)
            {
                matches.Add(new MatchData(new List<BoardItem>(allMatchedItems)));
            }
            
            return matches;
        }
        
        /// <summary>
        /// Finds matches that include the items at the specified positions.
        /// Use this after a swap to check only the affected positions.
        /// </summary>
        public List<MatchData> FindMatchesAtPositions(params (int x, int y)[] positions)
        {
            HashSet<BoardItem> allMatchedItems = new HashSet<BoardItem>();
            
            foreach (var (x, y) in positions)
            {
                FindMatchesAt(x, y, allMatchedItems);
            }
            
            List<MatchData> matches = new List<MatchData>();
            if (allMatchedItems.Count > 0)
            {
                matches.Add(new MatchData(new List<BoardItem>(allMatchedItems)));
            }
            
            return matches;
        }
        
        private void FindMatchesAt(int x, int y, HashSet<BoardItem> matchedItems)
        {
            BoardItem item = _gridManager.GetItemAt(x, y);
            if (item == null) return;
            
            ItemType type = item.Type;
            
            // Find horizontal match
            List<BoardItem> horizontalMatch = GetHorizontalMatch(x, y, type);
            if (horizontalMatch.Count >= MinMatchCount)
            {
                foreach (var matchedItem in horizontalMatch)
                {
                    matchedItems.Add(matchedItem);
                }
            }
            
            // Find vertical match
            List<BoardItem> verticalMatch = GetVerticalMatch(x, y, type);
            if (verticalMatch.Count >= MinMatchCount)
            {
                foreach (var matchedItem in verticalMatch)
                {
                    matchedItems.Add(matchedItem);
                }
            }
        }
        
        private List<BoardItem> GetHorizontalMatch(int startX, int startY, ItemType type)
        {
            List<BoardItem> match = new List<BoardItem>();
            
            // Add the starting item
            BoardItem startItem = _gridManager.GetItemAt(startX, startY);
            if (startItem != null)
            {
                match.Add(startItem);
            }
            
            // Scan left
            for (int x = startX - 1; x >= 0; x--)
            {
                BoardItem item = _gridManager.GetItemAt(x, startY);
                if (item == null || item.Type != type) break;
                match.Add(item);
            }
            
            // Scan right
            for (int x = startX + 1; x < _gridManager.Width; x++)
            {
                BoardItem item = _gridManager.GetItemAt(x, startY);
                if (item == null || item.Type != type) break;
                match.Add(item);
            }
            
            return match;
        }
        
        private List<BoardItem> GetVerticalMatch(int startX, int startY, ItemType type)
        {
            List<BoardItem> match = new List<BoardItem>();
            
            // Add the starting item
            BoardItem startItem = _gridManager.GetItemAt(startX, startY);
            if (startItem != null)
            {
                match.Add(startItem);
            }
            
            // Scan down
            for (int y = startY - 1; y >= 0; y--)
            {
                BoardItem item = _gridManager.GetItemAt(startX, y);
                if (item == null || item.Type != type) break;
                match.Add(item);
            }
            
            // Scan up
            for (int y = startY + 1; y < _gridManager.Height; y++)
            {
                BoardItem item = _gridManager.GetItemAt(startX, y);
                if (item == null || item.Type != type) break;
                match.Add(item);
            }
            
            return match;
        }
    }
}

using System.Collections.Generic;
using System.Linq;
using Enums;

namespace Core
{
    /// <summary>
    /// Detects matches of 3 or more consecutive same-type items on the grid.
    /// Tracks individual match segments with orientation for power-up spawning.
    /// </summary>
    public class MatchDetector
    {
        private readonly GridManager _gridManager;
        private const int MinMatchCount = 3;
        private const int PowerUpMatchCount = 4;
        
        public MatchDetector(GridManager gridManager)
        {
            _gridManager = gridManager;
        }
        
        /// <summary>
        /// Finds all matches on the entire grid.
        /// Returns individual match segments with orientation.
        /// </summary>
        public List<MatchData> FindAllMatches()
        {
            List<MatchData> matches = new List<MatchData>();
            HashSet<BoardItem> processedHorizontal = new HashSet<BoardItem>();
            HashSet<BoardItem> processedVertical = new HashSet<BoardItem>();
            
            // Scan entire grid
            for (int x = 0; x < _gridManager.Width; x++)
            {
                for (int y = 0; y < _gridManager.Height; y++)
                {
                    BoardItem item = _gridManager.GetItemAt(x, y);
                    if (item == null || !IsColoredItem(item.Type)) continue;
                    
                    // Check horizontal match starting from this position
                    if (!processedHorizontal.Contains(item))
                    {
                        var horizontalMatch = GetHorizontalMatchSegment(x, y, item.Type);
                        if (horizontalMatch.Count >= MinMatchCount)
                        {
                            // Use center item as pivot for initial matches
                            var pivot = horizontalMatch[horizontalMatch.Count / 2];
                            matches.Add(new MatchData(horizontalMatch, MatchOrientation.Horizontal, (pivot.X, pivot.Y)));
                            foreach (var matched in horizontalMatch)
                            {
                                processedHorizontal.Add(matched);
                            }
                        }
                    }
                    
                    // Check vertical match starting from this position
                    if (!processedVertical.Contains(item))
                    {
                        var verticalMatch = GetVerticalMatchSegment(x, y, item.Type);
                        if (verticalMatch.Count >= MinMatchCount)
                        {
                            // Use center item as pivot for initial matches
                            var pivot = verticalMatch[verticalMatch.Count / 2];
                            matches.Add(new MatchData(verticalMatch, MatchOrientation.Vertical, (pivot.X, pivot.Y)));
                            foreach (var matched in verticalMatch)
                            {
                                processedVertical.Add(matched);
                            }
                        }
                    }
                }
            }
            
            return matches;
        }
        
        /// <summary>
        /// Finds matches that include the items at the specified positions.
        /// Use this after a swap to check only the affected positions.
        /// The pivot will be set to the swap position for power-up spawning.
        /// </summary>
        public List<MatchData> FindMatchesAtPositions(params (int x, int y)[] positions)
        {
            List<MatchData> matches = new List<MatchData>();
            HashSet<BoardItem> processedHorizontal = new HashSet<BoardItem>();
            HashSet<BoardItem> processedVertical = new HashSet<BoardItem>();
            
            foreach (var (x, y) in positions)
            {
                BoardItem item = _gridManager.GetItemAt(x, y);
                if (item == null || !IsColoredItem(item.Type)) continue;
                
                // Check horizontal match
                if (!processedHorizontal.Contains(item))
                {
                    var horizontalMatch = GetHorizontalMatchSegment(x, y, item.Type);
                    if (horizontalMatch.Count >= MinMatchCount)
                    {
                        // Use the swap position as pivot for power-up spawning
                        matches.Add(new MatchData(horizontalMatch, MatchOrientation.Horizontal, (x, y)));
                        foreach (var matched in horizontalMatch)
                        {
                            processedHorizontal.Add(matched);
                        }
                    }
                }
                
                // Check vertical match
                if (!processedVertical.Contains(item))
                {
                    var verticalMatch = GetVerticalMatchSegment(x, y, item.Type);
                    if (verticalMatch.Count >= MinMatchCount)
                    {
                        // Use the swap position as pivot for power-up spawning
                        matches.Add(new MatchData(verticalMatch, MatchOrientation.Vertical, (x, y)));
                        foreach (var matched in verticalMatch)
                        {
                            processedVertical.Add(matched);
                        }
                    }
                }
            }
            
            return matches;
        }
        
        /// <summary>
        /// Checks if the item type is a colored/matchable item (not a power-up).
        /// </summary>
        private bool IsColoredItem(ItemType type)
        {
            return type == ItemType.CubeRed || 
                   type == ItemType.CubeYellow || 
                   type == ItemType.CubeGreen || 
                   type == ItemType.CubeBlue;
        }
        
        /// <summary>
        /// Gets a horizontal match segment starting from the given position.
        /// Returns items sorted by X coordinate.
        /// </summary>
        private List<BoardItem> GetHorizontalMatchSegment(int startX, int startY, ItemType type)
        {
            List<BoardItem> match = new List<BoardItem>();
            
            // Find the leftmost item of this match
            int leftX = startX;
            while (leftX > 0)
            {
                BoardItem item = _gridManager.GetItemAt(leftX - 1, startY);
                if (item == null || item.Type != type) break;
                leftX--;
            }
            
            // Collect all items from left to right
            for (int x = leftX; x < _gridManager.Width; x++)
            {
                BoardItem item = _gridManager.GetItemAt(x, startY);
                if (item == null || item.Type != type) break;
                match.Add(item);
            }
            
            return match;
        }
        
        /// <summary>
        /// Gets a vertical match segment starting from the given position.
        /// Returns items sorted by Y coordinate.
        /// </summary>
        private List<BoardItem> GetVerticalMatchSegment(int startX, int startY, ItemType type)
        {
            List<BoardItem> match = new List<BoardItem>();
            
            // Find the bottommost item of this match
            int bottomY = startY;
            while (bottomY > 0)
            {
                BoardItem item = _gridManager.GetItemAt(startX, bottomY - 1);
                if (item == null || item.Type != type) break;
                bottomY--;
            }
            
            // Collect all items from bottom to top
            for (int y = bottomY; y < _gridManager.Height; y++)
            {
                BoardItem item = _gridManager.GetItemAt(startX, y);
                if (item == null || item.Type != type) break;
                match.Add(item);
            }
            
            return match;
        }
    }
}

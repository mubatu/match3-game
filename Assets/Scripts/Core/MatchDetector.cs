using System.Collections.Generic;
using System.Linq;
using Enums;

namespace Core
{
    /// <summary>
    /// Detects matches of 3 or more consecutive same-type items on the grid.
    /// Also detects 2x2 square patterns for Snitch power-up creation.
    /// Tracks individual match segments with orientation for power-up spawning.
    /// </summary>
    public class MatchDetector
    {
        private readonly GridManager _gridManager;
        private const int MinMatchCount = 3;
        private const int PowerUpMatchCount = 4;
        private const int SquareMatchSize = 2;
        
        public MatchDetector(GridManager gridManager)
        {
            _gridManager = gridManager;
        }
        
        /// <summary>
        /// Finds all matches on the entire grid.
        /// Returns individual match segments with orientation.
        /// Square (2x2) matches are detected first and have priority over linear matches.
        /// </summary>
        public List<MatchData> FindAllMatches()
        {
            List<MatchData> matches = new List<MatchData>();
            HashSet<BoardItem> processedHorizontal = new HashSet<BoardItem>();
            HashSet<BoardItem> processedVertical = new HashSet<BoardItem>();
            
            // First, detect 2x2 square matches (higher priority for Snitch creation)
            HashSet<BoardItem> squareMatchedItems = new HashSet<BoardItem>();
            var squareMatches = Find2x2Matches();
            foreach (var squareMatch in squareMatches)
            {
                matches.Add(squareMatch);
                foreach (var item in squareMatch.MatchedItems)
                {
                    squareMatchedItems.Add(item);
                }
            }
            
            // Scan entire grid for linear matches
            for (int x = 0; x < _gridManager.Width; x++)
            {
                for (int y = 0; y < _gridManager.Height; y++)
                {
                    BoardItem item = _gridManager.GetItemAt(x, y);
                    if (item == null || !IsColoredItem(item.Type)) continue;
                    
                    // Skip items already part of a square match
                    if (squareMatchedItems.Contains(item)) continue;
                    
                    // Check horizontal match starting from this position
                    if (!processedHorizontal.Contains(item))
                    {
                        var horizontalMatch = GetHorizontalMatchSegment(x, y, item.Type);
                        // Filter out items that are part of square matches
                        horizontalMatch = horizontalMatch.Where(i => !squareMatchedItems.Contains(i)).ToList();
                        
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
                        // Filter out items that are part of square matches
                        verticalMatch = verticalMatch.Where(i => !squareMatchedItems.Contains(i)).ToList();
                        
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
        /// Square (2x2) matches are detected first and have priority.
        /// </summary>
        public List<MatchData> FindMatchesAtPositions(params (int x, int y)[] positions)
        {
            List<MatchData> matches = new List<MatchData>();
            HashSet<BoardItem> processedHorizontal = new HashSet<BoardItem>();
            HashSet<BoardItem> processedVertical = new HashSet<BoardItem>();
            
            // First, detect 2x2 square matches around the swap positions
            HashSet<BoardItem> squareMatchedItems = new HashSet<BoardItem>();
            var squareMatches = Find2x2MatchesAtPositions(positions);
            foreach (var squareMatch in squareMatches)
            {
                matches.Add(squareMatch);
                foreach (var item in squareMatch.MatchedItems)
                {
                    squareMatchedItems.Add(item);
                }
            }
            
            foreach (var (x, y) in positions)
            {
                BoardItem item = _gridManager.GetItemAt(x, y);
                if (item == null || !IsColoredItem(item.Type)) continue;
                
                // Skip items already part of a square match
                if (squareMatchedItems.Contains(item)) continue;
                
                // Check horizontal match
                if (!processedHorizontal.Contains(item))
                {
                    var horizontalMatch = GetHorizontalMatchSegment(x, y, item.Type);
                    // Filter out items that are part of square matches
                    horizontalMatch = horizontalMatch.Where(i => !squareMatchedItems.Contains(i)).ToList();
                    
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
                    // Filter out items that are part of square matches
                    verticalMatch = verticalMatch.Where(i => !squareMatchedItems.Contains(i)).ToList();
                    
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
        /// Finds all 2x2 square matches on the entire grid.
        /// Each cell is checked as the bottom-left corner of a potential 2x2 square.
        /// </summary>
        private List<MatchData> Find2x2Matches()
        {
            List<MatchData> squareMatches = new List<MatchData>();
            HashSet<(int, int)> processedSquares = new HashSet<(int, int)>();
            
            // For each cell (x,y), check if it's the bottom-left of a 2x2 square
            for (int x = 0; x < _gridManager.Width - 1; x++)
            {
                for (int y = 0; y < _gridManager.Height - 1; y++)
                {
                    var squareMatch = TryGet2x2MatchAt(x, y);
                    if (squareMatch != null && !processedSquares.Contains((x, y)))
                    {
                        squareMatches.Add(squareMatch);
                        processedSquares.Add((x, y));
                    }
                }
            }
            
            return squareMatches;
        }
        
        /// <summary>
        /// Finds 2x2 square matches that involve the specified positions.
        /// Checks all possible 2x2 squares that could include any of the given positions.
        /// Uses the swap position as pivot for power-up spawning.
        /// </summary>
        private List<MatchData> Find2x2MatchesAtPositions((int x, int y)[] positions)
        {
            List<MatchData> squareMatches = new List<MatchData>();
            HashSet<(int, int)> checkedCorners = new HashSet<(int, int)>();
            
            foreach (var (px, py) in positions)
            {
                // Check all 4 possible 2x2 squares that could contain this position
                // Position could be: bottom-left, bottom-right, top-left, or top-right of a square
                (int, int)[] possibleCorners = new[]
                {
                    (px, py),       // position is bottom-left
                    (px - 1, py),   // position is bottom-right
                    (px, py - 1),   // position is top-left
                    (px - 1, py - 1) // position is top-right
                };
                
                foreach (var (cornerX, cornerY) in possibleCorners)
                {
                    if (checkedCorners.Contains((cornerX, cornerY))) continue;
                    if (cornerX < 0 || cornerY < 0) continue;
                    
                    checkedCorners.Add((cornerX, cornerY));
                    
                    // Use swap position as pivot for Snitch spawning
                    var squareMatch = TryGet2x2MatchAt(cornerX, cornerY, (px, py));
                    if (squareMatch != null)
                    {
                        squareMatches.Add(squareMatch);
                    }
                }
            }
            
            return squareMatches;
        }
        
        /// <summary>
        /// Attempts to get a 2x2 match starting at the given bottom-left corner.
        /// Returns null if no valid 2x2 match exists at this position.
        /// </summary>
        /// <param name="x">Bottom-left X coordinate of the square</param>
        /// <param name="y">Bottom-left Y coordinate of the square</param>
        /// <param name="pivotOverride">Optional pivot position override (e.g., swap position)</param>
        private MatchData TryGet2x2MatchAt(int x, int y, (int X, int Y)? pivotOverride = null)
        {
            // Check bounds - need space for 2x2 square
            if (x + 1 >= _gridManager.Width || y + 1 >= _gridManager.Height) return null;
            
            var bottomLeft = _gridManager.GetItemAt(x, y);
            var bottomRight = _gridManager.GetItemAt(x + 1, y);
            var topLeft = _gridManager.GetItemAt(x, y + 1);
            var topRight = _gridManager.GetItemAt(x + 1, y + 1);
            
            // All 4 positions must have items
            if (bottomLeft == null || bottomRight == null || 
                topLeft == null || topRight == null) return null;
            
            // Only colored items can form square matches
            if (!IsColoredItem(bottomLeft.Type)) return null;
            
            // All 4 must be the same type
            if (bottomLeft.Type != bottomRight.Type ||
                bottomLeft.Type != topLeft.Type ||
                bottomLeft.Type != topRight.Type) return null;
            
            // Valid 2x2 match found - use pivot override if provided, otherwise bottom-left
            var pivot = pivotOverride ?? (x, y);
            var items = new List<BoardItem> { bottomLeft, bottomRight, topLeft, topRight };
            return new MatchData(items, MatchOrientation.Square, pivot);
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

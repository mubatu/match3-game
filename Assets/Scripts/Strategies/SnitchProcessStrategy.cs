using System.Collections.Generic;
using Core;
using Enums;
using Items;
using UnityEngine;

namespace Strategies
{
    /// <summary>
    /// Strategy for Snitch power-ups. When executed, blasts adjacent cells
    /// plus random cells (1 for regular Snitch, 2 for SnitchLucky).
    /// </summary>
    public class SnitchProcessStrategy : IProcessStrategy
    {
        public void Execute(BoardItem item, GridManager gridManager)
        {
            if (item is not Snitch snitch) return;
            
            List<BoardItem> itemsToBlast = CollectItemsToBlast(snitch, gridManager);
            
            if (itemsToBlast.Count > 0)
            {
                // Create a match data representing the snitch blast
                var blastData = new MatchData(
                    itemsToBlast,
                    MatchOrientation.Square,
                    (snitch.X, snitch.Y)
                );
                
                // Fire the snitch blast event
                GameEvents.SnitchBlast(blastData, snitch);
            }
        }
        
        /// <summary>
        /// Collects all items that should be blasted by the Snitch.
        /// Includes: the Snitch itself, 4 adjacent cells, and 1-2 random cells.
        /// </summary>
        private List<BoardItem> CollectItemsToBlast(Snitch snitch, GridManager gridManager)
        {
            List<BoardItem> items = new List<BoardItem>();
            HashSet<(int, int)> collectedPositions = new HashSet<(int, int)>();
            
            // Add the Snitch itself
            items.Add(snitch);
            collectedPositions.Add((snitch.X, snitch.Y));
            
            // Add 4 adjacent cells (up, down, left, right)
            (int dx, int dy)[] adjacentOffsets = { (0, 1), (0, -1), (-1, 0), (1, 0) };
            foreach (var (dx, dy) in adjacentOffsets)
            {
                int newX = snitch.X + dx;
                int newY = snitch.Y + dy;
                
                BoardItem adjacent = gridManager.GetItemAt(newX, newY);
                if (adjacent != null && !collectedPositions.Contains((newX, newY)))
                {
                    items.Add(adjacent);
                    collectedPositions.Add((newX, newY));
                }
            }
            
            // Add random cells (1 for regular Snitch, 2 for SnitchLucky)
            int randomCellCount = snitch.IsLucky ? 2 : 1;
            var randomItems = CollectRandomItems(gridManager, collectedPositions, randomCellCount);
            items.AddRange(randomItems);
            
            return items;
        }
        
        /// <summary>
        /// Collects random items from the grid that haven't been collected yet.
        /// </summary>
        private List<BoardItem> CollectRandomItems(GridManager gridManager, HashSet<(int, int)> excludePositions, int count)
        {
            List<BoardItem> randomItems = new List<BoardItem>();
            List<BoardItem> availableItems = new List<BoardItem>();
            
            // Gather all available items that aren't already collected
            for (int x = 0; x < gridManager.Width; x++)
            {
                for (int y = 0; y < gridManager.Height; y++)
                {
                    if (excludePositions.Contains((x, y))) continue;
                    
                    BoardItem item = gridManager.GetItemAt(x, y);
                    if (item != null)
                    {
                        availableItems.Add(item);
                    }
                }
            }
            
            // Randomly select items
            for (int i = 0; i < count && availableItems.Count > 0; i++)
            {
                int randomIndex = Random.Range(0, availableItems.Count);
                BoardItem selectedItem = availableItems[randomIndex];
                
                randomItems.Add(selectedItem);
                excludePositions.Add((selectedItem.X, selectedItem.Y));
                availableItems.RemoveAt(randomIndex);
            }
            
            return randomItems;
        }
    }
}

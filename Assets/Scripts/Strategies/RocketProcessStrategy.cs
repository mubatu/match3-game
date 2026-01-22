using System.Collections.Generic;
using Core;
using Enums;
using Items;

namespace Strategies
{
    /// <summary>
    /// Strategy for rocket power-ups. When executed, collects all items
    /// in the rocket's row (horizontal) or column (vertical) and triggers their blast.
    /// </summary>
    public class RocketProcessStrategy : IProcessStrategy
    {
        public void Execute(BoardItem item, GridManager gridManager)
        {
            if (item is not Rocket rocket) return;
            
            List<BoardItem> itemsToBlast = CollectItemsInPath(rocket, gridManager);
            
            if (itemsToBlast.Count > 0)
            {
                // Create a match data representing the rocket blast
                var blastData = new MatchData(
                    itemsToBlast, 
                    rocket.Orientation, 
                    (rocket.X, rocket.Y)
                );
                
                // Fire the rocket blast event
                GameEvents.RocketBlast(blastData, rocket);
            }
        }
        
        /// <summary>
        /// Collects all items in the rocket's blast path (row or column).
        /// </summary>
        private List<BoardItem> CollectItemsInPath(Rocket rocket, GridManager gridManager)
        {
            List<BoardItem> items = new List<BoardItem>();
            
            if (rocket.Orientation == MatchOrientation.Horizontal)
            {
                // Collect all items in the row
                for (int x = 0; x < gridManager.Width; x++)
                {
                    BoardItem item = gridManager.GetItemAt(x, rocket.Y);
                    if (item != null)
                    {
                        items.Add(item);
                    }
                }
            }
            else // Vertical
            {
                // Collect all items in the column
                for (int y = 0; y < gridManager.Height; y++)
                {
                    BoardItem item = gridManager.GetItemAt(rocket.X, y);
                    if (item != null)
                    {
                        items.Add(item);
                    }
                }
            }
            
            return items;
        }
    }
}

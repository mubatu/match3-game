using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Enums;
using UnityEngine;

namespace Core
{
    /// <summary>
    /// Handles the destruction of matched items and power-up creation.
    /// Orchestrates blast animations, rocket spawning, and notifies when complete.
    /// </summary>
    public class BlastHandler : MonoBehaviour
    {
        [SerializeField] private float blastDuration = 0.2f;
        [SerializeField] private float rocketBlastCollectionDelay = 0.05f;
        [SerializeField] private GridManager gridManager;
        
        private int _pendingBlasts;
        private Queue<MatchData> _pendingMatches = new Queue<MatchData>();
        private Queue<(MatchData blastData, BoardItem rocket)> _pendingRocketBlasts = new Queue<(MatchData, BoardItem)>();
        private Queue<(MatchData blastData, BoardItem snitch)> _pendingSnitchBlasts = new Queue<(MatchData, BoardItem)>();
        private bool _isProcessing;
        
        private void OnEnable()
        {
            GameEvents.OnMatchFound += HandleMatchFound;
            GameEvents.OnRocketBlast += HandleRocketBlast;
            GameEvents.OnSnitchBlast += HandleSnitchBlast;
        }
        
        private void OnDisable()
        {
            GameEvents.OnMatchFound -= HandleMatchFound;
            GameEvents.OnRocketBlast -= HandleRocketBlast;
            GameEvents.OnSnitchBlast -= HandleSnitchBlast;
        }
        
        private void HandleMatchFound(MatchData matchData)
        {
            if (matchData == null || matchData.Count == 0) return;
            
            _pendingMatches.Enqueue(matchData);
            
            if (!_isProcessing)
            {
                StartCoroutine(ProcessAllMatches());
            }
        }
        
        private void HandleRocketBlast(MatchData blastData, BoardItem rocket)
        {
            if (blastData == null || blastData.Count == 0) return;
            
            _pendingRocketBlasts.Enqueue((blastData, rocket));
            
            if (!_isProcessing)
            {
                StartCoroutine(ProcessAllRocketBlasts());
            }
        }
        
        private void HandleSnitchBlast(MatchData blastData, BoardItem snitch)
        {
            if (blastData == null || blastData.Count == 0) return;
            
            _pendingSnitchBlasts.Enqueue((blastData, snitch));
            
            if (!_isProcessing)
            {
                StartCoroutine(ProcessAllSnitchBlasts());
            }
        }
        
        private IEnumerator ProcessAllMatches()
        {
            _isProcessing = true;
            
            // Collect all items to blast and power-ups to spawn
            HashSet<BoardItem> allItemsToBlast = new HashSet<BoardItem>();
            List<(int x, int y, MatchOrientation orientation)> rocketsToSpawn = new List<(int, int, MatchOrientation)>();
            List<(int x, int y)> snitchesToSpawn = new List<(int, int)>();
            
            while (_pendingMatches.Count > 0)
            {
                var matchData = _pendingMatches.Dequeue();
                
                if (matchData.IsSnitchMatch)
                {
                    // 2x2 square match: spawn Snitch at pivot, blast all 4 items
                    if (!snitchesToSpawn.Exists(s => s.x == matchData.PivotPosition.X && s.y == matchData.PivotPosition.Y))
                    {
                        snitchesToSpawn.Add((matchData.PivotPosition.X, matchData.PivotPosition.Y));
                    }
                    
                    foreach (var item in matchData.MatchedItems)
                    {
                        allItemsToBlast.Add(item);
                    }
                }
                else if (matchData.IsPowerUpMatch)
                {
                    // 4+ linear match: spawn rocket at pivot, blast other items
                    var pivotItem = gridManager.GetItemAt(matchData.PivotPosition.X, matchData.PivotPosition.Y);
                    
                    foreach (var item in matchData.MatchedItems)
                    {
                        // Don't blast the pivot position - a rocket will spawn there
                        if (item.X == matchData.PivotPosition.X && item.Y == matchData.PivotPosition.Y)
                        {
                            // Mark for rocket spawn
                            if (!rocketsToSpawn.Any(r => r.x == item.X && r.y == item.Y))
                            {
                                rocketsToSpawn.Add((item.X, item.Y, matchData.Orientation));
                            }
                        }
                        allItemsToBlast.Add(item);
                    }
                }
                else
                {
                    // Regular 3-match: blast all items
                    foreach (var item in matchData.MatchedItems)
                    {
                        allItemsToBlast.Add(item);
                    }
                }
            }
            
            // Remove rocket spawn positions from blast list
            foreach (var rocket in rocketsToSpawn)
            {
                var itemAtRocketPos = allItemsToBlast.FirstOrDefault(i => i.X == rocket.x && i.Y == rocket.y);
                if (itemAtRocketPos != null)
                {
                    allItemsToBlast.Remove(itemAtRocketPos);
                }
            }
            
            // Remove Snitch spawn positions from blast list
            foreach (var snitch in snitchesToSpawn)
            {
                var itemAtSnitchPos = allItemsToBlast.FirstOrDefault(i => i.X == snitch.x && i.Y == snitch.y);
                if (itemAtSnitchPos != null)
                {
                    allItemsToBlast.Remove(itemAtSnitchPos);
                }
            }
            
            // Blast all items except power-up spawn positions
            if (allItemsToBlast.Count > 0)
            {
                yield return StartCoroutine(BlastItems(allItemsToBlast.ToList()));
            }
            
            // Spawn rockets at their positions
            foreach (var (x, y, orientation) in rocketsToSpawn)
            {
                // Remove existing item at position if any
                var existingItem = gridManager.GetItemAt(x, y);
                if (existingItem != null)
                {
                    gridManager.RemoveItem(existingItem);
                    Destroy(existingItem.gameObject);
                }
                
                gridManager.SpawnRocket(x, y, orientation);
            }
            
            // Spawn Snitches at their positions
            foreach (var (x, y) in snitchesToSpawn)
            {
                // Remove existing item at position if any
                var existingItem = gridManager.GetItemAt(x, y);
                if (existingItem != null)
                {
                    gridManager.RemoveItem(existingItem);
                    Destroy(existingItem.gameObject);
                }
                
                gridManager.SpawnSnitch(x, y);
            }
            
            _isProcessing = false;
            GameEvents.BlastCompleted();
        }
        
        /// <summary>
        /// Processes all queued rocket blasts with chain reaction support.
        /// Waits briefly to collect multiple simultaneous rocket activations (e.g., rocket-rocket swap).
        /// </summary>
        private IEnumerator ProcessAllRocketBlasts()
        {
            _isProcessing = true;
            
            // Brief delay to collect multiple simultaneous rocket activations
            yield return new WaitForSeconds(rocketBlastCollectionDelay);
            
            // Track all items that have been processed to avoid duplicates
            HashSet<BoardItem> processedItems = new HashSet<BoardItem>();
            
            // Process all queued rockets and their chain reactions (including Snitch chains)
            while (_pendingRocketBlasts.Count > 0 || _pendingSnitchBlasts.Count > 0)
            {
                // Process rocket blasts
                while (_pendingRocketBlasts.Count > 0)
                {
                    var (blastData, rocket) = _pendingRocketBlasts.Dequeue();
                    
                    // Collect items to blast that haven't been processed yet
                    List<BoardItem> itemsToBlast = new List<BoardItem>();
                    List<BoardItem> chainRockets = new List<BoardItem>();
                    List<BoardItem> chainSnitches = new List<BoardItem>();
                    
                    foreach (var item in blastData.MatchedItems)
                    {
                        if (item == null || processedItems.Contains(item)) continue;
                        
                        processedItems.Add(item);
                        
                        // Check if this item is a power-up (for chain reaction)
                        if (item != rocket)
                        {
                            if (item.Type == ItemType.RocketHorizontal || item.Type == ItemType.RocketVertical)
                            {
                                chainRockets.Add(item);
                            }
                            else if (item.Type == ItemType.Snitch || item.Type == ItemType.SnitchLucky)
                            {
                                chainSnitches.Add(item);
                            }
                        }
                        
                        itemsToBlast.Add(item);
                    }
                    
                    // Queue chain rockets before blasting (capture their blast paths while they still exist in grid)
                    foreach (var chainRocket in chainRockets)
                    {
                        var chainBlastItems = CollectRocketBlastPath(chainRocket);
                        if (chainBlastItems.Count > 0)
                        {
                            var chainBlastData = new MatchData(
                                chainBlastItems,
                                chainRocket.Type == ItemType.RocketHorizontal ? MatchOrientation.Horizontal : MatchOrientation.Vertical,
                                (chainRocket.X, chainRocket.Y)
                            );
                            _pendingRocketBlasts.Enqueue((chainBlastData, chainRocket));
                        }
                    }
                    
                    // Queue chain snitches
                    foreach (var chainSnitch in chainSnitches)
                    {
                        var chainBlastItems = CollectSnitchBlastPath(chainSnitch);
                        if (chainBlastItems.Count > 0)
                        {
                            var chainBlastData = new MatchData(
                                chainBlastItems,
                                MatchOrientation.Square,
                                (chainSnitch.X, chainSnitch.Y)
                            );
                            _pendingSnitchBlasts.Enqueue((chainBlastData, chainSnitch));
                        }
                    }
                    
                    // Blast current batch of items
                    if (itemsToBlast.Count > 0)
                    {
                        yield return StartCoroutine(BlastItems(itemsToBlast));
                    }
                }
                
                // Process any snitch blasts triggered by rocket chain reactions
                while (_pendingSnitchBlasts.Count > 0)
                {
                    var (blastData, snitch) = _pendingSnitchBlasts.Dequeue();
                    
                    List<BoardItem> itemsToBlast = new List<BoardItem>();
                    List<BoardItem> chainRockets = new List<BoardItem>();
                    List<BoardItem> chainSnitches = new List<BoardItem>();
                    
                    foreach (var item in blastData.MatchedItems)
                    {
                        if (item == null || processedItems.Contains(item)) continue;
                        
                        processedItems.Add(item);
                        
                        if (item != snitch)
                        {
                            if (item.Type == ItemType.RocketHorizontal || item.Type == ItemType.RocketVertical)
                            {
                                chainRockets.Add(item);
                            }
                            else if (item.Type == ItemType.Snitch || item.Type == ItemType.SnitchLucky)
                            {
                                chainSnitches.Add(item);
                            }
                        }
                        
                        itemsToBlast.Add(item);
                    }
                    
                    foreach (var chainRocket in chainRockets)
                    {
                        var chainBlastItems = CollectRocketBlastPath(chainRocket);
                        if (chainBlastItems.Count > 0)
                        {
                            var chainBlastData = new MatchData(
                                chainBlastItems,
                                chainRocket.Type == ItemType.RocketHorizontal ? MatchOrientation.Horizontal : MatchOrientation.Vertical,
                                (chainRocket.X, chainRocket.Y)
                            );
                            _pendingRocketBlasts.Enqueue((chainBlastData, chainRocket));
                        }
                    }
                    
                    foreach (var chainSnitch in chainSnitches)
                    {
                        var chainBlastItems = CollectSnitchBlastPath(chainSnitch);
                        if (chainBlastItems.Count > 0)
                        {
                            var chainBlastData = new MatchData(
                                chainBlastItems,
                                MatchOrientation.Square,
                                (chainSnitch.X, chainSnitch.Y)
                            );
                            _pendingSnitchBlasts.Enqueue((chainBlastData, chainSnitch));
                        }
                    }
                    
                    if (itemsToBlast.Count > 0)
                    {
                        yield return StartCoroutine(BlastItems(itemsToBlast));
                    }
                }
            }
            
            _isProcessing = false;
            GameEvents.BlastCompleted();
        }
        
        /// <summary>
        /// Processes all queued Snitch blasts with chain reaction support.
        /// Handles chain reactions when Snitch hits rockets or other Snitches.
        /// </summary>
        private IEnumerator ProcessAllSnitchBlasts()
        {
            _isProcessing = true;
            
            // Brief delay to collect multiple simultaneous activations
            yield return new WaitForSeconds(rocketBlastCollectionDelay);
            
            // Track all items that have been processed to avoid duplicates
            HashSet<BoardItem> processedItems = new HashSet<BoardItem>();
            
            // Process all queued snitches and their chain reactions
            while (_pendingSnitchBlasts.Count > 0 || _pendingRocketBlasts.Count > 0)
            {
                // Process Snitch blasts first
                while (_pendingSnitchBlasts.Count > 0)
                {
                    var (blastData, snitch) = _pendingSnitchBlasts.Dequeue();
                    
                    List<BoardItem> itemsToBlast = new List<BoardItem>();
                    List<BoardItem> chainRockets = new List<BoardItem>();
                    List<BoardItem> chainSnitches = new List<BoardItem>();
                    
                    foreach (var item in blastData.MatchedItems)
                    {
                        if (item == null || processedItems.Contains(item)) continue;
                        
                        processedItems.Add(item);
                        
                        // Check for chain reactions with other power-ups
                        if (item != snitch)
                        {
                            if (item.Type == ItemType.RocketHorizontal || item.Type == ItemType.RocketVertical)
                            {
                                chainRockets.Add(item);
                            }
                            else if (item.Type == ItemType.Snitch || item.Type == ItemType.SnitchLucky)
                            {
                                chainSnitches.Add(item);
                            }
                        }
                        
                        itemsToBlast.Add(item);
                    }
                    
                    // Queue chain rockets
                    foreach (var chainRocket in chainRockets)
                    {
                        var chainBlastItems = CollectRocketBlastPath(chainRocket);
                        if (chainBlastItems.Count > 0)
                        {
                            var chainBlastData = new MatchData(
                                chainBlastItems,
                                chainRocket.Type == ItemType.RocketHorizontal ? MatchOrientation.Horizontal : MatchOrientation.Vertical,
                                (chainRocket.X, chainRocket.Y)
                            );
                            _pendingRocketBlasts.Enqueue((chainBlastData, chainRocket));
                        }
                    }
                    
                    // Queue chain snitches
                    foreach (var chainSnitch in chainSnitches)
                    {
                        var chainBlastItems = CollectSnitchBlastPath(chainSnitch);
                        if (chainBlastItems.Count > 0)
                        {
                            var chainBlastData = new MatchData(
                                chainBlastItems,
                                MatchOrientation.Square,
                                (chainSnitch.X, chainSnitch.Y)
                            );
                            _pendingSnitchBlasts.Enqueue((chainBlastData, chainSnitch));
                        }
                    }
                    
                    // Blast current batch of items
                    if (itemsToBlast.Count > 0)
                    {
                        yield return StartCoroutine(BlastItems(itemsToBlast));
                    }
                }
                
                // Process any rocket blasts triggered by snitch chain reactions
                while (_pendingRocketBlasts.Count > 0)
                {
                    var (blastData, rocket) = _pendingRocketBlasts.Dequeue();
                    
                    List<BoardItem> itemsToBlast = new List<BoardItem>();
                    List<BoardItem> chainRockets = new List<BoardItem>();
                    List<BoardItem> chainSnitches = new List<BoardItem>();
                    
                    foreach (var item in blastData.MatchedItems)
                    {
                        if (item == null || processedItems.Contains(item)) continue;
                        
                        processedItems.Add(item);
                        
                        if (item != rocket)
                        {
                            if (item.Type == ItemType.RocketHorizontal || item.Type == ItemType.RocketVertical)
                            {
                                chainRockets.Add(item);
                            }
                            else if (item.Type == ItemType.Snitch || item.Type == ItemType.SnitchLucky)
                            {
                                chainSnitches.Add(item);
                            }
                        }
                        
                        itemsToBlast.Add(item);
                    }
                    
                    // Queue chain rockets
                    foreach (var chainRocket in chainRockets)
                    {
                        var chainBlastItems = CollectRocketBlastPath(chainRocket);
                        if (chainBlastItems.Count > 0)
                        {
                            var chainBlastData = new MatchData(
                                chainBlastItems,
                                chainRocket.Type == ItemType.RocketHorizontal ? MatchOrientation.Horizontal : MatchOrientation.Vertical,
                                (chainRocket.X, chainRocket.Y)
                            );
                            _pendingRocketBlasts.Enqueue((chainBlastData, chainRocket));
                        }
                    }
                    
                    // Queue chain snitches
                    foreach (var chainSnitch in chainSnitches)
                    {
                        var chainBlastItems = CollectSnitchBlastPath(chainSnitch);
                        if (chainBlastItems.Count > 0)
                        {
                            var chainBlastData = new MatchData(
                                chainBlastItems,
                                MatchOrientation.Square,
                                (chainSnitch.X, chainSnitch.Y)
                            );
                            _pendingSnitchBlasts.Enqueue((chainBlastData, chainSnitch));
                        }
                    }
                    
                    if (itemsToBlast.Count > 0)
                    {
                        yield return StartCoroutine(BlastItems(itemsToBlast));
                    }
                }
            }
            
            _isProcessing = false;
            GameEvents.BlastCompleted();
        }
        
        /// <summary>
        /// Collects items for a Snitch's blast path (adjacents + random).
        /// Used for chain reactions when a Snitch is hit by another power-up.
        /// </summary>
        private List<BoardItem> CollectSnitchBlastPath(BoardItem snitch)
        {
            List<BoardItem> items = new List<BoardItem>();
            HashSet<(int, int)> collectedPositions = new HashSet<(int, int)>();
            
            // Add the Snitch itself
            items.Add(snitch);
            collectedPositions.Add((snitch.X, snitch.Y));
            
            // Add 4 adjacent cells
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
            int randomCellCount = (snitch.Type == ItemType.SnitchLucky) ? 2 : 1;
            
            List<BoardItem> availableItems = new List<BoardItem>();
            for (int x = 0; x < gridManager.Width; x++)
            {
                for (int y = 0; y < gridManager.Height; y++)
                {
                    if (collectedPositions.Contains((x, y))) continue;
                    BoardItem item = gridManager.GetItemAt(x, y);
                    if (item != null)
                    {
                        availableItems.Add(item);
                    }
                }
            }
            
            for (int i = 0; i < randomCellCount && availableItems.Count > 0; i++)
            {
                int randomIndex = Random.Range(0, availableItems.Count);
                BoardItem selectedItem = availableItems[randomIndex];
                items.Add(selectedItem);
                collectedPositions.Add((selectedItem.X, selectedItem.Y));
                availableItems.RemoveAt(randomIndex);
            }
            
            return items;
        }
        
        /// <summary>
        /// Collects all items in a rocket's blast path (row or column).
        /// </summary>
        private List<BoardItem> CollectRocketBlastPath(BoardItem rocket)
        {
            List<BoardItem> items = new List<BoardItem>();
            
            if (rocket.Type == ItemType.RocketHorizontal)
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
            else if (rocket.Type == ItemType.RocketVertical)
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

        private IEnumerator BlastItems(IReadOnlyList<BoardItem> items)
        {
            _pendingBlasts = items.Count;
            
            // Notify that items are being blasted (for scoring, effects, etc.)
            GameEvents.ItemsBlasted(items);
            
            // Remove items from grid first (before destruction)
            foreach (var item in items)
            {
                gridManager.RemoveItem(item);
            }
            
            // Start blast animation on all items simultaneously
            foreach (var item in items)
            {
                if (item != null)
                {
                    item.Blast(blastDuration, OnItemBlastComplete);
                }
                else
                {
                    _pendingBlasts--;
                }
            }
            
            // Wait until all blasts complete
            while (_pendingBlasts > 0)
            {
                yield return null;
            }
        }
        
        private void OnItemBlastComplete()
        {
            _pendingBlasts--;
        }
    }
}

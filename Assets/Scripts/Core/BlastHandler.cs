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
        private bool _isProcessing;
        
        private void OnEnable()
        {
            GameEvents.OnMatchFound += HandleMatchFound;
            GameEvents.OnRocketBlast += HandleRocketBlast;
        }
        
        private void OnDisable()
        {
            GameEvents.OnMatchFound -= HandleMatchFound;
            GameEvents.OnRocketBlast -= HandleRocketBlast;
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
        
        private IEnumerator ProcessAllMatches()
        {
            _isProcessing = true;
            
            // Collect all items to blast and rockets to spawn
            HashSet<BoardItem> allItemsToBlast = new HashSet<BoardItem>();
            List<(int x, int y, MatchOrientation orientation)> rocketsToSpawn = new List<(int, int, MatchOrientation)>();
            
            while (_pendingMatches.Count > 0)
            {
                var matchData = _pendingMatches.Dequeue();
                
                if (matchData.IsPowerUpMatch)
                {
                    // 4+ match: spawn rocket at pivot, blast other items
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
            
            // Blast all items except rocket spawn positions
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
            
            // Process all queued rockets and their chain reactions
            while (_pendingRocketBlasts.Count > 0)
            {
                var (blastData, rocket) = _pendingRocketBlasts.Dequeue();
                
                // Collect items to blast that haven't been processed yet
                List<BoardItem> itemsToBlast = new List<BoardItem>();
                List<BoardItem> chainRockets = new List<BoardItem>();
                
                foreach (var item in blastData.MatchedItems)
                {
                    if (item == null || processedItems.Contains(item)) continue;
                    
                    processedItems.Add(item);
                    
                    // Check if this item is a rocket (for chain reaction)
                    if (item != rocket && (item.Type == ItemType.RocketHorizontal || item.Type == ItemType.RocketVertical))
                    {
                        chainRockets.Add(item);
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
                
                // Blast current batch of items
                if (itemsToBlast.Count > 0)
                {
                    yield return StartCoroutine(BlastItems(itemsToBlast));
                }
            }
            
            _isProcessing = false;
            GameEvents.BlastCompleted();
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

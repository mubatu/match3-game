using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Core
{
    /// <summary>
    /// Handles gravity logic after items are blasted.
    /// Scans columns for empty cells and moves items down to fill gaps.
    /// </summary>
    public class GravityHandler : MonoBehaviour
    {
        [SerializeField] private GridManager gridManager;
        [SerializeField] private float fallDuration = 0.3f;
        [SerializeField] private float fallDelayPerRow = 0.05f;
        
        private int _pendingFalls;
        
        private void OnEnable()
        {
            GameEvents.OnBlastCompleted += HandleBlastCompleted;
        }
        
        private void OnDisable()
        {
            GameEvents.OnBlastCompleted -= HandleBlastCompleted;
        }
        
        private void HandleBlastCompleted()
        {
            StartCoroutine(ApplyGravity());
        }
        
        /// <summary>
        /// Applies gravity to all columns, making items fall to fill empty spaces.
        /// </summary>
        private IEnumerator ApplyGravity()
        {
            GameEvents.GravityStarted();
            
            List<FallData> fallOperations = CalculateFallOperations();
            
            if (fallOperations.Count == 0)
            {
                // No items need to fall
                GameEvents.GravityCompleted();
                yield break;
            }
            
            _pendingFalls = fallOperations.Count;
            
            // Group falls by source row for staggered animation
            var fallsBySourceRow = GroupFallsBySourceRow(fallOperations);
            
            // Execute falls with stagger delay per row
            foreach (var rowGroup in fallsBySourceRow)
            {
                foreach (var fall in rowGroup.Value)
                {
                    ExecuteFall(fall);
                }
                
                // Small delay before next row starts falling
                if (fallDelayPerRow > 0)
                {
                    yield return new WaitForSeconds(fallDelayPerRow);
                }
            }
            
            // Wait for all falls to complete
            while (_pendingFalls > 0)
            {
                yield return null;
            }
            
            GameEvents.GravityCompleted();
        }
        
        /// <summary>
        /// Calculates all fall operations needed for the current board state.
        /// Scans each column bottom-up to find empty cells and items above them.
        /// </summary>
        private List<FallData> CalculateFallOperations()
        {
            List<FallData> fallOperations = new List<FallData>();
            
            int width = gridManager.Width;
            int height = gridManager.Height;
            
            // Process each column independently
            for (int x = 0; x < width; x++)
            {
                int emptyCount = 0;
                
                // Scan column from bottom to top
                for (int y = 0; y < height; y++)
                {
                    BoardItem item = gridManager.GetItemAt(x, y);
                    
                    if (item == null)
                    {
                        // Found an empty cell, increment counter
                        emptyCount++;
                    }
                    else if (emptyCount > 0)
                    {
                        // Found an item with empty cells below it
                        int targetY = y - emptyCount;
                        
                        fallOperations.Add(new FallData
                        {
                            Item = item,
                            FromX = x,
                            FromY = y,
                            ToX = x,
                            ToY = targetY,
                            FallDistance = emptyCount
                        });
                    }
                }
            }
            
            return fallOperations;
        }
        
        /// <summary>
        /// Groups fall operations by their source row for staggered animation.
        /// </summary>
        private SortedDictionary<int, List<FallData>> GroupFallsBySourceRow(List<FallData> fallOperations)
        {
            var groups = new SortedDictionary<int, List<FallData>>();
            
            foreach (var fall in fallOperations)
            {
                if (!groups.ContainsKey(fall.FromY))
                {
                    groups[fall.FromY] = new List<FallData>();
                }
                groups[fall.FromY].Add(fall);
            }
            
            return groups;
        }
        
        /// <summary>
        /// Executes a single fall operation - updates grid and animates item.
        /// </summary>
        private void ExecuteFall(FallData fall)
        {
            // Update grid array: clear old position, set new position
            UpdateGridPosition(fall.Item, fall.FromX, fall.FromY, fall.ToX, fall.ToY);
            
            // Update item's internal coordinates
            fall.Item.SetGridPosition(fall.ToX, fall.ToY);
            
            // Calculate target world position and animate
            Vector3 targetPosition = gridManager.GetWorldPosition(fall.ToX, fall.ToY);
            
            // Duration scales with fall distance for more natural feel
            float adjustedDuration = fallDuration + (fall.FallDistance - 1) * 0.05f;
            
            StartCoroutine(AnimateFall(fall.Item, targetPosition, adjustedDuration));
        }
        
        /// <summary>
        /// Updates the grid array to reflect the item's new position.
        /// </summary>
        private void UpdateGridPosition(BoardItem item, int fromX, int fromY, int toX, int toY)
        {
            // Use reflection or add a method to GridManager for direct grid access
            // For now, we need to add a SetItemAt method to GridManager
            gridManager.SetItemAt(toX, toY, item);
            gridManager.ClearItemAt(fromX, fromY);
        }
        
        /// <summary>
        /// Animates an item falling to its target position.
        /// </summary>
        private IEnumerator AnimateFall(BoardItem item, Vector3 targetPosition, float duration)
        {
            item.MoveTo(targetPosition, duration);
            
            // Wait for movement to complete
            while (item != null && item.IsMoving)
            {
                yield return null;
            }
            
            _pendingFalls--;
        }
        
        /// <summary>
        /// Data structure to hold information about a single fall operation.
        /// </summary>
        private struct FallData
        {
            public BoardItem Item;
            public int FromX;
            public int FromY;
            public int ToX;
            public int ToY;
            public int FallDistance;
        }
    }
}

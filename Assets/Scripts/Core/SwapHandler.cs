using System.Collections.Generic;
using UnityEngine;

namespace Core
{
    /// <summary>
    /// Handles the swap operation between two adjacent board items.
    /// Orchestrates validation, animation, grid updates, and match detection.
    /// </summary>
    public class SwapHandler : MonoBehaviour
    {
        [SerializeField] private float swapDuration = 0.25f;
        [SerializeField] private GridManager gridManager;
        
        private bool _isSwapping;
        private MatchDetector _matchDetector;
        
        private void Awake()
        {
            _matchDetector = new MatchDetector(gridManager);
        }
        
        private void OnEnable()
        {
            GameEvents.OnSwapRequested += HandleSwapRequest;
        }
        
        private void OnDisable()
        {
            GameEvents.OnSwapRequested -= HandleSwapRequest;
        }
        
        private void HandleSwapRequest(BoardItem itemA, BoardItem itemB)
        {
            if (_isSwapping) return;
            
            if (!ValidateSwap(itemA, itemB))
            {
                Debug.LogWarning($"Invalid swap attempted: ({itemA.X},{itemA.Y}) to ({itemB.X},{itemB.Y})");
                return;
            }
            
            ExecuteSwap(itemA, itemB);
        }
        
        private bool ValidateSwap(BoardItem itemA, BoardItem itemB)
        {
            if (itemA == null || itemB == null) return false;
            if (itemA.IsMoving || itemB.IsMoving) return false;
            if (!gridManager.AreAdjacent(itemA, itemB)) return false;
            
            return true;
        }
        
        private void ExecuteSwap(BoardItem itemA, BoardItem itemB)
        {
            _isSwapping = true;
            
            // Cache target positions before updating grid
            Vector3 positionA = gridManager.GetWorldPosition(itemA.X, itemA.Y);
            Vector3 positionB = gridManager.GetWorldPosition(itemB.X, itemB.Y);
            
            // Update the grid array and item coordinates
            gridManager.SwapItemsInGrid(itemA, itemB);
            
            // Animate both items to their new positions
            itemA.MoveTo(positionB, swapDuration);
            itemB.MoveTo(positionA, swapDuration);
            
            // Start monitoring for swap completion
            StartCoroutine(WaitForSwapCompletion(itemA, itemB));
        }
        
        private System.Collections.IEnumerator WaitForSwapCompletion(BoardItem itemA, BoardItem itemB)
        {
            // Wait until both items finish moving
            while (itemA.IsMoving || itemB.IsMoving)
            {
                yield return null;
            }
            
            GameEvents.SwapCompleted(itemA, itemB);
            
            // Check for matches at both swapped positions
            List<MatchData> matches = _matchDetector.FindMatchesAtPositions(
                (itemA.X, itemA.Y),
                (itemB.X, itemB.Y)
            );
            
            if (matches.Count > 0)
            {
                // Valid swap - matches found
                foreach (var match in matches)
                {
                    GameEvents.MatchFound(match);
                }
            }
            else
            {
                // Invalid swap - no matches, revert
                yield return StartCoroutine(RevertSwap(itemA, itemB));
            }
            
            _isSwapping = false;
        }
        
        private System.Collections.IEnumerator RevertSwap(BoardItem itemA, BoardItem itemB)
        {
            // Cache positions before reverting
            Vector3 positionA = gridManager.GetWorldPosition(itemA.X, itemA.Y);
            Vector3 positionB = gridManager.GetWorldPosition(itemB.X, itemB.Y);
            
            // Swap back in grid
            gridManager.SwapItemsInGrid(itemA, itemB);
            
            // Animate back
            itemA.MoveTo(positionB, swapDuration);
            itemB.MoveTo(positionA, swapDuration);
            
            // Wait for animation
            while (itemA.IsMoving || itemB.IsMoving)
            {
                yield return null;
            }
            
            Debug.Log("Swap reverted - no matches found");
        }
    }
}
    


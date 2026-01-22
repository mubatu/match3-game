using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Core
{
    /// <summary>
    /// Handles the destruction of matched items.
    /// Orchestrates blast animations and notifies when complete.
    /// </summary>
    public class BlastHandler : MonoBehaviour
    {
        [SerializeField] private float blastDuration = 0.2f;
        [SerializeField] private GridManager gridManager;
        
        private int _pendingBlasts;
        
        private void OnEnable()
        {
            GameEvents.OnMatchFound += HandleMatchFound;
        }
        
        private void OnDisable()
        {
            GameEvents.OnMatchFound -= HandleMatchFound;
        }
        
        private void HandleMatchFound(MatchData matchData)
        {
            if (matchData == null || matchData.Count == 0) return;
            
            StartCoroutine(BlastItems(matchData.MatchedItems));
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
            
            GameEvents.BlastCompleted();
        }
        
        private void OnItemBlastComplete()
        {
            _pendingBlasts--;
        }
    }
}

using System.Collections;
using UnityEngine;
using Enums;

namespace Core
{
    /// <summary>
    /// Orchestrates the game loop and handles cascade detection after gravity/refill.
    /// Manages game state transitions and ensures proper sequencing of phases.
    /// </summary>
    public class CascadeController : MonoBehaviour
    {
        [SerializeField] private GridManager gridManager;
        [SerializeField] private float cascadeCheckDelay = 0.1f;
        [SerializeField] private int maxCascadeDepth = 50;
        
        private GameState _currentState = GameState.Idle;
        private int _cascadeCount;
        
        public GameState CurrentState => _currentState;
        
        private void OnEnable()
        {
            GameEvents.OnSwapRequested += HandleSwapRequested;
            GameEvents.OnSwapCompleted += HandleSwapCompleted;
            GameEvents.OnMatchFound += HandleMatchFound;
            GameEvents.OnBlastCompleted += HandleBlastCompleted;
            GameEvents.OnGravityStarted += HandleGravityStarted;
            GameEvents.OnGravityCompleted += HandleGravityCompleted;
            GameEvents.OnRefillStarted += HandleRefillStarted;
            GameEvents.OnRefillCompleted += HandleRefillCompleted;
        }
        
        private void OnDisable()
        {
            GameEvents.OnSwapRequested -= HandleSwapRequested;
            GameEvents.OnSwapCompleted -= HandleSwapCompleted;
            GameEvents.OnMatchFound -= HandleMatchFound;
            GameEvents.OnBlastCompleted -= HandleBlastCompleted;
            GameEvents.OnGravityStarted -= HandleGravityStarted;
            GameEvents.OnGravityCompleted -= HandleGravityCompleted;
            GameEvents.OnRefillStarted -= HandleRefillStarted;
            GameEvents.OnRefillCompleted -= HandleRefillCompleted;
        }
        
        /// <summary>
        /// Returns whether the game is in a state that accepts player input.
        /// </summary>
        public bool CanAcceptInput()
        {
            return _currentState == GameState.Idle;
        }
        
        private void SetState(GameState newState)
        {
            if (_currentState == newState) return;
            
            _currentState = newState;
            GameEvents.GameStateChanged(newState);
            
            Debug.Log($"[CascadeController] State changed to: {newState}");
        }
        
        private void HandleSwapRequested(BoardItem from, BoardItem to)
        {
            if (_currentState != GameState.Idle)
            {
                Debug.LogWarning("[CascadeController] Swap rejected - game not idle");
                return;
            }
            
            SetState(GameState.Swapping);
            _cascadeCount = 0;
        }
        
        private void HandleSwapCompleted(BoardItem itemA, BoardItem itemB)
        {
            // Swap handler will check for matches
            // State transitions happen based on match results
        }
        
        private void HandleMatchFound(MatchData matchData)
        {
            SetState(GameState.Blasting);
        }
        
        private void HandleBlastCompleted()
        {
            // Gravity handler will pick up from here
            // State transition happens in HandleGravityStarted
        }
        
        private void HandleGravityStarted()
        {
            SetState(GameState.Gravity);
        }
        
        private void HandleGravityCompleted()
        {
            // Refill handler will pick up from here
            // State transition happens in HandleRefillStarted
        }
        
        private void HandleRefillStarted()
        {
            SetState(GameState.Refilling);
        }
        
        private void HandleRefillCompleted()
        {
            StartCoroutine(CheckForCascadeMatches());
        }
        
        /// <summary>
        /// Checks for new matches after gravity/refill and triggers cascade if found.
        /// </summary>
        private IEnumerator CheckForCascadeMatches()
        {
            SetState(GameState.Cascading);
            
            // Small delay to let animations settle
            yield return new WaitForSeconds(cascadeCheckDelay);
            
            _cascadeCount++;
            
            if (_cascadeCount > maxCascadeDepth)
            {
                Debug.LogWarning($"[CascadeController] Max cascade depth ({maxCascadeDepth}) reached!");
                SetState(GameState.Idle);
                yield break;
            }
            
            // Check for new matches
            gridManager.CheckAndBlastMatches();
            
            // If no matches were found, CheckAndBlastMatches won't fire OnMatchFound
            // We need to detect this and return to idle
            yield return new WaitForSeconds(0.1f);
            
            // If state is still Cascading, no matches were found
            if (_currentState == GameState.Cascading)
            {
                Debug.Log($"[CascadeController] No cascade matches. Total cascades: {_cascadeCount}");
                SetState(GameState.Idle);
            }
        }
    }
}

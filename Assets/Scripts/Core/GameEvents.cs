using System;
using System.Collections.Generic;
using Enums;

namespace Core
{
    /// <summary>
    /// Static event bus for decoupled communication between game systems.
    /// </summary>
    public static class GameEvents
    {
        // Input events
        public static event Action<int, int> OnItemClicked;
        
        // Swap events
        public static event Action<BoardItem> OnDragStarted;
        public static event Action<BoardItem, BoardItem> OnSwapRequested;
        public static event Action<BoardItem, BoardItem> OnSwapCompleted;
        
        // Match events
        public static event Action<MatchData> OnMatchFound;
        public static event Action<IReadOnlyList<BoardItem>> OnItemsBlasted;
        public static event Action OnBlastCompleted;
        
        // Rocket events
        public static event Action<MatchData, BoardItem> OnRocketBlast;
        
        // Snitch events
        public static event Action<MatchData, BoardItem> OnSnitchBlast;
        
        // Gravity events
        public static event Action OnGravityStarted;
        public static event Action OnGravityCompleted;
        
        // Refill events
        public static event Action OnRefillStarted;
        public static event Action OnRefillCompleted;
        
        // Game state events
        public static event Action<GameState> OnGameStateChanged;
        
        // Raise methods - Input
        public static void ItemClicked(int x, int y) => OnItemClicked?.Invoke(x, y);
        public static void DragStarted(BoardItem item) => OnDragStarted?.Invoke(item);
        
        // Raise methods - Swap
        public static void SwapRequested(BoardItem from, BoardItem to) => OnSwapRequested?.Invoke(from, to);
        public static void SwapCompleted(BoardItem itemA, BoardItem itemB) => OnSwapCompleted?.Invoke(itemA, itemB);
        
        // Raise methods - Match
        public static void MatchFound(MatchData matchData) => OnMatchFound?.Invoke(matchData);
        public static void ItemsBlasted(IReadOnlyList<BoardItem> items) => OnItemsBlasted?.Invoke(items);
        public static void BlastCompleted() => OnBlastCompleted?.Invoke();
        
        // Raise methods - Rocket
        public static void RocketBlast(MatchData blastData, BoardItem rocket) => OnRocketBlast?.Invoke(blastData, rocket);
        
        // Raise methods - Snitch
        public static void SnitchBlast(MatchData blastData, BoardItem snitch) => OnSnitchBlast?.Invoke(blastData, snitch);
        
        // Raise methods - Gravity
        public static void GravityStarted() => OnGravityStarted?.Invoke();
        public static void GravityCompleted() => OnGravityCompleted?.Invoke();
        
        // Raise methods - Refill
        public static void RefillStarted() => OnRefillStarted?.Invoke();
        public static void RefillCompleted() => OnRefillCompleted?.Invoke();
        
        // Raise methods - Game State
        public static void GameStateChanged(GameState newState) => OnGameStateChanged?.Invoke(newState);
    }
}
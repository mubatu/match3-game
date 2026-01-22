using System;
using System.Collections.Generic;

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
        
        // Raise methods
        public static void ItemClicked(int x, int y) => OnItemClicked?.Invoke(x, y);
        public static void DragStarted(BoardItem item) => OnDragStarted?.Invoke(item);
        public static void SwapRequested(BoardItem from, BoardItem to) => OnSwapRequested?.Invoke(from, to);
        public static void SwapCompleted(BoardItem itemA, BoardItem itemB) => OnSwapCompleted?.Invoke(itemA, itemB);
        public static void MatchFound(MatchData matchData) => OnMatchFound?.Invoke(matchData);
        public static void ItemsBlasted(IReadOnlyList<BoardItem> items) => OnItemsBlasted?.Invoke(items);
        public static void BlastCompleted() => OnBlastCompleted?.Invoke();
    }
}
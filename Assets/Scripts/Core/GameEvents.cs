using System;
using UnityEditor;

namespace Core
{
    public class GameEvents
    {
        // Input events
        public static event Action<int, int> OnItemClicked;
        
        // Raise methods
        public static void ItemClicked(int x, int y) => OnItemClicked?.Invoke(x, y);
    }
}
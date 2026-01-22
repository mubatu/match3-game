using Core;
using UnityEngine;

namespace Strategies
{
    public class MatchProcessStrategy : IProcessStrategy
    {
        public void Execute(BoardItem item, GridManager gridManager)
        {
            Debug.Log($"Processing Match logic for {item.Type} at ({item.X}, {item.Y})");
        }
    }
}
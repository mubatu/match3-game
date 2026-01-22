using Enums;
using UnityEngine;
using Strategies;

namespace Core
{
    public abstract class BoardItem : MonoBehaviour
    {
        [SerializeField] public ItemType Type;
        public int X { get; private set; }
        public int Y { get; private set; }
        protected IProcessStrategy _processStrategy;
        
        public void Initialize(int x, int y)
        {
            X = x;
            Y = y;
            SetupStrategy();
        }
        protected abstract void SetupStrategy();
        public void CallStrategy(GridManager gridManager)
        {
            _processStrategy?.Execute(this, gridManager);
        }
    }
}
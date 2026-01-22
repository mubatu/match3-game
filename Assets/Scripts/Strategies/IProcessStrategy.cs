using Core;

namespace Strategies
{
    public interface IProcessStrategy
    {
        // The contract for all strategies
        void Execute(BoardItem item, GridManager gridManager);
    }   
}
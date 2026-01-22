using Core;
using Strategies;

namespace Items
{
    public class Cube : BoardItem
    {
        protected override void SetupStrategy()
        {
            // All cubes use the match strategy
            _processStrategy = new MatchProcessStrategy();
        }
    }
}
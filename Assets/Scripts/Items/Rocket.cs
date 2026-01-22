using Core;
using Enums;
using Strategies;

namespace Items
{
    /// <summary>
    /// Rocket power-up that destroys an entire row or column when activated.
    /// Horizontal rockets destroy the row, vertical rockets destroy the column.
    /// </summary>
    public class Rocket : BoardItem
    {
        /// <summary>
        /// The orientation of this rocket (determines blast direction).
        /// </summary>
        public MatchOrientation Orientation => Type == ItemType.RocketHorizontal 
            ? MatchOrientation.Horizontal 
            : MatchOrientation.Vertical;
        
        protected override void SetupStrategy()
        {
            _processStrategy = new RocketProcessStrategy();
        }
    }
}

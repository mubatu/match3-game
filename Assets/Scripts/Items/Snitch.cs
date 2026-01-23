using Core;
using Enums;
using Strategies;

namespace Items
{
    /// <summary>
    /// Snitch power-up created by matching 4 items in a 2x2 square pattern.
    /// Has two variants: regular Snitch and SnitchLucky (50% chance each).
    /// Blast behavior will be implemented later.
    /// </summary>
    public class Snitch : BoardItem
    {
        /// <summary>
        /// Whether this is a lucky Snitch variant.
        /// </summary>
        public bool IsLucky => Type == ItemType.SnitchLucky;
        
        protected override void SetupStrategy()
        {
            _processStrategy = new SnitchProcessStrategy();
        }
    }
}

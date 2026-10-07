using System;
using System.Collections.Generic;
using System.Text;

namespace SnowFlakeSharpId
{
    /// <summary>
    /// Defines what happens when the system clock goes back in time.
    /// </summary>
    public enum ClockBackwardsPolicy
    {
        /// <summary>Throws ClockMovedBackwardsException (default).</summary>
        Throw = 0,

        /// <summary>
        /// Keeps generating IDs from the last known timestamp until the
        /// real clock catches up. Never throws because of the clock.
        /// </summary>
        Continue = 1
    }
}

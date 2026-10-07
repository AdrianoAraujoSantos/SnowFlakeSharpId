using System;
using System.Collections.Generic;
using System.Text;

namespace SnowFlakeSharpId
{
    public sealed class ClockMovedBackwardsException : Exception
    {
        public long OffsetMs { get; }
        public ClockMovedBackwardsException(long offsetMs)
       : base($"The clock went back. {offsetMs}ms.") => OffsetMs = offsetMs;
    }
}

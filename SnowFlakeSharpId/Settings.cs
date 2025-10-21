using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SnowFlakeSharpId
{
    public class Settings
    {
        public uint? MachineID { get; set; }
        public uint? DataCenterID { get; set; }

        public DateTimeOffset? CustomDate { get; set; }

        #region Bits for each part of the ID

        public int? MachineIdBits { get; set; } = 5;

        public int? DataCenterIdBits { get; set; } = 5;

        public int? SequenceBits { get; set; }=12;

        #endregion
    }
}

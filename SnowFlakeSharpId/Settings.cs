using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SnowFlakeSharpId
{
    /// <summary>
    /// Class for Snowflake Instance Settings.
    /// </summary>
    public class Settings
    {
        /// <summary>
        /// Gets or sets the machine ID for the Snowflake instance. 
        /// </summary>
        public uint? MachineID { get; set; }

        /// <summary>
        /// Gets or sets the data center ID for the Snowflake instance. 
        /// </summary>
        public uint? DataCenterID { get; set; }

        /// <summary>
        /// Gets or sets the custom epoch for the Snowflake instance. 
        /// </summary>
        public DateTimeOffset? CustomDate { get; set; }

        #region Bits for each part of the ID

        /// <summary>
        /// Gets or sets the number of bits for the machine ID. For example, if set to 5, the machine ID will have a maximum value of 31.
        /// </summary>
        public int? MachineIdBits { get; set; } = 5;

        /// <summary>
        /// Gets or sets the number of bits for the data center ID. For example, if set to 5, the data center ID will have a maximum value of 31.
        /// </summary>
        public int? DataCenterIdBits { get; set; } = 5;

        /// <summary>
        /// Gets or sets the number of bits for the sequence. For example, if set to 12, the sequence will have a maximum value of 4095.
        /// </summary>
        public int? SequenceBits { get; set; }=12;

        #endregion
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SnowFlakeSharpId
{
    public class SnowflakeId
    {
        // The epoch (in milliseconds) to generate the ID.
        private long Epoch = 1735689600000L; // January 1, 2025, 00:00:00 UTC


        private  long MaxMachineId= long.MaxValue;
        private  long MaxDataCenterId= long.MaxValue;
        private  long MaxSequence= long.MaxValue;
        private  int MachineIdShift=int.MaxValue;
        private  int DataCenterIdShift=int.MaxValue;
        private  int TimestampShift=int.MaxValue;
     

        private readonly uint _machineId;
        private readonly uint _datacenterId;
        private long _lastTimestamp = -1L;
        private long _sequence = 0L;
        private int _machineIdBits = 0;
        private int _sequenceBits = 0;
        private readonly object _lock = new object();

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="settings"></param>
        /// <exception cref="Exception"></exception>
        /// <exception cref="ArgumentException"></exception>
        public SnowflakeId(Settings settings = null)
        {
            settings= settings ?? new Settings();

            // Masks to ensure values ​​stay within limits
            MaxMachineId = (-1L ^ (-1L << settings?.MachineIdBits??0)); // 31
            MaxDataCenterId = -1L ^ (-1L << settings?.DataCenterIdBits??0); // 31
            MaxSequence = -1L ^ (-1L << settings?.SequenceBits??0); // 4095

            // Shifts to position each part in the 64-bit ID
            MachineIdShift = (settings?.SequenceBits??0); // 12
            DataCenterIdShift = (settings?.SequenceBits??0) + (settings?.MachineIdBits??0); // 17
            TimestampShift = (settings?.SequenceBits??0) + (settings?.MachineIdBits??0) + (settings?.DataCenterIdBits??0); // 22


            if (settings?.CustomDate != null && settings.CustomDate.Value.UtcDateTime >= DateTimeOffset.UtcNow)
            {
                throw new Exception($"Custom epoch must be earlier than the start time. Provided custom epoch: {settings.CustomDate}, start time: {DateTimeOffset.UtcNow}.");
            }
            else
            {

                Epoch = settings?.CustomDate != null? settings?.CustomDate?.ToUnixTimeMilliseconds() ?? 0: Epoch;
            }

            _lastTimestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - Epoch;

            if ((settings?.MachineID??0)  > MaxMachineId || (settings?.MachineID??0) < 0)
            {
                throw new ArgumentException($"Machine ID cannot be greater than {MaxMachineId} or less than 0.");
            }

            if ((settings?.DataCenterID??0) > MaxDataCenterId || (settings?.DataCenterID??0) < 0)
            {
                throw new ArgumentException($"Datacenter ID cannot be greater than {MaxDataCenterId} oor less than 0.");
            }

            _machineId = settings?.MachineID ?? 0;
            _datacenterId = settings?.DataCenterID ?? 0;
            _machineIdBits = settings?.MachineIdBits ?? 0;
            _sequenceBits = settings?.SequenceBits ?? 0;

        }

        /// <summary>
        /// Generate a new ID
        /// </summary>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public long NextID()
        {
            lock (_lock)
            {
                long timestamp = GetCurrentTimestamp();

                if (timestamp < _lastTimestamp)
                {
                    // Treatment for clocks that go back in time.
                    throw new Exception($"The system clock has gone back in time. Unable to generate IDs for {(_lastTimestamp - timestamp)}ms.");
                }


                if (_lastTimestamp == timestamp)
                {
                    //If the same millisecond, increment the sequence
                    _sequence = (_sequence + 1) & MaxSequence;
                    if (_sequence == 0)
                    {
                        // The sequence has burst, wait for the next millisecond
                        timestamp = WaitNextMillis(_lastTimestamp);
                    }
                }
                else
                {
                    // Millisecond different, restart the sequence
                    _sequence = 0L;
                }

                _lastTimestamp = timestamp;

                // Combine the parts to form the final ID
                long id = (timestamp - Epoch) << TimestampShift |
                          (_datacenterId << DataCenterIdShift) |
                          (_machineId << MachineIdShift) |
                          _sequence;

                return id;
            }
        }
        /// <summary>
        /// Decode ID to parts to return to the data that generated the Id
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public (long Timestamp,uint DataCenterID ,uint MachineID, uint Sequence) DecodeID(long id)
        {
            long timestamp = (id >> TimestampShift) + Epoch;
            uint datacenterId = (uint)((id >> DataCenterIdShift) & MaxDataCenterId);
            uint machineId = (uint)((id >> MachineIdShift) & MaxMachineId);
            uint sequence = (uint)(id & MaxSequence);
            return (timestamp,datacenterId,machineId, sequence);
        }
        /// <summary>
        /// Converts a timestamp in milliseconds to a DateTime object
        /// </summary>
        /// <param name="timestamp"></param>
        /// <returns></returns>
        public DateTime TimestampToDateTime(long timestamp)
        {
            return DateTimeOffset.FromUnixTimeMilliseconds(timestamp).DateTime;
        }

        // Wait until the next millisecond
        private long WaitNextMillis(long lastTimestamp)
        {
            long timestamp = GetCurrentTimestamp();
            while (timestamp <= lastTimestamp)
            {
                timestamp = GetCurrentTimestamp();
            }
            return timestamp;
        }

        // Gets the current timestamp in milliseconds
        private long GetCurrentTimestamp()
        {
            return DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        }
    }
}



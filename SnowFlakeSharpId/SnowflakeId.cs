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
        private readonly object _lock = new object();
        private readonly Func<long> _timeSource;
        private readonly ClockBackwardsPolicy _clockPolicy;

        public SnowflakeId(Settings? settings = null)
        : this(settings, () => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds())
        {
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="settings"></param>
        /// <exception cref="Exception"></exception>
        /// <exception cref="ArgumentException"></exception>
        internal SnowflakeId(Settings? settings, Func<long> timeSource)
        {
            _timeSource = timeSource;
            settings ??= new Settings();

            var machineBits = settings.MachineIdBits ?? 5;
            var dataCenterBits = settings.DataCenterIdBits ?? 5;
            var sequenceBits = settings.SequenceBits ?? 12;

            if (!Enum.IsDefined(typeof(ClockBackwardsPolicy), settings.ClockBackwardsPolicy))
                throw new ArgumentException("Invalid ClockBackwardsPolicy.");


            _clockPolicy = settings.ClockBackwardsPolicy;


            if (machineBits < 0 || dataCenterBits < 0 || sequenceBits < 1 ||
                machineBits + dataCenterBits + sequenceBits > 22)
            {
                throw new ArgumentException(
                    "Invalid bits: SequenceBits >= 1, no field can be negative, " +
                    "and the sum of the three fields must be <= 22.");
            }


            // Masks to ensure values ​​stay within limits
            MaxMachineId = -1L ^ (-1L << machineBits); // 31
            MaxDataCenterId = -1L ^ (-1L << dataCenterBits); // 31
            MaxSequence = -1L ^ (-1L << sequenceBits); // 4095

            // Shifts to position each part in the 64-bit ID
            MachineIdShift = sequenceBits; // 12
            DataCenterIdShift = sequenceBits + machineBits; // 17
            TimestampShift = sequenceBits + machineBits + dataCenterBits; // 22


            if (settings.CustomDate is { } customDate)
            {
                if (customDate >= DateTimeOffset.UtcNow)
                    throw new ArgumentException(
                        $"Custom epoch must be earlier than the current time. Provided: {customDate}, now: {DateTimeOffset.UtcNow}.");
                Epoch = customDate.ToUnixTimeMilliseconds();
            }

            _lastTimestamp = GetCurrentTimestamp();

            var machineId = settings.MachineID ?? 0;
            var dataCenterId = settings.DataCenterID ?? 0;

            if (machineId > MaxMachineId)
                throw new ArgumentException($"Machine ID cannot be greater than {MaxMachineId}.");

            if (dataCenterId > MaxDataCenterId)
                throw new ArgumentException($"Datacenter ID cannot be greater than {MaxDataCenterId}.");

            _machineId = machineId;
            _datacenterId = dataCenterId;

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
                long now = GetCurrentTimestamp();
                long timestamp = now;
                bool clockBehind = now < _lastTimestamp;

                if (clockBehind)
                {
                    if (_clockPolicy == ClockBackwardsPolicy.Throw)
                        throw new ClockMovedBackwardsException(_lastTimestamp - now);

                    // Continue: do not let the timestamp go backwards.
                    timestamp = _lastTimestamp;
                }

                if (timestamp < _lastTimestamp)
                {
                    // Treatment for clocks that go back in time.
                    throw new ClockMovedBackwardsException(_lastTimestamp - timestamp);
                }


                if (_lastTimestamp == timestamp)
                {
                    //If the same millisecond, increment the sequence
                    _sequence = (_sequence + 1) & MaxSequence;
                    if (_sequence == 0)
                    {
                        // The sequence has burst, wait for the next millisecond
                        timestamp = clockBehind
                    ? _lastTimestamp + 1
                    : WaitNextMillis(_lastTimestamp);
                    }
                }
                else
                {
                    // Millisecond different, restart the sequence
                    _sequence = 0L;
                }

                _lastTimestamp = timestamp;

                var elapsed = timestamp - Epoch;

                // negative reading and blowout
                if (elapsed >> 41 != 0)   
                    throw new InvalidOperationException("Timestamp out of range for the configured epoch.");

                // Combine the parts to form the final ID
                return (elapsed << TimestampShift)
                 | ((long)_datacenterId << DataCenterIdShift)
                 | ((long)_machineId << MachineIdShift)
                 | _sequence;


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
            return DateTimeOffset.FromUnixTimeMilliseconds(timestamp).UtcDateTime;
        }

        // Wait until the next millisecond
        private long WaitNextMillis(long lastTimestamp)
        {
            var spinner = new SpinWait();
            long timestamp;
            while ((timestamp = GetCurrentTimestamp()) <= lastTimestamp)
                spinner.SpinOnce();
            return timestamp;
        }

        private long GetCurrentTimestamp() => _timeSource();

    }
}



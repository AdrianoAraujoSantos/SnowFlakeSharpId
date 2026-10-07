using System.Collections.Concurrent;

namespace SnowFlakeSharpId.Test;

public class SnowflakeIdTests
{
    // Default epoch of the library: 2025-01-01T00:00:00Z
    private const long DefaultEpoch = 1735689600000L;

    // Fake "now": 2026-06-01T00:00:00Z (later than every custom epoch used in the tests)
    private static readonly long StartMs =
        new DateTimeOffset(2026, 6, 1, 0, 0, 0, TimeSpan.Zero).ToUnixTimeMilliseconds();

    private sealed class FakeClock
    {
        public long Now { get; set; }
        public FakeClock(long start) => Now = start;
        public long Read() => Now;
        public void Advance(long ms) => Now += ms;
    }

    // Requires an internal constructor SnowflakeId(Settings?, Func<long>)
    // and [assembly: InternalsVisibleTo("SnowFlakeSharpId.Test")] in the library.
    private static (SnowflakeId Gen, FakeClock Clock) Create(Settings? settings = null, long? startMs = null)
    {
        var clock = new FakeClock(startMs ?? StartMs);
        var gen = new SnowflakeId(settings, clock.Read);
        clock.Advance(1); // the first ID never shares the constructor's millisecond
        return (gen, clock);
    }

    private static Settings AppSettings(ClockBackwardsPolicy policy = ClockBackwardsPolicy.Throw) => new()
    {
        MachineID = 1234,
        DataCenterIdBits = 0,
        MachineIdBits = 14,
        SequenceBits = 8,
        CustomDate = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero),
        ClockBackwardsPolicy = policy
    };

    // ---------- Basics ----------

    [Fact]
    public void NextID_WithDefaultSettings_ReturnsPositiveId()
    {
        var gen = new SnowflakeId();

        var id = gen.NextID();

        Assert.True(id > 0);
    }

    [Fact]
    public void NextID_WithSettings_EncodesAllParts()
    {
        var settings = new Settings
        {
            MachineID = 3,
            DataCenterID = 2,
            CustomDate = new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero)
        };
        var (gen, clock) = Create(settings);

        var id = gen.NextID();
        var (timestamp, dataCenterId, machineId, sequence) = gen.DecodeID(id);

        Assert.Equal(clock.Now, timestamp);
        Assert.Equal(2u, dataCenterId);
        Assert.Equal(3u, machineId);
        Assert.Equal(0u, sequence);
    }

    [Fact]
    public void NextID_InTheSameMillisecond_IncrementsSequence()
    {
        var (gen, _) = Create();

        var sequences = Enumerable.Range(0, 3)
            .Select(_ => gen.DecodeID(gen.NextID()).Sequence)
            .ToArray();

        Assert.Equal(new uint[] { 0, 1, 2 }, sequences);
    }

    [Fact]
    public void NextID_IsStrictlyIncreasing()
    {
        var gen = new SnowflakeId();
        var previous = gen.NextID();

        for (var i = 0; i < 10_000; i++)
        {
            var current = gen.NextID();
            Assert.True(current > previous, $"ID {current} is not greater than {previous} at iteration {i}.");
            previous = current;
        }
    }

    // ---------- Uniqueness ----------

    [Fact]
    public void NextID_OneMillionIds_AreUnique_DefaultSettings()
    {
        var gen = new SnowflakeId();
        var ids = new HashSet<long>();

        for (var i = 0; i < 1_000_000; i++)
            Assert.True(ids.Add(gen.NextID()), $"Duplicate ID at iteration {i}.");
    }

    [Fact]
    public void NextID_ManyIds_AreUnique_WithSettings()
    {
        var gen = new SnowflakeId(new Settings
        {
            MachineID = 1,
            DataCenterID = 1,
            CustomDate = new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero)
        });
        var ids = new HashSet<long>();

        for (var i = 0; i < 1_000_000; i++)
            Assert.True(ids.Add(gen.NextID()), $"Duplicate ID at iteration {i}.");
    }

    [Fact]
    public void NextID_AppSettings_AreUnique()
    {
        // 6 sequence bits = 64 IDs per ms, so keep the volume moderate.
        var gen = new SnowflakeId(AppSettings());
        var ids = new HashSet<long>();

        for (var i = 0; i < 50_000; i++)
            Assert.True(ids.Add(gen.NextID()), $"Duplicate ID at iteration {i}.");
    }

    [Fact]
    public void NextID_SequenceExhaustion_WaitsForNextMillisecond()
    {
        // 1 sequence bit = only 2 IDs per millisecond, so the sequence overflows constantly.
        var gen = new SnowflakeId(new Settings { SequenceBits = 1, MachineIdBits = 5, DataCenterIdBits = 5 });
        var ids = new List<long>();

        for (var i = 0; i < 200; i++)
            ids.Add(gen.NextID());

        Assert.Equal(ids.Count, ids.Distinct().Count());
        Assert.Equal(ids.OrderBy(x => x), ids);
    }

    [Fact]
    public async Task NextID_FromManyThreads_AreUnique()
    {
        var gen = new SnowflakeId();
        const int threads = 8;
        const int perThread = 50_000;

        var tasks = Enumerable.Range(0, threads)
            .Select(_ => Task.Run(() =>
            {
                var list = new List<long>(perThread);
                for (var i = 0; i < perThread; i++)
                    list.Add(gen.NextID());
                return list;
            }))
            .ToArray();

        var results = await Task.WhenAll(tasks);
        var all = new HashSet<long>();
        foreach (var list in results)
            foreach (var id in list)
                Assert.True(all.Add(id), $"Duplicate ID {id} across threads.");

        Assert.Equal(threads * perThread, all.Count);
    }

    // ---------- Decode ----------

    [Fact]
    public void DecodeID_RoundTrip_WithDefaultMaxValues()
    {
        var (gen, clock) = Create(new Settings { MachineID = 31, DataCenterID = 31 });

        var (timestamp, dataCenterId, machineId, _) = gen.DecodeID(gen.NextID());

        Assert.Equal(clock.Now, timestamp);
        Assert.Equal(31u, dataCenterId);
        Assert.Equal(31u, machineId);
    }

    [Fact]
    public void DecodeID_RoundTrip_WithAppSettings_MaxMachineId()
    {
        var settings = AppSettings();
        settings.MachineID = 1234;
        var (gen, clock) = Create(settings);

        var (timestamp, dataCenterId, machineId, sequence) = gen.DecodeID(gen.NextID());

        Assert.Equal(clock.Now, timestamp);
        Assert.Equal(0u, dataCenterId);
        Assert.Equal(settings.MachineID, machineId);
        Assert.Equal(0u, sequence);
    }

    [Fact]
    public void TimestampToDateTime_ReturnsUtc()
    {
        var (gen, clock) = Create();

        var dateTime = gen.TimestampToDateTime(clock.Now);

        Assert.Equal(DateTimeKind.Utc, dateTime.Kind);
        Assert.Equal(DateTimeOffset.FromUnixTimeMilliseconds(clock.Now).UtcDateTime, dateTime);
    }

    // ---------- Settings validation ----------

    [Fact]
    public void Settings_DefaultPolicy_IsThrow()
    {
        Assert.Equal(ClockBackwardsPolicy.Throw, new Settings().ClockBackwardsPolicy);
    }

    [Theory]
    [InlineData(5, 5, 12)]
    [InlineData(16, 0, 6)]
    [InlineData(0, 0, 22)]
    [InlineData(21, 0, 1)]
    public void Constructor_ValidBitWidths_DoesNotThrow(int machineBits, int dataCenterBits, int sequenceBits)
    {
        var ex = Record.Exception(() => new SnowflakeId(new Settings
        {
            MachineIdBits = machineBits,
            DataCenterIdBits = dataCenterBits,
            SequenceBits = sequenceBits
        }));

        Assert.Null(ex);
    }

    [Theory]
    [InlineData(5, 5, 13)]   // sum 23
    [InlineData(16, 0, 7)]   // sum 23
    [InlineData(5, 5, 0)]    // sequence < 1
    [InlineData(-1, 5, 12)]  // negative machine bits
    [InlineData(5, -1, 12)]  // negative data center bits
    [InlineData(5, 5, -1)]   // negative sequence bits
    public void Constructor_InvalidBitWidths_ThrowsArgumentException(int machineBits, int dataCenterBits, int sequenceBits)
    {
        Assert.Throws<ArgumentException>(() => new SnowflakeId(new Settings
        {
            MachineIdBits = machineBits,
            DataCenterIdBits = dataCenterBits,
            SequenceBits = sequenceBits
        }));
    }

    [Fact]
    public void Constructor_MachineIdAboveMax_Throws()
    {
        Assert.Throws<ArgumentException>(() => new SnowflakeId(new Settings { MachineID = 32 }));
        Assert.Null(Record.Exception(() => new SnowflakeId(new Settings { MachineID = 31 })));
    }

    [Fact]
    public void Constructor_DataCenterIdAboveMax_Throws()
    {
        Assert.Throws<ArgumentException>(() => new SnowflakeId(new Settings { DataCenterID = 32 }));
        Assert.Null(Record.Exception(() => new SnowflakeId(new Settings { DataCenterID = 31 })));
    }

    [Fact]
    public void Constructor_ZeroDataCenterBits_OnlyAcceptsZeroId()
    {
        Assert.Throws<ArgumentException>(() => new SnowflakeId(new Settings
        {
            DataCenterIdBits = 0,
            DataCenterID = 1
        }));

        Assert.Null(Record.Exception(() => new SnowflakeId(new Settings
        {
            DataCenterIdBits = 0,
            DataCenterID = 0
        })));
    }

    [Fact]
    public void Constructor_CustomDateInTheFuture_Throws()
    {
        Assert.Throws<ArgumentException>(() => new SnowflakeId(new Settings
        {
            CustomDate = DateTimeOffset.UtcNow.AddDays(1)
        }));
    }

    [Fact]
    public void Constructor_InvalidClockPolicy_Throws()
    {
        Assert.Throws<ArgumentException>(() => new SnowflakeId(new Settings
        {
            ClockBackwardsPolicy = (ClockBackwardsPolicy)99
        }));
    }

    // ---------- Clock going back in time ----------

    [Fact]
    public void NextID_ClockGoesBack_WithThrowPolicy_Throws()
    {
        var (gen, clock) = Create();
        gen.NextID();

        clock.Advance(-5);

        var ex = Assert.Throws<ClockMovedBackwardsException>(() => gen.NextID());
        Assert.Equal(5, ex.OffsetMs);
    }

    [Fact]
    public void NextID_ClockGoesBack_WithContinuePolicy_NeverRegresses()
    {
        var (gen, clock) = Create(new Settings { ClockBackwardsPolicy = ClockBackwardsPolicy.Continue });
        var first = gen.NextID();

        clock.Advance(-5_000);
        var second = gen.NextID();

        Assert.True(second > first);
        Assert.True(gen.DecodeID(second).Timestamp >= gen.DecodeID(first).Timestamp);
    }

    [Fact]
    public void NextID_ContinuePolicy_SequenceExhaustedWhileBehind_AdvancesLogicalTime()
    {
        var (gen, clock) = Create(new Settings
        {
            SequenceBits = 2, // 4 IDs per millisecond
            MachineIdBits = 5,
            DataCenterIdBits = 5,
            ClockBackwardsPolicy = ClockBackwardsPolicy.Continue
        });
        var ids = new List<long> { gen.NextID() };

        clock.Advance(-1_000);
        for (var i = 0; i < 10; i++)
            ids.Add(gen.NextID());

        Assert.Equal(ids.Count, ids.Distinct().Count());
        Assert.Equal(ids.OrderBy(x => x), ids);
        Assert.True(gen.DecodeID(ids[^1]).Timestamp > gen.DecodeID(ids[0]).Timestamp);
    }

    [Fact]
    public void NextID_ContinuePolicy_FollowsRealClockAfterItCatchesUp()
    {
        var (gen, clock) = Create(new Settings { ClockBackwardsPolicy = ClockBackwardsPolicy.Continue });
        gen.NextID();

        clock.Advance(-10);
        gen.NextID();           // generated from the logical timestamp

        clock.Advance(1_000);   // real clock is now ahead of the logical one
        var id = gen.NextID();

        Assert.Equal(clock.Now, gen.DecodeID(id).Timestamp);
    }

    // ---------- Timestamp range ----------

    [Fact]
    public void NextID_TimestampBeyond41Bits_Throws()
    {
        // Create() advances the clock by 1 ms, so the first ID is exactly epoch + 2^41.
        var (gen, _) = Create(startMs: DefaultEpoch + (1L << 41) - 1);

        Assert.Throws<InvalidOperationException>(() => gen.NextID());
    }

    [Fact]
    public void NextID_TimestampBeforeEpoch_Throws()
    {
        var (gen, _) = Create(startMs: DefaultEpoch - 10);

        Assert.Throws<InvalidOperationException>(() => gen.NextID());
    }
}

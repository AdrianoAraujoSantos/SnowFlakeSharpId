# SnowFlakeSharpId

![Package icon](icon.png)

SnowFlakeSharpId is a unique ID generator based on [Twitter's Snowflake](https://blog.twitter.com/engineering/en_us/a/2010/announcing-snowflake "Twitter Snowflake Blog"). It generates 64-bit, time-ordered, unique IDs that fit in a C# `long`, and it is thread-safe.

Compatible with .NET 10.

## Features

- 64-bit IDs, sortable by creation time.
- Thread-safe generation.
- Custom epoch.
- Configurable number of bits for the data center ID, machine ID and sequence.
- Configurable behavior when the system clock goes back in time (`ClockBackwardsPolicy`).
- Decoding of an ID back into its parts.
- Validation of all settings at construction time.

## Installation

Package Manager Console:

```powershell
Install-Package SnowFlakeSharpId
```

.NET CLI:

```
dotnet add package SnowFlakeSharpId
```

## ID layout

An ID is a signed 64-bit integer. The highest bit is always 0, so IDs are never negative.

```
| 1 bit  | 41 bits   | DataCenterIdBits | MachineIdBits | SequenceBits |
| unused | timestamp | data center ID   | machine ID    | sequence     |
```

- **Timestamp (41 bits, fixed):** milliseconds elapsed since the epoch. 41 bits cover about 69.7 years from the epoch.
- **Data center ID, machine ID and sequence:** their widths are configurable, but the **sum of the three cannot exceed 22 bits** and `SequenceBits` must be at least 1.

Default widths:

| Field         | Default bits | Maximum value |
| ------------- | ------------ | ------------- |
| Data center   | 5            | 31            |
| Machine       | 5            | 31            |
| Sequence      | 12           | 4095          |

The sequence limits throughput: one instance can generate at most `2^SequenceBits` IDs per millisecond (4096 with the defaults). If the limit is reached within a millisecond, generation waits for the next one.

## Usage

```csharp
using SnowFlakeSharpId;
```

Create an instance. Settings are optional, and `new SnowflakeId()` uses the defaults:

```csharp
var settings = new Settings
{
    MachineID = 1,
    DataCenterID = 1,
    CustomDate = new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero)
};

var snowflake = new SnowflakeId(settings);
```

Generate a new ID:

```csharp
long id = snowflake.NextID();
```

Decode an ID into its parts:

```csharp
var (timestamp, dataCenterId, machineId, sequence) = snowflake.DecodeID(id);
```

`timestamp` is the Unix time in milliseconds (the epoch is already added back). Convert it to a UTC date and time:

```csharp
DateTime createdAtUtc = snowflake.TimestampToDateTime(timestamp);
```

The returned `DateTime` has `Kind = Utc`.

## Settings

| Property               | Type                   | Default                    | Description |
| ---------------------- | ---------------------- | -------------------------- | ----------- |
| `MachineID`            | `uint?`                | `0`                        | Identifier of the machine or instance generating IDs. Must fit in `MachineIdBits`. |
| `DataCenterID`         | `uint?`                | `0`                        | Identifier of the data center (or any other origin you want to encode). Must fit in `DataCenterIdBits`. |
| `CustomDate`           | `DateTimeOffset?`      | `2025-01-01T00:00:00Z`     | Custom epoch. Must be earlier than the current time. |
| `MachineIdBits`        | `int?`                 | `5`                        | Bits reserved for the machine ID. |
| `DataCenterIdBits`     | `int?`                 | `5`                        | Bits reserved for the data center ID. `0` is allowed, and then `DataCenterID` can only be `0`. |
| `SequenceBits`         | `int?`                 | `12`                       | Bits reserved for the sequence. Must be at least 1. |
| `ClockBackwardsPolicy` | `ClockBackwardsPolicy` | `Throw`                    | What to do when the system clock goes back in time. See below. |

### Validation

The `SnowflakeId` constructor throws `ArgumentException` when:

- any bit width is negative;
- `SequenceBits` is less than 1;
- `MachineIdBits + DataCenterIdBits + SequenceBits` is greater than 22;
- `MachineID` or `DataCenterID` is greater than the maximum allowed by its bit width;
- `CustomDate` is not earlier than the current time;
- `ClockBackwardsPolicy` is not a defined value.

## Clock going back in time

System clocks can move backwards (automatic time adjustment, time zone or NTP corrections, manual changes). The behavior is controlled by `Settings.ClockBackwardsPolicy`:

| Policy               | Behavior |
| -------------------- | -------- |
| `Throw` (default)    | `NextID()` throws `ClockMovedBackwardsException`. The exception exposes `OffsetMs`, how many milliseconds the clock went back. |
| `Continue`           | `NextID()` keeps using the last known timestamp, so IDs never decrease and the clock never causes an exception. If the sequence is exhausted, the timestamp is advanced logically by 1 ms. When the real clock catches up, generation follows it again. |

```csharp
var settings = new Settings
{
    MachineID = 7,
    ClockBackwardsPolicy = ClockBackwardsPolicy.Continue
};
```

Trade-off of `Continue`: during a long clock regression, the timestamp embedded in new IDs can be ahead of the real time. If you need the real creation time, store it in a separate field.

Neither policy can detect a clock that goes back **between runs of the application**, because a new process has no memory of the last timestamp. If duplicates must be impossible in that case, rely on a unique constraint in your database and retry on conflict.

## Other exceptions

| Exception                       | When |
| ------------------------------- | ---- |
| `ArgumentException`             | Invalid settings (see Validation). |
| `ClockMovedBackwardsException`  | Clock went back and the policy is `Throw`. It derives from `Exception`. |
| `InvalidOperationException`     | The timestamp is outside the 41-bit range for the configured epoch (negative, or more than about 69.7 years after the epoch). |

## How `NextID()` works

1. The method takes a lock, so it is thread-safe.
2. It reads the current time in milliseconds (UTC).
3. If the current time is earlier than the last timestamp, it applies the `ClockBackwardsPolicy`: it throws (`Throw`) or keeps using the last timestamp (`Continue`).
4. If the timestamp is the same as the last one, the sequence is incremented. If the sequence wraps around to 0, the method waits for the next real millisecond (or, under `Continue` with a clock behind, advances the timestamp by 1 ms).
5. If the timestamp is different, the sequence is reset to 0.
6. The elapsed time since the epoch is checked against the 41-bit range.
7. Timestamp, data center ID, machine ID and sequence are combined into the final ID.

## Recommendations

- **Use one instance per generator and share it.** Register it as a singleton (for example, in dependency injection). Two instances with the same `MachineID` and `DataCenterID` can produce duplicate IDs.
- **Make every generator unique.** The pair `DataCenterID` + `MachineID` must be different for every process or device that generates IDs at the same time.
- **Offline or mobile apps.** If each installation generates IDs by itself, give more bits to the machine ID and fewer to the sequence, since one device generates few IDs per millisecond. For example:

  ```csharp
  var settings = new Settings
  {
      MachineID = installationNodeId,   // 0 to 65535
      DataCenterIdBits = 0,
      MachineIdBits = 16,
      SequenceBits = 6,                 // up to 64 IDs per millisecond
      CustomDate = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero),
      ClockBackwardsPolicy = ClockBackwardsPolicy.Continue
  };
  ```

- **All systems must share the same layout.** Every application that creates or decodes IDs (mobile app, API, web) must use the same bit widths and the same `CustomDate`. Changing them later makes old IDs decode incorrectly.
- **Always set `CustomDate` explicitly** in your applications instead of depending on the library default.
- **JavaScript and JSON.** JavaScript numbers lose precision above 2^53, and Snowflake IDs are larger than that. Serialize IDs as **strings** in JSON APIs that browsers consume.
- **SQLite and databases.** IDs fit in a signed 64-bit column (`INTEGER`, `BIGINT`).

## Changelog

### 1.1.0

- **New:** `ClockBackwardsPolicy` setting (`Throw` or `Continue`).
- **New:** `ClockMovedBackwardsException`, with the `OffsetMs` property, replacing the generic `Exception`.
- **New:** protection against timestamp overflow (`InvalidOperationException`).
- **Fixed:** validation of bit widths. The sum of the three fields (maximum 22) is now checked, and negative widths and `SequenceBits < 1` are rejected. Previously, invalid sums produced corrupted or negative IDs silently.
- **Fixed:** `MachineID` and `DataCenterID` are now cast to `long` before shifting, preventing silent loss of bits.
- **Fixed:** the initial last timestamp now uses the same scale as `NextID()`, so a clock set back right after creation is detected from the first call.
- **Fixed:** `TimestampToDateTime` now returns a `DateTime` with `Kind = Utc`.
- **Changed:** invalid settings now throw `ArgumentException` consistently.
- **Docs:** corrected the README bit layout (41 bits of timestamp, and the sum of the other three fields limited to 22), the description of `DataCenterIdBits`, and the settings table.
- **Removed:** unused internal fields.

## License

This project is licensed under the [MIT License](https://github.com/AdrianoAraujoSantos/SnowFlakeSharpId/blob/master/LICENSE.txt).

## Contributing

Contributions are welcome. Please submit a pull request or create an issue to discuss your proposed changes.
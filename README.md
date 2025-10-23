# SnowFlakeSharpId
SnowFlakeSharpId is a unique ID generator based on [Twitter's Snowflake](https://blog.twitter.com/engineering/en_us/a/2010/announcing-snowflake "Twitter Snowflake Blog"). It generates 64-bit, time-ordered, unique IDs based on the Snowflake algorithm. It is written in C# and is compatible with .NET Standard 2.0.

The default bit assignment for this Snowflake implementation is:
```
22 bits for the TimeStamp value
17 bits for the DataCenterId value
12 bits for the MachineID value
12 bits for Sequence value
```

This provides by default:
MachineId maximum default value is 31
DataCenterId maximum default value is 31
Sequence maximum default value is 4095

If you require a higher generation rate or large range of MachineID's these values can be customised by the Settings used to initialize a Snowflake instance.

**_NOTE:_** 41 bits are always reserved for the TimeStamp value. Therefore, the sum of DataCenterIdBits, MachineIdBits and SequenceBits cannot exceed 22. SequenceBits must be at least equal to 1.

# Features

 - Generates 64-bit unique IDs, which are time-ordered.
 - Customizable machine ID and custom epoch settings.
 - Thread-safe ID generation.
 - High-performance and low-latency.

# Installation

To install the SnowFlakeSharpId library, you can use the following command in the Package Manager Console:
```powershell
     Install-Package SnowFlakeSharpId
```
Alternatively, you can use the .NET CLI:
```
     dotnet add package SnowFlakeSharpId
```

# Usage

First, import the SnowFlakeSharpId namespace:
```csharp
         using SnowFlakeSharpId;
```

Create a new instance of the `snowflakeid` class with optional settings:

```csharp
          var settings = new Settings()
            {
                MachineID = 1,
                DataCenterID = 1,
                CustomDate = new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc),
            };

             var snowflakeid = new SnowflakeId(settings);
```
 **Note: Using the setting is optional

Generate a new unique ID:

```csharp
     var id = snowflakeid.NextID();
```

 Decode ID to parts to return to the data that generated the Id
```csharp
            long timeStamp;
            long dataCenterId;
            long machineId;
            long sequence;

             (timeStamp, dataCenterId, machineId, sequence) =  snowflakeid.DecodeID(id);
```

Convert the timeStamp to the UTC date and time used in generating the ID
```csharp
     var dateTime = snowflakeid.TimestampToDateTime(timeStamp);
```

## Settings

The `Settings` class allows you to customize the SnowFlakeSharpId instance:

-   `MachineID`: A unique identifier for the machine or instance generating the IDs.
	- Defaults to: 0
-   `DataCenterID`: A unique identifier for the data center will be used to generate the IDs.
	- Defaults to: 0
-   `CustomDate`: A custom epoch or reference point for the timestamp portion of the generated ID.
	- Defaults to: 2025-01-01T00:00:00.000Z
- `MachineIdBits`: Sets the number of bits allocated to the Machine ID part of the Snowflake ID.
	- Defaults to: 5
- `DataCenterIdBits`: Sets the number of bits allocated to the Sequence part of the Snowflake ID.
	- Defaults to: 5
- `SequenceBits`: Sets the number of bits allocated to the Sequence part of the Snowflake ID.
	- Defaults to: 12


## Explanation of the NextID function algorithm
  
1-The method is locked using a lock statement to ensure thread safety.
2-The current timestamp is obtained using the GetCurrentTimestamp() method.
3-If the current timestamp is less than the previous timestamp, an exception is thrown indicating that the system clock has gone back in time.
4-If the current timestamp is the same as the previous timestamp, the sequence number is incremented and checked if it has reached its maximum value. If so, the method waits for the next millisecond using the WaitNextMillis() method.
5-If the current timestamp is different from the previous timestamp, the sequence number is reset to 0.
6-The timestamp, data center ID, machine ID, and sequence number are combined to form the final ID.
7-The final ID is returned.

## License

This project is licensed under the [MIT License](https://github.com/AdrianoAraujoSantos/SnowFlakeSharpId/blob/master/LICENSE.txt).
## Contributing

Contributions are welcome. Please submit a pull request or create an issue to discuss your proposed changes.

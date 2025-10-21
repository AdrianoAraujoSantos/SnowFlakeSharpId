using SnowFlakeSharpId;


var snowflakeid = new SnowflakeId();

for (int i = 0; i < 1000; i++)
{
    long snowflakeId = snowflakeid.NextID();
    Console.WriteLine($"Generated Snowflake ID: {snowflakeId}");
}

namespace SnowFlakeSharpId.Test
{
    public class SnowflakeIdTests
    {
        [Fact]
        public void NextIDTest()
        {
            // Arrange
            var snowflakeid = new SnowflakeId();

            // Act
            var id1 = snowflakeid.NextID();
            var id2 = snowflakeid.NextID();
            var id3 = snowflakeid.NextID();

            // Assert
            Assert.NotEqual(id1, id2, id3);
        }

        [Fact]
        public void NextIDSettingsTest()
        {
            var settings = new Settings()
            {
                MachineID = 1,
                DataCenterID = 1,
                CustomDate = new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc),
            };

            // Arrange
            var snowflakeid = new SnowflakeId(settings);

            // Act
            var id1 = snowflakeid.NextID();
            var id2 = snowflakeid.NextID();
            var id3 = snowflakeid.NextID();

            // Assert
            Assert.NotEqual(id1, id2, id3);
        }
    }
}

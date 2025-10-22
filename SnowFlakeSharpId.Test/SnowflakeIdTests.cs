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

            // Assert
            Assert.NotEqual(0L, id1);
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


            // Assert
            Assert.NotEqual(0L, id1);
        }
        [Fact]
        public void NextIDThousandIdTest()
        {
            // Arrange
            var snowflakeid = new SnowflakeId();
            HashSet<long> ids = [];

             // Act
            for (int i = 0; i < 1000; i++)
            {
                var id1 = snowflakeid.NextID();

                if (ids.Contains(id1))
                    Assert.True(false);
                else
                    ids.Add(id1);
            }

           
        }
        [Fact]
        public void NextIDOneMillionIDsTest()
        {
            // Arrange
            var snowflakeid = new SnowflakeId();
            HashSet<long> ids = [];

            // Act
            for (int i = 0; i < 1000000; i++)
            {
                var id1 = snowflakeid.NextID();

                if (ids.Contains(id1))
                    Assert.True(false);
                else
                    ids.Add(id1);
            }
        }

        [Fact]
        public void NextIDThousandIdSettingsTest()
        {
            var settings = new Settings()
            {
                MachineID = 1,
                DataCenterID = 1,
                CustomDate = new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc),
            };

            // Arrange
            var snowflakeid = new SnowflakeId(settings);
            HashSet<long> ids = [];

            // Act
            for (int i = 0; i < 1000; i++)
            {
                var id1 = snowflakeid.NextID();

                if (ids.Contains(id1))
                    Assert.True(false);
                else
                    ids.Add(id1);
            }


        }
        [Fact]
        public void NextIDOneMillionIDsSettingsTest()
        {
            var settings = new Settings()
            {
                MachineID = 1,
                DataCenterID = 1,
                CustomDate = new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc),
            };
            // Arrange
            var snowflakeid = new SnowflakeId(settings);
            HashSet<long> ids = [];

            // Act
            for (int i = 0; i < 1000000; i++)
            {
                var id1 = snowflakeid.NextID();

                if (ids.Contains(id1))
                    Assert.True(false);
                else
                    ids.Add(id1);
            }
        }
    }
}

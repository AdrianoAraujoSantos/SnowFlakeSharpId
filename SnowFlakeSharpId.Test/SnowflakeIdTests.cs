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
            var id = snowflakeid.NextID();

            // Assert
            Assert.NotEqual(0L, id);
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
            var id = snowflakeid.NextID();


            // Assert
            Assert.NotEqual(0L, id);
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
                var id = snowflakeid.NextID();

                if (ids.Contains(id))
                    Assert.True(false);
                else
                    ids.Add(id);
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
                var id = snowflakeid.NextID();

                if (ids.Contains(id))
                    Assert.True(false);
                else
                    ids.Add(id);
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
                var id = snowflakeid.NextID();

                if (ids.Contains(id))
                    Assert.True(false);
                else
                    ids.Add(id);
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
                var id = snowflakeid.NextID();

                if (ids.Contains(id))
                    Assert.True(false);
                else
                    ids.Add(id);
            }
        }

        [Fact]
        public void DecodeIdTest()
        {
           
            long timeStamp;
            long dataCenterId;
            long machineId;
            long sequence;

            var settings = new Settings()
            {
                MachineID = 1,
                DataCenterID = 1,
                CustomDate = new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc),
            };

            // Arrange
            var snowflakeid = new SnowflakeId(settings);

            // Act
            var id = snowflakeid.NextID();
            (timeStamp, dataCenterId, machineId, sequence) =  snowflakeid.DecodeID(id);

            //Converts a timestamp in milliseconds to a DateTime object
            var dateTime = snowflakeid.TimestampToDateTime(timeStamp);

            // Assert
            Assert.True(dataCenterId == 1 && machineId == 1 && sequence == 0);
        }
    }
}

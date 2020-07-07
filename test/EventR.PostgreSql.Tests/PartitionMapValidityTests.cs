namespace EventR.PostgreSql.Tests
{
    using System;
    using Xunit;

    public class PartitionMapValidityTests
    {
        [Fact]
        public void EmptyHosts()
        {
            var pm = new PartitionMap { PartitionCount = 3, Hosts = Array.Empty<PartitionHostMap>() };
            var (ok, err) = pm.Validate();
            Assert.False(ok);
        }

        [Fact]
        public void WrongNumberOfPartitions()
        {
            var pm = new PartitionMap
            {
                PartitionCount = 3,
                Hosts = new[]
                {
                    new PartitionHostMap { Host = "localhost", Port = 5432, Partitions = new[] { 1, 2 } }
                }
            };
            var (ok, err) = pm.Validate();
            Assert.False(ok);
        }

        [Fact]
        public void DuplicatePartitions()
        {
            var pm = new PartitionMap
            {
                PartitionCount = 3,
                Hosts = new[]
                {
                    new PartitionHostMap { Host = "localhost", Port = 5432, Partitions = new[] { 1, 2, 2 } }
                }
            };
            var (ok, err) = pm.Validate();
            Assert.False(ok);
        }

        [Fact]
        public void Correct()
        {
            var pm = new PartitionMap
            {
                PartitionCount = 3,
                Hosts = new[]
                {
                    new PartitionHostMap { Host = "localhost", Port = 5432, Partitions = new[] { 1, 2 } },
                    new PartitionHostMap { Host = "localhost", Port = 5433, Partitions = new[] { 3 } }
                }
            };
            var (ok, err) = pm.Validate();
            Assert.True(ok);
        }
    }
}

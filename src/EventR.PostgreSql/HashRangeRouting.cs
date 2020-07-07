namespace EventR.PostgreSql
{
    using EventR.Abstractions;
    using EventR.Abstractions.Exceptions;
    using System.Collections.Generic;
    using System.Text;

    public sealed class HashRangeRouting : IProvideRoutingInfo
    {
        private readonly Encoding encoding = Encoding.ASCII;
        private readonly uint partitionStep;
        private readonly Dictionary<int, PartitionHostMap> hosts;

        public HashRangeRouting(PartitionMap partitionMap)
        {
            Expect.NotNull(partitionMap, nameof(partitionMap));
            partitionMap.ValidateOrThrow();
            partitionStep = (uint)(uint.MaxValue / partitionMap.PartitionCount);
            hosts = CreateHosts(partitionMap);
        }

        public RoutingInfo CreateRoutingInfo(string streamId)
        {
            var hash = MurmurHash2.Hash(encoding.GetBytes(streamId));
            var partition = Partition(hash);
            if (hosts.TryGetValue(partition, out var pmh))
            {
                return new RoutingInfo(pmh.Host, pmh.Port, partition);
            }

            throw new EventStoreException($"no host is configured for partition {partition} derived from stream {streamId}");
        }

        private int Partition(uint hash)
            => (int)((hash / partitionStep) + 1);

        private static Dictionary<int, PartitionHostMap> CreateHosts(PartitionMap partitionMap)
        {
            var dict = new Dictionary<int, PartitionHostMap>(partitionMap.PartitionCount);
            foreach (var phm in partitionMap.Hosts)
            {
                foreach (var p in phm.Partitions)
                {
                    dict.Add(p, phm);
                }
            }

            return dict;
        }
    }
}

namespace EventR.PostgreSql
{
    public sealed class PartitionHostMap
    {
        public string Host { get; set; }

        public int Port { get; set; } = 5432;

        public int[] Partitions { get; set; }
    }
}

namespace EventR.PostgreSql
{
    using System;

    /// <summary>
    /// Defines routing - definition of host, port and partition - in a persistence layer.
    /// </summary>
    public sealed class RoutingInfo : IEquatable<RoutingInfo>
    {
        public RoutingInfo(string host, int port = 5432, int partition = 0)
        {
            Host = host;
            Port = port;
            Partition = partition;
            HostAndPort = host + ":" + port;
        }

        public string Host { get; }

        public int Port { get; }

        public int Partition { get; }

        public bool HasPartition => Partition > 0;

        public string HostAndPort { get; }

        public bool Equals(RoutingInfo other)
        {
            if (object.ReferenceEquals(other, null))
            {
                return false;
            }

            if (object.ReferenceEquals(this, other))
            {
                return true;
            }

            return Host.Equals(other.Host) && Port.Equals(other.Port) && Partition.Equals(other.Partition);
        }

        public override bool Equals(object obj)
        {
            return obj != null && obj is RoutingInfo ri
                ? Equals(ri)
                : false;
        }

        public override string ToString()
            => $"{HostAndPort}/{Partition}";

        public override int GetHashCode()
        {
            int h;
            unchecked
            {
                h = Host.GetHashCode();
                h = (17 * h) + Port.GetHashCode();
                h = (17 * h) + Partition.GetHashCode();
            }

            return h;
        }

        public static bool operator ==(RoutingInfo lhs, RoutingInfo rhs)
        {
            if (object.ReferenceEquals(lhs, null))
            {
                return object.ReferenceEquals(rhs, null);
            }

            return lhs.Equals(rhs);
        }

        public static bool operator !=(RoutingInfo lhs, RoutingInfo rhs)
        {
            return !lhs.Equals(rhs);
        }
    }
}

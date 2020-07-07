namespace EventR.PostgreSql
{
    using EventR.Abstractions;
    using System;
    using System.Text.RegularExpressions;

    public struct HostAndPort : IEquatable<HostAndPort>
    {
        private static readonly Regex HostAndPortRx = new Regex(@"^(?<host>[\w\.\-]+)(:(?<port>\d+))?$", RegexOptions.Compiled);

        public string Host { get; }

        public int Port { get; }

        public HostAndPort(string host, int port)
        {
            Expect.NotEmpty(host, nameof(host));
            Expect.Range(port, 1024, 65535, nameof(port));
            Host = host;
            Port = port;
        }

        public bool Equals(HostAndPort other)
        {
            if (object.ReferenceEquals(other, null))
            {
                return false;
            }

            if (object.ReferenceEquals(this, other))
            {
                return true;
            }

            return Host.Equals(other.Host) && Port.Equals(other.Port);
        }

        public override bool Equals(object obj)
        {
            return obj != null && obj is HostAndPort hp
                ? Equals(hp)
                : false;
        }

        public override int GetHashCode()
        {
            int h;
            unchecked
            {
                h = Host.GetHashCode();
                h = (17 * h) + Port.GetHashCode();
            }

            return h;
        }

        public override string ToString()
            => $"{Host}:{Port}";

        public static HostAndPort Parse(string hostAndPort)
        {
            var m = HostAndPortRx.Match(hostAndPort);
            if (!m.Success)
            {
                throw new ArgumentException("invalid host name", nameof(hostAndPort));
            }

            var host = m.Groups["host"].Value;
            var portGrp = m.Groups["port"];
            var port = portGrp.Success
                ? int.Parse(portGrp.Value)
                : 5432;

            return new HostAndPort(host, port);
        }

        public static bool operator ==(HostAndPort left, HostAndPort right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(HostAndPort left, HostAndPort right)
        {
            return !left.Equals(right);
        }
    }
}

using EventR.Abstractions.Exceptions;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System.Linq;

namespace EventR.PostgreSql
{
    public sealed class PartitionMap
    {
        public int PartitionCount { get; set; } = 16;

        public PartitionHostMap[] Hosts { get; set; }

        public static PartitionMap FromConfig(IConfiguration config, string sectionKey = "eventr:postgresql:partitionMap")
        {
            return config.GetSection(sectionKey).Get<PartitionMap>();
        }

        public (bool ok, ICollection<string> errors) Validate()
        {
            var err = new List<string>();
            if (PartitionCount < 1 || PartitionCount > 65535)
            {
                err.Add("PartitionCount is not in range <1, 65535>");
            }

            if (Hosts == null || Hosts.Length == 0)
            {
                err.Add("Hosts are null or empty");
            }
            else
            {
                var defined = Hosts.SelectMany(x => x.Partitions).ToArray();
                if (defined.Length != PartitionCount)
                {
                    err.Add("Hosts define incorrect number of partitions");
                }
                else
                {
                    var all = Enumerable.Range(1, PartitionCount).ToArray();
                    var diff = defined.Except(all);
                    if (diff.Any())
                    {
                        err.Add($"Hosts define incorrect partitions: {string.Join(", ", diff)}");
                    }

                    var duplicate = defined.GroupBy(x => x).Where(g => g.Count() > 1).Select(g => g.Key).ToArray();
                    if (duplicate.Length > 0)
                    {
                        err.Add($"Partitions {string.Join(", ", duplicate)} are defined more than once");
                    }
                }
            }

            var ok = err.Count == 0;
            return (ok, ok ? null : err);
        }

        public void ValidateOrThrow()
        {
            var (ok, errors) = Validate();
            if (!ok)
            {
                var msg = $"{string.Join("; ", errors)}";
                throw new EventStoreException(msg);
            }
        }
    }
}

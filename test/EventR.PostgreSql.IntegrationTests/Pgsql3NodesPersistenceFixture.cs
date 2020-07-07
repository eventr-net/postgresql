namespace EventR.PostgreSql.IntegrationTests
{
    using EventR.Abstractions;
    using EventR.Spec.Persistence;
    using KellermanSoftware.CompareNetObjects;
    using System.Linq;
    using System.Threading.Tasks;

    public sealed class Pgsql3NodesPersistenceFixture : IPersistenceSpecFixture
    {
        private bool disposed;
        private readonly ConfigFixture config;
        private readonly string schema;
        private readonly PgsqlPersistence persistence;
        private readonly CompareLogic comparer;

        public IPersistence Persistence => persistence;

        public string Description => "PostgreSQL persistence";

        public Pgsql3NodesPersistenceFixture()
        {
            config = new ConfigFixture();
            comparer = new CompareLogic();
            schema = PostgreSql.Util.EnsureStorageForTests(config.ConnectionString, config.PartitionMap);
            var connFactory = new RoutedDbConnFactory(config.ConnectionString);
            var routingProvider = new HashRangeRouting(config.PartitionMap);
            persistence = new PgsqlPersistence(connFactory, routingProvider, schema);
        }

        public void Dispose()
        {
            if (disposed)
            {
                return;
            }

            persistence.Dispose();
            PostgreSql.Util.DeleteStorageAfterTests(config.ConnectionString, schema, config.PartitionMap);
            disposed = true;
        }

        public async Task<(bool ok, string errorDetail)> HasBeenSavedAsync(Commit expected)
        {
            using (var sess = persistence.OpenSession(false))
            {
                var load = await sess.LoadCommitsAsync(expected.StreamId).ConfigureAwait(false);
                var matched = load.Commits
                    .Where(x =>
                        string.CompareOrdinal(x.StreamId, expected.StreamId) == 0 &&
                        x.Version == expected.Version)
                    .ToArray();

                if (matched.Length != 1)
                {
                    return (false, $"commit (stream '{expected.StreamId}', version {expected.Version}) has not been found");
                }

                var storageCommit = matched.First();
                var result = comparer.Compare(expected, storageCommit);
                if (!result.AreEqual)
                {
                    var errorDetail =
                        $"commit (stream '{expected.StreamId}', version {expected.Version}) has been found, " +
                        $"but it has different data properties: {result.DifferencesString}";
                    return (false, errorDetail);
                }

                return (true, null);
            }
        }
    }
}

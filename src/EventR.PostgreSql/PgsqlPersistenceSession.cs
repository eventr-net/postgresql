namespace EventR.PostgreSql
{
    using EventR.Abstractions;
    using EventR.Abstractions.Exceptions;
    using Npgsql;
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Data.Common;
    using System.Linq;
    using System.Text.RegularExpressions;
    using System.Threading.Tasks;
    using System.Transactions;

    public sealed class PgsqlPersistenceSession : IPersistenceSession
    {
        private const string PgsqlCodeOnDuplicateRec = "23505";
        private static readonly Regex NpgsqlExDetailRx = new Regex(
            @"^Key \(streamid, version\)=\((?<streamId>[\w\-\/\.]+), (?<version>\d+)\) already exists",
            RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

        private readonly IDbConnectionFactory connectionFactory;
        private readonly IProvideRoutingInfo routing;
        private readonly string schema;
        private readonly ConcurrentDictionary<string, NpgsqlConnection> connections;
        private bool isDisposed;

        public bool SuppressAmbientTransaction { get; }

        public PgsqlPersistenceSession(
            IDbConnectionFactory connectionFactory,
            IProvideRoutingInfo routingProvider,
            bool suppressAmbientTransaction = false,
            string schema = "es")
        {
            Expect.NotNull(connectionFactory, nameof(connectionFactory));
            Expect.NotNull(routingProvider, nameof(routingProvider));
            Expect.NotEmpty(schema, nameof(schema));

            this.connectionFactory = connectionFactory;
            routing = routingProvider;
            SuppressAmbientTransaction = suppressAmbientTransaction;
            this.schema = schema;
            connections = new ConcurrentDictionary<string, NpgsqlConnection>();
        }

        public void Dispose()
        {
            if (isDisposed)
            {
                return;
            }

            CloseAllConnections();
            isDisposed = true;
        }

        private void CloseAllConnections()
        {
            foreach (var conection in connections.Values.Where(c => c != null))
            {
                conection.Close();
            }

            connections.Clear();
        }

        public async Task<CommitsLoad> LoadCommitsAsync(string streamId)
        {
            Expect.NotDisposed(isDisposed);
            Expect.NotEmpty(streamId, "streamId");

            var routingInfo = routing.CreateRoutingInfo(streamId);

            return await Execute(async () =>
            {
                var result = new List<Commit>();

                var conn = await GetOrCreateOpenedConnection(routingInfo).ConfigureAwait(false);
                using (var cmd = SqlCommands.Load(streamId, routingInfo.Partition, schema))
                {
                    cmd.Connection = conn;
                    using (var reader = await cmd.ExecuteReaderAsync().ConfigureAwait(false))
                    {
                        while (reader.Read())
                        {
                            var commit = ReadRowAsCommit(reader);
                            result.Add(commit);
                        }
                    }
                }

                var commits = result.OrderBy(c => c.Version).ToArray();

                return commits.Length > 0
                    ? new CommitsLoad(commits, commits.Last().Version)
                    : CommitsLoad.Empty;
            }).ConfigureAwait(false);
        }

        private static Commit ReadRowAsCommit(DbDataReader reader)
        {
            var commit = new Commit();
            commit.Id = reader.GetGuid(0);
            commit.StreamId = reader.GetString(1);
            commit.ItemsCount = reader.GetInt16(2);
            commit.Version = reader.GetInt32(3);
            commit.SerializerId = reader.GetString(4);
            commit.Payload = reader.GetAllBytes(5);
            if (!reader.IsDBNull(6))
            {
                commit.PayloadLayout = new PayloadLayout(reader.GetAllBytes(6));
            }

            return commit;
        }

        public async Task<bool> SaveAsync(Commit commit)
        {
            Expect.NotDisposed(isDisposed);
            Expect.NotNull(commit, "commit");
            commit.ThrowIfContainsInvalidData();

            var routingInfo = routing.CreateRoutingInfo(commit.StreamId);

            return await Execute(async () =>
            {
                var conn = await GetOrCreateOpenedConnection(routingInfo).ConfigureAwait(false);
                using (var cmd = SqlCommands.Save(commit, routingInfo.Partition, schema))
                {
                    cmd.Connection = conn;
                    try
                    {
                        var affected = await cmd.ExecuteNonQueryAsync().ConfigureAwait(false);
                        return affected == 1;
                    }
                    catch (PostgresException ex)
                    {
                        if (ex.SqlState == PgsqlCodeOnDuplicateRec)
                        {
                            throw ToVersionConflict(ex);
                        }

                        throw;
                    }
                }
            }).ConfigureAwait(false);
        }

        public async Task<bool> DeleteAsync(string streamId)
        {
            Expect.NotDisposed(isDisposed);
            Expect.NotEmpty(streamId, "streamId");

            var routingInfo = routing.CreateRoutingInfo(streamId);

            return await Execute(async () =>
            {
                var conn = await GetOrCreateOpenedConnection(routingInfo).ConfigureAwait(false);
                using (var cmd = SqlCommands.Delete(streamId, routingInfo.Partition, schema))
                {
                    cmd.Connection = conn;
                    var affected = await cmd.ExecuteNonQueryAsync().ConfigureAwait(false);
                    return affected == 1;
                }
            }).ConfigureAwait(false);
        }

        private async Task<T> Execute<T>(Func<Task<T>> exec)
        {
            try
            {
                if (!SuppressAmbientTransaction)
                {
                    return await exec().ConfigureAwait(false);
                }

                using (new TransactionScope(TransactionScopeOption.Suppress, TransactionScopeAsyncFlowOption.Enabled))
                {
                    return await exec().ConfigureAwait(false);
                }
            }
            catch (Exception)
            {
                CloseAllConnections();
                throw;
            }
        }

        private async Task<NpgsqlConnection> GetOrCreateOpenedConnection(RoutingInfo routingInfo)
        {
            var init = StorageInitialization.Current;
            if (connections.TryGetValue(routingInfo.HostAndPort, out NpgsqlConnection connection))
            {
                await init.EnsureStorageIsReadyAsync(connection, schema, routingInfo.Partition).ConfigureAwait(false);
                return connection;
            }

            connection = connectionFactory.CreateConnection(routingInfo);
            await init.EnsureStorageIsReadyAsync(connection, schema, routingInfo.Partition).ConfigureAwait(false);
            await connection.OpenAsync().ConfigureAwait(false);

            connections.TryAdd(routingInfo.HostAndPort, connection);

            return connection;
        }

        private static VersionConflictException ToVersionConflict(PostgresException ex)
        {
            string streamId = null;
            var version = 0;
            if (ex.Detail != null)
            {
                var match = NpgsqlExDetailRx.Match(ex.Detail);
                if (match.Success)
                {
                    streamId = match.Groups["streamId"].Value;
                    _ = int.TryParse(match.Groups["version"].Value, out version);
                }
            }

            return new VersionConflictException(streamId, version) { Detail = ex.TableName };
        }
    }
}

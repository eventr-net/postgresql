namespace EventR.PostgreSql
{
    using Npgsql;
    using System.Collections.Generic;
    using System.Data;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Cache for executed storage initializations.
    /// </summary>
    internal sealed class StorageInitialization
    {
        private static readonly HashSet<string> FinishedInits = new HashSet<string>();
        private static readonly SemaphoreSlim InitLock = new SemaphoreSlim(1, 1);

        public static readonly StorageInitialization Current = new StorageInitialization();

        private StorageInitialization()
        {
        }

        /// <summary>
        /// Creates partition (table) in the host if it does not exists.
        /// It also creates database schema if it is missing (before it creates the table).
        /// </summary>
        /// <param name="connection">database connection string</param>
        /// <param name="schema">database schema name</param>
        /// <param name="partition">partition number</param>
        public async ValueTask<bool> EnsureStorageIsReadyAsync(NpgsqlConnection connection, string schema, int partition = 0)
        {
            var key = $"{connection.Host}:{connection.Port}/{schema}/{partition}";
            if (FinishedInits.Contains(key))
            {
                return false;
            }

            try
            {
                await InitLock.WaitAsync().ConfigureAwait(false);

                if (FinishedInits.Contains(key))
                {
                    return false;
                }

                try
                {
                    // Initialization of internal database tables needs to be created in new dedicated connection
                    // which doesn't enlist to any ambient transactions.
                    var connBuilder = new NpgsqlConnectionStringBuilder(connection.ConnectionString)
                    {
                        Enlist = false,
                    };

                    var owner = connection.GetUserId();
                    using (var conn = new NpgsqlConnection(connBuilder.ToString()))
                    using (var cmd = SqlCommands.EnsureStorage(partition, owner, schema))
                    {
                        await conn.OpenAsync().ConfigureAwait(false);
                        cmd.Connection = conn;
                        await cmd.ExecuteNonQueryAsync().ConfigureAwait(false);
                    }
                }
                catch (PostgresException ex)
                {
                    if (ex.SqlState != "42P07") // 42P07 = duplicate table
                    {
                        throw;
                    }
                }

                FinishedInits.Add(key);
                return true;
            }
            finally
            {
                InitLock.Release();
            }
        }
    }
}

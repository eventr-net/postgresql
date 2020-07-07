namespace EventR.PostgreSql
{
    using Npgsql;
    using System;
    using System.Linq;
    using System.Threading.Tasks;

    internal static class Util
    {
        internal const string LocalConnectionString = "Server=localhost; Port=5432; Database=eventr; User Id=postgres; Password=Password12!; ApplicationName=EventR.PostgreSql.Tests";

        internal static string EnsureStorageForTests(string connectionString, string schema = null)
        {
            schema ??= "test" + Guid.NewGuid().ToString("N").Substring(0, 8);
            var cb = new NpgsqlConnectionStringBuilder(connectionString)
            {
                Enlist = false,
            };
            try
            {
                using (var conn = new NpgsqlConnection(cb.ToString()))
                {
                    conn.Open();
                    using (var cmd = SqlCommands.EnsureSchema(schema, cb.Username))
                    {
                        cmd.Connection = conn;
                        cmd.ExecuteNonQuery();
                    }

                    using (var cmd = SqlCommands.EnsureStorage(schema))
                    {
                        cmd.Connection = conn;
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch (PostgresException ex)
            {
                if (!IsThrownBecauseItAlreadyExists(ex))
                {
                    throw;
                }
            }

            return schema;
        }

        internal static void DeleteStorageAfterTests(string connectionString, string schema)
        {
            var connBuilder = new NpgsqlConnectionStringBuilder(connectionString)
            {
                Enlist = false,
            };

            using (var conn = new NpgsqlConnection(connBuilder.ToString()))
            using (var cmd = SqlCommands.DropSchemaIfExists(schema))
            {
                conn.Open();
                cmd.Connection = conn;
                cmd.ExecuteNonQuery();
            }
        }

        private static bool IsThrownBecauseItAlreadyExists(PostgresException ex)
        {
            return ex.SqlState == "42P07"; // DUPLICATE TABLE
        }

        internal static string EnsureStorageForTests(string connectionString, PartitionMap partitionMap)
        {
            var schema = "test" + Guid.NewGuid().ToString("N").Substring(0, 8);
            var source = partitionMap.Hosts.Select(x =>
            {
                var cb = new NpgsqlConnectionStringBuilder(connectionString)
                {
                    Host = x.Host,
                    Port = x.Port,
                    Enlist = false,
                };
                return (Builder: cb, x.Partitions);
            });

            Parallel.ForEach(source, src =>
            {
                using (var conn = new NpgsqlConnection(src.Builder.ToString()))
                {
                    conn.Open();
                    using (var cmd = SqlCommands.EnsureSchema(schema, src.Builder.Username))
                    {
                        cmd.Connection = conn;
                        cmd.ExecuteNonQuery();
                    }

                    using (var cmd = SqlCommands.EnsureStorage(src.Partitions, schema))
                    {
                        cmd.Connection = conn;
                        cmd.ExecuteNonQuery();
                    }
                }
            });

            return schema;
        }

        internal static void DeleteStorageAfterTests(string connectionString, string schema, PartitionMap partitionMap)
        {
            var connStrings = partitionMap.Hosts.Select(x =>
            {
                var cb = new NpgsqlConnectionStringBuilder(connectionString)
                {
                    Host = x.Host,
                    Port = x.Port,
                    Enlist = false,
                };
                return cb.ToString();
            });
            Parallel.ForEach(connStrings, cs => DeleteStorageAfterTests(cs, schema));
        }
    }
}

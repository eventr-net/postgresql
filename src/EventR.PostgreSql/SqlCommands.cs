namespace EventR.PostgreSql
{
#pragma warning disable CA2100 // Review SQL queries for security vulnerabilities

    using EventR.Abstractions;
    using Npgsql;
    using System;
    using System.Text;

    /// <summary>
    /// Factory for <see cref="NpgsqlCommand"/>s encapsulating actual SQL strings and parameter handling.
    /// </summary>
    internal static class SqlCommands
    {
        internal static NpgsqlCommand Load(string streamId, int partition, string schema)
        {
            var sql = string.Format(
                "SELECT id, streamid, items, version, serializer, payload, payload_layout FROM {0}.commits{1} WHERE streamid = :streamId",
                schema,
                partition > 0 ? ("_" + partition) : string.Empty);

            var cmd = new NpgsqlCommand(sql);
            cmd.Parameters.AddWithValue("streamId", streamId);
            return cmd;
        }

        internal static NpgsqlCommand Save(Commit commit, int partition, string schema)
        {
            const string SqlPattern =
                "INSERT INTO {0}.commits{1} (id, streamid, items, version, serializer, payload, payload_layout) "
                + "VALUES (:id, :streamid, :items, :version, :serializer, :payload, :payload_layout)";
            var sql = string.Format(SqlPattern, schema, partition > 0 ? ("_" + partition) : string.Empty);
            var cmd = new NpgsqlCommand(sql);
            cmd.Parameters.AddWithValue("id", commit.Id.ToGuid());
            cmd.Parameters.AddWithValue("streamid", commit.StreamId);
            cmd.Parameters.AddWithValue("items", commit.ItemsCount);
            cmd.Parameters.AddWithValue("version", commit.Version);
            cmd.Parameters.AddWithValue("serializer", commit.SerializerId);
            cmd.Parameters.AddWithValue("payload", commit.Payload);
            if (commit.HasPayloadLayout)
            {
                cmd.Parameters.AddWithValue("payload_layout", commit.PayloadLayout.ToBytes());
            }
            else
            {
                cmd.Parameters.AddWithValue("payload_layout", DBNull.Value);
            }

            return cmd;
        }

        internal static NpgsqlCommand Delete(string streamId, int partition, string schema)
        {
            var sql = string.Format(
                "DELETE FROM {0}.commits{1} WHERE streamid = :streamid",
                schema,
                partition > 0 ? ("_" + partition) : string.Empty);

            var cmd = new NpgsqlCommand(sql);
            cmd.Parameters.AddWithValue("streamid", streamId);
            return cmd;
        }

        private const string TablePattern =
@"IF NOT EXISTS (SELECT 1 FROM pg_catalog.pg_class c JOIN pg_catalog.pg_namespace n ON n.oid = c.relnamespace WHERE n.nspname = '{0}' AND c.relname = 'commits{1}') THEN
    CREATE TABLE {0}.commits{1} (
        id UUID NOT NULL,
        streamid VARCHAR(100) NOT NULL,
        items SMALLINT NOT NULL,
        version INTEGER NOT NULL,
        serializer VARCHAR(20),
        payload BYTEA NOT NULL,
        payload_layout BYTEA,
        created TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
        CONSTRAINT commits{1}_pk PRIMARY KEY(id)
    )
    WITH (oids = false);
    CREATE UNIQUE INDEX commits{1}_streamid_version_uq ON {0}.commits{1} USING btree (streamid, version);
END IF;";

        internal static NpgsqlCommand EnsureStorage(string schema)
        {
            var sql = string.Format($"DO $$BEGIN\r\n{TablePattern}\r\nEND$$;", schema, string.Empty);
            return new NpgsqlCommand(sql);
        }

        internal static NpgsqlCommand EnsureStorage(int partition, string owner, string schema)
        {
            var p = partition > 0 ? $"_{partition}" : string.Empty;
            var sql = string.Format($"DO $$BEGIN\r\nCREATE SCHEMA IF NOT EXISTS {schema} AUTHORIZATION {owner};\r\n{TablePattern}\r\nEND$$;", schema, p);
            return new NpgsqlCommand(sql);
        }

        internal static NpgsqlCommand EnsureStorage(int[] partitions, string schema)
        {
            var sb = new StringBuilder();
            sb.AppendLine("DO $$BEGIN");
            foreach (var partition in partitions)
            {
                sb.AppendFormat(TablePattern, schema, $"_{partition}");
                sb.AppendLine();
            }

            sb.Append(@"END$$;");
            return new NpgsqlCommand(sb.ToString());
        }

        internal static NpgsqlCommand EnsureSchema(string schema, string owner)
        {
            return new NpgsqlCommand($"CREATE SCHEMA IF NOT EXISTS {schema} AUTHORIZATION {owner}");
        }

        internal static NpgsqlCommand BlankSlate(string owner, string schema)
        {
            const string SqlPattern = @"DO $$
BEGIN
    IF EXISTS(SELECT 1 FROM pg_namespace WHERE nspname = '{0}') THEN
		DROP SCHEMA {0} CASCADE;
    END IF;
    CREATE SCHEMA {0} AUTHORIZATION {1};
END$$;";
            var sql = string.Format(SqlPattern, schema, owner);
            return new NpgsqlCommand(sql);
        }

        internal static NpgsqlCommand DropSchemaIfExists(string schema)
        {
            const string SqlPattern = @"DO $$
BEGIN
    IF EXISTS(SELECT 1 FROM pg_namespace WHERE nspname = '{0}') THEN
		DROP SCHEMA {0} CASCADE;
    END IF;
END$$;";
            var sql = string.Format(SqlPattern, schema);
            return new NpgsqlCommand(sql);
        }

        internal static NpgsqlCommand AllStreamIdsInPartition(int partition, string schema)
        {
            var sql = string.Format(
                "SELECT DISTINCT streamid FROM {0}.commits{1}",
                schema,
                partition > 0 ? ("_" + partition) : string.Empty);
            return new NpgsqlCommand(sql);
        }

        internal static NpgsqlCommand Tables(string tableNameStartsWith, string schema)
        {
            var sql = string.Format(
                "SELECT table_name FROM information_schema.tables WHERE table_schema = '{0}' AND table_name LIKE '{1}%' AND table_type = 'BASE TABLE'",
                schema,
                tableNameStartsWith);
            return new NpgsqlCommand(sql);
        }
    }
#pragma warning restore CA2100 // Review SQL queries for security vulnerabilities
}

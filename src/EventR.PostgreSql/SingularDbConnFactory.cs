namespace EventR.PostgreSql
{
    using EventR.Abstractions;
    using Npgsql;
    using System;

    /// <summary>
    /// Database connection factory that ignores routing information.
    /// Useful when sharding is not required and there is only one database.
    /// </summary>
    public class SingularDbConnFactory : IDbConnectionFactory
    {
        private readonly string connectionString;

        public SingularDbConnFactory(string connectionString)
        {
            Expect.NotEmpty(connectionString, nameof(connectionString));
            this.connectionString = Environment.ExpandEnvironmentVariables(connectionString);
        }

        public NpgsqlConnection CreateConnection(RoutingInfo routingInfo)
        {
            var cb = new NpgsqlConnectionStringBuilder(connectionString)
            {
                Enlist = true,
                PersistSecurityInfo = true,
            };
            return new NpgsqlConnection(cb.ToString());
        }
    }
}

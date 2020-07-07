namespace EventR.PostgreSql
{
    using EventR.Abstractions;
    using Npgsql;
    using System;

    /// <summary>
    /// Database connection factory which cares about routing information.
    /// It uses template of a connection string and merge it with the information
    /// from the routing such as host and port.
    /// </summary>
    public class RoutedDbConnFactory : IDbConnectionFactory
    {
        private readonly string templateConnString;

        public RoutedDbConnFactory(string connectionString)
        {
            Expect.NotEmpty(connectionString, nameof(connectionString));
            templateConnString = Environment.ExpandEnvironmentVariables(connectionString);
        }

        public NpgsqlConnection CreateConnection(RoutingInfo routingInfo)
        {
            var cb = new NpgsqlConnectionStringBuilder(templateConnString)
            {
                Host = routingInfo.Host,
                Port = routingInfo.Port,
                Enlist = true,
                PersistSecurityInfo = true,
            };
            return new NpgsqlConnection(cb.ToString());
        }
    }
}

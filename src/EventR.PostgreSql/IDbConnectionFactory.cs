namespace EventR.PostgreSql
{
    using Npgsql;

    /// <summary>
    /// Provides database connection strings.
    /// </summary>
    public interface IDbConnectionFactory
    {
        /// <summary>
        /// Creates database connection based on routing information.
        /// </summary>
        NpgsqlConnection CreateConnection(RoutingInfo routingInfo);
    }
}

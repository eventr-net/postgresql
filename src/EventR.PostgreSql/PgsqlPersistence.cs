namespace EventR.PostgreSql
{
    using EventR.Abstractions;

    public sealed class PgsqlPersistence : IPersistence
    {
        private readonly IDbConnectionFactory connectionFactory;
        private readonly IProvideRoutingInfo routing;
        private readonly string schema;

        public PgsqlPersistence(IDbConnectionFactory connectionFactory, IProvideRoutingInfo routingProvider, string schema = "es")
        {
            this.connectionFactory = connectionFactory;
            routing = routingProvider;
            this.schema = schema;
        }

        public IPersistenceSession OpenSession(bool suppressAmbientTransaction = false)
        {
            return new PgsqlPersistenceSession(connectionFactory, routing, suppressAmbientTransaction, schema);
        }

        public void Dispose()
        { }
    }
}

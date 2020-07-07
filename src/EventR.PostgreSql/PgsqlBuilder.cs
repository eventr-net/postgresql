namespace EventR.PostgreSql
{
    using EventR.Abstractions;

    /// <summary>
    /// Fluent API configuration related to PostgreSql module features.
    /// </summary>
    public class PgsqlBuilder : BuilderBase
    {
        public PgsqlBuilder(ConfigurationContext context)
            : base(context)
        {
            context.PersistenceFactory = () => CreatePersistence();
        }

        private bool sharding;
        private string connectionString;
        private string schema;
        private PartitionMap partitionMap;

        public PgsqlBuilder WithSharding(PartitionMap partitionMap)
        {
            Expect.NotNull(partitionMap, nameof(partitionMap));
            sharding = true;
            this.partitionMap = partitionMap;
            partitionMap.ValidateOrThrow();
            return this;
        }

        public PgsqlBuilder ConnectionStringOrName(string connectionString)
        {
            Expect.NotEmpty(connectionString, nameof(connectionString));
            this.connectionString = connectionString;
            return this;
        }

        public PgsqlBuilder Schema(string schema)
        {
            Expect.NotEmpty(schema, nameof(schema));
            this.schema = schema;
            return this;
        }

        private IPersistence CreatePersistence()
        {
            IDbConnectionFactory connFactory;
            IProvideRoutingInfo routing;
            if (sharding)
            {
                routing = new HashRangeRouting(partitionMap);
                connFactory = new RoutedDbConnFactory(connectionString);
            }
            else
            {
                routing = new VoidRouting();
                connFactory = new SingularDbConnFactory(connectionString);
            }

            return new PgsqlPersistence(connFactory, routing, schema);
        }
    }
}

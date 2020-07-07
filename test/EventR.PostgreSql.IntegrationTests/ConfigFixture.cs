namespace EventR.PostgreSql.IntegrationTests
{
    using Microsoft.Extensions.Configuration;
    using System;

    public sealed class ConfigFixture
    {
        public IConfiguration Config { get; }

        public string ConnectionString { get; }

        public PartitionMap PartitionMap { get; }

        public ConfigFixture()
        {
            var cfg = new ConfigurationBuilder().SetBasePath(AppContext.BaseDirectory);
            cfg.AddJsonFile("appsettings.json", optional: false, reloadOnChange: false);
            cfg.AddJsonFile("appsettings.dev.json", optional: true, reloadOnChange: false);
            Config = cfg.Build();

            ConnectionString = Config.GetValue("eventr:postgresql:connectionString", Util.LocalConnectionString);
            PartitionMap = PartitionMap.FromConfig(Config, "eventr:postgresql:partitionMap");
            PartitionMap.ValidateOrThrow();
        }
    }
}

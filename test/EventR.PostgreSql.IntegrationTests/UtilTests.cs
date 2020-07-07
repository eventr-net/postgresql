namespace EventR.PostgreSql.IntegrationTests
{
    using Xunit;

    public class UtilTests : IClassFixture<ConfigFixture>
    {
        private readonly ConfigFixture fixture;

        public UtilTests(ConfigFixture fixture)
        {
            this.fixture = fixture;
        }

        [Fact]
        public void CreateAndDeleteStorageForTests()
        {
            var schema = Util.EnsureStorageForTests(fixture.ConnectionString);
            Assert.NotNull(schema);

            Util.DeleteStorageAfterTests(fixture.ConnectionString, schema);
        }

        [Fact]
        public void CreateAndDeleteStorage3ForTests()
        {
            var schema = Util.EnsureStorageForTests(fixture.ConnectionString, fixture.PartitionMap);
            Assert.NotNull(schema);
            Util.DeleteStorageAfterTests(fixture.ConnectionString, schema, fixture.PartitionMap);
        }
    }
}

namespace EventR.PostgreSql.IntegrationTests
{
    using EventR.Spec.Persistence;

    public sealed class PgsqlPersistenceTests : PersistenceSpec<PgsqlPersistenceFixture>
    {
        public PgsqlPersistenceTests(PgsqlPersistenceFixture fixture)
            : base(fixture)
        { }
    }
}

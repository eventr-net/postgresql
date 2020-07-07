namespace EventR.PostgreSql.IntegrationTests
{
    using EventR.Spec.Persistence;

    public sealed class Pgsql3NodesPersistenceTests : PersistenceSpec<Pgsql3NodesPersistenceFixture>
    {
        public Pgsql3NodesPersistenceTests(Pgsql3NodesPersistenceFixture fixture)
            : base(fixture)
        { }
    }
}

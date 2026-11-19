namespace ElsasaSis.IntegrationTests.Fixtures;

[CollectionDefinition(Name)]
public sealed class IntegrationTestCollection : ICollectionFixture<IntegrationTestWebApplicationFactory>
{
    public const string Name = "IntegrationTests";
}

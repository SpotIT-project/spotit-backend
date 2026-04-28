using Xunit;

namespace SpotIt.IntegrationTests.Infrastructure;

[CollectionDefinition("Database")]
public class DatabaseCollection : ICollectionFixture<SpotItWebApplicationFactory> {}

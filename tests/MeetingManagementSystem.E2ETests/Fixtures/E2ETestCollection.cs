namespace MeetingManagementSystem.E2ETests.Fixtures;

/// <summary>
/// Collection definition for E2E tests that share PostgreSQL container, WebApplicationFactory, and Playwright instances.
/// Each collection gets its own isolated database container.
/// </summary>
[CollectionDefinition("E2E Collection")]
public class E2ETestCollection : ICollectionFixture<PostgreSqlContainerFixture>, 
                                  ICollectionFixture<PlaywrightFixture>
{
    // This class is just a marker for xUnit to create the collection
    // The actual fixtures are created by xUnit's dependency injection
}

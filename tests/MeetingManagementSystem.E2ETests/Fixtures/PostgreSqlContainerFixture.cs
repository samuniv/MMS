using Testcontainers.PostgreSql;

namespace MeetingManagementSystem.E2ETests.Fixtures;

/// <summary>
/// PostgreSQL container fixture that creates a new isolated database container per test collection.
/// Implements IAsyncLifetime for proper async initialization and cleanup.
/// </summary>
public class PostgreSqlContainerFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container;

    public string ConnectionString => _container.GetConnectionString();

    public PostgreSqlContainerFixture()
    {
        _container = new PostgreSqlBuilder()
            .WithImage("postgres:16-alpine")
            .WithDatabase("meetingmanagement_test")
            .WithUsername("postgres")
            .WithPassword("postgres")
            .WithCleanUp(true)
            .Build();
    }

    public async Task InitializeAsync()
    {
        await _container.StartAsync();
    }

    public async Task DisposeAsync()
    {
        await _container.DisposeAsync();
    }
}

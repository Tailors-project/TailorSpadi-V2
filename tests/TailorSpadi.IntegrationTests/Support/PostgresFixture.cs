using Npgsql;
using Testcontainers.PostgreSql;

namespace TailorSpadi.IntegrationTests.Support;

/// <summary>One PostgreSQL container for all database tests; each factory gets its own empty database.</summary>
public sealed class PostgresFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:17-alpine").Build();

    public Task InitializeAsync() => _postgres.StartAsync();

    public Task DisposeAsync() => _postgres.DisposeAsync().AsTask();

    public string AdminConnectionString => _postgres.GetConnectionString();

    public async Task<string> CreateDatabaseAsync()
    {
        string name = "t_" + Guid.NewGuid().ToString("N");
        await using var connection = new NpgsqlConnection(AdminConnectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand($"CREATE DATABASE \"{name}\"", connection);
        await command.ExecuteNonQueryAsync();

        // Each test owns a small pool, cleared when its factory is disposed.
        var builder = new NpgsqlConnectionStringBuilder(AdminConnectionString)
        {
            Database = name,
            MaxPoolSize = 10,
            Timeout = 60,
            CommandTimeout = 60
        };
        return builder.ConnectionString;
    }
}

[CollectionDefinition(Name)]
public sealed class DatabaseCollection : ICollectionFixture<PostgresFixture>
{
    public const string Name = "Database";
}

using Npgsql;

namespace TailorSpadi.IntegrationTests.Support;

/// <summary>Starts the API against its own empty database for each test.</summary>
[Collection(DatabaseCollection.Name)]
[Trait("Category", "Database")]
public abstract class DatabaseTestBase(PostgresFixture postgres) : IAsyncLifetime
{
    private string _connectionString = string.Empty;

    protected virtual IDictionary<string, string?>? Overrides => null;

    protected virtual TimeProvider? Clock => null;

    protected ApiFactory Factory { get; private set; } = null!;

    protected TestApp App { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        _connectionString = await postgres.CreateDatabaseAsync();
        Factory = new ApiFactory(_connectionString, Overrides, Clock);
        App = new TestApp(Factory);
        await App.EnsureDatabaseAsync();
    }

    public async Task DisposeAsync()
    {
        App.Dispose();
        await Factory.DisposeAsync();
        using var connection = new NpgsqlConnection(_connectionString);
        NpgsqlConnection.ClearPool(connection);
    }
}

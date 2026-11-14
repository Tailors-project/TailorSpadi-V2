using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using TailorSpadi.Infrastructure.Persistence;
using TailorSpadi.IntegrationTests.Support;

namespace TailorSpadi.IntegrationTests;

/// <summary>Applies the committed migrations to an empty database; fails until InitialIdentityAndTenancy exists.</summary>
[Collection(DatabaseCollection.Name)]
[Trait("Category", "Database")]
public sealed class MigrationTests(PostgresFixture postgres)
{
    [Fact]
    public async Task InitialIdentityAndTenancyMigrationAppliesAndMatchesTheModel()
    {
        string connectionString = await postgres.CreateDatabaseAsync();
        await using var factory = new ApiFactory(connectionString);
        using IServiceScope scope = factory.Services.CreateScope();
        AppDbContext db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        Assert.Contains(db.Database.GetMigrations(), name => name.EndsWith("InitialIdentityAndTenancy", StringComparison.Ordinal));
        await db.Database.MigrateAsync();
        Assert.Empty(await db.Database.GetPendingMigrationsAsync());
        Assert.False(db.Database.HasPendingModelChanges());

        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(
            "SELECT count(*) FROM pg_indexes WHERE indexname ILIKE '%NormalizedEmail%' OR indexname ILIKE '%Subdomain%'", connection);
        Assert.True((long)(await command.ExecuteScalarAsync())! >= 2);
    }
}

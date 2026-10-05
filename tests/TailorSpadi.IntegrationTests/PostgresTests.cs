using System.Net;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using TailorSpadi.Infrastructure.Persistence;
using Testcontainers.PostgreSql;

namespace TailorSpadi.IntegrationTests;

[Trait("Category", "Database")]
public sealed class PostgresTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:17-alpine")
        .Build();

    public Task InitializeAsync() => _postgres.StartAsync();
    public Task DisposeAsync() => _postgres.DisposeAsync().AsTask();

    [Fact]
    public async Task RealDatabaseIsReadyAndPreservesUtcAndCalendarDates()
    {
        await using var factory = new ApiFactory(_postgres.GetConnectionString());
        using HttpClient client = factory.CreateClient();
        using HttpResponseMessage response = await client.GetAsync("/health/ready");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using IServiceScope scope = factory.Services.CreateScope();
        AppDbContext dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.True(await dbContext.Database.CanConnectAsync());
        Assert.Empty(dbContext.Model.GetEntityTypes());

        await using var connection = new NpgsqlConnection(_postgres.GetConnectionString());
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand("SELECT @instant::timestamptz, @day::date", connection);
        var instant = new DateTime(2026, 9, 27, 23, 30, 0, DateTimeKind.Utc);
        var day = new DateOnly(2026, 9, 28);
        command.Parameters.AddWithValue("instant", instant);
        command.Parameters.AddWithValue("day", day);
        await using NpgsqlDataReader reader = await command.ExecuteReaderAsync();
        Assert.True(await reader.ReadAsync());
        Assert.Equal(instant, reader.GetDateTime(0));
        Assert.Equal(DateTimeKind.Utc, reader.GetDateTime(0).Kind);
        Assert.Equal(day, reader.GetFieldValue<DateOnly>(1));
    }
}

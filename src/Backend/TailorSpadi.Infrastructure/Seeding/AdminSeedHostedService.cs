using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TailorSpadi.Application.Abstractions;
using TailorSpadi.Application.Options;
using TailorSpadi.Application.Security;
using TailorSpadi.Domain.Identity;

namespace TailorSpadi.Infrastructure.Seeding;

/// <summary>
/// Creates the first platform administrator when, and only when, it is explicitly enabled in configuration.
/// There are no default credentials, an existing account is never overwritten, and nothing secret is logged.
/// </summary>
public sealed class AdminSeedHostedService(
    IServiceScopeFactory scopes, IOptions<AuthOptions> options, TimeProvider clock, ILogger<AdminSeedHostedService> logger)
    : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        AdminSeedOptions seed = options.Value.AdminSeed;
        if (!seed.Enabled)
        {
            return;
        }

        string? problem = PasswordPolicy.Validate(seed.Password, seed.Email);
        if (string.IsNullOrWhiteSpace(seed.Email) || problem is not null)
        {
            throw new InvalidOperationException("Auth:AdminSeed is enabled but its email or password is missing or too weak.");
        }

        try
        {
            using IServiceScope scope = scopes.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<IAppDbContext>();
            var passwords = scope.ServiceProvider.GetRequiredService<IPasswordService>();

            string normalized = User.NormalizeEmail(seed.Email);
            if (await db.Users.AnyAsync(user => user.NormalizedEmail == normalized, cancellationToken))
            {
                logger.LogInformation("Platform administrator seed skipped: the account already exists.");
                return;
            }

            var admin = new User
            {
                Id = Guid.NewGuid(),
                Email = seed.Email.Trim(),
                NormalizedEmail = normalized,
                FirstName = seed.FirstName,
                LastName = seed.LastName,
                IsPlatformAdmin = true,
                EmailConfirmed = true,
                CreatedAtUtc = clock.GetUtcNow().UtcDateTime
            };
            admin.PasswordHash = passwords.Hash(admin, seed.Password);
            db.Users.Add(admin);
            await db.SaveChangesAsync(cancellationToken);
            logger.LogInformation("Platform administrator account created.");
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogError("Platform administrator seed failed with {ExceptionType}.", exception.GetType().Name);
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}

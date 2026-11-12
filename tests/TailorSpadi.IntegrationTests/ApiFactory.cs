using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using TailorSpadi.Application.Abstractions;
using TailorSpadi.IntegrationTests.Support;

namespace TailorSpadi.IntegrationTests;

public sealed class ApiFactory(
    string connectionString,
    IDictionary<string, string?>? overrides = null,
    TimeProvider? clock = null) : WebApplicationFactory<Program>
{
    public const string SigningKey = "test-key-k8Qz3vN0xLp2Rt7YwB5mHc9JdFa4UeSg";

    public string StorageRoot { get; } = Path.Combine(Path.GetTempPath(), "tailorspadi-tests", Guid.NewGuid().ToString("N"));

    public CapturingEmailSender Emails { get; } = new();

    public void InvalidateTenantCache(string subdomain) =>
        Services.GetRequiredService<TailorSpadi.Application.Abstractions.ITenantCache>().Invalidate(subdomain);

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureAppConfiguration((context, configuration) =>
        {
            var values = new Dictionary<string, string?>
            {
                ["ConnectionStrings:Postgres"] = connectionString,
                ["Auth:SigningKey"] = SigningKey,
                ["Auth:CookieSecure"] = "false",
                ["Storage:Root"] = StorageRoot,
                ["RateLimiting:LoginPerMinute"] = "100000",
                ["RateLimiting:RegisterPerMinute"] = "100000",
                ["RateLimiting:PasswordResetPerMinute"] = "100000",
                ["RateLimiting:RefreshPerMinute"] = "100000",
                ["RateLimiting:SubdomainPerMinute"] = "100000",
                ["RateLimiting:UploadPerMinute"] = "100000"
            };

            foreach (KeyValuePair<string, string?> pair in overrides ?? new Dictionary<string, string?>())
            {
                values[pair.Key] = pair.Value;
            }

            configuration.AddInMemoryCollection(values);
        });

        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IEmailSender>();
            services.AddSingleton<IEmailSender>(Emails);
            if (clock is not null)
            {
                services.RemoveAll<TimeProvider>();
                services.AddSingleton(clock);
            }
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing && Directory.Exists(StorageRoot))
        {
            Directory.Delete(StorageRoot, recursive: true);
        }
    }
}

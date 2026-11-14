using System.Globalization;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;
using TailorSpadi.Api.Errors;
using TailorSpadi.Application.Common;

namespace TailorSpadi.Api.Security;

public sealed class RateLimitSettings
{
    public const string SectionName = "RateLimiting";

    public int LoginPerMinute { get; set; } = 10;
    public int RegisterPerMinute { get; set; } = 5;
    public int PasswordResetPerMinute { get; set; } = 5;
    public int RefreshPerMinute { get; set; } = 60;
    public int SubdomainPerMinute { get; set; } = 60;
    public int UploadPerMinute { get; set; } = 10;
}

public static class RateLimitingSetup
{
    public static IServiceCollection AddTailorSpadiRateLimiting(this IServiceCollection services)
    {
        services.AddOptions<RateLimitSettings>().BindConfiguration(RateLimitSettings.SectionName);

        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.OnRejected = async (context, cancellationToken) =>
            {
                if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out TimeSpan retryAfter))
                {
                    context.HttpContext.Response.Headers.RetryAfter =
                        ((int)Math.Ceiling(retryAfter.TotalSeconds)).ToString(CultureInfo.InvariantCulture);
                }

                await ProblemFactory.Result(
                        context.HttpContext,
                        new ProblemException(StatusCodes.Status429TooManyRequests, "rate_limited", "Too many requests. Please wait and try again."))
                    .ExecuteAsync(context.HttpContext);
            };
        });

        // Limits come from lazily bound options, so deployment and test configuration both apply.
        services.AddOptions<RateLimiterOptions>().Configure<IOptions<RateLimitSettings>>((options, configured) =>
        {
            RateLimitSettings settings = configured.Value;
            AddPolicy(options, RateLimitPolicies.Login, settings.LoginPerMinute);
            AddPolicy(options, RateLimitPolicies.Register, settings.RegisterPerMinute);
            AddPolicy(options, RateLimitPolicies.PasswordReset, settings.PasswordResetPerMinute);
            AddPolicy(options, RateLimitPolicies.Refresh, settings.RefreshPerMinute);
            AddPolicy(options, RateLimitPolicies.Subdomain, settings.SubdomainPerMinute);
            AddPolicy(options, RateLimitPolicies.Upload, settings.UploadPerMinute);
        });

        return services;
    }

    private static void AddPolicy(RateLimiterOptions options, string name, int permitsPerMinute)
    {
        // Partitioned by caller address (after trusted forwarded-header processing) and policy.
        options.AddPolicy(name, context => RateLimitPartition.GetFixedWindowLimiter(
            context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = permitsPerMinute,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
                AutoReplenishment = true
            }));
    }
}

using FluentValidation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TailorSpadi.Application.Abstractions;
using TailorSpadi.Application.Features.Identity;
using TailorSpadi.Application.Features.Onboarding;
using TailorSpadi.Application.Options;
using TailorSpadi.Application.Security;
using TailorSpadi.Application.Tenancy;
using TailorSpadi.Application.Time;

namespace TailorSpadi.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddValidatorsFromAssemblyContaining<BusinessTime>();
        services.AddOptions<BusinessTimeOptions>()
            .Bind(configuration.GetSection("BusinessTime"))
            .Validate(options => IsValidTimeZone(options.DefaultTimeZone), "DefaultTimeZone must be a supported time zone.")
            .ValidateOnStart();
        services.AddSingleton<BusinessTime>();
        services.AddSingleton(TimeProvider.System);

        services.Configure<AuthOptions>(configuration.GetSection(AuthOptions.SectionName));
        services.Configure<TenancyOptions>(configuration.GetSection(TenancyOptions.SectionName));
        services.Configure<StorageOptions>(configuration.GetSection(StorageOptions.SectionName));

        // One tenant context per request, filled by the tenant resolution middleware.
        services.AddScoped<TenantContext>();
        services.AddScoped<ITenantContext>(provider => provider.GetRequiredService<TenantContext>());

        services.AddScoped<SessionService>();
        services.AddScoped<SessionValidator>();
        services.AddScoped<AccountRegistrar>();
        services.AddScoped<OnboardingService>();
        services.AddScoped<LogoService>();
        services.AddScoped<SubdomainAllocator>();
        return services;
    }

    private static bool IsValidTimeZone(string timeZoneId)
    {
        return !string.IsNullOrWhiteSpace(timeZoneId) && TimeZoneInfo.TryFindSystemTimeZoneById(timeZoneId, out _);
    }
}

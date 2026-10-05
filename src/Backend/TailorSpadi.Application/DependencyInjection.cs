using FluentValidation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
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
        return services;
    }

    private static bool IsValidTimeZone(string timeZoneId)
    {
        return !string.IsNullOrWhiteSpace(timeZoneId) && TimeZoneInfo.TryFindSystemTimeZoneById(timeZoneId, out _);
    }
}

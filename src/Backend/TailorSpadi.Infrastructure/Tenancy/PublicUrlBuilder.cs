using System.Globalization;
using Microsoft.Extensions.Options;
using TailorSpadi.Application.Abstractions;
using TailorSpadi.Application.Options;

namespace TailorSpadi.Infrastructure.Tenancy;

public sealed class PublicUrlBuilder(IOptions<TenancyOptions> options) : IPublicUrlBuilder
{
    public string Platform(string path) => Build(options.Value.BaseDomain, path);

    public string Tenant(string subdomain, string path) => Build(subdomain + "." + options.Value.BaseDomain, path);

    private string Build(string host, string path)
    {
        TenancyOptions value = options.Value;
        string port = value.PublicPort is { } number
            ? ":" + number.ToString(CultureInfo.InvariantCulture)
            : string.Empty;
        string normalizedPath = path.StartsWith('/') ? path : "/" + path;
        return $"{value.PublicScheme}://{host}{port}{normalizedPath}";
    }
}

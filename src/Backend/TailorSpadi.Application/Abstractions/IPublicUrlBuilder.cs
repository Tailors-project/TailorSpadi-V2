namespace TailorSpadi.Application.Abstractions;

public interface IPublicUrlBuilder
{
    string Platform(string path);
    string Tenant(string subdomain, string path);
}

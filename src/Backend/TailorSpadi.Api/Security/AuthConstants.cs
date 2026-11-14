namespace TailorSpadi.Api.Security;

public static class AuthPolicies
{
    public const string AnySession = "AnySession";
    public const string TenantUser = "TenantUser";
    public const string TenantBackoffice = "TenantBackoffice";
    public const string ManageBusiness = "ManageBusiness";
    public const string PlatformUser = "PlatformUser";
    public const string PlatformAdmin = "PlatformAdmin";
}

public static class RateLimitPolicies
{
    public const string Login = "login";
    public const string Register = "register";
    public const string PasswordReset = "password-reset";
    public const string Refresh = "refresh";
    public const string Subdomain = "subdomain";
    public const string Upload = "upload";
}

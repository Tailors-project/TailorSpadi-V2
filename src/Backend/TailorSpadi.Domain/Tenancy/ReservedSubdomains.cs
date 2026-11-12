namespace TailorSpadi.Domain.Tenancy;

public static class ReservedSubdomains
{
    private static readonly HashSet<string> Names = new(StringComparer.Ordinal)
    {
        "www", "api", "admin", "administrator", "backoffice", "app", "apps", "mail", "email", "smtp", "imap", "pop",
        "ftp", "sftp", "ssh", "static", "cdn", "assets", "media", "images", "files", "support", "help", "billing",
        "login", "logout", "register", "signup", "signin", "onboarding", "account", "accounts", "status", "docs",
        "dev", "staging", "stage", "test", "testing", "demo", "sandbox", "prod", "production", "localhost",
        "tailorspadi", "tailor", "tailors", "storefront", "store", "shop", "web", "root", "system", "security",
        "ns", "ns1", "ns2", "mx", "webmail", "blog", "news", "portal", "dashboard", "console", "internal",
        "health", "metrics", "monitor", "auth", "oauth", "sso", "payments", "payment", "checkout", "orders"
    };

    public static bool IsReserved(string label) => Names.Contains(label);
}

namespace TailorSpadi.Application.Security;

/// <summary>A small bundled deny list of very common passwords that satisfy the minimum length.</summary>
internal static class CommonPasswords
{
    private static readonly HashSet<string> Values = new(StringComparer.OrdinalIgnoreCase)
    {
        "password123", "password1234", "password12345", "passw0rd123", "p@ssw0rd123", "p@ssword123", "password!23",
        "1234567890", "12345678910", "0123456789", "1111111111", "0000000000", "9876543210", "123456789a",
        "a123456789", "abc1234567", "abcd123456", "qwertyuiop", "qwerty1234", "qwerty12345", "qwerty123456",
        "qwertyuiop123", "1q2w3e4r5t", "1q2w3e4r5t6y", "asdfghjkl1", "asdfghjkl123", "zxcvbnm123", "iloveyou123",
        "iloveyou1234", "letmein1234", "letmein12345", "welcome123", "welcome1234", "welcome12345", "admin12345",
        "admin123456", "administrator", "changeme123", "changeme1234", "trustno1234", "monkey12345", "dragon12345",
        "football123", "baseball123", "superman123", "princess123", "sunshine123", "master12345", "login12345",
        "passwordpassword", "mypassword1", "mypassword123", "tailorspadi", "tailorspadi1", "tailorspadi123",
        "fashion1234", "fashion12345", "tailor12345", "nigeria1234", "nigeria12345", "lagos12345", "naija12345",
        "abcdefghij", "abcdefghijk", "aaaaaaaaaa", "1234512345", "12341234123"
    };

    public static bool Contains(string password) => Values.Contains(password);
}

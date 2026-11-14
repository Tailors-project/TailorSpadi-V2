using System.Text.RegularExpressions;

namespace TailorSpadi.Application.Security;

public static class PhonePolicy
{
    public static bool IsValid(string? value)
    {
        string phone = value?.Trim() ?? "";
        int digits = phone.Count(c => c is >= '0' and <= '9');
        return phone.Length <= 32 && digits is >= 7 and <= 15
            && Regex.IsMatch(phone, @"\A\+?(?:[0-9]+|\([0-9]+\))(?:[ -]?(?:[0-9]+|\([0-9]+\)))*\z");
    }
}

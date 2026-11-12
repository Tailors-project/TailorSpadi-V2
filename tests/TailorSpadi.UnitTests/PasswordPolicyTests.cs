using TailorSpadi.Application.Security;

namespace TailorSpadi.UnitTests;

public sealed class PasswordPolicyTests
{
    [Fact]
    public void AcceptsAnUnpredictablePasswordOfSufficientLength()
    {
        Assert.Null(PasswordPolicy.Validate("Sew-Tailor#Lagos-2026"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("short1!")]
    [InlineData("123456789")]
    public void RejectsMissingOrShortPasswords(string? password)
    {
        Assert.NotNull(PasswordPolicy.Validate(password));
    }

    [Fact]
    public void LengthBoundsAre10And128()
    {
        Assert.Null(PasswordPolicy.Validate(new string('a', 5) + "B3$xyz9Q"));
        Assert.NotNull(PasswordPolicy.Validate("abcdefghi"));
        Assert.Null(PasswordPolicy.Validate("Aa1!" + new string('x', 124)));
        Assert.NotNull(PasswordPolicy.Validate("Aa1!" + new string('x', 125)));
    }

    [Theory]
    [InlineData("password123")]
    [InlineData("PASSWORD123")]
    [InlineData("qwertyuiop")]
    [InlineData("1234567890")]
    [InlineData("aaaaaaaaaa")]
    [InlineData("abababababab")]
    public void RejectsCommonOrTriviallySimplePasswords(string password)
    {
        Assert.NotNull(PasswordPolicy.Validate(password));
    }

    [Fact]
    public void RejectsThePasswordWhenItEqualsTheEmail()
    {
        Assert.NotNull(PasswordPolicy.Validate("owner@tailor.example", "OWNER@tailor.example"));
        Assert.Null(PasswordPolicy.Validate("owner@tailor.example", "someone@else.example"));
    }
}

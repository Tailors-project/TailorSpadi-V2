using TailorSpadi.Application.Features.Identity;
using TailorSpadi.Application.Features.Onboarding;
using TailorSpadi.Application.Security;

namespace TailorSpadi.UnitTests;

public sealed class PhoneValidationTests
{
    [Theory]
    [InlineData("", true)]
    [InlineData(" ", true)]
    [InlineData("+234 (901) 234-5678", true)]
    [InlineData("222222222222222222222222", false)]
    [InlineData("-------", false)]
    [InlineData("12+3456789", false)]
    public void ValidatesOptionalWhatsAppNumber(string whatsApp, bool valid)
    {
        var business = new BusinessInfo("Ada Fashion", "", "+2349012345678", whatsApp, "ada@example.com", "", "", "", "Nigeria");
        var result = new SaveBusinessInfoValidator().Validate(new SaveBusinessInfoCommand(business));
        Assert.Equal(valid, result.IsValid);
        if (!valid)
        {
            Assert.Contains(result.Errors, e => e.PropertyName == "Business.WhatsApp");
        }
    }

    [Theory]
    [InlineData("+2349000000000", true)]
    [InlineData("09012345678", true)]
    [InlineData("+234 (901) 234-5678", true)]
    [InlineData("1234567", true)]
    [InlineData("+123456789012345", true)]
    [InlineData("123456", false)]
    [InlineData("+1234567890123456", false)]
    [InlineData("-------", false)]
    [InlineData("12+3456789", false)]
    [InlineData("++2349012345678", false)]
    [InlineData("(1234567", false)]
    [InlineData("phone1234567", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void ChecksPhoneFormat(string? phone, bool valid)
    {
        Assert.Equal(valid, PhonePolicy.IsValid(phone));
    }

    [Fact]
    public void RegistrationRejectsOversizedPhonesAndAcceptsSevenCharacterPasswords()
    {
        var tailor = new RegisterTailorCommand("Ada", "Bello", "ada@example.com", "+2349012345678", "sewing7", "sewing7");
        var customer = new RegisterCustomerCommand("Ada", "Bello", "ada@example.com", "", "sewing7", "sewing7");
        Assert.True(new RegisterTailorValidator().Validate(tailor).IsValid);
        Assert.True(new RegisterCustomerValidator().Validate(customer).IsValid);
        string oversized = "+234" + new string('0', 60);
        Assert.Contains(new RegisterTailorValidator().Validate(tailor with { Phone = oversized }).Errors, e => e.PropertyName == "Phone");
        Assert.Contains(new RegisterCustomerValidator().Validate(customer with { Phone = oversized }).Errors, e => e.PropertyName == "Phone");
        Assert.False(new RegisterTailorValidator().Validate(tailor with { Phone = "" }).IsValid);
    }
}

using TailorSpadi.Domain.Tenancy;

namespace TailorSpadi.UnitTests;

public sealed class SubdomainRulesTests
{
    [Theory]
    [InlineData("John & Sons Fashion", "john-and-sons-fashion")]
    [InlineData("  Ade's  Tailoring!!  ", "ade-s-tailoring")]
    [InlineData("Café Élégance", "cafe-elegance")]
    [InlineData("Ọlá Fáshion", "ola-fashion")]
    [InlineData("---Mary---Wear---", "mary-wear")]
    [InlineData("TAILOR 247", "tailor-247")]
    public void BaseIsLowercaseAsciiWithSingleHyphens(string input, string expected)
    {
        Assert.Equal(expected, SubdomainRules.CreateBase(input));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("!!!")]
    [InlineData("裁縫")]
    [InlineData("ab")]
    public void NamesWithoutEnoughLatinLettersAreNotMeaningful(string input)
    {
        Assert.False(SubdomainRules.IsMeaningful(SubdomainRules.CreateBase(input)));
    }

    [Fact]
    public void LongNamesStayWithinTheDnsLimit()
    {
        string slugBase = SubdomainRules.CreateBase(new string('a', 200));
        string label = slugBase;

        Assert.True(slugBase.Length <= SubdomainRules.MaxBaseLength);
        Assert.True(label.Length <= SubdomainRules.MaxLabelLength);
        Assert.True(SubdomainRules.IsValidLabel(label));
    }

    [Fact]
    public void TruncationNeverLeavesATrailingHyphen()
    {
        string input = new string('a', SubdomainRules.MaxBaseLength - 1) + " b";
        string slugBase = SubdomainRules.CreateBase(input);

        Assert.False(slugBase.EndsWith('-'));
    }

    [Theory]
    [InlineData("admin")]
    [InlineData("www")]
    [InlineData("api")]
    [InlineData("tailorspadi")]
    [InlineData("login")]
    public void ReservedNamesAreRejected(string name)
    {
        Assert.True(ReservedSubdomains.IsReserved(name));
    }

    [Fact]
    public void OrdinaryNamesAreNotReserved()
    {
        Assert.False(ReservedSubdomains.IsReserved("john-fashion"));
    }

    [Theory]
    [InlineData("john-fashion-001", true)]
    [InlineData("a", true)]
    [InlineData("-bad", false)]
    [InlineData("bad-", false)]
    [InlineData("UPPER", false)]
    [InlineData("has space", false)]
    [InlineData("under_score", false)]
    [InlineData("a.b", false)]
    [InlineData("xn--bcher-kva", false)]
    [InlineData("double--hyphen", false)]
    [InlineData("", false)]
    public void LabelValidationIsStrict(string label, bool expected)
    {
        Assert.Equal(expected, SubdomainRules.IsValidLabel(label));
    }

    [Fact]
    public void LabelOver63CharactersIsInvalid()
    {
        Assert.False(SubdomainRules.IsValidLabel(new string('a', 64)));
        Assert.True(SubdomainRules.IsValidLabel(new string('a', 63)));
    }
}

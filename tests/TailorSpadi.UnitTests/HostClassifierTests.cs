using TailorSpadi.Application.Options;
using TailorSpadi.Application.Tenancy;

namespace TailorSpadi.UnitTests;

public sealed class HostClassifierTests
{
    private static readonly TenancyOptions Local = new()
    {
        BaseDomain = "localhost",
        PlatformHosts = ["localhost", "127.0.0.1", "[::1]"]
    };

    private static readonly TenancyOptions Production = new()
    {
        BaseDomain = "tailorspadi.com",
        PlatformHosts = ["tailorspadi.com", "www.tailorspadi.com"]
    };

    [Theory]
    [InlineData("localhost")]
    [InlineData("localhost:8080")]
    [InlineData("LOCALHOST:5174")]
    [InlineData("127.0.0.1:5080")]
    [InlineData("[::1]:5080")]
    [InlineData("localhost.")]
    public void PlatformHostsHaveNoTenant(string host)
    {
        Assert.Equal(HostKind.Platform, HostClassifier.Classify(host, Local).Kind);
    }

    [Theory]
    [InlineData("johnfashion-001.localhost", "johnfashion-001")]
    [InlineData("JohnFashion-001.LOCALHOST:8080", "johnfashion-001")]
    [InlineData("a.localhost.", "a")]
    public void SingleLabelBeneathTheBaseDomainIsATenantCandidate(string host, string label)
    {
        HostClassification result = HostClassifier.Classify(host, Local);

        Assert.Equal(HostKind.TenantCandidate, result.Kind);
        Assert.Equal(label, result.Label);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("evil.com")]
    [InlineData("localhost.evil.com")]
    [InlineData("notlocalhost")]
    [InlineData("a.b.localhost")]
    [InlineData("-a.localhost")]
    [InlineData("a_b.localhost")]
    [InlineData(".localhost")]
    [InlineData("xn--bcher-kva.localhost")]
    [InlineData("a:b:c")]
    [InlineData("[::1")]
    public void EverythingElseIsInvalid(string? host)
    {
        Assert.Equal(HostKind.Invalid, HostClassifier.Classify(host, Local).Kind);
    }

    [Fact]
    public void ProductionDomainsFollowTheSameRules()
    {
        Assert.Equal(HostKind.Platform, HostClassifier.Classify("tailorspadi.com", Production).Kind);
        Assert.Equal(HostKind.Platform, HostClassifier.Classify("www.tailorspadi.com", Production).Kind);

        HostClassification tenant = HostClassifier.Classify("john-fashion-001.tailorspadi.com", Production);
        Assert.Equal(HostKind.TenantCandidate, tenant.Kind);
        Assert.Equal("john-fashion-001", tenant.Label);

        Assert.Equal(HostKind.Invalid, HostClassifier.Classify("eviltailorspadi.com", Production).Kind);
        Assert.Equal(HostKind.Invalid, HostClassifier.Classify("a.b.tailorspadi.com", Production).Kind);
    }
}

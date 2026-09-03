using PortManager.Services;
using Xunit;

namespace PortManager.Tests;

public class FirewallServiceTests
{
    [Fact]
    public void CreateRuleModel_MapsNativeFirewallValues()
    {
        var rule = FirewallService.CreateRuleModel("HTTPS", 1, 6, "443", "*", int.MaxValue, true);

        Assert.Equal("HTTPS", rule.Name);
        Assert.Equal("Inbound", rule.Direction);
        Assert.Equal("TCP", rule.Protocol);
        Assert.Equal("443", rule.PortDisplay);
        Assert.Equal("Any", rule.RemotePort);
        Assert.Equal("Any", rule.Profile);
    }

    [Theory]
    [InlineData(null, "Any")]
    [InlineData("", "Any")]
    [InlineData("*", "Any")]
    [InlineData("443", "443")]
    public void NormalizePorts_HandlesFirewallWildcards(string? value, string expected)
    {
        Assert.Equal(expected, FirewallService.NormalizePorts(value));
    }

    [Theory]
    [InlineData("Any", false)]
    [InlineData("*", false)]
    [InlineData("80", true)]
    [InlineData("8000-8100", true)]
    public void HasSpecificPort_FiltersRulesWithoutPortConditions(string value, bool expected)
    {
        Assert.Equal(expected, FirewallService.HasSpecificPort(value));
    }
}

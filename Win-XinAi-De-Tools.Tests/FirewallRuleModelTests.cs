using PortManager.Models;
using PortManager.Services;
using Xunit;

namespace PortManager.Tests;

public class FirewallRuleModelTests
{
    [Fact]
    public void DirectionAndProtocol_AreDisplayedForChineseUi()
    {
        LanguageState.Current = AppLanguage.Chinese;
        var rule = new FirewallRule { Direction = "Inbound", Protocol = "6" };
        Assert.Equal("入站", rule.DirectionDisplay);
        Assert.Equal("TCP", rule.ProtocolDisplay);
    }

    [Fact]
    public void Direction_IsDisplayedForEnglishUi()
    {
        LanguageState.Current = AppLanguage.English;
        var rule = new FirewallRule { Direction = "Outbound", Protocol = "17" };
        Assert.Equal("Outbound", rule.DirectionDisplay);
        Assert.Equal("UDP", rule.ProtocolDisplay);
    }

    [Theory]
    [InlineData("Inbound", "443", "Any", "443")]
    [InlineData("Outbound", "Any", "443", "443")]
    [InlineData("Outbound", "8080", "Any", "8080")]
    public void PortDisplay_UsesTheRelevantEndpoint(
        string direction, string localPort, string remotePort, string expected)
    {
        var rule = new FirewallRule
        {
            Direction = direction,
            LocalPort = localPort,
            RemotePort = remotePort
        };

        Assert.Equal(expected, rule.PortDisplay);
    }

    [Fact]
    public void UnknownValues_ArePreserved()
    {
        var rule = new FirewallRule { Direction = "Custom", Protocol = "SCTP" };
        Assert.Equal("Custom", rule.DirectionDisplay);
        Assert.Equal("SCTP", rule.ProtocolDisplay);
    }
}

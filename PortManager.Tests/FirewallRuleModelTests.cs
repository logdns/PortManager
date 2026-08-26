using PortManager.Models;
using Xunit;

namespace PortManager.Tests;

public class FirewallRuleModelTests
{
    [Fact]
    public void DirectionAndProtocol_AreDisplayedForChineseUi()
    {
        var rule = new FirewallRule { Direction = "Inbound", Protocol = "Any" };
        Assert.Equal("入站", rule.DirectionDisplay);
        Assert.Equal("ANY", rule.ProtocolDisplay);
    }

    [Fact]
    public void UnknownValues_ArePreserved()
    {
        var rule = new FirewallRule { Direction = "Custom", Protocol = "SCTP" };
        Assert.Equal("Custom", rule.DirectionDisplay);
        Assert.Equal("SCTP", rule.ProtocolDisplay);
    }
}

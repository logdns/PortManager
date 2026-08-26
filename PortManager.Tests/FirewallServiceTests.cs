using PortManager.Services;
using Xunit;

namespace PortManager.Tests;

public class FirewallServiceTests
{
    [Fact]
    public void ParseRulesJson_AcceptsSinglePowerShellObject()
    {
        const string json = """
            {"Name":"HTTPS","Dir":"Inbound","Proto":"TCP","LocalPort":"443","RemotePort":"Any","Profile":"Any","Enabled":"True"}
            """;

        var rules = FirewallService.ParseRulesJson(json);

        var rule = Assert.Single(rules);
        Assert.Equal("HTTPS", rule.Name);
        Assert.Equal("443", rule.PortDisplay);
    }

    [Fact]
    public void ParseRulesJson_AcceptsObjectArray()
    {
        const string json = """
            [{"Name":"DNS in","Dir":"Inbound","Proto":"UDP","LocalPort":"53","RemotePort":"Any","Profile":"Any","Enabled":"True"},{"Name":"DNS out","Dir":"Outbound","Proto":"UDP","LocalPort":"Any","RemotePort":"53","Profile":"Any","Enabled":"True"}]
            """;

        var rules = FirewallService.ParseRulesJson(json);

        Assert.Equal(2, rules.Count);
        Assert.All(rules, rule => Assert.Equal("53", rule.PortDisplay));
    }

    [Fact]
    public void ParseRulesJson_RejectsMalformedOutput()
    {
        Assert.Throws<FirewallOperationException>(() => FirewallService.ParseRulesJson("not-json"));
    }

    [Theory]
    [InlineData("in", 443, "localport=443")]
    [InlineData("out", 443, "remoteport=443")]
    public void GetPortArgument_UsesTheCorrectEndpoint(string direction, int port, string expected)
    {
        Assert.Equal(expected, FirewallService.GetPortArgument(direction, port));
    }
}

using PortManager.Models;
using PortManager.Services;
using Xunit;

namespace PortManager.Tests;

public class RuleTransferServiceTests
{
    [Fact]
    public void SerializeAndParse_PreservesRuleFields()
    {
        var source = new FirewallRule { Name = "Web", Direction = "Inbound", Protocol = "TCP", LocalPort = "443", RemotePort = "Any", Profile = "Any", Enabled = "True" };
        var document = RuleTransferService.Parse(RuleTransferService.Serialize(new[] { source }));
        Assert.Equal(1, document.FormatVersion);
        Assert.Single(document.Rules);
        Assert.Equal("Web", document.Rules[0].Name);
        Assert.Equal("443", document.Rules[0].LocalPort);
        Assert.Equal("TCP", document.Rules[0].Protocol);
    }

    [Fact]
    public void Parse_RejectsInvalidJson() => Assert.Throws<RuleTransferException>(() => RuleTransferService.Parse("not-json"));

    [Fact]
    public void Parse_RejectsUnsupportedVersion()
    {
        var exception = Assert.Throws<RuleTransferException>(() => RuleTransferService.Parse("{\"FormatVersion\":2,\"Rules\":[]}"));
        Assert.Contains("Unsupported", exception.Message);
    }

    [Fact]
    public void Parse_RejectsIncompleteRule() => Assert.Throws<RuleTransferException>(() => RuleTransferService.Parse("{\"FormatVersion\":1,\"Rules\":[{\"Name\":\"x\",\"Dir\":\"Inbound\"}]}"));
}

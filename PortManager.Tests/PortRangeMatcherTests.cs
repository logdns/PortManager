using PortManager.Services;
using Xunit;

namespace PortManager.Tests;

public class PortRangeMatcherTests
{
    [Theory]
    [InlineData("80", 80, true)]
    [InlineData("8080", 80, false)]
    [InlineData("8000-8100", 8080, true)]
    [InlineData("53, 80, 443", 80, true)]
    [InlineData("RPC", 135, false)]
    public void Matches_HandlesWindowsFirewallPortFormats(string value, int port, bool expected)
    {
        Assert.Equal(expected, PortRangeMatcher.Matches(value, port));
    }
}

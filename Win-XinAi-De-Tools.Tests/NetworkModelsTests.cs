using PortManager.Models;
using Xunit;

namespace PortManager.Tests;

public sealed class NetworkModelsTests
{
    [Theory]
    [InlineData("192.168.1.10", true)]
    [InlineData("10.0.0.1", true)]
    [InlineData("256.1.1.1", false)]
    [InlineData("fe80::1", false)]
    [InlineData("", false)]
    public void IsValidIpv4_ValidatesIpv4Only(string value, bool expected)
    {
        Assert.Equal(expected, NetworkConfigurationModel.IsValidIpv4(value));
    }
}

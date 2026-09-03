using System.Linq;
using PortManager.Services;
using Xunit;

namespace PortManager.Tests;

public sealed class WslServiceTests
{
    [Fact]
    public void ParseDistributions_HandlesDefaultAndStates()
    {
        var rows = WslService.ParseDistributions("  NAME                   STATE           VERSION\n* Ubuntu                 Running         2\n  Debian                 Stopped         2\n");

        Assert.Equal(2, rows.Count);
        var ubuntu = Assert.Single(rows, row => row.Name == "Ubuntu");
        Assert.True(ubuntu.IsDefault);
        Assert.Equal("Running", ubuntu.State);
        Assert.Equal(2, ubuntu.Version);
        var debian = Assert.Single(rows, row => row.Name == "Debian");
        Assert.False(debian.IsDefault);
    }

    [Theory]
    [InlineData("WslRegisterDistribution failed because WSL is not installed.")]
    [InlineData("未安装适用于 Linux 的 Windows 子系统。")]
    [InlineData("Please enable the Virtual Machine Platform Windows feature.")]
    public void IsNotInstalledMessage_RecognizesSetupErrors(string message)
    {
        Assert.True(WslService.IsNotInstalledMessage(message));
    }

    [Theory]
    [InlineData("There are no installed distributions.")]
    [InlineData("没有安装发行版。")]
    public void IsNoDistributionMessage_RecognizesEmptyInstall(string message)
    {
        Assert.True(WslService.IsNoDistributionMessage(message));
    }

    [Fact]
    public void ParseDistributions_SkipsLocalizedHeader()
    {
        var rows = WslService.ParseDistributions("名称 状态 版本\n* Ubuntu Running 2\n");

        var distro = Assert.Single(rows);
        Assert.Equal("Ubuntu", distro.Name);
        Assert.True(distro.IsDefault);
    }
}

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
        var ubuntu = Assert.Single(rows.Where(row => row.Name == "Ubuntu"));
        Assert.True(ubuntu.IsDefault);
        Assert.Equal("Running", ubuntu.State);
        Assert.Equal(2, ubuntu.Version);
        var debian = Assert.Single(rows.Where(row => row.Name == "Debian"));
        Assert.False(debian.IsDefault);
    }
}

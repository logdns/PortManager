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
        Assert.Equal("Ubuntu", rows[0].Name);
        Assert.True(rows[0].IsDefault);
        Assert.Equal("Running", rows[0].State);
        Assert.Equal(2, rows[0].Version);
        Assert.False(rows[1].IsDefault);
    }
}

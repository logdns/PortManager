using System;
using System.Threading.Tasks;
using PortManager.Services;
using Xunit;

namespace PortManager.Tests;

public class FirewallIntegrationTests
{
    [Fact]
    [Trait("Category", "WindowsIntegration")]
    public async Task AddQueryDelete_CompletesFirewallRuleRoundTrip()
    {
        if (!OperatingSystem.IsWindows() ||
            Environment.GetEnvironmentVariable("PORTMANAGER_RUN_INTEGRATION") != "1")
        {
            return;
        }

        var ruleName = $"PortManager_CI_{Guid.NewGuid():N}";
        const int port = 54321;

        try
        {
            var addResult = await FirewallService.AddRuleAsync(port, "TCP", "in", ruleName);
            Assert.True(addResult.Success, addResult.ErrorMessage);

            var listedRules = await FirewallService.ListRulesAsync();
            Assert.Contains(listedRules, rule => rule.Name == ruleName && rule.PortDisplay == port.ToString());

            var queriedRules = await FirewallService.QueryPortAsync(port);
            Assert.Contains(queriedRules, rule => rule.Name == ruleName);

            Assert.True(await FirewallService.DeleteRuleAsync(ruleName));
            listedRules = await FirewallService.ListRulesAsync();
            Assert.DoesNotContain(listedRules, rule => rule.Name == ruleName);
        }
        finally
        {
            await FirewallService.DeleteRuleAsync(ruleName);
        }
    }
}

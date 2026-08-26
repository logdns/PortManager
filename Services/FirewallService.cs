using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using PortManager.Models;

namespace PortManager.Services;

public static class FirewallService
{
    private const string ListRulesScript = """
        $ErrorActionPreference = 'Stop'
        $ProgressPreference = 'SilentlyContinue'
        [Console]::OutputEncoding = [System.Text.Encoding]::UTF8

        Get-NetFirewallRule -Action Allow -Enabled True -ErrorAction Stop |
          ForEach-Object {
            $rule = $_
            $rule | Get-NetFirewallPortFilter -ErrorAction Stop |
              ForEach-Object {
                $filter = $_
                $localPort = [string]$filter.LocalPort
                $remotePort = [string]$filter.RemotePort
                if (($localPort -and $localPort -ne 'Any') -or
                    ($remotePort -and $remotePort -ne 'Any')) {
                  $protocol = switch ([string]$filter.Protocol) {
                    '6' { 'TCP' }
                    '17' { 'UDP' }
                    '256' { 'Any' }
                    default { [string]$filter.Protocol }
                  }

                  [PSCustomObject]@{
                    Name = [string]$rule.DisplayName
                    Dir = [string]$rule.Direction
                    Proto = $protocol
                    LocalPort = $localPort
                    RemotePort = $remotePort
                    Profile = [string]$rule.Profile
                    Enabled = [string]$rule.Enabled
                  }
                }
              }
          } | ConvertTo-Json -Depth 3 -Compress
        """;

    public static async Task<List<FirewallRule>> ListRulesAsync()
    {
        var output = await RunPowerShellAsync(ListRulesScript);
        return ParseRulesJson(output);
    }

    public static async Task<List<FirewallRule>> QueryPortAsync(int port)
    {
        var all = await ListRulesAsync();
        return all.Where(rule =>
                PortRangeMatcher.Matches(rule.LocalPort, port) ||
                PortRangeMatcher.Matches(rule.RemotePort, port))
            .ToList();
    }

    public static async Task<OperationResult> AddRuleAsync(
        int port, string protocol, string direction, string ruleName)
    {
        var result = new OperationResult();
        var protocols = protocol == "ANY" ? new[] { "TCP", "UDP" } : new[] { protocol };
        var directions = direction switch
        {
            "Both" => new[] { "in", "out" },
            "out" => new[] { "out" },
            _ => new[] { "in" }
        };

        var errors = new List<string>();
        foreach (var dir in directions)
        {
            foreach (var proto in protocols)
            {
                var name = ruleName;
                if (protocols.Length > 1)
                    name += $"_{proto}";
                if (directions.Length > 1)
                    name += dir == "in" ? "_Inbound" : "_Outbound";
                var (success, error) = await AddSingleRuleAsync(name, port, proto, dir);
                if (success)
                    result.SuccessCount++;
                else
                {
                    result.FailedCount++;
                    if (!string.IsNullOrWhiteSpace(error))
                        errors.Add(error.Trim());
                }
            }
        }

        result.Success = result.FailedCount == 0;
        result.Message = result.Success
            ? $"Added {result.SuccessCount} firewall rule(s)."
            : $"Added {result.SuccessCount}; failed {result.FailedCount}.";
        result.ErrorMessage = string.Join(Environment.NewLine, errors.Distinct());
        return result;
    }

    public static async Task<bool> DeleteRuleAsync(string ruleName)
    {
        var (exitCode, _) = await RunNetshAsync(
            "advfirewall", "firewall", "delete", "rule", $"name={ruleName}");
        return exitCode == 0;
    }

    public static async Task<OperationResult> ModifyRuleAsync(
        string oldName, int port, string protocol, string direction, string newName)
    {
        if (!await DeleteRuleAsync(oldName))
        {
            return new OperationResult
            {
                Success = false,
                FailedCount = 1,
                Message = "The existing rule could not be removed."
            };
        }

        return await AddRuleAsync(port, protocol, direction, newName);
    }

    internal static List<FirewallRule> ParseRulesJson(string output)
    {
        if (string.IsNullOrWhiteSpace(output))
            return new List<FirewallRule>();

        var json = output.Trim();
        if (!json.StartsWith('['))
            json = $"[{json}]";

        try
        {
            return JsonSerializer.Deserialize<List<FirewallRule>>(json) ?? new List<FirewallRule>();
        }
        catch (JsonException ex)
        {
            throw new FirewallOperationException("Windows Firewall returned an unreadable response.", ex);
        }
    }

    private static async Task<(bool success, string error)> AddSingleRuleAsync(
        string name, int port, string protocol, string direction)
    {
        var portArgument = GetPortArgument(direction, port);
        var (exitCode, output) = await RunNetshAsync(
            "advfirewall", "firewall", "add", "rule",
            $"name={name}", $"dir={direction}", "action=allow",
            $"protocol={protocol}", portArgument, "profile=any", "enable=yes");
        return (exitCode == 0, output);
    }

    internal static string GetPortArgument(string direction, int port) =>
        direction == "out" ? $"remoteport={port}" : $"localport={port}";

    private static async Task<string> RunPowerShellAsync(string script)
    {
        var psi = CreateProcessStartInfo("powershell.exe");
        psi.ArgumentList.Add("-NoLogo");
        psi.ArgumentList.Add("-NoProfile");
        psi.ArgumentList.Add("-NonInteractive");
        psi.ArgumentList.Add("-EncodedCommand");
        psi.ArgumentList.Add(Convert.ToBase64String(Encoding.Unicode.GetBytes(script)));

        var (exitCode, output, error) = await RunProcessAsync(psi);
        if (exitCode != 0)
            throw new FirewallOperationException(BuildProcessError("PowerShell firewall query failed", error, output));

        return output;
    }

    private static async Task<(int exitCode, string output)> RunNetshAsync(params string[] arguments)
    {
        var psi = CreateProcessStartInfo("netsh.exe");
        foreach (var argument in arguments)
            psi.ArgumentList.Add(argument);

        var (exitCode, output, error) = await RunProcessAsync(psi);
        return (exitCode, string.IsNullOrWhiteSpace(error) ? output : error);
    }

    private static ProcessStartInfo CreateProcessStartInfo(string fileName) => new()
    {
        FileName = fileName,
        UseShellExecute = false,
        RedirectStandardOutput = true,
        RedirectStandardError = true,
        StandardOutputEncoding = Encoding.UTF8,
        StandardErrorEncoding = Encoding.UTF8,
        CreateNoWindow = true
    };

    private static async Task<(int exitCode, string output, string error)> RunProcessAsync(ProcessStartInfo psi)
    {
        try
        {
            using var process = Process.Start(psi);
            if (process is null)
                throw new FirewallOperationException($"Could not start {psi.FileName}.");

            var outputTask = process.StandardOutput.ReadToEndAsync();
            var errorTask = process.StandardError.ReadToEndAsync();
            await process.WaitForExitAsync();
            return (process.ExitCode, await outputTask, await errorTask);
        }
        catch (FirewallOperationException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new FirewallOperationException($"Could not run {psi.FileName}.", ex);
        }
    }

    private static string BuildProcessError(string prefix, string error, string output)
    {
        var detail = string.IsNullOrWhiteSpace(error) ? output : error;
        return string.IsNullOrWhiteSpace(detail) ? prefix : $"{prefix}: {detail.Trim()}";
    }
}

public sealed class FirewallOperationException : Exception
{
    public FirewallOperationException(string message) : base(message)
    {
    }

    public FirewallOperationException(string message, Exception innerException) : base(message, innerException)
    {
    }
}

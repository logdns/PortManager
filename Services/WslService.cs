using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PortManager.Models;

namespace PortManager.Services;

public static class WslService
{
    public static async Task<List<WslDistributionModel>> ListDistributionsAsync()
    {
        EnsureWindows();
        var result = await RunAsync("--list", "--verbose");
        if (result.ExitCode != 0)
            throw new WslOperationException(string.IsNullOrWhiteSpace(result.Error) ? "WSL is not available." : result.Error.Trim());
        return ParseDistributions(result.Output);
    }

    public static Task StartAsync(string name) => RunCheckedAsync("--distribution", name, "--exec", "true");
    public static Task StopAsync(string name) => RunCheckedAsync("--terminate", name);
    public static Task SetDefaultAsync(string name) => RunCheckedAsync("--set-default", name);

    public static void OpenTerminal(string name)
    {
        EnsureWindows();
        var info = new ProcessStartInfo { FileName = "wsl.exe", UseShellExecute = true };
        info.ArgumentList.Add("--distribution");
        info.ArgumentList.Add(name);
        Process.Start(info);
    }

    internal static List<WslDistributionModel> ParseDistributions(string output)
    {
        var rows = new List<WslDistributionModel>();
        foreach (var rawLine in output.Replace("\0", string.Empty, StringComparison.Ordinal).Split('\n'))
        {
            var line = rawLine.Trim();
            if (line.Length == 0 || line.StartsWith("NAME", StringComparison.OrdinalIgnoreCase) || line.StartsWith("Windows Subsystem", StringComparison.OrdinalIgnoreCase)) continue;
            var isDefault = line[0] == '*';
            if (isDefault) line = line[1..].TrimStart();
            var parts = line.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 2) continue;
            var version = 0;
            _ = int.TryParse(parts[^1], out version);
            var state = parts.Length >= 3 ? parts[^2] : string.Empty;
            var name = string.Join(' ', parts[..Math.Max(1, parts.Length - 2)]);
            rows.Add(new WslDistributionModel { Name = name, State = state, Version = version, IsDefault = isDefault });
        }
        return rows.OrderBy(item => item.Name, StringComparer.OrdinalIgnoreCase).ToList();
    }

    private static async Task RunCheckedAsync(params string[] arguments)
    {
        EnsureWindows();
        var result = await RunAsync(arguments);
        if (result.ExitCode != 0) throw new WslOperationException(string.IsNullOrWhiteSpace(result.Error) ? "WSL command failed." : result.Error.Trim());
    }

    private static async Task<(int ExitCode, string Output, string Error)> RunAsync(params string[] arguments)
    {
        var info = new ProcessStartInfo { FileName = "wsl.exe", UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true, RedirectStandardError = true, StandardOutputEncoding = Encoding.Unicode, StandardErrorEncoding = Encoding.Unicode };
        foreach (var argument in arguments) info.ArgumentList.Add(argument);
        using var process = Process.Start(info) ?? throw new WslOperationException("Could not start wsl.exe.");
        var outputTask = process.StandardOutput.ReadToEndAsync();
        var errorTask = process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();
        return (process.ExitCode, await outputTask, await errorTask);
    }

    private static void EnsureWindows() { if (!OperatingSystem.IsWindows()) throw new PlatformNotSupportedException("WSL management is available only on Windows."); }
}

using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PortManager.Models;

namespace PortManager.Services;

public static class WslService
{
    public static async Task<WslStatusModel> GetStatusAsync()
    {
        EnsureWindows();
        try
        {
            var distributions = await ListDistributionsAsync();
            return new WslStatusModel { IsInstalled = true, Distributions = distributions };
        }
        catch (WslNotInstalledException)
        {
            return new WslStatusModel { IsInstalled = false };
        }
    }

    public static async Task<List<WslDistributionModel>> ListDistributionsAsync()
    {
        EnsureWindows();
        (int ExitCode, string Output, string Error) result;
        try
        {
            result = await RunAsync("--list", "--verbose");
        }
        catch (WslNotInstalledException)
        {
            throw;
        }

        if (result.ExitCode != 0)
        {
            var details = string.IsNullOrWhiteSpace(result.Error) ? result.Output : result.Error;
            if (IsNotInstalledMessage(details))
                throw new WslNotInstalledException(details.Trim());
            if (IsNoDistributionMessage(details))
                return new List<WslDistributionModel>();
            throw new WslOperationException(string.IsNullOrWhiteSpace(details) ? "WSL is not available." : details.Trim());
        }
        return ParseDistributions(result.Output);
    }

    public static Task InstallAsync()
    {
        EnsureWindows();
        StartElevated("--install");
        return Task.CompletedTask;
    }

    public static Task InstallDistributionAsync(string distribution = "Ubuntu")
    {
        EnsureWindows();
        StartElevated("--install", "--distribution", distribution);
        return Task.CompletedTask;
    }

    public static void OpenInstallHelp()
    {
        EnsureWindows();
        Process.Start(new ProcessStartInfo
        {
            FileName = "https://aka.ms/wslinstall",
            UseShellExecute = true
        });
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
            if (line.Length == 0 || line.StartsWith("NAME", StringComparison.OrdinalIgnoreCase) ||
                line.StartsWith("名称", StringComparison.OrdinalIgnoreCase) ||
                line.StartsWith("Windows Subsystem", StringComparison.OrdinalIgnoreCase) ||
                line.StartsWith("适用于 Linux 的 Windows 子系统", StringComparison.OrdinalIgnoreCase)) continue;
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
        Process process;
        try
        {
            process = Process.Start(info) ?? throw new WslOperationException("Could not start wsl.exe.");
        }
        catch (Win32Exception exception) when (exception.NativeErrorCode is 2 or 3)
        {
            throw new WslNotInstalledException("wsl.exe was not found. Install Windows Subsystem for Linux first.");
        }

        using (process)
        {
            var outputTask = process.StandardOutput.ReadToEndAsync();
            var errorTask = process.StandardError.ReadToEndAsync();
            await process.WaitForExitAsync();
            return (process.ExitCode, await outputTask, await errorTask);
        }
    }

    private static void StartElevated(params string[] arguments)
    {
        var info = new ProcessStartInfo
        {
            FileName = "wsl.exe",
            UseShellExecute = true,
            Verb = "runas",
            WindowStyle = ProcessWindowStyle.Normal
        };
        foreach (var argument in arguments)
            info.ArgumentList.Add(argument);
        try
        {
            Process.Start(info);
        }
        catch (Win32Exception exception) when (exception.NativeErrorCode == 1223)
        {
            throw new WslOperationException("The administrator approval was cancelled.");
        }
    }

    internal static bool IsNotInstalledMessage(string? message)
    {
        if (string.IsNullOrWhiteSpace(message)) return false;
        var normalized = message.ToLowerInvariant();
        return normalized.Contains("not installed", StringComparison.Ordinal) ||
               normalized.Contains("not enabled", StringComparison.Ordinal) ||
               normalized.Contains("未安装", StringComparison.Ordinal) ||
               normalized.Contains("未启用", StringComparison.Ordinal) ||
               normalized.Contains("enable the virtual machine platform", StringComparison.Ordinal);
    }

    internal static bool IsNoDistributionMessage(string? message)
    {
        if (string.IsNullOrWhiteSpace(message)) return false;
        var normalized = message.ToLowerInvariant();
        return normalized.Contains("no distributions", StringComparison.Ordinal) ||
               normalized.Contains("no installed distributions", StringComparison.Ordinal) ||
               normalized.Contains("没有安装分发版", StringComparison.Ordinal) ||
               normalized.Contains("没有安装发行版", StringComparison.Ordinal);
    }

    private static void EnsureWindows() { if (!OperatingSystem.IsWindows()) throw new PlatformNotSupportedException("WSL management is available only on Windows."); }
}

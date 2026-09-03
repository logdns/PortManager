using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading.Tasks;
using PortManager.Models;

namespace PortManager.Services;

public static class WslService
{
    public static bool ShutdownOnExit { get; set; }
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
        return RunElevatedCheckedAsync("--install");
    }

    public static Task InstallDistributionAsync(string distribution = "Ubuntu")
    {
        EnsureWindows();
        return RunElevatedCheckedAsync("--install", "--distribution", distribution, "--no-launch");
    }

    public static async Task<string> GetVersionInfoAsync()
    {
        EnsureWindows();
        var result = await RunAsync("--version");
        if (result.ExitCode != 0)
            throw new WslOperationException(string.IsNullOrWhiteSpace(result.Error) ? "Could not read the WSL version." : result.Error.Trim());
        return result.Output.Replace("\0", string.Empty, StringComparison.Ordinal).Trim();
    }

    public static Task UpdateAsync() => RunCheckedAsync("--update");
    public static Task ShutdownAllAsync() => RunCheckedAsync("--shutdown");

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
    public static Task TerminateAsync(string name) => RunCheckedAsync("--terminate", name);
    public static void ShutdownAll()
    {
        EnsureWindows();
        using var process = Process.Start(new ProcessStartInfo { FileName = ResolveWslExecutable(), UseShellExecute = false, CreateNoWindow = true, ArgumentList = { "--shutdown" } });
        process?.WaitForExit(10_000);
    }
    public static Task SetDefaultAsync(string name) => RunCheckedAsync("--set-default", name);
    public static Task UnregisterAsync(string name) => RunCheckedAsync("--unregister", name);

    public static Task ExportAsync(string name, string archivePath) => RunCheckedAsync("--export", name, archivePath);

    public static Task ImportAsync(string name, string installPath, string archivePath, int version = 2)
        => RunCheckedAsync("--import", name, installPath, archivePath, "--version", version.ToString());

    public static Task MoveAsync(string name, string targetPath) => RunCheckedAsync("--manage", name, "--move", targetPath);

    public static async Task<string> GetDiskUsageAsync(string name)
    {
        EnsureWindows();
        var result = await RunAsync("--distribution", name, "--exec", "df", "-h", "/");
        if (result.ExitCode != 0)
            throw new WslOperationException(string.IsNullOrWhiteSpace(result.Error) ? "Could not read disk usage." : result.Error.Trim());
        return result.Output.Trim();
    }

    public static void OpenExplorer(string name)
    {
        EnsureWindows();
        var info = new ProcessStartInfo { FileName = "explorer.exe", UseShellExecute = true };
        info.ArgumentList.Add($"\\\\wsl$\\{name}");
        Process.Start(info);
    }

    public static void OpenVsCode(string name)
    {
        EnsureWindows();
        var info = new ProcessStartInfo { FileName = "code", UseShellExecute = true };
        info.ArgumentList.Add("--remote");
        info.ArgumentList.Add($"wsl+{name}");
        Process.Start(info);
    }

    public static Task SetAutostartAsync(string name, bool enabled)
    {
        EnsureWindows();
        var taskName = $"Win-XinAi-De-Tools\\WSL\\{name}";
        return enabled
            ? RunWindowsCommandAsync("schtasks.exe", "/Create", "/TN", taskName, "/SC", "ONLOGON", "/TR", BuildScheduledWslCommand(name, "true"), "/F")
            : RunWindowsCommandAsync("schtasks.exe", "/Delete", "/TN", taskName, "/F");
    }

    public static Task ScheduleCommandAsync(string taskName, string name, string command, string schedule, string startTime)
    {
        EnsureWindows();
        var fullName = $"Win-XinAi-De-Tools\\WSL\\{taskName}";
        var normalizedSchedule = schedule.Equals("once", StringComparison.OrdinalIgnoreCase) ? "ONCE" : schedule.ToUpperInvariant();
        return RunWindowsCommandAsync("schtasks.exe", "/Create", "/TN", fullName, "/SC", normalizedSchedule, "/ST", startTime, "/TR", BuildScheduledWslCommand(name, command), "/F");
    }

    public static Task RemoveScheduledCommandAsync(string taskName)
    {
        EnsureWindows();
        return RunWindowsCommandAsync("schtasks.exe", "/Delete", "/TN", $"Win-XinAi-De-Tools\\WSL\\{taskName}", "/F");
    }

    public static Task AddPortProxyAsync(int listenPort, string address, int port)
    {
        EnsureWindows();
        return RunWindowsCommandAsync("netsh.exe", "interface", "portproxy", "add", "v4tov4", $"listenport={listenPort}", "listenaddress=0.0.0.0", $"connectport={port}", $"connectaddress={address}");
    }

    public static Task RemovePortProxyAsync(int listenPort)
    {
        EnsureWindows();
        return RunWindowsCommandAsync("netsh.exe", "interface", "portproxy", "delete", "v4tov4", $"listenport={listenPort}", "listenaddress=0.0.0.0");
    }

    public static Task SetHttpProxyAsync(string? proxy)
    {
        EnsureWindows();
        return string.IsNullOrWhiteSpace(proxy)
            ? RunWindowsCommandAsync("netsh.exe", "winhttp", "reset", "proxy")
            : RunWindowsCommandAsync("netsh.exe", "winhttp", "set", "proxy", proxy);
    }

    public static Task MountVhdAsync(string diskPath, int? partition = null)
    {
        EnsureWindows();
        return partition.HasValue
            ? RunCheckedAsync("--mount", diskPath, "--partition", partition.Value.ToString())
            : RunCheckedAsync("--mount", diskPath);
    }

    public static Task UnmountVhdAsync(string diskPath)
    {
        EnsureWindows();
        return RunCheckedAsync("--unmount", diskPath);
    }

    public static async Task<IReadOnlyList<string>> ListUsbDevicesAsync()
    {
        EnsureWindows();
        var result = await RunWindowsCommandCaptureAsync("usbipd.exe", "list");
        if (result.ExitCode != 0) throw new WslOperationException(string.IsNullOrWhiteSpace(result.Error) ? "usbipd-win is not installed." : result.Error.Trim());
        return result.Output.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    }

    public static Task BindUsbDeviceAsync(string busId) => RunWindowsCommandAsync("usbipd.exe", "bind", "--busid", busId);
    public static Task AttachUsbDeviceAsync(string busId, string distribution) => RunWindowsCommandAsync("usbipd.exe", "attach", "--wsl", "--busid", busId);
    public static Task DetachUsbDeviceAsync(string busId) => RunWindowsCommandAsync("usbipd.exe", "detach", "--busid", busId);

    public static void OpenTerminal(string name)
    {
        EnsureWindows();
        var info = new ProcessStartInfo { FileName = ResolveWslExecutable(), UseShellExecute = true };
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

    private static async Task RunWindowsCommandAsync(string fileName, params string[] arguments)
    {
        var result = await RunWindowsCommandCaptureAsync(fileName, arguments);
        if (result.ExitCode != 0)
            throw new WslOperationException(string.IsNullOrWhiteSpace(result.Error) ? $"Command failed: {fileName}" : result.Error.Trim());
    }

    private static async Task<(int ExitCode, string Output, string Error)> RunWindowsCommandCaptureAsync(string fileName, params string[] arguments)
    {
        var info = new ProcessStartInfo { FileName = fileName, UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true, RedirectStandardError = true };
        foreach (var argument in arguments) info.ArgumentList.Add(argument);
        Process process;
        try { process = Process.Start(info) ?? throw new WslOperationException($"Could not start {fileName}."); }
        catch (Win32Exception exception) when (exception.NativeErrorCode is 2 or 3)
        { throw new WslOperationException($"{fileName} was not found. Install the required Windows component first."); }
        using (process)
        {
            var outputTask = process.StandardOutput.ReadToEndAsync();
            var errorTask = process.StandardError.ReadToEndAsync();
            await process.WaitForExitAsync();
            return (process.ExitCode, await outputTask, await errorTask);
        }
    }

    internal static string QuoteWindowsArgument(string value)
    {
        if (value.Length > 0 && !value.Any(character => char.IsWhiteSpace(character) || character == '"'))
            return value;

        var result = new StringBuilder("\"");
        var backslashes = 0;
        foreach (var character in value)
        {
            if (character == '\\')
            {
                backslashes++;
                continue;
            }

            if (character == '"')
                result.Append('\\', backslashes * 2 + 1);
            else
                result.Append('\\', backslashes);

            result.Append(character);
            backslashes = 0;
        }

        result.Append('\\', backslashes * 2);
        return result.Append('"').ToString();
    }

    private static string BuildScheduledWslCommand(string distribution, string command)
        => $"wsl.exe --distribution {QuoteWindowsArgument(distribution)} --exec sh -lc {QuoteWindowsArgument(command)}";

    private static async Task<(int ExitCode, string Output, string Error)> RunAsync(params string[] arguments)
    {
        var info = new ProcessStartInfo { FileName = ResolveWslExecutable(), UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true, RedirectStandardError = true, StandardOutputEncoding = Encoding.Unicode, StandardErrorEncoding = Encoding.Unicode };
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

    internal static void EnsureSuccessfulInstallerExitCode(int exitCode)
    {
        if (exitCode == 0)
            return;

        var hexadecimalCode = unchecked((uint)exitCode).ToString("X8");
        throw new WslOperationException($"The WSL installer failed with exit code {exitCode} (0x{hexadecimalCode}).");
    }

    private static async Task RunElevatedCheckedAsync(params string[] arguments)
    {
        var info = new ProcessStartInfo
        {
            FileName = ResolveWslExecutable(),
            UseShellExecute = true,
            Verb = "runas",
            WindowStyle = ProcessWindowStyle.Normal
        };
        foreach (var argument in arguments)
            info.ArgumentList.Add(argument);
        try
        {
            using var process = Process.Start(info) ?? throw new WslOperationException("Could not start the WSL installer.");
            await process.WaitForExitAsync();
            EnsureSuccessfulInstallerExitCode(process.ExitCode);
        }
        catch (Win32Exception exception) when (exception.NativeErrorCode == 1223)
        {
            throw new WslOperationException("The administrator approval was cancelled.");
        }
    }

    private static string ResolveWslExecutable()
    {
        var helper = Environment.GetEnvironmentVariable("WINXINAI_WSL_HELPER");
        return !string.IsNullOrWhiteSpace(helper) && File.Exists(helper) ? helper : "wsl.exe";
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

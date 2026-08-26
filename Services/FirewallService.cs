using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using PortManager.Models;

namespace PortManager.Services;

/// <summary>
/// 防火墙管理服务：封装 netsh + PowerShell 调用
/// 对应 bat 脚本的五项功能
/// </summary>
public static class FirewallService
{
    /// <summary>
    /// 查询所有已启用、带端口过滤的放行规则
    /// 对应 bat 脚本功能2
    /// </summary>
    public static async Task<List<FirewallRule>> ListRulesAsync()
    {
        var ps = $@"
Get-NetFirewallRule -Action Allow -Direction Inbound,Outbound |
  Where-Object {{ $_.Enabled -eq 'True' }} |
  ForEach-Object {{
    $pf = $_ | Get-NetFirewallPortFilter -ErrorAction SilentlyContinue
    if ($pf -and $pf.LocalPort -ne 'Any' -and $pf.LocalPort) {{
      [PSCustomObject]{{
        Name=$_.DisplayName
        Dir=$_.Direction
        Proto=$(if ($pf.Protocol -eq 'Any') {{'Any'}} else {{$pf.Protocol}})
        LocalPort=$pf.LocalPort
        Profile=$_.Profile
        Enabled=$_.Enabled
      }}
    }}
  }} | ConvertTo-Json -Compress
";

        var output = await RunPowerShellAsync(ps);
        if (string.IsNullOrWhiteSpace(output))
            return new List<FirewallRule>();

        // 单条结果返回对象而非数组，需包装
        var json = output.Trim();
        if (!json.StartsWith("["))
            json = "[" + json + "]";

        try
        {
            return JsonSerializer.Deserialize<List<FirewallRule>>(json) ?? new();
        }
        catch
        {
            return new();
        }
    }

    /// <summary>
    /// 查询指定端口的所有放行规则
    /// 对应 bat 脚本功能5
    /// </summary>
    public static async Task<List<FirewallRule>> QueryPortAsync(int port)
    {
        var all = await ListRulesAsync();
        return all.Where(r => PortRangeMatcher.Matches(r.LocalPort, port)).ToList();
    }

    /// <summary>
    /// 添加防火墙端口规则
    /// 对应 bat 脚本功能1
    /// ANY 协议自动拆成 TCP + UDP 两条规则
    /// </summary>
    public static async Task<OperationResult> AddRuleAsync(
        int port, string protocol, string direction, string ruleName)
    {
        var result = new OperationResult();
        var protocols = protocol == "ANY" ? new[] { "TCP", "UDP" } : new[] { protocol };
        var directions = direction switch
        {
            "Both" => new[] { "in", "out" },
            "out"  => new[] { "out" },
            _      => new[] { "in" }
        };

        foreach (var dir in directions)
        {
            foreach (var proto in protocols)
            {
                var name = protocol == "ANY" ? $"{ruleName}_{proto}" : ruleName;
                var ok = await AddSingleRuleAsync(name, port, proto, dir);
                if (ok) result.SuccessCount++;
                else result.FailedCount++;
            }
        }

        result.Success = result.FailedCount == 0;
        result.Message = result.FailedCount == 0
            ? $"成功添加 {result.SuccessCount} 条规则"
            : $"成功 {result.SuccessCount} 条，失败 {result.FailedCount} 条";
        return result;
    }

    /// <summary>
    /// 删除防火墙规则（按名称精确匹配）
    /// 对应 bat 脚本功能3
    /// </summary>
    public static async Task<bool> DeleteRuleAsync(string ruleName)
    {
        var (exitCode, _) = await RunNetshAsync(
            "advfirewall", "firewall", "delete", "rule", $"name={ruleName}");
        return exitCode == 0;
    }

    /// <summary>
    /// 修改规则 = 删除旧规则 + 创建新规则
    /// 对应 bat 脚本功能4
    /// </summary>
    public static async Task<OperationResult> ModifyRuleAsync(
        string oldName, int port, string protocol, string direction, string newName)
    {
        // 先删旧
        await DeleteRuleAsync(oldName);
        // 再建新
        return await AddRuleAsync(port, protocol, direction, newName);
    }

    // ===== 内部方法 =====

    private static async Task<bool> AddSingleRuleAsync(
        string name, int port, string protocol, string direction)
    {
        var (exitCode, _) = await RunNetshAsync(
            "advfirewall", "firewall", "add", "rule",
            $"name={name}", $"dir={direction}", "action=allow",
            $"protocol={protocol}", $"localport={port}", "profile=any");
        return exitCode == 0;
    }

    private static async Task<string> RunPowerShellAsync(string script)
    {
        var psi = new ProcessStartInfo
        {
            FileName = "powershell.exe",
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };
        psi.ArgumentList.Add("-NoProfile");
        psi.ArgumentList.Add("-NonInteractive");
        psi.ArgumentList.Add("-Command");
        psi.ArgumentList.Add(script);

        using var p = Process.Start(psi);
        if (p == null) return string.Empty;
        var outputTask = p.StandardOutput.ReadToEndAsync();
        var errorTask = p.StandardError.ReadToEndAsync();
        await p.WaitForExitAsync();
        var output = await outputTask;
        _ = await errorTask;
        return p.ExitCode == 0 ? output : string.Empty;
    }

    private static async Task<(int exitCode, string output)> RunNetshAsync(params string[] arguments)
    {
        var psi = new ProcessStartInfo
        {
            FileName = "netsh.exe",
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };
        foreach (var argument in arguments)
            psi.ArgumentList.Add(argument);

        using var p = Process.Start(psi);
        if (p == null) return (-1, string.Empty);
        var outputTask = p.StandardOutput.ReadToEndAsync();
        var errorTask = p.StandardError.ReadToEndAsync();
        await p.WaitForExitAsync();
        var output = await outputTask;
        var error = await errorTask;
        return (p.ExitCode, string.IsNullOrWhiteSpace(output) ? error : output);
    }
}

using System;
using System.Text.Json.Serialization;

namespace PortManager.Models;

/// <summary>
/// 防火墙规则数据模型，对应 netsh/PowerShell 查询结果
/// </summary>
public class FirewallRule
{
    [JsonPropertyName("Name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("Dir")]
    public string Direction { get; set; } = string.Empty;

    [JsonPropertyName("Proto")]
    public string Protocol { get; set; } = string.Empty;

    [JsonPropertyName("LocalPort")]
    public string LocalPort { get; set; } = string.Empty;

    [JsonPropertyName("RemotePort")]
    public string RemotePort { get; set; } = string.Empty;

    [JsonPropertyName("Profile")]
    public string Profile { get; set; } = string.Empty;

    [JsonPropertyName("Enabled")]
    public string Enabled { get; set; } = string.Empty;

    public string DirectionDisplay => Direction switch
    {
        "Inbound"  => Services.LanguageState.IsEnglish ? "Inbound" : "入站",
        "Outbound" => Services.LanguageState.IsEnglish ? "Outbound" : "出站",
        _          => Direction
    };

    public string ProtocolDisplay => Protocol switch
    {
        "6" => "TCP",
        "17" => "UDP",
        "256" => "ANY",
        "Any" => "ANY",
        _ => Protocol.ToUpperInvariant()
    };

    public string PortDisplay => Direction == "Outbound" && IsSpecificPort(RemotePort)
        ? RemotePort
        : LocalPort;

    private static bool IsSpecificPort(string value) =>
        !string.IsNullOrWhiteSpace(value) && !value.Equals("Any", StringComparison.OrdinalIgnoreCase);
}

/// <summary>
/// 操作结果
/// </summary>
public class OperationResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public int SuccessCount { get; set; }
    public int FailedCount { get; set; }
    public string ErrorMessage { get; set; } = string.Empty;
}

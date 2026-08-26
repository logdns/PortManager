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

    [JsonPropertyName("Profile")]
    public string Profile { get; set; } = string.Empty;

    [JsonPropertyName("Enabled")]
    public string Enabled { get; set; } = string.Empty;

    /// <summary>
    /// 格式化方向显示：Inbound -> 入站, Outbound -> 出站
    /// </summary>
    public string DirectionDisplay => Direction switch
    {
        "Inbound"  => "入站",
        "Outbound" => "出站",
        _          => Direction
    };

    /// <summary>
    /// 格式化协议显示
    /// </summary>
    public string ProtocolDisplay => Protocol switch
    {
        "Any" => "ANY",
        _    => Protocol
    };
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
}

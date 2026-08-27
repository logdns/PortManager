using System;
using System.Globalization;
using System.Text.Json.Serialization;

namespace PortManager.Models;

public sealed class ConnectionModel
{
    public string Protocol { get; init; } = string.Empty;
    public string LocalAddress { get; init; } = string.Empty;
    public int LocalPort { get; init; }
    public string RemoteAddress { get; init; } = string.Empty;
    public int RemotePort { get; init; }
    public string State { get; init; } = string.Empty;
    public int ProcessId { get; init; }
    public string ProcessName { get; init; } = string.Empty;

    [JsonIgnore]
    public string LocalEndpoint => $"{LocalAddress}:{LocalPort.ToString(CultureInfo.InvariantCulture)}";

    [JsonIgnore]
    public string RemoteEndpoint => RemotePort == 0
        ? RemoteAddress
        : $"{RemoteAddress}:{RemotePort.ToString(CultureInfo.InvariantCulture)}";

    [JsonIgnore]
    public string StateDisplay => State switch
    {
        "Listen" => Services.LanguageState.IsEnglish ? "Listening" : "监听",
        "Established" => Services.LanguageState.IsEnglish ? "Established" : "已建立",
        "TimeWait" => Services.LanguageState.IsEnglish ? "Time wait" : "等待关闭",
        "CloseWait" => Services.LanguageState.IsEnglish ? "Close wait" : "等待关闭",
        "Udp" => "UDP",
        _ => State
    };
}

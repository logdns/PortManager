using System;
using System.Text.Json.Serialization;

namespace PortManager.Models;

public sealed class AuditLogEntry
{
    public DateTimeOffset Timestamp { get; init; }
    public string Action { get; init; } = string.Empty;
    public string Details { get; init; } = string.Empty;
    public bool Success { get; init; }

    [JsonIgnore]
    public string ResultDisplay => Success
        ? (Services.LanguageState.IsEnglish ? "Success" : "成功")
        : (Services.LanguageState.IsEnglish ? "Failed" : "失败");
}

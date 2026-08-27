using System;

namespace PortManager.Models;

public sealed class AuditLogEntry
{
    public DateTimeOffset Timestamp { get; init; }
    public string Action { get; init; } = string.Empty;
    public string Details { get; init; } = string.Empty;
    public bool Success { get; init; }
}

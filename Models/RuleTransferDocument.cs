using System;
using System.Collections.Generic;

namespace PortManager.Models;

public sealed class RuleTransferDocument
{
    public int FormatVersion { get; init; } = 1;
    public DateTimeOffset ExportedAt { get; init; }
    public List<FirewallRule> Rules { get; set; } = new();
}

using System;

namespace PortManager.Models;

public sealed class WslDistributionModel
{
    public string Name { get; init; } = string.Empty;
    public string State { get; init; } = string.Empty;
    public int Version { get; init; }
    public bool IsDefault { get; init; }

    public string DisplayName => IsDefault ? $"{Name} (default)" : Name;
}

public sealed class WslOperationException : Exception
{
    public WslOperationException(string message) : base(message) { }
}

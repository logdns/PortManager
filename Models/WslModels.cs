using System;
using System.Collections.Generic;

namespace PortManager.Models;

public sealed class WslDistributionModel
{
    public string Name { get; init; } = string.Empty;
    public string State { get; init; } = string.Empty;
    public int Version { get; init; }
    public bool IsDefault { get; init; }
    public string VersionLabel => Version > 0 ? $"WSL {Version}" : string.Empty;

    public string DisplayName => IsDefault ? $"{Name} (default)" : Name;
}

public sealed class WslTaskModel
{
    public string Name { get; init; } = string.Empty;
    public string Distribution { get; init; } = string.Empty;
    public string Command { get; init; } = string.Empty;
    public string Schedule { get; init; } = string.Empty;
}

public sealed class WslStatusModel
{
    public bool IsInstalled { get; init; }
    public IReadOnlyList<WslDistributionModel> Distributions { get; init; } = Array.Empty<WslDistributionModel>();
    public bool HasDistributions => Distributions.Count > 0;
}

public class WslOperationException : Exception
{
    public WslOperationException(string message) : base(message) { }
}

public sealed class WslNotInstalledException : WslOperationException
{
    public WslNotInstalledException(string message) : base(message) { }
}

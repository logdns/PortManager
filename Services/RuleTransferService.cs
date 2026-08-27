using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using PortManager.Models;

namespace PortManager.Services;

public static class RuleTransferService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    public static string Serialize(IEnumerable<FirewallRule> rules)
    {
        var document = new RuleTransferDocument
        {
            FormatVersion = 1,
            ExportedAt = DateTimeOffset.Now,
            Rules = rules.ToList()
        };
        return JsonSerializer.Serialize(document, JsonOptions);
    }

    public static RuleTransferDocument Parse(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            throw new RuleTransferException("The selected file is empty.");

        try
        {
            var document = JsonSerializer.Deserialize<RuleTransferDocument>(json, JsonOptions)
                ?? throw new RuleTransferException("The selected file does not contain a rule document.");
            if (document.FormatVersion != 1)
                throw new RuleTransferException($"Unsupported rule document version: {document.FormatVersion}.");
            document.Rules ??= new List<FirewallRule>();
            Validate(document.Rules);
            return document;
        }
        catch (JsonException ex)
        {
            throw new RuleTransferException("The selected file is not valid JSON.", ex);
        }
    }

    public static async Task<OperationResult> ImportAsync(IEnumerable<FirewallRule> rules) =>
        await FirewallService.ImportRulesAsync(rules).ConfigureAwait(false);

    private static void Validate(IEnumerable<FirewallRule> rules)
    {
        foreach (var rule in rules)
        {
            if (string.IsNullOrWhiteSpace(rule.Name))
                throw new RuleTransferException("Every imported rule must have a name.");
            if (string.IsNullOrWhiteSpace(rule.Direction))
                throw new RuleTransferException($"Rule '{rule.Name}' has no direction.");
            if (string.IsNullOrWhiteSpace(rule.Protocol))
                throw new RuleTransferException($"Rule '{rule.Name}' has no protocol.");
        }
    }
}

public sealed class RuleTransferException : Exception
{
    public RuleTransferException(string message) : base(message)
    {
    }

    public RuleTransferException(string message, Exception innerException) : base(message, innerException)
    {
    }
}

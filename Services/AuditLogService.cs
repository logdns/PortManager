using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using PortManager.Models;

namespace PortManager.Services;

public static class AuditLogService
{
    private static readonly object SyncRoot = new();
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static string LogPath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "PortManager", "audit.log");

    public static void Record(string action, string details, bool success = true)
    {
        try
        {
            var entry = new AuditLogEntry
            {
                Timestamp = DateTimeOffset.Now,
                Action = action,
                Details = details,
                Success = success
            };
            var line = JsonSerializer.Serialize(entry, JsonOptions);
            lock (SyncRoot)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(LogPath)!);
                File.AppendAllText(LogPath, line + Environment.NewLine);
            }
        }
        catch
        {
            // Audit logging must never block the firewall operation.
        }
    }

    public static Task<List<AuditLogEntry>> ReadAsync() => Task.Run(() =>
    {
        lock (SyncRoot)
        {
            if (!File.Exists(LogPath))
                return new List<AuditLogEntry>();

            var entries = new List<AuditLogEntry>();
            foreach (var line in File.ReadLines(LogPath).Reverse())
            {
                try
                {
                    var entry = JsonSerializer.Deserialize<AuditLogEntry>(line, JsonOptions);
                    if (entry is not null)
                        entries.Add(entry);
                }
                catch (JsonException)
                {
                    // Keep valid entries when an interrupted write left a bad line.
                }
            }

            return entries;
        }
    });

    public static Task ClearAsync() => Task.Run(() =>
    {
        lock (SyncRoot)
        {
            if (File.Exists(LogPath))
                File.Delete(LogPath);
        }
    });
}

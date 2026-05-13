using System;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Norevia_Startup_Manager_Lite.Domain;

namespace Norevia_Startup_Manager_Lite.Services;

public sealed class BackupService : IBackupService
{
    public string GetBackupRoot()
    {
        var root = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "Norevia",
            "StartupManagerLite",
            "Backup");

        Directory.CreateDirectory(root);
        return root;
    }

    public async Task BackupBeforeChangeAsync(StartupEntry entry, CancellationToken ct)
    {
        // MVP: snimi JSON “audit” prije svake promjene
        // (RegistryRunSource već radi premještanje u RunDisabled; folder source radi premještanje fajla)
        ct.ThrowIfCancellationRequested();

        var dateFolder = Path.Combine(GetBackupRoot(), DateTime.Now.ToString("yyyy-MM-dd"));
        Directory.CreateDirectory(dateFolder);

        var safeName = MakeSafeFileName($"{entry.Location}_{entry.Name}");
        var file = Path.Combine(dateFolder, $"{safeName}_{DateTime.Now:HHmmss}.json");

        var payload = new
        {
            entry.Id,
            entry.Name,
            entry.Path,
            entry.Publisher,
            entry.Location,
            entry.Status,
            Timestamp = DateTimeOffset.Now
        };

        var json = JsonSerializer.Serialize(payload, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(file, json, ct);
    }

    private static string MakeSafeFileName(string name)
    {
        foreach (var c in Path.GetInvalidFileNameChars())
            name = name.Replace(c, '_');
        return name;
    }
}
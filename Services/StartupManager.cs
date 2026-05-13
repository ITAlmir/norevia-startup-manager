using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Norevia_Startup_Manager_Lite.Domain;

namespace Norevia_Startup_Manager_Lite.Services;

public sealed class StartupManager : IStartupManager
{
    private readonly IReadOnlyList<IStartupSource> _sources;
    private readonly IBackupService _backup;
    private readonly ICsvExporter _csv;
    private readonly IPublisherResolver _publisher;

    public StartupManager(
        IEnumerable<IStartupSource> sources,
        IBackupService backup,
        ICsvExporter csv,
        IPublisherResolver publisher)
    {
        _sources = sources?.ToList() ?? throw new ArgumentNullException(nameof(sources));
        _backup = backup ?? throw new ArgumentNullException(nameof(backup));
        _csv = csv ?? throw new ArgumentNullException(nameof(csv));
        _publisher = publisher ?? throw new ArgumentNullException(nameof(publisher));
    }

    public async Task<IReadOnlyList<StartupEntry>> GetAllAsync(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        // paralelno čitanje svih izvora
        var tasks = _sources.Select(s => s.ReadAsync(ct)).ToArray();
        var results = await Task.WhenAll(tasks);

        var all = results.SelectMany(x => x).ToList();

        // Normalizuj publisher gdje je null (opcionalno)
        // (RegistryRunSource već radi publisher, ali folder možda ne)
        all = all.Select(e =>
        {
            if (!string.IsNullOrWhiteSpace(e.Publisher)) return e;

            // pokušaj publisher ako ima exe path (ako si već normalizovao u source-u)
            // ovdje ne diramo Path string ako je pun komande; ostavimo Lite.
            return e;
        }).ToList();

        // Dedupe logika:
        // Ako ima i Enabled i Disabled entry istog "identity" (npr. HKCU Run + HKCU RunDisabled),
        // pokaži samo jedan (Disabled treba pobijediti).
        var deduped = DeduplicatePreferDisabled(all);

        // sort
        return deduped
            .OrderByDescending(x => x.Status == StartupStatus.Enabled) // enabled gore (optional)
            .ThenBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public async Task<OperationResult> ToggleAsync(StartupEntry entry, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        if (entry is null) return OperationResult.Fail("Entry is null.");

        var desiredAction = entry.Status == StartupStatus.Enabled
            ? "Disable"
            : "Enable";

        // Nađi source koji može da obradi ovu lokaciju
        var source = FindSourceFor(entry.Location);
        if (source == null)
            return OperationResult.Fail($"No startup source found for location: {entry.Location}");

        try
        {
            // Backup prije izmjene (trust + rollback)
            await _backup.BackupBeforeChangeAsync(entry, ct);

            return entry.Status == StartupStatus.Enabled
                ? await source.DisableAsync(entry, ct)
                : await source.EnableAsync(entry, ct);
        }
        catch (Exception ex)
        {
            return OperationResult.Fail($"{desiredAction} failed: {ex.Message}");
        }
    }

    public async Task<string> ExportCsvAsync(IEnumerable<StartupEntry> entries, string outputPath, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(outputPath))
            throw new ArgumentException("Output path is empty.", nameof(outputPath));

        var list = entries?.ToList() ?? new List<StartupEntry>();

        await _csv.ExportAsync(list, outputPath, ct);
        return outputPath;
    }

    private IStartupSource? FindSourceFor(StartupLocation location)
    {
        // Registry sources: HKCU/HKLM i njihove Disabled varijante mapiramo na isti source.
        // Folder: direktno.
        return _sources.FirstOrDefault(s =>
        {
            if (s.Location == location) return true;

            // Mapiranja za RegistryRunSource:
            if (location == StartupLocation.Registry_HKCU_RunDisabled && s.Location == StartupLocation.Registry_HKCU_Run)
                return true;

            if (location == StartupLocation.Registry_HKLM_RunDisabled && s.Location == StartupLocation.Registry_HKLM_Run)
                return true;

            return false;
        });
    }

    private static List<StartupEntry> DeduplicatePreferDisabled(List<StartupEntry> all)
    {
        // Ključ za spajanje: hive-scope + name
        // Pošto u modelu nemamo hive direktno, oslanjamo se na Location + Name.
        // Disabled treba pobijediti.
        string Key(StartupEntry e)
        {
            var scope = e.Location switch
            {
                StartupLocation.Registry_HKCU_Run or StartupLocation.Registry_HKCU_RunDisabled => "HKCU",
                StartupLocation.Registry_HKLM_Run or StartupLocation.Registry_HKLM_RunDisabled => "HKLM",
                StartupLocation.StartupFolder_User => "FOLDER_USER",
                _ => e.Location.ToString()
            };

            return $"{scope}:{e.Name}".ToLowerInvariant();
        }

        var map = new Dictionary<string, StartupEntry>();

        foreach (var e in all)
        {
            var k = Key(e);

            if (!map.TryGetValue(k, out var existing))
            {
                map[k] = e;
                continue;
            }

            // Ako je jedan disabled a drugi enabled — uzmi disabled
            if (existing.Status == StartupStatus.Enabled && e.Status == StartupStatus.Disabled)
            {
                map[k] = e;
                continue;
            }

            // Ako su oba enabled ili oba disabled:
            // preferiraj onaj koji ima Publisher, ili kraći/čišći Path (optional)
            if (string.IsNullOrWhiteSpace(existing.Publisher) && !string.IsNullOrWhiteSpace(e.Publisher))
            {
                map[k] = e;
                continue;
            }
        }

        return map.Values.ToList();
    }
}
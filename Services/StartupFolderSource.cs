using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Norevia_Startup_Manager_Lite.Domain;

namespace Norevia_Startup_Manager_Lite.Services;

public sealed class StartupFolderSource : IStartupSource
{
    public StartupLocation Location => StartupLocation.StartupFolder_User;

    private readonly IBackupService _backup;

    private readonly string _startupFolder;
    private readonly string _disabledFolder;

    public StartupFolderSource(IBackupService backup)
    {
        _backup = backup ?? throw new ArgumentNullException(nameof(backup));

        _startupFolder = Environment.GetFolderPath(Environment.SpecialFolder.Startup);

        // U backup root-u pravimo Disabled\StartupFolder_User
        _disabledFolder = Path.Combine(_backup.GetBackupRoot(), "Disabled", "StartupFolder_User");
        Directory.CreateDirectory(_disabledFolder);
    }

    public Task<IReadOnlyList<StartupEntry>> ReadAsync(CancellationToken ct)
    {
        return Task.Run(() =>
        {
            ct.ThrowIfCancellationRequested();

            var list = new List<StartupEntry>();

            // Enabled = fajlovi u Startup folderu
            if (Directory.Exists(_startupFolder))
            {
                foreach (var file in Directory.EnumerateFiles(_startupFolder))
                {
                    ct.ThrowIfCancellationRequested();

                    list.Add(new StartupEntry
                    {
                        Id = $"folder_user:enabled:{Path.GetFileName(file)}".ToLowerInvariant(),
                        Name = Path.GetFileNameWithoutExtension(file),
                        Path = file,
                        Publisher = null,
                        Location = StartupLocation.StartupFolder_User,
                        Status = StartupStatus.Enabled
                    });
                }
            }

            // Disabled = fajlovi koje smo premjestili u _disabledFolder
            if (Directory.Exists(_disabledFolder))
            {
                foreach (var file in Directory.EnumerateFiles(_disabledFolder))
                {
                    ct.ThrowIfCancellationRequested();

                    list.Add(new StartupEntry
                    {
                        Id = $"folder_user:disabled:{Path.GetFileName(file)}".ToLowerInvariant(),
                        Name = Path.GetFileNameWithoutExtension(file),
                        Path = file,
                        Publisher = null,
                        Location = StartupLocation.StartupFolder_User,
                        Status = StartupStatus.Disabled
                    });
                }
            }

            return (IReadOnlyList<StartupEntry>)list
                .OrderBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }, ct);
    }

    public Task<OperationResult> DisableAsync(StartupEntry entry, CancellationToken ct)
    {
        return Task.Run(() =>
        {
            ct.ThrowIfCancellationRequested();

            if (entry.Status != StartupStatus.Enabled)
                return OperationResult.Ok();

            try
            {
                var sourcePath = entry.Path;
                if (!File.Exists(sourcePath))
                    return OperationResult.Fail("Startup file not found.");

                var fileName = Path.GetFileName(sourcePath);
                var targetPath = Path.Combine(_disabledFolder, fileName);

                // Backup audit (json) je već u BackupService, a ovdje radimo stvarni move
                // Ako target postoji, dodaj suffix
                targetPath = EnsureUnique(targetPath);

                File.Move(sourcePath, targetPath);
                return OperationResult.Ok();
            }
            catch (Exception ex)
            {
                return OperationResult.Fail(ex.Message);
            }
        }, ct);
    }

    public Task<OperationResult> EnableAsync(StartupEntry entry, CancellationToken ct)
    {
        return Task.Run(() =>
        {
            ct.ThrowIfCancellationRequested();

            if (entry.Status != StartupStatus.Disabled)
                return OperationResult.Ok();

            try
            {
                var sourcePath = entry.Path;
                if (!File.Exists(sourcePath))
                    return OperationResult.Fail("Disabled startup file not found.");

                var fileName = Path.GetFileName(sourcePath);
                var targetPath = Path.Combine(_startupFolder, fileName);

                Directory.CreateDirectory(_startupFolder);

                targetPath = EnsureUnique(targetPath);

                File.Move(sourcePath, targetPath);
                return OperationResult.Ok();
            }
            catch (Exception ex)
            {
                return OperationResult.Fail(ex.Message);
            }
        }, ct);
    }

    private static string EnsureUnique(string path)
    {
        if (!File.Exists(path)) return path;

        var dir = Path.GetDirectoryName(path)!;
        var name = Path.GetFileNameWithoutExtension(path);
        var ext = Path.GetExtension(path);

        for (int i = 1; i < 9999; i++)
        {
            var candidate = Path.Combine(dir, $"{name}_{i}{ext}");
            if (!File.Exists(candidate))
                return candidate;
        }

        // fallback
        return Path.Combine(dir, $"{name}_{Guid.NewGuid():N}{ext}");
    }
}
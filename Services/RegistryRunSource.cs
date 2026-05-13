using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Win32;
using Norevia_Startup_Manager_Lite.Domain;
using Norevia_Startup_Manager_Lite.Infrastructure;

namespace Norevia_Startup_Manager_Lite.Services;

public sealed class RegistryRunSource : IStartupSource
{
    // Windows "Run" ključevi
    private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    // Naš "disabled" container
    private const string RunDisabledKeyPath = @"Software\Microsoft\Windows\CurrentVersion\RunDisabled";

    public StartupLocation Location { get; }
    private readonly RegistryHive _hive;
    private readonly IPublisherResolver _publisher;

    public RegistryRunSource(StartupLocation location, RegistryHive hive, IPublisherResolver? publisherResolver = null)
    {
        Location = location;
        _hive = hive;
        _publisher = publisherResolver ?? new PublisherResolver();
    }

    public Task<IReadOnlyList<StartupEntry>> ReadAsync(CancellationToken ct)
    {
        return Task.Run(() =>
        {
            ct.ThrowIfCancellationRequested();

            var enabled = ReadKeyEntries(RunKeyPath, isDisabledBucket: false, ct);
            var disabled = ReadKeyEntries(RunDisabledKeyPath, isDisabledBucket: true, ct);

            // merge
            return (IReadOnlyList<StartupEntry>)enabled.Concat(disabled)
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
                // probaj prvo 64 pa 32, ali i realno provjeri gdje value postoji
                OperationResult TryDisableInView(RegistryView view)
                {
                    using var runKey = RegistryUtil.OpenWritable(_hive, RunKeyPath, view);
                    if (runKey == null) return OperationResult.Fail("Run key not found.");

                    var value = runKey.GetValue(entry.Name) as string;
                    if (string.IsNullOrWhiteSpace(value))
                        return OperationResult.Fail("Value not found in this registry view.");

                    using var disabledKey = RegistryUtil.CreateOrOpenWritable(_hive, RunDisabledKeyPath, view);
                    disabledKey.SetValue(entry.Name, value, RegistryValueKind.String);

                    runKey.DeleteValue(entry.Name, throwOnMissingValue: false);
                    return OperationResult.Ok();
                }

                var r64 = TryDisableInView(RegistryView.Registry64);
                if (r64.Success) return r64;

                var r32 = TryDisableInView(RegistryView.Registry32);
                if (r32.Success) return r32;

                // Ako oba failaju, vrati smisleniju poruku
                if (_hive == RegistryHive.LocalMachine)
                    return OperationResult.Fail("Requires Administrator privileges to modify HKLM startup entries.");

                return OperationResult.Fail("Could not modify this entry (not found or access denied).");
            }
            catch (Exception ex) when (RegistryUtil.IsUnauthorized(ex))
            {
                return OperationResult.Fail(
                    _hive == RegistryHive.LocalMachine
                        ? "Requires Administrator privileges to modify HKLM startup entries."
                        : "Access denied.");
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
                OperationResult TryEnableInView(RegistryView view)
                {
                    using var disabledKey = RegistryUtil.OpenWritable(_hive, RunDisabledKeyPath, view);
                    if (disabledKey == null) return OperationResult.Fail("RunDisabled key not found.");

                    var value = disabledKey.GetValue(entry.Name) as string;
                    if (string.IsNullOrWhiteSpace(value))
                        return OperationResult.Fail("Value not found in this registry view.");

                    using var runKey = RegistryUtil.CreateOrOpenWritable(_hive, RunKeyPath, view);
                    runKey.SetValue(entry.Name, value, RegistryValueKind.String);

                    disabledKey.DeleteValue(entry.Name, throwOnMissingValue: false);
                    return OperationResult.Ok();
                }

                var r64 = TryEnableInView(RegistryView.Registry64);
                if (r64.Success) return r64;

                var r32 = TryEnableInView(RegistryView.Registry32);
                if (r32.Success) return r32;

                if (_hive == RegistryHive.LocalMachine)
                    return OperationResult.Fail("Requires Administrator privileges to modify HKLM startup entries.");

                return OperationResult.Fail("Could not modify this entry (not found or access denied).");
            }
            catch (Exception ex) when (RegistryUtil.IsUnauthorized(ex))
            {
                return OperationResult.Fail(
                    _hive == RegistryHive.LocalMachine
                        ? "Requires Administrator privileges to modify HKLM startup entries."
                        : "Access denied.");
            }
            catch (Exception ex)
            {
                return OperationResult.Fail(ex.Message);
            }
        }, ct);
    }

    private IEnumerable<StartupEntry> ReadKeyEntries(string subKeyPath, bool isDisabledBucket, CancellationToken ct)
    {
        var (key, _) = RegistryUtil.TryOpenReadable64Then32(_hive, subKeyPath);
        if (key == null) yield break;

        try
        {
            foreach (var name in key.GetValueNames())
            {
                ct.ThrowIfCancellationRequested();

                var raw = key.GetValue(name) as string;
                if (string.IsNullOrWhiteSpace(raw)) continue;

                var exePath = PathUtil.ExtractExecutablePath(raw);
                var publisher = _publisher.TryResolvePublisher(exePath);

                yield return new StartupEntry
                {
                    Id = $"{_hive}:{subKeyPath}:{name}".ToLowerInvariant(),
                    Name = name,
                    Path = raw,
                    Publisher = publisher,
                    Location = MapLocation(subKeyPath),
                    Status = isDisabledBucket ? StartupStatus.Disabled : StartupStatus.Enabled
                };
            }
        }
        finally
        {
            key.Dispose();
        }
    }

    private StartupLocation MapLocation(string subKeyPath)
    {
        bool isHkcu = _hive == RegistryHive.CurrentUser;

        if (subKeyPath.Equals(RunDisabledKeyPath, StringComparison.OrdinalIgnoreCase))
            return isHkcu ? StartupLocation.Registry_HKCU_RunDisabled : StartupLocation.Registry_HKLM_RunDisabled;

        return isHkcu ? StartupLocation.Registry_HKCU_Run : StartupLocation.Registry_HKLM_Run;
    }
}

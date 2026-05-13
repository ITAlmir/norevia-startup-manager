using System;
using Microsoft.Win32;

namespace Norevia_Startup_Manager_Lite.Infrastructure;

public static class RegistryUtil
{
    public static RegistryKey? OpenReadable(RegistryHive hive, string subKey, RegistryView view)
    {
        var baseKey = RegistryKey.OpenBaseKey(hive, view);
        return baseKey.OpenSubKey(subKey, writable: false);
    }

    public static RegistryKey? OpenWritable(RegistryHive hive, string subKey, RegistryView view)
    {
        var baseKey = RegistryKey.OpenBaseKey(hive, view);
        return baseKey.OpenSubKey(subKey, writable: true);
    }

    public static RegistryKey CreateOrOpenWritable(RegistryHive hive, string subKey, RegistryView view)
    {
        var baseKey = RegistryKey.OpenBaseKey(hive, view);
        return baseKey.CreateSubKey(subKey, writable: true)
               ?? throw new InvalidOperationException($"Failed to create/open registry key: {hive}\\{subKey} ({view})");
    }

    public static bool IsUnauthorized(Exception ex) => ex is UnauthorizedAccessException;

    // helper: probaj 64 pa 32
    public static (RegistryKey? key, RegistryView view) TryOpenWritable64Then32(RegistryHive hive, string subKey)
    {
        var k64 = OpenWritable(hive, subKey, RegistryView.Registry64);
        if (k64 != null) return (k64, RegistryView.Registry64);

        var k32 = OpenWritable(hive, subKey, RegistryView.Registry32);
        return (k32, RegistryView.Registry32);
    }

    public static (RegistryKey? key, RegistryView view) TryOpenReadable64Then32(RegistryHive hive, string subKey)
    {
        var k64 = OpenReadable(hive, subKey, RegistryView.Registry64);
        if (k64 != null) return (k64, RegistryView.Registry64);

        var k32 = OpenReadable(hive, subKey, RegistryView.Registry32);
        return (k32, RegistryView.Registry32);
    }
}
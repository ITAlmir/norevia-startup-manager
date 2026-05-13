using System;

namespace Norevia_Startup_Manager_Lite.Infrastructure;

public static class PathUtil
{
    public static string ExtractExecutablePath(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return string.Empty;

        raw = raw.Trim();

        // Ako počinje sa navodnicima: "C:\...\app.exe" --arg
        if (raw.StartsWith("\""))
        {
            var endQuote = raw.IndexOf('"', 1);
            if (endQuote > 1)
                return raw.Substring(1, endQuote - 1).Trim();
        }

        // Inače uzmi do prvog space-a
        var firstSpace = raw.IndexOf(' ');
        if (firstSpace > 0)
            return raw.Substring(0, firstSpace).Trim();

        return raw;
    }
}

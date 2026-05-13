using System.Diagnostics;
using System.IO;

namespace Norevia_Startup_Manager_Lite.Services;

public sealed class PublisherResolver : IPublisherResolver
{
    public string? TryResolvePublisher(string rawPath)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(rawPath)) return null;

            // rawPath ovdje očekujemo kao exe path (već "ExtractExecutablePath")
            if (!File.Exists(rawPath)) return null;

            var info = FileVersionInfo.GetVersionInfo(rawPath);
            return string.IsNullOrWhiteSpace(info.CompanyName) ? null : info.CompanyName;
        }
        catch
        {
            return null;
        }
    }
}

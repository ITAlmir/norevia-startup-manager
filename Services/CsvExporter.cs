using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Norevia_Startup_Manager_Lite.Domain;

namespace Norevia_Startup_Manager_Lite.Services;

public sealed class CsvExporter : ICsvExporter
{
    public async Task ExportAsync(IEnumerable<StartupEntry> entries, string filePath, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        var sb = new StringBuilder();
        sb.AppendLine("Name,Publisher,Path,Location,Status");

        foreach (var e in entries)
        {
            sb.Append(Escape(e.Name)).Append(',')
              .Append(Escape(e.Publisher ?? "")).Append(',')
              .Append(Escape(e.Path)).Append(',')
              .Append(Escape(e.Location.ToString())).Append(',')
              .Append(Escape(e.Status.ToString()))
              .AppendLine();
        }

        await File.WriteAllTextAsync(filePath, sb.ToString(), Encoding.UTF8, ct);
    }

    private static string Escape(string s)
    {
        s ??= "";
        if (s.Contains('"') || s.Contains(',') || s.Contains('\n') || s.Contains('\r'))
        {
            s = s.Replace("\"", "\"\"");
            return $"\"{s}\"";
        }
        return s;
    }
}
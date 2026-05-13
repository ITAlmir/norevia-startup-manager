using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Norevia_Startup_Manager_Lite.Domain;

namespace Norevia_Startup_Manager_Lite.Services;

public interface ICsvExporter
{
    Task ExportAsync(IEnumerable<StartupEntry> entries, string filePath, CancellationToken ct);
}
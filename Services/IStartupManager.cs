using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Norevia_Startup_Manager_Lite.Domain;

namespace Norevia_Startup_Manager_Lite.Services;

public interface IStartupManager
{
    Task<IReadOnlyList<StartupEntry>> GetAllAsync(CancellationToken ct);
    Task<OperationResult> ToggleAsync(StartupEntry entry, CancellationToken ct);
    Task<string> ExportCsvAsync(IEnumerable<StartupEntry> entries, string outputPath, CancellationToken ct);
}
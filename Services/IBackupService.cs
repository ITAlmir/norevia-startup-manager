using System.Threading;
using System.Threading.Tasks;
using Norevia_Startup_Manager_Lite.Domain;

namespace Norevia_Startup_Manager_Lite.Services;

public interface IBackupService
{
    Task BackupBeforeChangeAsync(StartupEntry entry, CancellationToken ct);
    string GetBackupRoot();
}
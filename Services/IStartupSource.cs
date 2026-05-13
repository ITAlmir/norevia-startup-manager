using Norevia_Startup_Manager_Lite.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Norevia_Startup_Manager_Lite.Services
{
    public interface IStartupSource
    {
        StartupLocation Location { get; }
        Task<IReadOnlyList<StartupEntry>> ReadAsync(CancellationToken ct);
        Task<OperationResult> DisableAsync(StartupEntry entry, CancellationToken ct);
        Task<OperationResult> EnableAsync(StartupEntry entry, CancellationToken ct);
    }
}

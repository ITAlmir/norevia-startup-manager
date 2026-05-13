using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Norevia_Startup_Manager_Lite.Domain
{
    public sealed class StartupEntry
    {
        public string Id { get; init; } = "";              // stabilan ključ (npr. location+name+path)
        public string Name { get; init; } = "";
        public string Path { get; init; } = "";
        public string? Publisher { get; init; }            // opcionalno u Lite
        public StartupLocation Location { get; init; }
        public StartupStatus Status { get; init; }
    }
}

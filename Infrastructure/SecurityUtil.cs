using System.Security.Principal;

namespace Norevia_Startup_Manager_Lite.Infrastructure;

public static class SecurityUtil
{
    public static bool IsRunningAsAdmin()
    {
        using var identity = WindowsIdentity.GetCurrent();
        var principal = new WindowsPrincipal(identity);
        return principal.IsInRole(WindowsBuiltInRole.Administrator);
    }
}
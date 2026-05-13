using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Norevia_Startup_Manager_Lite.Domain;

public enum StartupLocation
{
    Registry_HKCU_Run,
    Registry_HKLM_Run,
    StartupFolder_User,

    // koristimo za "Disable" preko premještanja vrijednosti
    Registry_HKCU_RunDisabled,
    Registry_HKLM_RunDisabled
}

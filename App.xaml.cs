using System.Windows;
using Microsoft.Win32;
using Norevia_Startup_Manager_Lite.Services;
using Norevia_Startup_Manager_Lite.ViewModels;
using Norevia_Startup_Manager_Lite.Domain;

namespace Norevia_Startup_Manager_Lite;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Services
        IBackupService backup = new BackupService();
        IPublisherResolver publisher = new PublisherResolver();

        var sources = new IStartupSource[]
        {
            new RegistryRunSource(StartupLocation.Registry_HKCU_Run, Microsoft.Win32.RegistryHive.CurrentUser, publisher),
            new RegistryRunSource(StartupLocation.Registry_HKLM_Run, Microsoft.Win32.RegistryHive.LocalMachine, publisher),
            new StartupFolderSource(backup)
        };

        ICsvExporter csv = new CsvExporter();
        var manager = new StartupManager(sources, backup, csv, publisher);

        var vm = new MainViewModel(manager);

        var w = new MainWindow { DataContext = vm };
        w.Show();

    }
}
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Input;
using Microsoft.Win32;
using Norevia_Startup_Manager_Lite.Domain;
using Norevia_Startup_Manager_Lite.Services;

namespace Norevia_Startup_Manager_Lite.ViewModels;

public sealed class MainViewModel : INotifyPropertyChanged
{
    private readonly IStartupManager _manager;
    private readonly ObservableCollection<StartupEntryViewModel> _items = new();

    private string _searchText = "";
    private bool _isBusy;
    private string _statusMessage = "";

    // status lock (da se warning ne pregazi)
    private DateTime _lockStatusUntil = DateTime.MinValue;

    public event PropertyChangedEventHandler? PropertyChanged;

    public ICollectionView ItemsView { get; }

    public bool IsAdmin { get; } =
        Norevia_Startup_Manager_Lite.Infrastructure.SecurityUtil.IsRunningAsAdmin();

    private bool _overlayVisible;
    private bool _overlayIsError;
    private string _overlayText = "Working...";

    public bool OverlayVisible
    {
        get => _overlayVisible;
        private set { _overlayVisible = value; OnPropertyChanged(); }
    }

    public bool OverlayIsError
    {
        get => _overlayIsError;
        private set { _overlayIsError = value; OnPropertyChanged(); }
    }

    public string OverlayText
    {
        get => _overlayText;
        private set { _overlayText = value; OnPropertyChanged(); }
    }

    private async Task ShowOverlayAsync(string text, bool isError, int ms)
    {
        OverlayText = text;
        OverlayIsError = isError;
        OverlayVisible = true;

        if (ms > 0)
            await Task.Delay(ms);

        // Ako je i dalje busy, ne skrivaj overlay (ostaje "Working")
        if (!IsBusy)
            OverlayVisible = false;
    }

    public string SearchText
    {
        get => _searchText;
        set
        {
            if (_searchText == value) return;
            _searchText = value;
            OnPropertyChanged();
            ItemsView.Refresh();
            OnPropertyChanged(nameof(TotalCount));
            OnPropertyChanged(nameof(EnabledCount));
            RaiseCanExecuteChanged();
        }
    }

    public bool IsBusy
    {
        get => _isBusy;
        private set
        {
            if (_isBusy == value) return;
            _isBusy = value;
            OnPropertyChanged();
            RaiseCanExecuteChanged();
        }
    }

    public string StatusMessage
    {
        get => _statusMessage;
        private set
        {
            if (_statusMessage == value) return;
            _statusMessage = value;
            OnPropertyChanged();
        }
    }

    public int TotalCount => ItemsView.Cast<object>().Count();
    public int EnabledCount => ItemsView.Cast<StartupEntryViewModel>().Count(x => x.Model.Status == StartupStatus.Enabled);

    public ICommand RefreshCommand { get; }
    public ICommand ExportCsvCommand { get; }
    public ICommand RestartAsAdminCommand { get; }

    public MainViewModel(IStartupManager manager)
    {
        _manager = manager ?? throw new ArgumentNullException(nameof(manager));

        ItemsView = CollectionViewSource.GetDefaultView(_items);
        ItemsView.Filter = FilterItem;

        RefreshCommand = new RelayCommand(async _ => await RefreshAsync(), _ => !IsBusy);
        ExportCsvCommand = new RelayCommand(async _ => await ExportCsvAsync(), _ => !IsBusy && TotalCount > 0);
        RestartAsAdminCommand = new RelayCommand(_ => RestartAsAdmin(), _ => !IsAdmin);

        _ = RefreshAsync();
    }

    // ---------- UX helpers ----------

    private void SetStatus(string message, int lockMs = 0)
    {
        if (lockMs > 0)
            _lockStatusUntil = DateTime.Now.AddMilliseconds(lockMs);

        // Ne prepisuj zaključanu poruku sa "Loaded..."
        if (DateTime.Now < _lockStatusUntil &&
            message.StartsWith("Loaded", StringComparison.OrdinalIgnoreCase))
            return;

        StatusMessage = message;
    }

    private static async Task EnsureMinDurationAsync(DateTime startedAt, int minMs)
    {
        var elapsed = (int)(DateTime.Now - startedAt).TotalMilliseconds;
        var remaining = minMs - elapsed;
        if (remaining > 0)
            await Task.Delay(remaining);
    }

    // ---------- filtering ----------

    private bool FilterItem(object obj)
    {
        if (obj is not StartupEntryViewModel vm) return false;

        if (string.IsNullOrWhiteSpace(SearchText))
            return true;

        var s = SearchText.Trim();

        return (vm.Name?.Contains(s, StringComparison.OrdinalIgnoreCase) ?? false)
            || (vm.Publisher?.Contains(s, StringComparison.OrdinalIgnoreCase) ?? false)
            || (vm.Path?.Contains(s, StringComparison.OrdinalIgnoreCase) ?? false)
            || (vm.Location?.Contains(s, StringComparison.OrdinalIgnoreCase) ?? false);
    }

    // ---------- main actions ----------

    public async Task RefreshAsync()
    {
        if (IsBusy) return;

        var started = DateTime.Now;

        IsBusy = true;
        await Task.Yield();

        // overlay ON
        OverlayText = "Loading startup entries...";
        OverlayIsError = false;
        OverlayVisible = true;

        try
        {
            await RefreshCoreAsync();

            // minimum trajanje (da UI ne "blinka")
            await EnsureMinDurationAsync(started, 1200);

            // overlay OFF
            OverlayVisible = false;

            // status dole (opcionalno)
            SetStatus($"Loaded {TotalCount} entries. Enabled: {EnabledCount}.");
        }
        catch (Exception ex)
        {
            // crveno gore u overlay
            OverlayText = $"FAILED: {ex.Message}";
            OverlayIsError = true;

            // zadrži 5 sekundi, pa sakrij
            await Task.Delay(5000);
            OverlayVisible = false;

            SetStatus($"Error: {ex.Message}", 5000);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task ToggleAsync(StartupEntryViewModel item)
    {
        if (IsBusy) return;
        if (item is null) return;

        bool isHKLM = item.Model.Location is
            StartupLocation.Registry_HKLM_Run or
            StartupLocation.Registry_HKLM_RunDisabled;

        if (isHKLM && !IsAdmin)
        {
            _ = ShowOverlayAsync("FAILED: Requires Administrator. Click 'Restart as Administrator'.", isError: true, ms: 6000);
            return;
        }

        var started = DateTime.Now;

        IsBusy = true;
        await Task.Yield();
        _ = ShowOverlayAsync("Working...", isError: false, ms: 0);

        var old = item.Model;

        // optimistic UI (da user odmah vidi promjenu)
        var optimisticStatus = old.Status == StartupStatus.Enabled
            ? StartupStatus.Disabled
            : StartupStatus.Enabled;

        item.Update(new StartupEntry
        {
            Id = old.Id,
            Name = old.Name,
            Path = old.Path,
            Publisher = old.Publisher,
            Location = old.Location,
            Status = optimisticStatus
        });

        try
        {
            var result = await _manager.ToggleAsync(old, CancellationToken.None);

            await EnsureMinDurationAsync(started, 3000);

            if (!result.Success)
            {
                item.Update(old);

                var err = string.IsNullOrWhiteSpace(result.Error) ? "Unknown error" : result.Error;

                // Ako liči na admin problem, reci to jasno
                if (!IsAdmin && (
                    err.Contains("access", StringComparison.OrdinalIgnoreCase) ||
                    err.Contains("denied", StringComparison.OrdinalIgnoreCase) ||
                    err.Contains("unauthorized", StringComparison.OrdinalIgnoreCase) ||
                    err.Contains("administrator", StringComparison.OrdinalIgnoreCase)))
                {
                    // pokaži crveno u overlay-u 6 sekundi
                    _ = ShowOverlayAsync("FAILED: Requires Administrator. Click 'Restart as Administrator'.", isError: true, ms: 6000);
                }
                else
                {
                    _ = ShowOverlayAsync($"FAILED: {err}", isError: true, ms: 6000);
                }

                return;
            }

            _ = ShowOverlayAsync("Done.", isError: false, ms: 1200);
        }
        catch (Exception ex)
        {
            item.Update(old);
            _ = ShowOverlayAsync($"FAILED: {ex.Message}", isError: true, ms: 6000);
        }
        finally
        {
            IsBusy = false;

            // ako nije error overlay, sakrij ga
            if (!OverlayIsError)
                OverlayVisible = false;
        }
    }

    private async Task RefreshCoreAsync()
    {
        var list = await _manager.GetAllAsync(CancellationToken.None);

        _items.Clear();
        foreach (var entry in list)
            _items.Add(new StartupEntryViewModel(entry, ToggleAsync));

        ItemsView.Refresh();
        OnPropertyChanged(nameof(TotalCount));
        OnPropertyChanged(nameof(EnabledCount));

        SetStatus($"Loaded {TotalCount} entries. Enabled: {EnabledCount}.");
    }

    private async Task ExportCsvAsync()
    {
        if (IsBusy) return;

        var sfd = new SaveFileDialog
        {
            Title = "Export Startup List",
            Filter = "CSV file (*.csv)|*.csv",
            FileName = $"norevia-startup-list-{DateTime.Now:yyyy-MM-dd}.csv"
        };

        if (sfd.ShowDialog() != true)
            return;

        IsBusy = true;
        SetStatus("Exporting CSV...");
        await Task.Yield();

        try
        {
            var entries = ItemsView.Cast<StartupEntryViewModel>().Select(x => x.Model).ToList();
            var path = sfd.FileName;

            var dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrWhiteSpace(dir))
                Directory.CreateDirectory(dir);

            await _manager.ExportCsvAsync(entries, path, CancellationToken.None);

            SetStatus($"Exported: {path}", 3000);
        }
        catch (Exception ex)
        {
            SetStatus($"Export failed: {ex.Message}", 4000);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void RaiseCanExecuteChanged()
    {
        if (RefreshCommand is RelayCommand rc1) rc1.RaiseCanExecuteChanged();
        if (ExportCsvCommand is RelayCommand rc2) rc2.RaiseCanExecuteChanged();
        if (RestartAsAdminCommand is RelayCommand rc3) rc3.RaiseCanExecuteChanged();
    }

    private void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

    private void RestartAsAdmin()
    {
        try
        {
            var exePath = System.Diagnostics.Process.GetCurrentProcess().MainModule!.FileName;

            var startInfo = new System.Diagnostics.ProcessStartInfo(exePath)
            {
                UseShellExecute = true,
                Verb = "runas"
            };

            System.Diagnostics.Process.Start(startInfo);
            System.Windows.Application.Current.Shutdown();
        }
        catch
        {
            SetStatus("Administrator permission was cancelled.", 3000);
        }
    }
}
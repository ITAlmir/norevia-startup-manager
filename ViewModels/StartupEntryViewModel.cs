using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using Norevia_Startup_Manager_Lite.Domain;

namespace Norevia_Startup_Manager_Lite.ViewModels;

public sealed class StartupEntryViewModel : INotifyPropertyChanged
{
    private StartupEntry _model;
    private readonly Func<StartupEntryViewModel, Task> _toggleAsync;

    public event PropertyChangedEventHandler? PropertyChanged;

    public StartupEntry Model => _model;

    public string Name => _model.Name;
    public string? Publisher => _model.Publisher;
    public string Path => _model.Path;
    public string Location => _model.Location.ToString();
    public string Status => _model.Status.ToString();

    public ICommand ToggleCommand { get; }

    public StartupEntryViewModel(StartupEntry model, Func<StartupEntryViewModel, Task> toggleAsync)
    {
        _model = model;
        _toggleAsync = toggleAsync;

        ToggleCommand = new RelayCommand(async _ => await _toggleAsync(this));
    }

    public void Update(StartupEntry newModel)
    {
        _model = newModel;

        // 🔥 Ovo je ključ: prisili UI da refresha kolone
        OnPropertyChanged(nameof(Model));
        OnPropertyChanged(nameof(Name));
        OnPropertyChanged(nameof(Publisher));
        OnPropertyChanged(nameof(Path));
        OnPropertyChanged(nameof(Location));
        OnPropertyChanged(nameof(Status));
    }

    private void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
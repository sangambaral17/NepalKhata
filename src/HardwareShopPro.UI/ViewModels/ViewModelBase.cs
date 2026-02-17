using CommunityToolkit.Mvvm.ComponentModel;

namespace HardwareShopPro.UI.ViewModels;

/// <summary>
/// Base class for all ViewModels. Provides INotifyPropertyChanged via ObservableObject.
/// </summary>
public abstract class ViewModelBase : ObservableObject
{
    private bool _isLoading;
    public bool IsLoading
    {
        get => _isLoading;
        set => SetProperty(ref _isLoading, value);
    }

    private string? _errorMessage;
    public string? ErrorMessage
    {
        get => _errorMessage;
        set => SetProperty(ref _errorMessage, value);
    }

    /// <summary>
    /// Called when the view is navigated to. Override to load data.
    /// </summary>
    public virtual Task LoadAsync() => Task.CompletedTask;
}

namespace HardwareShopPro.UI.Services;

/// <summary>
/// Simple navigation service for switching views in the MainWindow content area.
/// </summary>
public class NavigationService
{
    private readonly Func<Type, ViewModels.ViewModelBase> _viewModelFactory;

    public event Action<ViewModels.ViewModelBase>? CurrentViewChanged;

    public ViewModels.ViewModelBase? CurrentView { get; private set; }

    public NavigationService(Func<Type, ViewModels.ViewModelBase> viewModelFactory)
    {
        _viewModelFactory = viewModelFactory;
    }

    /// <summary>
    /// Navigate to a view by its ViewModel type.
    /// </summary>
    public async Task NavigateToAsync<TViewModel>() where TViewModel : ViewModels.ViewModelBase
    {
        var vm = _viewModelFactory(typeof(TViewModel));
        CurrentView = vm;
        CurrentViewChanged?.Invoke(vm);
        await vm.LoadAsync();
    }

    /// <summary>
    /// Navigate to a specific ViewModel instance.
    /// </summary>
    public async Task NavigateToAsync(ViewModels.ViewModelBase viewModel)
    {
        CurrentView = viewModel;
        CurrentViewChanged?.Invoke(viewModel);
        await viewModel.LoadAsync();
    }
}

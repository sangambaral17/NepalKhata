using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HardwareShopPro.Core.Models;
using HardwareShopPro.Core.Services;
using HardwareShopPro.UI.Services;
using MaterialDesignThemes.Wpf;
using Serilog;

namespace HardwareShopPro.UI.ViewModels;

/// <summary>
/// Main shell ViewModel. Manages navigation, current user, and theme toggle.
/// </summary>
public partial class MainViewModel : ViewModelBase
{
    private readonly NavigationService _navigation;
    private readonly AuthenticationService _authService;
    private static readonly ILogger Logger = Log.ForContext<MainViewModel>();

    [ObservableProperty] private ViewModelBase? _currentView;
    [ObservableProperty] private string _currentViewTitle = "Dashboard";
    [ObservableProperty] private User? _currentUser;
    [ObservableProperty] private bool _isDarkTheme = true;
    [ObservableProperty] private string _selectedMenuItem = "Dashboard";

    public event Action? LogoutRequested;

    public MainViewModel(NavigationService navigation, AuthenticationService authService)
    {
        _navigation = navigation;
        _authService = authService;
        _currentUser = authService.CurrentUser;

        _navigation.CurrentViewChanged += vm =>
        {
            CurrentView = vm;
            _authService.RecordActivity();
        };
    }

    public override async Task LoadAsync()
    {
        await _navigation.NavigateToAsync<DashboardViewModel>();
    }

    [RelayCommand]
    private async Task NavigateTo(string destination)
    {
        SelectedMenuItem = destination;
        CurrentViewTitle = destination;

        switch (destination)
        {
            case "Dashboard":
                await _navigation.NavigateToAsync<DashboardViewModel>();
                break;
            case "Products":
                await _navigation.NavigateToAsync<ProductListViewModel>();
                break;
            case "Suppliers":
                await _navigation.NavigateToAsync<SupplierListViewModel>();
                break;
            case "Customers":
                await _navigation.NavigateToAsync<CustomerListViewModel>();
                break;
            default:
                Logger.Warning("Unknown navigation target: {Destination}", destination);
                break;
        }
    }

    [RelayCommand]
    private void ToggleTheme()
    {
        IsDarkTheme = !IsDarkTheme;
        ApplyTheme();
    }

    public void ApplyTheme()
    {
        var paletteHelper = new PaletteHelper();
        var theme = paletteHelper.GetTheme();
        theme.SetBaseTheme(IsDarkTheme ? BaseTheme.Dark : BaseTheme.Light);
        paletteHelper.SetTheme(theme);
    }

    [RelayCommand]
    private async Task Logout()
    {
        await _authService.LogoutAsync();
        LogoutRequested?.Invoke();
    }
}

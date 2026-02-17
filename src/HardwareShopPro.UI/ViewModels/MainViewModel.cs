using System.Windows;
using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
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
    [ObservableProperty] private bool _isDarkTheme = false; // Default to light
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

        // Listen for theme changes from Settings
        WeakReferenceMessenger.Default.Register<ThemeChangedMessage>(this, (r, m) =>
        {
            IsDarkTheme = m.IsDarkTheme;
            ApplyTheme();
        });
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
                CurrentViewTitle = "Inventory";
                await _navigation.NavigateToAsync<ProductListViewModel>();
                break;
            case "Suppliers":
                await _navigation.NavigateToAsync<SupplierListViewModel>();
                break;
            case "Customers":
                await _navigation.NavigateToAsync<CustomerListViewModel>();
                break;
            case "Billing":
                CurrentViewTitle = "Billing / POS";
                await _navigation.NavigateToAsync<BillingViewModel>();
                break;
            case "Reports":
                await _navigation.NavigateToAsync<ReportsViewModel>();
                break;
            case "Settings":
                await _navigation.NavigateToAsync<SettingsViewModel>();
                break;
            case "AIAssistant":
                CurrentViewTitle = "AI Assistant";
                await _navigation.NavigateToAsync<AIAssistantViewModel>();
                break;
            case "Help":
                await _navigation.NavigateToAsync<HelpViewModel>();
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

        // Apply custom ShopPro theme brushes
        var resources = Application.Current.Resources;

        if (IsDarkTheme)
        {
            resources["BackgroundBrush"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#0F1117"));
            resources["SurfaceBrush"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1A1D27"));
            resources["Surface2Brush"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#22263A"));
            resources["BorderBrush"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2C3050"));
            resources["TextPrimaryBrush"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F1F3F5"));
            resources["TextSecondaryBrush"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#909BBD"));
            resources["SidebarBgBrush"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1A1D27"));
            resources["SidebarActiveBrush"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2A3159"));
        }
        else
        {
            resources["BackgroundBrush"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F8F9FA"));
            resources["SurfaceBrush"] = new SolidColorBrush(Colors.White);
            resources["Surface2Brush"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F1F3F5"));
            resources["BorderBrush"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E9ECEF"));
            resources["TextPrimaryBrush"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#212529"));
            resources["TextSecondaryBrush"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#868E96"));
            resources["SidebarBgBrush"] = new SolidColorBrush(Colors.White);
            resources["SidebarActiveBrush"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#EEF2FF"));
        }
    }

    [RelayCommand]
    private async Task Logout()
    {
        await _authService.LogoutAsync();
        LogoutRequested?.Invoke();
    }
}

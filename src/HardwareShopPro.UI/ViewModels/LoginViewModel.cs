using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HardwareShopPro.Core.Models;
using HardwareShopPro.Core.Services;
using Serilog;

namespace HardwareShopPro.UI.ViewModels;

/// <summary>
/// ViewModel for the login window.
/// </summary>
public partial class LoginViewModel : ViewModelBase
{
    private readonly AuthenticationService _authService;
    private static readonly ILogger Logger = Log.ForContext<LoginViewModel>();

    [ObservableProperty] private string _username = string.Empty;
    [ObservableProperty] private string _password = string.Empty;
    [ObservableProperty] private string? _loginError;
    [ObservableProperty] private bool _isLoggingIn;

    public event Action<User>? LoginSucceeded;

    public LoginViewModel(AuthenticationService authService)
    {
        _authService = authService;
    }

    [RelayCommand]
    private async Task LoginAsync()
    {
        LoginError = null;
        IsLoggingIn = true;

        try
        {
            var user = await _authService.LoginAsync(Username, Password);
            if (user != null)
            {
                Logger.Information("Login successful for {Username}", user.Username);
                LoginSucceeded?.Invoke(user);
            }
            else
            {
                LoginError = "Invalid username or password.";
            }
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Login failed unexpectedly");
            LoginError = "An unexpected error occurred. Please try again.";
        }
        finally
        {
            IsLoggingIn = false;
        }
    }
}

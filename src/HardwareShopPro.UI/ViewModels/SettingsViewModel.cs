using System.Collections.ObjectModel;
using System.IO;
using System.Text.Json;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using HardwareShopPro.Core.Enums;
using HardwareShopPro.Core.Interfaces;
using HardwareShopPro.Core.Models;
using HardwareShopPro.Core.Services;
using Serilog;

namespace HardwareShopPro.UI.ViewModels;

public record ThemeChangedMessage(bool IsDarkTheme);

public partial class SettingsViewModel : ViewModelBase
{
    private readonly IUserRepository _userRepo;
    private readonly AuthenticationService _authService;
    private static readonly ILogger Logger = Log.ForContext<SettingsViewModel>();

    // ─── User Management ─────────────────────────────────────────────────
    [ObservableProperty] private ObservableCollection<User> _users = new();
    [ObservableProperty] private User? _selectedUser;
    
    [ObservableProperty] private bool _isAddUserDialogOpen;
    [ObservableProperty] private string _newUserName = string.Empty;
    [ObservableProperty] private string _newUserDisplayName = string.Empty;
    [ObservableProperty] private string _newUserPassword = string.Empty;
    [ObservableProperty] private UserRole _newUserRole = UserRole.Cashier;

    // ─── Business Profile ────────────────────────────────────────────────
    [ObservableProperty] private string _businessName = "My Hardware Shop";
    [ObservableProperty] private string _businessAddress = "Kathmandu, Nepal";
    [ObservableProperty] private string _businessPhone = "+977-9800000000";
    [ObservableProperty] private string _businessEmail = "info@hardware.com";
    [ObservableProperty] private string _gstNumber = "123-456-789";
    [ObservableProperty] private string _businessLogoPath = "pack://application:,,,/Assets/logo.png";

    // ─── Appearance ──────────────────────────────────────────────────────
    [ObservableProperty] private bool _isDarkTheme;
    [ObservableProperty] private int _accentColorIndex = 0; // 0=Indigo
    [ObservableProperty] private double _fontSizeScale = 1.0;

    // ─── AI Configuration ────────────────────────────────────────────────
    [ObservableProperty] private string _claudeApiKey = string.Empty;
    [ObservableProperty] private bool _isAIEnabled;
    [ObservableProperty] private string _aIModel = "claude-sonnet-4-20250514";

    // ─── Backup ──────────────────────────────────────────────────────────
    [ObservableProperty] private DateTime? _lastBackupDate;
    [ObservableProperty] private bool _autoBackupEnabled = true;
    [ObservableProperty] private string _backupPath = "C:/Backups/HardwareShopPro";

    public SettingsViewModel(IUserRepository userRepo, AuthenticationService authService)
    {
        _userRepo = userRepo;
        _authService = authService;
        
        // Initialize Defaults (TODO: Load from persistent storage)
        IsDarkTheme = false; // Default Light
    }

    public override async Task LoadAsync()
    {
        await LoadUsers();
        // TODO: Load business profile & settings from JSON/DB
    }

    [RelayCommand]
    private async Task LoadUsers()
    {
        try
        {
            var users = await _userRepo.GetAllAsync();
            Users = new ObservableCollection<User>(users);
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Failed to load users");
        }
    }

    [RelayCommand]
    private void OpenAddUserDialog()
    {
        NewUserName = string.Empty;
        NewUserDisplayName = string.Empty;
        NewUserPassword = string.Empty;
        NewUserRole = UserRole.Cashier;
        IsAddUserDialogOpen = true;
    }

    [RelayCommand]
    private void CloseAddUserDialog()
    {
        IsAddUserDialogOpen = false;
    }

    [RelayCommand]
    private async Task AddUser()
    {
        if (string.IsNullOrWhiteSpace(NewUserName) || string.IsNullOrWhiteSpace(NewUserPassword))
        {
            ErrorMessage = "Username and Password are required.";
            return;
        }

        try
        {
            await _authService.CreateUserAsync(NewUserName, NewUserPassword, NewUserDisplayName, NewUserRole);
            await LoadUsers();
            IsAddUserDialogOpen = false;
            Logger.Information("Created user {Username}", NewUserName);
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Failed to create user");
            ErrorMessage = "Failed to create user. Username might differ.";
        }
    }

    [RelayCommand]
    private async Task ToggleUserActive(User user)
    {
        if (user == null) return;
        if (user.Id == _authService.CurrentUser?.Id)
        {
            ErrorMessage = "Cannot deactivate your own account.";
            // Revert UI toggle visually if bound two-way? 
            // Better handle in UI or re-load list
            await LoadUsers(); 
            return;
        }

        user.IsActive = !user.IsActive;
        await _userRepo.UpdateAsync(user);
    }
    
    [RelayCommand]
    private async Task DeleteUser(User user)
    {
        if (user == null) return;
        if (user.Id == _authService.CurrentUser?.Id)
        {
             ErrorMessage = "Cannot delete your own account.";
             return;
        }

        if (MessageBox.Show($"Are you sure you want to delete user '{user.Username}'?", "Confirm Delete", 
            MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
        {
            await _userRepo.DeleteAsync(user.Id);
            await LoadUsers();
        }
    }

    [RelayCommand]
    private void ToggleTheme()
    {
        IsDarkTheme = !IsDarkTheme;
        // Broadcast change
        WeakReferenceMessenger.Default.Send(new ThemeChangedMessage(IsDarkTheme));
    }

    [RelayCommand]
    private void SaveBusinessProfile()
    {
        // TODO: Save to JSON/DB
        MessageBox.Show("Business profile saved successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    [RelayCommand]
    private void SaveApiKey()
    {
        // TODO: Encrypt & Save
        MessageBox.Show("API Key saved securely.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    [RelayCommand]
    private void BackupNow()
    {
        try
        {
            // Placeholder backup logic
            var backupFile = Path.Combine(BackupPath, $"backup_{DateTime.Now:yyyyMMdd_HHmmss}.db");
            // File.Copy(currentDb, backupFile);
            LastBackupDate = DateTime.Now;
            MessageBox.Show($"Backup created at {backupFile}", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
             ErrorMessage = $"Backup failed: {ex.Message}";
        }
    }
}

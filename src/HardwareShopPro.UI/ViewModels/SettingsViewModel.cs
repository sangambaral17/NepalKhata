using System.Collections.ObjectModel;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using HardwareShopPro.Core.Enums;
using HardwareShopPro.Core.Interfaces;
using HardwareShopPro.Core.Models;
using HardwareShopPro.Core.Services;
using HardwareShopPro.UI.Services;
using Microsoft.Win32;
using Serilog;

namespace HardwareShopPro.UI.ViewModels;

public record ThemeChangedMessage(bool IsDarkTheme, int AccentColorIndex);

public partial class SettingsViewModel : ViewModelBase
{
    private readonly IUserRepository _userRepo;
    private readonly AuthenticationService _authService;
    private readonly AppConfigService _configService;
    private readonly DatabasePathInfo _dbPathInfo;
    private static readonly ILogger Logger = Log.ForContext<SettingsViewModel>();

    // ─── User Management ─────────────────────────────────────────────────
    [ObservableProperty] private ObservableCollection<User> _users = new();
    [ObservableProperty] private User? _selectedUser;
    
    [ObservableProperty] private bool _isAddUserDialogOpen;
    [ObservableProperty] private string _newUserName = string.Empty;
    [ObservableProperty] private string _newUserDisplayName = string.Empty;
    [ObservableProperty] private string _newUserPassword = string.Empty;
    [ObservableProperty] private int _newUserRoleIndex = 0; // 0=Cashier, 1=Manager, 2=Admin

    [ObservableProperty] private bool _isResetPasswordDialogOpen;
    [ObservableProperty] private string _resetPasswordValue = string.Empty;
    [ObservableProperty] private User? _resetPasswordUser;

    // ─── Business Profile ────────────────────────────────────────────────
    [ObservableProperty] private string _businessName = "My Hardware Shop";
    [ObservableProperty] private string _businessAddress = "Kathmandu, Nepal";
    [ObservableProperty] private string _businessPhone = "+977-9800000000";
    [ObservableProperty] private string _businessEmail = "info@hardware.com";
    [ObservableProperty] private string _gstNumber = string.Empty;
    [ObservableProperty] private string _businessLogoPath = string.Empty;
    [ObservableProperty] private string _businessProfileStatus = string.Empty;

    // ─── Appearance ──────────────────────────────────────────────────────
    [ObservableProperty] private bool _isDarkTheme;
    [ObservableProperty] private int _accentColorIndex = 0;
    [ObservableProperty] private double _fontSizeScale = 1.0;

    // ─── AI Configuration ────────────────────────────────────────────────
    [ObservableProperty] private string _claudeApiKey = string.Empty;
    [ObservableProperty] private bool _isAIEnabled;
    [ObservableProperty] private string _aIModel = "claude-sonnet-4-20250514";
    [ObservableProperty] private string _aiConnectionStatus = string.Empty;
    [ObservableProperty] private bool _isAiConnected;
    [ObservableProperty] private bool _isTestingConnection;
    [ObservableProperty] private bool _showApiKey;

    // ─── Backup ──────────────────────────────────────────────────────────
    [ObservableProperty] private DateTime? _lastBackupDate;
    [ObservableProperty] private bool _autoBackupEnabled = true;
    [ObservableProperty] private string _backupPath = string.Empty;
    [ObservableProperty] private string _backupStatus = string.Empty;

    // ─── Toast ───────────────────────────────────────────────────────────
    [ObservableProperty] private string _toastMessage = string.Empty;
    [ObservableProperty] private bool _isToastVisible;

    public SettingsViewModel(IUserRepository userRepo, AuthenticationService authService,
        AppConfigService configService, DatabasePathInfo dbPathInfo)
    {
        _userRepo = userRepo;
        _authService = authService;
        _configService = configService;
        _dbPathInfo = dbPathInfo;

        // Load saved settings
        LoadFromConfig();
    }

    private void LoadFromConfig()
    {
        var bp = _configService.BusinessProfile;
        BusinessName = bp.Name;
        BusinessAddress = bp.Address;
        BusinessPhone = bp.Phone;
        BusinessEmail = bp.Email;
        GstNumber = bp.GSTIN;
        BusinessLogoPath = bp.LogoPath;

        IsDarkTheme = _configService.IsDarkTheme;
        AccentColorIndex = _configService.AccentColorIndex;
        FontSizeScale = _configService.FontSizeScale > 0 ? _configService.FontSizeScale : 1.0;

        ClaudeApiKey = _configService.GetApiKey();
        IsAIEnabled = _configService.IsAIEnabled;
        AIModel = _configService.AIModel;

        BackupPath = _configService.BackupPath;
        AutoBackupEnabled = _configService.AutoBackupEnabled;
        LastBackupDate = _configService.LastBackupDate;
    }

    public override async Task LoadAsync()
    {
        await LoadUsers();
    }

    // ═══════════════════════════════════════════════════════════════════
    // USER MANAGEMENT
    // ═══════════════════════════════════════════════════════════════════

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
        NewUserRoleIndex = 0;
        ErrorMessage = null;
        IsAddUserDialogOpen = true;
    }

    [RelayCommand]
    private void CloseAddUserDialog()
    {
        IsAddUserDialogOpen = false;
        ErrorMessage = null;
    }

    [RelayCommand]
    private async Task AddUser()
    {
        // Validation
        if (string.IsNullOrWhiteSpace(NewUserName))
        {
            ErrorMessage = "Username is required.";
            return;
        }
        if (NewUserName.Length < 3)
        {
            ErrorMessage = "Username must be at least 3 characters.";
            return;
        }
        if (string.IsNullOrWhiteSpace(NewUserPassword))
        {
            ErrorMessage = "Password is required.";
            return;
        }
        if (NewUserPassword.Length < 6)
        {
            ErrorMessage = "Password must be at least 6 characters.";
            return;
        }
        if (string.IsNullOrWhiteSpace(NewUserDisplayName))
        {
            NewUserDisplayName = NewUserName;
        }

        var role = NewUserRoleIndex switch
        {
            0 => UserRole.Cashier,
            1 => UserRole.Manager,
            2 => UserRole.Admin,
            _ => UserRole.Cashier
        };

        try
        {
            // Check for duplicate username
            var existing = await _userRepo.GetByUsernameAsync(NewUserName);
            if (existing != null)
            {
                ErrorMessage = $"Username '{NewUserName}' already exists.";
                return;
            }

            await _authService.CreateUserAsync(NewUserName, NewUserPassword, NewUserDisplayName, role);
            await LoadUsers();
            IsAddUserDialogOpen = false;
            ErrorMessage = null;
            ShowToast($"User '{NewUserName}' created successfully!");
            Logger.Information("Created user {Username}", NewUserName);
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Failed to create user");
            ErrorMessage = $"Failed to create user: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task ToggleUserActive(User user)
    {
        if (user == null) return;
        if (user.Id == _authService.CurrentUser?.Id)
        {
            ShowToast("Cannot deactivate your own account.");
            await LoadUsers();
            return;
        }

        user.IsActive = !user.IsActive;
        await _userRepo.UpdateAsync(user);
        ShowToast(user.IsActive ? $"User '{user.Username}' activated." : $"User '{user.Username}' deactivated.");
    }

    [RelayCommand]
    private async Task DeleteUser(User user)
    {
        if (user == null) return;
        if (user.Id == _authService.CurrentUser?.Id)
        {
            ShowToast("Cannot delete your own account.");
            return;
        }

        if (MessageBox.Show($"Are you sure you want to delete user '{user.Username}'?", "Confirm Delete",
            MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
        {
            // Soft delete: deactivate
            user.IsActive = false;
            await _userRepo.UpdateAsync(user);
            await LoadUsers();
            ShowToast($"User '{user.Username}' has been deleted.");
        }
    }

    [RelayCommand]
    private void OpenResetPasswordDialog(User user)
    {
        if (user == null) return;
        ResetPasswordUser = user;
        ResetPasswordValue = string.Empty;
        IsResetPasswordDialogOpen = true;
    }

    [RelayCommand]
    private void CloseResetPasswordDialog()
    {
        IsResetPasswordDialogOpen = false;
        ErrorMessage = null;
    }

    [RelayCommand]
    private async Task ResetPassword()
    {
        if (ResetPasswordUser == null) return;
        if (string.IsNullOrWhiteSpace(ResetPasswordValue) || ResetPasswordValue.Length < 6)
        {
            ErrorMessage = "New password must be at least 6 characters.";
            return;
        }

        try
        {
            ResetPasswordUser.PasswordHash = BCrypt.Net.BCrypt.HashPassword(ResetPasswordValue, workFactor: 12);
            await _userRepo.UpdateAsync(ResetPasswordUser);
            IsResetPasswordDialogOpen = false;
            ErrorMessage = null;
            ShowToast($"Password reset for '{ResetPasswordUser.Username}'.");
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Failed to reset password");
            ErrorMessage = $"Failed: {ex.Message}";
        }
    }

    // ═══════════════════════════════════════════════════════════════════
    // BUSINESS PROFILE
    // ═══════════════════════════════════════════════════════════════════

    [RelayCommand]
    private void BrowseLogo()
    {
        var dialog = new OpenFileDialog
        {
            Filter = "Image Files|*.png;*.jpg;*.jpeg;*.bmp;*.gif",
            Title = "Select Business Logo"
        };

        if (dialog.ShowDialog() == true)
        {
            try
            {
                // Copy to app data folder
                var logoDir = Path.Combine(_configService.ConfigDirectory, "Logo");
                Directory.CreateDirectory(logoDir);
                var destPath = Path.Combine(logoDir, "business_logo" + Path.GetExtension(dialog.FileName));
                File.Copy(dialog.FileName, destPath, true);
                BusinessLogoPath = destPath;
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Failed to copy logo file");
                ShowToast($"Failed to set logo: {ex.Message}");
            }
        }
    }

    [RelayCommand]
    private void SaveBusinessProfile()
    {
        // Validation
        if (string.IsNullOrWhiteSpace(BusinessName))
        {
            BusinessProfileStatus = "❌ Business name is required.";
            return;
        }
        if (!string.IsNullOrWhiteSpace(BusinessEmail) && !Regex.IsMatch(BusinessEmail, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
        {
            BusinessProfileStatus = "❌ Invalid email format.";
            return;
        }
        if (!string.IsNullOrWhiteSpace(BusinessPhone) && !Regex.IsMatch(BusinessPhone, @"^[\+\-\d\s\(\)]{7,15}$"))
        {
            BusinessProfileStatus = "❌ Invalid phone format.";
            return;
        }

        _configService.BusinessProfile = new BusinessProfile
        {
            Name = BusinessName,
            Address = BusinessAddress,
            Phone = BusinessPhone,
            Email = BusinessEmail,
            GSTIN = GstNumber,
            LogoPath = BusinessLogoPath
        };

        BusinessProfileStatus = string.Empty;
        ShowToast("Business profile saved successfully!");
        Logger.Information("Business profile saved");
    }

    // ═══════════════════════════════════════════════════════════════════
    // APPEARANCE
    // ═══════════════════════════════════════════════════════════════════

    [RelayCommand]
    private void ToggleTheme()
    {
        IsDarkTheme = !IsDarkTheme;
        _configService.IsDarkTheme = IsDarkTheme;
        WeakReferenceMessenger.Default.Send(new ThemeChangedMessage(IsDarkTheme, AccentColorIndex));
    }

    [RelayCommand]
    private void SetAccentColor(string indexStr)
    {
        if (int.TryParse(indexStr, out var index))
        {
            AccentColorIndex = index;
            _configService.AccentColorIndex = index;
            WeakReferenceMessenger.Default.Send(new ThemeChangedMessage(IsDarkTheme, AccentColorIndex));
        }
    }

    [RelayCommand]
    private void SetFontSize(string scale)
    {
        if (double.TryParse(scale, System.Globalization.CultureInfo.InvariantCulture, out var s))
        {
            FontSizeScale = s;
            _configService.FontSizeScale = s;
            ApplyFontScale();
        }
    }

    private void ApplyFontScale()
    {
        var resources = Application.Current.Resources;
        resources["BodyFontSize"] = 14.0 * FontSizeScale;
        resources["H1FontSize"] = 32.0 * FontSizeScale;
        resources["H2FontSize"] = 24.0 * FontSizeScale;
        resources["H3FontSize"] = 18.0 * FontSizeScale;
        resources["LabelFontSize"] = 12.0 * FontSizeScale;
    }

    // ═══════════════════════════════════════════════════════════════════
    // AI CONFIGURATION
    // ═══════════════════════════════════════════════════════════════════

    [RelayCommand]
    private void ToggleShowApiKey()
    {
        ShowApiKey = !ShowApiKey;
    }

    [RelayCommand]
    private void SaveApiKey()
    {
        _configService.SetApiKey(ClaudeApiKey);
        _configService.IsAIEnabled = IsAIEnabled;
        _configService.AIModel = AIModel;
        ShowToast("AI configuration saved securely (DPAPI encrypted).");
    }

    [RelayCommand]
    private async Task TestAiConnection()
    {
        if (string.IsNullOrWhiteSpace(ClaudeApiKey))
        {
            AiConnectionStatus = "❌ Please enter an API key first.";
            IsAiConnected = false;
            return;
        }

        IsTestingConnection = true;
        AiConnectionStatus = "Testing connection...";
        try
        {
            using var client = new HttpClient();
            client.DefaultRequestHeaders.Add("x-api-key", ClaudeApiKey);
            client.DefaultRequestHeaders.Add("anthropic-version", "2023-06-01");
            var content = new StringContent(
                JsonSerializer.Serialize(new
                {
                    model = AIModel,
                    max_tokens = 10,
                    messages = new[] { new { role = "user", content = "Hello" } }
                }),
                System.Text.Encoding.UTF8, "application/json");
            var response = await client.PostAsync("https://api.anthropic.com/v1/messages", content);
            if (response.IsSuccessStatusCode)
            {
                AiConnectionStatus = "✅ Connected successfully!";
                IsAiConnected = true;
            }
            else
            {
                var body = await response.Content.ReadAsStringAsync();
                AiConnectionStatus = $"❌ API Error: {response.StatusCode}";
                IsAiConnected = false;
            }
        }
        catch (Exception ex)
        {
            AiConnectionStatus = $"❌ Connection failed: {ex.Message}";
            IsAiConnected = false;
        }
        finally
        {
            IsTestingConnection = false;
        }
    }

    // ═══════════════════════════════════════════════════════════════════
    // BACKUP & RESTORE
    // ═══════════════════════════════════════════════════════════════════

    [RelayCommand]
    private void BackupNow()
    {
        try
        {
            var backupDir = BackupPath;
            if (string.IsNullOrWhiteSpace(backupDir))
                backupDir = Path.Combine(_configService.ConfigDirectory, "Backups");
            
            Directory.CreateDirectory(backupDir);
            var backupFile = Path.Combine(backupDir, $"HardwareShopPro_backup_{DateTime.Now:yyyyMMdd_HHmmss}.db");
            
            var dbPath = _dbPathInfo.FullPath;
            if (File.Exists(dbPath))
            {
                File.Copy(dbPath, backupFile, true);
                LastBackupDate = DateTime.Now;
                _configService.LastBackupDate = LastBackupDate;
                _configService.BackupPath = backupDir;
                BackupPath = backupDir;
                BackupStatus = $"✅ Backup created: {Path.GetFileName(backupFile)}";
                ShowToast("Database backup created successfully!");
                Logger.Information("Backup created at {BackupFile}", backupFile);
            }
            else
            {
                BackupStatus = "❌ Database file not found!";
            }
        }
        catch (Exception ex)
        {
            BackupStatus = $"❌ Backup failed: {ex.Message}";
            Logger.Error(ex, "Backup failed");
        }
    }

    [RelayCommand]
    private void RestoreBackup()
    {
        var dialog = new OpenFileDialog
        {
            Filter = "Database Files|*.db",
            Title = "Select Backup to Restore",
            InitialDirectory = BackupPath
        };

        if (dialog.ShowDialog() == true)
        {
            var result = MessageBox.Show(
                "⚠ WARNING: This will replace ALL current data with the backup data.\n\n" +
                "The application will restart after restore.\n\n" +
                "Are you absolutely sure?",
                "Restore Database", MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    var dbPath = _dbPathInfo.FullPath;
                    File.Copy(dialog.FileName, dbPath, true);
                    ShowToast("Database restored! Application will restart...");
                    Logger.Information("Database restored from {BackupFile}", dialog.FileName);
                    
                    // Restart the application
                    System.Diagnostics.Process.Start(Environment.ProcessPath!);
                    Application.Current.Shutdown();
                }
                catch (Exception ex)
                {
                    BackupStatus = $"❌ Restore failed: {ex.Message}";
                    Logger.Error(ex, "Restore failed");
                }
            }
        }
    }

    [RelayCommand]
    private void ToggleAutoBackup()
    {
        _configService.AutoBackupEnabled = AutoBackupEnabled;
    }

    // ═══════════════════════════════════════════════════════════════════
    // HELPERS
    // ═══════════════════════════════════════════════════════════════════

    private async void ShowToast(string message)
    {
        ToastMessage = message;
        IsToastVisible = true;
        await Task.Delay(3000);
        IsToastVisible = false;
    }
}

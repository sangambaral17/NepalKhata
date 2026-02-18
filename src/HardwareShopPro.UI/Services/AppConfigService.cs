using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using HardwareShopPro.Core.Models;
using Serilog;

namespace HardwareShopPro.UI.Services;

/// <summary>
/// Persists application settings (business profile, theme, AI config, backup) to a JSON file.
/// Uses Windows DPAPI for API key encryption.
/// </summary>
public class AppConfigService
{
    private static readonly ILogger Logger = Log.ForContext<AppConfigService>();
    private readonly string _configDir;
    private readonly string _configFilePath;
    private AppConfig _config;

    public AppConfigService()
    {
        _configDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "HardwareShopPro");
        Directory.CreateDirectory(_configDir);
        _configFilePath = Path.Combine(_configDir, "settings.json");
        _config = Load();
    }

    public string ConfigDirectory => _configDir;

    // ─── Business Profile ────────────────────────────────────────────────
    public BusinessProfile BusinessProfile
    {
        get => _config.BusinessProfile;
        set { _config.BusinessProfile = value; Save(); }
    }

    // ─── Theme ───────────────────────────────────────────────────────────
    public bool IsDarkTheme
    {
        get => _config.IsDarkTheme;
        set { _config.IsDarkTheme = value; Save(); }
    }

    public int AccentColorIndex
    {
        get => _config.AccentColorIndex;
        set { _config.AccentColorIndex = value; Save(); }
    }

    public double FontSizeScale
    {
        get => _config.FontSizeScale;
        set { _config.FontSizeScale = value; Save(); }
    }

    // ─── AI ──────────────────────────────────────────────────────────────
    public string GetApiKey()
    {
        if (string.IsNullOrEmpty(_config.EncryptedApiKey))
            return string.Empty;
        try
        {
            var encryptedBytes = Convert.FromBase64String(_config.EncryptedApiKey);
            var decryptedBytes = ProtectedData.Unprotect(encryptedBytes, null, DataProtectionScope.CurrentUser);
            return Encoding.UTF8.GetString(decryptedBytes);
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Failed to decrypt API key");
            return string.Empty;
        }
    }

    public void SetApiKey(string apiKey)
    {
        if (string.IsNullOrEmpty(apiKey))
        {
            _config.EncryptedApiKey = string.Empty;
        }
        else
        {
            var plainBytes = Encoding.UTF8.GetBytes(apiKey);
            var encryptedBytes = ProtectedData.Protect(plainBytes, null, DataProtectionScope.CurrentUser);
            _config.EncryptedApiKey = Convert.ToBase64String(encryptedBytes);
        }
        Save();
    }

    public bool IsAIEnabled
    {
        get => _config.IsAIEnabled;
        set { _config.IsAIEnabled = value; Save(); }
    }

    public string AIModel
    {
        get => _config.AIModel;
        set { _config.AIModel = value; Save(); }
    }

    // ─── Backup ──────────────────────────────────────────────────────────
    public string BackupPath
    {
        get => string.IsNullOrEmpty(_config.BackupPath)
            ? Path.Combine(_configDir, "Backups")
            : _config.BackupPath;
        set { _config.BackupPath = value; Save(); }
    }

    public bool AutoBackupEnabled
    {
        get => _config.AutoBackupEnabled;
        set { _config.AutoBackupEnabled = value; Save(); }
    }

    public DateTime? LastBackupDate
    {
        get => _config.LastBackupDate;
        set { _config.LastBackupDate = value; Save(); }
    }

    // ─── Persistence ─────────────────────────────────────────────────────
    private AppConfig Load()
    {
        try
        {
            if (File.Exists(_configFilePath))
            {
                var json = File.ReadAllText(_configFilePath);
                return JsonSerializer.Deserialize<AppConfig>(json) ?? new AppConfig();
            }
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Failed to load config from {Path}", _configFilePath);
        }
        return new AppConfig();
    }

    public void Save()
    {
        try
        {
            var json = JsonSerializer.Serialize(_config, new JsonSerializerOptions
            {
                WriteIndented = true
            });
            File.WriteAllText(_configFilePath, json);
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Failed to save config to {Path}", _configFilePath);
        }
    }

    public void Reload()
    {
        _config = Load();
    }
}

/// <summary>
/// Internal config model for JSON serialization.
/// </summary>
public class AppConfig
{
    public BusinessProfile BusinessProfile { get; set; } = new();
    public bool IsDarkTheme { get; set; }
    public int AccentColorIndex { get; set; }
    public double FontSizeScale { get; set; } = 1.0;
    public string EncryptedApiKey { get; set; } = string.Empty;
    public bool IsAIEnabled { get; set; }
    public string AIModel { get; set; } = "claude-sonnet-4-20250514";
    public string BackupPath { get; set; } = string.Empty;
    public bool AutoBackupEnabled { get; set; } = true;
    public DateTime? LastBackupDate { get; set; }
}

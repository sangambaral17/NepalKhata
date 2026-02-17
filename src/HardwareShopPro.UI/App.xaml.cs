using System.IO;
using System.Windows;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using HardwareShopPro.AI.Services;
using HardwareShopPro.Core.Interfaces;
using HardwareShopPro.Core.Services;
using HardwareShopPro.Data;
using HardwareShopPro.Data.Database;
using HardwareShopPro.Data.Repositories;
using HardwareShopPro.UI.Services;
using HardwareShopPro.UI.ViewModels;
using HardwareShopPro.UI.Views;
using Serilog;

namespace HardwareShopPro.UI;

public partial class App : Application
{
    private ServiceProvider? _serviceProvider;

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // ─── Configuration ─────────────────────────────────────
        var config = new ConfigurationBuilder()
            .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
            .Build();

        // ─── Serilog ───────────────────────────────────────────
        var logPath = config["Logging:Path"] ?? "logs/app-.log";
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .WriteTo.File(logPath, rollingInterval: RollingInterval.Day, retainedFileCountLimit: 30)
            .CreateLogger();

        Log.Information("═══════════════════════════════════════════");
        Log.Information("HardwareShopPro starting up...");

        // ─── DI Container ──────────────────────────────────────
        var services = new ServiceCollection();

        // Database
        var dbPath = config["Database:Path"] ?? "HardwareShopPro.db";
        var fullDbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, dbPath);
        var dbContext = new DatabaseContext(fullDbPath);
        services.AddSingleton(dbContext);

        // Repositories
        services.AddSingleton<IProductRepository, ProductRepository>();
        services.AddSingleton<ISupplierRepository, SupplierRepository>();
        services.AddSingleton<ICustomerRepository, CustomerRepository>();
        services.AddSingleton<IInvoiceRepository, InvoiceRepository>();
        services.AddSingleton<IUserRepository, UserRepository>();
        services.AddSingleton<IAuditLogRepository, AuditLogRepository>();

        // AI Service
        var apiKey = config["AI:ApiKey"];
        var model = config["AI:Model"] ?? "claude-sonnet-4-20250514";
        var maxRetries = int.TryParse(config["AI:MaxRetries"], out var r) ? r : 3;
        services.AddSingleton<IAIService>(new ClaudeAIService(apiKey, model, maxRetries));

        // Auth
        services.AddSingleton<AuthenticationService>();

        // Navigation
        services.AddSingleton<NavigationService>(sp =>
        {
            return new NavigationService(type =>
            {
                return (ViewModelBase)sp.GetRequiredService(type);
            });
        });

        // ViewModels
        services.AddTransient<LoginViewModel>();
        services.AddTransient<MainViewModel>();
        services.AddTransient<DashboardViewModel>();
        services.AddTransient<ProductListViewModel>();
        services.AddTransient<SupplierListViewModel>();
        services.AddTransient<CustomerListViewModel>();

        _serviceProvider = services.BuildServiceProvider();

        // ─── Initialize Database ───────────────────────────────
        try
        {
            await dbContext.InitializeAsync();
            var seeder = new DatabaseSeeder(dbContext);
            await seeder.SeedAsync();
            Log.Information("Database ready.");
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Database initialization failed!");
            MessageBox.Show($"Database Error: {ex.Message}", "Fatal Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown(1);
            return;
        }

        // ─── Show Login Window ─────────────────────────────────
        ShowLoginWindow();
    }

    private void ShowLoginWindow()
    {
        var loginVm = _serviceProvider!.GetRequiredService<LoginViewModel>();
        var loginWindow = new LoginWindow { DataContext = loginVm };

        loginVm.LoginSucceeded += user =>
        {
            loginWindow.Hide();
            ShowMainWindow();
        };

        loginWindow.Show();
    }

    private async void ShowMainWindow()
    {
        var mainVm = _serviceProvider!.GetRequiredService<MainViewModel>();
        var mainWindow = new MainWindow { DataContext = mainVm };

        mainVm.LogoutRequested += () =>
        {
            mainWindow.Close();
            ShowLoginWindow();
        };

        mainVm.ApplyTheme();
        mainWindow.Show();
        await mainVm.LoadAsync();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        Log.Information("HardwareShopPro shutting down.");
        Log.CloseAndFlush();
        _serviceProvider?.Dispose();
        base.OnExit(e);
    }
}

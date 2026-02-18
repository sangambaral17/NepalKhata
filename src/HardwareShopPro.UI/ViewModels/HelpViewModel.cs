using System.Collections.ObjectModel;
using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace HardwareShopPro.UI.ViewModels;

public record ShortcutItem(string Keys, string Description, string Category);
public record FaqItem(string Question, string Answer);

public partial class HelpViewModel : ViewModelBase
{
    [ObservableProperty] private ObservableCollection<ShortcutItem> _shortcuts = new();
    [ObservableProperty] private ObservableCollection<FaqItem> _faqs = new();
    [ObservableProperty] private int _selectedSectionIndex = 0;
    [ObservableProperty] private string _searchQuery = string.Empty;
    [ObservableProperty] private string _appVersion = "2.0.0";

    public HelpViewModel()
    {
        PopulateShortcuts();
        PopulateFaqs();
    }

    private void PopulateShortcuts()
    {
        Shortcuts = new ObservableCollection<ShortcutItem>
        {
            // Navigation
            new("Ctrl + 1", "Go to Dashboard", "Navigation"),
            new("Ctrl + 2", "Go to Products", "Navigation"),
            new("Ctrl + 3", "Go to Suppliers", "Navigation"),
            new("Ctrl + 4", "Go to Customers", "Navigation"),
            new("Ctrl + 5", "Go to Billing", "Navigation"),
            new("Ctrl + 6", "Go to Reports", "Navigation"),
            new("Ctrl + 7", "Go to Settings", "Navigation"),

            // Billing
            new("F2", "Focus Product Search", "Billing"),
            new("F5", "Generate Invoice", "Billing"),
            new("F9", "Clear Cart", "Billing"),
            new("Ctrl + P", "Print Last Invoice", "Billing"),

            // General
            new("Ctrl + S", "Save Current Form", "General"),
            new("Ctrl + N", "New Record", "General"),
            new("Ctrl + F", "Search / Filter", "General"),
            new("F1", "Open Help", "General"),
            new("Ctrl + L", "Logout", "General"),
            new("Ctrl + B", "Toggle Sidebar", "General"),
            new("Esc", "Close Dialog / Cancel", "General"),
        };
    }

    private void PopulateFaqs()
    {
        Faqs = new ObservableCollection<FaqItem>
        {
            new("How do I add a new product?",
                "Navigate to Products from the sidebar. Click the 'Add Product' button in the top right. Fill in the product name, category, price, stock, and other details. Click Save."),
            new("How do I generate an invoice?",
                "Go to Billing/POS. Search for products and add them to the cart. Select or add a customer. Choose the payment method. Click 'Generate Invoice'. The invoice will be saved and a PDF can be generated."),
            new("How do I backup my data?",
                "Go to Settings → Backup & Data. Click 'Backup Now' to create an instant backup of your database. You can also enable Auto Backup for daily backups. To restore, click 'Restore from Backup' and select a backup file."),
            new("How do I change the app theme?",
                "Go to Settings → Appearance. Toggle Dark Mode on/off and select an accent color from the color swatches. Changes apply instantly."),
            new("How do I add a new user?",
                "Go to Settings → User Management. Click 'Add User'. Enter a username, display name, password, and select a role (Cashier, Manager, or Admin). Only Admin users can manage other users."),
            new("How do I view reports?",
                "Navigate to Reports from the sidebar. Select a report type (Sales, Inventory, etc.), choose a date range, and click 'Generate Report'. You can export reports as PDF or Excel."),
            new("How do I set up AI features?",
                "Go to Settings → AI Configuration. Enter your Claude API key, click 'Save Key', then 'Test Connection' to verify. Enable AI features with the checkbox."),
            new("Can I track customer purchase history?",
                "Yes! Go to Customers, click on any customer to view their details and complete purchase history."),
            new("What is GST/PAN number used for?",
                "The GST/PAN number appears on your invoices for tax compliance. Set it in Settings → Business Profile."),
            new("How do I handle returns or refunds?",
                "Currently, you can manually adjust stock levels in the Products section and create a credit note. Full return management is planned for a future update."),
        };
    }

    [RelayCommand]
    private void OpenLink(string url)
    {
        try
        {
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
        }
        catch { /* silently fail */ }
    }
}

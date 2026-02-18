using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HardwareShopPro.Core.Interfaces;
using HardwareShopPro.UI.Models;
using Serilog;

namespace HardwareShopPro.UI.ViewModels;

public partial class AIAssistantViewModel : ViewModelBase
{
    private readonly IAIService _aiService;
    private readonly IProductRepository _productRepo;
    private readonly IInvoiceRepository _invoiceRepo;
    private static readonly ILogger Logger = Log.ForContext<AIAssistantViewModel>();

    [ObservableProperty] private ObservableCollection<ChatMessage> _messages = new();
    [ObservableProperty] private string _currentInput = string.Empty;
    [ObservableProperty] private bool _isTyping;
    [ObservableProperty] private bool _isConnected;
    [ObservableProperty] private bool _hasMessages;

    // Suggestion chips shown when chat is empty
    public List<string> Suggestions { get; } = new()
    {
        "📦 What products are running low on stock?",
        "💰 What were my top selling items this week?",
        "📊 Summarize today's sales performance",
        "🔍 Find all RAM products under Rs. 5000",
        "📈 Which customers bought the most this month?",
        "🔔 Show me reorder alerts"
    };

    public AIAssistantViewModel(IAIService aiService, IProductRepository productRepo, IInvoiceRepository invoiceRepo)
    {
        _aiService = aiService;
        _productRepo = productRepo;
        _invoiceRepo = invoiceRepo;

        Messages.CollectionChanged += (s, e) => HasMessages = Messages.Count > 0;
    }

    public override async Task LoadAsync()
    {
        try
        {
            IsConnected = await _aiService.IsAvailableAsync();
        }
        catch
        {
            IsConnected = false;
        }

        if (!IsConnected)
        {
            Messages.Add(new ChatMessage("assistant",
                "⚠️ AI Service is currently offline. Please configure your Claude API key in Settings → AI Configuration.\n\nYou can still use the app fully — AI features are optional.",
                IsError: true));
        }
        else
        {
            Messages.Add(new ChatMessage("assistant",
                "👋 Hello! I'm your AI Assistant powered by Claude.\n\nI can help you analyze sales, find products, track inventory, and answer questions about your business. What would you like to know?"));
        }
    }

    [RelayCommand]
    private async Task SendMessage()
    {
        if (string.IsNullOrWhiteSpace(CurrentInput)) return;

        var userMessage = CurrentInput.Trim();
        CurrentInput = string.Empty;

        Messages.Add(new ChatMessage("user", userMessage));
        IsTyping = true;

        try
        {
            string response = await GenerateResponseAsync(userMessage);
            Messages.Add(new ChatMessage("assistant", response));
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Error sending message to AI");
            Messages.Add(new ChatMessage("assistant",
                "❌ Sorry, I encountered an error processing your request. Please try again.",
                IsError: true));
        }
        finally
        {
            IsTyping = false;
        }
    }

    private async Task<string> GenerateResponseAsync(string userMessage)
    {
        var msg = userMessage.ToLowerInvariant();

        if (msg.Contains("low stock") || msg.Contains("running low") || msg.Contains("reorder"))
        {
            try
            {
                var lowStockProducts = await _productRepo.GetLowStockAsync();
                string inventoryJson = System.Text.Json.JsonSerializer.Serialize(lowStockProducts);
                
                var alertsResponse = await _aiService.GetReorderAlertsAsync(inventoryJson);
                
                if (!string.IsNullOrEmpty(alertsResponse))
                {
                    return alertsResponse;
                }
                return "✅ Great news! All products are currently well-stocked. No reorder alerts at this time.";
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Error fetching reorder alerts");
                return "📦 I couldn't fetch stock data right now. Please check the Products section for current inventory levels.";
            }
        }

        // ─── Product search ──────────────────────────────────────────────
        if (msg.Contains("find") || msg.Contains("search") || msg.Contains("product") || msg.Contains("under") || msg.Contains("price"))
        {
            try
            {
                var criteria = await _aiService.SmartSearchAsync(userMessage);
                if (criteria != null)
                {
                    var parts = new List<string>();
                    if (!string.IsNullOrEmpty(criteria.Brand)) parts.Add($"Brand: {criteria.Brand}");
                    if (!string.IsNullOrEmpty(criteria.Category)) parts.Add($"Category: {criteria.Category}");
                    if (criteria.MaxPrice > 0) parts.Add($"Max Price: Rs. {criteria.MaxPrice:N0}");

                    var products = await _productRepo.SearchAsync(criteria.Brand ?? criteria.Category ?? "");
                    var filtered = products.Where(p => criteria.MaxPrice <= 0 || p.SellingPrice <= criteria.MaxPrice).Take(5).ToList();

                    if (filtered.Any())
                    {
                        var lines = filtered.Select(p => $"• **{p.Name}** — Rs. {p.SellingPrice:N0} (Stock: {p.Stock})");
                        return $"🔍 Found {filtered.Count} products matching your criteria ({string.Join(", ", parts)}):\n\n{string.Join("\n", lines)}\n\nGo to **Products** tab to see the full list.";
                    }
                    return $"🔍 No products found matching: {string.Join(", ", parts)}. Try different search terms.";
                }
            }
            catch { }
            return "🔍 I can help you find products! Try: \"Find RAM under Rs. 5000\" or \"Search for Intel processors\".";
        }

        // ─── Sales queries ───────────────────────────────────────────────
        if (msg.Contains("sales") || msg.Contains("revenue") || msg.Contains("invoice") || msg.Contains("top selling"))
        {
            return "📊 For detailed sales analysis, check the **Reports** tab which shows:\n• Daily/weekly/monthly revenue\n• Top selling products\n• Customer purchase history\n• Profit margins\n\nWould you like me to explain any specific metric?";
        }

        // ─── Customer queries ────────────────────────────────────────────
        if (msg.Contains("customer") || msg.Contains("client") || msg.Contains("buyer"))
        {
            return "👥 Customer information is available in the **Customers** tab. You can:\n• View purchase history\n• Track outstanding balances\n• See top customers by revenue\n\nFor AI-powered customer insights, make sure your Claude API key is configured in Settings.";
        }

        // ─── Help / How-to queries ───────────────────────────────────────
        if (msg.Contains("how") || msg.Contains("help") || msg.Contains("guide") || msg.Contains("tutorial"))
        {
            return "📚 Here's a quick guide to HardwareShopPro:\n\n• **Dashboard** — Overview of sales, stock alerts, and key metrics\n• **Billing** — Create invoices, search products, process payments\n• **Products** — Manage inventory, prices, and stock levels\n• **Customers** — Track customer details and purchase history\n• **Reports** — Analyze sales performance and trends\n• **Settings** — Configure business profile, users, and AI\n\nCheck the **Help** tab for keyboard shortcuts and FAQs!";
        }

        // ─── Greeting ────────────────────────────────────────────────────
        if (msg.Contains("hello") || msg.Contains("hi") || msg.Contains("hey") || msg.Contains("namaste"))
        {
            return "👋 Namaste! I'm your AI business assistant. I can help you:\n\n• 📦 Check stock levels and reorder alerts\n• 🔍 Find products by name, brand, or price range\n• 📊 Understand your sales performance\n• 💡 Get business insights and recommendations\n\nWhat would you like to know today?";
        }

        // ─── Default response ────────────────────────────────────────────
        return "🤔 I understand you're asking about: \"" + userMessage + "\"\n\nI can help with:\n• **Stock alerts** — \"Show low stock items\"\n• **Product search** — \"Find RAM under Rs. 5000\"\n• **Sales info** — \"How are my sales this week?\"\n• **Customer data** — \"Who are my top customers?\"\n\nTry one of the suggestion chips below or ask a specific question!";
    }

    [RelayCommand]
    private void UseSuggestion(string suggestion)
    {
        // Strip emoji prefix for cleaner query
        CurrentInput = suggestion;
        SendMessageCommand.Execute(null);
    }

    [RelayCommand]
    private void ClearConversation()
    {
        Messages.Clear();
        HasMessages = false;
    }
}

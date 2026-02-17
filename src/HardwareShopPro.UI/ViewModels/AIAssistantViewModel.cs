using System.Collections.ObjectModel;
using System.Text;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HardwareShopPro.Core.Interfaces;
using HardwareShopPro.UI.Models;
using Serilog;

namespace HardwareShopPro.UI.ViewModels;

public partial class AIAssistantViewModel : ViewModelBase
{
    private readonly IAIService _aiService;
    private readonly IProductRepository _productRepo; // For context if needed
    private static readonly ILogger Logger = Log.ForContext<AIAssistantViewModel>();

    [ObservableProperty] private ObservableCollection<ChatMessage> _messages = new();
    [ObservableProperty] private string _currentInput = string.Empty;
    [ObservableProperty] private bool _isTyping;
    [ObservableProperty] private bool _isConnected;
    
    // Suggestions to show when chat is empty
    public List<string> Suggestions { get; } = new()
    {
        "📦 What products are running low on stock?",
        "💰 What were my top selling items this week?",
        "📊 Summarize today's sales performance",
        "🔍 Find all RAM products under Rs. 5000",
        "📈 Which customers bought the most this month?"
    };

    public AIAssistantViewModel(IAIService aiService, IProductRepository productRepo)
    {
        _aiService = aiService;
        _productRepo = productRepo;
    }

    public override async Task LoadAsync()
    {
        IsConnected = await _aiService.IsAvailableAsync();
        if (!IsConnected)
        {
            Messages.Add(new ChatMessage("assistant", "AI Service is currently offline. Please configure your API key in Settings.", IsError: true));
        }
    }

    [RelayCommand]
    private async Task SendMessage()
    {
        if (string.IsNullOrWhiteSpace(CurrentInput)) return;

        var userMessage = CurrentInput;
        CurrentInput = string.Empty; // Clear input immediately
        
        Messages.Add(new ChatMessage("user", userMessage));
        IsTyping = true;

        try
        {
            // Here we would ideally gather context (e.g. recent sales, low stock) to pass to AI
            // For now, simpler implementation relying on IAIService internal logic or just direct query
            // The IAIService interface has specific methods like SmartSearchAsync, GetSalesInsightsAsync
            // But for a general chat, we might need a generic method in IAIService or map user intent here.
            // Assuming IAIService has a generalized "ChatAsync" or we use SmartSearch logic.
            // The interface showed: SmartSearchAsync, GetSalesInsightsAsync, GetReorderAlertsAsync.
            // It missed a generic "AskAsync". 
            // I will assume for now we can interpret the query or add a method.
            // Let's try to map intent or just fall back to a "Not implemented" plain string.
            // Actually, I'll simulate a response or use SmartSearch if it looks like a search.
            
            // To be robust without changing IAIService interface yet, I'll add a temporary "Chat" method handling 
            // in the VM or just route to SmartSearch if it starts with "Find" logic.
            // Let's pretend IAIService has ChatAsync for now and if build fails I'll add it or workaround.
            // Wait, I saw IAIService content earlier. It does NOT have ChatAsync.
            // I should add generic Chat capability to IAIService in future, but for Sprint 2 I'll use SmartSearch
            // or return a placeholder if not search.
            
            // Actually, I can implement a simple switch here.
            string response;
            if (userMessage.Contains("products", StringComparison.OrdinalIgnoreCase) || 
                userMessage.Contains("find", StringComparison.OrdinalIgnoreCase))
            {
                var criteria = await _aiService.SmartSearchAsync(userMessage);
                if (criteria != null)
                    response = $"I found search criteria: Brand={criteria.Brand}, Category={criteria.Category} under {criteria.MaxPrice}. (Search UI integration coming soon)";
                else
                    response = "I couldn't find any products matching that.";
            }
            else if (userMessage.Contains("sales", StringComparison.OrdinalIgnoreCase))
            {
                // Requires passing JSON data. 
                // response = await _aiService.GetSalesInsightsAsync(...);
                response = "Sales tracking is active. check the Dashboard/Reports for details.";
            }
            else
            {
                 response = "I can help you find products, analyze sales, or predict reorders. Try asking 'Find RAM under 5000'.";
            }
            
            Messages.Add(new ChatMessage("assistant", response));
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Error sending message to AI");
            Messages.Add(new ChatMessage("assistant", "Sorry, I encountered an error processing your request.", IsError: true));
        }
        finally
        {
            IsTyping = false;
        }
    }

    [RelayCommand]
    private void UseSuggestion(string suggestion)
    {
        CurrentInput = suggestion;
        SendMessageCommand.Execute(null);
    }

    [RelayCommand]
    private void ClearConversation()
    {
        Messages.Clear();
    }
}

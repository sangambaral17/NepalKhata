namespace HardwareShopPro.UI.Models;

public record ChatMessage(string Role, string Content, bool IsError = false)
{
    public DateTime Timestamp { get; } = DateTime.Now;
    
    // UI Helpers
    public bool IsUser => Role == "user";
    public bool IsAssistant => Role == "assistant";
}

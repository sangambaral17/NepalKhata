namespace HardwareShopPro.Core.Models;

/// <summary>
/// Represents the business/store profile for invoices and documents.
/// </summary>
public class BusinessProfile
{
    public string Name { get; set; } = "My Hardware Shop";
    public string Address { get; set; } = "Kathmandu, Nepal";
    public string Phone { get; set; } = "+977-9800000000";
    public string Email { get; set; } = "info@hardware.com";
    public string GSTIN { get; set; } = string.Empty;
    public string LogoPath { get; set; } = string.Empty;
}

namespace HardwareShopPro.Core.Enums;

/// <summary>
/// Payment status for invoices.
/// </summary>
public enum PaymentStatus
{
    Unpaid = 0,
    Partial = 1,
    Paid = 2
}

/// <summary>
/// Payment mode used for transactions.
/// </summary>
public enum PaymentMode
{
    Cash = 0,
    Card = 1,
    UPI = 2,
    BankTransfer = 3,
    Credit = 4
}

/// <summary>
/// User roles for role-based access control.
/// </summary>
public enum UserRole
{
    Cashier = 0,
    Manager = 1,
    Admin = 2
}

# Database Schema

NepalKhata uses **SQLite** as its local database engine. The data access is handled via **Dapper** with **WAL (Write-Ahead Logging)** mode enabled for optimal performance during concurrent read/write operations.

## Enums (Mapping)

- **UserRole:** 0 = Cashier, 1 = Manager, 2 = Admin
- **PaymentStatus:** 0 = Pending, 1 = Paid, 2 = PartiallyPaid, 3 = Cancelled
- **PaymentMode:** 0 = Cash, 1 = Card, 2 = UPI, 3 = Credit

## Tables

### Users
Stores application user credentials and roles.
- `Id`: INTEGER (PK)
- `Username`: TEXT (Unique)
- `PasswordHash`: TEXT
- `DisplayName`: TEXT
- `Role`: INTEGER
- `IsActive`: INTEGER (Boolean)
- `LastLogin`: DATETIME

### Products
Core inventory items.
- `Id`: INTEGER (PK)
- `Name`: TEXT
- `Category`: TEXT
- `Brand`: TEXT
- `SKU`: TEXT (Unique)
- `PurchasePrice`: DECIMAL
- `SellingPrice`: DECIMAL
- `Stock`: INTEGER
- `MinStockLevel`: INTEGER
- `SupplierId`: INTEGER (FK)
- `LastRestockDate`: DATETIME

### Suppliers
- `Id`: INTEGER (PK)
- `Name`: TEXT
- `Contact`: TEXT
- `Email`: TEXT
- `Address`: TEXT
- `GSTIN`: TEXT

### Customers
- `Id`: INTEGER (PK)
- `Name`: TEXT
- `Phone`: TEXT
- `Email`: TEXT
- `Address`: TEXT
- `GSTIN`: TEXT

### Invoices
- `Id`: INTEGER (PK)
- `InvoiceNumber`: TEXT (Unique)
- `CustomerId`: INTEGER (FK)
- `Date`: DATETIME
- `TotalAmount`: DECIMAL
- `TaxAmount`: DECIMAL
- `DiscountAmount`: DECIMAL
- `PaymentStatus`: INTEGER
- `PaymentMode`: INTEGER

### InvoiceItems
- `Id`: INTEGER (PK)
- `InvoiceId`: INTEGER (FK)
- `ProductId`: INTEGER (FK)
- `Quantity`: INTEGER
- `Price`: DECIMAL
- `Discount`: DECIMAL

### AuditLog
Tracks all significant user actions for security.
- `Id`: INTEGER (PK)
- `Timestamp`: DATETIME
- `UserId`: INTEGER (FK)
- `Action`: TEXT
- `EntityName`: TEXT
- `EntityId`: TEXT
- `Details`: TEXT

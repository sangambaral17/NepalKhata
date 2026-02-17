# Billing & POS System Documentation

## Overview
The Billing/POS module is the core transaction interface for HardwareShopPro. It allows cashiers to process sales, manage customer carts, and generate invoices with automatic stock deduction.

## Features implemented
- **Product Search**: Real-time search by name, SKU, or barcode (simulated via text input).
- **Cart Management**: Add items, adjust quantities, apply line-item discounts, and remove items.
- **Stock Validation**: Prevents adding more items than available in stock.
- **Customer Selection**: Search and link existing customers or process as "Walk-in".
- **Dynamic Totals**: Real-time calculation of Subtotal, Discount, Tax (13% VAT), and Grand Total.
- **Payment Modes**: Support for Cash, Card, UPI, and Credit payments.
- **Change Calculator**: Automatically calculates change to return for cash payments.
- **Invoice Generation**: Atomic transaction that:
  1. Creates Invoice record.
  2. Creates InvoiceItem records.
  3. Decrements Product stock.
  4. Updates Audit Log.

## Technical Details
### ViewModel: `BillingViewModel`
- **Dependencies**: `IProductRepository`, `IInvoiceRepository`, `ICustomerRepository`.
- **State**: Manages `CartItems` (ObservableCollection), `SelectedCustomer`, and payment state.
- **Commands**: `SearchProducts`, `AddToCart`, `GenerateInvoice`, `ClearCart`.

### Database Transactions
Invoice generation uses atomic SQL transactions to ensure data integrity. If stock deduction fails, the entire invoice creation is rolled back.

### Key Shortcuts
- **F2**: Focus Search (Product)
- **F9**: Generate Invoice / Checkout
- **Esc**: Clear Cart

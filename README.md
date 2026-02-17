# NepalKhata (HardwareShopPro)

**Copyright (c) 2026 Walsong Group**

## Overview

NepalKhata is a modern, AI-powered Hardware Shop Management System built for the desktop. It streamlines inventory tracking, invoicing, customer management, and supplier relations with a premium, user-friendly interface.

## Tech Stack

*   **Framework:** .NET 8 (WPF)
*   **Database:** SQLite with Dapper (WAL mode enabled)
*   **Architecture:** MVVM (CommunityToolkit.Mvvm)
*   **UI Library:** MaterialDesignInXaml
*   **AI Integration:** Anthropic Claude API (Smart Search, Insights)
*   **Logging:** Serilog

## Key Features

*   **Dashboard:** Real-time statistics, low stock alerts, and sales charts.
*   **Inventory Management:** Product tracking, category/brand organization, and barcode support.
*   **Smart Search:** AI-powered search to find products by vague descriptions or usage.
*   **Invoicing:** Fast, transactional billing with auto-stock deduction and GST calculation.
*   **User Management:** Role-based access (Admin, Manager, Cashier) with secure BCrypt authentication.
*   **Offline Capable:** Core features work without internet; AI features degrade gracefully.

## Getting Started

1.  Clone the repository:
    ```bash
    git clone https://github.com/sangambaral17/NepalKhata.git
    ```
2.  Open `HardwareShopPro.sln` in Visual Studio 2022 or Rider.
3.  Restore NuGet packages:
    ```bash
    dotnet restore
    ```
4.  Configure `appsettings.json` (copy from `appsettings.template.json` and add your API key).
5.  Build and Run.

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.
**Copyright (c) 2026 Walsong Group.**

# Security Implementation

NepalKhata is designed with security as a priority, ensuring that business data is protected from unauthorized access and tampering.

## 1. Authentication
- **BCrypt Hashing:** User passwords are never stored in plain text. We use the **BCrypt.Net-Next** library with a work factor of 12 to generate cryptographically secure hashes.
- **Session Management:** The `AuthenticationService` manages the lifecycle of the `CurrentUser`. It includes role checks and activity recording.
- **Inactivity Timeout:** (Planned) The system will automatically log out users after a configurable period of inactivity.

## 2. Role-Based Access Control (RBAC)
The application enforces different permission levels based on the user's role:
- **Admin:** Full access to all features, including user management and system settings.
- **Manager:** Access to inventory, reports, and sales, but limited system configuration.
- **Cashier:** Restricted to sales, invoicing, and basic product lookups.

## 3. Audit Logging
Every critical action in the system is recorded in the `AuditLog` table. This provides a clear trail of "Who did what and when."
Captured events include:
- Successful and failed logins.
- Product creation, modification, and deletion.
- Invoice generation and payment status changes.
- Supplier/Customer record updates.

## 4. Data Security
- **Parameterized Queries:** All database interactions use Dapper's parameterized queries to completely eliminate the risk of **SQL Injection**.
- **WAL Mode:** SQLite WAL mode ensures database integrity even in the event of an application crash.
- **API Key Protection:** (In Progress) Anthropic API keys will be encrypted using Windows **DPAPI** (Data Protection API) to ensure they cannot be stolen from the configuration files.

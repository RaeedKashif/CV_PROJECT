# Cheque Printing & Management System

An ASP.NET Core MVC application for managing banks, bank accounts, payees, cheque issuance, cheque printing, reporting, and bulk import/export. The solution follows a layered Clean Architecture approach and uses SQL Server, Entity Framework Core, and ASP.NET Identity.

## Overview

This project is designed for organizations that need to:

- maintain bank and bank account masters
- register payees
- issue and track cheques through their lifecycle
- configure cheque print coordinates per bank account
- print cheques using position-based layout data
- import cheque data from Excel
- export filtered reports to Excel and PDF
- monitor cheque activity from a dashboard
- control access with role-based authentication

The application ships with seeded demo data and default user accounts so it can be explored immediately after the database is created.

## Key Features

### 1. Authentication and Authorization

- ASP.NET Identity based login system
- role-based access using `Admin`, `Accountant`, and `Viewer`
- persistent sign-in support
- access-denied flow for unauthorized actions
- unique-email enforcement for users

### 2. Bank Management

- create, edit, search, paginate, and delete banks
- store bank name and optional SWIFT code
- deletion restricted to `Admin`

### 3. Bank Account Management

- create, edit, search, paginate, and delete bank accounts
- map each account to a bank
- maintain opening balance
- configure cheque printing coordinates for:
  - date
  - payee name
  - numeric amount
  - amount in words
- store account-specific custom cheque template CSS
- deletion restricted to `Admin`

### 4. Payee Management

- create, edit, search, paginate, and delete payees
- store payee name, email, and phone
- deletion restricted to `Admin`

### 5. Cheque Issuance and Lifecycle

- create cheques with:
  - cheque number
  - cheque date
  - payee
  - amount
  - bank account
  - status
  - remarks
- auto-convert the amount into words
- support attachment upload during cheque creation
- support redirect-to-print immediately after creation
- support cheque cancellation with a reason
- prevent cancellation of already cancelled cheques
- prevent cancellation of cleared cheques

Supported statuses:

- `Issued`
- `Cleared`
- `Cancelled`
- `PostDated`

Validation rules for new cheques:

- cheque number is required
- amount must be greater than zero
- payee is required
- bank account is required
- date is required
- remarks are limited to 512 characters
- new cheques can only be created as `Issued` or `PostDated`

### 6. Cheque Printing

- dedicated print view for each cheque
- print layout driven by account-level coordinates
- formatted date rendering for boxed cheque date layouts
- support for custom CSS template per bank account

This makes the system adaptable to different cheque stationery layouts without changing application code.

### 7. Dashboard and Monitoring

The dashboard provides a quick operational snapshot including:

- total cheque count
- total cleared amount
- number of pending cheques
- number of upcoming post-dated cheques within 14 days
- estimated remaining balance by bank

Balance calculation is based on:

- sum of opening balances for accounts under each bank
- minus total amount of cleared cheques for those accounts

### 8. Reporting

- filter cheques by:
  - date range
  - payee name
  - bank account number
  - cheque number
  - status
- paginated report listing
- summary counts for:
  - cancelled cheques
  - pending cheques
  - cleared cheques
- export filtered report to Excel
- export filtered report to PDF

### 9. Excel Import

- download a ready-made Excel import template
- import cheques in bulk from Excel
- validate row data during import
- skip invalid rows while continuing valid imports
- return a result summary with:
  - imported count
  - skipped count
  - row-level issues

Expected import columns:

1. `ChequeNumber`
2. `Date`
3. `PayeeName`
4. `Amount`
5. `AccountNumber`
6. `Status`
7. `Remarks`

Import expectations:

- payee name must exactly match an existing payee
- account number must exactly match an existing bank account
- blank rows are ignored
- invalid rows are skipped and reported
- if status is missing or invalid, it defaults to `Issued`

### 10. File Attachments

- optional file upload when creating a cheque
- uploaded files are stored under `wwwroot/uploads`
- attachment metadata is saved in the database

### 11. Audit Support

The system includes audit-aware entities and persists audit records for important actions such as:

- cheque creation
- cheque cancellation

Base entities also carry audit-style timestamps and user references.

### 12. API Endpoints

Authenticated API endpoints are available for simple integrations:

- `GET /api/api/cheques`
- `GET /api/api/dashboard`

Both endpoints require authentication.

## Solution Structure

The solution is split into separate projects:

- `src/ChequePrintingSystem.Presentation`
  ASP.NET Core MVC UI, controllers, views, static assets, and API endpoints.

- `src/ChequePrintingSystem.Application`
  application services, DTOs, validation, import/export logic, and business orchestration.

- `src/ChequePrintingSystem.Domain`
  core entities and enums such as `Cheque`, `Bank`, `BankAccount`, `Payee`, `Attachment`, and `ChequeStatus`.

- `src/ChequePrintingSystem.Infrastructure`
  Entity Framework Core persistence, SQL Server configuration, repository pattern, unit of work, Identity integration, current-user service, migrations, and data seeding.

- `tests/ChequePrintingSystem.Application.Tests`
  unit tests for application-level behavior.

## Technology Stack

- .NET 8
- ASP.NET Core MVC
- ASP.NET Core Identity
- Entity Framework Core
- SQL Server
- FluentValidation
- Serilog
- ClosedXML
- QuestPDF
- xUnit

## Architecture Notes

The codebase follows a layered separation of concerns:

- `Presentation` handles HTTP requests, views, and user interactions.
- `Application` contains business logic, orchestration, DTO mapping, and validation.
- `Domain` holds business entities and enums without UI or persistence dependencies.
- `Infrastructure` handles persistence, Identity, and cross-cutting services.

The application also uses:

- repository pattern for data access abstraction
- unit of work for coordinated persistence
- dependency injection for service composition

## Core Domain Model

### Bank

Represents a financial institution.

Main fields:

- `Name`
- `SwiftCode`

### BankAccount

Represents a bank account and its cheque print layout configuration.

Main fields:

- `BankId`
- `AccountNumber`
- `OpeningBalance`
- `DateX`, `DateY`
- `PayeeX`, `PayeeY`
- `AmountNumericX`, `AmountNumericY`
- `AmountWordsX`, `AmountWordsY`
- `TemplateCss`

### Payee

Represents a cheque recipient.

Main fields:

- `Name`
- `Email`
- `Phone`

### Cheque

Represents an issued cheque and its lifecycle state.

Main fields:

- `ChequeNumber`
- `Date`
- `PayeeId`
- `Amount`
- `AmountInWords`
- `BankAccountId`
- `Status`
- `ClearedOn`
- `Remarks`

### Attachment

Stores metadata for files attached to a cheque.

### AuditLog

Stores audit trail entries for key operations.

### ApplicationUser

Extends ASP.NET Identity user with:

- `FullName`

## Default Seed Data

On startup, the application automatically applies migrations and seeds initial data.
Seeding behavior is environment-aware so that production never ships with known
credentials or demo records.

### Seeded Roles (all environments)

- `Admin`
- `Accountant`
- `Viewer`

### Seeded Users

In **Development** (and only there) the following demo accounts are created:

- `admin@cheque.local` / `Admin@123`
- `accountant@cheque.local` / `Accountant@123`
- `viewer@cheque.local` / `Viewer@123`

In **Production**, no demo accounts are created. An initial administrator is created
only when you provide credentials via configuration (see below). If you do not,
the app logs a warning and seeds no login, leaving user provisioning to you.

### Seeded Business Data

The demo bank, demo bank account, demo payee, and sample cheques are seeded only in
**Development**. Production starts with an empty business dataset.

## Production Configuration

The committed `appsettings.json` is geared for local development. For production,
override the following via environment variables (the standard ASP.NET Core
double-underscore convention) or your platform's secret store:

| Setting | Env variable | Purpose |
| --- | --- | --- |
| Database | `ConnectionStrings__DefaultConnection` | Production SQL Server connection string (e.g. SQL auth / Azure SQL). The default `Server=.;Trusted_Connection=True` only works locally. |
| Initial admin email | `Seed__AdminEmail` | Email/username of the administrator to provision on first run. |
| Initial admin password | `Seed__AdminPassword` | Password for that administrator (must satisfy the Identity policy: 8+ chars, upper, digit). |
| Data-protection key path | `DataProtection__KeysPath` | Directory where data-protection keys are persisted. Defaults to `dp-keys` under the content root. Point this at durable/shared storage so auth cookies and antiforgery tokens survive restarts and scale-out. |

Additional production hardening already built in:

- SQL connections use `EnableRetryOnFailure` for transient cloud database errors.
- `X-Forwarded-For` / `X-Forwarded-Proto` headers are honored, so HTTPS redirection
  and secure cookies work correctly behind a reverse proxy or load balancer.
- HTTPS redirection is enabled only when an HTTPS endpoint is actually configured.
- Antiforgery (CSRF) validation is enforced globally on all state-changing requests.

## Running the Project

### Prerequisites

- .NET SDK 8 or newer installed
- SQL Server available locally
  examples:
  - SQL Server Express
  - LocalDB
  - full SQL Server

### Connection String

Default connection string in `src/ChequePrintingSystem.Presentation/appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=.;Database=cheque_printing_db;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true"
  }
}
```

Update it if your SQL Server instance name is different.

### First-Time Setup

Run from the repository root:

```powershell
cd "c:\Users\Shekhani Laptops\ChequePrintingSystem-master\ChequePrintingSystem-master"
dotnet tool install --global dotnet-ef
dotnet ef database update --project src\ChequePrintingSystem.Infrastructure --startup-project src\ChequePrintingSystem.Presentation
dotnet build src\ChequePrintingSystem.Presentation
```

### Start the Application

```powershell
dotnet run --project src\ChequePrintingSystem.Presentation --no-build
```

Default URLs from launch settings:

- `http://localhost:5050`
- `https://localhost:7280`

In the current local setup, HTTP on `http://localhost:5050` is the reliable path to use.

If port `5050` is already in use, either stop the existing process or run on a different port:

```powershell
dotnet run --project src\ChequePrintingSystem.Presentation --no-build --urls "http://localhost:5051"
```

## Typical User Workflow

1. Log in with a seeded account.
2. Create or update banks.
3. Create bank accounts and configure cheque print positions.
4. Create payees.
5. Issue cheques manually or import them from Excel.
6. Print a cheque using the print view.
7. Track cheque states from the cheque list and dashboard.
8. Filter reports and export data to Excel or PDF.

## Role Expectations

### Admin

- full access to all modules
- can create, edit, and delete banks, payees, and bank accounts
- can issue and cancel cheques

### Accountant

- can create and edit operational master data
- can issue and cancel cheques
- cannot perform admin-only deletes

### Viewer

- can sign in and view authorized pages
- cannot perform restricted write operations

## Testing

Application tests are available in `tests/ChequePrintingSystem.Application.Tests`.

They cover logic such as:

- cheque service behavior
- amount-to-words conversion

Run tests with:

```powershell
dotnet test
```

## Logging

Serilog is configured for:

- console logging
- rolling file logging under `logs/app-.log`

This is useful for:

- startup diagnostics
- request tracing
- runtime error investigation

## Known Notes

- the project targets `net8.0`
- the solution can be built with newer SDKs, but it still targets .NET 8
- HTTPS may not be available in every local environment without certificate setup
- the application auto-runs EF Core migrations during startup through the database seeder
- attachment files are stored on disk, so deployment should account for persistent file storage

## Potential Next Enhancements

- richer approval workflows
- cheque clearing and reconciliation screens
- stronger audit reporting UI
- notification and reminder background jobs
- role-based menu trimming
- API expansion for external integrations
- attachment download and management screens

## License

No license file is currently included in this repository. Add one if you plan to distribute or publish the project.

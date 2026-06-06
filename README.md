# Global Logistics Management System (GLMS)

**TechMove Logistics — EAPD7111 Part 2**  
Enterprise ASP.NET Core MVC monolith with Entity Framework Core, SQL Server, external currency API integration, and unit tests.

---

## Table of contents

1. [Project overview](#project-overview)
2. [Assignment requirements checklist](#assignment-requirements-checklist)
3. [Technology stack](#technology-stack)
4. [Solution structure](#solution-structure)
5. [Prerequisites](#prerequisites)
6. [Getting started](#getting-started)
7. [Database and migrations](#database-and-migrations)
8. [Application features](#application-features)
9. [Business logic and services](#business-logic-and-services)
10. [External currency API](#external-currency-api)
11. [Unit testing](#unit-testing)
12. [Demonstration workflow (for video)](#demonstration-workflow-for-video)
13. [Submission checklist](#submission-checklist)
14. [Troubleshooting](#troubleshooting)
15. [Part 3 — Docker & SOA](#part-3--docker--soa)

---

## Part 3 — Docker & SOA

Part 3 refactors GLMS into a **Service-Oriented Architecture**:

| Container | Service | Port |
|-----------|---------|------|
| `sql-server-db` | SQL Server 2022 | 1433 |
| `glms-backend-api` | ASP.NET Core Web API + JWT + Swagger | 8080 |
| `glms-frontend-web` | ASP.NET Core MVC (HttpClient only) | 8081 |

### Run with Docker

```bash
docker compose up --build
```

- **MVC UI:** http://localhost:8081  
- **Swagger API:** http://localhost:8080/swagger  
- **Login:** admin / Admin@123  

### API Endpoints

- `POST /api/auth/login` — JWT token
- `GET /api/contracts?startDate=&endDate=&status=` — filtered list
- `POST /api/contracts` — create (multipart PDF)
- `PATCH /api/contracts/{id}/status` — update status
- Full CRUD for clients and service requests

### Submission documents

- `docs/GLMS_Part3_Submission.html` — open in Word, save as .docx
- `docs/GLMS_Part3_Technical_Reflection_Report.html` — save as PDF for ARC

---

## Project overview

GLMS replaces TechMove’s legacy spreadsheets and email chains with a single web platform for:

- **Clients** — international customer records and contact details  
- **Contracts** — legal agreements, SLAs, status workflow, and signed PDF storage  
- **Service requests** — logistics jobs linked to contracts, with USD→ZAR conversion  

This repository implements **Part 2**: a functional **monolith** prototype with automated unit tests. Part 3 will containerise and split services.

---

## Assignment requirements checklist

| Requirement | Status | Implementation |
|-------------|--------|----------------|
| ASP.NET Core MVC monolith | Done | `EAPD7111_PART2` web project |
| SQL Server + EF Core | Done | `GLMSDbContext`, `Migrations/` |
| **Client** (Name, Contact, Region) | Done | `Models/Client.cs`, `ClientController` |
| **Contract** (Client, dates, status, SLA) | Done | `Models/Contract.cs`, `ContractController` |
| **ServiceRequest** (Contract, description, cost, status) | Done | `Models/ServiceRequest.cs`, `CostUSD` + `CostZAR` |
| Status workflow: Draft, Active, Expired, On Hold | Done | `ContractStatus` enum |
| Automated status tracking | Done | `ContractStatusAutomationService` auto-expires Active contracts past `EndDate` |
| PDF upload per contract | Done | `FileUploadService` → `wwwroot/uploads/contracts/` |
| PDF download in UI | Done | `ContractController.Download` |
| Service request blocked if Expired / On Hold | Done | `ContractWorkflowService` |
| Only **Active** contracts in create dropdown | Done | `ServiceRequestController.Create` |
| LINQ search/filter contracts (date + status) | Done | `ContractQueryService`, Contract Index filter form |
| External USD→ZAR API via HttpClient | Done | `CurrencyConversionService` (open.er-api.com) |
| Auto-calculate ZAR on create page | Done | JavaScript + `GetExchangeRate` endpoint |
| Save ZAR cost to database | Done | `CostZAR` on POST Create |
| Separate unit test project | Done | `EAPD7111_PART2.Tests` (xUnit) |
| Currency calculation tests | Done | `CurrencyConversionServiceTests` |
| File validation tests (.exe rejected) | Done | `FileUploadServiceTests` |
| Additional business logic tests | Done | Workflow, query filter, automation, date validation |

---

## Technology stack

| Layer | Technology |
|-------|------------|
| Framework | .NET 9.0 |
| UI | ASP.NET Core MVC (Razor views) |
| ORM | Entity Framework Core 9 |
| Database | SQL Server (LocalDB default) |
| HTTP | `IHttpClientFactory` / typed `HttpClient` |
| Tests | xUnit, Moq |

---

## Solution structure

```
EAPD7111_PART2-1/
├── EAPD7111_PART2.sln              # Open this in Visual Studio
├── EAPD7111_PART2.csproj           # Main web application
├── EAPD7111_PART2.Tests/           # Unit test project (xUnit)
├── Controllers/
│   ├── ClientController.cs
│   ├── ContractController.cs
│   ├── ServiceRequestController.cs
│   └── HomeController.cs
├── Models/
│   ├── Client.cs
│   ├── Contract.cs
│   ├── ServiceRequest.cs
│   ├── ContractStatus.cs
│   └── ServiceRequestStatus.cs
├── Data/
│   └── GLMSDbContext.cs
├── Services/                       # Testable business logic
│   ├── CurrencyConversionService.cs
│   ├── FileUploadService.cs
│   ├── ContractWorkflowService.cs
│   ├── ContractQueryService.cs
│   ├── ContractStatusAutomationService.cs
│   └── ContractValidationService.cs
├── Migrations/                     # EF Core migration scripts
├── Views/                          # Razor UI
├── wwwroot/
│   └── uploads/contracts/          # Simulated file server for PDFs
├── appsettings.json
└── README.md
```

---

## Prerequisites

Install before running:

1. [.NET 9 SDK](https://dotnet.microsoft.com/download)
2. **SQL Server** — one of:
   - Visual Studio **SQL Server Express LocalDB** (Windows), or  
   - SQL Server / Azure SQL with an updated connection string
3. **Visual Studio 2022** (17.8+) or **VS Code** + C# Dev Kit (recommended for Test Explorer screenshots)
4. EF Core tools (for manual migrations):

   ```bash
   dotnet tool install --global dotnet-ef
   ```

---

## Getting started

### 1. Clone the repository

```bash
git clone <your-github-repo-url>
cd EAPD7111_PART2-1
```

### 2. Configure the database connection

Default in `appsettings.json`:

```json
"ConnectionStrings": {
  "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=GLMSDb;Trusted_Connection=True;MultipleActiveResultSets=true"
}
```

On **macOS/Linux**, use Docker SQL Server or a remote instance, for example:

```json
"DefaultConnection": "Server=localhost,1433;Database=GLMSDb;User Id=sa;Password=YourStrong!Passw0rd;TrustServerCertificate=True"
```

### 3. Restore, migrate, and run

```bash
dotnet restore
dotnet ef database update
dotnet run --project EAPD7111_PART2.csproj
```

The app applies pending migrations automatically on startup. Browse to the URL shown in the console (typically `https://localhost:7xxx`).

### 4. Run unit tests

```bash
dotnet test
```

Expected: **all tests passed** (29 tests).

**Visual Studio:** Test → Test Explorer → Run All Tests → capture screenshot for submission.

---

## Database and migrations

### Entities

| Table | Key fields |
|-------|------------|
| `Clients` | Name, Email, PhoneNumber, Address, **Region**, ContactPerson |
| `Contracts` | ClientId, ContractNumber, StartDate, EndDate, **Status**, ServiceLevel, SignedAgreementFilePath |
| `ServiceRequests` | ContractId, RequestNumber, Description, **CostUSD**, **CostZAR**, Status |

### Migration scripts

Located in `Migrations/`:

- `20260517204710_InitialCreate.cs` — creates all tables and relationships  
- `GLMSDbContextModelSnapshot.cs` — current model snapshot  

### Commands

```bash
# Apply migrations to database
dotnet ef database update --project EAPD7111_PART2.csproj

# Create a new migration after model changes
dotnet ef migrations add <MigrationName> --project EAPD7111_PART2.csproj
```

---

## Application features

### Clients

- Full CRUD: list, create, edit, delete, details  
- Contact details: email, phone, address, region, optional contact person  
- Details page shows linked contracts  

### Contracts (Contract Management Hub)

- Link to client, contract number, start/end dates, SLA (`ServiceLevel`), description  
- **Status:** Draft, Active, Expired, On Hold  
- **Upload** signed agreement (PDF only)  
- **Download** PDF from the contracts list  
- **Filter** by start date, end date, and status (LINQ)  
- **Auto-expire:** Active contracts with `EndDate` in the past are set to **Expired** when the contract list is loaded  

### Service requests

- Linked to an **Active** contract only (dropdown filtered)  
- Server-side validation blocks Expired, On Hold, and Draft contracts  
- Enter cost in **USD**; **ZAR** is calculated using the live exchange rate  
- ZAR value is persisted in `CostZAR` on save  
- Status: Pending, In Progress, Completed, Cancelled  

---

## Business logic and services

Logic is extracted from controllers into services so it can be **unit tested**.

| Service | Responsibility |
|---------|----------------|
| `CurrencyConversionService` | USD→ZAR rate from API; `CalculateZARFromUSD(usd, rate)` |
| `FileUploadService` | PDF-only validation, save to `wwwroot`, download bytes |
| `ContractWorkflowService` | Whether a service request may be created for a contract status |
| `ContractQueryService` | LINQ filters for date range and status |
| `ContractStatusAutomationService` | Auto-expire Active contracts past end date |
| `ContractValidationService` | Start date ≤ end date validation |

### Service request rules

| Contract status | Can create service request? |
|-----------------|----------------------------|
| Active | Yes |
| Draft | No |
| Expired | No |
| On Hold | No |

### Currency formula

```
CostZAR = Round(CostUSD × ExchangeRate, 2)
```

If the API is unavailable, fallback rate **18.50** is used (`CurrencyConversionService.FallbackUsdToZarRate`).

---

## External currency API

- **Provider:** [open.er-api.com](https://open.er-api.com)  
- **Endpoint:** `GET https://open.er-api.com/v6/latest/USD`  
- **Client:** Typed `HttpClient` registered in `Program.cs`  
- **UI:** Create Service Request page calls `/ServiceRequest/GetExchangeRate` and updates ZAR as the user types USD  

---

## Unit testing

Project: **`EAPD7111_PART2.Tests`** (xUnit)

| Test class | What it verifies |
|------------|------------------|
| `CurrencyConversionServiceTests` | USD→ZAR math, rounding, invalid inputs |
| `FileUploadServiceTests` | `.pdf` allowed, `.exe` rejected, upload throws, download missing file |
| `ContractWorkflowServiceTests` | Active vs Expired/On Hold/Draft rules |
| `ContractQueryServiceTests` | LINQ date and status filters |
| `ContractStatusAutomationServiceTests` | Auto-expire when past end date |
| `ContractValidationServiceTests` | End date must be ≥ start date |

```bash
dotnet test --verbosity normal
```

For submission: screenshot **Test Explorer** showing all green results.

---

## Demonstration workflow (for video)

Record a walkthrough covering:

1. **Home** — introduce GLMS and TechMove  
2. **Clients** — create a client (name, region, contact details)  
3. **Contracts** — create contract with status **Active**, upload a **PDF**, show download  
4. **Contracts filter** — filter by status and date range  
5. **Service request** — select active contract, enter USD, show live ZAR conversion, submit, verify saved ZAR on details/list  
6. **Validation** — change contract to **On Hold** or **Expired**, show that a new service request is blocked  
7. **Auto-expire** — create Active contract with end date in the past, open contract list, show status changed to Expired  
8. **Tests** — run `dotnet test` or Test Explorer, show all passed  


---

## Troubleshooting

| Issue | Solution |
|-------|----------|
| **Cannot copy `EAPD7111_PART2.exe` — file in use** | The app is still running. In Visual Studio click **Stop** (Shift+F5), or close the browser tab, or open Task Manager and end **EAPD7111_PART2.exe**, then rebuild. |
| **Visual Studio does not support .NET 10** | Ensure `EAPD7111_PART2.Tests` targets **net9.0** (not net10.0). Pull latest from GitHub or set `<TargetFramework>net9.0</TargetFramework>` in the test `.csproj`. |
| Cannot connect to database | Verify SQL Server/LocalDB is running; update `appsettings.json` connection string |
| `dotnet ef` not found | Run `dotnet tool install --global dotnet-ef` |
| Exchange rate shows fallback 18.50 | No internet or API down; app still works with fallback |
| PDF upload fails | Only `.pdf` extension allowed; check `wwwroot/uploads/contracts/` exists |
| Tests not visible in VS | Open `EAPD7111_PART2.sln`, build solution, open Test Explorer |
| macOS LocalDB error | Use Docker SQL Server and update connection string |

### File locked during build (Windows)

If you see `The file is locked by: "EAPD7111_PART2 (####)"`:

1. **Stop debugging** in Visual Studio (red square / Shift+F5).
2. Or run in **Developer PowerShell**:

   ```powershell
   taskkill /F /IM EAPD7111_PART2.exe
   ```

3. **Build → Clean Solution**, then **Rebuild Solution**.

---


EAPD7111 — Enterprise Application Development  
TechMove Logistics GLMS PoE Part 2

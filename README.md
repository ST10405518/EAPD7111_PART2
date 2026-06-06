# Global Logistics Management System (GLMS)

**TechMove Logistics — EAPD7111 Parts 2 & 3**

GLMS is a web platform for managing international logistics clients, contracts, and service requests. Part 2 delivers the business features (CRUD, PDF uploads, contract workflow, USD→ZAR conversion). Part 3 refactors the system into a **Service-Oriented Architecture (SOA)** with a separate Web API, JWT authentication, and an MVC frontend that talks to the API over HTTP.

**Repository:** https://github.com/ST10405518/EAPD7111_PART2

---

## Table of contents

1. [Architecture](#architecture)
2. [Prerequisites](#prerequisites)
3. [Run with Visual Studio (recommended)](#run-with-visual-studio-recommended)
4. [Sign in](#sign-in)
5. [Run from the command line](#run-from-the-command-line)
6. [Run with Docker](#run-with-docker)
7. [Solution structure](#solution-structure)
8. [Features](#features)
9. [API overview](#api-overview)
10. [Database](#database)
11. [Testing](#testing)
12. [Troubleshooting](#troubleshooting)
13. [Submission documents](#submission-documents)

---

## Architecture

| Project | Role | Default URL |
|---------|------|-------------|
| **GLMS.Api** | ASP.NET Core Web API — EF Core, repositories, JWT, Swagger | http://localhost:8080 |
| **EAPD7111_PART2** | ASP.NET Core MVC frontend — `HttpClient` only, no direct DB access | https://localhost:7159 or http://localhost:5064 |
| **GLMS.Shared** | Shared models and DTOs used by API and MVC | — |
| **EAPD7111_PART2.Tests** | Unit tests + API integration tests | — |

```
Browser  →  EAPD7111_PART2 (MVC)  →  GLMS.Api (REST + JWT)  →  SQL Server (GLMSDb)
```

The MVC app stores a JWT in the `glms_token` cookie after login. All data requests go through `GlmsApiClient`, which attaches that token to API calls.

---

## Prerequisites

Install before running:

1. [.NET 9 SDK](https://dotnet.microsoft.com/download)
2. **Visual Studio 2022** (17.8+) with ASP.NET and web development workload
3. **SQL Server Express LocalDB** (included with Visual Studio on Windows)
4. Optional — [Docker Desktop](https://www.docker.com/products/docker-desktop/) for containerised deployment

For manual EF Core commands:

```bash
dotnet tool install --global dotnet-ef
```

---

## Run with Visual Studio (recommended)

Both projects must run at the same time. The MVC app calls the API at `http://localhost:8080`; if only the frontend is running, pages will fail to load data.

### One-time setup: multiple startup projects

1. Open **`EAPD7111_PART2.sln`** in Visual Studio.
2. In **Solution Explorer**, right-click the **Solution** (top node).
3. Click **Properties**.
4. Select **Startup Project**.
5. Choose **Multiple startup projects**.
6. Set the action for each project:

   | Project | Action |
   |---------|--------|
   | **GLMS.Api** | **Start** |
   | **EAPD7111_PART2** | **Start** |
   | EAPD7111_PART2.Tests | None |
   | GLMS.Shared | None |

7. Click **Apply** → **OK**.

### Start the application

1. If either project is already running from a previous session, stop it (**Shift+F5** or **Ctrl+C** in the terminal).
2. Press **F5** (or click **Start**).
3. Visual Studio should launch:
   - **GLMS.Api** — Swagger at http://localhost:8080/swagger
   - **EAPD7111_PART2** — the GLMS web UI (browser opens automatically)
4. In the MVC app, go to **Login** and sign in (see [Sign in](#sign-in)).
5. Use **Clients**, **Contracts**, and **Service Requests** from the navigation menu.

> **Order matters:** `GLMS.Api` must be running before you sign in or browse data pages. With multiple startup projects configured, Visual Studio starts both together.

---

## Sign in

| Field | Value |
|-------|-------|
| Username | `admin` |
| Password | `Admin@123` |

After a successful login, the MVC app stores a JWT cookie and can load clients, contracts, and service requests from the API.

---

## Run from the command line

Use two terminals. Start the **API first**, then the **MVC app**.

**Terminal 1 — API**

```bash
cd GLMS.Api
dotnet run
```

**Terminal 2 — MVC**

```bash
dotnet run --project EAPD7111_PART2.csproj
```

Then open the MVC URL shown in the console and sign in.

**Windows shortcut scripts** (in `scripts/`):

- `scripts/start-dev.bat`
- `scripts/start-dev.sh`

---

## Run with Docker

```bash
docker compose up --build
```

| Service | URL |
|---------|-----|
| MVC UI | http://localhost:8081 |
| Swagger API | http://localhost:8080/swagger |
| SQL Server | `localhost:1433` |

Login: `admin` / `Admin@123`

---

## Solution structure

```
EAPD7111_PART2-1/
├── EAPD7111_PART2.sln
├── EAPD7111_PART2.csproj          # MVC frontend
├── GLMS.Api/                      # Web API backend
│   ├── Controllers/
│   ├── Repositories/
│   ├── Services/
│   ├── Data/
│   └── Migrations/
├── GLMS.Shared/                   # Shared models & DTOs
├── EAPD7111_PART2.Tests/          # Unit + integration tests
├── Controllers/                   # MVC controllers (API client only)
├── Services/Api/                  # GlmsApiClient, JWT cookie handler
├── Views/
├── docker-compose.yml
├── docs/                          # Part 3 submission HTML templates
└── README.md
```

---

## Features

### Clients

- Full CRUD: list, create, edit, delete, details
- Contact details: email, phone, address, region, contact person
- Details page shows linked contracts

### Contracts

- Linked to a client; contract number, dates, SLA, description
- Status workflow: **Draft**, **Active**, **Expired**, **On Hold**
- Upload and download signed agreement PDFs
- Filter by start date, end date, and status (LINQ)
- Auto-expire: Active contracts past `EndDate` are set to **Expired** when the list is loaded

### Service requests

- Linked to an **Active** contract (dropdown filtered on create)
- Blocked when contract is Draft, Expired, or On Hold
- Cost in **USD** with live **USD→ZAR** conversion
- `CostZAR` saved to the database on create/update
- Status: Pending, In Progress, Completed, Cancelled

### Business rules

| Contract status | New service request allowed? |
|-----------------|------------------------------|
| Active | Yes |
| Draft | No |
| Expired | No |
| On Hold | No |

**Currency formula:** `CostZAR = Round(CostUSD × ExchangeRate, 2)`  
**Fallback rate** if the external API is unavailable: **18.50**

---

## API overview

Base URL (local): **http://localhost:8080**

| Endpoint | Description |
|----------|-------------|
| `GET /api/health` | Health check (no auth) |
| `POST /api/auth/login` | Returns JWT token |
| `GET/POST/PUT/DELETE /api/clients` | Client CRUD |
| `GET/POST/PUT/PATCH/DELETE /api/contracts` | Contract CRUD + status patch |
| `GET /api/contracts/{id}/download` | Download signed PDF |
| `GET/POST/PUT/DELETE /api/servicerequests` | Service request CRUD |
| `GET /api/servicerequests/exchange-rate` | Current USD→ZAR rate |

Interactive documentation: http://localhost:8080/swagger

---

## Database

Both **GLMS.Api** and the legacy Part 2 migrations use the same database name: **`GLMSDb`**.

Default connection string (`GLMS.Api/appsettings.json`):

```json
"ConnectionStrings": {
  "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=GLMSDb;Trusted_Connection=True;MultipleActiveResultSets=true"
}
```

The API applies EF Core migrations automatically on startup. Existing Part 2 data in LocalDB is preserved and accessible through the API.

**Manual migration (if needed):**

```bash
dotnet ef database update --project GLMS.Api
```

---

## Testing

```bash
dotnet test
```

| Test area | Project path |
|-----------|----------------|
| Currency, file upload, workflow, validation | `EAPD7111_PART2.Tests/Services/` |
| API auth, CRUD, nested JSON responses | `EAPD7111_PART2.Tests/Integration/` |

**Visual Studio:** Test → Test Explorer → Run All Tests.

---

## Troubleshooting

| Issue | What to do |
|-------|------------|
| **Could not load … from the API** | Ensure **GLMS.Api** is running on port **8080** before using the MVC app. Configure multiple startup projects (see above). |
| **Cannot reach the API at login** | Start `GLMS.Api` first. Check `appsettings.Development.json` → `ApiSettings:BaseUrl` is `http://localhost:8080`. |
| **Session expired / sign in again** | Log in again with `admin` / `Admin@123`. The JWT expires after 60 minutes. |
| **Cannot connect to database** | Confirm SQL Server LocalDB is installed and running. Update the connection string in `GLMS.Api/appsettings.json` if needed. |
| **File locked during build** | Stop debugging (**Shift+F5**) or end `GLMS.Api.exe` / `EAPD7111_PART2.exe` in Task Manager, then rebuild. |
| **Visual Studio does not support .NET 10** | All projects target **net9.0**. Pull the latest code from GitHub. |
| **Exchange rate shows 18.50** | No internet or API down; the app uses the fallback rate and still works. |
| **PDF upload fails** | Only `.pdf` files are allowed. |

### Verify the API is running

Open http://localhost:8080/swagger or http://localhost:8080/api/health — you should get a successful response before signing in to the MVC app.

---

## Submission documents

Part 3 write-up templates (open in Microsoft Word, then save as required format):

- `docs/GLMS_Part3_Submission.html` → save as `.docx`
- `docs/GLMS_Part3_Technical_Reflection_Report.html` → save as `.pdf`

---

EAPD7111 — Enterprise Application Development  
TechMove Logistics GLMS — Parts 2 & 3

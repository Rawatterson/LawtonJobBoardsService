# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

ASP.NET Core 10.0 Web API that acts as a proxy between the **LawtonJobBoards Blazor frontend** and the **Ordant print shop management API**. It surfaces Ordant orders and production scheduler jobs as job-board cards, adding a computed `DueStatus` field based on each job's due date.

No database. All data is fetched live from Ordant on every request.

## Commands

```bash
dotnet restore
dotnet build

# Run (HTTP on port 5106)
dotnet run --project LawtonJobBoardsServices --launch-profile http

# Run (HTTPS on port 7135)
dotnet run --project LawtonJobBoardsServices --launch-profile https
```

## Architecture

**Stack:** .NET 10, ASP.NET Core Web API, controller-based routing, `System.Text.Json`, `Microsoft.AspNetCore.WebUtilities` for query-string building. OpenAPI (`Microsoft.AspNetCore.OpenApi`) auto-registered.

**Solution format:** Modern `.slnx` (`LawtonJobBoardsServices.slnx`).

**Entry point:** `Program.cs` — registers `OrdantClient` as a typed `HttpClient`, sets the `X-Authentication` header from config, and registers `DueStatusCalculator`.

### Key layers

| Layer | Path | Purpose |
|-------|------|---------|
| Configuration | `Configuration/OrdantSettings.cs` | Typed options: `BaseUrl`, `ApiKey`, `DueSoonThresholdHours` |
| HTTP client | `Services/OrdantClient.cs` | One method per Ordant endpoint; builds PHP-style serializer/criteria query params |
| Computed field | `Services/DueStatusCalculator.cs` | Converts `dueDate` → `DueStatus` enum (`NoDueDate / OnTime / DueSoon / Overdue`) |
| Ordant models | `Models/Ordant/OrdantModels.cs` | POCO classes that match Ordant's JSON response shapes (deserialization only) |
| DTOs | `Models/Dto/` | Lean shapes returned to the Blazor frontend; include `DueStatus` |
| Controllers | `Controllers/` | `OrdersController` and `ResourcePlannerController` — map Ordant models → DTOs |

### Ordant API facts

- **Base URL:** configured in `appsettings.json` → `Ordant:BaseUrl` (dev: `https://service-development.ordant.com:8000/v3/`)
- **Auth:** `X-Authentication` header; key lives in `appsettings.Development.json` → `Ordant:ApiKey`
- **List endpoints** require PHP-style `serializer[]` and `criteria[]` query params — `OrdantClient` builds these internally; controllers expose clean params (`page`, `limitPerPage`, `isComplete`, `status` / `stationId`)
- **Detail endpoints** (`/order/{id}`, `/scheduler/job/{id}`) return the full object with no serializer needed

### Exposed endpoints

| Method | Path | Proxies to |
|--------|------|-----------|
| GET | `/api/orders` | `GET /order` |
| GET | `/api/orders/{orderId}` | `GET /order/{orderId}` |
| GET | `/api/resource-planner` | `GET /scheduler/job` |
| GET | `/api/resource-planner/{jobId}` | `GET /scheduler/job/{jobId}` |

### DueStatus logic

Computed by `DueStatusCalculator` using `Ordant:DueSoonThresholdHours` (default 4 h):

- `NoDueDate` — `dueDate` is null
- `Overdue` — `dueDate` is in the past and job is not complete
- `DueSoon` — `dueDate` is within `DueSoonThresholdHours`
- `OnTime` — `dueDate` is beyond the threshold

For resource-planner jobs, `DueStatus` is computed from `orderItem.dateDue` (not the parent order's due date, since that field is absent from the scheduler job response).

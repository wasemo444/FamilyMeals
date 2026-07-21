# Manage Family Meals

Blazor web app with a PostgreSQL-backed API for storing meal categories and links.

## Prerequisites

- [.NET SDK 10](https://dotnet.microsoft.com/download)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/) (for PostgreSQL)

## Run locally

You need **three things running**: PostgreSQL, the API, and the Web UI. Start them in separate terminals.

### 1. Start PostgreSQL

From the repository root:

```powershell
docker compose up -d
```

PostgreSQL listens on **localhost:55432** (port 5432 inside the container).

| Setting  | Value               |
|----------|---------------------|
| Host     | `localhost`         |
| Port     | `55432`             |
| Database | `managefamilymeals` |
| User     | `mfm`               |
| Password | `mfm_dev`           |

### 2. Start the API

```powershell
cd src/ManageFamilyMeals.Api
dotnet run
```

The API runs at **http://localhost:5280**.

- There is no home page or Swagger UI at `/`.
- To verify it is working, open **http://localhost:5280/api/bootstrap** (JSON snapshot of categories, links, and settings).
- On first run in Development, EF Core applies migrations automatically.

### 3. Start the Web app

In a **second** terminal:

```powershell
cd src/ManageFamilyMeals.Web/ManageFamilyMeals.Web
dotnet run
```

Open **http://localhost:5084** in your browser (or the HTTPS URL shown in the console).

The Web client calls the API at `http://localhost:5280` (configured in `appsettings.json` / `ManageFamilyMeals.Web.Client/wwwroot/appsettings.json`).

### Build the whole solution (optional)

From the repository root:

```powershell
dotnet build ManageFamilyMeals.slnx
```

### Run tests (optional)

Use Release so tests do not conflict with a running Debug API:

```powershell
dotnet test tests/ManageFamilyMeals.Tests/ManageFamilyMeals.Tests.csproj -c Release
```

## Troubleshooting

**`MSB3027` / file locked by `ManageFamilyMeals.Api`**

Stop the running API before rebuilding (`Ctrl+C` in its terminal, or `Stop-Process -Name ManageFamilyMeals.Api -Force`), then run again.

**API fails on startup (database connection)**

Ensure Docker is running and PostgreSQL is up:

```powershell
docker compose ps
docker compose up -d
```

**Data disappears after `docker compose down -v`**

The `-v` flag removes the Docker volume and wipes the database. Use `docker compose down` without `-v` to keep data.

## Access the database

SSMS does not work with PostgreSQL. Use **pgAdmin**, **DBeaver**, **Azure Data Studio** (PostgreSQL extension), or `psql`:

```powershell
docker exec -it newdietapp-postgres-1 psql -U mfm -d managefamilymeals
```

Main tables: `meal_categories`, `meal_links`, `app_settings`.

## Project layout

| Project | Role |
|---------|------|
| `ManageFamilyMeals.Api` | REST API, EF Core, PostgreSQL, link preview fetch |
| `ManageFamilyMeals.Shared` | Models, `MealDataService`, API client, RESX strings |
| `ManageFamilyMeals.Web` | ASP.NET Core host (Blazor Auto) |
| `ManageFamilyMeals.Web.Client` | Interactive WASM pages (Home, Category, Archive) |
| `ManageFamilyMeals.Tests` | Unit and integration tests |

## What this validates

- **Blazor Web App (Auto)** with interactive WebAssembly components
- **Shared library** for models, services, and localization
- **PostgreSQL** persistence via EF Core and a standalone API
- **Bilingual EN/AR** with RTL support
- **Soft delete** with 7-day archive and restore
- **Link previews** via server-side Open Graph metadata fetch

See [docs/PRDV2.md](docs/PRDV2.md) for full requirements and epic breakdown.

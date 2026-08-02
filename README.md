# LinkNest

Blazor web app with a PostgreSQL-backed API for saving and organizing links — videos, courses, articles, recipes, or any other content — into categories and shared collections.

## Documentation

Start here for architecture, navigation, and class-level reference:

- **[docs/agents.md](docs/agents.md)** — entry point for developers and AI agents (architecture map, edit guide, guardrails)
- [docs/L1.md](docs/L1.md) — system context and module overview
- [docs/L2.md](docs/L2.md) — implementation flows (auth, data, ownership, API, Blazor)
- [docs/L3.md](docs/L3.md) — deep class reference and edge cases

Product requirements: [docs/PRDV2.md](docs/PRDV2.md)

**Production hosting, SMTP, Docker, and store publishing:** [docs/deployment.md](docs/deployment.md)

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
| Database | `linknest` |
| User     | `linknest`               |
| Password | `linknest_dev`           |

### 2. Start the API

```powershell
cd src/LinkNest.Api
dotnet run
```

The API runs at **http://localhost:5280**.

- There is no home page or Swagger UI at `/`.
- To verify it is working, log in at the Web app or POST to `/api/auth/login` (or `/api/auth/token` for mobile), then open **http://localhost:5280/api/bootstrap** (requires authentication — web: cookie; mobile/direct: `Authorization: Bearer` JWT).
- On first run in Development, EF Core applies migrations automatically.

### 3. Start the Web app

In a **second** terminal:

```powershell
cd src/LinkNest.Web/LinkNest.Web
dotnet run
```

The Web client proxies API calls through the Web host at the same origin (`/api/*` → API). Start the **API first**, then the Web app.

Open **http://localhost:5084** in your browser (or the HTTPS URL shown in the console).

### Authentication (E2 + E9)

- Register at `/register` or log in at `/login`.
- Default dev user (seeded on first run): `dev@linknest.local` / `DevPassword1!`
- Protected pages and `/api/*` data endpoints require authentication (web: cookie; mobile: JWT bearer); unauthenticated requests return **401**.
- Log out from the shell header menu.
- **Forgot password:** `/forgot-password` → reset link by email → `/reset-password`.
- **Settings:** `/settings` — display name, language, deactivate account.
- **DataProtection keys:** In Development, API and Web default to `%LOCALAPPDATA%/LinkNest/DataProtection-Keys` (see `appsettings.Development.json`). Both hosts must resolve to the **same directory** or auth cookies will not decrypt. In Production, set `LINKNEST_DATA_PROTECTION_KEYS_PATH` (legacy `MFM_DATA_PROTECTION_KEYS_PATH` is also accepted).
- **Ownership (E3+):** categories and links are scoped per user and group membership. See [docs/agents.md](docs/agents.md) and [docs/L2.md](docs/L2.md).

### Email (confirmation & password reset)

By default in **Development**, the API **logs** emails to the console instead of sending them. Look for a line like:

```
DEV MODE — email NOT sent via SMTP. Recipient: ...
```

Copy the confirmation or reset link from that log and open it in your browser.

**To send real email locally via Brevo**, set env vars on the **API** terminal (see [docs/deployment.md](docs/deployment.md#local-dev-smtp-brevo)) or use the `http-smtp` launch profile with user-secrets. On startup the API logs:

```
Email config: UseSmtp=True, ... EffectiveMode=SMTP
Email delivery mode: SMTP (real email)
```

If you see `EffectiveMode=LogOnly`, SMTP is not enabled — check `Email__UseSmtp=true` and that you restarted the API after setting env vars.

**Production / Docker:** SMTP is used automatically (`ASPNETCORE_ENVIRONMENT=Production`). See [docs/deployment.md](docs/deployment.md).

### Build the whole solution (optional)

From the repository root:

```powershell
dotnet build LinkNest.slnx
```

### Run tests (optional)

Use Release so tests do not conflict with a running Debug API:

```powershell
dotnet test tests/LinkNest.Tests/LinkNest.Tests.csproj -c Release
```

E2E tests (Playwright; first run installs Chromium):

```powershell
dotnet test tests/LinkNest.E2E.Tests/LinkNest.E2E.Tests.csproj -c Release
```

### Run the Mobile app (E7 — Windows)

**Prerequisites:** [.NET MAUI workload](https://learn.microsoft.com/dotnet/maui/get-started/installation) — run **once**, not in parallel with other workload installs:

```powershell
dotnet workload restore
dotnet workload install maui
```

Start PostgreSQL and the API first (steps 1–2 above), then:

```powershell
dotnet run --project src/LinkNest.Mobile/LinkNest.Mobile.csproj -f net10.0-windows10.0.19041.0
```

- Default API URL: `http://localhost:5280/` (see `src/LinkNest.Mobile/appsettings.json`).
- Physical device / emulator on another machine: set `LINKNEST_API_BASE_URL` or edit `appsettings.Development.json` (e.g. `http://192.168.x.x:5280/`).
- Login: `dev@linknest.local` / `DevPassword1!` (JWT bearer — not the web cookie flow).

If build fails with `NETSDK1147` mentioning `maui-tizen`, MAUI packs are incomplete — close Visual Studio, run `dotnet workload repair` once, then rebuild. See [E9 H3](docs/tickets/E9-carry-over-from-previous-epic.md#h3--android-target-framework-and-emulator-support) for Android target work.

Deferred mobile work (Android TFM, UI library extraction): [docs/tickets/E9-carry-over-from-previous-epic.md](docs/tickets/E9-carry-over-from-previous-epic.md#e7-mobile--deferred-follow-ups-carry-over-from-architect-review).

## Troubleshooting

**`MSB3027` / file locked by `LinkNest.Api`**

Stop the running API before rebuilding (`Ctrl+C` in its terminal, or `Stop-Process -Name LinkNest.Api -Force`), then run again.

**API fails on startup (database connection)**

Ensure Docker is running and PostgreSQL is up:

```powershell
docker compose ps
docker compose up -d
```

**Data disappears after `docker compose down -v`**

The `-v` flag removes the Docker volume and wipes the database. Use `docker compose down` without `-v` to keep data.

**MAUI build: `NETSDK1147` / `maui-tizen` / workload install `0x652`**

- The `maui-tizen` message is often misleading — it usually means MAUI workload **packs** are missing or a partial install failed.
- Do not run multiple `dotnet workload install` or `workload restore` commands at the same time (Windows MSI error `0x652`).
- Close Visual Studio, wait for installers to finish, then run `dotnet workload repair` once, followed by `dotnet workload restore src/LinkNest.Mobile/LinkNest.Mobile.csproj`.

**Email not arriving (local dev)**

- Default Development mode **does not send email** — copy the link from the **API** console log, or enable SMTP (see [Email](#email-confirmation--password-reset) above).
- SMTP runs on the **API** only (port 5280), not the Web host.
- On API startup, check `Email delivery mode:` — must say `SMTP (real email)` for Brevo delivery.
- Brevo **From address** must be verified under **Senders & IP** (not any random email).
- Brevo **SMTP login** is on **Settings → SMTP & API → SMTP** tab (may differ from your account email).

**Neon connection string / EF migrations fail**

Neon provides `postgresql://...?sslmode=require` URIs. LinkNest converts these automatically. Use single quotes in PowerShell. See [docs/deployment.md](docs/deployment.md#1-database-neon--free-tier).

**Reset test users**

Run `scripts/reset-users.sql` in the Neon SQL Editor (or local `psql`) to delete all users and start fresh. See [docs/deployment.md](docs/deployment.md#reset-test-users).

## Access the database

SSMS does not work with PostgreSQL. Use **pgAdmin**, **DBeaver**, **Azure Data Studio** (PostgreSQL extension), or `psql`:

```powershell
docker exec -it newdietapp-postgres-1 psql -U linknest -d linknest
```

Main tables: `meal_categories`, `meal_links` (legacy names kept for DB compatibility), `app_settings`.

## Upgrading from LinkNest

If you have an existing dev database or Docker volume from before the LinkNest rebrand:

1. **PostgreSQL:** `docker-compose.yml` now uses database/user `linknest` and volume `linknest_pgdata`. Either run `docker compose down -v && docker compose up -d` for a fresh database, or point connection strings at your old `linknest` database and user `mfm` until you migrate data.
2. **Auth:** Cookie name and Data Protection application name changed — all users must **log in again** after upgrading.
3. **Default dev user:** On startup the seeder updates the well-known default user to `dev@linknest.local` if it still has the legacy `dev@mfm.local` email.
4. **Migrations:** Start the API once so EF applies any pending migrations before serving traffic.
5. **C# domain types:** `ContentCategory`, `SavedLink`, and `IContentDataService` replace the old `Meal*` names; HTTP JSON shape is unchanged (`Categories` / `Links`).

## Project layout

| Project | Role |
|---------|------|
| `LinkNest.Api` | REST API, EF Core, PostgreSQL, link preview fetch |
| `LinkNest.Shared` | Models, `ContentDataService`, API client, RESX strings |
| `LinkNest.Web` | ASP.NET Core Blazor host (`src/LinkNest.Web/LinkNest.Web/`) — YARP proxy, form login |
| `LinkNest.Web.Client` | Interactive pages (Home, Category, Archive, Groups, Share, Login, Register, Settings, …) |
| `LinkNest.Mobile` | MAUI Blazor Hybrid (Windows; Android deferred — see E9 H3) |
| `LinkNest.Tests` | Unit and integration tests |
| `LinkNest.E2E.Tests` | Playwright end-to-end tests |

## What this validates

- **Blazor Web App (Auto)** with interactive WebAssembly components
- **Shared library** for models, services, and localization
- **PostgreSQL** persistence via EF Core and a standalone API
- **Bilingual EN/AR** with RTL support
- **Soft delete** with 7-day archive and restore
- **Link previews** via server-side Open Graph metadata fetch

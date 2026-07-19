# E1 — API + PostgreSQL Foundation

## Goal

Stand up `ManageFamilyMeals.Api` (ASP.NET Core Web API) backed by PostgreSQL via EF Core, and migrate the existing Blazor Web Client off client-side JSON/localStorage (`IAppDataStore`) onto this API. No ownership, auth, or groups yet — this epic only replaces the storage layer under the app that already exists (v1 scope: US-01–US-12).

## Depends On

None. This is the first epic.

## In Scope (PRDV2 references)

- FR-24, FR-25, FR-26, FR-27
- Validation Checklist #10, #11

## Acceptance Criteria

- New `ManageFamilyMeals.Api` project exists, builds, and runs.
- EF Core `DbContext` configured with the Npgsql provider; an initial migration creates tables for `MealCategory`, `MealLink`, `AppSettings` mirroring the current v1 shape (no `OwnerType`/`OwnerUserId`/`OwnerGroupId`/`RowVersion` columns yet — those belong to E3).
- API exposes endpoints covering every operation `IAppDataStore`/`IMealDataService` currently perform locally: list/create/archive/restore categories and links, toggle favorite, the 7-day purge-on-load sweep, link preview fetch, and settings (culture) read/write.
- `ManageFamilyMeals.Web.Client` is updated to call the API over HTTP instead of reading/writing local storage. No client persists data locally as the system of record.
- All existing v1 features (US-01 through US-12) continue to work end-to-end against PostgreSQL: create/archive category and link, favorites + sort-to-top, EN/AR switch + RTL, 7-day archive/restore, link preview card.
- Validation Checklist rows #10 and #11 flip from "Not started" to "Implemented."

## Out of Scope

- Authentication (endpoints are unauthenticated for now — added in E2).
- Ownership columns, groups, sharing.
- SSRF hardening of the link-preview fetch (that's FR-11, handled in E6).
- Mobile wiring (E7).

## Likely Files/Areas

- New `src/ManageFamilyMeals.Api/` project (Program.cs, DbContext, entity configs, controllers/minimal-API endpoints, `Npgsql.EntityFrameworkCore.PostgreSQL` package, initial migration).
- `src/ManageFamilyMeals.Shared/Services/` — `IAppDataStore`, `IMealDataService`, `MealDataService` likely need an API-backed implementation (or replacement) instead of the current local-storage-backed one.
- `src/ManageFamilyMeals.Web/ManageFamilyMeals.Web.Client/Services/` and `Program.cs` — register `HttpClient` pointed at the new API instead of local storage services.
- `appsettings*.json` (both Api and Web) — PostgreSQL connection string (use user secrets or local dev config; production hosting is an open question, not blocking this epic — target a local/dev PostgreSQL instance).

## Manual Test Notes (for your own verification after Agent A/B pass)

- Run the API locally against a local PostgreSQL instance, confirm the migration applies cleanly.
- Run the Web app, exercise every v1 flow (create category, add link, favorite, archive, restore, switch language), restart the API process, and confirm data survived (proves PostgreSQL is now the system of record, not localStorage).

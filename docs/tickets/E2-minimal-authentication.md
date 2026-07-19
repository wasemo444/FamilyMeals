# E2 — Minimal Authentication

## Goal

Add email/password authentication (ASP.NET Core Identity) so every user has an account. All existing data becomes owned by a single default/system user during migration so E1's flows keep working, but from this epic onward every API call is authenticated.

## Depends On

E1 (API + PostgreSQL must exist first).

## In Scope (PRDV2 references)

- US-13 (register/login), and the auth-related stories that assume a logged-in user
- FR-28, FR-29, FR-30
- §4 Approved Architecture Decisions — Auth mechanism: Email/password via ASP.NET Core Identity (resolved)
- §4 — Session/token mechanism: cookie + `PersistingAuthenticationStateProvider` for web (mobile bearer/JWT deferred to E7)
- Validation Checklist #12, #13

## Acceptance Criteria

- `ManageFamilyMeals.Api` uses ASP.NET Core Identity with a PostgreSQL-backed `IdentityDbContext` (or Identity tables added to the existing DbContext).
- Register and login endpoints exist; passwords are hashed via Identity's default hasher (no custom crypto).
- Web app issues an auth cookie on login; `PersistingAuthenticationStateProvider` (or equivalent) flows auth state from server to the WASM client so `AuthorizeView`/`[Authorize]` work in Blazor Auto render mode.
- All data endpoints from E1 now require authentication (`[Authorize]`), returning 401 when no valid session exists.
- A logout action clears the session.
- Existing pre-E2 data (if any was created during E1 testing) is associated with a seeded default user so nothing is orphaned when auth is turned on.
- Validation Checklist rows #12 and #13 flip to "Implemented."

## Out of Scope

- Ownership model beyond "every row belongs to a user" (`OwnerType`/`OwnerUserId`/`OwnerGroupId` columns and per-item ownership logic are E3).
- Groups, invites, group roles.
- Mobile/MAUI token-based auth (E7).
- Password reset, email confirmation, 2FA, external providers — not in PRDV2 scope.

## Likely Files/Areas

- `src/ManageFamilyMeals.Api/` — Identity setup in `Program.cs`, `ApplicationUser` entity, Identity DbContext config, register/login/logout endpoints, migration adding Identity tables.
- `src/ManageFamilyMeals.Web/` (server host) — cookie auth configuration, `PersistingAuthenticationStateProvider`, login/register Razor pages or components.
- `src/ManageFamilyMeals.Web.Client/` — `AuthorizeView` usage, redirect-to-login handling, auth state consumption.
- `src/ManageFamilyMeals.Shared/` — any shared auth-state contracts/DTOs.

## Manual Test Notes

- Register a new user, log in, confirm the auth cookie is set and protected pages/API calls succeed.
- Log out, confirm protected API calls now return 401 and the UI redirects to login.
- Confirm data created in E1 testing is still visible after auth is enabled (i.e., nothing got silently dropped).

# E7 — Mobile (.NET MAUI Blazor Hybrid)

**Status: Implemented**

## Goal

Add the `LinkNest.Mobile` MAUI Blazor Hybrid app (planned since v1 but never built) reusing `LinkNest.Shared`, wired to the same API as the web app, including bearer/JWT auth for mobile clients. This is the last epic — it depends on the full web-side feature set being in place.

## Depends On

E6 (all core functionality and hardening should exist before duplicating the client onto a second platform).

## In Scope (PRDV2 references)

- §10 Solution Structure — `LinkNest.Mobile` project
- §4 — session/token mechanism for mobile: bearer/JWT (as opposed to web's cookie-based auth from E2)
- All prior user stories (US-01–US-20), now delivered on a second platform
- Validation Checklist — mobile-equivalents of the rows already covered for web (no new checklist rows introduced by PRDV2; this epic is "make the existing checklist true on mobile too")

## Acceptance Criteria

- New `LinkNest.Mobile` MAUI Blazor Hybrid project exists, referencing `LinkNest.Shared`, and builds/runs on at least one target platform (Android or Windows, whichever is easiest to verify locally).
- Mobile app authenticates against the same API using bearer/JWT tokens (separate from the web cookie flow added in E2); token is stored securely (platform secure storage, not plain file/prefs) and attached to API requests.
- All core flows work on mobile: login, view home (personal + group content), create/archive/favorite categories and links, view/restore archive, switch language/RTL, view group members, send/accept invites.
- Mobile reuses **`LinkNest.Web.Client`** pages via project reference today (H4: extract to `LinkNest.UI` RCL). Shared holds services/DTOs and bearer auth handlers, not routable UI.

## Out of Scope

- New features beyond parity with the web app.
- Push notifications, offline-first sync, app store deployment (all explicitly out of scope per PRD v1 §9 and not reopened in PRDV2).

## Likely Files/Areas

- New `src/LinkNest.Mobile/` project (MauiProgram.cs, platform folders, `wwwroot` shared with/mirroring the web client's static assets).
- `src/LinkNest.Shared/` — any service abstractions that assumed a browser (e.g., localStorage-based settings persistence, if still referenced) need a platform-agnostic interface with a MAUI-specific implementation (secure storage) alongside the existing web implementation.
- `src/LinkNest.Api/` — JWT bearer authentication scheme added alongside the existing cookie scheme (both active simultaneously, selected per-request via the auth scheme).

## Manual Test Notes

- Install/run the mobile app on an emulator or device, log in, and walk through create/favorite/archive/restore for both personal and group content.
- Kill and relaunch the app, confirm the stored token keeps the session logged in (or prompts to re-login gracefully if the token expired) rather than crashing.
- Switch language on mobile and confirm RTL renders correctly on that platform's rendering engine.

### Build prerequisites

```powershell
dotnet workload restore
dotnet build src/LinkNest.Mobile/LinkNest.Mobile.csproj -c Release -f net10.0-windows10.0.19041.0
```

### E7 manual test matrix (L4)

| # | Scenario | Expected |
|---|----------|----------|
| 1 | Login with dev credentials | Home loads personal + group content |
| 2 | Create / favorite / archive / restore (personal) | Same as web; bootstrap reloads |
| 3 | Shared group content + invites (Groups page) | Accept invite → shared filter shows content |
| 4 | Switch EN ↔ AR | RTL/LTR and persisted culture |
| 5 | Kill and relaunch app | Session restored from JWT or login prompt if expired |
| 6 | Expired / invalid token | 401 → local clear → login screen (no crash) |
| 7 | API unreachable at startup | Login screen; token not wiped (retry when API up) |

Full deferred work: [E9 § E7 Mobile follow-ups](E9-carry-over-from-previous-epic.md#e7-mobile--deferred-follow-ups-carry-over-from-architect-review).

### API URL configuration (physical devices / emulators)

Override the default API URL via `src/LinkNest.Mobile/appsettings.Development.json` or environment variable `LINKNEST_API_BASE_URL` (e.g. `http://192.168.x.x:5280/` for a physical device on the same LAN). Android emulator default when unset: `http://10.0.2.2:5280/`.

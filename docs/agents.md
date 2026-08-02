# Agent & Developer Entry Point

This file is the **first document** to read when entering the LinkNest codebase — whether you are an AI agent or a human engineer.

## What This System Is

LinkNest is a **Blazor Web App** with a **standalone ASP.NET Core REST API** and **PostgreSQL** persistence. Users save and organize links to any online content in categories, optionally share collections with groups, and browse content in English or Arabic.

## Solution Map

| Project | Path | Role |
|---------|------|------|
| **LinkNest.Shared** | `src/LinkNest.Shared/` | Domain models, service contracts, pure query/ownership rules, auth DTOs, localization |
| **LinkNest.Api** | `src/LinkNest.Api/` | System-of-record HTTP API: EF Core, Identity cookies, ownership enforcement, link preview |
| **LinkNest.Web** | `src/LinkNest.Web/LinkNest.Web/` | Blazor host: YARP proxy, browser login (`/account/*`), auth-state serialization |
| **LinkNest.Web.Client** | `src/LinkNest.Web/LinkNest.Web.Client/` | Interactive UI pages/components; HTTP client adapters to API |
| **LinkNest.Mobile** | `src/LinkNest.Mobile/` | MAUI Blazor Hybrid — JWT bearer auth, reuses Web.Client pages (H4: extract to `LinkNest.UI`) |
| **Tests** | `tests/` | Unit/integration (`LinkNest.Tests`) and Playwright E2E (`LinkNest.E2E.Tests`) |

## Architecture in One Diagram

```
Browser (web)                         MAUI (mobile)
  └─► Web host (:5084)                  └─► Api (:5280) direct HTTP
        ├─► /account/login|logout             Authorization: Bearer {jwt}
        ├─► Blazor → ApiContentDataService
        └─► /api/* (YARP + cookie forward)
              └─► Api (:5280) SmartAuth → cookie OR JWT
                    └─► ContentDataService → PostgreSQL
```

**Critical:** Content **data** always flows through the API. The **browser** uses cookies from the Web host (`CookieForwardingHandler`). **MAUI** uses JWT from `POST /api/auth/token` stored in SecureStorage (`BearerTokenHandler`).

## Layered Documentation

Read documents in this order based on task depth:

| Document | Depth | When to Use |
|----------|-------|-------------|
| **[L1.md](L1.md)** | Surface | First orientation: what the system does, entry points, module map |
| **[L2.md](L2.md)** | Mid | Implementing features: data flow, auth, ownership, DI, API surface, Blazor patterns |
| **[L3.md](L3.md)** | Deep | Debugging a specific class, edge cases, cross-references to XML doc comments |

**Product requirements:** [PRDV2.md](PRDV2.md)  
**Production hosting & SMTP:** [deployment.md](deployment.md)  
**Epic tickets:** [docs/tickets/](tickets/) (E1–E10)

## Run Order (Local Dev)

1. `docker compose up -d` — PostgreSQL on `localhost:55432`
2. `dotnet run` in `src/LinkNest.Api` — **http://localhost:5280**
3. `dotnet run` in `src/LinkNest.Web/LinkNest.Web` — **http://localhost:5084**

Default dev user: `dev@linknest.local` / `DevPassword1!`

**Email (local):** By default, confirmation/reset links are **logged to the API console** — not sent. Set `Email__UseSmtp=true` + Brevo SMTP vars on the API, or use `--launch-profile http-smtp`. See [deployment.md](deployment.md#local-dev-smtp-brevo).

**Mobile (E7):** After `dotnet workload restore`, run API then `dotnet run --project src/LinkNest.Mobile -f net10.0-windows10.0.19041.0`. See [README](../README.md#run-the-mobile-app-e7--windows).

## Where to Edit What

| Change | Location |
|--------|----------|
| UI page or component | `Web.Client/Pages/`, `Web.Client/Components/` (until E9 H4: `LinkNest.UI/` — edits there affect both web and mobile) |
| HTTP API route | `Api/Endpoints/` |
| Group membership (invites, leave, remove) | `Api/Endpoints/GroupMembershipEndpoints.cs`, `Api/Services/GroupMembershipService.cs` |
| Group members UI | `Web.Client/Pages/GroupMembers.razor` |
| Domain rules (shared) | `Shared/Services/ContentDataQueries.cs`, `OwnershipRules.cs` |
| Persistence / scoping | `Api/Data/EfAppDataStore.cs` |
| Browser form login/logout | `Web/Endpoints/AccountEndpoints.cs` |
| JSON auth API | `Api/Endpoints/AuthEndpoints.cs` |
| JWT / SmartAuth (mobile bearer) | `Api/Identity/IdentityServiceExtensions.cs`, `JwtTokenService.cs`, `ConfigureJwtOptions.cs` |
| SMTP / transactional email | `Api/Identity/SmtpEmailSender.cs`, `EmailOptions.cs`, `EmailStartupDiagnostics.cs` |
| Password reset / confirmation | `Api/Identity/PasswordResetService.cs`, `EmailConfirmationService.cs`, `AuthEndpoints.cs` |
| Mobile bootstrap & token storage | `Mobile/MauiProgram.cs`, `Mobile/Services/MauiSecureTokenStore.cs`, `Shared/Auth/BearerTokenHandler.cs` |
| Reverse proxy / cookie forward | `Web/ReverseProxy/` |
| EF entities / migrations | `Api/Data/` |

## Non-Negotiable Rules for Agents

1. **Do not bypass the Web proxy** in production paths — the browser calls same-origin `/api/*`, not `:5280` directly.
2. **Ownership rules live in Shared** — `OwnershipRules` and `ContentDataQueries` are the single source of filter/mutate logic. Do not duplicate in UI.
3. **Unauthorized resource access returns 404** (not 403) on category/link mutations — intentional obfuscation.
4. **DataProtection keys must match** between Api and Web (`DataProtection:KeysPath`).
5. **Do not register `LocalStorageAppDataStore`** — legacy v1 client storage; not wired in DI.
6. **Links inherit category ownership** on create and when `CategoryId` changes in `EfAppDataStore`.
7. **RowVersion is required** for updates; stale tokens → `ConcurrencyConflictException` → HTTP 409.
8. **Group member cap is 10** (`GroupPolicy.MaxMembers`) — invite and accept reject when full; no waitlist.
9. **Group invites require a registered, email-confirmed account** — API returns structured 400 codes (`invitee_not_found`, `invitee_email_unconfirmed`); Web maps codes to localized messages via `ApiBadRequestException`.
10. **Removed/departed members' group-owned content stays with the group** — do not delete or reassign on leave/remove.
11. **Mobile uses bearer JWT, not cookies** — do not call `POST /api/auth/logout` from MAUI for session end; clear `ISecureTokenStore` locally. `/api/auth/logout` is cookie-only.
12. **JWT secret must not be committed** — use `ConfigureJwtOptions` dev fallback or env/`Jwt__Secret` in non-dev.
13. **Stop Api/Web processes before rebuilding** to avoid DLL file locks on Windows.
14. **MAUI workload installs must be serial** — parallel `dotnet workload install` on Windows causes MSI `0x652` failures.

## XML Documentation (Phase 1)

Public types in Shared, Api, and Web projects have `///` XML comments. Enable IntelliSense via `<GenerateDocumentationFile>true</GenerateDocumentationFile>` in each `.csproj`. Cross-reference type names in L3.md to jump to source.

## Test Commands

```powershell
# Unit/integration (use Release if Api is running in Debug)
dotnet test tests/LinkNest.Tests/LinkNest.Tests.csproj -c Release

# E2E (Playwright)
dotnet test tests/LinkNest.E2E.Tests/LinkNest.E2E.Tests.csproj -c Release
```

## Common Agent Mistakes

| Mistake | Correct Approach |
|---------|-------------------|
| Fixing login in Api when browser uses Web form | Edit `AccountEndpoints` on Web host |
| Adding ownership checks only in endpoints | Enforce in `OwnershipRules` + `EfAppDataStore` |
| Expecting 403 on forbidden category edit | Expect **404** |
| Calling Api directly from Blazor without proxy | Use `HttpClient` named `"LinkNestApi"` (same-origin via Web) |
| Editing migration after apply | Create a new migration instead |
| Allowing a user in multiple groups | Supported — users may create/join multiple groups; cap is per-group (10 members) |
| Using invite code for join in E5 | Email invite flow only; invite code field exists but is not the join path |
| Calling `/api/auth/logout` from MAUI | Clear local JWT via `ISecureTokenStore`; cookie logout does not invalidate bearer tokens |
| Referencing Web.Client from Mobile long-term | Deferred H4 — extract pages to `LinkNest.UI` RCL |

## Documentation Maintenance

When adding a new public class to Shared, Api, or Web:

1. Add XML `///` summary (and param/returns as needed)
2. Update **L3.md** if the class is Tier 1–3 (see L3 class index)
3. Update **L2.md** if a new subsystem or flow is introduced

# Agent & Developer Entry Point

This file is the **first document** to read when entering the ManageFamilyMeals codebase — whether you are an AI agent or a human engineer.

## What This System Is

ManageFamilyMeals is a **Blazor Web App** with a **standalone ASP.NET Core REST API** and **PostgreSQL** persistence. Users organize meal recipe links into categories, optionally share categories with family groups, and browse content in English or Arabic.

## Solution Map

| Project | Path | Role |
|---------|------|------|
| **ManageFamilyMeals.Shared** | `src/ManageFamilyMeals.Shared/` | Domain models, service contracts, pure query/ownership rules, auth DTOs, localization |
| **ManageFamilyMeals.Api** | `src/ManageFamilyMeals.Api/` | System-of-record HTTP API: EF Core, Identity cookies, ownership enforcement, link preview |
| **ManageFamilyMeals.Web** | `src/ManageFamilyMeals.Web/ManageFamilyMeals.Web/` | Blazor host: YARP proxy, browser login (`/account/*`), auth-state serialization |
| **ManageFamilyMeals.Web.Client** | `src/ManageFamilyMeals.Web/ManageFamilyMeals.Web.Client/` | Interactive UI pages/components; HTTP client adapters to API |
| **Tests** | `tests/` | Unit/integration (`ManageFamilyMeals.Tests`) and Playwright E2E (`ManageFamilyMeals.E2E.Tests`) |

## Architecture in One Diagram

```
Browser
  └─► Web host (:5084)
        ├─► /account/login|logout  → Web Identity (cookie on Web origin)
        ├─► Blazor pages/components  → ApiMealDataService
        └─► /api/* (YARP proxy)      → Api host (:5280)
              └─► MealDataService → EfAppDataStore → PostgreSQL
```

**Critical:** Meal **data** always flows through the API. Identity cookies for the browser are issued by the **Web** host via form login, then forwarded to the API by `CookieForwardingHandler`.

## Layered Documentation

Read documents in this order based on task depth:

| Document | Depth | When to Use |
|----------|-------|-------------|
| **[L1.md](L1.md)** | Surface | First orientation: what the system does, entry points, module map |
| **[L2.md](L2.md)** | Mid | Implementing features: data flow, auth, ownership, DI, API surface, Blazor patterns |
| **[L3.md](L3.md)** | Deep | Debugging a specific class, edge cases, cross-references to XML doc comments |

**Product requirements:** [PRDV2.md](PRDV2.md)  
**Epic tickets:** [docs/tickets/](tickets/) (E1–E9)

## Run Order (Local Dev)

1. `docker compose up -d` — PostgreSQL on `localhost:55432`
2. `dotnet run` in `src/ManageFamilyMeals.Api` — **http://localhost:5280**
3. `dotnet run` in `src/ManageFamilyMeals.Web/ManageFamilyMeals.Web` — **http://localhost:5084**

Default dev user: `dev@mfm.local` / `DevPassword1!`

## Where to Edit What

| Change | Location |
|--------|----------|
| UI page or component | `Web.Client/Pages/`, `Web.Client/Components/` |
| HTTP API route | `Api/Endpoints/` |
| Domain rules (shared) | `Shared/Services/MealDataQueries.cs`, `OwnershipRules.cs` |
| Persistence / scoping | `Api/Data/EfAppDataStore.cs` |
| Browser form login/logout | `Web/Endpoints/AccountEndpoints.cs` |
| JSON auth API | `Api/Endpoints/AuthEndpoints.cs` |
| Reverse proxy / cookie forward | `Web/ReverseProxy/` |
| EF entities / migrations | `Api/Data/` |

## Non-Negotiable Rules for Agents

1. **Do not bypass the Web proxy** in production paths — the browser calls same-origin `/api/*`, not `:5280` directly.
2. **Ownership rules live in Shared** — `OwnershipRules` and `MealDataQueries` are the single source of filter/mutate logic. Do not duplicate in UI.
3. **Unauthorized resource access returns 404** (not 403) on category/link mutations — intentional obfuscation.
4. **DataProtection keys must match** between Api and Web (`DataProtection:KeysPath`).
5. **Do not register `LocalStorageAppDataStore`** — legacy v1 client storage; not wired in DI.
6. **Links inherit category ownership** on create and when `CategoryId` changes in `EfAppDataStore`.
7. **RowVersion is required** for updates; stale tokens → `ConcurrencyConflictException` → HTTP 409.
8. **Stop Api/Web processes before rebuilding** to avoid DLL file locks on Windows.

## XML Documentation (Phase 1)

Public types in Shared, Api, and Web projects have `///` XML comments. Enable IntelliSense via `<GenerateDocumentationFile>true</GenerateDocumentationFile>` in each `.csproj`. Cross-reference type names in L3.md to jump to source.

## Test Commands

```powershell
# Unit/integration (use Release if Api is running in Debug)
dotnet test tests/ManageFamilyMeals.Tests/ManageFamilyMeals.Tests.csproj -c Release

# E2E (Playwright)
dotnet test tests/ManageFamilyMeals.E2E.Tests/ManageFamilyMeals.E2E.Tests.csproj -c Release
```

## Common Agent Mistakes

| Mistake | Correct Approach |
|---------|-------------------|
| Fixing login in Api when browser uses Web form | Edit `AccountEndpoints` on Web host |
| Adding ownership checks only in endpoints | Enforce in `OwnershipRules` + `EfAppDataStore` |
| Expecting 403 on forbidden category edit | Expect **404** |
| Calling Api directly from Blazor without proxy | Use `HttpClient` named `"MealDataApi"` (same-origin via Web) |
| Editing migration after apply | Create a new migration instead |

## Documentation Maintenance

When adding a new public class to Shared, Api, or Web:

1. Add XML `///` summary (and param/returns as needed)
2. Update **L3.md** if the class is Tier 1–3 (see L3 class index)
3. Update **L2.md** if a new subsystem or flow is introduced

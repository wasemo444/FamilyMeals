# E10 — Static Web (Cloudflare Pages) + API (Render)

**Status: In progress** — Phase 0 complete; Phase 1 complete; **Phase 2 complete**; Phase 3 next.

> Epic file: `E10-static-web-cloud-run-api.md` (legacy filename). **Default Api host: [Render](https://render.com/) free web service** — no credit card on free tier. [Google Cloud Run](https://cloud.google.com/run) optional in H3-alt. **Koyeb** removed as default (Feb 2026: new accounts require paid Pro+ after Mistral acquisition).

## Goal

Deploy LinkNest using a **split, low-cost / free-tier** architecture:

| Layer | Platform | Role |
|-------|----------|------|
| **Frontend** | [Cloudflare Pages](https://pages.cloudflare.com/) | Static **Blazor WebAssembly** (`LinkNest.Web.Client`) |
| **Backend** | **[Render](https://render.com/)** (default) | Docker **LinkNest.Api** only (`Dockerfile.api`) |
| **Database** | [Neon](https://neon.tech/) (existing) | PostgreSQL — app data |
| **Email** | [Brevo](https://www.brevo.com/) (existing) | SMTP for confirm/reset (Api only) |

Web clients authenticate with **JWT bearer tokens** (same model as MAUI mobile today), not cookie auth through a Web host. This removes the need for:

- `LinkNest.Web` server in production
- YARP reverse proxy
- Shared Data Protection keys between two hosts

Mobile (Android/iOS) continues to call the **same public Api URL** (Render) with JWT — no separate backend.

## Depends On

- **E2** — Identity, JWT (`POST /api/auth/token`), register/login endpoints
- **E7** — Mobile bearer auth pipeline — **reuse for static web** via shared bearer registration
- **E9 (partial)** — SMTP, password reset, resend confirmation (must work in production Api)
- **Neon migrations applied** — including `DataProtectionKeys` table (mandatory for Api-only; see H2)

## Background / Motivation

Current production path (E9 § Production Hosting) deploys **Api + Web** together (Docker Compose, Fly.io, VM) because the browser uses **cookie auth** and **Interactive Server** Blazor, requiring shared Data Protection keys and a Web host with YARP.

Constraints driving E10:

- Oracle Cloud Free signup blocked (user waiting on support ticket)
- Fly.io is not permanently free after trial
- **Koyeb** no longer viable for new free hobby deploys (Mistral acquisition; paid plans only)
- User wants **$0 hosting without a GCP billing card**
- **Render free web service** — Docker, no card, 750 instance-hours/month
- **Cloudflare Pages** — free static hosting + HTTPS
- Mobile store roadmap (E9 H3/H4) benefits from **one public Api URL** and JWT everywhere

## Target Architecture

```
                    ┌─────────────────────────┐
  Browser / PWA     │  Cloudflare Pages       │
  (Blazor WASM)     │  https://app.pages.dev  │
        │           └───────────┬─────────────┘
        │  HTTPS + CORS         │
        │  Authorization:       │
        │  Bearer <JWT>         ▼
        │           ┌─────────────────────────┐
        └──────────►│  Render Web Service     │
  MAUI Android/iOS  │  LinkNest.Api (Docker)  │
  (same JWT flow)   │  https://xxx.onrender.com│
                    └───────────┬─────────────┘
                                │
                    ┌───────────▼─────────────┐
                    │  Neon PostgreSQL        │
                    │  Brevo SMTP (Api)       │
                    └─────────────────────────┘
```

### Auth flow (production web)

1. User opens Cloudflare Pages URL → WASM loads
2. Login form calls `POST /api/auth/token` on Render Api (CORS allowed)
3. Api returns JWT → stored in browser (`sessionStorage` via `BrowserSecureTokenStore`)
4. All API calls include `Authorization: Bearer …`
5. Register / forgot-password / reset use existing Api JSON endpoints
6. Email links point to **WASM routes** on Cloudflare — not `/account/*` on Web host

## In Scope

### H1 — Static web client (JWT bearer mode)

1. **Standalone WASM** — publish `LinkNest.Web.Client`; `ApiBaseUrl` → Render URL; SPA fallback on Cloudflare
2. **JWT auth** — `AddLinkNestBearerAuth()`, `BrowserSecureTokenStore`, remove cookie/SSR bridge in static profile
3. **WASM-only render modes** — `ConfigureStaticWebRenderModes()`; fix `ThemeSync.razor`
4. **Confirm email** — WASM `/confirm-email` + `POST /api/auth/confirm-email`
5. **Logout** — client-side JWT clear

### H2 — Api changes for split deployment

1. **CORS** — dev cookie vs prod `StaticWeb` (`Cors__AllowedOrigins`, no credentials)
2. **Data Protection** — **mandatory** `DataProtection__Storage=Database` on Render (Neon)
3. **Confirm email Api endpoint** + updated email link templates
4. **Health check** — `GET /health` (Render health check path when configured)
5. **Environment variables** — see deployment.md D2 table and **`Auth__WebBaseUrl`** below

### H2.1 — `Auth__WebBaseUrl` (email links)

The Api uses **`Auth__WebBaseUrl`** (maps to `Auth:WebBaseUrl`) as the **public website base URL** embedded in outbound email links (confirm email, reset password). It is **not** the same as the Render Api URL used for `POST /api/auth/token`.

| Setting | Purpose | Example |
|---------|---------|---------|
| Render service URL | Api host — JWT, register, CRUD | `https://linknest-api.onrender.com` |
| **`Auth__WebBaseUrl`** | Frontend host — links **inside emails** | `https://linknest.pages.dev/` |

**Format:** HTTPS URL with a **trailing slash** (e.g. `https://your-app.pages.dev/`).

**How to get the value:**

| Phase | Where the frontend lives | Set `Auth__WebBaseUrl` to |
|-------|--------------------------|---------------------------|
| **Phase 0** (Api only, no Pages yet) | No public web app | Placeholder is fine for smoke tests. For register/login without email confirm, set `Auth__RequireConfirmedEmail=false` — links are unused. Do **not** use the Render Api URL expecting confirm links to work (Api has no `/account/confirm-email` page). |
| **Phase 0** (testing email send) | Not deployed | Use a placeholder with trailing slash, e.g. `https://localhost/` — links will 404 until Phase 3; verify Brevo delivery in logs/inbox only. |
| **Phase 3** (Cloudflare Pages live) | Cloudflare Pages | Your Pages URL from **Cloudflare Dashboard → Workers & Pages → your project → Visit** — e.g. `https://linknest.pages.dev/` or custom domain `https://app.yourdomain.com/` |
| **Custom domain** (optional) | Cloudflare custom domain | The canonical HTTPS origin users open in the browser, e.g. `https://app.yourdomain.com/` |

After Phase 1–2 (E10 confirm-email), email templates should point to **`{WebBaseUrl}/confirm-email?…`** and **`{WebBaseUrl}/reset-password?…`** on Pages — not `/account/*` on a server host.

**Related Render env vars (Phase 0 minimum):**

| Variable | Phase 0–2 value | Phase 3 value (after Cloudflare Pages exists) |
|----------|-----------------|-----------------------------------------------|
| `Auth__WebBaseUrl` | Placeholder OK, or omit — **not needed until Pages** | Your Pages URL with trailing slash, e.g. `https://linknest.pages.dev/` |
| `Auth__AllowRegistration` | `true` (Production defaults to `false` → register returns **404**) | `true` (unchanged) |
| `Auth__RequireConfirmedEmail` | **`false`** — register/login via curl/MAUI without email confirm | **`true`** — once WASM `/confirm-email` works on Pages |
| `Cors__AllowedOrigins` | **Leave unset** — no browser frontend yet; curl/MAUI unaffected | Exact Pages origin(s), e.g. `https://linknest.pages.dev` (no trailing slash) |

> **Note — do not set CORS / WebBaseUrl / RequireConfirmedEmail until Phase 3.**  
> After Phase 0–1 deploy (Api only on Render), keep using curl or MAUI. Empty `Cors__AllowedOrigins` logs a startup warning but is expected. When Cloudflare Pages is live and Phase 2 WASM is deployed, add all three vars on Render — see [Phase 3 Render env update](#render-env-vars--add-after-phase-3).

### H3 — Render deployment artifacts (default)

1. **Render Web Service** — Docker, `Dockerfile.api`, **Free** instance type
2. **Port 8080** (matches `Dockerfile.api` / `ASPNETCORE_URLS`)
3. **GitHub** connect for auto-deploy on push (optional)
4. **Free tier limits:** 512 MB RAM; spins down after **15 min** idle; 750 instance-hours/month
5. **Optional:** `render.yaml` in repo root for reproducible deploy
6. **CI optional:** GitHub Actions → Render deploy hook

See [docs/deployment.md](../deployment.md) Path D2.

### H3-alt — Google Cloud Run (optional)

Card required; Always Free tier in US regions. Documented in deployment.md **D2-alt**.

### H4 — Cloudflare Pages deployment artifacts

Build-time `ApiBaseUrl` → Render service URL; `_redirects` for SPA routing.

### H5 — Documentation

Path D in `deployment.md`; L1/L2 static web auth path.

### H6 — Tests

CORS + JWT integration tests; cookie Web tests unchanged.

## Out of Scope

- Removing `LinkNest.Web` from repo (local dev)
- Render **two** free services (only Api needed — one slot)
- Koyeb (paid for new users)
- JWT refresh tokens

## Acceptance Criteria

### Static web (Cloudflare)

- [ ] WASM on Pages; JWT auth; full register/confirm/reset flows

### Api (Render)

- [ ] `Dockerfile.api` on Render free tier; Neon + Brevo + `DataProtection__Storage=Database`
- [x] CORS for Pages origin; `GET /health` when implemented
- [ ] Cold start after 15 min idle documented (~30–60 s)

### Mobile

- [ ] Same Render `ApiBaseUrl` — config only

### Dev / regression

- [ ] Local cookie Web + `dotnet test` unchanged

## Implementation Phases

### Phase overview

| Phase | Focus | Blocks | Code vs deploy |
|-------|--------|--------|----------------|
| **0** | Api on Render | — | Deploy + env vars (**done** when register/token work via curl) |
| **1** | Api hardening | Phase 3 browser calls | **Code** — Api only; redeploy Render |
| **2** | WASM + JWT client | Phase 3 | **Code** — `LinkNest.Web.Client`; test locally first |
| **3** | Cloudflare Pages E2E | — | **Deploy** — mostly config + publish script |
| **4** | CI (optional) | — | GitHub Actions; not blocking |

**Critical path:** Phase 1 → Phase 2 → Phase 3. Phase 4 can wait.

**Parallel (no code):** Point MAUI `ApiBaseUrl` at Render; sign up for Cloudflare Pages before Phase 3.

---

### Phase 0 — Api on Render (no web changes)

**Status:** Complete when all boxes checked (Render live, Neon migrated, register + JWT login via curl/MAUI).

**Goal:** Public Api on Render; mobile and curl can authenticate. Browser web app not required yet.

#### Checklist

- [x] Render web service live (`Dockerfile` at repo root — Render expects `./Dockerfile`; keep `Dockerfile.api` for Compose/Fly/CI)
- [x] Neon migrations applied (`DataProtectionKeys` included)
- [x] Render **Environment** vars set — see [H2.1](#h21--auth__webbaseurl-email-links):
  - [x] `PORT=8080`
  - [x] `DataProtection__Storage=Database`
  - [x] `ConnectionStrings__DefaultConnection` (Neon)
  - [x] `Jwt__Secret` (≥ 32 characters)
  - [x] `Auth__AllowRegistration=true` (Production default is `false` → register returns **404**)
  - [x] `Auth__WebBaseUrl` — placeholder OK for Phase 0; set to Pages URL in Phase 3
  - [x] `Auth__RequireConfirmedEmail=false` until Phase 3 (optional for smoke test)
  - [x] Brevo `Email__Smtp__*` including `Email__Smtp__FromAddress` (verified sender)
- [x] `POST /api/auth/register` then `POST /api/auth/token` works (curl / MAUI)

#### PowerShell smoke test (Windows)

Use `curl.exe` (not PowerShell `curl` alias) or `Invoke-RestMethod`:

```powershell
$API = 'https://YOUR-SERVICE.onrender.com'
# register → token (see deployment.md D3)
```

#### Known Phase 0 quirks (addressed in Phase 1)

| Issue | Phase 0 workaround | Phase 1 fix |
|-------|-------------------|-------------|
| Unknown email on `/api/auth/token` returns **500** | Register first; use real credentials | **401** when user not found |
| Register returns **404** | Set `Auth__AllowRegistration=true` | Documented in H2.1 |
| Email confirm links 404 on cookie Web | `RequireConfirmedEmail=false` | Web `GET /confirm-email` redirects to `/account/confirm-email` (Paths A–C); WASM `/confirm-email` in Phase 2 |

---

### Phase 1 — Api hardening

**Status:** Complete (code merged; redeploy Render to apply).

**Goal:** Api ready for browser clients on Cloudflare Pages — CORS, health probe, JSON confirm-email, updated email URLs. Redeploy Render after merge.

**Depends on:** Phase 0.

**Blocks:** Phase 3 (browser cannot call Api cross-origin until CORS ships).

#### Tasks

| # | Task | Details | Likely files |
|---|------|---------|--------------|
| 1.1 | **`GET /health`** | Returns `200` with simple JSON (e.g. `{ "status": "ok" }`). Optional `GET /health/ready` with DB ping. Set Render **Health Check Path** to `/health`. | `HealthEndpoints.cs`, `Program.cs` |
| 1.2 | **Configurable CORS** | Replace hardcoded localhost-only policy. Read `Cors__AllowedOrigins` (comma-separated). **Production (Path D):** allow listed Pages origins, **no credentials**, any header/method. **Development:** keep current cookie policy (`AllowCredentials`, localhost origins). | `Program.cs`, `CorsOptions` (new) |
| 1.3 | **`POST /api/auth/confirm-email`** | JSON body: `userId`, `code` (same tokens Identity generates today). Confirm via `UserManager.ConfirmEmailAsync`. Return `200` or validation errors. Mirror logic from `LinkNest.Web` `AccountEndpoints.ConfirmEmailAsync`. | `AuthEndpoints.cs` |
| 1.4 | **Email link templates** | Change `EmailConfirmationService.BuildConfirmationLink` from `{WebBaseUrl}/account/confirm-email?…` to `{WebBaseUrl}/confirm-email?…`. `PasswordResetService.BuildResetLink` already uses `/reset-password` — verify query params match WASM page. | `EmailConfirmationService.cs`, `PasswordResetService.cs` |
| 1.5 | **Token login bug** | `POST /api/auth/token` with unknown email must return **401**, not **500** (`EvaluateLoginEligibility` + null user). | `AuthEndpoints.cs` |
| 1.6 | **Tests** | Integration tests: CORS preflight from configured origin; confirm-email endpoint; health returns 200. | `LinkNest.Api.Tests` |

#### Render env vars (Phase 1 deploy — Api only, no Pages yet)

**Set now:**

| Variable / setting | Value |
|--------------------|--------|
| **Health Check Path** (Render dashboard) | `/health` |
| Phase 0 vars | unchanged (`PORT`, Neon, JWT, Brevo, `DataProtection__Storage`, etc.) |
| `Auth__RequireConfirmedEmail` | **`false`** (keep until Phase 3) |

**Do not set until Phase 3** (no Cloudflare Pages URL yet):

| Variable | Why wait |
|----------|----------|
| `Cors__AllowedOrigins` | Browser on Pages must call Api cross-origin — no frontend yet |
| `Auth__WebBaseUrl` | Email links must point at Pages — no Pages URL yet |

See [Render env vars — add after Phase 3](#render-env-vars--add-after-phase-3).

#### Phase 1 verification

```powershell
curl.exe -s "$API/health"
curl.exe -s -X OPTIONS "$API/api/auth/token" -H "Origin: https://your-app.pages.dev" -H "Access-Control-Request-Method: POST" -v
curl.exe -s -X POST "$API/api/auth/confirm-email" -H "Content-Type: application/json" -d "{\"userId\":\"...\",\"code\":\"...\"}"
```

- [x] `/health` → 200
- [x] CORS allows configured Pages origin (no credentials)
- [x] Confirm-email Api endpoint works
- [x] Email templates use `/confirm-email` (not `/account/confirm-email`)
- [x] Unknown email on token login → 401
- [x] `dotnet test` passes

---

### Phase 2 — Static WASM + JWT web client

**Status:** Complete (code merged; verify locally before Phase 3 deploy).

**Goal:** `LinkNest.Web.Client` runs as **standalone WASM** with **JWT bearer auth** (same model as MAUI), callable against Render Api locally before Cloudflare deploy.

**Depends on:** Phase 1 (CORS + confirm-email Api recommended before full auth testing).

**Blocks:** Phase 3 (Pages needs publishable JWT WASM).

#### Current state (before Phase 2)

| Area | Today (cookie / Web host) | Target (static Path D) |
|------|---------------------------|-------------------------|
| `Web.Client/Program.cs` | `AuthenticationStateDeserialization()` | JWT bearer registration |
| `WebClientAuthMode` | `UsesBearerToken = false` | Bearer mode or shared `AddLinkNestBearerAuth()` |
| Token storage | `WebSecureTokenStore` (cookie-oriented) | `BrowserSecureTokenStore` (`sessionStorage` + expiry) |
| Api calls | Via YARP / Web host proxy | Direct to Render `ApiBaseUrl` |
| Confirm email | Web host `/account/confirm-email` | WASM `/confirm-email` → `POST /api/auth/confirm-email` |
| `ThemeSync.razor` | `@rendermode InteractiveAuto` | WASM-compatible mode (see below) |

#### Tasks

| # | Task | Details | Likely files |
|---|------|---------|--------------|
| 2.1 | **Shared bearer registration** | Extract/refactor `AddLinkNestMobileBearerAuth()` into shared `AddLinkNestBearerAuth()`; Mobile keeps `MauiSecureTokenStore`, web uses `BrowserSecureTokenStore`. | `LinkNest.Shared` or `Web.Client`, `MobileServiceCollectionExtensions.cs` |
| 2.2 | **`BrowserSecureTokenStore`** | Persist JWT + expiry in `sessionStorage`; clear on logout. | `Web.Client/Services/` |
| 2.3 | **Static WASM host profile** | New DI entry (e.g. `AddLinkNestStaticWebClientServices`) — core client + bearer auth, **no** cookie auth. | `ClientServiceCollectionExtensions.cs`, `Program.cs` |
| 2.4 | **Remove SSR auth bridge** | Drop `AuthenticationStateDeserialization()` in static profile. | `Web.Client/Program.cs` |
| 2.5 | **WASM-only render modes** | `ConfigureStaticWebRenderModes()` — register `InteractiveWebAssemblyRenderMode(prerender: false)` only. | `InteractiveRenderSettings.cs`, host setup |
| 2.6 | **Fix `ThemeSync.razor`** | Replace hardcoded `InteractiveAuto` with static-compatible render mode (or make render mode configurable). | `ThemeSync.razor` |
| 2.7 | **WASM `/confirm-email` page** | Read `userId` + `code` from query string; call `POST /api/auth/confirm-email`; redirect to login with success/error. | New `Pages/ConfirmEmail.razor` (+ `.cs`) |
| 2.8 | **Login/register/logout** | Login via `POST /api/auth/token`; store JWT; attach `BearerTokenHandler` on `LinkNestApi` HttpClient; logout clears token client-side. | `Login.razor.cs`, auth client |
| 2.9 | **Build-time `ApiBaseUrl`** | `appsettings.json` / publish substitution → Render URL. | `Web.Client/wwwroot/appsettings.json`, publish script |
| 2.10 | **Publish script** | `scripts/publish-static-web.ps1` — `dotnet publish` WASM output to a folder for Pages upload. | `scripts/` |
| 2.11 | **SPA fallback file** | `_redirects` or `_routes.json` for client-side routing on Cloudflare. | `Web.Client/wwwroot/` |
| 2.12 | **Tests** | Bearer auth mode tests; existing cookie/Web host tests unchanged. | Test projects |

#### Local verification (before Phase 3)

1. Run Api locally (or point at Render) with CORS allowing `https://localhost:7xxx` or dev origin.
2. Publish/run WASM standalone with `ApiBaseUrl` set.
3. Manual: login → JWT in sessionStorage → home loads → logout clears token.
4. Register → confirm email flow (if RequireConfirmedEmail enabled against dev/staging Api).

#### Phase 2 checklist

- [x] Standalone WASM starts without `LinkNest.Web` server
- [x] JWT login/logout works against Render (or local Api)
- [x] `/confirm-email` WASM page + Api endpoint E2E
- [x] Forgot/reset password works (links hit WASM `/reset-password`)
- [x] EN/AR RTL unchanged
- [x] Cookie-based local Web host (`LinkNest.Web`) still works for dev
- [x] `dotnet test` passes

---

### Phase 3 — Cloudflare Pages end-to-end

**Status:** Not started.

**Goal:** Production static site on Cloudflare Pages; full auth and app flows against Render Api.

**Depends on:** Phase 1 (CORS, health, confirm-email) + Phase 2 (publishable WASM).

**Mostly deployment and config** — little new application code.

#### Tasks

| # | Task | Details |
|---|------|---------|
| 3.1 | **Cloudflare Pages project** | Connect GitHub repo **or** manual upload of publish output folder. |
| 3.2 | **Build / publish** | Run `scripts/publish-static-web.ps1` (or CI) with `ApiBaseUrl=https://YOUR-SERVICE.onrender.com`. Deploy `wwwroot` output. |
| 3.3 | **SPA routing** | Ensure `_redirects` / `_routes.json` serves `index.html` for deep links (`/login`, `/confirm-email`, etc.). |
| 3.4 | **Render env update** | Add vars below — [Render env vars — add after Phase 3](#render-env-vars--add-after-phase-3) |
| 3.5 | **Render health check** | Health Check Path = `/health` (set in Phase 1 if not already) |
| 3.6 | **Manual test matrix** | Run full checklist below. |

#### Render env vars — add after Phase 3

When Cloudflare Pages is live and WASM auth flows work (Phases 2–3 complete), update Render **Environment**:

| Variable | Example | Notes |
|----------|---------|--------|
| `Cors__AllowedOrigins` | `https://linknest.pages.dev` | Exact origin from **Cloudflare → Workers & Pages → your project → Visit**; comma-separate custom domains; **no trailing slash** |
| `Auth__WebBaseUrl` | `https://linknest.pages.dev/` | Same URL with **trailing slash** — used in Brevo email links |
| `Auth__RequireConfirmedEmail` | `true` | Only after `/confirm-email` on Pages works end-to-end |

```
[ ] Pages project created; public URL copied
[ ] Cors__AllowedOrigins set to Pages URL
[ ] Auth__WebBaseUrl set to Pages URL (trailing slash)
[ ] Auth__RequireConfirmedEmail=true
[ ] Redeploy Render; register → email → confirm → login works in browser
```

**Before Phase 3:** leave `Cors__AllowedOrigins` unset, `Auth__RequireConfirmedEmail=false`, and `Auth__WebBaseUrl` as placeholder or omit — curl/MAUI against Render Api still works.

#### Phase 3 checklist

```
[ ] Pages URL loads over HTTPS (e.g. https://linknest.pages.dev)
[ ] Login → JWT → home shows content
[ ] Register → Brevo email → link uses Pages domain (/confirm-email)
[ ] /confirm-email → POST /api/auth/confirm-email → login succeeds
[ ] Forgot password → email → /reset-password works
[ ] Logout clears session; protected routes redirect to login
[ ] EN/AR RTL
[ ] MAUI same Render ApiBaseUrl (unchanged)
[ ] GET /health on Render returns 200
[ ] CORS: no browser console errors on Api calls
```

#### Optional

- Custom domain on Cloudflare → add to `Cors__AllowedOrigins` and `Auth__WebBaseUrl`
- PWA / install prompt (out of scope unless requested)

---

### Phase 4 — CI (optional)

**Status:** Not started — **not blocking** hobby deploy.

**Goal:** Push to `main` auto-deploys Render Api and (optionally) Cloudflare Pages.

#### Tasks

| # | Task | Details |
|---|------|---------|
| 4.1 | **Api deploy** | GitHub Actions: build `Dockerfile.api` / push / Render deploy hook **or** rely on Render GitHub auto-deploy (already available). |
| 4.2 | **Pages deploy** | GitHub Actions: `dotnet publish` WASM → Cloudflare Pages action (Wrangler or Direct Upload). |
| 4.3 | **Secrets** | Store Neon/JWT/Brevo in GitHub Secrets only if needed for build; Render/Cloudflare env vars stay in dashboards. |
| 4.4 | **PR checks** | Extend existing CI — WASM publish smoke, Api tests, no regression on cookie Web path. |

#### Phase 4 checklist

- [ ] Push to `main` deploys Api (Render auto-deploy or workflow)
- [ ] Push to `main` deploys Pages (optional workflow)
- [ ] Failed deploy visible in GitHub Actions / Render / Cloudflare dashboards

---

## Manual Test Matrix

| # | Scenario | Phase | Expected |
|---|----------|-------|----------|
| 1 | Open Pages URL | 3 | Login page over HTTPS |
| 2 | JWT login | 2–3 | Home shows content |
| 3 | Register + confirm email | 1–3 | Email link on Pages domain; confirm → login |
| 4 | Forgot / reset password | 2–3 | Email → WASM reset page |
| 5 | Logout | 2–3 | Token cleared; redirect login |
| 6 | EN/AR RTL | 2–3 | Layout and text correct |
| 7 | curl register + token | 0 | JWT returned |
| 8 | Unknown email token login | 1 | **401**, not 500 |
| 9 | CORS from Pages origin | 1–3 | No browser CORS errors |
| 10 | `GET /health` | 1 | 200 |
| 11 | Render cold start after 15 min idle | 0+ | First request succeeds (~30–60 s) |
| 12 | MAUI same Api URL | 0+ | Unchanged |

## Cost Expectations (Hobby)

| Service | Expected cost |
|---------|----------------|
| Cloudflare Pages | $0 |
| **Render** | **$0** (free web service; no card; 750 hrs/mo) |
| Neon | $0 |
| Brevo | $0 |

## Risks & Mitigations

| Risk | Mitigation |
|------|------------|
| Render sleep after 15 min idle | Document cold start; hobby traffic only |
| 750 hrs/month cap | One Api service; spins down when idle saves hours |
| 512 MB RAM | Monitor logs; upgrade to Starter ($7) if OOM |
| JWT XSS | HTTPS; sessionStorage |
| Data Protection | `Database` mode on Neon |

## Relationship to E9

E10 adds JWT + static web + Render Api alongside E9 cookie/Docker paths.

## Architect Review

### Initial architecture (2026-08-02)

**Status:** Approved — Cloudflare WASM + Api-only + JWT + Neon.

**Api host history:** Cloud Run → Koyeb → **Render** (Koyeb free tier ended for new signups post-Mistral).

### Phase 1 code review (2026-08-03)

**Verdict:** **Approve with changes** — all recommended changes applied.

| Topic | Outcome |
|-------|---------|
| CORS split (dev credentials vs prod origins) | Approved |
| `GET /health` / `GET /health/ready` | Approved — use `/health` for Render health check |
| `POST /api/auth/confirm-email` | Approved — rate-limited under existing `auth` limiter |
| Email links → `/confirm-email` | Approved — Web host redirect added for Paths A–C compatibility |
| Token login 500 → 401 | Approved |
| Empty `Cors__AllowedOrigins` in Production | Startup **warning** logged |
| Deactivated user confirm-email | Returns **401** |

**Unblocks:** Phase 2 (JWT WASM client) and Phase 3 (Cloudflare Pages).

**Deferred to Phase 2+:** extra CORS test coverage, JWT audience rename (`LinkNest.Clients`), WASM dev-server origin in dev CORS.

### Decisions (unchanged)

| Topic | Decision |
|-------|----------|
| Confirm email | Option A — WASM + `POST /api/auth/confirm-email` |
| Data Protection | `Database` on Neon (**mandatory** on Render) |
| Client auth | `AddLinkNestBearerAuth()` + `BrowserSecureTokenStore` |
| CORS | Dev credentials vs prod no-credentials |

### Implementation checklist

- [x] `POST /api/auth/confirm-email` + WASM `/confirm-email`
- [x] `BrowserSecureTokenStore` + `AddLinkNestBearerAuth()`
- [x] `ConfigureStaticWebRenderModes()` + `ThemeSync.razor`
- [x] `GET /health`, split CORS, build-time `ApiBaseUrl`
- [ ] Render web service deploy documented (Path D2)
- [ ] `Auth__WebBaseUrl` documented — Pages URL after Phase 3; not Api URL

---

*Epic created: 2026-08-02. Api host default: **Render free**. Phase 0 complete (Render + curl/MAUI JWT). Phase 1 complete (Api hardening). Phase 2 complete (static WASM + JWT web client). Next: **Phase 3** (Cloudflare Pages E2E).*

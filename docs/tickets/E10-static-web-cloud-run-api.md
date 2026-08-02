# E10 — Static Web (Cloudflare Pages) + API (Render)

**Status: Proposed**

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
5. **Environment variables** — see deployment.md D2 table

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
- [ ] CORS for Pages origin; `GET /health` when implemented
- [ ] Cold start after 15 min idle documented (~30–60 s)

### Mobile

- [ ] Same Render `ApiBaseUrl` — config only

### Dev / regression

- [ ] Local cookie Web + `dotnet test` unchanged

## Implementation Phases

### Phase 0 — Api on Render (no web changes)

- [ ] Render web service live
- [ ] `POST /api/auth/token` works (curl / MAUI)

### Phase 1 — Api hardening

- [ ] CORS, `/health`, confirm-email Api

### Phase 2 — Static WASM + JWT web client

### Phase 3 — Cloudflare Pages end-to-end

### Phase 4 — CI (optional)

## Manual Test Matrix

| # | Scenario | Expected |
|---|----------|----------|
| 11 | Render cold start after 15 min idle | First request succeeds (~30–60 s) |
| 12 | MAUI same Api URL | Unchanged |

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

**Status:** Reviewed 2026-08-02 — architecture approved.

**Api host history:** Cloud Run → Koyeb (2026-08-02) → **Render** (2026-08-02, Koyeb free tier ended for new signups post-Mistral). Same code changes (JWT, CORS, confirm-email, Data Protection Database mode) apply regardless of PaaS host.

### Decisions (unchanged)

| Topic | Decision |
|-------|----------|
| Confirm email | Option A — WASM + `POST /api/auth/confirm-email` |
| Data Protection | `Database` on Neon (**mandatory** on Render) |
| Client auth | `AddLinkNestBearerAuth()` + `BrowserSecureTokenStore` |
| CORS | Dev credentials vs prod no-credentials |

### Implementation checklist

- [ ] `POST /api/auth/confirm-email` + WASM `/confirm-email`
- [ ] `BrowserSecureTokenStore` + `AddLinkNestBearerAuth()`
- [ ] `ConfigureStaticWebRenderModes()` + `ThemeSync.razor`
- [ ] `GET /health`, split CORS, build-time `ApiBaseUrl`
- [ ] Render web service deploy documented (Path D2)

---

*Epic created: 2026-08-02. Api host default: **Render free** (updated 2026-08-02). Ready for Phase 0 when Neon + Brevo are configured.*

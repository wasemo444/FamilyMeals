# LinkNest — Production deployment

This guide covers hosting LinkNest on free/low-cost infrastructure, configuring SMTP, Docker deployment, CI/CD, and store publishing options. Production paths include Docker (A/B), Fly.io (C), and **proposed** Cloudflare Pages + **Render** Api (D) — see [Architecture](#architecture) and [Path D](#path-d--cloudflare-pages--render-static-web--api-only).

## Architecture

| Component | Role |
|-----------|------|
| **PostgreSQL** | System of record (use Neon, Supabase, or self-hosted) |
| **LinkNest.Api** | REST API, Identity, JWT, outbound email |
| **LinkNest.Web** | Blazor UI, cookie auth, YARP proxy to `/api/*` (Paths A–C) |
| **LinkNest.Web.Client** | Blazor WebAssembly static site (Path D only) |

### Deployment paths

| Path | Stack | Auth model | Best for |
|------|-------|------------|----------|
| **A** | Docker Compose on your PC | Cookie + YARP | Validate images before a server |
| **B** | Docker Compose on a Linux VM | Cookie + YARP | Full control, Oracle/Hetzner |
| **C** | Fly.io (Api + Web apps) | Cookie + YARP | No VM; HTTPS on Fly edge |
| **D** | Cloudflare Pages + **Render** + Neon | **JWT bearer** (same as MAUI) | $0 hobby; no VM; **no card** on Render free |

Path D is **proposed** — see [E10 — Static Web + Render Api](tickets/E10-static-web-cloud-run-api.md). Api-only Render deploy works today (Phase 0); full Cloudflare Pages cutover requires E10 implementation (JWT web client, CORS split, confirm-email Api endpoint).

### Data Protection keys

Paths A–C (cookie auth) require a shared key ring between **Api** and **Web** or auth cookies break.

| Deployment | Key storage | Config |
|------------|-------------|--------|
| **Docker Compose / VM** | Shared filesystem volume | `DataProtection__KeysPath=/keys` |
| **Fly.io (two apps)** | PostgreSQL (`DataProtectionKeys` table) | `DataProtection__Storage=Database` |
| **Render Api-only (Path D)** | PostgreSQL (`DataProtectionKeys` table) | `DataProtection__Storage=Database` (**mandatory**) |
| **Cloud Run Api-only (Path D alt.)** | PostgreSQL (`DataProtectionKeys` table) | `DataProtection__Storage=Database` (**mandatory**) |

Path D has **no Web host** and **no shared `/keys` volume** — Identity token encryption uses Neon only. The `/keys` directory in `Dockerfile.api` is for Docker Compose (Path A/B), not Render or Cloud Run.

Public URL for users:

- **Paths A–C:** **Web only**. The API can stay internal (`http://api:8080` in Docker, `http://linknest-api.internal:8080` on Fly.io).
- **Path D:** **Cloudflare Pages** (static WASM) for browsers; **Render** is the public Api URL for web and mobile (`Authorization: Bearer` JWT, not cookies).

---

## Quick start (Docker + external PostgreSQL)

### 1. Database (Neon — free tier)

1. Create a project at [neon.tech](https://neon.tech).
2. Copy the PostgreSQL connection string (URI format is fine).
3. Apply migrations once from your machine:

```powershell
$env:ConnectionStrings__DefaultConnection = '<paste-neon-connection-string-here>'
dotnet ef database update --project src/LinkNest.Api/LinkNest.Api.csproj
```

Use **single quotes** around the Neon URI so PowerShell does not treat `&` as a command separator.

LinkNest converts Neon/libpq URIs (`postgresql://...?sslmode=require`) to Npgsql format automatically. If you prefer, use ADO.NET format directly:

```powershell
$env:ConnectionStrings__DefaultConnection = 'Host=ep-xxx.neon.tech;Database=neondb;Username=...;Password=...;SSL Mode=Require'
```

### 2. SMTP (Brevo — free ~300 emails/day)

Brevo only gives you an **SMTP key** (password) and **SMTP login** (username). Host and port are fixed — you configure them in LinkNest, not in Brevo.

1. Sign up at [brevo.com](https://www.brevo.com).
2. **Verify a sender** under **Senders & IP** (domain or single email). This becomes your `FromAddress`.
3. Go to **Settings → SMTP & API → SMTP** tab:
   - Copy the **SMTP login** (username — may differ from your Brevo account email).
   - Click **Generate a new SMTP key** → copy the key (shown once).
4. Use these values:

| LinkNest setting | Brevo value |
|------------------|-------------|
| Host | `smtp-relay.brevo.com` (fixed) |
| Port | `587` (fixed) |
| Username | **SMTP login** from SMTP tab |
| Password | **SMTP key** (not your account password) |
| FromAddress | A **verified sender** from Senders & IP |
| UseStartTls | `true` |

Alternatives: SendGrid (100/day), Mailjet (200/day).

---

## Local dev SMTP (Brevo)

By default, **Development** logs emails to the API console (no inbox delivery). To test real Brevo email locally:

### Option A — environment variables (API terminal)

Set vars **before** `dotnet run` in the **API** terminal (not Web):

```powershell
$env:Email__UseSmtp = 'true'
$env:Email__Smtp__Host = 'smtp-relay.brevo.com'
$env:Email__Smtp__Port = '587'
$env:Email__Smtp__Username = 'YOUR_BREVO_SMTP_LOGIN'
$env:Email__Smtp__Password = 'YOUR_SMTP_KEY'
$env:Email__Smtp__FromAddress = 'your-verified-sender@email.com'
$env:Email__Smtp__FromName = 'LinkNest'
$env:Email__Smtp__UseStartTls = 'true'
$env:Auth__WebBaseUrl = 'http://localhost:5084/'

dotnet run --project src/LinkNest.Api/LinkNest.Api.csproj
```

### Option B — launch profile + user-secrets

Store secrets once, then use the `http-smtp` profile:

```powershell
dotnet user-secrets set "Email:Smtp:Username" "YOUR_BREVO_SMTP_LOGIN" --project src/LinkNest.Api/LinkNest.Api.csproj
dotnet user-secrets set "Email:Smtp:Password" "YOUR_SMTP_KEY" --project src/LinkNest.Api/LinkNest.Api.csproj
dotnet user-secrets set "Email:Smtp:FromAddress" "your-verified-sender@email.com" --project src/LinkNest.Api/LinkNest.Api.csproj

dotnet run --project src/LinkNest.Api/LinkNest.Api.csproj --launch-profile http-smtp
```

### Verify SMTP is active

On API startup you should see:

```text
Email config: UseSmtp=True, Environment=Development, Host=smtp-relay.brevo.com, Port=587, UsernameSet=True, FromAddress=..., PasswordSet=True, EffectiveMode=SMTP
Email delivery mode: SMTP (real email)
```

When sending:

```text
Sending email to user@example.com via SMTP host smtp-relay.brevo.com:587
Email delivered to user@example.com with subject Confirm your LinkNest account
```

If you see `DEV MODE — email NOT sent via SMTP`, SMTP is **not** enabled — check `Email__UseSmtp=true` and restart the API.

### Local dev without SMTP

Register or request password reset → copy the link from the **API** console log → paste in browser.

---

## Reset test users

To delete all users and re-test registration:

**Neon SQL Editor** — paste and run [scripts/reset-users.sql](../scripts/reset-users.sql), or:

```sql
BEGIN;
DELETE FROM meal_links;
DELETE FROM meal_categories;
DELETE FROM group_invites;
DELETE FROM group_memberships;
DELETE FROM groups;
DELETE FROM "AspNetUserTokens";
DELETE FROM "AspNetUserRoles";
DELETE FROM "AspNetUserLogins";
DELETE FROM "AspNetUserClaims";
DELETE FROM "AspNetUsers";
COMMIT;
```

**Local Docker Postgres:**

```powershell
Get-Content scripts/reset-users.sql | docker exec -i newdietapp-postgres-1 psql -U linknest -d linknest
```

---

## Where you are now

If you completed **§1 Database (Neon)** and **§2 SMTP (Brevo)** and tested locally, you already have:

| Done | What it means |
|------|----------------|
| Neon database | PostgreSQL in the cloud; migrations applied |
| Brevo SMTP | Confirmation/reset emails work |
| Local `dotnet run` | App logic verified on your PC |

**Production** means running LinkNest in **Docker on a server** that others can reach via **HTTPS** — not `dotnet run` on your laptop.

You still need:

| Step | What | Cost |
|------|------|------|
| A | A small Linux VM (or your own server) | $0 (Oracle Cloud Free) or ~$5/mo |
| B | A domain name (recommended) | ~$10–15/year (optional for first Docker test on localhost) |
| C | HTTPS in front of the app (Caddy/nginx) | Free (Let's Encrypt) |
| D | `.env` file + `docker compose up` on the server | — |

---

## Production quick start

### Path A — Test Docker on your PC first (recommended)

Confirms Docker builds work **before** you rent a server. Uses the same Neon DB and Brevo settings you already tested.

**1. Create `.env` from the template**

```powershell
cd C:\repos\GitHub\Test-ConsoleAPP\New DietApp
copy env.production.example .env
notepad .env
```

**2. Fill in `.env`** (reuse values from local testing):

```env
DATABASE_URL=postgresql://...your-neon-uri...?sslmode=require
WEB_BASE_URL=http://localhost:8080/
JWT_SECRET=paste-a-long-random-string-at-least-32-characters
WEB_PORT=8080
ALLOW_REGISTRATION=true
REQUIRE_CONFIRMED_EMAIL=true
SMTP_HOST=smtp-relay.brevo.com
SMTP_PORT=587
SMTP_USERNAME=your-brevo-smtp-login
SMTP_PASSWORD=your-brevo-smtp-key
SMTP_FROM_ADDRESS=your-verified-sender@email.com
SMTP_FROM_NAME=LinkNest
SMTP_USE_STARTTLS=true
```

| Variable | What to put |
|----------|-------------|
| `DATABASE_URL` | Same Neon connection string you used for `dotnet ef database update` |
| `WEB_BASE_URL` | `http://localhost:8080/` for this test (trailing slash matters) |
| `JWT_SECRET` | Random string ≥ 32 chars — [generate one](https://generate-secret.vercel.app/32) |
| `SMTP_*` | Same Brevo values that worked locally |

**3. Build and run Docker**

```powershell
docker compose -f docker-compose.prod.yml up -d --build
```

First run takes several minutes (builds Api + Web images).

**4. Verify**

```powershell
docker compose -f docker-compose.prod.yml ps
docker compose -f docker-compose.prod.yml logs api --tail 30
```

Look for `Email delivery mode: SMTP (real email)` in API logs.

- Open **http://localhost:8080** → login page
- Register → confirmation email should arrive (same Neon DB as local test)
- `/privacy` → privacy policy page

**5. Stop when done**

```powershell
docker compose -f docker-compose.prod.yml down
```

---

### Path B — Deploy to the internet (real production)

Do this after Path A succeeds, or skip Path A if you are comfortable on a server.

#### B1. Get a server

Pick one:

| Provider | Cost | Notes |
|----------|------|-------|
| [Oracle Cloud Free Tier](https://www.oracle.com/cloud/free/) | $0 | Always-free VM; best match for this guide |
| Hetzner / DigitalOcean | ~$4–6/mo | Simple, reliable |
| Any Linux VM | — | Ubuntu 22.04+ recommended |

Install **Docker** and **Docker Compose** on the VM.

#### B2. Get a domain (recommended)

Point an **A record** at your server's public IP, e.g. `app.yourdomain.com → 203.0.113.10`.

Without a domain you can use the raw IP, but **HTTPS and email links are awkward** — get a domain before sharing with friends.

#### B3. Copy the project to the server

```bash
git clone https://github.com/YOUR_USER/FamilyMeals.git
cd FamilyMeals   # or your repo folder name
copy env.production.example .env
nano .env
```

Fill `.env` — **change `WEB_BASE_URL` to your public HTTPS URL:**

```env
WEB_BASE_URL=https://app.yourdomain.com/
DATABASE_URL=postgresql://...neon...
JWT_SECRET=...
SMTP_USERNAME=...
SMTP_PASSWORD=...
SMTP_FROM_ADDRESS=...
ALLOW_REGISTRATION=true
```

Neon and Brevo values are the **same** as local testing. Only `WEB_BASE_URL` must become your real public URL (email links depend on it).

#### B4. Run Docker on the server

```bash
docker compose -f docker-compose.prod.yml up -d --build
```

Containers listen on port **8080** on the VM. Do **not** expose 8080 directly to the internet — put HTTPS in front (next step).

#### B5. Add HTTPS with Caddy (easiest)

On the VM, install [Caddy](https://caddyserver.com/docs/install), then create `/etc/caddy/Caddyfile`:

```text
app.yourdomain.com {
    reverse_proxy localhost:8080
}
```

```bash
sudo systemctl reload caddy
```

Caddy obtains a Let's Encrypt certificate automatically. Users open **https://app.yourdomain.com**.

#### B6. Verify production

- [ ] `https://app.yourdomain.com/` → login page (padlock in browser)
- [ ] Register with real email → confirmation arrives → link opens **your domain** (not localhost)
- [ ] Login works after confirm
- [ ] Forgot password → email → reset works
- [ ] `/privacy` loads

---

### What Docker runs

```
Internet → Caddy (:443 HTTPS)
              ↓
         Web container (:8080)  ← users hit this
              ↓ /api/*
         Api container (:8080, internal only)
              ↓
         Neon PostgreSQL (external)
         Brevo SMTP (external)
```

Both containers share a Docker volume `/keys` for auth cookies. Neon and Brevo stay external — same accounts you already configured.

---

### Path C — Fly.io (no VM, two Fly apps)

Use this when you want **HTTPS on Fly's edge** without renting a Linux VM. Api and Web run as **separate Fly apps**; Data Protection keys live in **Neon** (`DataProtectionKeys` table) instead of a shared volume.

#### Architecture on Fly.io

```
Internet → Fly HTTPS (linknest-web.fly.dev)
              ↓
         Web machine (:8080)
              ↓ /api/*
         linknest-api.internal:8080  (private Fly network)
              ↓
         Neon PostgreSQL (data + DataProtectionKeys)
         Brevo SMTP (Api only)
```

Config files: [fly.api.toml](../fly.api.toml) and [fly.web.toml](../fly.web.toml).

#### C1. Prerequisites

1. Install [flyctl](https://fly.io/docs/flyctl/install/) and sign in: `fly auth login`
2. **Neon** database with migrations applied — including the new `DataProtectionKeys` table:

```powershell
$env:ConnectionStrings__DefaultConnection = '<your-neon-uri>'
dotnet ef database update --project src/LinkNest.Api/LinkNest.Api.csproj
```

3. **Brevo** SMTP credentials (same as local/Docker testing)
4. A **JWT secret** (≥ 32 random characters)

#### C2. Create the two Fly apps

Pick a region near your users (example: `ams`). Use unique app names if these are taken:

```powershell
fly apps create linknest-api
fly apps create linknest-web
```

Edit `fly.api.toml` / `fly.web.toml` if you use different app names — Web's `ReverseProxy__ApiBaseAddress` must match the API app name (`http://<api-app-name>.internal:8080`).

#### C3. Set API secrets

Replace placeholders with your real values. Use single quotes around the Neon URI in PowerShell:

```powershell
fly secrets set --app linknest-api `
  ConnectionStrings__DefaultConnection='<neon-connection-string>' `
  Jwt__Secret='<random-32+-chars>' `
  Auth__WebBaseUrl='https://linknest-web.fly.dev/' `
  Auth__AllowRegistration='true' `
  Auth__RequireConfirmedEmail='true' `
  Email__Smtp__Host='smtp-relay.brevo.com' `
  Email__Smtp__Port='587' `
  Email__Smtp__Username='<brevo-smtp-login>' `
  Email__Smtp__Password='<brevo-smtp-key>' `
  Email__Smtp__FromAddress='<verified-sender@email.com>' `
  Email__Smtp__FromName='LinkNest' `
  Email__Smtp__UseStartTls='true'
```

`DataProtection__Storage=Database` is set in `fly.api.toml` (no `/keys` volume needed).

#### C4. Deploy API and make it internal-only

```powershell
fly deploy --config fly.api.toml --app linknest-api
```

After the first deploy, **remove public IPs** so the API is reachable only on Fly's private network:

```powershell
fly ips list --app linknest-api
fly ips release <public-ipv4-or-ipv6> --app linknest-api
```

Verify the API machine is running:

```powershell
fly status --app linknest-api
fly logs --app linknest-api
```

Look for `Email delivery mode: SMTP (real email)` in the logs.

#### C5. Set Web secrets and deploy

Set `Auth__WebBaseUrl` and `WebBaseUrl` to your **public Web URL** (Fly assigns `https://<app-name>.fly.dev` unless you added a custom domain):

```powershell
fly secrets set --app linknest-web `
  ConnectionStrings__DefaultConnection='<same-neon-connection-string>' `
  WebBaseUrl='https://linknest-web.fly.dev/' `
  Auth__WebBaseUrl='https://linknest-web.fly.dev/' `
  Auth__AllowRegistration='true' `
  Auth__RequireConfirmedEmail='true'

fly deploy --config fly.web.toml --app linknest-web
```

`ReverseProxy__ApiBaseAddress` and `DataProtection__Storage=Database` are in `fly.web.toml`.

Optional custom domain:

```powershell
fly certs add app.yourdomain.com --app linknest-web
```

Then update both `WebBaseUrl` / `Auth__WebBaseUrl` secrets (Api + Web) to `https://app.yourdomain.com/`.

#### C6. Test Fly.io deployment

Work through this checklist on the **public Web URL** (`https://linknest-web.fly.dev` or your custom domain):

```
[ ] fly status --app linknest-api   → machine running
[ ] fly status --app linknest-web   → machine running
[ ] fly logs --app linknest-api     → no startup exceptions; SMTP mode active
[ ] Open https://linknest-web.fly.dev/ → login page loads (padlock)
[ ] Register with a real email → confirmation arrives
[ ] Confirmation link uses your Fly/custom domain (not localhost)
[ ] Click confirm → login succeeds
[ ] Forgot password → email → reset works
[ ] /privacy loads
[ ] Logout → login again works (cookie + shared DB keys)
```

Quick smoke test from your machine:

```powershell
curl -I https://linknest-web.fly.dev/
fly logs --app linknest-web --tail
```

Register a test user, then confirm API calls succeed (Web proxies `/api/*` to the internal API):

```powershell
fly ssh console --app linknest-web -C "wget -qO- http://linknest-api.internal:8080/api/bootstrap 2>&1 | head -c 200"
```

Expect `401` or similar without a cookie — that proves Web can reach the internal API.

#### C7. Troubleshooting Fly.io

| Symptom | Likely cause | Fix |
|---------|--------------|-----|
| Login works on Web but `/api/*` returns 502 | Web cannot reach API | Check `ReverseProxy__ApiBaseAddress`; API app name must match `.internal` hostname |
| Login succeeds then immediately logged out | Data Protection keys not shared | Both apps need `DataProtection__Storage=Database` and the **same** Neon DB; run `dotnet ef database update` |
| Email links point to localhost | Wrong `Auth__WebBaseUrl` on **Api** | `fly secrets set Auth__WebBaseUrl='https://your-web-url/' --app linknest-api` |
| Cookie/auth issues behind HTTPS | Forwarded headers | Already enabled in Web for Production; ensure you open the `https://` URL |
| API reachable from the public internet | Public IP still allocated | `fly ips release` on linknest-api |
| `relation "DataProtectionKeys" does not exist` | Migration not applied | Run `dotnet ef database update` against Neon |
| Cold start delay (~5–10 s) | Free tier machines stop when idle | Normal on Fly free tier; first request wakes the machine |

#### C8. Cost notes

Fly.io free tier includes limited compute; machines may **auto-stop** when idle (`auto_stop_machines = 'stop'` in the toml files). Neon + Brevo free tiers are unchanged from Docker deployment.

---

### Path D — Cloudflare Pages + Render (static web + Api only)

**Status: Proposed** — see epic [E10 — Static Web + Render Api](tickets/E10-static-web-cloud-run-api.md).

Use this when you want **$0 hosting without a VM or GCP billing card**. Browsers load **Blazor WebAssembly** from Cloudflare Pages; **LinkNest.Api** runs alone on **[Render](https://render.com/)** (free web service — Docker, no credit card). Web and mobile authenticate with **JWT bearer tokens** (same as MAUI) — no `LinkNest.Web` server, no YARP, no shared cookie keys.

> **Why not Koyeb?** After Mistral acquired Koyeb (Feb 2026), new accounts require a paid Pro+ plan. Render free tier is the current default for Path D.

#### D0. When to choose Path D

| Situation | Path D fit |
|-----------|------------|
| Hobby / friends-only traffic | Yes — Cloudflare Pages + Render free tier |
| No Linux VM, no Fly.io, **no GCP card** | Yes — Render free does not require a card |
| Oracle Cloud Free signup blocked | Yes |
| One public Api URL for web + Android/iOS | Yes — JWT everywhere |
| Need Interactive Server / cookie auth in prod | No — use Path A/B/C until E10 ships |

**What you can do today (Phase 0):** Deploy Api to Render with [Dockerfile.api](../Dockerfile.api), connect Neon + Brevo, test `POST /api/auth/token` via curl or MAUI.

**What requires E10 before full cutover:** Static WASM on Cloudflare Pages, web JWT client, CORS, `POST /api/auth/confirm-email`, Pages `/confirm-email`, `GET /health`. Until then, browser register/confirm will not work end-to-end.

#### Architecture (Path D)

```
Browser / PWA          ┌─────────────────────────┐
(Blazor WASM)          │  Cloudflare Pages       │
       │               │  https://app.pages.dev  │
       │  HTTPS+CORS   └───────────┬─────────────┘
       │  Authorization:           │
       │  Bearer <JWT>             ▼
       │               ┌─────────────────────────┐
       └──────────────►│  Render Web Service     │
MAUI Android/iOS       │  LinkNest.Api (Docker)  │
(same JWT flow)        │  https://xxx.onrender.com│
                       └───────────┬─────────────┘
                                   │
                       ┌───────────▼─────────────┐
                       │  Neon PostgreSQL        │
                       │  Brevo SMTP (Api only)  │
                       └─────────────────────────┘
```

Confirm-email (after E10 Phase 1–2): Pages `/confirm-email?…` → WASM calls **`POST /api/auth/confirm-email`** on Render.

#### D1. Prerequisites

1. **[Render](https://render.com/) account** (free tier — no credit card required)
2. **Neon** with migrations applied — **including `DataProtectionKeys`**:

```powershell
$env:ConnectionStrings__DefaultConnection = '<your-neon-uri>'
dotnet ef database update --project src/LinkNest.Api/LinkNest.Api.csproj
```

3. **Brevo** SMTP credentials (same as Paths A–C)
4. **JWT secret** (≥ 32 random characters)
5. **GitHub repo** connected to Render (recommended) for auto-deploy
6. **E10 implementation** for Cloudflare Pages frontend (Phases 2–3)

#### D2. Deploy Api to Render

**Dashboard (recommended for first deploy)**

1. Sign in at [dashboard.render.com](https://dashboard.render.com/) → **New +** → **Web Service**
2. Connect **GitHub** → select this repository
3. **Name:** e.g. `linknest-api`
4. **Region:** choose nearest (e.g. Frankfurt, Oregon)
5. **Branch:** `main` (or your default)
6. **Runtime:** **Docker**
7. **Dockerfile path:** `Dockerfile.api` (repository root context)
8. **Instance type:** **Free** (512 MB RAM)
9. **Port:** `8080` (must match `ASPNETCORE_URLS` in `Dockerfile.api`)
10. **Health check path:** `/health` (after E10 Phase 1; leave blank until then)
11. **Environment variables** (add each in the Render dashboard):

| Variable | Value |
|----------|--------|
| `ASPNETCORE_ENVIRONMENT` | `Production` |
| `DataProtection__Storage` | `Database` |
| `ConnectionStrings__DefaultConnection` | Neon connection string |
| `Jwt__Secret` | Random string ≥ 32 chars |
| `Auth__WebBaseUrl` | `https://your-app.pages.dev/` (future Pages URL; trailing slash) |
| `Auth__AllowRegistration` | `true` |
| `Auth__RequireConfirmedEmail` | `true` |
| `Email__Smtp__Host` | `smtp-relay.brevo.com` |
| `Email__Smtp__Port` | `587` |
| `Email__Smtp__Username` | Brevo SMTP login |
| `Email__Smtp__Password` | Brevo SMTP key |
| `Email__Smtp__FromAddress` | Verified sender |
| `Email__Smtp__FromName` | `LinkNest` |
| `Email__Smtp__UseStartTls` | `true` |

After E10 Phase 1, add:

| Variable | Value |
|----------|--------|
| `Cors__AllowedOrigins` | `https://your-app.pages.dev,https://app.yourdomain.com` |

**Do not set** `DataProtection__KeysPath` or `ReverseProxy__*` on Render.

12. **Create Web Service** — first Docker build takes several minutes. Note the public URL, e.g. `https://linknest-api.onrender.com`.

**Optional — `render.yaml` (Infrastructure as Code)**

Add to repo root for reproducible deploys:

```yaml
services:
  - type: web
    name: linknest-api
    runtime: docker
    dockerfilePath: ./Dockerfile.api
    plan: free
    region: frankfurt
    healthCheckPath: /health
    envVars:
      - key: ASPNETCORE_ENVIRONMENT
        value: Production
      - key: DataProtection__Storage
        value: Database
      # Add remaining vars in Render dashboard or sync: false + secret refs
```

Connect the Blueprint in Render → **New +** → **Blueprint** after committing `render.yaml`.

#### D2-alt. Deploy Api to Google Cloud Run (optional)

Use this if you prefer GCP **Always Free** tier and accept a **billing account** (card required). Same env vars as D2; see [Cloud Run pricing](https://cloud.google.com/run/pricing).

```powershell
$PROJECT_ID = 'your-gcp-project-id'
$REGION = 'us-central1'
$IMAGE = "$REGION-docker.pkg.dev/$PROJECT_ID/linknest/linknest-api:latest"

gcloud config set project $PROJECT_ID
gcloud services enable run.googleapis.com artifactregistry.googleapis.com
gcloud auth configure-docker "$REGION-docker.pkg.dev"

docker build -f Dockerfile.api -t $IMAGE .
docker push $IMAGE

gcloud run deploy linknest-api `
  --image $IMAGE `
  --region $REGION `
  --port 8080 `
  --memory 1024Mi `
  --min-instances 0 `
  --allow-unauthenticated `
  --set-env-vars "ASPNETCORE_ENVIRONMENT=Production,DataProtection__Storage=Database,..."
```

Set `DataProtection__Storage=Database` only — not `KeysPath`. Prefer Secret Manager for passwords in production.

#### D3. Verify Api (Phase 0)

**Health check** — `GET /health` ships in E10 Phase 1; until then skip or probe a public route.

**JWT login:**

```powershell
$API = 'https://linknest-api.onrender.com'

curl -s -X POST "$API/api/auth/token" `
  -H "Content-Type: application/json" `
  -d '{"email":"you@example.com","password":"YourPassword"}'
```

Expect JSON with `accessToken` on success.

**Logs:** Render dashboard → your service → **Logs**. Look for `Email delivery mode: SMTP (real email)`.

**MAUI / mobile:** Set `ApiBaseUrl` / `LINKNEST_API_BASE_URL` to the same Render URL.

**Free tier note:** Render spins down after **15 minutes** without traffic. First request after idle may take **~30–60 seconds** while the container starts. Free tier includes **750 instance-hours/month** — enough for one hobby Api service.

#### D4. Cloudflare Pages (after E10 Phase 2–3)

1. Publish static WASM from `LinkNest.Web.Client`
2. Set build-time **`ApiBaseUrl`** to your **Render** service URL
3. Deploy `wwwroot` to Cloudflare Pages; configure SPA fallback (`_redirects` / `_routes.json`)
4. Set **`Cors__AllowedOrigins`** on Render to your Pages URL
5. Set **`Auth__WebBaseUrl`** on Render to Pages URL for email links

#### D5. Test checklist (Path D)

**Phase 0 — Api only (today):**

```
[ ] Neon migrations applied (including DataProtectionKeys)
[ ] Render web service deployed; public URL reachable
[ ] DataProtection__Storage=Database (not KeysPath)
[ ] POST /api/auth/token returns JWT
[ ] Render logs → SMTP mode active
[ ] MAUI or curl login against Render URL works
```

**Phase 3 — full stack (after E10):**

```
[ ] Cloudflare Pages loads login (HTTPS)
[ ] JWT login; home shows content
[ ] Register → email → Pages domain in links
[ ] /confirm-email → POST /api/auth/confirm-email → login
[ ] Forgot password / reset works
[ ] Logout; EN/AR RTL
[ ] MAUI same ApiBaseUrl
[ ] GET /health returns 200
```

#### D6. Troubleshooting Path D

| Symptom | Likely cause | Fix |
|---------|--------------|-----|
| CORS error in browser | E10 CORS not deployed | Set `Cors__AllowedOrigins` to exact Pages URL; redeploy Render |
| Api 500 on Identity | Missing `DataProtectionKeys` | `dotnet ef database update` on Neon |
| `KeysPath` set on Render | Wrong for Path D | Use `DataProtection__Storage=Database` only |
| Slow first request after idle | Render free tier sleep (15 min) | Normal; wait ~30–60 s and retry |
| Build fails on Render | Docker context / Dockerfile path | Confirm `Dockerfile.api` at repo root; check build logs |
| OOM / crash on startup | 512 MB tight for .NET | Check logs; upgrade to Starter ($7/mo) if needed |
| Email link 404 | E10 not shipped | Expected until Phase 1–2 |
| JWT in MAUI but not browser | WASM still cookie auth | Complete E10 Phase 2 |

#### D7. Cost expectations (Path D hobby)

| Service | Expected cost |
|---------|----------------|
| Cloudflare Pages | $0 |
| **Render** | **$0** (free web service; no card; 750 hrs/mo) |
| Neon | $0 |
| Brevo | $0 |
| Custom domain (optional) | ~$10–15/year |

**Cloud Run alternative (D2-alt):** $0 within GCP Always Free limits; **billing account required**.

---

### Production checklist (copy/paste)

```
[ ] Neon migrations applied (dotnet ef database update)
[ ] Brevo sender verified; SMTP tested locally or in Docker
[ ] .env created from env.production.example (never commit .env)
[ ] DATABASE_URL = Neon connection string
[ ] WEB_BASE_URL = public https://your-domain/  (use http://localhost:8080/ for local Docker test)
[ ] JWT_SECRET = random, ≥ 32 characters
[ ] SMTP_* = Brevo values
[ ] docker compose -f docker-compose.prod.yml up -d --build
[ ] docker compose -f docker-compose.prod.yml ps  → both api and web "Up" (not Restarting)
[ ] API logs show "Email delivery mode: SMTP (real email)"
[ ] HTTPS reverse proxy in front of port 8080 (skip for local Docker test)
[ ] Register → confirm email → login works on public URL
```

**Path D (Cloudflare + Render)** — see [D5](#d5-test-checklist-path-d) for full lists. Minimum before sharing:

```
[ ] Neon migrations applied (including DataProtectionKeys)
[ ] Render Api deployed; DataProtection__Storage=Database
[ ] JWT login works (curl or MAUI)
[ ] Auth__WebBaseUrl matches future Pages URL
[ ] E10 complete before browser register/confirm on Pages
```

### Troubleshooting Docker

**`ERR_EMPTY_RESPONSE` or containers show `Restarting`**

Check logs:

```powershell
docker compose -f docker-compose.prod.yml logs api --tail 40
docker compose -f docker-compose.prod.yml logs web --tail 40
```

| Log message | Fix |
|-------------|-----|
| `Email:Smtp:Host must be configured` (web) | Fixed in current code — rebuild images (`--build`). Web does not need SMTP; only Api sends email. |
| `Default dev seed password cannot be used in Production` (api) | Fixed — production Docker skips dev user seed. Rebuild images. |
| `Restarting (139)` | Container crashed — read full log trace above the restart line |
| Page loads but API calls fail | Ensure `WEB_BASE_URL` in `.env` matches the URL you open in the browser |

After code changes, always rebuild:

```powershell
docker compose -f docker-compose.prod.yml up -d --build
```

---

## Environment variables

### API and Web (shared)

| Variable | Required | Description |
|----------|----------|-------------|
| `ConnectionStrings__DefaultConnection` | Yes | PostgreSQL connection string |
| `DataProtection__Storage` | Fly.io, Path D | Set to `Database` — keys in PostgreSQL (**mandatory** on Render / Cloud Run) |
| `DataProtection__KeysPath` | Docker/VM prod (A/B) | Shared directory, e.g. `/keys` — **not used on Render or Cloud Run** |
| `Jwt__Secret` | Yes (prod) | ≥ 32 characters |
| `Auth__WebBaseUrl` | Yes (API) | Public web URL for email links |
| `Auth__AllowRegistration` | No | `true` / `false` (default prod: false) |
| `Auth__RequireConfirmedEmail` | No | Default `true` |

### SMTP (API only)

| Variable | Example | Notes |
|----------|---------|-------|
| `Email__UseSmtp` | `true` | **Local dev only** — send via SMTP in Development. Production uses SMTP automatically. |
| `Email__Smtp__Host` | `smtp-relay.brevo.com` | Required when SMTP is active |
| `Email__Smtp__Port` | `587` | |
| `Email__Smtp__Username` | Brevo **SMTP login** | From SMTP tab, not always your account email |
| `Email__Smtp__Password` | Brevo SMTP key | |
| `Email__Smtp__FromAddress` | Verified sender | Must match **Senders & IP** in Brevo |
| `Email__Smtp__FromName` | `LinkNest` | |
| `Email__Smtp__UseStartTls` | `true` | |

Docker `.env` maps `SMTP_*` vars to these (see `env.production.example`). Docker sets `ASPNETCORE_ENVIRONMENT=Production`, so `Email__UseSmtp` is not required.

### Email behavior by environment

| Environment | Default | With `Email__UseSmtp=true` |
|-------------|---------|----------------------------|
| Development | Log to API console | Send via Brevo SMTP |
| Production | Send via SMTP | Send via SMTP |

In **Production**, missing SMTP host or from address **fails startup** (no silent fallback).

Registration rolls back if confirmation email cannot be sent when SMTP is enabled.

### Web proxy (Paths A–C only)

| Variable | Docker default | Fly.io |
|----------|----------------|--------|
| `ReverseProxy__ApiBaseAddress` | `http://api:8080` | `http://linknest-api.internal:8080` |

Path D has **no Web host** — do not set `ReverseProxy__*`.

### Path D — Render Api-only (default)

| Variable | Required | Description |
|----------|----------|-------------|
| `DataProtection__Storage` | **Yes** | Must be `Database` (Neon `DataProtectionKeys`) |
| `Cors__AllowedOrigins` | After E10 | Comma-separated Cloudflare Pages URL + custom domain |
| `ConnectionStrings__DefaultConnection` | Yes | Neon connection string |
| `Jwt__Secret` | Yes | ≥ 32 characters |
| `Auth__WebBaseUrl` | Yes | Cloudflare Pages URL for email links (trailing slash) |
| `Auth__AllowRegistration` | No | `true` / `false` |
| `Auth__RequireConfirmedEmail` | No | Default `true` |
| `Email__Smtp__*` | Yes | Brevo SMTP (same as other paths) |
| `ASPNETCORE_ENVIRONMENT` | Yes | `Production` |

**Do not set on Render:** `DataProtection__KeysPath`, `ReverseProxy__ApiBaseAddress`.

See [D2](#d2-deploy-api-to-render) for the full deploy table. **Cloud Run (D2-alt)** uses the same variables.

### Path D — Cloudflare Pages (build-time, WASM client)

| Variable | When | Description |
|----------|------|-------------|
| `ApiBaseUrl` | Publish / CI | Render service URL, e.g. `https://linknest-api.onrender.com` |
| `Auth__WebBaseUrl` | Publish (optional) | Pages public URL if baked into client config |

Inject at `dotnet publish` for `LinkNest.Web.Client` (E10 Phase 2). Browsers call Api directly with JWT — no server-side proxy.

---

## TLS reverse proxy (Caddy example)

On a VM with domain `app.example.com` pointing to the server:

```text
app.example.com {
    reverse_proxy localhost:8080
}
```

Caddy obtains Let's Encrypt certificates automatically.

---

## GitHub Actions

| Workflow | Trigger | Purpose |
|----------|---------|---------|
| `.github/workflows/ci.yml` | Push / PR | `dotnet test` + Docker build verify |
| `.github/workflows/deploy.yml` | Push to `main` / manual | Push images to `ghcr.io` |

After deploy workflow runs, pull on your server:

```bash
docker pull ghcr.io/<your-org>/linknest-api:latest
docker pull ghcr.io/<your-org>/linknest-web:latest
```

Make packages **public** in GitHub Packages settings, or `docker login ghcr.io` on the server.

---

## Free hosting options

| Stack | Cost | Notes |
|-------|------|-------|
| **Neon + VM (Oracle Cloud Free)** | $0 | Recommended; run `docker-compose.prod.yml` (Path B) |
| **Neon + Fly.io (2 apps)** | $0* | No VM; `DataProtection__Storage=Database`; see Path C |
| **Neon + Cloudflare Pages + Render** | $0 | JWT static web + Api only; **Path D default**; no card; E10 proposed |
| **Neon + Cloudflare Pages + Cloud Run** | $0* | Path D2-alt; GCP billing account required |
| **Neon + Render (2 services)** | $0* | Hard to share Data Protection keys on free tier |
| **Supabase DB + Fly.io** | $0* | Same as Neon + Fly.io |

\*Free tiers may sleep or have cold starts. Path D: [E10 epic](tickets/E10-static-web-cloud-run-api.md).

---

## Mobile & app stores

### iPhone for 2–3 friends **without** paying Apple $99/year

**You cannot** distribute a native iOS app to other people via App Store or TestFlight without the **Apple Developer Program ($99/year)**.

**Free alternatives for a small group:**

1. **PWA (recommended)** — Deploy LinkNest Web with HTTPS. On iPhone: Safari → Share → **Add to Home Screen**. Works like an app icon; uses the same login and data. Free for unlimited users.
2. **Mobile web bookmark** — Same as PWA without install prompt.
3. **Free Apple ID + Xcode** — Install only on **your own** device; certificate expires every 7 days; **cannot** legitimately share with friends.

### Google Play ($25 one-time)

Requires Android target (`net10.0-android`, E9 H3 — not in repo yet). Internal testing track supports up to 100 testers.

### Native MAUI iOS ($99/year)

Requires `net10.0-ios`, macOS, Xcode, and Apple Developer enrollment.

**Practical order:** Web (+ PWA) → Android internal test → iOS when budget allows.

---

## Store checklist (when ready)

- [ ] Public HTTPS URL
- [ ] Privacy policy at `/privacy`
- [ ] Production SMTP tested
- [ ] Screenshots (after E8 polish)
- [ ] Google Play: $25, signed AAB, `com.linknest.mobile`
- [ ] Apple: $99/year, TestFlight, demo account for review

---

## Rollback & migrations

Apply migrations before or during deploy:

```powershell
dotnet ef database update --project src/LinkNest.Api/LinkNest.Api.csproj
```

Rollback: redeploy previous Docker image tag (`:sha` from GHCR). Database rollbacks require manual EF downgrade — avoid on production unless planned.

---

## Cost summary

| Item | Typical cost |
|------|----------------|
| Web + DB (free tiers) | $0–15/mo |
| SMTP (Brevo/SendGrid free) | $0 at low volume |
| Domain (optional) | ~$10–15/year |
| Google Play | $25 one-time |
| Apple Developer | $99/year |

True **$0 ongoing** is possible for web + email on free tiers. **Apple App Store always requires $99/year** for native iOS distribution to others.

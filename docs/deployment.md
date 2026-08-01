# LinkNest — Production deployment

This guide covers hosting LinkNest on free/low-cost infrastructure, configuring SMTP, Docker deployment, CI/CD, and store publishing options.

## Architecture

| Component | Role |
|-----------|------|
| **PostgreSQL** | System of record (use Neon, Supabase, or self-hosted) |
| **LinkNest.Api** | REST API, Identity, JWT, outbound email |
| **LinkNest.Web** | Blazor UI, cookie auth, YARP proxy to `/api/*` |

Both **Api** and **Web** must share the same **Data Protection key ring** (`/keys` volume in Docker) or auth cookies will not work.

Public URL for users: **Web only**. The API can stay internal (`http://api:8080` inside Docker).

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
| `DataProtection__KeysPath` | Yes (prod) | Shared directory, e.g. `/keys` |
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

### Web proxy

| Variable | Docker default |
|----------|----------------|
| `ReverseProxy__ApiBaseAddress` | `http://api:8080` |

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
| **Neon + VM (Oracle Cloud Free)** | $0 | Recommended; run `docker-compose.prod.yml` |
| **Neon + Render (2 services)** | $0* | Hard to share Data Protection keys on free tier |
| **Supabase DB + Fly.io** | $0* | Usage limits; shared keys need extra setup |

\*Free tiers may sleep or have cold starts.

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

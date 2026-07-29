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
2. Copy the PostgreSQL connection string.
3. Apply migrations once from your machine:

```powershell
$env:ConnectionStrings__DefaultConnection = "<neon-connection-string>"
dotnet ef database update --project src/LinkNest.Api/LinkNest.Api.csproj
```

### 2. SMTP (Brevo — free ~300 emails/day)

1. Sign up at [brevo.com](https://www.brevo.com).
2. Verify a sender address under **Senders & IP**.
3. Create an **SMTP key** under **SMTP & API**.
4. Use:

| Setting | Value |
|---------|--------|
| Host | `smtp-relay.brevo.com` |
| Port | `587` |
| Username | Your Brevo login email |
| Password | SMTP key (not account password) |
| STARTTLS | `true` |

Alternatives: SendGrid (100/day), Mailjet (200/day).

### 3. Configure environment

```powershell
copy env.production.example .env
# Edit .env — set DATABASE_URL, WEB_BASE_URL, JWT_SECRET, SMTP_* values
```

**Important:** `WEB_BASE_URL` must be the public HTTPS URL users open (e.g. `https://app.example.com/`). Confirmation and password-reset links use this value.

### 4. Run containers

```powershell
docker compose -f docker-compose.prod.yml up -d --build
```

Web listens on port **8080** (override with `WEB_PORT` in `.env`). Put **Caddy** or **nginx** in front for HTTPS.

### 5. Verify

- Open `https://your-domain/` → login page
- Register with a real email → confirm via inbox
- `/privacy` → privacy policy (required for app stores)

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

| Variable | Example |
|----------|---------|
| `Email__Smtp__Host` | `smtp-relay.brevo.com` |
| `Email__Smtp__Port` | `587` |
| `Email__Smtp__Username` | Brevo login email |
| `Email__Smtp__Password` | Brevo SMTP key |
| `Email__Smtp__FromAddress` | Verified sender |
| `Email__Smtp__FromName` | `LinkNest` |
| `Email__Smtp__UseStartTls` | `true` |

### Web proxy

| Variable | Docker default |
|----------|----------------|
| `ReverseProxy__ApiBaseAddress` | `http://api:8080` |

In **Development**, email is logged to the API console instead of sent.

In **Production**, missing SMTP host/from address **fails startup** (no silent fallback).

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

# Cloudflare deploy — LinkNest static WASM

## Why the dashboard upload fails

If you see **"Create a Worker"** with a **Worker name** field:

| Problem | What you see | Fix |
|---------|--------------|-----|
| **Invalid name** | Red border on name like `.mwasim-alkurdi...`, **Deploy disabled** | Use a valid name: `linknestapplication` (letters/numbers/hyphens only, no leading dot) |
| **Wrong SPA routing** | Home works but `/login` 404 after deploy | Workers **ignore `_redirects`** — use Wrangler CLI with `wrangler.toml` (SPA mode) in this repo |
| **Git "build command" missing** | Wrangler upload, no output directory | Cloudflare Git builder has **no .NET** — build locally or use GitHub Actions |

**Recommended:** skip the dashboard drag-and-drop and run:

```powershell
./scripts/deploy-cloudflare.ps1 -ApiBaseUrl "https://familymeals-dyrq.onrender.com"
```

First run opens a browser for Cloudflare login. This uses `wrangler.toml` with `not_found_handling = "single-page-application"` so Blazor routes work.

Cloudflare's built-in Git builder also **does not include .NET**, so you cannot run `dotnet publish` there. Use **Wrangler CLI**, **Direct Upload to Pages**, or **GitHub Actions**.

---

## Option A — Direct Upload (manual, recommended first time)

### 1. Publish locally

```powershell
./scripts/publish-static-web.ps1 -ApiBaseUrl "https://familymeals-dyrq.onrender.com"
```

Output folder: `publish/static-web/` — must contain `index.html` at the **top level** (not inside a `wwwroot` subfolder).

### 2. Create a zip (important)

Zip the **contents** of `publish/static-web`, not the parent folder.

PowerShell:

```powershell
Compress-Archive -Path "publish/static-web/*" -DestinationPath "publish/linknest-pages.zip" -Force
```

When you open the zip, the first entry must be `index.html`, not `static-web/index.html`.

### 3. Upload in Cloudflare

1. [Cloudflare Dashboard](https://dash.cloudflare.com/) → **Workers & Pages**
2. **Create** → tab **Pages** (not Workers)
3. **Upload assets** (not "Connect to Git" on first deploy)
4. Project name: e.g. `linknestapplication`
5. Upload `linknest-pages.zip` or drag the folder contents
6. **Save and deploy**
7. Copy the URL: e.g. `https://linknestapplication.pages.dev`

### 4. Update Render env vars

| Variable | Example |
|----------|---------|
| `Cors__AllowedOrigins` | `https://linknestapplication.pages.dev` |
| `Auth__WebBaseUrl` | `https://linknestapplication.pages.dev/` |
| `Auth__RequireConfirmedEmail` | `true` |

Redeploy Render after saving.

---

## Option B — GitHub Actions (automated)

Use when you want deploy on push without building on Cloudflare.

### 1. Create an empty Pages project (once)

Dashboard → Workers & Pages → Create → **Pages** → project name `linknestapplication` (can be empty / placeholder).

### 2. Cloudflare API token

Dashboard → My Profile → **API Tokens** → Create token → template **Edit Cloudflare Workers** (includes Pages).

Copy:

- API token
- Account ID (Workers & Pages overview, right column)

### 3. GitHub repository secrets

Settings → Secrets and variables → Actions:

| Secret | Value |
|--------|-------|
| `CLOUDFLARE_API_TOKEN` | token from step 2 |
| `CLOUDFLARE_ACCOUNT_ID` | account ID |

### 4. Run the workflow

Actions → **Deploy Cloudflare Pages** → **Run workflow**

Or push to `main` / `E10-Phase-3` after merging the workflow file.

---

## Option C — Wrangler CLI (recommended)

**Workers (SPA mode, uses `wrangler.toml` in repo root):**

```powershell
./scripts/deploy-cloudflare.ps1 -ApiBaseUrl "https://familymeals-dyrq.onrender.com"
```

The deploy script removes `_redirects` automatically (Workers rejects it; SPA routing is in `wrangler.toml`).

Site URL: `https://linknestapplication.workers.dev`

**Pages (uses `_redirects` instead):**

```powershell
./scripts/publish-static-web.ps1 -ApiBaseUrl "https://familymeals-dyrq.onrender.com"
npx wrangler pages deploy publish/static-web --project-name=linknestapplication
```

Site URL: `https://linknestapplication.pages.dev`

First run prompts for Cloudflare login.

---

## Troubleshooting manual upload

| Symptom | Fix |
|---------|-----|
| Blank page / 404 on refresh | `_redirects` missing from zip root — republish with `./scripts/publish-static-web.ps1` |
| Api calls fail (CORS) | Set `Cors__AllowedOrigins` on Render to exact Pages URL |
| Login works locally, not on Pages | Check `publish/static-web/appsettings.json` has Render `ApiBaseUrl` |
| Zip upload rejected / empty site | Zip **contents** of `static-web`, not the folder itself |
| Git connect shows Wrangler only | Wrong product — use **Pages → Upload assets** or GitHub Actions |
| Wrangler error 100324 infinite loop on `_redirects` | Use `./scripts/deploy-cloudflare.ps1` (strips `_redirects` for Workers) |

---

## Verify after deploy

```
[ ] https://YOUR-PROJECT.pages.dev loads login
[ ] Login → home
[ ] Register → email link uses Pages domain (/confirm-email)
[ ] No CORS errors in browser console
[ ] GET https://familymeals-dyrq.onrender.com/health → 200
```

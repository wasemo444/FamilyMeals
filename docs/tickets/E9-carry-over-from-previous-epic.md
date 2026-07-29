# E9 — Carry Over from Previous Epic

## Goal

Close auth and email-related gaps deferred from E2, finish E7 mobile follow-ups (H3/H4), and **deploy LinkNest to production** — hosted web + API + database on free/low-cost tiers where possible, with **Google Play** and **Apple App Store** publishing when developer accounts and platform targets are ready.

## Depends On

E2 (minimal authentication and the `IEmailSender` / `EmailConfirmationService` abstractions must exist).  
E7 (mobile app for store builds).  
E8 recommended before public store listings (screenshots and responsive polish).

## Carry Over from E2 / PRDV2

Items explicitly listed as out of scope in E2 and deferred in PRDV2 §11:

- **Real email delivery (SMTP)** — replace `LoggingEmailSender` with a production-capable sender so confirmation (and later reset) emails reach users inboxes, not only the API console log.
- **Password reset** — forgot-password request, email with reset link, reset form, and API/Web endpoints.
- **Resend confirmation email** — optional but recommended UX when a user registers but never confirms (today they must copy the link from dev logs).
- **External identity providers** — Google / Microsoft / Apple (PRDV2 deferred fuller auth UX).
- **Multi-factor authentication (2FA)** — PRDV2 deferred fuller auth UX.

## In Scope (this epic — minimum viable)

- **SMTP-backed `IEmailSender`**
  - Configurable provider settings (host, port, credentials/API key, from address) via `appsettings` / environment variables — no secrets committed.
  - `LoggingEmailSender` remains the default in Development; a real sender (e.g. SMTP via MailKit, or SendGrid/SES adapter) is registered for Staging/Production.
  - Confirmation emails sent on register use the real sender in non-dev environments.
- **Email confirmation delivery verified**
  - Register → user receives confirmation email → click link → `GET /account/confirm-email` → can log in.
  - Existing `RequireConfirmedEmail = true` behavior unchanged; only delivery mechanism is completed.
- **Resend confirmation** — API endpoint + Web UI affordance on login/register when `error=unconfirmed`.
- **Password reset flow** — request reset by email, tokenized reset link, set new password, login with new password.

## Acceptance Criteria

- A non-dev configuration profile sends real confirmation emails through the configured SMTP (or provider) instead of logging the body to the console.
- Misconfiguration (missing host/credentials) fails fast at startup or first send with a clear error — no silent fallback to logging in Production.
- Manual test: register a new user in a Staging-like profile → email arrives → confirm → login succeeds.
- Manual test: forgot password → email arrives → reset link works → login with new password succeeds.
- Resend confirmation works for an unconfirmed account (rate-limited to prevent abuse).
- Unit/integration tests cover the SMTP sender configuration binding and at least one happy-path send (can use a test double or local capture server).
- E2 ticket "Out of Scope" items for email confirmation delivery and password reset are satisfied; external providers and 2FA remain optional stretch goals within this epic unless explicitly pulled into the sprint.

## Out of Scope

- Mobile-specific auth UX beyond what E7 already provides (token refresh, etc.).
- Marketing/onboarding email templates beyond plain transactional HTML for confirm/reset.

*(Production hosting, app store publishing, and deployment runbooks are **in scope** below — § Production hosting & store publishing.)*

## Likely Files/Areas

- `src/LinkNest.Api/Identity/` — `SmtpEmailSender` (or provider-specific implementation), options class, DI registration replacing unconditional `LoggingEmailSender`.
- `src/LinkNest.Api/Identity/EmailConfirmationService.cs` — reuse as-is; may add resend helper.
- `src/LinkNest.Api/Endpoints/AuthEndpoints.cs` — resend-confirmation, forgot-password, reset-password endpoints.
- `src/LinkNest.Web/LinkNest.Web/Endpoints/AccountEndpoints.cs` — Web-facing reset/confirm pages or form POST handlers as needed.
- `src/LinkNest.Web/LinkNest.Web.Client/Pages/` — forgot-password, reset-password, resend-confirmation UI.
- `src/LinkNest.Api/appsettings*.json` — `Email` / `Smtp` section (placeholder values only).
- `src/LinkNest.Shared/Resources/LocalizationCatalog.cs` — strings for reset/resend flows.

## Manual Test Notes

- **Dev (unchanged):** register → copy confirmation URL from API log → confirm → login.
- **Staging/Production profile:** register with a real mailbox → confirm via inbox link → login.
- Request password reset for a confirmed user → complete reset → old password rejected, new password accepted.
- Attempt resend confirmation for unconfirmed user → second email received; rate limit returns 429 after threshold.
- Confirm Production startup fails or logs critical error if SMTP settings are absent (no silent log-only sender).

## Notes

- Current state (post-E2 follow-up): `RequireConfirmedEmail` is enabled, register sends confirmation via `EmailConfirmationService`, and `LoggingEmailSender` writes the link to the API terminal. E9 completes real delivery and the remaining deferred auth UX from E2.

---

## User Settings Menu (Toolbar)

### Goal

Give authenticated users a dedicated Settings page for account and UI preferences, and declutter the app header. Today the header toolbar in `InteractiveShell` shows the user's display name, logout, and an inline `<LanguageSwitcher />` (EN/AR toggle buttons). Language switching and other self-service account actions belong on a Settings screen reachable from the toolbar (gear icon or user menu), not in the global header.

### Depends On

E2 (authenticated users, `ApplicationUser.DisplayName`, cookie session). Password-reset and change-password flows in the SMTP/password-reset portion of this epic (above) must exist or ship in the same sprint so Settings can link to them.

### In Scope (PRDV2 references)

- US-10 (switch between English and Arabic) — **relocate** the existing switcher UI from the header into Settings; behavior unchanged (FR-19, FR-21).
- FR-22, FR-23 — default language from browser on first visit; persist selected culture (today via `CultureService` → `PUT /api/settings`; may migrate to per-user profile storage as part of this work if the singleton `AppSettings` row is insufficient for multi-user deployments).
- Deferred fuller auth UX (PRDV2 §11) — account self-service subset: display name, read-only email, change password entry point, account deactivation.
- Validation Checklist #5 — language switch remains implemented; entry point moves from header to Settings.

### Acceptance Criteria

- **Header cleanup:** `<LanguageSwitcher />` is removed from `InteractiveShell.razor` header. The header retains brand, Home/Archive nav, and auth affordances (display name + logout when signed in; login/register when not). No language toggle remains in the toolbar.
- **Settings entry point:** Signed-in users reach Settings from the header via a gear icon and/or a user menu item (e.g. "Settings" alongside display name). Route is stable (e.g. `/settings`) and protected — unauthenticated users are redirected to login.
- **Language preference:** Settings includes EN/AR selection using the existing `LanguageSwitcher` component (or equivalent markup) embedded on the page. Selecting a language updates UI culture, RTL/LTR (`dir`), and persisted preference exactly as today; no regression in Arabic layout (US-11).
- **Display name:** User can view and edit their display name. Save persists to `ApplicationUser.DisplayName` via a new API endpoint; header display name updates on next load or via refreshed auth claims without requiring re-login.
- **Email (read-only):** Settings shows the account email from `AuthUserInfo` / current user; no in-app email change in this epic (would require re-confirmation flow).
- **Change password:** Settings provides a link or section that navigates to the E9 forgot-password / reset-password flow (or an authenticated change-password form if implemented in this epic). Signed-in users are not forced through "forgot email" if a dedicated change-password path exists.
- **Deactivate account:** Self-service deactivation with explicit confirmation (e.g. type display name or "DEACTIVATE", plus current password). Deactivated users cannot log in; session is terminated on success. Account row is soft-deactivated (e.g. lockout / `IsActive` flag), not hard-deleted — FR-42 still applies (user with owned content cannot be physically removed). Group memberships and owned content follow existing E3/E5 orphaning rules (content stays; membership may be removed or left per existing product defaults — document chosen behavior in implementation).
- **Logout:** May remain in header for convenience; Settings may duplicate logout as a secondary action (optional).
- Localization strings added for Settings labels, confirmation copy, and success/error messages (EN + AR).

### Out of Scope

- Email address change and re-verification.
- External identity providers and MFA (same stretch goals as the SMTP section above).
- Group-administration settings (invite/manage members — E5).
- Mobile-specific Settings UI beyond responsive web layout (MAUI settings screen is E7 unless explicitly shared components are reused).
- Full profile photo, notification preferences, or theme/dark mode.

### Likely Files/Areas

- `src/LinkNest.Web/LinkNest.Web.Client/Components/InteractiveShell.razor` — remove `<LanguageSwitcher />`; add gear/user-menu link to Settings.
- `src/LinkNest.Web/LinkNest.Web.Client/Components/LanguageSwitcher.razor` (+ `.cs`) — reuse on Settings page (no header placement).
- `src/LinkNest.Web/LinkNest.Web.Client/Pages/Settings.razor` (+ `.cs`) — new Settings page composing language, profile, password link, deactivate flow.
- `src/LinkNest.Web/LinkNest.Web.Client/Services/CultureService.cs` — unchanged contract; called from Settings-hosted switcher.
- `src/LinkNest.Api/Endpoints/AuthEndpoints.cs` (or new `AccountSettingsEndpoints.cs`) — `PATCH /api/auth/me` (display name), `POST /api/auth/deactivate` (or equivalent).
- `src/LinkNest.Api/Identity/ApplicationUser.cs` — optional `IsActive` / deactivation timestamp if not using permanent lockout only.
- `src/LinkNest.Shared/Auth/` — request/response DTOs for profile update and deactivation.
- `src/LinkNest.Shared/Resources/LocalizationCatalog.cs` — Settings UI strings.
- `src/LinkNest.Api/Endpoints/SettingsEndpoints.cs` — culture persistence (possibly refactor singleton → per-user if needed).

### Manual Test Notes

- Sign in → confirm header has **no** EN/AR buttons; open Settings via gear/user menu → switch EN ↔ AR → UI and `dir` update; refresh → preference retained.
- Change display name on Settings → save → header shows new name; API `/api/auth/me` returns updated `DisplayName`.
- Settings shows email read-only; cannot edit email field.
- From Settings, follow change-password link → complete E9 reset/change flow → log in with new password.
- Deactivate account: confirm dialog requires explicit acknowledgment + password → redirected to login; subsequent login fails with clear message; deactivated user's categories/links still exist (FR-42 / ownership intact).
- Sign out from header still works; unauthenticated user hitting `/settings` is sent to login.
- Arabic Settings page renders RTL correctly with localized labels.

---

## E7 Mobile — Deferred Follow-ups (Carry Over from Architect Review)

These items were intentionally deferred during E7 implementation because they require larger refactors or additional platform tooling. They do **not** block web or API functionality; E7 shipped Windows-target MAUI with JWT auth and Web.Client page reuse. Track completion here in E9 (or a dedicated sprint) before E8 visual polish if mobile parity matters for the release.

**Depends on:** E7 (Mobile MAUI Blazor Hybrid — implemented on Windows).

### H3 — Android target framework and emulator support

**Priority:** High (deferred)  
**Why deferred:** Adding `net10.0-android` requires MAUI Android platform files, workload packs, and cleartext/network config for dev HTTP. Windows was chosen as the first verified target.

**What to do:**

1. **Update `src/LinkNest.Mobile/LinkNest.Mobile.csproj`**
   - Add `net10.0-android` to `<TargetFrameworks>` alongside `net10.0-windows10.0.19041.0`.
   - Keep `<EnableTizen>false</EnableTizen>` unless Tizen is explicitly in scope.

2. **Add Android platform bootstrap** — use the full `Platforms/Android/` tree from `dotnet new maui` as reference (not only three files). Minimum includes:
   - `Platforms/Android/MainActivity.cs`
   - `Platforms/Android/MainApplication.cs`
   - `Platforms/Android/AndroidManifest.xml` with `android:usesCleartextTraffic="true"` for **Development only** (API uses `http://` on LAN/emulator), or a `network_security_config.xml` that whitelists dev hosts
   - `Platforms/Android/Resources/**` (styles, colors, mipmap placeholders as generated by the template)

3. **API URL defaults** — confirm `MobileApiConfiguration.ResolveApiBaseUrl` (in `LinkNest.Shared/Configuration/`) resolves:
   - Android emulator: `http://10.0.2.2:5280/` when config/env unset
   - Physical device: set `LINKNEST_API_BASE_URL=http://192.168.x.x:5280/` in `appsettings.Development.json`

4. **LAN / physical device dev**
   - Bind the API to the host LAN interface for device testing (e.g. add machine IP to `applicationUrl` in `src/LinkNest.Api/Properties/launchSettings.json`, or run with `--urls http://0.0.0.0:5280`).
   - Cleartext HTTP applies to physical devices the same as emulators — ensure Android manifest or network security config allows `http://192.168.x.x:5280/` in Development.

5. **Workload / CI**
   - Document and run **one at a time** (never parallel): `dotnet workload restore` then `dotnet build -f net10.0-android`.
   - Do **not** stack multiple `dotnet workload install` commands — concurrent MSI installs fail with `0x652` on Windows.

6. **Acceptance criteria**
   - `dotnet build src/LinkNest.Mobile/LinkNest.Mobile.csproj -f net10.0-android` succeeds on a machine with MAUI Android workload installed.
   - Login with `dev@linknest.local` / `DevPassword1!` against a running API; bootstrap loads; create/archive/favorite flows work on emulator.
   - Kill and relaunch app — JWT in SecureStorage restores session or prompts re-login gracefully.

**Likely files:** `LinkNest.Mobile.csproj`, `Platforms/Android/*`, `appsettings.Development.json`, `docs/tickets/E7-mobile-maui.md` (Android manual test notes).

---

### H4 — Extract shared UI into neutral Razor class library (`LinkNest.UI`)

**Priority:** High (deferred)  
**Why deferred:** E7 references `LinkNest.Web.Client` (Blazor WebAssembly SDK) from the MAUI host to reuse pages quickly. That pulls WASM-oriented dependencies and render-mode concerns into the native shell. PRDV2 §10 originally envisioned Shared RCL + platform hosts, not Web.Client inside Mobile.

**What to do:**

1. **Create `src/LinkNest.UI/LinkNest.UI.csproj`**
   - SDK: `Microsoft.NET.Sdk.Razor` (class library, **not** `Microsoft.NET.Sdk.BlazorWebAssembly`).
   - Reference `LinkNest.Shared` only (no Web host, no MAUI).
   - Package refs: `Microsoft.AspNetCore.Components.Web`, `Microsoft.AspNetCore.Components.Authorization`, localization packages as needed by moved components.

2. **Move from `LinkNest.Web.Client` into `LinkNest.UI`:**
   - All routable pages under `Pages/` (Home, Category, Archive, Login, Register, Groups, GroupMembers, Share, …)
   - `Components/` used by those pages (`InteractiveShell`, `CategoryCard`, `LinkCard`, `LanguageSwitcher`, …)
   - `LocalizedComponentBase`, `CultureService`, `LinkPreviewClient`, `InteractiveRenderSettings`
   - Shared `_Imports.razor` and page-scoped CSS as applicable
   - Decide static assets: either move linked `app.css` / `storage.js` into UI `wwwroot` or keep Mobile/Web `.csproj` links pointing at UI paths

3. **Keep in `LinkNest.Web.Client`** (WASM-specific bootstrapping only):
   - `Program.cs` (WASM host builder)
   - `AddLinkNestCoreClientServices` and `AddLinkNestWebCookieAuth` in `ClientServiceCollectionExtensions` (core DI stays here until a future `LinkNest.Client.Core` split, if desired)
   - `WebClientAuthMode`, `WebSecureTokenStore`, `WebAuthStateNotifier`
   - Any WASM-only auth deserialization wiring

4. **Keep in `LinkNest.Mobile`** (platform shell only):
   - `MauiProgram.cs`, `MobileServiceCollectionExtensions`, `MauiSecureTokenStore`, `MobileClientAuthMode`
   - `Components/Routes.razor`, `RedirectToLogin.razor`, `Layout/MainLayout.razor`
   - Platform folders, MAUI assets, `appsettings.json`

5. **Update project references:**
   - `LinkNest.Web.Client` → references `LinkNest.UI` + registers web cookie auth via `AddLinkNestWebCookieAuth()`
   - `LinkNest.Mobile` → references `LinkNest.UI` instead of `LinkNest.Web.Client`
   - `LinkNest.Web` host → still references Web.Client for Auto render mode and WASM bootstrapping

6. **Routing & assembly registration**
   - MAUI `Components/Routes.razor` → `AdditionalAssemblies` = `LinkNest.UI` assembly.
   - Web host **must also** register UI assembly in both:
     - `src/LinkNest.Web/LinkNest.Web/Program.cs` — `.AddAdditionalAssemblies(typeof(LinkNest.UI._Imports).Assembly)` (or equivalent)
     - `src/LinkNest.Web/LinkNest.Web/Components/Routes.razor` — `AdditionalAssemblies` array
   - Update `_Imports.razor` namespaces in Web and Mobile from `LinkNest.Web.Client` to `LinkNest.UI` where pages moved.

7. **Acceptance criteria**
   - Web app: all existing pages and E2E paths unchanged (no functional regression).
   - Mobile: builds and runs on Windows (and Android once H3 is done) without referencing BlazorWebAssembly SDK.
   - `dotnet test` count unchanged or increased; no duplicate page logic between Web and Mobile.

**Likely files:** new `src/LinkNest.UI/`, `LinkNest.Web.Client.csproj`, `LinkNest.Mobile.csproj`, `LinkNest.Web/Program.cs`, `LinkNest.Web/Components/Routes.razor`, `LinkNest.Mobile/Components/Routes.razor`, `LinkNest.slnx`.

---

### E7 polish items (architect review L2–L4)

Smaller follow-ups from the E7 architecture review; safe to batch with H3/H4 or land independently.

| ID | Task | Instructions |
|----|------|--------------|
| **L2** | MAUI workload / CI documentation | Add to README and E7 ticket: run `dotnet workload restore` before first Mobile build; install MAUI workload once; never parallel workload installs on Windows. Note misleading `maui-tizen` NETSDK1147 error when packs are incomplete. |
| **L3** | Auth-mode conditional cleanup | Extract `UsesBearerToken` branches from `Login.razor`, `InteractiveShell.razor.cs`, `Register.razor.cs` into small shared helpers (e.g. `ClientAuthUi`) as mobile/web divergence grows. |
| **L4** | E7 manual test matrix | Record in E7 or E9: login, bootstrap, create/archive/favorite (personal + group), groups/invites, language/RTL, kill/relaunch token persistence, 401 logout behavior on expired JWT. |

### Out of scope (E9 mobile section)

- Server-side JWT revocation / refresh tokens (client-side clear on logout/expiry is sufficient for v2).
- Push notifications, offline-first sync (unchanged from E7 out of scope).

---

## Production Hosting & Store Publishing

**Goal:** Deploy LinkNest so real users can reach the **web app** and install **native mobile apps** from Google Play and the Apple App Store, using **lowest-cost / free-tier options** where viable. Document every step so the stack is reproducible without guesswork.

**Depends on:** E7 (mobile clients exist), E9 SMTP section (email for register/reset in production), E9 H3 (Android for Play Store), and ideally E8 (polish before public store listing — recommended, not blocking for internal/beta deploy).

### Architecture to host

| Component | Role | Production notes |
|-----------|------|------------------|
| **PostgreSQL** | System of record | Managed DB; never expose 5432 publicly without firewall |
| **LinkNest.Api** | REST + Identity + JWT | HTTPS only; env-based secrets (`Jwt__Secret`, connection string) |
| **LinkNest.Web** | Blazor host + YARP proxy | Same origin for cookies; proxies `/api/*` to Api |
| **LinkNest.Mobile** | Store binaries | Points at public API URL via `ApiBaseUrl` / `LINKNEST_API_BASE_URL` |

Minimum production checklist:

- [ ] HTTPS everywhere (TLS certificates — Let's Encrypt or platform-managed).
- [ ] Shared **DataProtection** key path accessible by Api + Web (or single co-hosted process).
- [ ] **JWT secret** and **DB credentials** in environment variables / secret store — not in git.
- [ ] **SMTP** configured (§ SMTP above) for confirm/reset emails.
- [ ] EF migrations applied on deploy (`FR-26`).
- [ ] CORS not wide-open; mobile app calls Api directly with bearer tokens.
- [ ] Rate limiting enabled on auth endpoints (already in Api).
- [ ] Backups enabled on PostgreSQL (managed providers include this).

---

### Phase 1 — Web + API + database (free / low-cost options)

Pick **one** stack; document the chosen provider in `docs/deployment.md` (create during implementation).

#### Recommended free-friendly combinations

| Provider | What you get (typical free tier) | Caveats |
|----------|----------------------------------|---------|
| **[Neon](https://neon.tech)** | PostgreSQL serverless, free tier | Pair with free web host below |
| **[Supabase](https://supabase.com)** | PostgreSQL + dashboard, free tier | Use DB only; app stays on LinkNest.Api |
| **[Render](https://render.com)** | Free web services + paid/free Postgres tiers | Free web **sleeps** after inactivity; cold starts |
| **[Fly.io](https://fly.io)** | Containers + small Postgres allowance | Requires `fly.toml`; free allowance limited |
| **[Railway](https://railway.app)** | Containers + Postgres | Trial credits; then usage-based |
| **Oracle Cloud Always Free** | ARM VM (Docker Compose) | More ops work; run `docker compose` + reverse proxy yourself |
| **Azure** | App Service F1 (limited), Container Apps trial | PostgreSQL usually **not** free — use Neon + Azure Web |

**Suggested minimal-cost path (documented default):**

1. **Database:** Neon or Supabase free PostgreSQL → connection string in Api/Web env.
2. **Api + Web:** Single **Docker Compose** image or two containers on Render/Fly/Railway free tier.
3. **TLS:** Platform-managed cert (Render/Fly) or Caddy/Traefik + Let's Encrypt on a VPS.
4. **CI:** GitHub Actions (free for public repos) — build, test, deploy on push to `main`.

#### What to implement in repo

1. **`Dockerfile`** (or split `Dockerfile.api`, `Dockerfile.web`) — multi-stage publish for Release builds.
2. **`docker-compose.prod.yml`** (optional) — Api + Web + external DB URL (no local postgres in prod compose if using Neon).
3. **`.github/workflows/deploy.yml`** — build, run tests, push container, deploy (provider-specific).
4. **`docs/deployment.md`** — env var table, first-time setup, rollback, migration steps.
5. **Production `appsettings.Production.json`** placeholders — no secrets; document required env vars:
   - `ConnectionStrings__DefaultConnection`
   - `Jwt__Secret`
   - `DataProtection__KeysPath` or shared volume mount
   - `Auth__WebBaseUrl` (public web URL)
   - `Email__*` / SMTP settings
   - `LINKNEST_API_BASE_URL` for mobile builds (CI variable)

#### Web acceptance criteria

- [ ] Public HTTPS URL loads login and home after auth.
- [ ] Register → confirmation email → login works with production SMTP.
- [ ] Cookie auth works through Web proxy to Api.
- [ ] PostgreSQL data persists across redeploys.
- [ ] `dotnet test` passes in CI before deploy.

---

### Phase 2 — Google Play Store (Android)

**Depends on:** E9 **H3** (`net10.0-android` target, signed release build).

| Requirement | Details |
|-------------|---------|
| **Developer account** | [Google Play Console](https://play.google.com/console) — **$25 one-time** registration fee (not free). |
| **Package name** | Already `com.linknest.mobile` in csproj — finalize before first upload (immutable after publish). |
| **Signing** | Release keystore (`.keystore` / `.jks`); store in CI secrets, never in git. |
| **Build output** | `dotnet publish -f net10.0-android -c Release` → **AAB** (Android App Bundle), not APK for store. |
| **API URL** | Production `ApiBaseUrl` baked or configured — **HTTPS** required for production. |
| **Store listing** | App name, short/full description, screenshots (phone + tablet), feature graphic, privacy policy URL. |
| **Privacy policy** | Public HTTPS page describing data collected (email, links, groups) — required by Google. |
| **Content rating** | Complete Play questionnaire (IARC). |
| **Data safety form** | Declare auth, user content, encryption in transit. |

#### Implementation tasks

1. Add **Android release signing** to `LinkNest.Mobile.csproj` / `Directory.Build.props` (Release only, from env).
2. Document **`dotnet publish`** command and Play Console upload steps in `docs/deployment.md`.
3. Generate store screenshots after E8 responsive pass (phone + tablet).
4. Internal testing track → closed testing → production rollout.

#### Acceptance criteria

- [ ] AAB uploads to Play Console without signing errors.
- [ ] Internal test install from Play Store (or internal app sharing) logs in against production API.
- [ ] App passes Play pre-launch report on reference devices (no critical crashes on login/home).

---

### Phase 3 — Apple App Store (iOS)

**Note:** Current MAUI project targets **Windows only**. iOS requires additional work beyond H3 Android:

| Requirement | Details |
|-------------|---------|
| **Developer account** | [Apple Developer Program](https://developer.apple.com/programs/) — **$99 USD/year** (not free). |
| **Build environment** | **macOS** with Xcode required for signing and upload (GitHub Actions `macos-latest` runner or local Mac). |
| **TFM** | Add `net10.0-ios` to `LinkNest.Mobile.csproj` + `Platforms/iOS/*` (same pattern as H3 Android). |
| **Signing** | Distribution certificate + provisioning profile (App Store Connect). |
| **Build output** | `dotnet publish -f net10.0-ios -c Release` → IPA → Transporter or `xcrun altool`. |
| **App Store Connect** | Bundle ID `com.linknest.mobile`, listing, screenshots (6.7", 6.5", iPad if supported). |
| **Privacy** | App Privacy nutrition labels; privacy policy URL (same as Google). |
| **Review guidelines** | Demo account for Apple reviewers (`dev@…` or dedicated test user with sample data). |

#### Implementation tasks

1. Add **`net10.0-ios`** target and platform folder (mirror MAUI template).
2. Configure **ApiBaseUrl** for production HTTPS API.
3. CI job on `macos-latest`: restore MAUI workloads, publish IPA, upload to TestFlight.
4. TestFlight internal → external beta → App Store submission.

#### Acceptance criteria

- [ ] TestFlight build installs on physical iPhone; login and bootstrap succeed against production API.
- [ ] App Store submission accepted (or rejection issues documented and fixed).

**If iOS is deferred:** Ship **Google Play + web** first; track iOS as E9 follow-up with explicit dependency on macOS CI and Apple Developer enrollment.

---

### Phase 4 — Windows (optional)

Microsoft Store distribution for MAUI Windows apps is possible but **lower priority** than web + Play + App Store. Document sideload / MSIX packaging only if requested.

---

### Cost summary (realistic)

| Item | Cost |
|------|------|
| Web + DB hosting | **$0–15/mo** on free tiers (Neon + Render/Fly); $0 if self-hosted on Oracle Free VM |
| Google Play | **$25 one-time** |
| Apple App Store | **$99/year** |
| Custom domain (optional) | ~$10–15/year |
| SMTP (SendGrid/SES free tiers) | Often **$0** at low volume |

True **$0 ongoing** is possible for web+DB on free tiers; **store publishing always requires Google and/or Apple developer fees**.

---

### Likely files (hosting & stores)

- `Dockerfile`, `docker-compose.prod.yml`
- `.github/workflows/ci.yml`, `.github/workflows/deploy.yml`
- `docs/deployment.md` — hosting, env vars, migrations, store checklists
- `src/LinkNest.Mobile/LinkNest.Mobile.csproj` — Android/iOS release signing properties
- `src/LinkNest.Mobile/appsettings.Production.json` — production API URL template
- Privacy policy page (static markdown hosted on web or GitHub Pages)

### Manual test notes — production

- End-to-end on production URL: register → email confirm → login → create category → add link → share in group.
- Mobile app (release build) against production API — not localhost.
- Verify JWT and cookies use HTTPS; HTTP redirects to HTTPS.
- Load test smoke: cold start on free tier acceptable for beta.

### Out of scope (hosting section)

- Enterprise SSO, multi-region HA, Kubernetes (overkill for v2 scale).
- Paid marketing, ASO agencies, localized store listings beyond EN/AR app UI.
- Windows Store unless explicitly requested.


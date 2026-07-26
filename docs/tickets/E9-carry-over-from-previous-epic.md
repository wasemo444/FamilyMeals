# E9 — Carry Over from Previous Epic

## Goal

Close auth and email-related gaps that were intentionally deferred from E2 (and PRDV2 §11) while the core register/login/group flows were built. E2 shipped minimal identity; email confirmation was added afterward with a dev-only logger instead of real delivery. This epic finishes those carry-over items so registration, confirmation, and password recovery work end-to-end in non-dev environments.

## Depends On

E2 (minimal authentication and the `IEmailSender` / `EmailConfirmationService` abstractions must exist).

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
- Full production hosting/deployment runbooks (still deferred per PRDV2 §14) — only the email sender configuration contract is in scope.

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
- App store deployment, push notifications, offline sync (unchanged from E7 out of scope).

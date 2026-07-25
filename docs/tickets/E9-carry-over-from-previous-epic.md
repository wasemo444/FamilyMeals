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

- `src/ManageFamilyMeals.Api/Identity/` — `SmtpEmailSender` (or provider-specific implementation), options class, DI registration replacing unconditional `LoggingEmailSender`.
- `src/ManageFamilyMeals.Api/Identity/EmailConfirmationService.cs` — reuse as-is; may add resend helper.
- `src/ManageFamilyMeals.Api/Endpoints/AuthEndpoints.cs` — resend-confirmation, forgot-password, reset-password endpoints.
- `src/ManageFamilyMeals.Web/ManageFamilyMeals.Web/Endpoints/AccountEndpoints.cs` — Web-facing reset/confirm pages or form POST handlers as needed.
- `src/ManageFamilyMeals.Web/ManageFamilyMeals.Web.Client/Pages/` — forgot-password, reset-password, resend-confirmation UI.
- `src/ManageFamilyMeals.Api/appsettings*.json` — `Email` / `Smtp` section (placeholder values only).
- `src/ManageFamilyMeals.Shared/Resources/LocalizationCatalog.cs` — strings for reset/resend flows.

## Manual Test Notes

- **Dev (unchanged):** register → copy confirmation URL from API log → confirm → login.
- **Staging/Production profile:** register with a real mailbox → confirm via inbox link → login.
- Request password reset for a confirmed user → complete reset → old password rejected, new password accepted.
- Attempt resend confirmation for unconfirmed user → second email received; rate limit returns 429 after threshold.
- Confirm Production startup fails or logs critical error if SMTP settings are absent (no silent log-only sender).

## Notes

- Current state (post-E2 follow-up): `RequireConfirmedEmail` is enabled, register sends confirmation via `EmailConfirmationService`, and `LoggingEmailSender` writes the link to the API terminal. E9 completes real delivery and the remaining deferred auth UX from E2.

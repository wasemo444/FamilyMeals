# E6 — Security & Migration Hardening

## Goal

Close the remaining architecture-review gaps that aren't tied to a specific feature epic: SSRF-safe link preview fetching, and a one-time migration path for any early adopters still on v1's local JSON/localStorage data model. This is a hardening/cleanup epic, not new user-facing functionality.

## Depends On

E3 (ownership model must exist as the migration target).

## In Scope (PRDV2 references)

- FR-11 (rewritten for v2 as an SSRF-safe fetcher)
- FR-43 (v1 → v2 migration tool)
- Validation Checklist #24, #25

## Acceptance Criteria

- The link-preview fetch endpoint validates the target URL before making a server-side request: rejects non-HTTP(S) schemes, rejects private/loopback/link-local IP ranges (and DNS names that resolve to them), and enforces a reasonable timeout/size limit on the fetched response.
- SSRF protection is covered by tests exercising at least: a private IP literal, a loopback URL, a non-http(s) scheme, and a normal public URL that should still succeed.
- A migration tool/script exists that takes v1's exported JSON (or reads a provided localStorage-shaped payload) and imports it into PostgreSQL as personally-owned (`OwnerType = User`) categories/links for a specified user, preserving `IsFavorite`, timestamps, and archive state.
- Migration tool is idempotent or clearly documented as run-once (no silent duplicate imports on re-run).
- Validation Checklist rows #24 and #25 flip to "Implemented."

## Out of Scope

- Any new feature surface — this epic only hardens existing behavior from E1–E5.
- Mobile (E7).
- Production hosting/deployment configuration (still deferred, non-blocking per §14).

## Likely Files/Areas

- `src/ManageFamilyMeals.Api/` — link preview fetcher (URL/IP validation logic, likely a dedicated `ISafeUrlFetcher` or similar), unit tests for the SSRF guard.
- `src/ManageFamilyMeals.Api/Migration/` (or a standalone console tool project) — v1 JSON → PostgreSQL import script.

## Manual Test Notes

- Attempt to add a link whose preview target points at `127.0.0.1` or a private IP and confirm the fetch is rejected, not silently retried against the real target.
- Run the migration tool against a sample v1 JSON export and confirm categories/links appear correctly owned by the target user with favorites/archive state intact.

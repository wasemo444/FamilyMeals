# Product Requirements Document — Manage Family Meals

**Version:** 2.0
**Date:** July 19, 2026
**Status:** Proposed — pending approval (introduces new architecture decisions: PostgreSQL persistence, group sharing, and a pulled-forward minimal authentication requirement)

> This document is a direct evolution of PRD v1.0 (POC). It preserves every v1 section, user story, functional requirement, and non-functional requirement, and edits them in place only where the two v2 decisions below require it. Nothing from v1 has been silently dropped — items that no longer apply (e.g., "multi-device data sync" as out of scope) are explicitly called out as changed, not deleted quietly.
>
> **Architecture review addendum:** an independent architecture review of the v2 decisions surfaced six high/critical-severity risks (ownership-model data integrity, missing concurrency control, unspecified delete/FK behavior, cross-client session handling, an SSRF gap in link previews, and no v1→v2 data migration path). All six are now closed by new/updated requirements below (FR-11, FR-29, FR-40–FR-43) rather than left as open risks.

---

## 1. Executive Summary

**Manage Family Meals** is a unified web and mobile application for a small group (~10 family/friends) to organize meal-related links into categories (Breakfast, Lunch, Snack, etc.). It supports English and Arabic with full RTL layout for Arabic.

**What's new in v2:** Version 1 stored data per device in JSON/localStorage and explicitly deferred both authentication and multi-device/shared storage. Version 2 resolves those deferrals for the pieces needed now:

- **Persistence moves to PostgreSQL.** A relational database becomes the system of record for all categories, links, users, and groups, accessed through a thin Web API so both the Blazor web client and the MAUI mobile client talk to the same backend.
- **Group sharing is introduced.** Users can create or join a group of up to 10 people. Within a group, categories and links (including calorie/nutritional info and external links) can be shared among members, while each user retains private categories/links visible only to themselves. The home experience is unified — a user sees their private content and their group's shared content together, with a clear visual distinction between "mine" and "shared."
- **A minimal authentication mechanism is pulled forward** from "later phase" to a hard prerequisite, because group membership, invites, and per-item ownership cannot exist without a way to identify distinct users. See [OPEN QUESTION — Auth mechanism] below.

Everything else about the product's purpose and scope from v1 remains unchanged.

---

## 2. Goals & Success Criteria

| Goal | Success Criteria |
|------|------------------|
| Unified cross-platform experience | Shared UI and business logic via .NET Razor Class Library |
| Simple link organization | Create categories and links in under 30 seconds |
| Fast access to favorites | Dedicated favorites sections + favorites sorted to top |
| Bilingual UX | EN/AR toggle; Arabic renders RTL |
| Centralized, reliable persistence *(was: "Low operational overhead")* | PostgreSQL is the system of record, accessed via a Web API; data is consistent across web and mobile clients and no longer trapped on a single device |
| Collaborative meal planning *(new in v2)* | A user can create/join a group of up to 10 people and share categories/links with the group, while private content stays visible only to them |
| Safe deletion | Soft-delete with 7-day archive before permanent removal |

> Note: v1's "low operational overhead" goal was explicitly satisfied by *avoiding* a database. v2 intentionally trades a small amount of operational overhead (running/hosting PostgreSQL and an API tier) for centralized, shareable, queryable data. This tradeoff is the core of the "Resolved Decisions for V2" section below.

---

## 3. Resolved Decisions for V2

This section states plainly the two decisions this phase locks in, since resolving them is the entire point of v2. Details, rationale, and consequences are expanded in the sections that follow.

1. **Database: PostgreSQL is adopted as the system of record**, replacing per-device JSON/localStorage. All categories, links, users, and groups live in PostgreSQL, accessed via Entity Framework Core. Because PostgreSQL is a server-side database, the MAUI mobile client can no longer read/write a local file directly — a thin backend Web API (ASP.NET Core Web API) is required for both the Blazor web client and the MAUI Hybrid app to reach the database over HTTP.

   **Why PostgreSQL fits this app's data shape:** the app's entities are structured and relational — users, groups, categories, and links connected by foreign keys (a link belongs to a category; a category or link belongs to a user or a group). The app also needs referential integrity for cascade-archive behavior (archiving a category must cascade to its links, and archiving/removing a group must resolve what happens to its shared content — see [Ownership & Permissions Model](#ownership--permissions-model)). Future features such as filtering meals by calories or nutritional attributes are naturally expressed as relational queries (joins, filters, aggregates) rather than document lookups. A document/NoSQL store would require re-deriving these relationships and integrity guarantees at the application layer; PostgreSQL provides them natively.

2. **Group functionality is in scope.** Users can create or join a group of up to 10 people. Within a group, meals/categories/links — including calorie/nutritional info and external links — are shareable among members. Each user also keeps private meals/categories/links visible only to them. The home experience is unified: a user sees their own private content and their group's shared content together in one view, not as two disconnected apps, with a clear visual distinction between "mine" and "shared."

   Group functionality introduces a new **ownership model** (every category and link now has an owner that is either a specific `User` or a `Group`) and depends on a minimal authentication mechanism being pulled forward from v1's "later phase" — see [OPEN QUESTION — Auth mechanism](#open-question--auth-mechanism-for-groups).

As a direct consequence, v1's "Out of Scope" items **"Multi-device data sync"** and **"Shared/cloud storage"** are no longer deferred — they are resolved and now in scope for v2 (see [Section 11](#11-out-of-scope-current-phase)).

---

## 4. Approved Architecture Decisions

| Decision | Choice |
|----------|--------|
| Cross-platform approach | Blazor Web App + .NET MAUI Blazor Hybrid + Shared RCL |
| Web render mode | Auto (SSR + WebAssembly) |
| Data sharing | **Per-user (private) + per-group (shared)** — resolved in v2, replacing v1's per-device model |
| Persistence format | **PostgreSQL (relational), accessed via EF Core and a Web API tier** — replaces v1's JSON file (browser localStorage on web; app data directory on mobile) |
| API tier *(new in v2)* | ASP.NET Core Web API — can be hosted alongside or reuse the Blazor Web App host process; both the Blazor web client and the MAUI Hybrid app call it over HTTP(S) instead of touching storage directly |
| Authentication | **Minimal authentication pulled forward as a hard prerequisite for group functionality.** **[OPEN QUESTION — Auth mechanism]** V1 deferred authentication entirely to "later phase." Group functionality cannot work without identifying distinct users — you cannot have a 10-person group, invite links, or per-user ownership without a `User` identity. **Recommended direction:** implement a minimal identity mechanism now (email/password via ASP.NET Core Identity, or a lightweight passwordless/email-link flow), sufficient only to identify users and support group membership; defer the fuller auth UX (password reset, social login, MFA, etc.) to a later phase, consistent with v1's original intent of keeping auth UX minimal at first. |
| Session/token mechanism *(new in v2 — architecture review finding)* | **Resolved per client type, not left to implementation.** Blazor Web (Auto render mode) uses cookie-based auth with an explicit server↔WASM auth-state handoff (e.g., `PersistingAuthenticationStateProvider`) so SSR and WASM stay authenticated as one session; the MAUI Hybrid app has no shared cookie jar and instead uses a bearer/JWT token stored in secure platform storage and attached to every API call. Both flows terminate at the same `ManageFamilyMeals.Api` identity endpoints (FR-29). This closes the "auth works on web, silently fails on mobile" risk identified in review. |
| Group size cap | Hard cap of **10 members per group**, enforced at invite/join time (see [Ownership & Permissions Model](#ownership--permissions-model)) |
| Group membership scope | **[OPEN QUESTION — Multi-group membership]** The task scope refers to "a group" (singular). **Recommended direction:** restrict each user to **one group at a time** for v2 to keep the ownership model simple; revisit multi-group membership in a later phase. |
| Delete behavior | Soft delete / archive for 7 days, then permanent purge (now applies equally to private and shared/group-owned content) |
| Favorites UX | Both: dedicated favorites section + favorites sorted to top |
| Link fields | Title + URL + optional link preview (WhatsApp-style OG card); optionally calorie/nutritional info when shared in a group context (see [Data Model](#9-data-model)) |
| Default language | Follow device/browser locale (en or ar) |
| Delivery priority | Web first, then mobile |
| Product name | **Manage Family Meals** |

---

## 5. User Stories

| ID | Story | Priority |
|----|-------|----------|
| US-01 | As a user, I see all categories on the home page | Must |
| US-02 | As a user, I create a new category | Must |
| US-03 | As a user, I archive a category (recoverable for 7 days) | Must |
| US-04 | As a user, I mark a category as favorite | Must |
| US-05 | As a user, I open a category and see its links | Must |
| US-06 | As a user, I add a link with title and URL | Must |
| US-07 | As a user, I see a preview card when adding a link | Should |
| US-08 | As a user, I archive a link (recoverable for 7 days) | Must |
| US-09 | As a user, I mark a link as favorite | Must |
| US-10 | As a user, I switch between English and Arabic | Must |
| US-11 | As a user, I see correct RTL layout in Arabic | Must |
| US-12 | As a user, I restore archived items within 7 days | Must |
| US-13 | As a user, I register and log in with a minimal identity (email/password, or a lightweight passwordless/email-link flow) so the app can distinguish me from other users | **Must** *(was "Later" in v1 — moved up because group functionality depends on it; see [OPEN QUESTION — Auth mechanism](#open-question--auth-mechanism-for-groups))* |
| US-14 | As a user, I create a new group and automatically become its admin | Must |
| US-15 | As a user, I join an existing group using an invite code/link | Must |
| US-16 | As a user, I view the list of members in my group | Should |
| US-17 | As a user, I leave a group I currently belong to | Should |
| US-18 | As a group admin, I remove a member from my group | Should |
| US-19 | As a user, I am blocked with a clear message if I try to join a group that already has 10 members | Must |
| US-20 | As a user, I mark one of my private categories or links as shared with my group, or keep it private | Must |
| US-21 | As a user, I see my private content and my group's shared content together on one home page, with a clear visual indicator distinguishing shared items | Must |
| US-22 | As a user, I filter my home/category view to show only "mine," only "shared," or "all" | Should |
| US-23 | As a group admin, I delete my group or transfer admin ownership to another member | Later |

---

## 6. Functional Requirements

### 6.1 Home Page (Categories)

- **FR-01:** Display favorite categories in a dedicated section
- **FR-02:** Display all active categories below, with favorites sorted first
- **FR-03:** Create category (name required)
- **FR-04:** Archive category (soft delete; cascades to its links)
- **FR-05:** Toggle category favorite
- **FR-06:** Navigate to category detail page
- **FR-07:** Show link count per category

### 6.2 Category Detail Page

- **FR-08:** Display favorite links in a dedicated section
- **FR-09:** Display all active links, favorites sorted first
- **FR-10:** Add link (URL required; title optional)
- **FR-11:** Fetch link preview (title, description, image, site name) server-side. *(Updated in v2 — architecture review finding: SSRF risk.)* "Valid HTTP(S) URL" alone is not a safe filter — a server-side fetch of a user-supplied URL can be pointed at internal/private-network addresses or cloud metadata endpoints (e.g., `169.254.169.254`). The fetch **must**: resolve the target host and reject private/link-local/loopback/metadata IP ranges before connecting; reject any redirect that resolves to a non-public IP; and enforce a request timeout and a maximum response size
- **FR-12:** Archive link (soft delete)
- **FR-13:** Toggle link favorite
- **FR-14:** Open link in external browser

### 6.3 Archive Page

- **FR-15:** List archived categories with restore action
- **FR-16:** List archived links with restore action
- **FR-17:** Display 7-day retention notice
- **FR-18:** Permanently purge items archived more than 7 days on app load

### 6.4 Localization

- **FR-19:** Language switcher (English / Arabic)
- **FR-20:** All UI strings via RESX resource files
- **FR-21:** RTL layout (`dir="rtl"`) when Arabic is active
- **FR-22:** Default language from browser/device locale on first visit
- **FR-23:** Persist language preference in local storage

### 6.5 Data & Persistence *(rewritten for v2 — PostgreSQL replaces JSON/localStorage)*

- **FR-24:** Persist all entities (users, groups, categories, links, settings) in **PostgreSQL** via **EF Core**, replacing v1's client-side JSON serialization
- **FR-25:** All reads/writes from both the Blazor web client and the MAUI Hybrid mobile client go through a **Web API tier** (ASP.NET Core Web API) over HTTP(S) — no client persists data locally as the system of record; local/offline caching, if any, is a later-phase concern
- **FR-26:** Manage schema evolution via **EF Core migrations**, applied as part of deployment
- **FR-27:** Data survives browser refresh, app restart, and device change — a user's data (and their group's shared data) is available from any device they log into, which directly resolves v1's deferred "multi-device data sync"

### 6.6 Authentication (Minimal, Pulled Forward) *(new in v2)*

- **FR-28:** User registers and creates an account with a minimal identity (email/password or passwordless email-link — see [OPEN QUESTION — Auth mechanism](#open-question--auth-mechanism-for-groups))
- **FR-29:** User logs in; an authenticated session/token is honored consistently by both the Blazor web client and the MAUI Hybrid app, using the concrete per-client mechanism specified in [Section 4](#4-approved-architecture-decisions) (cookie + auth-state handoff for Blazor Auto; bearer/JWT in secure storage for MAUI) — not left as an undecided implementation detail
- **FR-30:** Every category and link is associated with an owning `User` at creation time, defaulting to **private** (visible only to the creator) unless explicitly shared to a group (FR-37)

### 6.7 Groups & Sharing *(new in v2)*

- **FR-31:** Create a group (name required); the creator is automatically assigned the **Admin** role for that group (see [Ownership & Permissions Model](#ownership--permissions-model))
- **FR-32:** Generate a unique invite code/link for a group, shareable outside the app (e.g., via messaging apps)
- **FR-33:** Join a group by redeeming a valid invite code/link. **[OPEN QUESTION — Join mechanism]** Should joining be invite-only, or should groups be publicly discoverable/searchable? **Recommended direction:** invite code/link only — no public group discovery — consistent with this being a small family/friends app, not a social platform
- **FR-34:** View the current list of group members and their roles
- **FR-35:** Leave a group voluntarily
- **FR-36:** Enforce a hard cap of **10 members per group**; reject any join attempt beyond the cap. **[OPEN QUESTION — Behavior at cap]** What should happen on the 11th join attempt against a full group? **Recommended direction:** block the join with a clear "this group is full (10/10)" message; no waitlist feature for v2
- **FR-37:** Toggle a personal category or link between **Private** (owned by the User) and **Shared** (owned by the Group) — this includes categories/links carrying calorie or nutritional info
- **FR-38:** Unified home view merges the user's private categories/links and their group's shared categories/links into a single list, with a visual badge/indicator (e.g., a "Shared" tag or icon) distinguishing shared items from private ones
- **FR-39:** Filter the home/category view by **Mine**, **Shared**, or **All**

### 6.8 Data Integrity, Concurrency & Migration *(new in v2 — architecture review findings)*

- **FR-40:** Enforce, at the database level, that every `MealCategory`/`MealLink` row has **exactly one** owner consistent with its `OwnerType` — a Postgres `CHECK` constraint (or equivalent) rejecting rows where `OwnerUserId`/`OwnerGroupId` are both null, both set, or set to the field that doesn't match `OwnerType`. This is not optional application-layer validation; it must hold even if application code has a bug, because the authorization layer trusts these columns directly.
- **FR-41:** Protect shared (group-owned) content from lost updates under concurrent edits using **optimistic concurrency** (a row version/concurrency token on `MealCategory` and `MealLink`). When two members edit or archive the same item concurrently, the second write must fail with a detectable conflict rather than silently overwriting the first; the UI surfaces a "this was changed by someone else — reload and retry" message rather than swallowing the conflict.
- **FR-42:** Foreign keys from `MealCategory`/`MealLink` to `User`/`Group` use **`RESTRICT`/`NoAction`** on delete, not `SetNull` or cascade — a user or group cannot be deleted while it still owns content. Deleting a group requires all of its `OwnerGroupId` content to be reassigned or archived within the same transaction as the delete, so no row can ever end up with `OwnerType = Group` and a null/dangling `OwnerGroupId`.
- **FR-43:** Provide a one-time migration path for existing v1 users' JSON/localStorage data into PostgreSQL at v2 cutover (e.g., an import tool or endpoint that reads a user's exported v1 JSON and creates the equivalent private `MealCategory`/`MealLink` rows owned by their new `User` account). V1 users' existing data must not be silently stranded when v2 ships.

---

## 7. Ownership & Permissions Model *(new section in v2)*

Group functionality requires every shared category and link to have a clear owner and a clear set of rules for who can act on it. This section defines that model, since it did not exist in v1 (which had no concept of shared data at all).

- **Ownership types.** Every `MealCategory` and `MealLink` is owned by exactly one of: a specific `User` (private — visible only to that user) or a `Group` (shared — visible to all current members of that group). See [Data Model](#9-data-model) for the schema representation.

- **Who can edit or archive shared (group-owned) content?** **[OPEN QUESTION — Edit/archive permissions on shared content]** Can any group member edit or archive shared content, or only the member who originally created it? **Recommended direction:** allow **any group member** to edit or archive shared content, since the point of group sharing is collaborative meal planning (e.g., one person adds a link, another corrects the title or archives a stale one). This is flagged explicitly as a **product decision**, not just a technical one — it affects trust and expectations among group members, and should be confirmed with the product owner before implementation, since the alternative (creator-only edit rights) is equally defensible for a smaller, more controlled sharing model.

- **What happens to shared content when its creator leaves the group?** **[OPEN QUESTION — Content orphaning on member departure]** If the member who created a shared category/link leaves or is removed from the group, does that content leave with them, get deleted, or stay behind? **Recommended direction:** shared content's ownership is the `Group`, not the individual member who created it — so when a member leaves, their previously-shared content **stays with the group**. This avoids the group losing data every time membership changes and is consistent with content being owned by `GroupId`, not by a "creator" foreign key, in the data model below. *(Architecture review, FR-42: this is enforced at the database level via `RESTRICT`/`NoAction` foreign keys, not just application convention — a group cannot be deleted while it still owns content, preventing orphaned or dangling ownership rows.)*

- **Does a group have an admin/owner role, or are all members equal?** **[OPEN QUESTION — Group roles]** **Recommended direction:** the user who creates a group becomes its **Admin** by default (distinct from regular `Member`s), with the ability to remove members and to delete or transfer ownership of the group (US-18, US-23). Regular members can view/share/edit content per the rule above, but cannot manage membership or the group itself. A group could later support multiple admins or role transfer on leave, but v2 keeps it simple: one Admin per group, assigned at creation.

- **How is the 10-member cap enforced at invite time?** Enforcement happens at the moment a join request (via invite code/link) is processed: the API validates current member count for the target group before creating the new `GroupMembership` row, inside a transaction, to avoid race conditions where two people redeem the last slot simultaneously. If the group is already at 10, the join is rejected per FR-36's recommended direction (clear "group is full" message, no waitlist).

- **Multi-group membership.** As noted in [Section 4](#4-approved-architecture-decisions), v2 restricts each user to one group at a time (**[OPEN QUESTION — Multi-group membership]**, recommended: one group per user for v2). This keeps `User.GroupId` a simple nullable foreign key rather than a many-to-many relationship, and keeps the "mine vs. shared" home view unambiguous (there is at most one "shared" bucket per user).

---

## 8. Non-Functional Requirements

| Area | Requirement |
|------|-------------|
| Performance | Pages load in < 1s on typical home network; API round-trips for common reads (home page, category detail) target < 300ms server-side under expected load |
| Reliability | No data loss on normal shutdown; PostgreSQL provides durable, transactional writes, replacing v1's "no data loss" guarantee that depended on browser/device storage persistence |
| Accessibility | Keyboard-navigable forms on web |
| Maintainability | Shared models/services/components in RCL; data-access logic centralized behind the Web API/EF Core layer rather than duplicated per client |
| Data Integrity *(new in v2 — architecture review finding)* | Every `MealCategory`/`MealLink` row's ownership is enforced at the database level, not just in application code — a Postgres `CHECK` constraint rejects any row whose `OwnerType`/`OwnerUserId`/`OwnerGroupId` combination is invalid (FR-40), so the authorization layer can trust these columns unconditionally |
| Scale | ~10 users **per group**, an initially small number of groups; low concurrency per group, and concurrent writes to shared (group-owned) content are protected by optimistic concurrency (a `RowVersion` token) rather than left as an unenforced assumption — the second of two simultaneous edits fails with a detectable conflict instead of silently overwriting the first (FR-41) |
| Visual Consistency *(new in v2 — delivery-planning addition)* | A single design language (palette, typography, spacing, shared component styles) is applied consistently across every screen delivered in this phase, on both the Blazor web client and the MAUI Hybrid app, rather than accumulating ad-hoc/default styling per feature as it ships. Delivered as a dedicated pass after core functionality (web + mobile) is in place, not spread across each feature epic. |
| Security | **Minimal authentication is now required** (see FR-28/FR-29 and [OPEN QUESTION — Auth mechanism](#open-question--auth-mechanism-for-groups)) — this is a change from v1's "No auth" POC posture. Authorization must enforce the ownership model: a user may read/write their own private content and their group's shared content, but not another user's private content or another group's shared content. Session handling follows the per-client mechanism resolved in [Section 4](#4-approved-architecture-decisions) (cookie + auth-state handoff for Blazor Auto; bearer/JWT for MAUI), rather than being an unspecified detail. Link preview fetching remains server-side only, and must additionally block requests to private/link-local/loopback/metadata IP ranges and unsafe redirects (FR-11 — architecture review finding: SSRF risk). |

---

## 9. Data Model

```
User                                    (new in v2)
├── Id: Guid
├── Email: string
├── DisplayName: string
├── PasswordHash: string?                 // or minimal passwordless identity — see OPEN QUESTION: Auth mechanism
├── PreferredCulture: string?
├── GroupId: Guid?                        // nullable FK — one group per user in v2 (see OPEN QUESTION: Multi-group membership)
└── CreatedAtUtc: DateTime

Group                                   (new in v2)
├── Id: Guid
├── Name: string
├── InviteCode: string                    // unique, used for join via FR-33
├── CreatedByUserId: Guid                 // the creator; becomes Admin (see Ownership & Permissions Model)
└── CreatedAtUtc: DateTime

GroupMembership                         (new in v2)
├── Id: Guid
├── GroupId: Guid
├── UserId: Guid
├── Role: enum { Admin, Member }          // see OPEN QUESTION: Group roles
└── JoinedAtUtc: DateTime

MealCategory
├── Id: Guid
├── Name: string
├── IsFavorite: bool
├── CreatedAtUtc: DateTime
├── IsDeleted: bool
├── DeletedAtUtc: DateTime?
├── OwnerType: enum { User, Group }       // new in v2 — ownership model; DB CHECK constraint enforces exactly one of OwnerUserId/OwnerGroupId set, matching OwnerType (FR-40)
├── OwnerUserId: Guid?                    // new in v2 — set when OwnerType = User (private); FK is RESTRICT/NoAction on delete (FR-42)
├── OwnerGroupId: Guid?                   // new in v2 — set when OwnerType = Group (shared); FK is RESTRICT/NoAction on delete (FR-42)
└── RowVersion: byte[]                    // new in v2 — optimistic concurrency token for shared-edit conflict detection (FR-41)

MealLink
├── Id: Guid
├── CategoryId: Guid
├── Title: string
├── Url: string
├── IsFavorite: bool
├── CreatedAtUtc: DateTime
├── IsDeleted: bool
├── DeletedAtUtc: DateTime?
├── PreviewTitle: string?
├── PreviewDescription: string?
├── PreviewImageUrl: string?
├── PreviewSiteName: string?
├── Calories: int?                        // new in v2 — supports "calorie/nutritional info" sharing and future filtering
├── NutritionalInfo: string?              // new in v2 — free-form/structured nutritional notes; future phase may normalize into its own table
├── OwnerType: enum { User, Group }       // new in v2 — ownership model; DB CHECK constraint enforces exactly one of OwnerUserId/OwnerGroupId set, matching OwnerType (FR-40)
├── OwnerUserId: Guid?                    // new in v2 — set when OwnerType = User (private); FK is RESTRICT/NoAction on delete (FR-42)
├── OwnerGroupId: Guid?                   // new in v2 — set when OwnerType = Group (shared); FK is RESTRICT/NoAction on delete (FR-42)
└── RowVersion: byte[]                    // new in v2 — optimistic concurrency token for shared-edit conflict detection (FR-41)

AppSettings
└── CultureCode: string?
```

**Referential integrity notes (why this matters for the PostgreSQL decision):** archiving a `MealCategory` must cascade to its `MealLink`s (unchanged from v1, FR-04). A `Group` being deleted must resolve what happens to its `MealCategory`/`MealLink` rows still owned by `OwnerGroupId` — per the [Ownership & Permissions Model](#ownership--permissions-model), shared content survives individual member departures because ownership is the group, not a member; a full group deletion (US-23) is a separate, explicit action and should be handled with an application-level decision (e.g., require re-assigning or archiving shared content before a group can be deleted) rather than a silent cascade delete, to avoid accidental data loss. This kind of integrity constraint is exactly what a relational database with foreign keys is suited to express and enforce.

**Architecture review additions (FR-40–FR-42):** the ownership columns above are not just documentation — they are backed by an explicit DB `CHECK` constraint (FR-40) so a mismatched `OwnerType`/FK combination is a rejected write, not a silent bad row that the authorization layer would trust. The `OwnerUserId`/`OwnerGroupId` foreign keys use `RESTRICT`/`NoAction` rather than EF Core's common `SetNull` default (FR-42) — `SetNull` would otherwise leave `OwnerType = Group` with a null `OwnerGroupId` the moment a group row is deleted, which is exactly the invalid state FR-40's constraint exists to prevent. The `RowVersion` concurrency token (FR-41) exists specifically because §7 allows any group member to edit shared content — without it, two concurrent edits to the same row produce a silent lost update with no conflict signal to either user.

---

## 10. Solution Structure

```
ManageFamilyMeals.slnx
src/
├── ManageFamilyMeals.Shared/       # Models, services, RESX localization
├── ManageFamilyMeals.Api/          # (New in v2) ASP.NET Core Web API — EF Core + PostgreSQL provider
│                                   #   data access, auth endpoints, group/membership services,
│                                   #   ownership/authorization checks (mine vs. shared)
├── ManageFamilyMeals.Web/          # Blazor Web App host (Auto render mode)
│   └── ManageFamilyMeals.Web.Client/  # Interactive WASM components & pages — calls ManageFamilyMeals.Api over HTTP
└── ManageFamilyMeals.Mobile/       # .NET MAUI Blazor Hybrid — calls ManageFamilyMeals.Api over HTTP
                                    #   (no longer reads/writes a local JSON file directly, per the PostgreSQL decision)
```

Notes on the new `ManageFamilyMeals.Api` project:

- Hosts EF Core `DbContext` and the PostgreSQL provider (e.g., `Npgsql.EntityFrameworkCore.PostgreSQL`)
- Can be deployed as a standalone process or co-hosted within the `ManageFamilyMeals.Web` host process for the initial v2 rollout to minimize new infrastructure — either approach satisfies the requirement that the MAUI client talks to a server-side API rather than local storage
- Owns the new **group/membership services**: create group, generate/redeem invite code, enforce 10-member cap, role assignment (Admin/Member), leave/remove member
- Owns the **ownership/authorization layer**: every request resolves the current user, checks whether the target `MealCategory`/`MealLink` is owned by that user or by that user's group, and rejects access otherwise
- Exposes authentication endpoints (register/login) per FR-28/FR-29
- *(Architecture review findings, closed as API responsibilities in v2)*:
  - Enforces the ownership invariant via a database `CHECK` constraint on `OwnerType`/`OwnerUserId`/`OwnerGroupId`, in addition to any application-layer validation (FR-40)
  - Detects and surfaces optimistic-concurrency conflicts (`RowVersion` mismatches) on updates to shared content as a distinct, handleable error rather than an overwrite (FR-41)
  - Performs link-preview fetches through an SSRF-safe fetcher that resolves and rejects private/link-local/loopback/metadata-range IPs and unsafe redirects before connecting, with a timeout and response-size cap (FR-11)
  - Hosts a one-time v1→v2 migration endpoint/tool that imports a user's exported JSON/localStorage data into PostgreSQL as private content owned by their new account (FR-43)

---

## 11. Out of Scope (Current Phase)

- ~~Email registration and login~~ — **moved into scope for v2** as minimal authentication (see [OPEN QUESTION — Auth mechanism](#open-question--auth-mechanism-for-groups)); the fuller auth UX (password reset, social login, MFA) remains out of scope
- ~~Multi-device data sync~~ — **resolved and now in scope for v2** via PostgreSQL + Web API (FR-27)
- ~~Shared/cloud storage~~ — **resolved and now in scope for v2** via PostgreSQL as the system of record
- .NET MAUI mobile app wiring to the new API tier (targeted for this phase's rollout, alongside web)
- Push notifications
- Link import/export
- Drag-and-drop reordering
- Dark mode
- App store deployment
- Multi-group membership per user (deferred — see [OPEN QUESTION — Multi-group membership](#4-approved-architecture-decisions))
- Public/discoverable group search (deferred — invite-only for v2, see [OPEN QUESTION — Join mechanism](#6-functional-requirements))
- Waitlists for full groups (deferred — see [OPEN QUESTION — Behavior at cap](#6-functional-requirements))
- Fuller authentication UX: password reset, social login (Google/Microsoft/Apple), multi-factor authentication

---

## 12. Validation Checklist

*(Renamed from v1's "POC Validation Checklist" — group functionality and PostgreSQL persistence are this phase's active work, not a future phase.)*

| # | Criterion | Status |
|---|-----------|--------|
| 1 | Create/delete (archive) category | Implemented (v1) |
| 2 | Create/delete (archive) link | Implemented (v1) |
| 3 | Favorites toggle (category + link) | Implemented (v1) |
| 4 | Favorites section + sort-to-top | Implemented (v1) |
| 5 | EN ↔ AR language switch | Implemented (v1) |
| 6 | RTL layout in Arabic | Implemented (v1) |
| 7 | Link preview (OG metadata) | Implemented (v1) |
| 8 | 7-day archive with restore | Implemented (v1) |
| 9 | Shared RCL compiles for web | Implemented (v1) |
| 10 | Data persisted in PostgreSQL via EF Core (replacing JSON/localStorage) | Not started |
| 11 | Web API tier deployed and reachable by both Blazor web and MAUI mobile clients | Not started |
| 12 | Minimal authentication: register/login (email/password or passwordless) | Not started |
| 13 | Create a group; creator becomes Admin | Not started |
| 14 | Join a group via invite code/link | Not started |
| 15 | Enforce max-10-member cap on group join | Not started |
| 16 | Toggle a category/link between Private and Shared | Not started |
| 17 | Unified home view shows private + shared content with a visual "shared" indicator | Not started |
| 18 | Filter home view by Mine / Shared / All | Not started |
| 19 | Leave a group; shared content remains with the group afterward | Not started |
| 20 | Archive of a category cascades to its links regardless of ownership (private or shared) | Not started |
| 21 | DB `CHECK` constraint rejects an `OwnerType`/`OwnerUserId`/`OwnerGroupId` mismatch (FR-40) | Not started |
| 22 | Concurrent edits to the same shared category/link produce a detectable conflict, not a silent overwrite (FR-41) | Not started |
| 23 | Link preview fetch blocks private/link-local/loopback/metadata IPs and unsafe redirects (FR-11) | Not started |
| 24 | Deleting a group is blocked (or requires reassignment/archiving first) while it still owns content, verifying `RESTRICT`/`NoAction` FK behavior (FR-42) | Not started |
| 25 | V1 JSON/localStorage export imports successfully into PostgreSQL as private content for the importing user (FR-43) | Not started |

---

## 13. Next Phase

*(Updated from v1 §11 — mobile wiring, auth, and sync/sharing are now this phase's scope, not a future one.)*

1. **This phase (v2):**
   - Stand up `ManageFamilyMeals.Api` with EF Core + PostgreSQL, and migrate persistence off JSON/localStorage
   - Implement minimal authentication (FR-28/FR-29)
   - Implement group create/join/leave/membership and the ownership model (FR-31–FR-39)
   - Wire the `ManageFamilyMeals.Mobile` MAUI Blazor Hybrid project to call the new API instead of local storage
   - Close all six architecture-review findings as delivered requirements, not open risks: ownership `CHECK` constraint (FR-40), optimistic concurrency on shared content (FR-41), `RESTRICT`/`NoAction` FK behavior on group deletion (FR-42), the per-client session/token model (§4, FR-29), SSRF-safe link preview fetching (FR-11), and the v1→v2 data migration path (FR-43)
   - Apply a dedicated visual style/theming pass (see Visual Consistency, §8) once web and mobile functionality are both in place, so styling work covers the full surface area once rather than being redone per feature
   - Delivery is broken into eight dependency-sequenced epics tracked in `docs/tickets/` (E1 API+PostgreSQL foundation → E2 authentication → E3 groups/ownership → E4 sharing/unified home → E5 membership management → E6 security/migration hardening → E7 mobile → E8 visual style/theming), each implemented and reviewed independently before the next begins
2. **Following phase (proposed, pending approval):**
   - Fuller authentication UX: password reset, social login, multi-factor authentication
   - Multi-group membership per user, if the product decision changes from the v2 one-group-per-user recommendation
   - Structured nutritional data (beyond the free-form `NutritionalInfo` field) to support richer calorie/nutrition filtering and search
   - Offline/local caching on mobile for degraded-connectivity scenarios

---

## 14. Open Questions

These consolidate every `[OPEN QUESTION]` raised inline above, plus v1's original five (still unresolved), renumbered together. Each includes its recommended direction for quick reference.

**Carried forward from v1 (§12), still unresolved:**

1. Category icons or colors?
2. Confirm-before-archive dialog?
3. Search/filter for links?
4. Manual reorder of categories/links?
5. Hosting target for **web** (local IIS, Azure, home server)? — **extended in v2:** this now also covers hosting for **PostgreSQL** itself. **Recommended direction:** favor a managed cloud PostgreSQL service (e.g., Azure Database for PostgreSQL) over self-hosting, to avoid taking on backup/patching/HA operations for a ~10-users-per-group app; confirm alongside the web hosting decision since they will likely share the same cloud environment.

**New in v2:**

6. **Auth mechanism for groups.** What minimal authentication mechanism should be used? **Recommended direction:** email/password via ASP.NET Core Identity, or a lightweight passwordless/email-link flow; treat this as a hard prerequisite for group functionality while deferring password reset, social login, and MFA to a later phase.
7. **Edit/archive permissions on shared content.** Can any group member edit/archive shared content, or only its creator? **Recommended direction:** any member can edit/archive shared content, to support collaborative meal planning — flagged as a product decision, not purely technical.
8. **Content orphaning on member departure.** What happens to a group's shared content when its creating member leaves or is removed? **Recommended direction:** shared content stays with the group (ownership is the group, not the departing member).
9. **Group roles.** Does a group have a designated Admin (its creator) with extra powers, or are all members equal? **Recommended direction:** creator becomes Admin by default, able to remove members and delete/transfer the group.
10. **Join mechanism.** Should joining a group use an invite code/link, or open discovery? **Recommended direction:** invite code/link only — no public group discovery.
11. **Behavior at the group cap.** What happens on the 11th join attempt against a full (10/10) group? **Recommended direction:** block with a clear "group is full" message; no waitlist for v2.
12. **Multi-group membership.** Can a user belong to more than one group in v2? **Recommended direction:** one group per user for v2, to keep the ownership model simple; revisit later.

---

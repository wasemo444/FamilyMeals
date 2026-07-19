# E3 — Groups Core & Ownership Integrity

## Goal

Introduce `Group` and `GroupMembership` entities and the ownership model (`OwnerType`/`OwnerUserId`/`OwnerGroupId`) on `MealCategory`/`MealLink`, plus the data-integrity hardening the architecture review called for (CHECK constraint, optimistic concurrency, FK delete behavior). This is the schema/ownership foundation E4 and E5 build on — it does not yet expose sharing UI.

## Depends On

E2 (need authenticated users before anything can be "owned").

## In Scope (PRDV2 references)

- US-14 (create a group), and the data-model portion of the group stories
- FR-31, FR-32 (partially — creation only; full membership management is E5), FR-40, FR-41, FR-42
- §7 Ownership & Permissions Model — orphaning behavior (locked default: content stays with the group, not deleted, if a member leaves/is removed), group roles (locked default: creator becomes Admin)
- §9 Data Model — `Group`, `GroupMembership`, `OwnerType`/`OwnerUserId`/`OwnerGroupId`/`RowVersion` columns
- Validation Checklist #14, #21, #22, #23

## Acceptance Criteria

- `Group` entity (Id, Name, CreatedAtUtc, CreatedByUserId) and `GroupMembership` entity (GroupId, UserId, Role, JoinedAtUtc) exist with a migration.
- `MealCategory` and `MealLink` gain `OwnerType` (User | Group), `OwnerUserId`, `OwnerGroupId`, and `RowVersion` columns via migration; existing rows are backfilled with `OwnerType = User` and `OwnerUserId` set to their creator.
- A DB-level CHECK constraint enforces exactly one of `OwnerUserId`/`OwnerGroupId` is set based on `OwnerType` (FR-40).
- `RowVersion` is wired for EF Core optimistic concurrency; a concurrent-edit conflict returns a 409/conflict result instead of silently overwriting (FR-41).
- Foreign keys from `MealCategory`/`MealLink` to `Group`/`User` use `RESTRICT`/`NoAction` delete behavior, not cascade delete, per FR-42 (a group or user with existing content cannot be deleted outright — deletion path for groups is out of scope here, just the FK behavior).
- A user can create a group and becomes its Admin (creator-becomes-Admin default, no group-role UI beyond this yet).
- A user can create categories/links owned by themselves (`OwnerType = User`) — this is just the existing E1/E2 behavior now expressed through the new ownership columns; owning content by a group is schema-ready but not yet reachable from UI (that's E4).
- Validation Checklist rows #14, #21, #22, #23 flip to "Implemented."

## Out of Scope

- Sharing UI, unified home view combining personal + group content (E4).
- Invite/join flow, adding/removing members, role changes beyond creator-as-Admin (E5).
- Group size cap enforcement UI/messaging (locked default: block at cap, no waitlist — enforced in E5 when membership management exists).
- Mobile (E7).

## Likely Files/Areas

- `src/ManageFamilyMeals.Api/` — `Group`, `GroupMembership` entities, EF configs, migration (schema + backfill), CHECK constraint (raw SQL in migration or `HasCheckConstraint`), concurrency token config, FK delete-behavior config, group-creation endpoint.
- `src/ManageFamilyMeals.Shared/Models/` — shared `Group`/ownership DTOs if UI needs them.
- Existing `MealCategory`/`MealLink` entity configs — updated for new columns.

## Manual Test Notes

- Create a group via API, confirm creator is recorded as Admin in `GroupMembership`.
- Attempt to delete a user/group that owns content and confirm the FK constraint blocks it (no cascade wipe).
- Edit the same category from two concurrent requests with stale `RowVersion` and confirm the second write is rejected as a conflict, not silently applied.

# E4 — Sharing & Unified Home View

## Goal

Let users create/move categories and links into a group they belong to, and see a single home view that blends their personal content with their group's shared content. This is the epic that makes groups actually visible and useful in the UI.

## Depends On

E3 (ownership columns and groups must exist).

## In Scope (PRDV2 references)

- US-15, US-16, US-17 (view/create shared content, unified home view)
- FR-33, FR-34, FR-35
- §7 Ownership & Permissions Model — edit/archive permissions on shared content (resolved this session: any group member can edit/archive)
- Validation Checklist #15, #16, #17, #18

## Acceptance Criteria

- When creating a category or link, a user who belongs to a group can choose to own it personally or assign it to the group (`OwnerType`/`OwnerGroupId` set accordingly).
- The home view shows both the user's personal categories and their group's shared categories, clearly distinguishing which is which (e.g., an "owner" badge/label), sorted per existing favorites-first rule within each set.
- Any member of the group that owns a category/link can edit or archive it (not just the original creator) — implements the "any group member" permission decision.
- A user with no group memberships sees only their personal content, unchanged from E1–E3 behavior (no regression for non-group users).
- Attempting to edit/archive content owned by a group the current user is not a member of is rejected (403/not found) at the API layer, not just hidden in the UI.
- Validation Checklist rows #15–#18 flip to "Implemented."

## Out of Scope

- Managing who is in the group (invite, remove, roles beyond the E3 creator-as-Admin default) — E5.
- Group size cap enforcement — E5.
- SSRF hardening, data migration tooling — E6.
- Mobile (E7).

## Likely Files/Areas

- `src/ManageFamilyMeals.Api/` — endpoints for creating group-owned content, authorization checks (member-of-owning-group) on edit/archive endpoints for categories and links.
- `src/ManageFamilyMeals.Shared/` — DTOs to carry owner info (personal vs. group + group name) to the UI.
- `src/ManageFamilyMeals.Web.Client/Pages/` — home page updated to render combined personal + group sections; category/link create forms updated with an owner picker (personal vs. one of the user's groups).

## Manual Test Notes

- As a member of a group, create a link owned by the group; confirm a second member of the same group can see and edit it.
- Confirm a user outside the group cannot see or modify that group's content via direct API calls.
- Confirm a user with zero groups sees an unchanged, personal-only home view.

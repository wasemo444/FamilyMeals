# E5 — Group Membership Management

## Goal

Let group Admins invite and remove members, and enforce the group size cap. Completes the group lifecycle that E3 (schema) and E4 (sharing) were built on top of.

## Depends On

E4 (sharing must exist so membership changes have something meaningful to affect).

## In Scope (PRDV2 references)

- US-18, US-19, US-20 (invite member, remove member, view member list)
- FR-36, FR-37, FR-38, FR-39
- §7/§14 — join mechanism (locked default: invite-only, no open/public join), behavior at group size cap (locked default: block new joins, no waitlist), multi-group membership (locked default: one group per user)
- Validation Checklist #19, #20

## Acceptance Criteria

- A group Admin can invite a user (by email, matching an existing registered account) to join the group.
- An invited user can accept or decline; accepting creates a `GroupMembership` row (role: Member unless promoted).
- A group Admin can remove a member; per E3's locked orphaning default, that member's previously-group-owned content stays with the group (is not deleted or reassigned).
- The group's member list is visible to all current members.
- Group size cap (value already defined in PRDV2 §4) is enforced: an invite/join that would exceed the cap is rejected with a clear error, no waitlist behavior.
- Enforce one-group-per-user: a user cannot accept a second group invite while already a member of a group (locked default from §14).
- Validation Checklist rows #19 and #20 flip to "Implemented."

## Out of Scope

- Changing a member's role beyond the E3 creator-as-Admin default (no promote/demote UI unless already implied by invite acceptance flow — keep role management minimal, just enough to satisfy FR-36–39).
- Public/open group discovery or join links (explicitly excluded by the invite-only decision).
- Invite code redemption join path (E5 uses email invite; `InviteCode` column retained for future use).
- Mobile (E7), migration tooling and SSRF hardening (E6).

## Status

**Implemented** — see [L2.md § Group Membership](../L2.md#group-membership-e5) and `GroupMembershipEndpointsTests`.

## Likely Files/Areas

- `src/LinkNest.Api/` — invite endpoint (Admin-only), accept/decline endpoint, remove-member endpoint (Admin-only), cap-check logic, one-group-per-user check.
- `src/LinkNest.Shared/` — invite/membership DTOs.
- `src/LinkNest.Web.Client/Pages/` — group members page (list, invite form, remove action), pending-invite view for the invited user.

## Manual Test Notes

- Invite a second test user to a group, accept the invite, confirm they can now see the group's shared content (from E4).
- Try inviting a user who is already a member of a different group; confirm it's rejected.
- Fill a group to its cap and confirm the next invite/join attempt is blocked with a clear message.
- Remove a member and confirm their previously-shared content remains visible to the rest of the group.

# E8 — Visual Style & Theming Polish

## Goal

Apply a cohesive visual identity pass across the app: consistent color palette, typography, spacing, and component styling on web (and mobile, once E7 exists), replacing the ad-hoc/default styling accumulated across the functional epics (E1–E7). This is a design-consistency pass, not new functionality — no new user stories or FRs are introduced.

## Depends On

E7 (all features should exist on both web and mobile before doing a full visual pass, so the styling work covers the complete surface area once, not repeatedly as new screens land).

## In Scope

- A defined design language: color palette (including favorite/shared/archived visual states), typography scale, spacing/grid rules, and shared component styles (buttons, cards, badges, forms) applied consistently across every existing screen (home, category detail, archive, login/register, group/members, invites).
- Shared CSS/styling lives in `ManageFamilyMeals.Shared` (or a shared stylesheet referenced by both Web and Mobile) so web and mobile don't visually diverge.
- Visual states already implied by existing FRs get consistent treatment under the new design language: the "shared" badge/indicator (FR-38), favorite sections vs. sort-to-top styling, RTL layout (FR-21) verified against the new styles in both directions.
- Accessibility carried forward, not regressed: keyboard-navigable forms remain keyboard-navigable after restyling (existing NFR, §8).

## Acceptance Criteria

- Every screen delivered in E1–E7 uses the same palette, spacing scale, and component styles — no screen still on default/unstyled Bootstrap or ad-hoc inline styles left over from earlier epics.
- The "shared vs. private" visual indicator (FR-38) is restyled consistently with the new design language, still clearly distinguishable at a glance.
- RTL (Arabic) rendering is re-verified after restyling — spacing/alignment/icons mirror correctly, not just LTR-checked.
- No functional regression: a full manual pass over the golden paths from E1–E7 (create/archive/favorite category & link, login, group create/invite/join, home view mine/shared) behaves identically to before the restyle.
- Mobile (MAUI) screens visually match their web counterparts using the same shared styling source, not a separately hand-tuned mobile theme.

## Out of Scope

- New features, new FRs, or new open-question resolutions (e.g., category icons/colors and confirm-before-archive dialog remain open questions in PRDV2 — not addressed here since the user chose "general visual polish/theming" scope for this epic, not those specific items).
- Dark mode (still explicitly out of scope per PRDV2 §11).
- Native platform-specific redesigns beyond what shared styling naturally produces.

## Likely Files/Areas

- `src/ManageFamilyMeals.Shared/wwwroot/` (or equivalent shared static assets) — shared CSS/design tokens (palette variables, spacing scale, typography).
- `src/ManageFamilyMeals.Web.Client/` — component markup/class updates to consume the shared design tokens instead of inline/default styles.
- `src/ManageFamilyMeals.Mobile/` — MAUI styling (`Styles.xaml`/shared CSS via Blazor Hybrid) aligned to the same tokens.

## Manual Test Notes

- Click through every screen on web in both English (LTR) and Arabic (RTL) and confirm consistent spacing, colors, and alignment.
- Run the same walkthrough on the mobile app and compare side-by-side with web for visual consistency.
- Re-run the E1–E7 golden-path checklist to confirm nothing functionally broke during restyling.

# E8 — Visual Style & Theming Polish

You are a senior front-end/UI engineer and design systems specialist. Help plan and spec a full visual identity pass for the LinkNest app — this is a design-consistency and polish sprint, not a feature sprint.


## The Task
Produce two things:

1. A design system spec — propose a complete visual direction from scratch (the user has no existing direction to match). Define color palette, typography scale, spacing tokens, and component rules. The app should feel trendy, modern, and distinctly human — not AI-scaffolded. The system must support both light and dark mode (explicitly required; dark mode is not out of scope).

2. A detailed implementation plan — tasks, file changes, and order of work to apply the design system across all E1–E7 screens.


## Depends On

E7 (all features should exist on both web and mobile before doing a full visual pass, so the styling work covers the complete surface area once, not repeatedly as new screens land).

## In Scope
Every screen from E1–E7 gets the same palette, spacing scale, and component styles — zero screens left on default Bootstrap or ad-hoc inline styles. Starting fresh means no meaningful prior styling needs to be preserved or migrated.

Where shared styles live:

Design tokens and shared CSS in src/LinkNest.Shared/wwwroot/ (or equivalent shared static assets) — single source of truth for web and mobile.
src/LinkNest.Web.Client/ — component markup consumes tokens.
src/LinkNest.Mobile/ — Styles.xaml / Blazor Hybrid shared CSS aligns to the same tokens so mobile matches web visually without a separately hand-tuned theme.
Shared component styles to define: buttons, cards, badges, forms, and navigation — styled once, consumed everywhere: home, category detail, archive, login/register, group/members, invites.

Animation and motion — use micro-interactions where they add polish and feel natural (hover states, transitions, loading skeletons, subtle entrance animations); skip them where they'd add noise.

Non-Negotiables to Plan Around
RTL (Arabic) — FR-21 — spacing, alignment, and icon mirroring must be re-verified under the new styles in both LTR and RTL directions. RTL is not an afterthought; the plan should treat it as a first-class concern.
Shared vs. private visual indicator — FR-38 — must remain clearly distinguishable at a glance within the new design language.
Accessibility — keyboard-navigable forms must remain keyboard-navigable after restyling.
No functional regression — every golden path from E1–E7 (create/archive/favorite category & link, login, group create/invite/join, home view mine/shared) must behave identically after the restyle.
Out of Scope
New features, new FRs, or open-question resolutions (category icons/colors, confirm-before-archive dialog).
Native platform-specific redesigns beyond what shared styling naturally produces.
What a Good Outcome Looks Like
A visual direction the user can evaluate and approve before implementation begins.
A plan specific enough that a developer could execute it file-by-file without ambiguity.
Web and mobile rendered from the same shared source, matching visually side-by-side in both light and dark mode.
The finished app looks and feels like a real, polished product.
# Product

<!-- impeccable:product-schema 1 -->

## Platform

adaptive

LinkNest ships as one product on **Blazor Web** (Auto SSR + WebAssembly, installable PWA) and **.NET MAUI Blazor Hybrid** (currently Windows TFM; Android/iOS TFMs planned). Both clients share Razor pages, business logic, and a planned shared CSS token layer. Native shell chrome (MAUI `Styles.xaml`) may diverge only where WebView styling cannot reach.

## Users

**Primary:** Individuals and small households or friend groups who save links to online content — videos, courses, articles, recipes, and similar — and want them organized by category with quick access to favorites.

**Secondary:** Group members (up to ~10 per group) who collaborate on shared link collections while keeping private categories and links visible only to themselves.

**Situation:** Daily or weekly link capture on phone or desktop; occasional browsing, favoriting, and archive cleanup; group admins invite members and manage shared content.

## Product Purpose

LinkNest makes it easy to **save, organize, and retrieve links** in categories, with **favorites**, **soft-delete archive**, and **optional group sharing** — all backed by a **central PostgreSQL database** accessed through a shared Web API so web and mobile stay in sync.

Success means a user can create a category and add a link in under 30 seconds, find favorites quickly, switch between English and Arabic (with correct RTL), and distinguish their private content from shared group content at a glance — on any supported screen size without functional regression.

## Positioning

Unlike a browser bookmark folder or a generic read-later app, LinkNest combines **per-user private collections** and **small-group shared collections** in one unified home experience, with **bilingual EN/AR + RTL**, **cross-platform parity from a single .NET codebase**, and **consistent persistence** across web and mobile through one API — not per-device JSON silos.

## Operating Context

- **Web:** Blazor Web App at `src/LinkNest.Web/` — cookie auth, SSR + WASM, PWA share-target and offline shell where configured.
- **Mobile:** MAUI Blazor Hybrid at `src/LinkNest.Mobile/` — JWT bearer auth stored in secure platform storage; reuses `LinkNest.Web.Client` pages.
- **API:** ASP.NET Core at `src/LinkNest.Api/` — identity, categories, links, groups, invites, link previews.
- **Data:** PostgreSQL via EF Core; Docker Compose for local development.
- **Epic context:** E1–E7 deliver core features; **E8** is a visual/theming/responsive pass (no new features); **E9** covers settings, deferred mobile TFMs, hosting, and store publishing.

**Key routes / surfaces (implemented):** Home, Category detail, Archive, Share target, Login, Register, Groups, Group members.

## Capabilities and Constraints

### Core capabilities (confirmed, implemented)

- Categories and links with title, URL, optional OG-style link preview.
- Favorites: dedicated section and favorites sorted to top within lists.
- Soft delete with 7-day archive before permanent purge.
- Authentication: cookie session on web; JWT bearer on mobile; both terminate at the same API identity endpoints.
- Groups: create, invite, join; member management; shared vs. private ownership on categories and links.
- **Multi-group membership:** implemented in current codebase and docs (PRD v2 recommended one group at a time — product may revisit; implementation supports multiple groups).
- Bilingual UI: English and Arabic with full RTL layout (FR-21).
- PWA share-target flow for incoming links.

### Product constraints future work must preserve

- **FR-38 — Mine vs. shared:** Visual distinction between private and group-shared content must remain obvious at all breakpoints and in both themes.
- **No functional regression:** Golden paths from E1–E7 (create/archive/favorite category & link, login, group create/invite/join, home mine/shared views) must behave identically after visual changes.
- **Responsive layout (E8):** Fully usable from ~320px phone widths through large desktop on web and MAUI WebView; mobile-first CSS; canonical breakpoints (`xs`–`xl`); min ~44×44px touch targets; `env(safe-area-inset-*)` where supported.
- **Theming (E8):** Light and dark mode both required; shared token source for web and mobile (target: `LinkNest.Shared/wwwroot/` or `LinkNest.UI/` once extracted).
- **Accessibility:** Keyboard-navigable forms after restyling; visible focus rings; WCAG AA contrast for text and controls; 200% zoom reflow (WCAG 1.4.10); respect `prefers-reduced-motion`.
- **RTL:** Spacing, alignment, icon direction, and collapsed navigation verified in Arabic at every breakpoint — not an afterthought.

### Explicitly undecided (do not invent)

- Category icons and color accents (open product question; E8 out of scope to resolve).
- Confirm-before-archive dialog behavior (deferred).
- Final production hosting provider and store release timeline (E9).

### Out of scope for E8 (product level)

- New features or new functional requirements.
- Native platform-specific UI beyond what shared CSS naturally produces.
- Hosting, deployment, and app store publishing (E9).

## Brand Commitments

- **Product name:** LinkNest (confirmed in PRD and codebase).
- **Voice:** Practical, friendly, task-focused — an organizer for saved links, not a social network.
- **No binding visual identity yet:** E8 explicitly starts visual direction from scratch; incumbent Bootstrap/ad-hoc styling is not a brand commitment to preserve.

## Evidence on Hand

| Asset | Location |
|-------|----------|
| Product requirements (v2) | `docs/PRDV2.md` |
| E8 visual/responsive epic spec | `docs/tickets/E8-visual-style-polish.md` |
| E9 carry-over, hosting, stores | `docs/tickets/E9-carry-over-from-previous-epic.md` |
| Architecture / agent context | `docs/L1.md`, `docs/L2.md`, `docs/L3.md`, `docs/agents.md` |
| Incumbent web styles (to be replaced) | `src/LinkNest.Web/LinkNest.Web/wwwroot/app.css` |
| README runbook | `README.md` |

**Absences — do not fabricate:** No marketing site, customer testimonials, pricing page, press coverage, or finalized design system (`DESIGN.md` not yet written). No production deployment URLs.

## Product Principles

1. **One truth, many clients** — Web and mobile read the same API and share UI logic; visual tokens should follow the same rule.
2. **Private and shared, clearly separated** — Collaboration works only if users always know what is theirs vs. the group's.
3. **Fast capture, calm browsing** — Optimize for adding and finding links, not engagement metrics or feeds.
4. **Language is a first-class layout concern** — Arabic RTL is equal to English LTR in every surface and breakpoint.
5. **Ship polish without breaking paths** — Visual redesigns must not regress established workflows or accessibility.

## Accessibility & Inclusion

- **Target:** WCAG 2.x Level AA for text, controls, and focus visibility.
- **RTL:** Full mirroring for Arabic (FR-21) across responsive layouts.
- **Motion:** Honor `prefers-reduced-motion`; animations are enhancement, not requirement.
- **Touch:** Minimum ~44×44px interactive targets on mobile and narrow viewports.
- **Zoom:** Content reflows at 200% without loss of functionality.
- **Adaptive platform note:** MAUI Hybrid renders shared web UI inside a WebView; native iOS/Android HIG/Material guarantees apply to shell chrome and future native TFMs, while in-WebView styling follows shared web tokens and E8 responsive rules.

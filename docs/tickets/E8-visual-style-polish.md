# E8 — Visual Style, Theming & Responsive Layout

You are a senior front-end/UI engineer and design systems specialist. Help plan and spec a full visual identity pass for the LinkNest app — this is a design-consistency, responsive-layout, and polish sprint, not a feature sprint.

## The Task

Produce two things:

1. **A design system spec** — propose a complete visual direction from scratch (the user has no existing direction to match). Define color palette, typography scale, spacing tokens, breakpoint tokens, and component rules. The app should feel trendy, modern, and distinctly human — not AI-scaffolded. The system must support both **light and dark mode** (explicitly required; dark mode is not out of scope).

2. **A detailed implementation plan** — tasks, file changes, and order of work to apply the design system and responsive layout rules across all E1–E7 screens on **web and mobile**.

## Depends On

E7 (all features should exist on both web and mobile before doing a full visual pass, so the styling work covers the complete surface area once, not repeatedly as new screens land).

E9 H4 (`LinkNest.UI` extraction) is **recommended before or in parallel with E8** so responsive CSS and tokens live in one shared RCL consumed by web and MAUI — not duplicated across `Web.Client` and `Mobile`.

---

## In Scope

### Visual design system

Every screen from E1–E7 gets the same palette, spacing scale, and component styles — zero screens left on default Bootstrap or ad-hoc inline styles. Starting fresh means no meaningful prior styling needs to be preserved or migrated.

**Where shared styles live:**

- Design tokens and shared CSS in `src/LinkNest.Shared/wwwroot/` (or `src/LinkNest.UI/wwwroot/` once H4 lands) — **single source of truth** for web and mobile.
- `src/LinkNest.Web.Client/` (or `LinkNest.UI/`) — component markup consumes tokens.
- `src/LinkNest.Mobile/` — Blazor Hybrid loads the same shared CSS; `Styles.xaml` only for MAUI shell chrome if needed.

**Shared component styles to define:** buttons, cards, badges, forms, navigation, empty states, modals/dialogs — styled once, consumed everywhere: home, category detail, archive, login/register, groups/members, invites, settings (E9).

**Animation and motion** — micro-interactions where they add polish (hover states, transitions, loading skeletons, subtle entrance animations); skip where they'd add noise. Respect `prefers-reduced-motion`.

### Responsive layout (all screen sizes)

The app must be **fully usable and visually coherent** from narrow phone widths through large desktop monitors, on **both**:

- **Browser (Blazor web)** — user resizes the window or uses DevTools device emulation; layout adapts fluidly without horizontal scroll, clipped controls, or unreadable text.
- **MAUI Blazor Hybrid (mobile)** — phone and tablet form factors in WebView; safe areas and touch targets respected.

This is **in scope for E8**, not a separate epic. Responsive behavior uses the **same design tokens** as the visual pass (spacing, typography, breakpoints defined once).

#### Breakpoint strategy

Define canonical breakpoints in shared CSS (custom properties or a single `tokens.css`):

| Token | Approx. width | Target |
|-------|---------------|--------|
| `--bp-xs` | &lt; 480px | Small phones |
| `--bp-sm` | 480–767px | Large phones |
| `--bp-md` | 768–1023px | Tablets / small laptops |
| `--bp-lg` | 1024–1439px | Desktop |
| `--bp-xl` | ≥ 1440px | Large desktop (optional max-width container) |

Use **mobile-first** CSS: base styles for narrow viewports; `@media (min-width: …)` for larger layouts. Avoid fixed pixel widths on containers except max-width for readability on ultra-wide screens.

#### Layout rules (apply to every screen)

- **Shell / navigation:** Collapse or stack header nav on narrow widths (hamburger or bottom nav pattern — pick one in the design spec). Brand, auth actions, and primary nav remain reachable without zoom.
- **Home category grid:** 1 column (xs/sm) → 2 columns (md) → 3+ columns (lg) with consistent gutters from spacing tokens.
- **Category / link lists:** Cards stack full-width on mobile; multi-column only when space allows. Favorite/archive actions remain tappable (min **44×44px** touch target).
- **Forms (login, register, settings):** Full-width inputs on mobile; constrained max-width (e.g. 400–480px) centered on desktop.
- **Group members / invites:** Tables become stacked cards or scrollable lists on narrow viewports — no clipped action buttons.
- **Archive / filters:** Filter chips wrap; do not overflow off-screen.
- **Share target page:** Usable on narrow mobile browser widths (PWA share flow).
- **Typography:** Scale down heading sizes on xs; body text never below 16px equivalent on mobile (avoid iOS zoom-on-focus).
- **Images / link preview cards:** `max-width: 100%`; preserve aspect ratio; no layout shift on load where possible.
- **RTL (Arabic):** Responsive rules must mirror correctly — padding, alignment, icon direction, and collapsed nav in RTL re-verified at each breakpoint.

#### Platform-specific responsive notes

| Platform | Requirement |
|----------|-------------|
| **Web browser** | Test resize from 320px to 1920px+; no broken layouts at intermediate widths (e.g. 600px, 900px). |
| **Web — SSR + WASM** | Styles live in shared CSS loaded by both render paths; no flash of unstyled narrow/wide layout on hydration. |
| **MAUI WebView** | Same CSS as web; verify on Windows window resize and Android/iOS device sizes once those TFMs exist (E9 H3 + future iOS). |
| **Safe areas** | Use `env(safe-area-inset-*)` for notched phones where Hybrid WebView supports it. |

#### Acceptance criteria — responsive

- [ ] Every E1–E7 screen listed in the implementation plan has responsive behavior documented and implemented.
- [ ] **Web manual test:** Chrome/Edge DevTools — iPhone SE, iPhone 14, iPad, and responsive mode at 320, 768, 1024, 1440px — no horizontal scrollbar on main flows (home, category, archive, login, groups, group members, share).
- [ ] **Web manual test:** Live browser window drag-resize — layout transitions smoothly; no overlapping header/footer; nav remains usable.
- [ ] **Mobile manual test:** MAUI app on smallest supported Windows width / Android phone emulator — same flows usable without pinch-zoom.
- [ ] **RTL manual test:** Repeat key scenarios in Arabic at xs and lg breakpoints.
- [ ] **Accessibility:** Zoom to 200% — content reflows without loss of functionality (WCAG 1.4.10 reflow goal).
- [ ] Playwright E2E (optional stretch): add viewport size variants for smoke tests on home + login at 375px and 1280px.

---

## Non-Negotiables to Plan Around

- **RTL (Arabic)** — FR-21 — spacing, alignment, and icon mirroring must be re-verified under the new styles in both LTR and RTL directions at **every breakpoint**. RTL is not an afterthought.
- **Shared vs. private visual indicator** — FR-38 — must remain clearly distinguishable at a glance within the new design language on all screen sizes.
- **Accessibility** — keyboard-navigable forms remain keyboard-navigable after restyling; focus rings visible; color contrast meets WCAG AA for text and controls.
- **No functional regression** — every golden path from E1–E7 (create/archive/favorite category & link, login, group create/invite/join, home view mine/shared) must behave identically after the restyle.

---

## Out of Scope

- New features, new FRs, or open-question resolutions (category icons/colors, confirm-before-archive dialog).
- Native platform-specific redesigns beyond what shared styling naturally produces (e.g. custom iOS tab bar separate from shared CSS).
- **Hosting / deployment / app stores** — E9 § Production hosting & store publishing.

---

## What a Good Outcome Looks Like

- A visual direction the user can evaluate and approve before implementation begins.
- A plan specific enough that a developer could execute it file-by-file without ambiguity.
- Web and mobile rendered from the same shared source, matching visually side-by-side in both light and dark mode.
- Resizing the browser or opening on a phone feels intentional — not a shrunk desktop layout.
- The finished app looks and feels like a real, polished product.

---

## Likely Files/Areas

- `src/LinkNest.Shared/wwwroot/css/` (or `LinkNest.UI/wwwroot/css/`) — `tokens.css`, `components.css`, `layout.css`, `responsive.css`
- `src/LinkNest.Web/LinkNest.Web/wwwroot/app.css` — import shared tokens or replace with shared bundle
- `src/LinkNest.Web/LinkNest.Web.Client/Components/InteractiveShell.razor` — responsive header/nav
- All `Pages/*.razor` — remove inline styles; use token-based classes
- `src/LinkNest.Mobile/wwwroot/index.html` — viewport meta (`width=device-width, initial-scale=1`)
- `docs/L2.md` — link to breakpoint tokens after implementation

---

## Manual Test Notes

- Visual review: light + dark mode on web and MAUI side-by-side.
- Responsive matrix: document screenshots or checklist per screen × breakpoint (store in ticket or `docs/` if helpful).
- Regression: run full E7 L4 mobile matrix + web golden paths after CSS changes.

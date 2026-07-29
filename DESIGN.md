# LinkNest Design System

<!-- impeccable:design-schema 1 -->
<!-- Direction: Neo-Tactile — Setproduct styleguide (Pinterest ref) -->

## World

**Neo-Tactile** — dark glass + soft neomorphic depth: frosted panels, electric blue active glow, cyan accents, large radii, tactile buttons. Reference: Setproduct "Neo-Tactile" UI kit styleguide.

## Color strategy

**Drenched dark** canvas; glass surfaces; **Committed** blue for primary/active.

| Role | Dark (default) | Light |
|------|----------------|-------|
| Canvas | `#12141a` | `#eef1f6` |
| Glass surface | `rgba(255,255,255,0.06)` | `rgba(255,255,255,0.85)` |
| Primary | `#3b82f6` | `#2563eb` |
| Cyan accent | `#06b6d4` | `#0891b2` |
| Text | `#f1f5f9` | `#0f172a` |
| Muted | `#94a3b8` | `#64748b` |
| Personal | Blue glow | Blue |
| Shared | Cyan | Teal |

## Typography

- **UI:** Albert Sans (400–800)

## Effects

- Glass: `backdrop-filter: blur(24px)` + 1px light border
- Neo depth: dual soft outer shadows + subtle top highlight
- Active glow: blue ring on nav/FAB/toggles

## Defaults

- **Dark mode is the default**; persisted in `localStorage` (`linknest-theme`)
- Theme re-applied on every navigation (`ThemeSync` + `enhancedload`)

## Preview scope

All primary routes styled: Shell, Home, Category, Archive, Groups, Group members, Share, Login, Register, Not Found, Error. Shared tokens in `LinkNest.Shared/wwwroot/css/`.

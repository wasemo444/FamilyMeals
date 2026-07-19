# Product Requirements Document — Manage Family Meals

**Version:** 1.0 (POC)  
**Date:** July 13, 2026  
**Status:** Approved decisions locked; web POC implemented

---

## 1. Executive Summary

**Manage Family Meals** is a unified web and mobile application for a small group (~10 family/friends) to organize meal-related links into categories (Breakfast, Lunch, Snack, etc.). The app requires no database server and stores data per device. It supports English and Arabic with full RTL layout for Arabic.

---

## 2. Goals & Success Criteria

| Goal | Success Criteria |
|------|------------------|
| Unified cross-platform experience | Shared UI and business logic via .NET Razor Class Library |
| Simple link organization | Create categories and links in under 30 seconds |
| Fast access to favorites | Dedicated favorites sections + favorites sorted to top |
| Bilingual UX | EN/AR toggle; Arabic renders RTL |
| Low operational overhead | No database; JSON persistence per device |
| Safe deletion | Soft-delete with 7-day archive before permanent removal |

---

## 3. Approved Architecture Decisions

| Decision | Choice |
|----------|--------|
| Cross-platform approach | Blazor Web App + .NET MAUI Blazor Hybrid + Shared RCL |
| Web render mode | Auto (SSR + WebAssembly) |
| Data sharing | Per-device |
| Persistence format | JSON file (browser localStorage on web; app data directory on mobile) |
| Authentication | Email login/register — **deferred to later phase** |
| Delete behavior | Soft delete / archive for 7 days, then permanent purge |
| Favorites UX | Both: dedicated favorites section + favorites sorted to top |
| Link fields | Title + URL + optional link preview (WhatsApp-style OG card) |
| Default language | Follow device/browser locale (en or ar) |
| Delivery priority | Web first, then mobile |
| Product name | **Manage Family Meals** |

---

## 4. User Stories

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
| US-13 | As a user, I register/login with email | Later |

---

## 5. Functional Requirements

### 5.1 Home Page (Categories)

- **FR-01:** Display favorite categories in a dedicated section
- **FR-02:** Display all active categories below, with favorites sorted first
- **FR-03:** Create category (name required)
- **FR-04:** Archive category (soft delete; cascades to its links)
- **FR-05:** Toggle category favorite
- **FR-06:** Navigate to category detail page
- **FR-07:** Show link count per category

### 5.2 Category Detail Page

- **FR-08:** Display favorite links in a dedicated section
- **FR-09:** Display all active links, favorites sorted first
- **FR-10:** Add link (URL required; title optional)
- **FR-11:** Fetch link preview (title, description, image, site name) server-side
- **FR-12:** Archive link (soft delete)
- **FR-13:** Toggle link favorite
- **FR-14:** Open link in external browser

### 5.3 Archive Page

- **FR-15:** List archived categories with restore action
- **FR-16:** List archived links with restore action
- **FR-17:** Display 7-day retention notice
- **FR-18:** Permanently purge items archived more than 7 days on app load

### 5.4 Localization

- **FR-19:** Language switcher (English / Arabic)
- **FR-20:** All UI strings via RESX resource files
- **FR-21:** RTL layout (`dir="rtl"`) when Arabic is active
- **FR-22:** Default language from browser/device locale on first visit
- **FR-23:** Persist language preference in local storage

### 5.5 Data & Persistence

- **FR-24:** Serialize all data as JSON
- **FR-25:** Store JSON in browser localStorage (web POC)
- **FR-26:** Store JSON in app data directory (mobile — future)
- **FR-27:** Data survives browser refresh and app restart

---

## 6. Non-Functional Requirements

| Area | Requirement |
|------|-------------|
| Performance | Pages load in < 1s on typical home network |
| Reliability | No data loss on normal shutdown |
| Accessibility | Keyboard-navigable forms on web |
| Maintainability | Shared models/services/components in RCL |
| Scale | ~10 users, single-device usage, low concurrency |
| Security (POC) | No auth; link preview fetched server-side only for valid HTTP(S) URLs |

---

## 7. Data Model

```
AppData
├── Categories[]: MealCategory
├── Links[]: MealLink
└── Settings: AppSettings

MealCategory
├── Id: Guid
├── Name: string
├── IsFavorite: bool
├── CreatedAtUtc: DateTime
├── IsDeleted: bool
└── DeletedAtUtc: DateTime?

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
└── PreviewSiteName: string?

AppSettings
└── CultureCode: string?
```

---

## 8. Solution Structure (POC)

```
ManageFamilyMeals.slnx
src/
├── ManageFamilyMeals.Shared/       # Models, services, RESX localization
├── ManageFamilyMeals.Web/          # Blazor Web App host (Auto render mode)
│   └── ManageFamilyMeals.Web.Client/  # Interactive WASM components & pages
└── ManageFamilyMeals.Mobile/       # (Future) MAUI Blazor Hybrid
```

---

## 9. Out of Scope (Current Phase)

- Email registration and login
- Multi-device data sync
- Shared/cloud storage
- .NET MAUI mobile app (next phase)
- Push notifications
- Link import/export
- Drag-and-drop reordering
- Dark mode
- App store deployment

---

## 10. POC Validation Checklist

| # | Criterion | Status |
|---|-----------|--------|
| 1 | Create/delete (archive) category | Implemented |
| 2 | Create/delete (archive) link | Implemented |
| 3 | Favorites toggle (category + link) | Implemented |
| 4 | Favorites section + sort-to-top | Implemented |
| 5 | EN ↔ AR language switch | Implemented |
| 6 | RTL layout in Arabic | Implemented |
| 7 | JSON persistence across refresh | Implemented |
| 8 | Link preview (OG metadata) | Implemented |
| 9 | 7-day archive with restore | Implemented |
| 10 | Shared RCL compiles for web | Implemented |

---

## 11. Next Phase (Pending Your Approval)

1. **Mobile:** Add `ManageFamilyMeals.Mobile` MAUI Blazor Hybrid project referencing Shared RCL
2. **Auth:** Email registration/login (ASP.NET Core Identity or external provider)
3. **Sync (optional):** If group sharing is needed later, introduce a minimal API + shared storage

---

## 12. Open Questions for Incremental Walkthrough

These are intentionally left for our next session:

1. Category icons or colors?
2. Confirm-before-archive dialog?
3. Search/filter for links?
4. Manual reorder of categories/links?
5. Hosting target for web (local IIS, Azure, home server)?

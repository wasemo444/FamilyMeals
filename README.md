# Manage Family Meals — POC

## Run the web app

```powershell
cd src/ManageFamilyMeals.Web/ManageFamilyMeals.Web
dotnet run
```

Open the URL shown in the console (typically `https://localhost:7xxx`).

## What this POC validates

- **Blazor Web App (Auto)** with interactive WebAssembly components
- **Shared RCL** for models, services, and localization
- **Per-device JSON** persistence via browser `localStorage`
- **Bilingual EN/AR** with RTL support
- **Soft delete** with 7-day archive and restore
- **Link previews** via server-side Open Graph metadata fetch

## Project layout

| Project | Role |
|---------|------|
| `ManageFamilyMeals.Shared` | Models, `MealDataService`, RESX strings |
| `ManageFamilyMeals.Web` | ASP.NET Core host, link preview API |
| `ManageFamilyMeals.Web.Client` | Interactive pages (Home, Category, Archive) |

## Notes

- Data is stored only in this browser's localStorage — clearing site data removes it.
- Link previews depend on the target site exposing Open Graph tags; some URLs will not preview.
- Mobile (MAUI Blazor Hybrid) is planned for the next phase.

See [docs/PRD.md](../docs/PRD.md) for full requirements.

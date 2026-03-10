# New Company Selection Template

This template was derived by comparing:
- `Views/CohesiveCompany/CCArmyFactionSelectionPage.xaml(.cs)`
- `Views/StandardCompany/StandardCompanySelectionPage.xaml(.cs)`

## Template style
- Inheritance-first.
- Shared behavior belongs in `CompanySelectionPageBase.cs.template`.
- Mode-specific behavior belongs in derived page code-behind (`CompanySelectionPage.xaml.cs.template`).

## Files
- `CompanySelectionPage.xaml.template`: shared page layout shell.
- `CompanySelectionPageBase.cs.template`: reusable base class with shared state, services, commands, and hook methods.
- `CompanySelectionPage.xaml.cs.template`: thin subclass + XAML event wrappers + overrides.

## What is shared
- Page layout and major sections (`FACTION SELECTION`, `UNIT SELECTION`, unit display panel)
- Core state (selected faction/unit, stats, company list)
- Main commands (select faction/unit, add/remove company entries, start company)
- Metadata/army/spec-ops service wiring

## What varies by mode
- Filter popup types:
  - Standard: `UnitFilterPopupPage`, `UnitFilterCriteria`, `UnitFilterPopupOptions`
  - Cohesive: `CCUnitFilterPopupPage`, `CCUnitFilterCriteria`, `CCUnitFilterPopupOptions`
- Cohesive-only behavior:
  - Tracked fireteam state/icon logic
  - FTO profile restriction path
  - Valid core fireteams cache

## How to use
1. Copy all three template files into your target folder.
2. Rename placeholders:
   - `{{NAMESPACE}}`
   - `{{PAGE_CLASS}}`
3. Keep shared logic in the base class; override only deltas in the derived class.
4. Choose your filter popup types and implement that in derived code.
5. Add cohesive-specific fireteam/FTO logic only in derived code when needed.

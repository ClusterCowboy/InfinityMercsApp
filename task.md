# Task: Migrate to Token-Based Font Sizing (Approach 2)

## Goal

Replace every hardcoded `FontSize` value in the codebase with `{DynamicResource}` token bindings
from `Sizes.xaml`. Combined with the global `FontAutoScalingEnabled="True"` already present on
all Labels, this gives the app automatic per-platform base sizing (via `OnIdiom`) plus
OS-level accessibility scaling on top — on Windows, Android, and iOS.

---

## What is Already Done

- `FontAutoScalingEnabled="True"` is set globally on `Label` in
  `Resources/Styles/Styles.xaml:169`. No change needed there.
- The token file `Resources/Styles/Sizes.xaml` already defines six tiers:

  | Token | Phone | Tablet | Desktop |
  |---|---|---|---|
  | `FontSizeCaption` | 11 | 13 | 12 |
  | `FontSizeBody` | 13 | 15 | 14 |
  | `FontSizeSectionHeader` | 16 | 20 | 18 |
  | `FontSizeSubHeadline` | 18 | 22 | 24 |
  | `FontSizeHeadline` | 24 | 28 | 32 |
  | `FontSizeDisplay` | 28 | 34 | 36 |

---

## Step 1 — Extend Sizes.xaml with Missing Tokens

The existing six tiers do not cover all sizes in use. The following new tokens are needed:

| Token (proposed) | Phone | Tablet | Desktop | Covers current hardcoded values |
|---|---|---|---|---|
| `FontSizeMicro` | 9 | 10 | 10 | 9, 10, 11 (dense tabular / badge text) |
| `FontSizeTitleSmall` | 20 | 24 | 22 | 20, 22 (mid-weight titles, card headers) |
| `FontSizeTitleLarge` | 26 | 30 | 30 | 26, 28, 30 (large hero text, popup headings) |

> **Note:** The 10/11 px stat-column labels in `UnitDisplayConfigurationsView` are a special
> case — see Step 4.

---

## Step 2 — XAML Files to Update

Each file listed below contains hardcoded numeric `FontSize` values. Replace each with the
appropriate `{DynamicResource}` token. Values that fall between two tiers should round to the
nearest token; if the gap is significant, add a new token in Step 1 instead of rounding.

### Pages

| File | Hardcoded sizes found |
|---|---|
| `Views/AboutPage/AboutPage.xaml` | 28, 18, 18, 18, 16 |
| `Views/ArmoryPage/ArmoryPage.xaml` | 20, 14, 18 |
| `Views/CreateNewCompanyPage.xaml` | 32 (×6) |
| `Views/DebugPage/DebugPage.xaml` | 32 |
| `Views/FeedbackBugsPage/FeedbackBugsPage.xaml` | 26 |
| `Views/LoadCompanyPage/LoadCompanyPage.xaml` | 28, 18, 12 |
| `Views/MainPage/MainPage.xaml` | 28, 22 |
| `Views/MarketplacesPage/MarketplacesPage.xaml` | 22, 18, 16, 15, 14, 13, 12, 9 |
| `Views/MercsGlossaryPage/MercsGlossaryPage.xaml` | 20, 20, 20, 18 |
| `Views/PerkTesterPage.xaml` | 20, 18, 12 |
| `Views/SettingsPage/SettingsPage.xaml` | 18 |
| `Views/SplashPage/SplashPage.xaml` | 30, 16 |
| `Views/UnitEncyclopedia/UnitEncyclopediaPage.xaml` | 20, 18, 12 |

### Company-selection pages

| File | Hardcoded sizes found |
|---|---|
| `Views/AirborneCompany/AirborneCompanySelectionPage.xaml` | 20 |
| `Views/CohesiveCompany/CohesiveCompanySelectionPage.xaml` | 20, 11 |
| `Views/InspiringCompany/InspiringCompanySelectionPage.xaml` | 20 |
| `Views/StandardCompany/StandardCompanySelectionPage.xaml` | 20 |
| `Views/StandardCompany/StandardCompanySourcePopupPage.xaml` | 30 (×3) |
| `Views/LoneWolfCompany/LoneWolfCompanySourcePopupPage.xaml` | 30 (×3) |
| `Views/TagCompany/TagCompanySourcePopupPage.xaml` | 30 (×3) |

### Season flow

| File | Hardcoded sizes found |
|---|---|
| `Views/Season/SeasonPage.xaml` | 20, 16, 15, 13 |
| `Views/Season/PlayModePage.xaml` | 20, 13, 10 |
| `Views/Season/LoadSeasonPage.xaml` | 28, 18, 12 |

### CompanyViewerPage

| File | Hardcoded sizes found |
|---|---|
| `Views/CompanyViewerPage/CompanyViewerPage.xaml` | 34, 28, 24, 30, 22 (and matching `Entry` at 34) |

This page mirrors `SeasonPage` structurally and should receive the same token treatment applied
to `SeasonPage` in the previous sprint.

### Shared controls

| File | Hardcoded sizes found | Notes |
|---|---|---|
| `Views/Controls/CompanyUnitSelectionListPanelView.xaml` | 24 | |
| `Views/Controls/CompanyViewerUnitTileView.xaml` | 14, 12 | |
| `Views/Controls/FactionListItemView.xaml` | 14, 12 | |
| `Views/Controls/MercsCompanyEntryCardView.xaml` | 18, 12 | |
| `Views/Controls/UnitFilterPopupView.xaml` | 24, 18, 12 | |
| `Views/Controls/UnitSelectionListItemView.xaml` | 12 | |
| `Views/Controls/ViewerListRowView.xaml` | 12 | |
| `Views/Controls/UnitDisplayConfigurationsView.xaml` | 10, 11, 12, 20 | See Step 4 |

---

## Step 3 — C# Code-Behind Files to Update

These files construct `Label` objects in C# with hardcoded `FontSize` property assignments.
The token values are not directly available as C# constants, so either:
- **(a)** convert the constructed views to XAML `DataTemplate`s where the token binding
  can be expressed, **or**
- **(b)** read the resolved token value at runtime via
  `Application.Current.Resources.TryGetValue("FontSizeBody", out var size)`.

Option (b) is simpler for now. Introduce a small static helper:

```csharp
// e.g. in a shared AppResources static class
public static double FontSize(string key, double fallback = 14d)
{
    if (Application.Current?.Resources.TryGetValue(key, out var raw) == true && raw is double d)
        return d;
    return fallback;
}
```

Files requiring this treatment:

| File | Hardcoded sizes found |
|---|---|
| `Views/Common/Captain/ConfigureCaptainPopupPage.cs` | 22, 19, 18, 15 |
| `Views/Controls/WeaponDetailCardView.xaml.cs` | 13, 12, 11 |
| `Views/LoadCompanyPage/LoadCompanyPage.xaml.cs` | 18 |
| `Views/MarketplacesPage/MarketplacesPage.xaml.cs` | 14, 13, 11, 9 |
| `Views/PerksTablesPage/PerksTablesPage.xaml.cs` | 14 |
| `Views/Season/SeasonPage.xaml.cs` | 22, 18, 15, 14, 13, 12, 11 |

---

## Step 4 — Stat-Block Labels (Special Case)

`Views/Controls/UnitDisplayConfigurationsView.xaml` contains 40 stat-column labels
(MOV, CC, BS … headers and values for both primary and peripheral stat blocks) hardcoded at
10 px (headers) and 11 px (values). These must remain compact regardless of idiom because the
10-column grid has physically limited column width at any screen size.

**Decision required:** either
- Add `FontSizeMicro` (10/10/10 across all idioms) and use it here, keeping all sizes
  token-driven, **or**
- Leave these 40 labels as the only accepted exception to the rule, documented in a comment.

The remaining non-stat labels in that file (peripheral name heading at 12, profile list items
at 12, "Profiles" heading at 20) follow the normal token path.

---

## Step 5 — Tune OnIdiom Values

Once all tokens are wired up, run the app on each target platform and verify the `Phone` and
`Tablet` values in `Sizes.xaml` produce acceptable layouts:

- **Windows** — always resolves to `Desktop`. The current values were designed for this.
- **Android** — resolves to `Phone` on handsets, `Tablet` on large tablets.
- **iOS** — resolves to `Phone` on iPhones, `Tablet` on iPads.

Adjust token values per idiom as needed after visual review on real or emulated devices.

---

## Step 6 — Remove the `UnitNameHeadingFontSize` Measure-and-Shrink

`UnitDisplayConfigurationsView.xaml.cs` contains a manual binary-search font-shrink loop
(`UpdateUnitHeadingFontSize`) that was built as a workaround for the unit name heading
overflowing its cell. Once the heading is sized with a token appropriate for each idiom,
verify whether the heading still overflows on any platform. If not, remove:

- `UpdateUnitHeadingFontSize()` and its callers
- `DefaultUnitHeadingMaxFontSize`, `DefaultUnitHeadingMinFontSize`, `DefaultUnitHeadingFontStep`
- `UnitNameHeadingFontSizeProperty` bindable property and its usages
- `OnUnitNameHeadingLabelSizeChanged` event handler and XAML hook
- `UnitNameHeadingSizeChanged` event and its usages in hosting pages

If overflow is still possible on narrow screens, keep the loop but feed it the token-resolved
max value instead of the hardcoded `24d`.

---

## Acceptance Criteria

- [ ] No raw numeric `FontSize` values remain in any `.xaml` file (except the stat-block
      exception if that decision is taken in Step 4)
- [ ] No raw numeric `FontSize` assignments remain in `.cs` files outside of the approved
      helper or documented exceptions
- [ ] `Sizes.xaml` contains all tokens referenced by the codebase
- [ ] The app builds with 0 errors and 0 warnings
- [ ] Visual review passes on Windows (all three phone-size presets), Android emulator, and
      iOS simulator

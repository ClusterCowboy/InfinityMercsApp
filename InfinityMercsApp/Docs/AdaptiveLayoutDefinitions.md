# Adaptive Layout Definitions

This document defines the intended phone, tablet, and desktop layouts for the app's user-facing pages. `DebugPage` and `PerkTesterPage` are intentionally excluded. Hidden shell entries that are still real user flows, such as create/load/company viewer, are included.

## Page Coverage

Included pages:

- `SplashPage`
- `MercsSeasonPage`
- `ModeSelectionPage`
- `CreateNewCompanyPage`
- `StandardCompanySourcePopupPage`
- `TagCompanySourcePopupPage`
- `LoneWolfCompanySourcePopupPage`
- `StandardCompanySelectionPage`
- `CohesiveCompanySelectionPage`
- `InspiringCompanySelectionPage`
- `AirborneCompanySelectionPage`
- `LoadCompanyPage`
- `CompanyViewerPage`
- `SeasonPage`
- `LoadSeasonPage`
- `PlayModePage`
- `GameModePage`
- `InjuriesPage`
- `MissionOutcomePage`
- `ExperiencePage`
- `DowntimePage`
- `MercsGlossaryPage`
- `PerksTablesPage`
- `ArmoryPage`
- `MarketplacesPage`
- `UnitEncyclopediaPage`
- `SettingsPage`
- `AboutPage`
- `FeedbackBugsPage`

Excluded debug-only pages:

- `DebugPage`
- `PerkTesterPage`

## Shared Layout Modes

Use available page width in MAUI logical units, not raw physical pixels, as the primary trigger. A desktop window can be narrow, and a tablet can be split-screen.

| Mode | Width | Intent |
| --- | ---: | --- |
| Compact | `< 600` | Phone-first, one column, stacked panels, full-width actions. |
| Medium | `600-899` | Tablet portrait or narrow desktop, usually two zones. |
| Expanded | `900-1199` | Tablet landscape or normal desktop, persistent side-by-side work areas. |
| Wide | `>= 1200` | Large desktop, add supporting rails, previews, or denser multi-column lists. |

## Target Phone Coverage

The compact definitions are intended to cover modern phone portrait widths, including iPhone X and later Face ID-era iPhones, plus Google Pixel 6 and later slab phones. Raw display pixels are much larger than these numbers; MAUI layout uses logical units after platform scaling.

| Target | Typical portrait logical width | Layout mode |
| --- | ---: | --- |
| iPhone X / XS / 11 Pro class | `375` | Compact |
| iPhone 12/13 mini class | `360` | Compact |
| Larger iPhone class | `390-430` | Compact |
| Pixel 6 / 6a / 7 / 8 / 9 / 10 slab class | about `393-448` | Compact |
| Pixel Fold/Pro Fold cover display | about phone width | Compact |
| Pixel Fold/Pro Fold unfolded display | usually `>= 600` | Medium or larger |

Acceptance target: compact pages must remain usable down to `360` logical units wide, with safe-area insets applied. If support is later expanded to older/smaller iPhones such as the first-generation iPhone SE, add a separate `320` logical-unit acceptance target.

## Shared UI Definitions

| Definition | Purpose |
| --- | --- |
| `AdaptiveLayoutMode` | Enum exposed by a shared page/helper: `Compact`, `Medium`, `Expanded`, `Wide`. |
| `AdaptiveContentPage` behavior | Updates layout mode on `SizeChanged`/`OnSizeAllocated`, so XAML and code-behind can react to width. |
| `AdaptivePagePadding` | Dynamic resource mapped from layout mode. Compact uses the existing phone padding; wide uses larger outer margins with max content widths. |
| `AdaptiveDialogWidth` | Popup/content dialog max width: compact fills most of screen, medium/wide uses centered modal width. |
| `AdaptiveListDetailLayout` | Reusable pattern for list/detail pages: compact swaps between list and detail; medium+ shows both. |
| `AdaptiveCommandBar` | Reusable bottom action bar on compact, right/top action cluster on medium+. |
| `AdaptiveCardGridColumns` | Reusable column count helper for choice cards: 1 compact, 2 medium, 3 expanded/wide. |
| `AdaptiveReferenceLayout` | Reusable pattern for reference screens: index/search, content/detail, optional metadata rail. |
| `AdaptiveCompanySelectionLayout` | Reusable pattern for company-builder screens: faction source, unit/team list, unit details, roster/start panel. |

## App Entry Pages

### SplashPage

| Mode | Layout |
| --- | --- |
| Compact | Centered logo, title, spinner, and status in a single column. Keep logo constrained to screen width. |
| Medium | Same single-column layout with larger logo and max text width. |
| Expanded | Centered content with fixed max width; do not stretch status text across the screen. |
| Wide | Same as expanded. No extra panes; splash should stay calm and fast. |

### MercsSeasonPage

| Mode | Layout |
| --- | --- |
| Compact | Current vertical layout: logo/title centered, action buttons anchored near bottom. |
| Medium | Two vertical zones: brand/status above, actions below, both constrained to a readable max width. |
| Expanded | Split horizontally: left brand/season introduction, right action panel. |
| Wide | Same as expanded, with optional recent-season summary below or beside actions if added later. |

### ModeSelectionPage

| Mode | Layout |
| --- | --- |
| Compact | Logo and title above stacked `New Company` and `Load Company` buttons. |
| Medium | Logo/title centered, buttons in a two-column row with fixed max button width. |
| Expanded | Keep content centered inside `MaxContentWidth`; avoid stretching the header image. |
| Wide | Same as expanded. This page does not need more density. |

## Company Creation Flow

### CreateNewCompanyPage

| Mode | Layout |
| --- | --- |
| Compact | One-column scrollable choice list. Each company type is a full-width action card with icon left/top and title. Back button in a sticky bottom command bar. |
| Medium | Two-column card grid. Back button remains at bottom/start. |
| Expanded | Three-column card grid with max content width. Cards keep stable aspect ratio. |
| Wide | Three-column grid centered; optional short category grouping can be added without changing navigation. |

### StandardCompanySourcePopupPage

| Mode | Layout |
| --- | --- |
| Compact | Full-screen or bottom-sheet style dialog. Source choices stack vertically. Back action stays at bottom. |
| Medium | Centered modal with two source cards side-by-side. |
| Expanded | Centered modal with max width around `760-860`; keep cards side-by-side. |
| Wide | Same as expanded. Avoid using the entire desktop width. |

### TagCompanySourcePopupPage

| Mode | Layout |
| --- | --- |
| Compact | Same as standard source selector: vertical source cards. |
| Medium | Centered two-card modal. |
| Expanded | Centered modal with max width. |
| Wide | Same as expanded. |

### LoneWolfCompanySourcePopupPage

| Mode | Layout |
| --- | --- |
| Compact | Same selector pattern, but title remains specific to Lone Wolf. |
| Medium | Centered two-card modal. |
| Expanded | Centered modal with max width. |
| Wide | Same as expanded. |

## Company Selection Pages

These pages should share one adaptive definition wherever possible:

- `StandardCompanySelectionPage`
- `StandardCompanySelectionPage` used for TAG company
- `StandardCompanySelectionPage` used for Lone Wolf company
- `CohesiveCompanySelectionPage`
- `InspiringCompanySelectionPage`
- `AirborneCompanySelectionPage`

### Shared Company Selection Layout

| Mode | Layout |
| --- | --- |
| Compact | Single active workspace. Shell title view remains compact with faction slots and points. Faction strip is horizontally scrollable and collapsible. Main content uses tabs/segmented state: `Units`, `Details`, `Roster`. Start-company action is sticky at bottom when roster is active. |
| Medium | Two-pane builder: left pane contains faction strip plus units/fireteams; right pane contains selected unit details and roster summary. Company name/start controls sit above roster or in a bottom command row. |
| Expanded | Three operational zones: left faction/unit/team list, center selected unit details/profiles, right roster/company start panel. Faction strip can remain full-width above all zones. |
| Wide | Same as expanded, with stable max/min widths: faction/unit rail about `320-380`, detail pane flexible, roster rail about `360-420`. |

### StandardCompanySelectionPage

Uses the shared company selection layout. This concrete page also backs the TAG and Lone Wolf flows when created through `CompanySelectionPageFactory`.

| Mode | Layout detail |
| --- | --- |
| Compact | `Units` tab prioritizes the unit list and filter. `Details` tab shows `UnitDisplayConfigurationsView`. `Roster` tab shows company name, validation, entries, and start button. |
| Medium+ | Roster remains visible whenever there is enough width, so adding/removing profiles gives immediate feedback. |

### CohesiveCompanySelectionPage

Uses the shared company selection layout with cohesive fireteam tracking.

| Mode | Layout detail |
| --- | --- |
| Compact | Fireteam tracking controls belong in the `Roster` or `Teams` tab, not squeezed into the company-name row. |
| Medium+ | Fireteam level/status sits between detail and roster or at the top of the roster rail. |
| Wide | Fireteam list can stay visible in the left pane while roster stays visible on the right. |

### InspiringCompanySelectionPage

Uses the shared company selection layout for generated-faction company selection.

| Mode | Layout detail |
| --- | --- |
| Compact | Treat generated company source as a locked right faction slot; keep the player-facing choice focused on selecting captain/lieutenant candidates. |
| Medium+ | Same three-zone builder as standard, with generated-company context visible in the title/faction selector. |

### AirborneCompanySelectionPage

Uses the shared company selection layout for generated-faction company selection.

| Mode | Layout detail |
| --- | --- |
| Compact | Treat generated company source as a locked right faction slot; keep the player-facing choice focused on selecting captain/lieutenant candidates. |
| Medium+ | Same three-zone builder as standard, with generated-company context visible in the title/faction selector. |

### UnitFilterPopupView

| Mode | Layout |
| --- | --- |
| Compact | Full-screen overlay or nearly full-width sheet; filter criteria and selected criterion stack vertically. |
| Medium | Centered dialog around `700` max width, two zones if height allows. |
| Expanded | Centered dialog; criteria list left, selected filter options right. |
| Wide | Same as expanded. |

### ConfigureCaptainPopupPage

| Mode | Layout |
| --- | --- |
| Compact | Full-screen dialog with stacked form sections and sticky confirm/cancel actions. |
| Medium | Centered modal with a constrained form width. |
| Expanded | Centered modal; optional side summary if captain data becomes dense. |
| Wide | Same as expanded. |

## Company Loading And Viewing

### LoadCompanyPage

| Mode | Layout |
| --- | --- |
| Compact | Current full-width card list. Delete action remains visible but comfortably separated from load tap target. |
| Medium | Centered list with max width, or two-column card grid if records are visually compact. |
| Expanded | List/detail layout: saved records on left, selected/hovered record preview on right if preview data is available. Otherwise use centered max-width list. |
| Wide | Two-column records grid plus optional preview rail. |

### CompanyViewerPage

| Mode | Layout |
| --- | --- |
| Compact | Current phone flow: horizontal unit strip at top, selected unit detail below, weapons/equipment scroll inside detail. |
| Medium | Two-pane layout: unit list/strip on left, selected unit details on right. |
| Expanded | Three zones: roster/unit selector left, unit stat/config detail center, weapons/peripherals/notes right. |
| Wide | Add stable rail widths; selected unit detail should not exceed a comfortable reading width. |

## Season Flow

### SeasonPage

| Mode | Layout |
| --- | --- |
| Compact | Current stacked model: header, selected-unit picker, detail sections, bottom navigation. Tabs remain phone-first. |
| Medium | Two-pane season dashboard: unit picker/list on left, selected unit detail and gear controls on right. Inventory/store sections become separate tabs or stacked panels below. |
| Expanded | Dashboard layout: top season summary band, left team/unit rail, center selected unit details, right resource/inventory/actions panel. |
| Wide | Keep the play-round action visible in the summary/action rail; use additional width for inventory/marketplace summaries rather than stretching text. |

### LoadSeasonPage

| Mode | Layout |
| --- | --- |
| Compact | Full-width saved-season cards below the heading. |
| Medium | Centered list with max width. |
| Expanded | Saved-season list left, selected-season preview/details right if available. |
| Wide | Two-column saved-season grid or list/detail with a wide preview panel. |

### PlayModePage

| Mode | Layout |
| --- | --- |
| Compact | Current deployment flow: horizontal unit strip with checkboxes, selected unit detail below, deploy status/action fixed at bottom. |
| Medium | Left deployment list with checkboxes; right selected unit detail. Deploy action stays in a bottom command bar. |
| Expanded | Three zones: deployable units left, selected unit detail center, deployment rules/status/actions right. |
| Wide | Keep deployment status and deploy action always visible in the right rail. |

### GameModePage

| Mode | Layout |
| --- | --- |
| Compact | Current round header, order pools, and unit accordion stack. |
| Medium | Sticky order-pool/control rail above or beside the accordion; unit accordion remains primary. |
| Expanded | Left round/order controls, center active unit accordion, right selected/expanded unit quick actions if available. |
| Wide | Keep order pool visible at all times; avoid making unit accordion rows excessively wide. |

### InjuriesPage

| Mode | Layout |
| --- | --- |
| Compact | Scrollable injury stack with full-width bottom continue button. |
| Medium | Two-column injury card grid if individual cards are compact; continue button remains bottom command. |
| Expanded | Injury list/grid left, resolution/progress summary right. |
| Wide | Grid can add columns, but individual injury controls keep fixed comfortable widths. |

### MissionOutcomePage

| Mode | Layout |
| --- | --- |
| Compact | Current stacked form: result, points, breakdown, continue. |
| Medium | Two-column form: result/points left, breakdown right. Continue button spans bottom. |
| Expanded | Centered outcome panel with persistent breakdown/credits summary rail. |
| Wide | Same as expanded with max content width. |

### ExperiencePage

| Mode | Layout |
| --- | --- |
| Compact | Current stacked unit experience list and bottom confirm button. |
| Medium | Unit cards in one or two columns depending on card width. |
| Expanded | Unit experience list/grid left, total XP/perk summary right. |
| Wide | Add columns only if controls remain readable; keep confirm action sticky. |

### DowntimePage

| Mode | Layout |
| --- | --- |
| Compact | Current event content stack and bottom roll/control bar. |
| Medium | Event detail above, controls in a compact side or bottom command panel. |
| Expanded | Event/result detail left, roll/real controls and resolution status right. |
| Wide | Optional history/resolution rail if downtime history is added. |

## Reference Pages

### MercsGlossaryPage

| Mode | Layout |
| --- | --- |
| Compact | List-first navigation. Select a glossary item to show detail view, with a clear way back to the index. |
| Medium | Two-pane: rules index left, WebView/detail right. |
| Expanded | Same as medium with wider detail pane and constrained index width. |
| Wide | Keep index width stable; detail pane gets max readable width rather than unlimited stretch. |

### PerksTablesPage

| Mode | Layout |
| --- | --- |
| Compact | Picker at top, table host below with pan/zoom or horizontal scroll behavior. |
| Medium | Picker/legend area above table; table gets most of the screen. |
| Expanded | Optional left table selector/legend rail, graph/table canvas right. |
| Wide | Keep table centered and readable; use extra width for legend/dependency notes. |

### ArmoryPage

| Mode | Layout |
| --- | --- |
| Compact | Search/list first. Selecting a weapon opens/reveals detail; detail should not require a permanent side pane. |
| Medium | Two-pane: weapon search/list left, weapon modes/detail right. |
| Expanded | Same two-pane layout with stable list width and richer detail area. |
| Wide | Optional compare/metadata rail can be added; list stays constrained. |

### MarketplacesPage

| Mode | Layout |
| --- | --- |
| Compact | Picker at top, selected marketplace sections stacked vertically. Quantity controls remain inside each row. |
| Medium | Store selector/header above, content in two section columns if item cards fit. |
| Expanded | Store selector/metadata rail left, marketplace sections center/right. |
| Wide | Optional cart/quantity summary rail if buying workflow becomes persistent. |

### UnitEncyclopediaPage

| Mode | Layout |
| --- | --- |
| Compact | Step-based navigation: factions list, then units/fireteams list, then selected unit detail. Filter overlay fills screen. |
| Medium | Two-pane layout: faction/unit navigation left, selected unit detail right. Use tabs inside the left pane for units/fireteams. |
| Expanded | Current three-pane layout: factions, unit/fireteam list, selected unit detail. |
| Wide | Stable column widths for factions and list; detail pane flexible with max readable content. |

## Utility Pages

### SettingsPage

| Mode | Layout |
| --- | --- |
| Compact | Current stacked settings form. Keep update overlay full-screen. |
| Medium | Centered settings panel with max width. Overlay dialog centered with constrained width. |
| Expanded | Two zones if more settings are added: category list left, setting detail right. Current settings can remain centered. |
| Wide | Same as expanded/centered. Do not stretch the simple settings form. |

### AboutPage

| Mode | Layout |
| --- | --- |
| Compact | Current single scrollable column. Attribution rows stack cleanly. |
| Medium | Content constrained to readable max width; attribution entries may wrap in two columns. |
| Expanded | Two-column layout: app/contact info left, attribution/license content right. |
| Wide | Multi-column attribution grid with fixed icon sizes and readable text width. |

### FeedbackBugsPage

| Mode | Layout |
| --- | --- |
| Compact | Current stacked form with full-width fields and submit button. |
| Medium | Centered form with max width. |
| Expanded | Two-column layout: explanatory/status content left, feedback form right. |
| Wide | Same as expanded with constrained form width. |

## Implementation Order

1. Add the shared width-based layout mode helper and adaptive resources.
2. Update simple centered pages first: `SplashPage`, `MercsSeasonPage`, `ModeSelectionPage`, `SettingsPage`, `AboutPage`, `FeedbackBugsPage`.
3. Update chooser/list pages: `CreateNewCompanyPage`, source popups, `LoadCompanyPage`, `LoadSeasonPage`.
4. Update reference list/detail pages: `MercsGlossaryPage`, `ArmoryPage`, `MarketplacesPage`, `PerksTablesPage`, `UnitEncyclopediaPage`.
5. Update shared company selection layout once, then apply it to Standard/TAG/Lone Wolf, Cohesive, Inspiring, and Airborne pages.
6. Update season workflow pages: `SeasonPage`, `PlayModePage`, `GameModePage`, `InjuriesPage`, `MissionOutcomePage`, `ExperiencePage`, `DowntimePage`.
7. Verify compact, medium, expanded, and wide widths on Windows and Android. For every page, check that no text overlaps, bottom command bars remain reachable, and scroll regions do not trap content behind fixed buttons.

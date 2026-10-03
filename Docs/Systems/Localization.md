# Localization and language settings

[Home](../Home.md) · [Project map](../ProjectMap.md) · [Menus](MenusAndScreenFlow.md)

Status: **source-reviewed, 2026-09-28**. Covers string lookup, formatting and menu
language persistence. Locale/table asset names were inspected; translation coverage,
font coverage, scene bindings and live language switching were not tested in Unity.

## String ownership

`GameLocalization` wraps Unity Localization string-table lookups. Its named tables are:

| Table | Helper / purpose |
| --- | --- |
| Descriptions | Get, GetContent, GetDescription; RuntimeTable and ContentTable both alias this table |
| Modifiers | GetModifier and FormatModifier |
| MainMenu | GetMainMenu and LocalizeMainMenuValueOrKey |
| GameUI | GetGameUI and FormatGameUI |
| Enemies | GetEnemy |
| Tutorial | TutorialDefinition and window labels |

Assets under `Assets/Localization` also include StaticText tables; these are not
listed as a GameLocalization constant. Do not assume the wrapper enumerates every
table or every scene-level localization binding. English, German and Russian locale
assets exist under `Assets/Localization/Locales`; their existence does not prove
every key has a translation in all three languages.

## Lookup and fallback contracts

The helper calls StringDatabase.GetTableEntry and obtains the entry's localized
string. Missing settings, blank table/key, missing entry or empty returned text
produce a lookup failure. GetFromTable returns the supplied fallback (or empty
string for a blank fallback). LocalizeValueOrKey instead returns the original
input when lookup fails; a missing key may therefore become visible as raw text.

The wrapper does not wait explicitly for initialization, catch all lookup errors,
or implement its own fallback-locale traversal. Package/table settings remain
separate from this source-level fallback behavior. Inspect those settings when
diagnosing startup text or language fallback rather than inferring them here.

`LocalizeEnum` uses `enum.<EnumTypeName>.<Value>` in Descriptions with a pretty-name
fallback. Renaming enum types/values can change lookup keys even without changing
the table helper. HumanizeIdentifier only inserts spaces before uppercase letters
following non-uppercase letters; it is not a translation mechanism.

## Formatting and embedded terms

The project's Format helpers replace `[[0]]`, `[[1]]`, etc. with each argument's
ToString value; null arguments become empty strings. Replacement is sequential
string replacement, not a full format-string parser or an explicit culture policy.
Unused placeholders remain. Preserve these tokens when translating.

Tooltip content can separately contain `{termId|visible text}` tokens, formatted
by TooltipTextLinkFormatter into TMP links. Keep the ID stable while translating
the visible label. GameLocalization itself does not turn those tokens into links;
the consuming presentation must invoke the formatter. Tooltip ownership, pinning
and nested-window behavior are described in [HUD and tooltips](HudAndTooltips.md).

## Selecting and saving a language

`MenuLanguageNodeAction` waits for localization initialization before initial node
selection. It prefers a valid saved language code, otherwise the current selected
locale, otherwise the first available locale. Node locale matching ignores case
and uses the locale code; optional name matching is available for authored nodes.

User allocation resolves the configured locale, sets SelectedLocale when different
and saves its code when changed. Initial selection applies the preferred locale
without saving it again. Missing settings or an unresolved configured locale logs
a warning. See [menu allocation](MenusAndScreenFlow.md) for graph and zone checks
that can affect selecting the node.

The language code lives in CloudSettingsService's separate settings document,
not the character snapshot. Despite the name, that service uses local SaveFileStorage;
no remote synchronization is established by this code. The menu action accepts an
injected service or constructs and loads a local fallback. Defaults use an empty
languageCode. See [save storage](SavesAndProfiles.md) before changing file ownership.

## Refresh is the consumer's responsibility

GameLocalization is a static lookup helper, not a global text-refresh service.
Inspected subscription sites include EnemyDataText, WaveUI, PlayerStatsWindow,
BonusZoneVisual and MenuVolumeZoneVisual. They subscribe to SelectedLocaleChanged.
TutorialWindow resolves its text when showing a topic and does not refresh an
already-visible topic on locale change. Do not extrapolate one component's refresh
behavior to every label or cached tooltip.

## Authoring and verification

Use the correct table and stable keys, preserve formatting/link tokens, and verify
literal fallback behavior. Location/enemy additions still follow the required
RU/EN/DE work in the [location guide](../Locations/LocationCreationGuide.md).
Writing this project's documentation in English does not remove game locales.

Recommended checks: first launch with no saved code; invalid saved code; switching
language with open windows; missing/empty entries; literal text; argument formatting;
term links; long translations and font glyphs. These checks were not run here.
This page is not a translation-completeness audit.

## Source entry points

- `Assets/Scripts/Localization/GameLocalization.cs`
- `Assets/Scripts/MenuTree/MenuLanguageNodeAction.cs`
- `Assets/Scripts/SaveSystem/SaveSettingsServices.cs`
- `Assets/Scripts/SaveSystem/SaveDataModels.cs`
- `Assets/Scripts/TooltipSystem/TooltipTextLinkFormatter.cs`
- `Assets/Scripts/Tutorials/TutorialDefinition.cs`
- `Assets/Scripts/Tutorials/TutorialWindow.cs`

Related: [tutorial authoring](../Tutorials/README.md), [audio settings](Audio.md),
[stats and modifier descriptions](StatsAndModifiers.md).

# HUD, tooltips and visual references

[Home](../Home.md) · [Project map](../ProjectMap.md) · [Screen flow](MenusAndScreenFlow.md)

Status: **source-reviewed, 2026-09-28**. Core health display and tooltip ownership
paths inspected. This is a component map, not a scene-wide hierarchy, appearance
or performance audit. Existing specialized visual records retain their own limits.

## Values and presentation

HealthBar subscribes to health, maximum-health, sacred-region and mystic absorption
changes, and removes those subscriptions on destruction. Its primary fill displays
CurrentHealth01; its text rounds current/maximum HP upward. Profaned and hallowed
overlays use their configured percentages and mirror according to the region side;
they are not additional health resources. Mystic fill displays absolute absorption
as a fraction of maximum HP. The procedural root chooses Light/Darkness appearance;
its graphic is tinted white when that shader supplies the color.

GSlider clamps incoming fill to 0..1. UpdateBar animates the primary Filled Image
after killing its old tween, while its optional secondary fill changes immediately.
MaskedWidth mode changes viewport width instead. SetBar is the immediate-set path.
Use [resource formulas](DefencesAndResources.md) for game rules rather than reading
mechanics back from animated bar widths. GSlider's normal fill tween does not
explicitly request unscaled time.

GoldCounterUI observes wallet DisplayedGold / OnGoldDisplayChanged, so it follows
the wallet's animated counter. EXPBar maps current experience to the next-level
threshold. These are displays; [economy](EconomyAndLoot.md) and [progression](SkillTree.md)
own the authoritative values.

Other UI entry points include PlayerStatsWindow, WaveUI, EnemyDataText,
EnemyIntelWindow, UnitEffectIconView and the inventory/shop presenters. Their
serialized references and prefabs require a targeted scene check when changing
layout. Their existence does not establish that every optional view is active.

## Tooltip ownership and nesting

TooltipUI tracks a current owner, provider and canvas target. HideTooltip and
RequestHideTooltip only accept the current owner, preventing an old hovered
object from hiding a newer object's tooltip. A requested hide is deferred while
Alt is held or the pointer remains over a visible tooltip, then resolved by Update.

Holding either Alt key pins the tooltip and enables optional descriptions.
Linked tooltip windows require this pinned state, a valid level below the configured
maximum, and an available parent window. Requesting a linked level hides the old
windows from that level before showing its replacement. DisplayLinkedTooltipAsRoot
is a separate entry path for a term as the main tooltip.

Canvas states keep their own window lists, canvas rectangle and camera. Positioning
converts screen coordinates and clamps windows to canvas bounds, with overflow
repositioning away from the pointer. Preserve the correct canvas/camera target when
connecting world objects and UI from different canvases.

Descriptions come through provider interfaces and term lookup, rather than all
being hardcoded in the window. `{termId|label}` is formatted into a TMP link;
malformed shorthand remains visible. Localization and `[[n]]` argument substitution
are a separate stage, described in [localization](Localization.md). Do not create
effects solely to query their tooltip type or text when construction owns modifiers.

## Specialized visual guides

- [Bar contour](../UIBarContour.md): local mesh coordinates, fill edges, masks and HDR contour controls.
- [Procedural magic root](../Reference/ProceduralMagicRootUI.md): per-style settings,
  current-fill tapering, material ownership and historical render checks.

These are rendering/presentation references, not alternate definitions of health,
absorption or damage. Render-path and target-hardware checks remain necessary when
changing shaders, masks or Canvas composition.

## Verification and sources

Suggested checks: resource changes during pause; immediate versus animated fill;
mirrored sacred regions; zero absorption/style changes; stale-owner hide; Alt
press/release; pointer travel into nested tooltips; overflow at all canvas edges;
locale changes with an open tooltip; disabled/destroyed owners and material cleanup.
No Unity visual checks were run in this documentation pass.

- `Assets/Scripts/UI/HealthBar.cs`
- `Assets/Scripts/UI/GSlider.cs`
- `Assets/Scripts/UI/GoldCounterUI.cs`
- `Assets/Scripts/UI/EXPBar.cs`
- `Assets/Scripts/TooltipSystem/TooltipUI.cs`
- `Assets/Scripts/TooltipSystem/TooltipTextLinkFormatter.cs`
- `Assets/Scripts/UI/UIBarContour.cs`
- `Assets/Scripts/UI/ProceduralMagicRootUI.cs`

## Stance status indicator (2026-10-03)

The [offensive/defensive modifier](StatsAndModifiers.md#offensive-and-defensive-stance-2026-10-03)
uses the existing UnitVisualEffectsController refresh path: its dynamic VisualType
selects a sword or shield mapping and GetIconText displays count/threshold. The
border stays fully filled in both stances; only the text reports counter progress. Each distinct
modifier asset gets its own indicator; copies of one asset share it. Actual sword
and shield sprites must be assigned manually in EffectIconsConfig. Without these
mappings, its configured default icon is used. No scene/UI visual check was run.

## Damage debt status (2026-10-03)

DamageDebtEffect entries share one default icon group while keeping independent
lifetimes. GetIconText sums RemainingDamage across that group and rounds upward;
the timer border tracks the nearest expiry using BaseEffect's existing behavior.
EffectVisualType.DamageDebt requires manual sprite mapping in EffectIconsConfig;
unmapped effects use its default icon. English name/description fallbacks are
provided. See the [combat contract](CombatAndEffects.md#deferred-attack-hp-damage-2026-10-03)
and [manual setup](StatsAndModifiers.md#deferred-attack-damage-2026-10-03).
No UI layout or serialized visual configuration was changed or visually verified.

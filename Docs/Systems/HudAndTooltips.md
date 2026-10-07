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

`TooltipTermDatabase.ActiveDatabase` is cleared by `SubsystemRegistration` before
scene startup, including when Domain Reload is disabled (2026-10-03). `TooltipUI.Awake`
registers its configured database. This prevents a previous Play Mode session's
database from remaining selected. Compilation was checked; repeated Play Mode entry
without Domain Reload was not playtested.

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

## Floating proc icons (2026-10-07)

`UnitVisual.DisplayIconNotification(Sprite, Color?)` spawns the existing
`unitNotificationEffect` prefab at the unit position. Callers supply the sprite and optional tint.
Notifications share a 0.2-second minimum interval per UnitVisual, measured with
unscaled time. Valid requests enter a FIFO queue preserving their sprite and tint.
The first request displays immediately when allowed; Update displays subsequent
requests at least 0.2 seconds apart, without catch-up bursts or dropping requests.
Disabling the visual pauses draining while preserving queued/new requests; enabling
it resumes draining. Destroying the visual discards its owned queue. Modifier
gameplay and text notifications are unaffected.
BlockRestoresBarrier now requests a notification via Unit.OnModifierProc only when its block reaction restores a charge. Its modifier asset owns the sprite; other combat triggers remain unconnected.
`UnitNotificationEffect.ShowIcon` configures a fresh instance before `Start`, hides
its TMP text and critical decoration, and enables the preconfigured non-interactive uGUI Image
under `objectToMove`. No components are created for icon notifications. The prefab's canvas, movement distance, position spread and
moving transform scale are reused.

DOTween grows the icon with OutBack easing, moves it along Icon Move Direction with OutCubic easing,
then shrinks and fades it to zero. The UnitNotification prefab exposes icon size
(48 x 48 local units), lifetime (0.9 s), growth (0.2 s), disappearance (0.25 s)
and peak scale (1.2 times the moving transform's initial scale).
Animation uses scaled time. Completion destroys the instance; disabling it kills
its sequence and destroys it. Text notifications retain their existing animation.
Visual timing and appearance have not been previewed in Unity.

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

Critical Charge (2026-10-04) adds the registered term `criticalCharge` and a static
Descriptions tooltip. Status text shows 1–3 charges and the border tracks its shared
four-second timer; assign the new visual type in EffectIconsConfig manually. See
[the effect contract](ReactiveCombatEffects.md#critical-charge-2026-10-04).

Scoped verification: dotnet build Assembly-CSharp.csproj --no-restore -v:q passed
with zero errors and assembly-reference conflict warnings; no Unity visual preview.

### Proc icon direction and circular fade (2026-10-07)

The notification prefab now exposes Icon Move Direction (world XY, normalized;
default -1, 1 for up-left) and Icon Move Distance (default 1.3 world units).
A zero direction keeps the icon stationary. Text movement settings are separate.

ProcIcon uses `Assets/Materials/UIProcIconCircle.mat` with the `UI/Proc Icon Circle`
shader. Circle Radius defaults to 0.5 of the shorter rendered side; Edge Fade Width
(default 0.08) fades alpha smoothly inward from its boundary. Image tint, sprite
alpha and the animation fade multiply the circular coverage. The existing
UIBarContour mesh effect is configured on ProcIcon in the prefab to supply UV1
coordinates and aspect ratio; no component is created at runtime. This keeps the
circle independent of sprite atlas UVs, transform scale and Canvas batching.
UI stencil masking and RectMask2D softness are supported. No icon artwork changes.
Shader compilation/rendering has not been verified in Unity.
Material assignment correction: ProcIcon Image's saved m_Material reference was
found empty and explicitly assigned to UIProcIconCircle (GUID cf5e5a5743ed4bd89db5fa7bf9ea4cec).
The saved reference and material/shader metadata were checked. Unity CLI found no
connected Pipeline Editor, so Editor import/rendering remains unverified.

The six additional modifier notification gates and configured assets are documented
in [Stats and modifiers](StatsAndModifiers.md#additional-queued-modifier-proc-icons-2026-10-07).

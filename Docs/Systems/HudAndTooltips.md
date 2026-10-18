# HUD, tooltips and visual references

[Home](../Home.md) · [Project map](../ProjectMap.md) · [Screen flow](MenusAndScreenFlow.md)

Status: **source-reviewed, 2026-09-28**. Core health display and tooltip ownership
paths inspected. This is a component map, not a scene-wide hierarchy, appearance
or performance audit. Existing specialized visual records retain their own limits.

## Values and presentation

Stat display fix, 2026-10-17: PlayerStatsWindow's simplified DPS clamps expected
ordinary critical layers to 0–1, and uses nonnegative uncapped chance when an
applicable OnAttack MultiCritModifier is collected. Nonpositive attack speed gives
zero estimated DPS. Zero/negative barrier regeneration displays an em dash, with
no seconds suffix, rather than an invalid interval. See the
[fix record](../Reference/StatTypeFixes20261017.md). Unity compilation passed;
no visual check or gameplay playtest was performed.

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

### Player stats defence layout (2026-10-12)

MainScene's authored PlayerStatsWindow contains a two-column, two-row defence grid,
including Block Power and its existing tooltip description. The earlier isolated
Edit Mode render verified this authored layout only.

The full PlayerStatsWindow layout is now authored and saved in MainScene. Runtime
code does not create, reparent or resize UI objects. Edit the RectTransforms, layout
groups, text components, separator Images and ContentSizeFitter directly in the
scene hierarchy. The Content layout retains its 20-unit right inset.

Attributes, Health & Barrier, Defences, Attack and Mystic remain always visible
without dropdown headers, preserving their authored columns/grids. Resistances
follow Mystic; separate maximum-resistance rows are omitted. Existing resistance
suffixes show uncapped resistance above the cap, not the cap itself. Health & Barrier
includes MysticNegation. The Parry pair is visible only when ParryChance > 0
(corrected on 2026-10-13), unless Show all stats is enabled. Recalculation changes
visibility and rebuilds layout only when that condition changes.

Ailments, Debuffs, Others and its five nested dropdowns have eight serialized
PlayerStatsSection entries, each with a persistent Button callback.
Their header Buttons have persistent ToggleSection(index) callbacks. Each entry
exposes its title, localization key/fallback, triangle text, body and Expanded state.
Right/down triangles identify collapsed/expanded state. Ailments retain their authored
internal groups; Debuffs use nine separate rows. Others contains five simple
subsections: Recovery & Pain, Sacred Health, Damage Reduction, Resistance Penetration
and Wisps. Their title references/keys, roots and bodies are stored in sections. Bodies
retain the 14-unit inset and subsection rows an additional 10-unit inset.

Others subsections hide when all of their configured visibilityStats equal their
defaultValue (currently 0, compared with Mathf.Approximately). Nonzero negative values
also make a subsection visible. Others and its preceding divider hide when all five
subsection roots are hidden. Visibility uses root activeSelf, so collapsing a body
does not incorrectly make its parent empty. These conditions refresh with stats;
foldout state is preserved when a subsection temporarily hides.

A centered Show all stats Toggle is saved at the end of Content, outside Others.
Its persistent SetShowAllStats(bool) callback immediately disables/enables the
zero-value filter, including Parry. It does not override manually collapsed bodies.
The toggle defaults off and lasts for the window instance, without profile storage.
Its English Descriptions key is ui.playerStats.showAllStats = Show all stats;
Russian/German translations remain TODOs.

Preview All Sections on PlayerStatsWindow expands all eight dropdowns in Edit Mode
and exposes empty subsections for authoring independently of the gameplay toggle.
Turn it off to preview individual Expanded flags in the same Inspector. This preview
updates through a deferred OnValidate callback without entering Play Mode. Awake
applies the configured Expanded states for gameplay (all eight default to collapsed).
The runtime preserves toggles across disable/re-enable for that instance; no profile
save field is added. Normal Buttons invoke their persistent callbacks in Play Mode;
Edit Mode preview is controlled through the Inspector.

All 67 statTexts entries and their label/value references are saved in the scene.
The existing stat/locale subscriptions update values and headings. Existing tooltip
components remain attached; no new tooltip assets are created. Missing numeric
values display zero. Damage Reduction displays the six final physical/fire/cold/
lightning/light/darkness values: general/elemental/mystic modifiers merge before
calculation, and poison/aggregate rows are deliberately omitted. Earlier excluded
numeric aggregates, masks and generic ailment stats remain excluded.

Headings use Descriptions keys ui.playerStats.section.<id>. English entries were
added; newly introduced keys remain Russian/German translator TODOs. Existing
translations and unused older heading keys are preserved.

Verification: the scene was authored/saved through Unity CLI in Edit Mode. Scoped
C# compilation passed; Unity reports no current Console errors. Top/bottom isolated
renders of the saved layout were inspected, including right scrollbar clearance.
Direct section toggling expanded/collapsed the saved body. The 2026-10-13 checks
covered all-zero hiding, one negative non-default value, positive Parry chance,
nested toggling and both Show all stats states using supplied stat-value delegates,
without changing gameplay stats. The saved nested dropdown/checkbox render was
inspected. Actual pointer-click
playback, combat updates and locale switching were not playtested.

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
AilmentAbsorptionCooldown also requests this floating UnitNotification icon when
its charged effect actually absorbs an incoming ailment (2026-10-13). Charging or
recharging alone does not emit a notification. The modifier's procIcon is passed
to its effect, and AilmentAbsorptionCooldown_5 uses AilmentAbsorbtionIcon. The
existing persistent status icon and shared notification queue remain in use.
Scoped verification: `dotnet build Assembly-CSharp.csproj --no-restore -v:minimal -m:1`
passed with zero errors and 20 dependency/other-source warnings. Notification
appearance in live combat was not previewed.
`UnitNotificationEffect.ShowIcon` configures a fresh instance before `Start`, hides
its TMP text and critical decoration, and enables the preconfigured non-interactive uGUI Image
under `objectToMove`. No components are created for icon notifications. The prefab's canvas, movement distance, position spread and
moving transform scale are reused.

DOTween grows the icon with OutBack easing, moves it along Icon Move Direction with OutCubic easing,
then shrinks and fades it to zero. The UnitNotification prefab exposes icon size
(48 x 48 local units), lifetime (0.9 s), growth (0.2 s), disappearance (0.25 s)
and peak scale (1.2 times the moving transform's initial scale).
Animation uses scaled time. Completion destroys the instance; disabling it kills
its sequence and destroys it. Text notifications use the separate animation below.
Icon timing and appearance have not been previewed in Unity.

## Floating text direction and impact (2026-10-15)

`UnitNotificationEffect` animates damage, healing and message text through the
existing TMP component. Its shared code also serves GoldNotification. Proc icons
retain their own direction, timing and DOTween sequence. No combat values or
critical-hit mechanics change; the retired critical decoration stays disabled.

Configure **Text flight** on `Assets/Prefabs/Visual/UnitNotification.prefab`:

- Text Move Direction is a normalized world XY vector: `0, 1` up, `1, 0` right,
  `-1, 0` left, `1, 1` diagonally up-right. Zero keeps the spawned text stationary.
- Text Direction Spread adds a random angle in either direction; the currently
  authored prefab uses 12 degrees and 10% Text Distance Variation. Its direction
  is `0.35, 1` and Move Distance is 1.5. Set spread to zero for an exact direction.
- Move Distance remains the total travel distance. Text Arc adds a random sideways
  bend as a fraction of that distance, default 0.12; zero makes the path straight.
- Life Time remains 0.8 seconds. Movement starts quickly and decelerates using
  elapsed scaled time, replacing the previous frame-dependent position Lerp.
- Text starts at 0.6 of its authored scale, reaches Scale Multiplier (1.4) in
  0.084 seconds, and settles to the authored scale by Effect Duration (0.3 seconds).
  Text Stretch controls the brief vertical stretch and horizontal squash;
  Text Tilt controls the initial random tilt, which settles to the authored rotation.
- Text Fade Portion fades alpha and slightly shrinks the text over the final 32%
  of its lifetime. Original damage/shield/heal/message colors are preserved.

Text animation now runs in a scaled-time DOTween Sequence, without a component
Update timer. A virtual movement tween uses OutCubic for ordinary text and InCubic
for critical text, including the sideways arc. Separate tweens drive OutCubic
growth, linear-phase sinusoidal stretch, InOutSine settling/rotation, and InOutSine
fade/shrink. Growth, stretch and fade scale compose independently, so changing the
fade window to overlap growth does not create competing transform writes.
Z position and scale are preserved. Sequence completion destroys the notification;
OnDisable kills the text and icon sequences, then destroys the disposable instance
during gameplay. The legacy InitTargetPos entry point
can supply an away-from-attacker direction before Start, without activating a
critical decoration; current health notifications use the prefab direction.

Verification on 2026-10-15: Unity CLI `recompile_status` reported completion with no
compilation errors. An isolated preview rendered the actual prefab at five times
for up, up-right and left, and the frames were inspected. The preview scene was
closed without saving the user's active scene. This verifies phase appearance and
direction changes, not live combat overlap, sustained burst readability or runtime
performance. No tests or player build were run.

### Critical damage numbers

The **Critical damage text** settings on UnitNotification configure critical
numbers: Critical Text Speed Multiplier defaults to 1.4, Critical Text Scale
Multiplier to 1.2, and Critical Text Color to golden orange (`#FFAD29`).
Speed shortens travel and the initial pop/settle animation, preserving the same
distance, direction, lifetime and fade window so numbers remain readable.
Critical movement uses InCubic easing: it starts slowly and accelerates toward
the destination. Ordinary text movement retains OutCubic easing. The size
multiplier applies to both the initial peak and settled text.

`Unit.ReceiveDamage` passes `DamageInfo.IsCritical` to the optional isCritical
parameter of `Health.TakeDamage`. Health emits OnHealthChangedNotification with
the actual HP delta and that flag when displayDamage is enabled. Healing emits
the same event with a false flag. UnitVisual uses this event instead of
OnHealthChangedDelta, producing one number per displayed nonzero delta; the old
event remains available for existing listeners. There is no pending critical
state that could carry over to another hit. Fully absorbed attacks display no HP
number, and DoT/debt paths retain their existing displayDamage=false behavior.
Crit styling takes priority over the optional shield tint in WriteDamage.
The old critical particle decoration remains disabled.

Scoped verification: Unity compilation completed without errors. The actual saved
prefab was rendered at five matching times for ordinary and critical text; size,
color and faster travel were visually inspected. Console had no current errors;
its retained historical entries concern the earlier preview cleanup and an
unrelated death visual. The event routing was source-reviewed, not playtested in
combat. No tests or player build were run.

DOTween refactor verification, 2026-10-15: Unity `recompile_status` completed
without errors. An isolated preview sought the actual text Sequence through five
times for normal and critical numbers, rendered both rows and inspected the
result. No current Console errors were reported. The preview compiler reported
CS1701 compatibility warnings for DOTween's older mscorlib reference; execution
succeeded. This is phase/render evidence, not live combat or completion/pause
playback. The active scene and serialized animation settings were not changed.

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

## Hidden service effects (2026-10-18)

Status visibility update, 2026-10-18: BaseEffect.ShowStatusIcon defaults to true.
UnitVisualEffectsController skips effects that opt out before icon grouping.
PeriodicElementalChargesEffect opts out to hide its internal generation timer;
the resulting Embers, Rime and Sparks retain their existing icons and counts.
No serialized UI settings or EffectVisualType IDs changed. See
[periodic charges](ReactiveCombatEffects.md#periodic-elemental-charge-generation-2026-10-18).
Compilation/source verification is recorded there; no visual playtest was run.

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

## Block Power tooltip (2026-10-07)

The registered `blockPower` term resolves to `Descriptions/BlockPower.asset`,
using Descriptions keys `description.blockPower1` and `description.blockPower2`.
English text explains flat subtraction, proportional reduction and the base value
of 10, confirmed in UnitInnateStats. `description.block1` links to this term,
and all six `modifier.container.<semantic>.BlockPower` English entries in Modifiers
use `{blockPower|Block Power}`. The Descriptions enum label is
`enum.StatType.BlockPower`. Russian and German tables were left unchanged; the new
keys remain translator TODOs. Serialized IDs, keys and GUID references were checked;
no Unity tooltip preview was performed.

## Parry text notification (2026-10-08)

UnitVisual now subscribes to Unit.OnBlockResolved for ordinary Block text. It
skips that text when AttackContext.IsParried is true; existing OnParry shows Parry.
Queued modifier proc icons remain independent. Parry tooltip uses Descriptions
keys combat.notification.parry, description.parry1 and description.parry2; both
parry and parryChance terms resolve to the existing Parry description asset.
The linked block and blockPower descriptions are registered in TooltipTerms.


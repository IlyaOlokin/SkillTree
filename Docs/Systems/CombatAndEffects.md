# Combat and Effects

[Home](../Home.md) · [Project map](../ProjectMap.md) · [Stats and modifiers](StatsAndModifiers.md)

Status: **source-reviewed, 2026-09-27**. Scope: combat scheduling, attack ordering,
damage-receipt boundaries and effect lifecycle. Companion pages now cover
[defences and resources](DefencesAndResources.md), [ailments](AilmentsAndDebuffs.md)
and [enemy generation](LocationsAndEnemies.md); navigation and receipt wording
were updated on 2026-09-28.
No Unity playtest was performed for this pass.

Companion references cover [reactive buffs and stored attack resources](ReactiveCombatEffects.md)
and [mini-game reward effects](BattleMiniGames.md).

## Combat clock

`BattleTickSystem` runs explicit phases for registered combat participants:

| Phase | `Unit` work |
| --- | --- |
| `Mods` | Process pending modifier recalculation |
| `Resources` | Tick health, barrier and mystic health |
| `Effects` | Tick active effects |
| `Actions` | Advance attacks |

Each phase visits the registered participants before the next phase begins.
Code defaults are a `1/60` second step, unscaled time, speed multiplier `1`, and
at most eight ticks per frame; serialized scene values can override these defaults.
Unprocessed time remains in the accumulator. Registration changes during a tick
are deferred until the tick finishes.

`Pause`/`Resume` control the ordinary pause flag. `AcquirePause(owner)` and
`ReleasePause(owner)` provide independent modal locks; `Resume` cannot remove
another owner's lock. Because the clock can use unscaled time, setting
`Time.timeScale = 0` alone is not a reliable combat pause. See the
[tutorial guide](../Tutorials/README.md) for its pause integration.

## Attack scheduling and target ownership

`Attacker` accumulates attack progress using calculated attack speed. One full
cycle requires progress `1`; extra attack moments can trigger within a cycle.
The loop is bounded to 128 cycles per tick and retains accumulated progress.
External progress locks block `ModifyAttackProgress`, not ordinary clock advancement.
The current suppression check prevents attacking while the owner has Freeze.

`AttackResolver` provides the player's current enemy target. `Attacker` resolves
that target before attack-start callbacks, so a selection change during those
callbacks affects subsequent attacks. The processor also resolves dynamic target
providers once when called directly.

The normal attacker path emits `OnAttack`, copies the owner's stat buckets into
a reusable attack snapshot, resets `DamageInfo`, processes the attack, and then
emits `OnAttackCompleted`. Do not retain the mutable `DamageInfo` object as a
permanent historical record: the attacker reuses it.

## Attack pipeline

The following order is taken from `AttackProcessor.HandleAttack`:

1. Resolve the defender and create `AttackContext`.
2. Check evasion. On success, notify defender evade and attacker miss, then stop.
3. Notify attacker `OnHit`; apply Overcharge, Pain and Vengeance attack effects,
   then attacker `OnAttack`-priority modifiers.
4. Recalculate critical chance, roll critical layers, then apply attacker
   `AfterCriticalHit` modifiers.
5. Calculate damage from the attack snapshot, including critical damage.
6. Apply defender `IncomingPreMitigation` modifiers.
7. Apply armor, resistance, mystic negation and per-type damage mitigation.
8. Apply defender `OnGettingHit`-priority modifiers.
9. Attempt Bleed, Ignite, Chill, Overcharge, Sunder, Distract and Expose application.
10. Check block; a block notifies the defender and invokes parry handling.
11. Call defender `ReceiveDamage`.
12. Attempt the applied Chill-to-Freeze upgrade, resolve queued successful-hit
    actions, apply lifesteal and emit attacker crit/non-crit notification.
13. Call `DamageDealt` (currently an empty hook), consume queued effects and finish.

`RunAfterCurrentAttack` defers actions until the outermost processor invocation
finishes. This is distinct from `AttackContext`'s successful-hit actions and effect
consumption queues. Choose the boundary required by the mechanic.

The sequence is implementation behavior, not permission to enable unfinished
content. In particular, existing Freeze behavior does not override tree-authoring
restrictions in the [filling guide](../SkillTree/SkillTreeFillingGuide.md).

## Events that are easy to confuse

| Event or phase | Meaning |
| --- | --- |
| `Unit.OnAttack` | Attack-start callback, before the normal attacker copies the snapshot |
| `ModifierPriority.OnAttack` | Attack modifier phase, after evasion has failed |
| `Unit.OnHit` | Evasion failed; damage has not yet been calculated or applied |
| `ModifierPriority.OnGettingHit` | Defender modifier phase after mitigation, before ailments and block |
| `Unit.OnGettingHit` | Damage-receipt callback after barrier, mystic and health processing |
| `Unit.OnHealthDamageTaken` | Receipt caused positive actual HP loss |
| `Unit.OnAttackCompleted` | Normal attacker callback after the processor returns, including an evaded attack |

Do not equate a hit notification with positive HP loss. Effects are attempted
before block in the current pipeline, so blocking is not a blanket cancellation
of all on-hit or ailment behavior.

## Damage and resource boundaries

`DamageCalculator` reads typed damage values from the snapshot. Critical damage
uses `1 + CritDamageBonus × CriticalLayerCount`. Normal crits have zero or one
layer. When multi-crit is enabled, positive chance supplies whole guaranteed
layers plus a roll for the fractional remainder.

`Unit.ReceiveDamage` processes barrier damage first, converts mystic damage into
absorption, then applies health damage. It measures actual health lost for
`OnHealthDamageTaken`. These stages mutate the received damage object.

`ReceiveDoT` directly calls health damage with `displayDamage = false`; it does not run
the normal attack pipeline, barrier receipt or hit callbacks. Inspect the specific
effect and `Health` implementation before extending DoT mitigation behavior.
See [resource formulas](DefencesAndResources.md) and
[ailment damage pools](AilmentsAndDebuffs.md#damage-over-time-pools) for details.

## Effect lifecycle and stacking

### Deferred attack HP damage (2026-10-03)

`Unit.OnBeforeAttackHealthDamage` receives a mutable copy of the attack damage after
barrier receipt and mystic absorption, immediately before `Health.TakeDamage`.
It leaves the original post-defence attack payload intact for source-side reactions.
`HealthDamageTaken` and `OnHealthDamageTaken` measure immediate HP loss only.
`ReceiveDoT` does not enter this stage.

`DeferredAttackDamageModifier` follows the per-owner delegate-binding pattern of
DamageTakenAsPain, but uses this new pre-HP stage. It moves a powered fraction
(clamped to 0..1, default 0.30) of positive physical/fire/cold/lightning damage into
an independent `DamageDebtEffect`. Duration is configurable, at least 0.01 seconds,
default 5, and does not scale with node power. Light/darkness absorption is unchanged.
Multiple copies process remaining immediate damage in subscription order: two
unpowered 30% copies defer 51% in total, not 60%. Their deadlines stay independent.

Each debt pays at its original damage divided by its original duration. The existing
effect controller caps the final tick to remaining lifetime. Payments call
`Health.TakeDamage` directly with their original damage types: no repeat mitigation,
barrier consumption, new debt, attack hit events or Pain gain. Payments pass
`displayDamage: false`, so they do not emit floating damage numbers. Health-change/death
and mystic absorption death-threshold validation still run. Debt is a separate effect,
not an ailment, and receives no ailment mitigation. Adding it without a repeat factory
prevents generic received-effect reactions from copying the debt.

Effects are non-stackable with a no-op OnStack: every hit has a separate active entry
and new hits never refresh prior entries. Default icon grouping gives one DamageDebt
icon (enum value 26); its text is the ceiling of the sum of remaining damage. Its
timer border follows the nearest-expiring debt. Runtime rebinding or removing the
modifier stops future deferral but preserves existing debts. ResetCombatState and
ordinary effect clearing discard debts. They are not persisted across combat resets.

The supplied definition is
`Assets/Scripts/SkillTree/Modifiers/ReactMods/DeferredAttackDamage.asset`.
Node assignment and icon mapping remain manual. See
[localization and setup](StatsAndModifiers.md#deferred-attack-damage-2026-10-03).

Verification: direct Roslyn `csc.exe @Temp/DamageDebtCompile/compile.rsp` compiled
the gameplay C# sources against installed Unity references with exit code 0.
The response file adds the two new sources and references existing ScriptAssemblies
for generated project dependencies. MSBuild was unavailable because SDK discovery
could not access the local Microsoft SDKs directory. No tests, player build or Unity
playtest were run; visual integration and actual combat behavior remain unverified.

`BaseEffect` defines apply, stack, tick, consume and remove hooks. `ActiveEffect`
holds an effect and its remaining time. `EffectController` owns the active list.

- Matching is by exact runtime type. If an existing effect is found, its
  `OnStack` runs. If that existing effect is stackable, the incoming candidate is
  not added separately; otherwise a new active entry is added afterward.
- Tick iteration uses a snapshot and checks membership again after callbacks.
  A callback may remove effects, add replacements or clear the owner.
- Timed effects receive at most their remaining lifetime in the last tick.
  Negative remaining time denotes an untimed effect, still subject to its
  `IsReadyToBeRemoved` condition.
- Removal detaches the entry, invokes `OnRemove`, then releases owned runtime
  modifiers in a `finally` block.
- Clearing rejects newly added effects and releases their owned runtime modifiers,
  preventing removal callbacks from repopulating the list during cleanup.

`ResetCombatState` restores health and barriers, resets mystic health, clears
effects and hard-resets attack progress. A temporary buff must clean up its
subscriptions and modifiers through the intended lifecycle.

## Attribution and repeated effects

Use the [effect application event contract](../Reference/EffectApplicationEvents.md) when
adding source-side or recipient-side reactions. Supplying a source to the factory
overload allows the accepted application to notify that source. Repeated effects
use `AddRepeatedEffect`, which suppresses both source application and recipient
received-effect events while retaining ordinary effect lifecycle/change callbacks.

Generic effect receipt and semantic ailment notifications are separate. Do not
call an effect factory just to discover its type: construction may allocate runtime
modifiers. The [modifier page](StatsAndModifiers.md) explains binding ownership.

## Verification when changing combat

From the repository root, the existing .NET 9 runner is:

```powershell
dotnet run --project Tests/CombatRegression
```

It links selected production files against stubs. It is not a full Unity build,
full-pipeline test, rendering check or performance measurement; consult
`Tests/CombatRegression/README.md` and the project file for its actual coverage.
This documentation-only pass did not run the combat suite.

For relevant changes, verify miss/hit/crit/block branches, target changes inside
callbacks, repeated attacks without persistent stat drift, expiry during a large
tick, effect removal during callbacks, reset cleanup and overlapping pause owners.
Use Unity Console and a targeted play scenario for lifecycle and scene integration.

## Source entry points

Repository-relative paths, outside this vault:

- `Assets/Scripts/Battle/BattleTickSystem.cs`
- `Assets/Scripts/Battle/Unit.cs`
- `Assets/Scripts/Battle/Attacker.cs`
- `Assets/Scripts/Battle/AttackResolver.cs`
- `Assets/Scripts/Battle/AttackProcessor.cs`
- `Assets/Scripts/Battle/AttackContext.cs`
- `Assets/Scripts/Battle/DamageInfo.cs`
- `Assets/Scripts/Battle/DamageCalculator.cs`
- `Assets/Scripts/Battle/Effects/EffectController.cs`
- `Assets/Scripts/Battle/Effects/BaseEffect.cs`
- `Assets/Scripts/Battle/Effects/ActiveEffect.cs`

Unit.OnAttackPrepared receives the mutable attack snapshot after DamageInfo.Reset and before evasion in the normal Attacker path. It supports pre-paid attack-local modifiers; completion remains the counter-advance boundary. See [barrier sacrifice attacks](StatsAndModifiers.md#barrier-sacrifice-attacks).

## Parry block power and final notification (2026-10-08)

Block captures its flat BlockPower and absorbs it proportionally across damage
components, then emits existing OnBlock reactions. A successful Parry absorbs
the same captured amount again before its callbacks. OnBlockResolved is emitted
after the parry roll, preserving both gameplay reactions while allowing UnitVisual
to suppress Block text on parried hits. Parry Attack Progress remains deferred.


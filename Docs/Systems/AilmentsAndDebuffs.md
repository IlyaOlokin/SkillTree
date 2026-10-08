# Ailments and attack debuffs

[Home](../Home.md) · [Project map](../ProjectMap.md) · [Combat pipeline](CombatAndEffects.md) · [Defences](DefencesAndResources.md)

Status: **source-reviewed, 2026-09-28**. Covers Bleed, Ignite, Chill/Freeze,
Overcharge, Sunder, Distract, Expose and incoming ailment absorption. This is not
an exhaustive catalog of buffs, reactive tree modifiers or mini-game effects.
No Unity playtest was run. Runtime support does not override the
[skill-tree authoring restrictions](../SkillTree/SkillTreeFillingGuide.md).

## Shared application boundary

The attack processor tries effects after armor, resistance, mystic negation and
per-type mitigation, but **before block, barrier and HP receipt**. Thus damage
used for their initial chance/power is not final HP lost. Chill's later Freeze
upgrade is the exception: it tests the recorded HP damage after receipt.

For ordinary Bleed/Ignite/Chill/Overcharge application, the matching physical,
fire, cold or lightning component must be positive. The chance comparison is:

`random(0, 1) < matchingDamage / defenderMaximumHealth * (1 + matchingChanceStat)`

The chance stat scales a damage-derived ratio; it is not an independent flat
chance. These paths do not explicitly clamp that ratio. Suppression exits before
application. Payload redirection can put the effect on the attacker, but the
ratio still uses the original defender's HP. Overcharge avoidance also checks
the original defender before redirection.

The four ailments consult `AilmentAbsorption` on the eventual recipient after
their roll (and Overcharge avoidance). Bleed additionally has a guaranteed path,
which still requires positive physical damage and checks absorption. The inspected
Ignite, Chill and Overcharge application methods do not consult a guaranteed flag.

For source/recipient events, factories and safe bonus copies, use the
[effect application contract](../Reference/EffectApplicationEvents.md). Generic accepted-effect
events and semantic `OnAilmentApplied`/`OnBleedApplied` events are distinct.

## Damage-over-time pools

| Property | Bleed | Ignite |
| --- | --- | --- |
| Initial damage | `physicalDamage * 0.3 * (1 + BleedPower) * (1 - min(1, BleedMitigation))`, bounded at zero | `baseDamage * 0.3 * (1 + IgnitePower) * (1 - min(1, IgniteMitigation))` |
| Lifetime | Five seconds per instance | Until remaining damage is exhausted |
| Multiple applications | Separate instances (`IsStackable = false`) | Add damage to one existing pool (`IsStackable = true`) |
| Tick | Initial pool / five seconds, capped by remaining damage/lifetime | 40% of the remaining pool per second; below one remaining damage, requests one damage per second |
| Burst | Removes and deals a clamped fraction of remaining damage | Removes and deals a clamped fraction of remaining damage |

Ignite normally uses fire damage as its base. A payload option also includes
lightning in the damage pool, while the chance and positive-damage gate still use
fire only. Both mitigation stats have an upper bound of one here but no lower
bound: negative protection increases the pool.

Bleed settles any floating-point remainder on its last tick. A burst consumes
existing damage rather than adding a second copy. `TryMergeStacks` merges **all**
current Bleed instances once the count reaches the threshold, multiplies their
remaining sum by `1 + max(0, moreDamage)`, and starts a fresh five-second instance.
That internal merge does not supply a source for application events.

Ignite drains a fraction of its remaining pool, so its usual tick rate declines
over time; it is not uniform damage over a fixed duration. Bursts and ticks cannot
deal more than its positive remaining pool. Both effects call `ReceiveDoT`, whose
resource and event boundaries are described in [defences](DefencesAndResources.md).

## Chill and Freeze

Chill supplies an `Increased AttackSpeed` modifier of
`-0.2 * (1 + ChillPower)`. Additional payload modifiers are scaled by the same
power. Duration is `3 * (1 - recipientChillDurationReduction)` seconds.
Reapplication preserves the longer remaining duration but replaces modifier values
with the newest Chill, even when the new effect is weaker. Removal releases its
outer modifiers.

**Boundary concern:** duration reduction is not clamped in this constructor.
Values above one create a negative duration, which the controller treats as
untimed. This is an implementation risk to test, not an intended immunity rule.

A hit that applies Chill upgrades it to Freeze if recorded HP damage divided by
the original defender's maximum HP is **strictly greater than 0.4**. The check is
not cold-only and does not use pre-block damage. It removes Chill from the stored
effect target and applies Freeze there, including when Chill was redirected.

Freeze runs its own two-second timer, applies `More AttackSpeed = -1`, and is also
checked explicitly by the attacker suppression logic. Reapplication extends the
timer to at least the new timer and replaces the stored Chill factory. On natural
expiry it reapplies the saved Chill snapshot without a source argument. Forced
removal does not reapply Chill. The saved snapshot uses its original duration,
not the remaining duration just before Freeze.

## Overcharge

Overcharge lives on the **defender** and boosts the attack hitting that defender.
At the next attack that passes evasion, the processor applies every existing
Overcharge instance to the incoming attack snapshot:

- `More Damage = 0.1 * (1 + applicationOverchargePower)`.
- `More CritDamageBonus = 0.2 * (1 + applicationOverchargePower)`.

These are separate More modifiers per instance, with the stat arithmetic described
in [stats and modifiers](StatsAndModifiers.md). Instances are queued for consumption
at the attack's consumption boundary. They are not a periodic lightning DoT, nor
a buff to the afflicted unit's outgoing attack. New Overcharge applied later in
the same attack remains for a subsequent hit. An evaded attack never reaches this
consumption path. Independent instances share an icon count.

## Sunder, Distract and Expose

These use a flat `clamp01(matchingChance)` roll, or their payload's guaranteed path.
They do not require a positive matching damage component and do not consult
`AilmentAbsorption`. All last five seconds and merge by extending remaining time
to the larger duration while replacing strength with the newest application.

| Effect | Increased modifier | Base value |
| --- | --- | --- |
| Sunder | Armor | -0.2 |
| Distract | Accuracy | -0.2 |
| Expose | AilmentGuard | -0.3 |

The applied value is `base * max(0, 1 + matchingPower) * (1 - clamp01(recipientMitigation))`.
These change an Increased bucket; they are not independent final-stat multipliers.
Sunder and Expose also carry extra payload modifiers. Distract can reduce attack
progress by a payload value clamped to 0..1, but only when the effect remains on
the original defender. Repeated-effect factories alone do not reproduce that
separate application-side action.

Sunder does not retroactively change armor mitigation already calculated for its
triggering hit. Likewise Expose is attempted after the four main ailments; do not
assume it changes their already-resolved chance or protection on that hit.

## Ailment absorption

`AilmentAbsorption` is a rechargeable effect tied to a collected source modifier.
With a positive cooldown it starts uncharged, becomes charged through effect
ticks, absorbs one incoming ailment and starts its cooldown again. Zero cooldown
charges immediately, including immediately after absorption. Reapplication updates
the source/cooldown, preserves a charged state, and shortens an uncharged remaining
wait if needed. Its tick marks it for removal when its source is no longer collected.

The check lives in the four ailments' application methods, not in generic
`EffectController.AddEffect`. Direct additions and bonus copies therefore do not
automatically roll avoidance or consume absorption again. This differs from the
signed mystic damage resource described in [defences](DefencesAndResources.md).

## Verification and source entry points

Recommended checks: chance at low/high damage ratios; suppression/redirection;
pre-block application; repeated applications with weaker strength; expiry during
large ticks; Bleed burst/merge conservation; Ignite draining; Overcharge on miss,
hit and blocked hit; exactly 40% versus greater-than-40% Freeze; forced removal;
negative Chill duration; absorption charge, recharge and source removal.

The existing `Tests/CombatRegression/Program.cs` checks Bleed timing, burst/merge
and effect lifecycle cases with stubs. It is not full ailment integration coverage.
`dotnet run --project Tests/CombatRegression` was not run for this documentation pass.

- `Assets/Scripts/Battle/AttackProcessor.cs`
- `Assets/Scripts/Battle/AttackEffectPayload.cs`
- `Assets/Scripts/Battle/Effects/Bleed.cs`
- `Assets/Scripts/Battle/Effects/Ignite.cs`
- `Assets/Scripts/Battle/Effects/Chill.cs`
- `Assets/Scripts/Battle/Effects/Freeze.cs`
- `Assets/Scripts/Battle/Effects/Overcharge.cs`
- `Assets/Scripts/Battle/Effects/Sunder.cs`
- `Assets/Scripts/Battle/Effects/Distract.cs`
- `Assets/Scripts/Battle/Effects/Expose.cs`
- `Assets/Scripts/Battle/Effects/AilmentAbsorption.cs`
- `Assets/Scripts/Battle/Effects/EffectController.cs`

## Bleed healing conversion (2026-10-04)

With a collected `BleedHealsInsteadOfDamage` on the recipient, new Bleed pools
ignore Bleed Mitigation. Ticks and bursts consume the remaining pool as normal
but call `Health.TakeHeal` instead of `ReceiveDoT`. Healing Received and maximum
Health apply; over-healing still consumes the pool. The initial pool is a snapshot,
while conversion is checked at payment time; changes to collected modifiers do
not recalculate existing pools. Merge and incoming ailment absorption retain
existing behavior. Copies and node power do not amplify conversion. The existing
Bleed status icon remains; no separate healing status is added.
See [modifier implementation and manual setup](StatsAndModifiers.md#bleed-heals-instead-of-damage-2026-10-04)
for localization, the supplied asset and verification limits.

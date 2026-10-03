# Source-side effect application reactions

[Home](../Home.md) · [Combat and effects](../Systems/CombatAndEffects.md) · [Stats and modifiers](../Systems/StatsAndModifiers.md)

Use `Unit.OnEffectApplied` for modifiers that react to effects applied by their
owner. Use `EffectController.OnEffectReceived` for reactions on the recipient.
For individual application gates, stacking and consumption rules, read
[ailments and debuffs](../Systems/AilmentsAndDebuffs.md).

## Applying an attributed effect

After chance, avoidance, absorption and target redirection checks, call:

```csharp
target.effectController.AddEffect(() => new SomeEffect(...), source);
```

The controller raises `source.OnEffectApplied(target, effectType, effectFactory)`
after it accepts the effect (including stacking/refresh through `OnStack`) and
after the existing received-effect notification. The type is taken from the
created effect, not by invoking the factory again. Null factories, null effects
and effects rejected while the controller is clearing do not raise the event.

The factory must create a fresh effect with the original application parameters.
Listeners should use it synchronously; factories may capture attack-local data.
This event reports effect application, not completion of the attack or all of the
mechanic's other side effects.

Calls without a source retain their existing behavior and do not raise the
source-side event. Attribution is currently wired for attack applications of
Bleed, Ignite, Chill, Overcharge, Sunder, Distract and Expose, plus the Chill to
Freeze upgrade. Other sources, such as self-buffs or Chill returning after Freeze,
must explicitly supply a source if source-side reactions are desired.

## Reactive modifiers

Subscribe and unsubscribe with `DelegateModifierRuntimeBinding` returned from
`CreateRuntimeBinding`. Compute the powered chance for that binding, filter the
effect type before rolling, and apply the factory to the event's target.
`RepeatAppliedOverchargeChance` is the concrete example.

Use `target.effectController.AddRepeatedEffect(effectFactory)` for bonus copies.
It returns whether the effect was accepted and raises neither `OnEffectApplied`
nor `OnEffectReceived`, preventing repeat chains. Existing icon/change callbacks,
stacking, duration and effect lifecycle behavior still run normally.

`OnAilmentApplied` and `OnBleedApplied` remain separate semantic notifications;
the generic controller does not classify effects as ailments. Reactions that
repeat an ailment must preserve the appropriate notification on successful
application, as the Overcharge modifier does.

Multiple reactive modifier bindings roll independently. Two successful bindings
can each add one copy; they do not combine their chances into a single roll.

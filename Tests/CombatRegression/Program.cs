using System;
using System.Collections.Generic;
using System.Reflection;
using Battle;
using SkillTree;

internal static class Program
{
    private static int _passed;
    private static void Main()
    {
        Run("attack rates and tick sizes", AttackRates);
        Run("attack cap retains outstanding cycles", AttackCap);
        Run("extra attacks per cycle", ExtraAttacks);
        Run("freeze and callback cancellation", AttackCancellation);
        Run("bleed uniform full damage and oversized last tick", BleedDamage);
        Run("bleed burst and merged stacks conserve damage", BleedBurstAndMerge);
        Run("effect removal during tick", EffectReentrancy);
        Run("effect modifier ownership and discarded stacks", ModifierOwnership);
        Run("bonus zone reuses modifier and container", BonusZoneCache);
        Run("enemy package owns only generated modifiers", EnemyOwnership);
        Run("barrier maximum clamps current count", BarrierClamp);
        Run("equal-stack absorption type switches", AbsorptionSwitch);
        Console.WriteLine($"PASS: {_passed} regression groups");
    }
    private static void Run(string name, Action action)
    { action(); _passed++; Console.WriteLine("PASS: " + name); }
    private static void Check(bool condition, string message)
    { if (!condition) throw new Exception(message); }
    private static void Near(double actual, double expected, double tolerance = 0.001)
    { Check(Math.Abs(actual - expected) <= tolerance, $"Expected {expected}, got {actual}"); }
    private static Attacker MakeAttacker(Unit unit, float speed)
    {
        unit.BaseUnitModifiers.Stats[StatType.AttackSpeed] = speed;
        var attacker = new Attacker(); attacker.Init(unit); attacker.SetTarget(new Unit()); return attacker;
    }
    private static void AttackRates()
    {
        foreach (float speed in new[] { 0f, 1f, 2.5f, 7f, 60f, 120f })
        foreach (int tickRate in new[] { 20, 60, 144 })
        {
            var unit = new Unit(); var attacker = MakeAttacker(unit, speed);
            for (int i = 0; i < tickRate * 10; i++) attacker.CombatTick(1f / tickRate);
            Check(unit.Attacks == (int)(speed * 10), $"{speed}/s at {tickRate}Hz: {unit.Attacks} attacks");
        }
    }
    private static void AttackCap()
    {
        var unit = new Unit(); var attacker = MakeAttacker(unit, 300);
        attacker.CombatTick(1); Check(unit.Attacks == 128, "Attack limit");
        attacker.CombatTick(0); attacker.CombatTick(0);
        Check(unit.Attacks == 300, "Lost attack backlog"); Near(attacker.AttackProgress, 0);
    }
    private static void ExtraAttacks()
    {
        var unit = new Unit(); var attacker = MakeAttacker(unit, 3);
        attacker.AddExtraAttackMoment(0.5f); attacker.CombatTick(1);
        Check(unit.Attacks == 6, "Expected main and extra attack in each of 3 cycles");
    }
    private static void AttackCancellation()
    {
        var unit = new Unit(); var attacker = MakeAttacker(unit, 60);
        unit.effectController.AddEffect(new Freeze()); attacker.CombatTick(1);
        Check(unit.Attacks == 0, "Frozen attacker attacked"); Near(attacker.AttackProgress, 0);
        unit.effectController.ClearAllEffects();
        unit.AttackCallback = _ => unit.isActiveAndEnabled = false;
        attacker.CombatTick(1); Check(unit.Attacks == 1, "Attack continued after owner disabled");
    }
    private static Bleed MakeBleed(float damage)
    {
        return (Bleed)Activator.CreateInstance(typeof(Bleed), BindingFlags.Instance | BindingFlags.NonPublic,
            null, new object[] { damage, 5f }, null);
    }
    private static void BleedDamage()
    {
        foreach (float dt in new[] { 1f / 60, 0.1f, 0.3f, 1f, 7f })
        {
            var unit = new Unit(); var bleed = MakeBleed(100); unit.effectController.AddEffect(bleed);
            double elapsed = 0;
            while (unit.effectController.Effects.Count > 0 && elapsed < 6)
            {
                unit.effectController.CombatTick(dt); elapsed += dt;
                Near(unit.DotDamage, Math.Min(elapsed, 5) * 20, 0.003);
            }
            Near(unit.DotDamage, 100); Near(bleed.RemainingDamage, 0);
            Check(unit.effectController.Effects.Count == 0, "Bleed did not expire");
        }
    }
    private static void BleedBurstAndMerge()
    {
        var unit = new Unit(); var bleed = MakeBleed(100); unit.effectController.AddEffect(bleed);
        unit.effectController.CombatTick(1);
        bleed.TriggerBurst(unit, unit.effectController.Effects[0], 0.5f); Near(unit.DotDamage, 60);
        unit.effectController.CombatTick(5); Near(unit.DotDamage, 100);
        unit = new Unit(); unit.effectController.AddEffect(MakeBleed(100)); unit.effectController.AddEffect(MakeBleed(50));
        unit.effectController.CombatTick(1); Near(unit.DotDamage, 30);
        Bleed.TryMergeStacks(unit, 2, 0.5f);
        Check(unit.effectController.Effects.Count == 1, "Stacks not merged");
        for (int i = 0; i < 5; i++) unit.effectController.CombatTick(1);
        Near(unit.DotDamage, 210);
    }
    private sealed class OwnedEffect : BaseEffect
    {
        public readonly BaseModifier Modifier;
        public int Removed;
        public Action<Unit> Tick;
        public Action<Unit> Removing;
        public override bool IsStackable { get; set; } = true;
        public OwnedEffect() { Modifier = CreateRuntimeModifier<BaseModifier>(); }
        public override void OnTick(Unit unit, float dt) => Tick?.Invoke(unit);
        public override void OnRemove(Unit unit) { Removed++; Removing?.Invoke(unit); }
    }
    private static void EffectReentrancy()
    {
        var unit = new Unit(); var other = new OwnedEffect(); unit.effectController.AddEffect(other);
        unit.effectController.AddEffect(MakeBleed(100));
        unit.DotCallback = () => unit.effectController.ClearAllEffects();
        unit.effectController.CombatTick(1);
        Check(other.Removed == 1 && unit.effectController.Effects.Count == 0, "Reentrant clear failed");

        var replacement = new OwnedEffect();
        var replacing = new OwnedEffect();
        replacing.Tick = owner => { owner.effectController.ClearAllEffects(); owner.effectController.AddEffect(replacement); };
        unit.effectController.AddEffect(replacing);
        unit.effectController.CombatTick(1);
        Check(unit.effectController.Effects.Count == 1 && replacing.Removed == 1, "Replacement lost during tick");
    }
    private static void ModifierOwnership()
    {
        var unit = new Unit(); var first = new OwnedEffect(); var discarded = new OwnedEffect();
        unit.effectController.AddEffect(first); unit.effectController.AddEffect(discarded);
        Check(first.Modifier.DestroyCalls == 0 && discarded.Modifier.DestroyCalls == 1, "Stack ownership");
        var duringClear = new OwnedEffect();
        first.Removing = owner => owner.effectController.AddEffect(duringClear);
        unit.effectController.ClearAllEffects(); unit.effectController.ClearAllEffects();
        Check(first.Modifier.DestroyCalls == 1 && duringClear.Modifier.DestroyCalls == 1, "Clear ownership");
        Check(unit.effectController.Effects.Count == 0, "Effect survived clear");
    }
    private static void SetField(object target, string name, object value)
    { target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value); }
    private static void Invoke(object target, string name)
    { target.GetType().GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(target, null); }
    private static void BonusZoneCache()
    {
        var zone = new BonusZone(); var node = new Node();
        SetField(zone, "nodes", new List<Node> { node });
        SetField(zone, "modContainer", new ModifierContainer(ModifierType.Added, StatType.AttackSpeed, 2));
        Invoke(zone, "Awake"); var modifier = (BaseModifier)zone.CollectModifier(); var container = modifier.modifierContainer;
        node.SetActive(true);
        for (int i = 0; i < 100; i++) Check(ReferenceEquals(modifier, zone.CollectModifier()), "New modifier on collection");
        Check(ReferenceEquals(container, modifier.modifierContainer), "New container on collection"); Near(container.value, 2);
        node.IsActive = false; zone.CollectModifier(); Near(container.value, 0);
        Invoke(zone, "OnDestroy"); Check(modifier.DestroyCalls == 1, "Zone modifier leaked");
    }
    private static void EnemyOwnership()
    {
        foreach (bool owned in new[] { false, true })
        {
            var modifier = new BaseInnateModifiers();
            var data = new EnemySpawnData(null, EnemyRarity.Normal, 1, 1, modifier, ownsModifiers: owned);
            data.Dispose(); data.Dispose(); Check(modifier.DestroyCalls == (owned ? 1 : 0), "Enemy package ownership");
        }
    }
    private static void BarrierClamp()
    {
        var unit = new Unit(); var barrier = new Barrier(); barrier.Init(unit);
        unit.BaseUnitModifiers.Stats[StatType.BarrierCount] = 5; unit.RaiseStats(); barrier.RestoreFull();
        unit.BaseUnitModifiers.Stats[StatType.BarrierCount] = 2; unit.RaiseStats();
        Check(barrier.BarrierCount == 2 && barrier.MaxBarrierCount == 2, "Excess barriers retained");
        unit.BaseUnitModifiers.Stats[StatType.BarrierCount] = -2; unit.RaiseStats();
        Check(barrier.BarrierCount == 0 && barrier.MaxBarrierCount == 0, "Negative barrier count");
    }
    private static void AbsorptionSwitch()
    {
        var unit = new Unit(); var mystic = new MysticHealth(); mystic.Init(unit);
        int events = 0; mystic.OnAbsorptionStacksChanged += (_, stacks) => { Near(stacks, 2); events++; };
        mystic.ApplyMysticDamage(10, 0); mystic.ApplyMysticDamage(0, 20); mystic.ApplyMysticDamage(20, 0);
        Check(events == 3, "Missed equal-stack type notification");
        unit.effectController.CombatTick(0);
        Check(unit.effectController.HasEffect<LightAbsorption>() && !unit.effectController.HasEffect<DarknessAbsorption>(), "Wrong absorption effect");
        Check(unit.Outer.Count == 2, "Stale absorption modifiers");
        unit.effectController.ClearAllEffects(); Check(unit.Outer.Count == 0, "Absorption modifiers not removed");
    }
}

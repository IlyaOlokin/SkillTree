using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Battle;
using SkillTree;
using UnityEngine;

namespace BalanceParity
{
    // Compiled outside Assets. The same fixture runs under native Play Mode and
    // the simulator's Edit Mode ownership adapter, using actual production actors.
    [Serializable] public class Trace { public string name; public float step; public List<Frame> frames = new List<Frame>(); }
    [Serializable] public class Frame
    {
        public int tick, painEffects, scarEffects, igniteEffects, surgeEffects, received, applied;
        public float health, opponentHealth, pain, armor, healing, fire, regeneration, barrier, progress;
    }
    public sealed class Observer : ICombatTickable
    {
        public Action action;
        public void CombatTick(float dt, CombatTickPhase phase) { if (phase == CombatTickPhase.Actions) action(); }
    }
    public sealed class Scenario : IDisposable
    {
        static readonly BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        public readonly Trace trace;
        public readonly BattleTickSystem driver;
        public readonly Unit owner, opponent;
        public readonly Observer observer;
        public bool Complete { get; private set; }
        public int Limit => trace.name == "ignite-pool-and-expiry" ? 720 : 10;
        readonly List<GameObject> roots = new List<GameObject>();
        readonly List<UnityEngine.Object> definitions = new List<UnityEngine.Object>();
        readonly bool editMode;
        readonly Action<EffectController> takeOwnership;
        readonly Action<float> runTick;
        Modifier progressModifier, repeatModifier;
        int tick, received, applied;
        public static readonly string[] Names = { "pain-scar-consume-expire", "repeat-effect-no-recursion", "barrier-loss-and-hit-binding", "ignite-pool-and-expiry" };

        static void Set(object obj, Type declaringType, string name, object value) => declaringType.GetField(name, Private).SetValue(obj, value);
        public Scenario(string name, float step, bool editMode, Action<EffectController> takeOwnership = null)
        {
            this.editMode = editMode; this.takeOwnership = takeOwnership;
            trace = new Trace { name = name, step = step };
            var root = NewRoot("BalanceParityTick"); driver = root.AddComponent<BattleTickSystem>();
            driver.TickDuration = step; driver.SpeedMultiplier = 40; Set(driver, typeof(BattleTickSystem), "maxTicksPerFrame", 1000);
            runTick = (Action<float>)typeof(BattleTickSystem).GetMethod("RunTick", Private).CreateDelegate(typeof(Action<float>), driver);
            root.SetActive(true); driver.Pause(); driver.enabled = !editMode;
            owner = Actor("BalanceParityOwner", 20, 0, name == "pain-scar-consume-expire" || name == "repeat-effect-no-recursion" ? 100 : 0);
            opponent = Actor("BalanceParityOpponent", 40, 0, 0);
            owner.attacker.SetTarget(opponent); opponent.attacker.SetTarget(owner);
            if (name == "pain-scar-consume-expire" || name == "barrier-loss-and-hit-binding" || name == "ignite-pool-and-expiry") {
                var producer = Definition<BaseModifier>(); producer.modifierContainer = new ModifierContainer(ModifierType.Added, StatType.PainReceived, 0.5f);
                owner.AddOuterModifier(producer);
            }
            if (name == "pain-scar-consume-expire") {
                var spender = Definition<PainConsumedGrantsScar>(); Set(spender, typeof(PainConsumedGrantsScar), "painConsumedAsScar", 1f);
                Set(spender, typeof(PainConsumedGrantsScar), "duration", step * 3); owner.AddOuterModifier(spender);
            }
            if (name == "repeat-effect-no-recursion") {
                repeatModifier = Definition<RepeatReceivedEffectChance>(); Set(repeatModifier, typeof(RepeatReceivedEffectChance), "chance", 1f);
                owner.AddOuterModifier(repeatModifier);
            }
            if (name == "barrier-loss-and-hit-binding") {
                Add(owner, StatType.BarrierCount, 3); Add(owner, StatType.BarrierCapacity, 30);
                Add(owner, StatType.BarrierDamageTypeMask, (float)DamageType.Physical);
                Add(owner, StatType.BarrierRegenerationSpeed, 1); Add(owner, StatType.FireDamage, 10);
                progressModifier = Definition<GainAttackProgressOnHit>(); Set(progressModifier, typeof(GainAttackProgressOnHit), "attackProgressGain", 0.25f);
                owner.AddOuterModifier(progressModifier);
                var surge = Definition<BarrierLossGrantsBarrierSurge>(); Set(surge, typeof(BarrierLossGrantsBarrierSurge), "duration", step * 3);
                owner.AddOuterModifier(surge);
            }
            owner.effectController.OnEffectReceived += unused => received++;
            opponent.OnEffectApplied += (target, type, factory) => applied++;
            observer = new Observer { action = AfterActions };
        }
        GameObject NewRoot(string name) { var root = new GameObject(name); root.SetActive(false); roots.Add(root); return root; }
        T Definition<T>() where T : ScriptableObject { var obj = ScriptableObject.CreateInstance<T>(); definitions.Add(obj); return obj; }
        Unit Actor(string name, float physical, float fire, float armor)
        {
            var root = NewRoot(name);
            var health = root.AddComponent<Health>(); var barrier = root.AddComponent<Barrier>(); var mystic = root.AddComponent<MysticHealth>();
            var effects = root.AddComponent<EffectController>(); var attacker = root.AddComponent<Attacker>(); var attributes = root.AddComponent<Attributes>();
            var unit = root.AddComponent<Unit>();
            unit.health = health; unit.barrier = barrier; unit.mysticHealth = mystic; unit.effectController = effects; unit.attacker = attacker; unit.attributes = attributes;
            var innate = Definition<BaseInnateModifiers>();
            foreach (var pair in new[] { new ModifierContainer(ModifierType.Added, StatType.MaximumHealth, 500),
                new ModifierContainer(ModifierType.Added, StatType.PhysicalDamage, physical), new ModifierContainer(ModifierType.Added, StatType.FireDamage, fire),
                new ModifierContainer(ModifierType.Added, StatType.Armor, armor), new ModifierContainer(ModifierType.Added, StatType.Accuracy, 100),
                new ModifierContainer(ModifierType.Added, StatType.BleedChance, -1), new ModifierContainer(ModifierType.Added, StatType.IgniteChance, -1) }) innate.AddModifier(pair);
            Set(unit, typeof(Unit), "baseInnateModifiers", innate); Set(unit, typeof(Unit), "innateModifiers", Definition<BaseInnateModifiers>());
            Set(unit, typeof(Unit), "_battleTickSystem", driver);
            if (editMode) typeof(Unit).GetMethod("Awake", Private).Invoke(unit, null);
            takeOwnership?.Invoke(effects);
            root.SetActive(true);
            if (editMode) typeof(Unit).GetMethod("RegisterToBattleTickSystem", Private).Invoke(unit, null);
            return unit;
        }
        void Add(Unit unit, StatType stat, float value)
        {
            var modifier = Definition<BaseModifier>(); modifier.modifierContainer = new ModifierContainer(ModifierType.Added, stat, value); unit.AddOuterModifier(modifier);
        }
        public void Prepare()
        {
            // Native Start/Awake have already run; establish a reset boundary.
            owner.RequestModRecalculation(); opponent.RequestModRecalculation();
            owner.CombatTick(0, CombatTickPhase.Mods); opponent.CombatTick(0, CombatTickPhase.Mods);
            owner.ResetCombatState(); opponent.ResetCombatState();
            driver.Register(observer); UnityEngine.Random.InitState(47701); driver.Resume();
        }
        public void Step() { if (!editMode) throw new InvalidOperationException("Play Mode is driven by native Update."); runTick(trace.step); }
        void Attack(Unit from) { from.attacker.ModifyAttackProgress(1); from.attacker.CombatTick(0); }
        void AfterActions()
        {
            if (Complete) return;
            tick++;
            if (trace.name == "pain-scar-consume-expire") {
                if (tick == 1) Attack(opponent);
                if (tick == 2) Attack(owner);
            } else if (trace.name == "repeat-effect-no-recursion") {
                if (tick == 1) owner.effectController.AddEffect(() => new Scar(trace.step * 3, 50), opponent);
                if (tick == 7) { owner.RemoveOuterModifier(repeatModifier); owner.effectController.ClearAllEffects(); }
                if (tick == 8) owner.effectController.AddEffect(() => new Scar(trace.step * 2, 50), opponent);
            } else if (trace.name == "barrier-loss-and-hit-binding") {
                if (tick == 1) Attack(opponent);
                if (tick == 7) { owner.RemoveOuterModifier(progressModifier); owner.ResetCombatState(); }
                if (tick == 8) Attack(opponent);
            } else if (trace.name == "ignite-pool-and-expiry" && tick == 1) {
                var damage = new DamageInfo(opponent, new BaseUnitModifiers(opponent.BaseUnitModifiers));
                owner.effectController.AddEffect(() => new Ignite(damage, owner, 100), opponent);
                owner.effectController.AddEffect(() => new Ignite(damage, owner, 100), opponent);
            }
            trace.frames.Add(new Frame { tick = tick, health = owner.health.CurrentHealth, opponentHealth = opponent.health.CurrentHealth,
                pain = owner.effectController.GetAllEffectsOfType<Pain>().Sum(e => ((Pain)e.Effect).Amount),
                painEffects = owner.effectController.GetAllEffectsOfType<Pain>().Count,
                scarEffects = owner.effectController.GetAllEffectsOfType<Scar>().Count, igniteEffects = owner.effectController.GetAllEffectsOfType<Ignite>().Count,
                surgeEffects = owner.effectController.GetAllEffectsOfType<BarrierSurge>().Count,
                armor = owner.BaseUnitModifiers.GetStatValue(StatType.Armor), healing = owner.BaseUnitModifiers.GetStatValue(StatType.HealingReceived),
                fire = owner.BaseUnitModifiers.GetStatValue(StatType.FireDamage), regeneration = owner.BaseUnitModifiers.GetStatValue(StatType.BarrierRegenerationSpeed),
                barrier = owner.barrier.BarrierCount, progress = owner.attacker.AttackProgress, received = received, applied = applied });
            if (tick >= Limit) { Complete = true; driver.Pause(); }
        }
        public void Dispose()
        {
            driver.Unregister(observer);
            foreach (var unit in new[] { owner, opponent }) {
                typeof(Unit).GetMethod("UnregisterFromBattleTickSystem", Private).Invoke(unit, null);
                unit.effectController.ClearAllEffects(); typeof(Unit).GetMethod("UnbindModifierRuntimes", Private).Invoke(unit, null);
            }
            foreach (var root in roots) { if (editMode) UnityEngine.Object.DestroyImmediate(root); else UnityEngine.Object.Destroy(root); }
            foreach (var obj in definitions) { if (editMode) UnityEngine.Object.DestroyImmediate(obj); else UnityEngine.Object.Destroy(obj); }
        }
    }
}

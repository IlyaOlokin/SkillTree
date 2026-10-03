// Minimal host for the linked production classes, not a replacement game implementation.
using System;
using System.Collections.Generic;

namespace UnityEngine
{
    public class Object
    {
        public int DestroyCalls;
        public static void Destroy(Object value) { if (value != null) value.DestroyCalls++; }
    }
    public class MonoBehaviour : Object { public bool isActiveAndEnabled = true; }
    public class ScriptableObject : Object
    {
        public static T CreateInstance<T>() where T : ScriptableObject => (T)Activator.CreateInstance(typeof(T));
    }
    public sealed class SerializeField : Attribute { }
    public static class Mathf
    {
        public const float Epsilon = float.Epsilon;
        public static float Max(float a, float b) => Math.Max(a, b);
        public static int Max(int a, int b) => Math.Max(a, b);
        public static float Min(float a, float b) => Math.Min(a, b);
        public static int Min(int a, int b) => Math.Min(a, b);
        public static float Clamp(float x, float a, float b) => Math.Clamp(x, a, b);
        public static int Clamp(int x, int a, int b) => Math.Clamp(x, a, b);
        public static float Clamp01(float x) => Clamp(x, 0, 1);
        public static float Abs(float x) => Math.Abs(x);
        public static int FloorToInt(float x) => (int)Math.Floor(x);
        public static int CeilToInt(float x) => (int)Math.Ceiling(x);
        public static bool Approximately(float a, float b) => Abs(b - a) < Max(0.000001f * Max(Abs(a), Abs(b)), Epsilon * 8);
    }
    public static class Random { public static float Range(float a, float b) => b; }
}
namespace LocalizationSupport
{
    public static class GameLocalization
    {
        public static string FormatContent(string key, string fallback, params object[] args) => fallback;
        public static string GetContent(string key, string fallback) => fallback;
        public static string HumanizeIdentifier(string value) => value;
    }
}
namespace TooltipSystem
{
    public class TooltipDescriptionData
    {
        public List<string> Descriptions = new List<string>();
        public IReadOnlyList<string> GetDescriptions(object[] args) => Descriptions;
    }
    public class TooltipTermDatabase
    {
        public static TooltipTermDatabase ActiveDatabase => null;
        public void TryGetDescription(string key, out TooltipDescriptionData data) { data = null; }
    }
}
namespace SkillTree
{
    public class Modifier : UnityEngine.ScriptableObject { }
    public class BaseModifier : Modifier { public ModifierContainer modifierContainer; }
    public class BaseInnateModifiers : Modifier { }
    public enum ModifierType { Added, More }
    public class ModifierContainer
    {
        public ModifierType modifierType;
        public Battle.StatType statType;
        public float value;
        public ModifierContainer(ModifierType type, Battle.StatType stat, float amount)
        { modifierType = type; statType = stat; value = amount; }
        public string GetDescription() => value.ToString();
    }
    public class Node
    {
        public bool IsActive;
        public event Action<Node> OnAllocatedChanged;
        public event Action<Node> OnActiveChanged;
        public void SetActive(bool active)
        { IsActive = active; OnActiveChanged?.Invoke(this); OnAllocatedChanged?.Invoke(this); }
    }
}
namespace Battle
{
    public enum StatType
    {
        AttackSpeed, BarrierCount, BarrierCapacity, BarrierRegenerationSpeed, BarrierDamageTypeMask,
        MysticCleansePerSecond, BleedMitigation, BleedPower, BleedChance, AilmentGuard, Accuracy,
        Regeneration, HealthRegeneration, LifeSteal, Evasion, Damage
    }
    public enum EffectVisualType { None, Bleed, LightAbsorptionDebuff, DarknessAbsorptionDebuff }
    [Flags] public enum DamageType { Physical = 1, Light = 2, Darkness = 4 }
    public interface IUnitComponent { void Init(Unit owner); }
    public interface ITarget { Unit UnitObject { get; } }
    public class BaseUnitModifiers
    {
        public readonly Dictionary<StatType, float> Stats = new Dictionary<StatType, float>();
        public float GetStatValue(StatType type) => Stats.TryGetValue(type, out float value) ? value : 0;
        public void CopyFrom(BaseUnitModifiers other) { }
    }
    public class DamageInstance
    {
        public Dictionary<DamageType, float> Damage = new Dictionary<DamageType, float>
        { [DamageType.Physical] = 0, [DamageType.Light] = 0, [DamageType.Darkness] = 0 };
    }
    public class Payload
    {
        public bool IsSuppressed<T>() => false;
        public bool IsRedirectedToOwner<T>() => false;
        public bool IsGuaranteed<T>() => true;
    }
    public class DamageInfo
    {
        public BaseUnitModifiers BaseUnitModifiers;
        public DamageInstance DamageInstance = new DamageInstance();
        public Payload AttackEffectPayload = new Payload();
        public DamageInfo(Unit owner, BaseUnitModifiers stats) { BaseUnitModifiers = stats; }
        public void Reset(Unit owner, BaseUnitModifiers stats) { BaseUnitModifiers = stats; }
    }
    public class Health { public float MaxHealth = 100; }
    public class Unit : UnityEngine.MonoBehaviour, ITarget
    {
        public Unit UnitObject => this;
        public readonly BaseUnitModifiers BaseUnitModifiers = new BaseUnitModifiers();
        public readonly Health health = new Health();
        public readonly EffectController effectController = new EffectController();
        public readonly List<SkillTree.Modifier> Outer = new List<SkillTree.Modifier>();
        public double DotDamage;
        public int Attacks;
        public Action<Unit> AttackCallback;
        public Action DotCallback;
        public event Action OnStatsRecalculated;
        public Unit() { effectController.Init(this); }
        public void RaiseStats() => OnStatsRecalculated?.Invoke();
        public void OnAttackStarted(Unit target) { }
        public void OnAttackFinished(Unit target) { Attacks++; AttackCallback?.Invoke(target); }
        public void BleedApplied(Unit target) { }
        public void ReceiveDoT(DamageInstance damage) { DotDamage += damage.Damage[DamageType.Physical]; DotCallback?.Invoke(); }
        public void AddOuterModifier(SkillTree.Modifier modifier) => Outer.Add(modifier);
        public void RemoveOuterModifier(SkillTree.Modifier modifier) => Outer.Remove(modifier);
    }
    public static class AttackProcessor { public static void HandleAttack(Unit attacker, DamageInfo damage, Unit defender) { } }
    public class Freeze : BaseEffect { public override bool IsStackable { get; set; } = true; }
    public static class AilmentAbsorption { public static bool TryAbsorbIncomingAilment(Unit unit) => false; }
    public class GeneratedEnemyDefinition { }
    public enum EnemyRarity { Normal }
    public class EnemyAffix { }
}

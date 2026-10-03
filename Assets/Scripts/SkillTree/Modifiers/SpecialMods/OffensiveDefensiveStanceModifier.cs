using Battle;
using LocalizationSupport;
using UnityEngine;

namespace SkillTree
{
    [CreateAssetMenu(menuName = "Modifiers/Special/Offensive Defensive Stance", fileName = "OffensiveDefensiveStance")]
    public class OffensiveDefensiveStanceModifier : Modifier
    {
        [SerializeField, Min(1)] private int attacksToDefence = 3;
        [SerializeField, Min(1)] private int hitsToOffence = 3;
        [SerializeField, Min(0f)] private float increasedDamage = 0.15f;
        [SerializeField, Min(0f)] private float increasedArmorAndEvasion = 0.20f;

        public int AttacksToDefence => Mathf.Max(1, attacksToDefence);
        public int HitsToOffence => Mathf.Max(1, hitsToOffence);
        public const string Description = "Start combat in Offence: [[0]]% increased Damage. After [[1]] completed attacks, including misses, switch to Defence: [[2]]% increased Armor and Evasion instead. After [[3]] incoming attacks that are not evaded, return to Offence. Blocked and fully absorbed attacks count.";

        public OffensiveDefensiveStanceEffect FindEffect(Unit unit)
        {
            if (unit?.effectController == null) return null;
            foreach (var active in unit.effectController.Effects)
                if (active.Effect is OffensiveDefensiveStanceEffect effect && ReferenceEquals(effect.Source, this))
                    return effect;
            return null;
        }

        public override bool IsInPriority(ModifierPriority priority) => priority == ModifierPriority.Special;
        public override void ApplyEffect(Unit unit) => ApplyEffect(unit, ModifierPowerContext.None);
        public override void ApplyEffect(Unit unit, ModifierPowerContext powerContext)
        {
            if (FindEffect(unit)?.IsDefensive == true)
            {
                unit.BaseUnitModifiers.ChangeModifierValue(new ModifierContainer(ModifierType.Increased, StatType.Armor, powerContext.Scale(increasedArmorAndEvasion)));
                unit.BaseUnitModifiers.ChangeModifierValue(new ModifierContainer(ModifierType.Increased, StatType.Evasion, powerContext.Scale(increasedArmorAndEvasion)));
            }
            else
                unit.BaseUnitModifiers.ChangeModifierValue(new ModifierContainer(ModifierType.Increased, StatType.Damage, powerContext.Scale(increasedDamage)));
        }

        public override IModifierRuntimeBinding CreateRuntimeBinding(Unit unit) => CreateRuntimeBinding(unit, ModifierPowerContext.None);
        public override IModifierRuntimeBinding CreateRuntimeBinding(Unit unit, ModifierPowerContext powerContext)
        {
            if (unit?.effectController == null) return null;
            void EnsureEffect()
            {
                var effect = FindEffect(unit);
                if (effect == null)
                {
                    effect = new OffensiveDefensiveStanceEffect(this);
                    unit.effectController.AddRepeatedEffect(() => effect);
                }
                float damage = 0f, defence = 0f;
                foreach (var collected in unit.GetAllModifiers())
                {
                    if (!ReferenceEquals(collected.Modifier, this)) continue;
                    damage += collected.PowerContext.Scale(increasedDamage);
                    defence += collected.PowerContext.Scale(increasedArmorAndEvasion);
                }
                effect.SetBonuses(damage, defence);
            }
            void Reset()
            {
                EnsureEffect();
                unit.RequestModRecalculation();
            }
            return new DelegateModifierRuntimeBinding(
                () => { EnsureEffect(); unit.OnCombatStateReset += Reset; },
                () => unit.OnCombatStateReset -= Reset);
        }

        public override string GetDescription() => GetDescription(ModifierPowerContext.None);
        public override string GetDescription(ModifierPowerContext powerContext) => GameLocalization.FormatModifier(
            "modifier.offensiveDefensiveStance.description", Description,
            powerContext.HighlightValue(powerContext.Scale(increasedDamage) * 100f), AttacksToDefence,
            powerContext.HighlightValue(powerContext.Scale(increasedArmorAndEvasion) * 100f), HitsToOffence);
    }
}

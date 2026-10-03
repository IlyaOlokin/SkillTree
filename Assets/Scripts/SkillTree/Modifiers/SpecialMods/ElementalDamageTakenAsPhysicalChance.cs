using Battle;
using LocalizationSupport;
using UnityEngine;

namespace SkillTree
{
    [CreateAssetMenu(menuName = "Modifiers/Special/Elemental Damage Taken As Physical Chance", fileName = "New ElementalDamageTakenAsPhysicalChance")]
    public class ElementalDamageTakenAsPhysicalChance : Modifier
    {
        [SerializeField, Range(0f, 1f)] private float chance = 0.1f;

        public override bool IsInPriority(ModifierPriority priority)
        {
            return priority == ModifierPriority.IncomingPreMitigation;
        }

        public override void ApplyEffect(DamageInfo damageInfo)
        {
            ApplyEffect(damageInfo, ModifierPowerContext.None);
        }

        public override void ApplyEffect(DamageInfo damageInfo, ModifierPowerContext powerContext)
        {
            if (damageInfo?.DamageInstance == null)
                return;

            var damage = damageInfo.DamageInstance.Damage;
            float elementalDamage = damage[DamageType.Fire] + damage[DamageType.Cold] + damage[DamageType.Lightning];
            float scaledChance = Mathf.Clamp01(powerContext.Scale(chance));
            if (elementalDamage <= 0f || scaledChance <= 0f)
                return;

            // One roll converts all three elements. At 100%, conversion is guaranteed.
            // Multiple copies roll independently until one succeeds; later copies have no elements left to convert.
            if (scaledChance < 1f && Random.Range(0f, 1f) >= scaledChance)
                return;

            damage[DamageType.Physical] += elementalDamage;
            damage[DamageType.Fire] = 0f;
            damage[DamageType.Cold] = 0f;
            damage[DamageType.Lightning] = 0f;
        }

        public override string GetDescription()
        {
            return GetDescription(ModifierPowerContext.None);
        }

        public override string GetDescription(ModifierPowerContext powerContext)
        {
            return GameLocalization.FormatModifier(
                "modifier.elementalDamageTakenAsPhysicalChance.description",
                "[[0]]% chance to take all Fire, Cold and Lightning Damage from an incoming attack as Physical Damage instead, before mitigation.",
                powerContext.HighlightValue(Mathf.Clamp01(powerContext.Scale(chance)) * 100f));
        }
    }
}

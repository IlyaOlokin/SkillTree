using UnityEngine;

namespace Battle
{
    public static class Parry
    {
        public const float AttackProgressBonus = 0.3f;

        public static bool Apply(AttackContext context, float? blockedPower = null)
        {
            Unit defender = context?.Defender?.UnitObject;
            if (context == null || !context.IsBlocked || context.IsParried ||
                defender?.BaseUnitModifiers == null)
            {
                return false;
            }

            // Conditional chance per successful block: 50% block * 10% parry = 5% overall.
            float chance = Mathf.Clamp01(defender.BaseUnitModifiers.GetStatValue(StatType.ParryChance));
            if (chance <= 0f || (chance < 1f && Random.Range(0f, 1f) >= chance))
            {
                return false;
            }

            context.IsParried = true;
            // Apply one more copy of the original flat BlockPower to the remaining
            // damage: together with block, this absorbs 2 * BlockPower in total.
            Block.ApplyBlockPower(context.DamageInfo.DamageInstance,
                blockedPower ?? Mathf.Max(0f, defender.BaseUnitModifiers.GetStatValue(StatType.BlockPower)));
            float power = Mathf.Max(0f, 1f + defender.BaseUnitModifiers.GetStatValue(StatType.ParryPower));
            float progressBonus = AttackProgressBonus * power;
            // Progress can trigger extra attacks. Wait until the incoming attack finishes
            // so a counterattack cannot overwrite an in-use damage snapshot.
            AttackProcessor.RunAfterCurrentAttack(() =>
            {
                if (defender != null && defender.isActiveAndEnabled)
                {
                    defender.attacker?.ModifyAttackProgress(progressBonus);
                }
            });
            defender.OnHitParried(context);
            context.Attacker?.OnAttackWasParried(context);
            return true;
        }
    }
}

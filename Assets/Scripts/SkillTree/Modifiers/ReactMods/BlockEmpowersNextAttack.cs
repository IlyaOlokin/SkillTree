using Battle;
using LocalizationSupport;
using UnityEngine;

namespace SkillTree
{
    [CreateAssetMenu(menuName = "Modifiers/Reactive/Block Empowers Next Attack", fileName = "New BlockEmpowersNextAttack")]
    public class BlockEmpowersNextAttack : Modifier
    {
        [SerializeField, Min(0f)] private float morePhysicalDamage = 0.4f;

        public override IModifierRuntimeBinding CreateRuntimeBinding(Unit unit)
        {
            return CreateRuntimeBinding(unit, ModifierPowerContext.None);
        }

        public override IModifierRuntimeBinding CreateRuntimeBinding(Unit unit, ModifierPowerContext powerContext)
        {
            void HandleBlock()
            {
                Modifier runtimeModifier = CreateRuntimeEffectModifier(powerContext, out bool ownsModifier);
                unit.effectController.AddEffect(new NextAttackModifierEffect(unit, runtimeModifier, ownsModifier));
            }

            return new DelegateModifierRuntimeBinding(
                () => unit.OnBlock += HandleBlock,
                () => unit.OnBlock -= HandleBlock);
        }

        public override void ApplyEffect(DamageInfo damageInfo)
        {
            damageInfo.BaseUnitModifiers.ChangeModifierValue(
                new ModifierContainer(ModifierType.More, StatType.PhysicalDamage, morePhysicalDamage));
            damageInfo.AttackEffectPayload.Guarantee<Bleed>();
        }

        public override void ApplyEffect(DamageInfo damageInfo, ModifierPowerContext powerContext)
        {
            damageInfo.BaseUnitModifiers.ChangeModifierValue(
                new ModifierContainer(ModifierType.More, StatType.PhysicalDamage, powerContext.Scale(morePhysicalDamage)));
            damageInfo.AttackEffectPayload.Guarantee<Bleed>();
        }

        public override string GetDescription(ModifierPowerContext powerContext)
        {
            return GameLocalization.FormatModifier(
                "modifier.blockEmpowersNextAttack.description",
                "After {block|Block}: next attack gains +[[0]]% more Physical Damage and always applies {bleed|Bleed}",
                powerContext.Scale(morePhysicalDamage) * 100f);
        }

        private Modifier CreateRuntimeEffectModifier(ModifierPowerContext powerContext, out bool ownsModifier)
        {
            if (Mathf.Approximately(powerContext.Multiplier, 1f))
            {
                ownsModifier = false;
                return this;
            }

            BlockEmpowersNextAttack runtimeModifier = Instantiate(this);
            runtimeModifier.name = name;
            runtimeModifier.morePhysicalDamage = powerContext.Scale(morePhysicalDamage);
            ownsModifier = true;
            return runtimeModifier;
        }
    }
}

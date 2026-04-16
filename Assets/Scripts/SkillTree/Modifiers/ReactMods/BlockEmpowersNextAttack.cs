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
            void HandleBlock()
            {
                unit.effectController.AddEffect(new NextAttackModifierEffect(unit, this));
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

        public override string GetDescription()
        {
            return GameLocalization.FormatModifier(
                "modifier.blockEmpowersNextAttack.description",
                "After {block|Block}: next attack gains +[[0]]% more Physical Damage and always applies {bleed|Bleed}",
                morePhysicalDamage * 100f);
        }
    }
}

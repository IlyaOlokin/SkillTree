using Battle;
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
            return $"After {{block|Block}}: next attack gains +{morePhysicalDamage * 100f:0.#}% more Physical Damage and always applies {{bleed|Bleed}}";
        }
    }
}

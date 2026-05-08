using Battle;
using LocalizationSupport;
using UnityEngine;

namespace SkillTree
{
    [CreateAssetMenu(menuName = "Modifiers/Reactive/Barrier Restoration Grants Power", fileName = "New BarrierRestorationGrantsPower")]
    public class BarrierRestorationGrantsBarrierSurge : Modifier
    {
        [SerializeField, Min(0f)] private float duration = 6f;
        [SerializeField] private float increasedAilmentPower = 0.3f;
        [SerializeField] private float increasedMysticDamage = 0.2f;

        public override IModifierRuntimeBinding CreateRuntimeBinding(Unit unit)
        {
            return CreateRuntimeBinding(unit, ModifierPowerContext.None);
        }

        public override IModifierRuntimeBinding CreateRuntimeBinding(Unit unit, ModifierPowerContext powerContext)
        {
            if (unit.barrier == null || unit.effectController == null)
            {
                return null;
            }

            float scaledAilmentPower = powerContext.Scale(increasedAilmentPower);
            float scaledMysticDamage = powerContext.Scale(increasedMysticDamage);

            void HandleBarrierRestored()
            {
                unit.effectController.AddEffect(new BarrierSurge(
                    duration,
                    scaledAilmentPower,
                    scaledMysticDamage));
            }

            return new DelegateModifierRuntimeBinding(
                () => unit.barrier.OnBarrierRestored += HandleBarrierRestored,
                () => unit.barrier.OnBarrierRestored -= HandleBarrierRestored);
        }

        public override string GetDescription(ModifierPowerContext powerContext)
        {
            return GameLocalization.FormatModifier(
                "modifier.barrierRestorationGrantsBarrierSurge.description",
                "Each restored {barrier|Barrier} grants [[0]]% increased Ailment Power and [[1]]% increased Mystic Damage for [[2]] seconds. Each stack has its own duration.",
                powerContext.Scale(increasedAilmentPower) * 100f,
                powerContext.Scale(increasedMysticDamage) * 100f,
                duration);
        }
    }
}

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
            if (unit.barrier == null || unit.effectController == null)
            {
                return null;
            }

            void HandleBarrierRestored()
            {
                unit.effectController.AddEffect(new BarrierSurge(
                    duration,
                    increasedAilmentPower,
                    increasedMysticDamage));
            }

            return new DelegateModifierRuntimeBinding(
                () => unit.barrier.OnBarrierRestored += HandleBarrierRestored,
                () => unit.barrier.OnBarrierRestored -= HandleBarrierRestored);
        }

        public override string GetDescription()
        {
            return GameLocalization.FormatModifier(
                "modifier.barrierRestorationGrantsBarrierSurge.description",
                "Each restored {barrier|Barrier} grants [[0]]% increased Ailment Power and [[1]]% increased Mystic Damage for [[2]] seconds. Each stack has its own duration.",
                increasedAilmentPower * 100f,
                increasedMysticDamage * 100f,
                duration);
        }
    }
}

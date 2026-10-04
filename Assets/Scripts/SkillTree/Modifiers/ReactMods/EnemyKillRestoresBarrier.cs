using Battle;
using LocalizationSupport;
using UnityEngine;

namespace SkillTree
{
    [CreateAssetMenu(menuName = "Modifiers/Reactive/Enemy Kill Restores Barrier", fileName = "New EnemyKillRestoresBarrier")]
    public class EnemyKillRestoresBarrier : Modifier
    {
        public override IModifierRuntimeBinding CreateRuntimeBinding(Unit unit)
        {
            if (unit?.barrier == null)
                return null;

            void HandleEnemyKilled(Unit enemy)
            {
                if (unit.isActiveAndEnabled)
                    unit.barrier.Restore(1);
            }

            return new DelegateModifierRuntimeBinding(
                () => unit.OnEnemyKilled += HandleEnemyKilled,
                () => unit.OnEnemyKilled -= HandleEnemyKilled);
        }

        public override string GetDescription()
        {
            return GameLocalization.GetModifier(
                "modifier.enemyKillRestoresBarrier.description",
                "Killing an enemy restores 1 {barrier|Barrier}");
        }
    }
}

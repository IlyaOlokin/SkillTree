using Battle;
using LocalizationSupport;
using UnityEngine;

namespace SkillTree
{
    [CreateAssetMenu(menuName = "Modifiers/Reactive/Enemy Kill Grants Embers", fileName = "New EnemyKillGrantsEmbers")]
    public sealed class EnemyKillGrantsEmbers : Modifier
    {
        [SerializeField, Min(0)] private int stacks = 1;

        public override IModifierRuntimeBinding CreateRuntimeBinding(Unit unit)
        {
            return CreateRuntimeBinding(unit, ModifierPowerContext.None);
        }

        public override IModifierRuntimeBinding CreateRuntimeBinding(Unit unit, ModifierPowerContext powerContext)
        {
            if (unit == null || unit.effectController == null) return null;

            EffectController controller = unit.effectController;
            int count = GetStackCount(powerContext);

            void HandleEnemyKilled(Unit enemy)
            {
                if (!unit.isActiveAndEnabled || count <= 0) return;

                // Direct self-buffs do not trigger generic received-effect repeats.
                controller.AddEffect(new Embers(count));
            }

            return new DelegateModifierRuntimeBinding(
                () => unit.OnEnemyKilled += HandleEnemyKilled,
                () => unit.OnEnemyKilled -= HandleEnemyKilled);
        }

        public override string GetDescription() => GetDescription(ModifierPowerContext.None);

        public override string GetDescription(ModifierPowerContext powerContext)
        {
            return GameLocalization.FormatModifier(
                "modifier.enemyKillGrantsEmbers.description",
                "On Kill: gain [[0]] {embers|Embers} charges.",
                powerContext.HighlightValue(GetStackCount(powerContext)));
        }

        private int GetStackCount(ModifierPowerContext powerContext)
        {
            return Mathf.Max(0, Mathf.FloorToInt(powerContext.Scale(Mathf.Max(0, stacks))));
        }
    }
}

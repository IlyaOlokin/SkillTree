using Battle;
using LocalizationSupport;
using UnityEngine;

namespace SkillTree
{
    [CreateAssetMenu(menuName = "Modifiers/Reactive/Critical Momentum On Crit", fileName = "New CriticalMomentumOnCrit")]
    public class CriticalMomentumOnCrit : Modifier
    {
        [SerializeField, Range(0f, 1f)] private float critChanceLossPerStack = 0.05f;
        [SerializeField, Range(0f, 5f)] private float critDamageBonusPerStack = 0.25f;

        public override IModifierRuntimeBinding CreateRuntimeBinding(Unit unit)
        {
            EffectController effectController = unit.effectController;
            if (effectController == null)
            {
                return null;
            }

            void HandleCrit(ITarget _)
            {
                effectController.AddEffect(new CriticalMomentum(
                    1,
                    critChanceLossPerStack,
                    critDamageBonusPerStack));
            }

            void HandleNonCrit(ITarget _)
            {
                effectController.RemoveEffectsOfType<CriticalMomentum>();
            }

            return new DelegateModifierRuntimeBinding(
                () =>
                {
                    unit.OnCrit += HandleCrit;
                    unit.OnNonCrit += HandleNonCrit;
                },
                () =>
                {
                    unit.OnCrit -= HandleCrit;
                    unit.OnNonCrit -= HandleNonCrit;
                });
        }

        public override string GetDescription()
        {
            return GameLocalization.FormatModifier(
                "modifier.criticalMomentumOnCrit.description",
                "After each Crit: gain {criticalMomentum|Critical Momentum}. Each stack grants [[1]]% more Crit Damage Bonus and [[0]]% less Crit Chance. Lose all stacks after a non-Crit hit.",
                critChanceLossPerStack * 100f,
                critDamageBonusPerStack * 100f);
        }
    }
}

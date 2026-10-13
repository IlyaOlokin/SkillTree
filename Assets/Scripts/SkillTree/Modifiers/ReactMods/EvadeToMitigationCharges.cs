using Battle;
using LocalizationSupport;
using UnityEngine;

namespace SkillTree
{
    [CreateAssetMenu(menuName = "Modifiers/Reactive/Evade Grants Fortification", fileName = "New Evade Grants Fortification")]
    public class EvadeToMitigationCharges : Modifier
    {
        [SerializeField, Min(0)] private int stacksPerEvade = 1;
        
        public override IModifierRuntimeBinding CreateRuntimeBinding(Unit unit)
        {
            if (unit == null || unit.effectController == null) return null;
            EffectController controller = unit.effectController;
            void HandleEvade()
            {
                int count = 0;
                foreach (var active in controller.Effects)
                    if (active.Effect is FortificationEffect) count++;
                int stacksToGrant = Mathf.Min(Mathf.Max(0, stacksPerEvade),
                    Mathf.Max(0, unit.Fortification.Maximum - count));
                for (int i = 0; i < stacksToGrant; i++)
                    controller.AddEffect(new FortificationEffect());
            }

            return new DelegateModifierRuntimeBinding(
                () => unit.OnEvade += HandleEvade,
                () => unit.OnEvade -= HandleEvade);
        }

        public override string GetDescription()
        {
            return GameLocalization.FormatModifier(
                "modifier.evadeToMitigationCharges.description",
                "On Evade: gain [[0]] stacks of {fortification|Fortification}.",
                Mathf.Max(0, stacksPerEvade));
        }
    }
}

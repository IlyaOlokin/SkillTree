using Battle;
using LocalizationSupport;
using UnityEngine;

namespace SkillTree
{
    [CreateAssetMenu(menuName = "Modifiers/Reactive/Crit Grants Sparks", fileName = "New CritGrantsSparks")]
    public sealed class CritGrantsSparks : Modifier
    {
        [SerializeField, Range(0f, 1f)] private float chance = 0.4f;
        [SerializeField, Min(0)] private int stacks = 2;

        public override IModifierRuntimeBinding CreateRuntimeBinding(Unit unit)
            => CreateRuntimeBinding(unit, ModifierPowerContext.None);

        public override IModifierRuntimeBinding CreateRuntimeBinding(Unit unit, ModifierPowerContext powerContext)
        {
            EffectController controller = unit?.effectController;
            float triggerChance = Mathf.Clamp01(chance);
            int count = GetStackCount(powerContext);
            if (controller == null || triggerChance <= 0f || count <= 0) return null;

            void HandleCrit(ITarget _)
            {
                if (triggerChance < 1f && Random.Range(0f, 1f) >= triggerChance) return;

                // Direct self-buff, without generic received-effect repeats or attribution.
                controller.AddEffect(new Sparks(count));
            }

            return new DelegateModifierRuntimeBinding(
                () => unit.OnCrit += HandleCrit,
                () => unit.OnCrit -= HandleCrit);
        }

        public override string GetDescription() => GetDescription(ModifierPowerContext.None);

        public override string GetDescription(ModifierPowerContext powerContext)
        {
            return GameLocalization.FormatModifier(
                "modifier.critGrantsSparks.description",
                "On Crit: [[0]]% chance to gain [[1]] {sparks|Sparks} charges.",
                Mathf.Clamp01(chance) * 100f,
                powerContext.HighlightValue(GetStackCount(powerContext)));
        }

        private int GetStackCount(ModifierPowerContext powerContext)
            => Mathf.Max(0, Mathf.FloorToInt(powerContext.Scale(Mathf.Max(0, stacks))));
    }
}

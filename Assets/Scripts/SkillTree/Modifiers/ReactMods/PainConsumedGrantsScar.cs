using Battle;
using LocalizationSupport;
using UnityEngine;

namespace SkillTree
{
    [CreateAssetMenu(menuName = "Modifiers/Reactive/Pain Consumed Grants Scar", fileName = "New PainConsumedGrantsScar")]
    public class PainConsumedGrantsScar : Modifier
    {
        [SerializeField, Range(0f, 1f)] private float painConsumedAsScar = 0.2f;
        [SerializeField, Min(0f)] private float duration = 6f;

        public override IModifierRuntimeBinding CreateRuntimeBinding(Unit unit)
        {
            return CreateRuntimeBinding(unit, ModifierPowerContext.None);
        }

        public override IModifierRuntimeBinding CreateRuntimeBinding(Unit unit, ModifierPowerContext powerContext)
        {
            EffectController effectController = unit.effectController;
            if (effectController == null)
            {
                return null;
            }

            float scaledPainConsumedAsScar = powerContext.Scale(painConsumedAsScar);

            void HandlePainConsumed(float consumedPain)
            {
                float scarAmount = Mathf.Max(0f, consumedPain) * scaledPainConsumedAsScar;
                if (scarAmount <= 0f)
                {
                    return;
                }

                effectController.AddEffect(() => new Scar(duration, scarAmount));
            }

            return new DelegateModifierRuntimeBinding(
                () => unit.OnPainConsumed += HandlePainConsumed,
                () => unit.OnPainConsumed -= HandlePainConsumed);
        }

        public override string GetDescription(ModifierPowerContext powerContext)
        {
            return GameLocalization.FormatModifier(
                "modifier.painConsumedGrantsScar.description",
                "When {pain|Pain} is consumed, gain {scar|Scar} equal to [[0]]% of consumed {pain|Pain} for [[1]] seconds. {scar|Scar} grants increased {armor|Armor} and {ailmentGuard|Ailment Guard} equal to its percentage of your Maximum Health, but reduces Healing received by the same amount.",
                powerContext.HighlightValue(powerContext.Scale(painConsumedAsScar) * 100f),
                duration);
        }
    }
}

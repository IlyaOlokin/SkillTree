using Battle;
using LocalizationSupport;
using UnityEngine;

namespace SkillTree
{
    [CreateAssetMenu(menuName = "Modifiers/Reactive/Combat Start Grants Elemental Charges", fileName = "New CombatStartGrantsElementalCharges")]
    public sealed class CombatStartGrantsElementalCharges : Modifier
    {
        public enum ChargeType
        {
            Embers = 0,
            Rime = 1,
            Sparks = 2
        }

        [SerializeField] private ChargeType chargeType;
        [SerializeField, Min(0)] private int stacks = 1;

        public override IModifierRuntimeBinding CreateRuntimeBinding(Unit unit)
        {
            return CreateRuntimeBinding(unit, ModifierPowerContext.None);
        }

        public override IModifierRuntimeBinding CreateRuntimeBinding(Unit unit, ModifierPowerContext powerContext)
        {
            if (unit == null || unit.effectController == null) return null;

            EffectController controller = unit.effectController;
            ChargeType type = chargeType;
            int count = GetStackCount(powerContext);

            void GrantCharges()
            {
                if (count <= 0) return;

                // Direct self-buffs do not trigger generic received-effect repeats.
                BaseEffect effect = type switch
                {
                    ChargeType.Embers => new Embers(count),
                    ChargeType.Rime => new Rime(count),
                    ChargeType.Sparks => new Sparks(count),
                    _ => null
                };
                controller.AddEffect(effect);
            }

            // Binding also runs on stat rebuilds: only a combat reset grants charges.
            return new DelegateModifierRuntimeBinding(
                () => unit.OnCombatStateReset += GrantCharges,
                () => unit.OnCombatStateReset -= GrantCharges);
        }

        public override string GetDescription() => GetDescription(ModifierPowerContext.None);

        public override string GetDescription(ModifierPowerContext powerContext)
        {
            string key;
            string text;
            switch (chargeType)
            {
                case ChargeType.Rime:
                    key = "modifier.combatStartGrantsRime.description";
                    text = "At the start of combat, gain [[0]] {rime|Rime} charges.";
                    break;
                case ChargeType.Sparks:
                    key = "modifier.combatStartGrantsSparks.description";
                    text = "At the start of combat, gain [[0]] {sparks|Sparks} charges.";
                    break;
                default:
                    key = "modifier.combatStartGrantsEmbers.description";
                    text = "At the start of combat, gain [[0]] {embers|Embers} charges.";
                    break;
            }

            return GameLocalization.FormatModifier(
                key, text, powerContext.HighlightValue(GetStackCount(powerContext)));
        }

        private int GetStackCount(ModifierPowerContext powerContext)
        {
            return Mathf.Max(0, Mathf.FloorToInt(powerContext.Scale(Mathf.Max(0, stacks))));
        }
    }
}

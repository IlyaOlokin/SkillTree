using Battle;
using LocalizationSupport;
using UnityEngine;

namespace SkillTree
{
    [CreateAssetMenu(menuName = "Modifiers/Special/Modifier Container Per Player Level", fileName = "New ModifierContainerPerPlayerLevel")]
    public class ModifierPerPlayerLevel : Modifier
    {
        [SerializeField] private ModifierContainer modifierContainer;

        public override bool IsApplicable(Unit unit)
        {
            return GetPlayerLevel(unit) > 0;
        }

        public override IModifierRuntimeBinding CreateRuntimeBinding(Unit unit)
        {
            if (unit is not PlayerUnit playerUnit || playerUnit.UnitLevel == null)
            {
                return null;
            }

            int cachedLevel = GetPlayerLevel(unit);

            void HandleLevelChanged()
            {
                int currentLevel = GetPlayerLevel(unit);
                if (currentLevel == cachedLevel)
                {
                    return;
                }

                cachedLevel = currentLevel;
                unit.RequestModRecalculation();
            }

            return new DelegateModifierRuntimeBinding(
                () => playerUnit.UnitLevel.OnExpChanged += HandleLevelChanged,
                () => playerUnit.UnitLevel.OnExpChanged -= HandleLevelChanged);
        }

        public override void ApplyEffect(Unit unit)
        {
            if (modifierContainer == null)
            {
                return;
            }

            int playerLevel = GetPlayerLevel(unit);
            if (playerLevel <= 0)
            {
                return;
            }

            unit.BaseUnitModifiers.ChangeModifierValue(modifierContainer * playerLevel);
        }

        public override void ApplyEffect(Unit unit, ModifierPowerContext powerContext)
        {
            if (modifierContainer == null)
            {
                return;
            }

            int playerLevel = GetPlayerLevel(unit);
            if (playerLevel <= 0)
            {
                return;
            }

            unit.BaseUnitModifiers.ChangeModifierValue(powerContext.Scale(modifierContainer) * playerLevel);
        }

        public override string GetDescription(ModifierPowerContext powerContext)
        {
            if (modifierContainer == null)
            {
                return GameLocalization.GetModifier(
                    "modifier.modifierPerPlayerLevel.noModifier",
                    "Applies modifier per Player Level");
            }

            return GameLocalization.FormatModifier(
                "modifier.modifierPerPlayerLevel.withModifier",
                "Adds '[[0]]' per Player Level",
                powerContext.Scale(modifierContainer).GetDescription());
        }

        private static int GetPlayerLevel(Unit unit)
        {
            return unit is PlayerUnit playerUnit && playerUnit.UnitLevel != null
                ? Mathf.Max(1, playerUnit.UnitLevel.Level)
                : 0;
        }
    }
}
